namespace SimpleMediator.Caching;

/// <summary>
/// Provides publish/subscribe capabilities for cross-instance communication.
/// </summary>
/// <remarks>
/// <para>
/// Pub/Sub is essential for:
/// </para>
/// <list type="bullet">
/// <item><description>Cache invalidation - Broadcasting invalidation messages across instances</description></item>
/// <item><description>Real-time updates - Pushing notifications to connected clients</description></item>
/// <item><description>Event distribution - Spreading domain events across service instances</description></item>
/// <item><description>Saga coordination - Notifying instances of saga state changes</description></item>
/// </list>
/// <para>
/// This interface abstracts over different pub/sub implementations:
/// </para>
/// <list type="bullet">
/// <item><description>Redis Pub/Sub - Native Redis messaging</description></item>
/// <item><description>Garnet - Compatible with Redis Pub/Sub</description></item>
/// <item><description>Memory - In-process messaging (single instance only)</description></item>
/// <item><description>NCache - Native .NET messaging</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Subscribe to cache invalidation messages
/// public class CacheInvalidationSubscriber : IHostedService
/// {
///     private readonly IPubSubProvider _pubSub;
///     private readonly ICacheProvider _cache;
///     private IAsyncDisposable? _subscription;
///
///     public async Task StartAsync(CancellationToken cancellationToken)
///     {
///         _subscription = await _pubSub.SubscribeAsync(
///             "cache:invalidate",
///             async message =>
///             {
///                 var pattern = message;
///                 await _cache.RemoveByPatternAsync(pattern);
///             },
///             cancellationToken);
///     }
///
///     public async Task StopAsync(CancellationToken cancellationToken)
///     {
///         if (_subscription is not null)
///         {
///             await _subscription.DisposeAsync();
///         }
///     }
/// }
///
/// // Publish cache invalidation
/// public class ProductUpdatedHandler
/// {
///     private readonly IPubSubProvider _pubSub;
///
///     public async Task HandleAsync(ProductUpdatedEvent @event, CancellationToken ct)
///     {
///         // Broadcast invalidation to all instances
///         await _pubSub.PublishAsync("cache:invalidate", $"product:{@event.ProductId}:*", ct);
///     }
/// }
/// </code>
/// </example>
public interface IPubSubProvider
{
    /// <summary>
    /// Publishes a message to a channel.
    /// </summary>
    /// <param name="channel">The channel to publish to.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(string channel, string message, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes a typed message to a channel.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <param name="channel">The channel to publish to.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// The message will be serialized to JSON before publishing.
    /// </remarks>
    Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to messages on a channel.
    /// </summary>
    /// <param name="channel">The channel to subscribe to.</param>
    /// <param name="handler">The handler to invoke when a message is received.</param>
    /// <param name="cancellationToken">Token to cancel the subscription.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that unsubscribes when disposed.</returns>
    Task<IAsyncDisposable> SubscribeAsync(
        string channel,
        Func<string, Task> handler,
        CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to typed messages on a channel.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <param name="channel">The channel to subscribe to.</param>
    /// <param name="handler">The handler to invoke when a message is received.</param>
    /// <param name="cancellationToken">Token to cancel the subscription.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that unsubscribes when disposed.</returns>
    /// <remarks>
    /// Messages will be deserialized from JSON to the specified type.
    /// </remarks>
    Task<IAsyncDisposable> SubscribeAsync<T>(
        string channel,
        Func<T, Task> handler,
        CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to messages matching a pattern.
    /// </summary>
    /// <param name="pattern">The pattern to match (e.g., "cache:*", "user:*:events").</param>
    /// <param name="handler">The handler to invoke when a message is received.</param>
    /// <param name="cancellationToken">Token to cancel the subscription.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that unsubscribes when disposed.</returns>
    /// <remarks>
    /// Pattern syntax depends on the provider. Most providers support glob-style patterns:
    /// <list type="bullet">
    /// <item><description><c>*</c> matches any sequence of characters</description></item>
    /// <item><description><c>?</c> matches any single character</description></item>
    /// </list>
    /// </remarks>
    Task<IAsyncDisposable> SubscribePatternAsync(
        string pattern,
        Func<string, string, Task> handler,
        CancellationToken cancellationToken);

    /// <summary>
    /// Unsubscribes from a channel.
    /// </summary>
    /// <param name="channel">The channel to unsubscribe from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeAsync(string channel, CancellationToken cancellationToken);

    /// <summary>
    /// Unsubscribes from a pattern.
    /// </summary>
    /// <param name="pattern">The pattern to unsubscribe from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribePatternAsync(string pattern, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the number of subscribers for a channel.
    /// </summary>
    /// <param name="channel">The channel to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of subscribers.</returns>
    Task<long> GetSubscriberCountAsync(string channel, CancellationToken cancellationToken);
}
