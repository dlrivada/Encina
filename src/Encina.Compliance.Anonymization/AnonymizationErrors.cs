namespace Encina.Compliance.Anonymization;

/// <summary>
/// Factory methods for Anonymization-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>anonymization.{category}</c>.
/// All errors include structured metadata for observability.
/// </para>
/// <para>
/// Relevant GDPR articles:
/// Article 4(5) — Definition of pseudonymisation.
/// Article 25 — Data protection by design and by default.
/// Article 32 — Security of processing (encryption, pseudonymisation).
/// Article 89 — Safeguards for archiving, research, statistics.
/// EDPB Guidelines 01/2025 on Pseudonymisation.
/// </para>
/// </remarks>
public static class AnonymizationErrors
{
    private const string MetadataKeyFieldName = "fieldName";
    private const string MetadataKeyKeyId = "keyId";
    private const string MetadataKeyToken = "token";
    private const string MetadataKeyTechnique = "technique";
    private const string MetadataKeyStage = "anonymization_processing";

    // --- Error codes ---

    /// <summary>Error code when a cryptographic key is not found.</summary>
    public const string KeyNotFoundCode = "anonymization.key_not_found";

    /// <summary>Error code when key rotation fails.</summary>
    public const string KeyRotationFailedCode = "anonymization.key_rotation_failed";

    /// <summary>Error code when no active key exists.</summary>
    public const string NoActiveKeyCode = "anonymization.no_active_key";

    /// <summary>Error code when encryption fails.</summary>
    public const string EncryptionFailedCode = "anonymization.encryption_failed";

    /// <summary>Error code when decryption fails.</summary>
    public const string DecryptionFailedCode = "anonymization.decryption_failed";

    /// <summary>Error code when a token is not found in the mapping store.</summary>
    public const string TokenNotFoundCode = "anonymization.token_not_found";

    /// <summary>Error code when tokenization fails.</summary>
    public const string TokenizationFailedCode = "anonymization.tokenization_failed";

    /// <summary>Error code when an anonymization technique cannot be applied.</summary>
    public const string TechniqueNotApplicableCode = "anonymization.technique_not_applicable";

    /// <summary>Error code when an anonymization technique is not registered.</summary>
    public const string TechniqueNotRegisteredCode = "anonymization.technique_not_registered";

    /// <summary>Error code when anonymization fails for a field.</summary>
    public const string AnonymizationFailedCode = "anonymization.anonymization_failed";

    /// <summary>Error code when pseudonymization fails.</summary>
    public const string PseudonymizationFailedCode = "anonymization.pseudonymization_failed";

    /// <summary>Error code when depseudonymization fails (e.g., HMAC-based pseudonym).</summary>
    public const string DepseudonymizationFailedCode = "anonymization.depseudonymization_failed";

    /// <summary>Error code when a risk assessment fails.</summary>
    public const string RiskAssessmentFailedCode = "anonymization.risk_assessment_failed";

    /// <summary>Error code when a store operation fails.</summary>
    public const string StoreErrorCode = "anonymization.store_error";

    /// <summary>Error code when an invalid parameter is provided.</summary>
    public const string InvalidParameterCode = "anonymization.invalid_parameter";

    // --- Key management errors ---

    /// <summary>
    /// Creates an error when a cryptographic key is not found.
    /// </summary>
    /// <param name="keyId">The identifier of the key that was not found.</param>
    /// <returns>An error indicating the cryptographic key was not found.</returns>
    /// <remarks>
    /// Per Article 32, controllers must implement appropriate encryption measures.
    /// A missing key prevents pseudonymisation or tokenisation operations from completing.
    /// </remarks>
    public static EncinaError KeyNotFound(string keyId) =>
        EncinaErrors.Create(
            code: KeyNotFoundCode,
            message: $"Cryptographic key '{keyId}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyKeyId] = keyId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when key rotation fails.
    /// </summary>
    /// <param name="keyId">The identifier of the key that failed rotation.</param>
    /// <param name="reason">Description of why the key rotation failed.</param>
    /// <returns>An error indicating key rotation failed.</returns>
    /// <remarks>
    /// Key rotation is a recommended practice under Article 32 to ensure ongoing
    /// security of pseudonymised data. Failure to rotate may indicate a compromised key store.
    /// </remarks>
    public static EncinaError KeyRotationFailed(string keyId, string reason) =>
        EncinaErrors.Create(
            code: KeyRotationFailedCode,
            message: $"Key rotation failed for key '{keyId}': {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyKeyId] = keyId,
                ["reason"] = reason,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when no active cryptographic key exists.
    /// </summary>
    /// <returns>An error indicating no active key is available.</returns>
    /// <remarks>
    /// An active key is required for pseudonymisation and tokenisation operations.
    /// The key provider must be initialized with at least one active key before use.
    /// </remarks>
    public static EncinaError NoActiveKey() =>
        EncinaErrors.Create(
            code: NoActiveKeyCode,
            message: "No active cryptographic key is available. Initialize the key provider first.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when encryption fails.
    /// </summary>
    /// <param name="keyId">The identifier of the key used for the failed encryption.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating the encryption operation failed.</returns>
    /// <remarks>
    /// Per Article 32(1)(a), encryption is listed as an appropriate technical measure
    /// for ensuring the security of processing.
    /// </remarks>
    public static EncinaError EncryptionFailed(string keyId, Exception? exception = null) =>
        EncinaErrors.Create(
            code: EncryptionFailedCode,
            message: $"Encryption failed using key '{keyId}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyKeyId] = keyId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when decryption fails.
    /// </summary>
    /// <param name="keyId">The identifier of the key used for the failed decryption.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating the decryption operation failed.</returns>
    /// <remarks>
    /// Decryption failure may indicate key corruption, tampering, or the use of an incorrect key.
    /// This affects the ability to reverse pseudonymisation as required by Article 4(5).
    /// </remarks>
    public static EncinaError DecryptionFailed(string keyId, Exception? exception = null) =>
        EncinaErrors.Create(
            code: DecryptionFailedCode,
            message: $"Decryption failed using key '{keyId}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyKeyId] = keyId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Tokenization errors ---

    /// <summary>
    /// Creates an error when a token is not found in the mapping store.
    /// </summary>
    /// <param name="token">The token value that was not found.</param>
    /// <returns>An error indicating the token was not found in the mapping store.</returns>
    public static EncinaError TokenNotFound(string token) =>
        EncinaErrors.Create(
            code: TokenNotFoundCode,
            message: $"Token '{token}' was not found in the token mapping store.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyToken] = token,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when tokenization fails.
    /// </summary>
    /// <param name="message">The error message describing the tokenization failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating the tokenization operation failed.</returns>
    public static EncinaError TokenizationFailed(string message, Exception? exception = null) =>
        EncinaErrors.Create(
            code: TokenizationFailedCode,
            message: $"Tokenization failed: {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Anonymization technique errors ---

    /// <summary>
    /// Creates an error when an anonymization technique cannot be applied to a field type.
    /// </summary>
    /// <param name="technique">The anonymization technique that cannot be applied.</param>
    /// <param name="fieldName">The name of the field the technique was applied to.</param>
    /// <param name="valueType">The CLR type of the field value.</param>
    /// <returns>An error indicating the technique is not applicable to the given field type.</returns>
    /// <remarks>
    /// Different anonymization techniques support different data types. For example,
    /// perturbation applies to numeric types, while data masking applies to strings.
    /// </remarks>
    public static EncinaError TechniqueNotApplicable(Model.AnonymizationTechnique technique, string fieldName, Type valueType) =>
        EncinaErrors.Create(
            code: TechniqueNotApplicableCode,
            message: $"Technique '{technique}' cannot be applied to field '{fieldName}' of type '{valueType.Name}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyTechnique] = technique.ToString(),
                [MetadataKeyFieldName] = fieldName,
                ["valueType"] = valueType.FullName,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when an anonymization technique is not registered in DI.
    /// </summary>
    /// <param name="technique">The anonymization technique that is not registered.</param>
    /// <returns>An error indicating no implementation is registered for the technique.</returns>
    /// <remarks>
    /// Register technique implementations via
    /// <c>AddEncinaAnonymization()</c> or by adding custom
    /// <see cref="IAnonymizationTechnique"/> implementations to the service collection.
    /// </remarks>
    public static EncinaError TechniqueNotRegistered(Model.AnonymizationTechnique technique) =>
        EncinaErrors.Create(
            code: TechniqueNotRegisteredCode,
            message: $"No implementation registered for anonymization technique '{technique}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyTechnique] = technique.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Operation failure errors ---

    /// <summary>
    /// Creates an error when anonymization fails for a specific field.
    /// </summary>
    /// <param name="fieldName">The name of the field that failed anonymization.</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating anonymization failed for the specified field.</returns>
    /// <remarks>
    /// Per Article 25, data protection by design requires that anonymization
    /// is applied correctly to all configured fields.
    /// </remarks>
    public static EncinaError AnonymizationFailed(string fieldName, string message, Exception? exception = null) =>
        EncinaErrors.Create(
            code: AnonymizationFailedCode,
            message: $"Anonymization failed for field '{fieldName}': {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyFieldName] = fieldName,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when pseudonymization fails.
    /// </summary>
    /// <param name="message">The error message describing the pseudonymization failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating the pseudonymization operation failed.</returns>
    /// <remarks>
    /// Per Article 4(5), pseudonymisation means processing personal data in such a manner
    /// that the data can no longer be attributed to a specific data subject without the use
    /// of additional information kept separately.
    /// </remarks>
    public static EncinaError PseudonymizationFailed(string message, Exception? exception = null) =>
        EncinaErrors.Create(
            code: PseudonymizationFailedCode,
            message: $"Pseudonymization failed: {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when depseudonymization is not possible.
    /// </summary>
    /// <param name="message">The error message describing why depseudonymization failed.</param>
    /// <returns>An error indicating the depseudonymization operation failed.</returns>
    /// <remarks>
    /// Depseudonymization may fail when using one-way algorithms such as HMAC-SHA256,
    /// which do not support reversal. Only AES-256-GCM pseudonyms can be reversed.
    /// </remarks>
    public static EncinaError DepseudonymizationFailed(string message) =>
        EncinaErrors.Create(
            code: DepseudonymizationFailedCode,
            message: $"Depseudonymization failed: {message}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a risk assessment fails.
    /// </summary>
    /// <param name="message">The error message describing the risk assessment failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating the risk assessment operation failed.</returns>
    /// <remarks>
    /// Risk assessment evaluates re-identification probability using k-anonymity, l-diversity,
    /// and t-closeness metrics as recommended by Article 89 safeguards.
    /// </remarks>
    public static EncinaError RiskAssessmentFailed(string message, Exception? exception = null) =>
        EncinaErrors.Create(
            code: RiskAssessmentFailedCode,
            message: $"Risk assessment failed: {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Store errors (persistence layer) ---

    /// <summary>
    /// Creates an error when a store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "AddEntry", "GetByToken", "Store").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating a store operation failure.</returns>
    public static EncinaError StoreError(string operation, string message, Exception? exception = null) =>
        EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"Anonymization store operation '{operation}' failed: {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Validation errors ---

    /// <summary>
    /// Creates an error when an invalid parameter is provided to an anonymization operation.
    /// </summary>
    /// <param name="parameterName">The name of the invalid parameter.</param>
    /// <param name="message">The error message describing why the parameter is invalid.</param>
    /// <returns>An error indicating an invalid parameter was provided.</returns>
    public static EncinaError InvalidParameter(string parameterName, string message) =>
        EncinaErrors.Create(
            code: InvalidParameterCode,
            message: $"Invalid parameter '{parameterName}': {message}",
            details: new Dictionary<string, object?>
            {
                ["parameterName"] = parameterName,
                [MetadataKeyStage] = MetadataKeyStage
            });
}
