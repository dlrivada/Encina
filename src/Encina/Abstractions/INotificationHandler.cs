using LanguageExt;

namespace Encina;

/// <summary>
/// Processes a notification published by the Encina using Railway Oriented Programming.
/// </summary>
/// <typeparam name="TNotification">Notification type being handled.</typeparam>
/// <remarks>
/// <para>
/// Handlers run sequentially following the container resolution order.
/// They must be idempotent and tolerate the presence of multiple consumers.
/// </para>
/// <para>
/// Handlers return <see cref="Either{L,R}"/> to enable explicit error handling without exceptions.
/// Return <c>Right(Unit.Default)</c> for success or <c>Left(error)</c> if the handler cannot process the notification.
/// The first handler that returns Left will stop the notification propagation (fail-fast).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class AuditReservationHandler : INotificationHandler&lt;ReservationCreatedNotification&gt;
/// {
///     public async Task&lt;Either&lt;EncinaError, Unit&gt;&gt; Handle(ReservationCreatedNotification notification, CancellationToken cancellationToken)
///     {
///         var reservation = await _repository.FindAsync(notification.ReservationId, cancellationToken);
///         if (reservation is null)
///             return Left(EncinaErrors.NotFound("Reservation not found for audit"));
///
///         await _auditLog.RecordAsync(notification.ReservationId, cancellationToken);
///         return Right(Unit.Default);
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
    /// <returns>
    /// Either a <see cref="EncinaError"/> (Left) if the handler cannot process the notification,
    /// or <see cref="Unit"/> (Right) on successful processing.
    /// </returns>
    /// <remarks>
    /// Use <c>static LanguageExt.Prelude</c> to access <c>Left</c> and <c>Right</c> factory methods.
    /// If this handler returns Left, subsequent handlers will not be executed (fail-fast behavior).
    /// </remarks>
    Task<Either<EncinaError, Unit>> Handle(TNotification notification, CancellationToken cancellationToken);
}
