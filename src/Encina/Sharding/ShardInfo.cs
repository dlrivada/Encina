using System.Text.Json.Serialization;

using Encina.Sharding.ReplicaSelection;

namespace Encina.Sharding;

/// <summary>
/// Describes a single shard in the topology, including optional read replicas.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ConnectionString"/> and <see cref="ReplicaConnectionStrings"/> are credentials.
/// They are marked <see cref="JsonIgnoreAttribute"/> and masked by <see cref="ToString"/>, so
/// logging, string interpolation and System.Text.Json serialization never write them; only the
/// number of replicas is shown.
/// </para>
/// <para>
/// As a consequence, a <see cref="ShardInfo"/> cannot be read back from its own JSON: the
/// serialized form carries no connection string, and the constructor rejects a missing one.
/// Build shards from configuration binding or code (for example
/// <c>ShardingOptions&lt;TEntity&gt;.AddShard</c>), never from System.Text.Json.
/// </para>
/// </remarks>
/// <param name="ShardId">Unique identifier for this shard.</param>
/// <param name="ConnectionString">
/// Database connection string for this shard's primary (write) endpoint.
/// WARNING: contains sensitive credential data; it is excluded from JSON serialization and
/// from <see cref="ToString"/>.
/// </param>
/// <param name="Weight">Relative weight for load distribution (default 1). Higher values attract more traffic.</param>
/// <param name="IsActive">Whether this shard is actively accepting reads and writes.</param>
/// <param name="ReplicaConnectionStrings">
/// Optional list of connection strings for read replicas associated with this shard.
/// When empty, all reads are served by the primary connection string.
/// WARNING: contains sensitive credential data; it is excluded from JSON serialization and
/// from <see cref="ToString"/>.
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
    /// <remarks>Excluded from JSON serialization and masked by <see cref="ToString"/>.</remarks>
    [JsonIgnore]
    public string ConnectionString { get; } = !string.IsNullOrWhiteSpace(ConnectionString)
        ? ConnectionString
        : throw new ArgumentException("Connection string cannot be null or whitespace.", nameof(ConnectionString));

    /// <summary>
    /// Gets the read replica connection strings for this shard.
    /// </summary>
    /// <value>
    /// A non-null list of replica connection strings. Empty when no replicas are configured.
    /// </value>
    /// <remarks>Excluded from JSON serialization and masked by <see cref="ToString"/>.</remarks>
    [JsonIgnore]
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

    /// <summary>
    /// Returns a description of the shard that never contains a connection string.
    /// </summary>
    /// <returns>
    /// The shard identifier, weight, active flag and replica strategy, with
    /// <c>ConnectionString = ***</c> and the replica connection strings reduced to their count
    /// (<c>ReplicaConnectionStrings = [N masked]</c>).
    /// </returns>
    public override string ToString() =>
        $"ShardInfo {{ ShardId = {ShardId}, ConnectionString = ***, Weight = {Weight}, IsActive = {IsActive}, ReplicaConnectionStrings = [{ReplicaCount} masked], ReplicaStrategy = {ReplicaStrategy} }}";

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
