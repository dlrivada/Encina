using Encina.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.SoftDelete;

/// <summary>
/// Hosted service that initializes <see cref="SoftDeleteMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <see cref="SoftDeleteMetrics"/> registers metric instruments on the static <c>Encina</c>
/// meter during construction. This hosted service ensures the metrics instance is created
/// during application startup so that instruments are active when an OpenTelemetry meter
/// listener starts collecting.
/// </remarks>
internal sealed class SoftDeleteMetricsInitializer : IHostedService
{
    private SoftDeleteMetrics? _metrics;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _metrics = new SoftDeleteMetrics();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
