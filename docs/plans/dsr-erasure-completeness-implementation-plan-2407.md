# Implementation Plan: DSR erasure completeness - cache purge, audit entry and processor notification

> **Issue**: [#2407](https://github.com/dlrivada/Encina/issues/2407)
> **Type**: Feature
> **Complexity**: High (9 phases, 9 pull requests of 13, 9, 15, 10, 15, 12, 7, 8 and 4 files)
> **Estimated Scope**: about 700 lines of production code, about 1500 lines of tests
> **Milestone**: v0.17.0 - Compliance Lifecycle (GDPR Art. 5(2), 17, 19, 30; SPEC-002)

---

## Summary

`DefaultDSRService.HandleErasureAsync` (`src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs`, lines 622-673) calls `IDataErasureExecutor.EraseAsync` and then, only when `IEncina` is registered, publishes a `DataErasedNotification` through `IEncina.Publish`; a `Left` from the publish is logged and dropped (`PublishNotificationAsync`, line 1147). Nothing purges cached copies, nothing records an audit entry, and nothing tells the processors. Re-checked against `origin/main` (e694eb032) the issue is also wrong on two premises that change the design:

- `ICacheProvider` has no tag invalidation. Its members are `GetAsync`, `SetAsync`, `RemoveAsync`, `RemoveByPatternAsync`, `ExistsAsync`, `GetOrSetAsync`, `SetWithSlidingExpirationAsync` and `RefreshAsync`. Tag-based invalidation is the open feature #271. Query-cache keys are hashes of the request properties (`DefaultCacheKeyGenerator`), so no pattern can find "the cached data of one subject".
- The request id does not exist where the issue wants it. `ErasureRequest(string SubjectId, ErasureReason Reason, ErasureScope? Scope)` carries no DSR request id; `HandleErasureAsync` invents `$"dsr-{Guid.NewGuid():N}"` and fake field names (`field-1`, `field-2`, ...) for the notification. `DataSubjectRightsOptions.TrackAuditTrail` is declared but read nowhere (`DataSubjectRightsOptions.cs:213`), and `DSRAuditEntry` is used nowhere in `src/`; ADR-036 decision 4 (`docs/architecture/adr/036-three-audit-stores.md:66`) already orders both deleted.

The plan adds one port, `IErasureCompletion`, that `DefaultDSRService` calls once after a successful erase. It runs an ordered set of `IErasureCompletionStep` services: cache purge, audit entry, outbox notification. The three steps are not options: ADR-050 line 77 lists them as required for DSR, with no opt-out, so there are no `PurgeCaches`/`RecordAudit`/`NotifyProcessors` flags, and the unread `TrackAuditTrail` is deleted (ADR-036:66). Because the completion takes its steps as a collection, a step added by a later pull request never changes the constructor of `DefaultErasureCompletion`. `DefaultDSRService` receives the port as one trailing optional constructor parameter (31 construction sites in 17 test files stay untouched); with no port the erasure returns `Left` (fail closed). `ErasureRequest` gains the real request id (Design Choice 2). The processor-side consumer of the notification is outside this issue (follow-up issue to be opened, draft: `artifacts/decisions/plan-batch/followups/processor-erasure-notification-handler.md`).

**Affected packages:** `Encina.Compliance.DataSubjectRights` (port, steps, errors, logs, telemetry, a new `ProjectReference` to `Encina.Security.Audit`), `Encina.Caching` (two small changes consumed by the purge, PR 2), `Encina.Security.Audit` (consumed; no change), `Encina.Messaging` (consumed: `OutboxOrchestrator`, `IOutboxStore`; no change), `Encina` (EventId range registration only).

**Provider category**: Caching (8) for the purge, applied through `ICacheProvider.RemoveByPatternAsync` and therefore identical for Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet and KeyDB (Memcached planned, #277); Hybrid removes only tracked keys until #2402 is fixed, so the purge is complete for Hybrid only after it. The audit entry goes to any `IOperationAuditStore` (ADR-036 stores: ADO.NET x3, Dapper x3, EF Core, MongoDB, Marten); no new store. The notification goes to any `IOutboxStore` provider of the 10-provider matrix; no new store.

**Sibling plans**: #2408 (post-commit invalidation) owns the Caching telemetry and the range `CachingInvalidation` 300-319; this plan owns range 320-329 (`ComplianceDSRErasure`, packed from 320; see Event ID Allocation). #1188 and #1283 (retention holds, blocked data) change what an erasure may do before this completion runs; they are not prerequisites.

---

## Design Choices

<details>
<summary><strong>1. Where the three steps are orchestrated</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `DefaultDSRService`, through small ports: one `IErasureCompletion` port that runs `IErasureCompletionStep` services in order, as an optional trailing constructor parameter** | One owner of the erasure result, as the issue decides; the constructor of a service with 31 construction sites in 17 test files does not change shape; a step added by a later pull request changes no constructor; each step is tested alone | Two more public interfaces |
| **B) A separate erasure saga** | Step-level compensation | Heavier; saga persistence is incomplete (`SagaRunner` ignores persistence failures, as the issue notes) |
| **C) Leave it to application handlers of the erasure notification** | No framework change | Today's failure mode; every application reimplements it |

### Chosen Option: **A - orchestrate in the service through ports**

### Rationale

- **Class:** Settled
- **Settled by:** the decision comment of 2026-10-17 on #2407 ("orchestrate inside `DefaultDSRService` through small ports (`IErasureCachePurger`, awaited `IOperationAuditStore` entry, `ErasureNotification` via the Outbox), a `Left` fails the request, startup fails when DSR has no audit store"; leaving it to handlers is the current failure mode and a saga is heavier). Option B is rejected by the same comment; option C is the failure mode the issue exists to remove. The step collection is an implementation detail of "small ports".

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
<summary><strong>3. The three steps are mandatory: no opt-out flags, and the audit store must be persistent</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No flags: the cache purge, the audit entry and the outbox notification always run; startup fails when the audit store is missing or is the in-memory default outside Development, or the outbox is missing; the unread `TrackAuditTrail` is deleted** | Follows the module requirement table; no way to switch off an Art. 17, 19 or 5(2) control | Applications that wanted to skip a step cannot |
| **B) Three boolean flags (`PurgeCaches`, `RecordAudit`, `NotifyProcessors`), default on** | Flexible | Each flag switches off a control the ADR lists as required; contradicts the compliance rule that only territorial non-applicability may disable a control |

### Chosen Option: **A - mandatory steps**

### Rationale

- **Class:** Settled
- **Settled by:** ADR-050 `docs/architecture/adr/050-modules-may-require-modules.md:77` (DSR requires operation audit awaited, cache purge and reliable notification; condition "Required", no opt-out column entry) and sub-decision 5 (line 120: audit mandatory whenever the module is enabled, awaited, its failure fails the operation); ADR-050:69 and :104 (the in-memory audit store never satisfies the requirement outside Development, enforced by an `IHostedService` startup validation); ADR-036:66 (`TrackAuditTrail` and `DSRAuditEntry` deleted); AGENTS.md section 3 (compliance gates fail closed). Option B contradicts the first citation.
- The explicit, logged opt-out of ADR-050 sub-decision 2 for in-memory implementations belongs to #2382, not to this plan; until it lands the check has no opt-out outside Development.

</details>

<details>
<summary><strong>4. How the caches are purged</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Subject patterns from contributors** (`ISubjectCacheKeyContributor` per package, plus DSR's own `dsr:tenant:{seg}:restriction:{subject}` key) | Targeted; no collateral eviction | Cannot find query-cache entries (keys are request-property hashes): the acceptance "cached data of the subject is not served" is not guaranteed |
| **B) Tag per subject through the cache provider** | Targeted and complete | Depends on the open feature #271 (no tag API exists on `ICacheProvider` today); needs all 8 providers |
| **C) Purge the tenant's cache namespace: `RemoveByPatternAsync("{CachingOptions.KeyPrefix}:t:{EscapeSegment(tenant)}:*")`, or `"{KeyPrefix}:*"` when the application is single-tenant (Design Choices 5 and 6 make both safe)** | Complete by construction for every provider that honors `RemoveByPatternAsync`; one pattern; no new abstraction | An erasure evicts the whole tenant's cached entries (a warm-up cost for one tenant per erasure; erasures are rare) |

### Chosen Option: **C - purge the tenant namespace** (recommended, pending the maintainer)

### Rationale

- The key shape is the one `DefaultCacheKeyGenerator` produces (`{prefix}:t:{escaped tenant}:` when the context has a tenant, line 92; `{prefix}:` otherwise, line 93). With multi-tenancy required (`IsTenantContextRequired`, the service's existing guard) and no tenant in the context the step returns the existing `dsr.tenant_required` error and purges nothing (fail closed); it never falls back to `{KeyPrefix}:*`.
- Option B wins once #271 ships tags and a subject-tag convention exists for query caching; the step class (`CachePurgeErasureStep`) is the seam to replace. Option A wins for a deployment whose caches never hold subject data outside known keys.
- The DSR service's own restriction key (`RestrictionCacheKey`, uses the literal tenant segment `-` when there is no tenant, so a tenant named `-` collides with "no tenant") is covered by the namespace pattern only when it uses the shared prefix; it does not (`dsr:` prefix). The step therefore also removes `dsr:tenant:{segment}:restriction:{subjectId}` explicitly. The collision of `-` is a defect of its own (follow-up issue to be opened, draft: `artifacts/decisions/plan-batch/followups/dsr-cache-key-tenant-segment-collision.md`). #3614 (open) tracks a related defect: invalidation patterns that miss keys with no tenant segment.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Glob characters in a tenant id and the case sensitivity of the Memory provider</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The step rejects a tenant id that contains a glob metacharacter (`*`, `?`, `[`, `]`, `\`) with `Left` (`dsr.erasure_cache_tenant_invalid`, `Validation`) and purges nothing; the Memory provider's case-insensitive match is documented as over-eviction only (tenants whose ids differ only in case lose cache entries too, never the reverse)** | No change to the escape contract or to any provider; fails closed; no pattern can ever reach another tenant's namespace by a glob character | A tenant with such an id cannot complete an erasure until its id changes (an unusual id; the erasure fails visibly); over-eviction on Memory is a performance cost, not a disclosure |
| **B) Extend the escaping: `CacheKeyRedactor.EscapeSegment` also escapes the glob metacharacters, each provider receives a matching escape convention (Redis family `\`; Memory has none and matches with `RegexOptions.IgnoreCase`, `MemoryCacheProvider.cs:228-240`, so it needs its own fix) | Removes the limitation at the root; helps every pattern user | Touches `Encina.Caching` and at least `Encina.Caching.Memory` plus per-provider tests (several PRs of its own); changes the shape of existing keys |

### Chosen Option: **A - reject glob characters** (recommended, pending the maintainer)

### Rationale

- Facts verified on `origin/main`: `CacheKeyRedactor.EscapeSegment` (`src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs:64`) escapes only `%` and `:`; a tenant `a*` would produce `sm:t:a*:*`, which also matches the keys of tenant `abc`.
- Option B wins if tenant ids with glob characters are a legitimate case for the reference application, or if other pattern users need the escaping; it is the better design in the long run but is larger than this issue (a follow-up issue to be opened, draft: `artifacts/decisions/plan-batch/followups/memory-cache-pattern-case-insensitive.md`, records the Memory inconsistency).
- Class: Dilemma

</details>

<details>
<summary><strong>6. Single-tenant purge: the idempotency and lock prefixes live inside the namespace</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Purge `{KeyPrefix}:*` and fail with `Left` (`dsr.erasure_cache_prefix_nested`, `FailedPrecondition`) when `CachingOptions.IdempotencyKeyPrefix` or `LockKeyPrefix` starts with `{KeyPrefix}:`; with today's defaults (`sm`, `sm:idempotency`, `sm:lock`, `CachingOptions.cs:54,66,78`) every single-tenant erasure fails until the application changes its prefixes | Self-contained in DSR; never deletes idempotency records or locks | The default configuration cannot erase |
| **B) Change the `CachingOptions` defaults so the prefixes do not nest (`sm-idempotency`, `sm-lock`) in `Encina.Caching` (PR 2), and keep the guard of option A for custom nested configuration** | Removes the root cause; the default configuration works; `sm:*` then cannot match idempotency or lock keys | Changes two defaults of another package (pre-1.0: no compatibility concern); idempotency records written under the old default are orphaned |
| **C) Never purge `{KeyPrefix}:*`: single-tenant deployments are served only by a tag-based purge after #271** | No collateral risk | The erasure leaves cached data in every single-tenant deployment until #271 ships; a disabled control |

### Chosen Option: **B - non-nested default prefixes plus the guard** (recommended, pending the maintainer)

### Rationale

- Verified: `DistributedIdempotencyPipelineBehavior.cs:105` writes keys `{IdempotencyKeyPrefix}:{tenant}:{key}`; with the default `sm:idempotency` the pattern `sm:*` deletes them, so duplicate messages would be processed again after an erasure. In multi-tenant mode the pattern `sm:t:{tenant}:*` cannot reach `sm:idempotency:` or `sm:lock:` keys.
- Option A wins if the maintainer does not want PR 2 (the change of two defaults) and accepts that single-tenant applications set their own prefixes first. Option C is rejected here because it disables a control for a whole deployment mode.
- Class: Dilemma

</details>

<details>
<summary><strong>7. Subject reference in the audit entry</strong></summary>

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
<summary><strong>8. Delivery of the processor notification</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ErasureNotification` written through `OutboxOrchestrator.AddAsync` and `IOutboxStore.SaveChangesAsync`; a `Left` fails the request; a missing outbox registration fails startup** | Durable from the moment the row is saved, at-least-once afterwards (retry and exhaustion are the outbox's); an unavailable outbox is an error, not a silent skip | The row is not in the erasure's transaction: the orchestrator never calls `SaveChangesAsync`, and the erasure runs through other stores, so a crash between the erase and the save loses the notification; the erasure can be retried (idempotent) and the notification re-written |
| **B) Keep `IEncina.Publish`, but make a `Left` fail the request** | No outbox needed | In-memory publish: lost on a crash, no retry |
| **C) Both: outbox when registered, publish otherwise** | Works everywhere | Two behaviors, one of them best-effort |

### Chosen Option: **A - outbox**

### Rationale

- **Class:** Settled
- **Settled by:** the decision comment of 2026-10-17 on #2407 ("`ErasureNotification` via the Outbox") and ADR-050:77 (reliable processor notification, Art. 19, required). Options B and C are in-memory or best-effort and do not give the reliable delivery the issue and the ADR require.
- The existing `DataErasedNotification` and its best-effort publish for erasure are replaced by `ErasureNotification` (PR 7 deletes the old record and the call); the best-effort publish of the other rights (rectification, restriction) is unchanged and outside this issue.

</details>

<details>
<summary><strong>9. What the notification payload carries, and how long it lives in the outbox</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The payload carries the DSR request id, tenant id, counts and `OccurredAtUtc`, but no subject id; a processor handler resolves the subject through an authorized lookup of the DSR request** | No personal data in the outbox table, which has no retention today (a `git grep` for Retention, Cleanup and Purge in `Encina.Messaging/Outbox` finds none; #1200 plans it, open) | The handler of the follow-up needs the lookup; the recipient cannot act on the message alone |
| **B) The payload carries the raw subject id (what the processors need to erase, Art. 19); processed erasure rows are deleted or redacted by a retention job (#1200 or a dedicated job)** | Self-contained message | Every erasure leaves the identifier of the erased subject in plaintext in the outbox table until that job exists |

### Chosen Option: **A - request id, no subject id** (recommended, pending the maintainer)

### Rationale

- Verified on `origin/main`: `Encina.Messaging/Outbox` has no retention or cleanup of processed rows, so option B leaves the subject id indefinitely; "the application's outbox retention applies" names a mechanism Encina does not provide.
- Option B wins if the recipients cannot call back into the controller (an external processor with no access to the DSR aggregate) and the maintainer accepts the interim exposure until #1200 ships.
- Under option A the plan's `ErasureNotification` has `DSRRequestId`, `TenantId`, `FieldsErasedCount`, `FieldsRetainedCount` and `OccurredAtUtc`; under B it adds `SubjectId`.
- Class: Dilemma

</details>

<details>
<summary><strong>10. Who receives the notification</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The notification carries no recipients; a handler in `Encina.Compliance.ProcessorAgreements` (follow-up) resolves the processors** | DSR gains no reference to `ProcessorAgreements`; `IProcessorService` returns every processor of the tenant, not the ones that received this subject's data, so the choice of recipients is a ProcessorAgreements concern | "Processors are notified" is met only once the follow-up handler exists; this issue delivers the durable event |
| **B) DSR lists `IProcessorService.GetAllProcessorsAsync` and puts the processor ids in the notification** | One issue delivers the whole chain | A new `ProjectReference` DSR to ProcessorAgreements; notifies every processor regardless of whether it held the subject's data (over-notification) |

### Chosen Option: **A - no recipients in the notification** (recommended, pending the maintainer)

### Rationale

- Verified: `Processor` has `ContactEmail` and no per-subject disclosure record, so the recipient set cannot be computed from the existing model.
- Option B wins if the maintainer wants Art. 19 over-notification as the safe default and accepts the dependency.
- The follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/processor-erasure-notification-handler.md`) describes the ProcessorAgreements handler and the missing outbound channel.
- Class: Dilemma

</details>

<details>
<summary><strong>11. Order and failure semantics of the steps</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Order: purge, audit, notify. Every step runs in order; the audit step still runs after a failed purge and records outcome `Failure` with the error code; a failed audit stops the chain before the notification; the first `Left` is returned; the erasure itself is not undone and the request can be repeated** | The erasure attempt is always accounted for (Art. 5(2)); an unauditable erasure is never reported complete (ADR-050 sub-decision 5); repeating is safe because every step is idempotent except the notification, which consumers deduplicate by request id | A repeated request writes one more audit entry and one more outbox row |
| **B) Stop at the first failure, record nothing for a failed purge** | Simpler | A failed erasure attempt leaves no accountability trace |
| **C) Run all steps and return all errors** | Maximum progress | A notification could announce an erasure whose audit failed; a combined error type |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- ADR-050 sub-decision 5 (line 120) settles only that an audit failure fails the operation; the ordering around a failed purge and the "no undo" rule are choices of this plan, hence a Dilemma.
- Option B wins if the maintainer does not want failed attempts in the audit trail; option C wins if partial progress matters more than strict ordering.
- The step contract: `IErasureCompletionStep { int Order { get; } bool RunsAfterFailure { get; } ValueTask<Either<EncinaError, Unit>> ExecuteAsync(ErasureCompletionContext context, CancellationToken cancellationToken); }`; `DefaultErasureCompletion` sorts by `Order` (purge 10, audit 20, notify 30), passes `context with { FailureCode = <first error code> }` to steps with `RunsAfterFailure` (audit), skips the others after a failure, and returns the first `Left`.
- Class: Dilemma

</details>

<details>
<summary><strong>12. Startup validation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A hosted startup check inside DSR now: `IOperationAuditStore` registered and not the in-memory default outside Development; `OutboxOrchestrator` and `IOutboxStore` registered; replaced by the module requirement of #2382 when it lands** | The failure the issue asks for exists today; the pattern is the one of `CryptoShreddingStartupValidationHostedService` | A second place to maintain until #2382 |
| **B) Wait for #2382 (`IEncinaModuleRequirements`, open)** | One mechanism | The erasure runs without a mandatory audit until it lands, against the 2026-10-17 comment and ADR-050:77 |

### Chosen Option: **A - local check now**

### Rationale

- **Class:** Settled
- **Settled by:** the decision comment of 2026-10-17 on #2407 ("startup fails when DSR has no audit store"), ADR-050:77 and :104 (an `IHostedService` that throws in `StartAsync`; the in-memory default never satisfies the requirement outside Development). Option B is excluded by both.
- The check resolves the services in a scope created in `StartAsync`, reports each missing service type by name (EventId 326), and throws `InvalidOperationException` so the host fails to start. The in-memory default is detected by the resolved type `InMemoryOperationAuditStore` (public, sealed) when the application's `IHostEnvironment` is not Development; a missing `IHostEnvironment` is treated as not Development (fail closed, as `BreakTheGlassStartupValidator.cs:186` reads it). A decorated in-memory store is not detected by a resolved-instance check (`OperationAuditStoreRegistration.IsInMemoryDefault` needs the `IServiceCollection`, which a hosted service does not have): this limit is stated in the README and closed by #2382.
- `AddEncinaDataSubjectRights` registers `TryAddSingleton<IOperationAuditStore, InMemoryOperationAuditStore>` (the ADR-050:104 `TryAdd` default, Development only in effect), so the existing DSR registration tests keep building with `ValidateOnBuild`; the application's own or a provider's store wins in any order.

</details>

<details>
<summary><strong>13. Errors, categories, EventIds and telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New codes through the categorized `EncinaErrors.Create(code, category, message)` overload (ADR-062, merged in #3599): `dsr.erasure_completion_unavailable` (`FailedPrecondition`), `dsr.erasure_cache_purge_failed` (`UpstreamFailure`), `dsr.erasure_cache_tenant_invalid` (`Validation`), `dsr.erasure_cache_prefix_nested` (`FailedPrecondition`), `dsr.erasure_audit_failed` (`UpstreamFailure`), `dsr.erasure_notification_failed` (`UpstreamFailure`); new range `ComplianceDSRErasure = (320, 329)`; a tenant tag on the new counter when multi-tenancy is on** | Follows ADR-062, the family EventId block and ADR-054 D4 | The existing DSR counters keep the tenant as an activity attribute only (a documented divergence, #906) |
| **B) Reuse `dsr.erasure_failed`** | No new codes | Hides which step failed |

### Chosen Option: **A**

### Rationale

- **Class:** Trivial
- **Settled by:** ADR-062 (every new code states its category and uses the categorized overload), ADR-054 D4 (tenant tag when multi-tenancy is enabled) and the EventId block 300-349 assigned to this family in `artifacts/decisions/plan-batch/eventid-blocks.md`; AGENTS.md section 7 (register the range, pack ids from the first). The two cache codes that exist only under the recommended options of Design Choices 5 and 6 are added by the PR that implements them (PR 4).

</details>

---

## Implementation Phases

### Phase 1: Real request id on `ErasureRequest` and the EventId range (pull request 1, 13 files)

<details>
<summary><strong>Tasks</strong></summary>

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Compliance.DataSubjectRights/Requests/ErasureRequest.cs` | modify (`string DSRRequestId` positional member) |
| 2 | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs` | modify (the notification carries the real id; the fabricated `dsr-{guid}` is removed; the fake `field-N` list stays until PR 7 replaces the notification) |
| 3 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`ComplianceDSRErasure = (320, 329)`) |
| 4 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (map `Encina.Compliance.DataSubjectRights` to `ComplianceDSR` and `ComplianceDSRErasure`) |
| 5 | `src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs` | modify (2 construction sites; new test: the notification carries the request id) |
| 8 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/DataSubjectRightsDiagnosticsErrorMessageLeakTests.cs` | modify (2 sites) |
| 9 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/DataSubjectRightsDiagnosticsSubjectIdLeakTests.cs` | modify (1 site) |
| 10 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/ModelRecordTests.cs` | modify (1 site) |
| 11 | `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` | modify (`mechanical-fixer`; targets for `Requests/ErasureRequest.cs`, see Phase 5 table of coverage targets) |
| 12 | `changelog.d/2407-erasure-request-carries-the-dsr-request-id.changed.md` | new (`mechanical-fixer`) |
| 13 | `src/Encina.Compliance.DataSubjectRights/README.md` | modify (`docs-writer`: the `ErasureRequest` sample at line 109 gets the request id) |

Tasks: the four test files are found with `Select-String -Pattern 'new ErasureRequest\('` (6 sites, none in `src/`); after the change `Select-String` finds no construction without the id. Guard tests are not needed: the record has no validated parameter, and `SubjectId` stays unvalidated as today.

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
- PublicAPI.Unshipped.txt, the manifest and the changelog fragment go through mechanical-fixer; the README sample through docs-writer.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs
```

</details>

### Phase 2: Encina.Caching prerequisites of the purge (pull request 2, 9 files)

<details>
<summary><strong>Tasks</strong></summary>

This phase implements the recommended options of Design Choices 5 and 6 (if the maintainer picks option A of Design Choice 6, PR 2 shrinks to the `EscapeSegment` part; if C, it is dropped).

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`IdempotencyKeyPrefix = "sm-idempotency"`, `LockKeyPrefix = "sm-lock"`, XML docs) |
| 2 | `src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs` | modify (`EscapeSegment` becomes public and checks its argument with `ArgumentNullException.ThrowIfNull`: the DSR purge step in another assembly must build the same tenant segment as `DefaultCacheKeyGenerator`; the other option, an `InternalsVisibleTo` entry for `Encina.Compliance.DataSubjectRights`, would expose every internal of the package) |
| 3 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 4 | `tests/Encina.UnitTests/Caching/Base/CachingOptionsTests.cs` | modify (defaults) |
| 5 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify (defaults) |
| 6 | `tests/Encina.UnitTests/Caching/CacheLogRedactionTests.cs` | modify (`EscapeSegment` as public API: `%`, `:` escaped, injective for `a:b` vs `a%3Ab`) |
| 7 | `tests/Encina.GuardTests/Caching/CacheKeyRedactorGuardTests.cs` | new (`EscapeSegment(null!)` throws `ArgumentNullException`) |
| 8 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`; see the targets table) |
| 9 | `changelog.d/2407-caching-non-nested-default-prefixes.changed.md` | new (`mechanical-fixer`) |

Tasks: `git grep -n -F "sm:idempotency"` and `-F "sm:lock"` on `origin/main` find only `CachingOptions.cs` and the four test lines above (the two `DistributedIdempotencyPipelineBehaviorTests.cs` files set the prefix explicitly and do not change); no documentation page types the old defaults.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged. The DSR cache purge (PR 4) removes "{KeyPrefix}:*" in single-tenant mode, which today also matches the
idempotency ("sm:idempotency") and lock ("sm:lock") keys, and it needs CacheKeyRedactor.EscapeSegment from another assembly.

TASK:
Change the two default prefixes so they do not nest under KeyPrefix, make EscapeSegment public with a null check, and test both.

KEY RULES:
- Pre-1.0: no compatibility alias for the old defaults.
- A public method with a reference-type parameter gets a guard test in Encina.GuardTests.
- PublicAPI.Unshipped.txt, the manifest and the changelog fragment go through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Caching/Configuration/CachingOptions.cs
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- tests/Encina.UnitTests/Caching/CachingOptionsTests.cs
```

</details>

### Phase 3: Completion port, step contract and service wiring (pull request 3, 15 files)

<details>
<summary><strong>Tasks</strong></summary>

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Compliance.DataSubjectRights/Abstractions/IErasureCompletion.cs` | new (port and the public record `ErasureCompletionContext(string DSRRequestId, string SubjectId, string? TenantId, ErasureResult Result, string? FailureCode)`) |
| 2 | `src/Encina.Compliance.DataSubjectRights/Abstractions/IErasureCompletionStep.cs` | new (Design Choice 11) |
| 3 | `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultErasureCompletion.cs` | new (constructor `(IEnumerable<IErasureCompletionStep> steps, ILogger<DefaultErasureCompletion> logger)`, never changed by later pull requests) |
| 4 | `src/Encina.Compliance.DataSubjectRights/DSRErrors.cs` | modify (`ErasureCompletionUnavailableCode`, `FailedPrecondition`) |
| 5 | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs` | modify (trailing `IErasureCompletion? completion = null`; call it after a successful erase; with `completion` null return `Left(ErasureCompletionUnavailable)`) |
| 6 | `src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs` | modify (`TryAddScoped<IErasureCompletion, DefaultErasureCompletion>`) |
| 7 | `src/Encina.Compliance.DataSubjectRights/Diagnostics/ErasureCompletionLog.cs` | new (ids 320 `CompletionUnavailable` Error, 321 `StepFailed` Error, error code only) |
| 8 | `src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/DefaultErasureCompletionTests.cs` | new (ordering by `Order`; `RunsAfterFailure` steps run after a `Left` and receive the failure code; first `Left` returned; skipped steps; DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 10 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs` | modify (the erasure success test passes a completion substitute; new tests: a `Left` of the completion fails the request; `completion` null returns `Left`) |
| 11 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/DataSubjectRightsDiagnosticsSubjectIdLeakTests.cs` | modify (the erasure test at line 103 reaches the completion, so it passes a substitute) |
| 12 | `tests/Encina.GuardTests/Compliance/DataSubjectRights/ErasureCompletionGuardTests.cs` | new (constructor of `DefaultErasureCompletion`, `CompleteAsync` null context; null steps) |
| 13 | `tests/Encina.ContractTests/Compliance/DataSubjectRights/IErasureCompletionContractTests.cs` | new (real `DefaultErasureCompletion` with fake steps: "a `Left` from any step fails the call, nothing is swallowed", ordering, failure-code propagation) |
| 14 | `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/2407-dsr-erasure-completion-port.added.md` | new (`mechanical-fixer`) |

Tasks: `DefaultDSRServiceMethodGuardTests.cs` and the two error-message-leak tests that stop at an executor failure are unchanged (verified on `origin/main`: `ArgumentNullException.ThrowIfNull(request)` is line 626, before the completion call; the leak tests fail the executor first). A `Left` of the completion fails the request (counted as failed, logged by code); the erasure itself is not undone and the request can be repeated. No step is registered yet: with no steps the completion returns `Right`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged: ErasureRequest carries DSRRequestId and the range ComplianceDSRErasure (320-329) is registered.
DefaultDSRService has 31 construction sites in 17 test files, so the new collaborator is a trailing optional parameter.

TASK:
Create IErasureCompletion, ErasureCompletionContext, IErasureCompletionStep and DefaultErasureCompletion (steps sorted by Order;
steps with RunsAfterFailure run after a Left and receive the first error code; the first Left is returned), the error code, the
log class (ids 320-321) and the registration; call the completion from HandleErasureAsync after a successful EraseAsync.

KEY RULES:
- A Left from any step fails the request; nothing is swallowed; completion null returns Left (fail closed).
- Categorized overload of EncinaErrors.Create (FailedPrecondition); never put EncinaError.Message in a log, tag or audit entry.
- The completion is scoped; never inject a scoped service into a singleton.
- Tests instantiate real classes; ValidateOnBuild and ValidateScopes DI test; guard and contract tests as listed.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina.Compliance.DataSubjectRights/DSRErrors.cs
- src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs
```

</details>

### Phase 4: Cache purge step (pull request 4, 10 files)

<details>
<summary><strong>Tasks</strong></summary>

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Compliance.DataSubjectRights/Erasure/CachePurgeErasureStep.cs` | new (`Order = 10`; constructor `(ICacheProvider cache, IOptions<CachingOptions> cachingOptions, ILogger<CachePurgeErasureStep> logger)`) |
| 2 | `src/Encina.Compliance.DataSubjectRights/DSRErrors.cs` | modify (`ErasureCachePurgeFailedCode` `UpstreamFailure`, `ErasureCacheTenantInvalidCode` `Validation`, `ErasureCachePrefixNestedCode` `FailedPrecondition`) |
| 3 | `src/Encina.Compliance.DataSubjectRights/Diagnostics/ErasureCompletionLog.cs` | modify (ids 322 `CachePurged` Debug, 323 `CachePurgeFailed` Warning, exception through `ForLogging()`) |
| 4 | `src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs` | modify (`TryAddEnumerable(ServiceDescriptor.Scoped<IErasureCompletionStep, CachePurgeErasureStep>())`) |
| 5 | `src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/CachePurgeErasureStepTests.cs` | new (including the registration test with `ValidateOnBuild` and `ValidateScopes`) |
| 7 | `tests/Encina.GuardTests/Compliance/DataSubjectRights/ErasureCompletionGuardTests.cs` | modify (constructor and `ExecuteAsync` of the step) |
| 8 | `tests/Encina.IntegrationTests/Compliance/DataSubjectRights/DSRErasureCachePurgeIntegrationTests.cs` | new (one collection: `[Collection(RedisCollection.Name)]`, `RedisFixture` of `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs:68`; the step against `RedisCacheProvider`: the tenant's entry is gone, another tenant's entry and an idempotency key remain) |
| 9 | `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` | modify (`mechanical-fixer`) |
| 10 | `changelog.d/2407-dsr-erasure-purges-caches.added.md` | new (`mechanical-fixer`) |

Tasks: the step builds `"{KeyPrefix}:t:{CacheKeyRedactor.EscapeSegment(tenantId)}:*"` and removes it and the DSR restriction key. It never reads `EncinaError.Message`. Checks, each a `Left` with nothing purged: a tenant id containing a glob metacharacter (`*`, `?`, `[`, `]`, `\`) -> `dsr.erasure_cache_tenant_invalid` (Design Choice 5); a multi-tenant context with no tenant -> `DSRErrors.TenantRequired("HandleErasure")` (fail closed, never `{KeyPrefix}:*`); in single-tenant mode an idempotency or lock prefix that starts with `{KeyPrefix}:` -> `dsr.erasure_cache_prefix_nested` (Design Choice 6). A thrown provider exception becomes `Left(dsr.erasure_cache_purge_failed)`; the log carries the exception type through `ForLogging()` and the code only. The three checks are separate private methods so every method stays at CRAP 10 or below.

The integration test has a single `[Collection]` (an xUnit class belongs to exactly one) and uses the shared Redis fixture; it never creates a per-class container. The Memory provider's case-insensitive over-eviction is asserted by a unit test with the real `MemoryCacheProvider`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phases 1 to 3 are merged: DefaultErasureCompletion runs the registered IErasureCompletionStep services; none is registered yet.
Encina.Caching exposes a public CacheKeyRedactor.EscapeSegment and non-nested default prefixes (Phase 2).

TASK:
Add CachePurgeErasureStep (Order 10): purge the tenant namespace through ICacheProvider.RemoveByPatternAsync and the DSR restriction key.

KEY RULES:
- Reject glob metacharacters in the tenant id, a missing required tenant, and nested idempotency/lock prefixes: Left, nothing purged.
- Never fall back to "{KeyPrefix}:*" when a tenant is required. Exceptions become Left by exception type only.
- Categorized EncinaErrors.Create (ADR-062); EventIds 322-323 only; no tenant value, key or pattern in logs or tags.
- Integration test: one shared Redis collection fixture; never a per-class container.

REFERENCE FILES:
- src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
```

</details>

### Phase 5: Audit step, startup validation and removal of `TrackAuditTrail` (pull request 5, 15 files)

<details>
<summary><strong>Tasks</strong></summary>

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Compliance.DataSubjectRights/Encina.Compliance.DataSubjectRights.csproj` | modify (`ProjectReference` to `Encina.Security.Audit`; no cycle: it references `Encina`, `Encina.DomainModeling` and `Encina.Messaging`) |
| 2 | `src/Encina.Compliance.DataSubjectRights/Erasure/AuditErasureStep.cs` | new (`Order = 20`, `RunsAfterFailure = true`) |
| 3 | `src/Encina.Compliance.DataSubjectRights/Erasure/ErasureStartupValidationHostedService.cs` | new (Design Choice 12; audit store part) |
| 4 | `src/Encina.Compliance.DataSubjectRights/DSRErrors.cs` | modify (`ErasureAuditFailedCode`, `UpstreamFailure`) |
| 5 | `src/Encina.Compliance.DataSubjectRights/Diagnostics/ErasureCompletionLog.cs` | modify (ids 324 `AuditRecorded` Debug, 325 `AuditFailed` Error code only, 326 `StartupValidationFailed` Error, the missing service type name) |
| 6 | `src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs` | modify (`TryAddEnumerable` of the audit step, `TryAddSingleton<IOperationAuditStore, InMemoryOperationAuditStore>`, the hosted service) |
| 7 | `src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs` | modify (delete `TrackAuditTrail`, ADR-036:66) |
| 8 | `src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`; also removes the `TrackAuditTrail` lines 354-355) |
| 9 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/AuditErasureStepTests.cs` | new (entry fields, no subject value anywhere, outcome mapping, failing store, failure code after a failed purge, registration test with `ValidateOnBuild` and `ValidateScopes`) |
| 10 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/ErasureStartupValidationTests.cs` | new (store missing, in-memory default in and outside Development, missing `IHostEnvironment`, real store) |
| 11 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/DataSubjectRightsOptionsExtendedTests.cs` | modify (remove the two `TrackAuditTrail` tests, lines 24, 71-75) |
| 12 | `tests/Encina.GuardTests/Compliance/DataSubjectRights/ErasureCompletionGuardTests.cs` | modify (audit step and hosted service constructors) |
| 13 | `tests/Encina.IntegrationTests/Compliance/DataSubjectRights/DSRErasureAuditIntegrationTests.cs` | new (one collection: `[Collection(MartenCollection.Name)]`, `MartenOperationAuditStore`: an audit entry exists after `HandleErasureAsync` and holds no subject value) |
| 14 | `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/2407-dsr-erasure-records-an-audit-entry.added.md` | new (`mechanical-fixer`) |

Tasks: the step builds the `OperationAuditEntry` of Design Choice 7 and awaits `IOperationAuditStore.RecordAsync`; a `Left` becomes `dsr.erasure_audit_failed`. The startup check resolves `IOperationAuditStore` and throws `InvalidOperationException` naming the missing or in-memory type. `Encina.Security.Audit`'s `TenantId` filter is unchanged; the entry carries `TenantId` from the context. The integration test has one collection (Marten); the purge is covered by Phase 4's Redis test, so no test joins two collections.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Phases 1 to 4 are merged. IOperationAuditStore (Encina.Security.Audit) has RecordAsync.

TASK:
Add AuditErasureStep (Order 20, RunsAfterFailure), the startup validation hosted service, the ProjectReference, and delete
DataSubjectRightsOptions.TrackAuditTrail with its tests.

KEY RULES:
- The entry holds the request id, counts and the error code only: no subject id, no ErrorMessage, no payloads.
- Await the store; a Left fails the request with dsr.erasure_audit_failed (UpstreamFailure).
- Startup fails, by service type name, when no IOperationAuditStore is registered or the in-memory default is registered outside Development.
- Integration tests use the shared Marten collection fixture; never a per-class container.

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
- src/Encina.Security.Audit/OperationAuditEntry.cs
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs
- tests/Encina.IntegrationTests/Compliance/DataSubjectRights/DSRTenantLifecycleIntegrationTests.cs
```

</details>

### Phase 6: Outbox notification step (pull request 6, 12 files)

<details>
<summary><strong>Tasks</strong></summary>

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Compliance.DataSubjectRights/Notifications/ErasureNotification.cs` | new (`INotification`; fields per Design Choice 9) |
| 2 | `src/Encina.Compliance.DataSubjectRights/Erasure/NotifyErasureStep.cs` | new (`Order = 30`; `OutboxOrchestrator.AddAsync` then `IOutboxStore.SaveChangesAsync`) |
| 3 | `src/Encina.Compliance.DataSubjectRights/Erasure/ErasureStartupValidationHostedService.cs` | modify (`OutboxOrchestrator` and `IOutboxStore` presence) |
| 4 | `src/Encina.Compliance.DataSubjectRights/DSRErrors.cs` | modify (`ErasureNotificationFailedCode`, `UpstreamFailure`) |
| 5 | `src/Encina.Compliance.DataSubjectRights/Diagnostics/ErasureCompletionLog.cs` | modify (ids 327 `NotificationQueued` Debug, 328 `NotificationFailed` Error code only) |
| 6 | `src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs` | modify (`TryAddEnumerable` of the notify step) |
| 7 | `src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/NotifyErasureStepTests.cs` | new (payload fields, both calls awaited, a `Left` of either, no subject value under option A, registration test with `ValidateOnBuild` and `ValidateScopes`) |
| 9 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/ErasureStartupValidationTests.cs` | modify (outbox missing) |
| 10 | `tests/Encina.GuardTests/Compliance/DataSubjectRights/ErasureCompletionGuardTests.cs` | modify (`NotifyErasureStep` constructor and `ExecuteAsync`) |
| 11 | `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/2407-dsr-erasure-notifies-through-the-outbox.added.md` | new (`mechanical-fixer`) |

Tasks: the step adds the notification to the outbox and saves; a `Left` of either call is `dsr.erasure_notification_failed`. After this pull request both the old best-effort publish and the outbox notification exist; PR 7 removes the old one. No integration test in this issue: the step has no SQL and the outbox store implementations are covered by their own provider tests; the manifest target says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
Phases 1 to 5 are merged. DefaultErasureCompletion purges caches and records the audit entry.

TASK:
Add ErasureNotification and NotifyErasureStep (Order 30), the outbox presence check, the error code and EventIds 327-328.

KEY RULES:
- OutboxOrchestrator.AddAsync never saves; call IOutboxStore.SaveChangesAsync and treat a Left of either as failure.
- Do not describe this as atomic or "same transaction": the row is saved after the erasure, outside its transaction.
- Follow Design Choice 9 for the payload (no subject id under the recommended option).
- Category UpstreamFailure for dsr.erasure_notification_failed (ADR-062).

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs
- src/Encina.Messaging/Outbox/IOutboxStore.cs
- src/Encina.Compliance.DataSubjectRights/Notifications/DataErasedNotification.cs
```

</details>

### Phase 7: Remove the best-effort erasure publish (pull request 7, 7 files)

<details>
<summary><strong>Tasks</strong></summary>

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Compliance.DataSubjectRights/Notifications/DataErasedNotification.cs` | delete (its only producer was `HandleErasureAsync`) |
| 2 | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs` | modify (remove the erasure `PublishNotificationAsync` call and the fake field list) |
| 3 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/NotificationRecordTests.cs` | modify (remove the `DataErasedNotification` tests, lines 12-33) |
| 4 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs` | modify (remove the erasure publish expectations) |
| 5 | `src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`; removes the `DataErasedNotification` lines) |
| 6 | `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` | modify (`mechanical-fixer`; removes the `Notifications/DataErasedNotification.cs` entry) |
| 7 | `changelog.d/2407-dsr-erasure-publish-replaced-by-the-outbox.removed.md` | new (`mechanical-fixer`) |

Tasks: `DataErasedNotification` is searched with `git grep` in `src/`, `tests/` and `docs/` before the deletion (on `origin/main`: `NotificationRecordTests.cs`, `DefaultDSRService.cs`, the PublicAPI file, `docs/INVENTORY.md:5963` and `docs/features/data-subject-rights.md:544-552,784`; the two docs files are PR 9). The best-effort publish for rectification and restriction is untouched.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
Phases 1 to 6 are merged: the outbox notification exists, the old best-effort publish still runs.

TASK:
Delete DataErasedNotification and the erasure PublishNotificationAsync call; fix the tests that construct or expect it.

KEY RULES:
- Search src/, tests/ and docs/ for DataErasedNotification first; every hit is handled in this pull request or in the documentation pull request.
- Rectification and restriction publishes stay.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- tests/Encina.UnitTests/Compliance/DataSubjectRights/NotificationRecordTests.cs
```

</details>

### Phase 8: Telemetry (pull request 8, 8 files)

<details>
<summary><strong>Tasks</strong></summary>

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs` | modify (activity `dsr.erasure.completion`; `Counter<long> encina.dsr.erasure.steps`) |
| 2 | `src/Encina.Compliance.DataSubjectRights/Erasure/DefaultErasureCompletion.cs` | modify (child span and counter per step; no constructor change) |
| 3 | `src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 4 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/Erasure/ErasureCompletionTelemetryTests.cs` | new (spans, counter tags, tenant tag only when multi-tenancy is on) |
| 5 | `tests/Encina.UnitTests/Compliance/DataSubjectRights/DataSubjectRightsDiagnosticsSubjectIdLeakTests.cs` | modify (the new spans and the counter never carry the subject id, a cache pattern or a key) |
| 6 | `tests/Encina.GuardTests/Compliance/DataSubjectRights/ErasureCompletionGuardTests.cs` | modify (new public diagnostics members with reference-type parameters) |
| 7 | `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` | modify (`mechanical-fixer`) |
| 8 | `changelog.d/2407-dsr-erasure-completion-telemetry.added.md` | new (`mechanical-fixer`) |

Tasks: tags `step` (`purge`, `audit`, `notify`) and `outcome` (`success`, `failed`, `skipped`), and the tenant tag only when multi-tenancy is on (ADR-054 D4). No subject id, cache pattern or key is a tag or attribute.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
The three steps exist (phases 4 to 6). Telemetry is the last code pull request.

TASK:
Add the completion activity, the step spans and the step counter to DataSubjectRightsDiagnostics and call them from DefaultErasureCompletion.

KEY RULES:
- Tenant tag only when multi-tenancy is on (ADR-054 D4); no subject id, cache pattern, key or EncinaError.Message in telemetry.
- Do not change the constructor of DefaultErasureCompletion.
- CRAP of every added method at most 10.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs
- tests/Encina.UnitTests/Compliance/DataSubjectRights/DataSubjectRightsDiagnosticsSubjectIdLeakTests.cs
```

</details>

### Phase 9: Documentation (pull request 9, 4 files)

<details>
<summary><strong>Tasks</strong></summary>

1. **`docs-writer`**: `src/Encina.Compliance.DataSubjectRights/README.md` (the completion, mandatory steps, fail-closed behavior, the cache purge scope and its Hybrid limitation until #2402, the audit entry shape, the outbox requirement, `TrackAuditTrail` removed from lines 50 and 232, the decorated in-memory store limit), `docs/features/data-subject-rights.md` (lines 544-552 and 784 name `DataErasedNotification`; line 581 sets `TrackAuditTrail`), `docs/INVENTORY.md` (line 5963), and `ROADMAP.md` only if the v0.17.0 text changes (4 files).
2. XML documentation with examples on every new public type is part of the code pull requests.
3. **Verification per pull request**: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`; the integration test with Docker; per-flag coverage with `coverage-report.cs` and `--check-justifications`; the CRAP table of the changed methods.
4. **Follow-up issue files** (Next Steps).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT:
Code and tests of the eight code pull requests exist. The documentation pull request remains.

TASK:
Through docs-writer, update the DSR README, the feature page and INVENTORY.md; confirm the changelog fragments exist.

KEY RULES:
- Documentation never types coverage figures; it cites cov: markers.
- State the Hybrid limitation (#2402) and that the notification is saved after the erasure, outside its transaction.
- Link ADRs and SPEC-002; one Diataxis quadrant per page.

REFERENCE FILES:
- docs/features/data-subject-rights.md
- .claude/skills/encina-docs/SKILL.md
- src/Encina.Compliance.DataSubjectRights/README.md
```

</details>

### Coverage targets (per flag, every new or touched `src/` file)

Entered by `mechanical-fixer` in the pull request that adds the tests. A flag at 0 has a reason that the tests of that flag cannot reach the code; coverage is per flag.

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|:----:|:-----:|:--------:|:--------:|:-----------:|---------------|
| `Requests/ErasureRequest.cs` | 70 | 0 | 0 | 0 | 0 | Positional record: the tests construct and read it; compiler-generated equality members are not worth direct tests; no validated parameter, so no guard test can reach code |
| `Abstractions/IErasureCompletion.cs` | 70 | 0 | 0 | 0 | 0 | One interface (no executable lines) and the `ErasureCompletionContext` record with compiler-generated members; contract tests drive the port through `DefaultErasureCompletion` |
| `Abstractions/IErasureCompletionStep.cs` | 0 | 0 | 0 | 0 | 0 | Interface with no executable lines; behaviour is covered through the implementations |
| `Erasure/DefaultErasureCompletion.cs` | 95 | 25 | 85 | 0 | 0 | Sequential steps with explicit branches driven by fake steps; guard reaches the constructor and context checks; contract runs the real class with fake steps; no non-trivial invariant needs a property test |
| `Erasure/CachePurgeErasureStep.cs` | 95 | 20 | 0 | 0 | 50 | Pattern building and three checks, all unit tested; the Redis integration test reaches the real removal |
| `Erasure/AuditErasureStep.cs` | 95 | 20 | 0 | 0 | 50 | Builds one entry; the Marten integration test writes it to the real store |
| `Erasure/NotifyErasureStep.cs` | 95 | 20 | 0 | 0 | 0 | Two awaited calls; unit tests substitute the orchestrator and the store; the outbox store tests of the messaging packages own the real store |
| `Erasure/ErasureStartupValidationHostedService.cs` | 90 | 20 | 0 | 0 | 0 | Resolves services in a scope; unit tests with and without each service |
| `Notifications/ErasureNotification.cs` | 70 | 0 | 0 | 0 | 0 | Positional record; compiler-generated members are not tested directly; no validated parameter |
| `Diagnostics/ErasureCompletionLog.cs` | 70 | 0 | 0 | 0 | 0 | Generated logger bodies run only when a level is enabled; no parameters a guard test could reach |
| `Diagnostics/DataSubjectRightsDiagnostics.cs` | keeps 100 | 0 | keeps 73 | keeps 61 | keeps 65 | Existing targets are not lowered (target-ratchet); the new members are driven by `ErasureCompletionTelemetryTests`; guard 0 because the new members take only strings and enums the unit tests already pass |
| `Services/DefaultDSRService.cs` | keeps 97 | keeps 7 | keeps 52 | keeps 38 | keeps 61 | Existing targets are not lowered; the new branch is covered by the unit tests of PR 3 |
| `DSRErrors.cs` | keeps 100 | 0 | 0 | 0 | 0 | Every new factory is a pure function asserted for its code and category |
| `ServiceCollectionExtensions.cs` | keeps 100 | keeps 100 | 0 | 0 | 0 | The new registrations are covered by the registration tests and the guard tests that call the extension |
| `DataSubjectRightsOptions.cs` | keeps 100 | 0 | 0 | 0 | 0 | The deleted property leaves plain properties that are each asserted |
| `Caching: Configuration/CachingOptions.cs` | 100 | keeps 85 | 0 | 0 | 0 | New defaults asserted by the two options tests; guard keeps its existing target |
| `Caching: KeyGeneration/CacheKeyRedactor.cs` | keeps 100 | 10 | 0 | 0 | 0 | `EscapeSegment` gains one `ThrowIfNull` line reached by the new guard test |

**Property, load and benchmark**: no property test (the injectivity of the tenant segment is the existing `CacheKeyRedactor.EscapeSegment` behavior asserted by unit tests with hostile tenant ids); load and benchmark tests are not written (an erasure is a rare, serial operation); the manifest `reason` of each flag says so.

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| GDPR Art. 17 | Erasure must remove the data wherever it is held, caches included |
| GDPR Art. 19 | The controller communicates the erasure to each recipient |
| GDPR Art. 5(2), Art. 30 | Accountability: the erasure is demonstrable through an audit entry |
| SPEC-002 DEC-006 | Compliance gates fail closed on missing context |
| ADR-036 | Operation audit store abstraction; decision 4 deletes `TrackAuditTrail` and `DSRAuditEntry` |
| ADR-050 (line 77, sub-decisions 2 and 5), #2381/#2382 | DSR requirements; startup validation |
| ADR-054 D4 and D6 | Tenant tag when multi-tenancy is on; no cache keys in telemetry |
| ADR-062 (merged, #3599) | Error category of every new code |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `DefaultDSRService.HandleErasureAsync` | `src/Encina.Compliance.DataSubjectRights/Services` | The erasure entry point |
| `ICacheProvider.RemoveByPatternAsync` | `src/Encina.Caching/Abstractions` | The purge |
| `DefaultCacheKeyGenerator`, `CacheKeyRedactor.EscapeSegment`, `CachingOptions` | `src/Encina.Caching` | Key shape, tenant segment, prefixes |
| `IOperationAuditStore`, `OperationAuditEntry`, `InMemoryOperationAuditStore` | `src/Encina.Security.Audit` | The audit entry and the in-memory default check |
| `OutboxOrchestrator`, `IOutboxStore` | `src/Encina.Messaging/Outbox` | The durable notification |
| `CryptoShreddingStartupValidationHostedService` | `src/Encina.Marten.GDPR` | Template of the startup check |
| `DataSubjectRightsDiagnostics`, `DSRLogMessages` | `src/Encina.Compliance.DataSubjectRights/Diagnostics` | Activity source, meter, logs |
| `DSRErrors.TenantRequired` | `src/Encina.Compliance.DataSubjectRights/DSRErrors.cs` | Fail-closed tenant error |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.DataSubjectRights` (new range `ComplianceDSRErasure`) | 320-329 (from block 300-349) | Registered in PR 1; packed in merge order: PR 3 320-321, PR 4 322-323, PR 5 324-326, PR 6 327-328; 329 stays free. `ComplianceDSR` 8300-8349 is full (sparse ids, no contiguous free block). #2408 owns 300-319 |

### Estimated File Count

| Pull request | Production code | Tests | Configuration (PublicAPI, manifest, changelog) | Documentation | Total |
|--------------|:---------------:|:-----:|:----------------------------------------------:|:-------------:|:-----:|
| PR 1 request id, EventId range | 3 | 5 | 4 | 1 | 13 |
| PR 2 Caching prerequisites | 2 | 4 | 3 | 0 | 9 |
| PR 3 completion port and wiring | 7 | 5 | 3 | 0 | 15 |
| PR 4 cache purge step | 4 | 3 | 3 | 0 | 10 |
| PR 5 audit step, startup, `TrackAuditTrail` | 7 | 5 | 3 | 0 | 15 |
| PR 6 outbox notification step | 6 | 3 | 3 | 0 | 12 |
| PR 7 remove the best-effort publish | 2 | 2 | 3 | 0 | 7 |
| PR 8 telemetry | 2 | 3 | 3 | 0 | 8 |
| PR 9 documentation | 0 | 0 | 0 | 4 | 4 |
| **Total** | **33** | **30** | **25** | **5** | **93** |

Each pull request builds and passes alone; none uses a type, project or log class created by a later one (PR 8 telemetry follows the steps it measures). The sums: 13 + 9 + 15 + 10 + 15 + 12 + 7 + 8 + 4 = 93.

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
2. Encina.Caching: non-nested default prefixes and a public EscapeSegment.
3. IErasureCompletion (one trailing optional parameter of DefaultDSRService) runs IErasureCompletionStep services in order:
   purge (10), audit (20, runs after a failure), notify (30). The three steps are mandatory; there are no flags.
4. Cache purge removes the tenant namespace pattern through ICacheProvider.RemoveByPatternAsync (no tag API exists; #271).
5. Audit entry through IOperationAuditStore with the request id, counts and the error code only; TrackAuditTrail is deleted.
6. ErasureNotification through OutboxOrchestrator.AddAsync and IOutboxStore.SaveChangesAsync; no recipients in the payload.
7. Startup validation of the audit store (not the in-memory default outside Development) and the outbox.
8. New error codes with ADR-062 categories; telemetry without subject data.
Eight code pull requests plus one documentation pull request.

KEY PATTERNS:
- A Left from any step fails the request; the erasure is not undone and the request can be repeated.
- Fail closed: missing tenant when required, missing completion, missing audit store or outbox at startup.
- Never claim atomicity: the audit entry and the outbox row are written after the erasure, outside its transaction.
- No subject id, error message or cache pattern in logs, tags, attributes or audit entries.
- Apply the Maintainer Decisions section for Design Choices 4 to 7 and 9 to 11 once the maintainer has answered.

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
| 1 | Caching | ✅ Include | The purge of the tenant's cached entries after an erasure (Design Choice 4). |
| 2 | OpenTelemetry | ✅ Include | A completion activity with step spans and a step counter; tenant tag when multi-tenancy is on (ADR-054 D4); no subject id or key (PR 8). |
| 3 | Structured Logging | ✅ Include | EventIds 320-328 in the new range 320-329, error codes and exception types only. |
| 4 | Health Checks | ❌ N/A | The existing DSR health check is unchanged; the startup check covers the new dependencies. |
| 5 | Validation | ✅ Include | The purge step validates the tenant id (glob metacharacters) before building a pattern; the erasure request itself is validated before it reaches the completion. |
| 6 | Resilience | ✅ Include | The notification is retried by the outbox; cache and audit failures fail the request, which can be repeated. |
| 7 | Distributed Locks | ❌ N/A | No new shared mutable state; repeating an erasure is idempotent. |
| 8 | Transactions | ✅ Include | The audit entry and the outbox row are saved after the erasure result, outside the erasure's transaction; a crash between them is recovered by repeating the request. |
| 9 | Idempotency | ✅ Include | The DSR request id is carried by the audit entry and the notification so a consumer deduplicates; the audit trail records each attempt. |
| 10 | Multi-Tenancy | ✅ Include | The purge pattern carries the escaped tenant segment and rejects ids that could widen it; the audit entry and the notification carry the tenant; a missing required tenant denies. |
| 11 | Module Isolation | ❌ N/A | Not module scoped. |
| 12 | Audit Trail | ✅ Include | One `IOperationAuditStore` entry per erasure attempt, awaited, mandatory (ADR-050 sub-decision 5). |

**Deferred items** (each a follow-up issue to be opened; drafts in `artifacts/decisions/plan-batch/followups/`):

- The processor-side handler of `ErasureNotification` (resolve recipients, notify each processor): `processor-erasure-notification-handler.md`.
- The `-` tenant segment collision in the DSR cache keys: `dsr-cache-key-tenant-segment-collision.md`.
- Case-insensitive, escape-less glob matching of the Memory cache provider (provider inconsistency): `memory-cache-pattern-case-insensitive.md`.
- The unused `DSRAuditEntry` record (ADR-036:66): `dsr-unused-audit-entry-record.md`.
- Retention of processed outbox rows: #1200 (open); under Design Choice 9 option B it becomes a prerequisite.
- Tag-based purge instead of the tenant namespace: waits for #271 (open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `IOperationAuditStore` and its stores | ✅ Available | ADR-036 stores exist |
| Outbox (`OutboxOrchestrator`, `IOutboxStore`) | ✅ Available | Opt-in (`UseOutbox`); the notification step requires it, and startup fails without it |
| ADR-062 error categories | ✅ Merged | PR #3599 |
| #2402 Hybrid `RemoveByPatternAsync` removes only tracked keys | ⏭️ Open bug | The purge is complete for Hybrid only after it is fixed |
| #271 Enhanced tag-based cache invalidation | ⏭️ Open feature | Enables a targeted purge (Design Choice 4, Option B) |
| #2382 Startup requirements validator | ⏭️ Open feature | Replaces the local startup check (Design Choice 12) and owns the explicit opt-out and decorated-store detection |
| #1200 Outbox retention | ⏭️ Open feature | Needed only under Design Choice 9 option B |
| #1188, #1283 Retention holds | ⏭️ Open | Change what an erasure may do before this completion runs; not prerequisites |
| Maintainer answers to Design Choices 4 to 7, 9, 10 and 11 | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 4, 5, 6, 7, 9, 10 and 11) when work starts, records the answers under `## Maintainer Decisions`, then briefs the eight code workers and the documentation worker in phase order (PR 2 changes or drops with the answer to Design Choice 6).
2. Open the follow-up issues from the drafts in `artifacts/decisions/plan-batch/followups/` and link them from #2407.
3. After the last pull request, comment on #2407 with the result and on #2408 with the namespace-purge decision.

---

## Maintainer Decisions

Recorded decision of 2026-10-17 (comment on #2407, applied in Design Choices 1, 8 and 12): "orchestrate inside `DefaultDSRService` through small ports (`IErasureCachePurger`, awaited `IOperationAuditStore` entry, `ErasureNotification` via the Outbox), a `Left` fails the request, startup fails when DSR has no audit store". Design Choices 4, 5, 6, 7, 9, 10 and 11 are Dilemmas; their answers are recorded here, numbered like the choices, when the maintainer decides.
