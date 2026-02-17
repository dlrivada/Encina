namespace Encina.Sharding.TimeBased;

/// <summary>
/// Persists and queries tier metadata for time-based shards.
/// </summary>
/// <remarks>
/// <para>
/// The tier store is the source of truth for shard tier state. It is consumed by both the
/// <see cref="ITimeBasedShardRouter"/> (to resolve connection strings and read-only status)
/// and the tier transition scheduler (to find shards eligible for promotion).
/// </para>
/// <para>
/// Implementations must be thread-safe. The default <see cref="InMemoryTierStore"/> uses a
/// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/> internally.
/// </para>
/// </remarks>
public interface ITierStore
{
    /// <summary>
    /// Gets all shard tier metadata entries.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>All known shard tier info entries.</returns>
    Task<IReadOnlyList<ShardTierInfo>> GetAllTierInfoAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tier metadata for a specific shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tier info, or <see langword="null"/> if the shard is not found.</returns>
    Task<ShardTierInfo?> GetTierInfoAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the tier of an existing shard and records the transition timestamp.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="newTier">The target tier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the shard was found and updated; <see langword="false"/> otherwise.</returns>
    Task<bool> UpdateTierAsync(
        string shardId,
        ShardTier newTier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new shard tier entry to the store.
    /// </summary>
    /// <param name="tierInfo">The tier metadata to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddShardAsync(
        ShardTierInfo tierInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all shards in a given tier whose period has ended longer ago than the specified threshold,
    /// making them eligible for tier transition.
    /// </summary>
    /// <param name="fromTier">The current tier to query.</param>
    /// <param name="ageThreshold">
    /// The minimum elapsed time since the shard's <see cref="ShardTierInfo.PeriodEnd"/>
    /// before it is considered due for transition.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Shards that are due for transition out of <paramref name="fromTier"/>.</returns>
    Task<IReadOnlyList<ShardTierInfo>> GetShardsDueForTransitionAsync(
        ShardTier fromTier,
        TimeSpan ageThreshold,
        CancellationToken cancellationToken = default);
}
