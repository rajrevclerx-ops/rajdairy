using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DairyProductApp.Models
{
    public class DairyProduct
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Product ka naam zaruri hai")]
        [Display(Name = "Product Name")]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category select karein")]
        [Display(Name = "Category")]
        public ProductCategory Category { get; set; }

        [Required(ErrorMessage = "Quantity zaruri hai")]
        [Display(Name = "Quantity")]
        [Range(0.01, 100000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Unit select karein")]
        [Display(Name = "Unit")]
        public ProductUnit Unit { get; set; }

        [Required(ErrorMessage = "Price zaruri hai")]
        [Display(Name = "Price (₹)")]
        [Range(0.01, 100000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Display(Name = "Manufacturing Date")]
        [DataType(DataType.Date)]
        public DateTime ManufacturingDate { get; set; } = DateTime.Today;

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Stock Available")]
        [Range(0, 100000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal StockQuantity { get; set; }

        [Display(Name = "Description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum ProductCategory
    {
        [Display(Name = "Milk")]
        Milk,
        [Display(Name = "Curd / Dahi")]
        Curd,
        [Display(Name = "Paneer")]
        Paneer,
        [Display(Name = "Butter / Makhan")]
        Butter,
        [Display(Name = "Ghee")]
        Ghee,
        [Display(Name = "Cream / Malai")]
        Cream,
        [Display(Name = "Buttermilk / Chaach")]
        Buttermilk,
        [Display(Name = "Khoya / Mawa")]
        Khoya,
        [Display(Name = "Cheese")]
        Cheese,
        [Display(Name = "Lassi")]
        Lassi,
        [Display(Name = "Ice Cream")]
        IceCream,
        [Display(Name = "Other")]
        Other
    }

    public enum ProductUnit
    {
        [Display(Name = "Liter")]
        Liter,
        [Display(Name = "Kilogram")]
        Kg,
        [Display(Name = "Gram")]
        Gram,
        [Display(Name = "Piece")]
        Piece,
        [Display(Name = "Packet")]
        Packet
    }
}
