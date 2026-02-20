namespace Encina.Security.Encryption;

/// <summary>
/// Factory methods for encryption-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes follow the convention <c>encryption.{category}</c>.
/// All errors include structured metadata for observability and debugging.
/// </remarks>
public static class EncryptionErrors
{
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageEncryption = "encryption";

    /// <summary>Error code when the requested encryption key is not found.</summary>
    public const string KeyNotFoundCode = "encryption.key_not_found";

    /// <summary>Error code when decryption of a ciphertext fails.</summary>
    public const string DecryptionFailedCode = "encryption.decryption_failed";

    /// <summary>Error code when the ciphertext is invalid or corrupted.</summary>
    public const string InvalidCiphertextCode = "encryption.invalid_ciphertext";

    /// <summary>Error code when the requested encryption algorithm is not supported.</summary>
    public const string AlgorithmNotSupportedCode = "encryption.algorithm_not_supported";

    /// <summary>Error code when key rotation fails.</summary>
    public const string KeyRotationFailedCode = "encryption.key_rotation_failed";

    /// <summary>
    /// Creates an error when an encryption key cannot be found.
    /// </summary>
    /// <param name="keyId">The key identifier that was not found.</param>
    /// <returns>An error indicating the key was not found.</returns>
    public static EncinaError KeyNotFound(string keyId) =>
        EncinaErrors.Create(
            code: KeyNotFoundCode,
            message: $"Encryption key '{keyId}' was not found.",
            details: new Dictionary<string, object?>
            {
                ["keyId"] = keyId,
                [MetadataKeyStage] = MetadataStageEncryption
            });

    /// <summary>
    /// Creates an error when decryption of a value fails.
    /// </summary>
    /// <param name="keyId">The key identifier used for the decryption attempt.</param>
    /// <param name="propertyName">The name of the property being decrypted, if applicable.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating decryption failed.</returns>
    public static EncinaError DecryptionFailed(
        string keyId,
        string? propertyName = null,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: DecryptionFailedCode,
            message: propertyName is not null
                ? $"Decryption failed for property '{propertyName}' with key '{keyId}'."
                : $"Decryption failed with key '{keyId}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["keyId"] = keyId,
                ["propertyName"] = propertyName,
                [MetadataKeyStage] = MetadataStageEncryption
            });

    /// <summary>
    /// Creates an error when the ciphertext is invalid or corrupted.
    /// </summary>
    /// <param name="propertyName">The name of the property with invalid ciphertext, if applicable.</param>
    /// <returns>An error indicating the ciphertext is invalid.</returns>
    public static EncinaError InvalidCiphertext(string? propertyName = null) =>
        EncinaErrors.Create(
            code: InvalidCiphertextCode,
            message: propertyName is not null
                ? $"Invalid or corrupted ciphertext for property '{propertyName}'."
                : "Invalid or corrupted ciphertext.",
            details: new Dictionary<string, object?>
            {
                ["propertyName"] = propertyName,
                [MetadataKeyStage] = MetadataStageEncryption
            });

    /// <summary>
    /// Creates an error when the requested encryption algorithm is not supported.
    /// </summary>
    /// <param name="algorithm">The algorithm that is not supported.</param>
    /// <returns>An error indicating the algorithm is not supported.</returns>
    public static EncinaError AlgorithmNotSupported(EncryptionAlgorithm algorithm) =>
        EncinaErrors.Create(
            code: AlgorithmNotSupportedCode,
            message: $"Encryption algorithm '{algorithm}' is not supported.",
            details: new Dictionary<string, object?>
            {
                ["algorithm"] = algorithm.ToString(),
                [MetadataKeyStage] = MetadataStageEncryption
            });

    /// <summary>
    /// Creates an error when key rotation fails.
    /// </summary>
    /// <param name="exception">The exception that caused the rotation failure, if any.</param>
    /// <returns>An error indicating key rotation failed.</returns>
    public static EncinaError KeyRotationFailed(Exception? exception = null) =>
        EncinaErrors.Create(
            code: KeyRotationFailedCode,
            message: "Encryption key rotation failed.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageEncryption
            });
}
