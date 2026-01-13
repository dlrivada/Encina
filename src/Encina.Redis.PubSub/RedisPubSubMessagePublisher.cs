using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using static LanguageExt.Prelude;

namespace Encina.Redis.PubSub;

/// <summary>
/// Redis Pub/Sub-based implementation of the message publisher.
/// </summary>
public sealed class RedisPubSubMessagePublisher : IRedisPubSubMessagePublisher
{
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisPubSubMessagePublisher> _logger;
    private readonly EncinaRedisPubSubOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisPubSubMessagePublisher"/> class.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public RedisPubSubMessagePublisher(
        IConnectionMultiplexer redis,
        ILogger<RedisPubSubMessagePublisher> logger,
        IOptions<EncinaRedisPubSubOptions> options)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _subscriber = redis.GetSubscriber();
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, long>> PublishAsync<TMessage>(
        TMessage message,
        string? channel = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var effectiveChannel = channel ?? $"{_options.ChannelPrefix}:{_options.EventChannel}";

        try
        {
            Log.PublishingMessage(_logger, typeof(TMessage).Name, effectiveChannel);

            var payload = JsonSerializer.Serialize(new RedisMessageWrapper<TMessage>
            {
                MessageType = typeof(TMessage).FullName ?? typeof(TMessage).Name,
                Payload = message,
                TimestampUtc = DateTime.UtcNow
            });

            var subscriberCount = await _subscriber.PublishAsync(
                RedisChannel.Literal(effectiveChannel),
                payload).ConfigureAwait(false);

            Log.SuccessfullyPublishedMessage(_logger, subscriberCount);

            return Right<EncinaError, long>(subscriberCount); // NOSONAR S6966: LanguageExt Right is a pure function
        }
        catch (Exception ex)
        {
            Log.FailedToPublishMessage(_logger, ex, typeof(TMessage).Name, effectiveChannel);

            return Left<EncinaError, long>( // NOSONAR S6966: LanguageExt Left is a pure function
                EncinaErrors.FromException(
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

        Log.SubscribingToChannel(_logger, effectiveChannel, typeof(TMessage).Name);

        var channelQueue = await _subscriber.SubscribeAsync(
            RedisChannel.Literal(effectiveChannel)).ConfigureAwait(false);

        channelQueue.OnMessage(async message =>
        {
            try
            {
                var wrapper = JsonSerializer.Deserialize<RedisMessageWrapper<TMessage>>(message.Message.ToString());
                if (wrapper?.Payload is not null)
                {
                    await handler(wrapper.Payload).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorProcessingMessage(_logger, ex, effectiveChannel);
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

        Log.SubscribingToPattern(_logger, fullPattern, typeof(TMessage).Name);

        var channelQueue = await _subscriber.SubscribeAsync(
            RedisChannel.Pattern(fullPattern)).ConfigureAwait(false);

        channelQueue.OnMessage(async message =>
        {
            try
            {
                var wrapper = JsonSerializer.Deserialize<RedisMessageWrapper<TMessage>>(message.Message.ToString());
                if (wrapper?.Payload is not null)
                {
                    await handler(message.Channel!, wrapper.Payload).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorProcessingMessage(_logger, ex, message.Channel!);
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
