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
/// context unchanged: it gets a derived context with the same correlation id, identity, tenant id
/// and metadata, its own <see cref="IRequestContext.Timestamp"/>, and no
/// <see cref="IRequestContext.IdempotencyKey"/>. The key identifies the entry point's logical
/// request; passing it on would make idempotency behaviors treat the nested request as a duplicate
/// of the outer one. A context passed explicitly to an overload is snapshotted (a foreign
/// implementation is copied into an immutable <see cref="global::Encina.RequestContext"/>) and the snapshot
/// is checked by the explicit-context rule (see
/// <see cref="IEncina.Send{TResponse}(IRequest{TResponse}, IRequestContext, CancellationToken)"/>).
/// </para>
/// <para>
/// <b>Binding an identity.</b> Identities are bound to a unit of work only through
/// <see cref="IRequestContextScopeFactory"/>; the context read here is the one of the innermost
/// scope (or entry point) of the current flow, and reads as <c>null</c> once that scope has ended.
/// </para>
/// <para>
/// <b>The setter</b> is host infrastructure (request middleware, the dispatcher). Application code
/// reads the context; it never sets an identity. The default implementation accepts a set only
/// when it preserves the identity and the origin of the readable context (anonymous with no origin
/// when none is readable), never clears it, and accepts a tenant change only while no dispatch is
/// in flight; any other set logs Warning 165 and throws <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// The default implementation (<see cref="RequestContextAccessor"/>) stores the value in an
/// <see cref="AsyncLocal{T}"/>, so it flows across <c>await</c> points and stays isolated between
/// concurrent logical calls. It is the only supported implementation: identity scopes and the
/// dispatcher share its store, and <c>AddEncinaRequestIdentity</c> fails startup when another
/// implementation is registered.
/// </para>
/// </remarks>
public interface IRequestContextAccessor
{
    /// <summary>
    /// Gets or sets the ambient request context, or <c>null</c> when no context is in flight.
    /// </summary>
    /// <remarks>
    /// The default implementation's setter is identity- and origin-preserving: the new value must
    /// carry the same identity (kind, user id, roles, permissions and non-per-token claims) and the
    /// same origin as the readable context, it may change the tenant only while no dispatch is in
    /// flight, and it can never be <see langword="null"/>. Use
    /// <see cref="IRequestContextScopeFactory"/> to run code as another identity.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// (Default implementation, on set.) The value is <see langword="null"/>, or changes the
    /// identity or the origin, or changes the tenant during a dispatch.
    /// </exception>
    IRequestContext? RequestContext { get; set; }
}
