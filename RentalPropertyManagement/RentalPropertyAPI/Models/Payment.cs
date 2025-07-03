using System.ComponentModel.DataAnnotations;

namespace RentalPropertyAPI.Models
{
    public class Payment
    {
        public int Id { get; set; }
        
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;
        
        public PaymentType Type { get; set; }
        
        public decimal Amount { get; set; }
        
        public DateTime PaymentDate { get; set; }
        
        public PaymentMethod Method { get; set; }
        
        [StringLength(100)]
        public string? TransactionReference { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public DateTime BillingPeriodStart { get; set; }
        public DateTime BillingPeriodEnd { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public enum PaymentType
    {
        Rent = 0,
        SecurityDeposit = 1,
        Electricity = 2,
        Water = 3,
        Maintenance = 4,
        Miscellaneous = 5
    }
    
    public enum PaymentMethod
    {
        Cash = 0,
        BankTransfer = 1,
        OnlinePayment = 2,
        Check = 3
    }
}