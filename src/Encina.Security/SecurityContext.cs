using System.Collections.Immutable;
using System.Security.Claims;

namespace Encina.Security;

/// <summary>
/// Default implementation of <see cref="ISecurityContext"/> that extracts
/// identity, roles, and permissions from a <see cref="ClaimsPrincipal"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation extracts claims using configurable claim types from
/// <see cref="SecurityOptions"/>. Defaults:
/// <list type="bullet">
/// <item><description><c>sub</c> (or <c>nameidentifier</c>) for <see cref="UserId"/></description></item>
/// <item><description><c>tenant_id</c> for <see cref="TenantId"/></description></item>
/// <item><description><c>role</c> for <see cref="Roles"/></description></item>
/// <item><description><c>permission</c> for <see cref="Permissions"/></description></item>
/// </list>
/// </para>
/// <para>
/// All collection properties are immutable after construction.
/// </para>
/// </remarks>
public sealed class SecurityContext : ISecurityContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityContext"/> class
    /// from a <see cref="ClaimsPrincipal"/> using the specified options for claim type resolution.
    /// </summary>
    /// <param name="principal">
    /// The claims principal representing the authenticated user, or <c>null</c> for anonymous requests.
    /// </param>
    /// <param name="options">
    /// The security options that configure which claim types to extract.
    /// When <c>null</c>, default claim types are used.
    /// </param>
    public SecurityContext(ClaimsPrincipal? principal, SecurityOptions? options = null)
    {
        var opts = options ?? new SecurityOptions();

        User = principal;
        IsAuthenticated = principal?.Identity?.IsAuthenticated ?? false;

        if (principal is null)
        {
            UserId = null;
            TenantId = null;
            Roles = ImmutableHashSet<string>.Empty;
            Permissions = ImmutableHashSet<string>.Empty;
            return;
        }

        UserId = principal.FindFirst(opts.UserIdClaimType)?.Value
              ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        TenantId = principal.FindFirst(opts.TenantIdClaimType)?.Value;

        Roles = principal.FindAll(opts.RoleClaimType)
            .Concat(principal.FindAll(ClaimTypes.Role))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        Permissions = principal.FindAll(opts.PermissionClaimType)
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? User { get; }

    /// <inheritdoc />
    public string? UserId { get; }

    /// <inheritdoc />
    public string? TenantId { get; }

    /// <inheritdoc />
    public IReadOnlySet<string> Roles { get; }

    /// <inheritdoc />
    public IReadOnlySet<string> Permissions { get; }

    /// <inheritdoc />
    public bool IsAuthenticated { get; }

    /// <summary>
    /// Creates an anonymous (unauthenticated) security context.
    /// </summary>
    /// <returns>A security context with no identity or claims.</returns>
    public static SecurityContext Anonymous => new(principal: null);
}
