# Implementation Plan: `Encina.OpenTelemetry.Prometheus` — Self-Hosted Scraping Endpoint

> **Issue**: [#180](https://github.com/dlrivada/Encina/issues/180)
> **Type**: Feature (observability, new package)
> **Related**: ADR-054 (settled point 1 exporters stay out of `WithEncina()`, settled point 4 instrument names, D4 tenant tag, D6), #1043 (OTLP exporter option, sibling plan), #613 and #614 (Jaeger and Zipkin, sibling plans of this family), #187 (dashboards, owns the series names it reads), #174 (request and messaging instruments), #2678 and #2677 (instrument and meter renames), #2676 (tenant attribute helper), #906 (observability guide), ADR-062
> **Complexity**: Medium (the exporter exists; the work is a safe endpoint, honest metric names and a prerelease dependency)
> **Estimated Scope**: ~190 lines of production code + ~560 lines of tests + 1 README, in two code pull requests of 14 and 13 files
> **Milestone**: v0.14.0 Hardening

---

## Summary

### What exists today (read on 2026-11-23, main `237a83364`)

- There is no `Encina.OpenTelemetry.Prometheus` project. `Directory.Packages.props` pins `OpenTelemetry.Exporter.Prometheus.AspNetCore` at `1.13.0-beta.1`, but no project references it (a search of every `*.csproj` finds none).
- **That exporter has no stable release.** `api.nuget.org` lists only `-beta.1` versions up to `1.21.0-beta.1` on 2026-11-23. The `1.13.0-beta.1` package ships `net8.0` and `net9.0` assets, depends on `OpenTelemetry [1.13.0, 2.0.0)` and carries a `Microsoft.AspNetCore.App` framework reference. Its surface (from the package documentation file): `MeterProviderBuilder.AddPrometheusExporter(Action<PrometheusAspNetCoreOptions>)`, `IApplicationBuilder.UseOpenTelemetryPrometheusScrapingEndpoint(path)`, `IEndpointRouteBuilder.MapPrometheusScrapingEndpoint(path)` and the options `ScrapeEndpointPath`, `ScrapeResponseCacheDurationMilliseconds` and `DisableTotalNameSuffixForCounters`.
- `Encina.OpenTelemetry.WithEncina` already subscribes the meters and the runtime metrics: `metrics.AddMeter("Encina")` and `metrics.AddRuntimeInstrumentation()` (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:213-214`). A scraping package therefore adds a reader and an endpoint, not subscriptions.
- **The metric names of the issue do not exist.** The issue shows `encina_requests_total`, `encina_request_duration_seconds_bucket`, `encina_outbox_pending_count`, `encina_saga_completed_total` and `encina_cache_hit_ratio`. The code exports instruments such as `Encina.request.success`, `Encina.request.duration` (milliseconds) and `encina.outbox.pending_count`; some are declared and never emitted (see the plan of #174), `encina_cache_hit_ratio` is a derived value, not an instrument, and #2678 will rename the existing instruments to the ADR-054 rules. The Prometheus exporter turns instrument names into series names (dots become underscores, counters get `_total`, unit `s` becomes `_seconds`), as the plan of #187 records, so a series name is a function of instrument name, kind and unit.
- The repository already runs the Collector route: `.github/observability/otel-collector-config.yaml` has a Prometheus exporter on port 8889 and `.github/observability/prometheus.yml` scrapes `otel-collector:8889`.
- ADR-054 D4 puts the tenant id on metrics as a tag when multi-tenancy is enabled, so a scrape endpoint of an application with tenants exposes tenant identifiers.

### What the issue proposes and what changed since

The issue (2026-era text, `status:proposal`) proposes `AddEncinaPrometheus(config => { EnableDefaultMetrics; EnableRuntimeMetrics; MetricsPath })` and `UseEncinaPrometheus()`, a catalogue of `encina_*` series, custom meter helpers and Grafana templates. Three points do not survive today's facts: the two metric switches duplicate what `WithEncina()` already does (settled point 1 and 8 of ADR-054), the series catalogue is wrong, and an unauthenticated `/metrics` that carries tenant tags is a data exposure the issue does not mention.

### What this plan delivers

- A new package `Encina.OpenTelemetry.Prometheus` with `AddEncinaPrometheus(Action<EncinaPrometheusOptions>?)` (adds the Prometheus reader, the validated options and extra meter names) and `MapEncinaPrometheus()` (maps the scraping endpoint and applies the access decision) (Phases 1 and 2).
- A startup validator: the host must choose an authorization policy or `AllowAnonymous = true`; neither is a startup failure (Design Choice 3).
- Two log events (endpoint mapped, anonymous access) in a new `10000+` range (Phase 2).
- Shared pieces and owner: this plan owns the package and its options; #1043 owns the OTLP wiring in `Encina.OpenTelemetry`; #187 owns the series names the dashboards read and the Grafana assets; #174 and #2678 own the instruments; #2676 owns the tenant tag; #906 owns the observability guide.
- Affected packages: `Encina.OpenTelemetry.Prometheus` (new). `Encina.OpenTelemetry` changes nothing. Provider category: observability (OpenTelemetry exporters), no database or transport provider.
- Not delivered, with the reason: the `EnableDefaultMetrics` and `EnableRuntimeMetrics` switches (Design Choice 4); a series catalogue and Grafana dashboards (#187); a derived `hit_ratio` series (a PromQL expression of the hits and misses counters, #187); a reachability health check (a pull endpoint has nothing to reach, matrix row 4).

---

## Design Choices

<details>
<summary><strong>1. Where self-hosted Prometheus support lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new package `Encina.OpenTelemetry.Prometheus`** (the issue) | The `Microsoft.AspNetCore.App` framework reference and the prerelease exporter reach only the hosts that opt in; independent versioning while the exporter is beta; matches the satellite names of the issue family (#613, #614) | One more package: project, `PublicAPI` files, README, manifest, three test-project references |
| **B) No package: document the Collector route** (`.github/observability` already scrapes `otel-collector:8889`) | No code, no beta dependency, no endpoint inside the application, no tenant tag exposed by the app | Needs a Collector, which the issue's motivation says not every deployment has ("simple deployment: just scrape `/metrics`") |
| **C) Fold it into `Encina.OpenTelemetry`** | No new package | Every `Encina.OpenTelemetry` consumer gains an ASP.NET Core framework reference and a prerelease dependency, against pay-for-what-you-use (`AGENTS.md` section 1) |

### Chosen Option: **A - a new package** (recommended, pending the maintainer)

### Rationale

- A keeps the opt-in cost with the host that wants it, the reason ADR-054 settled point 1 keeps exporters out of `WithEncina()`.
- B wins when the deployment already runs an OpenTelemetry Collector (the repository's own stack does) and the maintainer prefers no in-process endpoint; C wins only if the maintainer accepts the framework reference for everyone. If B is chosen this plan reduces to documentation owned by #906 and #187.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What produces the Prometheus text and the status of the package while its dependency is beta</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wrap `OpenTelemetry.Exporter.Prometheus.AspNetCore`; ship the package with a prerelease version suffix while that dependency is beta** | The official exporter, maintained with the SDK; correct histogram and exemplar output; a few dozen lines of Encina code | The package cannot be stable while the dependency is prerelease (NuGet pack warning NU5104 once `VersionSuffix` is dropped); it carries `net8.0`/`net9.0` assets, usable from `net10.0` |
| **B) Build on `prometheus-net`** | Stable releases exist | A second metrics pipeline beside `System.Diagnostics.Metrics`; Encina's instruments would need a bridge and duplicated registration |
| **C) Write the text exposition format inside Encina** | No third-party dependency | A format with histogram buckets, units, `_total` and escaping rules to maintain forever; a bug is silent wrong data |

### Chosen Option: **A - wrap the official exporter, prerelease while it is beta** (recommended, pending the maintainer)

### Rationale

- The repository already carries the pin and other prerelease pins with a comment naming the issue that moves them (`Microsoft.DurableTask.InProcessTestHost`, `MySqlCdc`); this one gets the same treatment: the pin moves from `1.13.0-beta.1` to the beta of the same train as `OpenTelemetry` 1.17.0 (`1.17.0-beta.1`, present on nuget.org), checked by a restore at the start of Phase 1, and carries a comment pointing at the follow-up issue (`artifacts/issues/prometheus-exporter-stable-pin.md`) that moves it to the first stable release.
- The package project sets a `VersionSuffix` of `beta` so no stable package depends on a prerelease one. Whether the 1.0 release ships this package while the exporter is still beta is the maintainer's release decision; the plan does not decide it.
- B wins when a stable dependency matters more than SDK alignment; C wins only if the maintainer wants zero third-party code in the scrape path.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Who may read the endpoint, and how it is mapped</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MapEncinaPrometheus()` on `IEndpointRouteBuilder`; options `AuthorizationPolicy` and `AllowAnonymous`; startup validation fails unless exactly one is chosen (fail closed); `AllowAnonymous` logs a Warning at mapping** | The choice is explicit and visible; endpoint routing gives `RequireAuthorization` and `AllowAnonymous` conventions; a tenant tag is never exposed by accident | A host with no authentication configured must say `AllowAnonymous = true` once |
| **B) `UseEncinaPrometheus()` middleware on `IApplicationBuilder`, anonymous by default, as in the issue** | Matches the issue text; zero configuration; works in apps without endpoint routing | No authorization conventions on a middleware; the default exposes operational data and, with multi-tenancy on (ADR-054 D4), tenant identifiers |

### Chosen Option: **A - an explicit access decision at startup** (recommended, pending the maintainer)

### Rationale

- `AGENTS.md` section 3 makes gates fail closed when context is missing; an endpoint that publishes tenant tags is such a gate. A fails startup with a message that names the two options and never a value.
- B wins for an in-cluster deployment protected by network policy where a metrics path must work with no authentication code; A costs that host one line, `AllowAnonymous = true`.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Options that duplicate what <code>WithEncina()</code> already does</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No `EnableDefaultMetrics` and no `EnableRuntimeMetrics`: the package adds the reader, the endpoint and `AdditionalMeters`; the Encina meters and the runtime metrics come from `WithEncina()`** | One owner per subscription; no double `AddRuntimeInstrumentation()` | A host that does not call `WithEncina()` sees no Encina series until it does (documented) |
| **B) Keep the two switches as the issue lists them** | Matches the issue text | They would register the meter and the runtime instrumentation a second time on the same provider, with two owners for one decision |

### Chosen Option: **A - drop the two switches**

### Rationale

- `WithEncina()` already calls `AddMeter("Encina")` and `AddRuntimeInstrumentation()` (`ServiceCollectionExtensions.cs:213-214`), and ADR-054 settled points 1 and 8 want one owner per source and meter; a second subscription path is the defect those points remove.
- Class: Settled
- Settled by: ADR-054 Decision, settled points 1 and 8; `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:213-214`

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Series names are whatever the exporter derives from the instruments; the package renames nothing and `DisableTotalNameSuffixForCounters` stays at the exporter default, so a counter named `encina.x` is scraped as `encina_x_total` (the rule the plan of #187 reads); the issue's example series are replaced by the real ones in the README, listed by #187 | Trivial | Exporter default; ADR-054 settled point 4 (instrument names carry no `_total`; the suffix is the exporter's) |
| C2 | The package emits no span or metric of its own and adds no tenant tag; the endpoint exposes the tag other packages emit under ADR-054 D4, and the cardinality risk is documented in the guide of #906 | Settled | ADR-054 D4 |
| C3 | No tenant-qualified id is built, so the reserved `default` tenant segment and the injectivity rule do not apply | Settled | `AGENTS.md` section 6 row 10 (applies to stores and queries of tenant data; the package stores and queries none) |
| C4 | No new `EncinaError` code and no ADR-062 `ErrorCategory`: failures are an `OptionsValidationException` at startup, never an `Either` | Settled | ADR-062 (a category is chosen where an `EncinaError` is created; none is created) |
| C5 | `MetricsPath` defaults to `/metrics`, must start with `/`, contain no whitespace, `?` or `#`; `AdditionalMeters` holds non-empty meter names that are added with `AddMeter` when the provider is built | Trivial | The issue's `MetricsPath = "/metrics"` and its custom meter helpers task |
| C6 | `ScrapeResponseCacheDurationMilliseconds` keeps the exporter default and is not exposed | Trivial | The issue does not ask for it |
| C7 | The package targets `net10.0` and the exporter's `net9.0` asset is used; the restore at the start of Phase 1 must show no `NU1701` | Trivial | `AGENTS.md` section 1 (.NET 10 only); the exporter ships `net8.0` and `net9.0` assets |
| C8 | New public names: `EncinaPrometheusOptions`, `AddEncinaPrometheus`, `MapEncinaPrometheus` | Dilemma (recommended, pending the maintainer) | Public surface; alternatives are `AddEncinaPrometheusExporter` and `UseEncinaPrometheus` (Design Choice 3, option B) |

---

## Implementation Phases

### Phase 1: The Package, Its Options and Registration (pull request P1)

> **Goal**: The package builds, validates its options and adds the Prometheus reader (Design Choices 1, 2 and 4; if the maintainer picks B of Design Choice 1 this phase is dropped).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.OpenTelemetry.Prometheus/Encina.OpenTelemetry.Prometheus.csproj` (net10.0, nullable, `ManagePackageVersionsCentrally`, `VersionSuffix` `beta`, a `FrameworkReference` to `Microsoft.AspNetCore.App`, `PackageReference` to `OpenTelemetry.Exporter.Prometheus.AspNetCore`, `Microsoft.Extensions.Options`, `Microsoft.CodeAnalysis.PublicApiAnalyzers` private, `ProjectReference` to `Encina.OpenTelemetry`, `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`, and the two `PublicAPI` files as `AdditionalFiles`), following `src/Encina.Hangfire/Encina.Hangfire.csproj`.
2. **Create** `src/Encina.OpenTelemetry.Prometheus/EncinaPrometheusOptions.cs`: `public sealed class EncinaPrometheusOptions` with `MetricsPath` (string, default `"/metrics"`), `AuthorizationPolicy` (string?), `AllowAnonymous` (bool, default false) and `AdditionalMeters` (`IList<string>`, empty). No secret: the policy name is not one, so no `[JsonIgnore]` or `ToString()` override is required.
3. **Create** `src/Encina.OpenTelemetry.Prometheus/EncinaPrometheusOptionsValidator.cs`: `internal sealed class : IValidateOptions<EncinaPrometheusOptions>`; failures: path empty, not starting with `/` or containing whitespace, `?` or `#`; both `AuthorizationPolicy` and `AllowAnonymous` set, or neither; a blank meter name. Messages name the option, never a value.
4. **Create** `src/Encina.OpenTelemetry.Prometheus/ServiceCollectionExtensions.cs`: `public static IServiceCollection AddEncinaPrometheus(this IServiceCollection services, Action<EncinaPrometheusOptions>? configure = null)`: `ArgumentNullException.ThrowIfNull(services)`; `AddOptions<EncinaPrometheusOptions>().Configure(configure ?? (_ => { })).ValidateOnStart()`; `TryAddEnumerable` of the validator; `services.AddOpenTelemetry().WithMetrics(m => m.AddPrometheusExporter())`; `services.ConfigureOpenTelemetryMeterProvider((sp, builder) => ...)` reading the validated options to call `AddMeter` for each `AdditionalMeters` entry. It registers no hosted service and no singleton that holds a scoped service.
5. **Edit** `Directory.Packages.props` (through `mechanical-fixer`): move the pin to `1.17.0-beta.1` with the comment of Design Choice 2; **edit** `Encina.slnx` to add the project; **edit** `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` to reference the new project.
6. **Create** the two `PublicAPI` files (`PublicAPI.Shipped.txt` empty, `PublicAPI.Unshipped.txt` with the symbols) and `.github/coverage-manifest/Encina.OpenTelemetry.Prometheus.json` through `mechanical-fixer` (you decide the exact lines from the Research table).
7. **Create** `tests/Encina.UnitTests/OpenTelemetry/Prometheus/EncinaPrometheusOptionsTests.cs`: defaults, and one test per validator rule with the exact failure (a valid policy, a valid anonymous choice, both, neither, bad paths, a blank meter).
8. **Create** `tests/Encina.UnitTests/OpenTelemetry/Prometheus/PrometheusRegistrationTests.cs`: a provider built with `ValidateOnBuild` and `ValidateScopes` starting a `MeterProvider`; the options are validated at start (a host with neither choice fails with `OptionsValidationException`); an additional meter's counter is visible to the exporter reader (a `Meter` created by the test, a counter incremented, the reader's output contains the series with the `_total` suffix).
9. **Create** `tests/Encina.GuardTests/OpenTelemetry/Prometheus/PrometheusGuardTests.cs`: `AddEncinaPrometheus` with a null `services` throws `ArgumentNullException`; a null `configure` is valid and yields the validation failure at start (neither choice); the validator with a null options object throws.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no compatibility layers). Issue #180, plan docs/plans/prometheus-exporter-implementation-plan-180.md,
phase 1. A new package Encina.OpenTelemetry.Prometheus wraps OpenTelemetry.Exporter.Prometheus.AspNetCore (beta only, pinned in
Directory.Packages.props at 1.13.0-beta.1 and unused). WithEncina() already subscribes the Encina meters and runtime metrics.

TASK:
1. Create the project, EncinaPrometheusOptions, EncinaPrometheusOptionsValidator and AddEncinaPrometheus as the plan lists.
2. Move the exporter pin to 1.17.0-beta.1 (restore must show no NU1605 or NU1701), add the project to Encina.slnx and reference it
   from Encina.UnitTests and Encina.GuardTests.
3. Startup validation fails unless exactly one of AuthorizationPolicy and AllowAnonymous is chosen (fail closed).
4. Add the unit and guard tests listed; PublicAPI files, the manifest and Directory.Packages.props go through mechanical-fixer.

KEY RULES:
- Do not subscribe Encina meters or add runtime instrumentation here (WithEncina owns them, ADR-054 settled points 1 and 8).
- Messages of validation failures name the option, never a value; no EncinaError.Message anywhere.
- Edit source only with Edit/Write; no [Obsolete]; tests use Shouldly through Encina.Testing.Shouldly and execute real code.

REFERENCE FILES:
- src/Encina.Hangfire/Encina.Hangfire.csproj (satellite project shape)
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs (WithEncina)
- Directory.Packages.props (the Prometheus pin and the prerelease comments)
```

</details>

---

### Phase 2: The Endpoint, Logging and the README (pull request P2)

> **Goal**: `MapEncinaPrometheus()` maps the scraping endpoint with the chosen access decision and logs it (Design Choice 3, option A; if the maintainer picks B the extension becomes `UseEncinaPrometheus` and the authorization checks move to the options validator).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.OpenTelemetry.Prometheus/EncinaPrometheusEndpointExtensions.cs`: `public static IEndpointConventionBuilder MapEncinaPrometheus(this IEndpointRouteBuilder endpoints)`: guard; read `IOptions<EncinaPrometheusOptions>` from `endpoints.ServiceProvider`; call `endpoints.MapPrometheusScrapingEndpoint(options.MetricsPath)`; when `AuthorizationPolicy` is set apply `RequireAuthorization(policy)`; when `AllowAnonymous` is set apply `AllowAnonymous()` and log Warning 60051; log Information 60050 with the path. Verify at the start of the phase that the pinned beta returns an `IEndpointConventionBuilder` (the documentation file lists the signatures, not the return type); if it does not, the extension maps a group and applies the conventions to the group.
2. **Create** `src/Encina.OpenTelemetry.Prometheus/PrometheusLog.cs`: `internal static partial class PrometheusLog` with two `[LoggerMessage]` methods, EventIds 60050 (endpoint mapped, path) and 60051 (anonymous access, path), XML docs naming the range `EventIdRanges.OpenTelemetryPrometheus`.
3. **Edit** `src/Encina/Diagnostics/EventIdRanges.cs`: `public static readonly (int Min, int Max) OpenTelemetryPrometheus = (60050, 60099);` with a summary; add a row to `docs/engineering/eventid-block-registry.md` (a registered range row and the block row set to `registered`); add `Encina.OpenTelemetry.Prometheus` to the `AssemblyRanges` map of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`; run `EventIdRegistryAgreementTests` and the architecture tests. The new field goes to `src/Encina/PublicAPI.Unshipped.txt` through `mechanical-fixer`.
4. **Edit** `src/Encina.OpenTelemetry.Prometheus/PublicAPI/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.OpenTelemetry.Prometheus.json` (mechanical-fixer); **edit** `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` to reference the package.
5. **Create** `tests/Encina.IntegrationTests/Observability/Prometheus/PrometheusEndpointTests.cs` (`[Trait("Category", "Integration")]`, TestHost, no Docker): with an authorization policy and a test authentication handler an unauthenticated scrape is denied and an authorized one returns 200 with `text/plain` Prometheus content containing the series of a test meter; with `AllowAnonymous = true` a scrape returns 200 and Warning 60051 is logged once; a host with neither choice fails to start with `OptionsValidationException`; the scrape body contains no value that the test put in an error message.
6. **Edit** `tests/Encina.GuardTests/OpenTelemetry/Prometheus/PrometheusGuardTests.cs`: `MapEncinaPrometheus` with a null `endpoints` throws.
7. **Create** `src/Encina.OpenTelemetry.Prometheus/README.md` (through `docs-writer`): install, the two calls, the access decision, the series-name rule (`_total`, `_seconds`), the beta status of the dependency, the tenant tag exposure and the Collector alternative; and `changelog.d/180-prometheus-scraping-endpoint.added.md` (through `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #180 plan docs/plans/prometheus-exporter-implementation-plan-180.md, phase 2. Phase 1 merged: the package, its options, the
validator and AddEncinaPrometheus exist. EventIds come from the 10000+ zone (Observability and platform base 60000); this package
takes 60050-60099 and the Serilog package of #182 takes 60000-60049.

TASK:
1. Add MapEncinaPrometheus (IEndpointRouteBuilder) mapping MapPrometheusScrapingEndpoint and applying RequireAuthorization(policy)
   or AllowAnonymous(); log 60050 on mapping and 60051 (Warning) for anonymous access.
2. Register the range OpenTelemetryPrometheus (60050, 60099) in EventIdRanges.cs, the block registry and the AssemblyRanges map;
   run EventIdRegistryAgreementTests and the architecture tests.
3. Add the TestHost integration tests and the guard test of the plan; README through docs-writer.

KEY RULES:
- Registry first, ids packed without gaps; never an id outside 60050-60099.
- Never log a header, a token, a tenant id or an exception message; the path is the only value logged.
- Fail closed: no endpoint is reachable without an explicit access decision.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- tests/Encina.IntegrationTests/Observability/OpenTelemetry/ (TestHost and trait conventions)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Prometheus exposition format and metric types | Counters end in `_total`, histograms expose `_bucket`, `_sum`, `_count`; a name is lowercase with underscores | Why the series differ from the instrument names (register row C1) |
| OpenTelemetry Prometheus exporter | A pull reader; counters get `_total`, unit `s` becomes `_seconds`; options `ScrapeEndpointPath`, `ScrapeResponseCacheDurationMilliseconds`, `DisableTotalNameSuffixForCounters` | The wrapped implementation |
| ADR-054 | Settled points 1, 4, 8; D4 tenant tag on metrics | Names, ownership, exposure |
| ADR-062 | Error category where an `EncinaError` is created | Register row C4 |
| `AGENTS.md` section 3 | Gates fail closed; messages never reach telemetry | Design Choice 3 |
| `AGENTS.md` section 7 | EventId ranges registered first, `10000+` zone, base 60000 for observability | Ids 60050-60051 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `WithEncina` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:213-214` | Already subscribes `Encina` and runtime metrics; the package adds the reader |
| Satellite project shape | `src/Encina.Hangfire/Encina.Hangfire.csproj` | Template for the csproj |
| Prerelease pin comments | `Directory.Packages.props` (`MySqlCdc`, `Microsoft.DurableTask.InProcessTestHost`) | Convention for the beta pin |
| Observability stack | `.github/observability/prometheus.yml`, `otel-collector-config.yaml` | The Collector route of Design Choice 1 option B |
| `EventIdRanges`, block registry, `AssemblyRanges` map | `src/Encina/Diagnostics/EventIdRanges.cs`, `docs/engineering/eventid-block-registry.md`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Range registration |
| TestHost tests | `tests/Encina.IntegrationTests/Observability/OpenTelemetry/` | Pattern for the endpoint tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.OpenTelemetry.Prometheus` | 60050-60099 (new range `OpenTelemetryPrometheus`, Observability and platform block 60000-60999) | 60050 endpoint mapped (path), 60051 anonymous access (Warning, path); 60052-60099 free. The plan of #182 claims 60000-60049 in the same block; the code pull request registers the range and the registry row |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI, the changelog fragment and the project references; each pull request builds and passes alone.

| PR | Content | New src | Changed src / config | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|----------------------|-----------|--------------|----------|-------|-------|--------------|
| P1 | Package, options, validator, registration | 4 (csproj, options, validator, extensions) | 4 (`Directory.Packages.props`, `Encina.slnx`, `UnitTests.csproj`, `GuardTests.csproj`) | 3 | 0 | 1 (new manifest) | 2 (`PublicAPI` Shipped and Unshipped) | 14 | ~700 |
| P2 | Endpoint, log class, EventIds, README | 2 (endpoint extension, log class) | 5 (`EventIdRanges.cs`, registry doc, `AssemblyRanges` test, `IntegrationTests.csproj`, `Encina/PublicAPI.Unshipped.txt`) | 1 | 1 (`PrometheusGuardTests`) | 1 (edit) | 3 (`PublicAPI.Unshipped.txt` of the package, README, changelog) | 13 | ~560 |
| **Total** | | **6** | **9** | **4** | **1** | **2** | **5** | **27** | **~1,260** |

The columns sum to the total (6 + 9 + 4 + 1 + 2 + 5 = 27; 14 + 13 = 27). The pull request of this plan itself is one file. If Design Choice 3 picks option B the registry and range edits remain and the extension name changes.

Per-file coverage targets (per flag, one sentence each), written to `.github/coverage-manifest/Encina.OpenTelemetry.Prometheus.json`; the changed methods reach full line coverage (CRAP gate):

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `EncinaPrometheusOptions.cs` | 90 | 0 | 0 | 0 | Defaults and the meter list are read by the validator tests; the class has no parameters to guard, no interface to keep and no invariant worth a property |
| `EncinaPrometheusOptionsValidator.cs` | 95 | 70 | 0 | 0 | One test per rule drives every branch; guard covers the null options argument; the rules are a short list, not a domain with invariants |
| `ServiceCollectionExtensions.cs` | 90 | 85 | 0 | 0 | The registration test starts a real meter provider and reads the exporter output; guard covers the null services and the null configure |
| `EncinaPrometheusEndpointExtensions.cs` | 70 | 80 | 0 | 0 | The mapping branches (policy, anonymous) run in the TestHost integration tests, which count for the integration flag, so unit stays moderate; guard covers the null endpoints |
| `PrometheusLog.cs` | 0 | 0 | 0 | 0 | Generated `[LoggerMessage]` code with no logic of its own; it is executed through the endpoint tests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #180 - a self-hosted Prometheus scraping package - in two pull requests (P1 and P2).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always; edit source only with the
  Edit/Write tools; PublicAPI, manifests, changelog fragments and package pins through mechanical-fixer; README through docs-writer.
- ADR-054: exporters stay out of WithEncina(); WithEncina() already subscribes the Encina meters and runtime metrics; instrument
  names carry no _total (the exporter adds it); the tenant tag is on metrics when multi-tenancy is on, so the endpoint is fail-closed.

IMPLEMENTATION OVERVIEW:
1. P1: project, EncinaPrometheusOptions, validator, AddEncinaPrometheus, the exporter pin move, solution and test references.
2. P2: MapEncinaPrometheus with RequireAuthorization or AllowAnonymous, PrometheusLog 60050-60051, the OpenTelemetryPrometheus range
   (60050-60099) in EventIdRanges, registry and AssemblyRanges, TestHost integration tests, README.

KEY PATTERNS:
- Startup validation (ValidateOnStart) with option names, never values, in messages.
- A real MeterProvider and a real Meter in tests; never reflection only.
- Guard tests for every public method with parameters; the nullable configure is valid.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, src/Encina.Hangfire/Encina.Hangfire.csproj
- src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
- docs/plans/prometheus-exporter-implementation-plan-180.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | The scrape output is produced on request by the SDK reader, so no cache provider is read. |
| 2 | OpenTelemetry | ✅ Phases 1-2 | It exports the meter provider fed by `WithEncina()`, adds no instrument of its own, and shows the ADR-054 D4 tenant tag emitted by other packages. |
| 3 | Structured Logging | ✅ Phase 2 | EventIds 60050-60051 report the mapped endpoint and a warning for anonymous access, without any error message text. |
| 4 | Health Checks | ❌ N/A | The pull exporter is served by the host itself, so nothing needs reaching, and the Prometheus `up` series is the probe. |
| 5 | Validation | ✅ Phase 1 | The options validator ensures the path is well formed and an access decision is made explicitly. |
| 6 | Resilience | ❌ N/A | There is no outbound call, so Prometheus retries on its side. |
| 7 | Distributed Locks | ❌ N/A | There is no shared state, so every instance serves its own series. |
| 8 | Transactions | ❌ N/A | The operation is a read-only scrape. |
| 9 | Idempotency | ❌ N/A | A scrape is idempotent by nature. |
| 10 | Multi-Tenancy | ✅ Phase 2 | The package adds no tenant tag, but the endpoint exposes the ADR-054 D4 tag, so access is fail-closed (Design Choice 3). |
| 11 | Module Isolation | ❌ N/A | Observability is cross-module by design. |
| 12 | Audit Trail | ❌ N/A | It is read access to operational metrics, not an auditable business operation. |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 accepted | On `main` | Settled points 1, 4, 8 and D4 |
| ADR-021 `10000+` zone and the block registry | On `main` | Base 60000 for observability and platform |
| `OpenTelemetry.Exporter.Prometheus.AspNetCore` beta that restores against `OpenTelemetry` 1.17.0 | Checked at the start of Phase 1 | `1.17.0-beta.1` is the planned pin; the exporter has no stable release |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Instrument rename and meter rename | #2678, #2677 | The series names change when they land; this package renames nothing |
| Dashboards that read the series | #187 | Owns the series catalogue and the Grafana assets |
| Tenant attribute helper | #2676 | Defines when the tenant tag exists, which decides what the endpoint exposes |
| Observability guide | #906 | Documents the tag cardinality and the Collector alternative |
| Moving the pin to the first stable exporter release | `artifacts/issues/prometheus-exporter-stable-pin.md` | The `VersionSuffix` is dropped in the same change; the pin comment names this issue |

---

## Next Steps

1. Link this plan from #180 and ask the maintainer to answer Design Choices 1 to 3 and register row C8 when work starts; Design Choice 1 is answered together with the family (#613, #614) and the OTLP plan (#1043).
2. Implement P1, then P2. The orchestrator opens the follow-up issue file `artifacts/issues/prometheus-exporter-stable-pin.md` before P1 starts, because the pin comment of Design Choice 2 names its number.
