using System.Collections.Concurrent;

namespace Encina.Polly;

/// <summary>
/// Adaptive rate limiter that adjusts capacity based on error rates.
/// </summary>
/// <remarks>
/// <para>
/// Implements a sliding window rate limiter with three states:
/// </para>
/// <list type="bullet">
/// <item><description><b>Normal</b>: Full capacity available.</description></item>
/// <item><description><b>Throttled</b>: Reduced capacity (10%) due to high error rate.</description></item>
/// <item><description><b>Recovering</b>: Gradually increasing capacity after successful cooldown.</description></item>
/// </list>
/// <para>
/// Thread-safe implementation using concurrent collections.
/// </para>
/// </remarks>
public sealed class AdaptiveRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets = new();
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptiveRateLimiter"/> class.
    /// </summary>
    public AdaptiveRateLimiter()
        : this(TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptiveRateLimiter"/> class with a custom time provider.
    /// </summary>
    /// <param name="timeProvider">Time provider for testability.</param>
    public AdaptiveRateLimiter(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc/>
    public ValueTask<RateLimitResult> AcquireAsync(
        string key,
        RateLimitAttribute config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(config);

        var bucket = _buckets.GetOrAdd(key, _ => new RateLimitBucket(config, _timeProvider));
        var result = bucket.TryAcquire();

        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public void RecordSuccess(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_buckets.TryGetValue(key, out var bucket))
        {
            bucket.RecordSuccess();
        }
    }

    /// <inheritdoc/>
    public void RecordFailure(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_buckets.TryGetValue(key, out var bucket))
        {
            bucket.RecordFailure();
        }
    }

    /// <inheritdoc/>
    public RateLimitState? GetState(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _buckets.TryGetValue(key, out var bucket) ? bucket.State : null;
    }

    /// <inheritdoc/>
    public void Reset(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        _buckets.TryRemove(key, out _);
    }

    /// <summary>
    /// Internal bucket that tracks rate limit state for a single key.
    /// </summary>
    private sealed class RateLimitBucket
    {
        private readonly RateLimitAttribute _config;
        private readonly TimeProvider _timeProvider;
        private readonly object _lock = new();

        private readonly Queue<DateTimeOffset> _requestTimestamps = new();
        private int _successCount;
        private int _failureCount;
        private DateTimeOffset _windowStart;
        private DateTimeOffset _lastStateChange;
        private double _currentCapacityFactor = 1.0;

        public RateLimitState State { get; private set; } = RateLimitState.Normal;

        public RateLimitBucket(RateLimitAttribute config, TimeProvider timeProvider)
        {
            _config = config;
            _timeProvider = timeProvider;
            _windowStart = timeProvider.GetUtcNow();
            _lastStateChange = _windowStart;
        }

        public RateLimitResult TryAcquire()
        {
            lock (_lock)
            {
                var now = _timeProvider.GetUtcNow();
                CleanupExpiredTimestamps(now);
                UpdateState(now);

                var currentLimit = GetCurrentLimit();
                var currentCount = _requestTimestamps.Count;
                var errorRate = CalculateErrorRate();

                if (currentCount >= currentLimit)
                {
                    // Calculate retry-after based on oldest request expiration
                    var retryAfter = _requestTimestamps.Count > 0
                        ? _requestTimestamps.Peek().AddSeconds(_config.WindowSizeSeconds) - now
                        : TimeSpan.FromSeconds(1);

                    if (retryAfter < TimeSpan.Zero)
                    {
                        retryAfter = TimeSpan.FromSeconds(1);
                    }

                    return RateLimitResult.Denied(
                        State,
                        retryAfter,
                        currentCount,
                        currentLimit,
                        errorRate);
                }

                // Allow the request
                _requestTimestamps.Enqueue(now);

                return RateLimitResult.Allowed(
                    State,
                    currentCount + 1,
                    currentLimit,
                    errorRate);
            }
        }

        public void RecordSuccess()
        {
            lock (_lock)
            {
                _successCount++;
            }
        }

        public void RecordFailure()
        {
            lock (_lock)
            {
                _failureCount++;
            }
        }

        private void CleanupExpiredTimestamps(DateTimeOffset now)
        {
            var windowStart = now.AddSeconds(-_config.WindowSizeSeconds);

            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < windowStart)
            {
                _requestTimestamps.Dequeue();
            }

            // Reset counters if window has fully expired
            if (_windowStart < windowStart)
            {
                _windowStart = windowStart;
                _successCount = 0;
                _failureCount = 0;
            }
        }

        private void UpdateState(DateTimeOffset now)
        {
            if (!_config.EnableAdaptiveThrottling)
            {
                return;
            }

            var totalRequests = _successCount + _failureCount;
            var errorRate = CalculateErrorRate();
            var timeSinceStateChange = now - _lastStateChange;

            switch (State)
            {
                case RateLimitState.Normal:
                    // Check if we should transition to Throttled
                    if (totalRequests >= _config.MinimumThroughputForThrottling &&
                        errorRate >= _config.ErrorThresholdPercent)
                    {
                        State = RateLimitState.Throttled;
                        _lastStateChange = now;
                        _currentCapacityFactor = 0.1; // Reduce to 10%
                    }
                    break;

                case RateLimitState.Throttled:
                    // Check if cooldown has elapsed
                    if (timeSinceStateChange >= TimeSpan.FromSeconds(_config.CooldownSeconds))
                    {
                        State = RateLimitState.Recovering;
                        _lastStateChange = now;
                        // Keep current capacity, will ramp up
                    }
                    break;

                case RateLimitState.Recovering:
                    // Check if we should return to Normal or back to Throttled
                    if (totalRequests >= _config.MinimumThroughputForThrottling &&
                        errorRate >= _config.ErrorThresholdPercent)
                    {
                        // Errors again, back to Throttled
                        State = RateLimitState.Throttled;
                        _lastStateChange = now;
                        _currentCapacityFactor = 0.1;
                    }
                    else if (timeSinceStateChange >= TimeSpan.FromSeconds(_config.WindowSizeSeconds))
                    {
                        // Successful window, ramp up capacity
                        _currentCapacityFactor = Math.Min(1.0, _currentCapacityFactor * _config.RampUpFactor);
                        _lastStateChange = now;

                        if (_currentCapacityFactor >= 1.0)
                        {
                            State = RateLimitState.Normal;
                            _currentCapacityFactor = 1.0;
                        }
                    }
                    break;
            }
        }

        private int GetCurrentLimit()
        {
            var limit = (int)Math.Ceiling(_config.MaxRequestsPerWindow * _currentCapacityFactor);
            return Math.Max(1, limit); // Always allow at least 1 request
        }

        private double CalculateErrorRate()
        {
            var total = _successCount + _failureCount;
            if (total == 0)
            {
                return 0.0;
            }

            return (_failureCount * 100.0) / total;
        }
    }
}
