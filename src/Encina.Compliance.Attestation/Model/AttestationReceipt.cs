namespace Encina.Compliance.Attestation.Model;

/// <summary>
/// Immutable receipt proving an audit record was attested at a specific time.
/// </summary>
public sealed record AttestationReceipt
{
    /// <summary>
    /// Gets the unique identifier for this attestation.
    /// </summary>
    public required Guid AttestationId { get; init; }

    /// <summary>
    /// Gets the identifier of the attested audit record.
    /// </summary>
    public required Guid AuditRecordId { get; init; }

    /// <summary>
    /// Gets the SHA-256 content hash of the audit record at attestation time.
    /// </summary>
    public required string ContentHash { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the attestation was created.
    /// </summary>
    public required DateTimeOffset AttestedAtUtc { get; init; }

    /// <summary>
    /// Gets the name identifying which provider created this attestation.
    /// </summary>
    public required string ProviderName { get; init; }

    /// <summary>
    /// Gets the cryptographic signature or proof binding the content hash to the attestation.
    /// </summary>
    public required string Signature { get; init; }

    /// <summary>
    /// Gets provider-specific proof data (e.g., ledger transaction ID, Merkle tree path, chain index).
    /// </summary>
    public IReadOnlyDictionary<string, string>? ProofMetadata { get; init; }
}
