using System.Threading;
using System.Threading.Tasks;
using LanguageExt;

namespace SimpleMediator;

/// <summary>
/// Central coordinator used to send commands/queries and publish notifications.
/// </summary>
/// <remarks>
/// The default implementation (<see cref="SimpleMediator"/>) creates a DI scope per operation,
/// runs behaviors in cascade, and delegates to the registered handlers.
/// </remarks>
/// <example>
/// <code>
/// var services = new ServiceCollection();
/// services.AddSimpleMediator(typeof(CreateReservation).Assembly);
/// var mediator = services.BuildServiceProvider().GetRequiredService&lt;IMediator&gt;();
///
/// var result = await mediator.Send(new CreateReservation(/* ... */), cancellationToken);
///
/// await result.Match(
///     Left: error =>
///     {
///         Console.WriteLine($"Reservation failed: {error.GetMediatorCode()} - {error.Message}");
///         return Task.CompletedTask;
///     },
///     Right: reservation => mediator.Publish(new ReservationCreatedNotification(reservation), cancellationToken));
/// </code>
/// </example>
public interface IMediator
{
    /// <summary>
    /// Sends a request that expects a <typeparamref name="TResponse"/> response.
    /// </summary>
    /// <typeparam name="TResponse">Response type returned by the handler.</typeparam>
    /// <param name="request">Request to process.</param>
    /// <param name="cancellationToken">Optional token to cancel the operation.</param>
    /// <returns>Response produced by the handler after flowing through the pipeline.</returns>
    Task<Either<Error, TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification that may be handled by zero or more handlers.
    /// </summary>
    /// <typeparam name="TNotification">Notification type being distributed.</typeparam>
    /// <param name="notification">Instance to propagate.</param>
    /// <param name="cancellationToken">Optional token to cancel the dispatch.</param>
    Task<Either<Error, Unit>> Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
