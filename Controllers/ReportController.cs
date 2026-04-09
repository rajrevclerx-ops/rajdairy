using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class ReportController : Controller
    {
        private readonly GoogleSheetsService _sheets;
        private readonly DataFilterService _filter;

        private string Username => HttpContext.Session.GetString("AdminUsername") ?? "";
        private string Role => HttpContext.Session.GetString("AdminRole") ?? "Admin";

        public ReportController(GoogleSheetsService sheets, DataFilterService filter)
        {
            _sheets = sheets;
            _filter = filter;
        }

        public async Task<IActionResult> Daily(DateTime? date)
        {
            date ??= DateTime.Today;

            var collections = (await _filter.GetMilkCollections(Username, Role))
                .Where(m => m.CollectionDate == date).ToList();
            var orders = (await _filter.GetOrders(Username, Role))
                .Where(o => o.OrderDate == date).ToList();
            var expenses = (await _sheets.GetAllExpenses())
                .Where(e => e.ExpenseDate == date).ToList();
            var subscriptions = (await _filter.GetSubscriptions(Username, Role))
                .Where(s => s.CreatedAt.Date == date).ToList();

            var model = new DailyReportViewModel
            {
                ReportDate = date.Value,
                TotalMilkCollected = collections.Sum(c => c.Quantity),
                TotalFarmers = collections.Select(c => c.FarmerName).Distinct().Count(),
                AvgFat = collections.Any() ? collections.Average(c => c.FatPercentage) : 0,
                AvgSNF = collections.Any() ? collections.Average(c => c.SNFPercentage) : 0,
                MilkRevenue = collections.Sum(c => c.TotalAmount),
                TotalOrders = orders.Count,
                OrderRevenue = orders.Sum(o => o.TotalAmount),
                TotalExpenses = expenses.Sum(e => e.Amount),
                NewSubscriptions = subscriptions.Count,
                Collections = collections,
                Expenses = expenses
            };
            model.NetDayProfit = model.MilkRevenue + model.OrderRevenue - model.TotalExpenses;

            return View(model);
        }
    }
}
