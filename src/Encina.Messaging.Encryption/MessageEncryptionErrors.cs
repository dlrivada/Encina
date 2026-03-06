namespace Encina.Messaging.Encryption;

/// <summary>
/// Factory methods for message encryption-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>msg_encryption.{category}</c> to avoid collision
/// with <c>encryption.*</c> codes used by field-level encryption in
/// <see cref="Security.Encryption.EncryptionErrors"/>.
/// </para>
/// <para>
/// All errors include structured metadata for observability, debugging, and compliance auditing.
/// </para>
/// </remarks>
public static class MessageEncryptionErrors
{
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageMessageEncryption = "message_encryption";

    /// <summary>Error code when message payload encryption fails.</summary>
    public const string EncryptionFailedCode = "msg_encryption.encryption_failed";

    /// <summary>Error code when message payload decryption fails.</summary>
    public const string DecryptionFailedCode = "msg_encryption.decryption_failed";

    /// <summary>Error code when the requested encryption key is not found.</summary>
    public const string KeyNotFoundCode = "msg_encryption.key_not_found";

    /// <summary>Error code when the encrypted payload format is invalid or corrupted.</summary>
    public const string InvalidPayloadCode = "msg_encryption.invalid_payload";

    /// <summary>Error code when the encrypted payload version is not supported.</summary>
    public const string UnsupportedVersionCode = "msg_encryption.unsupported_version";

    /// <summary>Error code when tenant key resolution fails.</summary>
    public const string TenantKeyResolutionFailedCode = "msg_encryption.tenant_key_resolution_failed";

    /// <summary>Error code when message serialization fails.</summary>
    public const string SerializationFailedCode = "msg_encryption.serialization_failed";

    /// <summary>Error code when message deserialization fails.</summary>
    public const string DeserializationFailedCode = "msg_encryption.deserialization_failed";

    /// <summary>Error code when the encryption provider is unavailable or misconfigured.</summary>
    public const string ProviderUnavailableCode = "msg_encryption.provider_unavailable";

    /// <summary>
    /// Creates an error when message payload encryption fails.
    /// </summary>
    /// <param name="messageType">The type name of the message being encrypted.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating encryption failed.</returns>
    public static EncinaError EncryptionFailed(string? messageType = null, Exception? exception = null) =>
        EncinaErrors.Create(
            code: EncryptionFailedCode,
            message: messageType is not null
                ? $"Message encryption failed for type '{messageType}'."
                : "Message encryption failed.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["messageType"] = messageType,
                [MetadataKeyStage] = MetadataStageMessageEncryption
            });

    /// <summary>
    /// Creates an error when message payload decryption fails.
    /// </summary>
    /// <param name="keyId">The key identifier used for the decryption attempt.</param>
    /// <param name="messageType">The type name of the message being decrypted, if known.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating decryption failed.</returns>
    public static EncinaError DecryptionFailed(
        string keyId,
        string? messageType = null,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: DecryptionFailedCode,
            message: messageType is not null
                ? $"Message decryption failed for type '{messageType}' with key '{keyId}'."
                : $"Message decryption failed with key '{keyId}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["keyId"] = keyId,
                ["messageType"] = messageType,
                [MetadataKeyStage] = MetadataStageMessageEncryption
            });

    /// <summary>
    /// Creates an error when the requested encryption key is not found.
    /// </summary>
    /// <param name="keyId">The key identifier that was not found.</param>
    /// <returns>An error indicating the key was not found.</returns>
    public static EncinaError KeyNotFound(string keyId) =>
        EncinaErrors.Create(
            code: KeyNotFoundCode,
            message: $"Message encryption key '{keyId}' was not found.",
            details: new Dictionary<string, object?>
            {
                ["keyId"] = keyId,
                [MetadataKeyStage] = MetadataStageMessageEncryption
            });

    /// <summary>
    /// Creates an error when the encrypted payload format is invalid or corrupted.
    /// </summary>
    /// <param name="reason">A description of why the payload is invalid.</param>
    /// <returns>An error indicating the payload is invalid.</returns>
    public static EncinaError InvalidPayload(string? reason = null) =>
        EncinaErrors.Create(
            code: InvalidPayloadCode,
            message: reason is not null
                ? $"Invalid encrypted message payload: {reason}."
                : "Invalid or corrupted encrypted message payload.",
            details: new Dictionary<string, object?>
            {
                ["reason"] = reason,
                [MetadataKeyStage] = MetadataStageMessageEncryption
            });

    /// <summary>
    /// Creates an error when the encrypted payload version is not supported.
    /// </summary>
    /// <param name="version">The unsupported version number.</param>
    /// <returns>An error indicating the version is not supported.</returns>
    public static EncinaError UnsupportedVersion(int version) =>
        EncinaErrors.Create(
            code: UnsupportedVersionCode,
            message: $"Encrypted message payload version '{version}' is not supported.",
            details: new Dictionary<string, object?>
            {
                ["version"] = version,
                [MetadataKeyStage] = MetadataStageMessageEncryption
            });

    /// <summary>
    /// Creates an error when tenant key resolution fails.
    /// </summary>
    /// <param name="tenantId">The tenant identifier for which key resolution failed.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating tenant key resolution failed.</returns>
    public static EncinaError TenantKeyResolutionFailed(string tenantId, Exception? exception = null) =>
        EncinaErrors.Create(
            code: TenantKeyResolutionFailedCode,
            message: $"Failed to resolve encryption key for tenant '{tenantId}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["tenantId"] = tenantId,
                [MetadataKeyStage] = MetadataStageMessageEncryption
            });

    /// <summary>
    /// Creates an error when message serialization fails.
    /// </summary>
    /// <param name="messageType">The type name of the message that failed to serialize.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating serialization failed.</returns>
    public static EncinaError SerializationFailed(string? messageType = null, Exception? exception = null) =>
        EncinaErrors.Create(
            code: SerializationFailedCode,
            message: messageType is not null
                ? $"Failed to serialize message of type '{messageType}'."
                : "Failed to serialize message.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["messageType"] = messageType,
                [MetadataKeyStage] = MetadataStageMessageEncryption
            });

    /// <summary>
    /// Creates an error when message deserialization fails.
    /// </summary>
    /// <param name="messageType">The type name of the message that failed to deserialize.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating deserialization failed.</returns>
    public static EncinaError DeserializationFailed(string? messageType = null, Exception? exception = null) =>
        EncinaErrors.Create(
            code: DeserializationFailedCode,
            message: messageType is not null
                ? $"Failed to deserialize message of type '{messageType}'."
                : "Failed to deserialize message.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["messageType"] = messageType,
                [MetadataKeyStage] = MetadataStageMessageEncryption
            });

    /// <summary>
    /// Creates an error when the encryption provider is unavailable or misconfigured.
    /// </summary>
    /// <param name="reason">A description of why the provider is unavailable.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating the provider is unavailable.</returns>
    public static EncinaError ProviderUnavailable(string? reason = null, Exception? exception = null) =>
        EncinaErrors.Create(
            code: ProviderUnavailableCode,
            message: reason is not null
                ? $"Message encryption provider is unavailable: {reason}."
                : "Message encryption provider is unavailable or misconfigured.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["reason"] = reason,
                [MetadataKeyStage] = MetadataStageMessageEncryption
            });
}
