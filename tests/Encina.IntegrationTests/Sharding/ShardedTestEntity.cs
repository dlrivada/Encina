using Encina.Sharding;

namespace Encina.IntegrationTests.Sharding;

/// <summary>
/// Test entity for sharding integration tests.
/// Implements <see cref="IShardable"/> for shard key extraction.
/// </summary>
public sealed class ShardedTestEntity : IShardable
{
    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the shard key used for routing.
    /// </summary>
    public string ShardKey { get; set; } = default!;

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets an optional value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public string GetShardKey() => ShardKey;
}
