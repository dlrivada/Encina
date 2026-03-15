using Encina.Compliance.CrossBorderTransfer.Model;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

namespace Encina.Compliance.CrossBorderTransfer.Events;

/// <summary>
/// Raised when a new Transfer Impact Assessment is created for a transfer route.
/// </summary>
/// <remarks>
/// Initiates the TIA lifecycle. The assessment starts in <see cref="TIAStatus.Draft"/> status
/// and must progress through risk assessment and DPO review before it can authorize transfers.
/// </remarks>
/// <param name="TIAId">The unique identifier for the TIA.</param>
/// <param name="SourceCountryCode">ISO 3166-1 alpha-2 country code of the data exporter.</param>
/// <param name="DestinationCountryCode">ISO 3166-1 alpha-2 country code of the data importer.</param>
/// <param name="DataCategory">Category of personal data being assessed for transfer.</param>
/// <param name="CreatedBy">Identifier of the user who initiated the TIA.</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record TIACreated(
    Guid TIAId,
    string SourceCountryCode,
    string DestinationCountryCode,
    string DataCategory,
    string CreatedBy,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when the risk assessment for a TIA has been completed by an assessor.
/// </summary>
/// <remarks>
/// Transitions the TIA from <see cref="TIAStatus.Draft"/> to <see cref="TIAStatus.InProgress"/>.
/// The risk score reflects the level of risk to data subjects' rights in the destination country,
/// considering government surveillance laws, judicial redress, and data protection authority effectiveness.
/// </remarks>
/// <param name="TIAId">The TIA identifier.</param>
/// <param name="RiskScore">Risk score between 0.0 (no risk) and 1.0 (maximum risk).</param>
/// <param name="Findings">Summary of risk assessment findings.</param>
/// <param name="AssessorId">Identifier of the person who performed the assessment.</param>
public sealed record TIARiskAssessed(
    Guid TIAId,
    double RiskScore,
    string? Findings,
    string AssessorId) : INotification;

/// <summary>
/// Raised when a supplementary measure is identified as required during the TIA process.
/// </summary>
/// <remarks>
/// Per EDPB Recommendations 01/2020, supplementary measures may be needed to ensure
/// "essentially equivalent" protection. Multiple measures can be required for a single TIA.
/// </remarks>
/// <param name="TIAId">The TIA identifier.</param>
/// <param name="MeasureId">Unique identifier for this supplementary measure.</param>
/// <param name="MeasureType">Category of the measure (technical, contractual, or organizational).</param>
/// <param name="Description">Human-readable description of the required measure.</param>
public sealed record TIASupplementaryMeasureRequired(
    Guid TIAId,
    Guid MeasureId,
    SupplementaryMeasureType MeasureType,
    string Description) : INotification;

/// <summary>
/// Raised when a TIA is submitted for review by the Data Protection Officer.
/// </summary>
/// <remarks>
/// Transitions the TIA from <see cref="TIAStatus.InProgress"/> to <see cref="TIAStatus.PendingDPOReview"/>.
/// The DPO must approve the assessment before it can be used to authorize transfers.
/// </remarks>
/// <param name="TIAId">The TIA identifier.</param>
/// <param name="SubmittedBy">Identifier of the person who submitted the TIA for review.</param>
public sealed record TIASubmittedForDPOReview(
    Guid TIAId,
    string SubmittedBy) : INotification;

/// <summary>
/// Raised when the DPO approves a Transfer Impact Assessment.
/// </summary>
/// <remarks>
/// The DPO has reviewed the risk assessment findings and supplementary measures,
/// and determined that the transfer can proceed under the identified legal basis.
/// </remarks>
/// <param name="TIAId">The TIA identifier.</param>
/// <param name="ReviewedBy">Identifier of the DPO who approved the assessment.</param>
public sealed record TIADPOApproved(
    Guid TIAId,
    string ReviewedBy) : INotification;

/// <summary>
/// Raised when the DPO rejects a Transfer Impact Assessment.
/// </summary>
/// <remarks>
/// The TIA is returned to <see cref="TIAStatus.InProgress"/> for revision.
/// The assessor must address the DPO's concerns and resubmit.
/// </remarks>
/// <param name="TIAId">The TIA identifier.</param>
/// <param name="ReviewedBy">Identifier of the DPO who rejected the assessment.</param>
/// <param name="Reason">Explanation of why the assessment was rejected.</param>
public sealed record TIADPORejected(
    Guid TIAId,
    string ReviewedBy,
    string Reason) : INotification;

/// <summary>
/// Raised when a TIA is completed after DPO approval.
/// </summary>
/// <remarks>
/// Transitions the TIA to <see cref="TIAStatus.Completed"/>. The assessment can now be
/// referenced by approved transfers as evidence of compliance with Schrems II requirements.
/// </remarks>
/// <param name="TIAId">The TIA identifier.</param>
public sealed record TIACompleted(
    Guid TIAId) : INotification;

/// <summary>
/// Raised when a TIA expires due to elapsed validity period or changed legal landscape.
/// </summary>
/// <remarks>
/// Transitions the TIA to <see cref="TIAStatus.Expired"/>. Any approved transfers that
/// reference this TIA should be reassessed. A new TIA must be conducted before transfers
/// can continue on this route.
/// </remarks>
/// <param name="TIAId">The TIA identifier.</param>
public sealed record TIAExpired(
    Guid TIAId) : INotification;
