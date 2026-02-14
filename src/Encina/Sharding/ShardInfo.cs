using Encina.Sharding.ReplicaSelection;

namespace Encina.Sharding;

/// <summary>
/// Describes a single shard in the topology, including optional read replicas.
/// </summary>
/// <param name="ShardId">Unique identifier for this shard.</param>
/// <param name="ConnectionString">Database connection string for this shard's primary (write) endpoint.</param>
/// <param name="Weight">Relative weight for load distribution (default 1). Higher values attract more traffic.</param>
/// <param name="IsActive">Whether this shard is actively accepting reads and writes.</param>
/// <param name="ReplicaConnectionStrings">
/// Optional list of connection strings for read replicas associated with this shard.
/// When empty, all reads are served by the primary connection string.
/// </param>
/// <param name="ReplicaStrategy">
/// Optional replica selection strategy override for this shard.
/// When <see langword="null"/>, the global default from <see cref="ShardedReadWriteOptions"/> is used.
/// </param>
public sealed record ShardInfo(
    string ShardId,
    string ConnectionString,
    int Weight = 1,
    bool IsActive = true,
    IReadOnlyList<string>? ReplicaConnectionStrings = null,
    ReplicaSelectionStrategy? ReplicaStrategy = null)
{
    /// <summary>
    /// Gets the shard identifier.
    /// </summary>
    public string ShardId { get; } = !string.IsNullOrWhiteSpace(ShardId)
        ? ShardId
        : throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(ShardId));

    /// <summary>
    /// Gets the connection string for this shard's primary (write) endpoint.
    /// </summary>
    public string ConnectionString { get; } = !string.IsNullOrWhiteSpace(ConnectionString)
        ? ConnectionString
        : throw new ArgumentException("Connection string cannot be null or whitespace.", nameof(ConnectionString));

    /// <summary>
    /// Gets the read replica connection strings for this shard.
    /// </summary>
    /// <value>
    /// A non-null list of replica connection strings. Empty when no replicas are configured.
    /// </value>
    public IReadOnlyList<string> ReplicaConnectionStrings { get; } = ValidateReplicaConnectionStrings(
        ReplicaConnectionStrings ?? []);

    /// <summary>
    /// Gets a value indicating whether this shard has read replicas configured.
    /// </summary>
    public bool HasReplicas => ReplicaConnectionStrings.Count > 0;

    /// <summary>
    /// Gets the number of read replicas configured for this shard.
    /// </summary>
    public int ReplicaCount => ReplicaConnectionStrings.Count;

    private static IReadOnlyList<string> ValidateReplicaConnectionStrings(IReadOnlyList<string> replicas)
    {
        for (var i = 0; i < replicas.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(replicas[i]))
            {
                throw new ArgumentException(
                    $"Replica connection string at index {i} cannot be null or whitespace.",
                    nameof(replicas));
            }
        }

        return replicas;
    }
}
