namespace Encina.Security.ABAC;

/// <summary>
/// Runs a store call with a hard time bound: the call gets a token that is cancelled at the bound
/// and is also raced against it, so a store that ignores its token still cannot hold the caller
/// longer than the bound.
/// </summary>
/// <remarks>
/// <para>
/// The token alone is cooperative: a store that never observes it would make the caller wait
/// forever, which under a fail-closed audit is worse than a denial. <see cref="Task.WaitAsync(TimeSpan, TimeProvider)"/>
/// gives up waiting at the bound; the abandoned call keeps running, and a later fault of it is
/// observed so it never surfaces as an unobserved task exception.
/// </para>
/// <para>
/// The bound is never linked to a caller's token: the decision audit must survive a client that
/// disconnects. The policy administration point's audit write (#1704) can reuse this helper.
/// </para>
/// </remarks>
internal static class TimeBoundedCall
{
    /// <summary>
    /// Runs <paramref name="operation"/> and returns its result, or throws <see cref="TimeoutException"/>
    /// when it does not complete within <paramref name="timeout"/>.
    /// </summary>
    /// <typeparam name="T">The result type of the call.</typeparam>
    /// <param name="operation">The call; it receives a token cancelled at the bound.</param>
    /// <param name="timeout">The bound; it must be greater than zero.</param>
    /// <param name="timeProvider">The clock of the bound.</param>
    /// <returns>The result of the call.</returns>
    /// <exception cref="TimeoutException">The call did not complete within the bound, whether it ignored its token or observed it.</exception>
    public static async Task<T> RunAsync<T>(
        Func<CancellationToken, ValueTask<T>> operation,
        TimeSpan timeout,
        TimeProvider timeProvider)
    {
        using var bound = new CancellationTokenSource(timeout, timeProvider);
        var call = operation(bound.Token).AsTask();

        try
        {
            return await call.WaitAsync(timeout, timeProvider).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            ObserveLateFault(call);
            throw;
        }
        catch (OperationCanceledException) when (bound.IsCancellationRequested)
        {
            // The store observed the bound's token: the same timeout, reported the same way.
            throw new TimeoutException("The call did not complete within its time bound.");
        }
    }

    private static void ObserveLateFault(Task call) =>
        _ = call.ContinueWith(
            static faulted => _ = faulted.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
}
