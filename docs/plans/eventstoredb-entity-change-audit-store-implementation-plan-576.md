# Implementation Plan: `EntityChangeAuditStoreEventStoreDB` — the entity-change audit store on EventStoreDB

> **Issue**: [#576](https://github.com/dlrivada/Encina/issues/576)
> **Type**: Feature
> **Complexity**: Medium-High (6 phases, 1 shared package, 5 pull requests)
> **Estimated Scope**: ~800 lines of production code + ~1,500 lines of tests

---

## Summary

Implement the entity-change audit store (today `IAuditLogStore`, becoming `IEntityChangeAuditStore` under ADR-036, work item #1778) on EventStoreDB as `EntityChangeAuditStoreEventStoreDB`, in the `Encina.EventStoreDB` package that the plan of #585 creates. The issue's sample is replaced where it does not hold: it says EventStoreDB "is already integrated in Encina" (it is not: no package and no client use exist), appends with `Uuid.NewUuid()` so a retry writes a duplicate, builds the stream name `audit-{EntityType}-{EntityId}` (not injective, no tenant, and EventStoreDB derives the category from the text before the first hyphen), serializes the whole entry in clear into an immutable event although old and new values are personal data that must be removable at the end of retention, and lists "built-in retention policies" without saying what they remove and when.

What the code looks like today (`origin/main` `4b0a600c7`):

- `src/Encina.EventStoreDB` does not exist. `Directory.Packages.props` pins `EventStore.Client.Grpc.Streams` 23.3.9 and no `Testcontainers.EventStoreDb`; `docker-compose.yml` has no EventStoreDB service. The #585 plan (dead letters, open) creates the package skeleton as its own first pull request and states that #576 depends on it; this plan is written on that basis.
- `IAuditLogStore` is in `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs` (`LogAsync`, `GetHistoryAsync(entityType, entityId)`); ADR-036 reshapes it (`Either`, `TimeProvider`, `TenantId`, `ModuleId`, `PurgeEntriesAsync`, redaction hook, `ChangedAtUtc` as `DateTimeOffset`) and moves it to `Encina.Security.Audit` as work item #1778 (open). The sibling backends #577-#582 are built on the same contract.
- **Two accepted ADRs point the other way.** ADR-027 says `Encina.EventStoreDB` is deprecated and excluded from every new feature, and ADR-036 decision 6 says no Marten (event-sourced) implementation of the entity-change store is part of 1.0. SPEC-000 DEC-009 (2026-10-23) made "everything except AI/LLM and cloud services without an emulator" part of 1.0 and brought the EventStoreDB dead letter store (#585) in explicitly; section 6 does not list #576. Design Choice 1 asks the maintainer to reconcile them.
- Marten already has an encrypted, shreddable entity audit path in `Encina.Audit.Marten` (#575 plan); EventStoreDB has no key store, which shapes Design Choice 4.

**Provider category**: event sourcing (`AGENTS.md` section 5: "EventStoreDB future"), outside the 10-provider database matrix; ADR-019: event-sourced stores have no InMemory variant, unit tests mock the client and integration tests run on a container. **Affected packages**: `Encina.EventStoreDB` (new, from #585), `Encina` (EventId registry), test projects, `Directory.Packages.props` (shared pin with #585).

---

## Design Choices

<details>
<summary><strong>1. Does this store go ahead given ADR-027 and ADR-036 decision 6? (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implement it on the shared package of #585 and amend ADR-027 and ADR-036 decision 6 for the entity-change store in the pull request that opens the ADR** | Follows SPEC-000 DEC-009 (anything not in section 6 is in 1.0) and the milestone v0.19.0; consistent with the #585 plan, which takes the same position | Two accepted ADRs are amended; a deprecated package gains a second capability |
| **B) Keep ADR-027 and ADR-036 decision 6: park #576 and #585 in "Post-1.0: Technical exclusions (ADR/SPEC)" and list them in SPEC-000 section 6.2** | Honors both ADRs; no code for a deprecated backend | Reverses DEC-009 for #585; a SPEC-000 amendment |
| **C) Implement it and leave both ADRs unchanged** | Fastest | The code and two accepted ADRs disagree |

### Chosen Option: **A — implement and amend** (recommended, pending the maintainer)

### Rationale

- A wins if the maintainer's DEC-009 direction ("everything except AI/LLM and no-emulator cloud") holds for EventStoreDB; B wins if ADR-027's "no new capability" is meant literally. The decision is the same for #585, #576 and the Marten entity-change store (#575), so it should be taken once. Under B nothing in this plan is built.

</details>

<details>
<summary><strong>2. Package and the shared skeleton (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.EventStoreDB`, skeleton owned by #585 as its first pull request; this plan adds an `EntityChangeAudit/` feature folder (`AGENTS.md` section 4)** | One client registration, one health check, one pin; no two pull requests create the same csproj | #576 waits for the skeleton |
| **B) The same package with #576 owning the skeleton** | #576 has the lower number | #585 waits; the skeleton is shaped by the audit store |
| **C) A separate `Encina.Security.Audit.EventStoreDB`** | Independent | Two packages with two client registrations over one server |

### Chosen Option: **A — the shared package, skeleton from #585** (recommended, pending the maintainer)

### Rationale

- A wins on cost; C wins if every capability must be an independent opt-in package. The decision is the one #585 asks (its D4); the two plans must be answered together.

</details>

<details>
<summary><strong>3. Stream identity, tenant and idempotent write (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Stream `entitychange.{hex(tenant)}-{b64url(type)}.{b64url(id)}`; the event id is the entry id (`Uuid.FromGuid`), appended with `StreamState.Any`; the tenant is the entry's `TenantId`, the reserved `default` when tenancy is off; with tenancy on and no tenant, write and read deny (`InvalidRequest`, ADR-062 R3), never falling back to `default`; a real tenant `default` is rejected on write** | Injective (hex has no `-`, `.` is outside the base64url alphabet, so the category `entitychange.{hex(tenant)}` is exactly one tenant and no two triples collide); a retry of the same entry id is deduplicated by the server within its duplicate-detection window (a cache of recent event ids, not unbounded; to be verified in Phase 0, including how long the window lasts); one category per tenant for the purge | A different payload under an existing entry id is not detected (the server deduplicates by event id only), and a retry after the window appends a second event: entry-id uniqueness is the writer's guarantee, as in #578, unless Phase 0 (a) shows the window too short, in which case the store reads the stream's last event id before appending; stream names are longer |
| **B) The issue's `audit-{EntityType}-{EntityId}` with `Uuid.NewUuid()`** | As written | Not injective; no tenant; every retry appends a duplicate; the category would be `audit` for every tenant |
| **C) One stream per tenant** | Few streams | Reading one entity's history means scanning the tenant's whole stream |

### Chosen Option: **A — injective tenant-qualified stream, deterministic event id** (recommended, pending the maintainer)

### Rationale

- A wins for entity history (one stream read, newest first) and for per-tenant purge (one category); C wins only for tiny catalogs. An entry id that is not a GUID is a `Left` (`InvalidRequest`); the EF writer builds `Guid.NewGuid().ToString()` ids. A stream name over the server's limit is a `Left` before any I/O (Phase 0 records the limit).

</details>

<details>
<summary><strong>4. Content, retention and what "erased" means (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) JSON event data after the writer's redaction hook; `PurgeEntriesAsync(tenant, cutoff)` reads the tenant's category stream, and per entity stream sets `TruncateBefore` to the first event at or after the cutoff in the stream metadata, or soft-deletes the stream when every event is older; events become unreadable at once and are removed physically by the server's scavenge, which the operator schedules (documented)** | Per-tenant retention through the contract; counted, logged; no key store needed | Physical removal waits for scavenge; until then the bytes exist on disk; the redaction hook is the only protection of values at rest |
| **B) `$maxAge` in each stream's metadata, set from the tenant's retention when the stream is first written** | Server-enforced, no purge job | A later change of retention does not reach existing streams; no count of purged entries; the contract's `PurgeEntriesAsync` has nothing to do |
| **C) Field-encrypt old and new values and destroy keys at the end of retention** | Immediate and physical erasure in effect, as in `Encina.Audit.Marten` | EventStoreDB has no key store: a new `ITemporalKeyProvider` implementation and its storage would be built for a deprecated backend |

### Chosen Option: **A — truncate or soft-delete through the purge, scavenge documented** (recommended, pending the maintainer)

### Rationale

- A wins when the redaction hook keeps personal data out of the values (the contract's premise) and the operator runs scavenge; C wins if old and new values must be unreadable even in a disk backup, which needs the key store. Phase 0 verifies that truncated and soft-deleted events are not readable and that the stream can be recreated.

</details>

<details>
<summary><strong>5. Queries and the extended capabilities of the issue (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The contract's queries only: entity history by one stream read; tenant-wide queries (by time range, by user) through the tenant's category stream with a scan cap (`MaxScanEvents`, a `Left` over the cap, never a truncated page), as #585 plans; plus an optional `EntityChangeAuditSubscriber` (a catch-up subscription on the tenant category, the caller keeps the checkpoint)** | Meets the issue's subscription criterion; exact; bounded cost | `GetUserActivityAsync` as written (a scan across all streams) is not offered |
| **B) All three extended methods of the issue** | Complete | `GetUserActivityAsync` and `GetHistoryPagedAsync` need an index that EventStoreDB does not have; a user query would read the whole database |
| **C) Only the contract, no subscription** | Smallest | Misses a stated acceptance criterion |

### Chosen Option: **A — contract queries, bounded scans, optional subscriber** (recommended, pending the maintainer)

### Rationale

- A wins on bounded cost; B wins if a full scan per user query is acceptable for small installations, which the scan cap already allows when raised. The category streams need the `$by_category` system projection (an operational dependency, checked at startup and by the health check: not running is a `Left`, fail closed), the same dependency as #585. The subscriber isolates handler exceptions per event and never lets one end the subscription.

</details>

<details>
<summary><strong>6. Contract, naming and failure semantics</strong></summary>

**Class:** Settled
**Settled by:** ADR-036 (decisions 1 and 7, naming tables), ADR-001, ADR-006, ADR-062 (categories), `AGENTS.md` section 3 (`TimeProvider`; fail closed; errors never swallowed) and section 4 (`{Pattern}Store{Provider}`), ADR-019 (event-sourced stores have no InMemory variant)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The ADR-036 contract on the class `EntityChangeAuditStoreEventStoreDB`: `Either` results, a failed write is a `Left` that fails the unit of work (`Unavailable`, `Timeout` or `UpstreamFailure` per ADR-062; no server message in the `Left`, a log or a span), cancellation propagates** | Provider coherence; fail-closed auditing | Depends on #1778 |
| **B) The old `IAuditLogStore` shape** | Buildable today | Removed by ADR-036 with no compatibility layer (decision 7) |

### Chosen Option: **A — the ADR-036 contract**

### Rationale

- ADR-036 says follow-up issues apply the names and do not revisit them. The error codes are a separate Trivial entry (Design Choice 7); this choice settles only the contract.

</details>

<details>
<summary><strong>7. Error codes of the store</strong></summary>

**Class:** Trivial
**Settled by:** ADR-062 (every code is created with `EncinaErrors.Create`, a named `ErrorCategory` and a constant message; the code prefix is the family prefix) and the sibling plans of the same contract, which use `entity_change_audit.<backend>.*` (#578 `entity_change_audit.azure_table.write_failed`, #582 `entity_change_audit.cosmos.*`, #579 `entity_change_audit.dynamodb.*`), not `entitychange.*`, so dashboards over the one contract share a prefix

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `entity_change_audit.eventstoredb.*`, listed in `EntityChangeAudit/EntityChangeAuditErrors.cs` (new, PR 1)** | Same family prefix as the siblings | None |
| **B) `entitychange.*` (the first draft)** | Shorter | A third scheme for one contract next to `entity_change_audit.*` and Marten's `audit.marten.*`: fragments dashboards and alerts |

### Chosen Option: **A**

### Rationale

- Codes and categories (all exist in `ErrorCategory`): `entity_change_audit.eventstoredb.unavailable` (`Unavailable`), `entity_change_audit.eventstoredb.timeout` (`Timeout`), `entity_change_audit.eventstoredb.entry_id_invalid`, `entity_change_audit.eventstoredb.stream_name_too_long` and `entity_change_audit.eventstoredb.tenant_required` (`InvalidRequest`), `entity_change_audit.eventstoredb.projection_required` (`FailedPrecondition`: `$by_category` not running).

</details>

---

## Implementation Phases

### Phase 0: Server verification (no pull request)

<details>
<summary>Tasks</summary>

1. With the vendor LTS EventStoreDB image started locally (a throwaway script, nothing committed), verify and record: (a) appending the same event id twice with `StreamState.Any` leaves one event; (b) the stream name limit and that the category of `entitychange.{hex}-{b64}.{b64}` is `entitychange.{hex}`; (c) `TruncateBefore` and soft delete make events unreadable at once and a deleted stream can be written again; (d) `$by_category` can be enabled and category reads resolve links; (e) the time a category stream lags behind a write.
2. A failed point (a) changes Design Choice 3 (the writer would then check the last event id); a failed (c) changes Design Choice 4. Report the table; land nothing.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 0</strong></summary>

```
CONTEXT:
- Design Choices 3 and 4 rest on five server behaviors that are assumed, not yet observed.

TASK:
Start the vendor LTS EventStoreDB image locally and verify the five points with a throwaway script; report a pass or fail table with the commands used.

KEY RULES:
- Commit nothing; the script is not a test.
- Record the exact server version and image tag.

REFERENCE FILES:
- docs/plans/eventstoredb-dead-letter-store-implementation-plan-585.md (by issue number: #585, its Phase 0)
```

</details>

---

### Phase 1: Entry mapping and stream names (pull request 1)

<details>
<summary>Tasks</summary>

1. `EntityChangeAudit/Internal/EntityChangeStreamNames.cs` (new, internal): stream and category builders of Design Choice 3, the tenant rule and the length check.
2. `EntityChangeAudit/Internal/EntityChangeEventData.cs` (new, internal): JSON mapping of the contract's entry to event data and back (`System.Text.Json`, camel case, ISO 8601 `DateTimeOffset`), event type `EntityChangeAuditEntry`, the entry id as `Uuid`.
3. `EntityChangeAudit/EntityChangeAuditEventStoreDBOptions.cs` (new): `MaxScanEvents` (default 100,000), `RequireProjection` (default `true`); validated.
4. `EntityChangeAudit/EntityChangeAuditErrors.cs` (new): the six `entity_change_audit.eventstoredb.*` codes of Design Choice 7 with their ADR-062 categories and constant messages.
5. Tests: `EntityChangeStreamNamesTests` (colliding triples, hyphens, dots and `%` in ids, reserved tenant, over-long name), `EntityChangeEventDataTests` (round trip, bad id), options guard tests; `PublicAPI.Unshipped.txt` (the errors class, options, names are covered); `.github/coverage-manifest/Encina.EventStoreDB.json` (edit).

Files: 9 (names, event data, options, errors, 3 test files, PublicAPI, manifest). PR 1 depends on the #585 skeleton (the csproj, the manifest `Encina.EventStoreDB.json`, the `Encina.slnx` row do not exist before it) and on #1778 (`EntityChangeAuditEntry`): it does not build alone before both merge.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- The Encina.EventStoreDB skeleton from #585 exists; #1778 provides the entry type.

TASK:
Add the stream name builders, the event mapping and the options, with tests.

KEY RULES:
- Tenant segment is hex, type and id are base64url joined by a dot; the category is the text before the first hyphen.
- An entry id that is not a GUID is a Left (InvalidRequest); names over the server limit are a Left before any I/O.
- Either<EncinaError, T>; constant messages; XML docs; PublicAPI lines; numeric per-flag manifest targets.

REFERENCE FILES:
- src/Encina.EventStoreDB (the skeleton of #585)
- docs/plans/azure-table-entity-change-audit-store-implementation-plan-578.md (by issue number: #578)
```

</details>

---

### Phase 2: The store and its registration (pull request 2)

<details>
<summary>Tasks</summary>

1. `EntityChangeAudit/EntityChangeAuditStoreEventStoreDB.cs` (new): implements `IEntityChangeAuditStore`; constructor `(EventStoreClient client, IOptions<EntityChangeAuditEventStoreDBOptions> options, ILogger<...> logger, TimeProvider? timeProvider = null)`; write (`AppendToStreamAsync`), history (`ReadStreamAsync` backwards), tenant-wide queries by category with the scan cap, every call with the `CancellationToken`; a missing stream is an empty history, any other failure is a `Left`.
2. `ServiceCollectionExtensions.cs` (edit, from #585): `AddEncinaEntityChangeAuditEventStoreDB()`; the store replaces the in-memory default in any registration order and never the application's own registration; it requires the client registered by the package.
3. Tests: `EntityChangeAuditStoreEventStoreDBTests` (substituted client: every `Left`, missing stream, scan cap), registration test with `ValidateOnBuild` and `ValidateScopes`, guard tests, `PublicAPI.Unshipped.txt`, manifest.

Files: 7.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 shipped names, mapping and options.

TASK:
Implement EntityChangeAuditStoreEventStoreDB and its registration.

KEY RULES:
- StreamNotFoundException on a history read is an empty list; other failures are Lefts with the codes of Design Choice 6; no server message in a Left, a log or a span.
- A scan over MaxScanEvents is a Left, never a truncated page.
- Never swallow errors; TimeProvider; exception types only in logs.

REFERENCE FILES:
- src/Encina.EventStoreDB (the skeleton of #585)
- docs/plans/eventstoredb-dead-letter-store-implementation-plan-585.md (by issue number: #585)
```

</details>

---

### Phase 3: Retention and EventIds (pull request 3)

<details>
<summary>Tasks</summary>

1. `PurgeEntriesAsync` in the store (edit) per Design Choice 4: per entity stream under the tenant's category, set `TruncateBefore` or soft-delete, count the entries made unreadable, return the count; the tenant is passed explicitly by the retention service of #1778 and #4182, with no cross-tenant opt-in.
2. `EntityChangeAudit/Diagnostics/EntityChangeAuditEventStoreDBLog.cs` (new, `[LoggerMessage]`); `src/Encina/Diagnostics/EventIdRanges.cs` (edit: `EventStoreDBEntityChangeAudit = (52700, 52749)`), `src/Encina/PublicAPI.Unshipped.txt` (edit), `docs/engineering/eventid-block-registry.md` (edit), `EncinaEventIdAllocationTests.cs` (edit: `AssemblyRanges`).
3. Tests for purge (truncate, soft delete, mixed streams, a failed metadata write) and logs; package `PublicAPI.Unshipped.txt`; manifest.

Files: 10 (store edit, log, 4 registry/API edits, 2 test files, package PublicAPI, manifest). The health check and its registration moved to PR 4 so this pull request stays small.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- The store writes and reads; retention is a per-tenant purge of the tenant's category.

TASK:
Implement PurgeEntriesAsync, the log class and the EventId range.

KEY RULES:
- Register EventStoreDBEntityChangeAudit (52700, 52749) in EventIdRanges.cs before using an id; ids packed from 52700; map the assembly in AssemblyRanges.
- A purge failure is a Left, never success; logs carry codes and exception types only.
- Purge only the tenant it is given; no cross-tenant path.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
- docs/plans/dynamodb-entity-change-audit-store-implementation-plan-579.md (by issue number: #579, its purge)
```

</details>

---

### Phase 4: Health check and subscriber (pull request 4)

<details>
<summary>Tasks</summary>

1. `EntityChangeAudit/EntityChangeAuditSubscriber.cs` (new): `SubscribeAsync(string? tenant, Func<EntityChangeAuditEntry, CancellationToken, ValueTask> handler, FromAll position, CancellationToken)` over `SubscribeToStream` on the tenant category; the checkpoint belongs to the caller; a handler exception is caught per event and logged by type and does not end the subscription (a background loop never lets an exception escape); a dropped subscription is reported through a callback.
2. `EntityChangeAudit/EntityChangeAuditEventStoreDBHealthCheck.cs` (new): a read-only probe plus the `$by_category` check; `DefaultName`, static `Tags`.
3. `ServiceCollectionExtensions.cs` (edit): registers the health check and the subscriber; `EntityChangeAuditRegistrationTests` (edit) builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves both.
4. Tests (substituted client) for the subscriber and the health check, guard tests, `PublicAPI.Unshipped.txt`, manifest.

Files: 9 (subscriber, health check, extensions edit, registration test edit, subscriber tests, health check tests, guard tests, PublicAPI, manifest).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- The tenant category stream exists and the store reads it for queries.

TASK:
Add the optional catch-up subscriber.

KEY RULES:
- At-least-once delivery; the caller stores the checkpoint; handler exceptions never end the loop; cancellation ends it cleanly.

REFERENCE FILES:
- src/Encina.EventStoreDB (the skeleton of #585)
```

</details>

---

### Phase 5: Integration tests and cross-cutting integration (pull request 5)

<details>
<summary>Tasks</summary>

1. `tests/Encina.IntegrationTests/EventStoreDB/EntityChangeAuditStoreEventStoreDBIntegrationTests.cs` (new) on the EventStoreDB collection fixture of #585 (`Testcontainers.EventStoreDb`): idempotent retry, history order, tenant isolation (two tenants), reserved tenant, purge per tenant, a category read after the projection catches up (awaited by polling the category with a deadline, never a sleep), the subscriber, the missing-projection `Left`.
2. Telemetry: spans and counters from the `InstrumentedEntityChangeAuditStore` of #1778, which wraps any store; nothing store-specific is added.
3. `changelog.d/576-eventstoredb-entity-change-audit-store.added.md`.

Files: 3.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- The store, purge, health check and subscriber exist.

TASK:
Write the integration tests on the shared EventStoreDB collection fixture.

KEY RULES:
- Shared [Collection] fixture; never a per-class container; no Thread.Sleep.
- [Trait("Category", "Integration")]; clean the tenant's streams in InitializeAsync.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures (the EventStoreDB fixture of #585)
```

</details>

---

### Phase 6: Documentation and finalization

<details>
<summary>Tasks</summary>

1. By `docs-writer`: the package README section (entity-change audit, the scavenge duty, the `$by_category` requirement, the redaction premise), the "which audit store do I need" page, and the ADR-027 and ADR-036 amendments when Dilemma D1 is answered A.
2. Verification: `dotnet build --configuration Release` with 0 warnings; each flag against the Research targets.

Files: 3.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- The code of #576 is merged.

TASK:
Update the EventStoreDB README and the audit guide; amend the ADRs if the maintainer chose A.

KEY RULES:
- Coverage cited with covref markers; English only; the reference application is never named.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| ADR-036 | The entity-change contract; decision 6 |
| ADR-027, ADR-019 | EventStoreDB deprecated; event-sourced stores have no InMemory variant |
| SPEC-000 DEC-009, section 6 | Scope: anything not listed is in 1.0; #585 named |
| GDPR Art. 5(1)(e), SPEC-002 REQ-007 | Storage limitation of old and new values |
| ADR-062 | Categories of the six new `entity_change_audit.eventstoredb.*` codes (Design Choice 7) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `Encina.EventStoreDB` skeleton, client registration, health check, fixture | #585 plan (first pull request) | Package, `EventStoreClient`, collection fixture |
| `IEntityChangeAuditStore`, `EntityChangeAuditEntry`, `InstrumentedEntityChangeAuditStore`, `EntityChangeAuditOptions` | `Encina.Security.Audit` after #1778 | Contract, telemetry, retention |
| Sibling backends | #577-#582 plans | Same contract and tenant rules |
| `EventStore.Client.Grpc.Streams` 23.3.9 | `Directory.Packages.props` | The client |

Per-file coverage targets (numeric, per flag):

| File | unit | guard | integration | Justification |
|------|-----:|------:|------------:|---------------|
| `EntityChangeAuditStoreEventStoreDB.cs` | 85 | 10 | 80 | A substituted client reaches every `Left`; the container covers category reads, truncation and deduplication |
| `Internal/EntityChangeStreamNames.cs`, `Internal/EntityChangeEventData.cs` | 100 | 15 | 0 | Pure functions with guarded arguments |
| `EntityChangeAuditSubscriber.cs` | 85 | 10 | 70 | Handler isolation and drop paths by unit tests; a real subscription by integration |
| `EntityChangeAuditEventStoreDBHealthCheck.cs` | 100 | 10 | 70 | Two outcomes plus the missing projection |
| `EntityChangeAuditEventStoreDBOptions.cs` | 100 | 40 | 0 | Validation only |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.EventStoreDB` (entity change audit) | `EventStoreDBEntityChangeAudit = (52700, 52749)` | Security and compliance zone, after `AuditMartenEntityChange` 52650-52699 (#575) and 52600-52649 (#3975); 52350-52599 are the backends of #577-#581; first free slot after checking `origin/main` and the open `docs/plan-*` heads. Ids: 52700 write failed (code), 52701 projection missing, 52702 scan cap reached, 52703 purge completed (count), 52704 purge failed, 52705 subscription dropped, 52706 handler failed (exception type); 52707-52749 free. The package-level ids of the dead letter store (6925-6934) are #585's |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Mapping, names, options, error codes (PR 1) | 9 | Includes `EntityChangeAuditErrors.cs`; depends on #585 and #1778 |
| Store and registration (PR 2) | 7 | |
| Retention and EventIds (PR 3) | 10 | Registry, `EventIdRanges.cs`, `AssemblyRanges`, PublicAPI x2 counted |
| Health check and subscriber (PR 4) | 9 | Includes the `ServiceCollectionExtensions.cs` and registration test edits |
| Integration tests and changelog (PR 5) | 3 | |
| Documentation (PR 6) | 3 | `docs-writer`, includes the ADR amendments |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is pre-1.0, .NET 10, C# 14; ROP with Either<EncinaError, T>.
- ADR-036 defines IEntityChangeAuditStore (work item #1778). The Encina.EventStoreDB package skeleton comes from #585.

IMPLEMENTATION OVERVIEW:
1. Phase 0: verify five server behaviors locally; Phase 1: stream names, event mapping, options.
2. Phase 2: EntityChangeAuditStoreEventStoreDB and registration.
3. Phase 3: purge by truncate or soft delete, EventIds 52700-52749.
4. Phase 4: health check and optional subscriber; Phase 5: integration tests; Phase 6: docs and ADR amendments.

KEY PATTERNS:
- Stream entitychange.{hex(tenant)}-{b64url(type)}.{b64url(id)}; event id is the entry id; tenant default reserved; deny without tenant under tenancy.
- Reads of one entity are one stream read; tenant-wide reads use the category with a scan cap and fail closed.
- Retention is a per-tenant purge; physical removal needs scavenge (documented).
- Errors never swallowed; no server message in a Left, a log or a span; TimeProvider.

REFERENCE FILES:
- docs/plans/eventstoredb-dead-letter-store-implementation-plan-585.md
- docs/architecture/adr/036-three-audit-stores.md
- docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Audit history is read rarely and must be fresh |
| 2 | OpenTelemetry | ✅ | Through `InstrumentedEntityChangeAuditStore` (#1778); no tenant id or entity id as a tag |
| 3 | Structured Logging | ✅ | `EntityChangeAuditEventStoreDBLog` 52700-52706 (Phase 3) |
| 4 | Health Checks | ✅ | Read probe and `$by_category` check (Phase 4) |
| 5 | Validation | ✅ | Entry id, tenant and stream-name checks before any I/O (Phase 1) |
| 6 | Resilience | ❌ | A failed write is a `Left` that fails the unit of work; the client's own deadlines apply; a retry layer would hide it |
| 7 | Distributed Locks | ❌ | Append with a deterministic event id needs no lock |
| 8 | Transactions | ❌ | One event per append; not atomic with an entity change in another database (documented) |
| 9 | Idempotency | ✅ | Deterministic event id; the server deduplicates a retry within its duplicate-detection window (verified in Phase 0); a retry after the window can append a second event unless the writer checks the stream's last event id |
| 10 | Multi-Tenancy | ✅ | Injective tenant-qualified streams, one category per tenant, deny without tenant under tenancy, reserved `default` |
| 11 | Module Isolation | ✅ | `ModuleId` is part of the stored entry, as ADR-036 requires |
| 12 | Audit Trail | ✅ | This is the entity-change audit trail |

---

## Prerequisites & Dependencies

- **Hard prerequisites**: the package skeleton of #585 (first pull request of its plan) and #1778 (the contract in `Encina.Security.Audit`). Only Phase 0 (the throwaway server verification, which commits nothing) can start before either merges; PR 1 needs the #585 skeleton (the package, its manifest and its `Encina.slnx` row) and #1778 (`EntityChangeAuditEntry`), so it cannot start in parallel.
- **Decision prerequisite**: Design Choice 1 (ADR-027 and ADR-036 decision 6); under B nothing in this plan is built.
- #4182 (per-tenant retention design) fixes how the retention service passes each tenant; #575 and #577-#582 are the sibling backends.

---

## Next Steps

1. The maintainer answers Dilemmas D1-D5 when work on the issue starts, D1 together with #585 and #575, D2 together with #585's D4.
2. Run Phase 0 on the vendor LTS image and report the table before any pull request.
3. After the prerequisites merge, cut PR 1 and report the per-file measurements against the targets above; register the EventId range in the pull request that first uses an id (PR 3).
