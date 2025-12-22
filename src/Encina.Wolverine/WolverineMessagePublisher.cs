using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolverine;
using static LanguageExt.Prelude;

namespace Encina.Wolverine;

/// <summary>
/// Wolverine-based implementation of the message publisher.
/// </summary>
public sealed class WolverineMessagePublisher : IWolverineMessagePublisher
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<WolverineMessagePublisher> _logger;
    private readonly EncinaWolverineOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WolverineMessagePublisher"/> class.
    /// </summary>
    /// <param name="messageBus">The Wolverine message bus.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public WolverineMessagePublisher(
        IMessageBus messageBus,
        ILogger<WolverineMessagePublisher> logger,
        IOptions<EncinaWolverineOptions> options)
    {
        ArgumentNullException.ThrowIfNull(messageBus);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _messageBus = messageBus;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            Log.PublishingMessage(_logger, typeof(TMessage).Name);

            await _messageBus.PublishAsync(message).ConfigureAwait(false);

            Log.SuccessfullyPublishedMessage(_logger, typeof(TMessage).Name);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToPublishMessage(_logger, ex, typeof(TMessage).Name);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    "WOLVERINE_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish message of type {typeof(TMessage).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> SendToEndpointAsync<TMessage>(
        string endpointName,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(endpointName);
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            Log.SendingToEndpoint(_logger, typeof(TMessage).Name, endpointName);

            await _messageBus.EndpointFor(endpointName).SendAsync(message).ConfigureAwait(false);

            Log.SuccessfullySentToEndpoint(_logger, typeof(TMessage).Name, endpointName);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToSendToEndpoint(_logger, ex, typeof(TMessage).Name, endpointName);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    "WOLVERINE_SEND_FAILED",
                    ex,
                    $"Failed to send message of type {typeof(TMessage).Name} to endpoint {endpointName}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> ScheduleAsync<TMessage>(
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            Log.SchedulingMessage(_logger, typeof(TMessage).Name, scheduledTime);

            await _messageBus.ScheduleAsync(message, scheduledTime).ConfigureAwait(false);

            Log.SuccessfullyScheduledMessage(_logger, typeof(TMessage).Name, scheduledTime);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToScheduleMessage(_logger, ex, typeof(TMessage).Name, scheduledTime);

            return Left<EncinaError, Unit>(
                EncinaErrors.FromException(
                    "WOLVERINE_SCHEDULE_FAILED",
                    ex,
                    $"Failed to schedule message of type {typeof(TMessage).Name} for {scheduledTime}."));
        }
    }
}
