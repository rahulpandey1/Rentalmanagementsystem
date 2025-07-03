using System.ComponentModel.DataAnnotations;

namespace RentalPropertyAPI.DTOs
{
    public class TenantDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PermanentAddress { get; set; } = string.Empty;
        public DateTime MoveInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }
        public decimal SecurityDeposit { get; set; }
        public bool IsActive { get; set; }
        public int RoomId { get; set; }
        public string? RoomNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTenantDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(500)]
        public string PermanentAddress { get; set; } = string.Empty;

        [Required]
        public DateTime MoveInDate { get; set; }

        [Required]
        public decimal SecurityDeposit { get; set; }

        [Required]
        public int RoomId { get; set; }
    }

    public class UpdateTenantDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(500)]
        public string PermanentAddress { get; set; } = string.Empty;

        public DateTime? MoveOutDate { get; set; }

        public decimal SecurityDeposit { get; set; }

        public int RoomId { get; set; }

        public bool IsActive { get; set; }
    }
}