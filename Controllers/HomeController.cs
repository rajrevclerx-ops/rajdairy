using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class HomeController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public HomeController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var allCollections = await _sheets.GetAllMilkCollections();
            var todayCollections = allCollections.Where(m => m.CollectionDate == today).ToList();
            var allGhee = await _sheets.GetAllGheeProducts();
            var allProducts = await _sheets.GetAllDairyProducts();
            var allPartners = await _sheets.GetAllPartners();
            var allSubscriptions = await _sheets.GetAllSubscriptions();
            var allOrders = await _sheets.GetAllOrders();
            var allTransactions = await _sheets.GetAllTransactions();

            // Chart data - last 7 days
            var chartLabels = new List<string>();
            var dailyMilk = new List<decimal>();
            var dailyRevenue = new List<decimal>();

            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                chartLabels.Add(date.ToString("dd MMM"));
                var dayData = allCollections.Where(m => m.CollectionDate == date);
                dailyMilk.Add(dayData.Sum(m => m.Quantity));
                dailyRevenue.Add(dayData.Sum(m => m.TotalAmount));
            }

            // Milk type distribution
            var totalMilk = allCollections.Sum(m => m.Quantity);
            var cowMilk = allCollections.Where(m => m.MilkType == MilkType.Cow).Sum(m => m.Quantity);
            var buffaloMilk = allCollections.Where(m => m.MilkType == MilkType.Buffalo).Sum(m => m.Quantity);
            var mixedMilk = allCollections.Where(m => m.MilkType == MilkType.Mixed).Sum(m => m.Quantity);

            // Pending payments
            var pendingPayments = allTransactions
                .Where(t => t.PaymentStatus == PaymentStatus.Pending || t.PaymentStatus == PaymentStatus.Partial)
                .Sum(t => t.TotalAmount);

            var model = new DashboardViewModel
            {
                TodayMilkCollection = todayCollections.Sum(m => m.Quantity),
                TotalFarmers = allCollections.Select(m => m.FarmerName).Distinct().Count(),
                TotalGheeStock = allGhee.Sum(g => g.StockKg),
                TotalProducts = allProducts.Count(p => p.IsActive),
                TodayRevenue = todayCollections.Sum(m => m.TotalAmount),
                TotalRevenue = allCollections.Sum(m => m.TotalAmount),
                AvgFat = allCollections.Any() ? allCollections.Average(m => m.FatPercentage) : 0,
                AvgSNF = allCollections.Any() ? allCollections.Average(m => m.SNFPercentage) : 0,
                TotalPartners = allPartners.Count(p => p.IsActive),
                ActiveSubscriptions = allSubscriptions.Count(s => s.Status == SubscriptionStatus.Active),
                TodayOrders = allOrders.Count(o => o.OrderDate == today),
                PendingOrders = allOrders.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed),
                PendingPayments = pendingPayments,
                RecentCollections = allCollections
                    .OrderByDescending(m => m.CollectionDate)
                    .ThenByDescending(m => m.CreatedAt)
                    .Take(5).ToList(),
                RecentGheeProduction = allGhee
                    .OrderByDescending(g => g.ProductionDate)
                    .Take(5).ToList(),
                RecentOrders = allOrders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5).ToList(),
                ChartLabels = chartLabels,
                DailyMilkData = dailyMilk,
                DailyRevenueData = dailyRevenue,
                CowMilkPercent = totalMilk > 0 ? (cowMilk / totalMilk) * 100 : 0,
                BuffaloMilkPercent = totalMilk > 0 ? (buffaloMilk / totalMilk) * 100 : 0,
                MixedMilkPercent = totalMilk > 0 ? (mixedMilk / totalMilk) * 100 : 0
            };

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
