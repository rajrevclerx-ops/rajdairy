namespace DairyProductApp.Models
{
    public class DashboardViewModel
    {
        public decimal TodayMilkCollection { get; set; }
        public decimal TotalFarmers { get; set; }
        public decimal TotalGheeStock { get; set; }
        public int TotalProducts { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal AvgFat { get; set; }
        public decimal AvgSNF { get; set; }
        public List<MilkCollection> RecentCollections { get; set; } = new();
        public List<GheeProduct> RecentGheeProduction { get; set; } = new();
    }
}
