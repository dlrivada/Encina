namespace Encina.Sharding.TimeBased;

/// <summary>
/// Configuration options for the time-based sharding lifecycle automation,
/// including tier transition scheduling and auto-shard creation.
/// </summary>
/// <remarks>
/// <para>
/// These options control the <see cref="TierTransitionScheduler"/> background service.
/// The scheduler periodically checks for shards that are due for tier transition
/// based on the configured <see cref="Transitions"/> thresholds.
/// </para>
/// <para>
/// Auto-shard creation ensures that a Hot-tier shard is ready before the next
/// time period begins. The <see cref="ShardCreationLeadTime"/> controls how far
/// in advance the new shard is created.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.Configure&lt;TimeBasedShardingOptions&gt;(options =>
/// {
///     options.Enabled = true;
///     options.CheckInterval = TimeSpan.FromMinutes(30);
///     options.Period = ShardPeriod.Monthly;
///     options.ShardCreationLeadTime = TimeSpan.FromDays(2);
///     options.ConnectionStringTemplate = "Server=hot;Database=orders_{0}";
///     options.ShardIdPrefix = "orders";
///     options.Transitions =
///     [
///         new TierTransition(ShardTier.Hot, ShardTier.Warm, TimeSpan.FromDays(30)),
///         new TierTransition(ShardTier.Warm, ShardTier.Cold, TimeSpan.FromDays(90)),
///     ];
/// });
/// </code>
/// </example>
public sealed class TimeBasedShardingOptions
{
    /// <summary>
    /// Gets or sets whether the tier transition scheduler is enabled.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval between tier transition checks.
    /// Defaults to 1 hour.
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the time period granularity for shard creation.
    /// </summary>
    public ShardPeriod Period { get; set; } = ShardPeriod.Monthly;

    /// <summary>
    /// Gets or sets the first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/> (ISO 8601). Ignored for other periods.
    /// </summary>
    public DayOfWeek WeekStart { get; set; } = DayOfWeek.Monday;

    /// <summary>
    /// Gets or sets the tier transition rules that define when shards move between tiers.
    /// </summary>
    /// <remarks>
    /// Each <see cref="TierTransition"/> specifies the source tier, target tier, and the minimum
    /// age (from period end) before the transition is eligible.
    /// </remarks>
    public IReadOnlyList<TierTransition> Transitions { get; set; } = [];

    /// <summary>
    /// Gets or sets whether auto-shard creation is enabled.
    /// When <see langword="true"/>, the scheduler creates new Hot-tier shards before
    /// the next time period begins. Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableAutoShardCreation { get; set; } = true;

    /// <summary>
    /// Gets or sets how far in advance of a new period the shard should be created.
    /// Defaults to 1 day.
    /// </summary>
    public TimeSpan ShardCreationLeadTime { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Gets or sets the prefix used for auto-generated shard IDs.
    /// The shard ID is formed as <c>"{ShardIdPrefix}-{PeriodLabel}"</c>.
    /// </summary>
    /// <example><c>"orders"</c> produces shard IDs like <c>"orders-2026-03"</c>.</example>
    public string ShardIdPrefix { get; set; } = "shard";

    /// <summary>
    /// Gets or sets the connection string template for auto-created shards.
    /// Use <c>{0}</c> as a placeholder for the period label.
    /// </summary>
    /// <example><c>"Server=hot;Database=orders_{0}"</c> produces
    /// <c>"Server=hot;Database=orders_2026-03"</c>.</example>
    public string? ConnectionStringTemplate { get; set; }
}
