using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.ReferenceTable;

/// <summary>
/// Hosted service that initializes <see cref="ReferenceTableMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReferenceTableMetrics"/> registers <c>ObservableGauge</c> instruments on the
/// static <c>Encina</c> meter during construction. This hosted service ensures the metrics
/// instance is created during application startup so that gauge callbacks are active when
/// an OpenTelemetry meter listener starts collecting.
/// </para>
/// <para>
/// If no <see cref="ReferenceTableMetricsCallbacks"/> is registered in the service collection,
/// this service completes without creating any metrics. The callbacks are registered by the
/// <c>Encina</c> package when reference table replication is enabled.
/// </para>
/// </remarks>
internal sealed class ReferenceTableMetricsInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private ReferenceTableMetrics? _metrics;

    public ReferenceTableMetricsInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var callbacks = _serviceProvider.GetService<ReferenceTableMetricsCallbacks>();
        if (callbacks is not null)
        {
            _metrics = new ReferenceTableMetrics(
                callbacks.RegisteredTablesCallback,
                callbacks.StalenessCallback);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
