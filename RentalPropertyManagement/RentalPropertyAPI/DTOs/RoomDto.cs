using System.ComponentModel.DataAnnotations;
using RentalPropertyAPI.Models;

namespace RentalPropertyAPI.DTOs
{
    public class RoomDto
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
        public RoomStatus Status { get; set; }
        public string ElectricMeterNumber { get; set; } = string.Empty;
        public int TenantCount { get; set; }
        public string? CurrentTenantName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateRoomDto
    {
        [Required]
        [StringLength(10)]
        public string RoomNumber { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Monthly rent must be greater than 0")]
        public decimal MonthlyRent { get; set; }

        [StringLength(20)]
        public string ElectricMeterNumber { get; set; } = string.Empty;
    }

    public class UpdateRoomDto
    {
        [Required]
        [StringLength(10)]
        public string RoomNumber { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Monthly rent must be greater than 0")]
        public decimal MonthlyRent { get; set; }

        public RoomStatus Status { get; set; }

        [StringLength(20)]
        public string ElectricMeterNumber { get; set; } = string.Empty;
    }
}