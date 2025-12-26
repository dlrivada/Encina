namespace Encina.Polly;

/// <summary>
/// Manages bulkhead semaphores for limiting concurrent executions per key.
/// </summary>
/// <remarks>
/// The bulkhead manager maintains a collection of semaphores, one per key (typically request type).
/// This enables isolation between different handler types, preventing one slow handler
/// from consuming all available threads.
/// </remarks>
public interface IBulkheadManager
{
    /// <summary>
    /// Attempts to acquire a permit from the bulkhead for the specified key.
    /// </summary>
    /// <param name="key">The key identifying the bulkhead (typically request type name).</param>
    /// <param name="config">Bulkhead configuration from the attribute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="BulkheadAcquireResult"/> indicating whether the permit was acquired
    /// and providing a disposable to release it when done.
    /// </returns>
    ValueTask<BulkheadAcquireResult> TryAcquireAsync(
        string key,
        BulkheadAttribute config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current metrics for the specified bulkhead.
    /// </summary>
    /// <param name="key">The key identifying the bulkhead.</param>
    /// <returns>
    /// The current metrics, or null if no bulkhead exists for the key.
    /// </returns>
    BulkheadMetrics? GetMetrics(string key);

    /// <summary>
    /// Resets the bulkhead for the specified key, clearing all state.
    /// </summary>
    /// <param name="key">The key identifying the bulkhead.</param>
    void Reset(string key);
}

/// <summary>
/// Result of attempting to acquire a bulkhead permit.
/// </summary>
/// <param name="IsAcquired">Whether the permit was successfully acquired.</param>
/// <param name="RejectionReason">
/// The reason for rejection if <paramref name="IsAcquired"/> is false.
/// </param>
/// <param name="Releaser">
/// A disposable that releases the permit when disposed.
/// Only valid when <paramref name="IsAcquired"/> is true.
/// </param>
/// <param name="Metrics">Current bulkhead metrics at the time of acquisition attempt.</param>
public readonly record struct BulkheadAcquireResult(
    bool IsAcquired,
    BulkheadRejectionReason RejectionReason,
    IDisposable? Releaser,
    BulkheadMetrics Metrics)
{
    /// <summary>
    /// Creates a successful acquisition result.
    /// </summary>
    public static BulkheadAcquireResult Acquired(IDisposable releaser, BulkheadMetrics metrics)
        => new(true, BulkheadRejectionReason.None, releaser, metrics);

    /// <summary>
    /// Creates a rejected result due to bulkhead being full.
    /// </summary>
    public static BulkheadAcquireResult RejectedBulkheadFull(BulkheadMetrics metrics)
        => new(false, BulkheadRejectionReason.BulkheadFull, null, metrics);

    /// <summary>
    /// Creates a rejected result due to queue timeout.
    /// </summary>
    public static BulkheadAcquireResult RejectedQueueTimeout(BulkheadMetrics metrics)
        => new(false, BulkheadRejectionReason.QueueTimeout, null, metrics);

    /// <summary>
    /// Creates a rejected result due to cancellation.
    /// </summary>
    public static BulkheadAcquireResult RejectedCancelled(BulkheadMetrics metrics)
        => new(false, BulkheadRejectionReason.Cancelled, null, metrics);
}

/// <summary>
/// Reason why a bulkhead permit was rejected.
/// </summary>
public enum BulkheadRejectionReason
{
    /// <summary>
    /// Permit was acquired successfully (not rejected).
    /// </summary>
    None = 0,

    /// <summary>
    /// Both concurrency and queue limits were reached.
    /// </summary>
    BulkheadFull = 1,

    /// <summary>
    /// Request timed out while waiting in the queue.
    /// </summary>
    QueueTimeout = 2,

    /// <summary>
    /// Request was cancelled while waiting.
    /// </summary>
    Cancelled = 3
}

/// <summary>
/// Metrics for a bulkhead at a point in time.
/// </summary>
/// <param name="CurrentConcurrency">Number of currently executing requests.</param>
/// <param name="CurrentQueuedCount">Number of requests waiting in the queue.</param>
/// <param name="MaxConcurrency">Maximum allowed concurrent executions.</param>
/// <param name="MaxQueuedActions">Maximum allowed queued requests.</param>
/// <param name="TotalAcquired">Total number of permits acquired since creation.</param>
/// <param name="TotalRejected">Total number of requests rejected since creation.</param>
public readonly record struct BulkheadMetrics(
    int CurrentConcurrency,
    int CurrentQueuedCount,
    int MaxConcurrency,
    int MaxQueuedActions,
    long TotalAcquired,
    long TotalRejected)
{
    /// <summary>
    /// Gets the percentage of concurrency capacity in use.
    /// </summary>
    public double ConcurrencyUtilization =>
        MaxConcurrency > 0 ? (double)CurrentConcurrency / MaxConcurrency * 100.0 : 0.0;

    /// <summary>
    /// Gets the percentage of queue capacity in use.
    /// </summary>
    public double QueueUtilization =>
        MaxQueuedActions > 0 ? (double)CurrentQueuedCount / MaxQueuedActions * 100.0 : 0.0;

    /// <summary>
    /// Gets the total rejection rate as a percentage.
    /// </summary>
    public double RejectionRate
    {
        get
        {
            var total = TotalAcquired + TotalRejected;
            return total > 0 ? (double)TotalRejected / total * 100.0 : 0.0;
        }
    }
}
