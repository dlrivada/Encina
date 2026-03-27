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
[RequirePolicy("finance-access")]                               // Named policy
[RequirePolicy("admin-override", AllMustPass = false)]          // OR logic
[RequireCondition("subject.department == 'engineering'")]        // Inline EEL
[RequireCondition("subject.clearanceLevel >= resource.classification")]
```

## Policy Builder (Minimal)

```csharp
var policy = new PolicyBuilder("my-policy")
    .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
    .AddRule("allow-read", Effect.Permit, rule => rule
        .WithCondition(ConditionBuilder.Equal(
            ConditionBuilder.Attribute(AttributeCategory.Action, "name", XACMLDataTypes.String),
            ConditionBuilder.StringValue("read"))))
    .Build();
```

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
subject.department                     // Subject attribute
resource.classification                // Resource attribute
environment.isBusinessHours            // Environment attribute

// Comparisons
subject.clearanceLevel >= resource.classification
subject.department == "engineering"

// Boolean logic
subject.isAdmin == true || subject.department == "security"
```

## Enforcement Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Block` | Deny stops request execution | Production |
| `Warn` | Deny logged but request proceeds | Policy validation / rollout |
| `Disabled` | ABAC skipped entirely | Development / feature flag |

## Error Codes

| Code | Description |
|------|-------------|
| `abac.access_denied` | Policy evaluation resulted in Deny |
| `abac.indeterminate` | Evaluation error (missing attribute, function failure) |
| `abac.policy_not_found` | Referenced policy does not exist |
| `abac.policy_set_not_found` | Referenced policy set does not exist |
| `abac.evaluation_failed` | Exception during evaluation |
| `abac.attribute_resolution_failed` | Required attribute unresolvable (MustBePresent) |
| `abac.invalid_policy` | Policy definition is invalid |
| `abac.invalid_policy_set` | PolicySet definition is invalid |
| `abac.invalid_condition` | EEL expression parse/compile failure |
| `abac.duplicate_policy` | Policy with same ID already exists |
| `abac.duplicate_policy_set` | PolicySet with same ID already exists |
| `abac.combining_failed` | Combining algorithm produced Indeterminate |
| `abac.missing_context` | Security context unavailable |
| `abac.obligation_failed` | Mandatory obligation handler failed (access denied per XACML 7.18) |
| `abac.function_not_found` | Function not registered in registry |
| `abac.function_error` | Function evaluation threw exception |
| `abac.variable_not_found` | VariableReference to undefined VariableDefinition |

## Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `abac.evaluation.total` | Counter | Total evaluations |
| `abac.evaluation.permitted` | Counter | Permit decisions |
| `abac.evaluation.denied` | Counter | Deny decisions |
| `abac.evaluation.not_applicable` | Counter | NotApplicable decisions |
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
| `DefaultNotApplicableEffect` | `Deny` | Effect when no policy matches |
| `IncludeAdvice` | `true` | Execute advice expressions |
| `FailOnMissingObligationHandler` | `true` | Deny if no handler (XACML 7.18) |
| `AddHealthCheck` | `false` | Register `encina-abac` health check |
| `ValidateExpressionsAtStartup` | `false` | Fail-fast on invalid EEL |
| `SeedPolicySets` | `[]` | PolicySets loaded at startup |
| `SeedPolicies` | `[]` | Standalone Policies loaded at startup |
| `CustomFunctions` | `[]` | Custom XACML functions |

## Health Check States

| State | Condition |
|-------|-----------|
| `Healthy` | At least one Policy or PolicySet loaded |
| `Degraded` | PAP is empty (all requests get NotApplicable) |
| `Unhealthy` | PAP query threw an exception |

## Log Event ID Ranges

| Range | Category |
|-------|----------|
| 9000-9009 | Pipeline (evaluation start, decision, enforcement) |
| 9010-9019 | Obligations (execution, failure, missing handler) |
| 9020-9029 | Advice (execution, failure, skipped) |
