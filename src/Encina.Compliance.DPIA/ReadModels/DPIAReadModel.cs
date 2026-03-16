using Encina.Compliance.DPIA.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.DPIA.ReadModels;

/// <summary>
/// Query-optimized projected view of a DPIA assessment, built from <see cref="Aggregates.DPIAAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the DPIA aggregate event stream by
/// <see cref="DPIAProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Article 5(2) accountability requirements.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection and cache invalidation.
/// </para>
/// <para>
/// Replaces the old entity-based <see cref="DPIAAssessment"/> for query purposes
/// in the event-sourced model.
/// </para>
/// </remarks>
public sealed class DPIAReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this assessment (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The fully-qualified type name of the request this assessment covers.
    /// </summary>
    /// <remarks>
    /// Used as the lookup key by the pipeline behavior.
    /// </remarks>
    public string RequestTypeName { get; set; } = string.Empty;

    /// <summary>
    /// The type of processing covered by this assessment (e.g., "AutomatedDecisionMaking").
    /// </summary>
    public string? ProcessingType { get; set; }

    /// <summary>
    /// The reason or justification for conducting this assessment.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// The current lifecycle status of this assessment.
    /// </summary>
    public DPIAAssessmentStatus Status { get; set; }

    /// <summary>
    /// The overall risk level from the most recent evaluation.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when the assessment has not yet been evaluated.
    /// </remarks>
    public RiskLevel? OverallRisk { get; set; }

    /// <summary>
    /// Individual risks identified during the most recent evaluation.
    /// </summary>
    public IReadOnlyList<RiskItem> IdentifiedRisks { get; set; } = [];

    /// <summary>
    /// Mitigation measures proposed during the most recent evaluation.
    /// </summary>
    public IReadOnlyList<Mitigation> ProposedMitigations { get; set; } = [];

    /// <summary>
    /// Whether prior consultation with the supervisory authority is required (Art. 36).
    /// </summary>
    public bool RequiresPriorConsultation { get; set; }

    /// <summary>
    /// The UTC timestamp when the risk evaluation was performed.
    /// </summary>
    public DateTimeOffset? AssessedAtUtc { get; set; }

    /// <summary>
    /// The DPO consultation record, or <c>null</c> if not yet initiated.
    /// </summary>
    public DPOConsultation? DPOConsultation { get; set; }

    /// <summary>
    /// The UTC timestamp when this assessment was approved, or <c>null</c> if not yet approved.
    /// </summary>
    public DateTimeOffset? ApprovedAtUtc { get; set; }

    /// <summary>
    /// The UTC timestamp for the next scheduled review (Art. 35(11)).
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
    /// Timestamp of the last modification to this assessment (UTC).
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
}
