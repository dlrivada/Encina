using Microsoft.Extensions.Logging;
using Quartz;

namespace Encina.Quartz;

/// <summary>
/// Quartz job that publishes a Encina notification.
/// </summary>
/// <typeparam name="TNotification">The type of notification to publish.</typeparam>
[DisallowConcurrentExecution]
public sealed class QuartzNotificationJob<TNotification> : IJob
    where TNotification : INotification
{
    private readonly IEncina _encina;
    private readonly ILogger<QuartzNotificationJob<TNotification>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuartzNotificationJob{TNotification}"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    public QuartzNotificationJob(
        IEncina encina,
        ILogger<QuartzNotificationJob<TNotification>> logger)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(logger);

        _encina = encina;
        _logger = logger;
    }

    /// <summary>
    /// Executes the Quartz job by publishing the notification through the Encina.
    /// </summary>
    /// <param name="context">The Quartz job execution context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var notificationObj = context.JobDetail.JobDataMap.Get(QuartzConstants.NotificationKey);

        if (notificationObj is not TNotification notification)
        {
            Log.NotificationNotFoundInJobDataMap(_logger, context.JobDetail.Key);

            throw new JobExecutionException($"Notification of type {typeof(TNotification).Name} not found in JobDataMap");
        }

        try
        {
            Log.PublishingNotificationJob(_logger, context.JobDetail.Key, typeof(TNotification).Name);

            await _encina.Publish(notification, context.CancellationToken)
                .ConfigureAwait(false);

            Log.NotificationJobCompleted(_logger, context.JobDetail.Key, typeof(TNotification).Name);
        }
        catch (Exception ex)
        {
            Log.NotificationJobException(_logger, ex, context.JobDetail.Key, typeof(TNotification).Name);

            throw new JobExecutionException(ex);
        }
    }
}
