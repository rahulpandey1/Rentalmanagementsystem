using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentMangementsystem.Models
{
    public class BillItem
    {
        public int Id { get; set; }
        
        [Required]
        public int BillId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ItemType { get; set; } = string.Empty; // Rent, Electric, Miscellaneous
        
        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        public int? Quantity { get; set; } = 1;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }
        
        [StringLength(500)]
        public string? Remarks { get; set; }
        
        // Navigation properties
        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; } = null!;
    }
}