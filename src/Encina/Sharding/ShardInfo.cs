namespace Encina.Sharding;

/// <summary>
/// Describes a single shard in the topology.
/// </summary>
/// <param name="ShardId">Unique identifier for this shard.</param>
/// <param name="ConnectionString">Database connection string for this shard.</param>
/// <param name="Weight">Relative weight for load distribution (default 1). Higher values attract more traffic.</param>
/// <param name="IsActive">Whether this shard is actively accepting reads and writes.</param>
public sealed record ShardInfo(
    string ShardId,
    string ConnectionString,
    int Weight = 1,
    bool IsActive = true)
{
    /// <summary>
    /// Gets the shard identifier.
    /// </summary>
    public string ShardId { get; } = !string.IsNullOrWhiteSpace(ShardId)
        ? ShardId
        : throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(ShardId));

    /// <summary>
    /// Gets the connection string for this shard.
    /// </summary>
    public string ConnectionString { get; } = !string.IsNullOrWhiteSpace(ConnectionString)
        ? ConnectionString
        : throw new ArgumentException("Connection string cannot be null or whitespace.", nameof(ConnectionString));
}
