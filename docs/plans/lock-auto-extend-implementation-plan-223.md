# Implementation Plan: Auto-Extending Locks (`IAutoExtendingLockHandle`, lock-lost signal, per-lock and global switch)

> **Issue**: [#223](https://github.com/dlrivada/Encina/issues/223)
> **Type**: Feature
> **Complexity**: Medium (a background renewal loop per handle, a decorator, a pipeline integration, 5 pull requests)
> **Estimated Scope**: ~700 lines of production code + ~1,500 lines of tests (about 3,600 lines over 5 pull requests; the per-PR estimates in "Estimated File Count" sum to it)
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, AC-027; [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3, D4, D6.3; [ADR-062](../architecture/adr/062-error-category-contract.md)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052 and makes every provider return `ILockHandle`; open, not a pending maintainer decision); PR 3 needs PR 2 of [#220](https://github.com/dlrivada/Encina/issues/220) (the behavior it extends); [#2405](https://github.com/dlrivada/Encina/issues/2405) (open bug: the SQL Server lock ignores its expiry) is a stated prerequisite for any SQL Server claim
> **Related**: family `lock-pipeline` (#220, #221, #224, #225); #216 (leader election renews its own lease with the same `ExtendAsync` rule), #218 (fencing tokens), #724, #741, #2653 (duplicate lock providers in the caching packages)

---

## Summary

A lock with an expiry protects a handler only while the handler is faster than the expiry. For variable workloads the choices today are an over-long expiry (a crashed holder blocks everyone) or manual `ExtendAsync` calls in every handler. This plan adds **automatic renewal** to `Encina.DistributedLock`:

1. **Handle wrapper.** `AutoExtendingLockHandle` (internal) wraps any `ILockHandle`, renews it every `Interval` through the ownership-checked `ILockHandle.ExtendAsync`, and exposes `IAutoExtendingLockHandle.LockLostToken` (public interface extending `ILockHandle`), cancelled when the lock is lost.
2. **Safe accounting.** The local lease expiry is the time read **before** the extend call plus the duration; if no extension is confirmed before that instant the lock is declared lost (fail closed, Design Choice 4). A definite `Right(false)` (not held any more) is lost at once.
3. **Global switch.** `AddEncinaDistributedLockAutoExtend(...)` decorates the registered provider so every acquired lock renews itself (option `AutoExtendLocks`, default false).
4. **Per-lock switch.** `[DistributedLock("resource", AutoExtend = LockAutoExtendMode.Enabled, AutoExtendIntervalSeconds = 5)]` (#220's attribute) wraps only that request's lock; the behavior returns `Left` `lock_pipeline.lock_lost` when the lock was lost while the handler ran and exposes the token to handlers (Design Choice 2).
5. **Background-loop rules.** One sequential loop per handle on a `PeriodicTimer` over `TimeProvider`; every iteration catches its failures; disposal stops the loop before releasing the lock.

**Standards**: ADR-052; ADR-054; ADR-062; `AGENTS.md` section 3 (background loops never let an exception escape, errors never swallowed, time through `TimeProvider`); lease renewal as in Azure Blob lease renewal and the DynamoDB lock client heartbeat (named comparable products, not dependencies).

**Affected packages**: `Encina.DistributedLock` (everything); `Encina` (EventId registration only when this plan is the first of the family with a log); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`. No provider package changes: the wrapper composes `ILockHandle`.

**Provider category**: Distributed locks (`AGENTS.md` section 5): the wrapper works on all seven providers by composition; its usefulness differs (Provider Matrix). Database (10), caching and transports do not apply.

**What the code does today** (`origin/main` at `f9063538e`):

- Nothing of this feature exists: no `AutoExtend*` type under `src/`. `ILockHandle.ExtendAsync(TimeSpan, CancellationToken = default)` returns `Task<bool>` today (`ILockHandle.cs`); #2667 makes it `Either`. In the Redis handle it is the ownership-checked Lua `PEXPIRE` (`RedisDistributedLockProvider.cs:223` onward); in the InMemory handle `TryExtendLock` compares the lock value (`InMemoryDistributedLockProvider.cs:250` onward); in the SQL Server handle it only moves the expected expiry because `sp_getapplock` locks never expire (`SqlServerDistributedLockProvider.cs:242` onward, #2405).
- The provider-level `IDistributedLockProvider.ExtendAsync(resource, ...)` is a different, unsafe call (it extends a lock the caller may not own: #2405); the wrapper never uses it.
- The issue's sketch uses `new Timer(async _ => ...)`: an `async` lambda here is `async void`, an exception in it ends the process (`AGENTS.md` section 3), and overlapping ticks are possible; this plan uses a sequential `PeriodicTimer` loop.
- Implementers: this plan adds **no member to `ILockHandle` or `IDistributedLockProvider`**; it adds the new interface `IAutoExtendingLockHandle : ILockHandle` that only the wrapper implements. The five existing provider classes (InMemory, Redis, SQL Server, `Encina.Caching.Memory/MemoryDistributedLockProvider.cs:21`, `Encina.Caching.Redis/RedisDistributedLockProvider.cs:17`) and the three private `LockHandle` classes (InMemory `:250`, Redis `:223`, SQL Server `:242`) are untouched; a declaration-form multi-line search of `src/` and `tests/` finds no test class implementing either interface. The two caching providers' private `LockHandle` implements `IAsyncDisposable` only (`MemoryDistributedLockProvider.cs:222`, `RedisDistributedLockProvider.cs:208`); #2667 and ADR-052 force them onto `ILockHandle` (open [#2653](https://github.com/dlrivada/Encina/issues/2653) decides how), and the wrapper needs nothing else.
- Constructor-site changes: PR 3 adds one dependency to the behavior of #220; the `new DistributedLockPipelineBehavior<,>(` sites are in #220's behavior unit test, DI test and guard test (grep them on `origin/main` when #220 PR 2 has merged); PR 2 adds `AddEncinaDistributedLockAutoExtend` (new registration, PublicAPI line, guard test).
- `Encina.DistributedLock` has no `InternalsVisibleTo` for `Encina.GuardTests` (public types only); `tests/Encina.ContractTests` references no `Encina.DistributedLock*` project and has no Redis or SQL Server collection; `tests/Encina.IntegrationTests` has the shared `ADO-SqlServer` collection and no Redis collection.
- `.github/coverage-manifest/Encina.DistributedLock.json`: package targets guard 20, unit 60; existing entries stay.

---

## Design Choices

<details>
<summary><strong>1. Where renewal lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One internal wrapper and one internal extender, used by both a provider decorator (global option) and the pipeline behavior (attribute)** | The issue's two switches share one implementation | Two entry points to document |
| **B) Only the attribute (behavior) path** | Smaller | `AutoExtendLocks` of the issue's options is not delivered; direct provider users renew by hand |
| **C) Inside each provider** | Native renewal per backend | Seven implementations of one loop; the decorators and the multi-lock would not see them |

### Chosen Option: **A**

### Rationale

- A delivers both items of the issue's acceptance criteria (`AutoExtendLocks` option and the `AutoExtend` attribute property) once, on every provider.

**Class:** Settled
**Settled by:** issue #223 Acceptance Criteria ("`AutoExtendLocks` option in `DistributedLockOptions`", "Integration with `DistributedLockPipelineBehavior`", "`AutoExtend` property in `[DistributedLock]` attribute")

</details>

<details>
<summary><strong>2. How a lost lock reaches the work it protected</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The handle exposes `LockLostToken`; the behavior also publishes it through an ambient `IDistributedLockScopeAccessor` (singleton, `AsyncLocal`, the pattern of `IRequestContextAccessor`) so a handler can read it; when the lock was lost while the handler ran, the behavior returns `Left` `lock_pipeline.lock_lost` (`Conflict`) instead of the handler's result** | Handlers can stop themselves; the caller never sees success for work done without the lock | The handler's side effects cannot be undone; a retry by the caller can repeat them (documented: handlers must be idempotent or check the token) |
| **B) The token on the handle only** | Smallest | A pipeline handler has no handle to read it from |
| **C) The behavior races `nextStep()` against the token and returns early** | Caller is released at once | The handler keeps running without the lock, orphaned and unobservable |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- A wins when handlers do long, interruptible work; B wins for direct provider users, where it is also provided; C is the worst case for correctness. `RequestHandlerCallback` takes no token, so the behavior cannot cancel the handler itself (`RequestHandlerDelegate.cs:9`). The accessor is a singleton and the behavior is transient, so no scoped service is injected into a singleton.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The renewal loop</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A sequential loop task per handle over `PeriodicTimer(interval, timeProvider)`, every iteration in `try/catch` (an `OperationCanceledException` from the stopping token ends the loop), logged by error code or `ForLogging()`** | No overlap of extensions; deterministic with `FakeTimeProvider`; failures never escape | One task per held lock |
| **B) `TimeProvider.CreateTimer` with an async callback** | No dedicated task | Callbacks can overlap; an escaped exception ends the process |

### Chosen Option: **A**

### Rationale

- Both can comply if every failure is caught; A removes the overlap hazard. `DisposeAsync` cancels the loop, waits for it (bounded by the interval), and only then disposes the inner handle, serialized with an in-flight extension by one `SemaphoreSlim`.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 3 (background loops and timers never let an exception escape; `PeriodicTimer` loops catch per iteration; time through `TimeProvider`)

</details>

<details>
<summary><strong>4. When a lock counts as lost</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed: `Right(false)` is lost at once; a `Left` (backend fault) is retried every `ExtensionRetryInterval` while `now < localLeaseExpiry` (the time read before the last confirmed extend plus the duration); past that instant the lock is lost** | No handler keeps believing it holds a lock the backend may have given away | A backend outage longer than one lease cancels handlers |
| **B) Lost only on a definite `Right(false)`; faults are retried forever** | Outages do not cancel work | After the lease expired another holder may have the lock while this handler still runs |

### Chosen Option: **A - fail closed** (recommended, pending the maintainer)

### Rationale

- A wins for correctness-critical work (the same rule #216 applies to leadership); B wins for long idempotent jobs on an unreliable network. `AbortOnExtensionFailure` (default true) decides whether loss cancels the token; false only logs and counts. Retry count is bounded by the lease, not by a number.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Configuration home</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new `LockAutoExtendOptions` (`AutoExtendLocks`, `Interval` 10 s, `Duration` null meaning the lease the lock was acquired with, `AbortOnExtensionFailure` true, `ExtensionRetryInterval` 1 s) with a validator (`Interval * 2 <= Duration`, all positive)** | Validated on start; `DistributedLockOptions` (shared by every provider) stays unchanged | A second options class |
| **B) The four properties on `DistributedLockOptions`, as the issue sketches** | One options class | No validation hook exists there today; every provider's options inherit the new properties |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- A wins when options must be validated at startup; B wins when "all lock settings in one object" matters more. The attribute carries `AutoExtend` as an enum `LockAutoExtendMode { Inherit, Enabled, Disabled }` (a `bool?` is not a legal attribute argument type) and `AutoExtendIntervalSeconds` (0 inherits).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Decoration order and double wrapping</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Documented order: register the lock provider, then auto-extend, then tenant scoping (#741), then telemetry (#724); the behavior skips wrapping a handle that already is an `IAutoExtendingLockHandle`; the extender emits its own instruments, so telemetry does not depend on the order** | No double renewal; clear README rule | Order matters |
| **B) Detect and reorder decorators automatically** | No rule to remember | Hidden behavior in the container |

### Chosen Option: **A**

### Rationale

- The same "decorates the registered provider, which must be registered first" rule as #724 and #741.

**Class:** Trivial
**Settled by:** `docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md` (a public opt-in registration that decorates the registered provider, registered first)

</details>

---

## Implementation Phases

### Phase 1: Options, Handle Interface and Wrapper (PR 1)

> **Goal**: The renewal engine, tested on the InMemory provider with a fake clock.

<details>
<summary><strong>Tasks</strong></summary>

1. **`AutoExtend/LockAutoExtendOptions.cs`** and **`LockAutoExtendOptionsValidator.cs`** (new) - Design Choice 5.
2. **`AutoExtend/IAutoExtendingLockHandle.cs`** (new, public) - `ILockHandle` plus `CancellationToken LockLostToken { get; }` and `bool IsLockLost { get; }`.
3. **`AutoExtend/AutoExtendingLockHandle.cs`** (new, internal sealed) and **`LockAutoExtender.cs`** (new, internal sealed, `Wrap(ILockHandle, ...)`): the loop of Design Choice 3, the accounting of Design Choice 4, `ExpiresAtUtc` = local lease expiry, manual `ExtendAsync` passes through and resets the local expiry, `DisposeAsync` stops the loop first.
4. **Tests** (unit, InMemory provider, `FakeTimeProvider`): renews on every tick and keeps the lock past its original expiry; `Right(false)` cancels the token at once; faults retried until the local expiry then lost; `AbortOnExtensionFailure = false` only logs; dispose stops the loop before release and is idempotent; an exception in the loop never escapes and the loop continues; manual extend; validator bounds; guard test of the validator. PublicAPI lines and manifest entries (`mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #223: the auto-extending lock handle.

CONTEXT:
- #2667 has changed ILockHandle.ExtendAsync and the provider methods to the ADR-052 Either shapes; read the current signatures.
- The wrapper uses only ILockHandle.ExtendAsync (ownership-checked), never IDistributedLockProvider.ExtendAsync(resource).
- Time comes from TimeProvider; PeriodicTimer(interval, timeProvider).

TASK:
Create LockAutoExtendOptions and its validator, IAutoExtendingLockHandle, AutoExtendingLockHandle and LockAutoExtender with the tests of the Phase 1 tasks.

KEY RULES:
- The loop catches per iteration and never throws; no async void; no Timer callback with an async lambda.
- Local lease expiry = time read BEFORE the extend call + duration; past it without a confirmed extension the lock is lost.
- Never log a resource name or EncinaError.Message.

REFERENCE FILES:
- src/Encina.DistributedLock/ILockHandle.cs, src/Encina.DistributedLock.InMemory/InMemoryDistributedLockProvider.cs
- docs/plans/leader-election-provider-implementation-plan-216.md (same lease accounting)
```

</details>

### Phase 2: Global Decorator and Registration (PR 2)

> **Goal**: `AutoExtendLocks = true` renews every lock.

<details>
<summary><strong>Tasks</strong></summary>

1. **`AutoExtend/AutoExtendDistributedLockProvider.cs`** (new, internal sealed decorator over `IDistributedLockProvider`): `TryAcquireAsync` and `AcquireAsync` wrap a `Some`/`Right` handle with `LockAutoExtender` when `AutoExtendLocks` is true; the other members forward unchanged; a `Left` and `Right(None)` are returned unchanged.
2. **`ServiceCollectionExtensions.cs`** (modify) - `AddEncinaDistributedLockAutoExtend(Action<LockAutoExtendOptions>?)`: validated options, decorates the registered provider, `InvalidOperationException` naming the missing call when none is registered, a second call changes nothing (registration completeness).
3. **Tests** - decorator on the InMemory provider (wrapped when on, untouched when off, contention and faults unchanged), DI test with `ValidateOnBuild` and `ValidateScopes`, guard test of the registration. README section and changelog fragment `changelog.d/223-lock-auto-extend.added.md` (specialists).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #223: the auto-extend provider decorator and its registration.

CONTEXT:
- Decorators over IDistributedLockProvider follow the shape of #724 and #741: they decorate the registered provider, which must be registered first.

TASK:
Create AutoExtendDistributedLockProvider and AddEncinaDistributedLockAutoExtend with the tests of the Phase 2 tasks.

KEY RULES:
- Forward every member; never swallow a Left; no scoped service captured by the singleton decorator.

REFERENCE FILES:
- docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md, src/Encina.DistributedLock/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Pipeline Integration (PR 3)

> **Goal**: `[DistributedLock(AutoExtend = ...)]` and the lost-lock outcome.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Pipeline/DistributedLockAttribute.cs`** (modify, from #220) - `AutoExtend` (`LockAutoExtendMode`, default `Inherit`) and `AutoExtendIntervalSeconds`; **`LockAutoExtendMode.cs`** is part of PR 1's file list as an enum in `AutoExtend/` (counted there).
2. **`AutoExtend/IDistributedLockScopeAccessor.cs`** and **`DistributedLockScopeAccessor.cs`** (new; singleton, `AsyncLocal<CancellationToken>`; the behavior sets it before `nextStep()` and restores it after).
3. **`Pipeline/DistributedLockPipelineBehavior.cs`** (modify) - wraps the handle when the attribute enables it (or the global option is on and the handle is not wrapped yet), publishes the token, and after `nextStep()` returns `lock_pipeline.lock_lost` when `IsLockLost`; constructor gains `LockAutoExtender` and `IDistributedLockScopeAccessor`. **`DistributedLockPipelineErrorCodes.cs`** (modify): `lock_pipeline.lock_lost` (`Conflict`).
4. **`ServiceCollectionExtensions.cs`** (modify) - registers the accessor and the extender (`TryAdd`).
5. **Tests** - attribute on and off, interval override, loss while the handler runs returns the Left and cancels the token the handler read, no double wrapping with the global switch, release after loss; the three constructor sites of #220 (behavior unit test, DI test, guard test) are updated. PublicAPI and manifest rows (`mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #223: auto-extend inside DistributedLockPipelineBehavior (#220).

CONTEXT:
- RequestHandlerCallback takes no token: the handler can only read the lost token through IDistributedLockScopeAccessor.
- The behavior is transient; accessor and extender are singletons.

TASK:
Extend the attribute, the behavior, the error codes and the registration; update every constructor call site (grep new DistributedLockPipelineBehavior< in src and tests first).

KEY RULES:
- A lost lock turns the result into Left lock_pipeline.lock_lost even if the handler returned Right; document that handlers must be idempotent.
- Restore the accessor value in finally; never log resource names.

REFERENCE FILES:
- docs/plans/lock-pipeline-behavior-implementation-plan-220.md, src/Encina/Abstractions/IRequestContextAccessor.cs
```

</details>

### Phase 4: Observability (PR 4)

> **Goal**: Renewals and losses are visible.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/AutoExtendLog.cs`** (new, `internal static partial`) - 30609 renewal started (Debug), 30610 extension failed and will be retried (Warning: error code), 30611 lock lost (Warning: reason `not_held`, `lease_expired`), 30612 renewal loop iteration failed (Error: `ForLogging()`), 30613 renewal stopped (Debug). Ids 30609-30613 of the family range `DistributedLockPipeline` (30600-30649).
2. **`Diagnostics/AutoExtendMetrics.cs`** (new, internal) - on the shared `DistributedLockDiagnostics` meter: counter `encina.distributed_lock.auto_extend.extensions` (tag `encina.lock.outcome`: `extended`, `failed`, `lost`) and up-down counter `encina.distributed_lock.auto_extend.active`; no `_total`; no tenant tag because a handle has no request context (the behavior's own counter of #220 carries it with multi-tenancy); never a resource name.
3. **EventId registration** only if this PR is the first of the family with a log (`EventIdRanges.cs` field `DistributedLockPipeline = (30600, 30649)`, core `PublicAPI.Unshipped.txt`, one registry row with the same bounds, `AssemblyRanges` entry; owned by #220 PR 4 by default).
4. **Tests** - `MeterListener` and log tests; load and benchmark justification `.md` files.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #223: telemetry of auto-extension.

CONTEXT:
- Ids 30609-30613 belong to the family range DistributedLockPipeline, registered by #220 PR 4 or by the first family PR with a log.

TASK:
Write AutoExtendLog and AutoExtendMetrics, emit them from the extender and add the tests.

KEY RULES:
- Pack ids sequentially; log error codes or exception types only; never resource names or EncinaError.Message.

REFERENCE FILES:
- docs/engineering/eventid-block-registry.md, docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md
```

</details>

### Phase 5: Contract and Integration Scenarios (PR 5)

> **Goal**: Renewal is proven on real backends.

<details>
<summary><strong>Tasks</strong></summary>

1. **`tests/Encina.ContractTests/DistributedLock/AutoExtendContractTests.cs`** (new) over the InMemory provider; `Encina.ContractTests.csproj` needs the two `ProjectReference` lines unless #220 PR 5 added them (it has none today).
2. **Integration** - `AutoExtendRedisIntegrationTests.cs` (shared Redis collection added by #220 PR 5, or here if first: 2 more files): a handler longer than the lease keeps the lock; deleting the key cancels the token. SQL Server: the same scenario waits for #2405 (its locks never expire, so renewal is a no-op there, documented in the README).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #223: contract and integration tests of auto-extension.

CONTEXT:
- Integration tests use shared [Collection] fixtures only; Redis expires on the server, so use short real expiries.

TASK:
Write the contract test and the Redis integration class.

KEY RULES:
- Real implementations only; FakeTimeProvider only on InMemory.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/RedisDistributedLockIntegrationTests.cs
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| SPEC-000 REQ-027, AC-027 | Seven lock providers |
| ADR-052, ADR-062, ADR-054 | Either outcomes, categories, telemetry |
| Azure Blob lease renewal; DynamoDB lock client heartbeat | Comparable renewal models |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `ILockHandle.ExtendAsync` | `src/Encina.DistributedLock/ILockHandle.cs` | The only renewal call |
| `IRequestContextAccessor` | `src/Encina/Abstractions/IRequestContextAccessor.cs` | Pattern for the scope accessor |
| `DistributedLockPipelineBehavior`, `DistributedLockAttribute` | #220 | Per-lock switch |
| Decorators and `DistributedLockDiagnostics` | #724, #741 | Decoration order, meter |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Renewal | Status | Proven by |
|----------|---------|--------|-----------|
| InMemory | resets the entry expiry (owner-checked) | exists | PR 1 unit, PR 5 contract |
| Redis | ownership-checked Lua `PEXPIRE` | exists | PR 5 integration |
| SQL Server | no-op: locks are held until released, expiry ignored (#2405) | exists | documented; no expiry scenario until #2405 |
| PostgreSQL, MySQL | session-held locks never expire (like SQL Server) | planned (#207, #208) | documented in each package |
| Azure Blob, DynamoDB | lease renewal and heartbeat: the natural targets | planned (#209, #210) | one contract subclass in each package's PR |

### Event ID Allocation

| Package | Range | Ids |
|---------|-------|-----|
| `Encina.DistributedLock` (`DistributedLockPipeline`, registered by #220 PR 4; first free 50-slot after 30599, no claim on main, in an open plan head or in the registry, checked 2026-11-27) | `30600-30649` | #223 uses 30609-30613; one registry row with identical bounds; no id below 10000 |

### Estimated File Count

| PR | Content | New | Modified | Tests, manifests, PublicAPI, docs | Total | Lines (est.) |
|----|---------|-----|----------|------------------------------------|-------|--------------|
| 1 | Options, interface, mode enum, wrapper, extender | 6 (options, validator, interface, enum, wrapper, extender) | 1 (PublicAPI) | 3 (wrapper tests, options tests, validator guard test) + manifest | 11 | 1,300 |
| 2 | Decorator, registration | 1 | 4 (`ServiceCollectionExtensions.cs`, PublicAPI, README, manifest) | 4 (decorator tests, DI test, guard test, fragment) | 9 | 800 |
| 3 | Pipeline integration | 2 (accessor interface and class) | 5 (attribute, behavior, error codes, `ServiceCollectionExtensions.cs`, PublicAPI) | 5 (new behavior auto-extend tests, behavior tests, DI tests, guard tests, manifest) | 12 | 950 |
| 4 | Log, metrics (+4 if first of the family) | 2 | 1 (extender) | 5 (metrics tests, log tests, manifest, 2 justification `.md`) | 8 (12) | 600 |
| 5 | Contract, integration | 2 (contract, Redis integration) | 1 (ContractTests csproj) | 0 | 3 (5 with the Redis fixture) | 450 |

Total about 4,100 lines. Every PR is at most 15 files and 1,500 lines and builds and passes alone (PR 3 needs #220 PR 2; PR 4 needs PR 1).

### Per-file coverage targets (numeric, per flag)

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `AutoExtendingLockHandle.cs`, `LockAutoExtender.cs` | 95 | 0 | 60 | 30 | Logic over an `ILockHandle` with a fake clock on the InMemory provider; internal types with no `InternalsVisibleTo` for `Encina.GuardTests`, so guard tests cannot reach them (arguments are checked through the public registration) |
| `AutoExtendDistributedLockProvider.cs` | 95 | 0 | 40 | 0 | Forwarding decorator, internal type; every branch on the InMemory provider |
| `LockAutoExtendOptionsValidator.cs`, `LockAutoExtendOptions.cs` | 95 | 40 | 0 | 0 | Pure validation |
| `DistributedLockScopeAccessor.cs`, `AutoExtendMetrics.cs` | 90 | 0 | 0 | 0 | `AsyncLocal` and `MeterListener` tests; internal types |
| `IAutoExtendingLockHandle.cs`, `IDistributedLockScopeAccessor.cs`, `LockAutoExtendMode.cs`, `AutoExtendLog.cs` | 0 | 0 | 0 | 0 | Interfaces, an enum and source-generated log methods: no executable logic |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is pre-1.0 (.NET 10, C# 14, nullable enabled). Rules are in AGENTS.md. Issue #223 adds automatic lock renewal to
Encina.DistributedLock by composing ILockHandle; no provider package changes and no change to ILockHandle or IDistributedLockProvider.

IMPLEMENTATION OVERVIEW:
1. Phase 1: LockAutoExtendOptions, IAutoExtendingLockHandle, AutoExtendingLockHandle, LockAutoExtender.
2. Phase 2: AutoExtendDistributedLockProvider decorator and AddEncinaDistributedLockAutoExtend.
3. Phase 3: [DistributedLock(AutoExtend = ...)] in the #220 behavior, scope accessor, lock_pipeline.lock_lost.
4. Phase 4: log 30609-30613 and metrics in the family range 30600-30649.
5. Phase 5: contract and Redis integration tests.

KEY PATTERNS:
- Sequential PeriodicTimer loop per handle, per-iteration catch, never async void.
- Fail closed: lease accounting from the time before the extend call; a lost lock cancels LockLostToken.
- Dilemmas 2, 4 and 5 are pending the maintainer: implement the recommended options only after he answers.
- SQL Server ignores lock expiry until #2405: state it, never claim renewal there.

REFERENCE FILES:
- docs/plans/lock-auto-extend-implementation-plan-223.md, src/Encina.DistributedLock/
- docs/plans/leader-election-provider-implementation-plan-216.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Locks are ephemeral coordination primitives |
| 2 | OpenTelemetry | ✅ Include | Phase 4: extension outcome counter and active-renewals gauge; no tenant tag (a handle has no request context), no resource names |
| 3 | Structured Logging | ✅ Include | Phase 4: `AutoExtendLog` 30609-30613, error codes and exception types only |
| 4 | Health Checks | ❌ N/A | Renewal is per lock, not a service dependency; backend health is the provider's |
| 5 | Validation | ✅ Include | `LockAutoExtendOptions` validated on start; attribute interval validated by #220's plan resolution |
| 6 | Resilience | ✅ Include | Failed extensions are retried while the local lease lasts (Design Choice 4); the retry interval is an option |
| 7 | Distributed Locks | ❌ N/A | This is a lock feature, not a consumer |
| 8 | Transactions | ❌ N/A | Locks are separate from transactions; the behavior orders them (#220) |
| 9 | Idempotency | ❌ N/A | Renewal is owner-checked and repeatable; a lost-lock `Left` may be retried by the caller, so handlers must be idempotent (documented) |
| 10 | Multi-Tenancy | ⏭️ Defer | Tenant scoping of lock names is the decorator of #741, which composes with this one (Design Choice 6) |
| 11 | Module Isolation | ❌ N/A | Modules are separated with `KeyPrefix` (the position of the #741 plan) |
| 12 | Audit Trail | ❌ N/A | Infrastructure coordination; a loss is logged by code and counted, not audited |

---

## Prerequisites & Dependencies

| Item | State | Needed for |
|------|-------|------------|
| #2667 ADR-052 implementation | open | All PRs |
| #220 PR 2 (behavior) | plan on main | PR 3 |
| Family EventId range | registered by #220 PR 4 or the first family PR with a log | PR 4 |
| #2405 SQL Server expiry and non-owned `ExtendAsync` | open bug | Any SQL Server renewal claim |
| #2653 duplicate caching lock providers | open | The two caching providers must return `ILockHandle` after #2667 |

---

## Next Steps

1. After #2667 merges open PR 1, then PR 2; PR 3 after #220 PR 2; PR 4 after PR 1; PR 5 after PR 2.
2. The maintainer answers Dilemmas 2, 4 and 5 when work on the issue starts; PRs are written for the recommended answers.
