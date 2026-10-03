---
title: "ABAC Error Reference"
layout: default
parent: "Features"
---

# ABAC Error Reference

## Overview

Encina ABAC uses the Railway Oriented Programming (ROP) pattern for error handling. All operations return `Either<EncinaError, T>`, where the left side contains a structured error and the right side contains the success value. No exceptions are thrown for business logic failures.

All ABAC errors are created through factory methods on the `ABACErrors` static class. Each error includes:

- A **code** string following the `abac.{category}` convention
- A **message** with human-readable context
- A **details** dictionary with structured metadata for observability (always includes `stage = "abac"`)

## Error Code Table

| Error Code | Constant | Factory Method | Parameters | When It Occurs |
|------------|----------|---------------|------------|----------------|
| `abac.access_denied` | `AccessDeniedCode` | `AccessDenied` | `Type requestType, string? policyId = null` | A policy named by `[RequirePolicy]` returned Deny or NotApplicable (an explicitly required policy that does not apply cannot authorize). |
| `abac.indeterminate` | `IndeterminateCode` | `Indeterminate` | `Type requestType, string? reason = null` | A required policy or a `[RequireCondition]` expression could not produce a definitive result (evaluation error, expression that does not compile). |
| `abac.policy_not_found` | `PolicyNotFoundCode` | `PolicyNotFound`, `RequiredPolicyNotFound` | `string policyId`; `Type requestType, string policyName` | A referenced policy does not exist in the PAP. `RequiredPolicyNotFound` is the error the PEP returns when `[RequirePolicy("name")]` names a policy set or policy that is not in the store; its message is fixed and the name is only in the details. |
| `abac.policy_set_not_found` | `PolicySetNotFoundCode` | `PolicySetNotFound` | `string policySetId` | A referenced policy set does not exist in the PAP. |
| `abac.evaluation_failed` | `EvaluationFailedCode` | `EvaluationFailed` | `Type requestType, Exception exception` | An unhandled exception occurred during policy evaluation. |
| `abac.attribute_resolution_failed` | `AttributeResolutionFailedCode` | `AttributeResolutionFailed` | `string attributeId, AttributeCategory category` | A required attribute (MustBePresent = true) could not be resolved. |
| `abac.invalid_policy` | `InvalidPolicyCode` | `InvalidPolicy` | `string policyId, string reason` | A policy definition is structurally invalid. |
| `abac.invalid_policy_set` | `InvalidPolicySetCode` | `InvalidPolicySet` | `string policySetId, string reason` | A policy set definition is structurally invalid. |
| `abac.invalid_condition` | `InvalidConditionCode` | `InvalidCondition` | `string expression, string? reason = null` | A condition expression could not be parsed or compiled. |
| `abac.duplicate_policy` | `DuplicatePolicyCode` | `DuplicatePolicy` | `string policyId` | A policy with the same ID already exists in the PAP. |
| `abac.duplicate_policy_set` | `DuplicatePolicySetCode` | `DuplicatePolicySet` | `string policySetId` | A policy set with the same ID already exists in the PAP. |
| `abac.combining_failed` | `CombiningFailedCode` | `CombiningFailed` | `string algorithmId, string? reason = null` | A combining algorithm produced an Indeterminate result. |
| `abac.missing_context` | `MissingContextCode` | `MissingContext` | `Type requestType` | The security context is not available for ABAC evaluation. |
| `abac.obligation_failed` | `ObligationFailedCode` | `ObligationFailed` | `string obligationId, string? reason = null` | A mandatory obligation handler failed or was not found. Per XACML 3.0 section 7.18, access must be denied. |
| `abac.function_not_found` | `FunctionNotFoundCode` | `FunctionNotFound` | `string functionId` | A function referenced in a policy condition is not registered in `IFunctionRegistry`. |
| `abac.function_error` | `FunctionErrorCode` | `FunctionError` | `string functionId, Exception exception` | A registered function threw an exception during evaluation. |
| `abac.variable_not_found` | `VariableNotFoundCode` | `VariableNotFound` | `string variableId` | A `VariableReference` references an undefined `VariableDefinition` within the policy. |
| `abac.condition_not_met` | `ConditionNotMetCode` | `ConditionNotMet` | `Type requestType, int conditionIndex` | A `[RequireCondition]` expression evaluated to `false`. The message is fixed. |
| `abac.obligation_handler_exception` | `ObligationHandlerExceptionCode` | `ObligationHandlerException` | `string obligationId, Type exceptionType` | An obligation or advice handler threw instead of returning a result. The message is fixed and the exception message is never recorded. `ObligationExecutor` handles this error itself: a mandatory obligation then fails the request with `abac.obligation_failed`, and advice is skipped. |

## Error Metadata

Every error includes structured metadata in the `Details` dictionary:

| Key | Present In | Value |
|-----|-----------|-------|
| `stage` | All errors | Always `"abac"` |
| `requestType` | `AccessDenied`, `Indeterminate`, `RequiredPolicyNotFound`, `ConditionNotMet`, `EvaluationFailed`, `MissingContext` | Fully qualified type name of the request |
| `policyId` | `AccessDenied`, `PolicyNotFound`, `RequiredPolicyNotFound`, `InvalidPolicy`, `DuplicatePolicy` | The policy identifier (for `RequiredPolicyNotFound`, the name from `[RequirePolicy]`) |
| `conditionIndex` | `ConditionNotMet` | The zero-based position of the condition among the request's `[RequireCondition]` attributes |
| `policySetId` | `PolicySetNotFound`, `InvalidPolicySet`, `DuplicatePolicySet` | The policy set identifier |
| `reason` | `Indeterminate`, `InvalidPolicy`, `InvalidPolicySet`, `InvalidCondition`, `CombiningFailed`, `ObligationFailed` | Description of why the error occurred |
| `attributeId` | `AttributeResolutionFailed` | The attribute identifier that could not be resolved |
| `category` | `AttributeResolutionFailed` | The `AttributeCategory` (Subject, Resource, Action, or Environment) |
| `expression` | `InvalidCondition` | The EEL expression that failed |
| `algorithmId` | `CombiningFailed` | The combining algorithm identifier |
| `obligationId` | `ObligationFailed`, `ObligationHandlerException` | The obligation or advice identifier |
| `functionId` | `FunctionNotFound`, `FunctionError` | The function identifier |
| `variableId` | `VariableNotFound` | The variable identifier |
| `exceptionType` | `EvaluationFailed`, `FunctionError`, `ObligationHandlerException` | Fully qualified exception type name |
| `requirement` | `MissingContext` | Always `"abac_context"` |

## Error Handling Patterns

### Using Either.Match

```csharp
Either<EncinaError, OrderResponse> result = await mediator.Send(new CreateOrderCommand(...));

result.Match(
    left: error =>
    {
        if (error.Code == ABACErrors.AccessDeniedCode)
            return Results.Forbid();

        if (error.Code == ABACErrors.MissingContextCode)
            return Results.Problem("Authorization context not configured.");

        return Results.Problem(error.Message);
    },
    right: response => Results.Ok(response)
);
```

### Checking Error Codes

```csharp
if (result.IsLeft)
{
    var error = result.LeftValue;

    switch (error.Code)
    {
        case ABACErrors.AccessDeniedCode:
            logger.LogWarning("Access denied: {ErrorCode}", error.GetCode().IfNone("encina.unknown"));
            break;

        case ABACErrors.ObligationFailedCode:
            logger.LogError("Obligation failure: {ObligationId}", error.Details["obligationId"]);
            break;

        case ABACErrors.AttributeResolutionFailedCode:
            logger.LogError("Missing attribute: {AttributeId} in {Category}",
                error.Details["attributeId"], error.Details["category"]);
            break;
    }
}
```

### Accessing Metadata

```csharp
var error = ABACErrors.AccessDenied(typeof(CreateOrderCommand), "order-policy-v1");

// error.Code        => "abac.access_denied"
// error.Message     => "Access denied for 'CreateOrderCommand' by policy 'order-policy-v1'."
// error.Details["requestType"]  => "MyApp.Commands.CreateOrderCommand"
// error.Details["stage"]        => "abac"
// error.Details["policyId"]     => "order-policy-v1"
```

## Common Error Scenarios

### 1. Access Denied (abac.access_denied)

**Scenario:** A user without the required role attempts an operation.

```
Error: Access denied for 'DeletePatientRecord' by policy 'medical-records-policy'.
```

**Resolution:** Verify the user has the required attributes (role, department, clearance level) that match the policy target. Check the policy rules to understand which conditions must be satisfied.

### 2. Missing Context (abac.missing_context)

**Scenario:** The ABAC pipeline behavior executes but no security context is available.

```
Error: Security context is not available for ABAC evaluation of 'CreateOrder'.
       Ensure ABAC middleware is configured.
```

**Resolution:** Verify that authentication middleware runs before the ABAC pipeline. Ensure `IAttributeProvider` has access to the current user's claims and request context.

### 3. Obligation Failed (abac.obligation_failed)

**Scenario:** A policy grants Permit with a mandatory obligation (e.g., audit logging), but no handler is registered.

```
Error: Mandatory obligation 'log-access-audit' could not be fulfilled.
       Access denied per XACML specification.
```

**Resolution:** Register an `IObligationHandler` for the obligation ID. If in development, set `ABACOptions.FailOnMissingObligationHandler = false` to allow soft-fail. This must be `true` in production per XACML 3.0 section 7.18.

### 4. Attribute Resolution Failed (abac.attribute_resolution_failed)

**Scenario:** A policy condition references an attribute marked `MustBePresent = true`, but the PIP cannot resolve it.

```
Error: Required attribute 'department' in category 'Subject' could not be resolved.
```

**Resolution:** Ensure the attribute is available via the `IAttributeProvider` or `IPolicyInformationPoint`. Check that the user's claims or the external data source contains the required attribute.

### 5. Function Not Found (abac.function_not_found)

**Scenario:** A policy condition references a custom function that was not registered.

```
Error: Function 'custom:geo-distance' is not registered in the function registry.
```

**Resolution:** Register the function via `ABACOptions.AddFunction()` during service configuration:

```csharp
options.AddFunction("custom:geo-distance", new GeoDistanceFunction());
```

### 6. Invalid Condition (abac.invalid_condition)

**Scenario:** An EEL expression in a policy has a syntax error.

```
Error: Condition expression is invalid: Unexpected token ')' at position 15.
```

**Resolution:** Fix the EEL expression syntax. Enable `ValidateExpressionsAtStartup` to catch these errors at application startup rather than at request time.

### 7. Variable Not Found (abac.variable_not_found)

**Scenario:** A `VariableReference` in a rule references a `VariableDefinition` that does not exist in the policy.

```
Error: Variable 'maxRetries' is not defined.
       Ensure a VariableDefinition with this ID exists in the policy.
```

**Resolution:** Add a `VariableDefinition` with the matching ID to the policy, or correct the `VariableReference` ID to match an existing definition.

### 8. Combining Algorithm Failed (abac.combining_failed)

**Scenario:** A combining algorithm encounters an error while merging child policy results.

```
Error: Combining algorithm 'deny-overrides' produced an indeterminate result.
```

**Resolution:** Inspect the child policies for evaluation errors. A combining algorithm produces Indeterminate when one or more child evaluations fail and the algorithm cannot resolve a definitive Permit or Deny.

### 9. Duplicate Policy (abac.duplicate_policy)

**Scenario:** Attempting to add a policy to the PAP when one with the same ID already exists.

```
Error: A policy with ID 'order-access-v2' already exists.
```

**Resolution:** Use a unique policy ID, or remove the existing policy before adding the new one. During seeding, duplicates are logged as warnings and skipped automatically.

### 10. Evaluation Failed (abac.evaluation_failed)

**Scenario:** An unhandled exception occurred during policy evaluation (e.g., a null reference in a custom function).

```
Error: Policy evaluation failed for 'TransferFunds': Object reference not set to an instance of an object.
```

**Resolution:** Check the `exceptionType` in the error metadata. Debug the custom function or attribute provider that threw the exception. The `EvaluationFailed` error wraps the original exception details.

### 11. Condition Not Met (abac.condition_not_met)

**Scenario:** A request type carries `[RequireCondition("user.clearanceLevel >= 3")]` and the expression is `false` for the current user.

```text
Error: A condition required by the request was not met. Access denied.
```

**Resolution:** Read `conditionIndex` in the error details to find which condition failed, then check the attributes your `IAttributeProvider` returns for `user`, `resource`, `environment` and `action`. A condition that cannot be compiled, or that reads an attribute that is missing, is not this error: it is `Indeterminate` (`abac.indeterminate`) and also denies.

### 12. Required Policy Not Found (abac.policy_not_found)

**Scenario:** A request type carries `[RequirePolicy("finance-access")]` but the policy store holds no policy set and no policy with that id.

```text
Error: A policy required by the request was not found in the policy store. Access denied.
```

**Resolution:** Read `policyId` in the error details, then seed or create the policy set or policy with that id. The PEP does not fall back to evaluating the rest of the store.

### 13. Obligation Handler Exception (abac.obligation_handler_exception)

**Scenario:** An `IObligationHandler` throws while handling an obligation or advice.

```text
Error: An obligation or advice handler threw an exception.
```

**Resolution:** Read `obligationId` and `exceptionType` in the error details and fix the handler. A mandatory obligation whose handler throws denies the request, and the caller receives `abac.obligation_failed`; advice whose handler throws is skipped. The exception message is never logged (EventId 9078 records the exception through `ForLogging()`).
