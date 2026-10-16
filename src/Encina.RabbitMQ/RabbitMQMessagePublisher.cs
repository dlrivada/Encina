using System.Text.Json;
using Encina.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using static LanguageExt.Prelude;

namespace Encina.RabbitMQ;

/// <summary>
/// RabbitMQ-based implementation of the message publisher.
/// </summary>
/// <remarks>
/// The publisher does not own the injected <see cref="IChannel"/>; the DI container owns
/// and disposes it, so ending a scope that resolved the publisher leaves the channel usable.
/// </remarks>
public sealed class RabbitMQMessagePublisher : IRabbitMQMessagePublisher
{
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMQMessagePublisher> _logger;
    private readonly EncinaRabbitMQOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQMessagePublisher"/> class.
    /// </summary>
    /// <param name="channel">The shared RabbitMQ channel (owned by the DI container).</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="timeProvider">The time provider. If <c>null</c>, <see cref="TimeProvider.System"/> is used.</param>
    public RabbitMQMessagePublisher(
        IChannel channel,
        ILogger<RabbitMQMessagePublisher> logger,
        IOptions<EncinaRabbitMQOptions> options,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _channel = channel;
        _logger = logger;
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private BasicProperties CreateProperties(string? messageId) => new()
    {
        ContentType = "application/json",
        DeliveryMode = _options.Durable ? DeliveryModes.Persistent : DeliveryModes.Transient,
        MessageId = string.IsNullOrEmpty(messageId) ? Guid.NewGuid().ToString() : messageId,
        Timestamp = new AmqpTimestamp(_timeProvider.GetUtcNow().ToUnixTimeSeconds())
    };

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        string? routingKey = null,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            var effectiveRoutingKey = routingKey ?? typeof(TMessage).Name;

            Log.PublishingMessage(_logger, typeof(TMessage).Name, _options.ExchangeName, effectiveRoutingKey);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = CreateProperties(messageId);

            await _channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: effectiveRoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            Log.SuccessfullyPublishedMessage(_logger, typeof(TMessage).Name);

            return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
        }
        catch (Exception ex)
        {
            Log.FailedToPublishMessage(_logger, ex.ForLogging(), typeof(TMessage).Name);

            return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    "RABBITMQ_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish message of type {typeof(TMessage).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> SendToQueueAsync<TMessage>(
        string queueName,
        TMessage message,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(queueName);
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            Log.SendingToQueue(_logger, typeof(TMessage).Name, queueName);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = CreateProperties(messageId);

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            Log.SuccessfullySentToQueue(_logger, typeof(TMessage).Name, queueName);

            return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
        }
        catch (Exception ex)
        {
            Log.FailedToSendToQueue(_logger, ex.ForLogging(), typeof(TMessage).Name, queueName);

            return Left<EncinaError, Unit>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
                    "RABBITMQ_SEND_FAILED",
                    ex,
                    $"Failed to send message of type {typeof(TMessage).Name} to queue {queueName}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> RequestAsync<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(request);

        // RPC pattern implementation would go here
        // For now, return a not implemented error
        return Left<EncinaError, TResponse>( // NOSONAR S6966: LanguageExt Left is a pure function
            EncinaErrors.Create(
                "RABBITMQ_RPC_NOT_IMPLEMENTED",
                "RPC pattern is not yet implemented. Use MassTransit for request/reply patterns."));
    }
}
