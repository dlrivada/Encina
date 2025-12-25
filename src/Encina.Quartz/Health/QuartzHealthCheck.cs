using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Encina.Quartz.Health;

/// <summary>
/// Health check for Quartz.NET job scheduler.
/// </summary>
public sealed class QuartzHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the Quartz health check.
    /// </summary>
    public const string DefaultName = "encina-quartz";

    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuartzHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve ISchedulerFactory from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public QuartzHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "scheduling", "quartz", "ready"])
    {
        _serviceProvider = serviceProvider;
        _options = options ?? new ProviderHealthCheckOptions();
    }

    /// <inheritdoc/>
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(_options.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var schedulerFactory = _serviceProvider.GetRequiredService<ISchedulerFactory>();
            var scheduler = await schedulerFactory.GetScheduler(linkedCts.Token).ConfigureAwait(false);

            if (scheduler == null)
            {
                return HealthCheckResult.Unhealthy($"{Name} scheduler not available");
            }

            // Build basic data first from scheduler properties
            var data = new Dictionary<string, object>
            {
                ["scheduler_name"] = scheduler.SchedulerName,
                ["is_started"] = scheduler.IsStarted,
                ["is_shutdown"] = scheduler.IsShutdown,
                ["is_standby"] = scheduler.InStandbyMode
            };

            // Check status before calling GetMetaData (which may fail)
            if (scheduler.IsShutdown)
            {
                return HealthCheckResult.Unhealthy($"{Name} scheduler is shut down", data: data);
            }

            if (scheduler.InStandbyMode)
            {
                return HealthCheckResult.Degraded($"{Name} scheduler is in standby mode", data: data);
            }

            if (!scheduler.IsStarted)
            {
                return HealthCheckResult.Degraded($"{Name} scheduler is not started", data: data);
            }

            // Only get metadata for operational schedulers
            var metadata = await scheduler.GetMetaData(linkedCts.Token).ConfigureAwait(false);
            data["running_since"] = metadata.RunningSince?.ToString("O") ?? "not started";
            data["jobs_executed"] = metadata.NumberOfJobsExecuted;

            return HealthCheckResult.Healthy($"{Name} is operational", data);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"{Name} health check timed out after {_options.Timeout.TotalSeconds}s");
        }
        catch (SchedulerException ex)
        {
            return HealthCheckResult.Unhealthy($"{Name} health check failed: {ex.Message}");
        }
    }
}
