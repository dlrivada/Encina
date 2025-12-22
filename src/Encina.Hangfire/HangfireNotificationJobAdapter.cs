using Microsoft.Extensions.Logging;

namespace Encina.Hangfire;

/// <summary>
/// Adapter that executes INotification as a Hangfire background job.
/// </summary>
/// <typeparam name="TNotification">The type of notification to publish.</typeparam>
public sealed class HangfireNotificationJobAdapter<TNotification>
    where TNotification : INotification
{
    private readonly IEncina _Encina;
    private readonly ILogger<HangfireNotificationJobAdapter<TNotification>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireNotificationJobAdapter{TNotification}"/> class.
    /// </summary>
    /// <param name="Encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    public HangfireNotificationJobAdapter(
        IEncina Encina,
        ILogger<HangfireNotificationJobAdapter<TNotification>> logger)
    {
        ArgumentNullException.ThrowIfNull(Encina);
        ArgumentNullException.ThrowIfNull(logger);

        _Encina = Encina;
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

            await _Encina.Publish(notification, cancellationToken)
                .ConfigureAwait(false);

            Log.NotificationJobCompleted(_logger, typeof(TNotification).Name);
        }
        catch (Exception ex)
        {
            Log.NotificationJobException(_logger, ex, typeof(TNotification).Name);

            throw;
        }
    }
}
