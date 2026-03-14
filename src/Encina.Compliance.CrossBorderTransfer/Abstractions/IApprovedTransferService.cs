using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using LanguageExt;

namespace Encina.Compliance.CrossBorderTransfer.Abstractions;

/// <summary>
/// Service interface for managing approved international data transfer lifecycle operations.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean API for approving, revoking, renewing, and querying approved transfers.
/// The implementation wraps the event-sourced <c>ApprovedTransferAggregate</c> via
/// <c>IAggregateRepository&lt;ApprovedTransferAggregate&gt;</c>.
/// </para>
/// <para>
/// An approved transfer records the authorization of a specific data transfer route
/// (source → destination) under a validated GDPR Chapter V legal basis.
/// </para>
/// </remarks>
public interface IApprovedTransferService
{
    /// <summary>
    /// Approves a new international data transfer under the specified legal basis.
    /// </summary>
    /// <param name="sourceCountryCode">ISO 3166-1 alpha-2 country code of the data exporter.</param>
    /// <param name="destinationCountryCode">ISO 3166-1 alpha-2 country code of the data importer.</param>
    /// <param name="dataCategory">Category of personal data authorized for transfer.</param>
    /// <param name="basis">The legal basis authorizing the transfer.</param>
    /// <param name="sccAgreementId">Reference to the supporting SCC agreement, if applicable.</param>
    /// <param name="tiaId">Reference to the supporting TIA, if applicable.</param>
    /// <param name="approvedBy">Identifier of the person approving the transfer.</param>
    /// <param name="expiresAtUtc">Optional expiration date of the transfer authorization.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly approved transfer.</returns>
    ValueTask<Either<EncinaError, Guid>> ApproveTransferAsync(
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        TransferBasis basis,
        Guid? sccAgreementId = null,
        Guid? tiaId = null,
        string approvedBy = "",
        DateTimeOffset? expiresAtUtc = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an approved transfer.
    /// </summary>
    /// <param name="transferId">The approved transfer identifier.</param>
    /// <param name="reason">Explanation of why the transfer is being revoked.</param>
    /// <param name="revokedBy">Identifier of the person revoking the transfer.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RevokeTransferAsync(
        Guid transferId,
        string reason,
        string revokedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renews an approved transfer with a new expiration date.
    /// </summary>
    /// <param name="transferId">The approved transfer identifier.</param>
    /// <param name="newExpiresAtUtc">The new expiration date.</param>
    /// <param name="renewedBy">Identifier of the person renewing the transfer.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RenewTransferAsync(
        Guid transferId,
        DateTimeOffset newExpiresAtUtc,
        string renewedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an approved transfer by its route (source, destination, data category).
    /// </summary>
    /// <param name="sourceCountryCode">ISO 3166-1 alpha-2 country code of the data exporter.</param>
    /// <param name="destinationCountryCode">ISO 3166-1 alpha-2 country code of the data importer.</param>
    /// <param name="dataCategory">Category of personal data.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found), or the approved transfer read model.</returns>
    ValueTask<Either<EncinaError, ApprovedTransferReadModel>> GetApprovedTransferAsync(
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a valid approved transfer exists for the specified route.
    /// </summary>
    /// <param name="sourceCountryCode">ISO 3166-1 alpha-2 country code of the data exporter.</param>
    /// <param name="destinationCountryCode">ISO 3166-1 alpha-2 country code of the data importer.</param>
    /// <param name="dataCategory">Category of personal data.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or <c>true</c> if a valid approved transfer exists.</returns>
    ValueTask<Either<EncinaError, bool>> IsTransferApprovedAsync(
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        CancellationToken cancellationToken = default);
}
