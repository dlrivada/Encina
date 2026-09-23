using Microsoft.Extensions.Logging;

namespace Encina.Hangfire;

/// <summary>
/// Adapter that executes INotification as a Hangfire background job.
/// </summary>
/// <typeparam name="TNotification">The type of notification to publish.</typeparam>
public sealed class HangfireNotificationJobAdapter<TNotification>
    where TNotification : INotification
{
    private readonly IEncina _encina;
    private readonly ILogger<HangfireNotificationJobAdapter<TNotification>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireNotificationJobAdapter{TNotification}"/> class.
    /// </summary>
    /// <param name="encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    public HangfireNotificationJobAdapter(
        IEncina encina,
        ILogger<HangfireNotificationJobAdapter<TNotification>> logger)
    {
        ArgumentNullException.ThrowIfNull(encina);
        ArgumentNullException.ThrowIfNull(logger);

        _encina = encina;
        _logger = logger;
    }

    /// <summary>
    /// Publishes the notification through the Encina as a Hangfire job.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublishAsync(
        TNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        try
        {
            Log.PublishingNotificationJob(_logger, typeof(TNotification).Name);

            var result = await _encina.Publish(notification, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: _ => Log.NotificationJobCompleted(_logger, typeof(TNotification).Name),
                Left: error =>
                {
                    Log.NotificationJobFailed(_logger, typeof(TNotification).Name, error.Message);

                    // Throw so Hangfire records the job as failed and applies its retry policy,
                    // instead of silently discarding the Left result.
                    throw new EncinaJobFailedException(error);
                });
        }
        catch (Exception ex) when (ex is not EncinaJobFailedException)
        {
            Log.NotificationJobException(_logger, ex, typeof(TNotification).Name);

            throw;
        }
    }
}
