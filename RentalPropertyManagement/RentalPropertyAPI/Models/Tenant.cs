using System.ComponentModel.DataAnnotations;

namespace RentalPropertyAPI.Models
{
    public class Tenant
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string PermanentAddress { get; set; } = string.Empty;
        
        public DateTime MoveInDate { get; set; }
        
        public DateTime? MoveOutDate { get; set; }
        
        public decimal SecurityDeposit { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;
        
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<TenantDocument> Documents { get; set; } = new List<TenantDocument>();
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}