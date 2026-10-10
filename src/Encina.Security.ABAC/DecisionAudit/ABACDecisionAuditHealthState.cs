namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// The outcome of the most recent decision audit write, kept for <see cref="Health.ABACHealthCheck"/>.
/// </summary>
/// <remarks>
/// <para>
/// The Policy Enforcement Point reports each write it makes: a failed write sets the failure flag and
/// the next successful write clears it. There is no write probe, so the state changes only when the
/// application is audited traffic; a failure the application has stopped producing evidence for stays
/// visible until a write succeeds, and the health check reads it as Degraded once it is older than
/// <see cref="ABACDecisionAuditOptions.HealthFailureWindow"/>.
/// </para>
/// <para>
/// The state holds an instant only: no subject, tenant, resource or error text. It is thread-safe and
/// registered as a singleton by <c>AddEncinaABAC</c>.
/// </para>
/// </remarks>
public sealed class ABACDecisionAuditHealthState
{
    private readonly TimeProvider _timeProvider;

    // UTC ticks of the last unresolved failure; 0 means none. A real instant is never 0 ticks.
    private long _lastFailureTicks;

    /// <summary>
    /// Initializes a new instance of the <see cref="ABACDecisionAuditHealthState"/> class.
    /// </summary>
    /// <param name="timeProvider">The clock that stamps a failure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <c>null</c>.</exception>
    public ABACDecisionAuditHealthState(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the instant of the last decision audit write that failed and has not been followed by a
    /// successful write, or <c>null</c> when the last write succeeded (or none was made).
    /// </summary>
    public DateTimeOffset? LastFailureAtUtc
    {
        get
        {
            var ticks = Interlocked.Read(ref _lastFailureTicks);
            return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }

    /// <summary>
    /// Records that a decision audit write succeeded, which clears the failure flag.
    /// </summary>
    public void RecordWriteSucceeded() => Interlocked.Exchange(ref _lastFailureTicks, 0);

    /// <summary>
    /// Records that a decision audit write failed (a store error, an exception, a timeout or a record
    /// that could not be built).
    /// </summary>
    public void RecordWriteFailed() =>
        Interlocked.Exchange(ref _lastFailureTicks, _timeProvider.GetUtcNow().UtcTicks);
}
