using LanguageExt;

namespace Encina.Messaging.ContentRouter;

/// <summary>
/// Routes messages to handlers based on content inspection.
/// </summary>
/// <remarks>
/// <para>
/// The Content-Based Router pattern inspects the content of a message and routes
/// it to the appropriate handler(s) based on configurable routing rules.
/// </para>
/// <para>
/// This is an Enterprise Integration Pattern (EIP) that enables dynamic routing
/// based on message content rather than message type alone.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define a router
/// var router = ContentRouterBuilder.Create&lt;Order&gt;()
///     .When(o => o.Total > 10000)
///         .RouteTo&lt;HighValueOrderHandler&gt;()
///     .When(o => o.IsInternational)
///         .RouteTo&lt;InternationalOrderHandler&gt;()
///     .Default&lt;StandardOrderHandler&gt;()
///     .Build();
///
/// // Route an order
/// var result = await contentRouter.RouteAsync(router, order, cancellationToken);
/// </code>
/// </example>
public interface IContentRouter
{
    /// <summary>
    /// Routes a message to the appropriate handler(s) based on content inspection.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to route.</typeparam>
    /// <typeparam name="TResult">The type of result returned by handlers.</typeparam>
    /// <param name="definition">The router definition containing routing rules.</param>
    /// <param name="message">The message to route.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Either an error or the routing result containing handler execution results.
    /// </returns>
    ValueTask<Either<EncinaError, ContentRouterResult<TResult>>> RouteAsync<TMessage, TResult>(
        BuiltContentRouterDefinition<TMessage, TResult> definition,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Routes a message to handlers that don't return a result.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to route.</typeparam>
    /// <param name="definition">The router definition containing routing rules.</param>
    /// <param name="message">The message to route.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or the routing result.</returns>
    ValueTask<Either<EncinaError, ContentRouterResult<Unit>>> RouteAsync<TMessage>(
        BuiltContentRouterDefinition<TMessage, Unit> definition,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
