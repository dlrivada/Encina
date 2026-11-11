# Implementation Plan: `Encina.Security.Audit` — Query-Level Read Audit with Data-Subject Resolution

> **Issue**: [#1194](https://github.com/dlrivada/Encina/issues/1194) (SPEC-002 tracking id **P-06**, priority P0)
> **Type**: Feature
> **Complexity**: Medium (7 phases, no provider-specific code; the 10-provider matrix is carried by #1193)
> **Estimated Scope**: ~500-700 lines of production code + ~1,100-1,400 lines of tests, in two pull requests
> **Milestone**: v0.17.0 — Compliance Lifecycle
> **Parent EPIC**: [#1186](https://github.com/dlrivada/Encina/issues/1186) — EU regulatory readiness (SPEC-002)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-008, AC-008, scenario S13; REQ-016 and DEC-006 (fail closed); cross-cutting REQ-061 (AC-043) and REQ-062 (AC-044)
> **Depends on**: [#1193](https://github.com/dlrivada/Encina/issues/1193) (P-05, evidential read audit: entry shape, `LogReadsAsync`, failure mode, purpose enforcement, subject-id converter); plan [evidential-read-audit-implementation-plan-1193.md](evidential-read-audit-implementation-plan-1193.md)
> **Related**: [#1149](https://github.com/dlrivada/Encina/issues/1149) (closed, non-string subject ids), [#798](https://github.com/dlrivada/Encina/issues/798) (tenancy filtering for Security.Audit), P-25 (reference scenario S13)

---

## Summary

An application that reads through CQRS query handlers (Dapper, ADO.NET or EF projections) gets nothing from repository read audit, because the handler never touches an Encina repository. Today:

- `AuditPipelineBehavior<,>` (`src/Encina.Security.Audit/AuditPipelineBehavior.cs`) writes an **operation** audit entry (`IOperationAuditStore`) with the request type and a payload hash. It records no data subject, so "who accessed patient X" cannot be answered for a query.
- The read-audit abstractions (`IReadAuditStore`, `ReadAuditEntry`, `ReadAccessMethod.DirectQuery`) exist, but their only writers are the repository decorators of `Encina.Security.Audit`.
- Scenario S13 of SPEC-002 (the therapist runs a Dapper appointment-list query and one read-audit entry per patient records who, when, which data and the purpose) cannot pass with the repository audit of #1193 alone.

This plan adds **query-level read audit**:

1. A typed contract `IExposesDataSubjects<TResponse>` that a query implements to declare its data category and to compute, from the handler's response, the data subjects it exposes (settled on the issue, 2026-10-17: a typed interface, not an attribute and not reflection over the response).
2. A pipeline behavior `QueryReadAuditPipelineBehavior<TRequest,TResponse>` that, after the handler succeeds, resolves the subjects and writes **one read-audit entry per distinct subject** in one batch through the `IReadAuditStore` of P-05, honouring the failure mode (fail closed by default), purpose enforcement, tenant and correlation context.
3. Fail-closed subject resolution: a declared subject that is `null` or cannot be converted, or a declaration that throws, returns a coded error (`read_audit.subject_unresolved`) and the response is not returned (SPEC-002 REQ-016, DEC-006); there is no silent fallback to the authenticated caller.

**Standards**: GDPR Art. 5(2), Art. 15, Art. 30, Art. 32; AEPD PD-00068-2026 [S] (a patient may learn who accessed their data); EHDS Reg. (EU) 2025/327 Art. 9 [S]; SPEC-002 REQ-007, REQ-008, REQ-016, REQ-019.

**Affected packages**: `Encina.Security.Audit` only. The core `Encina` package is not touched by this plan (the `IDataSubjectIdConverter` it consumes is created by #1193, OD-3).

**Provider category**: none of its own. The behavior writes through `IReadAuditStore`, whose 10 database providers (ADO.NET x3, Dapper x3, EF Core x3, MongoDB) and Marten implementation are owned by #1193. This plan adds no provider-specific code; one integration test proves a Dapper query handler on PostgreSQL end to end (S13) and one test per provider family proves the entries are stored and queryable by subject.

**Size**: the code is small, but the tests and documentation add many files, so it ships as two worker pull requests (see Phase 6 and Next Steps): PR A (contract, behavior, DI, diagnostics, unit and guard tests; about 13 files, 900 lines) and PR B (contract, property and integration tests, docs, changelog; about 12 files, 1,000 lines).

---

## Design Choices

<details>
<summary><strong>1. Declaration Mechanism — typed interface <code>IExposesDataSubjects&lt;TResponse&gt;</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Typed interface on the request: `IExposesDataSubjects<TResponse>` with `DataCategory` and `GetDataSubjects(TResponse)`** | Computes subjects from the response (the only place the ids exist for a list query); no reflection; compile-time checked; the request type opts itself in | One interface per query that needs audit |
| **B) An attribute on the query or response** | Declarative | An attribute cannot compute subjects from a response |
| **C) Reflect over the response for id properties** | No code in the query | Fragile and silently incomplete: it fails open when a property is missing or renamed |

### Chosen Option: **A — typed interface**

### Rationale

- The issue records the choice (comment of 2026-10-17, "Settled by elimination"): B cannot compute subjects, C fails open while SPEC-002 REQ-016 and DEC-006 require fail closed.
- The marker is the opt-in: a query that does not implement it is never touched by the behavior (pay-for-what-you-use, AGENTS.md section 1).
- `TResponse` is contravariant (`in`): the interface only consumes the response.
- Departure from the issue's sketch: `GetDataSubjects` returns `IEnumerable<object?>` instead of `IEnumerable<string>`. The sketch converts with `ToString()` in the query, which is the defect of #1149 (a `Guid` or strongly-typed id formatted by hand, or a `null` turned into an empty string). Returning the typed ids lets the shared converter apply the same rules as DSR and consent.

**Class:** Settled
**Settled by:** Issue #1194 comment of 2026-10-17 (settled by elimination); SPEC-002 REQ-016 and DEC-006; AGENTS.md section 3 ("Compliance and security gates fail closed")

</details>

<details>
<summary><strong>2. Where the Behavior Lives — a new behavior beside <code>AuditPipelineBehavior</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `QueryReadAuditPipelineBehavior<,>` in `Encina.Security.Audit`** | Operation audit and read audit keep separate stores, options and failure modes; the read-audit behavior is the only writer of `IReadAuditStore` for queries | Two behaviors can both run for a query (operation audit and read audit), each with its own entry |
| **B) Extend `AuditPipelineBehavior` to also write read-audit entries** | One behavior | Mixes `IOperationAuditStore` and `IReadAuditStore`, two options classes and two failure modes in one type; the behavior is already large |
| **C) A new package `Encina.Security.Audit.Queries`** | Isolated | A package for one behavior; `Encina.Security.Audit` already owns `IReadAuditStore` |

### Chosen Option: **A — new behavior in the same package**

### Rationale

- `IOperationAuditStore` (what was requested, by whom, with what outcome) and `IReadAuditStore` (which subject's data was read) are separate abstractions with separate retention and queries; the behavior follows that split.
- The behavior reuses the shape of `AuditPipelineBehavior`: static per-closed-generic-type decisions, scoped store resolution through `IServiceScopeFactory`, `TransactionScope.Suppress` around the write.

**Class:** Trivial
**Settled by:** Existing convention in `src/Encina.Security.Audit/` (`IOperationAuditStore` and `IReadAuditStore` are separate abstractions); AGENTS.md section 3 (provider coherence: abstractions live apart from implementations); SPEC-002 REQ-008 ("the audit behaviour writes one REQ-007 entry per subject")

</details>

<details>
<summary><strong>3. Entry Granularity and Content — one entry per distinct subject, one batch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One entry per distinct subject in the response, written with one `LogReadsAsync` call and linked by a shared `ReadOperationId`** | Matches AC-008 and S13 ("one read-audit entry per patient"); the per-subject query of #1193 is an indexed lookup; one round trip | A list of 500 patients writes 500 rows |
| **B) One entry per query with the list of subjects in `Metadata`** | One row | The per-subject query cannot use an index; subject ids sit in a metadata blob |
| **C) One entry per response row** | Simple | A subject that appears 30 times writes 30 identical entries |

### Chosen Option: **A — one entry per distinct subject, one batch**

### Rationale

- Entry fields: `EntityType` = the request type name; `EntityId` = `null`; `DataSubjectId` and `DataCategory` as declared (the subject id is stored as a keyed hash, per #1193 OD-5); `UserId`, `TenantId` and `CorrelationId` from `IRequestContext`; `Purpose` from `IReadAuditContext`; `AccessMethod = ReadAccessMethod.DirectQuery` (already in the enum); `EntityCount` = the number of times the subject occurs in the response; `Metadata["request_type"]`; shared `ReadOperationId`.
- Distinctness uses the converted subject string with ordinal comparison.
- A response that exposes no subject writes one entry with `EntityCount = 0` and no subject (evidence that the lookup happened), as #1193 does for repository reads.

**Class:** Settled
**Settled by:** #1193 plan, Design Choice 2 (maintainer decision OD-2, settled 2026-09-23); SPEC-002 AC-008 and S13

</details>

<details>
<summary><strong>4. Failure Semantics — fail closed by default, same options as P-05</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `ReadAuditOptions.FailureMode` of #1193 (default `FailClosed`); on a store failure or an unresolved subject return `Left`, without the response** | One setting for repository and query audit; S13 requires "with fail-closed on, an audit-store failure fails the read" | A store outage blocks audited queries |
| **B) A separate failure-mode setting for queries** | Queries could be fail-open while repositories are fail-closed | Two settings that must be kept in step; a way to weaken evidence by accident |
| **C) Always fail open (log and return the response)** | Never blocks a read | Not evidence; contradicts DEC-006 and the AGENTS.md rule that errors are not swallowed |

### Chosen Option: **A — one failure mode, fail closed by default**

### Rationale

- Order in the behavior: purpose check (reject before the handler runs) -> handler -> on `Right`, resolve subjects -> `LogReadsAsync` awaited -> return the response, or `Left(read_audit.audit_write_failed)` / `Left(read_audit.subject_unresolved)` in fail-closed mode.
- In fail-open mode (the explicit, logged opt-out of #1193 OD-4) a failure is logged by error code, metered, and the response is returned; an unresolved subject is skipped and counted.
- A `Left` from the handler writes no entry: a failed query exposed no data. A cancelled request propagates `OperationCanceledException`; the audit write itself uses `CancellationToken.None`, as `AuditPipelineBehavior` does, so a completed read is never left unrecorded because the caller went away.
- An exception from `GetDataSubjects` is caught, mapped to `read_audit.subject_unresolved` and logged with `ForLogging()` (exception type and stack trace, never the message).

**Class:** Settled
**Settled by:** #1193 plan, Design Choice 4 (maintainer decision OD-4, settled 2026-09-23); SPEC-002 REQ-016, REQ-019 and DEC-006; AGENTS.md section 3 ("Errors are NEVER swallowed in background infrastructure", "Compliance and security gates fail closed")

</details>

<details>
<summary><strong>5. Registration Surface — who registers the behavior</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate opt-in call `AddEncinaQueryReadAuditing()`** | Explicit; an application that audits only repository reads pays nothing | Easy to forget: a query that implements the marker is silently not audited if the call is missing (fail open by omission) |
| **B) `AddEncinaReadAuditing` registers the open-generic behavior; the marker interface on a query is the opt-in** | One call; a query that declares subjects is audited whenever read auditing is on; no silent gap | Every request resolves the behavior (a type check per request, then pass-through) in applications that use read auditing |

### Chosen Option: **B — `AddEncinaReadAuditing` registers the behavior (recommended, pending the maintainer)**

### Rationale

- Scenario in which A wins: an application that uses repository read audit only and measures the cost of one extra pass-through behavior per request on a hot path.
- Scenario in which B wins: the reference application declares `IExposesDataSubjects` on its Dapper query and registers read auditing once; with A, forgetting a second call would drop the audit with no error, which is the failure the compliance rules forbid.
- Recommendation B: the per-request cost is one cached type check, and the registration is complete by construction (AGENTS.md section 3, registration completeness; a DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`).

**Class:** Dilemma
**Settled by:** none; awaits the maintainer

</details>

<details>
<summary><strong>6. Subject-Id Conversion and Subject Protection — shared with P-05, no new rules</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Consume the shared `IDataSubjectIdConverter` of core `Encina` (#1193 OD-3) and pass the converted id to the same entry-building path as the repository decorator (keyed hash per #1193 OD-5)** | A subject id reads the same in DSR, consent, repository audit and query audit; one place to change | This plan cannot be implemented before #1193 lands the converter |
| **B) Convert with a private helper in this behavior** | Independent of #1193 | A fourth set of conversion rules, which is how #1149 happened |

### Chosen Option: **A — shared converter and shared entry path**

### Rationale

- The converter's exact contract is fixed when #1193 implements it; this plan consumes it and treats "no value" as `read_audit.subject_unresolved`.
- The behavior never hashes or stores the plaintext subject itself: it hands the converted id to the entry-building code shared with the repository decorator, so the keyed-hash rule is applied in one place.
- No subject id, request payload or response body reaches a log, an activity tag or a metric tag (REQ-062).

**Class:** Settled
**Settled by:** #1193 plan, Design Choice 3 (OD-3) and Design Choice 7 (OD-5), maintainer decisions settled 2026-09-23; SPEC-002 REQ-016 (subject ids of any type)

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Security.Audit/`

1. **`Abstractions/IExposesDataSubjects.cs`** (new) — `public interface IExposesDataSubjects<in TResponse>` with `string DataCategory { get; }` and `IEnumerable<object?> GetDataSubjects(TResponse response)`; XML docs with the `GetAgendaQuery` example of the issue, the fail-closed rule (a `null` element or a throw is an error) and "valid on any `IRequest<TResponse>`, intended for queries"
2. **`ReadAuditErrors.cs`** (modify) — only if #1193 has not added them: `read_audit.subject_unresolved` (details: request type, never the subject), reuse `read_audit.audit_write_failed` and `read_audit.purpose_required` from #1193
3. **`PublicAPI.Unshipped.txt`** (modify) — the interface and its members

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #1194 (SPEC-002 P-06, REQ-008). Package src/Encina.Security.Audit. #1193 (P-05) has landed or is on the
  same branch: ReadAuditEntry has DataSubjectId, DataCategory, ReadOperationId; IReadAuditStore has
  LogReadsAsync; ReadAuditFailureMode and PurposeEnforcement exist; core Encina has IDataSubjectIdConverter.
- Pre-1.0: no [Obsolete], no compatibility layers.

TASK:
Add IExposesDataSubjects<in TResponse> (DataCategory, GetDataSubjects returning IEnumerable<object?>) with full
XML docs, the read_audit.subject_unresolved error if missing, and the PublicAPI.Unshipped.txt lines.

KEY RULES:
- The interface carries no reflection and no attribute.
- Errors carry a code and the request type, never a subject id or a message built from data.
- XML docs on every public member, with an example; nullable annotations exact (object?).

REFERENCE FILES:
- src/Encina.Security.Audit/ReadAuditErrors.cs
- src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs
- src/Encina/Abstractions/CqrsContracts.cs (IQuery<TResponse>)
```

</details>

---

### Phase 2: Pipeline Behavior — `QueryReadAuditPipelineBehavior`

<details>
<summary><strong>Tasks</strong></summary>

1. **`QueryReadAuditPipelineBehavior.cs`** (new, `src/Encina.Security.Audit/`) — `public sealed partial class QueryReadAuditPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>`
   - Constructor: `IServiceScopeFactory`, `IReadAuditContext`, `IOptions<ReadAuditOptions>`, `IDataSubjectIdConverter`, `ILogger<…>`, `TimeProvider? timeProvider = null` (defaults to `TimeProvider.System`)
   - Static per-closed-generic-type flag `IsDeclared = typeof(IExposesDataSubjects<TResponse>).IsAssignableFrom(typeof(TRequest))`; when false, `return await nextStep()` immediately
   - `Handle`: options enabled? -> system-access exclusion (`ExcludeSystemAccess` and `IdentityKind.Service`, as in `AuditedReadOnlyRepository.ShouldAudit`) -> purpose enforcement (reject before `nextStep`, recording the refused attempt per #1193 OD-6) -> `nextStep()` -> on `Left` return unchanged -> on `Right` call `GetDataSubjects` inside try/catch -> convert and de-duplicate -> build the entries -> `WriteAsync` -> reconcile with the failure mode
   - `WriteAsync`: `TransactionScope(Suppress, AsyncFlowOption.Enabled)`, `CreateAsyncScope()`, resolve `IReadAuditStore`, `LogReadsAsync(entries, CancellationToken.None)`, catch provider exceptions into `Left(read_audit.audit_write_failed)`
2. **Entry building** — shared with the repository decorator through the entry-building helper that #1193 introduces; if it does not exist when this plan is implemented, extract it there, never duplicate it here

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Behavior pattern to copy: src/Encina.Security.Audit/AuditPipelineBehavior.cs (static per-type cache, scoped
  store through IServiceScopeFactory, TransactionScope.Suppress, Reconcile by failure mode, LoggerMessage).
- Handle signature: ValueTask<Either<EncinaError, TResponse>> Handle(TRequest, IRequestContext,
  RequestHandlerCallback<TResponse>, CancellationToken).
- IReadAuditStore.LogReadsAsync(IReadOnlyList<ReadAuditEntry>, CancellationToken) comes from #1193.

TASK:
Implement QueryReadAuditPipelineBehavior<TRequest,TResponse> as described in the Phase 2 tasks.

KEY RULES:
- Await the audit write in every mode; never "_ = task".
- Fail closed by default: Left(read_audit.audit_write_failed) or Left(read_audit.subject_unresolved), the
  response is not returned. Fail open only through the logged opt-out of ReadAuditOptions.
- A null subject element, a conversion failure or an exception from GetDataSubjects is subject_unresolved;
  no fallback to the caller's UserId.
- OperationCanceledException from the handler propagates; the audit write uses CancellationToken.None.
- Time from TimeProvider; never DateTime.UtcNow.
- No subject id, payload or response body in logs, tags or errors; log exceptions with ForLogging().
- Error.Message never reaches logs.

REFERENCE FILES:
- src/Encina.Security.Audit/AuditPipelineBehavior.cs
- src/Encina.Security.Audit/AuditedReadOnlyRepository.cs (ShouldAudit, entry building)
- src/Encina.Security.Audit/ReadAuditErrors.cs
```

</details>

---

### Phase 3: Configuration, DI and Auto-Registration

<details>
<summary><strong>Tasks</strong></summary>

1. **`ServiceCollectionExtensions.AddEncinaReadAuditing`** (modify) — `services.AddScoped(typeof(IPipelineBehavior<,>), typeof(QueryReadAuditPipelineBehavior<,>))` (pending Design Choice 5); `IDataSubjectIdConverter` is registered by #1193 (`TryAdd`), and a missing one fails the DI test
2. **Start-up validation** — nothing new: the `ReadAuditStartupValidator` of #1193 already rejects fail-closed with the in-memory store outside `Development`, which covers query audit
3. **DI test** — builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves `IPipelineBehavior<GetAgendaQuery, …>` (AGENTS.md section 3, registration completeness)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- AddEncinaReadAuditing in src/Encina.Security.Audit/ServiceCollectionExtensions.cs registers options, a
  TryAdd InMemoryReadAuditStore and a scoped IReadAuditContext. AddEncina (operation audit) registers
  AuditPipelineBehavior as AddScoped(typeof(IPipelineBehavior<,>), ...).

TASK:
Register the open-generic QueryReadAuditPipelineBehavior and add the DI completeness test.

KEY RULES:
- Registration completeness: every option type and dependency the behavior resolves is registered; the test
  builds the provider with ValidateOnBuild = true and ValidateScopes = true.
- A database store wins over the in-memory default in any registration order (the Replace change of #1193).

REFERENCE FILES:
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Security/Audit/AuditPipelineBehaviorRegistrationTests.cs
```

</details>

---

### Phase 4: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy** — entries take `TenantId` from `IRequestContext`; with tenancy off the value is the fixed default tenant of REQ-061; the per-subject query of #1193 is tenant-scoped; the two-tenant test is in Phase 6
2. **Purpose** — `IReadAuditContext.Purpose` is copied to each entry; `PurposeEnforcement.Reject` rejects before the handler runs
3. **Validation** — a declared subject that cannot be read returns the coded error (Design Choice 4)
4. **Audit trail** — the entries are the audit trail; a query also remains subject to `AuditPipelineBehavior` when the application audits queries, giving one operation entry and N read entries
5. **Evaluated and not integrated** — caching, health checks, resilience, locks, transactions, idempotency, module isolation (see the matrix)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- IRequestContext exposes TenantId, CorrelationId and Identity (UserId, Kind). IReadAuditContext exposes
  Purpose and Metadata. Tenancy filtering of the other audit queries is #798.

TASK:
Make sure the behavior carries tenant, correlation, user and purpose into every entry, and that
PurposeEnforcement.Reject and the system-access exclusion behave as in the repository decorator.

KEY RULES:
- Tenant is never taken from the request body.
- A fixed default tenant applies when tenancy is off; no tenant configuration is needed.
- A read refused by purpose enforcement records an entry of the attempt (no data read), per #1193 OD-6.

REFERENCE FILES:
- src/Encina/Abstractions/IRequestContext.cs
- src/Encina.Security.Audit/Abstractions/IReadAuditContext.cs
- docs/plans/evidential-read-audit-implementation-plan-1193.md (Design Choices 4, 5)
```

</details>

---

### Phase 5: Observability

<details>
<summary><strong>Tasks</strong></summary>

The names in the issue (`encina.read_audit.query_entries`, `encina.read_audit.query`) were proposals. The plan follows the package's existing names so that repository and query audit appear in the same series.

1. **`Diagnostics/ReadAuditMeter.cs`** (modify) — no new counter for entries: `read_audit.entries_logged.total` already has the tags `entity_type` and `access_method`; query entries use `access_method = DirectQuery`, `entity_type` = the request type name, and the tags `data_category` and `encina.tenant_id` added by #1193. New counters: `read_audit.subject_unresolved.total` (tag `entity_type`), and reuse `read_audit.log_failures.total`
2. **`Diagnostics/ReadAuditActivitySource.cs`** (modify) — the `ReadAudit.Write` activity of #1193 is started by the behavior with `read_audit.access_method = DirectQuery`, `read_audit.entity_type`, `read_audit.entity_count`, `encina.tenant_id`; never a subject id
3. **`Diagnostics/ReadAuditLog.cs`** (modify) — new `[LoggerMessage]` in the registered range `SecurityAuditRead` (1700-1799), packed after the ids #1193 reserves (1739-1749): `QueryReadsRecorded` (1750, Debug), `QueryAuditWriteFailed` (1751, Error, fail closed), `QueryAuditWriteFailedOpen` (1752, Warning), `QuerySubjectUnresolved` (1753, Warning), `QueryPurposeRejected` (1754, Warning), `QueryDeclarationThrew` (1755, Error, exception through `ForLogging()`); messages carry the request type and the error code, never a subject or a payload
4. **`EventIdRanges.cs` and `EncinaEventIdAllocationTests`** — no change: `SecurityAuditRead` is registered and `Encina.Security.Audit` is mapped to it
5. Health checks: none (no new dependency; the store health check of #1193 covers the store)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- ActivitySource and Meter are both named "Encina.ReadAudit". EventIds 1700-1799 are SecurityAuditRead
  (registered); 1700-1702, 1710-1712, 1720-1722, 1730-1738 are used today; #1193 reserves 1739-1749.

TASK:
Add the query counters, the activity tags and the [LoggerMessage] entries 1750-1755 listed in the Phase 5 tasks.

KEY RULES:
- Pack EventIds sequentially from 1750; stay inside 1700-1799; use the [LoggerMessage] generator.
- XML docs name the range ("Event IDs: 1750-1755 (see EventIdRanges.SecurityAuditRead)").
- No subject ids, response bodies or purposes in tags or messages; tenant as "encina.tenant_id".
- Run the architecture test EncinaEventIdAllocationTests.

REFERENCE FILES:
- src/Encina.Security.Audit/Diagnostics/ReadAuditMeter.cs, ReadAuditActivitySource.cs, ReadAuditLog.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 6: Testing

<details>
<summary><strong>Tasks</strong></summary>

Manifest first: `.github/coverage-manifest/Encina.Security.Audit.json` gets per-file targets for `QueryReadAuditPipelineBehavior.cs` and `Abstractions/IExposesDataSubjects.cs` with a one-sentence justification per flag (the interface has no executable lines: unit target 0).

1. **UnitTests** (`tests/Encina.UnitTests/Security/Audit/`) — `QueryReadAuditPipelineBehaviorTests`: undeclared query passes through untouched; one entry per distinct subject with tenant, user, correlation, purpose and `DirectQuery`; duplicates collapse with the right `EntityCount`; empty response writes the zero entry; a `Left` from the handler writes nothing; fail closed on store `Left` and on a throwing store; fail open logs and returns; a `null` subject, a conversion failure and a throwing `GetDataSubjects` return `subject_unresolved`; `Reject` stops before the handler; cancellation propagates; `FakeTimeProvider` for timestamps; telemetry test with an in-memory exporter (activity and counter carry the tenant tag, no subject id anywhere in tags, metrics or captured logs); DI test with `ValidateOnBuild`/`ValidateScopes`
2. **GuardTests** (`tests/Encina.GuardTests/Security/Audit/`) — constructor and `Handle` null arguments
3. **ContractTests** (`tests/Encina.ContractTests/Security/Audit/`) — the declaration contract: real implementations of `IExposesDataSubjects<T>` honoured identically for `string`, `Guid`, strongly-typed and `int` ids (same output as the DSR extractor rules)
4. **PropertyTests** (`tests/Encina.PropertyTests/Security/Audit/`) — FsCheck: for any list of subjects the number of entries equals the number of distinct converted subjects, and the sum of `EntityCount` equals the number of non-null elements
5. **IntegrationTests** (`tests/Encina.IntegrationTests/Security/Audit/ReadAudit/`) — `[Collection("Dapper-PostgreSQL")]` S13 test: a Dapper query handler declares its subjects, `AddEncinaReadAuditing` with the Dapper PostgreSQL store, one entry per patient is stored, "who accessed patient X" returns it, and with fail-closed on a failing store fails the read; a two-tenant test (tenant A never sees tenant B's entries); one equivalent test per remaining family (ADO.NET SqlServer, EF Core MySQL, MongoDB) because the behavior is provider-independent and the 10-provider store matrix is exercised by #1193
6. **LoadTests / BenchmarkTests** — `.md` justifications under `tests/Encina.LoadTests/Security/Audit/ReadAudit/` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Security/Audit/ReadAudit/` (the existing `ReadAuditLoadTests.md` and `ReadAuditBenchmarks.md` are updated; the behavior is not a concurrent hot path and its cost is one cached type check plus the batch write)
7. **Split** — PR A: Phases 1-5 plus unit and guard tests; PR B: contract, property and integration tests, justification updates, documentation (Phase 7)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Test projects are consolidated under tests/ (UnitTests, GuardTests, ContractTests, PropertyTests,
  IntegrationTests). Assertions use Shouldly through Encina.Testing.Shouldly. Integration tests use shared
  [Collection("Dapper-PostgreSQL")] fixtures and ClearAllDataAsync in InitializeAsync.
- Existing neighbours: tests/Encina.UnitTests/Security/Audit/AuditPipelineBehaviorTests.cs and
  tests/Encina.IntegrationTests/Security/Audit/ReadAudit/ReadAuditStoreDapperPostgreSqlIntegrationTests.cs.

TASK:
Implement the tests listed in Phase 6 and the manifest targets for the new files, then measure per-flag coverage.

KEY RULES:
- Tests execute real package code; no reflection-only tests.
- Never FluentAssertions; builders for test data; no Thread.Sleep; FakeTimeProvider for time.
- One behavior per test, Arrange-Act-Assert, deterministic.
- CRAP <= 10 on the new methods: keep Handle small by extracting resolve, build and write steps.
- Measure with: dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory
  artifacts\coverage\<Flag>Tests, then the coverage-report script (Per-file targets table).

REFERENCE FILES:
- .github/coverage-manifest/Encina.Security.Audit.json
- tests/Encina.UnitTests/Security/Audit/AuditPipelineBehaviorTests.cs
- tests/Encina.IntegrationTests/Security/Audit/ReadAudit/
```

</details>

---

### Phase 7: Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on all new public APIs (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`)
2. `changelog.d/1194-query-read-audit.added.md` — one bullet for `IExposesDataSubjects<TResponse>` and the query read-audit behavior
3. `src/Encina.Security.Audit/README.md` — section "Auditing reads outside repositories"
4. `docs/features/read-auditing.md` — query-level audit, the declaration example, failure mode and the S13 flow
5. `docs/INVENTORY.md` — new files
6. `PublicAPI.Unshipped.txt` — complete for every new public symbol
7. ROADMAP.md and `docs/releases/v0.17.0/README.md` — only if the milestone text names this item
8. ADR: not needed (the decisions follow the ADR-less choices of #1193; the choice of the typed interface is recorded on the issue). ADR number assigned when the ADR pull request opens, if the maintainer asks for one
9. Build verification: `dotnet build --configuration Release` with 0 warnings; `dotnet test` green; every coverage flag reaches its target in the manifest; no hand-typed coverage figures in documentation (cite `cov:` markers)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- Documentation rules: .claude/skills/encina-docs/SKILL.md. The package README and docs/features/read-auditing.md
  describe repository read audit today. Changelog fragments follow changelog.d/README.md.

TASK:
Document query-level read audit: the interface, a worked Dapper example (the appointment list), the failure
mode, purpose and tenant behavior, and how to query "who accessed subject X" (the #1193 query service).

KEY RULES:
- English only; no private client application named (write "the reference application").
- Never type coverage figures; cite cov: markers.
- Never edit CHANGELOG.md; add the fragment.

REFERENCE FILES:
- docs/features/read-auditing.md
- src/Encina.Security.Audit/README.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| GDPR | Art. 15, Art. 5(2), Art. 30, Art. 32 | Right of access (who accessed the data), accountability, records, security of processing |
| AEPD | PD-00068-2026 (13 July 2026) | A patient may learn who accessed their data, when and which data [S] |
| EHDS | Reg. (EU) 2025/327 Art. 9 | Access information from 26 March 2029 [S] |
| SPEC-002 | REQ-007, REQ-008, REQ-016, REQ-019, AC-008, S13, REQ-061, REQ-062 | The requirement this plan implements |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `AuditPipelineBehavior<,>` | `src/Encina.Security.Audit/AuditPipelineBehavior.cs` | Pattern for the new behavior (static per-type cache, scoped store, transaction suppression) |
| `IReadAuditStore`, `ReadAuditEntry`, `ReadAccessMethod.DirectQuery` | `src/Encina.Security.Audit/` | The store contract and entry shape; `DirectQuery` already exists |
| `LogReadsAsync`, `ReadAuditFailureMode`, `PurposeEnforcement`, `IDataSubjectIdConverter` | #1193 (P-05) | Batch write, failure mode, purpose enforcement, subject-id conversion |
| `IReadAuditContext` | `src/Encina.Security.Audit/Abstractions/IReadAuditContext.cs` | Declared purpose and metadata |
| `IRequestContext` | `src/Encina/Abstractions/IRequestContext.cs` | Tenant, correlation, caller identity |
| `ReadAuditMeter`, `ReadAuditActivitySource`, `ReadAuditLog` | `src/Encina.Security.Audit/Diagnostics/` | Telemetry and logging (range `SecurityAuditRead`) |
| Dapper PostgreSQL read-audit store and fixtures | `src/Encina.Dapper.PostgreSQL/Auditing/`, `tests/Encina.IntegrationTests/Dapper/PostgreSQL` | Integration test S13 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.Audit` (read audit) | 1700-1799 (`SecurityAuditRead`, registered) | This plan: 1750-1755, after the ids #1193 reserves (1739-1749); no new range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | ~6 | interface, behavior, error, meter, activity source, log, DI (edits count as one each) |
| Tests | ~10 | unit, guard, contract, property, integration (Dapper PostgreSQL plus three families), DI test |
| Justification updates | 2 | load and benchmark `.md` |
| Manifest, PublicAPI, changelog | 3 | coverage manifest, `PublicAPI.Unshipped.txt`, `changelog.d` fragment |
| Documentation | ~4 | README, feature page, INVENTORY, release notes |
| **Total** | **~25** | two pull requests (PR A about 13 files, PR B about 12) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1194 (SPEC-002 P-06): query-level read audit in Encina.Security.Audit.

PROJECT CONTEXT:
- .NET 10 / C# 14, ROP (Either<EncinaError, T>), pre-1.0 (breaking changes preferred, no [Obsolete]).
- Depends on #1193 (P-05): ReadAuditEntry with DataSubjectId/DataCategory/ReadOperationId, IReadAuditStore.LogReadsAsync,
  ReadAuditFailureMode (default FailClosed), PurposeEnforcement, IDataSubjectIdConverter in core Encina.
- No provider-specific code: the 10 database providers and Marten are covered by the store of #1193.

IMPLEMENTATION OVERVIEW:
Phase 1: IExposesDataSubjects<in TResponse> (DataCategory, GetDataSubjects -> IEnumerable<object?>), subject_unresolved error
Phase 2: QueryReadAuditPipelineBehavior<TRequest,TResponse>: purpose check -> handler -> resolve subjects -> one entry per
         distinct subject in one LogReadsAsync batch -> fail closed or open per ReadAuditOptions
Phase 3: register the open-generic behavior in AddEncinaReadAuditing (pending maintainer decision 5); DI completeness test
Phase 4: tenant, correlation, user, purpose in every entry; Reject before the handler
Phase 5: Encina.ReadAudit meter/activity reuse; EventIds 1750-1755 in SecurityAuditRead
Phase 6: unit, guard, contract, property, integration (Dapper PostgreSQL S13 + three families + two tenants); manifest targets
Phase 7: docs, README, changelog fragment, PublicAPI

KEY PATTERNS:
- Typed marker interface, not attributes or reflection over the response; no silent fallback to the caller as subject.
- Await the audit write; never fire-and-forget; fail closed by default (DEC-006).
- No subject ids, payloads or Error.Message in logs, tags or metrics; exceptions through ForLogging().
- TimeProvider for time; static per-closed-generic-type decisions; CRAP <= 10 on every new method.
- Tests execute real code; Shouldly via Encina.Testing.Shouldly; shared [Collection] fixtures.

REFERENCE FILES:
- src/Encina.Security.Audit/AuditPipelineBehavior.cs
- src/Encina.Security.Audit/AuditedReadOnlyRepository.cs
- src/Encina.Security.Audit/Diagnostics/ReadAuditLog.cs
- docs/plans/evidential-read-audit-implementation-plan-1193.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-008, AC-008, S13)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path that benefits from caching: audit writes are never cached, and a cached query result would still need its access recorded |
| 2 | OpenTelemetry | ✅ | `ReadAudit.Write` activity with `DirectQuery`, existing counters plus `read_audit.subject_unresolved.total`, `encina.tenant_id` tag, no subject ids (Phase 5) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 1750-1755 in the registered `SecurityAuditRead` range (ADR-021) |
| 4 | Health Checks | ❌ | No new checkable dependency; the store health check of #1193 covers `IReadAuditStore` |
| 5 | Validation | ✅ | A declared subject that is `null`, unconvertible or whose declaration throws returns `read_audit.subject_unresolved` (fail closed, REQ-016) |
| 6 | Resilience | ❌ | Local store write; fail closed is the explicit behavior on failure and retrying inside a read only adds latency |
| 7 | Distributed Locks | ❌ | Append-only entries per request; no shared state updated by concurrent hosts |
| 8 | Transactions | ❌ | The batch is written atomically by the store; the audit write is deliberately outside the application's transaction (`TransactionScope.Suppress`) so an audited read is recorded even if the caller later rolls back |
| 9 | Idempotency | ❌ | Not a message entry point; each query execution is a distinct access and must be recorded as such |
| 10 | Multi-Tenancy | ✅ | REQ-061/AC-043: entries carry the tenant from `IRequestContext`; the per-subject query of #1193 is tenant-scoped; two-tenant test; a fixed default tenant when tenancy is off |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | This feature writes read-audit entries; the operation audit of `AuditPipelineBehavior` is unchanged |

---

## Prerequisites & Dependencies

| Item | Why | State |
|------|-----|-------|
| #1193 (P-05) entry shape, `LogReadsAsync`, `ReadAuditFailureMode`, `PurposeEnforcement` | The behavior writes through them | Open, plan merged; implement #1193 first or on the same branch |
| `IDataSubjectIdConverter` in core `Encina` (#1193 OD-3) | Shared subject-id conversion | Created by #1193 |
| Keyed-hash storage of the subject id (#1193 OD-5) | The behavior hands a plain converted id to the shared entry path | Owned by #1193 |
| Reference scenario S13 (P-25) | End-to-end acceptance | S13 passes when #1193 and this plan have both landed |

---

## Next Steps

1. Land #1193 (or its entry shape and store contract), then implement PR A (Phases 1-5 with unit and guard tests)
2. Implement PR B (contract, property and integration tests, justifications, documentation, changelog fragment)
3. Link this plan from issue #1194; ask the maintainer the Dilemma of Design Choice 5 when work on the issue starts
4. The final commit references `Fixes #1194`
