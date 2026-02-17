using LanguageExt;

namespace Encina.Cdc.Messaging;

/// <summary>
/// Interceptor invoked by the CDC dispatcher after a change event has been
/// successfully dispatched to its handler. Enables cross-cutting concerns
/// like publishing notifications or auditing.
/// </summary>
/// <remarks>
/// <para>
/// Interceptors run after the handler returns a successful result (Right branch).
/// They receive the original <see cref="ChangeEvent"/> and can perform additional
/// actions like publishing to the messaging system.
/// </para>
/// <para>
/// If the interceptor returns a Left (error), the error is logged but does not
/// prevent the position from being saved (handler execution was already successful).
/// </para>
/// </remarks>
public interface ICdcEventInterceptor
{
    /// <summary>
    /// Called after a change event has been successfully dispatched to its handler.
    /// </summary>
    /// <param name="changeEvent">The change event that was processed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> OnEventDispatchedAsync(
        ChangeEvent changeEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after a sharded change event has been successfully dispatched to its handler.
    /// Provides shard context for topic routing and notification enrichment.
    /// </summary>
    /// <param name="changeEvent">The change event that was processed.</param>
    /// <param name="shardId">The shard identifier where the event originated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or unit on success.</returns>
    /// <remarks>
    /// The default implementation delegates to <see cref="OnEventDispatchedAsync(ChangeEvent, CancellationToken)"/>,
    /// ignoring the shard context. Implementations that are shard-aware should override this method.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> OnShardedEventDispatchedAsync(
        ChangeEvent changeEvent,
        string shardId,
        CancellationToken cancellationToken = default)
        => OnEventDispatchedAsync(changeEvent, cancellationToken);
}
