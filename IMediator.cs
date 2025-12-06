using System.Threading;
using System.Threading.Tasks;

namespace SimpleMediator;

/// <summary>
/// Coordinador central para enviar comandos/consultas y publicar notificaciones.
/// </summary>
/// <remarks>
/// La implementación por defecto (<see cref="SimpleMediator"/>) crea un ámbito de DI por
/// operación, ejecuta behaviors en cascada y delega en los handlers registrados.
/// </remarks>
/// <example>
/// <code>
/// var services = new ServiceCollection();
/// services.AddSimpleMediator(typeof(CreateReservation).Assembly);
/// var mediator = services.BuildServiceProvider().GetRequiredService&lt;IMediator&gt;();
///
/// var result = await mediator.Send(new CreateReservation(/* ... */), cancellationToken);
/// await mediator.Publish(new ReservationCreatedNotification(result.Value), cancellationToken);
/// </code>
/// </example>
public interface IMediator
{
    /// <summary>
    /// Envía una solicitud que espera una respuesta de tipo <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">Tipo devuelto por el manejador asociado.</typeparam>
    /// <param name="request">Solicitud a procesar.</param>
    /// <param name="cancellationToken">Token opcional para cancelar la operación.</param>
    /// <returns>Respuesta producida por el handler tras pasar por el pipeline.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publica una notificación que puede ser manejada por cero o más handlers.
    /// </summary>
    /// <typeparam name="TNotification">Tipo de notificación distribuida.</typeparam>
    /// <param name="notification">Instancia a propagar.</param>
    /// <param name="cancellationToken">Token opcional para cancelar la difusión.</param>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
