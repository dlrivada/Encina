using Encina.Compliance.DPIA.Model;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.

namespace Encina.Compliance.DPIA.Events;

/// <summary>
/// Raised when a new Data Protection Impact Assessment is initiated for a processing operation.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 35(1), the controller must carry out a DPIA "where a type of processing [...]
/// is likely to result in a high risk to the rights and freedoms of natural persons." This event
/// records the initiation of that assessment, capturing the processing context and reason.
/// </para>
/// <para>
/// This is the first event in a DPIA aggregate stream. It establishes the assessment scope
/// (request type, processing type) and cross-cutting context (tenant, module).
/// </para>
/// </remarks>
/// <param name="AssessmentId">Unique identifier for the DPIA assessment.</param>
/// <param name="RequestTypeName">Fully-qualified type name of the request this assessment covers.</param>
/// <param name="ProcessingType">Type of processing covered (e.g., "AutomatedDecisionMaking"), or <c>null</c> if unspecified.</param>
/// <param name="Reason">Justification for conducting this assessment, or <c>null</c> if not provided.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the assessment was created.</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record DPIACreated(
    Guid AssessmentId,
    string RequestTypeName,
    string? ProcessingType,
    string? Reason,
    DateTimeOffset OccurredAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a DPIA assessment is evaluated by the risk assessment engine.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 35(7), a DPIA must contain: (c) an assessment of the risks to the rights
/// and freedoms of data subjects, and (d) the measures envisaged to address the risks.
/// This event captures the outcome of that evaluation, including identified risks,
/// proposed mitigations, and the overall risk determination.
/// </para>
/// <para>
/// Transitions the assessment from <see cref="DPIAAssessmentStatus.Draft"/>
/// to <see cref="DPIAAssessmentStatus.InReview"/>.
/// </para>
/// </remarks>
/// <param name="AssessmentId">The assessment being evaluated.</param>
/// <param name="OverallRisk">The aggregate risk level after considering all identified risks.</param>
/// <param name="IdentifiedRisks">Individual risks identified during the assessment.</param>
/// <param name="ProposedMitigations">Mitigation measures proposed to address identified risks.</param>
/// <param name="RequiresPriorConsultation">Whether prior consultation with the supervisory authority is required (Art. 36).</param>
/// <param name="AssessedAtUtc">UTC timestamp when the risk evaluation was performed.</param>
/// <param name="OccurredAtUtc">UTC timestamp when this event occurred.</param>
public sealed record DPIAEvaluated(
    Guid AssessmentId,
    RiskLevel OverallRisk,
    IReadOnlyList<RiskItem> IdentifiedRisks,
    IReadOnlyList<Mitigation> ProposedMitigations,
    bool RequiresPriorConsultation,
    DateTimeOffset AssessedAtUtc,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a DPO consultation is requested for a DPIA assessment.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 35(2), "the controller shall seek the advice of the data protection officer,
/// where designated, when carrying out a data protection impact assessment." This event records
/// the initiation of that mandatory consultation.
/// </para>
/// <para>
/// The consultation remains in <see cref="DPOConsultationDecision.Pending"/> status until
/// a <see cref="DPIADPOResponded"/> event is recorded.
/// </para>
/// </remarks>
/// <param name="AssessmentId">The assessment requiring DPO consultation.</param>
/// <param name="ConsultationId">Unique identifier for this consultation record.</param>
/// <param name="DPOName">Full name of the Data Protection Officer consulted.</param>
/// <param name="DPOEmail">Email address of the Data Protection Officer.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the consultation was requested.</param>
public sealed record DPIADPOConsultationRequested(
    Guid AssessmentId,
    Guid ConsultationId,
    string DPOName,
    string DPOEmail,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when the Data Protection Officer responds to a consultation request.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 39(1)(c), the DPO's tasks include "to provide advice where requested
/// as regards the data protection impact assessment and monitor its performance pursuant
/// to Article 35." This event records the DPO's formal response.
/// </para>
/// <para>
/// The DPO may approve, reject, or conditionally approve the assessment. Conditional
/// approval includes specific conditions that must be met before processing can proceed.
/// </para>
/// </remarks>
/// <param name="AssessmentId">The assessment that was consulted on.</param>
/// <param name="ConsultationId">The consultation record being responded to.</param>
/// <param name="Decision">The DPO's decision on the assessment.</param>
/// <param name="Comments">Additional comments or observations from the DPO.</param>
/// <param name="Conditions">Conditions to meet for conditional approval, or <c>null</c> if not applicable.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the DPO responded.</param>
public sealed record DPIADPOResponded(
    Guid AssessmentId,
    Guid ConsultationId,
    DPOConsultationDecision Decision,
    string? Comments,
    string? Conditions,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a DPIA assessment is approved and the processing operation may proceed.
/// </summary>
/// <remarks>
/// <para>
/// Approval indicates that the assessment has been reviewed, risks are acceptable or adequately
/// mitigated, and the processing operation may proceed. The assessment remains valid until
/// its <paramref name="NextReviewAtUtc"/> date, after which it transitions to
/// <see cref="DPIAAssessmentStatus.Expired"/> per Article 35(11).
/// </para>
/// </remarks>
/// <param name="AssessmentId">The assessment being approved.</param>
/// <param name="ApprovedBy">Identifier of the person who approved the assessment.</param>
/// <param name="NextReviewAtUtc">Scheduled date for the next periodic review (Art. 35(11)), or <c>null</c> for no scheduled review.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the approval occurred.</param>
public sealed record DPIAApproved(
    Guid AssessmentId,
    string ApprovedBy,
    DateTimeOffset? NextReviewAtUtc,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a DPIA assessment is rejected and the processing operation must not proceed.
/// </summary>
/// <remarks>
/// <para>
/// Rejection indicates that the identified risks are unacceptable and cannot be adequately
/// mitigated. Per Article 36(1), if the controller cannot sufficiently reduce the risk,
/// they must consult the supervisory authority before proceeding.
/// </para>
/// </remarks>
/// <param name="AssessmentId">The assessment being rejected.</param>
/// <param name="RejectedBy">Identifier of the person who rejected the assessment.</param>
/// <param name="Reason">Explanation of why the assessment was rejected.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the rejection occurred.</param>
public sealed record DPIARejected(
    Guid AssessmentId,
    string RejectedBy,
    string Reason,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a DPIA assessment is sent back for revision before it can be approved.
/// </summary>
/// <remarks>
/// <para>
/// Revision is typically requested when the DPO or reviewer identifies gaps in the risk analysis
/// or mitigation measures. The assessment transitions to <see cref="DPIAAssessmentStatus.RequiresRevision"/>,
/// allowing it to be re-evaluated after corrections are made.
/// </para>
/// </remarks>
/// <param name="AssessmentId">The assessment requiring revision.</param>
/// <param name="RequestedBy">Identifier of the person requesting revision.</param>
/// <param name="Reason">Explanation of what revisions are needed.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the revision was requested.</param>
public sealed record DPIARevisionRequested(
    Guid AssessmentId,
    string RequestedBy,
    string Reason,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a previously approved DPIA assessment has exceeded its scheduled review date.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 35(11), "the controller shall carry out a review to assess if processing
/// is performed in accordance with the data protection impact assessment at least when there
/// is a change of the risk represented by processing operations." This event marks the
/// transition to <see cref="DPIAAssessmentStatus.Expired"/>, requiring re-evaluation before
/// the processing operation can continue.
/// </para>
/// </remarks>
/// <param name="AssessmentId">The assessment that has expired.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the expiration was recorded.</param>
public sealed record DPIAExpired(
    Guid AssessmentId,
    DateTimeOffset OccurredAtUtc) : INotification;
