using Encina.Marten.Projections;

namespace Encina.Compliance.LawfulBasis.ReadModels;

/// <summary>
/// Query-optimized projected view of a lawful basis registration, built from
/// <see cref="Aggregates.LawfulBasisAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the lawful basis aggregate event stream by
/// <see cref="LawfulBasisProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Article 5(2) accountability requirements.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// <para>
/// Used by query methods to return lawful basis state to consumers.
/// Replaces the old entity-based <c>LawfulBasisRegistration</c> for query purposes
/// in the event-sourced model.
/// </para>
/// </remarks>
public sealed class LawfulBasisReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this registration (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The assembly-qualified name of the request type this registration applies to.
    /// </summary>
    public string RequestTypeName { get; set; } = string.Empty;

    /// <summary>
    /// The current lawful basis for processing under Article 6(1).
    /// </summary>
    public GDPR.LawfulBasis Basis { get; set; }

    /// <summary>
    /// The purpose of the processing.
    /// </summary>
    public string? Purpose { get; set; }

    /// <summary>
    /// Reference to a Legitimate Interest Assessment, when basis is
    /// <see cref="GDPR.LawfulBasis.LegitimateInterests"/>.
    /// </summary>
    public string? LIAReference { get; set; }

    /// <summary>
    /// Reference to the specific legal provision, when basis is
    /// <see cref="GDPR.LawfulBasis.LegalObligation"/>.
    /// </summary>
    public string? LegalReference { get; set; }

    /// <summary>
    /// Reference to the contract or pre-contractual steps, when basis is
    /// <see cref="GDPR.LawfulBasis.Contract"/>.
    /// </summary>
    public string? ContractReference { get; set; }

    /// <summary>
    /// Whether this registration has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Reason for revocation, if applicable.
    /// </summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    /// Timestamp when the registration was originally created (UTC).
    /// </summary>
    public DateTimeOffset RegisteredAtUtc { get; set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this registration (UTC).
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
