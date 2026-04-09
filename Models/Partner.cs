using System.ComponentModel.DataAnnotations;

namespace DairyProductApp.Models
{
    public class Partner
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Partner ka naam zaruri hai")]
        [Display(Name = "Partner Name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number zaruri hai")]
        [Display(Name = "Mobile Number")]
        [Phone]
        [StringLength(15)]
        public string Mobile { get; set; } = string.Empty;

        [Display(Name = "Address")]
        [StringLength(300)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Partner type select karein")]
        [Display(Name = "Partner Type")]
        public PartnerType Type { get; set; }

        [Display(Name = "Access Code")]
        [StringLength(10)]
        public string AccessCode { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public enum PartnerType
    {
        [Display(Name = "Milk Supplier (Doodh Dene Wala)")]
        Supplier,
        [Display(Name = "Milk Buyer (Doodh Lene Wala)")]
        Buyer,
        [Display(Name = "Both (Dono)")]
        Both
    }
}
