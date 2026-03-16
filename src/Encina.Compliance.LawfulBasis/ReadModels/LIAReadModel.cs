using Encina.Compliance.GDPR;
using Encina.Marten.Projections;

namespace Encina.Compliance.LawfulBasis.ReadModels;

/// <summary>
/// Query-optimized projected view of a Legitimate Interest Assessment (LIA), built from
/// <see cref="Aggregates.LIAAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the LIA aggregate event stream by
/// <see cref="LIAProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Article 5(2) accountability requirements.
/// </para>
/// <para>
/// The read model includes all EDPB three-part test fields (Purpose Test, Necessity Test,
/// Balancing Test) to support audit reporting and governance dashboards without requiring
/// access to the event stream.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// </remarks>
public sealed class LIAReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this LIA (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Document reference identifier (e.g., "LIA-2024-FRAUD-001").
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for this LIA.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The processing purpose this LIA covers.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    // --- Purpose Test ---

    /// <summary>
    /// Description of the legitimate interest being pursued.
    /// </summary>
    public string LegitimateInterest { get; set; } = string.Empty;

    /// <summary>
    /// Benefits of the processing to the controller, data subject, or third parties.
    /// </summary>
    public string Benefits { get; set; } = string.Empty;

    /// <summary>
    /// Consequences of not carrying out the processing.
    /// </summary>
    public string ConsequencesIfNotProcessed { get; set; } = string.Empty;

    // --- Necessity Test ---

    /// <summary>
    /// Justification for why the processing is necessary for the legitimate interest.
    /// </summary>
    public string NecessityJustification { get; set; } = string.Empty;

    /// <summary>
    /// Alternative approaches considered before choosing this processing.
    /// </summary>
    public IReadOnlyList<string> AlternativesConsidered { get; set; } = [];

    /// <summary>
    /// Notes on data minimisation measures applied to the processing.
    /// </summary>
    public string DataMinimisationNotes { get; set; } = string.Empty;

    // --- Balancing Test ---

    /// <summary>
    /// Description of the nature of the personal data being processed.
    /// </summary>
    public string NatureOfData { get; set; } = string.Empty;

    /// <summary>
    /// Assessment of the data subject's reasonable expectations regarding the processing.
    /// </summary>
    public string ReasonableExpectations { get; set; } = string.Empty;

    /// <summary>
    /// Assessment of the impact on data subjects' rights and freedoms.
    /// </summary>
    public string ImpactAssessment { get; set; } = string.Empty;

    /// <summary>
    /// Safeguards implemented to mitigate the impact on data subjects.
    /// </summary>
    public IReadOnlyList<string> Safeguards { get; set; } = [];

    // --- Governance ---

    /// <summary>
    /// Name or role of the person who conducted the assessment.
    /// </summary>
    public string AssessedBy { get; set; } = string.Empty;

    /// <summary>
    /// Whether the DPO was involved in or consulted during the assessment.
    /// </summary>
    public bool DPOInvolvement { get; set; }

    /// <summary>
    /// Timestamp when the assessment was conducted (UTC).
    /// </summary>
    public DateTimeOffset AssessedAtUtc { get; set; }

    /// <summary>
    /// Any conditions attached to the assessment.
    /// </summary>
    public string? Conditions { get; set; }

    // --- Outcome ---

    /// <summary>
    /// The current outcome of the LIA assessment.
    /// </summary>
    public LIAOutcome Outcome { get; set; }

    /// <summary>
    /// Summary conclusion of the assessment.
    /// </summary>
    public string? Conclusion { get; set; }

    /// <summary>
    /// Timestamp when the next periodic review is due (UTC).
    /// </summary>
    public DateTimeOffset? NextReviewAtUtc { get; set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this LIA (UTC).
    /// </summary>
    /// <remarks>
    /// Updated on every event applied to the aggregate.
    /// Enables efficient change detection and cache invalidation.
    /// </remarks>
    public DateTimeOffset LastModifiedAtUtc { get; set; }

    /// <summary>
    /// Event stream version for optimistic concurrency.
    /// </summary>
    /// <remarks>
    /// Incremented on every event. Matches the aggregate's <see cref="DomainModeling.AggregateBase.Version"/>.
    /// </remarks>
    public int Version { get; set; }
}
