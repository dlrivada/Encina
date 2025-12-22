namespace Encina;

/// <summary>
/// Signal or event that can be published to multiple handlers.
/// </summary>
/// <remarks>
/// Unlike <see cref="IRequest{TResponse}"/>, notifications do not expect a response.
/// They are handy to propagate domain events or integrate asynchronous processes.
/// </remarks>
/// <example>
/// <code>
/// public sealed record InvoiceIssuedNotification(Guid InvoiceId) : INotification;
///
/// public sealed class NotifyAccountingHandler : INotificationHandler&lt;InvoiceIssuedNotification&gt;
/// {
///     public Task Handle(InvoiceIssuedNotification notification, CancellationToken cancellationToken)
///     {
///         // Send a message to the accounting system...
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface INotification
{
}
