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
    private const string MetadataKeyBasis = "lawfulBasis";
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageGDPR = "gdpr_compliance";
    private const string MetadataStageLawfulBasis = "lawful_basis";

    /// <summary>Error code when a processing activity is not registered in the RoPA.</summary>
    public const string UnregisteredActivityCode = "gdpr.unregistered_activity";

    /// <summary>Error code when a request fails GDPR compliance validation.</summary>
    public const string ComplianceValidationFailedCode = "gdpr.compliance_validation_failed";

    /// <summary>Error code when the registry lookup fails.</summary>
    public const string RegistryLookupFailedCode = "gdpr.registry_lookup_failed";

    /// <summary>Error code when no lawful basis is declared for a request type.</summary>
    public const string LawfulBasisNotDeclaredCode = "gdpr.lawful_basis_not_declared";

    /// <summary>Error code when consent is required but no consent record was found.</summary>
    public const string ConsentNotFoundCode = "gdpr.consent_not_found";

    /// <summary>Error code when a Legitimate Interest Assessment (LIA) reference is missing.</summary>
    public const string LIANotFoundCode = "gdpr.lia_not_found";

    /// <summary>Error code when a LIA exists but has not been approved.</summary>
    public const string LIANotApprovedCode = "gdpr.lia_not_approved";

    /// <summary>Error code when the consent status provider is not registered but consent basis is declared.</summary>
    public const string ConsentProviderNotRegisteredCode = "gdpr.consent_provider_not_registered";

    /// <summary>Error code when a lawful basis store operation fails.</summary>
    public const string LawfulBasisStoreErrorCode = "gdpr.lawful_basis_store_error";

    /// <summary>Error code when a LIA store operation fails.</summary>
    public const string LIAStoreErrorCode = "gdpr.lia_store_error";

    /// <summary>Error code when a processing activity store operation fails.</summary>
    public const string ProcessingActivityStoreErrorCode = "gdpr.processing_activity_store_error";

    /// <summary>Error code when a duplicate processing activity is detected.</summary>
    public const string ProcessingActivityDuplicateCode = "gdpr.processing_activity_duplicate";

    /// <summary>Error code when a processing activity is not found for update.</summary>
    public const string ProcessingActivityNotFoundCode = "gdpr.processing_activity_not_found";

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

    // --- Lawful Basis errors (Article 6) ---

    /// <summary>
    /// Creates an error when no lawful basis is declared for a request type.
    /// </summary>
    /// <param name="requestType">The request type missing a lawful basis declaration.</param>
    /// <returns>An error indicating no lawful basis is declared.</returns>
    public static EncinaError LawfulBasisNotDeclared(Type requestType) =>
        EncinaErrors.Create(
            code: LawfulBasisNotDeclaredCode,
            message: $"No lawful basis is declared for '{requestType.Name}'. "
                + "All personal data processing must have a lawful basis under Article 6(1).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageLawfulBasis,
                ["requirement"] = "article_6_lawful_basis"
            });

    /// <summary>
    /// Creates an error when consent-based processing has no active consent record.
    /// </summary>
    /// <param name="requestType">The request type requiring consent.</param>
    /// <param name="subjectId">The data subject identifier, if available.</param>
    /// <returns>An error indicating consent was not found.</returns>
    public static EncinaError ConsentNotFound(Type requestType, string? subjectId = null) =>
        EncinaErrors.Create(
            code: ConsentNotFoundCode,
            message: $"No active consent found for '{requestType.Name}'. "
                + "Consent-based processing (Article 6(1)(a)) requires the data subject's active consent.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyBasis] = nameof(LawfulBasis.Consent),
                [MetadataKeyStage] = MetadataStageLawfulBasis,
                ["requirement"] = "article_6_1_a_consent",
                ["subjectId"] = subjectId
            });

    /// <summary>
    /// Creates an error when a Legitimate Interest Assessment reference is missing.
    /// </summary>
    /// <param name="requestType">The request type claiming legitimate interests basis.</param>
    /// <returns>An error indicating the LIA reference is missing.</returns>
    public static EncinaError LIANotFound(Type requestType) =>
        EncinaErrors.Create(
            code: LIANotFoundCode,
            message: $"No Legitimate Interest Assessment (LIA) reference found for '{requestType.Name}'. "
                + "Legitimate interests basis (Article 6(1)(f)) requires a documented balancing test.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyBasis] = nameof(LawfulBasis.LegitimateInterests),
                [MetadataKeyStage] = MetadataStageLawfulBasis,
                ["requirement"] = "article_6_1_f_lia"
            });

    /// <summary>
    /// Creates an error when a LIA exists but has not been approved.
    /// </summary>
    /// <param name="requestType">The request type whose LIA is not approved.</param>
    /// <param name="liaReference">The LIA reference identifier.</param>
    /// <returns>An error indicating the LIA is not approved.</returns>
    public static EncinaError LIANotApproved(Type requestType, string liaReference) =>
        EncinaErrors.Create(
            code: LIANotApprovedCode,
            message: $"Legitimate Interest Assessment '{liaReference}' for '{requestType.Name}' has not been approved. "
                + "Processing cannot proceed until the LIA is reviewed and approved.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyBasis] = nameof(LawfulBasis.LegitimateInterests),
                [MetadataKeyStage] = MetadataStageLawfulBasis,
                ["requirement"] = "article_6_1_f_lia_approval",
                ["liaReference"] = liaReference
            });

    // --- LIA reference-based overloads (used by ILegitimateInterestAssessment) ---

    /// <summary>
    /// Creates an error when no LIA record exists for the given reference identifier.
    /// </summary>
    /// <param name="liaReference">The LIA reference identifier that was not found.</param>
    /// <returns>An error indicating the LIA was not found.</returns>
    /// <remarks>
    /// This overload is used by <see cref="ILegitimateInterestAssessment"/> implementations
    /// that validate LIAs by reference without a request type context.
    /// </remarks>
    public static EncinaError LIANotFound(string liaReference) =>
        EncinaErrors.Create(
            code: LIANotFoundCode,
            message: $"No Legitimate Interest Assessment (LIA) found for reference '{liaReference}'. "
                + "Legitimate interests basis (Article 6(1)(f)) requires a documented balancing test.",
            details: new Dictionary<string, object?>
            {
                ["liaReference"] = liaReference,
                [MetadataKeyBasis] = nameof(LawfulBasis.LegitimateInterests),
                [MetadataKeyStage] = MetadataStageLawfulBasis,
                ["requirement"] = "article_6_1_f_lia"
            });

    /// <summary>
    /// Creates an error when a LIA exists but has not been approved (reference-based).
    /// </summary>
    /// <param name="liaReference">The LIA reference identifier.</param>
    /// <param name="outcome">The current outcome of the LIA.</param>
    /// <returns>An error indicating the LIA is not approved.</returns>
    /// <remarks>
    /// This overload is used by <see cref="ILegitimateInterestAssessment"/> implementations
    /// that validate LIAs by reference without a request type context.
    /// </remarks>
    public static EncinaError LIANotApproved(string liaReference, LIAOutcome outcome) =>
        EncinaErrors.Create(
            code: LIANotApprovedCode,
            message: $"Legitimate Interest Assessment '{liaReference}' has not been approved (current outcome: {outcome}). "
                + "Processing cannot proceed until the LIA is reviewed and approved.",
            details: new Dictionary<string, object?>
            {
                ["liaReference"] = liaReference,
                ["outcome"] = outcome.ToString(),
                [MetadataKeyBasis] = nameof(LawfulBasis.LegitimateInterests),
                [MetadataKeyStage] = MetadataStageLawfulBasis,
                ["requirement"] = "article_6_1_f_lia_approval"
            });

    // --- Store errors (persistence layer) ---

    /// <summary>
    /// Creates an error when a lawful basis store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "Register", "GetAll").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>An error indicating a lawful basis store failure.</returns>
    public static EncinaError LawfulBasisStoreError(string operation, string message) =>
        EncinaErrors.Create(
            code: LawfulBasisStoreErrorCode,
            message: $"Lawful basis store operation '{operation}' failed: {message}",
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataStageLawfulBasis
            });

    /// <summary>
    /// Creates an error when a LIA store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "Store", "GetByReference").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>An error indicating a LIA store failure.</returns>
    public static EncinaError LIAStoreError(string operation, string message) =>
        EncinaErrors.Create(
            code: LIAStoreErrorCode,
            message: $"LIA store operation '{operation}' failed: {message}",
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataStageLawfulBasis
            });

    /// <summary>
    /// Creates an error when the consent status provider is not registered but consent basis is declared.
    /// </summary>
    /// <param name="requestType">The request type declaring consent-based processing.</param>
    /// <returns>An error indicating the consent provider is missing.</returns>
    public static EncinaError ConsentProviderNotRegistered(Type requestType) =>
        EncinaErrors.Create(
            code: ConsentProviderNotRegisteredCode,
            message: $"Consent-based processing is declared for '{requestType.Name}' but no IConsentStatusProvider is registered. "
                + "Register the Encina.Compliance.Consent package or implement IConsentStatusProvider to validate consent status.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyBasis] = nameof(LawfulBasis.Consent),
                [MetadataKeyStage] = MetadataStageLawfulBasis,
                ["requirement"] = "consent_provider_registration"
            });

    // --- Processing Activity store errors (Article 30 persistence) ---

    /// <summary>
    /// Creates an error when a processing activity store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "Register", "GetAll").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>An error indicating a processing activity store failure.</returns>
    public static EncinaError ProcessingActivityStoreError(string operation, string message) =>
        EncinaErrors.Create(
            code: ProcessingActivityStoreErrorCode,
            message: $"Processing activity store operation '{operation}' failed: {message}",
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataStageGDPR
            });

    /// <summary>
    /// Creates an error when a duplicate processing activity is detected during registration.
    /// </summary>
    /// <param name="requestTypeName">The request type name that already has a registered activity.</param>
    /// <returns>An error indicating a duplicate processing activity.</returns>
    public static EncinaError ProcessingActivityDuplicate(string requestTypeName) =>
        EncinaErrors.Create(
            code: ProcessingActivityDuplicateCode,
            message: $"A processing activity is already registered for request type '{requestTypeName}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestTypeName,
                [MetadataKeyStage] = MetadataStageGDPR,
                ["requirement"] = "article_30_ropa"
            });

    /// <summary>
    /// Creates an error when a processing activity is not found for update.
    /// </summary>
    /// <param name="requestTypeName">The request type name that was not found.</param>
    /// <returns>An error indicating the processing activity was not found.</returns>
    public static EncinaError ProcessingActivityNotFound(string requestTypeName) =>
        EncinaErrors.Create(
            code: ProcessingActivityNotFoundCode,
            message: $"No processing activity found for request type '{requestTypeName}'. Cannot update a non-existent record.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestTypeName,
                [MetadataKeyStage] = MetadataStageGDPR,
                ["requirement"] = "article_30_ropa"
            });
}
