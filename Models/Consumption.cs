using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DOInventoryManager.Models
{
    public class Consumption
    {
        public int Id { get; set; }
        
        [Required]
        public int VesselId { get; set; }
        public virtual Vessel Vessel { get; set; } = null!;
        
        [Required]
        public DateTime ConsumptionDate { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ConsumptionLiters { get; set; }
        
        [Required]
        [StringLength(7)] // Format: YYYY-MM
        public string Month { get; set; } = string.Empty;
        
        // Trip tracking
        public int LegsCompleted { get; set; } = 0;
        
        // Calculated field
        [Column(TypeName = "decimal(18,4)")]
        public decimal ConsumptionPerLeg => LegsCompleted > 0 ? ConsumptionLiters / LegsCompleted : 0;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    }
}
