namespace Encina.Compliance.Attestation;

/// <summary>
/// Well-known error codes and factory methods for attestation-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes follow the convention <c>attestation.{category}</c>.
/// All factory methods include structured metadata for observability.
/// </remarks>
public static class AttestationErrors
{
    private const string MetadataKeyProvider = "provider";
    private const string MetadataKeyRecordId = "recordId";
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageAttestation = "attestation";

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

    /// <summary>Error code when a requested receipt is not found.</summary>
    public const string ReceiptNotFoundCode = "attestation.receipt_not_found";

    /// <summary>Error code when the HTTP response body exceeds the configured size limit.</summary>
    public const string HttpResponseTooLargeCode = "attestation.http_response_too_large";

    // --- Factory methods ---

    /// <summary>
    /// Creates an error when attestation receipt verification fails.
    /// </summary>
    /// <param name="recordId">The audit record identifier whose receipt failed verification.</param>
    /// <param name="provider">The provider that performed the verification.</param>
    /// <param name="reason">The specific reason for the failure.</param>
    public static EncinaError VerificationFailed(Guid recordId, string provider, string reason) =>
        EncinaErrors.Create(
            code: VerificationFailedCode,
            message: $"Attestation verification failed for record '{recordId}' (provider: {provider}): {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRecordId] = recordId.ToString(),
                [MetadataKeyProvider] = provider,
                [MetadataKeyStage] = MetadataStageAttestation,
                ["reason"] = reason
            });

    /// <summary>
    /// Creates an error when the attestation provider is unavailable or returns an unexpected response.
    /// </summary>
    /// <param name="provider">The provider that is unavailable.</param>
    /// <param name="reason">The underlying reason.</param>
    public static EncinaError ProviderUnavailable(string provider, string reason) =>
        EncinaErrors.Create(
            code: ProviderUnavailableCode,
            message: $"Attestation provider '{provider}' is unavailable: {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProvider] = provider,
                [MetadataKeyStage] = MetadataStageAttestation,
                ["reason"] = reason
            });

    /// <summary>
    /// Creates an error when the content hash of an audit record does not match.
    /// </summary>
    /// <param name="recordId">The audit record identifier.</param>
    /// <param name="provider">The provider that detected the mismatch.</param>
    public static EncinaError ContentHashMismatch(Guid recordId, string provider) =>
        EncinaErrors.Create(
            code: ContentHashMismatchCode,
            message: $"Content hash mismatch for record '{recordId}' (provider: {provider}). "
                + "The audit record may have been tampered with after attestation.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRecordId] = recordId.ToString(),
                [MetadataKeyProvider] = provider,
                [MetadataKeyStage] = MetadataStageAttestation
            });

    /// <summary>
    /// Creates an error when the hash chain integrity check fails.
    /// </summary>
    /// <param name="provider">The provider whose chain is broken.</param>
    /// <param name="brokenAtIndex">The chain index at which the integrity check failed.</param>
    public static EncinaError ChainIntegrityBroken(string provider, int brokenAtIndex) =>
        EncinaErrors.Create(
            code: ChainIntegrityBrokenCode,
            message: $"Hash chain integrity broken at index {brokenAtIndex} (provider: {provider}). "
                + "One or more chain entries may have been tampered with.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProvider] = provider,
                [MetadataKeyStage] = MetadataStageAttestation,
                ["brokenAtIndex"] = brokenAtIndex
            });

    /// <summary>
    /// Creates an error when the HTTP attestation endpoint returns an error response.
    /// </summary>
    /// <param name="endpointUrl">The endpoint URL that returned the error.</param>
    /// <param name="statusCode">The HTTP status code returned.</param>
    /// <param name="truncatedBody">A truncated excerpt of the error response body.</param>
    public static EncinaError HttpEndpointError(Uri endpointUrl, int statusCode, string truncatedBody) =>
        EncinaErrors.Create(
            code: HttpEndpointErrorCode,
            message: $"HTTP attestation endpoint '{endpointUrl}' returned status {statusCode}.",
            details: new Dictionary<string, object?>
            {
                ["endpointUrl"] = endpointUrl.ToString(),
                ["statusCode"] = statusCode,
                ["responseExcerpt"] = truncatedBody,
                [MetadataKeyStage] = MetadataStageAttestation
            });

    /// <summary>
    /// Creates an error when the HTTP response body exceeds the configured size limit.
    /// </summary>
    /// <param name="provider">The provider that received the oversized response.</param>
    /// <param name="contentLength">The reported content length in bytes.</param>
    /// <param name="maxBytes">The maximum allowed response size in bytes.</param>
    public static EncinaError HttpResponseTooLarge(string provider, long contentLength, long maxBytes) =>
        EncinaErrors.Create(
            code: HttpResponseTooLargeCode,
            message: $"HTTP attestation response from provider '{provider}' exceeds the maximum allowed size "
                + $"({contentLength} bytes > {maxBytes} bytes). The response was rejected to prevent memory exhaustion.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyProvider] = provider,
                [MetadataKeyStage] = MetadataStageAttestation,
                ["contentLength"] = contentLength,
                ["maxBytes"] = maxBytes
            });

    /// <summary>
    /// Creates an error when a receipt for the given record identifier is not found.
    /// </summary>
    /// <param name="recordId">The audit record identifier that was not found.</param>
    /// <param name="provider">The provider that was queried.</param>
    public static EncinaError ReceiptNotFound(Guid recordId, string provider) =>
        EncinaErrors.Create(
            code: ReceiptNotFoundCode,
            message: $"No attestation receipt found for record '{recordId}' (provider: {provider}).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRecordId] = recordId.ToString(),
                [MetadataKeyProvider] = provider,
                [MetadataKeyStage] = MetadataStageAttestation
            });
}
