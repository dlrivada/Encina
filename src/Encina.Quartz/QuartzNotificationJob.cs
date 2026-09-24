using Encina.Messaging.Recoverability;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Encina.Quartz;

/// <summary>
/// Quartz job that publishes a Encina notification.
/// </summary>
/// <typeparam name="TNotification">The type of notification to publish.</typeparam>
/// <remarks>
/// A <c>Left</c> result from <see cref="IEncina.Publish{TNotification}"/> fails the job exactly like
/// <see cref="QuartzRequestJob{TRequest, TResponse}"/> does: <see cref="OperationCanceledException"/> for a
/// cancelled job (an Encina <c>*.cancelled</c> error such as <see cref="EncinaErrorCodes.NotificationCancelled"/>
/// with the job's token cancelled), otherwise a <see cref="JobExecutionException"/> with
/// <see cref="JobExecutionException.RefireImmediately"/> set to <c>false</c> and the error code and
/// classification in <see cref="Exception.Data"/> (see <see cref="EncinaJobFailureData"/>).
/// </remarks>
[DisallowConcurrentExecution]
public sealed class QuartzNotificationJob<TNotification> : IJob
    where TNotification : INotification
{
    private readonly IEncina _encina;
    private readonly ILogger<QuartzNotificationJob<TNotification>> _logger;
    private readonly IErrorClassifier _errorClassifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuartzNotificationJob{TNotification}"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorClassifier">
    /// The classifier that decides whether a failure is permanent or transient. When <c>null</c>,
    /// <see cref="DefaultErrorClassifier"/> is used.
    /// </param>
    public QuartzNotificationJob(
        IEncina encina,
        ILogger<QuartzNotificationJob<TNotification>> logger,
        IErrorClassifier? errorClassifier = null)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(logger);

        _encina = encina;
        _logger = logger;
        _errorClassifier = errorClassifier ?? new DefaultErrorClassifier();
    }

    /// <summary>
    /// Executes the Quartz job by publishing the notification through the Encina.
    /// </summary>
    /// <param name="context">The Quartz job execution context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">The job was cancelled through the context's cancellation token.</exception>
    /// <exception cref="JobExecutionException">The notification is missing, a handler failed, or a handler threw.</exception>
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.JobDetail.JobDataMap.TryGetValue(QuartzConstants.NotificationKey, out var notificationObj) ||
            notificationObj is not TNotification notification)
        {
            Log.NotificationNotFoundInJobDataMap(_logger, context.JobDetail.Key);

            throw new JobExecutionException($"Notification of type {typeof(TNotification).Name} not found in JobDataMap");
        }

        var notificationType = typeof(TNotification).Name;
        Either<EncinaError, Unit> result;

        try
        {
            Log.PublishingNotificationJob(_logger, context.JobDetail.Key, notificationType);

            result = await _encina.Publish(notification, context.CancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.NotificationJobException(_logger, ex, context.JobDetail.Key, notificationType);

            throw new JobExecutionException(ex);
        }

        result.Match(
            Right: _ => Log.NotificationJobCompleted(_logger, context.JobDetail.Key, notificationType),
            Left: error => throw ToException(error, context, notificationType));
    }

    private Exception ToException(EncinaError error, IJobExecutionContext context, string notificationType)
    {
        if (JobFailure.IsJobCancellation(error, context.CancellationToken))
        {
            Log.NotificationJobCancelled(_logger, context.JobDetail.Key, notificationType);
            return JobFailure.Cancelled(error, context.CancellationToken);
        }

        var classification = JobFailure.Classify(error, _errorClassifier);
        Log.NotificationJobFailed(_logger, context.JobDetail.Key, notificationType, error.GetCode().IfNone("encina.unknown"), classification.ToString());
        return JobFailure.Failed(error, classification);
    }
}
