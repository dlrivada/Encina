# Implementation Plan: `Encina.OpenTelemetry.Zipkin` — Zipkin Distributed Tracing Integration

> **Issue**: [#614](https://github.com/dlrivada/Encina/issues/614)
> **Type**: Feature (observability, new package)
> **Related**: #613 (Jaeger, sibling plan of this family), #1043 (OTLP exporter option), #1049 (endpoint health check), #183 (trace sampling), #180 (Prometheus, sibling plan), #132 (context propagation API), ADR-054 (settled points 1 and 3, D3, D4), #906 (observability guide), ADR-062
> **Complexity**: Low (about 100 lines of production code; the weight is the B3 decision that the ADR already answers in part)
> **Estimated Scope**: ~100 lines of production code + ~420 lines of tests + 1 README, in two code pull requests of 14 and 6 files
> **Milestone**: v0.14.0 Hardening

---

## Summary

### What exists today (read on 2026-11-23, main `237a83364`)

- There is no Zipkin code and no reference to `OpenTelemetry.Exporter.Zipkin` in any project or in `Directory.Packages.props`. The version to pin is `1.17.0`, the train of the pinned `OpenTelemetry` 1.17.0: the package exists on nuget.org, has a `net10.0` asset, and depends on `OpenTelemetry` `1.17.0` exactly (read from its nuspec on 2026-11-23).
- The exporter's surface (from the package documentation file): `TracerProviderBuilder.AddZipkinExporter(Action<ZipkinExporterOptions>)` and the named overload `AddZipkinExporter(string name, Action<ZipkinExporterOptions>)`; the options `Endpoint`, `UseShortTraceIds`, `MaxPayloadSizeInBytes`, `ExportProcessorType`, `BatchExportProcessorOptions` and `HttpClientFactory`.
- The observability stack of the repository has no Zipkin service: `.github/observability/docker-compose.observability.yml` runs the OTel Collector, Prometheus, Jaeger, Loki and Grafana, and `otel-collector-config.yaml` exports traces only to Jaeger.
- `OpenTelemetry.Extensions.Propagators` `1.17.0` is pinned in `Directory.Packages.props` and referenced by no project; it holds `B3Propagator` and `JaegerPropagator`.
- Encina propagates nothing yet; the plan of #132 designs `IContextPropagator` for `traceparent` and `tracestate` only, as ADR-054 settled point 3 requires: "W3C Trace Context is the wire format. `traceparent` and `tracestate` are the only propagation fields."
- As in #613: `WithEncina()` owns the resource service name (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:158-217`) and the plan of #183 owns sampling.

### What the issue proposes and what changed since

The issue (2026-era text, `status:proposal`) proposes a package with `AddEncinaZipkin(Action<ZipkinOptions>)`, options `Endpoint` (default `http://localhost:9411/api/v2/spans`) and `ServiceName` (default `"encina-service"`), and "B3 context propagation" as an acceptance item. Three points change: the default `ServiceName` would override the name `WithEncina()` sets, the endpoint default is already the exporter's own and is better left to the SDK (which also reads `OTEL_EXPORTER_ZIPKIN_ENDPOINT`), and B3 contradicts ADR-054 settled point 3 as written (Design Choice 2). Alternative 2 of the issue says a generic OTLP exporter reaches Zipkin through an OTLP endpoint; this plan does not rely on that, because the Zipkin v2 span endpoint is what the exporter and the test use, and Phase 2 verifies with the fixture image whether OTLP is accepted and records the result in the README.

### What this plan delivers

- A new package `Encina.OpenTelemetry.Zipkin` with `ZipkinOptions`, a startup validator and `AddEncinaZipkin(this IServiceCollection, Action<ZipkinOptions>?)`: a traces-only Zipkin exporter registered under its own name (Phase 1).
- A real-Zipkin integration test through a Testcontainers fixture, a README, and the changelog fragment (Phase 2).
- Shared pieces and owner: #1043 owns the OTLP wiring; #183 owns sampling; #132 owns Encina's propagation API (W3C only); #1049 and the follow-up issue file `artifacts/issues/exporter-endpoint-health-checks.md` own endpoint reachability; #906 owns the guide; this plan owns the Zipkin package.
- Affected packages: `Encina.OpenTelemetry.Zipkin` (new), `Encina.TestInfrastructure` (one fixture). `Encina.OpenTelemetry` changes nothing. Provider category: observability (OpenTelemetry exporters); no database or transport provider.
- Not delivered, with the reason: B3 propagation (Design Choice 2); a sampling option and a default service name (as in #613); a Zipkin health check (the follow-up issue file); a Zipkin service in the repository's compose stack (the test uses its own container).

---

## Design Choices

<details>
<summary><strong>1. Where the Zipkin support lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new package `Encina.OpenTelemetry.Zipkin`** (the issue) | `OpenTelemetry.Exporter.Zipkin` and its transitive packages reach only the hosts that use Zipkin (pay-for-what-you-use, `AGENTS.md` section 1); independent versioning | One more package: project, `PublicAPI` pair, README, manifest, three test-project references |
| **B) A `Zipkin` folder in `Encina.OpenTelemetry`** | No new package | Every `Encina.OpenTelemetry` consumer restores the Zipkin exporter and its dependencies |
| **C) No code: document `WithTracing(t => t.AddZipkinExporter(...))`** | No API | The named-options, validation and service-name details are what hosts get wrong; the issue's acceptance list is unmet |

### Chosen Option: **A - a new package** (recommended, pending the maintainer)

### Rationale

- The rule is the dependency: Jaeger (#613) needs no new dependency and stays in `Encina.OpenTelemetry`; Zipkin needs one and earns a package. The two answers are given together.
- B wins when the maintainer wants one observability package whatever it restores; C wins when three lines of SDK code are not worth an Encina API. If B is chosen the file count of the pull request falls by the project file, two `PublicAPI` files, a manifest, the solution entry and three test-project references; the ADR number, if one is wanted, is assigned when the ADR pull request opens.
- Class: Dilemma

</details>

<details>
<summary><strong>2. B3 propagation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No B3 in Encina: W3C only; the guide gives the host recipe with `OpenTelemetry.Extensions.Propagators` (`CompositeTextMapPropagator` of W3C and `B3Propagator`, installed by the host with `Sdk.SetDefaultTextMapPropagator`)** | Follows ADR-054 settled point 3 as written; one wire format to document and test across 10 transports; the host owns process-wide SDK state (settled point 1) | The acceptance item "B3 context propagation" is met by documentation, not code; an estate of B3-only services needs the recipe |
| **B) An opt-in helper `ZipkinPropagation.CreateCompositePropagator()` returning the W3C plus B3 propagator, which the host installs; Encina never sets the global propagator and its own transports stay W3C** | The issue's item gets code and a tested example; the host controls the global | A second propagation format in the product surface and its tests; ADR-054 settled point 3 says `traceparent` and `tracestate` are the only propagation fields, so the helper needs the maintainer to read that point as limited to Encina's own wire |
| **C) Amend ADR-054 settled point 3 so Encina's transports also carry `b3`** | Full B3 interoperability | Every transport (10 plus 6 planned) and the persisted envelope gain a second header set; an architecture decision beyond this plan |

### Chosen Option: **A - W3C only, B3 by host recipe** (recommended, pending the maintainer)

### Rationale

- A keeps the one decision the ADR already took and costs a documentation paragraph.
- B wins for a mixed estate where the Encina application sits between B3-only services at an HTTP edge and the maintainer reads settled point 3 as covering only Encina's own wire; C wins if the maintainer decides B3 must be a first-class Encina format (a new ADR, whose number is assigned when its pull request opens).
- Class: Dilemma

</details>

<details>
<summary><strong>3. Exporter registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Traces only, through the named overload `AddZipkinExporter("EncinaZipkin", configure)`; `Endpoint` nullable (null keeps the SDK default and `OTEL_EXPORTER_ZIPKIN_ENDPOINT`); the rest of `ZipkinExporterOptions` through a `ConfigureExporter` callback** | The named options never alias another Zipkin registration; the environment variable keeps working; the options class stays small | The host sets `UseShortTraceIds` or the payload cap through the callback |
| **B) The unnamed `AddZipkinExporter(configure)` with a fixed default endpoint, as in the issue** | Matches the issue | An unnamed second registration shares options with any other Zipkin exporter; a code default hides the environment variable |

### Chosen Option: **A - named options, nullable endpoint, callback for the rest** (recommended, pending the maintainer)

### Rationale

- The named overload exists in the pinned package (`AddZipkinExporter(TracerProviderBuilder, string, Action<ZipkinExporterOptions>)`, read from the package documentation file); Zipkin ingests traces only, so no other signal is wired. Keeping the SDK default honors ADR-054 settled point 1 (the application owns SDK configuration).
- B wins for a host that binds the default `ZipkinExporterOptions` from `IConfiguration` (`services.Configure<ZipkinExporterOptions>(section)` reaches only the unnamed instance) and wants one Zipkin exporter with no Encina options at all.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Integration test target</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A Testcontainers fixture with the `openzipkin/zipkin` image, queried through the v2 API (`/api/v2/traces?serviceName=`)** | Real Zipkin, no dependency on the compose stack, shared collection fixture | A new fixture file in `Encina.TestInfrastructure` |
| **B) Add Zipkin to `.github/observability/docker-compose.observability.yml` and test against it** | One stack for manual and automated use | The compose stack is for local use and is not started by CI; the tests would depend on it being up |

### Chosen Option: **A - a Testcontainers fixture** (recommended, pending the maintainer)

### Rationale

- `AGENTS.md` section 9 asks for real infrastructure through Docker and Testcontainers with shared collection fixtures, and the run of CI does not start the compose stack, so a test that needs it is skipped or red there.
- B wins when the maintainer wants developers to look at the Zipkin UI of the same stack that already serves Jaeger, Prometheus and Grafana during manual checks.
- Class: Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | `ZipkinOptions`: `Endpoint` (`Uri?`), `ServiceName` (`string?`), `DeploymentEnvironment` (`string?`) and `ConfigureExporter` (`Action<ZipkinExporterOptions>?`); no sampling option, no default service name | Trivial | The issue's options minus the two decisions of #613 (sampling is #183's; `WithEncina()` owns the name, `ServiceCollectionExtensions.cs:158-217`) |
| C2 | `ServiceName` and the environment attribute (`deployment.environment.name`) are applied through `ConfigureResource` only when set | Trivial | Same as #613 register rows C3 and C4; ADR-054 D3 |
| C3 | Validation at startup (`ValidateOnStart`): an `Endpoint` that is relative, not `http` or `https`, or carries user info (credentials in a URI would reach `ToString()` and diagnostics; credentials go in the callback through `HttpClientFactory`), and a blank name; messages name the option, never a value | Trivial | `AGENTS.md` section 3 (options never print credentials) |
| C4 | The options class holds no secret (the user-info rule keeps a credential out of `Endpoint`), so no `[JsonIgnore]` and no `ToString()` override is required | Trivial | `AGENTS.md` section 3 (the rule applies to an options class with a password, token or key) |
| C5 | Enabling `EnableOtlpExporter` of #1043 and `AddEncinaZipkin` exports traces twice, to two backends; documented, not detected (`WithEncina(builder, options)` receives its options directly, not through DI) | Trivial | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:158` |
| C6 | No new `EncinaError` code and no ADR-062 `ErrorCategory`: a bad option is an `OptionsValidationException` at startup | Settled | ADR-062 (a category is chosen where an `EncinaError` is created; none is created) |
| C7 | The package emits no span or metric, so no tenant tag (ADR-054 D4) and no tenant-qualified id (the reserved `default` tenant segment does not apply); no log message and no EventId (matrix row 3) | Settled | ADR-054 D4; `AGENTS.md` section 6 row 10 |
| C8 | The fixture image is one constant, taken from the vendor's stable line when work starts | Trivial | `src/Encina.Testing.Testcontainers/ContainerDefaultImages.cs` header |
| C9 | New public names: `ZipkinOptions`, `AddEncinaZipkin` | Dilemma (recommended, pending the maintainer) | Public surface; the issue's names are kept, the alternative is `WithEncinaZipkin` on `OpenTelemetryBuilder` |

---

## Implementation Phases

### Phase 1: The Package, Options, Validator and Registration (pull request Z1)

> **Goal**: `AddEncinaZipkin` registers a validated, traces-only, named Zipkin exporter (Design Choices 1 and 3; if the maintainer picks B of Design Choice 1 the same source files go to a folder of `Encina.OpenTelemetry` and the skeleton files disappear).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.OpenTelemetry.Zipkin/Encina.OpenTelemetry.Zipkin.csproj` (net10.0, nullable, `ManagePackageVersionsCentrally`, `PackageReference` to `OpenTelemetry.Exporter.Zipkin`, `Microsoft.Extensions.Options` and the private `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `ProjectReference` to `Encina.OpenTelemetry`, `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests` and `Encina.IntegrationTests`, the two `PublicAPI` files as `AdditionalFiles`), following `src/Encina.Hangfire/Encina.Hangfire.csproj`.
2. **Create** `src/Encina.OpenTelemetry.Zipkin/ZipkinOptions.cs`, `ZipkinOptionsValidator.cs` (`internal sealed class : IValidateOptions<ZipkinOptions>`, register row C3) and `ServiceCollectionExtensions.cs`: `public static IServiceCollection AddEncinaZipkin(this IServiceCollection services, Action<ZipkinOptions>? configure = null)`: `ArgumentNullException.ThrowIfNull(services)`; options with `ValidateOnStart`; `services.AddOpenTelemetry().WithTracing(t => t.AddZipkinExporter("EncinaZipkin", z => ...))` with the callback reading the validated options through `services.ConfigureOpenTelemetryTracerProvider((sp, builder) => ...)`, setting `Endpoint` only when not null, then invoking `ConfigureExporter`; resource changes only when `ServiceName` or `DeploymentEnvironment` is set. No hosted service and no scoped service captured by a singleton.
3. **Edit** `Directory.Packages.props` (add `OpenTelemetry.Exporter.Zipkin` `1.17.0` next to the other OpenTelemetry pins), `Encina.slnx`, `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` (reference the project) through `mechanical-fixer`; **create** the two `PublicAPI` files and `.github/coverage-manifest/Encina.OpenTelemetry.Zipkin.json` through `mechanical-fixer` (the lines come from the Research table).
4. **Create** `tests/Encina.UnitTests/OpenTelemetry/Zipkin/ZipkinOptionsTests.cs`: defaults; one test per validator rule (relative endpoint, wrong scheme, user info, blank names).
5. **Create** `tests/Encina.UnitTests/OpenTelemetry/Zipkin/ZipkinRegistrationTests.cs`: a provider built with `ValidateOnBuild` and `ValidateScopes` resolves the `TracerProvider`; the named exporter options carry the configured `Endpoint` and keep the SDK default (and the environment variable) when null; `ConfigureExporter` runs; the resource carries the service name and environment only when set; with `EnableOtlpExporter` of #1043 on the same builder the two registrations stay separate.
6. **Create** `tests/Encina.GuardTests/OpenTelemetry/Zipkin/ZipkinGuardTests.cs`: a null `services` throws `ArgumentNullException`; a null `configure` is valid and registers the defaults.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no compatibility layers). Issue #614, plan docs/plans/zipkin-exporter-implementation-plan-614.md,
phase 1. A new package Encina.OpenTelemetry.Zipkin wraps OpenTelemetry.Exporter.Zipkin 1.17.0 (the train of the pinned OpenTelemetry
1.17.0). The Jaeger helper of #613 is the sibling: same shape, same decisions on sampling and service name.

TASK:
1. Create the project, ZipkinOptions, ZipkinOptionsValidator and AddEncinaZipkin as the plan lists.
2. Register the exporter with the NAMED overload AddZipkinExporter("EncinaZipkin", ...) on WithTracing only; Endpoint null keeps the SDK
   default and OTEL_EXPORTER_ZIPKIN_ENDPOINT.
3. No B3 code, no sampling option, no default ServiceName.
4. Add the pin, solution entry and test references, and the unit and guard tests listed; PublicAPI, the manifest and the pin go
   through mechanical-fixer.

KEY RULES:
- Reject an Endpoint with user info; validation messages name the option, never a value.
- Do not touch sampling (#183), WithEncina() or any propagator (ADR-054 settled point 3).
- Edit source only with Edit/Write; no [Obsolete]; tests use Shouldly through Encina.Testing.Shouldly and execute real code.

REFERENCE FILES:
- src/Encina.Hangfire/Encina.Hangfire.csproj, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- the plan of #613 (the Jaeger helper, same shape and decisions)
- tests/Encina.GuardTests/Infrastructure/OpenTelemetry/ServiceCollectionExtensionsGuardTests.cs
```

</details>

---

### Phase 2: Integration Against a Real Zipkin, README (pull request Z2)

> **Goal**: Prove spans reach Zipkin with the real container (Design Choice 4) and document the package and the B3 recipe (Design Choice 2, option A).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.TestInfrastructure/Fixtures/ZipkinFixture.cs`: `ZipkinFixture : IAsyncLifetime` built with the generic `ContainerBuilder`, exposing port 9411 and waiting on the health endpoint; and `ZipkinCollection : ICollectionFixture<ZipkinFixture>` named `Zipkin`, following `NatsFixture.cs`.
2. **Create** `tests/Encina.IntegrationTests/Observability/OpenTelemetry/ZipkinExporterIntegrationTests.cs` (`[Collection("Zipkin")]`, `[Trait("Category", "Integration")]`, `[Trait("Component", "OpenTelemetry")]`): a host with `AddEncinaZipkin` pointing at the mapped port emits a span, flushes and polls `/api/v2/traces?serviceName=<name>` until it appears (a deadline, no `Thread.Sleep`); an unreachable endpoint starts, emits and shuts down without throwing; a probe of whether the image accepts OTLP records its finding in a test comment and the README.
3. **Edit** `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` (reference the project) and `.github/coverage-manifest/Encina.OpenTelemetry.Zipkin.json` (the integration target) through `mechanical-fixer`.
4. **Create** `src/Encina.OpenTelemetry.Zipkin/README.md` (through `docs-writer`): install, the call, the defaults, the rule not to combine it with `EnableOtlpExporter` for traces, the B3 host recipe (Design Choice 2, option A) and the OTLP finding; and `changelog.d/614-zipkin-trace-exporter.added.md` (through `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #614 plan docs/plans/zipkin-exporter-implementation-plan-614.md, phase 2. Phase 1 merged: the package and AddEncinaZipkin exist.
There is no Zipkin test fixture; fixtures live in tests/Encina.TestInfrastructure/Fixtures (see NatsFixture.cs).

TASK:
1. Add ZipkinFixture and ZipkinCollection (generic Testcontainers ContainerBuilder, one image constant).
2. Add the integration tests: a span reaches Zipkin; an unreachable endpoint does not break the host.
3. Reference the package from Encina.IntegrationTests; README and changelog fragment through the specialists.

KEY RULES:
- Poll the Zipkin v2 API with a deadline; never Thread.Sleep.
- The collection is shared; never create a per-class container (AGENTS.md section 9).
- Document the B3 recipe with OpenTelemetry.Extensions.Propagators; write no B3 code in Encina.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/NatsFixture.cs
- tests/Encina.IntegrationTests/Observability/OpenTelemetry/ConsoleExporterIntegrationTests.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md (settled point 3)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Zipkin v2 span API and B3 propagation | `POST /api/v2/spans`; `b3` single header or `X-B3-*` multi-header | The wire the exporter uses; why B3 is Design Choice 2 |
| W3C Trace Context | `traceparent`, `tracestate` | ADR-054 settled point 3 |
| OpenTelemetry .NET SDK | Named exporter options; `ConfigureOpenTelemetryTracerProvider` | Design Choice 3 |
| OpenTelemetry semantic conventions | `deployment.environment.name` resource attribute | Register row C2 |
| ADR-054 | Settled points 1 and 3; D3, D4 | Ownership, wire format, no telemetry of its own |
| ADR-062 | Error category where an `EncinaError` is created | Register row C6 |
| `AGENTS.md` sections 3 and 9 | Options never print credentials; Testcontainers fixtures | Register row C3, Design Choice 4 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `WithEncina`, `EncinaOpenTelemetryOptions` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`, `EncinaOpenTelemetryOptions.cs` | Resource and source subscription the package must not duplicate |
| Satellite project shape | `src/Encina.Hangfire/Encina.Hangfire.csproj` | Template for the csproj |
| Pinned propagators package | `Directory.Packages.props` (`OpenTelemetry.Extensions.Propagators` 1.17.0) | The B3 host recipe, no code |
| Fixture pattern | `tests/Encina.TestInfrastructure/Fixtures/NatsFixture.cs` | `IAsyncLifetime` fixture and `ICollectionFixture` collection |
| Observability integration tests | `tests/Encina.IntegrationTests/Observability/OpenTelemetry/` | Traits and structure |
| Jaeger helper (sibling plan) | #613 | Same shape and the same decisions |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | The package has no loop and no I/O of its own: export failures are reported by the SDK self-diagnostics. No range is claimed and nothing is taken from the `10000+` zone |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI, the project references and the changelog fragment; each pull request builds and passes alone.

| PR | Content | New src | Changed src / config | New tests | Test infrastructure | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|----------------------|-----------|---------------------|----------|-------|-------|--------------|
| Z1 | Package, options, validator, registration | 4 (csproj, options, validator, extensions) | 4 (`Directory.Packages.props`, `Encina.slnx`, `UnitTests.csproj`, `GuardTests.csproj`) | 3 | 0 | 1 (new manifest) | 2 (`PublicAPI` Shipped and Unshipped) | 14 | ~620 |
| Z2 | Fixture, integration test, README | 0 | 1 (`IntegrationTests.csproj`) | 1 | 1 (`ZipkinFixture.cs`) | 1 (edit) | 2 (README, changelog) | 6 | ~330 |
| **Total** | | **4** | **5** | **4** | **1** | **2** | **4** | **20** | **~950** |

The columns sum to the total (4 + 5 + 4 + 1 + 2 + 4 = 20; 14 + 6 = 20). The pull request of this plan itself is one file. Under Design Choice 1 option B the skeleton files (project, `PublicAPI` pair, manifest, solution entry, three project references) disappear and the work fits one pull request.

Per-file coverage targets (per flag, one sentence each), written to `.github/coverage-manifest/Encina.OpenTelemetry.Zipkin.json`; the changed methods reach full line coverage (CRAP gate):

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `ZipkinOptions.cs` | 90 | 0 | 0 | 0 | Defaults are read by the registration tests; the class has no parameters to guard and no invariant worth a property |
| `ZipkinOptionsValidator.cs` | 95 | 70 | 0 | 0 | One test per rule drives every branch; guard covers the null options argument |
| `ServiceCollectionExtensions.cs` | 90 | 85 | 0 | 0 | The registration test builds a real provider and reads the named exporter options; guard covers the null services and the null configure; the real-container path counts for the integration flag (target 70) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #614 - Zipkin trace export - in two pull requests (Z1 and Z2).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always; edit source only with the
  Edit/Write tools; PublicAPI, manifests, pins and changelog fragments through mechanical-fixer; README through docs-writer.
- ADR-054 settled point 3: W3C Trace Context is the only propagation format of Encina, so B3 is a host recipe, not Encina code.
  Sampling is #183's; the service name is WithEncina()'s unless the host sets one here.

IMPLEMENTATION OVERVIEW:
1. Z1: the package Encina.OpenTelemetry.Zipkin: ZipkinOptions, ZipkinOptionsValidator, AddEncinaZipkin (named exporter options), the
   OpenTelemetry.Exporter.Zipkin 1.17.0 pin, solution and test references, unit and guard tests.
2. Z2: ZipkinFixture and the integration tests against the real container, the README with the B3 recipe, the changelog fragment.

KEY PATTERNS:
- Named exporter options; nullable endpoint keeps the SDK default and the environment variable; user info in the endpoint is rejected.
- Real TracerProvider in tests; the real container for the integration flag; poll with a deadline, never Thread.Sleep.
- Guard tests for every public method with parameters; the nullable configure is valid.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, src/Encina.Hangfire/Encina.Hangfire.csproj
- tests/Encina.TestInfrastructure/Fixtures/NatsFixture.cs
- docs/plans/zipkin-exporter-implementation-plan-614.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Exporters do not cache telemetry. |
| 2 | OpenTelemetry | ✅ Phases 1-2 | It adds the Zipkin span exporter with no own `ActivitySource` or `Meter`, so no tenant tag; B3 propagation is Design Choice 2. |
| 3 | Structured Logging | ❌ N/A | Export failures use the SDK self-diagnostics, and the package has no loop, no I/O and no EventId. |
| 4 | Health Checks | ⏭️ Follow-up issue file | `artifacts/issues/exporter-endpoint-health-checks.md` reuses the probe of #1049 for the Zipkin endpoint. |
| 5 | Validation | ✅ Phase 1 | The options validator for the endpoint and names runs at startup. |
| 6 | Resilience | ❌ N/A | The SDK owns the export pipeline, with the same documented behavior as the plan of #1043. |
| 7 | Distributed Locks | ❌ N/A | There is no shared state. |
| 8 | Transactions | ❌ N/A | The export is fire-and-forget. |
| 9 | Idempotency | ❌ N/A | Span export is append-only. |
| 10 | Multi-Tenancy | ❌ N/A | The package emits no telemetry; the tenant tag is added by the span-creating packages (ADR-054 D4). |
| 11 | Module Isolation | ❌ N/A | Observability is cross-module by design. |
| 12 | Audit Trail | ❌ N/A | An infrastructure package, not an auditable operation. |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 accepted | On `main` | Settled points 1 and 3, D3, D4 |
| `OpenTelemetry.Exporter.Zipkin` 1.17.0 | On nuget.org, `net10.0` asset | Depends on `OpenTelemetry` 1.17.0 exactly, the pinned version |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| OTLP exporter option | #1043 | The record of the all-signals route; this package never touches it |
| Trace sampling | #183 | Owns `SetSampler`; the package has no sampling option |
| Endpoint reachability check | #1049 and `artifacts/issues/exporter-endpoint-health-checks.md` | The probe for the Zipkin endpoint |
| Context propagation API | #132 | W3C only, which is why B3 is a host recipe |
| Observability guide | #906 | Documents the backends and the recipe |
| Sibling exporters | #613 (Jaeger), #180 (Prometheus) | Same family; answer the placement question together |

---

## Next Steps

1. Link this plan from #614 and ask the maintainer to answer Design Choices 1 to 4 and register row C9 when work starts; Design Choice 1 is answered together with #613 and #180, and Design Choice 2 is the maintainer's reading of ADR-054 settled point 3.
2. Implement Z1, then Z2. The orchestrator opens the follow-up issue file `artifacts/issues/exporter-endpoint-health-checks.md` (shared with #613 and #182).
