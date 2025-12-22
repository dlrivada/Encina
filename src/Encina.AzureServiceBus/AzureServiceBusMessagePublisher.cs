using System.Text.Json;
using Azure.Messaging.ServiceBus;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.AzureServiceBus;

/// <summary>
/// Azure Service Bus-based implementation of the message publisher.
/// </summary>
public sealed class AzureServiceBusMessagePublisher : IAzureServiceBusMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<AzureServiceBusMessagePublisher> _logger;
    private readonly EncinaAzureServiceBusOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureServiceBusMessagePublisher"/> class.
    /// </summary>
    /// <param name="client">The Azure Service Bus client.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public AzureServiceBusMessagePublisher(
        ServiceBusClient client,
        ILogger<AzureServiceBusMessagePublisher> logger,
        IOptions<EncinaAzureServiceBusOptions> options)
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
            Log.SendingToQueue(_logger, typeof(TMessage).Name, effectiveQueueName);

            await using var sender = _client.CreateSender(effectiveQueueName);

            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(message))
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                Subject = typeof(TMessage).Name
            };

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);

            Log.SuccessfullySentToQueue(_logger, typeof(TMessage).Name, effectiveQueueName);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToSendToQueue(_logger, ex, typeof(TMessage).Name, effectiveQueueName);

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
            Log.PublishingToTopic(_logger, typeof(TMessage).Name, effectiveTopicName);

            await using var sender = _client.CreateSender(effectiveTopicName);

            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(message))
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                Subject = typeof(TMessage).Name
            };

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);

            Log.SuccessfullyPublishedToTopic(_logger, typeof(TMessage).Name, effectiveTopicName);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToPublishToTopic(_logger, ex, typeof(TMessage).Name, effectiveTopicName);

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
            Log.SchedulingMessage(_logger, typeof(TMessage).Name, scheduledEnqueueTime, effectiveQueueName);

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

            Log.SuccessfullyScheduledMessage(_logger, typeof(TMessage).Name, sequenceNumber);

            return Right<MediatorError, long>(sequenceNumber);
        }
        catch (Exception ex)
        {
            Log.FailedToScheduleMessage(_logger, ex, typeof(TMessage).Name);

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
            Log.CancellingScheduledMessage(_logger, sequenceNumber, effectiveQueueName);

            await using var sender = _client.CreateSender(effectiveQueueName);
            await sender.CancelScheduledMessageAsync(sequenceNumber, cancellationToken).ConfigureAwait(false);

            Log.SuccessfullyCancelledMessage(_logger, sequenceNumber);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToCancelMessage(_logger, ex, sequenceNumber);

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
