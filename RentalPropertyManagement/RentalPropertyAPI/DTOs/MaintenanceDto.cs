using System.ComponentModel.DataAnnotations;
using RentalPropertyAPI.Models;

namespace RentalPropertyAPI.DTOs
{
    public class MaintenanceRequestDto
    {
        public int Id { get; set; }
        public int? RoomId { get; set; }
        public string? RoomNumber { get; set; }
        public int? TenantId { get; set; }
        public string? TenantName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MaintenanceType Type { get; set; }
        public MaintenanceStatus Status { get; set; }
        public MaintenancePriority Priority { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? ActualCost { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? Notes { get; set; }
        public bool ChargeToTenant { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateMaintenanceRequestDto
    {
        public int? RoomId { get; set; }

        public int? TenantId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public MaintenanceType Type { get; set; }

        public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;

        [Range(0, double.MaxValue, ErrorMessage = "Estimated cost must be a positive number")]
        public decimal? EstimatedCost { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool ChargeToTenant { get; set; } = false;
    }

    public class UpdateMaintenanceRequestDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public MaintenanceType Type { get; set; }

        public MaintenanceStatus Status { get; set; }

        public MaintenancePriority Priority { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Estimated cost must be a positive number")]
        public decimal? EstimatedCost { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Actual cost must be a positive number")]
        public decimal? ActualCost { get; set; }

        public DateTime? CompletedDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool ChargeToTenant { get; set; }
    }
}