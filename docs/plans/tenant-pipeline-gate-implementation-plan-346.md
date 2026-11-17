# Implementation Plan: Tenant gate in the request pipeline (multi-tenancy core, remaining work)

> **Issue**: [#346](https://github.com/dlrivada/Encina/issues/346)
> **Type**: Feature (the open umbrella of the multi-tenancy core, child of the EPIC #876; most of its text is already implemented in `Encina.Tenancy`)
> **Complexity**: Medium (3 phases of code, 1 testing and documentation phase, 3 worker pull requests)
> **Estimated Scope**: ~7 production files, ~6 test files, ~9 metadata and documentation files
> **Related plans** (cited by issue number): #1257 (shared tenant-scope primitive, the default tenant, the tenant-id rules and the isolation suite), #798, #845, #1636, #752 (merged plans of the multitenancy-3 family), #338 (tenant existence and status, planned in this family), #304 (ownership validation, planned in this family), #385 (EF filter for any context and the security guide, planned in this family), #125 (messaging dispatch guard, planned in this family), #711 (tenant connection cache), #2676 (tenant telemetry helper)

---

## Summary

#346 asked for "a new package `Encina.MultiTenancy`" with resolvers, a tenant context, tenant attributes, two pipeline behaviors, database strategies and an EF Core integration. The package that exists is `Encina.Tenancy` (plus `Encina.Tenancy.AspNetCore` and the `Tenancy/` folders of the EF Core, Dapper, ADO.NET and MongoDB packages), and it already delivers most of the text. This plan records, item by item, what is done and what remains, and plans only the remainder: **a tenant gate in the request pipeline** that denies a request with no tenant on every entry point, not only on HTTP.

**State of the code on `ec129cae5` (current `origin/main`; the cited files were read on it).**

| Item of #346 | State | Evidence or owner |
|--------------|-------|-------------------|
| Resolvers: header, subdomain, claim, route, composite fallback chain | Done | `src/Encina.Tenancy.AspNetCore/Resolution/Resolvers/*.cs`, `TenantResolverChain.cs` |
| Query-string resolver | Not built; the doc example in `Encina.Tenancy.AspNetCore/ServiceCollectionExtensions.cs:134` names a `QueryStringTenantResolver` that does not exist | Design Choice 5 |
| `TenantContext` record | Done as `TenantInfo` (id, name, isolation strategy, connection string masked from logs, schema) | `src/Encina.Tenancy/Abstractions/TenantInfo.cs` |
| `ITenantContextAccessor` | Done as `IRequestContextAccessor` (ambient `IRequestContext.TenantId`) and `ITenantProvider` | `src/Encina/Abstractions/IRequestContextAccessor.cs`, `src/Encina.Tenancy/Abstractions/ITenantProvider.cs` |
| `TenantResolutionPipelineBehavior` | Not needed: resolution happens at the entry points (middleware; background restore by #1164), a behavior has no source to resolve from | Design Choice 1 |
| `TenantIsolationPipelineBehavior`, `[TenantIsolated]`, `[TenantAgnostic]` | **Missing: this plan** (`[TenantAgnostic]` and the gate; `[TenantIsolated]` is the default, not an attribute) | Design Choices 1 and 2 |
| `[CrossTenant]` | Not built here: the only cross-tenant path is the audited platform-operator permission | Design Choice 6 |
| Database-per-tenant, schema-per-tenant, shared database | Done | `TenantIsolationStrategy`, `ITenantConnectionFactory`, the per-provider `TenantConnectionFactory` |
| EF Core query filters | Done for `TenantDbContext` descendants; any other context is #385 | `src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs` |
| Tenant caching for performance | #711 (connection strings and filter expressions) and #596 (persistent store) | open issues |
| `OnTenantNotFound = UseDefault` or `Redirect` | Not built: a fallback tenant is forbidden | Design Choice 6 |

**What is missing, by reading the code.** `TenantResolutionMiddleware` is HTTP only. A request dispatched through `IEncina.Send` from a hosted service, a gRPC or GraphQL bridge that falls back to an anonymous context (#1164, comment), a CLI or a test host reaches its handler with no tenant, and nothing denies it: the repository filters return nothing for a null tenant, but handlers that call other stores, caches or the file system carry on. `AGENTS.md` section 3 says a gate with missing context denies. Notifications (`Publish`) run no pipeline behaviors; the notification path is the dispatch guard of #125.

**Scope.** `TenantAgnosticAttribute`, `TenantGatePipelineBehavior<TRequest, TResponse>` and its stream twin, registration in `AddEncinaTenancy`, one counter, three log messages in a new range, the documentation fix and the documentation of the gate. **Out of scope**: existence and status of the tenant (#338), ownership of referenced resources (#304), the EF filter for any context (#385), message dispatch (#125), the tenant tag helper (#2676), the platform-operator path (#3748).

**Affected packages**: `Encina.Tenancy` (behaviors, diagnostics), `Encina` (the `[TenantAgnostic]` attribute and the EventId range registration), `Encina.Tenancy.AspNetCore` (documentation comment only). **Provider category**: none (the gate reads the request context; no provider-specific code).

---

## Design Choices

<details>
<summary><strong>1. Where the tenant is resolved and where it is enforced</strong></summary>

**Class:** Settled
**Settled by:** `ITenantResolver` takes an `HttpContext` (`src/Encina.Tenancy.AspNetCore/Resolution/ITenantResolver.cs`), so resolution cannot run inside a pipeline behavior that receives only `IRequestContext`; the persisted-context restore of background work is #1164; `AGENTS.md` section 3 (gates fail closed when context is missing).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Resolve at each entry point (HTTP middleware, #1164 restore, hosts that set the context), enforce in one pipeline behavior** | Each entry point resolves from what it has; one enforcement point for every entry point | The behavior cannot repair a missing tenant, only deny |
| **B) A `TenantResolutionPipelineBehavior` that resolves inside the pipeline (issue text)** | One class resolves for everyone | There is no source to resolve from inside the pipeline (no `HttpContext`, no claims, no header); it would need a second non-HTTP resolver abstraction for no entry point that lacks a tenant |

### Chosen Option: **A — Resolve at the entry points, enforce in the pipeline**

### Rationale

- B would duplicate `TenantResolutionMiddleware` for a pipeline that already carries the tenant; the only requests that reach the pipeline without one are those no resolver can help, and those must be denied.

</details>

<details>
<summary><strong>2. Default of the gate: deny a request with no tenant unless its type opts out</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed; the only opt-out is explicit and logged); `TenancyOptions.RequireTenant` defaults to `true` (`src/Encina.Tenancy/Configuration/TenancyOptions.cs`); the maintainer principle of 2026-10-30 recorded in #3111.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every request needs a tenant; `[TenantAgnostic]` on the request type is the explicit, logged opt-out; `TenancyOptions.RequireTenant = false` (existing explicit opt-out of the middleware) turns the gate off and logs it once** | Fail closed; a forgotten attribute can only deny, never leak | Platform requests (health, migrations, tenant administration) must carry `[TenantAgnostic]` |
| **B) Opt-in `[TenantIsolated]` on the requests that need a tenant (issue text)** | No change for existing requests | Fail open: a request type without the attribute runs with no tenant; a forgotten attribute leaks |

### Chosen Option: **A — Deny by default, `[TenantAgnostic]` as the explicit opt-out**

### Rationale

- The attribute `[TenantIsolated]` of the issue is the default behavior and is not created (no line serves a purpose, `AGENTS.md` section 3). The attribute sits on the request type, not the handler, because the behavior sees the request type before the handler is resolved; it is cached per closed generic type like the other attribute-driven behaviors (`ProcessorValidationPipelineBehavior<,>` keeps a static `ConcurrentDictionary<Type, ...>`).

</details>

<details>
<summary><strong>3. Pipeline stage and order of the gate</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PipelineStage.Authorization`, `Order = 5` (the stage and order of the `TenantGateBehavior` example in `PipelineStage.cs`)** | Runs after `Audit` (300) and `Authentication` (400), so a denial is audited with the identity, and before ABAC and the compliance stages that read the tenant | The stage vocabulary is declared but "not yet applied by the pipeline builders" (#2184 phase note), so until it is applied the order is the registration order |
| **B) `PipelineStage.Authentication`** | Earlier: no authorization work for a tenantless request | A missing tenant is a denial, not an identity question; mixes with signature and token verification |
| **C) `PipelineStage.DataRouting` ("tenant or shard selection")** | Next to the data source choice | Compliance and ABAC behaviors run before it with no tenant gate in front of them |

### Chosen Option: **A — Authorization, Order 5** (recommended, pending the maintainer)

### Rationale

- A wins when the tenant must be present before any policy reads it (ABAC, consent, retention all do); C wins if the only consumer of the tenant is the data layer and policy behaviors are tenant-agnostic. The attribute `[PipelineStage(PipelineStage.Authorization, Order = 5)]` is declared now so the position is correct the day the builders apply the stages; #338 and #304 use `Order = 6` and `Order = 7`.

</details>

<details>
<summary><strong>4. How the gate decides "no tenant": the behavior's own check, or the shared primitive of #1257</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The behavior calls `ITenantScopeResolver.ResolveRead(null)` of #1257 (PR 1257-A1)** | One rule for every consumer: blank is no tenant, the reserved `default` id, the 128-character limit and padded-id rejection live in one place | Hard prerequisite on a merged #1257-A1 whose Design Choices 1, 2 and 8 are still open |
| **B) The behavior checks `string.IsNullOrWhiteSpace(context.TenantId)` itself and the format rules stay in the options validator** | Independent of #1257; small | A third copy of the rule next to `DefaultConsentService` and `ABACDecisionAuditReader`, the drift #1257 sets out to remove |

### Chosen Option: **A — Use the shared primitive** (recommended, pending the maintainer)

### Rationale

- A wins when #1257-A1 merges before this plan's PR (its declared role is the hard prerequisite of every sibling); B wins if the maintainer answers #1257 Design Choice 1 with "keep the rule inside each package", in which case the gate keeps its own check and #1257's primitive is not a dependency. The code is written so the decision lives in one private method (`HasTenant`), changed in one place.

</details>

<details>
<summary><strong>5. A query-string tenant resolver</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `QueryStringTenantResolver`, disabled by default, with the same trust rule as the header resolver (the verified claim always wins; a mismatch is 403)** | Works where a client cannot set a header: browser links, WebSocket and Server-Sent Events handshakes | A client-supplied value in the URL is logged, cached, shared and sent in referrers; every use is a spoofing surface |
| **B) Do not add it; fix the documentation example; a host that needs it registers its own `ITenantResolver` (`AddTenantResolver<T>()` exists)** | Smaller surface; no tenant in URLs | A host with browser links writes its own resolver |

### Chosen Option: **B — Not added** (recommended, pending the maintainer)

### Rationale

- A wins for deployments whose only client is a browser without a gateway that can add a header; B wins everywhere else, and the existing extension point covers A at the cost of one class in the host. The documentation example at `ServiceCollectionExtensions.cs:134` is corrected in PR 346-B so it names a custom resolver, not a type that does not exist.

</details>

<details>
<summary><strong>6. The cross-tenant attribute and the "use the default tenant" fallback</strong></summary>

**Class:** Settled
**Settled by:** the maintainer decision recorded in the register of [#3111](https://github.com/dlrivada/Encina/issues/3111) (gate G3: cross-tenant access only through the audited platform-operator permission with a mandatory reason, never a boolean), the open issue #3748 that creates that path, and the maintainer comment of 2026-10-17 on #1257 ("a missing tenant configuration fails closed and never falls back to another tenant").

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No `[CrossTenant]` attribute and no `UseDefault` or `Redirect` fallback; the platform-operator path is #3748** | Complies with the decisions above; no silent cross-tenant switch | A platform request that needs several tenants waits for #3748 |
| **B) `[CrossTenant]` and `OnTenantNotFound = UseDefault` as written in #346** | Matches the issue text | An attribute that turns the gate off without audit and a fallback to another tenant, both contradicted by the cited decisions |

### Chosen Option: **A — Not built here**

### Rationale

- Only A complies. A request that must see no tenant at all uses `[TenantAgnostic]`; one that must read across tenants uses the operator permission of #3748 when it exists.

</details>

<details>
<summary><strong>7. The error returned and its category</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `TenancyErrors.TenantRequired()`: code `tenancy.tenant_required`, `ErrorCategory.InvalidRequest` (ADR-062 R3, the `TenantResolutionMiddleware` 400 precedent)** | One code per condition, already asserted by tests | #1257 proposes `encina.tenancy.tenant_required` for the same condition, so the two plans use different strings |
| **B) Create the #1257 code `encina.tenancy.tenant_required` and keep both** | Matches #1257 | Two codes for one condition; clients must match both |

### Chosen Option: **A — Reuse the existing code** (recommended, pending the maintainer)

### Rationale

- A wins on one-code-per-condition; B wins if the maintainer prefers the `encina.` prefix for every new code. The conflict is recorded in a follow-up issue file (`artifacts/issues/tenancy-error-code-prefix.md`) so #1257 and the existing code are reconciled once, in either direction.

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions & Models (PR 346-A)

> **Goal**: the opt-out attribute and the gate.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina/Abstractions/TenantAgnosticAttribute.cs`**: `[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)] public sealed class TenantAgnosticAttribute : Attribute` (namespace `Encina`, in the core assembly, like `MultiTenancyMarker`: `Encina.Tenancy` references `Encina.Messaging` (`src/Encina.Tenancy/Encina.Tenancy.csproj`), so an attribute defined in `Encina.Tenancy` could not be read by a messaging processor; #338, #304 and #125 all read this one attribute), XML docs with an `<example>` for a platform request and the rule "everything else needs a tenant".
2. **Create `src/Encina.Tenancy/Gate/TenantGatePipelineBehavior.cs`**: `[PipelineStage(PipelineStage.Authorization, Order = 5)] public sealed class TenantGatePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>`; constructor `(IOptions<TenancyOptions> options, ITenantScopeResolver scope, ILogger<TenantGatePipelineBehavior<TRequest, TResponse>> logger)` (the `ITenantScopeResolver` argument follows Design Choice 4 A; under B it is dropped). `Handle`: if `!RequireTenant` pass (logged once per closed type, EventId 7852); if the request type carries `[TenantAgnostic]` pass (logged once per closed type, EventId 7851); `HasTenant(context)` false returns `Left(TenancyErrors.TenantRequired())`, increments `encina.tenancy.gate.denied` and logs EventId 7850 by code; otherwise `nextStep()`. A private static `ConcurrentDictionary<Type, bool>` caches the attribute lookup.
3. **Create `TenantGateStreamPipelineBehavior<TRequest, TItem>`** (`IStreamPipelineBehavior<TRequest, TItem>`, same stage and order): a stream request that fails the gate yields one `Left(TenancyErrors.TenantRequired())` and ends. Without it a stream request would bypass the gate (fail closed).
4. **Create `src/Encina.Tenancy/Diagnostics/TenantGateMetrics.cs`** (`internal static`): a counter `encina.tenancy.gate.denied` on the shared `Encina` meter (ADR-054 naming: lowercase dot-separated, no `_total`), tag `outcome` = `tenant_required`; `RecordDenied()`. It is a static instrument, not a `TenancyMetrics` instance, because `TenancyMetrics` is created only by the OpenTelemetry hosted service (`src/Encina.OpenTelemetry/Tenancy/TenancyMetricsInitializer.cs`) and is not registered in the container, so a behavior cannot inject it. No tenant tag: a denial has no tenant by definition (ADR-054 D4 applies to a resolved tenant; the tag helper is #2676).
5. **Create `src/Encina.Tenancy/Diagnostics/TenancyPipelineLog.cs`** (`[LoggerMessage]` source generator, EventIds 7850, 7851, 7852 of the new range of Phase 3); messages carry the request type name and never a tenant value.
6. PublicAPI lines for the attribute and the two behaviors (mechanical-fixer); the metrics class is internal.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #346, plan tenant-pipeline-gate-implementation-plan-346.md. Encina.Tenancy has resolution, a provider, a store and isolation strategies but no pipeline behavior. Design Choices 1, 2, 3, 4 and 7 define the gate; read them first.
- The pipeline contract is IPipelineBehavior<TRequest, TResponse>.Handle(request, context, nextStep, cancellationToken) returning ValueTask<Either<EncinaError, TResponse>>.

TASK:
Create TenantAgnosticAttribute, TenantGatePipelineBehavior and TenantGateStreamPipelineBehavior, add the denied counter (TenantGateMetrics) and the log class. No existing file is changed in this phase.

KEY RULES:
- Fail closed: no tenant denies unless the request type has [TenantAgnostic] or RequireTenant is false; both opt-outs are logged once per closed type.
- ROP: Left(TenancyErrors.TenantRequired()), no exceptions; the message carries no tenant value; logs carry the request type and the error code, never EncinaError.Message.
- Static per-generic-type attribute cache (ConcurrentDictionary<Type, bool>); no allocation on the allowed path.
- XML docs on every public member; zero warnings; CRAP <= 10 per method (Handle delegates to small private methods).

REFERENCE FILES:
- src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs (attribute cache, Handle shape)
- src/Encina.Tenancy/TenancyErrors.cs
- src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs (meter and instrument naming)
- src/Encina/Pipeline/Ordering/PipelineStage.cs
```

</details>

### Phase 2: Registration and Configuration (PR 346-A)

> **Goal**: `AddEncinaTenancy` registers the gate; the resolver dependency is satisfied in any order.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Tenancy/ServiceCollectionExtensions.cs`** (`AddEncinaTenancy`): `services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(TenantGatePipelineBehavior<,>)))` and the stream twin; the counter needs no registration (static). Behaviors are transient and hold no scoped service (`IOptions` and the singleton resolver only).
2. **DI test** `tests/Encina.UnitTests/Tenancy/ServiceCollectionExtensionsTests.cs`: build the provider with `ValidateOnBuild` and `ValidateScopes`, `AddEncina` before and after `AddEncinaTenancy` (registration completeness, `AGENTS.md` section 3); assert one gate registration per open generic.
3. No new option class: the gate reads `TenancyOptions.RequireTenant`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Phase 1 merged in the same pull request. AddEncinaTenancy registers the marker, the store and the provider.

TASK:
Register the two gate behaviors with TryAddEnumerable (transient); extend the DI test.

KEY RULES:
- Registration completeness: every dependency of the behaviors resolves with ValidateOnBuild and ValidateScopes, in both registration orders; no scoped service in a singleton.
- TryAdd only; never override an application registration.

REFERENCE FILES:
- src/Encina.Tenancy/ServiceCollectionExtensions.cs
- src/Encina.Compliance.ProcessorAgreements/ServiceCollectionExtensions.cs (TryAddEnumerable of a behavior)
- tests/Encina.UnitTests/Tenancy/ServiceCollectionExtensionsTests.cs
```

</details>

### Phase 3: Observability (PR 346-A)

> **Goal**: the new EventId range, registered first.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina/Diagnostics/EventIdRanges.cs`**: `public static readonly (int Min, int Max) TenancyPipeline = (7850, 7879);` with an XML comment naming the owners (this plan 7850-7852, #338 7853-7854, #304 7855-7856; 7857-7879 spare). The range comes from the block assigned to this family (7850-7899); `Encina.Tenancy` already maps to `Tenancy` (1800-1899), and the assembly now maps to both ranges (the same pattern as `Encina.Compliance.AIAct`).
2. **Modify `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`**: add `TenancyPipeline` to the `Encina.Tenancy` entry of `AssemblyRanges`.
3. Log messages (Phase 1 task 5): 7850 gate denied (Warning, request type), 7851 request type is `[TenantAgnostic]` (Information, once per type), 7852 gate inactive because `RequireTenant` is false (Warning, once per type). Packed from 7850, no gaps.
4. The counter of Phase 1 task 4; no span (hot path, one comparison).
5. `PublicAPI.Unshipped.txt` of `Encina` for the new range field and for `TenantAgnosticAttribute` (mechanical-fixer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- ADR-021: a range is registered in EventIdRanges.cs before any [LoggerMessage] uses it. The family block is 7850-7899 (artifacts/decisions/plan-batch/eventid-blocks.md); this plan owns the range TenancyPipeline.

TASK:
Register the range, map the assembly in EncinaEventIdAllocationTests, write TenancyPipelineLog with EventIds 7850-7852 and run the architecture tests.

KEY RULES:
- Pack EventIds sequentially from 7850; no id outside the range; XML docs name the range.
- Codes and request type names only in messages: never EncinaError.Message, never a tenant value.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Tenancy.AspNetCore/Diagnostics/TenancyAspNetCoreLogMessages.cs
```

</details>

### Phase 4: Testing, Documentation & Finalization (PR 346-A tests; PR 346-B documentation)

> **Goal**: every flag at its target, the documentation says what the code does.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** `tests/Encina.UnitTests/Tenancy/Gate/TenantGatePipelineBehaviorTests.cs`: tenant present passes; null, empty and whitespace deny; `[TenantAgnostic]` passes with no tenant; `RequireTenant = false` passes; the counter increments once per denial (in-memory meter listener); the log is emitted once per closed type for each opt-out; the stream twin yields one `Left` and no item; the attribute cache is stable across calls.
2. **Guard tests** `tests/Encina.GuardTests/Tenancy/TenantGatePipelineBehaviorGuardTests.cs`: the constructor rejects each null argument.
3. **Property test** (FsCheck, `tests/Encina.PropertyTests/Database/Tenancy/`): for any string that is null, empty or whitespace the gate denies; for any non-blank string it passes the request through.
4. **Load test**: justification `.md` (`tests/Encina.LoadTests/Tenancy/TenantGate.md`): the behavior holds no shared mutable state beyond a read-mostly cache and does no I/O.
5. **Benchmark**: justification `.md` (`tests/Encina.BenchmarkTests/Tenancy/TenantGate.md`): one string check on the hot path; the allocation claim is covered by a unit test that asserts the allowed path allocates nothing beyond the delegate call.
6. **Integration**: justification in the unit test class comment: no database or broker is involved (a non-database feature, `AGENTS.md` section 9); the end-to-end behavior with the middleware is added to `tests/Encina.UnitTests/Tenancy/AspNetCore/TenantResolutionMiddlewareTests.cs`-style host tests in PR 346-B.
7. **Documentation (PR 346-B, docs-writer)**: `docs/features/multi-tenancy.md` gains a "The tenant gate" section (what is denied, `[TenantAgnostic]`, the `RequireTenant` opt-out, streams, notifications are #125) and the FAQ "How do I handle admin/system operations without tenant context?" is rewritten to point at the attribute; the `QueryStringTenantResolver` example in `src/Encina.Tenancy.AspNetCore/ServiceCollectionExtensions.cs` is replaced by a custom-resolver example; `src/Encina.Tenancy/README.md` mentions the gate.
8. **Changelog fragment** `changelog.d/346-tenant-pipeline-gate.added.md` (mechanical-fixer); manifests `Encina.Tenancy.json` and `Encina.json` through mechanical-fixer with the per-file targets of the Testing table.
9. Build with 0 warnings; every flag at its target.

**Per-file targets** (proposed; measured values replace them and never go down; "new" means no manifest entry yet; the package default of `Encina.Tenancy.json` is guard 15, unit 60):

| File | Baseline | Flags | Justification |
|------|----------|-------|---------------|
| `Gate/TenantGatePipelineBehavior.cs` (new) | new | unit 100, guard 90, property 80 | every branch (tenant present, blank, attribute, `RequireTenant` false) is a unit case; guard reaches the constructor checks; the property test covers the blank-or-not decision. |
| `Gate/TenantGateStreamPipelineBehavior.cs` (new) | new | unit 100, guard 90 | one denied and one allowed stream, both asserted item by item. |
| `Encina/Abstractions/TenantAgnosticAttribute.cs` (new) | new | unit 0 | an attribute with no executable line; its effect is asserted through the behavior. |
| `Diagnostics/TenantGateMetrics.cs` (new) | new | unit 100 | one counter read through an in-memory meter listener. |
| `Diagnostics/TenancyPipelineLog.cs` (new) | new | unit 90 | generated logging methods exercised with a fake logger; the generated partial cannot be fully reached. |
| `ServiceCollectionExtensions.cs` | unit 95, guard 70 | unchanged | two more registrations in an already covered method; the DI test proves them. |
| `Encina/Diagnostics/EventIdRanges.cs` | none | unit 0 | one static field, covered by the architecture tests that scan it; a 0 because the field has no executable line. |

CRAP gate: `Handle` delegates to three small private methods (opt-out, denial, allow), each at complexity 3 or less.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Code phases of PR 346-A are written. Per-flag coverage is measured separately (AGENTS.md section 9); tests execute real code.

TASK:
Write the tests of the list, delegate the manifest entries, PublicAPI lines and the fragment to mechanical-fixer, the pages to docs-writer, then measure per-flag coverage of the new files.

KEY RULES:
- Shared fixtures and Shouldly through Encina.Testing.Shouldly; no Thread.Sleep; no reflection-only tests.
- Justification .md only for load and benchmark; unit, guard and property are never justified away.
- Never edit CHANGELOG.md; coverage figures in documentation are cited by marker, never typed.

REFERENCE FILES:
- tests/Encina.UnitTests/Tenancy/ServiceCollectionExtensionsTests.cs
- .github/coverage-manifest/Encina.Tenancy.json
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Use in this feature |
|--------|-------------|---------------------|
| `AGENTS.md` section 3 | gates fail closed when context is missing; the opt-out is explicit and logged | Design Choice 2 |
| SPEC-002 DEC-006, REQ-061, INV-008 | no cross-tenant read, action or export; fail closed | the gate on every entry point |
| ADR-062 (R3, R6) | a missing tenant is `InvalidRequest`; denials are `Forbidden` | error category of the gate |
| ADR-054 D4 | tenant as attribute and metric tag only when multi-tenancy is on | the denied counter carries no tenant tag |
| ADR-018, ADR-021 | 12-function evaluation; EventId ranges registered first | matrix, Phase 3 |
| OWASP ASVS V4 (access control, deny by default) | deny by default | Design Choice 2 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| `IPipelineBehavior<TRequest, TResponse>`, `IStreamPipelineBehavior<TRequest, TItem>` | `src/Encina/Abstractions/IPipelineBehavior.cs`, `src/Encina/Abstractions/IStreamPipelineBehavior.cs` | the two behaviors implement them |
| `PipelineStage`, `PipelineStageAttribute` | `src/Encina/Pipeline/Ordering/PipelineStage.cs`, `PipelineStageAttribute.cs` | the declared position of the gate |
| `TenancyOptions.RequireTenant`, `TenancyErrors` | `src/Encina.Tenancy/Configuration/TenancyOptions.cs`, `src/Encina.Tenancy/TenancyErrors.cs` | the switch (default true) and the error |
| `AddEncinaTenancy`, `MultiTenancyMarker` | `src/Encina.Tenancy/ServiceCollectionExtensions.cs`, `src/Encina/Abstractions/MultiTenancyMarker.cs` | registration point |
| `TenancyMetrics`, `TenancyLog`, `TenancyMetricsInitializer` | `src/Encina.Tenancy/Diagnostics/`, `src/Encina.OpenTelemetry/Tenancy/TenancyMetricsInitializer.cs` | the `Encina` meter is shared; `TenancyMetrics` is not in the container, so the gate counter is a static instrument on the same meter |
| `ProcessorValidationPipelineBehavior<,>` | `src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs` | pattern: static attribute cache, `TryAddEnumerable` transient |
| `TenantResolutionMiddleware` | `src/Encina.Tenancy.AspNetCore/Middleware/TenantResolutionMiddleware.cs` | the HTTP entry point whose 400 the gate mirrors for other entry points |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | range registration |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Tenancy` (pipeline) | 7850-7879 (new `TenancyPipeline`, from the block assigned to the family `multitenancy-1`) | registered by PR 346-A first; this plan uses 7850-7852; #338 reserves 7853-7854 and #304 7855-7856 (assigned in merge order, packed without gaps); 7857-7879 spare |
| `Encina.Tenancy` (existing) | none | `Tenancy = (1800, 1899)`: 1800-1804 used; 1805-1810 are #711's; this plan adds none there |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Production (attribute, two behaviors, log class, gate metrics class, registration edit, EventIdRanges edit) | 7 | PR 346-A |
| Tests (behavior unit, guard, property, DI test edit, allocation test edit) | 5 | PR 346-A |
| Metadata (`Encina.Tenancy.json`, `Encina.json`, two `PublicAPI.Unshipped.txt`, changelog fragment) | 5 | PR 346-A |
| Property test, `Encina.json` manifest, justification files (load, benchmark) | 4 | PR 346-A2 |
| Documentation (`multi-tenancy.md`, Tenancy README) and the doc-comment fix in `Encina.Tenancy.AspNetCore` | 3 | PR 346-B |
| Total | 22 | three worker pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, ROP (Either<EncinaError, T>). Issue #346 (open umbrella of the multi-tenancy core): the package that exists is Encina.Tenancy; what is missing is a pipeline gate that denies a request with no tenant on every entry point.
- The tenant is IRequestContext.TenantId; multi-tenancy is on when MultiTenancyMarker is registered (AddEncinaTenancy). Resolution happens at the entry points; enforcement happens in the pipeline.

IMPLEMENTATION OVERVIEW:
1. TenantAgnosticAttribute (opt-out, explicit and logged) and TenantGatePipelineBehavior plus its stream twin, stage Authorization Order 5.
2. Registration in AddEncinaTenancy with TryAddEnumerable (transient); DI test in both registration orders.
3. New range TenancyPipeline 7850-7879 registered first; EventIds 7850-7852; counter encina.tenancy.gate.denied.
4. Tests of unit, guard and property types; load and benchmark justifications; manifests via mechanical-fixer.
5. Documentation by docs-writer: gate section, FAQ, the doc example fix, README; changelog fragment.

KEY PATTERNS:
- Fail closed: a request with no tenant is denied unless its type has [TenantAgnostic] or RequireTenant is false; both opt-outs are logged once per closed type.
- Error code and category: TenancyErrors.TenantRequired() (InvalidRequest); no tenant value in any message; logs carry codes and request type names.
- Static per-generic-type attribute cache; no scoped service in a singleton.
- No cross-tenant attribute and no default-tenant fallback (the platform-operator path is #3748).

REFERENCE FILES:
- src/Encina.Tenancy/ServiceCollectionExtensions.cs
- src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs
- src/Encina/Pipeline/Ordering/PipelineStage.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/features/multi-tenancy.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The gate reads the request context and calls no cache; the tenant segment of cache keys already exists (`DefaultCacheKeyGenerator`) and is #1257's `TenantCacheKey`. |
| 2 | OpenTelemetry | ✅ | Counter `encina.tenancy.gate.denied` with no tenant tag (a denial has no tenant); the tenant tag on resolved requests is #2676 (ADR-054 D4). |
| 3 | Structured Logging | ✅ | `TenancyPipelineLog`, EventIds 7850-7852 in the new `TenancyPipeline` range, codes and request type names only. |
| 4 | Health Checks | ❌ | The gate has no dependency to check; the tenant store health check exists (`TenantHealthCheck`). |
| 5 | Validation | ✅ | Presence of the tenant is validated for every request; existence and status are #338. |
| 6 | Resilience | ❌ | No external call. |
| 7 | Distributed Locks | ❌ | No shared state is updated. |
| 8 | Transactions | ❌ | The gate runs before the transaction stage and writes nothing. |
| 9 | Idempotency | ❌ | A pure check; replay returns the same result. |
| 10 | Multi-Tenancy | ✅ | This feature. |
| 11 | Module Isolation | ❌ | Module scoping is `ModuleId` (#1636), not part of the tenant gate. |
| 12 | Audit Trail | ⏭️ | The denial is counted and logged; an audit event for a tenant-boundary denial needs a dependency on `Encina.Security.Audit` and is the follow-up issue file `tenant-boundary-audit` written with #304. |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect on this plan |
|--------------|-------|---------------------|
| #1257 PR 1257-A1 (`ITenantScopeResolver`) | plan merged, code not started | Design Choice 4 A needs it; under B the gate is independent |
| #2184 (the stage vocabulary applied by the pipeline builders) | phase note in `PipelineStage.cs` | until applied the gate runs in registration order; the attribute is declared now |
| #3748 (platform-operator path) | open, `needs-decision` | not a dependency: the plan builds nothing cross-tenant |
| #1164 (persisted context restored on dispatch) | open, settled | background entry points reach the gate with a tenant |

**Sequencing**: PR 346-A first (it registers the EventId range that #338 and #304 use), then 346-B (documentation and the doc-comment fix).

---

## Next Steps

1. Maintainer answers Design Choices 3, 4, 5 and 7 when work on the issue starts (recorded in `## Maintainer Decisions`).
2. Worker pull requests (counts include tests, manifests, PublicAPI lines and fragments; recount by grep when the worker starts):
   - **PR 346-A** (gate, attribute, streams, registration, range, log, counter): `TenantAgnosticAttribute.cs`, `TenantGatePipelineBehavior.cs`, `TenantGateStreamPipelineBehavior.cs`, `TenancyPipelineLog.cs`, `TenantGateMetrics.cs`, `ServiceCollectionExtensions.cs`, `EventIdRanges.cs` (7); `TenantGatePipelineBehaviorTests.cs`, `TenantGatePipelineBehaviorGuardTests.cs`, the DI test edit, `EncinaEventIdAllocationTests.cs` (4); `Encina.Tenancy.json`, `PublicAPI.Unshipped.txt` of `Encina.Tenancy` and of `Encina`, `346-tenant-pipeline-gate.added.md` (4): 15 files.
   - **PR 346-A2** (property test and the justifications): `TenantGatePipelinePropertyTests.cs`, `Encina.json` (the target of `EventIdRanges.cs`), `TenantGate.md` for load and for benchmark (4 files). It builds alone on 346-A.
   - **PR 346-B** (documentation): `docs/features/multi-tenancy.md`, `src/Encina.Tenancy/README.md`, the doc-comment fix in `src/Encina.Tenancy.AspNetCore/ServiceCollectionExtensions.cs`: 3 files.
3. Link this plan from #346; the note on the closed-out items of the issue goes in the same comment.
