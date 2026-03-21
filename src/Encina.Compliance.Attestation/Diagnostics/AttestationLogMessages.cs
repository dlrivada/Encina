using Microsoft.Extensions.Logging;

namespace Encina.Compliance.Attestation.Diagnostics;

/// <summary>
/// High-performance structured log messages for the attestation compliance pipeline.
/// </summary>
/// <remarks>
/// Event IDs are allocated in the 9600–9620 range reserved for attestation compliance
/// (see <c>EventIdRanges.ComplianceAttestation</c>).
/// </remarks>
internal static class AttestationLogMessages
{
    // -- 9600: Attestation created --

    private static readonly Action<ILogger, Guid, string, Exception?> AttestationCreatedDef =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(9600, nameof(AttestationCreated)),
            "Attestation created. RecordId={RecordId}, Provider={Provider}");

    internal static void AttestationCreated(ILogger logger, Guid recordId, string provider)
        => AttestationCreatedDef(logger, recordId, provider, null);

    // -- 9601: Verification completed --

    private static readonly Action<ILogger, Guid, bool, string, Exception?> VerificationCompletedDef =
        LoggerMessage.Define<Guid, bool, string>(
            LogLevel.Information,
            new EventId(9601, nameof(VerificationCompleted)),
            "Attestation verification completed. RecordId={RecordId}, IsValid={IsValid}, Provider={Provider}");

    internal static void VerificationCompleted(ILogger logger, Guid recordId, bool isValid, string provider)
        => VerificationCompletedDef(logger, recordId, isValid, provider, null);

    // -- 9602: Idempotent attestation returned --

    private static readonly Action<ILogger, Guid, string, Exception?> IdempotentAttestationReturnedDef =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Debug,
            new EventId(9602, nameof(IdempotentAttestationReturned)),
            "Idempotent attestation returned (duplicate RecordId). RecordId={RecordId}, Provider={Provider}");

    internal static void IdempotentAttestationReturned(ILogger logger, Guid recordId, string provider)
        => IdempotentAttestationReturnedDef(logger, recordId, provider, null);

    // -- 9603: Chain integrity broken --

    private static readonly Action<ILogger, int, int, Exception?> ChainIntegrityBrokenDef =
        LoggerMessage.Define<int, int>(
            LogLevel.Error,
            new EventId(9603, nameof(ChainIntegrityBroken)),
            "Hash chain integrity broken at index {BrokenIndex} of {ChainLength}.");

    internal static void ChainIntegrityBroken(ILogger logger, int brokenIndex, int chainLength)
        => ChainIntegrityBrokenDef(logger, brokenIndex, chainLength, null);

    // -- 9604: HTTP endpoint error --

    private static readonly Action<ILogger, Uri, int, Exception?> HttpEndpointErrorDef =
        LoggerMessage.Define<Uri, int>(
            LogLevel.Error,
            new EventId(9604, nameof(HttpEndpointError)),
            "HTTP attestation endpoint error. Url={Url}, StatusCode={StatusCode}");

    internal static void HttpEndpointError(ILogger logger, Uri url, int statusCode)
        => HttpEndpointErrorDef(logger, url, statusCode, null);

    // -- 9605: Health check completed --

    private static readonly Action<ILogger, string, string, Exception?> HealthCheckCompletedDef =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(9605, nameof(HealthCheckCompleted)),
            "Attestation health check completed. Status={Status}, Provider={Provider}");

    internal static void HealthCheckCompleted(ILogger logger, string status, string provider)
        => HealthCheckCompletedDef(logger, status, provider, null);
}
