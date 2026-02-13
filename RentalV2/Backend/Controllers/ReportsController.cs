using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Models;
using System.Globalization;

namespace RentalBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public ReportsController(RentManagementContext context)
        {
            _context = context;
        }

        [HttpGet("monthly-summary")]
        public async Task<ActionResult<IEnumerable<object>>> GetMonthlySummary(int month, int year)
        {
            if (month < 1 || month > 12 || year < 2000 || year > 2100)
            {
                return BadRequest("Invalid month or year.");
            }

            // Fetch records from the dedicated monthly table (populated via Import)
            var records = await _context.RoomMonthlyRecords
                .Include(r => r.Room)
                .Where(r => r.Month == month && r.Year == year)
                .OrderBy(r => r.RoomId)
                .ToListAsync();

            // If no records found for this month, we might want to return empty or 
            // maybe generate a skeleton if it's the current month and not yet imported?
            // For now, let's return what we have. If empty, the frontend shows empty.
            // But to be helpful, if empty, we could fallback to 'All Rooms' with empty values.
            
            if (!records.Any())
            {
                 // Fallback: Get all rooms and show as empty/vacant or current state
                 // This ensures the table isn't just blank if no import happened yet for this month.
                 var rooms = await _context.Rooms.OrderBy(r => r.Id).ToListAsync();
                 return Ok(rooms.Select(room => new
                 {
                     RoomNo = room.RoomNumber,
                     Name = "VACANT", // Default to vacant if no record
                     IsVacant = true,
                     CurrentRent = room.MonthlyRent,
                     Remarks = "No data imported"
                 }));
            }

            var summary = records.Select(r => new
            {
                RoomNo = r.Room.RoomNumber,
                Name = r.TenantName,
                IsVacant = r.IsVacant,
                
                // Allotment
                DateOfAllotment = r.InitialAllotmentDate,
                CurrentAllotment = r.CurrentAllotmentDate,
                
                // Rent
                InitialRent = r.InitialRent,
                CurrentRent = r.CurrentRent,
                
                // Security
                InitialSecurity = r.ElectricSecurity, // Mapped to 'ELECTRIC SECURITY' column
                CurrentAdvance = r.CurrentAdvance,
                
                // Electric
                MeterNew = r.CurrentReading,
                MeterPrev = r.PreviousReading,
                MeterUnits = r.UnitsConsumed,
                ElectricCost = r.ElectricBillAmount,
                
                // Financials
                MiscRent = r.MiscCharges,
                BalanceForward = r.BalanceBroughtForward,
                TotalAmountDue = r.TotalAmountDue,
                AmountPaid = r.AmountPaid,
                CarryForward = r.BalanceCarriedForward,
                
                PaymentDate = r.PaymentDate,
                Remarks = r.Remarks
            });

            return Ok(summary);
        }
    }
}
