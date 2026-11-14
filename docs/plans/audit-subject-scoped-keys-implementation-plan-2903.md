# Implementation Plan: Subject-scoped audit keys for exceptional, justified erasure of one person's audit records

> **Issue**: [#2903](https://github.com/dlrivada/Encina/issues/2903)
> **Type**: Feature
> **Complexity**: High (7 phases, 1 provider: Marten, 53 files, split in 4 PRs)
> **Estimated Scope**: ~1,200 lines of production code + ~1,900 lines of tests

---

## Summary

Audit PII fields are encrypted with per-period temporal keys (`AuditEventEncryptor`, `ITemporalKeyProvider`). Destroying a period key shreds every subject of that period, so a court order to delete one person's audit records cannot be met without collateral loss. The maintainer decision of 2026-10-26 on #2847 makes that capability **path A**: an explicit opt-in, last-resort route, never the default (default is path C, planned in the #2847 plan: record the request, keep the data until retention). This plan adds subject-scoped keys to `Encina.Audit.Marten`:

- An opt-in flag, `MartenOperationAuditOptions.EnableSubjectScopedErasure` (default `false`). When on, the PII fields of entries whose actor is known are encrypted in two layers: inner with a per-subject key, outer with the period key (the retention shred still works). Entries written before enabling carry no subject layer and cannot be subject-shredded (events are immutable); the documentation says so.
- `IAuditSubjectErasureService.ShredSubjectAsync(subjectId, justification, ct)`: fails closed (`Left`) when the option is off, the tenant is missing under tenancy, or the justification (legal basis or order reference, actor, date) is invalid; writes a write-ahead audit entry of the shred; stores a permanent shred record, deletes the subject key, removes the index; entries of that subject then project as `[SHREDDED]` carrying the justification reference; other subjects stay readable. The sequence is NOT atomic: it is ordered so that every interruption converges when the call is repeated (Design Choice 4).
- Distinct from `[EXPIRED]` (retention) and from the typed failure of an unrecorded destruction (#2770).

- **Standards**: GDPR Art. 17 (exceptional erasure order), Art. 5(2) (accountability: the shred is justified and audited).
- **Affected packages**: `Encina.Audit.Marten` (the change), `Encina.Marten.GDPR` (reuses `ISubjectKeyProvider`, no change), `Encina` core (`EventIdRanges` registration).
- **Provider category**: event sourcing, Marten on PostgreSQL only; no database-provider matrix.
- **Scope and split**: counted with tests, manifests, PublicAPI files and changelog fragments the work is 53 files, so it ships as four pull requests that each build every test project and pass their own tests (the table under Estimated File Count lists the files of each): PR 1 = option, namespace, documents, EventId range and log class (15 files); PR 2 = index store, two-layer write, projection classification (15 files); PR 3 = erasure service and its observability (15 files); PR 4 = end-to-end tests, ADR and documentation (8 files).
- **Prerequisites**: the recorder and legal-basis types of the #2847 plan are independent of this plan; `ErasureReference`/`RetentionReference` and the marker already exist (PR 2a of #2770). Shared pieces: the marker semantics (`[SHREDDED]` only with a recorded reference) are owned by #2770. This plan uses its own EventId range (see Event ID Allocation); the `AuditMarten` ids 2593-2598 belong to the #3049 plan and the #2847 plan has its own range.

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
<summary><strong>2. Isolation from the application's own subject keys, and the namespaced subject id</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `ISubjectKeyProvider` with a reserved, injective subject-id namespace** | The issue asks to reuse it; every key store (PostgreSQL, in-memory) works; no new interface | The namespace convention must be enforced by one factory |
| **B) A second, audit-only provider interface and store** | Total separation | Duplicates the provider and its tests; two key stores to operate |
| **C) Share the application's subject key unchanged** | Zero code | Erasing the application subject (`CryptoShredErasureStrategy`) would also shred the audit trail, contradicting the default of path C |

### Chosen Option: **A — namespaced reuse**

### Rationale

- `CryptoShredErasureStrategy.EraseFieldAsync` deletes keys by the plain subject id, so the namespaced ids are untouched by ordinary erasure.
- **Injective encoding.** Joining caller-controlled parts with `:` collides (tenant `a` with subject `b:c`, and tenant `a:b` with subject `c`, would share one key, and shredding one would shred the other). `AuditSubjectKeyNamespace.Create(tenantKey, subjectId)` is the single internal factory and returns `audit:{tenantKey.Length}:{tenantKey}:{subjectId}`; the length prefix fixes the split whatever either part contains. `tenantKey` is the resolved tenant when tenancy is on (detected by `MultiTenancyMarker`) and the fixed `default` partition when tenancy is off; under tenancy the id `default` is reserved (compared case-insensitively) and refused, following the reserved-`default` rule of #122 and #1239, so no real tenant aliases the no-tenant partition (ADR-054 D4: tenancy off emits no tenant). The key store formats the id it receives as `subject:{subjectId}:v{version}` (`PostgreSqlSubjectKeyProvider.cs:624`), parsing the version from the end; the integration test in PR 2 creates keys for the colliding pairs above and asserts none reads another's key through the store. A property test asserts that distinct (tenant, subject) pairs never produce the same namespaced id.
- **Class:** Settled
- **Settled by:** the #2903 issue body, which asks to reuse `ISubjectKeyProvider` (option B does not reuse it) and the maintainer decision of 2026-10-26 on #2847 (an erasure request never destroys other audit data by default), which excludes option C

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
<summary><strong>4. Opt-in, fail closed, justification, tenant and the order of the shred</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Flag off by default; the service returns `Left` when off, when the tenant is missing under tenancy or when the justification is incomplete; the audit entry is written first; the steps are ordered to converge on retry** | Meets the decision; no silent shred; no state the projection reads as tampering | Operators must enable the flag before the entries are written |
| **B) Always available, justification optional** | Simple | Violates opt-in and fail closed |

### Chosen Option: **A — opt-in, fail closed, audited, retry-safe**

### Rationale

- The justification is a record `SubjectErasureJustification(Reference, Actor, DateOnly Date)` validated before any write; a missing piece returns `audit.marten.invalid_shred_justification`. `Reference` must match `[A-Za-z0-9._/-]{1,64}` (a case or order number, never a name), which also bounds the activity tag (Phase 5).
- **The shred is not atomic.** The key deletion lives in `PostgreSqlSubjectKeyProvider`, which opens its own locked session, so it is a separate commit from the Marten documents. The order is therefore: (1) validate flag, tenant and justification; (2) write-ahead audit entry through `IOperationAuditStore` (entity id = the opaque ref, never the subject id), skipped when `GetByEntityAsync` already finds one for this ref; if it fails, nothing is destroyed; (3) insert the `AuditSubjectShredRecord` (insert-once by ref) and save: the record alongside a live key changes nothing, because the projection tries the key first; (4) delete the subject key (`DeleteSubjectKeysAsync`; an already-forgotten subject is success); (5) delete the index document. The irreversible step (4) comes after the evidence (3).
- **Classification after any interruption** (the projection, Phase 3): key found, decrypt; key missing and a shred record for the ref exists, `[SHREDDED]` with the reference whether or not the index still exists; key missing and no record, the typed `EncryptedFieldDecryptionException` (tampering or a bug). A repeated `ShredSubjectAsync` finishes whichever of steps 3-5 are missing and writes no second audit entry. Failure-injection tests run a fault between every pair of steps, then repeat the call and assert convergence, and assert that the projection never reaches the typed failure for a legitimate interrupted shred.
- **Tenant source.** See Design Choice 6.
- Error categories (ADR-062), created with `EncinaErrors.Create(code, category, ...)`: `audit.marten.invalid_shred_justification` `Validation` (R3); option off `audit.marten.subject_erasure_disabled` `Forbidden` (R6); `audit.marten.subject_tenant_required` `InvalidRequest` (R3); key store and document faults `Internal` (R4). A `GetCategory` test covers each.

- **Class:** Settled
- **Settled by:** maintainer decision of 2026-10-26 on #2847 (path A is explicit opt-in, fail closed without a justification reference, logged as an audit event) at https://github.com/dlrivada/Encina/issues/2847, AGENTS.md section 3 and SPEC-002 DEC-006; option B fails the opt-in and fail-closed rules

</details>

<details>
<summary><strong>5. Evidence that makes a destroyed subject key lawful, and the opaque reference</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A random opaque `SubjectKeyRef` in each field, an index document `AuditSubjectKeyIndex` (namespaced id to ref) deleted at shred, and an `AuditSubjectShredRecord` (ref plus justification) kept forever** | The projection tells a lawful shred (`[SHREDDED]` plus reference) from tampering (typed failure); the record holds no subject identifier; the ref cannot be derived from the id | Two small documents and one lookup per distinct ref per projection batch |
| **B) Store the subject id in the field** | No index | The identifier would survive the shred inside the immutable event |
| **C) Reuse `SubjectForgottenMarker` of `Encina.Marten.GDPR`** | Existing type | It stores the plain subject id and no justification |

### Chosen Option: **A — random opaque reference, index, shred record**

### Rationale

- **The reference is random, never derived.** A ref derived from the id (a hash of the namespaced id) would let anyone holding the immutable events and a list of user ids confirm who was shredded by a dictionary attack, so the data would stay pseudonymous and linkable to a name, defeating the erasure. The ref is a random 128-bit value generated when the subject's index document is first created (`Guid.CreateVersion7` is NOT used because it embeds time; a cryptographic random `Guid`/16 bytes), stored only in the index document (namespaced id to ref) and in the fields. After the shred the index is deleted, so no stored data maps the ref to the person; the residual is that entries of one erased person share a ref and are linkable to each other (not to the person), which the documentation states.
- **One ref per subject under concurrency.** The index document id is the namespaced id and it is inserted insert-once; the loser of two concurrent first writes sees the conflict, reloads the winner's ref and uses it, and the fields are encrypted only after the index is established. The test runs two concurrent first writes and asserts one ref, and that two entries of one subject share it and that the ref differs between runs for the same subject.
- This mirrors the period design: marker with a reference means lawful (`[SHREDDED]`), no marker means the shard pauses with `EncryptedFieldDecryptionException` and an operator resolves it. The marker text is `MartenOperationAuditOptions.ShreddedPlaceholder` (`[SHREDDED]`), unchanged.

- **Class:** Settled
- **Settled by:** #2770 (a destruction without a recorded reference is not a lawful erasure), ADR-034 and decision D15 recorded in the #2903 body; options B and C keep the subject id or lack the justification, which these sources exclude

</details>

<details>
<summary><strong>6. Where the tenant of a shred comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ambient: the same tenant source the write path uses (`IRequestContext.TenantId` today, the tenant scope resolver of #1257 when that plan lands); `ShredSubjectAsync` has no tenant parameter** | A shred can only reach the tenant the operator is in; one source for write and shred, so the namespaced ids always match | A background job must establish the tenant context first |
| **B) Explicit `tenantId` parameter on `ShredSubjectAsync`** | Callable without ambient context | An operator in one tenant could name another; the parameter and the write-time source can disagree |

### Chosen Option: **A — ambient tenant (recommended, pending the maintainer)**

### Rationale

- Under tenancy (`MultiTenancyMarker`) with no tenant, or with the reserved `default`, the service returns `Left(audit.marten.subject_tenant_required)` before any write (fail closed). With tenancy off the fixed `default` partition is used.
- Option A wins when shreds are performed interactively by an operator inside a tenant; option B wins when a platform operator shreds across tenants from a job.

- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Option, justification, namespace and documents (PR 1)

<details>
<summary>Tasks</summary>

1. `MartenOperationAuditOptions.EnableSubjectScopedErasure` (default `false`, XML docs naming the limitation for earlier entries) and validator rule.
2. `SubjectErasure/AuditSubjectKeyNamespace.cs` (internal): the injective id factory of Design Choice 2, with the reserved-`default` refusal.
3. Documents `AuditSubjectKeyIndex` (id = namespaced id, field `Ref`) and `AuditSubjectShredRecord` (id = ref; justification reference, actor, date, `ShreddedAtUtc`; no subject id), registered in `ConfigureMartenOperationAuditProjections`.
4. EventId range `AuditMartenSubjectErasure` and the log class (Phase 5 describes the ids; they are created here so later PRs compile and log).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Audit.Marten; path A of the maintainer decision on #2847; issue #2903, Design Choices 2, 4, 5.
TASK: Add the option, the namespace factory, the two Marten documents, the range and the log class.
KEY RULES: opt-in default false; Either for validation outcomes; UTC suffix naming; the shred record and every log never contain the subject id; the namespaced id is length-prefixed; add the property test "distinct (tenant, subject) pairs never collide" and the reserved-default test; PublicAPI.Unshipped.txt lines (via mechanical-fixer).
REFERENCE FILES: src/Encina.Audit.Marten/MartenOperationAuditOptions.cs, src/Encina.Audit.Marten/Crypto/TemporalKeyDestroyedMarker.cs, src/Encina.Audit.Marten/Projections/ConfigureMartenOperationAuditProjections.cs, src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

### Phase 2: Index store and two-layer encryption on write (PR 2)

<details>
<summary>Tasks</summary>

1. `SubjectErasure/AuditSubjectKeyIndexStore.cs` (internal, Scoped): `GetOrCreateRefAsync(tenantKey, subjectId, ct)`: load the index by namespaced id; absent, generate a cryptographically random 128-bit ref, insert once (own lightweight session, committed before use); on a conflict reload the winner's ref.
2. `Events/EncryptedField.cs`: nullable `SubjectKeyRef`; `Encrypt` overload that wraps an already encrypted inner field.
3. `AuditEventEncryptor.EncryptAuditEntryAsync(entry, ct)` (line 79) and `EncryptReadAuditEntryAsync` (line 155): the signatures and the two callers (`MartenOperationAuditStore` line 125 and `MartenReadAuditStore` line 103) are unchanged. The public constructor `AuditEventEncryptor(ITemporalKeyProvider, IOptions<MartenOperationAuditOptions>, ILogger<AuditEventEncryptor>)` is also unchanged (its `PublicAPI.Unshipped.txt` line 6 stays) because it is built directly with `new` in 14 test files (`git grep -l "new AuditEventEncryptor(" -- src tests` on origin/main: the benchmark, `AuditEncryptionLoadTests`, `AuditEncryptionLoadTest`, `AuditEventEncryptorTests`, two `AuditEventEncryptorGuardTests`, `MartenOperationAuditStoreGuardTests`, `MartenReadAuditStoreGuardTests`, `AuditStoreQueryGuardTests`, the two store query tests, the purge and UTC-filter integration tests and the ABAC round-trip test) and a trailing optional parameter is impossible: the index store is `internal` and a public constructor cannot expose an internal type. The subject layer therefore enters through a new `internal` constructor that takes the three existing arguments plus `AuditSubjectKeyIndexStore?` and `ISubjectKeyProvider?` (the public one chains to it with `null, null`, which is the option-off path), and `AddEncinaAuditMarten` registers the encryptor with a factory (`services.AddScoped(sp => new AuditEventEncryptor(..., sp.GetService<AuditSubjectKeyIndexStore>(), sp.GetService<ISubjectKeyProvider>()))`, replacing `services.AddScoped<AuditEventEncryptor>()` at line 122) so DI always uses the internal constructor. When the option is on and either dependency is null (no `ISubjectKeyProvider` registered by `Encina.Marten.GDPR`), recording returns `Left` (fail closed) instead of writing a single-layer field. When the option is on and `entry.UserId` is present, get the ref from the index store, get/create the subject key through `ISubjectKeyProvider.GetOrCreateSubjectKeyAsync(namespaced id)`, encrypt each PII field with it, then with the period key, and set `SubjectKeyRef`. A `Left` from the index store or the key provider fails the recording (never writes a plaintext or single-layer field when the option is on). The encryptor relies on the per-subject locked session of `PostgreSqlSubjectKeyProvider` for key creation and on the insert-once index for the ref.
4. `Projections/OperationAuditEntryProjection.cs` and `ReadAuditEntryProjection.cs`: classification of Design Choice 4 (key found, decrypt; key missing plus shred record, `ShreddedPlaceholder`; neither, throw the typed `EncryptedFieldDecryptionException`); the period classification (`ClassifyTemporalKeyLookup`) stays unchanged and is evaluated first, so an expired period still reads `[EXPIRED]`; shred records cached per batch only.
5. Registration of the index store in `AddEncinaAuditMarten` (`TryAdd`) and the factory registration of the encryptor (task 3); DI test with `ValidateOnBuild` and `ValidateScopes` in `tests/Encina.UnitTests/AuditMarten/ServiceCollectionExtensionsTests.cs` (modified; resolves the encryptor with the option on and off and with and without an `ISubjectKeyProvider`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: AuditEventEncryptor encrypts six operation fields and three read fields with the period key (AuditEventEncryptor.cs lines 79 and 155). OperationAuditEntryProjection classifies the period key via ClassifyTemporalKeyLookup (line 228) and decrypts fields (line 301). Phase 1 types exist. Marten's async daemon pauses a shard on a thrown typed exception.
TASK: Implement the index store, the inner subject layer with SubjectKeyRef when EnableSubjectScopedErasure is true, and the projection classification; keep the option-off path identical.
KEY RULES: the public AuditEventEncryptor constructor is unchanged (14 test files build it with new); the subject layer enters through an internal constructor and a factory registration; fail closed; key material never logged; async with CancellationToken; ROP; the subject id only travels to ISubjectKeyProvider and the namespace factory; the ref is random, never derived; a missing key with no shred record is never silently shown as shredded; per-batch cache only; tests: two concurrent first writes give one ref, two entries of one subject share it, the colliding (tenant, subject) pairs read each other's keys never.
REFERENCE FILES: src/Encina.Audit.Marten/AuditEventEncryptor.cs, src/Encina.Audit.Marten/Events/EncryptedField.cs, src/Encina.Audit.Marten/Projections/OperationAuditEntryProjection.cs, src/Encina.Audit.Marten/Projections/ReadAuditEntryProjection.cs, src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs
```

</details>

### Phase 3: Erasure service (PR 3)

<details>
<summary>Tasks</summary>

1. `SubjectErasure/SubjectErasureJustification.cs` (record: `Reference`, `Actor`, `Date`; `IsValid` with the reference format of Design Choice 4), `IAuditSubjectErasureService.cs` and internal `MartenAuditSubjectErasureService` (Scoped) implementing the five ordered steps of Design Choice 4, idempotent, finishing missing steps on a repeat.
2. Registration in `AddEncinaAuditMarten` always (`TryAdd`): the service returns `Left(audit.marten.subject_erasure_disabled)` when the option is off, because the option value is only known after configuration runs and a registration-time read of a callback-configured option is fragile. DI test in both registration orders with `ValidateOnBuild` and `ValidateScopes`.
3. Error codes and categories in `MartenAuditErrors` (Design Choice 4) with `GetCategory` tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1-2 are done. The service is the only public way to shred one subject's audit records.
TASK: Implement IAuditSubjectErasureService, the justification record and the registration.
KEY RULES: fail closed with Left; time from TimeProvider; the order is audit entry (skipped when one exists for this ref), shred record, key deletion, index removal; NOT atomic, every interruption converges when repeated; if the audit entry write fails nothing is destroyed; tenant from the same source as the write path, Left(subject_tenant_required) under tenancy without one; never log the subject id, the justification text or EncinaError.Message; add failure-injection tests between every pair of steps and two concurrent shred requests.
REFERENCE FILES: src/Encina.Audit.Marten/MartenOperationAuditStore.cs, src/Encina.Audit.Marten/ServiceCollectionExtensions.cs, src/Encina.Marten.GDPR/KeyStore/PostgreSqlSubjectKeyProvider.cs
```

</details>

### Phase 4: Observability (log class in PR 1, counter and activity in PR 3)

<details>
<summary>Tasks</summary>

1. New range `AuditMartenSubjectErasure = (7200, 7224)` in `src/Encina/Diagnostics/EventIdRanges.cs` (inside the es-integrity-crypto block 7200-7249 of the batch registry, `eventid-blocks.md`; registered first, ADR-021, the registry row `7200-7249 | 7200-7207 used | #2903 AuditMartenSubjectErasure` records it; re-read it and confirm with the orchestrator that the block is this plan's before registering); add it to the `Encina.Audit.Marten` entry of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the assembly then maps to its several ranges); add the field to the `PublicAPI.Unshipped.txt` of `Encina`.
2. `Diagnostics/MartenAuditSubjectErasureLog.cs` (source-generated, exactly the ids used, packed 7200-7207): 7200 shred requested, 7201 shred rejected (code), 7202 shred completed, 7203 shred repeated, 7204 key deletion failed, 7205 projection saw a shredded subject field, 7206 projection found a missing record, 7207 tenant required.
3. Metric `encina.audit.shredded.subject.count` (`Counter<long>`, tag `outcome`) and activity `encina.audit.shred.subject` with tag `audit.justification_ref` (the validated reference format of Design Choice 4, bounded to 64 characters of `[A-Za-z0-9._/-]`, never the subject id), as in the issue; both add `encina.tenant_id` only when multi-tenancy is on (ADR-054 D4).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: AuditMarten (2550-2599) is almost full and #3049 and #2847 use other ranges, so this feature registers the range 7200-7224 from the family block 7200-7249.
TASK: Register the range, write the log class, the counter and the activity; update the architecture test map.
KEY RULES: register the range before using ids; sequential ids with no gaps and exactly the ones used; XML docs naming the range; low-cardinality tags; encina.tenant_id only when tenancy is on; no subject id anywhere.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditMeter.cs
```

</details>

### Phase 5: Testing (spread over the PRs)

<details>
<summary>Tasks</summary>

1. Unit: justification validation, namespace builder, encryptor two layers and option-off identity, projection classification (key present, shredded with and without index, missing evidence), service ordering and failure injection between every pair of steps, `GetCategory` of each code.
2. Guard: every new public method and constructor (`ArgumentNullException`).
3. Contract: `IAuditSubjectErasureService` contract (success on repeat, `Left` when off or invalid) with the real service.
4. Property (FsCheck): distinct (tenant, subject) pairs never collide (PR 1); shredding subject A never changes the decryptability of subject B or of A's entries after period expiry order changes (PR 4).
5. Integration (Marten on PostgreSQL via Testcontainers, `MartenCollection`, no in-memory store): end to end with two subjects, period expiry still yields `[EXPIRED]`, shred yields `[SHREDDED]`, entries written before enabling stay readable and cannot be shredded, missing evidence pauses the shard, two concurrent first writes, two concurrent shreds, an interrupted shred never pauses the shard.
6. Load and benchmark: `.md` justifications (rare, exceptional path); a note that the read path adds one key lookup per distinct ref.
7. Per-file coverage targets, written to `Encina.Audit.Marten.json` via `mechanical-fixer` (the worker measures the baseline with the AGENTS.md section 9 commands; the numbers are the floor; `Encina.Marten.GDPR.json` is unchanged):

| File (PR) | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|-----------|------|-------|----------|----------|-------------|----------------------------|
| `AuditSubjectKeyNamespace.cs` (1) | 100 | 100 | 0 | 95 | 0 | One pure function: unit cases for the reserved `default` and empty parts, a property test for non-collision. |
| `MartenOperationAuditOptions.cs` and validator (1) | 95 | 0 | 0 | 0 | 0 | One property and one rule, unit-tested for both values. |
| `AuditSubjectKeyIndexStore.cs` (2) | 85 | 60 | 0 | 0 | 85 | Insert-once and conflict-reload branches are unit-tested with a substituted session; PostgreSQL proves the concurrent first writes. |
| `AuditEventEncryptor.cs` (2) | 85 | 50 | 0 | 0 | 75 | The new subject-layer branch and the option-off identity are unit-tested; integration reaches both layers on a real store. |
| `Events/EncryptedField.cs` (2) | 90 | 0 | 0 | 0 | 70 | The new field and overload are asserted by round-trip unit tests. |
| `OperationAuditEntryProjection.cs`, `ReadAuditEntryProjection.cs` (2) | 85 | 0 | 0 | 0 | 80 | The three classifications (decrypt, shredded, typed failure) are unit-tested; integration drives them through the daemon. |
| `MartenAuditSubjectErasureService.cs` (3) | 90 | 70 | 90 | 0 | 85 | Every branch (off, tenant, justification, each step, repeat) has a unit case with failure injection; PostgreSQL covers the end to end. |
| `SubjectErasureJustification.cs`, `ServiceCollectionExtensions.cs` (3) | 95 | 65 | 0 | 0 | 0 | A validator and a registration, covered by unit and DI tests. |
| `Diagnostics/MartenAuditSubjectErasureLog.cs`, `MartenAuditMeter.cs`, `MartenAuditActivitySource.cs` (3) | 80 | 0 | 0 | 0 | 60 | Counter, activity and log calls run on every service outcome; unit tests assert the tags and the absent tenant tag with tenancy off. |

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Per-flag coverage (AGENTS.md section 9); manifest .github/coverage-manifest/Encina.Audit.Marten.json (and Encina.Marten.GDPR.json is unchanged).
TASK: Write the tests of the PR you were briefed on; add per-file targets with justifications for every touched and new src file through mechanical-fixer; measure each flag against the Phase 5 table.
KEY RULES: real implementations instantiated; Shouldly through Encina.Testing.Shouldly; shared Marten collection fixture and ClearAllDataAsync; compliance modules use Marten on PostgreSQL; CRAP <= 10 on changed methods; the PR must build and pass its own tests and stay at 15 files or fewer.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/AuditProjectionUnreadableEventIntegrationTests.cs, tests/Encina.UnitTests/AuditMarten/InMemoryTemporalKeyProviderTests.cs, tests/Encina.GuardTests/AuditMarten/MartenTemporalKeyProviderGuardTests.cs
```

</details>

### Phase 6: Documentation and finalization (PR 4)

<details>
<summary>Tasks</summary>

1. ADR for the key hierarchy: ADR number assigned when the ADR pull request opens (via `docs-writer`).
2. `Encina.Audit.Marten` README and a configuration guide: enabling the option, the limitation for earlier entries, the residual linkability of one person's entries by the shared ref, the exceptional-use procedure and the justification format (via `docs-writer`).
3. Changelog fragments `changelog.d/2903-audit-subject-scoped-keys.added.md` (PR 2, the first PR with user-visible behavior) and PublicAPI lines (every PR) via `mechanical-fixer`.
4. `dotnet build --configuration Release` with 0 warnings; every flag reaches its manifest target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1-5 are implemented.
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
| ADR-054 D4 | Tenant attribute and tag only when multi-tenancy is on |
| ADR-058 D15-D17 | Write-ahead, awaited audit of the shred |
| ADR-062 | Error categories of the new codes |
| SPEC-002 DEC-006 | Compliance gates fail closed |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ISubjectKeyProvider` | `src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs` | Per-subject keys (`GetOrCreateSubjectKeyAsync`, `GetSubjectKeyAsync`, `DeleteSubjectKeysAsync`) |
| `AuditEventEncryptor` | `src/Encina.Audit.Marten/AuditEventEncryptor.cs` | Gains the inner layer |
| `EncryptedField` | `src/Encina.Audit.Marten/Events/EncryptedField.cs` | Gains `SubjectKeyRef` |
| Projections | `src/Encina.Audit.Marten/Projections/` | Classify `[SHREDDED]` / failure |
| `TemporalKeyDestroyedMarker`, `ErasureReference` | `src/Encina.Audit.Marten/Crypto/` | Pattern for evidence and reference validation |
| `MartenOperationAuditStore` | `src/Encina.Audit.Marten/MartenOperationAuditStore.cs` | Write-ahead audit entry and `GetByEntityAsync` for the retry check |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Detects tenancy-on for the fail-closed tenant gate |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Audit.Marten (subject erasure) | 7200-7224 (new range `AuditMartenSubjectErasure`, ids 7200-7207 used) | From the es-integrity-crypto block 7200-7249 of the batch registry; registered in `EventIdRanges.cs` first; `AuditMarten` 2593-2598 belongs to #3049 and #2847 has its own range, so no id of `AuditMarten` is used |

### Estimated File Count

| PR | Category | Files | Notes |
|----|----------|-------|-------|
| 1 | Production | 8 | `MartenOperationAuditOptions.cs`, its validator, `AuditSubjectKeyNamespace.cs`, `AuditSubjectKeyIndex.cs`, `AuditSubjectShredRecord.cs`, `ConfigureMartenOperationAuditProjections.cs`, `EventIdRanges.cs`, `MartenAuditSubjectErasureLog.cs` |
| 1 | Tests | 4 | Namespace unit, namespace property, options/validator unit (modified), `EncinaEventIdAllocationTests.cs` map |
| 1 | Metadata | 3 | `PublicAPI.Unshipped.txt` of `Audit.Marten` and of `Encina`, manifest |
| 1 | **Total** | **15** | Builds alone: the types are registered and unused until PR 2, and the log ids 7200-7207 stay registered and unused until PRs 2 and 3 |
| 2 | Production | 6 | `AuditSubjectKeyIndexStore.cs` (new), `EncryptedField.cs`, `AuditEventEncryptor.cs`, the two projections, `ServiceCollectionExtensions.cs` |
| 2 | Tests | 6 | Index store integration, encryptor unit, projection unit, concurrent-first-write integration, guard, DI test (`ServiceCollectionExtensionsTests.cs`, modified); the 14 test files that build `AuditEventEncryptor` with `new` are unchanged because its public constructor is unchanged |
| 2 | Metadata | 3 | `PublicAPI.Unshipped.txt` (`EncryptedField.SubjectKeyRef`; the internal constructor needs no line), manifest, fragment `2903-audit-subject-scoped-keys.added.md` |
| 2 | **Total** | **15** | Needs PR 1; no `ProjectReference` change (`Encina.Audit.Marten` already references `Encina.Marten.GDPR`, which holds `ISubjectKeyProvider`) |
| 3 | Production | 7 | `IAuditSubjectErasureService.cs`, `SubjectErasureJustification.cs`, `MartenAuditSubjectErasureService.cs`, `ServiceCollectionExtensions.cs`, `MartenAuditMeter.cs`, `MartenAuditActivitySource.cs`, `MartenAuditErrors.cs` |
| 3 | Tests | 6 | Service unit with failure injection, failure-injection integration, contract, guard, DI test (modified), observability unit |
| 3 | Metadata | 2 | `PublicAPI.Unshipped.txt`, manifest |
| 3 | **Total** | **15** | Needs PR 2 |
| 4 | Tests | 4 | Property (isolation), end-to-end integration, load and benchmark `.md` justifications |
| 4 | Docs and metadata | 4 | ADR, README, configuration guide, manifest |
| 4 | **Total** | **8** | Needs PR 3 |
| | **Grand total** | **53** | 15 + 15 + 15 + 8; every PR is at most 15 files and under 1,500 lines |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #2903: exceptional, justified, subject-scoped crypto-shredding of one person's audit records (path A of the #2847 decision). AGENTS.md rules apply (ROP, opt-in, fail closed, TimeProvider, no subject id in logs, per-flag coverage, CRAP <= 10).

IMPLEMENTATION OVERVIEW: PR 1: option, injective namespace, documents, EventId range 7200-7224 and log class. PR 2: index store, two-layer encryption, projection classification. PR 3: IAuditSubjectErasureService, registration, metric and activity. PR 4: end-to-end tests, ADR, docs.

KEY PATTERNS: namespaced reuse of ISubjectKeyProvider with a length-prefixed id; random opaque SubjectKeyRef plus insert-once index and shred record; write-ahead awaited audit entry then record, key deletion, index removal (not atomic, converges on retry); typed failure when evidence is missing; error categories per ADR-062; Marten on PostgreSQL through Testcontainers; specialists for PublicAPI, changelog, manifests and docs.

REFERENCE FILES: src/Encina.Audit.Marten/AuditEventEncryptor.cs, src/Encina.Audit.Marten/Projections/OperationAuditEntryProjection.cs, src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs, src/Encina/Diagnostics/EventIdRanges.cs, .github/coverage-manifest/Encina.Audit.Marten.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Only a per-batch lookup cache inside the projection; no `ICacheProvider` use |
| 2 | OpenTelemetry | ✅ | Counter `encina.audit.shredded.subject.count` and activity `encina.audit.shred.subject` (Phase 4), tenant tag only when tenancy is on (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Range 7200-7224, ids 7200-7207; codes only |
| 4 | Health Checks | ❌ | No new checkable dependency |
| 5 | Validation | ✅ | Justification, tenant and option validated before any write |
| 6 | Resilience | ❌ | Local database operations |
| 7 | Distributed Locks | ❌ | Not applicable as a new lock: key creation is serialized per subject by the locked session of `PostgreSqlSubjectKeyProvider`, the index and shred record are insert-once, and the shred steps are idempotent; the concurrent first-write and concurrent shred tests prove it |
| 8 | Transactions | ❌ | Not atomic by design: the key deletion is a separate commit from the Marten documents; the steps are ordered (audit entry, shred record, key deletion, index removal) so every interruption converges when repeated |
| 9 | Idempotency | ✅ | A repeat with the same subject finishes the missing steps and writes no second audit entry |
| 10 | Multi-Tenancy | ✅ | Injective tenant-qualified subject key ids, tenant required under `MultiTenancyMarker`, reserved `default` refused (#122, #1239 rule), tenant tag per ADR-054 D4 |
| 11 | Module Isolation | ❌ | Audit-specific flow |
| 12 | Audit Trail | ✅ | The shred is itself an audit event written ahead |

---

## Next Steps

1. Commit this plan, open its PR and link it from #2903.
2. When the issue is picked up, re-check the choices against today's code and ask the maintainer about Design Choices 1, 3 and 6 (the Dilemmas).
3. Brief PR 1, then PR 2, PR 3 and PR 4 in order, after the #2847 recorder is merged (independent code, shared documentation).
