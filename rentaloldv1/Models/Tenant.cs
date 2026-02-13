using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentMangementsystem.Models
{
    public class Tenant
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;
        
        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        [StringLength(300)]
        public string? Address { get; set; }
        
        public DateTime DateOfBirth { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual ICollection<RentAgreement> RentAgreements { get; set; } = new List<RentAgreement>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}