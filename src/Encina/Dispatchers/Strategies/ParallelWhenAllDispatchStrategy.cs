using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dispatchers.Strategies;

/// <summary>
/// Dispatches notifications to all handlers in parallel, waiting for all to complete.
/// Errors are aggregated into a single <see cref="MediatorError"/> containing all failure details.
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
    public async Task<Either<MediatorError, Unit>> DispatchAsync<TNotification>(
        IReadOnlyList<object> handlers,
        TNotification notification,
        Func<object, TNotification, CancellationToken, Task<Either<MediatorError, Unit>>> invoker,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (handlers.Count == 0)
        {
            return Right<MediatorError, Unit>(Unit.Default);
        }

        // Single handler - no parallelism needed
        if (handlers.Count == 1)
        {
            var handler = handlers[0];
            return handler is null
                ? Right<MediatorError, Unit>(Unit.Default)
                : await invoker(handler, notification, cancellationToken).ConfigureAwait(false);
        }

        // Use SemaphoreSlim for throttling
        using var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism, _maxDegreeOfParallelism);

        var tasks = new List<Task<(object Handler, Either<MediatorError, Unit> Result)>>(handlers.Count);

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
        var errors = new List<(object Handler, MediatorError Error)>();
        foreach (var (handler, result) in results)
        {
            if (result.IsLeft)
            {
                var error = result.Match(
                    Left: err => err,
                    Right: _ => MediatorErrors.Unknown);
                errors.Add((handler, error));
            }
        }

        if (errors.Count == 0)
        {
            return Right<MediatorError, Unit>(Unit.Default);
        }

        if (errors.Count == 1)
        {
            return Left<MediatorError, Unit>(errors[0].Error);
        }

        // Aggregate multiple errors
        return Left<MediatorError, Unit>(CreateAggregateError(errors, notification));
    }

    private static async Task<(object Handler, Either<MediatorError, Unit> Result)> ExecuteWithThrottlingAsync<TNotification>(
        object handler,
        TNotification notification,
        Func<object, TNotification, CancellationToken, Task<Either<MediatorError, Unit>>> invoker,
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
            var error = MediatorErrors.Create(
                MediatorErrorCodes.NotificationCancelled,
                $"Handler {handler.GetType().Name} was cancelled.",
                ex);
            return (handler, Left<MediatorError, Unit>(error));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException(
                MediatorErrorCodes.NotificationException,
                ex,
                $"Handler {handler.GetType().Name} threw an unexpected exception.");
            return (handler, Left<MediatorError, Unit>(error));
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static MediatorError CreateAggregateError<TNotification>(
        List<(object Handler, MediatorError Error)> errors,
        TNotification notification)
    {
        var notificationName = notification?.GetType().Name ?? typeof(TNotification).Name;
        var message = $"Multiple notification handlers failed for {notificationName} ({errors.Count} errors)";

        var errorDetails = errors.Select(e => new Dictionary<string, object?>
        {
            ["handler"] = e.Handler.GetType().FullName,
            ["code"] = e.Error.GetMediatorCode(),
            ["message"] = e.Error.Message
        }).ToList();

        var metadata = new Dictionary<string, object?>
        {
            ["notification"] = notificationName,
            ["error_count"] = errors.Count,
            ["errors"] = errorDetails,
            ["failed_handlers"] = errors.Select(e => e.Handler.GetType().Name).ToList()
        };

        return MediatorErrors.Create(
            MediatorErrorCodes.NotificationMultipleFailures,
            message,
            details: metadata);
    }
}
