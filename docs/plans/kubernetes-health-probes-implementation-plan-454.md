# Implementation Plan: Kubernetes Health Probes (liveness, readiness, startup)

> **Issue**: [#454](https://github.com/dlrivada/Encina/issues/454)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 26 file changes, split into 3 pull requests of 12, 10 and 4 files)
> **Estimated Scope**: about 400 lines of production code, about 700 lines of tests
> **Milestone**: Encina 1.0 (cloud-native, observability)

---

## Summary

Add separate liveness, readiness and startup endpoints for Encina applications on top of the checks that already exist. The outbox, inbox, saga, scheduling and dead-letter checks (`src/Encina.Messaging/Health`), their registration (`src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs`: `AddEncinaHealthChecks`, `AddEncinaOutbox`, `AddEncinaInbox`, `AddEncinaSaga`, `AddEncinaScheduling`) and the thresholds (`OutboxHealthCheckOptions`, `SagaHealthCheckOptions`, `SchedulingHealthCheckOptions`) are done. What is missing, verified against `main`: `MapEncinaHealthChecks`, probe options (paths and tag sets), a JSON response writer, a startup check, a handler-resolvability check and a pipeline check.

The plan delivers:

1. `EncinaHealthTags` constants (`live`, `ready`, `startup`) and two new checks, `HandlerHealthCheck` and `PipelineHealthCheck`, built on `PipelineRegistrationSnapshot` and `PipelineOrderResolver`, which `Encina` already exposes to `Encina.AspNetCore` through `InternalsVisibleTo`.
2. `MapEncinaHealthChecks(Action<EncinaProbeOptions>?)` mapping three endpoints with tag predicates and an Encina JSON writer.
3. An ADR for the probe contract, the README and guide updates.

**Affected packages:**

- `Encina.AspNetCore` - tags, checks, builder extensions, probe options, endpoint mapping, response writer, logging, README.
- `Encina` (core) - dependency only; no change. `Encina.Messaging` - dependency only; the existing checks are tagged, not changed.
- `Encina.OpenTelemetry` - no change (Design Choice 10).
- Test projects: `tests/Encina.ContractTests/Encina.ContractTests.csproj` gains a `ProjectReference` to `Encina.AspNetCore` (it has none today; the Unit, Guard and Integration projects already reference it).

**Provider category**: none. The checks read stores through abstractions (`IOutboxStore` and siblings), so one real database provider is enough for an integration test of the endpoint behavior.

**Differences from the issue text, found while reading the code:**

- The issue's `AddEncinaHealthChecks(config => ...)` DSL and `config.AddOutboxHealthCheck(...)` duplicate `AddEncinaOutbox(name, options, tags, failureStatus)`, which already takes the options (Design Choice 3).
- The issue's threshold names (`MaxPendingMessages`, `MaxAgeMinutes`, `MaxStuckSagas`) are replaced by the existing ones (`PendingMessageWarningThreshold`, `PendingMessageCriticalThreshold`, `StuckSagaThreshold`, ...), with no aliases (Design Choice 5).
- `PipelineOrderValidationHostedService` (`src/Encina/Pipeline/Ordering`) already validates the pipeline of every closed handler at host start, but a search of `src/` finds no registration of it outside its own file (the unit tests assert it is absent in one mode). Phase 1 verifies when it is registered before the new checks duplicate its work.
- The issue asks for the outbox and inbox backlog in readiness. Today `AddEncinaOutbox` is deliberately NOT tagged `ready` (`DefaultTagsWithoutReady`), while inbox, saga, scheduling, pool and the composite are (`DefaultTags` = `encina`, `ready`); Design Choice 9 records the choice.
- The issue text proposed a probe metric and span, a liveness deadlock detector and a nested configuration DSL; Design Choices 2, 3 and 10 record why each is replaced. The issue also marked validation not applicable; here the probe paths are validated (Design Choice 8), and caching, which the issue did not consider, applies to the cached handler and pipeline evaluation (Design Choice 11).

**Sibling plans (health-checks family)**: this plan owns the probe tag constants and the endpoint mapping; #622 owns the health-check telemetry (one per-check gauge), so this plan emits no metric; #2396 registers processor liveness checks with a tag the host can add to the liveness set; #754 owns the shared `ProviderHealthCheckOptions`. This plan creates no `ProviderHealthCheckOptions` consumer, so the per-registration options wiring of #754, #755 and #622 does not apply here.

---

## Design Choices

<details>
<summary><strong>1. Where the endpoint mapping lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `Encina.AspNetCore` next to the existing health builder extensions** | The existing home; `MapEncinaHealthChecks` needs `IEndpointRouteBuilder`, which the package already references (`Microsoft.AspNetCore.App`); no new package | Web hosts only |
| **B) A new `Encina.HealthChecks` package** (the original text; #181 asks for it too) | A separate package with a clean name | One more package; it needs the same ASP.NET reference, so a non-web host gains nothing; moves the dozens of existing registrations or splits them |

### Chosen Option: **A - in `Encina.AspNetCore`** (recommended, pending the maintainer)

### Rationale

- Probes are HTTP endpoints; a host without an HTTP listener has no use for `MapHealthChecks`, so Option B's extra package buys no reach.
- Option B wins when non-web hosts also expose probes through another transport. This is the same package question as #622 Design Choice 1 and #181; answer them together.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What may fail the liveness probe</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only that the process responds** (no check is tagged `live` by default; a host may add some) | Follows the Kubernetes guidance and the repository guide (`docs/guides/health-checks.md` says liveness should not check external dependencies); a dependency outage never restarts pods | A stuck pipeline is detected by readiness only |
| **B) Pipeline responsiveness and deadlock detection, as in the original table** | Earlier detection of a stuck pipeline | A false positive restarts pods in a loop; a deadlock detector that is correct is itself a research item |

### Chosen Option: **A - process responds only; hosts opt checks into `live`** (recommended, pending the maintainer)

### Rationale

- The `live` tag is the extension point: a host that wants a restart when a background processor dies tags the #2396 processor-liveness checks `live`; the default set stays empty, so the endpoint answers 200 with an empty report.
- Option B wins when stuck pipelines are the main failure and the maintainer accepts the restart risk.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Configuration shape</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MapEncinaHealthChecks(Action<EncinaProbeOptions>?)` plus the existing `AddEncina*` builder extensions** | No parallel DSL; thresholds stay in the options each check already has; small API | The issue's nested `AddEncinaHealthChecks(config => ...)` sketch is not built |
| **B) The issue's configuration DSL** (`AddEncinaHealthChecks(config => { config.AddOutboxHealthCheck(...); config.LivenessPath = ...; })`) | One call configures everything | It duplicates `AddEncinaOutbox`, `AddEncinaInbox`, `AddEncinaSaga`, `AddEncinaScheduling`; two ways to do one thing |

### Chosen Option: **A - mapping extension plus existing builder extensions** (recommended, pending the maintainer)

### Rationale

- `EncinaProbeOptions` holds `LivenessPath` (`/health/live`), `ReadinessPath` (`/health/ready`), `StartupPath` (`/health/startup`), `IncludeDetails` (default false) and an optional authorization policy name; paths are validated at startup (leading slash, distinct).
- Option B wins when a single registration object matters more than a small surface.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Handler and pipeline checks: cost and source of truth</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse the internal `PipelineRegistrationSnapshot` and `PipelineOrderResolver` from `Encina.AspNetCore`** | `Encina` already lists `Encina.AspNetCore` in `InternalsVisibleTo`; no new public API in core | `Encina.AspNetCore` is coupled to internals of core |
| **B) Add a public validation method to `IEncinaPipelineInspector`** | Usable by any host; no friend-assembly coupling | New public core API for one consumer; commits the resolver's shape |

### Chosen Option: **A - reuse the internals** (recommended, pending the maintainer)

### Rationale

- `PipelineRegistrationSnapshot.GetClosedHandlerServiceTypes()` lists the closed request and stream handlers; `HandlerHealthCheck` resolves each in a scope (`IServiceProvider.CreateScope()`), `PipelineHealthCheck` calls the resolver's validation. Results report counts (`handlers_total`, `handlers_unresolvable`) and the exception type; unresolvable type names go to the log, not to the response.
- Resolving every closed handler instantiates it (constructor side effects, cost proportional to the handler count); the cost is paid once because the result is cached (Design Choice 11). Open generic handlers are skipped by the snapshot, so `handlers_total` counts closed handlers only and the guide says the check is not full coverage.
- Option B wins when core already needs a public pipeline-validation API for other hosts; this is a core API decision, so it is the maintainer's.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Threshold names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the existing names; no aliases** | One name per setting; the options exist and are tested | The issue's sketch uses other names |
| **B) Add the issue's names as aliases** | Matches the issue text | A compatibility layer for a name that never shipped |

### Chosen Option: **A - keep the existing names**

### Rationale

- The issue's names were a proposal written before the checks existed; nothing ships under them.
- Class: Settled
- Settled by: AGENTS.md section 3 (never add compatibility aliases or layers) and AGENTS.md section 1 (pre-1.0, best solution first).

</details>

<details>
<summary><strong>6. Startup probe semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A distinct startup endpoint answering from the one-time handler and pipeline checks (tag `startup`)** | The Kubernetes `startupProbe` disables liveness and readiness until it succeeds, so it must be cheap and distinct from readiness; the result is cached after the first evaluation | One more endpoint |
| **B) Point the startup probe at the readiness endpoint** (what the repository guide shows today) | No new endpoint | Readiness runs every store check on every startup poll and flaps while dependencies warm up |

### Chosen Option: **A - a distinct startup endpoint** (recommended, pending the maintainer)

### Rationale

- The probe tells Kubernetes when initialization is done, which is exactly what the cached handler and pipeline checks answer.
- The Kubernetes documentation (startup probes gate liveness and readiness) is satisfied by Option B as well, so it does not decide; the choice is the maintainer's.
- Class: Dilemma

</details>

<details>
<summary><strong>7. Response body</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Status code always; a minimal Encina JSON body only when `IncludeDetails` is true (default false)** | Probes are usually unauthenticated, so the body stays off by default | A host must opt in to see details |
| **B) Always write the detailed body** | Convenient | Backlog counts and check names reach every caller |

### Chosen Option: **A - details opt-in**

### Rationale

- Detailed bodies carry counts and check names only (the checks already return no messages), but they still describe internal state to whoever can reach the endpoint; Option B contradicts the opt-in rule, so only A complies.
- Class: Settled
- Settled by: AGENTS.md section 1 (every feature is opt-in, pay-for-what-you-use) and AGENTS.md section 3 (no error message in health-check results).

</details>

<details>
<summary><strong>8. Where probe options live and how they are validated</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MapEncinaHealthChecks(Action<EncinaProbeOptions>?)` builds and validates the options itself and throws `InvalidOperationException` at mapping time when paths are invalid (no leading slash, not distinct)** | One call; no service registration; the host fails at startup, before serving a request; nothing to forget to register | Not the `IOptions` pattern; the options are not visible in the container |
| **B) A service-side registration (`AddEncinaProbes(Action<EncinaProbeOptions>?)`) registering `EncinaProbeOptions` with an `IValidateOptions` validator and `ValidateOnStart`; `MapEncinaHealthChecks` reads `IOptions<EncinaProbeOptions>`** | The standard options pattern; DI-visible | Two calls to wire one feature; a host that forgets the registration gets a runtime failure in `MapEncinaHealthChecks` |

### Chosen Option: **A - validate inside the mapping call** (recommended, pending the maintainer)

### Rationale

- `MapEncinaHealthChecks(this IEndpointRouteBuilder, ...)` runs after the provider is built, so it cannot register services or `ValidateOnStart` itself; Option B needs a second registration method to deliver "invalid paths fail the host".
- Under Option A there is nothing to register, so registration completeness (AGENTS.md section 3) is proven by an integration test with a `TestServer` host that maps the endpoints; the checks themselves are registered through the existing `AddEncina*` extensions, covered by a DI test with `ValidateOnBuild` and `ValidateScopes`. Under Option B the same DI test also covers the options.
- Class: Dilemma

</details>

<details>
<summary><strong>9. Readiness set, tags and failure status of the new checks</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep today's tagging: composite, inbox, saga, scheduling, pool and `AddEncinaHealthCheck` carry `ready`; the outbox does not (`DefaultTagsWithoutReady`); the new handler and pipeline checks carry `encina` and `startup` only (not `ready`); the two new extensions take the same `failureStatus` parameter as their siblings (default as theirs)** | No behavior change for existing hosts; a slow outbox backlog does not take the pod out of rotation | The issue's "outbox and inbox backlog in readiness" is met for the inbox only; a host adds `tags: ["ready"]` for the outbox |
| **B) Put the outbox in `ready` by default and the handler and pipeline checks in `ready` too** | Matches the issue text | Changes readiness for existing hosts; a backlog removes pods from rotation, which the code comment of `AddEncinaOutbox` deliberately avoids |

### Chosen Option: **A - keep today's tagging** (recommended, pending the maintainer)

### Rationale

- Verified in `HealthCheckBuilderExtensions.cs`: `DefaultTags` is `["encina", "ready"]`, `DefaultTagsWithoutReady` is `["encina"]`, and the outbox alone uses the latter.
- Which checks decide readiness is a user-visible operational default.
- Class: Dilemma

</details>

<details>
<summary><strong>10. Telemetry of the probe endpoints</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No metric or span here; the per-check gauge is built once by #622** | One mechanism; ASP.NET Core request instrumentation already traces the probe request | The issue's `encina.health.probe_status` metric and `encina.health.probe` span are not built |
| **B) Emit a probe metric and span in the endpoint** | Per-request freshness; matches the issue text | A second metric next to #622's per-check gauge; per-probe instrumentation duplicated |

### Chosen Option: **A - telemetry owned by #622** (recommended, pending the maintainer)

### Rationale

- The difference from the issue text is recorded in "Differences from the issue text"; the choice drops requested content, so it is the maintainer's.
- Class: Dilemma

</details>

<details>
<summary><strong>11. Caching of the handler and pipeline evaluation, and the empty startup set</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cache terminal results for the process lifetime (a completed evaluation, Healthy or Unhealthy); a cancelled or faulted evaluation is never cached; the evaluation runs with the application's stopping token, not the request token; a startup endpoint with no check tagged `startup` fails closed: `MapEncinaHealthChecks` throws `InvalidOperationException` at mapping time naming the missing `AddEncinaHandlers` call** | The handler set cannot change after the provider is built, so one evaluation is exact; a client that aborts the first probe cannot poison the cache; a startup gate never passes empty (fail closed, AGENTS.md section 3) | A failed evaluation stays 503 until restart (correct: the handler set is fixed); a host that wants only readiness must not map the startup path (a `StartupPath = null` option disables it) |
| **B) Cache Healthy results only; re-evaluate after a failure; an empty startup set answers 200** | A transient failure self-heals; no mapping-time exception | Re-resolves every handler on each failed probe; an empty startup gate passes (fail open) |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Caching behavior, the token used, and fail-open versus fail-closed for the startup gate are each user-visible semantics.
- The liveness endpoint with no check tagged `live` still answers 200 (Design Choice 2: the process responds); only the startup gate fails closed.
- Class: Dilemma

</details>

<details>
<summary><strong>12. Default paths and authorization</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Paths `/health/live`, `/health/ready`, `/health/startup`; no authorization by default (the status-only body discloses nothing); an optional `AuthorizationPolicy` is applied through `RequireAuthorization` when set** | Probes are called by the kubelet without credentials; matches the guide | A host that sets `IncludeDetails = true` without a policy exposes details; the guide documents the risk (no extra EventId: the block is 226-229) |
| **B) Require an authorization policy whenever `IncludeDetails` is true** | Details cannot be exposed unauthenticated | Another failure mode at mapping time |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Default names and the default of no authorization are public, user-visible defaults.
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: Tags and the handler and pipeline checks (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 12 files (the log class and EventIds 226-228 are created here, the first PR that logs):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.AspNetCore/Health/EncinaHealthTags.cs` | new |
| 2 | `src/Encina.AspNetCore/Health/HandlerHealthCheck.cs` | new |
| 3 | `src/Encina.AspNetCore/Health/PipelineHealthCheck.cs` | new |
| 4 | `src/Encina.AspNetCore/Health/HealthCheckLog.cs` | new (226-228) |
| 5 | `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs` | modify |
| 6 | `src/Encina.AspNetCore/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/AspNetCore/Health/HandlerPipelineHealthCheckTests.cs` | new (checks, tags, builder extensions, DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 8 | `tests/Encina.GuardTests/AspNetCore/Health/HandlerPipelineHealthCheckGuardTests.cs` | new |
| 9 | `tests/Encina.ContractTests/AspNetCore/EncinaHealthCheckContractTests.cs` | new (the new checks honor `IEncinaHealthCheck`: name, tags, never throws) |
| 10 | `tests/Encina.ContractTests/Encina.ContractTests.csproj` | modify (`ProjectReference` to `Encina.AspNetCore`) |
| 11 | `.github/coverage-manifest/Encina.AspNetCore.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/454-handler-pipeline-health-checks.added.md` | new (`mechanical-fixer`) |

Tasks:

1. Verify where `PipelineOrderValidationHostedService` is registered; if it never is, the checks are the only runtime evidence, and the worker reports it.
2. **Create** `EncinaHealthTags`: `public static class EncinaHealthTags { public const string Live = "live"; public const string Ready = "ready"; public const string Startup = "startup"; }`; the private tag literals in `HealthCheckBuilderExtensions.cs` use the constants.
3. **Create** `HandlerHealthCheck` (`DefaultName = "encina-handlers"`, tags `encina`, `startup`) and `PipelineHealthCheck` (`DefaultName = "encina-pipeline"`, tags `encina`, `startup`; Design Choice 9): `EncinaHealthCheck` subclasses that evaluate once and cache terminal results per Design Choice 11 (a cancelled or faulted evaluation is not cached; the evaluation uses the application's stopping token, not the request token), and never throw.
4. **Modify** `HealthCheckBuilderExtensions.cs`: `AddEncinaHandlers(...)` and `AddEncinaPipeline(...)` with the same parameters as `AddEncinaOutbox` (name, tags, `failureStatus`).
5. **Create** `HealthCheckLog` with `[LoggerMessage]` EventIds 226 (`HandlersUnresolvable`, count, Warning), 227 (`UnresolvableHandlerType`, type name, Warning), 228 (`PipelineValidationFailed`, exception type through `ForLogging()`, Error) inside `AspNetCore` (200-249; 200-207 used, 208-225 claimed by #3443, #710 and #2229).
6. **Tests and manifest in this PR**: unit tests for healthy, unhealthy (a handler whose dependency is missing), result cached on the second call, a cancelled first evaluation not cached, no handler type name or message in the result; guard tests (the builder extensions, constructors); the contract test; manifest per-file targets with one-sentence justifications in `Encina.AspNetCore.json`: `HandlerHealthCheck.cs` and `PipelineHealthCheck.cs` (unit 90, guard 15, contract 40 each), `HealthCheckLog.cs` (unit 70; guard 0 and contract 0 because generated logger bodies run only when a check logs), `EncinaHealthTags.cs` (unit 90; guard 0 and contract 0: constants only, no executable code that guard tests reach), `HealthCheckBuilderExtensions.cs` (keeps its targets, unit raised for the two new methods). Property, load and benchmark: not required (the manifest `reason` says so).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library. Issue #454 adds Kubernetes probe support. Encina grants Encina.AspNetCore access
to its internals (InternalsVisibleTo), including PipelineRegistrationSnapshot and PipelineOrderResolver in
src/Encina/Pipeline/Ordering. The host registration lives in src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs.

TASK:
Create EncinaHealthTags, HandlerHealthCheck, PipelineHealthCheck and the AddEncinaHandlers/AddEncinaPipeline builder
extensions, plus the log messages with EventIds 226-228. Build with zero warnings.

KEY RULES:
- Derive from EncinaHealthCheck; results carry counts and the exception type only, never a message or a handler type name.
- Scoped services are resolved through IServiceProvider.CreateScope(); evaluate once and cache terminal results only (never a cancelled or faulted evaluation); the check never throws.
- Use the [LoggerMessage] source generator; EventIds 226-228 sit inside the registered AspNetCore range.
- XML docs with examples; sealed classes; nullable enabled.

REFERENCE FILES:
- src/Encina/Pipeline/Ordering/PipelineRegistrationSnapshot.cs
- src/Encina/Pipeline/Ordering/PipelineOrderValidationHostedService.cs
- src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 2: Probe options, endpoint mapping and response writer (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 10 files (PR 1 is merged first; EventId 229 is added to the log class PR 1 created):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.AspNetCore/Health/EncinaProbeOptions.cs` | new (options and `Validate`) |
| 2 | `src/Encina.AspNetCore/Health/EncinaHealthEndpointRouteBuilderExtensions.cs` | new |
| 3 | `src/Encina.AspNetCore/Health/EncinaProbeResponseWriter.cs` | new |
| 4 | `src/Encina.AspNetCore/Health/HealthCheckLog.cs` | modify (229) |
| 5 | `src/Encina.AspNetCore/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.UnitTests/AspNetCore/Health/EncinaProbeOptionsAndWriterTests.cs` | new |
| 7 | `tests/Encina.GuardTests/AspNetCore/Health/MapEncinaHealthChecksGuardTests.cs` | new |
| 8 | `tests/Encina.IntegrationTests/AspNetCore/Health/ProbeEndpointsIntegrationTests.cs` | new (`TestServer`; `Microsoft.AspNetCore.TestHost` is already referenced by the project) |
| 9 | `.github/coverage-manifest/Encina.AspNetCore.json` | modify (`mechanical-fixer`) |
| 10 | `changelog.d/454-kubernetes-health-probes.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `EncinaProbeOptions`: `LivenessPath`, `ReadinessPath`, `StartupPath` (nullable: null disables the startup endpoint), `IncludeDetails`, `AuthorizationPolicy`; an internal validation (leading slash, distinct paths) called by `MapEncinaHealthChecks`, which throws `InvalidOperationException` at mapping time (Design Choice 8, Option A; if the maintainer chooses Option B the options move to an `AddEncinaProbes` registration with an `IValidateOptions` validator and `ValidateOnStart`, and the DI test covers them).
2. **Create** `EncinaHealthEndpointRouteBuilderExtensions`: `MapEncinaHealthChecks(this IEndpointRouteBuilder, Action<EncinaProbeOptions>? configure = null)` returning the three `IEndpointConventionBuilder`s; each calls `MapHealthChecks(path, new HealthCheckOptions { Predicate = r => r.Tags.Contains(tag), ResponseWriter = ... })`; readiness uses `ready`, startup uses `startup` (fails closed at mapping time when no check carries it, Design Choice 11), liveness uses `live` (an empty set answers healthy, Design Choice 2); `RequireAuthorization(policy)` only when a policy name is set (Design Choice 12); logs 229 with the paths.
3. **Create** `EncinaProbeResponseWriter`: status-code-only when `IncludeDetails` is false; otherwise `{ "status": "...", "entries": { "<name>": { "status": "...", "data": { ... } } } }` with no description field and no exception.
4. **Tests and manifest in this PR**: unit tests for options validation (invalid and duplicate paths) and the writer with and without details; guard tests for `MapEncinaHealthChecks`; the integration test (below); manifest targets per file with one-sentence justifications (`EncinaProbeOptions.cs` unit 90, guard 0 because the options class has no method parameters for guard tests to reach; `EncinaHealthEndpointRouteBuilderExtensions.cs` unit 85, guard 15, integration 60; `EncinaProbeResponseWriter.cs` unit 90, guard 15; `HealthCheckLog.cs` keeps PR 1's targets). The contract flag has no new target: the endpoint behavior is an integration concern, and no new public interface or abstract class exists in this PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: EncinaHealthTags and the handler and pipeline checks exist. Encina.AspNetCore references
Microsoft.AspNetCore.App.

TASK:
Create EncinaProbeOptions with its validator, MapEncinaHealthChecks and the response writer as described in the plan.

KEY RULES:
- The default response is the status code only; details are opt-in and never include descriptions or exception text.
- Options are validated inside MapEncinaHealthChecks (Design Choice 8): invalid paths throw InvalidOperationException at mapping time, so the host fails at startup, not on the first request.
- No metric or span here (owned by #622).
- A startup endpoint with no check tagged startup fails closed at mapping time (Design Choice 11).
- Registration completeness: the TestServer integration test maps the endpoints; the DI test with ValidateOnBuild and ValidateScopes ran in PR 1.

REFERENCE FILES:
- src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs
- src/Encina.AspNetCore/Health/CompositeEncinaHealthCheck.cs
- src/Encina.AspNetCore/Health/EncinaHealthCheckAdapter.cs
- docs/guides/health-checks.md (Kubernetes Configuration section)
```

</details>

### Phase 3: Observability and structured logging

<details>
<summary><strong>Tasks</strong></summary>

1. Logging is delivered in Phase 1 (EventIds 226-228, in PR 1) and extended in Phase 2 with 229 (`ProbeEndpointsMapped`, paths, Information, in PR 2) in the same `AspNetCore` range; no new range, and no id is used before the PR that creates or extends the log class.
2. No `ActivitySource` and no `Meter` in this plan; the per-check gauge is #622 Phase 4.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
EventIds 200-207 are used in the AspNetCore range (200-249) and 208-225 are claimed by other plans; Phase 1 took 226-228.

TASK:
Add EventId 229 to HealthCheckLog and call it when MapEncinaHealthChecks maps the endpoints.

KEY RULES:
- Source generator only; packed sequentially; no new range; EncinaEventIdAllocationTests must still pass.
- Log paths and the tag names only; never headers or response bodies.

REFERENCE FILES:
- src/Encina.AspNetCore/Health/HealthCheckLog.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 4: Testing

<details>
<summary><strong>Tasks</strong></summary>

The tests, manifest entries and PublicAPI lines belong to the pull request of each file table above (PR 1 and PR 2); this phase states what they contain and how they are verified.

1. Unit tests in `tests/Encina.UnitTests/AspNetCore/Health/` (PR 1 and PR 2): tag constants used by the builder extensions; handler check healthy/unhealthy (a handler whose dependency is missing), result cached on the second call, no handler type name or message in the result; probe options validation; response writer with and without details.
2. Guard tests in `tests/Encina.GuardTests/AspNetCore/Health/` for the new public methods and constructors: `MapEncinaHealthChecks`, `AddEncinaHandlers`, `AddEncinaPipeline`, the check constructors (`ArgumentNullException`), with their `PublicAPI.Unshipped.txt` lines.
3. Contract tests (PR 1): the new checks honor `IEncinaHealthCheck` (name, tags, never throws). The behavior "the three endpoints return 200 or 503 per status" is an integration test, not a contract.
4. Integration (`tests/Encina.IntegrationTests`, PR 2): an ASP.NET Core `TestServer` host with `MapEncinaHealthChecks`, the outbox check registered over one real database provider through the shared collection fixture (the checks use `IOutboxStore`, so one provider proves the endpoint behavior; the 10-provider coverage of the stores belongs to the store features), asserting: liveness 200 while readiness is 503, startup 503 until the cached check passes, invalid paths throw at mapping, and a startup endpoint without a startup check throws.
5. Property, load, benchmark: not required (threshold comparisons are simple; low-frequency probes); each manifest `reason` says so.
6. `mechanical-fixer` per PR: per-file targets and one-sentence justifications in `.github/coverage-manifest/Encina.AspNetCore.json`, entered in the PR whose tests meet them.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phases 1 to 3 exist. Tests must execute the real checks and the real endpoint pipeline.

TASK:
Write the unit, guard, contract and integration tests listed in the plan, measure per-flag coverage of the new files
with the coverage-report script and report it against the manifest targets.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; no Thread.Sleep; FakeTimeProvider if time matters.
- Integration tests use a shared [Collection("<Family>-<Database>")] fixture, [Trait("Category","Integration")], ClearAllDataAsync in InitializeAsync.
- CRAP of every added method 10 or less.

REFERENCE FILES:
- tests/Encina.UnitTests/AspNetCore/
- .github/coverage-manifest/Encina.AspNetCore.json
- docs/testing/integration-tests.md
```

</details>

### Phase 5: Documentation, ADR and finalization (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 4 files (PR 1 and PR 2 are merged first; each earlier PR already carries its own changelog fragment and PublicAPI lines):

| # | File | Change |
|---|------|--------|
| 1 | `docs/guides/health-checks.md` | modify (the Kubernetes sections; the startup probe now points at `/health/startup`; the risk of `IncludeDetails` without a policy; handler check coverage limits) |
| 2 | `src/Encina.AspNetCore/README.md` | modify (a Deployment example) |
| 3 | `docs/architecture/adr/<number>-kubernetes-health-probes.md` | new (ADR number assigned when the ADR pull request opens) |
| 4 | `docs/architecture/adr/index.md` | modify (the ADR row) |

Tasks:

1. `docs-writer`: the two documentation files above, XML docs reviewed.
2. ADR for the probe contract (what each probe may check, the tag constants, the opt-in details body, the fail-closed startup gate); the ADR is written by `docs-writer`.
3. Follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/per-tenant-health-checks.md; shared with #622): per-tenant health.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
The code and tests are merged. Documentation, the ADR and the changelog remain.

TASK:
Update the guide and README, write the ADR, add the changelog fragment, run the build and the tests.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers only); English; no emojis.
- Never edit CHANGELOG.md; use changelog.d/.
- The ADR records the three probe semantics and the tag constants.

REFERENCE FILES:
- docs/guides/health-checks.md
- src/Encina.AspNetCore/README.md
- docs/architecture/adr/index.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| Kubernetes probes (liveness, readiness, startup) | Defines what each probe does when it fails |
| ASP.NET Core health checks (`MapHealthChecks`, `HealthCheckOptions.Predicate`, `ResponseWriter`) | Tag-filtered endpoints |
| AGENTS.md section 3 | No error or exception message in health-check results; registration completeness; no compatibility aliases |
| AGENTS.md section 6, function 4 | A checkable dependency exposes a health check |
| ADR-018 | Cross-cutting integration evaluation |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `HealthCheckBuilderExtensions` | `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs` | Existing registrations and tag helpers; the two new extensions join it |
| `CompositeEncinaHealthCheck`, `EncinaHealthCheckAdapter` | `src/Encina.AspNetCore/Health/` | Aggregation and adapter to `IHealthCheck` |
| `OutboxHealthCheck` and options, `SagaHealthCheck`, `SchedulingHealthCheck`, `InboxHealthCheck`, `DeadLetterHealthCheck` | `src/Encina.Messaging/Health/` | The readiness set; thresholds stay as they are |
| `EncinaHealthCheck`, `HealthCheckResult` | `src/Encina.Messaging/Health/` | Base type and result |
| `PipelineRegistrationSnapshot`, `PipelineOrderResolver`, `PipelineOrderValidationHostedService` | `src/Encina/Pipeline/Ordering/` | Source of the closed handler list and the pipeline validation |
| `InternalsVisibleTo` for `Encina.AspNetCore` | `src/Encina/Encina.csproj` | Lets the checks use the internals |
| `EventIdRanges.AspNetCore` | `src/Encina/Diagnostics/EventIdRanges.cs` | Existing range 200-249 |
| Kubernetes section of the guide | `docs/guides/health-checks.md` | Updated to the startup endpoint |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.AspNetCore` | 200-249 (existing `AspNetCore`) | 200-207 used, 208-225 claimed by #3443, #710, #2229; this plan takes 226-229; no new range |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 7 | 2 | New: tags, two checks, log class (PR 1), probe options, mapping extension, response writer (PR 2); modified: `HealthCheckBuilderExtensions.cs`, `HealthCheckLog.cs` (PR 2, EventId 229) |
| Tests | 6 | 1 | New: unit (2), guard (2), contract (1), integration with `TestServer` (1); modified: `Encina.ContractTests.csproj` |
| Documentation | 1 | 3 | ADR (new); guide, README, ADR index |
| Configuration | 2 | 4 | Two changelog fragments (new); two `PublicAPI.Unshipped.txt` edits and two `Encina.AspNetCore.json` manifest edits (PR 1 and PR 2) |
| **Total** | **16** | **10** | 26 file changes in 3 pull requests: PR 1 = 12, PR 2 = 10, PR 3 = 4 (12 + 10 + 4 = 26); each at most 15 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #454 adds Kubernetes probe endpoints.
The outbox, inbox, saga, scheduling and dead-letter checks and their builder extensions already exist in
Encina.Messaging/Health and Encina.AspNetCore/Health. Telemetry for health checks is owned by #622; processor
liveness by #2396; the shared ProviderHealthCheckOptions by #754.

IMPLEMENTATION OVERVIEW:
1. EncinaHealthTags (live, ready, startup); HandlerHealthCheck and PipelineHealthCheck evaluated once, terminal results cached.
2. AddEncinaHandlers and AddEncinaPipeline builder extensions.
3. EncinaProbeOptions (validated inside the mapping call), MapEncinaHealthChecks (three endpoints, tag predicates, startup fails closed), Encina JSON writer.
4. EventIds 226-229 inside the AspNetCore range; no metric or span.
5. Unit, guard, contract and TestServer integration tests; manifest, guide, README, ADR, changelog through the specialists.
Three pull requests: checks, mapping, documentation.

KEY PATTERNS:
- Results carry counts and the exception type only; the default response is the status code.
- Liveness checks nothing by default (hosts add the live tag); readiness uses ready; startup uses startup.
- Reuse the existing threshold names; no aliases; no configuration DSL.
- Scoped services resolve through IServiceProvider.CreateScope(); options validated inside MapEncinaHealthChecks (unless the maintainer chooses a service-side registration).

REFERENCE FILES:
- src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs
- src/Encina.AspNetCore/Health/CompositeEncinaHealthCheck.cs
- src/Encina/Pipeline/Ordering/PipelineRegistrationSnapshot.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
- docs/guides/health-checks.md
- .github/coverage-manifest/Encina.AspNetCore.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The handler and pipeline evaluation is cached once per process (Design Choice 11); all other checks reflect real-time status and are never cached |
| 2 | OpenTelemetry | ⏭️ Defer | The per-check gauge is #622 Phase 4; the probe metric and span of the issue text are replaced (Design Choice 10) |
| 3 | Structured Logging | ✅ Include | Phases 1 and 3: EventIds 226-228 (PR 1) and 229 (PR 2) in the existing AspNetCore range |
| 4 | Health Checks | ✅ Include | This is the feature |
| 5 | Validation | ✅ Include | `EncinaProbeOptions` paths validated when the endpoints are mapped, at host startup (Design Choice 8) |
| 6 | Resilience | ❌ N/A | The endpoint makes no external call; per-check timeouts already come from `ProviderHealthCheckOptions.Timeout` (the issue text deferred timeouts to the checks too) |
| 7 | Distributed Locks | ❌ N/A | Each instance reports itself |
| 8 | Transactions | ❌ N/A | Read-only probes |
| 9 | Idempotency | ❌ N/A | Read-only |
| 10 | Multi-Tenancy | ⏭️ Defer | Per-tenant status needs a tenant-aware design; follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/per-tenant-health-checks.md; shared with #622) |
| 11 | Module Isolation | ❌ N/A | Module checks exist (`AddEncinaModuleHealthChecks`, tag `modules`) and hosts map them with their own tag predicate |
| 12 | Audit Trail | ❌ N/A | Infrastructure monitoring, no compliance event |

**Deferred items:**

- OpenTelemetry for health: #622 Phase 4.
- Per-tenant health: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/per-tenant-health-checks.md).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| Existing checks and builder extensions | ✅ Available | `Encina.Messaging/Health`, `Encina.AspNetCore/Health` |
| `InternalsVisibleTo` for `Encina.AspNetCore` | ✅ Available | `src/Encina/Encina.csproj` |
| EventId range 200-249 | ✅ Available | 226-249 free after the other plans' claims |
| #2138 (SchedulingHealthCheck reports dead-lettered messages as overdue forever) | Open bug | `AddEncinaScheduling` is already tagged `ready` (`DefaultTags`), so the readiness endpoint of this plan inherits the false failure for hosts that register it; the code of this plan does not depend on the fix, but the readiness acceptance of a host that uses scheduling does |
| Registration of `PipelineOrderValidationHostedService` | Unverified | Phase 1 task 1 |
| #181 (duplicate package request) | Open | Answered together with Design Choice 1 |
| Maintainer answers to the Dilemmas (Design Choices 1, 2, 3, 4, 6, 8, 9, 10, 11, 12) | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 2, 3, 4, 6, 8, 9, 10, 11 and 12; and #622 Design Choice 1 together with Choice 1) when work starts, records the answers under `## Maintainer Decisions`, then briefs three workers.
2. After merge, comment on #454 with the result and the differences from the issue text, and on #2396 that its checks can carry the `live` tag.
