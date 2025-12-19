using Microsoft.Extensions.Logging;

namespace SimpleMediator.Hangfire;

/// <summary>
/// Adapter that executes INotification as a Hangfire background job.
/// </summary>
/// <typeparam name="TNotification">The type of notification to publish.</typeparam>
public sealed class HangfireNotificationJobAdapter<TNotification>
    where TNotification : INotification
{
    private readonly IMediator _mediator;
    private readonly ILogger<HangfireNotificationJobAdapter<TNotification>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireNotificationJobAdapter{TNotification}"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance.</param>
    /// <param name="logger">The logger instance.</param>
    public HangfireNotificationJobAdapter(
        IMediator mediator,
        ILogger<HangfireNotificationJobAdapter<TNotification>> logger)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(logger);

        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Publishes the notification through the mediator as a Hangfire job.
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
            _logger.LogInformation(
                "Publishing Hangfire notification job for {NotificationType}",
                typeof(TNotification).Name);

            await _mediator.Publish(notification, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Hangfire notification job completed successfully for {NotificationType}",
                typeof(TNotification).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception in Hangfire notification job for {NotificationType}",
                typeof(TNotification).Name);

            throw;
        }
    }
}
