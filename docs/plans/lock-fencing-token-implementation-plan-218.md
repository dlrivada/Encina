# Implementation Plan: Fencing Tokens on `ILockHandle` (`long FencingToken`, strictly increasing per resource)

> **Issue**: [#218](https://github.com/dlrivada/Encina/issues/218)
> **Type**: Feature
> **Complexity**: Medium-high (an interface change plus a counter in seven providers; 8 small pull requests, implementers first)
> **Estimated Scope**: ~1,100 lines of production code + ~2,800 lines of tests (about 3,900 lines over 8 pull requests; the per-PR estimates in "Estimated File Count" sum to it)
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-009, AC-027; [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-062](../architecture/adr/062-error-category-contract.md); Martin Kleppmann, "How to do distributed locking" (the fencing token argument)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision; it rewrites the same provider files, so this plan starts after it); the four planned provider packages (#207 to #210) for PRs 5 to 8
> **Related**: family `lock-primitives` ([#215](https://github.com/dlrivada/Encina/issues/215), [#216](https://github.com/dlrivada/Encina/issues/216), [#217](https://github.com/dlrivada/Encina/issues/217), [#219](https://github.com/dlrivada/Encina/issues/219)); [#717](https://github.com/dlrivada/Encina/issues/717) plan (lists fencing as the missing protection for a paused ex-leader); #226 (RedLock quorum), #724 (telemetry wrapper of `ILockHandle`), #2405

---

## Summary

A lock with an expiry is not enough: a holder that pauses (a GC stall, a network partition) can resume after its lock expired and another holder took it, and its write then conflicts with the new holder's. The standard remedy is a **fencing token**: every grant of a lock returns a number that is larger than the number of every earlier grant, the holder passes it to the resource it writes to, and that resource rejects any token lower than the highest it has seen. This plan adds `long FencingToken { get; }` to `ILockHandle` and implements it in every lock provider of SPEC-000 DEC-009 (InMemory, Redis, SQL Server, PostgreSQL, MySQL, Azure Blob, DynamoDB).

1. **Contract.** `FencingToken` is greater than zero and strictly greater than the token of every earlier grant **of the same resource** by the same backend (not gap-free, and no relation across resources is promised). `ExtendAsync` never changes it. A token is never reused.
2. **Issued while the lock is held, atomically with the grant.** The token must be generated inside the grant (one Lua script on Redis; the session lock still held on SQL Server, PostgreSQL and MySQL; the lease or the conditional write itself on Azure Blob and DynamoDB), never in a second step after it. A second step lets a stalled holder whose lock already expired draw a token larger than the new holder's, which defeats the feature (Design Choice 1).
3. **A failed token is a failed acquire.** When the counter cannot be advanced the provider releases the lock and returns `Left` with `lock.fencing_failed` (ADR-062 category `Internal`, R4); it never returns a handle with a missing token.
4. **Implementers first.** The property lands on each provider's handle in its own pull request (PRs 1 to 3 and 5 to 8); the member is added to the interface in PR 4, after the three existing implementers have it, so the build is green between pull requests.
5. **Resource-side check.** Rejecting a stale token is the job of the resource that receives it; the README shows the pattern. This plan ships no helper for it.

**Standards**: Kleppmann (fencing tokens); ADR-052; ADR-062; SPEC-000 REQ-027; Redis `INCR`, SQL Server and PostgreSQL sequences, DynamoDB atomic counters (named backend primitives).

**Estimated scope**: 3 new DDL scripts, 1 new contract test suite, about 10 modified production files, about 14 test files, README and changelog fragment; 8 pull requests.

**Affected packages**: `Encina.DistributedLock` (interface, README), `.InMemory`, `.Redis`, `.SqlServer`; later `.PostgreSQL`, `.MySQL`, `.AzureBlob`, `.DynamoDB`; test projects `Encina.UnitTests`, `Encina.ContractTests`, `Encina.IntegrationTests`. No EventId and no new diagnostics type: a failed token is a failed acquire and is already counted and logged by the #724 and #222 decorator as a `failed` outcome (Design Choice 5).

**Provider category**: Distributed locks (AGENTS.md section 5): all seven providers; the cloud ones are tested with Azurite and LocalStack. `Encina.Caching.Memory` and `Encina.Caching.Redis` also implement `IDistributedLockProvider`, but their private `LockHandle` implements `IAsyncDisposable` only, not `ILockHandle` (`MemoryDistributedLockProvider.cs:222`, `RedisDistributedLockProvider.cs:208` in `Encina.Caching.Redis`); they are not in the seven of SPEC-000 DEC-009 and this plan covers every class that implements `ILockHandle` when PR 4 starts.

**Shared pieces and ownership (family `lock-primitives`)**: the schema-provisioning option of the SQL providers (`EnsureSemaphoreSchema` in #215 Design Choice 3, default `false`) has a sibling here, `EnsureFencingSchema`, with the same default and the same answer; the multi-lock handle of #219 exposes each inner handle's token through `Handles`.

**What the code does today** (`origin/main` at `326e65ebf`):

- `ILockHandle` has `Resource`, `LockId`, `AcquiredAtUtc`, `ExpiresAtUtc`, `IsReleased` and `ExtendAsync`; nothing numeric orders grants. Three classes implement it, each a private `LockHandle`: `InMemoryDistributedLockProvider.cs:250`, `RedisDistributedLockProvider.cs:223` (Encina.DistributedLock.Redis) and `SqlServerDistributedLockProvider.cs:242`. No test class implements `ILockHandle` and no test substitutes it (`git grep` over `tests/`: only casts and reads of the three provider handles).
- The Redis provider acquires with `StringSetAsync` (`SET NX PX`, `RedisDistributedLockProvider.cs:127`) and releases and extends with Lua scripts that check ownership; `RedisLockOptions.Database` selects a numbered database, so the package targets a single Redis or Sentinel deployment (a Redis Cluster has only database 0 and the scripts address more than one key); the Lua change below is correct only there and the README states it.
- The SQL Server provider holds one connection per handle with `sp_getapplock` (`Exclusive`, `Session` owner) and does not enforce expiry today (#2405); the planned PostgreSQL (#207) and MySQL (#208) providers also use session locks and enforce expiry with a `TimeProvider` timer that closes the session.
- Extension is a reset to now plus the extension (`PEXPIRE` in `ExtendLockScript`; `now.Add(extension)` in InMemory).
- Manifests: `.github/coverage-manifest/Encina.DistributedLock.json` (guard 20, unit 60) and the three provider manifests.

---

## Design Choices

<details>
<summary><strong>1. Where the counter lives and how it is advanced atomically with the grant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One counter per backend where the lock is held while it advances (Redis: key `{KeyPrefix}:fence` incremented inside the acquire Lua script; SQL Server, PostgreSQL: a sequence read on the connection that holds the session lock; MySQL: a counter row updated on that session), and a counter inside the lock record where nothing else is atomic (Azure Blob: blob metadata written under the lease; DynamoDB: a `FencingToken` attribute increased by the same conditional `UpdateItem` that takes the lock; the record is kept, not deleted, on release)** | No per-resource key growth on the four primary backends; each grant is atomic with its token; tokens are monotonic across resources too (stronger than required) | Two placements to explain; a hot single counter (acceptable: one `INCR` or `NEXT VALUE` per lock grant) |
| **B) One counter per resource on every provider** (Redis `{KeyPrefix}:fence:{resource}` as in the issue's snippet; SQL tables keyed by resource) | One placement; the contract is exactly per resource | One never-expiring key or row per distinct resource name (names like `saga:payment:{orderId}` are high-cardinality), or a TTL that would reset the counter and make the new tokens lower than the highest one the resource has already seen |

### Chosen Option: **A - one counter per backend, in-record counter on Azure Blob and DynamoDB** (recommended, pending the maintainer)

### Rationale

- A wins for high-cardinality resource names; B wins when per-resource counters are wanted for operations (reading the last token of one resource) and the key growth is acceptable. The atomicity requirement is not a choice: with a second step after the grant a holder whose lock expired between its grant and its token draw would receive a larger token than the next holder. On Azure Blob and DynamoDB a global counter cannot be advanced atomically with the grant (DynamoDB `TransactWriteItems` returns no values; an Azure counter blob is writable without the lease), so those two use the in-record counter under both options.
- SQL Server, PostgreSQL and MySQL need the sequence or counter row created: the opt-in `EnsureFencingSchema` (default `false`, a shipped DDL script) follows the answer to #215 Dilemma 3.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Redis counter durability</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A plain `INCR` on `{KeyPrefix}:fence`; the README states that tokens are only as durable as the Redis persistence (AOF with `appendfsync always` or `everysec` accepts a window; no persistence means a restart or a flush restarts the counter)** | Simple; the contract is the Kleppmann one | After data loss new tokens can be lower than tokens the resource already saw; the resource then rejects the new holder (fail closed, never a stale write) |
| **B) The token is `max(counter + 1, Redis TIME in microseconds)` computed in the same script** | A flushed counter restarts above every earlier token as long as the Redis clock never runs backwards | Tokens are time-flavoured numbers (large, with gaps); an unusual clock jump breaks the order; harder to explain |

### Chosen Option: **A - plain `INCR` with a documented persistence requirement** (recommended, pending the maintainer)

### Rationale

- A wins when Redis is run with persistence as the lock deployment guide advises; B wins for deployments that may lose Redis data and still want fencing to keep working. Under both options a lost counter can only make the resource reject a token, never accept a stale one.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Redis quorum mode (#226)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In quorum mode the token comes from the counter of the first configured endpoint only; the XML docs and README state that the quorum protects the lock, not the token** | The lock keeps working with N endpoints; one place for the counter | If that endpoint fails over, the counter may regress (Design Choice 2); the quorum does not make tokens quorum-safe (the known limit of Redlock with fencing) |
| **B) In quorum mode `AcquireAsync` returns `Left` (`lock.fencing_unsupported`, `Internal`)** | Fail closed: nobody gets a token the system cannot stand behind | Quorum users lose locking entirely until they disable the quorum or accept A |

### Chosen Option: **A - first endpoint, documented** (recommended, pending the maintainer)

### Rationale

- A wins when quorum users mostly need the lock and treat the token as best effort; B wins when fencing is a safety requirement of the deployment. The same single-endpoint rule is already used for the semaphore in the #215 plan.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Order of the pull requests when an interface gains a member</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implementers first: each provider's handle gets the property in its own PR (PRs 1 to 3); PR 4 adds the member to `ILockHandle` and the contract suite** | Every PR builds and passes alone; small, reviewable PRs | The property exists on three classes before the interface requires it |
| **B) One PR that changes the interface and all three implementers** | One step | About 14 files with three providers' scripts and tests; any wrapper that implements `ILockHandle` (the #724 `InstrumentedLockHandle`) breaks in the same PR |

### Chosen Option: **A - implementers first**

### Rationale

- Each PR must pass CI alone, and the member is only valid once every implementer has it. If #724 has merged by PR 4, its `InstrumentedLockHandle` forwards the property in PR 4 (a compile error otherwise); if not, #724's first PR includes the forwarding.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 9 (CI enforces that all tests pass and zero warnings on every pull request) and the three implementers listed under "What the code does today"

</details>

<details>
<summary><strong>5. Telemetry, logging and the value zero</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No new instrument, log or EventId: a failed token is a `failed` acquire counted by the #724 decorator; the token value is added as the span attribute `encina.lock.fencing_token` by the #724 handle wrapper (a counter value, not data) and never as a metric tag (unbounded)** | Nothing to register; no cardinality risk | The issue's "fencing token events logged" is satisfied by the existing acquire logs |
| **B) A log per issued token and a gauge of the highest token** | Visible token flow | A log per lock grant and an unbounded value on a metric |

### Chosen Option: **A**

### Rationale

- ADR-054 forbids unbounded tag values and data-bearing names; the token carries no data but has unbounded cardinality. The value `0` means "no token" and no provider ever returns it (the contract suite asserts `> 0`).

**Class:** Trivial
**Settled by:** [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3 and D6.3 (bounded tags, no data-bearing names)

</details>

---

## Implementation Phases

### Phase 1: Providers That Exist (PRs 1 to 3)

> **Goal**: The three existing providers issue tokens on their handles.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 1 - InMemory** - `InMemoryDistributedLockProvider.cs` (modify): a `long _fencingCounter` field advanced with `Interlocked.Increment` inside the grant (under the same entry update that takes the lock); the private `LockHandle` gets `public long FencingToken { get; }`. The counter is per provider instance, not static (an InMemory provider is one lock universe).
2. **PR 2 - Redis** - `RedisDistributedLockProvider.cs` (modify): acquisition becomes one Lua script `if redis.call('SET', KEYS[1], ARGV[1], 'NX', 'PX', ARGV[2]) then return redis.call('INCR', KEYS[2]) else return 0 end` (keys: the lock key and `{KeyPrefix}:fence`; a `0` result is contention); the handle gets `FencingToken`; a script failure is `Left` `lock.fencing_failed` (the lock is not held: the script is atomic). README states the single-node limit and the persistence requirement (Design Choice 2).
3. **PR 3 - SQL Server** - `SqlServerDistributedLockProvider.cs` (modify): after `sp_getapplock` succeeds, `SELECT NEXT VALUE FOR [dbo].[EncinaLockFencingToken]` on the same connection; on failure release the lock and return `Left` `lock.fencing_failed`; `SqlServerLockOptions` gets `EnsureFencingSchema` (default `false`) and the sequence name; `Scripts/LockFencing.sql` (new); a missing sequence is a `Left` that names no object.
4. Each PR: unit tests (token greater than zero, strictly increasing across successive holders, a second holder after expiry gets a larger token, `ExtendAsync` keeps the token, a token failure leaves no lock held), integration tests for Redis and SQL Server on the shared fixtures (50 concurrent acquirers: the tokens are unique and ordered by the order of holding), PublicAPI is not touched in PRs 1 to 3 (the handles are private classes), manifest rows.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing one pull request of Phase 1 of issue #218: fencing tokens on one existing lock provider.

CONTEXT:
- ILockHandle is NOT changed in this PR; only the provider's private LockHandle class gets a public long FencingToken.
- The token must be generated inside the grant (atomic with it), never in a second step: a stalled holder whose lock expired
  must not be able to draw a token larger than the next holder's.
- After #2667 the provider returns Either; a token failure releases the lock and returns Left (ADR-062 Internal).

TASK:
Implement the PR named in your brief with the mechanism of the Phase 1 tasks and the tests listed there.

KEY RULES:
- Tokens are > 0, strictly increasing per resource, never reused; ExtendAsync never changes the token.
- No resource name in errors, logs or exceptions; asynchronous database calls with a CancellationToken.
- Time through TimeProvider; the InMemory counter is an instance field, not static.

REFERENCE FILES:
- src/Encina.DistributedLock.InMemory/InMemoryDistributedLockProvider.cs
- src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs
```

</details>

### Phase 2: The Interface Member and the Contract Suite (PR 4)

> **Goal**: `ILockHandle.FencingToken` exists and every implementer is proven by one suite.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ILockHandle.cs`** (modify) - `long FencingToken { get; }` with XML docs stating the contract of the Summary (greater than zero, strictly increasing per resource, not gap-free, unchanged by `ExtendAsync`, the holder passes it to the resource and the resource rejects lower tokens).
2. **`PublicAPI.Unshipped.txt`** (core, via `mechanical-fixer`): `Encina.DistributedLock.ILockHandle.FencingToken.get -> long`.
3. **`tests/Encina.ContractTests/DistributedLock/FencingTokenContractTests.cs`** (new) - an abstract base: successive holders get increasing tokens; concurrent contenders get unique tokens in holding order; a holder after expiry gets a larger token than the expired one; the token is positive and stable across `ExtendAsync`; subclasses for InMemory, Redis and SQL Server (the later providers add one each).
4. **#724 wrapper** - when `InstrumentedLockHandle` exists on main, forward the property and add the span attribute of Design Choice 5 in this PR.
5. **README** section "Fencing tokens" with the resource-side check example and the per-provider durability table (via `docs-writer`); changelog fragment `changelog.d/218-lock-fencing-token.added.md` (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #218: the FencingToken member of ILockHandle and its contract suite.

CONTEXT:
- PRs 1 to 3 already gave the three provider handles the property; adding the member now cannot break an implementer.
- Search for every ILockHandle implementer and substitute at the start (git grep) and handle each; do not assume three.

TASK:
Add the member, the PublicAPI line and the abstract contract suite with its three subclasses; forward the property in the
#724 wrapper if it exists.

KEY RULES:
- Contract tests instantiate real implementations on the shared fixtures (Redis, SQL Server).
- The XML docs state the contract precisely, including "not gap-free".

REFERENCE FILES:
- src/Encina.DistributedLock/ILockHandle.cs
- docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md
```

</details>

### Phase 3: The Planned Providers (PRs 5 to 8)

> **Goal**: Tokens on the other four providers, one pull request each, after their package merged.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 5 - PostgreSQL** (after #207): `SELECT nextval('encina_lock_fencing_token')` on the connection that holds the advisory lock; DDL script; `EnsureFencingSchema`.
2. **PR 6 - MySQL** (after #208): `UPDATE encina_lock_fencing SET value = LAST_INSERT_ID(value + 1)` then `SELECT LAST_INSERT_ID()` on the session that holds `GET_LOCK`; DDL script; `EnsureFencingSchema`.
3. **PR 7 - Azure Blob** (after #209): the lock blob keeps the counter in its metadata (`fencingtoken`); after the lease is acquired the provider reads it, writes `value + 1` with the lease id (a stale holder without the lease cannot write), and the lock blob is not deleted on release; Azurite tests.
4. **PR 8 - DynamoDB** (after #210): the acquire `UpdateItem` of #210 additionally sets `FencingToken = if_not_exists(FencingToken, :zero) + :one` and returns the new value (`ReturnValues = UPDATED_NEW`); release clears the expiry instead of deleting the item; LocalStack tests.
5. Each PR adds its contract subclass and its manifest lines; no interface change.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing one pull request of Phase 3 of issue #218: fencing tokens for a lock package that exists.

CONTEXT:
- FencingTokenContractTests (Phase 2) is an abstract base; add one subclass for the provider.
- The token must be advanced atomically with the grant (see the plan's Design Choice 1).

TASK:
Implement the provider's token with the mechanism of the Phase 3 tasks and make the contract suite pass on its fixture.

KEY RULES:
- A token failure releases the lock and returns Left (lock.fencing_failed); never a handle without a token.
- No resource name in errors, logs or tags.

REFERENCE FILES:
- docs/plans/lock-postgresql-implementation-plan-207.md (or -208, -209, -210)
- tests/Encina.ContractTests/DistributedLock/FencingTokenContractTests.cs
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| Kleppmann, "How to do distributed locking" | The fencing token argument and the resource-side check |
| SPEC-000 REQ-027, DEC-009, AC-027 | Seven providers, contract tests over all |
| ADR-052, ADR-062 | Acquire outcomes; category of `lock.fencing_failed` (`Internal`, R4) |
| ADR-054 D3, D6.3 | No unbounded tag values |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `LockHandle` classes (3) | the three provider packages | Where the property is added |
| `ExtendLockScript` / `ReleaseLockScript` | `src/Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs` | Ownership-checked Lua pattern for the acquire script |
| `SqlServerLockOptions` | `src/Encina.DistributedLock.SqlServer/` | Home of `EnsureFencingSchema` |
| #215 Design Choice 3 | `docs/plans/lock-semaphore-implementation-plan-215.md` (plan #215) | Schema provisioning answer |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Token source | Status of the package | Lands in |
|----------|--------------|-----------------------|----------|
| InMemory | `Interlocked.Increment`, instance field | exists | PR 1 |
| Redis | `INCR {KeyPrefix}:fence` inside the acquire script | exists | PR 2 |
| SQL Server | sequence read on the session that holds `sp_getapplock` | exists | PR 3 |
| PostgreSQL | sequence read on the advisory-lock session | planned (#207) | PR 5 |
| MySQL | `LAST_INSERT_ID` counter row on the `GET_LOCK` session | planned (#208) | PR 6 |
| Azure Blob | lock-blob metadata written under the lease | planned (#209) | PR 7 |
| DynamoDB | `FencingToken` attribute in the lock item | planned (#210) | PR 8 |

### Event ID Allocation

| Package | Range | Ids |
|---------|-------|-----|
| none | none | No EventId is used or registered by this plan (Design Choice 5); the family range `DistributedLockPrimitives` (30550-30564) belongs to #215, #216, #217 and #219 |

### Estimated File Count

| PR | Content | New | Modified | Tests, manifests, PublicAPI, docs | Total | Lines (est.) |
|----|---------|-----|----------|------------------------------------|-------|--------------|
| 1 | InMemory | 0 | 1 | 2 (unit, manifest) | 3 | 350 |
| 2 | Redis | 0 | 1 | 4 (unit, integration, manifest, README note) | 5 | 500 |
| 3 | SQL Server | 1 (DDL script) | 2 (provider, options) | 4 (unit, integration, manifest, README note) | 7 | 700 |
| 4 | Interface, contract suite | 1 (contract suite) | 1 (`ILockHandle.cs`) | 5 (PublicAPI, README section, fragment, manifest, #724 wrapper forwarding when present) | 7 | 600 |
| 5 | PostgreSQL | 1 (DDL) | 1 | 3 | 5 | 400 |
| 6 | MySQL | 1 (DDL) | 1 | 3 | 5 | 450 |
| 7 | Azure Blob | 0 | 1 | 3 | 4 | 450 |
| 8 | DynamoDB | 0 | 1 | 3 | 4 | 450 |

Total about 3,900 lines. Every PR is at most 15 files and 1,500 lines and builds and passes alone (the interface changes in PR 4, after every implementer has the property).

### Per-file coverage targets (numeric, per flag)

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `InMemoryDistributedLockProvider.cs` (token paths) | 95 | 13 | 80 | 0 | Existing file; the added lines are in-process and fully reachable with `FakeTimeProvider`; guard target stays at the manifest's current value because no parameter is added |
| `RedisDistributedLockProvider.cs` (token paths) | 75 | 13 | 70 | 75 | Script arguments and result mapping unit-testable with a mocked `IDatabase`; atomicity only on a real Redis |
| `SqlServerDistributedLockProvider.cs` (token paths) | 65 | 13 | 70 | 75 | Failure-to-`Left` mapping unit-testable; sequence behavior needs a real server |
| `ILockHandle.cs` | 0 | 0 | 0 | 0 | An interface member: no executable code; covered through the providers |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is pre-1.0 (.NET 10, C# 14, nullable enabled). Rules are in AGENTS.md. Issue #218 adds long FencingToken to
ILockHandle and implements it in every lock provider of SPEC-000 DEC-009.

IMPLEMENTATION OVERVIEW:
1. Phase 1 (PRs 1-3): InMemory, Redis, SQL Server handles get the property and the atomic token source; the interface is untouched.
2. Phase 2 (PR 4): add the member to ILockHandle, the PublicAPI line, the abstract contract suite with three subclasses,
   forward in the #724 wrapper if present, README and fragment through the specialists.
3. Phase 3 (PRs 5-8): PostgreSQL, MySQL, Azure Blob, DynamoDB after their packages (#207-#210).

KEY PATTERNS:
- The token is generated inside the grant, atomically; tokens are > 0 and strictly increasing per resource.
- A token failure releases the lock and returns Left (lock.fencing_failed, ADR-062 Internal).
- Implementers first; grep every ILockHandle implementer and substitute before changing the interface.
- Dilemmas 1, 2 and 3 are pending the maintainer: implement the recommended options only after he answers.

REFERENCE FILES:
- docs/plans/lock-fencing-token-implementation-plan-218.md, src/Encina.DistributedLock/ILockHandle.cs
- docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Locks are ephemeral and a cached token would be stale by definition |
| 2 | OpenTelemetry | ✅ Include | The token is a span attribute of the acquire span through the #724 wrapper (Phase 2); never a metric tag |
| 3 | Structured Logging | ❌ N/A | The existing acquire logs of the #724 and #222 decorator cover a failed token; a log per grant would be noise (Design Choice 5); no EventId needed |
| 4 | Health Checks | ❌ N/A | The existing lock health checks probe the same backend and connection |
| 5 | Validation | ✅ Include | The contract suite asserts positivity and strict monotonicity (the issue's "token monotonicity validation"); no user input |
| 6 | Resilience | ❌ N/A | A failed token fails the acquire visibly; retrying inside the provider would hold or re-take the lock behind the caller's back |
| 7 | Distributed Locks | ✅ Include | This is a property of the lock handle on all seven providers |
| 8 | Transactions | ❌ N/A | Fencing is orthogonal to database transactions |
| 9 | Idempotency | ✅ Include | The token lets the resource reject stale writes; Encina ships the token and the README pattern, not the resource-side check |
| 10 | Multi-Tenancy | ⏭️ Defer | A backend-wide counter is shared by all tenants and stays monotonic per resource, so tenant scoping of the counter is not needed; tenant scoping of lock keys is #741, see `artifacts/issues/lock-primitives-tenant-scoping.md` for the primitives |
| 11 | Module Isolation | ❌ N/A | Cross-module lock primitive |
| 12 | Audit Trail | ❌ N/A | Infrastructure property with no personal or regulated data; the span attribute is the trace |

---

## Prerequisites & Dependencies

| Item | State | Needed for |
|------|-------|------------|
| #2667 ADR-052 implementation | open | PRs 1 to 4 (same provider files; `Either` outcomes) |
| #724 `InstrumentedLockHandle` | plan on main, code not written | PR 4 (forwarding), only if merged first |
| #215 Dilemma 3 (schema provisioning) | pending the maintainer | PRs 3, 5, 6 (`EnsureFencingSchema`) |
| #207, #208, #209, #210 | plans on main, packages not written | PRs 5 to 8 |

---

## Next Steps

1. After #2667 merges open PRs 1, 2 and 3 in parallel, then PR 4; PRs 5 to 8 as the provider packages merge.
2. The maintainer answers Dilemmas 1, 2 and 3 when work on the issue starts; PR 4 and PR 1 do not depend on them.
3. Open the tenant-scoping follow-up issue once for the whole family.
