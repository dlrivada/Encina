using Microsoft.Extensions.Logging;
using Quartz;

namespace SimpleMediator.Quartz;

/// <summary>
/// Quartz job that publishes a SimpleMediator notification.
/// </summary>
/// <typeparam name="TNotification">The type of notification to publish.</typeparam>
[DisallowConcurrentExecution]
public sealed class QuartzNotificationJob<TNotification> : IJob
    where TNotification : INotification
{
    private readonly IMediator _mediator;
    private readonly ILogger<QuartzNotificationJob<TNotification>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuartzNotificationJob{TNotification}"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance.</param>
    /// <param name="logger">The logger instance.</param>
    public QuartzNotificationJob(
        IMediator mediator,
        ILogger<QuartzNotificationJob<TNotification>> logger)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(logger);

        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Executes the Quartz job by publishing the notification through the mediator.
    /// </summary>
    /// <param name="context">The Quartz job execution context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var notificationObj = context.JobDetail.JobDataMap.Get(QuartzConstants.NotificationKey);

        if (notificationObj is not TNotification notification)
        {
            _logger.LogError(
                "Notification not found in JobDataMap for job {JobKey}",
                context.JobDetail.Key);

            throw new JobExecutionException($"Notification of type {typeof(TNotification).Name} not found in JobDataMap");
        }

        try
        {
            _logger.LogInformation(
                "Publishing Quartz notification job {JobKey} for {NotificationType}",
                context.JobDetail.Key,
                typeof(TNotification).Name);

            await _mediator.Publish(notification, context.CancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Quartz notification job {JobKey} completed successfully for {NotificationType}",
                context.JobDetail.Key,
                typeof(TNotification).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception in Quartz notification job {JobKey} for {NotificationType}",
                context.JobDetail.Key,
                typeof(TNotification).Name);

            throw new JobExecutionException(ex);
        }
    }
}
