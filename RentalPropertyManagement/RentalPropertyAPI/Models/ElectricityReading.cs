using System.ComponentModel.DataAnnotations;

namespace RentalPropertyAPI.Models
{
    public class ElectricityReading
    {
        public int Id { get; set; }
        
        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;
        
        public decimal Reading { get; set; }
        
        public DateTime ReadingDate { get; set; }
        
        public decimal? UnitsConsumed { get; set; }
        
        public decimal? BillAmount { get; set; }
        
        public decimal UnitRate { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}