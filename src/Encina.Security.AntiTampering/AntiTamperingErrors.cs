namespace Encina.Security.AntiTampering;

/// <summary>
/// Factory methods for anti-tampering-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes follow the convention <c>antitampering.{category}</c>.
/// All errors include structured metadata for observability and debugging.
/// </remarks>
public static class AntiTamperingErrors
{
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageAntiTampering = "antitampering";

    /// <summary>Error code when the HMAC signature does not match the expected value.</summary>
    public const string SignatureInvalidCode = "antitampering.signature_invalid";

    /// <summary>Error code when the signature header is missing from the request.</summary>
    public const string SignatureMissingCode = "antitampering.signature_missing";

    /// <summary>Error code when the request timestamp exceeds the configured tolerance window.</summary>
    public const string TimestampExpiredCode = "antitampering.timestamp_expired";

    /// <summary>Error code when a nonce has already been used (replay attack detected).</summary>
    public const string NonceReusedCode = "antitampering.nonce_reused";

    /// <summary>Error code when the nonce header is missing from the request.</summary>
    public const string NonceMissingCode = "antitampering.nonce_missing";

    /// <summary>Error code when the requested signing key cannot be found.</summary>
    public const string KeyNotFoundCode = "antitampering.key_not_found";

    /// <summary>Error code when a signed request has no <see cref="Microsoft.AspNetCore.Http.HttpContext"/> to validate against.</summary>
    public const string NoHttpContextCode = "antitampering.no_http_context";

    /// <summary>
    /// Creates an error when the HMAC signature does not match.
    /// </summary>
    /// <param name="keyId">The key identifier used for verification.</param>
    /// <returns>An error indicating the signature is invalid.</returns>
    public static EncinaError SignatureInvalid(string keyId) =>
        EncinaErrors.Create(
            code: SignatureInvalidCode,
            message: $"HMAC signature verification failed for key '{keyId}'.",
            details: new Dictionary<string, object?>
            {
                ["keyId"] = keyId,
                [MetadataKeyStage] = MetadataStageAntiTampering
            });

    /// <summary>
    /// Creates an error when the signature header is missing.
    /// </summary>
    /// <param name="headerName">The name of the missing header.</param>
    /// <returns>An error indicating the signature header is missing.</returns>
    public static EncinaError SignatureMissing(string headerName) =>
        EncinaErrors.Create(
            code: SignatureMissingCode,
            message: $"Required signature header '{headerName}' is missing.",
            details: new Dictionary<string, object?>
            {
                ["headerName"] = headerName,
                [MetadataKeyStage] = MetadataStageAntiTampering
            });

    /// <summary>
    /// Creates an error when the request timestamp exceeds the tolerance window.
    /// </summary>
    /// <param name="timestamp">The timestamp from the request.</param>
    /// <param name="toleranceMinutes">The configured tolerance window in minutes.</param>
    /// <returns>An error indicating the timestamp has expired.</returns>
    public static EncinaError TimestampExpired(DateTimeOffset timestamp, int toleranceMinutes) =>
        EncinaErrors.Create(
            code: TimestampExpiredCode,
            message: $"Request timestamp '{timestamp:O}' exceeds the {toleranceMinutes}-minute tolerance window.",
            details: new Dictionary<string, object?>
            {
                ["timestamp"] = timestamp.ToString("O"),
                ["toleranceMinutes"] = toleranceMinutes,
                [MetadataKeyStage] = MetadataStageAntiTampering
            });

    /// <summary>
    /// Creates an error when a nonce has already been used.
    /// </summary>
    /// <param name="nonce">The reused nonce value.</param>
    /// <returns>An error indicating a replay attack was detected.</returns>
    public static EncinaError NonceReused(string nonce) =>
        EncinaErrors.Create(
            code: NonceReusedCode,
            message: $"Nonce '{nonce}' has already been used. Possible replay attack.",
            details: new Dictionary<string, object?>
            {
                ["nonce"] = nonce,
                [MetadataKeyStage] = MetadataStageAntiTampering
            });

    /// <summary>
    /// Creates an error when the nonce header is missing.
    /// </summary>
    /// <param name="headerName">The name of the missing nonce header.</param>
    /// <returns>An error indicating the nonce header is missing.</returns>
    public static EncinaError NonceMissing(string headerName) =>
        EncinaErrors.Create(
            code: NonceMissingCode,
            message: $"Required nonce header '{headerName}' is missing.",
            details: new Dictionary<string, object?>
            {
                ["headerName"] = headerName,
                [MetadataKeyStage] = MetadataStageAntiTampering
            });

    /// <summary>
    /// Creates an error when the requested signing key cannot be found.
    /// </summary>
    /// <param name="keyId">The key identifier that was not found.</param>
    /// <returns>An error indicating the key was not found.</returns>
    public static EncinaError KeyNotFound(string keyId) =>
        EncinaErrors.Create(
            code: KeyNotFoundCode,
            message: $"Signing key '{keyId}' was not found.",
            details: new Dictionary<string, object?>
            {
                ["keyId"] = keyId,
                [MetadataKeyStage] = MetadataStageAntiTampering
            });

    /// <summary>
    /// Creates an error when a request decorated with <c>[RequireSignature]</c> arrives with no
    /// <see cref="Microsoft.AspNetCore.Http.HttpContext"/> available to extract signature headers from.
    /// </summary>
    /// <param name="requestType">The name of the request type that required a signature.</param>
    /// <returns>An error indicating the request was rejected because no HTTP context was available.</returns>
    public static EncinaError NoHttpContext(string requestType) =>
        EncinaErrors.Create(
            code: NoHttpContextCode,
            message: $"Request '{requestType}' requires a signature but no HttpContext was available to validate it. " +
                     "Fail closed by default; opt in explicitly via AntiTamperingOptions.SkipWhenNoHttpContext or " +
                     "RequireSignatureAttribute.SkipWhenNoHttpContext if this request type is meant to run outside HTTP.",
            details: new Dictionary<string, object?>
            {
                ["requestType"] = requestType,
                [MetadataKeyStage] = MetadataStageAntiTampering
            });
}
