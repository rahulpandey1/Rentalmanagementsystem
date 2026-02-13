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
    public class TenantsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public TenantsController(RentManagementContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTenants(int? year, int? month)
        {
            // If period specified, show tenants based on ledger data for that month
            if (month != null && year != null)
            {
                var period = new DateOnly(year.Value, month.Value, 1);
                var ledgers = await _context.MonthlyLedgers
                    .Include(l => l.Tenant)
                    .Include(l => l.Flat)
                    .Where(l => l.Period == period && l.TenantId != null)
                    .OrderBy(l => l.Tenant!.Name)
                    .ToListAsync();

                var result = ledgers.Select(l => new
                {
                    Id = l.TenantId,
                    TenantId = l.TenantId,
                    Name = l.Tenant?.Name ?? "VACANT",
                    FirstName = l.Tenant?.Name ?? "VACANT",
                    LastName = "",
                    PhoneNumber = (string?)null,
                    Email = (string?)null,
                    Address = (string?)null,
                    IsActive = true,
                    IsAssigned = true,
                    RoomNumber = l.Flat?.RoomCode,
                    FlatId = l.FlatId,
                    MonthlyRent = l.MonthlyRent,
                    SecurityDeposit = l.ElectricSecurity,
                    StartDate = l.DateOfAllotment?.ToDateTime(TimeOnly.MinValue),
                    NeedsRentIncrease = false,
                    ClosingBalance = l.ClosingBalance
                });

                return Ok(result);
            }

            // Default: show all tenants with current occupancy
            var tenants = await _context.Tenants
                .Include(t => t.Occupancies)
                    .ThenInclude(o => o.Flat)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var defaultResult = tenants.Select(t =>
            {
                var activeOcc = t.Occupancies
                    .FirstOrDefault(o => o.EndDate == null);

                var latestLedger = _context.MonthlyLedgers
                    .Where(l => l.TenantId == t.TenantId)
                    .OrderByDescending(l => l.Period)
                    .FirstOrDefault();

                return new
                {
                    Id = t.TenantId,
                    t.TenantId,
                    t.Name,
                    FirstName = t.Name,
                    LastName = "",
                    PhoneNumber = (string?)null,
                    Email = (string?)null,
                    Address = (string?)null,
                    IsActive = true,
                    IsAssigned = activeOcc != null,
                    RoomNumber = activeOcc?.Flat?.RoomCode,
                    FlatId = activeOcc?.FlatId,
                    MonthlyRent = latestLedger?.MonthlyRent ?? 0,
                    SecurityDeposit = latestLedger?.ElectricSecurity ?? 0,
                    StartDate = activeOcc?.StartDate?.ToDateTime(TimeOnly.MinValue),
                    NeedsRentIncrease = false,
                    ClosingBalance = latestLedger?.ClosingBalance ?? 0
                };
            });

            return Ok(defaultResult);
        }

        [HttpGet("unassigned")]
        public async Task<ActionResult<IEnumerable<object>>> GetUnassignedTenants()
        {
            var allTenantIds = await _context.Tenants.Select(t => t.TenantId).ToListAsync();
            var assignedTenantIds = await _context.Occupancies
                .Where(o => o.EndDate == null && o.TenantId != null)
                .Select(o => o.TenantId!.Value)
                .Distinct()
                .ToListAsync();

            var unassignedIds = allTenantIds.Except(assignedTenantIds).ToList();

            var unassigned = await _context.Tenants
                .Where(t => unassignedIds.Contains(t.TenantId))
                .Select(t => new
                {
                    Id = t.TenantId,
                    t.TenantId,
                    t.Name,
                    FirstName = t.Name,
                    LastName = "",
                    PhoneNumber = (string?)null
                })
                .ToListAsync();

            return Ok(unassigned);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTenant(Guid id)
        {
            var tenant = await _context.Tenants
                .Include(t => t.Occupancies)
                    .ThenInclude(o => o.Flat)
                .FirstOrDefaultAsync(t => t.TenantId == id);

            if (tenant == null) return NotFound();

            var activeOcc = tenant.Occupancies.FirstOrDefault(o => o.EndDate == null);

            // Get ledger history
            var ledgerHistory = await _context.MonthlyLedgers
                .Where(l => l.TenantId == id)
                .Include(l => l.Flat)
                .OrderByDescending(l => l.Period)
                .Take(24)
                .Select(l => new
                {
                    Period = l.Period.ToString("yyyy-MM"),
                    RoomCode = l.Flat!.RoomCode,
                    l.MonthlyRent,
                    l.ElecCost,
                    l.MiscRent,
                    l.TotalDue,
                    l.AmountPaid,
                    l.ClosingBalance
                })
                .ToListAsync();

            var latestLedger = ledgerHistory.FirstOrDefault();

            var result = new
            {
                Id = tenant.TenantId,
                tenant.TenantId,
                tenant.Name,
                FirstName = tenant.Name,
                LastName = "",
                PhoneNumber = (string?)null,
                Email = (string?)null,
                Address = (string?)null,
                IsActive = true,
                IsAssigned = activeOcc != null,
                RoomNumber = activeOcc?.Flat?.RoomCode,
                FlatId = activeOcc?.FlatId,
                StartDate = activeOcc?.StartDate?.ToDateTime(TimeOnly.MinValue),
                MonthlyRent = latestLedger?.MonthlyRent ?? 0,
                ClosingBalance = latestLedger?.ClosingBalance ?? 0,
                LedgerHistory = ledgerHistory,
                OccupancyHistory = tenant.Occupancies.OrderByDescending(o => o.StartDate).Select(o => new
                {
                    RoomCode = o.Flat?.RoomCode,
                    o.StartDate,
                    o.EndDate,
                    IsActive = o.EndDate == null
                })
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> PostTenant([FromBody] TenantCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) && string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest("Name is required.");

            var name = !string.IsNullOrWhiteSpace(request.Name)
                ? request.Name
                : $"{request.FirstName} {request.LastName}".Trim();

            // Check for duplicate
            var exists = await _context.Tenants.AnyAsync(t => t.Name == name);
            if (exists)
                return BadRequest($"Tenant '{name}' already exists.");

            var tenant = new Tenant { Name = name };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // If a flat was specified, create occupancy
            if (request.FlatId != null || request.RoomId != null)
            {
                Flat? flat = null;
                if (request.FlatId != null)
                    flat = await _context.Flats.FindAsync(request.FlatId);
                else if (request.RoomId != null)
                {
                    // RoomId might be sent as string GUID from frontend
                    if (Guid.TryParse(request.RoomId, out var roomGuid))
                        flat = await _context.Flats.FindAsync(roomGuid);
                }

                if (flat != null)
                {
                    var occupancy = new Occupancy
                    {
                        FlatId = flat.FlatId,
                        TenantId = tenant.TenantId,
                        StartDate = request.StartDate != null
                            ? DateOnly.FromDateTime(request.StartDate.Value)
                            : DateOnly.FromDateTime(DateTime.UtcNow)
                    };
                    _context.Occupancies.Add(occupancy);
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { message = $"Tenant '{name}' added.", tenantId = tenant.TenantId });
        }

        [HttpPost("{id}/assign")]
        public async Task<ActionResult> AssignTenantToRoom(Guid id, [FromBody] RoomAssignmentRequest request)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return NotFound("Tenant not found.");

            Flat? flat = null;
            if (request.FlatId != null)
                flat = await _context.Flats.FindAsync(request.FlatId);
            else if (request.RoomId != null)
            {
                if (Guid.TryParse(request.RoomId, out var roomGuid))
                    flat = await _context.Flats.FindAsync(roomGuid);
            }

            if (flat == null) return NotFound("Flat not found.");

            // Check if flat already has active occupancy
            var existingOcc = await _context.Occupancies
                .FirstOrDefaultAsync(o => o.FlatId == flat.FlatId && o.EndDate == null && o.TenantId != null);
            if (existingOcc != null)
                return BadRequest("This flat is already occupied.");

            var occupancy = new Occupancy
            {
                FlatId = flat.FlatId,
                TenantId = id,
                StartDate = request.StartDate != null
                    ? DateOnly.FromDateTime(request.StartDate.Value)
                    : DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _context.Occupancies.Add(occupancy);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Tenant '{tenant.Name}' assigned to {flat.RoomCode}." });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> PutTenant(Guid id, [FromBody] TenantUpdateRequest request)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name))
                tenant.Name = request.Name;
            else if (!string.IsNullOrWhiteSpace(request.FirstName))
                tenant.Name = $"{request.FirstName} {request.LastName}".Trim();

            tenant.UpdatedUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tenant updated." });
        }
    }

    public class TenantCreateRequest
    {
        public string? Name { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? IdProofType { get; set; }
        public string? IdProofNumber { get; set; }
        public Guid? FlatId { get; set; }
        public string? RoomId { get; set; }
        public DateTime? StartDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal SecurityDeposit { get; set; }
    }

    public class RoomAssignmentRequest
    {
        public Guid? FlatId { get; set; }
        public string? RoomId { get; set; }
        public DateTime? StartDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal SecurityDeposit { get; set; }
    }

    public class TenantUpdateRequest
    {
        public string? Name { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
