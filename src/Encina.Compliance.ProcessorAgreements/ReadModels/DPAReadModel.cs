using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.ProcessorAgreements.ReadModels;

/// <summary>
/// Query-optimized projected view of a Data Processing Agreement, built from <see cref="Aggregates.DPAAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the DPA aggregate event stream by
/// <see cref="DPAProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Art. 5(2) accountability requirements.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// <para>
/// Tracks the full DPA lifecycle (Active → PendingRenewal → Active, or Active → Expired/Terminated),
/// mandatory terms compliance per Article 28(3)(a)-(h), and audit history per Article 28(3)(h).
/// </para>
/// </remarks>
public sealed class DPAReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this agreement (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The identifier of the processor this agreement covers.
    /// </summary>
    public Guid ProcessorId { get; set; }

    /// <summary>
    /// The current lifecycle status of this agreement.
    /// </summary>
    public DPAStatus Status { get; set; }

    /// <summary>
    /// The compliance status of the eight mandatory contractual terms per Article 28(3)(a)-(h).
    /// </summary>
    public DPAMandatoryTerms MandatoryTerms { get; set; } = null!;

    /// <summary>
    /// Whether Standard Contractual Clauses are included per Articles 46(2)(c)/(d).
    /// </summary>
    public bool HasSCCs { get; set; }

    /// <summary>
    /// The documented processing purposes covered by this agreement.
    /// </summary>
    /// <remarks>
    /// Per Article 28(3), the contract must set out "the nature and purpose of the processing."
    /// </remarks>
    public List<string> ProcessingPurposes { get; set; } = [];

    /// <summary>
    /// The UTC timestamp when this agreement was signed by both parties.
    /// </summary>
    public DateTimeOffset SignedAtUtc { get; set; }

    /// <summary>
    /// The UTC expiration date, or <c>null</c> for indefinite agreements.
    /// </summary>
    /// <remarks>
    /// When set, the expiration monitoring system publishes notifications as the date approaches.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    /// <summary>
    /// The reason the agreement was terminated, or <c>null</c> if not terminated.
    /// </summary>
    /// <remarks>
    /// Per Article 28(3)(g), upon termination the processor must delete or return all personal data.
    /// </remarks>
    public string? TerminationReason { get; set; }

    /// <summary>
    /// The UTC timestamp when the agreement was terminated, or <c>null</c> if not terminated.
    /// </summary>
    public DateTimeOffset? TerminatedAtUtc { get; set; }

    /// <summary>
    /// Audit history for this agreement per Article 28(3)(h).
    /// </summary>
    /// <remarks>
    /// Each entry captures the auditor, findings, and timestamp of an audit conducted on this agreement.
    /// The processor must make available all information necessary to demonstrate compliance
    /// and allow for and contribute to audits, including inspections.
    /// </remarks>
    public List<AuditRecord> AuditHistory { get; set; } = [];

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// The UTC timestamp when this agreement was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this agreement record (UTC).
    /// </summary>
    /// <remarks>
    /// Updated on every event applied to the DPA aggregate.
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
    /// Determines whether this agreement is currently active and not expired.
    /// </summary>
    /// <remarks>
    /// An agreement is active when its <see cref="Status"/> is <see cref="DPAStatus.Active"/>
    /// and its <see cref="ExpiresAtUtc"/> has not passed (or is <c>null</c>).
    /// </remarks>
    /// <param name="nowUtc">The current UTC time for comparison against <see cref="ExpiresAtUtc"/>.</param>
    /// <returns><see langword="true"/> if the agreement is active and has not expired; otherwise, <see langword="false"/>.</returns>
    public bool IsActive(DateTimeOffset nowUtc) =>
        Status == DPAStatus.Active &&
        (ExpiresAtUtc is null || ExpiresAtUtc > nowUtc);
}

/// <summary>
/// Summary of an audit conducted on a Data Processing Agreement per GDPR Article 28(3)(h).
/// </summary>
/// <remarks>
/// Per Article 28(3)(h), the processor must make available to the controller all information
/// necessary to demonstrate compliance and allow for and contribute to audits, including inspections.
/// Each record captures a single audit event for the accountability trail.
/// </remarks>
/// <param name="AuditorId">Identifier of the person who conducted the audit.</param>
/// <param name="AuditFindings">Summary of the audit findings and observations.</param>
/// <param name="AuditedAtUtc">Timestamp when the audit was conducted (UTC).</param>
public sealed record AuditRecord(
    string AuditorId,
    string AuditFindings,
    DateTimeOffset AuditedAtUtc);
