# Implementation Plan: `DeadLetterStoreMarten` — Marten (PostgreSQL) backend for the persistent dead letter queue

> **Issue**: [#584](https://github.com/dlrivada/Encina/issues/584)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 1 provider, ~16 source files + tests)
> **Estimated Scope**: ~900-1,200 lines of production code + ~1,500-2,000 lines of tests

---

## Summary

Implement `Encina.Messaging.DeadLetter.IDeadLetterStore` on Marten 9 (PostgreSQL, ADR-027) as `DeadLetterStoreMarten`, so an application that already runs Marten for event sourcing keeps its dead letters in the same database, in the same backup and the same operational model, without adding a relational provider. The store implements all 11 methods of the ADR-046 contract (`AddAsync` returning `Right(false)` for a repeated `(SourcePattern, SourceMessageId)`, oldest-first paged reads, exact counts, the atomic replay claim, set-based deletes, expiry from `TimeProvider`), and keeps an append-only, content-free lifecycle history of every dead letter (captured, replay claimed, replayed, deleted) as the issue promises.

What the code looks like today (main `7a0049c07`):

- The contract is `IDeadLetterStore` in `src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`; `DeadLetterFilter`, `IDeadLetterMessage`, `IDeadLetterMessageFactory`, `DeadLetterErrorCodes` and `DeadLetterStoreLimits` sit next to it. The 10 database providers implement it (`DeadLetterStoreADO`, `DeadLetterStoreDapper`, `DeadLetterStoreEF`, `DeadLetterStoreMongoDB`), and `InstrumentedDeadLetterStore` (`Encina.OpenTelemetry`) and `FakeDeadLetterStore` (`Encina.Testing.Fakes`) wrap or fake it. The original samples of the issue (`DeadLetterMessage`, `GetPendingAsync`, `MarkAsReprocessedAsync`, `DeadLetterStatus`, `DateTime.UtcNow`) no longer exist or break `AGENTS.md` section 3 and are replaced here.
- `src/Encina.Marten` holds the aggregate repository, snapshots, versioning and projections; it has no dead letter code. It already references `Encina.Messaging` (`Encina.Marten.csproj`), so the dependency argument of the issue's second dilemma does not hold (see Design Choice 2).
- `src/Encina.Audit.Marten` is the precedent for a Marten-backed store: options with a validator, `IConfigureOptions<StoreOptions>` for document and projection registration, a health check, an `AddEncinaAuditMarten` registration. The integration fixture is `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs` (PostgreSQL 17 through Testcontainers).

**Provider category**: event sourcing (Marten), outside the 10-provider database matrix (`AGENTS.md` section 5), an additional backend of an interface the matrix implements (#583, closed). **Affected packages**: `Encina.Marten` (or a new package per Design Choice 2), `Encina.Messaging` (no change), test projects.

---

## Design Choices

<details>
<summary><strong>1. Lifecycle model — Marten document for the state, event stream for the history (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One event stream per dead letter plus a projected read model** (the original idea) | Full history is the primary record; dashboards read projections; natural fit with Marten | The contract needs `DeleteAsync`, `DeleteManyAsync` and `DeleteExpiredAsync`, which contradict "append only" (stream deletion or tombstone plus purge, and erasure obligations cannot be met by append only); `GetMessagesAsync`, `GetCountAsync` and the atomic claim run on an eventually consistent or inline projection; a projection rebuild is a new failure mode |
| **B) One Marten document per dead letter, no events** | Smallest design; atomic claim, set-based delete and exact counts are plain document operations in one PostgreSQL transaction | Loses the lifecycle history the issue promised |
| **C) Document for the state plus a small, content-free event stream per dead letter for the lifecycle** | Contract operations run on the document (exact, atomic); the history survives as events that carry ids, instants and outcome codes but never the payload, so deleting the document leaves no personal data behind; no projection to rebuild | Two writes per change (in one session, so one transaction); a little more code than B |

### Chosen Option: **C — document for the state, content-free event stream for the history** (recommended, pending the maintainer)

### Rationale

- Option C is the only one that delivers the history without bending the contract: the contract is state-oriented (claim, filter, count, delete) and Marten documents are the natural carrier for state.
- The events hold no `RequestContent`, `ExceptionStackTrace` or `TenantId`-bearing text, so a `DeleteAsync` removes the personal data while the audit trail of what happened (and when) remains.
- The scenario where A wins: dashboards over the full lifecycle of every dead letter with no need to delete (a closed compliance archive). The scenario where B wins: nobody reads the history and one write per change matters.
- No projection exists in this design, so the Marten projection rules of `AGENTS.md` section 3 (dependencies through `IDocumentOperations`, one test registering each projection with a real store) have nothing to apply to; if the maintainer picks A, they become Phase 2 obligations.

</details>

<details>
<summary><strong>2. Package placement — `DeadLetter/` folder in `Encina.Marten` or a separate package (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `src/Encina.Marten/DeadLetter/` in the existing package** | Follows plan #583 Choice 3 (`DeadLetter/` folder in the existing provider packages, decided 2026-10-09); Marten users get the store by configuration; no new package to ship, document and version; `Encina.Marten` already references `Encina.Messaging` | `Encina.Marten` gains a dead letter surface that applications without the queue never use |
| **B) New package `Encina.Marten.DeadLetter`** (satellite, like `Encina.Marten.GDPR`) | Pay-for-what-you-use at package level; own README and release notes | New csproj, manifest, README and INVENTORY entry; the stated reason (avoid an `Encina.Messaging` dependency) is false because `Encina.Marten` already references it |

### Chosen Option: **A — folder in `Encina.Marten`** (recommended, pending the maintainer)

### Rationale

- The dependency argument of the issue is the correction this plan brings: `Encina.Marten.csproj` already references `Encina.Messaging`, so B buys only packaging tidiness.
- A is the established pattern of the 10 database providers and the cheapest to maintain. B wins when the maintainer wants every Marten capability to be an independently versioned opt-in, as `Encina.Audit.Marten` and `Encina.Marten.GDPR` are (they carry crypto and compliance dependencies that this store does not).
- EventIds follow the choice: A uses free ids 2731-2745 inside `EventIdRanges.Marten` (2600-2799, "2701-2799 free for the package"); B takes a new range from the block assigned to this family, 6915-6924.

</details>

<details>
<summary><strong>3. Session scope and concurrency — store-owned session per operation with optimistic revisions (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The store opens a lightweight session per operation from `IDocumentStore`, saves immediately, and uses Marten numeric revisions for the claim** | A failure is a `Left` of the call that caused it; a capture runs in its own transaction as `DeadLetterSourceCapture` requires (ADR-046 decision 11); the claim is Marten's documented optimistic concurrency (`ConcurrencyException` becomes `Right(false)`); `SaveChangesAsync` is a no-op like the Mongo store | Cannot join the application's own Marten transaction |
| **B) The store takes the scoped `IDocumentSession` and writes on `SaveChangesAsync`** (the EF Core shape) | Joins the application's unit of work | A capture in a failing source scope would be rolled back with it or save the source's pending changes (the reason for decision 11); errors surface at `SaveChangesAsync`, not at the operation |
| **C) Raw conditional `UPDATE` on Marten's `mt_doc_*` table for the claim** | One statement, exact row count | Couples the code to Marten's internal table layout and JSON shape |

### Chosen Option: **A — session per operation, optimistic revisions** (recommended, pending the maintainer)

### Rationale

- ADR-046 decision 11 already settled for the other stores that a capture must not share the source's scope; A gives that for free.
- B wins only when a dead letter must commit atomically with the application's own Marten writes (not a dead letter queue requirement today). C wins if profiling shows the revision round trip (load, check, write) is a bottleneck on a hot claim path.
- The claim: load the document, return `Right(false)` when `ReplayedAtUtc` is set or a live claim exists, otherwise set `ReplayClaimedAtUtc` from the store's `TimeProvider` and store with the loaded revision; `ConcurrencyException` means another host won, so `Right(false)`.

</details>

<details>
<summary><strong>4. Contract and semantics — ADR-046, copied exactly</strong></summary>

**Class:** Settled
**Settled by:** ADR-046 (decisions 1, 5, 6, 7), ADR-001, ADR-006, AGENTS.md section 3 (TimeProvider, errors never swallowed)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implement the 11 methods with the ADR-046 semantics (oldest first by `DeadLetteredAtUtc` then `Id`, `Right(false)` on a repeated source key, replay result as a code, "now" from `TimeProvider`)** | Provider coherence: switching provider is a registration change (`AGENTS.md` section 5) | None |
| **B) A reduced Marten-specific contract** | Easier to build | Breaks provider coherence and the manager features |

### Chosen Option: **A — the ADR-046 contract**

### Rationale

- The contract is fixed by ADR-046 and enforced by the shared `DeadLetterStoreContract` suite, which this store must pass unchanged. A store `Left` fails the operation that received it; the stored replay outcome is a code, never error text; `ExceptionMessage` does not exist.
- Argument rules (`skip >= 0`, `1 <= take <= DeadLetterStoreLimits.MaxPageSize`, `messageId != Guid.Empty`, non-blank `replayResult`) throw before any I/O.

</details>

<details>
<summary><strong>5. Tenant, naming and registration</strong></summary>

**Class:** Settled
**Settled by:** ADR-046 Choice 7, SPEC-002 REQ-061, AGENTS.md section 4 and section 3 (registration completeness)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TenantId` as a stored, indexed field; `DeadLetterStoreMarten`; `AddEncinaDeadLetterMarten()` using the shared `AddDeadLetterQueueServices` first-wins registration** | Same behavior as every other store (a query returns every tenant unless the filter names one); naming by `{Pattern}Store{Provider}`; the store wins in any registration order | None |
| **B) Marten conjoined tenancy (session scoped to a tenant)** | Database-enforced isolation | A session sees one tenant, but the contract and the cleanup processor need every tenant when the filter names none; it changes the document identity of every Marten document in the store |

### Chosen Option: **A — stored field, `DeadLetterStoreMarten`, `AddEncinaDeadLetterMarten()`**

### Rationale

- The ADR fixes the tenant semantics (the manager gates, the store is explicit). `AGENTS.md` section 4 fixes the name. The DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` in both registration orders (this store and the queue).
- There is no InMemory store; unit tests mock `IDocumentStore`/`IDocumentSession` with NSubstitute and integration tests run on PostgreSQL through Testcontainers (`AGENTS.md` section 3, event-sourced stores).

</details>

---

## Implementation Phases

### Phase 1: Documents, lifecycle events and mapping

> **Goal**: the persisted shapes, with the unique source key and filter indexes.

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten/DeadLetter/DeadLetterDocument.cs`: public sealed class implementing `IDeadLetterMessage`; `Id` (Guid, Marten identity), every field of the contract; Marten numeric revisions enabled for this document type.
2. `src/Encina.Marten/DeadLetter/DeadLetterDocumentFactory.cs`: `IDeadLetterMessageFactory.Create(DeadLetterData)` returning a `DeadLetterDocument` (same shape as `src/Encina.MongoDB/DeadLetter/DeadLetterMessageFactory.cs`).
3. `src/Encina.Marten/DeadLetter/DeadLetterLifecycleEvents.cs`: `DeadLetterCaptured(Guid Id, string SourcePattern, DateTime DeadLetteredAtUtc)`, `DeadLetterReplayClaimed(Guid Id, DateTime ClaimedAtUtc)`, `DeadLetterReplayed(Guid Id, DateTime ReplayedAtUtc, string ReplayResult)`, `DeadLetterDeleted(Guid Id, DateTime DeletedAtUtc)`. Content-free by rule: no payload, no stack trace, no tenant, no error text.
4. `src/Encina.Marten/DeadLetter/DeadLetterMartenSchema.cs`: `IConfigureOptions<StoreOptions>` registering the document, a unique computed index on `(SourcePattern, SourceMessageId)`, indexes on `DeadLetteredAtUtc`, `TenantId`, `ExpiresAtUtc`, and UTC handling of the `DateTime` fields (stored with `Kind.Utc`, returned with `Kind.Utc`, ADR-046 decision 6).
5. `PublicAPI.Unshipped.txt` lines for every public type.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>).
- Issue #584: a Marten (PostgreSQL) backend of IDeadLetterStore (src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs, contract per ADR-046).
- Folder: src/Encina.Marten/DeadLetter/ (or the new package if the maintainer chose option B of Design Choice 2).

TASK:
Create DeadLetterDocument, DeadLetterDocumentFactory, the four content-free lifecycle events and DeadLetterMartenSchema, as listed in the Phase 1 Tasks.

KEY RULES:
- The document implements every member of IDeadLetterMessage (including IsReplayed and IsExpiredAt(DateTime utcNow)); IsExpiredAt never reads the system clock.
- Events carry ids, UTC instants and outcome codes only; never RequestContent, ExceptionStackTrace, TenantId or error text.
- DateTime fields are stored and returned with DateTimeKind.Utc.
- Names: {Pattern}Store{Provider} for the store; no [Obsolete], no compatibility layer; XML docs on every public type.

REFERENCE FILES:
- src/Encina.MongoDB/DeadLetter/DeadLetterMessage.cs and DeadLetterMessageFactory.cs
- src/Encina.Audit.Marten/Projections/ConfigureMartenOperationAuditProjections.cs (IConfigureOptions<StoreOptions>)
- src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs
```

</details>

---

### Phase 2: `DeadLetterStoreMarten`

> **Goal**: the 11 contract methods on Marten.

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten/DeadLetter/DeadLetterStoreMarten.cs`: `sealed class DeadLetterStoreMarten : IDeadLetterStore`; constructor `(IDocumentStore store, TimeProvider timeProvider, ILogger<DeadLetterStoreMarten> logger, IOptions<DeadLetterMartenOptions>? options = null)`.
2. `AddAsync`: open a lightweight session, check the source key, insert the document and (when `RecordLifecycleEvents`) append `DeadLetterCaptured` in the same `SaveChangesAsync`; the unique index violation (`PostgresException`, SQLSTATE 23505) or the pre-check hit maps to `Right(false)`. Any other exception maps to `Left` with `DeadLetterErrorCodes.StoreFailed`; the exception type is logged, never its message.
3. `GetAsync`, `GetMessagesAsync` (Marten LINQ on the indexed fields, order `DeadLetteredAtUtc` then `Id`, `newestFirst` reverses both, paging by `Skip`/`Take`), `GetCountAsync` (`CountAsync` on the same predicate), a private `ApplyFilter(IQueryable<DeadLetterDocument>, DeadLetterFilter)` covering every filter field (`SourcePattern`, `RequestType`, `ErrorCode`, `CorrelationId`, `ExcludeReplayed`, the two instants, `TenantId`, `SourceMessageId`, `ExpiresAtOrBeforeUtc`).
4. `TryClaimForReplayAsync`: per Design Choice 3 (revision check, `ConcurrencyException` to `Right(false)`); `MarkAsReplayedAsync`: updates only while `ReplayedAtUtc` is null; both append their lifecycle event.
5. `DeleteAsync`, `DeleteManyAsync(filter)` (Marten `DeleteWhere` with the same predicate, returning the count through a count-then-delete in one session), `DeleteExpiredAsync` (`ExpiresAtUtc <= timeProvider.GetUtcNow()`), `SaveChangesAsync` (returns `Right(Unit)`; every operation already saved). Deletes append `DeadLetterDeleted` per removed id when events are on.
6. Cancellation token on every call; no synchronous Marten API; `ArgumentException` rules before I/O.
7. Cyclomatic complexity of every method at most 10 or CRAP at most 10 with its tests: split `ApplyFilter` into one small method per predicate group.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 created DeadLetterDocument, the lifecycle events and the schema registration.
- The store contract is src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs; the shared behavior suite is tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs.

TASK:
Implement DeadLetterStoreMarten with all 11 methods as described in the Phase 2 Tasks.

KEY RULES:
- Every method returns Either<EncinaError, T>; a Marten or Npgsql failure is a Left (DeadLetterErrorCodes.*), never a swallowed success.
- "Now" and expiry come from the injected TimeProvider; production code never reads DateTime.UtcNow.
- Log by error code or exception type; never exception messages (AGENTS.md section 3).
- The unique source key gives Right(false) for a duplicate; the claim is optimistic-revision based; the replay result stored is a code.
- Keep every method at CRAP <= 10 (split filter application).

REFERENCE FILES:
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterStoreEF.cs
- src/Encina.Marten/MartenAggregateRepository.cs (session handling, ConcurrencyException mapping)
```

</details>

---

### Phase 3: Configuration, DI and registration

> **Goal**: `AddEncinaDeadLetterMarten()` and a health check, proven by a DI test.

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten/DeadLetter/DeadLetterMartenOptions.cs`: `RecordLifecycleEvents` (default `true`), `AddHealthCheck` (default `false`, pay-for-what-you-use).
2. `src/Encina.Marten/DeadLetter/DeadLetterMartenServiceCollectionExtensions.cs`: `AddEncinaDeadLetterMarten(this IServiceCollection, Action<DeadLetterMartenOptions>? configure = null, Action<DeadLetterOptions>? configureQueue = null)`: registers the options, the schema configurator (`TryAddEnumerable`), `TimeProvider` (`TryAddSingleton`), and delegates to `AddDeadLetterQueueServices<DeadLetterStoreMarten, DeadLetterDocumentFactory>(true, options)` so the store, factory, orchestrator, manager and source capture are registered first-wins in any order.
3. Health: reuse the existing `MartenHealthCheck` of the package when registered; add a dead letter table probe only if the existing check does not cover the document schema.
4. DI test: build the provider with `ValidateOnBuild` and `ValidateScopes`, resolve `IDeadLetterStore`, `IDeadLetterManager`, `DeadLetterOrchestrator`; assert the store wins when `AddEncinaDeadLetterQueue<TStore, TFactory>` is called before and after, and that an application's own `IDeadLetterStore` registration is kept.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Phase 2 produced DeadLetterStoreMarten. Registration of the queue is shared: src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs and AddDeadLetterQueueServices (first registration wins via TryAdd).

TASK:
Create DeadLetterMartenOptions and AddEncinaDeadLetterMarten as described in the Phase 3 Tasks, plus the DI test.

KEY RULES:
- Registration completeness (AGENTS.md section 3): register every option type and dependency the store resolves; the test builds with ValidateOnBuild and ValidateScopes.
- Satellite registration is called before the core queue registration and never overrides the application's own IDeadLetterStore.
- Every feature is opt-in; nothing is registered unless this method is called.

REFERENCE FILES:
- src/Encina.MongoDB/ServiceCollectionExtensions.cs (AddDeadLetterQueueServices call)
- src/Encina.Audit.Marten/ServiceCollectionExtensions.cs
- tests/Encina.GuardTests/Marten/Core/MartenServiceCollectionExtensionsGuardTests.cs
```

</details>

---

### Phase 4: Cross-cutting integration and observability

> **Goal**: logging, tracing through the existing decorator, and the EventIds.

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten/DeadLetter/DeadLetterMartenLog.cs`: `[LoggerMessage]` source-generated messages (store failure by operation, claim lost, duplicate capture, delete count), ids 2731-2745 inside `EventIdRanges.Marten` (the range comment says 2701-2799 are free), packed sequentially; update the range comment in `src/Encina/Diagnostics/EventIdRanges.cs` to name them. If Design Choice 2 picks a separate package, register `DeadLetterMarten = (6915, 6924)` first (the block assigned to this family) and add the assembly to `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
2. OpenTelemetry: no code; `InstrumentedDeadLetterStore` (Encina.OpenTelemetry) decorates any registered store; document it in the README and add a test that the decorator wraps `DeadLetterStoreMarten`.
3. Tenant: no `TenantId` in logs or tags (SPEC-002 REQ-062).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- Logging uses the [LoggerMessage] source generator with EventIds registered in src/Encina/Diagnostics/EventIdRanges.cs (ADR-021).

TASK:
Add DeadLetterMartenLog with the ids named in the Phase 4 Tasks, wire it into DeadLetterStoreMarten, and add the architecture test entry if a new range is used.

KEY RULES:
- Register the range before use; never assign an id outside it; pack ids sequentially.
- Log error codes or exception types only; exception text and the tenant id never reach logs.
- XML docs name the range ("Event IDs: 2731-2745 (see EventIdRanges.Marten)").

REFERENCE FILES:
- src/Encina.Marten/Log.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every applicable flag reaches its target from `.github/coverage-manifest/Encina.Marten.json` (new entries for each new file, with one-sentence justifications).

<details>
<summary>Tasks</summary>

1. **Unit** (`tests/Encina.UnitTests/Marten/DeadLetter/`): filter translation, mapping, error mapping to `Left`, claim outcomes (`ConcurrencyException`), event content (no payload), with NSubstitute for the Marten session and a `FakeTimeProvider`.
2. **Guard** (`tests/Encina.GuardTests/Marten/DeadLetter/`): null and range checks for every public method and for `AddEncinaDeadLetterMarten`.
3. **Contract** (`tests/Encina.ContractTests/Messaging/DeadLetter/`): derive from `DeadLetterStoreContract` against Marten (needs a database, so the derived class runs in `Encina.IntegrationTests`, as the other persistent stores do).
4. **Integration** (`tests/Encina.IntegrationTests/Infrastructure/Marten/DeadLetter/`): a shared `[Collection]` Marten fixture, `ClearAllDataAsync` in `InitializeAsync`; cases: duplicate source key, concurrent claim from two stores, expiry boundary, tenant filter, delete, lifecycle events present after delete without payload.
5. **Property** (`tests/Encina.PropertyTests/Messaging/DeadLetter/`): filter translation agrees with an in-memory oracle for random filters; order invariant.
6. **Load** (justification file unless the maintainer wants it): concurrent `TryClaimForReplayAsync` is covered by the integration test; **Benchmark**: `.md` justification (not a hot path), per `AGENTS.md` section 9.
7. Report the measured per-flag coverage of each new file against its manifest target in the PR, and the CRAP table of the touched methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Tests must execute real package code. Event-sourced stores have no InMemory variant: unit tests mock the Marten session, integration tests use Marten on PostgreSQL via Testcontainers (AGENTS.md section 3).

TASK:
Write the tests listed in the Phase 5 Tasks and add per-file targets with justifications to .github/coverage-manifest/Encina.Marten.json.

KEY RULES:
- Assertions use Shouldly through Encina.Testing.Shouldly; never FluentAssertions.
- Integration tests use a shared [Collection] fixture, never IClassFixture; call ClearAllDataAsync in InitializeAsync.
- No Thread.Sleep; time comes from FakeTimeProvider / the controllable clock of the contract base.
- Targets never go down; justify each target in one sentence.

REFERENCE FILES:
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
- tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/DeadLetterStoreMongoDBIntegrationTests.cs
```

</details>

---

### Phase 6: Documentation and finalization

> **Goal**: user documentation and the release records.

<details>
<summary>Tasks</summary>

1. XML docs on every public API (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`).
2. `changelog.d/584-marten-dead-letter-store.added.md` (a fragment, never `CHANGELOG.md`).
3. Package README of `Encina.Marten` (or the new package) and the dead letter feature page `docs/features/dead-letter-queue.md` (a "Marten" section); `docs/INVENTORY.md` and `ROADMAP.md` if affected.
4. ADR: none new unless Design Choice 1 or 3 is answered in a way that changes ADR-046; ADR number assigned when the ADR pull request opens.
5. `PublicAPI.Unshipped.txt` complete; build with 0 warnings; `dotnet test` per flag.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Documentation of this feature is split between the package README, docs/features/dead-letter-queue.md and the changelog fragment.

TASK:
Complete the documentation tasks of Phase 6 and verify the build.

KEY RULES:
- Never edit CHANGELOG.md; add changelog.d/584-marten-dead-letter-store.added.md.
- Documentation never types coverage figures by hand (covref markers).
- English only; no emojis; the reference application is never named.

REFERENCE FILES:
- docs/features/dead-letter-queue.md
- src/Encina.Audit.Marten/README.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Topic | Requirement |
|--------|-------|-------------|
| SPEC-000 DEC-008, REQ-030 | Dead letter queue in 1.0 | #584-#589 are additional backends (milestone v0.19.0) |
| ADR-046 | Persistent dead letter queue | Oldest-first order, `Right(false)` on a repeated source key, atomic claim, set-based deletes, `TimeProvider`, `TenantId` field |
| ADR-027, ADR-019 | Marten as the event sourcing provider | Marten on PostgreSQL; no InMemory event-sourced stores |
| SPEC-002 REQ-061, REQ-062 | Tenant isolation | Tenant is a filter; telemetry never carries the tenant id |
| AGENTS.md section 3 | Marten rule | Projections take `IDocumentOperations`, never `IServiceProvider`; at least one test registers each projection with a real store |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in a new IDeadLetterStore backend |
|-----------|----------|------------------------------------------|
| `IDeadLetterStore` | `src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs` | The Railway Oriented Programming contract this backend implements |
| `IDeadLetterMessage` | `src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs` | The record the Marten document implements |
| `DeadLetterData` and `IDeadLetterMessageFactory` | `src/Encina.Messaging/DeadLetter/IDeadLetterMessageFactory.cs` | Input and factory contract for the backend's own message type |
| `DeadLetterFilter` | `src/Encina.Messaging/DeadLetter/DeadLetterFilter.cs` | Filter fields translated to Marten LINQ predicates |
| `DeadLetterErrorCodes`, `DeadLetterStoreLimits` | `src/Encina.Messaging/DeadLetter/` | Error codes for `Left` values and the page size and result length limits |
| `AddEncinaDeadLetterQueue` / `AddDeadLetterQueueServices` | `src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs` | First-wins registration of the store, factory, orchestrator, manager |
| `InstrumentedDeadLetterStore` | `src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs` | Adds tracing and metrics around any store without store code |
| `DeadLetterStoreContract` | `tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs` | Shared behavior suite the Marten store inherits |
| `DeadLetterStoreMongoDB` | `src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs` | Closest reference: document store, session-less writes, `TimeProvider` |
| `MartenFixture` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs` | PostgreSQL through Testcontainers for integration tests |
| `AddEncinaAuditMarten` | `src/Encina.Audit.Marten/ServiceCollectionExtensions.cs` | Precedent for Marten schema configuration and registration |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` | 2731-2745 (inside `Marten` 2600-2799) | Used if Design Choice 2 keeps the store in `Encina.Marten`; 2701-2799 are free per `EventIdRanges.cs` |
| `Encina.Marten.DeadLetter` | 6915-6924 (`DeadLetterMarten`) | Used only if Design Choice 2 picks a separate package; block 6900-6949 is this family's |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production code (Phases 1-4) | ~10 | Document, factory, events, schema, store, options, registration, log, `PublicAPI` |
| Unit and guard tests | ~5 | Store, filter, registration |
| Contract and integration tests | ~3 | Derived contract class, integration cases, fixture collection |
| Property tests | 1 | Filter oracle |
| Manifest, README, feature page, changelog | ~4 | Documentation phase |
| **Total** | **~23** | Exceeds the 15-file limit of one worker pull request. Proposed split: PR 1 = Phases 1-4 (production, ~10 files, plus the unit and guard tests of the store, ~15 files); PR 2 = Phases 5-6 remainder (contract/integration/property tests, manifest, README, feature page, changelog) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution first.
- Railway Oriented Programming: Either<EncinaError, T> on every store method; errors are never swallowed; TimeProvider for all time; async with CancellationToken only.
- Issue #584: DeadLetterStoreMarten implements IDeadLetterStore (ADR-046) on Marten 9 / PostgreSQL.

IMPLEMENTATION OVERVIEW:
Phase 1: DeadLetterDocument, DeadLetterDocumentFactory, content-free lifecycle events, schema with unique (SourcePattern, SourceMessageId) index.
Phase 2: DeadLetterStoreMarten, 11 methods, session per operation, optimistic-revision claim.
Phase 3: DeadLetterMartenOptions, AddEncinaDeadLetterMarten delegating to AddDeadLetterQueueServices, DI test with ValidateOnBuild and ValidateScopes.
Phase 4: DeadLetterMartenLog with EventIds registered first (2731-2745 or 6915-6924 per the maintainer's package choice).
Phase 5: unit, guard, contract/integration (DeadLetterStoreContract), property tests; manifest targets.
Phase 6: docs, changelog fragment, PublicAPI.

KEY PATTERNS:
- Store naming DeadLetterStoreMarten; satellite DI AddEncinaDeadLetterMarten() with first-wins registration.
- Lifecycle events never carry payload, stack trace, tenant or error text.
- Event-sourced stores have no InMemory variant; integration tests use the Marten/PostgreSQL fixture in a shared [Collection].
- Dilemmas D1-D3 are recommended, pending the maintainer: do not start Phase 1 before they are answered.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/ (contract and registration)
- src/Encina.MongoDB/DeadLetter/ (closest implementation)
- src/Encina.Audit.Marten/ (Marten store and registration precedent)
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
- docs/architecture/adr/046-persistent-dead-letter-queue.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Dead letters are an operator tool and are not cached; the original "cache read models" is dropped. |
| 2 | OpenTelemetry | ✅ | `InstrumentedDeadLetterStore` decorates any store; Phase 4 adds a test that it wraps this one. |
| 3 | Structured logging | ✅ | Phase 4: `[LoggerMessage]` ids 2731-2745 (or 6915-6924), error codes and exception types only. |
| 4 | Health checks | ✅ | Phase 3 reuses `MartenHealthCheck` and adds a document-schema probe only if needed. |
| 5 | Validation | ✅ | Contract argument rules throw before I/O in the store; input rules live once in the orchestrator and manager (ADR-046 decision 6). |
| 6 | Resilience | ✅ | A Marten or Npgsql failure maps to `Left` (`dlq.store_failed`); retry belongs to the caller, as for the other stores. |
| 7 | Distributed locks | ❌ | `TryClaimForReplayAsync` is the atomic claim (optimistic revision); no lock is needed. |
| 8 | Transactions | ✅ | Each operation is one Marten session and one PostgreSQL transaction (document and lifecycle event together). |
| 9 | Idempotency | ✅ | Unique `(SourcePattern, SourceMessageId)`; a repeated capture returns `Right(false)`. |
| 10 | Multi-tenancy | ✅ | `TenantId` stored and filterable (ADR-046 Choice 7); the manager gates the ambient tenant. |
| 11 | Module isolation | ❌ | `IDeadLetterMessage` has no `ModuleId`; the store follows the record if it gains one. |
| 12 | Audit trail | ⏭️ | The lifecycle events are the store's own history; operator replay and delete audit is #2013 (issue exists). |

---

## Prerequisites & Dependencies

- #583 (closed): the contract and the shared contract suite exist. #2013 (open) adds `ActorId` and `CausationId` to the record later; the document gains them then.
- Marten 9.38.0 and `Testcontainers.PostgreSql` 4.15.0 are already pinned in `Directory.Packages.props`; no new package is needed.
- No decision of another issue blocks this plan; its own Dilemmas D1-D3 block Phase 1.

---

## Next Steps

1. The maintainer answers D1, D2 and D3 when work on #584 starts; the answers go into `## Maintainer Decisions`.
2. Re-check each Design Choice against main and the open issues; `adversarial-reviewer` verifies the Settled choices.
3. Brief a worker for Phases 1-4 (production), then Phases 5-6 as a second pull request if the 15-file limit requires it.
4. Final commit references `Fixes #584`.
