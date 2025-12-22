using Microsoft.Extensions.Logging;

namespace Encina.Wolverine;

/// <summary>
/// Base class for Wolverine handlers that bridge to Encina requests.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public abstract class WolverineRequestHandler<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
{
    private readonly IEncina _Encina;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WolverineRequestHandler{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="Encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    protected WolverineRequestHandler(
        IEncina Encina,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(Encina);
        ArgumentNullException.ThrowIfNull(logger);

        _Encina = Encina;
        _logger = logger;
    }

    /// <summary>
    /// Handles the incoming Wolverine message by delegating to Encina.
    /// </summary>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response or throws on error.</returns>
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Log.HandlingMessage(_logger, typeof(TRequest).Name);

        var result = await _Encina.Send<TResponse>(
            request,
            cancellationToken).ConfigureAwait(false);

        return result.Match(
            Right: response =>
            {
                Log.SuccessfullyHandledMessage(_logger, typeof(TRequest).Name);
                return response;
            },
            Left: error =>
            {
                Log.MessageHandlingFailed(_logger, typeof(TRequest).Name, error.Message);
                throw new WolverineEncinaException(error);
            });
    }
}

/// <summary>
/// Base class for Wolverine handlers that bridge to Encina notifications.
/// </summary>
/// <typeparam name="TNotification">The type of the notification.</typeparam>
public abstract class WolverineNotificationHandler<TNotification>
    where TNotification : class, INotification
{
    private readonly IEncina _Encina;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WolverineNotificationHandler{TNotification}"/> class.
    /// </summary>
    /// <param name="Encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    protected WolverineNotificationHandler(
        IEncina Encina,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(Encina);
        ArgumentNullException.ThrowIfNull(logger);

        _Encina = Encina;
        _logger = logger;
    }

    /// <summary>
    /// Handles the incoming Wolverine notification by delegating to Encina.
    /// </summary>
    /// <param name="notification">The notification message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task Handle(TNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        Log.HandlingNotification(_logger, typeof(TNotification).Name);

        await _Encina.Publish(
            notification,
            cancellationToken).ConfigureAwait(false);

        Log.SuccessfullyPublishedNotification(_logger, typeof(TNotification).Name);
    }
}
