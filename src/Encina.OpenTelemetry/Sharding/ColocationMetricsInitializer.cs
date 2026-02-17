using Encina.Sharding.Colocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.Sharding;

/// <summary>
/// Hosted service that initializes <see cref="ColocationMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ColocationMetrics"/> registers <c>ObservableGauge</c> instruments on the
/// static <c>Encina</c> meter during construction. This hosted service ensures the metrics
/// instance is created during application startup so that gauge callbacks are active when
/// an OpenTelemetry meter listener starts collecting.
/// </para>
/// <para>
/// If no <see cref="ColocationGroupRegistry"/> is registered, this service completes
/// without creating any metrics.
/// </para>
/// </remarks>
internal sealed class ColocationMetricsInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private ColocationMetrics? _metrics;

    public ColocationMetricsInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var registry = _serviceProvider.GetService<ColocationGroupRegistry>();
        if (registry is not null)
        {
            _metrics = new ColocationMetrics(registry);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
