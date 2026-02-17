namespace Encina.Security;

/// <summary>
/// Configuration options for the Encina security pipeline.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <c>SecurityPipelineBehavior</c> and how
/// <see cref="SecurityContext"/> extracts claims from the <c>ClaimsPrincipal</c>.
/// Register via <c>AddEncinaSecurity(options => { ... })</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaSecurity(options =>
/// {
///     options.RequireAuthenticatedByDefault = true;
///     options.UserIdClaimType = "sub";
///     options.RoleClaimType = "role";
///     options.PermissionClaimType = "permission";
///     options.TenantIdClaimType = "tenant_id";
/// });
/// </code>
/// </example>
public sealed class SecurityOptions
{
    /// <summary>
    /// Gets or sets whether all requests require authentication by default.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, requests without any security attributes will still require
    /// <see cref="ISecurityContext.IsAuthenticated"/> to be <c>true</c>.
    /// Use <see cref="AllowAnonymousAttribute"/> to opt out individual requests.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool RequireAuthenticatedByDefault { get; set; }

    /// <summary>
    /// Gets or sets the claim type used to extract the user identifier.
    /// </summary>
    /// <remarks>
    /// The <see cref="SecurityContext"/> also checks <c>ClaimTypes.NameIdentifier</c>
    /// as a fallback when the primary claim type is not found.
    /// Default is <c>"sub"</c>.
    /// </remarks>
    public string UserIdClaimType { get; set; } = "sub";

    /// <summary>
    /// Gets or sets the claim type used to extract user roles.
    /// </summary>
    /// <remarks>
    /// The <see cref="SecurityContext"/> also checks <c>ClaimTypes.Role</c>
    /// as a fallback to support both short and URI-based claim types.
    /// Default is <c>"role"</c>.
    /// </remarks>
    public string RoleClaimType { get; set; } = "role";

    /// <summary>
    /// Gets or sets the claim type used to extract user permissions.
    /// </summary>
    /// <remarks>
    /// Default is <c>"permission"</c>.
    /// </remarks>
    public string PermissionClaimType { get; set; } = "permission";

    /// <summary>
    /// Gets or sets the claim type used to extract the tenant identifier.
    /// </summary>
    /// <remarks>
    /// Default is <c>"tenant_id"</c>.
    /// </remarks>
    public string TenantIdClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// Gets or sets whether to throw when <see cref="ISecurityContext"/> is not available.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, requests that have security attributes but no security context
    /// will throw an exception via <see cref="SecurityErrors.MissingContext"/>.
    /// When <c>false</c>, the behavior returns an error result instead.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool ThrowOnMissingSecurityContext { get; set; }

    /// <summary>
    /// Gets or sets whether to register the security health check.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, a health check is registered that verifies all security services
    /// are resolvable from the DI container.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }
}
