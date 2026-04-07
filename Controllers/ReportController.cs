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

        public ReportController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Daily(DateTime? date)
        {
            date ??= DateTime.Today;

            var collections = (await _sheets.GetAllMilkCollections())
                .Where(m => m.CollectionDate == date).ToList();
            var orders = (await _sheets.GetAllOrders())
                .Where(o => o.OrderDate == date).ToList();
            var expenses = (await _sheets.GetAllExpenses())
                .Where(e => e.ExpenseDate == date).ToList();
            var subscriptions = (await _sheets.GetAllSubscriptions())
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
