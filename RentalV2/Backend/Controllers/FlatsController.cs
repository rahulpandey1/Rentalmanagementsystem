using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Filters;
using RentalBackend.Models;

namespace RentalBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [AuditLog("Property", "Flat")]
    public class FlatsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public FlatsController(RentManagementContext context)
        {
            _context = context;
        }

        private async Task<int> GetNextInvoiceBaseNumber()
        {
            var maxInvoice = await _context.MonthlyLedgers
                .Where(l => l.InvoiceNumber != null)
                .OrderByDescending(l => l.InvoiceNumber)
                .Select(l => l.InvoiceNumber)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (maxInvoice != null && maxInvoice.StartsWith("INV-") && 
                int.TryParse(maxInvoice.Substring(4), out int currentNum))
            {
                nextNum = currentNum + 1;
            }

            return nextNum;
        }

        private string FormatInvoice(int num) => $"INV-{num:D6}";

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

                // Load active occupancies to cross-reference with ledger data
                var activeOccupancies = await _context.Occupancies
                    .Include(o => o.Tenant)
                    .Where(o => o.EndDate == null)
                    .ToListAsync();
                var activeOccByFlat = activeOccupancies
                    .GroupBy(o => o.FlatId)
                    .ToDictionary(g => g.Key, g => g.First());

                var ledgerByFlat = ledgers.ToDictionary(l => l.FlatId);

                var result = allFlats.Select((f, index) =>
                {
                    ledgerByFlat.TryGetValue(f.FlatId, out var ledger);
                    activeOccByFlat.TryGetValue(f.FlatId, out var activeOcc);
                    var tenantName = ledger?.Tenant?.Name ?? activeOcc?.Tenant?.Name;

                    // Room is vacant if: no active occupancy (source of truth), 
                    // OR ledger explicitly shows VACANT name (historical data)
                    var hasActiveOccupancy = activeOcc != null && activeOcc.TenantId != null;
                    var isVacant = !hasActiveOccupancy;

                    return new
                    {
                        f.FlatId,
                        RoomNumber = f.RoomCode,
                        RoomCode = f.RoomCode,
                        FloorNumber = f.Floor ?? 0,
                        f.MeterId,
                        f.BaseRent,
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
                        MiscChargeName = ledger?.MiscChargeName,
                        Carryover = ledger?.Carryover ?? 0,
                        TotalDue = ledger?.TotalDue ?? 0,
                        AmountPaid = ledger?.AmountPaid ?? 0,
                        ClosingBalance = ledger?.ClosingBalance ?? 0,
                        PaymentDate = ledger?.PaymentDate?.ToString("dd-MMM-yyyy"),
                        Remarks = ledger?.Remarks,
                        // For card-view compatibility
                        CurrentTenant = isVacant ? null : new
                        {
                            Id = ledger?.TenantId ?? activeOcc?.TenantId,
                            Name = tenantName,
                            Since = ledger?.DateOfAllotment?.ToString("dd-MMM-yyyy")
                                ?? activeOcc?.StartDate?.ToString("dd-MMM-yyyy"),
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
                    f.MeterId,
                    f.BaseRent,
                    MonthlyRent = latestLedger?.MonthlyRent ?? f.BaseRent,
                    IsAvailable = activeOcc == null || activeOcc.TenantId == null,
                    ElectricMeterNumber = f.MeterId,
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

            // Create ledger entries from start period through current month
            var startDate = assignment.StartDate ?? DateTime.UtcNow;
            var startPeriod = new DateOnly(startDate.Year, startDate.Month, 1);
            var currentPeriod = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            // Get previous month's data for continuity
            var prevPeriod = startPeriod.AddMonths(-1);
            var prevLedger = await _context.MonthlyLedgers
                .FirstOrDefaultAsync(l => l.FlatId == id && l.Period == prevPeriod);

            var period = startPeriod;
            MonthlyLedger? lastCreated = null;
            var invoiceCounter = await GetNextInvoiceBaseNumber();

            while (period <= currentPeriod)
            {
                var ledger = await _context.MonthlyLedgers
                    .FirstOrDefaultAsync(l => l.FlatId == id && l.Period == period);

                if (ledger == null)
                {
                    // Use previous ledger (from DB or just created) for continuity
                    var prev = lastCreated ?? prevLedger;
                    ledger = new MonthlyLedger
                    {
                        MonthlyLedgerId = Guid.NewGuid(),
                        FlatId = id,
                        TenantId = assignment.TenantId,
                        Period = period,
                        DateOfAllotment = period == startPeriod ? DateOnly.FromDateTime(startDate) : null,
                        MonthlyRent = assignment.MonthlyRent,
                        ElectricSecurity = assignment.SecurityDeposit, // Electric meter security
                        ElecPrev = prev?.ElecNew ?? 0,
                        ElecNew = prev?.ElecNew ?? 0,
                        ElecRate = prev?.ElecRate ?? 12.0m,
                        Carryover = prev?.ClosingBalance ?? 0,
                        MiscRent = assignment.MiscRent,
                        MiscChargeName = assignment.MiscChargeName
                    };
                    ledger.TotalDue = ledger.Carryover + ledger.MonthlyRent + ledger.ElecCost + ledger.MiscRent;
                    ledger.ClosingBalance = ledger.TotalDue - ledger.AmountPaid;
                    ledger.UpdatedUtc = DateTime.UtcNow;
                    ledger.InvoiceNumber = FormatInvoice(invoiceCounter++);
                    _context.MonthlyLedgers.Add(ledger);
                }
                else
                {
                    // Update existing ledger
                    ledger.TenantId = assignment.TenantId;
                    if (period == startPeriod)
                        ledger.DateOfAllotment = DateOnly.FromDateTime(startDate);
                    ledger.MonthlyRent = assignment.MonthlyRent;
                    ledger.ElectricSecurity = assignment.SecurityDeposit;
                    ledger.MiscRent = assignment.MiscRent;
                    ledger.MiscChargeName = assignment.MiscChargeName;
                    ledger.TotalDue = ledger.Carryover + ledger.MonthlyRent + ledger.ElecCost + ledger.MiscRent;
                    ledger.ClosingBalance = ledger.TotalDue - ledger.AmountPaid;
                    ledger.UpdatedUtc = DateTime.UtcNow;
                    if (string.IsNullOrEmpty(ledger.InvoiceNumber))
                        ledger.InvoiceNumber = FormatInvoice(invoiceCounter++);
                }

                lastCreated = ledger;
                period = period.AddMonths(1);
            }

            // Set initial ROOM security deposit on tenant (separate from electric security)
            var roomDeposit = assignment.RoomSecurityDeposit;
            if (roomDeposit > 0)
            {
                tenant.SecurityDeposit += roomDeposit;
                tenant.UpdatedUtc = DateTime.UtcNow;

                _context.SecurityDepositTransactions.Add(new SecurityDepositTransaction
                {
                    TenantId = assignment.TenantId,
                    Amount = roomDeposit,
                    Type = "Collection",
                    Description = $"Initial room security deposit on assignment to {flat.RoomCode}"
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Tenant '{tenant.Name}' assigned to {flat.RoomCode}" });
        }

        /// <summary>
        /// Vacate flat — includes settlement (deposit vs outstanding)
        /// </summary>
        [HttpPost("{id}/vacate")]
        public async Task<ActionResult> VacateFlat(Guid id)
        {
            var activeOcc = await _context.Occupancies
                .Include(o => o.Tenant)
                .FirstOrDefaultAsync(o => o.FlatId == id && o.EndDate == null);

            if (activeOcc == null)
                return BadRequest("No active occupancy found for this flat.");

            // Calculate total outstanding across all bills for this tenant+flat
            var totalOutstanding = await _context.MonthlyLedgers
                .Where(l => l.FlatId == id && l.TenantId == activeOcc.TenantId && l.ClosingBalance > 0)
                .SumAsync(l => l.ClosingBalance);

            var tenant = activeOcc.Tenant;
            var depositBalance = tenant?.SecurityDeposit ?? 0;
            var netRefund = depositBalance - totalOutstanding; // Positive = refund to tenant, Negative = tenant owes

            // Record a Refund transaction for the deposit
            if (tenant != null && depositBalance > 0)
            {
                var refundAmount = Math.Min(depositBalance, depositBalance); // full deposit returned/adjusted
                _context.SecurityDepositTransactions.Add(new SecurityDepositTransaction
                {
                    TenantId = tenant.TenantId,
                    Amount = -refundAmount,
                    Type = "Refund",
                    Description = $"Settlement on vacate from room {activeOcc.Flat?.RoomCode ?? id.ToString()[..8]}. Outstanding: ₹{totalOutstanding}, Deposit: ₹{depositBalance}, Net: ₹{netRefund}"
                });

                // If deposit covers outstanding, clear the bills
                if (depositBalance >= totalOutstanding && totalOutstanding > 0)
                {
                    var outstandingLedgers = await _context.MonthlyLedgers
                        .Where(l => l.FlatId == id && l.TenantId == activeOcc.TenantId && l.ClosingBalance > 0)
                        .ToListAsync();
                    foreach (var ledger in outstandingLedgers)
                    {
                        ledger.AmountPaid += ledger.ClosingBalance;
                        ledger.ClosingBalance = 0;
                        ledger.UpdatedUtc = DateTime.UtcNow;
                    }
                }

                tenant.SecurityDeposit = 0;
                tenant.UpdatedUtc = DateTime.UtcNow;
            }

            activeOcc.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Flat vacated. Tenant '{tenant?.Name}' removed.",
                settlement = new
                {
                    depositBalance,
                    totalOutstanding,
                    netRefund,
                    summary = netRefund >= 0
                        ? $"Refund ₹{netRefund} to tenant"
                        : $"Tenant owes ₹{Math.Abs(netRefund)}"
                }
            });
        }

        /// <summary>
        /// Renew agreement / Hike rent for same tenant in same flat
        /// Works in two modes:
        /// 1. If active occupancy exists: renews occupancy + updates rent on ledger
        /// 2. If no active occupancy but ledger exists for period: just updates rent (rent hike)
        /// </summary>
        [HttpPost("{id}/renew-agreement")]
        public async Task<ActionResult> RenewAgreement(Guid id, [FromBody] RenewRequest request)
        {
            var activeOcc = await _context.Occupancies
                .Include(o => o.Tenant)
                .FirstOrDefaultAsync(o => o.FlatId == id && o.EndDate == null);

            var now = DateTime.UtcNow;
            var effectiveDate = request.StartDate ?? now;
            var period = new DateOnly(effectiveDate.Year, effectiveDate.Month, 1);

            // Get current ledger for this period
            var ledger = await _context.MonthlyLedgers
                .FirstOrDefaultAsync(l => l.FlatId == id && l.Period == period);

            if (activeOcc != null && activeOcc.TenantId != null)
            {
                // Mode 1: Full renewal — end current occupancy, start new one, update rent
                activeOcc.EndDate = DateOnly.FromDateTime(effectiveDate).AddDays(-1);

                var newOcc = new Occupancy
                {
                    FlatId = id,
                    TenantId = activeOcc.TenantId,
                    StartDate = DateOnly.FromDateTime(effectiveDate)
                };
                _context.Occupancies.Add(newOcc);

                // Update rent on current ledger if exists
                if (ledger != null && request.MonthlyRent > 0)
                {
                    var oldRent = ledger.MonthlyRent;
                    ledger.MonthlyRent = request.MonthlyRent;
                    ledger.TotalDue = ledger.Carryover + ledger.MonthlyRent + ledger.ElecCost + ledger.MiscRent;
                    ledger.ClosingBalance = ledger.TotalDue - ledger.AmountPaid;
                    ledger.UpdatedUtc = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"Agreement renewed for '{activeOcc.Tenant?.Name}'. Rent updated to ₹{request.MonthlyRent}." });
            }
            else if (ledger != null && ledger.TenantId != null)
            {
                // Mode 2: Rent hike only — no active occupancy but ledger shows tenant
                // This happens when occupancy was ended but ledger still has data
                if (request.MonthlyRent > 0)
                {
                    var oldRent = ledger.MonthlyRent;
                    ledger.MonthlyRent = request.MonthlyRent;
                    ledger.TotalDue = ledger.Carryover + ledger.MonthlyRent + ledger.ElecCost + ledger.MiscRent;
                    ledger.ClosingBalance = ledger.TotalDue - ledger.AmountPaid;
                    ledger.UpdatedUtc = DateTime.UtcNow;

                    // Also re-create the occupancy since it should be active
                    var newOcc = new Occupancy
                    {
                        FlatId = id,
                        TenantId = ledger.TenantId,
                        StartDate = DateOnly.FromDateTime(effectiveDate)
                    };
                    _context.Occupancies.Add(newOcc);

                    await _context.SaveChangesAsync();

                    var tenant = await _context.Tenants.FindAsync(ledger.TenantId);
                    return Ok(new { message = $"Rent updated for '{tenant?.Name}' from ₹{oldRent} to ₹{request.MonthlyRent}." });
                }

                return BadRequest("Monthly rent must be greater than 0.");
            }
            else
            {
                return BadRequest("No active occupancy or ledger found for this flat. Assign a tenant first.");
            }
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
        public decimal SecurityDeposit { get; set; }  // Electric meter security (goes to ledger)
        public decimal RoomSecurityDeposit { get; set; }  // Room security deposit (goes to tenant balance)
        public decimal MiscRent { get; set; }
        public string? MiscChargeName { get; set; }
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
        public string? MeterId { get; set; }
        public decimal BaseRent { get; set; }
    }
}
