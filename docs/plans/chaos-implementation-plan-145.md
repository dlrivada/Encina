# Implementation Plan: Chaos engineering (Polly chaos strategies) in `Encina.Resilience`

> **Issue**: [#145](https://github.com/dlrivada/Encina/issues/145)
> **Type**: Feature
> **Complexity**: Medium (7 phases, no database providers, 4 pull requests)
> **Estimated Scope**: ~450 lines of production code + ~900 lines of tests (about 1,600 changed lines in total, four pull requests)

---

## Summary

Let a team inject faults and latency into chosen request types to prove that its retry, breaker and timeout settings work, using Polly v8's chaos strategies as the last strategy of the per-type pipeline ([ADR-055](../architecture/adr/055-resilience-family.md) R2: "rate limiter, total timeout, retry (or hedging instead), circuit breaker, per-attempt timeout, chaos last"). The table of ADR-055 puts #145 in `Encina.Resilience`. Chaos is the one resilience capability that does harm by design, so this plan makes it **off by default and impossible to enable by accident**: nothing is injected unless the application asks for it, the host environment must allow it, the request that is on is logged and visible in the health report, and a host without an environment is denied.

What exists today (read in `origin/main` at `098caa920`):

- No chaos code: `git grep -i "chaos"` over `src` returns no match. `Encina.Polly` and `Encina.Extensions.Resilience` (both replaced by ADR-055 R1) reference `Polly` and `Polly.Extensions` (version 8.6.6, `Directory.Packages.props:72`). The assembly `Polly.Core` 8.6.6 ships the chaos types under `Polly.Simmy` (`ChaosFaultStrategyOptions`, `ChaosLatencyStrategyOptions`, `ChaosOutcomeStrategyOptions<T>`, `ChaosBehaviorStrategyOptions`, base `ChaosStrategyOptions`), read in its XML documentation in the local NuGet cache.
- The package that ADR-055 R1 names, `Encina.Resilience`, **does not exist in `src/`**, and no open issue creates it. The continuation `RequestHandlerCallback<TResponse>()` has no token (R6, planned).
- Precedent for environment gating: `EncinaAspNetCoreOptions` documents an option "honoured only when `IHostEnvironment.IsDevelopment()` is true" (`src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs:64`), and `BreakTheGlassStartupValidator` reads `IHostEnvironment` through `GetService` (`src/Encina.Security.BreakTheGlass/Services/BreakTheGlassStartupValidator.cs:186`). The plan for #2382 (startup requirements validator, on main) plans to reject test-only implementations outside Development unless an explicit, logged opt-out is set.
- Health checks follow `DefaultName` plus a static tag array (`src/Encina.Kafka/Health/KafkaHealthCheck.cs:15`).

What this plan delivers, all in `Encina.Resilience`: `ChaosOptions` (per request type and default, off by default), `IChaosController` with its singleton implementation (runtime switch, read by Polly's generators), `ChaosStrategyFactory` (adds latency and outcome strategies in the last slot), the environment gate, the injected-error code `encina.chaos.injected`, a health check that reports `Degraded` while chaos is on, `AddEncinaChaos`, five `[LoggerMessage]` definitions and the tests. The issue's `ChaosPipelineBehavior<,>` and `IChaosStrategyProvider.GetPipeline<TRequest>()` are replaced where they contradict ADR-055 (choice 2).

- **Affected packages**: `Encina.Resilience` (planned), `Encina` core (`EventIdRanges.cs` only). The issue also ticks `Encina.AspNetCore`: it needs no code, because the gate reads `IHostEnvironment`, which every host provides.
- **Provider category**: Resilience (AGENTS.md section 5, "Other categories"). No database, caching, transport, lock or validation matrix applies: chaos wraps the call of a request type, whatever it calls.
- **Estimated size**: 4 pull requests of at most 15 files and 1,500 lines, split in "Implementation Phases".
- **Prerequisites**: the foundation (package, compiler, options front-ends), described in "Prerequisites & Dependencies".

### Shared Pieces and Ownership (family resilience-patterns-1 and resilience-patterns-2)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project, per-type compiler (R2), options front-ends (R3), strategy-factory slots | The foundation issue (issue file `resilience-foundation-encina-resilience.md` of the family resilience-patterns-1) | every plan of both families |
| `ChaosOptions`, `IChaosController`, the environment gate, `ChaosStrategyFactory`, `ChaosErrorCodes` | **This plan (#145)** | #136 hedging tests may inject chaos into an attempt (sibling plan, by number) |
| The fixed strategy order (chaos last) | The foundation compiler | this plan fills the last slot |
| The `Meter("Encina.Resilience")` holder and instrument names | #728 (plan on main; choices 1 and 8 pending) | this plan creates no instrument (Phase 5) |
| EventIds 960-969 (`ResilienceChaos`), 960-964 used | This plan | none |

---

## Design Choices

<details>
<summary><strong>1. Where the code lives while `Encina.Resilience` does not exist (shared with #136, #137, #138, #139, #141 and #335)</strong></summary>

ADR-055 R1 puts chaos in `Encina.Resilience`, whose compiler and options model do not exist yet.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wait for the foundation; this plan is written against the R2 compiler and the R3 options model** | One implementation in its final home; nothing that R1 deletes | Blocked on the foundation |
| **B) Build `ChaosPipelineBehavior<,>` in `Encina.Polly` now, as the issue sketches** | Ships without the foundation | R2 option 2b (a behavior per strategy) was rejected; the code is deleted by R1 |
| **C) The first pull request creates a minimal `Encina.Resilience` project and compiler stub** | Unblocks the plan | The first plan to land defines the foundation's shape for every capability |

### Chosen Option: **A — Wait for the foundation** (recommended, pending the maintainer)

### Rationale

- A wins when the 1.0 order of the spike report is kept; B wins if chaos must ship before the foundation (it is `priority-low` in the issue, which favours waiting); C wins if the foundation should grow out of a first capability. The question is asked once for the resilience family (the shared Dilemma 1 of #136).
- Choices 2 to 7 are written for A. Under B or C the package changes and choice 2 is re-planned.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Strategy form: strategies of the per-type pipeline, or the `ChaosPipelineBehavior<,>` and `GetPipeline<TRequest>()` of the issue</strong></summary>

The issue builds a `ResiliencePipeline` inside a provider and a behavior executes it around `next`. ADR-055 R2 compiles one pipeline per request type with chaos in the last slot.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The foundation's compiler adds Polly's latency and outcome chaos strategies in the last slot; the controller feeds their `EnabledGenerator` and `InjectionRateGenerator` delegates** | Chaos sits inside retry, breaker and timeout, which is the only place where those are exercised; one pipeline per call; nothing is rebuilt to flip a switch | The class named in the issue is not created |
| **B) A `ChaosPipelineBehavior<,>` with its own pipeline** | Matches the issue text | R2 option 2b was rejected; a second pipeline per call; the behavior would sit outside retry and breaker, testing nothing |

### Chosen Option: **A — Strategies of the per-type pipeline** (recommended, pending the maintainer)

### Rationale

- A is what ADR-055 R2 decided (2a chosen, 2b rejected); the plan keeps it a Dilemma only because it does not hold under choice 1 option B. The acceptance criterion "Create `ChaosPipelineBehavior<,>`" is met by the Resilience-stage behavior that runs the compiled pipeline; no class of that name exists.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. How chaos is kept out of production</strong></summary>

The issue's sample sets `options.Enabled = builder.Environment.IsDevelopment()`, which leaves the decision to every application. AGENTS.md section 3 says a gate with no context denies and the only opt-out is explicit and logged; AGENTS.md section 1 says every capability is opt-in. Chaos is off unless `AddEncinaChaos` is called and a rate above zero is configured (this part holds under every option).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An environment gate in `Encina.Resilience`: chaos is honoured only when `IHostEnvironment.EnvironmentName` is in `AllowedEnvironments` (default: `Development`); any other environment with chaos enabled fails startup (`ValidateOnStart`) naming the environment; an application that really wants it elsewhere lists that environment explicitly, which is logged at warning on every start and reported `Degraded` by the health check; a container with no `IHostEnvironment` denies** | Fail closed by default; staging and game-day use remain possible; matches the precedent of `EncinaAspNetCoreOptions` | A deployment that names `Production` in configuration can still inject faults (explicit and logged, but possible) |
| **B) Chaos code only in a testing package (`Encina.Testing` family), never in the runtime package** | Structurally absent from a production deployment | The runtime pipeline would need an extension point only for this package; staging and game-day injection become impossible; the issue puts it in `Encina.Resilience` |
| **C) No gate: the application decides `Enabled` (the issue's sample)** | Smallest | One misconfiguration injects faults into production; nothing fails closed |

### Chosen Option: **A — Environment allow-list, fail closed, explicit and logged opt-in** (recommended, pending the maintainer)

### Rationale

- A wins when chaos must also run in a deployed non-production environment (staging, a game day); B wins when chaos is only a test tool and a production deployment must be unable to contain it; C does not satisfy AGENTS.md section 3 and is listed to show what A prevents.
- Test hosts built with the generic host default to the environment `Production`, so a test enables chaos by `UseEnvironment("Development")` or by listing its environment; the documentation says so.

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Configuration front-end: options only, or also a `[Chaos]` attribute</strong></summary>

ADR-055 R3 lets attributes and options feed one model. A request attribute is compile-time and ships in every deployment.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Options only (`AddEncinaChaos(o => o.For<T>(...))` and `IConfiguration` binding); no attribute** | Chaos is an environment concern, so it lives in environment configuration; a request type never carries injection settings into production; reloadable | A request type's chaos is not visible next to it |
| **B) A `[Chaos]` attribute as well** | Same shape as `[Retry]` and `[Timeout]` | Injection settings compiled into the shipped assembly; a request carries a fault policy everywhere it runs |

### Chosen Option: **A — Options only** (recommended, pending the maintainer)

### Rationale

- A wins on keeping fault injection out of production artifacts; B wins if teams want chaos documented on the request. The issue's `TargetHandlers` list becomes `For<TRequest>()` entries (choice 5).

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. What is injected: coded outcomes and latency, or also exceptions</strong></summary>

The issue shows `AddChaosFault` (an exception), `AddChaosLatency` and `AddChaosOutcome<HttpResponseMessage>`. In Encina a failing dependency normally becomes a `Left` (ROP), and the retry layer decides by classification (ADR-029).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Latency and an injected `Left`: code `encina.chaos.injected` (ADR-062 category `Unavailable` by default, settable to `Timeout`, `UpstreamFailure` or `Internal`), constant message, a detail naming the kind** | Everything stays ROP; the retry and breaker react through the category, exactly as for a real failure; no exception to catch | Does not exercise a handler that throws |
| **B) Also an injected exception (`AddChaosFault`)** | Exercises the exception path of the dispatcher | Depends on how the compiler's `ShouldHandle` treats exceptions; a second failure shape to document |

### Chosen Option: **A — Latency and a coded `Left`** (recommended, pending the maintainer)

### Rationale

- A wins for testing the strategies of the pipeline; B wins when the goal is to test handler exception safety. The injected `Left` is a normal `Left`: ADR-029 classification decides whether a retry happens (category `Unavailable` is transient), and a command is retried only when idempotent.

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. The runtime switch of the issue: `EnableChaos`, `SetFaultInjectionRate`, `SetLatencyInjection`</strong></summary>

The usage example flips chaos on in a test after the container is built. A mutable global switch on a singleton is also an attack surface in a deployed host.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IChaosController`, a singleton read through Polly's generators with `Volatile` fields (thread-safe, no lock); its setters validate (rate between 0 and 1 inclusive) and return `Either<EncinaError, Unit>`; every change is logged at warning; it cannot turn chaos on in an environment the gate denied** | Meets the example; no pipeline rebuild; the gate still wins | A mutable switch exists in the container |
| **B) No runtime switch: settings come from options and reload through the registry (R3)** | No mutable state; the gate is the only control | A test needs a host per setting; reload semantics (a failed rebuild stops later reloads) make a flip unreliable |

### Chosen Option: **A — A controller behind the gate** (recommended, pending the maintainer)

### Rationale

- A wins for tests and game days that flip settings quickly; B wins if the maintainer does not want any runtime mutation. Concurrency: the controller is a singleton and holds no scoped service; a flip applies to calls that start after it, calls in flight finish with the value they read.

- **Class:** Dilemma

</details>

<details>
<summary><strong>7. Selecting which requests are targeted</strong></summary>

The issue lists handler types (`TargetHandlers`). ADR-055 R2 compiles one pipeline per request type.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) By request type: `For<TRequest>(o => o.Chaos...)`; the default entry injects nothing (rate 0)** | The only key the compiler has; nothing is targeted by accident | The issue's list of handler types is translated to request types |
| **B) By handler type** | Matches the issue | The compiler has no handler key; one handler may serve several request types |

### Chosen Option: **A — By request type**

### Rationale

- B does not exist in the R2 model, so A is the only option that fits; it holds under every option of choices 1 to 6 because it concerns the key of the configuration, not where the code lives or how it is switched.
- **Class:** Settled
- **Settled by:** ADR-055 R2 ("one `ResiliencePipeline<Either<EncinaError, T>>` per request type") and R3 (`For<T>(...)`); AGENTS.md section 1 (opt-in: a default of rate 0 injects nothing)

</details>

---

## Implementation Phases

Four pull requests, in order: **PR-1** Phase 1, the strategy factory of Phase 2 (settings come from the options entry only), the gate of Phase 4 and the registration of Phase 3, with their tests (the gate lands with the first way to inject, so no pull request merges an ungated one); **PR-2** the controller of Phase 2 and the health check of Phase 4 (the factory and the registration are modified to read the controller); **PR-3** Phase 5 (EventIds and logs; it modifies the gate, the controller and the factory to emit them); **PR-4** the contract and property tests and Phase 7. Every PR needs the foundation merged (choice 1 option A); under B or C the split is recounted.

### Phase 1: Core Models

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Chaos/ChaosOptions.cs`: `double FaultRate` (default 0), `ErrorCategory FaultCategory` (default `Unavailable`), `TimeSpan Latency`, `double LatencyRate` (default 0), `IReadOnlyCollection<string> AllowedEnvironments` (default `["Development"]`, used from PR-2); per-type entries through the foundation's `For<T>` section.
2. `ChaosOptionsValidator.cs` (internal): rates in [0, 1], latency not negative, `FaultCategory` among `Unavailable`, `Timeout`, `UpstreamFailure`, `Internal`; an invalid value fails at startup.
3. `ChaosErrorCodes.cs`: `public const string Injected = "encina.chaos.injected"` (ADR-062 category `Unavailable` by default; settable per choice 5) and the detail key `encina.chaos.kind`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #145, plan docs/plans/chaos-implementation-plan-145.md. Encina.Resilience exists with the per-type compiler and options model.
TASK: Create ChaosOptions, its validator and ChaosErrorCodes.
KEY RULES: every default injects nothing (rate 0); invalid options fail at startup; no exception for business flow; XML docs with examples on every public member.
REFERENCE FILES: src/Encina/Errors/EncinaErrorCodes.cs, src/Encina/Errors/ErrorCategory.cs, docs/architecture/adr/055-resilience-family.md
```

</details>

### Phase 2: Default Implementation — Controller and Strategies

<details>
<summary>Tasks</summary>

1. `Chaos/IChaosController.cs` (public): `bool IsEnabled { get; }`, `Either<EncinaError, Unit> Enable(bool enabled)`, `Either<EncinaError, Unit> SetFaultRate(double rate)`, `Either<EncinaError, Unit> SetLatency(TimeSpan latency, double rate)`; `ChaosController.cs` (internal sealed, singleton): `Volatile` fields; the setters return a `Left` for a rate outside [0, 1] (validation returns `Either`, AGENTS.md section 3).
2. `ChaosStrategyFactory.cs` (internal): implements the foundation's strategy-factory contract; for a request type whose entry has a rate above zero it adds `AddChaosLatency` and `AddChaosOutcome<Either<EncinaError, T>>` in the last slot, with `EnabledGenerator` and `InjectionRateGenerator` reading the controller and the entry; the outcome generator returns `Left` built with `EncinaErrors.Create(ChaosErrorCodes.Injected, category: options.FaultCategory, "A fault was injected by chaos.")` (constant message); the random source and the latency use `TimeProvider` and a `Func<double>` injected for tests.
3. The factory reads the controller once per call, never a scoped service.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 done.
TASK: Implement IChaosController, ChaosController and ChaosStrategyFactory.
KEY RULES: thread-safe without locks (Volatile); time through TimeProvider; the injected error uses EncinaErrors.Create with a named category: argument and a constant message; no singleton takes a scoped dependency.
REFERENCE FILES: src/Encina/Results/EncinaErrors.cs, src/Encina.Polly/Behaviors/RetryPipelineBehavior.cs
```

</details>

### Phase 3: Configuration and DI

<details>
<summary>Tasks</summary>

1. `Chaos/ChaosServiceCollectionExtensions.cs` (public): `AddEncinaChaos(this IServiceCollection services, Action<ChaosOptions>? configure = null)`: `ArgumentNullException.ThrowIfNull(services)`; registers the options (validated on start), the controller and the factory with `TryAdd*`; returns `services`.
2. The DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves the controller and the factory (AGENTS.md section 3, registration completeness).
3. The registration applies the gate from PR-1 (Phase 4), so a configured rate above zero in a denied environment fails startup; PR-2 adds the controller and the health check registrations.
4. `PublicAPI.Unshipped.txt`: the interface, the options, the codes and the extension (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 done.
TASK: Add AddEncinaChaos and its DI test.
KEY RULES: TryAdd* registrations; ValidateOnBuild and ValidateScopes in the test; the registration never enables chaos by itself.
REFERENCE FILES: src/Encina.Kafka/ServiceCollectionExtensions.cs, tests/Encina.UnitTests (existing DI tests)
```

</details>

### Phase 4: Cross-Cutting Integration — the Gate and Visibility

<details>
<summary>Tasks</summary>

1. `Chaos/ChaosEnvironmentGate.cs` (internal, PR-1): `bool IsAllowed(IHostEnvironment? environment)`; false when the environment is null or its name is not in `AllowedEnvironments` (case-insensitive); a configured rate above zero in a denied environment fails `ValidateOnStart` with a message naming the environment (the options message, not an `EncinaError`), and the controller (PR-2) refuses `Enable(true)` when the gate denies.
2. `Chaos/ChaosHealthCheck.cs` (PR-2) (`IEncinaHealthCheck`, `DefaultName = "encina-chaos"`, static tags `["encina", "resilience", "chaos"]`): `Degraded` while the controller is enabled or any entry has a rate above zero; `Healthy` otherwise.
3. Tenant tag on telemetry only when multi-tenancy is on (ADR-054 D4); no tenant-qualified key exists.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1 to 3 merged.
TASK: Add the environment gate and the health check; make the validator and the controller depend on the gate.
KEY RULES: a missing IHostEnvironment denies; the only opt-out is listing the environment explicitly; the health check follows DefaultName and static Tags conventions and resolves services through a scope.
REFERENCE FILES: src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs, src/Encina.Security.BreakTheGlass/Services/BreakTheGlassStartupValidator.cs, src/Encina.Kafka/Health/KafkaHealthCheck.cs
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `ResilienceChaos = (960, 969)`; core `PublicAPI.Unshipped.txt`; the `AssemblyRanges` entry of `Encina.Resilience` in `EncinaEventIdAllocationTests.cs` gains the range.
2. `Chaos/ChaosLogMessages.cs`: 960 `ChaosActive` (Warning at start: environment, rates), 961 `ChaosDeniedInEnvironment` (Error), 962 `ChaosSettingChanged` (Warning), 963 `ChaosInjected` (Debug: kind), 964 `ChaosEnvironmentUnknown` (Warning: no `IHostEnvironment`, denied); 965 to 969 spare. Never log `EncinaError.Message` or request data.
3. Telemetry: Polly's chaos strategies report their own events through the pipeline telemetry that R5 enriches, so this plan creates no instrument (Polly measures it); the PR verifies with a listener test that an injected fault produces a Polly strategy event, and, if Polly 8.6.6 does not, adds one counter named by the #728 convention.
4. `ChaosStrategyFactory.cs` and `ChaosController.cs` (modified): emit the logs.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Phases 1 to 4 merged.
TASK: Register the EventId range, add the log messages and emit them.
KEY RULES: register the range in EventIdRanges.cs first (ADR-021), pack ids 960-964 without gaps; [LoggerMessage] source generator; no EncinaError.Message or request data in logs.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Resilience/Chaos/`): rate 1 injects the coded `Left`, rate 0 injects nothing; latency uses a fake clock; with `[Retry]` semantics a retry layer sees the injected category; gate denies an unlisted environment, a null environment and an invalid option; allows `Development`; the controller rejects a rate outside [0, 1] with a `Left` and cannot enable chaos in a denied environment; a concurrent flip test (`Parallel.For`) reads only whole values.
2. Guard (`tests/Encina.GuardTests/Resilience/Chaos/`): `AddEncinaChaos` with a null `services`; a null `configure` is accepted (valid result); the controller's setters with out-of-range values return a `Left` (a validation result, asserted in the unit tests, not an exception).
3. Contract (`tests/Encina.ContractTests/Resilience/`, PR-4): any `IChaosController` honours the same setter contract; runs the real controller.
4. Property (`tests/Encina.PropertyTests/Resilience/Chaos/`): FsCheck, for any rate in [0, 1] and a seeded random source the injected count matches the generator's decisions.
5. Integration, load, benchmark: not applicable (no infrastructure; arithmetic plus a Polly strategy); justification `.md` files for load and benchmark.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests.
KEY RULES: tests execute real package code; FakeTimeProvider and a seeded random source, never Thread.Sleep; Shouldly through Encina.Testing.Shouldly; guard tests only for public members with validated parameters.
REFERENCE FILES: .github/coverage-manifest/Encina.Polly.json, docs/en/guides/TESTING.md
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. `docs/features/resilience-chaos.md` and the `Encina.Resilience` README section (`docs-writer`): the gate, how a test enables chaos, the injected code, the health report; a warning that listing `Production` is explicit and logged.
2. `changelog.d/145-chaos-engineering.added.md`, `PublicAPI.Unshipped.txt`, the `Encina.Resilience.json` manifest rows (`mechanical-fixer`).
3. Verification: `dotnet build` 0 warnings; the tests filtered to `Chaos`; EventId architecture tests; `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Phases 1 to 6 green.
TASK: Documentation and verification.
KEY RULES: never edit CHANGELOG.md; documentation by docs-writer, metadata by mechanical-fixer; no hand-typed coverage figures.
REFERENCE FILES: changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| ADR-055 R2, R3, R5 | Chaos last in the fixed order; one options model; telemetry |
| ADR-062 | Category of `encina.chaos.injected` (`Unavailable` by default) |
| ADR-029 | Classification decides whether an injected failure is retried |
| ADR-054 D4 | Tenant tag only with multi-tenancy |
| AGENTS.md sections 1 and 3 | Opt-in; gates fail closed; explicit and logged opt-out |
| Polly v8 chaos strategies (`Polly.Simmy`) | Latency, outcome, `EnabledGenerator`, `InjectionRateGenerator` |
| Principles of Chaos Engineering | Hypothesis, small blast radius, ability to stop |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IHostEnvironment` precedent | `src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs:64` | The gate |
| `IEncinaHealthCheck` convention | `src/Encina.Kafka/Health/KafkaHealthCheck.cs` | `ChaosHealthCheck` |
| `EncinaErrors.Create` with category | `src/Encina/Results/EncinaErrors.cs` | The injected `Left` |
| `IErrorClassifier` | `src/Encina/Recoverability/IErrorClassifier.cs` | Category decides retry |
| `Polly` 8.6.6 | `Directory.Packages.props:72` | Chaos strategies |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceChaos`) | 960-969 | New range from the block 950-999; 960-964 used, 965-969 spare; verified free by repository-wide search on `098caa920` |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 13 | 6 production (`ChaosOptions`, `ChaosOptionsValidator`, `ChaosErrorCodes`, `ChaosEnvironmentGate`, `ChaosStrategyFactory`, `ChaosServiceCollectionExtensions`), 5 tests (strategy factory, gate, options and validator, DI, guard), `PublicAPI.Unshipped.txt`, manifest rows |
| PR-2 | 12 | `IChaosController`, `ChaosController`, `ChaosHealthCheck`, `ChaosStrategyFactory` and the registration (modified), controller test, health check test, factory test, DI test and guard test (modified), `PublicAPI.Unshipped.txt`, manifest |
| PR-3 | 12 | `ChaosLogMessages`, `EventIdRanges.cs`, core `PublicAPI.Unshipped.txt`, `EncinaEventIdAllocationTests.cs`, `ChaosEnvironmentGate`, `ChaosController` and `ChaosStrategyFactory` (modified, they emit the logs), their three tests (modified, log assertions), Resilience `PublicAPI.Unshipped.txt`, manifest |
| PR-4 | 8 | contract test, property test, 2 justification `.md`, feature page, README section, changelog fragment, manifest row |

Each PR is within 15 files and 1,500 lines. No test double changes belong to this plan.

Per-file targets (new files; baseline: none exists on `origin/main`):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `ChaosOptions.cs`, `ChaosErrorCodes.cs` | 90 | 0 | 0 | 0 | 0 | Property bags and constants read by validator and factory tests; no parameters |
| `ChaosOptionsValidator.cs` | 95 | 0 | 0 | 0 | 0 | Internal; one test per rule through `InternalsVisibleTo` |
| `IChaosController.cs` | 0 | 0 | 0 | 0 | 0 | Interface, no executable code |
| `ChaosController.cs` | 95 | 0 | 90 | 0 | 0 | Internal; setters return `Left` for bad values (unit), contract test runs the real controller |
| `ChaosStrategyFactory.cs` | 90 | 0 | 0 | 85 | 0 | Rate 0, rate 1 and latency with a fake clock; the property test drives the seeded decisions |
| `ChaosServiceCollectionExtensions.cs` | 90 | 100 | 0 | 0 | 0 | One guarded public method plus the DI test |
| `ChaosEnvironmentGate.cs` | 95 | 0 | 0 | 0 | 0 | Internal; every branch (null, allowed, denied) |
| `ChaosHealthCheck.cs` | 90 | 0 | 0 | 0 | 0 | Healthy and Degraded paths; constructed through DI in the test |
| `ChaosLogMessages.cs` | 85 | 0 | 0 | 0 | 0 | Log assertions with a fake logger |

CRAP of each added method is at most 10 (the factory splits generator construction into small methods).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #145: chaos engineering in Encina.Resilience (ADR-055 R2). AGENTS.md rules apply. The foundation is merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) ChaosOptions, validator and ChaosErrorCodes; (2) IChaosController and ChaosStrategyFactory adding Polly's latency and outcome chaos in the last slot; (3) AddEncinaChaos; (4) the environment gate and a Degraded health check; (5) EventIds 960-964; (6) unit, guard, contract and property tests; (7) documentation and fragments.

KEY PATTERNS: off by default and fail closed (a missing environment denies); explicit opt-in is logged; the injected failure is a coded Left with ErrorCategory.Unavailable and a constant message; time and randomness injectable; no scoped service in a singleton; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: docs/architecture/adr/055-resilience-family.md, src/Encina.AspNetCore/EncinaAspNetCoreOptions.cs, src/Encina/Results/EncinaErrors.cs, src/Encina.Kafka/Health/KafkaHealthCheck.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Chaos injects failures into live calls |
| 2 | OpenTelemetry | ✅ | Polly's strategy events enriched by R5; no instrument of our own unless Polly lacks the event (Phase 5); tenant tag only with multi-tenancy |
| 3 | Structured Logging | ✅ | EventIds 960-964 |
| 4 | Health Checks | ✅ | `ChaosHealthCheck` reports `Degraded` while chaos is on |
| 5 | Validation | ✅ | Options validated at startup; controller setters return `Left` |
| 6 | Resilience | ✅ | Chaos is the last slot of the R2 order and tests the others |
| 7 | Distributed Locks | ❌ | No shared state; the controller is per process |
| 8 | Transactions | ❌ | An injected `Left` makes the transaction behavior roll back as for any failure; no code here |
| 9 | Idempotency | ❌ | A command is retried after an injected failure only when idempotent (ADR-029) |
| 10 | Multi-Tenancy | ❌ | Chaos targets request types, not tenants; the telemetry tenant tag follows ADR-054 D4; no tenant-qualified key exists |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Infrastructure; the activation is logged (EventId 960 and 962), not audited |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| The foundation: `Encina.Resilience`, compiler with a last strategy slot, options front-end, strategy-factory contract | No issue exists: issue file `resilience-foundation-encina-resilience.md` (family resilience-patterns-1, to be opened by the orchestrator) | Blocks implementation (choice 1) |
| #2382 startup requirements validator | Open; plan on main | Its rule "test-only implementations outside Development" is the family of the gate; this plan's gate is self-contained and may later move into that validator |
| #728 telemetry plan | On main, choices pending | Instrument names only if Polly lacks the chaos event |
| #136 hedging and #335 timeouts (sibling plans) | By number | Chaos tests of their strategies |

---

## Next Steps

1. Commit this plan, open its PR and link it from #145.
2. When #145 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1 to 6).
3. Brief workers for PR-1 to PR-4 in order, after the foundation is merged.
