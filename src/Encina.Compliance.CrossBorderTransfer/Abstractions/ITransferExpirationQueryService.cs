using LanguageExt;

namespace Encina.Compliance.CrossBorderTransfer.Abstractions;

/// <summary>
/// Service abstraction for querying cross-border transfer artifacts approaching or past
/// their expiration dates.
/// </summary>
/// <remarks>
/// <para>
/// This interface decouples the <see cref="Notifications.TransferExpirationMonitor"/>
/// from the underlying data store (Marten projections, read models, etc.), allowing
/// different query implementations depending on the infrastructure setup.
/// </para>
/// <para>
/// Implementors should query for non-revoked, non-expired transfers whose
/// <c>ExpiresAtUtc</c> falls within the specified time range.
/// </para>
/// </remarks>
public interface ITransferExpirationQueryService
{
    /// <summary>
    /// Gets approved transfers that are expiring within the specified time window.
    /// </summary>
    /// <param name="nowUtc">The current UTC timestamp.</param>
    /// <param name="expirationWindowUtc">The upper bound of the expiration window (UTC).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or a list of expiring transfer information.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<ExpiringTransferInfo>>> GetExpiringTransfersAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset expirationWindowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approved transfers that have already expired but have not been revoked.
    /// </summary>
    /// <param name="nowUtc">The current UTC timestamp.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or a list of expired transfer information.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<ExpiringTransferInfo>>> GetExpiredTransfersAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight projection of a transfer's key attributes for expiration monitoring.
/// </summary>
/// <remarks>
/// Used by <see cref="ITransferExpirationQueryService"/> to return only the fields
/// needed for expiration checks and notification publishing, avoiding full aggregate hydration.
/// </remarks>
/// <param name="Id">The transfer identifier.</param>
/// <param name="SourceCountryCode">ISO 3166-1 alpha-2 code of the data exporter country.</param>
/// <param name="DestinationCountryCode">ISO 3166-1 alpha-2 code of the data importer country.</param>
/// <param name="DataCategory">The data category of the transfer.</param>
/// <param name="ExpiresAtUtc">Timestamp when the transfer authorization expires (UTC).</param>
public sealed record ExpiringTransferInfo(
    Guid Id,
    string SourceCountryCode,
    string DestinationCountryCode,
    string DataCategory,
    DateTimeOffset ExpiresAtUtc);
