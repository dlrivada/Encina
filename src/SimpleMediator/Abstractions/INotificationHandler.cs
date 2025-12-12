namespace SimpleMediator;

/// <summary>
/// Processes a notification published by the mediator.
/// </summary>
/// <typeparam name="TNotification">Notification type being handled.</typeparam>
/// <remarks>
/// Handlers run sequentially following the container resolution order.
/// They must be idempotent and tolerate the presence of multiple consumers.
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
    /// Executes the logic associated with the received notification.
    /// </summary>
    /// <param name="notification">Event or signal to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation when needed.</param>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
