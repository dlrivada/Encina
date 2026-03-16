using Encina.Compliance.ProcessorAgreements.Model;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

namespace Encina.Compliance.ProcessorAgreements.Events;

/// <summary>
/// Raised when a new Data Processing Agreement is executed between a controller and a processor.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 28(3), processing by a processor shall be governed by a contract that sets out
/// the subject-matter, duration, nature and purpose of the processing, the type of personal data,
/// categories of data subjects, and the obligations and rights of the controller.
/// </para>
/// <para>
/// This event initiates the DPA lifecycle. The agreement starts in <see cref="DPAStatus.Active"/>
/// status and tracks compliance with the eight mandatory contractual terms from Article 28(3)(a)-(h)
/// via <see cref="MandatoryTerms"/>.
/// </para>
/// </remarks>
/// <param name="DPAId">The unique identifier for the agreement.</param>
/// <param name="ProcessorId">The identifier of the processor this agreement covers.</param>
/// <param name="MandatoryTerms">Compliance status of the eight mandatory terms per Article 28(3)(a)-(h).</param>
/// <param name="HasSCCs">Whether Standard Contractual Clauses are included per Articles 46(2)(c)/(d).</param>
/// <param name="ProcessingPurposes">The documented processing purposes covered by this agreement.</param>
/// <param name="SignedAtUtc">The UTC timestamp when the agreement was signed by both parties.</param>
/// <param name="ExpiresAtUtc">The UTC expiration date, or <see langword="null"/> for indefinite agreements.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when this event was raised.</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record DPAExecuted(
    Guid DPAId,
    Guid ProcessorId,
    DPAMandatoryTerms MandatoryTerms,
    bool HasSCCs,
    IReadOnlyList<string> ProcessingPurposes,
    DateTimeOffset SignedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset OccurredAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a Data Processing Agreement is amended with updated terms.
/// </summary>
/// <remarks>
/// Amendments may update mandatory terms, SCC inclusion, or processing purposes.
/// Per GDPR Article 28(9), the contract must be in writing, including in electronic form.
/// All amendments are recorded as immutable events for Art. 5(2) accountability.
/// </remarks>
/// <param name="DPAId">The identifier of the agreement being amended.</param>
/// <param name="UpdatedTerms">The updated mandatory terms after the amendment.</param>
/// <param name="HasSCCs">Whether Standard Contractual Clauses are included after the amendment.</param>
/// <param name="ProcessingPurposes">The updated processing purposes after the amendment.</param>
/// <param name="AmendmentReason">The reason for amending the agreement.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the amendment occurred.</param>
public sealed record DPAAmended(
    Guid DPAId,
    DPAMandatoryTerms UpdatedTerms,
    bool HasSCCs,
    IReadOnlyList<string> ProcessingPurposes,
    string AmendmentReason,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when an audit is conducted on a Data Processing Agreement.
/// </summary>
/// <remarks>
/// Per GDPR Article 28(3)(h), the processor must make available to the controller all information
/// necessary to demonstrate compliance and allow for and contribute to audits, including inspections.
/// This event records the audit findings for the accountability trail.
/// </remarks>
/// <param name="DPAId">The identifier of the agreement being audited.</param>
/// <param name="AuditorId">The identifier of the person who conducted the audit.</param>
/// <param name="AuditFindings">Summary of the audit findings and observations.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the audit was conducted.</param>
public sealed record DPAAudited(
    Guid DPAId,
    string AuditorId,
    string AuditFindings,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a Data Processing Agreement is renewed with an updated expiration date.
/// </summary>
/// <remarks>
/// Renewal transitions a <see cref="DPAStatus.PendingRenewal"/> or <see cref="DPAStatus.Active"/>
/// agreement back to <see cref="DPAStatus.Active"/> with a new expiration date. The renewal event
/// provides a complete audit trail of the contractual timeline.
/// </remarks>
/// <param name="DPAId">The identifier of the agreement being renewed.</param>
/// <param name="NewExpiresAtUtc">The new UTC expiration date after renewal.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the renewal occurred.</param>
public sealed record DPARenewed(
    Guid DPAId,
    DateTimeOffset NewExpiresAtUtc,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a Data Processing Agreement is explicitly terminated by one of the parties.
/// </summary>
/// <remarks>
/// Per GDPR Article 28(3)(g), upon termination the processor must, at the choice of the controller,
/// delete or return all personal data and certify that it has done so, unless Union or Member State
/// law requires storage.
/// </remarks>
/// <param name="DPAId">The identifier of the agreement being terminated.</param>
/// <param name="Reason">The reason for terminating the agreement.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the termination occurred.</param>
public sealed record DPATerminated(
    Guid DPAId,
    string Reason,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a Data Processing Agreement has passed its expiration date without renewal.
/// </summary>
/// <remarks>
/// Transitions the agreement to <see cref="DPAStatus.Expired"/>. Processing operations relying
/// on this agreement should be blocked or warned by the <c>ProcessorValidationPipelineBehavior</c>
/// until a new agreement is signed.
/// </remarks>
/// <param name="DPAId">The identifier of the expired agreement.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the expiration was detected.</param>
public sealed record DPAExpired(
    Guid DPAId,
    DateTimeOffset OccurredAtUtc) : INotification;

/// <summary>
/// Raised when a Data Processing Agreement is marked as pending renewal due to approaching expiration.
/// </summary>
/// <remarks>
/// Transitions the agreement to <see cref="DPAStatus.PendingRenewal"/>. This status triggers
/// <c>DPAExpiringNotification</c> to alert compliance teams about upcoming renewal deadlines.
/// The agreement remains valid for processing operations while in this status.
/// </remarks>
/// <param name="DPAId">The identifier of the agreement approaching expiration.</param>
/// <param name="OccurredAtUtc">The UTC timestamp when the pending renewal was detected.</param>
public sealed record DPAMarkedPendingRenewal(
    Guid DPAId,
    DateTimeOffset OccurredAtUtc) : INotification;
