using Encina.Messaging.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AspNetHealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;
using EncinaHealthStatus = Encina.Messaging.Health.HealthStatus;

namespace Encina.AspNetCore.Health;

/// <summary>
/// A composite health check that aggregates multiple <see cref="IEncinaHealthCheck"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This health check runs all registered Encina health checks and returns:
/// <list type="bullet">
/// <item><description><b>Healthy</b>: If all checks are healthy</description></item>
/// <item><description><b>Degraded</b>: If any check is degraded but none are unhealthy</description></item>
/// <item><description><b>Unhealthy</b>: If any check is unhealthy</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class CompositeEncinaHealthCheck : IHealthCheck
{
    private readonly IEnumerable<IEncinaHealthCheck> _healthChecks;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeEncinaHealthCheck"/> class.
    /// </summary>
    /// <param name="healthChecks">The health checks to aggregate.</param>
    public CompositeEncinaHealthCheck(IEnumerable<IEncinaHealthCheck> healthChecks)
    {
        _healthChecks = healthChecks ?? [];
    }

    /// <inheritdoc />
    public async Task<AspNetHealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var checks = _healthChecks.ToList();

        if (checks.Count == 0)
        {
            return AspNetHealthCheckResult.Healthy("No Encina health checks registered");
        }

        var results = new Dictionary<string, object>();
        var overallStatus = EncinaHealthStatus.Healthy;
        var descriptions = new List<string>();

        foreach (var check in checks)
        {
            var result = await check.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

            // Track individual check results
            results[check.Name] = new
            {
                status = result.Status.ToString(),
                description = result.Description,
                data = result.Data
            };

            // Update overall status (worst status wins)
            if (result.Status < overallStatus)
            {
                overallStatus = result.Status;
            }

            // Collect descriptions for non-healthy checks
            if (result.Status != EncinaHealthStatus.Healthy)
            {
                descriptions.Add($"{check.Name}: {result.Description}");
            }
        }

        var description = descriptions.Count > 0
            ? string.Join("; ", descriptions)
            : $"All {checks.Count} Encina health checks passed";

        return ToAspNetResult(overallStatus, description, results);
    }

    // crap-exempt: single-question switch — maps one status to its ASP.NET result
    private static AspNetHealthCheckResult ToAspNetResult(
        EncinaHealthStatus status,
        string description,
        IReadOnlyDictionary<string, object> results)
        => status switch
        {
            EncinaHealthStatus.Healthy => AspNetHealthCheckResult.Healthy(description, results),
            // The exception object of a check is never forwarded to the endpoint result.
            EncinaHealthStatus.Degraded => AspNetHealthCheckResult.Degraded(description, data: results),
            EncinaHealthStatus.Unhealthy => AspNetHealthCheckResult.Unhealthy(description, data: results),
            _ => AspNetHealthCheckResult.Unhealthy($"Unknown health status: {status}")
        };
}
