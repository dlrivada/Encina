using Encina.Compliance.DataResidency.Model;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

namespace Encina.Compliance.DataResidency.Events;

/// <summary>
/// Raised when a new data residency policy is created for a specific data category.
/// </summary>
/// <remarks>
/// <para>
/// Initiates the residency policy lifecycle. The policy defines which geographic regions
/// are allowed for storing and processing data of the specified <paramref name="DataCategory"/>,
/// per GDPR Chapter V (Articles 44–49) international data transfer requirements.
/// </para>
/// <para>
/// When <paramref name="AllowedRegionCodes"/> is empty, no geographic restrictions are applied
/// to the data category. When <paramref name="RequireAdequacyDecision"/> is <see langword="true"/>,
/// data can only be stored in regions with an EU adequacy decision under Article 45.
/// </para>
/// <para>
/// The <paramref name="AllowedTransferBases"/> list defines which GDPR Chapter V legal bases
/// are acceptable for cross-border transfers involving this data category (e.g., SCCs, BCRs,
/// explicit consent).
/// </para>
/// </remarks>
/// <param name="PolicyId">Unique identifier for this residency policy aggregate.</param>
/// <param name="DataCategory">The data category this policy applies to (e.g., "personal-data", "healthcare-data").</param>
/// <param name="AllowedRegionCodes">Region codes where data of this category may be stored. Empty means no restrictions.</param>
/// <param name="RequireAdequacyDecision">Whether the region must have an EU adequacy decision (GDPR Art. 45).</param>
/// <param name="AllowedTransferBases">Legal bases acceptable for cross-border transfers (GDPR Art. 46, 49).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record ResidencyPolicyCreated(
    Guid PolicyId,
    string DataCategory,
    IReadOnlyList<string> AllowedRegionCodes,
    bool RequireAdequacyDecision,
    IReadOnlyList<TransferLegalBasis> AllowedTransferBases,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when an existing residency policy is updated with new parameters.
/// </summary>
/// <remarks>
/// <para>
/// Updates the policy's allowed regions, adequacy decision requirement, or allowed transfer bases.
/// The <paramref name="PolicyId"/> and data category remain unchanged — to change the category,
/// delete the existing policy and create a new one.
/// </para>
/// <para>
/// Per GDPR Article 5(2) accountability, this event provides an immutable record of all
/// policy changes, enabling organizations to demonstrate that data residency rules were
/// reviewed and adjusted as necessary in response to regulatory changes (e.g., new adequacy
/// decisions, Schrems II implications).
/// </para>
/// </remarks>
/// <param name="PolicyId">The residency policy aggregate identifier.</param>
/// <param name="AllowedRegionCodes">Updated list of allowed region codes.</param>
/// <param name="RequireAdequacyDecision">Updated adequacy decision requirement.</param>
/// <param name="AllowedTransferBases">Updated list of allowed transfer legal bases.</param>
public sealed record ResidencyPolicyUpdated(
    Guid PolicyId,
    IReadOnlyList<string> AllowedRegionCodes,
    bool RequireAdequacyDecision,
    IReadOnlyList<TransferLegalBasis> AllowedTransferBases) : INotification;

/// <summary>
/// Raised when a residency policy is deleted (soft-deleted), preventing further enforcement.
/// </summary>
/// <remarks>
/// <para>
/// A deleted policy no longer enforces data residency rules for its data category.
/// Existing data locations tracked under this policy are not affected — they remain
/// in their current regions but are no longer validated against the policy.
/// </para>
/// <para>
/// Deletion is a soft operation — the event stream preserves the full history of the policy
/// for GDPR Article 5(2) accountability and Article 58 supervisory authority inquiries.
/// </para>
/// </remarks>
/// <param name="PolicyId">The residency policy aggregate identifier.</param>
/// <param name="Reason">The reason for deleting this policy.</param>
public sealed record ResidencyPolicyDeleted(
    Guid PolicyId,
    string Reason) : INotification;
