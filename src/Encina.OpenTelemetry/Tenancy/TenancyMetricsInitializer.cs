using Encina.Tenancy.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.Tenancy;

/// <summary>
/// Hosted service that initializes <see cref="TenancyMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <see cref="TenancyMetrics"/> registers metric instruments on the static <c>Encina</c>
/// meter during construction. This hosted service ensures the metrics instance is created
/// during application startup so that instruments are active when an OpenTelemetry meter
/// listener starts collecting.
/// </remarks>
internal sealed class TenancyMetricsInitializer : IHostedService
{
    private TenancyMetrics? _metrics;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _metrics = new TenancyMetrics();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
