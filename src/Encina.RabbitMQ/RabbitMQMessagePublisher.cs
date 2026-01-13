using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using static LanguageExt.Prelude;

namespace Encina.RabbitMQ;

/// <summary>
/// RabbitMQ-based implementation of the message publisher.
/// </summary>
public sealed class RabbitMQMessagePublisher : IRabbitMQMessagePublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMQMessagePublisher> _logger;
    private readonly EncinaRabbitMQOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQMessagePublisher"/> class.
    /// </summary>
    /// <param name="connection">The RabbitMQ connection.</param>
    /// <param name="channel">The RabbitMQ channel.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public RabbitMQMessagePublisher(
        IConnection connection,
        IChannel channel,
        ILogger<RabbitMQMessagePublisher> logger,
        IOptions<EncinaRabbitMQOptions> options)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _connection = connection;
        _channel = channel;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        string? routingKey = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            var effectiveRoutingKey = routingKey ?? typeof(TMessage).Name;

            Log.PublishingMessage(_logger, typeof(TMessage).Name, _options.ExchangeName, effectiveRoutingKey);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = _options.Durable ? DeliveryModes.Persistent : DeliveryModes.Transient,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

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
            Log.FailedToPublishMessage(_logger, ex, typeof(TMessage).Name);

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
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(queueName);
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            Log.SendingToQueue(_logger, typeof(TMessage).Name, queueName);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = _options.Durable ? DeliveryModes.Persistent : DeliveryModes.Transient,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

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
            Log.FailedToSendToQueue(_logger, ex, typeof(TMessage).Name, queueName);

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
        await Task.CompletedTask;

        return Left<EncinaError, TResponse>( // NOSONAR S6966: LanguageExt Left is a pure function
            EncinaErrors.Create(
                "RABBITMQ_RPC_NOT_IMPLEMENTED",
                "RPC pattern is not yet implemented. Use MassTransit for request/reply patterns."));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync().ConfigureAwait(false);
        await _connection.CloseAsync().ConfigureAwait(false);
    }
}
