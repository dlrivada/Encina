using Encina.Compliance.CrossBorderTransfer.Events;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.DomainModeling;

namespace Encina.Compliance.CrossBorderTransfer.Aggregates;

/// <summary>
/// Event-sourced aggregate representing an approved international data transfer.
/// </summary>
/// <remarks>
/// <para>
/// An approved transfer records the authorization of a specific data transfer route
/// (source country → destination country) for a given data category under a validated
/// GDPR Chapter V legal basis.
/// </para>
/// <para>
/// The lifecycle is: Approved → Active until Revoked, Expired, or Renewed.
/// The <see cref="IsValid"/> method determines current validity by checking revocation status
/// and expiration date.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Art. 5(2) accountability requirements.
/// </para>
/// </remarks>
public sealed class ApprovedTransferAggregate : AggregateBase
{
    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the data exporter.
    /// </summary>
    public string SourceCountryCode { get; private set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the data importer.
    /// </summary>
    public string DestinationCountryCode { get; private set; } = string.Empty;

    /// <summary>
    /// Category of personal data authorized for transfer.
    /// </summary>
    public string DataCategory { get; private set; } = string.Empty;

    /// <summary>
    /// The legal basis under which the transfer was authorized.
    /// </summary>
    public TransferBasis Basis { get; private set; }

    /// <summary>
    /// Reference to the supporting SCC agreement, if the basis is <see cref="TransferBasis.SCCs"/>.
    /// </summary>
    public Guid? SCCAgreementId { get; private set; }

    /// <summary>
    /// Reference to the supporting Transfer Impact Assessment, if applicable.
    /// </summary>
    public Guid? TIAId { get; private set; }

    /// <summary>
    /// Identifier of the person who approved the transfer.
    /// </summary>
    public string ApprovedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Optional expiration date of the transfer authorization (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the transfer has no fixed expiration date.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Indicates whether the transfer has been revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// Timestamp when the transfer was revoked (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the transfer has not been revoked.
    /// </remarks>
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>
    /// Indicates whether the transfer has expired.
    /// </summary>
    public bool IsExpired { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// Approves a new international data transfer under the specified legal basis.
    /// </summary>
    /// <param name="id">Unique identifier for the approved transfer.</param>
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
    /// <returns>A new <see cref="ApprovedTransferAggregate"/>.</returns>
    public static ApprovedTransferAggregate Approve(
        Guid id,
        string sourceCountryCode,
        string destinationCountryCode,
        string dataCategory,
        TransferBasis basis,
        Guid? sccAgreementId = null,
        Guid? tiaId = null,
        string approvedBy = "",
        DateTimeOffset? expiresAtUtc = null,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceCountryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationCountryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);

        if (basis == TransferBasis.Blocked)
        {
            throw new ArgumentException("Cannot approve a transfer with a 'Blocked' basis.", nameof(basis));
        }

        var aggregate = new ApprovedTransferAggregate();
        aggregate.RaiseEvent(new TransferApproved(id, sourceCountryCode, destinationCountryCode, dataCategory, basis, sccAgreementId, tiaId, approvedBy, expiresAtUtc, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Revokes the approved transfer.
    /// </summary>
    /// <param name="reason">Explanation of why the transfer is being revoked.</param>
    /// <param name="revokedBy">Identifier of the person revoking the transfer.</param>
    /// <exception cref="InvalidOperationException">Thrown when the transfer is already revoked.</exception>
    public void Revoke(string reason, string revokedBy)
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException("Cannot revoke a transfer that is already revoked.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(revokedBy);

        RaiseEvent(new TransferRevoked(Id, reason, revokedBy));
    }

    /// <summary>
    /// Expires the approved transfer.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the transfer is already expired or revoked.</exception>
    public void Expire()
    {
        if (IsExpired)
        {
            throw new InvalidOperationException("Cannot expire a transfer that is already expired.");
        }

        if (IsRevoked)
        {
            throw new InvalidOperationException("Cannot expire a transfer that has been revoked.");
        }

        RaiseEvent(new TransferExpired(Id));
    }

    /// <summary>
    /// Renews the approved transfer with a new expiration date.
    /// </summary>
    /// <param name="newExpiresAtUtc">The new expiration date for the transfer authorization.</param>
    /// <param name="renewedBy">Identifier of the person renewing the transfer.</param>
    /// <exception cref="InvalidOperationException">Thrown when the transfer is revoked.</exception>
    public void Renew(DateTimeOffset newExpiresAtUtc, string renewedBy)
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException("Cannot renew a transfer that has been revoked.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(renewedBy);

        RaiseEvent(new TransferRenewed(Id, newExpiresAtUtc, renewedBy));
    }

    /// <summary>
    /// Determines whether this approved transfer is currently valid.
    /// </summary>
    /// <param name="nowUtc">The current UTC time for expiration evaluation.</param>
    /// <returns><c>true</c> if the transfer is active, not revoked, and not expired; otherwise <c>false</c>.</returns>
    public bool IsValid(DateTimeOffset nowUtc)
    {
        if (IsRevoked || IsExpired)
        {
            return false;
        }

        if (ExpiresAtUtc.HasValue && nowUtc >= ExpiresAtUtc.Value)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case TransferApproved e:
                Id = e.TransferId;
                SourceCountryCode = e.SourceCountryCode;
                DestinationCountryCode = e.DestinationCountryCode;
                DataCategory = e.DataCategory;
                Basis = e.Basis;
                SCCAgreementId = e.SCCAgreementId;
                TIAId = e.TIAId;
                ApprovedBy = e.ApprovedBy;
                ExpiresAtUtc = e.ExpiresAtUtc;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                break;

            case TransferRevoked:
                IsRevoked = true;
                RevokedAtUtc = DateTimeOffset.UtcNow;
                break;

            case TransferExpired:
                IsExpired = true;
                break;

            case TransferRenewed e:
                ExpiresAtUtc = e.NewExpiresAtUtc;
                IsExpired = false;
                break;
        }
    }
}
