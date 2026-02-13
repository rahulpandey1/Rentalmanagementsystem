using System.ComponentModel.DataAnnotations;

namespace RentMangementsystem.Models
{
    public class SystemConfiguration
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ConfigKey { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string ConfigValue { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string Category { get; set; } = string.Empty; // Electric, General, etc.
        
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}