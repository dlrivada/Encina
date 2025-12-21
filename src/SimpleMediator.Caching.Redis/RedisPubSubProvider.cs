using System.Text.Json;

namespace SimpleMediator.Caching.Redis;

/// <summary>
/// Redis implementation of <see cref="IPubSubProvider"/> using StackExchange.Redis.
/// </summary>
/// <remarks>
/// <para>
/// This provider enables cross-instance communication for:
/// </para>
/// <list type="bullet">
/// <item><description>Cache invalidation across multiple application instances</description></item>
/// <item><description>Real-time notifications</description></item>
/// <item><description>Event broadcasting</description></item>
/// </list>
/// <para>
/// This provider is wire-compatible with Redis, Garnet, Valkey, Dragonfly, and KeyDB.
/// </para>
/// </remarks>
public sealed partial class RedisPubSubProvider : IPubSubProvider
{
    private readonly IConnectionMultiplexer _connection;
    private readonly ILogger<RedisPubSubProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisPubSubProvider"/> class.
    /// </summary>
    /// <param name="connection">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger.</param>
    public RedisPubSubProvider(
        IConnectionMultiplexer connection,
        ILogger<RedisPubSubProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(logger);

        _connection = connection;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private ISubscriber Subscriber => _connection.GetSubscriber();

    /// <inheritdoc/>
    public async Task PublishAsync(string channel, string message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        await Subscriber.PublishAsync(RedisChannel.Literal(channel), message).ConfigureAwait(false);
        LogPublish(_logger, channel, message);
    }

    /// <inheritdoc/>
    public async Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        cancellationToken.ThrowIfCancellationRequested();

        var json = JsonSerializer.Serialize(message, _jsonOptions);
        await PublishAsync(channel, json, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IAsyncDisposable> SubscribeAsync(
        string channel,
        Func<string, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(handler);
        cancellationToken.ThrowIfCancellationRequested();

        var redisChannel = RedisChannel.Literal(channel);

        await Subscriber.SubscribeAsync(redisChannel, async (ch, message) =>
        {
            if (!message.IsNullOrEmpty)
            {
                await handler(message!).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

        LogSubscribe(_logger, channel);

        return new ChannelSubscription(Subscriber, redisChannel);
    }

    /// <inheritdoc/>
    public async Task<IAsyncDisposable> SubscribeAsync<T>(
        string channel,
        Func<T, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(handler);
        cancellationToken.ThrowIfCancellationRequested();

        Func<string, Task> stringHandler = async message =>
        {
            var typedMessage = JsonSerializer.Deserialize<T>(message, _jsonOptions);
            if (typedMessage is not null)
            {
                await handler(typedMessage).ConfigureAwait(false);
            }
        };

        return await SubscribeAsync(channel, stringHandler, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IAsyncDisposable> SubscribePatternAsync(
        string pattern,
        Func<string, string, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(handler);
        cancellationToken.ThrowIfCancellationRequested();

        var redisPattern = RedisChannel.Pattern(pattern);

        await Subscriber.SubscribeAsync(redisPattern, async (ch, message) =>
        {
            if (!message.IsNullOrEmpty)
            {
                await handler(ch!, message!).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

        LogSubscribePattern(_logger, pattern);

        return new PatternSubscription(Subscriber, redisPattern);
    }

    /// <inheritdoc/>
    public async Task UnsubscribeAsync(string channel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        cancellationToken.ThrowIfCancellationRequested();

        await Subscriber.UnsubscribeAsync(RedisChannel.Literal(channel)).ConfigureAwait(false);
        LogUnsubscribe(_logger, channel);
    }

    /// <inheritdoc/>
    public async Task UnsubscribePatternAsync(string pattern, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        cancellationToken.ThrowIfCancellationRequested();

        await Subscriber.UnsubscribeAsync(RedisChannel.Pattern(pattern)).ConfigureAwait(false);
        LogUnsubscribePattern(_logger, pattern);
    }

    /// <inheritdoc/>
    public async Task<long> GetSubscriberCountAsync(string channel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await Subscriber.PublishAsync(
            RedisChannel.Literal(channel),
            RedisValue.Null,
            CommandFlags.DemandMaster).ConfigureAwait(false);

        return result;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Publishing to channel {Channel}: {Message}")]
    private static partial void LogPublish(ILogger logger, string channel, string message);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Subscribed to channel: {Channel}")]
    private static partial void LogSubscribe(ILogger logger, string channel);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Subscribed to pattern: {Pattern}")]
    private static partial void LogSubscribePattern(ILogger logger, string pattern);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Unsubscribed from channel: {Channel}")]
    private static partial void LogUnsubscribe(ILogger logger, string channel);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Unsubscribed from pattern: {Pattern}")]
    private static partial void LogUnsubscribePattern(ILogger logger, string pattern);

    private sealed class ChannelSubscription : IAsyncDisposable
    {
        private readonly ISubscriber _subscriber;
        private readonly RedisChannel _channel;

        public ChannelSubscription(ISubscriber subscriber, RedisChannel channel)
        {
            _subscriber = subscriber;
            _channel = channel;
        }

        public async ValueTask DisposeAsync()
        {
            await _subscriber.UnsubscribeAsync(_channel).ConfigureAwait(false);
        }
    }

    private sealed class PatternSubscription : IAsyncDisposable
    {
        private readonly ISubscriber _subscriber;
        private readonly RedisChannel _pattern;

        public PatternSubscription(ISubscriber subscriber, RedisChannel pattern)
        {
            _subscriber = subscriber;
            _pattern = pattern;
        }

        public async ValueTask DisposeAsync()
        {
            await _subscriber.UnsubscribeAsync(_pattern).ConfigureAwait(false);
        }
    }
}
