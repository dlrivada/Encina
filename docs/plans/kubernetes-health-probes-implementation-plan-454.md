# Implementation Plan: Kubernetes Health Probes (liveness, readiness, startup)

> **Issue**: [#454](https://github.com/dlrivada/Encina/issues/454)
> **Type**: Feature
> **Complexity**: Medium (5 phases, about 25 files, split into 3 pull requests)
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
- `Encina.OpenTelemetry` - no change (Design Choice 7).

**Provider category**: none. The checks read stores through abstractions (`IOutboxStore` and siblings), so one real database provider is enough for an integration test of the endpoint behavior.

**Differences from the issue text, found while reading the code:**

- The issue's `AddEncinaHealthChecks(config => ...)` DSL and `config.AddOutboxHealthCheck(...)` duplicate `AddEncinaOutbox(name, options, tags, failureStatus)`, which already takes the options (Design Choice 3).
- The issue's threshold names (`MaxPendingMessages`, `MaxAgeMinutes`, `MaxStuckSagas`) are replaced by the existing ones (`PendingMessageWarningThreshold`, `PendingMessageCriticalThreshold`, `StuckSagaThreshold`, ...), with no aliases (Design Choice 5).
- `PipelineOrderValidationHostedService` (`src/Encina/Pipeline/Ordering`) already validates the pipeline of every closed handler at host start, but no `AddHostedService` registration of it exists in `src/` (the unit tests assert it is absent in one mode). Phase 1 verifies when it is registered before the new checks duplicate its work.

**Sibling plans (health-checks family)**: this plan owns the probe tag constants and the endpoint mapping; #622 owns the health-check telemetry (one per-check gauge), so this plan emits no metric; #2396 registers processor liveness checks with a tag the host can add to the liveness set; #754 owns the shared `ProviderHealthCheckOptions`.

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
| **A) Reuse the internal `PipelineRegistrationSnapshot` and `PipelineOrderResolver` from `Encina.AspNetCore`; evaluate once and cache the result for the process lifetime** | `Encina` already lists `Encina.AspNetCore` in `InternalsVisibleTo`; no new public API in core; the handler set cannot change after the provider is built, so one evaluation is exact | The result is a startup-class check; it does not detect a handler that breaks later |
| **B) Add a public validation method to `IEncinaPipelineInspector`** | Usable by any host | New public core API for one consumer; commits the resolver's shape |

### Chosen Option: **A - reuse the internals, evaluate once, cache**

### Rationale

- `PipelineRegistrationSnapshot.GetClosedHandlerServiceTypes()` lists the closed request and stream handlers; `HandlerHealthCheck` resolves each in a scope (`IServiceProvider.CreateScope()`), `PipelineHealthCheck` calls the resolver's validation. Results report counts (`handlers_total`, `handlers_unresolvable`) and the exception type; unresolvable type names go to the log, not to the response.
- Class: Trivial
- Settled by: AGENTS.md section 3 (no error or exception message in health-check results) and the `InternalsVisibleTo` entry for `Encina.AspNetCore` in `src/Encina/Encina.csproj`.

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

### Chosen Option: **A - a distinct startup endpoint**

### Rationale

- The probe tells Kubernetes when initialization is done, which is exactly what the cached handler and pipeline checks answer.
- Class: Settled
- Settled by: https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/ (startup probes gate liveness and readiness) and AGENTS.md section 6 (function 4).

</details>

<details>
<summary><strong>7. Response content and telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Status code always; a minimal Encina JSON body only when `IncludeDetails` is true; no metric or span here** | Probes are usually unauthenticated, so the body stays off by default; telemetry is one mechanism owned by #622 | The issue's `encina.health.probe_status` metric and `encina.health.probe` span are not built |
| **B) Always write the detailed body and emit a probe metric and span** | Convenient | Backlog counts and check names reach every caller; a second metric next to #622's per-check gauge; ASP.NET Core request instrumentation already traces the probe request |

### Chosen Option: **A - details opt-in, telemetry owned by #622**

### Rationale

- Detailed bodies carry counts and check names only (the checks already return no messages), but they still describe internal state to whoever can reach the endpoint.
- Class: Trivial
- Settled by: AGENTS.md section 1 (every feature is opt-in, pay-for-what-you-use) and AGENTS.md section 3 (no error message in health-check results).

</details>

---

## Implementation Phases

### Phase 1: Tags and the handler and pipeline checks (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

1. Verify where `PipelineOrderValidationHostedService` is registered; if it never is, the checks are the only runtime evidence, and the worker reports it.
2. **Create** `src/Encina.AspNetCore/Health/EncinaHealthTags.cs`: `public static class EncinaHealthTags { public const string Live = "live"; public const string Ready = "ready"; public const string Startup = "startup"; }`; the private tag literals in `HealthCheckBuilderExtensions.cs` use the constants.
3. **Create** `src/Encina.AspNetCore/Health/HandlerHealthCheck.cs` (`DefaultName = "encina-handlers"`, tags `encina`, `startup`) and `PipelineHealthCheck.cs` (`DefaultName = "encina-pipeline"`): `EncinaHealthCheck` subclasses, evaluate once, cache the result (an `Interlocked`-guarded lazy `Task<HealthCheckResult>`), never throw.
4. **Modify** `HealthCheckBuilderExtensions.cs`: `AddEncinaHandlers(...)` and `AddEncinaPipeline(...)` with the same parameters as `AddEncinaOutbox` (name, tags, failure status).
5. **Create** `src/Encina.AspNetCore/Health/HealthCheckLog.cs`: `[LoggerMessage]` EventIds 226 (`HandlersUnresolvable`, count, Warning), 227 (`UnresolvableHandlerType`, type name, Warning), 228 (`PipelineValidationFailed`, exception type, Error) inside `AspNetCore` (200-249; 200-207 used, 208-225 claimed by #3443, #710 and #2229).
6. `PublicAPI.Unshipped.txt` lines (`mechanical-fixer`).

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
- Scoped services are resolved through IServiceProvider.CreateScope(); evaluate once and cache; the check never throws.
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

1. **Create** `src/Encina.AspNetCore/Health/EncinaProbeOptions.cs`: `LivenessPath`, `ReadinessPath`, `StartupPath`, `IncludeDetails`, `AuthorizationPolicy`; an `IValidateOptions<EncinaProbeOptions>` validator (leading slash, distinct paths) registered with `ValidateOnStart`.
2. **Create** `src/Encina.AspNetCore/Health/EncinaHealthEndpointRouteBuilderExtensions.cs`: `MapEncinaHealthChecks(this IEndpointRouteBuilder, Action<EncinaProbeOptions>? configure = null)` returning the three `IEndpointConventionBuilder`s; each calls `MapHealthChecks(path, new HealthCheckOptions { Predicate = r => r.Tags.Contains(tag), ResponseWriter = ... })`; readiness uses `ready`, startup uses `startup`, liveness uses `live` (an empty set answers healthy); `RequireAuthorization(policy)` only when a policy name is set.
3. **Create** `src/Encina.AspNetCore/Health/EncinaProbeResponseWriter.cs`: status-code-only when `IncludeDetails` is false; otherwise `{ "status": "...", "entries": { "<name>": { "status": "...", "data": { ... } } } }` with no description field and no exception.
4. `PublicAPI.Unshipped.txt` lines (`mechanical-fixer`).

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
- Options are validated at startup (ValidateOnStart); invalid paths fail the host, not the first request.
- No metric or span here (owned by #622).
- Registration completeness: a DI test builds the provider with ValidateOnBuild and ValidateScopes.

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

1. Logging is delivered in Phase 1 (EventIds 226-228) and extended in Phase 2 with 229 (`ProbeEndpointsMapped`, paths, Information) in the same `AspNetCore` range; no new range.
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

1. Unit tests in `tests/Encina.UnitTests/AspNetCore/Health/`: tag constants used by the builder extensions; handler check healthy/unhealthy (a handler whose dependency is missing), result cached on the second call, no handler type name or message in the result; probe options validation; response writer with and without details.
2. Guard tests for the new public methods and constructors (`ArgumentNullException`).
3. Contract tests: the new checks honor `IEncinaHealthCheck` (name, tags, never throws); the three endpoints return 200 or 503 per status.
4. Integration (`tests/Encina.IntegrationTests`): an ASP.NET Core `TestServer` host with `MapEncinaHealthChecks`, the outbox check registered over one real database provider through the shared collection fixture (the checks use `IOutboxStore`, so one provider proves the endpoint behavior; the 10-provider coverage of the stores belongs to the store features), asserting: liveness 200 while readiness is 503, startup 503 until the cached check passes.
5. Property, load, benchmark: not required (threshold comparisons are simple; low-frequency probes); each manifest `reason` says so.
6. `mechanical-fixer`: per-file targets and one-sentence justifications in `.github/coverage-manifest/Encina.AspNetCore.json`.

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

1. `docs-writer`: the Kubernetes sections of `docs/guides/health-checks.md` (the startup probe now points at `/health/startup`), the `Encina.AspNetCore` README with a Deployment example, XML docs reviewed.
2. ADR for the probe contract (what each probe may check, the tag constants, the opt-in details body); ADR number assigned when the ADR pull request opens.
3. `mechanical-fixer`: changelog fragment `changelog.d/454-kubernetes-health-probes.added.md`, PublicAPI lines.
4. Open the per-tenant health issue from the issue file written with this plan; link it in the matrix.

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
| Production code | 7 | 1 | Tags, two checks, probe options and validator, mapping extension, response writer, log class; `HealthCheckBuilderExtensions.cs` |
| Tests | 6 | 0 | Unit (checks, options, writer), guard, contract, integration with `TestServer` |
| Documentation | 1 | 2 | ADR; guide, README |
| Configuration | 1 | 2 | Changelog fragment; `PublicAPI.Unshipped.txt`, `Encina.AspNetCore.json` manifest |
| **Total** | **15** | **5** | 20 files in 3 pull requests: PR 1 tags and checks (about 9), PR 2 mapping (about 9), PR 3 documentation and ADR (about 4); each under 15 files |

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
1. EncinaHealthTags (live, ready, startup); HandlerHealthCheck and PipelineHealthCheck evaluated once and cached.
2. AddEncinaHandlers and AddEncinaPipeline builder extensions.
3. EncinaProbeOptions (+ validator), MapEncinaHealthChecks (three endpoints, tag predicates), Encina JSON writer.
4. EventIds 226-229 inside the AspNetCore range; no metric or span.
5. Unit, guard, contract and TestServer integration tests; manifest, guide, README, ADR, changelog through the specialists.
Three pull requests: checks, mapping, documentation.

KEY PATTERNS:
- Results carry counts and the exception type only; the default response is the status code.
- Liveness checks nothing by default (hosts add the live tag); readiness uses ready; startup uses startup.
- Reuse the existing threshold names; no aliases; no configuration DSL.
- Scoped services resolve through IServiceProvider.CreateScope(); options validated with ValidateOnStart.

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
| 1 | Caching | ❌ N/A | Health checks must reflect real-time status; only the one-time startup result is cached, by design |
| 2 | OpenTelemetry | ⏭️ Defer | The per-check gauge is #622 Phase 4; the probe metric and span of the issue text are replaced (Design Choice 7) |
| 3 | Structured Logging | ✅ Include | Phases 1 and 3: EventIds 226-229 in the existing AspNetCore range |
| 4 | Health Checks | ✅ Include | This is the feature |
| 5 | Validation | ✅ Include | `EncinaProbeOptions` validated at startup (paths) |
| 6 | Resilience | ❌ N/A | The endpoint makes no external call; per-check timeouts already come from `ProviderHealthCheckOptions.Timeout` |
| 7 | Distributed Locks | ❌ N/A | Each instance reports itself |
| 8 | Transactions | ❌ N/A | Read-only probes |
| 9 | Idempotency | ❌ N/A | Read-only |
| 10 | Multi-Tenancy | ⏭️ Defer | Per-tenant status needs a tenant-aware design; the issue file written with this plan (shared with #622) tracks it |
| 11 | Module Isolation | ❌ N/A | Module checks exist (`AddEncinaModuleHealthChecks`, tag `modules`) and hosts map them with their own tag predicate |
| 12 | Audit Trail | ❌ N/A | Infrastructure monitoring, no compliance event |

**Deferred items:**

- OpenTelemetry for health: #622 Phase 4.
- Per-tenant health: the issue file `artifacts/issues/health-per-tenant-checks.md` written with this plan (open it before the pull requests merge).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| Existing checks and builder extensions | ✅ Available | `Encina.Messaging/Health`, `Encina.AspNetCore/Health` |
| `InternalsVisibleTo` for `Encina.AspNetCore` | ✅ Available | `src/Encina/Encina.csproj` |
| EventId range 200-249 | ✅ Available | 226-249 free after the other plans' claims |
| #2138 (SchedulingHealthCheck reports dead-lettered messages as overdue forever) | Open bug | Fix before the scheduling check is put in the readiness set, or readiness fails forever |
| Registration of `PipelineOrderValidationHostedService` | Unverified | Phase 1 task 1 |
| #181 (duplicate package request) | Open | Answered together with Design Choice 1 |
| Maintainer answers to Design Choices 1, 2 and 3 | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer Design Choices 1, 2 and 3 when work starts (and #622 Design Choice 1 with Choice 1), records the answers under `## Maintainer Decisions`, then briefs three workers.
2. After merge, comment on #454 with the result and the differences from the issue text, and on #2396 that its checks can carry the `live` tag.
