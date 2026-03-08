namespace Encina.Security.ABAC;

/// <summary>
/// Factory methods for ABAC-related <see cref="EncinaError"/> instances.
/// </summary>
/// <remarks>
/// Error codes follow the convention <c>abac.{category}</c>.
/// All errors include structured metadata for observability.
/// </remarks>
public static class ABACErrors
{
    private const string MetadataKeyRequestType = "requestType";
    private const string MetadataKeyStage = "stage";
    private const string MetadataStageAbac = "abac";

    // ── Error Code Constants ────────────────────────────────────────

    /// <summary>Error code when policy evaluation resulted in Deny.</summary>
    public const string AccessDeniedCode = "abac.access_denied";

    /// <summary>Error code when policy evaluation resulted in Indeterminate (error during evaluation).</summary>
    public const string IndeterminateCode = "abac.indeterminate";

    /// <summary>Error code when the referenced policy does not exist.</summary>
    public const string PolicyNotFoundCode = "abac.policy_not_found";

    /// <summary>Error code when the referenced policy set does not exist.</summary>
    public const string PolicySetNotFoundCode = "abac.policy_set_not_found";

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

    /// <summary>Error code when the security context is not available.</summary>
    public const string MissingContextCode = "abac.missing_context";

    /// <summary>Error code when a mandatory obligation handler failed (access must be denied per XACML spec).</summary>
    public const string ObligationFailedCode = "abac.obligation_failed";

    /// <summary>Error code when a referenced function is not in the registry.</summary>
    public const string FunctionNotFoundCode = "abac.function_not_found";

    /// <summary>Error code when function evaluation threw an exception.</summary>
    public const string FunctionErrorCode = "abac.function_error";

    /// <summary>Error code when a VariableReference references an undefined VariableDefinition.</summary>
    public const string VariableNotFoundCode = "abac.variable_not_found";

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
    /// <param name="exception">The exception that occurred during evaluation.</param>
    /// <returns>An error indicating evaluation failure.</returns>
    public static EncinaError EvaluationFailed(Type requestType, Exception exception) =>
        EncinaErrors.Create(
            code: EvaluationFailedCode,
            message: $"Policy evaluation failed for '{requestType.Name}': {exception.Message}",
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
    /// Creates an error when the security context is not available for ABAC evaluation.
    /// </summary>
    /// <param name="requestType">The request type that required ABAC evaluation.</param>
    /// <returns>An error indicating the security context is missing.</returns>
    public static EncinaError MissingContext(Type requestType) =>
        EncinaErrors.Create(
            code: MissingContextCode,
            message: $"Security context is not available for ABAC evaluation of '{requestType.Name}'. Ensure ABAC middleware is configured.",
            details: new Dictionary<string, object?>
            {
                [MetadataKeyRequestType] = requestType.FullName,
                [MetadataKeyStage] = MetadataStageAbac,
                ["requirement"] = "abac_context"
            });

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
    /// <param name="exception">The exception that occurred during function evaluation.</param>
    /// <returns>An error indicating function evaluation failure.</returns>
    public static EncinaError FunctionError(string functionId, Exception exception) =>
        EncinaErrors.Create(
            code: FunctionErrorCode,
            message: $"Function '{functionId}' evaluation failed: {exception.Message}",
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
}
