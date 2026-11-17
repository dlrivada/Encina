# Implementation Plan: `Encina.Aspire.ServiceDefaults` — `AddEncinaDefaults` for the Aspire service defaults project

> **Issue**: [#417](https://github.com/dlrivada/Encina/issues/417)
> **Type**: Feature
> **Complexity**: Medium (6 phases, 3 pull requests of 11, 15 and 7 files)
> **Estimated Scope**: ~500 lines of production code + ~1,000 lines of tests
> **Family**: Aspire (aspire-1): #416 hosting package and the environment contract (writer), #419 dashboard commands, #421 deployment, #440 umbrella and naming owner; the family continues in #424, #425, #382, #387 and #391. **This plan owns the application-side package, the reader of the environment contract, the declared-pattern start check and the `AspireServiceDefaults` EventId range.**

---

## Summary

Create `Encina.Aspire.ServiceDefaults`, the application-side package: one call, `AddEncinaDefaults`, that a solution's own service defaults project makes next to Aspire's `AddServiceDefaults`. State of `origin/main` (`ec129cae5`), verified by reading and grep:

| Item of the issue | Today | Owner of what remains |
|-------------------|-------|-----------------------|
| OpenTelemetry tracing and metrics of Encina | `AddEncinaOpenTelemetry(Action<EncinaOpenTelemetryOptions>?)` and `OpenTelemetryBuilder.WithEncina(EncinaOpenTelemetryOptions?)` in `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` (`WithEncina` subscribes 21 sources and the `Encina` meter, `:181-213`); `AddEncinaInstrumentation()` for a tracer or meter builder subscribes only `Encina` (`:288`, `:301`) | **This plan** wires it; the exporter is not wired (Design Choice 5) |
| Health checks for the messaging patterns | `AddEncinaOutbox`, `AddEncinaInbox`, `AddEncinaSaga`, `AddEncinaScheduling`, the composite `AddEncinaHealthChecks` and the generic `AddEncinaHealthCheck<T>` in `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs`; `DeadLetterHealthCheck` exists (`src/Encina.Messaging/Health/DeadLetterHealthCheck.cs`) with no builder extension | **This plan** registers them from the declaration |
| `AddEncinaOutboxHealthCheck` and siblings of the issue sketch | Do not exist; the real names are the ones above | Names corrected here |
| `AddEncinaResilienceHandler` | Does not exist; `AddEncinaStandardResilience` (`src/Encina.Extensions.Resilience`) is a handler pipeline behavior, not an HTTP handler | Design Choice 8 |
| Service discovery | Aspire's template already calls `AddServiceDiscovery`; the Encina abstraction and its Aspire provider are #607 (`AddEncinaServiceDiscoveryAspire`) | #607 |
| Per-pattern endpoints `/health/outbox` and siblings | Missing | Design Choice 7 (not delivered) |
| Environment contract written by the AppHost | Planned by #416 | **This plan** reads it |

Differences from the issue text found while reading the code: the sketch passes no options to `WithEncina`, but `AddEncinaOpenTelemetry` stores its own options instance and `WithEncina` builds its own, so the service name set through one does not reach the resource (the finding is recorded in the #188 plan); this plan therefore passes the options to `WithEncina` directly. The Aspire template already wires the OTLP exporter and standard HTTP resilience.

**Delivered**: package `Encina.Aspire.ServiceDefaults`; `AddEncinaDefaults(this IHostApplicationBuilder builder, Action<EncinaDefaultsOptions>? configure = null)`; `EncinaDefaultsOptions` (five pattern flags, `ServiceName`, `EnableTelemetry`, `EnableHealthChecks`); a reader of `Encina:Aspire:Patterns:*` (the keys #416 writes as `Encina__Aspire__Patterns__*`); registration of `WithEncina` telemetry; registration of the health check of each declared pattern; a hosted start check that fails the host when a declared pattern has no store; log messages 800-803.

**Not in scope**: the exporter and the template's `UseOtlpExporter` (the application's, Design Choice 5); route mapping (`MapDefaultEndpoints` stays in the template, `MapEncinaHealthChecks` is #454); service discovery (#607); outbound HTTP resilience (#1233); commands (#419); publishers (#421).

**Affected packages**: `Encina.Aspire.ServiceDefaults` (new); `Encina` (core: one EventId range and its PublicAPI line); `Encina.OpenTelemetry`, `Encina.AspNetCore` and `Encina.Messaging` are referenced, not changed.

**Provider category**: none. The start check reads whether a store is registered; it does not touch a database, so the 10-provider matrix does not apply, and one integration test with the in-memory fakes shows the check across every pattern.

---

## Design Choices

<details>
<summary><strong>1. Where the code lives (Dilemma D1, the same answer as #440 D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A dedicated package `Encina.Aspire.ServiceDefaults`** (the issue's name) | Applications and the AppHost take only what they use; the package depends on the application-side Encina packages and the AppHost package does not | One more package |
| **B) A folder `ServiceDefaults/` in an umbrella `Encina.Aspire` package (the #440 sketch)** | One application-side package for the whole Aspire family | The package would reference `Encina.OpenTelemetry`, `Encina.AspNetCore` and `Encina.Messaging`, so a future AppHost-side addition in it would pull them in |
| **C) Add the method to `Encina.OpenTelemetry` or `Encina.AspNetCore`** | No new package | Both packages would reference `Microsoft.Extensions.Hosting` abstractions and Aspire concepts they do not need |

### Chosen Option: **A — a dedicated package** (recommended, pending the maintainer)

### Rationale

A wins because the declaration contract and the start check are Aspire-specific, and the cost of a package is one project and one manifest. B wins if the maintainer wants a single application-side Aspire package and accepts that it grows with the family. C is rejected: it would put Aspire vocabulary into two general packages.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Name of the entry point (Dilemma D2, owned by #440 D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaDefaults`, the name of the #417 issue; it sits beside `AddServiceDefaults` and follows the `AddEncina*` rule** | Reads as "the Encina part of the service defaults" | The #449 triage comment asked to carry `AddEncinaAspireDefaults` into #440 |
| **B) `AddEncinaAspireDefaults`, the name carried from #449 into #440** | Says it is Aspire-specific | Longer; the package namespace already says Aspire |

### Chosen Option: **A — `AddEncinaDefaults`** (recommended, pending the maintainer)

### Rationale

Having both names would be a compatibility alias, which AGENTS.md section 3 forbids, so exactly one exists. A wins on brevity and because two issues name it; B wins if the maintainer prefers the name that the closed duplicate #449 proposed. The answer is given once for #417 and #440.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What decides which health checks exist (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The declaration decides: patterns named by `Encina:Aspire:Patterns:*` (written by #416's `WithEncina`) or by `EncinaDefaultsOptions`; each declared pattern gets its health check** | A check is registered only for a pattern that exists (`AddEncinaOutbox` resolves `IOutboxStore` with `GetRequiredService`, so a check for an absent store would always be unhealthy); no work for a pattern the AppHost did not declare; typed configuration | An application without the AppHost declares in code (`configure`) |
| **B) Detect: one lazy check that looks up each store when it runs and reports only those present** | Needs no declaration; works in tests | New check logic that duplicates the four existing checks and their thresholds |
| **C) Register all five checks always** | Simplest | An application that does not use inbox or sagas starts Unhealthy |

### Chosen Option: **A — the declaration decides** (recommended, pending the maintainer)

### Rationale

A reuses the existing checks and their options and fails visibly when the declaration is wrong (Design Choice 4). B wins for an application that is never run under an AppHost and wants zero configuration; its code would need its own thresholds. C is rejected by the code: `AddEncinaOutbox` throws when `IOutboxStore` is missing, so a check for an unused pattern reports Unhealthy for ever.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. A declared pattern with no store stops the host (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A hosted start check resolves each declared pattern's store in a scope; a missing store throws `InvalidOperationException` from `StartAsync`, so the host does not start; the log carries the code and the pattern name** | The declared architecture and the code cannot drift silently; fail closed (AGENTS.md section 3) | A developer who declared a pattern in the AppHost and forgot the code sees the host fail |
| **B) Log an error and let the pattern's health check report Unhealthy** | The service starts | A broken declaration is only visible in the dashboard |

### Chosen Option: **A — fail the start** (recommended, pending the maintainer)

### Rationale

A wins because a declaration that is false means an operator is told the application runs outbox when it does not. B wins for a team that wants the service up while the AppHost is being edited. The check uses `IServiceScopeFactory.CreateScope()` because the stores are scoped and the hosted service is a singleton (never inject a scoped service into a singleton).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Telemetry: `WithEncina`, and no exporter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddOpenTelemetry().WithEncina(options)` plus `AddEncinaOpenTelemetry` for the metrics initializers; no exporter (the template's `UseOtlpExporter` and the AppHost's `OTEL_EXPORTER_OTLP_ENDPOINT` deliver to the dashboard)** | All Encina sources and the meter reach the dashboard; the library does not choose exporters | The application must keep an exporter (the template has one) |
| **B) `AddEncinaInstrumentation()` on the tracer and meter builders** | Smaller code | Subscribes only the `Encina` source (`:288`); the outbox, inbox, saga, scheduling and dead letter sources would not be traced, which fails the issue's "tracing integration with Encina activities" for the messaging patterns |
| **C) Also call `UseOtlpExporter()`** | Works in a project that has no exporter | A second exporter in a template project duplicates every signal; a library must not pick exporters |

### Chosen Option: **A — `WithEncina`, no exporter**

### Rationale

The messaging sources are subscribed only by `WithEncina` (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:181-213`), and ADR-054 settled point 1 keeps exporters out of the library: the application chooses the SDK and exporters.

**Class:** Settled
**Settled by:** ADR-054 settled point 1 (exporters stay out of `WithEncina()`; the application chooses them); `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:181-213` against `:288` (the sources each entry point subscribes).

</details>

<details>
<summary><strong>6. A second call</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A second `AddEncinaDefaults` on the same builder throws `InvalidOperationException`** | The precedent of the repository; no double registration of hosted services and checks | None found |
| **B) Silently ignore it** | Never throws | Hides a configuration mistake |

### Chosen Option: **A — throw**

### Rationale

`WithEncinaTestSupport` throws on a second call and its registration test pins that behavior.

**Class:** Trivial
**Settled by:** `src/Encina.Aspire.Testing/DistributedApplicationTestingBuilderExtensions.cs` (`WithEncinaTestSupport` second-call `InvalidOperationException`) and `tests/Encina.UnitTests/Aspire/Testing/WithEncinaTestSupportRegistrationTests.cs`.

</details>

<details>
<summary><strong>7. Probe endpoints and per-pattern routes (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Map nothing here: the template's `MapDefaultEndpoints` serves `/health` and `/alive` over the tags, and the Encina probes are #454's `MapEncinaHealthChecks`; the per-pattern checks carry tags (`outbox`, `inbox`, `saga`, `scheduling`, `deadletter`) so a host can build its own routes** | No unauthenticated endpoint multiplies (each route would expose a backlog state); no overlap with #454 | The issue's `/health/outbox`, `/health/inbox`, `/health/sagas` and `/health/scheduler` routes are not delivered |
| **B) Map the four per-pattern routes** | Matches the issue sketch | A package that registers services cannot map routes (it needs a `WebApplication`), so it adds a second method; the routes duplicate #454's endpoint family |

### Chosen Option: **A — tags, no new routes** (recommended, pending the maintainer)

### Rationale

A wins because the dashboard already lists each registered check by name under the resource and #454 owns the endpoint family. B wins for an operator who wants to curl one pattern.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Resilience is not part of the defaults (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not delivered: Aspire's template already adds the standard HTTP resilience handler; Encina's handler-level resilience (`AddEncinaStandardResilience`) stays the application's choice; outbound Encina HTTP is #1233** | Pay-for-what-you-use; no overlap | The issue's "Standard Resilience pipeline integration" is not delivered |
| **B) An option `EnableStandardResilience` that calls `AddEncinaStandardResilience()`** | One call enables handler resilience | A behavior that changes every request's pipeline behind a defaults call; it needs its own options |

### Chosen Option: **A — not delivered** (recommended, pending the maintainer)

### Rationale

A wins because the issue's own matrix marks resilience deferred and `AddEncinaResilienceHandler` does not exist; B wins for a team that wants a one-line baseline.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package, options and the declaration reader (pull request 1)

> **Goal**: The package exists and the declaration (`Encina:Aspire:Patterns:*` plus the options) is read and validated.

<details>
<summary>Tasks</summary>

1. `src/Encina.Aspire.ServiceDefaults/Encina.Aspire.ServiceDefaults.csproj` (`PackageId`, XML docs, `ProjectReference` to `Encina.OpenTelemetry`, `Encina.AspNetCore` and `Encina.Messaging`, `InternalsVisibleTo` for the unit, guard and contract test projects, PublicAPI files) and the project entry in `Encina.slnx`.
2. `EncinaDefaultsOptions.cs` (public sealed): `Outbox`, `Inbox`, `Sagas`, `Scheduling`, `DeadLetterQueue` (bool), `ServiceName` (`string?`), `EnableTelemetry` (default true), `EnableHealthChecks` (default true).
3. `EncinaAspireConfiguration.cs` (internal static): the keys `Encina:Aspire:Name` and `Encina:Aspire:Patterns:{Outbox|Inbox|Sagas|Scheduling|DeadLetterQueue}` (the colon form of the AppHost's `Encina__Aspire__*` variables), and `Read(IConfiguration)` returning the declared patterns; a value that is not `true` or `false` (case-insensitive) is an invalid declaration that throws `InvalidOperationException` naming the key (never the value); an absent key is "not declared".
4. Tests: unit for the reader (each key, absent, `true`/`false`/`True`, an invalid value, the exception names the key and not the value); guard; the two project references of `Encina.UnitTests.csproj` and `Encina.GuardTests.csproj`; `PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.Aspire.ServiceDefaults.json` through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #417; plan docs/plans/aspire-service-defaults-implementation-plan-417.md. D1 to D4, D7 and D8 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- #416 writes the environment variables Encina__Aspire__Name and Encina__Aspire__Patterns__{Outbox,Inbox,Sagas,Scheduling,DeadLetterQueue}; .NET maps a double underscore to a colon.

TASK:
Create the package, EncinaDefaultsOptions and the declaration reader with their tests.

KEY RULES:
- The reader is pure and has no side effects; an invalid declaration throws naming the key, never the value.
- Absent configuration means "not declared"; it never means "all patterns".
- XML docs with examples; PublicAPI.Unshipped.txt; zero warnings.

REFERENCE FILES:
- src/Encina.Aspire.Testing/Encina.Aspire.Testing.csproj
- src/Encina.Messaging/MessagingConfiguration.cs
- tests/Encina.UnitTests/Aspire
```

</details>

---

### Phase 2: `AddEncinaDefaults` and the start check (pull request 2)

> **Goal**: One call registers telemetry and the health checks of the declared patterns, and the host fails to start when a declared pattern has no store.

<details>
<summary>Tasks</summary>

1. `EncinaDefaultsExtensions.AddEncinaDefaults(this IHostApplicationBuilder builder, Action<EncinaDefaultsOptions>? configure = null)`: guards; a second call throws (Design Choice 6, recorded with a marker singleton in `builder.Properties`); merges the declaration read from `builder.Configuration` with the options (`configure` runs after the read and may add patterns; it never removes a declared one); when `EnableTelemetry`, `builder.Services.AddEncinaOpenTelemetry()` and `builder.Services.AddOpenTelemetry().WithEncina(new EncinaOpenTelemetryOptions { ServiceName = ... })` where the service name is the option, else `builder.Configuration["OTEL_SERVICE_NAME"]` (what the AppHost injects), else `builder.Environment.ApplicationName`; when `EnableHealthChecks`, `AddHealthChecks()` then `AddEncinaOutbox()`, `AddEncinaInbox()`, `AddEncinaSaga()`, `AddEncinaScheduling()` for the declared patterns and `AddEncinaHealthCheck<DeadLetterHealthCheck>("encina-deadletter", ["deadletter"])` for a declared dead letter queue; registers the start check. Returns the builder.
2. `EncinaDefaultsStartupValidator.cs` (internal sealed, `IHostedService`): takes the declared patterns and an `IServiceScopeFactory`; in `StartAsync` creates a scope and, for each declared pattern, `GetService` of its store type (`IOutboxStore`, `IInboxStore`, `ISagaStore`, `IScheduledMessageStore`, `IDeadLetterStore`); any absent store logs once per pattern and throws one `InvalidOperationException` listing the pattern names. Failure code, pattern name and nothing else are logged.
3. `Diagnostics/EncinaDefaultsLog.cs` (`[LoggerMessage]`): 800 defaults applied (Information: pattern count, telemetry and health flags), 801 declared pattern has no store (Error: pattern name and code `encina.aspire.defaults.pattern_not_registered`), 802 invalid declaration (Error: key name and code `encina.aspire.defaults.invalid_declaration`), 803 no declaration found (Debug). `EventIdRanges.AspireServiceDefaults = (800, 819)` registered first in `src/Encina/Diagnostics/EventIdRanges.cs`, the `Encina` PublicAPI line, and the `AssemblyRanges` entry of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
4. Tests: unit (each pattern registers its check; undeclared patterns register none; telemetry registers the Encina sources, asserted with a real `MeterListener` and `ActivityListener`; `ServiceName` precedence; `configure` adds patterns; second call throws; the start check passes with the fake stores and fails naming the missing patterns), DI (`ValidateOnBuild` and `ValidateScopes` over a real `HostApplicationBuilder`), guard, and the `Encina.json` manifest, `PublicAPI.Unshipped.txt` and the package manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #417, Phase 1 is merged. D3 (the declaration decides), D4 (fail the start), D5 (WithEncina, no exporter), D6 (second call throws) apply.

TASK:
Add AddEncinaDefaults, the start check, the log class and the EventId range with their tests.

KEY RULES:
- Pass the options to WithEncina: the options given to AddEncinaOpenTelemetry do not reach the resource.
- Never resolve a scoped store from a singleton: use IServiceScopeFactory.
- Register EventIdRanges.AspireServiceDefaults first; ids 800-803 packed; log the code and the pattern name, never an exception message.
- A declared pattern never gets a health check for a store that is not registered.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs
- src/Encina.Messaging/Health/DeadLetterHealthCheck.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 3: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. OpenTelemetry (function 2) and health checks (function 4): Phase 2. Structured logging (function 3): Phase 2 and Phase 4. Validation (function 5): the declaration reader and the start check.
2. Failure codes and their ADR-062 categories (the package creates no `EncinaError`; the codes are logged and carried by exceptions):

   | Code | `ErrorCategory` | Where |
   |------|-----------------|-------|
   | `encina.aspire.defaults.invalid_declaration` | `Validation` | The reader, a value that is not a boolean |
   | `encina.aspire.defaults.pattern_not_registered` | `FailedPrecondition` | The start check, a declared pattern without a store |

3. Tenancy (ADR-054 D4): this package adds no span attribute or metric tag; the tenant tag is emitted by `Encina.OpenTelemetry` when multi-tenancy is on. The health checks it registers do not select a tenant and the package sets no response writer, so no tenant data reaches a response.
4. Deferred: outbound resilience (#1233), service discovery (#607), probe endpoints (#454), modules (#423).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #417; the cross-cutting matrix is the contract.

TASK:
Verify each included row exists in code and tests.

KEY RULES:
- Never log EncinaError.Message or an exception message; log codes and pattern names only.
- Fail closed: a declared pattern without a store stops the host; no silent skip.

REFERENCE FILES:
- AGENTS.md sections 3 and 6
- docs/architecture/adr/062-error-category-contract.md
```

</details>

---

### Phase 4: Observability

<details>
<summary>Tasks</summary>

1. The package defines no `ActivitySource` and no `Meter`; it wires the existing ones through `WithEncina`.
2. `EncinaDefaultsLog` with EventIds 800-803 inside `AspireServiceDefaults = (800, 819)` (16 ids spare), packed with no gaps, with XML docs naming the range; the allocation architecture test passes.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #417. EventIds 800-849 are the block of the Aspire family; this package takes 800-819.

TASK:
Confirm the log class, the registered range and the architecture test.

KEY RULES:
- Use the [LoggerMessage] source generator; EventIds only inside the registered range; no exception message in any log.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 5: Testing

<details>
<summary>Tasks</summary>

1. **Unit, Guard, DI** as in Phases 1 and 2. **Contract**: `tests/Encina.ContractTests/Aspire/EnvironmentContractTests.cs` is owned by #440 (it compares the keys of this package and of #416 once both exist). **Property**: justification file (the reader maps strings to booleans, a table). **Integration**: `tests/Encina.IntegrationTests/Aspire/ServiceDefaultsHostTests.cs` builds a real host with the Encina fake stores and runs the start check and `/health` through the in-process test server; no database, so no provider fixture. **Load** and **Benchmark**: justification files (startup-time logic).
2. **Per-file targets** (measured at work start; a target is the measured value or higher; new files have no baseline):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|:---:|:---:|:---:|:---:|:---:|---------------|
   | `EncinaDefaultsOptions.cs` | 100 | 0 | 0 | 0 | 0 | Auto-properties with no parameterized member; every property is read in the extension tests |
   | `EncinaAspireConfiguration.cs` | 100 | 85 | 80 | 0 | 0 | The reader runs on real `IConfiguration`; guard reaches the null checks; the contract test of #440 reads the keys |
   | `EncinaDefaultsExtensions.cs` | 95 | 90 | 0 | 0 | 80 | Real `HostApplicationBuilder` in unit tests; the host test runs the whole registration |
   | `EncinaDefaultsStartupValidator.cs` | 95 | 70 | 0 | 0 | 80 | Real scopes over fake stores; guard reaches the constructor checks of an internal type through `InternalsVisibleTo` |
   | `Diagnostics/EncinaDefaultsLog.cs` | 90 | 0 | 0 | 0 | 0 | Generated log methods, reached through the start check; no parameterized public member |

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #417. Per-flag coverage per AGENTS.md section 9; targets live in .github/coverage-manifest/Encina.Aspire.ServiceDefaults.json.

TASK:
Write the tests, the manifest entries and the three justification files (property, load, benchmark).

KEY RULES:
- Tests execute the real extension over a real HostApplicationBuilder and never swallow an exception.
- A timer or background failure is never swallowed by a test; the start check throws and the test asserts it.
- Shouldly through Encina.Testing.Shouldly; targets never go down; CRAP <= 10 on every changed method.

REFERENCE FILES:
- tests/Encina.UnitTests/Aspire
- tests/Encina.IntegrationTests/Observability/OpenTelemetry/OpenTelemetryIntegrationTests.cs
- .github/coverage-manifest/Encina.Aspire.Testing.json
```

</details>

---

### Phase 6: Documentation & Finalization (pull request 3)

<details>
<summary>Tasks</summary>

1. XML docs with examples on all public APIs; `PublicAPI.Unshipped.txt`.
2. `src/Encina.Aspire.ServiceDefaults/README.md` and the usage page (through `docs-writer`: add the call to a template ServiceDefaults project, the declaration table, what each pattern registers, the start check, what is not included and who owns it: exporter, routes, resilience, service discovery); `docs/INVENTORY.md` line.
3. `changelog.d/417-aspire-service-defaults.added.md` (through `mechanical-fixer`).
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #417 is implemented and tested.

TASK:
Write the documentation set of the Phase 6 tasks through the specialists that own them.

KEY RULES:
- Pages through docs-writer; fragments, PublicAPI lines and manifests through mechanical-fixer; never edit CHANGELOG.md; coverage figures are cited with covref markers, never typed; the reference application is named only as "the reference application".

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | csproj, `Encina.slnx` edit, `EncinaDefaultsOptions`, `EncinaAspireConfiguration`, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, manifest, reader unit and guard tests, two test csproj edits (Unit, Guard) | 11 | 650 |
| 2 | `EncinaDefaultsExtensions`, `EncinaDefaultsStartupValidator`, `EncinaDefaultsLog`, `EventIdRanges.cs` edit, `Encina` `PublicAPI.Unshipped.txt` edit, allocation test map edit, `Encina.json` manifest edit, package manifest edit, package `PublicAPI.Unshipped.txt` edit, unit, DI, guard and integration test files, `Encina.IntegrationTests.csproj` edit | 15 | 1,300 |
| 3 | README, usage page, `docs/INVENTORY.md` line, changelog fragment, three justification files (property, load, benchmark) | 7 | 330 |

PR 2 needs PR 1; PR 3 is last; the package is useful with declarations written by #416, but compiles and tests without it (the keys are read from any `IConfiguration`). No interface changes, so no implementer is touched. A pull request that measures over 15 files or 1,500 lines when it is briefed is split again (PR 2 is the first candidate: the integration test and its csproj edit move to a PR 2b).

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 1, 3, 6 and 7 | Opt-in, fail-closed, scoped services never in singletons, 12 functions, EventId ranges | D3, D4, Phase 4 |
| ADR-021 | EventId ranges registered first, packed | `AspireServiceDefaults` 800-819 |
| ADR-054 | Observability conventions; exporters outside the library; tenant tag when multi-tenancy is on | D5, Phase 3 |
| ADR-062 | Error category contract | Phase 3 table of codes |
| SPEC-000 section 6 and DEC-009 | Aspire integration that runs locally is in 1.0 | Scope |
| Aspire service defaults template (`AddServiceDefaults`, `MapDefaultEndpoints`, `/health`, `/alive`) | The application-side convention | D7 |
| OpenTelemetry resource semantic conventions (`service.name`, `OTEL_SERVICE_NAME`) | Service name precedence | Phase 2 task 1 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AddEncinaOpenTelemetry`, `WithEncina`, `EncinaOpenTelemetryOptions` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`, `EncinaOpenTelemetryOptions.cs` | Telemetry registration |
| `AddEncinaOutbox`, `AddEncinaInbox`, `AddEncinaSaga`, `AddEncinaScheduling`, `AddEncinaHealthCheck<T>` | `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs` | Health check registration |
| `DeadLetterHealthCheck` | `src/Encina.Messaging/Health/DeadLetterHealthCheck.cs` | Dead letter check through the generic extension |
| `IOutboxStore`, `IInboxStore`, `ISagaStore`, `IScheduledMessageStore`, `IDeadLetterStore` | `src/Encina.Messaging/` | The start check resolves them |
| The environment contract | #416 | Read by the reader |
| `WithEncinaTestSupport` | `src/Encina.Aspire.Testing/DistributedApplicationTestingBuilderExtensions.cs` | Second-call convention |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Aspire.ServiceDefaults` (`AspireServiceDefaults`, new) | 800-819, ids 800-803 used | From the assigned block 800-849 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); 804-819 free; `AspireHosting = (820, 839)` is #419's; 840-849 free; registered in `EventIdRanges.cs` and the allocation test map before use |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 7 | csproj, options, configuration reader, extension, validator, log, `EventIdRanges.cs` edit |
| Public API and manifests | 5 | Shipped, Unshipped, package manifest, `Encina.json` edit, `Encina` PublicAPI edit |
| Tests and project files | 12 | Seven test files (reader unit and guard, extension unit, validator unit, DI, extension guard, integration), allocation map edit, 3 test csproj edits, `Encina.slnx` edit |
| Justification files | 3 | Property, load, benchmark |
| Documentation | 4 | README, usage page, inventory line, changelog fragment |
| **Total** | **31 distinct files; 33 file touches across 3 pull requests** | `PublicAPI.Unshipped.txt` and the package manifest are edited in pull requests 1 and 2; the sum of the split table (11 + 15 + 7) is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #417: AddEncinaDefaults for the Aspire service defaults project. Plan: docs/plans/aspire-service-defaults-implementation-plan-417.md; D1-D4, D7 and D8 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (3 pull requests, each builds alone):
1. Package, EncinaDefaultsOptions, the declaration reader (Phase 1).
2. AddEncinaDefaults, the start check, log 800-803 and the EventId range (Phase 2).
3. README, usage page, inventory, changelog fragment, justification files (Phase 6).

KEY PATTERNS:
- The declaration (Encina:Aspire:Patterns:*, written by #416) decides which health checks exist; configure may add patterns, never remove.
- Telemetry is WithEncina with options passed directly; no exporter is added (ADR-054).
- A declared pattern without a store stops the host from starting (IServiceScopeFactory, never a scoped service in a singleton); logs carry the code and the pattern name only.
- No route is mapped; the checks carry tags.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Aspire.Testing.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: registration only; nothing is cached |
| 2 | OpenTelemetry | ✅ | Phase 2: `WithEncina` with the options passed through; no exporter (D5) |
| 3 | Structured Logging | ✅ | Phase 2: `EncinaDefaultsLog`, EventIds 800-803 of `AspireServiceDefaults` |
| 4 | Health Checks | ✅ | Phase 2: the check of each declared pattern, tagged; no route mapped (D7) |
| 5 | Validation | ✅ | The declaration values are validated and the start check compares them with the registered stores |
| 6 | Resilience | ⏭️ | Deferred as in the issue: outbound HTTP resilience is #1233 (open); the template already adds the standard HTTP handler; D8 |
| 7 | Distributed Locks | ❌ | Not applicable: no shared state |
| 8 | Transactions | ❌ | Not applicable: registration only |
| 9 | Idempotency | ✅ | A second `AddEncinaDefaults` throws, so hosted services and checks are registered once |
| 10 | Multi-Tenancy | ❌ | Not applicable: no tenant data is read or emitted by this package; the tenant tag of ADR-054 D4 is emitted by `Encina.OpenTelemetry`, and the reserved `default` tenant segment does not apply |
| 11 | Module Isolation | ⏭️ | Deferred as in the issue: per-module health is #364 and the AppHost module declaration is #423 |
| 12 | Audit Trail | ❌ | Not applicable: developer infrastructure with no business operation |

---

## Prerequisites & Dependencies

- None to compile: the package depends on existing Encina packages. #416 supplies the declaration at run time; the keys are fixed in the #416 plan, so both can be built in parallel.
- #440 owns the naming answer (D2) and the parity test of the keys; #454 (probe endpoints), #607 (service discovery) and #1233 (outbound HTTP) are independent.
- The #188 plan records the service-name defect (`AddEncinaOpenTelemetry` versus `WithEncina`) that this plan avoids by passing the options.

---

## Next Steps

1. The maintainer answers D1, D2, D3, D4, D7 and D8 when work on #417 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1, then 2, then 3.
