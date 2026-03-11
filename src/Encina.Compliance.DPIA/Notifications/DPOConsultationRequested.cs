namespace Encina.Compliance.DPIA;

/// <summary>
/// Notification published when a DPO consultation has been requested for a DPIA assessment.
/// </summary>
/// <remarks>
/// <para>
/// Published after <see cref="IDPIAAssessmentEngine.RequestDPOConsultationAsync"/> creates
/// a new <see cref="Model.DPOConsultation"/> record in <see cref="Model.DPOConsultationDecision.Pending"/>
/// status.
/// </para>
/// <para>
/// Per GDPR Article 35(2), "the controller shall seek the advice of the data protection
/// officer, where designated, when carrying out a data protection impact assessment."
/// This notification enables integration with DPO workflow systems.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DPOConsultationRequested&gt;</c>
/// can use this to:
/// </para>
/// <list type="bullet">
/// <item><description>Send email notifications to the DPO.</description></item>
/// <item><description>Create tasks in workflow management systems.</description></item>
/// <item><description>Update compliance dashboards with pending consultations.</description></item>
/// <item><description>Start SLA tracking for consultation response times.</description></item>
/// </list>
/// </remarks>
/// <param name="AssessmentId">The unique identifier of the assessment requiring consultation.</param>
/// <param name="ConsultationId">The unique identifier of the newly created consultation record.</param>
/// <param name="DPOEmail">The email address of the DPO to be consulted.</param>
public sealed record DPOConsultationRequested(
    Guid AssessmentId,
    Guid ConsultationId,
    string DPOEmail) : INotification;
