# Implementation Plan: OpenTelemetry for the ADO.NET Store Providers (Store Metrics, Provider Identity, Verification on the Three Providers)

> **Issue**: [#721](https://github.com/dlrivada/Encina/issues/721)
> **Type**: Feature (observability)
> **Related**: ADR-054 (D3 names, D4 tenant tag, D6 database stores at Encina operation level and never `db.query.text`), ADR-062, #722 (Dapper, same mechanism, uses this plan's shared pieces), #2676 (shared tenant tag helper), #1787 (every source and meter subscribed), #1790 (dead and duplicate sources), #1788 (error text in spans), #906 (observability guide, owns the cardinality note), #177 (messaging attribute constants)
> **Complexity**: Medium (the decorators already exist; the work is provider identity, store metrics and proof on real databases)
> **Estimated Scope**: ~330 lines of production code + ~900 lines of tests, in five pull requests of at most 15 files each

---

## Summary

### What exists today (read on 2026-11-20, main `0336d31c7`)

- No ADO.NET package emits telemetry: a search of `src/Encina.ADO.SqlServer`, `src/Encina.ADO.PostgreSQL` and `src/Encina.ADO.MySQL` for `new ActivitySource(` and `new Meter(` finds nothing. `Encina.ADO.Sqlite` does not exist in `src` (SQLite is outside the provider matrix, ADR-024), so the issue's "4 providers" are 3.
- The tracing the issue asks for already exists, provider-agnostic. `AddEncinaOpenTelemetry` (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`) calls `DecorateService` eight times: `IUnitOfWork`, `IOutboxStore`, `IInboxStore`, `ISagaStore`, `IScheduledMessageStore`, `IDeadLetterStore`, `IOperationAuditStore` and `ICacheProvider`. Each `Instrumented*Store` (internal sealed, folder `MessagingStores/`) owns an `ActivitySource` (`Encina.Messaging.Outbox`, `.Inbox`, `.Saga`, `.Scheduling`, `.DeadLetter`), guards every span with `HasListeners()`, marks an error with the `EncinaError` code only (`ActivityStatusDescriptions.ForError`) and passes the count queries through. The ADO, Dapper, EF Core and MongoDB stores all implement those interfaces, so an ADO store is traced when the decorator is applied.
- What is missing for the issue: no span or metric says which database or access technology served the operation (`db.system` appears only in `Encina.Messaging/Diagnostics/TransactionActivitySource.cs` and `OutboxProcessorMetrics.cs`, classified from a connection by the internal `DbSystemName`), and no instrument measures the duration, the count or the failures of a store operation. The only store instruments are `MessagingStoreMetrics` (`Encina.Messaging`) and `UnitOfWorkMetrics` (`Encina.DomainModeling`), which carry `_total` and `_ms` names that ADR-054 settled point 4 forbids and that the #1791 rename follow-up removes.
- Provider identity is already in the container: `IDatabaseHealthMonitor.ProviderName` (`src/Encina/Database/IDatabaseHealthMonitor.cs`, registered by every provider with `TryAddSingleton`) is `ado-sqlserver`, `ado-postgresql`, `ado-mysql`, `dapper-sqlserver`, `dapper-postgresql`, `dapper-mysql`, `efcore` or `mongodb` (the `base("...")` arguments of the eight monitors; `DatabaseHealthMonitorBase` is the base of seven).
- The decorators are applied only when the inner service is already registered when `AddEncinaOpenTelemetry` runs (`DecorateService` returns without a trace otherwise): called first, it silently adds no spans.
- `InstrumentedFunctionalRepository<,>` and `InstrumentedBulkOperations<>` are constructed only by tests; no production code registers them, so repository and bulk-operation spans never occur (Design Choice 5).
- Tests today: `tests/Encina.UnitTests/OpenTelemetry/MessagingStores/Instrumented{Outbox,Inbox,Saga,ScheduledMessage,DeadLetter}StoreTests.cs`, `UnitOfWork/InstrumentedUnitOfWorkTests.cs`, the matching guard tests in `tests/Encina.GuardTests/Infrastructure/OpenTelemetry/` and `InstrumentedWrappersIntegrationTests.cs` (mocks only: no real database).

### What this plan delivers

- Provider identity on every store span and metric: `db.system.name` and `encina.db.access` (`ado`, `dapper`, `efcore`, `mongodb`), taken from `IDatabaseHealthMonitor.ProviderName` (Design Choice 2), through one internal `DatabaseIdentity`.
- Two instruments for the five message stores, with no `_total` suffix and seconds as unit: `encina.store.operations` (counter) and `encina.store.operation.duration` (histogram), tagged with the store type, the operation, the provider identity, the outcome and the error code (Design Choice 3).
- The tenant tag through the shared helper of #2676, once it exists (ADR-054 D4): this plan writes no tenant tag code of its own.
- Proof on the three real ADO providers (integration tests) that a store call emits the span and the instruments with the right provider tags, and a provider-coherence contract test.
- Not delivered, each in a Design Choice: ActivitySource or Meter inside the six ADO and Dapper packages (1), `db.query.text` (never emitted, ADR-054 D6.3), repository and bulk-operation wiring (5).
- Shared pieces and owner: this plan owns `StoreTelemetry` and `DatabaseIdentity` (folder `Telemetry/` of `Encina.OpenTelemetry`); #722 uses them and adds Dapper proof only; the tenant helper is #2676's; the cache, lock and transport instruments belong to #723, #724 and #725 and share only the naming rules.
- Affected packages: `Encina.OpenTelemetry` only. No ADO, Dapper or EF Core package changes; the three ADO packages are touched by tests.
- Provider category: Database (10): the decorators cover ADO.NET (3), Dapper (3), EF Core (3) and MongoDB (1) by interface; this plan proves the 3 ADO providers and #722 proves the 3 Dapper providers; EF Core and MongoDB get the same tags automatically and are proved by the unit test of `DatabaseIdentity` over their provider names.

---

## Design Choices

<details>
<summary><strong>1. Where the store instrumentation lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep and extend the provider-agnostic decorators of `Encina.OpenTelemetry`** | One implementation for the 10 database providers; MongoDB and the 6 EF Core and Dapper stores get it for free; matches ADR-054 D6.1 (Encina operation level, every provider) | The packages must call `AddEncinaOpenTelemetry` after the provider registration (existing behavior); a provider-specific operation that no interface exposes is invisible |
| **B) An `ActivitySource` and `Meter` inside each of the six ADO and Dapper packages** (the issue's text) | Works without `Encina.OpenTelemetry` | 6 copies of the same code and tests, and the three EF Core providers and MongoDB would need a seventh and eighth; the interfaces already give one seam; ADR-054 D3 wants one source per package, which would add 6 more sources |
| **C) Driver-level instrumentation (SQL text, connection spans)** | Richest data | Ruled out by ADR-054 D6.2 and D6.3: no duplicated driver instrumentation and never `db.query.text` |

### Chosen Option: **A - Extend the decorators** (recommended, pending the maintainer)

### Rationale

- A wins on `main` today because the decorators exist for five of the issue's stores and apply to all 10 providers; B wins only for an application that wants store telemetry without the `Encina.OpenTelemetry` package, a case ADR-054 settled point 1 does not require (the library depends on the API, the application chooses the SDK).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the provider identity comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Read the optional `IDatabaseHealthMonitor.ProviderName` and map it in one internal `DatabaseIdentity`** | No interface change; every provider already registers a monitor; one unit test pins all eight names | A string convention (`<access>-<system>`); a provider that renames its monitor changes its telemetry, so the test is the guard |
| **B) Add `DatabaseSystem` and `AccessTechnology` members to `IDatabaseHealthMonitor`** | Typed | An interface change: 7 implementers through `DatabaseHealthMonitorBase`, `MongoDbDatabaseHealthMonitor` and every test double; mixes a telemetry concern into a health interface |
| **C) Classify a connection like `DbSystemName.From(IDbConnection)`** | Already used for transactions | The decorators never see a connection (the stores open their own), and the class is `internal` to `Encina.Messaging` |

### Chosen Option: **A - Map `ProviderName`** (recommended, pending the maintainer)

### Rationale

- A wins now: the monitor is a singleton registered by every database provider, and the decorator wraps scoped stores, so the factory resolves it once per scope. B wins if a fourth access technology or a provider such as SQLite returns and breaks the `<access>-<system>` convention.
- When no monitor is registered the identity is `unknown` and the tags are omitted, never guessed.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Meter name, instruments and how failures are counted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Meter("Encina.Messaging")` (the package of the instrumented abstractions, ADR-054 D3 and the family of #266 and #741); one counter `encina.store.operations` with tag `encina.outcome` (`success`, `error`) and `error.type` on errors, and one histogram `encina.store.operation.duration` in `s`** | One convention across the family; a single counter gives the error rate by division; names follow ADR-054 settled point 4 | The acceptance criterion says "an error counter": it is the same counter filtered on `encina.outcome=error` |
| **B) `Meter("Encina.OpenTelemetry")` (the emitting package)** | Matches "one meter per package" literally | The decorator package would own instruments about messaging stores; sibling plans of caches and locks use the abstraction's package |
| **C) A separate `encina.store.operation.errors` counter** | Matches the issue's wording | Two writes per failure on a hot path and a second name to document |

### Chosen Option: **A - Package meter and one counter plus one histogram** (recommended, pending the maintainer)

### Rationale

- A wins when the host subscribes to Encina by package (the D3 rule) and alerts on a ratio; C wins for a team whose backend cannot filter a counter by tag.
- Tags are bounded: `encina.store.type` (5 values), `encina.store.operation` (about 25 values across the five stores), `db.system.name` (4), `encina.db.access` (4), `encina.outcome` (2), `error.type` (the `EncinaError` code or exception type, never the message, ADR-054 settled point 2), plus the tenant tag of #2676 when multi-tenancy is on. The cardinality note of the tenant tag is #906's.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Attribute keys and the existing `db.system` sites</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Emit the latest database keys: `db.system.name` for the system and an Encina key `encina.store.operation` for the operation; leave the three existing `db.system` sites for a follow-up** | ADR-054 D3 pins the latest names; the Encina operation is not a database operation, so it does not borrow `db.operation.name` | Two spellings coexist until the follow-up (`db.system` in the transaction span, `db.system.name` here) |
| **B) Emit `db.system` and `db.operation` as the issue lists them** | Matches the issue text | The legacy name; `db.operation` would carry values such as `add` that no database ran |
| **C) Emit both spellings** | Tolerant of old dashboards | ADR-054 D3 rejected emitting both for messaging; pre-1.0 has no old dashboards |

### Chosen Option: **A - Latest keys, Encina operation key, follow-up for the old sites** (recommended, pending the maintainer)

### Rationale

- A wins because the keys sit behind one constants class (ADR-054 D3) and the OpenTelemetry database conventions name the system `db.system.name`; B wins if the maintainer prefers the issue's literal keys to the current convention. The values are the convention's: `microsoft.sql_server`, `postgresql`, `mysql` (and `mongodb` for the document store); `db.query.text` is never emitted (ADR-054 D6.3).
- The three existing `db.system` sites (`TransactionActivitySource`, `OutboxProcessorMetrics`, `DbSystemName`) are listed in the follow-up issue file `db-system-attribute-spelling` (Next Steps).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Repository and bulk-operation spans</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer to its own issue** | The wiring is a different mechanism (per-entity closed generics) with its own order problem; keeps each pull request inside 15 files | The issue's list names "repository"; its criterion "traces for store operations" is met for the five message stores only |
| **B) Wrap each closed `IFunctionalRepository<,>` and `IBulkOperations<>` descriptor inside `AddEncinaOpenTelemetry`** | One place, works for all providers | `AddEncinaRepository<TEntity,TId>()` runs per entity, usually after the infrastructure, so the wrapper silently misses most registrations unless the call comes last |
| **C) Register the wrapper inside each provider's `AddEncinaRepository`** | Order independent | 9 provider registrations (ADO, Dapper, EF Core x 3) and a dependency on the OpenTelemetry package from every provider |

### Chosen Option: **A - Defer** (recommended, pending the maintainer)

### Rationale

- A wins now: the two classes exist and are used only by tests (verified by grep), so the gap is one of wiring and deserves a design of its own; C wins if the maintainer wants repository telemetry without the order rule. The follow-up is the issue file `repository-bulk-instrumentation-wiring`.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Cost without a listener, time and the tenant tag</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the `HasListeners()` guard for spans; record metrics only when the instrument is enabled (`Counter.Enabled`, `Histogram.Enabled`); read the clock from an injected `TimeProvider` (optional, default `TimeProvider.System`); the tenant tag comes from #2676's helper** | Zero allocation and no clock read without a listener; deterministic tests; one tenant rule | One more optional constructor parameter per decorator |
| **B) Always measure** | Simpler | Pays a timestamp per store call with nothing listening |

### Chosen Option: **A - Guarded, injected time, shared tenant helper**

### Rationale

- ADR-054 settled point 7 requires zero cost without a listener and time from `TimeProvider`; `AGENTS.md` section 3 requires `TimeProvider` in production code; ADR-054 D4 and #2676 make the tenant tag one shared helper.

**Class:** Trivial
**Settled by:** ADR-054 settled point 7 and D4; `AGENTS.md` section 3 (time comes from `TimeProvider`)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code: the decorators return the inner `Either` unchanged, so there is no ADR-062 `ErrorCategory` to assign | Settled | ADR-062 (a category is chosen where an error is created) |
| C2 | No logging and no EventId: failures already surface as `Left` and as an error span; the registry block 6380-6399 assigned to this family is not used | Settled | `AGENTS.md` section 7 (a range is registered only with a `[LoggerMessage]`) |
| C3 | `error.type` carries the `EncinaError` code or the exception type, never `EncinaError.Message` | Settled | `AGENTS.md` section 3; ADR-054 settled point 2 |
| C4 | No tenant-qualified id is built and nothing is keyed by tenant, so the reserved `default` segment and injectivity do not apply; with multi-tenancy off no tenant tag is emitted | Settled | ADR-054 D4 |
| C5 | A decorator never turns a `Left` into success, never retries and never swallows an exception: it records and returns | Settled | `AGENTS.md` section 3 (errors are never swallowed) |
| C6 | Decoration order (the provider before `AddEncinaOpenTelemetry`) is unchanged and documented; the DI test pins the supported order and a follow-up issue file records the silent skip in the other order | Dilemma (recommended, pending the maintainer) | the alternatives (a deferred decoration at provider build, a startup validation) change the registration model of every decorator |

---

## Implementation Phases

### Phase 1: Shared Store Telemetry and Provider Identity (pull request S1, with Phase 2)

> **Goal**: one place that knows the meter, the instruments, the tag keys and the provider mapping.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.OpenTelemetry/Telemetry/DatabaseIdentity.cs` (`internal sealed record DatabaseIdentity(string? System, string Access)`): `static DatabaseIdentity From(string? providerName)` maps `ado-sqlserver`, `dapper-sqlserver` to `microsoft.sql_server`, `-postgresql` to `postgresql`, `-mysql` to `mysql`, `mongodb` to `mongodb`, `efcore` to no system (the monitor of EF Core does not know the database); the access is the prefix (`ado`, `dapper`, `efcore`, `mongodb`); `null`, empty or unknown yields `Unknown` (no tags). Constants for the tag keys `db.system.name`, `encina.db.access`.
2. **Create** `src/Encina.OpenTelemetry/Telemetry/StoreTelemetry.cs` (`internal static`): `Meter Meter = new("Encina.Messaging", "1.0")`, `Counter<long> Operations` (`encina.store.operations`, unit `{operation}`), `Histogram<double> Duration` (`encina.store.operation.duration`, unit `s`); a `readonly struct StoreMeasurement` created by `Begin(string storeType, string operation, DatabaseIdentity identity, TimeProvider time)` that reads `time.GetTimestamp()` only when an instrument is enabled, and `Complete(Either<EncinaError, T> result)` / `Failed(Exception)` that record outcome, `error.type` (`ActivityStatusDescriptions.ForError` or the exception type name) and the duration in seconds.
3. **Modify** `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`: add a `DecorateService<TService>(services, Func<IServiceProvider, TService, TService>)` overload (the existing factory keeps working: it passes the scope's `IServiceProvider` to the decorator factory, which resolves `DatabaseIdentity.From(sp.GetService<IDatabaseHealthMonitor>()?.ProviderName)` and `TimeProvider`), and `metrics.AddMeter("Encina.Messaging")` in `WithEncina` next to `AddMeter("Encina")`.
4. **Manifest and changelog** through `mechanical-fixer`: `.github/coverage-manifest/Encina.OpenTelemetry.json` (the two new files) and `changelog.d/721-store-telemetry.added.md`.
5. Tests: `tests/Encina.UnitTests/OpenTelemetry/Telemetry/DatabaseIdentityTests.cs` (new: the eight provider names, `null`, empty, unknown, case) and `StoreTelemetryTests.cs` (new: a `MeterListener` sees the counter and the histogram with the expected tags; no measurement and no clock read when no listener is attached; a `Left` records `error.type` with the code and never the message; a fake `TimeProvider` gives an exact duration).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #721, plan docs/plans/ado-opentelemetry-implementation-plan-721.md, phase 1.
Encina.OpenTelemetry already decorates IOutboxStore, IInboxStore, ISagaStore, IScheduledMessageStore, IDeadLetterStore and
IUnitOfWork (ServiceCollectionExtensions.DecorateService). ADR-054: lowercase dotted instrument names, seconds, no _total,
error code only, zero cost without a listener, time from TimeProvider.

TASK:
1. Create DatabaseIdentity (internal) mapping IDatabaseHealthMonitor.ProviderName to db.system.name and encina.db.access.
2. Create StoreTelemetry (internal static): Meter "Encina.Messaging" 1.0, counter encina.store.operations, histogram
   encina.store.operation.duration (unit s), a StoreMeasurement struct that does no work when no instrument is enabled.
3. Add a DecorateService overload that passes the IServiceProvider; subscribe the meter in WithEncina.
4. Unit tests that run the real classes with a MeterListener. Manifest and changelog through mechanical-fixer.

KEY RULES:
- Never tag EncinaError.Message or an exception message; no db.query.text; no tenant code (phase 4 uses the helper of #2676).
- Time through TimeProvider (optional parameter, default TimeProvider.System); optional parameters go last (CS1737).
- No [Obsolete], no compatibility layer.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs
- src/Encina/Database/IDatabaseHealthMonitor.cs, src/Encina.Messaging/Health/DatabaseHealthMonitorBase.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

### Phase 2: Outbox and Inbox Decorators (pull request S1)

> **Goal**: the first two stores record the instruments and carry the provider tags.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs` and `InstrumentedInboxStore.cs`: constructor `(IOutboxStore inner, DatabaseIdentity? identity = null, TimeProvider? timeProvider = null)` (`identity` defaults to `Unknown`); each instrumented method calls `StoreTelemetry.Begin(...)` before the inner call and `Complete(result)` after it, and the span gets `db.system.name` and `encina.db.access` when the identity is known; the pass-through count queries and `SaveChangesAsync` stay untouched.
2. **Modify** `ServiceCollectionExtensions.cs` registrations of the two stores to the new overload.
3. Tests: edit `InstrumentedOutboxStoreTests.cs` and `InstrumentedInboxStoreTests.cs` (assert counter and histogram per operation, error path, provider tags on the span); edit `ServiceCollectionExtensionsTests.cs` (a provider built with `ValidateOnBuild` and `ValidateScopes` resolves the decorated outbox store with an `IDatabaseHealthMonitor` of each of the three ADO names; with no monitor the store still works and emits no provider tag); edit `InstrumentedOutboxStoreGuardTests.cs` and `InstrumentedInboxStoreGuardTests.cs` (a `null` identity and a `null` time provider are valid and give a working store: valid-result tests, not exception tests).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #721 plan docs/plans/ado-opentelemetry-implementation-plan-721.md, phase 2. Phase 1 created DatabaseIdentity and StoreTelemetry.

TASK:
1. Give InstrumentedOutboxStore and InstrumentedInboxStore the optional trailing parameters identity and timeProvider, record
   the instruments around every instrumented method and add the provider tags to the span.
2. Register both through the DecorateService overload that passes the IServiceProvider.
3. Update the unit, guard and DI tests listed in the plan; a null optional parameter is valid and gets a valid-result test.

KEY RULES:
- Keep the HasListeners guard, the error status with the code only and the pass-through of the count queries.
- Optional parameters after required ones (CS1737); 30+ existing "new Instrumented...Store(inner)" test call sites must still compile.
- Tests use real decorators with real ActivityListener and MeterListener, not reflection.

REFERENCE FILES:
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs, InstrumentedInboxStore.cs
- tests/Encina.UnitTests/OpenTelemetry/MessagingStores/InstrumentedOutboxStoreTests.cs
- tests/Encina.GuardTests/Infrastructure/OpenTelemetry/InstrumentedOutboxStoreGuardTests.cs
```

</details>

---

### Phase 3: Saga, Scheduled, Dead Letter (pull request S2) and Unit of Work (pull request S2b)

> **Goal**: the remaining decorators follow the same pattern, and a contract test proves they emit the same instruments and keys. The unit of work is its own small pull request (S2b) so that S2 stays within 15 files.

<details>
<summary><strong>Tasks</strong></summary>

1. S2: **Modify** `InstrumentedSagaStore.cs`, `InstrumentedScheduledMessageStore.cs` and `InstrumentedDeadLetterStore.cs` (same constructor and calls as Phase 2) and their three registrations in `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` (that file is counted in S2).
2. S2 tests: edit the three matching unit tests, the three matching guard tests (`InstrumentedSagaStoreGuardTests.cs`, `InstrumentedScheduledMessageStoreGuardTests.cs`, `InstrumentedDeadLetterStoreGuardTests.cs`, all existing) and `ServiceCollectionExtensionsTests.cs` (DI test with `ValidateOnBuild` and `ValidateScopes` for the three stores); **create** `tests/Encina.ContractTests/Infrastructure/OpenTelemetry/StoreTelemetryContractTests.cs` (for each of the five message stores, the same operation produces the same instrument names, the same tag keys and `encina.store.type` value set; a provider-coherence check that the eight provider names map to the documented identities, including a cross-access case: the same operation on an ADO identity and a Dapper identity differs only in `encina.db.access` and the system value; #722 relies on this test and adds no coherence test of its own).
3. S2b: **Modify** `UnitOfWork/InstrumentedUnitOfWork.cs` (constructor `(IUnitOfWork inner, DatabaseIdentity? identity = null)` with a new `ArgumentNullException.ThrowIfNull(inner)`; only the provider tags on its spans: `UnitOfWorkMetrics` already measures it, so no new instrument and no double count) and its registration in `ServiceCollectionExtensions.cs`. Tests: edit `InstrumentedUnitOfWorkTests.cs` and `ServiceCollectionExtensionsTests.cs`; **create** `tests/Encina.GuardTests/Infrastructure/OpenTelemetry/InstrumentedUnitOfWorkGuardTests.cs` (it does not exist on main; the constructor gains an optional parameter, so a `null` identity is a valid-result test).
4. `.github/coverage-manifest/Encina.OpenTelemetry.json` through `mechanical-fixer`, in S2 and again in S2b.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #721 plan docs/plans/ado-opentelemetry-implementation-plan-721.md, phase 3. Phases 1 and 2 merged (StoreTelemetry,
DatabaseIdentity, outbox and inbox decorators done).

TASK:
1. Apply the phase 2 pattern to InstrumentedSagaStore, InstrumentedScheduledMessageStore and InstrumentedDeadLetterStore.
2. In InstrumentedUnitOfWork (separate pull request S2b) add only db.system.name and encina.db.access to the existing spans;
   no new instrument; create its guard test file (it does not exist yet).
3. Add the contract test and update the unit and guard tests.

KEY RULES:
- Do not touch IOperationAuditStore: it is not one of the five stores.
- A Left stays a Left; record and return; no logging.

REFERENCE FILES:
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs, InstrumentedScheduledMessageStore.cs, InstrumentedDeadLetterStore.cs
- src/Encina.OpenTelemetry/UnitOfWork/InstrumentedUnitOfWork.cs, src/Encina.DomainModeling/Diagnostics/UnitOfWorkMetrics.cs
```

</details>

---

### Phase 4: Tenant Tag (pull request S3, after #2676)

> **Goal**: ADR-054 D4 on the store instruments and spans, with the shared helper.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `StoreTelemetry.cs`: add the tenant tag to the `TagList` of `Complete` and to the span through the helper of #2676 (only when multi-tenancy is on and a tenant is resolved; otherwise nothing).
2. Tests: edit `StoreTelemetryTests.cs` (tenant on: the tag is present on counter, histogram and span; tenant off: absent; blank tenant: absent). Changelog fragment `changelog.d/721-store-telemetry-tenant.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #721 plan docs/plans/ado-opentelemetry-implementation-plan-721.md, phase 4. #2676 merged: it provides the shared tenant
tag helper in core diagnostics.

TASK:
1. Call the helper from StoreTelemetry for the instruments and the span; no other file changes.
2. Add the tenant-on, tenant-off and blank-tenant tests.

KEY RULES:
- Only when multi-tenancy is enabled; never baggage; the cardinality note belongs to the guide of #906.

REFERENCE FILES:
- the helper delivered by #2676, src/Encina.OpenTelemetry/Telemetry/StoreTelemetry.cs
```

</details>

---

### Phase 5: Proof on the Three ADO Providers (pull request S4)

> **Goal**: a real store on a real database emits the span and the instruments with the provider tags.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** one integration test per provider, using the shared collections and fixtures (`[Collection("ADO-SqlServer")]` with `SqlServerFixture`, `ADO-PostgreSQL` with `PostgreSqlFixture`, `ADO-MySQL` with `MySqlFixture`, `ClearAllDataAsync()` in `InitializeAsync()`, `[Trait("Category", "Integration")]`): `tests/Encina.IntegrationTests/ADO/SqlServer/Observability/StoreTelemetryIntegrationTests.cs`, `ADO/PostgreSQL/Observability/...` and `ADO/MySQL/Observability/...`. Each builds a service provider with the ADO outbox store, a real `IDatabaseHealthMonitor` and `AddEncinaOpenTelemetry`, adds and processes an outbox message, and asserts through an `ActivityListener` and a `MeterListener` the span tags (`db.system.name` of the provider, `encina.db.access = ado`), the counter and the histogram, and that a store failure (a closed connection) records `encina.outcome = error` with the error code.
2. `.github/coverage-manifest/Encina.OpenTelemetry.json` (integration target of the new files) through `mechanical-fixer`; the package README of `Encina.OpenTelemetry` (new instruments and tags) through `docs-writer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #721 plan docs/plans/ado-opentelemetry-implementation-plan-721.md, phase 5. Phases 1-3 merged.

TASK:
1. For each of SqlServer, PostgreSQL and MySQL write one integration test class in tests/Encina.IntegrationTests/ADO/<Provider>/Observability/
   using the shared collection fixture; assert span tags, counter, histogram and the error path with a real store.

KEY RULES:
- Shared [Collection("ADO-<Db>")] fixtures only; never a per-class fixture, never IClassFixture, never dispose the fixture.
- ClearAllDataAsync in InitializeAsync; no Thread.Sleep; listeners disposed in DisposeAsync.

REFERENCE FILES:
- tests/Encina.IntegrationTests/ADO/SqlServer/Outbox/OutboxStoreADOTests.cs, tests/Encina.IntegrationTests/ADO/Collections.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ADR-054 | Settled points 2, 4, 7; D3 names; D4 tenant tag; D6 database stores at Encina operation level, never `db.query.text` | Instrument names, error code only, zero cost, scope |
| OpenTelemetry database conventions | `db.system.name` values `microsoft.sql_server`, `postgresql`, `mysql`, `mongodb`; duration unit `s` | Keys and values |
| ADR-018, `AGENTS.md` section 6 | Cross-cutting evaluation | The matrix below |
| ADR-062 | Error category contract | No error is created (C1) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Instrumented*Store`, `InstrumentedUnitOfWork` | `src/Encina.OpenTelemetry/MessagingStores/`, `UnitOfWork/` | Extended |
| `DecorateService` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | New overload passes the scope's provider |
| `IDatabaseHealthMonitor.ProviderName` | `src/Encina/Database/IDatabaseHealthMonitor.cs` | Provider identity |
| `ActivityStatusDescriptions.ForError` | `src/Encina/Diagnostics/ActivityStatusDescriptions.cs` | Error code for status and `error.type` |
| Shared collections | `tests/Encina.IntegrationTests/ADO/Collections.cs` | Phase 5 |
| `MessagingStoreMetrics`, `UnitOfWorkMetrics` | `src/Encina.Messaging/Diagnostics/`, `src/Encina.DomainModeling/Diagnostics/` | Existing store counters, renamed by the #1791 follow-up, not touched |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.OpenTelemetry` | none | No logging code is added (register row C2); the block 6380-6399 is not used by this plan |

### Estimated File Count

Every count includes tests, manifest, PublicAPI and the changelog fragment; each pull request builds and passes alone. No public API changes (all new types are internal), so no `PublicAPI` file changes.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Phases 1-2: shared telemetry, outbox and inbox | 2 | 3 | 2 | 5 | 1 | 1 (changelog) | 14 | ~700 |
| S2 | Phase 3: saga, scheduled, dead letter (the three stores and `ServiceCollectionExtensions.cs`) | 0 | 4 | 1 (contract) | 7 (3 unit, 3 guard, DI test) | 1 | 0 | 13 | ~550 |
| S2b | Phase 3: unit of work | 0 | 2 (decorator, `ServiceCollectionExtensions.cs`) | 1 (guard) | 2 (unit, DI test) | 1 | 0 | 6 | ~200 |
| S3 | Phase 4: tenant tag (after #2676) | 0 | 1 | 0 | 1 | 0 | 1 (changelog) | 3 | ~80 |
| S4 | Phase 5: proof on SqlServer, PostgreSQL, MySQL | 0 | 0 | 3 | 0 | 1 | 1 (README) | 5 | ~420 |
| **Total** | | **2** | **10** | **7** | **15** | **4** | **3** | **41** | **~1,950** |

Columns sum: 2 + 10 + 7 + 15 + 4 + 3 = 41 = 14 + 13 + 6 + 3 + 5. The integration test project already references `Encina.OpenTelemetry`, so S4 needs no csproj edit. Test doubles: no interface changes, so no fake changes; the `new Instrumented*Store(inner)` call sites in tests (counted with grep before each PR) keep compiling because the new parameters are optional and trailing.

Per-file coverage targets (per flag, one sentence each; none lowers an existing target):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Telemetry/DatabaseIdentity.cs` | 95 | 60 | 0 | 0 | 0 | Every provider name, `null`, empty and unknown are table-driven unit cases; guard tests cover the `null` input as a valid result; contract tests exercise it through the store contract test; no integration target because the class has no I/O |
| `Telemetry/StoreTelemetry.cs` | 90 | 0 | 0 | 0 | 40 | Listener-driven unit tests reach success, error, disabled-instrument and tenant paths; guard 0 because the type is internal with no validated parameter (its callers' guard tests exist); the three integration tests run it on real stores |
| `MessagingStores/InstrumentedOutboxStore.cs` (existing 61 unit, 58 guard) | 75 | 60 | 0 | 0 | 0 | The new metric calls are on every instrumented path and driven by the updated unit tests; guard targets rise slightly with the null-optional-parameter cases |
| `MessagingStores/InstrumentedInboxStore.cs` (existing 88, 74) | 90 | 76 | 0 | 0 | 0 | Same pattern as the outbox decorator, already covered more heavily today |
| `MessagingStores/InstrumentedSagaStore.cs` (existing 76, 73) | 80 | 75 | 0 | 0 | 0 | Same pattern; the rest is the no-listener fast path |
| `MessagingStores/InstrumentedScheduledMessageStore.cs` (existing 76, 71) | 80 | 73 | 0 | 0 | 0 | Same pattern |
| `MessagingStores/InstrumentedDeadLetterStore.cs` (existing 95, 8) | 95 | 8 | 0 | 0 | 0 | Targets stay: the existing guard target is low because the file is mostly delegation and the added lines are unit-driven |
| `UnitOfWork/InstrumentedUnitOfWork.cs` (existing 90 unit, 0 integration) | 90 | 50 | 0 | 0 | 0 | Only two tags are added to existing spans; the unit target stays; the new guard test file (S2b) runs the constructor with a `null` identity as a valid result and adds the inner check (`ArgumentNullException.ThrowIfNull(inner)`, absent today) with its exception test, so a guard target of 50 is realistic |
| `ServiceCollectionExtensions.cs` (existing 90, 35) | 90 | 35 | 0 | 0 | 0 | The new overload is driven by the DI tests; targets stay |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #721 - OpenTelemetry for the ADO.NET store providers - in five small pull requests (S1, S2, S2b, S3, S4).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no compatibility layers; edit source only with the Edit/Write tools; manifests,
  changelog fragments and PublicAPI lines through mechanical-fixer; READMEs through docs-writer.
- Encina.OpenTelemetry already decorates the message stores and the unit of work for ALL database providers. ADR-054 fixes names
  (lowercase dotted, seconds, no _total), error code only, zero cost without a listener, tenant tag only when multi-tenancy is on.

IMPLEMENTATION OVERVIEW:
1. S1: DatabaseIdentity and StoreTelemetry (internal), a DecorateService overload with the IServiceProvider, the meter
   "Encina.Messaging" subscribed in WithEncina, outbox and inbox decorators record encina.store.operations and
   encina.store.operation.duration with db.system.name and encina.db.access.
2. S2: the same for saga, scheduled, dead letter and a contract test across the five stores; S2b: provider tags on the unit of work spans.
3. S3 (after #2676): the tenant tag through the shared helper.
4. S4: integration tests for ADO SqlServer, PostgreSQL and MySQL with the shared collection fixtures.

KEY PATTERNS:
- Optional trailing parameters (identity, timeProvider); guard tests give null a valid-result test.
- No db.query.text, no EncinaError.Message, no ActivitySource or Meter inside the ADO or Dapper packages.
- Repository and bulk-operation wiring is out of scope (follow-up issue file).

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, MessagingStores/, UnitOfWork/
- src/Encina/Database/IDatabaseHealthMonitor.cs
- tests/Encina.IntegrationTests/ADO/, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Instrumentation observes stores; it adds no cached reads or writes |
| 2 | OpenTelemetry | ✅ Phases 1-5 | This is the feature: store metrics, provider identity and proof on the three ADO providers |
| 3 | Structured Logging | ❌ N/A | The decorators log nothing and no EventId is added (register row C2) |
| 4 | Health Checks | ❌ N/A | Observes operations; `IDatabaseHealthMonitor` is read for its provider name only |
| 5 | Validation | ❌ N/A | Instrumentation receives no input |
| 6 | Resilience | ❌ N/A | The decorator adds no external call and never retries |
| 7 | Distributed Locks | ❌ N/A | No shared state is written |
| 8 | Transactions | ❌ N/A | The unit of work decorator only gains provider tags; it never commits or rolls back |
| 9 | Idempotency | ❌ N/A | Recording a measurement twice is harmless; no deduplication is involved |
| 10 | Multi-Tenancy | ✅ Phase 4 | Tenant tag only when multi-tenancy is on, through the helper of #2676 (ADR-054 D4) |
| 11 | Module Isolation | ❌ N/A | Telemetry carries no module data |
| 12 | Audit Trail | ❌ N/A | No personal data reaches telemetry; the operation audit decorator is unchanged |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 accepted | ✅ On `main` | D3, D4, D6 |
| #2676 shared tenant helper | Open | Only S3 waits for it; S1, S2 and S4 do not |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Dapper proof on the three Dapper providers | #722 | Uses the pieces of S1; opens after S1 |
| Every source and meter subscribed by wildcard | #1787 | `AddMeter("Encina.Messaging")` is added explicitly meanwhile |
| Dead and duplicate activity sources | #1790 | Not touched here |
| Observability guide (instrument catalog and tenant cardinality) | #906 | Documents the new instruments |

---

## Next Steps

1. Link this plan from #721 and ask the maintainer to answer Design Choices 1 to 5 and register row C6 when work starts.
2. Implement S1, S2, S2b and S4; S3 after #2676. Pull request order inside the family: this plan first, then #722.
3. Follow-up issue files written in `artifacts/issues/` of the planning worktree, to open with this plan: `db-system-attribute-spelling` (the three legacy `db.system` sites), `repository-bulk-instrumentation-wiring` (Design Choice 5), `otel-decoration-order` (register row C6).
4. Maintainer action: amend the acceptance criteria of #721 that name SQLite, `db.operation` and a separate error counter to the outcome of Design Choices 3 and 4 (this plan never comments on an issue).
