using LanguageExt;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.MassTransit;

/// <summary>
/// MassTransit consumer that bridges incoming messages to Encina notifications.
/// </summary>
/// <typeparam name="TNotification">The notification type implementing INotification.</typeparam>
public sealed class MassTransitNotificationConsumer<TNotification> : IConsumer<TNotification>
    where TNotification : class, INotification
{
    private readonly IEncina _Encina;
    private readonly ILogger<MassTransitNotificationConsumer<TNotification>> _logger;
    private readonly EncinaMassTransitOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MassTransitNotificationConsumer{TNotification}"/> class.
    /// </summary>
    /// <param name="Encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public MassTransitNotificationConsumer(
        IEncina Encina,
        ILogger<MassTransitNotificationConsumer<TNotification>> logger,
        IOptions<EncinaMassTransitOptions> options)
    {
        ArgumentNullException.ThrowIfNull(Encina);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _Encina = Encina;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Consumes a MassTransit message and publishes it as a Encina notification.
    /// </summary>
    /// <param name="context">The consume context containing the message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Consume(ConsumeContext<TNotification> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var notificationType = typeof(TNotification).Name;

        Log.ConsumingNotification(_logger, notificationType, context.MessageId);

        var result = await _Encina.Publish(context.Message, context.CancellationToken)
            .ConfigureAwait(false);

        result.Match(
            Right: _ =>
            {
                Log.PublishedNotificationWithMessageId(_logger, notificationType, context.MessageId);
            },
            Left: error =>
            {
                Log.FailedToPublishNotification(_logger, notificationType, context.MessageId, error.Message);

                if (_options.ThrowOnEncinaError)
                {
                    throw new EncinaConsumerException(error);
                }
            });
    }
}
