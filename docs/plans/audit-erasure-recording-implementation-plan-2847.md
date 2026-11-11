# Implementation Plan: Erasure requests reach the audit trail with a recorded erasure reference

> **Issue**: [#2847](https://github.com/dlrivada/Encina/issues/2847)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 2 packages plus DSR, ~24 files, proposed split in 2 PRs)
> **Estimated Scope**: ~600 lines of production code + ~1,100 lines of tests

---

## Summary

The audit trail keeps an erasure request invisible: the data subject rights executor (`DefaultDataErasureExecutor`) never reaches `Encina.Audit.Marten`, and no erasure flow calls `ITemporalKeyProvider.DestroyKeysBeforeAsync`. The maintainer decision of 2026-10-26 on #2847 settles the scope: **path C is the default**. Audit data is excluded from erasure by GDPR Art. 17(3)(b) (legal obligation) and 17(3)(e) (legal claims); an erasure request that reaches the audit trail is **recorded** (erasure reference: `ErasureRequestId`, date, actor, plus the legal basis of the retention) and the data stays until its retention expires, when the period key is destroyed with a `RetentionReference` and fields read `[EXPIRED]`. No audit key is destroyed by an erasure request on this path and no other subject's audit data is ever affected. Subject-scoped destruction (path A, the `[SHREDDED]` exceptional route) is the separate issue #2903.

What already exists (PR 2a of #2770): `ErasureReference`, `KeyDestructionReference`, `RetentionReference`, the `reference` parameter of `DestroyKeysBeforeAsync` and the marker fields (`ErasureRequestId`, `ErasureRequestedAtUtc`, `ErasureActor`) on `TemporalKeyDestroyedMarker`. What is missing, and this plan adds:

1. A DSR-side context and recorder abstraction (`ErasureRequestContext`, `IErasureRequestRecorder`) and the executor call that notifies recorders for each erasure request.
2. An audit-side recorder, `MartenAuditErasureRequestRecorder`, writing an `AuditErasureRequestRecord` document (no subject identifier) idempotently, with the legal basis and the write-ahead audit entry of the recording itself.

- **Affected packages**: `Encina.Compliance.DataSubjectRights`, `Encina.Audit.Marten` (the issue also names `Encina.Marten.GDPR`; it needs no change because the audit recorder is registered by `Encina.Audit.Marten`, which already references it).
- **Provider category**: event sourcing, Marten on PostgreSQL only; the 10-database matrix does not apply.
- **Scope and split**: about 24 files. Two worker PRs: PR 1 = DSR (Phases 1-2, about 9 files); PR 2 = `Encina.Audit.Marten` (Phases 3-6, about 15 files).
- **Prerequisites**: PR 2a of #2770 is merged (the types above exist). #2903 builds on the same marker and recorder and is planned in its own file.

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
<summary><strong>2. How the erasure request context (request id, date, actor) reaches the executor</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `ErasureRequestContext(RequestId, RequestedAtUtc, Actor)` record, a required parameter of `IDataErasureExecutor.EraseAsync`; `ErasureRequest` gains `RequestId` and `Actor`** | Explicit, testable, no hidden state; pre-1.0 so the signature is changed completely | Touches the interface, the executor, `DefaultDSRService.HandleErasureAsync` and their tests |
| **B) Carry it inside `ErasureScope`** | No interface change | `ErasureScope` says what to erase, not who asked; mixing concerns |
| **C) Ambient (`AsyncLocal`) context** | No signature change | Hidden; breaks in background processing; harder to test |

### Chosen Option: **A — explicit context parameter (recommended, pending the maintainer)**

### Rationale

- Today `HandleErasureAsync(ErasureRequest)` (`DefaultDSRService.cs:622`) has only `SubjectId`, `Reason` and `Scope`; there is no id or actor anywhere on the erasure path, so some carrier is needed.
- Option A wins when erasure is driven from several entry points (service, background job); option B wins only if the maintainer wants no interface change at all.
- `ErasureReference` lives in `Encina.Audit.Marten.Crypto` and DSR cannot reference it (dependency direction is Audit.Marten to Marten.GDPR to DSR), hence a DSR-level context that the audit recorder maps to an `ErasureReference`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Which abstraction notifies the audit trail: a recorder called by the executor</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IErasureRequestRecorder` (DSR), invoked by the executor once per request for every registered recorder** | Generic; pay-for-what-you-use (none registered = no-op); no audit dependency in DSR | New public interface |
| **B) A locator returning audit locations flagged `HasLegalRetention` and an `IDataErasureStrategy` | Reuses existing abstractions | Executor never calls the strategy for retained fields, so nothing is recorded; locating a subject's audit entries means decrypting every user id |
| **C) Call `Encina.Audit.Marten` directly from DSR** | Smallest | Inverts the package dependency |

### Chosen Option: **A — `IErasureRequestRecorder`**

### Rationale

- Recording is unconditional per request (no subject lookup, so no subject identifier is read, stored or logged), which keeps the record accurate: the audit trail is always retained under Art. 17(3).
- A recorder failure fails the erasure (returns the `Left`; the existing `ErasureFailed(code)` log is used, so DSR needs no new EventId; `ComplianceDSR` 8300-8349 is exhausted at 8349).

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (errors never swallowed in background infrastructure; compliance gates fail closed) and the module-dependency direction in `src/Encina.Audit.Marten/Encina.Audit.Marten.csproj`

</details>

<details>
<summary><strong>4. Storage and idempotency of the record</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marten document `AuditErasureRequestRecord` with id `audit-erasure-request:{RequestId}`; insert once, a repeat is a no-op** | Same pattern as `TemporalKeyDestroyedMarker` (`temporal-destroyed:{period}`); idempotent by id | One more document type to register |
| **B) Append an audit event to the audit stream** | Reuses the event store | Events are encrypted per period; a request is not tied to one; replay semantics unclear |
| **C) Extend `TemporalKeyDestroyedMarker`** | No new type | A marker exists only after a destruction; path C destroys nothing |

### Chosen Option: **A — keyed document, insert-once**

### Rationale

- Load-then-insert in one session; a concurrent duplicate insert (Marten document-already-exists) is treated as success, so concurrent identical requests converge without a distributed lock.
- The record holds `RequestId`, `RequestedAtUtc`, `Actor`, `LegalBasis`, `RecordedAtUtc` (from `TimeProvider`) and `TenantId`; never a subject identifier.
- Idempotency of a repeated request with the same reference is the issue's acceptance criterion.

- **Class:** Settled
- **Settled by:** issue #2847 acceptance criteria (a repeat neither fails nor destroys data) and AGENTS.md section 3 (`TimeProvider`, ROP)

</details>

<details>
<summary><strong>5. Legal basis recorded with the request</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Enum `AuditRetentionLegalBasis { LegalObligation, LegalClaims }` (Art. 17(3)(b), (e)), configurable default on `MartenOperationAuditOptions`** | Closed set matches the decision; auditable | Another option property |
| **B) Free-text string** | Flexible | Unvalidated; cannot be reported by category |
| **C) Reuse `DSR.ErasureExemption`** | Existing enum | Its members (FreedomOfExpression, PublicHealth, Archiving) are not valid grounds for the audit trail; couples audit to DSR |

### Chosen Option: **A — closed enum with an option-level default**

### Rationale

- The default is `LegalObligation`; an application with a different legal ground sets it in `MartenOperationAuditOptions` (validated by `MartenOperationAuditOptionsValidator`).

- **Class:** Settled
- **Settled by:** maintainer decision of 2026-10-26 on #2847 ("the legal basis of the retention") and GDPR Art. 17(3)(b) and (e)

</details>

---

## Implementation Phases

### Phase 1: DSR context and recorder abstraction

<details>
<summary>Tasks</summary>

1. `src/Encina.Compliance.DataSubjectRights/Model/ErasureRequestContext.cs`: `sealed record ErasureRequestContext(string RequestId, DateTimeOffset RequestedAtUtc, string Actor)`.
2. `src/Encina.Compliance.DataSubjectRights/Abstractions/IErasureRequestRecorder.cs`: `ValueTask<Either<EncinaError, Unit>> RecordAsync(ErasureRequestContext context, ErasureResult result, CancellationToken ct)`.
3. `Abstractions/IDataErasureExecutor.cs`: `EraseAsync(string subjectId, ErasureScope scope, ErasureRequestContext context, CancellationToken ct)` (changed completely, no overload kept).
4. `Requests/ErasureRequest.cs`: add `RequestId` and `Actor`.
5. `PublicAPI.Unshipped.txt` lines (via `mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Compliance.DataSubjectRights. IDataErasureExecutor.EraseAsync(subjectId, scope, ct) has no request id or actor. Issue #2847, Design Choices 2 and 3.
TASK: Add ErasureRequestContext and IErasureRequestRecorder; change IDataErasureExecutor.EraseAsync to take the context; add RequestId and Actor to ErasureRequest. Do not keep the old overload.
KEY RULES: no [Obsolete], no compatibility layer (pre-1.0); Either<EncinaError, T>; XML docs with examples; the context never carries the subject id; PublicAPI.Unshipped.txt.
REFERENCE FILES: src/Encina.Compliance.DataSubjectRights/Abstractions/IDataErasureExecutor.cs, src/Encina.Compliance.DataSubjectRights/Requests/ErasureRequest.cs, src/Encina.Compliance.DataSubjectRights/Model/ErasureResult.cs
```

</details>

### Phase 2: Executor and service wiring

<details>
<summary>Tasks</summary>

1. `Erasure/DefaultDataErasureExecutor.cs`: inject `IEnumerable<IErasureRequestRecorder>`; after the strategy loop and before building the result, call each recorder; a `Left` stops and is returned (logged with `ErasureFailed(code)`), recorders never hidden behind a `try/catch`. Recorders also run when `locations.Count == 0` (the audit trail is retained regardless).
2. `Services/DefaultDSRService.cs` (`HandleErasureAsync`, line 622): build the context from `request.RequestId`, `request.Actor` and `_timeProvider.GetUtcNow()`; pass it to the executor.
3. Update existing tests of the executor and the service for the new signature (`tests/Encina.UnitTests/Compliance/DataSubjectRights/DefaultDataErasureExecutorTests.cs`, `tests/Encina.GuardTests/Compliance/DataSubjectRights/DefaultDataErasureExecutorGuardTests.cs`, `...MethodGuardTests.cs`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 added ErasureRequestContext and IErasureRequestRecorder. DefaultDataErasureExecutor currently returns early with an empty result when no locations exist.
TASK: Call every registered IErasureRequestRecorder from DefaultDataErasureExecutor (also on the empty-locations path) and thread the context from DefaultDSRService.HandleErasureAsync.
KEY RULES: a recorder Left fails the erasure (AGENTS.md section 3); log only error codes via the existing ErasureFailed message; never log the subject id or EncinaError.Message; constructor guards ArgumentNullException; no new EventId (ComplianceDSR 8300-8349 is full).
REFERENCE FILES: src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs, src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs, src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Audit-side document, enum and options

<details>
<summary>Tasks</summary>

1. `src/Encina.Audit.Marten/Erasure/AuditRetentionLegalBasis.cs` (enum) and `Erasure/AuditErasureRequestRecord.cs` (document: `Id`, `RequestId`, `RequestedAtUtc`, `Actor`, `LegalBasis`, `RecordedAtUtc`, `TenantId`).
2. `MartenOperationAuditOptions`: `ErasureLegalBasis` (default `LegalObligation`); validator rejects undefined values.
3. `Projections/ConfigureMartenOperationAuditProjections.cs`: register the document (identity on `Id`, index on `RequestedAtUtc`).
4. `MartenAuditErrors`: reuse `InvalidErasureReferenceCode`; add `ErasureRecordFailedCode = "audit.marten.erasure_record_failed"`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Encina.Audit.Marten already has ErasureReference/RetentionReference (src/Encina.Audit.Marten/Crypto) and TemporalKeyDestroyedMarker with a deterministic document id.
TASK: Add AuditRetentionLegalBasis, AuditErasureRequestRecord, the ErasureLegalBasis option (with validation) and the Marten document registration.
KEY RULES: options without secrets need no ToString override; validator returns failures, never throws business errors; UTC suffix naming (RequestedAtUtc, RecordedAtUtc); no subject identifier in the document.
REFERENCE FILES: src/Encina.Audit.Marten/Crypto/TemporalKeyDestroyedMarker.cs, src/Encina.Audit.Marten/MartenOperationAuditOptions.cs, src/Encina.Audit.Marten/MartenOperationAuditOptionsValidator.cs, src/Encina.Audit.Marten/Projections/ConfigureMartenOperationAuditProjections.cs
```

</details>

### Phase 4: Recorder and registration

<details>
<summary>Tasks</summary>

1. `src/Encina.Audit.Marten/Erasure/MartenAuditErasureRequestRecorder.cs` (`internal sealed`, `IErasureRequestRecorder`, Scoped): maps the context to an `ErasureReference`, fails with `InvalidErasureReference` when `!IsValid`; loads `audit-erasure-request:{RequestId}`; when absent inserts and saves; when present returns success without writing; records a write-ahead `OperationAuditEntry` of the recording through `IOperationAuditStore` (action `ErasureRequestRecorded`, entity type `AuditTrail`, entity id = request id) before the insert, awaited.
2. `ServiceCollectionExtensions.AddEncinaAuditMarten`: `services.TryAddEnumerable(ServiceDescriptor.Scoped<IErasureRequestRecorder, MartenAuditErasureRequestRecorder>())`.
3. DI test with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1-3 are done. IOperationAuditStore (Encina.Security.Audit) is replaced by MartenOperationAuditStore by AddEncinaAuditMarten.
TASK: Implement MartenAuditErasureRequestRecorder and its registration with TryAddEnumerable.
KEY RULES: time from TimeProvider; async with CancellationToken; the audit entry of the recording is written before the document and awaited (ADR-058); a duplicate insert is success; never log the subject id, the actor beyond its type, or EncinaError.Message; Either everywhere.
REFERENCE FILES: src/Encina.Audit.Marten/MartenOperationAuditStore.cs, src/Encina.Audit.Marten/Crypto/MartenTemporalKeyProvider.cs, src/Encina.Audit.Marten/ServiceCollectionExtensions.cs
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `Diagnostics/MartenAuditLog.cs`: EventIds 2593-2596 inside the registered `AuditMarten` range (2550-2599; 2550-2592 used): request recorded, request already recorded (no-op), recording failed (error code), invalid reference.
2. `Diagnostics/MartenAuditMeter.cs`: `Counter<long>` `audit.marten.erasure_requests.recorded` with tag `outcome` (recorded, duplicate, failed); `MartenAuditActivitySource`: `audit.marten.erasure.record` activity with the request id tag only.
3. `EventIdRanges.AuditMarten` already covers the ids; `EncinaEventIdAllocationTests` already maps the assembly.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Encina.Audit.Marten owns EventIds 2550-2599; 2550-2592 are used. Ids 2597-2599 stay free for later work (the subject-scoped erasure of #2903 uses its own range).
TASK: Add log messages 2593-2596, the outcome counter and the activity.
KEY RULES: ADR-021 sequential ids; low-cardinality tags; no subject identifier, no EncinaError.Message, exceptions through ForLogging().
REFERENCE FILES: src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditMeter.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditActivitySource.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 6: Testing and documentation

<details>
<summary>Tasks</summary>

1. Unit: executor calls recorders (also on empty locations), stops on a recorder `Left`; recorder maps context, rejects invalid references, is idempotent (NSubstitute for `IDocumentSession`/store); options validator.
2. Guard: new constructors and public methods throw `ArgumentNullException` (`tests/Encina.GuardTests/AuditMarten/`, `.../Compliance/DataSubjectRights/`).
3. Contract: `IErasureRequestRecorder` implementations honour the contract (success on repeat, `Left` on invalid context).
4. Integration (Marten on PostgreSQL via Testcontainers, `MartenCollection`; no in-memory store for a compliance module): a request through `DefaultDSRService.HandleErasureAsync` creates one record; repeating it creates none and fails nothing; audit data and keys of other periods are untouched; the audit entry of the recording exists.
5. Property, load, benchmark: `.md` justifications (no complex invariant; rare write path).
6. Docs via `docs-writer`: `Encina.Audit.Marten` README (erasure recording, legal basis, `[EXPIRED]` after retention) and the operator runbook; changelog fragment `changelog.d/2847-audit-erasure-recording.added.md` via `mechanical-fixer`; per-file manifest targets with justifications for every touched `src/` file.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag coverage (AGENTS.md section 9). Manifests: .github/coverage-manifest/Encina.Audit.Marten.json and Encina.Compliance.DataSubjectRights.json.
TASK: Write the tests and docs of Phase 6; measure per-flag coverage of the touched files against the manifest targets.
KEY RULES: real implementations instantiated; Shouldly via Encina.Testing.Shouldly; shared Marten collection fixture with ClearAllDataAsync; CRAP <= 10 on changed methods; docs by docs-writer, fragments and manifest lines by mechanical-fixer.
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
| ADR-058 (D15-D17) | Write-ahead, awaited audit of sensitive operations |
| AGENTS.md section 3 | Fail closed, ROP, no subject identifier in logs |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ErasureReference`, `KeyDestructionReference` | `src/Encina.Audit.Marten/Crypto/` | Validation of the mapped reference (`IsValid`) |
| `TemporalKeyDestroyedMarker` | `src/Encina.Audit.Marten/Crypto/TemporalKeyDestroyedMarker.cs` | Pattern for a keyed Marten document; retention marker unchanged |
| `DefaultDataErasureExecutor` | `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs` | Calls the recorders |
| `DefaultDSRService.HandleErasureAsync` | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs` | Entry point that builds the context |
| `MartenOperationAuditStore` | `src/Encina.Audit.Marten/MartenOperationAuditStore.cs` | Write-ahead audit entry of the recording |
| `MartenAuditErrors` | `src/Encina.Audit.Marten/MartenAuditErrors.cs` | Error codes |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Audit.Marten | 2593-2596 | Inside the registered `AuditMarten` range 2550-2599 (2550-2592 used); 2597-2599 left free |
| Encina.Compliance.DataSubjectRights | none | `ComplianceDSR` 8300-8349 is full; the executor reuses `ErasureFailed(code)` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production (DSR) | 5 | Context, recorder interface, executor interface, request, executor, service |
| Production (Audit.Marten) | 9 | Enum, document, recorder, options, validator, configure, errors, log, meter, activity, registration |
| Tests | 10 | Unit, guard, contract, integration, justification `.md` files |
| Docs and metadata | 6 | README, runbook, changelog fragment, PublicAPI x2, manifests |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #2847: an erasure request reaches the audit trail and is recorded with its erasure reference and legal basis (maintainer decision: path C, record and keep until retention; no key is destroyed). AGENTS.md rules apply (ROP, fail closed, TimeProvider, no subject id in logs, per-flag coverage, CRAP <= 10). Subject-scoped destruction is issue #2903, not this plan.

IMPLEMENTATION OVERVIEW: PR 1 (DSR): ErasureRequestContext, IErasureRequestRecorder, executor and service wiring. PR 2 (Audit.Marten): AuditErasureRequestRecord, legal-basis option, MartenAuditErasureRequestRecorder, EventIds 2593-2596, tests, docs.

KEY PATTERNS: TryAddEnumerable for the recorder; keyed Marten document, insert once; write-ahead awaited audit entry; Marten on PostgreSQL through Testcontainers, never an in-memory store; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs, src/Encina.Audit.Marten/Crypto/TemporalKeyDestroyedMarker.cs, src/Encina.Audit.Marten/MartenOperationAuditStore.cs, src/Encina.Audit.Marten/Diagnostics/MartenAuditLog.cs, .github/coverage-manifest/Encina.Audit.Marten.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A write-once operation with no read cache |
| 2 | OpenTelemetry | ✅ | Outcome counter and activity (Phase 5) |
| 3 | Structured Logging | ✅ | EventIds 2593-2596; codes and request ids only |
| 4 | Health Checks | ❌ | No new checkable dependency |
| 5 | Validation | ✅ | `ErasureReference.IsValid` and the legal-basis option are validated before recording |
| 6 | Resilience | ❌ | Local database write; no external call |
| 7 | Distributed Locks | ⏭️ | Concurrent identical requests converge through the insert-once document; a lock is a follow-up only if contention is measured (issue to be opened then) |
| 8 | Transactions | ✅ | Record and audit entry in one Marten session save |
| 9 | Idempotency | ✅ | Keyed document; a repeated reference is a no-op |
| 10 | Multi-Tenancy | ⏭️ | The record stores `TenantId` when present; audit temporal keys are not tenant-scoped today (as the issue states), follow-up with the tenancy audit work |
| 11 | Module Isolation | ❌ | Not module-scoped |
| 12 | Audit Trail | ✅ | The recording is itself an audit entry written ahead (ADR-058) |

---

## Next Steps

1. Commit this plan, open its PR and link it from #2847.
2. When the issue is picked up, re-check the choices (notably the executor signature change against any newer callers) and ask the maintainer about Design Choice 2.
3. Brief PR 1 (DSR) first, then PR 2 (Audit.Marten); #2903 follows once the recorder exists.
