# Implementation Plan: `Encina.DistributedLock.MySQL` — distributed lock provider on MySQL user-level locks (`GET_LOCK`)

> **Issue**: [#208](https://github.com/dlrivada/Encina/issues/208)
> **Type**: Feature
> **Complexity**: Medium (one new provider package, 4 pull requests)
> **Estimated Scope**: ~600-750 lines of production code + ~1,100-1,400 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-003, DEC-009, AC-027
> **Decision**: [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (lock failure contract), [ADR-062](../architecture/adr/062-error-category-contract.md) (error categories)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision; it blocks this issue)
> **Related**: [#207](https://github.com/dlrivada/Encina/issues/207) (PostgreSQL provider, same family and same shape), [#209](https://github.com/dlrivada/Encina/issues/209), [#210](https://github.com/dlrivada/Encina/issues/210), [#226](https://github.com/dlrivada/Encina/issues/226) (Redis RedLock), [#741](https://github.com/dlrivada/Encina/issues/741) (tenant-scoped locks), [#755](https://github.com/dlrivada/Encina/issues/755) (health checks), [#724](https://github.com/dlrivada/Encina/issues/724) (lock observability), [#2405](https://github.com/dlrivada/Encina/issues/2405), [#2720](https://github.com/dlrivada/Encina/issues/2720), [#2722](https://github.com/dlrivada/Encina/issues/2722), [#2724](https://github.com/dlrivada/Encina/issues/2724) (defects of the SQL Server provider this plan must not repeat)

---

## Summary

Add the package `Encina.DistributedLock.MySQL`, an `IDistributedLockProvider` over MySQL **user-level locks** (`GET_LOCK`, `RELEASE_LOCK`, `IS_USED_LOCK`). It is the MySQL entry of the seven lock providers SPEC-000 DEC-009 fixes for 1.0.

1. **One dedicated, unpooled connection per live handle.** A user-level lock belongs to the MySQL session; closing an unpooled connection always ends the session and the server frees the lock, even when `RELEASE_LOCK` fails (design choice 5).
2. **Poll, do not block.** Acquire calls `GET_LOCK(name, 0)` every `retry` until the `wait` deadline, so `retry` keeps its meaning, delays use `TimeProvider` and cancellation needs no server-side kill.
3. **Hashed lock names.** `GET_LOCK` names are limited to 64 characters; the lock name is the lower-case hex SHA-256 (exactly 64 characters) of the logical key `{KeyPrefix}:lock:{resource}` (design choice 2).
4. **Expiry is enforced.** A user-level lock has no TTL, so the handle arms a `TimeProvider` timer that releases the lock at `ExpiresAtUtc`; `ExtendAsync` verifies on the server (`IS_USED_LOCK(name) = CONNECTION_ID()`) that the session still holds the lock and moves the deadline.
5. **ADR-052 outcomes.** Acquired is `Right(Some(handle))`, contention or the wait elapsing is `Right(None)` (`GET_LOCK` returns 0), a database fault is `Left` (a `NULL` result or a driver exception), cancellation propagates.
6. **Non-intrusive probe.** `IsLockedAsync` runs `IS_USED_LOCK(name)`; it never takes the lock to test it (the defect of #2722).
7. **Operations.** A health check, `[LoggerMessage]` EventIds 7560-7567, an `ActivitySource` and a `Meter`.

**Standards**: MySQL reference manual, "Locking Functions" (`GET_LOCK`, `RELEASE_LOCK`, `IS_USED_LOCK`, `IS_FREE_LOCK`); ADR-052; SPEC-000 REQ-027 / AC-027.

**Estimated scope**: 9 new production files (csproj, options, name function, session seam and its MySqlConnector adapter, provider, diagnostics, health check, registration), 4 modified metadata files, about 14 test files, README and changelog fragment; 4 pull requests.

**Affected packages**: `Encina.DistributedLock.MySQL` (new); `Encina` (EventId registry and its `PublicAPI.Unshipped.txt`); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`, `Encina.PropertyTests` (project references and tests). `Encina.DistributedLock` is consumed, not changed.

**Provider category**: Distributed locks (`AGENTS.md` section 5); matrix in Research.

**What the code does today** (`origin/main` at `db1e7f688`):

- The package does not exist. `MySqlConnector` 2.6.1 is in `Directory.Packages.props`; `MySqlFixture` (`tests/Encina.TestInfrastructure/Fixtures/MySqlFixture.cs`, image `mysql:9.1`) and the collection `ADO-MySQL` (`tests/Encina.IntegrationTests/ADO/Collections.cs`) exist.
- `IDistributedLockProvider` still has the pre-ADR-052 shapes; [#2667](https://github.com/dlrivada/Encina/issues/2667) changes them and creates `LockErrorCodes`. **This plan is written to the ADR-052 contract; implementation starts after #2667 merges.**
- `SqlServerDistributedLockProvider` is the structural model (connection per handle, `GetLockKey`, health registration, EventIds 3800-3804). The plan of [#207](https://github.com/dlrivada/Encina/issues/207) designs the same provider for PostgreSQL; the two share the seam shape, the expiry timer and the registry, but no code (each package is separate and no shared helper is added to `Encina.DistributedLock`).
- No contract test class for locks exists yet; #2667 creates the suite.

---

## Design Choices

<details>
<summary><strong>1. Package placement and name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.DistributedLock.MySQL`** | The name SPEC-000 REQ-027 uses; same shape as the other provider packages; MySqlConnector only for those who ask | One more package, README and manifest |
| **B) A lock folder inside `Encina.ADO.MySQL`** | Fewer packages | Mixes a coordination feature into a store package; breaks the `Encina.DistributedLock.{Backend}` naming |

### Chosen Option: **A — new package `Encina.DistributedLock.MySQL`**

### Rationale

- SPEC-000 REQ-027 names the package, and the three existing providers are separate packages with a `DistributedLock` prefix.

**Class:** Settled
**Settled by:** SPEC-000 REQ-027; `AGENTS.md` section 5 (the lock table names `MySQL (GET_LOCK, #208)`)

</details>

<details>
<summary><strong>2. Lock name — always the SHA-256 hex, or the raw name with a rejection above 64 characters</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Always the lower-case hex SHA-256 of `{KeyPrefix}:lock:{resource}` (64 characters)** | Fits the 64-character limit for every input; one rule; no case or collation question (the name is lower-case hex); a tenant-qualified name ([#741](https://github.com/dlrivada/Encina/issues/741)) of any length works | Names in `performance_schema` are opaque; two names collide with probability about 2^-256 |
| **B) The raw logical key; a name over 64 characters is rejected with an error** | Readable lock names for operators | A tenant-qualified name easily exceeds 64 characters; collation rules of the session may make two raw names equal; a new error code and a user-visible limit |

### Chosen Option: **A — always the SHA-256 hex** (recommended, pending the maintainer)

### Rationale

- Option A makes the limit invisible to callers, and operators correlate a lock with its resource through the structured log (`EventId 7560` carries the resource).
- Option B wins when operators routinely inspect `performance_schema.metadata_locks` and the application only uses short names; the maintainer can pick it when work starts, in which case the plan adds the code `lock.resource_name_too_long` with `ErrorCategory.Validation` (ADR-062 rule R3, a field rule).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Acquire strategy — poll the try-lock, or let the server wait</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Poll `GET_LOCK(name, 0)` every `retry` until the `wait` deadline** | Matches the interface (`wait`, `retry`); delays use `TimeProvider` and `FakeTimeProvider`; cancellation is a token check between polls | Up to one `retry` of latency after the holder releases; no fairness between waiters |
| **B) `GET_LOCK(name, timeoutSeconds)` with the wait on the server** | No polling latency | The timeout argument is expressed in seconds, so a sub-second `wait` needs a rounding rule; cancelling a blocked call needs a second connection and `KILL QUERY`; `retry` is meaningless |

### Chosen Option: **A — poll the try-lock** (recommended, pending the maintainer)

### Rationale

- The `retry` parameter exists for a polling provider, every other provider polls, and one deterministic loop is what the contract tests exercise. `AcquireAsync` is the same loop without the deadline, using `DefaultRetry`.
- Option B wins for a hot lock with many waiters where the polling latency matters more than testability.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Expiry semantics — enforce the expiry, or leave it informational</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Informational: the lock lives until disposal or connection loss** | No timer; what the SQL Server provider does today | The interface promises auto-release after `expiry`; a hung holder keeps the lock while its process lives |
| **B) Enforced: a `TimeProvider` timer releases the lock and closes the session at `ExpiresAtUtc`; `ExtendAsync` moves the deadline** | The same observable contract as Redis (`PX`) and the cloud providers; a hung holder cannot starve others | A timer callback per live handle (it must never throw, `AGENTS.md` section 3); a holder past its expiry loses the lock without notice until it extends or disposes |

### Chosen Option: **B — enforced expiry** (recommended, pending the maintainer)

### Rationale

- `IDistributedLockProvider.TryAcquireAsync` documents that the lock is released after the expiry time even if not disposed, and the contract tests (AC-027) run the same expiry scenario on all seven providers.
- Option A wins when the work under the lock legitimately outlasts any bound; the maintainer can pick it when work starts.
- The timer callback catches every exception and logs only the exception type (`AGENTS.md` section 3); the timer release runs `RELEASE_LOCK` with a short timeout and then closes the connection, so a failed release still frees the lock.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Connection pooling of the lock connections</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dedicated unpooled connections (`Pooling = false` on the lock connection string)** | Closing the connection always ends the session, so a failed `RELEASE_LOCK` cannot leave a lock held on an idle pooled connection | A handshake per acquire; each live handle holds one server connection (`max_connections`) |
| **B) Pooled connections, explicit `RELEASE_LOCK`, discard the connection on failure** | No handshake per acquire | MySqlConnector resets a pooled connection when it is next used, not when it is returned, so a lock can survive on an idle connection until then; discarding one pooled connection has no direct public API |

### Chosen Option: **A — unpooled lock connections** (recommended, pending the maintainer)

### Rationale

- Locks are coordination calls, not a hot data path, and mutual exclusion must not depend on a pool detail. Option B wins for a very high acquire rate; the integration test "a failed release never leaves the lock held" is its acceptance.
- The README documents `max_connections` sizing, and the provider exposes no pool setting.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. MariaDB — documented compatibility only, or a tested target</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) MySQL 8 and later is the supported and tested target; the README states that MariaDB uses the same functions but is not in the CI matrix** | No new test dependency; the claim is exactly what the tests prove | The issue's "compatible with MySQL and MariaDB" task is not backed by a test |
| **B) Add a MariaDB container (`Testcontainers.MariaDb`, a fixture and a collection) and run the integration tests on both** | The compatibility claim is tested | A new package in `Directory.Packages.props`, a fixture, a second container in CI for one provider; AGENTS.md section 5 lists no MariaDB provider |

### Chosen Option: **A — MySQL tested, MariaDB documented** (recommended, pending the maintainer)

### Rationale

- The database matrix of `AGENTS.md` section 5 names MySQL only, and a compatibility claim without a test is the kind of statement the project avoids. Option B wins if a user of the reference application runs MariaDB in production.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Probe, provider-level extend and the testability seam</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IsLockedAsync` runs `IS_USED_LOCK(name)` (NULL means free); provider `ExtendAsync(resource)` extends only a handle this provider instance holds, else `Right(false)`; an `internal` seam `IUserLockSession` with one MySqlConnector adapter holds all SQL** | No lock is taken to test one; ownership is real; the retry loop, timer, registry and error mapping run in unit tests against a fake | One more internal file |
| **B) Probe by trying the lock and releasing it; `ExtendAsync` answers `Right(true)`; connection created inline** | Fewer files | The probe causes false contention and can hold the lock if the release fails (#2722); `Right(true)` for an unchecked lock is #2405; MySqlConnector types are not mockable, so the logic is reachable only through Docker |

### Chosen Option: **A — `IS_USED_LOCK` probe, local registry, session seam**

### Rationale

- ADR-052 defines `ExtendAsync` as `Right(false)` when the lock is "not held"; `ILockHandle.ExtendAsync` additionally runs `SELECT IS_USED_LOCK(@name) = CONNECTION_ID()` to detect a session that lost the lock. The blocking `crap-gate` needs the logic covered by fast tests (`AGENTS.md` section 9).

**Class:** Settled
**Settled by:** #2722 and #2405 (the defects option B reproduces); ADR-052 ("not held" is `Right(false)`); `AGENTS.md` section 9

</details>

<details>
<summary><strong>8. Error mapping and categories</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Classify once in the adapter: `GET_LOCK` returning `NULL`, a `MySqlException` or a connection failure become `Left` with the backend-fault code of `LockErrorCodes`; `GET_LOCK` returning 0 is `Right(None)`; cancellation propagates** | One mapping; contention is never logged as a failure; cancellation is not an error | Needs the exact code names of #2667 (read before editing) |
| **B) Catch `Exception` in the provider** | Smaller | Collapses cancellation and programming errors into `Left` (the defect of #2654) |

### Chosen Option: **A — classify in the adapter**

### Rationale

- This plan adds **no new error code** unless the maintainer picks option B of choice 2: every `Left` carries the backend-fault code #2667 defines, whose category follows ADR-062 rule R4 (provider failures are `Internal`); cancellation is never a `Left` (R5). The error never carries the exception message; logs carry the exception type through `ForLogging()`.

**Class:** Trivial
**Settled by:** ADR-052; ADR-062 rules R4 and R5; `AGENTS.md` section 3 (`EncinaError.Message` rule)

</details>

---

## Implementation Phases

Four pull requests, each at most 15 files and 1500 changed lines counting tests, `PublicAPI.*.txt`, the coverage manifest, project references, the README and the changelog fragment (`AGENTS.md` section 11). **PRs 2 to 4 build only after [#2667](https://github.com/dlrivada/Encina/issues/2667) merges**; PR 1 does not depend on it.

| PR | Phases | Production and metadata files | Test and doc files | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | Phase 1 and the EventId registration of Phase 5 | csproj, `MySQLLockOptions`, `MySQLLockName`, two `PublicAPI` files, `Encina.slnx`, `EventIdRanges.cs`, `Encina` `PublicAPI.Unshipped.txt`, manifest (9) | `EncinaEventIdAllocationTests`, `Encina.UnitTests.csproj` reference, `MySQLLockOptionsTests`, `MySQLLockNameTests` (4) | 13 files / 600 |
| 2 | Phases 2 and 5 (logs, diagnostics) | `IUserLockSession`, `MySqlConnectorUserLockSession`, `MySQLDistributedLockProvider`, `MySQLLockDiagnostics`, `PublicAPI.Unshipped.txt`, manifest (6) | `FakeUserLockSession`, provider unit tests, handle unit tests, guard tests, `Encina.GuardTests.csproj` reference (5) | 11 files / 1,300 |
| 3 | Phase 3 | `ServiceCollectionExtensions`, `Health/MySQLDistributedLockHealthCheck`, `PublicAPI.Unshipped.txt`, manifest (4) | DI test with `ValidateOnBuild`/`ValidateScopes`, health check unit tests, guard tests (3) | 7 files / 700 |
| 4 | Phases 4, 6 and 7 | none | contract class and `Encina.ContractTests.csproj` reference, integration tests and `Encina.IntegrationTests.csproj` reference, property test of the name function and `Encina.PropertyTests.csproj` reference, README, changelog fragment, two load/benchmark justification files, manifest (11) | 11 files / 900 |

If a PR measures over 15 files or 1,500 lines, the worker splits it again and never asks for `size-exception-approved`. PR 2 has the least slack: if the provider tests grow, the handle tests move to PR 3.

### Phase 1: Package skeleton, options and lock name

> **Goal**: The package builds with its configuration surface and the name function, with their tests.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.MySQL/`

1. **`Encina.DistributedLock.MySQL.csproj`** (new) — modeled on `Encina.DistributedLock.SqlServer.csproj`: `ProjectReference` to `Encina.DistributedLock`, `PackageReference` `MySqlConnector` (central version), PublicApiAnalyzers, `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.IntegrationTests`, `Encina.ContractTests`, `Encina.PropertyTests`; a new GUID
2. **`MySQLLockOptions.cs`** (new, public sealed) — `: DistributedLockOptions`; `[JsonIgnore] string? ConnectionString` (secret rule, `AGENTS.md` section 3); `TimeSpan ReleaseTimeout = 5 s` (the bound of a timer-driven release); `ToString()` prints only the prefix and the defaults
3. **`MySQLLockName.cs`** (new, internal static) — `static string Compose(string prefix, string resource)` (the `lock:` format of the SQL Server provider) and `static string ToLockName(string composedKey)` (lower-case hex SHA-256, 64 characters)
4. **`PublicAPI.Shipped.txt`** (new, `#nullable enable`) and **`PublicAPI.Unshipped.txt`**
5. **`Encina.slnx`** (modify) and **`.github/coverage-manifest/Encina.DistributedLock.MySQL.json`** (new, written by `mechanical-fixer` from the target table of Phase 6)
6. **`tests/Encina.UnitTests/Encina.UnitTests.csproj`** (modify) — project reference
7. **`tests/Encina.UnitTests/DistributedLock/MySQL/MySQLLockOptionsTests.cs`** and **`MySQLLockNameTests.cs`** (new) — defaults, `ToString` hides the connection string, name is 64 characters of lower-case hex, deterministic, prefix changes the name

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #208: the skeleton of Encina.DistributedLock.MySQL.

CONTEXT:
- New package src/Encina.DistributedLock.MySQL next to src/Encina.DistributedLock.SqlServer; same layout.
- The lock name is the lower-case hex SHA-256 (64 characters, the GET_LOCK limit) of "{KeyPrefix}:lock:{resource}"
  (no prefix: "lock:{resource}"), design choice 2.
- The options hold a connection string: [JsonIgnore] and a ToString() that never prints it.

TASK:
Create the csproj, MySQLLockOptions, the internal MySQLLockName, the two PublicAPI files, the slnx line, the unit
test project reference and the unit tests of options and name. Ask mechanical-fixer for the manifest and the
Unshipped lines.

KEY RULES:
- No behavior beyond options and name. Zero warnings (CA and RS rules).
- XML documentation on every public member.

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

#### `src/Encina.DistributedLock.MySQL/`

1. **`IUserLockSession.cs`** (new, internal) — `Task<Either<EncinaError, bool>> TryLockAsync(string name, CancellationToken ct)`, `HoldsLockAsync(string name, ct)`, `IsLockedByAnyoneAsync(string name, ct)`, `ReleaseAsync(string name, ct)`, `ValueTask DisposeAsync()`; plus an internal `IUserLockSessionFactory.OpenAsync(CancellationToken ct)`
2. **`MySqlConnectorUserLockSession.cs`** (new, internal sealed) — wraps one unpooled `MySqlConnection` (the factory sets `Pooling = false` through `MySqlConnectionStringBuilder`, design choice 5); `SELECT GET_LOCK(@n, 0)`, `SELECT RELEASE_LOCK(@n)`, `SELECT IS_USED_LOCK(@n) = CONNECTION_ID()`, `SELECT IS_USED_LOCK(@n) IS NOT NULL`; a `NULL` result or a `MySqlException` becomes `Left` (`LockErrorCodes` backend-fault code of #2667), cancellation rethrown
3. **`MySQLDistributedLockProvider.cs`** (new, public sealed partial) — `IDistributedLockProvider`; constructor `(IOptions<MySQLLockOptions> options, ILogger<MySQLDistributedLockProvider> logger, TimeProvider? timeProvider = null)` plus an internal constructor taking the session factory; `TryAcquireAsync` (poll loop), `AcquireAsync` (same loop, no deadline), `IsLockedAsync`, `ExtendAsync(resource, extension, ct)` through the registry of live handles; a private `LockHandle : ILockHandle` that arms the expiry timer with `TimeProvider.CreateTimer` (the callback catches everything and logs the exception type), releases at the deadline, and verifies the session on `ExtendAsync`
4. **`MySQLLockDiagnostics.cs`** (new, internal static) — `ActivitySource` and `Meter` named `Encina.DistributedLock.MySQL`; counter `encina.distributed_lock.acquisitions` with tags `provider` (`mysql`) and `outcome` (`acquired`, `contended`, `failed`); no tenant tag (the provider is tenant-unaware; the decorator of #741 adds `encina.tenant_id` per ADR-054 D4)
5. **`[LoggerMessage]` methods** inside the provider (`partial`), EventIds 7560-7567 (Phase 5)
6. **`PublicAPI.Unshipped.txt`** and the manifest (targets in Phase 6)
7. **Tests**: `FakeUserLockSession`, provider unit tests (acquired, contended until the deadline with `FakeTimeProvider`, fault on try-lock, `NULL` result, cancellation, registry extend held and unheld, expiry release, double dispose), handle unit tests, guard tests, `Encina.GuardTests.csproj` project reference

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #208: the MySQL lock provider on the ADR-052 contract.

CONTEXT:
- #2667 has merged: IDistributedLockProvider returns Either<EncinaError, Option<ILockHandle>> (TryAcquireAsync),
  Either<EncinaError, ILockHandle> (AcquireAsync), Either<EncinaError, bool> (IsLockedAsync, ExtendAsync);
  LockErrorCodes exists. Read their real signatures and codes before editing.
- One dedicated unpooled session per live handle; poll GET_LOCK(name, 0) every `retry` until `wait`; enforce the
  expiry with a TimeProvider timer; IsLockedAsync uses IS_USED_LOCK (design choices 3, 4, 5, 7).

TASK:
Write IUserLockSession and its MySqlConnector adapter, the provider with its private handle, the diagnostics class
and the log methods (EventIds 7560-7567). Write the fake session and the unit and guard tests listed in the Tasks.

KEY RULES:
- Contention is Right(None), never a failure in logs or metrics. A fault (including NULL from GET_LOCK) is Left;
  the error and every log carry no exception message (ForLogging, exception type only).
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

#### `src/Encina.DistributedLock.MySQL/`

1. **`ServiceCollectionExtensions.cs`** (new, public static) — `AddEncinaDistributedLockMySQL(this IServiceCollection, string connectionString)` and the overload with `Action<MySQLLockOptions>`; `TryAddSingleton(TimeProvider.System)`, `Configure<MySQLLockOptions>`, `TryAddSingleton<IUserLockSessionFactory>`, `TryAddSingleton<IDistributedLockProvider, MySQLDistributedLockProvider>`; registers the health check when `ProviderHealthCheck.Enabled`; options validated at start (non-empty connection string)
2. **`Health/MySQLDistributedLockHealthCheck.cs`** (new, public sealed) — `IEncinaHealthCheck`; `public const string DefaultName = "mysql-distributed-lock"`; tags `["mysql", "distributed-lock", "ready"]`; opens a session, takes and releases a random lock name, returns `Unhealthy` with the exception type only
3. **`PublicAPI.Unshipped.txt`**, the manifest
4. **Tests**: DI test with `ValidateOnBuild` and `ValidateScopes` (registration completeness, `AGENTS.md` section 3), a registration-order test (an application's own provider registration is not overridden), health check unit tests with the fake session, guard tests of both public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #208: registration and health check of the MySQL lock provider.

CONTEXT:
- Phase 2 produced MySQLDistributedLockProvider and IUserLockSessionFactory.
- The registration must resolve everything the provider needs: options, TimeProvider, the session factory, logger.

TASK:
Write ServiceCollectionExtensions (two overloads) and MySQLDistributedLockHealthCheck; add the DI test with
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

1. **Tenancy**: no code in this package. The key the provider receives is already tenant-qualified by the decorator of [#741](https://github.com/dlrivada/Encina/issues/741) (reserved `default` tenant segment, injective encoding); the name function hashes the whole composed key, so distinct tenant/resource pairs stay distinct. A unit test asserts the provider never reads a tenant.
2. **Fail closed**: a database fault never reports a held lock: `Left`; a lock that cannot be verified on `ExtendAsync` is `Right(false)`.
3. **Resilience**: no Polly policy; the wait/retry loop is the retry, and a faulted try-lock ends the attempt with `Left` (the caller decides).
4. **README note** (through `docs-writer`): one server connection per live lock, `max_connections` sizing, MariaDB statement of design choice 6.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #208: the cross-cutting statements of the MySQL lock provider.

CONTEXT:
- Tenant scoping lives above the provider (issue #741); the provider hashes the composed name it is given.
- A fault is Left, never "held" and never "free".

TASK:
Add the unit test that proves the provider is tenant-agnostic, the tests that prove a fault is Left on every public
method, and the README paragraph through docs-writer.

KEY RULES:
- No new public API in this phase.
- Documentation goes through docs-writer; changelog and PublicAPI through mechanical-fixer.

REFERENCE FILES:
- issue #741 (the tenant decorator; read the issue, its plan is not on main yet)
- src/Encina.DistributedLock.MySQL/MySQLDistributedLockProvider.cs
```

</details>

### Phase 5: Observability — logs, diagnostics and EventIds

> **Goal**: Operational visibility with EventIds registered first (ADR-021).

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina/Diagnostics/EventIdRanges.cs`** (modify, PR 1) — `public static readonly (int Min, int Max) DistributedLockMySQL = (7560, 7569);` with the XML summary `Encina.DistributedLock.MySQL — MySQL user-level lock provider`; the new field line in `src/Encina/PublicAPI.Unshipped.txt`
2. **`tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`** (modify, PR 1) — `["Encina.DistributedLock.MySQL"] = [nameof(EventIdRanges.DistributedLockMySQL)]`
3. **Log messages** (PR 2) — 7560 lock acquired (Debug, carries the resource), 7561 lock released (Debug), 7562 wait elapsed, lock contended (Debug), 7563 acquire fault (Warning, exception type), 7564 release failed, connection closed to free the lock (Warning), 7565 expiry reached, lock released by the timer (Information), 7566 extend found the lock lost (Warning), 7567 timer release failed (Warning). 7568-7569 stay spare. Contention is never an error level.
4. **Metrics and spans** (PR 2) — `MySQLLockDiagnostics`: spans `DistributedLock.Acquire` and `DistributedLock.Release`, counter `encina.distributed_lock.acquisitions`; names align with the shared instruments of [#724](https://github.com/dlrivada/Encina/issues/724) and move onto them if it lands first.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #208: EventIds, logs and instruments of the MySQL lock provider.

CONTEXT:
- ADR-021: register the range in src/Encina/Diagnostics/EventIdRanges.cs before using an id; map the assembly in
  EncinaEventIdAllocationTests. The family block 7550-7599 of the plan batch gives this package 7560-7569.
- Contention is not a failure: Debug level and outcome "contended".

TASK:
Register DistributedLockMySQL = (7560, 7569), map the assembly, add the [LoggerMessage] methods with the ids in
the Tasks, and the diagnostics class. Run the architecture tests.

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

Per-file targets (the manifest lists them with these sentences; the numbers are a demanding, realistic start and the worker raises them to what it measures):

| File | unit | guard | contract | integration | property | Justification |
|---|---:|---:|---:|---:|---:|---|
| `MySQLDistributedLockProvider.cs` | 90 | 14 | 45 | 70 | 0 | The poll loop, registry, timer, handle and error mapping run against the fake session; the contract suite adds the shared scenarios; integration adds the real server; property 0: no randomized logic beyond the name |
| `MySqlConnectorUserLockSession.cs` | 5 | 5 | 0 | 90 | 0 | Needs a real MySQL; unit and guard reach only the guards; contract 0 because the contract class runs on the shared container (integration flag) |
| `MySQLLockName.cs` | 100 | 0 | 0 | 0 | 95 | Pure function; guard 0: internal type without `InternalsVisibleTo` for `Encina.GuardTests`; FsCheck covers determinism, length and alphabet |
| `MySQLLockOptions.cs` | 100 | 0 | 0 | 0 | 0 | Property bag; guard 0: no parameters |
| `ServiceCollectionExtensions.cs` | 95 | 90 | 0 | 0 | 0 | DI with a validated provider; guard covers each overload |
| `Health/MySQLDistributedLockHealthCheck.cs` | 85 | 85 | 0 | 75 | 0 | Fake session for unit; the integration test runs it against the container |
| `MySQLLockDiagnostics.cs` | 80 | 0 | 0 | 0 | 0 | Instruments exercised through the provider tests; guard 0: no parameters |

1. **Unit** — the scenarios of Phases 1 to 3 with `FakeTimeProvider` and the fake session.
2. **Guard** — constructor (null options, null logger, options without a connection string) and each method (null or whitespace resource, non-positive expiry as #2724 decides).
3. **Contract** — one derived class of the suite #2667 creates, `MySQLDistributedLockContractTests`, running the same scenarios as InMemory, Redis and SQL Server (free, contended, expiry, extend held and unheld, double dispose, cancellation); `Encina.ContractTests.csproj` reference.
4. **Property** — FsCheck on `MySQLLockName`: always 64 lower-case hex characters, deterministic, no exception for any string.
5. **Integration** — `[Collection("ADO-MySQL")]` on `MySqlFixture`, `[Trait("Category", "Integration")]`, `[Trait("Database", "MySQL")]`: two providers on one database block each other; release frees the lock; `KILL CONNECTION` of the holder frees it; the expiry timer frees it; `IsLockedAsync` does not disturb a concurrent contender; a long raw name (200 characters) works; cancellation inside a wait; health check on a real server.
6. **Load and benchmark** — `.md` justification files under `tests/Encina.NBomber/Scenarios/Locking/` and `tests/Encina.BenchmarkTests/Encina.DistributedLock.Benchmarks/`: the provider is a thin client over a database primitive, covered by the shared lock scenarios once its factory is added by the issue that owns those scenarios; a missing `.md` would mean "not evaluated" (`AGENTS.md` section 9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #208: the tests of the MySQL lock provider.

CONTEXT:
- Unit and guard tests were written with each phase; this phase adds contract, property, integration and the
  justification files, and raises the manifest targets to the measured values.
- Integration tests use the shared collection ADO-MySQL (MySqlFixture); never a per-class container.

TASK:
Write the contract class, the FsCheck name property, the integration tests of the Tasks, the two justification
files, and measure every flag from the repository root with `dotnet test tests\Encina.<Flag>Tests --collect
"XPlat Code Coverage"` and `coverage-report.cs`.

KEY RULES:
- Tests execute real package code; assertions with Shouldly through Encina.Testing.Shouldly.
- No Thread.Sleep: FakeTimeProvider for unit tests; polling with timeouts for integration.
- The PR reports measured per-flag coverage against the manifest targets.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/SqlServerDistributedLockIntegrationTests.cs
- tests/Encina.IntegrationTests/ADO/Collections.cs
- tests/Encina.TestInfrastructure/Fixtures/MySqlFixture.cs
```

</details>

### Phase 7: Documentation and Finalization

> **Goal**: The package is documented and the 1.0 inventory is true.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation** on every public member (summary, remarks, param, returns, one example on the registration).
2. **`src/Encina.DistributedLock.MySQL/README.md`** (new, `docs-writer`) — what the provider does, the hashed lock name, expiry behavior, connection sizing, MariaDB statement, a `Match` example with `await using`.
3. **`changelog.d/208-lock-mysql.added.md`** (`mechanical-fixer`).
4. **`AGENTS.md` section 5**: no edit (the lock table already lists MySQL, #208); `docs/INVENTORY.md` gets the package row when the file lists packages (`docs-writer` checks).
5. **ADR**: none; the decisions are the lock family's ADR-052 and this plan's choices.
6. **Build and test verification**: `dotnet build --configuration Release` with zero warnings; every flag at its manifest target; `check-plan.ps1` for this file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are finishing issue #208: documentation and verification of Encina.DistributedLock.MySQL.

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
| MySQL reference manual, "Locking Functions" | `GET_LOCK(str, timeout)` returns 1, 0 or NULL; `RELEASE_LOCK` returns 1, 0 or NULL; `IS_USED_LOCK` returns the holder's connection id or NULL; lock names are limited to 64 characters; a lock is released when the session ends | The functions, the name limit and the release behavior |
| MySqlConnector documentation, connection pooling and reset | A pooled connection is reset when it is next used | Why the lock connections are unpooled (design choice 5) |
| ADR-052 | Lock providers return `Either`; contention is `Right(None)`; a fault is `Left` | Outcome mapping of every method |
| ADR-062 | Error categories; R4 provider failures are `Internal`; R5 cancellation | Categories of the `Left` cases |
| SPEC-000 REQ-027, DEC-003, DEC-009, AC-027 | Seven lock providers for 1.0, contract tests over them | This provider joins the contract suite |
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Secrets, time, async database calls, errors, EventIds, coverage | Constraints used throughout |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IDistributedLockProvider`, `ILockHandle`, `DistributedLockOptions` | `src/Encina.DistributedLock/` | The contract (after #2667) and the base options |
| `SqlServerDistributedLockProvider` and its registration | `src/Encina.DistributedLock.SqlServer/` | Structural model: connection per handle, `GetLockKey`, log methods, health registration |
| `SqlServerDistributedLockHealthCheck` | `src/Encina.DistributedLock.SqlServer/Health/` | Model of the health check |
| `IEncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check contract and options |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | Registration of 7560-7569 and the assembly map |
| `MySqlFixture` and `ADO-MySQL` collection | `tests/Encina.TestInfrastructure/Fixtures/`, `tests/Encina.IntegrationTests/ADO/Collections.cs` | Integration tests on the shared container |
| `MySqlConnector` 2.6.1 | `Directory.Packages.props` | The driver |
| `ForLogging()` | `src/Encina/Diagnostics/` | Exception logging without the message |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | This plan | Notes |
|----------|:-:|-------|
| InMemory | N/A | Single-process testing provider; changed by #2667 |
| Redis | N/A | Quorum mode is [#226](https://github.com/dlrivada/Encina/issues/226) |
| SqlServer | N/A | Model for this plan; its defects are #2405, #2720, #2722 |
| PostgreSQL (#207) | N/A | Own plan; same shape and the same timer and registry design |
| MySQL (#208) | Included | The subject of this plan |
| Azure Blob (#209) | N/A | Own plan; lease based |
| DynamoDB (#210) | N/A | Own plan; conditional write |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DistributedLock.MySQL` (new range `DistributedLockMySQL`) | 7560-7569 | From the block 7550-7599 assigned to the family `lock-providers-1`; 7560-7567 used, 7568-7569 spare. Siblings: 7550-7559 PostgreSQL (#207), 7570-7579 Azure Blob (#209), 7580-7589 DynamoDB (#210) |
| `Encina.DistributedLock.SqlServer` (existing, for reference) | 3800-3849 | Not touched |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 9 | csproj, options, name, session seam, MySqlConnector adapter, provider, diagnostics, health check, registration |
| Metadata (new and modified) | 7 | two `PublicAPI` files, `Encina.slnx`, `EventIdRanges.cs`, `Encina` `PublicAPI.Unshipped.txt`, manifest (`Directory.Packages.props` is not touched: `MySqlConnector` is already there) |
| Unit tests | 6 | options, name, provider, handle, DI, health check (plus the fake session) |
| Guard tests | 2 | provider, registration and health check |
| Contract and property tests | 2 | contract class, name property |
| Integration tests | 1 | one class on the shared collection |
| Test project references | 5 | UnitTests, GuardTests, ContractTests, IntegrationTests, PropertyTests |
| Documentation and changelog | 4 | README, fragment, two justification files |
| **Total** | **~36 touches** | Over 4 pull requests of at most 15 files each (13, 11, 7 and 11) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #208: Encina.DistributedLock.MySQL, a lock provider on MySQL user-level locks.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- Lock contract: ADR-052, implemented by #2667 before this work: TryAcquireAsync returns
  Either<EncinaError, Option<ILockHandle>>, AcquireAsync returns Either<EncinaError, ILockHandle>, IsLockedAsync and
  ExtendAsync return Either<EncinaError, bool>, cancellation propagates, LockErrorCodes holds the Left codes.
- Time only from TimeProvider. EncinaError.Message never reaches logs, tags or health checks.

IMPLEMENTATION OVERVIEW:
1. Package skeleton: csproj, MySQLLockOptions ([JsonIgnore] connection string), MySQLLockName (lower-case hex
   SHA-256 of "{prefix}:lock:{resource}", 64 characters).
2. IUserLockSession seam and MySqlConnectorUserLockSession adapter (unpooled connection); MySQLDistributedLockProvider:
   poll GET_LOCK(name, 0) every retry until wait, one session per handle, expiry enforced by a TimeProvider timer
   that never throws, provider ExtendAsync through a registry of live handles, IsLockedAsync through IS_USED_LOCK.
3. AddEncinaDistributedLockMySQL (two overloads, TryAdd*), health check, DI test with ValidateOnBuild.
4. EventIds 7560-7567 in range DistributedLockMySQL (7560-7569), diagnostics, ActivitySource and Meter.
5. Tests: unit (fake session, FakeTimeProvider), guard, contract (suite of #2667), property (name), integration on
   the ADO-MySQL collection; documentation through docs-writer; changelog and PublicAPI through mechanical-fixer.

KEY PATTERNS:
- Contention is Right(None); a database fault (including NULL from GET_LOCK) is Left; a lost lock is Right(false)
  on ExtendAsync.
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
| 3 | Structured Logging | ✅ | Phase 5: `[LoggerMessage]` EventIds 7560-7567 in the registered range, exception type only |
| 4 | Health Checks | ✅ | Phase 3: `MySQLDistributedLockHealthCheck` takes and releases a random lock name |
| 5 | Validation | ✅ | Phases 1 and 3: connection string validated at startup; resource and expiry arguments validated as [#2724](https://github.com/dlrivada/Encina/issues/2724) decides |
| 6 | Resilience | ❌ | The provider's own wait/retry loop is the retry; a fault ends the attempt with `Left` and the caller decides; a Polly policy would double the retries |
| 7 | Distributed Locks | ❌ | This is the distributed lock feature itself |
| 8 | Transactions | ❌ | A lock is not a transactional resource; no unit of work is involved |
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
| [#207](https://github.com/dlrivada/Encina/issues/207) — PostgreSQL provider | Open (family plan) | Same shape; no shared code; the two plans keep the timer, registry and seam rules identical |
| [#741](https://github.com/dlrivada/Encina/issues/741) — tenant-scoped locks | Open (family plan) | Owns tenant key composition; provider unchanged |
| [#755](https://github.com/dlrivada/Encina/issues/755) — health checks of the other lock providers | Open | Adds the first lock reference to the contract test project; whichever lands first, the other drops its line |
| [#724](https://github.com/dlrivada/Encina/issues/724) — lock observability | Open | Phase 5 reuses shared instruments if they exist |

---

## Next Steps

1. Link this plan from issue #208 and start the PR 1 brief; PRs 2 to 4 wait for [#2667](https://github.com/dlrivada/Encina/issues/2667).
2. When work on the issue starts, ask the maintainer to answer Dilemmas 2 (hashed or raw name), 3 (poll or block), 4 (enforced expiry), 5 (unpooled connections) and 6 (MariaDB), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices.
3. One commit per phase; the last commit of PR 4 references `Fixes #208`.
