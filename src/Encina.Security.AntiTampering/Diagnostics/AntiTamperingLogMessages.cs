using Microsoft.Extensions.Logging;

namespace Encina.Security.AntiTampering.Diagnostics;

/// <summary>
/// High-performance structured log messages for anti-tampering operations using
/// <see cref="LoggerMessageAttribute"/>-generated methods.
/// </summary>
/// <remarks>
/// Uses compile-time source generation for zero-allocation logging when the
/// log level is not enabled. Each method corresponds to a specific anti-tampering event.
/// EventIds are in the 9100–9199 range reserved for anti-tampering diagnostics.
/// </remarks>
internal static partial class AntiTamperingLogMessages
{
    [LoggerMessage(
        EventId = 9100,
        Level = LogLevel.Debug,
        Message = "Signature validation started for {RequestType} (keyId={KeyId})")]
    internal static partial void SignatureValidationStarted(
        ILogger logger, string requestType, string keyId);

    [LoggerMessage(
        EventId = 9101,
        Level = LogLevel.Debug,
        Message = "Signature validation succeeded for keyId={KeyId} in {DurationMs:F2}ms")]
    internal static partial void SignatureValidationSucceeded(
        ILogger logger, string keyId, double durationMs);

    [LoggerMessage(
        EventId = 9102,
        Level = LogLevel.Warning,
        Message = "Signature validation failed for keyId={KeyId}: {Reason} (requestType={RequestType})")]
    internal static partial void SignatureValidationFailed(
        ILogger logger, string keyId, string reason, string requestType);

    [LoggerMessage(
        EventId = 9103,
        Level = LogLevel.Warning,
        Message = "Timestamp expired: {Timestamp} exceeds {ToleranceMinutes}-minute tolerance (serverTime={ServerTime})")]
    internal static partial void TimestampExpired(
        ILogger logger, string timestamp, int toleranceMinutes, string serverTime);

    [LoggerMessage(
        EventId = 9104,
        Level = LogLevel.Warning,
        Message = "Nonce rejected (replay detected): {NoncePrefix}...")]
    internal static partial void NonceRejected(
        ILogger logger, string noncePrefix);

    [LoggerMessage(
        EventId = 9105,
        Level = LogLevel.Error,
        Message = "Signing key not found: keyId={KeyId}")]
    internal static partial void KeyNotFound(
        ILogger logger, string keyId);
}
