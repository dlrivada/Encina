namespace Encina.Testing.Time;

/// <summary>
/// A controllable timer implementation for testing time-dependent code.
/// </summary>
/// <remarks>
/// <para>
/// FakeTimer works with <see cref="FakeTimeProvider"/> to provide deterministic timer behavior.
/// Timers only fire when time is advanced via the associated FakeTimeProvider.
/// </para>
/// </remarks>
internal sealed class FakeTimer : ITimer
{
    private readonly FakeTimeProvider _provider;
    private readonly TimerCallback _callback;
    private readonly object? _state;
    private readonly object _lock = new();

    private DateTimeOffset _dueTime;
    private TimeSpan _period;
    private bool _isActive;
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="FakeTimer"/>.
    /// </summary>
    /// <param name="provider">The time provider that owns this timer.</param>
    /// <param name="callback">The callback to invoke when the timer fires.</param>
    /// <param name="state">State to pass to the callback.</param>
    /// <param name="dueTime">The time until the first invocation.</param>
    /// <param name="period">The interval between subsequent invocations.</param>
    /// <param name="currentTime">The current time from the provider.</param>
    internal FakeTimer(
        FakeTimeProvider provider,
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period,
        DateTimeOffset currentTime)
    {
        _provider = provider;
        _callback = callback;
        _state = state;
        _period = period;

        if (dueTime == Timeout.InfiniteTimeSpan)
        {
            _isActive = false;
            _dueTime = DateTimeOffset.MaxValue;
        }
        else
        {
            _isActive = true;
            _dueTime = currentTime.Add(dueTime);
        }
    }

    /// <summary>
    /// Gets whether the timer is currently active.
    /// </summary>
    internal bool IsActive
    {
        get
        {
            lock (_lock)
            {
                return _isActive && !_disposed;
            }
        }
    }

    /// <summary>
    /// Determines if the timer is due to fire at the specified time.
    /// </summary>
    /// <param name="currentTime">The current time to check against.</param>
    /// <returns>True if the timer should fire.</returns>
    internal bool IsDue(DateTimeOffset currentTime)
    {
        lock (_lock)
        {
            return _isActive && !_disposed && currentTime >= _dueTime;
        }
    }

    /// <summary>
    /// Fires the timer callback and schedules the next invocation if periodic.
    /// </summary>
    internal void Fire()
    {
        lock (_lock)
        {
            if (_disposed || !_isActive)
            {
                return;
            }

            // Invoke the callback
            _callback(_state);

            // Schedule next invocation if periodic
            if (_period > TimeSpan.Zero && _period != Timeout.InfiniteTimeSpan)
            {
                _dueTime = _dueTime.Add(_period);
            }
            else
            {
                // One-shot timer
                _isActive = false;
            }
        }
    }

    /// <inheritdoc />
    public bool Change(TimeSpan dueTime, TimeSpan period)
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return false;
            }

            _period = period;

            if (dueTime == Timeout.InfiniteTimeSpan)
            {
                _isActive = false;
                _dueTime = DateTimeOffset.MaxValue;
            }
            else
            {
                _isActive = true;
                _dueTime = _provider.GetUtcNow().Add(dueTime);
            }

            return true;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Disposes the timer and removes it from the provider.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _disposed = true;
                _isActive = false;
                _provider.RemoveTimer(this);
            }
        }
    }

    /// <summary>
    /// Disposes the timer without removing it from the provider's collection.
    /// Used when the provider is clearing all timers to avoid collection modification during enumeration.
    /// </summary>
    internal void DisposeWithoutRemoval()
    {
        lock (_lock)
        {
            _disposed = true;
            _isActive = false;
        }
    }
}
