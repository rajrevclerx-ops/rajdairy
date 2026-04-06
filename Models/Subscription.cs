using System.ComponentModel.DataAnnotations;

namespace DairyProductApp.Models
{
    public class Subscription
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Partner select karein")]
        [Display(Name = "Partner")]
        public int PartnerId { get; set; }

        [Display(Name = "Partner Name")]
        public string PartnerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product select karein")]
        [Display(Name = "Product")]
        public SubscriptionProduct Product { get; set; }

        [Required(ErrorMessage = "Quantity zaruri hai")]
        [Display(Name = "Daily Quantity")]
        [Range(0.1, 1000)]
        public decimal DailyQuantity { get; set; }

        [Display(Name = "Unit")]
        public string Unit { get; set; } = "Liter";

        [Required(ErrorMessage = "Rate zaruri hai")]
        [Display(Name = "Rate Per Unit (₹)")]
        public decimal RatePerUnit { get; set; }

        [Required(ErrorMessage = "Start date zaruri hai")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Delivery Time")]
        public DeliverySlot DeliverySlot { get; set; } = DeliverySlot.Morning;

        [Display(Name = "Delivery Address")]
        [StringLength(300)]
        public string? DeliveryAddress { get; set; }

        [Display(Name = "Status")]
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        [Display(Name = "Frequency")]
        public DeliveryFrequency Frequency { get; set; } = DeliveryFrequency.Daily;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string? Notes { get; set; }

        // Calculated
        public decimal MonthlyEstimate => DailyQuantity * RatePerUnit * (Frequency == DeliveryFrequency.Daily ? 30 : Frequency == DeliveryFrequency.AlternateDays ? 15 : 4);
    }

    public enum SubscriptionProduct
    {
        [Display(Name = "Cow Milk (Gaye ka Doodh)")]
        CowMilk,
        [Display(Name = "Buffalo Milk (Bhains ka Doodh)")]
        BuffaloMilk,
        [Display(Name = "Mixed Milk")]
        MixedMilk,
        [Display(Name = "Curd (Dahi)")]
        Curd,
        [Display(Name = "Paneer")]
        Paneer,
        [Display(Name = "Ghee (Desi Ghee)")]
        Ghee,
        [Display(Name = "Butter (Makhan)")]
        Butter,
        [Display(Name = "Buttermilk (Chaach)")]
        Buttermilk,
        [Display(Name = "Cream (Malai)")]
        Cream
    }

    public enum DeliverySlot
    {
        [Display(Name = "Morning (5 AM - 8 AM)")]
        Morning,
        [Display(Name = "Evening (4 PM - 7 PM)")]
        Evening,
        [Display(Name = "Both (Morning + Evening)")]
        Both
    }

    public enum SubscriptionStatus
    {
        [Display(Name = "Active (Chalu)")]
        Active,
        [Display(Name = "Paused (Ruka Hua)")]
        Paused,
        [Display(Name = "Cancelled (Band)")]
        Cancelled,
        [Display(Name = "Expired (Khatam)")]
        Expired
    }

    public enum DeliveryFrequency
    {
        [Display(Name = "Daily (Roz)")]
        Daily,
        [Display(Name = "Alternate Days (Ek Din Chhod Ke)")]
        AlternateDays,
        [Display(Name = "Weekly (Hafta)")]
        Weekly
    }
}
