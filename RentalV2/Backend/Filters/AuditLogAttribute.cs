namespace RentalBackend.Filters;

/// <summary>
/// Marks a controller or action for automatic audit logging.
/// Specify the module name (e.g., "Tenant", "Property", "Payment").
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AuditLogAttribute : Attribute
{
    public string ModuleName { get; }
    public string? EntityName { get; }

    public AuditLogAttribute(string moduleName, string? entityName = null)
    {
        ModuleName = moduleName;
        EntityName = entityName;
    }
}
