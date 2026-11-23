# Implementation Plan: Jaeger Distributed Tracing Integration (`AddEncinaJaeger`)

> **Issue**: [#613](https://github.com/dlrivada/Encina/issues/613)
> **Type**: Feature (observability, trace exporter)
> **Related**: #1043 (OTLP exporter option in `Encina.OpenTelemetry`, prerequisite), #1049 (OTLP endpoint health check), #183 (trace sampling, owns sampling), #614 (Zipkin, sibling plan of this family), #180 (Prometheus, sibling plan), ADR-054 (settled points 1 and 3, D3, D4), #906 (observability guide), ADR-062
> **Complexity**: Low (about 120 lines of production code; the care is in not duplicating #1043 and #183 and in two SDK details: named exporter options and the HTTP path)
> **Estimated Scope**: ~120 lines of production code + ~430 lines of tests + 1 README section, in one code pull request of 12 files
> **Milestone**: v0.14.0 Hardening

---

## Summary

### What exists today (read on 2026-11-23, main `682b1d70b`)

- There is no Jaeger code. `Encina.OpenTelemetry` references `OpenTelemetry.Exporter.OpenTelemetryProtocol` as `PrivateAssets="all"` (`src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj:24`, the version `1.17.0` is pinned in `Directory.Packages.props`) and calls it nowhere; `WithEncina()` only subscribes sources and meters and sets the resource name (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:166-217`).
- Jaeger receives OTLP natively. The repository's own stack proves it: `.github/observability/docker-compose.observability.yml` runs `jaegertracing/all-in-one:1.57` with OTLP gRPC and HTTP mapped to host ports 4320 and 4321, and `otel-collector-config.yaml` exports traces to it with `otlp/jaeger`. The issue's own Alternative 1 says the legacy Jaeger client is deprecated in favor of OTLP, so the exporter is the OTLP one and no Jaeger-specific SDK package exists to add.
- The plan of #1043 (open issue, plan on `main`) adds `EncinaOpenTelemetryOptions.EnableOtlpExporter` and `ConfigureOtlpExporter`, which wire one OTLP configuration to traces, metrics and logs and drop `PrivateAssets="all"`. A Jaeger instance is a trace backend (in the repository's stack metrics go to Prometheus and logs to Loki through the Collector), so `EnableOtlpExporter` aimed straight at Jaeger sends metrics and logs to a backend that stores neither, and a host without a Collector needs a traces-only exporter.
- Sampling is owned elsewhere: the plan of #183 adds `EncinaOpenTelemetryOptions.Sampling` and an `EncinaSampler`; the issue's `JaegerOptions.SamplingRate` would be a second owner of `SetSampler`, where the last call wins silently.
- The issue's `JaegerOptions.ServiceName` defaults to `"encina-service"`. `WithEncina()` already sets the resource name from `EncinaOpenTelemetryOptions.ServiceName`; a Jaeger default applied afterwards would silently rename the service in every host that set its own.

### What the issue proposes and what changed since

The issue (2026-era text, `status:proposal`) proposes a package `Encina.OpenTelemetry.Jaeger` with `AddEncinaJaeger(Action<JaegerOptions>)`, options `Endpoint` (default `http://localhost:4317`), `ServiceName` and `SamplingRate`, OTLP gRPC only, and an integration test against the Jaeger all-in-one container. Four points need correcting: the helper is a thin layer over the SDK (no new dependency is needed once #1043 makes the OTLP package public), `SamplingRate` and a default `ServiceName` duplicate decisions owned elsewhere, the default endpoint is right only for gRPC (for HTTP the SDK needs the full signal path when the endpoint is set in code), and a second unnamed `AddOtlpExporter` would share its options with the one of #1043.

### What this plan delivers

- `JaegerOptions`, a startup validator and `AddEncinaJaeger(this IServiceCollection, Action<JaegerOptions>?)`: a traces-only OTLP exporter registered under its own name, with Jaeger defaults for each protocol and an environment attribute (Phase 1).
- A real-Jaeger integration test through a Testcontainers fixture, for both protocols (Phase 2).
- Shared pieces and owner: #1043 owns the OTLP package visibility and the all-signals wiring; #183 owns sampling; #1049 and the follow-up issue file `artifacts/issues/exporter-endpoint-health-checks.md` own endpoint reachability; #906 owns the guide; this plan owns the Jaeger helper.
- Affected packages: `Encina.OpenTelemetry` (a `Jaeger` folder, under the recommended Design Choice 1), `Encina.TestInfrastructure` (one fixture). Provider category: observability (OpenTelemetry exporters); no database or transport provider.
- Not delivered, with the reason: `SamplingRate` (Design Choice 3); a default `ServiceName` (register row C4); a Jaeger health check (the follow-up issue file); metrics and logs export to Jaeger (not accepted by Jaeger).

---

## Design Choices

<details>
<summary><strong>1. Where the Jaeger helper lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Jaeger` folder in `Encina.OpenTelemetry`, no new package** | The helper needs only the OTLP exporter, which #1043 turns into a normal dependency of the package; one dependency for hosts; no project, `PublicAPI` pair, manifest or three test-project references | The package gains a public helper that only some hosts use (it costs nothing unless called) |
| **B) A new package `Encina.OpenTelemetry.Jaeger`** (the issue) | A named package per backend, independent versioning, matches #614 and #178 and #179 | A package of about 120 lines with no dependency of its own: project, README, manifest, solution entry and test references for little code |
| **C) No code: document `WithTracing(t => t.AddOtlpExporter(...))`** | No API | The traces-only, named-options and HTTP-path details are the parts hosts get wrong; the issue's acceptance list (validation, defaults, environment tag) is unmet |

### Chosen Option: **A - a folder in `Encina.OpenTelemetry`** (recommended, pending the maintainer)

### Rationale

- The rule that separates this from #614 is the dependency: a helper that needs no new dependency adds nothing to opt out of, while Zipkin needs `OpenTelemetry.Exporter.Zipkin` and so earns a package (pay-for-what-you-use, `AGENTS.md` section 1).
- B wins when the maintainer wants every backend to be a separate, discoverable NuGet package whatever its size, or an independent release cadence; C wins when the maintainer sees three lines of SDK code as not worth any Encina API. If B is chosen, the file count of the pull request grows by the project file, two `PublicAPI` files, a README, a manifest, the solution entry and three test-project references (a split into two pull requests), and the ADR number, if one is wanted, is assigned when the ADR pull request opens.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Which exporter registration the helper makes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Traces only, through the named overload `AddOtlpExporter("EncinaJaeger", configure)` on the `TracerProviderBuilder`** | Jaeger gets only what it ingests; the named options never alias the default unnamed `OtlpExporterOptions` that #1043 configures, so both can coexist without sharing an endpoint | The host must not enable both for traces (documented, register row C2) |
| **B) The unnamed `AddOtlpExporter(configure)` for traces and metrics, as #1043 does** | One shape for all signals | Metrics and logs sent to Jaeger fail; an unnamed second registration shares options with #1043's and the endpoints overwrite each other |
| **C) Reuse `EncinaOpenTelemetryOptions.EnableOtlpExporter` and tell hosts to point it at Jaeger** | No new code | All three signals go to one endpoint; a Jaeger without a Collector in front has no store for two of them |

### Chosen Option: **A - traces only, named exporter options** (recommended, pending the maintainer)

### Rationale

- The named overload (`AddOtlpExporter(string? name, Action<OtlpExporterOptions>? configure)`) is the SDK's mechanism for several exporters of one signal; an unnamed one is the single shared configuration. The compose stack (`.github/observability/docker-compose.observability.yml`) uses Jaeger as the trace backend only; metrics go to Prometheus and logs to Loki through the Collector.
- C wins when the OTLP endpoint is an OpenTelemetry Collector that fans out to Jaeger, Prometheus and Loki (the shape of the repository's own stack): then `EnableOtlpExporter` of #1043 is the right switch and no Jaeger helper is needed; B wins only when a host wants one unnamed registration for every signal and accepts the aliasing.
- Class: Dilemma

</details>

<details>
<summary><strong>3. The issue's <code>SamplingRate</code> option</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No sampling option: sampling is the host's or #183's (`EncinaOpenTelemetryOptions.Sampling`)** | One owner of `SetSampler`; no silent last-call-wins between two Encina options | A Jaeger-only host that does not use #183 sets the sampler itself, or the `OTEL_TRACES_SAMPLER` environment variables |
| **B) `JaegerOptions.SamplingRate` applying a `ParentBasedSampler(TraceIdRatioBasedSampler(rate))`** (the issue) | A one-line sampling switch for a Jaeger-only host | A second Encina place that calls `SetSampler`, which conflicts with #183's `EncinaSampler`; the later registration wins without a message |

### Chosen Option: **A - no sampling option** (recommended, pending the maintainer)

### Rationale

- A keeps ADR-054 settled point 1 (the application owns the SDK configuration) and the single owner of #183.
- B wins for a host that wants Jaeger up in one call and will never use per-request-type sampling; it would need a documented rule that `Sampling` of #183 and `SamplingRate` are mutually exclusive, enforced at startup.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Defaults per protocol</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Endpoint` is nullable; null selects the default of the protocol: gRPC `http://localhost:4317`, HTTP/protobuf `http://localhost:4318/v1/traces`** | Correct for both protocols; the HTTP path the SDK requires when the endpoint is set in code is not left to the host | Two defaults to document and test |
| **B) One default `http://localhost:4317` (the issue)** | Matches the issue | Wrong for `HttpProtobuf`: the exporter posts to a path that does not exist |

### Chosen Option: **A - a default per protocol**

### Rationale

- The OTLP exporter appends `/v1/traces` only when the endpoint comes from the environment variable; an endpoint assigned in code is used as given, so the full path is needed for HTTP. Both protocols are exercised against the real container in Phase 2 (ports 4317 and 4318 of the all-in-one image).
- Class: Trivial (a default value, proven by a test rather than by a rule)
- Proof: the Phase 2 integration test posts spans over HTTP/protobuf to the real all-in-one container using the default endpoint; with option B the trace never arrives. The ports 4317 (gRPC) and 4318 (HTTP) are the OTLP defaults the compose stack already uses (`.github/observability/docker-compose.observability.yml`)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | `JaegerOptions`: `Endpoint` (`Uri?`, null = protocol default), `Protocol` (`OtlpExportProtocol`, default `Grpc`), `DeploymentEnvironment` (`string?`), `ServiceName` (`string?`) and `ConfigureExporter` (`Action<OtlpExporterOptions>?`, the escape hatch for headers and timeouts) | Trivial | The issue's options minus `SamplingRate`; headers go through the callback or `OTEL_EXPORTER_OTLP_HEADERS`, so no secret lives in the options class and no `[JsonIgnore]` or `ToString()` override is required |
| C2 | Enabling both `EnableOtlpExporter` (#1043) and `AddEncinaJaeger` exports traces twice, to two endpoints that may be the same Jaeger; it is documented, and not detected, because `WithEncina(builder, options)` receives its options object directly and not through DI (`ServiceCollectionExtensions.cs:166`) | Trivial | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:166` |
| C3 | `DeploymentEnvironment` sets the resource attribute `deployment.environment.name`, an `internal const` of the options file (a resource attribute, not a messaging or database key of the constants class of ADR-054 D3) | Trivial | ADR-054 D3 (latest semantic conventions) |
| C4 | `ServiceName` is applied with `ConfigureResource(r => r.AddService(name))` only when set; null leaves the name chosen by `WithEncina()` or the host | Settled | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:166-217` (`WithEncina` sets the resource name; a later default would override it) |
| C5 | Validation at startup (`ValidateOnStart`): an `Endpoint` with a scheme other than `http` or `https`, a relative `Endpoint`, an `Endpoint` that carries user info (a credential in a URI would reach `ToString()` and diagnostics; credentials go through `ConfigureExporter` headers or `OTEL_EXPORTER_OTLP_HEADERS`), an undefined `Protocol`, a blank `ServiceName` or `DeploymentEnvironment`; messages name the option, never a value | Trivial | `AGENTS.md` section 3 (options never print credentials; messages and values do not reach logs) |
| C6 | No new `EncinaError` code and no ADR-062 `ErrorCategory`: a bad option is an `OptionsValidationException` at startup | Settled | ADR-062 (a category is chosen where an `EncinaError` is created; none is created) |
| C7 | The helper emits no span or metric, so no tenant tag (ADR-054 D4) and no tenant-qualified id (the reserved `default` tenant segment does not apply); no log message and no EventId (matrix row 3) | Settled | ADR-054 D4; `AGENTS.md` section 6 row 10 |
| C8 | The integration image is one constant of the fixture, taken from the vendor's current supported line when work starts (the compose stack pins `1.57`) | Trivial | `src/Encina.Testing.Testcontainers/ContainerDefaultImages.cs` header (each default is the vendor's supported line) |
| C9 | New public names: `JaegerOptions`, `AddEncinaJaeger` | Dilemma (recommended, pending the maintainer) | Public surface; the issue's names are kept, the alternative is `WithEncinaJaeger` on `OpenTelemetryBuilder` to match `WithEncina` |

---

## Implementation Phases

### Phase 1: Options, Validator and Registration (pull request J1)

> **Goal**: `AddEncinaJaeger` registers a validated, traces-only, named OTLP exporter with Jaeger defaults (Design Choices 1 to 4; if the maintainer picks B of Design Choice 1 the same files go to a new project).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.OpenTelemetry/Jaeger/JaegerOptions.cs`: `public sealed class JaegerOptions` with the five properties of register row C1, XML docs with an `<example>`, and an `internal static class JaegerDefaults` holding `GrpcEndpoint` (`http://localhost:4317`), `HttpEndpoint` (`http://localhost:4318/v1/traces`) and the exporter name `EncinaJaeger`.
2. **Create** `src/Encina.OpenTelemetry/Jaeger/JaegerOptionsValidator.cs`: `internal sealed class : IValidateOptions<JaegerOptions>` with the rules of register row C5.
3. **Create** `src/Encina.OpenTelemetry/Jaeger/JaegerServiceCollectionExtensions.cs`: `public static IServiceCollection AddEncinaJaeger(this IServiceCollection services, Action<JaegerOptions>? configure = null)`: `ArgumentNullException.ThrowIfNull(services)`; options with `ValidateOnStart`; `services.ConfigureOpenTelemetryTracerProvider((sp, builder) => builder.AddOtlpExporter(JaegerDefaults.ExporterName, otlp => ...))`: the `(sp, builder)` overload is the only form with a service provider (the `WithTracing(t => ...)` callback has none), so it reads the validated `JaegerOptions` from `sp`, sets `otlp.Endpoint`, `otlp.Protocol`, then invokes `ConfigureExporter`; the same registration also adds `ConfigureResource` for the environment and, when set, the service name. It registers no hosted service and no singleton that holds a scoped service.
4. **Edit** `src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.OpenTelemetry.json` through `mechanical-fixer` (the lines come from the Research table).
5. **Create** `tests/Encina.UnitTests/OpenTelemetry/Jaeger/JaegerOptionsTests.cs`: defaults; one test per validator rule; the default endpoint per protocol (gRPC and HTTP) resolved by the registration.
6. **Create** `tests/Encina.UnitTests/OpenTelemetry/Jaeger/JaegerRegistrationTests.cs`: a provider built with `ValidateOnBuild` and `ValidateScopes` resolves the `TracerProvider`; the named exporter options carry the configured endpoint and protocol while the unnamed `OtlpExporterOptions` stay at their defaults (both `AddEncinaJaeger` and `EnableOtlpExporter` of #1043 on one builder do not alias); `ConfigureExporter` runs after the defaults; the resource carries `deployment.environment.name` when set and the host's `service.name` when `ServiceName` is null.
7. **Create** `tests/Encina.GuardTests/Infrastructure/OpenTelemetry/JaegerGuardTests.cs`: `AddEncinaJaeger` with a null `services` throws `ArgumentNullException`; a null `configure` is valid and registers the defaults.
8. Add `changelog.d/613-jaeger-trace-exporter.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no compatibility layers). Issue #613, plan docs/plans/jaeger-exporter-implementation-plan-613.md,
phase 1. Jaeger ingests OTLP natively, so the helper is a traces-only OTLP exporter with Jaeger defaults inside Encina.OpenTelemetry.
Prerequisite: issue #1043 merged (it makes OpenTelemetry.Exporter.OpenTelemetryProtocol a normal dependency instead of PrivateAssets="all").

TASK:
1. Create JaegerOptions, JaegerOptionsValidator and AddEncinaJaeger under src/Encina.OpenTelemetry/Jaeger/.
2. Register the exporter with the NAMED overload, only through ConfigureOpenTelemetryTracerProvider((sp, builder) =>
   builder.AddOtlpExporter("EncinaJaeger", ...)) (the form that has a service provider), traces only; the default endpoint depends on
   the protocol (gRPC http://localhost:4317, HTTP/protobuf http://localhost:4318/v1/traces).
3. No SamplingRate, no default ServiceName; apply ServiceName and the environment attribute only when set.
4. Add the unit and guard tests listed; PublicAPI, the manifest and the changelog fragment go through mechanical-fixer.

KEY RULES:
- Do not touch sampling (#183 owns it) or WithEncina().
- Validation messages name the option, never a value; no EncinaError.Message anywhere.
- Edit source only with Edit/Write; no [Obsolete]; tests use Shouldly through Encina.Testing.Shouldly and execute real code.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, EncinaOpenTelemetryOptions.cs
- docs/plans/otlp-exporter-implementation-plan-1043.md
- tests/Encina.GuardTests/Infrastructure/OpenTelemetry/ServiceCollectionExtensionsGuardTests.cs
```

</details>

---

### Phase 2: Integration Against a Real Jaeger (same pull request J1)

> **Goal**: Prove spans reach Jaeger over both protocols with the real container, the acceptance item of the issue.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.TestInfrastructure/Fixtures/JaegerFixture.cs`: `JaegerFixture : IAsyncLifetime` built with the generic `ContainerBuilder` (the `Testcontainers` package is already referenced by `Encina.TestInfrastructure`) from the all-in-one image, exposing ports 4317, 4318 and 16686 and waiting on the UI port; only the fixture class lives in `Encina.TestInfrastructure`, following `NatsFixture.cs`. xUnit collection definitions are per assembly (`AzuriteFixture.cs:25`, `LocalStackFixture.cs:22`; `NATSJetStreamDeduplicationIntegrationTests.cs:13-15` redeclares the collection over `NatsFixture`), so the `[CollectionDefinition("Jaeger")] JaegerCollection : ICollectionFixture<JaegerFixture>` class goes inside the integration test file of task 2, in `Encina.IntegrationTests` (no extra file).
2. **Create** `tests/Encina.IntegrationTests/Observability/OpenTelemetry/JaegerExporterIntegrationTests.cs` (`[Collection("Jaeger")]`, `[Trait("Category", "Integration")]`, `[Trait("Component", "OpenTelemetry")]`): for each protocol a host with `AddEncinaJaeger` pointing at the mapped port emits a span from an `ActivitySource`, flushes the provider and polls the Jaeger query API (`/api/traces?service=<name>`) until the trace appears (bounded by a deadline, no `Thread.Sleep`); an unreachable endpoint starts, emits and shuts down without throwing.
3. **Edit** `src/Encina.OpenTelemetry/README.md` (through `docs-writer`): the Jaeger helper, its defaults per protocol, the rule not to combine it with `EnableOtlpExporter` for traces and the pointer to #183 for sampling.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #613 plan docs/plans/jaeger-exporter-implementation-plan-613.md, phase 2. Phase 1 added AddEncinaJaeger. The repository has no
Jaeger test fixture; fixtures live in tests/Encina.TestInfrastructure/Fixtures (see NatsFixture.cs).

TASK:
1. Add JaegerFixture in Encina.TestInfrastructure (generic Testcontainers ContainerBuilder, one image constant); the [CollectionDefinition]
   JaegerCollection goes in the Encina.IntegrationTests test file, because xUnit collection definitions are per assembly.
2. Add the integration tests for gRPC and HTTP/protobuf and for an unreachable endpoint.
3. README section through docs-writer.

KEY RULES:
- Poll with a deadline against the Jaeger query API; never Thread.Sleep.
- The collection is shared; never create a per-class container (AGENTS.md section 9).
- The image tag is one constant; read the vendor's current supported line when you start.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/NatsFixture.cs
- tests/Encina.IntegrationTests/Observability/OpenTelemetry/ConsoleExporterIntegrationTests.cs
- .github/observability/docker-compose.observability.yml
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| OpenTelemetry OTLP exporter specification | Default ports 4317 (gRPC) and 4318 (HTTP); the signal path `/v1/traces` is appended only to the general endpoint variable | Design Choice 4 |
| OpenTelemetry .NET SDK | Named exporter options per signal; `ConfigureOpenTelemetryTracerProvider` with the service provider | Design Choice 2 and Phase 1 |
| OpenTelemetry semantic conventions | `deployment.environment.name` resource attribute | Register row C3 |
| Jaeger | Native OTLP ingestion for traces; the issue's Alternative 1 records the deprecation of the legacy client | Why the exporter is the OTLP one |
| ADR-054 | Settled points 1, 3; D3 conventions; D4 tenant tag | Ownership and the no-telemetry statement |
| ADR-062 | Error category where an `EncinaError` is created | Register row C6 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `WithEncina`, `EncinaOpenTelemetryOptions` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`, `EncinaOpenTelemetryOptions.cs` | Resource and source subscription the helper must not duplicate |
| OTLP package reference | `src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj:24` | Becomes public with #1043 |
| Observability stack | `.github/observability/docker-compose.observability.yml`, `otel-collector-config.yaml` | Shows Jaeger as a trace backend and its OTLP ports |
| Fixture pattern | `tests/Encina.TestInfrastructure/Fixtures/NatsFixture.cs` | `IAsyncLifetime` fixture and `ICollectionFixture` collection |
| Observability integration tests | `tests/Encina.IntegrationTests/Observability/OpenTelemetry/` | Traits and structure |
| `ContainerDefaultImages` | `src/Encina.Testing.Testcontainers/ContainerDefaultImages.cs` | The supported-line image rule |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | The helper has no loop and no I/O of its own: export failures are reported by the SDK self-diagnostics. No range is claimed and nothing is taken from the `10000+` zone |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI, the changelog fragment and the README; the pull request builds and passes alone.

| PR | Content | New src | Changed src / config | New tests | Test infrastructure | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|----------------------|-----------|---------------------|----------|-------|-------|--------------|
| J1 | Helper, validator, fixture and integration test | 3 (options, validator, extensions) | 1 (`PublicAPI.Unshipped.txt`) | 4 (two unit, one guard, one integration) | 1 (`JaegerFixture.cs`) | 1 (edit of `Encina.OpenTelemetry.json`) | 2 (README, changelog) | 12 | ~550 |
| **Total** | | **3** | **1** | **4** | **1** | **1** | **2** | **12** | **~550** |

The columns sum to the total (3 + 1 + 4 + 1 + 1 + 2 = 12). The pull request of this plan itself is one file. Under Design Choice 1 option B the pull request splits into the package skeleton (project, two `PublicAPI` files, manifest, solution entry, three test-project references) and the helper with its tests.

Per-file coverage targets (per flag, one sentence each), written to `.github/coverage-manifest/Encina.OpenTelemetry.json`; the changed methods reach full line coverage (CRAP gate):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Jaeger/JaegerOptions.cs` | 90 | 0 | 0 | 0 | 0 | Defaults and the defaults-per-protocol constants are read by the registration tests; the class has no parameters to guard and no invariant worth a property |
| `Jaeger/JaegerOptionsValidator.cs` | 95 | 70 | 0 | 0 | 0 | One test per rule drives every branch; guard covers the null options argument |
| `Jaeger/JaegerServiceCollectionExtensions.cs` | 90 | 85 | 0 | 0 | 70 | The registration test builds a real provider and reads the named exporter options; guard covers the null services and the null configure; the real-container path (both protocols against the Jaeger all-in-one image) is what the integration target of 70 measures |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #613 - Jaeger trace export - in one pull request (J1) with two phases.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always; edit source only with the
  Edit/Write tools; PublicAPI, manifests and changelog fragments through mechanical-fixer; README through docs-writer.
- Jaeger ingests OTLP natively; the helper is a traces-only OTLP exporter registered under the name EncinaJaeger. Issue #1043 makes
  the OTLP package a normal dependency first. Sampling is #183's; the service name is WithEncina()'s unless the host sets one here.

IMPLEMENTATION OVERVIEW:
1. Phase 1: JaegerOptions, JaegerOptionsValidator, AddEncinaJaeger, unit and guard tests.
2. Phase 2: JaegerFixture and the integration tests for gRPC and HTTP/protobuf against the all-in-one container.

KEY PATTERNS:
- Named exporter options; default endpoint per protocol; ConfigureExporter as the escape hatch for headers.
- Real TracerProvider in tests; the real container for the integration flag; poll with a deadline, never Thread.Sleep.
- Guard tests for every public method with parameters; the nullable configure is valid.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- tests/Encina.TestInfrastructure/Fixtures/NatsFixture.cs
- docs/plans/jaeger-exporter-implementation-plan-613.md, docs/plans/otlp-exporter-implementation-plan-1043.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Exporters do not cache telemetry. |
| 2 | OpenTelemetry | ✅ Phases 1-2 | It adds a traces-only OTLP exporter for Jaeger, with no own `ActivitySource` or `Meter`, so no tenant tag. |
| 3 | Structured Logging | ❌ N/A | Export failures use the SDK self-diagnostics, and the helper has no loop, no I/O and no EventId. |
| 4 | Health Checks | ⏭️ Follow-up issue file | `artifacts/issues/exporter-endpoint-health-checks.md` reuses the probe of #1049 for the Jaeger endpoint. |
| 5 | Validation | ✅ Phase 1 | The options validator for endpoint, protocol and names runs at startup. |
| 6 | Resilience | ❌ N/A | The SDK owns the export pipeline, with the same documented behavior as the plan of #1043. |
| 7 | Distributed Locks | ❌ N/A | There is no shared state. |
| 8 | Transactions | ❌ N/A | The export is fire-and-forget. |
| 9 | Idempotency | ❌ N/A | Span export is append-only. |
| 10 | Multi-Tenancy | ❌ N/A | The helper emits no telemetry; the tenant tag is added by the span-creating packages (ADR-054 D4). |
| 11 | Module Isolation | ❌ N/A | Observability is cross-module by design. |
| 12 | Audit Trail | ❌ N/A | An infrastructure package, not an auditable operation. |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #1043 OTLP exporter option (drops `PrivateAssets="all"` on the OTLP package) | Open, plan on `main` | The helper's public signature exposes `OtlpExporterOptions`, so consumers need the package transitively |
| ADR-054 accepted | On `main` | Settled points 1 and 3, D3, D4 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Trace sampling | #183 | Owns `SetSampler`; the helper has no sampling option |
| Endpoint reachability check | #1049 and `artifacts/issues/exporter-endpoint-health-checks.md` | The probe for the Jaeger endpoint |
| Observability guide | #906 | Documents the backend choices and the Collector route |
| Sibling exporters | #614 (Zipkin), #180 (Prometheus) | Same family; answer the placement question together |

---

## Next Steps

1. Link this plan from #613 and ask the maintainer to answer Design Choices 1 to 3 and register row C9 when work starts; Design Choice 1 is answered together with #614 and #180.
2. Implement J1 after #1043 merges. The orchestrator opens the follow-up issue file `artifacts/issues/exporter-endpoint-health-checks.md` (shared with #614 and #182).
