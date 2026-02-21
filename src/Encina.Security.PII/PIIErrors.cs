namespace Encina.Security.PII;

/// <summary>
/// Factory methods for PII masking-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes follow the convention <c>pii.{category}</c>.
/// All errors include structured metadata for observability and debugging.
/// </remarks>
public static class PIIErrors
{
    private const string MetadataKeyStage = "stage";
    private const string MetadataStagePII = "pii_masking";

    /// <summary>Error code when PII masking of a response fails.</summary>
    public const string MaskingFailedCode = "pii.masking_failed";

    /// <summary>Error code when the masking strategy for a PII type cannot be found.</summary>
    public const string StrategyNotFoundCode = "pii.strategy_not_found";

    /// <summary>Error code when serialization/deserialization of an object for masking fails.</summary>
    public const string SerializationFailedCode = "pii.serialization_failed";

    /// <summary>
    /// Creates an error when PII masking fails for a response.
    /// </summary>
    /// <param name="typeName">The name of the type that failed masking.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating masking failed.</returns>
    public static EncinaError MaskingFailed(string typeName, Exception? exception = null) =>
        EncinaErrors.Create(
            code: MaskingFailedCode,
            message: $"PII masking failed for type '{typeName}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["typeName"] = typeName,
                [MetadataKeyStage] = MetadataStagePII
            });

    /// <summary>
    /// Creates an error when a masking strategy cannot be found for the specified PII type.
    /// </summary>
    /// <param name="piiType">The PII type for which no strategy was found.</param>
    /// <param name="fieldName">The name of the field being masked, if applicable.</param>
    /// <returns>An error indicating the strategy was not found.</returns>
    public static EncinaError StrategyNotFound(PIIType piiType, string? fieldName = null) =>
        EncinaErrors.Create(
            code: StrategyNotFoundCode,
            message: fieldName is not null
                ? $"No masking strategy found for PII type '{piiType}' on field '{fieldName}'."
                : $"No masking strategy found for PII type '{piiType}'.",
            details: new Dictionary<string, object?>
            {
                ["piiType"] = piiType.ToString(),
                ["fieldName"] = fieldName,
                [MetadataKeyStage] = MetadataStagePII
            });

    /// <summary>
    /// Creates an error when serialization or deserialization fails during PII masking.
    /// </summary>
    /// <param name="typeName">The name of the type that failed serialization.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    /// <returns>An error indicating serialization failed.</returns>
    public static EncinaError SerializationFailed(string typeName, Exception? exception = null) =>
        EncinaErrors.Create(
            code: SerializationFailedCode,
            message: $"Serialization failed during PII masking for type '{typeName}'.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["typeName"] = typeName,
                [MetadataKeyStage] = MetadataStagePII
            });
}
