using Encina.Compliance.DPIA.Model;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Notification published when a DPIA assessment has been completed with a risk evaluation result.
/// </summary>
/// <remarks>
/// <para>
/// Published after the <see cref="IDPIAAssessmentEngine"/> completes a full risk assessment
/// and produces a <see cref="DPIAResult"/>. This marks the transition from the evaluation
/// phase to the consultation/approval phase of the DPIA lifecycle.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DPIAAssessmentCompleted&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Trigger DPO consultation workflows when high risk is identified.</description></item>
/// <item><description>Send internal notifications to compliance teams.</description></item>
/// <item><description>Update compliance dashboards and reporting systems.</description></item>
/// <item><description>Initiate prior consultation with the supervisory authority when <see cref="RequiresPriorConsultation"/> is <see langword="true"/> (Article 36).</description></item>
/// </list>
/// </remarks>
/// <param name="AssessmentId">The unique identifier of the completed assessment.</param>
/// <param name="RequestTypeName">The fully-qualified type name of the request that was assessed.</param>
/// <param name="OverallRisk">The overall risk level determined by the assessment.</param>
/// <param name="RequiresPriorConsultation">
/// Whether prior consultation with the supervisory authority is required per Article 36(1),
/// because residual risk remains high even after proposed mitigations.
/// </param>
public sealed record DPIAAssessmentCompleted(
    Guid AssessmentId,
    string RequestTypeName,
    RiskLevel OverallRisk,
    bool RequiresPriorConsultation) : INotification;
