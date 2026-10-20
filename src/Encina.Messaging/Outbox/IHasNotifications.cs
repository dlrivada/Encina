namespace Encina.Messaging.Outbox;

/// <summary>
/// Marker interface for requests that can emit notifications.
/// </summary>
/// <remarks>
/// The notifications are read after the handler has run and written to the outbox inside the request's
/// transaction, so a request that fails or rolls back leaves no outbox row.
/// </remarks>
public interface IHasNotifications
{
    /// <summary>
    /// Gets the notifications to be published.
    /// </summary>
    IEnumerable<INotification> GetNotifications();
}
