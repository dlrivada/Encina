# Implementation Plan: Subject-scoped audit keys for exceptional, justified erasure of one person's audit records

> **Issue**: [#2903](https://github.com/dlrivada/Encina/issues/2903)
> **Type**: Feature
> **Complexity**: High (7 phases, 1 provider: Marten, ~28 files, proposed split in 2 PRs)
> **Estimated Scope**: ~1,100 lines of production code + ~1,800 lines of tests

---

## Summary

Audit PII fields are encrypted with per-period temporal keys (`AuditEventEncryptor`, `ITemporalKeyProvider`). Destroying a period key shreds every subject of that period, so a court order to delete one person's audit records cannot be met without collateral loss. The maintainer decision of 2026-10-26 on #2847 makes that capability **path A**: an explicit opt-in, last-resort route, never the default (default is path C, planned in the #2847 plan: record the request, keep the data until retention). This plan adds subject-scoped keys to `Encina.Audit.Marten`:

- An opt-in flag, `MartenOperationAuditOptions.EnableSubjectScopedErasure` (default `false`). When on, the PII fields of entries whose actor is known are encrypted in two layers: inner with a per-subject key, outer with the period key (the retention shred still works). Entries written before enabling carry no subject layer and cannot be subject-shredded (events are immutable); the documentation says so.
- `IAuditSubjectErasureService.ShredSubjectAsync(subjectId, justification, ct)`: fails closed (`Left`) when the option is off or the justification (legal basis or order reference, actor, date) is invalid; writes a write-ahead audit entry of the shred; deletes the subject key; stores a shred record; entries of that subject then project as `[SHREDDED]` carrying the justification reference; other subjects stay readable.
- Distinct from `[EXPIRED]` (retention) and from the typed failure of an unrecorded destruction (#2770).

- **Standards**: GDPR Art. 17 (exceptional erasure order), Art. 5(2) (accountability: the shred is justified and audited).
- **Affected packages**: `Encina.Audit.Marten` (the change), `Encina.Marten.GDPR` (reuses `ISubjectKeyProvider`, no change), `Encina` core (`EventIdRanges` registration).
- **Provider category**: event sourcing, Marten on PostgreSQL only; no database-provider matrix.
- **Scope and split**: about 28 files. Two worker PRs: PR 1 = key hierarchy and decryption (Phases 1-3, about 14 files); PR 2 = service, records, observability, docs (Phases 4-7, about 14 files).
- **Prerequisites**: the recorder and legal-basis types of the #2847 plan are independent of this plan; `ErasureReference`/`RetentionReference` and the marker already exist (PR 2a of #2770). Shared pieces: the marker semantics (`[SHREDDED]` only with a recorded reference) are owned by #2770; EventIds 2593-2599 of `AuditMarten` belong to the #2847 plan, this plan uses its own range.

---

## Design Choices

<details>
<summary><strong>1. Key hierarchy: how the subject key combines with the period key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two layers: field encrypted with the subject key, result encrypted with the period key** | Both shreds work independently: period expiry and subject shred; one decrypt path per layer; no retention regression | Double encryption cost on opted-in entries; two key lookups on read |
| **B) Subject key replaces the period key for subject fields** | One layer | Retention expiry no longer shreds those fields (period key unused), breaking Art. 5(1)(e) and the `[EXPIRED]` path |
| **C) Per-entry data key wrapped by the period key and by the subject key** | Standard envelope encryption | Largest change: new wrapped-key fields, rotation and re-wrap semantics; overkill for a rare path |

### Chosen Option: **A — two layers (recommended, pending the maintainer)**

### Rationale

- Option A wins when retention shredding must keep working for opted-in entries (the default expectation); option C wins only if the maintainer later wants many independent shred axes (per tenant, per case) on the same entry.
- `EncryptedField` (`Events/EncryptedField.cs`) gains a nullable `SubjectKeyRef`; entries without it decrypt exactly as today (no migration).

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Isolation from the application's own subject keys</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `ISubjectKeyProvider` with a reserved subject-id namespace (`audit:{tenant}:{subjectId}`)** | The issue asks to reuse it; every key store (PostgreSQL, in-memory) works; no new interface | The namespace convention must be enforced by one factory |
| **B) A second, audit-only provider interface and store** | Total separation | Duplicates the provider and its tests; two key stores to operate |
| **C) Share the application's subject key unchanged** | Zero code | Erasing the application subject (`CryptoShredErasureStrategy`) would also shred the audit trail, contradicting the default of path C |

### Chosen Option: **A — namespaced reuse**

### Rationale

- `CryptoShredErasureStrategy.EraseFieldAsync` deletes keys by the plain subject id, so the `audit:`-prefixed ids are untouched by ordinary erasure.
- The prefix is applied in one internal type, `AuditSubjectKeyNamespace`, and tenant-scoped when a tenant is present (multi-tenancy, matrix function 10).

- **Class:** Settled
- **Settled by:** maintainer decision of 2026-10-26 on #2847 ("a request reaching the audit trail never destroys other audit data by default") at https://github.com/dlrivada/Encina/issues/2847 and the reuse requested in the #2903 body

</details>

<details>
<summary><strong>3. Who counts as "the subject" of an audit entry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The entry's principal (`UserId`) for operation and read entries** | Deterministic at write time; matches what an order about "one person's audit records" means in practice | Entries about a person as the target (`EntityId`) stay outside the scope |
| **B) Principal and target (`EntityId` resolved to a data subject)** | Complete | Requires resolving entity ids to subjects at write time through the application; no abstraction exists |

### Chosen Option: **A — principal only (recommended, pending the maintainer)**

### Rationale

- Option A wins for the stated use case (a court order about the records a person generated or read); option B wins if the reference application must also erase records about a person made by staff.
- B needs a new resolver abstraction and is a follow-up if the maintainer picks it.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Opt-in, fail closed and justification</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Flag off by default; the service returns `Left` when off or when the justification is incomplete; the shred is recorded before it runs** | Meets the decision; no silent shred | Operators must enable the flag before the entries are written |
| **B) Always available, justification optional** | Simple | Violates opt-in and fail closed |

### Chosen Option: **A — opt-in, fail closed, audited**

### Rationale

- The justification is a record `SubjectErasureJustification(Reference, Actor, DateOnly Date)` validated before any write; a missing piece returns `MartenAuditErrors.InvalidErasureReference`-style error code `audit.marten.invalid_shred_justification`.
- The audit entry of the shred is written ahead and awaited (ADR-058 D15-D17); if it fails, nothing is destroyed.

- **Class:** Settled
- **Settled by:** maintainer decision of 2026-10-26 on #2847 (path A is explicit opt-in, fail closed without a justification reference, logged as an audit event) at https://github.com/dlrivada/Encina/issues/2847, AGENTS.md section 3 and SPEC-002 DEC-006

</details>

<details>
<summary><strong>5. Evidence that makes a destroyed subject key lawful</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An opaque `SubjectKeyRef` in each field, an index document `AuditSubjectKeyIndex` (ref to subject) deleted at shred, and a `AuditSubjectShredRecord` (ref plus justification) kept forever** | The projection tells a lawful shred (`[SHREDDED]` plus reference) from tampering (typed failure); the record holds no subject identifier | Two small documents and one lookup per distinct ref per projection batch |
| **B) Store the subject id in the field** | No index | The identifier would survive the shred inside the immutable event |
| **C) Reuse `SubjectForgottenMarker` of `Encina.Marten.GDPR`** | Existing type | It stores the plain subject id and no justification |

### Chosen Option: **A — opaque reference, index, shred record**

### Rationale

- This mirrors the period design: marker with a reference means lawful (`[SHREDDED]`), no marker means the shard pauses with `EncryptedFieldDecryptionException` and an operator resolves it.
- The marker text is `MartenOperationAuditOptions.ShreddedPlaceholder` (`[SHREDDED]`), unchanged.

- **Class:** Settled
- **Settled by:** #2770 (a destruction without a recorded reference is not a lawful erasure), ADR-034 and decision D15 recorded in the #2903 body

</details>

---

## Implementation Phases

### Phase 1: Option, justification and namespace types

<details>
<summary>Tasks</summary>

1. `MartenOperationAuditOptions.EnableSubjectScopedErasure` (default `false`, XML docs naming the limitation for earlier entries) and validator rule.
2. `src/Encina.Audit.Marten/SubjectErasure/SubjectErasureJustification.cs` (record: `Reference`, `Actor`, `Date`; `IsValid`).
3. `SubjectErasure/AuditSubjectKeyNamespace.cs` (internal): builds `audit:{tenant}:{subjectId}` and the opaque ref.
4. Documents `AuditSubjectKeyIndex` and `AuditSubjectShredRecord` (no subject id in the shred record), registered in `ConfigureMartenOperationAuditProjections`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Audit.Marten; path A of the maintainer decision on #2847; issue #2903, Design Choices 2, 4, 5.
TASK: Add the option, the justification record, the namespace helper and the two Marten documents.
KEY RULES: opt-in default false; Either for validation outcomes; UTC suffix naming; the shred record and every log never contain the subject id; PublicAPI.Unshipped.txt lines (via mechanical-fixer).
REFERENCE FILES: src/Encina.Audit.Marten/MartenOperationAuditOptions.cs, src/Encina.Audit.Marten/Crypto/TemporalKeyDestroyedMarker.cs, src/Encina.Audit.Marten/Projections/ConfigureMartenOperationAuditProjections.cs
```

</details>

### Phase 2: Two-layer encryption on write

<details>
<summary>Tasks</summary>

1. `Events/EncryptedField.cs`: nullable `SubjectKeyRef`; `Encrypt` overload that wraps an already encrypted inner field.
2. `AuditEventEncryptor.EncryptAuditEntryAsync` and `EncryptReadAuditEntryAsync`: when the option is on and `entry.UserId` is present, get/create the subject key through `ISubjectKeyProvider.GetOrCreateSubjectKeyAsync(namespaced id)`, encrypt each PII field with it, then with the period key; set `SubjectKeyRef`; upsert the index document in the same session. A `Left` from the key provider fails the recording (never writes a plaintext or single-layer field when the option is on).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: AuditEventEncryptor encrypts six operation fields and three read fields with the period key (AuditEventEncryptor.cs lines 79 and 155). Phase 1 types exist.
TASK: Implement the inner subject layer and the SubjectKeyRef when EnableSubjectScopedErasure is true; keep the option-off path identical.
KEY RULES: fail closed; key material never logged; async with CancellationToken; ROP; the subject id only travels to ISubjectKeyProvider.
REFERENCE FILES: src/Encina.Audit.Marten/AuditEventEncryptor.cs, src/Encina.Audit.Marten/Events/EncryptedField.cs, src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs
```

</details>

### Phase 3: Decryption and `[SHREDDED]` projection

<details>
<summary>Tasks</summary>

1. `Projections/OperationAuditEntryProjection.cs` and `ReadAuditEntryProjection.cs`: after the period layer, when `SubjectKeyRef` is present resolve the index document (cached per batch), then `GetSubjectKeyAsync`; classify: key found (decrypt), index deleted plus valid `AuditSubjectShredRecord` (field reads `ShreddedPlaceholder`), neither (throw the typed `EncryptedFieldDecryptionException`, the shard pauses for an operator, consistent with #2770).
2. Keep period classification (`ClassifyTemporalKeyLookup`) unchanged and evaluated first: an expired period still reads `[EXPIRED]`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: OperationAuditEntryProjection classifies the period key via ClassifyTemporalKeyLookup (line 228) and decrypts fields (line 301). Marten's async daemon pauses a shard on a thrown typed exception.
TASK: Add the subject-layer classification described in Phase 3 to both projections without changing the period behavior.
KEY RULES: a missing index plus missing shred record is never silently shown as shredded; no subject id in exceptions or logs; per-batch cache only (no cross-batch stale data).
REFERENCE FILES: src/Encina.Audit.Marten/Projections/OperationAuditEntryProjection.cs, src/Encina.Audit.Marten/Projections/ReadAuditEntryProjection.cs, src/Encina.Audit.Marten/Events/EncryptedFieldDecryptionException.cs
```

</details>

### Phase 4: Erasure service

<details>
<summary>Tasks</summary>

1. `SubjectErasure/IAuditSubjectErasureService.cs` and internal `MartenAuditSubjectErasureService` (Scoped): validate flag and justification; write-ahead `OperationAuditEntry` (action `AuditSubjectShredded`, entity id = the opaque ref, never the subject id) through `IOperationAuditStore`, awaited; delete the subject key with `ISubjectKeyProvider.DeleteSubjectKeysAsync` (an already-forgotten subject is success); insert the `AuditSubjectShredRecord`; delete the index document; one session save.
2. Idempotency: the shred record id is the opaque ref; a repeat returns success without a second audit entry.
3. Registration in `AddEncinaAuditMarten` only when the option is on (pay-for-what-you-use), with a DI test using `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1-3 are done. The service is the only public way to shred one subject's audit records.
TASK: Implement IAuditSubjectErasureService and its registration behind the option.
KEY RULES: fail closed with Left; time from TimeProvider; ordering is audit entry, key deletion, shred record, index removal; if the audit entry write fails nothing is destroyed; never log the subject id, the justification text or EncinaError.Message.
REFERENCE FILES: src/Encina.Audit.Marten/MartenOperationAuditStore.cs, src/Encina.Audit.Marten/ServiceCollectionExtensions.cs, src/Encina.Marten.GDPR/KeyStore/PostgreSqlSubjectKeyProvider.cs
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. New range `AuditMartenSubjectErasure = (7200, 7224)` in `src/Encina/Diagnostics/EventIdRanges.cs` (block assigned to this family, registered first, ADR-021); add it to the `Encina.Audit.Marten` entry of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`; add the field to `PublicAPI.Unshipped.txt`.
2. `Diagnostics/MartenAuditSubjectErasureLog.cs` (source-generated, packed 7200-7208): shred requested, rejected (code), completed, repeat, key deletion failed, projection saw a shredded subject field, projection found a missing record.
3. Metric `encina.audit.shredded.subject.count` (`Counter<long>`, tag `outcome`) and activity `encina.audit.shred.subject` with tag `audit.justification_ref` (never the subject id), as in the issue.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: AuditMarten (2550-2599) is almost full, so this feature registers a new range from the family block 7200-7249.
TASK: Register the range, write the log class, the counter and the activity; update the architecture test map.
KEY RULES: register the range before using ids; sequential ids with no gaps; XML docs naming the range; low-cardinality tags; no subject id anywhere.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditMeter.cs
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit: justification validation, namespace builder, encryptor two layers and option-off identity, projection classification (key present, shredded, missing evidence), service ordering and failure injection.
2. Guard: every new public method and constructor (`ArgumentNullException`).
3. Contract: `IAuditSubjectErasureService` contract (success on repeat, `Left` when off or invalid).
4. Property (FsCheck): shredding subject A never changes the decryptability of subject B or of A's entries after period expiry order changes.
5. Integration (Marten on PostgreSQL via Testcontainers, `MartenCollection`, no in-memory store): end to end with two subjects, period expiry still yields `[EXPIRED]`, shred yields `[SHREDDED]`, entries written before enabling stay readable and cannot be shredded, missing evidence pauses the shard.
6. Load and benchmark: `.md` justifications (rare, exceptional path); a note that the read path adds one key lookup per distinct ref.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag coverage (AGENTS.md section 9); manifest .github/coverage-manifest/Encina.Audit.Marten.json (and Encina.Marten.GDPR.json is unchanged).
TASK: Write the tests of Phase 6; add per-file targets with justifications for every touched and new src file through mechanical-fixer; measure each flag.
KEY RULES: real implementations instantiated; Shouldly through Encina.Testing.Shouldly; shared Marten collection fixture and ClearAllDataAsync; compliance modules use Marten on PostgreSQL; CRAP <= 10 on changed methods.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/AuditProjectionUnreadableEventIntegrationTests.cs, tests/Encina.UnitTests/AuditMarten/InMemoryTemporalKeyProviderTests.cs, tests/Encina.GuardTests/AuditMarten/MartenTemporalKeyProviderGuardTests.cs
```

</details>

### Phase 7: Documentation and finalization

<details>
<summary>Tasks</summary>

1. ADR for the key hierarchy: ADR number assigned when the ADR pull request opens (via `docs-writer`).
2. `Encina.Audit.Marten` README and a configuration guide: enabling the option, the limitation for earlier entries, the exceptional-use procedure and the justification (via `docs-writer`).
3. `changelog.d/2903-audit-subject-scoped-erasure.added.md` and PublicAPI lines (via `mechanical-fixer`).
4. `dotnet build --configuration Release` with 0 warnings; every flag reaches its manifest target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Phases 1-6 are implemented.
TASK: Write the documentation, ADR, changelog fragment and PublicAPI lines; run build and tests.
KEY RULES: docs by docs-writer, fragments and manifest lines by mechanical-fixer; never edit CHANGELOG.md; no typed coverage figures in docs; name the reference application only generically.
REFERENCE FILES: src/Encina.Audit.Marten/README.md, docs/architecture/adr/index.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| GDPR Art. 17(1) and 17(3)(b),(e) | Exceptional erasure order versus the default retention exemption |
| GDPR Art. 5(2) | Accountability: justified, audited shred |
| #2847 maintainer decision (2026-10-26) | Path C default, path A exceptional, fields of the justification |
| #2770 Q1/Q3 and ADR-034 | Erasure reference, `[SHREDDED]` versus `[EXPIRED]`, typed failure |
| ADR-058 D15-D17 | Write-ahead, awaited audit of the shred |
| SPEC-002 DEC-006 | Compliance gates fail closed |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ISubjectKeyProvider` | `src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs` | Per-subject keys (`GetOrCreateSubjectKeyAsync`, `GetSubjectKeyAsync`, `DeleteSubjectKeysAsync`) |
| `AuditEventEncryptor` | `src/Encina.Audit.Marten/AuditEventEncryptor.cs` | Gains the inner layer |
| `EncryptedField` | `src/Encina.Audit.Marten/Events/EncryptedField.cs` | Gains `SubjectKeyRef` |
| Projections | `src/Encina.Audit.Marten/Projections/` | Classify `[SHREDDED]` / failure |
| `TemporalKeyDestroyedMarker`, `ErasureReference` | `src/Encina.Audit.Marten/Crypto/` | Pattern for evidence and reference validation |
| `MartenOperationAuditStore` | `src/Encina.Audit.Marten/MartenOperationAuditStore.cs` | Write-ahead audit entry |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Audit.Marten (subject erasure) | 7200-7224 (new range `AuditMartenSubjectErasure`, ids 7200-7208 used) | From the es-integrity-crypto block 7200-7249; registered in `EventIdRanges.cs` first; `AuditMarten` 2593-2599 stays with the #2847 plan |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | 16 | Options, validator, justification, namespace, two documents, service and interface, encryptor, field, two projections, log, meter, activity, registration, ranges |
| Tests | 9 | Unit, guard, contract, property, integration, justification `.md` files, architecture map |
| Docs and metadata | 5 | ADR, README, guide, changelog fragment, PublicAPI and manifest |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #2903: exceptional, justified, subject-scoped crypto-shredding of one person's audit records (path A of the #2847 decision). AGENTS.md rules apply (ROP, opt-in, fail closed, TimeProvider, no subject id in logs, per-flag coverage, CRAP <= 10).

IMPLEMENTATION OVERVIEW: PR 1: option, justification, documents, two-layer encryption, projection classification. PR 2: IAuditSubjectErasureService, registration, EventIds 7200-7224, metric and activity, tests, ADR, docs.

KEY PATTERNS: namespaced reuse of ISubjectKeyProvider; opaque SubjectKeyRef plus index and shred record; write-ahead awaited audit entry; typed failure when evidence is missing; Marten on PostgreSQL through Testcontainers; specialists for PublicAPI, changelog, manifests and docs.

REFERENCE FILES: src/Encina.Audit.Marten/AuditEventEncryptor.cs, src/Encina.Audit.Marten/Projections/OperationAuditEntryProjection.cs, src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs, src/Encina/Diagnostics/EventIdRanges.cs, .github/coverage-manifest/Encina.Audit.Marten.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Only a per-batch lookup cache inside the projection; no `ICacheProvider` use |
| 2 | OpenTelemetry | ✅ | Counter `encina.audit.shredded.subject.count` and activity `encina.audit.shred.subject` (Phase 5) |
| 3 | Structured Logging | ✅ | Range 7200-7224; codes only |
| 4 | Health Checks | ❌ | No new checkable dependency |
| 5 | Validation | ✅ | Justification and option validated before any write |
| 6 | Resilience | ❌ | Local database operations |
| 7 | Distributed Locks | ❌ | The shred is idempotent (key deletion and insert-once record), so concurrent identical requests converge without a lock |
| 8 | Transactions | ✅ | Shred record and index removal in one session save; audit entry written first |
| 9 | Idempotency | ✅ | A repeat with the same subject is a no-op |
| 10 | Multi-Tenancy | ✅ | Subject key ids are tenant-scoped (`audit:{tenant}:{subjectId}`) |
| 11 | Module Isolation | ❌ | Audit-specific flow |
| 12 | Audit Trail | ✅ | The shred is itself an audit event written ahead |

---

## Next Steps

1. Commit this plan, open its PR and link it from #2903.
2. When the issue is picked up, re-check the choices against today's code and ask the maintainer about Design Choices 1 and 3 (the Dilemmas).
3. Brief PR 1 then PR 2 after the #2847 recorder is merged (independent code, shared documentation).
