# Implementation Plan: Audit trail for tenant cache policy changes and quota enforcement summaries

> **Issue**: [#4414](https://github.com/dlrivada/Encina/issues/4414)
> **Type**: Feature
> **Complexity**: Medium (2 code pull requests of 15 and 15 files, and a documentation pull request of 2 files)
> **Estimated Scope**: about 600 lines of production code, about 1,000 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #4414 asks for one audit event when a tenant's resolved cache policy changes and one summary event per window and tenant for quota and rate-limit enforcement, written through `IOperationAuditStore`. It extends the tenant policies of #275 (open, plan merged). Re-checked against `origin/main` (d099efe3e):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| "who changed which tenant's policy" is answerable | The resolver of #275 only sees what `ITenantCachePolicyProvider.GetPolicyAsync(tenantId, ct)` returns: a policy or none, never an actor. The event can record the tenant, the previous and the new values and the detection time. The actor exists only if the application's own provider or administration tool audits it. |
| One event per change | The resolver state is node-local (#275 Design Choice 2), so every node that observes a change writes its own event: one event per change per node. |
| `IOperationAuditStore` is the sink | Correct: ADR-036 decision 2 makes the operation store the general write sink, distinguished by `Action` and `Metadata`. The providers register it scoped (`services.TryAddScoped<IOperationAuditStore, OperationAuditStoreEF>()`, `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:463`; ADO, Dapper and MongoDB likewise) and the in-memory default as a singleton (`src/Encina.Security.Audit/ServiceCollectionExtensions.cs:96`), so a singleton writer needs `IServiceScopeFactory`. |
| `Encina.Caching` can call it | It cannot today: `src/Encina.Caching/Encina.Caching.csproj` references `Encina`, `Encina.DistributedLock` and `Encina.Messaging` only; `Encina.Security.Audit` references `Encina`, `Encina.DomainModeling` and `Encina.Messaging`, so a reference from caching to audit creates no cycle. |
| An audit failure never fails a request | The policy and summary writes are not on the request's result path in this plan (they are queued, Design Choice 4), so that holds by construction; the repository default for pipeline audit is the opposite (`OperationAuditFailureMode.FailClosed`, `src/Encina.Security.Audit/OperationAuditFailureMode.cs`), which audits requests, not background evidence. |
| A window concept exists | No: #275 counts denials in a metric only (`encina.cache.tenant.rejected`, reasons `quota`, `rate_limit`; #4412 adds `memory`). The aggregation is new. |

The plan adds a shared audit recorder to `Encina.Caching` (`ICacheAuditRecorder`, used also by #4415 and #4416), a change detector in the policy resolver, a bounded per-tenant enforcement aggregator and one flush service that writes queued events and window summaries. Caching (8) is behavior-level, identical on all 8 providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277); the audit store keeps its own provider matrix, unchanged.

**Shared pieces and ownership (family cache-patterns-2):** this plan owns `ICacheAuditRecorder`, `CacheAuditEvent`, `CacheAuditSchema`, the `Encina.Caching` reference to `Encina.Security.Audit` and the `CachingTenantAudit` range 528-533; #4416 (second) and #4415 (third) only consume them. #275 owns `CacheTenantPolicy`, the resolver, the governor and `CacheTenantPolicyOptions`; this plan modifies them after #275 PR 3, and after #4412 when its `memory` reason exists (the aggregator takes a reason string, so either order builds). Cited siblings are open plans, by number only.

**Affected packages:** `Encina.Caching` (core), `Encina` (EventId registry). **Provider category:** Caching (8), behavior-level.

---

## Design Choices

<details>
<summary><strong>1. What is audited (Dilemma D1, the issue's D1 to D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every denial is an event** | Complete; wins under a regulation naming each decision | One store write per rejected request: a noisy tenant floods the store and the request path |
| **B) Policy changes and one summary per tenant per window** | Proportionate; answers "why did tenant X stop caching" | Individual denials are not in the audit store (they remain metrics and logs) |
| **C) Logs and metrics only** | No audit dependency | No audit trail at all |

### Chosen Option: **B - changes and window summaries** (recommended, pending the maintainer)

### Rationale

- Option A wins when a regulation demands each enforcement decision as evidence; C wins when no audit trail is required (the state of #275).
- Class: Dilemma

</details>

<details>
<summary><strong>2. Where the audit dependency lives (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Caching` takes a `ProjectReference` to `Encina.Security.Audit`; the recorder implementation is internal** | No new package; one place for #4414, #4415 and #4416 | Every caching user loads the audit assembly, although nothing runs unless the feature is on |
| **B) A new package `Encina.Caching.Audit` holds the implementation; `Encina.Caching` keeps only `ICacheAuditRecorder`** | Pay-for-what-you-use at assembly level | A package, README, csproj and 1.0 package count for about 150 lines |

### Chosen Option: **A - project reference** (recommended, pending the maintainer)

### Rationale

- Option B wins if the maintainer wants the caching package free of the audit assembly. Under either option the public contract (`ICacheAuditRecorder`) stays in `Encina.Caching`.
- Class: Dilemma

</details>

<details>
<summary><strong>3. The sink store and the entry mapping</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IOperationAuditStore` with `Action` `CachePolicyChanged` / `CacheEnforcementSummary`, `EntityType` `CacheTenantPolicy`, `EntityId` the tenant key, `Metadata` with the values** | The documented sink for administrative evidence; filters by action and tenant already exist | None |
| **B) A new `ICacheAuditStore`** | Own schema | A fourth audit store and ten provider implementations |

### Chosen Option: **A - operation store**

### Rationale

- **Class:** Settled
- **Settled by:** ADR-036 decision 2 ("Operation store is the general write sink ... distinguished by `Action` and `Metadata`"); AGENTS.md section 5 (a new store would need all 10 database providers).
- Mapping: `Id` is a version 7 GUID from `TimeProvider`, `CorrelationId` equals it (there is no request), `TenantId` is the tenant when multi-tenancy is on and `null` otherwise, `EntityId` is the tenant key verbatim (the reserved segment `default` in single-tenant mode, the convention of #275 Design Choice 5; the two modes never share a store row because `TenantId` differs), `Outcome` is `AuditOutcome.Success`, `UserId` is `null` (no actor), `ErrorMessage` is never set. `Metadata` holds numbers, member names and the window bounds only.

</details>

<details>
<summary><strong>4. Write path: queued or inline (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A bounded in-memory queue (`MaxPendingAuditEvents`, default 1,000) drained by one flush service together with the window summaries** | The request that triggers a refresh never waits for the store; one loop to reason about | A crash loses queued events; at the bound the event is dropped and logged (EventId 529) |
| **B) The resolver awaits `RecordAsync` inline, best effort** | No loss window | The unlucky request pays the store latency; a slow store slows tenant resolution |

### Chosen Option: **A - queue and flush service** (recommended, pending the maintainer)

### Rationale

- Option B wins when a lost policy-change event is unacceptable and the added latency of a rare refresh is not. The queue never blocks and never throws into the caller.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Flush failures are visible and retried</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Left` or exception from the store keeps the item for the next tick (at most `MaxFlushAttempts`, default 3), logs EventId 530 by error code or exception type, and logs EventId 531 when it is finally dropped** | The failure is visible and retried, never reported as success | Bounded memory needs a drop rule |
| **B) Log once and drop** | Simplest | An unwritten evidence row is lost after one failure |

### Chosen Option: **A - retry, then a visible drop**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 ("Errors are NEVER swallowed in background infrastructure: a `Left` ... from a store inside a processor ... fails that operation (retry, dead-letter or exception per its semantics)") and the background-loop rule of the same section: the loop catches per iteration, an `OperationCanceledException` from the stopping token ends it quietly, logs carry the error code or `ForLogging()`, never `EncinaError.Message`.
- On stop, the service makes one best-effort final flush bounded by the host's shutdown token.

</details>

<details>
<summary><strong>6. A missing audit store at start (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IValidateOptions` with `ValidateOnStart` fails the host when an audit option is on and no `IOperationAuditStore` descriptor exists** | An audit option never silently does nothing | The application must register a store (`AddEncinaAudit()` or a database provider) |
| **B) `AddEncinaCaching` registers the in-memory store with `TryAdd`, like `AddEncinaAudit` does** | Always works | Evidence that vanishes at restart looks like an audit trail |

### Chosen Option: **A - fail at start** (recommended, pending the maintainer)

### Rationale

- Option B wins for development setups. The validator checks the descriptor with `IServiceProviderIsService`, so it never resolves a scoped store at start.
- Class: Dilemma

</details>

<details>
<summary><strong>7. What counts as a change, error codes and telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A change is a difference (record equality of `CacheTenantPolicy`, plus a transition to or from "no policy") between two successful lookups of the same tenant on this node; the first lookup after start is the baseline and a failed or invalid lookup is not a change** | Matches "a policy change produces exactly one event"; no event on restart | A change made while a node was down is seen by the other nodes only |
| **B) The first lookup also writes an "observed" event** | The store always has a baseline | An event per tenant per node start: a restart floods the store |

### Chosen Option: **A - difference of two successful lookups**

### Rationale

- **Class:** Trivial
- **Settled by:** the acceptance criterion of #4414 ("A policy change produces exactly one audit event"); a first observation is not a change.
- Error code: `cache.audit_write_failed`, ADR-062 `ErrorCategory.Unavailable`, constant message "The cache audit event could not be written.", returned by `ICacheAuditRecorder` and logged by code (never the store's message). No other code is created. ADR-054 D4: no metric or span is added, so there is no tenant tag to place; the audit entry carries the tenant in its own field.

</details>

---

## Implementation Phases

### Phase 1: The shared audit recorder (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Encina.Caching.csproj` | modify (`ProjectReference` to `Encina.Security.Audit`, Design Choice 2) |
| 2 | `src/Encina.Caching/Audit/ICacheAuditRecorder.cs` | new (public interface, public sealed record `CacheAuditEvent`, public enum `CacheAuditEventKind`: `PolicyChanged`, `EnforcementSummary`, `KeysDeleted`, `ValueRead`) |
| 3 | `src/Encina.Caching/Audit/CacheAuditSchema.cs` | new (public static: the `Action` and `EntityType` constants and the error code `WriteFailedCode`) |
| 4 | `src/Encina.Caching/Audit/OperationStoreCacheAuditRecorder.cs` | new (internal sealed) |
| 5 | `src/Encina.Caching/CacheAuditServiceCollectionExtensions.cs` | new (public `AddEncinaCacheAudit`) |
| 6 | `src/Encina.Caching/Diagnostics/CacheAuditLog.cs` | new (EventId 528 and 529 in this PR) |
| 7 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingTenantAudit = (528, 533)`) |
| 8 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (add the range to the `Encina.Caching` entry) |
| 9 | `docs/engineering/eventid-block-registry.md` | modify (a row `CachingTenantAudit \| 528-533 \| Caching \| historical` in "Registered ranges" and the `500-549 reserved-pre-zone` block row split; read `EventIdRegistryAgreementTests` first) |
| 10 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 11 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 12 | `tests/Encina.UnitTests/Caching/CacheAuditRecorderTests.cs` | new (includes the DI test built with `ValidateOnBuild` and `ValidateScopes`) |
| 13 | `tests/Encina.GuardTests/Caching/CacheAuditGuardTests.cs` | new |
| 14 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/4414-cache-audit-recorder.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `ICacheAuditRecorder`: `ValueTask<Either<EncinaError, Unit>> RecordAsync(CacheAuditEvent auditEvent, CancellationToken cancellationToken)`. `CacheAuditEvent` carries `Kind`, `TenantKey` (the verbatim tenant, `default` when single-tenant), `TenantId` (`string?`), `UserId` (`string?`), `Count`, `Metadata` (`IReadOnlyDictionary<string, object?>`). Cancellation propagates as `OperationCanceledException`, never as a `Left`.
2. **Create** `OperationStoreCacheAuditRecorder(IServiceScopeFactory scopes, TimeProvider timeProvider, ILogger<...> logger)`: opens a scope per write, resolves `IOperationAuditStore` inside it (never holds a scoped store in the singleton), maps per Design Choice 3 (`ReadAuditEntry` mapping for `ValueRead` is #4415's PR), calls `RecordAsync`, turns a `Left` or an exception (not cancellation) into `cache.audit_write_failed` and logs EventId 528 with the kind and the error code or exception type.
3. **Create** `AddEncinaCacheAudit(this IServiceCollection services)`: `ArgumentNullException.ThrowIfNull(services)`; registers the recorder with `TryAddSingleton` and the start validator of Design Choice 6 (it fails when no `IOperationAuditStore` descriptor exists). Registration is `TryAdd`, so an application recorder wins.
4. **Tests:** a successful write maps every field as Design Choice 3 says; a `Left` and an exception both return `cache.audit_write_failed` and log 528; cancellation propagates; a scoped fake store resolves per call; start validation fails without a store and passes with one; the DI provider builds with `ValidateOnBuild` and `ValidateScopes`. Guard tests: null services throws; null `CacheAuditEvent` throws `ArgumentNullException`; a `Metadata` of null is rejected; a `default` `TenantId` of null is valid and gets a valid-result test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4414 adds an audit recorder to Encina.Caching. IOperationAuditStore
(src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs) is registered scoped by the database providers and as a singleton by
AddEncinaAudit; OperationAuditEntry is in src/Encina.Security.Audit/OperationAuditEntry.cs.

TASK:
1. Add the ProjectReference, ICacheAuditRecorder, CacheAuditEvent, CacheAuditSchema, the internal recorder and AddEncinaCacheAudit.
2. Create CacheAuditLog (EventIds 528-529), register CachingTenantAudit = (528, 533) in EventIdRanges.cs, the assembly map and the registry doc.
3. Write the unit, DI and guard tests; ask mechanical-fixer for the PublicAPI files, the manifest rows and the fragment.

KEY RULES:
- Resolve the audit store inside a new scope per write; never inject a scoped service into a singleton.
- Cancellation propagates; a store Left or exception becomes cache.audit_write_failed (ErrorCategory.Unavailable, constant message).
- Logs carry the event kind and the error code or exception type only, never EncinaError.Message, a key or a value.
- Time from TimeProvider; no static state.

REFERENCE FILES:
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
- src/Encina.Security.Audit/OperationAuditEntry.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
```

</details>

### Phase 2: Change detection, window summaries and the flush service (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 needs #275 PR 3 merged and holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Audit/TenantPolicyChangeDetector.cs` | new (internal sealed) |
| 2 | `src/Encina.Caching/Audit/TenantEnforcementWindow.cs` | new (internal sealed aggregator) |
| 3 | `src/Encina.Caching/Audit/CacheAuditPipeline.cs` | new (internal sealed: bounded queue and the window table) |
| 4 | `src/Encina.Caching/Audit/CacheAuditFlushService.cs` | new (internal sealed `BackgroundService`) |
| 5 | `src/Encina.Caching/Tenancy/TenantCachePolicyResolver.cs` | modify (an optional trailing detector parameter, one call at a successful refresh) |
| 6 | `src/Encina.Caching/Tenancy/TenantCacheGovernor.cs` | modify (one call at each place the `encina.cache.tenant.rejected` counter is incremented) |
| 7 | `src/Encina.Caching/Tenancy/CacheTenantPolicyOptions.cs` | modify (`AuditPolicyChanges`, `AuditEnforcementSummaries`, `SummaryWindow`, `MaxPendingAuditEvents`, `MaxFlushAttempts`) |
| 8 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (register the pipeline, the detector and the hosted service with `TryAddEnumerable` when an option is on; call `AddEncinaCacheAudit`) |
| 9 | `src/Encina.Caching/Diagnostics/CacheAuditLog.cs` | modify (EventIds 530 to 533) |
| 10 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the option properties) |
| 11 | `tests/Encina.UnitTests/Caching/TenantPolicyAuditTests.cs` | new (detector, window, resolver and governor hooks) |
| 12 | `tests/Encina.UnitTests/Caching/CacheAuditFlushServiceTests.cs` | new (`FakeTimeProvider`; includes the registration assertions) |
| 13 | `tests/Encina.GuardTests/Caching/CacheTenantPolicyGuardTests.cs` | modify (the new option setters; file created by #275 PR 1) |
| 14 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/4414-cache-tenant-audit.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `TenantPolicyChangeDetector.Observe(tenantKey, outcome)` (Design Choice 7): keeps per tenant the last successful outcome in a table bounded by `MaxTrackedTenants` of #275 (a tenant beyond the bound is not tracked and EventId 532 is logged once per window, never folded into another tenant's row); on a difference it enqueues one `PolicyChanged` event whose `Metadata` lists the changed member names with the old and new numeric values (`MaxEntries`, `Duration`, rate-limit members and every member #4412 and #4413 add). A unit test compares the detector's member list with the public properties of `CacheTenantPolicy` by reflection, so a member added later without detection fails the build.
2. **Create** `TenantEnforcementWindow`: `Add(tenantKey, reason)` increments a per-tenant dictionary of reason to count under the table bound; `Drain()` swaps the table and returns one `EnforcementSummary` per tenant that had at least one denial (no denial, no event: an empty window writes nothing); the metadata holds the window start and end from `TimeProvider`, the per-reason counts and nothing else.
3. **Create** `CacheAuditPipeline` (the queue, bounded by `MaxPendingAuditEvents`; `TryEnqueue` never blocks and never throws, EventId 529 at the bound) and `CacheAuditFlushService`: a `PeriodicTimer` loop on `SummaryWindow` (default 5 minutes) that drains the queue and the window, writes through `ICacheAuditRecorder` with the caller's stopping token, keeps a failed item for the next tick up to `MaxFlushAttempts` (EventIds 530 and 531), catches per iteration (EventId 533, exception type through `ForLogging()`), ends quietly on stopping-token cancellation, and makes one final flush on stop.
4. **Modify** the resolver and the governor with optional parameters only: with both audit options off the detector and the aggregator are not registered, the parameters are null and the code path costs one null check.
5. **Options:** `SummaryWindow` positive and at least 1 second, `MaxPendingAuditEvents` at least 1, `MaxFlushAttempts` 1 to 10; validated by the `IValidateOptions` of #275 at host start.
6. **Tests:** exactly one event per change, none for the first lookup, a failed lookup or an unchanged policy; a transition to no policy is one event; two tenants never share a summary; an empty window writes nothing; one summary per tenant per window with the right reason counts; the table bound; a store failure is retried and then dropped with 530 and 531; a throwing store never ends the loop; cancellation ends the loop; the queue bound drops and logs; the hosted service and the options validator are registered by DI built with `ValidateOnBuild` and `ValidateScopes`; with both options off nothing is registered.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: ICacheAuditRecorder, AddEncinaCacheAudit and CacheAuditLog (528-529) exist. #275 PRs 1-3 are merged: TenantCachePolicyResolver,
TenantCacheGovernor and CacheTenantPolicyOptions exist.

TASK:
1. Create the change detector, the enforcement window, the pipeline and the flush service; hook them into the resolver and the governor with optional parameters.
2. Add the five options and their validation, the registrations (hosted service with TryAddEnumerable) and EventIds 530-533.
3. Write the unit, DI and guard tests; ask mechanical-fixer for PublicAPI, the manifest rows and the fragment.

KEY RULES:
- The loop catches per iteration and never lets an exception escape; a stopping-token cancellation ends it quietly.
- A store failure is retried, then dropped visibly (530, 531); never swallowed silently; logs carry codes and exception types only.
- All tables are bounded by MaxTrackedTenants and never fold one tenant into another's row; time from TimeProvider; no static state.
- With both options off nothing is registered.

REFERENCE FILES:
- src/Encina.Caching/Sharding/Services/TopologyRefreshHostedService.cs
- src/Encina.Caching/Tenancy/TenantCacheGovernor.cs (after #275 PR 2)
- tests/Encina.UnitTests/Caching/CacheTenantFailClosedTests.cs
```

</details>

### Phase 3: Documentation (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 2 files, written by `docs-writer`: `docs/features/handler-caching.md` (a "Auditing tenant policies" section: the two options, what an event holds, that events are per node and that the actor is not recorded, no hand-typed figures) and `src/Encina.Caching/README.md` (the `AddEncinaCacheAudit` line and the new project reference). XML documentation with an `<example>` on every public type is part of PRs 1 and 2.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged.

TASK:
Ask docs-writer for the section in docs/features/handler-caching.md and the README line; verify links and lint.

KEY RULES:
- No hand-typed coverage figures; cite tests by name; Diataxis one quadrant per page.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/features/handler-caching.md
```

</details>

### Phase 4: Cross-cutting integration, observability and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds:** `CachingTenantAudit` 528-533 emitted only by `Encina.Caching`: 528 audit event write failed, 529 audit queue full and event dropped, 530 summary or event flush failed and kept for the next tick, 531 item dropped after `MaxFlushAttempts`, 532 audit tracked-tenant bound reached, 533 flush loop iteration faulted. Verified free by a grep of `docs/engineering/eventid-block-registry.md` (`500-549` assigned to cache-patterns-2) and of the merged and open sibling plans (#272 500-507, #274 508-513, #275 514-519, #4411 520-523, #4412 524-525, #4413 526-527); 534-549 belong to #4415, #4416 and #4417. The assembly map line of `Encina.Caching` is edited by several plans: later pull requests rebase and append only their range.
2. **Error codes:** `cache.audit_write_failed` (`Unavailable`, constant message), the only one.
3. **Coverage targets (per flag, set in the PR that adds each file; the measured values go in the PR body):**

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching/Audit/ICacheAuditRecorder.cs` | 0 | 0 | 0 | 0 | Interface, an event record and an enum: no executable validation |
| `Encina.Caching/Audit/CacheAuditSchema.cs` | 0 | 0 | 0 | 0 | Constants only, no executable line |
| `Encina.Caching/Audit/OperationStoreCacheAuditRecorder.cs` | 95 | 0 | 0 | 0 | Mapping, failure and cancellation paths are unit tested; internal, no `InternalsVisibleTo` for `Encina.GuardTests` |
| `Encina.Caching/CacheAuditServiceCollectionExtensions.cs` | 95 | 30 | 0 | 0 | One null guard plus registrations: the guard flag reaches the check and the valid-result test |
| `Encina.Caching/Diagnostics/CacheAuditLog.cs` | 90 | 0 | 0 | 0 | Generated bodies run in the tests above; internal |
| `Encina.Caching/Audit/TenantPolicyChangeDetector.cs`, `TenantEnforcementWindow.cs` | 100 | 0 | 0 | 0 | Pure logic with `FakeTimeProvider`; internal |
| `Encina.Caching/Audit/CacheAuditPipeline.cs` | 95 | 0 | 0 | 0 | Bounded queue driven by unit tests; internal |
| `Encina.Caching/Audit/CacheAuditFlushService.cs` | 90 | 0 | 0 | 0 | Loop, retry and stop paths with `FakeTimeProvider`; the unreachable remainder is the host start glue |
| `Encina.Caching/Tenancy/CacheTenantPolicyOptions.cs`, `TenantCachePolicyResolver.cs`, `TenantCacheGovernor.cs` | keep | keep | 0 | keep | Rows are created by #275 (no row exists on `origin/main` today); this plan never lowers them and tests its hooks in `TenantPolicyAuditTests.cs` |
| `Encina.Caching/ServiceCollectionExtensions.cs` | keep | keep | 0 | 0 | No per-file row on `origin/main` (package targets unit 60, guard 30, contract 10 apply); new registrations are tested in PR 2 |

   The worker measures every flag per PR with `coverage-report.cs`; no target is lowered (`target-ratchet`) and the CRAP table of changed methods stays at 10 or below.
4. **Property, contract, integration, load, benchmark:** no property test (grids cover the detector); no contract test (one recorder implementation); no integration test in this issue (the audit store has its own, and the recorder is proven against the in-memory store in unit tests); no load test or benchmark (node-local, off the request path by the queue).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The code pull requests are merged. Verification remains.

TASK:
Verify each PR carried its per-flag targets and the EventId registration; run coverage-report.cs --check-justifications; fix omissions through mechanical-fixer.

KEY RULES:
- Never lower an existing target (target-ratchet); add no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Background errors are not swallowed; loops catch per iteration; fail-closed; no `EncinaError.Message` in logs |
| AGENTS.md section 7, ADR-021 | EventId range registered before use |
| ADR-036 | Operation store as the general write sink |
| ADR-054 D4 | Tenant tags only with multi-tenancy on (no telemetry is added here) |
| ADR-062 | `cache.audit_write_failed` is `Unavailable` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `IOperationAuditStore`, `OperationAuditEntry` | `src/Encina.Security.Audit/` | The sink and the entry |
| `TopologyRefreshHostedService` | `src/Encina.Caching/Sharding/Services/` | Precedent of a `PeriodicTimer` loop that catches per iteration |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Decides `TenantId` null or set in the entry |
| Tenant policies, governor (#275) | `src/Encina.Caching/Tenancy/` (after #275) | The hook points |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/` | New range and assembly map |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingTenantAudit`, block 500-549) | 528-533 | This plan, see Phase 4 |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 8 | 8 | Audit folder (6), extension, log; csproj, options, resolver, governor, DI, registry range |
| Tests | 4 | 3 | Unit (3), guard (1); allocation test, guard file of #275 |
| Documentation | 0 | 2 | Handler-caching page and README |
| Configuration | 2 | 4 | Two fragments; manifest, PublicAPI x2, registry doc |
| **Total** | **14** | **17** | 3 pull requests of 15, 15 and 2 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4414 audits tenant cache policy changes and per-window enforcement summaries through
IOperationAuditStore, building on the tenant policies of #275.

IMPLEMENTATION OVERVIEW:
1. PR 1: ICacheAuditRecorder (shared with #4415 and #4416), the Security.Audit reference, EventIds 528-533 registered.
2. PR 2: change detector, enforcement window, queue and flush service, options, hooks in the resolver and the governor.
3. PR 3: documentation by docs-writer.

KEY PATTERNS:
- Write through a new scope per write; queue and flush off the request path; retry then a visible drop.
- Everything bounded by MaxTrackedTenants; the reserved segment default for the implicit tenant; no actor is recorded.
- Follow the maintainer's answers to Design Choices 1, 2, 4 and 6, given in your brief.

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends #275 for all 8 providers at behavior level. |
| 2 | OpenTelemetry | ❌ N/A | The metrics are #275's; no instrument or span is added (the issue says none). |
| 3 | Structured Logging | ✅ Include | `CacheAuditLog`, EventIds 528-533, codes only. |
| 4 | Health Checks | ❌ N/A | No backend of its own; the audit store has its own check (`OperationAuditStoreHealthCheck`). |
| 5 | Validation | ✅ Include | Window and bound options, and the store-presence validator at host start. |
| 6 | Resilience | ✅ Include | Retry then a visible drop for the flush; a store failure never reaches a request. |
| 7 | Distributed Locks | ❌ N/A | Node-local state; one event per node by design. |
| 8 | Transactions | ❌ N/A | Each write runs in its own scope, independent of any request transaction. |
| 9 | Idempotency | ❌ N/A | Summaries are per window and per node; a retried item keeps its id. |
| 10 | Multi-Tenancy | ✅ Include | Tenant on every event, the reserved `default` segment, bounded tables. |
| 11 | Module Isolation | ⏭️ Defer | Follows the module dimension of #746 (open) like #275. |
| 12 | Audit Trail | ✅ Include | This issue. |

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #275 PRs 1 to 3 (resolver, governor, options) | Open | PR 2 of this plan needs them; PR 1 needs nothing |
| #4412 (`memory` reason) | Open | Optional: the aggregator takes any reason string |
| Block 500-549 of the EventId registry | ✅ Assigned | 528-533 are this plan's |
| Maintainer answers to Design Choices 1, 2, 4 and 6 | Pending | Asked when work starts |

Order inside the family: this plan's PR 1 first (it owns the recorder), then #4416, then #4415 (the other two consume `ICacheAuditRecorder`); #4417 is independent.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas when work starts, records the answers under `## Maintainer Decisions`, then briefs the workers in PR order.
2. Merge #275 PR 3 before PR 2 of this plan.
