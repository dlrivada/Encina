namespace Encina.Security.ABAC;

/// <summary>
/// Factory methods for ABAC-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes follow the convention <c>abac.{category}</c>, except the invalid decision audit query
/// (<see cref="InvalidDecisionAuditQueryCode"/>, a <c>validation.*</c> code answered with HTTP 400) and the definite authorization
/// denials (<see cref="AccessDeniedCode"/>, <see cref="ConditionNotMetCode"/>,
/// <see cref="ObligationFailedCode"/> and <see cref="RequiredPolicyNotFoundCode"/>) and the tenant denials of the
/// decision audit reader (<see cref="DecisionAuditTenantRequiredCode"/> and
/// <see cref="DecisionAuditTenantMismatchCode"/>): they use the
/// <c>encina.authorization.abac_{reason}</c> codes so every host adapter answers them with
/// HTTP 403 through the shared <c>encina.authorization.*</c> prefix rule.
/// All errors include structured metadata for observability.
/// </remarks>
public static class ABACErrors
{
    private const string MetadataKeyRequestType = "requestType";
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageAbac = "abac";

    // ── Error Code Constants ────────────────────────────────────────

    /// <summary>Error code when policy evaluation resulted in Deny (an authorization denial, HTTP 403).</summary>
    public const string AccessDeniedCode = "encina.authorization.abac_access_denied";

    /// <summary>Error code when policy evaluation resulted in Indeterminate (error during evaluation).</summary>
    public const string IndeterminateCode = "abac.indeterminate";

    /// <summary>
    /// Error code when an administrative or lookup operation references a policy that does not exist
    /// (a missing resource, not an authorization denial; the <c>.not_found</c> suffix makes the host
    /// adapters answer HTTP 404). A request denied because a required policy
    /// is missing uses <see cref="RequiredPolicyNotFoundCode"/> instead.
    /// </summary>
    public const string PolicyNotFoundCode = "abac.policy.not_found";

    /// <summary>
    /// Error code when a request is denied because a policy it requires is missing from the policy
    /// store (a fail-closed authorization denial, HTTP 403).
    /// </summary>
    public const string RequiredPolicyNotFoundCode = "encina.authorization.abac_policy_not_found";

    /// <summary>Error code when the referenced policy set does not exist (HTTP 404 in the host adapters).</summary>
    public const string PolicySetNotFoundCode = "abac.policy_set.not_found";

    /// <summary>Error code when policy evaluation threw an exception.</summary>
    public const string EvaluationFailedCode = "abac.evaluation_failed";

    /// <summary>Error code when a required attribute could not be resolved (MustBePresent = true).</summary>
    public const string AttributeResolutionFailedCode = "abac.attribute_resolution_failed";

    /// <summary>Error code when the policy definition is invalid.</summary>
    public const string InvalidPolicyCode = "abac.invalid_policy";

    /// <summary>Error code when the policy set definition is invalid.</summary>
    public const string InvalidPolicySetCode = "abac.invalid_policy_set";

    /// <summary>Error code when a condition expression could not be parsed or compiled.</summary>
    public const string InvalidConditionCode = "abac.invalid_condition";

    /// <summary>Error code when a policy with the same ID already exists.</summary>
    public const string DuplicatePolicyCode = "abac.duplicate_policy";

    /// <summary>Error code when a policy set with the same ID already exists.</summary>
    public const string DuplicatePolicySetCode = "abac.duplicate_policy_set";

    /// <summary>Error code when the combining algorithm produced Indeterminate.</summary>
    public const string CombiningFailedCode = "abac.combining_failed";

    /// <summary>Error code when a mandatory obligation handler failed (access must be denied per XACML spec).</summary>
    public const string ObligationFailedCode = "encina.authorization.abac_obligation_failed";

    /// <summary>Error code when a referenced function is not in the registry.</summary>
    public const string FunctionNotFoundCode = "abac.function_not_found";

    /// <summary>Error code when function evaluation threw an exception.</summary>
    public const string FunctionErrorCode = "abac.function_error";

    /// <summary>Error code when a VariableReference references an undefined VariableDefinition.</summary>
    public const string VariableNotFoundCode = "abac.variable_not_found";

    /// <summary>Error code when policy serialization fails.</summary>
    public const string SerializationFailedCode = "abac.serialization_failed";

    /// <summary>Error code when policy deserialization fails.</summary>
    public const string DeserializationFailedCode = "abac.deserialization_failed";

    /// <summary>Error code when a policy store operation fails due to an infrastructure error.</summary>
    public const string StoreOperationFailedCode = "abac.store_operation_failed";

    /// <summary>Error code when <see cref="ABACOptions.UsePersistentPAP"/> is enabled but no <see cref="Persistence.IPolicyStore"/> is registered.</summary>
    public const string PersistentStoreNotRegisteredCode = "abac.persistent_store_not_registered";

    /// <summary>Error code when policy caching is enabled but no <c>ICacheProvider</c> is registered.</summary>
    public const string CacheProviderNotRegisteredCode = "abac.cache_provider_not_registered";

    /// <summary>Error code when a <see cref="RequireConditionAttribute"/> expression evaluated to <c>false</c>.</summary>
    public const string ConditionNotMetCode = "encina.authorization.abac_condition_not_met";

    /// <summary>Error code when an obligation or advice handler threw an exception instead of returning a result.</summary>
    public const string ObligationHandlerExceptionCode = "abac.obligation_handler_exception";

    /// <summary>Error code when a policy change is refused because the request context has no authenticated caller to attribute it to.</summary>
    public const string PolicyChangePrincipalRequiredCode = "abac.policy_change_principal_required";

    /// <summary>Error code when the audit record of a policy change could not be written, so the change was not applied.</summary>
    public const string PolicyChangeAuditFailedCode = "abac.policy_change_audit_failed";

    /// <summary>
    /// Error code when the decision audit record of a request that would proceed could not be written,
    /// so the request is denied (fail closed). A server-side failure, not an authorization denial.
    /// </summary>
    public const string DecisionAuditFailedCode = "abac.decision_audit_failed";

    /// <summary>
    /// Error code when a decision audit query has invalid arguments (page size, date range): a client
    /// input error, so it uses the <c>validation.</c> family that every host adapter answers with HTTP 400.
    /// </summary>
    public const string InvalidDecisionAuditQueryCode = "validation.abac_decision_audit_query_invalid";

    /// <summary>Error code when the decision audit reader or export finds no <c>IOperationAuditStore</c> registered.</summary>
    public const string DecisionAuditStoreUnavailableCode = "abac.decision_audit_store_unavailable";

    /// <summary>
    /// Error code when the decision audit reader is asked for data while multi-tenancy is enabled and the
    /// request carries no tenant (an authorization denial, HTTP 403).
    /// </summary>
    public const string DecisionAuditTenantRequiredCode = "encina.authorization.abac_audit_tenant_required";

    /// <summary>
    /// Error code when the decision audit reader is asked for the data of a tenant other than the
    /// tenant of the request (an authorization denial, HTTP 403).
    /// </summary>
    public const string DecisionAuditTenantMismatchCode = "encina.authorization.abac_audit_tenant_mismatch";

    // ── Factory Methods ─────────────────────────────────────────────

    /// <summary>
    /// Creates an error for access denied by ABAC policy evaluation.
    /// </summary>
    /// <param name="requestType">The request type that was denied.</param>
    /// <param name="policyId">The identifier of the policy that produced the Deny decision.</param>
    /// <returns>An error indicating ABAC access denial.</returns>
    public static EncinaError AccessDenied(Type requestType, string? policyId = null) =>
        EncinaErrors.Create(
            code: AccessDeniedCode,
            message: policyId is not null
                ? $"Access denied for '{requestType.Name}' by policy '{policyId}'."
                : $"Access denied for '{requestType.Name}' by ABAC policy evaluation.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageAbac,
                ["policyId"] = policyId
            });

    /// <summary>
    /// Creates an error for Indeterminate policy evaluation result.
    /// </summary>
    /// <param name="requestType">The request type that produced Indeterminate.</param>
    /// <param name="reason">A description of why the evaluation was indeterminate.</param>
    /// <returns>An error indicating an indeterminate evaluation result.</returns>
    public static EncinaError Indeterminate(Type requestType, string? reason = null) =>
        EncinaErrors.Create(
            code: IndeterminateCode,
            message: reason is not null
                ? $"Policy evaluation for '{requestType.Name}' is indeterminate: {reason}"
                : $"Policy evaluation for '{requestType.Name}' produced an indeterminate result.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageAbac,
                ["reason"] = reason
            });

    /// <summary>
    /// Creates an error when a referenced policy does not exist.
    /// </summary>
    /// <param name="policyId">The identifier of the policy that was not found.</param>
    /// <returns>An error indicating the policy was not found.</returns>
    public static EncinaError PolicyNotFound(string policyId) =>
        EncinaErrors.Create(
            code: PolicyNotFoundCode,
            message: $"Policy '{policyId}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["policyId"] = policyId
            });

    /// <summary>
    /// Creates an error when a policy named by <see cref="RequirePolicyAttribute"/> is neither a
    /// policy set nor a policy in the policy store.
    /// </summary>
    /// <param name="requestType">The request type that requires the policy.</param>
    /// <param name="policyName">The required policy name, recorded in the error details only.</param>
    /// <returns>An error with code <see cref="RequiredPolicyNotFoundCode"/> and a fixed message.</returns>
    public static EncinaError RequiredPolicyNotFound(Type requestType, string policyName)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(policyName);

        return EncinaErrors.Create(
            code: RequiredPolicyNotFoundCode,
            message: "A policy required by the request was not found in the policy store. Access denied.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageAbac,
                ["policyId"] = policyName
            });
    }

    /// <summary>
    /// Creates an error when a <see cref="RequireConditionAttribute"/> expression evaluated to <c>false</c>.
    /// </summary>
    /// <param name="requestType">The request type whose condition was not met.</param>
    /// <param name="conditionIndex">The zero-based position of the condition among the request's conditions.</param>
    /// <returns>An error with code <see cref="ConditionNotMetCode"/> and a fixed message.</returns>
    public static EncinaError ConditionNotMet(Type requestType, int conditionIndex)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        return EncinaErrors.Create(
            code: ConditionNotMetCode,
            message: "A condition required by the request was not met. Access denied.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageAbac,
                ["conditionIndex"] = conditionIndex
            });
    }

    /// <summary>
    /// Creates an error when an obligation or advice handler threw an exception.
    /// </summary>
    /// <param name="obligationId">The identifier of the obligation or advice whose handler threw.</param>
    /// <param name="exceptionType">The type of the exception; its message is never recorded.</param>
    /// <returns>An error with code <see cref="ObligationHandlerExceptionCode"/> and a fixed message.</returns>
    public static EncinaError ObligationHandlerException(string obligationId, Type exceptionType)
    {
        ArgumentNullException.ThrowIfNull(obligationId);
        ArgumentNullException.ThrowIfNull(exceptionType);

        return EncinaErrors.Create(
            code: ObligationHandlerExceptionCode,
            message: "An obligation or advice handler threw an exception.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["obligationId"] = obligationId,
                ["exceptionType"] = exceptionType.FullName
            });
    }

    /// <summary>
    /// Creates an error when a referenced policy set does not exist.
    /// </summary>
    /// <param name="policySetId">The identifier of the policy set that was not found.</param>
    /// <returns>An error indicating the policy set was not found.</returns>
    public static EncinaError PolicySetNotFound(string policySetId) =>
        EncinaErrors.Create(
            code: PolicySetNotFoundCode,
            message: $"Policy set '{policySetId}' was not found.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["policySetId"] = policySetId
            });

    /// <summary>
    /// Creates an error when policy evaluation threw an exception.
    /// </summary>
    /// <param name="requestType">The request type whose evaluation failed.</param>
    /// <param name="exception">The exception that occurred during evaluation; only its type is recorded, never its message.</param>
    /// <returns>An error indicating evaluation failure.</returns>
    public static EncinaError EvaluationFailed(Type requestType, Exception exception) =>
        EncinaErrors.Create(
            code: EvaluationFailedCode,
            message: $"Policy evaluation failed for '{requestType.Name}'. Access denied.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageAbac,
                ["exceptionType"] = exception.GetType().FullName
            });

    /// <summary>
    /// Creates an error when a required attribute could not be resolved.
    /// </summary>
    /// <param name="attributeId">The identifier of the attribute that could not be resolved.</param>
    /// <param name="category">The attribute category (Subject, Resource, Action, or Environment).</param>
    /// <returns>An error indicating attribute resolution failure.</returns>
    public static EncinaError AttributeResolutionFailed(string attributeId, AttributeCategory category) =>
        EncinaErrors.Create(
            code: AttributeResolutionFailedCode,
            message: $"Required attribute '{attributeId}' in category '{category}' could not be resolved.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["attributeId"] = attributeId,
                ["category"] = category.ToString()
            });

    /// <summary>
    /// Creates an error when a policy definition is invalid.
    /// </summary>
    /// <param name="policyId">The identifier of the invalid policy.</param>
    /// <param name="reason">A description of why the policy is invalid.</param>
    /// <returns>An error indicating an invalid policy definition.</returns>
    public static EncinaError InvalidPolicy(string policyId, string reason) =>
        EncinaErrors.Create(
            code: InvalidPolicyCode,
            message: $"Policy '{policyId}' is invalid: {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["policyId"] = policyId,
                ["reason"] = reason
            });

    /// <summary>
    /// Creates an error when a policy set definition is invalid.
    /// </summary>
    /// <param name="policySetId">The identifier of the invalid policy set.</param>
    /// <param name="reason">A description of why the policy set is invalid.</param>
    /// <returns>An error indicating an invalid policy set definition.</returns>
    public static EncinaError InvalidPolicySet(string policySetId, string reason) =>
        EncinaErrors.Create(
            code: InvalidPolicySetCode,
            message: $"Policy set '{policySetId}' is invalid: {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["policySetId"] = policySetId,
                ["reason"] = reason
            });

    /// <summary>
    /// Creates an error when a condition expression could not be parsed or compiled.
    /// </summary>
    /// <param name="expression">The condition expression that failed.</param>
    /// <param name="reason">A description of the parsing or compilation error.</param>
    /// <returns>An error indicating an invalid condition expression.</returns>
    public static EncinaError InvalidCondition(string expression, string? reason = null) =>
        EncinaErrors.Create(
            code: InvalidConditionCode,
            message: reason is not null
                ? $"Condition expression is invalid: {reason}"
                : $"Condition expression could not be parsed.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["expression"] = expression,
                ["reason"] = reason
            });

    /// <summary>
    /// Creates an error when a policy with the same ID already exists.
    /// </summary>
    /// <param name="policyId">The identifier of the duplicate policy.</param>
    /// <returns>An error indicating a duplicate policy.</returns>
    public static EncinaError DuplicatePolicy(string policyId) =>
        EncinaErrors.Create(
            code: DuplicatePolicyCode,
            message: $"A policy with ID '{policyId}' already exists.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["policyId"] = policyId
            });

    /// <summary>
    /// Creates an error when a policy set with the same ID already exists.
    /// </summary>
    /// <param name="policySetId">The identifier of the duplicate policy set.</param>
    /// <returns>An error indicating a duplicate policy set.</returns>
    public static EncinaError DuplicatePolicySet(string policySetId) =>
        EncinaErrors.Create(
            code: DuplicatePolicySetCode,
            message: $"A policy set with ID '{policySetId}' already exists.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["policySetId"] = policySetId
            });

    /// <summary>
    /// Creates an error when a combining algorithm produced Indeterminate.
    /// </summary>
    /// <param name="algorithmId">The identifier of the combining algorithm that failed.</param>
    /// <param name="reason">An optional description of what went wrong.</param>
    /// <returns>An error indicating combining algorithm failure.</returns>
    public static EncinaError CombiningFailed(string algorithmId, string? reason = null) =>
        EncinaErrors.Create(
            code: CombiningFailedCode,
            message: reason is not null
                ? $"Combining algorithm '{algorithmId}' failed: {reason}"
                : $"Combining algorithm '{algorithmId}' produced an indeterminate result.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["algorithmId"] = algorithmId,
                ["reason"] = reason
            });

    /// <summary>
    /// Creates the error the Policy Enforcement Point returns when the request has no authenticated
    /// caller (<see cref="IRequestContext.Identity"/> is anonymous). It is returned in every
    /// enforcement mode, before any attribute is collected.
    /// </summary>
    /// <param name="requestType">The request type that required ABAC evaluation.</param>
    /// <returns>
    /// An error with the shared code <see cref="EncinaErrorCodes.AuthorizationUnauthenticated"/> and
    /// the detail <c>gate = abac</c>, so every host answers it as unauthenticated. The message is fixed
    /// and carries no caller data.
    /// </returns>
    public static EncinaError UnauthenticatedCaller(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        return EncinaErrors.Create(
            code: EncinaErrorCodes.AuthorizationUnauthenticated,
            message: "ABAC evaluation requires an authenticated caller. Access denied.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageAbac,
                ["gate"] = "abac",
                ["requirement"] = "authenticated"
            });
    }

    /// <summary>
    /// Creates an error when a mandatory obligation handler failed.
    /// </summary>
    /// <param name="obligationId">The identifier of the obligation that failed.</param>
    /// <param name="reason">An optional description of the failure.</param>
    /// <returns>An error indicating obligation execution failure. Per XACML 3.0 §7.18, access must be denied.</returns>
    public static EncinaError ObligationFailed(string obligationId, string? reason = null) =>
        EncinaErrors.Create(
            code: ObligationFailedCode,
            message: reason is not null
                ? $"Mandatory obligation '{obligationId}' failed: {reason}"
                : $"Mandatory obligation '{obligationId}' could not be fulfilled. Access denied per XACML specification.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["obligationId"] = obligationId,
                ["reason"] = reason
            });

    /// <summary>
    /// Creates an error when a referenced function is not found in the registry.
    /// </summary>
    /// <param name="functionId">The identifier of the function that was not found.</param>
    /// <returns>An error indicating the function is not registered.</returns>
    public static EncinaError FunctionNotFound(string functionId) =>
        EncinaErrors.Create(
            code: FunctionNotFoundCode,
            message: $"Function '{functionId}' is not registered in the function registry.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["functionId"] = functionId
            });

    /// <summary>
    /// Creates an error when function evaluation threw an exception.
    /// </summary>
    /// <param name="functionId">The identifier of the function that failed.</param>
    /// <param name="exception">The exception that occurred during function evaluation; its message is never recorded.</param>
    /// <returns>An error indicating function evaluation failure, with a fixed message and the exception type in the details.</returns>
    public static EncinaError FunctionError(string functionId, Exception exception) =>
        EncinaErrors.Create(
            code: FunctionErrorCode,
            message: $"Function '{functionId}' evaluation failed.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["functionId"] = functionId,
                ["exceptionType"] = exception.GetType().FullName
            });

    /// <summary>
    /// Creates an error when a VariableReference references an undefined VariableDefinition.
    /// </summary>
    /// <param name="variableId">The identifier of the undefined variable.</param>
    /// <returns>An error indicating the variable is not defined.</returns>
    public static EncinaError VariableNotFound(string variableId) =>
        EncinaErrors.Create(
            code: VariableNotFoundCode,
            message: $"Variable '{variableId}' is not defined. Ensure a VariableDefinition with this ID exists in the policy.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["variableId"] = variableId
            });

    /// <summary>
    /// Creates an error when policy serialization fails.
    /// </summary>
    /// <param name="targetType">The type being serialized (e.g., "PolicySet", "Policy").</param>
    /// <param name="reason">A description of why serialization failed.</param>
    /// <returns>An error indicating serialization failure.</returns>
    public static EncinaError SerializationFailed(string targetType, string reason) =>
        EncinaErrors.Create(
            code: SerializationFailedCode,
            message: $"Failed to serialize {targetType}: {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["targetType"] = targetType,
                ["reason"] = reason
            });

    /// <summary>
    /// Creates an error when policy deserialization fails.
    /// </summary>
    /// <param name="targetType">The type being deserialized (e.g., "PolicySet", "Policy").</param>
    /// <param name="reason">A description of why deserialization failed.</param>
    /// <returns>An error indicating deserialization failure.</returns>
    public static EncinaError DeserializationFailed(string targetType, string reason) =>
        EncinaErrors.Create(
            code: DeserializationFailedCode,
            message: $"Failed to deserialize {targetType}: {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["targetType"] = targetType,
                ["reason"] = reason
            });

    /// <summary>
    /// Creates an error when a policy store operation fails due to an infrastructure error.
    /// </summary>
    /// <param name="operation">The store operation that failed (e.g., "SavePolicySetAsync").</param>
    /// <param name="reason">A description of the failure reason.</param>
    /// <returns>An error indicating a store infrastructure failure.</returns>
    public static EncinaError StoreOperationFailed(string operation, string reason) =>
        EncinaErrors.Create(
            code: StoreOperationFailedCode,
            message: $"Policy store operation '{operation}' failed: {reason}",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["operation"] = operation,
                ["reason"] = reason
            });

    /// <summary>
    /// Creates an error when <see cref="ABACOptions.UsePersistentPAP"/> is enabled but
    /// no <see cref="Persistence.IPolicyStore"/> implementation is registered.
    /// </summary>
    /// <returns>An error indicating the persistent store is not registered.</returns>
    public static EncinaError PersistentStoreNotRegistered() =>
        EncinaErrors.Create(
            code: PersistentStoreNotRegisteredCode,
            message: "UsePersistentPAP is enabled but no IPolicyStore implementation is registered. " +
                     "Register a provider package (e.g., AddEncinaEntityFrameworkCore with UseABACPolicyStore = true).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["requirement"] = "IPolicyStore"
            });

    /// <summary>
    /// Creates an error when policy caching is enabled but no <c>ICacheProvider</c>
    /// implementation is registered.
    /// </summary>
    /// <returns>An error indicating the cache provider is not registered.</returns>
    public static EncinaError CacheProviderNotRegistered() =>
        EncinaErrors.Create(
            code: CacheProviderNotRegisteredCode,
            message: "Policy caching is enabled but no ICacheProvider implementation is registered. " +
                     "Register a caching package (e.g., AddEncinaRedisCache, AddEncinaMemoryCache).",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["requirement"] = "ICacheProvider"
            });

    /// <summary>
    /// Creates an error when a policy change is refused because the request context has no
    /// authenticated caller (a user or a declared service identity) to attribute the change to.
    /// The message is fixed and carries no caller data.
    /// </summary>
    /// <returns>An error indicating that a policy change needs an authenticated caller.</returns>
    public static EncinaError PolicyChangePrincipalRequired() =>
        EncinaErrors.Create(
            code: PolicyChangePrincipalRequiredCode,
            message: "A policy change requires an authenticated caller; the current request context has none.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac
            });

    /// <summary>
    /// Creates an error when the audit record of a policy change could not be written, so the
    /// change was not applied (the policy administration point fails closed). The message is
    /// fixed; only the underlying error code (or exception type) is recorded.
    /// </summary>
    /// <param name="cause">The error code or exception type name of the audit failure.</param>
    /// <returns>An error indicating that the policy change was not applied.</returns>
    public static EncinaError PolicyChangeAuditFailed(string cause) =>
        EncinaErrors.Create(
            code: PolicyChangeAuditFailedCode,
            message: "The audit record of the policy change could not be written; the change was not applied.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["cause"] = cause
            });

    /// <summary>
    /// Creates the error the Policy Enforcement Point returns when the decision audit record of a
    /// request that would have proceeded could not be written (fail closed). The message is fixed;
    /// only the request type and the store's error code (or exception type) are recorded.
    /// </summary>
    /// <param name="requestType">The request type whose decision could not be recorded.</param>
    /// <param name="storeErrorCode">The error code or exception type name of the store failure, when known; never a message.</param>
    /// <returns>An error with code <see cref="DecisionAuditFailedCode"/>.</returns>
    public static EncinaError DecisionAuditFailed(Type requestType, string? storeErrorCode)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        return EncinaErrors.Create(
            code: DecisionAuditFailedCode,
            message: "The ABAC decision could not be recorded in the audit trail. Access denied.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageAbac,
                ["cause"] = storeErrorCode
            });
    }

    /// <summary>
    /// Creates an error when a decision audit query has invalid arguments.
    /// </summary>
    /// <param name="reason">A fixed description of the invalid argument (for example <c>"pageSize"</c>); never caller data.</param>
    /// <returns>An error with code <see cref="InvalidDecisionAuditQueryCode"/>.</returns>
    public static EncinaError InvalidDecisionAuditQuery(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        return EncinaErrors.Create(
            code: InvalidDecisionAuditQueryCode,
            message: "The decision audit query is invalid.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["reason"] = reason
            });
    }

    /// <summary>
    /// Creates an error when the decision audit recorder, reader or export needs an
    /// <c>IOperationAuditStore</c> and none is registered. The same cause and code serve the write
    /// and the read path, so the message names neither alone.
    /// </summary>
    /// <returns>An error with code <see cref="DecisionAuditStoreUnavailableCode"/> and a fixed message.</returns>
    public static EncinaError DecisionAuditStoreUnavailable() =>
        EncinaErrors.Create(
            code: DecisionAuditStoreUnavailableCode,
            message: "No operation audit store is registered, so the decision audit trail cannot be written or read.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["requirement"] = "IOperationAuditStore"
            });

    /// <summary>
    /// Creates the denial the decision audit reader returns when multi-tenancy is enabled and the
    /// request carries no tenant.
    /// </summary>
    /// <returns>An error with code <see cref="DecisionAuditTenantRequiredCode"/> and a fixed message.</returns>
    public static EncinaError DecisionAuditTenantRequired() =>
        EncinaErrors.Create(
            code: DecisionAuditTenantRequiredCode,
            message: "Reading the decision audit trail requires a tenant. Access denied.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["requirement"] = "tenant"
            });

    /// <summary>
    /// Creates the denial the decision audit reader returns when the query names a tenant other than
    /// the tenant of the request.
    /// </summary>
    /// <returns>An error with code <see cref="DecisionAuditTenantMismatchCode"/> and a fixed message; neither tenant is recorded.</returns>
    public static EncinaError DecisionAuditTenantMismatch() =>
        EncinaErrors.Create(
            code: DecisionAuditTenantMismatchCode,
            message: "The decision audit query names a different tenant than the request. Access denied.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyStage] = MetadataStageAbac,
                ["requirement"] = "same-tenant"
            });
}
