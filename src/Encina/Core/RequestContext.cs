using System.Collections.Immutable;
using System.Diagnostics;

namespace Encina;

/// <summary>
/// Default implementation of <see cref="IRequestContext"/>.
/// </summary>
/// <remarks>
/// Immutable by design - all <c>With*</c> methods return new instances.
/// Thread-safe for concurrent access.
/// </remarks>
public sealed class RequestContext : IRequestContext
{
    /// <inheritdoc />
    public string CorrelationId { get; init; } = string.Empty;

    /// <inheritdoc />
    public string? UserId { get; init; }

    /// <inheritdoc />
    public string? IdempotencyKey { get; init; }

    /// <inheritdoc />
    public string? TenantId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = ImmutableDictionary<string, object?>.Empty;

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
        UserId = source.UserId;
        IdempotencyKey = source.IdempotencyKey;
        TenantId = source.TenantId;
        Timestamp = source.Timestamp;
        Metadata = source.Metadata;
    }

    /// <summary>
    /// Creates a new context with auto-generated correlation ID.
    /// </summary>
    /// <returns>New context instance.</returns>
    /// <remarks>
    /// <para>
    /// Correlation ID is extracted from <see cref="Activity.Current"/> if available,
    /// otherwise a new GUID is generated.
    /// </para>
    /// <para>
    /// Timestamp is set to current UTC time.
    /// </para>
    /// </remarks>
    public static IRequestContext Create() => new RequestContext
    {
        CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N"),
        Timestamp = DateTimeOffset.UtcNow,
        Metadata = ImmutableDictionary<string, object?>.Empty
    };

    /// <summary>
    /// Creates a new context with specified correlation ID.
    /// </summary>
    /// <param name="correlationId">Correlation ID to use.</param>
    /// <returns>New context instance.</returns>
    /// <remarks>
    /// Useful when correlation ID is provided externally (e.g., from HTTP headers).
    /// </remarks>
    public static IRequestContext Create(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        return new RequestContext
        {
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = ImmutableDictionary<string, object?>.Empty
        };
    }

    /// <summary>
    /// Creates a test context with specified properties.
    /// </summary>
    /// <param name="userId">User ID (optional).</param>
    /// <param name="tenantId">Tenant ID (optional).</param>
    /// <param name="idempotencyKey">Idempotency key (optional).</param>
    /// <param name="correlationId">Correlation ID (optional, auto-generated if not provided).</param>
    /// <returns>New context instance.</returns>
    /// <remarks>
    /// Helper method for unit tests. Provides a fluent way to create contexts with specific values.
    /// </remarks>
    public static IRequestContext CreateForTest(
        string? userId = null,
        string? tenantId = null,
        string? idempotencyKey = null,
        string? correlationId = null) => new RequestContext
        {
            CorrelationId = correlationId ?? $"test-{Guid.NewGuid():N}",
            UserId = userId,
            TenantId = tenantId,
            IdempotencyKey = idempotencyKey,
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = ImmutableDictionary<string, object?>.Empty
        };

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
    public IRequestContext WithUserId(string? userId) =>
        new RequestContext(this) { UserId = userId };

    /// <inheritdoc />
    public IRequestContext WithIdempotencyKey(string? idempotencyKey) =>
        new RequestContext(this) { IdempotencyKey = idempotencyKey };

    /// <inheritdoc />
    public IRequestContext WithTenantId(string? tenantId) =>
        new RequestContext(this) { TenantId = tenantId };

    /// <inheritdoc />
    public override string ToString() =>
        $"RequestContext {{ CorrelationId = {CorrelationId}, UserId = {UserId ?? "(null)"}, TenantId = {TenantId ?? "(null)"}, IdempotencyKey = {IdempotencyKey ?? "(null)"} }}";
}
