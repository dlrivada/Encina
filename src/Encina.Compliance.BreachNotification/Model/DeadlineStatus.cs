namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Provides a point-in-time view of a breach's notification deadline status.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 33(1), the controller must notify the supervisory authority
/// "without undue delay and, where feasible, not later than 72 hours after having
/// become aware of it." This type encapsulates the current state of that deadline
/// for a specific breach.
/// </para>
/// <para>
/// The <c>BreachDeadlineMonitorService</c> periodically evaluates all active breaches
/// and produces <see cref="DeadlineStatus"/> snapshots. When a breach approaches its
/// deadline (configurable alert thresholds), a <c>DeadlineWarningNotification</c> is
/// published.
/// </para>
/// <para>
/// Per EDPB Guidelines 9/2022, the 72-hour period begins when the controller has a
/// "reasonable degree of certainty" that a breach has occurred. The <see cref="DetectedAtUtc"/>
/// timestamp should reflect this awareness moment, not the time of the underlying
/// security incident.
/// </para>
/// </remarks>
public sealed record DeadlineStatus
{
    /// <summary>
    /// Identifier of the breach being tracked.
    /// </summary>
    public required string BreachId { get; init; }

    /// <summary>
    /// Timestamp when the breach was detected (UTC).
    /// </summary>
    public required DateTimeOffset DetectedAtUtc { get; init; }

    /// <summary>
    /// Deadline for notifying the supervisory authority (UTC).
    /// </summary>
    /// <remarks>
    /// Calculated as <see cref="DetectedAtUtc"/> + 72 hours per Art. 33(1).
    /// </remarks>
    public required DateTimeOffset DeadlineUtc { get; init; }

    /// <summary>
    /// Number of hours remaining until the notification deadline.
    /// </summary>
    /// <remarks>
    /// Negative values indicate the deadline has passed. The value is calculated
    /// at the time of snapshot creation.
    /// </remarks>
    public required double RemainingHours { get; init; }

    /// <summary>
    /// Whether the notification deadline has passed without authority notification.
    /// </summary>
    public required bool IsOverdue { get; init; }

    /// <summary>
    /// Current lifecycle status of the breach.
    /// </summary>
    public required BreachStatus Status { get; init; }
}
