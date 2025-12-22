using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Hangfire;

/// <summary>
/// Extension methods for configuring Encina with Hangfire integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Hangfire integration to the service collection.
    /// Registers job adapters for executing requests and notifications as background jobs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaHangfire(this IServiceCollection services)
    {
        // Register adapters as transient (Hangfire creates new instances per job)
        services.TryAddTransient(typeof(HangfireRequestJobAdapter<,>));
        services.TryAddTransient(typeof(HangfireNotificationJobAdapter<>));

        return services;
    }

    /// <summary>
    /// Enqueues a request to be executed as a Hangfire background job.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    /// <param name="client">The Hangfire background job client.</param>
    /// <param name="request">The request to execute.</param>
    /// <returns>The Hangfire job ID.</returns>
    public static string EnqueueRequest<TRequest, TResponse>(
        this IBackgroundJobClient client,
        TRequest request)
        where TRequest : IRequest<TResponse>
    {
        return client.Enqueue<HangfireRequestJobAdapter<TRequest, TResponse>>(
            adapter => adapter.ExecuteAsync(request, default));
    }

    /// <summary>
    /// Schedules a request to be executed as a Hangfire background job after a delay.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    /// <param name="client">The Hangfire background job client.</param>
    /// <param name="request">The request to execute.</param>
    /// <param name="delay">The delay before execution.</param>
    /// <returns>The Hangfire job ID.</returns>
    public static string ScheduleRequestWithDelay<TRequest, TResponse>(
        this IBackgroundJobClient client,
        TRequest request,
        TimeSpan delay)
        where TRequest : IRequest<TResponse>
    {
        return client.Schedule<HangfireRequestJobAdapter<TRequest, TResponse>>(
            adapter => adapter.ExecuteAsync(request, default),
            delay);
    }

    /// <summary>
    /// Schedules a request to be executed as a Hangfire background job at a specific time.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    /// <param name="client">The Hangfire background job client.</param>
    /// <param name="request">The request to execute.</param>
    /// <param name="enqueueAt">The time to execute the job.</param>
    /// <returns>The Hangfire job ID.</returns>
    public static string ScheduleRequestAt<TRequest, TResponse>(
        this IBackgroundJobClient client,
        TRequest request,
        DateTimeOffset enqueueAt)
        where TRequest : IRequest<TResponse>
    {
        return client.Schedule<HangfireRequestJobAdapter<TRequest, TResponse>>(
            adapter => adapter.ExecuteAsync(request, default),
            enqueueAt);
    }

    /// <summary>
    /// Enqueues a notification to be published as a Hangfire background job.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="client">The Hangfire background job client.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <returns>The Hangfire job ID.</returns>
    public static string EnqueueNotification<TNotification>(
        this IBackgroundJobClient client,
        TNotification notification)
        where TNotification : INotification
    {
        return client.Enqueue<HangfireNotificationJobAdapter<TNotification>>(
            adapter => adapter.PublishAsync(notification, default));
    }

    /// <summary>
    /// Schedules a notification to be published as a Hangfire background job after a delay.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="client">The Hangfire background job client.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="delay">The delay before execution.</param>
    /// <returns>The Hangfire job ID.</returns>
    public static string ScheduleNotificationWithDelay<TNotification>(
        this IBackgroundJobClient client,
        TNotification notification,
        TimeSpan delay)
        where TNotification : INotification
    {
        return client.Schedule<HangfireNotificationJobAdapter<TNotification>>(
            adapter => adapter.PublishAsync(notification, default),
            delay);
    }

    /// <summary>
    /// Schedules a notification to be published as a Hangfire background job at a specific time.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="client">The Hangfire background job client.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="enqueueAt">The time to execute the job.</param>
    /// <returns>The Hangfire job ID.</returns>
    public static string ScheduleNotificationAt<TNotification>(
        this IBackgroundJobClient client,
        TNotification notification,
        DateTimeOffset enqueueAt)
        where TNotification : INotification
    {
        return client.Schedule<HangfireNotificationJobAdapter<TNotification>>(
            adapter => adapter.PublishAsync(notification, default),
            enqueueAt);
    }

    /// <summary>
    /// Adds a recurring request job to Hangfire.
    /// </summary>
    /// <typeparam name="TRequest">The type of request.</typeparam>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    /// <param name="manager">The recurring job manager.</param>
    /// <param name="recurringJobId">The unique identifier for the recurring job.</param>
    /// <param name="request">The request to execute.</param>
    /// <param name="cronExpression">The CRON expression for scheduling.</param>
    /// <param name="options">The recurring job options (optional).</param>
    public static void AddOrUpdateRecurringRequest<TRequest, TResponse>(
        this IRecurringJobManager manager,
        string recurringJobId,
        TRequest request,
        string cronExpression,
        RecurringJobOptions? options = null)
        where TRequest : IRequest<TResponse>
    {
        manager.AddOrUpdate<HangfireRequestJobAdapter<TRequest, TResponse>>(
            recurringJobId,
            adapter => adapter.ExecuteAsync(request, default),
            cronExpression,
            options ?? new RecurringJobOptions());
    }

    /// <summary>
    /// Adds a recurring notification job to Hangfire.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="manager">The recurring job manager.</param>
    /// <param name="recurringJobId">The unique identifier for the recurring job.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cronExpression">The CRON expression for scheduling.</param>
    /// <param name="options">The recurring job options (optional).</param>
    public static void AddOrUpdateRecurringNotification<TNotification>(
        this IRecurringJobManager manager,
        string recurringJobId,
        TNotification notification,
        string cronExpression,
        RecurringJobOptions? options = null)
        where TNotification : INotification
    {
        manager.AddOrUpdate<HangfireNotificationJobAdapter<TNotification>>(
            recurringJobId,
            adapter => adapter.PublishAsync(notification, default),
            cronExpression,
            options ?? new RecurringJobOptions());
    }
}
