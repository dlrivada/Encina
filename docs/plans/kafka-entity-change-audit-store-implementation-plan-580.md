# Implementation Plan: `EntityChangeAuditStoreKafka` — Apache Kafka streaming for the entity-change audit store

> **Issue**: [#580](https://github.com/dlrivada/Encina/issues/580)
> **Type**: Feature
> **Complexity**: Medium (6 phases, a decorator store over a queryable store, a replay reader, ~10 source files + tests)
> **Estimated Scope**: ~700-1,000 lines of production code + ~1,300-1,800 lines of tests

---

## Summary

Stream every entity-change audit entry (today `IAuditLogStore`, becoming `IEntityChangeAuditStore` under ADR-036) to an Apache Kafka topic, so SIEM, analytics and other systems receive the trail as it is written and can replay it. A Kafka topic is an append-only log with no random-access query and no per-record delete, while the ADR-036 contract also needs paged history by entity and `PurgeEntriesAsync`. The issue itself recommends Kafka "combined with a queryable store" and asks for a composite store; this plan builds `EntityChangeAuditStoreKafka` as that composite (Design Choice 1): it writes to a queryable inner store and then produces to Kafka, and delegates reads and purge to the inner store. The samples of the issue (`IProducer<string, string>`, `JsonSerializer.Serialize(entry)` with the user id in plain headers, `Task` returns, a non-injective `EntityType:EntityId` key) are replaced by the ADR-036 shapes.

What the code looks like today (origin/main `ff9a579c1`, re-checked for this revision; the plan branch was cut from `039cd3524`):

- `IAuditLogStore` lives in `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs`; `AuditLogEntry` and `InMemoryAuditLogStore` sit next to it. ADR-036 renames and reshapes all of it; that work is **#1778, open**.
- `src/Encina.Kafka` (transport) has `KafkaMessagePublisher`, `KafkaHealthCheck`, `EncinaKafkaOptions` and `AddEncinaKafka`, which registers one singleton `IProducer<string, byte[]>` with `TryAddSingleton` (idempotence on by default, acks `all`). It references only `Encina` and `Encina.Messaging` and has no PublicAPI files. EventId range `Kafka` (4150-4199, ids 4150-4155 in use in `src/Encina.Kafka/Log.cs`) moves to 40050-40099 in #4007 TR-1, and the #587 plan (Kafka dead letters, open) claims 4156-4175 of it; this plan uses neither.
- `Confluent.Kafka` 2.14.0 (Apache-2.0) and `Testcontainers.Kafka` 4.15.0 are pinned; `tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs` (image `confluentinc/cp-kafka:7.6.0`, collection `Kafka`) exists. Manifest `.github/coverage-manifest/Encina.Kafka.json` has package targets guard 15, unit 55.
- #636 (CDC audit streaming, open) streams captured database changes to sinks; it is independent of this store (different source, its own `ICdcAuditSink`).

**Provider category**: Kafka is a message transport (`AGENTS.md` section 5, transports), used here as an audit sink, outside the 10-provider database matrix. **Affected packages**: a new audit Kafka package (Design Choice 2), `Encina.Security.Audit` (the contract, from #1778), `Encina` (EventId registry), test projects.

---

## Design Choices

<details>
<summary><strong>1. Role of Kafka in the store (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Composite decorator: `EntityChangeAuditStoreKafka` wraps the registered queryable `IEntityChangeAuditStore` (any of the 10 database providers or another backend); write = inner write, then produce and await delivery; history and `PurgeEntriesAsync` delegate to the inner store** | The full contract holds (history, paging, per-tenant purge); Kafka adds streaming only; matches the issue's recommended architecture and its composite criterion | Two systems per write; a delivery failure after the inner write leaves the entry stored but not streamed until the retried write produces it |
| **B) Full store on Kafka with a log-backed in-process index (the approach of the #587 plan)** | Kafka is the only system | Rebuilding an index of every audit entry in memory does not scale to audit volumes; purge is impossible per record, so retention needs one topic per tenant whose lifetime equals that tenant's policy (Design Choice 5 does not apply) |
| **C) Kafka-only sink that answers history and purge with a `Left`** | Simplest | Breaks the contract; the retention service would fail on it |

### Chosen Option: **A — composite decorator over a queryable store** (recommended, pending the maintainer)

### Rationale

- Ordering (rule): the inner write goes first because it is the system of record; if delivery fails, the call returns `Left` (`Unavailable`), so the fail-closed writer fails the unit of work and its retry repeats both steps (the inner write is idempotent; Kafka may see the entry twice and consumers deduplicate by the `encina-entry-id` header). B wins for small, Kafka-only deployments; C wins only if the contract were split into write and read interfaces, which ADR-036 does not do.
- Retention under A: the composite's `PurgeEntriesAsync` passes the tenant and cutoff it receives straight to the inner store; the retention service calls it once per tenant from a tenant-less scope after enumerating `ITenantStore` (#4182). The composite never reads the ambient tenant for a purge and has no cross-tenant purge opt-in; the Kafka copies are bounded by Design Choice 5.
- **Tenant segments that `ITenantStore` no longer returns (rule).** Enumerating registered tenants alone would never purge entries of an offboarded tenant (kept under path C of #2847 after its tenant record is removed) or `default`-segment entries written before tenancy was switched on (GDPR Art. 5(1)(e), SPEC-002 REQ-007). The composite delegates `ListTenantSegmentsAsync` (or the name #1778 gives the store's segment listing) to the inner store, and the retention service purges every listed segment that `ITenantStore` does not return, including `default` once tenancy is on, under the residual policy #4182 defines; the topic rule of Design Choice 5 bounds their Kafka copies like any other. This case is an acceptance item of #4182.

</details>

<details>
<summary><strong>2. Package placement (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.Security.Audit.Kafka`** referencing `Encina.Kafka` and `Encina.Security.Audit` | Transport users do not take the audit package (pay-for-what-you-use, `AGENTS.md` section 1); reuses the transport's producer registration | One more package |
| **B) An `EntityChangeAudit/` folder inside `Encina.Kafka`** (as the #587 plan does for dead letters) | No new package | Every Kafka transport user takes `Encina.Security.Audit` and its dependencies |

### Chosen Option: **A — `Encina.Security.Audit.Kafka`** (recommended, pending the maintainer)

### Rationale

- The dead letter store of #587 depends only on `Encina.Messaging`, which `Encina.Kafka` already references; the audit store would add a new dependency to the transport. B wins when the maintainer prefers one Kafka package regardless of dependencies.

</details>

<details>
<summary><strong>3. Topic layout and tenant isolation (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One topic (default `encina.entity-change-audit`); message key = length-prefixed `(tenant, entityType, entityId)` so one entity's changes stay ordered in one partition; headers `encina-entry-id`, `encina-tenant`, `encina-schema`** | One topic to provision; per-entity ordering | Every consumer of the topic sees every tenant; isolation depends on consumers filtering |
| **B) One topic per tenant (`{prefix}.{encoded tenant}`)** | Isolation enforceable with topic ACLs | Topic count grows with tenants; topics must exist before a tenant's first write |

### Chosen Option: **A — one topic, tenant-qualified keys and headers** (recommended, pending the maintainer)

### Rationale

- Tenant rules are those of the family: reserved segment `default` only when tenancy is off; a real tenant `default` rejected (`InvalidRequest`, ADR-062 R3); tenancy on and no tenant resolved denies before the inner write (`InvalidRequest`). The replay reader of Design Choice 4 filters by the ambient tenant and needs an explicit, logged opt-in to read all tenants (`Forbidden` otherwise, R6). B wins when tenants' audit streams go to different consumers that must not see each other.
- The user id and values never go into headers; they stay in the payload, which consumers already must protect.

</details>

<details>
<summary><strong>4. The "streamable" acceptance criterion: subscription and replay (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IEntityChangeAuditStream.ReadFromAsync(DateTimeOffset fromUtc, string? entityTypeFilter, CancellationToken)` returning `IAsyncEnumerable<Either<EncinaError, EntityChangeAuditEntry>>`: an assigned consumer (no consumer group) positioned with `OffsetsForTimes`, tenant-filtered, that keeps following new records until cancelled (subscription = replay without an end)** | Covers both "subscribe" and "replay" with one method; no group offsets for Encina to manage | Each reader holds a consumer; parallel consumption is the caller's design |
| **B) Consumer-group subscription with committed offsets inside Encina** | Shared progress across instances | Offset management, rebalancing and a hosted service inside the library |
| **C) Defer the capability to its own issue** | Smallest pull requests | Drops an acceptance criterion |

### Chosen Option: **A — replay-and-follow with an assigned consumer** (recommended, pending the maintainer)

### Rationale

- A gives both behaviors of the issue's `IAuditLogStoreStreamable` without background machinery. B wins when several instances must share one feed; C wins if the maintainer wants the composite store first.

</details>

<details>
<summary><strong>5. The topic retention rule (under option A of Design Choice 1)</strong></summary>

**Class:** Settled
**Settled by:** ADR-036 decision 1 (old and new values are never kept indefinitely by default), GDPR Art. 5(1)(e) (storage limitation), AGENTS.md section 3 (compliance gates fail closed; the only opt-out is explicit and logged), #4182 (retention is per tenant)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rule: under option A of Design Choice 1 (the inner store is the record), the effective lifetime of a Kafka copy (`retention.ms` plus `segment.ms`, because Kafka deletes only closed segments, plus the broker's retention check interval) never exceeds the shortest retention that applies to the tenants written to that topic, and `cleanup.policy` never includes `compact`; the only exception is the explicit, logged `AllowRetentionMismatch` opt-out. How the rule is enforced is Design Choice 10** | Copies of personal data never outlive any tenant's retention by accident; compaction, which keeps only the last record per key, is excluded | The shortest tenant policy bounds every tenant's copies in a shared topic |
| **B) No rule: topic retention is the operator's business** | Nothing to enforce | A misconfigured topic keeps audit data indefinitely, which ADR-036 decision 1 forbids by default |

### Chosen Option: **A — the rule, with the logged opt-out as the only exception**

### Rationale

- Under option A of Design Choice 1, Kafka copies are outside `PurgeEntriesAsync`, so the topic's retention is the only limit on them; B leaves that limit to chance, so A is the only compliant option. Shortening a copy below a tenant's retention is harmless there, because the inner store keeps the record.
- **Under option B of Design Choice 1 this rule does not apply.** Kafka is then the only copy, so a shared topic at the shortest tenant retention would delete other tenants' audit evidence before their retention ends (GDPR Art. 17(3)(b)). Option B therefore needs one topic per tenant whose effective lifetime equals that tenant's retention; that requirement is part of option B's cost in Design Choice 1.

</details>

<details>
<summary><strong>6. Payload format (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) UTF-8 JSON (System.Text.Json, explicit property names) with a schema version header `encina-schema: entity-change-audit/1`** | No new dependency; readable by any consumer; matches the transport's `byte[]` producer | No registry-enforced schema evolution |
| **B) Avro or Protobuf with Confluent Schema Registry** | Enforced compatibility; compact | New dependencies and a registry in CI; not in `Directory.Packages.props` |

### Chosen Option: **A — JSON with a schema version header** (recommended, pending the maintainer)

### Rationale

- A keeps the package dependency-light; B wins in organizations that already run Schema Registry for every topic.

</details>

<details>
<summary><strong>7. Tamper evidence (Dilemma D6)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rely on the log: records are immutable once written, compaction is refused (Design Choice 5), a `encina-payload-hash` header (SHA-256) travels with each record, and documented ACLs (producer: `Write`; readers: `Read`; nobody but operators: `Delete`, `Alter`); a hash chain is left to #1240** | No new mechanism; Kafka's own guarantee | A cluster operator can still delete or rewrite segments |
| **B) A per-topic hash chain built now** | Gaps and rewrites detectable | Serialises production; duplicates #1240 |

### Chosen Option: **A — log immutability, payload hash header, ACLs; chain through #1240** (recommended, pending the maintainer)

### Rationale

- #1240 (open) owns the cross-provider chain. B wins if a consumer must verify completeness before #1240 lands.

</details>

<details>
<summary><strong>8. Contract, naming and failure semantics</strong></summary>

**Class:** Settled
**Settled by:** ADR-036 (decisions 1 and 7), ADR-001, ADR-006, ADR-062 (R3-R6), AGENTS.md section 3 (fail closed; errors never swallowed; TimeProvider), AGENTS.md section 4

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EntityChangeAuditStoreKafka : IEntityChangeAuditStore` on the ADR-036 contract; `Either` results; a delivery failure (`ProduceException`, `PersistenceStatus.NotPersisted`, delivery timeout) is a `Left` that fails the unit of work; cancellation propagates** | Fail-closed auditing; coherence | Depends on #1778 |
| **B) Fire-and-forget `Produce` with a delivery handler that only logs** | Lowest latency | Swallows a failed audit write, forbidden by section 3 |

### Chosen Option: **A — awaited delivery on the ADR-036 contract**

### Rationale

- B reports success for an entry that may never reach the stream. Only the Kafka `ErrorCode` reaches errors and logs, never `Error.Reason` text; every `EncinaErrors.Create` passes a named `category:` and a constant message. Bootstrap servers and SASL credentials stay in `EncinaKafkaOptions` of the transport; any credential added there is `[JsonIgnore]` with a safe `ToString()`.

</details>

<details>
<summary><strong>9. Erasure requests and the copies in Kafka (under option A of Design Choice 1)</strong></summary>

**Class:** Settled
**Settled by:** maintainer decision of 2026-10-26 on #2847 (path C is the default for audit data: GDPR Art. 17(3)(b) and 17(3)(e)), ADR-036 decision 1, Design Choice 5 (Settled rule: the effective topic lifetime never exceeds the applicable tenant retention, except through the logged opt-out)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No per-subject delete: the inner store follows path C, and the Kafka copies expire with the topic, whose effective lifetime the rule of Design Choice 5 keeps within the applicable tenant retention (an operator who sets the explicit `AllowRetentionMismatch` opt-out takes that bound out, and the opt-out is logged); minimisation by the writer's redaction hook** | Matches the audit erasure decision; Kafka cannot delete single records anyway | An exceptional per-person deletion cannot reach records already streamed |
| **B) Tombstones per subject** | — | Requires compaction, which removes the history this store exists to keep |

### Chosen Option: **A — path C, the topic rule bounds the copies**

### Rationale

- Path C records the request and keeps audit data until retention. The README states that streamed copies follow topic retention and that consumers which persist them inherit the same retention duty.
- The entry holds under option A of Design Choice 1, which every enforcement option of Design Choice 10 serves. Under option B of Design Choice 1 there is no inner store: path C applies to the per-tenant topics themselves, whose lifetime equals each tenant's retention (Design Choice 5).

</details>

<details>
<summary><strong>10. How the topic retention rule is enforced (Dilemma D7)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Check and refuse: a validator reads each audit topic's `retention.ms`, `segment.ms` and `cleanup.policy` through `IAdminClient.DescribeConfigsAsync` at start-up (fails start-up on a breach) and again whenever the retention service runs or a tenant policy changes (#4182); after a breach found at run time, writes return `Left` (`Internal`) until the topic is fixed; the logged `AllowRetentionMismatch` opt-out skips both** | Encina never changes broker configuration; a policy shortened or a tenant onboarded with a shorter policy is caught at the next retention run instead of the next restart | Needs `DescribeConfigs`; a breach found at run time stops audited writes, which fails the units of work that produce them |
| **B) Encina sets the configuration: at start-up and whenever the shortest applicable policy changes, `IAdminClient.IncrementalAlterConfigsAsync` sets `retention.ms`, `segment.ms` and `cleanup.policy=delete`** | No manual topic tuning; no write outage after a policy change | Needs `AlterConfigs` on the topic; Encina owns broker configuration that operators may manage elsewhere |
| **C) One topic per tenant (Design Choice 3 option B), each configured by A or B with its own tenant's policy** | No tenant's copies are shortened by another tenant's policy | Topic count grows with tenants; needs Design Choice 3 option B |
| **D) Start-up check only** | Simplest | A policy shortened after start-up leaves copies past retention until the next restart |

### Chosen Option: **A — check at start-up and at every retention run, refuse writes after a run-time breach** (recommended, pending the maintainer)

### Rationale

- A, B and C all satisfy the rule of Design Choice 5 continuously; D leaves the run-time gap the rule forbids, so it is listed only for completeness. A wins when operators own topic configuration; B wins when the application owns its topics; C wins when tenant retentions differ widely. The effective lifetime always counts `segment.ms` (default 7 days) and the broker's retention check interval, not `retention.ms` alone. When Encina creates the topic (`CreateTopicIfNotExists`, default `false`) it sets `cleanup.policy=delete`, `retention.ms` and `segment.ms` so that their sum stays within the shortest applicable policy.

</details>

---

## Implementation Phases

### Phase 1: Package, options and message mapping

<details>
<summary>Tasks</summary>

1. `src/Encina.Security.Audit.Kafka/Encina.Security.Audit.Kafka.csproj` (name per Design Choice 2; references `Encina.Kafka` and `Encina.Security.Audit`; PublicAPI analyzers; XML docs; `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.PropertyTests`, `Encina.IntegrationTests`, `Encina.ContractTests`, not `Encina.GuardTests`, so the guard target of 0 for internal types stays valid); `Encina.slnx` row; PublicAPI x2.
2. `EntityChangeAuditKafkaOptions` (public, sealed): `TopicName`, `CreateTopicIfNotExists` (`false`), `Partitions`, `ReplicationFactor`, `DeliveryTimeout`, `AllowRetentionMismatch` (`false`), `AllowCrossTenantReplay` (`false`).
3. `EntityChangeAuditKafkaMessageMapper` (internal): entry to and from key, headers and JSON payload (Design Choices 3 and 6), payload hash.
4. Unit tests (key injectivity, header set, round trip), options guard tests, manifest `.github/coverage-manifest/Encina.Security.Audit.Kafka.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library (ROP). The ADR-036 contract is delivered by #1778.
- Encina.Kafka registers IProducer<string, byte[]> with TryAddSingleton.

TASK:
Create the package, options and message mapper of the Phase 1 Tasks with tests and manifest.

KEY RULES:
- Key length-prefixed and tenant-qualified; never user ids or values in headers.
- JSON with explicit names and the encina-schema header.
- No [Obsolete]; XML docs; PublicAPI complete.

REFERENCE FILES:
- src/Encina.Kafka/ServiceCollectionExtensions.cs
- src/Encina.Kafka/KafkaMessagePublisher.cs
- docs/architecture/adr/036-three-audit-stores.md
```

</details>

---

### Phase 2: The composite store, topic validator and registration

<details>
<summary>Tasks</summary>

1. `EntityChangeAuditStoreKafka` (public, sealed) `: IEntityChangeAuditStore`; constructor `(IEntityChangeAuditStore inner, IProducer<string, byte[]> producer, ITenantContext? tenantContext, IOptions<EntityChangeAuditKafkaOptions> options, ILogger<EntityChangeAuditStoreKafka> logger)`; write per Design Choice 1; history delegates to `inner`; `PurgeEntriesAsync` passes the tenant and cutoff it receives to `inner` unchanged (signature from #4182 and #1778) and never reads the ambient tenant; the composite must be constructible in the tenant-less scope of the retention service.
2. `EntityChangeAuditTopicValidator` (internal, `IHostedService`): creates the topic on opt-in and enforces the rule of Design Choice 5 (effective lifetime `retention.ms` + `segment.ms` + check interval within the shortest applicable tenant policy, no compaction) through an `IAdminClient` built from `EncinaKafkaOptions`, with the mechanism chosen in Design Choice 10 (under the recommended option: at start-up and on every retention run, with writes refused after a run-time breach).
3. `EntityChangeAuditKafkaErrors`.
4. `ServiceCollectionExtensions.AddEncinaEntityChangeAuditKafka(this IServiceCollection, Action<EntityChangeAuditKafkaOptions>? configure = null)`: requires `AddEncinaKafka` (clear build-time error otherwise) and a registered inner `IEntityChangeAuditStore`; decorates it (the decorator must sit inside the ADR-036 `InstrumentedEntityChangeAuditStore` so spans cover both writes; the mechanism follows #1778's registration); options with `ValidateOnStart`; the validator through `TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EntityChangeAuditTopicValidator>())`. The decorator keeps the inner store's lifetime (a singleton decorator over a scoped store is not allowed).
5. EventId range and log class in this PR. DI test with `ValidateOnBuild` and `ValidateScopes` (singleton and scoped inner stores). CRAP <= 10 per method.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 created the package, options and mapper.

TASK:
Implement the composite store, the topic validator, the error codes, the registration, the EventId range and the log class, with unit, guard and DI tests.

KEY RULES:
- Inner write first, then awaited delivery; a delivery failure is a Left (Unavailable); cancellation propagates.
- The decorator keeps the inner store's lifetime; never a scoped service inside a singleton.
- Hosted services through TryAddEnumerable.

REFERENCE FILES:
- src/Encina.Kafka/ServiceCollectionExtensions.cs
- docs/architecture/adr/062-error-category-contract.md
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 3: Replay reader and health check

<details>
<summary>Tasks</summary>

1. `IEntityChangeAuditStream` (public) and `EntityChangeAuditStreamKafka` (Design Choice 4): consumer built per call from `EncinaKafkaOptions` with `EnableAutoCommit = false` and no group subscription, `Assign` with `OffsetsForTimes`, tenant filter from the header before deserialising, a corrupt record yields one `Left` (`Internal`) and the stream continues; registered by `AddEncinaEntityChangeAuditKafka`.
2. `EntityChangeAuditKafkaHealthCheck : IEncinaHealthCheck`: topic metadata through the admin client; const `DefaultName`, static `Tags`; registered by the same method.
3. Unit and guard tests; manifest edit; `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Phase 2 shipped the composite store and its registration.

TASK:
Add IEntityChangeAuditStream with its Kafka reader and the health check, both registered.

KEY RULES:
- No consumer group, no committed offsets; tenant-bound unless the explicit, logged opt-in.
- Disposal of the consumer on cancellation; no exception message in a Left.

REFERENCE FILES:
- src/Encina.Kafka/Health/KafkaHealthCheck.cs
- tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs
```

</details>

---

### Phase 4: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. EventId range `KafkaEntityChangeAudit = (52500, 52549)` in `src/Encina/Diagnostics/EventIdRanges.cs`, registered in PR 2 with its line in `src/Encina/PublicAPI.Unshipped.txt` (one line for the new field), the `["Encina.Security.Audit.Kafka"]` entry in `AssemblyRanges` of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`, and in `docs/engineering/eventid-block-registry.md` a "Registered ranges" row `KafkaEntityChangeAudit | 52500-52549` (Zone `zone`) and a "Blocks" row `52500-52549` with status `registered`.
2. `Diagnostics/KafkaEntityChangeAuditLog.cs`, ids packed from 52500: 52500 topic created, 52501 delivery failed (error code), 52502 topic retention mismatch, 52503 retention mismatch accepted by opt-out, 52504 compaction refused, 52505 replay started, 52506 corrupt record skipped, 52507 cross-tenant replay opt-in used, 52508 writes refused after a run-time retention breach. 52509-52549 stay free.
3. The ADR-036 `InstrumentedEntityChangeAuditStore` wraps the composite (DI test); produce spans follow the transport's conventions; ADR-054 D4 tenant attribute and tag only when multi-tenancy is on; no baggage is propagated to the broker (ADR-054 D4).
4. **ErrorCategory (ADR-062)**: `entity_change_audit.kafka.delivery_failed` `Unavailable`; `.delivery_rejected` (message too large, authorization) `Internal` (R4); `.tenant_required` and `.reserved_tenant` `InvalidRequest` (R3); `.cross_tenant_denied` `Forbidden` (R6); `.corrupt_record` `Internal`; `.topic_retention_breach` (a configuration fault, R1) `Internal`; `.invalid_replay` `Validation`. Errors of the inner store pass through unchanged. Cancellation propagates.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- AGENTS.md section 7: new ranges only in the 10000+ zone, registered before use.

TASK:
Register KafkaEntityChangeAudit 52500-52549 (in PR 2), write the log class, wire the categories and the decorator test.

KEY RULES:
- Ids packed from 52500; nothing outside the range.
- Never log EncinaError.Message, Kafka reason text, values or ids.

REFERENCE FILES:
- docs/engineering/eventid-block-registry.md
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every applicable flag reaches its target in `.github/coverage-manifest/Encina.Security.Audit.Kafka.json`, each target and its justification set in the pull request whose tests reach it.

<details>
<summary>Tasks</summary>

1. **Unit**: mapper, composite ordering (inner `Left` stops before produce; produce failure after inner success is `Left`), tenant rules, purge passes the given tenant and cutoff to the inner store unchanged (never the ambient tenant), segment listing delegated to the inner store, validator branches (infinite retention, `retention.ms` + `segment.ms` longer than the shortest applicable tenant policy, compaction, opt-out, a breach found at a retention run refuses writes), replay filter and corrupt records; `IProducer`, `IAdminClient` and `IConsumer` substituted (interfaces; `DeliveryResult` and `ProduceException` have public constructors), a fake inner store.
2. **Guard** (`tests/Encina.GuardTests/Security/Audit/Kafka/`): constructors, methods, registration, options.
3. **Contract** (PR 4): the shared entity-change contract base run against the composite over the in-memory inner store and the Kafka container (the contract's history and purge pass through).
4. **Integration** (PR 4, `[Collection("Kafka")]`, `[Trait("Category", "Integration")]`): entry produced with key and headers, per-entity ordering, delivery failure with the broker paused, topic created with delete policy and `retention.ms` + `segment.ms` within the policy, validator refusing a compacted topic and a topic whose segments outlive the policy, replay from a time and follow, tenant-filtered replay, health check. Topic names are unique per test class. Collection definitions are per assembly: the `KafkaCollection` next to `KafkaFixture` in `Encina.TestInfrastructure` is not visible to the test projects, and `Encina.IntegrationTests` has no Kafka collection today (its Kafka tests use `IClassFixture<KafkaFixture>`). PR 4 therefore adds a `Kafka` collection definition over `KafkaFixture` in `Encina.IntegrationTests` and one in `Encina.ContractTests`. `Testcontainers.Kafka` 4.15.0 (MIT) and the `confluentinc/cp-kafka:7.6.0` image are already used on main for the transport tests; the image runs only in tests.
5. **Property** (PR 4): key injectivity and mapper round trip.
6. **Load/Benchmark**: `.md` justifications (throughput is the broker's; not a hot path of Encina).

**Manifest rule**: the manifest is new (no existing floor; `Encina.Kafka.json` is not touched). PR 1 creates it with package-level `targets` for unit and guard; PR 4 adds the package-level contract, property and integration targets. Each PR adds the per-file `targets` and one `justifications` sentence per flag (below) for the flags its own tests reach, so `coverage-report.cs --check-justifications` passes on every PR.

**Per-file targets** (`-` = not applicable; "PR" adds the target, its tests and its justification):

| File | Unit | Guard | Contract | Property | Integration | PR | Justification |
|------|:----:|:-----:|:--------:|:--------:|:-----------:|:--:|---------------|
| `EntityChangeAuditKafkaOptions.cs` | 90 | 50 | - | - | 60 | 1, 4 | Unit: defaults and validation of names and timeouts. Guard: invalid values on each validated property run the validation half of the file. Integration: built through the registration in the broker tests. |
| `EntityChangeAuditKafkaMessageMapper.cs` | 95 | 0 | - | 90 | 85 | 1, 4 | Unit: key, headers and payload round trip. Guard: 0, internal and `Encina.GuardTests` has no `InternalsVisibleTo`. Property: injectivity and round trip over random entries. Integration: every produced record goes through it. |
| `EntityChangeAuditStoreKafka.cs` | 95 | 30 | 85 | - | 85 | 2, 4 | Unit: ordering, failure and delegation paths with substitutes. Guard: constructor and method argument checks only, about a third of the lines. Contract: the shared suite drives every contract method through the composite. Integration: the broker covers delivery. |
| `EntityChangeAuditTopicValidator.cs` | 90 | 0 | - | - | 80 | 2, 4 | Unit: every configuration branch with a substituted admin client. Guard: 0, internal with no `InternalsVisibleTo` for guard tests. Integration: the broker proves real topic configs. |
| `EntityChangeAuditKafkaErrors.cs` | 100 | 0 | - | - | - | 2 | Unit: every factory method is called by the tests. Guard: 0, internal with no validated parameters. |
| `ServiceCollectionExtensions.cs` | 95 | 40 | - | - | - | 2 | Unit: the DI test builds the provider with ValidateOnBuild for singleton and scoped inner stores. Guard: null `services` and null `configure` run the argument checks before a short registration body. |
| `EntityChangeAuditStreamKafka.cs` | 85 | 30 | - | - | 80 | 3, 4 | Unit: filter and corrupt-record paths with a substituted consumer. Guard: argument and bound checks only. Integration: the broker proves offsets for times and following. |
| `EntityChangeAuditKafkaHealthCheck.cs` | 90 | 30 | - | - | 70 | 3, 4 | Unit: healthy, missing-topic and failure mappings. Guard: the constructor checks only. Integration: the broker proves the healthy path. |
| `Diagnostics/KafkaEntityChangeAuditLog.cs` | 0 | 0 | - | - | - | 2 | Unit: 0, source-generated `[LoggerMessage]` partial methods with no hand-written executable code. Guard: 0, same reason. |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Tests execute real package code; the broker is shared through the Kafka collection.

TASK:
Write the tests of Phase 5 and the manifest targets with justifications in the pull requests that reach them.

KEY RULES:
- Shouldly via Encina.Testing.Shouldly; no FluentAssertions, per-class containers or Thread.Sleep.
- Unique topic names per class; deterministic waits on delivery results, never sleeps.
- Targets never go down; run coverage-report.cs --check-justifications.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
- .github/coverage-manifest/Encina.Kafka.json
```

</details>

---

### Phase 6: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs; `src/Encina.Security.Audit.Kafka/README.md` (composite model, ordering and duplicates, topic retention rule, replay, ACLs, consumers' retention duty); a feature page under `docs/features/`; `docs/INVENTORY.md`; ROADMAP if affected (through `docs-writer`).
2. `changelog.d/580-kafka-entity-change-audit-store.added.md` (through `mechanical-fixer`).
3. ADR only if a Design Choice changes a rule of ADR-036; ADR number assigned when the ADR pull request opens.
4. Build 0 warnings; per-flag coverage and CRAP reported.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Documentation: README, feature page, inventory, changelog fragment.

TASK:
Complete the documentation tasks of Phase 6.

KEY RULES:
- Never edit CHANGELOG.md; use changelog.d/.
- No hand-typed coverage figures; English only; never name the reference application.

REFERENCE FILES:
- docs/features/read-auditing.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Topic | Requirement |
|--------|-------|-------------|
| ADR-036 | Three audit stores | Entity-change contract, 2555-day retention, values never kept indefinitely, fail closed |
| ADR-062 | Error categories | Every code categorised (R3-R6) |
| ADR-054 D4 | Telemetry | Tenant attribute and tag only when multi-tenancy is on; no baggage to brokers |
| SPEC-002 REQ-007 | Retention | Per-category retention, enforced by the inner store and bounded topic retention |
| #4182 (open, `[DEBT]`) | Per-tenant retention | Retention iterates `ITenantStore` with each tenant's policy and passes the tenant to the purge; never the ambient tenant or a cross-tenant opt-in |
| GDPR Art. 5(1)(e), 17(3)(b) | Storage limitation, erasure exception | Topic retention within the shortest applicable tenant retention; recorded erasure (decision on #2847) |
| Apache Kafka topic configuration (vendor) | `retention.ms`, `cleanup.policy`, idempotent producer, `offsetsForTimes` | Checked by the validator; used by the replay reader |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in the Kafka entity-change audit store |
|-----------|----------|----------------------------------------------|
| `IAuditLogStore` (becomes `IEntityChangeAuditStore`) | `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs` | The contract and the inner store |
| `AddEncinaKafka`, `IProducer<string, byte[]>` | `src/Encina.Kafka/ServiceCollectionExtensions.cs` | Producer reused by the composite |
| `EncinaKafkaOptions` | `src/Encina.Kafka/EncinaKafkaOptions.cs` | Bootstrap servers for the admin client and replay consumer |
| `KafkaHealthCheck` | `src/Encina.Kafka/Health/KafkaHealthCheck.cs` | Health check pattern |
| `KafkaFixture`, `Kafka` collection | `tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs` | Shared broker |
| `InstrumentedEntityChangeAuditStore`, `EntityChangeAuditRetentionService` | delivered by #1778 | Tracing; per-tenant retention of the inner store (#4182) |
| `ITenantStore.GetAllTenantsAsync` | `src/Encina.Tenancy/Abstractions/ITenantStore.cs` | Tenant enumeration of the retention service (#4182) |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registration of the new range |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.Audit.Kafka` | 52500-52549 (`KafkaEntityChangeAudit`) | Fourth of the five audit-stores-1 slots 52350-52599, the first free Security and compliance space after the #4007 reservations 50000-52349 (registry on origin/main `1dee4ba61`); ids 52500-52507 used, packed; the transport range `Kafka` (4150-4199, moving to 40050-40099 in #4007 TR-1) is left to the transport and #587 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | ~10 | csproj, options, mapper, composite store, validator, errors, registration, stream interface and reader, health check, log class |
| Tests | ~11 | unit x4, guard x2, DI, contract, integration, property, load/benchmark `.md` x2 |
| Metadata | ~9 | `Encina.slnx`, PublicAPI x2, manifest, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, registry page, allocation test, test-project references |
| **Total** | **~36 with edits** | Every PR starts after #1778, builds and passes alone, at most 15 files and 1500 lines. **PR 1** (11 files): csproj, `Encina.slnx`, PublicAPI x2, options, mapper, `Encina.UnitTests` and `Encina.GuardTests` project references, mapper unit tests, options guard tests, manifest (no service introduced). **PR 2** (15 files): composite store, validator, errors, `ServiceCollectionExtensions`, log class, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, registry page, `EncinaEventIdAllocationTests.cs`, package `PublicAPI.Unshipped.txt`, store unit tests, validator unit tests, DI test, store and registration guard tests, manifest. **PR 3** (9 files): stream interface, reader, health check, registration edit, `PublicAPI.Unshipped.txt`, unit tests x2, guard tests, manifest. **PR 4** (12 files): three test-project references, a `Kafka` collection definition in `Encina.IntegrationTests` and one in `Encina.ContractTests`, contract class, integration tests, property tests, two `.md` justifications, manifest. **PR 5** (4 files): README, feature page, `docs/INVENTORY.md`, changelog fragment |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution first.
- ROP: Either<EncinaError, T>; errors never swallowed; async with CancellationToken; cancellation propagates.
- Issue #580: EntityChangeAuditStoreKafka is a composite IEntityChangeAuditStore (ADR-036, delivered by #1778): inner queryable store first, then awaited Kafka delivery; reads and purge delegate.

IMPLEMENTATION OVERVIEW:
Phase 1: package (recommended Encina.Security.Audit.Kafka), options, message mapper.
Phase 2: composite store (purge passes the given tenant to the inner store), topic validator (retention against the shortest applicable tenant policy, compaction), registration, EventId range 52500-52549, log class.
Phase 3: IEntityChangeAuditStream replay-and-follow reader, health check.
Phase 4: error categories, instrumentation, telemetry tags.
Phase 5: unit, guard, contract, integration (Kafka collection), property tests; manifest targets with justifications.
Phase 6: docs and changelog fragment.

KEY PATTERNS:
- Dilemmas D1-D7 are recommended, pending the maintainer: follow the answers recorded in the plan.
- Tenant-qualified, length-prefixed keys; tenant header; reserved default; deny without tenant; explicit logged cross-tenant replay; the purge receives its tenant from the retention service (#4182) and is passed through unchanged.
- Topic rule (under D1 option A): retention.ms + segment.ms within the shortest applicable tenant retention, never compaction, the logged opt-out as the only exception; enforced by the mechanism of D7 (recommended: check at start-up and at every retention run, refuse writes after a run-time breach).
- Never user ids or values in headers; never log values, ids or reason text.

REFERENCE FILES:
- docs/architecture/adr/036-three-audit-stores.md
- src/Encina.Kafka/ServiceCollectionExtensions.cs
- tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs
- docs/plans/kafka-dead-letter-store-implementation-plan-587.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A write path and a replay stream; nothing to cache. |
| 2 | OpenTelemetry | ✅ | The ADR-036 decorator wraps the composite; produce spans; tenant tag per ADR-054 D4, no baggage. |
| 3 | Structured logging | ✅ | Range 52500-52549 registered first; Kafka error codes only. |
| 4 | Health checks | ✅ | Phase 3: `EntityChangeAuditKafkaHealthCheck` (topic metadata). |
| 5 | Validation | ✅ | Argument checks before I/O; replay bounds validated; topic configuration checked against the rule of Design Choice 5 by the mechanism of Design Choice 10. |
| 6 | Resilience | ✅ | The idempotent producer retries within `delivery.timeout.ms` without duplicating; a final failure is `Unavailable` for the caller's policy. |
| 7 | Distributed locks | ❌ | Append-only; partition order arbitrates per-entity ordering. |
| 8 | Transactions | ⏭️ | Atomic "database write plus stream" would come from the outbox carrying the entry; out of this issue's scope, tracked by an issue to be opened (draft entity-change-audit-outbox-streaming); its number replaces this text when it exists. |
| 9 | Idempotency | ✅ | Inner store idempotent; producer idempotence; `encina-entry-id` header for consumer deduplication of retried writes. |
| 10 | Multi-tenancy | ✅ | Tenant-qualified key and header; reserved `default`; deny without tenant; tenant-bound replay (Design Choice 3); per-tenant retention purge delegated with the tenant passed explicitly (Design Choice 1, #4182). |
| 11 | Module isolation | ✅ | `ModuleId` in the payload (ADR-036 entry). |
| 12 | Audit trail | ✅ | This store streams the entity-change audit trail. |

---

## Prerequisites & Dependencies

- **#1778 (open, `[DEBT]`, no `needs-decision`, no blocking dependency)** delivers the contract, the decorator, the retention service and the shared behavioral contract base. If it merges without a reusable base, the first of #577-#582 to reach its integration pull request adds it (the same fallback as the #582 plan).
- A queryable inner store must be registered (any of the 10 database providers from #1778, or #577-#579, #581, #582).
- #587 (Kafka dead letters) shares the transport producer; no shared files.
- #1240 (open) owns the hash chain (Design Choice 7); #636 (open) streams CDC changes separately.
- **#4182 (open, `[DEBT]`)** decides the per-tenant retention mechanism (tenant enumeration through `ITenantStore`, each tenant's policy, the tenant-explicit purge signature, and the residual policy for segments `ITenantStore` no longer returns: offboarded tenants and the legacy `default` segment) before #1778 implements `EntityChangeAuditRetentionService`; the purge delegation of Design Choice 1, the topic rule of Design Choice 5 and the run-time check of Design Choice 10 follow it.
- The EventId slot 52500-52549 lies after the #4007 reservations (50000-52349, #4174 merged).

---

## Next Steps

1. The maintainer answers D1 to D7 when work on #580 starts; the answers go into `## Maintainer Decisions`.
2. Wait for #4182 and #1778; re-check each Design Choice against main; `adversarial-reviewer` verifies the Settled ones.
3. Brief one worker per proposed pull request (5 pull requests); the orchestrator opens the deferred transactions issue (draft entity-change-audit-outbox-streaming) and cites its number in matrix row 8.
4. The final pull request references `Fixes #580`.
