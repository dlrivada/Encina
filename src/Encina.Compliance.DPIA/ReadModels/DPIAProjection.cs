using Encina.Compliance.DPIA.Events;
using Encina.Compliance.DPIA.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.DPIA.ReadModels;

/// <summary>
/// Marten inline projection that transforms DPIA aggregate events into <see cref="DPIAReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for DPIA assessment management. It handles all 8
/// DPIA event types, creating or updating the <see cref="DPIAReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="DPIACreated"/> — Creates a new read model (first event in stream)</description></item>
///   <item><description><see cref="DPIAEvaluated"/> — Records risk evaluation; transitions to <see cref="DPIAAssessmentStatus.InReview"/></description></item>
///   <item><description><see cref="DPIADPOConsultationRequested"/> — Records DPO consultation initiation</description></item>
///   <item><description><see cref="DPIADPOResponded"/> — Records DPO decision and comments</description></item>
///   <item><description><see cref="DPIAApproved"/> — Records approval; transitions to <see cref="DPIAAssessmentStatus.Approved"/></description></item>
///   <item><description><see cref="DPIARejected"/> — Records rejection; transitions to <see cref="DPIAAssessmentStatus.Rejected"/></description></item>
///   <item><description><see cref="DPIARevisionRequested"/> — Transitions to <see cref="DPIAAssessmentStatus.RequiresRevision"/></description></item>
///   <item><description><see cref="DPIAExpired"/> — Transitions to <see cref="DPIAAssessmentStatus.Expired"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class DPIAProjection :
    IProjection<DPIAReadModel>,
    IProjectionCreator<DPIACreated, DPIAReadModel>,
    IProjectionHandler<DPIAEvaluated, DPIAReadModel>,
    IProjectionHandler<DPIADPOConsultationRequested, DPIAReadModel>,
    IProjectionHandler<DPIADPOResponded, DPIAReadModel>,
    IProjectionHandler<DPIAApproved, DPIAReadModel>,
    IProjectionHandler<DPIARejected, DPIAReadModel>,
    IProjectionHandler<DPIARevisionRequested, DPIAReadModel>,
    IProjectionHandler<DPIAExpired, DPIAReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "DPIAProjection";

    /// <summary>
    /// Creates a new <see cref="DPIAReadModel"/> from a <see cref="DPIACreated"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a DPIA aggregate stream. It initializes the assessment
    /// in <see cref="DPIAAssessmentStatus.Draft"/> status with the processing context
    /// and cross-cutting identifiers.
    /// </remarks>
    /// <param name="domainEvent">The DPIA created event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="DPIAReadModel"/> in <see cref="DPIAAssessmentStatus.Draft"/> status.</returns>
    public DPIAReadModel Create(DPIACreated domainEvent, ProjectionContext context)
    {
        return new DPIAReadModel
        {
            Id = domainEvent.AssessmentId,
            RequestTypeName = domainEvent.RequestTypeName,
            ProcessingType = domainEvent.ProcessingType,
            Reason = domainEvent.Reason,
            Status = DPIAAssessmentStatus.Draft,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            LastModifiedAtUtc = domainEvent.OccurredAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when the assessment is evaluated by the risk engine.
    /// </summary>
    /// <remarks>
    /// Records the risk evaluation outcome and transitions to <see cref="DPIAAssessmentStatus.InReview"/>.
    /// Per Art. 35(7), the evaluation captures identified risks (c) and proposed mitigations (d).
    /// </remarks>
    /// <param name="domainEvent">The DPIA evaluated event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DPIAReadModel Apply(DPIAEvaluated domainEvent, DPIAReadModel current, ProjectionContext context)
    {
        current.OverallRisk = domainEvent.OverallRisk;
        current.IdentifiedRisks = domainEvent.IdentifiedRisks;
        current.ProposedMitigations = domainEvent.ProposedMitigations;
        current.RequiresPriorConsultation = domainEvent.RequiresPriorConsultation;
        current.AssessedAtUtc = domainEvent.AssessedAtUtc;
        current.Status = DPIAAssessmentStatus.InReview;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a DPO consultation is requested.
    /// </summary>
    /// <remarks>
    /// Per Art. 35(2), the controller must seek DPO advice. This records the consultation
    /// initiation with a <see cref="DPOConsultationDecision.Pending"/> decision.
    /// </remarks>
    /// <param name="domainEvent">The DPO consultation requested event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DPIAReadModel Apply(DPIADPOConsultationRequested domainEvent, DPIAReadModel current, ProjectionContext context)
    {
        current.DPOConsultation = new DPOConsultation
        {
            Id = domainEvent.ConsultationId,
            DPOName = domainEvent.DPOName,
            DPOEmail = domainEvent.DPOEmail,
            RequestedAtUtc = domainEvent.OccurredAtUtc,
            Decision = DPOConsultationDecision.Pending,
        };
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the DPO responds to a consultation.
    /// </summary>
    /// <remarks>
    /// Records the DPO's formal decision, comments, and any conditions for conditional approval.
    /// </remarks>
    /// <param name="domainEvent">The DPO responded event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DPIAReadModel Apply(DPIADPOResponded domainEvent, DPIAReadModel current, ProjectionContext context)
    {
        if (current.DPOConsultation is not null)
        {
            current.DPOConsultation = current.DPOConsultation with
            {
                Decision = domainEvent.Decision,
                Comments = domainEvent.Comments,
                Conditions = domainEvent.Conditions,
                RespondedAtUtc = domainEvent.OccurredAtUtc,
            };
        }
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the assessment is approved.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="DPIAAssessmentStatus.Approved"/> and records the scheduled
    /// review date per Art. 35(11).
    /// </remarks>
    /// <param name="domainEvent">The DPIA approved event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DPIAReadModel Apply(DPIAApproved domainEvent, DPIAReadModel current, ProjectionContext context)
    {
        current.Status = DPIAAssessmentStatus.Approved;
        current.ApprovedAtUtc = domainEvent.OccurredAtUtc;
        current.NextReviewAtUtc = domainEvent.NextReviewAtUtc;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the assessment is rejected.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="DPIAAssessmentStatus.Rejected"/>. The controller must
    /// redesign the processing or consult the supervisory authority under Art. 36.
    /// </remarks>
    /// <param name="domainEvent">The DPIA rejected event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DPIAReadModel Apply(DPIARejected domainEvent, DPIAReadModel current, ProjectionContext context)
    {
        current.Status = DPIAAssessmentStatus.Rejected;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when revision is requested.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="DPIAAssessmentStatus.RequiresRevision"/>, allowing
    /// the assessment to be re-evaluated after corrections.
    /// </remarks>
    /// <param name="domainEvent">The revision requested event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DPIAReadModel Apply(DPIARevisionRequested domainEvent, DPIAReadModel current, ProjectionContext context)
    {
        current.Status = DPIAAssessmentStatus.RequiresRevision;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the assessment expires.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="DPIAAssessmentStatus.Expired"/> per Art. 35(11).
    /// The assessment must be re-evaluated before the processing operation can continue.
    /// </remarks>
    /// <param name="domainEvent">The DPIA expired event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public DPIAReadModel Apply(DPIAExpired domainEvent, DPIAReadModel current, ProjectionContext context)
    {
        current.Status = DPIAAssessmentStatus.Expired;
        current.LastModifiedAtUtc = domainEvent.OccurredAtUtc;
        current.Version++;
        return current;
    }
}
