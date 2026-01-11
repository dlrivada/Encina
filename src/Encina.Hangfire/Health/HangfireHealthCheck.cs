using Encina.Messaging.Health;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Hangfire.Health;

/// <summary>
/// Health check for Hangfire job scheduler.
/// </summary>
public sealed class HangfireHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the Hangfire health check.
    /// </summary>
    public const string DefaultName = "encina-hangfire";

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve JobStorage from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public HangfireHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "scheduling", "hangfire", "ready"])
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var storage = _serviceProvider.GetService<JobStorage>() ?? JobStorage.Current;

            if (storage == null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} storage is not configured"));
            }

            using var connection = storage.GetConnection();
            var monitoringApi = storage.GetMonitoringApi();

            // Get basic stats to verify connectivity
            var stats = monitoringApi.GetStatistics();

            var data = new Dictionary<string, object>
            {
                ["servers"] = stats.Servers,
                ["queues"] = stats.Queues,
                ["scheduled"] = stats.Scheduled,
                ["enqueued"] = stats.Enqueued,
                ["processing"] = stats.Processing,
                ["succeeded"] = stats.Succeeded,
                ["failed"] = stats.Failed
            };

            if (stats.Servers == 0)
            {
                return Task.FromResult(HealthCheckResult.Degraded($"{Name} has no active servers", data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"{Name} is operational", data));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} health check failed: {ex.Message}"));
        }
    }
}
