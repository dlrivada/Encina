using System.Globalization;

using Encina.Compliance.PrivacyByDesign.Model;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Factory methods for Privacy by Design-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>pbd.{category}</c>.
/// All errors include structured metadata for observability.
/// </para>
/// <para>
/// Relevant GDPR articles:
/// Article 25(1) — Data protection by design.
/// Article 25(2) — Data protection by default.
/// Article 5(1)(b) — Purpose limitation principle.
/// Article 5(1)(c) — Data minimisation principle.
/// Recital 78 — Appropriate technical and organisational measures.
/// </para>
/// </remarks>
public static class PrivacyByDesignErrors
{
    private const string MetadataKeyRequestType = "requestType";
    private const string MetadataKeyStage = "pbd";

    // --- Error codes ---

    /// <summary>Error code when data minimization violations are detected in a request.</summary>
    public const string DataMinimizationViolationCode = "pbd.data_minimization_violation";

    /// <summary>Error code when fields violate purpose limitation constraints.</summary>
    public const string PurposeLimitationViolationCode = "pbd.purpose_limitation_violation";

    /// <summary>Error code when field values deviate from declared privacy defaults.</summary>
    public const string DefaultPrivacyViolationCode = "pbd.default_privacy_violation";

    /// <summary>Error code when the minimization score falls below the configured threshold.</summary>
    public const string MinimizationScoreBelowThresholdCode = "pbd.minimization_score_below_threshold";

    /// <summary>Error code when a requested purpose definition is not found in the registry.</summary>
    public const string PurposeNotFoundCode = "pbd.purpose_not_found";

    /// <summary>Error code when a duplicate purpose name conflicts within the same module scope.</summary>
    public const string DuplicatePurposeCode = "pbd.duplicate_purpose";

    /// <summary>Error code when a purpose definition has expired and is no longer valid.</summary>
    public const string PurposeExpiredCode = "pbd.purpose_expired";

    /// <summary>Error code when a Privacy by Design store operation fails.</summary>
    public const string StoreErrorCode = "pbd.store_error";

    // --- Data minimization errors ---

    /// <summary>
    /// Creates an error when data minimization violations are detected in a request.
    /// </summary>
    /// <param name="requestTypeName">The fully-qualified type name of the request.</param>
    /// <param name="violationCount">The number of violations detected.</param>
    /// <param name="minimizationScore">The computed minimization score (0.0–1.0).</param>
    /// <returns>An error indicating data minimization violations were found.</returns>
    /// <remarks>
    /// Per GDPR Article 5(1)(c), personal data shall be "adequate, relevant and limited
    /// to what is necessary in relation to the purposes for which they are processed."
    /// </remarks>
    public static EncinaError DataMinimizationViolation(
        string requestTypeName,
        int violationCount,
        double minimizationScore) =>
        EncinaErrors.Create(
            code: DataMinimizationViolationCode,
            message: string.Format(
                CultureInfo.InvariantCulture,
                "Request '{0}' has {1} data minimization violation(s) "
                + "with a minimization score of {2:F2}. "
                + "Per Article 5(1)(c), only data necessary for the processing purpose should be collected.",
                requestTypeName, violationCount, minimizationScore),
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestTypeName,
                ["violationCount"] = violationCount,
                ["minimizationScore"] = minimizationScore,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_5_1_c_data_minimisation"
            });

    /// <summary>
    /// Creates an error when fields violate purpose limitation constraints.
    /// </summary>
    /// <param name="requestTypeName">The fully-qualified type name of the request.</param>
    /// <param name="declaredPurpose">The declared processing purpose.</param>
    /// <param name="violatingFields">The fields that violate purpose limitation.</param>
    /// <returns>An error indicating purpose limitation violations were found.</returns>
    /// <remarks>
    /// Per GDPR Article 5(1)(b), personal data shall be "collected for specified, explicit
    /// and legitimate purposes and not further processed in a manner that is incompatible
    /// with those purposes."
    /// </remarks>
    public static EncinaError PurposeLimitationViolation(
        string requestTypeName,
        string declaredPurpose,
        IReadOnlyList<string> violatingFields) =>
        EncinaErrors.Create(
            code: PurposeLimitationViolationCode,
            message: $"Request '{requestTypeName}' has {violatingFields.Count} field(s) that violate "
                + $"purpose limitation for purpose '{declaredPurpose}': [{string.Join(", ", violatingFields)}]. "
                + "Per Article 5(1)(b), data must not be processed incompatibly with the declared purpose.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestTypeName,
                ["declaredPurpose"] = declaredPurpose,
                ["violatingFields"] = violatingFields,
                ["violatingFieldCount"] = violatingFields.Count,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_5_1_b_purpose_limitation"
            });

    /// <summary>
    /// Creates an error when field values deviate from their declared privacy defaults.
    /// </summary>
    /// <param name="requestTypeName">The fully-qualified type name of the request.</param>
    /// <param name="overriddenFieldCount">The number of fields deviating from defaults.</param>
    /// <returns>An error indicating default privacy violations were found.</returns>
    /// <remarks>
    /// Per GDPR Article 25(2), "the controller shall implement appropriate technical and
    /// organisational measures for ensuring that, by default, personal data are not made
    /// accessible without the individual's intervention."
    /// </remarks>
    public static EncinaError DefaultPrivacyViolation(
        string requestTypeName,
        int overriddenFieldCount) =>
        EncinaErrors.Create(
            code: DefaultPrivacyViolationCode,
            message: $"Request '{requestTypeName}' has {overriddenFieldCount} field(s) deviating from "
                + "their declared privacy defaults. "
                + "Per Article 25(2), personal data should not be accessible by default.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestTypeName,
                ["overriddenFieldCount"] = overriddenFieldCount,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_25_2_data_protection_by_default"
            });

    /// <summary>
    /// Creates an error when the minimization score falls below the configured threshold.
    /// </summary>
    /// <param name="requestTypeName">The fully-qualified type name of the request.</param>
    /// <param name="actualScore">The computed minimization score.</param>
    /// <param name="threshold">The configured minimum threshold.</param>
    /// <returns>An error indicating the minimization score is too low.</returns>
    /// <remarks>
    /// The threshold is configured via <c>PrivacyByDesignOptions.MinimizationScoreThreshold</c>.
    /// A lower score means more unnecessary fields have values, indicating poor data minimization.
    /// </remarks>
    public static EncinaError MinimizationScoreBelowThreshold(
        string requestTypeName,
        double actualScore,
        double threshold) =>
        EncinaErrors.Create(
            code: MinimizationScoreBelowThresholdCode,
            message: string.Format(
                CultureInfo.InvariantCulture,
                "Request '{0}' has a minimization score of {1:F2}, "
                + "which is below the configured threshold of {2:F2}. "
                + "Per Article 25(2), only necessary personal data should be processed.",
                requestTypeName, actualScore, threshold),
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestTypeName,
                ["actualScore"] = actualScore,
                ["threshold"] = threshold,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_25_2_data_protection_by_default"
            });

    // --- Purpose registry errors ---

    /// <summary>
    /// Creates an error when a purpose definition is not found in the registry.
    /// </summary>
    /// <param name="purposeName">The name of the purpose that was not found.</param>
    /// <param name="moduleId">The module scope that was searched, or <see langword="null"/> for global.</param>
    /// <returns>An error indicating the purpose was not found.</returns>
    public static EncinaError PurposeNotFound(string purposeName, string? moduleId = null) =>
        EncinaErrors.Create(
            code: PurposeNotFoundCode,
            message: moduleId is not null
                ? $"Purpose '{purposeName}' not found in module '{moduleId}' or global scope."
                : $"Purpose '{purposeName}' not found in global scope.",
            details: new Dictionary<string, object?>
            {
                ["purposeName"] = purposeName,
                ["moduleId"] = moduleId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a purpose name conflicts with an existing definition in the same scope.
    /// </summary>
    /// <param name="purposeName">The conflicting purpose name.</param>
    /// <param name="moduleId">The module scope of the conflict, or <see langword="null"/> for global.</param>
    /// <returns>An error indicating a duplicate purpose name.</returns>
    public static EncinaError DuplicatePurpose(string purposeName, string? moduleId = null) =>
        EncinaErrors.Create(
            code: DuplicatePurposeCode,
            message: moduleId is not null
                ? $"A purpose named '{purposeName}' already exists in module '{moduleId}'."
                : $"A purpose named '{purposeName}' already exists in global scope.",
            details: new Dictionary<string, object?>
            {
                ["purposeName"] = purposeName,
                ["moduleId"] = moduleId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a purpose definition has expired.
    /// </summary>
    /// <param name="purposeName">The name of the expired purpose.</param>
    /// <param name="expiredAtUtc">The UTC timestamp when the purpose expired.</param>
    /// <returns>An error indicating the purpose has expired.</returns>
    public static EncinaError PurposeExpired(string purposeName, DateTimeOffset expiredAtUtc) =>
        EncinaErrors.Create(
            code: PurposeExpiredCode,
            message: $"Purpose '{purposeName}' expired at {expiredAtUtc:O}. "
                + "Processing under an expired purpose is not permitted.",
            details: new Dictionary<string, object?>
            {
                ["purposeName"] = purposeName,
                ["expiredAtUtc"] = expiredAtUtc,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Store errors (persistence layer) ---

    /// <summary>
    /// Creates an error when a Privacy by Design store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "RegisterPurpose", "GetPurpose").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating a store operation failure.</returns>
    public static EncinaError StoreError(
        string operation,
        string message,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"Privacy by Design store operation '{operation}' failed: {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });
}
