using Encina.Security.Encryption;

namespace Encina.Marten.GDPR;

/// <summary>
/// Factory methods for crypto-shredding-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>crypto.{category}</c>.
/// All errors include structured metadata for observability and debugging.
/// </para>
/// <para>
/// For key-not-found scenarios, use <see cref="EncryptionErrors.KeyNotFound"/> directly
/// to avoid duplicating error codes across the encryption infrastructure.
/// </para>
/// </remarks>
public static class CryptoShreddingErrors
{
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageCryptoShredding = "crypto-shredding";

    /// <summary>Error code when attempting to operate on a forgotten subject.</summary>
    public const string SubjectForgottenCode = "crypto.subject_forgotten";

    /// <summary>Error code when PII field encryption fails during serialization.</summary>
    public const string EncryptionFailedCode = "crypto.encryption_failed";

    /// <summary>Error code when PII field decryption fails during deserialization.</summary>
    public const string DecryptionFailedCode = "crypto.decryption_failed";

    /// <summary>Error code when subject key rotation fails.</summary>
    public const string KeyRotationFailedCode = "crypto.key_rotation_failed";

    /// <summary>Error code when the key store encounters an infrastructure error.</summary>
    public const string KeyStoreErrorCode = "crypto.key_store_error";

    /// <summary>Error code when an invalid or empty subject identifier is provided.</summary>
    public const string InvalidSubjectIdCode = "crypto.invalid_subject_id";

    /// <summary>Error code when attempting to create a key for a subject that already has one.</summary>
    public const string KeyAlreadyExistsCode = "crypto.key_already_exists";

    /// <summary>Error code when crypto-shredding serialization/deserialization fails.</summary>
    public const string SerializationErrorCode = "crypto.serialization_error";

    /// <summary>Error code when <c>[CryptoShredded]</c> attribute is misconfigured.</summary>
    public const string AttributeMisconfiguredCode = "crypto.attribute_misconfigured";

    /// <summary>
    /// Creates an error indicating the subject has already been cryptographically forgotten.
    /// </summary>
    /// <param name="subjectId">The identifier of the forgotten subject.</param>
    /// <returns>An error indicating the subject has been forgotten (GDPR Article 17).</returns>
    public static EncinaError SubjectForgotten(string subjectId) =>
        EncinaErrors.Create(
            code: SubjectForgottenCode,
            message: $"Subject '{subjectId}' has been cryptographically forgotten. PII is permanently unreadable.",
            details: new Dictionary<string, object?>
            {
                ["subjectId"] = subjectId,
                [MetadataKeyStage] = MetadataStageCryptoShredding
            });

    /// <summary>
    /// Creates an error when encryption of a PII field fails during event serialization.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="propertyName">The name of the property that failed to encrypt.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating PII encryption failed.</returns>
    public static EncinaError EncryptionFailed(
        string subjectId,
        string propertyName,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: EncryptionFailedCode,
            message: $"Failed to encrypt PII property '{propertyName}' for subject '{subjectId}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["subjectId"] = subjectId,
                ["propertyName"] = propertyName,
                [MetadataKeyStage] = MetadataStageCryptoShredding
            });

    /// <summary>
    /// Creates an error when decryption of a PII field fails during event deserialization.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="propertyName">The name of the property that failed to decrypt.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating PII decryption failed.</returns>
    public static EncinaError DecryptionFailed(
        string subjectId,
        string propertyName,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: DecryptionFailedCode,
            message: $"Failed to decrypt PII property '{propertyName}' for subject '{subjectId}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["subjectId"] = subjectId,
                ["propertyName"] = propertyName,
                [MetadataKeyStage] = MetadataStageCryptoShredding
            });

    /// <summary>
    /// Creates an error when key rotation fails for a subject.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="exception">The exception that caused the rotation failure, if any.</param>
    /// <returns>An error indicating key rotation failed.</returns>
    public static EncinaError KeyRotationFailed(
        string subjectId,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: KeyRotationFailedCode,
            message: $"Key rotation failed for subject '{subjectId}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["subjectId"] = subjectId,
                [MetadataKeyStage] = MetadataStageCryptoShredding
            });

    /// <summary>
    /// Creates an error when the key store encounters an infrastructure failure.
    /// </summary>
    /// <param name="operation">The operation that failed (e.g., "GetKey", "DeleteKeys").</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating a key store infrastructure failure.</returns>
    public static EncinaError KeyStoreError(
        string operation,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: KeyStoreErrorCode,
            message: $"Key store error during '{operation}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataStageCryptoShredding
            });

    /// <summary>
    /// Creates an error when an invalid or empty subject identifier is provided.
    /// </summary>
    /// <param name="subjectId">The invalid subject identifier that was provided.</param>
    /// <returns>An error indicating the subject identifier is invalid.</returns>
    public static EncinaError InvalidSubjectId(string? subjectId) =>
        EncinaErrors.Create(
            code: InvalidSubjectIdCode,
            message: $"Invalid subject identifier: '{subjectId ?? "(null)"}'. Subject ID must be a non-empty string.",
            details: new Dictionary<string, object?>
            {
                ["subjectId"] = subjectId,
                [MetadataKeyStage] = MetadataStageCryptoShredding
            });

    /// <summary>
    /// Creates an error when attempting to create a key for a subject that already has an active key.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <returns>An error indicating a key already exists for this subject.</returns>
    public static EncinaError KeyAlreadyExists(string subjectId) =>
        EncinaErrors.Create(
            code: KeyAlreadyExistsCode,
            message: $"An active encryption key already exists for subject '{subjectId}'. Use key rotation instead.",
            details: new Dictionary<string, object?>
            {
                ["subjectId"] = subjectId,
                [MetadataKeyStage] = MetadataStageCryptoShredding
            });

    /// <summary>
    /// Creates an error when the crypto-shredding serializer encounters a serialization failure.
    /// </summary>
    /// <param name="eventType">The type of event that failed to serialize/deserialize.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating a serialization failure.</returns>
    public static EncinaError SerializationError(
        Type eventType,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: SerializationErrorCode,
            message: $"Crypto-shredding serialization error for event type '{eventType.Name}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["eventType"] = eventType.FullName,
                [MetadataKeyStage] = MetadataStageCryptoShredding
            });

    /// <summary>
    /// Creates an error when a <c>[CryptoShredded]</c> attribute is misconfigured.
    /// </summary>
    /// <param name="propertyName">The name of the misconfigured property.</param>
    /// <param name="declaringType">The type that declares the misconfigured property.</param>
    /// <param name="reason">Description of the misconfiguration.</param>
    /// <returns>An error indicating attribute misconfiguration.</returns>
    public static EncinaError AttributeMisconfigured(
        string propertyName,
        Type declaringType,
        string reason) =>
        EncinaErrors.Create(
            code: AttributeMisconfiguredCode,
            message: $"[CryptoShredded] attribute on '{declaringType.Name}.{propertyName}' is misconfigured: {reason}",
            details: new Dictionary<string, object?>
            {
                ["propertyName"] = propertyName,
                ["declaringType"] = declaringType.FullName,
                ["reason"] = reason,
                [MetadataKeyStage] = MetadataStageCryptoShredding
            });
}
