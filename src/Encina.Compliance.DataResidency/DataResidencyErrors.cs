namespace Encina.Compliance.DataResidency;

/// <summary>
/// Factory methods for Data Residency-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>residency.{category}</c>.
/// All errors include structured metadata for observability.
/// </para>
/// <para>
/// Relevant GDPR articles:
/// Article 44 — General principle for transfers (Chapter V conditions).
/// Article 45 — Transfers on the basis of an adequacy decision.
/// Article 46 — Transfers subject to appropriate safeguards (SCCs, BCRs).
/// Article 49 — Derogations for specific situations.
/// Article 5(2) — Accountability principle (demonstrate compliance).
/// Article 30 — Records of processing activities including transfers.
/// </para>
/// </remarks>
public static class DataResidencyErrors
{
    private const string MetadataKeyDataCategory = "dataCategory";
    private const string MetadataKeyRegionCode = "regionCode";
    private const string MetadataKeySourceRegion = "sourceRegion";
    private const string MetadataKeyDestinationRegion = "destinationRegion";
    private const string MetadataKeyStage = "residency_processing";

    // --- Error codes ---

    /// <summary>Error code when the target region is not allowed for the data category.</summary>
    public const string RegionNotAllowedCode = "residency.region_not_allowed";

    /// <summary>Error code when a cross-border transfer is denied.</summary>
    public const string CrossBorderTransferDeniedCode = "residency.cross_border_denied";

    /// <summary>Error code when the current region could not be resolved.</summary>
    public const string RegionNotResolvedCode = "residency.region_not_resolved";

    /// <summary>Error code when no residency policy is found for a data category.</summary>
    public const string PolicyNotFoundCode = "residency.policy_not_found";

    /// <summary>Error code when a residency policy already exists for the given category.</summary>
    public const string PolicyAlreadyExistsCode = "residency.policy_already_exists";

    /// <summary>Error code when a store operation fails.</summary>
    public const string StoreErrorCode = "residency.store_error";

    /// <summary>Error code when transfer validation fails.</summary>
    public const string TransferValidationFailedCode = "residency.transfer_validation_failed";

    // --- Region errors ---

    /// <summary>
    /// Creates an error when the target region is not allowed for the data category.
    /// </summary>
    /// <param name="dataCategory">The data category being checked.</param>
    /// <param name="regionCode">The region code that is not allowed.</param>
    /// <returns>An error indicating the region is not permitted for the category.</returns>
    /// <remarks>
    /// Per Article 44, data may only be transferred to regions that comply with
    /// Chapter V conditions. This error indicates a policy violation.
    /// </remarks>
    public static EncinaError RegionNotAllowed(string dataCategory, string regionCode) =>
        EncinaErrors.Create(
            code: RegionNotAllowedCode,
            message: $"Region '{regionCode}' is not allowed for data category '{dataCategory}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyDataCategory] = dataCategory,
                [MetadataKeyRegionCode] = regionCode,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_44_general_principle"
            });

    /// <summary>
    /// Creates an error when a cross-border transfer is denied.
    /// </summary>
    /// <param name="source">The source region code.</param>
    /// <param name="destination">The destination region code.</param>
    /// <param name="reason">The reason for the denial.</param>
    /// <returns>An error indicating the transfer was blocked.</returns>
    /// <remarks>
    /// Per GDPR Chapter V (Articles 44-49), cross-border transfers require
    /// an adequacy decision, appropriate safeguards, or a valid derogation.
    /// </remarks>
    public static EncinaError CrossBorderTransferDenied(string source, string destination, string reason) =>
        EncinaErrors.Create(
            code: CrossBorderTransferDeniedCode,
            message: $"Cross-border transfer from '{source}' to '{destination}' denied: {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySourceRegion] = source,
                [MetadataKeyDestinationRegion] = destination,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "chapter_v_international_transfers"
            });

    /// <summary>
    /// Creates an error when the current region could not be resolved.
    /// </summary>
    /// <param name="reason">Description of why the region could not be determined.</param>
    /// <returns>An error indicating the region resolution failed.</returns>
    public static EncinaError RegionNotResolved(string reason) =>
        EncinaErrors.Create(
            code: RegionNotResolvedCode,
            message: $"Could not resolve the current region: {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when no residency policy is found for a data category.
    /// </summary>
    /// <param name="dataCategory">The data category with no policy defined.</param>
    /// <returns>An error indicating no policy exists for the category.</returns>
    /// <remarks>
    /// Per Article 44, controllers should establish explicit residency policies for all
    /// categories of personal data that may be subject to international transfer.
    /// </remarks>
    public static EncinaError PolicyNotFound(string dataCategory) =>
        EncinaErrors.Create(
            code: PolicyNotFoundCode,
            message: $"No residency policy is defined for data category '{dataCategory}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyDataCategory] = dataCategory,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_44_general_principle"
            });

    /// <summary>
    /// Creates an error when a residency policy already exists for the given category.
    /// </summary>
    /// <param name="dataCategory">The data category with an existing policy.</param>
    /// <returns>An error indicating a policy already exists for the category.</returns>
    public static EncinaError PolicyAlreadyExists(string dataCategory) =>
        EncinaErrors.Create(
            code: PolicyAlreadyExistsCode,
            message: $"A residency policy already exists for data category '{dataCategory}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyDataCategory] = dataCategory,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Store errors ---

    /// <summary>
    /// Creates an error when a store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "Record", "GetByEntity").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating a store operation failure.</returns>
    public static EncinaError StoreError(string operation, string message, Exception? exception = null) =>
        EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"Residency store operation '{operation}' failed: {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Transfer validation errors ---

    /// <summary>
    /// Creates an error when transfer validation fails.
    /// </summary>
    /// <param name="reason">Description of why the validation failed.</param>
    /// <returns>An error indicating the transfer validation could not be performed.</returns>
    public static EncinaError TransferValidationFailed(string reason) =>
        EncinaErrors.Create(
            code: TransferValidationFailedCode,
            message: $"Transfer validation failed: {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "chapter_v_international_transfers"
            });
}
