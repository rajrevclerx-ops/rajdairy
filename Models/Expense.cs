using System.ComponentModel.DataAnnotations;

namespace DairyProductApp.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category select karein")]
        [Display(Name = "Category")]
        public ExpenseCategory Category { get; set; }

        [Required(ErrorMessage = "Description zaruri hai")]
        [Display(Name = "Description")]
        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount zaruri hai")]
        [Display(Name = "Amount (₹)")]
        public decimal Amount { get; set; }

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [Display(Name = "Payment Mode")]
        public PaymentMode Mode { get; set; } = PaymentMode.Cash;

        [Display(Name = "Remarks")]
        [StringLength(500)]
        public string? Remarks { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum ExpenseCategory
    {
        [Display(Name = "Bijli (Electricity)")]
        Electricity,
        [Display(Name = "Labour / Mazdoori")]
        Labour,
        [Display(Name = "Transport / Gaadi")]
        Transport,
        [Display(Name = "Packaging")]
        Packaging,
        [Display(Name = "Equipment / Machine")]
        Equipment,
        [Display(Name = "Rent / Kiraya")]
        Rent,
        [Display(Name = "Animal Feed / Chara")]
        AnimalFeed,
        [Display(Name = "Maintenance / Repair")]
        Maintenance,
        [Display(Name = "Mobile / Internet")]
        Communication,
        [Display(Name = "Taxes / License")]
        Taxes,
        [Display(Name = "Other / Baaki")]
        Other
    }

    public enum PaymentMode
    {
        [Display(Name = "Cash")]
        Cash,
        [Display(Name = "UPI")]
        UPI,
        [Display(Name = "Bank Transfer")]
        BankTransfer,
        [Display(Name = "Cheque")]
        Cheque
    }

    // Profit & Loss ViewModel
    public class ProfitLossViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Income
        public decimal MilkSalesIncome { get; set; }
        public decimal GheeSalesIncome { get; set; }
        public decimal ProductSalesIncome { get; set; }
        public decimal OtherIncome { get; set; }
        public decimal TotalIncome => MilkSalesIncome + GheeSalesIncome + ProductSalesIncome + OtherIncome;

        // Expenses by category
        public List<CategoryExpense> ExpensesByCategory { get; set; } = new();
        public decimal TotalExpenses => ExpensesByCategory.Sum(e => e.Amount);

        // P&L
        public decimal NetProfit => TotalIncome - TotalExpenses;
        public decimal ProfitMargin => TotalIncome > 0 ? (NetProfit / TotalIncome) * 100 : 0;

        public List<Expense> RecentExpenses { get; set; } = new();
    }

    public class CategoryExpense
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    // Daily Report ViewModel
    public class DailyReportViewModel
    {
        public DateTime ReportDate { get; set; } = DateTime.Today;
        public decimal TotalMilkCollected { get; set; }
        public int TotalFarmers { get; set; }
        public decimal AvgFat { get; set; }
        public decimal AvgSNF { get; set; }
        public decimal MilkRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal OrderRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetDayProfit { get; set; }
        public int NewSubscriptions { get; set; }
        public List<MilkCollection> Collections { get; set; } = new();
        public List<Expense> Expenses { get; set; } = new();

        // For WhatsApp share
        public string GetWhatsAppText()
        {
            return $"📊 *Raj Dairy - Daily Report*\n" +
                   $"📅 Date: {ReportDate:dd MMM yyyy}\n\n" +
                   $"🥛 Milk Collected: {TotalMilkCollected:F1} L\n" +
                   $"👥 Farmers: {TotalFarmers}\n" +
                   $"📈 Avg Fat: {AvgFat:F1}% | SNF: {AvgSNF:F1}%\n\n" +
                   $"💰 Milk Revenue: ₹{MilkRevenue:N0}\n" +
                   $"📦 Orders: {TotalOrders} (₹{OrderRevenue:N0})\n" +
                   $"💸 Expenses: ₹{TotalExpenses:N0}\n\n" +
                   $"✅ *Net Profit: ₹{NetDayProfit:N0}*\n\n" +
                   $"- Raj Dairy Pro";
        }
    }
}
