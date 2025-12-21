using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using static LanguageExt.Prelude;

namespace SimpleMediator.Redis.PubSub;

/// <summary>
/// Redis Pub/Sub-based implementation of the message publisher.
/// </summary>
#pragma warning disable CA1848 // Use LoggerMessage delegates
public sealed class RedisPubSubMessagePublisher : IRedisPubSubMessagePublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisPubSubMessagePublisher> _logger;
    private readonly SimpleMediatorRedisPubSubOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisPubSubMessagePublisher"/> class.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public RedisPubSubMessagePublisher(
        IConnectionMultiplexer redis,
        ILogger<RedisPubSubMessagePublisher> logger,
        IOptions<SimpleMediatorRedisPubSubOptions> options)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _redis = redis;
        _subscriber = redis.GetSubscriber();
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<MediatorError, long>> PublishAsync<TMessage>(
        TMessage message,
        string? channel = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var effectiveChannel = channel ?? $"{_options.ChannelPrefix}:{_options.EventChannel}";

        try
        {
            _logger.LogDebug(
                "Publishing message of type {MessageType} to channel {Channel}",
                typeof(TMessage).Name,
                effectiveChannel);

            var payload = JsonSerializer.Serialize(new RedisMessageWrapper<TMessage>
            {
                MessageType = typeof(TMessage).FullName ?? typeof(TMessage).Name,
                Payload = message,
                TimestampUtc = DateTime.UtcNow
            });

            var subscriberCount = await _subscriber.PublishAsync(
                RedisChannel.Literal(effectiveChannel),
                payload).ConfigureAwait(false);

            _logger.LogDebug(
                "Successfully published message to {SubscriberCount} subscribers",
                subscriberCount);

            return Right<MediatorError, long>(subscriberCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish message of type {MessageType} to channel {Channel}",
                typeof(TMessage).Name,
                effectiveChannel);

            return Left<MediatorError, long>(
                MediatorErrors.FromException(
                    "REDIS_PUBLISH_FAILED",
                    ex,
                    $"Failed to publish message of type {typeof(TMessage).Name} to channel {effectiveChannel}."));
        }
    }

    /// <inheritdoc />
    public async ValueTask<IAsyncDisposable> SubscribeAsync<TMessage>(
        Func<TMessage, ValueTask> handler,
        string? channel = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var effectiveChannel = channel ?? $"{_options.ChannelPrefix}:{_options.EventChannel}";

        _logger.LogDebug(
            "Subscribing to channel {Channel} for messages of type {MessageType}",
            effectiveChannel,
            typeof(TMessage).Name);

        var channelQueue = await _subscriber.SubscribeAsync(
            RedisChannel.Literal(effectiveChannel)).ConfigureAwait(false);

        channelQueue.OnMessage(async message =>
        {
            try
            {
                var wrapper = JsonSerializer.Deserialize<RedisMessageWrapper<TMessage>>((string)message.Message!);
                if (wrapper?.Payload is not null)
                {
                    await handler(wrapper.Payload).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing message from channel {Channel}",
                    effectiveChannel);
            }
        });

        return new RedisSubscription(channelQueue);
    }

    /// <inheritdoc />
    public async ValueTask<IAsyncDisposable> SubscribePatternAsync<TMessage>(
        string pattern,
        Func<string, TMessage, ValueTask> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(handler);

        var fullPattern = $"{_options.ChannelPrefix}:{pattern}";

        _logger.LogDebug(
            "Subscribing to pattern {Pattern} for messages of type {MessageType}",
            fullPattern,
            typeof(TMessage).Name);

        var channelQueue = await _subscriber.SubscribeAsync(
            RedisChannel.Pattern(fullPattern)).ConfigureAwait(false);

        channelQueue.OnMessage(async message =>
        {
            try
            {
                var wrapper = JsonSerializer.Deserialize<RedisMessageWrapper<TMessage>>((string)message.Message!);
                if (wrapper?.Payload is not null)
                {
                    await handler(message.Channel!, wrapper.Payload).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing message from channel {Channel}",
                    message.Channel);
            }
        });

        return new RedisSubscription(channelQueue);
    }
}

internal sealed class RedisMessageWrapper<T>
{
    public string MessageType { get; set; } = string.Empty;
    public T? Payload { get; set; }
    public DateTime TimestampUtc { get; set; }
}

internal sealed class RedisSubscription : IAsyncDisposable
{
    private readonly ChannelMessageQueue _channelQueue;

    public RedisSubscription(ChannelMessageQueue channelQueue)
    {
        _channelQueue = channelQueue;
    }

    public async ValueTask DisposeAsync()
    {
        await _channelQueue.UnsubscribeAsync().ConfigureAwait(false);
    }
}
