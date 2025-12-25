using Encina.Messaging.Health;
using Encina.Quartz.Health;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Encina.Quartz;

/// <summary>
/// Extension methods for configuring Encina with Quartz.NET integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Quartz integration to the service collection.
    /// Configures Quartz to work with Encina requests and notifications.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional Quartz configuration action.</param>
    /// <param name="configureOptions">Optional Encina options configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaQuartz(
        this IServiceCollection services,
        Action<IServiceCollectionQuartzConfigurator>? configure = null,
        Action<EncinaQuartzOptions>? configureOptions = null)
    {
        var options = new EncinaQuartzOptions();
        configureOptions?.Invoke(options);

        services.AddQuartz(quartzConfig =>
        {
            // Allow user customization
            configure?.Invoke(quartzConfig);
        });

        // Add Quartz hosted service
        services.AddQuartzHostedService(opts =>
        {
            opts.WaitForJobsToComplete = true;
        });

        // Register health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(options.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, QuartzHealthCheck>();
        }

        return services;
    }

    /// <summary>
    /// Schedules a request to execute as a Quartz job.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    /// <param name="scheduler">The Quartz scheduler.</param>
    /// <param name="request">The request to execute.</param>
    /// <param name="trigger">The trigger defining when the job should run.</param>
    /// <param name="jobKey">Optional job key. If not provided, a unique key will be generated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scheduled job's trigger key.</returns>
    public static async Task<TriggerKey> ScheduleRequest<TRequest, TResponse>(
        this IScheduler scheduler,
        TRequest request,
        ITrigger trigger,
        JobKey? jobKey = null,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>
    {
        var effectiveJobKey = jobKey ?? new JobKey($"Request-{typeof(TRequest).Name}-{Guid.NewGuid():N}");

        var job = JobBuilder.Create<QuartzRequestJob<TRequest, TResponse>>()
            .WithIdentity(effectiveJobKey)
            .Build();

        job.JobDataMap.Put(QuartzConstants.RequestKey, request!);

        await scheduler.ScheduleJob(job, trigger, cancellationToken).ConfigureAwait(false);

        return trigger.Key;
    }

    /// <summary>
    /// Schedules a notification to be published as a Quartz job.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="scheduler">The Quartz scheduler.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="trigger">The trigger defining when the job should run.</param>
    /// <param name="jobKey">Optional job key. If not provided, a unique key will be generated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scheduled job's trigger key.</returns>
    public static async Task<TriggerKey> ScheduleNotification<TNotification>(
        this IScheduler scheduler,
        TNotification notification,
        ITrigger trigger,
        JobKey? jobKey = null,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var effectiveJobKey = jobKey ?? new JobKey($"Notification-{typeof(TNotification).Name}-{Guid.NewGuid():N}");

        var job = JobBuilder.Create<QuartzNotificationJob<TNotification>>()
            .WithIdentity(effectiveJobKey)
            .Build();

        job.JobDataMap.Put(QuartzConstants.NotificationKey, notification!);

        await scheduler.ScheduleJob(job, trigger, cancellationToken).ConfigureAwait(false);

        return trigger.Key;
    }

    /// <summary>
    /// Adds a job for a request to the Quartz configurator (for use during startup configuration).
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    /// <param name="configurator">The Quartz configurator.</param>
    /// <param name="request">The request to execute.</param>
    /// <param name="configureTrigger">Action to configure the trigger.</param>
    /// <param name="jobKey">Optional job key.</param>
    /// <returns>The Quartz configurator for chaining.</returns>
    public static IServiceCollectionQuartzConfigurator AddRequestJob<TRequest, TResponse>(
        this IServiceCollectionQuartzConfigurator configurator,
        TRequest request,
        Action<ITriggerConfigurator> configureTrigger,
        JobKey? jobKey = null)
        where TRequest : IRequest<TResponse>
    {
        var effectiveJobKey = jobKey ?? new JobKey($"Request-{typeof(TRequest).Name}-{Guid.NewGuid():N}");

        configurator.AddJob<QuartzRequestJob<TRequest, TResponse>>(effectiveJobKey, job =>
        {
            job.StoreDurably();
            // Store request in the JobDetail's JobDataMap
            job.SetJobData(new JobDataMap { { QuartzConstants.RequestKey, request! } });
        });

        configurator.AddTrigger(trigger =>
        {
            trigger.ForJob(effectiveJobKey);
            configureTrigger(trigger);
        });

        return configurator;
    }

    /// <summary>
    /// Adds a job for a notification to the Quartz configurator (for use during startup configuration).
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="configurator">The Quartz configurator.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="configureTrigger">Action to configure the trigger.</param>
    /// <param name="jobKey">Optional job key.</param>
    /// <returns>The Quartz configurator for chaining.</returns>
    public static IServiceCollectionQuartzConfigurator AddNotificationJob<TNotification>(
        this IServiceCollectionQuartzConfigurator configurator,
        TNotification notification,
        Action<ITriggerConfigurator> configureTrigger,
        JobKey? jobKey = null)
        where TNotification : INotification
    {
        var effectiveJobKey = jobKey ?? new JobKey($"Notification-{typeof(TNotification).Name}-{Guid.NewGuid():N}");

        configurator.AddJob<QuartzNotificationJob<TNotification>>(effectiveJobKey, job =>
        {
            job.StoreDurably();
            // Store notification in the JobDetail's JobDataMap
            job.SetJobData(new JobDataMap { { QuartzConstants.NotificationKey, notification! } });
        });

        configurator.AddTrigger(trigger =>
        {
            trigger.ForJob(effectiveJobKey);
            configureTrigger(trigger);
        });

        return configurator;
    }
}
