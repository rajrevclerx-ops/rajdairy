using System.ComponentModel.DataAnnotations;

namespace DairyProductApp.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Partner select karein")]
        [Display(Name = "Partner")]
        public int PartnerId { get; set; }

        [Display(Name = "Partner Name")]
        public string PartnerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Transaction type select karein")]
        [Display(Name = "Type")]
        public TransactionType Type { get; set; }

        [Required(ErrorMessage = "Item select karein")]
        [Display(Name = "Item")]
        public TransactionItem Item { get; set; }

        [Display(Name = "Description")]
        [StringLength(300)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Quantity zaruri hai")]
        [Display(Name = "Quantity")]
        public decimal Quantity { get; set; }

        [Display(Name = "Unit")]
        [StringLength(20)]
        public string Unit { get; set; } = "Liter";

        [Display(Name = "Rate")]
        public decimal Rate { get; set; }

        [Display(Name = "Total Amount (₹)")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Payment Status")]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime TransactionDate { get; set; } = DateTime.Today;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Remarks")]
        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    public enum TransactionType
    {
        [Display(Name = "Received (Maal Liya)")]
        Received,
        [Display(Name = "Given (Maal Diya)")]
        Given
    }

    public enum TransactionItem
    {
        [Display(Name = "Milk (Doodh)")]
        Milk,
        [Display(Name = "Ghee")]
        Ghee,
        [Display(Name = "Paneer")]
        Paneer,
        [Display(Name = "Curd (Dahi)")]
        Curd,
        [Display(Name = "Butter (Makhan)")]
        Butter,
        [Display(Name = "Cream (Malai)")]
        Cream,
        [Display(Name = "Cash Payment")]
        Cash,
        [Display(Name = "Other")]
        Other
    }

    public enum PaymentStatus
    {
        [Display(Name = "Pending (Baaki)")]
        Pending,
        [Display(Name = "Paid (Chuka Diya)")]
        Paid,
        [Display(Name = "Partial (Kuch Diya)")]
        Partial
    }

    public class PartnerLedgerViewModel
    {
        public Partner Partner { get; set; } = new();
        public List<Transaction> Transactions { get; set; } = new();
        public decimal TotalReceived { get; set; }
        public decimal TotalGiven { get; set; }
        public decimal TotalPending { get; set; }
        public decimal Balance { get; set; }
    }
}
