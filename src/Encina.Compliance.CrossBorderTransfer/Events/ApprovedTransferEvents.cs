using Encina.Compliance.CrossBorderTransfer.Model;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.

namespace Encina.Compliance.CrossBorderTransfer.Events;

/// <summary>
/// Raised when an international data transfer is approved under a specific legal basis.
/// </summary>
/// <remarks>
/// Records the authorization of a transfer route (source → destination) for a specific data category.
/// The approved transfer references the legal basis, any supporting SCC agreement, and TIA.
/// </remarks>
/// <param name="TransferId">Unique identifier for the approved transfer.</param>
/// <param name="SourceCountryCode">ISO 3166-1 alpha-2 country code of the data exporter.</param>
/// <param name="DestinationCountryCode">ISO 3166-1 alpha-2 country code of the data importer.</param>
/// <param name="DataCategory">Category of personal data authorized for transfer.</param>
/// <param name="Basis">The legal basis under which the transfer is authorized.</param>
/// <param name="SCCAgreementId">Reference to the supporting SCC agreement, if applicable.</param>
/// <param name="TIAId">Reference to the supporting Transfer Impact Assessment, if applicable.</param>
/// <param name="ApprovedBy">Identifier of the person who approved the transfer.</param>
/// <param name="ExpiresAtUtc">Optional expiration date of the transfer authorization.</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record TransferApproved(
    Guid TransferId,
    string SourceCountryCode,
    string DestinationCountryCode,
    string DataCategory,
    TransferBasis Basis,
    Guid? SCCAgreementId,
    Guid? TIAId,
    string ApprovedBy,
    DateTimeOffset? ExpiresAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when an approved transfer is revoked.
/// </summary>
/// <remarks>
/// Revocation may occur due to changes in the destination country's legal framework,
/// expiration of supporting agreements (SCC, TIA), or supervisory authority orders.
/// The transfer must cease immediately upon revocation.
/// </remarks>
/// <param name="TransferId">The approved transfer identifier.</param>
/// <param name="Reason">Explanation of why the transfer was revoked.</param>
/// <param name="RevokedBy">Identifier of the person who revoked the transfer.</param>
public sealed record TransferRevoked(
    Guid TransferId,
    string Reason,
    string RevokedBy) : INotification;

/// <summary>
/// Raised when an approved transfer expires based on its expiration date.
/// </summary>
/// <remarks>
/// The transfer authorization has reached its <see cref="TransferApproved.ExpiresAtUtc"/> date.
/// The transfer must be renewed before data can continue flowing on this route.
/// </remarks>
/// <param name="TransferId">The approved transfer identifier.</param>
public sealed record TransferExpired(
    Guid TransferId) : INotification;

/// <summary>
/// Raised when an approved transfer is renewed with a new expiration date.
/// </summary>
/// <remarks>
/// Extends the validity of an existing approved transfer without requiring a new approval process.
/// The underlying legal basis, SCC agreement, and TIA must still be valid.
/// </remarks>
/// <param name="TransferId">The approved transfer identifier.</param>
/// <param name="NewExpiresAtUtc">The new expiration date for the transfer authorization.</param>
/// <param name="RenewedBy">Identifier of the person who renewed the transfer.</param>
public sealed record TransferRenewed(
    Guid TransferId,
    DateTimeOffset NewExpiresAtUtc,
    string RenewedBy) : INotification;
