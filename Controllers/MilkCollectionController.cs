using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class MilkCollectionController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public MilkCollectionController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Index(string? searchFarmer, MilkType? milkType, DateTime? fromDate, DateTime? toDate)
        {
            var all = await _sheets.GetAllMilkCollections();

            if (!string.IsNullOrEmpty(searchFarmer))
                all = all.Where(m => m.FarmerName.Contains(searchFarmer, StringComparison.OrdinalIgnoreCase)).ToList();
            if (milkType.HasValue)
                all = all.Where(m => m.MilkType == milkType.Value).ToList();
            if (fromDate.HasValue)
                all = all.Where(m => m.CollectionDate >= fromDate.Value).ToList();
            if (toDate.HasValue)
                all = all.Where(m => m.CollectionDate <= toDate.Value).ToList();

            ViewBag.SearchFarmer = searchFarmer;
            ViewBag.MilkType = milkType;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var collections = all
                .OrderByDescending(m => m.CollectionDate)
                .ThenByDescending(m => m.CreatedAt)
                .ToList();

            return View(collections);
        }

        // ============ QUICK ENTRY - Modern Daily Collection ============
        public async Task<IActionResult> QuickEntry()
        {
            var partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            ViewBag.Partners = partners.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
            ViewBag.Today = DateTime.Today.ToString("yyyy-MM-dd");
            ViewBag.CurrentShift = DateTime.Now.Hour < 14 ? "Morning" : "Evening";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickEntry(MilkCollection milkCollection)
        {
            if (ModelState.IsValid)
            {
                // Auto-calculate rate
                var rate = await _sheets.FindMatchingRate(milkCollection.MilkType, milkCollection.FatPercentage, milkCollection.SNFPercentage);
                if (rate != null)
                    milkCollection.RatePerLiter = rate.RatePerLiter;

                milkCollection.TotalAmount = milkCollection.Quantity * milkCollection.RatePerLiter;
                milkCollection.CreatedAt = DateTime.Now;

                await _sheets.AddMilkCollection(milkCollection);

                // Auto-create Transaction in Partner Ledger (farmer ka hisab)
                var allPartners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
                var farmerPartner = allPartners.FirstOrDefault(p =>
                    p.Name.Equals(milkCollection.FarmerName, StringComparison.OrdinalIgnoreCase));

                if (farmerPartner != null)
                {
                    var txn = new Transaction
                    {
                        PartnerId = farmerPartner.Id,
                        PartnerName = farmerPartner.Name,
                        Type = TransactionType.Received, // Humne milk LIYA farmer se
                        Item = TransactionItem.Milk,
                        Description = $"{milkCollection.MilkType} Milk - {milkCollection.Shift} | Fat:{milkCollection.FatPercentage}% SNF:{milkCollection.SNFPercentage}%",
                        Quantity = milkCollection.Quantity,
                        Unit = "Liter",
                        Rate = milkCollection.RatePerLiter,
                        TotalAmount = milkCollection.TotalAmount,
                        PaymentStatus = PaymentStatus.Pending,
                        TransactionDate = milkCollection.CollectionDate,
                        CreatedAt = DateTime.Now,
                        Remarks = $"{milkCollection.Shift} shift milk collection"
                    };
                    await _sheets.AddTransaction(txn);
                }

                // Generate WhatsApp receipt URL
                var receiptText = System.Net.WebUtility.UrlEncode(
                    $"🥛 *Raj Dairy - Milk Receipt*\n" +
                    $"━━━━━━━━━━━━━━━━━━\n" +
                    $"👤 {milkCollection.FarmerName}\n" +
                    $"📅 {milkCollection.CollectionDate:dd MMM yyyy}\n" +
                    $"🌅 Shift: {milkCollection.Shift}\n" +
                    $"━━━━━━━━━━━━━━━━━━\n" +
                    $"🐄 Type: {milkCollection.MilkType} Milk\n" +
                    $"📊 Quantity: {milkCollection.Quantity:F1} Liter\n" +
                    $"🔬 Fat: {milkCollection.FatPercentage:F1}%\n" +
                    $"🔬 SNF: {milkCollection.SNFPercentage:F1}%\n" +
                    $"💰 Rate: ₹{milkCollection.RatePerLiter:F2}/L\n" +
                    $"━━━━━━━━━━━━━━━━━━\n" +
                    $"💵 *Total: ₹{milkCollection.TotalAmount:F2}*\n" +
                    $"━━━━━━━━━━━━━━━━━━\n" +
                    $"✅ Raj Dairy - Thank You!\n" +
                    $"📞 Contact: Admin"
                );

                var mobile = milkCollection.MobileNumber;
                if (!mobile.StartsWith("+")) mobile = "91" + mobile;

                TempData["Success"] = "Milk collection add ho gayi!";
                TempData["WhatsAppUrl"] = $"https://wa.me/{mobile}?text={receiptText}";
                TempData["ReceiptData"] = System.Text.Json.JsonSerializer.Serialize(new
                {
                    milkCollection.FarmerName,
                    milkCollection.MobileNumber,
                    Date = milkCollection.CollectionDate.ToString("dd MMM yyyy"),
                    Shift = milkCollection.Shift.ToString(),
                    MilkType = milkCollection.MilkType.ToString(),
                    Qty = milkCollection.Quantity.ToString("F1"),
                    Fat = milkCollection.FatPercentage.ToString("F1"),
                    SNF = milkCollection.SNFPercentage.ToString("F1"),
                    Rate = milkCollection.RatePerLiter.ToString("F2"),
                    Total = milkCollection.TotalAmount.ToString("F2")
                });

                return RedirectToAction(nameof(QuickEntry));
            }

            var partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            ViewBag.Partners = partners.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
            ViewBag.Today = DateTime.Today.ToString("yyyy-MM-dd");
            ViewBag.CurrentShift = DateTime.Now.Hour < 14 ? "Morning" : "Evening";
            return View(milkCollection);
        }

        // API: Get farmer/partner details
        [HttpGet]
        public async Task<IActionResult> GetPartnerDetails(int partnerId)
        {
            var partner = await _sheets.GetPartnerById(partnerId);
            if (partner == null) return Json(new { found = false });

            // Get last collection for this farmer to pre-fill milk type
            var collections = await _sheets.GetAllMilkCollections();
            var lastEntry = collections
                .Where(c => c.FarmerName == partner.Name)
                .OrderByDescending(c => c.CollectionDate)
                .ThenByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            return Json(new
            {
                found = true,
                name = partner.Name,
                mobile = partner.Mobile,
                milkType = lastEntry?.MilkType.ToString() ?? "Cow",
                lastQty = lastEntry?.Quantity ?? 0,
                lastFat = lastEntry?.FatPercentage ?? 0,
                lastSnf = lastEntry?.SNFPercentage ?? 0
            });
        }

        // Old Create - kept for backward compatibility
        public IActionResult Create()
        {
            return RedirectToAction(nameof(QuickEntry));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MilkCollection milkCollection)
        {
            if (ModelState.IsValid)
            {
                var rate = await _sheets.FindMatchingRate(milkCollection.MilkType, milkCollection.FatPercentage, milkCollection.SNFPercentage);
                if (rate != null)
                    milkCollection.RatePerLiter = rate.RatePerLiter;

                milkCollection.TotalAmount = milkCollection.Quantity * milkCollection.RatePerLiter;
                milkCollection.CreatedAt = DateTime.Now;

                await _sheets.AddMilkCollection(milkCollection);
                TempData["Success"] = "Milk collection add ho gayi!";
                return RedirectToAction(nameof(Index));
            }
            return View(milkCollection);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var milkCollection = await _sheets.GetMilkCollectionById(id.Value);
            if (milkCollection == null) return NotFound();
            return View(milkCollection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MilkCollection milkCollection)
        {
            if (id != milkCollection.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var rate = await _sheets.FindMatchingRate(milkCollection.MilkType, milkCollection.FatPercentage, milkCollection.SNFPercentage);
                if (rate != null)
                    milkCollection.RatePerLiter = rate.RatePerLiter;

                milkCollection.TotalAmount = milkCollection.Quantity * milkCollection.RatePerLiter;

                await _sheets.UpdateMilkCollection(milkCollection);
                TempData["Success"] = "Milk collection update ho gayi!";
                return RedirectToAction(nameof(Index));
            }
            return View(milkCollection);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var milkCollection = await _sheets.GetMilkCollectionById(id.Value);
            if (milkCollection == null) return NotFound();
            return View(milkCollection);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var milkCollection = await _sheets.GetMilkCollectionById(id.Value);
            if (milkCollection == null) return NotFound();
            return View(milkCollection);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _sheets.DeleteMilkCollection(id);
            TempData["Success"] = "Milk collection delete ho gayi!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetRate(MilkType milkType, decimal fat, decimal snf)
        {
            var rate = await _sheets.FindMatchingRate(milkType, fat, snf);
            return Json(new { ratePerLiter = rate?.RatePerLiter ?? 0 });
        }

        [HttpGet]
        public async Task<IActionResult> GetTodayStats()
        {
            var all = await _sheets.GetAllMilkCollections();
            var today = all.Where(m => m.CollectionDate == DateTime.Today).ToList();

            return Json(new
            {
                totalMilk = today.Sum(m => m.Quantity).ToString("F1"),
                totalRevenue = today.Sum(m => m.TotalAmount).ToString("N0"),
                totalFarmers = today.Select(m => m.FarmerName).Distinct().Count(),
                avgFat = today.Any() ? today.Average(m => m.FatPercentage).ToString("F1") : "0",
                avgSnf = today.Any() ? today.Average(m => m.SNFPercentage).ToString("F1") : "0",
                entries = today.OrderByDescending(m => m.CreatedAt).Take(20).Select(m => new
                {
                    name = m.FarmerName,
                    shift = m.Shift.ToString(),
                    milkType = m.MilkType.ToString(),
                    qty = m.Quantity.ToString("F1"),
                    fat = m.FatPercentage.ToString("F1"),
                    snf = m.SNFPercentage.ToString("F1"),
                    amount = m.TotalAmount.ToString("N0")
                })
            });
        }
    }
}
