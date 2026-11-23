# Implementation Plan: `Encina.OpenTelemetry` — OTLP Collector Reachability Health Check

> **Issue**: [#1049](https://github.com/dlrivada/Encina/issues/1049)
> **Type**: Feature
> **Complexity**: Low-medium (one health check, one options type, one registration, one pull request)
> **Estimated Scope**: ~330-420 lines of production code + ~450-600 lines of tests
> **Specification**: OTLP/gRPC and OTLP/HTTP receivers (default ports 4317 and 4318); [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) (settled point 1 names this issue as exporter work; settled points 2 and 7); `AGENTS.md` sections 3 and 6
> **Depends on**: [#1043](https://github.com/dlrivada/Encina/issues/1043) (implementation: `EnableOtlpExporter` and `ConfigureOtlpExporter`; its plan is on `main`, the code is not)
> **Related**: [#1048](https://github.com/dlrivada/Encina/issues/1048) (logs wiring; independent of this check), [#622](https://github.com/dlrivada/Encina/issues/622) (health checks plan; takes EventId 7010 of the same range, with #174 7010-7012 and #183 7013-7015), [#888](https://github.com/dlrivada/Encina/issues/888) (parent EPIC)

---

## Summary

Once #1043 lands, an application can export traces, metrics and logs to a collector, but nothing tells an operator at runtime that the collector became unreachable; telemetry is lost silently. This plan adds an opt-in `OtlpExporterHealthCheck` to `Encina.OpenTelemetry` that probes the configured collector endpoint with a bounded TCP connect (and, when asked, an HTTP `HEAD`) and reports `Healthy`, `Degraded` or `Unhealthy`.

1. **The check.** `OtlpExporterHealthCheck : IEncinaHealthCheck` resolves the effective endpoint once, opens a TCP connection with a timeout from `TimeProvider`, and maps the outcome: reachable within the degraded threshold is `Healthy`, reachable but slower is `Degraded`, a refusal, DNS failure or timeout is `Unhealthy`. With the exporter off it returns `Healthy` ("not configured").
2. **Two corrections of the issue's sketch.** `IEncinaHealthCheck` (`src/Encina.Messaging/Health/IEncinaHealthCheck.cs`) has `CheckHealthAsync(CancellationToken)` with no `HealthCheckContext`, and `EncinaOpenTelemetryOptions` is registered as a plain singleton instance, not as `IOptions<>`, so the constructor takes the instance (Design Choice 1 and the register).
3. **Opt-in.** `EncinaOpenTelemetryOptions.AddOtlpHealthCheck` (default `false`) registers the check from `AddEncinaOpenTelemetry`; thresholds are configured through a callback (Design Choice 2).
4. **Cancellation is not a failure.** A caller's cancellation propagates; only the probe's own timeout makes the result `Unhealthy`.

**Standards**: OTLP receiver ports; `AGENTS.md` section 3 (the endpoint may carry credentials, so it is never reported; cancellation propagates).

**Estimated scope**: 5 new production files, 2 modified, 1 `PublicAPI.Unshipped.txt`, 1 manifest, 4 test and justification files, the package README, a changelog fragment; one pull request of 15 files (see Implementation Phases).

**Affected packages**: `Encina.OpenTelemetry`.

**Provider category**: Observability (OpenTelemetry); none of the database, cache, transport or lock matrices applies.

**What the code does today** (`origin/main` at `ce352001f`):

- `src/Encina.OpenTelemetry/Migrations/SchemaDriftHealthCheck.cs` and `Resharding/ReshardingHealthCheck.cs` implement `Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck` (not `IEncinaHealthCheck`) with an options object taken in the constructor and `TimeProvider? timeProvider = null`; they are registered by the consumer through `AddHealthChecks().Add(...)`. No class in the package implements `IEncinaHealthCheck`.
- `IEncinaHealthCheck` (`Encina.Messaging.Health`, referenced by the project) has `Name`, `Tags` and `CheckHealthAsync(CancellationToken)`; `Encina.AspNetCore` adapts every registered `IEncinaHealthCheck` (`HealthCheckBuilderExtensions` resolves `GetServices<IEncinaHealthCheck>()`); provider packages register theirs as `AddSingleton<IEncinaHealthCheck, X>()`. The base class `EncinaHealthCheck` turns a cancelled call into `Unhealthy`, which Design Choice 1 avoids by implementing the interface directly.
- `AddEncinaOpenTelemetry` registers the options with `services.TryAddSingleton(options)` and eight decorators; `EncinaOpenTelemetryOptions` has `ServiceName`, `ServiceVersion`, `EnableMessagingEnrichers`. The OTLP properties of #1043 do not exist yet, and the OTLP package is referenced with `PrivateAssets="all"` until #1043.
- Existing EventIds of the package: 7000-7009 (`ReshardingLogMessages`) inside the registered range `OpenTelemetry = (7000, 7099)`; the plans on `main` claim 7010 (#622), 7010-7012 (#174) and 7013-7015 (#183).
- The observability stack `.github/observability/docker-compose.observability.yml` exposes the collector on 4317 (gRPC) and 4318 (HTTP).
- The manifest `.github/coverage-manifest/Encina.OpenTelemetry.json` gives the default health-check files the flags unit and guard with no numeric target, `ServiceCollectionExtensions.cs` unit 90 and guard 35, and `Resharding/ReshardingLogMessages.cs` no flags.

---

## Design Choices

<details>
<summary><strong>1. The health check contract: `IEncinaHealthCheck` or `IHealthCheck`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IEncinaHealthCheck`, implemented directly (not through `EncinaHealthCheck`), registered by `AddEncinaOpenTelemetry` as `IEncinaHealthCheck`** | The issue's contract and the `AGENTS.md` section 6 row 4 integration point; picked up by the existing Encina health-check wiring with no consumer code; the base class is avoided so a caller's cancellation propagates | Differs from the two existing checks of the package, which are `IHealthCheck` registered by the consumer; the Encina wiring lives in `Encina.AspNetCore` |
| **B) `Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck`, as `SchemaDriftHealthCheck` and `ReshardingHealthCheck`** | Same shape as its package siblings; works with plain `AddHealthChecks()` | The consumer must add the registration by hand, so the `AddOtlpHealthCheck` flag cannot register it; the issue says `IEncinaHealthCheck` |
| **C) Derive from `EncinaHealthCheck`** | Gets the name and tag handling and the exception catch for free | The base turns a cancelled call into `Unhealthy` (a failure), against `AGENTS.md` section 3 |

### Chosen Option: **A — `IEncinaHealthCheck`, direct implementation** (recommended, pending the maintainer)

### Rationale

- A wins for an application that already calls the Encina health-check wiring and wants the flag to be the only switch; B wins for an application that uses only the Microsoft health-check API and for consistency with the two siblings of the package. C is ruled out by the cancellation rule.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How the thresholds are configured</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A callback `Action<OtlpHealthCheckOptions>? ConfigureOtlpHealthCheck` on `EncinaOpenTelemetryOptions`, next to `AddOtlpHealthCheck`** | Mirrors `ConfigureOtlpExporter` of #1043; the options object is created by the registration, so a consumer never builds the check by hand | One more property on the options class |
| **B) A property `OtlpHealthCheckOptions OtlpHealthCheck { get; }` initialised to defaults and mutated by the consumer** | No delegate | Public mutable nested object on a registered singleton; different style from the exporter callback |
| **C) The consumer constructs `OtlpHealthCheckOptions` and the check manually, as for `ReshardingHealthCheck`** | Matches the siblings | The flag registers nothing useful; thresholds cannot be set through `AddEncinaOpenTelemetry` |

### Chosen Option: **A — callback** (recommended, pending the maintainer)

### Rationale

- A wins on symmetry with the exporter callback and on a single configuration site; B wins when the consumer prefers binding thresholds from `IConfiguration`, which an options property binds more naturally. The constructor of the check validates the values (`Timeout` and `DegradedThreshold` positive, `DegradedThreshold` less than `Timeout`) and throws `ArgumentOutOfRangeException`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Telemetry of the probe itself</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One span per probe on an `ActivitySource` named `Encina.OpenTelemetry` (ADR-054 D3, one source per package), guarded by `HasListeners()`, tagged with the outcome and a fixed reason value** | The issue's cross-cutting row 2; the probe's latency is visible in traces | The span of a failing probe goes to the very collector that is unreachable, so it is lost exactly when it matters; a new source must be subscribed in `WithEncina` |
| **B) No span and no metric; the result and two log messages are the whole signal** | Nothing circular; no new source; fewer files | Drops a row the issue marks as included; latency history is only in the health report |

### Chosen Option: **A — a span per probe** (recommended, pending the maintainer)

### Rationale

- A wins when traces also reach a second backend or the collector is reachable most of the time (the span then shows slow probes); B wins when the probe runs every few seconds and the span noise outweighs the value, or when the only exporter is the probed collector. Tags are the fixed values `healthy`, `degraded`, `unhealthy` and `timeout`, `refused`, `dns`, `error`: bounded cardinality, no endpoint, no exception message (ADR-054 settled point 2).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. The HTTP client of the optional `HEAD` probe</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A short-lived `HttpClient` over a `SocketsHttpHandler` created per probe, only when `ProbeHttpEndpoint` is on** | No new package or registration; the default path (TCP only) allocates no client | A handler per probe (health checks run every few seconds at most); each probe opens a new connection, which is what is being tested |
| **B) `IHttpClientFactory`** | Pooled handlers, central configuration | Adds `Microsoft.Extensions.Http` to the package and a registration for an opt-in flag |

### Chosen Option: **A — per-probe client** (recommended, pending the maintainer)

### Rationale

- A wins for an infrequent probe and a package that depends on no HTTP stack today; B wins when the application already registers `IHttpClientFactory` and wants the probe in its handler policy. Any HTTP response, including `405` (the OTLP/HTTP receiver rejects `HEAD`), counts as reachable; only a transport failure or the timeout is `Unhealthy`.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. When the check logs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Log on transitions only: a Warning when the status leaves `Healthy` (message 7016, reason and latency) and an Information when it returns (message 7017), tracked with an `Interlocked` field of the singleton** | One line per incident, however often the probe runs | Per-instance state in a singleton; two messages |
| **B) Log every non-healthy result** | Stateless | A collector down for an hour at a 10-second probe writes 360 identical warnings |

### Chosen Option: **A — transitions** (recommended, pending the maintainer)

### Rationale

- A wins for any probe interval under a minute; B wins when the log pipeline already deduplicates and operators want one line per check. The messages carry only the reason value and the latency in milliseconds, never the endpoint (register C3), and an exception passes through `ForLogging()` if one is ever attached.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Settled by |
|----|--------|-------|------------|
| C1 | No `EncinaError` is created (the result is a health status), so there is no ADR-062 `ErrorCategory` to assign | Settled | ADR-062 (a category is chosen where an error is created) |
| C2 | Any EventId this feature needs (two under Dilemma 5 option A, none under option B) comes from the registered range `OpenTelemetry = (7000, 7099)` and is taken after the highest id that any plan on `main` or the code already claims: 7000-7009 are used by resharding, `docs/plans` claims 7010 (#622), 7010-7012 (#174) and 7013-7015 (#183), so 7016 is the first free id on current `main`; the ids are re-read from `docs/plans` and the registry when the pull request opens; no new range and no registry edit (a range below 10000 that is not historical fails `EventIdRegistryAgreementTests` rule 5, and this range is historical) | Settled | `AGENTS.md` section 7 (ids inside a registered range, packed without gaps; new ranges only in the 10000+ zone) |
| C3 | The endpoint is never put in the result data, the span, a tag or a log: a URI can carry user information or tokens in its query | Settled | `AGENTS.md` section 3 (credentials never printed); ADR-054 settled point 2 |
| C4 | A caller's cancellation propagates as `OperationCanceledException`; only the probe's own timeout (a token from `TimeProvider`) is `Unhealthy`; cancellation is never a failure result | Settled | `AGENTS.md` section 3 (cancellation is not an error and propagates); ADR-052 |
| C5 | `EnableOtlpExporter == false` returns `Healthy` with the description "OTLP exporter not configured", also when the flag `AddOtlpHealthCheck` is on | Settled | #1049 acceptance criterion 4 |
| C6 | The check's name and tags are the constants `DefaultName = "encina-otlp-exporter"` and the static array `Tags = ["encina", "observability", "otlp", "ready"]` under either option of Dilemma 1 (the instance `Name` and `Tags` of `IEncinaHealthCheck` return them; a Microsoft registration would use the same strings) | Trivial | Existing Encina health checks (`DefaultName` const and static `Tags`, the prompt's health-check rule); the issue sketch |
| C7 | The effective endpoint is resolved once and cached: a fresh `OtlpExporterOptions` (which reads `OTEL_EXPORTER_OTLP_ENDPOINT` and the SDK default) is passed through `ConfigureOtlpExporter`; per-signal variables such as `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT` are not read and the README says so | Trivial | #1043 plan Design Choice 4 (one callback for all three signals); the options are fixed at startup |
| C8 | Under either option of Dilemma 1 the check is a singleton with no scoped dependency, registered by `AddEncinaOpenTelemetry` only when `AddOtlpHealthCheck` is on, resolving `TimeProvider` and `ILogger<>` optionally, and proven by a DI test with `ValidateOnBuild` and `ValidateScopes`; the registration form follows Dilemma 1: option A is the typed descriptor `ServiceDescriptor.Singleton<IEncinaHealthCheck, OtlpExporterHealthCheck>(factory)` through `TryAddEnumerable` (the one-type-argument form with a factory has the service type as its implementation type and `TryAddEnumerable` rejects it), option B a `HealthCheckRegistration` | Settled | `AGENTS.md` section 3 (registration completeness) |
| C9 | No tenant tag and no tenant-qualified id: the check is infrastructure-wide and has no tenant data | Settled | ADR-054 D4 (a tenant tag exists only where tenant data exists) |
| C10 | The probe is read-only and fires no telemetry round trip (a TCP connect and an optional `HEAD`); any span, metric or log that Dilemmas 3 and 5 add carries only fixed values | Settled | #1049 acceptance criterion 5; ADR-054 settled point 2 |
| C11 | The thresholds are validated at startup: an internal `IValidateOptions<OtlpHealthCheckOptions>` registered with `AddOptions<OtlpHealthCheckOptions>().ValidateOnStart()` rejects a non-positive `Timeout`, a non-positive `DegradedThreshold` and a `DegradedThreshold` not less than `Timeout`; the check's public constructor repeats the range checks for a caller that builds it directly (`ArgumentOutOfRangeException`). `ValidateOnBuild` does not run factories, so the options validator, not the constructor, is the startup gate | Trivial | The existing practice of `ValidateOnStart` in other `src` packages (for example `src/Encina.AmazonSQS/ServiceCollectionExtensions.cs` and `src/Encina.Cdc.Debezium/ServiceCollectionExtensions.cs`) |

---

## Implementation Phases

One pull request of 15 files (about 1100 lines including tests; if the line count passes 1500, the README, the changelog fragment and the justification file move to a second pull request): 5 new production files (`Health/OtlpExporterHealthCheck.cs`, `Health/OtlpHealthCheckOptions.cs`, `Health/OtlpHealthCheckOptionsValidator.cs`, `Health/OtlpHealthCheckLogMessages.cs`, `Health/OtlpHealthCheckTelemetry.cs`), 2 modified (`EncinaOpenTelemetryOptions.cs`, `ServiceCollectionExtensions.cs`), `PublicAPI/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.OpenTelemetry.json`, 3 test files (unit check tests, DI registration test, guard tests), 1 justification file (integration), the package README and the changelog fragment. Phase 1 is gated on the code of #1043 being on `main`.

### Phase 1: Options and the Probe

> **Goal**: The options, the check and its result mapping.

<details>
<summary><strong>Tasks</strong></summary>

1. **Gate**: confirm `EncinaOpenTelemetryOptions.EnableOtlpExporter` and `ConfigureOtlpExporter` exist on `main` (#1043); if not, stop and report
2. **`src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs`** (modify) — `bool AddOtlpHealthCheck { get; set; } = false` and `Action<OtlpHealthCheckOptions>? ConfigureOtlpHealthCheck { get; set; }` with XML docs
3. **`src/Encina.OpenTelemetry/Health/OtlpHealthCheckOptions.cs`** (new, public sealed) — `TimeSpan Timeout = 2 s`, `TimeSpan DegradedThreshold = 500 ms`, `bool ProbeHttpEndpoint = false`
4. **`src/Encina.OpenTelemetry/Health/OtlpExporterHealthCheck.cs`** (new, public sealed, `IEncinaHealthCheck`) — `public const string DefaultName`, `public static readonly string[] Tags`; public constructor `(EncinaOpenTelemetryOptions options, OtlpHealthCheckOptions healthOptions, TimeProvider? timeProvider = null, ILogger<OtlpExporterHealthCheck>? logger = null)` with `ArgumentNullException.ThrowIfNull` and the range checks of Design Choice 2; an internal constructor adds a probe delegate `Func<string, int, bool, CancellationToken, Task>` (host, port, http flag) for tests (a public constructor never takes an internal type); `CheckHealthAsync(CancellationToken)` split into `ResolveTarget`, `ProbeAsync` and `Classify` so every method stays at CRAP 10 or below: a linked token from `_timeProvider.CreateCancellationTokenSource(Timeout)`, latency from `GetTimestamp` and `GetElapsedTime`, `OperationCanceledException` rethrown when the caller's token is cancelled and mapped to `Unhealthy` ("timeout") when the probe's token fired, `SocketException` mapped by `SocketError` to `refused`, `dns` or `error`
5. **`src/Encina.OpenTelemetry/Health/OtlpHealthCheckOptionsValidator.cs`** (new, internal sealed, `IValidateOptions<OtlpHealthCheckOptions>`) — the startup rules of C11 (returns `ValidateOptionsResult.Fail` naming the property and the condition, never a value that could carry data)
6. **`PublicAPI.Unshipped.txt`** (`mechanical-fixer`) — every new public member

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1049: the OTLP collector reachability health check.

CONTEXT:
- IEncinaHealthCheck (Encina.Messaging.Health) has Name, Tags and CheckHealthAsync(CancellationToken); implement it directly, never through
  EncinaHealthCheck (that base turns cancellation into Unhealthy).
- EncinaOpenTelemetryOptions is a plain singleton instance (services.TryAddSingleton(options)), not IOptions. #1043 adds EnableOtlpExporter and
  ConfigureOtlpExporter (Action<OtlpExporterOptions>).
- The endpoint is resolved once from a fresh OtlpExporterOptions passed through ConfigureOtlpExporter and cached.

TASK:
Add the two options properties, OtlpHealthCheckOptions and OtlpExporterHealthCheck as in the plan. Reachable within DegradedThreshold is Healthy,
slower is Degraded, refused, DNS failure or the probe's own timeout is Unhealthy, exporter disabled is Healthy "OTLP exporter not configured".

KEY RULES:
- Never put the endpoint in the result data, a tag or a log. Never use EncinaError.Message or an exception message.
- A caller's cancellation propagates; only the probe timeout (TimeProvider.CreateCancellationTokenSource) is Unhealthy.
- Time from TimeProvider only. XML docs on every public member; PublicAPI lines through mechanical-fixer; zero warnings.

REFERENCE FILES:
- src/Encina.Messaging/Health/IEncinaHealthCheck.cs
- src/Encina.OpenTelemetry/Resharding/ReshardingHealthCheck.cs
- src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs
```

</details>

### Phase 2: Registration

> **Goal**: `AddOtlpHealthCheck` registers the check, and only it.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ServiceCollectionExtensions.cs`** (modify) — inside `AddEncinaOpenTelemetry`, when `options.AddOtlpHealthCheck`: `services.AddOptions<OtlpHealthCheckOptions>().Configure(o => options.ConfigureOtlpHealthCheck?.Invoke(o)).ValidateOnStart()` with the internal validator `Health/OtlpHealthCheckOptionsValidator.cs` (C11), and `services.TryAddEnumerable(ServiceDescriptor.Singleton<IEncinaHealthCheck, OtlpExporterHealthCheck>(sp => new OtlpExporterHealthCheck(options, sp.GetRequiredService<IOptions<OtlpHealthCheckOptions>>().Value, sp.GetService<TimeProvider>(), sp.GetService<ILogger<OtlpExporterHealthCheck>>())))` (the typed descriptor; Design Choice 1 option A)
2. With Design Choice 3 option A: `tracing.AddSource(OtlpHealthCheckTelemetry.SourceName)` in `WithEncina`
3. A DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`, with the flag on and off, resolves `IEnumerable<IEncinaHealthCheck>` and asserts one instance after a second `AddEncinaOpenTelemetry` call (the typed descriptor must not throw); a second test starts a host with an invalid threshold and asserts that startup fails with an `OptionsValidationException` (C11)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1049: the opt-in registration of the OTLP health check.

CONTEXT:
- AddEncinaOpenTelemetry (src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs) registers the options instance and decorators.
- Provider packages register health checks as IEncinaHealthCheck singletons; the check has no scoped dependency.

TASK:
Register the check when AddOtlpHealthCheck is true, with TryAddEnumerable and the TYPED descriptor ServiceDescriptor.Singleton<IEncinaHealthCheck, OtlpExporterHealthCheck>(factory)
so a second call adds nothing and TryAddEnumerable does not throw; add the options validator with ValidateOnStart; prove both with DI tests.

KEY RULES:
- Default off (pay-for-what-you-use). TimeProvider and ILogger are optional dependencies.
- Registration completeness: the DI test uses ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs
```

</details>

### Phase 3: Cross-Cutting Integration

> **Goal**: The matrix functions marked included are in place.

<details>
<summary><strong>Tasks</strong></summary>

1. **Health Checks (4)** — Phases 1 and 2
2. **Resilience (6)** — not a policy: the probe has its own timeout and fails visibly
3. **Validation (5)** — constructor range checks of Design Choice 2
4. **Fail-closed** — an unreachable or unresolvable endpoint is `Unhealthy`, never `Healthy`; the only `Healthy` shortcut is the exporter being off (C5)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1049: cross-cutting verification.

CONTEXT:
- A failure to reach or resolve the endpoint must never read as Healthy.

TASK:
Add unit tests for an endpoint that cannot be parsed (Unhealthy), a closed loopback port (Unhealthy "refused"), and an exporter that is off (Healthy).

KEY RULES:
- Real loopback sockets for the reachable and refused cases (TcpListener on port 0); a fake delegate only for timeout and slow paths.

REFERENCE FILES:
- tests/Encina.UnitTests/OpenTelemetry/Resharding/ReshardingHealthCheckTests.cs
```

</details>

### Phase 4: Observability

> **Goal**: The probe's span and its two log messages (Design Choices 3 and 5).

<details>
<summary><strong>Tasks</strong></summary>

1. **`Health/OtlpHealthCheckTelemetry.cs`** (new, internal static) — `SourceName = "Encina.OpenTelemetry"`, an `ActivitySource`, `StartProbe()` returning `null` without a listener, and the tag constants for the outcome and the reason (kept inside this class; `ActivityTagNames.cs` is not edited, so the pull request stays at 15 files)
2. **`Health/OtlpHealthCheckLogMessages.cs`** (new, internal static partial) — `[LoggerMessage]` methods: `EventId = 7016` Warning `OtlpCollectorUnhealthy(ILogger, string reason, long latencyMs)`; `EventId = 7017` Information `OtlpCollectorRecovered(ILogger)` (the ids are the first free ones after 7015 on `main` today and are re-checked against `docs/plans` when the pull request opens); XML docs "Event IDs: 7016-7017 (see EventIdRanges.OpenTelemetry)". No registration is needed: the ids sit inside the registered range, and `EncinaEventIdAllocationTests` already maps the assembly
3. Transition tracking with an `Interlocked.Exchange` on an `int` field; the run of the EventId architecture tests

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1049: the probe span and two log messages.

CONTEXT:
- Encina.OpenTelemetry owns the registered range 7000-7099; 7000-7009 are used and plans on main claim 7010-7015 (#622, #174, #183). This feature takes the next two free ids, 7016-7017 today, re-checked when the pull request opens.
- ADR-054: one ActivitySource per package named Encina.*, HasListeners() guard, error status carries a code or type only.

TASK:
Add the telemetry class and the log messages and use them in the check; log only on a status change.

KEY RULES:
- Never log the endpoint, EncinaError.Message or an exception message; the reason is one of timeout, refused, dns, error.
- EventIds packed (7016, 7017), no gaps; run tests/Encina.UnitTests EncinaEventIdAllocationTests and EventIdRegistryAgreementTests.

REFERENCE FILES:
- src/Encina.OpenTelemetry/Resharding/ReshardingLogMessages.cs
- src/Encina.OpenTelemetry/Resharding/ReshardingActivitySource.cs
- docs/engineering/eventid-block-registry.md
```

</details>

### Phase 5: Testing

> **Goal**: Unit and guard flags at their targets; integration justified.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/OpenTelemetry/Health/OtlpExporterHealthCheckTests.cs`, `OtlpHealthCheckRegistrationTests.cs`): reachable and fast is `Healthy`; reachable and slower than the threshold (a delegate that advances a `FakeTimeProvider`) is `Degraded`; refused (closed loopback port) and timeout are `Unhealthy`; the exporter off is `Healthy`; a cancelled caller token throws `OperationCanceledException` and is not `Unhealthy`; the HTTP `HEAD` probe treats `405` as reachable; transitions log once; the result data never contains the endpoint; the registration tests of Phase 2
2. **Guard** (`tests/Encina.GuardTests/Infrastructure/OpenTelemetry/OtlpExporterHealthCheckGuardTests.cs`): null `options` and null `healthOptions`; `Timeout` zero or negative, `DegradedThreshold` zero or not less than `Timeout` throw `ArgumentOutOfRangeException`; the nullable `timeProvider` and `logger` accept `null` (a test of the valid result)
3. **Contract, property** — not applicable: the contract is the existing interface; no complex invariant
4. **Integration** — justification file `tests/Encina.IntegrationTests/Observability/OpenTelemetry/OtlpExporterHealthCheck.md` (no database storage; the reachable and refused paths run against real loopback sockets in the unit tests; the collector stack of `docker-compose.observability.yml` is not started by the suite)
5. **Targets** (manifest, written by `mechanical-fixer`, measured by the PR): `Health/OtlpExporterHealthCheck.cs` unit 90 (every branch is driven by a loopback socket or the probe delegate) and guard 12 (the constructor checks are the reachable lines); `Health/OtlpHealthCheckOptions.cs` unit 100 and guard 0 (auto-properties with no parameters; validation lives in the check's constructor); `Health/OtlpHealthCheckTelemetry.cs` unit 80 (the span helper runs with and without a listener), guard 0 (a static class whose methods take no reference-type or validated parameters); `Health/OtlpHealthCheckOptionsValidator.cs` unit 100 (three rules and the valid case) and guard 0 (an internal class whose only method takes the options instance the framework supplies; no null check is written); `Health/OtlpHealthCheckLogMessages.cs` no flags (source-generated, as `ReshardingLogMessages.cs`); `EncinaOpenTelemetryOptions.cs` unit 100; `ServiceCollectionExtensions.cs` unit 90 and guard 35 unchanged (the new branch is covered by the registration tests); no target is lowered

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1049: tests of the OTLP health check.

CONTEXT:
- Tests execute the real check; Shouldly through Encina.Testing.Shouldly; FakeTimeProvider for time.
- Coverage is per flag against .github/coverage-manifest/Encina.OpenTelemetry.json; a target never goes down.

TASK:
Write the tests of the Phase 5 tasks, measure the unit and guard coverage of the touched files with
`dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests` (and GuardTests) and report the per-file table.

KEY RULES:
- No Thread.Sleep; a test never swallows an exception of the code under test; no hard-coded fixed port (listen on port 0).
- Justification .md only for the integration flag, never for unit or guard.

REFERENCE FILES:
- tests/Encina.UnitTests/OpenTelemetry/Resharding/ReshardingHealthCheckTests.cs
- tests/Encina.GuardTests/Infrastructure/OpenTelemetry/HealthCheckGuardTests.cs
- .github/coverage-manifest/Encina.OpenTelemetry.json
```

</details>

### Phase 6: Documentation and Finalization

> **Goal**: Operators know how to enable and read the check.

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on every new public type and member, with an `<example>` on the check
2. `src/Encina.OpenTelemetry/README.md` through `docs-writer`: a "Health Checks" section (enable flag, thresholds, what each status means, that the endpoint is not reported and per-signal variables are not read)
3. `changelog.d/1049-otlp-health-check.added.md` (written by `mechanical-fixer`)
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test`; every flag at its target; CRAP <= 10 on changed methods

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are finishing issue #1049: documentation and verification.

CONTEXT:
- Documentation goes through docs-writer and the encina-docs skill; the changelog fragment and PublicAPI lines through mechanical-fixer.

TASK:
Complete the Phase 6 tasks and report the per-flag figures of the touched files against their targets.

KEY RULES:
- English only; no private client application named; state plainly that Healthy with "not configured" means the exporter is off.
- Zero warnings; no [Obsolete].

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- src/Encina.OpenTelemetry/README.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| OTLP specification | Receivers listen on 4317 (gRPC) and 4318 (HTTP, paths `/v1/traces`, `/v1/metrics`, `/v1/logs`) | Default ports and the optional `HEAD` probe |
| ADR-054 | Settled points 1, 2 and 7; D3 one source per package | The ground for Design Choices 3 and 5 and register C3 |
| `AGENTS.md` section 3 | Cancellation propagates; credentials never printed; registration completeness | Register C3, C4 and C8 |
| `AGENTS.md` section 6, row 4 | Health checks through `IEncinaHealthCheck` | Design Choice 1 |
| ADR-062 | `ErrorCategory` of a created error | None created (C1) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IEncinaHealthCheck`, `HealthCheckResult` | `src/Encina.Messaging/Health/` | The contract and the result |
| `EncinaHealthCheckAdapter`, `HealthCheckBuilderExtensions` | `src/Encina.AspNetCore/Health/` | Consume the registered checks |
| `ReshardingHealthCheck`, `SchemaDriftHealthCheck` | `src/Encina.OpenTelemetry/` | Style reference for options and `TimeProvider` |
| `AddEncinaOpenTelemetry`, `EncinaOpenTelemetryOptions` | `src/Encina.OpenTelemetry/` | Registration site and the options |
| `ReshardingLogMessages`, `ReshardingActivitySource` | `src/Encina.OpenTelemetry/Resharding/` | Log and source style (new code uses `[LoggerMessage]`) |
| `docker-compose.observability.yml` | `.github/observability/` | Local collector on 4317 and 4318 (manual checks, not in the suite) |
| OTLP exporter plan | `docs/plans/otlp-exporter-implementation-plan-1043.md` (#1043) | `EnableOtlpExporter` and `ConfigureOtlpExporter` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.OpenTelemetry` | 7016-7017 inside the registered `OpenTelemetry = (7000, 7099)` | 7000-7009 used, 7010-7015 claimed by the plans of #622, #174 and #183, 7018-7099 unclaimed today; re-check when the pull request opens; no registration and no registry edit; the block 1000-1049 assigned to this family is not needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 5 | Check, options, options validator, log messages, telemetry |
| Production (modified) | 2 | `EncinaOpenTelemetryOptions.cs`, `ServiceCollectionExtensions.cs` |
| PublicAPI and manifest | 2 | `PublicAPI.Unshipped.txt`, `Encina.OpenTelemetry.json` |
| Tests | 3 | Check tests plus registration tests (one file each), guard tests |
| Justification | 1 | Integration |
| Documentation and changelog | 2 | README, fragment |
| **Total** | **15** | One pull request, about 1100 lines; at most 15 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1049: the OTLP collector reachability health check of Encina.OpenTelemetry.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- Starts after #1043 is on main (EnableOtlpExporter, ConfigureOtlpExporter). IEncinaHealthCheck lives in Encina.Messaging.Health.

IMPLEMENTATION OVERVIEW:
1. Options: AddOtlpHealthCheck (default false) and ConfigureOtlpHealthCheck on EncinaOpenTelemetryOptions; OtlpHealthCheckOptions (Timeout 2 s,
   DegradedThreshold 500 ms, ProbeHttpEndpoint false).
2. OtlpExporterHealthCheck : IEncinaHealthCheck (direct, never EncinaHealthCheck): resolve the endpoint once, TCP connect (optional HTTP HEAD, any
   response is reachable) with a timeout from TimeProvider; Healthy, Degraded, Unhealthy; exporter off is Healthy "not configured"; caller
   cancellation propagates.
3. Registration in AddEncinaOpenTelemetry with TryAddEnumerable as a singleton; a span per probe on source Encina.OpenTelemetry; log messages 7016 and
   7017 on transitions only (inside the registered range 7000-7099).
4. Tests: loopback sockets for reachable and refused, FakeTimeProvider for slow and timeout, guard tests, DI with ValidateOnBuild and ValidateScopes;
   integration justification; README through docs-writer; fragment, PublicAPI and manifest through mechanical-fixer.

KEY PATTERNS:
- Never report the endpoint (data, tag, log). Never log EncinaError.Message or exception messages. Bounded tag values only.
- Methods small enough for CRAP <= 10. Targets never go down.

REFERENCE FILES:
- docs/plans/otlp-health-check-implementation-plan-1049.md
- src/Encina.Messaging/Health/IEncinaHealthCheck.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- .github/coverage-manifest/Encina.OpenTelemetry.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A probe result must be current; the health-check framework already caches nothing here |
| 2 | OpenTelemetry | ✅ | Phase 4: a span per probe (Design Choice 3) |
| 3 | Structured Logging | ✅ | Phase 4: EventIds 7016-7017 on transitions |
| 4 | Health Checks | ✅ | This plan is the health check (Phases 1 and 2) |
| 5 | Validation | ✅ | Phase 1: constructor range checks of the thresholds |
| 6 | Resilience | ❌ | The probe has its own timeout and reports the failure; no retry policy is added |
| 7 | Distributed Locks | ❌ | Stateless apart from one in-process transition field |
| 8 | Transactions | ❌ | Stateless |
| 9 | Idempotency | ❌ | Read-only probe |
| 10 | Multi-Tenancy | ❌ | Infrastructure-wide; no tenant data (C9) |
| 11 | Module Isolation | ❌ | Cross-cutting telemetry |
| 12 | Audit Trail | ❌ | Operational, not auditable |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#1043](https://github.com/dlrivada/Encina/issues/1043) — OTLP exporter opt-in | Open; plan on `main`, no code | Phase 1 gate: the two exporter properties must exist |
| [#1048](https://github.com/dlrivada/Encina/issues/1048) — logs wiring | Open; plan written | Independent; the probe does not need the logs signal |
| [#622](https://github.com/dlrivada/Encina/issues/622) — health checks plan | Open; plan on `main` | Takes EventId 7010 of the same range (with #174 7010-7012 and #183 7013-7015); this plan takes the next free two ids (7016-7017 today), re-checked whichever merges first |
| [#1787](https://github.com/dlrivada/Encina/issues/1787) — every source subscribed | Open | The new source `Encina.OpenTelemetry` is subscribed in `WithEncina` in Phase 2 |

---

## Next Steps

1. Link this plan from issue #1049; the worker brief follows once #1043 is implemented.
2. When work on the issue starts, ask the maintainer to answer Dilemmas 1 to 5 (health-check contract; threshold configuration; probe telemetry; HTTP client; log cadence), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices.
3. One commit per phase; the last commit references `Fixes #1049`.
