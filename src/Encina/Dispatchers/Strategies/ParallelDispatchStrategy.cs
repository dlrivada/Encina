using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dispatchers.Strategies;

/// <summary>
/// Dispatches notifications to handlers in parallel with fail-fast behavior.
/// First error cancels remaining handlers via linked cancellation token.
/// </summary>
/// <remarks>
/// <para>Uses Task.WhenAll with linked CancellationTokenSource to detect first failure and cancel remaining work.</para>
/// <para>Suitable when handlers are independent and early failure detection is desired.</para>
/// </remarks>
internal sealed class ParallelDispatchStrategy : INotificationDispatchStrategy
{
    private readonly int _maxDegreeOfParallelism;

    /// <summary>
    /// Creates a new parallel dispatch strategy.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Maximum concurrent handlers. -1 for unlimited.</param>
    public ParallelDispatchStrategy(int maxDegreeOfParallelism = -1)
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

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var linkedToken = cts.Token;

        // Use SemaphoreSlim for throttling if max parallelism is set
        using var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism, _maxDegreeOfParallelism);

        // Use a wrapper class to capture the first error (can't use ref with async)
        var errorHolder = new ErrorHolder();
        var tasks = new List<Task>(handlers.Count);

        foreach (var handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            var capturedHandler = handler;
            tasks.Add(ExecuteWithThrottlingAsync(
                capturedHandler,
                notification,
                invoker,
                semaphore,
                cts,
                errorHolder,
                linkedToken));
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (errorHolder.Error is not null)
        {
            // Expected when we cancel due to handler error
        }

        // NOSONAR S6966: LanguageExt Left/Right are pure functions, not async operations
        return errorHolder.Error is not null
            ? Left<EncinaError, Unit>(errorHolder.Error)
            : Right<EncinaError, Unit>(Unit.Default);
    }

    private static async Task ExecuteWithThrottlingAsync<TNotification>(
        object handler,
        TNotification notification,
        Func<object, TNotification, CancellationToken, Task<Either<EncinaError, Unit>>> invoker,
        SemaphoreSlim semaphore,
        CancellationTokenSource cts,
        ErrorHolder errorHolder,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        try
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return; // Cancelled before we could start
        }

        try
        {
            // Check if already cancelled by another handler's failure
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var result = await invoker(handler, notification, cancellationToken).ConfigureAwait(false);

            if (result.IsLeft)
            {
                lock (errorHolder)
                {
                    // Only capture the first error
                    if (errorHolder.Error is null)
                    {
                        errorHolder.Error = result.Match(
                            Left: err => err,
                            Right: _ => EncinaErrors.Unknown);

                        // Cancel remaining handlers
                        try
                        {
                            cts.Cancel();
                        }
                        catch (ObjectDisposedException)
                        {
                            // CTS already disposed, ignore
                        }
                    }
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Wrapper class to hold the first error (needed because async methods can't use ref parameters).
    /// </summary>
    private sealed class ErrorHolder
    {
        public EncinaError? Error { get; set; }
    }
}
