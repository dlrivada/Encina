using Encina.DomainModeling.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.Audit;

/// <summary>
/// Hosted service that initializes <see cref="AuditMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <see cref="AuditMetrics"/> registers metric instruments on the static <c>Encina</c>
/// meter during construction. This hosted service ensures the metrics instance is created
/// during application startup so that instruments are active when an OpenTelemetry meter
/// listener starts collecting.
/// </remarks>
internal sealed class AuditMetricsInitializer : IHostedService
{
    private AuditMetrics? _metrics;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _metrics = new AuditMetrics();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
