namespace DairyProductApp.Models
{
    public class DashboardViewModel
    {
        public decimal TodayMilkCollection { get; set; }
        public decimal TotalFarmers { get; set; }
        public decimal TotalGheeStock { get; set; }
        public int TotalProducts { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgFat { get; set; }
        public decimal AvgSNF { get; set; }
        public int TotalPartners { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TodayOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal PendingPayments { get; set; }
        public List<MilkCollection> RecentCollections { get; set; } = new();
        public List<GheeProduct> RecentGheeProduction { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();

        // Chart Data - Last 7 days
        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> DailyMilkData { get; set; } = new();
        public List<decimal> DailyRevenueData { get; set; } = new();

        // Milk Type Distribution
        public decimal CowMilkPercent { get; set; }
        public decimal BuffaloMilkPercent { get; set; }
        public decimal MixedMilkPercent { get; set; }
    }

    public class AnalyticsViewModel
    {
        // Monthly Data
        public List<string> MonthLabels { get; set; } = new();
        public List<decimal> MonthlyMilkData { get; set; } = new();
        public List<decimal> MonthlyRevenueData { get; set; } = new();
        public List<decimal> MonthlyGheeData { get; set; } = new();

        // Weekly Data
        public List<string> WeekLabels { get; set; } = new();
        public List<decimal> WeeklyMilkData { get; set; } = new();

        // Top Farmers
        public List<FarmerStat> TopFarmers { get; set; } = new();

        // Product Distribution
        public List<string> ProductLabels { get; set; } = new();
        public List<decimal> ProductData { get; set; } = new();

        // Summary
        public decimal TotalMilkCollected { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
        public decimal TotalGheeProduced { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AvgDailyMilk { get; set; }
        public decimal AvgFatPercent { get; set; }
        public decimal AvgSNFPercent { get; set; }

        // Growth indicators
        public decimal MilkGrowthPercent { get; set; }
        public decimal RevenueGrowthPercent { get; set; }
    }

    public class FarmerStat
    {
        public string FarmerName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal AvgFat { get; set; }
        public int TotalEntries { get; set; }
    }
}
