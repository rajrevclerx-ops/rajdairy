using System.ComponentModel.DataAnnotations;

namespace DairyProductApp.Models
{
    public class AdminUser
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username zaruri hai")]
        [Display(Name = "Username")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password zaruri hai")]
        [Display(Name = "Password")]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name zaruri hai")]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Mobile")]
        [StringLength(15)]
        public string? Mobile { get; set; }

        [Display(Name = "Dairy Name")]
        [StringLength(100)]
        public string DairyName { get; set; } = "Raj Dairy";

        [Required]
        [Display(Name = "Role")]
        public AdminRole Role { get; set; } = AdminRole.Admin;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Last Login")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum AdminRole
    {
        [Display(Name = "Super Admin (Full Control)")]
        SuperAdmin,
        [Display(Name = "Admin (Manage Dairy)")]
        Admin,
        [Display(Name = "Operator (Collection Only)")]
        Operator
    }

    public class ActivityLog
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
