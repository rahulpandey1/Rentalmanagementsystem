using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Services;

namespace RentalBackend.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly RentManagementContext _context;
    private readonly AuditService _auditService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(
        RentManagementContext context,
        AuditService auditService,
        IConfiguration configuration,
        ILogger<AuditLogsController> logger)
    {
        _context = context;
        _auditService = auditService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated audit logs with filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? user,
        [FromQuery] string? role,
        [FromQuery] string? action,
        [FromQuery] string? moduleName,
        [FromQuery] string? entityName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.CreatedDateTime >= startDate.Value.ToUniversalTime());
        if (endDate.HasValue)
            query = query.Where(a => a.CreatedDateTime <= endDate.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(user))
            query = query.Where(a => a.UserId.Contains(user));
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(a => a.Role == role);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(moduleName))
            query = query.Where(a => a.ModuleName == moduleName);
        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.UserName,
                a.Role,
                a.Action,
                a.ModuleName,
                a.EntityName,
                a.EntityId,
                a.IpAddress,
                a.CreatedDateTime
            })
            .ToListAsync();

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    /// <summary>
    /// Get single audit log detail
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuditLog(Guid id)
    {
        var log = await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (log == null) return NotFound();

        return Ok(log);
    }

    /// <summary>
    /// Get distinct filter values for the audit logs page
    /// </summary>
    [HttpGet("filters")]
    public async Task<IActionResult> GetFilterOptions()
    {
        var users = await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.UserId)
            .Distinct()
            .OrderBy(u => u)
            .ToListAsync();

        var actions = await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        var modules = await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.ModuleName)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();

        var entities = await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        return Ok(new { users, actions, modules, entities });
    }

    /// <summary>
    /// Manually trigger audit log cleanup
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupAuditLogs()
    {
        var retentionDays = _configuration.GetValue<int>("AuditRetentionDays", 90);
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        var deleted = await _context.Database.ExecuteSqlRawAsync(
            @"DELETE FROM public.""AuditLogs"" WHERE ""CreatedDateTime"" < {0}", cutoff);

        _logger.LogInformation("Manual audit cleanup: deleted {Count} records older than {Days} days", deleted, retentionDays);

        await _auditService.LogAsync("Cleanup", "AuditLog", "AuditLogs", null, null,
            new { retentionDays, deletedCount = deleted });

        return Ok(new { deleted, retentionDays, cutoffDate = cutoff });
    }

    /// <summary>
    /// Cleanup test occupancies and ledgers from a flat (admin utility)
    /// </summary>
    [HttpPost("cleanup-flat/{flatId}")]
    public async Task<IActionResult> CleanupFlat(Guid flatId, [FromQuery] string nameFilter = "User One,User Two")
    {
        var filters = nameFilter.Split(',').Select(f => f.Trim()).ToList();
        
        // Find test tenant IDs matching the name filter
        var testTenantIds = await _context.Set<RentalBackend.Models.Tenant>()
            .Where(t => filters.Any(f => t.Name.Contains(f)))
            .Select(t => t.TenantId)
            .ToListAsync();

        // Delete matching ledger entries
        var ledgers = await _context.MonthlyLedgers
            .Where(l => l.FlatId == flatId && l.TenantId.HasValue && testTenantIds.Contains(l.TenantId.Value))
            .ToListAsync();
        _context.MonthlyLedgers.RemoveRange(ledgers);

        // Delete matching occupancies  
        var occupancies = await _context.Set<RentalBackend.Models.Occupancy>()
            .Where(o => o.FlatId == flatId && o.TenantId.HasValue && testTenantIds.Contains(o.TenantId.Value))
            .ToListAsync();
        _context.Set<RentalBackend.Models.Occupancy>().RemoveRange(occupancies);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned flat {FlatId}: removed {Ledgers} ledgers, {Occupancies} occupancies", 
            flatId, ledgers.Count, occupancies.Count);

        return Ok(new { 
            flatId, 
            ledgersRemoved = ledgers.Count, 
            occupanciesRemoved = occupancies.Count, 
            nameFilter 
        });
    }

    /// <summary>
    /// Delete audit entries by action type (e.g. remove all "View" entries)
    /// </summary>
    [HttpPost("purge-action")]
    public async Task<IActionResult> DeleteByAction([FromQuery] string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            return BadRequest("action query parameter is required");
        var count = await _context.Database.ExecuteSqlRawAsync(
            @"DELETE FROM public.""AuditLogs"" WHERE ""Action"" = {0}", action);
        _logger.LogInformation("Deleted {Count} audit entries with action '{Action}'", count, action);
        return Ok(new { deleted = count, action });
    }
}
