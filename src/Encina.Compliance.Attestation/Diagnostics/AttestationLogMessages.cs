using Microsoft.Extensions.Logging;

namespace Encina.Compliance.Attestation.Diagnostics;

/// <summary>
/// High-performance structured log messages for the attestation compliance pipeline.
/// </summary>
/// <remarks>
/// Event IDs are allocated in the 9600–9699 range reserved for attestation compliance
/// (see <c>EventIdRanges.ComplianceAttestation</c>).
/// </remarks>
internal static partial class AttestationLogMessages
{
    [LoggerMessage(9600, LogLevel.Information,
        "Attestation created. RecordId={RecordId}, Provider={Provider}")]
    internal static partial void AttestationCreated(ILogger logger, Guid recordId, string provider);

    [LoggerMessage(9601, LogLevel.Information,
        "Attestation verification completed. RecordId={RecordId}, IsValid={IsValid}, Provider={Provider}")]
    internal static partial void VerificationCompleted(ILogger logger, Guid recordId, bool isValid, string provider);

    [LoggerMessage(9602, LogLevel.Debug,
        "Idempotent attestation returned (duplicate RecordId). RecordId={RecordId}, Provider={Provider}")]
    internal static partial void IdempotentAttestationReturned(ILogger logger, Guid recordId, string provider);

    [LoggerMessage(9603, LogLevel.Error,
        "Hash chain integrity broken at index {BrokenIndex} of {ChainLength}.")]
    internal static partial void ChainIntegrityBroken(ILogger logger, int brokenIndex, int chainLength);

    [LoggerMessage(9604, LogLevel.Error,
        "HTTP attestation endpoint error. Url={Url}, StatusCode={StatusCode}")]
    internal static partial void HttpEndpointError(ILogger logger, Uri url, int statusCode);

    [LoggerMessage(9605, LogLevel.Debug,
        "Attestation health check completed. Status={Status}, Provider={Provider}")]
    internal static partial void HealthCheckCompleted(ILogger logger, string status, string provider);

    [LoggerMessage(9606, LogLevel.Warning,
        "Attestation failed and enforcement mode blocked the pipeline. RequestType={RequestType}, Error={Error}")]
    internal static partial void AttestationEnforced(ILogger logger, string requestType, string error);

    [LoggerMessage(9607, LogLevel.Warning,
        "Attestation failed in LogOnly mode — pipeline continues. RequestType={RequestType}, Error={Error}")]
    internal static partial void AttestationLogOnly(ILogger logger, string requestType, string error);

    [LoggerMessage(9608, LogLevel.Warning,
        "HashChain provider is using an ephemeral HMAC key. The chain is in-memory and will be lost on process restart. " +
        "For persistent verification, provide a stable key via HashChainOptions.HmacKey and use an external store or the HttpAttestationProvider.")]
    internal static partial void EphemeralHmacKeyWarning(ILogger logger);
}
