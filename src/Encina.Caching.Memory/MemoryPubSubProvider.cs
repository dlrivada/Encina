using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Encina.Caching.Memory;

/// <summary>
/// In-memory implementation of <see cref="IPubSubProvider"/> for single-instance scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This provider is useful for:
/// </para>
/// <list type="bullet">
/// <item><description>Development and testing without external dependencies</description></item>
/// <item><description>Single-instance applications that need pub/sub locally</description></item>
/// <item><description>Unit testing cache invalidation flows</description></item>
/// </list>
/// <para>
/// For distributed pub/sub across multiple instances, use Redis or NCache providers.
/// </para>
/// </remarks>
public sealed partial class MemoryPubSubProvider : IPubSubProvider
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<Func<string, Task>>> _channelSubscribers = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<Func<string, string, Task>>> _patternSubscribers = new();
    private readonly ILogger<MemoryPubSubProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryPubSubProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public MemoryPubSubProvider(ILogger<MemoryPubSubProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task PublishAsync(string channel, string message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        LogPublish(_logger, channel, message);

        // Notify channel subscribers
        if (_channelSubscribers.TryGetValue(channel, out var subscribers))
        {
            foreach (var handler in subscribers)
            {
                await handler(message).ConfigureAwait(false);
            }
        }

        // Notify pattern subscribers
        foreach (var (pattern, handlers) in _patternSubscribers)
        {
            if (MatchesPattern(pattern, channel))
            {
                foreach (var handler in handlers)
                {
                    await handler(channel, message).ConfigureAwait(false);
                }
            }
        }
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
    public Task<IAsyncDisposable> SubscribeAsync(
        string channel,
        Func<string, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(handler);
        cancellationToken.ThrowIfCancellationRequested();

        var subscribers = _channelSubscribers.GetOrAdd(channel, _ => new ConcurrentBag<Func<string, Task>>());
        subscribers.Add(handler);

        LogSubscribe(_logger, channel);

        return Task.FromResult<IAsyncDisposable>(new ChannelSubscription(this, channel, handler));
    }

    /// <inheritdoc/>
    public Task<IAsyncDisposable> SubscribeAsync<T>(
        string channel,
        Func<T, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(handler);
        cancellationToken.ThrowIfCancellationRequested();

        // Wrap the typed handler in a string handler that deserializes
        Func<string, Task> stringHandler = async message =>
        {
            var typedMessage = JsonSerializer.Deserialize<T>(message, _jsonOptions);
            if (typedMessage is not null)
            {
                await handler(typedMessage).ConfigureAwait(false);
            }
        };

        return SubscribeAsync(channel, stringHandler, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IAsyncDisposable> SubscribePatternAsync(
        string pattern,
        Func<string, string, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(handler);
        cancellationToken.ThrowIfCancellationRequested();

        var handlers = _patternSubscribers.GetOrAdd(pattern, _ => new ConcurrentBag<Func<string, string, Task>>());
        handlers.Add(handler);

        LogSubscribePattern(_logger, pattern);

        return Task.FromResult<IAsyncDisposable>(new PatternSubscription(this, pattern, handler));
    }

    /// <inheritdoc/>
    public Task UnsubscribeAsync(string channel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        cancellationToken.ThrowIfCancellationRequested();

        _channelSubscribers.TryRemove(channel, out _);
        LogUnsubscribe(_logger, channel);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UnsubscribePatternAsync(string pattern, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        cancellationToken.ThrowIfCancellationRequested();

        _patternSubscribers.TryRemove(pattern, out _);
        LogUnsubscribePattern(_logger, pattern);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<long> GetSubscriberCountAsync(string channel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        cancellationToken.ThrowIfCancellationRequested();

        var count = 0L;

        if (_channelSubscribers.TryGetValue(channel, out var subscribers))
        {
            count += subscribers.Count;
        }

        // Count pattern subscribers that match this channel
        foreach (var (pattern, handlers) in _patternSubscribers)
        {
            if (MatchesPattern(pattern, channel))
            {
                count += handlers.Count;
            }
        }

        return Task.FromResult(count);
    }

    private void RemoveChannelSubscriber(string channel, Func<string, Task> handler)
    {
        if (_channelSubscribers.TryGetValue(channel, out var subscribers))
        {
            // ConcurrentBag doesn't support removal, so we rebuild without the handler
            var newBag = new ConcurrentBag<Func<string, Task>>(
                subscribers.Where(h => !ReferenceEquals(h, handler)));
            _channelSubscribers.TryUpdate(channel, newBag, subscribers);
        }
    }

    private void RemovePatternSubscriber(string pattern, Func<string, string, Task> handler)
    {
        if (_patternSubscribers.TryGetValue(pattern, out var handlers))
        {
            var newBag = new ConcurrentBag<Func<string, string, Task>>(
                handlers.Where(h => !ReferenceEquals(h, handler)));
            _patternSubscribers.TryUpdate(pattern, newBag, handlers);
        }
    }

    private static bool MatchesPattern(string pattern, string channel)
    {
        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(channel, regexPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
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
        private readonly MemoryPubSubProvider _provider;
        private readonly string _channel;
        private readonly Func<string, Task> _handler;

        public ChannelSubscription(MemoryPubSubProvider provider, string channel, Func<string, Task> handler)
        {
            _provider = provider;
            _channel = channel;
            _handler = handler;
        }

        public ValueTask DisposeAsync()
        {
            _provider.RemoveChannelSubscriber(_channel, _handler);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PatternSubscription : IAsyncDisposable
    {
        private readonly MemoryPubSubProvider _provider;
        private readonly string _pattern;
        private readonly Func<string, string, Task> _handler;

        public PatternSubscription(MemoryPubSubProvider provider, string pattern, Func<string, string, Task> handler)
        {
            _provider = provider;
            _pattern = pattern;
            _handler = handler;
        }

        public ValueTask DisposeAsync()
        {
            _provider.RemovePatternSubscriber(_pattern, _handler);
            return ValueTask.CompletedTask;
        }
    }
}
