using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;
using System.Text;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class PaymentController : Controller
    {
        private readonly GoogleSheetsService _sheets;
        private readonly DataFilterService _filter;

        private string Username => HttpContext.Session.GetString("AdminUsername") ?? "";
        private string Role => HttpContext.Session.GetString("AdminRole") ?? "Admin";

        public PaymentController(GoogleSheetsService sheets, DataFilterService filter)
        {
            _sheets = sheets;
            _filter = filter;
        }

        // Bulk Payment Page - shows all farmers with pending amounts
        public async Task<IActionResult> BulkPayment()
        {
            var partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            var allCollections = await _filter.GetMilkCollections(Username, Role);
            var allTransactions = await _filter.GetTransactions(Username, Role);

            var currentMonth = DateTime.Today.Month;
            var currentYear = DateTime.Today.Year;

            var farmerBalances = new List<FarmerBalanceViewModel>();

            foreach (var partner in partners.Where(p => p.IsActive))
            {
                var monthCollections = allCollections
                    .Where(m => m.FarmerName.Equals(partner.Name, StringComparison.OrdinalIgnoreCase)
                        && m.CollectionDate.Month == currentMonth
                        && m.CollectionDate.Year == currentYear)
                    .ToList();

                var monthPayments = allTransactions
                    .Where(t => t.PartnerId == partner.Id
                        && t.Type == TransactionType.Given
                        && t.Item == TransactionItem.Cash
                        && t.TransactionDate.Month == currentMonth
                        && t.TransactionDate.Year == currentYear)
                    .ToList();

                var milkAmount = monthCollections.Sum(m => m.TotalAmount);
                var paidAmount = monthPayments.Sum(t => t.TotalAmount);
                var balance = milkAmount - paidAmount;

                if (milkAmount > 0 || paidAmount > 0)
                {
                    farmerBalances.Add(new FarmerBalanceViewModel
                    {
                        PartnerId = partner.Id,
                        PartnerName = partner.Name,
                        Mobile = partner.Mobile,
                        TotalMilk = monthCollections.Sum(m => m.Quantity),
                        MilkAmount = milkAmount,
                        PaidAmount = paidAmount,
                        BalanceDue = balance
                    });
                }
            }

            ViewBag.MonthName = new DateTime(currentYear, currentMonth, 1).ToString("MMMM yyyy");
            ViewBag.TotalDue = farmerBalances.Sum(f => f.BalanceDue);
            return View(farmerBalances.OrderByDescending(f => f.BalanceDue).ToList());
        }

        // Pay single farmer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayFarmer(int partnerId, decimal amount, string mode)
        {
            var partner = await _sheets.GetPartnerById(partnerId);
            if (partner == null) return NotFound();

            var txn = new Transaction
            {
                PartnerId = partner.Id,
                PartnerName = partner.Name,
                Type = TransactionType.Given,
                Item = TransactionItem.Cash,
                Description = $"Payment - {mode}",
                Quantity = 1,
                Unit = "Rupees",
                Rate = amount,
                TotalAmount = amount,
                PaymentStatus = PaymentStatus.Paid,
                TransactionDate = DateTime.Today,
                CreatedAt = DateTime.Now,
                Remarks = $"Bulk payment via {mode}"
            };
            await _sheets.AddTransaction(txn);

            TempData["Success"] = $"₹{amount:N0} payment {partner.Name} ko diya!";
            return RedirectToAction(nameof(BulkPayment));
        }

        // Export Milk Collections to CSV
        public async Task<IActionResult> ExportMilkCSV(int? month, int? year)
        {
            month ??= DateTime.Today.Month;
            year ??= DateTime.Today.Year;

            var collections = (await _filter.GetMilkCollections(Username, Role))
                .Where(m => m.CollectionDate.Month == month && m.CollectionDate.Year == year)
                .OrderBy(m => m.CollectionDate)
                .ThenBy(m => m.FarmerName)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Date,Shift,Farmer,MilkType,Quantity(L),Fat%,SNF%,Rate/L,TotalAmount");

            foreach (var c in collections)
            {
                sb.AppendLine($"{c.CollectionDate:dd/MM/yyyy},{c.Shift},{c.FarmerName},{c.MilkType},{c.Quantity:F1},{c.FatPercentage:F1},{c.SNFPercentage:F1},{c.RatePerLiter:F2},{c.TotalAmount:F2}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"MilkCollection_{month}_{year}.csv");
        }

        // Export Farmer Summary to CSV
        public async Task<IActionResult> ExportFarmerCSV()
        {
            var partners = await _sheets.GetPartnersByUser(HttpContext.Session.GetString("AdminUsername") ?? "", HttpContext.Session.GetString("AdminRole") ?? "Admin");
            var allCollections = await _filter.GetMilkCollections(Username, Role);
            var allTransactions = await _filter.GetTransactions(Username, Role);

            var month = DateTime.Today.Month;
            var year = DateTime.Today.Year;

            var sb = new StringBuilder();
            sb.AppendLine("Farmer,Mobile,TotalMilk(L),MilkAmount,PaidAmount,BalanceDue");

            foreach (var p in partners.Where(x => x.IsActive))
            {
                var milk = allCollections
                    .Where(m => m.FarmerName.Equals(p.Name, StringComparison.OrdinalIgnoreCase)
                        && m.CollectionDate.Month == month && m.CollectionDate.Year == year)
                    .Sum(m => m.TotalAmount);

                var paid = allTransactions
                    .Where(t => t.PartnerId == p.Id && t.Type == TransactionType.Given
                        && t.Item == TransactionItem.Cash
                        && t.TransactionDate.Month == month && t.TransactionDate.Year == year)
                    .Sum(t => t.TotalAmount);

                var totalMilk = allCollections
                    .Where(m => m.FarmerName.Equals(p.Name, StringComparison.OrdinalIgnoreCase)
                        && m.CollectionDate.Month == month && m.CollectionDate.Year == year)
                    .Sum(m => m.Quantity);

                sb.AppendLine($"{p.Name},{p.Mobile},{totalMilk:F1},{milk:F2},{paid:F2},{(milk - paid):F2}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"FarmerSummary_{month}_{year}.csv");
        }

        // Export Expenses to CSV
        public async Task<IActionResult> ExportExpenseCSV(int? month, int? year)
        {
            month ??= DateTime.Today.Month;
            year ??= DateTime.Today.Year;

            var expenses = (await _sheets.GetAllExpenses())
                .Where(e => e.ExpenseDate.Month == month && e.ExpenseDate.Year == year)
                .OrderBy(e => e.ExpenseDate)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Date,Category,Description,Amount,PaymentMode,Remarks");

            foreach (var e in expenses)
            {
                sb.AppendLine($"{e.ExpenseDate:dd/MM/yyyy},{e.Category},{e.Description},{e.Amount:F2},{e.Mode},{e.Remarks}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"Expenses_{month}_{year}.csv");
        }
    }

    public class FarmerBalanceViewModel
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public decimal TotalMilk { get; set; }
        public decimal MilkAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceDue { get; set; }
    }
}
