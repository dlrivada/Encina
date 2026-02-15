using Encina.Sharding.Routing;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// Internal entry combining a <see cref="ShardRange"/> with <see cref="ShardTierInfo"/>
/// for use by the <see cref="TimeBasedShardRouter"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Range"/> provides the lexicographic key boundaries used for binary search
/// routing, while <see cref="TierInfo"/> carries the full tier metadata including the
/// connection string, read-only status, and transition history.
/// </para>
/// <para>
/// Entries are sorted by <see cref="ShardRange.StartKey"/> in the router's internal array
/// to enable efficient binary search.
/// </para>
/// </remarks>
/// <param name="Range">The shard range with start/end keys and shard ID for binary search routing.</param>
/// <param name="TierInfo">The tier metadata for the shard.</param>
internal sealed record TimeBasedShardEntry(ShardRange Range, ShardTierInfo TierInfo)
{
    /// <summary>
    /// Gets the shard range used for binary search routing.
    /// </summary>
    public ShardRange Range { get; } = Range ?? throw new ArgumentNullException(nameof(Range));

    /// <summary>
    /// Gets the tier metadata for the shard.
    /// </summary>
    public ShardTierInfo TierInfo { get; } = TierInfo ?? throw new ArgumentNullException(nameof(TierInfo));
}
