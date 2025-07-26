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
        [StringLength(7)] // Format: YYYY-MM
        public string Month { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
