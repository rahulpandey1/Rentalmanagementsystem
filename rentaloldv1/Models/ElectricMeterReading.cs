using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentMangementsystem.Models
{
    public class ElectricMeterReading
    {
        public int Id { get; set; }
        
        [Required]
        public int RoomId { get; set; }
        
        public int PreviousReading { get; set; }
        
        public int CurrentReading { get; set; }
        
        public DateTime ReadingDate { get; set; }
        
        public DateTime PreviousReadingDate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitsConsumed => CurrentReading - PreviousReading;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ElectricCharges { get; set; }
        
        [StringLength(500)]
        public string? Remarks { get; set; }
        
        public bool IsBilled { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ForeignKey("RoomId")]
        public virtual Room Room { get; set; } = null!;
        
        public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }
}