using Encina.DomainModeling.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.BulkOperations;

/// <summary>
/// Hosted service that initializes <see cref="BulkOperationsMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <see cref="BulkOperationsMetrics"/> registers metric instruments on the static <c>Encina</c>
/// meter during construction. This hosted service ensures the metrics instance is created
/// during application startup so that instruments are active when an OpenTelemetry meter
/// listener starts collecting.
/// </remarks>
internal sealed class BulkOperationsMetricsInitializer : IHostedService
{
    private BulkOperationsMetrics? _metrics;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _metrics = new BulkOperationsMetrics();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
