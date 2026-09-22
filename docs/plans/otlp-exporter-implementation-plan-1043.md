# Implementation Plan: `Encina.OpenTelemetry` — Wire OTLP Exporter as Opt-In

> **Issue**: [#1043](https://github.com/dlrivada/Encina/issues/1043)
> **Type**: Feature (fast-follow to security bump #1041)
> **Complexity**: Medium (6 phases, ~10 production files touched, 1 package)
> **Estimated Scope**: ~150-250 lines of production code + ~250-400 lines of tests
> **Milestone**: [v0.19.0 — Observability & Resilience](https://github.com/dlrivada/Encina/milestone/30)
> **Parent EPIC**: [#888](https://github.com/dlrivada/Encina/issues/888)
> **Depends on**: [#1048](https://github.com/dlrivada/Encina/issues/1048) — Wire `WithLogging` in `Encina.OpenTelemetry.WithEncina` (prerequisite for OTLP logs)

---

## Summary

Add an opt-in OTLP exporter wiring inside `Encina.OpenTelemetry`. Today the package ships
`OpenTelemetry.Exporter.OpenTelemetryProtocol 1.15.3` as `PrivateAssets="all"` (introduced by
the security bump in [#1041](https://github.com/dlrivada/Encina/pull/1041) for transitive
version pinning), but **`WithEncina(...)` and `AddEncinaOpenTelemetry(...)` never call
`AddOtlpExporter(...)`** — the dependency is unused at the API level.

This plan adds two properties to `EncinaOpenTelemetryOptions` (`EnableOtlpExporter`,
`ConfigureOtlpExporter`) and wires OTLP for **all three OpenTelemetry signals — traces,
metrics, and logs — in a single call site**. Once wired, the `PrivateAssets="all"`
constraint is dropped from `Encina.OpenTelemetry.csproj` so the OTLP dependency flows to
consumers (now justified by the public opt-in surface). The WireMock package keeps its
private reference — it exists purely for transitive version pinning and the package is
fixtures-only.

> **Prerequisite**: [#1048](https://github.com/dlrivada/Encina/issues/1048) wires
> `WithLogging` into `WithEncina(...)`. That issue must merge **before** this plan can
> complete — without `WithLogging`, the `AddOtlpExporter` call on the logs pipeline has
> nothing to attach to. See Design Choice #3 below.

| Affected Package / Area | File Count | Notes |
|-------------------------|:---------:|-------|
| `Encina.OpenTelemetry` (source) | ~4 | Options, ServiceCollectionExtensions, csproj, PublicAPI |
| `Encina.Testing.WireMock` (csproj comment) | ~1 | Inline comment documenting the `PrivateAssets="all"` retention |
| `tests/Encina.UnitTests/OpenTelemetry/` | ~3 | Options, exporter wiring, callback invocation |
| `tests/Encina.GuardTests/Infrastructure/OpenTelemetry/` | ~1 | Null-callback guard tests |
| `tests/Encina.IntegrationTests/Observability/OpenTelemetry/` | ~1 | OTLP exporter against the existing observability docker-compose stack |
| Documentation | ~7 | Package README + CHANGELOG + new feature guide + INVENTORY enrichment + new ADR-026 + ADR index update + coverage manifest verification (see Phase 6 for the full 11-item checklist) |

**Provider Category**: Observability (`Encina.OpenTelemetry`) — the Pre-1.0 plan calls for
exporter satellites (Azure Monitor, AWS X-Ray, Prometheus, Jaeger…), but **OTLP is the
canonical, transport-neutral path** that those satellites build on. This issue does not
introduce a new package; it activates the existing OTLP dependency at the public API level.

---

## Design Choices

<details>
<summary><strong>1. Package Placement — Extend the existing <code>Encina.OpenTelemetry</code> package (no new satellite)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend existing `Encina.OpenTelemetry` package** | Single dependency for consumers, OTLP is the canonical exporter, `OpenTelemetry.Exporter.OpenTelemetryProtocol` is already a transitive dependency (1.15.3) added in #1041 | Slightly bloats the core package surface (~2 properties + ~10 lines of wiring) |
| **B) New `Encina.OpenTelemetry.Otlp` satellite** | Mirrors the per-vendor satellite pattern (`AzureMonitor`, `AwsXRay`, `Prometheus`) | OTLP is **not** a vendor — it's the standard wire format; spinning up a satellite for it is over-engineering |
| **C) Move OTLP wiring into a new `Encina.OpenTelemetry.Exporters` umbrella** | Single home for all exporters | Conflicts with the per-vendor satellite plan tracked under #888; doesn't solve the immediate "OTLP dependency is unused" problem |

### Chosen Option: **A — Extend `Encina.OpenTelemetry`**

### Rationale

- The OTLP package `OpenTelemetry.Exporter.OpenTelemetryProtocol` is **already** in
  `Encina.OpenTelemetry.csproj` as a `PrivateAssets="all"` reference for transitive pinning
  (see commit [`95bae061`](https://github.com/dlrivada/Encina/commit/95bae061) and PR
  [#1041](https://github.com/dlrivada/Encina/pull/1041)). Wiring it into `WithEncina` keeps
  the dependency *at exactly one place* in the dependency graph and removes the misleading
  "we depend on OTLP but never call it" smell that CodeRabbit flagged.
- OTLP is the canonical OpenTelemetry wire format — every collector and most APMs (Honeycomb,
  Tempo, Grafana Cloud, Datadog OTLP endpoint, New Relic OTLP endpoint, etc.) speak it. A
  per-vendor satellite is justifiable for `AzureMonitor` (proprietary SDK), `AwsXRay` (X-Ray
  semantic conventions), `Sentry`, etc., but **not for the protocol itself**.
- Drops `PrivateAssets="all"` so consumers transitively get the OTLP package when they
  reference `Encina.OpenTelemetry` — now justified by the opt-in public API.

</details>

<details>
<summary><strong>2. Options Surface Design — Single <code>EnableOtlpExporter</code> flag + single <code>ConfigureOtlpExporter</code> callback</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single flag `EnableOtlpExporter` + single callback `ConfigureOtlpExporter`** (issue spec) | Matches issue spec, one call site, OTLP options are usually identical for tracing/metrics/logs (same endpoint, same headers) | If a user wants different endpoints for traces vs metrics, they have to write the wiring themselves |
| **B) Per-signal flags `EnableOtlpTracesExporter`, `EnableOtlpMetricsExporter`, `EnableOtlpLogsExporter`** | Granular control, can disable metrics-only export | 3× the surface area, premature optimization (most users want all signals exported) |
| **C) Single flag, three callbacks (`ConfigureTracesOtlp`, `ConfigureMetricsOtlp`, `ConfigureLogsOtlp`)** | Per-signal customization | Verbose for the 95% case where one set of OTLP options is enough |
| **D) Use `Microsoft.Extensions.Options` named options pattern (`OtlpExporterOptions:Tracing`, `OtlpExporterOptions:Metrics`)** | Idiomatic .NET 10, configuration-driven | Bypasses `EncinaOpenTelemetryOptions` and pushes complexity onto the consumer's `appsettings.json` |

### Chosen Option: **A — Single flag + single callback** (issue spec)

### Rationale

- Matches the issue specification exactly. CodeRabbit's flagged comment in #1041 used this
  shape; aligning prevents drift.
- The OTLP package's `AddOtlpExporter` already supports `IConfiguration` binding via the
  underlying `OtlpExporterOptions` (`OtlpExporterOptions.Endpoint`, `Headers`, `Protocol`,
  `TimeoutMilliseconds`, `ExportProcessorType`, `BatchExportProcessorOptions`). Power users
  who need per-signal differentiation can drop down to `WithTracing(t => t.AddOtlpExporter(...))`
  / `WithMetrics(m => m.AddOtlpExporter(...))` directly — the chosen design does **not**
  prevent that.
- The OTLP exporter respects the standard `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_HEADERS`,
  and `OTEL_EXPORTER_OTLP_PROTOCOL` environment variables out of the box, so the most common
  zero-code configuration scenario already works without any callback at all.
- Pre-1.0 philosophy: ship the simplest design that satisfies the 95% case; add per-signal
  flags later if real usage demands it (no backward-compatibility cost).

### Resulting Surface

```csharp
public sealed class EncinaOpenTelemetryOptions
{
    // Existing
    public string ServiceName { get; set; } = "Encina";
    public string ServiceVersion { get; set; } = "1.0.0";
    public bool EnableMessagingEnrichers { get; set; } = true;

    // NEW
    public bool EnableOtlpExporter { get; set; }
    public Action<OtlpExporterOptions>? ConfigureOtlpExporter { get; set; }
}
```

</details>

<details>
<summary><strong>3. Signal Coverage — Wire OTLP for all three signals (traces + metrics + logs), gated on prerequisite #1048</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wire traces + metrics only** (original issue spec) | Matches what `WithEncina` already configures — `WithTracing` and `WithMetrics` exist; no `WithLogging` is wired today. Symmetric surface. | Logs OTLP is a real use case (Loki via OTLP, Datadog logs via OTLP); deferring leaves a known gap; the user explicitly chose to fill that gap. |
| **B) Wire traces + metrics + logs** | Complete coverage, single flag covers all three OTel signals; matches the OpenTelemetry vision (one config → three exports); future-proofs the API for the broader v0.19.0 observability work. | Requires `WithLogging` to be wired in `WithEncina` first — that's a separate, foundational gap currently unfilled. **Tracked as prerequisite #1048.** |
| **C) Wire traces + metrics + opt-in logs (`EnableOtlpLogExporter` separately)** | Granular control over each signal | Splits the property pair into more properties; bloats the API for what should be a one-knob feature. |

### Chosen Option: **B — Wire all three signals (traces + metrics + logs)**

### Rationale

- A consumer who turns on `EnableOtlpExporter` reasonably expects the **OTel exporter** —
  not the "OTel-traces-and-metrics-but-not-logs exporter". Option A would create a
  surprising asymmetry that a follow-up would later have to fix anyway.
- OpenTelemetry's three-signal model (traces, metrics, logs) is the canonical mental model;
  matching it in `EncinaOpenTelemetryOptions.EnableOtlpExporter` keeps the API close to the
  underlying spec.
- Per-signal callbacks (Option C) are over-engineered for the 95% case where one set of
  OTLP options serves all three signals. The single `ConfigureOtlpExporter` callback —
  invoked once per signal with a fresh `OtlpExporterOptions` — handles all common scenarios.
- The prerequisite (`WithLogging` wiring in `WithEncina`) is itself a small, foundational
  change that benefits the broader v0.19.0 observability effort beyond this issue (e.g.,
  Console log exporter, Sentry log shipping, Serilog→OTel bridge #182).

### Prerequisite Dependency

[Issue #1048](https://github.com/dlrivada/Encina/issues/1048) — *"Wire `WithLogging` in
`Encina.OpenTelemetry.WithEncina` (prerequisite for OTLP logs)"* — must merge before this
plan can complete its logs portion. The work split:

| Issue | Scope |
|-------|-------|
| **#1048** | Add unconditional `builder.WithLogging(logging => { ... })` block inside `WithEncina(...)` after the existing `WithMetrics(...)` block, with sensible defaults (`IncludeFormattedMessage = true`, `IncludeScopes = true`, `ParseStateValues = true`). No new public API. |
| **#1043 (this plan)** | Add `EnableOtlpExporter` / `ConfigureOtlpExporter` to `EncinaOpenTelemetryOptions`, wire `AddOtlpExporter` on traces, metrics, **and logs** (the logs branch reads off the `WithLogging` block from #1048). |

If #1048 ships first, this plan's Phase 2 wiring includes a third block:

```csharp
if (options.EnableOtlpExporter)
{
    builder.WithTracing(tracing => tracing.AddOtlpExporter(o => options.ConfigureOtlpExporter?.Invoke(o)));
    builder.WithMetrics(metrics => metrics.AddOtlpExporter(o => options.ConfigureOtlpExporter?.Invoke(o)));
    builder.WithLogging(logging => logging.AddOtlpExporter(o => options.ConfigureOtlpExporter?.Invoke(o)));
}
```

If #1048 is **not** merged at the time #1043 is implemented, this plan's Phase 2 falls back
to the original two-signal wiring (traces + metrics only) and the logs branch is split out
as a follow-up bound to whenever #1048 lands. That fallback should be the exception, not
the plan: the dependency is the cleaner path.

### Note on Callback Invocation

`AddOtlpExporter` for logs lives in the `OpenTelemetry.Logs` namespace (same package). The
callback signature is the same `Action<OtlpExporterOptions>`, so `ConfigureOtlpExporter` is
invoked once per signal — **3 times total** when all three branches run.

</details>

<details>
<summary><strong>4. Callback Lifecycle — Invoke <code>ConfigureOtlpExporter</code> per signal (traces, metrics, and logs each receive a separate <code>OtlpExporterOptions</code> instance)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Invoke the same delegate for each signal with separate `OtlpExporterOptions` instances** (matches `AddOtlpExporter`'s native API) | Mirrors how `AddOtlpExporter` itself works; each call gets its own options object | Delegate invoked 3× (negligible cost — runs once at startup per signal) |
| **B) Build a single `OtlpExporterOptions`, deep-clone it, hand one to each signal** | Delegate invoked once | Cloning options is fragile; `OtlpExporterOptions` has internal state and isn't designed for cloning |
| **C) Pass the same `OtlpExporterOptions` instance to all three** | Single source of truth | The OTLP exporter mutates its options object internally during construction — sharing instances across signals is unsupported by the underlying SDK |

### Chosen Option: **A — Invoke per signal**

### Rationale

- This is exactly the pattern documented by the OpenTelemetry .NET SDK:
  `tracing.AddOtlpExporter(opt => ...)`, `metrics.AddOtlpExporter(opt => ...)`, and
  `logging.AddOtlpExporter(opt => ...)` each take their own configurator.
- The cost of "invoke 3 times" is three delegate invocations at startup — pre-1.0 we always
  prefer the simple model that matches the underlying SDK over premature micro-optimization.
- Users who legitimately want to share state across the three invocations can capture a
  local in their lambda:
  `var endpoint = new Uri("https://collector:4317"); options.ConfigureOtlpExporter = o => o.Endpoint = endpoint;`.

### Wiring Sketch

```csharp
if (options.EnableOtlpExporter)
{
    builder.WithTracing(tracing =>
        tracing.AddOtlpExporter(o => options.ConfigureOtlpExporter?.Invoke(o)));
    builder.WithMetrics(metrics =>
        metrics.AddOtlpExporter(o => options.ConfigureOtlpExporter?.Invoke(o)));
    builder.WithLogging(logging =>
        logging.AddOtlpExporter(o => options.ConfigureOtlpExporter?.Invoke(o)));
}
```

> The `WithLogging` block above is added by prerequisite issue #1048 — see Design Choice #3.

</details>

<details>
<summary><strong>5. Dependency Visibility — Drop <code>PrivateAssets="all"</code> from <code>Encina.OpenTelemetry.csproj</code>, keep it on <code>Encina.Testing.WireMock.csproj</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Drop `PrivateAssets="all"` only on `Encina.OpenTelemetry`; keep on `Encina.Testing.WireMock`** | OTLP package now justified by public API surface; WireMock keeps it as a transitive-pin only (the package never *uses* OTLP, it's there to constrain the version graph) | Two divergent reference styles in the same repo — needs an inline comment |
| **B) Drop `PrivateAssets="all"` on both** | Uniform | WireMock has no public API touching OTLP; consumers reference WireMock for HTTP mocking, they'd be surprised by a transitive OTLP dependency |
| **C) Remove the OTLP `PackageReference` from `Encina.Testing.WireMock` entirely** | Smallest change | Tested in #1041 — restore breaks because the package enters transitively at 1.14.0 elsewhere in the graph and CPM doesn't pin transitives without `CentralPackageTransitivePinningEnabled` (which surfaces unrelated conflicts; out of scope) |

### Chosen Option: **A**

### Rationale

- Aligns with the issue's "Proposed Solution" exactly.
- WireMock's csproj keeps a one-line comment documenting why `PrivateAssets="all"` stays
  ("transitive version pin only; no public API consumes OTLP").
- A future issue can revisit `CentralPackageTransitivePinningEnabled` once the
  Marten/JasperFx (`Microsoft.CodeAnalysis.Common` 4.14 vs 5.0) and Aspire (`Grpc.Tools`)
  conflicts are resolved (out of scope here, tracked under the v0.19.0 milestone alongside
  the broader observability work).

</details>

<details>
<summary><strong>6. Configuration Validation — No <code>IValidateOptions</code> for this feature</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No options validator** | OTLP exporter validates its own `Endpoint`, `Headers`, `Protocol`, `TimeoutMilliseconds` at construction time; bool flags can't be invalid | None |
| **B) Add `EncinaOpenTelemetryOptionsValidator : IValidateOptions<EncinaOpenTelemetryOptions>`** | Consistent with how compliance packages validate their options | Validates pre-existing properties (`ServiceName`, `ServiceVersion`) that have never been validated and aren't part of this issue's scope |

### Chosen Option: **A — No validator**

### Rationale

- The two new properties are a bool and a nullable delegate — neither has a meaningful
  invalid state.
- OTLP-specific configuration (endpoint URL, header format, timeout > 0) is validated by the
  `OtlpExporterOptions` SDK at exporter construction. Errors surface early with clear messages.
- Adding a validator for `ServiceName`/`ServiceVersion` is a separate concern and not
  required by this issue. Pre-1.0 philosophy: don't add validation for scenarios that can't
  happen.

</details>

<details>
<summary><strong>7. Test Strategy — Unit + Guard + (optional) Integration via existing observability docker-compose</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unit + Guard, defer integration** | Fast feedback; `OtlpExporterOptions` mutation is verifiable without a real collector | No verification that spans actually land in a collector |
| **B) Unit + Guard + Integration via `.github/observability/docker-compose.observability.yml`** | The compose file already provisions an OTel Collector with OTLP gRPC (4317) and HTTP (4318) receivers — zero new infrastructure | Integration tests need the stack running (CI cost) |
| **C) Unit + Guard + Integration via Testcontainers ephemeral collector** | Self-contained; no shared compose dependency | Extra container per test run; startup cost |

### Chosen Option: **B — Unit + Guard + opt-in Integration via existing compose stack**

### Rationale

- The `.github/observability/docker-compose.observability.yml` stack is already documented
  and used by feature reference docs (`docs/features/abac/reference/observability.md`).
  Reusing it costs nothing.
- Integration tests gate on `[Trait("Category", "Integration")]` and run only when the
  stack is up — same gating as the existing `ConsoleExporterIntegrationTests`.
- Unit tests cover: `EnableOtlpExporter=false` → no exporter registered (assertion via
  introspecting the `IServiceCollection`/`TracerProviderBuilder` state),
  `EnableOtlpExporter=true` → exporter registered, `ConfigureOtlpExporter` callback invoked
  with the live `OtlpExporterOptions`, callback invoked **once per signal** (three times total:
  traces, metrics, logs).
- Guard tests cover null-callback handling (with `EnableOtlpExporter=true`, a null callback
  must not throw — it just leaves the SDK defaults).
- Integration tests cover: spans, metrics and log records actually export to the collector
  (assert via the collector's health check / received-data inspection if available, or simply
  that no exporter exception is thrown during shutdown).
- Integration tests also cover the **collector-unavailable** path: with the endpoint pointing
  at a closed port, the host starts, emits telemetry and shuts down without throwing, and the
  export failure is visible only through the SDK's self-diagnostics (see the Resilience row
  of the cross-cutting matrix for the documented behaviour).

</details>

<details>
<summary><strong>8. Naming — <code>EnableOtlpExporter</code> (singular), not <code>EnableOtlpExporters</code> (plural)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EnableOtlpExporter` (singular)** (issue spec) | Matches the underlying API name `AddOtlpExporter` (also singular), matches the issue spec | Wires *two* exporters (traces + metrics) under a "singular" flag — slight terminology stretch |
| **B) `EnableOtlpExporters` (plural)** | Acknowledges that traces + metrics each get their own exporter | Diverges from the issue spec and from the `AddOtlpExporter` SDK naming |

### Chosen Option: **A**

### Rationale

- "OTLP exporter" is treated as a configuration concept (one transport, one wire protocol),
  not a physical-instance noun. The SDK uses `AddOtlpExporter` singular for the same reason.
- Aligns with the issue spec verbatim, no debate.

</details>

---

## Implementation Phases

### Phase 1: Options Extension

> **Goal**: Add the two new properties to `EncinaOpenTelemetryOptions` with full XML doc.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** [src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs](../../src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs):
   - Add `using OpenTelemetry.Exporter;` at the top of the file.
   - Add two properties after `EnableMessagingEnrichers`:

     ```csharp
     /// <summary>
     /// Gets or sets a value indicating whether the OpenTelemetry Protocol (OTLP) exporter
     /// is registered for traces, metrics, and logs.
     /// </summary>
     /// <value>
     /// Default is <c>false</c>. When set to <c>true</c>, <see cref="WithEncina"/> calls
     /// <c>AddOtlpExporter</c> on the <see cref="OpenTelemetry.Trace.TracerProviderBuilder"/>,
     /// the <see cref="OpenTelemetry.Metrics.MeterProviderBuilder"/> and the
     /// <see cref="OpenTelemetry.Logs.LoggerProviderBuilder"/>.
     /// </value>
     /// <remarks>
     /// OTLP is the canonical wire format used by OpenTelemetry collectors, Jaeger,
     /// Tempo, Grafana Cloud, Honeycomb, Datadog (OTLP endpoint), and most modern
     /// observability backends. The exporter respects the standard environment
     /// variables <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>, <c>OTEL_EXPORTER_OTLP_HEADERS</c>,
     /// and <c>OTEL_EXPORTER_OTLP_PROTOCOL</c> when no <see cref="ConfigureOtlpExporter"/>
     /// callback is supplied.
     /// </remarks>
     /// <example>
     /// <code>
     /// services.AddOpenTelemetry()
     ///     .WithEncina(new EncinaOpenTelemetryOptions
     ///     {
     ///         EnableOtlpExporter = true,
     ///         ConfigureOtlpExporter = otlp =>
     ///         {
     ///             otlp.Endpoint = new Uri("http://localhost:4317");
     ///             otlp.Protocol = OtlpExportProtocol.Grpc;
     ///         }
     ///     });
     /// </code>
     /// </example>
     public bool EnableOtlpExporter { get; set; }

     /// <summary>
     /// Gets or sets the optional callback used to customize <see cref="OtlpExporterOptions"/>.
     /// </summary>
     /// <value>
     /// Default is <c>null</c> — the OTLP exporter uses its built-in defaults
     /// (<c>localhost:4317</c> over gRPC, no headers, batch processor). Only invoked
     /// when <see cref="EnableOtlpExporter"/> is <c>true</c>.
     /// </value>
     /// <remarks>
     /// The callback is invoked once per signal (once for traces, once for metrics, once for logs)
     /// with a fresh <see cref="OtlpExporterOptions"/> instance each time. To share
     /// configuration across signals, capture local variables in the lambda; do not
     /// store and reuse the supplied <see cref="OtlpExporterOptions"/> instance.
     /// </remarks>
     public Action<OtlpExporterOptions>? ConfigureOtlpExporter { get; set; }
     ```

2. **Update** [src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt](../../src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt):
   - Add the four new public symbols (two properties × get/set):

     ```text
     Encina.OpenTelemetry.EncinaOpenTelemetryOptions.EnableOtlpExporter.get -> bool
     Encina.OpenTelemetry.EncinaOpenTelemetryOptions.EnableOtlpExporter.set -> void
     Encina.OpenTelemetry.EncinaOpenTelemetryOptions.ConfigureOtlpExporter.get -> System.Action<OpenTelemetry.Exporter.OtlpExporterOptions!>?
     Encina.OpenTelemetry.EncinaOpenTelemetryOptions.ConfigureOtlpExporter.set -> void
     ```

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of the OTLP exporter wiring (Issue #1043).

CONTEXT:
- Encina.OpenTelemetry is at src/Encina.OpenTelemetry/.
- The package already references OpenTelemetry.Exporter.OpenTelemetryProtocol 1.15.3
  (PrivateAssets="all" — to be dropped in Phase 3).
- EncinaOpenTelemetryOptions today has 3 properties: ServiceName, ServiceVersion,
  EnableMessagingEnrichers.

TASK:
Add two new public properties to EncinaOpenTelemetryOptions:
  - EnableOtlpExporter (bool, default false)
  - ConfigureOtlpExporter (Action<OtlpExporterOptions>?, default null)

KEY RULES:
- Default for EnableOtlpExporter MUST be false (opt-in).
- ConfigureOtlpExporter is nullable — null is a valid "use SDK defaults" value.
- Add `using OpenTelemetry.Exporter;` at the top of the file.
- XML doc both properties with <summary>, <value>, <remarks>, and an <example> on
  EnableOtlpExporter showing typical Endpoint + Protocol configuration.
- Update src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt with the four
  new symbols (.get + .set for each property). The file uses the
  Microsoft.CodeAnalysis.PublicApiAnalyzers format — match the existing entries.
- Do NOT modify ServiceCollectionExtensions.cs in this phase (Phase 2).
- Do NOT touch the .csproj in this phase (Phase 3).

REFERENCE FILES:
- src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs
- src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt
- The OtlpExporterOptions API:
  https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/OtlpExporterOptions.cs
```

</details>

---

### Phase 2: Pipeline Wiring (`WithEncina` extension)

> **Goal**: Wire conditional `AddOtlpExporter` calls inside `WithEncina(...)` for traces, metrics, and logs.
>
> **Prerequisite gate**: This phase wires three branches. The third (logs) requires
> [#1048](https://github.com/dlrivada/Encina/issues/1048) to have merged. If it has not,
> see the *Fallback* note below.

<details>
<summary><strong>Tasks</strong></summary>

1. **Verify prerequisite #1048 is merged**:
   - Check that `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` already contains
     a `builder.WithLogging(...)` block inside `WithEncina(...)` (added by #1048).
   - If not, **stop and either wait for #1048 or implement the fallback** (traces + metrics
     only, with logs deferred as a follow-up issue).

2. **Modify** [src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs](../../src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs):
   - Add `using OpenTelemetry.Exporter;` and `using OpenTelemetry.Logs;` at the top.
   - Inside `WithEncina(this OpenTelemetryBuilder builder, EncinaOpenTelemetryOptions? options = null)`,
     after the `WithLogging(...)` block (added by #1048), add:

     ```csharp
     if (options.EnableOtlpExporter)
     {
         builder.WithTracing(tracing =>
             tracing.AddOtlpExporter(otlp =>
                 options.ConfigureOtlpExporter?.Invoke(otlp)));

         builder.WithMetrics(metrics =>
             metrics.AddOtlpExporter(otlp =>
                 options.ConfigureOtlpExporter?.Invoke(otlp)));

         builder.WithLogging(logging =>
             logging.AddOtlpExporter(otlp =>
                 options.ConfigureOtlpExporter?.Invoke(otlp)));
     }
     ```

3. **No changes to** `AddEncinaOpenTelemetry`:
   - That method registers DI services and instrumented decorators; it does **not** touch
     `OpenTelemetryBuilder`. OTLP exporter wiring belongs strictly to `WithEncina`.

4. **Verify** the existing `using OpenTelemetry.Trace;`, `using OpenTelemetry.Metrics;`, and
   (post-#1048) `using OpenTelemetry.Logs;` directives are present — they're needed for the
   `WithTracing` / `WithMetrics` / `WithLogging` extensions and the per-signal
   `AddOtlpExporter` overloads.

### Fallback (only if #1048 has not merged)

If the prerequisite hasn't landed, omit the `builder.WithLogging(...)` block above and ship
this issue with traces + metrics only. Then file a follow-up issue (or reopen #1043) to add
the logs branch once #1048 lands. **Prefer not to take this path** — the dependency is the
cleaner story and keeps the API consistent with the OTel three-signal model from day one.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of the OTLP exporter wiring (Issue #1043).

CONTEXT:
- Phase 1 added EnableOtlpExporter and ConfigureOtlpExporter to EncinaOpenTelemetryOptions.
- ServiceCollectionExtensions.cs has a WithEncina(OpenTelemetryBuilder, EncinaOpenTelemetryOptions?)
  method that today configures Resource, Tracing (12 sources), and Metrics ("Encina" meter
  + runtime instrumentation).

TASK:
Add conditional OTLP exporter wiring inside WithEncina, after the existing WithMetrics block.

KEY RULES:
- The new block MUST be guarded by `if (options.EnableOtlpExporter)` so default behavior
  (EnableOtlpExporter = false) is unchanged — no exporter, no extra dependency cost.
- Call AddOtlpExporter once on TracerProviderBuilder and once on MeterProviderBuilder.
  Each call MUST get its own callback lambda — do NOT share a single OtlpExporterOptions
  instance across signals.
- The lambda body MUST be `options.ConfigureOtlpExporter?.Invoke(otlp)` — null-conditional
  on the delegate so a null callback simply leaves SDK defaults (no NRE).
- Do NOT call AddOtlpLogExporter or wire WithLogging — logs are out of scope (see Design
  Choice #3).
- Do NOT modify AddEncinaOpenTelemetry. The OTLP wiring is tied to OpenTelemetryBuilder,
  not to IServiceCollection.
- Add `using OpenTelemetry.Exporter;` at the top of the file (needed for OtlpExporterOptions
  type referenced in the existing tracing/metrics usings is already there for the builder
  extensions; the using is needed because the lambda parameter `otlp` is typed via inference
  but the `AddOtlpExporter` extension lives in OpenTelemetry.Exporter).

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs (existing WithEncina method)
- The AddOtlpExporter extension method:
  https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/OtlpTraceExporterHelperExtensions.cs
```

</details>

---

### Phase 3: Project File Update — Drop `PrivateAssets="all"`

> **Goal**: Remove the `PrivateAssets="all"` constraint from the OTLP `PackageReference` in
> `Encina.OpenTelemetry.csproj` (now that the dependency is consumed via the public API).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** [src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj](../../src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj):
   - Replace line 24:

     ```xml
     <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" PrivateAssets="all" />
     ```

     with:

     ```xml
     <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
     ```

2. **Do NOT modify** [src/Encina.Testing.WireMock/Encina.Testing.WireMock.csproj](../../src/Encina.Testing.WireMock/Encina.Testing.WireMock.csproj):
   - The OTLP reference there exists purely for transitive version pinning (the package never
     consumes OTLP via public API; it's a fixtures-only test-helper package).
   - Add an inline XML comment documenting the rationale (so a future contributor doesn't
     copy the `PrivateAssets="all"` removal here):

     ```xml
     <!-- OTLP package referenced for transitive version pinning only — not consumed at API level. -->
     <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" PrivateAssets="all" />
     ```

3. **Verify** with `dotnet restore`:
   - From the repo root, run `dotnet restore Encina.slnx`.
   - The OTLP package version stays pinned at 1.15.3 (per
     [`Directory.Packages.props`](../../Directory.Packages.props)). No transitive downgrade warnings.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of the OTLP exporter wiring (Issue #1043).

CONTEXT:
- Phases 1 & 2 added the public API and wired AddOtlpExporter into WithEncina.
- The OTLP package (OpenTelemetry.Exporter.OpenTelemetryProtocol 1.15.3) is referenced
  in src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj as PrivateAssets="all" — added
  in PR #1041 for transitive version pinning. Now justified by the public API surface,
  the constraint should be dropped so consumers transitively get the OTLP package.

TASK:
1. Remove PrivateAssets="all" from the OTLP PackageReference in Encina.OpenTelemetry.csproj.
2. KEEP PrivateAssets="all" on the OTLP PackageReference in Encina.Testing.WireMock.csproj
   (it's a transitive-pin only — no public API consumes OTLP).
3. Add a one-line XML comment above the WireMock OTLP reference documenting why it stays
   private.

KEY RULES:
- Do NOT touch any other PackageReference in the file — leave OpenTelemetry,
  OpenTelemetry.Api, OpenTelemetry.Exporter.Console, OpenTelemetry.Extensions.Hosting,
  OpenTelemetry.Instrumentation.Runtime, etc., exactly as they are.
- Do NOT change Directory.Packages.props — the version (1.15.3) is already pinned there.
- After the change, run `dotnet restore Encina.slnx` and verify no NU1605 (downgrade)
  warnings appear.

REFERENCE FILES:
- src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj (line 24)
- src/Encina.Testing.WireMock/Encina.Testing.WireMock.csproj (line 30)
- Directory.Packages.props (OTLP version pinning)
- PR #1041 commit history for context on why PrivateAssets="all" was originally added
```

</details>

---

### Phase 4: Cross-Cutting Integration

> **Goal**: Apply the cross-cutting integration evaluation. For this feature, all 12
> transversal functions are either ✅ **already covered** (OpenTelemetry — this *is* OTel
> wiring) or ❌ **N/A** (configuration-only, stateless, telemetry-not-domain). No additional
> integrations are required in this phase — see the Cross-Cutting Integration Matrix at the
> bottom of this document.

<details>
<summary><strong>Tasks</strong></summary>

1. **Verify cross-cutting integrations** (no new code; checklist only):
   - **OpenTelemetry**: ✅ The feature itself is OTel wiring. The existing
     `ActivitySource("Encina")`, runtime instrumentation, and 12 tracing sources continue
     to work and are now exportable via OTLP for all three signals.
   - **Structured Logging**: ✅ Via prerequisite #1048. With `WithLogging` wired in
     `WithEncina`, all `[LoggerMessage]`-generated events in the Encina codebase are
     visible to the OTel logger provider and exported through the OTLP logs branch when
     `EnableOtlpExporter = true`. This is the indirect-but-correct integration: this
     issue's wiring depends on #1048, and #1048 is the one that physically attaches
     Encina structured logs to the OTel pipeline.
   - **Health Checks**: ⏭️ Deferred to a follow-up issue. An OTLP collector reachability
     health check (TCP probe to the configured endpoint, with a configurable timeout) would
     be valuable but is out of scope for this issue. If accepted as a follow-up, it would
     live in `Encina.OpenTelemetry/Health/OtlpExporterHealthCheck.cs` and follow the same
     `IEncinaHealthCheck` pattern as `SchemaDriftHealthCheck` /
     [`ReshardingHealthCheck`](../../src/Encina.OpenTelemetry/Resharding/ReshardingHealthCheck.cs).
   - **Resilience**: ❌ N/A (documented limitation) — `OtlpExporterOptions` exposes
     `TimeoutMilliseconds`; `BatchExportProcessorOptions` controls batching only (queue size,
     delay, batch size), not retries. The SDK has no circuit breaker; retry exists only as the
     experimental `OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY` switch, which this plan does not enable.
     A failed export drops the batch and is visible only through SDK self-diagnostics. The
     exporter is not wrapped in Polly because the SDK owns the export pipeline; the
     collector-unavailable integration test pins this behaviour.
   - **Caching, Validation, Distributed Locks, Transactions, Idempotency, Multi-Tenancy,
     Module Isolation, Audit Trail**: ❌ All N/A — exporter wiring is configuration-only and
     does not touch domain data, request handling, or tenancy boundaries. Tenant context,
     when present, already flows through `Activity.Current` tags via the existing
     `TenancyActivityEnricher` and is exported transparently.

2. **No follow-up issues created in this phase** — the only deferral (Health Checks) is
   documented here and in the Cross-Cutting Integration Matrix. The Structured Logging
   integration is covered by the prerequisite (#1048). If a collector-reachability health
   check is desired, a separate issue can be filed in the v0.19.0 milestone.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of the OTLP exporter wiring (Issue #1043).

CONTEXT:
- Phases 1-3 added the public API, wired the exporter, and updated the .csproj.
- Cross-cutting integration analysis is required for every Encina feature per
  CLAUDE.md "Cross-Cutting Integration Rule" — but for THIS feature, all 12 transversal
  functions are either already covered (OpenTelemetry, which IS this feature) or N/A
  (configuration-only, stateless, no domain data).

TASK:
- No code changes in this phase.
- Verify the Cross-Cutting Integration Matrix at the bottom of the plan accurately
  reflects the status of each function for this feature.
- If, during implementation, you discover a cross-cutting concern that was missed
  (e.g., the OTLP exporter needs a circuit breaker because it's hammering a dead
  collector), STOP and create a GitHub issue rather than expanding scope.

KEY RULES:
- Do NOT add a health check, structured logging, resilience wrapper, or any other
  cross-cutting integration in this phase. Those are deferred and should be tracked
  as separate follow-up issues if desired.
- Do NOT introduce a new EventId range for this feature — there is no [LoggerMessage]
  to register.

REFERENCE FILES:
- CLAUDE.md "Cross-Cutting Integration Rule" section (the 12 transversal functions)
- The Cross-Cutting Integration Matrix at the bottom of this plan
```

</details>

---

### Phase 5: Testing — Unit + Guard + Integration

> **Goal**: Verify the new behavior with three layers of tests.

<details>
<summary><strong>Tasks</strong></summary>

#### 5a. Unit Tests (`tests/Encina.UnitTests/OpenTelemetry/`)

1. **Create** `EncinaOpenTelemetryOptionsOtlpTests.cs` (mirror the
   [`EncinaOpenTelemetryOptionsEnableMessagingTests.cs`](../../tests/Encina.UnitTests/OpenTelemetry/EncinaOpenTelemetryOptionsEnableMessagingTests.cs)
   pattern):

   ```csharp
   public sealed class EncinaOpenTelemetryOptionsOtlpTests
   {
       [Fact]
       public void EnableOtlpExporter_DefaultsToFalse() { /* ... */ }

       [Fact]
       public void EnableOtlpExporter_CanBeSetToTrue() { /* ... */ }

       [Fact]
       public void ConfigureOtlpExporter_DefaultsToNull() { /* ... */ }

       [Fact]
       public void ConfigureOtlpExporter_CanBeAssigned()
       {
           Action<OtlpExporterOptions> callback = _ => { };
           var options = new EncinaOpenTelemetryOptions { ConfigureOtlpExporter = callback };
           options.ConfigureOtlpExporter.ShouldBeSameAs(callback);
       }
   }
   ```

2. **Create** `WithEncinaOtlpExporterTests.cs`:

   ```csharp
   public sealed class WithEncinaOtlpExporterTests
   {
       [Fact]
       public void WithEncina_EnableOtlpFalse_DoesNotRegisterOtlpExporter()
       {
           var services = new ServiceCollection();
           services.AddOpenTelemetry()
               .WithEncina(new EncinaOpenTelemetryOptions { EnableOtlpExporter = false });

           // Assert: build provider, query no OtlpTraceExporter / OtlpMetricExporter
           //         is registered. The internals expose this via the BatchExportProcessor.
           //         A simpler proxy: ensure the IServiceCollection does NOT contain a
           //         descriptor whose ImplementationType.FullName starts with
           //         "OpenTelemetry.Exporter.OpenTelemetryProtocol".
       }

       [Fact]
       public void WithEncina_EnableOtlpTrue_RegistersOtlpExporterForAllSignals()
       {
           var services = new ServiceCollection();
           services.AddOpenTelemetry()
               .WithEncina(new EncinaOpenTelemetryOptions { EnableOtlpExporter = true });

           // Assert: descriptors registering the OTLP exporter exist for tracing,
           //         metrics, AND logs (logs branch requires prerequisite #1048).
       }

       [Fact]
       public void WithEncina_EnableOtlpTrue_InvokesCallbackPerSignal()
       {
           var invocations = 0;
           var lastEndpoint = (Uri?)null;

           var services = new ServiceCollection();
           services.AddOpenTelemetry()
               .WithEncina(new EncinaOpenTelemetryOptions
               {
                   EnableOtlpExporter = true,
                   ConfigureOtlpExporter = otlp =>
                   {
                       invocations++;
                       otlp.Endpoint = new Uri("http://my-collector:4317");
                       lastEndpoint = otlp.Endpoint;
                   }
               });

           // Build the provider so the OpenTelemetrySdk constructs the exporters and
           // the lambda fires once per signal (3 total: traces + metrics + logs):
           using var sp = services.BuildServiceProvider();
           sp.GetRequiredService<TracerProvider>();
           sp.GetRequiredService<MeterProvider>();
           // For logs, resolving an ILogger<T> through the host triggers the OTel
           // logger provider construction. Use LoggerFactory directly:
           sp.GetRequiredService<ILoggerFactory>();

           invocations.ShouldBe(3); // traces + metrics + logs
           lastEndpoint.ShouldBe(new Uri("http://my-collector:4317"));
       }

       [Fact]
       public void WithEncina_EnableOtlpTrueWithNullCallback_DoesNotThrow()
       {
           var services = new ServiceCollection();
           services.AddOpenTelemetry()
               .WithEncina(new EncinaOpenTelemetryOptions
               {
                   EnableOtlpExporter = true,
                   ConfigureOtlpExporter = null
               });

           Should.NotThrow(() =>
           {
               using var sp = services.BuildServiceProvider();
               sp.GetRequiredService<TracerProvider>();
               sp.GetRequiredService<MeterProvider>();
               sp.GetRequiredService<ILoggerFactory>();
           });
       }
   }
   ```

3. **Update** `EncinaOpenTelemetryOptionsTests.cs` defaults assertion to include the two new
   properties:

   ```csharp
   options.EnableOtlpExporter.ShouldBeFalse();
   options.ConfigureOtlpExporter.ShouldBeNull();
   ```

**Target**: ~6-8 unit tests covering options defaults, callback wiring, per-signal
invocation, and null-callback safety.

#### 5b. Guard Tests (`tests/Encina.GuardTests/Infrastructure/OpenTelemetry/`)

1. **Update** [`ServiceCollectionExtensionsGuardTests.cs`](../../tests/Encina.GuardTests/Infrastructure/OpenTelemetry/ServiceCollectionExtensionsGuardTests.cs)
   with two new tests:

   ```csharp
   [Fact]
   public void WithEncina_EnableOtlpTrue_NullCallback_DoesNotThrow()
   {
       var services = new ServiceCollection();
       var act = () => services.AddOpenTelemetry()
           .WithEncina(new EncinaOpenTelemetryOptions
           {
               EnableOtlpExporter = true,
               ConfigureOtlpExporter = null
           });

       Should.NotThrow(act);
   }

   [Fact]
   public void WithEncina_EnableOtlpFalse_NullCallback_DoesNotInvokeCallback()
   {
       var invoked = false;
       var services = new ServiceCollection();
       services.AddOpenTelemetry()
           .WithEncina(new EncinaOpenTelemetryOptions
           {
               EnableOtlpExporter = false,
               ConfigureOtlpExporter = _ => invoked = true
           });

       using var sp = services.BuildServiceProvider();
       sp.GetRequiredService<TracerProvider>();
       sp.GetRequiredService<MeterProvider>();

       invoked.ShouldBeFalse();
   }
   ```

**Target**: ~2 guard tests (the rest of the guard surface — null builder, null services —
is already covered by existing `ServiceCollectionExtensionsGuardTests`).

#### 5c. Integration Tests (`tests/Encina.IntegrationTests/Observability/OpenTelemetry/`)

1. **Create** `OtlpExporterIntegrationTests.cs` (mirror the
   [`ConsoleExporterIntegrationTests.cs`](../../tests/Encina.IntegrationTests/Observability/OpenTelemetry/ConsoleExporterIntegrationTests.cs)
   pattern, gated by `[Trait("Category", "Integration")]` and `[Trait("Component", "OpenTelemetry")]`):

   ```csharp
   [Trait("Category", "Integration")]
   [Trait("Component", "OpenTelemetry")]
   public sealed class OtlpExporterIntegrationTests
   {
       // Endpoint provisioned by .github/observability/docker-compose.observability.yml
       private const string CollectorOtlpGrpcEndpoint = "http://localhost:4317";

       [Fact]
       public async Task Send_Request_With_OtlpExporter_Should_Not_Throw_During_Shutdown()
       {
           // Arrange
           var services = new ServiceCollection();
           services.AddEncina(config => { });
           services.AddSingleton<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();

           services.AddOpenTelemetry()
               .WithEncina(new EncinaOpenTelemetryOptions
               {
                   ServiceName = "Encina.IntegrationTests",
                   ServiceVersion = "0.13.0-dev",
                   EnableOtlpExporter = true,
                   ConfigureOtlpExporter = otlp =>
                   {
                       otlp.Endpoint = new Uri(CollectorOtlpGrpcEndpoint);
                       otlp.Protocol = OtlpExportProtocol.Grpc;
                       otlp.TimeoutMilliseconds = 2_000;
                   }
               });

           var sp = services.BuildServiceProvider();
           var encina = sp.GetRequiredService<IEncina>();

           // Act
           var result = await encina.Send(new TestRequest { Value = 7 }, CancellationToken.None);

           // Assert: the request succeeds, and shutdown of the exporter (during
           // service-provider disposal) does not throw.
           result.ShouldBeSuccess();
           Should.NotThrow(() => sp.Dispose());
       }

       [Fact]
       public async Task Metrics_Pipeline_With_OtlpExporter_Should_Not_Throw_During_Shutdown()
       {
           // Arrange — configure metrics exporter via OTLP, generate one Encina request
           // to record the request.duration histogram, then dispose to trigger flush.
           // ...
       }

       [Fact]
       public async Task Logs_Pipeline_With_OtlpExporter_Should_Not_Throw_During_Shutdown()
       {
           // Arrange — configure logs exporter via OTLP, emit one ILogger<T>.LogInformation
           // call from a request handler, then dispose to trigger flush.
           // (Requires prerequisite #1048 — WithLogging wired in WithEncina.)
       }

       // ... TestRequest / TestResponse / TestRequestHandler exactly as in the Console test
   }
   ```

   - The existing observability stack is started with
     `docker compose -f .github/observability/docker-compose.observability.yml up -d`.
     Tests are skipped when the stack is not reachable (use a quick TCP probe in
     `IAsyncLifetime.InitializeAsync` and `Skip.IfNot(...)`).

**Target**: ~2-3 integration tests, gated on the observability stack being reachable.

#### 5d. Test Justification — No Property / Contract / Load / Benchmark

| Test Type | Required? | Justification |
|-----------|:---------:|--------------|
| **PropertyTests** | ❌ | No invariants beyond "callback invoked iff enabled" — already covered by unit tests. No domain logic to property-test. |
| **ContractTests** | ❌ | Single feature in a single package, no multi-provider implementations to verify against a shared contract. |
| **LoadTests** | ❌ | The wiring code runs once at startup. The OTLP exporter itself has its own load characteristics governed by the SDK; load-testing it is the SDK's responsibility, not Encina's. |
| **BenchmarkTests** | ❌ | One bool check + (optionally) two delegate invocations during DI build. Not a hot path. The exporter's runtime cost is the SDK's domain. |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of the OTLP exporter wiring (Issue #1043).

CONTEXT:
- Phases 1-4 implemented the public API, wired the exporter, updated the .csproj, and
  documented cross-cutting evaluations.
- Test infrastructure: Unit tests under tests/Encina.UnitTests/OpenTelemetry/, guard
  tests under tests/Encina.GuardTests/Infrastructure/OpenTelemetry/, integration tests
  under tests/Encina.IntegrationTests/Observability/OpenTelemetry/.
- Existing patterns: Shouldly assertions, xunit v3, sealed test classes, AAA-named
  methods (Method_Scenario_ExpectedResult).
- Integration test stack: .github/observability/docker-compose.observability.yml
  exposes OTLP gRPC on localhost:4317 and OTLP HTTP on localhost:4318.

TASK:
1. Create tests/Encina.UnitTests/OpenTelemetry/EncinaOpenTelemetryOptionsOtlpTests.cs
   with tests for the new properties' defaults and assignment.
2. Create tests/Encina.UnitTests/OpenTelemetry/WithEncinaOtlpExporterTests.cs with
   tests for: EnableOtlpExporter=false → no exporter, =true → exporter registered for
   traces+metrics+logs, callback invoked once per signal (three times total), null-callback safety.
3. Update tests/Encina.UnitTests/OpenTelemetry/EncinaOpenTelemetryOptionsTests.cs
   default-values test to assert EnableOtlpExporter == false and ConfigureOtlpExporter == null.
4. Add two new test methods to
   tests/Encina.GuardTests/Infrastructure/OpenTelemetry/ServiceCollectionExtensionsGuardTests.cs
   for null-callback handling and ensuring the callback is NOT invoked when
   EnableOtlpExporter is false.
5. Create tests/Encina.IntegrationTests/Observability/OpenTelemetry/OtlpExporterIntegrationTests.cs
   that hits localhost:4317 (gRPC OTLP) on the existing observability stack. Cover all
   three signals (traces, metrics, logs) with one test method each. Tests should be
   marked [Trait("Category", "Integration")] and skip cleanly if the collector is not
   reachable (use a TCP probe in InitializeAsync).

KEY RULES:
- All test classes are `public sealed class`.
- Test methods follow Method_Scenario_ExpectedResult naming.
- Use `Should.NotThrow(() => ...)` for negative assertions.
- For "exporter registered" assertions, the simplest reliable signal is "the callback
  was invoked exactly twice (once per signal) when the SDK builds the providers via
  GetRequiredService<TracerProvider>() and GetRequiredService<MeterProvider>()".
  Avoid reflecting into SDK internals.
- The integration test MUST gracefully skip (not fail) when the OTel collector is
  not reachable. Use a TCP-connect probe with a 500ms timeout in InitializeAsync,
  and call `Skip.IfNot(reachable, "OTel collector not running on localhost:4317")`.
- Do NOT mock the OpenTelemetry SDK in unit tests — use the real SDK with real
  TracerProvider and MeterProvider; the surface area is small and the SDK is fast.

REFERENCE FILES:
- tests/Encina.UnitTests/OpenTelemetry/EncinaOpenTelemetryOptionsTests.cs
- tests/Encina.UnitTests/OpenTelemetry/EncinaOpenTelemetryOptionsEnableMessagingTests.cs
- tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs
- tests/Encina.GuardTests/Infrastructure/OpenTelemetry/ServiceCollectionExtensionsGuardTests.cs
- tests/Encina.IntegrationTests/Observability/OpenTelemetry/ConsoleExporterIntegrationTests.cs
- .github/observability/docker-compose.observability.yml (collector setup)
```

</details>

---

### Phase 6: Documentation & Finalization

> **Goal**: Comprehensive documentation pass against the **11 mandatory documentation items**
> defined by Encina's plan-template instructions, plus build and test verification.

> **Mandatory documentation checklist** (one section per item):

<details>
<summary><strong>Tasks</strong></summary>

#### 1. XML doc comments on all new public APIs ✅ Required

Verify that every public symbol introduced by Phases 1-3 has full XML documentation:

| Symbol | Required tags |
|--------|---------------|
| `EncinaOpenTelemetryOptions.EnableOtlpExporter` | `<summary>`, `<value>`, `<remarks>`, `<example>` (already mandated by Phase 1) |
| `EncinaOpenTelemetryOptions.ConfigureOtlpExporter` | `<summary>`, `<value>`, `<remarks>` (already mandated by Phase 1) |
| (No new methods, types, or interfaces in this issue — only properties) | — |

**Action**:

- Run `dotnet build Encina.slnx --configuration Release` — `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is enabled in the package, so missing XML docs surface as **CS1591** warnings. Zero CS1591 warnings == complete coverage.
- Spot-check the rendered IntelliSense for both new properties in a sample consumer project.

#### 2. CHANGELOG.md ✅ Required

Update [CHANGELOG.md](../../CHANGELOG.md) under `## [Unreleased]` → `### Added`:

```markdown
#### Encina.OpenTelemetry — OTLP Exporter Opt-In Wiring (#1043)

- **`EncinaOpenTelemetryOptions.EnableOtlpExporter`** (default `false`): when set to
  `true`, `WithEncina(...)` registers `AddOtlpExporter` on the tracing, metrics, AND
  logs pipelines (logs branch enabled by prerequisite #1048).
- **`EncinaOpenTelemetryOptions.ConfigureOtlpExporter`** (`Action<OtlpExporterOptions>?`):
  optional callback invoked once per signal with a fresh `OtlpExporterOptions` instance
  (3 invocations total — traces + metrics + logs), allowing per-signal customization of
  endpoint, protocol, headers, and batch settings.
- **Dependency visibility**: `Encina.OpenTelemetry.csproj` drops `PrivateAssets="all"`
  from `OpenTelemetry.Exporter.OpenTelemetryProtocol` (now justified by the public
  opt-in API). As a consequence `OpenTelemetry.Exporter.OpenTelemetryProtocol` becomes a
  transitive dependency of every consumer of `Encina.OpenTelemetry`, whether or not they
  opt in; this is required because `WithEncina` must be able to call `AddOtlpExporter`.
  `Encina.Testing.WireMock.csproj` keeps `PrivateAssets="all"` — the reference there
  exists purely for transitive version pinning.
- Default behavior is unchanged at runtime: `EnableOtlpExporter = false` means no OTLP
  exporter is registered and nothing is sent anywhere. The opt-in is a runtime switch, not
  a packaging boundary.
```

> **Design note (dependency surface).** `PrivateAssets="all"` keeps a package out of the
> consumer's dependency graph; removing it is the only way to let `Encina.OpenTelemetry`
> reference `AddOtlpExporter` at runtime without a separate package. The alternative — a new
> `Encina.OpenTelemetry.Otlp` satellite package that owns the reference and the two options —
> keeps the core package's graph unchanged but adds a package to maintain and document. The
> implementer must pick one before Phase 1 and record it in ADR-026; the acceptance criteria
> below describe the runtime contract and must not promise "no extra package surface" if the
> in-package option is chosen.

#### 3. ROADMAP.md ⏭️ Verify, no update expected

- **Audit**: open [`ROADMAP.md`](../../ROADMAP.md) lines 298-310 (the v0.19.0 — Observability & Resilience section).
- **Verdict**: the v0.19.0 description lists "OpenTelemetry integration", "Metrics & Tracing", "Circuit Breaker patterns" at a feature-area level. OTLP wiring is **already implicitly covered** under "OpenTelemetry integration" — no per-issue enumeration in this section. **No update needed**.
- **Document the verification** in the PR description ("Verified ROADMAP.md v0.19.0 entry — no update required, OTLP is implicit under OpenTelemetry integration").

#### 4. Package README.md ✅ Required

Update [src/Encina.OpenTelemetry/README.md](../../src/Encina.OpenTelemetry/README.md):

- **Configuration Options → EncinaOpenTelemetryOptions** table — add two rows:

  | Property | Type | Default | Description |
  |----------|------|---------|-------------|
  | `EnableOtlpExporter` | `bool` | `false` | When `true`, registers the OTLP exporter for **traces, metrics, and logs** |
  | `ConfigureOtlpExporter` | `Action<OtlpExporterOptions>?` | `null` | Optional callback to customize endpoint, protocol, headers, batch settings. Invoked once per signal (3 times total). |

- **What Gets Instrumented?** section — add a new "Logs" subsection alongside "Tracing" and "Metrics" (or coordinate with #1048 README update if it lands first).

- **Integration with Observability Platforms** section — add a new "OTLP (OpenTelemetry Protocol)" subsection between Jaeger and Prometheus:

  ````markdown
  ### OTLP (OpenTelemetry Protocol)

  OTLP is the canonical wire format for OpenTelemetry collectors. Use it to ship
  traces, metrics, and logs to any OTLP-compatible backend (OTel Collector, Jaeger,
  Tempo, Loki, Honeycomb, Grafana Cloud, Datadog, New Relic, ...).

  ```csharp
  builder.Services.AddOpenTelemetry()
      .WithEncina(new EncinaOpenTelemetryOptions
      {
          ServiceName = "MyApp",
          EnableOtlpExporter = true,
          ConfigureOtlpExporter = otlp =>
          {
              otlp.Endpoint = new Uri("http://otel-collector:4317");
              otlp.Protocol = OtlpExportProtocol.Grpc;
              otlp.Headers = "x-honeycomb-team=YOUR_API_KEY";
          }
      });
  ```

  The exporter respects the standard `OTEL_EXPORTER_OTLP_ENDPOINT`,
  `OTEL_EXPORTER_OTLP_HEADERS`, and `OTEL_EXPORTER_OTLP_PROTOCOL` environment
  variables when no `ConfigureOtlpExporter` callback is supplied.
  ````

#### 5. docs/features/*.md ✅ Required

Create a new feature page: `docs/features/opentelemetry-otlp-exporter.md` (to be created).

Suggested structure (mirror existing observability-flavored pages such as `docs/features/cdc.md`):

```markdown
# OTLP Exporter (Encina.OpenTelemetry)

> **Issue**: #1043 · **Prerequisite**: #1048 (`WithLogging` wiring) · **Milestone**: v0.19.0

## Overview
Brief description of OTLP and why opt-in matters.

## Configuration
- The `EnableOtlpExporter` flag
- The `ConfigureOtlpExporter` callback (per-signal invocation semantics)
- Environment variable fallback (`OTEL_EXPORTER_OTLP_ENDPOINT`, etc.)

## Quick Start
- Local OTel Collector via `.github/observability/docker-compose.observability.yml`
- Honeycomb / Grafana Cloud / Datadog OTLP endpoint examples

## Three Signals
- Traces (uses `Encina` + 12 ActivitySources already configured by `WithEncina`)
- Metrics (uses `Encina` Meter + runtime instrumentation)
- Logs (requires #1048; flows all `[LoggerMessage]` events)

## Health Check
Reference to follow-up #1049 (collector reachability probe).

## Troubleshooting
- "No spans appear" → check `OTEL_EXPORTER_OTLP_PROTOCOL`, gRPC vs HTTP, port 4317 vs 4318
- "Headers not applied" → callback invoked per signal, capture endpoint/headers in lambda closure
- "WireMock package transitively pulls OTLP" → expected; pinning concern, see #1051

## Related
- Package README: src/Encina.OpenTelemetry/README.md
- Prerequisite: #1048
- Health check follow-up: #1049
- ADR: docs/architecture/adr/026-otlp-exporter-opt-in.md
```

Cross-link from `src/Encina.OpenTelemetry/README.md` → `docs/features/opentelemetry-otlp-exporter.md` for deep-dive content.

#### 6. docs/INVENTORY.md ✅ Required (small enrichment)

Update [`docs/INVENTORY.md`](../../docs/INVENTORY.md):

- **Line 2960**: enrich the `Encina.OpenTelemetry` row from `"Trazas y métricas | ✅ Completo"` to `"Trazas, métricas y logs (OTLP opt-in via #1043, logs vía #1048) | ✅ Completo"`.
- **Optional**: if a new "OTLP Exporter" sub-feature row exists at the package-feature granularity, add it; otherwise the row enrichment above is sufficient (the change is opt-in, additive, and doesn't introduce new packages or modules).

#### 7. docs/architecture/adr/*.md ✅ Required (new ADR-026)

Create `docs/architecture/adr/026-otlp-exporter-opt-in.md` (to be created). The latest existing ADR is [`025-performance-measurement-infrastructure.md`](../../docs/architecture/adr/025-performance-measurement-infrastructure.md), so this becomes ADR-026.

Why an ADR is justified despite the small code footprint:

| Decision | Why future contributors need it |
|----------|---------------------------------|
| OTLP is wired in the existing `Encina.OpenTelemetry` package, **not** as a per-vendor satellite | Sets the boundary for the satellite-package pattern (vendor-specific = satellite, protocol-level = core). Future "should we add `Encina.OpenTelemetry.X` for protocol Y?" questions reference this. |
| **One** `EnableOtlpExporter` flag covers all three OTel signals (traces + metrics + logs) | Establishes the "one knob per protocol, not per signal" convention. Influences how Sentry/Datadog/Azure Monitor satellites should expose their flags. |
| The `ConfigureOtlpExporter` callback is invoked **per signal** with a fresh `OtlpExporterOptions` each time | Prevents future contributors from "optimizing" by sharing/cloning options instances — which the SDK explicitly doesn't support. Documents the rationale ("AddOtlpExporter mutates the options object internally"). |
| `Encina.OpenTelemetry.csproj` drops `PrivateAssets="all"`; `Encina.Testing.WireMock.csproj` keeps it | Documents why two packages with the same dependency have different visibility settings — non-obvious and easy to "fix" wrongly later. References #1051 (CPM transitive-pinning spike) as the path forward. |

Suggested ADR sections: **Status** (Accepted), **Context** (existing OTLP package was unused at API level after #1041), **Decision** (the four choices above), **Consequences** (positive: clean opt-in, justified dependency, three-signal symmetry; negative: WireMock divergence stays until #1051 resolves), **References** (#1041, #1043, #1048, #1049, #1050, #1051).

Update [`docs/architecture/adr/index.md`](../../docs/architecture/adr/index.md) with the new entry.

#### 8. PublicAPI.Shipped.txt / PublicAPI.Unshipped.txt ✅ Required (Unshipped only)

- **PublicAPI.Unshipped.txt**: add the four new symbols (already mandated by Phase 1):

  ```text
  Encina.OpenTelemetry.EncinaOpenTelemetryOptions.EnableOtlpExporter.get -> bool
  Encina.OpenTelemetry.EncinaOpenTelemetryOptions.EnableOtlpExporter.set -> void
  Encina.OpenTelemetry.EncinaOpenTelemetryOptions.ConfigureOtlpExporter.get -> System.Action<OpenTelemetry.Exporter.OtlpExporterOptions!>?
  Encina.OpenTelemetry.EncinaOpenTelemetryOptions.ConfigureOtlpExporter.set -> void
  ```

- **PublicAPI.Shipped.txt**: **no change** — symbols only move from Unshipped to Shipped on actual release (handled by the release workflow, not this PR).

- **Verify** with `dotnet build Encina.slnx --configuration Release`: zero **RS0016** (Symbol not in declared API) and zero **RS0017** (Symbol in declared API but not public/found) warnings.

#### 9. docs/releases/vX.Y.Z/ ⏭️ Conditional

The current in-progress release folder is [`docs/releases/v0.13.0/`](../../docs/releases/v0.13.0/). This issue targets **v0.19.0** — that release folder does not yet exist (the latest tracked is v0.13.0).

**Two paths**:

- **Path A (preferred)**: when v0.19.0 starts assembly (per the EPIC #888 timeline), create `docs/releases/v0.19.0/README.md` (to be created) with an entry for OTLP wiring at that time. The CHANGELOG `[Unreleased]` entry from item #2 above gets moved into the release notes when the version is cut. **This is the standard release flow** — release notes are not authored issue-by-issue, they're aggregated at version-cut time.
- **Path B (only if release folder already exists at PR time)**: append an "Encina.OpenTelemetry — OTLP Exporter Opt-In (#1043)" sub-section to the existing v0.19.0/README.md, mirroring the v0.13.0 structure.

**Action for this PR**: ensure the CHANGELOG entry from item #2 is complete and self-contained — it serves as the single source of truth until release notes are aggregated.

#### 10. Build verification ✅ Required (zero warnings)

```bash
# Restore — zero NU* warnings
dotnet restore Encina.slnx

# Build — zero errors, zero warnings (strict)
dotnet build Encina.slnx --configuration Release /p:TreatWarningsAsErrors=true

# Specifically verify:
#   - 0 CS1591 (missing XML doc) — confirms item #1
#   - 0 RS0016 / RS0017 (PublicAPI mismatch) — confirms item #8
#   - 0 NU1605 (downgrade) — confirms Phase 3 csproj change is clean
```

#### 11. Test verification ✅ Required (≥85% line coverage)

```bash
# Unit + Guard tests — all green
dotnet test tests/Encina.UnitTests/Encina.UnitTests.csproj \
  --filter "FullyQualifiedName~OpenTelemetry" \
  --configuration Release

dotnet test tests/Encina.GuardTests/Encina.GuardTests.csproj \
  --filter "FullyQualifiedName~OpenTelemetry" \
  --configuration Release

# Integration tests — requires the observability stack
docker compose -f .github/observability/docker-compose.observability.yml up -d
dotnet test tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj \
  --filter "FullyQualifiedName~OtlpExporter" \
  --configuration Release

# Coverage — verify ≥85% line coverage on the new wiring
dotnet test --collect "XPlat Code Coverage" \
  --results-directory artifacts/coverage \
  --filter "FullyQualifiedName~OpenTelemetry"
```

Coverage targets per [CLAUDE.md Testing Standards](../../CLAUDE.md#coverage-targets):

| Metric | Target |
|--------|--------|
| Line coverage | **≥85%** for new code in `EncinaOpenTelemetryOptions.cs` and the `WithEncina` OTLP block |
| Branch coverage | ≥80% (the `if (options.EnableOtlpExporter)` branch + the three `?.Invoke(otlp)` null-callback branches) |
| Method coverage | ≥90% (all four new accessors + the wiring block) |

Per-flag coverage manifest (`.github/coverage-manifest/Encina.OpenTelemetry.json`) — verify the new file lines are accounted for under the `unit` and `guard` flags. The `integration` flag is optional and gated on the docker-compose stack.

#### PR description checklist (final)

- [ ] Prerequisite #1048 merged (logs branch enabled)
- [ ] XML doc on both new properties (item #1)
- [ ] CHANGELOG entry under `[Unreleased]` (item #2)
- [ ] ROADMAP.md verified — no update needed (item #3)
- [ ] Package README updated (item #4)
- [ ] `docs/features/opentelemetry-otlp-exporter.md` created (item #5)
- [ ] `docs/INVENTORY.md` row enriched (item #6)
- [ ] ADR-026 created and linked from `index.md` (item #7)
- [ ] `PublicAPI.Unshipped.txt` updated with 4 new symbols (item #8)
- [ ] `docs/releases/v0.19.0/` deferred to release-cut time (item #9)
- [ ] `dotnet build` clean — zero warnings, including CS1591/RS00xx/NU1605 (item #10)
- [ ] `dotnet test` green; coverage ≥85% on new lines (item #11)
- [ ] PR title: `feat(opentelemetry): wire OTLP exporter as opt-in for traces, metrics, and logs`
- [ ] PR body references `Fixes #1043`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of the OTLP exporter wiring (Issue #1043).

CONTEXT:
- Phases 1-5 implemented the feature and added tests.
- Documentation pass remaining — there are 11 mandatory items per Encina's plan-template
  instructions; this phase MUST address each one explicitly (do not skip "obvious" ones).

TASK — execute all 11 items in order:

1. XML doc on new APIs — verify <summary>, <value>, <remarks>, and (where useful) <example>
   on EnableOtlpExporter and ConfigureOtlpExporter. Confirm by building with
   GenerateDocumentationFile=true and checking for zero CS1591 warnings.

2. CHANGELOG.md — append an "Encina.OpenTelemetry — OTLP Exporter Opt-In Wiring (#1043)"
   sub-section under [Unreleased] -> ### Added. Cover: both new properties, the
   PrivateAssets="all" change, the WireMock divergence, and unchanged default behavior.

3. ROADMAP.md — VERIFY only. Inspect lines 298-310 (v0.19.0 entry). Confirm the
   feature-area description ("OpenTelemetry integration", "Metrics & Tracing") implicitly
   covers OTLP wiring; no per-issue enumeration required. Document the verification in
   the PR description (do NOT skip the verification step itself).

4. src/Encina.OpenTelemetry/README.md — add two rows to the Configuration Options table,
   add a "Logs" subsection under "What Gets Instrumented?" (or coordinate with #1048
   README update if it lands first), add an "OTLP (OpenTelemetry Protocol)" subsection
   under "Integration with Observability Platforms" between Jaeger and Prometheus.

5. docs/features/opentelemetry-otlp-exporter.md — CREATE this new feature page covering
   Overview, Configuration (flag + callback semantics + env var fallback), Quick Start
   (local collector + Honeycomb/Grafana Cloud examples), Three Signals coverage, Health
   Check pointer to #1049, Troubleshooting, and Related links. Mirror the structure of
   docs/features/cdc.md for tone and depth.

6. docs/INVENTORY.md — enrich line 2960 (the Encina.OpenTelemetry row): change
   "Trazas y métricas" to "Trazas, métricas y logs (OTLP opt-in via #1043, logs vía #1048)".
   No new packages or modules to add — this is a row enrichment.

7. ADR — CREATE docs/architecture/adr/026-otlp-exporter-opt-in.md. The ADR is justified
   by 4 non-trivial design decisions documented in this plan (see Phase 6 task list,
   item #7 in the plan). Sections: Status (Accepted), Context, Decision, Consequences,
   References. Update docs/architecture/adr/index.md with the new entry.

8. PublicAPI — add 4 entries to PublicAPI.Unshipped.txt for the two new properties
   (.get + .set each). Do NOT touch PublicAPI.Shipped.txt — symbols only move on
   release. Verify zero RS0016 / RS0017 warnings.

9. docs/releases/v0.19.0/ — DEFER to release-cut time. The current in-progress folder
   is v0.13.0; v0.19.0 doesn't exist yet. The CHANGELOG [Unreleased] entry from item #2
   is the single source of truth until v0.19.0 is cut and the release folder is created.

10. Build verification — run dotnet restore + dotnet build with TreatWarningsAsErrors.
    Verify zero CS1591 (XML doc), zero RS0016/RS0017 (PublicAPI), zero NU1605 (downgrade).

11. Test verification — dotnet test for unit + guard + integration; verify ≥85% line
    coverage on new code via XPlat Code Coverage collector (artifacts/coverage/).

KEY RULES:
- Address EVERY ONE of the 11 items explicitly. If an item legitimately requires no
  change (e.g., ROADMAP.md), document the verification — do NOT silently skip.
- All new code blocks in markdown use the `csharp` language hint.
- ADR-026 follows the same structure as ADR-025 (latest existing ADR).
- The PR description checklist at the bottom of Phase 6 has 13 boxes — every one must
  be checked (or explicitly noted as N/A with justification).
- Commit message: `feat(opentelemetry): wire OTLP exporter as opt-in for traces, metrics, and logs (Fixes #1043)`.

REFERENCE FILES:
- src/Encina.OpenTelemetry/README.md (existing, to update)
- CHANGELOG.md (top of file, under [Unreleased] → ### Added; PR #1041 entries are the
  closest reference for tone)
- ROADMAP.md (lines 298-310 for v0.19.0)
- docs/INVENTORY.md (line 2960)
- docs/features/cdc.md (template for new feature page structure)
- docs/architecture/adr/025-performance-measurement-infrastructure.md (latest ADR; copy
  its structure for ADR-026)
- docs/architecture/adr/index.md (add ADR-026 entry)
- docs/releases/v0.13.0/README.md (template for future v0.19.0 release notes)
- src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt
- .github/coverage-manifest/Encina.OpenTelemetry.json (per-flag coverage targets)
```

</details>

---

## Research

### OpenTelemetry Specifications & Standards

| Reference | Relevance |
|-----------|-----------|
| [OTLP/gRPC Specification](https://opentelemetry.io/docs/specs/otlp/#otlpgrpc) | Wire format for traces, metrics, logs over gRPC (port 4317) |
| [OTLP/HTTP Specification](https://opentelemetry.io/docs/specs/otlp/#otlphttp) | Wire format for traces, metrics, logs over HTTP (port 4318) |
| [OTel SDK Configuration Env Vars](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/) | `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_HEADERS`, `OTEL_EXPORTER_OTLP_PROTOCOL` defaults |
| [OpenTelemetry .NET — `OtlpExporterOptions`](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/OtlpExporterOptions.cs) | Endpoint, Protocol, Headers, TimeoutMilliseconds, BatchExportProcessorOptions |
| [OpenTelemetry .NET — `AddOtlpExporter`](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/OtlpTraceExporterHelperExtensions.cs) | Extension method on `TracerProviderBuilder` and `MeterProviderBuilder` |
| [GHSA-mr8r-92fq-pj8p](https://github.com/advisories/GHSA-mr8r-92fq-pj8p) | OTLP exporter advisory addressed by 1.15.3 (relevant context — bumped in #1041) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `EncinaOpenTelemetryOptions` | `src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs` | Extend with two new properties |
| `WithEncina(OpenTelemetryBuilder, EncinaOpenTelemetryOptions?)` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | Add conditional `AddOtlpExporter` call after `WithMetrics(...)` |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol 1.15.3` | `Directory.Packages.props` | Already pinned; change is `PrivateAssets="all"` removal in csproj |
| `.github/observability/docker-compose.observability.yml` | Repo root | OTel Collector with OTLP gRPC (4317) and HTTP (4318) — used for integration tests |
| `ConsoleExporterIntegrationTests.cs` pattern | `tests/Encina.IntegrationTests/Observability/OpenTelemetry/` | Template for OTLP integration test |
| `EncinaOpenTelemetryOptionsEnableMessagingTests.cs` pattern | `tests/Encina.UnitTests/OpenTelemetry/` | Template for option-property unit tests |
| `ServiceCollectionExtensionsGuardTests.cs` | `tests/Encina.GuardTests/Infrastructure/OpenTelemetry/` | Add new guard tests here, do not create a new file |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.OpenTelemetry` | **Not registered** (pending [#1050](https://github.com/dlrivada/Encina/issues/1050)) | This feature does NOT introduce a new `[LoggerMessage]`. Pre-existing `ReshardingLogMessages` (in 7000-7099 range) is unregistered — fix tracked as #1050. |
| `Encina.OpenTelemetry` (future) | **TBD** | If structured logging is added around OTLP wiring (e.g., `OtlpExporterEnabled`, `OtlpEndpointResolved`) or for the health check #1049, a sub-range within 7000-7099 should be carved out — depends on #1050 landing first to formalize the parent range. |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Source — Options + Wiring | 2 | `EncinaOpenTelemetryOptions.cs`, `ServiceCollectionExtensions.cs` |
| Source — Project file | 1 | `Encina.OpenTelemetry.csproj` (1-line change) |
| Source — WireMock comment | 1 | `Encina.Testing.WireMock.csproj` (add inline comment) |
| Source — PublicAPI | 1 | `PublicAPI.Unshipped.txt` (4 new lines) |
| Tests — Unit | 2-3 | `EncinaOpenTelemetryOptionsOtlpTests.cs`, `WithEncinaOtlpExporterTests.cs`, optional update to existing `EncinaOpenTelemetryOptionsTests.cs` |
| Tests — Guard | 1 | Updates to `ServiceCollectionExtensionsGuardTests.cs` |
| Tests — Integration | 1 | `OtlpExporterIntegrationTests.cs` (3 test methods — one per signal: traces, metrics, logs) |
| Documentation — README | 1 | `src/Encina.OpenTelemetry/README.md` (table rows + new sections) |
| Documentation — CHANGELOG | 1 | `CHANGELOG.md` ([Unreleased] entry) |
| Documentation — Feature guide | 1 | **NEW** `docs/features/opentelemetry-otlp-exporter.md` |
| Documentation — INVENTORY | 1 | `docs/INVENTORY.md` (line 2960 row enrichment) |
| Documentation — ADR | 2 | **NEW** `docs/architecture/adr/026-otlp-exporter-opt-in.md` + `docs/architecture/adr/index.md` (1-line update) |
| Documentation — Coverage manifest | 1 | `.github/coverage-manifest/Encina.OpenTelemetry.json` (verify new lines accounted for under unit/guard flags) |
| **Total** | **~14-17 files touched** | (Up from ~10-12 in the original draft — the difference is the documentation pass auditing all 11 mandatory items.) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing Issue #1043 — Wire OTLP exporter as opt-in option in Encina.OpenTelemetry.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, Pre-1.0 (no backward compatibility, no Obsolete).
- Railway Oriented Programming with Either<EncinaError, T> for store/handler methods.
- Encina.OpenTelemetry is the OTel integration package — provides automatic tracing/metrics
  for Encina's CQRS pipeline, messaging stores, sharding, migrations, etc.
- Issue #1041 (security bump) added OpenTelemetry.Exporter.OpenTelemetryProtocol 1.15.3 as
  a private (PrivateAssets="all") direct dependency for transitive version pinning, but the
  package is not consumed at the public API level today. CodeRabbit flagged this as a
  follow-up.
- Issue #1048 (PREREQUISITE) wires WithLogging into WithEncina — must merge before this
  plan can wire the OTLP logs branch. See Design Choice #3.

IMPLEMENTATION OVERVIEW:
Single-package change in src/Encina.OpenTelemetry/.

Phase 1: Add EnableOtlpExporter (bool) and ConfigureOtlpExporter (Action<OtlpExporterOptions>?)
         to EncinaOpenTelemetryOptions; update PublicAPI.Unshipped.txt.
Phase 2: Inside WithEncina(OpenTelemetryBuilder, EncinaOpenTelemetryOptions?), wire
         AddOtlpExporter conditionally for tracing, metrics, AND logs (logs branch
         requires prerequisite #1048). The callback runs once per signal — 3 times total.
Phase 3: Drop PrivateAssets="all" from the OTLP PackageReference in
         Encina.OpenTelemetry.csproj. Keep it on Encina.Testing.WireMock.csproj (transitive
         pin only).
Phase 4: Cross-cutting integration check — Structured Logging is ✅ Included (via #1048),
         OpenTelemetry is ✅ Included (this IS OTel wiring), Health Checks ⏭️ Deferred,
         the rest N/A; no new code in this phase.
Phase 5: Tests — unit (~6-8), guard (~2), integration (~3, one per signal, gated on the
         existing observability docker-compose stack at localhost:4317).
Phase 6: Comprehensive documentation pass against the 11 mandatory documentation items
         per Encina's plan-template instructions: XML docs, CHANGELOG, ROADMAP verify,
         README, NEW docs/features/opentelemetry-otlp-exporter.md, INVENTORY enrichment,
         NEW ADR-026, PublicAPI, deferred release notes, build verification (zero
         warnings), test verification (≥85% line coverage). Each item must be addressed
         explicitly — even "no-update" items require documented verification.

KEY PATTERNS:
- Default for EnableOtlpExporter is FALSE (opt-in, pay-for-what-you-use).
- ConfigureOtlpExporter is invoked PER SIGNAL with a fresh OtlpExporterOptions each time;
  do not share an instance across traces, metrics, and logs.
- Null callback is valid — guard with `?.Invoke(otlp)` so the SDK applies its own defaults
  (which respect OTEL_EXPORTER_OTLP_* env vars).
- All three OTel signals (traces + metrics + logs) are wired when EnableOtlpExporter=true.
  The logs branch requires prerequisite #1048 — do NOT proceed without verifying #1048
  is merged (or implement the fallback documented in Phase 2).
- No health check, no new [LoggerMessage], no resilience wrapper — all deferred or N/A.
- The OTLP package's csproj reference moves from PrivateAssets="all" to no privacy
  attribute, in Encina.OpenTelemetry only. WireMock keeps the private reference with an
  inline comment explaining why (transitive version pin only, no API consumption).

REFERENCE FILES:
- src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs (extend)
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs (modify WithEncina)
- src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj (drop PrivateAssets="all")
- src/Encina.Testing.WireMock/Encina.Testing.WireMock.csproj (add inline comment)
- src/Encina.OpenTelemetry/PublicAPI/PublicAPI.Unshipped.txt (add 4 entries)
- tests/Encina.UnitTests/OpenTelemetry/EncinaOpenTelemetryOptionsEnableMessagingTests.cs (template)
- tests/Encina.UnitTests/OpenTelemetry/ServiceCollectionExtensionsTests.cs (template)
- tests/Encina.GuardTests/Infrastructure/OpenTelemetry/ServiceCollectionExtensionsGuardTests.cs (extend)
- tests/Encina.IntegrationTests/Observability/OpenTelemetry/ConsoleExporterIntegrationTests.cs (template)
- .github/observability/docker-compose.observability.yml (collector at localhost:4317)
- src/Encina.OpenTelemetry/README.md (update Configuration Options + add OTLP section)
- CHANGELOG.md (add under [Unreleased] → ### Added)

ACCEPTANCE CRITERIA (from issue #1043, expanded for Option B logs coverage):
- EnableOtlpExporter=true results in OTLP exporter registered for traces, metrics, AND logs.
- ConfigureOtlpExporter callback is invoked with the live OtlpExporterOptions, 3 times
  total (once per signal).
- Default (false) behavior is identical to today at runtime — no exporter registered,
  nothing exported. The OTLP package still flows transitively if PrivateAssets="all" is
  removed; the opt-in is a runtime switch, not a packaging boundary.
- Dependency-surface decision recorded in ADR-026: PrivateAssets="all" removed from
  Encina.OpenTelemetry's OTLP PackageReference, or a separate Encina.OpenTelemetry.Otlp
  package owns the reference.
- Zero build warnings.
- PublicAPI.Unshipped.txt tracks all four new symbols.
- Prerequisite #1048 (WithLogging in WithEncina) merged before the logs branch is wired.
```

</details>

---

## Cross-Cutting Integration Matrix

Evaluation against the 12 transversal functions defined in
[CLAUDE.md](../../CLAUDE.md#cross-cutting-integration-rule-mandatory) "Cross-Cutting Integration Rule":

| # | Function | Status | Notes |
|---|----------|:------:|-------|
| 1 | **Caching** | ❌ N/A | Exporter wiring is configuration-only; no domain data passes through this code path. The OTLP exporter has its own internal batching (controlled by `BatchExportProcessorOptions`) which is the right level for this concern. |
| 2 | **OpenTelemetry** | ✅ Included | This issue *is* the OTel wiring. Existing `ActivitySource("Encina")`, runtime instrumentation, and 12 tracing sources continue to be exported — now also via OTLP when the consumer opts in. |
| 3 | **Structured Logging** | ✅ Included (via #1048) | With the prerequisite #1048 wiring `WithLogging` in `WithEncina`, all Encina `[LoggerMessage]`-generated events (e.g., `ReshardingLogMessages` and future Encina log sources) flow through the OTel logger provider and are exported via OTLP when `EnableOtlpExporter = true`. Adding Encina-native `[LoggerMessage]` events around the OTLP wiring decision itself (e.g., `OtlpExporterEnabled`, `OtlpEndpointResolved`) would require registering a new EventId range — the existing 7000-7099 range used by `ReshardingLogMessages` is currently unregistered, tracked as [#1050](https://github.com/dlrivada/Encina/issues/1050). |
| 4 | **Health Checks** | ⏭️ Deferred (tracked as [#1049](https://github.com/dlrivada/Encina/issues/1049)) | An `OtlpExporterHealthCheck` (TCP probe to the configured endpoint with a configurable timeout) would be valuable but is out of scope for this issue. Tracked separately under EPIC #888 for v0.19.0. |
| 5 | **Validation** | ❌ N/A | Two new properties: a bool and a nullable delegate. Neither has a meaningful invalid state. OTLP-specific configuration (endpoint URL format, timeout > 0, header syntax) is validated by the `OtlpExporterOptions` SDK at exporter construction with clear error messages. |
| 6 | **Resilience** | ❌ N/A (documented limitation) | The SDK does **not** provide a circuit breaker, and `BatchExportProcessorOptions` only controls batching (queue size, delay, batch size); it is not a retry policy. Effective behaviour when the collector is unavailable: each export attempt fails after `OtlpExporterOptions.TimeoutMilliseconds` (default 10 s), the batch is dropped, the failure is reported through the SDK self-diagnostics only, and the application is never blocked or faulted. Retry exists only as an experimental opt-in via `OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY=in_memory` or `disk`, which the plan neither enables nor documents as supported. Wrapping the exporter in Polly is not done because the SDK owns the export pipeline. The collector-unavailable integration test (Phase 2) pins this behaviour. |
| 7 | **Distributed Locks** | ❌ N/A | Stateless feature. No shared resource to coordinate access to. |
| 8 | **Transactions** | ❌ N/A | Stateless feature. No multi-operation atomicity to guarantee. |
| 9 | **Idempotency** | ❌ N/A | Telemetry export is one-way (Encina → collector). The OTLP protocol itself handles duplicate-suppression at the collector via OpenTelemetry sampling and aggregation — out of Encina's domain. |
| 10 | **Multi-Tenancy** | ❌ N/A | Tenant context, when present in the request pipeline, already flows through `Activity.Current` tags via the existing `TenancyActivityEnricher` (`tenancy.tenant_id` etc.) and is exported transparently as part of the span attributes. No additional tenancy work needed. |
| 11 | **Module Isolation** | ❌ N/A | OTel wiring is a single global pipeline by design; module-scoped exporters would defeat the purpose of cross-module distributed tracing. The existing `ModuleMetrics` and `Encina.Modules` ActivitySource handle module-aware instrumentation; OTLP transport doesn't change. |
| 12 | **Audit Trail** | ❌ N/A | Telemetry (metrics + traces) is operational, not auditable. Compliance audit events flow through `IAuditStore`, which is wrapped by `InstrumentedAuditStore` and exported as spans regardless of OTLP being enabled. |

**Summary**: 2 ✅ Included (OpenTelemetry, Structured Logging — via prerequisite #1048),
1 ⏭️ Deferred (Health Checks), 9 ❌ N/A. The two ✅ entries are satisfied by the
combination of #1048 (logs wiring) and this plan (OTLP for all three signals); the single
⏭️ entry (OTLP collector reachability health check) is documented as a follow-up candidate.

---

## Acceptance Criteria

(From issue #1043, expanded to cover all three OTel signals per Design Choice #3.)

- [ ] **Prerequisite #1048 merged** — `WithLogging` is wired into `WithEncina(...)`
- [ ] `EnableOtlpExporter=true` results in OTLP exporter being registered for **traces, metrics, AND logs**
- [ ] `ConfigureOtlpExporter` callback is invoked with the live `OtlpExporterOptions` once per signal (3 times total)
- [ ] Default (`false`) behavior is identical to today at runtime — no exporter registered, nothing exported
- [ ] Dependency-surface decision recorded in ADR-026: either `PrivateAssets="all"` removed from `Encina.OpenTelemetry`'s OTLP `PackageReference` (the OTLP package becomes a transitive dependency of all consumers) or a separate `Encina.OpenTelemetry.Otlp` package owns the reference
- [ ] Collector-unavailable integration test passes (no exception surfaces, host shuts down cleanly)
- [ ] Zero build warnings
- [ ] PublicAPI tracked

---

## Next Steps

1. **Land prerequisite #1048** — wire `WithLogging` in `WithEncina(...)`. This is a small,
   foundational change that must merge **before** this plan's Phase 2 can wire the OTLP
   logs branch.
2. **Review and approve this plan** — confirm the design choices (particularly Option B on
   signal coverage and the WireMock divergence in Phase 3) align with reviewer expectations.
3. **Implement Phase 1** in a new session (Options + PublicAPI).
4. **Implement Phase 2** (wiring) — must follow Phase 1 (reads the new property) and #1048
   (provides the `WithLogging` block to attach the OTLP log exporter to).
5. **Implement Phase 3** (csproj change) — independent of Phases 1/2; can be batched with
   them in the same PR.
6. **Implement Phase 4** (cross-cutting check) — confirms no new integrations needed; no
   code change.
7. **Implement Phase 5** (tests) — unit + guard required; integration optional but
   recommended.
8. **Implement Phase 6** (docs + verify) — finalize the PR.
9. **PR title**: `feat(opentelemetry): wire OTLP exporter as opt-in for traces, metrics, and logs (Fixes #1043)`
10. **Follow-up issues** (created and tracked under EPIC #888):
    - [#1049](https://github.com/dlrivada/Encina/issues/1049) — OTLP collector reachability
      health check (`OtlpExporterHealthCheck : IEncinaHealthCheck`)
    - [#1050](https://github.com/dlrivada/Encina/issues/1050) — Register
      `Encina.OpenTelemetry` EventId range 7000-7099 in `EventIdRanges.cs` (resharding
      log messages currently use unregistered IDs in violation of ADR-021)
    - [#1051](https://github.com/dlrivada/Encina/issues/1051) — SPIKE: re-evaluate
      `CentralPackageTransitivePinningEnabled` to retire the WireMock
      `PrivateAssets="all"` workaround (depends on resolving Marten/JasperFx and Aspire
      dependency conflicts)
