# Implementation Plan: `Encina.Serilog.OpenTelemetry` — Log Enrichment, Request Logging and OTLP Log Export

> **Issue**: [#182](https://github.com/dlrivada/Encina/issues/182)
> **Type**: Feature (observability, structured logging, new package)
> **Related**: ADR-054 (settled point 2, D4 tenant, D6 point 3), ADR-021 and the block registry (EventIds), #1043 and #1048 (OTLP exporter option and `WithLogging` in `Encina.OpenTelemetry`), #613 and #614 (sibling exporter plans of this family), #175 (correlation and causation), #186 (trace context on `IRequestContext`), #2442 (`ModuleId`), #906 (observability guide), ADR-062
> **Complexity**: Medium (small code, but six decisions about privacy, placement and the log-export route)
> **Estimated Scope**: ~330 lines of production code + ~900 lines of tests + 1 README, in four code pull requests of 13, 11, 11 and 13 files
> **Milestone**: v0.14.0 Hardening

---

## Summary

### What exists today (read on 2026-11-23, main `682b1d70b`)

- Serilog appears nowhere in `src/` or `Directory.Packages.props` (the one hit, a comment in `src/Encina.Audit.Marten/ServiceCollectionExtensions.cs:112`, only names it as an example of a logger provider). The package needs new central pins: `Serilog` `4.4.0` and `Serilog.Sinks.OpenTelemetry` `4.2.0`, the latest stable versions on nuget.org on 2026-11-23. The sink targets up to `net9.0`, depends on `Serilog` 4.2.0, and brings `Google.Protobuf` and `Grpc.Net.Client`; the restore audit of the chosen versions is checked at the start of Phase 1.
- The ambient request context is `IRequestContextAccessor` (`src/Encina/Abstractions/IRequestContextAccessor.cs`), backed by one static `AsyncLocal` (`src/Encina/Core/RequestContextAccessor.cs:53`): "every instance observes the same" value (the class summary, line 12), so an enricher built without DI reads the same context as the dispatcher. `IRequestContext` carries `CorrelationId` (string), `CausationId`, `TenantId` (`string?`) and `Identity` (`context.UserId` is an extension property over it).
- No request logging behavior exists: `src/Encina/Pipeline/Behaviors/` holds only the two activity and the two metrics behaviors, and a search of `src` for `BeginScope(` finds nothing.
- Serilog's own `LogEvent` has `TraceId` and `SpanId` (read from the 4.2.0 documentation file), which Serilog sets from `Activity.Current` when the event is created and which the OpenTelemetry sink reads; Phase 1 proves that with a test before any code relies on it.
- Log export to OTLP has a second route in `Encina.OpenTelemetry`: #1048 adds `WithLogging` to `WithEncina()` and #1043 wires `AddOtlpExporter` on it, for hosts that log through `Microsoft.Extensions.Logging` providers.
- `src/Encina.Security.PII` has `IPIIMasker`, `[PII]`, `[SensitiveData]`, `[MaskInLogs]` and `PIILoggerExtensions.LogMasked`, which mask a value for a logger call; no Serilog adapter exists.
- No EventId range is registered for the package; the block registry has no `10000+` rows yet, so the first free slot of the observability and platform block is 60000.

### What the issue proposes and what changed since

The issue (2026-era text, `status:proposal`) proposes `EncinaLogEnricher` (`CorrelationId`, `TenantId`, `UserId`), a `TraceContextEnricher`, `AddEncinaSerilog` with `EnableOpenTelemetryExport`, a `LoggingBehavior<,>`, an OTLP sink helper, log levels per request type and sensitive-data filtering. Facts that change it: Serilog cannot be reconfigured after the logger is built, so the sink and the enrichers can only be added on `LoggerConfiguration`, never by a DI call; a user id in every log line is personal data and data minimisation (GDPR Art. 5(1)(c) and Art. 25, the principle ADR-054 D4 applies to baggage) points at off by default; Serilog already carries the trace and span ids; the request logging behavior does not depend on Serilog at all; and Serilog's own per-category minimum levels already give "log levels per request type".

### What this plan delivers

- A new package `Encina.Serilog.OpenTelemetry` with `EncinaSerilogOptions`, a validator, the property-name constants, `EncinaLogEnricher` (correlation id, tenant when resolved, user id only on opt-in), `TraceContextEnricher`, `Enrich.WithEncinaContext()`, and `AddEncinaSerilog` (Phases 1 and 2).
- A `RequestLoggingPipelineBehavior<,>` with three EventIds, registered only when enabled (Phase 3).
- `WriteTo.EncinaOpenTelemetry()`, a helper over the OpenTelemetry sink with Encina defaults (Phase 4).
- Shared pieces and owner: this plan owns the package, the log property names and EventIds 60000-60002; #1043 and #1048 own the Microsoft-logging OTLP route; #186 owns trace fields on `IRequestContext`; #2442 owns `ModuleId`; #906 owns the guide; the Prometheus plan of #180 takes 60050-60099 of the same block.
- Affected packages: `Encina.Serilog.OpenTelemetry` (new), `Encina` (core, only `EventIdRanges.cs` and its `PublicAPI` line). Provider category: observability (logging); no database or transport provider.
- Not delivered, with the reason: a destructuring policy that masks `[PII]` properties of logged objects (a follow-up issue file `artifacts/issues/serilog-pii-destructuring-policy.md`, Design Choice 6); a request-payload log (Design Choice 6); a sink endpoint health check (`artifacts/issues/exporter-endpoint-health-checks.md`, matrix row 4); `ModuleId` (#2442).

---

## Design Choices

<details>
<summary><strong>1. Where the request logging behavior lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In this package, registered by `AddEncinaSerilog` when `EnableRequestLogging` is true** (the issue) | One package for "how an Encina request looks in a log"; the property names and the behavior share one owner | A behavior that uses only `ILogger` sits in a package named for Serilog |
| **B) In core `Encina` next to the activity behaviors, as a logging-framework-agnostic `RequestLoggingPipelineBehavior`** | Hosts on NLog or plain Microsoft logging get it; core already holds opt-in behaviors | A core public type, EventIds in the core range, a core change for a satellite feature |

### Chosen Option: **A - in this package** (recommended, pending the maintainer)

### Rationale

- The behavior sets the same property vocabulary the enricher uses, so one owner keeps them in step; it stays opt-in and uses only `ILogger`, so a later move to core is a rename, not a redesign.
- B wins when most Encina hosts do not use Serilog and the maintainer wants request logging for all of them in the core package.
- Class: Dilemma

</details>

<details>
<summary><strong>2. The user id in log events</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Off by default: `EncinaSerilogOptions.IncludeUserId = false`; the host turns it on** | Data minimisation by default (a user id is a personal identifier; the reference application handles health data); one flag to switch on for debugging | The issue's "automatic UserId enrichment" needs one line of configuration |
| **B) Always on, as in the issue** | Per-user troubleshooting with no configuration | Every log line, in every sink, carries a personal identifier; erasure obligations reach the log store |
| **C) A pseudonymised id (a keyed hash)** | Correlates a user's events without the raw id | A key to manage and rotate, and a hash of a user id is still personal data under GDPR |

### Chosen Option: **A - off by default** (recommended, pending the maintainer)

### Rationale

- Data minimisation (GDPR Art. 5(1)(c) and Art. 25) is the principle ADR-054 D4 applies to baggage (Encina never propagates or stores it); D4's tenant rule covers span attributes and metric tags only, not logs, so this plan extends the principle to user ids on logs and the maintainer confirms the extension.
- B wins for a non-regulated internal tool where support needs the user in every line; C wins when logs must correlate one user across systems without storing the raw id and the host can manage a key.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Trace and span properties</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No `TraceContextEnricher`: rely on `LogEvent.TraceId` and `LogEvent.SpanId`** | No code; nothing to keep in step with Serilog | Sinks that print properties but not the event fields (console, file, Loki-style sinks) show no trace id |
| **B) A small `TraceContextEnricher` adding `TraceId` and `SpanId` text properties from `Activity.Current` (default on, as in the issue), only when absent** | Every sink shows the ids; matches the issue; trivial code | Duplicates what the event fields and the OpenTelemetry sink already carry |

### Chosen Option: **B - the enricher, on by default** (recommended, pending the maintainer)

### Rationale

- The package's stated users are Seq and Loki examples (the issue's documentation task); a property is what text sinks show. The OpenTelemetry sink and Seq read the event fields, so the property is for the rest.
- A wins when the maintainer's hosts all use field-aware sinks and prefers no duplicate data.
- Class: Dilemma

</details>

<details>
<summary><strong>4. How logs reach OTLP</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A Serilog sink helper, `WriteTo.EncinaOpenTelemetry()`, over `Serilog.Sinks.OpenTelemetry`** (the issue) | Serilog events keep their templates and properties; works with Serilog-only hosts and alongside Seq or file sinks | A second OTLP configuration beside the traces and metrics one (endpoint, service name drift); a host that also uses #1048 must pick one route |
| **B) No sink: log through `UseSerilog(writeToProviders: true)` into the OpenTelemetry logger provider of #1048 and #1043** | One OTLP configuration and one resource for all three signals; no sink dependency (`Google.Protobuf`, `Grpc.Net.Client`) | Serilog events pass through the Microsoft logging layer, which flattens templates and some property types |

### Chosen Option: **A - the sink helper, with a documented rule to choose one route** (recommended, pending the maintainer)

### Rationale

- Hosts that adopt Serilog usually keep its sink pipeline; the helper gives them the OTLP route without leaving it. The rule "one route per host" is documented, and not detected (the two configurations never meet in DI).
- B wins when a host already configures OpenTelemetry once through `WithEncina()` and wants identical resource attributes on all signals; Phase 4 is then replaced by documentation, and the package carries no `Serilog.Sinks.OpenTelemetry` dependency.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Log levels per request type</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A custom table in the options: request type name or pattern to `LogLevel`** (the issue) | Levels differ per outcome (success, failure) per request without touching logging configuration | A second level mechanism beside the standard filters; a pattern language to define, document and test |
| **B) The behavior logs through a logger whose category is the request type's full name; the standard category filters do the rest** (`MinimumLevel.Override("MyApp.Orders.CreateOrder", ...)` in Serilog, `Logging:LogLevel` in Microsoft logging, namespace prefixes included) | One mechanism the host already knows; zero custom code; works for every logger | Levels are per category, not per outcome: success and failure of one request type share the category (the failure level has its own option, default `Warning`) |

### Chosen Option: **B - the request type is the log category** (recommended, pending the maintainer)

### Rationale

- The two outcome levels are two options (`SuccessLevel`, default `Information`; `FailureLevel`, default `Warning`); the category filter decides whether each is emitted for a type.
- A wins when the maintainer needs "log failures of this type at Error and successes at Debug" in one table without touching the logging configuration.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Sensitive data in request logs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) By construction: the behavior logs the request type name, the outcome, the error code and the elapsed time only; never the request, the response, an error message or an exception message; the PII destructuring policy is a follow-up** | Nothing to mask; follows the rule that `EncinaError.Message` never reaches logs; exceptions go through `ForLogging()` | A developer who wants the payload in a development log cannot get it from the behavior |
| **B) An opt-in payload log masked by `IPIIMasker` of `Encina.Security.PII`** | Debuggable payloads with masking | A package dependency on `Encina.Security.PII`; masking is only as good as the `[PII]` annotations; a mistake leaks personal data into logs |

### Chosen Option: **A - metadata only** (recommended, pending the maintainer)

### Rationale

- A matches `AGENTS.md` section 3 and ADR-054 settled point 2 (code or type only). The destructuring policy (masking `[PII]` properties of objects the application logs with `{@Obj}`) is useful beyond this behavior and is opened as its own issue.
- B wins in a development environment where the maintainer accepts the masking dependency and wants payloads in the log.
- Class: Dilemma

</details>

<details>
<summary><strong>7. Where the sink and the enrichers are configured</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) On `LoggerConfiguration`: `Enrich.WithEncinaContext()` and `WriteTo.EncinaOpenTelemetry()`; `AddEncinaSerilog` registers options, the enricher as `ILogEventEnricher` (for `ReadFrom.Services`) and the behavior** | Matches how Serilog works: the logger is immutable once built | The issue's `EnableOpenTelemetryExport` flag in `AddEncinaSerilog` does not exist |
| **B) A DI call that changes the logger after it is built** | The issue's shape | Not possible: `Logger` is immutable once `CreateLogger()` has run |

### Chosen Option: **A - configure on `LoggerConfiguration`**

### Rationale

- Serilog's documented model builds the logger from `LoggerConfiguration` once; a later change needs a new logger, which `AddEncinaSerilog` must not replace under the host's feet.
- Class: Settled
- Settled by: the Serilog API (`Logger` has no mutating members, `LoggerConfiguration.CreateLogger()` is the only constructor path; Serilog 4.x documentation file); `src/Encina/Core/RequestContextAccessor.cs:12` (the static store lets `WithEncinaContext()` read the context without DI)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | `EncinaSerilogOptions`: `EnrichWithEncinaContext` (default true), `EnrichWithTraceContext` (default true), `IncludeUserId` (default false), `EnableRequestLogging` (default false: opt-in), `SuccessLevel` (default `Information`), `FailureLevel` (default `Warning`) | Trivial | The issue's flags; `AGENTS.md` section 3 (every messaging and cross-cutting feature is opt-in) |
| C2 | Property names are constants of one public class `EncinaLogProperties`: `CorrelationId`, `TenantId`, `UserId`, `TraceId`, `SpanId`, `RequestType`, `ErrorCode`, `ElapsedMs`, `Outcome` | Trivial | ADR-054 D3 (every attribute key lives in one constants class) |
| C3 | The enricher adds `TenantId` only when `IRequestContext.TenantId` is non-null (multi-tenancy resolved a tenant); with multi-tenancy off no tenant property exists; no tenant-qualified id is composed, so the reserved `default` tenant segment does not apply | Dilemma (recommended, pending the maintainer) | ADR-054 D4 covers span attributes and metric tags only (a tenant id there is the existing practice); a raw tenant id in every log event is an extension of that rule to logs, the same family as Design Choice 2. Alternatives: no tenant property by default (a flag like `IncludeUserId`), or a hashed tenant id. Recommended: the raw tenant id, only when resolved, because a tenant id is an operational key, not a personal identifier of a data subject |
| C4 | Fail closed has no meaning here (no security gate): a missing request context adds no Encina property and never throws; the enricher swallows nothing because it has nothing to throw | Settled | `AGENTS.md` section 3 (the fail-closed rule is for compliance and security gates; this is neither) |
| C5 | The behavior logs a failure with the `EncinaError` code only (the public `error.GetCode().IfNone("encina.unknown")`, `src/Encina/Results/EncinaErrors.cs:195`, as `SchemaDriftHealthCheck.cs:128` does; `GetEncinaCode()` is `internal` at `:280` and the new assembly has no `InternalsVisibleTo`), never `EncinaError.Message`; an exception that escapes `nextStep` is logged with `ex.GetType().Name` and `ex.ForLogging()` and rethrown, never swallowed; elapsed time comes from the injected `TimeProvider` | Settled | `AGENTS.md` section 3 (messages never reach logs; `ForLogging()`; time from `TimeProvider`); ADR-054 settled point 2 |
| C6 | No new `EncinaError` code and no ADR-062 `ErrorCategory`: the behavior returns the `Either` of `nextStep` unchanged | Settled | ADR-062 (a category is chosen where an `EncinaError` is created; none is created) |
| C7 | The package has no `ActivitySource` and no `Meter`, so no tenant tag on telemetry (ADR-054 D4 concerns spans and metrics) | Settled | ADR-054 D4 |
| C8 | The sink helper has `Endpoint` (`Uri?`), `Protocol` (default `Grpc`), `ServiceName`, `DeploymentEnvironment` and `ConfigureSink` (`Action<BatchedOpenTelemetrySinkOptions>`, the escape hatch for headers and batching); no header property, so no secret lives in the options class and no `[JsonIgnore]` or `ToString()` override is required; an `Endpoint` with user info is rejected | Trivial | `AGENTS.md` section 3 (options with a token or key); the sink API (`OpenTelemetry(LoggerSinkConfiguration, Action<BatchedOpenTelemetrySinkOptions>)`) |
| C9 | EventIds 60000 (request started, Debug), 60001 (request completed, `SuccessLevel`), 60002 (request failed, `FailureLevel`) in the new range `SerilogOpenTelemetry` (60000, 60049); the dynamic levels use the `LogLevel` parameter of `[LoggerMessage]` | Settled | `AGENTS.md` section 7 (ADR-021, the `10000+` zone, base 60000 for observability and platform) |
| C10 | New public names: `EncinaSerilogOptions`, `EncinaLogProperties`, `EncinaLogEnricher`, `TraceContextEnricher`, `RequestLoggingPipelineBehavior<,>`, `WithEncinaContext`, `EncinaOpenTelemetry`, `AddEncinaSerilog` | Dilemma (recommended, pending the maintainer) | Public surface; the issue's names are kept except `LoggingBehavior`, renamed to say what it does |

---

## Implementation Phases

### Phase 1: The Package, Options and Property Names (pull request S1)

> **Goal**: The package builds, validates its options and holds the property vocabulary.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Serilog.OpenTelemetry/Encina.Serilog.OpenTelemetry.csproj` (net10.0, nullable, `ManagePackageVersionsCentrally`, `PackageReference` to `Serilog` and the private `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`; `ProjectReference` to `Encina`; `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests` and `Encina.IntegrationTests`; the two `PublicAPI` files as `AdditionalFiles`), following `src/Encina.Hangfire/Encina.Hangfire.csproj`. The sink package arrives in Phase 4.
2. **Create** `EncinaSerilogOptions.cs` (register row C1), `EncinaSerilogOptionsValidator.cs` (`internal sealed class : IValidateOptions<EncinaSerilogOptions>`: `SuccessLevel` and `FailureLevel` not `LogLevel.None`, both defined enum values; messages name the option, never a value) and `EncinaLogProperties.cs` (register row C2).
3. **Edit** `Directory.Packages.props` (add `Serilog` `4.4.0`), `Encina.slnx`, `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` through `mechanical-fixer`; **create** the two `PublicAPI` files and `.github/coverage-manifest/Encina.Serilog.OpenTelemetry.json` through `mechanical-fixer` (the lines come from the Research table).
4. **Create** `tests/Encina.UnitTests/Serilog/EncinaSerilogOptionsTests.cs`: defaults (each register row C1 value) and one test per validator rule; and a first test that proves the premise of Design Choice 3: a real Serilog `Logger` writing inside an `Activity` yields a `LogEvent` whose `TraceId` and `SpanId` equal the activity's. The test registers an `ActivityListener` (`ShouldListenTo` true, `Sample` returning `AllDataAndRecorded`) and starts the activity from its own `ActivitySource`, and first asserts `Activity.Current` is not null, otherwise the test would pass vacuously on null ids.
5. **Create** `tests/Encina.GuardTests/Serilog/EncinaSerilogGuardTests.cs`: the validator with a null options object throws `ArgumentNullException`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no compatibility layers). Issue #182, plan docs/plans/serilog-opentelemetry-implementation-plan-182.md,
phase 1. A new package Encina.Serilog.OpenTelemetry holds Serilog enrichment, an opt-in request logging behavior and an OTLP sink
helper. Serilog is not referenced anywhere yet.

TASK:
1. Create the project, EncinaSerilogOptions, EncinaSerilogOptionsValidator and EncinaLogProperties as the plan lists.
2. Add Serilog 4.4.0 to Directory.Packages.props, the project to Encina.slnx and the references from Encina.UnitTests and
   Encina.GuardTests; the restore must show no audit warning.
3. Add the options tests and the test that a Serilog LogEvent carries the current Activity's TraceId and SpanId (register an
   ActivityListener with sampling on and assert Activity.Current is not null first, or the test is vacuous).

KEY RULES:
- The package namespace Encina.Serilog.OpenTelemetry and the test namespace Encina.UnitTests.Serilog shadow the root Serilog and
  OpenTelemetry namespaces: use using directives, never fully qualified Serilog.Core.X or OpenTelemetry.X references.
- IncludeUserId defaults to false; EnableRequestLogging defaults to false.
- Messages of validation failures name the option, never a value.
- Edit source only with Edit/Write; no [Obsolete]; tests use Shouldly through Encina.Testing.Shouldly and execute real code.
- PublicAPI files, the manifest and the pins go through mechanical-fixer.

REFERENCE FILES:
- src/Encina.Hangfire/Encina.Hangfire.csproj
- src/Encina/Abstractions/IRequestContextAccessor.cs
- docs/engineering/eventid-block-registry.md (block 60000)
```

</details>

---

### Phase 2: Enrichment and Registration (pull request S2)

> **Goal**: Every log event carries the correlation id, the tenant when resolved, optionally the user id, and the trace and span ids.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `EncinaLogEnricher.cs`: `public sealed class EncinaLogEnricher : ILogEventEnricher`; constructor `(IRequestContextAccessor accessor, EncinaSerilogOptions options)` with `ArgumentNullException.ThrowIfNull` on both; `Enrich` reads `accessor.RequestContext`; when null it adds nothing; otherwise `AddPropertyIfAbsent` for `CorrelationId` (when non-empty), `TenantId` (when non-null) and `UserId` (only when `IncludeUserId` and the identity has a user id).
2. **Create** `TraceContextEnricher.cs` (`ILogEventEnricher`, no constructor parameters): adds `TraceId` and `SpanId` from `Activity.Current` as text properties, only when absent and only when an activity exists.
3. **Create** `LoggerEnrichmentConfigurationExtensions.cs`: `public static LoggerConfiguration WithEncinaContext(this LoggerEnrichmentConfiguration enrich, Action<EncinaSerilogOptions>? configure = null)`: guard; builds the options, validates them with the validator and throws `OptionsValidationException` on failure; builds `new EncinaLogEnricher(new RequestContextAccessor(), options)` (the static store makes the instance equivalent to the registered one, `RequestContextAccessor.cs:12`) and, when `EnrichWithTraceContext`, a `TraceContextEnricher`.
4. **Create** `ServiceCollectionExtensions.cs`: `public static IServiceCollection AddEncinaSerilog(this IServiceCollection services, Action<EncinaSerilogOptions>? configure = null)`: guard; options with `ValidateOnStart`; `TryAddEnumerable` of the validator; `TryAddEnumerable` of `ILogEventEnricher` singletons (`EncinaLogEnricher` resolving `IRequestContextAccessor`, `TraceContextEnricher` when enabled) so `ReadFrom.Services` finds them; registers nothing scoped in a singleton.
5. **Edit** `PublicAPI.Unshipped.txt` and the manifest through `mechanical-fixer`; add `changelog.d/182-serilog-enrichment.added.md` through `mechanical-fixer`.
6. **Create** tests: `tests/Encina.UnitTests/Serilog/EncinaLogEnricherTests.cs` (a real `Logger` with an in-memory sink and a real `RequestContext`: correlation id present, tenant absent when null and present when set, user id absent by default and present with `IncludeUserId`, no context adds nothing and does not throw, a property already on the event is kept), `TraceContextEnricherTests.cs` (inside and outside an `Activity`), `EnrichmentRegistrationTests.cs` (a provider built with `ValidateOnBuild` and `ValidateScopes` resolves the enrichers; `WithEncinaContext()` on a real `LoggerConfiguration` works with no DI), and **edit** the guard test file for the constructor and extension null checks (a null `configure` is valid).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #182 plan docs/plans/serilog-opentelemetry-implementation-plan-182.md, phase 2. Phase 1 merged: the package, options, validator
and EncinaLogProperties exist. IRequestContextAccessor reads one static AsyncLocal, so a new RequestContextAccessor() sees the same
context as the dispatcher.

TASK:
1. Add EncinaLogEnricher, TraceContextEnricher, Enrich.WithEncinaContext() and AddEncinaSerilog as the plan lists.
2. Add the unit, registration and guard tests with a real Serilog Logger and an in-memory sink.

KEY RULES:
- UserId only when IncludeUserId is true; TenantId only when the context has a tenant; a missing context adds nothing and never throws.
- Never put EncinaError.Message, a request or a response into a property.
- Guard tests for every public constructor and method with parameters; the nullable configure is valid.

REFERENCE FILES:
- src/Encina/Core/RequestContextAccessor.cs, src/Encina/Abstractions/IRequestContext.cs
- src/Encina/Identity (RequestIdentity, the UserId extension)
- docs/plans/serilog-opentelemetry-implementation-plan-182.md (Design Choices 2, 3, 7)
```

</details>

---

### Phase 3: The Request Logging Behavior and EventIds (pull request S3)

> **Goal**: An opt-in behavior logs started, completed and failed with metadata only (Design Choices 1, 5 and 6).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `RequestLoggingPipelineBehavior.cs`: `public sealed class RequestLoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>`; constructor `(ILoggerFactory loggerFactory, IOptions<EncinaSerilogOptions> options, TimeProvider? timeProvider = null)`; the logger is `loggerFactory.CreateLogger(typeof(TRequest).FullName ?? typeof(TRequest).Name)` so category filters apply (Design Choice 5); `Handle` begins one scope with `RequestType` only (the enricher owns the context properties), logs 60000, awaits `nextStep()`, then logs 60001 or, for a `Left`, 60002 with `error.GetCode().IfNone("encina.unknown")` (public; the `internal` `GetEncinaCode()` is not reachable from this assembly); an exception is logged with its type name and `ForLogging()` and rethrown (register row C5). The type parameter constraint matches `MessagingEnricherPipelineBehavior`.
2. **Create** `Diagnostics/RequestLoggingLogMessages.cs`: `internal static partial class` with three `[LoggerMessage]` methods, EventIds 60000-60002, the level of 60001 and 60002 passed as a `LogLevel` parameter, XML docs naming the range `EventIdRanges.SerilogOpenTelemetry`.
3. **Edit** `src/Encina/Diagnostics/EventIdRanges.cs`: `public static readonly (int Min, int Max) SerilogOpenTelemetry = (60000, 60049);` with a summary; the registry rows in `docs/engineering/eventid-block-registry.md`; the `AssemblyRanges` map of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`; the line in `src/Encina/PublicAPI.Unshipped.txt` (through `mechanical-fixer`); run `EventIdRegistryAgreementTests` and the architecture tests.
4. **Edit** `ServiceCollectionExtensions.cs`: `EnableRequestLogging` is bound lazily through `Action<EncinaSerilogOptions>`, so `AddEncinaSerilog` invokes the `configure` action once on a fresh `EncinaSerilogOptions` at registration time to read the flag (the same action is also passed to `services.Configure`; the action must be side-effect free, stated in its XML doc); when the flag is true, `TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(RequestLoggingPipelineBehavior<,>)))` as `AddEncinaOpenTelemetry` does for its behavior; verify the position of the behavior against `PipelineOrderResolver` at the start of the phase and state the answer in the PR.
5. **Create** `tests/Encina.UnitTests/Serilog/RequestLoggingPipelineBehaviorTests.cs` (a real behavior, a real logger factory with a capturing provider: a `Right` logs started and completed at the configured level; a `Left` logs failed with the error code and never the message; a thrown exception is logged with the type name and rethrown; a category filter on the request type suppresses completed; the elapsed time uses a `FakeTimeProvider`; and a registration test: flag false, the behavior is not in the provider and a request writes no request log); **edit** the guard test file (null `loggerFactory`, null `options`, a null `timeProvider` is valid) and the package `PublicAPI.Unshipped.txt` and manifest through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #182 plan docs/plans/serilog-opentelemetry-implementation-plan-182.md, phase 3. Phases 1 and 2 merged. EventIds come from the
10000+ zone (base 60000, Observability and platform); this package takes 60000-60049 and #180 takes 60050-60099.

TASK:
1. Add RequestLoggingPipelineBehavior<,> and RequestLoggingLogMessages (60000-60002) as the plan lists.
2. Register the range SerilogOpenTelemetry (60000, 60049) in EventIdRanges.cs, the block registry and the AssemblyRanges map; run
   EventIdRegistryAgreementTests and the architecture tests.
3. Register the behavior when EnableRequestLogging is true (AddEncinaSerilog invokes a copy of configure once at registration to read
   the flag); add the unit and guard tests, including flag false: no behavior, no log.

KEY RULES:
- Log the failure code with the public error.GetCode().IfNone("encina.unknown"); GetEncinaCode() is internal to core and not reachable.
- Registry first, ids packed without gaps; never an id outside 60000-60049.
- Log the request type, outcome, error code and elapsed time only; never EncinaError.Message, a request, a response or an exception message;
  exceptions go through ForLogging() and are rethrown.
- Time from the injected TimeProvider; no DateTime.UtcNow.

REFERENCE FILES:
- src/Encina.OpenTelemetry/Behaviors/MessagingEnricherPipelineBehavior.cs (behavior shape and registration)
- src/Encina/Pipeline/Behaviors/CommandMetricsPipelineBehavior.cs (error code, failure detector pattern)
- src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
```

</details>

---

### Phase 4: The OTLP Sink Helper and the README (pull request S4)

> **Goal**: `WriteTo.EncinaOpenTelemetry()` configures the OpenTelemetry sink with Encina defaults (Design Choice 4, option A; if the maintainer picks B this phase becomes documentation and the package carries no sink dependency).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `EncinaOtlpSinkOptions.cs` (register row C8) with an `internal` validator `EncinaOtlpSinkOptionsValidator.cs`, and `LoggerSinkConfigurationExtensions.cs`: `public static LoggerConfiguration EncinaOpenTelemetry(this LoggerSinkConfiguration sink, Action<EncinaOtlpSinkOptions>? configure = null)`: guard, validate, map `Endpoint` (null keeps the sink default), `Protocol`, and `ResourceAttributes` (`service.name` when set, `deployment.environment.name` when set) into `BatchedOpenTelemetrySinkOptions`, then invoke `ConfigureSink`; it calls `sink.OpenTelemetry(...)`.
2. **Edit** `Directory.Packages.props` (add `Serilog.Sinks.OpenTelemetry` `4.2.0`) and the project file (the `PackageReference`) through `mechanical-fixer`; run the restore audit and record any transitive advisory for `Google.Protobuf` or `Grpc.Net.Client` in the PR.
3. **Create** tests: `tests/Encina.UnitTests/Serilog/EncinaOtlpSinkOptionsTests.cs` (defaults, validator rules including user info in the endpoint) and `tests/Encina.IntegrationTests/Observability/Serilog/SerilogOtlpSinkIntegrationTests.cs` (`[Trait("Category", "Integration")]`, no Docker: an in-process Kestrel or TestServer endpoint receives the HTTP/protobuf export; a logger with `WriteTo.EncinaOpenTelemetry` and the enrichers writes inside an `Activity`; the captured body contains the 16 trace-id bytes and the 8 span-id bytes and the correlation id text; an unreachable endpoint does not throw at write time); **edit** the guard test file (null `sink`, a null `configure` is valid) and the manifest and `PublicAPI.Unshipped.txt` through `mechanical-fixer`; **edit** `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` to reference the package.
4. **Create** `src/Encina.Serilog.OpenTelemetry/README.md` (through `docs-writer`): install, the three calls, the property table, the user-id and trace rules, "one OTLP route per host" with the `Encina.OpenTelemetry` alternative, and Seq and Loki examples; add `changelog.d/182-serilog-otlp-sink.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #182 plan docs/plans/serilog-opentelemetry-implementation-plan-182.md, phase 4. Phases 1 to 3 merged: options, enrichers,
AddEncinaSerilog and the request logging behavior exist. Serilog.Sinks.OpenTelemetry 4.2.0 exposes
WriteTo.OpenTelemetry(Action<BatchedOpenTelemetrySinkOptions>).

TASK:
1. Add EncinaOtlpSinkOptions, its validator and WriteTo.EncinaOpenTelemetry() as the plan lists.
2. Add the sink pin and the PackageReference; run the restore audit.
3. Add the unit tests and the in-process HTTP receiver integration test; README and changelog through the specialists.

KEY RULES:
- No header or token property in the options; credentials go through ConfigureSink or the environment; reject user info in Endpoint.
- Poll or await the export with a deadline in the integration test; never Thread.Sleep.
- Guard tests for every public method with parameters; the nullable configure is valid.

REFERENCE FILES:
- src/Encina.Serilog.OpenTelemetry (phases 1-3)
- tests/Encina.IntegrationTests/Observability/OpenTelemetry/ConsoleExporterIntegrationTests.cs
- docs/plans/otlp-exporter-implementation-plan-1043.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| OpenTelemetry Logs data model | A log record carries `TraceId`, `SpanId`, severity, body and attributes | Why the trace and span ids correlate logs with traces |
| Serilog 4.x | `LogEvent.TraceId`, `LogEvent.SpanId`; `ILogEventEnricher`; an immutable `Logger` | Design Choices 3 and 7 |
| `Serilog.Sinks.OpenTelemetry` 4.2.0 | `WriteTo.OpenTelemetry(Action<BatchedOpenTelemetrySinkOptions>)` | Phase 4 |
| ADR-054 | Settled point 2 (code or type only), D3 (constants), D4 (tenant rule, data minimisation) | Register rows C2, C3, C5, C7 |
| ADR-021 and the block registry | EventIds registered first; `10000+` zone, base 60000 for observability and platform | Register row C9 |
| ADR-062 | Error category where an `EncinaError` is created | Register row C6 |
| GDPR Art. 5(1)(c) and Art. 25 | Data minimisation and by design | Design Choice 2 (not legal advice) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestContextAccessor`, `RequestContextAccessor` | `src/Encina/Abstractions/IRequestContextAccessor.cs`, `src/Encina/Core/RequestContextAccessor.cs` | Ambient context for the enricher (static store, line 12) |
| `IRequestContext`, `RequestIdentity` | `src/Encina/Abstractions/IRequestContext.cs`, `src/Encina/Identity/` | Correlation id, tenant id, user id |
| `MessagingEnricherPipelineBehavior` and its registration | `src/Encina.OpenTelemetry/Behaviors/`, `ServiceCollectionExtensions.cs` | Shape of an opt-in behavior and `TryAddEnumerable` registration |
| `ForLogging()` | `src/Encina/Diagnostics/RedactedException.cs:150` | Exceptions in logs |
| `EventIdRanges`, block registry, `AssemblyRanges` map | `src/Encina/Diagnostics/EventIdRanges.cs`, `docs/engineering/eventid-block-registry.md`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Range registration |
| `Encina.Security.PII` | `src/Encina.Security.PII` | Out of scope here; the follow-up destructuring policy |
| Satellite project shape | `src/Encina.Hangfire/Encina.Hangfire.csproj` | Template for the csproj |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Serilog.OpenTelemetry` | 60000-60049 (new range `SerilogOpenTelemetry`, Observability and platform block 60000-60999) | 60000 request started (Debug), 60001 request completed, 60002 request failed (error code only); 60003-60049 free. The plan of #180 claims 60050-60099 in the same block; the code pull request registers the range and the registry row |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI, the changelog fragment, the pins and the project references; each pull request builds and passes alone.

| PR | Content | New src | Changed src / config | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|----------------------|-----------|--------------|----------|-------|-------|--------------|
| S1 | Package, options, validator, constants | 4 (csproj, options, validator, properties) | 4 (`Directory.Packages.props`, `Encina.slnx`, `UnitTests.csproj`, `GuardTests.csproj`) | 2 | 0 | 1 (new manifest) | 2 (`PublicAPI` Shipped and Unshipped) | 13 | ~480 |
| S2 | Enrichers, `WithEncinaContext`, `AddEncinaSerilog` | 4 (two enrichers, configuration extensions, `ServiceCollectionExtensions`) | 1 (`PublicAPI.Unshipped.txt`) | 3 | 1 (guard file) | 1 (edit) | 1 (changelog) | 11 | ~620 |
| S3 | Request logging behavior, log messages, EventIds | 2 (behavior, log messages) | 6 (`ServiceCollectionExtensions.cs`, `EventIdRanges.cs`, registry doc, `AssemblyRanges` test, `Encina/PublicAPI.Unshipped.txt`, package `PublicAPI.Unshipped.txt`) | 1 | 1 (guard file) | 1 (edit) | 0 | 11 | ~560 |
| S4 | OTLP sink helper, README | 3 (sink options, validator, extensions) | 4 (`Directory.Packages.props`, project file, `IntegrationTests.csproj`, `PublicAPI.Unshipped.txt`) | 2 (options unit test, integration test) | 1 (guard file) | 1 (edit) | 2 (README, changelog) | 13 | ~600 |
| **Total** | | **13** | **15** | **8** | **3** | **4** | **5** | **48** | **~2,260** |

The columns sum to the total (13 + 15 + 8 + 3 + 4 + 5 = 48; 13 + 11 + 11 + 13 = 48). The pull request of this plan itself is one file. If Design Choice 3 picks A the two enricher files and their tests leave S2; if Design Choice 4 picks B, S4 shrinks to documentation.

Per-file coverage targets (per flag, one sentence each), written to `.github/coverage-manifest/Encina.Serilog.OpenTelemetry.json`; the changed methods reach full line coverage (CRAP gate):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `EncinaSerilogOptions.cs` | 90 | 0 | 0 | 0 | 0 | Defaults are read by the options tests; the class has no parameters to guard and no invariant worth a property |
| `EncinaSerilogOptionsValidator.cs` | 95 | 70 | 0 | 0 | 0 | One test per rule drives every branch; guard covers the null options argument |
| `EncinaLogProperties.cs` | 0 | 0 | 0 | 0 | 0 | Constant strings with no executable code; they are asserted through the enricher and behavior tests |
| `EncinaLogEnricher.cs` | 95 | 80 | 0 | 0 | 0 | A real logger drives every branch (no context, tenant, user opt-in, existing property); guard covers both constructor arguments |
| `TraceContextEnricher.cs` | 95 | 0 | 0 | 0 | 0 | Inside and outside an activity cover both branches; the class has no constructor parameters to guard |
| `LoggerEnrichmentConfigurationExtensions.cs` | 90 | 85 | 0 | 0 | 0 | A real `LoggerConfiguration` runs it; guard covers the null `enrich` and the nullable `configure` |
| `ServiceCollectionExtensions.cs` | 90 | 85 | 0 | 0 | 0 | The registration test builds the provider with `ValidateOnBuild` and `ValidateScopes`; guard covers null `services` and the nullable `configure` |
| `RequestLoggingPipelineBehavior.cs` | 95 | 80 | 0 | 0 | 0 | Success, `Left`, exception and category-filter paths run with a real logger factory; the `IPipelineBehavior` contract is asserted by the core behavior list, not repeated for a satellite behavior |
| `Diagnostics/RequestLoggingLogMessages.cs` | 0 | 0 | 0 | 0 | 0 | Generated `[LoggerMessage]` code with no logic of its own; it is executed through the behavior tests |
| `EncinaOtlpSinkOptions.cs`, `EncinaOtlpSinkOptionsValidator.cs` | 90 | 70 | 0 | 0 | 0 | Defaults and each rule are unit tests; guard covers the null options argument |
| `LoggerSinkConfigurationExtensions.cs` | 85 | 85 | 0 | 0 | 80 | The mapping to the sink options is unit-tested; the in-process receiver counts for the integration flag; guard covers null `sink` and the nullable `configure` |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #182 - Serilog enrichment, request logging and OTLP log export - in four small pull requests (S1 to S4).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always; edit source only with the
  Edit/Write tools; PublicAPI, manifests, pins and changelog fragments through mechanical-fixer; README through docs-writer.
- ADR-054 settled point 2: logs carry codes and types, never EncinaError.Message; the tenant appears only when resolved (register row C3); a user id is personal data
  and is off by default. EventIds come from the 10000+ zone (base 60000): this package takes 60000-60049.

IMPLEMENTATION OVERVIEW:
1. S1: the package project, EncinaSerilogOptions, validator, EncinaLogProperties, Serilog pin, solution and test references.
2. S2: EncinaLogEnricher, TraceContextEnricher, Enrich.WithEncinaContext(), AddEncinaSerilog.
3. S3: RequestLoggingPipelineBehavior<,> and RequestLoggingLogMessages (60000-60002), the SerilogOpenTelemetry range in EventIdRanges,
   the block registry and the AssemblyRanges map.
4. S4: WriteTo.EncinaOpenTelemetry() over Serilog.Sinks.OpenTelemetry, an in-process OTLP receiver integration test, README.

KEY PATTERNS:
- A real Serilog Logger with an in-memory sink and a real RequestContext in tests; never reflection only.
- The request type is the logger category; levels are controlled by the standard category filters.
- No credentials in options classes; reject user info in endpoints; guard tests for every public method with parameters.

REFERENCE FILES:
- src/Encina/Core/RequestContextAccessor.cs, src/Encina.OpenTelemetry/Behaviors/MessagingEnricherPipelineBehavior.cs
- src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
- docs/plans/serilog-opentelemetry-implementation-plan-182.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Log events are real time and are not cacheable. |
| 2 | OpenTelemetry | ✅ Phases 2 and 4 | It adds trace and span correlation on every event and an OTLP sink helper, with no own `ActivitySource` or `Meter`. |
| 3 | Structured Logging | ✅ Phase 3 | This is the structured logging package, using EventIds 60000-60002 for the request logging behavior. |
| 4 | Health Checks | ⏭️ Follow-up issue file | `artifacts/issues/exporter-endpoint-health-checks.md` covers the sink endpoint with the probe of #1049. |
| 5 | Validation | ✅ Phases 1 and 4 | Options validators for the options of the package and of the sink helper. |
| 6 | Resilience | ❌ N/A | Batching and failure handling belong to the Serilog OpenTelemetry sink, and failures go to the Serilog `SelfLog`. |
| 7 | Distributed Locks | ❌ N/A | There is no shared state. |
| 8 | Transactions | ❌ N/A | Logging is non-transactional. |
| 9 | Idempotency | ❌ N/A | Log events are append-only. |
| 10 | Multi-Tenancy | ✅ Phase 2 | `TenantId` is added only when the request context has a resolved tenant, the same rule as the tenant tag on spans and metrics (ADR-054 D4); extending it to logs is register row C3, a Dilemma. |
| 11 | Module Isolation | ⏭️ #2442 | `ModuleId` is added by one line once `IRequestContext` carries it. |
| 12 | Audit Trail | ❌ N/A | The user id is off by default, and logs are not an audit store (`IOperationAuditStore` is). |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 accepted | On `main` | Settled point 2, D3, D4 |
| ADR-021 `10000+` zone and the block registry | On `main` | Base 60000 for observability and platform |
| `Serilog` 4.4.0 and `Serilog.Sinks.OpenTelemetry` 4.2.0 restore without an audit failure | Checked at the start of Phases 1 and 4 | New central pins |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| OTLP exporter option and `WithLogging` | #1043, #1048 | The other OTLP log route (Design Choice 4) |
| Trace fields on the request context | #186 | `TraceId` and `SpanId` could then come from the context; the enricher reads `Activity.Current` meanwhile |
| Correlation and causation | #175 | A `CausationId` property can be added when it is settled |
| `ModuleId` | #2442 | Matrix row 11 |
| PII destructuring policy | `artifacts/issues/serilog-pii-destructuring-policy.md` | Design Choice 6 follow-up |
| Endpoint reachability | #1049 and `artifacts/issues/exporter-endpoint-health-checks.md` | Matrix row 4 |
| Observability guide | #906 | Documents the logging route choice |
| Sibling plans | #180, #613, #614 | The same family; #180 shares the 60000 block |

---

## Next Steps

1. Link this plan from #182 and ask the maintainer to answer Design Choices 1 to 6 and register row C10 when work starts; Design Choices 2 (user id) and 4 (OTLP route) are the ones that change behavior for hosts.
2. Implement S1 to S4 in order. The orchestrator opens the follow-up issue files `artifacts/issues/serilog-pii-destructuring-policy.md` and `artifacts/issues/exporter-endpoint-health-checks.md`.
