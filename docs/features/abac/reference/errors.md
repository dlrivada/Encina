---
title: "ABAC Error Reference"
layout: default
parent: "Features"
---

# ABAC Error Reference

## Overview

Encina ABAC uses the Railway Oriented Programming (ROP) pattern for error handling. All operations return `Either<EncinaError, T>`, where the left side contains a structured error and the right side contains the success value. No exceptions are thrown for business logic failures.

All ABAC errors are created through factory methods on the `ABACErrors` static class. Each error includes:

- A **code** string. Most codes follow the `abac.{category}` convention; the four definite denials the PEP returns to the caller (`AccessDeniedCode`, `ConditionNotMetCode`, `ObligationFailedCode`, `RequiredPolicyNotFoundCode`) use the `encina.authorization.` prefix instead
- A **message** with human-readable context
- A **details** dictionary with structured metadata for observability (always includes `stage = "abac"`)

## Error Code Table

| Error Code | Constant | Factory Method | Parameters | When It Occurs |
|------------|----------|---------------|------------|----------------|
| `encina.authorization.abac_access_denied` | `AccessDeniedCode` | `AccessDenied` | `Type requestType, string? policyId = null` | A policy named by `[RequirePolicy]` returned Deny or NotApplicable (an explicitly required policy that does not apply cannot authorize). |
| `abac.indeterminate` | `IndeterminateCode` | `Indeterminate` | `Type requestType, string? reason = null` | A required policy or a `[RequireCondition]` expression could not produce a definitive result (evaluation error, expression that does not compile). |
| `abac.policy.not_found` | `PolicyNotFoundCode` | `PolicyNotFound` | `string policyId` | An administrative lookup, not a request denial: a PAP operation (update or remove) or `EvaluatePolicyAsync` on the PDP names a policy that does not exist. The PEP never returns this code to the caller. |
| `encina.authorization.abac_policy_not_found` | `RequiredPolicyNotFoundCode` | `RequiredPolicyNotFound` | `Type requestType, string policyName` | A request denial: `[RequirePolicy("name")]` names no top-level policy set or standalone policy in the store (a policy that exists only nested inside a set is not found; name its parent set). The message is fixed and the name is only in the details. |
| `abac.policy_set.not_found` | `PolicySetNotFoundCode` | `PolicySetNotFound` | `string policySetId` | A referenced policy set does not exist in the PAP. |
| `abac.evaluation_failed` | `EvaluationFailedCode` | `EvaluationFailed` | `Type requestType, Exception exception` | An unhandled exception occurred during policy evaluation. The message is fixed (`Policy evaluation failed for '<RequestType>'. Access denied.`); only the exception type is recorded, in `details["exceptionType"]`, never the exception message. |
| `abac.attribute_resolution_failed` | `AttributeResolutionFailedCode` | `AttributeResolutionFailed` | `string attributeId, AttributeCategory category` | A required attribute (MustBePresent = true) could not be resolved. |
| `abac.invalid_policy` | `InvalidPolicyCode` | `InvalidPolicy` | `string policyId, string reason` | A policy definition is structurally invalid. |
| `abac.invalid_policy_set` | `InvalidPolicySetCode` | `InvalidPolicySet` | `string policySetId, string reason` | A policy set definition is structurally invalid. |
| `abac.invalid_condition` | `InvalidConditionCode` | `InvalidCondition` | `string expression, string? reason = null` | A condition expression could not be parsed or compiled. |
| `abac.duplicate_policy` | `DuplicatePolicyCode` | `DuplicatePolicy` | `string policyId` | A policy with the same ID already exists in the PAP. |
| `abac.duplicate_policy_set` | `DuplicatePolicySetCode` | `DuplicatePolicySet` | `string policySetId` | A policy set with the same ID already exists in the PAP. |
| `abac.combining_failed` | `CombiningFailedCode` | `CombiningFailed` | `string algorithmId, string? reason = null` | A combining algorithm produced an Indeterminate result. |
| `abac.missing_context` | `MissingContextCode` | `MissingContext` | `Type requestType` | There is no security context, it is not authenticated (`IsAuthenticated` is `false`, even when it carries a user id claim), or its `UserId` is null, empty or whitespace. The PEP denies in every enforcement mode (`Block` and `Warn`) before it collects any attribute. |
| `encina.authorization.abac_obligation_failed` | `ObligationFailedCode` | `ObligationFailed` | `string obligationId, string? reason = null` | A mandatory obligation handler failed or was not found. Per XACML 3.0 section 7.18, access must be denied. |
| `abac.function_not_found` | `FunctionNotFoundCode` | `FunctionNotFound` | `string functionId` | A function referenced in a policy condition is not registered in `IFunctionRegistry`. |
| `abac.function_error` | `FunctionErrorCode` | `FunctionError` | `string functionId, Exception exception` | A registered function threw an exception during evaluation. The message is fixed (`Function '<id>' evaluation failed.`); only the exception type is recorded, in `details["exceptionType"]`, never the exception message. |
| `abac.variable_not_found` | `VariableNotFoundCode` | `VariableNotFound` | `string variableId` | A `VariableReference` references an undefined `VariableDefinition` within the policy. |
| `encina.authorization.abac_condition_not_met` | `ConditionNotMetCode` | `ConditionNotMet` | `Type requestType, int conditionIndex` | A `[RequireCondition]` expression evaluated to `false`. The message is fixed. |
| `abac.policy_change_principal_required` | `PolicyChangePrincipalRequiredCode` | `PolicyChangePrincipalRequired` | none | `PersistentPolicyAdministrationPoint` refused a mutation because the request context carries no principal. The message is fixed. |
| `abac.policy_change_audit_failed` | `PolicyChangeAuditFailedCode` | `PolicyChangeAuditFailed` | `string cause` | The audit record of a policy change could not be written, so the change was not applied. `cause` (details) is the underlying error code or exception type. |
| `abac.obligation_handler_exception` | `ObligationHandlerExceptionCode` | `ObligationHandlerException` | `string obligationId, Type exceptionType` | An obligation or advice handler threw instead of returning a result. The message is fixed and the exception message is never recorded. `ObligationExecutor` handles this error itself: a mandatory obligation then fails the request with `encina.authorization.abac_obligation_failed`, and advice is skipped. |

### Decision audit codes

Returned by the PEP, `AuditStoreABACDecisionRecorder` and `IABACDecisionAuditReader`; see [Decision audit](decision-audit.md). Their messages are fixed.

| Error Code | Constant | Factory Method | Parameters | When It Occurs |
|------------|----------|---------------|------------|----------------|
| `abac.decision_audit_failed` | `DecisionAuditFailedCode` | `DecisionAuditFailed` | `Type requestType, string? storeErrorCode` | The decision record of a request that would proceed could not be written and `FailureMode` is `FailClosed`. A server-side failure, not an authorization denial. `details["cause"]` is the store's error code or exception type. |
| `validation.abac_decision_audit_query_invalid` | `InvalidDecisionAuditQueryCode` | `InvalidDecisionAuditQuery` | `string reason` | A decision audit query has an invalid `pageNumber`, `pageSize`, `dateRange` or `tenantId` (named in `details["reason"]`). |
| `abac.decision_audit_store_unavailable` | `DecisionAuditStoreUnavailableCode` | `DecisionAuditStoreUnavailable` | none | The recorder, reader or export finds no `IOperationAuditStore` registered. |
| `abac.decision_audit_record_unreadable` | `DecisionAuditRecordUnreadableCode` | `DecisionAuditRecordUnreadable` | `string cause` | A stored entry's metadata cannot be read back by `QueryAsync` or `ExportAsync`. The message is fixed; `details["cause"]` is the exception type name. |
| `abac.decision_audit_export_incomplete` | `DecisionAuditExportIncompleteCode` | `DecisionAuditExportIncomplete` | `int linesWritten, string causeCode` | `ExportAsync` failed on a page after lines were already written. The message is fixed; `details["linesWritten"]` is the number of lines written and `details["cause"]` is the code of the failing page. The destination holds a partial file that must be discarded. A failure before the first line returns its own error and writes nothing. |
| `encina.authorization.abac_audit_tenant_required` | `DecisionAuditTenantRequiredCode` | `DecisionAuditTenantRequired` | none | The reader is asked for data in a multi-tenant application and the request carries no tenant (and `AllowCrossTenantQueries` is `false`). |
| `encina.authorization.abac_audit_tenant_mismatch` | `DecisionAuditTenantMismatchCode` | `DecisionAuditTenantMismatch` | none | The query names a tenant other than the tenant of the request. Neither tenant is recorded. |

The two `encina.authorization.` codes are answered with HTTP 403 and `validation.abac_decision_audit_query_invalid` with HTTP 400 by the host adapters' existing prefix rules.

## HTTP Mapping

The four definite denials start with `encina.authorization.`, so the ASP.NET Core, Azure Functions and AWS Lambda adapters answer them with HTTP 403 through their existing `encina.authorization.` prefix rule; there is no ABAC-specific mapping table. The other `abac.*` codes keep the mapping the adapters already give them. The two administrative lookup codes `abac.policy.not_found` and `abac.policy_set.not_found` end in `.not_found`, so the adapters answer them with HTTP 404. `abac.function_not_found` and `abac.variable_not_found` do not end in `.not_found`; they are policy configuration errors raised while a policy is evaluated and stay HTTP 500.

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

// error.Code        => "encina.authorization.abac_access_denied"
// error.Message     => "Access denied for 'CreateOrderCommand' by policy 'order-policy-v1'."
// error.Details["requestType"]  => "MyApp.Commands.CreateOrderCommand"
// error.Details["stage"]        => "abac"
// error.Details["policyId"]     => "order-policy-v1"
```

## Common Error Scenarios

### 1. Access Denied (encina.authorization.abac_access_denied)

**Scenario:** A user without the required role attempts an operation.

```
Error: Access denied for 'DeletePatientRecord' by policy 'medical-records-policy'.
```

**Resolution:** Verify the user has the required attributes (role, department, clearance level) that match the policy target. Check the policy rules to understand which conditions must be satisfied.

### 2. Missing Context (abac.missing_context)

**Scenario:** The ABAC pipeline behavior executes for a request with `[RequirePolicy]` or `[RequireCondition]`, but `ISecurityContextAccessor.SecurityContext` is null, it is not authenticated (`SecurityContext.IsAuthenticated` is `false`), or its `UserId` is null, empty or whitespace.

```
Error: Authenticated security context with a user is not available for ABAC evaluation of 'CreateOrder'. Access denied.
```

The request is denied in every enforcement mode, `Warn` included, and no attribute is requested when there is no authenticated user. The denial is logged with EventId 9091 (request type and error code only).

**Resolution:** ABAC needs an `ISecurityContextAccessor` whose `SecurityContext` is populated and authenticated.

- The application registers `Encina.Security` (`AddEncinaSecurity`) and sets the context for every request, as shown in [Set Security Context](https://github.com/dlrivada/Encina/blob/main/src/Encina.Security/README.md#3-set-security-context).
- Background jobs and scheduled messages set a context with a service identity: a `ClaimsIdentity` created with an authentication type (for example `new ClaimsIdentity(claims, "service")`), so `IsAuthenticated` is true, and with a `sub` or `NameIdentifier` claim.
- A request meant to run without a user must not carry `[RequirePolicy]` or `[RequireCondition]` (or ABAC must run in `Disabled` mode).
- Populating the context automatically, for example from `HttpContext.User`, is tracked by #1705.

### 3. Obligation Failed (encina.authorization.abac_obligation_failed)

**Scenario:** A policy grants Permit with a mandatory obligation (e.g., audit logging), but no handler is registered.

```
Error: Mandatory obligation 'log-access-audit' could not be fulfilled.
       Access denied per XACML specification.
```

**Resolution:** Register an `IObligationHandler` for the obligation ID. A mandatory obligation with no handler always denies, in every enforcement mode; there is no option to relax this (XACML 3.0 section 7.18). Advice without a handler is skipped.

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
Error: Policy evaluation failed for 'TransferFunds'. Access denied.
```

**Resolution:** Read the `exceptionType` in the error details, then debug the custom function or attribute provider that threw. The error never carries the exception message, because it can contain data; the exception itself is logged with EventId 9009 through `ForLogging()` (type and stack trace only).

### 11. Condition Not Met (encina.authorization.abac_condition_not_met)

**Scenario:** A request type carries `[RequireCondition("user.clearanceLevel >= 3")]` and the expression is `false` for the current user.

```text
Error: A condition required by the request was not met. Access denied.
```

**Resolution:** Read `conditionIndex` in the error details to find which condition failed, then check the attributes your `IAttributeProvider` returns for `user`, `resource`, `environment` and `action`. A condition that cannot be compiled, or that reads an attribute that is missing, is not this error: it is `Indeterminate` (`abac.indeterminate`) and also denies.

### 12. Required Policy Not Found (encina.authorization.abac_policy_not_found)

**Scenario:** A request type carries `[RequirePolicy("finance-access")]` but the policy store holds no policy set and no policy with that id.

```text
Error: A policy required by the request was not found in the policy store. Access denied.
```

**Resolution:** Read `policyId` in the error details, then seed or create the policy set or policy with that id. The PEP does not fall back to evaluating the rest of the store. This is a request denial; `abac.policy.not_found` (`PolicyNotFoundCode`) is a different code, returned only by administrative lookups and `EvaluatePolicyAsync`, never by the PEP.

### 13. Obligation Handler Exception (abac.obligation_handler_exception)

**Scenario:** An `IObligationHandler` throws while handling an obligation or advice.

```text
Error: An obligation or advice handler threw an exception.
```

**Resolution:** Read `obligationId` and `exceptionType` in the error details and fix the handler. A mandatory obligation whose handler throws denies the request, and the caller receives `encina.authorization.abac_obligation_failed`; advice whose handler throws is skipped. The exception message is never logged (EventId 9078 records the exception through `ForLogging()`).
