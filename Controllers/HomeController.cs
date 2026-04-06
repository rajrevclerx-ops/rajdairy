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

            var model = new DashboardViewModel
            {
                TodayMilkCollection = todayCollections.Sum(m => m.Quantity),
                TotalFarmers = allCollections.Select(m => m.FarmerName).Distinct().Count(),
                TotalGheeStock = allGhee.Sum(g => g.StockKg),
                TotalProducts = allProducts.Count(p => p.IsActive),
                TodayRevenue = todayCollections.Sum(m => m.TotalAmount),
                AvgFat = allCollections.Any() ? allCollections.Average(m => m.FatPercentage) : 0,
                AvgSNF = allCollections.Any() ? allCollections.Average(m => m.SNFPercentage) : 0,
                RecentCollections = allCollections
                    .OrderByDescending(m => m.CollectionDate)
                    .ThenByDescending(m => m.CreatedAt)
                    .Take(5).ToList(),
                RecentGheeProduction = allGhee
                    .OrderByDescending(g => g.ProductionDate)
                    .Take(5).ToList()
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
