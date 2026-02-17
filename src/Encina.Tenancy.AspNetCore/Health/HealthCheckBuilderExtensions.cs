using Encina.Messaging.Health;
using Encina.Tenancy.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AspNetHealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;
using EncinaHealthStatus = Encina.Messaging.Health.HealthStatus;

namespace Encina.Tenancy.AspNetCore.Health;

/// <summary>
/// Extension methods for adding Encina tenancy health checks to ASP.NET Core.
/// </summary>
public static class HealthCheckBuilderExtensions
{
    /// <summary>
    /// Adds the tenant resolution health check.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to "encina-tenancy".</param>
    /// <param name="tags">Additional tags to apply.</param>
    /// <param name="failureStatus">The failure status to use.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This health check verifies that the tenant resolution infrastructure is operational.
    /// Requires <see cref="ITenantProvider"/> to be registered.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddHealthChecks()
    ///     .AddEncinaTenancy();
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddEncinaTenancy(
        this IHealthChecksBuilder builder,
        string name = TenantHealthCheck.DefaultName,
        IEnumerable<string>? tags = null,
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus? failureStatus = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var allTags = CombineTags(tags, "tenancy");

        builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var provider = sp.GetRequiredService<ITenantProvider>();
                var healthCheck = new TenantHealthCheck(provider);
                return new EncinaHealthCheckBridge(healthCheck);
            },
            failureStatus,
            allTags));

        return builder;
    }

    private static readonly string[] DefaultTags = ["encina", "ready"];

    private static string[] CombineTags(IEnumerable<string>? additionalTags, params string[] baseTags)
    {
        var combinedTags = new List<string>(DefaultTags);
        combinedTags.AddRange(baseTags);

        if (additionalTags is not null)
        {
            combinedTags.AddRange(additionalTags);
        }

        return [.. combinedTags.Distinct()];
    }

    /// <summary>
    /// Bridges an <see cref="IEncinaHealthCheck"/> to ASP.NET Core's <see cref="IHealthCheck"/>.
    /// </summary>
    private sealed class EncinaHealthCheckBridge(IEncinaHealthCheck inner) : IHealthCheck
    {
        public async Task<AspNetHealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var result = await inner.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

            return result.Status switch
            {
                EncinaHealthStatus.Healthy => AspNetHealthCheckResult.Healthy(
                    result.Description, ConvertData(result.Data)),
                EncinaHealthStatus.Degraded => AspNetHealthCheckResult.Degraded(
                    result.Description, result.Exception, ConvertData(result.Data)),
                EncinaHealthStatus.Unhealthy => AspNetHealthCheckResult.Unhealthy(
                    result.Description, result.Exception, ConvertData(result.Data)),
                _ => AspNetHealthCheckResult.Unhealthy($"Unknown health status: {result.Status}")
            };
        }

        private static IReadOnlyDictionary<string, object>? ConvertData(IReadOnlyDictionary<string, object> data)
            => data.Count > 0 ? data : null;
    }
}
