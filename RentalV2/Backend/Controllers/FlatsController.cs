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
    public class FlatsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public FlatsController(RentManagementContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetFlats(int? month, int? year)
        {
            // If period specified, return ledger-based data for that month
            if (month != null && year != null)
            {
                var period = new DateOnly(year.Value, month.Value, 1);
                var allFlats = await _context.Flats.OrderBy(f => f.RoomCode).ToListAsync();
                var ledgers = await _context.MonthlyLedgers
                    .Include(l => l.Tenant)
                    .Where(l => l.Period == period)
                    .ToListAsync();

                var ledgerByFlat = ledgers.ToDictionary(l => l.FlatId);

                var result = allFlats.Select((f, index) =>
                {
                    ledgerByFlat.TryGetValue(f.FlatId, out var ledger);
                    var tenantName = ledger?.Tenant?.Name;
                    var isVacant = ledger == null || tenantName == null ||
                        tenantName.Contains("VACANT", StringComparison.OrdinalIgnoreCase) ||
                        tenantName.Contains("VACAMT", StringComparison.OrdinalIgnoreCase);

                    return new
                    {
                        f.FlatId,
                        RoomNumber = f.RoomCode,
                        RoomCode = f.RoomCode,
                        FloorNumber = f.Floor ?? 0,
                        SerialNumber = ledger?.SerialNumber ?? (index + 1),
                        // Tenant info
                        TenantName = tenantName ?? "VACANT",
                        IsAvailable = isVacant,
                        // Allotment
                        DateOfAllotment = ledger?.DateOfAllotment?.ToString("dd-MMM-yyyy"),
                        // Rent
                        MonthlyRent = ledger?.MonthlyRent ?? 0,
                        // Security
                        ElectricSecurity = ledger?.ElectricSecurity ?? 0,
                        // Electric
                        ElecNew = ledger?.ElecNew ?? 0,
                        ElecPrev = ledger?.ElecPrev ?? 0,
                        ElecUnits = ledger?.ElecUnits ?? 0,
                        ElecCost = ledger?.ElecCost ?? 0,
                        ElecRate = ledger?.ElecRate ?? 0,
                        // Financials
                        MiscRent = ledger?.MiscRent ?? 0,
                        Carryover = ledger?.Carryover ?? 0,
                        TotalDue = ledger?.TotalDue ?? 0,
                        AmountPaid = ledger?.AmountPaid ?? 0,
                        ClosingBalance = ledger?.ClosingBalance ?? 0,
                        PaymentDate = ledger?.PaymentDate?.ToString("dd-MMM-yyyy"),
                        Remarks = ledger?.Remarks,
                        // For card-view compatibility
                        CurrentTenant = isVacant ? null : new
                        {
                            Id = ledger?.TenantId,
                            Name = tenantName,
                            Since = ledger?.DateOfAllotment?.ToString("dd-MMM-yyyy"),
                            SecurityDeposit = ledger?.ElectricSecurity ?? 0
                        }
                    };
                });

                return Ok(result);
            }

            // Default: current occupancy state
            var flats = await _context.Flats
                .Include(f => f.Occupancies.Where(o => o.EndDate == null))
                    .ThenInclude(o => o.Tenant)
                .OrderBy(f => f.RoomCode)
                .ToListAsync();

            var defaultResult = flats.Select(f =>
            {
                var activeOcc = f.Occupancies.FirstOrDefault(o => o.EndDate == null);
                var latestLedger = _context.MonthlyLedgers
                    .Where(l => l.FlatId == f.FlatId)
                    .OrderByDescending(l => l.Period)
                    .FirstOrDefault();

                return new
                {
                    f.FlatId,
                    RoomNumber = f.RoomCode,
                    RoomCode = f.RoomCode,
                    FloorNumber = f.Floor ?? 0,
                    MonthlyRent = latestLedger?.MonthlyRent ?? 0,
                    IsAvailable = activeOcc == null || activeOcc.TenantId == null,
                    ElectricMeterNumber = (string?)null,
                    LastMeterReading = (decimal?)latestLedger?.ElecNew,
                    LastReadingDate = latestLedger?.Period.ToDateTime(TimeOnly.MinValue),
                    CurrentTenant = activeOcc?.Tenant == null ? null : new
                    {
                        Id = activeOcc.Tenant.TenantId,
                        Name = activeOcc.Tenant.Name,
                        Since = activeOcc.StartDate?.ToDateTime(TimeOnly.MinValue),
                        SecurityDeposit = latestLedger?.ElectricSecurity ?? 0
                    }
                };
            });

            return Ok(defaultResult);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetFlat(Guid id)
        {
            var flat = await _context.Flats
                .Include(f => f.Occupancies)
                    .ThenInclude(o => o.Tenant)
                .FirstOrDefaultAsync(f => f.FlatId == id);

            if (flat == null) return NotFound();

            var activeOcc = flat.Occupancies.FirstOrDefault(o => o.EndDate == null);

            // Get ledger history for this flat
            var ledgerHistory = await _context.MonthlyLedgers
                .Where(l => l.FlatId == id)
                .OrderByDescending(l => l.Period)
                .Take(12)
                .Select(l => new
                {
                    Period = l.Period.ToString("yyyy-MM"),
                    l.MonthlyRent,
                    ElecPrev = l.ElecPrev,
                    ElecNew = l.ElecNew,
                    ElecUnits = l.ElecUnits,
                    ElecCost = l.ElecCost,
                    l.TotalDue,
                    l.AmountPaid,
                    l.ClosingBalance,
                    ReadingDate = l.Period.ToDateTime(TimeOnly.MinValue),
                    PreviousReading = l.ElecPrev,
                    CurrentReading = l.ElecNew,
                    UnitsConsumed = l.ElecUnits,
                    ElectricCharges = l.ElecCost
                })
                .ToListAsync();

            var latestLedger = ledgerHistory.FirstOrDefault();

            var result = new
            {
                flat.FlatId,
                RoomNumber = flat.RoomCode,
                RoomCode = flat.RoomCode,
                FloorNumber = flat.Floor ?? 0,
                MonthlyRent = latestLedger?.MonthlyRent ?? 0,
                IsAvailable = activeOcc == null || activeOcc.TenantId == null,
                CurrentTenant = activeOcc?.Tenant == null ? null : new
                {
                    Id = activeOcc.Tenant.TenantId,
                    Name = activeOcc.Tenant.Name,
                    Phone = (string?)null,
                    Since = activeOcc.StartDate?.ToDateTime(TimeOnly.MinValue),
                    SecurityDeposit = latestLedger?.ElecCost ?? 0
                },
                MeterReadingHistory = ledgerHistory,
                OccupancyHistory = flat.Occupancies.OrderByDescending(o => o.StartDate).Select(o => new
                {
                    o.OccupancyId,
                    TenantName = o.Tenant?.Name,
                    o.StartDate,
                    o.EndDate,
                    IsActive = o.EndDate == null
                })
            };

            return Ok(result);
        }

        /// <summary>
        /// Assign tenant to flat
        /// </summary>
        [HttpPost("{id}/assign-tenant")]
        public async Task<ActionResult> AssignTenant(Guid id, [FromBody] FlatTenantAssignment assignment)
        {
            var flat = await _context.Flats
                .Include(f => f.Occupancies.Where(o => o.EndDate == null))
                .FirstOrDefaultAsync(f => f.FlatId == id);

            if (flat == null) return NotFound("Flat not found");

            var activeOcc = flat.Occupancies.FirstOrDefault(o => o.EndDate == null);
            if (activeOcc != null && activeOcc.TenantId != null)
                return BadRequest("Flat is already occupied. Vacate first.");

            var tenant = await _context.Tenants.FindAsync(assignment.TenantId);
            if (tenant == null) return NotFound("Tenant not found");

            var occupancy = new Occupancy
            {
                FlatId = id,
                TenantId = assignment.TenantId,
                StartDate = assignment.StartDate != null
                    ? DateOnly.FromDateTime(assignment.StartDate.Value)
                    : DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _context.Occupancies.Add(occupancy);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Tenant '{tenant.Name}' assigned to {flat.RoomCode}" });
        }

        /// <summary>
        /// Vacate flat
        /// </summary>
        [HttpPost("{id}/vacate")]
        public async Task<ActionResult> VacateFlat(Guid id)
        {
            var activeOcc = await _context.Occupancies
                .Include(o => o.Tenant)
                .FirstOrDefaultAsync(o => o.FlatId == id && o.EndDate == null);

            if (activeOcc == null)
                return BadRequest("No active occupancy found for this flat.");

            activeOcc.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Flat vacated. Tenant '{activeOcc.Tenant?.Name}' removed." });
        }

        /// <summary>
        /// Renew agreement (end current, start new with same tenant)
        /// </summary>
        [HttpPost("{id}/renew-agreement")]
        public async Task<ActionResult> RenewAgreement(Guid id, [FromBody] RenewRequest request)
        {
            var activeOcc = await _context.Occupancies
                .Include(o => o.Tenant)
                .FirstOrDefaultAsync(o => o.FlatId == id && o.EndDate == null);

            if (activeOcc == null || activeOcc.TenantId == null)
                return BadRequest("No active occupancy to renew.");

            // End current occupancy
            activeOcc.EndDate = request.StartDate != null
                ? DateOnly.FromDateTime(request.StartDate.Value).AddDays(-1)
                : DateOnly.FromDateTime(DateTime.UtcNow);

            // Create new occupancy
            var newOcc = new Occupancy
            {
                FlatId = id,
                TenantId = activeOcc.TenantId,
                StartDate = request.StartDate != null
                    ? DateOnly.FromDateTime(request.StartDate.Value)
                    : DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _context.Occupancies.Add(newOcc);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Agreement renewed for '{activeOcc.Tenant?.Name}'." });
        }

        [HttpPut("{id}/availability")]
        public async Task<ActionResult> UpdateAvailability(Guid id, [FromBody] bool isAvailable)
        {
            // Availability is determined by occupancy, so this is a no-op
            // but we keep the endpoint for compatibility
            return Ok();
        }

        [HttpPut("{id}/rent")]
        public async Task<ActionResult> UpdateRent(Guid id, [FromBody] decimal newRent)
        {
            // Rent is stored in ledger entries. No direct update on flat.
            // This endpoint is kept for frontend compatibility but is a no-op.
            return Ok();
        }

        [HttpGet("for-billing")]
        public async Task<ActionResult<IEnumerable<object>>> GetFlatsForBilling()
        {
            var flats = await _context.Flats
                .Include(f => f.Occupancies.Where(o => o.EndDate == null))
                    .ThenInclude(o => o.Tenant)
                .OrderBy(f => f.RoomCode)
                .ToListAsync();

            var result = flats.Select(f =>
            {
                var activeOcc = f.Occupancies.FirstOrDefault(o => o.EndDate == null);
                var latestLedger = _context.MonthlyLedgers
                    .Where(l => l.FlatId == f.FlatId)
                    .OrderByDescending(l => l.Period)
                    .FirstOrDefault();

                return new
                {
                    Id = f.FlatId,
                    RoomNumber = f.RoomCode,
                    MonthlyRent = latestLedger?.MonthlyRent ?? 0,
                    LastMeterReading = latestLedger?.ElecNew ?? 0,
                    IsOccupied = activeOcc != null && activeOcc.TenantId != null,
                    TenantName = activeOcc?.Tenant?.Name,
                    TenantId = activeOcc?.TenantId
                };
            });

            return Ok(result);
        }

        /// <summary>
        /// Add a new flat/room
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> AddFlat([FromBody] AddFlatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoomCode))
                return BadRequest("Room code is required.");

            var exists = await _context.Flats.AnyAsync(f => f.RoomCode == request.RoomCode);
            if (exists)
                return BadRequest($"Room with code '{request.RoomCode}' already exists.");

            var flat = new Flat
            {
                RoomCode = request.RoomCode,
                Floor = request.Floor
            };

            _context.Flats.Add(flat);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Room '{flat.RoomCode}' added successfully.", flatId = flat.FlatId });
        }
    }

    public class FlatTenantAssignment
    {
        public Guid TenantId { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal SecurityDeposit { get; set; }
        public DateTime? StartDate { get; set; }
    }

    public class RenewRequest
    {
        public decimal MonthlyRent { get; set; }
        public DateTime? StartDate { get; set; }
    }

    public class AddFlatRequest
    {
        public string RoomCode { get; set; } = string.Empty;
        public int? Floor { get; set; }
    }
}
