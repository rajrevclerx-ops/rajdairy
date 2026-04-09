using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;

namespace DairyProductApp.Controllers
{
    public class PublicController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public PublicController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string accessCode)
        {
            if (string.IsNullOrEmpty(accessCode))
            {
                ViewBag.Error = "Access Code daalna zaruri hai!";
                return View();
            }

            var partner = await _sheets.GetPartnerByAccessCode(accessCode.Trim().ToUpper());
            if (partner == null)
            {
                ViewBag.Error = "Galat Access Code! Admin se sampark karein.";
                return View();
            }

            return RedirectToAction(nameof(Dashboard), new { code = partner.AccessCode });
        }

        public async Task<IActionResult> Dashboard(string code)
        {
            if (string.IsNullOrEmpty(code)) return RedirectToAction(nameof(Login));

            var partner = await _sheets.GetPartnerByAccessCode(code);
            if (partner == null) return RedirectToAction(nameof(Login));

            var transactions = await _sheets.GetTransactionsByPartnerId(partner.Id);

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

            // Partner's milk collections
            var allCollections = await _sheets.GetAllMilkCollections();
            var myCollections = allCollections
                .Where(m => m.FarmerName.Equals(partner.Name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.CollectionDate)
                .ThenByDescending(m => m.CreatedAt)
                .ToList();
            ViewBag.MilkCollections = myCollections;
            ViewBag.TotalMilk = myCollections.Sum(m => m.Quantity);
            ViewBag.TotalMilkAmount = myCollections.Sum(m => m.TotalAmount);

            // This month's milk
            var thisMonth = myCollections.Where(m => m.CollectionDate.Month == DateTime.Today.Month && m.CollectionDate.Year == DateTime.Today.Year).ToList();
            ViewBag.MonthMilk = thisMonth.Sum(m => m.Quantity);
            ViewBag.MonthMilkAmount = thisMonth.Sum(m => m.TotalAmount);

            // Partner's orders
            var allOrders = await _sheets.GetAllOrders();
            ViewBag.Orders = allOrders
                .Where(o => o.PartnerId == partner.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            // Partner's subscriptions
            var allSubs = await _sheets.GetAllSubscriptions();
            ViewBag.Subscriptions = allSubs
                .Where(s => s.PartnerId == partner.Id)
                .ToList();

            // Payment info
            ViewBag.UpiId = await _sheets.GetSetting("UpiId");
            ViewBag.UpiName = await _sheets.GetSetting("UpiName") ?? "Raj Dairy";
            ViewBag.QrCode = await _sheets.GetSetting("QrCode");

            return View(model);
        }

        // Privacy Policy (required for Play Store)
        public IActionResult Privacy()
        {
            return View();
        }

        // Public order tracking by order number (no login needed)
        public async Task<IActionResult> TrackOrder(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
                return View(model: null);

            var allOrders = await _sheets.GetAllOrders();
            var order = allOrders.FirstOrDefault(o =>
                o.OrderNumber.Equals(orderNumber.Trim(), StringComparison.OrdinalIgnoreCase));

            return View(model: order);
        }
    }
}
