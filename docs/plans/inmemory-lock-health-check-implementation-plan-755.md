# Implementation Plan: Health Check for the InMemory Distributed Lock Provider

> **Issue**: [#755](https://github.com/dlrivada/Encina/issues/755)
> **Type**: Feature
> **Complexity**: Low (4 phases, about 12 files)
> **Estimated Scope**: about 120 lines of production code, about 250 lines of tests
> **Milestone**: Encina 1.0 (health checks of the lock category, SPEC-000 DEC-009)

---

## Summary

Add `InMemoryDistributedLockHealthCheck` to `Encina.DistributedLock.InMemory` and register it from `AddEncinaDistributedLockInMemory` when `ProviderHealthCheck.Enabled` is true, the way the Redis (`RedisDistributedLockHealthCheck`) and SqlServer (`SqlServerDistributedLockHealthCheck`) lock providers do. The check proves the lock mechanism, not only that the provider exists: it acquires and releases a probe resource through `IDistributedLockProvider`, bounded by `ProviderHealthCheckOptions.Timeout`, and reports only the exception type on failure.

**Affected packages:**

- `Encina.DistributedLock.InMemory` - the check, registration, option defaults, README, `PublicAPI.Unshipped.txt`.
- `Encina.DistributedLock` and `Encina.Messaging` - dependencies only. `Encina.DistributedLock` already references `Encina.Messaging` (`DistributedLockOptions.ProviderHealthCheck` is a `ProviderHealthCheckOptions`), so the InMemory project reaches `IEncinaHealthCheck` through it and needs no new `ProjectReference`.

**Provider category**: Distributed locks (AGENTS.md section 5). Exactly 7 providers are in scope for 1.0: InMemory, Redis, SqlServer, PostgreSQL, MySQL, Azure Blob, DynamoDB. Today Redis and SqlServer have a check, InMemory gets it here, and the other four providers do not exist in `src/` yet (#207, #208, #209, #210): their checks ship with those provider issues and this plan leaves the pattern for them (Design Choice 5).

**Out of scope**: `MemoryDistributedLockProvider` in `Encina.Caching.Memory` is a second in-process lock provider registered by `AddEncinaMemoryCache`; it is not one of the 7 providers of the category and is not covered here (Design Choice 6). The cache-side checks are #754; the umbrella is #622.

**Sibling plans (health-checks family)**: #754 owns every change to the shared `ProviderHealthCheckOptions` type; this plan uses it unchanged. #622 owns the transport checks; #454 owns the probe endpoints that select these checks by tag.

---

## Design Choices

<details>
<summary><strong>1. Probe depth of the InMemory check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Acquire and release a probe lock through `IDistributedLockProvider`** | Proves the mechanism; same depth as the Redis check (sets and deletes a test key) and the acceptance criterion "lock mechanism verified (not just connectivity)" | Touches shared in-process lock state for an instant (a unique probe resource name) |
| **B) Only confirm the singleton resolves from DI** | No side effect on lock state | Proves nothing about locking; a broken lock implementation reports Healthy |

### Chosen Option: **A - acquire and release a probe lock** (recommended, pending the maintainer)

### Rationale

- A unique probe resource per call (`health:lock:{Guid:N}`, as the Redis check names its key) cannot contend with an application resource.
- Option B wins only when the check exists to show that the registration worked; the issue asks for the mechanism.
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

### Chosen Option: **A - derive from `EncinaHealthCheck`**

### Rationale

- The base class carries the "no exception message in the result" rule (#1259), so a new check cannot forget it.
- Class: Trivial
- Settled by: AGENTS.md section 3 (`EncinaError.Message` never reaches health-check results) and the existing `src/Encina.Caching.Redis/Health/RedisHealthCheck.cs`.

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
<summary><strong>4. Registration, defaults and options lifetime</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Register inside `AddEncinaDistributedLockInMemoryCore` when `options.ProviderHealthCheck.Enabled`, with `AddSingleton(options.ProviderHealthCheck)` and `AddSingleton<IEncinaHealthCheck, ...>`** | Identical to Redis (`ServiceCollectionExtensions.cs` lines 133 and 141-148) and SqlServer (lines 104-110); the check only runs when the application calls `AddEncinaHealthChecks` (the composite resolves `IEnumerable<IEncinaHealthCheck>`) | `Enabled` defaults to true, so the registration is present by default |
| **B) Register through `IOptions<InMemoryLockOptions>` at resolution time** | Honors configuration applied later | The Redis precedent decides at registration time; two patterns in one family |

### Chosen Option: **A - registration-time gate like Redis and SqlServer**

### Rationale

- Nothing runs unless the host opts in with `AddEncinaHealthChecks`, so pay-for-what-you-use holds.
- The registration is proven by a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` (AGENTS.md section 3, registration completeness).
- Class: Trivial
- Settled by: AGENTS.md section 3 (registration completeness) and `src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs` (`RegisterHealthCheck`).

</details>

<details>
<summary><strong>5. PostgreSQL, MySQL, Azure Blob and DynamoDB checks</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each check ships with its provider issue** (#207, #208, #209, #210) | The check is written against a provider that exists and can be integration tested | The category stays incomplete until those issues land |
| **B) Write abstract check scaffolding now** | Gives future providers a base | A base with one user is speculation; the Redis and SqlServer checks already share nothing but the options type |

### Chosen Option: **A - the checks ship with their providers**

### Rationale

- None of the four providers exists under `src/`; the lock projects are `Encina.DistributedLock`, `.InMemory`, `.Redis` and `.SqlServer`.
- The "framework ready" acceptance item is met by the reusable pieces already in place: `DistributedLockOptions.ProviderHealthCheck` and the registration shape documented in the README by Phase 4.
- Class: Settled
- Settled by: AGENTS.md section 5 (distributed locks: a lock feature is complete when the 7 providers are covered) and #207, #208, #209, #210.

</details>

<details>
<summary><strong>6. The lock provider inside Encina.Caching.Memory</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Leave `MemoryDistributedLockProvider` out** | Matches the category list; keeps the PR small | An application that uses only `AddEncinaMemoryCache` has a lock provider without a check |
| **B) Add a second check in `Encina.Caching.Memory`** | Covers every in-process lock | The package is not in the 7-provider list; duplicates this check |

### Chosen Option: **A - leave it out**

### Rationale

- It is a convenience registration of the memory cache package, not a lock provider of the category.
- Class: Settled
- Settled by: AGENTS.md section 5 (the distributed-lock row lists exactly the 7 providers) and SPEC-000 DEC-009.

</details>

---

## Implementation Phases

### Phase 1: The check and its registration

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.DistributedLock.InMemory/Health/InMemoryDistributedLockHealthCheck.cs`
   - `public sealed class InMemoryDistributedLockHealthCheck : EncinaHealthCheck` in namespace `Encina.DistributedLock.InMemory.Health`.
   - `public const string DefaultName = "encina-distributed-lock-inmemory";`
   - Constructor `(IDistributedLockProvider provider, ProviderHealthCheckOptions options)`: `ArgumentNullException.ThrowIfNull` on both; `base(options.Name ?? DefaultName, options.Tags)`.
   - `protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken ct)`: link `ct` with a `CancellationTokenSource(options.Timeout)`; `TryAcquireAsync("health:lock:" + Guid.NewGuid().ToString("N"), expiry, wait: TimeSpan.Zero, retry, token)`; a null handle returns `Unhealthy("<Name> could not acquire the probe lock")`; otherwise `await using` the handle, then `IsLockedAsync` must be true while held; Healthy on success; a timeout returns `Unhealthy("<Name> health check timed out after <n>s")`.
   - No exception message and no `EncinaError` text in any result.
2. **Modify** `src/Encina.DistributedLock.InMemory/InMemoryLockOptions.cs`: constructor assigns `ProviderHealthCheck.Tags = ["encina", "distributed-lock", "inmemory", "ready"]` (Design Choice 3).
3. **Modify** `src/Encina.DistributedLock.InMemory/ServiceCollectionExtensions.cs`: add `internal static IServiceCollection RegisterHealthCheck(this IServiceCollection services, InMemoryLockOptions options)` and call it from `AddEncinaDistributedLockInMemoryCore` after the provider registration; body as in the Redis registration.
4. **Modify** `src/Encina.DistributedLock.InMemory/PublicAPI.Unshipped.txt`: the class, its constructor, `DefaultName`.

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
- Time comes from TimeProvider if any time arithmetic is needed; the timeout comes from ProviderHealthCheckOptions.Timeout.
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

1. **Create** `src/Encina.DistributedLock.InMemory/Health/InMemoryDistributedLockHealthLog.cs`: an `internal static partial class` with `HealthCheckFailed(ILogger, string failureType)` (EventId 3704) and `HealthCheckTimedOut(ILogger, double timeoutSeconds)` (EventId 3705), both inside the registered range `DistributedLockInMemory` (3700-3749; the provider's own `[LoggerMessage]` methods in `InMemoryDistributedLockProvider.cs` use 3700-3703). No new range is registered.
2. Inject `ILogger<InMemoryDistributedLockHealthCheck>` as an optional constructor parameter (default `NullLogger`), log by exception type only through `ForLogging()`.
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
3704 and 3705) and call them from the check on failure and on timeout.

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

1. `tests/Encina.UnitTests/DistributedLock/InMemory/InMemoryDistributedLockHealthCheckTests.cs`: healthy (real `InMemoryDistributedLockProvider` on a `FakeTimeProvider`), unhealthy when acquisition returns null (a resource already held), timeout (cancelled linked token), result text carries no exception message, tags and name from the options, probe lock released after the check.
2. `tests/Encina.UnitTests/DistributedLock/InMemory/InMemoryRegistrationTests.cs`: builds the provider with `ValidateOnBuild` and `ValidateScopes`, asserts exactly one `IEncinaHealthCheck` when enabled, none when `Enabled = false`.
3. Guard tests for the constructor (null provider, null options) in `tests/Encina.GuardTests/`.
4. Contract test: the Redis, SqlServer and InMemory lock checks honor the same `IEncinaHealthCheck` contract (name, non-empty tags, never throws).
5. Property, integration, load and benchmark: not required (deterministic in-process logic, no external service, single probe); the manifest `reason` for each states this.
6. `mechanical-fixer` updates `.github/coverage-manifest/Encina.DistributedLock.InMemory.json` with per-file targets and one-sentence justifications for the new file: unit 90, guard 15 (the guard flag reaches only the constructor lines; the measured value replaces this estimate before the PR).

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

1. `docs-writer`: update `src/Encina.DistributedLock.InMemory/README.md` (how to enable the check, tags, the registration shape later providers copy) and the lock section of `docs/guides/health-checks.md`.
2. `mechanical-fixer`: changelog fragment `changelog.d/755-inmemory-lock-health-check.added.md`; the `PublicAPI.Unshipped.txt` lines.
3. Verify: `dotnet build --configuration Release` (0 warnings), `dotnet test`, per-flag coverage against the manifest.
4. Open the follow-up issue only if the maintainer chooses Design Choice 3 Option B.

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
| Tests | 3 | 0 | Unit tests, registration test, guard tests (the contract test extends an existing file when one exists) |
| Documentation | 0 | 2 | Package README, `docs/guides/health-checks.md` |
| Configuration | 1 | 2 | Changelog fragment; `PublicAPI.Unshipped.txt`, coverage manifest |
| **Total** | **6** | **6** | One pull request, 12 files, under the 15-file limit |

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

1. The orchestrator briefs a worker for one pull request (12 files) once the maintainer has answered Design Choices 1 and 3.
2. After merge, comment on #755 with the result and note on #207, #208, #209 and #210 that their providers must ship a check following this shape.
3. Open an issue for the `RedisDistributedLockHealthCheck` timeout: it ignores `ProviderHealthCheckOptions.Timeout` today (found while reading the code).
