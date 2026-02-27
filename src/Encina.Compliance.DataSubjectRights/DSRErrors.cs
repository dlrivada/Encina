namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Factory methods for Data Subject Rights-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes follow the convention <c>dsr.{category}</c>.
/// All errors include structured metadata for observability.
/// </remarks>
public static class DSRErrors
{
    private const string MetadataKeySubjectId = "subjectId";
    private const string MetadataKeyRequestId = "dsrRequestId";
    private const string MetadataKeyRight = "rightType";
    private const string MetadataKeyStageDSR = "dsr_processing";

    /// <summary>Error code when a DSR request is not found.</summary>
    public const string RequestNotFoundCode = "dsr.request_not_found";

    /// <summary>Error code when a DSR request has already been completed.</summary>
    public const string RequestAlreadyCompletedCode = "dsr.request_already_completed";

    /// <summary>Error code when the data subject's identity has not been verified.</summary>
    public const string IdentityNotVerifiedCode = "dsr.identity_not_verified";

    /// <summary>Error code when a processing restriction is active for the data subject.</summary>
    public const string RestrictionActiveCode = "dsr.restriction_active";

    /// <summary>Error code when a data erasure operation fails.</summary>
    public const string ErasureFailedCode = "dsr.erasure_failed";

    /// <summary>Error code when a data export operation fails.</summary>
    public const string ExportFailedCode = "dsr.export_failed";

    /// <summary>Error code when the requested export format is not supported.</summary>
    public const string FormatNotSupportedCode = "dsr.format_not_supported";

    /// <summary>Error code when the DSR request deadline has expired.</summary>
    public const string DeadlineExpiredCode = "dsr.deadline_expired";

    /// <summary>Error code when an exemption applies to the requested operation.</summary>
    public const string ExemptionAppliesCode = "dsr.exemption_applies";

    /// <summary>Error code when the data subject is not found in the system.</summary>
    public const string SubjectNotFoundCode = "dsr.subject_not_found";

    /// <summary>Error code when the personal data locator fails.</summary>
    public const string LocatorFailedCode = "dsr.locator_failed";

    /// <summary>Error code when a DSR store operation fails.</summary>
    public const string StoreErrorCode = "dsr.store_error";

    /// <summary>Error code when a data rectification operation fails.</summary>
    public const string RectificationFailedCode = "dsr.rectification_failed";

    /// <summary>Error code when an objection to processing is rejected.</summary>
    public const string ObjectionRejectedCode = "dsr.objection_rejected";

    /// <summary>Error code when a DSR request is invalid.</summary>
    public const string InvalidRequestCode = "dsr.invalid_request";

    // --- Request lifecycle errors ---

    /// <summary>
    /// Creates an error when a DSR request is not found.
    /// </summary>
    /// <param name="requestId">The identifier of the request that was not found.</param>
    /// <returns>An error indicating the DSR request was not found.</returns>
    public static EncinaError RequestNotFound(string requestId) =>
        EncinaErrors.Create(
            code: RequestNotFoundCode,
            message: $"DSR request '{requestId}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestId] = requestId,
                [MetadataKeyStageDSR] = MetadataKeyStageDSR
            });

    /// <summary>
    /// Creates an error when a DSR request has already been completed and cannot be modified.
    /// </summary>
    /// <param name="requestId">The identifier of the completed request.</param>
    /// <returns>An error indicating the request is already completed.</returns>
    public static EncinaError RequestAlreadyCompleted(string requestId) =>
        EncinaErrors.Create(
            code: RequestAlreadyCompletedCode,
            message: $"DSR request '{requestId}' has already been completed and cannot be modified.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestId] = requestId,
                [MetadataKeyStageDSR] = MetadataKeyStageDSR
            });

    /// <summary>
    /// Creates an error when the data subject's identity has not been verified.
    /// </summary>
    /// <param name="requestId">The identifier of the request requiring identity verification.</param>
    /// <returns>An error indicating identity verification is required.</returns>
    /// <remarks>
    /// Per Article 12(6), the controller may request additional information necessary to confirm
    /// the identity of the data subject before processing the request.
    /// </remarks>
    public static EncinaError IdentityNotVerified(string requestId) =>
        EncinaErrors.Create(
            code: IdentityNotVerifiedCode,
            message: $"Identity verification is required before processing DSR request '{requestId}'. "
                + "Per Article 12(6), the controller must confirm the identity of the data subject.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestId] = requestId,
                [MetadataKeyStageDSR] = MetadataKeyStageDSR,
                ["requirement"] = "article_12_6_identity_verification"
            });

    /// <summary>
    /// Creates an error when a processing restriction is active for the data subject.
    /// </summary>
    /// <param name="subjectId">The identifier of the restricted data subject.</param>
    /// <returns>An error indicating processing is restricted.</returns>
    /// <remarks>
    /// Per Article 18(2), while restriction is active, data may only be stored — not processed —
    /// except with consent, for legal claims, for protecting rights, or for important public interest.
    /// </remarks>
    public static EncinaError RestrictionActive(string subjectId) =>
        EncinaErrors.Create(
            code: RestrictionActiveCode,
            message: $"Processing is restricted for data subject '{subjectId}'. "
                + "Per Article 18(2), restricted data may only be stored, not processed.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                [MetadataKeyStageDSR] = MetadataKeyStageDSR,
                ["requirement"] = "article_18_2_restriction"
            });

    // --- Operation failure errors ---

    /// <summary>
    /// Creates an error when a data erasure operation fails.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>An error indicating the erasure operation failed.</returns>
    public static EncinaError ErasureFailed(string subjectId, string message) =>
        EncinaErrors.Create(
            code: ErasureFailedCode,
            message: $"Erasure failed for data subject '{subjectId}': {message}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                [MetadataKeyRight] = nameof(DataSubjectRight.Erasure),
                [MetadataKeyStageDSR] = MetadataKeyStageDSR,
                ["requirement"] = "article_17_erasure"
            });

    /// <summary>
    /// Creates an error when a data export operation fails.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="format">The requested export format.</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>An error indicating the export operation failed.</returns>
    public static EncinaError ExportFailed(string subjectId, ExportFormat format, string message) =>
        EncinaErrors.Create(
            code: ExportFailedCode,
            message: $"Data export failed for subject '{subjectId}' in format '{format}': {message}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                [MetadataKeyRight] = nameof(DataSubjectRight.Portability),
                ["format"] = format.ToString(),
                [MetadataKeyStageDSR] = MetadataKeyStageDSR,
                ["requirement"] = "article_20_portability"
            });

    /// <summary>
    /// Creates an error when the requested export format is not supported.
    /// </summary>
    /// <param name="format">The unsupported export format.</param>
    /// <returns>An error indicating the format is not supported.</returns>
    public static EncinaError FormatNotSupported(ExportFormat format) =>
        EncinaErrors.Create(
            code: FormatNotSupportedCode,
            message: $"Export format '{format}' is not supported. No IExportFormatWriter is registered for this format.",
            details: new Dictionary<string, object?>
            {
                ["format"] = format.ToString(),
                [MetadataKeyRight] = nameof(DataSubjectRight.Portability),
                [MetadataKeyStageDSR] = MetadataKeyStageDSR
            });

    /// <summary>
    /// Creates an error when the DSR request deadline has expired.
    /// </summary>
    /// <param name="requestId">The identifier of the expired request.</param>
    /// <param name="deadlineUtc">The deadline that was exceeded.</param>
    /// <returns>An error indicating the deadline has expired.</returns>
    /// <remarks>
    /// Per Article 12(3), the controller must respond within one month of receipt, extendable
    /// by two further months for complex or numerous requests.
    /// </remarks>
    public static EncinaError DeadlineExpired(string requestId, DateTimeOffset deadlineUtc) =>
        EncinaErrors.Create(
            code: DeadlineExpiredCode,
            message: $"DSR request '{requestId}' deadline expired at {deadlineUtc:O}. "
                + "Per Article 12(3), requests must be fulfilled within the statutory timeframe.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestId] = requestId,
                ["deadlineUtc"] = deadlineUtc,
                [MetadataKeyStageDSR] = MetadataKeyStageDSR,
                ["requirement"] = "article_12_3_deadline"
            });

    /// <summary>
    /// Creates an error when an exemption applies to the requested operation.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="exemption">The applicable exemption.</param>
    /// <param name="reason">Description of why the exemption applies.</param>
    /// <returns>An error indicating an exemption prevents the operation.</returns>
    /// <remarks>
    /// Per Article 17(3), erasure may be refused when processing is necessary for the
    /// applicable exemption reason.
    /// </remarks>
    public static EncinaError ExemptionApplies(string subjectId, ErasureExemption exemption, string reason) =>
        EncinaErrors.Create(
            code: ExemptionAppliesCode,
            message: $"Exemption '{exemption}' applies for data subject '{subjectId}': {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                ["exemption"] = exemption.ToString(),
                [MetadataKeyStageDSR] = MetadataKeyStageDSR,
                ["requirement"] = "article_17_3_exemptions"
            });

    /// <summary>
    /// Creates an error when the data subject is not found in the system.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject that was not found.</param>
    /// <returns>An error indicating the subject was not found.</returns>
    public static EncinaError SubjectNotFound(string subjectId) =>
        EncinaErrors.Create(
            code: SubjectNotFoundCode,
            message: $"Data subject '{subjectId}' was not found in the system.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                [MetadataKeyStageDSR] = MetadataKeyStageDSR
            });

    /// <summary>
    /// Creates an error when the personal data locator fails.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>An error indicating the locator failed.</returns>
    public static EncinaError LocatorFailed(string subjectId, string message) =>
        EncinaErrors.Create(
            code: LocatorFailedCode,
            message: $"Failed to locate personal data for subject '{subjectId}': {message}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                [MetadataKeyStageDSR] = MetadataKeyStageDSR
            });

    // --- Store errors (persistence layer) ---

    /// <summary>
    /// Creates an error when a DSR store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "Create", "GetById", "UpdateStatus").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>An error indicating a store operation failure.</returns>
    public static EncinaError StoreError(string operation, string message) =>
        EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"DSR store operation '{operation}' failed: {message}",
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStageDSR] = MetadataKeyStageDSR
            });

    /// <summary>
    /// Creates an error when a data rectification operation fails.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="fieldName">The name of the field that could not be rectified.</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>An error indicating the rectification failed.</returns>
    public static EncinaError RectificationFailed(string subjectId, string fieldName, string message) =>
        EncinaErrors.Create(
            code: RectificationFailedCode,
            message: $"Rectification of field '{fieldName}' failed for data subject '{subjectId}': {message}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                ["fieldName"] = fieldName,
                [MetadataKeyRight] = nameof(DataSubjectRight.Rectification),
                [MetadataKeyStageDSR] = MetadataKeyStageDSR,
                ["requirement"] = "article_16_rectification"
            });

    /// <summary>
    /// Creates an error when an objection to processing is rejected.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="processingPurpose">The processing purpose that was objected to.</param>
    /// <param name="reason">The reason for rejecting the objection.</param>
    /// <returns>An error indicating the objection was rejected.</returns>
    /// <remarks>
    /// Per Article 21(1), the controller may continue processing if it demonstrates compelling
    /// legitimate grounds that override the interests, rights, and freedoms of the data subject.
    /// </remarks>
    public static EncinaError ObjectionRejected(string subjectId, string processingPurpose, string reason) =>
        EncinaErrors.Create(
            code: ObjectionRejectedCode,
            message: $"Objection to processing purpose '{processingPurpose}' rejected for subject '{subjectId}': {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeySubjectId] = subjectId,
                ["processingPurpose"] = processingPurpose,
                [MetadataKeyRight] = nameof(DataSubjectRight.Objection),
                [MetadataKeyStageDSR] = MetadataKeyStageDSR,
                ["requirement"] = "article_21_objection"
            });

    /// <summary>
    /// Creates an error when a DSR request is invalid.
    /// </summary>
    /// <param name="message">Description of why the request is invalid.</param>
    /// <returns>An error indicating the request is invalid.</returns>
    public static EncinaError InvalidRequest(string message) =>
        EncinaErrors.Create(
            code: InvalidRequestCode,
            message: $"Invalid DSR request: {message}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStageDSR] = MetadataKeyStageDSR
            });
}
