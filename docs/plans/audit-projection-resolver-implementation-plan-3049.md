# Implementation Plan: `IAuditProjectionResolver` — Audited Operator Resolution of Unreadable Marten Audit Events

> **Issue**: [#3049](https://github.com/dlrivada/Encina/issues/3049) (part 2b of [#2770](https://github.com/dlrivada/Encina/issues/2770))
> **Type**: Feature
> **Complexity**: Medium-High (7 phases, 9 PRs, one package, Marten only)
> **Estimated Scope**: ~1,700-2,100 lines of production code, ~2,400-3,000 lines of tests across the PRs, 4 docs files

---

## Summary

`Encina.Audit.Marten` projects encrypted audit events into read models. Since part 2a ([#2928](https://github.com/dlrivada/Encina/pull/2928), merged 2026-10-29) an event that cannot be decrypted fails typed (`EncryptedFieldDecryptionException`) and Marten's async daemon pauses the shard. A paused shard does not retry on its own, and the only recovery was editing the temporal key marker by hand, which leaves no audit record. This plan adds the operator path decided in Q2 of #2770: an audited, append-only resolution that the projection consults before it throws.

**Maintainer framing (2026-11-04, decision D13).** A key destroyed by a legitimate, recorded erasure (erasure reference) or retention execution never pauses the projection: it projects as `[SHREDDED]` or `[EXPIRED]` and keeps running (already so since #2928). The pause, and therefore this resolution service, exists only for unreadable events whose cause is unknown (no reference: possible attack, corruption or error), where stopping is the correct fail-closed reaction.

**What it implements**

- `IAuditProjectionResolver` with **one write operation**, `RecordAsync`, which records a batch of resolutions (a single resolution is a batch of one); `PreviewAsync` (the validation step alone, a dry run that writes nothing); `GetAsync` and `ListAsync` (reads). Every method returns `ValueTask<Either<EncinaError, T>>` and takes a `CancellationToken`. There is no removal and no revoke method.
- `AuditResolution`: an **append-only, immutable** Marten document, one per recorded resolution, in the same store as `TemporalKeyDocument` and `TemporalKeyDestroyedMarker`. The latest resolution wins; each new one records the ids of the resolutions it supersedes.
- Resolution kinds: `Unreadable`, `Shredded` (needs an `ErasureReference`), `Expired` (needs a `RetentionReference`), `Excluded`, and `Withdrawn` ("no resolution": returns the event to unresolved, so a rebuild pauses on it again).
- A required permission (`audit:resolve`, `MartenOperationAuditOptions.ResolutionPermission`, no opt-out) gates every method; security-relevant denials are audited as one `Denied` entry.
- Batches are validated first and written all or nothing: every resolution document and its audit entry go into **one Marten session and one `SaveChangesAsync`**.
- Projection support: on the failure path only, `OperationAuditEntryProjection` and `ReadAuditEntryProjection` load the event's resolutions through `IDocumentOperations` and apply the effective one deterministically (a shard rebuild yields the same read model).
- `[UNREADABLE]` placeholder (`MartenOperationAuditOptions.UnreadablePlaceholder`), an `IsUnreadable` flag and a persisted `MetadataJsonMasked` flag on both read models.

**Standards and specifications**: GDPR Art. 5(1)(c), 5(1)(e), 5(2) and 32; ADR-020 (temporal crypto-shredding), ADR-034, ADR-036, ADR-006 (pure ROP), ADR-018, ADR-019, ADR-021; SPEC-002 DEC-006 (fail closed).

**Affected packages**: `Encina.Audit.Marten` (code, `PublicAPI.Unshipped.txt`); `Encina` core only for reading `EventIdRanges.cs` (the `AuditMarten` range 2550-2599 already exists, no new range). Tests in `tests/Encina.UnitTests`, `GuardTests`, `ContractTests`, `PropertyTests`, `IntegrationTests`. Docs: `docs/features/audit-marten.md`, ADR-061, ADR-020 pointer.

**Provider category**: none of the database matrix. This is the event-sourcing category (Marten primary, AGENTS.md section 5); it touches no ADO.NET, Dapper, EF Core or MongoDB code, so the 10-provider rule does not apply. Integration tests run against real Marten on PostgreSQL.

### Goal and non-goals

**Goal.** An operator holding the `audit:resolve` permission can resolve poison audit events (events whose key or field cannot be decrypted and carry no erasure or retention reference) from code, with a full audit record, so the paused projection shard continues after a restart or rebuild. The result is deterministic: the applied outcome is a pure function of the event and the stored resolutions. Anonymous callers, callers without the permission and a failing audit write refuse the operation.

**Non-goals**

- Admin endpoint: [#3024](https://github.com/dlrivada/Encina/issues/3024). CLI command: [#3025](https://github.com/dlrivada/Encina/issues/3025). `Encina.Audit.Marten` gets no ASP.NET Core or CLI reference.
- Health check for a paused shard: [#3027](https://github.com/dlrivada/Encina/issues/3027). Operator restart and rebuild daemon semantics, and any in-process `IProjectionDaemon` seam: spike [#3026](https://github.com/dlrivada/Encina/issues/3026). The resolver only writes data; the operator restarts the host or runs `RebuildProjectionAsync` (documented, proven by an integration test).
- Period scope: decided out by D23 = (d); the period-wide legacy or damaged marker case is [#2910](https://github.com/dlrivada/Encina/issues/2910).
- Removing, editing or revoking a resolution (D17 = D: resolutions are immutable, like ADRs), idempotency keys and conflict detection between operators (D14 = D: Encina is not designed for several operators working at once).
- HMAC signing or an append-only event stream for resolution documents (D27 = (a): accepted and documented); changing Marten's store-wide error flags (D22 = (e) of #2770).

### Prerequisite and delivery split (size rule)

[#3012](https://github.com/dlrivada/Encina/issues/3012) (the "no key and no marker" case) is fixed by PR #3083, merged: `OperationAuditEntryProjection.ClassifyTemporalKeyLookup` now throws `EncryptedFieldDecryptionException(EncryptedFieldDecryptionFailureReason.KeyMissing)` instead of `KeyNotFoundException` (D14 of #2770). Phase 3 can start once the gate below is passed; re-check the post-#3083 names and signatures in the code before its brief. The period leaking into `MartenAuditLog` and `MartenAuditErrors` is tracked by bug [#3088](https://github.com/dlrivada/Encina/issues/3088); the messages of this feature must not add the period to logs.

The size rule of the EPIC #2845 working rule (#2900, `.claude/skills/worker-brief/SKILL.md`) allows one worker PR, one package, at most 15 files and 1500 changed lines. The feature exceeds it, so it ships as several PRs. Each phase below is one PR (Phase 4 and Phase 2 may split) and carries its own `Size:` estimate and its own tests at realistic per-file targets; re-measure at brief time.

| PR | Phase | Closes | Size (estimate) |
|----|-------|--------|-----------------|
| 1 | Resolution model, document registration, error codes (implemented against the old design: see "Rework needed"; PR #3169 is already 15 files) | `Refs #3049` | 15 files, ~950 lines (as implemented) |
| 2 | Non-throwing field read, `UnreadablePlaceholder`, `IsUnreadable` (the value-based store rule is removed) | `Refs #3049` | 15 files, ~500 lines (as implemented) |
| 2b | `MetadataJsonMasked` on both read models, set by the projections where they mask, read by the query stores, with its tests | `Refs #3049` | 12 files, ~450 lines |
| 3 | Projection lookup and apply (latest resolution wins), non-throwing key classification | `Refs #3049` | 14 files, ~850 lines |
| 4a | Resolution item, batch, preview and result types, limits, `ResolutionPermission`, whole-batch validation, hash serializer, effective-resolution evaluator, and their tests | `Refs #3049` | 15 files, ~1,200 lines |
| 4b | Access gate and `Denied` entries, `GetAsync` and `ListAsync`, DI registration | `Refs #3049` | 11 files, ~700 lines |
| 4c | Atomic `RecordAsync` with the staging method on `MartenOperationAuditStore`, and the integration tests | `Refs #3049` | 11 files, ~900 lines |
| 5 | Observability and cross-cutting integration | `Refs #3049` | 9 files, ~450 lines |
| 6 | Full-flow tests (integration, property, contract) and justification files | `Refs #3049` | 9 files, ~800 lines |
| 7 | Docs, ADR-061, changelog | `Fixes #3049` (and `Fixes #2770` when #3049 is its last open child) | 7 files, ~450 lines |

**Phase 3 gate (implementer check, D15) — findings recorded now.** The resolution documents and the audit entries can share one Marten session and database: `MartenOperationAuditStore`, `MartenTemporalKeyProvider` (and, through it, `AuditEventEncryptor`) all take the scope's `IDocumentSession`. Three facts shape the design and are part of the plan (checked in the code on 2026-11-04): (1) `MartenOperationAuditStore.RecordAsync` saves by itself, so Phase 4c adds an internal staging method that encrypts and appends without saving; (2) `MartenTemporalKeyProvider.GetOrCreateKeyAsync` creates a missing period key and saves the session, so the resolver warms the keys of every period of the batch **before** it stores the first document (a key created for the current period is an infrastructure artifact, not part of the all-or-nothing batch; only a destroyed marker makes staging fail, which is how the "audit staging fails, nothing persists" test is built); (3) a failed `SaveChangesAsync` leaves its pending operations in a shared scoped session and the store saves by itself, so the resolver opens its **own scope per call** (`IServiceScopeFactory`, as `PersistentPolicyAdministrationPoint` does for the same reason) and resolves the store, the key provider and the session from it; the scope is disposed after the call, and the `Denied` entry uses a separate scope. The implementer still confirms in Phase 4c that nothing in a batch is written to another store; if it is, report it, because atomicity cannot then be claimed.

### Rework needed in Phases 1 and 2 (implemented against the old design)

| Phase | What must change | Why |
|-------|-----------------|-----|
| 1 (PR #3169) | `AuditResolution.Id` becomes a per-resolution `Guid` (UUID version 7 from `TimeProvider`), no longer the deterministic string `audit-resolution:{trail}:{eventId}:{field}`; add `Sequence` (`long`, per trail and event) and `Supersedes` (list of ids) | Resolutions are append-only; nothing is overwritten (D14, D17) |
| 1 | `AuditResolutionIds.Format` and its round-trip test go; keep the valid field set per trail (`IsValid(trail, field)`) and add `NewId(TimeProvider)` with a test that ids created in different milliseconds sort by creation time (ids in the same millisecond are not ordered: `Sequence` decides) | Same |
| 1 | `AuditResolution`: the old `AuditEntryId` is renamed `EventAuditEntryId` (the poison event's payload id) and `ResolutionAuditEntryId` is added; the positional `AuditResolutionRecord` gains `Id`, `Sequence` and `Supersedes` | Append-only shape, evidence link |
| 1 | `AuditResolutionKind` gains `Withdrawn = 4` | "No resolution": returns the event to unresolved (D17) |
| 1 | `MartenAuditErrors`: drop `ResolutionConflict` (and any not-found or removal code); add `ResolutionForbidden` (`audit.marten.resolution_forbidden`); keep `ResolutionPrincipalRequired`, `InvalidResolutionReason`, `ResolutionEventNotFound`, `InvalidResolutionField` (`ResolutionWriteFailed` is added in Phase 4) | D13, D14, D17 |
| 1 | `ConfigureMartenOperationAuditProjections`: `Schema.For<AuditResolution>()` with a `Guid` identity and an index on (`Trail`, `EventId`) | The projection loads an event's resolutions by that key |
| 1 | `ConfigureMartenOperationAuditProjections` test: assert that the index on (`Trail`, `EventId`) exists in the generated DDL, not only that the table exists | The "indexed lookup" claim must be proven |
| 2 (branch `feat/3049-p2-nonthrowing-read`) | Remove the value-based placeholder rule and the "built-in defaults count as placeholders" interim rule in the two query stores, with their tests (the stores return to their current behaviour until PR 2b); rewrite the `UnreadablePlaceholder` option remark that says the query stores treat a metadata value equal to a placeholder as a placeholder | D16 = A supersedes them |
| 2b (new PR) | Add `bool MetadataJsonMasked` to **both** read models, set by the projections where they already mask `MetadataJson` (shredded, expired), and make the two query stores read the flag and never infer masking from the text. Tests: rows queried after the placeholder options change still read; the flag is set for shredded and expired and only when `MetadataJson` itself is substituted; stores parse `MetadataJson` only when the flag is false. Rows projected before the flag exist only after a rebuild (pre-1.0, no migration) | D16 = A; kept out of PR 2 because PR 2 is already 15 files |
| 4a | `MartenOperationAuditOptions.ResolutionPermission` (default `audit:resolve`; blank rejected at startup validation; no opt-out property) moves to Phase 4a, where it is used | Keeps Phase 1 and 2 within the size rule |

---

## Design Choices

Maintainer decisions are quoted from the comments of #2770 and #3049 and recorded dated in the last section. The decision register with the earlier options is `artifacts/decisions/2770-2b.md` (ids D1-D56, DL1-DL9); the orchestrator screening is `artifacts/decisions/2770-2b-screen.md`. Both are working files; each block below carries the citation that makes it checkable. Choices 1 to 3, 6, 7, 10, 11, 13, 18 and 19 are Settled, choices 16 and 17 are Trivial; choices 4, 5, 8, 9, 12, 14 and 15 were Dilemmas and the maintainer answered them (2026-10-30 and 2026-11-04); no Dilemma is open.

<details>
<summary><strong>1. Storage model — append-only per-resolution documents, latest wins, same Marten store</strong></summary>

Register: D1, D2, D3, D4, D6; reworked by D14 = D and D17 = D of 2026-11-04.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One immutable `AuditResolution` document per recorded resolution (`Guid` id, UUID version 7), carrying `Sequence` and `Supersedes`; the projection loads the event's resolutions with one indexed query on (`Trail`, `EventId`) ordered by `Sequence` then `Id` descending, on the failure path only; same store and schema as the keys and markers, registered in `ConfigureMartenOperationAuditProjections`** | Append-only like an audit log; history shows who changed what and when; no overwrite and no delete, so no concurrency machinery; one query per failing event; deterministic on rebuild | A document can still be edited in SQL (choice 6) |
| B) The same documents plus a "latest pointer" document per key | A primary-key load finds the latest | A second write per resolution that must stay consistent with the first; an extra document to tamper with |
| C) One mutable document per key (the previous design) | One row per key | Overwrites history; needs conflict and removal machinery that D14 and D17 rejected |

### Chosen Option: **A — immutable documents, indexed latest-wins lookup**

### Rationale

- The trail (`Operation` or `Read`) is part of every document and of the index because there are two event types, two projections and different field sets.
- `Sequence` is assigned inside the writing transaction as the highest sequence of the event plus one; a tie (two sessions at once) is broken by `Id`, so the order is total and rebuild-stable. UUID version 7 ids sort by creation time.
- The pointer (B) was weighed and rejected: the maintainer delegated the choice ("pick the cleaner and justify") and the indexed query needs no second write and no second consistency rule.
- `AuditResolutionField` is a closed enum named after the read-model properties (Operation: `UserId`, `IpAddress`, `UserAgent`, `RequestPayload`, `ResponsePayload`, `MetadataJson`; Read: `UserId`, `Purpose`, `MetadataJson`) plus `All`; a field that does not belong to the trail is rejected.

**Class:** Settled
**Settled by:** Maintainer decisions D14 = D and D17 = D on #3049 (comment of 2026-11-04); Q2 comment 6395857025 on #2770 ("a resolution document in Marten"); ADR-020 section 6 (keys in the same PostgreSQL database)
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>2. Event identifier — Marten `IEvent.Id`</strong></summary>

Register: D5 (screen D24).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marten event id (`IEvent.Id`, a `Guid`)** | It is what the daemon log prints (`Failure to apply event #<sequence> Id(<guid>)`); unique and rebuild-stable; the resolver can check existence by primary key | `Create` takes `IEvent<T>`, so unit tests that call `Create(payload, ...)` change |
| B) The payload's audit entry `Id` | Matches query results | A poison event has no read-model row, so the operator must read `mt_events` JSON to learn it; not a primary key of `mt_events` |
| C) The event sequence number (`long`) | Short | A store-internal counter, easily confused with stream version |

### Chosen Option: **A — Marten `IEvent.Id`**

### Rationale

- Only A is operable for a poison event. The payload `Id` is stored in the document as informational data (`EventAuditEntryId`), and log message 2593 prints the event id, the sequence number and the payload id.
- Condition proven in Phase 3: `Create(IEvent<T>, IDocumentOperations, CancellationToken)` works with Marten 9.38 (see the day-1 experiment).

**Class:** Settled
**Settled by:** Q2 comment 6395857025 (keyed by "event"); maintainer decision D24 = (b) on #2770 (comment 6521533698, not vetoed); runbook `docs/features/audit-marten.md` (the daemon prints the event guid)
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>3. Resolution kinds — `Unreadable`, `Shredded`, `Expired`, `Excluded`, `Withdrawn`; exclusion means no row</strong></summary>

Register: D24 (screen D25), D26 of the register, D19, D56; reworked by D17 = D.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Five kinds; `Shredded` needs a valid `ErasureReference`, `Expired` a valid `RetentionReference`; `Excluded` makes the projection yield no row; `Withdrawn` ("no resolution") returns the event to unresolved, so a rebuild pauses on it again and the effective state reads as pending** | Accurate legal labels (Art. 5(2)); undo without deleting (D17); exclusion works with the store-wide skip flags `false` and is identical on every rebuild | One more kind than Q2 listed |
| B) Q2's four only, with a delete to undo | Strict wording | The operator can only pick `[SHREDDED]` (forbidden without reference by Q1) or `[UNREADABLE]`; undo needs a delete that D17 rejected |
| C) Use Marten's own skip or dead-letter for exclusion | Native | Needs the skip flags `true`, which D22 = (e) forbids; not rebuild-stable |

### Chosen Option: **A — five kinds**

### Rationale

- A resolution applies only where a failure would occur; if the cause was repaired the event reads fine and the stale resolution is harmless (D19), so a planted `[SHREDDED]` cannot hide readable data.
- With `AllowSkippedProjectionErrors = true` the lookup still runs before the throw; an unresolved failure is then skipped silently, which the runbook states (D56).
- Exclusion is the exception: ADR-020 prefers rows; the runbook says so. **Unverified Marten 9.38 behaviour, proven by an integration experiment in Phase 3**: `EventProjection.Create` may return `null` without error and the shard's progress advances by event sequence. Fallback if not: a `Project(IEvent<T>, IDocumentOperations, CancellationToken)` method that calls `Store` only when the event is not excluded.

**Class:** Settled
**Settled by:** Q3 comment 6413750564 and Q1 comment 6395833133 on #2770; maintainer decisions D25 = (b) (comment 6521533698) and D17 = D on #3049 (2026-11-04); Q2 comment 6395857025 (exclude the event, deterministic on rebuild) and D22 comment 6440026324 (the store-wide skip flags stay `false`); AGENTS.md section 3 (fail closed). That a `null` return from `Create` excludes cleanly is the day-1 experiment's responsibility
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>4. Granularity — event + field only, with a batch operation</strong></summary>

Register: D7 (screen D23, DL1).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A) Event + field plus an enumerating `ResolvePeriodAsync` helper | Literal Q2 key | One document and one audit entry per event of a period: volume and noise |
| B) Add a period scope to the same document type | One step for a lost key of a month | Wider blast radius, extends Q2's key |
| C) Event + field only; fix period-wide causes on the marker (audited attach, never overwrite) | Marker stays the single source of why a key is gone | Edits the marker, which D15 of #2770 wanted untouched |
| **D) Event + field only, with the batch operation of choice 9; the period-wide case is #2910** | Q2 key kept; since Q3 new retention purges write a reference, so the remaining period-wide causes are a lost key or a damaged marker | #2910 covers only attaching a reference to an existing marker; a lost key with no marker is resolved event by event with a batch (the runbook and ADR-061 say so) |

### Chosen Option: **D — event + field only, with a batch; period case is #2910 (maintainer decision 2026-10-30)**

### Rationale

- The maintainer chose (d) on 2026-10-30. A period scope is reopened only if real data needs it.

**Class:** Dilemma
**Settled by:** Maintainer decision D23 = (d) on #2770, comments 6521533698 and 6522969450, 2026-10-30
**Verified:** n/a (answered by the maintainer)

</details>

<details>
<summary><strong>5. Several resolutions for one event — the most recent wins, as an append-only record</strong></summary>

Register: D27 (screen D26, DL5) replaced by D14 = D of 2026-11-04.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A) Reject a different kind for the same key, idempotent identical repeat | Write-once principle | Needs conflict machinery for a case the maintainer judged unrealistic |
| B) Last write overwrites one row | Simple | Loses history |
| C) Versioned rows with an explicit conflict rule | History | Complexity for little gain |
| **D) The most recent resolution wins, per field; resolutions are an append-only record of operator decisions; a newer one amends the earlier ones it covers (`All` covers every field, a field-specific one its field, `Excluded` excludes the event, a later non-excluding resolution brings it back); each new resolution records the ids it supersedes; a duplicate is recorded twice and the latest wins; no idempotency key, no identical-repeat rejection, no conflict code** | Matches the realistic case (an amendment, not two operators disagreeing); full history of who changed what and when; no concurrency special cases | Encina is not designed for several operators working at once; a tie between two sessions is broken by `Id` |

### Chosen Option: **D — most recent wins (maintainer decision 2026-11-04)**

### Rationale

- Effective resolution of a field `F` is the one with the highest `Sequence` among the event's resolutions whose field is `F` or `All`; a `Withdrawn` effective resolution means unresolved. The event is excluded when the highest-sequence resolution of the event, of any field, is `Excluded`; any later resolution brings it back, and the fields it does not cover are then judged by their own effective resolution (an uncovered failing field pauses the shard, so `All` is the normal scope after an exclusion).
- `Supersedes` of a new resolution lists the ids of the effective resolutions it covers when written (`All`: every effective one of the event; a field: the effective one of that field; `Excluded`: every effective one), so the history is auditable without reading the whole set.

**Class:** Dilemma
**Settled by:** Maintainer decision D14 = D on #3049 (comment of 2026-11-04)
**Verified:** n/a (answered by the maintainer)

</details>

<details>
<summary><strong>6. Tamper evidence — accept and document, SHA-256 in the audit entry</strong></summary>

Register: D31, D32 (screen D27, DL6).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Accept and document; store the SHA-256 of the canonical resolution fields in the audit entry (`RequestPayloadHash`); the projection never trusts `ResolvedBy`** | Same trust level as keys, markers, events and read models in the same database; no key management | An insider with database write and no application access can forge a document |
| B) HMAC per document under an `IKeyProvider` key | Separation of duties | A new key, rotation and a new failure mode; protects a weaker link than the unprotected markers beside it |
| C) Append-only stream plus document | Event-store convention | Departs from Q2's "document"; same SQL-edit weakness |

### Chosen Option: **A — accept, document, hash in the audit entry**

### Rationale

- Canonical fields, every one that decides the projected result or names the actor and the time (the informational links `TenantId`, `EventAuditEntryId` and `ResolutionAuditEntryId` are not hashed: the last is the id of the entry that carries the hash, and the others are copied from the event): format version (`v1`), resolution id, trail, event id, field, kind, `Sequence`, the superseded ids (sorted, comma separated), the whole applicable reference (erasure: request id, request date, actor; retention: policy id, cutoff date, execution date, actor; absent parts are empty), the resolver identity (`ResolvedBy`) and the resolution time (`ResolvedAtUtc`). Serialization (fixed, so an auditor recomputes it): one `name=value` entry per field in the order above, joined with a line feed (`\n`) and encoded UTF-8; each value is written as `<length>:<text>` (length in UTF-16 code units, so no value can imitate a separator); the event id and the resolution id use the lowercase `D` Guid format; dates are `ToUniversalTime()` first and UTC ISO 8601 round-trip (`O`); enums are written as their invariant names; the digest is SHA-256 in lowercase hexadecimal, stored in `RequestPayloadHash`. The entry names are `v`, `id`, `trail`, `event`, `field`, `kind`, `sequence`, `supersedes`, `erasure.request`, `erasure.requestedAt`, `erasure.actor`, `retention.policy`, `retention.cutoff`, `retention.executedAt`, `retention.actor`, `resolvedBy`, `resolvedAt`.
- Absent and empty values both serialize as `0:`; this is not ambiguous in practice because an optional reference part is either absent as a whole or valid (`IsValid` rejects blank parts); the property generators for the hash tests build only valid references and non-empty identities, with the absent-versus-empty case named as the one exception in a separate test. Tests (Phase 4): a golden vector, and a property test that changing any single field, including each reference date and actor, changes the hash.
- The resolution document stores the id of its audit entry (`ResolutionAuditEntryId`), so a document and its evidence can be matched. A package-wide integrity design (keys, markers, resolutions together) is a separate spike, opened only if SPEC-002 needs it.

**Class:** Settled
**Settled by:** Maintainer decision D27 = (a) on #2770 (comment 6521533698); principle C1b of #2770 (Encina limits itself to Encina); `OperationAuditEntry.RequestPayloadHash` (tamper detection); ADR-020 amendments of 2026-10-26
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>7. Public API shape — one write operation, a preview, reads; ambient actor; registration</strong></summary>

Register: D8, D9, D10, D11, D13 (screen S2, S3, S4, DL8, DL9); reworked by D15 = E and D17 = D.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RecordAsync(batch, mode, ct)` as the only write operation, `PreviewAsync(batch, ct)` (the validation step alone), `GetAsync` and `ListAsync`; items are built by per-kind factories (`Shredded` cannot omit its reference); `Either` returns with a trailing `CancellationToken`; actor from `IRequestContextAccessor`; registered by `AddEncinaAuditMarten` (scoped, `TryAdd`) with the accessor; no CLI or endpoint** | One write path to audit, test and gate; compile-time safety through the item factories; actor not forgeable by a parameter | A batch type for the single case |
| B) One method per kind, plus a bulk helper | Familiar | Several write paths; a second implementation of batching |
| C) One `ResolveAsync(request, ct)` with runtime validation | Small interface | Illegal combinations caught at runtime only |

### Chosen Option: **A — one write operation**

### Rationale

- The actor (user id and tenant) is read once from the ambient identity; background or CLI hosts open `IRequestContextScopeFactory.RunAsServiceAsync`. Missing or anonymous principals and a missing permission are handled by choice 8.
- `AddEncinaAuditMarten` already registers `ITemporalKeyProvider` unconditionally; a scoped service costs nothing until used.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming; registration completeness with a `ValidateOnBuild` test); maintainer decisions D17 = D and D15 = E on #3049 (2026-11-04: one write operation, batch form); `src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs` (`TryResolveActor`, actor read once); Q2 comment 6395857025
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>8. Who may call the resolver — a required permission, no opt-out</strong></summary>

Register: D12 (DL7, screen S1); decided D13 = C of 2026-11-04.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A) Host-side authorisation (the sibling convention) | No new vocabulary | The package default is fail-open for any authenticated caller, and the service can hide audit evidence |
| **C) A required permission, `audit:resolve`, name configurable through `MartenOperationAuditOptions.ResolutionPermission`, checked against `RequestIdentity.Permissions`; no opt-out property; anonymous callers and callers without the permission are denied** | Secure by default; a service identity with the declared permission still works (CLI, background hosts); the identity cannot be minted by host code | A package-level vocabulary string; tests need a permissioned identity |
| D) A replaceable authorizer, permission based by default | Pluggable policy | The largest public API |

### Chosen Option: **C — required permission (maintainer decision 2026-11-04)**

### Rationale

- Codes: `audit.marten.resolution_principal_required` (anonymous or no context) and `audit.marten.resolution_forbidden` (authenticated, permission missing). The check runs first in every public method, including `PreviewAsync`, `GetAsync` and `ListAsync`, before validation or any store access. A blank `ResolutionPermission` fails startup validation. The errors carry no identity or permission text.
- Each denial is audited under choice 15.

**Class:** Dilemma
**Settled by:** Maintainer decision D13 = C on #3049 (comment of 2026-11-04)
**Verified:** n/a (answered by the maintainer)

</details>

<details>
<summary><strong>9. Batch semantics — validate everything, then all or nothing in one transaction</strong></summary>

Register: new; decided D15 = E of 2026-11-04.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A) Stop at the first failure; earlier events stay resolved | Fail fast | Partial results; one problem per run |
| B) Continue and return per-event results | One pass shows every problem | A partial write can look like success |
| C) The hybrid: business rejections per item, infrastructure failures abort | Both scenarios served | Still a partial write |
| **E) Validate the whole batch first (business rejections per item with the error code only; infrastructure failures abort; this step alone is `PreviewAsync`, a dry run with no write); with zero errors write every resolution and its audit entries in one atomic transaction (one Marten session, one `SaveChangesAsync`), repeating the validation inside the unit of work; any failure rolls the whole batch back; with errors nothing is written and the per-item report is returned; applying only the valid subset needs an explicit parameter (`AuditResolutionApplyMode.ValidSubsetOnly`) and that run writes its own audit entry listing the applied and skipped events with their error codes; never silent, never the default** | No partial state; the operator sees every problem before anything changes; premeditated subset | The audit entries must be staged into the resolver's own session (see below) |

### Chosen Option: **E — validate, then all or nothing (maintainer decision 2026-11-04)**

### Rationale

- Result of `RecordAsync`: `Right(AuditResolutionBatchResult)` with an outcome (`Written`, `WrittenValidSubset`) and per-item results by error code only. A batch rejected by validation in `AllOrNothing` mode returns `Left` `audit.marten.resolution_batch_rejected` whose details carry the per-item report (item index to error code, codes only), so a caller that matches only `Right` can never read a rejected batch as success (the failure pattern of #1150-#1153); `PreviewAsync` returns the same report as `Right`. `Left` also for infrastructure failures, a denial or an invalid batch (blank or over-limit reason, empty batch, more than `AuditResolutionLimits.MaxBatchSize` items). This delivers the per-item report of D15 = E through the error details; the maintainer is told in the report, it is not a new option.
- Per-item business rejections: field not valid for the trail, `Excluded` with a field other than `All`, invalid erasure or retention reference, event not found or not of the trail's type. Validation failures, including every dry-run report, go to the log by error code only.
- **Single transaction design.** The resolver opens its own scope per call (`IServiceScopeFactory`, as `PersistentPolicyAdministrationPoint` does for the same reason: a failed save leaves its pending operations in a shared scoped session, and the audit store saves by itself); inside that scope `MartenOperationAuditStore`, `AuditEventEncryptor` and `MartenTemporalKeyProvider` take the scope's one `IDocumentSession`. `IOperationAuditStore.RecordAsync` saves by itself, so the resolver cannot call it inside the batch; Phase 4c adds an internal staging method on `MartenOperationAuditStore` that encrypts and appends the audit event to a given session without saving. The resolver first warms the period keys of the whole batch (`GetOrCreateKeyAsync` creates a missing key and saves the session, so this must precede the first `Store`; a key created for the current period is an infrastructure artifact, not part of the all-or-nothing batch), then stores all documents, stages all entries and calls `SaveChangesAsync` once. If the audit entry cannot be staged (the period's key was destroyed: only a destroyed marker makes the key provider return `Left`) the batch fails and nothing is persisted: audit-first fail closed, without any intent or completion machinery. The scope is disposed after the call, so a failed batch leaves nothing behind for later calls.
- Gate: the findings and the implementer check of the "Phase 3 gate" paragraph.

**Class:** Dilemma
**Settled by:** Maintainer decision D15 = E on #3049 (comment of 2026-11-04)
**Verified:** n/a (answered by the maintainer)

</details>

<details>
<summary><strong>10. Projection control flow — failure-path lookup through `IDocumentOperations`, a non-throwing internal read</strong></summary>

Register: D14, D15, D16, D17, D18, D20 of the register.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The key lookup returns a `TemporalKeyOutcome` (`Key`, `Shredded`, `Expired`, `Failure(reason)`) and an internal non-throwing read returns an outcome per field; the projection decides, loads the event's resolutions (one indexed query) only when the key or a field failed, applies them, and throws the original typed failure when none covers a failing field; public `Decrypt` keeps throwing** | No exceptions as business logic; zero extra queries on the healthy path; deterministic | `EncryptedField` parsing is refactored |
| B) Catch `EncryptedFieldDecryptionException`, then look up and re-map | Small diff | Exceptions as control flow; `MapToReadModel` is synchronous while the lookup is asynchronous |
| C) Pre-load all resolutions of the event on every event | Simple | One more query on the healthy path |

### Chosen Option: **A — non-throwing read plus failure-path lookup**

### Rationale

- Constructor injection only; no `IServiceProvider` (Marten projection rule).
- If the lookup itself fails (for example a PostgreSQL error) the exception propagates and the shard pauses; it is never read as "no resolution" (D20).
- The applied outcome is a pure function of the event and the stored resolutions (kind, reference, sequence, placeholders from options): no clock, no actor and no random value reach the read model.

**Class:** Settled
**Settled by:** AGENTS.md section 3 ("Marten projections take dependencies through `IDocumentOperations` and constructor injection, NEVER `IServiceProvider`"; "NEVER use exceptions for business logic"; errors never swallowed); ADR-006; D14 and D15 comments on #2770 (6395444785, 6395499536)
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>11. Audit entries, reason handling, tenant and time</strong></summary>

Register: D28, D29, D30, D32, D33, D34; reworked by D15 = E.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `OperationAuditEntry` per recorded resolution, staged into the same session and committed with the documents (choice 9); `Action` `ResolveUnreadableAuditEvent`, `EntityType` `AuditEvent`, `EntityId` the event id, `UserId`, `TenantId` and `CorrelationId` from the ambient context, `ErrorMessage` empty on success, `RequestPayloadHash` the hash of choice 6, `Metadata` with trail, field, kind, reference identifiers, supersedes ids and the reason; the reason is required, non-blank, bounded (choice 17) and only in the encrypted entry; `TimeProvider` injected; the document copies the event's `TenantId`** | Fail closed and atomic; the reason is shreddable with its period (GDPR Art. 5(1)(c), 5(1)(e)); testable time | The entry is encrypted with the current period key, so a destroyed current period key also refuses the batch |
| B) Document first, audit second | Simpler | A crash leaves an unaudited resolution |
| C) Intent entry first and a linked completion entry afterwards | Works when the audit store is a different store | Two entries per resolution and a reconciliation rule; unnecessary when one session commits both |
| D) Reason also stored in the document | Easier to list | Plaintext operator text kept beyond retention |

### Chosen Option: **A — one entry per resolution in the same atomic commit**

### Rationale

- A subset run (`ValidSubsetOnly`) adds one more entry, `Action` `ResolveUnreadableAuditEvent.Batch`, listing the applied and skipped event ids with their error codes (no free text).
- There is no tenant-isolation check: the shard is global across tenants and the operator is a platform role.
- The document stores `ResolvedBy` and `ResolvedAtUtc` (informational, never trusted by the projection) and never the reason.

**Class:** Settled
**Settled by:** AGENTS.md section 6 function 12 and section 3 (fail closed; `TimeProvider`); Q2 comment 6395857025 ("recorded in the audit trail"); maintainer decision D15 = E on #3049; GDPR Art. 5(1)(c) and 5(1)(e); ADR-020 section 2
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>12. Reading `MetadataJson` — a persisted `MetadataJsonMasked` flag</strong></summary>

Register: new; decided D16 = A of 2026-11-04.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A persisted `bool MetadataJsonMasked` on both read models, set by the projection whenever it masks `MetadataJson`; the query stores parse `MetadataJson` only when it is false and never infer masking from the text** | Independent of any option value; works for field-specific resolutions; no heuristic | A new public read-model property; rows projected before it exist only after a rebuild (pre-1.0, no migration) |
| B) A value rule over the current options (plus the built-in defaults) | No new property | Breaks when a placeholder changes after rows were written |
| C) Parse defensively and treat a failure as the stored text | No schema change | Swallows a real corruption error (AGENTS.md section 3) |

### Chosen Option: **A — persisted discriminator (maintainer decision 2026-11-04)**

### Rationale

- The flag is introduced in Phase 2, where the read models and the query stores change, together with the existing masking (shredded, expired) setting it; Phase 3 sets it for resolution placeholders. This replaces the value-based rule and the interim "built-in defaults count as placeholders" rule.

**Class:** Dilemma
**Settled by:** Maintainer decision D16 = A on #3049 (comment of 2026-11-04)
**Verified:** n/a (answered by the maintainer)

</details>

<details>
<summary><strong>13. `[UNREADABLE]` placeholder, `IsUnreadable` flag and typed missing-key reason</strong></summary>

Register: D14 (missing key), D21, D22.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MartenOperationAuditOptions.UnreadablePlaceholder` (const `DefaultUnreadablePlaceholder = "[UNREADABLE]"`) passed to both projection constructors with the same `ThrowIfNullOrWhiteSpace` validation; event-level `bool IsUnreadable` on both read models; the missing key reads as `EncryptedFieldDecryptionFailureReason.KeyMissing` (merged with #3083)** | Mirrors `ShreddedPlaceholder` and `ExpiredPlaceholder`; a blank placeholder cannot project blank text | Constructors change (RS0017 lines replaced) |
| B) Hard-coded literal | Fewer lines | Not configurable like the other two |
| C) Per-field flags | Finer queries | A list of field names for no current use |

### Chosen Option: **A — option, event-level flag, typed reason**

### Rationale

- A resolution targets one field, so `IsShredded`, `IsUnreadable` and `IsExpired` can now be true together; the doc comment "Mutually exclusive with `IsShredded`" on `IsExpired` is amended.

**Class:** Settled
**Settled by:** D15 comment 6395499536 on #2770 (`[UNREADABLE]` = operator-acknowledged unreadable data); precedent `MartenOperationAuditOptions.ShreddedPlaceholder` and `ExpiredPlaceholder`; AGENTS.md section 3 ("Every line serves a current purpose")
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>14. No removal — resolutions are immutable</strong></summary>

Register: new; decided D17 = D of 2026-11-04 (replaces the removal-mechanism options: token-conditional delete, Marten version check, lock).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A) A token-conditional delete (raw SQL with a row count) | A definite per-attempt outcome | Raw SQL on Marten internals; a mechanism to prove |
| B) Marten's own version check | No raw SQL | Unproven on delete |
| C) A distributed lock | Serialises removers | A lock provider and a failure mode |
| **D) No removal at all; to undo or change a resolution the operator records a new one; the kinds include `Withdrawn`, which returns the event to unresolved (a rebuild pauses on it again, fail closed; queries show it as pending, not as erased)** | Nothing is edited or physically deleted, so A to C are not needed; a complete history | A mistaken resolution stays in the history |

### Chosen Option: **D — immutable resolutions (maintainer decision 2026-11-04)**

### Rationale

- There is no `RemoveAsync`, no revoke method, no deletion token, no conditional delete and no not-found or double-removal code. Duplicate writes follow choice 5 (recorded twice, latest wins).

**Class:** Dilemma
**Settled by:** Maintainer decision D17 = D on #3049 (comment of 2026-11-04)
**Verified:** n/a (answered by the maintainer)

</details>

<details>
<summary><strong>15. Auditing rejected attempts — only denials and anonymous calls, as one `Denied` entry</strong></summary>

Register: new; decided D20 = B adjusted of 2026-11-04.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A) Only attempts that reach the write | Fewer entries | A denied or anonymous attempt on an evidence-hiding service leaves no audit trace |
| **B, adjusted) Audit security-relevant rejections as one `Denied` entry with the error code and the actor (user id, or "anonymous") and no free text: a permission denial (choice 8) and an anonymous call; validation failures, including every dry-run report, go to the log by error code only** | Attempts against evidence are traceable; low noise; an anonymous call on a health-data application signals an attack or a misconfigured host | One more entry type |
| C) Audit every rejection | Complete trail | Noise from caller bugs |

### Chosen Option: **B, adjusted — denials and anonymous calls (maintainer decision 2026-11-04)**

### Rationale

- The denial entry is written in its own commit (the batch transaction never started): `Action` `ResolveUnreadableAuditEvent`, `Outcome` `Denied`, `ErrorMessage` the error code, `UserId` the identity or `anonymous`, `TenantId` from the context when present, no `Metadata`. If the denial entry cannot be written, the caller still gets the denial `Left` and the write failure is logged by code.
- The "identical repeat" and "removal of a missing resolution" rejections of the earlier design no longer exist (choices 5 and 14).

**Class:** Dilemma
**Settled by:** Maintainer decision D20 = B (adjusted) on #3049 (comment of 2026-11-04)
**Verified:** n/a (answered by the maintainer)

</details>

<details>
<summary><strong>16. Observability and logging — counters, one activity, EventIds 2593-2598 in the existing `AuditMarten` range</strong></summary>

Register: D36, D38, D39.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Counter `encina.audit.marten.resolutions` (tags `kind`, `trail`) and `encina.audit.marten.resolution_denials` (tag `reason`, the error code) in `MartenAuditMeter`, activity `AuditMarten.Resolve` in `MartenAuditActivitySource`, `[LoggerMessage]` 2593-2598 in `MartenAuditLog`, codes only** | The package already has the meter, the activity source and the range; nothing to register | Block 2590-2599 is labelled "Startup validation" in the log block table (relabel it); 2599 stays free |
| B) A new EventId range | Room to grow | Not needed for six messages; the range cannot grow past 2599 |

### Chosen Option: **A — extend the existing meter, activity source and log block**

### Rationale

- Messages: 2593 `UnreadableEventDetected` (Error, logged by the projection before it rethrows), 2594 `ResolutionApplied` (Debug), 2595 `ResolutionsRecorded` (Information, batch size and kinds), 2596 `ResolutionRejected` (Warning, validation code, also for a dry run), 2597 `ResolutionWriteFailed` (Error, code), 2598 `ResolutionDenied` (Warning, code).
- Never the reason, the actor or free text; exceptions go through `ForLogging()`. The activity needs an error path: the existing `Complete` always sets `Ok`.

**Class:** Trivial
**Settled by:** AGENTS.md section 7 (`EventIdRanges.AuditMarten = (2550, 2599)`, pack sequentially) and section 6 functions 2 and 3; `src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs` block table
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>17. Limits — reason of 500 characters and a maximum batch size, as public constants</strong></summary>

Register: D30 (limit value), PR #3085 review; the batch bound follows D15 = E.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A public static type `AuditResolutionLimits` with `ReasonMaxLength = 500` (checked on the trimmed text, `Left` when exceeded) and `MaxBatchSize` (proposal: 1,000, so that one transaction stays bounded); no options** | Follows the package-wide convention of named limit constants for operator text; no configuration surface without a current purpose | Changing a limit is a code change |
| B) Options properties | Tunable | Options with no current need |

### Chosen Option: **A — constants**

### Rationale

- The numbers are implementation parameters, not derived from a rule; the GDPR minimisation rule (Art. 5(1)(c)) only requires that the reason stays in the encrypted, shreddable entry.

**Class:** Trivial
**Settled by:** AGENTS.md section 3 ("Every line serves a current purpose") and the repository convention of named limit constants for operator text, such as `BreakTheGlassLimits.CloseReasonMaxLength` (256) and the justification length limit of `BreakTheGlassOptions`; GDPR Art. 5(1)(c); issue #3049 (reason at most 500 characters)
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>18. `Excluded` is accepted only with field `All`</strong></summary>

Register: D26 detail (PR #3085 review).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AuditResolutionItem.Excluded(trail, eventId)` has no field; the validation rejects an `Excluded` item with a field other than `All`; a hand-written document that violates it fails closed at read time** | Exclusion means "no row", which is event-level | None |
| B) Field-level exclusion | Finer control | A read-model row cannot omit one field; nulling it is indistinguishable from a legitimately null field, which is silent data loss |

### Chosen Option: **A — event-level exclusion only**

### Rationale

- Field-level marking is already `Unreadable`, `Shredded` or `Expired`.

**Class:** Settled
**Settled by:** Q2 comment 6395857025 on #2770 ("exclude the event"); AGENTS.md section 3 (errors are never swallowed; fail closed)
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>19. Delivery — several PRs, tests per flag in each, ADR-061, runbook</strong></summary>

Register: D49-D55.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) PRs of at most 15 files and 1500 lines, each carrying the unit, guard, contract and property tests of its own files at realistic per-flag targets and integration tests where Marten is needed; load and benchmark justified in `.md` files; ADR-061 plus a pointer in ADR-020; runbook updated by `docs-writer`; the last PR closes the issue** | Follows the size rule; each PR reviewable; coverage measured per PR | More review cycles |
| B) One PR | One review | Breaks the size rule |

### Chosen Option: **A — several PRs**

### Rationale

- ADR-061 is the next number that is neither used nor reserved (checked 2026-11-04): `origin/main` has ADRs up to 058 and 060, and `docs/architecture/adr/index.md` reserves 059 for "#2770 (PR 2b): AES-GCM decrypt sites", a different topic that this plan neither writes nor repurposes. The index carries the 061 reservation row (plain path text, no link, because `docs/_config.yml` excludes `plans/` from the site); Phase 7 replaces it by the ADR row and re-checks the number.
- Every new or touched `src/` file gets per-flag targets and one-sentence justifications in `.github/coverage-manifest/Encina.Audit.Marten.json`; a target of 0 only where the flag cannot exercise the file; CRAP stays at 10 or less on changed methods (the pure outcome mapping is one `switch` that may carry `// crap-exempt: single-question switch`).

**Class:** Settled
**Settled by:** AGENTS.md section 8 (ADR for architectural decisions; PublicAPI), section 9 (per-flag obligations, CRAP, integration required for database features) and section 11 (changelog fragments); EPIC #2845 size rule (#2900)
**Verified:** adversarial-reviewer 2026-11-04

</details>

<details>
<summary><strong>20. Result shape of a rejected batch — `Left` for `RecordAsync`, `Right` with the report for `PreviewAsync`</strong></summary>

Register: new (PR #3085 review, 2026-11-04); refines the per-item report of D15 = E.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A fully rejected `AllOrNothing` batch is a failure of the operation: `RecordAsync` returns `Left` `audit.marten.resolution_batch_rejected` whose error details carry the per-item error codes (item index to code, codes only); `PreviewAsync` is a query that succeeds and returns `Right` with the per-item report** | Railway Oriented Programming: a caller that matches only `Right` can never read a write that did nothing as success (the failure pattern of #1150-#1153); the report still reaches the operator through the details | The report travels in the error details of the write |
| B) `RecordAsync` returns `Right(report)` with an outcome flag (`RejectedNothingWritten`) | One result type for every case | Success-shaped result for a write that wrote nothing; a caller that ignores the flag treats it as success |

### Chosen Option: **A — `Left` for the write, `Right` for the preview**

### Rationale

- The operation was requested to record resolutions and did not: that is a failure, not a successful report. The preview was requested to report: it succeeds with the report.
- A subset run (`ValidSubsetOnly`) that applies at least one item is a success (`Right`, outcome `WrittenValidSubset`); a subset run with zero valid items writes nothing and is `Left` `audit.marten.resolution_batch_rejected` as well.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming: "operations return `Either<EncinaError, T>` ... validation returns `Either` with detailed errors"; a `Left` "fails that operation ... never reports success") and ADR-001 and ADR-006; precedent `ValidationOrchestrator` (an invalid input is a `Left`) and `EncinaErrors.Create` with a details dictionary; the best-effort `DeadLetterManager.ReplayAllAsync` returns `Right` with per-item results only because it is non-atomic, which this atomic all-or-nothing write is not; confirmation of 2026-11-04 recorded by the orchestrator ("correct, no maintainer decision needed; register as Settled")
**Verified:** adversarial-reviewer 2026-11-04

</details>

---

## Implementation Phases

API proposal (names are proposals derived from the register, namespace `Encina.Audit.Marten.Resolution`; the exact signatures are fixed in each phase's brief):

```csharp
public enum AuditResolutionTrail { Operation = 0, Read = 1 }
public enum AuditResolutionKind { Unreadable = 0, Shredded = 1, Expired = 2, Excluded = 3, Withdrawn = 4 }
public enum AuditResolutionField { All = 0, UserId, IpAddress, UserAgent, RequestPayload, ResponsePayload, MetadataJson, Purpose }
public enum AuditResolutionApplyMode { AllOrNothing = 0, ValidSubsetOnly = 1 }

public sealed record AuditResolutionItem   // built only through factories
{
    public static AuditResolutionItem Unreadable(AuditResolutionTrail trail, Guid eventId, AuditResolutionField field);
    public static AuditResolutionItem Shredded(AuditResolutionTrail trail, Guid eventId, AuditResolutionField field, ErasureReference erasure);
    public static AuditResolutionItem Expired(AuditResolutionTrail trail, Guid eventId, AuditResolutionField field, RetentionReference retention);
    public static AuditResolutionItem Excluded(AuditResolutionTrail trail, Guid eventId);
    public static AuditResolutionItem Withdrawn(AuditResolutionTrail trail, Guid eventId, AuditResolutionField field);
}

public sealed record AuditResolutionBatch(IReadOnlyList<AuditResolutionItem> Items, string Reason);

public interface IAuditProjectionResolver
{
    ValueTask<Either<EncinaError, AuditResolutionPreview>> PreviewAsync(AuditResolutionBatch batch, CancellationToken cancellationToken = default);
    ValueTask<Either<EncinaError, AuditResolutionBatchResult>> RecordAsync(AuditResolutionBatch batch, AuditResolutionApplyMode mode = AuditResolutionApplyMode.AllOrNothing, CancellationToken cancellationToken = default);
    ValueTask<Either<EncinaError, Option<AuditResolutionRecord>>> GetAsync(AuditResolutionTrail trail, Guid eventId, AuditResolutionField field, CancellationToken cancellationToken = default);
    ValueTask<Either<EncinaError, IReadOnlyList<AuditResolutionRecord>>> ListAsync(AuditResolutionTrail trail, Guid eventId, CancellationToken cancellationToken = default);
}
```

- `AuditResolution` (Marten document, public settable properties like `TemporalKeyDestroyedMarker`): `Id` (`Guid`, UUID version 7), `Trail`, `EventId`, `Field`, `Kind`, `Sequence`, `Supersedes` (list of ids), `EventAuditEntryId` (the payload id of the poison event, informational), `ResolutionAuditEntryId` (the id of the audit entry written for this resolution), `TenantId` (copied from the event), flat erasure fields (`ErasureRequestId`, `ErasureRequestedAtUtc`, `ErasureActor`) and retention fields (`RetentionPolicyId`, `RetentionCutoffUtc`, `RetentionExecutedAtUtc`, `RetentionActor`) as in the marker, `ResolvedBy`, `ResolvedAtUtc`. No reason text. It is never updated or deleted.
- `AuditResolutionRecord`: immutable snapshot returned to callers (the document without internals); `GetAsync` returns the effective resolution (a `Withdrawn` one reads as pending), `ListAsync` the full history newest first. `AuditResolutionPreview` and `AuditResolutionBatchResult` carry the batch outcome and per-item results by error code only.
- Validation: batch level (`Left`): blank or over-500-character reason, empty or over-`MaxBatchSize` batch. Item level (reported per item with the code): field not valid for the trail, `Excluded` with a field other than `All`, invalid erasure or retention reference (`MartenAuditErrors.RejectInvalidReference`; the factories throw `ArgumentNullException` for a null reference because `RejectInvalidReference(null)` returns `null`, meaning valid), event not found or not of the trail's type (existence check by `IEvent.Id` against `mt_events`). Access (`Left`, audited as a `Denied` entry): `audit.marten.resolution_principal_required`, `audit.marten.resolution_forbidden`.
- **Key-level outcome**: `LoadTemporalKeyAsync` and `ClassifyTemporalKeyLookup` throw on a key-level failure (no reference on the marker, both references, `KeyMissing`) before any resolution can be consulted. Phase 3 refactors them to return `TemporalKeyOutcome` (internal): `Key(material)`, `Shredded`, `Expired` or `Failure(reason)`. No exception is used for control flow (AGENTS.md section 3, ADR-006); the alternative (one `catch` in the projection that looks up and rethrows) is rejected because it makes the projection catch its own exception around an asynchronous lookup. The projection throws `new EncryptedFieldDecryptionException(reason)` itself only when no resolution covers a failing field. The existing `ClassifyTemporalKeyLookup` unit tests change to assert outcomes.
- **Projection lookup algorithm** (failure path only): the projection computes the key outcome, then for each non-null encrypted field the field outcome (`Value(text)` or `Failure(reason)`, from the Phase 2 non-throwing read; a `Failure` key outcome fails the event, key-first, exactly as today: its reason applies to every field, even when all encrypted fields are null). If neither the key nor a field failed, build the read model with no extra query. Otherwise load the event's resolutions with one indexed query (`Trail`, `EventId`, ordered by `Sequence` then `Id` descending) through the `IDocumentOperations` parameter. Rules, in order: (1) if the highest-sequence resolution of the event with `Field = All` is `Excluded` and no resolution of the event has a higher sequence, the event yields no row; a hand-written `Excluded` under a field other than `All` is not an exclusion and fails closed (typed throw), neither honoured nor ignored; (2) otherwise each failing field takes its effective resolution: the highest sequence among the resolutions of the field and of `All`, **ignoring `Excluded` ones** (an exclusion is event-level state only, so after `Excluded` then `Unreadable(UserId)` the event is back, `UserId` reads `[UNREADABLE]` and any other failing field has no effective resolution and pauses the shard; `All` is the normal scope after an exclusion); (3) `Unreadable`, `Shredded` or `Expired` substitutes the matching placeholder and sets the matching flag, and sets `MetadataJsonMasked` only when `MetadataJson` itself is substituted; `Withdrawn` or no effective resolution makes the projection throw the typed failure (the first failing field's reason) and log 2593 at Error; an applied resolution logs 2594 at Debug (so a rebuild over many resolved events produces no Error noise). A key-level failure applies to every non-null field and is resolved only by `All`, by an exclusion, or by covering every non-null encrypted field; a field-specific resolution on a null field resolves nothing. Determinism: the output depends only on the event, the stored kinds, references and sequences, and the three placeholder strings from the options.
- **Day-1 integration experiment (Phase 3)**: before any other Phase 3 code, extend `AuditProjectionUnreadableEventIntegrationTests` with a throwaway projection that returns `null` from `Create` for a marked event and assert the daemon does not fail and progress passes that event; and that `Create(IEvent<T>, IDocumentOperations, CancellationToken)` is accepted by the Marten 9.38 validator (the class remarks name the non-generic `JasperFx.Events.IEvent` as allowed; the generic form is unverified). If `null` is not accepted, use the `Project`-method fallback of choice 3. Record the result in the PR.
- **Atomic write flow** (Phase 4c): check access (choice 8; a denial writes the `Denied` entry of choice 15 in its own scope and commit and returns `Left`) → validate the whole batch (choice 9; stop with `Left` `audit.marten.resolution_batch_rejected` when it has item errors and the mode is `AllOrNothing`) → open the call's own scope and warm the period keys → inside that unit of work: repeat the validation (an item that became invalid since the preview aborts an `AllOrNothing` batch with the same `Left`, and in `ValidSubsetOnly` is skipped and listed in the `.Batch` entry), apply the items in batch order, loading each event's current resolutions once and assigning `Sequence` incrementally per event in that order (the id is only a tie-break: UUID version 7 ids are not ordered inside one millisecond), set `Supersedes` from the effective resolutions the item covers at that point (including earlier items of the same batch), `Store` each document, stage each audit entry (internal staging method on `MartenOperationAuditStore`, no save), then one `SaveChangesAsync` → on any failure nothing persists and the result is `Left` `audit.marten.resolution_write_failed` (code only; an `OperationCanceledException` is rethrown). There is no fixed 30 s timeout on a batch (the value of `PersistentPolicyAdministrationPoint` is for one audit write); the caller's `CancellationToken` bounds the call, and `MaxBatchSize` is confirmed by a 1,000-item integration test (lowered if it is slow).
- **Query stores**: `MartenOperationAuditStore.MapToAuditEntry` and `MartenReadAuditStore.MapToReadAuditEntry` deserialize `MetadataJson` as JSON; they read `MetadataJsonMasked` (choice 12) and parse only when it is false.

### Phase 1: Resolution Model, Document Registration & Error Codes

> **Goal**: The types every later phase uses, and the table registered before any daemon reads it. **Size**: 13 files, ~450 lines. PR 1 (PR #3169, in review; see "Rework needed"), `Refs #3049`.

<details>
<summary><strong>Tasks</strong></summary>

0. Tests that Phase 1 carries for its own files: a property test of `AuditResolutionIds` (`NewId` ordering by creation time; the valid field set per trail) and unit or contract tests for the enums' explicit values and for `AuditResolutionRecord`, at realistic per-flag targets.
1. `src/Encina.Audit.Marten/Resolution/AuditResolutionTrail.cs`, `AuditResolutionKind.cs` (with `Withdrawn`), `AuditResolutionField.cs`: the enums above, XML docs, explicit values.
2. `Resolution/AuditResolution.cs`: the document; `Resolution/AuditResolutionRecord.cs`: the snapshot record; `Resolution/AuditResolutionIds.cs`: `internal static Guid NewId(TimeProvider)` (UUID version 7) and the valid field set per trail (`IsValid(trail, field)`).
3. `MartenAuditErrors.cs`: codes and factories `ResolutionPrincipalRequired`, `ResolutionForbidden`, `InvalidResolutionReason`, `ResolutionEventNotFound`, `InvalidResolutionField`; messages carry no reason or identity.
4. (Moved: `ResolutionPermission` is added in Phase 4a.)
5. `Projections/ConfigureMartenOperationAuditProjections.cs`: `options.Schema.For<AuditResolution>()` with a `Guid` identity and an index on (`Trail`, `EventId`); a test asserts the index exists in the generated DDL. The existing key and marker documents have no `Schema.For` today, so this registration is new behaviour: the table must exist before the daemon reads it on the failure path, and the task adds a test that the table exists on a fresh store.
6. `PublicAPI.Unshipped.txt` (new public symbols), `.github/coverage-manifest/Encina.Audit.Marten.json` (per-file targets and justifications; `mechanical-fixer`).
7. Tests: `tests/Encina.UnitTests/AuditMarten/AuditResolutionIdsTests.cs`, `AuditResolutionErrorsTests.cs`; `tests/Encina.GuardTests/AuditMarten/AuditResolutionGuardTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Issue #3049, plan docs/plans/audit-projection-resolver-implementation-plan-3049.md, Phase 1 (rework per the plan's "Rework needed" table). Package src/Encina.Audit.Marten (.NET 10, C# 14, Marten, LanguageExt Either). Marten only; no database provider matrix.
TASK: Add the Resolution/ model types (enums incl. Kind.Withdrawn, AuditResolution as an append-only Guid-id document with Sequence and Supersedes, AuditResolutionRecord, AuditResolutionIds), the MartenAuditErrors codes (no conflict or removal codes), the Schema.For<AuditResolution>() registration with an index on (Trail, EventId), PublicAPI.Unshipped lines, coverage-manifest entries and tests (unit, guard, property).
KEY RULES: resolutions are immutable and append-only (never updated or deleted); errors carry codes only (AGENTS.md section 3); no reason text in the document; XML docs on every public member; zero warnings; every new file has per-flag targets with justifications; CRAP <= 10; max 15 files and 1500 lines.
REFERENCE FILES: src/Encina.Audit.Marten/Crypto/TemporalKeyDestroyedMarker.cs, src/Encina.Audit.Marten/Crypto/TemporalKeyDocument.cs, src/Encina.Audit.Marten/MartenAuditErrors.cs, src/Encina.Audit.Marten/Projections/ConfigureMartenOperationAuditProjections.cs, tests/Encina.UnitTests/AuditMarten/
```

</details>

### Phase 2: Non-Throwing Field Read, `UnreadablePlaceholder` & `IsUnreadable`

> **Goal**: The projections can decide without catching their own exception. **Size**: 15 files, ~500 lines (as implemented). PR 2, `Refs #3049`. The `MetadataJsonMasked` flag is Phase 2b.

<details>
<summary><strong>Tasks</strong></summary>

1. `Events/EncryptedField.cs`: split `Decrypt` into an internal non-throwing read (proposal: `internal bool TryDecrypt(byte[] keyMaterial, out string? plaintext, out EncryptedFieldDecryptionFailureReason reason)`) over non-throwing envelope parsing and size validation; public `Decrypt` and `DecryptOrPlaceholder` keep their throwing contract and call it. Keep CRAP at 10 or less per method.
2. `MartenOperationAuditOptions.cs`: `public const string DefaultUnreadablePlaceholder = "[UNREADABLE]"` and `UnreadablePlaceholder { get; set; }` with XML docs like `ExpiredPlaceholder`.
3. `Projections/OperationAuditEntryReadModel.cs` and `ReadAuditEntryReadModel.cs`: `bool IsUnreadable`; amend the `IsShredded`/`IsExpired` doc comments (no longer mutually exclusive).
4. (Moved to Phase 2b: `MetadataJsonMasked` and the query stores; the value-based store rule of the first Phase 2 draft is removed.)
6. `PublicAPI.Unshipped.txt`, coverage manifest. Coverage rule for every code phase (AGENTS.md section 9): the PR that adds or touches a `src/` file carries the tests that reach demanding, realistic per-flag targets for that file in `.github/coverage-manifest/Encina.Audit.Marten.json`: unit and guard tests always, contract tests for new public interfaces and enums, property tests for the logic that is complex (ids, hash, outcome mapping), integration tests where the file needs real Marten. A target of 0 is allowed only where the flag cannot exercise the file (coverage methodology, "Per-file targets and justifications"); "the tests arrive in a later phase" never justifies a 0. A phase that cannot carry its tests within the size rule is split further instead.
7. Tests: update `EncryptedFieldTests.cs`, `EncryptedFieldUnreadableTests.cs` (every failure reason through the non-throwing read), `MartenOperationAuditOptionsTests.cs`; property test `AuditMartenEncryptedFieldProperties.cs` (the throwing and non-throwing reads agree).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 2 (rework per the plan's "Rework needed" table). EncryptedField currently throws EncryptedFieldDecryptionException from Decrypt; projections let it escape (part 2a). The lookup in Phase 3 is asynchronous, so the projection must decide without catching.
TASK: Add an internal non-throwing read to EncryptedField, MartenOperationAuditOptions.UnreadablePlaceholder (default "[UNREADABLE]") and IsUnreadable on both read models; keep the public throwing contract unchanged; remove the value-based placeholder rule from the two query stores (the MetadataJsonMasked flag is Phase 2b).
KEY RULES: never use exceptions for business logic (AGENTS.md section 3, ADR-006); the failure reason is the only output of the failure (never a message, envelope or key id); amend, do not duplicate, the XML docs; PublicAPI lines; per-flag manifest targets; CRAP <= 10; tests execute real code.
REFERENCE FILES: src/Encina.Audit.Marten/Events/EncryptedField.cs, src/Encina.Audit.Marten/Events/EncryptedFieldDecryptionException.cs, src/Encina.Audit.Marten/MartenOperationAuditOptions.cs, tests/Encina.UnitTests/AuditMarten/EncryptedFieldUnreadableTests.cs
```

</details>

### Phase 2b: `MetadataJsonMasked` Flag & the Query Stores

> **Goal**: The query stores know whether `MetadataJson` is masked without reading the text (D16 = A). **Size**: 12 files, ~450 lines. PR 2b, `Refs #3049`.

<details>
<summary><strong>Tasks</strong></summary>

1. `Projections/OperationAuditEntryReadModel.cs` and `ReadAuditEntryReadModel.cs`: `bool MetadataJsonMasked`.
2. `Projections/OperationAuditEntryProjection.cs` and `ReadAuditEntryProjection.cs`: set it where they already mask `MetadataJson` (shredded and expired key outcomes), and only when `MetadataJson` itself is substituted.
3. `MartenOperationAuditStore.cs` (`MapToAuditEntry`) and `MartenReadAuditStore.cs` (`MapToReadAuditEntry`): parse `MetadataJson` only when `MetadataJsonMasked` is false and never infer masking from the text; there is no value rule and no built-in-defaults rule. They are `internal static` with one parameter today; the existing tests change as needed. Rows projected before the flag exist only after a rebuild (pre-1.0, no migration; the runbook says so).
4. `PublicAPI.Unshipped.txt`, coverage manifest (targets as in Phase 2, task 6).
5. Tests: queries over rows projected after the placeholder options changed still read; the flag is set for shredded and expired and only when `MetadataJson` is a placeholder (property test: `MetadataJsonMasked` implies `MetadataJson` is a placeholder); masked metadata never reaches the JSON parser.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2b</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 2b. The query stores must know whether MetadataJson is masked without inferring it from the text; the projections already mask it for shredded and expired keys.
TASK: Add MetadataJsonMasked to both read models, set it in the projections where they substitute MetadataJson, and make MartenOperationAuditStore.MapToAuditEntry and MartenReadAuditStore.MapToReadAuditEntry parse MetadataJson only when the flag is false.
KEY RULES: no value-based placeholder rule; the flag is set only when MetadataJson itself is substituted; PublicAPI lines; per-flag manifest targets; CRAP <= 10; tests execute real code; max 15 files and 1500 lines.
REFERENCE FILES: src/Encina.Audit.Marten/Projections/OperationAuditEntryReadModel.cs, src/Encina.Audit.Marten/Projections/ReadAuditEntryReadModel.cs, src/Encina.Audit.Marten/MartenOperationAuditStore.cs, src/Encina.Audit.Marten/MartenReadAuditStore.cs
```

</details>

### Phase 3: Projection Lookup & Apply

> **Goal**: Both projections consult the event's resolutions on the failure path and apply the effective one deterministically. **Size**: 14 files, ~850 lines. PR 3, `Refs #3049`. The Phase 3 gate findings are recorded above; #3083 is merged, so it can start.

<details>
<summary><strong>Tasks</strong></summary>

1. Day-1 experiment (see the algorithm section) in `tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/AuditProjectionUnreadableEventIntegrationTests.cs`; record the outcome in the PR description.
2. `Projections/OperationAuditEntryProjection.cs`: replace the throwing `LoadTemporalKeyAsync`/`ClassifyTemporalKeyLookup` with the internal `TemporalKeyOutcome` (`Key`, `Shredded`, `Expired`, `Failure(reason)`); `ReadAuditEntryProjection.cs` reuses it. Both projections: constructors gain `unreadablePlaceholder` (3 to 4 parameters, same `ThrowIfNullOrWhiteSpace`); `Create` takes `IEvent<T>` (or the fallback `Project` method) and returns no row for an excluded event; the outcome mapping is a pure internal method (one `switch`, `// crap-exempt: single-question switch` when it qualifies); the lookup helper runs the indexed query through the `IDocumentOperations` parameter and computes the effective resolution per failing field; the projections set `MetadataJsonMasked` for resolution placeholders.
3. `Projections/ConfigureMartenOperationAuditProjections.cs`: pass `UnreadablePlaceholder` from `MartenOperationAuditOptions` to both projections.
4. `Diagnostics/MartenAuditLog.cs`: 2593 `UnreadableEventDetected` and 2594 `ResolutionApplied`; update the block table; confirm `EncinaEventIdAllocationTests` passes.
5. `PublicAPI.Unshipped.txt` (remove the old constructor lines and the old public `Create` lines, add the new constructors and the `Create` with `IEvent<T>` and its nullable return, RS0016/RS0017), coverage manifest, XML docs.
6. Tests: update `OperationAuditEntryProjectionTests.cs`, `ReadAuditEntryProjectionTests.cs`, `OperationAuditEntryProjectionGuardTests.cs`, `ConfigureMartenOperationAuditProjectionsTests.cs`; unit tests of the effective-resolution rule (latest wins per field, `All` covers every field, `Excluded` excludes and is ignored for field resolution, a later resolution brings the event back and an uncovered failing field pauses, `Withdrawn` pauses again, a key-level failure on an all-null event is resolved only by `All` or an exclusion, a hand-written `Excluded` under a non-`All` field fails closed) and a property test of the rule over random resolution sequences (the same sequence always gives the same read model; `MetadataJsonMasked` implies `MetadataJson` is a placeholder); integration: pause on a poison event, write resolution documents directly, restart (a new host over the same store) and rebuild (`RebuildProjectionAsync`), assert the read model; rebuild twice and compare field by field.
7. Replace the untyped `KeyNotFoundException` expectations at `IT:206-250` of `AuditProjectionUnreadableEventIntegrationTests` with the typed `KeyMissing` failure (#3083 already changed them; re-check against main).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 3 (#3083 is merged: KeyMissing typed failure; ClassifyTemporalKeyLookup currently throws and must become a TemporalKeyOutcome). Marten async daemon pauses the shard on a thrown projection failure; a rebuild re-projects every event.
TASK: First run the day-1 experiment (EventProjection.Create returning null; Create(IEvent<T>, IDocumentOperations, CancellationToken) accepted by Marten 9.38) and choose Create or the Project fallback. Then make both projections load the event's AuditResolution documents (one indexed query) only when the key or a field failed, apply the effective resolution (highest Sequence among the field and All; Excluded excludes the event; Withdrawn or none rethrows the typed failure), and set MetadataJsonMasked for placeholders.
KEY RULES: Marten projections take dependencies via IDocumentOperations and constructor injection, NEVER IServiceProvider; zero extra queries on the healthy path; the outcome depends only on the event, the stored kinds, references and sequences and the three placeholders (no clock, actor or random); a lookup exception propagates (never swallowed); log codes only (EventIds 2593-2594); integration tests use the shared MartenCollection fixture and prove restart and rebuild, rebuilding twice and comparing.
REFERENCE FILES: src/Encina.Audit.Marten/Projections/OperationAuditEntryProjection.cs, src/Encina.Audit.Marten/Projections/ReadAuditEntryProjection.cs, tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/AuditProjectionUnreadableEventIntegrationTests.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs
```

</details>

### Phase 4: `IAuditProjectionResolver`, Permission Gate, Batch Validation & Atomic Write

> **Goal**: The operator service, gated, audited and atomic. **Size**: three PRs. 4a: 15 files, ~1,200 lines (types, limits, `ResolutionPermission`, validation, hash serializer, effective-resolution evaluator and their tests); 4b: 11 files, ~700 lines (access gate, `Denied` entries, `GetAsync`/`ListAsync`, DI); 4c: 11 files, ~900 lines (atomic `RecordAsync`, staging method, integration tests). `Refs #3049`. Waits for nothing: all Dilemmas are answered.

<details>
<summary><strong>Tasks</strong></summary>

1. `Resolution/IAuditProjectionResolver.cs`, `AuditProjectionResolver.cs` (internal sealed, scoped), `AuditResolutionItem.cs`, `AuditResolutionBatch.cs`, `AuditResolutionApplyMode.cs`, `AuditResolutionPreview.cs`, `AuditResolutionBatchResult.cs`, `AuditResolutionLimits.cs` (`ReasonMaxLength = 500`, `MaxBatchSize`, proposal 1,000), plus the internal hash serializer and effective-resolution evaluator, `MartenOperationAuditOptions.ResolutionPermission` (default `audit:resolve`, blank rejected at startup validation, no opt-out property). Constructor dependencies (proposal): `IServiceScopeFactory` (a new scope per call, from which the store, key provider and session are resolved), `IRequestContextAccessor`, `IOptions<MartenOperationAuditOptions>`, `TimeProvider`, `ILogger<AuditProjectionResolver>`.
2. Access check first in every public method (choice 8): anonymous or no context gives `Left` `audit.marten.resolution_principal_required`; a missing permission gives `Left` `audit.marten.resolution_forbidden`; both write one `Denied` entry (choice 15) in their own commit.
3. `PreviewAsync` and the validation step of `RecordAsync` (choice 9): batch-level `Left` for blank or over-limit reason, empty or oversized batch; per-item codes; validation failures logged by code only.
4. `RecordAsync` atomic write (algorithm section, "Atomic write flow"): `MartenOperationAuditStore` gains an internal method that encrypts and appends an entry to a given session without saving; the resolver assigns `Sequence` and `Supersedes`, stores all documents, stages all entries and saves once; `ValidSubsetOnly` adds the `.Batch` entry listing applied and skipped event ids with codes. The hash serialization is fixed in choice 6.
5. `GetAsync` (effective resolution; `Withdrawn` reads as pending) and `ListAsync` (history, newest first), both gated.
6. `ServiceCollectionExtensions.cs`: `TryAddScoped<IAuditProjectionResolver, AuditProjectionResolver>()`, `TryAddScoped<MartenOperationAuditStore>()` (today only the `IOperationAuditStore` mapping is registered, so the concrete type must be registered for the resolver's scope and the `ValidateOnBuild` test; a host that replaces `IOperationAuditStore` does not change the resolver, which deliberately uses the Marten store because the atomic write needs it) and `TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>()`; `MartenAuditErrors.ResolutionWriteFailed` (`audit.marten.resolution_write_failed`).
7. `PublicAPI.Unshipped.txt`, coverage manifest.
8. Tests (each PR carries the tests of its own files at their per-file targets; Phase 4 owns the concurrency and atomicity scenarios, Phase 6 only extends the shared contract and property files `AuditProjectionResolverContractTests.cs` and `AuditMartenResolutionProperties.cs`, created here): unit (every `Left` path, denial entries with the actor and no free text, per-item codes, `ValidSubsetOnly` entry, no reason in the document, hash recomputable), guard (every public method and constructor), contract (instantiating the real resolver), property (the hash golden vector and "changing any single field changes the hash"; the effective-resolution property test lives in Phase 3 with the rule), DI test (`ValidateOnBuild` and `ValidateScopes`, store wins in any registration order); integration against real Marten: a batch writes all documents and entries in one commit; a failing audit staging (the period's key is destroyed, built with a destroyed marker) persists nothing, including after a first batch of the month that had to create the key; a 1,000-item batch completes in a reasonable time and is atomic; two items for one event in one batch get increasing sequences in batch order; an item that became invalid between preview and commit is skipped and listed in `ValidSubsetOnly` and aborts `AllOrNothing`; a batch with one invalid item writes nothing: `RecordAsync` in `AllOrNothing` mode returns `Left` `audit.marten.resolution_batch_rejected` with the index-to-code report in the details, `PreviewAsync` returns the same report as `Right`, and a `ValidSubsetOnly` run with zero valid items returns the same `Left`; `ValidSubsetOnly` writes the valid items and the batch entry; a duplicate resolution is recorded twice and the latest wins; `Withdrawn` returns the event to unresolved.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 4. Projections (Phase 3) already apply AuditResolution documents; this phase writes them. Precedent for actor handling and the audit-write timeout: src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs. Resolution documents and their audit entries must share the scope's one Marten IDocumentSession and commit in one SaveChangesAsync.
TASK: Implement IAuditProjectionResolver (PreviewAsync, RecordAsync, GetAsync, ListAsync; no removal or revoke method) and AuditProjectionResolver as in the plan's API proposal and the Phase 4 tasks, with the permission gate, the whole-batch validation, the atomic write through an internal staging method on MartenOperationAuditStore, the Denied entry for denials and anonymous calls, and DI registration in AddEncinaAuditMarten.
KEY RULES: Either<EncinaError, T> and CancellationToken everywhere; the permission audit:resolve (MartenOperationAuditOptions.ResolutionPermission) is checked first, no opt-out; actor read once from IRequestContextAccessor, never a parameter; resolutions are immutable; the batch is validated first and written all or nothing in one transaction; reason (required, non-blank, <= 500 chars after trimming) only in the encrypted entry Metadata; SHA-256 of the canonical fields in RequestPayloadHash; TimeProvider injected; errors carry codes only; DI test with ValidateOnBuild and ValidateScopes; max 15 files and 1500 lines (PRs 4a, 4b and 4c); the resolver opens its own scope per call and warms the period keys before the first Store.
REFERENCE FILES: src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs, src/Encina.Audit.Marten/MartenOperationAuditStore.cs, src/Encina.Audit.Marten/AuditEventEncryptor.cs, src/Encina.Audit.Marten/MartenAuditErrors.cs, src/Encina.Audit.Marten/ServiceCollectionExtensions.cs, src/Encina.Security.Audit/OperationAuditEntry.cs
```

</details>

### Phase 5: Cross-Cutting Integration & Observability

> **Goal**: Telemetry and logs for the resolver, tenant propagation verified. **Size**: 9 files, ~450 lines. PR 5, `Refs #3049`.

<details>
<summary><strong>Tasks</strong></summary>

1. `Diagnostics/MartenAuditMeter.cs`: `internal static readonly Counter<long> Resolutions` (`encina.audit.marten.resolutions`, tags `kind`, `trail`) and `ResolutionDenials` (`encina.audit.marten.resolution_denials`, tag `reason` = the error code).
2. `Diagnostics/MartenAuditActivitySource.cs`: `StartResolve(kind, trail)` with the tags `audit.marten.resolution_kind` and `audit.marten.resolution_trail`; never the reason, actor or ids; completion: the existing `Complete` always sets `ActivityStatusCode.Ok`, so add an error path (`Fail(activity, errorCode)`, status `Error` with the code only) used for every `Left`.
3. `Diagnostics/MartenAuditLog.cs`: 2595 `ResolutionsRecorded`, 2596 `ResolutionRejected`, 2597 `ResolutionWriteFailed`, 2598 `ResolutionDenied`; update the block table.
4. `AuditProjectionResolver.cs`: call the counters, activity and log messages; the document `TenantId` copied from the event, the entries' `TenantId` from the actor.
5. Coverage manifest. Tests: unit for the meter and activity tags (no reason or actor present), log tests for the codes-only rule, a tenant propagation test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 5. The package already has MartenAuditMeter, MartenAuditActivitySource and MartenAuditLog; EventId range AuditMarten 2550-2599 is registered in src/Encina/Diagnostics/EventIdRanges.cs; 2593-2594 are used by Phase 3.
TASK: Add the resolutions and denials counters, the AuditMarten.Resolve activity with an error path and EventIds 2595-2598, and call them from AuditProjectionResolver; verify TenantId on the document and the entries.
KEY RULES: pack EventIds sequentially, no gaps (AGENTS.md section 7), 2599 stays free; telemetry and logs carry codes, kind and trail only, never the reason, the actor or free text; exceptions through ForLogging(); run EncinaEventIdAllocationTests; zero warnings.
REFERENCE FILES: src/Encina.Audit.Marten/Diagnostics/MartenAuditMeter.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditActivitySource.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 6: Testing — Full-Flow Integration, Property, Contract & Justification Files

> **Goal**: The cross-phase acceptance tests exist and the final coverage proof is run. Each earlier code phase already carries the unit, guard, contract and property tests of its own files at their per-file targets (see the coverage rule in Phase 2); this PR adds the full-flow integration scenarios, the cross-phase property tests and the justification files. **Size**: 9 files, ~800 lines. PR 6, `Refs #3049`.

<details>
<summary><strong>Tasks</strong></summary>

Test matrix (the flags of AGENTS.md section 9):

| Flag | Required? | What it proves | Notes |
|------|-----------|----------------|-------|
| Unit | Required | Resolver, kind mapping, effective-resolution rule, projection outcome for every kind, every `Left` path, denial entries | Real package code, dependencies mocked; added in phases 1 to 5 |
| Guard | Required | `ArgumentNullException` on every new public method and constructor with parameters | `GuardClauses.xUnit`; added in phases 1 to 5 |
| Contract | Required | `IAuditProjectionResolver`, the public records and enums keep their contract; folder `tests/Encina.ContractTests/AuditMarten/` | Instantiates the real resolver; created in Phase 4 |
| Property | Required | FsCheck: random resolution sequences give the latest-wins read model in any arrival order of independent events, and projecting twice gives equal read models; hash changes with any field; validation never throws | Determinism is the feature's core promise; created in Phase 4 |
| Integration | Required | Real Marten on PostgreSQL through the shared `MartenCollection` fixture: pause on a poison event, each kind applied, restart and rebuild pick it up, rebuild twice equal field by field, a later resolution brings an excluded event back, `Withdrawn` pauses again, a batch is atomic, a denied or anonymous call writes one `Denied` entry | Extends `AuditProjectionUnreadableEventIntegrationTests`; `[Trait("Category", "Integration")]` |
| Load | Not required | Admin operation; only the failure path does a lookup | `.md` justification file in `tests/Encina.LoadTests/AuditMarten/` |
| Benchmark | Not required | Failure path only, not a hot path | `.md` justification file in `tests/Encina.BenchmarkTests/AuditMarten/` |

1. New integration test file(s) for the acceptance scenarios above; extend (do not recreate) `tests/Encina.ContractTests/AuditMarten/AuditProjectionResolverContractTests.cs` and `tests/Encina.PropertyTests/AuditMartenResolutionProperties.cs`, created in Phase 4a, with the cross-phase cases.
2. Two justification `.md` files with the headings required by AGENTS.md section 9.
3. `.github/coverage-manifest/Encina.Audit.Marten.json`: final per-file targets; measure each flag (`dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" ...`), run `coverage-report.cs` and `--check-justifications`; keep every changed method at CRAP 10 or less.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 6. The code exists (phases 1-5); this PR adds the cross-flag tests and the coverage proof. Integration tests use real Marten on PostgreSQL (Testcontainers) through the shared MartenCollection fixture.
TASK: Write the integration scenarios listed in the plan's test matrix, extend the contract and property files created in Phase 4, write the Load and Benchmark justification .md files, then measure per-flag coverage of the changed files against the manifest and fix gaps.
KEY RULES: tests MUST execute real package code (no reflection-only tests); Shouldly through Encina.Testing.Shouldly; never IClassFixture for database fixtures; ClearAllDataAsync in InitializeAsync; no Thread.Sleep; justification files never for unit, guard or contract tests; report the measured per-flag coverage in the PR.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/AuditProjectionUnreadableEventIntegrationTests.cs, tests/Encina.PropertyTests/AuditMartenEncryptedFieldProperties.cs, .github/coverage-manifest/Encina.Audit.Marten.json, docs/testing/coverage-measurement-methodology.md
```

</details>

### Phase 7: Documentation & Finalization

> **Goal**: The runbook, the ADR and the changelog. **Size**: 7 files, ~450 lines. PR 7, `Fixes #3049`.

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/features/audit-marten.md` (by `docs-writer`): replace the sentences "Operator resolution ... is not available until the later change tracked as PR 2b" and the "Limits today" bullet (lines 210 and 222 on 2026-11-04; locate them by text; also the stale query-helper note and the manual marker-editing advice); add the how-to "Resolve an unreadable audit event" (recognise, choose the kind, preview with the dry run, record, restart or rebuild, verify, with a Mermaid flow and a code sample) and a reference section (API, the `audit:resolve` permission, kinds, placeholders, flags `IsShredded`/`IsUnreadable`/`IsExpired`/`MetadataJsonMasked`, error codes); explain that resolutions and the `Denied` and resolution audit entries are appended behind the poison event, so they are not queryable until the shard resumes, and that an amendment or `Withdrawn` changes the read model only at the next rebuild while `GetAsync` shows the new state at once; explain that resolutions are append-only and the latest wins, that an exclusion is the exception, that `Withdrawn` pauses the shard again, that rows projected before `MetadataJsonMasked` need a rebuild, the `AllowSkippedProjectionErrors` caveat and the tamper limit; state that a lost key with no marker is resolved event by event with a batch; cite coverage only through `cov:` markers.
2. `docs/architecture/adr/061-audit-projection-resolution.md` (new) and its row in `docs/architecture/adr/index.md` replacing the 061 reservation row: Context, Decision (append-only documents, event id, kinds, latest wins, permission, atomic batches, determinism, tamper limit, and that `ResolvedBy` and `ErasureActor` are operator personal data kept in the plaintext document like the marker's actor, while the reason stays only in the encrypted entry), Alternatives (choices 1 to 15), Consequences; one-line pointer amendment in ADR-020.
3. Changelog fragments (`mechanical-fixer`), named by the rule `changelog.d/<issue>-<slug>.<section>.md` with issue 3049 (the issue's Documentation checklist says `2770-...` and should be edited to `3049-...`): `changelog.d/3049-audit-projection-resolver.added.md` (resolver, append-only document, `[UNREADABLE]`, `IsUnreadable`, `MetadataJsonMasked`, `ResolutionPermission`) and `changelog.d/3049-audit-unreadable-placeholder.changed.md` (projection constructors, typed missing-key failure).
4. Package README only if it describes the placeholders; `docs/INVENTORY.md` if it lists package types; `ROADMAP.md` is not affected.
5. Close-out: open or confirm the follow-up issues #3024, #3025, #3026, #3027; run `check-plan` without `-Draft`; build with zero warnings and run all test flags.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 7. Code and tests are merged (phases 1-6). Docs follow the encina-docs skill (.claude/skills/encina-docs/SKILL.md): one Diataxis quadrant per page, junior-readable English, no hand-typed coverage figures.
TASK: Update docs/features/audit-marten.md (how-to plus reference), write ADR-061 and replace its index reservation row, add the ADR-020 pointer, add the two changelog fragments.
KEY RULES: confirm ADR number 061 is neither used (git ls-tree origin/main docs/architecture/adr/ --name-only) nor reserved by another plan in the index (059 is reserved for the AES-GCM decrypt-site topic, 058 and 060 exist); every identifier named in docs exists in src/; coverage only via cov: markers; no emojis; the fragments are written by mechanical-fixer; the PR says Fixes #3049.
REFERENCE FILES: docs/features/audit-marten.md, docs/architecture/adr/020-temporal-crypto-shredding-audit-store.md, docs/architecture/adr/index.md, changelog.d/README.md, changelog.d/2770-encryptedfield-envelope.added.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Article / section | Requirement used here |
|----------|-------------------|-----------------------|
| GDPR | Art. 5(1)(c) data minimisation | The operator's reason is stored only in the encrypted, shreddable audit entry, never in a plaintext document |
| GDPR | Art. 5(1)(e) storage limitation | Retention expiry reads `[EXPIRED]` with a `RetentionReference`; operator text does not outlive retention |
| GDPR | Art. 5(2) accountability | A resolution records who, when and why; the label states the legal fact only with its reference; the history is append-only |
| GDPR | Art. 17 erasure | `[SHREDDED]` only with an `ErasureReference` (Q1) |
| ADR-020 | Temporal crypto-shredding audit store | Keys and markers live in the same PostgreSQL database; amended for #2770 |
| ADR-034 / ADR-036 | Marten encryption failure; three audit stores | Integrity over availability; operation and read audit stay separate |
| ADR-006 / ADR-001 | Pure ROP | `Either` returns, no exceptions as business logic |
| SPEC-002 DEC-006 | Compliance gates fail closed | Anonymous callers, callers without the permission and a failing audit write refuse the operation |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `TemporalKeyDestroyedMarker`, `TemporalKeyDocument` | `src/Encina.Audit.Marten/Crypto/` | Pattern for the document; same store |
| `ErasureReference`, `RetentionReference`, `MartenAuditErrors.RejectInvalidReference` | `src/Encina.Audit.Marten/Crypto/`, `MartenAuditErrors.cs` | Reference validation for `Shredded` and `Expired` |
| `OperationAuditEntryProjection`, `ReadAuditEntryProjection` | `src/Encina.Audit.Marten/Projections/` | Gain the failure-path lookup |
| `ConfigureMartenOperationAuditProjections` | `src/Encina.Audit.Marten/Projections/` | Registers the table and index, passes the placeholder |
| `MartenOperationAuditStore`, `AuditEventEncryptor` | `src/Encina.Audit.Marten/` | Entry encryption and append; gains the internal staging method |
| `IOperationAuditStore`, `OperationAuditEntry` | `src/Encina.Security.Audit/` | The audit entries (`RequestPayloadHash`, `Metadata`, `ErrorMessage` code, `AuditOutcome.Denied`) |
| `PersistentPolicyAdministrationPoint` | `src/Encina.Security.ABAC/Administration/` | Precedent: actor from `IRequestContextAccessor`, audit write timeout |
| `IRequestContextAccessor`, `RequestIdentity.Permissions`, `IRequestContextScopeFactory.RunAsServiceAsync` | `Encina` core | Ambient actor, the permission check, background and CLI hosts |
| `MartenAuditMeter`, `MartenAuditActivitySource`, `MartenAuditLog` | `src/Encina.Audit.Marten/Diagnostics/` | Counters, activity, EventIds |
| `AuditProjectionErrorPolicyStartupGuard` | `src/Encina.Audit.Marten/Projections/` | Unchanged; exclusion relies on the store-wide flags being `false` |
| `AuditProjectionUnreadableEventIntegrationTests` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/` | Template for pause, restart and rebuild proofs |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Audit.Marten` | 2550-2599 (registered, `EventIdRanges.AuditMarten`) | Used: 2550-2554, 2560-2561, 2563, 2570-2574, 2580-2586, 2590-2592. New: **2593-2598** (2593-2594 Phase 3, 2595-2598 Phase 5); 2599 stays free. No new range needed |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production (new) | 15 | 3 enums plus `AuditResolutionApplyMode`, `AuditResolution`, `AuditResolutionRecord`, `AuditResolutionIds`, `AuditResolutionItem`, `AuditResolutionBatch`, `AuditResolutionPreview`, `AuditResolutionBatchResult`, `AuditResolutionLimits`, `IAuditProjectionResolver`, `AuditProjectionResolver`, `TemporalKeyOutcome` |
| Production (modified) | 16 | `EncryptedField`, two projections, two read models, two query stores, options, `MartenAuditErrors`, `MartenOperationAuditStore`, configure, log, meter, activity source, `ServiceCollectionExtensions`, `PublicAPI.Unshipped.txt` |
| Tests | 20-26 | Unit, guard, contract, property, integration across the PRs, plus 2 justification `.md` files |
| Coverage manifest | 1 | Touched by every code PR |
| Docs and changelog | 5 | Runbook, ADR-061, ADR index, ADR-020 pointer, 2 fragments |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #3049: IAuditProjectionResolver, the audited operator path that resolves unreadable Marten audit events (part 2b of #2770).

PROJECT CONTEXT:
- Encina is pre-1.0, .NET 10 / C# 14, nullable enabled; best solution always, no compatibility layers.
- Package src/Encina.Audit.Marten only; Marten event sourcing, not the 10-database-provider matrix.
- Railway Oriented Programming: Either<EncinaError, T>; CancellationToken on every async method; TimeProvider injected.
- Part 2a (merged) made unreadable audit fields fail typed so the async daemon pauses the shard; #3083 (merged) added the typed KeyMissing failure. A key destroyed by a recorded erasure or retention never pauses; the pause and this service are only for unreadable events of unknown cause.

IMPLEMENTATION OVERVIEW:
Phase 1: AuditResolution (append-only, Guid id, Sequence, Supersedes), enums incl. Withdrawn, ids, error codes, ResolutionPermission, Schema registration with an index. Phase 2: non-throwing EncryptedField read, UnreadablePlaceholder, IsUnreadable, MetadataJsonMasked read by the query stores. Phase 3: projections load the event's resolutions on the failure path only and apply the latest (day-1 Marten 9.38 experiment first; gate: documents and audit entries share one Marten session). Phase 4: IAuditProjectionResolver with one write operation (RecordAsync batch), PreviewAsync, permission gate, Denied entries, atomic single-transaction write. Phase 5: meter, activity, EventIds 2593-2598. Phase 6: integration, contract, property tests and justification files. Phase 7: runbook, ADR-061, changelog. One PR per phase, at most 15 files and 1500 lines each.

KEY PATTERNS:
- Resolutions are immutable and append-only; no removal; Withdrawn returns an event to unresolved; the most recent resolution wins per field; eventId is Marten IEvent.Id; the field set is a closed enum plus All.
- Projections take dependencies through IDocumentOperations and the constructor, never IServiceProvider; the outcome depends only on the event, the stored resolutions and the placeholders.
- Permission audit:resolve required (no opt-out); anonymous and forbidden calls write one Denied entry (code and actor, no free text); batches are validated first and written all or nothing in one session and one SaveChangesAsync.
- Reason required, <= 500 chars, only in the encrypted audit entry Metadata; SHA-256 of canonical fields in RequestPayloadHash; logs and tags carry codes only.
- Per-flag coverage targets in .github/coverage-manifest/Encina.Audit.Marten.json, each PR with its own tests; CRAP <= 10; PublicAPI.Unshipped.txt.

REFERENCE FILES: docs/plans/audit-projection-resolver-implementation-plan-3049.md, src/Encina.Audit.Marten/Projections/OperationAuditEntryProjection.cs, src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs, tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/AuditProjectionUnreadableEventIntegrationTests.cs, docs/features/audit-marten.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Resolutions are read only on the failure path; caching would break determinism (D37). |
| 2 | OpenTelemetry | ✅ | Counters `encina.audit.marten.resolutions` (tags `kind`, `trail`) and `encina.audit.marten.resolution_denials` (tag `reason`), activity `AuditMarten.Resolve` with an error path; never reason, actor or ids (D38, Phase 5). |
| 3 | Structured Logging | ✅ | EventIds 2593-2598 in `MartenAuditLog`, codes only; validation failures and dry-run reports are logged by code (D39, Phases 3 and 5). |
| 4 | Health Checks | ⏭️ | The resolver's only dependency is the Marten store the existing check already probes; a check for a paused shard needs an in-process daemon and is tracked by #3027 (D40). |
| 5 | Validation | ✅ | The whole batch is validated first (trail and field, kind, references, event existence, reason, size) and reported per item by code; the validation step is the dry run `PreviewAsync` (D41, Phase 4). |
| 6 | Resilience | ❌ | No external system beyond the Marten store; one atomic write with the 30 s audit-write timeout copied from `PersistentPolicyAdministrationPoint` (D42). |
| 7 | Distributed Locks | ❌ | Resolutions are append-only and never edited or deleted; a tie between two sessions is broken by `Id`; no lock provider is needed (D14 = D, D43). |
| 8 | Transactions | ✅ | A batch writes its resolution documents and audit entries in one Marten session and one `SaveChangesAsync`; any failure rolls the whole batch back (D15 = E, Phase 4). |
| 9 | Idempotency | ❌ | Deliberately none: a duplicate decision is recorded twice and the latest wins; no idempotency key (D14 = D). |
| 10 | Multi-Tenancy | ✅ | The document copies the event's `TenantId`; the entries carry the actor's tenant; no isolation check because the shard is global across tenants (D34, D46). |
| 11 | Module Isolation | ❌ | `OperationAuditEntry` and the Marten events have no `ModuleId` (D35, D47). |
| 12 | Audit Trail | ✅ | This feature is the audit trail for resolutions: one entry per recorded resolution committed with its document, a `.Batch` entry for subset runs, and one `Denied` entry per denial or anonymous call (D28, D48, choices 11 and 15). |

---

## Prerequisites & Dependencies

| Item | Status | Effect |
|------|--------|--------|
| PR #3083 (`Fixes #3012`; `ClassifyTemporalKeyLookup` throws `EncryptedFieldDecryptionException(KeyMissing)` for "neither key nor marker") | Merged | Phase 3 can start; re-check the post-#3083 code names |
| Phase 3 gate: documents and audit entries share one Marten session and database | Checked in the code (findings in the "Phase 3 gate" paragraph); the implementer re-confirms in Phase 4c | If a batch write goes to another store, report it: atomicity cannot be claimed |
| PR #3169 (Phase 1) and branch `feat/3049-p2-nonthrowing-read` (Phase 2) | Implemented against the old design | Rework per the "Rework needed" table |
| Bug #3088 (the period leaks into `MartenAuditLog` and `MartenAuditErrors`) | Open | New log messages and errors of this feature carry no period |
| Issues #3024, #3025, #3026, #3027, #2910 | Open follow-ups | Out of scope; linked from the PRs |
| ADR number | 061 free and unreserved on `origin/main` (2026-11-04); 058 merged (#2980), 059 reserved for another topic, 060 exists | The index carries the 061 reservation; re-check in Phase 7 |

**Risks**

| Risk | Impact | Mitigation |
|------|--------|------------|
| Marten 9.38 `Create` may not accept a `null` return, or progress may not advance past an excluded event | Exclusion would not work | Day-1 integration experiment in Phase 3; fallback `Project` method that stores only non-excluded events |
| `Create(IEvent<T>, IDocumentOperations, CancellationToken)` not accepted by the validator | Event id unavailable to the projection | Same experiment; the class remarks name the non-generic `JasperFx.Events.IEvent` as allowed, so the generic form is proven before other Phase 3 code |
| The key provider saves the session when it creates a missing period key, or a failed save leaves state in a shared scoped session | A batch is partly persisted, or a later call commits a rolled-back batch | The resolver warms the keys before the first `Store` and uses its own scope per call (Phase 3 gate paragraph); Phase 4c adds the internal staging method and the tests |
| A batch of 1,000 items is slow in one transaction | Timeouts, long locks | A 1,000-item integration test in Phase 4c; `MaxBatchSize` is lowered if it is slow |
| Two sessions record for the same event at once | Equal `Sequence` | Total order by (`Sequence`, `Id`); Encina is not designed for several operators at once (D14 = D) |
| Rows projected before `MetadataJsonMasked` exists | Queries over old masked rows break until rebuilt | Pre-1.0 rebuild documented in the runbook; the flag defaults to false only for new rows |
| Size rule: a phase exceeds 15 files or 1500 lines | PR not reviewable | Re-measure at brief time; Phases 2 and 4 are pre-split |
| Tamper limit: a resolution document can be edited in SQL | Forged `[SHREDDED]` evidence on rebuild | D27 = (a): hash in the audit entry, documented in the runbook and ADR-061; HMAC or a package-wide integrity design only if SPEC-002 needs it |
| Store-wide skip flags (`SkipApplyErrors`, `SkipSerializationErrors`) set to `true` by the application | An unresolved failure is skipped silently | The startup guard fails the host unless `AllowSkippedProjectionErrors` is set; the runbook states the consequence (D56) |

**Assumptions**: (1) #3083 is merged and added `EncryptedFieldDecryptionFailureReason.KeyMissing`; (2) the batch size limit (proposal 1,000) and the 500-character reason limit are implementation constants; (3) the staging method on `MartenOperationAuditStore` is internal and the resolver depends on that concrete scoped store, not on `IOperationAuditStore`, because the interface saves by itself; (4) the last PR closes #3049 and also #2770 when no other child of #2770 is open (the issue text asks for `Fixes #2770` in the closing PR); (5) UUID version 7 ids come from `Guid.CreateVersion7` with the injected `TimeProvider`.

---

## Next Steps

1. Review this plan; the orchestrator links it from #3049 (`artifacts/issues/3049-plan-link.md`).
2. Rework Phase 1 (PR #3169) and Phase 2 per the "Rework needed" table; rewrite the body and Documentation checklist of #3049 (it still describes `MarkUnreadableAsync`, `RemoveAsync`, `resolution_conflict` and the open Dilemmas) so linked-issue validation judges the PRs against this plan.
3. Decompose the remaining phases into ready sub-issues (size rule), one worker brief each; Phases 2b and 4a to 4c are the pre-split PRs.
4. Phase 3 can start (#3083 is merged, the gate findings are recorded); Phase 4a to 4c follow Phase 3; Phases 5 to 7 follow in order.
5. Each PR reports the measured per-flag coverage of its files against the manifest; the last PR says `Fixes #3049`.

---

## Maintainer Decisions

1. (2026-10-25) **Q2 = (a)** on #2770 (comment 6395857025): operator resolution is a first-class audited feature; a resolution document in Marten keyed by event and field, applied before the typed failure is thrown, deterministic on rebuild. Reworked by D14 and D17 of 2026-11-04 (append-only documents).
2. (2026-10-29) **D24 = (b)**: the event is identified by Marten `IEvent.Id`, with the payload id stored as informational data. Screened as settled by rule in `artifacts/decisions/2770-2b-screen.md`; posted and not vetoed in comment 6521533698 (2026-10-30).
3. (2026-10-29) **D25 = (b)**: `Expired` with a validated `RetentionReference`, on top of Q3 = (a) (2026-10-26, comment 6413750564) and Q1 = (b) (2026-10-25, comment 6395833133); and (2026-11-04) **D17 = D** adds the `Withdrawn` kind.
4. (2026-10-30) **D23 = (d)**: resolutions stay at event and field granularity (Q2), with a bulk operation; the period-wide case (legacy or damaged marker without a reference) is covered by #2910; a period scope is reopened only if real data needs it (comments 6521533698 and 6522969450).
5. (2026-11-04) **D14 = D** on #3049: the most recent resolution wins, per field; resolutions are an append-only record that amends earlier ones and records the ids it supersedes; duplicates are recorded twice, latest wins; no idempotency key, no identical-repeat rejection, no conflict code.
6. (2026-10-29) **D27 = (a)** (settled by rule, not vetoed, comment 6521533698): tamper risk accepted and documented, with a SHA-256 of the canonical fields in the audit entry; the projection never trusts `ResolvedBy`.
7. (2026-11-04) **D15 = E** and **D17 = D** on #3049 fix the API: one write operation (`RecordAsync`, batch form), the dry run, no removal; and (2026-10-25) **Q2 = (a)** says the service is usable from code, a CLI or an admin endpoint, the CLI and endpoint being the follow-ups #3024 and #3025.
8. (2026-11-04) **D13 = C** on #3049: required permission `audit:resolve` (name configurable through `MartenOperationAuditOptions.ResolutionPermission`), checked against `RequestIdentity.Permissions`, no opt-out; anonymous callers and callers without the permission are denied. Framing: a legitimately recorded erasure or retention never pauses the projection; the pause and this service are only for unreadable events of unknown cause.
9. (2026-11-04) **D15 = E** on #3049: validate the whole batch first (the validation alone is a dry run), then write all or nothing in one transaction; applying only the valid subset needs an explicit parameter and writes its own audit entry; implementer check: documents and audit entries in the same Marten session and database, reported before Phase 3 if not.
10. (2026-10-25) **D14 = (c)** (comment 6395444785) and **D15 amended** (comment 6395499536) on #2770: integrity over availability; the shard pauses on an unreadable field and the operator resolves it; a missing or destroyed key without a recorded reference is not a lawful erasure.
11. (2026-10-25) **Q2 = (a)**: every resolution is recorded in the audit trail (who, when, reason, label) and logs carry codes only.
12. (2026-11-04) **D16 = A** on #3049: a persisted `MetadataJsonMasked` discriminator on both read models, set by the projection when it masks `MetadataJson`; the query stores read the flag and never infer masking from the text.
13. (2026-10-27) **D21 = (b)** and **D22 = (e)** (comment 6440026324): the retention policy id option, and Encina does not change Marten's store-wide error flags; the startup guard stays the fail-closed check.
14. (2026-11-04) **D17 = D** on #3049: no removal at all; resolutions are immutable like ADRs; to undo or change one the operator records a new one; `Withdrawn` returns the event to unresolved (a rebuild pauses on it again).
15. (2026-11-04) **D20 = B, adjusted** on #3049: a permission denial and an anonymous call are audited as one `Denied` entry with the error code and the actor (user id, or "anonymous") and no free text; validation failures, including dry-run reports, go to the log by error code only; the identical-repeat and missing-removal rejections no longer exist.
