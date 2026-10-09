---
title: "ABAC Quick Reference"
layout: default
parent: "Features"
---

# ABAC Quick Reference

## Service Registration

```csharp
services.AddEncinaABAC();                                      // Defaults
services.AddEncinaABAC(o => o.EnforcementMode = ABACEnforcementMode.Block);  // Configured
```

## Request Decoration

```csharp
[RequirePolicy("finance-access")]                               // Named policy, must permit (AND)
[RequirePolicy("admin-override", AllMustPass = false)]          // At least one of these must permit (OR)
[RequireCondition("user.department == \"engineering\"")]        // Inline EEL (C# string literals, escaped inside the attribute)
[RequireCondition("user.clearanceLevel >= resource.classification")]
```

| Rule | Behavior |
|------|----------|
| Named policy | The top-level policy set or standalone policy (one contained in no set) with that id is evaluated on its own; only `Permit` passes. A policy that exists only inside a set is not found: name the parent set instead |
| `NotApplicable` from a required policy | Denies |
| Policy not in the store | Denies with `encina.authorization.abac_policy_not_found` |
| `Indeterminate` or evaluation error | Denies (`abac.indeterminate` / `abac.evaluation_failed`), in every enforcement mode |
| No security context, an unauthenticated one (`IsAuthenticated` is `false`), or empty `UserId` | Denies with `abac.missing_context`, in every enforcement mode, before any attribute is collected |
| Several `[RequirePolicy]` | `AllMustPass = true` ones are ANDed, `AllMustPass = false` ones are ORed, both groups must hold |
| `[RequireCondition]` is `false` | Denies with `encina.authorization.abac_condition_not_met` |
| `[RequireCondition]` fails to compile or throws | `Indeterminate`, denies with `abac.indeterminate` |
| Policies and conditions | Combined with AND; conditions run only after the named policies permit, in declaration order. The variables are `user`, `resource`, `environment` and `action` (`action.name` is the request type name) |
| No `[RequirePolicy]` and no `[RequireCondition]` | The request is not evaluated |

## Policy Builder (Minimal)

```csharp
var policy = new PolicyBuilder("my-policy")
    .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
    .AddRule("allow-get-report", Effect.Permit, rule => rule
        .WithCondition(ConditionBuilder.Equal(
            ConditionBuilder.Attribute(AttributeCategory.Action, "name", XACMLDataTypes.String),
            ConditionBuilder.StringValue("GetReportQuery"))))
    .Build();
```

The action attribute `name` is the request type name, so the value above matches a `GetReportQuery` request.

## PolicySet Builder (Minimal)

```csharp
var policySet = new PolicySetBuilder("org-policies")
    .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
    .AddPolicy("child-policy", p => p
        .AddRule("rule-1", Effect.Permit, _ => { }))
    .Build();
```

## Effects

| Effect | Value | Meaning |
|--------|-------|---------|
| `Permit` | Request allowed | Policy explicitly grants access |
| `Deny` | Request blocked | Policy explicitly refuses access |
| `NotApplicable` | No opinion | No policy target matched the request |
| `Indeterminate` | Error | Evaluation failed (missing attribute, function error) |

## Combining Algorithms

| Algorithm | Behavior |
|-----------|----------|
| `DenyOverrides` | Any Deny wins. Safest for mandatory access control |
| `PermitOverrides` | Any Permit wins. For discretionary access |
| `FirstApplicable` | First matching rule/policy wins (order-sensitive) |
| `OnlyOneApplicable` | Exactly one must match; otherwise Indeterminate |
| `DenyUnlessPermit` | Default Deny unless explicit Permit. Never returns NotApplicable |
| `PermitUnlessDeny` | Default Permit unless explicit Deny. Never returns NotApplicable |
| `OrderedDenyOverrides` | DenyOverrides with deterministic obligation ordering |
| `OrderedPermitOverrides` | PermitOverrides with deterministic obligation ordering |

## Attribute Categories

| Category | XACML URN | Typical Attributes |
|----------|-----------|-------------------|
| `Subject` | `access-subject` | userId, roles, department, clearanceLevel |
| `Resource` | `resource` | resourceType, classification, owner |
| `Action` | `action` | name (read/write/delete), httpMethod |
| `Environment` | `environment` | currentTime, dayOfWeek, ipAddress, isBusinessHours |

## Common Functions

| Function ID | Signature | Description |
|-------------|-----------|-------------|
| `string-equal` | `(string, string) -> bool` | String equality |
| `integer-equal` | `(int, int) -> bool` | Integer equality |
| `boolean-equal` | `(bool, bool) -> bool` | Boolean equality |
| `integer-greater-than` | `(int, int) -> bool` | Integer > comparison |
| `integer-less-than` | `(int, int) -> bool` | Integer < comparison |
| `string-contains` | `(string, string) -> bool` | Substring check |
| `string-starts-with` | `(string, string) -> bool` | Prefix check |
| `string-regexp-match` | `(string, string) -> bool` | Regex match |
| `string-is-in` | `(string, bag) -> bool` | Membership test |
| `string-one-and-only` | `(bag) -> string` | Extract single value from bag |
| `and` | `(bool...) -> bool` | Logical AND (short-circuit) |
| `or` | `(bool...) -> bool` | Logical OR (short-circuit) |
| `not` | `(bool) -> bool` | Logical NOT |
| `any-of` | `(fn, bag) -> bool` | True if fn(element) for any element |
| `all-of` | `(fn, bag) -> bool` | True if fn(element) for all elements |

## EEL Quick Reference

```csharp
// Attribute access
user.department                        // Subject attribute
resource.classification                // Resource attribute
environment.isBusinessHours            // Environment attribute
action.name                            // Action attribute (the request type name)

// Comparisons
user.clearanceLevel >= resource.classification
user.department == "engineering"

// Boolean logic
user.isAdmin == true || user.department == "security"
```

## Enforcement Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Block` | Deny stops request execution | Production |
| `Warn` | Definite verdicts (Deny, required policy NotApplicable/Deny/not found, condition `false`) are logged and the request proceeds; errors (`abac.missing_context`, `abac.indeterminate`, `abac.evaluation_failed`, `encina.authorization.abac_obligation_failed`) still deny when they decide the verdict (a definite denial found next to an error is the verdict and passes) | Policy validation / rollout |
| `Disabled` | ABAC skipped entirely | Development / feature flag |

## Error Codes

| Code | Description |
|------|-------------|
| `encina.authorization.abac_access_denied` | Policy evaluation resulted in Deny (HTTP 403) |
| `abac.indeterminate` | Evaluation error (missing attribute, function failure) |
| `abac.policy.not_found` | Administrative lookup (PAP update/remove, `EvaluatePolicyAsync`) on a policy that does not exist; never returned by the PEP |
| `encina.authorization.abac_policy_not_found` | A `[RequirePolicy]` name is not in the policy store (HTTP 403) |
| `abac.policy_set.not_found` | Referenced policy set does not exist |
| `abac.evaluation_failed` | Exception during evaluation |
| `abac.attribute_resolution_failed` | Required attribute unresolvable (MustBePresent) |
| `abac.invalid_policy` | Policy definition is invalid |
| `abac.invalid_policy_set` | PolicySet definition is invalid |
| `abac.invalid_condition` | EEL expression parse/compile failure |
| `abac.duplicate_policy` | Policy with same ID already exists |
| `abac.duplicate_policy_set` | PolicySet with same ID already exists |
| `abac.combining_failed` | Combining algorithm produced Indeterminate |
| `abac.missing_context` | Security context unavailable, not authenticated, or its `UserId` is empty |
| `encina.authorization.abac_obligation_failed` | Mandatory obligation handler failed (access denied per XACML 7.18; HTTP 403) |
| `abac.function_not_found` | Function not registered in registry |
| `abac.function_error` | Function evaluation threw exception |
| `abac.variable_not_found` | VariableReference to undefined VariableDefinition |
| `encina.authorization.abac_condition_not_met` | A `[RequireCondition]` expression evaluated to `false` (HTTP 403) |
| `abac.obligation_handler_exception` | An obligation or advice handler threw (handled inside the executor; the request fails with `encina.authorization.abac_obligation_failed`) |

## Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `abac.evaluation.total` | Counter | Total evaluations |
| `abac.evaluation.permitted` | Counter | Permit decisions |
| `abac.evaluation.denied` | Counter | Deny decisions |
| `abac.evaluation.indeterminate` | Counter | Indeterminate decisions |
| `abac.obligation.executed` | Counter | Obligations executed |
| `abac.obligation.failed` | Counter | Obligations failed |
| `abac.obligation.no_handler` | Counter | Missing obligation handlers |
| `abac.advice.executed` | Counter | Advice executed |
| `abac.evaluation.duration` | Histogram (ms) | Evaluation latency |
| `abac.obligation.duration` | Histogram (ms) | Obligation latency |

## Key Interfaces

| Interface | Role | Lifetime |
|-----------|------|----------|
| `IPolicyDecisionPoint` | Evaluates requests against policies, returns `PolicyDecision` | Singleton |
| `IPolicyAdministrationPoint` | CRUD for Policy/PolicySet (ROP: `Either<EncinaError, T>`) | Singleton |
| `IPolicyInformationPoint` | On-demand attribute resolution via `AttributeDesignator` | Singleton |
| `IAttributeProvider` | Bridges app domain to XACML attributes (subject/resource/env) | Scoped |
| `IObligationHandler` | Executes mandatory post-decision obligations | Scoped |
| `IFunctionRegistry` | Registry of XACML functions for condition evaluation | Singleton |
| `ICombiningAlgorithm` | Aggregates rule/policy results into single decision | Singleton |

## ABACOptions Quick Reference

| Property | Default | Description |
|----------|---------|-------------|
| `EnforcementMode` | `Block` | Block / Warn / Disabled |
| `IncludeAdvice` | `true` | Execute advice expressions |
| `AddHealthCheck` | `false` | Register `encina-abac` health check |
| `ValidateExpressionsAtStartup` | `false` | Fail-fast on invalid EEL |
| `SeedPolicySets` | `[]` | PolicySets loaded at startup |
| `SeedPolicies` | `[]` | Standalone Policies loaded at startup |
| `CustomFunctions` | `[]` | Custom XACML functions |

## Health Check States

| State | Condition |
|-------|-----------|
| `Healthy` | At least one Policy or PolicySet loaded |
| `Degraded` | PAP is empty (every `[RequirePolicy]` is missing, so those requests are denied) |
| `Unhealthy` | PAP query threw an exception |

## Log Event ID Ranges

| Range | Category |
|-------|----------|
| 9000-9005, 9008-9009 | Pipeline (evaluation start, decision, enforcement) |
| 9010-9019 | Obligations (execution, failure, missing handler) |
| 9020-9029 | Advice (execution, failure, skipped) |
| 9072-9078 | Required policies, conditions and obligation or advice handler exceptions |
| 9091-9093 | Fail-closed denials: missing context, policy store retrieval failure, store evaluation exception (9079-9083 and 9088-9090 are the decision audit trail, see [observability](observability.md#decision-audit-messages-9079-9090)) |
| 9094 | Policy change audit write failed (error code) |
| 9095 | Exception during a policy change audit write |
| 9096 | System actor scope opened for startup seeding |
| 9097 | Policy changes applied without an audit store (Warning, once per PAP instance) |
| 9098 | A stored decision audit entry could not be read; the query fails (Error) |
| 9099 | A failed decision audit write could not be confirmed because the look-up failed too (Debug) |
