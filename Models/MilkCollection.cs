using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DairyProductApp.Models
{
    public class MilkCollection
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Farmer ka naam zaruri hai")]
        [Display(Name = "Farmer Name")]
        [StringLength(100)]
        public string FarmerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number zaruri hai")]
        [Display(Name = "Mobile Number")]
        [Phone]
        [StringLength(15)]
        public string MobileNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Milk type select karein")]
        [Display(Name = "Milk Type")]
        public MilkType MilkType { get; set; }

        [Required(ErrorMessage = "Quantity zaruri hai")]
        [Display(Name = "Quantity (Liters)")]
        [Range(0.1, 10000, ErrorMessage = "Quantity 0.1 se 10000 liters ke beech honi chahiye")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Fat % zaruri hai")]
        [Display(Name = "Fat (%)")]
        [Range(0.1, 15.0, ErrorMessage = "Fat 0.1% se 15% ke beech hona chahiye")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal FatPercentage { get; set; }

        [Required(ErrorMessage = "SNF % zaruri hai")]
        [Display(Name = "SNF (%)")]
        [Range(5.0, 15.0, ErrorMessage = "SNF 5% se 15% ke beech hona chahiye")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal SNFPercentage { get; set; }

        [Display(Name = "Rate Per Liter (₹)")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal RatePerLiter { get; set; }

        [Display(Name = "Total Amount (₹)")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Display(Name = "Shift")]
        public Shift Shift { get; set; }

        [Required]
        [Display(Name = "Collection Date")]
        [DataType(DataType.Date)]
        public DateTime CollectionDate { get; set; } = DateTime.Today;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Remarks")]
        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    public enum MilkType
    {
        [Display(Name = "Cow Milk")]
        Cow,
        [Display(Name = "Buffalo Milk")]
        Buffalo,
        [Display(Name = "Mixed Milk")]
        Mixed
    }

    public enum Shift
    {
        [Display(Name = "Morning")]
        Morning,
        [Display(Name = "Evening")]
        Evening
    }
}
