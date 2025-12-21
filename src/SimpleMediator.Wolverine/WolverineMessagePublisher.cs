using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolverine;
using static LanguageExt.Prelude;

namespace SimpleMediator.Wolverine;

/// <summary>
/// Wolverine-based implementation of the message publisher.
/// </summary>
#pragma warning disable CA1848 // Use LoggerMessage delegates
public sealed class WolverineMessagePublisher : IWolverineMessagePublisher
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<WolverineMessagePublisher> _logger;
    private readonly SimpleMediatorWolverineOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WolverineMessagePublisher"/> class.
    /// </summary>
    /// <param name="messageBus">The Wolverine message bus.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public WolverineMessagePublisher(
        IMessageBus messageBus,
        ILogger<WolverineMessagePublisher> logger,
        IOptions<SimpleMediatorWolverineOptions> options)
    {
        ArgumentNullException.ThrowIfNull(messageBus);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _messageBus = messageBus;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            _logger.LogDebug(
                "Publishing message of type {MessageType} via Wolverine",
                typeof(TMessage).Name);

            await _messageBus.PublishAsync(message).ConfigureAwait(false);

            _logger.LogDebug(
                "Successfully published message of type {MessageType}",
                typeof(TMessage).Name);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish message of type {MessageType}",
                typeof(TMessage).Name);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "WOLVERINE_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish message of type {typeof(TMessage).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> SendToEndpointAsync<TMessage>(
        string endpointName,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(endpointName);
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            _logger.LogDebug(
                "Sending message of type {MessageType} to endpoint {Endpoint}",
                typeof(TMessage).Name,
                endpointName);

            await _messageBus.EndpointFor(endpointName).SendAsync(message).ConfigureAwait(false);

            _logger.LogDebug(
                "Successfully sent message of type {MessageType} to endpoint {Endpoint}",
                typeof(TMessage).Name,
                endpointName);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send message of type {MessageType} to endpoint {Endpoint}",
                typeof(TMessage).Name,
                endpointName);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "WOLVERINE_SEND_FAILED",
                    ex,
                    $"Failed to send message of type {typeof(TMessage).Name} to endpoint {endpointName}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> ScheduleAsync<TMessage>(
        TMessage message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            _logger.LogDebug(
                "Scheduling message of type {MessageType} for {ScheduledTime}",
                typeof(TMessage).Name,
                scheduledTime);

            await _messageBus.ScheduleAsync(message, scheduledTime).ConfigureAwait(false);

            _logger.LogDebug(
                "Successfully scheduled message of type {MessageType} for {ScheduledTime}",
                typeof(TMessage).Name,
                scheduledTime);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to schedule message of type {MessageType} for {ScheduledTime}",
                typeof(TMessage).Name,
                scheduledTime);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "WOLVERINE_SCHEDULE_FAILED",
                    ex,
                    $"Failed to schedule message of type {typeof(TMessage).Name} for {scheduledTime}."));
        }
    }
}
#pragma warning restore CA1848
