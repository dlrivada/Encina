using System.Text;
using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using static LanguageExt.Prelude;

namespace SimpleMediator.RabbitMQ;

/// <summary>
/// RabbitMQ-based implementation of the message publisher.
/// </summary>
#pragma warning disable CA1848 // Use LoggerMessage delegates
public sealed class RabbitMQMessagePublisher : IRabbitMQMessagePublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMQMessagePublisher> _logger;
    private readonly SimpleMediatorRabbitMQOptions _options;

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
        IOptions<SimpleMediatorRabbitMQOptions> options)
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
    public async ValueTask<Either<MediatorError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        string? routingKey = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            var effectiveRoutingKey = routingKey ?? typeof(TMessage).Name;

            _logger.LogDebug(
                "Publishing message of type {MessageType} to exchange {Exchange} with routing key {RoutingKey}",
                typeof(TMessage).Name,
                _options.ExchangeName,
                effectiveRoutingKey);

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
                    "RABBITMQ_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish message of type {typeof(TMessage).Name}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, Unit>> SendToQueueAsync<TMessage>(
        string queueName,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(queueName);
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            _logger.LogDebug(
                "Sending message of type {MessageType} to queue {Queue}",
                typeof(TMessage).Name,
                queueName);

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

            _logger.LogDebug(
                "Successfully sent message of type {MessageType} to queue {Queue}",
                typeof(TMessage).Name,
                queueName);

            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send message of type {MessageType} to queue {Queue}",
                typeof(TMessage).Name,
                queueName);

            return Left<MediatorError, Unit>(
                MediatorErrors.FromException(
                    "RABBITMQ_SEND_FAILED",
                    ex,
                    $"Failed to send message of type {typeof(TMessage).Name} to queue {queueName}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, TResponse>> RequestAsync<TRequest, TResponse>(
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

        return Left<MediatorError, TResponse>(
            MediatorErrors.Create(
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
