using Microsoft.Extensions.Logging;

namespace Encina.Compliance.Anonymization.Diagnostics;

/// <summary>
/// High-performance structured log messages for the Anonymization module.
/// </summary>
/// <remarks>
/// <para>
/// Uses the <c>[LoggerMessage]</c> source generator for zero-allocation logging in hot paths.
/// Event IDs are allocated in the 8400-8499 range to avoid collisions with other
/// Encina subsystems (GDPR uses 8100-8199, Consent uses 8200-8299, DSR uses 8300-8399).
/// </para>
/// <para>
/// Allocation blocks:
/// <list type="table">
/// <item><term>8400-8409</term><description>Pipeline behavior</description></item>
/// <item><term>8410-8419</term><description>Field transformation</description></item>
/// <item><term>8420-8429</term><description>Enforcement</description></item>
/// <item><term>8430-8439</term><description>Auto-registration</description></item>
/// <item><term>8440-8449</term><description>Health check</description></item>
/// <item><term>8450-8459</term><description>Key management</description></item>
/// <item><term>8460-8469</term><description>Pseudonymization</description></item>
/// <item><term>8470-8479</term><description>Tokenization</description></item>
/// <item><term>8480-8489</term><description>Risk assessment</description></item>
/// <item><term>8490-8499</term><description>Audit trail</description></item>
/// </list>
/// </para>
/// </remarks>
internal static partial class AnonymizationLogMessages
{
    // ========================================================================
    // Pipeline behavior log messages (8400-8409)
    // ========================================================================

    /// <summary>Anonymization pipeline check skipped because enforcement mode is Disabled.</summary>
    [LoggerMessage(
        EventId = 8400,
        Level = LogLevel.Trace,
        Message = "Anonymization pipeline skipped (enforcement disabled). RequestType={RequestType}")]
    internal static partial void AnonymizationPipelineDisabled(this ILogger logger, string requestType);

    /// <summary>Anonymization pipeline skipped because no anonymization attributes found on response type.</summary>
    [LoggerMessage(
        EventId = 8401,
        Level = LogLevel.Trace,
        Message = "Anonymization pipeline skipped (no anonymization attributes on response). RequestType={RequestType}, ResponseType={ResponseType}")]
    internal static partial void AnonymizationPipelineNoAttributes(this ILogger logger, string requestType, string responseType);

    /// <summary>Anonymization pipeline started transformation of response fields.</summary>
    [LoggerMessage(
        EventId = 8402,
        Level = LogLevel.Debug,
        Message = "Anonymization pipeline started. RequestType={RequestType}, ResponseType={ResponseType}, FieldCount={FieldCount}")]
    internal static partial void AnonymizationPipelineStarted(this ILogger logger, string requestType, string responseType, int fieldCount);

    /// <summary>Anonymization pipeline completed all transformations successfully.</summary>
    [LoggerMessage(
        EventId = 8403,
        Level = LogLevel.Debug,
        Message = "Anonymization pipeline completed. RequestType={RequestType}, ResponseType={ResponseType}, FieldsTransformed={FieldsTransformed}")]
    internal static partial void AnonymizationPipelineCompleted(this ILogger logger, string requestType, string responseType, int fieldsTransformed);

    /// <summary>Anonymization pipeline received a handler error and is passing it through.</summary>
    [LoggerMessage(
        EventId = 8404,
        Level = LogLevel.Debug,
        Message = "Anonymization pipeline passing through handler error. RequestType={RequestType}")]
    internal static partial void AnonymizationPipelineHandlerError(this ILogger logger, string requestType);

    // ========================================================================
    // Field transformation log messages (8410-8419)
    // ========================================================================

    /// <summary>Field anonymization completed successfully.</summary>
    [LoggerMessage(
        EventId = 8410,
        Level = LogLevel.Debug,
        Message = "Field anonymized. FieldName={FieldName}, Technique={Technique}, ResponseType={ResponseType}")]
    internal static partial void FieldAnonymized(this ILogger logger, string fieldName, string technique, string responseType);

    /// <summary>Field pseudonymization completed successfully.</summary>
    [LoggerMessage(
        EventId = 8411,
        Level = LogLevel.Debug,
        Message = "Field pseudonymized. FieldName={FieldName}, Algorithm={Algorithm}, ResponseType={ResponseType}")]
    internal static partial void FieldPseudonymized(this ILogger logger, string fieldName, string algorithm, string responseType);

    /// <summary>Field tokenization completed successfully.</summary>
    [LoggerMessage(
        EventId = 8412,
        Level = LogLevel.Debug,
        Message = "Field tokenized. FieldName={FieldName}, Format={Format}, ResponseType={ResponseType}")]
    internal static partial void FieldTokenized(this ILogger logger, string fieldName, string format, string responseType);

    /// <summary>Field transformation failed for a specific field.</summary>
    [LoggerMessage(
        EventId = 8413,
        Level = LogLevel.Warning,
        Message = "Field transformation failed. FieldName={FieldName}, TransformationType={TransformationType}, ResponseType={ResponseType}, ErrorMessage={ErrorMessage}")]
    internal static partial void FieldTransformationFailed(this ILogger logger, string fieldName, string transformationType, string responseType, string errorMessage);

    // ========================================================================
    // Enforcement log messages (8420-8429)
    // ========================================================================

    /// <summary>Transformation failure blocked the response (Block mode).</summary>
    [LoggerMessage(
        EventId = 8420,
        Level = LogLevel.Warning,
        Message = "Anonymization transformation blocked. FieldName={FieldName}, ResponseType={ResponseType}, ErrorMessage={ErrorMessage}")]
    internal static partial void TransformationBlocked(this ILogger logger, string fieldName, string responseType, string errorMessage);

    /// <summary>Transformation failure logged as warning but response allowed (Warn mode).</summary>
    [LoggerMessage(
        EventId = 8421,
        Level = LogLevel.Warning,
        Message = "Anonymization transformation failed — proceeding in Warn mode. FieldName={FieldName}, ResponseType={ResponseType}, ErrorMessage={ErrorMessage}")]
    internal static partial void TransformationWarned(this ILogger logger, string fieldName, string responseType, string errorMessage);

    /// <summary>Unexpected error during pipeline execution.</summary>
    [LoggerMessage(
        EventId = 8422,
        Level = LogLevel.Error,
        Message = "Anonymization pipeline error. RequestType={RequestType}, ResponseType={ResponseType}, ErrorMessage={ErrorMessage}")]
    internal static partial void AnonymizationPipelineError(this ILogger logger, string requestType, string responseType, string errorMessage);

    // ========================================================================
    // Auto-registration log messages (8430-8439)
    // ========================================================================

    /// <summary>Anonymization auto-registration completed.</summary>
    [LoggerMessage(
        EventId = 8430,
        Level = LogLevel.Information,
        Message = "Anonymization auto-registration completed. FieldsDiscovered={FieldsDiscovered}, AssembliesScanned={AssembliesScanned}")]
    internal static partial void AnonymizationAutoRegistrationCompleted(this ILogger logger, int fieldsDiscovered, int assembliesScanned);

    /// <summary>Anonymization auto-registration skipped.</summary>
    [LoggerMessage(
        EventId = 8431,
        Level = LogLevel.Debug,
        Message = "Anonymization auto-registration skipped (no assemblies configured or auto-registration disabled)")]
    internal static partial void AnonymizationAutoRegistrationSkipped(this ILogger logger);

    /// <summary>Anonymization field discovered during auto-registration.</summary>
    [LoggerMessage(
        EventId = 8432,
        Level = LogLevel.Debug,
        Message = "Anonymization field discovered. EntityType={EntityType}, FieldName={FieldName}, TransformationType={TransformationType}")]
    internal static partial void AnonymizationFieldDiscovered(this ILogger logger, string entityType, string fieldName, string transformationType);

    /// <summary>Entity type with anonymization attributes discovered during auto-registration.</summary>
    [LoggerMessage(
        EventId = 8433,
        Level = LogLevel.Debug,
        Message = "Entity with anonymization attributes discovered. EntityType={EntityType}, FieldCount={FieldCount}")]
    internal static partial void AnonymizationEntityDiscovered(this ILogger logger, string entityType, int fieldCount);

    // ========================================================================
    // Health check log messages (8440-8449)
    // ========================================================================

    /// <summary>Anonymization health check completed.</summary>
    [LoggerMessage(
        EventId = 8440,
        Level = LogLevel.Debug,
        Message = "Anonymization health check completed. Status={Status}, RegisteredTechniques={RegisteredTechniques}")]
    internal static partial void AnonymizationHealthCheckCompleted(this ILogger logger, string status, int registeredTechniques);

    // ========================================================================
    // Key management log messages (8450-8459)
    // ========================================================================

    /// <summary>Cryptographic key not found in the key provider.</summary>
    [LoggerMessage(
        EventId = 8450,
        Level = LogLevel.Warning,
        Message = "Cryptographic key not found. KeyId={KeyId}")]
    internal static partial void KeyNotFound(this ILogger logger, string keyId);

    /// <summary>Cryptographic key rotated successfully.</summary>
    [LoggerMessage(
        EventId = 8451,
        Level = LogLevel.Information,
        Message = "Key rotated successfully. KeyId={KeyId}, NewKeyId={NewKeyId}")]
    internal static partial void KeyRotated(this ILogger logger, string keyId, string newKeyId);

    /// <summary>Key rotation failed.</summary>
    [LoggerMessage(
        EventId = 8452,
        Level = LogLevel.Warning,
        Message = "Key rotation failed. KeyId={KeyId}, Reason={Reason}")]
    internal static partial void KeyRotationFailed(this ILogger logger, string keyId, string reason);

    /// <summary>No active cryptographic key available.</summary>
    [LoggerMessage(
        EventId = 8453,
        Level = LogLevel.Warning,
        Message = "No active cryptographic key available")]
    internal static partial void NoActiveKey(this ILogger logger);

    /// <summary>Encryption operation failed.</summary>
    [LoggerMessage(
        EventId = 8454,
        Level = LogLevel.Warning,
        Message = "Encryption failed. KeyId={KeyId}, ErrorMessage={ErrorMessage}")]
    internal static partial void EncryptionFailed(this ILogger logger, string keyId, string errorMessage);

    /// <summary>Decryption operation failed.</summary>
    [LoggerMessage(
        EventId = 8455,
        Level = LogLevel.Warning,
        Message = "Decryption failed. KeyId={KeyId}, ErrorMessage={ErrorMessage}")]
    internal static partial void DecryptionFailed(this ILogger logger, string keyId, string errorMessage);

    // ========================================================================
    // Pseudonymization log messages (8460-8469)
    // ========================================================================

    /// <summary>Pseudonymization operation completed successfully.</summary>
    [LoggerMessage(
        EventId = 8460,
        Level = LogLevel.Debug,
        Message = "Pseudonymization completed. Algorithm={Algorithm}, FieldCount={FieldCount}")]
    internal static partial void PseudonymizationCompleted(this ILogger logger, string algorithm, int fieldCount);

    /// <summary>Pseudonymization operation failed.</summary>
    [LoggerMessage(
        EventId = 8461,
        Level = LogLevel.Warning,
        Message = "Pseudonymization failed. ErrorMessage={ErrorMessage}")]
    internal static partial void PseudonymizationFailed(this ILogger logger, string errorMessage);

    /// <summary>Depseudonymization operation completed successfully.</summary>
    [LoggerMessage(
        EventId = 8462,
        Level = LogLevel.Debug,
        Message = "Depseudonymization completed. KeyId={KeyId}, FieldCount={FieldCount}")]
    internal static partial void DepseudonymizationCompleted(this ILogger logger, string keyId, int fieldCount);

    /// <summary>Depseudonymization operation failed.</summary>
    [LoggerMessage(
        EventId = 8463,
        Level = LogLevel.Warning,
        Message = "Depseudonymization failed. ErrorMessage={ErrorMessage}")]
    internal static partial void DepseudonymizationFailed(this ILogger logger, string errorMessage);

    // ========================================================================
    // Tokenization log messages (8470-8479)
    // ========================================================================

    /// <summary>Tokenization operation completed successfully.</summary>
    [LoggerMessage(
        EventId = 8470,
        Level = LogLevel.Debug,
        Message = "Tokenization completed. TokenFormat={TokenFormat}")]
    internal static partial void TokenizationCompleted(this ILogger logger, string tokenFormat);

    /// <summary>Token not found in the token mapping store.</summary>
    [LoggerMessage(
        EventId = 8471,
        Level = LogLevel.Warning,
        Message = "Token not found. Token={Token}")]
    internal static partial void TokenNotFound(this ILogger logger, string token);

    /// <summary>Detokenization operation completed successfully.</summary>
    [LoggerMessage(
        EventId = 8472,
        Level = LogLevel.Debug,
        Message = "Detokenization completed. Token={Token}")]
    internal static partial void DetokenizationCompleted(this ILogger logger, string token);

    /// <summary>Detokenization operation failed.</summary>
    [LoggerMessage(
        EventId = 8473,
        Level = LogLevel.Warning,
        Message = "Detokenization failed. Token={Token}, ErrorMessage={ErrorMessage}")]
    internal static partial void DetokenizationFailed(this ILogger logger, string token, string errorMessage);

    /// <summary>Tokenization operation failed.</summary>
    [LoggerMessage(
        EventId = 8474,
        Level = LogLevel.Warning,
        Message = "Tokenization failed. ErrorMessage={ErrorMessage}")]
    internal static partial void TokenizationFailed(this ILogger logger, string errorMessage);

    // ========================================================================
    // Risk assessment log messages (8480-8489)
    // ========================================================================

    /// <summary>Risk assessment completed successfully.</summary>
    [LoggerMessage(
        EventId = 8480,
        Level = LogLevel.Information,
        Message = "Risk assessment completed. DatasetSize={DatasetSize}, KAnonymity={KAnonymity}, LDiversity={LDiversity}, IsAcceptable={IsAcceptable}")]
    internal static partial void RiskAssessmentCompleted(this ILogger logger, int datasetSize, int kAnonymity, int lDiversity, bool isAcceptable);

    /// <summary>Risk assessment failed.</summary>
    [LoggerMessage(
        EventId = 8481,
        Level = LogLevel.Warning,
        Message = "Risk assessment failed. DatasetSize={DatasetSize}, ErrorMessage={ErrorMessage}")]
    internal static partial void RiskAssessmentFailed(this ILogger logger, int datasetSize, string errorMessage);

    /// <summary>Re-identification risk threshold exceeded.</summary>
    [LoggerMessage(
        EventId = 8482,
        Level = LogLevel.Warning,
        Message = "Re-identification risk threshold exceeded. Probability={Probability}, KAnonymity={KAnonymity}")]
    internal static partial void RiskThresholdExceeded(this ILogger logger, double probability, int kAnonymity);

    // ========================================================================
    // Audit trail log messages (8490-8499)
    // ========================================================================

    /// <summary>Audit entry recorded successfully.</summary>
    [LoggerMessage(
        EventId = 8490,
        Level = LogLevel.Debug,
        Message = "Anonymization audit entry recorded. Operation={Operation}, SubjectId={SubjectId}")]
    internal static partial void AuditEntryRecorded(this ILogger logger, string operation, string? subjectId);

    /// <summary>Failed to record an audit entry.</summary>
    [LoggerMessage(
        EventId = 8491,
        Level = LogLevel.Warning,
        Message = "Failed to record anonymization audit entry. Operation={Operation}, ErrorMessage={ErrorMessage}")]
    internal static partial void AuditEntryFailed(this ILogger logger, string operation, string errorMessage);
}
