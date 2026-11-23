# Implementation Plan: `Encina.OpenTelemetry` — Wire the Logs Signal in `WithEncina`

> **Issue**: [#1048](https://github.com/dlrivada/Encina/issues/1048)
> **Type**: Feature
> **Complexity**: Low (one method, one pull request)
> **Estimated Scope**: ~25-40 lines of production code + ~120-180 lines of tests
> **Specification**: OpenTelemetry logs signal; [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) (observability conventions); `AGENTS.md` section 1 (pay-for-what-you-use)
> **Related**: [#1043](https://github.com/dlrivada/Encina/issues/1043) (OTLP exporter; its plan wires the logs exporter on the block this plan adds), [#1049](https://github.com/dlrivada/Encina/issues/1049) (collector reachability health check), [#888](https://github.com/dlrivada/Encina/issues/888) (parent EPIC)

---

## Summary

`WithEncina(this OpenTelemetryBuilder builder, EncinaOpenTelemetryOptions? options = null)` (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`) configures the resource, tracing (`AddSource` for the 20 Encina sources) and metrics (`AddMeter("Encina")`, runtime instrumentation); it has no `WithLogging` block, so the logs signal is not wired through the Encina call site. This plan adds that block.

1. **A logging block in `WithEncina`** that registers the OpenTelemetry logger provider on the builder and sets `IncludeFormattedMessage`, `IncludeScopes` and `ParseStateValues` (Design Choices 1 and 2 decide whether it is unconditional and with which values).
2. **The right API shape.** The issue's sketch sets the three properties on the lambda argument of `WithLogging`, but that lambda receives a `LoggerProviderBuilder`; the three properties belong to `OpenTelemetryLoggerOptions`, set through the second lambda of the two-lambda overload of `WithLogging` (Design Choice 3, verified by compiling in Phase 1).
3. **One thing the issue does not say.** The OpenTelemetry logger provider receives every `ILogger` category of the host, not only the `Encina.*` ones, so wiring it exports application logs through whatever exporter the application attaches. Design Choice 1 is about exactly that.

**Standards**: OpenTelemetry logs signal (the SDK package `OpenTelemetry` 1.17.0, already referenced by the project through `Directory.Packages.props`; the issue says 1.15.3).

**Estimated scope**: 0 new production files, 1 or 2 modified production files (2 and a `PublicAPI.Unshipped.txt` line when Design Choice 1 is option B), 1 unit test file extended, the package README, a changelog fragment, the coverage manifest when the options file changes; one pull request.

**Affected packages**: `Encina.OpenTelemetry`.

**Provider category**: None (observability, provider-independent).

**What the code does today** (`origin/main` at `da9dd5e3e`):

- `WithEncina` has the blocks `ConfigureResource`, `WithTracing` and `WithMetrics`; its second parameter `EncinaOpenTelemetryOptions` is a plain instance (not `IOptions`), used for the service name and version only. `EncinaOpenTelemetryOptions` has three properties: `ServiceName`, `ServiceVersion`, `EnableMessagingEnrichers`.
- `Encina.OpenTelemetry.csproj` references `OpenTelemetry`, `OpenTelemetry.Api`, `OpenTelemetry.Exporter.Console`, `OpenTelemetry.Exporter.OpenTelemetryProtocol` (`PrivateAssets="all"`, made public by #1043) and `OpenTelemetry.Extensions.Hosting`, all 1.17.0 (`Directory.Packages.props`); no code under `src/Encina.OpenTelemetry` uses `OpenTelemetry.Logs`.
- The unit tests of `WithEncina` are in `tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs` (builder returned, options, `null` options) and the null-builder guard in `tests/Encina.GuardTests/Infrastructure/OpenTelemetry/ServiceCollectionExtensionsGuardTests.cs`.
- The coverage manifest `.github/coverage-manifest/Encina.OpenTelemetry.json` gives `ServiceCollectionExtensions.cs` the targets unit 90 and guard 35; `EncinaOpenTelemetryOptions.cs` has no explicit targets (default rule `*Options.cs`, flag unit).
- The plan of #1043 (on `main`) writes `builder.WithLogging(logging => logging.AddOtlpExporter(...))` after the block of this plan, which is the `LoggerProviderBuilder` overload and is compatible with both options of Design Choice 1.

---

## Design Choices

<details>
<summary><strong>1. Unconditional logs wiring, or an opt-in flag</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unconditional `WithLogging` block, no new option (the issue's choice)** | Symmetric with `WithTracing` and `WithMetrics`; no new public API; the acceptance criterion "no new public API on `EncinaOpenTelemetryOptions`" holds | Every `ILogger` category of the application, not only `Encina.*`, now reaches the OpenTelemetry logger provider and any exporter the application attaches, including logs that may carry personal data written by the application's own code; a host that already calls `logging.AddOpenTelemetry(...)` may end up with two providers (verified by a Phase 1 test) |
| **B) A flag `EncinaOpenTelemetryOptions.EnableLogs`, default `false`, that switches the block on** | Pay-for-what-you-use (`AGENTS.md` section 1); calling `WithEncina` for traces and metrics never starts exporting application logs; no clash with an application that wires logs itself | One new public property (the acceptance criterion about no new API is dropped); the OTLP logs branch of #1043 depends on the flag; one more switch to document |
| **C) Unconditional block restricted to the `Encina` category prefix** | Exports only Encina's own logs; no surprise for application logs | Application logs never flow through `WithEncina`, which the issue's motivation ("one-stop call") does not want; an exporter that expects all categories silently gets fewer; extra filter rules to maintain |

### Chosen Option: **B — opt-in `EnableLogs` flag** (recommended, pending the maintainer)

### Rationale

- A wins when the maintainer treats `WithEncina` as the application's whole OpenTelemetry setup and wants the three signals to behave alike; B wins in the case the health-data reference application shows: calling `WithEncina` today only exports Encina's sources and meters, and a default-on logs block would start exporting every application log (which can include personal data the application logs itself) to the collector without any new line in the application's own configuration. C wins when only Encina's own logs are wanted and the application keeps its own logging pipeline.
- ADR-054 settled point 1 says "Exporters and dashboards stay out of `WithEncina()`: the application chooses the SDK, configuration and exporters", and names #1043, #1048 and #1049 as the exporter work. Read strictly, an unconditional logs provider in `WithEncina` (A or C) is SDK configuration the application did not ask for; read loosely, the provider is not an exporter and `WithTracing` and `WithMetrics` are already unconditional. Both readings are defensible, so the ADR does not settle the choice and the maintainer picks.
- Under A the acceptance criterion "No new public API on `EncinaOpenTelemetryOptions`" holds; under B and C it does not, so the choice is the maintainer's. If B is chosen, the logs branch of #1043 (`WithLogging(... AddOtlpExporter ...)`) is nested under `EnableLogs` as well as `EnableOtlpExporter`, a one-condition edit in that plan's Phase 2.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Values of the three logger options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IncludeFormattedMessage = true`, `IncludeScopes = true`, `ParseStateValues = true` (the issue's values)** | The rendered message, the scope values and the structured state of `[LoggerMessage]` calls all reach the exporter; richest data | Scope values can carry tenant or user identifiers set by the application; the formatted message duplicates the template plus attributes (more bytes); `ParseStateValues` costs allocations per record |
| **B) `IncludeFormattedMessage = true` only; scopes and state parsing left at the SDK default (`false`)** | Smallest payload and cost; no scope values leave the process by default | `[LoggerMessage]` parameters do not arrive as attributes (the structured part the issue wants is lost); a consumer needs a second configuration to get them |
| **C) Leave the three at the SDK defaults and let the application set them** | No opinion in the library | Defeats "exact defaults" of the issue and gives the weakest out-of-the-box result |

### Chosen Option: **A — the issue's three values** (recommended, pending the maintainer)

### Rationale

- A wins when the exporter is a trusted collector inside the operator's boundary and structured attributes are the point of the feature (the issue's motivation); B wins when scopes can contain identifiers that must not leave the process, or when volume is a concern. The properties of `OpenTelemetryLoggerOptions` stay overridable by the application through a later `Configure<OpenTelemetryLoggerOptions>`, so the choice sets a default, not a ceiling.
- The values are the same under every option of Design Choice 1; they apply only where the block is active.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. API shape of the logging block</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The two-lambda `WithLogging(Action<LoggerProviderBuilder>, Action<OpenTelemetryLoggerOptions>)`: the first lambda stays empty (exporters are added by the application or by #1043), the second sets the three properties** | Uses the types the properties belong to; compiles against `OpenTelemetry` 1.17.0 | Needs a compile-time check of the overload (Phase 1) |
| **B) The issue's sketch (properties set on the first lambda's argument)** | Matches the issue text | Does not compile: `LoggerProviderBuilder` has no `IncludeFormattedMessage`, `IncludeScopes` or `ParseStateValues` |
| **C) `services.AddLogging(b => b.AddOpenTelemetry(o => ...))` on `builder.Services`** | The classic registration | Bypasses the `OpenTelemetryBuilder` the method already receives, so other `WithLogging` extensions (such as the OTLP one of #1043) would not share the provider |

### Chosen Option: **A — two-lambda overload**

### Rationale

- B is a defect of the sketch, not an option that can be implemented; C splits the logs pipeline from the builder. If Phase 1 finds that the overload does not exist in 1.17.0, the implementer stops and reports instead of choosing C.

**Class:** Settled
**Settled by:** the public API of `OpenTelemetry` 1.17.0 (`Directory.Packages.props`): `OpenTelemetryLoggerOptions` owns `IncludeFormattedMessage`, `IncludeScopes` and `ParseStateValues`; the first lambda of `WithLogging` receives a `LoggerProviderBuilder`

</details>

<details>
<summary><strong>4. Register nothing else: no source list, no new service</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only the logging block (and the flag when Design Choice 1 is B); no `AddEncinaOpenTelemetry` change** | Smallest change; the logger provider has no source list to maintain | None |
| **B) Also register a hosted service or decorator for log enrichment (tenant, module)** | Tenant and module on every record | Out of scope; log scopes already carry them where the application sets them; a new service needs registration tests and EventIds |

### Chosen Option: **A — the block only**

### Rationale

- The logs provider needs no `AddSource` equivalent, so there is no list to keep in step with the 20 tracing sources (#1787 owns the subscription audit).

**Class:** Trivial
**Settled by:** `AGENTS.md` section 1 (every line serves a current purpose); the existing `WithEncina` shape (one block per signal)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Settled by |
|----|--------|-------|------------|
| C1 | No new `EncinaError` code and no `EncinaError` is created: the change only registers a provider, so there is no ADR-062 `ErrorCategory` to assign | Settled | ADR-062 (a category is chosen where an error is created) |
| C2 | No new `[LoggerMessage]` and no EventId: nothing is logged by the wiring; the registry block 1000-1049 assigned to this family is not used by this plan | Settled | `AGENTS.md` section 7 (a range is registered only with a `[LoggerMessage]`) |
| C3 | No tenant tag is added: the block adds no metric tag; scope values reach the exporter only as the application wrote them (ADR-054 D4 concerns metric and span tags) | Settled | ADR-054 D4 |
| C4 | The block does not log or serialize any exception or `EncinaError.Message` | Settled | `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs) |

---

## Implementation Phases

One pull request (about 5 to 7 files, about 220 lines): `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`, the extended unit test file `tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs`, `src/Encina.OpenTelemetry/README.md`, the changelog fragment `changelog.d/1048-otel-logging-wiring.added.md`; and, only when Design Choice 1 is B, `src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs`, `src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.OpenTelemetry.json` (the options file gets an explicit unit target).

### Phase 1: Logging Block

> **Goal**: `WithEncina` registers the OpenTelemetry logger provider with the three options.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`** (modify) — add `using OpenTelemetry.Logs;`; after the `WithMetrics(...)` block add the two-lambda `builder.WithLogging(logging => { }, options => { options.IncludeFormattedMessage = true; options.IncludeScopes = true; options.ParseStateValues = true; })` (under Design Choice 1 option B inside `if (options.EnableLogs)`); update the XML remarks of `WithEncina` to name the third signal
2. **`src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs`** (modify, only for Design Choice 1 option B) — `bool EnableLogs { get; set; } = false` with XML docs naming the application-wide scope of the logs provider; the PublicAPI line (`mechanical-fixer`)
3. **Compile check first**: if the two-lambda overload or the three properties are missing in 1.17.0, stop and report (Design Choice 3 note)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1048: the logs block of Encina.OpenTelemetry.WithEncina.

CONTEXT:
- WithEncina (src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs) has ConfigureResource, WithTracing and WithMetrics.
- OpenTelemetry 1.17.0: WithLogging takes Action<LoggerProviderBuilder> and Action<OpenTelemetryLoggerOptions>; the three
  properties IncludeFormattedMessage, IncludeScopes, ParseStateValues belong to OpenTelemetryLoggerOptions.
- The maintainer's answer to Design Choice 1 decides whether the block is unconditional or behind EncinaOpenTelemetryOptions.EnableLogs.

TASK:
Add the block and, when the answer is the flag, the option with its XML docs. Build first against the overload to prove it exists.

KEY RULES:
- Pre-1.0: no compatibility layers, no [Obsolete]; XML docs on every public member; PublicAPI lines through mechanical-fixer.
- Zero warnings; do not add or change any other signal.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs
- docs/plans/otlp-exporter-implementation-plan-1043.md
```

</details>

### Phase 2: Cross-Cutting Integration

> **Goal**: The matrix functions marked included are in place.

<details>
<summary><strong>Tasks</strong></summary>

1. **OpenTelemetry (2)** and **Structured Logging (3)** — Phase 1 is the integration
2. **Registration completeness** — the block registers no Encina service; a DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves `IOptions<OpenTelemetryLoggerOptions>` and the `ILoggerFactory` after `AddOpenTelemetry().WithEncina(...)`
3. **#1043 alignment** — no code; the plan of #1043 keeps its logs branch on the same builder (its Phase 2 adds the flag condition when Design Choice 1 is B)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1048: cross-cutting verification.

CONTEXT:
- The change adds no Encina service. The proof is a service provider built with ValidateOnBuild and ValidateScopes.

TASK:
Add the DI test in tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs.

KEY RULES:
- Resolve the real providers; no mocks of the OpenTelemetry SDK.

REFERENCE FILES:
- tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Observability

> **Goal**: State what is emitted.

<details>
<summary><strong>Tasks</strong></summary>

1. No `ActivitySource`, `Meter` or `[LoggerMessage]` is added: the feature is the wiring of the logs signal itself
2. No EventId is allocated; the block 1000-1049 assigned to this family is untouched by this plan

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are checking Phase 3 of issue #1048: observability of the change.

CONTEXT:
- The change wires the logs signal; it does not log, trace or measure anything itself.

TASK:
Confirm by search that the diff adds no ActivitySource, Meter or LoggerMessage and no EventId.

KEY RULES:
- Do not register an EventId range without a [LoggerMessage] (AGENTS.md section 7).

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 4: Testing

> **Goal**: Unit and guard flags at or above their targets.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs`, extended): after `services.AddOpenTelemetry().WithEncina(options)`, the built provider resolves an `OpenTelemetryLoggerOptions` with the three properties `true` (with the flag on, under option B); with the flag off (option B) the logger provider is not registered; a log record written through an `ILogger<T>` reaches a test exporter once (a small `BaseExporter<LogRecord>` collecting records, attached with a `SimpleLogRecordExportProcessor`; no new package); the record is exported once when the application also calls `logging.AddOpenTelemetry()`; the tracing and metrics registrations still pass their existing tests
2. **Guard** — the null-builder check of `WithEncina` already exists in `ServiceCollectionExtensionsGuardTests`; no new public method or constructor, no new guard test
3. **Contract, property, integration** — not applicable: no new public interface, no invariant, no infrastructure (the exporter is a test double); the existing `BasicInstrumentationTests` integration file is untouched
4. **Targets** (measured by the PR against `.github/coverage-manifest/Encina.OpenTelemetry.json`): `ServiceCollectionExtensions.cs` unit 90 (unchanged; the new block is a straight line covered by the unit tests above) and guard 35 (unchanged; no new parameter); `EncinaOpenTelemetryOptions.cs` (option B only) gets the explicit target unit 100, because the POCO has two default-value tests; no target is lowered

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1048: tests of the logs block.

CONTEXT:
- Tests execute the real OpenTelemetry SDK registration; assertions use Shouldly through Encina.Testing.Shouldly.
- Coverage is per flag against .github/coverage-manifest/Encina.OpenTelemetry.json; a target never goes down.

TASK:
Write the tests of the Phase 4 tasks, measure the unit and guard coverage of ServiceCollectionExtensions.cs (and of
EncinaOpenTelemetryOptions.cs under option B) with
`dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests` and report the table.

KEY RULES:
- No Thread.Sleep; no new package for the test exporter; a test never swallows an exception of the code under test.

REFERENCE FILES:
- tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs
- tests/Encina.GuardTests/Infrastructure/OpenTelemetry/ServiceCollectionExtensionsGuardTests.cs
- .github/coverage-manifest/Encina.OpenTelemetry.json
```

</details>

### Phase 5: Documentation and Finalization

> **Goal**: Consumers know the logs signal is wired and what it exports.

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on `WithEncina` (third signal) and on `EnableLogs` when it exists
2. `src/Encina.OpenTelemetry/README.md` through `docs-writer`: a "Logs" subsection next to "Tracing" and "Metrics" in "What Gets Instrumented?", stating that the provider receives every `ILogger` category of the host and how to scope it
3. `changelog.d/1048-otel-logging-wiring.added.md` (written by `mechanical-fixer`)
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test`; every flag at its target; CRAP <= 10 on changed methods (`WithEncina` gains one block and at most one branch)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are finishing issue #1048: documentation and verification.

CONTEXT:
- Documentation goes through docs-writer and the encina-docs skill; the changelog fragment through mechanical-fixer.

TASK:
Complete the Phase 5 tasks and report the per-flag figures of the touched files against their targets.

KEY RULES:
- English only; no private client application named; say plainly that the logger provider receives every category.
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
| OpenTelemetry logs signal | `LoggerProvider` and `OpenTelemetryLoggerOptions` | The three options of Design Choice 2 |
| ADR-054 | D3 names, D4 tenant tag, settled point 2 (error code, never the message) | Why the block adds no tag and no message |
| `AGENTS.md` section 1 | Pay-for-what-you-use | The ground of the recommendation in Design Choice 1 |
| ADR-062 | `ErrorCategory` of a created error | None created (C1) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `WithEncina` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | Gains the logging block |
| `EncinaOpenTelemetryOptions` | `src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs` | Gains `EnableLogs` under Design Choice 1 option B |
| OTLP exporter plan | `docs/plans/otlp-exporter-implementation-plan-1043.md` (#1043) | Adds the logs exporter on this block |
| `ServiceCollectionExtensionsTests`, `ServiceCollectionExtensionsGuardTests` | `tests/Encina.UnitTests/OpenTelemetry/`, `tests/Encina.GuardTests/Infrastructure/OpenTelemetry/` | Extended and unchanged |
| Coverage manifest | `.github/coverage-manifest/Encina.OpenTelemetry.json` | Targets of the two files |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.OpenTelemetry` | none | No `[LoggerMessage]` is added; the family block 1000-1049 stays unused by this plan |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (modified) | 1 or 2 | `ServiceCollectionExtensions.cs`; plus `EncinaOpenTelemetryOptions.cs` under option B |
| PublicAPI and manifest | 0 or 2 | `PublicAPI.Unshipped.txt` and `Encina.OpenTelemetry.json` under option B |
| Tests | 1 | `ServiceCollectionExtensionsTests.cs` extended |
| Documentation and changelog | 2 | package README, fragment |
| **Total** | **4 to 7** | One pull request, about 220 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1048: the logs signal in Encina.OpenTelemetry.WithEncina.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- OpenTelemetry 1.17.0; WithEncina has resource, tracing and metrics blocks; the logger provider receives every ILogger category.

IMPLEMENTATION OVERVIEW:
1. Add the two-lambda WithLogging block with IncludeFormattedMessage, IncludeScopes and ParseStateValues set per the maintainer's
   answers to Design Choices 1 and 2 (flag EnableLogs when the answer is the opt-in).
2. Prove it with unit tests: options values, provider registered or absent per the flag, one exported record, no duplicate
   when the application also calls logging.AddOpenTelemetry(), DI build with ValidateOnBuild and ValidateScopes.
3. README Logs subsection through docs-writer; changelog fragment, PublicAPI and manifest through mechanical-fixer.

KEY PATTERNS:
- No EventId, no tag, no EncinaError.Message anywhere in the change.
- Targets never go down: ServiceCollectionExtensions.cs unit 90 and guard 35 stay.

REFERENCE FILES:
- docs/plans/otel-logging-wiring-implementation-plan-1048.md
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- .github/coverage-manifest/Encina.OpenTelemetry.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Logger pipeline; no domain data |
| 2 | OpenTelemetry | ✅ | This plan is the logs signal wiring (Phase 1) |
| 3 | Structured Logging | ✅ | Existing `[LoggerMessage]` events become visible to the OpenTelemetry pipeline; no new message |
| 4 | Health Checks | ❌ | The collector probe is #1049's |
| 5 | Validation | ❌ | Configuration only; the three values are booleans |
| 6 | Resilience | ❌ | The logger provider batches and drops on its own; no Encina call is made |
| 7 | Distributed Locks | ❌ | Stateless |
| 8 | Transactions | ❌ | Stateless |
| 9 | Idempotency | ❌ | Telemetry, not commands |
| 10 | Multi-Tenancy | ❌ | Tenant values reach the exporter only through scopes the application sets (ADR-054 D4 concerns metric and span tags) |
| 11 | Module Isolation | ❌ | Cross-cutting telemetry |
| 12 | Audit Trail | ❌ | Logs are operational; audit events go through the audit stores |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#1043](https://github.com/dlrivada/Encina/issues/1043) — OTLP exporter as opt-in | Open; plan on `main` | Its logs branch needs this block; this plan lands first |
| [#1049](https://github.com/dlrivada/Encina/issues/1049) — collector reachability health check | Open | Independent of this plan |
| [#1787](https://github.com/dlrivada/Encina/issues/1787) — every source and meter subscribed | Open | Owns the audit of subscribed sources; the logs provider has no source list |

---

## Next Steps

1. Link this plan from issue #1048; the worker brief can follow once the Dilemmas are answered.
2. When work on the issue starts, ask the maintainer to answer Dilemmas 1 and 2 (unconditional or opt-in logs; values of the three options), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices.
3. One commit for the change; it references `Fixes #1048`.
