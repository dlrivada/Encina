using System.Security.Claims;

namespace Encina.Security;

/// <summary>
/// Represents the security context for the current request, providing
/// identity, role, and permission information for authorization decisions.
/// </summary>
/// <remarks>
/// <para>
/// The security context is populated per-request and carries all security-relevant
/// information needed by the Encina security pipeline. It is intentionally separate
/// from <see cref="IRequestContext"/> to maintain single responsibility.
/// </para>
/// <para>
/// All properties are read-only to ensure immutability after creation.
/// </para>
/// <para><b>Common Use Cases:</b></para>
/// <list type="bullet">
/// <item><description>Permission-based authorization in pipeline behaviors</description></item>
/// <item><description>Role-based access control (RBAC) enforcement</description></item>
/// <item><description>Resource ownership verification</description></item>
/// <item><description>Multi-tenant security isolation</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Checking permissions in a handler
/// if (securityContext.IsAuthenticated &amp;&amp;
///     securityContext.Permissions.Contains("orders:read"))
/// {
///     // Authorized access
/// }
/// </code>
/// </example>
public interface ISecurityContext
{
    /// <summary>
    /// Gets the claims principal representing the authenticated user.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the request is unauthenticated (anonymous).
    /// In ASP.NET Core, this is typically populated from <c>HttpContext.User</c>.
    /// </remarks>
    ClaimsPrincipal? User { get; }

    /// <summary>
    /// Gets the unique identifier of the authenticated user.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the request is unauthenticated.
    /// Typically extracted from the <c>sub</c> or <c>nameidentifier</c> claim.
    /// </remarks>
    string? UserId { get; }

    /// <summary>
    /// Gets the tenant identifier for multi-tenant scenarios.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the application is not multi-tenant or tenant cannot be determined.
    /// Used for tenant-scoped authorization decisions.
    /// </remarks>
    string? TenantId { get; }

    /// <summary>
    /// Gets the set of roles assigned to the current user.
    /// </summary>
    /// <remarks>
    /// Returns an empty set for unauthenticated users.
    /// Roles follow a flat hierarchy (e.g., "Admin", "Manager", "User").
    /// </remarks>
    IReadOnlySet<string> Roles { get; }

    /// <summary>
    /// Gets the set of permissions granted to the current user.
    /// </summary>
    /// <remarks>
    /// Returns an empty set for unauthenticated users.
    /// Permissions follow a resource:action convention (e.g., "orders:read", "users:delete").
    /// </remarks>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    /// <remarks>
    /// <c>true</c> when a valid identity is present; <c>false</c> for anonymous requests.
    /// </remarks>
    bool IsAuthenticated { get; }
}
