namespace Encina.MassTransit;

/// <summary>
/// Interface for publishing Encina messages through MassTransit.
/// </summary>
public interface IMassTransitMessagePublisher
{
    /// <summary>
    /// Publishes a notification through MassTransit to all subscribers.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : class, INotification;

    /// <summary>
    /// Sends a request to a specific endpoint through MassTransit.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="destinationAddress">The destination endpoint address.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAsync<TRequest, TResponse>(
        TRequest request,
        Uri destinationAddress,
        CancellationToken cancellationToken = default)
        where TRequest : class, IRequest<TResponse>;
}
