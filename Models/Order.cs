using System.ComponentModel.DataAnnotations;

namespace DairyProductApp.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Display(Name = "Order Number")]
        public string OrderNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Partner select karein")]
        [Display(Name = "Partner")]
        public int PartnerId { get; set; }

        [Display(Name = "Partner Name")]
        public string PartnerName { get; set; } = string.Empty;

        [Display(Name = "Partner Mobile")]
        public string PartnerMobile { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product select karein")]
        [Display(Name = "Product")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity zaruri hai")]
        [Display(Name = "Quantity")]
        [Range(0.1, 10000)]
        public decimal Quantity { get; set; }

        [Display(Name = "Unit")]
        public string Unit { get; set; } = "Liter";

        [Display(Name = "Rate (₹)")]
        public decimal Rate { get; set; }

        [Display(Name = "Total Amount (₹)")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Display(Name = "Delivery Date")]
        [DataType(DataType.Date)]
        public DateTime DeliveryDate { get; set; } = DateTime.Today;

        [Display(Name = "Delivery Slot")]
        public DeliverySlot DeliverySlot { get; set; } = DeliverySlot.Morning;

        [Display(Name = "Status")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Display(Name = "Payment Status")]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [Display(Name = "Delivery Address")]
        [StringLength(300)]
        public string? DeliveryAddress { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public enum OrderStatus
    {
        [Display(Name = "Pending (Baaki)")]
        Pending,
        [Display(Name = "Confirmed (Pakka)")]
        Confirmed,
        [Display(Name = "Out for Delivery (Delivery Mein)")]
        OutForDelivery,
        [Display(Name = "Delivered (Pohoch Gaya)")]
        Delivered,
        [Display(Name = "Cancelled (Radd)")]
        Cancelled
    }
}
