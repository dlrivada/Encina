namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Notification published when a breach is approaching its 72-hour notification deadline.
/// </summary>
/// <remarks>
/// <para>
/// Published by the <c>BreachDeadlineMonitorService</c> at configurable alert thresholds
/// (default: 48, 24, 12, 6, and 1 hours remaining) to warn stakeholders that the
/// supervisory authority notification deadline is approaching.
/// </para>
/// <para>
/// Per GDPR Article 33(1), the controller must notify the supervisory authority
/// "without undue delay and, where feasible, not later than 72 hours after having
/// become aware of it." Deadline warnings provide proactive alerting to help
/// ensure this obligation is met.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DeadlineWarningNotification&gt;</c>
/// can use this to escalate to management, send urgent alerts, or trigger
/// automated notification workflows.
/// </para>
/// </remarks>
/// <param name="BreachId">Identifier of the breach approaching its deadline.</param>
/// <param name="RemainingHours">Number of hours remaining until the deadline.</param>
/// <param name="DeadlineUtc">The notification deadline timestamp (UTC).</param>
public sealed record DeadlineWarningNotification(
    string BreachId,
    double RemainingHours,
    DateTimeOffset DeadlineUtc) : INotification;
