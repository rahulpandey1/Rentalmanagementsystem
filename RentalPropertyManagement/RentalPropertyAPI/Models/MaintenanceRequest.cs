using System.ComponentModel.DataAnnotations;

namespace RentalPropertyAPI.Models
{
    public class MaintenanceRequest
    {
        public int Id { get; set; }
        
        public int? RoomId { get; set; }
        public Room? Room { get; set; }
        
        public int? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        public MaintenanceType Type { get; set; }
        
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;
        
        public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
        
        public decimal? EstimatedCost { get; set; }
        
        public decimal? ActualCost { get; set; }
        
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedDate { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public bool ChargeToTenant { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum MaintenanceType
    {
        Plumbing = 0,
        Electrical = 1,
        Cleaning = 2,
        Repair = 3,
        Replacement = 4,
        Painting = 5,
        Other = 6
    }
    
    public enum MaintenanceStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 3
    }
    
    public enum MaintenancePriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Urgent = 3
    }
}