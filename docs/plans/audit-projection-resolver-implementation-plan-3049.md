# Implementation Plan: `IAuditProjectionResolver` — Audited Operator Resolution of Unreadable Marten Audit Events

> **Issue**: [#3049](https://github.com/dlrivada/Encina/issues/3049) (part 2b of [#2770](https://github.com/dlrivada/Encina/issues/2770))
> **Type**: Feature
> **Complexity**: Medium-High (7 phases, 7 PRs, one package, Marten only)
> **Estimated Scope**: ~1,600-2,000 lines of production code, ~2,200-2,800 lines of tests across seven PRs, 4 docs files

---

## Summary

`Encina.Audit.Marten` projects encrypted audit events into read models. Since part 2a ([#2928](https://github.com/dlrivada/Encina/pull/2928), merged 2026-10-29) an event that cannot be decrypted fails typed (`EncryptedFieldDecryptionException`) and Marten's async daemon pauses the shard. A paused shard does not retry on its own, and the only recovery today is editing the temporal key marker by hand, which leaves no audit record. This plan adds the operator path decided in Q2 of #2770: an audited resolution that the projection consults before it throws.

**What it implements**

- `IAuditProjectionResolver`: one method per resolution kind (`MarkUnreadableAsync`, `MarkShreddedAsync` with an `ErasureReference`, `MarkExpiredAsync` with a `RetentionReference`, `ExcludeAsync`), an audited `RemoveAsync`, `GetAsync`, `ListAsync` and a bulk helper. Every method returns `ValueTask<Either<EncinaError, T>>` and takes a `CancellationToken`.
- `AuditResolution`: a mutable Marten document in the same store and schema as `TemporalKeyDocument` and `TemporalKeyDestroyedMarker`, keyed by trail, Marten `IEvent.Id` and field.
- Projection support: on the failure path only, `OperationAuditEntryProjection` and `ReadAuditEntryProjection` look up a resolution through `IDocumentOperations` and apply it deterministically (a shard rebuild yields the same read model).
- `[UNREADABLE]` placeholder (`MartenOperationAuditOptions.UnreadablePlaceholder`), an `IsUnreadable` flag on both read models, and a typed missing-key reason.
- One audit entry per resolution through `IOperationAuditStore`, written and awaited before the document; the entry carries the reason (encrypted `Metadata`) and a SHA-256 of the canonical resolution fields.

**Standards and specifications**: GDPR Art. 5(1)(c), 5(1)(e), 5(2) and 32; ADR-020 (temporal crypto-shredding), ADR-034, ADR-036, ADR-006 (pure ROP), ADR-018, ADR-019, ADR-021; SPEC-002 DEC-006 (fail closed).

**Affected packages**: `Encina.Audit.Marten` (code, `PublicAPI.Unshipped.txt`); `Encina` core only for reading `EventIdRanges.cs` (the `AuditMarten` range 2550-2599 already exists, no new range). Tests in `tests/Encina.UnitTests`, `GuardTests`, `ContractTests`, `PropertyTests`, `IntegrationTests`. Docs: `docs/features/audit-marten.md`, ADR-059, ADR-020 pointer.

**Provider category**: none of the database matrix. This is the event-sourcing category (Marten primary, AGENTS.md section 5); it touches no ADO.NET, Dapper, EF Core or MongoDB code, so the 10-provider rule does not apply. Integration tests run against real Marten on PostgreSQL.

### Goal and non-goals

**Goal.** An operator can resolve a poison audit event (one whose key or field cannot be decrypted) from code, with a full audit record, so the paused projection shard continues after a restart or rebuild. The result is deterministic: the applied outcome is a pure function of the event and the stored resolution. Resolutions are fail-closed (anonymous callers and a failing audit write refuse the resolution).

**Non-goals**

- CLI command: [#3024](https://github.com/dlrivada/Encina/issues/3024). Admin endpoint: [#3025](https://github.com/dlrivada/Encina/issues/3025). `Encina.Audit.Marten` gets no ASP.NET Core or CLI reference.
- Health check for a paused shard: [#3027](https://github.com/dlrivada/Encina/issues/3027). Operator restart and rebuild daemon semantics, and any in-process `IProjectionDaemon` seam: spike [#3026](https://github.com/dlrivada/Encina/issues/3026). The resolver only writes data; the operator restarts the host or runs `RebuildProjectionAsync` (documented, proven by an integration test).
- Period scope: decided out by D23 = (d); the period-wide legacy or damaged marker case is [#2910](https://github.com/dlrivada/Encina/issues/2910) (audited attach of a reference to the existing marker).
- HMAC signing or an append-only stream for resolution documents (D27 = (a): accepted and documented).
- Changing Marten's store-wide error flags (D22 = (e) of #2770; `AuditProjectionErrorPolicyStartupGuard` is untouched).

### Prerequisite and delivery split (size rule)

[#3012](https://github.com/dlrivada/Encina/issues/3012) is the bug issue for the "no key and no marker" case; its fix (branch `fix/3012-audit-key-missing-typed-failure`, PR `Fixes #3012` being opened) makes `OperationAuditEntryProjection.ClassifyTemporalKeyLookup` throw `EncryptedFieldDecryptionException(EncryptedFieldDecryptionFailureReason.KeyMissing)` instead of `KeyNotFoundException` (D14). Phases 3 to 7 depend on that PR being merged; this plan is written against the post-#3012 code, and the worktree re-merges `origin/main` when it lands. Phases 1 and 2 do not depend on it.

The size rule of the EPIC #2845 working rule (#2900, `.claude/skills/worker-brief/SKILL.md`) allows one worker PR, one package, at most 15 files and 1500 changed lines. The feature (about 14 production files, 16 or more test files, manifest, PublicAPI, docs) exceeds it, so it ships as seven PRs. Each phase below is one PR and carries its own `Size:` estimate; re-measure at brief time and merge two phases only if both fit.

| PR | Phase | Closes | Size (estimate) |
|----|-------|--------|-----------------|
| 1 | Resolution model, document registration, error codes | `Refs #3049` | 13 files, ~450 lines |
| 2 | Non-throwing field read, `UnreadablePlaceholder`, `IsUnreadable`, query-store mapping | `Refs #3049` | 14 files, ~650 lines |
| 3 | Projection lookup and apply, non-throwing key classification (after the #3012 PR merges) | `Refs #3049` | 14 files, ~850 lines |
| 4 | `IAuditProjectionResolver`, audit-first write, DI (pre-split into 4a interface/model/validation and 4b write flow/DI if the brief-time count passes 15 files or 1500 lines) | `Refs #3049` | 13 files, ~1,300 lines |
| 5 | Observability and cross-cutting integration | `Refs #3049` | 9 files, ~450 lines |
| 6 | Full-flow tests (integration, property, contract) and justification files | `Refs #3049` | 9 files, ~800 lines |
| 7 | Docs, ADR-059, changelog | `Fixes #3049` (and `Fixes #2770` when #3049 is its last open child) | 7 files, ~450 lines |

---

## Design Choices

Maintainer decisions are quoted from the comments of #2770 and recorded dated in the last section. The decision register with every option is `artifacts/decisions/2770-2b.md` (ids D1-D56, DL1-DL9); the orchestrator screening is `artifacts/decisions/2770-2b-screen.md`. Both are working files; each block below carries the citation that makes it checkable. Choices 1 to 3, 5 to 10 and 12 are Settled, choice 11 is Trivial; choice 4 was a Dilemma and the maintainer answered it on 2026-10-30.

<details>
<summary><strong>1. Storage model — a mutable `AuditResolution` document, deterministic string id, same Marten store and schema</strong></summary>

Register: D1, D2, D3, D4, D6.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Mutable Marten document `AuditResolution` in the same store and schema, id `audit-resolution:{trail}:{eventId}:{field}`, registered in `ConfigureMartenOperationAuditProjections`** | Matches Q2 ("a resolution document in Marten"); same database as keys and markers (ADR-020 section 6); the table exists before the daemon reads it; deterministic id gives duplicate detection | A document can be edited in SQL (see choice 6) |
| B) Append-only `AuditResolutionRecorded` event projected into a document | Event-store immutability | Departs from Q2's wording; the daemon would project a document it must then read inside another projection |
| C) Separate schema or database | Isolation | A second store to operate, migrate and back up; no gain over A |

### Chosen Option: **A — mutable document in the same store, deterministic string id**

### Rationale

- The trail (`Operation` or `Read`) is part of the id because there are two event types, two projections and different field sets; without it an id and field pair is ambiguous across the shards.
- `AuditResolutionField` is a closed enum named after the read-model properties (Operation: `UserId`, `IpAddress`, `UserAgent`, `RequestPayload`, `ResponsePayload`, `MetadataJson`; Read: `UserId`, `Purpose`, `MetadataJson`) plus `All`; a field that does not belong to the trail returns `Left`.
- The authoritative history is the audit entry (choice 9), not the document.

**Class:** Settled
**Settled by:** Q2 comment 6395857025 on #2770; ADR-020 section 6 (`docs/architecture/adr/020-temporal-crypto-shredding-audit-store.md`); package convention of string ids from the natural key (`TemporalKeyDestroyedMarker` id `temporal-destroyed:{period}`)
**Verified:** adversarial-reviewer 2026-10-30

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

- Only A is operable for a poison event. The payload `Id` is stored in the document as informational data once known (`AuditEntryId`), and log message 2593 prints the event id, the sequence number and the payload id.
- Condition proven in Phase 3: `Create(IEvent<T>, IDocumentOperations, CancellationToken)` works with Marten 9.38 (see the day-1 experiment).

**Class:** Settled
**Settled by:** Q2 comment 6395857025 (keyed by "event"); maintainer decision D24 = (b) on #2770 (comment 6521533698, not vetoed); runbook `docs/features/audit-marten.md` (the daemon prints the event guid)
**Verified:** adversarial-reviewer 2026-10-30

</details>

<details>
<summary><strong>3. Resolution kinds — `Unreadable`, `Shredded`, `Expired`, `Excluded`, plus audited removal; exclusion means no row</strong></summary>

Register: D24 (screen D25), D26, D19, D56.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Four kinds; `Shredded` needs a valid `ErasureReference`, `Expired` a valid `RetentionReference`; `Excluded` makes the projection yield no row; removal deletes the document (audited)** | Accurate legal labels (Art. 5(2)); a legacy marker without reference is no longer forced into a false `[SHREDDED]`; exclusion works with the store-wide skip flags `false` and is identical on every rebuild | One more public method than Q2 listed |
| B) Q2's four only (no `Expired`) | Strict wording | The operator can only pick `[SHREDDED]` (forbidden without reference by Q1) or `[UNREADABLE]` (inexact) |
| C) Use Marten's own skip or dead-letter for exclusion | Native | Needs the skip flags `true`, which D22 = (e) forbids; not rebuild-stable |

### Chosen Option: **A — four kinds plus audited removal**

### Rationale

- A resolution applies only where a failure would occur; if the cause was repaired the event reads fine and the stale document is harmless (D19), so a planted `[SHREDDED]` cannot hide readable data.
- With `AllowSkippedProjectionErrors = true` the lookup still runs before the throw; an unresolved failure is then skipped silently, which the runbook states (D56).
- Exclusion is the exception: ADR-020 prefers rows; the runbook says so.
- **Unverified Marten 9.38 behaviour, proven by an integration experiment in Phase 3**: `EventProjection.Create` may return `null` without error and the shard's progress advances by event sequence. Fallback if not: a `Project(IEvent<T>, IDocumentOperations, CancellationToken)` method that calls `Store` only when the event is not excluded.

**Class:** Settled
**Settled by:** Q3 comment 6413750564 and Q1 comment 6395833133 on #2770; maintainer decision D25 = (b) (comment 6521533698); Q2 comment 6395857025 (exclude the event, deterministic on rebuild) and D22 comment 6440026324 (the store-wide skip flags stay `false`, so Marten's own skip is unavailable); AGENTS.md section 3 (fail closed; errors never swallowed). That a `null` return from `Create` excludes cleanly is the day-1 experiment's responsibility
**Verified:** adversarial-reviewer 2026-10-30

</details>

<details>
<summary><strong>4. Granularity — event + field only, with a bulk helper</strong></summary>

Register: D7 (screen D23, DL1).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A) Event + field plus an enumerating `ResolvePeriodAsync` helper | Literal Q2 key | One document and one audit entry per event of a period: volume and noise |
| B) Add a period scope to the same document type | One step for a lost key of a month | Wider blast radius, extends Q2's key |
| C) Event + field only; fix period-wide causes on the marker (audited attach, never overwrite) | Marker stays the single source of why a key is gone | Edits the marker, which D15 wanted untouched |
| **D) Event + field only, with a bulk helper over many events; the period-wide case is #2910** | Q2 key kept; since Q3 new retention purges write a reference, so the remaining period-wide causes are a lost key or a damaged marker | #2910 covers only attaching a reference to an existing marker; a lost key with no marker is resolved event by event with the bulk helper (the runbook and ADR-059 say so) |

### Chosen Option: **D — event + field only, with a bulk helper; period case is #2910 (maintainer decision 2026-10-30)**

### Rationale

- The maintainer chose (d) on 2026-10-30. A period scope is reopened only if real data needs it.
- The bulk helper applies one decision to many event ids of one trail, one audit-first write per event, stopping at the first failure (shape: Phase 4).

**Class:** Dilemma
**Settled by:** Maintainer decision D23 = (d) on #2770, comments 6521533698 and 6522969450, 2026-10-30
**Verified:** n/a (answered by the maintainer)

</details>

<details>
<summary><strong>5. Repeated or conflicting resolutions — reject a different kind, idempotent identical, change through audited removal</strong></summary>

Register: D27 (screen D26, DL5), D43, D45.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A different kind returns `Left` `audit.marten.resolution_conflict`; an identical repeat returns `Right` and writes no second audit entry; change only after an audited `RemoveAsync`** | Write-once principle; clean history; no lock (the deterministic id and the store's own uniqueness serialise concurrent writers) | One extra step to change |
| B) Last write wins | Operator speed | Silently replaces `[SHREDDED]` evidence |
| C) Versioned documents | In-store history | Latest-version query on the failure path for little gain over the audit trail |

### Chosen Option: **A — reject different, idempotent identical, audited removal**

### Rationale

- Implemented with an insert on the deterministic id: a duplicate raises the store's own error, mapped to `Left`; the identical case is detected by comparing the stored document with the requested one.
- Identity of "identical": same kind and same reference fields. The reason text and actor do not take part.

**Class:** Settled
**Settled by:** #2910 Expected Behavior (never overwrite an existing reference); maintainer decision D26 = (a) on #2770 (comment 6521533698); AGENTS.md section 3 (fail closed) and section 6 function 9 (idempotency)
**Verified:** adversarial-reviewer 2026-10-30

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

- Canonical fields: trail, event id, field, kind, erasure request id or retention policy id. An auditor recomputes and compares.
- A package-wide integrity design (keys, markers, resolutions together) is a separate spike, opened only if SPEC-002 needs it.

**Class:** Settled
**Settled by:** Maintainer decision D27 = (a) on #2770 (comment 6521533698); principle C1b of #2770 (Encina limits itself to Encina); `OperationAuditEntry.RequestPayloadHash` (tamper detection); ADR-020 amendments of 2026-10-26
**Verified:** adversarial-reviewer 2026-10-30

</details>

<details>
<summary><strong>7. Public API shape, actor, authorisation and registration</strong></summary>

Register: D8, D9, D10, D11, D12, D13 (screen S1, S2, S3, S4, DL7, DL8, DL9).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One method per kind plus `GetAsync`, `ListAsync`, a bulk helper; `Either` returns with a trailing `CancellationToken`; actor from `IRequestContextAccessor`, anonymous refused; authorisation left to the application; registered by `AddEncinaAuditMarten` (scoped, `TryAdd`) with the accessor; no CLI or endpoint** | Compile-time safety (a `Shredded` call cannot omit its reference); actor not forgeable by a parameter; consistent with `PersistentPolicyAdministrationPoint` and `DestroyKeysBeforeAsync` | The host must gate who may call the service |
| B) One `ResolveAsync(request, ct)` with runtime validation | Small interface | Illegal combinations caught at runtime only |
| C) Required permission option (`audit:resolve`) | Secure by default | New policy inconsistent with the sibling APIs; mixes authorisation into a storage package |

### Chosen Option: **A — per-kind methods, ambient actor, application-side authorisation**

### Rationale

- A missing or anonymous principal returns `Left` `audit.marten.resolution_principal_required` (fail closed); background or CLI hosts open `IRequestContextScopeFactory.RunAsServiceAsync`.
- A shared internal method takes one `AuditResolutionDecision` record; the public per-kind methods build it.
- `AddEncinaAuditMarten` already registers `ITemporalKeyProvider` unconditionally; a scoped service costs nothing until used.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming; compliance gates fail closed; registration completeness with a `ValidateOnBuild` test); `src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs` (`TryResolveActor`, records the actor but does not authorise it); Q2 comment 6395857025
**Verified:** adversarial-reviewer 2026-10-30

</details>

<details>
<summary><strong>8. Projection control flow — failure-path lookup through `IDocumentOperations`, a non-throwing internal read</strong></summary>

Register: D14, D15, D16, D17, D18, D20.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The key lookup returns a `TemporalKeyOutcome` and an internal non-throwing read returns an outcome per field; the projection decides, loads resolutions only when a field failed (at most one primary-key load per candidate id: `All` and the failing field), applies them, and throws the original typed failure when none exists; public `Decrypt` keeps throwing** | No exceptions as business logic; zero extra queries on the healthy path; deterministic | `EncryptedField` parsing is refactored |
| B) Catch `EncryptedFieldDecryptionException`, then look up and re-map | Small diff | Exceptions as control flow; `MapToReadModel` is synchronous while the lookup is asynchronous |
| C) Pre-load all resolutions of the event on every event | Simple | One more query on the healthy path |

### Chosen Option: **A — non-throwing read plus failure-path lookup**

### Rationale

- Constructor injection only; no `IServiceProvider` (Marten projection rule).
- If the lookup itself fails (for example a PostgreSQL error) the exception propagates and the shard pauses; it is never read as "no resolution" (D20).
- The applied outcome is a pure function of the event and the stored resolution (kind, reference, placeholders from options): no clock, no actor and no random value reach the read model.

**Class:** Settled
**Settled by:** AGENTS.md section 3 ("Marten projections take dependencies through `IDocumentOperations` and constructor injection, NEVER `IServiceProvider`"; "NEVER use exceptions for business logic"; errors never swallowed); ADR-006; D14 and D15 comments on #2770 (6395444785, 6395499536)
**Verified:** adversarial-reviewer 2026-10-30

</details>

<details>
<summary><strong>9. Audit-first recording, reason handling, tenant and time</strong></summary>

Register: D28, D29, D30, D32, D33, D34.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `OperationAuditEntry` through `IOperationAuditStore.RecordAsync`, awaited first (own scope, as `PersistentPolicyAdministrationPoint`); the document second; a failed document write adds a second entry with outcome `Error`; reason required, non-blank, at most 500 characters, only in the encrypted entry `Metadata`; `TimeProvider` injected; the document copies the event's `TenantId`** | Fail closed; the reason is shreddable with its period (GDPR Art. 5(1)(c), 5(1)(e)); testable time | The entry is encrypted with the current period key, so a missing current key also refuses the resolution |
| B) Document first, audit second | Simpler failure handling | A crash leaves an unaudited resolution |
| C) Reason also stored in the document | Easier to list | Plaintext operator text kept beyond retention |

### Chosen Option: **A — audit-first, reason only in the encrypted entry**

### Rationale

- The entry: `Action` `ResolveUnreadableAuditEvent` (removal: `RemoveAuditResolution`), `EntityType` `AuditEvent`, `EntityId` the event id, `UserId`, `TenantId` and `CorrelationId` from the ambient context, `ErrorMessage` an error code only, `Metadata` with trail, field, kind, reference identifiers and the reason.
- There is no tenant-isolation check: the shard is global across tenants and the operator is a platform role.
- The document stores `ResolvedBy` and `ResolvedAtUtc` (informational, never trusted by the projection) and never the reason.

**Class:** Settled
**Settled by:** AGENTS.md section 6 function 12 and section 3 (fail closed; `TimeProvider`); Q2 comment 6395857025 ("recorded in the audit trail"); GDPR Art. 5(1)(c) and 5(1)(e); `PersistentPolicyAdministrationPoint` (audit awaited before the change); ADR-020 section 2
**Verified:** adversarial-reviewer 2026-10-30

</details>

<details>
<summary><strong>10. `[UNREADABLE]` placeholder, `IsUnreadable` flag and typed missing-key reason</strong></summary>

Register: D14 (missing key), D21, D22.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MartenOperationAuditOptions.UnreadablePlaceholder` (const `DefaultUnreadablePlaceholder = "[UNREADABLE]"`) passed to both projection constructors with the same `ThrowIfNullOrWhiteSpace` validation; event-level `bool IsUnreadable` on both read models; the missing key reads as `EncryptedFieldDecryptionFailureReason.KeyMissing` (from #3012)** | Mirrors `ShreddedPlaceholder` and `ExpiredPlaceholder`; a blank placeholder cannot project blank text | Constructors change (RS0017 lines replaced) |
| B) Hard-coded literal | Fewer lines | Not configurable like the other two |
| C) Per-field flags | Finer queries | A list of field names for no current use |

### Chosen Option: **A — option, event-level flag, typed reason**

### Rationale

- A resolution targets one field, so `IsShredded`, `IsUnreadable` and `IsExpired` can now be true together; the doc comment "Mutually exclusive with `IsShredded`" on `IsExpired` is amended, and the query-helper treatment of placeholders in the runbook is updated.

**Class:** Settled
**Settled by:** D15 comment 6395499536 on #2770 (`[UNREADABLE]` = operator-acknowledged unreadable data); precedent `MartenOperationAuditOptions.ShreddedPlaceholder` and `ExpiredPlaceholder`; AGENTS.md section 3 ("Every line serves a current purpose")
**Verified:** adversarial-reviewer 2026-10-30

</details>

<details>
<summary><strong>11. Observability and logging — one counter, one activity, EventIds 2593-2598 in the existing `AuditMarten` range</strong></summary>

Register: D36, D38, D39.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Counter `encina.audit.marten.resolutions` (tags `kind`, `trail`) in `MartenAuditMeter`, activity `AuditMarten.Resolve` in `MartenAuditActivitySource`, `[LoggerMessage]` 2593-2598 in `MartenAuditLog`, codes only** | The package already has the meter, the activity source and the range; nothing to register | Block 2590-2599 is labelled "Startup validation" in the log block table (relabel it); 2599 is kept free |
| B) A new EventId range | Room to grow | Not needed for six messages; the range cannot grow past 2599 |

### Chosen Option: **A — extend the existing meter, activity source and log block**

### Rationale

- Messages: 2593 `UnreadableEventDetected` (Error, logged by the projection before it rethrows), 2594 `ResolutionApplied` (Debug), 2595 `ResolutionRecorded` (Information), 2596 `ResolutionRejected` (Warning, code), 2597 `ResolutionAuditFailed` (Error, code), 2598 `ResolutionRemoved` (Information); 2599 stays free.
- Never the reason, the actor or free text; exceptions go through `ForLogging()`.

**Class:** Trivial
**Settled by:** AGENTS.md section 7 (`EventIdRanges.AuditMarten = (2550, 2599)`, pack sequentially) and section 6 functions 2 and 3; `src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs` block table
**Verified:** adversarial-reviewer 2026-10-30

</details>

<details>
<summary><strong>12. Delivery — seven PRs, tests per flag, ADR-059, runbook</strong></summary>

Register: D49-D55.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Seven PRs of at most 15 files and 1500 lines; unit, guard, contract, property and integration tests; load and benchmark justified in `.md` files; ADR-059 plus a pointer in ADR-020; runbook updated by `docs-writer`; the last PR closes the issue** | Follows the size rule; each PR reviewable; coverage obligations measured per PR | More review cycles |
| B) One PR | One review | Breaks the size rule (about 40 files) |

### Chosen Option: **A — seven PRs**

### Rationale

- ADR-059 is the next free number: `origin/main` ends at 057 and 058 is taken by open PR #2980 (checked 2026-10-30); confirm again when Phase 7 starts.
- Every new or touched `src/` file gets per-flag targets and one-sentence justifications in `.github/coverage-manifest/Encina.Audit.Marten.json`; CRAP stays at 10 or less on changed methods (the pure outcome mapping is one `switch` that may carry `// crap-exempt: single-question switch`).

**Class:** Settled
**Settled by:** AGENTS.md section 8 (ADR for architectural decisions; PublicAPI), section 9 (per-flag obligations, CRAP, integration required for database features) and section 11 (changelog fragments); EPIC #2845 size rule (#2900)
**Verified:** adversarial-reviewer 2026-10-30

</details>

---

## Implementation Phases

Data model and algorithm used by the phases. **API proposal** (names are proposals derived from the register, namespace `Encina.Audit.Marten.Resolution`; the exact signatures are fixed in each phase's brief):

```csharp
public enum AuditResolutionTrail { Operation = 0, Read = 1 }
public enum AuditResolutionKind { Unreadable = 0, Shredded = 1, Expired = 2, Excluded = 3 }
public enum AuditResolutionField { All = 0, UserId, IpAddress, UserAgent, RequestPayload, ResponsePayload, MetadataJson, Purpose }

public interface IAuditProjectionResolver
{
    ValueTask<Either<EncinaError, AuditResolutionRecord>> MarkUnreadableAsync(AuditResolutionTrail trail, Guid eventId, AuditResolutionField field, string reason, CancellationToken cancellationToken = default);
    ValueTask<Either<EncinaError, AuditResolutionRecord>> MarkShreddedAsync(AuditResolutionTrail trail, Guid eventId, AuditResolutionField field, ErasureReference erasure, string reason, CancellationToken cancellationToken = default);
    ValueTask<Either<EncinaError, AuditResolutionRecord>> MarkExpiredAsync(AuditResolutionTrail trail, Guid eventId, AuditResolutionField field, RetentionReference retention, string reason, CancellationToken cancellationToken = default);
    ValueTask<Either<EncinaError, AuditResolutionRecord>> ExcludeAsync(AuditResolutionTrail trail, Guid eventId, string reason, CancellationToken cancellationToken = default);
    ValueTask<Either<EncinaError, Unit>> RemoveAsync(AuditResolutionTrail trail, Guid eventId, AuditResolutionField field, string reason, CancellationToken cancellationToken = default);
    ValueTask<Either<EncinaError, Option<AuditResolutionRecord>>> GetAsync(AuditResolutionTrail trail, Guid eventId, AuditResolutionField field, CancellationToken cancellationToken = default);
    ValueTask<Either<EncinaError, IReadOnlyList<AuditResolutionRecord>>> ListAsync(AuditResolutionTrail trail, Guid eventId, CancellationToken cancellationToken = default);
    ValueTask<Either<EncinaError, AuditResolutionBatchResult>> ResolveManyAsync(AuditResolutionTrail trail, IReadOnlyCollection<Guid> eventIds, AuditResolutionField field, AuditResolutionDecision decision, string reason, CancellationToken cancellationToken = default);
}
```

- `AuditResolution` (Marten document, public settable properties like `TemporalKeyDestroyedMarker`): `Id` (`audit-resolution:{trail}:{eventId}:{field}`), `Trail`, `EventId`, `Field`, `Kind`, `AuditEntryId` (the payload id, informational), `TenantId` (copied from the event), flat erasure fields (`ErasureRequestId`, `ErasureRequestedAtUtc`, `ErasureActor`) and retention fields (`RetentionPolicyId`, `RetentionCutoffUtc`, `RetentionExecutedAtUtc`, `RetentionActor`) as in the marker, `ResolvedBy`, `ResolvedAtUtc`. No reason text.
- `AuditResolutionRecord`: immutable snapshot returned to callers (the document without internals). `AuditResolutionDecision`: the shared request record (kind plus optional reference, factories per kind). `AuditResolutionBatchResult`: applied and unchanged counts of a fully successful batch; the batch stops at the first `Left`, which is returned (earlier events stay resolved, each independently audited).
- Validation returns `Left`: invalid trail and field combination, `Excluded` with a field other than `All`, blank or over-500-character reason, invalid reference (`MartenAuditErrors.RejectInvalidReference`, codes `audit.marten.invalid_erasure_reference` and `audit.marten.invalid_retention_reference`), unknown event (existence check by `IEvent.Id` against `mt_events` and the trail's event type), conflict (`audit.marten.resolution_conflict`), no principal (`audit.marten.resolution_principal_required`).
- **Key-level outcome (M2, chosen design)**: today `LoadTemporalKeyAsync` and `ClassifyTemporalKeyLookup` throw on a key-level failure (no reference on the marker, both references, and after #3012 `KeyMissing`) before any resolution can be consulted. Phase 3 refactors them to return an outcome type, `TemporalKeyOutcome` (internal): `Key(material)`, `Shredded`, `Expired` or `Failure(reason)`. No exception is used for control flow (AGENTS.md section 3, ADR-006), and the alternative (one `catch (EncryptedFieldDecryptionException)` in the projection that looks up and rethrows) is rejected because it makes the projection catch its own exception and keeps a try/catch around an asynchronous lookup. The throwing contract is kept only where the projection has no resolution: it throws `new EncryptedFieldDecryptionException(reason)` itself. The existing `ClassifyTemporalKeyLookup` unit tests change to assert outcomes.
- **Projection lookup algorithm** (failure path only): the projection computes the key outcome, then for each non-null encrypted field the field outcome (`Value(text)` or `Failure(reason)`, from the Phase 2 non-throwing read; a `Failure` key outcome makes every non-null field fail with that reason; an event whose encrypted fields are all null therefore projects even with a failing key, because nothing in it is unreadable: a deliberate relaxation of today's key-first throw, covered by a Phase 3 test). If no field failed, build the read model with no extra query. Otherwise log 2593, load the `All` resolution (one primary-key load) and the resolution of each failing field (one load per field). Precedence: an `Excluded` resolution (only valid under `All`) wins and yields no row; otherwise an `All` resolution of another kind applies to every failing field and a field-specific resolution applies to its own field (the field-specific one wins over `All` for that field). `Unreadable`, `Shredded` or `Expired` substitutes the matching placeholder and sets the matching flag; a failing field with no resolution makes the projection throw the typed failure (the first failing field's reason). Different ids never conflict under the choice 5 rule, so precedence is defined here and in the Phase 3 acceptance tests. Shard rebuild determinism: the output depends only on the event, the stored `Kind` and references, and the three placeholder strings from the options.
- **Query stores (M1)**: `MartenOperationAuditStore.MapToAuditEntry` and `MartenReadAuditStore.MapToReadAuditEntry` deserialize `MetadataJson` as JSON unless the model is shredded, expired or the value equals the shredded placeholder; `[UNREADABLE]` is not JSON and a per-field `Shredded`/`Expired` resolution can leave a readable `MetadataJson` beside a set flag. Phase 2 changes the decision to the value: `MetadataJson` is parsed only when it is not one of the three configured placeholders, so a flag no longer drops readable metadata and a placeholder never reaches the JSON parser.
- **Null guards**: `MartenAuditErrors.RejectInvalidReference(null)` returns `null` (valid), so `MarkShreddedAsync` and `MarkExpiredAsync` throw `ArgumentNullException` for a null reference (guard test) and call `RejectInvalidReference` only for a non-null one.
- **Concurrent writers**: the flow audits first, then inserts. On a duplicate-key error from the insert the loser reloads the stored document and compares: identical returns `Right`, different returns `Left` conflict. A race can therefore leave one extra audit entry for the loser, even when its resolution is identical: this is the one accepted exception to "no second audit entry" (the entry exists before the insert and cannot be withdrawn). The Phase 4 flow has the reload step and an integration test with two concurrent identical resolutions.
- **Day-1 integration experiment (Phase 3)**: before any other Phase 3 code, extend `AuditProjectionUnreadableEventIntegrationTests` with a throwaway projection that returns `null` from `Create` for a marked event and assert the daemon does not fail and progress passes that event; and that `Create(IEvent<T>, IDocumentOperations, CancellationToken)` is accepted by the Marten 9.38 validator. If `null` is not accepted, use the `Project`-method fallback of choice 3 (the resolution then calls `operations.Store` only for non-excluded events). The result decides the code shape; record it in the PR.

### Phase 1: Resolution Model, Document Registration & Error Codes

> **Goal**: The types every later phase uses, and the table registered before any daemon reads it. **Size**: 13 files, ~450 lines. PR 1, `Refs #3049`.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Audit.Marten/Resolution/AuditResolutionTrail.cs`, `AuditResolutionKind.cs`, `AuditResolutionField.cs`: the three enums above, XML docs, explicit values.
2. `Resolution/AuditResolution.cs`: the document; `Resolution/AuditResolutionRecord.cs`: the snapshot record; `Resolution/AuditResolutionIds.cs`: `internal static string Format(AuditResolutionTrail, Guid, AuditResolutionField)` (the id) and the valid field set per trail (`IsValid(trail, field)`).
3. `MartenAuditErrors.cs`: codes and factories `ResolutionConflict`, `ResolutionPrincipalRequired`, `InvalidResolutionReason`, `ResolutionEventNotFound`, `InvalidResolutionField`; messages carry no reason or identity.
4. `Projections/ConfigureMartenOperationAuditProjections.cs`: `options.Schema.For<AuditResolution>().Identity(x => x.Id)` in the same method. The existing key and marker documents have no `Schema.For` today (Marten creates them on first use), so this registration is new behaviour: the table must exist before the daemon reads it on the failure path, and the task adds a test that the table exists on a fresh store. Registering the two older documents too is not part of this plan.
5. `PublicAPI.Unshipped.txt` (new public symbols), `.github/coverage-manifest/Encina.Audit.Marten.json` (per-file targets and justifications; `mechanical-fixer`).
6. Tests: `tests/Encina.UnitTests/AuditMarten/AuditResolutionIdsTests.cs` (id format, valid field sets), `AuditResolutionErrorsTests.cs`; `tests/Encina.GuardTests/AuditMarten/AuditResolutionGuardTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Issue #3049, plan docs/plans/audit-projection-resolver-implementation-plan-3049.md, Phase 1. Package src/Encina.Audit.Marten (.NET 10, C# 14, Marten, LanguageExt Either). Marten only; no database provider matrix.
TASK: Add the Resolution/ model types (three enums, AuditResolution document, AuditResolutionRecord, AuditResolutionIds), the five MartenAuditErrors codes/factories, the Schema.For<AuditResolution>() registration in ConfigureMartenOperationAuditProjections, PublicAPI.Unshipped lines, coverage-manifest entries and tests (unit, guard).
KEY RULES: Id format audit-resolution:{trail}:{eventId}:{field}; errors carry codes only (AGENTS.md section 3); no reason text in the document; XML docs on every public member; zero warnings; every new file has per-flag targets with justifications; CRAP <= 10; max 15 files and 1500 lines.
REFERENCE FILES: src/Encina.Audit.Marten/Crypto/TemporalKeyDestroyedMarker.cs, src/Encina.Audit.Marten/Crypto/TemporalKeyDocument.cs, src/Encina.Audit.Marten/MartenAuditErrors.cs, src/Encina.Audit.Marten/Projections/ConfigureMartenOperationAuditProjections.cs, tests/Encina.UnitTests/AuditMarten/
```

</details>

### Phase 2: Non-Throwing Field Read, `UnreadablePlaceholder` & `IsUnreadable`

> **Goal**: The projections can decide without catching their own exception. **Size**: 14 files, ~650 lines. PR 2, `Refs #3049`.

<details>
<summary><strong>Tasks</strong></summary>

1. `Events/EncryptedField.cs`: split `Decrypt` into an internal non-throwing read (proposal: `internal bool TryDecrypt(byte[] keyMaterial, out string? plaintext, out EncryptedFieldDecryptionFailureReason reason)`) over non-throwing envelope parsing and size validation; public `Decrypt` and `DecryptOrPlaceholder` keep their throwing contract and call it. Keep CRAP at 10 or less per method.
2. `MartenOperationAuditOptions.cs`: `public const string DefaultUnreadablePlaceholder = "[UNREADABLE]"` and `UnreadablePlaceholder { get; set; }` with XML docs like `ExpiredPlaceholder`.
3. `Projections/OperationAuditEntryReadModel.cs` and `ReadAuditEntryReadModel.cs`: `bool IsUnreadable`; amend the `IsShredded`/`IsExpired` doc comments (no longer mutually exclusive).
4. `MartenOperationAuditStore.cs` (`MapToAuditEntry`) and `MartenReadAuditStore.cs` (`MapToReadAuditEntry`): parse `MetadataJson` only when it is not one of the three configured placeholders (the existing `[SHREDDED]`/`[EXPIRED]` skip, extended to `[UNREADABLE]` and made value-based, M1); the stores read the placeholders from their injected `MartenOperationAuditOptions`. `MapToAuditEntry` and `MapToReadAuditEntry` are `internal static` with one parameter today, so they become instance methods (or take the options); the existing tests that call them statically change.
5. `PublicAPI.Unshipped.txt`, coverage manifest. Per-file targets for later phases' files are staged: each phase adds the targets of the files it touches, a temporary 0 only with a one-sentence reason naming the later phase.
6. Tests: query tests that a resolved event (`[UNREADABLE]` metadata, per-field `Shredded` with readable metadata) does not throw and returns the expected entry, in the existing store test files; update `EncryptedFieldTests.cs`, `EncryptedFieldUnreadableTests.cs` (every failure reason through the non-throwing read), `MartenOperationAuditOptionsTests.cs`; property test `AuditMartenEncryptedFieldProperties.cs` (the throwing and non-throwing reads agree).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 2. EncryptedField currently throws EncryptedFieldDecryptionException from Decrypt; projections let it escape (part 2a). The lookup in Phase 3 is asynchronous, so the projection must decide without catching.
TASK: Add an internal non-throwing read to EncryptedField, MartenOperationAuditOptions.UnreadablePlaceholder (default "[UNREADABLE]"), and IsUnreadable on both read models; keep the public throwing contract unchanged.
KEY RULES: never use exceptions for business logic (AGENTS.md section 3, ADR-006); the failure reason is the only output of the failure (never a message, envelope or key id); amend, do not duplicate, the XML docs; PublicAPI lines; per-flag manifest targets; CRAP <= 10; tests execute real code.
REFERENCE FILES: src/Encina.Audit.Marten/Events/EncryptedField.cs, src/Encina.Audit.Marten/Events/EncryptedFieldDecryptionException.cs, src/Encina.Audit.Marten/MartenOperationAuditOptions.cs, tests/Encina.UnitTests/AuditMarten/EncryptedFieldUnreadableTests.cs
```

</details>

### Phase 3: Projection Lookup & Apply

> **Goal**: Both projections consult a resolution on the failure path and apply it deterministically. **Size**: 14 files, ~850 lines. PR 3, `Refs #3049`. Starts after the #3012 PR merges.

<details>
<summary><strong>Tasks</strong></summary>

1. Day-1 experiment (see the algorithm section) in `tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/AuditProjectionUnreadableEventIntegrationTests.cs`; record the outcome in the PR description.
2. `Projections/OperationAuditEntryProjection.cs`: replace the throwing `LoadTemporalKeyAsync`/`ClassifyTemporalKeyLookup` with the internal `TemporalKeyOutcome` (`Key`, `Shredded`, `Expired`, `Failure(reason)`; M2 design, see the algorithm section); `ReadAuditEntryProjection.cs` reuses it. Both projections: constructors gain `unreadablePlaceholder` (3 to 4 parameters, same `ThrowIfNullOrWhiteSpace`); `Create` takes `IEvent<T>` (or the fallback `Project` method) and returns no row for `Excluded`; the outcome mapping is a pure internal method (one `switch`, `// crap-exempt: single-question switch` when it qualifies); the lookup helper loads by primary key (`All`, then the failing field) through the `IDocumentOperations` parameter.
3. `Projections/ConfigureMartenOperationAuditProjections.cs`: pass `UnreadablePlaceholder` from `MartenOperationAuditOptions` to both projections.
4. `Diagnostics/MartenAuditLog.cs`: 2593 `UnreadableEventDetected` and 2594 `ResolutionApplied`; update the block table; confirm `EncinaEventIdAllocationTests` passes.
5. `PublicAPI.Unshipped.txt` (remove the old constructor lines and the old public `Create` lines, add the new constructors and the `Create` with `IEvent<T>` and its nullable return, RS0016/RS0017), coverage manifest, XML docs.
6. Tests: update `OperationAuditEntryProjectionTests.cs`, `ReadAuditEntryProjectionTests.cs`, `OperationAuditEntryProjectionGuardTests.cs`, `ConfigureMartenOperationAuditProjectionsTests.cs`; integration: pause on a poison event, write a resolution document directly, restart (new host over the same store) and rebuild (`RebuildProjectionAsync`), assert the read model; rebuild twice and compare field by field.
7. Replace the untyped `KeyNotFoundException` expectations at `IT:206-250` of `AuditProjectionUnreadableEventIntegrationTests` with the typed `KeyMissing` failure (the #3012 PR changes them; re-check after the merge), and add acceptance tests for the precedence rule (`All` versus field-specific, `Excluded` wins).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 3 (needs the #3012 PR merged: KeyMissing typed failure; ClassifyTemporalKeyLookup currently throws and must become a TemporalKeyOutcome). Marten async daemon pauses the shard on a thrown projection failure; a rebuild re-projects every event.
TASK: First run the day-1 experiment (EventProjection.Create returning null; Create(IEvent<T>, IDocumentOperations, CancellationToken) accepted by Marten 9.38) and choose Create or the Project fallback. Then make both projections look up AuditResolution (ids audit-resolution:{trail}:{eventId}:{field}) only when a field failed, apply Unreadable/Shredded/Expired/Excluded, and rethrow the original typed failure when no resolution exists.
KEY RULES: Marten projections take dependencies via IDocumentOperations and constructor injection, NEVER IServiceProvider; zero extra queries on the healthy path; the outcome depends only on the event, the stored kind/reference and the three placeholders (no clock, actor or random); a lookup exception propagates (never swallowed); log codes only (EventIds 2593-2594); integration tests use the shared MartenCollection fixture and prove restart and rebuild, rebuilding twice and comparing.
REFERENCE FILES: src/Encina.Audit.Marten/Projections/OperationAuditEntryProjection.cs, src/Encina.Audit.Marten/Projections/ReadAuditEntryProjection.cs, tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/AuditProjectionUnreadableEventIntegrationTests.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs
```

</details>

### Phase 4: `IAuditProjectionResolver`, Audit-First Write & DI

> **Goal**: The operator service, audited and fail closed. **Size**: 13 files, ~1,300 lines (close to the limit: split at brief time into 4a, the interface, model records, validation and the audit-entry builder, and 4b, the write flow, bulk helper and DI, when the count passes 15 files or 1500 lines). PR 4, `Refs #3049`.

<details>
<summary><strong>Tasks</strong></summary>

1. `Resolution/IAuditProjectionResolver.cs`, `AuditProjectionResolver.cs` (internal sealed, scoped), `AuditResolutionDecision.cs`, `AuditResolutionBatchResult.cs`. Constructor dependencies (proposal): `IDocumentSession`, `IServiceScopeFactory` (the audit store is resolved in its own scope, as `PersistentPolicyAdministrationPoint` does, so a failed document write cannot taint the audit session), `IRequestContextAccessor`, `IOptions<MartenOperationAuditOptions>`, `TimeProvider`, `ILogger<AuditProjectionResolver>`.
2. Flow per write: read the actor once (`Left` `audit.marten.resolution_principal_required` when anonymous) → validate (field set, reason, reference, event exists by `IEvent.Id`) → load the existing document (identical → `Right`, no second entry; different kind → `Left` conflict) → write the audit entry and await it (a `Left`, an exception or the 30 s timeout fails the resolution) → insert the document and save → on a duplicate-key error reload the stored document and compare (identical returns `Right`, different returns `Left` conflict); on any other failed save write a second entry with outcome `Error` and return `Left`. `RemoveAsync` follows the same order with its own action.
3. `ServiceCollectionExtensions.cs`: `TryAddScoped<IAuditProjectionResolver, AuditProjectionResolver>()` and `TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>()`.
4. `PublicAPI.Unshipped.txt`, coverage manifest.
5. Tests: unit (every `Left` path, idempotent repeat writes no second entry, audit failure persists no document, entry content: no reason in the document, hash recomputable), guard (every public method and constructor), DI test (`ValidateOnBuild` and `ValidateScopes`, store wins in any registration order); integration of the resolver against real Marten for at least `MarkUnreadableAsync` and the audit-failure case.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 4. Projections (Phase 3) already apply AuditResolution documents; this phase writes them. Precedent for actor/audit-first/document-second: src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs.
TASK: Implement IAuditProjectionResolver and AuditProjectionResolver as in the plan's API proposal, with the per-write flow in the Phase 4 tasks, and register it in AddEncinaAuditMarten.
KEY RULES: Either<EncinaError, T> and CancellationToken everywhere; actor from IRequestContextAccessor read once, never a parameter, anonymous refused; audit entry awaited BEFORE the document, any failure fails the resolution; reason (required, non-blank, <= 500 chars) only in the entry Metadata; SHA-256 of the canonical fields in RequestPayloadHash; TimeProvider injected; errors carry codes only; DI test with ValidateOnBuild and ValidateScopes; max 15 files and 1500 lines.
REFERENCE FILES: src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs, src/Encina.Audit.Marten/Crypto/MartenTemporalKeyProvider.cs, src/Encina.Audit.Marten/MartenAuditErrors.cs, src/Encina.Audit.Marten/ServiceCollectionExtensions.cs, src/Encina.Security.Audit/OperationAuditEntry.cs
```

</details>

### Phase 5: Cross-Cutting Integration & Observability

> **Goal**: Telemetry and logs for the resolver, tenant propagation verified. **Size**: 9 files, ~450 lines. PR 5, `Refs #3049`.

<details>
<summary><strong>Tasks</strong></summary>

1. `Diagnostics/MartenAuditMeter.cs`: `internal static readonly Counter<long> Resolutions` (`encina.audit.marten.resolutions`, tags `kind`, `trail`).
2. `Diagnostics/MartenAuditActivitySource.cs`: `StartResolve(kind, trail)` with the tags `audit.marten.resolution_kind` and `audit.marten.resolution_trail`; never the reason, actor or ids; completion: the existing `Complete` always sets `ActivityStatusCode.Ok`, so add an error path (`Fail(activity, errorCode)`, status `Error` with the code only) used for every `Left` and failed audit write.
3. `Diagnostics/MartenAuditLog.cs`: 2595 `ResolutionRecorded`, 2596 `ResolutionRejected`, 2597 `ResolutionAuditFailed`, 2598 `ResolutionRemoved`; update the block table.
4. `AuditProjectionResolver.cs`: call the counter, activity and log messages; the document `TenantId` copied from the event, the entry `TenantId` from the actor.
5. Coverage manifest. Tests: unit for the meter and activity tags (no reason or actor present), log tests for the codes-only rule, a tenant propagation test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 5. The package already has MartenAuditMeter, MartenAuditActivitySource and MartenAuditLog; EventId range AuditMarten 2550-2599 is registered in src/Encina/Diagnostics/EventIdRanges.cs; 2593-2594 are used by Phase 3.
TASK: Add the resolutions counter, the AuditMarten.Resolve activity and EventIds 2595-2598, and call them from AuditProjectionResolver; verify TenantId on the document and the entry.
KEY RULES: pack EventIds sequentially, no gaps (AGENTS.md section 7), 2599 stays free; telemetry and logs carry codes, kind and trail only, never the reason, the actor or free text; exceptions through ForLogging(); run EncinaEventIdAllocationTests; zero warnings.
REFERENCE FILES: src/Encina.Audit.Marten/Diagnostics/MartenAuditMeter.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditActivitySource.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 6: Testing — Full-Flow Integration, Property, Contract & Justification Files

> **Goal**: Every coverage flag reaches its target and the issue's acceptance tests exist. **Size**: 9 files, ~800 lines. PR 6, `Refs #3049`.

<details>
<summary><strong>Tasks</strong></summary>

Test matrix (the flags of AGENTS.md section 9; phases 1 to 5 add the unit and guard tests next to their code, this phase completes the other flags):

| Flag | Required? | What it proves | Notes |
|------|-----------|----------------|-------|
| Unit | Required | Resolver, kind mapping, projection outcome for every kind, every `Left` path, idempotency, audit-first ordering | Real package code, dependencies mocked; added in phases 1 to 5 |
| Guard | Required | `ArgumentNullException` on every new public method and constructor with parameters | `GuardClauses.xUnit`; added in phases 1 to 5 |
| Contract | Required | `IAuditProjectionResolver`, the public records and enums keep their contract; new folder `tests/Encina.ContractTests/AuditMarten/` | Instantiates the real resolver |
| Property | Required | FsCheck: independent resolutions applied in any order, and projecting twice, give equal read models; id format round-trips; validation never throws | Complex enough: determinism is the feature's core promise |
| Integration | Required | Real Marten on PostgreSQL through the shared `MartenCollection` fixture: pause on a poison event, each kind applied, restart and rebuild pick it up, rebuild twice equal field by field, identical repeat is `Right` with no second audit entry, a different kind is `Left` conflict, no principal is `Left`, audit write failure persists no document | Extends `AuditProjectionUnreadableEventIntegrationTests`; `[Trait("Category", "Integration")]` |
| Load | Not required | Admin operation; only the failure path does a lookup | `.md` justification file in `tests/Encina.LoadTests/AuditMarten/` |
| Benchmark | Not required | Failure path only, not a hot path | `.md` justification file in `tests/Encina.BenchmarkTests/AuditMarten/` |

1. New integration test file(s) for the acceptance scenarios above; new `tests/Encina.ContractTests/AuditMarten/AuditProjectionResolverContractTests.cs`; new `tests/Encina.PropertyTests/AuditMartenResolutionProperties.cs`.
2. Two justification `.md` files with the headings required by AGENTS.md section 9.
3. `.github/coverage-manifest/Encina.Audit.Marten.json`: final per-file targets; measure each flag (`dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" ...`), run `coverage-report.cs` and `--check-justifications`; keep every changed method at CRAP 10 or less.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 6. The code exists (phases 1-5); this PR adds the cross-flag tests and the coverage proof. Integration tests use real Marten on PostgreSQL (Testcontainers) through the shared MartenCollection fixture.
TASK: Write the integration scenarios listed in the plan's test matrix, the contract and property tests, the Load and Benchmark justification .md files, then measure per-flag coverage of the changed files against the manifest and fix gaps.
KEY RULES: tests MUST execute real package code (no reflection-only tests); Shouldly through Encina.Testing.Shouldly; never IClassFixture for database fixtures; ClearAllDataAsync in InitializeAsync; no Thread.Sleep; justification files never for unit, guard or contract tests; report the measured per-flag coverage in the PR.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/AuditProjectionUnreadableEventIntegrationTests.cs, tests/Encina.PropertyTests/AuditMartenEncryptedFieldProperties.cs, .github/coverage-manifest/Encina.Audit.Marten.json, docs/testing/coverage-measurement-methodology.md
```

</details>

### Phase 7: Documentation & Finalization

> **Goal**: The runbook, the ADR and the changelog. **Size**: 7 files, ~450 lines. PR 7, `Fixes #3049`.

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/features/audit-marten.md` (by `docs-writer`): replace the sentences "Operator resolution ... is not available until the later change tracked as PR 2b" and the "Limits today" bullet (lines 209 and 221 at the time of writing; also the stale query-helper note near line 205, the manual marker-editing advice near line 214, and the `KeyNotFoundException` text near line 185 once the #3012 PR has changed it); add the how-to "Resolve an unreadable audit event" (recognise, choose the label, call the resolver, restart or rebuild, verify, with a Mermaid flow and a code sample) and a reference section (API, placeholders, flags `IsShredded`/`IsUnreadable`/`IsExpired`, error codes); state the exclusion caveat, the `AllowSkippedProjectionErrors` caveat and the tamper limit; cite coverage only through `cov:` markers.
2. `docs/architecture/adr/059-audit-projection-resolution.md` (new) and its row in `docs/architecture/adr/index.md`: Context, Decision (document, event id, kinds, determinism, audit-first, tamper limit), Alternatives (choices 1 to 8), Consequences; one-line pointer amendment in ADR-020.
3. Changelog fragments (`mechanical-fixer`), named by the rule `changelog.d/<issue>-<slug>.<section>.md` with issue 3049 (the issue's Documentation checklist says `2770-...` and should be edited to `3049-...`): `changelog.d/3049-audit-projection-resolver.added.md` (resolver, document, `[UNREADABLE]`, `IsUnreadable`) and `changelog.d/3049-audit-unreadable-placeholder.changed.md` (projection constructors, typed missing-key failure).
4. Package README only if it describes the placeholders; `docs/INVENTORY.md` if it lists package types; `ROADMAP.md` is not affected.
5. Close-out: open or confirm the follow-up issues #3024, #3025, #3026, #3027; run `check-plan` without `-Draft`; build with zero warnings and run all test flags.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: Issue #3049, plan Phase 7. Code and tests are merged (phases 1-6). Docs follow the encina-docs skill (.claude/skills/encina-docs/SKILL.md): one Diataxis quadrant per page, junior-readable English, no hand-typed coverage figures.
TASK: Update docs/features/audit-marten.md (how-to plus reference), write ADR-059 and its index row, add the ADR-020 pointer, add the two changelog fragments.
KEY RULES: confirm ADR number 059 is free with git ls-tree origin/main docs/architecture/adr/ --name-only (058 is open PR #2980); every identifier named in docs exists in src/; coverage only via cov: markers; no emojis; the fragments are written by mechanical-fixer; the PR says Fixes #3049.
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
| GDPR | Art. 5(2) accountability | A resolution records who, when and why; the label states the legal fact only with its reference |
| GDPR | Art. 17 erasure | `[SHREDDED]` only with an `ErasureReference` (Q1) |
| ADR-020 | Temporal crypto-shredding audit store | Keys and markers live in the same PostgreSQL database; amended for #2770 |
| ADR-034 / ADR-036 | Marten encryption failure; three audit stores | Integrity over availability; operation and read audit stay separate |
| ADR-006 / ADR-001 | Pure ROP | `Either` returns, no exceptions as business logic |
| SPEC-002 DEC-006 | Compliance gates fail closed | Anonymous callers and a failing audit write refuse the resolution |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `TemporalKeyDestroyedMarker`, `TemporalKeyDocument` | `src/Encina.Audit.Marten/Crypto/` | Pattern for the document and its string id; same store |
| `ErasureReference`, `RetentionReference`, `MartenAuditErrors.RejectInvalidReference` | `src/Encina.Audit.Marten/Crypto/`, `MartenAuditErrors.cs` | Reference validation for `Shredded` and `Expired` |
| `OperationAuditEntryProjection`, `ReadAuditEntryProjection` | `src/Encina.Audit.Marten/Projections/` | Gain the failure-path lookup |
| `ConfigureMartenOperationAuditProjections` | `src/Encina.Audit.Marten/Projections/` | Registers the table and passes the placeholder |
| `IOperationAuditStore`, `OperationAuditEntry` | `src/Encina.Security.Audit/` | The audit-first record (`RequestPayloadHash`, `Metadata`, `ErrorMessage` code) |
| `PersistentPolicyAdministrationPoint` | `src/Encina.Security.ABAC/Administration/` | Precedent: actor from `IRequestContextAccessor`, audit awaited before the change |
| `IRequestContextAccessor`, `IRequestContextScopeFactory.RunAsServiceAsync` | `Encina` core | Ambient actor; background and CLI hosts |
| `MartenAuditMeter`, `MartenAuditActivitySource`, `MartenAuditLog` | `src/Encina.Audit.Marten/Diagnostics/` | Counter, activity, EventIds |
| `AuditProjectionErrorPolicyStartupGuard` | `src/Encina.Audit.Marten/Projections/` | Unchanged; exclusion relies on the store-wide flags being `false` |
| `AuditProjectionUnreadableEventIntegrationTests` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/` | Template for pause, restart and rebuild proofs |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Audit.Marten` | 2550-2599 (registered, `EventIdRanges.AuditMarten`) | Used: 2550-2554, 2560-2561, 2563, 2570-2574, 2580-2586, 2590-2592. New: **2593-2598** (2593-2594 Phase 3, 2595-2598 Phase 5); 2599 stays free. No new range needed |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production (new) | 12 | 3 enums, `AuditResolution`, `AuditResolutionRecord`, `AuditResolutionIds`, `AuditResolutionDecision`, `AuditResolutionBatchResult`, `IAuditProjectionResolver`, `AuditProjectionResolver`, `TemporalKeyOutcome` |
| Production (modified) | 15 | `EncryptedField`, two projections, two read models, two query stores, options, `MartenAuditErrors`, configure, log, meter, activity source, `ServiceCollectionExtensions`, `PublicAPI.Unshipped.txt` |
| Tests | 18-22 | Unit, guard, contract, property, integration across seven PRs, plus 2 justification `.md` files |
| Coverage manifest | 1 | Touched by every code PR |
| Docs and changelog | 5 | Runbook, ADR-059, ADR index, ADR-020 pointer, 2 fragments |

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
- Part 2a (merged) made unreadable audit fields fail typed so the async daemon pauses the shard; the #3012 PR adds the typed KeyMissing failure and must be merged before Phase 3.

IMPLEMENTATION OVERVIEW:
Phase 1: AuditResolution document, enums, ids, error codes, Schema registration. Phase 2: non-throwing EncryptedField read, UnreadablePlaceholder, IsUnreadable. Phase 3: projections look up and apply a resolution on the failure path only (day-1 Marten 9.38 experiment first). Phase 4: IAuditProjectionResolver, audit entry awaited before the document, DI. Phase 5: meter, activity, EventIds 2593-2598. Phase 6: integration, contract, property tests and justification files. Phase 7: runbook, ADR-059, changelog. One PR per phase, at most 15 files and 1500 lines each.

KEY PATTERNS:
- Document id audit-resolution:{trail}:{eventId}:{field}; eventId is Marten IEvent.Id; field set is a closed enum plus All.
- Projections take dependencies through IDocumentOperations and the constructor, never IServiceProvider; the outcome depends only on the event, the stored resolution and the placeholders.
- Actor from IRequestContextAccessor, anonymous refused (audit.marten.resolution_principal_required); different kind -> audit.marten.resolution_conflict; identical repeat -> Right, no second audit entry.
- Reason required, <= 500 chars, only in the encrypted audit entry Metadata; SHA-256 of canonical fields in RequestPayloadHash; logs and tags carry codes only.
- Per-flag coverage targets in .github/coverage-manifest/Encina.Audit.Marten.json; CRAP <= 10; PublicAPI.Unshipped.txt.

REFERENCE FILES: docs/plans/audit-projection-resolver-implementation-plan-3049.md, src/Encina.Audit.Marten/Projections/OperationAuditEntryProjection.cs, src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs, tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/AuditProjectionUnreadableEventIntegrationTests.cs, docs/features/audit-marten.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Resolutions are read only on the failure path; caching would break determinism (D37). |
| 2 | OpenTelemetry | ✅ | Counter `encina.audit.marten.resolutions` (tags `kind`, `trail`) and activity `AuditMarten.Resolve`; never reason, actor or ids (D38, Phase 5). |
| 3 | Structured Logging | ✅ | EventIds 2593-2598 in `MartenAuditLog`, codes only (D39, Phases 3 and 5). |
| 4 | Health Checks | ⏭️ | The resolver's only dependency is the Marten store the existing check already probes; a check for a paused shard needs an in-process daemon and is tracked by #3027 (D40). |
| 5 | Validation | ✅ | The resolver validates trail and field, kind, reason, references and event existence and returns `Left` (D41, Phase 4). |
| 6 | Resilience | ❌ | No external system beyond the Marten store; one awaited write with the 30 s audit-write timeout copied from `PersistentPolicyAdministrationPoint` (D42). |
| 7 | Distributed Locks | ❌ | Concurrent resolutions of one key are serialised by the deterministic id and the store's uniqueness plus the conflict rule (D43). |
| 8 | Transactions | ❌ | One document write per resolution; audit-first ordering replaces a shared unit of work (D44). |
| 9 | Idempotency | ✅ | An identical repeat returns `Right` and writes no second audit entry (D45, Phase 4). |
| 10 | Multi-Tenancy | ✅ | The document copies the event's `TenantId`; the entry carries the actor's tenant; no isolation check because the shard is global across tenants (D34, D46). |
| 11 | Module Isolation | ❌ | `OperationAuditEntry` and the Marten events have no `ModuleId` (D35, D47). |
| 12 | Audit Trail | ✅ | This feature is the audit trail for resolutions: one `OperationAuditEntry` per resolution, written first (D28, D48). |

---

## Prerequisites & Dependencies

| Item | Status | Effect |
|------|--------|--------|
| #3012 PR (`Fixes #3012`; `ClassifyTemporalKeyLookup` throws `EncryptedFieldDecryptionException(KeyMissing)` for "neither key nor marker") | Fix done on `fix/3012-audit-key-missing-typed-failure`, PR being opened (orchestrator, 2026-10-30) | Phases 3 to 7 depend on it being merged; re-merge `origin/main` when it lands |
| PR #2928 (part 2a) | Merged 2026-10-29 | Typed failure, `[SHREDDED]`/`[EXPIRED]` rules, startup guard |
| Issues #3024, #3025, #3026, #3027, #2910 | Open follow-ups | Out of scope; linked from the PRs |
| ADR number | 059 free on `origin/main`; 058 held by open PR #2980 | Re-check in Phase 7 |

**Risks**

| Risk | Impact | Mitigation |
|------|--------|------------|
| Marten 9.38 `Create` may not accept a `null` return, or progress may not advance past an excluded event | Exclusion would not work | Day-1 integration experiment in Phase 3; fallback `Project` method that stores only non-excluded events |
| `Create(IEvent<T>, IDocumentOperations, CancellationToken)` not accepted by the validator | Event id unavailable to the projection | Same experiment; the class remarks name the non-generic `JasperFx.Events.IEvent` as allowed, so the generic `IEvent<T>` is unverified and proven before other Phase 3 code |
| The #3012 PR merges late or changes the shape of `ClassifyTemporalKeyLookup` | Phase 3 delayed or reworked | Phases 1 and 2 independent; Phase 3 is written against the post-#3012 code and re-checked after the merge |
| `[UNREADABLE]` metadata reaching the JSON parser of the query stores | Queries returning a resolved event throw | Phase 2 makes the metadata handling value-based and tests it (M1) |
| Size rule: a phase exceeds 15 files or 1500 lines | PR not reviewable | Re-measure at brief time; split further instead of widening |
| Tamper limit: a resolution document can be edited in SQL | Forged `[SHREDDED]` evidence on rebuild | D27 = (a): hash in the audit entry, documented in the runbook and ADR-059; HMAC or a package-wide integrity design only if SPEC-002 needs it |
| ADR number collision with #2980 | ADR file conflict | 059 chosen; confirm when Phase 7 starts |
| Store-wide skip flags (`SkipApplyErrors`, `SkipSerializationErrors`) set to `true` by the application | An unresolved failure is skipped silently | The startup guard fails the host unless `AllowSkippedProjectionErrors` is set; the runbook states the consequence (D56) |

**Assumptions** (no open questions): (1) the #3012 PR adds `EncryptedFieldDecryptionFailureReason.KeyMissing` and merges before Phase 3; (2) the audit store is resolved in its own scope, as in `PersistentPolicyAdministrationPoint`; (3) the bulk helper stops at the first `Left` and returns it (D23 = (d) asks for "a bulk helper" without a shape); (4) `Excluded` is accepted only with field `All`; (5) the 500-character reason limit is an implementation constant the DPO may change; (6) the last PR closes #3049 and also #2770 when no other child of #2770 is open (the issue text asks for `Fixes #2770` in the closing PR).

---

## Next Steps

1. Review this plan; the orchestrator links it from #3049 (`artifacts/issues/3049-plan-link.md`).
2. When #3049 is picked up, re-check every Design Choice against today's code, run the decision-extractor and the adversarial review, then run `check-plan` without `-Draft`.
3. Decompose the seven phases into ready sub-issues (size rule), one worker brief each.
4. Phases 1 and 2 can run in parallel; Phase 3 waits for the #3012 PR to merge; Phases 4 to 7 follow in order.
5. Each PR reports the measured per-flag coverage of its files against the manifest; the last PR says `Fixes #3049`.

---

## Maintainer Decisions

1. (2026-10-25) **Q2 = (a)** on #2770 (comment 6395857025): operator resolution is a first-class audited feature; a resolution document in Marten keyed by event and field, applied before the typed failure is thrown, deterministic on rebuild.
2. (2026-10-29) **D24 = (b)**: the event is identified by Marten `IEvent.Id`, with the payload id stored as informational data. Screened as settled by rule in `artifacts/decisions/2770-2b-screen.md`; posted and not vetoed in comment 6521533698 (2026-10-30).
3. (2026-10-29) **D25 = (b)**: `Expired` with a validated `RetentionReference` and `MarkExpiredAsync`, on top of Q3 = (a) (2026-10-26, comment 6413750564) and Q1 = (b) (2026-10-25, comment 6395833133). Comment 6521533698 records it as settled by rule and not vetoed.
4. (2026-10-30) **D23 = (d)**: resolutions stay at event and field granularity (Q2), with a bulk helper; the period-wide case (legacy or damaged marker without a reference) is covered by #2910; a period scope is reopened only if real data needs it (comments 6521533698 and 6522969450).
5. (2026-10-29) **D26 = (a)** (settled by rule, not vetoed, comment 6521533698): a different kind is rejected with `audit.marten.resolution_conflict`, an identical repeat is idempotent, a change goes through an audited removal.
6. (2026-10-29) **D27 = (a)** (settled by rule, not vetoed, comment 6521533698): tamper risk accepted and documented, with a SHA-256 of the canonical fields in the audit entry; the projection never trusts `ResolvedBy`.
7. (2026-10-25) **Q2 = (a)**: the service is usable from code, a CLI or an admin endpoint; this feature delivers the service, and the CLI and endpoint are the follow-ups #3024 and #3025 (screened S3).
8. (2026-10-25) **D14 = (c)** (comment 6395444785) and **D15 amended** (comment 6395499536): integrity over availability; the shard pauses on an unreadable field and the operator resolves it; a missing or destroyed key without a recorded reference is not a lawful erasure.
9. (2026-10-25) **Q2 = (a)**: every resolution is recorded in the audit trail (who, when, reason, label) and logs carry codes only.
10. (2026-10-27) **D21 = (b)** and **D22 = (e)** (comment 6440026324): the retention policy id option, and Encina does not change Marten's store-wide error flags; the startup guard stays the fail-closed check.
