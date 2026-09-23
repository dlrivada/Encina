namespace Encina;

/// <summary>
/// Provides access to the ambient <see cref="IRequestContext"/> of the current logical call.
/// </summary>
/// <remarks>
/// <para>
/// This is the single propagation point for identity, tenant, idempotency key and correlation
/// data. Entry points fill it (for example <c>EncinaContextMiddleware</c> in <c>Encina.AspNetCore</c>
/// for every HTTP request), and <see cref="IEncina.Send{TResponse}(IRequest{TResponse}, CancellationToken)"/>,
/// <see cref="IEncina.Publish{TNotification}(TNotification, CancellationToken)"/> and
/// <see cref="IEncina.Stream{TItem}(IStreamRequest{TItem}, CancellationToken)"/> seed the pipeline's
/// <see cref="IRequestContext"/> from it.
/// </para>
/// <para>
/// While a request, notification or stream is being dispatched, the accessor holds the context of
/// that dispatch, so its behaviors, its handler, an EF Core interceptor or a tenant provider all
/// observe the same context. The dispatcher restores the previous value when the dispatch finishes
/// (also when it throws, is cancelled, or a stream is abandoned early), so the context never leaks
/// into unrelated calls.
/// </para>
/// <para>
/// A dispatch that starts while another one is running (a handler sending a nested request,
/// publishing a notification or domain events, or enumerating a stream) does not reuse the outer
/// context unchanged: it gets a derived context with the same correlation id, user id, tenant id
/// and metadata, its own <see cref="IRequestContext.Timestamp"/>, and no
/// <see cref="IRequestContext.IdempotencyKey"/>. The key identifies the entry point's logical
/// request; passing it on would make idempotency behaviors treat the nested request as a duplicate
/// of the outer one. A context passed explicitly to an overload is always used as-is.
/// </para>
/// <para>
/// The default implementation (<see cref="RequestContextAccessor"/>) stores the value in an
/// <see cref="AsyncLocal{T}"/>, so it flows across <c>await</c> points and stays isolated between
/// concurrent logical calls. Entry points without an ambient context (background jobs, webhooks,
/// outbox or scheduled-message dispatch) pass one explicitly through the
/// <see cref="IEncina.Send{TResponse}(IRequest{TResponse}, IRequestContext, CancellationToken)"/> overloads.
/// </para>
/// </remarks>
public interface IRequestContextAccessor
{
    /// <summary>
    /// Gets or sets the ambient request context, or <c>null</c> when no context is in flight.
    /// </summary>
    IRequestContext? RequestContext { get; set; }
}
