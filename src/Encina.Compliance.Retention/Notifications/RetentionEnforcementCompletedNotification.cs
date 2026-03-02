using Encina.Compliance.Retention.Model;

namespace Encina.Compliance.Retention;

/// <summary>
/// Notification published when a retention enforcement cycle has completed.
/// </summary>
/// <remarks>
/// <para>
/// Published at the end of each enforcement execution, summarizing the outcomes:
/// how many records were deleted, retained, failed, or held by legal holds.
/// Per GDPR Article 5(2) (accountability principle), controllers must demonstrate
/// compliance with data protection principles including storage limitation.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;RetentionEnforcementCompletedNotification&gt;</c>
/// can log enforcement summaries, update compliance dashboards, send reports
/// to data protection officers, or trigger follow-up actions for failed deletions.
/// </para>
/// </remarks>
/// <param name="Result">Summary result of the enforcement execution, including per-record details.</param>
/// <param name="OccurredAtUtc">Timestamp when the enforcement cycle completed (UTC).</param>
public sealed record RetentionEnforcementCompletedNotification(
    DeletionResult Result,
    DateTimeOffset OccurredAtUtc) : INotification;
