using System.Collections.Immutable;
using System.Diagnostics;

namespace Encina;

/// <summary>
/// Default implementation of <see cref="IRequestContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// Immutable by design - all <c>With*</c> methods return new instances.
/// Thread-safe for concurrent access.
/// </para>
/// <para>
/// The public factories create <b>anonymous</b> contexts only. An authenticated
/// <see cref="RequestIdentity"/> is set by Encina's identity entry points (request middleware and
/// identity scopes); tests build authenticated contexts with <c>Encina.Testing</c>
/// (<c>TestRequestContext.For(TestIdentity.User(...))</c>).
/// </para>
/// </remarks>
public sealed class RequestContext : IRequestContext
{
    /// <inheritdoc />
    public string CorrelationId { get; init; } = string.Empty;

    /// <inheritdoc />
    public string? CausationId { get; private init; }

    /// <inheritdoc />
    public RequestIdentity Identity { get; private init; } = RequestIdentity.Anonymous;

    /// <inheritdoc />
    public string? IdempotencyKey { get; init; }

    /// <inheritdoc />
    public string? TenantId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = ImmutableDictionary<string, object?>.Empty;

    /// <summary>
    /// Gets where the context was opened (inbound request, identity scope, restored message).
    /// </summary>
    internal RequestOrigin Origin { get; private init; }

    /// <summary>
    /// Private constructor for internal use.
    /// </summary>
    private RequestContext()
    {
    }

    /// <summary>
    /// Copy constructor for immutable With* methods.
    /// </summary>
    private RequestContext(RequestContext source)
    {
        CorrelationId = source.CorrelationId;
        CausationId = source.CausationId;
        Identity = source.Identity;
        IdempotencyKey = source.IdempotencyKey;
        TenantId = source.TenantId;
        Timestamp = source.Timestamp;
        Metadata = source.Metadata;
        Origin = source.Origin;
    }

    /// <summary>
    /// Creates a new anonymous context with an auto-generated correlation ID.
    /// </summary>
    /// <returns>New context instance.</returns>
    /// <remarks>
    /// <para>
    /// Correlation ID is extracted from <see cref="Activity.Current"/> if available,
    /// otherwise a new GUID is generated.
    /// </para>
    /// <para>
    /// Timestamp is read from <see cref="TimeProvider.System"/>; use
    /// <see cref="CreateAnonymousAt"/> with an injected <see cref="TimeProvider"/> in production code.
    /// </para>
    /// </remarks>
    public static IRequestContext Create() => CreateAnonymousAt(TimeProvider.System.GetUtcNow(), NewCorrelationId());

    /// <summary>
    /// Creates a new anonymous context stamped with <paramref name="timestamp"/>.
    /// </summary>
    /// <param name="timestamp">When the request started, read from an injected <see cref="TimeProvider"/>.</param>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="tenantId">The tenant id, if any.</param>
    /// <param name="idempotencyKey">The idempotency key, if any.</param>
    /// <returns>A context with <see cref="RequestIdentity.Anonymous"/>.</returns>
    /// <remarks>
    /// The production factory to use with an injected <see cref="TimeProvider"/> (<see cref="Create"/>
    /// reads <see cref="TimeProvider.System"/>). Like every public factory, it never creates an
    /// authenticated identity.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="correlationId"/> is null, empty or whitespace.</exception>
    /// <example>
    /// <code>
    /// var context = RequestContext.CreateAnonymousAt(timeProvider.GetUtcNow(), correlationId, tenantId);
    /// </code>
    /// </example>
    public static IRequestContext CreateAnonymousAt(
        DateTimeOffset timestamp,
        string correlationId,
        string? tenantId = null,
        string? idempotencyKey = null) =>
        CreateAt(timestamp, correlationId, RequestIdentity.Anonymous, tenantId, idempotencyKey);

    /// <summary>
    /// Creates a context carrying <paramref name="identity"/>. Internal: only identity entry points
    /// and the declared test seam call it.
    /// </summary>
    internal static RequestContext CreateAt(
        DateTimeOffset timestamp,
        string correlationId,
        RequestIdentity identity,
        string? tenantId = null,
        string? idempotencyKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(identity);

        return new RequestContext
        {
            CorrelationId = correlationId,
            Identity = identity,
            TenantId = tenantId,
            IdempotencyKey = idempotencyKey,
            Timestamp = timestamp,
            Metadata = ImmutableDictionary<string, object?>.Empty
        };
    }

    /// <summary>
    /// The correlation id of a context created without one: <see cref="Activity.Current"/>'s id or a new GUID.
    /// </summary>
    internal static string NewCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString("N");

    /// <summary>
    /// Creates the context of a dispatch nested inside another one.
    /// </summary>
    /// <param name="parent">The context of the outer dispatch.</param>
    /// <param name="timestamp">When the nested dispatch starts.</param>
    /// <returns>
    /// A context with the parent's correlation id, causation id, identity, tenant id, origin and
    /// metadata, stamped with <paramref name="timestamp"/>, without an idempotency key, and marked as
    /// nested (see <see cref="RequestContextDispatchExtensions.IsNestedDispatch"/>).
    /// </returns>
    /// <remarks>
    /// The idempotency key identifies the entry point's logical request. Handing it to a nested
    /// request would make the idempotency stores treat the nested request as a duplicate of (or as
    /// in progress with) the outer one.
    /// </remarks>
    internal static IRequestContext ForNestedDispatch(IRequestContext parent, DateTimeOffset timestamp)
    {
        var source = CopyOf(parent);
        var metadata = source.Metadata as ImmutableDictionary<string, object?>
            ?? source.Metadata.ToImmutableDictionary();

        return new RequestContext(source)
        {
            IdempotencyKey = null,
            Timestamp = timestamp,
            Metadata = metadata.SetItem(RequestContextDispatchExtensions.NestedDispatchKey, true)
        };
    }

    /// <summary>
    /// Returns <paramref name="source"/> itself when it is a <see cref="RequestContext"/>, otherwise a
    /// <see cref="RequestContext"/> with its values (a <c>null</c> identity becomes anonymous).
    /// </summary>
    internal static RequestContext CopyOf(IRequestContext source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source as RequestContext ?? new RequestContext
        {
            CorrelationId = source.CorrelationId,
            CausationId = source.CausationId,
            Identity = source.Identity ?? RequestIdentity.Anonymous,
            IdempotencyKey = source.IdempotencyKey,
            TenantId = source.TenantId,
            Timestamp = source.Timestamp,
            Metadata = source.Metadata.ToImmutableDictionary()
        };
    }

    /// <summary>
    /// Creates an anonymous test context with specified properties.
    /// </summary>
    /// <param name="tenantId">Tenant ID (optional).</param>
    /// <param name="idempotencyKey">Idempotency key (optional).</param>
    /// <param name="correlationId">Correlation ID (optional, auto-generated if not provided).</param>
    /// <returns>New anonymous context instance.</returns>
    /// <remarks>
    /// Helper method for unit tests. It never carries an authenticated identity; build authenticated
    /// test contexts with <c>Encina.Testing</c> (<c>TestRequestContext.For(TestIdentity.User(...))</c>).
    /// </remarks>
    public static IRequestContext CreateForTest(
        string? tenantId = null,
        string? idempotencyKey = null,
        string? correlationId = null) =>
        CreateAnonymousAt(TimeProvider.System.GetUtcNow(), correlationId ?? $"test-{Guid.NewGuid():N}", tenantId, idempotencyKey);

    /// <inheritdoc />
    public IRequestContext WithMetadata(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var newMetadata = Metadata is ImmutableDictionary<string, object?> immutable
            ? immutable.SetItem(key, value)
            : Metadata.ToImmutableDictionary().SetItem(key, value);

        return new RequestContext(this) { Metadata = newMetadata };
    }

    /// <inheritdoc />
    public IRequestContext WithIdempotencyKey(string? idempotencyKey) =>
        new RequestContext(this) { IdempotencyKey = idempotencyKey };

    /// <inheritdoc />
    public IRequestContext WithTenantId(string? tenantId) =>
        new RequestContext(this) { TenantId = tenantId };

    /// <summary>
    /// Creates a copy carrying <paramref name="identity"/>. Internal: identities are set only by
    /// Encina's identity entry points and the declared test seam.
    /// </summary>
    internal RequestContext WithIdentity(RequestIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return new RequestContext(this) { Identity = identity };
    }

    /// <summary>
    /// Creates a copy carrying <paramref name="causationId"/>.
    /// </summary>
    internal RequestContext WithCausationId(string? causationId) =>
        new(this) { CausationId = causationId };

    /// <summary>
    /// Creates a copy marked with <paramref name="origin"/>.
    /// </summary>
    internal RequestContext WithOrigin(RequestOrigin origin) =>
        new(this) { Origin = origin };

    /// <summary>
    /// Returns the correlation id, identity kind, tenant id and idempotency key. The user id is never printed.
    /// </summary>
    /// <returns>A diagnostic string.</returns>
    public override string ToString() =>
        $"RequestContext {{ CorrelationId = {CorrelationId}, IdentityKind = {Identity.Kind}, TenantId = {TenantId ?? "(null)"}, IdempotencyKey = {IdempotencyKey ?? "(null)"} }}";
}
