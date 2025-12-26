using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.AspNetCore.Health;

/// <summary>
/// A health check that always returns healthy with a message indicating
/// that no health checks are configured.
/// </summary>
/// <remarks>
/// This is used as a fallback when a required service (like IModuleRegistry)
/// is not registered in the DI container.
/// </remarks>
internal sealed class EmptyHealthCheck : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            HealthCheckResult.Healthy("No health checks configured (missing required services)"));
    }
}
