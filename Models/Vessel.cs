using System.ComponentModel.DataAnnotations;

namespace DOInventoryManager.Models
{
    public class Vessel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(10)]
        public string Type { get; set; } = string.Empty; // "Vessel" or "Boat"
        
        // Computed property for route
        public string Route => Type == "Vessel" ? "Aqaba-Nuweibaa-Aqaba" : "Aqaba-Taba-Aqaba";
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual ICollection<Purchase> Purchases { get; set; } = [];
        public virtual ICollection<Consumption> Consumptions { get; set; } = [];
    }
}
