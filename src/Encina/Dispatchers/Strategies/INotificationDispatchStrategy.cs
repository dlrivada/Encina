using LanguageExt;

namespace Encina.Dispatchers.Strategies;

/// <summary>
/// Defines a strategy for dispatching notifications to multiple handlers.
/// </summary>
internal interface INotificationDispatchStrategy
{
    /// <summary>
    /// Dispatches a notification to all handlers using the strategy's execution pattern.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification.</typeparam>
    /// <param name="handlers">The list of handlers to invoke.</param>
    /// <param name="notification">The notification to dispatch.</param>
    /// <param name="invoker">The delegate to invoke each handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either a success unit or an error.</returns>
    Task<Either<EncinaError, Unit>> DispatchAsync<TNotification>(
        IReadOnlyList<object> handlers,
        TNotification notification,
        Func<object, TNotification, CancellationToken, Task<Either<EncinaError, Unit>>> invoker,
        CancellationToken cancellationToken)
        where TNotification : INotification;
}
