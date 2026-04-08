using Microsoft.AspNetCore.Mvc;
using DairyProductApp.Models;
using DairyProductApp.Services;
using DairyProductApp.Filters;

namespace DairyProductApp.Controllers
{
    [AdminOnly]
    public class ToolsController : Controller
    {
        private readonly GoogleSheetsService _sheets;

        public ToolsController(GoogleSheetsService sheets)
        {
            _sheets = sheets;
        }

        // Rate Calculator - standalone tool for field use
        public async Task<IActionResult> RateCalculator()
        {
            var rates = await _sheets.GetAllMilkRates();
            ViewBag.Rates = rates.Where(r => r.IsActive).OrderBy(r => r.MilkType).ThenBy(r => r.MinFat).ToList();
            return View();
        }

        // Farmer Attendance - who came today, who didn't
        public async Task<IActionResult> Attendance(DateTime? date)
        {
            date ??= DateTime.Today;
            var partners = (await _sheets.GetAllPartners()).Where(p => p.IsActive).ToList();
            var collections = await _sheets.GetAllMilkCollections();
            var dayCollections = collections.Where(m => m.CollectionDate == date).ToList();

            var attendance = new List<FarmerAttendanceViewModel>();

            foreach (var p in partners.Where(p => p.Type != PartnerType.Buyer))
            {
                var farmerEntries = dayCollections
                    .Where(m => m.FarmerName.Equals(p.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var morningEntry = farmerEntries.FirstOrDefault(m => m.Shift == Shift.Morning);
                var eveningEntry = farmerEntries.FirstOrDefault(m => m.Shift == Shift.Evening);

                // Calculate streak - consecutive days
                var streak = 0;
                var checkDate = date.Value;
                while (true)
                {
                    var hasEntry = collections.Any(m =>
                        m.FarmerName.Equals(p.Name, StringComparison.OrdinalIgnoreCase)
                        && m.CollectionDate == checkDate);
                    if (hasEntry) { streak++; checkDate = checkDate.AddDays(-1); }
                    else break;
                }

                // Last 7 days attendance
                var last7 = new List<bool>();
                for (int i = 6; i >= 0; i--)
                {
                    var d = date.Value.AddDays(-i);
                    last7.Add(collections.Any(m =>
                        m.FarmerName.Equals(p.Name, StringComparison.OrdinalIgnoreCase)
                        && m.CollectionDate == d));
                }

                attendance.Add(new FarmerAttendanceViewModel
                {
                    PartnerId = p.Id,
                    FarmerName = p.Name,
                    Mobile = p.Mobile,
                    MorningPresent = morningEntry != null,
                    EveningPresent = eveningEntry != null,
                    MorningQty = morningEntry?.Quantity ?? 0,
                    EveningQty = eveningEntry?.Quantity ?? 0,
                    TotalQty = (morningEntry?.Quantity ?? 0) + (eveningEntry?.Quantity ?? 0),
                    Streak = streak,
                    Last7Days = last7
                });
            }

            ViewBag.Date = date.Value;
            ViewBag.DateStr = date.Value.ToString("yyyy-MM-dd");
            ViewBag.TotalFarmers = attendance.Count;
            ViewBag.PresentCount = attendance.Count(a => a.MorningPresent || a.EveningPresent);
            ViewBag.AbsentCount = attendance.Count(a => !a.MorningPresent && !a.EveningPresent);
            ViewBag.TotalMilk = attendance.Sum(a => a.TotalQty);

            return View(attendance.OrderByDescending(a => a.TotalQty > 0).ThenByDescending(a => a.Streak).ToList());
        }

        // Farmer Leaderboard
        public async Task<IActionResult> Leaderboard(int? month)
        {
            month ??= DateTime.Today.Month;
            var year = DateTime.Today.Year;

            var collections = (await _sheets.GetAllMilkCollections())
                .Where(m => m.CollectionDate.Month == month && m.CollectionDate.Year == year)
                .ToList();

            var leaderboard = collections
                .GroupBy(m => m.FarmerName)
                .Select(g => new FarmerRankViewModel
                {
                    FarmerName = g.Key,
                    TotalMilk = g.Sum(m => m.Quantity),
                    TotalAmount = g.Sum(m => m.TotalAmount),
                    AvgFat = g.Average(m => m.FatPercentage),
                    AvgSNF = g.Average(m => m.SNFPercentage),
                    TotalDays = g.Select(m => m.CollectionDate).Distinct().Count(),
                    TotalEntries = g.Count()
                })
                .OrderByDescending(f => f.TotalMilk)
                .ToList();

            ViewBag.Month = month;
            ViewBag.MonthName = new DateTime(year, month.Value, 1).ToString("MMMM yyyy");

            return View(leaderboard);
        }

        // Delivery Route - area-wise order grouping
        public async Task<IActionResult> DeliveryRoute(DateTime? date)
        {
            date ??= DateTime.Today;
            var allOrders = await _sheets.GetAllOrders();
            var dayOrders = allOrders
                .Where(o => o.DeliveryDate == date &&
                    (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.OutForDelivery))
                .OrderBy(o => o.DeliveryAddress)
                .ToList();

            // Group by area (first word of address)
            var routes = dayOrders
                .GroupBy(o => string.IsNullOrEmpty(o.DeliveryAddress) ? "No Address" :
                    o.DeliveryAddress.Split(',')[0].Trim())
                .Select(g => new DeliveryRouteViewModel
                {
                    Area = g.Key,
                    Orders = g.ToList(),
                    TotalOrders = g.Count(),
                    TotalAmount = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(r => r.TotalOrders)
                .ToList();

            ViewBag.Date = date.Value;
            ViewBag.DateStr = date.Value.ToString("yyyy-MM-dd");
            ViewBag.TotalOrders = dayOrders.Count;
            ViewBag.TotalAmount = dayOrders.Sum(o => o.TotalAmount);

            return View(routes);
        }
    }

    public class FarmerRankViewModel
    {
        public string FarmerName { get; set; } = string.Empty;
        public decimal TotalMilk { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AvgFat { get; set; }
        public decimal AvgSNF { get; set; }
        public int TotalDays { get; set; }
        public int TotalEntries { get; set; }
    }

    public class DeliveryRouteViewModel
    {
        public string Area { get; set; } = string.Empty;
        public List<Order> Orders { get; set; } = new();
        public int TotalOrders { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class FarmerAttendanceViewModel
    {
        public int PartnerId { get; set; }
        public string FarmerName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public bool MorningPresent { get; set; }
        public bool EveningPresent { get; set; }
        public decimal MorningQty { get; set; }
        public decimal EveningQty { get; set; }
        public decimal TotalQty { get; set; }
        public int Streak { get; set; }
        public List<bool> Last7Days { get; set; } = new();
    }
}
