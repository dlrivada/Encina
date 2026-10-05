using System.Collections.Frozen;
using Encina.Testing.Identity;

namespace Encina.TestInfrastructure.PropertyTests;

/// <summary>
/// A simple test implementation of <see cref="IRequestContext"/> for use in tests.
/// </summary>
/// <remarks>
/// <para>
/// This record provides an immutable implementation with default values,
/// suitable for unit and property-based tests that need an IRequestContext.
/// </para>
/// <para>
/// The With* methods return new instances with the specified value changed,
/// preserving immutability and thread-safety.
/// </para>
/// </remarks>
public sealed record TestRequestContext : IRequestContext
{
    /// <summary>
    /// A fixed timestamp used for deterministic test behavior.
    /// Represents 2020-01-01T00:00:00Z.
    /// </summary>
    private static readonly DateTimeOffset FixedTimestamp = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public string CorrelationId => "test-correlation";

    /// <inheritdoc />
    public RequestIdentity Identity { get; init; } = RequestIdentity.Anonymous;

    /// <inheritdoc />
    public string? IdempotencyKey { get; init; }

    /// <inheritdoc />
    public string? TenantId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset Timestamp => FixedTimestamp;

    /// <inheritdoc />
    public string? CausationId { get; init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = FrozenDictionary<string, object?>.Empty;

    /// <inheritdoc />
    public IRequestContext WithMetadata(string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        var newMetadata = new Dictionary<string, object?>(Metadata) { [key] = value }.ToFrozenDictionary();
        return this with { Metadata = newMetadata };
    }

    /// <inheritdoc />
    public IRequestContext WithIdempotencyKey(string? idempotencyKey) => this with { IdempotencyKey = idempotencyKey };

    /// <inheritdoc />
    public IRequestContext WithTenantId(string? tenantId) => this with { TenantId = tenantId };
}
