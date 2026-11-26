# Implementation Plan: Audit trail for cache diagnostics delete operations

> **Issue**: [#4416](https://github.com/dlrivada/Encina/issues/4416)
> **Type**: Feature
> **Complexity**: Low (1 code pull request of 11 files, and a documentation pull request of 2 files)
> **Estimated Scope**: about 150 lines of production code, about 400 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #4416 asks that each administrative delete executed through the cache diagnostics endpoints of #276 (open, plan merged) writes an audit event with the authenticated user, the tenant, the number of keys and the redacted pattern. Re-checked against `origin/main` (326e65ebf):

| Premise of the issue | State in the code and the plans |
|----------------------|---------------------------------|
| "The #276 plan logs a delete by count only (EventId 535)" | The merged #276 plan logs the executed delete with EventId 60351 in `Encina.AspNetCore` (range `AspNetCoreCachingDiagnostics` 60350-60352) and 30350 in `Encina.Caching`; 535 is not used there. This plan leaves both as they are. |
| The endpoint knows the user | The endpoints read `IRequestContext` of `UseEncinaContext()`; the caller is `IRequestContext.Identity` (`RequestIdentity`, `src/Encina/Identity/RequestIdentity.cs`, with `IsAuthenticated` and `UserId`; `src/Encina/Abstractions/IRequestContext.cs`), never null: a request without a caller carries `RequestIdentity.Anonymous`. |
| The audit store is reachable from the endpoint | `Encina.AspNetCore.csproj` already references `Encina.Security.Audit` and `Encina.Caching`. The store is scoped in the database providers (`TryAddScoped<IOperationAuditStore, ...>` in each provider's `ServiceCollectionExtensions.cs`), so the write goes through the recorder of #4414, which opens its own scope. |
| There is one delete operation | #276 has two: `ICacheDiagnostics.DeleteKeysAsync(tenantId, pattern, ct)` (a count) and `DeleteKeyAsync(tenantId, id, ct)` (one key, by opaque id). Both are audited. |
| Patterns are safe to store once redacted | `CacheKeyRedactor` masks the `t:` and `u:` segments and, by its own remarks, leaves application-substituted values (`orders:{CustomerId}`) as they are (`src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs`): a pattern can still hold personal data (Design Choice 3). |
| `AuditDeletes` lives on `AddEncinaCacheDiagnostics` | Yes: `CacheDiagnosticsOptions` (#276 PR 1) is the only options source of the diagnostics registration. |

The plan adds the `AuditDeletes` option, one small internal writer in `Encina.AspNetCore` that builds a `CacheAuditEvent` and hands it to `ICacheAuditRecorder` (owned by #4414), and the start validation. Caching (8): behavior-level registry, identical on all 8 providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277); the audit store keeps its own provider matrix, unchanged.

**Shared pieces and ownership (family cache-patterns-2):** #4414 owns `ICacheAuditRecorder`, `CacheAuditEvent` (including `EntityId`), `CacheAuditEventKind.KeysDeleted`, `CacheAuditSchema` (the `Action` `CacheKeysDeleted`, `EntityType` `CacheEntry` and metadata-key constants `count`, `patternHash`, `pattern`, `scope`), the recorder's mapping of `KeysDeleted` to `OperationAuditEntry`, the `Encina.Caching` reference to `Encina.Security.Audit` and EventId 528 (an audit write failure is logged there, by event kind and code); this plan consumes them and so starts after #4414 PR 1 and edits none of those files. This plan owns the one endpoint identity helper `src/Encina.AspNetCore/CacheDiagnosticsIdentity.cs` (the code `cache.diagnostics_identity_required`), which #4415 reuses; the order #4414, #4416, #4415 is therefore fixed. #276 owns the endpoints and `CacheDiagnosticsOptions`; this plan modifies them after #276 PR 3. This plan allocates **no EventId**: failures are logged by #4414's 528 and the delete count by #276's 60351.

**Affected packages:** `Encina.AspNetCore` (endpoints), `Encina.Caching` (option and validator). **Provider category:** Caching (8), behavior-level.

---

## Design Choices

<details>
<summary><strong>1. What one audit event covers (Dilemma D1, the issue's D1 to D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One event per delete request, with the count** | Proportionate; one row answers who cleared what and when | The individual keys are not listed |
| **B) One event per removed key** | Complete; wins under a regulation naming records | A pattern delete can write up to `MaxDeleteBatch` (default 1,000, #276) rows |
| **C) Log only (the state of #276)** | No audit store needed | No evidence of an administrative action |

### Chosen Option: **A - one event per request** (recommended, pending the maintainer)

### Rationale

- Option B wins when a regulation requires each removed record to be named; C wins when no audit store is configured. A delete that matched nothing still writes an event with count 0: the administrative action happened (the acceptance criterion of #4416 speaks of a successful delete, and a delete of zero keys is successful).
- Class: Dilemma

</details>

<details>
<summary><strong>2. When the event is written and what an audit failure does (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) After a successful delete, best effort: a failing store is logged by code (EventId 528) and the delete answer is unchanged** | A cache cleanup during an incident never depends on the audit store; the text of the issue | A delete can be unaudited when the store fails |
| **B) Before the delete (write-ahead), fail closed: when the write fails the delete does not run and the endpoint answers `cache.diagnostics_audit_failed` (ADR-062 `ErrorCategory.Internal`, rule R4: a store failure is `Internal`; constant message "The cache audit event could not be written.")** | No administrative action without evidence, the rule `ABACPipelineBehavior` follows (#751) and the default of `OperationAuditFailureMode.FailClosed` | An audit outage blocks cache administration; the count is the number of registry keys matched at that moment, not of keys removed |

### Chosen Option: **A - after, best effort** (recommended, pending the maintainer)

### Rationale

- Option B wins under a compliance regime that requires evidence before any administrative action. A cache delete is idempotent and rebuildable, which is why A is recommended. The error code exists only if B is chosen.
- Class: Dilemma

</details>

<details>
<summary><strong>3. What the event records about the pattern (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The pattern redacted by `CacheKeyRedactor.Redact(pattern, tenantId, userId)`, at most 200 characters (the #276 limit), in the metadata key `pattern`** | An auditor reads what was cleared | A template-substituted value in a pattern (the redactor leaves `orders:{CustomerId}` as typed) is stored as plaintext in an audit row |
| **B) Only a SHA-256 prefix of the pattern (16 hex characters) in the metadata key `patternHash`** | No pattern text, so no personal data in the audit store | An auditor cannot read the pattern, only compare it with a known one |

### Chosen Option: **B - hash only** (recommended, pending the maintainer)

### Rationale

- The reference application caches health-related results, and the redactor itself says application-substituted values are not masked, so storing the text would put possible personal data into plaintext audit rows. Option A wins when keys and patterns are opaque by policy and auditors need to read what was cleared. A single-key delete records the opaque id of #276, never the key. The audit store is plaintext storage, so neither option stores the raw key or a value.
- Class: Dilemma

</details>

<details>
<summary><strong>4. A missing caller identity denies the delete when auditing is on</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) With `AuditDeletes` on, a request whose `Identity` is not authenticated is rejected with `cache.diagnostics_identity_required` (ADR-062 `ErrorCategory.Unauthenticated`, constant message "A caller identity is required.") before any delete** | An unattributed administrative delete never happens | A mis-ordered middleware (no `UseEncinaContext()`) fails visibly |
| **B) Record the event with a null user** | The delete proceeds | Evidence without an actor |

### Chosen Option: **A - deny**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 ("Compliance and security gates fail closed: when context is missing (no `HttpContext`, tenant or principal) ... the gate denies") and ADR-062 rule R6 (no identity is `Unauthenticated`). Under multi-tenancy the existing denial `cache.diagnostics_tenant_required` of #276 still comes first.

</details>

<details>
<summary><strong>5. The sink and the event mapping</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IOperationAuditStore` through #4414's recorder: the event is `KeysDeleted`, mapped by #4414 PR 1 (`Action` `CacheKeysDeleted`, `EntityType` `CacheEntry`, `EntityId` the opaque id for a single-key delete and null for a pattern, `UserId` from `Identity.UserId`, `TenantId` the tenant when multi-tenancy is on, `Metadata` with `count`, `scope` (`pattern` or `key`) and the pattern text or hash of Design Choice 3)** | The documented sink for administrative evidence; filters by action and tenant exist | None |
| **B) A new cache audit store** | Own schema | A fourth audit store and ten provider implementations |

### Chosen Option: **A - operation store**

### Rationale

- **Class:** Settled
- **Settled by:** ADR-036 decision 2 (the operation store is the general write sink, distinguished by `Action` and `Metadata`) and AGENTS.md section 5 (a new store would need all 10 database providers). The names are #4414's, which confirms them with the maintainer.

</details>

<details>
<summary><strong>6. Opt-in and registration (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AuditDeletes` is false by default; the application calls `AddEncinaCacheAudit()` itself; an `IValidateOptions<CacheDiagnosticsOptions>` (an internal nested type in `CacheDiagnosticsServiceCollectionExtensions.cs`, bound with `ValidateOnStart`) fails the host when `AuditDeletes` is on and no `ICacheAuditRecorder` descriptor exists (`IServiceProviderIsService`, nothing is resolved at start)** | An audit option never silently does nothing; diagnostics users without audit never load the audit registration | Two registration calls |
| **B) `AddEncinaCacheDiagnostics` always registers the recorder with `TryAdd`, and the missing store surfaces when `AuditDeletes` is on** | One call | Every diagnostics user takes the audit assembly into the container, and a missing store is found at the first write, not at start |

### Chosen Option: **A - explicit call and start validation** (recommended, pending the maintainer)

### Rationale

- Option B wins for teams that want a single registration call and accept a late failure. Neither AGENTS.md nor ADR-036 mandates a start validator, so this stays a Dilemma; the choice is independent of #4414's own store-presence validator (A holds with or without it).
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: Option, writer, endpoint hooks and validation (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 needs #4414 PR 1 and #276 PR 3 merged, and holds exactly these 11 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.AspNetCore/CacheDiagnosticsEndpointExtensions.cs` | modify (the two delete handlers call the identity helper and the writer; each call is one private method, so the handlers keep CRAP 10 or below) |
| 2 | `src/Encina.AspNetCore/CacheDeleteAuditWriter.cs` | new (internal static) |
| 3 | `src/Encina.AspNetCore/CacheDiagnosticsIdentity.cs` | new (internal static: `RequireAuthenticated(IRequestContext)`, the one owner of `cache.diagnostics_identity_required`, reused by #4415) |
| 4 | `src/Encina.Caching/Configuration/CacheDiagnosticsOptions.cs` | modify (`AuditDeletes`, default false) |
| 5 | `src/Encina.Caching/CacheDiagnosticsServiceCollectionExtensions.cs` | modify (the start validator of Design Choice 6) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the property) |
| 7 | `tests/Encina.UnitTests/AspNetCore/CacheDeleteAuditTests.cs` | new (test server with the in-memory operation store; includes the validator and DI assertions) |
| 8 | `tests/Encina.GuardTests/AspNetCore/CacheDeleteAuditWriterGuardTests.cs` | new (the writer and the identity helper are reachable from the guard project) |
| 9 | `.github/coverage-manifest/Encina.AspNetCore.json` | modify (`mechanical-fixer`) |
| 10 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 11 | `changelog.d/4416-cache-diagnostics-delete-audit.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `CacheDeleteAuditWriter.WriteAsync(ICacheAuditRecorder? recorder, IRequestContext context, CacheDeleteAuditInput input, CancellationToken ct)`: a no-op when the option is off; builds a `CacheAuditEvent` of kind `KeysDeleted` (Design Choice 5 mapping; `EntityId` the opaque id or null; metadata `count`, `scope` and the pattern text or hash of Design Choice 3, using #4414's constants) with the pattern handled per Design Choice 3, calls the recorder, and on a `Left` returns it to the caller, which logs nothing itself (the recorder logged 528) and keeps the delete answer (Design Choice 2, option A). A cancelled token propagates as `OperationCanceledException`, never as a `Left`.
2. **Modify** the handlers of `DELETE {prefix}/keys/{id}` and `DELETE {prefix}/keys?pattern=`: with `AuditDeletes` on, call `CacheDiagnosticsIdentity.RequireAuthenticated(context)` first (Design Choice 4), run the delete through `ICacheDiagnostics` (which returns only the count, `Either<EncinaError, int>`, and is not changed here, so no truncation flag is recorded), and on `Right` write the event with the count (1 for the single key).
3. **Modify** the options and the registration: `AuditDeletes` and the `IValidateOptions` validator of Design Choice 6 that fails at host start when it is on and no `ICacheAuditRecorder` descriptor exists.
4. **Tests:** a pattern delete writes exactly one event with the caller's user id, the tenant, the count and the redacted pattern; a single-key delete writes one event with count 1 and the opaque id, never the key; tenant A's delete event carries tenant A; a delete matching nothing writes count 0; a failing recorder leaves the delete answer unchanged and the failure is logged once (528 through the recorder); an anonymous identity is rejected before any delete and writes nothing; the option off writes nothing and the endpoints behave as in #276; the raw key and any value never appear in the event; cancellation propagates; the validator fails without a recorder and passes with one; DI is built with `ValidateOnBuild` and `ValidateScopes`. Guard tests (`Encina.AspNetCore` grants `InternalsVisibleTo` to `Encina.GuardTests`, `src/Encina.AspNetCore/Properties/AssemblyInfo.cs:7`, so the guard project can call the internal writer and helper): a null request context throws `ArgumentNullException` in both; a null recorder is the "audit off" state and gets a valid-result test (nothing is written, no exception); a null input throws. `AuditDeletes` is a boolean property with no guard.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4416 audits the delete endpoints of the cache diagnostics (#276). #4414 PR 1 created
ICacheAuditRecorder, CacheAuditEvent and CacheAuditSchema in Encina.Caching; Encina.AspNetCore references Encina.Security.Audit and Encina.Caching.

TASK:
1. Add CacheDiagnosticsOptions.AuditDeletes and its start validator; create CacheDeleteAuditWriter; hook the two delete handlers.
2. Write the unit tests with a test server and the in-memory operation store; ask mechanical-fixer for PublicAPI, both manifests and the fragment.

KEY RULES:
- Fail closed: with AuditDeletes on an unauthenticated identity deletes nothing (ErrorCategory.Unauthenticated, constant message).
- Cancellation propagates; an audit failure is logged by code through the recorder and never changes the delete answer (as answered in your brief for Design Choice 2).
- The event holds the opaque id and the pattern text or hash (Design Choice 3), never a raw key, a value or EncinaError.Message.
- No new EventId; no new public type in Encina.AspNetCore.

REFERENCE FILES:
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- src/Encina/Abstractions/IRequestContext.cs
- src/Encina.Security.Audit/InMemoryOperationAuditStore.cs
```

</details>

### Phase 2: Documentation (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 2 files, written by `docs-writer`: `docs/features/cache-diagnostics.md` (the page #276 creates; an "Auditing deletes" section: the option, the recorder prerequisite, what an event holds, that the key and values are never stored) and `src/Encina.AspNetCore/README.md` (one line). XML documentation with an `<example>` on `AuditDeletes` is part of PR 1.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged.

TASK:
Ask docs-writer for the section and the README line; verify links and lint.

KEY RULES:
- No hand-typed figures; cite tests by name; one Diataxis quadrant per page.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/features/cache-diagnostics.md (after #276)
```

</details>

### Phase 3: Cross-cutting integration, observability and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds:** none are allocated. A failed audit write is logged by #4414's EventId 528 (`CachingTenantAudit`, kind and code only); the delete count log stays #276's 60351.
2. **Error codes:** `cache.diagnostics_identity_required` (`Unauthenticated`, constant message, always when the option is on); `cache.diagnostics_audit_failed` (`Internal`, ADR-062 R4, constant message) only if the maintainer answers option B of Design Choice 2. The endpoints map them through the existing `ProblemDetailsExtensions`.
3. **Coverage targets (per flag, set in the PR that adds each file; measured values go in the PR body):**

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.AspNetCore/CacheDeleteAuditWriter.cs` | 100 | 25 | 0 | 0 | Small internal static class driven through the test server; the guard flag reaches the null checks and the audit-off path in `CacheDeleteAuditWriterGuardTests.cs` (reachable: `Encina.AspNetCore` grants `InternalsVisibleTo` to the guard project) |
| `Encina.AspNetCore/CacheDiagnosticsIdentity.cs` | 100 | 25 | 0 | 0 | One short method with a null check, unit and guard tested |
| `Encina.AspNetCore/CacheDiagnosticsEndpointExtensions.cs` | 90 | 70 | 0 | 0 | The numbers the #276 plan sets for this file (unit 90, guard 70; no row exists on `origin/main` until #276 PR 3); this PR never lowers them and the new branches are covered by `CacheDeleteAuditTests.cs` |
| `Encina.Caching/Configuration/CacheDiagnosticsOptions.cs` | 100 | 80 | 0 | 0 | The numbers #276 sets (unit 100, guard 80); one boolean property is added |
| `Encina.Caching/CacheDiagnosticsServiceCollectionExtensions.cs` | 100 | 100 | 0 | 0 | The numbers #276 sets (unit 100, guard 100); the validator is covered by the unit test |

   The worker measures every flag with `coverage-report.cs`; no target is lowered (`target-ratchet`).
4. **Property, contract, integration, load, benchmark:** none: the change is one writer and two hooks; the recorder has its own tests in #4414 and the audit store its own integration tests; a development tool off the request path needs no load test or benchmark.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PR 1 is merged. Verification remains.

TASK:
Verify the per-flag targets and run coverage-report.cs --check-justifications; fix omissions through mechanical-fixer.

KEY RULES:
- Never lower an existing target (target-ratchet); add no log or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.AspNetCore.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Gates fail closed (missing principal); no `EncinaError.Message` in logs or plaintext storage |
| ADR-036 | The operation store as the general write sink |
| ADR-062 | Categories of the two codes (R6: no identity is `Unauthenticated`) |
| ADR-054 D4 | No telemetry is added, so no tenant tag is needed |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `ICacheAuditRecorder` (#4414) | `src/Encina.Caching/Audit/` (after #4414) | The write path |
| `RequestIdentity`, `IRequestContext` | `src/Encina/Identity/RequestIdentity.cs`, `src/Encina/Abstractions/IRequestContext.cs` | Caller and tenant |
| `CacheKeyRedactor` | `src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs` | Pattern redaction |
| Diagnostics endpoints (#276) | `src/Encina.AspNetCore/` (after #276) | The two delete handlers |
| `InMemoryOperationAuditStore` | `src/Encina.Security.Audit/InMemoryOperationAuditStore.cs` | The test store |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | Failures use #4414's 528; the delete count uses #276's 60351 |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 2 | 4 | Writer and identity helper; endpoints, options, DI extension, PublicAPI |
| Tests | 2 | 0 | One unit file, one guard file |
| Documentation | 0 | 2 | Diagnostics page and README |
| Configuration | 1 | 2 | One fragment; two manifests |
| **Total** | **5** | **8** | 2 pull requests of 11 and 2 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4416 writes one audit event per administrative cache delete of the diagnostics
endpoints (#276), through ICacheAuditRecorder (#4414) and IOperationAuditStore.

IMPLEMENTATION OVERVIEW:
1. PR 1: AuditDeletes option and validator, the internal writer, the two handler hooks, the tests.
2. PR 2: documentation by docs-writer.

KEY PATTERNS:
- Fail closed on a missing identity; the event holds user, tenant, count and the redacted pattern or opaque id.
- An audit failure is logged by code and, as answered for Design Choice 2, does not change the delete answer.
- Follow the maintainer's answers to Design Choices 1 to 3, given in your brief.

REFERENCE FILES:
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- .github/coverage-manifest/Encina.AspNetCore.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends #276 for all 8 providers at registry level. |
| 2 | OpenTelemetry | ❌ N/A | A development tool; no instrument or span (the issue says none). |
| 3 | Structured Logging | ✅ Include | No new event: #4414's 528 logs a failed write, #276's 60351 the delete. |
| 4 | Health Checks | ❌ N/A | No backend of its own. |
| 5 | Validation | ✅ Include | Start validation of `AuditDeletes` against the recorder registration. |
| 6 | Resilience | ✅ Include | An audit failure never changes the delete answer unless the maintainer chooses write-ahead. |
| 7 | Distributed Locks | ❌ N/A | Deletes are idempotent and node-local. |
| 8 | Transactions | ❌ N/A | The recorder writes in its own scope. |
| 9 | Idempotency | ❌ N/A | A repeated delete writes a repeated event, which is the truth. |
| 10 | Multi-Tenancy | ✅ Include | The event carries the caller's tenant; a missing tenant is already denied by #276. |
| 11 | Module Isolation | ⏭️ Defer | Follows the module dimension of #746 (open). |
| 12 | Audit Trail | ✅ Include | This issue. |

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #4414 PR 1 (`ICacheAuditRecorder`) | Open | Order: #4414, then this plan |
| #276 PRs 1 to 3 (options, `ICacheDiagnostics`, endpoints) | Open | Plan merged |
| Maintainer answers to Design Choices 1 to 3 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas when work starts, records the answers under `## Maintainer Decisions`, then briefs the worker after #4414 PR 1 and #276 PR 3.
