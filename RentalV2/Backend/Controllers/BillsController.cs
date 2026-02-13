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
                Status = l.ClosingBalance <= 0 ? "Paid" : "Pending",
                
                // Details for Invoice
                MonthlyRent = l.MonthlyRent,
                ElectricAmount = l.ElecCost, 
                MiscAmount = l.MiscRent,
                Carryover = l.Carryover,
                Remarks = l.Remarks,
                
                // Meter Readings
                ElecPrev = l.ElecPrev,
                ElecNew = l.ElecNew,
                ElecUnits = l.ElecUnits,
                ElecRate = l.ElecRate
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
                Status = "Pending",

                // Details for Invoice
                MonthlyRent = l.MonthlyRent,
                ElectricAmount = l.ElecCost,
                MiscAmount = l.MiscRent,
                Carryover = l.Carryover,
                Remarks = l.Remarks
            });

            return Ok(result);
        }

        /// <summary>
        /// Generate monthly bills for all active tenants
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateBills([FromQuery] int? month, [FromQuery] int? year)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var period = new DateOnly(targetYear, targetMonth, 1);
            var prevPeriod = period.AddMonths(-1);

            // 1. Get all active occupancies for this period
            // Active means: StartDate <= Period AND (EndDate is NULL OR EndDate >= Period)
            var activeOccupancies = await _context.Occupancies
                .Include(o => o.Flat)
                .Include(o => o.Tenant)
                .Where(o => o.StartDate <= period && (o.EndDate == null || o.EndDate >= period))
                .ToListAsync();

            int generatedCount = 0;

            foreach (var occupancy in activeOccupancies)
            {
                // 2. Check if ledger already handles this period
                var existing = await _context.MonthlyLedgers
                    .FirstOrDefaultAsync(l => l.Period == period && l.FlatId == occupancy.FlatId);

                if (existing != null) continue; // Skip if already exists

                // 3. Get previous month's ledger to copy data
                var prevLedger = await _context.MonthlyLedgers
                    .FirstOrDefaultAsync(l => l.Period == prevPeriod && l.FlatId == occupancy.FlatId);

                // 4. Create new ledger
                var newLedger = new MonthlyLedger
                {
                    Period = period,
                    FlatId = occupancy.FlatId,
                    TenantId = occupancy.TenantId,
                    DateOfAllotment = occupancy.StartDate,
                    
                    // Copy from previous or defaults
                    MonthlyRent = prevLedger?.MonthlyRent ?? 0, // TODO: How to get rent for new tenant?
                    ElectricSecurity = prevLedger?.ElectricSecurity ?? 0,
                    ElecPrev = prevLedger?.ElecNew ?? 0,
                    ElecNew = prevLedger?.ElecNew ?? 0, // Default to same as prev
                    ElecRate = prevLedger?.ElecRate ?? 8.0m, // Default rate
                    MiscRent = 0, // Reset misc charges
                    
                    // Carryover is previous closing balance
                    Carryover = prevLedger?.ClosingBalance ?? 0,
                };

                // Calculate totals
                // Total Due = Arrears + Rent + Misc + (Elec - paid separately usually, but here part of bill?)
                // Assuming ElecCost is calculated when readings are entered. For now just Rent + Carryover
                newLedger.TotalDue = newLedger.Carryover + newLedger.MonthlyRent + newLedger.MiscRent;
                newLedger.ClosingBalance = newLedger.TotalDue; // Assumes 0 paid initially

                _context.MonthlyLedgers.Add(newLedger);
                generatedCount++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Generated {generatedCount} bills for {period:MMMM yyyy}", count = generatedCount });
        }

        /// <summary>
        /// Update meter reading and recalculate bill
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBill([FromRoute] Guid id, [FromBody] BillUpdateModel model)
        {
            var ledger = await _context.MonthlyLedgers.FindAsync(id);
            if (ledger == null) return NotFound();

            // Update fields
            if (model.CurrentReading.HasValue)
            {
                ledger.ElecNew = model.CurrentReading.Value;
                ledger.ElecUnits = ledger.ElecNew - ledger.ElecPrev;
                if (ledger.ElecUnits < 0) ledger.ElecUnits = 0; 
                
                ledger.ElecCost = ledger.ElecUnits * ledger.ElecRate;
            }

            if (model.MonthlyRent.HasValue)
            {
                ledger.MonthlyRent = model.MonthlyRent.Value;
            }

            if (model.MiscAmount.HasValue)
            {
                ledger.MiscRent = model.MiscAmount.Value;
            }

            if (model.Remarks != null)
            {
                ledger.Remarks = model.Remarks;
            }

            // Recalculate Totals
            // Total = Arrears + Rent + Electric + Misc
            ledger.TotalDue = ledger.Carryover + ledger.MonthlyRent + ledger.ElecCost + ledger.MiscRent;
            
            // Recalculate Closing Balance (Total - Paid)
            ledger.ClosingBalance = ledger.TotalDue - ledger.AmountPaid;

            ledger.UpdatedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Bill updated successfully", ledger });
        }

        public class BillUpdateModel
        {
            public decimal? CurrentReading { get; set; }
            public decimal? MonthlyRent { get; set; }
            public decimal? MiscAmount { get; set; }
            public string? Remarks { get; set; }
        }
    }
}
