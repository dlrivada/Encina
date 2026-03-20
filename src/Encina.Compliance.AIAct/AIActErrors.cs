using Encina.Compliance.AIAct.Model;

namespace Encina.Compliance.AIAct;

/// <summary>
/// Factory methods for AI Act compliance-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>aiact.{category}</c>.
/// All errors include structured metadata with EU AI Act article references for observability.
/// </para>
/// <para>
/// Relevant EU AI Act articles:
/// Article 5 — Prohibited AI practices.
/// Article 6 — Classification rules for high-risk AI systems.
/// Article 13 — Transparency and provision of information to deployers.
/// Article 14 — Human oversight.
/// Article 50 — Transparency obligations for certain AI systems.
/// </para>
/// </remarks>
public static class AIActErrors
{
    private const string MetadataKeyRequestType = "requestType";
    private const string MetadataKeySystemId = "systemId";
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageCompliance = "aiact_compliance";

    // --- Error codes ---

    /// <summary>Error code when a prohibited AI practice is detected (Art. 5).</summary>
    public const string ProhibitedUseCode = "aiact.prohibited_use";

    /// <summary>Error code when AI Act compliance validation fails.</summary>
    public const string ComplianceValidationFailedCode = "aiact.compliance_validation_failed";

    /// <summary>Error code when human oversight is required but not provided (Art. 14).</summary>
    public const string HumanOversightRequiredCode = "aiact.human_oversight_required";

    /// <summary>Error code when transparency obligations are not met (Art. 13, Art. 50).</summary>
    public const string TransparencyRequiredCode = "aiact.transparency_required";

    /// <summary>Error code when an AI system is not registered in the registry.</summary>
    public const string SystemNotRegisteredCode = "aiact.system_not_registered";

    /// <summary>Error code when the AI Act pipeline blocks a request.</summary>
    public const string PipelineBlockedCode = "aiact.pipeline_blocked";

    /// <summary>Error code when the compliance validator returns an error.</summary>
    public const string ValidatorErrorCode = "aiact.validator_error";

    // --- Prohibited use errors (Article 5) ---

    /// <summary>
    /// Creates an error when a prohibited AI practice is detected.
    /// </summary>
    /// <param name="requestType">The request type that triggered the prohibited use check.</param>
    /// <param name="systemId">The identifier of the AI system.</param>
    /// <param name="violations">The list of Art. 5 violations detected.</param>
    /// <returns>An error indicating prohibited AI practice.</returns>
    /// <remarks>
    /// Per Art. 5, certain AI practices are unconditionally prohibited. Systems classified
    /// as <see cref="AIRiskLevel.Prohibited"/> cannot be deployed regardless of enforcement mode.
    /// </remarks>
    public static EncinaError ProhibitedUse(
        Type requestType,
        string systemId,
        IReadOnlyList<string> violations) =>
        EncinaErrors.Create(
            code: ProhibitedUseCode,
            message: $"Request '{requestType.Name}' uses AI system '{systemId}' which involves prohibited practices "
                + $"under Art. 5: {string.Join("; ", violations)}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeySystemId] = systemId,
                [MetadataKeyStage] = MetadataStageCompliance,
                ["requirement"] = "article_5_prohibited_practices",
                ["violations"] = violations
            });

    // --- Compliance validation errors ---

    /// <summary>
    /// Creates an error when AI Act compliance validation fails with violations.
    /// </summary>
    /// <param name="requestType">The request type that failed compliance validation.</param>
    /// <param name="systemId">The identifier of the AI system.</param>
    /// <param name="riskLevel">The assessed risk level of the system.</param>
    /// <param name="violations">The compliance violations detected.</param>
    /// <returns>An error indicating compliance validation failure.</returns>
    public static EncinaError ComplianceValidationFailed(
        Type requestType,
        string systemId,
        AIRiskLevel riskLevel,
        IReadOnlyList<string> violations) =>
        EncinaErrors.Create(
            code: ComplianceValidationFailedCode,
            message: $"AI Act compliance validation failed for '{requestType.Name}' "
                + $"(system '{systemId}', risk level: {riskLevel}): {string.Join("; ", violations)}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeySystemId] = systemId,
                [MetadataKeyStage] = MetadataStageCompliance,
                ["riskLevel"] = riskLevel.ToString(),
                ["requirement"] = "article_6_classification",
                ["violations"] = violations
            });

    // --- Human oversight errors (Article 14) ---

    /// <summary>
    /// Creates an error when human oversight is required but not provided.
    /// </summary>
    /// <param name="requestType">The request type requiring human oversight.</param>
    /// <param name="systemId">The identifier of the AI system.</param>
    /// <returns>An error indicating human oversight is required.</returns>
    /// <remarks>
    /// Per Art. 14, high-risk AI systems must be designed to allow effective oversight by
    /// natural persons during use. This error indicates the request requires human review
    /// before processing can proceed.
    /// </remarks>
    public static EncinaError HumanOversightRequired(Type requestType, string systemId) =>
        EncinaErrors.Create(
            code: HumanOversightRequiredCode,
            message: $"Request '{requestType.Name}' requires human oversight for AI system '{systemId}' "
                + "under Art. 14. A human decision must be recorded before processing can proceed.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeySystemId] = systemId,
                [MetadataKeyStage] = MetadataStageCompliance,
                ["requirement"] = "article_14_human_oversight"
            });

    // --- Transparency errors (Articles 13, 50) ---

    /// <summary>
    /// Creates an error when transparency obligations are not met.
    /// </summary>
    /// <param name="requestType">The request type with unmet transparency obligations.</param>
    /// <param name="systemId">The identifier of the AI system.</param>
    /// <returns>An error indicating transparency requirements are not satisfied.</returns>
    /// <remarks>
    /// Per Art. 13, high-risk AI systems must be transparent and provide sufficient information
    /// to deployers. Per Art. 50, certain AI systems (chatbots, deepfakes, etc.) must disclose
    /// their AI nature to users.
    /// </remarks>
    public static EncinaError TransparencyRequired(Type requestType, string systemId) =>
        EncinaErrors.Create(
            code: TransparencyRequiredCode,
            message: $"Request '{requestType.Name}' for AI system '{systemId}' has unmet transparency obligations "
                + "under Art. 13/Art. 50.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeySystemId] = systemId,
                [MetadataKeyStage] = MetadataStageCompliance,
                ["requirement"] = "article_13_50_transparency"
            });

    // --- Registry errors ---

    /// <summary>
    /// Creates an error when an AI system is not registered in the registry.
    /// </summary>
    /// <param name="requestType">The request type referencing the unregistered system.</param>
    /// <param name="systemId">The identifier of the unregistered AI system.</param>
    /// <returns>An error indicating the system is not registered.</returns>
    public static EncinaError SystemNotRegistered(Type requestType, string systemId) =>
        EncinaErrors.Create(
            code: SystemNotRegisteredCode,
            message: $"AI system '{systemId}' referenced by '{requestType.Name}' is not registered. "
                + "Register the system via IAISystemRegistry or apply [HighRiskAI] with AutoRegisterFromAttributes enabled.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeySystemId] = systemId,
                [MetadataKeyStage] = MetadataStageCompliance
            });

    // --- Pipeline behavior errors ---

    /// <summary>
    /// Creates an error when the AI Act pipeline behavior blocks a request.
    /// </summary>
    /// <param name="requestType">The type name of the blocked request.</param>
    /// <param name="reason">The reason the request was blocked.</param>
    /// <returns>An error indicating the request was blocked by AI Act enforcement.</returns>
    public static EncinaError PipelineBlocked(string requestType, string reason) =>
        EncinaErrors.Create(
            code: PipelineBlockedCode,
            message: $"Request '{requestType}' blocked by AI Act compliance enforcement: {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType,
                [MetadataKeyStage] = MetadataStageCompliance
            });

    /// <summary>
    /// Creates an error when the compliance validator returns an error.
    /// </summary>
    /// <param name="requestType">The request type whose validation failed.</param>
    /// <param name="innerError">The underlying error from the validator.</param>
    /// <returns>An error wrapping the validator failure.</returns>
    public static EncinaError ValidatorError(Type requestType, EncinaError innerError) =>
        EncinaErrors.Create(
            code: ValidatorErrorCode,
            message: $"AI Act compliance validator failed for '{requestType.Name}': {innerError.Message}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageCompliance,
                ["innerError"] = innerError.Message
            });
}
