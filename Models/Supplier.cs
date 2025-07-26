using System.ComponentModel.DataAnnotations;

namespace DOInventoryManager.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "USD"; // USD, JOD, EGP
        
        public decimal ExchangeRate { get; set; } = 1.0m; // Rate to USD
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation property
        public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}