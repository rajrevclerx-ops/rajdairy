using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DairyProductApp.Models
{
    public class MilkRate
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Milk type select karein")]
        [Display(Name = "Milk Type")]
        public MilkType MilkType { get; set; }

        [Required(ErrorMessage = "Minimum Fat % zaruri hai")]
        [Display(Name = "Min Fat (%)")]
        [Range(0.1, 15.0)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal MinFat { get; set; }

        [Required(ErrorMessage = "Maximum Fat % zaruri hai")]
        [Display(Name = "Max Fat (%)")]
        [Range(0.1, 15.0)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal MaxFat { get; set; }

        [Required(ErrorMessage = "Minimum SNF % zaruri hai")]
        [Display(Name = "Min SNF (%)")]
        [Range(5.0, 15.0)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal MinSNF { get; set; }

        [Required(ErrorMessage = "Maximum SNF % zaruri hai")]
        [Display(Name = "Max SNF (%)")]
        [Range(5.0, 15.0)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal MaxSNF { get; set; }

        [Required(ErrorMessage = "Rate zaruri hai")]
        [Display(Name = "Rate Per Liter (₹)")]
        [Range(1, 500)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal RatePerLiter { get; set; }

        [Display(Name = "Effective From")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; } = DateTime.Today;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
