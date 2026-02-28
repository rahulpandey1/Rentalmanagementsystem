namespace RentalBackend.Models
{
    public class Flat
    {
        public Guid FlatId { get; set; } = Guid.NewGuid();
        public required string RoomCode { get; set; }
        public int? Floor { get; set; }
        public string? MeterId { get; set; }
        public decimal BaseRent { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Occupancy> Occupancies { get; set; } = new List<Occupancy>();
        public ICollection<MonthlyLedger> MonthlyLedgers { get; set; } = new List<MonthlyLedger>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class Tenant
    {
        public Guid TenantId { get; set; } = Guid.NewGuid();
        public required string Name { get; set; }
        public string? FatherName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? AadhaarNumber { get; set; }
        public string? PanNumber { get; set; }
        public string? PermanentAddress { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? TentativeRoomCode { get; set; }
        public decimal TentativeRent { get; set; }
        public decimal SecurityDeposit { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Occupancy> Occupancies { get; set; } = new List<Occupancy>();
        public ICollection<MonthlyLedger> MonthlyLedgers { get; set; } = new List<MonthlyLedger>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<TenantDocument> Documents { get; set; } = new List<TenantDocument>();
        public ICollection<SecurityDepositTransaction> DepositTransactions { get; set; } = new List<SecurityDepositTransaction>();
    }

    public class TenantDocument
    {
        public Guid DocumentId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string DocumentType { get; set; } = "Other"; // Aadhaar, PAN, Other
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public Tenant? Tenant { get; set; }
    }

    public class SecurityDepositTransaction
    {
        public Guid TransactionId { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Collection"; // Collection, TopUp, Adjustment, Refund
        public string? Description { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public Tenant? Tenant { get; set; }
    }

    public class Occupancy
    {
        public Guid OccupancyId { get; set; } = Guid.NewGuid();
        public Guid FlatId { get; set; }
        public Guid? TenantId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        // Navigation
        public Flat? Flat { get; set; }
        public Tenant? Tenant { get; set; }
    }

    public class MonthlyLedger
    {
        public Guid MonthlyLedgerId { get; set; } = Guid.NewGuid();
        public string? InvoiceNumber { get; set; }
        public DateOnly Period { get; set; }
        public Guid FlatId { get; set; }
        public Guid? TenantId { get; set; }
        public int? SerialNumber { get; set; }
        public DateOnly? DateOfAllotment { get; set; }

        // Financial fields
        public decimal ElectricSecurity { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal ElecNew { get; set; }
        public decimal ElecPrev { get; set; }
        public decimal ElecRate { get; set; }
        public decimal ElecUnits { get; set; }
        public decimal ElecCost { get; set; }
        public decimal MiscRent { get; set; }
        public string? MiscChargeName { get; set; }
        public decimal Carryover { get; set; }
        public decimal TotalDue { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ClosingBalance { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string? Remarks { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public Flat? Flat { get; set; }
        public Tenant? Tenant { get; set; }
    }

    public class Payment
    {
        public Guid PaymentId { get; set; } = Guid.NewGuid();
        public DateOnly Period { get; set; }
        public Guid FlatId { get; set; }
        public Guid? TenantId { get; set; }
        public decimal Amount { get; set; }
        public DateOnly? PaymentDate { get; set; }
        public string Source { get; set; } = "ExcelImport";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public Flat? Flat { get; set; }
        public Tenant? Tenant { get; set; }
    }

    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string Action { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? BrowserInfo { get; set; }
        public string? RequestUrl { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    }
}
