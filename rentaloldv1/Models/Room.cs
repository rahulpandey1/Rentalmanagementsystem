using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentMangementsystem.Models
{
    public class Room
    {
        public int Id { get; set; }
        
        [Required]
        public int PropertyId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string RoomNumber { get; set; } = string.Empty; // e.g., "G/1", "1/1", "2/1"
        
        [Required]
        [StringLength(50)]
        public string Floor { get; set; } = string.Empty; // Ground, First, Second
        
        public int FloorNumber { get; set; } // 0 for Ground, 1 for First, 2 for Second
        
        public decimal Area { get; set; } // Square feet/meters of the room
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }
        
        public bool IsAvailable { get; set; } = true;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(100)]
        public string? RoomType { get; set; } // Single, Double, Suite, etc.
        
        public bool HasPrivateBathroom { get; set; } = false;
        
        public bool HasAC { get; set; } = false;
        
        public bool IsFurnished { get; set; } = false;
        
        // Electric meter information
        [StringLength(100)]
        public string? ElectricMeterNumber { get; set; }
        
        public int? LastMeterReading { get; set; }
        
        public DateTime? LastReadingDate { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; } = null!;
        
        public virtual ICollection<RentAgreement> RentAgreements { get; set; } = new List<RentAgreement>();
        public virtual ICollection<ElectricMeterReading> ElectricMeterReadings { get; set; } = new List<ElectricMeterReading>();
        public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
        
        [NotMapped]
        public string DisplayName => $"Room {RoomNumber} ({Floor} Floor)";
    }
}