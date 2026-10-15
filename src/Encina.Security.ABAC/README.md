# Encina.Security.ABAC

[![NuGet](https://img.shields.io/nuget/v/Encina.Security.ABAC.svg)](https://www.nuget.org/packages/Encina.Security.ABAC/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

XACML 3.0-based Attribute-Based Access Control (ABAC) engine for Encina. Provides fine-grained, context-aware authorization using attributes of the subject, resource, action, and environment — without XML.

## Features

- **XACML 3.0 Semantics** - Full policy evaluation model (PolicySet/Policy/Rule hierarchy, four-effect model, 8 combining algorithms)
- **No XML** - C#-idiomatic sealed records and fluent builders replace XACML XML
- **70+ Standard Functions** - Equality, comparison, arithmetic, string, logical, bag, set, higher-order, type conversion, regex
- **EEL (Encina Expression Language)** - Write policy conditions as inline C# boolean expressions compiled by Roslyn
- **Obligations & Advice** - Mandatory post-decision actions (XACML 3.0 section 7.18) with pluggable handlers
- **Decision Audit Trail** - Opt-in, write-ahead, fail-closed record of every enforced decision in the operation audit store
- **Full Observability** - OpenTelemetry tracing, counters, histograms and structured log events
- **Pipeline Integration** - Seamless integration with Encina's CQRS pipeline via `ABACPipelineBehavior`
- **Railway Oriented Programming** - All operations return `Either<EncinaError, T>`, no exceptions
- **Health Check** - Optional health check verifying PAP policy loading
- **.NET 10 Compatible** - Built with latest C# 14 features

## Installation

```bash
dotnet add package Encina.Security.ABAC
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncinaABAC(options =>
{
    options.EnforcementMode = ABACEnforcementMode.Block;
    options.AddHealthCheck = true;
});
```

### 2. Define Policies

#### Option A: Fluent Builder DSL

```csharp
services.AddEncinaABAC(options =>
{
    options.SeedPolicySets.Add(
        new PolicySetBuilder("finance-policies")
            .AddPolicy(new PolicyBuilder("approve-transactions")
                .ForResourceType<ApproveTransaction>()
                .AddRule(new RuleBuilder("finance-only", Effect.Permit)
                    .WithCondition(ConditionBuilder.And(
                        ConditionBuilder.Equal(
                            AttributeCategory.Subject, "department", XACMLDataTypes.String, "Finance"),
                        ConditionBuilder.LessThanOrEqual(
                            AttributeCategory.Resource, "amount", XACMLDataTypes.Integer, 50000)))
                    .Build())
                .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
                .Build())
            .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
            .Build());
});
```

#### Option B: EEL Inline Expressions

```csharp
[RequireCondition("user.department == \"Finance\" && resource.amount <= 50000")]
public sealed record ApproveTransaction(decimal Amount) : IRequest<ApprovalResult>;
```

### 3. Implement Attribute Providers

```csharp
public sealed class AppAttributeProvider(IHttpContextAccessor httpContext) : IAttributeProvider
{
    public Task<IReadOnlyDictionary<string, object?>> GetAttributesAsync(
        AttributeCategory category, CancellationToken ct = default)
    {
        var attrs = new Dictionary<string, object?>();

        if (category == AttributeCategory.Subject)
        {
            var user = httpContext.HttpContext?.User;
            attrs["department"] = user?.FindFirst("department")?.Value;
            attrs["clearanceLevel"] = int.Parse(user?.FindFirst("clearance")?.Value ?? "0");
        }

        return Task.FromResult<IReadOnlyDictionary<string, object?>>(attrs);
    }
}

// Register before AddEncinaABAC()
services.AddSingleton<IAttributeProvider, AppAttributeProvider>();
```

### 4. Decorate Request Types

```csharp
// Policy-based authorization
[RequirePolicy("finance-policies")]
public sealed record GetFinancialReport(Guid ReportId) : IQuery<ReportDto>;

// Inline expression authorization
[RequireCondition("user.roles.Contains(\"admin\") || user.clearanceLevel >= 5")]
public sealed record AccessClassifiedDocument(Guid DocId) : IQuery<DocumentDto>;

// A policy and a condition on the same request: both must hold (AND)
[RequirePolicy("global-security")]
[RequireCondition("environment.isBusinessHours")]
public sealed record TransferFunds(decimal Amount) : ICommand<TransferResult>;
```

### How the attributes decide a request

- `[RequirePolicy("name")]` evaluates the top-level policy set or standalone policy (one contained in no set) with that id on its own, through `IPolicyDecisionPoint.EvaluatePolicyAsync`; the rest of the policy store is not evaluated. Only `Permit` passes. `Deny` and `NotApplicable` deny, because an explicitly required policy that does not apply cannot authorize. A name that matches no top-level set or standalone policy, including a policy that exists only nested inside a set, denies with `encina.authorization.abac_policy_not_found`; to require a nested policy, name its parent set. `Indeterminate` denies with `abac.indeterminate` and an exception from the attribute provider or the PDP with `abac.evaluation_failed` (fixed message, exception type in the details), in every enforcement mode (`Warn` relaxes only definite verdicts such as a Deny). An anonymous caller (the `IRequestContext.Identity` of the request is not authenticated) is denied in `Block` and `Warn` mode, before any attribute is collected, with `ABACErrors.UnauthenticatedCaller` (code `encina.authorization.unauthenticated`; details `gate`, `requestType` and `requirement`); `Disabled` mode skips the check. With the [decision audit](#decision-audit) enabled the denial is always recorded, with a `null` `UserId`, whatever `Outcomes` selects.
- Several `[RequirePolicy]` attributes: every one with `AllMustPass = true` (the default) must permit, and when there is at least one with `AllMustPass = false`, at least one of those must permit. Both groups must hold.
- `[RequireCondition("expression")]` is an EEL expression evaluated per request, after the required policies permit, against the variables `user`, `resource`, `environment` and `action` (built from the `IAttributeProvider` dictionaries; `action.name` is the request type name). Resource attributes come from `IAttributeProvider.GetResourceAttributesAsync(request)`. A `false` result denies with `encina.authorization.abac_condition_not_met`; a compile or evaluation error is `Indeterminate` and denies with `abac.indeterminate`.
- Policies and conditions combine with AND. A request with neither attribute is not evaluated and passes through.
- The enforcement point reads `IRequestContext.Identity` once; `AddEncinaABAC` registers the request identity services (`AddEncinaRequestIdentity`), so no `Encina.Security` registration is needed. In HTTP, call `app.UseEncinaContext()` so the context carries the authenticated user. In a background job or scheduled message, declare a service identity (`AddEncinaServiceIdentity("name")`) and run the work inside `IRequestContextScopeFactory.RunAsServiceAsync`. A user and a declared service identity are both evaluated. The enforcement point always adds the subject attributes `subject-id` (`RequestIdentity.UserId`, for example `service:<name>` for a service) and `identity-kind` (`user` or `service`), constants in `ABACSubjectAttributes`, after the ones from `IAttributeProvider.GetSubjectAttributesAsync(RequestIdentity identity, ...)`, so a provider cannot replace them. A policy selects them with a Subject attribute designator whose `AttributeId` is `subject-id` or `identity-kind` (`ABACSubjectAttributes.SubjectId`, `ABACSubjectAttributes.IdentityKind`), and an EEL condition (`[RequireCondition]`, where `user` is a dictionary of subject attributes) reads them by name, for example `((System.Collections.Generic.IDictionary<string, object>)user)["identity-kind"].ToString() == "service"`.

## Two Authorization Models

| Feature | EEL (Inline Expressions) | XACML Expression Trees |
|---------|--------------------------|------------------------|
| **Syntax** | `"user.department == \"Finance\""` | `Apply` + `AttributeDesignator` nodes |
| **Best for** | Simple-to-moderate conditions | Complex, composable policies |
| **Definition** | `[RequireCondition]` attribute | Fluent builders / PAP API |
| **Compilation** | Roslyn at startup (~50-100ms) | Immediate (C# objects) |
| **Combining** | AND between multiple attributes | 8 XACML combining algorithms |
| **Obligations** | Not supported | Full XACML 3.0 support |

## Combining Algorithms

| Algorithm | Behavior |
|-----------|----------|
| `DenyOverrides` | Any Deny wins (recommended default) |
| `PermitOverrides` | Any Permit wins |
| `FirstApplicable` | First matching rule wins |
| `OnlyOneApplicable` | Exactly one must match |
| `DenyUnlessPermit` | Default deny, no edge cases |
| `PermitUnlessDeny` | Default permit |
| `OrderedDenyOverrides` | DenyOverrides with deterministic obligation order |
| `OrderedPermitOverrides` | PermitOverrides with deterministic obligation order |

## Obligations & Advice

Obligations are post-decision actions that policy authors attach to a decision, for example sending a notification when a record is read. Implement an `IObligationHandler` for each obligation id and register it in DI; the handler contract is in [Obligations & Advice](../../docs/features/abac/xacml/obligations-advice.md). To keep a record of who was allowed or denied what, use the decision audit below, not an obligation.

Per XACML 3.0 section 7.18: if a mandatory obligation handler fails or is missing, access is automatically denied; there is no option to relax this, and advice without a handler is skipped. A handler that throws does not escape the pipeline: the exception becomes `abac.obligation_handler_exception` (fixed message, exception type in the error details) inside the executor, so a mandatory obligation denies with `encina.authorization.abac_obligation_failed` and advice is skipped. The exception message is never logged. Cancellation of the request token still propagates.

## Decision Audit

The enforcement point can write one audit row for every decision it enforces: who asked (`UserId`, `service:<name>` for a service identity), for which request type and resource, what ABAC decided and why. It is opt-in and off by default. Rows are `OperationAuditEntry` values stored through the application's `IOperationAuditStore` ([ADR-036](../../docs/architecture/adr/036-three-audit-stores.md)); ABAC adds no store, table or provider code. The design is [ADR-047](../../docs/architecture/adr/047-abac-decision-audit.md).

```csharp
services.AddEncinaSecurity();   // before AddEncinaABAC, see "Registration order" below
services.AddEncinaABAC(options => options.AuditDecisions(audit =>
{
    audit.Outcomes = ABACDecisionAuditOutcomes.Denied | ABACDecisionAuditOutcomes.NotEnforced;
}));
// The application or a provider package registers a persistent IOperationAuditStore.
```

A request declares the resource it touches with `IABACResourceIdentity` (or a resource attribute named by `ResourceIdAttributeName`, default `resourceId`); a request that declares neither leaves `EntityId` empty:

```csharp
public sealed record GetPatientRecordQuery(Guid PatientId) : IQuery<PatientRecord>, IABACResourceIdentity
{
    public string? ResourceId => PatientId.ToString();
}
```

What a row carries:

- `Action` is `ABACDecision`; `EntityType` is the request type name; `EntityId` is the declared resource id; `CorrelationId` and `TenantId` come from the request context.
- `Outcome` is `Success` when ABAC let the request through (a Warn-mode would-deny included: its metadata says `abac.enforced=false`), `Denied`, or `Error` for an indeterminate decision or an evaluation failure.
- `ErrorMessage` holds the reason code only; the metadata holds the deciding policy and rule ids, the evaluation trace, attribute names (values only for `RecordedAttributeValues`) and `abac.identity_kind`. No error or exception message is ever stored.
- The write happens before the handler runs. By default (`FailClosed`) a request that would proceed is denied with `abac.decision_audit_failed` when its row cannot be written; `BestEffort` is the logged opt-out.

Read the trail with `IABACDecisionAuditReader` (query and JSON Lines export). Put it behind your own authorization.

### Registration order

Pipeline behaviors run in registration order. Call `AddEncinaSecurity` before `AddEncinaABAC` so the security gates (`[DenyAnonymous]`, roles, permissions) run first and ABAC rows exist only for requests that passed them. Encina logs no warning for the opposite order; the execution-order contract is tracked in [#1783](https://github.com/dlrivada/Encina/issues/1783). Either way, an ABAC row records the ABAC decision, not the final outcome of the request: a later behavior or the handler can still fail, and a denial by another gate is not in the ABAC row.

### Limits

- No row means "not evaluated" (ABAC disabled, or a request with no ABAC attribute), never "granted".
- A direct call to `IPolicyDecisionPoint` is not audited; only requests that pass through the enforcement point are.
- Obligations and advice run before the row is written, so their side effects stay when a `FailClosed` write then denies the request.
- Relational and MongoDB stores keep ids and metadata in plaintext; retention is the operation audit store's (`OperationAuditOptions.RetentionDays`, default 2555; `EnableAutoPurge`, default `false`).

The setup steps are in [How to audit ABAC decisions](../../docs/features/abac/audit-decisions.md); the entry shape, limits and errors are in the [decision audit reference](../../docs/features/abac/reference/decision-audit.md).

## Configuration

| Option | Default | Description |
|--------|---------|-------------|
| `EnforcementMode` | `Block` | `Block`, `Warn` (definite denials are logged and proceed; errors, including an anonymous caller, still deny when they decide the verdict), or `Disabled` |
| `IncludeAdvice` | `true` | Execute advice expressions after decision |
| `ValidateExpressionsAtStartup` | `false` | Pre-compile all EEL expressions at startup |
| `AddHealthCheck` | `false` | Register ABAC health check |
| `DecisionAudit` | disabled | Decision audit trail; `AuditDecisions(...)` turns it on. Options in the [configuration reference](../../docs/features/abac/reference/configuration.md#decisionaudit-options) |

## Observability

- **Tracing**: `Encina.Security.ABAC` ActivitySource with `ABAC.Evaluate` spans
- **Metrics**: counters (`abac.evaluation.*`, `abac.obligation.*`, `abac.advice.*`) + 2 histograms (`abac.evaluation.duration`, `abac.obligation.duration`); with the decision audit enabled also `abac.decision_audit.recorded`, `abac.decision_audit.failed` and `abac.decision_audit.duration` (alert on any increase of `failed`)
- **Logging**: structured log events (EventIds allocated inside 9000-9099, with unused ids 9006-9007, 9016-9019, 9023-9029, 9041-9049 and 9056-9057 to reuse first; the ABAC decision audit trail (#751; see the observability reference) uses 9079-9090 and 9098-9099: 9084, 9086 and 9087 are its startup check, 9085 (`EnforcementDisabled`, logged once at startup when `EnforcementMode` is `Disabled`) is the enforcement-mode startup check, 9090 warns about a cross-tenant query, 9098 reports a stored entry that could not be read and 9099 a failed write that could not be confirmed, and 9094, 9095, 9096 and 9097 come from `PersistentPolicyAdministrationPoint` (9094, 9095, 9097) and `ABACPolicySeedingHostedService` (9096, the seeding summary); `EventIdRanges.SecurityABAC`) via `[LoggerMessage]` source generator; errors are logged by code or exception type, never by message
- **Health Check**: `encina-abac` with tags `encina`, `security`, `abac`, `ready`

## Documentation

- [Quick Start Guide](../../docs/features/abac/quick-start.md)
- [Tutorials (8 scenarios)](../../docs/features/abac/tutorials.md)
- [XACML Architecture](../../docs/features/abac/xacml/architecture.md)
- [Policy Language](../../docs/features/abac/xacml/policy-language.md)
- [Combining Algorithms](../../docs/features/abac/xacml/combining-algorithms.md)
- [EEL Guide](../../docs/features/abac/eel/guide.md)
- [EEL Cookbook (17 patterns)](../../docs/features/abac/eel/cookbook.md)
- [Fluent DSL Guide](../../docs/features/abac/dsl-guide.md)
- [How to audit ABAC decisions](../../docs/features/abac/audit-decisions.md)
- [Decision Audit Reference](../../docs/features/abac/reference/decision-audit.md)
- [Configuration Reference](../../docs/features/abac/reference/configuration.md)
- [Error Reference](../../docs/features/abac/reference/errors.md)
- [Observability Reference](../../docs/features/abac/reference/observability.md)
- [Function Library (70+)](../../docs/features/abac/reference/function-library.md)
- [Cheat Sheet](../../docs/features/abac/reference/cheat-sheet.md)

## Architecture Decision Records

- [ADR-015: XACML 3.0 as ABAC Foundation](../../docs/architecture/adr/015-xacml-3.0-abac-foundation.md)
- [ADR-016: Roslyn Scripting for Expression Language](../../docs/architecture/adr/016-roslyn-expression-language.md)
- [ADR-017: EEL Naming and Design](../../docs/architecture/adr/017-eel-naming-design.md)
- [ADR-036: Three Purpose-Named Audit Stores](../../docs/architecture/adr/036-three-audit-stores.md)
- [ADR-047: ABAC Decisions Are Audited Write-Ahead at the Enforcement Point, Fail-Closed](../../docs/architecture/adr/047-abac-decision-audit.md)

## Related Packages

- [`Encina.Security`](../Encina.Security/README.md) - Core RBAC security (can be combined with ABAC)
- [`Encina.Security.Encryption`](../Encina.Security.Encryption/README.md) - Field-level encryption
- [`Encina.Security.AntiTampering`](../Encina.Security.AntiTampering/README.md) - Request integrity verification

## License

[MIT](../../LICENSE)
