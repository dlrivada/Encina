---
title: "ABAC Security Guide"
layout: default
parent: "Features"
---

# ABAC Security Guide

Encina.Security.ABAC implements the OASIS XACML 3.0 standard with security built into every layer. This guide covers the security considerations, best practices, and configuration options for hardening your ABAC deployment.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Defense in Depth](#2-defense-in-depth)
3. [Policy Evaluation Security](#3-policy-evaluation-security)
4. [EEL Expression Safety](#4-eel-expression-safety)
5. [Attribute Integrity](#5-attribute-integrity)
6. [Obligation Security](#6-obligation-security)
7. [Default Deny Principle](#7-default-deny-principle)
8. [Enforcement Mode Considerations](#8-enforcement-mode-considerations)
9. [Audit Trail](#9-audit-trail)
10. [Policy Administration Security](#10-policy-administration-security)
11. [Common Pitfalls](#11-common-pitfalls)

---

## 1. Overview

ABAC security is not a single mechanism but a composition of interconnected guarantees. The XACML 3.0 standard prescribes specific behaviors for error handling, obligation enforcement, and attribute resolution that, when followed correctly, produce a secure-by-default authorization system. Encina enforces these guarantees at the framework level so that application developers benefit from them without manual configuration in most cases.

The three core security principles of Encina ABAC are:

- **Closed-world assumption**: unmatched requests are denied by default.
- **Obligation enforcement**: unfulfilled obligations override Permit decisions.
- **Fail-safe evaluation**: errors produce Indeterminate, never silent Permits.

---

## 2. Defense in Depth

ABAC should never be the sole layer of security in your application. Encina positions ABAC as one behavior in a multi-layered pipeline, complementing coarse-grained checks with fine-grained attribute evaluation.

### Pipeline Security Layers

| Layer | Mechanism | Granularity | Cost |
|-------|-----------|-------------|------|
| 1. Authentication | JWT / Cookie / OAuth | Identity verification | Low |
| 2. Validation | FluentValidation / DataAnnotations | Input sanitization | Low |
| 3. RBAC | `[RequireRole]` attribute | Role membership | O(1) |
| 4. **ABAC** | `[RequirePolicy]` / `[RequireCondition]` | Attribute evaluation | Variable |
| 5. Transaction | Encina Transaction Behavior | Data integrity | Medium |
| 6. Business Logic | Request Handler | Domain rules | Variable |

ABAC runs after RBAC intentionally. Role checks are constant-time lookups that reject unauthorized users before the more expensive attribute resolution and policy evaluation occurs. If a user lacks the basic role, ABAC never executes.

### Complementary Controls

ABAC decisions should be reinforced by lower-level protections:

```csharp
// ABAC provides the authorization decision
[RequirePolicy("document-access")]
[RequireCondition("user.clearanceLevel >= resource.classification")]
public sealed record GetClassifiedDocumentQuery(Guid DocumentId) : IQuery<DocumentDto>;

// But the handler should still validate ownership/access at the domain level
public sealed class GetClassifiedDocumentHandler : IQueryHandler<GetClassifiedDocumentQuery, DocumentDto>
{
    public async ValueTask<Either<EncinaError, DocumentDto>> Handle(
        GetClassifiedDocumentQuery query, CancellationToken ct)
    {
        var document = await _repository.GetAsync(query.DocumentId, ct);
        // Domain-level check as defense in depth
        if (document.IsRedacted && !_currentUser.HasRedactionAccess)
            return DomainErrors.InsufficientAccess();

        return document.ToDto();
    }
}
```

---

## 3. Policy Evaluation Security

The `XACMLPolicyDecisionPoint` is designed to never throw exceptions during evaluation. All failures are captured as `Effect.Indeterminate` with a descriptive `DecisionStatus`, ensuring the PEP always receives a usable decision.

### Four-Effect Model

The four-effect model (Permit, Deny, NotApplicable, Indeterminate) is a security feature, not a complexity burden. Collapsing it to a two-effect model (Permit/Deny) would silently lose information:

| Effect | Meaning | Security Implication |
|--------|---------|---------------------|
| Permit | Explicitly allowed | Proceed with obligation execution |
| Deny | Explicitly refused | Block access |
| NotApplicable | No policy matched | Depends on `DefaultNotApplicableEffect` -- Deny by default |
| Indeterminate | Evaluation error | Treated as Deny in Block mode |

### Combining Algorithm Security

The root-level combining algorithm is hardcoded to `DenyOverrides`. This means that across all policy sets and standalone policies, a single Deny from any source overrides all Permits. This is the safest default and matches XACML 3.0 recommendations for security-sensitive deployments.

Within individual policies, choose your combining algorithm carefully:

| Algorithm | Security Profile | Use Case |
|-----------|-----------------|----------|
| `DenyOverrides` | Conservative | Any deny wins -- use for sensitive resources |
| `PermitOverrides` | Permissive | Any permit wins -- use cautiously |
| `DenyUnlessPermit` | Very conservative | Deny unless explicitly permitted |
| `PermitUnlessDeny` | Permissive | Permit unless explicitly denied |
| `FirstApplicable` | Order-dependent | First matching rule wins -- order matters |
| `OnlyOneApplicable` | Strict | Error if multiple policies apply |

---

## 4. EEL Expression Safety

The Encina Expression Language (EEL) compiles C# boolean expressions via Roslyn's scripting API. This provides flexibility but requires careful sandboxing.

### Roslyn Sandboxing

The `EELCompiler` restricts the scripting environment through `ScriptOptions`:

```csharp
_scriptOptions = ScriptOptions.Default
    .WithReferences(
        typeof(object).Assembly,          // System.Runtime
        typeof(Enumerable).Assembly,      // System.Linq
        typeof(ExpandoObject).Assembly,   // System.Dynamic
        typeof(CSharpArgumentInfo).Assembly) // Microsoft.CSharp
    .WithImports(
        "System",
        "System.Linq",
        "System.Collections.Generic");
```

**What EEL expressions CAN do:**

- Access the four attribute categories (`user`, `resource`, `environment`, `action`) as dynamic objects
- Use standard comparison, logical, and arithmetic operators
- Call LINQ methods on collections (`user.roles.Contains("admin")`)
- Use string methods (`resource.name.StartsWith("classified-")`)

**What EEL expressions CANNOT do:**

- Access the file system (no `System.IO` import)
- Make network calls (no `System.Net` import)
- Create threads or tasks (no `System.Threading` import)
- Access reflection APIs (no `System.Reflection` import)
- Execute arbitrary code outside the sandboxed globals

### Compilation Caching and Thread Safety

Each unique expression is compiled once and cached in a `ConcurrentDictionary<string, ScriptRunner<bool>>`. Compilation is serialized via `SemaphoreSlim` with double-check locking to prevent duplicate compilation of the same expression under concurrent load.

### Startup Validation

Enable `ValidateExpressionsAtStartup` to catch invalid EEL expressions at application startup rather than at request time:

```csharp
services.AddEncinaABAC(options =>
{
    options.ValidateExpressionsAtStartup = true;
    options.ExpressionScanAssemblies.Add(typeof(MyCommand).Assembly);
});
```

This scans assemblies for `[RequireCondition]` attributes and compiles every expression during startup. If any expression fails compilation, the application throws `InvalidOperationException` immediately.

### Expression Injection Prevention

EEL expressions are defined in C# attributes at compile time, not from user input. Never construct EEL expressions from user-provided strings:

```csharp
// NEVER do this -- expression injection vulnerability
[RequireCondition($"user.department == \"{userInput}\"")]

// CORRECT -- expressions are compile-time constants
[RequireCondition("user.department == resource.requiredDepartment")]
```

---

## 5. Attribute Integrity

ABAC decisions are only as trustworthy as the attributes they evaluate. If an attacker can manipulate attribute values, they can manipulate authorization decisions.

### Trusted Attribute Sources

| Source | Trust Level | Recommendation |
|--------|------------|----------------|
| JWT claims (signed) | High | Validate signature, issuer, audience |
| Server-side database | High | Query from trusted data store |
| HTTP headers | Low | Never trust without validation |
| Query parameters | Low | Treat as untrusted input |
| Client-side cookies (unsigned) | Low | Do not use for authorization attributes |

### MustBePresent Flag

The `AttributeDesignator.MustBePresent` flag is a critical security mechanism. When set to `true`, a missing attribute causes `Effect.Indeterminate` instead of silently evaluating with an empty bag:

```csharp
var designator = new AttributeDesignator
{
    Category = AttributeCategory.Subject,
    AttributeId = "clearanceLevel",
    DataType = "integer",
    MustBePresent = true // Missing attribute = Indeterminate, not empty bag
};
```

**Security recommendation**: Set `MustBePresent = true` for all attributes that are essential to the authorization decision. An empty bag where you expected a value is almost always a bug or an attack.

### Attribute Provider Validation

Your `IAttributeProvider` implementation should validate attribute values before returning them:

```csharp
public sealed class SecureAttributeProvider : IAttributeProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var user = await _userService.GetVerifiedUserAsync(userId, ct)
            ?? throw new InvalidOperationException($"User '{userId}' not found in trusted store.");

        return new Dictionary<string, object>
        {
            ["department"] = user.Department ?? string.Empty,
            ["clearanceLevel"] = user.ClearanceLevel,
            // Derive from trusted source, not from request
            ["roles"] = user.Roles.ToList()
        };
    }
}
```

---

## 6. Obligation Security

XACML 3.0 section 7.18 mandates that if any obligation cannot be fulfilled, the PEP **must deny access** regardless of the PDP decision. Encina enforces this requirement at the framework level.

### Obligation Failure Semantics

The `ObligationExecutor` checks two failure conditions:

1. **Missing handler**: No registered `IObligationHandler` returns `true` from `CanHandle(obligationId)`.
2. **Handler failure**: A handler returns `Either.Left(EncinaError)` from `HandleAsync`.

Both conditions produce an immediate Deny, even when the PDP returned Permit:

```
PDP Decision: Permit
Obligation: "audit-log" -> Handler not registered
Final Decision: DENY (obligation failure overrides Permit)
```

### FailOnMissingObligationHandler

The `ABACOptions.FailOnMissingObligationHandler` option controls behavior when no handler is found:

| Value | Behavior | Environment |
|-------|----------|-------------|
| `true` (default) | Missing handler = Deny | Production |
| `false` | Missing handler is logged, execution continues | Development only |

```csharp
services.AddEncinaABAC(options =>
{
    // MUST be true in production
    options.FailOnMissingObligationHandler = true;
});
```

**Warning**: Setting `FailOnMissingObligationHandler = false` in production violates XACML 3.0 section 7.18 and creates a security gap where obligations are silently ignored.

### Advice vs Obligation

Advice expressions are non-mandatory. Their failure is logged as a warning but does not affect the authorization decision. Use obligations for security-critical actions (audit logging, MFA challenge) and advice for optional recommendations (UI hints, preference suggestions).

---

## 7. Default Deny Principle

Encina defaults to the **closed-world assumption**: if no policy matches a request, the request is denied. This is controlled by `ABACOptions.DefaultNotApplicableEffect`:

```csharp
services.AddEncinaABAC(options =>
{
    // Default -- secure by default
    options.DefaultNotApplicableEffect = Effect.Deny;
});
```

### Why Default Deny Matters

Consider a system where a new request type is added but no policy has been written for it yet:

| DefaultNotApplicableEffect | Behavior | Risk |
|---------------------------|----------|------|
| `Effect.Deny` (default) | New request is blocked until a policy is created | None -- fail-safe |
| `Effect.Permit` | New request is allowed without any policy evaluation | High -- open by default |

The open-world assumption (`Effect.Permit`) is appropriate only in systems where ABAC is advisory rather than authoritative, such as during migration from a legacy authorization system.

### Configuration for Open-World (Use with Caution)

```csharp
// Only use during migration or when ABAC is advisory
services.AddEncinaABAC(options =>
{
    options.DefaultNotApplicableEffect = Effect.Permit;
    options.EnforcementMode = ABACEnforcementMode.Warn; // Log but do not block
});
```

---

## 8. Enforcement Mode Considerations

The `ABACEnforcementMode` enum enables gradual rollout of ABAC policies without risking production outages.

### Rollout Strategy

| Phase | Mode | Purpose |
|-------|------|---------|
| 1. Development | `Disabled` | No ABAC overhead, focus on business logic |
| 2. Shadow mode | `Warn` | Evaluate all policies, log decisions, never block |
| 3. Partial rollout | `Block` + feature flags | Enforce for specific request types |
| 4. Full enforcement | `Block` | All requests are subject to ABAC |

### Warn Mode Security Implications

In `Warn` mode, Deny decisions are logged but the request proceeds. This is useful for validating policies against real traffic, but it means **no authorization is enforced**. Monitor logs for unexpected Deny decisions before transitioning to `Block`:

```csharp
// During shadow mode, monitor these log patterns:
// [ABAC] Evaluation for {RequestType}: Deny (enforcement=Warn, proceeding)
// [ABAC] NotApplicable for {RequestType}: no matching policy
```

### Disabled Mode

When `EnforcementMode = Disabled`, the `ABACPipelineBehavior` calls `nextStep()` immediately without evaluating any policies, collecting any attributes, or executing any obligations. This is zero overhead but provides zero protection.

---

## 9. Audit Trail

ABAC decisions should be logged for compliance and forensic analysis. Encina provides two mechanisms for audit trails: obligation handlers and OpenTelemetry diagnostics.

### Audit Obligation Handler

Create a dedicated obligation handler that logs every access decision:

```csharp
public sealed class SecurityAuditObligationHandler : IObligationHandler
{
    private readonly IAuditStore _auditStore;
    private readonly TimeProvider _timeProvider;

    public bool CanHandle(string obligationId)
        => obligationId is "security-audit" or "compliance-log";

    public async ValueTask<Either<EncinaError, Unit>> HandleAsync(
        Obligation obligation,
        PolicyEvaluationContext context,
        CancellationToken ct)
    {
        var entry = new AuditEntry
        {
            TimestampUtc = _timeProvider.GetUtcNow().UtcDateTime,
            SubjectId = context.SubjectAttributes.GetValueOrDefault("userId")?.ToString(),
            ResourceType = context.ActionAttributes.GetValueOrDefault("action-id")?.ToString(),
            Decision = obligation.FulfillOn.ToString(),
            Attributes = ExtractRelevantAttributes(context)
        };

        await _auditStore.RecordAsync(entry, ct);
        return Unit.Default;
    }
}
```

### Policy with Audit Obligation

Attach the audit obligation to policies that govern sensitive resources:

```csharp
var policy = new PolicyBuilder("classified-access")
    .WithDescription("Access control for classified documents")
    .WithCombiningAlgorithm(CombiningAlgorithmId.DenyOverrides)
    .WithObligationOnPermit("security-audit", b => b
        .WithAttributeAssignment("decision", "Permit")
        .WithAttributeAssignment("policyId", "classified-access"))
    .WithObligationOnDeny("security-audit", b => b
        .WithAttributeAssignment("decision", "Deny")
        .WithAttributeAssignment("policyId", "classified-access"))
    .Build();
```

### OpenTelemetry Metrics

Encina emits OpenTelemetry metrics for all ABAC evaluations via `ABACDiagnostics`:

| Metric | Type | Tags |
|--------|------|------|
| `encina.abac.evaluation.total` | Counter | request_type, enforcement_mode |
| `encina.abac.evaluation.permitted` | Counter | request_type |
| `encina.abac.evaluation.denied` | Counter | request_type |
| `encina.abac.evaluation.indeterminate` | Counter | request_type |
| `encina.abac.evaluation.not_applicable` | Counter | request_type |
| `encina.abac.evaluation.duration` | Histogram | -- |
| `encina.abac.obligation.executed` | Counter | obligation_id |
| `encina.abac.obligation.failed` | Counter | obligation_id |

---

## 10. Policy Administration Security

The `IPolicyAdministrationPoint` controls what policies exist in the system. Unauthorized modifications to the PAP can grant or revoke access to any resource.

### In-Memory PAP Limitations

The built-in `InMemoryPolicyAdministrationPoint` stores policies in `ConcurrentDictionary` instances. It is thread-safe but has no access control, no audit logging, and no persistence. Policies are lost on process restart.

**Production recommendation**: Implement a database-backed `IPolicyAdministrationPoint` with:

- Authentication and authorization for policy CRUD operations
- Audit logging of all policy changes (who changed what, when)
- Policy versioning to support rollback
- Approval workflows for policy modifications

### Protecting PAP Operations

When exposing PAP operations via an API, wrap them in their own ABAC policies or role checks:

```csharp
[RequireRole("PolicyAdministrator")]
[RequireCondition("user.department == \"Security\"")]
public sealed record AddPolicyCommand(Policy Policy, string? ParentPolicySetId) : ICommand;
```

### Policy Seeding Security

Policies seeded via `ABACOptions.SeedPolicySets` and `ABACOptions.SeedPolicies` are loaded at startup by `ABACPolicySeedingHostedService`. Ensure that seed policies are defined in code (not loaded from untrusted external sources) and are reviewed as part of your code review process.

---

## 11. Common Pitfalls

### Pitfall 1: Open-by-Default Policies

A policy with no target matches all requests. Combined with `PermitOverrides`, this creates an open-by-default system:

```csharp
// DANGEROUS -- permits everything
var dangerousPolicy = new PolicyBuilder("catch-all")
    .WithCombiningAlgorithm(CombiningAlgorithmId.PermitOverrides)
    .WithRule(new RuleBuilder("permit-all")
        .WithEffect(Effect.Permit)
        // No target = matches everything
        .Build())
    .Build();
```

**Fix**: Always define explicit targets on policies and rules. Use `DenyOverrides` as the combining algorithm.

### Pitfall 2: Missing MustBePresent

Forgetting `MustBePresent = true` on critical attribute designators means a missing attribute produces an empty bag instead of Indeterminate. An empty bag compared with a value evaluates to `false`, which may silently change the authorization decision:

```csharp
// WRONG -- missing clearanceLevel silently produces empty bag
new Match
{
    MatchFunction = "integer-greater-than-or-equal",
    AttributeDesignator = new AttributeDesignator
    {
        Category = AttributeCategory.Subject,
        AttributeId = "clearanceLevel",
        DataType = "integer",
        MustBePresent = false // A missing clearance = empty bag, not error
    },
    AttributeValue = new AttributeValue { DataType = "integer", Value = 3 }
}
```

**Fix**: Set `MustBePresent = true` for all security-critical attributes.

### Pitfall 3: Ignoring Indeterminate

Treating Indeterminate as NotApplicable or silently swallowing evaluation errors hides bugs and potential attacks. Encina treats Indeterminate according to the enforcement mode (Deny in Block mode), but your logging and monitoring should alert on Indeterminate results:

```
// Alert condition: indeterminate rate > 1% of total evaluations
encina.abac.evaluation.indeterminate / encina.abac.evaluation.total > 0.01
```

### Pitfall 4: Not Registering Obligation Handlers

If a policy includes obligations but no handler is registered, the `ObligationExecutor` denies access (when `FailOnMissingObligationHandler = true`). This is correct security behavior but can cause unexpected denials during development. Use the health check to verify:

```csharp
services.AddEncinaABAC(options =>
{
    options.AddHealthCheck = true; // Verifies at least one policy is loaded
});
```

### Pitfall 5: Using Warn Mode in Production

`ABACEnforcementMode.Warn` logs denials but allows requests through. This is intended for shadow-mode testing only. A misconfiguration that leaves Warn mode active in production effectively disables authorization:

```csharp
// Validate enforcement mode at startup
if (app.Environment.IsProduction())
{
    var abacOptions = app.Services.GetRequiredService<IOptions<ABACOptions>>();
    if (abacOptions.Value.EnforcementMode != ABACEnforcementMode.Block)
    {
        throw new InvalidOperationException(
            "ABAC enforcement mode must be Block in production.");
    }
}
```

### Pitfall 6: Trusting Client-Supplied Attributes

Never derive authorization attributes from values the client can manipulate (query parameters, request body, unsigned cookies). Always resolve attributes from trusted server-side sources:

```csharp
// WRONG -- clearance comes from the request (client-controlled)
public sealed record GetDocumentQuery(Guid Id, int ClearanceLevel) : IQuery<DocumentDto>;

// CORRECT -- clearance is resolved from trusted user store
public sealed class AppAttributeProvider : IAttributeProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken ct)
    {
        var user = await _userStore.GetAsync(userId, ct);
        return new Dictionary<string, object>
        {
            ["clearanceLevel"] = user.ClearanceLevel // From trusted DB
        };
    }
}
```

---

## See Also

- [Architecture](../xacml/architecture.md) -- XACML component architecture and request flow
- [Effects](../xacml/effects.md) -- Permit, Deny, NotApplicable, Indeterminate semantics
- [Conformance](conformance.md) -- XACML 3.0 conformance status
