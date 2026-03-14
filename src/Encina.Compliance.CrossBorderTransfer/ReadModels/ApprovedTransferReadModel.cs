using Encina.Compliance.CrossBorderTransfer.Model;

namespace Encina.Compliance.CrossBorderTransfer.ReadModels;

/// <summary>
/// Read-only projected view of an approved transfer, built from approved transfer aggregate events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the <c>ApprovedTransferAggregate</c> event stream
/// by Marten inline projections. It provides an efficient query view without replaying events.
/// </para>
/// <para>
/// Used by <c>IApprovedTransferService</c> query methods to return approved transfer state to consumers.
/// </para>
/// </remarks>
public sealed record ApprovedTransferReadModel
{
    /// <summary>
    /// Unique identifier for this approved transfer.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the data exporter.
    /// </summary>
    public required string SourceCountryCode { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code of the data importer.
    /// </summary>
    public required string DestinationCountryCode { get; init; }

    /// <summary>
    /// Category of personal data authorized for transfer.
    /// </summary>
    public required string DataCategory { get; init; }

    /// <summary>
    /// The legal basis under which the transfer was authorized.
    /// </summary>
    public required TransferBasis Basis { get; init; }

    /// <summary>
    /// Reference to the supporting SCC agreement, if the basis is <see cref="TransferBasis.SCCs"/>.
    /// </summary>
    public Guid? SCCAgreementId { get; init; }

    /// <summary>
    /// Reference to the supporting Transfer Impact Assessment, if applicable.
    /// </summary>
    public Guid? TIAId { get; init; }

    /// <summary>
    /// Identifier of the person who approved the transfer.
    /// </summary>
    public required string ApprovedBy { get; init; }

    /// <summary>
    /// Optional expiration date of the transfer authorization (UTC).
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>
    /// Indicates whether the transfer has been revoked.
    /// </summary>
    public required bool IsRevoked { get; init; }

    /// <summary>
    /// Timestamp when the transfer was revoked (UTC).
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; init; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; init; }

    /// <summary>
    /// Determines whether this approved transfer is currently valid.
    /// </summary>
    /// <param name="nowUtc">The current UTC time for expiration evaluation.</param>
    /// <returns><c>true</c> if the transfer is active, not revoked, and not expired; otherwise <c>false</c>.</returns>
    public bool IsValid(DateTimeOffset nowUtc)
    {
        if (IsRevoked)
        {
            return false;
        }

        if (ExpiresAtUtc.HasValue && nowUtc >= ExpiresAtUtc.Value)
        {
            return false;
        }

        return true;
    }
}
