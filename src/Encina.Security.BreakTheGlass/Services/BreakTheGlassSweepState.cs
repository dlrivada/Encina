namespace Encina.Security.BreakTheGlass;

/// <summary>
/// What the expiry sweeper tells the health check: when it started and when a cycle last completed.
/// </summary>
/// <remarks>
/// A singleton written by <see cref="BreakTheGlassExpiryService"/> and read by
/// <see cref="Health.BreakTheGlassHealthCheck"/>. A cycle is "completed" only when every row of the
/// batch was closed, audited and marked; a cycle that failed leaves the previous completion time, so
/// the health check turns Degraded once three intervals pass without a completed cycle.
/// </remarks>
internal sealed class BreakTheGlassSweepState
{
    private long _startedAtTicks;
    private long _lastCompletedAtTicks;

    /// <summary>The instant the sweeper started; <see langword="null"/> while it has not run (not applicable).</summary>
    public DateTimeOffset? StartedAtUtc => Read(ref _startedAtTicks);

    /// <summary>The instant the last cycle completed; <see langword="null"/> when none has.</summary>
    public DateTimeOffset? LastCompletedAtUtc => Read(ref _lastCompletedAtTicks);

    /// <summary>Records that the sweeper started.</summary>
    public void RecordStarted(DateTimeOffset nowUtc) => Volatile.Write(ref _startedAtTicks, nowUtc.UtcTicks);

    /// <summary>Records that a cycle completed with every row handled.</summary>
    public void RecordCompleted(DateTimeOffset nowUtc) => Volatile.Write(ref _lastCompletedAtTicks, nowUtc.UtcTicks);

    private static DateTimeOffset? Read(ref long ticks)
    {
        var value = Volatile.Read(ref ticks);
        return value == 0 ? null : new DateTimeOffset(value, TimeSpan.Zero);
    }
}
