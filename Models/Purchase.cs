using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DOInventoryManager.Models
{
    public class Purchase
    {
        public int Id { get; set; }
        
        [Required]
        public int VesselId { get; set; }
        public virtual Vessel Vessel { get; set; } = null!;
        
        [Required]
        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; } = null!;
        
        [Required]
        public DateTime PurchaseDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string InvoiceReference { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityLiters { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityTons { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalValue { get; set; } // In supplier's currency
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalValueUSD { get; set; } // Converted to USD
        
        // Optional invoice tracking
        public DateTime? InvoiceReceiptDate { get; set; }
        public DateTime? DueDate { get; set; }
        
        // Auto-calculated fields
        [Column(TypeName = "decimal(18,6)")]
        public decimal PricePerLiter => QuantityLiters > 0 ? TotalValue / QuantityLiters : 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerTon => QuantityTons > 0 ? TotalValue / QuantityTons : 0;
        
        [Column(TypeName = "decimal(8,6)")]
        public decimal Density => QuantityLiters > 0 ? QuantityTons / (QuantityLiters / 1000) : 0;

        [Column(TypeName = "decimal(18,6)")]
        public decimal PricePerLiterUSD => QuantityLiters > 0 ? TotalValueUSD / QuantityLiters : 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerTonUSD => QuantityTons > 0 ? TotalValueUSD / QuantityTons : 0;

        [Column(TypeName = "decimal(18,3)")]
        public decimal RemainingQuantityTons => RemainingQuantity > 0 && QuantityLiters > 0 ? (RemainingQuantity / 1000) * Density : 0;

        // FIFO tracking
        [Column(TypeName = "decimal(18,3)")]
        public decimal RemainingQuantity { get; set; } // For FIFO allocation
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Payment tracking
        public DateTime? PaymentDate { get; set; }
        public decimal? PaymentAmount { get; set; } // Amount actually paid in original currency
        public decimal? PaymentAmountUSD { get; set; } // Amount paid converted to USD

        // Computed properties
        [NotMapped]
        public bool IsPaid => PaymentDate.HasValue;

        [NotMapped]
        public string PaymentStatus => IsPaid ? "Paid" : "Outstanding";

        [NotMapped]
        public string FormattedPaymentDate => PaymentDate?.ToString("dd/MM/yyyy") ?? "Not Paid";

        [NotMapped]
        public string FormattedPaymentAmount => IsPaid
            ? (Supplier?.Currency == "USD" ? PaymentAmountUSD?.ToString("C2") ?? "$0.00" : $"{PaymentAmount:N3} {Supplier?.Currency}")
            : "Outstanding";

        // Navigation properties
        public virtual ICollection<Allocation> Allocations { get; set; } = [];
    }
}
