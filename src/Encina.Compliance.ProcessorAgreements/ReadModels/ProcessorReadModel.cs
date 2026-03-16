using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.ProcessorAgreements.ReadModels;

/// <summary>
/// Query-optimized projected view of a data processor, built from <see cref="Aggregates.ProcessorAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the processor aggregate event stream by
/// <see cref="ProcessorProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Art. 5(2) accountability requirements.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// <para>
/// Tracks processor identity, hierarchical position (via <see cref="ParentProcessorId"/>
/// and <see cref="Depth"/>), and sub-processor authorization type per GDPR Article 28(2).
/// </para>
/// </remarks>
public sealed class ProcessorReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this processor (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The display name of the processor (e.g., "Stripe", "AWS").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The country where the processor is established (ISO 3166-1).
    /// </summary>
    /// <remarks>
    /// Relevant for determining whether Standard Contractual Clauses are required
    /// for cross-border data transfers per Articles 44-49.
    /// </remarks>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// The contact email for the processor's data protection representative,
    /// or <c>null</c> if not provided.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// The identifier of the parent processor, or <c>null</c> for top-level processors.
    /// </summary>
    /// <remarks>
    /// Per Article 28(2), the processor shall not engage another processor without prior
    /// written authorization. This field tracks the hierarchical relationship.
    /// </remarks>
    public Guid? ParentProcessorId { get; set; }

    /// <summary>
    /// The depth of this processor in the sub-processor hierarchy.
    /// </summary>
    /// <remarks>
    /// <c>0</c> = top-level processor, <c>1</c> = direct sub-processor,
    /// <c>2</c> = sub-sub-processor, etc. Bounded by <c>MaxSubProcessorDepth</c> in configuration.
    /// </remarks>
    public int Depth { get; set; }

    /// <summary>
    /// The type of written authorization granted for sub-processor engagement per Article 28(2).
    /// </summary>
    public SubProcessorAuthorizationType AuthorizationType { get; set; }

    /// <summary>
    /// Whether this processor has been removed from the processing chain.
    /// </summary>
    public bool IsRemoved { get; set; }

    /// <summary>
    /// The number of sub-processors currently registered under this processor.
    /// </summary>
    /// <remarks>
    /// Incremented when a sub-processor is added and decremented when removed.
    /// Provides a quick view of the processing chain breadth without querying sub-processor aggregates.
    /// </remarks>
    public int SubProcessorCount { get; set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// The UTC timestamp when this processor was registered.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this processor record (UTC).
    /// </summary>
    /// <remarks>
    /// Updated on every event applied to the processor aggregate.
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
