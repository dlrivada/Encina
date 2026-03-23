using Encina.Compliance.Attestation.Model;

using LanguageExt;

namespace Encina.Compliance.Attestation.Abstractions;

/// <summary>
/// Creates and verifies tamper-evident attestations for audit records.
/// Implementations range from local hash chains (free, self-hosted)
/// to cloud immutable ledgers (Azure, AWS) and third-party services.
/// </summary>
public interface IAuditAttestationProvider
{
    /// <summary>
    /// Gets the unique name identifying this provider (e.g., "InMemory", "HashChain", "Http").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Creates a tamper-evident attestation for an audit record,
    /// producing a receipt that can be independently verified.
    /// </summary>
    /// <param name="record">The audit record to attest.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An attestation receipt, or an error if attestation failed.</returns>
    ValueTask<Either<EncinaError, AttestationReceipt>> AttestAsync(
        AuditRecord record, CancellationToken ct = default);

    /// <summary>
    /// Verifies that an attestation receipt is valid and the
    /// referenced audit record has not been tampered with.
    /// </summary>
    /// <param name="receipt">The receipt to verify.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A verification result, or an error if verification could not be performed.</returns>
    ValueTask<Either<EncinaError, AttestationVerification>> VerifyAsync(
        AttestationReceipt receipt, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a previously issued attestation receipt by audit record identifier.
    /// </summary>
    /// <param name="recordId">The audit record identifier to retrieve the receipt for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The attestation receipt, or an error if not found or retrieval failed.</returns>
    ValueTask<Either<EncinaError, AttestationReceipt>> GetReceiptAsync(
        Guid recordId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves multiple attestation receipts by audit record identifiers.
    /// </summary>
    /// <param name="recordIds">The audit record identifiers to retrieve receipts for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of found receipts, or an error if retrieval failed.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<AttestationReceipt>>> GetReceiptsAsync(
        IEnumerable<Guid> recordIds, CancellationToken ct = default);
}
