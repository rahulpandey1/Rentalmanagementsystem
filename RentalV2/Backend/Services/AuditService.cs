using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Models;

namespace RentalBackend.Services;

/// <summary>
/// Centralized audit logging service. Inserts audit entries into the database.
/// On failure, logs the error to file logs and never throws to avoid breaking the main flow.
/// </summary>
public class AuditService
{
    private readonly RentManagementContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;
    private readonly IConfiguration _configuration;

    public AuditService(
        RentManagementContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task LogAsync(
        string action,
        string moduleName,
        string entityName,
        string? entityId = null,
        object? oldValues = null,
        object? newValues = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
                         ?? httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? "Anonymous";

            var adminEmails = _configuration.GetSection("AdminEmails").Get<string[]>() ?? Array.Empty<string>();
            var role = adminEmails.Any(e => e.Equals(userId, StringComparison.OrdinalIgnoreCase))
                ? "Admin"
                : "User";

            var correlationId = httpContext?.Items["CorrelationId"]?.ToString();
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var browserInfo = httpContext?.Request?.Headers["User-Agent"].FirstOrDefault();
            var requestUrl = httpContext != null
                ? $"{httpContext.Request.Method} {httpContext.Request.Path}{httpContext.Request.QueryString}"
                : null;

            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = userId,
                Role = role,
                Action = action,
                ModuleName = moduleName,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues != null ? SerializeToJson(oldValues) : null,
                NewValues = newValues != null ? SerializeToJson(newValues) : null,
                IpAddress = ipAddress,
                BrowserInfo = browserInfo,
                RequestUrl = requestUrl,
                CorrelationId = correlationId,
                CreatedDateTime = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Audit: {Action} on {Module}/{Entity} by {User} [CID:{CorrelationId}]",
                action, moduleName, entityName, userId, correlationId);
        }
        catch (Exception ex)
        {
            // Never throw — audit failure must not break the main flow
            _logger.LogError(ex,
                "Failed to write audit log for {Action} on {Module}/{Entity}",
                action, moduleName, entityName);
        }
    }

    private static string SerializeToJson(object value)
    {
        if (value is string s) return s;
        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
