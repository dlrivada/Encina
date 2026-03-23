using Encina.Compliance.Attestation.Model;

namespace Encina.Compliance.Attestation.Abstractions;

/// <summary>
/// Persists and retrieves attestation receipts independently of the attestation provider.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IAuditAttestationProvider"/> creates cryptographic proof. This interface
/// handles durable storage of the resulting receipts — a separate concern.
/// </para>
/// <para>
/// <see cref="Providers.InMemoryAttestationProvider"/> and
/// <see cref="Providers.HashChainAttestationProvider"/> implement both interfaces,
/// storing receipts internally. For <see cref="Providers.HttpAttestationProvider"/>,
/// register a separate <see cref="IAttestationReceiptStore"/> to persist receipts returned
/// by the external endpoint.
/// </para>
/// </remarks>
public interface IAttestationReceiptStore
{
    /// <summary>
    /// Persists an attestation receipt.
    /// </summary>
    /// <param name="receipt">The receipt to store.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask StoreReceiptAsync(AttestationReceipt receipt, CancellationToken ct = default);

    /// <summary>
    /// Retrieves an attestation receipt by audit record identifier.
    /// </summary>
    /// <param name="recordId">The audit record identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The receipt if found; otherwise null.</returns>
    ValueTask<AttestationReceipt?> GetReceiptAsync(Guid recordId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves multiple attestation receipts by audit record identifiers.
    /// Records without a stored receipt are omitted from the result.
    /// </summary>
    /// <param name="recordIds">The audit record identifiers.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask<IReadOnlyList<AttestationReceipt>> GetReceiptsAsync(
        IEnumerable<Guid> recordIds, CancellationToken ct = default);
}
