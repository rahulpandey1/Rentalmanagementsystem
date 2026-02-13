using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Models;

namespace RentalBackend.Controllers
{
    [Authorize]
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

            var period = new DateOnly(year, month, 1);

            var records = await _context.MonthlyLedgers
                .Include(l => l.Flat)
                .Include(l => l.Tenant)
                .Where(l => l.Period == period)
                .OrderBy(l => l.SerialNumber)
                .ThenBy(l => l.Flat!.RoomCode)
                .ToListAsync();

            if (!records.Any())
            {
                // Fallback: show all flats with empty values
                var flats = await _context.Flats.OrderBy(f => f.RoomCode).ToListAsync();
                return Ok(flats.Select(flat => new
                {
                    RoomNo = flat.RoomCode,
                    Name = "VACANT",
                    IsVacant = true,
                    CurrentRent = 0m,
                    Remarks = "No data for this period"
                }));
            }

            var summary = records.Select(r => new
            {
                RoomNo = r.Flat?.RoomCode,
                Name = r.Tenant?.Name ?? "VACANT",
                IsVacant = r.TenantId == null || (r.Tenant?.Name?.Contains("VACANT", StringComparison.OrdinalIgnoreCase) ?? false) || (r.Tenant?.Name?.Contains("VACAMT", StringComparison.OrdinalIgnoreCase) ?? false),

                // Allotment
                DateOfAllotment = r.DateOfAllotment?.ToDateTime(TimeOnly.MinValue),
                CurrentAllotment = r.DateOfAllotment?.ToDateTime(TimeOnly.MinValue),

                // Rent
                InitialRent = r.MonthlyRent,
                CurrentRent = r.MonthlyRent,

                // Security
                InitialSecurity = r.ElectricSecurity,
                CurrentAdvance = 0m,

                // Electric
                MeterNew = r.ElecNew,
                MeterPrev = r.ElecPrev,
                MeterUnits = r.ElecUnits,
                ElectricCost = r.ElecCost,

                // Financials
                MiscRent = r.MiscRent,
                BalanceForward = r.Carryover,
                TotalAmountDue = r.TotalDue,
                AmountPaid = r.AmountPaid,
                CarryForward = r.ClosingBalance,

                PaymentDate = r.PaymentDate?.ToDateTime(TimeOnly.MinValue),
                Remarks = r.Remarks
            });

            return Ok(summary);
        }
    }
}
