using MassTransit;
using Microsoft.Extensions.Logging;

namespace Encina.MassTransit;

/// <summary>
/// Publishes Encina notifications through MassTransit message bus.
/// </summary>
public sealed class MassTransitMessagePublisher : IMassTransitMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ILogger<MassTransitMessagePublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MassTransitMessagePublisher"/> class.
    /// </summary>
    /// <param name="publishEndpoint">The MassTransit publish endpoint.</param>
    /// <param name="sendEndpointProvider">The MassTransit send endpoint provider.</param>
    /// <param name="logger">The logger instance.</param>
    public MassTransitMessagePublisher(
        IPublishEndpoint publishEndpoint,
        ISendEndpointProvider sendEndpointProvider,
        ILogger<MassTransitMessagePublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ArgumentNullException.ThrowIfNull(sendEndpointProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _publishEndpoint = publishEndpoint;
        _sendEndpointProvider = sendEndpointProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : class, INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = typeof(TNotification).Name;

        Log.PublishingNotification(_logger, notificationType);

        await _publishEndpoint.Publish(notification, cancellationToken)
            .ConfigureAwait(false);

        Log.PublishedNotification(_logger, notificationType);
    }

    /// <inheritdoc />
    public async Task SendAsync<TRequest, TResponse>(
        TRequest request,
        Uri destinationAddress,
        CancellationToken cancellationToken = default)
        where TRequest : class, IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(destinationAddress);

        var requestType = typeof(TRequest).Name;

        Log.SendingRequest(_logger, requestType, destinationAddress);

        var sendEndpoint = await _sendEndpointProvider
            .GetSendEndpoint(destinationAddress)
            .ConfigureAwait(false);

        await sendEndpoint.Send(request, cancellationToken)
            .ConfigureAwait(false);

        Log.SentRequest(_logger, requestType, destinationAddress);
    }
}
