using Encina.Messaging.Recoverability;
using Microsoft.Extensions.Logging;

namespace Encina.Hangfire;

/// <summary>
/// Adapter that executes INotification as a Hangfire background job.
/// </summary>
/// <typeparam name="TNotification">The type of notification to publish.</typeparam>
/// <remarks>
/// <para>
/// A <c>Left</c> result from <see cref="IEncina.Publish{TNotification}"/> is turned into an exception
/// exactly like <see cref="HangfireRequestJobAdapter{TRequest, TResponse}"/> does:
/// <see cref="OperationCanceledException"/> for a cancelled job (an Encina <c>*.cancelled</c> error such
/// as <see cref="EncinaErrorCodes.NotificationCancelled"/> with the job's token cancelled),
/// <see cref="EncinaJobPermanentFailureException"/> for a permanent failure, and
/// <see cref="EncinaJobFailedException"/> for any other failure.
/// </para>
/// </remarks>
public sealed class HangfireNotificationJobAdapter<TNotification>
    where TNotification : INotification
{
    private readonly IEncina _encina;
    private readonly ILogger<HangfireNotificationJobAdapter<TNotification>> _logger;
    private readonly IErrorClassifier _errorClassifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireNotificationJobAdapter{TNotification}"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errorClassifier">
    /// The classifier that decides whether a failure is permanent or transient. When <c>null</c>,
    /// <see cref="DefaultErrorClassifier"/> is used.
    /// </param>
    public HangfireNotificationJobAdapter(
        IEncina encina,
        ILogger<HangfireNotificationJobAdapter<TNotification>> logger,
        IErrorClassifier? errorClassifier = null)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(logger);

        _encina = encina;
        _logger = logger;
        _errorClassifier = errorClassifier ?? new DefaultErrorClassifier();
    }

    /// <summary>
    /// Publishes the notification through the Encina as a Hangfire job.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">The job was cancelled through <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="EncinaJobPermanentFailureException">A handler failed with a permanent error.</exception>
    /// <exception cref="EncinaJobFailedException">A handler failed with a transient or unclassified error.</exception>
    public async Task PublishAsync(
        TNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = typeof(TNotification).Name;
        LanguageExt.Either<EncinaError, LanguageExt.Unit> result;

        try
        {
            Log.PublishingNotificationJob(_logger, notificationType);

            result = await _encina.Publish(notification, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.NotificationJobException(_logger, ex, notificationType);

            throw;
        }

        result.Match(
            Right: _ => Log.NotificationJobCompleted(_logger, notificationType),
            Left: error => throw ToException(error, notificationType, cancellationToken));
    }

    private Exception ToException(EncinaError error, string notificationType, CancellationToken cancellationToken)
    {
        if (JobFailure.IsJobCancellation(error, cancellationToken))
        {
            Log.NotificationJobCancelled(_logger, notificationType);
            return JobFailure.Cancelled(error, cancellationToken);
        }

        var classification = JobFailure.Classify(error, _errorClassifier);
        Log.NotificationJobFailed(_logger, notificationType, error.GetCode().IfNone("encina.unknown"), classification.ToString());
        return JobFailure.Failed(error, classification);
    }
}
