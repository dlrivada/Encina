# Implementation Plan: Health Check for the InMemory Distributed Lock Provider

> **Issue**: [#755](https://github.com/dlrivada/Encina/issues/755)
> **Type**: Feature
> **Complexity**: Low (4 phases, 14 files)
> **Estimated Scope**: about 120 lines of production code, about 250 lines of tests
> **Milestone**: Encina 1.0 (health checks of the lock category, SPEC-000 DEC-009)

---

## Summary

Add `InMemoryDistributedLockHealthCheck` to `Encina.DistributedLock.InMemory` and register it from `AddEncinaDistributedLockInMemory` when `ProviderHealthCheck.Enabled` is true, the way the Redis (`RedisDistributedLockHealthCheck`) and SqlServer (`SqlServerDistributedLockHealthCheck`) lock providers do. The check proves the lock mechanism, not only that the provider exists: it acquires and releases a probe resource through `IDistributedLockProvider`, bounded by `ProviderHealthCheckOptions.Timeout`, and reports only the exception type on failure.

**Affected packages:**

- `Encina.DistributedLock.InMemory` - the check, its log class, registration, option defaults, a new package README (none exists today), `PublicAPI.Unshipped.txt`.
- `tests/Encina.ContractTests` - one `ProjectReference` to `Encina.DistributedLock.InMemory` (the project has no lock reference today).
- `Encina.DistributedLock` and `Encina.Messaging` - dependencies only. `Encina.DistributedLock` already references `Encina.Messaging` (`DistributedLockOptions.ProviderHealthCheck` is a `ProviderHealthCheckOptions`), so the InMemory project reaches `IEncinaHealthCheck` through it and needs no new `ProjectReference`.

**Provider category**: Distributed locks (AGENTS.md section 5). Exactly 7 providers are in scope for 1.0: InMemory, Redis, SqlServer, PostgreSQL, MySQL, Azure Blob, DynamoDB. Today Redis and SqlServer have a check, InMemory gets it here, and the other four providers do not exist in `src/` yet (#207, #208, #209, #210): their checks ship with those provider issues and this plan leaves the pattern for them (Design Choice 5).

**Out of scope**: `MemoryDistributedLockProvider` in `Encina.Caching.Memory` is a second in-process lock provider registered by `AddEncinaMemoryCache`; it is not one of the 7 providers of the category and is not covered here (Design Choice 6). The cache-side checks are #754; the umbrella is #622.

**Sibling plans (health-checks family)**: #754 owns every change to the shared `ProviderHealthCheckOptions` type; this plan uses it unchanged. #622 owns the transport checks; #454 owns the probe endpoints that select these checks by tag. Options wiring follows the same story as #754 and #622 (recommended, pending the maintainer): each check receives the `ProviderHealthCheckOptions` instance of its own registration, and the type itself is never registered as a service (Design Choice 4).

---

## Design Choices

<details>
<summary><strong>1. Probe depth of the InMemory check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Acquire a probe lock through `IDistributedLockProvider`, check `IsLockedAsync` is true while it is held, then release it** | Proves the mechanism; same depth as the Redis check (sets and deletes a test key) and the acceptance criterion "lock mechanism verified (not just connectivity)" | Touches shared in-process lock state for an instant (a unique probe resource name) |
| **B) Only confirm the singleton resolves from DI** | No side effect on lock state | Proves nothing about locking; a broken lock implementation reports Healthy |

### Chosen Option: **A - acquire, verify `IsLockedAsync`, release** (recommended, pending the maintainer)

### Rationale

- A unique probe resource per call (`health:lock:{Guid:N}`, as the Redis check names its key) cannot contend with an application resource.
- Option B wins only when the check exists to show that the registration worked; the issue asks for the mechanism.
- `IsLockedAsync` returning false while the handle is held is reported as Unhealthy: that is a new semantic (the Redis check does not read the lock back) and belongs to this Dilemma.
- Class: Dilemma
- The issue itself lists this as its Dilemma D1, so the plan records it for the maintainer.

</details>

<details>
<summary><strong>2. Base type of the check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Derive from `EncinaHealthCheck`** | The base class catches every exception and returns only its type name (`Health check failed: <Type>`); the `encina` tag is always present; `RedisHealthCheck` and `NATSHealthCheck` do this | The sibling lock checks implement `IEncinaHealthCheck` directly, so the lock family would not look uniform |
| **B) Implement `IEncinaHealthCheck` directly** | Uniform with `RedisDistributedLockHealthCheck` and `SqlServerDistributedLockHealthCheck` | Re-implements the try/catch and the no-message rule in every check |

### Chosen Option: **A - derive from `EncinaHealthCheck`** (recommended, pending the maintainer)

### Rationale

- The base class carries the "no exception message in the result" rule (#1259), so a new check cannot forget it.
- Option B also satisfies AGENTS.md section 3 (an own try/catch can return the type name only) and matches the two sibling lock checks, so the cited rule does not decide between them; the convention points both ways (`RedisHealthCheck` derives from the base, the lock checks implement the interface).
- Class: Dilemma

</details>

<details>
<summary><strong>3. Default tags and name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The InMemory options set their own tags** (`InMemoryLockOptions` assigns `ProviderHealthCheck.Tags` to `encina`, `distributed-lock`, `inmemory`, `ready` in its constructor) | Local to the package; probes can filter by `distributed-lock` | Other lock providers keep the misleading shared default until each fixes itself |
| **B) Change the shared default of `ProviderHealthCheckOptions.Tags`** | One change fixes every provider | The shared default is `["encina", "database", "ready"]` today, so every Redis cache, NATS or lock check registered through the options carries the tag `database`; changing it alters the tag filters of every check built from the shared options and belongs to the owner of the shared type (#754) |

### Chosen Option: **A - the InMemory options set their own tags** (recommended, pending the maintainer)

### Rationale

- Verified in code: `RedisDistributedLockHealthCheck.Tags` returns `_options.Tags ?? [...]`, and the options object is never null with a non-null default, so the "redis, distributed-lock" fallback never applies and the check is tagged `database`.
- Option B wins when the maintainer wants all providers relabelled in one step; #754 raises that for the cache category and this plan follows its answer.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Registration and options wiring (per-registration options)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Register the check with a factory that captures the `ProviderHealthCheckOptions` of this registration** (`services.AddSingleton<IEncinaHealthCheck>(sp => new InMemoryDistributedLockHealthCheck(provider, options.ProviderHealthCheck, ...))`), gated by `options.ProviderHealthCheck.Enabled` at registration time; the shared type is NOT registered as a service | Each check keeps its own options, whatever other providers are registered; the check only runs when the application calls `AddEncinaHealthChecks` (the composite resolves `IEnumerable<IEncinaHealthCheck>`) | Differs from Redis and SqlServer today (`ServiceCollectionExtensions.cs` of each registers `AddSingleton(options.ProviderHealthCheck)`, a type-keyed registration where the last registration wins and every check constructor receives the same instance) |
| **B) Copy the Redis shape: `AddSingleton(options.ProviderHealthCheck)` plus `AddSingleton<IEncinaHealthCheck, ...>`** | Identical to Redis (line 133 and lines 141-148 of its `ServiceCollectionExtensions.cs`) and SqlServer (lines 104-110) | Cross-wires options: with two providers registered (for example this check and the Redis lock or a cache check) both resolve the last `ProviderHealthCheckOptions`, so one provider's `Name`, `Tags`, `Timeout` or `Enabled` apply to the other |
| **C) Named options or keyed services** (`AddKeyedSingleton<ProviderHealthCheckOptions>("distributed-lock-inmemory", ...)`) | Container-visible per-provider options; configurable later | More surface for one check; two patterns in one family unless every provider moves together |

### Chosen Option: **A - per-registration options by capture** (recommended, pending the maintainer)

### Rationale

- A check that takes its options from the registration that created it cannot be cross-wired by registration order, which Option B allows (#754 and #622 use the same story for the cache and transport checks).
- Nothing runs unless the host opts in with `AddEncinaHealthChecks`, so pay-for-what-you-use holds.
- The factory also resolves an optional `TimeProvider` (`sp.GetService<TimeProvider>() ?? TimeProvider.System`) and an optional `ILogger<InMemoryDistributedLockHealthCheck>` (`GetService`, no new registration needed because the provider takes `ILogger<>` from the container the same way); `AddEncinaDistributedLockInMemoryCore` does not register a `TimeProvider` today (the Redis core does with `TryAddSingleton(TimeProvider.System)`), so the plan does not add one: the application's own `TimeProvider` is used when present.
- Moving Redis and SqlServer to the same wiring is out of scope here: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/lock-healthcheck-options-wiring.md).
- Proven by a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` (AGENTS.md section 3, registration completeness) and by a test that registers another `ProviderHealthCheckOptions` instance with a different `Name` and asserts the InMemory check still reports its own.
- Class: Dilemma

</details>

<details>
<summary><strong>5. PostgreSQL, MySQL, Azure Blob and DynamoDB checks</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each check ships with its provider issue** (#207, #208, #209, #210) | The check is written against a provider that exists and can be integration tested | The category stays incomplete until those issues land |
| **B) Write abstract check scaffolding now** | Gives future providers a base | A base with one user is speculation; the Redis and SqlServer checks already share nothing but the options type |

### Chosen Option: **A - the checks ship with their providers** (recommended, pending the maintainer)

### Rationale

- None of the four providers exists under `src/`; the lock projects are `Encina.DistributedLock`, `.InMemory`, `.Redis` and `.SqlServer`.
- The "framework ready" acceptance item is met by the reusable pieces already in place: `DistributedLockOptions.ProviderHealthCheck` and the registration shape documented in the README by Phase 4.
- Option B (scaffolding now) is not ruled out by AGENTS.md section 5 (it only says a lock feature is complete when the 7 providers are covered); only the issue text and scope choose A, so the maintainer confirms.
- Class: Dilemma

</details>

<details>
<summary><strong>6. The lock provider inside Encina.Caching.Memory</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Leave `MemoryDistributedLockProvider` out** | Matches the category list; keeps the PR small | An application that uses only `AddEncinaMemoryCache` has a lock provider without a check |
| **B) Add a second check in `Encina.Caching.Memory`** | Covers every in-process lock | The package is not in the 7-provider list; duplicates this check |

### Chosen Option: **A - leave it out** (recommended, pending the maintainer)

### Rationale

- It is a convenience registration of the memory cache package, not a lock provider of the category.
- AGENTS.md section 5 and SPEC-000 DEC-009 list exactly the 7 providers, but they do not forbid a second check for another in-process lock (Option B), so the choice is the maintainer's.
- Class: Dilemma

</details>

<details>
<summary><strong>7. Public name of the check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `encina-distributed-lock-inmemory`** (`encina-<provider>` convention of the options documentation) | Consistent with the `encina-` prefix of the other Encina checks | The Redis lock check uses `redis-distributed-lock`, so the lock family is not uniform |
| **B) `inmemory-distributed-lock`** (the Redis lock shape) | Uniform inside the lock family | Differs from the documented `encina-<provider>` convention |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- The name is public (`DefaultName`) and is what probes and dashboards show; a configured `ProviderHealthCheckOptions.Name` overrides it.
- Class: Dilemma

</details>

<details>
<summary><strong>8. Probe parameters, failure status and constructor shape</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Failure status is always `Unhealthy`; probe uses `expiry = options.Timeout`, `wait = TimeSpan.Zero`, `retry = TimeSpan.FromMilliseconds(10)`; constructor `(provider, options, timeProvider = null, logger = null)`** | A leaked probe handle expires on its own after the check's own bound; zero wait never blocks; simple | `ProviderHealthCheckOptions.FailureStatus` is ignored |
| **B) Report `options.FailureStatus` (Unhealthy or Degraded) on failure** | Honors the documented option; a host can mark a lock problem Degraded | The base class returns `Unhealthy` for every exception, so honoring it needs a mapping in the check |

### Chosen Option: **A for the probe parameters and constructor; B for the failure status is the open question** (recommended, pending the maintainer)

### Rationale

- Probe values are internal and not observable except through the timeout and the expiry of a leaked handle: Trivial (no public surface, no citation needed beyond the unique probe key of Design Choice 1); with `wait = TimeSpan.Zero` the provider still attempts once (`InMemoryDistributedLockProvider.TryAcquireAsync` always tries before it checks the deadline), so `retry` is never used and any positive value is equivalent.
- The optional `TimeProvider` follows AGENTS.md section 3 (time comes from `TimeProvider`, optional parameter defaulting to `TimeProvider.System`): Trivial. The optional `ILogger` parameter (default `NullLogger`) widens the public constructor and the PublicAPI line: Trivial, as the Redis lock and cache checks take no logger today only because they log nothing.
- `FailureStatus` is a user-visible semantic (a lock failure is Unhealthy or Degraded in the probe result), so the choice of ignoring it (A) or honoring it (B) is a Dilemma.
- Class: Dilemma (failure status); Trivial (probe values, constructor shape)

</details>

---

## Implementation Phases

### Phase 1: The check and its registration

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.DistributedLock.InMemory/Health/InMemoryDistributedLockHealthCheck.cs`
   - `public sealed class InMemoryDistributedLockHealthCheck : EncinaHealthCheck` in namespace `Encina.DistributedLock.InMemory.Health`.
   - `public const string DefaultName = "encina-distributed-lock-inmemory";` (Design Choice 7).
   - Constructor `(IDistributedLockProvider provider, ProviderHealthCheckOptions options, TimeProvider? timeProvider = null, ILogger<InMemoryDistributedLockHealthCheck>? logger = null)` (Design Choice 8): `ArgumentNullException.ThrowIfNull` on `provider` and `options`; `base(options.Name ?? DefaultName, options.Tags)`; `timeProvider ?? TimeProvider.System`; `logger ?? NullLogger<...>.Instance`.
   - `protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken ct)`: create the timeout source with `timeProvider.CreateCancellationTokenSource(options.Timeout)` (a system timer would not follow a `FakeTimeProvider`; AGENTS.md section 3) and link it with `ct` through `CancellationTokenSource.CreateLinkedTokenSource`, both disposed in a `using`; `TryAcquireAsync("health:lock:" + Guid.NewGuid().ToString("N"), expiry: options.Timeout, wait: TimeSpan.Zero, retry: TimeSpan.FromMilliseconds(10), token)`; a null handle returns `Unhealthy("<Name> could not acquire the probe lock")`; otherwise `await using` the handle, then `IsLockedAsync` must be true while held; Healthy on success; an `OperationCanceledException` raised while `ct` is not cancelled (the timeout fired) returns `Unhealthy("<Name> health check timed out after <n>s")`, and the handler disposes both sources so no timer leaks.
   - No exception message and no `EncinaError` text in any result.
2. **Modify** `src/Encina.DistributedLock.InMemory/InMemoryLockOptions.cs`: constructor assigns `ProviderHealthCheck.Tags = ["encina", "distributed-lock", "inmemory", "ready"]` (Design Choice 3).
3. **Modify** `src/Encina.DistributedLock.InMemory/ServiceCollectionExtensions.cs`: add `internal static IServiceCollection RegisterHealthCheck(this IServiceCollection services, InMemoryLockOptions options)` and call it from `AddEncinaDistributedLockInMemoryCore` after the provider registration; when `options.ProviderHealthCheck.Enabled`, it adds the `IEncinaHealthCheck` factory of Design Choice 4 (captures `options.ProviderHealthCheck`; no `AddSingleton(ProviderHealthCheckOptions)`).
4. **Modify** `src/Encina.DistributedLock.InMemory/PublicAPI.Unshipped.txt`: the class, its four-parameter constructor (with `TimeProvider? timeProvider = null` and `ILogger<...>? logger = null`), `DefaultName`.

The public lines for the PublicAPI file and the coverage manifest are written by `mechanical-fixer` (decided by the worker).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library. Issue #755 adds a health check to the InMemory distributed lock provider.
Encina.DistributedLock.InMemory references Encina.DistributedLock, which references Encina.Messaging, so
EncinaHealthCheck and ProviderHealthCheckOptions (namespace Encina.Messaging.Health) are available without a
new ProjectReference.

TASK:
1. Create src/Encina.DistributedLock.InMemory/Health/InMemoryDistributedLockHealthCheck.cs as described in the plan.
2. Set the default tags in InMemoryLockOptions.
3. Register the check in AddEncinaDistributedLockInMemoryCore when options.ProviderHealthCheck.Enabled.
4. Build with dotnet build --configuration Release and fix every RS0016 by adding the symbol to PublicAPI.Unshipped.txt.

KEY RULES:
- Derive from EncinaHealthCheck; a failure result carries only the exception type name, never a message.
- A probe resource name is unique per call; release the handle in a finally/await using.
- The timeout source comes from TimeProvider.CreateCancellationTokenSource(options.Timeout), never new CancellationTokenSource(timeout); the timeout value comes from ProviderHealthCheckOptions.Timeout.
- Register the check with a factory that captures this registration's ProviderHealthCheckOptions; never AddSingleton(ProviderHealthCheckOptions).
- XML docs with an example on the public class; sealed class; nullable enabled; zero warnings.
- Do not change ProviderHealthCheckOptions (owned by #754).

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/Health/RedisDistributedLockHealthCheck.cs
- src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs (RegisterHealthCheck)
- src/Encina.Caching.Redis/Health/RedisHealthCheck.cs (timeout-linked token)
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- src/Encina.DistributedLock/DistributedLockOptions.cs (line 49)
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
```

</details>

### Phase 2: Observability and structured logging

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.DistributedLock.InMemory/Health/InMemoryDistributedLockHealthLog.cs`: an `internal static partial class` with `ProbeFailed(ILogger, Exception exception)` (EventId 3704; the caller passes `ex.ForLogging()`, so only the type and stack trace are logged) and `ProbeTimedOut(ILogger, double timeoutSeconds)` (EventId 3705), both inside the registered range `DistributedLockInMemory` (3700-3749; the provider's own `[LoggerMessage]` methods in `InMemoryDistributedLockProvider.cs` use 3700-3703). No new range is registered.
2. The optional `ILogger<InMemoryDistributedLockHealthCheck>` constructor parameter is part of Phase 1 (Design Choice 8). Logging paths: `EncinaHealthCheck.CheckHealthAsync` already catches every exception and returns the type name without logging, so the check logs in two places only. (a) The timeout path of `CheckHealthCoreAsync` calls `ProbeTimedOut`. (b) A `catch (Exception ex) when (ex is not OperationCanceledException)` inside `CheckHealthCoreAsync` calls `ProbeFailed(logger, ex.ForLogging())` and rethrows with `throw;`, so the base class still produces the type-name result. A null handle (the probe resource was already held) is a result, not an exception, and logs nothing.
3. No new metric or span: the existing health-check telemetry belongs to the OpenTelemetry package (matrix row 2).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
The InMemory lock package already owns EventIds 3700-3703 inside EventIdRanges.DistributedLockInMemory (3700-3749).
Phase 1 created InMemoryDistributedLockHealthCheck.

TASK:
Create the internal partial log class InMemoryDistributedLockHealthLog with two [LoggerMessage] methods (EventIds
3704 ProbeFailed(ILogger, Exception) and 3705 ProbeTimedOut(ILogger, double)) and call them from the check: the
timeout path logs ProbeTimedOut; a non-cancellation exception logs ProbeFailed(logger, ex.ForLogging()) and is
rethrown so EncinaHealthCheck returns the type-name result.

KEY RULES:
- Use the source generator, not LoggerMessage.Define; EventIds packed sequentially after 3703.
- Log the exception type and ForLogging() output, never ex.Message and never EncinaError.Message.
- No new EventId range is registered; the test EncinaEventIdAllocationTests must still pass.

REFERENCE FILES:
- src/Encina.DistributedLock.InMemory/ (existing log class)
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 3: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. `tests/Encina.UnitTests/DistributedLock/InMemory/InMemoryDistributedLockHealthCheckTests.cs` (new folder `InMemory`; the existing InMemory unit tests sit directly in `DistributedLock/`): healthy (real `InMemoryDistributedLockProvider` on a `FakeTimeProvider`), unhealthy when acquisition returns null (a resource already held), unhealthy when `IsLockedAsync` reports false while held (substitute provider), timeout (the `FakeTimeProvider` is advanced past `Timeout` while a substitute provider blocks; deterministic because the source comes from the `TimeProvider`), a non-cancellation exception is logged through `ForLogging()` and the result carries the type name only, result text carries no exception message, tags and name from the options, probe lock released after the check.
2. `tests/Encina.UnitTests/DistributedLock/InMemory/InMemoryRegistrationTests.cs`: builds the provider with `ValidateOnBuild` and `ValidateScopes`, asserts exactly one `IEncinaHealthCheck` when enabled, none when `Enabled = false`, and that with another `ProviderHealthCheckOptions` instance (different `Name`) registered in the same collection the InMemory check still reports its own name and tags (no cross-wiring, Design Choice 4).
3. `tests/Encina.GuardTests/Infrastructure/DistributedLock/InMemoryDistributedLockHealthCheckGuardTests.cs`: the constructor (null provider, null options; `timeProvider` and `logger` null are valid and produce a working check).
4. `tests/Encina.ContractTests/DistributedLock/InMemoryDistributedLockHealthCheckContractTests.cs` (new file: no lock health-check contract file exists in `Encina.ContractTests`, which has no lock project reference, so the plan adds the `ProjectReference` to `Encina.DistributedLock.InMemory` in `tests/Encina.ContractTests/Encina.ContractTests.csproj`). Scope: InMemory only, with the real provider, asserting the `IEncinaHealthCheck` contract (non-empty name, tags include `encina`, a result and no throw on a cancelled token). The Redis and SqlServer checks need a multiplexer or a SQL connection and are not compared here.
5. Property, integration, load and benchmark: not required (deterministic in-process logic, no external service, single probe); the manifest `reason` for each states this. No `.md` justification is written for the contract flag; the contract test above exists.
6. `mechanical-fixer` updates `.github/coverage-manifest/Encina.DistributedLock.InMemory.json` with per-file targets and one-sentence justifications for all four files, in this PR with the tests that meet them (the measured value replaces each estimate before the PR):

   | File | Unit | Guard | Contract | Justification |
   |------|-----:|------:|---------:|---------------|
   | `Health/InMemoryDistributedLockHealthCheck.cs` | 90 | 15 | 40 | Unit tests drive success, null handle, not-locked, timeout and exception paths; guard tests reach only the constructor checks; the contract test runs the probe once. |
   | `Health/InMemoryDistributedLockHealthLog.cs` | 80 | 0 | 0 | Generated `[LoggerMessage]` bodies run only when the check logs (unit); guard and contract tests never reach a logging path, so 0 for both with that reason. |
   | `InMemoryLockOptions.cs` | 90 | 0 | 0 | The constructor that sets the default tags runs in every unit test that builds the options; guard tests have no parameters to check here. |
   | `ServiceCollectionExtensions.cs` | 85 | 15 | 0 | The registration tests run the enabled and disabled branches; guard tests reach the existing null checks; no contract test touches registration. |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phase 1 and 2 are done. Tests must execute the real InMemoryDistributedLockProvider; reflection-only tests cover nothing.

TASK:
Write the unit, guard and contract tests listed in the plan, then measure per-flag coverage of the new file with
dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" and the coverage-report script, and report it.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; FakeTimeProvider for time; AAA; descriptive names.
- No Thread.Sleep; no per-class database fixtures (not applicable here).
- CRAP of every added method must be 10 or less; keep CheckHealthCoreAsync small.

REFERENCE FILES:
- tests/Encina.UnitTests/DistributedLock/Redis/RedisDistributedLockHealthCheckTests.cs
- tests/Encina.UnitTests/Caching/Redis/Health/RedisHealthCheckTests.cs
- .github/coverage-manifest/Encina.DistributedLock.InMemory.json
```

</details>

### Phase 4: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. `docs-writer`: create `src/Encina.DistributedLock.InMemory/README.md` (the package has none today: what the provider is, how to enable the check, tags, the per-registration options shape later providers copy) and update the lock section of `docs/guides/health-checks.md` (the file exists).
2. `mechanical-fixer`: changelog fragment `changelog.d/755-inmemory-lock-health-check.added.md`; the `PublicAPI.Unshipped.txt` lines.
3. Verify: `dotnet build --configuration Release` (0 warnings), `dotnet test`, per-flag coverage against the manifest.
4. Open the follow-up issue only if the maintainer chooses Design Choice 3 Option B (its issue number is assigned when it opens; none exists today).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The check and tests exist. Documentation, changelog and manifests remain.

TASK:
Update the InMemory lock README and the health-checks guide, add the changelog fragment, run the build and the tests.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers only).
- English only; no emojis; no private application names.
- Never edit CHANGELOG.md; use changelog.d/.

REFERENCE FILES:
- changelog.d/README.md
- docs/guides/health-checks.md
- src/Encina.DistributedLock.InMemory/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Results never carry an error or exception message; registration completeness proven by a DI test |
| AGENTS.md section 5 (distributed locks) | The 7 providers of 1.0; a lock feature is complete when all are covered |
| AGENTS.md section 6, function 4 | A checkable dependency exposes a health check |
| SPEC-000 DEC-009 | The exact lock set for 1.0 |
| ASP.NET Core health checks (`Microsoft.Extensions.Diagnostics.HealthChecks`) | The adapter in `Encina.AspNetCore` turns `IEncinaHealthCheck` into `IHealthCheck` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `EncinaHealthCheck`, `ProviderHealthCheckOptions`, `HealthCheckResult` | `src/Encina.Messaging/Health/` | Base type, options (Enabled, Timeout, Name, Tags, FailureStatus), result |
| `RedisDistributedLockHealthCheck` | `src/Encina.DistributedLock.Redis/Health/` | Reference for probe shape and registration |
| `SqlServerDistributedLockHealthCheck` | `src/Encina.DistributedLock.SqlServer/Health/` | Second reference |
| `RedisHealthCheck` | `src/Encina.Caching.Redis/Health/RedisHealthCheck.cs` | Timeout-linked cancellation pattern |
| `IDistributedLockProvider` | `src/Encina.DistributedLock/IDistributedLockProvider.cs` | `TryAcquireAsync`, `IsLockedAsync` used by the probe |
| `DistributedLockOptions.ProviderHealthCheck` | `src/Encina.DistributedLock/DistributedLockOptions.cs` (line 49) | Inherited by `InMemoryLockOptions` |
| `InMemoryDistributedLockProvider` | `src/Encina.DistributedLock.InMemory/` | Real provider used by the probe and the tests |
| `EncinaHealthCheckAdapter`, `AddEncinaHealthChecks` | `src/Encina.AspNetCore/Health/` | How the host picks the check up |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DistributedLock.InMemory` | 3700-3749 (existing `DistributedLockInMemory`) | 3700-3703 used; this plan takes 3704-3705; no new range |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 2 | 2 | New check and its log class; `ServiceCollectionExtensions.cs`, `InMemoryLockOptions.cs` |
| Tests | 4 | 1 | Unit tests, registration test, guard tests, contract test (all new); `Encina.ContractTests.csproj` (project reference) |
| Documentation | 1 | 1 | New package README, `docs/guides/health-checks.md` |
| Configuration | 1 | 2 | Changelog fragment; `PublicAPI.Unshipped.txt`, coverage manifest |
| **Total** | **8** | **6** | One pull request, 14 files, under the 15-file limit |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #755 adds a health check to the
InMemory distributed lock provider. Redis and SqlServer lock providers already have one; PostgreSQL, MySQL, Azure
Blob and DynamoDB providers do not exist yet and ship their own checks.

IMPLEMENTATION OVERVIEW:
1. InMemoryDistributedLockHealthCheck (EncinaHealthCheck) probing acquire/release through IDistributedLockProvider.
2. InMemoryLockOptions sets its own tags; AddEncinaDistributedLockInMemoryCore registers the check when enabled.
3. Two [LoggerMessage] methods, EventIds 3704-3705, inside the existing range 3700-3749.
4. Unit, guard and contract tests, plus a DI test with ValidateOnBuild and ValidateScopes.
5. README, guide, changelog fragment, PublicAPI lines, coverage manifest (specialists write the documentation and the mechanical files).

KEY PATTERNS:
- Failure results carry the exception type name only; timeouts come from ProviderHealthCheckOptions.Timeout.
- Registration is gated by ProviderHealthCheck.Enabled and mirrors RegisterHealthCheck in the Redis lock package.
- Do not modify ProviderHealthCheckOptions (owned by #754).

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/Health/RedisDistributedLockHealthCheck.cs
- src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs
- src/Encina.DistributedLock.InMemory/ServiceCollectionExtensions.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
- .github/coverage-manifest/Encina.DistributedLock.InMemory.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A probe must reflect real-time state; caching it would mislead |
| 2 | OpenTelemetry | ⏭️ Defer | Health-check metrics and spans are a single cross-provider concern owned by the probe work in #454 (`encina.health.probe_status`); no new metric here |
| 3 | Structured Logging | ✅ Include | Phase 2: EventIds 3704-3705 in the existing InMemory range, exception type only |
| 4 | Health Checks | ✅ Include | This is the feature |
| 5 | Validation | ❌ N/A | No input |
| 6 | Resilience | ✅ Include | The probe is bounded by `ProviderHealthCheckOptions.Timeout` |
| 7 | Distributed Locks | ✅ Include | The probe uses the lock provider itself |
| 8 | Transactions | ❌ N/A | No data mutation |
| 9 | Idempotency | ❌ N/A | A probe is repeatable by design |
| 10 | Multi-Tenancy | ❌ N/A | Process-scoped state |
| 11 | Module Isolation | ❌ N/A | Not module scoped |
| 12 | Audit Trail | ❌ N/A | Infrastructure probe, no compliance event |

**Deferred items:**

- OpenTelemetry for health checks: tracked with the probe work in #454 and the umbrella #622.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `EncinaHealthCheck` and `ProviderHealthCheckOptions` | ✅ Available | `src/Encina.Messaging/Health/` |
| `DistributedLockOptions.ProviderHealthCheck` | ✅ Available | Inherited by `InMemoryLockOptions` |
| EventId range 3700-3749 | ✅ Available | 3704-3749 free |
| Other lock providers | ⏭️ Later | #207, #208, #209, #210 deliver their own checks |
| Shared tag default (`database`) | Observation | Redis and SqlServer lock checks and the Redis cache check carry the `database` tag today; the answer to Design Choice 3 and #754 decides whether it is fixed centrally |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator briefs a worker for one pull request (14 files) once the maintainer has answered the Dilemmas (Design Choices 1 to 8, in particular 1, 3, 4 and 8).
2. After merge, comment on #755 with the result and note on #207, #208, #209 and #210 that their providers must ship a check following this shape.
3. Follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/redis-lock-healthcheck-timeout.md): `RedisDistributedLockHealthCheck` ignores `ProviderHealthCheckOptions.Timeout` today (found while reading the code).
4. Follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/lock-healthcheck-options-wiring.md): move the Redis and SqlServer lock checks to per-registration options (Design Choice 4).
