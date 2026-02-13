using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentMangementsystem.Models
{
    public class Property
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string PropertyType { get; set; } = string.Empty; // Boarding House, Rental Property, etc.
        
        [StringLength(100)]
        public string PropertyName { get; set; } = string.Empty; // Name of your property
        
        public int TotalRooms { get; set; } // Total number of rooms (22 in your case)
        
        public int NumberOfFloors { get; set; } // Number of floors (3 in your case)
        
        public decimal TotalArea { get; set; } // Total area of the property
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public bool HasSharedKitchen { get; set; } = false;
        
        public bool HasSharedBathrooms { get; set; } = false;
        
        public bool HasParking { get; set; } = false;
        
        public bool HasLaundryFacility { get; set; } = false;
        
        public bool HasWiFi { get; set; } = false;
        
        // Navigation properties
        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
        public virtual ICollection<RentAgreement> RentAgreements { get; set; } = new List<RentAgreement>();
        
        [NotMapped]
        public int AvailableRooms => Rooms?.Count(r => r.IsAvailable) ?? 0;
        
        [NotMapped]
        public int OccupiedRooms => Rooms?.Count(r => !r.IsAvailable) ?? 0;
    }
}