using System.Collections.Frozen;
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
/// <item><description>No user-id claim on any authenticated identity: <see cref="CheckSubject"/> refuses it with
/// <see cref="RequestIdentityErrorCodes.MissingSubjectClaim"/> and logs Warning 162.</description></item>
/// <item><description>A malformed user id or a reserved (<c>service:</c>) one: refused with
/// <see cref="RequestIdentityErrorCodes.MalformedSubjectClaim"/> or
/// <see cref="RequestIdentityErrorCodes.ReservedSubjectClaim"/>; Warning 163.</description></item>
/// <item><description>Authenticated identities with different subjects, or several values of the winning claim type
/// in one identity: refused with <see cref="RequestIdentityErrorCodes.ConflictingSubjectClaims"/>; Warning 164.
/// Per identity the first claim type of the list with a non-blank value wins, so <c>sub</c> and <c>oid</c> of
/// one identity never conflict.</description></item>
/// <item><description><see cref="Create"/> keeps <see cref="RequestIdentity.Anonymous"/> as a last-resort floor for
/// these cases and for conflicting tenants (Warning 164); the scope factory refuses before it gets there.</description></item>
/// </list>
/// Logs carry claim types and the authentication type only, never claim values.
/// </remarks>
internal sealed class ClaimsRequestIdentityFactory : IRequestIdentityFactory
{
    private const string SubjectClaimKind = "subject";
    private const string TenantClaimKind = "tenant";

    private readonly RequestIdentityOptions _options;
    private readonly FrozenSet<string> _perTokenClaimTypes;
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
        _perTokenClaimTypes = _options.PerTokenClaimTypes
            .Where(static claimType => !string.IsNullOrWhiteSpace(claimType))
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _logger = logger ?? NullLogger<ClaimsRequestIdentityFactory>.Instance;
    }

    /// <summary>
    /// Gets the configured per-token claim types, frozen once.
    /// </summary>
    internal FrozenSet<string> PerTokenClaimTypes => _perTokenClaimTypes;

    /// <inheritdoc />
    public RequestIdentity Create(ClaimsPrincipal? principal, IdentityIssuer? issuer = null)
    {
        var identities = AuthenticatedIdentities(principal);
        if (identities.Count == 0)
        {
            return RequestIdentity.Anonymous;
        }

        // The floor for a caller that skipped CheckSubject; the scope factory never reaches it, so the
        // subject warnings are logged once, by CheckSubject.
        var resolution = ResolveSubject(identities);
        if (resolution.Subject is not { } subject)
        {
            return RequestIdentity.Anonymous;
        }

        if (!TryResolveTenant(identities, out _))
        {
            RequestIdentityLog.ConflictingAuthenticatedIdentities(_logger, TenantClaimKind);
            return RequestIdentity.Anonymous;
        }

        return RequestIdentity.ForUser(
            subject, principal, CollectRoles(identities), CollectPermissions(identities), issuer, _perTokenClaimTypes);
    }

    /// <inheritdoc />
    public EncinaError? CheckSubject(ClaimsPrincipal? principal)
    {
        var identities = AuthenticatedIdentities(principal);
        if (identities.Count == 0)
        {
            return null;
        }

        var resolution = ResolveSubject(identities);
        if (resolution.Outcome == SubjectOutcome.Resolved)
        {
            return null;
        }

        LogSubjectRefusal(resolution, identities[0].AuthenticationType);
        return RefusalOf(resolution.Outcome);
    }

    /// <inheritdoc />
    public bool TryResolveTenantId(ClaimsPrincipal? principal, out string? tenantId) =>
        TryResolveTenant(AuthenticatedIdentities(principal), out tenantId);

    private static List<ClaimsIdentity> AuthenticatedIdentities(ClaimsPrincipal? principal) =>
        principal is null
            ? []
            : [.. principal.Identities.Where(static identity => identity.IsAuthenticated)];

    // The first user-id claim type, in priority order, that has a non-blank value wins per identity
    // (so sub and oid in one identity are not a conflict); several distinct values of that winning
    // type inside one identity are a conflict, as are different values across identities.
    private SubjectResolution ResolveSubject(List<ClaimsIdentity> identities)
    {
        var winners = identities
            .Select(WinningClaims)
            .Where(static claims => claims.Count > 0)
            .ToList();

        if (winners.Count == 0)
        {
            return new SubjectResolution(SubjectOutcome.Missing, null, null);
        }

        var firstClaim = winners[0][0];
        var distinct = winners
            .SelectMany(static claims => claims)
            .Select(static claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return distinct > 1
            ? new SubjectResolution(SubjectOutcome.Conflicting, null, firstClaim.Type)
            : ClassifySubject(firstClaim);
    }

    private List<Claim> WinningClaims(ClaimsIdentity identity)
    {
        foreach (var claimType in _options.UserIdClaimTypes)
        {
            var claims = identity.FindAll(claimType).Where(static claim => !string.IsNullOrWhiteSpace(claim.Value)).ToList();
            if (claims.Count > 0)
            {
                return claims;
            }
        }

        return [];
    }

    private static SubjectResolution ClassifySubject(Claim claim)
    {
        if (RequestIdentity.IsValidUserId(claim.Value))
        {
            return new SubjectResolution(SubjectOutcome.Resolved, claim.Value, claim.Type);
        }

        var outcome = RequestIdentity.IsReservedSubject(claim.Value) ? SubjectOutcome.Reserved : SubjectOutcome.Malformed;
        return new SubjectResolution(outcome, null, claim.Type);
    }

    private void LogSubjectRefusal(SubjectResolution resolution, string? authenticationType)
    {
        if (resolution.Outcome == SubjectOutcome.Missing)
        {
            RequestIdentityLog.AuthenticatedPrincipalWithoutSubject(
                _logger, authenticationType, string.Join(", ", _options.UserIdClaimTypes));
        }
        else if (resolution.Outcome == SubjectOutcome.Conflicting)
        {
            RequestIdentityLog.ConflictingAuthenticatedIdentities(_logger, SubjectClaimKind);
        }
        else
        {
            RequestIdentityLog.ReservedServiceSubjectRejected(_logger, resolution.ClaimType ?? string.Empty);
        }
    }

    // crap-exempt: single-question switch — maps the subject outcome to its typed refusal
    private static EncinaError RefusalOf(SubjectOutcome outcome) => outcome switch
    {
        SubjectOutcome.Missing => RequestIdentityErrors.MissingSubjectClaim(),
        SubjectOutcome.Reserved => RequestIdentityErrors.ReservedSubjectClaim(),
        SubjectOutcome.Conflicting => RequestIdentityErrors.ConflictingSubjectClaims(),
        _ => RequestIdentityErrors.MalformedSubjectClaim()
    };

    private enum SubjectOutcome
    {
        Resolved,
        Missing,
        Malformed,
        Reserved,
        Conflicting
    }

    private readonly record struct SubjectResolution(SubjectOutcome Outcome, string? Subject, string? ClaimType);

    private bool TryResolveTenant(List<ClaimsIdentity> identities, out string? tenantId)
    {
        // Every tenant claim of every configured type counts: one identity carrying two different
        // tenant values is as ambiguous as two identities doing so.
        var tenants = identities
            .SelectMany(identity => _options.TenantIdClaimTypes.SelectMany(identity.FindAll))
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
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
