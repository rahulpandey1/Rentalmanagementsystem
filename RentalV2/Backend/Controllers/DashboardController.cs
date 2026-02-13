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
    public class DashboardController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public DashboardController(RentManagementContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetDashboardStats(int? year, int? month)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;

            var totalFlats = await _context.Flats.CountAsync();
            var totalTenants = await _context.Tenants.CountAsync();

            // Period-specific data from ledger
            var period = new DateOnly(targetYear, targetMonth, 1);
            var ledgersForPeriod = await _context.MonthlyLedgers
                .Include(l => l.Tenant)
                .Where(l => l.Period == period)
                .ToListAsync();

            int occupiedCount;
            int availableCount;

            if (ledgersForPeriod.Any())
            {
                // Use ledger data to determine occupancy for the period
                occupiedCount = ledgersForPeriod
                    .Where(l => l.TenantId != null && l.Tenant != null
                        && !l.Tenant.Name.Contains("VACANT", StringComparison.OrdinalIgnoreCase)
                        && !l.Tenant.Name.Contains("VACAMT", StringComparison.OrdinalIgnoreCase))
                    .Select(l => l.FlatId)
                    .Distinct()
                    .Count();
                availableCount = totalFlats - occupiedCount;
            }
            else
            {
                // No ledger data for this period — all rooms vacant
                occupiedCount = 0;
                availableCount = totalFlats;
            }

            // Floor-wise occupancy based on period
            var flatsWithFloor = await _context.Flats.ToListAsync();
            var groundFloorTotal = flatsWithFloor.Count(f => f.Floor == 0 || f.Floor == null);
            var firstFloorTotal = flatsWithFloor.Count(f => f.Floor == 1);
            var secondFloorTotal = flatsWithFloor.Count(f => f.Floor == 2);

            var occupiedFlatIdsForPeriod = ledgersForPeriod
                .Where(l => l.TenantId != null && l.Tenant != null
                    && !l.Tenant.Name.Contains("VACANT", StringComparison.OrdinalIgnoreCase)
                    && !l.Tenant.Name.Contains("VACAMT", StringComparison.OrdinalIgnoreCase))
                .Select(l => l.FlatId)
                .Distinct()
                .ToHashSet();

            var occupiedFlats = flatsWithFloor.Where(f => occupiedFlatIdsForPeriod.Contains(f.FlatId)).ToList();
            var groundFloorOccupied = occupiedFlats.Count(f => f.Floor == 0 || f.Floor == null);
            var firstFloorOccupied = occupiedFlats.Count(f => f.Floor == 1);
            var secondFloorOccupied = occupiedFlats.Count(f => f.Floor == 2);

            // Financial data — scope to selected period
            var totalRevenue = ledgersForPeriod.Sum(l => l.AmountPaid);
            var totalOutstanding = ledgersForPeriod.Sum(l => l.ClosingBalance);
            var totalRentDue = ledgersForPeriod.Sum(l => l.MonthlyRent);
            var totalElectricDue = ledgersForPeriod.Sum(l => l.ElecCost);
            var overdueLedgers = ledgersForPeriod.Count(l => l.ClosingBalance > 0);
            var activeAgreements = ledgersForPeriod
                .Where(l => l.TenantId != null)
                .Select(l => new { l.FlatId, l.TenantId })
                .Distinct()
                .Count();

            var dashboardData = new
            {
                Year = targetYear,
                Month = targetMonth,
                TotalRooms = totalFlats,
                AvailableRooms = availableCount,
                OccupiedRooms = occupiedCount,
                TotalTenants = totalTenants,
                ActiveRentAgreements = activeAgreements,
                TotalRevenue = totalRevenue,
                TotalOutstanding = totalOutstanding,
                TotalRentDue = totalRentDue,
                TotalElectricDue = totalElectricDue,
                OverdueBills = overdueLedgers,
                GroundFloorOccupied = groundFloorOccupied,
                FirstFloorOccupied = firstFloorOccupied,
                SecondFloorOccupied = secondFloorOccupied,
                GroundFloorTotal = groundFloorTotal,
                FirstFloorTotal = firstFloorTotal,
                SecondFloorTotal = secondFloorTotal
            };

            return Ok(dashboardData);
        }

        [HttpGet("available-periods")]
        public async Task<ActionResult<object>> GetAvailablePeriods()
        {
            var periods = await _context.MonthlyLedgers
                .Select(l => l.Period)
                .Distinct()
                .OrderByDescending(p => p)
                .ToListAsync();

            var result = periods.Select(p => new
            {
                Year = p.Year,
                Month = p.Month,
                Label = p.ToString("MMMM yyyy")
            });

            return Ok(result);
        }

        [HttpGet("billing-summary")]
        public async Task<ActionResult<object>> GetBillingSummary(int? year, int? month)
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

            var summary = new
            {
                TotalOutstanding = ledgers.Sum(l => l.ClosingBalance),
                TotalRentDue = ledgers.Sum(l => l.MonthlyRent),
                TotalElectricDue = ledgers.Sum(l => l.ElecCost),
                TotalMiscDue = ledgers.Sum(l => l.MiscRent),
                TotalAdvance = ledgers.Where(l => l.ClosingBalance < 0).Sum(l => Math.Abs(l.ClosingBalance)),
                OverdueBills = ledgers.Count(l => l.ClosingBalance > 0),
                RecentLedgers = ledgers.Select(l => new
                {
                    l.MonthlyLedgerId,
                    Period = l.Period.ToString("yyyy-MM"),
                    RoomCode = l.Flat?.RoomCode,
                    TenantName = l.Tenant?.Name ?? "VACANT",
                    l.MonthlyRent,
                    l.ElecCost,
                    l.MiscRent,
                    l.TotalDue,
                    l.AmountPaid,
                    l.ClosingBalance
                }),
                OutstandingByTenant = ledgers
                    .Where(l => l.ClosingBalance > 0 && l.Tenant != null)
                    .GroupBy(l => new { l.TenantId, l.Tenant!.Name, l.Flat!.RoomCode })
                    .Select(g => new
                    {
                        TenantId = g.Key.TenantId,
                        TenantName = g.Key.Name,
                        RoomNumber = g.Key.RoomCode,
                        TotalOutstanding = g.Sum(l => l.ClosingBalance),
                        BillCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalOutstanding)
                    .ToList()
            };

            return Ok(summary);
        }
    }
}
