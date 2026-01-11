using System.Collections.Concurrent;

namespace Encina.Polly;

/// <summary>
/// Default implementation of <see cref="IBulkheadManager"/> using semaphores.
/// </summary>
/// <remarks>
/// This implementation uses <see cref="SemaphoreSlim"/> for each bulkhead key,
/// providing efficient thread-safe concurrency control. Each key (typically a request type)
/// gets its own independent bulkhead.
/// </remarks>
public sealed class BulkheadManager : IBulkheadManager, IDisposable
{
    private readonly ConcurrentDictionary<string, BulkheadBucket> _buckets = new();
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkheadManager"/> class.
    /// </summary>
    public BulkheadManager()
        : this(TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkheadManager"/> class
    /// with a custom time provider for testing.
    /// </summary>
    /// <param name="timeProvider">Time provider for timeout calculations.</param>
    public BulkheadManager(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc/>
    public async ValueTask<BulkheadAcquireResult> TryAcquireAsync(
        string key,
        BulkheadAttribute config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(config);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bucket = _buckets.GetOrAdd(key, _ => new BulkheadBucket(config, _timeProvider));

        return await bucket.TryAcquireAsync(config, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public BulkheadMetrics? GetMetrics(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_buckets.TryGetValue(key, out var bucket))
        {
            return bucket.GetMetrics();
        }

        return null;
    }

    /// <inheritdoc/>
    public void Reset(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_buckets.TryRemove(key, out var bucket))
        {
            bucket.Dispose();
        }
    }

    /// <summary>
    /// Disposes all bulkhead resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var bucket in _buckets.Values)
        {
            bucket.Dispose();
        }

        _buckets.Clear();
    }

    private sealed class BulkheadBucket : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConcurrency;
        private readonly int _maxQueuedActions;

        private long _totalAcquired;
        private long _totalRejected;
        private int _currentQueuedCount;
        private bool _disposed;

        public BulkheadBucket(BulkheadAttribute config, TimeProvider timeProvider)
        {
            _maxConcurrency = config.MaxConcurrency;
            _maxQueuedActions = config.MaxQueuedActions;
            // timeProvider reserved for future use (e.g., timeout tracking, metrics)
            _ = timeProvider;

            // Semaphore starts with full capacity (maxConcurrency)
            _semaphore = new SemaphoreSlim(config.MaxConcurrency, config.MaxConcurrency);
        }

        public async ValueTask<BulkheadAcquireResult> TryAcquireAsync(
            BulkheadAttribute config,
            CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Check if we can try to acquire (either directly or by queuing)
            var currentCount = _semaphore.CurrentCount;
            var currentQueued = Volatile.Read(ref _currentQueuedCount);

            // If semaphore is at 0 (all permits in use) and queue is full, reject immediately
            if (currentCount == 0 && currentQueued >= _maxQueuedActions)
            {
                Interlocked.Increment(ref _totalRejected);
                return BulkheadAcquireResult.RejectedBulkheadFull(GetMetrics());
            }

            // Track that we're queued (if semaphore count is 0)
            if (currentCount == 0)
            {
                Interlocked.Increment(ref _currentQueuedCount);
            }

            try
            {
                // Try to acquire with timeout
                var timeout = TimeSpan.FromMilliseconds(config.QueueTimeoutMs);
                using var timeoutCts = new CancellationTokenSource(timeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCts.Token);

                bool acquired;
                try
                {
                    acquired = await _semaphore.WaitAsync(timeout, linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    Interlocked.Increment(ref _totalRejected);
                    return BulkheadAcquireResult.RejectedQueueTimeout(GetMetrics());
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref _totalRejected);
                    return BulkheadAcquireResult.RejectedCancelled(GetMetrics());
                }

                if (!acquired)
                {
                    Interlocked.Increment(ref _totalRejected);
                    return BulkheadAcquireResult.RejectedQueueTimeout(GetMetrics());
                }

                Interlocked.Increment(ref _totalAcquired);
                var releaser = new BulkheadReleaser(_semaphore);
                return BulkheadAcquireResult.Acquired(releaser, GetMetrics());
            }
            finally
            {
                // If we were queued, decrement the queue count
                if (currentCount == 0)
                {
                    Interlocked.Decrement(ref _currentQueuedCount);
                }
            }
        }

        public BulkheadMetrics GetMetrics()
        {
            var available = _semaphore.CurrentCount;
            var currentConcurrency = _maxConcurrency - available;

            return new BulkheadMetrics(
                CurrentConcurrency: currentConcurrency,
                CurrentQueuedCount: Volatile.Read(ref _currentQueuedCount),
                MaxConcurrency: _maxConcurrency,
                MaxQueuedActions: _maxQueuedActions,
                TotalAcquired: Volatile.Read(ref _totalAcquired),
                TotalRejected: Volatile.Read(ref _totalRejected));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _semaphore.Dispose();
        }
    }

    private sealed class BulkheadReleaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private int _released;

        public BulkheadReleaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            // Ensure we only release once
            if (Interlocked.CompareExchange(ref _released, 1, 0) == 0)
            {
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // Semaphore was disposed, ignore
                }
                catch (SemaphoreFullException)
                {
                    // Already at max count, ignore
                }
            }
        }
    }
}
