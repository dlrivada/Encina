using Encina.Marten.Projections;

namespace Encina.Compliance.Consent.ReadModels;

/// <summary>
/// Query-optimized projected view of a consent record, built from <see cref="Aggregates.ConsentAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the consent aggregate event stream by
/// <see cref="ConsentProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Article 5(2) accountability requirements.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// <para>
/// Used by <c>IConsentService</c> query methods to return consent state to consumers.
/// Replaces the old entity-based <c>ConsentRecord</c> for query purposes in the event-sourced model.
/// </para>
/// </remarks>
public sealed class ConsentReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this consent (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the data subject who gave consent.
    /// </summary>
    /// <remarks>
    /// Stable identifier for the data subject (e.g., user ID, customer number).
    /// Used together with <see cref="Purpose"/> as the natural key for consent lookup.
    /// </remarks>
    public string DataSubjectId { get; set; } = string.Empty;

    /// <summary>
    /// The specific processing purpose for which consent was given.
    /// </summary>
    /// <remarks>
    /// Purposes should be granular and specific as required by GDPR Article 6(1)(a).
    /// </remarks>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Current lifecycle status of this consent.
    /// </summary>
    public ConsentStatus Status { get; set; }

    /// <summary>
    /// Identifier of the consent version the data subject currently agreed to.
    /// </summary>
    /// <remarks>
    /// Updated when the data subject renews consent or provides reconsent under new terms.
    /// </remarks>
    public string ConsentVersionId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when consent was originally granted (UTC).
    /// </summary>
    public DateTimeOffset GivenAtUtc { get; set; }

    /// <summary>
    /// Timestamp when the data subject withdrew consent (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if consent has not been withdrawn.
    /// </remarks>
    public DateTimeOffset? WithdrawnAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this consent expires (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if no expiration is set. Updated on renewal or reconsent.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    /// <summary>
    /// The source or channel through which consent was most recently collected.
    /// </summary>
    /// <example>"web-form", "api", "mobile-app", "in-person", "email"</example>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The IP address of the data subject at the time consent was most recently given.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when consent is collected through channels where IP address is not available.
    /// </remarks>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Hash or reference to the consent form shown to the data subject.
    /// </summary>
    /// <remarks>
    /// Proof of what the data subject was presented with, as required by GDPR Article 7(1).
    /// </remarks>
    public string? ProofOfConsent { get; set; }

    /// <summary>
    /// Additional metadata associated with this consent record.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this consent (UTC).
    /// </summary>
    /// <remarks>
    /// Updated on every event applied to the consent aggregate.
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
