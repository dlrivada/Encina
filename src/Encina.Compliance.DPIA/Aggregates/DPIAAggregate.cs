using Encina.Compliance.DPIA.Events;
using Encina.Compliance.DPIA.Model;
using Encina.DomainModeling;

namespace Encina.Compliance.DPIA.Aggregates;

/// <summary>
/// Event-sourced aggregate representing a Data Protection Impact Assessment lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 35, the controller must carry out a DPIA when processing is likely
/// to result in a high risk to the rights and freedoms of natural persons. This aggregate
/// captures the full assessment lifecycle as an immutable event stream, providing the
/// accountability required by Article 5(2).
/// </para>
/// <para>
/// The assessment lifecycle follows these status transitions:
/// <list type="bullet">
///   <item><description><b>Happy path</b>: Draft → InReview → Approved → Expired</description></item>
///   <item><description><b>Rejection</b>: Draft → InReview → Rejected</description></item>
///   <item><description><b>Revision</b>: Draft → InReview → RequiresRevision → (re-evaluate) → InReview</description></item>
/// </list>
/// </para>
/// <para>
/// All state changes are captured as immutable events. The event stream replaces the
/// separate <c>IDPIAAuditStore</c> — domain events ARE the audit trail.
/// </para>
/// </remarks>
public sealed class DPIAAggregate : AggregateBase
{
    /// <summary>
    /// The fully-qualified type name of the request this assessment covers.
    /// </summary>
    /// <remarks>
    /// Used as the lookup key by the pipeline behavior. Stored as a string
    /// to support persistence and cross-assembly resolution.
    /// </remarks>
    public string RequestTypeName { get; private set; } = string.Empty;

    /// <summary>
    /// The type of processing covered by this assessment (e.g., "AutomatedDecisionMaking").
    /// </summary>
    public string? ProcessingType { get; private set; }

    /// <summary>
    /// The reason or justification for conducting this assessment.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// The current lifecycle status of this assessment.
    /// </summary>
    public DPIAAssessmentStatus Status { get; private set; }

    /// <summary>
    /// The assessment result containing risk analysis and mitigations.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when the assessment is in <see cref="DPIAAssessmentStatus.Draft"/>
    /// status and has not yet been evaluated.
    /// </remarks>
    public DPIAResult? Result { get; private set; }

    /// <summary>
    /// The DPO consultation record for this assessment.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when DPO consultation has not yet been initiated.
    /// Per Article 35(2), the controller must seek the DPO's advice when carrying out a DPIA.
    /// </remarks>
    public DPOConsultation? DPOConsultation { get; private set; }

    /// <summary>
    /// The UTC timestamp when this assessment was approved, or <see langword="null"/> if not yet approved.
    /// </summary>
    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    /// <summary>
    /// The UTC timestamp for the next scheduled review of this assessment.
    /// </summary>
    /// <remarks>
    /// Per Article 35(11), the controller must carry out a review "at least when there is
    /// a change of the risk represented by processing operations." When this date passes,
    /// the assessment transitions to <see cref="DPIAAssessmentStatus.Expired"/>.
    /// </remarks>
    public DateTimeOffset? NextReviewAtUtc { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// Creates a new DPIA assessment in <see cref="DPIAAssessmentStatus.Draft"/> status.
    /// </summary>
    /// <param name="id">Unique identifier for the assessment.</param>
    /// <param name="requestTypeName">Fully-qualified type name of the request this assessment covers.</param>
    /// <param name="occurredAtUtc">UTC timestamp when the assessment was created.</param>
    /// <param name="processingType">Type of processing covered, or <c>null</c> if unspecified.</param>
    /// <param name="reason">Justification for conducting this assessment, or <c>null</c>.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="DPIAAggregate"/> in Draft status.</returns>
    public static DPIAAggregate Create(
        Guid id,
        string requestTypeName,
        DateTimeOffset occurredAtUtc,
        string? processingType = null,
        string? reason = null,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeName);

        var aggregate = new DPIAAggregate();
        aggregate.RaiseEvent(new DPIACreated(
            id, requestTypeName, processingType, reason, occurredAtUtc, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Records the risk evaluation result for this assessment.
    /// </summary>
    /// <param name="result">The risk evaluation result from the assessment engine.</param>
    /// <param name="occurredAtUtc">UTC timestamp when the evaluation occurred.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the assessment is not in <see cref="DPIAAssessmentStatus.Draft"/>
    /// or <see cref="DPIAAssessmentStatus.RequiresRevision"/> status.
    /// </exception>
    public void Evaluate(DPIAResult result, DateTimeOffset occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (Status != DPIAAssessmentStatus.Draft && Status != DPIAAssessmentStatus.RequiresRevision)
        {
            throw new InvalidOperationException(
                $"Cannot evaluate an assessment in '{Status}' status. Assessment must be in 'Draft' or 'RequiresRevision' status.");
        }

        RaiseEvent(new DPIAEvaluated(
            Id,
            result.OverallRisk,
            result.IdentifiedRisks,
            result.ProposedMitigations,
            result.RequiresPriorConsultation,
            result.AssessedAtUtc,
            occurredAtUtc));
    }

    /// <summary>
    /// Initiates a DPO consultation for this assessment.
    /// </summary>
    /// <param name="consultationId">Unique identifier for the consultation record.</param>
    /// <param name="dpoName">Full name of the Data Protection Officer.</param>
    /// <param name="dpoEmail">Email address of the Data Protection Officer.</param>
    /// <param name="occurredAtUtc">UTC timestamp when the consultation was requested.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the assessment is not in <see cref="DPIAAssessmentStatus.InReview"/> status.
    /// </exception>
    public void RequestDPOConsultation(
        Guid consultationId,
        string dpoName,
        string dpoEmail,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dpoName);
        ArgumentException.ThrowIfNullOrWhiteSpace(dpoEmail);

        if (Status != DPIAAssessmentStatus.InReview)
        {
            throw new InvalidOperationException(
                $"Cannot request DPO consultation for an assessment in '{Status}' status. Assessment must be in 'InReview' status.");
        }

        RaiseEvent(new DPIADPOConsultationRequested(
            Id, consultationId, dpoName, dpoEmail, occurredAtUtc));
    }

    /// <summary>
    /// Records the DPO's response to a consultation request.
    /// </summary>
    /// <param name="consultationId">The consultation record being responded to.</param>
    /// <param name="decision">The DPO's decision on the assessment.</param>
    /// <param name="occurredAtUtc">UTC timestamp when the DPO responded.</param>
    /// <param name="comments">Additional comments or observations from the DPO.</param>
    /// <param name="conditions">Conditions for conditional approval, or <c>null</c>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no DPO consultation is pending or the consultation ID does not match.
    /// </exception>
    public void RecordDPOResponse(
        Guid consultationId,
        DPOConsultationDecision decision,
        DateTimeOffset occurredAtUtc,
        string? comments = null,
        string? conditions = null)
    {
        if (DPOConsultation is null || DPOConsultation.Id != consultationId)
        {
            throw new InvalidOperationException(
                $"No pending DPO consultation with ID '{consultationId}' exists for this assessment.");
        }

        if (DPOConsultation.Decision != DPOConsultationDecision.Pending)
        {
            throw new InvalidOperationException(
                "The DPO consultation has already been responded to.");
        }

        RaiseEvent(new DPIADPOResponded(
            Id, consultationId, decision, comments, conditions, occurredAtUtc));
    }

    /// <summary>
    /// Approves the assessment, allowing the processing operation to proceed.
    /// </summary>
    /// <param name="approvedBy">Identifier of the person approving the assessment.</param>
    /// <param name="occurredAtUtc">UTC timestamp when the approval occurred.</param>
    /// <param name="nextReviewAtUtc">Scheduled date for the next periodic review (Art. 35(11)), or <c>null</c>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the assessment is not in <see cref="DPIAAssessmentStatus.InReview"/> status.
    /// </exception>
    public void Approve(
        string approvedBy,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset? nextReviewAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);

        if (Status != DPIAAssessmentStatus.InReview)
        {
            throw new InvalidOperationException(
                $"Cannot approve an assessment in '{Status}' status. Assessment must be in 'InReview' status.");
        }

        RaiseEvent(new DPIAApproved(Id, approvedBy, nextReviewAtUtc, occurredAtUtc));
    }

    /// <summary>
    /// Rejects the assessment; the processing operation must not proceed.
    /// </summary>
    /// <param name="rejectedBy">Identifier of the person rejecting the assessment.</param>
    /// <param name="reason">Explanation of why the assessment was rejected.</param>
    /// <param name="occurredAtUtc">UTC timestamp when the rejection occurred.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the assessment is not in <see cref="DPIAAssessmentStatus.InReview"/> status.
    /// </exception>
    public void Reject(
        string rejectedBy,
        string reason,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rejectedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != DPIAAssessmentStatus.InReview)
        {
            throw new InvalidOperationException(
                $"Cannot reject an assessment in '{Status}' status. Assessment must be in 'InReview' status.");
        }

        RaiseEvent(new DPIARejected(Id, rejectedBy, reason, occurredAtUtc));
    }

    /// <summary>
    /// Sends the assessment back for revision before it can be approved.
    /// </summary>
    /// <param name="requestedBy">Identifier of the person requesting revision.</param>
    /// <param name="reason">Explanation of what revisions are needed.</param>
    /// <param name="occurredAtUtc">UTC timestamp when the revision was requested.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the assessment is not in <see cref="DPIAAssessmentStatus.InReview"/> status.
    /// </exception>
    public void RequestRevision(
        string requestedBy,
        string reason,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != DPIAAssessmentStatus.InReview)
        {
            throw new InvalidOperationException(
                $"Cannot request revision for an assessment in '{Status}' status. Assessment must be in 'InReview' status.");
        }

        RaiseEvent(new DPIARevisionRequested(Id, requestedBy, reason, occurredAtUtc));
    }

    /// <summary>
    /// Expires the assessment when its scheduled review date has passed.
    /// </summary>
    /// <param name="occurredAtUtc">UTC timestamp when the expiration was recorded.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the assessment is not in <see cref="DPIAAssessmentStatus.Approved"/> status.
    /// </exception>
    public void Expire(DateTimeOffset occurredAtUtc)
    {
        if (Status != DPIAAssessmentStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Cannot expire an assessment in '{Status}' status. Assessment must be in 'Approved' status.");
        }

        RaiseEvent(new Events.DPIAExpired(Id, occurredAtUtc));
    }

    /// <summary>
    /// Determines whether this assessment is currently valid for allowing processing to proceed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An assessment is current when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Its <see cref="Status"/> is <see cref="DPIAAssessmentStatus.Approved"/>.</description></item>
    ///   <item><description>Its <see cref="NextReviewAtUtc"/> has not passed (or is <see langword="null"/>).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="nowUtc">The current UTC time for comparison against <see cref="NextReviewAtUtc"/>.</param>
    /// <returns><see langword="true"/> if the assessment is approved and not expired; otherwise, <see langword="false"/>.</returns>
    public bool IsCurrent(DateTimeOffset nowUtc) =>
        Status == DPIAAssessmentStatus.Approved &&
        (NextReviewAtUtc is null || NextReviewAtUtc > nowUtc);

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case DPIACreated e:
                Id = e.AssessmentId;
                RequestTypeName = e.RequestTypeName;
                ProcessingType = e.ProcessingType;
                Reason = e.Reason;
                Status = DPIAAssessmentStatus.Draft;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                break;

            case DPIAEvaluated e:
                Result = new DPIAResult
                {
                    OverallRisk = e.OverallRisk,
                    IdentifiedRisks = e.IdentifiedRisks,
                    ProposedMitigations = e.ProposedMitigations,
                    RequiresPriorConsultation = e.RequiresPriorConsultation,
                    AssessedAtUtc = e.AssessedAtUtc,
                };
                Status = DPIAAssessmentStatus.InReview;
                break;

            case DPIADPOConsultationRequested e:
                DPOConsultation = new DPOConsultation
                {
                    Id = e.ConsultationId,
                    DPOName = e.DPOName,
                    DPOEmail = e.DPOEmail,
                    RequestedAtUtc = e.OccurredAtUtc,
                    Decision = DPOConsultationDecision.Pending,
                };
                break;

            case DPIADPOResponded e:
                if (DPOConsultation is not null)
                {
                    DPOConsultation = DPOConsultation with
                    {
                        Decision = e.Decision,
                        Comments = e.Comments,
                        Conditions = e.Conditions,
                        RespondedAtUtc = e.OccurredAtUtc,
                    };
                }
                break;

            case DPIAApproved e:
                Status = DPIAAssessmentStatus.Approved;
                ApprovedAtUtc = e.OccurredAtUtc;
                NextReviewAtUtc = e.NextReviewAtUtc;
                break;

            case DPIARejected:
                Status = DPIAAssessmentStatus.Rejected;
                break;

            case DPIARevisionRequested:
                Status = DPIAAssessmentStatus.RequiresRevision;
                break;

            case Events.DPIAExpired:
                Status = DPIAAssessmentStatus.Expired;
                break;
        }
    }
}
