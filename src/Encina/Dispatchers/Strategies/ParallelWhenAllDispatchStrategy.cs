using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dispatchers.Strategies;

/// <summary>
/// Dispatches notifications to all handlers in parallel, waiting for all to complete.
/// Errors are aggregated into a single <see cref="EncinaError"/> containing all failure details.
/// </summary>
/// <remarks>
/// <para>Suitable when all handlers must attempt execution regardless of individual failures.</para>
/// <para>Use this strategy for scenarios like logging, metrics, or notification fanout where
/// partial success is acceptable and you want visibility into all failures.</para>
/// </remarks>
internal sealed class ParallelWhenAllDispatchStrategy : INotificationDispatchStrategy
{
    private readonly int _maxDegreeOfParallelism;

    /// <summary>
    /// Creates a new parallel-when-all dispatch strategy.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Maximum concurrent handlers. -1 for unlimited.</param>
    public ParallelWhenAllDispatchStrategy(int maxDegreeOfParallelism = -1)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism == -1
            ? Environment.ProcessorCount
            : maxDegreeOfParallelism;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> DispatchAsync<TNotification>(
        IReadOnlyList<object> handlers,
        TNotification notification,
        Func<object, TNotification, CancellationToken, Task<Either<EncinaError, Unit>>> invoker,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (handlers.Count == 0)
        {
            return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
        }

        // Single handler - no parallelism needed
        if (handlers.Count == 1)
        {
            var handler = handlers[0];
            return handler is null
                ? Right<EncinaError, Unit>(Unit.Default) // NOSONAR S6966: LanguageExt Right is a pure function
                : await invoker(handler, notification, cancellationToken).ConfigureAwait(false);
        }

        // Use SemaphoreSlim for throttling
        using var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism, _maxDegreeOfParallelism);

        var tasks = new List<Task<(object Handler, Either<EncinaError, Unit> Result)>>(handlers.Count);

        foreach (var handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            var capturedHandler = handler;
            tasks.Add(ExecuteWithThrottlingAsync(capturedHandler, notification, invoker, semaphore, cancellationToken));
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // Collect all errors
        var errors = new List<(object Handler, EncinaError Error)>();
        foreach ((var handler, var result) in results)
        {
            if (result.IsLeft)
            {
                var error = result.Match(
                    Left: err => err,
                    Right: _ => EncinaErrors.Unknown);
                errors.Add((handler, error));
            }
        }

        if (errors.Count == 0)
        {
            return Right<EncinaError, Unit>(Unit.Default); // NOSONAR S6966: LanguageExt Right is a pure function
        }

        if (errors.Count == 1)
        {
            return Left<EncinaError, Unit>(errors[0].Error); // NOSONAR S6966: LanguageExt Left is a pure function
        }

        // Aggregate multiple errors
        return Left<EncinaError, Unit>(CreateAggregateError(errors, notification)); // NOSONAR S6966: LanguageExt Left is a pure function
    }

    private static async Task<(object Handler, Either<EncinaError, Unit> Result)> ExecuteWithThrottlingAsync<TNotification>(
        object handler,
        TNotification notification,
        Func<object, TNotification, CancellationToken, Task<Either<EncinaError, Unit>>> invoker,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await invoker(handler, notification, cancellationToken).ConfigureAwait(false);
            return (handler, result);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var error = EncinaErrors.Create(
                EncinaErrorCodes.NotificationCancelled,
                $"Handler {handler.GetType().Name} was cancelled.",
                ex);
            return (handler, Left<EncinaError, Unit>(error)); // NOSONAR S6966: LanguageExt Left is a pure function
        }
        catch (Exception ex)
        {
            var error = EncinaErrors.FromException(
                EncinaErrorCodes.NotificationException,
                ex,
                $"Handler {handler.GetType().Name} threw an unexpected exception.");
            return (handler, Left<EncinaError, Unit>(error)); // NOSONAR S6966: LanguageExt Left is a pure function
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static EncinaError CreateAggregateError<TNotification>(
        List<(object Handler, EncinaError Error)> errors,
        TNotification notification)
    {
        var notificationName = notification?.GetType().Name ?? typeof(TNotification).Name;
        var message = $"Multiple notification handlers failed for {notificationName} ({errors.Count} errors)";

        var errorDetails = errors.Select(e => new Dictionary<string, object?>
        {
            ["handler"] = e.Handler.GetType().FullName,
            ["code"] = e.Error.GetEncinaCode(),
            ["message"] = e.Error.Message
        }).ToList();

        var metadata = new Dictionary<string, object?>
        {
            ["notification"] = notificationName,
            ["error_count"] = errors.Count,
            ["errors"] = errorDetails,
            ["failed_handlers"] = errors.Select(e => e.Handler.GetType().Name).ToList()
        };

        return EncinaErrors.Create(
            EncinaErrorCodes.NotificationMultipleFailures,
            message,
            details: metadata);
    }
}
