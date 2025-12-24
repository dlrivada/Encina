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
public interface IEncina
{
    /// <summary>
    /// Sends a request that expects a <typeparamref name="TResponse"/> response.
    /// </summary>
    /// <typeparam name="TResponse">Response type returned by the handler.</typeparam>
    /// <param name="request">Request to process.</param>
    /// <param name="cancellationToken">Optional token to cancel the operation.</param>
    /// <returns>Response produced by the handler after flowing through the pipeline.</returns>
    ValueTask<Either<EncinaError, TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification that may be handled by zero or more handlers.
    /// </summary>
    /// <typeparam name="TNotification">Notification type being distributed.</typeparam>
    /// <param name="notification">Instance to propagate.</param>
    /// <param name="cancellationToken">Optional token to cancel the dispatch.</param>
    ValueTask<Either<EncinaError, Unit>> Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
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
    ///         Left: error => _logger.LogError("Failed to fetch product: {Error}", error.Message),
    ///         Right: product => Console.WriteLine($"Product: {product.Name}"));
    /// }
    /// </code>
    /// </example>
    IAsyncEnumerable<Either<EncinaError, TItem>> Stream<TItem>(IStreamRequest<TItem> request, CancellationToken cancellationToken = default);
}
