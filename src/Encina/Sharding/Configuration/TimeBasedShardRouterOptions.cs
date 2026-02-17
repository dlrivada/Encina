using Encina.Sharding.TimeBased;

namespace Encina.Sharding.Configuration;

/// <summary>
/// Configuration options for time-based shard routing, combining the routing strategy
/// with lifecycle automation (tier transitions, auto-shard creation, and retention).
/// </summary>
/// <remarks>
/// <para>
/// This class is used with <see cref="ShardingOptions{TEntity}.UseTimeBasedRouting"/>
/// to configure time-partitioned sharding. It merges routing configuration (period, week start)
/// with lifecycle automation options (tier transitions, auto-shard creation, scheduler settings).
/// </para>
/// <para>
/// Per-tier connection string templates enable automatic connection routing as shards
/// transition through the lifecycle: Hot → Warm → Cold → Archived. Use <c>{0}</c>
/// as a placeholder for the period label in connection string templates.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaSharding&lt;Order&gt;(options =>
/// {
///     options.UseTimeBasedRouting(tb =>
///     {
///         tb.Period = ShardPeriod.Monthly;
///         tb.ShardIdPrefix = "orders";
///         tb.HotTierConnectionString = "Server=hot;Database=orders_{0}";
///         tb.WarmTierConnectionString = "Server=warm;Database=orders_{0}";
///         tb.TierTransitions =
///         [
///             new TierTransition(ShardTier.Hot, ShardTier.Warm, TimeSpan.FromDays(30)),
///             new TierTransition(ShardTier.Warm, ShardTier.Cold, TimeSpan.FromDays(90)),
///         ];
///     })
///     .AddShard("orders-2026-01", "Server=hot;Database=orders_2026_01")
///     .AddShard("orders-2025-12", "Server=warm;Database=orders_2025_12");
/// });
/// </code>
/// </example>
public sealed class TimeBasedShardRouterOptions
{
    /// <summary>
    /// Gets or sets the time period granularity for shard partitioning.
    /// Defaults to <see cref="ShardPeriod.Monthly"/>.
    /// </summary>
    public ShardPeriod Period { get; set; } = ShardPeriod.Monthly;

    /// <summary>
    /// Gets or sets the first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/> (ISO 8601). Ignored for other periods.
    /// </summary>
    public DayOfWeek WeekStart { get; set; } = DayOfWeek.Monday;

    /// <summary>
    /// Gets or sets the prefix used for auto-generated shard IDs.
    /// The shard ID is formed as <c>"{ShardIdPrefix}-{PeriodLabel}"</c>.
    /// </summary>
    /// <example><c>"orders"</c> produces shard IDs like <c>"orders-2026-03"</c>.</example>
    public string ShardIdPrefix { get; set; } = "shard";

    /// <summary>
    /// Gets or sets whether to automatically create new Hot-tier shards before each
    /// time period begins. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// When enabled, the <see cref="TierTransitionScheduler"/> creates new shards using
    /// the <see cref="HotTierConnectionString"/> template. The shard is created
    /// <see cref="ShardCreationLeadTime"/> before the period start.
    /// </remarks>
    public bool AutoCreateShards { get; set; }

    /// <summary>
    /// Gets or sets how far in advance of a new period the shard should be auto-created.
    /// Defaults to 1 day. Only used when <see cref="AutoCreateShards"/> is <see langword="true"/>.
    /// </summary>
    public TimeSpan ShardCreationLeadTime { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Gets or sets the tier transition rules that define when shards move between tiers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each <see cref="TierTransition"/> specifies the source tier, target tier, and the minimum
    /// age (from period end) before the transition is eligible. Transitions must not skip tiers
    /// (e.g., Hot → Cold is invalid; use Hot → Warm, then Warm → Cold).
    /// </para>
    /// </remarks>
    public IReadOnlyList<TierTransition> TierTransitions { get; set; } = [];

    /// <summary>
    /// Gets or sets the interval between tier transition checks by the scheduler.
    /// Defaults to 1 hour. Only used when <see cref="TierTransitions"/> is non-empty
    /// or <see cref="AutoCreateShards"/> is <see langword="true"/>.
    /// </summary>
    public TimeSpan TransitionCheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the connection string template for Hot-tier shards.
    /// Use <c>{0}</c> as a placeholder for the period label.
    /// Also used as the template for auto-shard creation.
    /// </summary>
    /// <example><c>"Server=hot;Database=orders_{0}"</c></example>
    public string? HotTierConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the connection string template for Warm-tier shards.
    /// Use <c>{0}</c> as a placeholder for the period label.
    /// </summary>
    /// <example><c>"Server=warm;Database=orders_{0}"</c></example>
    public string? WarmTierConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the connection string template for Cold-tier shards.
    /// Use <c>{0}</c> as a placeholder for the period label.
    /// </summary>
    /// <example><c>"Server=cold;Database=orders_{0}"</c></example>
    public string? ColdTierConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the connection string template for Archived-tier shards.
    /// Use <c>{0}</c> as a placeholder for the period label.
    /// </summary>
    /// <example><c>"Server=archive;Database=orders_{0}"</c></example>
    public string? ArchivedTierConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the optional retention period after which shards are eligible
    /// for automatic deletion. When <see langword="null"/>, shards are never deleted.
    /// </summary>
    /// <remarks>
    /// The retention period is measured from the shard's <see cref="ShardTierInfo.PeriodEnd"/>.
    /// Shards that have exceeded the retention period will be deleted by the
    /// <see cref="TierTransitionScheduler"/> during its next check cycle.
    /// </remarks>
    public TimeSpan? RetentionPeriod { get; set; }

    /// <summary>
    /// Gets or sets the initial <see cref="ShardTierInfo"/> entries that seed the
    /// <see cref="ITierStore"/> at startup. These represent the existing shards
    /// in the time-based topology.
    /// </summary>
    /// <remarks>
    /// Each entry defines the shard's ID, tier, period boundaries, and connection string.
    /// The router is built from these entries.
    /// </remarks>
    public IReadOnlyList<ShardTierInfo> InitialShards { get; set; } = [];
}
