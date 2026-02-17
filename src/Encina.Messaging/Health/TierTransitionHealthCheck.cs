using Encina.Sharding.TimeBased;
using Encina.Sharding.TimeBased.Health;

namespace Encina.Messaging.Health;

/// <summary>
/// Health check that monitors tier transition timeliness for time-based shards.
/// </summary>
/// <remarks>
/// <para>
/// Evaluates each shard's age (days since <see cref="ShardTierInfo.PeriodEnd"/>) against
/// per-tier thresholds configured in <see cref="TierTransitionHealthCheckOptions"/>:
/// </para>
/// <list type="bullet">
///   <item><description><b>Degraded</b>: A shard has exceeded its tier's expected age threshold,
///   indicating a missed tier transition.</description></item>
///   <item><description><b>Unhealthy</b>: A shard has exceeded the unhealthy multiplier of its
///   tier's threshold, or the tier store is unreachable.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// builder.Services
///     .AddHealthChecks()
///     .AddEncinaTierTransition(options: new TierTransitionHealthCheckOptions
///     {
///         MaxExpectedHotAgeDays = 35,
///         MaxExpectedWarmAgeDays = 95,
///     });
/// </code>
/// </example>
public sealed class TierTransitionHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// Default name for this health check.
    /// </summary>
    public const string DefaultName = "encina-tier-transition";

    private static readonly IReadOnlyCollection<string> DefaultTags = ["encina", "ready", "sharding", "tiering"];

    private readonly ITierStore _tierStore;
    private readonly TimeProvider _timeProvider;
    private readonly TierTransitionHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TierTransitionHealthCheck"/> class.
    /// </summary>
    /// <param name="tierStore">The tier metadata store to query.</param>
    /// <param name="options">Optional health check configuration. Uses defaults if <see langword="null"/>.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public TierTransitionHealthCheck(
        ITierStore tierStore,
        TierTransitionHealthCheckOptions? options = null,
        TimeProvider? timeProvider = null)
        : base(DefaultName, DefaultTags)
    {
        ArgumentNullException.ThrowIfNull(tierStore);

        _tierStore = tierStore;
        _options = options ?? new TierTransitionHealthCheckOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var allShards = await _tierStore.GetAllTierInfoAsync(cancellationToken).ConfigureAwait(false);

        if (allShards.Count == 0)
        {
            return HealthCheckResult.Healthy("No shards registered in the tier store.");
        }

        var nowDate = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var overdueShards = new List<string>();
        var criticalShards = new List<string>();
        var tierCounts = new Dictionary<string, int>();

        foreach (var shard in allShards)
        {
            var tierName = shard.CurrentTier.ToString();
            tierCounts[tierName] = tierCounts.GetValueOrDefault(tierName) + 1;

            // Skip Archived shards — they're terminal
            if (shard.CurrentTier == ShardTier.Archived)
            {
                continue;
            }

            var ageDays = nowDate.DayNumber - shard.PeriodEnd.DayNumber;

            if (ageDays <= 0)
            {
                continue; // Shard period hasn't ended yet
            }

            var maxAge = GetMaxExpectedAge(shard.CurrentTier);

            if (ageDays > maxAge * _options.UnhealthyMultiplier)
            {
                criticalShards.Add(
                    $"{shard.ShardId} ({tierName}, {ageDays}d overdue, max {maxAge}d)");
            }
            else if (ageDays > maxAge)
            {
                overdueShards.Add(
                    $"{shard.ShardId} ({tierName}, {ageDays}d overdue, max {maxAge}d)");
            }
        }

        var data = new Dictionary<string, object>
        {
            ["total_shards"] = allShards.Count,
            ["tier_distribution"] = tierCounts,
        };

        if (criticalShards.Count > 0)
        {
            data["critical_shards"] = criticalShards;
            return HealthCheckResult.Unhealthy(
                $"{criticalShards.Count} shard(s) critically overdue for tier transition.",
                data: data);
        }

        if (overdueShards.Count > 0)
        {
            data["overdue_shards"] = overdueShards;
            return HealthCheckResult.Degraded(
                $"{overdueShards.Count} shard(s) overdue for tier transition.",
                data: data);
        }

        return HealthCheckResult.Healthy(
            "All shards are within expected tier age thresholds.",
            data);
    }

    private int GetMaxExpectedAge(ShardTier tier) => tier switch
    {
        ShardTier.Hot => _options.MaxExpectedHotAgeDays,
        ShardTier.Warm => _options.MaxExpectedWarmAgeDays,
        ShardTier.Cold => _options.MaxExpectedColdAgeDays,
        _ => int.MaxValue
    };
}
