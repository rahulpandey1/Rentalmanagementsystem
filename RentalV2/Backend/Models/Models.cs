using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalBackend.Models
{
    public class Property
    {
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string PropertyName { get; set; } = string.Empty;
        [MaxLength(200)]
        public string? Address { get; set; }
        public int TotalFloors { get; set; }
        public int TotalRooms { get; set; }
    }

    public class Room
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        [ForeignKey("PropertyId")]
        public Property? Property { get; set; }
        [Required, MaxLength(20)]
        public string RoomNumber { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }
        public bool IsAvailable { get; set; } = true;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Area { get; set; }
        [MaxLength(50)]
        public string? ElectricMeterNumber { get; set; }
        public int? LastMeterReading { get; set; }
        public DateTime? LastReadingDate { get; set; }

        public ICollection<RentAgreement>? RentAgreements { get; set; }
        public ICollection<ElectricMeterReading>? ElectricMeterReadings { get; set; }
    }

    public class Tenant
    {
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        [MaxLength(100)]
        public string? Email { get; set; }
        [MaxLength(200)]
        public string? Address { get; set; }
        [MaxLength(50)]
        public string? IdProofType { get; set; }
        [MaxLength(50)]
        public string? IdProofNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }
        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
        public ICollection<RentAgreement>? RentAgreements { get; set; }
        public ICollection<Payment>? Payments { get; set; }
    }

    public class RentAgreement
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public int? RoomId { get; set; }
        [ForeignKey("RoomId")]
        public Room? Room { get; set; }
        public int TenantId { get; set; }
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal SecurityDeposit { get; set; }
        [MaxLength(50)]
        public string? AgreementType { get; set; }
        public string? Terms { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class ElectricMeterReading
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        [ForeignKey("RoomId")]
        public Room? Room { get; set; }
        public int PreviousReading { get; set; }
        public int CurrentReading { get; set; }
        public DateTime ReadingDate { get; set; } = DateTime.UtcNow;
        public DateTime? PreviousReadingDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ElectricCharges { get; set; }
        public bool IsBilled { get; set; }
        [MaxLength(200)]
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class Bill
    {
        public int Id { get; set; }
        public int RentAgreementId { get; set; }
        public int TenantId { get; set; }
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }
        public int? RoomId { get; set; }
        [ForeignKey("RoomId")]
        public Room? Room { get; set; }
        public int? ElectricMeterReadingId { get; set; }
        [ForeignKey("ElectricMeterReadingId")]
        public ElectricMeterReading? ElectricMeterReading { get; set; }
        [Required, MaxLength(50)]
        public string BillNumber { get; set; } = string.Empty;
        public DateTime BillDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        [MaxLength(50)]
        public string? BillPeriod { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal RentAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ElectricAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MiscAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        [NotMapped]
        public decimal OutstandingAmount => TotalAmount - PaidAmount;
        public ICollection<BillItem>? BillItems { get; set; }
    }

    public class BillItem
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        [Required, MaxLength(50)]
        public string ItemType { get; set; } = string.Empty;
        [MaxLength(200)]
        public string? Description { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        [MaxLength(200)]
        public string? Remarks { get; set; }
    }

    public class Payment
    {
        public int Id { get; set; }
        public int? RentAgreementId { get; set; }
        public int TenantId { get; set; }
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }
        public int? BillId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        [MaxLength(50)]
        public string? PaymentMethod { get; set; }
        [MaxLength(50)]
        public string? PaymentType { get; set; }
        [MaxLength(100)]
        public string? TransactionReference { get; set; }
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Completed";
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
    }
    
    public class RoomMonthlyRecord
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        public string? TenantName { get; set; } // Stores name or "VACANT"
        public bool IsVacant { get; set; }

        // Allotment Details
        public DateTime? InitialAllotmentDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal InitialRent { get; set; }
        public DateTime? CurrentAllotmentDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentRent { get; set; }

        // Security / Advance
        [Column(TypeName = "decimal(18,2)")]
        public decimal ElectricSecurity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentAdvance { get; set; }

        // Electric Meter
        public int PreviousReading { get; set; }
        public int CurrentReading { get; set; }
        public int UnitsConsumed { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal RatePerUnit { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ElectricBillAmount { get; set; }

        // Financials
        [Column(TypeName = "decimal(18,2)")]
        public decimal MiscCharges { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceBroughtForward { get; set; } // Arrears from previous month
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountDue { get; set; } // Rent + Electric + Misc + B/F
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceCarriedForward { get; set; } // Remaining Due or Advance Balance

        public DateTime? PaymentDate { get; set; }
        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class SystemConfiguration
    {
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string ConfigKey { get; set; } = string.Empty;
        public string? ConfigValue { get; set; }
        [MaxLength(200)]
        public string? Description { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
