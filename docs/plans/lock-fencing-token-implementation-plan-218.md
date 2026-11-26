# Implementation Plan: Fencing Tokens on `ILockHandle` (`long FencingToken`, strictly increasing per resource)

> **Issue**: [#218](https://github.com/dlrivada/Encina/issues/218)
> **Type**: Feature
> **Complexity**: Medium-high (an interface change plus a counter in seven providers; 8 small pull requests, implementers first)
> **Estimated Scope**: ~1,300 lines of production code + ~3,200 lines of tests (about 4,500 lines over 8 pull requests; the per-PR estimates in "Estimated File Count" sum to it)
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-009, AC-027; [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-062](../architecture/adr/062-error-category-contract.md); Martin Kleppmann, "How to do distributed locking" (the fencing token argument)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision; it rewrites the same provider files, so this plan starts after it); the four planned provider packages (#207 to #210) for PRs 5 to 8; [#2405](https://github.com/dlrivada/Encina/issues/2405) (open bug: the SQL Server lock does not enforce its expiry) for every "holder after expiry gets a larger token" scenario on SQL Server (PR 3, PR 4)
> **Related**: family `lock-primitives` ([#215](https://github.com/dlrivada/Encina/issues/215), [#216](https://github.com/dlrivada/Encina/issues/216), [#217](https://github.com/dlrivada/Encina/issues/217), [#219](https://github.com/dlrivada/Encina/issues/219)); [#717](https://github.com/dlrivada/Encina/issues/717) plan (lists fencing as the missing protection for a paused ex-leader); #226 (RedLock quorum), #724 (telemetry wrapper of `ILockHandle`), #609 (MongoDB lock plan, a further `ILockHandle` implementer)

---

## Summary

A lock with an expiry is not enough: a holder that pauses (a GC stall, a network partition) can resume after its lock expired and another holder took it, and its write then conflicts with the new holder's. The standard remedy is a **fencing token**: every grant of a lock returns a number that is larger than the number of every earlier grant, the holder passes it to the resource it writes to, and that resource rejects any token lower than the highest it has seen. This plan adds `long FencingToken { get; }` to `ILockHandle` and implements it in every lock provider of SPEC-000 DEC-009 (InMemory, Redis, SQL Server, PostgreSQL, MySQL, Azure Blob, DynamoDB).

1. **Contract.** `FencingToken` is greater than zero and strictly greater than the token of every earlier grant **of the same resource** by the same backend (not gap-free, and no relation across resources is promised). `ExtendAsync` never changes it. A token is never reused.
2. **Issued while the lock is held, atomically with the grant.** The token must be generated inside the grant (one Lua script on Redis; the session lock still held on SQL Server, PostgreSQL and MySQL; the lease on Azure Blob; one conditional transaction on DynamoDB, Design Choice 6), never in a second step after it. A second step lets a stalled holder whose lock already expired draw a token larger than the new holder's, which defeats the feature (Design Choice 1).
3. **A failed token is a failed acquire.** When the counter cannot be advanced the provider releases the lock and returns `Left` with `lock.fencing_failed` (ADR-062 category `Internal`, R4); it never returns a handle with a missing token.
4. **Implementers first.** The property lands on each provider's handle in its own pull request (PRs 1 to 3 and 5 to 8); the member is added to the interface only when every `ILockHandle` implementer on `main` has it, so the build is green between pull requests. Which PR adds the property to which handle, and the order PR 4 waits for, is Design Choice 4 (a Dilemma: the four planned packages create their own handles).
5. **Resource-side check.** Rejecting a stale token is the job of the resource that receives it; the README shows the pattern. This plan ships no helper for it.

**Standards**: Kleppmann (fencing tokens); ADR-052; ADR-062; SPEC-000 REQ-027; Redis `INCR`, SQL Server and PostgreSQL sequences, DynamoDB atomic counters (named backend primitives).

**Estimated scope**: 3 new DDL scripts, 1 new public error-code class, 1 new contract test suite, about 11 modified production files, about 16 test files, README and changelog fragment; 8 pull requests.

**Affected packages**: `Encina.DistributedLock` (interface, README), `.InMemory`, `.Redis`, `.SqlServer`; later `.PostgreSQL`, `.MySQL`, `.AzureBlob`, `.DynamoDB`; test projects `Encina.UnitTests`, `Encina.ContractTests`, `Encina.IntegrationTests`. No EventId and no new diagnostics type: a failed token is a failed acquire and is already counted and logged by the #724 and #222 decorator as a `failed` outcome (Design Choice 5).

**Provider category**: Distributed locks (AGENTS.md section 5): all seven providers; the cloud ones are tested with Azurite and LocalStack. `Encina.Caching.Memory` and `Encina.Caching.Redis` also implement `IDistributedLockProvider`, but their private `LockHandle` implements `IAsyncDisposable` only, not `ILockHandle` (`MemoryDistributedLockProvider.cs:222`, `RedisDistributedLockProvider.cs:208` in `Encina.Caching.Redis`); they are not in the seven of SPEC-000 DEC-009 and this plan covers every class that implements `ILockHandle` when PR 4 starts.

**Shared pieces and ownership (family `lock-primitives`)**: the schema-provisioning option of the SQL providers (`EnsureSemaphoreSchema` in #215 Design Choice 3, default `false`) has a sibling here, `EnsureFencingSchema`, with the same default and the same answer; the multi-lock handle of #219 exposes each inner handle's token through `Handles`.

**What the code does today** (`origin/main` at `326e65ebf`):

- `ILockHandle` has `Resource`, `LockId`, `AcquiredAtUtc`, `ExpiresAtUtc`, `IsReleased` and `ExtendAsync`; nothing numeric orders grants. Three classes implement it, each a private `LockHandle`: `InMemoryDistributedLockProvider.cs:250`, `RedisDistributedLockProvider.cs:223` (Encina.DistributedLock.Redis) and `SqlServerDistributedLockProvider.cs:242`. No test class implements `ILockHandle` and no test substitutes it (`git grep` over `tests/`: only casts and reads of the three provider handles). The plans on `main` that will add more implementers (grep of `: ILockHandle` over `docs/plans/`): a private `LockHandle` in each of #207 (PostgreSQL), #208 (MySQL), #209 (Azure Blob), #210 (DynamoDB) and #609 (MongoDB lock, outside the seven providers), and `InstrumentedLockHandle` of #724; none of those plans implements `FencingToken`. #741 and #226 add no handle class.
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
| **A) One counter per backend where the lock is held while it advances (Redis: key `{KeyPrefix}:fence` incremented inside the acquire Lua script; SQL Server, PostgreSQL: a sequence read on the connection that holds the session lock; MySQL: a counter row updated on that session), and a counter next to the lock where nothing else is atomic (Azure Blob: blob metadata written under the lease; DynamoDB: a per-resource counter item advanced in the same transaction that takes the lock, Design Choice 6)** | No per-resource key growth on the four primary backends; each grant is atomic with its token; on Redis, SQL Server, PostgreSQL and MySQL tokens are monotonic across resources too (stronger than required), on Azure Blob and DynamoDB they are per resource only | Two placements to explain; a hot single counter (acceptable: one `INCR` or `NEXT VALUE` per lock grant) |
| **B) One counter per resource on every provider** (Redis `{KeyPrefix}:fence:{resource}` as in the issue's snippet; SQL tables keyed by resource) | One placement; the contract is exactly per resource | One never-expiring key or row per distinct resource name (names like `saga:payment:{orderId}` are high-cardinality), or a TTL that would reset the counter and make the new tokens lower than the highest one the resource has already seen |

### Chosen Option: **A - one counter per backend, a per-resource counter next to the lock on Azure Blob and DynamoDB** (recommended, pending the maintainer)

### Rationale

- A wins for high-cardinality resource names; B wins when per-resource counters are wanted for operations (reading the last token of one resource) and the key growth is acceptable. The atomicity requirement is not a choice: with a second step after the grant a holder whose lock expired between its grant and its token draw would receive a larger token than the next holder. On Azure Blob and DynamoDB a global counter cannot be advanced atomically with the grant (DynamoDB `TransactWriteItems` returns no values; an Azure counter blob is writable without the lease), so those two use a per-resource counter next to the lock under both options (blob metadata under the lease; the DynamoDB counter item of Design Choice 6).
- SQL Server, PostgreSQL and MySQL need the sequence or counter row created: the opt-in `EnsureFencingSchema` (default `false`, a shipped DDL script) follows the answer to #215 Dilemma 3.
- The Redis key is `fence` when `KeyPrefix` is empty and `{KeyPrefix}:fence` otherwise (a `GetFenceKey` next to `GetLockKey`, which already handles the empty prefix at `RedisDistributedLockProvider.cs:173-177`); it cannot collide with a lock key, which always contains `lock:`.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Redis counter durability</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A plain `INCR` on the fence key; the README requires persistence (AOF with `appendfsync always`; `everysec` still accepts a window) and states that the contract holds only while the counter never regresses** | Simple; the contract is the Kleppmann one | **Unsafe when the counter regresses** (worked counter-example below): a flush, a restart without persistence or a failover to a lagging replica lets a stale holder's late write through |
| **B) The token is `max(INCR, floor)` where the floor is the Redis `TIME` in microseconds, computed in the same script (`local c = redis.call('INCR', KEYS[2]); local floor = t[1] * 1000000 + t[2]; if c < floor then redis.call('SET', KEYS[2], floor); c = floor end`, with `t = redis.call('TIME')`; script effects replication, the default since Redis 5, makes the non-deterministic `TIME` read legal)** | A flushed or lagging counter restarts above every earlier token, because earlier tokens are either small counts or earlier microsecond values; no operator duty beyond a sane clock | Tokens are large time-flavoured numbers with gaps (the contract already says "not gap-free"); safe only while the Redis server clock does not run backwards past the last token issued (a failover to a node whose clock is behind is the residual case, stated in the README) |

**Worked counter-example for A.** Holder A is granted token 7 and stalls (a GC pause) before it writes. The Redis counter is lost (flush, or a restart without persistence). A's lock expires and holder B is granted the lock; the counter restarts, so B draws token 1 (or, after enough grants, the same value 7). B writes to the resource with token 1: the resource has seen no token yet, so it accepts and records 1. A resumes and writes with token 7: 7 is higher than 1, so the resource accepts it, and A's stale write lands after B's. With a repeated value (B draws 7 too) the resource, which rejects only tokens lower than the highest seen, accepts both holders. This is exactly the write fencing exists to stop; a lost counter can therefore make the resource accept a stale token, not only reject one. Under B, B draws at least the microsecond floor, far above 7, and A's late write with 7 is rejected.

### Chosen Option: **B - counter with a time floor** (recommended, pending the maintainer)

### Rationale

- The contract of the Summary is "strictly increasing per resource"; A delivers it only while the counter persists and never regresses, which the provider cannot verify, so an operator mistake silently disables the safety property. B keeps the property across counter loss as long as the Redis clock is monotonic. A wins when Redis is run with durable persistence (AOF `always`) by policy and plain small integers are wanted; the maintainer chooses. Under either option the README states the residual condition, and the contract suite on Redis adds a scenario that flushes the counter key between two grants and asserts the second token is still larger (it fails for A by design: with A that scenario is dropped and the README requirement stays).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Redis quorum mode (#226)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In quorum mode the token comes from the counter of the first configured endpoint only; the XML docs and README state that the quorum protects the lock, not the token** | The lock keeps working with N endpoints; one place for the counter | If that endpoint fails over, the counter may regress (Design Choice 2; the time floor of option B narrows it, plain `INCR` does not); the quorum does not make tokens quorum-safe (the known limit of Redlock with fencing) |
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
| **A) Interface early: PRs 1 to 3 give the three existing handles the property; PR 4 then adds the member and the contract suite (with subclasses for the three existing providers); every handle created afterwards (#207 to #210, #609, #724) implements the property in its own PR** | PR 4 does not wait for four packages that do not exist yet; each new package is written against the contract suite | PRs 5 to 8 are not separate pull requests: their token work is a task inside each package's own PR (their size budgets in the #207 to #210 plans must absorb about 400 lines each), and if a package merges before PR 4, PR 4 must also implement the property on that handle |
| **B) Interface last: PRs 1 to 3, then PRs 5 to 8 add the property to each new handle after its package merged, and PR 4 (interface member, PublicAPI, README, subclasses of all seven providers) merges only when every `ILockHandle` implementer on `main` has it** | Keeps the 8 small PRs; every PR builds and passes alone; no package PR carries token work | The `FencingToken` interface member and the contract suite wait for the four planned packages (and the contract PR is the last one); implementers created after PR 4 (#609, #724 `InstrumentedLockHandle`) must implement the member in their own PR |

**Which PR adds the property to which handle type** (both options):

| Handle type | Class (file, today or planned) | Adds the property |
|-------------|--------------------------------|-------------------|
| InMemory | `LockHandle` in `InMemoryDistributedLockProvider.cs:250` | PR 1 |
| Redis | `LockHandle` in `RedisDistributedLockProvider.cs:223` | PR 2 |
| SQL Server | `LockHandle` in `SqlServerDistributedLockProvider.cs:242` | PR 3 |
| PostgreSQL, MySQL, Azure Blob, DynamoDB | private `LockHandle` of #207, #208, #209, #210 | A: the package's own PR; B: PR 5, 6, 7, 8 (after the package merged) |
| MongoDB lock | private `LockHandle` of #609 (outside the seven providers) | whichever of #609 and PR 4 merges second |
| `InstrumentedLockHandle` | #724 | whichever of #724 and PR 4 merges second (it forwards the inner handle's token) |

**Build order**: under B, PRs 1 to 3 (any order after #2667), then PRs 5 to 8 as each package merges, then PR 4; under A, PRs 1 to 3, PR 4, then each package PR with its token. A default interface member (`long FencingToken => 0`) is rejected under both: it would be a compatibility layer (`AGENTS.md` section 3) and would let an implementer return a handle with no token, which the contract forbids.

### Chosen Option: **B - interface last, so every pull request stays small and green** (recommended, pending the maintainer)

### Rationale

- Both options keep every PR green; they differ in who carries the token work of the four planned providers and when the contract suite exists. A wins when the maintainer wants the contract and the interface member before the cloud and database packages are written; B wins when the 8-PR split with its size limits matters more. The member is only valid once every implementer has the property; the grep of `docs/plans/` shows the four packages, #609 and #724 as the unwritten implementers, and the Next Steps and the briefs of #207 to #210, #609 and #724 carry the rule "whichever PR lands second adds the property".

**Class:** Dilemma

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

<details>
<summary><strong>6. DynamoDB: where the counter lives given the design of #210</strong></summary>

### What the #210 plan on main does (quoted, not paraphrased)

- Acquire is a `PutItem` with the condition `attribute_not_exists(LockKey) OR ExpiresAtMs < :now` (its Summary item 1, gateway method `TryPutAsync`); it is **not** an `UpdateItem`. A `PutItem` replaces the whole item, so a `FencingToken` attribute written by an earlier holder is wiped by the next acquire.
- Its Dilemma 5 recommends **delete on release** (`DeleteAsync` with `LockId = :id`); option B (keep the item, `Version` keeps counting) is pending the maintainer. It also notes "a counter that the TTL can reset is not a sound token".
- Its Dilemma 4 recommends creating the table with **TTL enabled on `Ttl`**, an attribute written with every lock item; DynamoDB deletes items past `Ttl`, which also resets any counter stored in them.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The counter lives in the lock item: the acquire becomes a conditional `UpdateItem` (`SET LockId, ExpiresAtMs ... , FencingToken = if_not_exists(FencingToken, :zero) + :one`, `ReturnValues = UPDATED_NEW`); the item is never deleted (release clears `LockId` and sets `ExpiresAtMs = 0`) and `Ttl` is not written and TTL is not enabled on the table** | One write per grant; atomic with the lock; the token comes back in the response | Changes three decisions of #210 (acquire `PutItem` to `UpdateItem`, Dilemma 5 forced to "keep the item", Dilemma 4 table without TTL); one item per resource ever locked, never cleaned |
| **B) The counter lives in a separate counter item per resource (`LockKey = fence#{resource}`, attribute `FencingToken`, never deleted, no `Ttl`); the acquire becomes one `TransactWriteItems`: the `Put` of the lock item with #210's condition plus an `Update` of the counter item `SET FencingToken = :expected + 1` with the condition `FencingToken = :expected` (or `attribute_not_exists(LockKey)` when `:expected` is absent), where `:expected` comes from a consistent `GetItem` just before; a counter condition failure re-reads and retries inside the acquire loop** | #210's lock item keeps its delete on release and its TTL cleanup; the token is known client side and both writes are atomic, so a stalled holder cannot draw a larger token than the next holder | Changes #210's acquire from `PutItem` to a transaction (two writes, double the write cost, one extra read, retries under contention on the same resource); one small never-cleaned counter item per resource ever locked |

Both options keep the item that carries the counter out of reach of the `Ttl` cleanup and of the release delete. A third shape, a global counter item drawn after the grant, is excluded: with a second step after the grant a holder whose lock expired between the two steps would draw a token larger than the next holder's (Design Choice 1).

### Chosen Option: **B - separate counter item advanced in the acquire transaction** (recommended, pending the maintainer)

### Rationale

- B leaves #210's release and TTL decisions (its Dilemmas 4 and 5) free, which is why it is recommended; A wins when one write per grant and one item per resource matter more and the maintainer is already choosing "keep on release, no TTL" for #210 Dilemma 5. PR 8 depends on #210's package being merged with its acquire exposed through the `IDynamoLockGateway` seam of its plan, and modifies `DynamoDBLockGateway.TryPutAsync` into a transactional acquire (option B) or an `UpdateItem` acquire (option A). The counter is never deleted and never given a `Ttl` attribute under either option.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Providers That Exist (PRs 1 to 3)

> **Goal**: The three existing providers issue tokens on their handles.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 1 - InMemory** - `InMemoryDistributedLockProvider.cs` (modify): a `long _fencingCounter` field advanced with `Interlocked.Increment` inside the grant (under the same entry update that takes the lock); the private `LockHandle` gets `public long FencingToken { get; }`. The counter is per provider instance, not static (an InMemory provider is one lock universe). **Also in PR 1: `FencingErrorCodes.cs`** (new, public static, `src/Encina.DistributedLock/`; the home of the code constant `FencingFailed = "lock.fencing_failed"`, ADR-062 category `Internal`, R4, and `FencingUnsupported = "lock.fencing_unsupported"` only if Design Choice 3 option B is chosen; `LockErrorCodes` is #2667's class and is not edited here, as #215 and #217 also keep their own code classes), with its `PublicAPI.Unshipped.txt` lines (via `mechanical-fixer`).
2. **PR 2 - Redis** - `RedisDistributedLockProvider.cs` (modify): acquisition becomes one Lua script. Under the recommended option B of Design Choice 2: `if redis.call('SET', KEYS[1], ARGV[1], 'NX', 'PX', ARGV[2]) then local t = redis.call('TIME'); local floor = t[1] * 1000000 + t[2]; local c = redis.call('INCR', KEYS[2]); if c < floor then redis.call('SET', KEYS[2], floor); c = floor end; return c else return 0 end`; under option A the `TIME` and floor lines are absent and the script is `... then return redis.call('INCR', KEYS[2]) else return 0 end`. Keys: the lock key and the fence key (`fence` for an empty `KeyPrefix`, `{KeyPrefix}:fence` otherwise, via `GetFenceKey`); a `0` result is contention; the handle gets `FencingToken`; a script failure is `Left` `lock.fencing_failed` (the lock is not held: the script is atomic). README states the single-node limit and the residual condition of the chosen option (Design Choice 2).
3. **PR 3 - SQL Server** - `SqlServerDistributedLockProvider.cs` (modify): after `sp_getapplock` succeeds, `SELECT NEXT VALUE FOR [dbo].[EncinaLockFencingToken]` on the same connection; on failure release the lock and return `Left` `lock.fencing_failed`; `SqlServerLockOptions` gets `EnsureFencingSchema` (default `false`) and the sequence name, **and their lines in `src/Encina.DistributedLock.SqlServer/PublicAPI.Unshipped.txt`** (RS0016, via `mechanical-fixer`) plus an options test; `Scripts/LockFencing.sql` (new); a missing sequence is a `Left` that names no object. The sequence is created with `CACHE` unset or a documented small cache: a sequence cache can skip values after a restart (gaps are allowed) but never reorders them, so strict increase holds.
4. Each PR: unit tests (token greater than zero, strictly increasing across successive holders, a second holder after expiry gets a larger token, `ExtendAsync` keeps the token, a token failure leaves no lock held), integration tests for Redis and SQL Server on the shared fixtures (50 concurrent acquirers: the tokens are unique and ordered by the order of holding), manifest rows. **SQL Server and expiry**: the SQL Server lock does not enforce its expiry today (#2405, `SqlServerDistributedLockProvider.cs:276-278`), so the scenario "a holder after expiry gets a larger token than the expired one" cannot pass there until #2405 lands; PR 3 and PR 4 skip it for SQL Server with a `Skip` reason naming #2405 (deleted in the PR that lands after #2405), and the successive-holders-after-release and concurrent scenarios run on SQL Server either way. PublicAPI: PR 1 adds the `FencingErrorCodes` lines; PRs 2 and 3 add only the PR 3 options lines above (the handles are private classes).

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
3. **`tests/Encina.ContractTests/DistributedLock/FencingTokenContractTests.cs`** (new) - an abstract base: successive holders get increasing tokens; concurrent contenders get unique tokens in holding order; a holder after expiry gets a larger token than the expired one (**SQL Server: skipped with a `Skip` reason naming #2405 until it lands**); the token is positive and stable across `ExtendAsync`; on Redis, with Design Choice 2 option B, the counter key is deleted between two grants and the second token is still larger; subclasses for InMemory, Redis and SQL Server (under Design Choice 4 option B the four later providers' subclasses are added here too, in the same file; under option A each package PR adds its own).
3a. **`tests/Encina.ContractTests/Encina.ContractTests.csproj`** (modify) and **`tests/Encina.ContractTests/DistributedLock/FencingBackendCollections.cs`** (new) - `Encina.ContractTests` has no reference to any `Encina.DistributedLock*` project and no Redis or SQL Server collection today (verified in its csproj on main); PR 4 adds the `ProjectReference` lines (`Encina.DistributedLock`, `.InMemory`, `.Redis`, `.SqlServer`, plus `.PostgreSQL`, `.MySQL`, `.AzureBlob`, `.DynamoDB` under option B) and the `[CollectionDefinition]` classes over `RedisFixture` and `SqlServerFixture` of `Encina.TestInfrastructure` (the LocalStack collection already exists in that assembly; Azurite, PostgreSQL and MySQL ones are added when their subclasses are). If #216's PR 3 merged first it already added the four lock references and the Redis and SQL Server collections: this PR then adds only what is missing.
4. **Wrappers and later implementers** - when `InstrumentedLockHandle` (#724) or the #609 handle exists on `main` at this point, forward or implement the property in this PR and add the span attribute of Design Choice 5 in the #724 wrapper; when they do not exist yet, their PRs implement the property (the rule is in their briefs and in the Next Steps).
5. **README** section "Fencing tokens" with the resource-side check example and the per-provider durability table (via `docs-writer`); changelog fragment `changelog.d/218-lock-fencing-token.added.md` (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #218: the FencingToken member of ILockHandle and its contract suite.

CONTEXT:
- PRs 1 to 3 already gave the three provider handles the property (and, under Design Choice 4 option B, PRs 5 to 8 the other four); adding the member must not break an implementer.
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

> **Goal**: Tokens on the other four providers, one pull request each, after their package merged. These four PRs exist as separate pull requests under Design Choice 4 option B; under option A the same tasks are a "Fencing token" task of each package's own PR (#207 to #210).

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 5 - PostgreSQL** (after #207): `SELECT nextval('encina_lock_fencing_token')` on the connection that holds the advisory lock; DDL script `CREATE SEQUENCE ... CACHE 1` (the default, and required: with a larger cache each session pre-allocates a block, so values are handed out per session out of grant order and a later grant can draw a lower token); `EnsureFencingSchema`; the private `LockHandle` of #207 gets the property.
2. **PR 6 - MySQL** (after #208): `UPDATE encina_lock_fencing SET value = LAST_INSERT_ID(value + 1)` then `SELECT LAST_INSERT_ID()` on the session that holds `GET_LOCK`; DDL script; `EnsureFencingSchema`.
3. **PR 7 - Azure Blob** (after #209): the lock blob keeps the counter in its metadata (`fencingtoken`); after the lease is acquired the provider reads it, writes `value + 1` with the lease id (a stale holder without the lease cannot write), and the lock blob is not deleted on release; Azurite tests.
4. **PR 8 - DynamoDB** (after #210 merged; option B of Design Choice 6 does not wait for #210's Dilemmas 4 and 5, option A does): the counter design is Design Choice 6 (recommended option B): `DynamoDBLockGateway.TryPutAsync` of #210 (a conditional `PutItem`, not an `UpdateItem`) becomes a transactional acquire (`TransactWriteItems`: the lock `Put` with #210's condition plus the `Update` of the counter item `fence#{resource}` conditioned on the value just read by a consistent `GetItem`; counter condition failure retries inside the acquire loop). The counter item is never deleted and never written with `Ttl`, so neither #210's delete on release nor its TTL cleanup of lock items resets it; the private `LockHandle` of #210 gets the property from the acquire result; LocalStack tests (including a release followed by a TTL-style delete of the lock item, then a re-acquire with a larger token). Under option A the acquire is a conditional `UpdateItem` and the PR also forces #210's release to keep the item and its table to be created without TTL.
5. Each PR adds its manifest lines; the contract subclass is added by PR 4 under Design Choice 4 option B, or by the package's own PR under option A. No interface change in PRs 5 to 8.

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
| Redis | `INCR` of the fence key with a `TIME` floor (Design Choice 2) inside the acquire script | exists | PR 2 |
| SQL Server | sequence read on the session that holds `sp_getapplock` | exists | PR 3 |
| PostgreSQL | sequence read on the advisory-lock session | planned (#207) | PR 5 |
| MySQL | `LAST_INSERT_ID` counter row on the `GET_LOCK` session | planned (#208) | PR 6 |
| Azure Blob | lock-blob metadata written under the lease | planned (#209) | PR 7 |
| DynamoDB | per-resource counter item advanced in the acquire transaction (Design Choice 6) | planned (#210) | PR 8 |

### Event ID Allocation

| Package | Range | Ids |
|---------|-------|-----|
| none | none | No EventId is used or registered by this plan (Design Choice 5); the family range `DistributedLockPrimitives` (30550-30564) belongs to #215, #216, #217 and #219 |

### Estimated File Count

| PR | Content | New | Modified | Tests, manifests, PublicAPI, docs | Total | Lines (est.) |
|----|---------|-----|----------|------------------------------------|-------|--------------|
| 1 | InMemory, error codes | 1 (`FencingErrorCodes.cs`) | 1 | 3 (unit, manifest, PublicAPI core) | 5 | 400 |
| 2 | Redis | 0 | 1 | 4 (unit, integration, manifest, README note) | 5 | 500 |
| 3 | SQL Server | 1 (DDL script) | 2 (provider, options) | 5 (unit, options unit, integration, manifest, PublicAPI SqlServer; README note counted in PR 4) | 8 | 800 |
| 4 | Interface, contract suite | 2 (contract suite with its subclasses, `FencingBackendCollections.cs`) | 2 (`ILockHandle.cs`, `Encina.ContractTests.csproj`) | 5 (PublicAPI, README section, fragment, manifest, #724 wrapper forwarding when present) | 9 | 900 (up to 1,100 with the four later subclasses under Design Choice 4 option B) |
| 5 | PostgreSQL | 1 (DDL) | 1 | 3 (unit, integration, manifest) | 5 | 400 |
| 6 | MySQL | 1 (DDL) | 1 | 3 | 5 | 450 |
| 7 | Azure Blob | 0 | 1 | 3 | 4 | 450 |
| 8 | DynamoDB | 0 | 2 (provider/handle, `DynamoDBLockGateway.cs`) | 3 (unit, LocalStack integration, manifest) | 5 | 600 |

Re-summed: 5 + 5 + 8 + 9 + 5 + 5 + 4 + 5 = 46 files; 400 + 500 + 800 + 900 + 400 + 450 + 450 + 600 = 4,500 lines (the header states about 4,500). Every PR is at most 15 files and 1,500 lines and builds and passes alone (the interface changes in PR 4 once every implementer on `main` has the property, Design Choice 4).

### Per-file coverage targets (numeric, per flag)

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `InMemoryDistributedLockProvider.cs` (token paths) | 95 | 13 | 80 | 0 | Existing file (manifest today: unit 95, guard 13); the added lines are in-process and fully reachable with `FakeTimeProvider`; guard stays at the manifest's current value because no parameter is added |
| `RedisDistributedLockProvider.cs` (token paths) | 95 | 14 | 70 | 75 | Existing file (manifest today: unit 95, guard 14, integration 70): no target is lowered, as the ratchet requires; script arguments and result mapping are unit-testable with a mocked `IDatabase`; atomicity and counter loss need a real Redis (integration up from 70 to 75 with the counter-loss and concurrency tests) |
| `SqlServerDistributedLockProvider.cs` (token paths) | 62 | 6 | 70 | 93 | Existing file (manifest today: unit 57, guard 6, integration 93): guard stays at 6 because no parameter is added (a higher guard target would be unreachable), integration stays at 93, unit rises to 62 with the failure-to-`Left` mapping tests; sequence behavior needs a real server |
| `FencingErrorCodes.cs` | 0 | 0 | 0 | 0 | Public constants only: no executable code; exercised through the provider tests that assert the code |
| `ILockHandle.cs` | 0 | 0 | 0 | 0 | An interface member: no executable code; covered through the providers |
| `PostgreSQLDistributedLockProvider.cs`, `MySQLDistributedLockProvider.cs`, `AzureBlobDistributedLockProvider.cs`, `DynamoDBDistributedLockProvider.cs` (token paths, PRs 5 to 8) | 90 | 14 | 60 | 70 | New files of #207 to #210 whose plans set unit 90, guard 14, contract 45, integration 70; this plan never lowers those and raises contract to 60 with the fencing scenarios; the token lines are reachable with the fake session or gateway of each package, guard stays because no parameter is added |
| `DynamoDBLockGateway.cs` (transactional acquire, PR 8) | 5 | 5 | 0 | 90 | Internal, SDK calls that need LocalStack: the values of the #210 plan (5 / 5 / 0 / 90) are kept, never lowered; unit and guard reach only the argument guards, the transaction itself runs in the integration flag |

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
- Grep every ILockHandle implementer and substitute (git grep, and the plans under docs/plans for ': ILockHandle') before changing the interface; whichever PR lands second adds the property.
- SQL Server does not enforce expiry until #2405: skip "holder after expiry" there with a reason naming it.
- Dilemmas 1, 2, 3, 4 and 6 are pending the maintainer: implement the recommended options only after he answers.

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
| #2405 SQL Server lock ignores its expiry (open bug) | open | The SQL Server "holder after expiry gets a larger token" scenario in PR 3 and PR 4 (skipped with the reason until it lands); no other scenario or PR |
| #724 `InstrumentedLockHandle`, #609 MongoDB lock handle | plans on main, code not written | PR 4 (forwarding or implementing the property), only if merged first; otherwise their own PRs implement it |
| #215 Dilemma 3 (schema provisioning) | pending the maintainer | PRs 3, 5, 6 (`EnsureFencingSchema`) |
| #207, #208, #209, #210 | plans on main, packages not written | PRs 5 to 8 (and PR 4 under Design Choice 4 option B) |
| #210 Dilemmas 4 and 5 (TTL on `Ttl`, delete on release) | pending the maintainer | PR 8: option B of Design Choice 6 leaves both free; option A forces "keep the item" and "no TTL" |

---

## Next Steps

1. After #2667 merges open PRs 1, 2 and 3 in parallel; then, under Design Choice 4 option B (recommended), PRs 5 to 8 as each provider package merges, and PR 4 (the interface member) last; under option A, PR 4 right after PRs 1 to 3.
2. The maintainer answers Dilemmas 1, 2, 3, 4 and 6 when work on the issue starts; PR 1 does not depend on them, the Redis script of PR 2 and the DynamoDB design of PR 8 do.
3. Rule for every implementer of `ILockHandle` that is not written yet (put it in the briefs of #207, #208, #209, #210, #609 and #724): whichever of that PR and PR 4 of this plan lands second adds `FencingToken` to its handle.
4. Open the tenant-scoping follow-up issue once for the whole family.
