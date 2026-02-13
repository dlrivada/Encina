using Encina.IdGeneration.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Encina.OpenTelemetry.IdGeneration;

/// <summary>
/// Hosted service that initializes <see cref="IdGenerationMetrics"/> on application startup.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IdGenerationMetrics"/> registers metric instruments on the static <c>Encina</c>
/// meter during construction. This hosted service ensures the metrics instance is created
/// during application startup so that instruments are active when an OpenTelemetry meter
/// listener starts collecting.
/// </para>
/// </remarks>
internal sealed class IdGenerationMetricsInitializer : IHostedService
{
    private IdGenerationMetrics? _metrics;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _metrics = new IdGenerationMetrics();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
