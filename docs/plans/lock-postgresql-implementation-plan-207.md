# Implementation Plan: `Encina.DistributedLock.PostgreSQL` — distributed lock provider on PostgreSQL advisory locks

> **Issue**: [#207](https://github.com/dlrivada/Encina/issues/207)
> **Type**: Feature
> **Complexity**: Medium (one new provider package, 4 pull requests)
> **Estimated Scope**: ~650-800 lines of production code + ~1,200-1,500 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-003, DEC-009, AC-027
> **Decision**: [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (lock failure contract), [ADR-062](../architecture/adr/062-error-category-contract.md) (error categories), [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) (D4: tenant tags on telemetry)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision; it blocks this issue)
> **Related**: [#208](https://github.com/dlrivada/Encina/issues/208) (MySQL provider, same family), [#209](https://github.com/dlrivada/Encina/issues/209), [#210](https://github.com/dlrivada/Encina/issues/210), [#226](https://github.com/dlrivada/Encina/issues/226) (Redis RedLock), [#741](https://github.com/dlrivada/Encina/issues/741) (tenant-scoped locks), [#755](https://github.com/dlrivada/Encina/issues/755) (health checks), [#724](https://github.com/dlrivada/Encina/issues/724) (lock observability), [#218](https://github.com/dlrivada/Encina/issues/218) (fencing), [#223](https://github.com/dlrivada/Encina/issues/223) (auto-extend), [#2405](https://github.com/dlrivada/Encina/issues/2405), [#2720](https://github.com/dlrivada/Encina/issues/2720), [#2722](https://github.com/dlrivada/Encina/issues/2722), [#2724](https://github.com/dlrivada/Encina/issues/2724) (defects of the SQL Server provider this plan must not repeat)

---

## Summary

Add the package `Encina.DistributedLock.PostgreSQL`, an `IDistributedLockProvider` over PostgreSQL **advisory locks** (`pg_try_advisory_lock`, `pg_advisory_unlock`, and the transaction-level `pg_try_advisory_xact_lock`). It is the PostgreSQL entry of the seven lock providers SPEC-000 DEC-009 fixes for 1.0 (InMemory, Redis, SqlServer, PostgreSQL, MySQL, Azure Blob, DynamoDB).

1. **One dedicated connection per live handle.** An advisory lock belongs to the database session, so the handle owns an `NpgsqlConnection` from acquire to release (the model of the SQL Server provider). The connection is unpooled (design choice 9), so closing it always ends the session and the server frees the lock, even when `pg_advisory_unlock` fails.
2. **Poll, do not block.** Acquire calls `pg_try_advisory_lock` every `retry` until the `wait` deadline, so the `retry` parameter of the interface has its meaning, delays use `TimeProvider`, and cancellation needs no server-side cancel request.
3. **Expiry is enforced.** An advisory lock has no TTL, so the handle arms a `TimeProvider` timer that releases the lock at `ExpiresAtUtc`; `ExtendAsync` verifies on the server that the session still holds the lock and moves the deadline. A crashed process loses its connection, and the server frees the lock.
4. **ADR-052 outcomes.** Acquired is `Right(Some(handle))`, contention or the wait elapsing is `Right(None)`, a database fault is `Left`, cancellation propagates.
5. **Non-intrusive probe.** `IsLockedAsync` reads `pg_locks`; it never takes the lock to test it (the defect of #2722 in SQL Server).
6. **Operations.** A health check, `[LoggerMessage]` EventIds 7550-7557, an `ActivitySource` and a `Meter`.

**Standards**: PostgreSQL documentation "Explicit Locking, Advisory Locks" (`pg_try_advisory_lock`, `pg_advisory_unlock`, `pg_advisory_xact_lock`, `pg_locks`) and "DISCARD"; ADR-052; SPEC-000 REQ-027 / AC-027.

**Estimated scope**: 9 new production files (csproj, options, key, session seam and its Npgsql adapter, provider, diagnostics, health check, registration), 4 modified metadata files, about 14 test files, README and changelog fragment; 4 pull requests (see Implementation Phases).

**Affected packages**: `Encina.DistributedLock.PostgreSQL` (new); `Encina` (EventId registry and its `PublicAPI.Unshipped.txt`); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests` (project references and tests). `Encina.DistributedLock` is consumed, not changed.

**Provider category**: Distributed locks (`AGENTS.md` section 5). The matrix is in Research. The issue's wording about "Database (10)" does not apply: the database matrix covers stores, and the lock set is the closed list of seven.

**What the code does today** (`origin/main` at `0b941beeb`):

- The package does not exist. `src/` has `Encina.DistributedLock`, `.InMemory`, `.Redis` and `.SqlServer`; `Npgsql` 10.0.2 is already in `Directory.Packages.props`; `Encina.slnx` lists the four lock projects.
- `IDistributedLockProvider` still has the pre-ADR-052 shapes (`Task<IAsyncDisposable?>`, `Task<bool>`). [#2667](https://github.com/dlrivada/Encina/issues/2667) changes them and creates `LockErrorCodes`; **this plan is written to the ADR-052 contract and its implementation starts after #2667 merges**.
- `SqlServerDistributedLockProvider` is the closest model: one `SqlConnection` per handle, `GetLockKey` builds `lock:{resource}` or `{KeyPrefix}:lock:{resource}`, a health check registered when `ProviderHealthCheck.Enabled`, `[LoggerMessage]` EventIds 3800-3804. Its known defects (probe takes the lock, `ExtendAsync` is a no-op, negative codes collapse) are issues #2722, #2405, #2720, and are not repeated here.
- `PostgreSqlFixture` (`tests/Encina.TestInfrastructure/Fixtures/PostgreSqlFixture.cs`, `postgres:17-alpine`) and the collection `ADO-PostgreSQL` (`tests/Encina.IntegrationTests/ADO/Collections.cs`) exist.
- No contract test class for locks exists yet; #2667 creates the suite and the plan of #755 adds the first project reference to the contract test project. `Encina.ContractTests` defines no `ADO-PostgreSQL` collection (`PostgreSqlFixture` is reachable through its `Encina.TestInfrastructure` reference; the collection lives only in `Encina.IntegrationTests/ADO/Collections.cs`), so PR 4 creates one.

---

## Design Choices

<details>
<summary><strong>1. Package placement and name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.DistributedLock.PostgreSQL`** | The name SPEC-000 REQ-027 uses; same shape as `Encina.DistributedLock.SqlServer`; pay-for-what-you-use (Npgsql only for those who ask) | One more package, README and manifest |
| **B) A lock folder inside a PostgreSQL package** | Fewer packages | No `Encina.PostgreSQL` package exists; mixes a coordination feature into a store package; breaks the `Encina.DistributedLock.{Backend}` naming |

### Chosen Option: **A — new package `Encina.DistributedLock.PostgreSQL`**

### Rationale

- SPEC-000 REQ-027 names the package, and the three existing providers are separate packages with a `DistributedLock` prefix.

**Class:** Settled
**Settled by:** SPEC-000 REQ-027; `AGENTS.md` section 5 (the lock table names `PostgreSQL (pg_advisory_lock, #207)`)

</details>

<details>
<summary><strong>2. Lock modes — session only, or session plus an opt-in transaction mode</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Session-level exclusive locks only** | One code path; simplest to test and to reason about | The lock cannot be used behind PgBouncer in transaction-pooling mode, where one client session is not one server session |
| **B) Session-level by default, transaction-level (`pg_try_advisory_xact_lock`) as an opt-in `PostgreSQLLockOptions.UseTransactionLocks`** | The issue's own task list; works behind a transaction pooler; the server frees the lock when the transaction ends, even if the unlock call never runs | A second path: the handle holds an open transaction for the whole hold time (idle in transaction), so long holds keep a snapshot and need `idle_in_transaction_session_timeout` awareness |

### Chosen Option: **B — session-level by default, `UseTransactionLocks` as an opt-in** (recommended, pending the maintainer)

### Rationale

- The issue lists the transaction mode as an implementation task, and the default stays the simple, safe session mode (pay-for-what-you-use).
- Option A wins when no deployment goes through a transaction pooler and the team wants the smaller surface; the maintainer can pick it when work starts. Shared (reader/writer) locks, also in the issue's task list, are not in either option: `IDistributedLockProvider` is an exclusive-lock contract that every contract test asserts, so a provider-wide `UseSharedLocks` flag would silently turn every lock into a shared one. They need a reader/writer abstraction, recorded in the follow-up issue file `lock-reader-writer-abstraction` (see Next Steps).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Key derivation — client-side SHA-256 to a 64-bit key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Client: the logical key `{KeyPrefix}:lock:{resource}` (UTF-8) hashed with SHA-256, the first 8 bytes as `bigint`** | Deterministic for every client and PostgreSQL version; no server function dependency; one parameter | A 64-bit space: two names may collide (probability about 2^-64 per pair) |
| **B) Server: `hashtextextended(@key, 0)`** | No client hashing code | A server-internal function whose output is not a documented contract across major versions |
| **C) A lock table with a `text` key and row locks** | No collisions | Not an advisory lock; table bloat and cleanup, which the issue rejects as an alternative |

### Chosen Option: **A — client-side SHA-256 to `bigint`**

### Rationale

- A collision only makes two unrelated resources share one lock, which costs concurrency and never mutual exclusion, so the failure is on the safe side. The tenant-qualified name composed by [#741](https://github.com/dlrivada/Encina/issues/741) (reserved `default` segment, injective encoding) is hashed as a whole, so distinct tenant/resource pairs stay distinct up to that bound.
- The family uses the same logical key as the SQL Server provider (`GetLockKey`), so a `KeyPrefix` behaves identically across providers.

**Class:** Trivial
**Settled by:** named comparable product `madelson/DistributedLock` (DistributedLock.Postgres hashes names client-side into the 64-bit key space, https://github.com/madelson/DistributedLock/blob/master/docs/DistributedLock.Postgres.md); `src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs` (`GetLockKey`)

</details>

<details>
<summary><strong>4. Acquire strategy — poll the try-lock, or block on the server</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Poll `pg_try_advisory_lock` every `retry` until the `wait` deadline** | Matches the interface (`wait`, `retry`); delays use `TimeProvider`, so tests use `FakeTimeProvider`; cancellation is a token check between polls | Up to one `retry` of latency after the holder releases; no fairness between waiters |
| **B) Block on `pg_advisory_lock` with `SET lock_timeout`** | No polling latency; the server queues waiters | `lock_timeout` is session state that must be reset; cancelling a blocked call needs a server-side cancel request; the `retry` parameter is meaningless |

### Chosen Option: **A — poll the try-lock** (recommended, pending the maintainer)

### Rationale

- The `retry` parameter exists only for a polling provider, every other provider polls, and a single deterministic loop is what the contract tests and the property tests exercise. `AcquireAsync` is the same loop without the deadline, using `DefaultRetry`.
- Option B wins for a hot lock with many waiters where the polling latency matters more than the testability; the maintainer can pick it when work starts.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Expiry semantics — enforce the expiry, or leave it informational</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Informational: `ExpiresAtUtc` is recorded, the lock lives until disposal or connection loss** | No timer; what the SQL Server provider does today | The interface promises auto-release after `expiry`; an operation that hangs holds the lock forever while its process lives |
| **B) Enforced: a `TimeProvider` timer releases the lock and closes the session at `ExpiresAtUtc`; `ExtendAsync` moves the deadline** | A similar contract to Redis (`PX`) and the cloud providers, but client-enforced: a hung holder whose process still runs cannot starve others | Redis `PX` is enforced by the server, while this timer runs in the holder's process: in a suspended process (debugger stop, SIGSTOP, VM freeze) the timer cannot fire and the advisory lock stays held until unlock or until the session ends (the server drops it only on connection loss, for example by TCP keepalive); a timer callback per live handle (it must never throw, `AGENTS.md` section 3); a holder that works past its expiry loses the lock without being told until it calls `ExtendAsync` or disposes |

### Chosen Option: **B — enforced expiry** (recommended, pending the maintainer)

### Rationale

- `IDistributedLockProvider.TryAcquireAsync` documents that the lock "is also released after the expiry time, even if not explicitly disposed", and the contract tests (AC-027) run the same expiry scenario on all seven providers.
- Option A wins when the work under the lock legitimately outlasts any bound and the connection lifetime is the intended guard; the maintainer can pick it when work starts.
- The timer callback catches every exception and only logs the exception type (`AGENTS.md` section 3, background rule). Releasing from a timer thread runs `pg_advisory_unlock` with a short timeout and then closes the connection, so a failed unlock still frees the lock.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Probe and provider-level extend</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IsLockedAsync` reads `pg_locks` (advisory, this database, the key's `classid`/`objid`/`objsubid`); provider `ExtendAsync(resource)` extends only a handle this provider instance holds, else `Right(false)`** | No lock is taken to test one; ownership is real, because only the owning process has the handle | The `pg_locks` view needs no special privilege for the own database but shows locks of all sessions only to a role that may read them (the default `pg_locks` is readable by every role) |
| **B) Probe by trying the lock and releasing it; provider `ExtendAsync` answers `Right(true)`** | Trivial code | The probe makes a concurrent contender fail for an instant (false contention) and can hold the lock if the unlock fails: the defect of #2722; `Right(true)` for a lock nobody checked is the defect of #2405 |

### Chosen Option: **A — `pg_locks` probe and a local handle registry**

### Rationale

- ADR-052 defines `ExtendAsync` as `Right(false)` when the lock is "not held"; only a registry of the live handles of this provider can answer that truthfully without a handle argument, and `ILockHandle.ExtendAsync` additionally runs `SELECT 1 FROM pg_locks WHERE pid = pg_backend_pid() AND locktype = 'advisory' AND ...` to detect a session that lost the lock.

**Class:** Settled
**Settled by:** #2722 and #2405 (the defects option B reproduces); ADR-052 ("not held" is `Right(false)`)

</details>

<details>
<summary><strong>7. Testability seam and connection ownership</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An `internal` seam `IAdvisoryLockSession` (`TryLockAsync`, `UnlockAsync`, `HoldsLockAsync`, `DisposeAsync`) with one Npgsql adapter; the provider holds all the logic** | The retry loop, expiry timer, registry and error mapping run in unit tests against a fake; only the thin adapter needs a database (the manifest of the SQL Server provider shows the cost of not having it: unit 57, integration 93) | One more internal file |
| **B) The provider creates `NpgsqlConnection` inline** | Fewer files | Npgsql types are not mockable: the logic is only reachable through Docker |

### Chosen Option: **A — session seam**

### Rationale

- The blocking `crap-gate` (`AGENTS.md` section 9) needs most of the provider logic covered by fast tests, and all database calls stay asynchronous with a token (`AGENTS.md` section 3).

**Class:** Trivial
**Settled by:** `AGENTS.md` section 9 (CRAP of changed methods at most 10; tests execute real package code); `.github/coverage-manifest/Encina.DistributedLock.SqlServer.json` (the unit target of that provider is held down by the missing seam)

</details>

<details>
<summary><strong>8. Error mapping and categories</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Classify once in the adapter: `PostgresException` and `NpgsqlException` become `Left` with the backend-fault code of `LockErrorCodes`; cancellation (including the `OperationCanceledException` Npgsql raises) propagates; `Right(None)` only for a `false` try-lock** | One mapping; contention is never logged as a failure; cancellation is not an error | Needs the exact code names of #2667 (read before editing) |
| **B) Catch `Exception` in the provider** | Smaller | Collapses cancellation and programming errors into `Left` (the defect #2654 describes for SQL Server) |

### Chosen Option: **A — classify in the adapter**

### Rationale

- This plan adds **no new error code**: every `Left` carries the backend-fault code #2667 defines, whose category follows ADR-062 rule R4 (store and provider failures are `Internal`); cancellation is never a `Left` (R5). The error never carries the exception message (`AGENTS.md` section 3); logs carry the exception type through `ForLogging()`.

**Class:** Trivial
**Settled by:** ADR-052; ADR-062 rules R4 and R5; `AGENTS.md` section 3 (`EncinaError.Message` rule)

</details>

<details>
<summary><strong>9. Connection pooling of the lock connections</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dedicated unpooled connections (`Pooling = false` on the lock connection string)** | On a direct connection closing the connection ends the session (behind PgBouncer session pooling only if `server_reset_query` runs `DISCARD ALL` or `pg_advisory_unlock_all()`, see the rationale), so a failed unlock can never leave a lock held on an idle pooled connection; no pool-reset subtleties | A TCP and authentication handshake per acquire; each live handle holds one server connection (`max_connections`) |
| **B) Pooled connections, explicit `pg_advisory_unlock`, and discard the connection when the unlock fails** | No handshake per acquire | Npgsql may defer the session reset of a pooled connection to its next use, so correctness rests on the unlock-failure path being right; discarding a single pooled connection has no direct public API (it needs a pool clear) |

### Chosen Option: **A — unpooled lock connections** (recommended, pending the maintainer)

### Rationale

- Locks are coordination calls, not a hot data path, and mutual exclusion must not depend on a pool detail. Option B wins for a very high acquire rate where the handshake dominates; the maintainer can pick it when work starts, with the integration test "a failed unlock never leaves the lock held" as its acceptance.
- "Closing the connection ends the session" holds only on a direct connection. Behind PgBouncer in session-pooling mode the server connection outlives the client one, so advisory locks are released only if `server_reset_query` (`DISCARD ALL` or `SELECT pg_advisory_unlock_all()`) is configured. The provider README MUST document this requirement and state that transaction-pooling and unconfigured session-pooling setups are unsupported for session mode; the integration test "a failed unlock never leaves the lock held" runs against a direct connection and against a PgBouncer session-pooling container with the reset query, and a second case without it asserts the documented unsupported behavior.
- Because every live handle holds a server connection, the README documents `max_connections` sizing, and the provider exposes no pool setting.

**Class:** Dilemma

</details>

---

## Implementation Phases

Four pull requests, each at most 15 files and 1500 changed lines counting tests, `PublicAPI.*.txt`, the coverage manifest, project references, the README and the changelog fragment (`AGENTS.md` section 11). **No PR builds before [#2667](https://github.com/dlrivada/Encina/issues/2667) merges** for PRs 2 to 4, because they implement the changed interface; PR 1 has no dependency on it.

| PR | Phases | Files (production and metadata) | Files (tests and docs) | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | Phase 1 and the EventId registration of Phase 5 | csproj, `PostgreSQLLockOptions`, `PostgreSQLLockKey`, two `PublicAPI` files, `Encina.slnx`, `EventIdRanges.cs`, `Encina` `PublicAPI.Unshipped.txt`, manifest (9) | `EncinaEventIdAllocationTests`, `Encina.UnitTests.csproj` reference, `PostgreSQLLockOptionsTests`, `PostgreSQLLockKeyTests` (4) | 13 files / 600 |
| 2 | Phases 2 and 5 (logs, diagnostics) | `IAdvisoryLockSession`, `NpgsqlAdvisoryLockSession`, `PostgreSQLDistributedLockProvider`, `PostgreSQLLockDiagnostics`, `PublicAPI.Unshipped.txt`, manifest (6) | `FakeAdvisoryLockSession`, provider unit tests (acquire/contend/expiry), handle unit tests, guard tests, `Encina.GuardTests.csproj` reference (5) | 11 files / 1,300 |
| 3 | Phase 3 | `ServiceCollectionExtensions`, `Health/PostgreSQLDistributedLockHealthCheck`, `PublicAPI.Unshipped.txt`, manifest (4) | DI test with `ValidateOnBuild`/`ValidateScopes`, health check unit tests, guard tests for both (3) | 7 files / 700 |
| 4 | Phases 4, 6 and 7 | none | contract class, the `ADO-PostgreSQL` collection definition in `Encina.ContractTests` (new file) and `Encina.ContractTests.csproj` reference, integration tests and `Encina.IntegrationTests.csproj` reference, property test of the key (`Encina.PropertyTests.csproj` reference), README, changelog fragment, two load/benchmark justification files, manifest (12) | 12 files / 950 |

If a PR measures over 15 files or 1,500 lines, the worker splits it again and never asks for `size-exception-approved`. PR 2 has the least slack (about 1,300 lines): if the provider tests grow, the handle tests move to PR 3.

### Phase 1: Package skeleton, options and key

> **Goal**: The package builds empty of behavior: configuration surface and the key function, with their tests.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.PostgreSQL/`

1. **`Encina.DistributedLock.PostgreSQL.csproj`** (new) — modeled on `Encina.DistributedLock.SqlServer.csproj`: `ProjectReference` to `Encina.DistributedLock`, `PackageReference` `Npgsql` (central version), the PublicApiAnalyzers, `InternalsVisibleTo` as `AssemblyAttribute` items for `Encina.UnitTests`, `Encina.IntegrationTests`, `Encina.ContractTests`, `Encina.PropertyTests` (the pattern of `Encina.Messaging.csproj`; the SQL Server csproj has none); `Encina.GuardTests` is deliberately not listed, so the internal types carry guard target 0 and the guard tests reach only the public constructor and methods; a new GUID
2. **`PostgreSQLLockOptions.cs`** (new, public sealed) — `: DistributedLockOptions`; `[JsonIgnore] string? ConnectionString` (secret rule, `AGENTS.md` section 3); `bool UseTransactionLocks`; `TimeSpan UnlockTimeout = 5 s` (the bound of a timer-driven release); a `ToString()` that prints only the prefix and the mode, never the connection string
3. **`PostgreSQLLockKey.cs`** (new, internal static) — `static string Compose(string prefix, string resource)` (the `lock:` format of the SQL Server provider) and `static long ToAdvisoryKey(string composedKey)` (SHA-256, first 8 bytes big-endian)
4. **`PublicAPI.Shipped.txt`** (new, `#nullable enable`) and **`PublicAPI.Unshipped.txt`** (the options members)
5. **`Encina.slnx`** (modify) — the project line; **`.github/coverage-manifest/Encina.DistributedLock.PostgreSQL.json`** (new, written by `mechanical-fixer` from the target table of Phase 6)
6. **`tests/Encina.UnitTests/Encina.UnitTests.csproj`** (modify) — project reference
7. **`tests/Encina.UnitTests/DistributedLock/PostgreSQL/PostgreSQLLockOptionsTests.cs`** and **`PostgreSQLLockKeyTests.cs`** (new) — defaults, `ToString` hides the connection string, key determinism, prefix handling, different names give different keys for a fixed sample set

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #207: the skeleton of Encina.DistributedLock.PostgreSQL.

CONTEXT:
- New package src/Encina.DistributedLock.PostgreSQL next to src/Encina.DistributedLock.SqlServer; same layout.
- The key is the logical key "{KeyPrefix}:lock:{resource}" (no prefix: "lock:{resource}") hashed with SHA-256; the
  first 8 bytes, big-endian, are the bigint advisory key (design choice 3).
- The options hold a connection string: [JsonIgnore] and a ToString() that never prints it.

TASK:
Create the csproj, PostgreSQLLockOptions, the internal PostgreSQLLockKey, the two PublicAPI files, the slnx line,
the unit test project reference and the unit tests of options and key. Ask mechanical-fixer for the manifest and
the Unshipped lines.

KEY RULES:
- No behavior beyond options and key. Zero warnings (CA and RS rules).
- XML documentation on every public member.
- Time is not read in this phase.

REFERENCE FILES:
- src/Encina.DistributedLock.SqlServer/Encina.DistributedLock.SqlServer.csproj
- src/Encina.DistributedLock.SqlServer/SqlServerLockOptions.cs
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs (GetLockKey)
```

</details>

### Phase 2: Session seam and provider

> **Goal**: The provider on the ADR-052 contract, fully testable without a database.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.PostgreSQL/`

1. **`IAdvisoryLockSession.cs`** (new, internal) — `Task<Either<EncinaError, bool>> TryLockAsync(long key, CancellationToken ct)`, `Task<Either<EncinaError, bool>> HoldsLockAsync(long key, CancellationToken ct)`, `Task<Either<EncinaError, bool>> IsLockedByAnyoneAsync(long key, CancellationToken ct)`, `Task<Either<EncinaError, bool>> UnlockAsync(long key, CancellationToken ct)`, `ValueTask DisposeAsync()`; plus an internal factory `IAdvisoryLockSessionFactory.OpenAsync(bool transactionMode, CancellationToken ct)`
2. **`NpgsqlAdvisoryLockSession.cs`** (new, internal sealed) — wraps one unpooled `NpgsqlConnection` (the factory sets `Pooling = false` on the connection string, design choice 9) and, in transaction mode, one `NpgsqlTransaction`; the four calls above through `NpgsqlCommand` with parameters; `pg_try_advisory_lock(@k)` or `pg_try_advisory_xact_lock(@k)`; `pg_advisory_unlock(@k)` in session mode; in transaction mode `UnlockAsync` ends the private transaction (commit), because a transaction-level advisory lock has no unlock function and is freed only when its transaction ends (a rollback on a faulted release path frees it equally), so it never calls `pg_advisory_unlock` on a lock it did not take at session level; the `pg_locks` queries of design choice 6; classification of `PostgresException` and `NpgsqlException` into `Left` (`LockErrorCodes` backend-fault code of #2667), cancellation rethrown
3. **`PostgreSQLDistributedLockProvider.cs`** (new, public sealed partial) — `IDistributedLockProvider`; constructor `(IOptions<PostgreSQLLockOptions> options, ILogger<PostgreSQLDistributedLockProvider> logger, TimeProvider? timeProvider = null)` plus an internal constructor taking the session factory; `TryAcquireAsync` (poll loop, `Right(Some(handle))` / `Right(None)` / `Left`), `AcquireAsync` (same loop, no deadline), `IsLockedAsync`, `ExtendAsync(resource, extension, ct)` through the registry of live handles; a private `LockHandle : ILockHandle` that arms the expiry timer with `TimeProvider.CreateTimer` (callback catches everything and logs the exception type), releases at the deadline, and runs `ExtendAsync` as a server-verified extension; the handle serializes the timer callback and `ExtendAsync` on its state (one `SemaphoreSlim`), and the callback rechecks the current deadline under that gate before releasing, so an extension that completes first keeps the lock held; the Phase 2 tests include one that fires the expiry (fake `TimeProvider`) while an extension is in flight and asserts the lock stays held and the deadline moved
4. **`PostgreSQLLockDiagnostics.cs`** (new, internal static) — `ActivitySource` and `Meter` named `Encina.DistributedLock.PostgreSQL`; counter `encina.distributed_lock.acquisitions` with tags `provider` (`postgresql`) and `outcome` (`acquired`, `contended`, `failed`); the provider tags no tenant (it is tenant-unaware; the decorator of #741 adds `encina.tenant_id` per [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4)
5. **`[LoggerMessage]` methods** inside the provider (`partial`), EventIds 7550-7557 (Phase 5)
6. **`PublicAPI.Unshipped.txt`** (the provider and its constructor), the manifest (per-file targets, Phase 7)
7. **Tests**: `FakeAdvisoryLockSession` (test double with scripted results and a log of calls), provider unit tests (acquired, contended until the deadline with `FakeTimeProvider`, fault on try-lock, cancellation, registry extend of a held and an unheld resource, expiry release, double dispose, release in transaction mode ends the transaction and frees the lock), handle unit tests, guard tests (constructor and the four methods), `Encina.GuardTests.csproj` project reference

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #207: the PostgreSQL lock provider on the ADR-052 contract.

CONTEXT:
- #2667 has merged: IDistributedLockProvider returns Either<EncinaError, Option<ILockHandle>> (TryAcquireAsync),
  Either<EncinaError, ILockHandle> (AcquireAsync), Either<EncinaError, bool> (IsLockedAsync, ExtendAsync);
  LockErrorCodes exists. Read their real signatures and codes before editing.
- One dedicated session per live handle; poll pg_try_advisory_lock every `retry` until `wait`; enforce the expiry
  with a TimeProvider timer; IsLockedAsync reads pg_locks (design choices 4 to 7).

TASK:
Write IAdvisoryLockSession and its Npgsql adapter, the provider with its private handle, the diagnostics class and
the log methods (EventIds 7550-7557). Write the fake session and the unit and guard tests listed in the Tasks.

KEY RULES:
- Contention is Right(None), never a failure in logs or metrics. A fault is Left with the LockErrorCodes code; the
  error and every log carry no exception message (ForLogging, exception type only).
- OperationCanceledException propagates; never wrap it.
- The timer callback never throws. Database calls are asynchronous with a CancellationToken.
- Time only from TimeProvider. Methods small enough for CRAP <= 10.

REFERENCE FILES:
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs and ILockHandle.cs
- .github/coverage-manifest/Encina.DistributedLock.SqlServer.json
```

</details>

### Phase 3: Configuration, DI and health check

> **Goal**: A registration that is complete on its own, and a health check.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.PostgreSQL/`

1. **`ServiceCollectionExtensions.cs`** (new, public static) — `AddEncinaDistributedLockPostgreSQL(this IServiceCollection, string connectionString)` and the overload with `Action<PostgreSQLLockOptions>`; `TryAddSingleton(TimeProvider.System)`, `Configure<PostgreSQLLockOptions>`, `TryAddSingleton<IAdvisoryLockSessionFactory>`, `TryAddSingleton<IDistributedLockProvider, PostgreSQLDistributedLockProvider>`; registers the health check when `ProviderHealthCheck.Enabled` (the shape of `AddEncinaDistributedLockSqlServer`); options validated at start (non-empty connection string)
2. **`Health/PostgreSQLDistributedLockHealthCheck.cs`** (new, public sealed) — `IEncinaHealthCheck`; `public const string DefaultName = "postgresql-distributed-lock"`; tags `["postgresql", "distributed-lock", "ready"]`; opens a session, takes and releases a random advisory key, returns `Unhealthy` with the exception type only (never the message or the connection string)
3. **`PublicAPI.Unshipped.txt`**, the manifest
4. **Tests**: DI test building the provider with `ValidateOnBuild` and `ValidateScopes` (registration completeness, `AGENTS.md` section 3), a registration-order test (a provider registered earlier by the application is not overridden: `TryAdd`), health check unit tests with the fake session, guard tests of both public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #207: registration and health check of the PostgreSQL lock provider.

CONTEXT:
- Phase 2 produced PostgreSQLDistributedLockProvider and IAdvisoryLockSessionFactory.
- The registration must resolve everything the provider needs: options, TimeProvider, the session factory, logger.

TASK:
Write ServiceCollectionExtensions (two overloads) and PostgreSQLDistributedLockHealthCheck; add the DI test with
ValidateOnBuild and ValidateScopes, the health check tests and the guard tests.

KEY RULES:
- TryAdd* registrations so an application registration wins in any order.
- A missing connection string fails at startup, not at the first lock (fail closed).
- The health check output never contains the connection string or an exception message.

REFERENCE FILES:
- src/Encina.DistributedLock.SqlServer/ServiceCollectionExtensions.cs
- src/Encina.DistributedLock.SqlServer/Health/SqlServerDistributedLockHealthCheck.cs
- tests/Encina.UnitTests/DistributedLock/SqlServer/ServiceCollectionExtensionsTests.cs
```

</details>

### Phase 4: Cross-cutting integration

> **Goal**: The decisions of the 12-function matrix that need code or a statement in tests.

<details>
<summary><strong>Tasks</strong></summary>

1. **Tenancy**: no code in this package. The key the provider receives is already tenant-qualified by the decorator of [#741](https://github.com/dlrivada/Encina/issues/741) (reserved `default` tenant segment, injective encoding); a unit test asserts the provider never reads a tenant and hashes the whole composed name.
2. **Fail closed**: a database fault never reports a held lock: `Left`. On `ExtendAsync`, a database fault from `HoldsLockAsync` is `Left` (backend fault, ADR-062 category), and `Right(false)` only when the check completes and finds no lock. Phase 4 tests assert `Left` on the faulted `HoldsLockAsync` path of `ExtendAsync` and `Right(false)` when the check finds no lock.
3. **Resilience**: no Polly policy; the wait/retry loop is the retry, and a faulted try-lock ends the attempt with `Left` (the caller decides).
4. **PgBouncer note**: README states that session mode needs a session-pooling or direct connection, and transaction mode is the option for transaction pooling.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #207: the cross-cutting statements of the PostgreSQL lock provider.

CONTEXT:
- Tenant scoping lives above the provider (issue #741); the provider hashes the composed name it is given.
- A fault is Left, never "held" and never "free".

TASK:
Add the unit test that proves the provider is tenant-agnostic, the tests that prove a fault is Left on every
public method, and the README paragraph on poolers (through docs-writer).

KEY RULES:
- No new public API in this phase.
- Documentation goes through docs-writer; changelog and PublicAPI through mechanical-fixer.

REFERENCE FILES:
- issue #741 (the tenant decorator; read the issue, its plan is not on main yet)
- src/Encina.DistributedLock.PostgreSQL/PostgreSQLDistributedLockProvider.cs
```

</details>

### Phase 5: Observability — logs, diagnostics and EventIds

> **Goal**: Operational visibility with EventIds registered first (ADR-021).

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina/Diagnostics/EventIdRanges.cs`** (modify, PR 1) — `public static readonly (int Min, int Max) DistributedLockPostgreSQL = (7550, 7559);` with the XML summary `Encina.DistributedLock.PostgreSQL — PostgreSQL advisory lock provider`; the new field line in `src/Encina/PublicAPI.Unshipped.txt`
2. **`tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`** (modify, PR 1) — `["Encina.DistributedLock.PostgreSQL"] = [nameof(EventIdRanges.DistributedLockPostgreSQL)]`
3. **Log messages** (PR 2) — 7550 lock acquired (Debug), 7551 lock released (Debug), 7552 wait elapsed, lock contended (Debug), 7553 acquire fault (Warning, exception type), 7554 release failed, connection closed to free the lock (Warning), 7555 expiry reached, lock released by the timer (Information), 7556 extend found the lock lost (Warning), 7557 timer release failed (Warning). 7558-7559 stay spare. Contention is never an error level.
4. **Metrics and spans** (PR 2) — `PostgreSQLLockDiagnostics`: spans `DistributedLock.Acquire` and `DistributedLock.Release`, counter `encina.distributed_lock.acquisitions`; names align with the shared instruments of [#724](https://github.com/dlrivada/Encina/issues/724) and move onto them if it lands first.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #207: EventIds, logs and instruments of the PostgreSQL lock provider.

CONTEXT:
- ADR-021: register the range in src/Encina/Diagnostics/EventIdRanges.cs before using an id; map the assembly in
  EncinaEventIdAllocationTests. The assigned block is 7550-7599 of the plan batch; this package uses 7550-7557.
- Contention is not a failure: Debug level and outcome "contended".

TASK:
Register DistributedLockPostgreSQL = (7550, 7559), map the assembly, add the [LoggerMessage] methods with the ids
in the Tasks, and the diagnostics class. Run the architecture tests.

KEY RULES:
- Packed ids, no gaps. Log the exception through ForLogging(), never its message.
- No tenant tag in the provider (ADR-054 D4 is applied by the #741 decorator).

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs (log methods)
```

</details>

### Phase 6: Testing

> **Goal**: Every flag reaches its own target; the same contract scenarios as the other providers.

<details>
<summary><strong>Tasks</strong></summary>

Per-file targets (the manifest lists them with these sentences; the numbers are the demanding, realistic start and the worker raises them to what it measures):

| File | unit | guard | contract | integration | property | Justification |
|---|---:|---:|---:|---:|---:|---|
| `PostgreSQLDistributedLockProvider.cs` | 90 | 14 | 45 | 70 | 0 | The poll loop, registry, timer, handle and error mapping run against the fake session; the contract suite adds the shared scenarios; integration adds the real server; property 0: no randomized logic beyond the key |
| `NpgsqlAdvisoryLockSession.cs` | 5 | 5 | 0 | 90 | 0 | Needs a real PostgreSQL; unit and guard reach only the guards; contract 0 because the contract class uses the provider on the shared container (integration flag) |
| `PostgreSQLLockKey.cs` | 100 | 0 | 0 | 0 | 95 | Pure function; guard 0: internal type without `InternalsVisibleTo` for `Encina.GuardTests`; FsCheck covers determinism and spread |
| `PostgreSQLLockOptions.cs` | 100 | 0 | 0 | 0 | 0 | Property bag; guard 0: no parameters |
| `ServiceCollectionExtensions.cs` | 95 | 90 | 0 | 0 | 0 | DI with a validated provider; guard covers each overload |
| `Health/PostgreSQLDistributedLockHealthCheck.cs` | 85 | 85 | 0 | 75 | 0 | Fake session for unit; the integration test runs it against the container |
| `PostgreSQLLockDiagnostics.cs` | 80 | 0 | 0 | 0 | 0 | Instruments exercised through the provider tests; guard 0: no parameters |

1. **Unit** — the scenarios of Phases 1 to 3, using `FakeTimeProvider` and the fake session.
2. **Guard** — constructor (null options, null logger, options without a connection string) and each method (null or whitespace resource, non-positive expiry as #2724 decides).
3. **Contract** — `PostgreSQLDistributedLockContractTests`, running the same scenarios as InMemory, Redis and SQL Server (free, contended, expiry, extend held/unheld, double dispose, cancellation); `Encina.ContractTests.csproj` reference. It needs the shared container, and `Encina.ContractTests` defines no `ADO-PostgreSQL` collection, so PR 4 adds `tests/Encina.ContractTests/DistributedLock/PostgreSQL/PostgreSqlCollection.cs` (`[CollectionDefinition("ADO-PostgreSQL")]` with `ICollectionFixture<PostgreSqlFixture>`, the shape of `LocalStackCollection.cs` in that project). #2667 does not state a reusable abstract base class: PR 4 reads the suite as #2667 merged it and either derives from its base or, when none exists, extracts one (the extraction is counted in the 12 files only if it adds a file; PR 4 then splits).
4. **Property** — FsCheck on `PostgreSQLLockKey`: determinism, prefix separation, no exception for any string.
5. **Integration** — `[Collection("ADO-PostgreSQL")]` on `PostgreSqlFixture`, `[Trait("Category", "Integration")]`, `[Trait("Database", "PostgreSQL")]`: two providers on one database block each other; release frees the lock; killing the session (`pg_terminate_backend`) frees it; the expiry timer frees it; transaction mode (including "release in transaction mode frees the lock" for another session); `IsLockedAsync` does not disturb a concurrent contender; cancellation inside a wait; health check on a real server.
6. **Load and benchmark** — `.md` justification files under `tests/Encina.NBomber/Scenarios/Locking/` and `tests/Encina.BenchmarkTests/Encina.DistributedLock.Benchmarks/`: the provider is a thin client over a database primitive and is covered by the shared lock scenarios once its factory is added by the issue that owns those scenarios; a missing `.md` would mean "not evaluated" (`AGENTS.md` section 9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #207: the tests of the PostgreSQL lock provider.

CONTEXT:
- Unit and guard tests were written with each phase; this phase adds contract, property, integration and the
  justification files, and raises the manifest targets to the measured values.
- Integration tests use the shared collection ADO-PostgreSQL (PostgreSqlFixture); never a per-class container.

TASK:
Write the contract class, the FsCheck key property, the integration tests of the Tasks, the two justification
files, and measure every flag from the repository root with `dotnet test tests\Encina.<Flag>Tests --collect
"XPlat Code Coverage"` and `coverage-report.cs`.

KEY RULES:
- Tests execute real package code; assertions with Shouldly through Encina.Testing.Shouldly.
- No Thread.Sleep: FakeTimeProvider for unit tests; polling with timeouts for integration.
- The PR reports measured per-flag coverage against the manifest targets.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/SqlServerDistributedLockIntegrationTests.cs
- tests/Encina.IntegrationTests/ADO/Collections.cs
- tests/Encina.TestInfrastructure/Fixtures/PostgreSqlFixture.cs
```

</details>

### Phase 7: Documentation and Finalization

> **Goal**: The package is documented and the 1.0 inventory is true.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation** on every public member (summary, remarks, param, returns, one example on the registration).
2. **`src/Encina.DistributedLock.PostgreSQL/README.md`** (new, `docs-writer`) — what the provider does, session versus transaction mode, pooler notes, the key derivation, expiry behavior, a `Match` example with `await using`.
3. **`changelog.d/207-lock-postgresql.added.md`** (`mechanical-fixer`).
4. **`AGENTS.md` section 5 and `docs/INVENTORY.md`**: no edit needed for the lock table (it already lists PostgreSQL, #207); `docs/INVENTORY.md` gets the package row when the file lists packages (`docs-writer` checks).
5. **ADR**: none; the decisions are the lock family's ADR-052 and this plan's choices.
6. **Build and test verification**: `dotnet build --configuration Release` with zero warnings; every flag at its manifest target; `pwsh tools/ai/plans/check-plan.ps1` for this file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are finishing issue #207: documentation and verification of Encina.DistributedLock.PostgreSQL.

CONTEXT:
- Phases 1 to 6 are merged; the package, tests and manifest exist.

TASK:
Complete the XML documentation, ask docs-writer for the package README, ask mechanical-fixer for the changelog
fragment and the PublicAPI lines, and run the full verification.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers, SPEC-001).
- Public text never names a private client application.

REFERENCE FILES:
- changelog.d/README.md
- src/Encina.DistributedLock.SqlServer/ (README pattern of sibling lock packages, when present)
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| PostgreSQL documentation, "Explicit Locking: Advisory Locks" | `pg_try_advisory_lock(bigint)`, `pg_advisory_unlock`, `pg_try_advisory_xact_lock`; session locks survive transaction end, are held until unlock or session end, and are not subject to rollback | Defines the two modes and the release behavior |
| PostgreSQL documentation, `DISCARD ALL`; Npgsql documentation on pooling and connection reset | `DISCARD ALL` includes `pg_advisory_unlock_all()`; Npgsql may defer the reset of a pooled connection to its next use | Why the lock connections are unpooled (design choice 9) |
| PostgreSQL documentation, `pg_locks` | Advisory locks appear with `locktype = 'advisory'`, `classid`/`objid` holding the two halves of the `bigint` key and `objsubid = 1` | The non-intrusive probe |
| ADR-052 | Lock providers return `Either`; contention is `Right(None)`; a fault is `Left` | Outcome mapping of every method |
| ADR-062 | Error categories; R4 provider failures are `Internal`; R5 cancellation | Categories of the `Left` cases |
| SPEC-000 REQ-027, DEC-003, DEC-009, AC-027 | Seven lock providers for 1.0, contract tests over them | This provider joins the contract suite |
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Secrets, time, async database calls, errors, EventIds, coverage | Constraints used throughout |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IDistributedLockProvider`, `ILockHandle`, `DistributedLockOptions` | `src/Encina.DistributedLock/` | The contract (after #2667) and the base options (`KeyPrefix`, `DefaultExpiry`, `DefaultWait`, `DefaultRetry`, `ProviderHealthCheck`) |
| `SqlServerDistributedLockProvider` and its registration | `src/Encina.DistributedLock.SqlServer/` | Structural model: connection per handle, `GetLockKey`, log methods, health registration |
| `SqlServerDistributedLockHealthCheck` | `src/Encina.DistributedLock.SqlServer/Health/` | Model of the health check |
| `IEncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check contract and options |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | Registration of 7550-7559 and the assembly map |
| `PostgreSqlFixture` and `ADO-PostgreSQL` collection | `tests/Encina.TestInfrastructure/Fixtures/`, `tests/Encina.IntegrationTests/ADO/Collections.cs` | Integration tests on the shared container |
| `Npgsql` 10.0.2 | `Directory.Packages.props` | The driver |
| `ForLogging()` | `src/Encina/Diagnostics/` | Exception logging without the message |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | This plan | Notes |
|----------|:-:|-------|
| InMemory | N/A | Single-process testing provider; changed by #2667 |
| Redis | N/A | Quorum mode is [#226](https://github.com/dlrivada/Encina/issues/226) |
| SqlServer | N/A | Model for this plan; its defects are #2405, #2720, #2722 |
| PostgreSQL (#207) | Included | The subject of this plan |
| MySQL (#208) | N/A | Same family, own plan; same key rule and seam |
| Azure Blob (#209) | N/A | Own plan; lease based |
| DynamoDB (#210) | N/A | Own plan; conditional write |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DistributedLock.PostgreSQL` (new range `DistributedLockPostgreSQL`) | 7550-7559 | From the block 7550-7599 assigned to this family (`lock-providers-1`); 7550-7557 used, 7558-7559 spare. The other provider plans of the family take 7560-7569 (MySQL), 7570-7579 (Azure Blob) and 7580-7589 (DynamoDB); 7590-7599 stay spare |
| `Encina.DistributedLock.SqlServer` (existing, for reference) | 3800-3849 | Not touched |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 9 | csproj, options, key, session seam, Npgsql adapter, provider, diagnostics, health check, registration |
| Metadata (new and modified) | 7 | two `PublicAPI` files, `Encina.slnx`, `EventIdRanges.cs`, `Encina` `PublicAPI.Unshipped.txt`, manifest (`Directory.Packages.props` is not touched: `Npgsql` is already there) |
| Unit tests | 6 | options, key, provider, handle, DI, health check (plus the fake session) |
| Guard tests | 2 | provider, registration and health check |
| Contract and property tests | 3 | contract class, `ADO-PostgreSQL` collection definition for `Encina.ContractTests`, key property |
| Integration tests | 1 | one class on the shared collection |
| Test project references | 5 | UnitTests, GuardTests, ContractTests, IntegrationTests, PropertyTests |
| Documentation and changelog | 4 | README, fragment, two justification files |
| **Total** | **~37 touches** | Over 4 pull requests of at most 15 files each (13, 11, 7 and 12) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #207: Encina.DistributedLock.PostgreSQL, a lock provider on PostgreSQL advisory locks.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- Lock contract: ADR-052, implemented by #2667 before this work: TryAcquireAsync returns
  Either<EncinaError, Option<ILockHandle>>, AcquireAsync returns Either<EncinaError, ILockHandle>, IsLockedAsync and
  ExtendAsync return Either<EncinaError, bool>, cancellation propagates, LockErrorCodes holds the Left codes.
- Time only from TimeProvider. EncinaError.Message never reaches logs, tags or health checks.

IMPLEMENTATION OVERVIEW:
1. Package skeleton: csproj, PostgreSQLLockOptions ([JsonIgnore] connection string, UseTransactionLocks),
   PostgreSQLLockKey (SHA-256 to bigint of "{prefix}:lock:{resource}").
2. IAdvisoryLockSession seam and NpgsqlAdvisoryLockSession adapter; PostgreSQLDistributedLockProvider: poll the
   try-lock every retry until wait, one session per handle, expiry enforced by a TimeProvider timer that never
   throws, provider ExtendAsync through a registry of live handles, IsLockedAsync through pg_locks.
3. AddEncinaDistributedLockPostgreSQL (two overloads, TryAdd*), health check, DI test with ValidateOnBuild.
4. EventIds 7550-7557 in range DistributedLockPostgreSQL (7550-7559), diagnostics, ActivitySource and Meter.
5. Tests: unit (fake session, FakeTimeProvider), guard, contract (suite of #2667), property (key), integration on
   the ADO-PostgreSQL collection; documentation through docs-writer; changelog and PublicAPI through mechanical-fixer.

KEY PATTERNS:
- Contention is Right(None); a database fault is Left (also from the HoldsLockAsync check of ExtendAsync); a lost lock is Right(false) on ExtendAsync only when the check completes.
- Satellite DI with TryAdd*; startup validation of the connection string.
- Four pull requests of at most 15 files; each builds and passes alone.

REFERENCE FILES:
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs
- src/Encina.DistributedLock.SqlServer/ServiceCollectionExtensions.cs
- .github/coverage-manifest/Encina.DistributedLock.SqlServer.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Lock state is ephemeral coordination state; caching it would be a defect |
| 2 | OpenTelemetry | ✅ | Phase 5: spans for acquire and release, acquisitions counter with `provider` and `outcome` tags; no tenant tag here, the #741 decorator adds `encina.tenant_id` (ADR-054 D4); names align with [#724](https://github.com/dlrivada/Encina/issues/724) |
| 3 | Structured Logging | ✅ | Phase 5: `[LoggerMessage]` EventIds 7550-7557 in the registered range, exception type only |
| 4 | Health Checks | ✅ | Phase 3: `PostgreSQLDistributedLockHealthCheck` takes and releases a random advisory key |
| 5 | Validation | ✅ | Phases 1 and 3: connection string validated at startup; resource and expiry arguments validated as [#2724](https://github.com/dlrivada/Encina/issues/2724) decides |
| 6 | Resilience | ❌ | The provider's own wait/retry loop is the retry; a fault ends the attempt with `Left` and the caller decides; a Polly policy would double the retries |
| 7 | Distributed Locks | ❌ | This is the distributed lock feature itself |
| 8 | Transactions | ❌ | A lock is not a transactional resource; the opt-in transaction mode only ties the lock lifetime to a private transaction, not to the caller's unit of work |
| 9 | Idempotency | ❌ | A repeated acquire is a new attempt by design |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped keys are composed above the provider by [#741](https://github.com/dlrivada/Encina/issues/741) (existing issue); the provider hashes the final name |
| 11 | Module Isolation | ❌ | Isolation is reached with `KeyPrefix`; the provider holds no module data |
| 12 | Audit Trail | ❌ | Acquire and release are coordination events, not data access or state changes; logs and metrics cover them |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#2667](https://github.com/dlrivada/Encina/issues/2667) — lock providers return `Either` (ADR-052) | Open, implementation issue (no pending maintainer decision) | PR 1 can start before it; PRs 2 to 4 start after it merges |
| [#2724](https://github.com/dlrivada/Encina/issues/2724) — argument validation of lock providers | Open | The guard tests follow its decision for empty resources and non-positive expiry |
| [#741](https://github.com/dlrivada/Encina/issues/741) — tenant-scoped locks | Open (family plan) | Owns tenant key composition; provider unchanged |
| [#755](https://github.com/dlrivada/Encina/issues/755) — health checks of the other lock providers | Open | Adds the first lock reference to the contract test project; whichever lands first, the other drops its line |
| [#724](https://github.com/dlrivada/Encina/issues/724) — lock observability | Open | Phase 5 reuses shared instruments if they exist |
| [#218](https://github.com/dlrivada/Encina/issues/218), [#223](https://github.com/dlrivada/Encina/issues/223) — fencing tokens, auto-extend | Open | Out of scope; documented as limits |

---

## Next Steps

1. Link this plan from issue #207 and start the PR 1 brief; PRs 2 to 4 wait for [#2667](https://github.com/dlrivada/Encina/issues/2667).
2. When work on the issue starts, ask the maintainer to answer Dilemmas 2 (transaction mode), 4 (poll or block), 5 (enforced expiry) and 9 (unpooled connections), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices.
3. Follow-up issue file written with this plan: `artifacts/issues/lock-reader-writer-abstraction.md` (shared locks need a reader/writer lock contract above `IDistributedLockProvider`; the PostgreSQL `pg_advisory_lock_shared` and the other backends are candidates).
4. One commit per phase; the last commit of PR 4 references `Fixes #207`.
