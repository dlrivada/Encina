namespace Encina.Compliance.DPIA;

/// <summary>
/// Notification published when a DPIA assessment has expired and requires re-evaluation.
/// </summary>
/// <remarks>
/// <para>
/// Published by the <c>DPIAExpirationMonitorService</c> when it detects that an assessment's
/// <see cref="Model.DPIAAssessment.NextReviewAtUtc"/> date has passed. This triggers the
/// transition to <see cref="Model.DPIAAssessmentStatus.Expired"/> status.
/// </para>
/// <para>
/// Per GDPR Article 35(11), "the controller shall carry out a review to assess if processing
/// is performed in accordance with the data protection impact assessment at least when there
/// is a change of the risk represented by processing operations." Expiration-based review
/// provides a safety net ensuring assessments are periodically re-evaluated even when no
/// explicit risk change has been identified.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DPIAAssessmentExpired&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Alert compliance teams about assessments needing re-evaluation.</description></item>
/// <item><description>Trigger automated re-assessment workflows.</description></item>
/// <item><description>Update compliance dashboards and risk registers.</description></item>
/// <item><description>Log audit entries per the accountability principle (Article 5(2)).</description></item>
/// </list>
/// </remarks>
/// <param name="AssessmentId">The unique identifier of the expired assessment.</param>
/// <param name="RequestTypeName">The fully-qualified type name of the request covered by the expired assessment.</param>
/// <param name="ExpiredAtUtc">The UTC timestamp when the assessment expired (its <see cref="Model.DPIAAssessment.NextReviewAtUtc"/> value).</param>
public sealed record DPIAAssessmentExpired(
    Guid AssessmentId,
    string RequestTypeName,
    DateTimeOffset ExpiredAtUtc) : INotification;
