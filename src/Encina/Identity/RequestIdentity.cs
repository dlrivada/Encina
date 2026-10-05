using System.Collections.Frozen;
using System.Security.Claims;

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

    private static readonly FrozenSet<string> EmptySet = Array.Empty<string>().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private RequestIdentity(
        IdentityKind kind,
        string? userId,
        ClaimsPrincipal? principal,
        FrozenSet<string> roles,
        FrozenSet<string> permissions)
    {
        Kind = kind;
        UserId = userId;
        Principal = principal;
        Roles = roles;
        Permissions = permissions;
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
    /// Gets the principal the identity was mapped from, or <see langword="null"/> when there is none.
    /// </summary>
    public ClaimsPrincipal? Principal { get; }

    /// <summary>
    /// Gets the caller's roles (case-insensitive).
    /// </summary>
    public IReadOnlySet<string> Roles { get; }

    /// <summary>
    /// Gets the caller's permissions (case-insensitive).
    /// </summary>
    public IReadOnlySet<string> Permissions { get; }

    /// <summary>
    /// Determines whether an authenticated <see cref="ClaimsIdentity"/> of <see cref="Principal"/>
    /// carries a claim of <paramref name="type"/> (and, when given, of <paramref name="value"/>).
    /// </summary>
    /// <param name="type">The claim type (compared case-insensitively).</param>
    /// <param name="value">The claim value (compared ordinally), or <see langword="null"/> for any value.</param>
    /// <returns><see langword="true"/> when an authenticated identity carries the claim.</returns>
    /// <remarks>
    /// Unauthenticated <see cref="ClaimsIdentity"/> instances (for example ones added by claims
    /// transformation) never count.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="type"/> is null, empty or whitespace.</exception>
    public bool HasClaim(string type, string? value = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        return Principal is not null
            && Principal.Identities.Any(identity => identity.IsAuthenticated && HasMatchingClaim(identity, type, value));
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
    internal static RequestIdentity ForUser(
        string userId,
        ClaimsPrincipal? principal = null,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null)
    {
        ArgumentNullException.ThrowIfNull(userId);
        if (!IsValidUserId(userId))
        {
            throw new ArgumentException(
                "A user id must not be blank, must have no leading or trailing whitespace or control characters, and must not start with the reserved 'service:' prefix.",
                nameof(userId));
        }

        return new RequestIdentity(IdentityKind.User, userId, principal, ToSet(roles), ToSet(permissions));
    }

    /// <summary>
    /// The user-id rule: not blank, no leading or trailing whitespace, no control characters, and not
    /// starting (case-insensitively, after trimming) with <see cref="ServiceSubjectPrefix"/>.
    /// </summary>
    internal static bool IsValidUserId(string? userId) =>
        !string.IsNullOrWhiteSpace(userId)
        && string.Equals(userId, userId.Trim(), StringComparison.Ordinal)
        && !userId.Any(char.IsControl)
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

    private static bool HasMatchingClaim(ClaimsIdentity identity, string type, string? value) =>
        identity.Claims.Any(claim =>
            string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase)
            && (value is null || string.Equals(claim.Value, value, StringComparison.Ordinal)));

    // crap-exempt: single-question switch — the actor id of each identity kind.
    private string? ActorId() => Kind switch
    {
        IdentityKind.User => UserId,
        IdentityKind.Service => UserId?[ServiceSubjectPrefix.Length..],
        _ => null
    };
}
