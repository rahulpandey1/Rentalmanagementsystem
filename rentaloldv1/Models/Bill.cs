using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentMangementsystem.Models
{
    public class Bill
    {
        public int Id { get; set; }
        
        [Required]
        public int RentAgreementId { get; set; }
        
        [Required]
        public int TenantId { get; set; }
        
        public int? RoomId { get; set; }
        
        public int? ElectricMeterReadingId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string BillNumber { get; set; } = string.Empty;
        
        public DateTime BillDate { get; set; } = DateTime.Now;
        
        public DateTime DueDate { get; set; }
        
        [StringLength(50)]
        public string BillPeriod { get; set; } = string.Empty; // e.g., "Jan 2024", "Q1 2024"
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal RentAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ElectricAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MiscAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingAmount => TotalAmount - PaidAmount;
        
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Partial, Overdue
        
        [StringLength(1000)]
        public string? Remarks { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ForeignKey("RentAgreementId")]
        public virtual RentAgreement RentAgreement { get; set; } = null!;
        
        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }
        
        [ForeignKey("ElectricMeterReadingId")]
        public virtual ElectricMeterReading? ElectricMeterReading { get; set; }
        
        public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}