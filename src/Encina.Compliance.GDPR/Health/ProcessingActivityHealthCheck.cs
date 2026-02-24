using Encina.Compliance.GDPR.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.GDPR.Health;

/// <summary>
/// Health check that verifies the processing activity registry is properly configured and accessible.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies:
/// <list type="bullet">
/// <item><description>The <see cref="IProcessingActivityRegistry"/> is resolvable from DI.</description></item>
/// <item><description>The registry is populated with at least one processing activity.</description></item>
/// <item><description>The registry is accessible (no connection or query errors).</description></item>
/// </list>
/// </para>
/// <para>
/// Returns <see cref="HealthCheckResult.Healthy"/> when the registry is resolvable and populated,
/// <see cref="HealthCheckResult.Degraded"/> when warnings exist (e.g., empty registry),
/// and <see cref="HealthCheckResult.Unhealthy"/> when the registry is not registered or inaccessible.
/// </para>
/// </remarks>
public sealed class ProcessingActivityHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-processing-activity";

    private static readonly string[] DefaultTags = ["encina", "gdpr", "processing-activity", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessingActivityHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessingActivityHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve processing activity services.</param>
    /// <param name="logger">The logger instance.</param>
    public ProcessingActivityHealthCheck(IServiceProvider serviceProvider, ILogger<ProcessingActivityHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default tags for the processing activity health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var warnings = new List<string>();

        using var scope = _serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // 1. Verify registry is resolvable
        var registry = scopedProvider.GetService<IProcessingActivityRegistry>();
        if (registry is null)
        {
            return HealthCheckResult.Unhealthy(
                "IProcessingActivityRegistry is not registered. "
                + "Call AddEncinaProcessingActivity*() in DI setup (e.g., AddEncinaProcessingActivityEFCore, AddEncinaProcessingActivityADOSqlite).",
                data: data);
        }

        data["registry_type"] = registry.GetType().Name;

        // 2. Count registered activities
        try
        {
            var activitiesResult = await registry.GetAllActivitiesAsync(cancellationToken).ConfigureAwait(false);

            activitiesResult.Match(
                Right: activities =>
                {
                    data["activities_count"] = activities.Count;

                    if (activities.Count == 0)
                    {
                        warnings.Add(
                            "No processing activities registered. "
                            + "Ensure request types are decorated with [ProcessingActivity] or registered manually.");
                    }
                },
                Left: error =>
                {
                    warnings.Add($"Failed to query processing activity registry: {error.Message}");
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded(
                "Failed to access IProcessingActivityRegistry.",
                exception: ex,
                data: data);
        }

        _logger.ProcessingActivityHealthCheckCompleted(
            warnings.Count == 0 ? "Healthy" : "Degraded",
            data.TryGetValue("activities_count", out var count) ? (int)count : 0);

        if (warnings.Count > 0)
        {
            data["warnings"] = warnings;
            return HealthCheckResult.Degraded(
                $"Processing activity infrastructure is partially configured: {string.Join("; ", warnings)}",
                data: data);
        }

        return HealthCheckResult.Healthy(
            "Processing activity registry is fully configured and populated.",
            data: data);
    }
}
