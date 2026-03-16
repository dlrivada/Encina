using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis.Events;
using Encina.DomainModeling;

namespace Encina.Compliance.LawfulBasis.Aggregates;

/// <summary>
/// Event-sourced aggregate representing the lifecycle of a Legitimate Interest Assessment (LIA)
/// following the EDPB three-part test under GDPR Article 6(1)(f).
/// </summary>
/// <remarks>
/// <para>
/// A LIA documents the assessment of whether a controller's legitimate interest is overridden
/// by the data subject's fundamental rights and freedoms. The aggregate captures the full
/// EDPB three-part test: Purpose Test, Necessity Test, and Balancing Test.
/// </para>
/// <para>
/// The lifecycle progresses through: <see cref="LIAOutcome.RequiresReview"/> (created) →
/// <see cref="LIAOutcome.Approved"/> or <see cref="LIAOutcome.Rejected"/>. Once approved,
/// a periodic review may be scheduled via <see cref="ScheduleReview"/>.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Art. 5(2) accountability requirements.
/// </para>
/// </remarks>
public sealed class LIAAggregate : AggregateBase
{
    /// <summary>
    /// Document reference identifier (e.g., "LIA-2024-FRAUD-001").
    /// </summary>
    public string Reference { get; private set; } = string.Empty;

    /// <summary>
    /// Human-readable name for this LIA.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The processing purpose this LIA covers.
    /// </summary>
    public string Purpose { get; private set; } = string.Empty;

    // --- Purpose Test ---

    /// <summary>
    /// Description of the legitimate interest being pursued.
    /// </summary>
    public string LegitimateInterest { get; private set; } = string.Empty;

    /// <summary>
    /// Benefits of the processing to the controller, data subject, or third parties.
    /// </summary>
    public string Benefits { get; private set; } = string.Empty;

    /// <summary>
    /// Consequences of not carrying out the processing.
    /// </summary>
    public string ConsequencesIfNotProcessed { get; private set; } = string.Empty;

    // --- Necessity Test ---

    /// <summary>
    /// Justification for why the processing is necessary for the legitimate interest.
    /// </summary>
    public string NecessityJustification { get; private set; } = string.Empty;

    /// <summary>
    /// Alternative approaches considered before choosing this processing.
    /// </summary>
    public IReadOnlyList<string> AlternativesConsidered { get; private set; } = [];

    /// <summary>
    /// Notes on data minimisation measures applied to the processing.
    /// </summary>
    public string DataMinimisationNotes { get; private set; } = string.Empty;

    // --- Balancing Test ---

    /// <summary>
    /// Description of the nature of the personal data being processed.
    /// </summary>
    public string NatureOfData { get; private set; } = string.Empty;

    /// <summary>
    /// Assessment of the data subject's reasonable expectations regarding the processing.
    /// </summary>
    public string ReasonableExpectations { get; private set; } = string.Empty;

    /// <summary>
    /// Assessment of the impact on data subjects' rights and freedoms.
    /// </summary>
    public string ImpactAssessment { get; private set; } = string.Empty;

    /// <summary>
    /// Safeguards implemented to mitigate the impact on data subjects.
    /// </summary>
    public IReadOnlyList<string> Safeguards { get; private set; } = [];

    // --- Governance ---

    /// <summary>
    /// Name or role of the person who conducted the assessment.
    /// </summary>
    public string AssessedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the DPO was involved in or consulted during the assessment.
    /// </summary>
    public bool DPOInvolvement { get; private set; }

    /// <summary>
    /// Any conditions attached to the assessment.
    /// </summary>
    public string? Conditions { get; private set; }

    // --- Outcome ---

    /// <summary>
    /// The current outcome of the LIA assessment.
    /// </summary>
    public LIAOutcome Outcome { get; private set; }

    /// <summary>
    /// Summary conclusion of the assessment.
    /// </summary>
    public string? Conclusion { get; private set; }

    /// <summary>
    /// Timestamp when the next periodic review is due (UTC).
    /// </summary>
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
    /// Creates a new Legitimate Interest Assessment following the EDPB three-part test.
    /// </summary>
    /// <param name="liaId">Unique identifier for the new LIA.</param>
    /// <param name="reference">Document reference identifier (e.g., "LIA-2024-FRAUD-001").</param>
    /// <param name="name">Human-readable name for this LIA.</param>
    /// <param name="purpose">The processing purpose this LIA covers.</param>
    /// <param name="legitimateInterest">Description of the legitimate interest (Purpose Test).</param>
    /// <param name="benefits">Benefits of the processing (Purpose Test).</param>
    /// <param name="consequencesIfNotProcessed">Consequences of not processing (Purpose Test).</param>
    /// <param name="necessityJustification">Justification for necessity (Necessity Test).</param>
    /// <param name="alternativesConsidered">Alternatives evaluated (Necessity Test).</param>
    /// <param name="dataMinimisationNotes">Data minimisation measures (Necessity Test).</param>
    /// <param name="natureOfData">Nature of the personal data (Balancing Test).</param>
    /// <param name="reasonableExpectations">Data subject's reasonable expectations (Balancing Test).</param>
    /// <param name="impactAssessment">Impact on data subjects' rights (Balancing Test).</param>
    /// <param name="safeguards">Safeguards to mitigate impact (Balancing Test).</param>
    /// <param name="assessedBy">Name or role of the assessor.</param>
    /// <param name="dpoInvolvement">Whether the DPO was consulted.</param>
    /// <param name="assessedAtUtc">Timestamp of assessment (UTC).</param>
    /// <param name="conditions">Any conditions attached, or <c>null</c>.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="LIAAggregate"/> with <see cref="LIAOutcome.RequiresReview"/> outcome.</returns>
    public static LIAAggregate Create(
        Guid liaId,
        string reference,
        string name,
        string purpose,
        string legitimateInterest,
        string benefits,
        string consequencesIfNotProcessed,
        string necessityJustification,
        IReadOnlyList<string> alternativesConsidered,
        string dataMinimisationNotes,
        string natureOfData,
        string reasonableExpectations,
        string impactAssessment,
        IReadOnlyList<string> safeguards,
        string assessedBy,
        bool dpoInvolvement,
        DateTimeOffset assessedAtUtc,
        string? conditions = null,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(legitimateInterest);
        ArgumentException.ThrowIfNullOrWhiteSpace(benefits);
        ArgumentException.ThrowIfNullOrWhiteSpace(consequencesIfNotProcessed);
        ArgumentException.ThrowIfNullOrWhiteSpace(necessityJustification);
        ArgumentNullException.ThrowIfNull(alternativesConsidered);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataMinimisationNotes);
        ArgumentException.ThrowIfNullOrWhiteSpace(natureOfData);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonableExpectations);
        ArgumentException.ThrowIfNullOrWhiteSpace(impactAssessment);
        ArgumentNullException.ThrowIfNull(safeguards);
        ArgumentException.ThrowIfNullOrWhiteSpace(assessedBy);

        var aggregate = new LIAAggregate();
        aggregate.RaiseEvent(new LIACreated(
            liaId,
            reference,
            name,
            purpose,
            legitimateInterest,
            benefits,
            consequencesIfNotProcessed,
            necessityJustification,
            alternativesConsidered,
            dataMinimisationNotes,
            natureOfData,
            reasonableExpectations,
            impactAssessment,
            safeguards,
            assessedBy,
            dpoInvolvement,
            assessedAtUtc,
            conditions,
            tenantId,
            moduleId));
        return aggregate;
    }

    /// <summary>
    /// Approves this LIA, confirming that the legitimate interest outweighs
    /// the data subject's rights and freedoms.
    /// </summary>
    /// <param name="conclusion">Summary conclusion of the assessment outcome.</param>
    /// <param name="approvedBy">Identifier of the person approving the LIA.</param>
    /// <param name="approvedAtUtc">Timestamp of approval (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the LIA is not in <see cref="LIAOutcome.RequiresReview"/> state.</exception>
    public void Approve(string conclusion, string approvedBy, DateTimeOffset approvedAtUtc)
    {
        if (Outcome != LIAOutcome.RequiresReview)
        {
            throw new InvalidOperationException(
                $"Cannot approve a LIA when it is in '{Outcome}' state. Approval is only allowed from RequiresReview state.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(conclusion);
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);

        RaiseEvent(new LIAApproved(Id, conclusion, approvedBy, approvedAtUtc, TenantId, ModuleId));
    }

    /// <summary>
    /// Rejects this LIA, indicating that the data subject's rights and freedoms
    /// override the legitimate interest.
    /// </summary>
    /// <param name="conclusion">Summary conclusion explaining the rejection.</param>
    /// <param name="rejectedBy">Identifier of the person rejecting the LIA.</param>
    /// <param name="rejectedAtUtc">Timestamp of rejection (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the LIA is not in <see cref="LIAOutcome.RequiresReview"/> state.</exception>
    public void Reject(string conclusion, string rejectedBy, DateTimeOffset rejectedAtUtc)
    {
        if (Outcome != LIAOutcome.RequiresReview)
        {
            throw new InvalidOperationException(
                $"Cannot reject a LIA when it is in '{Outcome}' state. Rejection is only allowed from RequiresReview state.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(conclusion);
        ArgumentException.ThrowIfNullOrWhiteSpace(rejectedBy);

        RaiseEvent(new LIARejected(Id, conclusion, rejectedBy, rejectedAtUtc, TenantId, ModuleId));
    }

    /// <summary>
    /// Schedules a periodic review for this approved LIA.
    /// </summary>
    /// <remarks>
    /// LIAs should be reviewed periodically to ensure the assessment remains valid
    /// as circumstances change.
    /// </remarks>
    /// <param name="nextReviewAtUtc">Timestamp when the next review is due (UTC).</param>
    /// <param name="scheduledBy">Identifier of the person scheduling the review.</param>
    /// <param name="scheduledAtUtc">Timestamp when the review was scheduled (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the LIA is not in <see cref="LIAOutcome.Approved"/> state.</exception>
    public void ScheduleReview(DateTimeOffset nextReviewAtUtc, string scheduledBy, DateTimeOffset scheduledAtUtc)
    {
        if (Outcome != LIAOutcome.Approved)
        {
            throw new InvalidOperationException(
                $"Cannot schedule a review when the LIA is in '{Outcome}' state. Reviews can only be scheduled for approved LIAs.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(scheduledBy);

        RaiseEvent(new LIAReviewScheduled(Id, nextReviewAtUtc, scheduledBy, scheduledAtUtc, TenantId, ModuleId));
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case LIACreated e:
                Id = e.LIAId;
                Reference = e.Reference;
                Name = e.Name;
                Purpose = e.Purpose;
                LegitimateInterest = e.LegitimateInterest;
                Benefits = e.Benefits;
                ConsequencesIfNotProcessed = e.ConsequencesIfNotProcessed;
                NecessityJustification = e.NecessityJustification;
                AlternativesConsidered = e.AlternativesConsidered;
                DataMinimisationNotes = e.DataMinimisationNotes;
                NatureOfData = e.NatureOfData;
                ReasonableExpectations = e.ReasonableExpectations;
                ImpactAssessment = e.ImpactAssessment;
                Safeguards = e.Safeguards;
                AssessedBy = e.AssessedBy;
                DPOInvolvement = e.DPOInvolvement;
                Conditions = e.Conditions;
                Outcome = LIAOutcome.RequiresReview;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                break;

            case LIAApproved e:
                Outcome = LIAOutcome.Approved;
                Conclusion = e.Conclusion;
                break;

            case LIARejected e:
                Outcome = LIAOutcome.Rejected;
                Conclusion = e.Conclusion;
                break;

            case LIAReviewScheduled e:
                NextReviewAtUtc = e.NextReviewAtUtc;
                break;
        }
    }
}
