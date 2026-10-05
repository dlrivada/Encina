using System.Security.Claims;

namespace Encina;

/// <summary>
/// The one claim map Encina uses to turn a <see cref="ClaimsPrincipal"/> into a <see cref="RequestIdentity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every list is ordered: the first claim type found wins. Only <b>authenticated</b>
/// <see cref="ClaimsIdentity"/> instances of a principal contribute the subject, roles, permissions
/// and tenant.
/// </para>
/// <para>
/// This is the only customisation point of the claim mapping; the mapper itself is internal and
/// cannot be replaced. Lists must not be empty and must not contain blank entries (validated at
/// startup).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaRequestIdentity(options =>
/// {
///     options.UserIdClaimTypes.Clear();
///     options.UserIdClaimTypes.Add("oid");
///     options.PermissionClaimTypes.Add("scope");
///     options.PermissionClaimSeparator = ' ';
/// });
/// </code>
/// </example>
public sealed class RequestIdentityOptions
{
    /// <summary>
    /// The Azure AD (Entra ID) object identifier claim type.
    /// </summary>
    public const string ObjectIdentifierClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    /// <summary>
    /// The Azure AD (Entra ID) tenant identifier claim type.
    /// </summary>
    public const string TenantIdentifierClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

    /// <summary>
    /// Gets the claim types that carry the user id, in precedence order.
    /// Default: <c>sub</c>, <see cref="ClaimTypes.NameIdentifier"/>, <see cref="ObjectIdentifierClaimType"/>.
    /// </summary>
    public IList<string> UserIdClaimTypes { get; } = ["sub", ClaimTypes.NameIdentifier, ObjectIdentifierClaimType];

    /// <summary>
    /// Gets the claim types that carry roles. Default: <c>role</c>, <see cref="ClaimTypes.Role"/>.
    /// </summary>
    public IList<string> RoleClaimTypes { get; } = ["role", ClaimTypes.Role];

    /// <summary>
    /// Gets or sets a value indicating whether each authenticated identity's own
    /// <see cref="ClaimsIdentity.RoleClaimType"/> also counts as a role claim type (as
    /// <see cref="ClaimsPrincipal.IsInRole"/> does). Default: <see langword="true"/>.
    /// </summary>
    public bool IncludeIdentityRoleClaimType { get; set; } = true;

    /// <summary>
    /// Gets the claim types that carry permissions. Default: <c>permission</c>.
    /// </summary>
    public IList<string> PermissionClaimTypes { get; } = ["permission"];

    /// <summary>
    /// Gets or sets the separator that splits one permission claim into several (for example
    /// <c>' '</c> for OAuth <c>scope</c> claims). Default: <see langword="null"/> (no split).
    /// </summary>
    public char? PermissionClaimSeparator { get; set; }

    /// <summary>
    /// Gets the claim types that carry the tenant id, in precedence order. Used only for
    /// authenticated principals. Default: <c>tenant_id</c>, <c>tid</c>, <see cref="TenantIdentifierClaimType"/>.
    /// </summary>
    public IList<string> TenantIdClaimTypes { get; } = ["tenant_id", "tid", TenantIdentifierClaimType];
}
