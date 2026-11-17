# Implementation Plan: Tenant ownership validation of requests that reference resources (tenant isolation behavior, remaining work)

> **Issue**: [#304](https://github.com/dlrivada/Encina/issues/304)
> **Type**: Feature (child of the EPIC #876; the EF Core filter and the provider repositories of its text are already implemented)
> **Complexity**: Medium (3 phases of code, 1 testing and documentation phase, 3 worker pull requests)
> **Estimated Scope**: ~8 production files, ~7 test files, ~6 metadata and documentation files
> **Related plans** (cited by issue number): #346 (the tenant gate; owner of `[TenantAgnostic]`, of the EventId range `TenancyPipeline` and of the stage and order convention), #338 (existence and status of the tenant, Order 6), #1257 (shared tenant-scope primitive and the isolation suite), #385 (EF filter for any context and the security guide), #125 (messaging dispatch guard), #798 (audit and ABAC tenant scoping), #3748 (the audited platform-operator path)

---

## Summary

#304 asks for marker interfaces for tenant-scoped queries and commands, a `TenantIsolationPipelineBehavior` that requires a tenant and validates ownership, an EF Core global filter, Dapper tenant parameters, a cross-tenant attribute and configuration. Most of it exists in another shape; what remains is **request-level ownership validation**: a command or query that names a resource (an order id, a document id) must be refused when the resource belongs to another tenant, including resources that the repository filter does not see.

**State of the code on `ec129cae5` (current `origin/main`; the cited files were read on it).**

| Item of #304 | State | Evidence or owner |
|--------------|-------|-------------------|
| `ITenantEntity` | Done | `src/Encina.Tenancy/Abstractions/ITenantEntity.cs` (`string TenantId { get; set; }`) |
| EF Core global query filter on `ITenantEntity` | Done for `TenantDbContext` descendants (named filter `Encina.Tenancy`, tenant stamped on save); any other context is #385 | `src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs` |
| Dapper, ADO.NET and MongoDB tenant filter and tenant stamping | Done for the 10 providers (`TenantAwareFunctionalRepository*` and `TenantAwareSpecificationSqlBuilder` in the six ADO and Dapper packages and in `Encina.MongoDB`); the three switches that make part of it optional are being removed by #3189 to #3197 | `src/Encina.*/Tenancy/` |
| Tenant context must exist | The gate is #346 | |
| `ITenantScopedQuery<T>`, `ITenantScopedCommand<T>` markers | Not built; Design Choice 1 | |
| `ITenantValidator.ValidateOwnershipAsync<TRequest>` and the behavior that calls it | **Missing: this plan** | |
| `[AllowCrossTenant]`, `CrossTenantRoles` | Not built here; Design Choice 5 | #3748 |
| Tenant boundary violations audited | Counted and logged here; the audit event is a follow-up issue file | `artifacts/issues/tenant-boundary-audit.md` |
| `ForTenant()` extension and a `TenantInfo` lookup helper | Not needed: the repositories filter by the ambient tenant | Design Choice 4 |

**What is missing, by reading the code.** The repository filters protect reads and modifications made through Encina repositories, and the request-level check does not exist. A command that carries a document id, an external blob key or a payment reference that lives outside a tenant-filtered repository reaches its handler with nothing asking whether the caller's tenant owns it. That is the insecure direct object reference class (OWASP A01): the handler must remember to check, in every handler.

**Scope.** `ITenantOwnedRequest` (marker), `ITenantOwnershipValidator<TRequest>`, `TenantOwnershipPipelineBehavior<TRequest, TResponse>` and its stream twin, two error factories, the registration helper `AddTenantOwnershipValidator<TRequest, TValidator>()`, one counter, two log messages, documentation. **Out of scope**: the tenant presence gate (#346), existence and status (#338), the cross-tenant operator path (#3748), a built-in validator for repository entities (Design Choice 4), the audit event (follow-up issue).

**Affected packages**: `Encina.Tenancy` (all code). **Provider category**: none (validators are written by the application against its own stores; the 10 database providers are reached only through the existing tenant-filtered repositories).

---

## Design Choices

<details>
<summary><strong>1. The shape of the ownership check</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's shape: `ITenantScopedQuery<T>` and `ITenantScopedCommand<T>` markers that extend `IQuery<T>` and `ICommand<T>`, and one non-generic-by-type validator `ValidateOwnershipAsync<TRequest>(request, tenantId, ct)`** | Matches the issue text | Two markers that mean the same; a request type can have only one base (a type that is both a command and needs the check must pick the marker); the generic method forces the one validator to switch over every request type |
| **B) One marker `ITenantOwnedRequest` (any request, command or query) and a typed `ITenantOwnershipValidator<in TRequest>` resolved per request type from DI; every validator registered for the type must pass; none registered is a denial** | One marker; a validator per request type is small, testable and resolved by the container; an application adds a validator next to its handler | A validator class per request type that references a resource |
| **C) No behavior: rely on the tenant-filtered repositories and on handler code** | No new API | The handler must remember; a resource outside a filtered repository is unchecked |

### Chosen Option: **B — One marker, a typed validator per request type** (recommended, pending the maintainer)

### Rationale

- B wins when the resources a request names live in more than one store or outside an Encina repository (blob keys, external ids), which is where the repository filter cannot help; A wins if the maintainer wants the issue's names exactly; C wins for an application whose every handler goes through a tenant-filtered repository, because there the filter already answers "not found".
- **Error semantics (follow the chosen option).** A validator returns `Right(Unit)` when the tenant owns everything the request names. A `Left` of category `NotFound` or `Forbidden` from a validator is normalized by the behavior to `TenancyErrors.OwnershipDenied()` (`tenancy.ownership_denied`, `ErrorCategory.Forbidden`, ADR-062 R6: a denial), with a fixed message and no resource or tenant value, so a client cannot tell "does not exist" from "belongs to another tenant" (the wording of the existing `TenancyErrors.TenantNotFound()` message already ends "Access denied."). Any other `Left` (an `Internal` store failure) is returned unchanged: a failure is never turned into a denial and never into a pass.
- **Without a tenant.** An ownership-marked request with a blank tenant returns `TenancyErrors.TenantRequired()` even when `[TenantAgnostic]` is also on the type (the marker wins: ownership cannot be checked without a tenant).

</details>

<details>
<summary><strong>2. Where the behavior sits in the pipeline</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PipelineStage.Authorization`, `Order = 7` (after the gate at 5 and the validation at 6 of #346 and #338)** | Runs once the tenant is known to be present, known and active; before compliance, caching and transactions, so a denied request opens no transaction and reads no cache | A validator that needs data prepared by an inner behavior cannot get it |
| **B) `PipelineStage.Application` (innermost, right before the handler)** | Sees the request after every inner preparation step | A cached or transactional behavior could act on a request whose ownership is not yet checked |

### Chosen Option: **A — Authorization, Order 7** (recommended, pending the maintainer)

### Rationale

- A wins when the check depends only on the request and the tenant, which is the contract of this validator; B wins if a validator must read data that an `InputPreparation` or decryption step produces. The numbers follow the convention of #346 Design Choice 3 and change with it.

</details>

<details>
<summary><strong>3. A marked request with no validator registered</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Return `Left(TenancyErrors.OwnershipValidatorMissing())`: code `tenancy.ownership_validator_missing`, `ErrorCategory.Internal` (ADR-062 R1: a server-side wiring lookup), logged by code (EventId 7856)** | Fail closed; no exception; the miswiring is visible on the first request in a test | The failure appears at the first request, not at startup |
| **B) Throw `InvalidOperationException` from the behavior** | Hard to miss | An exception where the rest of the pipeline returns `Either` (ROP); a request the application chose to mark brings the host handler down |
| **C) Allow and log a warning** | Never blocks | Fail open: a marked request runs unchecked |

### Chosen Option: **A — A fail-closed `Left`** (recommended, pending the maintainer)

### Rationale

- A wins when misconfiguration should be a normal, observable error of the request; B wins if the maintainer wants a miswired validator to crash the first test run. C is not an option: it contradicts `AGENTS.md` section 3. A startup scan for marked request types is not planned: closed generic request types are known only when they are first dispatched.

</details>

<details>
<summary><strong>4. A built-in validator for repository entities</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) None: an application validator calls its own tenant-filtered repository and returns `Left` for a missing row; the behavior normalizes it to a denial** | No new provider-facing API; the existing filters on the 10 providers do the work | Each application writes a few lines per request type |
| **B) A generic `EntityOwnershipValidator<TRequest, TEntity, TId>` over `IFunctionalRepository<TEntity, TId>`** | Zero lines for the common case | A new generic over every repository; the id extraction from a request needs a selector per request type, which is the few lines it saves; couples `Encina.Tenancy` to the repository abstraction |

### Chosen Option: **A — No built-in validator** (recommended, pending the maintainer)

### Rationale

- A wins while the filtered repository already turns a foreign id into "not found", which holds for every tenant-aware repository (`TenantAwareFunctionalRepository*` on the 10 providers); B wins if many request types reference one entity by one id and the maintainer wants the convenience. The `ForTenant()` extension of #338 and #304's text is not created: the repositories filter by the ambient tenant.

</details>

<details>
<summary><strong>5. The cross-tenant attribute and the cross-tenant roles</strong></summary>

**Class:** Settled
**Settled by:** the maintainer decision recorded in the register of [#3111](https://github.com/dlrivada/Encina/issues/3111) (gate G3: cross-tenant access only through the audited platform-operator permission with a mandatory reason, never a boolean or a role list on the request type) and the open issue #3748 that creates that path.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No `[AllowCrossTenant]` and no `CrossTenantRoles` configuration; a request that must read across tenants uses the operator permission of #3748 when it exists** | Complies with the decision above | Cross-tenant platform requests wait for #3748 |
| **B) `[AllowCrossTenant(RequiredRoles = ...)]` as written in #304** | Matches the issue text | A role list on a request type is a boolean in disguise: no mandatory reason, no audit; the decision above rules it out |

### Chosen Option: **A — Not built here**

### Rationale

- Only A complies. The behavior has no way to be switched off for a request except the `[TenantAgnostic]` of #346 (which skips presence and existence, not ownership: a marked request is always checked).

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions & Models (PR 304-A)

> **Goal**: the marker, the validator contract and the two errors.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Tenancy/Ownership/ITenantOwnedRequest.cs`**: `public interface ITenantOwnedRequest;` (namespace `Encina.Tenancy`), XML docs with an `<example>` of a command that names a document id.
2. **Create `src/Encina.Tenancy/Ownership/ITenantOwnershipValidator.cs`**: `public interface ITenantOwnershipValidator<in TRequest> where TRequest : ITenantOwnedRequest { ValueTask<Either<EncinaError, Unit>> ValidateAsync(TRequest request, string tenantId, CancellationToken cancellationToken); }`; the XML docs state the contract of Design Choice 1 (return a `Left` for a missing or foreign resource, never distinguish them; an `Internal` `Left` for a store failure).
3. **Modify `src/Encina.Tenancy/TenancyErrors.cs`**: `OwnershipDeniedCode = "tenancy.ownership_denied"` and `OwnershipDenied()` (`ErrorCategory.Forbidden`); `OwnershipValidatorMissingCode = "tenancy.ownership_validator_missing"` and `OwnershipValidatorMissing()` (`ErrorCategory.Internal`); fixed messages with no tenant or resource value.
4. PublicAPI lines (mechanical-fixer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #304, plan tenant-ownership-validation-implementation-plan-304.md. Design Choices 1 to 5 define the shape; read them first. TenancyErrors holds fixed-message factories with an ErrorCategory (ADR-062).

TASK:
Create ITenantOwnedRequest, ITenantOwnershipValidator<TRequest> and the two TenancyErrors factories.

KEY RULES:
- ROP: the validator returns Either<EncinaError, Unit>; no exceptions for business logic.
- Messages carry no tenant id and no resource identifier; categories Forbidden and Internal as the plan states; unit tests assert code, category and message like the existing factories.
- XML docs on every public member with an example; zero warnings.

REFERENCE FILES:
- src/Encina.Tenancy/TenancyErrors.cs
- src/Encina.Tenancy/Abstractions/ITenantEntity.cs (style of the abstractions)
- tests/Encina.UnitTests/Tenancy/ (TenancyErrorsTests)
```

</details>

### Phase 2: Pipeline Behavior and Registration (PR 304-A, stream twin in PR 304-B)

> **Goal**: check ownership before the handler, fail closed.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Tenancy/Ownership/TenantOwnershipPipelineBehavior.cs`**: `[PipelineStage(PipelineStage.Authorization, Order = 7)] public sealed class TenantOwnershipPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>`; constructor `(IServiceProvider services, ILogger<...> logger)` resolving `IEnumerable<ITenantOwnershipValidator<TRequest>>` only when the request type is marked: a `private static readonly bool IsOwned = typeof(ITenantOwnedRequest).IsAssignableFrom(typeof(TRequest))` per closed type makes every unmarked request a straight `nextStep()` with no resolution. Marked: blank `context.TenantId` returns `Left(TenancyErrors.TenantRequired())`; no validator returns `Left(TenancyErrors.OwnershipValidatorMissing())` (EventId 7856, counter outcome `validator_missing`); validators run in registration order and the first `Left` ends the check: `NotFound` or `Forbidden` becomes `TenancyErrors.OwnershipDenied()` (EventId 7855, counter outcome `denied`), any other `Left` is returned unchanged. The cast to the marker uses the closed type's validators, so a validator for a base request type also applies (contravariance of `in TRequest`).
2. **Create `TenantOwnershipStreamPipelineBehavior<TRequest, TItem>`** (PR 304-B, same stage and order): a denied stream yields one `Left` and ends.
3. **Create `src/Encina.Tenancy/Diagnostics/TenantOwnershipMetrics.cs`** (`internal static`): counter `encina.tenancy.ownership.denied` on the shared `Encina` meter, tag `outcome` (`denied`, `validator_missing`); no tenant tag (ADR-054 D4, helper #2676).
4. **Modify `src/Encina.Tenancy/Diagnostics/TenancyPipelineLog.cs`** (created by #346): EventIds 7855 (ownership denied, Warning, request type and code) and 7856 (validator missing, Error, request type and code).
5. **Modify `src/Encina.Tenancy/ServiceCollectionExtensions.cs`**: `TryAddEnumerable` transient registration of the behavior; `AddTenantOwnershipValidator<TRequest, TValidator>(this IServiceCollection)` registering `ITenantOwnershipValidator<TRequest>` as transient (the validators are usually scoped consumers: transient avoids a scoped-in-singleton and works from a scope). DI test with `ValidateOnBuild` and `ValidateScopes`: a validator that takes a scoped `DbContext`-like dependency resolves from a scope.
6. PublicAPI lines (mechanical-fixer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Phase 1 is in the same pull request; PR 346-A (TenantAgnosticAttribute, the TenancyPipeline EventId range, TenancyPipelineLog) and PR 338-B (Order 6) are merged or in the same batch.

TASK:
Write TenantOwnershipPipelineBehavior (the stream twin goes in the follow-up pull request), the counter, the two log methods and the registrations.

KEY RULES:
- Unmarked requests do no work (a static per-closed-type bool, no resolution, no allocation).
- Fail closed: no tenant, no validator, or a failing validator never reaches the handler; a store failure (Internal) is returned as it is, never as a denial or a pass.
- A Left of category NotFound or Forbidden from a validator becomes TenancyErrors.OwnershipDenied(): never distinguish a missing resource from a foreign one.
- No scoped service in a singleton; logs and the counter carry the request type and a code, never a tenant or resource value.
- CRAP <= 10 per method: Handle delegates to small private methods (check, normalize).

REFERENCE FILES:
- src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs
- src/Encina.Tenancy/ServiceCollectionExtensions.cs
- src/Encina/Pipeline/Ordering/PipelineStage.cs
```

</details>

### Phase 3: Observability and Cross-Cutting Integration (PR 304-A)

> **Goal**: the denial is visible; the audit event is tracked.

<details>
<summary><strong>Tasks</strong></summary>

1. EventIds 7855 and 7856 packed after #338's 7853-7854 in the range `TenancyPipeline` (7850-7879) registered by #346; this PR registers nothing.
2. The counter of Phase 2; no span (the validator is application code and traces itself).
3. Audit of boundary violations: a follow-up issue file (`artifacts/issues/tenant-boundary-audit.md`): an audit event for every denied ownership check needs `Encina.Security.Audit` (or the Marten audit stores of #798) as a dependency of `Encina.Tenancy`, which it does not have today; the file proposes where the adapter lives. It is shared with the audit rows of #346 and #338.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- ADR-021: EventIds come from the registered range TenancyPipeline (7850-7879); this plan's ids are 7855-7856, assigned in merge order after #346 (7850-7852) and #338 (7853-7854).

TASK:
Add the two log methods and the counter; run the architecture tests that scan EventIds.

KEY RULES:
- Packed sequentially, no gaps, no id outside the range; XML docs name the range.
- Never log a tenant value, a resource identifier or EncinaError.Message.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Tenancy/Diagnostics/TenancyPipelineLog.cs (from PR 346-A)
```

</details>

### Phase 4: Testing, Documentation & Finalization (tests in PR 304-A and 304-B; documentation in PR 304-C)

> **Goal**: every flag at its target; the guide shows how to write a validator.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** `tests/Encina.UnitTests/Tenancy/Ownership/TenantOwnershipPipelineBehaviorTests.cs`: unmarked request passes with no validator resolved; marked and owned passes; marked and foreign denied with `tenancy.ownership_denied` and category `Forbidden`; `NotFound` and `Forbidden` from a validator both normalize to the same error; an `Internal` `Left` is returned unchanged; no validator returns `tenancy.ownership_validator_missing`; blank tenant returns `tenancy.tenant_required` even with `[TenantAgnostic]` on the type; two validators, the second not called after the first fails; a base-type validator applies (contravariance); the counter outcome tags; the log once per denial.
2. **Guard tests** `tests/Encina.GuardTests/Tenancy/`: constructor, registration helper (`null` services) and the error factories' parameterless contract.
3. **Property test** (FsCheck): for any `ErrorCategory` the normalization returns `OwnershipDenied` exactly for `NotFound` and `Forbidden` and the identical error otherwise.
4. **Integration**: justification in the unit test class comment: the behavior has no database dependency (a non-database feature, `AGENTS.md` section 9); the repository filters that make a foreign id "not found" are covered by the existing per-provider tenancy integration tests (`tests/Encina.IntegrationTests/.../Tenancy/`), and the two-tenant suite of #1257 adds the cross-store check.
5. **Load and benchmark**: justification `.md` files (`tests/Encina.LoadTests/Tenancy/TenantOwnership.md`, `tests/Encina.BenchmarkTests/Tenancy/TenantOwnership.md`): no shared mutable state; the unmarked path is one static bool; the cost of a marked request is the application's validator.
6. **Documentation (PR 304-C, docs-writer)**: `docs/features/multi-tenancy.md` gains "Ownership validation" (the marker, a validator example with a repository, the denial semantics, why a foreign id and a missing id look the same, what `[TenantAgnostic]` does not skip) and an FAQ answer for "How do I prevent cross-tenant data access?"; `src/Encina.Tenancy/README.md`.
7. **Changelog fragments** `changelog.d/304-tenant-ownership-validation.added.md` (mechanical-fixer); manifests and PublicAPI through mechanical-fixer.

**Per-file targets** (proposed; measured values replace them and never go down; package default of `Encina.Tenancy.json`: guard 15, unit 60):

| File | Baseline | Flags | Justification |
|------|----------|-------|---------------|
| `Ownership/TenantOwnershipPipelineBehavior.cs` (new) | new | unit 100, guard 90, property 80 | every branch is a unit case; guard reaches the constructor; the property test pins the normalization table. |
| `Ownership/TenantOwnershipStreamPipelineBehavior.cs` (new) | new | unit 100, guard 90 | one denied and one allowed stream. |
| `Ownership/ITenantOwnedRequest.cs`, `ITenantOwnershipValidator.cs` (new) | new | unit 0 | interfaces with no executable line; exercised through the behavior. |
| `Diagnostics/TenantOwnershipMetrics.cs` (new) | new | unit 100 | counter read through an in-memory meter listener. |
| `TenancyErrors.cs` | unit 100, guard 0 | unchanged | two more factories asserted for code, category and message; no guard clause exists. |
| `Diagnostics/TenancyPipelineLog.cs` | set by #346 | raised | two more generated methods exercised with a fake logger. |
| `ServiceCollectionExtensions.cs` | unit 95, guard 70 | unchanged | the behavior registration and the helper are tested in a covered method; the guard test rejects null services. |

CRAP gate: `Handle` delegates the check and the normalization to small private methods at complexity 4 or less.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Code phases written. Per-flag coverage is measured separately (AGENTS.md section 9); tests execute real code.

TASK:
Write the tests; delegate manifests, PublicAPI and the fragment to mechanical-fixer and the pages to docs-writer; measure per-flag coverage.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; no Thread.Sleep; no reflection-only tests.
- Justification .md only for load and benchmark; never for unit, guard or property.
- Never edit CHANGELOG.md; documentation never types coverage figures.

REFERENCE FILES:
- tests/Encina.UnitTests/Tenancy/
- .github/coverage-manifest/Encina.Tenancy.json
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Use in this feature |
|--------|-------------|---------------------|
| `AGENTS.md` section 3 | gates fail closed; errors never swallowed; ROP | Design Choices 1 and 3 |
| SPEC-002 REQ-061, INV-008 | no cross-tenant read, action or export | the ownership check |
| ADR-062 (R1, R6) | server-side lookups are `Internal`; denials are `Forbidden` | the two error categories |
| ADR-054 D4 | tenant tag through one helper | no tenant tag here (#2676) |
| ADR-018, ADR-021 | 12-function evaluation; EventIds from a registered range | matrix, Phase 3 |
| OWASP Top 10 A01 (broken access control, IDOR) and ASVS V4.2 | verify object-level authorization on the server | the purpose of the behavior |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| `ITenantEntity` | `src/Encina.Tenancy/Abstractions/ITenantEntity.cs` | the entity contract the validators check against |
| `TenantDbContext` | `src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs` | the EF filter that turns a foreign id into "not found" |
| `TenantAwareFunctionalRepository*` | `src/Encina.ADO.*/Tenancy/`, `src/Encina.Dapper.*/Tenancy/`, `src/Encina.MongoDB/Tenancy/` | the filter on the other providers |
| `TenancyErrors` | `src/Encina.Tenancy/TenancyErrors.cs` | the error factories the plan extends |
| `TenantGatePipelineBehavior`, `TenantAgnosticAttribute` (core `Encina`), `TenancyPipelineLog` | planned in #346 (`src/Encina.Tenancy/Gate/`, `src/Encina/Abstractions/`, `Diagnostics/`) | the preceding order, the opt-out and the log class and range |
| `TenantValidationPipelineBehavior` | planned in #338 (`src/Encina.Tenancy/Validation/`) | Order 6 |
| `ProcessorValidationPipelineBehavior<,>` | `src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs` | pattern for an attribute- or marker-driven validating behavior |
| `PipelineStage`, `PipelineStageAttribute` | `src/Encina/Pipeline/Ordering/` | the declared position |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | the range registered by #346 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Tenancy` (pipeline) | 7855-7856 inside `TenancyPipeline` (7850-7879), registered by #346 | this plan reserves 7855-7856 (assigned in merge order after #346's 7850-7852 and #338's 7853-7854); 7857-7879 spare |
| `Encina.Tenancy` (existing) | none | `Tenancy = (1800, 1899)` is not touched |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Production, contracts and errors (marker, validator, `TenancyErrors`) | 3 | PR 304-A |
| Production, behavior, metrics, log edit, registration edit | 4 | PR 304-A |
| Production, stream twin and its registration edit | 2 | PR 304-B (the registration edit is the same file as in 304-A) |
| Tests (behavior unit, guard, errors test edit, DI test edit) | 4 | PR 304-A |
| Tests (stream unit, property) and justification files (load, benchmark) | 4 | PR 304-B |
| Metadata (manifest, `PublicAPI.Unshipped.txt`, changelog fragment) | 3 | PR 304-A; 2 more in 304-B |
| Documentation (`multi-tenancy.md`, Tenancy README) | 2 | PR 304-C |
| Total | 22 | three worker pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, ROP (Either<EncinaError, T>). Issue #304: the tenant filters of the EF Core, Dapper, ADO.NET and MongoDB repositories already exist; what is missing is a request-level check that the caller's tenant owns the resources a request names.
- The tenant is IRequestContext.TenantId; the gate (#346) runs at Authorization Order 5, existence and status (#338) at Order 6, this behavior at Order 7.

IMPLEMENTATION OVERVIEW:
1. ITenantOwnedRequest (marker), ITenantOwnershipValidator<in TRequest>, TenancyErrors.OwnershipDenied (Forbidden) and OwnershipValidatorMissing (Internal).
2. TenantOwnershipPipelineBehavior (static per-type bool, validators resolved only for marked requests) and, in a second pull request, the stream twin.
3. AddTenantOwnershipValidator<TRequest, TValidator>() registers a transient validator; the behavior is registered with TryAddEnumerable.
4. Counter encina.tenancy.ownership.denied; EventIds 7855-7856 of the TenancyPipeline range registered by #346.
5. Tests of unit, guard and property types; load and benchmark justifications; documentation and fragments by specialists; the follow-up issue file for the audit event.

KEY PATTERNS:
- Fail closed: no tenant, no validator or a failing validator never reaches the handler; an Internal Left is returned unchanged.
- NotFound and Forbidden from a validator become one OwnershipDenied: a missing resource and a foreign one are indistinguishable.
- Unmarked requests cost one static bool; no scoped service in a singleton; no tenant or resource value in logs, tags or messages.
- No cross-tenant attribute and no role list (the platform-operator path is #3748).

REFERENCE FILES:
- src/Encina.Tenancy/TenancyErrors.cs
- src/Encina.Tenancy/ServiceCollectionExtensions.cs
- src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs
- src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs
- docs/features/multi-tenancy.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The behavior calls the application's validator; cached query results are already keyed by tenant (`DefaultCacheKeyGenerator`) and the ownership check runs before the caching stage. |
| 2 | OpenTelemetry | ✅ | Counter `encina.tenancy.ownership.denied` with an outcome tag and no tenant tag; the ADR-054 D4 tenant tag is #2676. |
| 3 | Structured Logging | ✅ | EventIds 7855-7856 in the `TenancyPipeline` range registered by #346; codes and request type names only. |
| 4 | Health Checks | ❌ | No dependency of its own; the validators are application code. |
| 5 | Validation | ✅ | This feature: ownership of the referenced resources; the presence and existence of the tenant are #346 and #338. |
| 6 | Resilience | ❌ | No external call of its own; a validator that calls a remote store applies its own policy. |
| 7 | Distributed Locks | ❌ | No shared state is updated. |
| 8 | Transactions | ❌ | Runs before the transaction stage and writes nothing, so a denied request opens no transaction. |
| 9 | Idempotency | ❌ | A pure check; replay returns the same result. |
| 10 | Multi-Tenancy | ✅ | This feature. |
| 11 | Module Isolation | ❌ | `ModuleId` is #1636; ownership here is by tenant. |
| 12 | Audit Trail | ⏭️ | An audit event for each denied check needs a dependency on the audit stores: the follow-up issue file `tenant-boundary-audit` (counted and logged meanwhile). |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect on this plan |
|--------------|-------|---------------------|
| PR 346-A (`[TenantAgnostic]`, the EventId range and `TenancyPipelineLog`, the stage and order convention) | planned | PR 304-A builds on it |
| PR 338-B (Order 6, EventIds 7853-7854) | planned | the order and the id sequence assume it; the code does not call it |
| #3189 to #3197 (removal of the optional tenant-isolation switches of the providers) | open | they make the repository filters unconditional; this plan does not depend on them |
| #3748 (platform-operator path) | open, `needs-decision` | not a dependency: nothing cross-tenant is built |

**Sequencing**: 346-A, then 304-A, then 304-B and 304-C in any order.

---

## Next Steps

1. Maintainer answers Design Choices 1, 2, 3 and 4 when work on the issue starts (recorded in `## Maintainer Decisions`).
2. Worker pull requests (counts include tests, manifests, PublicAPI lines and fragments; recount by grep when the worker starts):
   - **PR 304-A** (contracts, errors, behavior, registration): `ITenantOwnedRequest.cs`, `ITenantOwnershipValidator.cs`, `TenancyErrors.cs`, `TenantOwnershipPipelineBehavior.cs`, `TenantOwnershipMetrics.cs`, `TenancyPipelineLog.cs`, `ServiceCollectionExtensions.cs` (7); behavior unit test, guard test, errors test edit, DI test edit (4); `Encina.Tenancy.json`, `PublicAPI.Unshipped.txt`, `304-tenant-ownership-validation.added.md` (3): 14 files.
   - **PR 304-B** (stream twin and the remaining tests): `TenantOwnershipStreamPipelineBehavior.cs`, `ServiceCollectionExtensions.cs` edit (2); stream unit test, property test (2); `TenantOwnership.md` for load and for benchmark (2); `Encina.Tenancy.json`, `PublicAPI.Unshipped.txt` (2): 8 files.
   - **PR 304-C** (documentation): `docs/features/multi-tenancy.md`, `src/Encina.Tenancy/README.md`: 2 files.
3. Link this plan from #304; the note on the closed-out items of the issue goes in the same comment.
