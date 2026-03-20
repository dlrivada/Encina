using Encina.Compliance.NIS2.Model;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Factory methods for NIS2 compliance–related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>nis2.{category}</c>.
/// All errors include structured metadata for observability.
/// </para>
/// <para>
/// Relevant NIS2 articles:
/// Article 20 — Governance: management body accountability for cybersecurity measures.
/// Article 21 — Cybersecurity risk-management measures (10 mandatory measures).
/// Article 23 — Reporting obligations: 24h early warning, 72h notification, 1-month final report.
/// Article 34 — Administrative fines: EUR 10M / 2% (essential), EUR 7M / 1.4% (important).
/// </para>
/// </remarks>
public static class NIS2Errors
{
    private const string MetadataKeyStage = "nis2_compliance";

    // --- Error codes ---

    /// <summary>Error code when a compliance validation check fails.</summary>
    public const string ComplianceCheckFailedCode = "nis2.compliance_check_failed";

    /// <summary>Error code when a specific NIS2 measure is not satisfied.</summary>
    public const string MeasureNotSatisfiedCode = "nis2.measure_not_satisfied";

    /// <summary>Error code when MFA is required but not enabled.</summary>
    public const string MFARequiredCode = "nis2.mfa_required";

    /// <summary>Error code when encryption requirements are not met.</summary>
    public const string EncryptionRequiredCode = "nis2.encryption_required";

    /// <summary>Error code when a supplier risk is too high for the operation.</summary>
    public const string SupplierRiskHighCode = "nis2.supplier_risk_high";

    /// <summary>Error code when a notification deadline has been exceeded.</summary>
    public const string DeadlineExceededCode = "nis2.deadline_exceeded";

    /// <summary>Error code when incident reporting fails.</summary>
    public const string IncidentReportFailedCode = "nis2.incident_report_failed";

    /// <summary>Error code when a supply chain check fails.</summary>
    public const string SupplyChainCheckFailedCode = "nis2.supply_chain_check_failed";

    /// <summary>Error code when management accountability information is missing.</summary>
    public const string ManagementAccountabilityMissingCode = "nis2.management_accountability_missing";

    /// <summary>Error code when the NIS2 pipeline behavior blocks a request.</summary>
    public const string PipelineBlockedCode = "nis2.pipeline_blocked";

    /// <summary>Error code when a supplier is not found in the registry.</summary>
    public const string SupplierNotFoundCode = "nis2.supplier_not_found";

    /// <summary>Error code when all notification phases are already complete.</summary>
    public const string AllPhasesCompleteCode = "nis2.all_phases_complete";

    /// <summary>Error code when a measure evaluator fails.</summary>
    public const string MeasureEvaluationFailedCode = "nis2.measure_evaluation_failed";

    // --- Compliance validation errors ---

    /// <summary>
    /// Creates an error when an overall compliance validation check fails.
    /// </summary>
    /// <param name="missingCount">The number of unsatisfied measures.</param>
    /// <param name="exception">The optional inner exception.</param>
    /// <returns>An error indicating the compliance check failed.</returns>
    /// <remarks>Per Art. 21(1), entities must implement all 10 mandatory measures.</remarks>
    public static EncinaError ComplianceCheckFailed(int missingCount, Exception? exception = null) =>
        EncinaErrors.Create(
            code: ComplianceCheckFailedCode,
            message: $"NIS2 compliance check failed: {missingCount} measure(s) not satisfied.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["missingCount"] = missingCount,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_21_2_mandatory_measures"
            });

    /// <summary>
    /// Creates an error when a specific NIS2 measure is not satisfied.
    /// </summary>
    /// <param name="measure">The unsatisfied measure.</param>
    /// <param name="details">Details explaining why the measure is not satisfied.</param>
    /// <returns>An error indicating the measure is not met.</returns>
    public static EncinaError MeasureNotSatisfied(NIS2Measure measure, string details) =>
        EncinaErrors.Create(
            code: MeasureNotSatisfiedCode,
            message: $"NIS2 measure '{measure}' is not satisfied: {details}",
            details: new Dictionary<string, object?>
            {
                ["measure"] = measure.ToString(),
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_21_2"
            });

    /// <summary>
    /// Creates an error when a measure evaluator fails during execution.
    /// </summary>
    /// <param name="measure">The measure whose evaluator failed.</param>
    /// <param name="reason">Description of the failure.</param>
    /// <param name="exception">The optional inner exception.</param>
    /// <returns>An error indicating the evaluator failed.</returns>
    public static EncinaError MeasureEvaluationFailed(
        NIS2Measure measure,
        string reason,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: MeasureEvaluationFailedCode,
            message: $"Evaluation of NIS2 measure '{measure}' failed: {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["measure"] = measure.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- MFA enforcement errors ---

    /// <summary>
    /// Creates an error when MFA is required but not enabled for the current user.
    /// </summary>
    /// <param name="requestType">The type name of the request that requires MFA.</param>
    /// <param name="userId">The identifier of the user lacking MFA, or <c>null</c> if unknown.</param>
    /// <returns>An error indicating MFA is required.</returns>
    /// <remarks>
    /// Per Art. 21(2)(j), entities must implement MFA or continuous authentication solutions
    /// where appropriate. This error is returned when a request decorated with <c>[RequireMFA]</c>
    /// is processed by a user without MFA enabled.
    /// </remarks>
    public static EncinaError MFARequired(string requestType, string? userId = null) =>
        EncinaErrors.Create(
            code: MFARequiredCode,
            message: $"Multi-factor authentication is required for request '{requestType}' but is not enabled"
                + (userId is not null ? $" for user '{userId}'." : "."),
            details: new Dictionary<string, object?>
            {
                ["requestType"] = requestType,
                ["userId"] = userId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_21_2_j_mfa"
            });

    // --- Encryption errors ---

    /// <summary>
    /// Creates an error when encryption requirements are not met.
    /// </summary>
    /// <param name="context">The data category or endpoint that lacks encryption.</param>
    /// <param name="encryptionType">The type of encryption missing (e.g., "at-rest", "in-transit").</param>
    /// <returns>An error indicating encryption is required.</returns>
    /// <remarks>Per Art. 21(2)(h), entities must implement cryptography and encryption policies.</remarks>
    public static EncinaError EncryptionRequired(string context, string encryptionType) =>
        EncinaErrors.Create(
            code: EncryptionRequiredCode,
            message: $"Encryption ({encryptionType}) is required for '{context}' per NIS2 Art. 21(2)(h).",
            details: new Dictionary<string, object?>
            {
                ["context"] = context,
                ["encryptionType"] = encryptionType,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_21_2_h_cryptography"
            });

    // --- Supply chain errors ---

    /// <summary>
    /// Creates an error when a supplier's risk level is too high for the requested operation.
    /// </summary>
    /// <param name="supplierId">The identifier of the high-risk supplier.</param>
    /// <param name="riskLevel">The assessed risk level.</param>
    /// <returns>An error indicating the supplier risk is too high.</returns>
    /// <remarks>Per Art. 21(2)(d), entities must address supply chain security risks.</remarks>
    public static EncinaError SupplierRiskHigh(string supplierId, SupplierRiskLevel riskLevel) =>
        EncinaErrors.Create(
            code: SupplierRiskHighCode,
            message: $"Supplier '{supplierId}' has risk level '{riskLevel}' which exceeds the acceptable threshold.",
            details: new Dictionary<string, object?>
            {
                ["supplierId"] = supplierId,
                ["riskLevel"] = riskLevel.ToString(),
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_21_2_d_supply_chain"
            });

    /// <summary>
    /// Creates an error when a supply chain check fails.
    /// </summary>
    /// <param name="supplierId">The identifier of the supplier.</param>
    /// <param name="reason">Description of the failure.</param>
    /// <param name="exception">The optional inner exception.</param>
    /// <returns>An error indicating the supply chain check failed.</returns>
    public static EncinaError SupplyChainCheckFailed(
        string supplierId,
        string reason,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: SupplyChainCheckFailedCode,
            message: $"Supply chain check failed for supplier '{supplierId}': {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["supplierId"] = supplierId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_21_2_d_supply_chain"
            });

    /// <summary>
    /// Creates an error when a supplier is not found in the configured registry.
    /// </summary>
    /// <param name="supplierId">The identifier of the unknown supplier.</param>
    /// <returns>An error indicating the supplier was not found.</returns>
    public static EncinaError SupplierNotFound(string supplierId) =>
        EncinaErrors.Create(
            code: SupplierNotFoundCode,
            message: $"Supplier '{supplierId}' is not registered in NIS2Options. "
                + "Register suppliers via NIS2Options.AddSupplier() during configuration.",
            details: new Dictionary<string, object?>
            {
                ["supplierId"] = supplierId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Incident / deadline errors ---

    /// <summary>
    /// Creates an error when a notification deadline has been exceeded.
    /// </summary>
    /// <param name="phase">The notification phase whose deadline was exceeded.</param>
    /// <param name="hoursOverdue">The number of hours past the deadline.</param>
    /// <returns>An error indicating the deadline has been exceeded.</returns>
    /// <remarks>
    /// Per Art. 23(4), strict deadlines apply: 24h for early warning, 72h for notification,
    /// 1 month for final report. Exceeding deadlines may result in supervisory action.
    /// </remarks>
    public static EncinaError DeadlineExceeded(NIS2NotificationPhase phase, double hoursOverdue) =>
        EncinaErrors.Create(
            code: DeadlineExceededCode,
            message: $"NIS2 notification deadline for phase '{phase}' has been exceeded "
                + $"by {hoursOverdue:F1} hours.",
            details: new Dictionary<string, object?>
            {
                ["phase"] = phase.ToString(),
                ["hoursOverdue"] = hoursOverdue,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_23_4_notification_timeline"
            });

    /// <summary>
    /// Creates an error when incident reporting fails.
    /// </summary>
    /// <param name="incidentId">The identifier of the incident.</param>
    /// <param name="reason">Description of the failure.</param>
    /// <param name="exception">The optional inner exception.</param>
    /// <returns>An error indicating incident reporting failed.</returns>
    public static EncinaError IncidentReportFailed(
        Guid incidentId,
        string reason,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: IncidentReportFailedCode,
            message: $"NIS2 incident report failed for incident '{incidentId}': {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["incidentId"] = incidentId.ToString(),
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_23_1_reporting"
            });

    /// <summary>
    /// Creates an error when all notification phases for an incident are already complete.
    /// </summary>
    /// <param name="incidentId">The identifier of the incident.</param>
    /// <returns>An error indicating there is no pending notification phase.</returns>
    public static EncinaError AllPhasesComplete(Guid incidentId) =>
        EncinaErrors.Create(
            code: AllPhasesCompleteCode,
            message: $"All notification phases for incident '{incidentId}' have been completed.",
            details: new Dictionary<string, object?>
            {
                ["incidentId"] = incidentId.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Management accountability errors ---

    /// <summary>
    /// Creates an error when management accountability information is missing.
    /// </summary>
    /// <returns>An error indicating management accountability is not configured.</returns>
    /// <remarks>
    /// Per Art. 20(1), management bodies must approve and oversee cybersecurity measures.
    /// Per Art. 20(2), management body members must follow cybersecurity training.
    /// Missing accountability information indicates a governance gap.
    /// </remarks>
    public static EncinaError ManagementAccountabilityMissing() =>
        EncinaErrors.Create(
            code: ManagementAccountabilityMissingCode,
            message: "NIS2 management accountability is not configured. "
                + "Per Art. 20, management body members must approve cybersecurity measures "
                + "and complete cybersecurity training.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_20_governance"
            });

    // --- Pipeline behavior errors ---

    /// <summary>
    /// Creates an error when the NIS2 pipeline behavior blocks a request due to a compliance violation.
    /// </summary>
    /// <param name="requestType">The type name of the blocked request.</param>
    /// <param name="reason">The reason the request was blocked.</param>
    /// <returns>An error indicating the request was blocked by NIS2 enforcement.</returns>
    public static EncinaError PipelineBlocked(string requestType, string reason) =>
        EncinaErrors.Create(
            code: PipelineBlockedCode,
            message: $"Request '{requestType}' blocked by NIS2 compliance enforcement: {reason}",
            details: new Dictionary<string, object?>
            {
                ["requestType"] = requestType,
                [MetadataKeyStage] = MetadataKeyStage
            });
}
