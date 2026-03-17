using Encina.Marten.Projections;

namespace Encina.Compliance.Retention.ReadModels;

/// <summary>
/// Query-optimized projected view of a legal hold, built from <see cref="Aggregates.LegalHoldAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the legal hold aggregate event stream by
/// <see cref="LegalHoldProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Article 5(2) accountability — who placed the hold, when, and why.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// <para>
/// Tracks the full legal hold lifecycle (Active → Lifted) per GDPR Article 17(3)(e):
/// processing is necessary "for the establishment, exercise or defence of legal claims."
/// </para>
/// </remarks>
public sealed class LegalHoldReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this legal hold (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The identifier of the data entity this hold applies to.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// The legal reason for the hold (e.g., "Ongoing litigation - Case #12345").
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the user who placed the hold (typically legal counsel).
    /// </summary>
    public string AppliedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this hold is currently active and preventing deletion.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Identifier of the user who lifted the hold, or <c>null</c> if still active.
    /// </summary>
    public string? ReleasedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the hold was placed (UTC).
    /// </summary>
    public DateTimeOffset AppliedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when the hold was lifted (UTC), or <c>null</c> if still active.
    /// </summary>
    public DateTimeOffset? ReleasedAtUtc { get; set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this legal hold record (UTC).
    /// </summary>
    /// <remarks>
    /// Updated on every event applied to the legal hold aggregate.
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
