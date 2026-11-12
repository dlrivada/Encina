# Implementation Plan: Erasure requests reach the audit trail with a recorded erasure reference

> **Issue**: [#2847](https://github.com/dlrivada/Encina/issues/2847)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 2 packages plus DSR, 51 files, split in 4 PRs)
> **Estimated Scope**: ~700 lines of production code + ~1,300 lines of tests

---

## Summary

The audit trail keeps an erasure request invisible: the data subject rights executor (`DefaultDataErasureExecutor`) never reaches `Encina.Audit.Marten`, and no erasure flow calls `ITemporalKeyProvider.DestroyKeysBeforeAsync`. The maintainer decision of 2026-10-26 on #2847 settles the scope: **path C is the default**. Audit data is excluded from erasure by GDPR Art. 17(3)(b) (legal obligation) and 17(3)(e) (legal claims); an erasure request that reaches the audit trail is **recorded** (erasure reference: `ErasureRequestId`, date, actor, plus the legal basis of the retention) and the data stays until its retention expires, when the period key is destroyed with a `RetentionReference` and fields read `[EXPIRED]`. No audit key is destroyed by an erasure request on this path and no other subject's audit data is ever affected. Subject-scoped destruction (path A, the `[SHREDDED]` exceptional route) is the separate issue #2903.

What already exists (PR 2a of #2770): `ErasureReference`, `KeyDestructionReference`, `RetentionReference`, the `reference` parameter of `DestroyKeysBeforeAsync` and the marker fields (`ErasureRequestId`, `ErasureRequestedAtUtc`, `ErasureActor`) on `TemporalKeyDestroyedMarker`. What is missing, and this plan adds:

1. A DSR-side context and recorder abstraction (`ErasureRequestContext`, `IErasureRequestRecorder`) and the executor call that notifies recorders for each erasure request, **before** any destructive step (write-ahead).
2. An audit-side recorder, `MartenAuditErasureRequestRecorder`, writing a tenant-qualified `AuditErasureRequestRecord` document (no subject identifier) idempotently, with the legal basis and the write-ahead audit entry of the recording itself.

- **Affected packages**: `Encina.Compliance.DataSubjectRights`, `Encina.Audit.Marten` (the issue also names `Encina.Marten.GDPR`; it needs no change because the audit recorder is registered by `Encina.Audit.Marten`, which already references it), core `Encina` (`EventIdRanges.cs`, one new range).
- **Provider category**: event sourcing, Marten on PostgreSQL only; the 10-database matrix does not apply.
- **Scope and split**: counted with tests, manifests, PublicAPI files and changelog fragments the work is 51 files, so it ships as four pull requests that each build and pass their own tests (the table under Estimated File Count lists the files of each): PR 1 = DSR (15 files), PR 2 = audit document, key and options (13 files), PR 3 = recorder and registration (9 files), PR 4 = observability and documentation (14 files).
- **Prerequisites**: PR 2a of #2770 is merged (the types above exist). #2903 builds on the same marker and recorder and is planned in its own file; the two plans share no EventId range (see Event ID Allocation).

---

## Design Choices

<details>
<summary><strong>1. Audit scope of an erasure request: record against the audit trail (path C), no key destruction</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Subject-scoped destruction** | Precise Art. 17 for audit | Needs a new key model and schema; separate issue #2903 |
| **B) Period-scoped destruction** | Works with today's keys | Destroys other subjects' audit data |
| **C) Record the request and the legal basis, keep data until retention** | Lawful by default (Art. 17(3)(b),(e)); no collateral loss | The subject's audit records remain until retention |

### Chosen Option: **C — record, keep until retention**

### Rationale

- The retention path already destroys keys with a `RetentionReference` (`MartenOperationAuditStore.cs` purge, `RetentionReference.ForPurge`), so expiry needs no new code.
- Path A is an explicit opt-in last resort in #2903; this plan changes nothing about `DestroyKeysBeforeAsync`.

- **Class:** Settled
- **Settled by:** maintainer decision of 2026-10-26 on #2847 (destruction scope: C by default plus A before 1.0 as an exceptional path) at https://github.com/dlrivada/Encina/issues/2847 and GDPR Art. 17(3)(b) and (e)

</details>

<details>
<summary><strong>2. How the erasure request context (request id, date, actor, tenant) reaches the executor</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `ErasureRequestContext(RequestId, RequestedAtUtc, Actor, TenantId)` record, a required parameter of `IDataErasureExecutor.EraseAsync`; `ErasureRequest` gains required `RequestId` and `Actor`** | Explicit, testable, no hidden state; pre-1.0 so the signature is changed completely | Touches the interface, the executor, `DefaultDSRService.HandleErasureAsync` and their tests |
| **B) Carry it inside `ErasureScope`** | No interface change | `ErasureScope` says what to erase, not who asked; mixing concerns |
| **C) Ambient (`AsyncLocal`) context** | No signature change | Hidden; breaks in background processing; harder to test |

### Chosen Option: **A — explicit context parameter (recommended, pending the maintainer)**

### Rationale

- Today `HandleErasureAsync(ErasureRequest)` (`DefaultDSRService.cs:622`) has only `SubjectId`, `Reason` and `Scope`; there is no id or actor anywhere on the erasure path, so some carrier is needed.
- Option A wins when erasure is driven from several entry points (service, background job); option B wins only if the maintainer wants no interface change at all.
- `ErasureReference` lives in `Encina.Audit.Marten.Crypto` and DSR cannot reference it (dependency direction is Audit.Marten to Marten.GDPR to DSR), hence a DSR-level context that the audit recorder maps to an `ErasureReference`.
- **Who fills the fields (the contract).** `RequestId` and `Actor` are required, non-blank parameters of the `ErasureRequest` record, with no default and no generation inside the service: the caller owns the identity of its request (a retry must repeat the same `RequestId` to be idempotent) and names the operator. `HandleErasureAsync` validates them first and returns `Left(dsr.erasure_request_incomplete)` (category `InvalidRequest`, ADR-062 R3: a missing id or operator) before anything runs; the check does not depend on which recorders are registered, so adding `Encina.Audit.Marten` later never turns working callers into failing ones. `TenantId` is read by `DefaultDSRService` from the request's tenant source (the core `IRequestContext.TenantId` today; the resolver of #1257 when that plan lands) and is null when there is none; the audit recorder is the fail-closed gate (Design Choice 4).
- **Callers to change** (all compile-time visible): `src/Encina.Compliance.DataSubjectRights/README.md` (the example `new ErasureRequest("user-123", reason, scope: null)`), `tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs` (2 constructions), `ModelRecordTests.cs` (1), `DataSubjectRightsDiagnosticsErrorMessageLeakTests.cs` (2), `DataSubjectRightsDiagnosticsSubjectIdLeakTests.cs` (1), and the executor tests. `PublicAPI.Unshipped.txt` lines to change: `ErasureRequest..ctor` (line 94), `IDataErasureExecutor.EraseAsync` (line 137), `DefaultDataErasureExecutor.EraseAsync` (line 314), plus the new members.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Which abstraction notifies the audit trail: a recorder called by the executor</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IErasureRequestRecorder` (DSR), invoked by the executor once per request for every registered recorder, before the first destructive step** | Generic; pay-for-what-you-use (none registered = no-op); no audit dependency in DSR; write-ahead | New public interface |
| **B) A locator returning audit locations flagged `HasLegalRetention` and an `IDataErasureStrategy`** | Reuses existing abstractions | Executor never calls the strategy for retained fields, so nothing is recorded; locating a subject's audit entries means decrypting every user id |
| **C) Call `Encina.Audit.Marten` directly from DSR** | Smallest | Inverts the package dependency |

### Chosen Option: **A — `IErasureRequestRecorder`, called first**

### Rationale

- Recording is unconditional per request (no subject lookup, so no subject identifier is read, stored or logged), which keeps the record accurate: the audit trail is always retained under Art. 17(3).
- **Order (write-ahead, ADR-058 D15-D17).** `DefaultDataErasureExecutor.EraseAsync` calls every recorder BEFORE the strategy loop (and before the empty-locations early return). A recorder `Left` is returned at once and no strategy runs: nothing is deleted and nothing is unrecorded. If a strategy later fails, the record of the request already exists; the retry repeats the same `RequestId`, finds the record and writes nothing new. The unit test "a recorder `Left` leaves every strategy uncalled" pins the order.
- A recorder failure fails the erasure (returns the `Left`; the existing `ErasureFailed(code)` log is used, so DSR needs no new EventId; `ComplianceDSR` 8300-8349 is exhausted at 8349).
- **No message re-wrapping.** `HandleErasureAsync` today wraps every `Left` of the executor with `DSRErrors.ErasureFailed(subjectDescription, error.Message)`. With a recorder error that would copy another package's message into the returned error. The wrapper is changed to carry only the inner error's code (`error.GetCode()`) in the message and details, never `error.Message`; a unit test asserts that a recorder error with a marker message does not appear in the returned error or in any log.

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (errors never swallowed in background infrastructure; compliance gates fail closed) and the module-dependency direction in `src/Encina.Audit.Marten/Encina.Audit.Marten.csproj`; options B and C fail the cited rules (B records nothing for retained fields, C inverts the dependency)

</details>

<details>
<summary><strong>4. Storage, tenant qualification and idempotency of the record</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marten document `AuditErasureRequestRecord` with a tenant-qualified id; insert once, a repeat is a no-op** | Same pattern as `TemporalKeyDestroyedMarker` (`temporal-destroyed:{period}`); idempotent by id; queryable by tenant and date | One more document type to register |
| **B) Append an audit event keyed by the request id to the audit stream** | Reuses the event store and its immutability | Events are encrypted per period; a request is not tied to one period; replay semantics unclear; idempotency needs a read of the stream first |
| **C) Extend `TemporalKeyDestroyedMarker`** | No new type | A marker exists only after a destruction; path C destroys nothing |

### Chosen Option: **A — keyed document, insert-once (recommended, pending the maintainer)**

### Rationale

- Options A and B can both be idempotent; the choice is the storage model. Option B wins if the maintainer wants every fact in the append-only stream and accepts a stream-read before each write; option A wins on a direct keyed lookup and a simple retention story.
- **Tenant-qualified id (injective across tenants).** `RequestId` is caller-supplied, so the id is built by one factory, `AuditErasureRequestKey.Create(tenantKey, requestId)`: `audit-erasure-request:{tenantKey.Length}:{tenantKey}:{requestId}`. The length prefix makes the split unambiguous whatever characters either part holds (`a` + `b:c` and `a:b` + `c` give different ids), so tenant B can never be read as a duplicate of tenant A's request. `tenantKey` is the resolved tenant when tenancy is on; the fixed `default` partition when tenancy is off; under tenancy the id `default` is reserved (compared case-insensitively) and refused, following the reserved-`default` rule of #122 and #1239, so no real tenant aliases the no-tenant partition. A property test generates random (tenant, request id) pairs and asserts distinct pairs never produce the same id.
- **Fail closed on tenancy.** The recorder detects tenancy-on by `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`). Tenancy on with no tenant (or the reserved `default`) returns `Left(audit.marten.erasure_tenant_required)` before anything is written and the erasure does not start (Design Choice 3 order). The two-tenant same-`RequestId` test and the tenancy-on-without-tenant test are in Phase 6. `TenantId` on the document is the resolved tenant, null when tenancy is off.
- **Two commits, in a safe order, retry-safe.** The audit entry goes through `IOperationAuditStore.RecordAsync` (one Marten save) and the document is inserted in the recorder's session (a second save), so the operation is NOT atomic. Order: (1) load the document by id; present means both steps completed earlier, return success and write nothing; (2) absent: look for an existing entry of this request with `IOperationAuditStore.GetByEntityAsync("AuditTrail", <tenant-qualified id>)`; write the write-ahead entry only when none exists (ADR-058: the entry precedes the effect); (3) insert the document; a concurrent duplicate insert is treated as success. A crash after step 2 leaves an entry and no record; the retry skips the entry and inserts the record. A failed insert after a successful entry returns `Left(audit.marten.erasure_record_failed)` and the entry stays (an append-only entry of an attempted recording is legitimate evidence). Two concurrent first writes can both write an entry before one wins the insert; that duplicate entry is accepted and documented (the record stays single). The failure-injection test (entry written, insert fails, retry converges to one entry and one record) is in Phase 6.
- The record holds `RequestId`, `RequestedAtUtc`, `Actor`, `LegalBasis`, `RecordedAtUtc` (from `TimeProvider`) and `TenantId`; never a subject identifier. `Actor` is the operator's identifier, personal data of the operator kept for the retention period; the README task says so.
- Idempotency of a repeated request with the same reference is the issue's acceptance criterion.

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Shape of the legal basis recorded with the request</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Enum `AuditRetentionLegalBasis { LegalObligation, LegalClaims }` (Art. 17(3)(b), (e)), set on `MartenOperationAuditOptions`** | Closed set matches the decision; auditable; reportable by category | Another option property |
| **B) Free-text string** | Flexible | Unvalidated; cannot be reported by category |
| **C) Reuse `DSR.ErasureExemption`** | Existing enum | Its members (FreedomOfExpression, PublicHealth, Archiving) are not valid grounds for the audit trail; couples audit to DSR |

### Chosen Option: **A — closed enum on the options**

### Rationale

- `MartenOperationAuditOptionsValidator` rejects undefined values.

- **Class:** Settled
- **Settled by:** maintainer decision of 2026-10-26 on #2847 ("the legal basis of the retention") and GDPR Art. 17(3)(b) and (e), which close the set to the two grounds that apply to an audit trail; B is unvalidated and C is not valid for audit

</details>

<details>
<summary><strong>6. What the recorder contract carries: intent only, or intent plus the result</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RecordAsync(ErasureRequestContext, CancellationToken)`, one call before the strategies; the record says "request received"** | Simplest; write-ahead is exact; the record holds no result so it needs none | The audit trail does not show how many fields were erased |
| **B) A second best-effort `CompleteAsync(context, ErasureResult, CancellationToken)` after the strategies** | The trail also shows the outcome | A second write that can fail after the destruction; needs a status on the document and a "best-effort" rule that conflicts with never swallowing errors |

### Chosen Option: **A — intent only (recommended, pending the maintainer)**

### Rationale

- Option A wins while the audit need is "a request reached the trail and the data is retained under Art. 17(3)"; option B wins if an auditor must see the erased-field count next to the request. The `ErasureResult` is already returned to the caller and logged by the DSR service.

- **Class:** Dilemma

</details>

<details>
<summary><strong>7. A repeated request id with different content</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) First record wins; the repeat returns success, writes nothing and counts as `duplicate`** | A retry (which carries a new `RequestedAtUtc`) converges | A repeat with a different `Actor` is dropped without a trace beyond the counter and the log |
| **B) A repeat with a different `Actor` returns `Left(audit.marten.erasure_record_conflict)` (`Conflict`, ADR-062)** | Surfaces identity reuse | A legitimate retry by a different operator fails; compares only the actor because the date always differs on a retry |

### Chosen Option: **A — first record wins (recommended, pending the maintainer)**

### Rationale

- Option A wins when retries may come from any operator or job; option B wins when a `RequestId` is meant to identify one operator's request and reuse is a defect worth surfacing.

- **Class:** Dilemma

</details>

<details>
<summary><strong>8. Default legal basis</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Default `LegalObligation`; an application with another ground changes the option** | Works out of the box; the common ground for audit trails | A wrong default is recorded silently by applications that never read the option |
| **B) No default: the validator fails the start-up until the application sets the option** | Every deployment states its ground | One more required setting before first use |

### Chosen Option: **A — default `LegalObligation` (recommended, pending the maintainer)**

### Rationale

- Option A wins for the common case (audit retention is a legal obligation); option B wins if the maintainer wants no deployment to record a ground it never chose.

- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: DSR context and recorder abstraction (PR 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Compliance.DataSubjectRights/Model/ErasureRequestContext.cs`: `sealed record ErasureRequestContext(string RequestId, DateTimeOffset RequestedAtUtc, string Actor, string? TenantId)`.
2. `src/Encina.Compliance.DataSubjectRights/Abstractions/IErasureRequestRecorder.cs`: `ValueTask<Either<EncinaError, Unit>> RecordAsync(ErasureRequestContext context, CancellationToken ct)` (Design Choice 6 option A; if the maintainer picks B the interface gains `CompleteAsync`).
3. `Abstractions/IDataErasureExecutor.cs`: `EraseAsync(string subjectId, ErasureScope scope, ErasureRequestContext context, CancellationToken ct)` (changed completely, no overload kept).
4. `Requests/ErasureRequest.cs`: add required `RequestId` and `Actor` (Design Choice 2 contract).
5. `PublicAPI.Unshipped.txt` lines (via `mechanical-fixer`): change lines 94, 137 and 314 and add the new members.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Compliance.DataSubjectRights. IDataErasureExecutor.EraseAsync(subjectId, scope, ct) has no request id, actor or tenant. Issue #2847, Design Choices 2, 3 and 6.
TASK: Add ErasureRequestContext and IErasureRequestRecorder; change IDataErasureExecutor.EraseAsync to take the context; add required RequestId and Actor to ErasureRequest. Do not keep the old overload.
KEY RULES: no [Obsolete], no compatibility layer (pre-1.0); Either<EncinaError, T>; XML docs with examples; the context never carries the subject id; PublicAPI.Unshipped.txt lines 94, 137, 314 changed.
REFERENCE FILES: src/Encina.Compliance.DataSubjectRights/Abstractions/IDataErasureExecutor.cs, src/Encina.Compliance.DataSubjectRights/Requests/ErasureRequest.cs, src/Encina.Compliance.DataSubjectRights/Model/ErasureResult.cs
```

</details>

### Phase 2: Executor and service wiring (PR 1)

<details>
<summary>Tasks</summary>

1. `Erasure/DefaultDataErasureExecutor.cs`: inject `IEnumerable<IErasureRequestRecorder>`; call each recorder FIRST in `EraseAsync`, before the location lookup result is processed and before any strategy; a `Left` stops and is returned (logged with `ErasureFailed(code)`), recorders never hidden behind a `try/catch`. Recorders run also when `locations.Count == 0` (the audit trail is retained regardless).
2. `Services/DefaultDSRService.cs` (`HandleErasureAsync`, line 622): validate `RequestId` and `Actor` (non-blank) and return `Left(dsr.erasure_request_incomplete)` (`InvalidRequest`) before logging the start; build the context from `request.RequestId`, `request.Actor`, `_timeProvider.GetUtcNow()` and the tenant source; pass it to the executor; wrap executor errors with the inner code only (Design Choice 3).
3. Update the existing tests of the executor and the service for the new signature (`DefaultDataErasureExecutorTests.cs`, `DefaultDataErasureExecutorGuardTests.cs`, `DefaultDSRServiceTests.cs`, `ModelRecordTests.cs`, the two diagnostics leak tests). Add: recorder `Left` leaves every strategy uncalled; recorder runs on the empty-locations path; blank request id or actor returns the `InvalidRequest` error and calls nothing; a recorder error message never appears in the returned error or logs.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 added ErasureRequestContext and IErasureRequestRecorder. DefaultDataErasureExecutor currently returns early with an empty result when no locations exist.
TASK: Call every registered IErasureRequestRecorder FIRST in DefaultDataErasureExecutor.EraseAsync (before any strategy, also on the empty-locations path), validate and thread the context from DefaultDSRService.HandleErasureAsync.
KEY RULES: a recorder Left fails the erasure and no strategy runs (AGENTS.md section 3, ADR-058 write-ahead); log only error codes via the existing ErasureFailed message; never log the subject id or EncinaError.Message; the service wraps an executor error with its code only; constructor guards ArgumentNullException; no new EventId (ComplianceDSR 8300-8349 is full); the new DSR error code dsr.erasure_request_incomplete is created with ErrorCategory.InvalidRequest.
REFERENCE FILES: src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs, src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs, src/Encina.Compliance.DataSubjectRights/DSRErrors.cs
```

</details>

### Phase 3: Audit-side document, key, enum and options (PR 2)

<details>
<summary>Tasks</summary>

1. `src/Encina.Audit.Marten/Erasure/AuditRetentionLegalBasis.cs` (enum), `Erasure/AuditErasureRequestRecord.cs` (document: `Id`, `RequestId`, `RequestedAtUtc`, `Actor`, `LegalBasis`, `RecordedAtUtc`, `TenantId`) and `Erasure/AuditErasureRequestKey.cs` (the single id factory of Design Choice 4, with the reserved-`default` refusal).
2. `MartenOperationAuditOptions`: `ErasureLegalBasis` (default per Design Choice 8); validator rejects undefined values.
3. `Projections/ConfigureMartenOperationAuditProjections.cs`: register the document (identity on `Id`, index on `RequestedAtUtc`).
4. `MartenAuditErrors`, each created with `EncinaErrors.Create(code, category, ...)` (ADR-062): reuse `InvalidErasureReferenceCode` (`Validation`, R3), add `ErasureRecordFailedCode = "audit.marten.erasure_record_failed"` (`Internal`, R4) and `ErasureTenantRequiredCode = "audit.marten.erasure_tenant_required"` (`InvalidRequest`, R3); a `GetCategory` unit test for each.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Encina.Audit.Marten already has ErasureReference/RetentionReference (src/Encina.Audit.Marten/Crypto) and TemporalKeyDestroyedMarker with a deterministic document id.
TASK: Add AuditRetentionLegalBasis, AuditErasureRequestRecord, AuditErasureRequestKey (length-prefixed tenant-qualified id), the ErasureLegalBasis option (with validation), the Marten document registration and the three error codes with their categories.
KEY RULES: options without secrets need no ToString override; validator returns failures, never throws business errors; UTC suffix naming (RequestedAtUtc, RecordedAtUtc); no subject identifier in the document; the key factory is the only place an id is built; add the property test "distinct (tenant, request id) pairs never collide" and the reserved-default test.
REFERENCE FILES: src/Encina.Audit.Marten/Crypto/TemporalKeyDestroyedMarker.cs, src/Encina.Audit.Marten/MartenOperationAuditOptions.cs, src/Encina.Audit.Marten/MartenOperationAuditOptionsValidator.cs, src/Encina.Audit.Marten/Projections/ConfigureMartenOperationAuditProjections.cs, src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

### Phase 4: Recorder and registration (PR 3)

<details>
<summary>Tasks</summary>

1. `src/Encina.Audit.Marten/Erasure/MartenAuditErasureRequestRecorder.cs` (`internal sealed`, `IErasureRequestRecorder`, Scoped): maps the context to an `ErasureReference`, fails with `InvalidErasureReference` when `!IsValid`; resolves the tenant key (fail closed under `MultiTenancyMarker`, Design Choice 4); loads the document; when present returns success without writing; otherwise checks for an existing entry (`GetByEntityAsync`), records the write-ahead `OperationAuditEntry` of the recording through `IOperationAuditStore` (action `ErasureRequestRecorded`, entity type `AuditTrail`, entity id = the tenant-qualified id) only when absent, awaited, then inserts and saves the document.
2. `ServiceCollectionExtensions.AddEncinaAuditMarten`: `services.TryAddEnumerable(ServiceDescriptor.Scoped<IErasureRequestRecorder, MartenAuditErasureRequestRecorder>())`.
3. DI test with `ValidateOnBuild` and `ValidateScopes`, in both registration orders relative to `AddEncinaComplianceDataSubjectRights`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1-3 are done. IOperationAuditStore (Encina.Security.Audit) is replaced by MartenOperationAuditStore by AddEncinaAuditMarten.
TASK: Implement MartenAuditErasureRequestRecorder and its registration with TryAddEnumerable.
KEY RULES: time from TimeProvider; async with CancellationToken; the audit entry is written before the document, awaited, and skipped when an entry of this request already exists (ADR-058); the operation is two commits, NOT atomic, and ordered so every interruption converges on retry; a duplicate insert is success; tenancy on without a tenant returns the tenant-required Left before any write; never log the subject id, the actor beyond its type, or EncinaError.Message; Either everywhere.
REFERENCE FILES: src/Encina.Audit.Marten/MartenOperationAuditStore.cs, src/Encina.Audit.Marten/Crypto/MartenTemporalKeyProvider.cs, src/Encina.Audit.Marten/ServiceCollectionExtensions.cs
```

</details>

### Phase 5: Observability (PR 4)

<details>
<summary>Tasks</summary>

1. New EventId range: the `AuditMarten` range (2550-2599) is not available: the merged #3049 plan takes 2593-2598 of it and leaves only 2599. This plan therefore takes a new range `AuditMartenErasure = (6360, 6369)` from the free row 6360-6399 of the batch registry (`eventid-blocks.md`; registry row `6360-6369 | audit-erasure: #2847 recorder | claimed 2026-11-12`), and registers it with the steps of AGENTS.md section 7: add `public static readonly (int Min, int Max) AuditMartenErasure = (6360, 6369);` to `src/Encina/Diagnostics/EventIdRanges.cs`, add the line to `PublicAPI.Unshipped.txt` of `Encina`, and map `Encina.Audit.Marten` to the new range in the `AssemblyRanges` map of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (the assembly maps to both ranges). Re-read the registry before writing the numbers.
2. `Diagnostics/MartenAuditErasureLog.cs` (new, `[LoggerMessage]`): 6360 request recorded, 6361 request already recorded (no-op), 6362 recording failed (error code), 6363 invalid reference, 6364 tenant required. Packed sequentially, five ids used, 6365-6369 left in the range.
3. `Diagnostics/MartenAuditMeter.cs`: `Counter<long>` `audit.marten.erasure_requests.recorded` with tag `outcome` (recorded, duplicate, failed) and, only when multi-tenancy is on, the tag `encina.tenant_id` (ADR-054 D4: no tenant tag when tenancy is off); `MartenAuditActivitySource`: `audit.marten.erasure.record` activity with the request id tag and, only when tenancy is on, the `encina.tenant_id` attribute.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Encina.Audit.Marten owns EventIds 2550-2599 (2593-2598 are taken by plan #3049); this plan takes the new range AuditMartenErasure 6360-6369 from the batch registry artifacts/decisions/plan-batch/eventid-blocks.md (re-read it first).
TASK: Register the range (EventIdRanges.cs, PublicAPI of Encina, AssemblyRanges map), add log messages 6360-6364, the outcome counter and the activity.
KEY RULES: ADR-021 sequential ids; low-cardinality tags; encina.tenant_id only when multi-tenancy is on (ADR-054 D4); no subject identifier, no EncinaError.Message, exceptions through ForLogging(); run the architecture tests.
REFERENCE FILES: src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditMeter.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditActivitySource.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 6: Testing and documentation (spread over the PRs)

<details>
<summary>Tasks</summary>

1. Unit: executor calls recorders first (also on empty locations) and no strategy runs after a recorder `Left` (PR 1); recorder maps context, rejects invalid references, is idempotent, skips the entry when one exists, fails closed under tenancy without a tenant (PR 3); options validator (PR 2). NSubstitute for `IDocumentSession`/store.
2. Guard: new constructors and public methods throw `ArgumentNullException` (`tests/Encina.GuardTests/AuditMarten/`, `.../Compliance/DataSubjectRights/`).
3. Contract: `IErasureRequestRecorder` implementations honour the contract (success on repeat, `Left` on invalid context) with the real recorder (PR 3).
4. Property: the key factory never collides across (tenant, request id) pairs (PR 2).
5. Integration (Marten on PostgreSQL via Testcontainers, `MartenCollection`; no in-memory store for a compliance module; PR 3): a request through `DefaultDSRService.HandleErasureAsync` creates one record; repeating it creates none and fails nothing; two tenants with the same `RequestId` get two records; tenancy on without a tenant denies and deletes nothing; failure injection (entry written, insert fails, retry converges to one entry and one record); audit data and keys of other periods are untouched; the audit entry of the recording exists.
6. Load and benchmark: `.md` justifications (rare write path, no concurrency beyond the insert-once race tested above) (PR 4).
7. Docs via `docs-writer` (PR 4): `Encina.Audit.Marten` README (erasure recording, legal basis, `[EXPIRED]` after retention, the stored `Actor` is the operator's personal data kept for the retention period) and the operator runbook; the DSR README example for the new `ErasureRequest` (PR 4, because PR 1 is at the 15-file limit; the example is stale for one PR); changelog fragments via `mechanical-fixer`: `changelog.d/2847-erasure-request-context.changed.md` (PR 1) and `changelog.d/2847-audit-erasure-recording.added.md` (PR 3).
8. Per-file coverage targets, written to `Encina.Compliance.DataSubjectRights.json` and `Encina.Audit.Marten.json` via `mechanical-fixer` (the worker measures the baseline with the AGENTS.md section 9 commands; these numbers are the floor):

| File (PR) | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|-----------|------|-------|----------|----------|-------------|----------------------------|
| `DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs` (1) | 90 | 40 | 0 | 0 | 0 | The new first-step recorder loop, the empty-locations path and the failure return are unit-tested with substituted recorders and strategies. |
| `DataSubjectRights/Services/DefaultDSRService.cs` (1) | 85 | 30 | 0 | 0 | 0 | The new validation, context build and code-only wrapping run in unit tests; the rest of the service keeps its existing coverage. |
| `DataSubjectRights/Model/ErasureRequestContext.cs`, `Requests/ErasureRequest.cs` (1) | 100 | 0 | 0 | 0 | 0 | Records with no logic, constructed by the service tests. |
| `Audit.Marten/Erasure/AuditErasureRequestKey.cs` (2) | 100 | 100 | 0 | 95 | 0 | One pure function: unit cases for the reserved `default` and empty parts, a property test for non-collision, guard tests for null arguments. |
| `Audit.Marten/MartenOperationAuditOptions.cs` and its validator (2) | 95 | 0 | 0 | 0 | 0 | One new property and one range check, unit-tested for defined and undefined values. |
| `Audit.Marten/Erasure/MartenAuditErasureRequestRecorder.cs` (3) | 90 | 70 | 90 | 0 | 85 | Every branch (present, absent, entry exists, invalid reference, tenancy denied, insert fails) has a unit case; PostgreSQL tests prove the end-to-end behavior and the failure injection. |
| `Audit.Marten/ServiceCollectionExtensions.cs` (3) | 95 | 65 | 0 | 0 | 0 | The `TryAddEnumerable` registration is covered by the DI test in both orders. |
| `Audit.Marten/Diagnostics/MartenAuditErasureLog.cs`, `MartenAuditMeter.cs`, `MartenAuditActivitySource.cs` (4) | 80 | 0 | 0 | 0 | 60 | The counter, activity and log calls run on every recorder outcome; unit tests assert the tags and the absence of the tenant tag with tenancy off. |

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag coverage (AGENTS.md section 9). Manifests: .github/coverage-manifest/Encina.Audit.Marten.json and Encina.Compliance.DataSubjectRights.json.
TASK: Write the tests and docs of Phase 6 for the PR you were briefed on; measure per-flag coverage of the touched files against the targets of the Phase 6 table.
KEY RULES: real implementations instantiated; Shouldly via Encina.Testing.Shouldly; shared Marten collection fixture with ClearAllDataAsync; CRAP <= 10 on changed methods; docs by docs-writer, fragments and manifest lines by mechanical-fixer; the PR must build and pass its own tests and stay at 15 files or fewer.
REFERENCE FILES: tests/Encina.UnitTests/Compliance/DataSubjectRights/DefaultDataErasureExecutorTests.cs, tests/Encina.IntegrationTests/Infrastructure/Marten/Audit/MartenTemporalKeyProviderIntegrationTests.cs, .github/coverage-manifest/Encina.Audit.Marten.json
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| GDPR Art. 17(3)(b) and (e) | Erasure does not apply where processing is needed for a legal obligation or legal claims: the audit trail |
| GDPR Art. 5(1)(e) and 5(2) | Storage limitation after retention and accountability: the record shows why data was kept |
| #2770 Q1 and Q3 | Erasure reference fields; `[EXPIRED]` after retention |
| ADR-034 | Audit encryption and temporal keys |
| ADR-054 D4 | Tenant attribute and tag only when multi-tenancy is on |
| ADR-058 (D15-D17) | Write-ahead, awaited audit of sensitive operations |
| ADR-062 | Error categories of the new codes |
| AGENTS.md section 3 | Fail closed, ROP, no subject identifier in logs |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ErasureReference`, `KeyDestructionReference` | `src/Encina.Audit.Marten/Crypto/` | Validation of the mapped reference (`IsValid`) |
| `TemporalKeyDestroyedMarker` | `src/Encina.Audit.Marten/Crypto/TemporalKeyDestroyedMarker.cs` | Pattern for a keyed Marten document; retention marker unchanged |
| `DefaultDataErasureExecutor` | `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs` | Calls the recorders first |
| `DefaultDSRService.HandleErasureAsync` | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs` | Entry point that validates and builds the context |
| `MartenOperationAuditStore` | `src/Encina.Audit.Marten/MartenOperationAuditStore.cs` | Write-ahead audit entry of the recording; `GetByEntityAsync` for the retry check |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Detects tenancy-on for the fail-closed gate |
| `MartenAuditErrors` | `src/Encina.Audit.Marten/MartenAuditErrors.cs` | Error codes |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Audit.Marten | 6360-6364 of the new range `AuditMartenErasure` (6360-6369) | New range from the free row 6360-6399 of the batch registry; the `AuditMarten` range 2550-2599 is not used because #3049 holds 2593-2598. Five ids used |
| Encina.Compliance.DataSubjectRights | none | `ComplianceDSR` 8300-8349 is full; the executor reuses `ErasureFailed(code)` |

### Estimated File Count

| PR | Category | Files | Notes |
|----|----------|-------|-------|
| 1 (DSR) | Production | 6 | `ErasureRequestContext.cs` (new), `IErasureRequestRecorder.cs` (new), `IDataErasureExecutor.cs`, `ErasureRequest.cs`, `DefaultDataErasureExecutor.cs`, `DefaultDSRService.cs` |
| 1 | Tests | 6 | Executor unit, executor guard, `DefaultDSRServiceTests.cs`, `ModelRecordTests.cs`, the two diagnostics leak tests (all modified) |
| 1 | Metadata | 3 | `PublicAPI.Unshipped.txt`, `Encina.Compliance.DataSubjectRights.json`, fragment `2847-erasure-request-context.changed.md` |
| 1 | **Total** | **15** | Builds alone: no recorder is registered, so the loop is a no-op |
| 2 (document, key, options) | Production | 7 | Enum, document, key factory, options, validator, configure, errors |
| 2 | Tests | 4 | Key unit, key property, options/validator unit (modified), guard (new) |
| 2 | Metadata | 2 | `PublicAPI.Unshipped.txt`, manifest |
| 2 | **Total** | **13** | Needs nothing from PR 1 |
| 3 (recorder) | Production | 2 | Recorder (new), `ServiceCollectionExtensions.cs` |
| 3 | Tests | 4 | Recorder unit, contract, DI test (modified), PostgreSQL integration (new) |
| 3 | Metadata | 3 | `PublicAPI.Unshipped.txt`, manifest, fragment `2847-audit-erasure-recording.added.md` |
| 3 | **Total** | **9** | Needs PRs 1 and 2 |
| 4 (observability, docs) | Production | 6 | `EventIdRanges.cs`, `MartenAuditErasureLog.cs` (new), `MartenAuditMeter.cs`, `MartenAuditActivitySource.cs`, recorder (log/meter calls), `PublicAPI.Unshipped.txt` of `Encina` |
| 4 | Tests | 4 | `EncinaEventIdAllocationTests.cs` map, observability unit test (new), load and benchmark `.md` justifications |
| 4 | Docs and metadata | 4 | Audit.Marten README, runbook, DSR README, manifest |
| 4 | **Total** | **14** | Needs PR 3 |
| | **Grand total** | **51** | Every PR is under 15 files and under 1,500 lines |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #2847: an erasure request reaches the audit trail and is recorded with its erasure reference and legal basis (maintainer decision: path C, record and keep until retention; no key is destroyed). AGENTS.md rules apply (ROP, fail closed, TimeProvider, no subject id in logs, per-flag coverage, CRAP <= 10). Subject-scoped destruction is issue #2903, not this plan.

IMPLEMENTATION OVERVIEW: PR 1 (DSR): ErasureRequestContext, IErasureRequestRecorder, recorders called FIRST in the executor, required RequestId and Actor. PR 2 (Audit.Marten): AuditErasureRequestRecord, tenant-qualified key factory, legal-basis option. PR 3: MartenAuditErasureRequestRecorder and registration. PR 4: EventId range AuditMartenErasure 6360-6369, observability, docs.

KEY PATTERNS: TryAddEnumerable for the recorder; keyed Marten document with a length-prefixed tenant-qualified id, insert once; two commits (audit entry then document) ordered so every interruption converges on retry; Marten on PostgreSQL through Testcontainers, never an in-memory store; error categories per ADR-062; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs, src/Encina.Audit.Marten/Crypto/TemporalKeyDestroyedMarker.cs, src/Encina.Audit.Marten/MartenOperationAuditStore.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs, .github/coverage-manifest/Encina.Audit.Marten.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A write-once operation with no read cache |
| 2 | OpenTelemetry | ✅ | Outcome counter and activity (Phase 5), tenant tag only when tenancy is on (ADR-054 D4) |
| 3 | Structured Logging | ✅ | EventIds 6360-6364 of the new `AuditMartenErasure` range; codes and request ids only |
| 4 | Health Checks | ❌ | No new checkable dependency |
| 5 | Validation | ✅ | `ErasureReference.IsValid`, the blank-id check of `HandleErasureAsync` and the legal-basis option are validated before recording |
| 6 | Resilience | ❌ | Local database write; no external call |
| 7 | Distributed Locks | ❌ | Not applicable: concurrent identical requests converge through the insert-once document, and the only race outcome (two audit entries for one request) is append-only evidence that is accepted and documented |
| 8 | Transactions | ❌ | Not atomic by design: two commits (audit entry, then document), ordered and retry-safe (Design Choice 4); the recorder runs before the destructive step so a failure never leaves a deletion unrecorded |
| 9 | Idempotency | ✅ | Tenant-qualified keyed document; a repeated reference is a no-op |
| 10 | Multi-Tenancy | ✅ | Integrated now: tenant-qualified id (injective), tenant required under `MultiTenancyMarker` (fail closed), reserved `default` partition refused; this plan creates and destroys no key, so the tenancy of audit temporal keys is outside it |
| 11 | Module Isolation | ❌ | Not module-scoped |
| 12 | Audit Trail | ✅ | The recording is itself an audit entry written ahead (ADR-058) |

---

## Next Steps

1. Commit this plan, open its PR and link it from #2847.
2. When the issue is picked up, re-check the choices (notably the executor signature change against any newer callers) and ask the maintainer about the Dilemmas (2, 4, 6, 7, 8).
3. Brief PR 1 (DSR) first, then PR 2, PR 3 and PR 4 in order; #2903 follows once the recorder exists.
