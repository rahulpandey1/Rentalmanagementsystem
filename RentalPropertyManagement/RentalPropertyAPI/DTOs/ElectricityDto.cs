using System.ComponentModel.DataAnnotations;

namespace RentalPropertyAPI.DTOs
{
    public class ElectricityReadingDto
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public decimal Reading { get; set; }
        public DateTime ReadingDate { get; set; }
        public decimal? UnitsConsumed { get; set; }
        public decimal? BillAmount { get; set; }
        public decimal UnitRate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateElectricityReadingDto
    {
        [Required]
        public int RoomId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Reading must be a positive number")]
        public decimal Reading { get; set; }

        [Required]
        public DateTime ReadingDate { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit rate must be greater than 0")]
        public decimal UnitRate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class ElectricityBillDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string? TenantName { get; set; }
        public decimal PreviousReading { get; set; }
        public decimal CurrentReading { get; set; }
        public decimal UnitsConsumed { get; set; }
        public decimal UnitRate { get; set; }
        public decimal BillAmount { get; set; }
        public DateTime BillingPeriodStart { get; set; }
        public DateTime BillingPeriodEnd { get; set; }
    }
}