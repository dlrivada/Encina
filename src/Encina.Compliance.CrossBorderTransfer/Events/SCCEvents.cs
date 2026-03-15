using Encina.Compliance.CrossBorderTransfer.Model;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.

namespace Encina.Compliance.CrossBorderTransfer.Events;

/// <summary>
/// Raised when a new Standard Contractual Clauses agreement is registered.
/// </summary>
/// <remarks>
/// Records the execution of an SCC agreement with a data importer under GDPR Art. 46(2)(c).
/// The agreement specifies the applicable SCC module, version, and execution date.
/// </remarks>
/// <param name="AgreementId">Unique identifier for the SCC agreement.</param>
/// <param name="ProcessorId">Identifier of the data processor/importer party to the agreement.</param>
/// <param name="Module">The SCC module applicable to this transfer relationship.</param>
/// <param name="Version">Version of the SCC clauses used (e.g., "2021/914").</param>
/// <param name="ExecutedAtUtc">Timestamp when the agreement was executed.</param>
/// <param name="ExpiresAtUtc">Optional expiration date of the agreement.</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record SCCAgreementRegistered(
    Guid AgreementId,
    string ProcessorId,
    SCCModule Module,
    string Version,
    DateTimeOffset ExecutedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a supplementary measure is added to an SCC agreement.
/// </summary>
/// <remarks>
/// Post-Schrems II, supplementary measures may be required to ensure SCCs provide
/// "essentially equivalent" protection. Each measure is tracked individually.
/// </remarks>
/// <param name="AgreementId">The SCC agreement identifier.</param>
/// <param name="MeasureId">Unique identifier for this supplementary measure.</param>
/// <param name="MeasureType">Category of the measure (technical, contractual, or organizational).</param>
/// <param name="Description">Human-readable description of the supplementary measure.</param>
public sealed record SCCSupplementaryMeasureAdded(
    Guid AgreementId,
    Guid MeasureId,
    SupplementaryMeasureType MeasureType,
    string Description) : INotification;

/// <summary>
/// Raised when an SCC agreement is revoked.
/// </summary>
/// <remarks>
/// Revocation may occur due to data importer non-compliance, supervisory authority order,
/// or material changes in the destination country's legal framework. Transfers relying on
/// this agreement must cease until a replacement agreement is in place.
/// </remarks>
/// <param name="AgreementId">The SCC agreement identifier.</param>
/// <param name="Reason">Explanation of why the agreement was revoked.</param>
/// <param name="RevokedBy">Identifier of the person who revoked the agreement.</param>
public sealed record SCCAgreementRevoked(
    Guid AgreementId,
    string Reason,
    string RevokedBy) : INotification;

/// <summary>
/// Raised when an SCC agreement expires based on its expiration date.
/// </summary>
/// <remarks>
/// The agreement has reached its <see cref="SCCAgreementRegistered.ExpiresAtUtc"/> date.
/// A new agreement must be executed before transfers can continue under SCCs.
/// </remarks>
/// <param name="AgreementId">The SCC agreement identifier.</param>
public sealed record SCCAgreementExpired(
    Guid AgreementId) : INotification;
