using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DairyProductApp.Models
{
    public class GheeProduct
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Batch number zaruri hai")]
        [Display(Name = "Batch Number")]
        [StringLength(50)]
        public string BatchNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ghee type select karein")]
        [Display(Name = "Ghee Type")]
        public GheeType GheeType { get; set; }

        [Required(ErrorMessage = "Milk used zaruri hai")]
        [Display(Name = "Milk Used (Liters)")]
        [Range(0.1, 100000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal MilkUsedLiters { get; set; }

        [Required(ErrorMessage = "Ghee produced zaruri hai")]
        [Display(Name = "Ghee Produced (Kg)")]
        [Range(0.01, 100000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal GheeProducedKg { get; set; }

        [Display(Name = "Yield Rate (%)")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal YieldRate { get; set; }

        [Required(ErrorMessage = "Price zaruri hai")]
        [Display(Name = "Price Per Kg (₹)")]
        [Range(1, 10000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerKg { get; set; }

        [Display(Name = "Total Value (₹)")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalValue { get; set; }

        [Display(Name = "Stock Available (Kg)")]
        [Range(0, 100000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal StockKg { get; set; }

        [Required]
        [Display(Name = "Production Date")]
        [DataType(DataType.Date)]
        public DateTime ProductionDate { get; set; } = DateTime.Today;

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddMonths(12);

        [Display(Name = "Quality Grade")]
        public QualityGrade Quality { get; set; } = QualityGrade.Standard;

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public enum GheeType
    {
        [Display(Name = "Cow Ghee")]
        CowGhee,
        [Display(Name = "Buffalo Ghee")]
        BuffaloGhee,
        [Display(Name = "Mixed Ghee")]
        MixedGhee
    }

    public enum QualityGrade
    {
        [Display(Name = "Premium")]
        Premium,
        [Display(Name = "Standard")]
        Standard,
        [Display(Name = "Economy")]
        Economy
    }
}
