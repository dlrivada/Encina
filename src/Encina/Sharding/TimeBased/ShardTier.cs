namespace Encina.Sharding.TimeBased;

/// <summary>
/// Represents the storage tier of a time-based shard, determining its performance
/// characteristics and mutability.
/// </summary>
/// <remarks>
/// <para>
/// Shards progress through tiers as they age: <see cref="Hot"/> (active writes) to
/// <see cref="Warm"/> (recent, read-heavy) to <see cref="Cold"/> (infrequent access) to
/// <see cref="Archived"/> (long-term retention).
/// </para>
/// <para>
/// Only <see cref="Hot"/> shards accept write operations. All other tiers are read-only
/// at the application level, and write attempts return error code
/// <c>encina.sharding.shard_read_only</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define tier transitions based on age
/// var transitions = new[]
/// {
///     new TierTransition(ShardTier.Hot, ShardTier.Warm, TimeSpan.FromDays(30)),
///     new TierTransition(ShardTier.Warm, ShardTier.Cold, TimeSpan.FromDays(90)),
///     new TierTransition(ShardTier.Cold, ShardTier.Archived, TimeSpan.FromDays(365)),
/// };
/// </code>
/// </example>
public enum ShardTier
{
    /// <summary>
    /// Active tier for current data. Accepts both reads and writes.
    /// Typically backed by high-performance storage (SSD, in-memory caches).
    /// </summary>
    Hot,

    /// <summary>
    /// Recent historical data. Read-only at the application level.
    /// Typically backed by standard storage with good read performance.
    /// </summary>
    Warm,

    /// <summary>
    /// Infrequently accessed historical data. Read-only at the application level.
    /// Typically backed by cost-optimized storage (HDD, compressed).
    /// </summary>
    Cold,

    /// <summary>
    /// Long-term retention data. Read-only at the application level.
    /// Typically backed by archival storage (object storage, tape).
    /// </summary>
    Archived
}
