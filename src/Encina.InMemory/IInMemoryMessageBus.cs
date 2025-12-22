using LanguageExt;

namespace Encina.InMemory;

/// <summary>
/// Interface for the in-memory message bus.
/// </summary>
public interface IInMemoryMessageBus
{
    /// <summary>
    /// Publishes a message to all subscribers.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Enqueues a message for background processing.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Either a EncinaError or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> EnqueueAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="handler">The message handler.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    IDisposable Subscribe<TMessage>(Func<TMessage, ValueTask> handler)
        where TMessage : class;

    /// <summary>
    /// Gets the number of pending messages in the queue.
    /// </summary>
    int PendingCount { get; }

    /// <summary>
    /// Gets the number of active subscribers.
    /// </summary>
    int SubscriberCount { get; }
}
