namespace Encina.Security;

/// <summary>
/// Requires the user to have specific permissions to execute the request.
/// </summary>
/// <remarks>
/// <para>
/// By default, the user needs at least one of the specified permissions (OR logic).
/// Set <see cref="RequireAll"/> to <c>true</c> for AND logic.
/// </para>
/// <para>
/// Permissions follow a <c>resource:action</c> convention (e.g., "orders:read", "users:delete").
/// Evaluation is delegated to <see cref="IPermissionEvaluator"/> for extensibility.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // OR logic (default): user needs "orders:read" OR "orders:admin"
/// [RequirePermission("orders:read", "orders:admin")]
/// public sealed record GetOrderQuery(Guid OrderId) : IQuery&lt;OrderDto&gt;;
///
/// // AND logic: user needs BOTH "orders:read" AND "reports:generate"
/// [RequirePermission("orders:read", "reports:generate", RequireAll = true)]
/// public sealed record GenerateOrderReportQuery(Guid OrderId) : IQuery&lt;ReportDto&gt;;
/// </code>
/// </example>
public sealed class RequirePermissionAttribute : SecurityAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
    /// </summary>
    /// <param name="permissions">One or more permission strings required to execute the request.</param>
    public RequirePermissionAttribute(params string[] permissions)
    {
        Permissions = permissions;
    }

    /// <summary>
    /// Gets the permissions required to execute the request.
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Gets or sets whether all permissions are required (AND logic) or any single permission suffices (OR logic).
    /// </summary>
    /// <remarks>
    /// Default is <c>false</c> (OR logic).
    /// </remarks>
    public bool RequireAll { get; set; }
}
