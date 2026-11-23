# Implementation Plan: Configurable Trace Sampling by Request Type (`EncinaSampler`)

> **Issue**: [#183](https://github.com/dlrivada/Encina/issues/183)
> **Type**: Feature (observability, `Encina.OpenTelemetry`)
> **Related**: ADR-054 (settled point 1 the application owns the SDK, D2 child or link rule, D4 tenant tag), #132 (trace context propagation: a received `traceparent` carries the upstream sampling flag), #184 (request spans, sibling plan of this family), #2676 (tenant attribute helper), #906 (observability guide), #1043 (OTLP exporter option), #1578 (a flaky test caused by unseeded sampling: the lesson behind Design Choice 3), ADR-062
> **Complexity**: Medium (the sampler itself is small; the work is making the request name visible when the decision is taken, keeping child spans consistent with their parent, and being honest about what head-based sampling cannot do)
> **Estimated Scope**: ~420 lines of production code + ~900 lines of tests + 1 documentation file, in three code pull requests and one documentation pull request of at most 15 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-20, main `0336d31c7`)

- No sampler exists: a search of `src` for `Sampler` finds nothing, and `WithEncina()` (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:158-217`) calls `AddSource` and `AddMeter` only. A host gets the SDK default, a `ParentBasedSampler` over `AlwaysOnSampler`.
- `EncinaOpenTelemetryOptions` (`src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs`) has `ServiceName`, `ServiceVersion` and `EnableMessagingEnrichers`; it has no `Sampling` member. `WithEncina(OpenTelemetryBuilder, EncinaOpenTelemetryOptions?)` receives the options object, so a sampler can be applied there with `tracing.SetSampler(...)`.
- **The request type is not visible when the sampling decision is taken.** The dispatcher span is named `Encina.Send` for every request (`src/Encina/Diagnostics/EncinaDiagnostics.cs:12-39`, `SendStarted` then `StartActivity("Encina.Send", ...)`), and the type is added afterwards with `SetTag(ActivityTagNames.RequestType, ...)`. An OpenTelemetry sampler sees only the parent context, the trace id, the span name and kind, the attributes passed at creation and the links, so the sampler of the issue (`parameters.Tags` holding `encina.request.type`) would never find the tag. `CommandActivityPipelineBehavior` and `QueryActivityPipelineBehavior` name their spans `Encina.Command.<Name>` and `Encina.Query.<Name>` but also tag late; `Encina.Publish` (`src/Encina/Dispatchers/Encina.NotificationDispatcher.cs:54`) is named without the type.
- The tag keys are internal constants of core (`src/Encina/Diagnostics/ActivityTagNames.cs:13-16, 24-26`: `Encina.request_name`, `Encina.notification_name`); core's `InternalsVisibleTo` list has no entry for `Encina.OpenTelemetry`, so the sampler cannot read them.
- The OpenTelemetry package references the SDK (`OpenTelemetry` 1.17.0 in `Directory.Packages.props`), so `Sampler`, `ParentBasedSampler` and `SamplingResult` are available without a new dependency.
- Request tracing today produces up to three spans per `Send` when the opt-in activity behaviors are registered (the dispatcher span and a command or query span); sampling is decided once at the root and the children follow (Design Choice 6).

### What the issue proposes and what changed since

The issue (2026-era text, `status:proposal`) proposes `SamplingConfiguration` (default rate, per-request-type rates with wildcards, always trace errors), an `EncinaSampler` using `Random.Shared`, and head-based plus tail-based strategies with a slow-request threshold. Three points do not survive today's facts: the request name is not on the span at creation (above), a head-based sampler cannot know whether the request will fail or be slow, and `Random.Shared` makes the decision non-deterministic across the services of one trace (a second service sampling the same trace id decides differently) and the tests flaky (#1578 is the same failure in another test).

### What this plan delivers

- Core: the request, notification and stream spans pass their type name as a creation-time attribute, and a public `RequestSpanKeys` class holds the two key strings, so a sampler outside core can read them.
- `Encina.OpenTelemetry`: `SamplingConfiguration` (a default rate and an ordered rule list), `EncinaSampler` (deterministic by trace id, rule lookup by request name with wildcards, wrapped in a `ParentBasedSampler`), a validator, `EncinaOpenTelemetryOptions.Sampling`, and `WithEncina()` applying the sampler only when `Sampling` is set (opt-in).
- A guide that says what head sampling can and cannot do, with the collector `tail_sampling` configuration for error and slow-request retention (Design Choice 4).
- Shared pieces and owner: this plan owns `RequestSpanKeys`, `SamplingConfiguration` and `EncinaSampler`; #184 owns the request and behavior spans that carry the key; #132 owns the received sampling flag (honored by the parent-based wrapper); #2676 owns the tenant attribute.
- Affected packages: `Encina` (core) and `Encina.OpenTelemetry`. Provider category: observability (OpenTelemetry), no database or transport provider.
- Not delivered, with the reason: tail-based sampling inside the process and "always trace errors" in code (Design Choice 4); per-tenant rates (the tenant attribute does not exist at span creation until #2676, and ADR-054 D4 allows it only when multi-tenancy is on: a follow-up issue file `artifacts/issues/per-tenant-sampling-rates.md`).

---

## Design Choices

<details>
<summary><strong>1. Build an Encina sampler or document the stock samplers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EncinaSampler` in `Encina.OpenTelemetry`, applied by `WithEncina()` when `Sampling` is configured** | Per-request-type rates and wildcards, which no stock sampler offers (a stock sampler cannot see a request type); one switch in the options | New public types to maintain |
| **B) Document `TraceIdRatioBasedSampler` and let the host configure it** | No code | One global rate only: the issue's acceptance criteria (per-request-type rates, wildcards) are not met |

### Chosen Option: **A — An Encina sampler** (recommended, pending the maintainer)

### Rationale

- A wins because per-request-type sampling is the point of the issue and needs the request name, which only Encina knows. B wins for a host that wants one global ratio; it keeps that by leaving `Sampling` unset (the SDK default, or the host's own `SetSampler`, is untouched).
- ADR-054 settled point 1 leaves the SDK and exporters to the application: `WithEncina()` sets a sampler only when the host sets `Sampling`, so a host that already calls `SetSampler` and omits `Sampling` is unaffected.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How the request name reaches the sampler</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pass the type name as an attribute at `StartActivity`, under a public key constant in core (`RequestSpanKeys.RequestName` and `NotificationName`, same strings as today)** | The OpenTelemetry sampling inputs include creation attributes; no span rename; the strings stay what dashboards and tests use | Four creation sites change (dispatcher, notification dispatcher, command and query behaviors) |
| **B) Parse the span name (`Encina.Command.<Name>`)** | No core change | `Encina.Send` and `Encina.Publish` carry no type, so the dispatcher span (the root) cannot be matched |
| **C) Rename the root spans to include the type (`Encina.Send CreateOrderCommand`)** | The name is a sampling input | A span-name change visible in every trace backend and dashboard; high-cardinality span names |

### Chosen Option: **A — Creation-time attribute under a public key** (recommended, pending the maintainer)

### Rationale

- A wins because it keeps span names stable and uses the input the sampling API defines for exactly this. B wins for a host that only registers the command and query behaviors and never samples the root by type. C wins for a team that already groups traces by span name.
- Phase 1 includes a test that a span started with creation attributes is still a child of `Activity.Current` (the `ActivitySource.StartActivity` overload that takes tags also takes a parent context; the test pins the behavior of passing `default`).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Deterministic decision by trace id</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The rate is applied to the trace id: the decision is `traceIdValue < rate * 2^63` (the same function as the stock `TraceIdRatioBasedSampler`)** | The same trace id gets the same decision in every service and in every test; no random source | The decision depends on the trace id distribution (uniform for W3C ids) |
| **B) `Random.Shared.NextDouble() < rate` (the issue's text)** | Simple | Two services sample one trace differently when their rates differ; a test needs a seeded random or becomes flaky |

### Chosen Option: **A — Trace id based**

### Rationale

- The OpenTelemetry specification defines `TraceIdRatioBased` as a deterministic function of the trace id, and `AGENTS.md` section 9 requires deterministic tests. The sampler is only consulted for root spans (Design Choice 6), so a given trace has one decision at its root.

**Class:** Trivial
**Settled by:** OpenTelemetry specification (Trace SDK, built-in samplers: `TraceIdRatioBased` is a deterministic function of the TraceId) and `AGENTS.md` section 9 (tests deterministic)

</details>

<details>
<summary><strong>4. Error, slow-request and tail-based sampling</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Head-based only; "always trace errors" and "slow requests" are served by the collector `tail_sampling` processor, with a ready configuration in the guide** | Correct: the decision at the root cannot depend on an outcome that does not exist yet; no half-recorded traces | The host needs a collector for that policy; the issue's `AlwaysTraceErrors` and tail options are not code |
| **B) A span-level upgrade: sample at a lower rate but record all spans, and flip the `Recorded` flag at the end of an error or slow span** | Keeps error spans without a collector | Only that span survives (its dropped children and the downstream services' decisions are already made): an orphaned error span in the backend; depends on the exporter honoring a flag changed at the end of the span, which the SDK does not promise |
| **C) In-process tail sampling: buffer every span of a trace until its root ends** | Whole traces kept by outcome | Memory bound per trace, loss on crash, only the local part of a distributed trace, and a processor ordering problem with the exporter: a new component to build and operate |

### Chosen Option: **A — Head-based only, collector for tail policies** (recommended, pending the maintainer)

### Rationale

- A wins when the host runs a collector (the OpenTelemetry recommendation for tail policies; ADR-054 settled point 1 keeps exporters and their topology with the application). B or C win for a single-service deployment with no collector and a hard cost limit; they are separate work the maintainer would have to approve, each with its own ADR-sized trade-off.
- If the maintainer picks B or C, the acceptance criteria "always trace errors", "slow request sampling" and "tail-based strategy" become a second plan; this plan's types do not block them (`SamplingConfiguration` gains members).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. How a request name is matched to a rate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rules are an ordered list; the most specific match wins (exact name, then the pattern with the longest literal text, then the first declared among equals); the default rate applies when nothing matches** | Same result whatever the order of an unordered source (a JSON configuration, a dictionary); the issue's `GetHealthQuery` beats `*Query` | Slightly more logic than first-match |
| **B) First match in declaration order wins** | Trivial to explain and to implement | The result depends on order, and the issue's examples (`*Query` after an exact name) only work when the author orders them right |

### Chosen Option: **A — Most specific wins** (recommended, pending the maintainer)

### Rationale

- A wins because configuration often arrives unordered (`IConfiguration` binding); the issue itself uses a `Dictionary`. B wins for a host that wants firewall-style explicit ordering.
- Patterns support only `*` (any run of characters, including none), compared ordinally and case-sensitively against `Type.Name`; at most 128 characters, at most 256 rules (a bound on the match cost).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Parent-based wrapper</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EncinaSampler` is the root sampler of a `ParentBasedSampler`** | A child span follows its parent's flag, so a `Send` consumer span stays in the sampled trace (ADR-054 D2) and the behavior spans under `Encina.Send` are never half-sampled; a received `traceparent` flag is honored | A rule never applies to a span that has a parent |
| **B) `EncinaSampler` standalone** | Every span consults the rules | A trace can have sampled and dropped spans; a producer's decision is ignored by its consumer |

### Chosen Option: **A — Parent-based over the Encina root sampler**

### Rationale

- ADR-054 D2 makes a `Send` consumer span a child of the producer span in the same trace; a trace with a dropped child is broken. The OpenTelemetry specification defines `ParentBased` for exactly this.

**Class:** Settled
**Settled by:** ADR-054 D2 (a `Send` consumer span is a child in the same trace) and the OpenTelemetry specification (Trace SDK, `ParentBased` sampler); only A keeps a trace whole

</details>

<details>
<summary><strong>7. Changing rates while the application runs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EncinaSampler.Update(SamplingConfiguration)`: the validated configuration is an immutable snapshot swapped atomically (`Volatile.Write`)** | The issue's "dynamic rate adjustment" at the cost of one method; a host wires it to `IOptionsMonitor` itself | One more public method |
| **B) Rates fixed at startup** | Smallest surface | A rate change needs a restart, which is the reason the issue asks for the feature (raise sampling during an incident) |

### Chosen Option: **A — An atomic swap** (recommended, pending the maintainer)

### Rationale

- A wins for incident response; B wins when the host never changes rates and wants the smallest API. An invalid configuration is rejected (the old snapshot stays) and logged by event 7015 with the reason code.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | `Sampling` is `null` by default: no sampler is set and the SDK default applies; setting it is the opt-in | Settled | `AGENTS.md` section 1 (every feature opt-in); ADR-054 settled point 1 |
| C2 | Validation at `WithEncina()` and `Update`: rates are finite and within 0 to 1 (NaN and infinity rejected), patterns non-empty, at most 128 characters, no duplicate pattern, at most 256 rules. Failure handling: a failure at `WithEncina()` throws `InvalidOperationException` at startup and a failure at `Update` returns `false`, keeps the old snapshot and logs 7015; no `EncinaError` is created, so no ADR-062 `ErrorCategory` (a category is chosen where an `EncinaError` is created). Alternatives: return `Either<EncinaError, Unit>` from `Update` (ADR-001 style, which then needs an error code and an `ErrorCategory`) or throw from `Update` as well | Dilemma (recommended, pending the maintainer) | `AGENTS.md` section 3 does not say how a configuration object reports invalid values, and more than one option fits; the recommendation keeps a hot reload from crashing a running host while a bad startup configuration fails fast |
| C3 | The matched attribute is `RequestSpanKeys.RequestName` for requests, commands, queries and streams and `RequestSpanKeys.NotificationName` for `Encina.Publish`; a span with neither attribute (a store or transport span) gets the default rate | Trivial | The two key strings exist today as `ActivityTagNames.RequestName` and `NotificationName` (`src/Encina/Diagnostics/ActivityTagNames.cs:13-16, 24-26`); the sampler reads those strings and no other key |
| C4 | The decision logs no per-span line (a hot path); only the configuration is logged: event 7013 configured (rule count and default rate, numbers only), 7014 updated, 7015 update rejected (reason code) | Settled | `AGENTS.md` section 7 (EventIds in the registered range, packed), ADR-021 |
| C5 | A counter `encina.sampling.decisions` (unit `{span}`, attribute `encina.sampling.decision` = `sampled` or `dropped`) on a meter named `Encina.OpenTelemetry` counts decisions so a host sees its effective rate; no request name attribute (unbounded by user types) and no tenant attribute (ADR-054 D4 is #2676's) | Trivial | The name and attribute follow ADR-054 D3 (meter named after the package) and settled point 4 (name rules); the choice of one counter is the plan's own, and it exists only when Design Choice 1 option A is built |
| C6 | The creation-time tag array is allocated only when `HasListeners()` is true, so without a listener the `StartActivity` path costs nothing | Settled | ADR-054 settled point 7 |
| C7 | `SamplingConfiguration` and `SamplingRule` are immutable records; the rule list is a `IReadOnlyList<SamplingRule>`; the sampler stores one `Volatile` reference to a compiled snapshot (exact-name dictionary plus a pattern array sorted by literal length descending) | Trivial | `AGENTS.md` section 3 (immutable value types in a hot path); the sorted array makes the lookup order independent |
| C8 | Public names: `Encina.OpenTelemetry.Sampling.SamplingConfiguration`, `SamplingRule`, `EncinaSampler`; core `Encina.Diagnostics.RequestSpanKeys` | Dilemma (recommended, pending the maintainer) | public surface; alternatives are the namespace `Encina.OpenTelemetry` and `ActivitySamplingRules` |

---

## Implementation Phases

### Phase 1: Creation-Time Request Attributes in Core (pull request S1)

> **Goal**: The request or notification name is an attribute of the span when the sampler runs.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Diagnostics/RequestSpanKeys.cs`: `public static class RequestSpanKeys { public const string RequestName = "Encina.request_name"; public const string NotificationName = "Encina.notification_name"; }` with XML docs saying these are the attributes a sampler reads. **Modify** `src/Encina/Diagnostics/ActivityTagNames.cs` so its `RequestName` and `NotificationName` constants are `RequestSpanKeys` references (one source of truth, the strings do not change).
2. **Modify** `src/Encina/Diagnostics/EncinaDiagnostics.cs` (`StartActivity`, line 24-38), `src/Encina/Pipeline/Behaviors/CommandActivityPipelineBehavior.cs` (`StartActivity`), `QueryActivityPipelineBehavior.cs` and `src/Encina/Dispatchers/Encina.NotificationDispatcher.cs:54`: start the span with the `StartActivity(name, kind, parentContext: default, tags)` overload and the one-element name attribute, only inside the existing `HasListeners()` branch, and keep every later `SetTag` as it is.
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.json`; add `changelog.d/183-request-span-creation-attributes.added.md` through `mechanical-fixer`.
4. Test (new): `tests/Encina.UnitTests/Diagnostics/RequestSpanCreationAttributesTests.cs`: an `ActivityListener` with a `Sample` callback that records `options.Tags` sees the name attribute for `Send`, a command, a query and a publish; a nested span is still a child of the current activity (same trace id, `ParentSpanId` equal) after the change; with no listener no allocation path runs (`StartActivity` returns `null`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #183, plan docs/plans/trace-sampling-implementation-plan-183.md, phase 1.
An OpenTelemetry sampler sees only creation-time attributes; Encina tags the request name after StartActivity, so no sampler can match it.

TASK:
1. Add the public RequestSpanKeys constants (same strings as today) and make ActivityTagNames refer to them.
2. Pass the request or notification name as an attribute at StartActivity in EncinaDiagnostics, the command and query behaviors and the
   notification dispatcher, inside the existing HasListeners() guard.
3. Add RequestSpanCreationAttributesTests with a real ActivityListener asserting the attributes and the parent link.
4. PublicAPI, manifest and changelog through mechanical-fixer.

KEY RULES:
- Span names and tag strings do not change. A started span is still a child of Activity.Current (assert it).
- Cost nothing without a listener (ADR-054 settled point 7).

REFERENCE FILES:
- src/Encina/Diagnostics/EncinaDiagnostics.cs, src/Encina/Diagnostics/ActivityTagNames.cs
- src/Encina/Dispatchers/Encina.NotificationDispatcher.cs, src/Encina/Pipeline/Behaviors/CommandActivityPipelineBehavior.cs
```

</details>

---

### Phase 2: Configuration, Rule Matching and Validation (pull request S2)

> **Goal**: A validated, immutable description of the rates and the lookup that applies it.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** (all new) `src/Encina.OpenTelemetry/Sampling/SamplingConfiguration.cs` (`public sealed record SamplingConfiguration(double DefaultRate, IReadOnlyList<SamplingRule> Rules)`, `public sealed record SamplingRule(string Pattern, double Rate)`), `src/Encina.OpenTelemetry/Sampling/SamplingRuleMatcher.cs` (`internal sealed class`: compiled snapshot of an exact dictionary and a pattern array sorted by literal length; `double Resolve(string? name)`, ordinal case-sensitive `*` matching without allocation) and `src/Encina.OpenTelemetry/Sampling/SamplingConfigurationValidator.cs` (`internal static` returning a reason code or `null`, per register row C2).
2. **Modify** `src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs`: `public SamplingConfiguration? Sampling { get; set; }` (default `null`). **Modify** `src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj`: add `<InternalsVisibleTo Include="Encina.PropertyTests" />` next to the four existing entries (UnitTests, GuardTests, ContractTests, IntegrationTests), because the property test targets the `internal` `SamplingRuleMatcher` (without it the test project fails with CS0122; `tests/Encina.PropertyTests/Encina.PropertyTests.csproj` already references `Encina.OpenTelemetry`, so no test project edit is needed). **Modify** `src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.OpenTelemetry.json`; add `changelog.d/183-sampling-configuration.added.md` through `mechanical-fixer`.
3. Tests (new): `tests/Encina.UnitTests/OpenTelemetry/Sampling/SamplingRuleMatcherTests.cs` (exact beats pattern, longest literal beats shorter, declaration order among equals, `*` alone, prefix, suffix and infix patterns, an empty or null name gets the default), `SamplingConfigurationValidatorTests.cs` (NaN, infinity, negative, above one, empty and oversize patterns, duplicates, 257 rules), `tests/Encina.GuardTests/OpenTelemetry/Sampling/SamplingConfigurationGuardTests.cs` (null rules list, null pattern, a valid empty rule list), `tests/Encina.PropertyTests/OpenTelemetry/Sampling/SamplingRuleMatcherProperties.cs` (FsCheck: the result is always one of the configured rates and does not depend on the rule order when no two matching patterns have the same literal length).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #183 plan docs/plans/trace-sampling-implementation-plan-183.md, phase 2. Phase 1 merged.

TASK:
1. Create SamplingConfiguration and SamplingRule (immutable records), SamplingRuleMatcher (exact dictionary plus patterns sorted by
   literal length, ordinal, no allocation per lookup) and SamplingConfigurationValidator (reason codes) in src/Encina.OpenTelemetry/Sampling/.
2. Add EncinaOpenTelemetryOptions.Sampling (null by default) and InternalsVisibleTo Encina.PropertyTests in Encina.OpenTelemetry.csproj
   (the property test targets the internal SamplingRuleMatcher).
3. Unit, guard and FsCheck property tests; PublicAPI, manifest and changelog through mechanical-fixer.

KEY RULES:
- Most specific rule wins; ties by declaration order; only '*' is a wildcard.
- Rates are finite and between 0 and 1; reject NaN. No EncinaError is created.

REFERENCE FILES:
- src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 3: The Sampler and `WithEncina()` (pull request S3)

> **Goal**: `WithEncina()` applies a parent-based, deterministic, updatable sampler when `Sampling` is set.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** (all new) `src/Encina.OpenTelemetry/Sampling/EncinaSampler.cs` (`public sealed class EncinaSampler : Sampler`; constructor `(SamplingConfiguration configuration, ILogger<EncinaSampler>? logger = null)`; `ShouldSample(in SamplingParameters)` reads `RequestSpanKeys.RequestName` or `NotificationName` from `parameters.Tags`, resolves the rate with the matcher and returns `RecordAndSample` when `TraceIdRatio(parameters.TraceId, rate)` holds, `Drop` otherwise; `public bool Update(SamplingConfiguration configuration)` validates and swaps the snapshot with `Volatile.Write`, logs 7014 or 7015 and returns whether it was applied), `src/Encina.OpenTelemetry/Sampling/SamplingMetrics.cs` (the counter of register row C5 on the meter `Encina.OpenTelemetry`) and `src/Encina.OpenTelemetry/Sampling/SamplingLog.cs` (`[LoggerMessage]` EventIds 7013 configured, 7014 updated, 7015 update rejected; "Event IDs: 7013-7015 (see EventIdRanges.OpenTelemetry)"; no new range).
2. **Modify** `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` (`WithEncina`, inside `WithTracing`): when `options.Sampling` is set, validate it (throw `InvalidOperationException` with the reason code on failure) and call `tracing.SetSampler(provider => new ParentBasedSampler(new EncinaSampler(options.Sampling, provider.GetService<ILogger<EncinaSampler>>())))`; add `metrics.AddMeter("Encina.OpenTelemetry")` next to `AddMeter("Encina")` (until #2677's wildcard replaces the list). Register the `EncinaSampler` root as a singleton so a host can resolve it and call `Update`: `services` access goes through `builder.Services`.
3. **Modify** `src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.OpenTelemetry.json`; add `changelog.d/183-encina-sampler.added.md` through `mechanical-fixer`.
4. Tests (new): `tests/Encina.UnitTests/OpenTelemetry/Sampling/EncinaSamplerTests.cs` (rate 0 drops, rate 1 samples, a fixed trace id gives the same decision on repeated calls and across two sampler instances, a rule beats the default, a span without the attribute gets the default, `Update` changes the next decision and an invalid update keeps the old snapshot and returns `false`), `tests/Encina.UnitTests/OpenTelemetry/Sampling/WithEncinaSamplingTests.cs` (an `OpenTelemetryBuilder` with `WithEncina(options)` and a test `BaseProcessor<Activity>`: a request type at rate 0 produces no exported span, another at rate 1 produces one; a child of a dropped parent is dropped; an unsampled remote `traceparent` is honored; with `Sampling` unset the SDK default applies and a host's own `SetSampler` is not replaced; an invalid configuration throws at `WithEncina`), `tests/Encina.GuardTests/OpenTelemetry/Sampling/EncinaSamplerGuardTests.cs` (null configuration; a null logger is valid; `Update(null)` throws), `tests/Encina.PropertyTests/OpenTelemetry/Sampling/EncinaSamplerProperties.cs` (FsCheck: over 10,000 random trace ids the sampled share of rate r is within 3 percentage points of r; rate 0 never and rate 1 always samples).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #183 plan docs/plans/trace-sampling-implementation-plan-183.md, phase 3. Phases 1 and 2 merged.

TASK:
1. Create EncinaSampler (deterministic by trace id, rule lookup by RequestSpanKeys attribute, atomic Update), SamplingMetrics and
   SamplingLog (EventIds 7013-7015 of EventIdRanges.OpenTelemetry).
2. In WithEncina, when options.Sampling is set, validate and SetSampler(ParentBasedSampler(EncinaSampler)); register the sampler so a host can call Update;
   subscribe the Encina.OpenTelemetry meter.
3. Tests with a real SDK builder and a test BaseProcessor<Activity>, a determinism check and FsCheck properties;
   PublicAPI, manifest and changelog through mechanical-fixer.

KEY RULES:
- No Random: the decision is a function of the trace id. No per-span log line. Never replace a sampler the host set when Sampling is null.
- The sampler is wrapped in ParentBasedSampler (ADR-054 D2).

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs
- the OpenTelemetry SDK TraceIdRatioBasedSampler (the decision function to match)
```

</details>

---

### Phase 4: Documentation (pull request D1)

> **Goal**: A cost guide that is honest about head sampling.

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/features/trace-sampling.md` through `docs-writer` (how-to): configuring `Sampling` with per-request rates, how a rule is matched, why the rate is applied at the root and the children follow, what head sampling cannot do (errors, slow requests) and the collector `tail_sampling` configuration for them, the `encina.sampling.decisions` counter, `Update` with `IOptionsMonitor`.
2. The `Encina.OpenTelemetry` README is not edited here (the observability guide is #906's).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #183 plan docs/plans/trace-sampling-implementation-plan-183.md, phase 4, documentation only.

TASK:
1. Write docs/features/trace-sampling.md (one Diataxis quadrant: how-to). Verify every identifier exists in src/.

KEY RULES:
- Use the encina-docs skill; no emojis; no hand-typed coverage figures; the collector configuration is an example, not Encina code.

REFERENCE FILES:
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| OpenTelemetry Trace SDK, sampling | A sampler receives parent context, trace id, name, kind, creation attributes and links; `ParentBased`, `TraceIdRatioBased` | Phase 1 attribute, Phase 3 sampler |
| OpenTelemetry sampling concepts | Head-based sampling decides at the start; tail-based needs the whole trace and a collector | Design Choice 4 |
| W3C Trace Context | The `sampled` flag of `traceparent` | Honored by the parent-based wrapper |
| ADR-054 | Settled point 1 SDK owned by the application, settled point 7 zero cost, D2 child or link, D3 meter name, D4 tenant tag | Opt-in, scope |
| ADR-062 | Error category contract | No error is created (register row C2) |
| `AGENTS.md` sections 3, 7, 9 | Startup validation, EventIds, deterministic tests | Validation, events 7013-7015, determinism |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EncinaDiagnostics` | `src/Encina/Diagnostics/EncinaDiagnostics.cs` | Gains the creation-time attribute |
| `ActivityTagNames` | `src/Encina/Diagnostics/ActivityTagNames.cs` | Refers to `RequestSpanKeys` |
| `CommandActivityPipelineBehavior`, `QueryActivityPipelineBehavior` | `src/Encina/Pipeline/Behaviors/` | Creation-time attribute |
| `Encina.NotificationDispatcher` | `src/Encina/Dispatchers/Encina.NotificationDispatcher.cs:54` | `Encina.Publish` attribute |
| `WithEncina` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:158` | Applies the sampler |
| `EncinaOpenTelemetryOptions` | `src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs` | Gains `Sampling` |
| `EventIdRanges.OpenTelemetry` | `src/Encina/Diagnostics/EventIdRanges.cs:303` | Ids 7013-7015 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.OpenTelemetry` | 7013-7015 (inside `EventIdRanges.OpenTelemetry` 7000-7099) | 7013 configured, 7014 updated, 7015 update rejected (reason code); 7010-7012 belong to the plan of #174; no new range and no id of the block 5970-5999 |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI and the changelog fragment; each pull request builds and passes alone.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Creation-time attributes in core | 1 | 6 (`ActivityTagNames`, `EncinaDiagnostics`, the two behaviors, the notification dispatcher, PublicAPI) | 1 | 0 | 1 | 1 (changelog) | 10 | ~420 |
| S2 | Configuration, matcher, validator | 3 | 3 (options, `Encina.OpenTelemetry.csproj` for the property-test `InternalsVisibleTo`, PublicAPI) | 4 | 0 | 1 | 1 | 12 | ~640 |
| S3 | Sampler and `WithEncina()` | 3 | 2 (`ServiceCollectionExtensions`, PublicAPI) | 4 | 0 | 1 | 1 | 11 | ~820 |
| D1 | Documentation | 0 | 0 | 0 | 0 | 0 | 1 (guide) | 1 | ~140 |
| **Total** | | **7** | **11** | **9** | **0** | **3** | **4** | **34** | **~2,020** |

The columns sum to the total (7 + 11 + 9 + 0 + 3 + 4 = 34; 10 + 12 + 11 + 1 = 34). The pull request of this plan itself is one file.

Per-file coverage targets (per flag, one sentence each), written to the package manifests:

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `Encina.OpenTelemetry/Sampling/EncinaSampler.cs` | 95 | 70 | 0 | 85 | Every decision path (rule, default, no attribute, drop, update accepted and rejected) runs with a real sampler; guard covers the constructor and `Update(null)`; the property test drives random trace ids; no contract applies to a concrete sampler |
| `Encina.OpenTelemetry/Sampling/SamplingRuleMatcher.cs` | 95 | 0 | 0 | 90 | Matching is pure and fully reachable with plain strings; the property test covers order independence; it is internal with no guardable public parameter |
| `Encina.OpenTelemetry/Sampling/SamplingConfigurationValidator.cs` | 95 | 0 | 0 | 0 | Each reason code is one reachable branch; internal static, no public parameter |
| `Encina.OpenTelemetry/Sampling/SamplingConfiguration.cs` | 90 | 80 | 0 | 0 | Two records whose only logic is the constructor checks, covered by unit and guard tests |
| `Encina.OpenTelemetry/Sampling/SamplingMetrics.cs`, `SamplingLog.cs` | 0 | 0 | 0 | 0 | A static counter holder and generated `[LoggerMessage]` code with no parameters of their own; executed through `EncinaSampler.cs` |
| `Encina/Diagnostics/RequestSpanKeys.cs` | 0 | 0 | 0 | 0 | Two constants with no executable code; read through the creation-attribute tests |
| `Encina/Diagnostics/EncinaDiagnostics.cs` and the changed `StartActivity` methods | current target kept | current target kept | 0 | 0 | The changed lines run in `RequestSpanCreationAttributesTests`; no new public parameter |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #183 - configurable trace sampling by request type - in three code pull requests and one documentation pull request.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always; edit source only with the
  Edit/Write tools; PublicAPI, manifests and changelog fragments through mechanical-fixer; docs through docs-writer.
- ADR-054: the application owns the SDK (no sampler unless Sampling is configured); D2: a Send consumer span is a child, so the sampler
  is parent-based; D4: no tenant attribute here (#2676).

IMPLEMENTATION OVERVIEW:
1. S1: RequestSpanKeys in core and creation-time request or notification name attributes on the Send, command, query and publish spans.
2. S2: SamplingConfiguration, SamplingRule, SamplingRuleMatcher (most specific wins), SamplingConfigurationValidator, options member.
3. S3: EncinaSampler (deterministic by trace id, atomic Update), SamplingMetrics, SamplingLog (7013-7015), WithEncina wiring with ParentBasedSampler.
4. D1: docs/features/trace-sampling.md including the collector tail_sampling configuration for error and slow-request retention.

KEY PATTERNS:
- Decision = function of the trace id (no Random). Tests use a real SDK builder with a BaseProcessor<Activity>, never reflection.
- Head sampling cannot see outcomes: do not add AlwaysTraceErrors or tail options (Design Choice 4 recommends the collector).
- Guard tests for every new public constructor and method with parameters.

REFERENCE FILES:
- src/Encina/Diagnostics/EncinaDiagnostics.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
- docs/plans/trace-sampling-implementation-plan-183.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A decision is a function of the trace id and the rule snapshot, with nothing to cache |
| 2 | OpenTelemetry | ✅ Phases 1-3 | This is the sampling work: creation attributes, the sampler, the decision counter |
| 3 | Structured Logging | ✅ Phase 3 | Events 7013-7015, configuration only, never per span |
| 4 | Health Checks | ❌ N/A | A sampling configuration has no dependency to check |
| 5 | Validation | ✅ Phase 2 | Rates and patterns validated at startup and at `Update` (register row C2) |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ❌ N/A | No shared state; the snapshot swap is a local `Volatile.Write` |
| 8 | Transactions | ❌ N/A | Sampling is non-transactional |
| 9 | Idempotency | ❌ N/A | The decision is a pure function of the trace id |
| 10 | Multi-Tenancy | ⏭️ Follow-up issue file | `artifacts/issues/per-tenant-sampling-rates.md`: per-tenant rates need the tenant attribute at span creation (#2676) and ADR-054 D4 allows it only when multi-tenancy is on |
| 11 | Module Isolation | ❌ N/A | Sampling operates on the request name, not on a module |
| 12 | Audit Trail | ❌ N/A | Infrastructure configuration, not a security-relevant state |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 accepted | ✅ On `main` | Settled points 1 and 7, D2, D3 |
| OpenTelemetry SDK 1.17.0 in `Encina.OpenTelemetry` | ✅ On `main` | `Sampler`, `ParentBasedSampler` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Request spans and behavior spans | #184 | Its spans use the same creation-time attribute; plan owned by this family |
| Received `traceparent` | #132 | Supplies the upstream sampled flag the parent-based wrapper honors |
| Tenant attribute helper | #2676 | Needed by the per-tenant rates follow-up |
| Meter rename and wildcard subscription | #2677 | Replaces the explicit `AddMeter("Encina.OpenTelemetry")` line |
| Observability guide | #906 | Links the sampling guide |

---

## Next Steps

1. Link this plan from #183 and ask the maintainer to answer Design Choices 1, 2, 4, 5 and 7 and register rows C2 and C8 when work starts.
2. Implement S1, S2 and S3 in order, then D1; when #183 is picked up, ask that its acceptance criteria be amended to the decision of Design Choice 4 (head-based in code, error and slow retention through the collector).
