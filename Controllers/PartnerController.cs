using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class PartnerController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public PartnerController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Index()
        {
            var partners = await _sheets.GetAllPartners();
            return View(partners.OrderBy(p => p.Name).ToList());
        }

        public IActionResult Create()
        {
            return View(new Partner());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Partner partner)
        {
            if (ModelState.IsValid)
            {
                partner.CreatedAt = DateTime.Now;
                await _sheets.AddPartner(partner);
                TempData["Success"] = $"Partner '{partner.Name}' add ho gaya! Access Code: {partner.AccessCode}";
                return RedirectToAction(nameof(Index));
            }
            return View(partner);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var partner = await _sheets.GetPartnerById(id.Value);
            if (partner == null) return NotFound();
            return View(partner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Partner partner)
        {
            if (id != partner.Id) return NotFound();
            if (ModelState.IsValid)
            {
                await _sheets.UpdatePartner(partner);
                TempData["Success"] = "Partner update ho gaya!";
                return RedirectToAction(nameof(Index));
            }
            return View(partner);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var partner = await _sheets.GetPartnerById(id.Value);
            if (partner == null) return NotFound();
            return View(partner);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _sheets.DeletePartner(id);
            TempData["Success"] = "Partner delete ho gaya!";
            return RedirectToAction(nameof(Index));
        }

        // Partner ka Ledger (hisab-kitab)
        public async Task<IActionResult> Ledger(int? id)
        {
            if (id == null) return NotFound();
            var partner = await _sheets.GetPartnerById(id.Value);
            if (partner == null) return NotFound();

            var transactions = await _sheets.GetTransactionsByPartnerId(id.Value);

            var model = new PartnerLedgerViewModel
            {
                Partner = partner,
                Transactions = transactions,
                TotalReceived = transactions.Where(t => t.Type == TransactionType.Received).Sum(t => t.TotalAmount),
                TotalGiven = transactions.Where(t => t.Type == TransactionType.Given).Sum(t => t.TotalAmount),
                TotalPending = transactions.Where(t => t.PaymentStatus == PaymentStatus.Pending).Sum(t => t.TotalAmount),
                Balance = transactions.Where(t => t.Type == TransactionType.Given).Sum(t => t.TotalAmount)
                        - transactions.Where(t => t.Type == TransactionType.Received).Sum(t => t.TotalAmount)
            };

            return View(model);
        }

        // Monthly Summary - farmer ka mahine ka hisab
        public async Task<IActionResult> MonthlySummary(int? id, int? month, int? year)
        {
            if (id == null) return NotFound();
            var partner = await _sheets.GetPartnerById(id.Value);
            if (partner == null) return NotFound();

            month ??= DateTime.Today.Month;
            year ??= DateTime.Today.Year;

            var transactions = await _sheets.GetTransactionsByPartnerId(id.Value);
            var monthTxns = transactions
                .Where(t => t.TransactionDate.Month == month && t.TransactionDate.Year == year)
                .OrderBy(t => t.TransactionDate)
                .ToList();

            // Milk collection entries for this partner this month
            var allCollections = await _sheets.GetAllMilkCollections();
            var monthCollections = allCollections
                .Where(m => m.FarmerName.Equals(partner.Name, StringComparison.OrdinalIgnoreCase)
                    && m.CollectionDate.Month == month && m.CollectionDate.Year == year)
                .OrderBy(m => m.CollectionDate)
                .ThenBy(m => m.Shift)
                .ToList();

            // Payment transactions (Cash given to farmer)
            var payments = transactions
                .Where(t => t.TransactionDate.Month == month && t.TransactionDate.Year == year
                    && t.Type == TransactionType.Given && t.Item == TransactionItem.Cash)
                .ToList();

            ViewBag.Partner = partner;
            ViewBag.Month = month;
            ViewBag.Year = year;
            ViewBag.MonthName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");
            ViewBag.Collections = monthCollections;
            ViewBag.TotalMilk = monthCollections.Sum(m => m.Quantity);
            ViewBag.TotalMilkAmount = monthCollections.Sum(m => m.TotalAmount);
            ViewBag.AvgFat = monthCollections.Any() ? monthCollections.Average(m => m.FatPercentage) : 0m;
            ViewBag.AvgSNF = monthCollections.Any() ? monthCollections.Average(m => m.SNFPercentage) : 0m;
            ViewBag.TotalDays = monthCollections.Select(m => m.CollectionDate).Distinct().Count();
            ViewBag.TotalPayments = payments.Sum(p => p.TotalAmount);
            ViewBag.BalanceDue = monthCollections.Sum(m => m.TotalAmount) - payments.Sum(p => p.TotalAmount);

            // WhatsApp statement text
            var bal = monthCollections.Sum(m => m.TotalAmount) - payments.Sum(p => p.TotalAmount);
            ViewBag.WhatsAppText = System.Net.WebUtility.UrlEncode(
                $"📋 *Raj Dairy - Monthly Statement*\n" +
                $"━━━━━━━━━━━━━━━━━━\n" +
                $"👤 {partner.Name}\n" +
                $"📅 {new DateTime(year.Value, month.Value, 1):MMMM yyyy}\n" +
                $"━━━━━━━━━━━━━━━━━━\n" +
                $"🥛 Total Milk: {monthCollections.Sum(m => m.Quantity):F1} L\n" +
                $"📊 Avg Fat: {(monthCollections.Any() ? monthCollections.Average(m => m.FatPercentage) : 0):F1}%\n" +
                $"📊 Avg SNF: {(monthCollections.Any() ? monthCollections.Average(m => m.SNFPercentage) : 0):F1}%\n" +
                $"📆 Collection Days: {monthCollections.Select(m => m.CollectionDate).Distinct().Count()}\n" +
                $"━━━━━━━━━━━━━━━━━━\n" +
                $"💰 Milk Amount: ₹{monthCollections.Sum(m => m.TotalAmount):N0}\n" +
                $"💸 Paid: ₹{payments.Sum(p => p.TotalAmount):N0}\n" +
                $"📌 *Balance Due: ₹{bal:N0}*\n" +
                $"━━━━━━━━━━━━━━━━━━\n" +
                $"✅ Raj Dairy - Thank You!"
            );

            return View();
        }
    }
}
