using System.Text.Json;
using Azure.Messaging.ServiceBus;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace SimpleMediator.AzureServiceBus;

/// <summary>
/// Azure Service Bus-based implementation of the message publisher.
/// </summary>
#pragma warning disable CA1848 // Use LoggerMessage delegates
public sealed class AzureServiceBusMessagePublisher : IAzureServiceBusMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<AzureServiceBusMessagePublisher> _logger;
    private readonly SimpleMediatorAzureServiceBusOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureServiceBusMessagePublisher"/> class.
    /// </summary>
    /// <param name="client">The Azure Service Bus client.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public AzureServiceBusMessagePublisher(
        ServiceBusClient client,
        ILogger<AzureServiceBusMessagePublisher> logger,
        IOptions<SimpleMediatorAzureServiceBusOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> SendToQueueAsync<TMessage>(
        TMessage message,
        string? queueName = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var effectiveQueueName = queueName ?? _options.DefaultQueueName;

        try
        {
            _logger.LogDebug(
                "Sending message of type {MessageType} to queue {Queue}",
                typeof(TMessage).Name,
                effectiveQueueName);

            await using var sender = _client.CreateSender(effectiveQueueName);

            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(message))
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                Subject = typeof(TMessage).Name
            };

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Successfully sent message of type {MessageType} to queue {Queue}",
                typeof(TMessage).Name,
                effectiveQueueName);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send message of type {MessageType} to queue {Queue}",
                typeof(TMessage).Name,
                effectiveQueueName);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "AZURE_SB_SEND_FAILED",
                    ex,
                    $"Failed to send message of type {typeof(TMessage).Name} to queue {effectiveQueueName}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> PublishToTopicAsync<TMessage>(
        TMessage message,
        string? topicName = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var effectiveTopicName = topicName ?? _options.DefaultTopicName;

        try
        {
            _logger.LogDebug(
                "Publishing message of type {MessageType} to topic {Topic}",
                typeof(TMessage).Name,
                effectiveTopicName);

            await using var sender = _client.CreateSender(effectiveTopicName);

            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(message))
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                Subject = typeof(TMessage).Name
            };

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Successfully published message of type {MessageType} to topic {Topic}",
                typeof(TMessage).Name,
                effectiveTopicName);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish message of type {MessageType} to topic {Topic}",
                typeof(TMessage).Name,
                effectiveTopicName);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "AZURE_SB_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish message of type {typeof(TMessage).Name} to topic {effectiveTopicName}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, long>> ScheduleAsync<TMessage>(
        TMessage message,
        DateTimeOffset scheduledEnqueueTime,
        string? queueName = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var effectiveQueueName = queueName ?? _options.DefaultQueueName;

        try
        {
            _logger.LogDebug(
                "Scheduling message of type {MessageType} for {ScheduledTime} to queue {Queue}",
                typeof(TMessage).Name,
                scheduledEnqueueTime,
                effectiveQueueName);

            await using var sender = _client.CreateSender(effectiveQueueName);

            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(message))
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                Subject = typeof(TMessage).Name
            };

            var sequenceNumber = await sender.ScheduleMessageAsync(
                serviceBusMessage,
                scheduledEnqueueTime,
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Successfully scheduled message of type {MessageType} with sequence number {SequenceNumber}",
                typeof(TMessage).Name,
                sequenceNumber);

            return Right<MediatorError, long>(sequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to schedule message of type {MessageType}",
                typeof(TMessage).Name);

            return Left<MediatorError, long>(
                MediatorErrors.FromException(
                    "AZURE_SB_SCHEDULE_FAILED",
                    ex,
                    $"Failed to schedule message of type {typeof(TMessage).Name} for {scheduledEnqueueTime}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> CancelScheduledAsync(
        long sequenceNumber,
        string? queueName = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveQueueName = queueName ?? _options.DefaultQueueName;

        try
        {
            _logger.LogDebug(
                "Cancelling scheduled message with sequence number {SequenceNumber} from queue {Queue}",
                sequenceNumber,
                effectiveQueueName);

            await using var sender = _client.CreateSender(effectiveQueueName);
            await sender.CancelScheduledMessageAsync(sequenceNumber, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Successfully cancelled scheduled message with sequence number {SequenceNumber}",
                sequenceNumber);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to cancel scheduled message with sequence number {SequenceNumber}",
                sequenceNumber);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "AZURE_SB_CANCEL_FAILED",
                    ex,
                    $"Failed to cancel scheduled message with sequence number {sequenceNumber}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
    }
}
