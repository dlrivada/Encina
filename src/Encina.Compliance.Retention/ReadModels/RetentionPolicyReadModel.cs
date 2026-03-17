using Encina.Compliance.Retention.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.Retention.ReadModels;

/// <summary>
/// Query-optimized projected view of a retention policy, built from <see cref="Aggregates.RetentionPolicyAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the retention policy aggregate event stream by
/// <see cref="RetentionPolicyProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Article 5(2) accountability requirements.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// <para>
/// Tracks the full policy lifecycle (Active → Deactivated), including retention period,
/// auto-deletion behavior, and legal basis per GDPR Article 5(1)(e) storage limitation.
/// </para>
/// </remarks>
public sealed class RetentionPolicyReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this retention policy (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The data category this policy applies to (e.g., "customer-data", "financial-records").
    /// </summary>
    public string DataCategory { get; set; } = string.Empty;

    /// <summary>
    /// How long data in this category should be retained before becoming eligible for deletion.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; }

    /// <summary>
    /// Whether the enforcement service should automatically delete expired data.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, expiration alerts are raised but deletion must be
    /// performed manually.
    /// </remarks>
    public bool AutoDelete { get; set; }

    /// <summary>
    /// The trigger mechanism for the retention period.
    /// </summary>
    public RetentionPolicyType PolicyType { get; set; }

    /// <summary>
    /// Optional reason or justification for this retention period.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Optional legal basis for the retention requirement (e.g., "Tax Code §147").
    /// </summary>
    public string? LegalBasis { get; set; }

    /// <summary>
    /// Whether this policy is currently active and accepting new retention records.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The reason this policy was deactivated, or <c>null</c> if still active.
    /// </summary>
    public string? DeactivationReason { get; set; }

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
    /// Updated on every event applied to the retention policy aggregate.
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
