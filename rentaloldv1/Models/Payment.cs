using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentMangementsystem.Models
{
    public class Payment
    {
        public int Id { get; set; }
        
        [Required]
        public int RentAgreementId { get; set; }
        
        [Required]
        public int TenantId { get; set; }
        
        public int? BillId { get; set; } // Reference to specific bill
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        public DateTime PaymentDate { get; set; }
        
        public DateTime DueDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Check, Bank Transfer, etc.
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Overdue, Partial
        
        [StringLength(100)]
        public string? TransactionReference { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [StringLength(100)]
        public string PaymentType { get; set; } = "Rent"; // Rent, Electric, Advance, Miscellaneous
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ForeignKey("RentAgreementId")]
        public virtual RentAgreement RentAgreement { get; set; } = null!;
        
        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("BillId")]
        public virtual Bill? Bill { get; set; }
    }
}