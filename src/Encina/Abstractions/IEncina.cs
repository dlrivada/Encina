using LanguageExt;

namespace Encina;

/// <summary>
/// Central coordinator used to send commands/queries and publish notifications.
/// </summary>
/// <remarks>
/// The default implementation (<see cref="Encina"/>) creates a DI scope per operation,
/// runs behaviors in cascade, and delegates to the registered handlers.
/// </remarks>
/// <example>
/// <code>
/// var services = new ServiceCollection();
/// services.AddEncina(typeof(CreateReservation).Assembly);
/// var Encina = services.BuildServiceProvider().GetRequiredService&lt;IEncina&gt;();
///
/// var result = await Encina.Send(new CreateReservation(/* ... */), cancellationToken);
///
/// await result.Match(
///     Left: error =>
///     {
///         Console.WriteLine($"Reservation failed: {error.GetEncinaCode()} - {error.Message}");
///         return Task.CompletedTask;
///     },
///     Right: reservation => Encina.Publish(new ReservationCreatedNotification(reservation), cancellationToken));
/// </code>
/// </example>
[System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
    Justification = "Pre-1.0: the explicit-context overloads mirror the ambient ones; the context parameter is required, so calls never become ambiguous.")]
public interface IEncina
{
    /// <summary>
    /// Sends a request that expects a <typeparamref name="TResponse"/> response.
    /// </summary>
    /// <typeparam name="TResponse">Response type returned by the handler.</typeparam>
    /// <param name="request">Request to process.</param>
    /// <param name="cancellationToken">Optional token to cancel the operation.</param>
    /// <returns>Response produced by the handler after flowing through the pipeline.</returns>
    /// <remarks>
    /// The pipeline's <see cref="IRequestContext"/> is the ambient one held by
    /// <see cref="IRequestContextAccessor"/> (filled, for example, by <c>EncinaContextMiddleware</c>
    /// for an HTTP request). When there is none, a fresh context is created with a correlation id
    /// taken from <see cref="System.Diagnostics.Activity.Current"/>.
    /// </remarks>
    ValueTask<Either<EncinaError, TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request with an explicit <see cref="IRequestContext"/>.
    /// </summary>
    /// <typeparam name="TResponse">Response type returned by the handler.</typeparam>
    /// <param name="request">Request to process.</param>
    /// <param name="context">
    /// Context the pipeline runs with. It takes precedence over the ambient context and becomes
    /// the ambient context for the duration of the call, so nested requests see it too. An
    /// anonymous context is always accepted. A context with an authenticated identity is refused
    /// with <see cref="RequestIdentityErrorCodes.ScopeConflict"/> when the scope that issued that
    /// identity has ended, when the current flow runs (or ran) for a user and the identity is not
    /// that user's, or when the identity was not issued by a scope and is not the identity of an
    /// active scope in the current flow. When the current flow runs for a user, a context with a
    /// different tenant is refused with <see cref="RequestIdentityErrorCodes.TenantConflict"/>.
    /// </param>
    /// <param name="cancellationToken">Optional token to cancel the operation.</param>
    /// <returns>Response produced by the handler after flowing through the pipeline.</returns>
    /// <remarks>
    /// An explicit context needs a live, issued identity: it cannot bind an identity by itself.
    /// Background jobs open a declared service identity with
    /// <see cref="IRequestContextScopeFactory.RunAsServiceAsync{T}"/>, and deferred dispatch (outbox,
    /// inbox, scheduled messages) restores the originating actor with
    /// <see cref="IRequestContextScopeFactory.RunRestoredAsync{T}"/>; inside the scope, use the
    /// overloads without a context or pass the scope's context.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
    ValueTask<Either<EncinaError, TResponse>> Send<TResponse>(IRequest<TResponse> request, IRequestContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification that may be handled by zero or more handlers.
    /// </summary>
    /// <typeparam name="TNotification">Notification type being distributed.</typeparam>
    /// <param name="notification">Instance to propagate.</param>
    /// <param name="cancellationToken">Optional token to cancel the dispatch.</param>
    /// <remarks>
    /// Handlers observe the ambient context through <see cref="IRequestContextAccessor"/>; when
    /// there is none, a fresh context is created for the dispatch.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    /// Publishes a notification with an explicit <see cref="IRequestContext"/>.
    /// </summary>
    /// <typeparam name="TNotification">Notification type being distributed.</typeparam>
    /// <param name="notification">Instance to propagate.</param>
    /// <param name="context">
    /// Context the handlers run with. It takes precedence over the ambient context and is the
    /// ambient context for the duration of the dispatch. The same rules as the explicit
    /// <c>Send</c> overload apply: an authenticated identity whose issuing scope has ended, that is
    /// not the user of the current flow, or that was not issued by an active scope of the same
    /// identity is refused with <see cref="RequestIdentityErrorCodes.ScopeConflict"/>, and a
    /// different tenant under a user with <see cref="RequestIdentityErrorCodes.TenantConflict"/>.
    /// </param>
    /// <param name="cancellationToken">Optional token to cancel the dispatch.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
    ValueTask<Either<EncinaError, Unit>> Publish<TNotification>(TNotification notification, IRequestContext context, CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    /// Sends a streaming request that produces a sequence of items asynchronously.
    /// </summary>
    /// <typeparam name="TItem">Type of each item yielded by the stream.</typeparam>
    /// <param name="request">Stream request to process.</param>
    /// <param name="cancellationToken">Optional token to cancel the stream iteration.</param>
    /// <returns>
    /// Async enumerable of <c>Either&lt;EncinaError, TItem&gt;</c>, where each element
    /// represents either an error (Left) or a successful item (Right).
    /// </returns>
    /// <remarks>
    /// <para>
    /// Stream requests enable efficient processing of large datasets, real-time feeds,
    /// and batch operations without loading all data into memory at once.
    /// </para>
    /// <para>
    /// The returned stream flows through all registered <see cref="IStreamPipelineBehavior{TRequest, TItem}"/>
    /// instances before reaching the handler. Each behavior can transform, filter, or enrich items.
    /// </para>
    /// <para>
    /// Use <c>await foreach</c> to consume the stream. Dispose or break early to trigger cancellation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await foreach (var result in Encina.Stream(new StreamProductsQuery(), cancellationToken))
    /// {
    ///     result.Match(
    ///         Left: error => _logger.LogError("Failed to fetch product: {ErrorCode}", error.GetCode().IfNone("encina.unknown")),
    ///         Right: product => Console.WriteLine($"Product: {product.Name}"));
    /// }
    /// </code>
    /// </example>
    IAsyncEnumerable<Either<EncinaError, TItem>> Stream<TItem>(IStreamRequest<TItem> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a streaming request with an explicit <see cref="IRequestContext"/>.
    /// </summary>
    /// <typeparam name="TItem">Type of each item yielded by the stream.</typeparam>
    /// <param name="request">Stream request to process.</param>
    /// <param name="context">
    /// Context the stream pipeline runs with. It takes precedence over the ambient context and is
    /// the ambient context while the stream is enumerated. The context is checked at the first
    /// <c>MoveNextAsync</c>, with the same rules as the explicit <c>Send</c> overload: an
    /// authenticated identity whose issuing scope has ended, that is not the user of the current
    /// flow, or that was not issued by an active scope of the same identity is refused with
    /// <see cref="RequestIdentityErrorCodes.ScopeConflict"/>, and a different tenant under a user
    /// with <see cref="RequestIdentityErrorCodes.TenantConflict"/>. Once the issuing scope ends,
    /// the remaining steps read no identity.
    /// </param>
    /// <param name="cancellationToken">Optional token to cancel the stream iteration.</param>
    /// <returns>
    /// Async enumerable of <c>Either&lt;EncinaError, TItem&gt;</c>, where each element
    /// represents either an error (Left) or a successful item (Right).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
    IAsyncEnumerable<Either<EncinaError, TItem>> Stream<TItem>(IStreamRequest<TItem> request, IRequestContext context, CancellationToken cancellationToken = default);
}
