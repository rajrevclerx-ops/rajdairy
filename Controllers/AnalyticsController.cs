using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class AnalyticsController : Controller
    {
        private readonly GoogleSheetsService _sheets;
        private readonly DataFilterService _filter;

        private string Username => HttpContext.Session.GetString("AdminUsername") ?? "";
        private string Role => HttpContext.Session.GetString("AdminRole") ?? "Admin";

        public AnalyticsController(GoogleSheetsService sheets, DataFilterService filter)
        {
            _sheets = sheets;
            _filter = filter;
        }

        public async Task<IActionResult> Index()
        {
            var allCollections = await _filter.GetMilkCollections(Username, Role);
            var allGhee = await _sheets.GetGheeProductsByUser(Username, Role);
            var allTransactions = await _filter.GetTransactions(Username, Role);
            var today = DateTime.Today;

            // Last 6 months data
            var monthLabels = new List<string>();
            var monthlyMilk = new List<decimal>();
            var monthlyRevenue = new List<decimal>();
            var monthlyGhee = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                monthLabels.Add(month.ToString("MMM yyyy"));
                var monthData = allCollections.Where(m => m.CollectionDate.Month == month.Month && m.CollectionDate.Year == month.Year);
                monthlyMilk.Add(monthData.Sum(m => m.Quantity));
                monthlyRevenue.Add(monthData.Sum(m => m.TotalAmount));
                var gheeData = allGhee.Where(g => g.ProductionDate.Month == month.Month && g.ProductionDate.Year == month.Year);
                monthlyGhee.Add(gheeData.Sum(g => g.GheeProducedKg));
            }

            // Last 7 days
            var weekLabels = new List<string>();
            var weeklyMilk = new List<decimal>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                weekLabels.Add(date.ToString("ddd"));
                weeklyMilk.Add(allCollections.Where(m => m.CollectionDate == date).Sum(m => m.Quantity));
            }

            // Top farmers
            var topFarmers = allCollections
                .GroupBy(m => m.FarmerName)
                .Select(g => new FarmerStat
                {
                    FarmerName = g.Key,
                    TotalQuantity = g.Sum(m => m.Quantity),
                    AvgFat = g.Average(m => m.FatPercentage),
                    TotalEntries = g.Count()
                })
                .OrderByDescending(f => f.TotalQuantity)
                .Take(10)
                .ToList();

            // Product distribution from transactions
            var productGroups = allTransactions
                .GroupBy(t => t.Item.ToString())
                .Select(g => new { Name = g.Key, Total = g.Sum(t => t.TotalAmount) })
                .OrderByDescending(x => x.Total)
                .ToList();

            // Growth calculation (this month vs last month)
            var thisMonth = allCollections.Where(m => m.CollectionDate.Month == today.Month && m.CollectionDate.Year == today.Year);
            var lastMonth = allCollections.Where(m => m.CollectionDate.Month == today.AddMonths(-1).Month && m.CollectionDate.Year == today.AddMonths(-1).Year);
            var thisMilk = thisMonth.Sum(m => m.Quantity);
            var lastMilk = lastMonth.Sum(m => m.Quantity);
            var thisRev = thisMonth.Sum(m => m.TotalAmount);
            var lastRev = lastMonth.Sum(m => m.TotalAmount);

            var model = new AnalyticsViewModel
            {
                MonthLabels = monthLabels,
                MonthlyMilkData = monthlyMilk,
                MonthlyRevenueData = monthlyRevenue,
                MonthlyGheeData = monthlyGhee,
                WeekLabels = weekLabels,
                WeeklyMilkData = weeklyMilk,
                TopFarmers = topFarmers,
                ProductLabels = productGroups.Select(x => x.Name).ToList(),
                ProductData = productGroups.Select(x => x.Total).ToList(),
                TotalMilkCollected = allCollections.Sum(m => m.Quantity),
                TotalRevenueGenerated = allCollections.Sum(m => m.TotalAmount),
                TotalGheeProduced = allGhee.Sum(g => g.GheeProducedKg),
                TotalTransactions = allTransactions.Count,
                AvgDailyMilk = allCollections.Any() ? allCollections.GroupBy(m => m.CollectionDate).Average(g => g.Sum(m => m.Quantity)) : 0,
                AvgFatPercent = allCollections.Any() ? allCollections.Average(m => m.FatPercentage) : 0,
                AvgSNFPercent = allCollections.Any() ? allCollections.Average(m => m.SNFPercentage) : 0,
                MilkGrowthPercent = lastMilk > 0 ? ((thisMilk - lastMilk) / lastMilk) * 100 : 0,
                RevenueGrowthPercent = lastRev > 0 ? ((thisRev - lastRev) / lastRev) * 100 : 0
            };

            return View(model);
        }
    }
}
