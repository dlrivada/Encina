namespace SimpleMediator;

/// <summary>
/// Señal o evento que puede ser publicado a múltiples handlers.
/// </summary>
/// <remarks>
/// A diferencia de <see cref="IRequest{TResponse}"/>, las notificaciones no esperan respuesta.
/// Resultan útiles para propagar eventos de dominio o integrar procesos asíncronos.
/// </remarks>
/// <example>
/// <code>
/// public sealed record InvoiceIssuedNotification(Guid InvoiceId) : INotification;
///
/// public sealed class NotifyAccountingHandler : INotificationHandler&lt;InvoiceIssuedNotification&gt;
/// {
///     public Task Handle(InvoiceIssuedNotification notification, CancellationToken cancellationToken)
///     {
///         // Enviar mensaje al sistema contable...
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface INotification
{
}
