using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RentalBackend.Services;

namespace RentalBackend.Filters;

/// <summary>
/// Global action filter that automatically creates audit log entries
/// for API actions decorated with [AuditLog].
/// </summary>
public class AuditActionFilter : IAsyncActionFilter
{
    private readonly AuditService _auditService;
    private readonly ILogger<AuditActionFilter> _logger;

    public AuditActionFilter(AuditService auditService, ILogger<AuditActionFilter> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Check for [AuditLog] attribute on the action or controller
        var auditAttr = context.ActionDescriptor.EndpointMetadata
            .OfType<AuditLogAttribute>()
            .FirstOrDefault();

        if (auditAttr == null)
        {
            // No audit attribute — just execute
            await next();
            return;
        }

        var httpMethod = context.HttpContext.Request.Method;
        var action = MapHttpMethodToAction(httpMethod);
        var moduleName = auditAttr.ModuleName;
        var entityName = auditAttr.EntityName ?? auditAttr.ModuleName;

        // Extract entity ID from route values
        string? entityId = null;
        if (context.ActionArguments.TryGetValue("id", out var idVal))
            entityId = idVal?.ToString();
        else if (context.RouteData.Values.TryGetValue("id", out var routeId))
            entityId = routeId?.ToString();

        // Capture request body for POST/PUT
        object? requestBody = null;
        if (httpMethod is "POST" or "PUT" or "PATCH")
        {
            requestBody = context.ActionArguments
                .Where(a => a.Key != "id")
                .Select(a => a.Value)
                .FirstOrDefault();
        }

        // Execute the action
        var resultContext = await next();

        // Only log if action succeeded (no exception)
        if (resultContext.Exception == null)
        {
            try
            {
                // Extract response body if it's an ObjectResult
                object? responseBody = null;
                if (resultContext.Result is ObjectResult objectResult)
                {
                    responseBody = objectResult.Value;
                }

                // For PUT/PATCH, request body = new values
                // For POST, response body = new entity
                object? oldValues = null;
                object? newValues = null;

                switch (httpMethod)
                {
                    case "POST":
                        newValues = responseBody ?? requestBody;
                        action = "Create";
                        break;
                    case "PUT":
                    case "PATCH":
                        newValues = requestBody;
                        action = "Update";
                        break;
                    case "DELETE":
                        action = "Delete";
                        break;
                    default:
                        // Don't audit GET requests
                        return;
                }

                await _auditService.LogAsync(action, moduleName, entityName, entityId, oldValues, newValues);
            }
            catch (Exception ex)
            {
                // Never let audit failure break the response
                _logger.LogError(ex, "AuditActionFilter failed to log audit entry");
            }
        }
    }

    private static string MapHttpMethodToAction(string method) => method switch
    {
        "POST" => "Create",
        "PUT" => "Update",
        "PATCH" => "Update",
        "DELETE" => "Delete",
        _ => "Read"
    };
}
