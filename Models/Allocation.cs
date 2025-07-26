using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DOInventoryManager.Models
{
    public class Allocation
    {
        public int Id { get; set; }
        
        [Required]
        public int PurchaseId { get; set; }
        public virtual Purchase Purchase { get; set; } = null!;
        
        [Required]
        public int ConsumptionId { get; set; }
        public virtual Consumption Consumption { get; set; } = null!;
        
        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal AllocatedQuantity { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AllocatedValue { get; set; } // In original purchase currency
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AllocatedValueUSD { get; set; } // Converted to USD

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal PurchaseBalanceAfter { get; set; } // Remaining quantity in purchase after this allocation

        [Column(TypeName = "decimal(18,3)")]
        public decimal AllocatedQuantityTons => Purchase != null && Purchase.QuantityLiters > 0 ?
            (AllocatedQuantity / 1000) * Purchase.Density : 0;

        [Column(TypeName = "decimal(18,3)")]
        public decimal PurchaseBalanceAfterTons => Purchase != null && Purchase.QuantityLiters > 0 ?
            (PurchaseBalanceAfter / 1000) * Purchase.Density : 0;

        [Required]
        [StringLength(7)] // Format: YYYY-MM
        public string Month { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
