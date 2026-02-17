using Encina.Sharding.TimeBased;
using Encina.Sharding.TimeBased.Health;

namespace Encina.Messaging.Health;

/// <summary>
/// Health check that monitors whether expected time-period shards exist.
/// </summary>
/// <remarks>
/// <para>
/// Checks that shards exist for the current and upcoming time periods based on
/// <see cref="ShardCreationHealthCheckOptions"/>:
/// </para>
/// <list type="bullet">
///   <item><description><b>Unhealthy</b>: The current period's shard is missing — data cannot
///   be written for the current time period.</description></item>
///   <item><description><b>Degraded</b>: The next period's shard is missing and we are within
///   the warning window (close to the end of the current period).</description></item>
///   <item><description><b>Healthy</b>: All expected shards exist.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// builder.Services
///     .AddHealthChecks()
///     .AddEncinaShardCreation(options: new ShardCreationHealthCheckOptions
///     {
///         Period = ShardPeriod.Monthly,
///         ShardIdPrefix = "orders",
///         WarningWindowDays = 5,
///     });
/// </code>
/// </example>
public sealed class ShardCreationHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// Default name for this health check.
    /// </summary>
    public const string DefaultName = "encina-shard-creation";

    private static readonly IReadOnlyCollection<string> DefaultTags = ["encina", "ready", "sharding", "tiering"];

    private readonly ITierStore _tierStore;
    private readonly TimeProvider _timeProvider;
    private readonly ShardCreationHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardCreationHealthCheck"/> class.
    /// </summary>
    /// <param name="tierStore">The tier metadata store to query.</param>
    /// <param name="options">Optional health check configuration. Uses defaults if <see langword="null"/>.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public ShardCreationHealthCheck(
        ITierStore tierStore,
        ShardCreationHealthCheckOptions? options = null,
        TimeProvider? timeProvider = null)
        : base(DefaultName, DefaultTags)
    {
        ArgumentNullException.ThrowIfNull(tierStore);

        _tierStore = tierStore;
        _options = options ?? new ShardCreationHealthCheckOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(nowUtc);

        // Compute current period boundaries and shard ID
        var currentPeriodStart = PeriodBoundaryCalculator.GetPeriodStart(today, _options.Period, _options.WeekStart);
        var currentPeriodLabel = PeriodBoundaryCalculator.GetPeriodLabel(currentPeriodStart, _options.Period, _options.WeekStart);
        var currentShardId = $"{_options.ShardIdPrefix}-{currentPeriodLabel}";

        // Check current period shard
        var currentShard = await _tierStore.GetTierInfoAsync(currentShardId, cancellationToken).ConfigureAwait(false);

        if (currentShard is null)
        {
            return HealthCheckResult.Unhealthy(
                $"Current period shard '{currentShardId}' is missing. " +
                "Data cannot be written for the current time period.",
                data: new Dictionary<string, object>
                {
                    ["missing_shard_id"] = currentShardId,
                    ["current_period"] = currentPeriodLabel,
                });
        }

        // Compute next period boundaries and shard ID
        var currentPeriodEnd = PeriodBoundaryCalculator.GetPeriodEnd(today, _options.Period, _options.WeekStart);
        var nextPeriodLabel = PeriodBoundaryCalculator.GetPeriodLabel(currentPeriodEnd, _options.Period, _options.WeekStart);
        var nextShardId = $"{_options.ShardIdPrefix}-{nextPeriodLabel}";

        // Check if we're within the warning window
        var daysUntilPeriodEnd = currentPeriodEnd.DayNumber - today.DayNumber;

        if (daysUntilPeriodEnd <= _options.WarningWindowDays)
        {
            var nextShard = await _tierStore.GetTierInfoAsync(nextShardId, cancellationToken).ConfigureAwait(false);

            if (nextShard is null)
            {
                return HealthCheckResult.Degraded(
                    $"Next period shard '{nextShardId}' is missing. " +
                    $"Current period ends in {daysUntilPeriodEnd} day(s).",
                    data: new Dictionary<string, object>
                    {
                        ["missing_shard_id"] = nextShardId,
                        ["next_period"] = nextPeriodLabel,
                        ["days_until_period_end"] = daysUntilPeriodEnd,
                    });
            }
        }

        return HealthCheckResult.Healthy(
            "All expected time-period shards exist.",
            new Dictionary<string, object>
            {
                ["current_shard_id"] = currentShardId,
                ["current_period"] = currentPeriodLabel,
                ["current_tier"] = currentShard.CurrentTier.ToString(),
            });
    }
}
