namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Factory methods for breach notification–related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>breach.{category}</c>.
/// All errors include structured metadata for observability.
/// </para>
/// <para>
/// Relevant GDPR articles:
/// Article 33(1) — 72-hour notification deadline to supervisory authority.
/// Article 33(3) — Required content of notification (nature, DPO, consequences, measures).
/// Article 33(4) — Phased reporting when full information is not immediately available.
/// Article 33(5) — Documentation of all breaches, effects, and remedial action.
/// Article 34(1) — Communication to data subjects when high risk to rights and freedoms.
/// Article 34(3) — Exemptions from data subject notification.
/// </para>
/// </remarks>
public static class BreachNotificationErrors
{
    private const string MetadataKeyBreachId = "breachId";
    private const string MetadataKeyStage = "breach_notification";

    // --- Error codes ---

    /// <summary>Error code when a breach record is not found.</summary>
    public const string NotFoundCode = "breach.not_found";

    /// <summary>Error code when a breach record already exists with the given ID.</summary>
    public const string AlreadyExistsCode = "breach.already_exists";

    /// <summary>Error code when attempting an action on an already-resolved breach.</summary>
    public const string AlreadyResolvedCode = "breach.already_resolved";

    /// <summary>Error code when breach detection fails.</summary>
    public const string DetectionFailedCode = "breach.detection_failed";

    /// <summary>Error code when a breach notification fails (generic).</summary>
    public const string NotificationFailedCode = "breach.notification_failed";

    /// <summary>Error code when supervisory authority notification fails.</summary>
    public const string AuthorityNotificationFailedCode = "breach.authority_notification_failed";

    /// <summary>Error code when data subject notification fails.</summary>
    public const string SubjectNotificationFailedCode = "breach.subject_notification_failed";

    /// <summary>Error code when the 72-hour notification deadline has expired.</summary>
    public const string DeadlineExpiredCode = "breach.deadline_expired";

    /// <summary>Error code when a store operation fails.</summary>
    public const string StoreErrorCode = "breach.store_error";

    /// <summary>Error code when an invalid parameter is provided.</summary>
    public const string InvalidParameterCode = "breach.invalid_parameter";

    /// <summary>Error code when a detection rule evaluation fails.</summary>
    public const string RuleEvaluationFailedCode = "breach.rule_evaluation_failed";

    /// <summary>Error code when adding a phased report fails.</summary>
    public const string PhasedReportFailedCode = "breach.phased_report_failed";

    /// <summary>Error code when the breach detection pipeline detects a potential breach and blocks the request.</summary>
    public const string BreachDetectedCode = "breach.detected";

    /// <summary>Error code when an invalid exemption is applied to a breach.</summary>
    public const string ExemptionInvalidCode = "breach.exemption_invalid";

    /// <summary>Error code when an invalid state transition is attempted on a breach.</summary>
    public const string InvalidStateTransitionCode = "breach.invalid_state_transition";

    /// <summary>Error code when a breach service operation fails.</summary>
    public const string ServiceErrorCode = "breach.service_error";

    /// <summary>Error code when breach event history is not available.</summary>
    public const string EventHistoryUnavailableCode = "breach.event_history_unavailable";

    // --- Breach record errors ---

    /// <summary>
    /// Creates an error when a breach record is not found.
    /// </summary>
    /// <param name="breachId">The identifier of the breach that was not found.</param>
    /// <returns>An error indicating the breach record was not found.</returns>
    public static EncinaError NotFound(string breachId) =>
        EncinaErrors.Create(
            code: NotFoundCode,
            message: $"Breach record '{breachId}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyBreachId] = breachId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a breach record already exists with the given ID.
    /// </summary>
    /// <param name="breachId">The identifier of the duplicate breach.</param>
    /// <returns>An error indicating the breach record already exists.</returns>
    public static EncinaError AlreadyExists(string breachId) =>
        EncinaErrors.Create(
            code: AlreadyExistsCode,
            message: $"Breach record '{breachId}' already exists.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyBreachId] = breachId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when an action is attempted on an already-resolved breach.
    /// </summary>
    /// <param name="breachId">The identifier of the resolved breach.</param>
    /// <returns>An error indicating the breach has already been resolved.</returns>
    /// <remarks>
    /// Once a breach is resolved, its record is considered closed. Subsequent modifications
    /// (e.g., additional notifications, phased reports) are not permitted.
    /// </remarks>
    public static EncinaError AlreadyResolved(string breachId) =>
        EncinaErrors.Create(
            code: AlreadyResolvedCode,
            message: $"Breach '{breachId}' has already been resolved.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyBreachId] = breachId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Detection errors ---

    /// <summary>
    /// Creates an error when breach detection fails.
    /// </summary>
    /// <param name="reason">Description of why detection failed.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating breach detection failed.</returns>
    /// <remarks>
    /// Per Article 33(1), the controller must become "aware" of breaches. Detection
    /// failures should be investigated promptly to avoid blind spots in breach awareness.
    /// </remarks>
    public static EncinaError DetectionFailed(string reason, Exception? exception = null) =>
        EncinaErrors.Create(
            code: DetectionFailedCode,
            message: $"Breach detection failed: {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_33_1_awareness"
            });

    /// <summary>
    /// Creates an error when a detection rule evaluation fails.
    /// </summary>
    /// <param name="ruleName">The name of the rule that failed.</param>
    /// <param name="reason">Description of why the rule evaluation failed.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating rule evaluation failed.</returns>
    public static EncinaError RuleEvaluationFailed(
        string ruleName,
        string reason,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: RuleEvaluationFailedCode,
            message: $"Detection rule '{ruleName}' evaluation failed: {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["ruleName"] = ruleName,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Notification errors ---

    /// <summary>
    /// Creates a generic notification failure error.
    /// </summary>
    /// <param name="breachId">The identifier of the breach.</param>
    /// <param name="reason">Description of why the notification failed.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating the notification failed.</returns>
    public static EncinaError NotificationFailed(
        string breachId,
        string reason,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: NotificationFailedCode,
            message: $"Breach notification failed for '{breachId}': {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyBreachId] = breachId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when supervisory authority notification fails.
    /// </summary>
    /// <param name="breachId">The identifier of the breach.</param>
    /// <param name="reason">Description of why the authority notification failed.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating authority notification failed.</returns>
    /// <remarks>
    /// Per Article 33(1), the controller must notify the supervisory authority within
    /// 72 hours. Authority notification failures should be retried immediately and the
    /// delay reason documented.
    /// </remarks>
    public static EncinaError AuthorityNotificationFailed(
        string breachId,
        string reason,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: AuthorityNotificationFailedCode,
            message: $"Authority notification failed for breach '{breachId}': {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyBreachId] = breachId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_33_1_authority_notification"
            });

    /// <summary>
    /// Creates an error when data subject notification fails.
    /// </summary>
    /// <param name="breachId">The identifier of the breach.</param>
    /// <param name="reason">Description of why the subject notification failed.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating data subject notification failed.</returns>
    /// <remarks>
    /// Per Article 34(1), the controller must communicate the breach to data subjects
    /// when it is likely to result in a high risk. Subject notification failures should
    /// be retried or alternative communication methods considered (e.g., public communication
    /// per Article 34(3)(c)).
    /// </remarks>
    public static EncinaError SubjectNotificationFailed(
        string breachId,
        string reason,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: SubjectNotificationFailedCode,
            message: $"Data subject notification failed for breach '{breachId}': {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyBreachId] = breachId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_34_1_subject_notification"
            });

    // --- Deadline errors ---

    /// <summary>
    /// Creates an error when the 72-hour notification deadline has expired.
    /// </summary>
    /// <param name="breachId">The identifier of the breach.</param>
    /// <param name="hoursOverdue">The number of hours past the deadline.</param>
    /// <returns>An error indicating the notification deadline has expired.</returns>
    /// <remarks>
    /// Per Article 33(1), "where the notification to the supervisory authority is not
    /// made within 72 hours, it shall be accompanied by reasons for the delay." This
    /// error signals that the deadline has passed and delay justification is required.
    /// </remarks>
    public static EncinaError DeadlineExpired(string breachId, double hoursOverdue) =>
        EncinaErrors.Create(
            code: DeadlineExpiredCode,
            message: $"The 72-hour notification deadline for breach '{breachId}' has expired "
                + $"({hoursOverdue:F1} hours overdue). Delay reasons must be documented per Article 33(1).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyBreachId] = breachId,
                ["hoursOverdue"] = hoursOverdue,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_33_1_72h_deadline"
            });

    // --- Store errors (persistence layer) ---

    /// <summary>
    /// Creates an error when a store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "RecordBreach", "GetBreach", "UpdateBreach").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating a store operation failure.</returns>
    public static EncinaError StoreError(string operation, string message, Exception? exception = null) =>
        EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"Breach store operation '{operation}' failed: {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Validation errors ---

    /// <summary>
    /// Creates an error when an invalid parameter is provided to a breach notification operation.
    /// </summary>
    /// <param name="parameterName">The name of the invalid parameter.</param>
    /// <param name="message">The error message describing why the parameter is invalid.</param>
    /// <returns>An error indicating an invalid parameter was provided.</returns>
    public static EncinaError InvalidParameter(string parameterName, string message) =>
        EncinaErrors.Create(
            code: InvalidParameterCode,
            message: $"Invalid parameter '{parameterName}': {message}",
            details: new Dictionary<string, object?>
            {
                ["parameterName"] = parameterName,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Phased reporting errors ---

    /// <summary>
    /// Creates an error when adding a phased report fails.
    /// </summary>
    /// <param name="breachId">The identifier of the breach.</param>
    /// <param name="reason">Description of why the phased report could not be added.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating phased report submission failed.</returns>
    /// <remarks>
    /// Per Article 33(4), information may be provided in phases "without undue further delay."
    /// Phased report failures should be retried to ensure timely disclosure.
    /// </remarks>
    public static EncinaError PhasedReportFailed(
        string breachId,
        string reason,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: PhasedReportFailedCode,
            message: $"Phased report submission failed for breach '{breachId}': {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyBreachId] = breachId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_33_4_phased_reporting"
            });

    // --- Exemption errors ---

    /// <summary>
    /// Creates an error when an invalid exemption is applied to a breach.
    /// </summary>
    /// <param name="breachId">The identifier of the breach.</param>
    /// <param name="reason">Description of why the exemption is invalid.</param>
    /// <returns>An error indicating the exemption is invalid.</returns>
    /// <remarks>
    /// Per Article 34(3), exemptions from data subject notification require specific
    /// conditions to be met: (a) encryption/pseudonymization rendering data unintelligible,
    /// (b) subsequent measures eliminating high risk, or (c) disproportionate effort
    /// with public communication as alternative.
    /// </remarks>
    public static EncinaError ExemptionInvalid(string breachId, string reason) =>
        EncinaErrors.Create(
            code: ExemptionInvalidCode,
            message: $"Invalid exemption for breach '{breachId}': {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyBreachId] = breachId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_34_3_exemption_conditions"
            });

    // --- Pipeline behavior errors ---

    // --- State transition errors ---

    /// <summary>
    /// Creates an error when an invalid state transition is attempted on a breach aggregate.
    /// </summary>
    /// <param name="breachId">The identifier of the breach.</param>
    /// <param name="operation">The operation that was attempted.</param>
    /// <returns>An error indicating an invalid state transition.</returns>
    public static EncinaError InvalidStateTransition(Guid breachId, string operation) =>
        EncinaErrors.Create(
            code: InvalidStateTransitionCode,
            message: $"Invalid state transition for breach '{breachId}' during operation '{operation}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyBreachId] = breachId.ToString(),
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Service errors ---

    /// <summary>
    /// Creates an error when a breach service operation fails.
    /// </summary>
    /// <param name="operation">The operation that failed (e.g., "RecordBreach", "AssessBreach").</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>An error indicating a service operation failure.</returns>
    public static EncinaError ServiceError(string operation, Exception exception) =>
        EncinaErrors.Create(
            code: ServiceErrorCode,
            message: $"Breach service operation '{operation}' failed: {exception.Message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Event history errors ---

    /// <summary>
    /// Creates an error when breach event history cannot be retrieved.
    /// </summary>
    /// <param name="breachId">The breach identifier.</param>
    /// <returns>An error indicating that event history is not available.</returns>
    public static EncinaError EventHistoryUnavailable(Guid breachId) =>
        EncinaErrors.Create(
            code: EventHistoryUnavailableCode,
            message: $"Event history for breach '{breachId}' is not available. "
                + "Event stream access requires Marten-specific APIs.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyBreachId] = breachId.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Pipeline behavior errors ---

    /// <summary>
    /// Creates an error when the breach detection pipeline behavior detects a potential
    /// breach and blocks the request.
    /// </summary>
    /// <param name="requestType">The type name of the request that triggered the detection.</param>
    /// <param name="ruleNames">Comma-separated names of the rules that detected breaches.</param>
    /// <returns>An error indicating the request was blocked due to breach detection.</returns>
    /// <remarks>
    /// This error is returned in <see cref="Model.BreachDetectionEnforcementMode.Block"/> mode
    /// when one or more detection rules identify a potential breach from the request's
    /// generated <see cref="Model.SecurityEvent"/>.
    /// </remarks>
    public static EncinaError BreachDetected(string requestType, string ruleNames) =>
        EncinaErrors.Create(
            code: BreachDetectedCode,
            message: $"Potential breach detected for request '{requestType}' by rules [{ruleNames}]. "
                + "Request blocked per enforcement mode.",
            details: new Dictionary<string, object?>
            {
                ["requestType"] = requestType,
                ["ruleNames"] = ruleNames,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_33_1_awareness"
            });
}
