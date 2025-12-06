using System.Threading;
using System.Threading.Tasks;

namespace SimpleMediator;

/// <summary>
/// Procesa una notificación publicada por el mediador.
/// </summary>
/// <typeparam name="TNotification">Tipo de evento atendido.</typeparam>
/// <remarks>
/// Los handlers se ejecutan secuencialmente en el orden de resolución dentro del contenedor.
/// Deben ser idempotentes y tolerar la existencia de múltiples consumidores.
/// </remarks>
/// <example>
/// <code>
/// public sealed class AuditReservationHandler : INotificationHandler&lt;ReservationCreatedNotification&gt;
/// {
///     public Task Handle(ReservationCreatedNotification notification, CancellationToken cancellationToken)
///     {
///         auditLog.Record(notification.ReservationId);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Ejecuta la lógica asociada a la notificación recibida.
    /// </summary>
    /// <param name="notification">Evento o señal propagada.</param>
    /// <param name="cancellationToken">Token para cancelar la operación si es necesario.</param>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
