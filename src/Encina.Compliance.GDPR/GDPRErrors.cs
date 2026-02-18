namespace Encina.Compliance.GDPR;

/// <summary>
/// Factory methods for GDPR compliance-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes follow the convention <c>gdpr.{category}</c>.
/// All errors include structured metadata for observability.
/// </remarks>
public static class GDPRErrors
{
    private const string MetadataKeyRequestType = "requestType";
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageGDPR = "gdpr_compliance";

    /// <summary>Error code when a processing activity is not registered in the RoPA.</summary>
    public const string UnregisteredActivityCode = "gdpr.unregistered_activity";

    /// <summary>Error code when a request fails GDPR compliance validation.</summary>
    public const string ComplianceValidationFailedCode = "gdpr.compliance_validation_failed";

    /// <summary>Error code when the registry lookup fails.</summary>
    public const string RegistryLookupFailedCode = "gdpr.registry_lookup_failed";

    /// <summary>
    /// Creates an error for unregistered processing activity.
    /// </summary>
    /// <param name="requestType">The request type that has no registered processing activity.</param>
    /// <returns>An error indicating the processing activity is not registered.</returns>
    public static EncinaError UnregisteredActivity(Type requestType) =>
        EncinaErrors.Create(
            code: UnregisteredActivityCode,
            message: $"No processing activity is registered for '{requestType.Name}'. "
                + "All personal data processing must be documented in the Record of Processing Activities (Article 30).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageGDPR,
                ["requirement"] = "article_30_ropa"
            });

    /// <summary>
    /// Creates an error when GDPR compliance validation fails.
    /// </summary>
    /// <param name="requestType">The request type that failed compliance validation.</param>
    /// <param name="errors">The compliance validation errors.</param>
    /// <returns>An error indicating compliance validation failure.</returns>
    public static EncinaError ComplianceValidationFailed(Type requestType, IReadOnlyList<string> errors) =>
        EncinaErrors.Create(
            code: ComplianceValidationFailedCode,
            message: $"GDPR compliance validation failed for '{requestType.Name}': {string.Join("; ", errors)}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageGDPR,
                ["requirement"] = "compliance_validation",
                ["errors"] = errors
            });

    /// <summary>
    /// Creates an error when the registry lookup fails.
    /// </summary>
    /// <param name="requestType">The request type whose registry lookup failed.</param>
    /// <param name="innerError">The underlying error from the registry.</param>
    /// <returns>An error indicating registry lookup failure.</returns>
    public static EncinaError RegistryLookupFailed(Type requestType, EncinaError innerError) =>
        EncinaErrors.Create(
            code: RegistryLookupFailedCode,
            message: $"Failed to look up processing activity for '{requestType.Name}': {innerError.Message}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageGDPR,
                ["requirement"] = "registry_access",
                ["innerError"] = innerError.Message
            });
}
