using Encina.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.QueryCache;

/// <summary>
/// Hosted service that initializes <see cref="QueryCacheMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <see cref="QueryCacheMetrics"/> registers metric instruments on the static <c>Encina</c>
/// meter during construction. This hosted service ensures the metrics instance is created
/// during application startup so that instruments are active when an OpenTelemetry meter
/// listener starts collecting.
/// </remarks>
internal sealed class QueryCacheMetricsInitializer : IHostedService
{
    private QueryCacheMetrics? _metrics;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _metrics = new QueryCacheMetrics();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
