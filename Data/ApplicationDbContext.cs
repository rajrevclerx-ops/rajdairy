using Microsoft.EntityFrameworkCore;
using DairyProductApp.Models;

namespace DairyProductApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MilkCollection> MilkCollections { get; set; }
        public DbSet<MilkRate> MilkRates { get; set; }
        public DbSet<DairyProduct> DairyProducts { get; set; }
        public DbSet<GheeProduct> GheeProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed Milk Rates
            modelBuilder.Entity<MilkRate>().HasData(
                // Cow Milk Rates
                new MilkRate { Id = 1, MilkType = MilkType.Cow, MinFat = 3.0m, MaxFat = 4.0m, MinSNF = 8.0m, MaxSNF = 8.5m, RatePerLiter = 32m, EffectiveFrom = DateTime.Today, IsActive = true },
                new MilkRate { Id = 2, MilkType = MilkType.Cow, MinFat = 4.0m, MaxFat = 5.0m, MinSNF = 8.5m, MaxSNF = 9.0m, RatePerLiter = 38m, EffectiveFrom = DateTime.Today, IsActive = true },
                new MilkRate { Id = 3, MilkType = MilkType.Cow, MinFat = 5.0m, MaxFat = 6.0m, MinSNF = 9.0m, MaxSNF = 9.5m, RatePerLiter = 44m, EffectiveFrom = DateTime.Today, IsActive = true },
                // Buffalo Milk Rates
                new MilkRate { Id = 4, MilkType = MilkType.Buffalo, MinFat = 5.0m, MaxFat = 6.5m, MinSNF = 8.5m, MaxSNF = 9.0m, RatePerLiter = 45m, EffectiveFrom = DateTime.Today, IsActive = true },
                new MilkRate { Id = 5, MilkType = MilkType.Buffalo, MinFat = 6.5m, MaxFat = 8.0m, MinSNF = 9.0m, MaxSNF = 9.5m, RatePerLiter = 55m, EffectiveFrom = DateTime.Today, IsActive = true },
                new MilkRate { Id = 6, MilkType = MilkType.Buffalo, MinFat = 8.0m, MaxFat = 10.0m, MinSNF = 9.5m, MaxSNF = 10.0m, RatePerLiter = 65m, EffectiveFrom = DateTime.Today, IsActive = true },
                // Mixed Milk Rates
                new MilkRate { Id = 7, MilkType = MilkType.Mixed, MinFat = 3.5m, MaxFat = 5.0m, MinSNF = 8.0m, MaxSNF = 8.5m, RatePerLiter = 35m, EffectiveFrom = DateTime.Today, IsActive = true },
                new MilkRate { Id = 8, MilkType = MilkType.Mixed, MinFat = 5.0m, MaxFat = 7.0m, MinSNF = 8.5m, MaxSNF = 9.5m, RatePerLiter = 48m, EffectiveFrom = DateTime.Today, IsActive = true }
            );

            // Seed Dairy Products
            modelBuilder.Entity<DairyProduct>().HasData(
                new DairyProduct { Id = 1, ProductName = "Full Cream Milk", Category = ProductCategory.Milk, Quantity = 1, Unit = ProductUnit.Liter, Price = 65, StockQuantity = 500, ManufacturingDate = DateTime.Today, ExpiryDate = DateTime.Today.AddDays(3), IsActive = true, CreatedAt = DateTime.Now },
                new DairyProduct { Id = 2, ProductName = "Toned Milk", Category = ProductCategory.Milk, Quantity = 1, Unit = ProductUnit.Liter, Price = 52, StockQuantity = 300, ManufacturingDate = DateTime.Today, ExpiryDate = DateTime.Today.AddDays(3), IsActive = true, CreatedAt = DateTime.Now },
                new DairyProduct { Id = 3, ProductName = "Fresh Dahi", Category = ProductCategory.Curd, Quantity = 1, Unit = ProductUnit.Kg, Price = 80, StockQuantity = 100, ManufacturingDate = DateTime.Today, ExpiryDate = DateTime.Today.AddDays(7), IsActive = true, CreatedAt = DateTime.Now },
                new DairyProduct { Id = 4, ProductName = "Fresh Paneer", Category = ProductCategory.Paneer, Quantity = 1, Unit = ProductUnit.Kg, Price = 350, StockQuantity = 50, ManufacturingDate = DateTime.Today, ExpiryDate = DateTime.Today.AddDays(5), IsActive = true, CreatedAt = DateTime.Now },
                new DairyProduct { Id = 5, ProductName = "White Butter", Category = ProductCategory.Butter, Quantity = 500, Unit = ProductUnit.Gram, Price = 280, StockQuantity = 30, ManufacturingDate = DateTime.Today, ExpiryDate = DateTime.Today.AddDays(30), IsActive = true, CreatedAt = DateTime.Now },
                new DairyProduct { Id = 6, ProductName = "Fresh Cream", Category = ProductCategory.Cream, Quantity = 1, Unit = ProductUnit.Liter, Price = 220, StockQuantity = 40, ManufacturingDate = DateTime.Today, ExpiryDate = DateTime.Today.AddDays(7), IsActive = true, CreatedAt = DateTime.Now },
                new DairyProduct { Id = 7, ProductName = "Chaach / Buttermilk", Category = ProductCategory.Buttermilk, Quantity = 1, Unit = ProductUnit.Liter, Price = 30, StockQuantity = 200, ManufacturingDate = DateTime.Today, ExpiryDate = DateTime.Today.AddDays(2), IsActive = true, CreatedAt = DateTime.Now },
                new DairyProduct { Id = 8, ProductName = "Khoya / Mawa", Category = ProductCategory.Khoya, Quantity = 1, Unit = ProductUnit.Kg, Price = 450, StockQuantity = 25, ManufacturingDate = DateTime.Today, ExpiryDate = DateTime.Today.AddDays(10), IsActive = true, CreatedAt = DateTime.Now }
            );

            // Seed Ghee Products
            modelBuilder.Entity<GheeProduct>().HasData(
                new GheeProduct { Id = 1, BatchNumber = "GH-2026-001", GheeType = GheeType.CowGhee, MilkUsedLiters = 100, GheeProducedKg = 5.5m, YieldRate = 5.5m, PricePerKg = 600, TotalValue = 3300, StockKg = 5.5m, ProductionDate = DateTime.Today, ExpiryDate = DateTime.Today.AddMonths(12), Quality = QualityGrade.Premium, CreatedAt = DateTime.Now },
                new GheeProduct { Id = 2, BatchNumber = "GH-2026-002", GheeType = GheeType.BuffaloGhee, MilkUsedLiters = 80, GheeProducedKg = 5.0m, YieldRate = 6.25m, PricePerKg = 700, TotalValue = 3500, StockKg = 5.0m, ProductionDate = DateTime.Today, ExpiryDate = DateTime.Today.AddMonths(12), Quality = QualityGrade.Premium, CreatedAt = DateTime.Now }
            );
        }
    }
}
