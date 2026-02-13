using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentMangementsystem.Models
{
    public class RentAgreement
    {
        public int Id { get; set; }
        
        [Required]
        public int PropertyId { get; set; }
        
        [Required]
        public int TenantId { get; set; }
        
        public int? RoomId { get; set; } // Specific room assignment (optional for flexibility)
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SecurityDeposit { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        [StringLength(1000)]
        public string? Terms { get; set; }
        
        [StringLength(100)]
        public string? AgreementType { get; set; } = "Monthly"; // Monthly, Weekly, Daily
        
        // Navigation properties
        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; } = null!;
        
        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }
        
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        
        [NotMapped]
        public string DisplayInfo => Room != null 
            ? $"{Tenant?.FullName} - {Room.DisplayName}" 
            : $"{Tenant?.FullName} - Property: {Property?.PropertyName}";
    }
}