using Encina.Sharding.TimeBased;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.Sharding;

/// <summary>
/// Hosted service that initializes <see cref="TimeBasedShardingMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TimeBasedShardingMetrics"/> registers <c>ObservableGauge</c> instruments on the
/// static <c>Encina</c> meter during construction. This hosted service ensures the metrics
/// instance is created during application startup so that gauge callbacks are active when
/// an OpenTelemetry meter listener starts collecting.
/// </para>
/// <para>
/// If no <see cref="ITierStore"/> is registered (i.e., time-based sharding is not enabled),
/// this service completes without creating any metrics. When an <see cref="ITierStore"/> is
/// available, the service creates <see cref="TimeBasedShardingMetricsCallbacks"/> from it
/// and initializes the metrics.
/// </para>
/// </remarks>
internal sealed class TimeBasedShardingMetricsInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private TimeBasedShardingMetrics? _metrics;

    public TimeBasedShardingMetricsInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // First check for explicit callbacks (registered by user or custom integration)
        var callbacks = _serviceProvider.GetService<TimeBasedShardingMetricsCallbacks>();

        if (callbacks is null)
        {
            // Auto-create callbacks from ITierStore if available
            var tierStore = _serviceProvider.GetService<ITierStore>();
            var timeProvider = _serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;

            if (tierStore is not null)
            {
                callbacks = CreateCallbacksFromTierStore(tierStore, timeProvider);
            }
        }

        if (callbacks is not null)
        {
            _metrics = new TimeBasedShardingMetrics(callbacks);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static TimeBasedShardingMetricsCallbacks CreateCallbacksFromTierStore(
        ITierStore tierStore,
        TimeProvider timeProvider)
    {
        return new TimeBasedShardingMetricsCallbacks(
            shardsPerTierCallback: () =>
            {
                var allShards = tierStore.GetAllTierInfoAsync().GetAwaiter().GetResult();
                return allShards
                    .GroupBy(s => s.CurrentTier)
                    .Select(g => (Tier: g.Key.ToString(), Count: g.Count()));
            },
            oldestHotShardAgeDaysCallback: () =>
            {
                var allShards = tierStore.GetAllTierInfoAsync().GetAwaiter().GetResult();
                var hotShards = allShards.Where(s => s.CurrentTier == ShardTier.Hot).ToList();

                if (hotShards.Count == 0)
                {
                    return null;
                }

                var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
                var oldestPeriodEnd = hotShards.Min(s => s.PeriodEnd);
                var ageDays = (DateOnly.FromDateTime(nowUtc).DayNumber - oldestPeriodEnd.DayNumber);

                return ageDays > 0 ? (double)ageDays : 0;
            });
    }
}
