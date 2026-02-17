using Encina.DomainModeling.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.UnitOfWork;

/// <summary>
/// Hosted service that initializes <see cref="UnitOfWorkMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <see cref="UnitOfWorkMetrics"/> registers metric instruments on the static <c>Encina</c>
/// meter during construction. This hosted service ensures the metrics instance is created
/// during application startup so that instruments are active when an OpenTelemetry meter
/// listener starts collecting.
/// </remarks>
internal sealed class UnitOfWorkMetricsInitializer : IHostedService
{
    private UnitOfWorkMetrics? _metrics;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _metrics = new UnitOfWorkMetrics();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
