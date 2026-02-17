namespace Encina.Sharding.Routing;

/// <summary>
/// Calculates the key ranges affected by a topology change for planning shard rebalancing.
/// </summary>
/// <remarks>
/// <para>
/// When using <see cref="HashShardRouter"/> with virtual nodes, adding or removing a shard
/// only affects approximately <c>1/N</c> of all keys (where N is the total shard count).
/// This is a fundamental benefit of consistent hashing over naive modular hashing.
/// </para>
/// <para>
/// The affected key ranges describe hash ring segments that change ownership. Use this
/// information to plan data migration between shards. Actual data movement is the
/// application's responsibility.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Plan a rebalancing operation after adding a new shard
/// var oldTopology = new ShardTopology([
///     new ShardInfo("shard-1", "conn1"),
///     new ShardInfo("shard-2", "conn2")
/// ]);
/// var newTopology = new ShardTopology([
///     new ShardInfo("shard-1", "conn1"),
///     new ShardInfo("shard-2", "conn2"),
///     new ShardInfo("shard-3", "conn3") // new shard
/// ]);
///
/// var affected = rebalancer.CalculateAffectedKeyRanges(oldTopology, newTopology);
/// // Each range describes: PreviousShardId -&gt; NewShardId
/// </code>
/// </example>
public interface IShardRebalancer
{
    /// <summary>
    /// Calculates the key ranges that are affected when the topology changes.
    /// </summary>
    /// <param name="oldTopology">The topology before the change.</param>
    /// <param name="newTopology">The topology after the change.</param>
    /// <returns>
    /// A collection of affected key ranges, each describing which shard owned the range before
    /// and which shard will own it after the change.
    /// </returns>
    IReadOnlyList<AffectedKeyRange> CalculateAffectedKeyRanges(ShardTopology oldTopology, ShardTopology newTopology);
}

/// <summary>
/// Describes a key range affected by a topology change.
/// </summary>
/// <param name="RingStart">The start position on the hash ring (inclusive).</param>
/// <param name="RingEnd">The end position on the hash ring (exclusive).</param>
/// <param name="PreviousShardId">The shard that previously owned this range.</param>
/// <param name="NewShardId">The shard that will own this range after the change.</param>
public sealed record AffectedKeyRange(
    ulong RingStart,
    ulong RingEnd,
    string PreviousShardId,
    string NewShardId);
