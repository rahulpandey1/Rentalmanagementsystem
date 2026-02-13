using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;

namespace RentalBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BillsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public BillsController(RentManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get ledger entries for a specific month/year period
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetBills(int? month, int? year)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var period = new DateOnly(targetYear, targetMonth, 1);

            var ledgers = await _context.MonthlyLedgers
                .Include(l => l.Flat)
                .Include(l => l.Tenant)
                .Where(l => l.Period == period)
                .OrderBy(l => l.SerialNumber)
                .ThenBy(l => l.Flat!.RoomCode)
                .ToListAsync();

            // If no data for selected period, show all flats as vacant
            if (!ledgers.Any())
            {
                var flats = await _context.Flats.OrderBy(f => f.RoomCode).ToListAsync();
                return Ok(flats.Select(f => new
                {
                    Id = f.FlatId,
                    TenantName = "VACANT",
                    RoomNumber = f.RoomCode,
                    BillPeriod = period.ToString("MMMM yyyy"),
                    TotalAmount = 0m,
                    PaidAmount = 0m,
                    ClosingBalance = 0m,
                    Status = "No Data"
                }));
            }

            var result = ledgers.Select(l => new
            {
                Id = l.MonthlyLedgerId,
                TenantName = l.Tenant?.Name ?? "VACANT",
                RoomNumber = l.Flat?.RoomCode,
                BillPeriod = l.Period.ToString("MMMM yyyy"),
                TotalAmount = l.TotalDue,
                PaidAmount = l.AmountPaid,
                ClosingBalance = l.ClosingBalance,
                Status = l.ClosingBalance <= 0 ? "Paid" : "Pending"
            });

            return Ok(result);
        }

        /// <summary>
        /// Get outstanding ledger entries for a specific month/year
        /// </summary>
        [HttpGet("outstanding")]
        public async Task<ActionResult<IEnumerable<object>>> GetOutstanding(int? month, int? year)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var period = new DateOnly(targetYear, targetMonth, 1);

            var ledgers = await _context.MonthlyLedgers
                .Include(l => l.Flat)
                .Include(l => l.Tenant)
                .Where(l => l.Period == period && l.ClosingBalance > 0)
                .OrderByDescending(l => l.ClosingBalance)
                .ToListAsync();

            var result = ledgers.Select(l => new
            {
                Id = l.MonthlyLedgerId,
                TenantName = l.Tenant?.Name ?? "VACANT",
                RoomNumber = l.Flat?.RoomCode,
                BillPeriod = l.Period.ToString("MMMM yyyy"),
                TotalAmount = l.TotalDue,
                PaidAmount = l.AmountPaid,
                ClosingBalance = l.ClosingBalance,
                Status = "Pending"
            });

            return Ok(result);
        }
    }
}
