namespace Encina.Compliance.Attestation;

/// <summary>
/// Well-known error codes for attestation operations.
/// </summary>
public static class AttestationErrors
{
    /// <summary>Error code when an attestation receipt fails verification.</summary>
    public const string VerificationFailedCode = "attestation.verification_failed";

    /// <summary>Error code when a duplicate record is attested and idempotency returns the existing receipt.</summary>
    public const string DuplicateRecordCode = "attestation.duplicate_record";

    /// <summary>Error code when the attestation provider is unavailable.</summary>
    public const string ProviderUnavailableCode = "attestation.provider_unavailable";

    /// <summary>Error code when the content hash does not match the expected value.</summary>
    public const string ContentHashMismatchCode = "attestation.content_hash_mismatch";

    /// <summary>Error code when the hash chain integrity is broken.</summary>
    public const string ChainIntegrityBrokenCode = "attestation.chain_integrity_broken";

    /// <summary>Error code when the HTTP attestation endpoint returns an error.</summary>
    public const string HttpEndpointErrorCode = "attestation.http_endpoint_error";
}
