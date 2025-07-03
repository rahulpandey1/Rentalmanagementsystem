using System.ComponentModel.DataAnnotations;

namespace RentalPropertyAPI.Models
{
    public class TenantDocument
    {
        public int Id { get; set; }
        
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string DocumentName { get; set; } = string.Empty;
        
        public DocumentType Type { get; set; }
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string FileExtension { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        
        [StringLength(500)]
        public string? Notes { get; set; }
    }
    
    public enum DocumentType
    {
        IdProof = 0,
        RentalAgreement = 1,
        IncomeProof = 2,
        EmergencyContact = 3,
        Other = 4
    }
}