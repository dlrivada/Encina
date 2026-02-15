namespace Encina.Sharding.TimeBased;

/// <summary>
/// Metadata describing a time-based shard's identity, tier, period boundaries, and connection details.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="ShardTierInfo"/> represents a single shard in a time-based sharding scheme,
/// combining the shard identifier with its current storage tier, time boundaries, and
/// connection information. The <see cref="PeriodStart"/> and <see cref="PeriodEnd"/> form a
/// half-open interval <c>[PeriodStart, PeriodEnd)</c>.
/// </para>
/// <para>
/// Only shards in the <see cref="ShardTier.Hot"/> tier accept writes. Shards in other tiers
/// are read-only at the application level, and the <see cref="IsReadOnly"/> property reflects this.
/// </para>
/// </remarks>
/// <param name="ShardId">Unique identifier for this shard (e.g., <c>"orders-2026-02"</c>).</param>
/// <param name="CurrentTier">The current storage tier of this shard.</param>
/// <param name="PeriodStart">Inclusive start date of the time period covered by this shard.</param>
/// <param name="PeriodEnd">Exclusive end date of the time period covered by this shard.</param>
/// <param name="IsReadOnly">
/// Whether write operations are blocked for this shard. Automatically <see langword="true"/> for
/// non-<see cref="ShardTier.Hot"/> tiers.
/// </param>
/// <param name="ConnectionString">Database connection string for this shard.</param>
/// <param name="CreatedAtUtc">UTC timestamp when this shard metadata was first created.</param>
/// <param name="LastTransitionAtUtc">
/// UTC timestamp of the most recent tier transition. <see langword="null"/> if no transitions have occurred.
/// </param>
public sealed record ShardTierInfo(
    string ShardId,
    ShardTier CurrentTier,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    bool IsReadOnly,
    string ConnectionString,
    DateTime CreatedAtUtc,
    DateTime? LastTransitionAtUtc = null)
{
    /// <summary>
    /// Gets the unique shard identifier.
    /// </summary>
    public string ShardId { get; } = !string.IsNullOrWhiteSpace(ShardId)
        ? ShardId
        : throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(ShardId));

    /// <summary>
    /// Gets the database connection string for this shard.
    /// </summary>
    public string ConnectionString { get; } = !string.IsNullOrWhiteSpace(ConnectionString)
        ? ConnectionString
        : throw new ArgumentException("Connection string cannot be null or whitespace.", nameof(ConnectionString));

    /// <summary>
    /// Gets the inclusive period start date.
    /// </summary>
    public DateOnly PeriodStart { get; } = PeriodStart < PeriodEnd
        ? PeriodStart
        : throw new ArgumentException("PeriodStart must be earlier than PeriodEnd.", nameof(PeriodStart));
}
