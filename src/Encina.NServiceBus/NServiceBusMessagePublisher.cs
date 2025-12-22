using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus;
using static LanguageExt.Prelude;

namespace Encina.NServiceBus;

/// <summary>
/// NServiceBus-based implementation of the message publisher.
/// </summary>
#pragma warning disable CA2016 // Forward CancellationToken to methods - NServiceBus doesn't accept CancellationToken
public sealed class NServiceBusMessagePublisher : INServiceBusMessagePublisher
{
    private readonly IMessageSession _messageSession;
    private readonly ILogger<NServiceBusMessagePublisher> _logger;
    private readonly EncinaNServiceBusOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="NServiceBusMessagePublisher"/> class.
    /// </summary>
    /// <param name="messageSession">The NServiceBus message session.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public NServiceBusMessagePublisher(
        IMessageSession messageSession,
        ILogger<NServiceBusMessagePublisher> logger,
        IOptions<EncinaNServiceBusOptions> options)
    {
        ArgumentNullException.ThrowIfNull(messageSession);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _messageSession = messageSession;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> SendAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            Log.SendingCommand(_logger, typeof(TCommand).Name);

            var sendOptions = new SendOptions();
            await _messageSession.Send(command, sendOptions).ConfigureAwait(false);

            Log.SuccessfullySentCommand(_logger, typeof(TCommand).Name);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToSendCommand(_logger, ex, typeof(TCommand).Name);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "NSERVICEBUS_SEND_FAILED",
                    ex,
                    $"Failed to send command of type {typeof(TCommand).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> PublishAsync<TEvent>(
        TEvent eventMessage,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(eventMessage);

        try
        {
            Log.PublishingEvent(_logger, typeof(TEvent).Name);

            var publishOptions = new PublishOptions();
            await _messageSession.Publish(eventMessage, publishOptions).ConfigureAwait(false);

            Log.SuccessfullyPublishedEvent(_logger, typeof(TEvent).Name);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToPublishEvent(_logger, ex, typeof(TEvent).Name);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "NSERVICEBUS_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish event of type {typeof(TEvent).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> ScheduleAsync<TMessage>(
        TMessage message,
        DateTimeOffset deliveryTime,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            Log.SchedulingMessage(_logger, typeof(TMessage).Name, deliveryTime);

            var sendOptions = new SendOptions();
            sendOptions.DoNotDeliverBefore(deliveryTime);

            await _messageSession.Send(message, sendOptions).ConfigureAwait(false);

            Log.SuccessfullyScheduledMessage(_logger, typeof(TMessage).Name, deliveryTime);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToScheduleMessage(_logger, ex, typeof(TMessage).Name);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "NSERVICEBUS_SCHEDULE_FAILED",
                    ex,
                    $"Failed to schedule message of type {typeof(TMessage).Name} for {deliveryTime}."));
        }
    }
}
#pragma warning restore CA2016
