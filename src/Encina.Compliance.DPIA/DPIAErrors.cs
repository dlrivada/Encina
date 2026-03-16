namespace Encina.Compliance.DPIA;

/// <summary>
/// Factory methods for DPIA-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>dpia.{category}</c>.
/// All errors include structured metadata for observability.
/// </para>
/// <para>
/// Relevant GDPR articles:
/// Article 35(1) — Obligation to carry out a DPIA for high-risk processing.
/// Article 35(2) — Requirement to seek the DPO's advice.
/// Article 35(7) — Required content of a DPIA.
/// Article 35(11) — Periodic review of assessments.
/// Article 36(1) — Prior consultation with supervisory authority for residual high risk.
/// </para>
/// </remarks>
public static class DPIAErrors
{
    private const string MetadataKeyAssessmentId = "assessmentId";
    private const string MetadataKeyRequestType = "requestType";
    private const string MetadataKeyStage = "dpia";

    // --- Error codes ---

    /// <summary>Error code when a DPIA assessment is required but none exists.</summary>
    public const string AssessmentRequiredCode = "dpia.assessment_required";

    /// <summary>Error code when an existing DPIA assessment has expired and needs re-evaluation.</summary>
    public const string AssessmentExpiredCode = "dpia.assessment_expired";

    /// <summary>Error code when a DPIA assessment has been rejected by the DPO or review process.</summary>
    public const string AssessmentRejectedCode = "dpia.assessment_rejected";

    /// <summary>
    /// Error code when prior consultation with the supervisory authority is required
    /// because residual risk remains high after mitigations.
    /// </summary>
    public const string PriorConsultationRequiredCode = "dpia.prior_consultation_required";

    /// <summary>Error code when DPO consultation is required but has not been completed.</summary>
    public const string DPOConsultationRequiredCode = "dpia.dpo_consultation_required";

    /// <summary>Error code when the assessed risk level exceeds the acceptable threshold.</summary>
    public const string RiskTooHighCode = "dpia.risk_too_high";

    /// <summary>Error code when a DPIA assessment is not found by its identifier.</summary>
    public const string AssessmentNotFoundCode = "dpia.assessment_not_found";

    /// <summary>Error code when a DPIA store operation fails.</summary>
    public const string StoreErrorCode = "dpia.store_error";

    /// <summary>Error code when a requested DPIA template is not found.</summary>
    public const string TemplateNotFoundCode = "dpia.template_not_found";

    // --- Assessment lifecycle errors ---

    /// <summary>
    /// Creates an error when a DPIA assessment is required but none exists for the request type.
    /// </summary>
    /// <param name="requestTypeName">The fully-qualified type name of the request.</param>
    /// <returns>An error indicating a DPIA assessment is required.</returns>
    /// <remarks>
    /// Per GDPR Article 35(1), a DPIA must be carried out before processing that is
    /// "likely to result in a high risk to the rights and freedoms of natural persons."
    /// </remarks>
    public static EncinaError AssessmentRequired(string requestTypeName) =>
        EncinaErrors.Create(
            code: AssessmentRequiredCode,
            message: $"A DPIA assessment is required for '{requestTypeName}' but none exists. "
                + "Per Article 35(1), a DPIA must be completed before processing can proceed.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestTypeName,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_35_1_dpia_required"
            });

    /// <summary>
    /// Creates an error when an existing DPIA assessment has expired and needs re-evaluation.
    /// </summary>
    /// <param name="assessmentId">The unique identifier of the expired assessment.</param>
    /// <param name="requestTypeName">The fully-qualified type name of the request.</param>
    /// <param name="expiredAtUtc">The UTC timestamp when the assessment expired.</param>
    /// <returns>An error indicating the assessment has expired.</returns>
    /// <remarks>
    /// Per GDPR Article 35(11), the controller must review the assessment periodically.
    /// An expired assessment indicates that the review date has passed and re-evaluation
    /// is needed.
    /// </remarks>
    public static EncinaError AssessmentExpired(
        Guid assessmentId,
        string requestTypeName,
        DateTimeOffset expiredAtUtc) =>
        EncinaErrors.Create(
            code: AssessmentExpiredCode,
            message: $"DPIA assessment '{assessmentId}' for '{requestTypeName}' expired at {expiredAtUtc:O}. "
                + "Per Article 35(11), a review must be carried out.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyAssessmentId] = assessmentId,
                [MetadataKeyRequestType] = requestTypeName,
                ["expiredAtUtc"] = expiredAtUtc,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_35_11_periodic_review"
            });

    /// <summary>
    /// Creates an error when a DPIA assessment has been rejected.
    /// </summary>
    /// <param name="assessmentId">The unique identifier of the rejected assessment.</param>
    /// <param name="requestTypeName">The fully-qualified type name of the request.</param>
    /// <returns>An error indicating the assessment was rejected.</returns>
    /// <remarks>
    /// A rejected assessment means the DPO or review process determined that the processing
    /// cannot proceed in its current form. The processing operation must be modified to
    /// address the identified risks before a new assessment can be submitted.
    /// </remarks>
    public static EncinaError AssessmentRejected(Guid assessmentId, string requestTypeName) =>
        EncinaErrors.Create(
            code: AssessmentRejectedCode,
            message: $"DPIA assessment '{assessmentId}' for '{requestTypeName}' has been rejected. "
                + "Processing cannot proceed until a new assessment is approved.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyAssessmentId] = assessmentId,
                [MetadataKeyRequestType] = requestTypeName,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when prior consultation with the supervisory authority is required.
    /// </summary>
    /// <param name="assessmentId">The unique identifier of the assessment.</param>
    /// <param name="requestTypeName">The fully-qualified type name of the request.</param>
    /// <returns>An error indicating prior consultation is required.</returns>
    /// <remarks>
    /// Per GDPR Article 36(1), "the controller shall, prior to processing, consult the
    /// supervisory authority where a data protection impact assessment [...] indicates
    /// that the processing would result in a high risk in the absence of measures taken
    /// by the controller to mitigate the risk." This error signals that residual risk
    /// remains high even after proposed mitigations.
    /// </remarks>
    public static EncinaError PriorConsultationRequired(Guid assessmentId, string requestTypeName) =>
        EncinaErrors.Create(
            code: PriorConsultationRequiredCode,
            message: $"DPIA assessment '{assessmentId}' for '{requestTypeName}' indicates high residual risk. "
                + "Prior consultation with the supervisory authority is required per Article 36(1).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyAssessmentId] = assessmentId,
                [MetadataKeyRequestType] = requestTypeName,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_36_1_prior_consultation"
            });

    /// <summary>
    /// Creates an error when DPO consultation has not been completed for an assessment.
    /// </summary>
    /// <param name="assessmentId">The unique identifier of the assessment.</param>
    /// <returns>An error indicating DPO consultation is required.</returns>
    /// <remarks>
    /// Per GDPR Article 35(2), "the controller shall seek the advice of the data protection
    /// officer, where designated, when carrying out a data protection impact assessment."
    /// </remarks>
    public static EncinaError DPOConsultationRequired(Guid assessmentId) =>
        EncinaErrors.Create(
            code: DPOConsultationRequiredCode,
            message: $"DPO consultation has not been completed for DPIA assessment '{assessmentId}'. "
                + "Per Article 35(2), the DPO must be consulted during the assessment.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyAssessmentId] = assessmentId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_35_2_dpo_consultation"
            });

    /// <summary>
    /// Creates an error when the assessed risk level exceeds the acceptable threshold.
    /// </summary>
    /// <param name="assessmentId">The unique identifier of the assessment.</param>
    /// <param name="requestTypeName">The fully-qualified type name of the request.</param>
    /// <param name="riskLevel">The assessed risk level.</param>
    /// <returns>An error indicating the risk level is too high.</returns>
    public static EncinaError RiskTooHigh(
        Guid assessmentId,
        string requestTypeName,
        Model.RiskLevel riskLevel) =>
        EncinaErrors.Create(
            code: RiskTooHighCode,
            message: $"DPIA assessment '{assessmentId}' for '{requestTypeName}' resulted in risk level '{riskLevel}', "
                + "which exceeds the acceptable threshold. Additional mitigations are required.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyAssessmentId] = assessmentId,
                [MetadataKeyRequestType] = requestTypeName,
                ["riskLevel"] = riskLevel.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Not found errors ---

    /// <summary>
    /// Creates an error when a DPIA assessment is not found by its identifier.
    /// </summary>
    /// <param name="assessmentId">The identifier that was looked up.</param>
    /// <returns>An error indicating the assessment was not found.</returns>
    public static EncinaError AssessmentNotFound(Guid assessmentId) =>
        EncinaErrors.Create(
            code: AssessmentNotFoundCode,
            message: $"DPIA assessment '{assessmentId}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyAssessmentId] = assessmentId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when no DPIA assessment is found for a request type.
    /// </summary>
    /// <param name="requestTypeName">The request type that was looked up.</param>
    /// <returns>An error indicating no assessment was found for the request type.</returns>
    public static EncinaError AssessmentNotFoundByRequestType(string requestTypeName) =>
        EncinaErrors.Create(
            code: AssessmentNotFoundCode,
            message: $"No DPIA assessment found for request type '{requestTypeName}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestTypeName,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Store errors (persistence layer) ---

    /// <summary>
    /// Creates an error when a DPIA store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "SaveAssessment", "GetAssessment").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating a store operation failure.</returns>
    public static EncinaError StoreError(
        string operation,
        string message,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"DPIA store operation '{operation}' failed: {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Template errors ---

    /// <summary>
    /// Creates an error when a DPIA template is not found for the specified processing type.
    /// </summary>
    /// <param name="processingType">The processing type for which no template was found.</param>
    /// <returns>An error indicating the template was not found.</returns>
    /// <remarks>
    /// Templates are optional — an assessment can proceed without a template.
    /// This error is returned by <see cref="IDPIATemplateProvider.GetTemplateAsync"/> when
    /// the caller explicitly requests a template for a processing type that has none configured.
    /// </remarks>
    public static EncinaError TemplateNotFound(string processingType) =>
        EncinaErrors.Create(
            code: TemplateNotFoundCode,
            message: $"No DPIA template found for processing type '{processingType}'.",
            details: new Dictionary<string, object?>
            {
                ["processingType"] = processingType,
                [MetadataKeyStage] = MetadataKeyStage
            });
}
