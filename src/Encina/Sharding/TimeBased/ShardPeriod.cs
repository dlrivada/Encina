namespace Encina.Sharding.TimeBased;

/// <summary>
/// Defines the time granularity used to partition shards in a time-based sharding strategy.
/// </summary>
/// <remarks>
/// <para>
/// Each period value determines how <see cref="PeriodBoundaryCalculator"/> computes
/// the start and end boundaries for a given timestamp. Shorter periods produce more
/// shards with smaller data volumes; longer periods produce fewer, larger shards.
/// </para>
/// <para>
/// Choose a period that balances query performance (fewer shards to scatter-gather)
/// against individual shard size (smaller shards are easier to archive and move between tiers).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Monthly sharding for audit logs
/// var period = ShardPeriod.Monthly;
///
/// // Daily sharding for high-volume IoT data
/// var period = ShardPeriod.Daily;
/// </code>
/// </example>
public enum ShardPeriod
{
    /// <summary>
    /// One shard per calendar day.
    /// </summary>
    Daily,

    /// <summary>
    /// One shard per ISO 8601 week (Monday through Sunday).
    /// </summary>
    Weekly,

    /// <summary>
    /// One shard per calendar month.
    /// </summary>
    Monthly,

    /// <summary>
    /// One shard per calendar quarter (Q1: Jan-Mar, Q2: Apr-Jun, Q3: Jul-Sep, Q4: Oct-Dec).
    /// </summary>
    Quarterly,

    /// <summary>
    /// One shard per calendar year.
    /// </summary>
    Yearly
}
