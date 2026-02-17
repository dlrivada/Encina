using Encina.DomainModeling.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.Repository;

/// <summary>
/// Hosted service that initializes <see cref="RepositoryMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <see cref="RepositoryMetrics"/> registers metric instruments on the static <c>Encina</c>
/// meter during construction. This hosted service ensures the metrics instance is created
/// during application startup so that instruments are active when an OpenTelemetry meter
/// listener starts collecting.
/// </remarks>
internal sealed class RepositoryMetricsInitializer : IHostedService
{
    private RepositoryMetrics? _metrics;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _metrics = new RepositoryMetrics();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
