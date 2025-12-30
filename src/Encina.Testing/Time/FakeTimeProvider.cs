namespace Encina.Testing.Time;

/// <summary>
/// A controllable <see cref="TimeProvider"/> implementation for testing time-dependent code.
/// </summary>
/// <remarks>
/// <para>
/// FakeTimeProvider allows tests to control time progression, enabling deterministic testing
/// of scheduling, timeouts, expiration, and other time-dependent logic.
/// </para>
/// <para>
/// All operations are thread-safe for parallel test execution.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
///
/// // Test that scheduled message is not due yet
/// var scheduledAt = timeProvider.GetUtcNow().AddHours(2);
/// scheduledAt.Should().BeAfter(timeProvider.GetUtcNow());
///
/// // Advance time
/// timeProvider.Advance(TimeSpan.FromHours(3));
///
/// // Now the scheduled time has passed
/// scheduledAt.Should().BeBefore(timeProvider.GetUtcNow());
/// </code>
/// </example>
public sealed class FakeTimeProvider : TimeProvider
{
    private readonly object _lock = new();
    private DateTimeOffset _utcNow;
    private readonly List<FakeTimer> _timers = [];
    private readonly TimeZoneInfo _localTimeZone;

    /// <summary>
    /// Creates a new <see cref="FakeTimeProvider"/> starting at the current UTC time.
    /// </summary>
    public FakeTimeProvider() : this(DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Creates a new <see cref="FakeTimeProvider"/> starting at the specified time.
    /// </summary>
    /// <param name="startTime">The initial time.</param>
    public FakeTimeProvider(DateTimeOffset startTime)
    {
        _utcNow = startTime;
        _localTimeZone = TimeZoneInfo.Utc;
    }

    /// <summary>
    /// Creates a new <see cref="FakeTimeProvider"/> with the specified start time and time zone.
    /// </summary>
    /// <param name="startTime">The initial time.</param>
    /// <param name="localTimeZone">The local time zone to use.</param>
    public FakeTimeProvider(DateTimeOffset startTime, TimeZoneInfo localTimeZone)
    {
        _utcNow = startTime;
        _localTimeZone = localTimeZone;
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        lock (_lock)
        {
            return _utcNow;
        }
    }

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone => _localTimeZone;

    /// <summary>
    /// Sets the current UTC time to the specified value.
    /// </summary>
    /// <param name="utcNow">The new UTC time. Must be greater than or equal to the current time.</param>
    /// <exception cref="ArgumentException">Thrown when attempting to move time backwards.</exception>
    public void SetUtcNow(DateTimeOffset utcNow)
    {
        lock (_lock)
        {
            if (utcNow < _utcNow)
            {
                throw new ArgumentException(
                    $"Cannot move time backwards. Current: {_utcNow:O}, Requested: {utcNow:O}",
                    nameof(utcNow));
            }

            _utcNow = utcNow;
            ProcessTimers();
        }
    }

    /// <summary>
    /// Advances the current time by the specified duration.
    /// </summary>
    /// <param name="duration">The duration to advance. Must be non-negative.</param>
    /// <exception cref="ArgumentException">Thrown when duration is negative.</exception>
    public void Advance(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentException("Duration must be non-negative.", nameof(duration));
        }

        lock (_lock)
        {
            _utcNow = _utcNow.Add(duration);
            ProcessTimers();
        }
    }

    /// <summary>
    /// Advances the current time by 24 hours.
    /// </summary>
    public void AdvanceToNextDay() => Advance(TimeSpan.FromDays(1));

    /// <summary>
    /// Advances the current time by 1 hour.
    /// </summary>
    public void AdvanceToNextHour() => Advance(TimeSpan.FromHours(1));

    /// <summary>
    /// Advances the current time by the specified number of minutes.
    /// </summary>
    /// <param name="minutes">The number of minutes to advance. Must be non-negative.</param>
    public void AdvanceMinutes(int minutes) => Advance(TimeSpan.FromMinutes(minutes));

    /// <summary>
    /// Advances the current time by the specified number of seconds.
    /// </summary>
    /// <param name="seconds">The number of seconds to advance. Must be non-negative.</param>
    public void AdvanceSeconds(int seconds) => Advance(TimeSpan.FromSeconds(seconds));

    /// <summary>
    /// Advances the current time by the specified number of milliseconds.
    /// </summary>
    /// <param name="milliseconds">The number of milliseconds to advance. Must be non-negative.</param>
    public void AdvanceMilliseconds(int milliseconds) => Advance(TimeSpan.FromMilliseconds(milliseconds));

    /// <inheritdoc />
    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);

        lock (_lock)
        {
            var timer = new FakeTimer(this, callback, state, dueTime, period, _utcNow);
            _timers.Add(timer);
            return timer;
        }
    }

    /// <summary>
    /// Gets the number of active timers.
    /// </summary>
    public int ActiveTimerCount
    {
        get
        {
            lock (_lock)
            {
                return _timers.Count(t => t.IsActive);
            }
        }
    }

    /// <summary>
    /// Creates a scope that freezes time. When the scope is disposed, time is restored.
    /// </summary>
    /// <returns>A disposable scope that restores time when disposed.</returns>
    /// <example>
    /// <code>
    /// var startTime = timeProvider.GetUtcNow();
    ///
    /// using (timeProvider.Freeze())
    /// {
    ///     timeProvider.Advance(TimeSpan.FromHours(5));
    ///     // Time is now 5 hours ahead
    /// }
    ///
    /// // Time is restored to startTime
    /// timeProvider.GetUtcNow().Should().Be(startTime);
    /// </code>
    /// </example>
    public IDisposable Freeze()
    {
        lock (_lock)
        {
            return new FrozenTimeScope(this, _utcNow);
        }
    }

    /// <summary>
    /// Resets the time provider to the specified time and clears all timers.
    /// </summary>
    /// <param name="time">The time to reset to.</param>
    public void Reset(DateTimeOffset time)
    {
        lock (_lock)
        {
            _utcNow = time;
            // Take a copy to avoid collection modification during enumeration
            var timersToDispose = _timers.ToList();
            _timers.Clear();
            foreach (var timer in timersToDispose)
            {
                timer.DisposeWithoutRemoval();
            }
        }
    }

    /// <summary>
    /// Resets the time provider to the current real UTC time and clears all timers.
    /// </summary>
    public void Reset() => Reset(DateTimeOffset.UtcNow);

    private void ProcessTimers()
    {
        // Process all due timers
        foreach (var timer in _timers.Where(t => t.IsActive).ToList())
        {
            while (timer.IsDue(_utcNow))
            {
                timer.Fire();
            }
        }
    }

    internal void RemoveTimer(FakeTimer timer)
    {
        lock (_lock)
        {
            _timers.Remove(timer);
        }
    }

    private sealed class FrozenTimeScope : IDisposable
    {
        private readonly FakeTimeProvider _provider;
        private readonly DateTimeOffset _frozenTime;
        private bool _disposed;

        public FrozenTimeScope(FakeTimeProvider provider, DateTimeOffset frozenTime)
        {
            _provider = provider;
            _frozenTime = frozenTime;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_provider._lock)
                {
                    _provider._utcNow = _frozenTime;
                }
                _disposed = true;
            }
        }
    }
}
