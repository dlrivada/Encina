using Encina.Compliance.Retention.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.Retention.ReadModels;

/// <summary>
/// Query-optimized projected view of a retention record, built from <see cref="Aggregates.RetentionRecordAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the retention record aggregate event stream by
/// <see cref="RetentionRecordProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Article 5(2) accountability requirements.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// <para>
/// Tracks the full retention record lifecycle: Active → Expired → Deleted (terminal),
/// with an optional UnderLegalHold state that suspends deletion per GDPR Article 17(3)(e).
/// Anonymization is an alternative terminal path per GDPR Recital 26.
/// </para>
/// </remarks>
public sealed class RetentionRecordReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this retention record (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The identifier of the data entity being tracked (e.g., customer ID, order ID).
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// The data category this entity belongs to (matches a retention policy).
    /// </summary>
    public string DataCategory { get; set; } = string.Empty;

    /// <summary>
    /// The retention policy aggregate ID governing this entity's lifecycle.
    /// </summary>
    public Guid PolicyId { get; set; }

    /// <summary>
    /// The retention period copied from the policy at tracking time.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; }

    /// <summary>
    /// Current lifecycle status of this retention record.
    /// </summary>
    public RetentionStatus Status { get; set; }

    /// <summary>
    /// The calculated expiration timestamp (UTC) — when the entity becomes eligible for deletion.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// The identifier of the legal hold currently applied, or <c>null</c> if not under hold.
    /// </summary>
    /// <remarks>
    /// When multiple holds exist for the same entity, this tracks the most recently applied hold.
    /// The full hold history is available via the event stream.
    /// </remarks>
    public Guid? LegalHoldId { get; set; }

    /// <summary>
    /// The UTC timestamp when the data was physically deleted, or <c>null</c> if not yet deleted.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>
    /// The UTC timestamp when the data was anonymized, or <c>null</c> if not anonymized.
    /// </summary>
    /// <remarks>
    /// Per GDPR Recital 26, anonymized data falls outside GDPR scope.
    /// </remarks>
    public DateTimeOffset? AnonymizedAtUtc { get; set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// The UTC timestamp when this record was first tracked.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this retention record (UTC).
    /// </summary>
    /// <remarks>
    /// Updated on every event applied to the retention record aggregate.
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
    /// Determines whether this retention record has expired based on the current time.
    /// </summary>
    /// <remarks>
    /// A record is considered expired when the current time is past <see cref="ExpiresAtUtc"/>
    /// and the record has not been deleted or anonymized.
    /// </remarks>
    /// <param name="nowUtc">The current UTC time for comparison against <see cref="ExpiresAtUtc"/>.</param>
    /// <returns><see langword="true"/> if the record has expired; otherwise, <see langword="false"/>.</returns>
    public bool IsExpired(DateTimeOffset nowUtc) =>
        nowUtc >= ExpiresAtUtc &&
        Status is not (RetentionStatus.Deleted);
}
