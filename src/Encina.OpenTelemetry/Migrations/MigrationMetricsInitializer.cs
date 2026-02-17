using Encina.Sharding.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.Migrations;

/// <summary>
/// Hosted service that initializes <see cref="MigrationMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MigrationMetrics"/> registers <c>ObservableGauge</c> instruments on the
/// static <c>Encina</c> meter during construction. This hosted service ensures the metrics
/// instance is created during application startup so that gauge callbacks are active when
/// an OpenTelemetry meter listener starts collecting.
/// </para>
/// <para>
/// If no <see cref="MigrationMetricsCallbacks"/> is registered (i.e., shard migration
/// coordination is not enabled), this service completes without creating any metrics.
/// When callbacks are available, the service creates the <see cref="MigrationMetrics"/>
/// instance and initializes all instruments.
/// </para>
/// </remarks>
internal sealed class MigrationMetricsInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private MigrationMetrics? _metrics;

    public MigrationMetricsInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Check for explicit callbacks (registered by user or migration coordination setup)
        var callbacks = _serviceProvider.GetService<MigrationMetricsCallbacks>();

        if (callbacks is null)
        {
            // Auto-create a no-op callback if migration coordination is registered
            // but no explicit callbacks were provided. This ensures counters and histograms
            // are still active for recording, even without an observable gauge data source.
            var coordinator = _serviceProvider.GetService<IShardedMigrationCoordinator>();

            if (coordinator is not null)
            {
                callbacks = new MigrationMetricsCallbacks(
                    driftDetectedCountCallback: () => 0);
            }
        }

        if (callbacks is not null)
        {
            _metrics = new MigrationMetrics(callbacks);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
