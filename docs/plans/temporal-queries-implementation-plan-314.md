# Implementation Plan: Temporal Queries (Time Travel) for Event-Sourced Aggregates (`Encina.Marten`)

> **Issue**: [#314](https://github.com/dlrivada/Encina/issues/314)
> **Type**: Feature
> **Related**: #325 (closed as a duplicate of this issue; its `ITemporalEventStore` and timestamp-index idea are carried into Design Choices 4 and 5), #328 (bi-temporal modeling, a different feature), #323 (snapshot strategies), #320 (decider pattern), #326 (Marten multi-tenancy), #697 (aggregate cache), #2676 (tenant tag helper), ADR-054, ADR-062
> **Complexity**: Medium (5 phases, 5 pull requests, one provider)
> **Estimated Scope**: ~700 lines of production code + ~1,500 lines of tests and documentation, in five pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-26, `origin/main` `11ead5212`)

- `IAggregateRepository<TAggregate>` (`src/Encina.Marten/IAggregateRepository.cs`) already loads the current state and the state at a **version**: `LoadAsync(Guid id, int version, ct)` replays `Events.FetchStreamAsync(id, version: version)` in `MartenAggregateRepository` and uses `ISnapshotStore<TAggregate>.GetAtVersionAsync(maxVersion)` through `SnapshotAwareAggregateRepository` (`FetchStreamAsync(..., fromVersion: snapshot.Version + 1)`, `SnapshotAwareAggregateRepository.cs` lines 356-365). There is no load **by instant**, no history page of one stream and no diff.
- `IEventMetadataQuery` (`src/Encina.Marten/IEventMetadataQuery.cs`) filters events by `EventQueryOptions.StreamId`, `FromTimestamp` and `ToTimestamp`, but is entered only through a correlation id, a causation id or an event id; its result types `EventQueryResult` and `EventWithMetadata` (`Id`, `StreamId`, `Version`, `Sequence`, `EventTypeName`, `Data`, `Timestamp`, `CorrelationId`, `CausationId`, `Headers`) are reusable.
- `Encina.EventStoreDB` does not exist in `src/` (it is planned by #576 and #585); `Encina.Marten` has no `PublicAPI.Unshipped.txt` on main, so no PublicAPI line is written for it; the package grants `InternalsVisibleTo` to UnitTests, GuardTests, IntegrationTests, ContractTests and PropertyTests (`Encina.Marten.csproj`).
- Marten 9.38.0 is pinned (`Directory.Packages.props`). The repository comment in `MartenAggregateRepository.SaveAsync` records that Marten 9 defaults to `EventAppendMode.QuickWithServerTimestamps`: event timestamps are written by the database server clock, not by the application.
- Tenancy: `EventMetadataEnrichmentService` copies `IRequestContext.TenantId` into the `TenantId` event header when `EventMetadataOptions.CaptureTenantId` (default `true`) is on; nothing in `Encina.Marten` isolates tenants yet (#326, open). `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`) signals that an application is multi-tenant.
- `Encina.Audit.Marten` has an unrelated `TemporalPeriodHelper` and `MartenTemporalKeyProvider` (audit retention windows); the new types live in `Encina.Marten.Temporal` and do not reuse those names.

### What this plan delivers

- **`ITemporalRepository<TAggregate>`** in `Encina.Marten.Temporal`, every method returning `Task<Either<EncinaError, ...>>` (the issue's `ValueTask<TAggregate?>` signatures return null or throw, which the ROP rule of `AGENTS.md` section 3 forbids):
  - `GetAtAsync(Guid id, DateTimeOffset pointInTimeUtc, ct)`: the aggregate as it was at that instant.
  - `GetHistoryAsync(Guid id, TemporalRange? range, int skip, int take, ct)`: a page of `EventWithMetadata` of one stream.
  - `GetDiffAsync(Guid id, DateTimeOffset fromUtc, DateTimeOffset toUtc, ct)`: `AggregateDiff<TAggregate>` with the state before, the state after and the events between.
- **Two implementations** registered by `AddTemporalRepository<TAggregate>()` (replay from the start of the stream) and `AddSnapshotableTemporalRepository<TAggregate>()` (starts from `ISnapshotStore<TAggregate>.GetAtVersionAsync`, the issue's snapshot optimization).
- **A fail-closed tenant gate**: with multi-tenancy on, a read with no tenant in the request context, or on a stream whose first event has no `TenantId` header or another tenant's, is denied (Design Choice 3).
- **Observability** in a new EventId range, an `ActivitySource` and a `Meter` (ADR-054).
- **Not delivered**: `ITemporalProjection<TReadModel>` (Design Choice 6 records why and where it goes), an EventStoreDB implementation (the package does not exist; the interface is store-neutral in shape so #576 can implement it), `GetAsync` and `GetAtVersionAsync` of the issue's interface (both already exist as `IAggregateRepository.LoadAsync(id)` and `LoadAsync(id, version)`, Design Choice 1).
- Affected packages: `Encina.Marten` only (code and `.github/coverage-manifest/Encina.Marten.json`); `Encina` core for the EventId range (`EventIdRanges.cs`, its `PublicAPI.Unshipped.txt`, manifest `Encina.json`). Provider category: event sourcing (Marten primary, `AGENTS.md` section 5); the 10-database rule does not apply; integration tests run Marten on PostgreSQL through Testcontainers (`AGENTS.md` section 3).

---

## Design Choices

<details>
<summary><strong>1. Shape of the interface: what is new and what already exists</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITemporalRepository<T>` holds only what is new (`GetAtAsync`, `GetHistoryAsync`, `GetDiffAsync`); current state and state at a version stay on `IAggregateRepository<T>`** | One code path per behavior; no second `LoadAsync(version)` to keep equal to the first | A caller needing both injects two interfaces |
| **B) The issue's full interface, including `GetAsync` and `GetAtVersionAsync`, delegating to `IAggregateRepository`** | One injected type for every time-travel call | Two public entry points for one behavior; the delegation is dead weight (`AGENTS.md` section 3, every line serves a current purpose) |

### Chosen Option: **A - Only the new operations**

### Rationale

- A follows the existing `LoadAsync(Guid, int, ct)` contract that `IAggregateRepositoryContractTests` already pins, and keeps the snapshot-aware and plain repositories as the single owners of version loading. The registration extension also registers nothing for `IAggregateRepository`: the application keeps its own `AddAggregateRepository<T>()`.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 3 ("Every line serves a current purpose"); existing `IAggregateRepository.LoadAsync(Guid, int, CancellationToken)` at `src/Encina.Marten/IAggregateRepository.cs`

</details>

<details>
<summary><strong>2. How a point in time is turned into a state</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Replay: `Events.FetchStreamAsync(id, timestamp: pointInTimeUtc)` and fold every event from version 1 (the plain repository)** | Native Marten support, no extra query; correct for any aggregate | Cost grows with the stream length |
| **B) Snapshot first: find the highest version with `Timestamp <= pointInTime` (a query over the stream's events), take `ISnapshotStore.GetAtVersionAsync(thatVersion)`, then replay only `fromVersion: snapshot.Version + 1` up to that version (the snapshot repository)** | Bounded replay for long streams; reuses the snapshot store and the `fromVersion` call of the existing repository | One extra query for the version; needs `ISnapshotable<TAggregate>` and a configured snapshot store |
| **C) Store a second set of time-indexed snapshots** | Constant-time reads | New storage on top of #323's snapshots, no demand recorded; rejected for pre-1.0 scope |

### Chosen Option: **A and B as two classes behind two registrations** (recommended, pending the maintainer)

### Rationale

- The two classes mirror `MartenAggregateRepository` and `SnapshotAwareAggregateRepository` and the two existing extensions `AddAggregateRepository` and `AddSnapshotableAggregate` (`ServiceCollectionExtensions.cs`); the generic constraint `ISnapshotable<TAggregate>` of `ISnapshotStore<T>` makes one class with an optional store impossible. A wins for short streams and for aggregates without snapshots; B wins when a stream passes the snapshot interval of #323. The issue asks for both ("fallback implementation with event replay" and "snapshot optimization"). Single-class alternatives are not valid because of the constraint; the Dilemma is only whether B ships in this plan or in the follow-up that comes after the maintainer sees A in use. The recommendation is to ship both (T2, T3).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Tenant isolation of a temporal read</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inherit the per-tenant session accessor of #326: isolation by construction** | No extra check; one mechanism | Blocked on #326 (open, no code); until it merges a temporal read is as open as `LoadAsync` is today |
| **B) Own gate now: when the application is multi-tenant (`MultiTenancyMarker` registered) and `TemporalOptions.RequireTenantContext` (default `true`) is on, deny a read with no `IRequestContext.TenantId` (`InvalidRequest`), and deny a stream whose first event has no `TenantId` header or a different one (the read is answered as `NotFound` so the stream's existence is not revealed)** | Fail closed today on a shared store; works with `CaptureTenantId` (default on); stays valid, as defence in depth, once #326 lands | Streams written before `CaptureTenantId` or with it off are unreadable in a multi-tenant application until backfilled; one extra event read per call |
| **C) No tenant check, document it** | Smallest | A temporal read would widen access to history compared to the gates of `AGENTS.md` section 3 |

### Chosen Option: **B - Own gate, kept after #326** (recommended, pending the maintainer)

### Rationale

- B wins because compliance gates fail closed when context is missing (`AGENTS.md` section 3; SPEC-002 DEC-006) and #326 has no code yet; A wins once #326 has merged if the maintainer prefers a single mechanism and accepts removing the header check then; C wins only for single-tenant applications, which B already treats as unchanged (the marker is absent). The plan-own option `RequireTenantContext = true` follows the pattern of `ConsentOptions.RequireTenantContext` (`src/Encina.Compliance.Consent/ConsentOptions.cs`), but is a plain `bool` with a fail-closed default: setting it to `false` is logged once at startup. Tenant segment note: there is no tenant-qualified id here, the stream id stays the caller's `Guid` and the tenant is compared as data, so two tenant/key pairs can never map to one id; the reserved tenant literal `default` is never invented: a missing tenant in a multi-tenant application is a `Left`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Timestamp source and the index carried over from #325</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the Marten event `Timestamp` (database server clock, `QuickWithServerTimestamps`) and no new index: every query is scoped to one `stream_id`, which Marten already indexes** | No schema change, no migration, no clock the application can skew | The reference instant is the database clock: an application with a skewed clock asks for instants that are shifted by the skew |
| **B) Add a Marten index on the event timestamp through `StoreOptions` (the #325 idea)** | Helps a cross-stream query by time ("all events at 14:00"), which this plan does not offer | Index maintenance on the hottest table for a query this plan never issues |
| **C) Store an application-written instant in a header and query that** | Application clock semantics | A second clock next to Marten's; unusable for streams already written |

### Chosen Option: **A - Marten timestamp, no new index** (recommended, pending the maintainer)

### Rationale

- A wins because per-aggregate queries are bounded by the stream; B wins when a later feature needs cross-stream queries by time (the temporal projection of Design Choice 6 would), and #325's index belongs to that feature, not to this one; C wins only if the application clock must be authoritative. The guide documents the database-clock semantics and that `pointInTimeUtc` is compared with it. The implementing PR measures the plan of the stream-and-timestamp query on PostgreSQL in its integration test before keeping A.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Result shape of the history</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A page: `Task<Either<EncinaError, EventQueryResult>>` with `Skip` and `Take` (default 100, at most 1,000), the type `IEventMetadataQuery` already returns** | One result type for every event read; a failure is one `Left`; bounded memory | The caller pages explicitly |
| **B) `IAsyncEnumerable<EventWithMetadata>` as the issue sketches** | Natural streaming | A failure in the middle of the stream can only be an exception, which the ROP rule forbids; no `Either` per item convention exists |

### Chosen Option: **A - A page of `EventQueryResult`**

### Rationale

- A follows the established read contract and keeps every failure a `Left`. `TemporalRange` carries `FromUtc`, `ToUtc`, `FromVersion` and `ToVersion` (all optional); a range with `FromUtc > ToUtc` or `FromVersion > ToVersion` is `Validation`.

**Class:** Trivial
**Settled by:** `src/Encina.Marten/IEventMetadataQuery.cs` (`EventQueryResult`, `EventQueryOptions`); `AGENTS.md` section 3 (Railway Oriented Programming)

</details>

<details>
<summary><strong>6. Temporal projections (`ITemporalProjection<TReadModel>`)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not in this plan: a read model at an instant needs the projection handlers re-run over the events up to that instant, which is the rebuild engine of the blue-green and extended-projection plans (#324, #535)** | No second replay engine; this plan stays in five pull requests | The issue's `ITemporalProjection` task stays open |
| **B) A single-stream implementation here: replay the stream's events through the registered `IProjection<TReadModel>` handlers into an in-memory read model** | Delivers the task for single-stream read models | Wrong for read models that span streams, which are the common case; duplicates handler invocation logic of `MartenProjectionManager` |

### Chosen Option: **A - Deferred to the projection plans** (recommended, pending the maintainer)

### Rationale

- A wins because correct temporal read models need the multi-stream replay that #324 and #535 own; B wins only if the maintainer accepts single-stream read models as sufficient. The acceptance criteria of the issue do not list `ITemporalProjection` (only `ITemporalRepository`, the two implementations, `AggregateDiff`, docs and tests), so the plan records the task as follow-up work inside this plan's Next Steps and the maintainer's amendment of the issue; no new issue is opened because #324 and #535 are the named owners and both are open.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Time is explicit: `GetAtAsync` and `GetDiffAsync` take `DateTimeOffset` instants and never read the clock; the repository takes no `TimeProvider` because it never needs "now". The issue's `DateTime` is replaced to avoid ambiguous kinds | Settled | `AGENTS.md` section 3 (time comes from `TimeProvider`; nothing reads `DateTime.UtcNow`) |
| C2 | Error codes (class `MartenErrorCodes`, prefix `marten.`): `marten.temporal_invalid_range` (`Validation`, `FromUtc > ToUtc`, `FromVersion > ToVersion`, `take` outside 1-1000, `default(Guid)`), `marten.temporal_tenant_required` (`InvalidRequest`, multi-tenant application with no tenant in the context), `marten.temporal_query_failed` (`Internal`, store or serialization failure). A stream with no events at or before the instant, and a stream denied by the tenant check, both reuse `marten.aggregate_not_found` (`NotFound`: the aggregate id is supplied by the caller) | Settled | ADR-062 rules R1, R3, R4 (`docs/architecture/adr/062-error-category-contract.md`) |
| C3 | The repositories read through `IQuerySession` (scoped), never `IDocumentSession`: a temporal read opens no unit of work and never calls `SaveChangesAsync` | Trivial | `src/Encina.Marten/MartenAggregateRepository.cs` (writes use the session; reads need no tracking) |
| C4 | Both repositories are registered scoped with `TryAddScoped`; neither is injected into a singleton | Settled | `AGENTS.md` section 3 (registration completeness); existing `AddAggregateRepository` uses `TryAddScoped` |
| C5 | A `Left` is returned to the caller and logged by error code and exception type through `ForLogging()`; `EncinaError.Message` never reaches a log or tag | Settled | `AGENTS.md` section 3 (`EncinaError.Message` rule) |
| C6 | Telemetry: `ActivitySource` and `Meter` named `Encina.Marten` (the same name #2383 and #320 give theirs; listeners subscribe by name); span `encina.marten.temporal.query`; counter `encina.marten.temporal.queries` (no `_total`) with tags `encina.marten.temporal.operation` (`get_at`, `history`, `diff`) and `encina.marten.temporal.outcome` (`ok`, `not_found`, `denied`, `failed`); histogram `encina.marten.temporal.query.duration` in seconds; the tenant tag only through the #2676 helper when it has merged; never the aggregate id, tenant id or user id | Settled | ADR-054 (no `_total` suffix, decision 4; tenant tag D4) |
| C7 | Reading history of an erased subject: events are returned as stored, so crypto-shredded fields stay unreadable exactly as in `LoadAsync`; the repository neither decrypts nor caches decrypted state | Trivial | `Encina.Marten.GDPR` crypto-shredding serializer (`src/Encina.Marten.GDPR`) is applied by Marten's serializer for every read |

---

## Implementation Phases

### Delivery order and size

Five pull requests; each builds and passes alone with its own tests. Counts include every edited file (manifest, DI, error codes, options, EventId registry, allocation test, PublicAPI of `Encina`, fragment).

| PR | Phase | Content | Files | Lines (est.) |
|----|-------|---------|-------|--------------|
| T1 | 1 | Contracts: `ITemporalRepository`, `TemporalRange`, `AggregateDiff`, `TemporalOptions`, error codes | 10 | ~550 |
| T2 | 2 | `MartenTemporalRepository`, `TemporalTenantGate`, `AddTemporalRepository`, integration tests on Marten | 8 | ~1,200 |
| T3 | 3 | `SnapshotAwareTemporalRepository`, `AddSnapshotableTemporalRepository`, contract and integration tests | 7 | ~1,000 |
| T4 | 4 | Observability: diagnostics, log, EventId range, registry, allocation map | 12 | ~900 |
| T5 | 5 | Guide, Marten README, INVENTORY, ROADMAP, changelog | 5 | ~450 |

### Phase 1: Contracts, Options and Error Codes (pull request T1)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.Marten/Temporal/ITemporalRepository.cs` (`public interface ITemporalRepository<TAggregate> where TAggregate : class, IAggregate`; the three methods above with XML docs and an example), `TemporalRange.cs` (`public sealed record TemporalRange(DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, int? FromVersion = null, int? ToVersion = null)` with `internal Either<EncinaError, Unit> Validate()`), `AggregateDiff.cs` (`public sealed class AggregateDiff<TAggregate> where TAggregate : class` with a constructor `(TAggregate before, TAggregate after, IReadOnlyList<EventWithMetadata> eventsBetween)` that null-checks all three), `TemporalOptions.cs` (`public sealed class TemporalOptions { public bool RequireTenantContext { get; set; } = true; }`).
2. **Modify** `src/Encina.Marten/MartenErrorCodes.cs` (the three codes of register row C2), `src/Encina.Marten/EncinaMartenOptions.cs` (property `Temporal` of type `TemporalOptions`), `.github/coverage-manifest/Encina.Marten.json` (per-file targets of the table below; through `mechanical-fixer`).
3. Tests: new `tests/Encina.UnitTests/Marten/Temporal/TemporalRangeTests.cs` (every invalid combination, valid open range), new `tests/Encina.GuardTests/Marten/Temporal/AggregateDiffGuardTests.cs` (null before, after and events; valid construction), `tests/Encina.UnitTests/Marten/Temporal/TemporalOptionsTests.cs` (default is fail closed).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #314, plan docs/plans/temporal-queries-implementation-plan-314.md, phase 1.
Encina.Marten has no PublicAPI.Unshipped.txt; do not create one.

TASK:
1. Create ITemporalRepository, TemporalRange, AggregateDiff and TemporalOptions in src/Encina.Marten/Temporal/.
2. Add the three error codes to MartenErrorCodes and the Temporal property to EncinaMartenOptions.
3. Unit and guard tests; manifest targets through mechanical-fixer.

KEY RULES:
- Every method returns Task<Either<EncinaError, T>>; no exceptions for invalid input; no DateTime, only DateTimeOffset.
- Error codes carry ErrorCategory per ADR-062 (Validation, InvalidRequest, Internal); never FailedPrecondition.
- TemporalOptions.RequireTenantContext defaults to true (fail closed).
- XML docs with examples on every public member; zero warnings.

REFERENCE FILES:
- src/Encina.Marten/IAggregateRepository.cs, src/Encina.Marten/IEventMetadataQuery.cs, src/Encina.Marten/MartenErrorCodes.cs
- src/Encina.Compliance.Consent/ConsentOptions.cs (RequireTenantContext precedent)
```

</details>

### Phase 2: Replay Repository and Tenant Gate (pull request T2)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.Marten/Temporal/MartenTemporalRepository.cs` (`public sealed class MartenTemporalRepository<TAggregate> : ITemporalRepository<TAggregate> where TAggregate : class, IAggregate, new()`; constructor `(IQuerySession session, IRequestContext requestContext, IOptions<EncinaMartenOptions> options, ILogger<MartenTemporalRepository<TAggregate>> logger, IServiceProvider? services = null)` where the optional provider only resolves `MultiTenancyMarker`; `GetAtAsync` fetches `Events.FetchStreamAsync(id, timestamp: pointInTimeUtc, token: ct)`, returns `marten.aggregate_not_found` for an empty list, folds with `new TAggregate().LoadFromHistory(events.Select(e => e.Data))` as `MartenAggregateRepository.Replay` does; `GetHistoryAsync` pages the stream through `QueryAllRawEvents()` filtered by `StreamId`, optional time and version bounds, ordered by version, mapping to `EventWithMetadata` with the tenant gate applied first; `GetDiffAsync` loads both states and the events in `(fromUtc, toUtc]`).
2. **Create** `src/Encina.Marten/Temporal/TemporalTenantGate.cs` (`internal sealed class`; `Task<Either<EncinaError, Unit>> CheckAsync(IQuerySession session, Guid streamId, ct)`: no-op when the application is not multi-tenant or `RequireTenantContext` is `false`; `InvalidRequest` when `IRequestContext.TenantId` is empty; reads the first event of the stream and denies as `aggregate_not_found` when its `TenantId` header is missing or different).
3. **Modify** `src/Encina.Marten/ServiceCollectionExtensions.cs` (`AddTemporalRepository<TAggregate>()` with `TryAddScoped`; one startup log line when `RequireTenantContext` is `false`).
4. Tests: new `tests/Encina.IntegrationTests/Marten/Temporal/MartenTemporalRepositoryIntegrationTests.cs` in the shared `[Collection(MartenCollection.Name)]` fixture (state at three instants, an instant before creation is `NotFound`, an instant after the last event equals the current state, history paging and bounds, diff, two tenants: B cannot read A's stream, no tenant in a multi-tenant host is `InvalidRequest`); new `tests/Encina.UnitTests/Marten/Temporal/TemporalTenantGateTests.cs`; new `tests/Encina.GuardTests/Marten/Temporal/MartenTemporalRepositoryGuardTests.cs` (null constructor arguments, empty id, `take` out of range through the method); extend the DI test of `Encina.Marten` with `ValidateOnBuild` and `ValidateScopes` for `AddTemporalRepository`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
Issue #314 plan docs/plans/temporal-queries-implementation-plan-314.md, phase 2. T1 merged.

TASK:
1. MartenTemporalRepository (replay), TemporalTenantGate, AddTemporalRepository.
2. Integration tests on Marten PostgreSQL (Testcontainers, shared MartenFixture collection), unit, guard and DI tests.

KEY RULES:
- Read through IQuerySession; never SaveChangesAsync. Scoped registration with TryAddScoped; never inject a scoped service into a singleton.
- Multi-tenant application (MultiTenancyMarker registered) and RequireTenantContext on: missing tenant is InvalidRequest, a foreign or unlabelled stream is NotFound; never invent the tenant "default".
- Catch exceptions at the store boundary and return marten.temporal_query_failed (Internal) with the exception attached; never swallow a Left.
- Do not describe any read as atomic or as part of a transaction.

REFERENCE FILES:
- src/Encina.Marten/MartenAggregateRepository.cs (Replay, LoadAsync with version), src/Encina.Marten/EventMetadataEnrichmentService.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
```

</details>

### Phase 3: Snapshot-Aware Temporal Repository (pull request T3)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.Marten/Temporal/SnapshotAwareTemporalRepository.cs` (`where TAggregate : class, IAggregate, ISnapshotable<TAggregate>, new()`; constructor `(IQuerySession session, ISnapshotStore<TAggregate> snapshotStore, IRequestContext requestContext, IOptions<EncinaMartenOptions> options, ILogger<...> logger, IServiceProvider? services = null)`; `GetAtAsync` resolves the highest version with `Timestamp <= pointInTimeUtc`, asks `GetAtVersionAsync(id, thatVersion)`, then folds `FetchStreamAsync(id, version: thatVersion, fromVersion: snapshot.Version + 1)` onto the snapshot state; with no snapshot it replays from the start; history and diff reuse the logic of `MartenTemporalRepository`, which T3 moves into an internal static helper class declared inside the existing file `MartenTemporalRepository.cs`, so no new file is added).
2. **Modify** `ServiceCollectionExtensions.cs` (`AddSnapshotableTemporalRepository<TAggregate>()`), `.github/coverage-manifest/Encina.Marten.json`.
3. Tests: new `tests/Encina.ContractTests/Marten/Core/ITemporalRepositoryContractTests.cs` (the same behavior assertions run against both implementations, modelled on `IAggregateRepositoryContractTests`), new `tests/Encina.IntegrationTests/Marten/Temporal/SnapshotAwareTemporalRepositoryIntegrationTests.cs` (result equals the replay implementation at every probed instant; a snapshot after the instant is ignored; pruned snapshots fall back to replay), guard tests for the constructor.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
Issue #314 plan docs/plans/temporal-queries-implementation-plan-314.md, phase 3. T1 and T2 merged.

TASK:
1. SnapshotAwareTemporalRepository and AddSnapshotableTemporalRepository; move the shared history and diff logic into an internal static helper inside MartenTemporalRepository.cs.
2. Contract test over both implementations, integration test proving equality with the replay result, guard tests.

KEY RULES:
- The snapshot is only an optimization: for every instant the result equals the replay result.
- A snapshot taken after the requested instant is never used (GetAtVersionAsync with the version at the instant).
- Same tenant gate and error codes as T2; contract and integration tests instantiate the real repositories on a real Marten store.

REFERENCE FILES:
- src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs (lines 319-365), src/Encina.Marten/Snapshots/ISnapshotStore.cs
- tests/Encina.ContractTests/Marten/Core/IAggregateRepositoryContractTests.cs
```

</details>

### Phase 4: Observability and EventIds (pull request T4)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.Marten/Temporal/Diagnostics/TemporalDiagnostics.cs` (internal static: `ActivitySource` and `Meter` named `Encina.Marten`, instruments of register row C6), `src/Encina.Marten/Temporal/TemporalLog.cs` (`[LoggerMessage]`, `internal static partial class`, ids 12450-12455: 12450 read denied by tenant gate (Warning: operation, outcome), 12451 not found at the instant (Debug: aggregate type), 12452 snapshot used (Debug: snapshot version, events replayed), 12453 replay completed (Debug: events replayed), 12454 temporal read failed (Error: error code, `ex.ForLogging()`), 12455 fail-open option logged at startup (Warning)).
2. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs` (field `MartenTemporal = (12450, 12499)`), `src/Encina/PublicAPI.Unshipped.txt` (one line for the field, through `mechanical-fixer`), `docs/engineering/eventid-block-registry.md` (a `Block | Area | Owner | Status` row `12450-12499 | Core and messaging | #314 MartenTemporal | registered` and the `Name | Block | Area | Zone` row), `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (`AssemblyRanges` entry: `Encina.Marten` also maps to `MartenTemporal`), the two repositories (record spans, counters and logs), `.github/coverage-manifest/Encina.json` and `Encina.Marten.json`.
3. Tests: new `tests/Encina.UnitTests/Marten/Temporal/TemporalDiagnosticsTests.cs` (listener sees the span and the instruments; no tag carries an id), edit the integration tests to assert the outcome tag; run `EventIdRegistryAgreementTests` and `EncinaEventIdAllocationTests`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
Issue #314 plan docs/plans/temporal-queries-implementation-plan-314.md, phase 4. T1-T3 merged.

TASK:
1. TemporalDiagnostics and TemporalLog; register EventIdRanges.MartenTemporal (12450, 12499) BEFORE using an id; registry rows; allocation map; PublicAPI of Encina.
2. Wire spans, counter, histogram and logs into both repositories.

KEY RULES:
- ADR-021 and ADR-054: names lowercase dotted, no _total suffix, duration in seconds with unit s; ids packed 12450-12455 without gaps.
- No aggregate id, tenant id, user id or EncinaError.Message in any tag or log; exceptions go through ForLogging().
- Instrumentation costs nothing without a listener.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
- src/Encina.Marten/Snapshots/SnapshotLog.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 5: Documentation and Finalization (pull request T5)

<details>
<summary>Tasks</summary>

1. Through `docs-writer`: `docs/features/temporal-queries.md` (guide: registration of both repositories, the three operations with the issue's examples rewritten against `Either`, database-clock semantics, tenant gate and `RequireTenantContext`, crypto-shredded history, when to add snapshots), the `src/Encina.Marten/README.md` section, `docs/INVENTORY.md`, `ROADMAP.md` line for #314.
2. `changelog.d/314-temporal-queries.added.md` through `mechanical-fixer`.
3. Build with zero warnings; every coverage flag reaches the targets below; the PR reports the measured per-file coverage.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
Issue #314 plan docs/plans/temporal-queries-implementation-plan-314.md, phase 5. All code merged.

TASK:
1. Ask docs-writer for the guide, README section, INVENTORY and ROADMAP lines; the fragment through mechanical-fixer.
2. Report measured per-file coverage against the plan.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers); public text never names a private client application.
- The guide states that the timestamp is the database clock and that no EventStoreDB implementation exists yet.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, docs/features/
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Martin Fowler, Event Sourcing (`martinfowler.com/eaaDev/EventSourcing.html`) | Temporal query: rebuild state as of a past time by replaying events | Contract of `GetAtAsync` |
| Axon Framework (named in the issue) | Point-in-time aggregate loading | Comparable product for the feature set |
| `AGENTS.md` section 3 | ROP, fail-closed gates, `EncinaError.Message` rule, event-sourced modules on Marten | Either everywhere, tenant gate, logging |
| SPEC-002 DEC-006, DEC-009 | Gates fail closed; multi-tenancy required in 1.0 | Design Choice 3 |
| ADR-054 | Telemetry names and privacy | Phase 4 |
| ADR-062 | `ErrorCategory` rules R1-R4 | Register row C2 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `MartenAggregateRepository<T>.Replay` and `LoadAsync(id, version)` | `src/Encina.Marten/MartenAggregateRepository.cs` | Fold pattern and version semantics |
| `SnapshotAwareAggregateRepository<T>`, `ISnapshotStore<T>.GetAtVersionAsync` | `src/Encina.Marten/Snapshots/` | Snapshot-first path |
| `EventQueryResult`, `EventWithMetadata`, `EventQueryOptions` | `src/Encina.Marten/IEventMetadataQuery.cs` | History page type |
| `EventMetadataEnrichmentService` (`TenantId` header) | `src/Encina.Marten/EventMetadataEnrichmentService.cs` | Tenant gate input |
| `MultiTenancyMarker`, `IRequestContext` | `src/Encina/Abstractions/` | Multi-tenant detection, tenant id |
| `MartenFixture` and `[Collection(MartenCollection.Name)]` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs` | Real Marten on PostgreSQL |
| `EncinaEventIdAllocationTests`, `EventIdRegistryAgreementTests` | `tests/Encina.UnitTests/Testing/Architecture/` | Allocation map and registry |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` (new field `MartenTemporal`) | `12450-12499`, ids 12450-12455 used | Area Core and messaging, first free 50-id slot after the claims on main through 12449 (`ValidationScoping` 12350-12399, `TenancyRedis` 12400-12449) and the #4007 reservations; the existing `Marten` range (2600-2799, moving to 11350-11549 in #4007 CM-6) is not used because 2701-2752 are claimed by #2383, #2384 and #584; T4 registers it and its registry rows |

### Estimated File Count

| PR | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| T1 | 4 (`ITemporalRepository`, `TemporalRange`, `AggregateDiff`, `TemporalOptions`) | 2 (`MartenErrorCodes.cs`, `EncinaMartenOptions.cs`) | 3 (`TemporalRangeTests`, `AggregateDiffGuardTests`, `TemporalOptionsTests`) | 0 | 1 (`Encina.Marten.json`) | 0 | 10 | ~550 |
| T2 | 2 (`MartenTemporalRepository`, `TemporalTenantGate`) | 1 (`ServiceCollectionExtensions.cs`) | 3 (integration, gate unit, guard) | 1 (the Marten DI test) | 1 | 0 | 8 | ~1,200 |
| T3 | 1 (`SnapshotAwareTemporalRepository`) | 2 (`ServiceCollectionExtensions.cs`, `MartenTemporalRepository.cs` helper extraction) | 3 (contract, integration, guard) | 0 | 1 | 0 | 7 | ~1,000 |
| T4 | 2 (`TemporalDiagnostics`, `TemporalLog`) | 4 (`EventIdRanges.cs`, `PublicAPI.Unshipped.txt` of `Encina`, the two repositories) | 1 (`TemporalDiagnosticsTests`) | 2 (`EncinaEventIdAllocationTests.cs`, the integration test) | 2 (`Encina.json`, `Encina.Marten.json`) | 1 (`eventid-block-registry.md`) | 12 | ~900 |
| T5 | 0 | 0 | 0 | 0 | 0 | 5 (guide, README, `INVENTORY.md`, `ROADMAP.md`, fragment) | 5 | ~450 |
| **Total** | **9** | **9** | **10** | **3** | **5** | **6** | **42** | **~4,100** |

Column check: 9 + 9 + 10 + 3 + 5 + 6 = 42 = 10 + 8 + 7 + 12 + 5. No existing interface changes: `IAggregateRepository`, `IEventMetadataQuery` and `ISnapshotStore` are untouched, so there are no implementers or test doubles to update; `ITemporalRepository` has two implementers (both in this plan) and no fake. The constructors are new, so `new MartenTemporalRepository<` sites exist only in the plan's own tests (counted above); the `EncinaMartenOptions` change adds a property, no constructor. `Encina.Marten` has no `PublicAPI.Unshipped.txt`; the only PublicAPI line is the `EventIdRanges` field in `Encina`. The orchestrator recounts with a grep on the branch when it writes the brief.

Per-file coverage targets (percent of coverable lines; baseline of the package: `Encina.Marten.json` targets unit 38, guard 7, contract 8, integration 65, set on 2026-11-16, measured against the Marten integration collection; the new rows are added to the manifest in the PR that creates the file):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Temporal/ITemporalRepository.cs` | 0 | 0 | 0 | 0 | 0 | Interface with no executable code; behavior is covered through both implementations |
| `Temporal/TemporalRange.cs` | 95 | 0 | 0 | 0 | 0 | Table-driven validation branches; no reference-type parameter checked |
| `Temporal/AggregateDiff.cs` | 80 | 90 | 0 | 0 | 0 | Constructor null checks by guard tests; property getters by unit |
| `Temporal/TemporalOptions.cs` | 90 | 0 | 0 | 0 | 0 | One default property |
| `Temporal/TemporalTenantGate.cs` | 90 | 0 | 0 | 0 | 80 | Every branch (not multi-tenant, option off, no tenant, header missing, header equal, header different) is unit tested over a substituted session; the integration test proves the real header read |
| `Temporal/MartenTemporalRepository.cs` | 30 | 60 | 60 | 0 | 85 | Behavior needs a real stream, covered by the Marten integration tests; guard covers constructor and method arguments; the contract test runs the shared assertions |
| `Temporal/SnapshotAwareTemporalRepository.cs` | 30 | 60 | 60 | 0 | 85 | Same as above, plus the snapshot-then-replay path against a real snapshot store |
| `Temporal/Diagnostics/TemporalDiagnostics.cs` | 80 | 0 | 0 | 0 | 0 | Instruments reached by the listener test; internal static, no validated parameter |
| `Temporal/TemporalLog.cs` | 0 | 0 | 0 | 0 | 0 | Source-generated `[LoggerMessage]` partial class; the generated bodies are exercised through the integration tests' logger |
| `Encina/Diagnostics/EventIdRanges.cs` | 0 | 0 | 0 | 0 | 0 | Constants only; checked by the allocation and registry agreement tests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #314 - temporal queries for event-sourced aggregates in Encina.Marten - in five small pull requests
(T1 to T5), in order. Plan: docs/plans/temporal-queries-implementation-plan-314.md.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no [Obsolete], no compatibility layers. Edit with Edit/Write only; PublicAPI lines, manifests and
  changelog through mechanical-fixer; the guide and README through docs-writer.
- Encina.Marten has no PublicAPI.Unshipped.txt. Marten 9.38.0 on PostgreSQL; integration tests use the shared MartenFixture collection.
- Do not implement ITemporalProjection or an EventStoreDB repository (see the plan's Design Choice 6 and Next Steps).

IMPLEMENTATION OVERVIEW:
1. T1: ITemporalRepository<T> (GetAtAsync, GetHistoryAsync, GetDiffAsync), TemporalRange, AggregateDiff, TemporalOptions, error codes.
2. T2: MartenTemporalRepository (replay from the stream start), TemporalTenantGate, AddTemporalRepository.
3. T3: SnapshotAwareTemporalRepository (snapshot first, then fromVersion replay), AddSnapshotableTemporalRepository, contract test.
4. T4: TemporalDiagnostics, TemporalLog and the MartenTemporal EventId range 12450-12499 (ids 12450-12455).
5. T5: documentation and changelog fragment.

KEY PATTERNS:
- ROP: every method returns Task<Either<EncinaError, T>>; errors are never swallowed; EncinaError.Message never reaches a log or tag.
- Fail closed: a multi-tenant application with no tenant, or a foreign or unlabelled stream, is denied (InvalidRequest or NotFound).
- ADR-062 categories: Validation (bad range), InvalidRequest (missing tenant), NotFound (no events at the instant), Internal (store failure).
- ADR-054: no _total suffix; no ids in tags; tenant tag only through the #2676 helper.
- Guard tests for every public constructor and method parameter; a nullable parameter that accepts null gets a valid-result test.
- Reads use IQuerySession; never call anything atomic or transactional.

REFERENCE FILES:
- src/Encina.Marten/MartenAggregateRepository.cs, src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs
- src/Encina.Marten/IEventMetadataQuery.cs, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Historical states are immutable per (id, version) and cacheable, but caching is the job of the aggregate cache plan (#697, open); no separate issue because #697 is the named owner |
| 2 | OpenTelemetry | ✅ Phase 4 | `Encina.Marten` source and meter, span and instruments of register row C6; never ids |
| 3 | Structured Logging | ✅ Phase 4 | `TemporalLog` ids 12450-12455 of `MartenTemporal`; error code and exception type only |
| 4 | Health Checks | ❌ N/A | A temporal read adds no dependency: `MartenHealthCheck` already covers the store |
| 5 | Validation | ✅ Phase 1 | `TemporalRange.Validate` and argument checks return `Validation` as `Left`; the issue marked it deferred, the plan integrates it because it is the boundary of a public API |
| 6 | Resilience | ❌ N/A | Single read; the application's resilience pipeline wraps the calling handler, and Marten/Npgsql owns connection retry |
| 7 | Distributed Locks | ❌ N/A | Read-only, no write contention |
| 8 | Transactions | ❌ N/A | Read-only through `IQuerySession`; no unit of work, never `SaveChangesAsync` |
| 9 | Idempotency | ❌ N/A | Queries are idempotent by nature |
| 10 | Multi-Tenancy | ✅ Phase 2 | `TemporalTenantGate` fails closed (Design Choice 3); isolation by construction arrives with #326 |
| 11 | Module Isolation | ❌ N/A | Streams and events carry no module identifier in `Encina.Marten` today, so there is nothing to scope by; the issue marked it included, the maintainer amends it (Next Steps) |
| 12 | Audit Trail | ⏭️ | Reading personal data's history should leave a read-audit record (`IReadAuditStore`, ADR-036); `Encina.Marten` does not reference `Encina.Security.Audit`, so the issue file `artifacts/issues/temporal-read-audit.md` ([DEBT]) proposes an opt-in decorator; the written issue is opened by the orchestrator |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `ISnapshotStore<T>.GetAtVersionAsync` and `FetchStreamAsync(fromVersion:)` | Merged | `src/Encina.Marten/Snapshots/` |
| Marten integration fixture | Merged | `MartenFixture` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Per-tenant Marten sessions | #326 | The header gate stays as defence in depth |
| Tenant tag helper | #2676 | T4 adds the tenant tag when it has merged |
| Snapshot strategies | #323 | Better snapshot density makes T3 faster |
| Aggregate cache | #697 | Historical-state caching |
| Multi-stream temporal projections | #324, #535 | Owners of the deferred `ITemporalProjection` |

---

## Next Steps

1. Link this plan from #314 and ask the maintainer to answer Design Choices 2, 3, 4 and 6 when work starts.
2. Implement T1 to T5 in order (T4 and T5 are independent after T3).
3. Maintainer action: amend the acceptance criteria of #314 that name `GetAsync`, `GetAtVersionAsync`, `ITemporalProjection`, an EventStoreDB path and module isolation to the outcome of this plan (this plan never comments on an issue); when `Encina.EventStoreDB` exists (#576, #585), its owners add an `ITemporalRepository` implementation over the stream read API.
