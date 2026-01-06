using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Encina.Caching;

namespace Encina.Testing.Fakes.Providers;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IPubSubProvider"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// Provides full implementation of the pub/sub provider interface using in-memory
/// collections. All operations are synchronous but return completed tasks
/// for interface compatibility.
/// </para>
/// <para>
/// This provider tracks all operations for verification in tests:
/// <list type="bullet">
/// <item><description><see cref="PublishedMessages"/>: All messages published to each channel</description></item>
/// <item><description><see cref="Subscriptions"/>: Active channel subscriptions</description></item>
/// <item><description><see cref="PatternSubscriptions"/>: Active pattern subscriptions</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class FakePubSubProvider : IPubSubProvider
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<PublishedMessage>> _publishedMessages = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<Func<string, Task>>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<Func<string, string, Task>>> _patternSubscriptions = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets all published messages grouped by channel.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<PublishedMessage>> PublishedMessages
    {
        get
        {
            lock (_lock)
            {
                return _publishedMessages.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyList<PublishedMessage>)kvp.Value.ToList().AsReadOnly());
            }
        }
    }

    /// <summary>
    /// Gets the list of channels with active subscriptions.
    /// </summary>
    public IReadOnlyList<string> Subscriptions
    {
        get
        {
            lock (_lock)
            {
                return _subscriptions.Keys.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets the list of active pattern subscriptions.
    /// </summary>
    public IReadOnlyList<string> PatternSubscriptions
    {
        get
        {
            lock (_lock)
            {
                return _patternSubscriptions.Keys.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to simulate errors.
    /// When true, all operations will throw <see cref="InvalidOperationException"/>.
    /// </summary>
    public bool SimulateErrors { get; set; }

    /// <summary>
    /// Gets or sets whether to deliver messages synchronously.
    /// When true (default), published messages are delivered to subscribers immediately.
    /// When false, messages are only recorded for verification.
    /// </summary>
    public bool DeliverMessages { get; set; } = true;

    /// <inheritdoc />
    public async Task PublishAsync(string channel, string message, CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);

        var publishedMessage = new PublishedMessage(channel, message, DateTime.UtcNow);

        var bag = _publishedMessages.GetOrAdd(channel, _ => new ConcurrentBag<PublishedMessage>());
        bag.Add(publishedMessage);

        if (DeliverMessages)
        {
            await DeliverToSubscribersAsync(channel, message);
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);

        var json = JsonSerializer.Serialize(message);
        await PublishAsync(channel, json, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IAsyncDisposable> SubscribeAsync(
        string channel,
        Func<string, Task> handler,
        CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentNullException.ThrowIfNull(handler);

        var handlers = _subscriptions.GetOrAdd(channel, _ => new ConcurrentBag<Func<string, Task>>());
        handlers.Add(handler);

        return Task.FromResult<IAsyncDisposable>(new SubscriptionHandle(() =>
        {
            // Note: ConcurrentBag doesn't support removal, so we use a marker approach
            // In a real implementation, you'd use a different data structure
            // For testing purposes, subscriptions persist until Clear() is called
        }));
    }

    /// <inheritdoc />
    public Task<IAsyncDisposable> SubscribeAsync<T>(
        string channel,
        Func<T, Task> handler,
        CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentNullException.ThrowIfNull(handler);

        return SubscribeAsync(
            channel,
            async message =>
            {
                var typed = JsonSerializer.Deserialize<T>(message);
                if (typed is not null)
                {
                    await handler(typed);
                }
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IAsyncDisposable> SubscribePatternAsync(
        string pattern,
        Func<string, string, Task> handler,
        CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(handler);

        var handlers = _patternSubscriptions.GetOrAdd(pattern, _ => new ConcurrentBag<Func<string, string, Task>>());
        handlers.Add(handler);

        return Task.FromResult<IAsyncDisposable>(new SubscriptionHandle(() =>
        {
            // Similar to above, subscriptions persist until Clear()
        }));
    }

    /// <inheritdoc />
    public Task UnsubscribeAsync(string channel, CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();

        _subscriptions.TryRemove(channel, out _);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnsubscribePatternAsync(string pattern, CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();

        _patternSubscriptions.TryRemove(pattern, out _);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> GetSubscriberCountAsync(string channel, CancellationToken cancellationToken)
    {
        ThrowIfSimulatingErrors();

        if (_subscriptions.TryGetValue(channel, out var handlers))
        {
            return Task.FromResult((long)handlers.Count);
        }

        return Task.FromResult(0L);
    }

    /// <summary>
    /// Checks if a message was published to a channel.
    /// </summary>
    /// <param name="channel">The channel to check.</param>
    /// <returns>True if at least one message was published to the channel.</returns>
    public bool WasMessagePublished(string channel)
    {
        return _publishedMessages.TryGetValue(channel, out var messages) && !messages.IsEmpty;
    }

    /// <summary>
    /// Checks if a specific message was published to a channel.
    /// </summary>
    /// <param name="channel">The channel to check.</param>
    /// <param name="message">The message content to look for.</param>
    /// <returns>True if the message was published to the channel.</returns>
    public bool WasMessagePublished(string channel, string message)
    {
        if (_publishedMessages.TryGetValue(channel, out var messages))
        {
            return messages.Any(m => m.Content == message);
        }

        return false;
    }

    /// <summary>
    /// Gets all messages published to a channel.
    /// </summary>
    /// <param name="channel">The channel to get messages for.</param>
    /// <returns>All messages published to the channel.</returns>
    public IReadOnlyList<PublishedMessage> GetPublishedMessages(string channel)
    {
        if (_publishedMessages.TryGetValue(channel, out var messages))
        {
            return messages.ToList().AsReadOnly();
        }

        return Array.Empty<PublishedMessage>();
    }

    /// <summary>
    /// Gets all messages published to channels matching a pattern.
    /// </summary>
    /// <param name="pattern">The glob pattern to match channels.</param>
    /// <returns>All messages published to matching channels.</returns>
    public IReadOnlyList<PublishedMessage> GetPublishedMessagesByPattern(string pattern)
    {
        var regex = GlobToRegex(pattern);
        var result = new List<PublishedMessage>();

        foreach (var kvp in _publishedMessages)
        {
            if (regex.IsMatch(kvp.Key))
            {
                result.AddRange(kvp.Value);
            }
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Gets the total number of messages published across all channels.
    /// </summary>
    public int TotalPublishedCount => _publishedMessages.Values.Sum(bag => bag.Count);

    /// <summary>
    /// Checks if a channel has active subscriptions.
    /// </summary>
    /// <param name="channel">The channel to check.</param>
    /// <returns>True if the channel has subscribers.</returns>
    public bool HasSubscribers(string channel)
    {
        return _subscriptions.ContainsKey(channel);
    }

    /// <summary>
    /// Checks if a pattern has active subscriptions.
    /// </summary>
    /// <param name="pattern">The pattern to check.</param>
    /// <returns>True if the pattern has subscribers.</returns>
    public bool HasPatternSubscribers(string pattern)
    {
        return _patternSubscriptions.ContainsKey(pattern);
    }

    /// <summary>
    /// Clears all published messages and subscriptions.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _publishedMessages.Clear();
            _subscriptions.Clear();
            _patternSubscriptions.Clear();
        }
    }

    /// <summary>
    /// Clears only the published messages while keeping subscriptions intact.
    /// </summary>
    public void ClearMessages()
    {
        lock (_lock)
        {
            _publishedMessages.Clear();
        }
    }

    /// <summary>
    /// Clears only the subscriptions while keeping published messages intact.
    /// </summary>
    public void ClearSubscriptions()
    {
        lock (_lock)
        {
            _subscriptions.Clear();
            _patternSubscriptions.Clear();
        }
    }

    private async Task DeliverToSubscribersAsync(string channel, string message)
    {
        // Deliver to exact channel subscribers
        if (_subscriptions.TryGetValue(channel, out var handlers))
        {
            foreach (var handler in handlers)
            {
                await handler(message);
            }
        }

        // Deliver to pattern subscribers
        foreach (var kvp in _patternSubscriptions)
        {
            var regex = GlobToRegex(kvp.Key);
            if (regex.IsMatch(channel))
            {
                foreach (var handler in kvp.Value)
                {
                    await handler(channel, message);
                }
            }
        }
    }

    private void ThrowIfSimulatingErrors()
    {
        if (SimulateErrors)
        {
            throw new InvalidOperationException("FakePubSubProvider is configured to simulate errors.");
        }
    }

    private static Regex GlobToRegex(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".")
            .Replace("\\[", "[")
            .Replace("\\]", "]") + "$";

        return new Regex(regexPattern, RegexOptions.Compiled);
    }

    private sealed class SubscriptionHandle : IAsyncDisposable
    {
        private readonly Action _onDispose;

        public SubscriptionHandle(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public ValueTask DisposeAsync()
        {
            _onDispose();
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>
/// Represents a message that was published to a channel.
/// </summary>
/// <param name="Channel">The channel the message was published to.</param>
/// <param name="Content">The message content.</param>
/// <param name="PublishedAtUtc">When the message was published.</param>
public sealed record PublishedMessage(string Channel, string Content, DateTime PublishedAtUtc)
{
    /// <summary>
    /// Deserializes the content as a typed message.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <returns>The deserialized message.</returns>
    public T? GetContent<T>() => JsonSerializer.Deserialize<T>(Content);
}
