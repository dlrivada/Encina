# Implementation Plan: Distributed Lock Observability Gap (Hold Duration, Releases, Extensions, Waiting Requests, Slow-Acquisition Log)

> **Issue**: [#222](https://github.com/dlrivada/Encina/issues/222)
> **Type**: Feature (observability)
> **Related**: #724 (the sibling plan: decorator, handle wrapper, acquisitions counter, acquisition duration and held handles; this plan adds only what #724 does not), #2667 (ADR-052 `Either` contract that #724 and this plan are written against), #741 (tenant-scoped locks, owns `EventIdRanges.DistributedLock` and the shared `DistributedLockDiagnostics`), #717 (leader election instruments), #2676 (tenant tag helper), #755 (health check of the InMemory provider), #906, #908 (lock documentation), ADR-054 (D3, D4, D6.3), ADR-052, ADR-062
> **Complexity**: Medium (four instruments, one options class, two log events; the weight is the boundary with #724)
> **Estimated Scope**: ~300 lines of production code + ~750 lines of tests, in four pull requests of at most 15 files
> **Provider category**: Distributed locks (AGENTS.md section 5): five providers exist (`Encina.DistributedLock.InMemory`, `.Redis`, `.SqlServer`, and the lock providers inside `Encina.Caching.Memory` and `Encina.Caching.Redis`), four are planned (#207, #208, #209, #210); all are covered by interface through the #724 decorator.

---

## Summary

### What exists today (read on 2026-11-24, main `ff9a579c1`)

- No lock provider emits telemetry. `Encina.DistributedLock` has no `Diagnostics/` folder, no `ActivitySource`, no `Meter` and no log class (its csproj references `Microsoft.Extensions.Logging.Abstractions` but no code uses it). `IDistributedLockProvider` still has the pre-ADR-052 shape on main (`Task<IAsyncDisposable?> TryAcquireAsync(resource, expiry, wait, retry, ct)`, `AcquireAsync`, `IsLockedAsync`, `ExtendAsync`); #2667 (open) changes it to `Either`. `ILockHandle` has `Resource`, `LockId`, `AcquiredAtUtc`, `ExpiresAtUtc`, `IsReleased` and `ExtendAsync(TimeSpan, CancellationToken)`. `DistributedLockOptions` (`KeyPrefix`, `DefaultExpiry`, `DefaultWait`, `DefaultRetry`, `ProviderHealthCheck`) is the existing options class; `AddEncinaDistributedLock()` registers nothing.
- **The #724 plan (merged on main as a plan) already covers most of the issue's catalog.** It plans, in `Encina.DistributedLock/Diagnostics/`, `InstrumentedDistributedLockProvider` and `InstrumentedLockHandle` with spans `encina.distributed_lock.try_acquire|acquire|is_locked|extend|release`, the counter `encina.distributed_lock.acquisitions` (tags `encina.lock.operation`, `encina.lock.outcome` = `acquired`, `contended` or `failed`, `encina.lock.system`, `error.type`), the histogram `encina.distributed_lock.acquisition.duration` (`s`) and the up-down counter `encina.distributed_lock.held`, on `Meter("Encina.DistributedLock")`, applied by a public `AddEncinaDistributedLockTelemetry()`. It never emits the resource name (its Design Choice 3, a Dilemma pending the maintainer) and gives #222 the remainder in its Design Choice 4. The shared `DistributedLockDiagnostics` holder is created by whichever of #724 and #741 lands first.
- **#741 owns the range**: `EventIdRanges.DistributedLock = (6250, 6299)` and the `AssemblyRanges` mapping of `Encina.DistributedLock` are planned there (6250-6269 for #741, 6270-6299 for #717); no id of that block is free.

### Issue item against plan

| Item of #222 | Where it is covered | What this plan does |
|--------------|---------------------|---------------------|
| `LockAcquisitions`, `LockAcquisitionFailures`, `LockTimeouts` | #724 `acquisitions`: outcome `acquired`, `failed`, `contended` (a `TryAcquireAsync` that returns no handle is the timeout) | Nothing: one counter with an outcome tag replaces three counters; the doc table maps the names |
| `LockAcquisitionDuration`, `LockWaitDuration` | #724 `acquisition.duration` (the `TryAcquireAsync` call includes the wait and retry loop, so wait time is its value for contended and acquired outcomes) | Nothing: no provider can split network latency from retry waiting through the interface |
| `ActiveLocks` | #724 `held` | Nothing |
| `LockExtensions` | none | New counter `encina.distributed_lock.extensions` |
| `LockReleases`, `LockHoldDuration` | none (#724 decrements `held` on dispose but counts nothing and times nothing) | New counter `encina.distributed_lock.releases` and histogram `encina.distributed_lock.hold.duration` |
| `WaitingRequests` | none | New up-down counter `encina.distributed_lock.waiting` |
| `DistributedLockActivitySource.StartAcquire/StartExtend/StartRelease` | #724 spans | Nothing |
| `DistributedLockObservabilityOptions` | none | New public options class, one property (Design Choice 2) |
| `SlowAcquisitionThreshold` and LoggerMessage logging | none | Two log events and a span tag (Design Choice 5) |
| High-cardinality protection (`IncludeResourceInMetrics`, `ResourceMetricAggregator`) | #724 Design Choice 3 (resource never emitted) | Not added (Design Choice 3) |
| Integration in every provider | #724: one decorator over the interface | Proof by contract and integration tests over InMemory, Redis and SQL Server |
| Documentation | #908 (lock documentation plan) and #906 | One metric table in the package README |

### What this plan delivers

- Four instruments added to the #724 holder `DistributedLockDiagnostics` on `Meter("Encina.DistributedLock")`: `encina.distributed_lock.extensions` (counter; tags `encina.lock.system`, `encina.lock.outcome` = `extended`, `lost` (the provider answered `false`: the lock was no longer held) or `failed`, `error.type` on failures), `encina.distributed_lock.releases` (counter; tags `encina.lock.system`, `encina.lock.release.outcome` = `released` or `expired`), `encina.distributed_lock.hold.duration` (histogram, unit `s`, same tags as releases) and `encina.distributed_lock.waiting` (up-down counter of acquire calls in flight; tag `encina.lock.system`).
- A public `DistributedLockObservabilityOptions` with `SlowAcquisitionThreshold` (default 5 seconds, validated greater than zero) and a second overload `AddEncinaDistributedLockTelemetry(Action<DistributedLockObservabilityOptions>)` that registers the options with validation on start.
- Two structured log events in `Diagnostics/DistributedLockLog.cs`: a Warning when an acquire call takes at least the threshold, and a Warning when a handle is disposed after its `ExpiresAtUtc` (the lock may have been lost). Neither logs the resource name or lock id (Design Choice 3).
- The tenant tag on the four new instruments through the helper of #2676, in its own pull request.
- Shared pieces and owner: #724 owns the decorator, handle wrapper and the three instruments; this plan edits those two files (so it starts after #724's first pull request); #741 owns `DistributedLockDiagnostics` creation and `EventIdRanges.DistributedLock`; this plan owns the `DistributedLockObservability` range (Design Choice 6, 30450-30499) and the options class.
- Affected packages: `Encina.DistributedLock` and `Encina` (range registration). Not delivered: a lock-provider health check (exists: `RedisDistributedLockHealthCheck`, `SqlServerDistributedLockHealthCheck`; the InMemory one is #755), a module dimension (an issue file accompanies this plan), a hit-rate style derived gauge.

---

## Design Choices

<details>
<summary><strong>1. Boundary with #724 (what this plan adds)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add only what #724 lacks: extensions, releases, hold duration, waiting; map the issue's other names to #724's outcome-tagged instruments in the documentation** | No duplicate instrument, no double writes; the outcome tag answers contention and timeout rates | The issue's names (`LockTimeouts`, `LockWaitDuration`) do not exist as instruments |
| **B) Also emit the issue's separate counters and a wait histogram as aliases** | Matches the issue text literally | Each acquire records two or three instruments for the same event; a wait histogram that equals the acquisition histogram |

### Chosen Option: **A - Only the gap** (recommended, pending the maintainer)

### Rationale

- A wins on cost and on one source of truth; B wins if #724 is dropped, or if the maintainer wants the exact catalog of the issue for dashboards that predate ADR-054. #724's own Design Choice 4 reserves exactly these items for #222.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Options surface</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DistributedLockObservabilityOptions` with only `SlowAcquisitionThreshold`; metrics and traces stay switched by registering the decorator and by subscribing the meter and source** | Pay-for-what-you-use already holds (#724 registers nothing unless `AddEncinaDistributedLockTelemetry` is called, and an unsubscribed meter costs nothing, ADR-054 settled point 7); no duplicate switches | The issue's `EnableMetrics` and `EnableTracing` do not exist |
| **B) The issue's class: `EnableMetrics`, `EnableTracing`, `IncludeResourceInMetrics`, `ResourceMetricAggregator`, `SlowAcquisitionThreshold`** | Matches the issue | Two ways to switch the same thing off; the last two properties conflict with #724's no-resource rule |

### Chosen Option: **A - One property** (recommended, pending the maintainer)

### Rationale

- A wins now; B wins if operators must switch metrics off while keeping spans without touching the OpenTelemetry pipeline configuration. The class is `public sealed`, in `Encina.DistributedLock.Diagnostics`, validated by an `IValidateOptions` registered by the overload (`ValidateOnStart`); registering the overload twice keeps the first registration (`TryAdd`).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Resource names, aggregation and what the logs say</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No resource, no lock id and no tenant text in any tag, span attribute or log of the new code; no aggregator** | Complies with ADR-054 D6.3 whatever the application puts in a lock name (the XML example of `IDistributedLockProvider` is `saga:payment:{orderId}`, which carries data); consistent with #724 | The issue's per-resource view is lost; the slow-acquisition log cannot name the lock |
| **B) A caller-supplied `Func<string, string>? ResourceMetricAggregator` that maps a name to a low-cardinality label (default null: nothing emitted)** | Operators can group by lock family safely | Public API; one wrong aggregator (returning the raw name) reintroduces the leak; every tag and log must be gated by it |
| **C) Include the resource in logs only** | Debuggable slow acquisitions | Logs are shipped and retained like telemetry; lock names are application data |

### Chosen Option: **A - No resource information** (recommended, pending the maintainer)

### Rationale

- A wins because the interface has no label and names are free text; B wins for a deployment where every lock name is `family:id` and the operator wants per-family contention (it needs #724's Design Choice 3 to choose option B or C there too, otherwise the two plans disagree); C wins for an environment where logs are access-controlled differently from telemetry. This choice is tied to #724's Design Choice 3: if the maintainer picks another option there, this plan adopts the same one.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What hold duration and release outcome mean</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Hold duration is the lifetime of the handle (creation to the first dispose), released as `released`, or `expired` when the dispose time is after `ExpiresAtUtc`; a double dispose records once** | Detects handles held past their expiry (the lock was probably lost) with one tag; no provider call | A handle abandoned without dispose is never recorded (documented; `held` from #724 shows it as a stuck gauge) |
| **B) Cap the hold duration at the expiry** | The histogram never shows a duration longer than the lease | Hides the very leak the metric should show |

### Chosen Option: **A - Handle lifetime with a release outcome** (recommended, pending the maintainer)

### Rationale

- A wins for operators who alert on `expired`; B wins if the histogram is used only for capacity planning of lease lengths. Time comes from the injected `TimeProvider` of the #724 decorator, and `ExpiresAtUtc` of an extended handle is read at dispose time (the handle forwards `ExtendAsync`, so an extension moves the expiry).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Slow-acquisition signal</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A Warning log and the span tag `encina.lock.slow` = `true` when an acquire call reaches the threshold, for every outcome; no metric tag** | Visible in traces and logs; no new cardinality on the metric | A bool tag only on spans; the log has no resource name (Design Choice 3) |
| **B) A counter `encina.distributed_lock.slow_acquisitions`** | Alertable without logs | A fifth instrument next to the acquisition histogram, whose upper buckets already show it |

### Chosen Option: **A - Log and span tag** (recommended, pending the maintainer)

### Rationale

- A wins now; B wins if alerting must not depend on log shipping (the histogram's high buckets also serve that). The default threshold is the issue's 5 seconds.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. EventId block</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The block the maintainer assigned: `DistributedLockObservability` 30000-30049** | As assigned; same area base as the lock providers (Caching and distributed locks, 30000) | `docs/engineering/eventid-block-registry.md` holds 30000-30349 for #4007 (`Caching` CA-1 and the lock providers CA-2); two `reserved` rows would overlap, and `EventIdRegistryAgreementTests` rule 4 (reserved blocks do not overlap each other) fails |
| **B) The first free 50-slot after the #4007 extent and after the slots merged or open plans claim: `DistributedLockObservability` 30450-30499** | Passes the registry test today; the registry text says new blocks go after the #4007 target extents | Differs from the assignment. Not 30350-30399 (reserved by the merged plan #276, `CachingDiagnostics`) and not 30400-30449 (claimed by #273 `CachingObservability` and by #278 `CachingRecovery`) |

### Chosen Option: **B - 30450-30499** (no row in the registry on main at `ff9a579c1`, and no merged or open plan claims it)

Re-slotted from 30350-30399 to 30450-30499 on 2026-11-24 after the #4174 reservations and plan #276.

### Rationale

- Only B satisfies the registry: after #4174 the page reserves Caching 30000-30349 for #4007 and the coordinator confirmed that no new block may use it, so A cannot merge green. 30350-30399 is reserved by the merged plan #276 and 30400-30449 is claimed by #273 and #278; 30450-30499 has no row on main at `ff9a579c1` and appears in no plan on main or in an open plan pull request. The pull request adds one `reserved` row (`| 30450-30499 | Caching | #222 DistributedLockObservability (ids 30450-30451) | reserved |`, status `registered` in the code pull request), one field `DistributedLockObservability` in `src/Encina/Diagnostics/EventIdRanges.cs`, **one line in `src/Encina/PublicAPI.Unshipped.txt`** (counted in the size below), the `AssemblyRanges` entry `["Encina.DistributedLock"] = [nameof(EventIdRanges.DistributedLock), nameof(EventIdRanges.DistributedLockObservability)]` (the first element is #741's; whichever pull request lands second merges the two names into the existing entry) and its row in the "Registered ranges" table. Two ids are used (30450 slow acquisition, 30451 handle disposed after expiry), packed from the start of the block.

**Class:** Settled for "not 30000-30349" (a chosen slot inside the #4007 extent fails the registry test); the choice among the free 50-slots after it is Trivial (30450-30499 is the first one no plan claims)
**Settled by:** `docs/engineering/eventid-block-registry.md` rule 4 (reserved blocks do not overlap) and the #4007 extent Caching 30000-30349 (#4174)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code: error codes of the ADR-052 contract pass through as `error.type`, and the options validation throws `OptionsValidationException` at start-up; no ADR-062 category | Settled | ADR-062; `AGENTS.md` section 3 |
| C2 | The decorator returns the inner result unchanged and records after the fact; a failure inside the instrumentation never changes a result; nothing here denies a lock (no gate) | Settled | `AGENTS.md` section 3; #724 register row C4 |
| C3 | Zero cost without a listener: `HasListeners()` for spans, `Instrument.Enabled` for metrics, injected `TimeProvider` (the optional trailing parameter of #724 stays; the options and logger parameters trail it); logging through the `[LoggerMessage]` generator with `ex.ForLogging()` for exceptions | Settled | ADR-054 settled point 7; `AGENTS.md` sections 3 and 7 |
| C4 | `waiting` counts acquire calls currently inside `TryAcquireAsync` or `AcquireAsync` (incremented at the start, decremented in a `finally`); a call that does not wait still counts for the duration of its call | Settled | the issue's `WaitingRequests` up-down counter |
| C5 | Tenant tag on the four new instruments only when multi-tenancy is on, through the helper of #2676, absent otherwise; no tenant text in a lock name | Settled | ADR-054 D4; #2676 |
| C6 | Instrument and tag names: lowercase dotted, seconds with unit `s`, no `_total`; the tag keys reuse #724's `encina.lock.system`, `encina.lock.outcome`; a new key `encina.lock.release.outcome` | Settled | ADR-054 settled point 4 |
| C7 | The lock-provider health check is not part of this feature: it exists for Redis and SQL Server and #755 plans the InMemory one | Settled | `src/Encina.DistributedLock.Redis/`, `src/Encina.DistributedLock.SqlServer/`; #755 |

---

## Implementation Phases

> **Not startable until #724's first pull request (L1) has merged**: every phase edits `InstrumentedDistributedLockProvider`, `InstrumentedLockHandle` or `DistributedLockDiagnostics`, which that pull request creates, and L1 itself waits for #2667. The tasks name the ADR-052 `Either` shapes.

### Phase 1: Options, Range and Log Events (pull request D1)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs` (field `DistributedLockObservability`, Design Choice 6), `src/Encina/PublicAPI.Unshipped.txt` (one line) and `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the `AssemblyRanges` entry); `docs/engineering/eventid-block-registry.md` (the `reserved` row, the "Registered ranges" row) through `docs-writer`.
2. **Create** `src/Encina.DistributedLock/Diagnostics/DistributedLockObservabilityOptions.cs` (`public sealed`, `TimeSpan SlowAcquisitionThreshold { get; set; } = TimeSpan.FromSeconds(5)`) and an internal `IValidateOptions<DistributedLockObservabilityOptions>` in the same file (greater than zero, else a failure message that names the property, never a value).
3. **Create** `src/Encina.DistributedLock/Diagnostics/DistributedLockLog.cs` (`internal static partial`, `[LoggerMessage]`: 30450 `SlowAcquisition` (Warning: operation, outcome, system, elapsed ms, threshold ms), 30451 `HandleDisposedAfterExpiry` (Warning: system, overrun ms); XML docs name the range).
4. **Modify** `Diagnostics/InstrumentedDistributedLockProvider.cs` (new optional trailing constructor parameters `IOptions<DistributedLockObservabilityOptions>? options = null` and `ILogger<InstrumentedDistributedLockProvider>? logger = null` after `TimeProvider? timeProvider = null`: every `new InstrumentedDistributedLockProvider(inner)` and `(inner, timeProvider)` site created by #724 keeps compiling; the span tag `encina.lock.slow`; the log call) and `ServiceCollectionExtensions.cs` (overload `AddEncinaDistributedLockTelemetry(this IServiceCollection, Action<DistributedLockObservabilityOptions>)` with `ValidateOnStart`); `PublicAPI.Unshipped.txt` of `Encina.DistributedLock` through `mechanical-fixer`.
5. Tests: new `tests/Encina.UnitTests/DistributedLock/Diagnostics/DistributedLockObservabilityOptionsTests.cs` (default 5 seconds, zero and negative rejected at provider build with `ValidateOnBuild` and `ValidateScopes`); edit `InstrumentedDistributedLockProviderTests.cs` (a `FakeTimeProvider` advanced by a slow inner provider logs once and tags the span, below the threshold nothing, the log text never contains the resource); edit `tests/Encina.GuardTests/Infrastructure/DistributedLock/InstrumentedDistributedLockProviderGuardTests.cs` (null `options` and null `logger` are valid: valid-result tests; null `configure` throws); manifest `Encina.DistributedLock.json` and changelog `changelog.d/222-lock-slow-acquisition.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #222, plan docs/plans/distributed-lock-metrics-implementation-plan-222.md, phase 1.
#724 L1 merged: InstrumentedDistributedLockProvider, InstrumentedLockHandle and DistributedLockDiagnostics exist in
src/Encina.DistributedLock/Diagnostics/ on the ADR-052 Either contract.

TASK:
1. Register the DistributedLockObservability EventId range (block as the plan states), PublicAPI line, AssemblyRanges entry.
2. Add DistributedLockObservabilityOptions (SlowAcquisitionThreshold, validated) and DistributedLockLog (30450, 30451).
3. Add the options and logger as optional trailing constructor parameters and the AddEncinaDistributedLockTelemetry overload.
4. Tests (unit, guard, DI with ValidateOnBuild and ValidateScopes); manifests, PublicAPI, changelog through mechanical-fixer; registry through docs-writer.

KEY RULES:
- Never log or tag the resource name, lock id, tenant text or an error message; exceptions through ex.ForLogging().
- Optional parameters last (CS1737); a null optional parameter is valid and gets a valid-result test.
- EventIds packed from the block start; the range is registered first (ADR-021).

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- the plan of #724 and docs/engineering/eventid-block-registry.md
```

</details>

---

### Phase 2: Extensions, Releases, Hold Duration and Waiting (pull request D2)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `Diagnostics/DistributedLockDiagnostics.cs`: the four instruments of the Summary (names fixed in a constants block), `RecordExtension`, `RecordRelease`, `WaitingScope` helpers; each checks `Instrument.Enabled` first.
2. **Modify** `Diagnostics/InstrumentedDistributedLockProvider.cs`: `waiting` incremented around `TryAcquireAsync` and `AcquireAsync`; `ExtendAsync` records `extended`, `lost` (`Right(false)`) or `failed` (`Left`, `error.type` = the code).
3. **Modify** `Diagnostics/InstrumentedLockHandle.cs`: the handle records its creation time (`TimeProvider`), its `ExtendAsync` records like the provider's, and the first dispose records `releases` and `hold.duration` (outcome `expired` when the time is after `ExpiresAtUtc`) and, for `expired`, calls `DistributedLockLog.HandleDisposedAfterExpiry`; a second dispose records nothing.
4. Tests: edit `InstrumentedDistributedLockProviderTests.cs` and `InstrumentedLockHandleTests.cs` (a `MeterListener` over the real `InMemoryDistributedLockProvider` with `FakeTimeProvider`: extension of a held lock, extension of an absent lock gives `lost`, a faulted inner gives `failed`; release before and after expiry; hold duration equals the advanced time; double dispose records once; waiting returns to zero after success, contention and exception; no resource text in any tag); new `tests/Encina.UnitTests/DistributedLock/Diagnostics/DistributedLockMetricsCatalogTests.cs` (instrument names, units and tag keys in one table); manifest `Encina.DistributedLock.json`, changelog `changelog.d/222-lock-metrics.added.md` through `mechanical-fixer`; the README metric table (including the mapping from the issue's names to #724's outcome tag) through `docs-writer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #222 plan docs/plans/distributed-lock-metrics-implementation-plan-222.md, phase 2. D1 merged.

TASK:
1. Add extensions, releases, hold.duration and waiting to DistributedLockDiagnostics.
2. Record them from InstrumentedDistributedLockProvider and InstrumentedLockHandle.
3. Unit tests with a real InMemoryDistributedLockProvider, FakeTimeProvider and MeterListener.

KEY RULES:
- Hold duration is the handle lifetime; outcome expired when disposed after ExpiresAtUtc; double dispose records once.
- Return the inner result unchanged; never tag the resource, lock id, tenant text or an error message.
- No Thread.Sleep; time from FakeTimeProvider.

REFERENCE FILES:
- src/Encina.DistributedLock/Diagnostics/ (created by #724), src/Encina.DistributedLock/ILockHandle.cs
```

</details>

---

### Phase 3: Tenant Tag (pull request D3, after #2676)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `Diagnostics/DistributedLockDiagnostics.cs` and `Diagnostics/InstrumentedLockHandle.cs`: the tenant tag from the helper of #2676 on the four new instruments and their spans, only when multi-tenancy is on and a tenant resolves; the handle captures the tenant at creation (the dispose may run outside the request scope).
2. Tests: edit `InstrumentedLockHandleTests.cs` and `DistributedLockMetricsCatalogTests.cs` (tenant on, off, blank, dispose after the scope ended); changelog `changelog.d/222-lock-metrics-tenant.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #222 plan docs/plans/distributed-lock-metrics-implementation-plan-222.md, phase 3. D2 and #2676 merged.

TASK:
1. Add the tenant tag through the shared helper to the four new instruments; capture it when the handle is created.
2. Tests for tenant on, off, blank and dispose after the request scope.

KEY RULES:
- Only when multi-tenancy is enabled; never baggage; never the tenant in a lock name.

REFERENCE FILES:
- the helper of #2676, src/Encina.DistributedLock/Diagnostics/
```

</details>

---

### Phase 4: Proof on Real Providers (pull request D4)

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `tests/Encina.ContractTests/Infrastructure/DistributedLock/LockTelemetryContractTests.cs` (created by #724 L3): InMemory, Redis (fake database) and SQL Server (fake connection) behind the decorator emit the same four instrument names, tag keys and outcomes for extend, release, hold and waiting.
2. **Modify** `tests/Encina.IntegrationTests/Infrastructure/DistributedLock/LockTelemetryIntegrationTests.cs` (created by #724 L3) with the existing Redis and SQL Server collection fixtures: a real held lock records one release and one hold duration, a real second caller raises `waiting` while it waits, and a handle disposed after a short lease records `expired`.
3. Manifest through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #222 plan docs/plans/distributed-lock-metrics-implementation-plan-222.md, phase 4. D2 merged; #724 L3 merged.

TASK:
1. Extend the contract test over InMemory, Redis and SqlServer providers with the four new instruments.
2. Extend the integration test on real Redis and SQL Server.

KEY RULES:
- Shared collection fixtures only; real providers; no Thread.Sleep (FakeTimeProvider where the provider allows it).

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/RedisDistributedLockIntegrationTests.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-054 | Settled points 2, 4, 7; D3, D4, D6.3 | Names, error text, zero cost, tenant tag, never data-bearing names |
| ADR-052 | Failure contract: contention is `Right(None)`, never an error | Outcome mapping of the new instruments |
| ADR-021 and its 10000+ zone amendment | Register the range first, new blocks in the zone | Design Choice 6 |
| `AGENTS.md` section 5 (locks) | TryAcquire with timeout, auto-release, `CancellationToken`; the 7 lock providers of 1.0 | All providers by interface |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IDistributedLockProvider` | `src/Encina.DistributedLock/IDistributedLockProvider.cs` | The interface the decorator wraps (`TryAcquireAsync`, `AcquireAsync`, `IsLockedAsync`, `ExtendAsync`) |
| `ILockHandle` | `src/Encina.DistributedLock/ILockHandle.cs` | Handle contract; hold duration and release are recorded when it is disposed |
| `DistributedLockOptions` | `src/Encina.DistributedLock/DistributedLockOptions.cs` | Existing options class next to which the observability options live |
| `InstrumentedDistributedLockProvider`, `InstrumentedLockHandle` | #724, `src/Encina.DistributedLock/Diagnostics/` (not on main yet) | The decorator and handle this plan extends |
| `DistributedLockDiagnostics` | #741 and #724, `src/Encina.DistributedLock/Diagnostics/` (not on main yet) | Shared holder of the source and meter named `Encina.DistributedLock` |
| `InMemoryDistributedLockProvider`, `RedisDistributedLockProvider`, `SqlServerDistributedLockProvider` | `src/Encina.DistributedLock.InMemory/`, `.Redis/`, `.SqlServer/` | Real providers used by unit, contract and integration tests |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registry where the new range is registered first |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DistributedLock` (`DistributedLockObservability`, new) | 30450-30499, free per the registry and the plans on main at `ff9a579c1` | 30450 slow acquisition, 30451 handle disposed after expiry; 30452-30499 free; Design Choice 6 explains why 30000-30049 is not used |
| `Encina.DistributedLock` (`DistributedLock`, #741) | 6250-6299 | Not touched here |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| D1 | Options, range, log | 2 (`DistributedLockObservabilityOptions.cs`, `DistributedLockLog.cs`) | 5 (`EventIdRanges.cs`, `Encina` PublicAPI, `InstrumentedDistributedLockProvider.cs`, `ServiceCollectionExtensions.cs`, `Encina.DistributedLock` PublicAPI) | 1 | 3 (allocation test, provider tests, guard tests) | 1 | 2 (registry page, changelog) | 14 | ~700 |
| D2 | Four instruments | 0 | 3 (`DistributedLockDiagnostics.cs`, provider, handle) | 1 | 2 | 1 | 2 (README, changelog) | 9 | ~650 |
| D3 | Tenant tag (after #2676) | 0 | 2 | 0 | 2 | 0 | 1 (changelog) | 5 | ~140 |
| D4 | Real providers | 0 | 0 | 0 | 2 | 1 | 0 | 3 | ~260 |
| **Total** | | **2** | **10** | **2** | **9** | **3** | **5** | **31** | **~1,750** |

Columns sum: 2 + 10 + 2 + 9 + 3 + 5 = 31 = 14 + 9 + 5 + 3. The `Encina` PublicAPI line for the new `EventIdRanges` field is counted in D1. `InternalsVisibleTo` for `Encina.GuardTests` on `Encina.DistributedLock` is added by #724 L1 (the options class and the overload are public, so no further csproj edit here). Test doubles: no interface changes; the optional trailing parameters keep every `new InstrumentedDistributedLockProvider(` site of #724 compiling (grep on main finds none today: the type does not exist yet).

Per-file coverage targets (existing manifest `Encina.DistributedLock.json`: package guard 20, unit 60; the #724 plan adds per-file entries that this plan only raises):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Diagnostics/DistributedLockObservabilityOptions.cs` | 100 | 40 | 0 | 0 | 0 | Default, setter and the validator branches (zero, negative, valid) run in the unit and DI tests; guard reaches the validator with null; contract 0 because the class holds no provider behavior |
| `Diagnostics/DistributedLockLog.cs` | 0 | 0 | 0 | 0 | 0 | Generator-emitted `[LoggerMessage]` partials are not measured as hand-written lines and have no validated parameter; the events are asserted through a fake logger in the provider and handle tests |
| `Diagnostics/InstrumentedDistributedLockProvider.cs` (#724: 90, 70, 25, 0, 40) | 92 | 70 | 25 | 0 | 40 | A rise on unit only: the extension, waiting and slow paths are driven with real providers; the other flags keep #724's values |
| `Diagnostics/InstrumentedLockHandle.cs` (#724: 95, 40) | 97 | 40 | 25 | 0 | 40 | Release before and after expiry, hold duration, double dispose and extension outcomes are table-driven; contract and integration add Redis and SQL Server |
| `Diagnostics/DistributedLockDiagnostics.cs` (#724: 80) | 90 | 0 | 0 | 0 | 0 | Four more instruments and their record helpers are reached by the catalog test; internal static, no validated parameter |
| `ServiceCollectionExtensions.cs` (#724: 90, 40) | 90 | 40 | 0 | 0 | 0 | Targets stay; the overload and its two failure paths (null `services`, null `configure`) are driven by the DI and guard tests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #222 - the distributed lock observability gap that #724 does not cover - in four small pull requests.
Nothing here is startable until #724's first pull request has merged (and with it #2667); D3 also waits for #2676.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0. Edit with Edit/Write only; PublicAPI, manifests and changelog through mechanical-fixer; README and
  the registry page through docs-writer.
- ADR-054 names and privacy rules; ADR-052 Either contract; the shared DistributedLockDiagnostics of #724 and #741.

IMPLEMENTATION OVERVIEW:
1. D1: DistributedLockObservability EventId range, options class (SlowAcquisitionThreshold), two log events, options overload.
2. D2: extensions, releases, hold.duration and waiting instruments recorded by the decorator and the handle wrapper.
3. D3 (after #2676): tenant tag. D4: contract and integration proof on InMemory, Redis and SqlServer.

KEY PATTERNS:
- Never emit the resource name, lock id, tenant text or an error message in a tag, span attribute or log.
- Do not add the issue's separate counters (acquisitions, timeouts, wait duration, active locks): #724's outcome-tagged instruments are them.
- Return the inner result unchanged; zero cost without a listener; optional parameters last.

REFERENCE FILES:
- src/Encina.DistributedLock/, the plans of #724, #741 and #717, docs/engineering/eventid-block-registry.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Lock telemetry is real-time and not cacheable |
| 2 | OpenTelemetry | ✅ Phases 1-4 | This is the feature (the gap over #724) |
| 3 | Structured Logging | ✅ Phase 1 | `DistributedLockLog`, ids 30450-30451, no resource name |
| 4 | Health Checks | ❌ N/A | Health checks of the lock providers exist (Redis, SQL Server) and #755 plans the InMemory one; this feature observes operations |
| 5 | Validation | ✅ Phase 1 | The options are validated at start-up (`SlowAcquisitionThreshold` greater than zero) |
| 6 | Resilience | ❌ N/A | Passive layer: adds no external call and never retries |
| 7 | Distributed Locks | ✅ Phases 2, 4 | The plan instruments the lock providers themselves |
| 8 | Transactions | ❌ N/A | Never commits or rolls back |
| 9 | Idempotency | ❌ N/A | Recording twice is harmless; a double dispose is recorded once |
| 10 | Multi-Tenancy | ✅ Phase 3 | Tenant tag only when multi-tenancy is on, through the helper of #2676; resource names are never emitted |
| 11 | Module Isolation | ⏭️ Deferred | Module dimension on lock and cache telemetry: follow-up issue file `lock-cache-telemetry-module-dimension.md` (to be opened by the orchestrator) |
| 12 | Audit Trail | ❌ N/A | Infrastructure telemetry; no personal data may reach it |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #724 first pull request (decorator, handle wrapper, shared diagnostics) | Open (plan on main) | Every phase edits its files |
| #2667 (ADR-052 contract) | Open | #724 L1 waits for it; the tasks use the `Either` shapes |
| #2676 tenant helper | Open | Only D3 waits |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Tenant-scoped locks, `EventIdRanges.DistributedLock` | #741 | Owns the 6250-6299 range and the first `AssemblyRanges` entry; D1 merges its entry with this plan's range name |
| Leader election instruments | #717 | Adds its own instruments to the shared holder |
| InMemory lock health check | #755 | Health status is separate from this telemetry |
| Planned lock providers | #207, #208, #209, #210 | Covered by the decorator when they land; each adds its row to the provider-label table of #724 |
| Lock documentation | #908, #906 | The README table here feeds those pages |

---

## Next Steps

1. Link this plan from #222 and ask the maintainer to answer Design Choices 1 to 5 when work starts (6 is settled).
2. Implement D1 after #724 L1, then D2, then D4; D3 after #2676.
3. Maintainer action: amend the acceptance criteria of #222 that name `DistributedLockMetrics`, `DistributedLockActivitySource`, `IncludeResourceInMetrics`, `ResourceMetricAggregator`, `EnableMetrics` and `EnableTracing` to the outcome of Design Choices 1 to 3 (this plan never comments on an issue).
