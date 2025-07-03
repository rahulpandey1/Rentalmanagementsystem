using System.ComponentModel.DataAnnotations;

namespace RentalPropertyAPI.Models
{
    public class Room
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(10)]
        public string RoomNumber { get; set; } = string.Empty;
        
        public decimal MonthlyRent { get; set; }
        
        public RoomStatus Status { get; set; } = RoomStatus.Available;
        
        [StringLength(20)]
        public string ElectricMeterNumber { get; set; } = string.Empty;
        
        // Navigation properties
        public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
        public ICollection<ElectricityReading> ElectricityReadings { get; set; } = new List<ElectricityReading>();
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum RoomStatus
    {
        Available = 0,
        Occupied = 1,
        Maintenance = 2
    }
}