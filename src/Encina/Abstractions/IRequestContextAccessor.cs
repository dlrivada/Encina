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
/// that dispatch, so a handler that sends a nested request, an EF Core interceptor or a tenant
/// provider all observe the same context. The dispatcher restores the previous value when the
/// dispatch finishes, so the context never leaks into unrelated calls.
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
