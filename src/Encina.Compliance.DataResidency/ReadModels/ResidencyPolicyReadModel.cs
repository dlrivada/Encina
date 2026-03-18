using Encina.Compliance.DataResidency.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.DataResidency.ReadModels;

/// <summary>
/// Query-optimized projected view of a residency policy, built from
/// <see cref="Aggregates.ResidencyPolicyAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the residency policy aggregate event stream by
/// <see cref="ResidencyPolicyProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Article 5(2) accountability requirements.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// <para>
/// Tracks the full policy lifecycle (Active → Deleted), including allowed regions,
/// adequacy decision requirements, and transfer legal bases per GDPR Chapter V
/// (Articles 44–49) international data transfer requirements.
/// </para>
/// </remarks>
public sealed class ResidencyPolicyReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this residency policy (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The data category this policy applies to (e.g., "personal-data", "healthcare-data").
    /// </summary>
    public string DataCategory { get; set; } = string.Empty;

    /// <summary>
    /// Region codes where data of this category is allowed to be stored.
    /// </summary>
    /// <remarks>
    /// An empty list means no geographic restrictions are applied. Region codes are
    /// case-insensitive identifiers (ISO 3166-1 alpha-2, regional, or custom).
    /// </remarks>
    public IReadOnlyList<string> AllowedRegionCodes { get; set; } = [];

    /// <summary>
    /// Whether the region must have an EU adequacy decision under GDPR Article 45.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, data can only be stored in regions with an adequacy decision
    /// from the European Commission. This is the strictest transfer mechanism and eliminates
    /// the need for supplementary measures.
    /// </remarks>
    public bool RequireAdequacyDecision { get; set; }

    /// <summary>
    /// Legal bases acceptable for cross-border transfers involving this data category.
    /// </summary>
    /// <remarks>
    /// Defines which GDPR Chapter V mechanisms are permitted: adequacy decisions (Art. 45),
    /// Standard Contractual Clauses (Art. 46(2)(c)), Binding Corporate Rules (Art. 47),
    /// explicit consent (Art. 49(1)(a)), or other derogations (Art. 49).
    /// </remarks>
    public IReadOnlyList<TransferLegalBasis> AllowedTransferBases { get; set; } = [];

    /// <summary>
    /// Whether this policy is currently active and enforcing data residency rules.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The reason this policy was deleted, or <c>null</c> if still active.
    /// </summary>
    public string? DeletionReason { get; set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// The UTC timestamp when this policy was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this policy record (UTC).
    /// </summary>
    /// <remarks>
    /// Updated on every event applied to the residency policy aggregate.
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
