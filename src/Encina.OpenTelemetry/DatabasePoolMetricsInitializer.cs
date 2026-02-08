using Encina.Database;
using Encina.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry;

/// <summary>
/// Hosted service that initializes <see cref="DatabasePoolMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DatabasePoolMetrics"/> registers <c>ObservableGauge</c> instruments on the
/// static <c>Encina</c> meter during construction. This hosted service ensures the metrics
/// instance is created during application startup so that gauge callbacks are active when
/// an OpenTelemetry meter listener starts collecting.
/// </para>
/// <para>
/// If no <see cref="IDatabaseHealthMonitor"/> is registered, this service completes
/// without creating any metrics.
/// </para>
/// </remarks>
internal sealed class DatabasePoolMetricsInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private DatabasePoolMetrics? _metrics;

    public DatabasePoolMetricsInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var monitor = _serviceProvider.GetService<IDatabaseHealthMonitor>();
        if (monitor is not null)
        {
            _metrics = new DatabasePoolMetrics(monitor);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
