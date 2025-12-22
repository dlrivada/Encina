using LanguageExt;

namespace Encina.Redis.PubSub;

/// <summary>
/// Interface for publishing messages through Redis Pub/Sub.
/// </summary>
public interface IRedisPubSubMessagePublisher
{
    /// <summary>
    /// Publishes a message to a channel.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="channel">The channel name. If null, uses the default channel.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a MediatorError or the number of subscribers that received the message.</returns>
    ValueTask<Either<MediatorError, long>> PublishAsync<TMessage>(
        TMessage message,
        string? channel = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Subscribes to a channel and invokes a handler for each message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="handler">The message handler.</param>
    /// <param name="channel">The channel name. If null, uses the default channel.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    ValueTask<IAsyncDisposable> SubscribeAsync<TMessage>(
        Func<TMessage, ValueTask> handler,
        string? channel = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Subscribes to channels matching a pattern.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="pattern">The channel pattern (e.g., "events.*").</param>
    /// <param name="handler">The message handler with channel name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    ValueTask<IAsyncDisposable> SubscribePatternAsync<TMessage>(
        string pattern,
        Func<string, TMessage, ValueTask> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
