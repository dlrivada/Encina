# Implementation Plan: `Encina.Compliance.DPIA` — Distributed Lock for `DPIAReviewReminderService`

> **Issue**: [#763](https://github.com/dlrivada/Encina/issues/763)
> **Type**: Feature
> **Complexity**: Low (one service, one option, 1 pull request)
> **Estimated Scope**: ~120-180 lines of production code + ~350-450 lines of tests
> **Specification**: GDPR Art. 35(11) (periodic review of the assessment); [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (lock failure contract); `AGENTS.md` section 3 (background infrastructure)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision)
> **Related**: [#3502](https://github.com/dlrivada/Encina/issues/3502) (the cycle publishes fire-and-forget and drops the `Left`), [#768](https://github.com/dlrivada/Encina/issues/768) (migrate the service to `Encina.Scheduling`), [#3270](https://github.com/dlrivada/Encina/issues/3270) (remove `EnableExpirationMonitoring`), [#717](https://github.com/dlrivada/Encina/issues/717) (generic leader election, same family, plan (#717)), [#741](https://github.com/dlrivada/Encina/issues/741) (tenant-scoped locks; this lock stays global), [#223](https://github.com/dlrivada/Encina/issues/223)

---

## Summary

`DPIAReviewReminderService` is a `BackgroundService` that, when `DPIAOptions.EnableExpirationMonitoring` is on, runs a cycle at start and every `ExpirationCheckInterval` (default 1 hour) on a `PeriodicTimer`: it asks `IDPIAService.GetExpiredAssessmentsAsync`, logs, and publishes `DPIAAssessmentExpired` through `IEncina`. In a multi-instance deployment every instance runs every cycle, so the same assessment is published once per instance and the store is queried N times. This plan makes **only one instance run each cycle**:

1. **Lock around the cycle.** Before the cycle opens its scope, the service tries `IDistributedLockProvider.TryAcquireAsync("encina:dpia:review-reminder", expiry: LockTimeout, wait: 0, retry: 0, ct)` and runs the cycle only while it holds the handle.
2. **ADR-052 outcomes.** `Right(Some(handle))` runs the cycle and disposes the handle at the end; `Right(None)` skips the cycle (another instance holds it; logged at Debug, counted as `skipped`); `Left` fails the cycle visibly (logged by error code, counted as `failed`, retried at the next tick); cancellation propagates.
3. **Opt-in.** The provider is resolved with `GetService` from the cycle scope; with no provider registered the service runs unlocked as today (single-instance assumption).
4. **The lock must cover the publishing.** Today `PublishExpiredNotificationsAsync` is started with `_ = ...` and the scope is disposed when the cycle method returns, so a lock released at the end of the cycle would release before the notifications are published. The plan awaits the publishing inside the locked section (the error handling of that publishing stays with [#3502](https://github.com/dlrivada/Encina/issues/3502)).

**Standards**: GDPR Art. 35(11); ADR-052; `AGENTS.md` section 3 (errors are not swallowed in background infrastructure; a `PeriodicTimer` loop catches per iteration).

**Estimated scope**: 0 new production files, 6 modified, about 3 test files extended, 2 documentation files.

**Affected packages**: `Encina.Compliance.DPIA` (with a new project reference to `Encina.DistributedLock`). No provider code is added.

**Provider category**: Distributed locks (consumer only). The feature works with any `IDistributedLockProvider`; it adds no provider code, so the seven-provider matrix of `AGENTS.md` section 5 is covered by the abstraction (see Research).

**What the code does today** (`origin/main` at `7a0049c07`):

- `src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs` is `internal sealed class ... : BackgroundService`, registered by `services.AddHostedService<DPIAReviewReminderService>()` (`ServiceCollectionExtensions.cs:184`) and built from `(IServiceScopeFactory, IOptions<DPIAOptions>, ILogger<>)`. Its `ExecuteAsync` returns when monitoring is off, runs the first cycle at once, then loops on `PeriodicTimer(ExpirationCheckInterval)`.
- `ExecuteReminderCycleAsync` creates an async scope, resolves `IDPIAService`, and matches the `Either`: `Right` logs and (when `PublishNotifications`) starts `_ = PublishExpiredNotificationsAsync(...)`; `Left` logs the code. It catches cancellation and any other exception per cycle (the loop never throws). Counter `dpia.review_reminder.cycles.total` already has outcomes `completed`, `failed`, `cancelled` (`DPIADiagnostics.ReviewReminderCyclesTotal`, tag `dpia.outcome`).
- Log messages are extension methods in `Diagnostics/DPIALogMessages.cs`; the block `8840-8849` (Expiration monitoring) uses 8840-8848, so **8849 is the only free id in that block**, and `8897-8899` (the ids the original issue suggested) sit inside the ASP.NET Core endpoints block `8890-8899`.
- `Encina.Compliance.DPIA` does not reference `Encina.DistributedLock`; no production code under `src/` consumes `IDistributedLockProvider` yet (ADR-052 context), so this is its first consumer. `Encina.DistributedLock` references `Encina` and `Encina.Messaging`, so the new reference creates no cycle.
- `DPIAOptionsValidator` (registered as `IValidateOptions<DPIAOptions>`, `ServiceCollectionExtensions.cs:109`) validates `ExpirationCheckInterval` when monitoring is enabled.
- The manifest `.github/coverage-manifest/Encina.Compliance.DPIA.json` gives `DPIAReviewReminderService.cs` the targets unit 93 and guard 7.
- Other services named in the original issue (`RetentionEnforcementService`, `BreachDeadlineMonitorService`) were not verified and are not in scope.

---

## Design Choices

<details>
<summary><strong>1. Lock expiry — fixed <code>LockTimeout</code>, or extend the lock while publishing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fixed expiry `DPIAOptions.LockTimeout` (default 30 s, the issue's value), wait 0, no extension** | Simplest; the generic extension loop is #223; a skipped cycle is cheap | A cycle that publishes thousands of notifications for longer than the expiry can overlap with another instance's cycle |
| **B) Extend the lock with `ILockHandle.ExtendAsync` after each published notification** | Safe for slow cycles | Couples the service to the handle's extension semantics (#2405 is open for the Redis provider-level extend) and duplicates what #223 will provide |

### Chosen Option: **A — fixed expiry** (recommended, pending the maintainer)

### Rationale

- A wins when the expected number of expired assessments per cycle is small (typical: tens), where a 30 s lock is ample; B wins for tenants with thousands of expired assessments and slow notification handlers.
- The expiry is configurable, so a deployment with slow handlers raises `LockTimeout`; the documentation says so. #223 (automatic extension) is the place to remove the limit for every consumer.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Lock the existing service now, or wait for the migration to <code>Encina.Scheduling</code> (#768)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add the lock to the current `BackgroundService`** | Closes the duplicate-notification defect now with a small change; independent of the open scheduling-lock work | The change is thrown away if #768 later replaces the service |
| **B) Do nothing and let #768 (recurring `ScheduledMessage`, lock through #716) fix it** | No throwaway work; one coordination mechanism | #768 is a medium refactor and #716 (locks in scheduled dispatch) is a large critical issue; the duplicate defect stays open meanwhile |

### Chosen Option: **A — lock the current service** (recommended, pending the maintainer)

### Rationale

- B wins if #768 is scheduled for the same release; A wins when it is not, which is the case today (neither #768 nor #716 has a plan or a worker).
- The lock call sits in one private method, so removing it during #768 is a one-method deletion.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. No provider registered — run unlocked</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Resolve `IDistributedLockProvider` optionally; without it the cycle runs as today** | Opt-in; single-instance deployments pay nothing | A multi-instance deployment that forgot the provider keeps duplicating, silently |
| **B) Require the provider whenever monitoring is on** | No silent duplicates | Forces a lock provider (and a Redis or SQL Server) on single-instance users; ADR-050 lists no such requirement |

### Chosen Option: **A — optional provider**

### Rationale

- The "distributed locks" row of ADR-050 is conditional on multi-instance and is enforced by startup validation of the lock providers (#2382), not by each consumer; this plan adds no requirement beyond it. One Information log at startup states whether the cycle is locked.

**Class:** Settled
**Settled by:** `AGENTS.md` section 1 (pay-for-what-you-use), ADR-050 (closed list of requirements; "Distributed locks" is conditional on multi-instance)

</details>

<details>
<summary><strong>4. Outcome handling — skip on contention, fail on a fault</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Right(None)` skips (Debug log, counter `skipped`); `Left` fails the cycle (log by error code, counter `failed`); the next tick retries** | Contention is normal and not an error; a fault is never reported as success | Two outcomes to test |
| **B) Treat `Left` like contention** | One branch | A broken lock provider would look like a healthy standby, and duplicates would resume unnoticed |

### Chosen Option: **A — ADR-052 mapping**

### Rationale

- The existing `ReviewReminderCycleFailed(errorCode)` message and the existing `failed` outcome carry the `Left`; only the "skipped" message is new.

**Class:** Settled
**Settled by:** ADR-052 (contention is not an error), `AGENTS.md` section 3 (a `Left` in background infrastructure fails the operation, never reports success; a `PeriodicTimer` loop catches per iteration)

</details>

<details>
<summary><strong>5. Lock scope, name, wait and where it is resolved</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Global lock named `encina:dpia:review-reminder`, wait 0 and retry 0, provider resolved from the cycle scope inside the cycle method** | The service checks all assessments, so one global cycle is right; the provider's `KeyPrefix` still applies; resolving per cycle needs no constructor change | The name is a constant in code |
| **B) Tenant-scoped lock per tenant** | Per-tenant cycles | The store query is not per tenant; #741 is not needed here |

### Chosen Option: **A — global, per-cycle resolution**

### Rationale

- If the application enables the tenant decorator of #741 with the default scope (global by default, recommended there), this lock stays global with no change.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 3 (time and options conventions), the lock name of the issue text, `src/Encina.DistributedLock/DistributedLockOptions.cs` (`KeyPrefix` applied by the provider)

</details>

<details>
<summary><strong>6. Telemetry and EventIds — reuse the existing counter, one new log id</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add outcome `skipped` to `dpia.review_reminder.cycles.total` and a `dpia.lock.outcome` tag on the cycle activity; one new message `ReviewReminderCycleSkippedLockNotAcquired` with EventId 8849 (the last free id of the expiration-monitoring block); acquired and failed reuse the existing cycle-start and cycle-failed messages** | No new instrument; the id stays in its block; packed | The lock outcome is on the activity, not a separate counter |
| **B) New counter `dpia.review_reminder.lock.total` and three messages at 8897-8899** | Separate lock metric | Ids inside the endpoints block (8890-8899); duplicates what the cycle counter already says |

### Chosen Option: **A — reuse, one new id (8849)**

### Rationale

- The DPIA range 8800-8899 is registered and fully grouped; no registry change is needed, and the issue's suggested ids 8944-8946 belong to Privacy by Design.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 7 (ids inside the package range, packed, grouped by area), `src/Encina.Compliance.DPIA/Diagnostics/DPIALogMessages.cs` allocation blocks

</details>

---

## Implementation Phases

One pull request (about 11 files, well under 15 files and 1500 lines).

### Phase 1: Options and Validation

> **Goal**: The `LockTimeout` option, validated.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Encina.Compliance.DPIA.csproj`** (modify) — `ProjectReference` to `..\Encina.DistributedLock\Encina.DistributedLock.csproj`
2. **`DPIAOptions.cs`** (modify) — `TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30)` with XML docs: the lock expiry of a review cycle; raise it when publishing is slow (see design choice 1)
3. **`DPIAOptionsValidator.cs`** (modify) — when `EnableExpirationMonitoring` is true, `LockTimeout` must be positive
4. **`PublicAPI.Unshipped.txt`** — `DPIAOptions.LockTimeout.get/set`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #763: the LockTimeout option of the DPIA review reminder.

CONTEXT:
- DPIAOptions has EnableExpirationMonitoring (default false) and ExpirationCheckInterval (default 1 hour);
  DPIAOptionsValidator rejects a non-positive interval when monitoring is on.

TASK:
Add the project reference, DPIAOptions.LockTimeout (default 30 s), the validator rule and the PublicAPI lines
(through mechanical-fixer). Extend the options and validator unit tests.

KEY RULES:
- XML documentation on the new property; zero warnings.
- The validator reports a failure message naming LockTimeout and the condition.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAOptions.cs
- src/Encina.Compliance.DPIA/DPIAOptionsValidator.cs
- src/Encina.Compliance.DPIA/Encina.Compliance.DPIA.csproj
```

</details>

### Phase 2: Lock the Reminder Cycle

> **Goal**: One instance per cycle, with the lock covering the publishing.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs` (modify; after #2667)

1. A constant `LockResource = "encina:dpia:review-reminder"`
2. Split the cycle into small methods so CRAP stays at or under 10: `ExecuteReminderCycleAsync` (activity, catch blocks, counter) calls `TryAcquireCycleLockAsync` (resolves `IDistributedLockProvider` with `GetService` from a scope, calls `TryAcquireAsync(LockResource, _options.LockTimeout, TimeSpan.Zero, TimeSpan.Zero, ct)` and maps the `Either` to a small internal result: `Unlocked`, `Held(handle)`, `Skipped`, `Faulted(errorCode)`) and `RunLockedCycleAsync` (the current body)
3. `Held`: `await using` the handle around `RunLockedCycleAsync`; `Skipped`: `ReviewReminderCycleSkippedLockNotAcquired`, counter outcome `skipped`, activity tag `dpia.lock.outcome=skipped`; `Faulted`: `ReviewReminderCycleFailed(errorCode)`, counter outcome `failed`; `Unlocked`: run as today
4. `PublishExpiredNotificationsAsync` is awaited inside the locked section instead of `_ = ...`; its own error handling is not changed (it belongs to #3502)
5. One Information log at service start saying whether a lock provider is present

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #763: the lock around DPIAReviewReminderService's cycle.

CONTEXT:
- Issue #2667 has changed IDistributedLockProvider to the ADR-052 shapes: TryAcquireAsync returns
  Either<EncinaError, Option<ILockHandle>>. Read the current signatures first.
- The loop is a PeriodicTimer loop: the cycle catches per iteration and never throws; cancellation propagates.
- PublishExpiredNotificationsAsync is currently fire-and-forget, so a lock released at the end of the cycle would
  release before the notifications are published; it must be awaited inside the locked section.

TASK:
Implement the Phase 2 tasks. Add unit tests: lock acquired and notifications published before the handle is
disposed (assert the order), contention skips and publishes nothing, Left logs the code and counts failed, no
provider runs unlocked, cancellation propagates, handle disposed on exception, second cycle after a skip.

KEY RULES:
- Never log EncinaError.Message; the error code only. Exceptions through ForLogging().
- Time and delays as today (PeriodicTimer); no DateTime.UtcNow.
- Every changed method CRAP <= 10: keep the lock decision separate from the cycle body.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- tests/Encina.UnitTests/Compliance/DPIA/DPIAReviewReminderServiceTests.cs
```

</details>

### Phase 3: Cross-Cutting Integration

> **Goal**: The matrix functions marked included are in place.

<details>
<summary><strong>Tasks</strong></summary>

1. **Validation (5)** — Phase 1 validator rule
2. **Distributed locks (7)** — Phase 2
3. **Resilience (6)** — not a policy: a failed acquisition fails the cycle and the next tick retries
4. **Registration completeness** — the lock provider is an optional dependency, so no registration is added by `AddEncinaDPIA`; a DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` with and without a lock provider registered

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #763: cross-cutting verification.

CONTEXT:
- The service resolves IDistributedLockProvider optionally per cycle; AddEncinaDPIA registers no lock provider.

TASK:
Add the DI test (with the InMemory lock provider and without any provider) proving the hosted service resolves and
runs one cycle in both cases with ValidateOnBuild and ValidateScopes.

KEY RULES:
- No new hard dependency on a lock provider.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs
- src/Encina.DistributedLock.InMemory/ServiceCollectionExtensions.cs
```

</details>

### Phase 4: Observability

> **Goal**: Skips and lock outcomes are visible.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/DPIALogMessages.cs`** (modify) — `ReviewReminderCycleSkippedLockNotAcquired(this ILogger logger)`, `EventId = 8849`, Debug, inside the expiration-monitoring block; update the block comment
2. **`Diagnostics/DPIADiagnostics.cs`** (modify) — a constant for the outcome value `skipped` and the activity tag key `dpia.lock.outcome`; no new instrument
3. The architecture tests for EventIds keep passing (the range 8800-8899 and the assembly mapping already exist)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #763: logging and telemetry of the lock outcomes.

CONTEXT:
- DPIA ids live in 8800-8899, grouped in blocks of ten; the expiration-monitoring block 8840-8849 has exactly one
  free id, 8849. The counter dpia.review_reminder.cycles.total has the tag dpia.outcome.

TASK:
Add the message and the diagnostics constants, use them in the service, run the EventId architecture tests.

KEY RULES:
- EventId inside the registered range, no gaps; XML docs name the block.
- No assessment id, tenant id or lock key in tags; the outcome values are fixed strings.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Diagnostics/DPIALogMessages.cs
- src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 5: Testing

> **Goal**: Unit and guard flags at or above their targets.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/DPIA/DPIAReviewReminderServiceTests.cs`) — the Phase 2 scenarios with a mocked provider, plus one test with the real InMemory provider (two services, one lock: only one runs the cycle), and the validator tests
2. **Guard** (`tests/Encina.GuardTests/Compliance/DPIA/DPIAReviewReminderServiceGuardTests.cs`) — unchanged constructor; extend only if a new public parameter appears
3. **Contract, property** — not applicable: no new public interface and no complex invariant
4. **Integration** — justification file: no database storage is added; the lock behavior is proved by the unit tests with the InMemory provider and by the lock providers' own integration tests
5. **Load and benchmark** — justification files: a background service that runs once per interval
6. **Coverage manifest** — targets of `DPIAReviewReminderService.cs` stay at or above unit 93 and guard 7; justifications updated (written by `mechanical-fixer`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #763: tests of the locked reminder cycle.

CONTEXT:
- Tests execute the real service with a FakeTimeProvider-driven PeriodicTimer where possible; assertions use
  Shouldly through Encina.Testing.Shouldly.
- Coverage is per flag against .github/coverage-manifest/Encina.Compliance.DPIA.json; a target never goes down.

TASK:
Write the tests of the Phase 5 tasks, measure unit and guard coverage of DPIAReviewReminderService.cs with
`dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage"` (and the guard project) and report the
per-file table against the targets.

KEY RULES:
- No Thread.Sleep; a test never swallows an exception thrown by the code under test.
- Justification .md files only for load, benchmark and integration, never for unit or guard.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/DPIA/DPIAReviewReminderServiceTests.cs
- tests/Encina.GuardTests/Compliance/DPIA/DPIAReviewReminderServiceGuardTests.cs
- .github/coverage-manifest/Encina.Compliance.DPIA.json
```

</details>

### Phase 6: Documentation and Finalization

> **Goal**: Operators know how to run the service on several instances.

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on `DPIAOptions.LockTimeout` and the updated remarks of `DPIAReviewReminderService`
2. `docs/features/dpia.md` through `docs-writer`: multi-instance guidance (register a lock provider, `LockTimeout`, what a skipped cycle means)
3. `changelog.d/763-dpia-reminder-lock.added.md` (written by `mechanical-fixer`)
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test`; every flag at its target; CRAP <= 10 on changed methods

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are finishing issue #763: documentation and verification.

CONTEXT:
- Documentation goes through docs-writer and the encina-docs skill; the changelog fragment through mechanical-fixer.

TASK:
Complete the Phase 6 tasks and report the per-flag figures of the touched files against their targets.

KEY RULES:
- English only; no private client application named; say plainly that without a lock provider every instance runs
  every cycle.
- Zero warnings; no [Obsolete].

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- docs/features/dpia.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| GDPR Art. 35(11) | Periodic review of the assessment | The cycle the service runs; duplicate runs duplicate the reminder |
| ADR-052 | Lock providers return `Either`; contention is `Right(None)` | Outcome mapping |
| ADR-050 | Distributed locks conditional on multi-instance, enforced by startup validation | Why the provider stays optional here |
| `AGENTS.md` section 3 | Background loops catch per iteration; a `Left` fails the operation | Cycle outcome handling |
| `AGENTS.md` section 7 | EventIds in the package range, packed | Message 8849 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `DPIAReviewReminderService` | `src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs` | The service gaining the lock |
| `DPIAOptions`, `DPIAOptionsValidator` | `src/Encina.Compliance.DPIA/DPIAOptions.cs`, `DPIAOptionsValidator.cs` | `LockTimeout` and its rule |
| `DPIADiagnostics` | `src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs` | `ReviewReminderCyclesTotal` counter, `TagOutcome`, `StartReviewReminderCycle` |
| `DPIALogMessages` | `src/Encina.Compliance.DPIA/Diagnostics/DPIALogMessages.cs` | Existing reminder messages 8840-8848; new 8849 |
| `IDistributedLockProvider`, `ILockHandle` | `src/Encina.DistributedLock/` | The abstraction consumed |
| `InMemoryDistributedLockProvider` | `src/Encina.DistributedLock.InMemory/` | Real provider for the two-instances unit test |
| `AddEncinaDPIA` registrations | `src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs` | Hosted service registration (line 184) and options validator (line 109) |
| DPIA reminder tests | `tests/Encina.UnitTests/Compliance/DPIA/DPIAReviewReminderServiceTests.cs`, `tests/Encina.GuardTests/Compliance/DPIA/DPIAReviewReminderServiceGuardTests.cs` | Extended |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Needed | Notes |
|----------|:-:|-------|
| InMemory | Test | One real-provider unit test; this is the first production consumer |
| Redis, SqlServer | N/A | Consumed through the abstraction; their own tests cover the locks |
| PostgreSQL (#207), MySQL (#208), Azure Blob (#209), DynamoDB (#210) | N/A | Covered by the abstraction when they land |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.DPIA` | 8849 | The last free id of the expiration-monitoring block (8840-8849) inside the registered range 8800-8899; no registry change |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (modified) | 6 | `DPIAReviewReminderService.cs`, `DPIAOptions.cs`, `DPIAOptionsValidator.cs`, `Diagnostics/DPIALogMessages.cs`, `Diagnostics/DPIADiagnostics.cs`, `Encina.Compliance.DPIA.csproj` |
| PublicAPI and manifest | 2 | `PublicAPI.Unshipped.txt`, `Encina.Compliance.DPIA.json` |
| Tests | 3 | Service tests, validator tests, DI test |
| Justification files | 3 | Integration, load, benchmark |
| Documentation and changelog | 2 | `docs/features/dpia.md`, fragment |
| **Total** | **~16** | Production and manifest fit one PR; the three justification files may ship in the same PR if the limit allows, otherwise a second small PR |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #763: only one instance runs the DPIA review reminder cycle.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- Lock contract: ADR-052, implemented by #2667 before this work (Either<EncinaError, Option<ILockHandle>>).
- DPIAReviewReminderService is an internal BackgroundService on a PeriodicTimer; a cycle never throws out of the loop.

IMPLEMENTATION OVERVIEW:
1. DPIAOptions.LockTimeout (30 s) with a validator rule when monitoring is on; project reference to
   Encina.DistributedLock.
2. In the service: resolve IDistributedLockProvider optionally per cycle; TryAcquireAsync("encina:dpia:review-reminder",
   LockTimeout, 0, 0, ct); Held runs the cycle under await using and awaits the notification publishing inside it;
   Right(None) skips (Debug log 8849, counter outcome skipped); Left fails the cycle by error code; no provider
   runs unlocked.
3. Observability: outcome value skipped on the existing counter, activity tag dpia.lock.outcome, message 8849.
4. Tests: unit (acquire, skip, Left, no provider, cancellation, order of publish and release, two real services over
   the InMemory provider), validator, DI proof; justification files for integration, load, benchmark; docs through
   docs-writer; changelog, PublicAPI and manifest through mechanical-fixer.

KEY PATTERNS:
- Never log EncinaError.Message; error code only; exceptions through ForLogging().
- Methods small enough for CRAP <= 10; the lock decision separate from the cycle body.
- Do not change the error handling of the notification publishing (issue #3502).

REFERENCE FILES:
- docs/plans/dpia-reminder-lock-implementation-plan-763.md
- src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- .github/coverage-manifest/Encina.Compliance.DPIA.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A lock attempt is instant; nothing to cache |
| 2 | OpenTelemetry | ✅ | Phase 4: outcome `skipped` on the existing cycle counter and a `dpia.lock.outcome` activity tag |
| 3 | Structured Logging | ✅ | Phase 4: one message, EventId 8849, inside the registered DPIA range |
| 4 | Health Checks | ❌ | The existing DPIA health check covers service state; lock providers have their own (#755) |
| 5 | Validation | ✅ | Phase 1: `LockTimeout` must be positive when monitoring is on |
| 6 | Resilience | ❌ | A failed acquisition fails the cycle and the next tick retries; no policy is added |
| 7 | Distributed Locks | ✅ | This is the integration (Phase 2) |
| 8 | Transactions | ❌ | No multi-store atomicity |
| 9 | Idempotency | ❌ | The lock prevents duplicate runs; publishing stays at-least-once (the dropped `Left` is #3502) |
| 10 | Multi-Tenancy | ❌ | The service checks all assessments; the lock is global and stays global under the #741 decorator with its recommended default |
| 11 | Module Isolation | ❌ | Application-level service with no module data |
| 12 | Audit Trail | ❌ | Lock outcomes are operational, not audit events |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#2667](https://github.com/dlrivada/Encina/issues/2667) — lock providers return `Either` (ADR-052) | Open, implementation issue (no pending maintainer decision) | Phase 2 starts after it merges; Phases 1, 3 and 4 can start earlier |
| [#3502](https://github.com/dlrivada/Encina/issues/3502) — publishing is fire-and-forget and drops the `Left` | Open | This plan only awaits the publishing inside the lock; the error handling is that issue's scope. If #3502 lands first, rebase Phase 2 task 4 onto its result |
| [#768](https://github.com/dlrivada/Encina/issues/768) — migrate the service to `Encina.Scheduling` | Open | Dilemma 2; would remove the lock call |
| [#3270](https://github.com/dlrivada/Encina/issues/3270) — remove `EnableExpirationMonitoring` | Open | The lock code does not depend on the flag; the validator rule changes with it |
| [#223](https://github.com/dlrivada/Encina/issues/223) — automatic lock extension | Open | Removes the fixed-expiry limit of Dilemma 1 later |

---

## Next Steps

1. Link this plan from issue #763 and start the brief after [#2667](https://github.com/dlrivada/Encina/issues/2667) merges.
2. When work on the issue starts, ask the maintainer to answer Dilemmas 1 and 2 (fixed lock expiry; lock now or wait for #768), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices.
3. One commit per phase; the last commit references `Fixes #763`.
