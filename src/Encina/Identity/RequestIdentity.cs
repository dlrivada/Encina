using System.Buffers;
using System.Collections.Frozen;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace Encina;

/// <summary>
/// The caller identity of a request: who the request runs as, and what that caller holds.
/// </summary>
/// <remarks>
/// <para>
/// Encina keeps one identity per request, carried by <see cref="IRequestContext.Identity"/>. Every
/// gate (authorization, ABAC, security attributes, compliance) and every audit sink reads this same
/// instance, so the subject a gate authorized is the subject audit records.
/// </para>
/// <para>
/// <b>Invariant:</b> <see cref="UserId"/> is not <see langword="null"/> if and only if
/// <see cref="IsAuthenticated"/> is <see langword="true"/>. An unauthenticated principal is
/// <see cref="Anonymous"/> even when it carries a subject claim.
/// </para>
/// <para>
/// Application code cannot mint an authenticated identity: identities are created by Encina's
/// entry points (request middleware and identity scopes), each validated and logged. Tests build
/// identities through <c>Encina.Testing</c> (<c>TestIdentity</c>).
/// </para>
/// <para>
/// <see cref="ToString"/> prints the kind only; the user id, roles, permissions and claim values
/// never reach logs, activity tags or diagnostics.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public ValueTask&lt;Either&lt;EncinaError, TResponse&gt;&gt; Handle(
///     TRequest request, IRequestContext context, RequestHandlerCallback&lt;TResponse&gt; nextStep, CancellationToken ct)
/// {
///     if (context.Identity is not { IsAuthenticated: true } identity)
///     {
///         return ValueTask.FromResult&lt;Either&lt;EncinaError, TResponse&gt;&gt;(MyErrors.Unauthenticated());
///     }
///
///     return identity.Roles.Contains("admin") ? nextStep() : ValueTask.FromResult&lt;Either&lt;EncinaError, TResponse&gt;&gt;(MyErrors.Forbidden());
/// }
/// </code>
/// </example>
public sealed class RequestIdentity
{
    /// <summary>
    /// The prefix of every service subject (<c>service:&lt;name&gt;</c>). A user id can never start
    /// with it, so a service can never collide with an identity-provider user.
    /// </summary>
    public const string ServiceSubjectPrefix = "service:";

    /// <summary>
    /// The authentication type of the principal of a service identity.
    /// </summary>
    internal const string ServiceAuthenticationType = "encina-service";

    /// <summary>
    /// The claim type that marks the principal of a service identity (value <c>service</c>).
    /// </summary>
    internal const string IdentityKindClaimType = "encina:identity_kind";

    /// <summary>
    /// The authentication type given to the copy of an authenticated identity that reports none, so
    /// the copy still reads as authenticated.
    /// </summary>
    internal const string AuthenticatedFallbackType = "encina-authenticated";

    private static readonly FrozenSet<string> EmptySet = Array.Empty<string>().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    // The principal built at construction from the caller's authenticated identities (plain
    // ClaimsIdentity copies). Readers get a clone of it, so nobody can change what the gates saw.
    private readonly ClaimsPrincipal? _principal;

    // The claims of the stored principal, frozen at creation like the roles and permissions.
    private readonly (string Type, string Value)[] _claims;

    // The claims that count for IsSameAs: _claims minus this identity's own per-token claim types.
    private readonly (string Type, string Value)[] _comparableClaims;

    private readonly FrozenSet<string> _perTokenClaimTypes;
    private readonly FrozenSet<string> _roles;
    private readonly FrozenSet<string> _permissions;

    private RequestIdentity(
        IdentityKind kind,
        string? userId,
        ClaimsPrincipal? principal,
        FrozenSet<string> roles,
        FrozenSet<string> permissions,
        IdentityIssuer? issuer = null,
        IReadOnlySet<string>? perTokenClaimTypes = null)
    {
        Kind = kind;
        UserId = userId;
        Issuer = issuer;
        _principal = CopyAuthenticated(principal);
        _roles = roles;
        _permissions = permissions;
        _claims = FreezeClaims(_principal);
        _perTokenClaimTypes = ToComparisonSet(perTokenClaimTypes);
        _comparableClaims = Without(_claims, _perTokenClaimTypes);
    }

    /// <summary>
    /// Gets the cached anonymous identity: no user id, no principal, no roles, no permissions.
    /// </summary>
    public static RequestIdentity Anonymous { get; } = new(IdentityKind.Anonymous, null, null, EmptySet, EmptySet);

    /// <summary>
    /// Gets the kind of caller.
    /// </summary>
    public IdentityKind Kind { get; }

    /// <summary>
    /// Gets the caller's user id: the identity-provider subject for a user, <c>service:&lt;name&gt;</c>
    /// for a service, and <see langword="null"/> for <see cref="Anonymous"/>.
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Gets a value indicating whether the caller is authenticated (any kind other than
    /// <see cref="IdentityKind.Anonymous"/>).
    /// </summary>
    public bool IsAuthenticated => Kind != IdentityKind.Anonymous;

    /// <summary>
    /// Gets a copy of the principal the identity was mapped from, or <see langword="null"/> when
    /// there is none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every read returns a new clone, so code that adds identities or claims to it never changes
    /// what another gate evaluates. The principal holds only the caller's <b>authenticated</b>
    /// identities, each copied as a plain <see cref="ClaimsIdentity"/> (claims, authentication
    /// type, name and role claim types; never the actor or the bootstrap context). An
    /// authenticated identity that reports no authentication type is copied with the type
    /// <c>encina-authenticated</c>, so it stays authenticated.
    /// </para>
    /// <para>
    /// Because the copies are plain identities, role checks that a specialised principal answers
    /// from outside its claims (for example Windows group names answered by a
    /// <c>WindowsPrincipal</c> from its token) do not match on this principal; only role claims do.
    /// Map such groups to role claims before the identity is built.
    /// </para>
    /// </remarks>
    public ClaimsPrincipal? Principal =>
        _principal is null ? null : new ClaimsPrincipal(_principal.Identities.Select(static identity => identity.Clone()));

    /// <summary>
    /// Gets the scope that issued this identity, or <see langword="null"/> for an identity built
    /// outside the scope factory (<see cref="Anonymous"/>, test builders). An identity whose issuer
    /// has ended no longer reads as a caller.
    /// </summary>
    internal IdentityIssuer? Issuer { get; }

    /// <summary>
    /// Gets the caller's roles (case-insensitive).
    /// </summary>
    public IReadOnlySet<string> Roles => _roles;

    /// <summary>
    /// Gets the caller's permissions (case-insensitive).
    /// </summary>
    public IReadOnlySet<string> Permissions => _permissions;

    /// <summary>
    /// Determines whether an authenticated <see cref="ClaimsIdentity"/> of <see cref="Principal"/>
    /// carried a claim of <paramref name="type"/> (and, when given, of <paramref name="value"/>) when
    /// this identity was created.
    /// </summary>
    /// <param name="type">The claim type (compared case-insensitively).</param>
    /// <param name="value">The claim value (compared ordinally), or <see langword="null"/> for any value.</param>
    /// <returns><see langword="true"/> when an authenticated identity carries the claim.</returns>
    /// <remarks>
    /// The claims are frozen when the identity is created, like <see cref="Roles"/> and
    /// <see cref="Permissions"/>: adding or removing claims on <see cref="Principal"/> afterwards does
    /// not change the answer. Unauthenticated <see cref="ClaimsIdentity"/> instances (for example ones
    /// added by claims transformation) never count.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="type"/> is null, empty or whitespace.</exception>
    public bool HasClaim(string type, string? value = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        return Array.Exists(_claims, claim =>
            string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase)
            && (value is null || string.Equals(claim.Value, value, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Produces the persisted form of this identity for a deferred message (SPEC-002 REQ-015).
    /// </summary>
    /// <param name="tenantId">The tenant the message belongs to, if any.</param>
    /// <param name="correlationId">The correlation id of the originating request.</param>
    /// <param name="causationId">The id of the message or request that caused this one, if any.</param>
    /// <returns>The persisted identity: kind, actor id, tenant, correlation and causation ids. Roles and permissions are never persisted.</returns>
    /// <exception cref="ArgumentException"><paramref name="correlationId"/> is null, empty or whitespace.</exception>
    public PersistedRequestIdentity ToPersisted(string? tenantId, string correlationId, string? causationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        return new PersistedRequestIdentity(Kind, ActorId(), tenantId, correlationId, causationId);
    }

    /// <summary>
    /// Returns the identity kind only; the user id, roles and claims are never printed.
    /// </summary>
    /// <returns>A string such as <c>RequestIdentity { Kind = User }</c>.</returns>
    public override string ToString() => $"RequestIdentity {{ Kind = {Kind} }}";

    /// <summary>
    /// Creates a user identity. Internal: identities are minted only by Encina's entry points and
    /// the declared test seam.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="userId"/> breaks the user-id rule (see <see cref="IsValidUserId"/>).</exception>
    /// <param name="userId">The user id.</param>
    /// <param name="principal">The principal the identity is mapped from; only its authenticated identities are kept.</param>
    /// <param name="roles">The roles.</param>
    /// <param name="permissions">The permissions.</param>
    /// <param name="issuer">The issuing scope (scope factory only); <see langword="null"/> for builders.</param>
    /// <param name="perTokenClaimTypes">The claim types <see cref="IsSameAs"/> ignores; <see langword="null"/> means <see cref="RequestIdentityOptions.DefaultPerTokenClaimTypes"/>.</param>
    internal static RequestIdentity ForUser(
        string userId,
        ClaimsPrincipal? principal = null,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null,
        IdentityIssuer? issuer = null,
        IReadOnlySet<string>? perTokenClaimTypes = null)
    {
        ArgumentNullException.ThrowIfNull(userId);
        if (!IsValidUserId(userId))
        {
            throw new ArgumentException(
                "A user id must not be blank, must have no leading or trailing whitespace, no control or format characters, and must not start with the reserved 'service:' prefix.",
                nameof(userId));
        }

        return new RequestIdentity(IdentityKind.User, userId, principal, ToSet(roles), ToSet(permissions), issuer, perTokenClaimTypes);
    }

    /// <summary>
    /// Creates a service identity from a declaration. Internal: the scope factory calls it once per
    /// scope (with an issuer), and the test builders call it without one.
    /// </summary>
    /// <param name="definition">The declared service identity.</param>
    /// <param name="issuer">The issuing scope; <see langword="null"/> for builders.</param>
    /// <param name="perTokenClaimTypes">The claim types <see cref="IsSameAs"/> ignores; <see langword="null"/> means <see cref="RequestIdentityOptions.DefaultPerTokenClaimTypes"/>.</param>
    /// <param name="roleClaimType">The claim type of the declared roles on the principal.</param>
    /// <param name="permissionClaimType">The claim type of the declared permissions on the principal.</param>
    /// <returns>
    /// A <see cref="IdentityKind.Service"/> identity with user id <c>service:&lt;name&gt;</c> and a
    /// principal of authentication type <c>encina-service</c> carrying <c>sub</c>, the identity-kind
    /// claim, the declared roles, permissions and claims.
    /// </returns>
    internal static RequestIdentity ForService(
        ServiceIdentityDefinition definition,
        IdentityIssuer? issuer = null,
        IReadOnlySet<string>? perTokenClaimTypes = null,
        string roleClaimType = "role",
        string permissionClaimType = "permission")
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleClaimType);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionClaimType);

        var subject = ServiceSubjectPrefix + definition.Name;
        List<Claim> claims =
        [
            new Claim("sub", subject),
            new Claim(IdentityKindClaimType, "service")
        ];
        claims.AddRange(definition.Roles.Select(role => new Claim(roleClaimType, role)));
        claims.AddRange(definition.Permissions.Select(permission => new Claim(permissionClaimType, permission)));
        claims.AddRange(definition.Claims.Select(static claim => new Claim(claim.Key, claim.Value)));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, ServiceAuthenticationType, ClaimsIdentity.DefaultNameClaimType, roleClaimType));

        return new RequestIdentity(
            IdentityKind.Service,
            subject,
            principal,
            ToSet(definition.Roles),
            ToSet(definition.Permissions),
            issuer,
            perTokenClaimTypes);
    }

    /// <summary>
    /// Determines whether <paramref name="other"/> is the same caller holding the same authority:
    /// same kind, same user id (ordinal), the same roles and permissions, and the same authenticated
    /// claims once the per-token claim types of <b>both</b> identities are removed.
    /// </summary>
    /// <remarks>
    /// Claim types compare case-insensitively and values ordinally, as <see cref="HasClaim"/> does.
    /// The issuer is ignored (it decides staleness, not sameness), and so is the tenant (tenant is
    /// not identity).
    /// </remarks>
    internal bool IsSameAs(RequestIdentity other) =>
        Kind == other.Kind
        && string.Equals(UserId, other.UserId, StringComparison.Ordinal)
        && _roles.SetEquals(other._roles)
        && _permissions.SetEquals(other._permissions)
        && HasSameClaims(other);

    private bool HasSameClaims(RequestIdentity other) =>
        Without(_comparableClaims, other._perTokenClaimTypes).ToHashSet(ClaimComparer.Instance)
            .SetEquals(Without(other._comparableClaims, _perTokenClaimTypes));

    /// <summary>
    /// The user-id rule: not blank, no leading or trailing whitespace, no control characters, no
    /// invisible format characters (such as U+200B or U+202E), and not starting (case-insensitively,
    /// after trimming) with <see cref="ServiceSubjectPrefix"/>.
    /// </summary>
    internal static bool IsValidUserId(string? userId) =>
        !string.IsNullOrWhiteSpace(userId)
        && string.Equals(userId, userId.Trim(), StringComparison.Ordinal)
        && !HasInvisibleOrInvalidCharacter(userId)
        && !IsReservedSubject(userId);

    /// <summary>
    /// Determines whether <paramref name="subject"/>, trimmed, starts with the reserved service prefix.
    /// </summary>
    internal static bool IsReservedSubject(string subject) =>
        subject.Trim().StartsWith(ServiceSubjectPrefix, StringComparison.OrdinalIgnoreCase);

    private static FrozenSet<string> ToSet(IEnumerable<string>? values) =>
        values is null
            ? EmptySet
            : values.Where(static value => !string.IsNullOrWhiteSpace(value)).ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    // Walks Unicode scalar values, not UTF-16 chars, so a supplementary-plane format character (such
    // as the U+E0001-U+E007F tags) is caught; a lone surrogate is malformed and rejected too.
    private static bool HasInvisibleOrInvalidCharacter(string userId)
    {
        var remaining = userId.AsSpan();
        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(remaining, out var rune, out var consumed) != OperationStatus.Done
                || Rune.GetUnicodeCategory(rune) is UnicodeCategory.Control or UnicodeCategory.Format)
            {
                return true;
            }

            remaining = remaining[consumed..];
        }

        return false;
    }

    private static (string Type, string Value)[] FreezeClaims(ClaimsPrincipal? principal) =>
        principal is null
            ? []
            : [.. principal.Identities
                .Where(static identity => identity.IsAuthenticated)
                .SelectMany(static identity => identity.Claims)
                .Select(static claim => (claim.Type, claim.Value))];

    // The stored principal keeps only the authenticated identities, each as a plain ClaimsIdentity:
    // never identity.Clone(), which would duplicate a WindowsIdentity token handle on every read, and
    // never the actor or the bootstrap context (which can hold the raw token).
    private static ClaimsPrincipal? CopyAuthenticated(ClaimsPrincipal? principal)
    {
        var copies = principal?.Identities
            .Where(static identity => identity.IsAuthenticated)
            .Select(static identity => new ClaimsIdentity(
                identity.Claims.Select(static claim => new Claim(claim.Type, claim.Value, claim.ValueType, claim.Issuer, claim.OriginalIssuer)),
                string.IsNullOrEmpty(identity.AuthenticationType) ? AuthenticatedFallbackType : identity.AuthenticationType,
                identity.NameClaimType,
                identity.RoleClaimType))
            .ToList();

        return copies is { Count: > 0 } ? new ClaimsPrincipal(copies) : null;
    }

    private static FrozenSet<string> ToComparisonSet(IReadOnlySet<string>? perTokenClaimTypes) =>
        perTokenClaimTypes switch
        {
            null => (FrozenSet<string>)RequestIdentityOptions.DefaultPerTokenClaimTypes,
            FrozenSet<string> frozen when ReferenceEquals(frozen.Comparer, StringComparer.OrdinalIgnoreCase) => frozen,
            _ => perTokenClaimTypes.ToFrozenSet(StringComparer.OrdinalIgnoreCase)
        };

    private static (string Type, string Value)[] Without((string Type, string Value)[] claims, FrozenSet<string> claimTypes) =>
        claimTypes.Count == 0
            ? claims
            : Array.FindAll(claims, claim => !claimTypes.Contains(claim.Type));

    // Claim type case-insensitive, value ordinal: the comparison HasClaim uses.
    private sealed class ClaimComparer : IEqualityComparer<(string Type, string Value)>
    {
        internal static readonly ClaimComparer Instance = new();

        public bool Equals((string Type, string Value) x, (string Type, string Value) y) =>
            string.Equals(x.Type, y.Type, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Value, y.Value, StringComparison.Ordinal);

        public int GetHashCode((string Type, string Value) obj) =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Type), StringComparer.Ordinal.GetHashCode(obj.Value));
    }

    // crap-exempt: single-question switch — the actor id of each identity kind.
    private string? ActorId() => Kind switch
    {
        IdentityKind.User => UserId,
        IdentityKind.Service => UserId?[ServiceSubjectPrefix.Length..],
        _ => null
    };
}
