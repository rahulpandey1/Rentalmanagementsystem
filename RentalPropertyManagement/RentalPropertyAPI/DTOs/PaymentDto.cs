using System.ComponentModel.DataAnnotations;
using RentalPropertyAPI.Models;

namespace RentalPropertyAPI.DTOs
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public PaymentType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMethod Method { get; set; }
        public string? TransactionReference { get; set; }
        public string? Description { get; set; }
        public DateTime BillingPeriodStart { get; set; }
        public DateTime BillingPeriodEnd { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePaymentDto
    {
        [Required]
        public int TenantId { get; set; }

        [Required]
        public PaymentType Type { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }

        [StringLength(100)]
        public string? TransactionReference { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime BillingPeriodStart { get; set; }

        [Required]
        public DateTime BillingPeriodEnd { get; set; }
    }

    public class PaymentSummaryDto
    {
        public decimal TotalRentCollected { get; set; }
        public decimal TotalElectricityPayments { get; set; }
        public decimal TotalSecurityDeposits { get; set; }
        public decimal TotalMaintenancePayments { get; set; }
        public decimal TotalMiscellaneousPayments { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}