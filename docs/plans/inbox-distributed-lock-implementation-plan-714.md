# Implementation Plan: Per-Message Distributed Lock for Inbox Processing

> **Issue**: [#714](https://github.com/dlrivada/Encina/issues/714)
> **Type**: Feature
> **Complexity**: High (4 pull requests; one new pipeline behavior placed outside the transaction)
> **Estimated Scope**: ~300-380 lines of production code + ~1,100-1,400 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027 (lock providers); [ADR-048](../architecture/adr/048-inbox-record-vs-business-transaction.md) (inbox record versus business transaction); [ADR-050](../architecture/adr/050-modules-may-require-modules.md) (the Inbox row: row claiming or a lock when consumers run concurrently); [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-054](../architecture/adr/054-observability-conventions.md) D4; [ADR-062](../architecture/adr/062-error-category-contract.md)
> **Depends on**: the shared contract of the #713 plan (PRs 1 to 3) and #2667 (ADR-052 implementation)
> **Related**: #713 (owns the shared piece), #2401 (the inbox runs the handler twice on concurrent delivery: the atomic-claim alternative), #2184 (pipeline stages), #715, #716, #741 (tenant-scoped locks), #223 (auto-extend), #218 (fencing)

---

## Summary

The inbox gives idempotent processing, but two deliveries of the same message that arrive together both read "not processed" and both run the handler (the defect recorded in #2401). This plan adds an **opt-in per-message lock** so that only one delivery of a message id is inside the inbox at a time; the second waits, then reads the first one's cached response.

The placement is the point of this plan, because of ADR-048: the inbox runs **inside** the business transaction (stage `Idempotency` 1400 is inner to `Transaction` 1300) and its processed mark commits with the business effect. A lock released at the end of `InboxOrchestrator.ProcessAsync` would be released **before** the transaction commits, so a second delivery could take it, find the first delivery's record still unprocessed and run the handler again. The lock must therefore be held until the transaction ended:

1. **A new pipeline behavior**, `InboxLockPipelineBehavior<TRequest, TResponse>`, declared `[PipelineStage(PipelineStage.Transaction, Order = -200)]`: outer to `TransactionPipelineBehavior` and to the post-commit processors (default order -100), so the lock is released after the commit and after the post-commit processors.
2. **Same admission as the inbox**: it locks only for an `IIdempotentRequest` with a non-blank idempotency key that is not a nested dispatch (the predicate of `InboxPipelineBehavior`, extracted to one internal helper both behaviors call).
3. **Lock name** `encina:inbox:{sha256-hex of the message id}` (Design Choice 2): fixed length, deterministic, within the SQL Server `sp_getapplock` resource limit.
4. **Outcomes**: acquired runs the pipeline; contention after `InboxOptions.LockWait` returns `Left` `inbox.lock_contended` (category `Conflict`, ADR-062), so the broker redelivers and the next delivery finds the processed record; a lock `Left` is returned unchanged; the lock is never bypassed.
5. **Opt-in**: `config.UseDistributedLocking` (flag, adapter registration and startup check come from #713) together with `UseInbox`.
6. **Limits stated**: the lock is a lease without fencing (#218); a handler that outlives `InboxOptions.LockExpiry` (default 30 s) loses it (Design Choice 3); the guarantee of the inbox itself (ADR-048) is unchanged.

**Standards**: ADR-048 (the transaction boundary the lock must enclose); ADR-052; ADR-054 D4 (the tenant is a tag where a tenant exists: the request context carries `TenantId`); ADR-062 (`inbox.lock_contended` is `Conflict`); `AGENTS.md` section 3.

**Estimated scope**: 2 new production files, 8 modified, 1 `PublicAPI`, 1 manifest, 10 test files plus 11 integration files, 4 documentation files; 4 pull requests.

**Affected packages**: `Encina.Messaging` only. The ten inbox stores are not edited.

**Provider category**: Database (10) as consumers through the shared orchestrator and behavior (`IInboxStore`: ADO.NET x3, Dapper x3, EF Core, MongoDB), plus the integration proof per provider; Distributed locks as the engine through the #713 adapter.

**What the code does today** (`origin/main` at `db1e7f688`):

- `InboxPipelineBehavior<,>` (`src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs`) admits an `IIdempotentRequest`, skips a nested dispatch that has no key of its own, validates the key and calls `InboxOrchestrator.ProcessAsync`. The orchestrator reads `GetMessageAsync`, adds a new record (an independent write) or handles an existing one, and never calls `SaveChangesAsync`: the processed mark is enlisted in the business transaction (ADR-048).
- Ordering: `PipelineStage` exists (`src/Encina/Pipeline/Ordering/PipelineStage.cs`: `Transaction` 1300, `Idempotency` 1400, `Application` 1500) and `PipelineOrderResolver` is wired in `src/Encina/Core/ServiceCollectionExtensions.cs` (line 65); a behavior with no declared stage is placed in `Application`/0 (`PipelineOrderResolver.cs` lines 289 to 304), which is inner to `Transaction`. `TransactionPipelineBehavior` and `InboxPipelineBehavior` declare no stage today; #2184 gives them one. A behavior declaring `Transaction`/-200 is outer to both before and after that change.
- The idempotency key is `MessageId` in the store: `InboxMessageConfiguration` keys on it with `MaxLength(255)` (`src/Encina.EntityFrameworkCore/Inbox/InboxMessageConfiguration.cs`); the store has no tenant segment in the key.
- The SQL Server lock provider builds the resource as `lock:{resource}` or `{KeyPrefix}:lock:{resource}` (`SqlServerDistributedLockProvider.GetLockKey`), and `sp_getapplock` accepts a resource of at most 255 characters, so a raw 255-character message id cannot be used as the name.
- `InboxOptions` has `MaxRetries`, `MessageRetentionPeriod`, `PurgeInterval`, `EnableAutomaticPurge`, `PurgeBatchSize`. The issue's "All 13 database providers" is 10 in `AGENTS.md` section 5.
- The shared contract (`IMessagingLockProvider`, `MessagingLockGate`, `MessagingLockMarker`, `MessagingLockNames`, EventIds 7500-7503, the `MessagingLocking` range) is delivered by the #713 plan; this plan adds EventId 7504.
- Manifest `Encina.Messaging.json`: `Inbox/InboxOrchestrator.cs` unit 95, guard 15, integration 75; `InboxPipelineBehavior.cs` has its own entry that this plan keeps.

---

## Design Choices

<details>
<summary><strong>1. Where the lock is held: outside the transaction, inside the orchestrator, or an atomic claim instead (#2401)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new behavior at `Transaction`/-200, outer to the transaction behavior** | The lock spans the handler, the commit and the post-commit processors, which is what closes the race given ADR-048; no change to `InboxOrchestrator` or the stores | One more behavior in the pipeline; relies on the stage contract |
| **B) Inside `InboxOrchestrator.ProcessAsync` (the issue's "check-process-mark within the lock scope")** | Smallest diff; no pipeline ordering | Released before the business transaction commits: the second delivery can find the unprocessed record and run the handler while the first one's commit is pending; it closes only the check-then-add window |
| **C) No lock: an atomic claim in the store (#2401)** | Correct on every provider without a lock service; the handler runs once even with the in-memory default | A schema and SQL change on ten providers; the issue (#714) is not delivered; #2401 has no plan yet |

### Chosen Option: **A — a behavior outside the transaction** (recommended, pending the maintainer)

### Rationale

- A wins when the lock must hold until the commit (the guarantee the issue asks for); C wins for deployments that will not run a lock service and want exactly-once handling from the database alone; B is invalid for the stated goal because of the commit window, which is also why the issue's wording cannot be followed literally.
- A and C are complementary: the lock avoids the wasted second handler run and the contention, the claim is the database-level safety net if the lock expires. Under C alone the Phase 3 and 4 code of this plan is not built.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Lock name derived from the message id</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Always `encina:inbox:` plus the lowercase hex SHA-256 of the UTF-8 message id** | One code path; fixed 77-character name; any id length and any characters; the same id always gives the same name | Names in Redis or `sys.dm_tran_locks` are not readable; two ids could in theory share a name (a 2^-128 event whose only effect is two messages briefly serialising) |
| **B) The escaped id (`Uri.EscapeDataString`) when it fits (at most 150 characters), the hash otherwise** | Readable names for normal ids | Two code paths and two name shapes to test; the escaped branch has no `:` so the hash branch cannot clash with it, but that argument has to be maintained |

### Chosen Option: **A — always hash** (recommended, pending the maintainer)

### Rationale

- A wins for simplicity and a hard length bound (the SQL Server resource limit is 255 including the provider prefix, and ids can be 255 characters); B wins when operators read lock keys by hand to debug contention.
- Equal ids give equal names (the property that matters); a hash collision can only cause extra serialising, never a duplicate run. No tenant segment is added: the store identifies a message by its id alone, so the lock has exactly the store's identity. If a tenant-qualified inbox key lands, `MessagingLockNames` changes in one place; an application that lists the name in the tenant scoping of #741 gets `@{tenant}:{name}` with that plan's rules (a tenant `default` is an ordinary tenant).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What a second delivery does while the first holds the lock, and for how long the lock lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wait up to `InboxOptions.LockWait` (10 s), then `Left` `inbox.lock_contended`; `LockExpiry` 30 s (the issue's default), fixed** | The second delivery usually returns the first one's cached response instead of an error; bounded wait; no renewal loop | A handler longer than 30 s loses the lock while it runs (EventId 7503 logs it); the wait occupies a consumer thread for up to 10 s |
| **B) Do not wait: `Left` `inbox.lock_contended` at once; the broker redelivers** | No thread held | A redelivery storm for slow handlers; the cached-response case becomes a retry |
| **C) A lease that is renewed while the handler runs (#223)** | Long handlers keep the lock | Needs a renewal task and a way to cancel the handler on loss; belongs to #223 |

### Chosen Option: **A — bounded wait, fixed expiry** (recommended, pending the maintainer)

### Rationale

- A wins when handlers are short and brokers redeliver slowly; B wins when consumer threads are scarce; C wins for long handlers and is the same question as Dilemma 4 of the #713 plan, answered once for the family.
- Both timings are options (`LockExpiry`, `LockWait`), validated (`LockExpiry` positive, `LockWait` not above `LockExpiry`).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Failure semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A lock `Left` is returned unchanged (nothing runs); contention after the wait is `Left` `inbox.lock_contended`; with the flag on the lock is never bypassed** | ADR-052 and the fail-closed rule; the message is neither lost nor run unguarded; the broker redelivers | A broken lock backend stops inbox processing |
| **B) On a lock `Left`, run unlocked** | Processing continues | Duplicate handler runs exactly when coordination is down |

### Chosen Option: **A — fail closed**

### Rationale

- The behavior returns `Either`; cancellation propagates; the log carries the error code (7500 from the shared gate) and the request type, never the message id or `EncinaError.Message` (`AGENTS.md` section 3).

**Class:** Settled
**Settled by:** ADR-052 (contention versus fault, cancellation propagates), `AGENTS.md` section 3 (gates fail closed; errors are never swallowed)

</details>

<details>
<summary><strong>5. Which requests are locked</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Exactly the requests the inbox admits: `IIdempotentRequest` with a non-blank key, not a nested dispatch without its own key; a shared internal predicate used by both behaviors** | The lock and the inbox can never disagree; a nested dispatch is covered by its entry point's lock | Touches `InboxPipelineBehavior` (a small extraction) |
| **B) Lock every request that carries an idempotency key** | No change to the inbox behavior | Locks requests the inbox ignores; a nested dispatch would try to take the lock its parent holds and wait out `LockWait` |

### Chosen Option: **A — the inbox's own admission**

### Rationale

- A nested dispatch with no key of its own inherits none, so it is not admitted and never re-enters the lock its entry point holds; a nested dispatch that sets its own key is a different message id.

**Class:** Settled
**Settled by:** `src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs` (the admission contract in its remarks and in `Handle`), `RequestContextDispatchExtensions.IsNestedDispatch`, and non-reentrancy of the lock providers (a second acquire of a held name waits)

</details>

<details>
<summary><strong>6. Error code and category</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `inbox.lock_contended` in `InboxErrorCodes`, created with `ErrorCategory.Conflict`** | The host and broker adapters map it by category, not by code text (ADR-062 R3); "another delivery is processing this message" is a conflict | One more code |
| **B) `ErrorCategory.Unavailable`** | Suggests a retry | The service is available; the resource is busy |

### Chosen Option: **A — `Conflict`**

### Rationale

- A test asserts `GetCategory` returns `Conflict` (the pattern of the #741 plan for `lock.tenant_required`).

**Class:** Trivial
**Settled by:** ADR-062 (the `Conflict` category exists, `src/Encina/Errors/ErrorCategory.cs`, value 6; hosts never parse code text)

</details>

---

## Implementation Phases

### Phase 1: Names, Options, Error Code and the Shared Admission Predicate — PR 1

> **Goal**: Everything the behavior needs, with no pipeline change. Starts after PRs 1 and 2 of the #713 plan.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Locking/MessagingLockNames.cs`** (modify, file of #713) — `static string Inbox(string messageId)`: `ArgumentException.ThrowIfNullOrWhiteSpace`, then `"encina:inbox:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(messageId)))`.
2. **`Inbox/InboxOptions.cs`** (modify) — `TimeSpan LockExpiry` (30 s) and `TimeSpan LockWait` (10 s) with the file's setter checks; `Validate` rejects a `LockWait` greater than `LockExpiry`.
3. **`Inbox/InboxOrchestrator.cs`** (modify, `InboxErrorCodes`) — `const string LockContended = "inbox.lock_contended"`.
4. **`Inbox/InboxRequestAdmission.cs`** (new, internal static) — `bool TryGetKey(object request, IRequestContext context, out string key)` holding the two conditions now in `InboxPipelineBehavior.Handle`.
5. **`Inbox/InboxPipelineBehavior.cs`** (modify) — call the helper; behavior unchanged (the existing tests are the regression guard).
6. **`PublicAPI.Unshipped.txt`** (Messaging): the options properties and the error-code constant, through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #714: names, options, the error code and the shared admission predicate.

CONTEXT:
- The shared lock contract (MessagingLockNames, gate, marker, flag) comes from the plan of #713; add only the Inbox member.
- InboxPipelineBehavior admits a request when it is IIdempotentRequest and either has a key or is not a nested dispatch.

TASK:
Create the items of the Phase 1 tasks. Unit tests: name determinism and format (lowercase hex, 77 characters), a
property test that equal ids give equal names and that names never contain the raw id, options and Validate cases,
the admission helper for every combination, and the unchanged InboxPipelineBehavior tests.

KEY RULES:
- The lock name never contains the raw message id.
- Behavior of InboxPipelineBehavior must not change: run its existing tests.

REFERENCE FILES: src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs, src/Encina.Messaging/Inbox/InboxOptions.cs,
src/Encina.Messaging/Inbox/InboxOrchestrator.cs, docs/plans/outbox-distributed-lock-implementation-plan-713.md.
```

</details>

### Phase 2: The Lock Behavior and its Registration — PR 2

> **Goal**: The lock encloses the transaction.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Inbox/InboxLockPipelineBehavior.cs`** (new, public sealed, `IPipelineBehavior<TRequest, TResponse>`, `[PipelineStage(PipelineStage.Transaction, Order = -200)]`) — constructor `(IMessagingLockProvider? lockProvider, MessagingLockMarker? marker, InboxOptions options, ILogger<InboxLockPipelineBehavior<TRequest, TResponse>> logger)`; `Handle`: not admitted (`InboxRequestAdmission`) means `nextStep()`; otherwise `MessagingLockGate.TryEnterAsync(..., "inbox", MessagingLockNames.Inbox(key), options.LockExpiry, options.LockWait, tenantId: context.TenantId, ...)`; `Left` returned unchanged; `None` returns `Left` `inbox.lock_contended` created with `ErrorCategory.Conflict` and logs 7504 (request type, correlation id); `Some(handle)`: `await using` around `nextStep()`; after it, log 7503 when `handle.ExpiresAtUtc` passed.
2. **`Locking/MessagingLockGate.cs`** and **`Diagnostics/MessagingLockMetrics.cs`** (modify, files of #713) — an optional `string? tenantId` parameter; the counter adds the tag `tenant.id` only when it is non-null (ADR-054 D4: the context carries a tenant only when tenancy is on).
3. **`Locking/MessagingLockLog.cs`** (modify) — EventId 7504 `InboxLockContended` (Warning: request type, correlation id), inside the `MessagingLocking` range registered by #713.
4. **`MessagingServiceCollectionExtensions.cs`** (modify) — when `UseInbox` and `UseDistributedLocking` are both on: `services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InboxLockPipelineBehavior<,>))` next to the existing inbox behavior registration (line 327).
5. **`PublicAPI.Unshipped.txt`**: the behavior and its constructor, through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #714: the inbox lock behavior outside the transaction.

CONTEXT:
- ADR-048: the inbox is inside the business transaction and its processed mark commits with the business effect, so the
  lock must be held until the transaction ended. PipelineStage.Transaction is 1300; a lower Order is further out.
- The behavior must be a transient scoped dependency of the pipeline: take the port and marker from DI per request
  scope; the options are a singleton.

TASK:
Create InboxLockPipelineBehavior, extend the gate and metrics with the optional tenant id, add EventId 7504 and the
registration as listed. Unit tests: not admitted passes through, flag off passes through, acquired runs the next step
inside the lock and releases after it returns (assert with an ordering log), contended after the wait returns Left with
category Conflict and logs 7504, provider Left returned unchanged, expired handle logs 7503, cancellation. An order test
through IEncinaPipelineInspector proving the lock behavior is outer to TransactionPipelineBehavior and InboxPipelineBehavior.
A DI test with ValidateOnBuild and ValidateScopes for the flags on and off. Guard tests for the public constructor.

KEY RULES:
- Never log the message id or EncinaError.Message; the tenant tag only when a tenant exists.
- Fail closed: a Left never runs the handler.

REFERENCE FILES: src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs, src/Encina/Pipeline/Ordering/PipelineStageAttribute.cs,
src/Encina/Pipeline/Ordering/IEncinaPipelineInspector.cs, src/Encina.Messaging/MessagingServiceCollectionExtensions.cs,
docs/architecture/adr/048-inbox-record-vs-business-transaction.md.
```

</details>

### Phase 3: Cross-Cutting Integration and Observability

> **Goal**: Record what is integrated and what is not.

<details>
<summary><strong>Tasks</strong></summary>

1. Telemetry reuses the counter of #713 with `pattern="inbox"` and the optional `tenant.id` tag; the behavior starts no new activity (the inbox and transaction spans exist).
2. Logs: 7500, 7501 and 7503 from the shared gate; 7504 added here. 7505 is not used by this plan (the next family id is 7505, taken by #715).
3. Health, caching, resilience, audit: evaluated in the matrix; none adds code here.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are verifying Phase 3 of issue #714: observability reuse.

CONTEXT:
- The counter and gate come from #713; the tenant tag rule is ADR-054 D4 (a tag only when a tenant exists).

TASK:
Test that the counter carries pattern "inbox" and tenant.id only when the request context has a tenant, and that no tag
or log contains the message id.

KEY RULES:
- Bounded tags only; document the tenant cardinality note by linking the observability guide (#906).

REFERENCE FILES: src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs, docs/architecture/adr/054-observability-conventions.md.
```

</details>

### Phase 4: Testing — PRs 2, 3 and 4

> **Goal**: Every flag reaches its target and every provider proves one handler run under concurrent delivery.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit, guard, contract**: in PRs 1 and 2 (files named above); the order test runs the real registrations.
2. **Property** (PR 1): name determinism and absence of the raw id.
3. **Integration** (PRs 3 and 4): an abstract `InboxLockConcurrencyIntegrationTestsBase` under `tests/Encina.IntegrationTests/Messaging/` sends the same message id twice at once through the full pipeline (transaction behavior, inbox behavior and the lock behavior) over one store and a Redis lock; it asserts the handler ran once and both deliveries returned the same response; one concrete class per provider in its `[Collection("<Family>-<Database>")]`: PR 3 has ADO and Dapper on SqlServer, PostgreSQL and MySQL (six classes), PR 4 has EF Core on the same three and MongoDB (four). All ten providers are covered.
4. **Load and benchmark**: justified in `.md` files (one acquire per idempotent request; not a hot path of its own).
5. **Per-file targets**:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `Inbox/InboxLockPipelineBehavior.cs` | 95 | 60 | 0 | 0 | 70 | Every branch is unit-testable; guard covers the public constructor; the concurrency test runs it on ten providers; contract 0: a behavior, not an interface |
| `Inbox/InboxRequestAdmission.cs` | 95 | 0 | 0 | 0 | 0 | Internal static helper; guard 0 because the type is internal without `InternalsVisibleTo` for `Encina.GuardTests` |
| `Inbox/InboxPipelineBehavior.cs` | 95 | 40 | 0 | 0 | 0 | Existing tests kept; the extraction adds no behavior |
| `Inbox/InboxOptions.cs` | 95 | 40 | 0 | 0 | 0 | Setters and `Validate` are directly drivable |
| `Locking/MessagingLockNames.cs` | 95 | 0 | 0 | 80 | 0 | Hash and format; the property test covers determinism |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #714: tests across the ten providers.

CONTEXT:
- Integration tests use shared [Collection("<Family>-<Database>")] fixtures, ClearAllDataAsync in InitializeAsync, and
  RedisFixture for the real lock. Tests execute real code and assert with Shouldly through Encina.Testing.Shouldly.

TASK:
Write the abstract concurrent-delivery base and the ten concrete classes, the unit, guard and property files, and the
.md justifications for load and benchmark. Report measured per-flag coverage against the Phase 4 targets.

KEY RULES:
- Release the two deliveries together with a barrier (a TaskCompletionSource), never Thread.Sleep.
- Never call a fixture's DisposeAsync from a test.

REFERENCE FILES: tests/Encina.IntegrationTests/Messaging, tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorTests.cs,
docs/testing/integration-tests.md.
```

</details>

### Phase 5: Documentation and Finalization — PR 4

> **Goal**: Users know why the lock sits outside the transaction and what it does not guarantee.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on the behavior, `LockExpiry`, `LockWait` and `InboxErrorCodes.LockContended`, with the ADR-048 explanation in the remarks.
2. `changelog.d/714-inbox-distributed-lock.added.md`, through `mechanical-fixer`.
3. The inbox section of the messaging locks page owned by #713 and `src/Encina.Messaging/README.md`, through `docs-writer`: when to enable the lock, the relation to #2401, the limits.
4. Build and test verification: zero warnings, per-flag targets met.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are finishing issue #714: documentation and verification.

CONTEXT:
- Documentation goes through docs-writer; the fragment and PublicAPI lines through mechanical-fixer.

TASK:
Complete the XML docs, request the fragment and the documentation, run the build and tests.

KEY RULES:
- State the limits plainly: a lease without fencing, a handler longer than LockExpiry loses the lock, the second delivery
  waits at most LockWait.
- Write "the reference application", never a private client name.

REFERENCE FILES: changelog.d/README.md, src/Encina.Messaging/README.md, docs/architecture/adr/048-inbox-record-vs-business-transaction.md.
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| ADR-048 | The inbox is inside the business transaction; the lock must enclose the commit |
| ADR-050 | Inbox row: row claiming or a lock when consumers run concurrently (#2401) |
| ADR-052 | Contention `Right(None)`, fault `Left`, cancellation propagates |
| ADR-054 D4 | Tenant tag only when a tenant exists |
| ADR-062 | `inbox.lock_contended` is `Conflict` |
| SQL Server `sp_getapplock` | Resource name limit of 255 characters, the reason for hashing |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `InboxPipelineBehavior`, `InboxOrchestrator` | `src/Encina.Messaging/Inbox/` | Admission predicate; the behavior the lock encloses |
| `PipelineStage`, `PipelineStageAttribute`, `IEncinaPipelineInspector` | `src/Encina/Pipeline/Ordering/` | Placement outside the transaction and the order test |
| `MessagingLockGate`, `MessagingLockNames`, `MessagingLockMarker` | #713 plan, `src/Encina.Messaging/Locking/` | The shared contract |
| `ErrorCategory` | `src/Encina/Errors/ErrorCategory.cs` | `Conflict` |
| `RedisFixture` | `tests/Encina.TestInfrastructure/RedisFixture.cs` | Real lock in integration tests |

### Provider Matrix (database stores as consumers)

| Family | Providers | Notes |
|--------|-----------|-------|
| ADO.NET | SqlServer, PostgreSQL, MySQL | `InboxStoreADO`; one integration class each |
| Dapper | SqlServer, PostgreSQL, MySQL | `InboxStoreDapper`; one integration class each |
| EF Core | SqlServer, PostgreSQL, MySQL | One `InboxStoreEF`; three integration classes |
| MongoDB | MongoDB | `InboxStoreMongoDB` (enlisted processed mark, ADR-048 amendment) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingLocking`, registered by the #713 plan) | 7504 | `InboxLockContended`; 7500-7503 are #713's and reused; #715 starts at 7505 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 2 | `InboxLockPipelineBehavior`, `InboxRequestAdmission` |
| Production (modified) | 8 | `MessagingLockNames`, `InboxOptions`, `InboxOrchestrator` (codes), `InboxPipelineBehavior`, `MessagingLockGate`, `MessagingLockMetrics`, `MessagingLockLog`, `MessagingServiceCollectionExtensions` |
| `PublicAPI.Unshipped.txt` and manifest | 2 | Messaging |
| Tests | 21 | Unit (6), guard (1), property (1), order and DI (2), integration base plus ten classes (11) |
| Documentation | 4 | Fragment, page section, README, two `.md` justifications |

PR split (each at most 15 files and 1500 lines, each building and passing alone):

| PR | Content | Files | Lines (est.) |
|----|---------|------:|-------------:|
| 1 | Phase 1: names, options, error code, admission helper, `InboxPipelineBehavior` extraction, tests, `PublicAPI`, manifest; starts after PRs 1 and 2 of the #713 plan | 12 | 700 |
| 2 | Phases 2 and 3: behavior, gate and metrics tenant tag, log 7504, registration, tests (unit, order, DI, guard), `PublicAPI`; needs the adapter (PR 3 of #713) only for integration | 13 | 1,100 |
| 3 | Phase 4 integration: base plus six ADO and Dapper classes | 7 | 700 |
| 4 | Phase 4 integration: EF Core x3 and MongoDB; Phase 5: fragment, README, page section, justifications | 9 | 650 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases of issue #714</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library with Railway Oriented Programming (Either<EncinaError, T>). Issue #714 adds an
opt-in per-message lock for inbox processing. Because the inbox runs inside the business transaction (ADR-048), the lock
is a pipeline behavior at PipelineStage.Transaction, Order -200, so it is released after the commit. The shared contract
comes from the plan of #713. Read AGENTS.md sections 3, 5, 6 and 9 and ADR-048, ADR-050, ADR-052, ADR-054, ADR-062 first.

IMPLEMENTATION OVERVIEW:
1. MessagingLockNames.Inbox (SHA-256 hex), InboxOptions.LockExpiry/LockWait, InboxErrorCodes.LockContended (Conflict),
   InboxRequestAdmission shared with InboxPipelineBehavior.
2. InboxLockPipelineBehavior: admission, gate with the tenant id, wait then Left on contention, dispose after nextStep.
3. EventId 7504; registration when UseInbox and UseDistributedLocking; order test via IEncinaPipelineInspector.
4. Unit, guard, property, DI and order tests; concurrent-delivery integration over ten providers; docs by specialists.

KEY PATTERNS:
- Fail closed; log codes and request types only; never the message id or EncinaError.Message.
- The lock is a lease without fencing; never describe it as atomic or transactional.
- TimeProvider for clock reads; Shouldly via Encina.Testing.Shouldly; no Thread.Sleep.

REFERENCE FILES: src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs, src/Encina.Messaging/Inbox/InboxOrchestrator.cs,
src/Encina/Pipeline/Ordering/, docs/architecture/adr/048-inbox-record-vs-business-transaction.md,
docs/plans/outbox-distributed-lock-implementation-plan-713.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The cached response is the inbox's own; the lock adds no cache |
| 2 | OpenTelemetry | ✅ | Phase 3: counter of #713 with `pattern="inbox"` and `tenant.id` only when a tenant exists (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phase 2: 7504 plus the shared 7500-7503; no message id or error message in any field |
| 4 | Health Checks | ❌ | No new checkable dependency; the lock provider has its own health check |
| 5 | Validation | ✅ | Phase 1: `InboxOptions.Validate` for the lock timings; the startup check is #713's |
| 6 | Resilience | ❌ | A contended message is redelivered by the broker; the pipeline's recoverability stage is outer and may retry the `Conflict` |
| 7 | Distributed Locks | ✅ | The feature itself |
| 8 | Transactions | ✅ | Phase 2: the behavior is placed outside the transaction so the lock is held until the commit (ADR-048); it does not make anything atomic |
| 9 | Idempotency | ✅ | The feature closes the concurrent-delivery window of the inbox; #2401 is the database-level alternative |
| 10 | Multi-Tenancy | ✅ | Tenant tag on telemetry when a tenant exists; the lock name carries no tenant because the store key has none; #741 can scope it |
| 11 | Module Isolation | ❌ | Not applicable: the inbox is not module scoped |
| 12 | Audit Trail | ⏭️ | Audit of inbox processing is #749 (open); the lock adds no audit event |

---

## Prerequisites & Dependencies

- The #713 plan's PRs 1 and 2 (names class, gate, marker, flag, range) before PR 1 here; PR 3 of #713 (adapter, startup check) and #2667 before integration tests run.
- #2184 is not a prerequisite: a behavior that declares `Transaction`/-200 is outer to the transaction behavior whether or not #2184 gives that behavior a stage.
- #2401 (atomic claim) is an alternative, not a blocker; its plan does not exist yet. Dilemma 1 asks which of A and C the maintainer wants.
- Not blocked by another issue's open maintainer decision.

## Next Steps

1. Open the plan PR; when work on #714 starts, ask the maintainer to answer Dilemmas 1 to 3 (with #713's Dilemma 4 on lock lifetime) and record them in `## Maintainer Decisions`.
2. Brief PR 1 after PR 2 of #713 merged; PRs 3 and 4 after the adapter and #2667.
