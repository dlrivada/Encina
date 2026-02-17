using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.Cdc;

/// <summary>
/// Hosted service that initializes <see cref="ShardedCdcMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ShardedCdcMetrics"/> registers <c>ObservableGauge</c> instruments on the
/// static <c>Encina</c> meter during construction. This hosted service ensures the metrics
/// instance is created during application startup so that gauge callbacks are active when
/// an OpenTelemetry meter listener starts collecting.
/// </para>
/// <para>
/// If no <see cref="ShardedCdcMetricsCallbacks"/> is registered in the service collection,
/// this service completes without creating any metrics. The callbacks are registered by the
/// <c>Encina.Cdc</c> package when sharded capture is enabled.
/// </para>
/// </remarks>
internal sealed class ShardedCdcMetricsInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private ShardedCdcMetrics? _metrics;

    public ShardedCdcMetricsInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var callbacks = _serviceProvider.GetService<ShardedCdcMetricsCallbacks>();
        if (callbacks is not null)
        {
            _metrics = new ShardedCdcMetrics(
                callbacks.ActiveConnectorsCallback,
                callbacks.LagCallback);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
