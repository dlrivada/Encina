using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.SignalR;

/// <summary>
/// Notification handler that broadcasts notifications to SignalR clients.
/// </summary>
/// <typeparam name="TNotification">The type of notification.</typeparam>
/// <remarks>
/// <para>
/// This handler intercepts all notifications and broadcasts those marked with
/// <see cref="BroadcastToSignalRAttribute"/> to SignalR clients.
/// </para>
/// <para>
/// Register this handler in DI to enable automatic SignalR broadcasting.
/// The handler has a low priority so it runs after other handlers.
/// </para>
/// </remarks>
public sealed class SignalRBroadcastHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    private readonly ISignalRNotificationBroadcaster _broadcaster;
    private readonly ILogger<SignalRBroadcastHandler<TNotification>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRBroadcastHandler{TNotification}"/> class.
    /// </summary>
    /// <param name="broadcaster">The SignalR notification broadcaster.</param>
    /// <param name="logger">The logger instance.</param>
    public SignalRBroadcastHandler(
        ISignalRNotificationBroadcaster broadcaster,
        ILogger<SignalRBroadcastHandler<TNotification>> logger)
    {
        _broadcaster = broadcaster;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> Handle(TNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await _broadcaster.BroadcastAsync(notification, cancellationToken).ConfigureAwait(false);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            Log.FailedToBroadcastNotificationToSignalR(_logger, ex, typeof(TNotification).Name);
            // Don't fail the notification - broadcasting is fire-and-forget
            return Right(Unit.Default);
        }
    }
}
