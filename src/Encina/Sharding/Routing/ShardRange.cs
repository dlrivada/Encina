namespace Encina.Sharding.Routing;

/// <summary>
/// Defines a key range mapped to a shard.
/// </summary>
/// <param name="StartKey">Inclusive start of the range.</param>
/// <param name="EndKey">Exclusive end of the range. Null means unbounded (extends to +infinity).</param>
/// <param name="ShardId">The shard that owns this range.</param>
/// <remarks>
/// <para>
/// Ranges are compared lexicographically by <see cref="RangeShardRouter"/> using ordinal string comparison.
/// A null <paramref name="EndKey"/> represents an unbounded range that extends to positive infinity,
/// making it suitable for the last range in a sorted partition scheme.
/// </para>
/// <para>
/// Ranges must be non-overlapping and cover the entire key space when used with <see cref="RangeShardRouter"/>.
/// Overlapping ranges are detected at configuration time and produce error code
/// <c>encina.sharding.overlapping_ranges</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Time-series partitioning: each shard covers a date range
/// var ranges = new[]
/// {
///     new ShardRange("2024-01", "2024-07", "shard-h1-2024"),
///     new ShardRange("2024-07", "2025-01", "shard-h2-2024"),
///     new ShardRange("2025-01", null, "shard-current") // unbounded: 2025-01 onwards
/// };
/// </code>
/// </example>
public sealed record ShardRange(string StartKey, string? EndKey, string ShardId)
{
    /// <summary>
    /// Gets the inclusive start of the range.
    /// </summary>
    public string StartKey { get; } = StartKey ?? throw new ArgumentNullException(nameof(StartKey));

    /// <summary>
    /// Gets the shard that owns this range.
    /// </summary>
    public string ShardId { get; } = !string.IsNullOrWhiteSpace(ShardId)
        ? ShardId
        : throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(ShardId));
}
