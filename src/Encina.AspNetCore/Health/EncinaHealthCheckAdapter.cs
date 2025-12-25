using Encina.Messaging.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AspNetHealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;
using EncinaHealthStatus = Encina.Messaging.Health.HealthStatus;

namespace Encina.AspNetCore.Health;

/// <summary>
/// Adapts an <see cref="IEncinaHealthCheck"/> to ASP.NET Core's <see cref="IHealthCheck"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// This adapter allows Encina health checks to integrate seamlessly with ASP.NET Core's
/// health check infrastructure, including:
/// <list type="bullet">
/// <item><description>Kubernetes readiness and liveness probes</description></item>
/// <item><description>Health check UI dashboards</description></item>
/// <item><description>Monitoring systems (Prometheus, Azure Monitor, etc.)</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class EncinaHealthCheckAdapter : IHealthCheck
{
    private readonly IEncinaHealthCheck _encinaHealthCheck;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaHealthCheckAdapter"/> class.
    /// </summary>
    /// <param name="encinaHealthCheck">The Encina health check to adapt.</param>
    public EncinaHealthCheckAdapter(IEncinaHealthCheck encinaHealthCheck)
    {
        ArgumentNullException.ThrowIfNull(encinaHealthCheck);
        _encinaHealthCheck = encinaHealthCheck;
    }

    /// <inheritdoc />
    public async Task<AspNetHealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await _encinaHealthCheck.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

        return result.Status switch
        {
            EncinaHealthStatus.Healthy => AspNetHealthCheckResult.Healthy(
                result.Description,
                ConvertData(result.Data)),

            EncinaHealthStatus.Degraded => AspNetHealthCheckResult.Degraded(
                result.Description,
                result.Exception,
                ConvertData(result.Data)),

            EncinaHealthStatus.Unhealthy => AspNetHealthCheckResult.Unhealthy(
                result.Description,
                result.Exception,
                ConvertData(result.Data)),

            _ => AspNetHealthCheckResult.Unhealthy($"Unknown health status: {result.Status}")
        };
    }

    private static IReadOnlyDictionary<string, object>? ConvertData(IReadOnlyDictionary<string, object> data)
    {
        return data.Count > 0 ? data : null;
    }
}
