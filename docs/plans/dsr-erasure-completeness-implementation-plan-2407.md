# Implementation Plan: DSR erasure completeness - cache purge, audit entry and processor notification

> **Issue**: [#2407](https://github.com/dlrivada/Encina/issues/2407)
> **Type**: Feature
> **Complexity**: High (6 phases, 5 pull requests of 12, 15, 15, 15 and 4 files)
> **Estimated Scope**: about 650 lines of production code, about 1300 lines of tests
> **Milestone**: v0.17.0 - Compliance Lifecycle (GDPR Art. 5(2), 17, 19, 30; SPEC-002)

---

## Summary

`DefaultDSRService.HandleErasureAsync` (`src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs`, lines 622-673) calls `IDataErasureExecutor.EraseAsync` and then, only when `IEncina` is registered, publishes a `DataErasedNotification` through `IEncina.Publish`; a `Left` from the publish is logged and dropped (`PublishNotificationAsync`, lines 1147-1163). Nothing purges cached copies, nothing records an audit entry, and nothing tells the processors. Re-checked against `origin/main` (db1e7f688) the issue is also wrong on two premises that change the design:

- `ICacheProvider` has no tag invalidation. Its members are `GetAsync`, `SetAsync`, `RemoveAsync`, `RemoveByPatternAsync`, `ExistsAsync`, `GetOrSetAsync`, `SetWithSlidingExpirationAsync` and `RefreshAsync`. Tag-based invalidation is the open feature #271. Query-cache keys are hashes of the request properties (`DefaultCacheKeyGenerator`), so no pattern can find "the cached data of one subject".
- The request id does not exist where the issue wants it. `ErasureRequest(string SubjectId, ErasureReason Reason, ErasureScope? Scope)` carries no DSR request id; `HandleErasureAsync` invents `$"dsr-{Guid.NewGuid():N}"` and fake field names (`field-1`, `field-2`, ...) for the notification. `DataSubjectRightsOptions.TrackAuditTrail` is declared but read nowhere, and `DSRAuditEntry` is used nowhere in `src/`.

The plan adds one port, `IErasureCompletion`, that `DefaultDSRService` calls once after a successful erase: it composes three small steps (cache purge, audit entry, outbox notification), each a `Left` on failure, in this order: purge, audit (recorded for every outcome, including a failed purge), notify. `DefaultDSRService` receives the port as one trailing optional constructor parameter (31 construction sites in 17 test files stay untouched); with a step enabled and no port, the erasure returns `Left` (fail closed). `ErasureRequest` gains the real request id (Design Choice 2). The processor-side consumer of the notification is outside this issue (follow-up issue file, Next Steps).

**Affected packages:** `Encina.Compliance.DataSubjectRights` (port, steps, options, errors, logs), `Encina.Security.Audit` (a new `ProjectReference` from DSR; no change to the package), `Encina.Messaging` (consumed: `OutboxOrchestrator`, `IOutboxStore`; no change), `Encina.Caching` (consumed; already referenced), `Encina` (EventId range registration only).

**Provider category**: Caching (8) for the purge, applied through `ICacheProvider.RemoveByPatternAsync` and therefore identical for Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet and KeyDB (Memcached planned, #277); Hybrid removes only tracked keys until #2402 is fixed, so the purge is complete for Hybrid only after it. The audit entry goes to any `IOperationAuditStore` (ADR-036 stores: ADO.NET x3, Dapper x3, EF Core, MongoDB, Marten); no new store.

**Sibling plans**: #2408 (post-commit invalidation) owns the Caching telemetry and the range `CachingInvalidation` 300-319; this plan owns range 320-329. #1188 and #1283 (retention holds, blocked data) change what an erasure may do before this completion runs; they are not prerequisites.

---

## Design Choices

<details>
<summary><strong>1. Where the three steps are orchestrated</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `DefaultDSRService`, through small ports, behind one `IErasureCompletion` port with an optional trailing constructor parameter** | One owner of the erasure result, as the issue decides; the constructor of a service with 31 construction sites in 17 test files does not change shape; each step is a small class tested alone | One more public interface |
| **B) A separate erasure saga** | Step-level compensation | Heavier; saga persistence is incomplete (`SagaRunner` ignores persistence failures, as the issue notes) |
| **C) Leave it to application handlers of the erasure notification** | No framework change | Today's failure mode; every application reimplements it |

### Chosen Option: **A - orchestrate in the service through ports**

### Rationale

- **Class:** Settled
- **Settled by:** the decision comment of 2026-10-17 on #2407 ("orchestrate inside `DefaultDSRService` through small ports (`IErasureCachePurger`, awaited `IOperationAuditStore` entry, `ErasureNotification` via the Outbox), a `Left` fails the request, startup fails when DSR has no audit store"; leaving it to handlers is the current failure mode and a saga is heavier). The single `IErasureCompletion` composition is an implementation detail of "small ports".

</details>

<details>
<summary><strong>2. Request identity of an erasure</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ErasureRequest` gains a required `DSRRequestId` (the id of the `DSRRequestAggregate`)** | The audit entry, the notification and the idempotency key refer to the real request; the fabricated `dsr-{guid}` disappears | A public record changes; only 6 construction sites in 4 test files use it (no production caller) |
| **B) A nullable id; the service generates one when absent** | No source change for callers | Keeps the fabricated id path; two attempts of the same request cannot be correlated |

### Chosen Option: **A - required `DSRRequestId`**

### Rationale

- **Class:** Settled
- **Settled by:** the issue acceptance ("erasure request id as the key", cross-cutting row 9 of #2407) and AGENTS.md section 1 (pre-1.0: no compatibility shims, best design). Option B cannot supply the idempotency key it is meant to supply.

</details>

<details>
<summary><strong>3. How the caches are purged</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Subject patterns from contributors** (`ISubjectCacheKeyContributor` per package, plus DSR's own `dsr:tenant:{seg}:restriction:{subject}` key) | Targeted; no collateral eviction | Cannot find query-cache entries (keys are request-property hashes): the acceptance "cached data of the subject is not served" is not guaranteed |
| **B) Tag per subject through the cache provider** | Targeted and complete | Depends on the open feature #271 (no tag API exists on `ICacheProvider` today); needs all 8 providers |
| **C) Purge the tenant's cache namespace: `RemoveByPatternAsync("{CachingOptions.KeyPrefix}:t:{EscapeSegment(tenant)}:*")`, or `"{KeyPrefix}:*"` when the application is single-tenant** | Complete by construction for every provider that honors `RemoveByPatternAsync`; one pattern; no new abstraction | An erasure evicts the whole tenant's cached entries (a warm-up cost for one tenant per erasure; erasures are rare) |

### Chosen Option: **C - purge the tenant namespace** (recommended, pending the maintainer)

### Rationale

- The key shape is the one `DefaultCacheKeyGenerator.GeneratePatternFromTemplate` produces (`{prefix}:t:{escaped tenant}:` when the context has a tenant); `CacheKeyRedactor.EscapeSegment` keeps the tenant segment injective, so one tenant's pattern can never match another's. With multi-tenancy required (`IsTenantContextRequired`, the service's existing guard) and no tenant in the context the step returns the existing `dsr.tenant_required` error and purges nothing (fail closed); it never falls back to `{KeyPrefix}:*`.
- Option B wins once #271 ships tags and a subject-tag convention exists for query caching; the purger interface (`IErasureCachePurger.PurgeAsync(tenantId, ct)`) is the seam. Option A wins for a deployment whose caches never hold subject data outside known keys.
- The DSR service's own restriction key (`RestrictionCacheKey`, uses the literal tenant segment `-` when there is no tenant, so a tenant named `-` collides with "no tenant") is covered by the namespace pattern only when it uses the shared prefix; it does not (`dsr:` prefix). The purge therefore also removes `dsr:tenant:{segment}:restriction:{subjectId}` explicitly. The collision of `-` is a defect of its own and has a follow-up issue file.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Subject reference in the audit entry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The DSR request id as `EntityId` (`EntityType = "DsrErasure"`), counts in `Metadata`, no subject value** | No personal data in an audit store that outlives the erasure; the request id links to the DSR aggregate for an authorized reader | Auditors search by request, not by subject |
| **B) A keyed hash (HMAC-SHA-256 with a configured secret) of the subject id** | Searchable by subject | A secret to manage and rotate; a hash of a low-entropy id is still re-identifiable; a new option with `[JsonIgnore]` and `ToString()` rules (AGENTS.md section 3) |
| **C) The raw subject id** | Simplest | Personal data persists in the audit trail after the erasure, which defeats the feature; excluded by the issue ("no error messages or personal values") |

### Chosen Option: **A - request id** (recommended, pending the maintainer)

### Rationale

- The entry is built with `Action = "Erase"`, `Outcome` from the step results, `TenantId` and `UserId` from the request context, `CorrelationId` from the request context, `ErrorMessage = null` (the error code goes to `Metadata["errorCode"]`), `RequestPayload`, `ResponsePayload` and `RequestPayloadHash` null, and `Metadata` with `fieldsErased`, `fieldsRetained`, `fieldsFailed`, `cachePurged` (all counts or booleans).
- Option B wins when auditors must answer "what happened to subject X" without the DSR aggregate.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Delivery of the processor notification</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ErasureNotification` written through `OutboxOrchestrator.AddAsync` and `IOutboxStore.SaveChangesAsync`; a `Left` fails the request; missing outbox registration fails startup (when `NotifyProcessors` is on)** | Durable from the moment the row is saved, at-least-once afterwards (retry and exhaustion are the outbox's); an unavailable outbox is an error, not a silent skip | The row is not in the erasure's transaction: the orchestrator never calls `SaveChangesAsync`, and the erasure runs through other stores, so a crash between the erase and the save loses the notification; the erasure can be retried (idempotent) and the notification re-written |
| **B) Keep `IEncina.Publish`, but make a `Left` fail the request** | No outbox needed | In-memory publish: lost on a crash, no retry |
| **C) Both: outbox when registered, publish otherwise** | Works everywhere | Two behaviors, one of them best-effort |

### Chosen Option: **A - outbox** (recommended, pending the maintainer)

### Rationale

- The issue asks for reliable delivery "or the request fails"; B can only fail loudly, C hides the weaker path.
- Option B wins for applications that run DSR without any outbox and accept in-process delivery; that is a configuration (`NotifyProcessors = false`), not a code path.
- The payload holds the subject id the processors need to erase (Art. 19), the tenant id, the DSR request id and counts; it holds no field names (the fabricated `field-N` names are dropped). The row lives until the outbox processes it; the application's outbox retention applies.
- The existing `DataErasedNotification` and its best-effort publish for erasure are replaced by `ErasureNotification`; the best-effort publish of the other rights (rectification, restriction) is unchanged and outside this issue.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Who receives the notification</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The notification carries no recipients; a handler in `Encina.Compliance.ProcessorAgreements` (follow-up) resolves the processors** | DSR gains no reference to `ProcessorAgreements`; `IProcessorService` returns every processor of the tenant, not the ones that received this subject's data, so the choice of recipients is a ProcessorAgreements concern | "Processors are notified" is met only once the follow-up handler exists; this issue delivers the durable event |
| **B) DSR lists `IProcessorService.GetAllProcessorsAsync` and puts the processor ids in the notification** | One issue delivers the whole chain | A new `ProjectReference` DSR to ProcessorAgreements; notifies every processor regardless of whether it held the subject's data (over-notification) |

### Chosen Option: **A - no recipients in the notification** (recommended, pending the maintainer)

### Rationale

- Verified: `Processor` has `ContactEmail` and no per-subject disclosure record, so the recipient set cannot be computed from the existing model.
- Option B wins if the maintainer wants Art. 19 over-notification as the safe default and accepts the dependency.
- The follow-up issue file `artifacts/issues/processor-erasure-notification-handler.md` describes the ProcessorAgreements handler and the missing outbound channel.
- Class: Dilemma

</details>

<details>
<summary><strong>7. Startup validation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A hosted startup check inside DSR now (audit store, outbox store and orchestrator present when the flags are on), replaced by the module requirement of #2382 when it lands** | The failure the issue asks for exists today; the pattern is the one of `CryptoShreddingStartupValidationHostedService` | A second place to maintain until #2382 |
| **B) Wait for #2382 (`IEncinaModuleRequirements`, open)** | One mechanism | The erasure runs without a mandatory audit until it lands |

### Chosen Option: **A - local check now** (recommended, pending the maintainer)

### Rationale

- The check resolves the services in a scope created in `StartAsync`, reports each missing service type by name (EventId 325), and throws `InvalidOperationException` so the host fails to start.
- Option B wins if the maintainer prefers not to add a mechanism #2382 will replace.
- Class: Dilemma

</details>

<details>
<summary><strong>8. Errors, categories, EventIds and telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New codes `dsr.erasure_cache_purge_failed`, `dsr.erasure_audit_failed`, `dsr.erasure_notification_failed` (ADR-062 category `UpstreamFailure`) and `dsr.erasure_completion_unavailable` (`FailedPrecondition`) through the categorized `EncinaErrors.Create(code, category, message)` overload; new range `ComplianceDSRErasure = (320, 329)`; a tenant tag on the new counter when multi-tenancy is on** | Follows ADR-062, the family EventId block and ADR-054 D4 | The existing DSR counters keep the tenant as an activity attribute only (a documented divergence, #906) |
| **B) Reuse `dsr.erasure_failed`** | No new codes | Hides which step failed |

### Chosen Option: **A**

### Rationale

- **Class:** Trivial
- **Settled by:** ADR-062 (every new code states its category and uses the categorized overload), ADR-054 D4 (tenant tag when multi-tenancy is enabled) and the EventId block 300-349 assigned to this family in `artifacts/decisions/plan-batch/eventid-blocks.md`; AGENTS.md section 7 (register the range, pack ids from the first).

</details>

---

## Implementation Phases

### Phase 1: Real request id on `ErasureRequest` and the EventId range (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Compliance.DataSubjectRights/Requests/ErasureRequest.cs` | modify (`string DSRRequestId` positional member) |
| 2 | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs` | modify (the notification carries the real id; the fabricated `dsr-{guid}` is removed; the fake `field-N` list stays until PR 4 replaces the notification) |
| 3 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`ComplianceDSRErasure = (320, 329)`) |
| 4 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (map `Encina.Compliance.DataSubjectRights` to `ComplianceDSR` and `ComplianceDSRErasure`) |
| 5 | `src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt` and `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`; two files) |
| 6 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs` | modify (2 construction sites; new test: the notification carries the request id) |
| 7 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/DataSubjectRightsDiagnosticsErrorMessageLeakTests.cs` | modify (2 sites) |
| 8 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/DataSubjectRightsDiagnosticsSubjectIdLeakTests.cs` | modify (1 site) |
| 9 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/ModelRecordTests.cs` | modify (1 site) |
| 10 | `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` | modify (`mechanical-fixer`) |
| 11 | `changelog.d/2407-erasure-request-carries-the-dsr-request-id.changed.md` | new (`mechanical-fixer`) |

Row 5 is two files, so the PR has 12 files. Tasks: the four test files are found with `Select-String -Pattern 'new ErasureRequest\('` (6 sites, none in `src/`); after the change `Select-String` finds no construction without the id. Guard tests are not needed: the record has no validated parameter, and `SubjectId` stays unvalidated as today.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #2407 makes DSR erasure complete; it needs the real DSR request id.
ErasureRequest (src/Encina.Compliance.DataSubjectRights/Requests/ErasureRequest.cs) has none, and HandleErasureAsync invents one.

TASK:
1. Add a required DSRRequestId positional member to ErasureRequest and use it in the DataErasedNotification built by HandleErasureAsync.
2. Register EventIdRanges.ComplianceDSRErasure = (320, 329) and map it for Encina.Compliance.DataSubjectRights in the allocation test.
3. Fix the six test construction sites; add one test proving the notification carries the request id.

KEY RULES:
- No optional parameter and no overload keeping the old shape (AGENTS.md section 1).
- Never log or tag the subject id.
- PublicAPI.Unshipped.txt, the manifest and the changelog fragment go through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs
```

</details>

### Phase 2: Completion port and cache purge (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Compliance.DataSubjectRights/Abstractions/IErasureCompletion.cs` | new |
| 2 | `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultErasureCompletion.cs` | new (purge step only; audit and notification join in PR 3 and PR 4) |
| 3 | `src/Encina.Compliance.DataSubjectRights/Abstractions/IErasureCachePurger.cs` | new |
| 4 | `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultErasureCachePurger.cs` | new |
| 5 | `src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs` | modify (`Erasure` property of the nested `ErasureOptions`: `PurgeCaches = true`, `RecordAudit = true`, `NotifyProcessors = true`) |
| 6 | `src/Encina.Compliance.DataSubjectRights/DSRErrors.cs` | modify (`ErasureCachePurgeFailedCode`, `ErasureCompletionUnavailableCode`) |
| 7 | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs` | modify (trailing `IErasureCompletion? completion = null`; call it after a successful erase) |
| 8 | `src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs` | modify (`TryAddScoped<IErasureCompletion, DefaultErasureCompletion>`, `TryAddScoped<IErasureCachePurger, DefaultErasureCachePurger>`) |
| 9 | `src/Encina.Compliance.DataSubjectRights/Diagnostics/ErasureCompletionLog.cs` | new (ids 320-322, Phase 5) |
| 10 | `src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 11 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/DefaultErasureCachePurgerTests.cs` | new |
| 12 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/ErasureCompletionTests.cs` | new (orchestration in `DefaultDSRService`, fail-closed flags, registration with `ValidateOnBuild` and `ValidateScopes`) |
| 13 | `tests/Encina.GuardTests/Compliance/DataSubjectRights/ErasureCompletionGuardTests.cs` | new |
| 14 | `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/2407-dsr-erasure-purges-caches.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `public interface IErasureCompletion { ValueTask<Either<EncinaError, Unit>> CompleteAsync(ErasureCompletionContext context, CancellationToken cancellationToken); }` where `ErasureCompletionContext` is a public sealed record `(string DSRRequestId, string SubjectId, string? TenantId, ErasureResult Result)` declared in the same file.
2. **Create** `IErasureCachePurger { ValueTask<Either<EncinaError, int>> PurgeAsync(string subjectId, string? tenantId, CancellationToken cancellationToken); }` and `DefaultErasureCachePurger(ICacheProvider cache, IOptions<CachingOptions> cachingOptions, ILogger<DefaultErasureCachePurger> logger)`: builds `"{KeyPrefix}:t:{CacheKeyRedactor.EscapeSegment(tenantId)}:*"` (single-tenant: `"{KeyPrefix}:*"`), removes it and the DSR's own restriction key, wraps a thrown exception as `Left` (`ErasureCachePurgeFailedCode`, category `UpstreamFailure`, exception type only through `ForLogging()` in the log, never `Message`), and returns the number of patterns removed (provider API returns no count). Multi-tenant with no tenant: `Left(DSRErrors.TenantRequired("HandleErasure"))`, nothing purged.
3. **Create** `DefaultErasureCompletion` with constructor `(IErasureCachePurger purger, IOptions<DataSubjectRightsOptions> options, ILogger<DefaultErasureCompletion> logger)`; with `PurgeCaches` off it returns `Right`; the audit and notification steps are added by later pull requests as constructor parameters.
4. **Modify** `HandleErasureAsync`: after `EraseAsync` returned `Right`, and when the options enable any step, call `completion.CompleteAsync`; with a step enabled and `completion` null return `Left(ErasureCompletionUnavailable)` (category `FailedPrecondition`). A `Left` of the completion fails the request (counted as failed, logged by code); the erasure itself is not undone and the request can be repeated.
5. **Tests**: purger removes the namespace pattern and the restriction key; tenant segment is escaped (a tenant containing `:` and `*` cannot widen the pattern); two different tenants never share a pattern; thrown provider exception becomes `Left` with the category; missing tenant fails closed; completion disabled by options; `completion` null with a step enabled returns `Left`; DI test with `ValidateOnBuild` and `ValidateScopes`; guard tests for both constructors and `PurgeAsync`/`CompleteAsync`. Integration: added to PR 3 (below).
6. **Manifest**: targets in Phase 5.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: ErasureRequest carries DSRRequestId and the range ComplianceDSRErasure (320-329) is registered.
DefaultDSRService has 31 construction sites in 17 test files, so the new collaborator is a trailing optional parameter.

TASK:
Create IErasureCompletion, IErasureCachePurger and their default implementations, the ErasureOptions flags, the two error codes
(with ADR-062 categories), the ErasureCompletionLog (ids 320-322) and the registrations; call the completion from HandleErasureAsync.

KEY RULES:
- Purge the tenant namespace only; with a required tenant context missing, return dsr.tenant_required and purge nothing.
- A Left from any step fails the request; nothing is swallowed. Exceptions of the cache provider become Left by exception type only.
- Categorized overload of EncinaErrors.Create; never put EncinaError.Message in a log, tag or audit entry.
- Never inject a scoped service into a singleton; the completion and the purger are scoped.
- Tests instantiate real classes; ValidateOnBuild and ValidateScopes DI test.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- src/Encina.Compliance.DataSubjectRights/DSRErrors.cs
- src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Audit entry and startup validation (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Compliance.DataSubjectRights/Encina.Compliance.DataSubjectRights.csproj` | modify (`ProjectReference` to `Encina.Security.Audit`; no cycle: it references `Encina`, `DomainModeling`, `Messaging`) |
| 2 | `src/Encina.Compliance.DataSubjectRights/Abstractions/IErasureAuditRecorder.cs` | new |
| 3 | `src/Encina.Compliance.DataSubjectRights/Erasure/OperationAuditErasureRecorder.cs` | new |
| 4 | `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultErasureCompletion.cs` | modify (audit step; audit recorded for every outcome) |
| 5 | `src/Encina.Compliance.DataSubjectRights/Erasure/ErasureStartupValidationHostedService.cs` | new (Design Choice 7) |
| 6 | `src/Encina.Compliance.DataSubjectRights/DSRErrors.cs` | modify (`ErasureAuditFailedCode`) |
| 7 | `src/Encina.Compliance.DataSubjectRights/Diagnostics/ErasureCompletionLog.cs` | modify (ids 323-325) |
| 8 | `src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs` | modify |
| 9 | `src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 10 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/OperationAuditErasureRecorderTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/ErasureStartupValidationTests.cs` | new |
| 12 | `tests/Encina.IntegrationTests/Compliance/DataSubjectRights/DSRErasureCompletionIntegrationTests.cs` | new (Marten collection fixture, `MartenOperationAuditStore`, Redis collection fixture: a cached entry is gone and an audit entry exists without a subject value) |
| 13 | `tests/Encina.GuardTests/Compliance/DataSubjectRights/ErasureCompletionGuardTests.cs` | modify |
| 14 | `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/2407-dsr-erasure-records-an-audit-entry.added.md` | new (`mechanical-fixer`) |

Tasks: the recorder builds the `OperationAuditEntry` of Design Choice 4 and awaits `IOperationAuditStore.RecordAsync`; a `Left` becomes `dsr.erasure_audit_failed` (`UpstreamFailure`). The order is purge, audit, notify. A failed audit stops the chain with that `Left` (an unauditable erasure is not reported as complete). The audit is attempted even when the purge failed (outcome `Failure`, `Metadata["errorCode"]`), and then the purge `Left` is returned. The startup check resolves `IOperationAuditStore` when `RecordAudit` is on and throws `InvalidOperationException` naming the missing type. `Encina.Security.Audit`'s `TenantId` filter is unchanged; the entry carries `TenantId` from the context. Unit tests: entry fields, no subject value anywhere (`ToString()` of every field checked), outcome mapping, failing store, startup check with and without the store; integration: erasure through `HandleErasureAsync` with a real Redis entry and the Marten store.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged: DefaultErasureCompletion runs the cache purge. IOperationAuditStore (Encina.Security.Audit) has RecordAsync.

TASK:
Add the audit step, the recorder, the startup validation hosted service and the ProjectReference; keep the order purge, audit, notify.

KEY RULES:
- The entry holds the request id, counts and the error code only: no subject id, no ErrorMessage, no payloads.
- Await the store; a Left fails the request with dsr.erasure_audit_failed (ADR-062 UpstreamFailure).
- The audit is recorded for every outcome, including a failed purge.
- Startup fails, by service type name, when RecordAudit is on and no IOperationAuditStore is registered.
- Integration tests use the shared Marten and Redis collection fixtures; never a per-class container.

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
- src/Encina.Security.Audit/OperationAuditEntry.cs
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs
- tests/Encina.IntegrationTests/Compliance/DataSubjectRights/DSRTenantLifecycleIntegrationTests.cs
```

</details>

### Phase 4: Outbox notification (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Compliance.DataSubjectRights/Notifications/ErasureNotification.cs` | new (`INotification`: `DSRRequestId`, `SubjectId`, `TenantId`, `FieldsErased`, `FieldsRetained`, `OccurredAtUtc`) |
| 2 | `src/Encina.Compliance.DataSubjectRights/Notifications/DataErasedNotification.cs` | delete (replaced; its only producer was `HandleErasureAsync`) |
| 3 | `src/Encina.Compliance.DataSubjectRights/Abstractions/IErasureNotificationPublisher.cs` | new |
| 4 | `src/Encina.Compliance.DataSubjectRights/Erasure/OutboxErasureNotificationPublisher.cs` | new (`OutboxOrchestrator.AddAsync` then `IOutboxStore.SaveChangesAsync`) |
| 5 | `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultErasureCompletion.cs` | modify (notify step) |
| 6 | `src/Encina.Compliance.DataSubjectRights/Erasure/ErasureStartupValidationHostedService.cs` | modify (outbox presence when `NotifyProcessors`) |
| 7 | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs` | modify (remove the erasure `PublishNotificationAsync` call and the fake field list) |
| 8 | `src/Encina.Compliance.DataSubjectRights/DSRErrors.cs` | modify (`ErasureNotificationFailedCode`) |
| 9 | `src/Encina.Compliance.DataSubjectRights/Diagnostics/ErasureCompletionLog.cs` | modify (ids 326-327) |
| 10 | `src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs` | modify |
| 11 | `src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 12 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/OutboxErasureNotificationPublisherTests.cs` | new |
| 13 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs` | modify (the erasure notification expectations) |
| 14 | `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/2407-dsr-erasure-notifies-through-the-outbox.changed.md` | new (`mechanical-fixer`) |

Tasks: the publisher adds the notification to the outbox and saves; a `Left` of either call is `dsr.erasure_notification_failed` (`UpstreamFailure`). This pull request is already at 15 files, so documentation is delivered in a **PR 5** of up to 4 files (`docs-writer`): `src/Encina.Compliance.DataSubjectRights/README.md`, the DSR page under `docs/features/` (verify the exact page name before the PR), `docs/INVENTORY.md`, and `ROADMAP.md` only if the milestone text changes. Existing tests that assert the best-effort erasure publish (`DefaultDSRServiceTests.cs`) are rewritten to the outbox. The best-effort publish for rectification and restriction is untouched. `DataErasedNotification` is searched for in `src/`, `tests/` and `docs/` before deletion.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phases 1 to 3 are merged. DefaultErasureCompletion purges caches and records the audit entry.

TASK:
Add ErasureNotification, the outbox publisher and the notify step; remove the best-effort erasure publish and DataErasedNotification.

KEY RULES:
- OutboxOrchestrator.AddAsync never saves; call IOutboxStore.SaveChangesAsync and treat a Left of either as failure.
- Do not describe this as atomic or "same transaction": the row is saved after the erasure, outside its transaction.
- The payload holds the subject id the processors need, tenant, request id and counts; no field names.
- Startup fails when NotifyProcessors is on and OutboxOrchestrator or IOutboxStore is not registered.
- Category UpstreamFailure for dsr.erasure_notification_failed (ADR-062).

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs
- src/Encina.Messaging/Outbox/IOutboxStore.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Compliance.DataSubjectRights/Notifications/DataErasedNotification.cs
```

</details>

### Phase 5: Observability, logging, coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **Logs** (`Diagnostics/ErasureCompletionLog.cs`, range `ComplianceDSRErasure` 320-329). Each pull request adds the ids it uses, in merge order, so the range stays packed in `main` after every merge: PR 2 adds 320 `CachePurged` (Debug), 321 `CachePurgeFailed` (Warning, exception through `ForLogging()`) and 322 `CompletionUnavailable` (Error); PR 3 adds 323 `AuditRecorded` (Debug), 324 `AuditFailed` (Error, error code only) and 325 `StartupValidationFailed` (Error, the missing service type name); PR 4 adds 326 `NotificationQueued` (Debug) and 327 `NotificationFailed` (Error, error code only). Ids 328-329 stay free.
2. **Telemetry**: an activity `dsr.erasure.completion` with child spans per step on the existing `ActivitySource` of `DataSubjectRightsDiagnostics`; `Counter<long> encina.dsr.erasure.steps` with tags `step` (`purge`, `audit`, `notify`) and `outcome` (`success`, `failed`, `skipped`), and the tenant tag only when multi-tenancy is on (ADR-054 D4). No subject id, cache pattern or key is a tag or attribute (`DataSubjectRightsDiagnosticsSubjectIdLeakTests` is extended with the new spans).
3. **Error categories (ADR-062)**: `dsr.erasure_cache_purge_failed`, `dsr.erasure_audit_failed`, `dsr.erasure_notification_failed` are `UpstreamFailure`; `dsr.erasure_completion_unavailable` is `FailedPrecondition`; each is created with `EncinaErrors.Create(code, category, message, exception?, details?)`. The existing `dsr.*` codes are not recategorized here (their producer slice is separate).
4. **Coverage targets (per flag, new or touched files)**, entered by `mechanical-fixer` in the PR that adds the tests:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|:----:|:-----:|:--------:|:--------:|:-----------:|---------------|
| `Erasure/DefaultErasureCachePurger.cs` | 95 | 25 | 0 | 0 | 50 | Pure pattern building and one provider call, fully unit tested; the Redis integration test reaches the real removal; no cross-implementation contract and no non-trivial invariant beyond the unit test of pattern injectivity |
| `Erasure/DefaultErasureCompletion.cs` | 95 | 25 | 0 | 0 | 50 | Sequential steps with explicit branches, all driven by fakes; integration reaches the happy path |
| `Erasure/OperationAuditErasureRecorder.cs` | 95 | 20 | 0 | 0 | 50 | Builds one entry; integration writes it to the Marten store |
| `Erasure/OutboxErasureNotificationPublisher.cs` | 95 | 20 | 0 | 0 | 0 | Two awaited calls; unit tests substitute the orchestrator and the store; no integration in this issue because the outbox store tests of the messaging packages own the real store |
| `Erasure/ErasureStartupValidationHostedService.cs` | 90 | 20 | 0 | 0 | 0 | Resolves services in a scope; unit tests with and without each service |
| `Diagnostics/ErasureCompletionLog.cs` | 70 | 0 | 0 | 0 | 0 | Generated logger bodies run only when a level is enabled; no parameters a guard test could reach |
| `Services/DefaultDSRService.cs` | keeps 97 | keeps 7 | keeps 52 | keeps 38 | keeps 61 | Existing targets are not lowered (target-ratchet); the new branch is covered by `ErasureCompletionTests` |
| `DataSubjectRightsOptions.cs` | keeps 100 | 0 | 0 | 0 | 0 | New defaults covered by the unit test of the options |

5. **Contract tests**: `IErasureCompletion` and the three step ports have a single implementation each; a contract test over the port semantics ("a `Left` from any step fails the call; nothing is swallowed") is part of `ErasureCompletionTests` and runs against a fake step set, so the contract flag target stays 0 with that reason.
6. **Property, load and benchmark**: no property test (the injectivity of the tenant segment is the existing `CacheKeyRedactor.EscapeSegment` behavior, asserted by the purger unit tests with hostile tenant ids; a property file would put PR 2 over 15 files); load and benchmark tests are not written (an erasure is a rare, serial operation); the manifest `reason` of each flag says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
The steps exist in phases 2 to 4. Telemetry, logs and targets remain per pull request.

TASK:
Add the logs and telemetry named in the plan to the pull request that introduces each step, measure per-flag coverage of every
touched file, record targets with one-sentence justifications through mechanical-fixer.

KEY RULES:
- EventIds only from 320-329, packed; the range is registered in EventIdRanges.cs first (Phase 1).
- No subject id, tenant value as a metric tag when multi-tenancy is off, cache pattern or error message in telemetry or logs.
- Do not lower any existing target of DefaultDSRService.cs.
- CRAP of every added method at most 10.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DSRLogMessages.cs
- .github/coverage-manifest/Encina.Compliance.DataSubjectRights.json
```

</details>

### Phase 6: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **Documentation (`docs-writer`, PR 5)**: the DSR package README (the erasure completion, the three flags, the fail-closed behavior, the cache purge scope and its Hybrid limitation until #2402, the audit entry shape, the outbox requirement), the feature page under `docs/features/`, `docs/INVENTORY.md`, XML documentation with examples on every new public type. No ADR: Design Choices 1, 2 and 8 follow ADR-036, ADR-050, ADR-062.
2. **Changelog fragments** as named in the phase tables; ROADMAP.md only if the v0.17.0 text changes.
3. **Verification per pull request**: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests`, `Encina.GuardTests`; the integration test with Docker; per-flag coverage with `coverage-report.cs` and `--check-justifications`; the CRAP table of the changed methods.
4. **Follow-up issue files** (Next Steps).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
Code and tests of the four pull requests exist. The documentation pull request remains.

TASK:
Through docs-writer, update the DSR README, the feature page and INVENTORY.md; confirm the changelog fragments exist.

KEY RULES:
- Documentation never types coverage figures; it cites cov: markers.
- State the Hybrid limitation (#2402) and that the notification is saved after the erasure, outside its transaction.
- Link ADRs and SPEC-002; one Diataxis quadrant per page.

REFERENCE FILES:
- docs/features/ (DSR page)
- .claude/skills/encina-docs/SKILL.md
- src/Encina.Compliance.DataSubjectRights/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| GDPR Art. 17 | Erasure must remove the data wherever it is held, caches included |
| GDPR Art. 19 | The controller communicates the erasure to each recipient |
| GDPR Art. 5(2), Art. 30 | Accountability: the erasure is demonstrable through an audit entry |
| SPEC-002 DEC-006 | Compliance gates fail closed on missing context |
| ADR-036 | Operation audit store abstraction |
| ADR-050, #2381/#2382 | Module requirements; startup validation |
| ADR-054 D4 and D6 | Tenant tag when multi-tenancy is on; no cache keys in telemetry |
| ADR-062 | Error category of every new code |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `DefaultDSRService.HandleErasureAsync` | `src/Encina.Compliance.DataSubjectRights/Services` | The erasure entry point |
| `ICacheProvider.RemoveByPatternAsync` | `src/Encina.Caching/Abstractions` | The purge |
| `DefaultCacheKeyGenerator`, `CacheKeyRedactor.EscapeSegment` | `src/Encina.Caching/KeyGeneration` | Key shape and injective tenant segment |
| `IOperationAuditStore`, `OperationAuditEntry` | `src/Encina.Security.Audit` | The audit entry |
| `OutboxOrchestrator`, `IOutboxStore` | `src/Encina.Messaging/Outbox` | The durable notification |
| `CryptoShreddingStartupValidationHostedService` | `src/Encina.Marten.GDPR` | Template of the startup check |
| `DataSubjectRightsDiagnostics`, `DSRLogMessages` | `src/Encina.Compliance.DataSubjectRights/Diagnostics` | Activity source, meter, logs |
| `DSRErrors.TenantRequired` | `src/Encina.Compliance.DataSubjectRights/DSRErrors.cs` | Fail-closed tenant error |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.DataSubjectRights` (new range `ComplianceDSRErasure`) | 320-329 (from block 300-349) | 320-327 used (PR 2: 320-322, PR 3: 323-325, PR 4: 326-327), 328-329 free; `ComplianceDSR` 8300-8349 is full (sparse ids, no contiguous free block) |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 11 | 9 | New: two interfaces for completion and purger, their defaults, audit recorder and interface, notification, publisher and interface, startup service, log; modified: `ErasureRequest`, service, options, errors (3 times), DI (3 times), csproj, `EventIdRanges.cs` |
| Tests | 7 | 8 | New: purger, completion, recorder, startup, publisher, guard, integration; modified: 4 construction-site files, the service tests, guard (twice), the allocation test |
| Documentation | 0 | 4 | README, feature page, `INVENTORY.md`, `ROADMAP.md` (PR 5) |
| Configuration | 4 | 7 | Four changelog fragments; four PublicAPI files over the PRs and the manifest four times |
| **Total** | **22** | **28** | 5 pull requests of 12, 15, 15, 15 and 4 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #2407 makes DSR erasure complete: after EraseAsync
succeeds, purge the tenant's caches, record an audit entry and queue a notification for the processors through the outbox.

IMPLEMENTATION OVERVIEW:
1. ErasureRequest carries the real DSRRequestId; range ComplianceDSRErasure (320-329) is registered.
2. IErasureCompletion (one trailing optional parameter of DefaultDSRService) composes the steps in the order purge, audit, notify.
3. Cache purge removes the tenant namespace pattern through ICacheProvider.RemoveByPatternAsync (no tag API exists; #271).
4. Audit entry through IOperationAuditStore with the request id, counts and the error code only.
5. ErasureNotification through OutboxOrchestrator.AddAsync and IOutboxStore.SaveChangesAsync; no recipients in the payload.
6. Startup validation of the audit store and the outbox; new error codes with ADR-062 categories; telemetry without subject data.
Four code pull requests plus one documentation pull request.

KEY PATTERNS:
- A Left from any step fails the request; the erasure is not undone and the request can be repeated.
- Fail closed: missing tenant when required, missing completion with a step enabled, missing audit store or outbox at startup.
- Never claim atomicity: the audit entry and the outbox row are written after the erasure, outside its transaction.
- No subject id, error message or cache pattern in logs, tags, attributes or audit entries.
- Apply Design Choices 3 to 7 as the Maintainer Decisions section says.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs
- src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Compliance.DataSubjectRights.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The purge of the tenant's cached entries after an erasure (Design Choice 3). |
| 2 | OpenTelemetry | ✅ Include | A completion activity with step spans and a step counter; tenant tag when multi-tenancy is on (ADR-054 D4); no subject id or key. |
| 3 | Structured Logging | ✅ Include | EventIds 320-327 in the new range 320-329, error codes and exception types only. |
| 4 | Health Checks | ❌ N/A | The existing DSR health check is unchanged; the startup check covers the new dependencies. |
| 5 | Validation | ❌ N/A | The erasure request is validated before it reaches the completion. |
| 6 | Resilience | ✅ Include | The notification is retried by the outbox; cache and audit failures fail the request, which can be repeated. |
| 7 | Distributed Locks | ❌ N/A | No new shared mutable state; repeating an erasure is idempotent. |
| 8 | Transactions | ✅ Include | The audit entry and the outbox row are saved after the erasure result, outside the erasure's transaction; a crash between them is recovered by repeating the request. |
| 9 | Idempotency | ✅ Include | The DSR request id is carried by the audit entry and the notification so a consumer deduplicates; the audit trail records each attempt. |
| 10 | Multi-Tenancy | ✅ Include | The purge pattern carries the escaped tenant segment; the audit entry and the notification carry the tenant; a missing required tenant denies. |
| 11 | Module Isolation | ❌ N/A | Not module scoped. |
| 12 | Audit Trail | ✅ Include | One `IOperationAuditStore` entry per erasure attempt, awaited. |

**Deferred items:**

- The processor-side handler of `ErasureNotification` (resolve recipients, notify each processor): follow-up issue file `artifacts/issues/processor-erasure-notification-handler.md`.
- The `-` tenant segment collision in the DSR cache keys: follow-up issue file `artifacts/issues/dsr-cache-key-tenant-segment-collision.md`.
- The unused `DataSubjectRightsOptions.TrackAuditTrail` and `DSRAuditEntry`: follow-up issue file `artifacts/issues/dsr-unused-audit-trail-option.md`.
- Tag-based purge instead of the tenant namespace: waits for #271 (open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `IOperationAuditStore` and its stores | ✅ Available | ADR-036 stores exist |
| Outbox (`OutboxOrchestrator`, `IOutboxStore`) | ✅ Available | Opt-in (`UseOutbox`); `NotifyProcessors` requires it |
| #2402 Hybrid `RemoveByPatternAsync` removes only tracked keys | ⏭️ Open bug | The purge is complete for Hybrid only after it is fixed |
| #271 Enhanced tag-based cache invalidation | ⏭️ Open feature | Enables a targeted purge (Design Choice 3, Option B) |
| #2382 Startup requirements validator | ⏭️ Open feature | Replaces the local startup check (Design Choice 7) |
| #1188, #1283 Retention holds | ⏭️ Open | Change what an erasure may do before this completion runs; not prerequisites |
| Maintainer answers to Design Choices 3 to 7 | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 3, 4, 5, 6, 7) when work starts, records the answers under `## Maintainer Decisions`, then briefs the four code workers and the documentation worker in phase order.
2. Open the three follow-up issues from `artifacts/issues/` (processor handler; `-` tenant segment; unused audit-trail option) and link them from #2407.
3. After the last pull request, comment on #2407 with the result and on #2408 with the namespace-purge decision.
