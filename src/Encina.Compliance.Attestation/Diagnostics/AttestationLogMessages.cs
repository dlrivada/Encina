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
}
