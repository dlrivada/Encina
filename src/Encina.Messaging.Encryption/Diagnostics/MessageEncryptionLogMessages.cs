using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Encryption.Diagnostics;

/// <summary>
/// High-performance structured log messages for the Message Encryption module.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 2400-2499 range to avoid collisions with other
/// Encina subsystems. KMS provider satellites use adjacent ranges:
/// Azure Key Vault (2500-2509), AWS KMS (2510-2519), Data Protection (2520-2529).
/// </para>
/// <para>
/// Allocation blocks:
/// <list type="table">
/// <item><term>2400-2409</term><description>Serialize / encrypt operations</description></item>
/// <item><term>2410-2419</term><description>Deserialize / decrypt operations</description></item>
/// <item><term>2420-2429</term><description>Key resolution and rotation</description></item>
/// <item><term>2430-2439</term><description>Health check</description></item>
/// <item><term>2440-2449</term><description>Configuration and lifecycle</description></item>
/// <item><term>2450-2459</term><description>Reserved for future use</description></item>
/// </list>
/// </para>
/// </remarks>
internal static partial class MessageEncryptionLogMessages
{
    // ========================================================================
    // Serialize / encrypt operations (2400-2409)
    // ========================================================================

    /// <summary>Message payload encrypted successfully.</summary>
    [LoggerMessage(
        EventId = 2400,
        Level = LogLevel.Debug,
        Message = "Message payload encrypted. MessageType={MessageType}, KeyId={KeyId}")]
    internal static partial void MessageEncrypted(this ILogger logger, string messageType, string keyId);

    /// <summary>Message encryption failed.</summary>
    [LoggerMessage(
        EventId = 2401,
        Level = LogLevel.Error,
        Message = "Message encryption failed. MessageType={MessageType}, ErrorMessage={ErrorMessage}")]
    internal static partial void EncryptionFailed(this ILogger logger, string messageType, string errorMessage);

    /// <summary>Encryption skipped because it is globally disabled.</summary>
    [LoggerMessage(
        EventId = 2402,
        Level = LogLevel.Trace,
        Message = "Encryption skipped (disabled). MessageType={MessageType}")]
    internal static partial void EncryptionSkippedDisabled(this ILogger logger, string messageType);

    /// <summary>Encryption skipped because the message type does not require it.</summary>
    [LoggerMessage(
        EventId = 2403,
        Level = LogLevel.Trace,
        Message = "Encryption skipped (not required). MessageType={MessageType}")]
    internal static partial void EncryptionSkippedNotRequired(this ILogger logger, string messageType);

    /// <summary>Encryption started for a message type.</summary>
    [LoggerMessage(
        EventId = 2404,
        Level = LogLevel.Debug,
        Message = "Encryption started. MessageType={MessageType}, PayloadSize={PayloadSize}")]
    internal static partial void EncryptionStarted(this ILogger logger, string messageType, int payloadSize);

    /// <summary>Encryption completed with timing information.</summary>
    [LoggerMessage(
        EventId = 2405,
        Level = LogLevel.Debug,
        Message = "Encryption completed. MessageType={MessageType}, KeyId={KeyId}, DurationMs={DurationMs}")]
    internal static partial void EncryptionCompleted(this ILogger logger, string messageType, string keyId, double durationMs);

    // ========================================================================
    // Deserialize / decrypt operations (2410-2419)
    // ========================================================================

    /// <summary>Message payload decrypted successfully.</summary>
    [LoggerMessage(
        EventId = 2410,
        Level = LogLevel.Debug,
        Message = "Message payload decrypted. KeyId={KeyId}")]
    internal static partial void MessageDecrypted(this ILogger logger, string keyId);

    /// <summary>Message decryption failed.</summary>
    [LoggerMessage(
        EventId = 2411,
        Level = LogLevel.Error,
        Message = "Message decryption failed. KeyId={KeyId}, ErrorMessage={ErrorMessage}")]
    internal static partial void DecryptionFailed(this ILogger logger, string keyId, string errorMessage);

    /// <summary>Decryption audit event logged for compliance.</summary>
    [LoggerMessage(
        EventId = 2412,
        Level = LogLevel.Information,
        Message = "Decryption audit. KeyId={KeyId}, MessageType={MessageType}")]
    internal static partial void DecryptionAudit(this ILogger logger, string keyId, string messageType);

    /// <summary>Decryption started for an encrypted payload.</summary>
    [LoggerMessage(
        EventId = 2413,
        Level = LogLevel.Debug,
        Message = "Decryption started. KeyId={KeyId}, Algorithm={Algorithm}")]
    internal static partial void DecryptionStarted(this ILogger logger, string keyId, string algorithm);

    /// <summary>Decryption completed with timing information.</summary>
    [LoggerMessage(
        EventId = 2414,
        Level = LogLevel.Debug,
        Message = "Decryption completed. KeyId={KeyId}, DurationMs={DurationMs}")]
    internal static partial void DecryptionCompleted(this ILogger logger, string keyId, double durationMs);

    /// <summary>Content is not encrypted; passing through to inner serializer.</summary>
    [LoggerMessage(
        EventId = 2415,
        Level = LogLevel.Trace,
        Message = "Content not encrypted, passing through")]
    internal static partial void ContentPassthrough(this ILogger logger);

    // ========================================================================
    // Key resolution and rotation (2420-2429)
    // ========================================================================

    /// <summary>Current key ID resolved from key provider.</summary>
    [LoggerMessage(
        EventId = 2420,
        Level = LogLevel.Debug,
        Message = "Key resolved. KeyId={KeyId}")]
    internal static partial void KeyResolved(this ILogger logger, string keyId);

    /// <summary>Key resolution failed.</summary>
    [LoggerMessage(
        EventId = 2421,
        Level = LogLevel.Error,
        Message = "Key resolution failed. ErrorMessage={ErrorMessage}")]
    internal static partial void KeyResolutionFailed(this ILogger logger, string errorMessage);

    /// <summary>Tenant-specific key resolved.</summary>
    [LoggerMessage(
        EventId = 2422,
        Level = LogLevel.Debug,
        Message = "Tenant key resolved. TenantId={TenantId}, KeyId={KeyId}")]
    internal static partial void TenantKeyResolved(this ILogger logger, string tenantId, string keyId);

    // ========================================================================
    // Health check (2430-2439)
    // ========================================================================

    /// <summary>Health check roundtrip probe started.</summary>
    [LoggerMessage(
        EventId = 2430,
        Level = LogLevel.Debug,
        Message = "Message encryption health check started")]
    internal static partial void HealthCheckStarted(this ILogger logger);

    /// <summary>Health check completed successfully.</summary>
    [LoggerMessage(
        EventId = 2431,
        Level = LogLevel.Debug,
        Message = "Message encryption health check passed. KeyId={KeyId}, Algorithm={Algorithm}")]
    internal static partial void HealthCheckPassed(this ILogger logger, string keyId, string algorithm);

    /// <summary>Health check failed.</summary>
    [LoggerMessage(
        EventId = 2432,
        Level = LogLevel.Warning,
        Message = "Message encryption health check failed. ErrorMessage={ErrorMessage}")]
    internal static partial void HealthCheckFailed(this ILogger logger, string errorMessage);

    /// <summary>Health check failed with an exception.</summary>
    [LoggerMessage(
        EventId = 2433,
        Level = LogLevel.Error,
        Message = "Message encryption health check exception")]
    internal static partial void HealthCheckException(this ILogger logger, Exception exception);

    // ========================================================================
    // Configuration and lifecycle (2440-2449)
    // ========================================================================

    /// <summary>Message encryption services registered.</summary>
    [LoggerMessage(
        EventId = 2440,
        Level = LogLevel.Information,
        Message = "Message encryption registered. Provider={ProviderType}, Tracing={TracingEnabled}, Metrics={MetricsEnabled}")]
    internal static partial void EncryptionRegistered(this ILogger logger, string providerType, bool tracingEnabled, bool metricsEnabled);

    /// <summary>Encryption provider type resolved during initialization.</summary>
    [LoggerMessage(
        EventId = 2441,
        Level = LogLevel.Debug,
        Message = "Encryption provider resolved. ProviderType={ProviderType}")]
    internal static partial void ProviderResolved(this ILogger logger, string providerType);
}
