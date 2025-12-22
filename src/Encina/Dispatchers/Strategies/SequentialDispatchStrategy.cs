using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dispatchers.Strategies;

/// <summary>
/// Dispatches notifications sequentially to handlers in registration order.
/// First error stops execution (fail-fast behavior).
/// </summary>
/// <remarks>
/// This is the default strategy and maintains backward compatibility with existing behavior.
/// </remarks>
internal sealed class SequentialDispatchStrategy : INotificationDispatchStrategy
{
    /// <summary>
    /// Singleton instance of the sequential strategy.
    /// </summary>
    public static readonly SequentialDispatchStrategy Instance = new();

    private SequentialDispatchStrategy() { }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> DispatchAsync<TNotification>(
        IReadOnlyList<object> handlers,
        TNotification notification,
        Func<object, TNotification, CancellationToken, Task<Either<EncinaError, Unit>>> invoker,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        for (var i = 0; i < handlers.Count; i++)
        {
            var handler = handlers[i];
            if (handler is null)
            {
                continue;
            }

            var result = await invoker(handler, notification, cancellationToken).ConfigureAwait(false);
            if (result.IsLeft)
            {
                return result; // Fail-fast: stop on first error
            }
        }

        return Right<EncinaError, Unit>(Unit.Default);
    }
}
