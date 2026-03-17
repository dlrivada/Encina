namespace Encina.Compliance.Retention;

/// <summary>
/// Factory methods for Retention-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Error codes follow the convention <c>retention.{category}</c>.
/// All errors include structured metadata for observability.
/// </para>
/// <para>
/// Relevant GDPR articles:
/// Article 5(1)(e) — Storage limitation (data kept no longer than necessary).
/// Article 5(2) — Accountability principle (demonstrate compliance).
/// Article 17(1)(a) — Right to erasure when data no longer necessary.
/// Article 17(3)(e) — Legal claims exemption (legal holds).
/// Recital 39 — Time limits for erasure or periodic review.
/// </para>
/// </remarks>
public static class RetentionErrors
{
    private const string MetadataKeyPolicyId = "policyId";
    private const string MetadataKeyRecordId = "recordId";
    private const string MetadataKeyHoldId = "holdId";
    private const string MetadataKeyEntityId = "entityId";
    private const string MetadataKeyDataCategory = "dataCategory";
    private const string MetadataKeyStage = "retention_processing";

    // --- Error codes ---

    /// <summary>Error code when a retention policy is not found.</summary>
    public const string PolicyNotFoundCode = "retention.policy_not_found";

    /// <summary>Error code when a retention policy already exists for the given category.</summary>
    public const string PolicyAlreadyExistsCode = "retention.policy_already_exists";

    /// <summary>Error code when a retention record is not found.</summary>
    public const string RecordNotFoundCode = "retention.record_not_found";

    /// <summary>Error code when a retention record already exists.</summary>
    public const string RecordAlreadyExistsCode = "retention.record_already_exists";

    /// <summary>Error code when a legal hold is not found.</summary>
    public const string HoldNotFoundCode = "retention.hold_not_found";

    /// <summary>Error code when a legal hold is already active for the entity.</summary>
    public const string HoldAlreadyActiveCode = "retention.hold_already_active";

    /// <summary>Error code when a legal hold has already been released.</summary>
    public const string HoldAlreadyReleasedCode = "retention.hold_already_released";

    /// <summary>Error code when the retention enforcement cycle fails.</summary>
    public const string EnforcementFailedCode = "retention.enforcement_failed";

    /// <summary>Error code when data deletion fails during enforcement.</summary>
    public const string DeletionFailedCode = "retention.deletion_failed";

    /// <summary>Error code when a store operation fails.</summary>
    public const string StoreErrorCode = "retention.store_error";

    /// <summary>Error code when an invalid parameter is provided.</summary>
    public const string InvalidParameterCode = "retention.invalid_parameter";

    /// <summary>Error code when no retention policy exists for the requested data category.</summary>
    public const string NoPolicyForCategoryCode = "retention.no_policy_for_category";

    /// <summary>Error code when the retention pipeline behavior fails to create a retention record.</summary>
    public const string PipelineRecordCreationFailedCode = "retention.pipeline_record_creation_failed";

    /// <summary>Error code when the retention pipeline behavior cannot resolve an entity ID from the response.</summary>
    public const string PipelineEntityIdNotFoundCode = "retention.pipeline_entity_id_not_found";

    /// <summary>Error code when an invalid aggregate state transition is attempted.</summary>
    public const string InvalidStateTransitionCode = "retention.invalid_state_transition";

    /// <summary>Error code when a service operation fails unexpectedly.</summary>
    public const string ServiceErrorCode = "retention.service_error";

    /// <summary>Error code when event history retrieval is not yet available.</summary>
    public const string EventHistoryUnavailableCode = "retention.event_history_unavailable";

    // --- Policy errors ---

    /// <summary>
    /// Creates an error when a retention policy is not found.
    /// </summary>
    /// <param name="policyId">The identifier of the policy that was not found.</param>
    /// <returns>An error indicating the retention policy was not found.</returns>
    public static EncinaError PolicyNotFound(string policyId) =>
        EncinaErrors.Create(
            code: PolicyNotFoundCode,
            message: $"Retention policy '{policyId}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyPolicyId] = policyId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a retention policy already exists for the given category.
    /// </summary>
    /// <param name="dataCategory">The data category with an existing policy.</param>
    /// <returns>An error indicating a policy already exists for the category.</returns>
    /// <remarks>
    /// Each data category should have at most one retention policy to avoid ambiguity
    /// in retention period resolution during enforcement.
    /// </remarks>
    public static EncinaError PolicyAlreadyExists(string dataCategory) =>
        EncinaErrors.Create(
            code: PolicyAlreadyExistsCode,
            message: $"A retention policy already exists for data category '{dataCategory}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyDataCategory] = dataCategory,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Record errors ---

    /// <summary>
    /// Creates an error when a retention record is not found.
    /// </summary>
    /// <param name="recordId">The identifier of the record that was not found.</param>
    /// <returns>An error indicating the retention record was not found.</returns>
    public static EncinaError RecordNotFound(string recordId) =>
        EncinaErrors.Create(
            code: RecordNotFoundCode,
            message: $"Retention record '{recordId}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRecordId] = recordId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a retention record already exists.
    /// </summary>
    /// <param name="recordId">The identifier of the duplicate record.</param>
    /// <returns>An error indicating the retention record already exists.</returns>
    public static EncinaError RecordAlreadyExists(string recordId) =>
        EncinaErrors.Create(
            code: RecordAlreadyExistsCode,
            message: $"Retention record '{recordId}' already exists.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRecordId] = recordId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Legal hold errors ---

    /// <summary>
    /// Creates an error when a legal hold is not found.
    /// </summary>
    /// <param name="holdId">The identifier of the hold that was not found.</param>
    /// <returns>An error indicating the legal hold was not found.</returns>
    public static EncinaError HoldNotFound(string holdId) =>
        EncinaErrors.Create(
            code: HoldNotFoundCode,
            message: $"Legal hold '{holdId}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyHoldId] = holdId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when a legal hold is already active for the entity.
    /// </summary>
    /// <param name="entityId">The identifier of the entity that already has an active hold.</param>
    /// <returns>An error indicating a hold is already active.</returns>
    /// <remarks>
    /// Per GDPR Article 17(3)(e), legal holds suspend deletion for entities under
    /// litigation or legal proceedings. Multiple holds on the same entity may indicate
    /// overlapping legal matters.
    /// </remarks>
    public static EncinaError HoldAlreadyActive(string entityId) =>
        EncinaErrors.Create(
            code: HoldAlreadyActiveCode,
            message: $"An active legal hold already exists for entity '{entityId}'.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyEntityId] = entityId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_17_3_e_legal_claims"
            });

    /// <summary>
    /// Creates an error when a legal hold has already been released.
    /// </summary>
    /// <param name="holdId">The identifier of the hold that was already released.</param>
    /// <returns>An error indicating the hold was already released.</returns>
    public static EncinaError HoldAlreadyReleased(string holdId) =>
        EncinaErrors.Create(
            code: HoldAlreadyReleasedCode,
            message: $"Legal hold '{holdId}' has already been released.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyHoldId] = holdId,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Enforcement errors ---

    /// <summary>
    /// Creates an error when the retention enforcement cycle fails.
    /// </summary>
    /// <param name="reason">Description of why the enforcement cycle failed.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating the enforcement cycle failed.</returns>
    /// <remarks>
    /// Per Article 5(1)(e), automatic enforcement ensures that personal data is not kept
    /// longer than necessary. Enforcement failures should be investigated promptly to
    /// maintain compliance with storage limitation requirements.
    /// </remarks>
    public static EncinaError EnforcementFailed(string reason, Exception? exception = null) =>
        EncinaErrors.Create(
            code: EnforcementFailedCode,
            message: $"Retention enforcement cycle failed: {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_5_1_e_storage_limitation"
            });

    /// <summary>
    /// Creates an error when data deletion fails during enforcement.
    /// </summary>
    /// <param name="entityId">The identifier of the entity that could not be deleted.</param>
    /// <param name="reason">Description of why the deletion failed.</param>
    /// <returns>An error indicating the deletion operation failed.</returns>
    /// <remarks>
    /// Per Article 17(1)(a), the data subject has the right to erasure when data is no longer
    /// necessary. Deletion failures during enforcement should be retried in subsequent cycles.
    /// </remarks>
    public static EncinaError DeletionFailed(string entityId, string reason) =>
        EncinaErrors.Create(
            code: DeletionFailedCode,
            message: $"Deletion failed for entity '{entityId}': {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyEntityId] = entityId,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_17_1_a_no_longer_necessary"
            });

    // --- Store errors (persistence layer) ---

    /// <summary>
    /// Creates an error when a store operation fails.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "Create", "GetById", "UpdateStatus").</param>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating a store operation failure.</returns>
    public static EncinaError StoreError(string operation, string message, Exception? exception = null) =>
        EncinaErrors.Create(
            code: StoreErrorCode,
            message: $"Retention store operation '{operation}' failed: {message}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Validation errors ---

    /// <summary>
    /// Creates an error when an invalid parameter is provided to a retention operation.
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

    /// <summary>
    /// Creates an error when no retention policy exists for the requested data category.
    /// </summary>
    /// <param name="dataCategory">The data category with no retention policy defined.</param>
    /// <returns>An error indicating no policy exists for the category.</returns>
    /// <remarks>
    /// Per Article 5(1)(e), controllers should establish explicit retention periods for all
    /// categories of personal data. A missing policy indicates a gap in compliance coverage
    /// that should be addressed by defining a <see cref="Aggregates.RetentionPolicyAggregate"/> for the category.
    /// </remarks>
    public static EncinaError NoPolicyForCategory(string dataCategory) =>
        EncinaErrors.Create(
            code: NoPolicyForCategoryCode,
            message: $"No retention policy is defined for data category '{dataCategory}'. "
                + "Per Article 5(1)(e), explicit retention periods should be established for all data categories.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyDataCategory] = dataCategory,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_5_1_e_storage_limitation"
            });

    // --- Pipeline behavior errors ---

    /// <summary>
    /// Creates an error when the retention pipeline behavior fails to create a retention record.
    /// </summary>
    /// <param name="dataCategory">The data category of the retention-decorated field.</param>
    /// <param name="reason">Description of why the record could not be created.</param>
    /// <param name="exception">The optional inner exception that caused the failure.</param>
    /// <returns>An error indicating the pipeline failed to create a retention record.</returns>
    /// <remarks>
    /// This error is returned only when <see cref="RetentionEnforcementMode.Block"/> is active.
    /// In <see cref="RetentionEnforcementMode.Warn"/> mode, a warning is logged instead.
    /// </remarks>
    public static EncinaError PipelineRecordCreationFailed(
        string dataCategory,
        string reason,
        Exception? exception = null) =>
        EncinaErrors.Create(
            code: PipelineRecordCreationFailedCode,
            message: $"Retention pipeline failed to create record for category '{dataCategory}': {reason}",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                [MetadataKeyDataCategory] = dataCategory,
                [MetadataKeyStage] = MetadataKeyStage,
                ["requirement"] = "article_5_1_e_storage_limitation"
            });

    // --- State transition errors ---

    /// <summary>
    /// Creates an error when an invalid aggregate state transition is attempted.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="operation">The operation that was attempted.</param>
    /// <returns>An error indicating an invalid state transition.</returns>
    public static EncinaError InvalidStateTransition(Guid aggregateId, string operation) =>
        EncinaErrors.Create(
            code: InvalidStateTransitionCode,
            message: $"Invalid state transition on aggregate '{aggregateId}' during operation '{operation}'.",
            details: new Dictionary<string, object?>
            {
                ["aggregateId"] = aggregateId.ToString(),
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Service errors ---

    /// <summary>
    /// Creates an error when a service operation fails unexpectedly.
    /// </summary>
    /// <param name="operation">The service operation that failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>An error indicating a service operation failure.</returns>
    public static EncinaError ServiceError(string operation, Exception? exception = null) =>
        EncinaErrors.Create(
            code: ServiceErrorCode,
            message: $"Retention service operation '{operation}' failed unexpectedly.",
            exception: exception,
            details: new Dictionary<string, object?>
            {
                ["operation"] = operation,
                [MetadataKeyStage] = MetadataKeyStage
            });

    /// <summary>
    /// Creates an error when event history retrieval is not yet available.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate whose history was requested.</param>
    /// <returns>An error indicating event history is not yet available.</returns>
    public static EncinaError EventHistoryUnavailable(Guid aggregateId) =>
        EncinaErrors.Create(
            code: EventHistoryUnavailableCode,
            message: $"Event history retrieval for aggregate '{aggregateId}' is not yet available. "
                + "This feature requires direct Marten event stream access (Phase 4+).",
            details: new Dictionary<string, object?>
            {
                ["aggregateId"] = aggregateId.ToString(),
                [MetadataKeyStage] = MetadataKeyStage
            });

    // --- Pipeline behavior errors ---

    /// <summary>
    /// Creates an error when the retention pipeline cannot resolve an entity ID from the response.
    /// </summary>
    /// <param name="responseType">The name of the response type that lacks an entity ID property.</param>
    /// <returns>An error indicating the entity ID could not be resolved.</returns>
    /// <remarks>
    /// The pipeline behavior looks for properties named <c>Id</c> or <c>EntityId</c> (case-insensitive)
    /// on the response type to resolve the entity identifier for the retention record.
    /// </remarks>
    public static EncinaError PipelineEntityIdNotFound(string responseType) =>
        EncinaErrors.Create(
            code: PipelineEntityIdNotFoundCode,
            message: $"Could not resolve an entity ID from response type '{responseType}'. "
                + "Ensure the response has a public property named 'Id' or 'EntityId'.",
            details: new Dictionary<string, object?>
            {
                ["responseType"] = responseType,
                [MetadataKeyStage] = MetadataKeyStage
            });
}
