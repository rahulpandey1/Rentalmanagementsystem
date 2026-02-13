namespace RentalBackend.Models
{
    public class Flat
    {
        public Guid FlatId { get; set; } = Guid.NewGuid();
        public required string RoomCode { get; set; }
        public int? Floor { get; set; }
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
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Occupancy> Occupancies { get; set; } = new List<Occupancy>();
        public ICollection<MonthlyLedger> MonthlyLedgers { get; set; } = new List<MonthlyLedger>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
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
}
