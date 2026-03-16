using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis.Events;
using Encina.Marten.Projections;

namespace Encina.Compliance.LawfulBasis.ReadModels;

/// <summary>
/// Marten inline projection that transforms LIA aggregate events into
/// <see cref="LIAReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for Legitimate Interest Assessment
/// management. It handles all 4 LIA event types, creating or updating the
/// <see cref="LIAReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="LIACreated"/> — Creates a new read model with all EDPB three-part test fields (first event in stream)</description></item>
///   <item><description><see cref="LIAApproved"/> — Updates outcome to <see cref="LIAOutcome.Approved"/> with conclusion</description></item>
///   <item><description><see cref="LIARejected"/> — Updates outcome to <see cref="LIAOutcome.Rejected"/> with conclusion</description></item>
///   <item><description><see cref="LIAReviewScheduled"/> — Records the next periodic review date</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class LIAProjection :
    IProjection<LIAReadModel>,
    IProjectionCreator<LIACreated, LIAReadModel>,
    IProjectionHandler<LIAApproved, LIAReadModel>,
    IProjectionHandler<LIARejected, LIAReadModel>,
    IProjectionHandler<LIAReviewScheduled, LIAReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "LIAProjection";

    /// <summary>
    /// Creates a new <see cref="LIAReadModel"/> from a <see cref="LIACreated"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a LIA aggregate stream. It initializes all EDPB three-part
    /// test fields (Purpose Test, Necessity Test, Balancing Test) and governance metadata.
    /// The initial outcome is <see cref="LIAOutcome.RequiresReview"/>.
    /// </remarks>
    /// <param name="domainEvent">The LIA created event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="LIAReadModel"/> in <see cref="LIAOutcome.RequiresReview"/> state.</returns>
    public LIAReadModel Create(LIACreated domainEvent, ProjectionContext context)
    {
        return new LIAReadModel
        {
            Id = domainEvent.LIAId,
            Reference = domainEvent.Reference,
            Name = domainEvent.Name,
            Purpose = domainEvent.Purpose,
            // Purpose Test
            LegitimateInterest = domainEvent.LegitimateInterest,
            Benefits = domainEvent.Benefits,
            ConsequencesIfNotProcessed = domainEvent.ConsequencesIfNotProcessed,
            // Necessity Test
            NecessityJustification = domainEvent.NecessityJustification,
            AlternativesConsidered = domainEvent.AlternativesConsidered,
            DataMinimisationNotes = domainEvent.DataMinimisationNotes,
            // Balancing Test
            NatureOfData = domainEvent.NatureOfData,
            ReasonableExpectations = domainEvent.ReasonableExpectations,
            ImpactAssessment = domainEvent.ImpactAssessment,
            Safeguards = domainEvent.Safeguards,
            // Governance
            AssessedBy = domainEvent.AssessedBy,
            DPOInvolvement = domainEvent.DPOInvolvement,
            AssessedAtUtc = domainEvent.AssessedAtUtc,
            Conditions = domainEvent.Conditions,
            // Outcome
            Outcome = LIAOutcome.RequiresReview,
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            LastModifiedAtUtc = domainEvent.AssessedAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when the LIA is approved.
    /// </summary>
    /// <remarks>
    /// Sets outcome to <see cref="LIAOutcome.Approved"/> and records the conclusion.
    /// An approved LIA can be referenced by lawful basis registrations claiming
    /// <see cref="GDPR.LawfulBasis.LegitimateInterests"/>.
    /// </remarks>
    /// <param name="domainEvent">The LIA approved event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public LIAReadModel Apply(LIAApproved domainEvent, LIAReadModel current, ProjectionContext context)
    {
        current.Outcome = LIAOutcome.Approved;
        current.Conclusion = domainEvent.Conclusion;
        current.LastModifiedAtUtc = domainEvent.ApprovedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the LIA is rejected.
    /// </summary>
    /// <remarks>
    /// Sets outcome to <see cref="LIAOutcome.Rejected"/> and records the conclusion.
    /// A rejected LIA cannot be used as a reference in lawful basis registrations.
    /// </remarks>
    /// <param name="domainEvent">The LIA rejected event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public LIAReadModel Apply(LIARejected domainEvent, LIAReadModel current, ProjectionContext context)
    {
        current.Outcome = LIAOutcome.Rejected;
        current.Conclusion = domainEvent.Conclusion;
        current.LastModifiedAtUtc = domainEvent.RejectedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a periodic review is scheduled.
    /// </summary>
    /// <remarks>
    /// Records the next review date for governance tracking. Only applicable to
    /// approved LIAs — the aggregate enforces this invariant.
    /// </remarks>
    /// <param name="domainEvent">The LIA review scheduled event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public LIAReadModel Apply(LIAReviewScheduled domainEvent, LIAReadModel current, ProjectionContext context)
    {
        current.NextReviewAtUtc = domainEvent.NextReviewAtUtc;
        current.LastModifiedAtUtc = domainEvent.ScheduledAtUtc;
        current.Version++;
        return current;
    }
}
