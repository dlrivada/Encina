using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina;

/// <summary>
/// The default <see cref="IRequestIdentityFactory"/>: maps the authenticated identities of a
/// principal with the claim map of <see cref="RequestIdentityOptions"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Only authenticated <see cref="ClaimsIdentity"/> instances contribute the subject, roles, permissions and tenant.</description></item>
/// <item><description>No authenticated identity: <see cref="RequestIdentity.Anonymous"/>.</description></item>
/// <item><description>No user-id claim: anonymous, Warning 162.</description></item>
/// <item><description>A reserved (<c>service:</c>) or malformed user id: anonymous, Warning 163.</description></item>
/// <item><description>Authenticated identities with different subjects or tenants: anonymous, Warning 164.</description></item>
/// </list>
/// Logs carry claim types and the authentication type only, never claim values.
/// </remarks>
internal sealed class ClaimsRequestIdentityFactory : IRequestIdentityFactory
{
    private const string SubjectClaimKind = "subject";
    private const string TenantClaimKind = "tenant";

    private readonly RequestIdentityOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimsRequestIdentityFactory"/> class.
    /// </summary>
    /// <param name="options">The claim map.</param>
    /// <param name="logger">The logger; a null logger when logging is not registered.</param>
    public ClaimsRequestIdentityFactory(
        IOptions<RequestIdentityOptions> options,
        ILogger<ClaimsRequestIdentityFactory>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _logger = logger ?? NullLogger<ClaimsRequestIdentityFactory>.Instance;
    }

    /// <inheritdoc />
    public RequestIdentity Create(ClaimsPrincipal? principal)
    {
        var identities = AuthenticatedIdentities(principal);
        if (identities.Count == 0)
        {
            return RequestIdentity.Anonymous;
        }

        var subject = ResolveSubject(identities);
        if (subject is null)
        {
            return RequestIdentity.Anonymous;
        }

        if (!TryResolveTenant(identities, out _))
        {
            RequestIdentityLog.ConflictingAuthenticatedIdentities(_logger, TenantClaimKind);
            return RequestIdentity.Anonymous;
        }

        return RequestIdentity.ForUser(subject, principal, CollectRoles(identities), CollectPermissions(identities));
    }

    /// <inheritdoc />
    public string? ResolveTenantId(ClaimsPrincipal? principal) =>
        TryResolveTenant(AuthenticatedIdentities(principal), out var tenantId) ? tenantId : null;

    private static List<ClaimsIdentity> AuthenticatedIdentities(ClaimsPrincipal? principal) =>
        principal is null
            ? []
            : [.. principal.Identities.Where(static identity => identity.IsAuthenticated)];

    private static Claim? FindFirst(ClaimsIdentity identity, IEnumerable<string> claimTypes) =>
        claimTypes
            .Select(claimType => identity.FindFirst(claimType))
            .FirstOrDefault(static claim => claim is not null && !string.IsNullOrWhiteSpace(claim.Value));

    private string? ResolveSubject(List<ClaimsIdentity> identities)
    {
        var claims = identities
            .Select(identity => FindFirst(identity, _options.UserIdClaimTypes))
            .OfType<Claim>()
            .ToList();

        if (claims.Count == 0)
        {
            RequestIdentityLog.AuthenticatedPrincipalWithoutSubject(
                _logger, identities[0].AuthenticationType, string.Join(", ", _options.UserIdClaimTypes));
            return null;
        }

        if (claims.Select(static claim => claim.Value).Distinct(StringComparer.Ordinal).Skip(1).Any())
        {
            RequestIdentityLog.ConflictingAuthenticatedIdentities(_logger, SubjectClaimKind);
            return null;
        }

        return CheckSubject(claims[0]);
    }

    private string? CheckSubject(Claim claim)
    {
        if (RequestIdentity.IsValidUserId(claim.Value))
        {
            return claim.Value;
        }

        RequestIdentityLog.ReservedServiceSubjectRejected(_logger, claim.Type);
        return null;
    }

    private bool TryResolveTenant(List<ClaimsIdentity> identities, out string? tenantId)
    {
        var tenants = identities
            .Select(identity => FindFirst(identity, _options.TenantIdClaimTypes)?.Value)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        tenantId = tenants.Count == 1 ? tenants[0] : null;
        return tenants.Count <= 1;
    }

    private IEnumerable<string> CollectRoles(List<ClaimsIdentity> identities) =>
        identities.SelectMany(identity => identity.Claims
            .Where(claim => IsRoleClaim(identity, claim.Type))
            .Select(static claim => claim.Value));

    private bool IsRoleClaim(ClaimsIdentity identity, string claimType) =>
        _options.RoleClaimTypes.Contains(claimType, StringComparer.OrdinalIgnoreCase)
        || (_options.IncludeIdentityRoleClaimType
            && string.Equals(identity.RoleClaimType, claimType, StringComparison.OrdinalIgnoreCase));

    private IEnumerable<string> CollectPermissions(List<ClaimsIdentity> identities) =>
        identities
            .SelectMany(static identity => identity.Claims)
            .Where(claim => _options.PermissionClaimTypes.Contains(claim.Type, StringComparer.OrdinalIgnoreCase))
            .SelectMany(claim => SplitPermission(claim.Value));

    private string[] SplitPermission(string value) =>
        _options.PermissionClaimSeparator is { } separator
            ? value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [value];
}
