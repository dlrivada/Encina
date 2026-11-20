# Implementation Plan: OpenTelemetry for the Distributed Lock Providers (One Decorator, Acquisition Outcome Metrics, No Resource Names)

> **Issue**: [#724](https://github.com/dlrivada/Encina/issues/724)
> **Type**: Feature (observability)
> **Related**: ADR-054 (D3, D4, D6.1 locks at Encina operation level, D6.3 no data-bearing names), ADR-052 and #2667 (the Either contract this decorator is written against), #222 (lock metrics catalog, planned separately: boundary in Design Choice 4), #741 (tenant-scoped locks, owns the range registration), #717 (leader election), #207, #208, #209, #210 (planned providers), #2676, #906, ADR-062
> **Complexity**: Medium (one decorator, a handle wrapper that must keep `ILockHandle`, and a sequencing question)
> **Estimated Scope**: ~260 lines of production code + ~700 lines of tests, in three pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-20, main `e31de98cb`)

- No lock provider emits telemetry. `IDistributedLockProvider` (`src/Encina.DistributedLock/IDistributedLockProvider.cs`) has `TryAcquireAsync` (returns `IAsyncDisposable?`, `null` on contention), `AcquireAsync`, `IsLockedAsync` and `ExtendAsync`. Five classes implement it: `InMemoryDistributedLockProvider`, `RedisDistributedLockProvider` and `SqlServerDistributedLockProvider` in the `Encina.DistributedLock.*` packages, and `MemoryDistributedLockProvider` and `RedisDistributedLockProvider` in `Encina.Caching.Memory` and `Encina.Caching.Redis`. The issue lists three; the ADR-054 D6.1 count and the code are five, and #207 to #210 add four more.
- The returned object is a private `LockHandle : ILockHandle` in each of the three lock packages, and callers cast it to `ILockHandle` (integration tests do, for example `(ILockHandle)await _provider.AcquireAsync(...)`). A wrapper that hides `ILockHandle` would break them.
- **ADR-052 (accepted) changes the contract**: #2667 (open, no plan on main) makes `TryAcquireAsync` return `Either<EncinaError, Option<ILockHandle>>` and `AcquireAsync` `Either<EncinaError, ILockHandle>`, with `IsLockedAsync` and `ExtendAsync` returning `Either<EncinaError, bool>`. A decorator written today would be rewritten by that change (Design Choice 1).
- The lock package is where the sibling plans put telemetry: #741 creates `Diagnostics/DistributedLockDiagnostics.cs` (one `ActivitySource` and one `Meter`, both named `Encina.DistributedLock`) and #717 adds its instruments to it; both say they reuse the instruments of this plan if it lands first. Resource names are application text such as `saga:payment:{orderId}` (the XML example of `IDistributedLockProvider`), so they carry data.
- `AddEncinaDistributedLock()` is a marker that registers nothing; each provider registers itself with `TryAddSingleton<IDistributedLockProvider, ...>`; `src/Encina.DistributedLock/Encina.DistributedLock.csproj` has `InternalsVisibleTo` for UnitTests, IntegrationTests, ContractTests, PropertyTests, the three lock providers and `Encina.Caching`, but **not for `Encina.GuardTests`**.
- No `src` code outside the lock and caching packages consumes the interface yet; the outbox, inbox, saga and election plans (#713, #714, #715, #716, #717) add consumers.

### What this plan delivers

- `InstrumentedDistributedLockProvider` (decorator over any provider) and `InstrumentedLockHandle : ILockHandle`, in `Encina.DistributedLock/Diagnostics/`, with spans `encina.distributed_lock.try_acquire|acquire|is_locked|extend|release`, outcome `acquired`, `contended` or `failed`, and the provider label derived from the provider type name (`redis`, `sqlserver`, `inmemory`, `memory`, and the four planned providers automatically).
- Instruments on `Meter("Encina.DistributedLock")`: `encina.distributed_lock.acquisitions` (counter; tags `encina.lock.operation`, `encina.lock.outcome`, `encina.lock.system`, `error.type` on failures), `encina.distributed_lock.acquisition.duration` (histogram, `s`) and `encina.distributed_lock.held` (up-down counter of handles not yet disposed).
- A public opt-in registration `AddEncinaDistributedLockTelemetry()` that decorates the registered provider (the provider must be registered first, as for #741's decorator).
- The tenant tag through the helper of #2676; the resource name is never emitted.
- Shared pieces and owner: this plan owns the decorator and the three instruments above; `DistributedLockDiagnostics` is created by whichever of #724 and #741 PR 2 lands first (it holds only source, meter and name constants); #222 adds hold duration, extension and release counters and waiting requests.
- Affected packages: `Encina.DistributedLock` (new code, `InternalsVisibleTo` entry, PublicAPI). Provider category: Distributed locks (5 exist, 4 planned): all covered by interface.

---

## Design Choices

<details>
<summary><strong>1. Sequencing against the ADR-052 contract (#2667)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Start after #2667 merges and write the decorator against the `Either` shapes** | No throwaway code; the handle is `ILockHandle` directly, so the wrapper needs no cast; failures are `Left` values the decorator can classify | #724 waits for a pull request that touches about 35 test files |
| **B) Write against today's `IAsyncDisposable?` shape and rewrite in #2667's pull request** | Starts now | The decorator and its tests change in #2667's pull request; contention is `null`, faults are raw exceptions, so the outcome logic is different code |

### Chosen Option: **A - After #2667** (recommended, pending the maintainer)

### Rationale

- A wins because pre-1.0 values the final design over an early start (`AGENTS.md` section 1); B wins if the maintainer needs lock telemetry before ADR-052 is implemented. The tasks below name the `Either` shapes; the today shapes differ only in how the outcome is read (`null` is `contended`, an exception is `failed`).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the decorator lives and how it is applied</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `Encina.DistributedLock`, folder `Diagnostics/`, applied by a public `AddEncinaDistributedLockTelemetry()`** | Same package and names as #741 and #717 (`Encina.DistributedLock` source and meter); no new project reference; works without `Encina.OpenTelemetry` | A new public registration; provider-first order; an `InternalsVisibleTo` entry for guard tests |
| **B) In `Encina.OpenTelemetry`, applied by `AddEncinaOpenTelemetry`** | One call for all telemetry, like the cache decorator | `Encina.OpenTelemetry` gains a reference to `Encina.DistributedLock`; the meter would be named after the emitter, breaking the shared holder of #741 |
| **C) Inside each provider** | Provider detail | 5 copies now and 4 more planned (ADR-054 D6.1 asks for Encina level) |

### Chosen Option: **A - In the lock package** (recommended, pending the maintainer)

### Rationale

- A wins on consistency with the sibling plans; B wins if the maintainer wants a single switch for all decorators. `ServiceCollectionExtensions` registration order rule: the decorator wraps the provider registered at call time and throws `InvalidOperationException` when none is, so a wrong order fails loudly instead of silently skipping.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What identifies a lock in telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Nothing about the resource; the provider label and the operation only** | Complies with ADR-054 D6.3 whatever the application puts in a name | The issue's `lock.name` is lost |
| **B) The segment before the first `:` (at most 32 characters)** | Groups by lock family (`saga`, `outbox`) | Leaks when a name starts with an id |
| **C) A caller-supplied low-cardinality label** | Exact and safe | A new parameter on the provider interface |

### Chosen Option: **A - No resource information** (recommended, pending the maintainer)

### Rationale

- A wins because lock names are free text and the interface has no label; B wins for deployments whose lock names are all family-first; C wins if #222 needs per-family contention and the maintainer accepts an interface change. The provider label comes from the type name (`RedisDistributedLockProvider` gives `redis`), verified by a test per provider.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Boundary with #222 and the meaning of "active locks"</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) This plan: acquisitions, acquisition duration and held handles; #222 adds hold duration, wait duration, extensions, releases, waiting requests; `held` counts handles created and not yet disposed** | No duplicate instruments; the outcome tag gives contention and timeout rates | A lock that expires without being disposed stays counted until the handle is disposed; documented |
| **B) Include hold duration and extensions now** | Closer to #222's text | Duplicates #222 |
| **C) No `held` instrument** | No drift | The issue asks for active locks |

### Chosen Option: **A - Three instruments** (recommended, pending the maintainer)

### Rationale

- A wins now; B wins if #222 is dropped. #222's plan must reconcile with this boundary.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Cost, time, tenant and errors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `HasListeners()` for spans, `Instrument.Enabled` for metrics, injected `TimeProvider`, `error.type` is the `EncinaError` code, tenant tag from #2676; the decorator returns the inner `Either` unchanged and never retries** | Zero cost without a listener; contention is not counted as a failure | One optional `TimeProvider` parameter |
| **B) Always measure** | Simpler | Pays per call |

### Chosen Option: **A - Guarded and pass-through**

### Rationale

- ADR-054 settled points 2 and 7, D4; ADR-052 (contention is `Right(None)`, never an error); `AGENTS.md` section 3.

**Class:** Trivial
**Settled by:** ADR-054 settled points 2 and 7 and D4; ADR-052; `AGENTS.md` section 3

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code (error codes of #2667 pass through), so no ADR-062 category here; no logging and no EventId: the `DistributedLock` range 6250-6299 belongs to #741 and #717 | Settled | ADR-062; `AGENTS.md` section 7 |
| C2 | The wrapper handle forwards `ExtendAsync`, `Resource`, `LockId`, `AcquiredAtUtc`, `ExpiresAtUtc` and dispose unchanged, and double dispose records once | Settled | `ILockHandle` contract (`src/Encina.DistributedLock/ILockHandle.cs`) |
| C3 | No resource name, no lock id and no tenant text in any tag; no tenant-qualified id is built, so the reserved `default` segment does not apply | Settled | ADR-054 D6.3, D4 |
| C4 | A failure inside the decorator itself never changes the result: it records and returns the inner result | Settled | `AGENTS.md` section 3 |

---

## Implementation Phases

### Phase 1: Decorator, Handle Wrapper and Instruments (pull request L1, after #2667)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create or complete** `src/Encina.DistributedLock/Diagnostics/DistributedLockDiagnostics.cs` (`internal static`): `ActivitySource` and `Meter` named `Encina.DistributedLock`, version `1.0`; if #741 PR 2 landed first, add only the three instruments of this plan.
2. **Create** `Diagnostics/InstrumentedDistributedLockProvider.cs` (`internal sealed`, `IDistributedLockProvider`): constructor `(IDistributedLockProvider inner, TimeProvider? timeProvider = null)`; each method starts a span, measures the duration and maps the result to `acquired` (`Right(Some)`), `contended` (`Right(None)`) or `failed` (`Left`, `error.type` = code); returns the inner value unchanged except that an acquired handle is wrapped.
3. **Create** `Diagnostics/InstrumentedLockHandle.cs` (`internal sealed`, `ILockHandle`) forwarding every member and decrementing `held` once on dispose.
4. **Modify** `ServiceCollectionExtensions.cs`: public `AddEncinaDistributedLockTelemetry(this IServiceCollection)` replacing the registered `IDistributedLockProvider` descriptor with the decorator (same lifetime), `InvalidOperationException` when none is registered; `Encina.DistributedLock.csproj` (`InternalsVisibleTo` `Encina.GuardTests`); `PublicAPI.Unshipped.txt`, manifest `Encina.DistributedLock.json` and changelog `changelog.d/724-lock-telemetry.added.md` through `mechanical-fixer`.
5. Tests: new `tests/Encina.UnitTests/DistributedLock/Diagnostics/InstrumentedDistributedLockProviderTests.cs` (real `InMemoryDistributedLockProvider` with `FakeTimeProvider`: acquired, contended, faulted inner via a throwing provider, instruments and span tags, no resource text in any tag, cast to `ILockHandle` still works, `held` returns to zero after dispose and after double dispose, no work without listener); new `InstrumentedLockHandleTests.cs`; new `tests/Encina.GuardTests/Infrastructure/DistributedLock/InstrumentedDistributedLockProviderGuardTests.cs` (null inner, null resource, null `timeProvider` valid, `services` null for the registration); edit the DI unit tests (provider built with `ValidateOnBuild` and `ValidateScopes`; no provider registered throws).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #724, plan docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md, phase 1.
#2667 merged: IDistributedLockProvider returns Either<EncinaError, Option<ILockHandle>> (TryAcquire), Either<EncinaError, ILockHandle>
(Acquire), Either<EncinaError, bool> (IsLocked, Extend). ADR-054: lowercase dotted names, seconds, no _total, never data-bearing names.

TASK:
1. DistributedLockDiagnostics (source and meter "Encina.DistributedLock"; reuse it if #741 created it).
2. InstrumentedDistributedLockProvider and InstrumentedLockHandle (keeps ILockHandle) with the three instruments.
3. AddEncinaDistributedLockTelemetry, InternalsVisibleTo Encina.GuardTests, PublicAPI/manifest/changelog through mechanical-fixer.
4. Unit, guard and DI tests with real providers.

KEY RULES:
- Never emit the resource name, lock id, tenant text or error message; contention is not a failure; return the inner result unchanged.
- Time through TimeProvider (optional last parameter, valid-result test for null); no scoped service in a singleton.

REFERENCE FILES:
- src/Encina.DistributedLock/IDistributedLockProvider.cs, ILockHandle.cs, ServiceCollectionExtensions.cs
- docs/plans/tenant-scoped-lock-implementation-plan-741.md (shared DistributedLockDiagnostics), docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

---

### Phase 2: Tenant Tag (pull request L2, after #2676)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `InstrumentedDistributedLockProvider.cs` and `DistributedLockDiagnostics.cs`: the tenant tag from the helper of #2676 on spans and instruments, only when multi-tenancy is on and a tenant resolves; absent otherwise.
2. Tests: edit the provider unit tests (tenant on, off, blank); changelog fragment `changelog.d/724-lock-telemetry-tenant.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #724 plan docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md, phase 2. #2676 merged (shared tenant tag helper).

TASK:
1. Add the tenant tag through the helper to spans and the acquisitions instrument only.
2. Tests for tenant on, off and blank.

KEY RULES:
- Only when multi-tenancy is enabled; never baggage; never the tenant in a lock name.

REFERENCE FILES:
- the helper of #2676, src/Encina.DistributedLock/Diagnostics/
```

</details>

---

### Phase 3: Proof on Real Providers (pull request L3)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.ContractTests/Infrastructure/DistributedLock/LockTelemetryContractTests.cs`: the in-memory, Redis (fake database) and SQL Server (fake connection) providers behind the decorator emit the same instrument names, tag keys and outcomes for acquire, contention and release.
2. **Create** `tests/Encina.IntegrationTests/Infrastructure/DistributedLock/LockTelemetryIntegrationTests.cs` using the existing Redis and SQL Server collection fixtures: real contention gives `contended`, a stopped server gives `failed`, no tag contains the resource name.
3. Manifest through `mechanical-fixer`; README of `Encina.DistributedLock` through `docs-writer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #724 plan docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md, phase 3. Phase 1 merged.

TASK:
1. Contract test over InMemory, Redis and SqlServer providers; integration test on real Redis and SQL Server.

KEY RULES:
- Shared collection fixtures only; real providers; no Thread.Sleep (use FakeTimeProvider where possible).

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/RedisDistributedLockIntegrationTests.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-054 | Settled points 2, 4, 7; D3, D4, D6 | Names, errors, scope |
| ADR-052 | Failure contract: contention is `Right(None)` | Outcome mapping |
| `AGENTS.md` section 5 | Lock category: TryAcquire with timeout, auto-release, `CancellationToken` | All providers by interface |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IDistributedLockProvider`, `ILockHandle` | `src/Encina.DistributedLock/` | Decorated |
| `DistributedLockDiagnostics` | #741 PR 2 (not on main) | Shared holder |
| Lock integration tests | `tests/Encina.IntegrationTests/Infrastructure/DistributedLock/` | Fixtures |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DistributedLock` | none | No logging code (register row C1); 6250-6299 is #741 and #717's |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| L1 | Phase 1 | 3 | 3 (`ServiceCollectionExtensions.cs`, csproj, PublicAPI) | 3 | 1 | 1 | 1 (changelog) | 12 | ~800 |
| L2 | Phase 2 | 0 | 2 | 0 | 1 | 0 | 1 (changelog) | 4 | ~100 |
| L3 | Phase 3 | 0 | 0 | 2 | 0 | 1 | 1 (README) | 4 | ~380 |
| **Total** | | **3** | **5** | **5** | **2** | **2** | **3** | **20** | **~1,280** |

Columns sum: 3 + 5 + 5 + 2 + 2 + 3 = 20 = 12 + 4 + 4. If #741 PR 2 landed first the `DistributedLockDiagnostics.cs` row moves from new to changed (no change in the total). Test doubles: the interface is not changed by this plan; the implementers affected by #2667 are #2667's.

Per-file coverage targets:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Diagnostics/InstrumentedDistributedLockProvider.cs` | 90 | 70 | 25 | 0 | 40 | All three outcomes and four methods are driven with real providers; guard covers constructor, resource and token arguments; contract and integration add Redis and SQL Server |
| `Diagnostics/InstrumentedLockHandle.cs` | 95 | 40 | 0 | 0 | 0 | Forwarding, dispose-once and extension are table-driven; guard covers the constructor; no contract of its own beyond `ILockHandle` |
| `Diagnostics/DistributedLockDiagnostics.cs` | 80 | 0 | 0 | 0 | 0 | Constants and instrument creation reached by unit tests; internal static with no validated parameter |
| `ServiceCollectionExtensions.cs` (existing file, see manifest) | 90 | 40 | 0 | 0 | 0 | The new registration and its two failure paths are driven by the DI tests; targets never go below the current manifest values |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #724 - OpenTelemetry for the distributed lock providers - in three small pull requests, after #2667.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0. Edit with Edit/Write only; PublicAPI, manifest and changelog through mechanical-fixer; README
  through docs-writer.
- ADR-054 names and privacy rules; ADR-052 Either contract; shared DistributedLockDiagnostics with #741 and #717.

IMPLEMENTATION OVERVIEW:
1. L1: InstrumentedDistributedLockProvider, InstrumentedLockHandle (keeps ILockHandle), three instruments, public registration.
2. L2 (after #2676): tenant tag.
3. L3: contract and integration proof on InMemory, Redis, SqlServer.

KEY PATTERNS:
- Never emit resource names, lock ids, tenant text or messages; contention is not a failure; pass the inner result through.
- Add InternalsVisibleTo Encina.GuardTests; null optional parameters get valid-result tests.

REFERENCE FILES:
- src/Encina.DistributedLock/, docs/plans/tenant-scoped-lock-implementation-plan-741.md, docs/plans/leader-election-implementation-plan-717.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Instrumentation observes locks; it adds no cached reads |
| 2 | OpenTelemetry | ✅ Phases 1-3 | This is the feature |
| 3 | Structured Logging | ❌ N/A | Providers already log lock failures; no EventId is added (register row C1) |
| 4 | Health Checks | ❌ N/A | Observes operations; provides no health status |
| 5 | Validation | ❌ N/A | No input beyond the provider's own checks |
| 6 | Resilience | ❌ N/A | Adds no external call and never retries |
| 7 | Distributed Locks | ✅ Phase 1 | The plan instruments the lock providers themselves |
| 8 | Transactions | ❌ N/A | Never commits or rolls back |
| 9 | Idempotency | ❌ N/A | Recording twice is harmless |
| 10 | Multi-Tenancy | ✅ Phase 2 | Tenant tag only when multi-tenancy is on, through the helper of #2676; resource names are never emitted |
| 11 | Module Isolation | ❌ N/A | No module data |
| 12 | Audit Trail | ❌ N/A | No personal data may reach telemetry |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #2667 (ADR-052 contract) | Open | L1 starts after it (Design Choice 1) |
| #2676 tenant helper | Open | Only L2 waits |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Tenant-scoped locks, shared diagnostics holder | #741 | Whichever lands first creates `DistributedLockDiagnostics.cs` |
| Leader election instruments | #717 | Adds its own to the holder |
| Lock metrics catalog | #222 | Planned separately; boundary in Design Choice 4 |
| Planned providers | #207, #208, #209, #210 | Covered by the decorator when they land |

---

## Next Steps

1. Link this plan from #724 and ask the maintainer to answer Design Choices 1 to 4 when work starts.
2. Implement L1 after #2667, then L2 and L3.
3. Maintainer action: amend the acceptance criteria of #724 that name three providers, `lock.name` and `lock.timeout` to the outcome of Design Choice 3 (this plan never comments on an issue).
