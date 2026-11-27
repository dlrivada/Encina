# Implementation Plan: `[DistributedLock]` Pipeline Behavior (declarative locks around a request, templated resource names, fail-closed context)

> **Issue**: [#220](https://github.com/dlrivada/Encina/issues/220)
> **Type**: Feature
> **Complexity**: Medium (one attribute, one template resolver, one behavior, 5 pull requests)
> **Estimated Scope**: ~900 lines of production code + ~1,600 lines of tests (about 3,750 lines over 5 pull requests; the per-PR estimates in "Estimated File Count" sum to it)
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, AC-027; [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3, D4, D6.3; [ADR-062](../architecture/adr/062-error-category-contract.md)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision); PR 3 also needs PR 1 of [#219](https://github.com/dlrivada/Encina/issues/219); PR 4 needs the family EventId range (registered by this plan's PR 4, see "Event ID Allocation"); [#2405](https://github.com/dlrivada/Encina/issues/2405) (open bug: the SQL Server lock ignores its expiry) is a prerequisite for the SQL Server expiry scenario of PR 5
> **Related**: family `lock-pipeline` ([#221](https://github.com/dlrivada/Encina/issues/221), [#223](https://github.com/dlrivada/Encina/issues/223), [#224](https://github.com/dlrivada/Encina/issues/224), [#225](https://github.com/dlrivada/Encina/issues/225)); #219 (multi-resource lock, sorted acquisition), #724 (telemetry decorator), #741 (tenant-scoped locks), #222 (lock metrics), #226 (RedLock), #2184 (pipeline stages)

---

## Summary

Handlers that need a distributed lock repeat the same code: acquire, check for contention, run, release. This plan adds an opt-in **pipeline behavior** to `Encina.DistributedLock` that does it from a declarative attribute on the request type:

1. **Attribute.** `[DistributedLock("order:{OrderId}", ExpirySeconds = 60)]` on a request class; `AllowMultiple = true` for requests that need several locks (a transfer).
2. **Template resolution.** `{PropertyName}` comes from a public property of the request; `{UserId}`, `{TenantId}` and `{CorrelationId}` come from `IRequestContext` (Design Choice 6). Every substituted value is percent-escaped so two different value sets can never produce the same resource name (injective). A placeholder whose value is missing denies the request (fail closed).
3. **Behavior.** `DistributedLockPipelineBehavior<TRequest, TResponse>` acquires the lock(s) through the registered `IDistributedLockProvider` with the ADR-052 shapes, runs the rest of the pipeline, and releases in a `finally`. Contention is `Left` with a stable code, never an exception (the issue's `ThrowOnFailure` is dropped, Design Choice 3).
4. **Several locks.** The behavior composes the multi-resource lock of #219 (ordinal sort, de-duplication, rollback, one time budget) instead of repeating its deadlock-prevention logic (Design Choice 5).
5. **Ordering.** The behavior declares a pipeline stage so the lock is taken outside the transaction and the inbox (lock, then transact) and inside caching and resilience (Design Choice 2).
6. **Opt-in.** `AddEncinaDistributedLockBehavior()` registers the behavior; a request type without the attribute pays one static `null` check.

**Standards**: ADR-052; ADR-054; ADR-062; `AGENTS.md` section 3 (ROP, fail-closed gates, errors never swallowed, registration completeness) and section 6; deadlock prevention by a global resource order.

**Affected packages**: `Encina.DistributedLock` (everything; the issue's "Encina (core)" box is stale: the core package cannot reference the lock package, Design Choice 1); `Encina` (EventId registration only, PR 4); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`, `Encina.TestInfrastructure`. No provider package changes.

**Provider category**: Distributed locks (`AGENTS.md` section 5, SPEC-000 DEC-009): the behavior runs on any `IDistributedLockProvider`, so it covers all seven providers (InMemory, Redis, SQL Server now; PostgreSQL #207, MySQL #208, Azure Blob #209, DynamoDB #210 when they land) with no provider code. Database (10), caching and transports do not apply.

**What the code does today** (`origin/main` at `f9063538e`):

- Nothing of this feature exists: no `DistributedLockAttribute` or lock behavior under `src/`. `IDistributedLockProvider` has the pre-ADR-052 shape (`Task<IAsyncDisposable?> TryAcquireAsync`, `src/Encina.DistributedLock/IDistributedLockProvider.cs`); #2667 changes it. No `src` code consumes the interface (ADR-052 context).
- `IPipelineBehavior<TRequest, TResponse>.Handle(request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken)` returns `ValueTask<Either<EncinaError, TResponse>>` (`src/Encina/Abstractions/IPipelineBehavior.cs`); `RequestHandlerCallback<TResponse>` takes no token (`RequestHandlerDelegate.cs:9`). `IRequestContext` has `CorrelationId`, `TenantId`, `Identity` (with `UserId`) and `Timestamp`.
- `PipelineStage` and `[PipelineStage(stage, Order = n)]` exist (`src/Encina/Pipeline/Ordering/PipelineStage.cs`, `PipelineStageAttribute.cs`) but the vocabulary is "declared, not yet applied by the pipeline builders" (#2184 is open).
- Attribute-driven behaviors register as `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(X<,>))` and cache the attribute in a static field of the generic type (`src/Encina.Caching/ServiceCollectionExtensions.cs:80`, `QueryCachingPipelineBehavior.cs`). Key templates with escaped segments exist in `src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs` (`CacheKeyRedactor.EscapeSegment`).
- Implementers of the interfaces this plan only consumes: five `IDistributedLockProvider` classes (`InMemoryDistributedLockProvider.cs:18`, `Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs:17`, `SqlServerDistributedLockProvider.cs:19`, `Encina.Caching.Memory/MemoryDistributedLockProvider.cs:21`, `Encina.Caching.Redis/RedisDistributedLockProvider.cs:17`) and three private `ILockHandle` classes (InMemory `:250`, Redis `:223`, SQL Server `:242`); a declaration-form multi-line search of `src/` and `tests/` finds no test class implementing either. This plan changes neither interface.
- `src/Encina.DistributedLock/Encina.DistributedLock.csproj` has `InternalsVisibleTo` for `Encina.UnitTests`, `IntegrationTests`, `ContractTests`, `PropertyTests`, the three lock packages and `Encina.Caching`, but **not** `Encina.GuardTests`: guard tests here target public types.
- `tests/Encina.ContractTests` has no reference to any `Encina.DistributedLock*` project and no Redis or SQL Server collection (its only collections are `LocalStackCollection` and `ServiceBusEmulatorCollection`); `tests/Encina.IntegrationTests` references all three lock packages, has the shared `ADO-SqlServer` collection, and has no Redis collection (the existing Redis lock test starts its own `RedisContainer`, `RedisDistributedLockIntegrationTests.cs:16`).
- `.github/coverage-manifest/Encina.DistributedLock.json` has package targets guard 20 and unit 60 and no per-file targets for existing files; they stay as they are.

---

## Design Choices

<details>
<summary><strong>1. Where the behavior lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.DistributedLock/Pipeline/`** | The lock abstractions are in this package; no new reference | The attribute is a lock-package type |
| **B) Core `Encina`** (the issue's "Encina (core)" box) | Attribute visible to every request assembly | Core would reference `Encina.DistributedLock`, which already references core: a project cycle |

### Chosen Option: **A**

### Rationale

- Only A builds: `Encina.DistributedLock.csproj` has `ProjectReference` to `..\Encina\Encina.csproj`, so core cannot reference it back.

**Class:** Settled
**Settled by:** `src/Encina.DistributedLock/Encina.DistributedLock.csproj` (ProjectReference to `Encina` and `Encina.Messaging`)

</details>

<details>
<summary><strong>2. Pipeline stage and order</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[PipelineStage(PipelineStage.Transaction, Order = -100)]`: same stage as the unit of work, further out** | Lock outside the transaction (commit happens while the lock is held) and outside the inbox check (duplicates serialize first); inside `Caching` (a cache hit takes no lock) and `Resilience` (a retry re-acquires); no change to the closed vocabulary | The stage name says "transaction" for a coordination step |
| **B) A new `Coordination` member between `DataRouting` (1200) and `Transaction` (1300)** | Names the concern | Changes the closed vocabulary owned by #2184; a core change for one behavior |
| **C) No declaration (default `Application`, 1500)** | Nothing to decide | Runs inside every transaction stage: the commit happens after the lock is released |

### Chosen Option: **A - Transaction stage, Order -100** (recommended, pending the maintainer)

### Rationale

- A wins while #2184's vocabulary is closed and the lock must wrap the unit of work; B wins if the maintainer wants a named stage for locks and leader gates (the leader gate of #221 would use it too). The declaration is inert until #2184 applies the vocabulary to the builders; until then the behavior runs in registration order and the README says so (register it after caching and resilience, before the transaction behavior).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The issue's `ThrowOnFailure`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Drop it: a lock that cannot be obtained is a `Left`** | Matches ROP; no exception from a gate | The issue's property disappears |
| **B) Keep it: `ThrowOnFailure = true` throws `LockAcquisitionException`** | Matches the issue | Business outcome as an exception; `LockAcquisitionException` is removed by ADR-052 |

### Chosen Option: **A**

### Rationale

- Only A complies: operations return `Either` and never use exceptions for business logic, and ADR-052 removes `LockAcquisitionException` ("no shim").

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (Railway Oriented Programming) and ADR-052 Decision ("`LockAcquisitionException` is removed")

</details>

<details>
<summary><strong>4. Outcome codes and categories of a lock that is not obtained</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Constant codes in `DistributedLockPipelineErrorCodes`: `lock_pipeline.not_acquired` (`Conflict`, ADR-062 R2: an operation in progress), `lock_pipeline.backend_failure` (`Internal`, R4), `lock_pipeline.invalid_template` and `lock_pipeline.invalid_attribute` (`Internal`, R1: configuration), `lock_pipeline.property_missing` (`Validation`, R3), `lock_pipeline.tenant_required` (`InvalidRequest`, R3: a missing tenant), `lock_pipeline.user_required` (`Unauthenticated`, R6), `lock_pipeline.resource_too_long` (`Validation`); a lock-provider `Left` is returned unchanged** | One stable code per cause; the category is declared once; clients branch on codes | The issue's `FailureErrorCode` override is dropped |
| **B) Keep the issue's `FailureErrorCode` string and `ThrowOnFailure`-style knobs** (category `Conflict` for any code) | An application maps contention to its own domain code | A free string defeats the declared code-and-category pairs; two attributes can carry different codes for one cause |
| **C) Contention as `Timeout`** | Reflects that the wait elapsed | R2 names "operation in progress" as `Conflict`; a `Timeout` reads as a server fault to monitoring |

### Chosen Option: **A - constant codes** (recommended, pending the maintainer)

### Rationale

- A wins for the library default and for monitoring that counts categories; B wins for an application whose clients branch on its own domain code (it can map `not_acquired` itself in an outer behavior). Contention is not a fault: logs and metrics count it as `contended`, never `failed` (ADR-052 consequences).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Several `[DistributedLock]` attributes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Compose `IDistributedMultiLockProvider` of #219 when the type has more than one attribute; the set uses the largest `ExpirySeconds` and `WaitSeconds` and the smallest `RetryMilliseconds` of its attributes (documented)** | Ordinal sort, de-duplication, rollback, one deadline and aligned expiry stay in one place; one lock handle to release | Per-attribute parameters collapse into one set of parameters; PR 3 waits for #219 PR 1 |
| **B) The behavior sorts and acquires each attribute with its own parameters, rolling back itself** | Per-attribute parameters honored; no dependency on #219 | A second copy of the deadlock-prevention and rollback logic; two places to keep correct |

### Chosen Option: **A - compose #219** (recommended, pending the maintainer)

### Rationale

- A wins when one correct implementation matters more than per-lock parameters (the issue's own example gives the two account locks the same parameters); B wins when the attributes of one request need very different waits. Until PR 3 lands, a type with more than one attribute returns `Left` `lock_pipeline.invalid_attribute`, so no request runs partly locked.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Placeholders: which source wins and what a missing value does</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `UserId`, `TenantId` and `CorrelationId` always resolve from `IRequestContext`; a request property with the same name is not reachable through that placeholder (the first use logs one warning per request type); every other name is a public readable property of the request** | A caller cannot choose the tenant or user segment through the request body | A background command that carries its own `TenantId` property needs the dispatcher to set the context tenant |
| **B) A request property wins over the context value of the same name** | The issue's example `tenant:{TenantId}:scheduler` with a `TenantId` property works with no context tenant | The authenticated tenant is bypassed by request data |

Missing values deny the request in both options: a null or empty tenant gives `lock_pipeline.tenant_required`, an identity without `UserId` gives `lock_pipeline.user_required`, a null property gives `lock_pipeline.property_missing` (`AGENTS.md` section 3: gates fail closed). The tenant is required only by a template that names `{TenantId}`; no `default` tenant segment is ever substituted for a missing one.

### Chosen Option: **A - context names resolve from the context** (recommended, pending the maintainer)

### Rationale

- A wins for any API-facing command; B wins for scheduled commands replayed by a processor without an ambient tenant. Values are formatted with `CultureInfo.InvariantCulture` and percent-escaped for `%` and `:` (and control characters), so a value can never move a segment boundary: two different (template, values) pairs never produce the same name. Property types are limited to string, `Guid`, integral types, enums and `bool`; any other type is `lock_pipeline.invalid_template`. The resolved name is at most `MaxResourceLength` (default 200; a provider adds its prefix) or `lock_pipeline.resource_too_long`.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Release, cancellation and the template cache</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Acquire, `await nextStep()`, release in `finally` with its own `try/catch` (logged by exception type, never by message); the result of the handler is never masked; `OperationCanceledException` from the acquire wait propagates; the attribute, the compiled template and the resolved plan are cached in static fields of the generic type, an invalid plan is cached as its `Left`** | The caller gets the real outcome; no reflection per request; an invalid attribute fails the same way every time | A release failure is visible only in telemetry |
| **B) Resolve the attribute per request** | No static state | Reflection and parsing on every request |

### Chosen Option: **A**

### Rationale

- `IAsyncDisposable.DisposeAsync` must not throw and the lock frees itself at its expiry; the static plan mirrors `QueryCachingPipelineBehavior`.

**Class:** Trivial
**Settled by:** `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` (static attribute field) and ADR-052 Decision (cancellation propagates)

</details>

<details>
<summary><strong>8. Registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaDistributedLockBehavior(Action<DistributedLockBehaviorOptions>?)`: options with validation on start, the open-generic transient behavior, `TryAdd` of the multi-lock registration of #219; `InvalidOperationException` naming the missing call when no `IDistributedLockProvider` is registered; a second call changes nothing** | A missing provider fails at startup; everything the behavior resolves is registered | The call order matters (provider first) |
| **B) Resolve lazily and fail on the first request** | Any order | The failure happens in production traffic |

### Chosen Option: **A**

### Rationale

- Registration completeness is a binding rule, proven by a DI test built with `ValidateOnBuild` and `ValidateScopes`. The behavior is transient and takes only singletons (provider, multi-lock provider, options), so no scoped service is injected into a singleton.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (registration completeness) and ADR-050 (registration errors)

</details>

---

## Implementation Phases

### Phase 1: Attribute, Template Resolver, Codes, Options (PR 1)

> **Goal**: Everything the behavior needs, testable without a lock provider.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Pipeline/DistributedLockAttribute.cs`** (new, `src/Encina.DistributedLock/`) - public sealed, `[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]`; constructor `(string resource)`; `ExpirySeconds`, `WaitSeconds`, `RetryMilliseconds` as `int` named properties defaulting to `0` meaning "use `DistributedLockOptions.DefaultExpiry`, `DefaultWait`, `DefaultRetry`" (so the defaults of the existing options stay the single source); `ThrowOnFailure` and `FailureErrorCode` are not carried over (Design Choices 3 and 4). `AutoExtend` is added by #223.
2. **`Pipeline/DistributedLockResourceTemplate.cs`** (new, internal sealed) - parses `{Name}` placeholders once, binds property getters with compiled delegates, resolves with the escape of Design Choice 6; returns `Either<EncinaError, string>`.
3. **`Pipeline/DistributedLockPipelineErrorCodes.cs`** (new, public static) - the constants of Design Choice 4 (each with its ADR-062 category in the XML doc and at its creation site).
4. **`Pipeline/DistributedLockBehaviorOptions.cs`** and **`DistributedLockBehaviorOptionsValidator.cs`** (new) - `MaxResourceLength` (default 200, range 1 to 255, the SQL Server `sp_getapplock` resource limit); validator is `IValidateOptions<DistributedLockBehaviorOptions>`.
5. **Tests** - template: static name, property, each context placeholder, missing tenant, anonymous user, null property, unsupported type, unknown placeholder, escaping and injectivity (a property test over pairs of values: different values never give the same name), length limit, culture independence; options validator; guard test of the validator. PublicAPI lines and the manifest entries (`mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #220: the attribute, the resource template resolver, the error codes and the options.

CONTEXT:
- Encina.DistributedLock references Encina and Encina.Messaging. IRequestContext has CorrelationId, TenantId and Identity.UserId.
- Errors are EncinaErrors.Create(code, ErrorCategory, message); the categories are fixed in the plan (ADR-062).
- No InternalsVisibleTo for Encina.GuardTests: guard-test public types only.

TASK:
Create DistributedLockAttribute, DistributedLockResourceTemplate (internal), DistributedLockPipelineErrorCodes,
DistributedLockBehaviorOptions and its validator, with the tests of the Phase 1 tasks.

KEY RULES:
- Percent-escape every substituted value (% and :) so the name is injective; invariant culture.
- Missing tenant, user or property is a Left, never a default value; never put a value in an error message.
- No exceptions for business outcomes; no [Obsolete]; XML docs on every public member.

REFERENCE FILES:
- src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs, src/Encina.Caching/Attributes/CacheAttribute.cs
- docs/architecture/adr/062-error-category-contract.md
```

</details>

### Phase 2: The Behavior and Its Registration (PR 2)

> **Goal**: A request with one attribute runs under its lock.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Pipeline/DistributedLockPipelineBehavior.cs`** (new, public sealed; `where TRequest : IRequest<TResponse>`; `[PipelineStage(PipelineStage.Transaction, Order = -100)]` per Design Choice 2; constructor `(IDistributedLockProvider, IOptions<DistributedLockOptions>, IOptions<DistributedLockBehaviorOptions>, ILogger<...>)`) - no attribute: `return await nextStep()`; resolve the plan (static, cached); `TryAcquireAsync(resource, expiry, wait, retry, ct)`; `Right(None)` is `lock_pipeline.not_acquired`; a `Left` is returned unchanged; run `nextStep()` in `try`, release in `finally` (Design Choice 7). More than one attribute returns `lock_pipeline.invalid_attribute` until PR 3.
2. **`ServiceCollectionExtensions.cs`** (modify) - `AddEncinaDistributedLockBehavior(...)` per Design Choice 8 (PR 2 registers options and the behavior; PR 3 adds the multi-lock `TryAdd`).
3. **Tests** - unit on the InMemory provider with `FakeTimeProvider`: runs under the lock and releases after success and after a `Left` from the handler; contention returns `not_acquired`; the handler is never invoked on any `Left`; a provider fault is propagated with its code; release failure does not mask the result; cancellation while waiting propagates; request without attribute passes through untouched; a second concurrent request on the same resource waits and then runs; DI test with `ValidateOnBuild` and `ValidateScopes` with and without a provider; guard tests (constructor nulls, null request context). README section, changelog fragment `changelog.d/220-distributed-lock-pipeline-behavior.added.md` (specialists).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #220: DistributedLockPipelineBehavior and AddEncinaDistributedLockBehavior.

CONTEXT:
- #2667 has changed IDistributedLockProvider to the ADR-052 shapes: TryAcquireAsync returns Either<EncinaError, Option<ILockHandle>>,
  contention is Right(None), a fault is Left, cancellation propagates. Read the current signatures.
- RequestHandlerCallback<TResponse> takes no token; the behavior's token is only for the acquire wait.
- The behavior is transient; it takes singletons only.

TASK:
Create the behavior and the registration with the tests of the Phase 2 tasks.

KEY RULES:
- Release in finally with its own try/catch; never mask the handler result; never log a resource name or EncinaError.Message.
- Fail closed: any Left from the plan stops the request before the handler runs.
- Time comes from the injected providers' TimeProvider; no DateTime.UtcNow.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs, src/Encina.Caching/ServiceCollectionExtensions.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs, docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

### Phase 3: Several Locks (PR 3)

> **Goal**: `AllowMultiple` works through the multi-lock of #219.

<details>
<summary><strong>Tasks</strong></summary>

1. **`DistributedLockPipelineBehavior.cs`** (modify) - more than one attribute: resolve every resource, call `IDistributedMultiLockProvider.TryAcquireMultipleAsync(resources, maxExpiry, maxWait, minRetry, ct)`; release the set handle in `finally`; constructor gains `IDistributedMultiLockProvider` (a constructor change: the one `new` site in tests is the PR 2 unit test file, the DI test and the guard test; all three change in this PR).
2. **`ServiceCollectionExtensions.cs`** (modify) - `TryAdd` of `AddEncinaDistributedMultiLock()` so the behavior's dependency is always registered.
3. **Tests** - transfer example with two accounts in opposite attribute orders does not deadlock; the second lock unavailable rolls back the first; a duplicate resolved name collapses; mixed parameters use the maximum rule; PublicAPI line for the changed constructor and the manifest rows (`mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #220: several [DistributedLock] attributes through IDistributedMultiLockProvider (#219).

CONTEXT:
- #219 PR 1 provides IDistributedMultiLockProvider.TryAcquireMultipleAsync (Either<EncinaError, Option<IMultiLockHandle>>) with ordinal sorting
  and rollback. Do not sort or roll back in the behavior.

TASK:
Extend the behavior and the registration and add the tests of the Phase 3 tasks; update every constructor call site.

KEY RULES:
- Parameters of the set: largest ExpirySeconds and WaitSeconds, smallest RetryMilliseconds.
- Grep new DistributedLockPipelineBehavior< sites in src and tests before changing the constructor.

REFERENCE FILES:
- docs/plans/lock-multi-resource-implementation-plan-219.md, src/Encina.DistributedLock/Pipeline/DistributedLockPipelineBehavior.cs
```

</details>

### Phase 4: Observability and EventId Range (PR 4)

> **Goal**: Metrics, logs and the family range.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/DistributedLockPipelineLog.cs`** (new, `internal static partial`) - 30600 lock acquired (Debug: request type, lock count), 30601 not acquired (Debug: request type, outcome `contended`), 30602 lock backend fault (Warning: request type, error code), 30603 release failed (Warning: exception type via `ForLogging()`), 30604 invalid attribute or template (Error, logged once per request type: error code). Request type names only: never a resolved resource name, tenant or user value, or `EncinaError.Message`.
2. **`Diagnostics/DistributedLockPipelineMetrics.cs`** (new, internal) - on the shared `DistributedLockDiagnostics` meter `Encina.DistributedLock` (created by whichever of #724 and #741 lands first): counter `encina.distributed_lock.pipeline.requests` with tags `request.name` (as in `IEncinaMetrics.cs:59`) and `encina.lock.outcome` (`acquired`, `contended`, `failed`); histogram `encina.distributed_lock.pipeline.wait` (`s`). No `_total` suffix (the instrument-name rule of ADR-054); the tenant tag only when multi-tenancy is on, through the helper of #2676; the resolved resource name is never a tag.
3. **EventId registration** (first of the family to land registers it): `EventIdRanges.cs` field `DistributedLockPipeline = (30600, 30649)`; `src/Encina/PublicAPI.Unshipped.txt` line; one row in `docs/engineering/eventid-block-registry.md` for the range with the bounds 30600-30649 (plus the Blocks row set to `registered`); the entry for `Encina.DistributedLock` in `AssemblyRanges` of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (appended to the entry #215 creates, or created here when this lands first).
4. **Tests** - `MeterListener` tests (contended is never `failed`; no resource name or tenant value in any tag or log argument), log tests, justification `.md` files for load and benchmark (thin gate over the provider; the contention path is covered by the provider's own load tests).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #220: telemetry of the lock behavior and the family EventId range.

CONTEXT:
- Range DistributedLockPipeline 30600-30649 is the family slot; this plan uses 30600-30604; #221 30605-30608, #223 30609-30613, #224 30614-30616, #225 30617-30622.
- ADR-054: meter Encina.DistributedLock, no _total suffix, tenant tag only with multi-tenancy.

TASK:
Write the log class, the metrics class, register the range (4 files) and add the tests.

KEY RULES:
- Pack ids sequentially; log error codes or exception types only; contention is not a failure.

REFERENCE FILES:
- docs/engineering/eventid-block-registry.md, src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md
```

</details>

### Phase 5: Contract and Integration Scenarios (PR 5)

> **Goal**: The behavior is proven on real providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **`tests/Encina.ContractTests/DistributedLock/DistributedLockPipelineContractTests.cs`** (new) and **`Encina.ContractTests.csproj`** (modify: two `ProjectReference` lines to `Encina.DistributedLock` and `Encina.DistributedLock.InMemory`; the project has none today and no Redis or SQL Server fixture) - the behavior contract over the InMemory provider: gate runs the handler only under the lock, releases after every outcome, fails closed on a missing tenant.
2. **Integration** - SQL Server in the existing `ADO-SqlServer` collection (`DistributedLockPipelineSqlServerIntegrationTests.cs`) and Redis through a new shared collection (`tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs` and its collection definition; no shared Redis fixture exists, `Testcontainers.Redis` is already referenced): two concurrent requests on one resource run one after the other; the lock is released when the handler returns a `Left`. The SQL Server scenario that relies on lock expiry waits for #2405.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #220: contract and integration tests of the lock behavior.

CONTEXT:
- Encina.ContractTests has no lock project reference; add the two ProjectReferences. Integration tests use shared [Collection] fixtures only.

TASK:
Write the contract test, the shared Redis fixture and collection, and the two integration test classes.

KEY RULES:
- Tests instantiate real implementations; real short expiries on Redis; FakeTimeProvider only on InMemory.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/SqlServerDistributedLockAcquireIntegrationTests.cs
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| SPEC-000 REQ-027, AC-027 | Seven lock providers; the behavior is provider-agnostic |
| ADR-052, ADR-062, ADR-054 | Either outcomes, categories, telemetry conventions |
| Deadlock prevention by a global resource order | Delegated to #219 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IPipelineBehavior<,>`, `PipelineStageAttribute` | `src/Encina/Abstractions/`, `src/Encina/Pipeline/Ordering/` | Behavior contract and stage |
| `QueryCachingPipelineBehavior` | `src/Encina.Caching/Behaviors/` | Static attribute cache and registration pattern |
| `DefaultCacheKeyGenerator`, `CacheKeyRedactor.EscapeSegment` | `src/Encina.Caching/KeyGeneration/` | Escaping precedent |
| `IDistributedMultiLockProvider` | #219 (plan on main) | Several locks |
| `DistributedLockDiagnostics`, decorators | #724, #741 (plans on main) | Meter, tenant tag helper (#2676) |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Status | Proven by |
|----------|--------|-----------|
| InMemory | exists | PR 2 unit, PR 5 contract |
| Redis | exists | PR 5 integration |
| SQL Server | exists (expiry ignored until #2405) | PR 5 integration |
| PostgreSQL, MySQL, Azure Blob, DynamoDB | planned (#207 to #210) | no code here; each package adds one integration scenario |

### Event ID Allocation

| Package | Range | Ids |
|---------|-------|-----|
| `Encina.DistributedLock` (new `DistributedLockPipeline`, first free 50-slot after 30599; no plan on main, no open `docs/plan-*` head and no registry row claims it, checked 2026-11-27) | `30600-30649` | #220 30600-30604; #221 30605-30608; #223 30609-30613; #224 30614-30616; #225 30617-30622; one registry row, bounds identical in `EventIdRanges.cs`; no id below 10000 |

### Estimated File Count

| PR | Content | New | Modified | Tests, manifests, PublicAPI, docs | Total | Lines (est.) |
|----|---------|-----|----------|------------------------------------|-------|--------------|
| 1 | Attribute, template, codes, options | 5 | 1 (PublicAPI) | 4 (template tests, options tests, validator guard test, manifest) | 10 | 900 |
| 2 | Behavior, registration | 1 | 3 (`ServiceCollectionExtensions.cs`, PublicAPI, README) | 5 (behavior tests, DI test, guard test, manifest, fragment) | 9 | 1,100 |
| 3 | Several locks | 0 | 2 (behavior, `ServiceCollectionExtensions.cs`) | 3 (multi tests, PublicAPI, manifest) | 5 | 450 |
| 4 | Log, metrics, range | 2 | 5 (behavior, `EventIdRanges.cs`, core PublicAPI, registry, `EncinaEventIdAllocationTests.cs`) | 5 (metrics tests, log tests, manifest, 2 justification `.md`) | 12 | 750 |
| 5 | Contract, integration | 5 (contract test, `RedisFixture`, Redis collection, 2 integration classes) | 1 (ContractTests csproj) | 0 | 6 | 600 |

Total about 3,800 lines. Every PR is at most 15 files and 1,500 lines and builds and passes alone (PR 3 needs #219 PR 1; PR 4 needs nothing else).

### Per-file coverage targets (numeric, per flag)

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `DistributedLockPipelineBehavior.cs` | 95 | 25 | 60 | 40 | Every branch runs on the InMemory provider with a fake clock; guards cover the constructor; the contract and integration suites run the gate on real providers |
| `DistributedLockResourceTemplate.cs` | 95 | 0 | 0 | 0 | Pure logic; internal type with no `InternalsVisibleTo` for `Encina.GuardTests`, so guard tests cannot reach it (its arguments are validated through the public behavior) |
| `DistributedLockBehaviorOptionsValidator.cs`, `DistributedLockBehaviorOptions.cs` | 95 | 40 | 0 | 0 | Pure validation |
| `DistributedLockPipelineMetrics.cs` | 85 | 0 | 0 | 0 | Instruments tested with a `MeterListener`; internal type |
| `DistributedLockAttribute.cs`, `DistributedLockPipelineErrorCodes.cs`, `DistributedLockPipelineLog.cs` | 0 | 0 | 0 | 0 | Data holders, constants and source-generated log methods: no executable logic; covered through the behavior |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is pre-1.0 (.NET 10, C# 14, nullable enabled). Rules are in AGENTS.md. Issue #220 adds a [DistributedLock] attribute and a
pipeline behavior to Encina.DistributedLock, running on any IDistributedLockProvider.

IMPLEMENTATION OVERVIEW:
1. Phase 1: DistributedLockAttribute, DistributedLockResourceTemplate (escaped, injective), error codes with ADR-062 categories, options.
2. Phase 2: DistributedLockPipelineBehavior (single attribute), AddEncinaDistributedLockBehavior, tests, README and fragment through specialists.
3. Phase 3: several attributes through IDistributedMultiLockProvider (#219).
4. Phase 4: log 30600-30604, metrics, range DistributedLockPipeline 30600-30649 registered in 4 files.
5. Phase 5: contract tests (ContractTests gets two ProjectReferences) and integration tests (shared Redis fixture is new).

KEY PATTERNS:
- ADR-052 outcomes: contention is Right(None) from the provider and Left not_acquired from the behavior; faults pass through unchanged.
- Fail closed on a missing tenant, user or property; never log resource names, tenant or user values or EncinaError.Message.
- Dilemmas 2, 4, 5 and 6 are pending the maintainer: implement the recommended options only after he answers.

REFERENCE FILES:
- docs/plans/lock-pipeline-behavior-implementation-plan-220.md, src/Encina.DistributedLock/
- docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Lock state must not be cached; the behavior sits inside the caching stage so a cache hit takes no lock |
| 2 | OpenTelemetry | ✅ Include | Phase 4: counter and wait histogram on the shared meter; tenant tag only with multi-tenancy (#2676); no resource names |
| 3 | Structured Logging | ✅ Include | Phase 4: `DistributedLockPipelineLog` 30600-30604, request type and error code only |
| 4 | Health Checks | ❌ N/A | Uses the underlying provider's health checks |
| 5 | Validation | ✅ Include | Attribute values, template and resolved name are validated; failures are `Left` with their ADR-062 category |
| 6 | Resilience | ❌ N/A | The behavior sits inside the resilience stage, so a retry re-acquires the lock; the wait budget is the attribute's `WaitSeconds` |
| 7 | Distributed Locks | ✅ Include | This is a lock integration over the provider and the multi-lock of #219 |
| 8 | Transactions | ✅ Include | Design Choice 2: the lock is taken outside the unit of work so the commit happens under the lock |
| 9 | Idempotency | ❌ N/A | The lock serializes duplicates before the inbox check; deduplication stays in the inbox behavior |
| 10 | Multi-Tenancy | ✅ Include | `{TenantId}` resolves from the context and denies when missing; tenant scoping of whole lock names is the decorator of #741 |
| 11 | Module Isolation | ❌ N/A | Modules are separated with `KeyPrefix` (the position of the #741 plan) |
| 12 | Audit Trail | ❌ N/A | Infrastructure coordination with no personal or regulated data |

---

## Prerequisites & Dependencies

| Item | State | Needed for |
|------|-------|------------|
| #2667 ADR-052 implementation | open | All PRs |
| #219 PR 1 (multi-lock) | plan on main, not written | PR 3 |
| Family EventId range | registered by PR 4 (or by the first family PR with a log) | PR 4 and the log PRs of #221, #223, #224, #225 |
| #724 / #741 (`DistributedLockDiagnostics`), #2676 (tenant tag helper) | plans on main / open | PR 4 |
| #2405 SQL Server expiry | open bug | The SQL Server expiry scenario of PR 5 |
| #2184 pipeline stages applied by the builders | open | The stage declaration becomes effective |

---

## Next Steps

1. After #2667 merges open PR 1, then PR 2; PR 3 after #219 PR 1; PR 4 and PR 5 after PR 2.
2. The maintainer answers Dilemmas 2, 4, 5 and 6 when work on the issue starts; PRs are written for the recommended answers.
3. #221 builds on this plan's attribute-resolution helper (`DistributedLockResourceTemplate`), so its PR 1 follows this plan's PR 1.
