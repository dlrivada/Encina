# Implementation Plan: How to view Encina telemetry in the .NET Aspire Dashboard — guide and sample application

> **Issue**: [#188](https://github.com/dlrivada/Encina/issues/188) (family: #87, #88, #89, #344; D1 is the same question as #87)
> **Type**: Feature (documentation and a sample; no change under `src/`)
> **Research**: `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`, `src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs`, `src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj`, `docs/architecture/adr/054-observability-conventions-and-trace-propagation.md`, `docs/guides/health-checks.md`, `docs/testing/aspire-migration-guide.md`, `Directory.Packages.props`
> **ADR**: none (ADR-054 is cited, not changed)
> **Related**: #87 (owns the `examples/` scaffold and Dilemma D1), #1043 (OTLP exporter as an opt-in option of `Encina.OpenTelemetry`, open), #1787, #1790 and #1791 (ADR-054 and its follow-ups), #3491 (the Aspire 13.6.5 pin)
> **Complexity**: Low to medium (one how-to page, a small sample, screenshots)
> **Estimated Scope**: about 10 files, about 650 lines plus images

---

## Summary

The .NET Aspire Dashboard is becoming a common way to look at .NET telemetry, and Encina already emits standard OpenTelemetry (ADR-054), so any backend works. This plan writes a how-to guide that takes a developer from a running Encina application to its traces, metrics and logs in the dashboard, plus a small sample application the guide links. The guide is a page under `docs/guides/` (how-to quadrant, `.claude/skills/encina-docs/SKILL.md` section 1), titled "How to view Encina telemetry in the Aspire Dashboard", indexed from `docs/guides/index.md`. `docs/testing/aspire-migration-guide.md` covers Aspire for tests only and is linked as a neighbour, not extended.

**Corrections to the issue text, verified in `src/`:**

- `EncinaOpenTelemetryOptions` has only `ServiceName`, `ServiceVersion` and `EnableMessagingEnrichers` (`src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs`). `OtlpEndpoint`, `EnableTracing`, `EnableMetrics` and `RecordExceptions` do not exist; the OTLP endpoint is configured through the OTLP exporter or the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable.
- **The service name set through `AddEncinaOpenTelemetry` does not reach the resource.** `AddEncinaOpenTelemetry(Action<EncinaOpenTelemetryOptions>?)` stores its own options instance as a singleton and reads only `EnableMessagingEnrichers` from it (`ServiceCollectionExtensions.cs:49-59`; `ServiceName` is read nowhere else in that file), while `WithEncina(EncinaOpenTelemetryOptions? options = null)` builds the resource from the options passed to it, defaulting to `new EncinaOpenTelemetryOptions()` whose `ServiceName` is `"Encina"` (`:166-177`). The issue's snippet (`AddEncinaOpenTelemetry(o => o.ServiceName = "my-service")` then `.WithEncina()`) would therefore show the application as `Encina` in the dashboard. The guide passes the options to `WithEncina(new EncinaOpenTelemetryOptions { ServiceName = "my-service" })`; the worker confirms this by running the sample and reading the resource name in the dashboard. If the behavior is a defect rather than a design, the worker reports it for an issue file; this plan does not decide it.
- **The OTLP exporter is not a transitive dependency of the application.** `Encina.OpenTelemetry` references `OpenTelemetry.Exporter.OpenTelemetryProtocol` with `PrivateAssets="all"` (`src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj`), and ADR-054 decision 1 keeps exporters out of `WithEncina()`. The sample therefore references the exporter package itself, and the guide says so; #1043 (open) would wire it as an opt-in option and is linked, not promised.
- **What the dashboard receives today.** `WithEncina()` adds an explicit list of `ActivitySource` names (`Encina`, `Encina.Sharding`, `Encina.Repository`, `Encina.UnitOfWork`, `Encina.Messaging.Outbox`, and others, `:181-208`) and one meter, `Encina` (`:213`), plus runtime instrumentation. ADR-054 (accepted 2026-10-22) decides renamed per-package meters and a wildcard subscription, but that is follow-up work (#1787, #1790, #1791); the guide describes the names that exist on `main` on the day it is written and cites ADR-054 for the direction. `AddServiceDefaults()` belongs to the Aspire project template, not to Encina.
- The repository pins `Aspire.Hosting.Testing` and `Aspire.Hosting.PostgreSQL` at 13.6.5 (`Directory.Packages.props`, after the advisory fix of #3491); the issue's 13.5.4 is out of date.

**Sections of the guide** (how-to): 1 run the standalone dashboard with Docker; 2 connect an Encina application; 3 find and filter traces; 4 read Encina metrics; 5 read structured logs and correlate them with traces; 6 use the Aspire host (resources and health); 7 troubleshooting; 8 the sample application and screenshots.

**Provider category**: Observability (OpenTelemetry plus exporters), described and not extended (AGENTS.md section 5).

**Affected packages**: `Encina.OpenTelemetry` (consumed, not changed); documentation under `docs/guides/`; a sample project (location per D1).

**Size**: 1 plan file; the implementation is one worker pull request of about 10 files and 650 lines, within the 15-file and 1500-line limit.

---

## Design Choices

<details>
<summary><strong>1. Where the sample application lives (D1, shared with #87, #88, #89)</strong></summary>

**Class:** Dilemma

The full question is Design Choice 1 of the #87 plan. For this guide the sample is a small Web API with Encina and the OTLP exporter, named `06-observability-dashboard` in the numbering of the examples (a naming proposal, Trivial).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) `examples/06-observability-dashboard` with `ProjectReference`, in `Encina.slnx` and CI** | Compiles against current code; the guide's snippet cannot rot silently | The sample needs `OpenTelemetry.Exporter.OpenTelemetryProtocol` and `OpenTelemetry.Extensions.Hosting` entries checked in `Directory.Packages.props` (the exporter and hosting versions are already pinned for `src/`) |
| **(b) The same project outside the solution, on a local package feed** | Closer to what a user copies | CI does not notice an API change |
| **(c) Snippets in the guide only; no runnable sample** | No build cost | The acceptance criterion "sample application with full integration" is not met; wins only if screenshots from a throwaway application are enough |

### Chosen Option: **(a) — `examples/06-observability-dashboard` with `ProjectReference`** (recommended, pending the maintainer)

### Rationale

Same reasoning as #87: while the API moves before 1.0, only a built sample stops the guide from describing code that no longer compiles. The answer is recorded once, in the #87 plan's `## Maintainer Decisions`, and this plan follows it.

</details>

<details>
<summary><strong>2. Whether the sample includes an Aspire AppHost project</strong></summary>

**Class:** Dilemma

Section 6 of the guide covers the Aspire host (resources, health). An AppHost project needs the Aspire AppHost SDK or package, which is not in `Directory.Packages.props` today (only `Aspire.Hosting.Testing` and `Aspire.Hosting.PostgreSQL` are, both pinned after the advisory of #3491) and which the build would have to restore.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **(a) Include an `AppHost` project next to the sample** | Every section is proven by a build; the reader can copy a complete host | Adds the Aspire AppHost SDK and its transitive packages to the solution build and to the audit of transitive packages; AppHost needs Docker only at run time but is built everywhere |
| **(b) Standalone dashboard only in the repository; the Aspire host section is a short verified snippet** | No new SDK in the solution; the AppHost is about ten lines | The snippet is verified once by a manual run, not by CI |

### Chosen Option: **(b) — Standalone sample; the host section as a verified snippet** (recommended, pending the maintainer)

### Rationale

We recommend (b): the guide's main path is the standalone dashboard (Design Choice 4), and ten lines do not justify a new SDK in every build and every dependency audit. (a) wins if the maintainer wants every section of the guide compiled in CI, accepting the extra dependency. The worker runs the AppHost snippet once in a scratch folder outside the repository and pastes the real output.

</details>

<details>
<summary><strong>3. The corrected configuration snippet</strong></summary>

**Class:** Settled
**Settled by:** ADR-054 decision 1 (exporters and dashboards stay out of `WithEncina()`; the application chooses the SDK and exporters), `.claude/skills/encina-docs/SKILL.md` house rule 3 (every code example names real API)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `WithEncina(new EncinaOpenTelemetryOptions { ServiceName = ... })` plus the application's own OTLP exporter and `OTEL_EXPORTER_OTLP_ENDPOINT`** | Real API; the service name reaches the resource; exporters stay in the application | The reader adds one package reference |
| **B — The issue's snippet (`AddEncinaOpenTelemetry(o => ...)` then `.WithEncina()`, with non-existent options)** | Shorter | Uses options that do not exist; the service name is lost |

### Chosen Option: **A — The corrected snippet**

### Rationale

Only A compiles and shows the intended service name. The worker verifies whether `UseOtlpExporter()` of `OpenTelemetry.Exporter.OpenTelemetryProtocol` covers traces, metrics and logs in one call and, if so, prefers it to three `AddOtlpExporter` calls; the logs view needs the logging pipeline wired too (see the #1043 plan).

</details>

<details>
<summary><strong>4. Standalone dashboard first, Aspire host second</strong></summary>

**Class:** Settled
**Settled by:** ADR-054 decision 1 (the application chooses exporters; no host is required), #188 (original reasoning: a full Aspire host must not be required)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Standalone Docker dashboard as the main path, the Aspire host as a later section** | Works for any .NET project; one `docker run` | The resource and health view is richer under an Aspire host, so section 6 is a second step |
| **B — Aspire host only** | One path | Excludes every project that is not an Aspire application |
| **C — Jaeger or Zipkin only (the original rejected alternative)** | Familiar | Misses the Aspire ecosystem |

### Chosen Option: **A — Standalone first**

### Rationale

Encina emits standard OpenTelemetry, so the dashboard is a consumer like any other. The guide pins the dashboard image tag to the one matching the repository's Aspire pin (13.6.5) after checking it exists, and does not use `latest`; it documents unauthenticated access only as a local-development setting with a warning, after verifying the setting names against the Microsoft documentation.

</details>

<details>
<summary><strong>5. The guide describes the telemetry names that exist today</strong></summary>

**Class:** Settled
**Settled by:** ADR-054 (accepted 2026-10-22; naming and wildcard subscription are follow-up work: #1787, #1790, #1791), `.claude/skills/encina-docs/SKILL.md` house rules 4 and 7 (link decisions; promise nothing pre-1.0)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Document the names `WithEncina()` subscribes today and cite ADR-054 for the direction** | True on the day it is written; no promise | The page needs an edit when the follow-ups land |
| **B — Document the ADR-054 target names** | Final names | Wrong today: the 27 plain `"Encina"` meters are not renamed yet |

### Chosen Option: **A — Today's names**

### Rationale

A reader following the guide must see what the guide says. The worker lists the names by running the sample and reading the dashboard, not from memory; it adds a "Names can change before 1.0" sentence linking ADR-054.

</details>

<details>
<summary><strong>6. Screenshots show error codes, never messages</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (`EncinaError.Message` never reaches logs, activity tags or health-check results), ADR-054 decision 2 (a span marked as error carries the code or the exception type only)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — The sample's failing request returns a coded error; screenshots show the code on the span and the log line** | Teaches the compliant pattern | Needs a coded error in the sample |
| **B — Show whatever message the sample emits** | Less work | Teaches the pattern the rule forbids |

### Chosen Option: **A — Codes only**

### Rationale

Only A complies. Known remaining leaks are tracked by the issues ADR-054 lists; the worker crops or avoids any screenshot that shows one and reports it.

</details>

---

## Implementation Phases

### Phase 1: The sample application

> **Goal**: A small Encina Web API that sends telemetry to the standalone dashboard. Depends on the #87 scaffold.

<details>
<summary>Tasks</summary>

1. `examples/06-observability-dashboard/06-observability-dashboard.csproj`: `Microsoft.NET.Sdk.Web`, references `Encina`, `Encina.OpenTelemetry`, `Encina.AspNetCore` and the OTLP exporter package (version from `Directory.Packages.props`; add an entry only if missing).
2. `Program.cs`: `AddEncina(typeof(Program).Assembly)`, `AddEncinaOpenTelemetry(...)`, `AddOpenTelemetry().WithEncina(new EncinaOpenTelemetryOptions { ServiceName = "encina-dashboard-sample" })` plus the OTLP exporter for the three signals, an endpoint that sends a command that succeeds and one that fails with a coded `Left`, and a request that publishes a notification.
3. One command, one failing command, one notification and their handlers; the failing one returns `EncinaErrors.Create("sample.rejected", "...")`.
4. A `launchSettings` or README step that sets `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317` for the standalone dashboard.
5. Design Choice 2 decides whether an `AppHost` folder exists; the recommended option keeps it out of the repository.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.OpenTelemetry exposes AddEncinaOpenTelemetry(Action<EncinaOpenTelemetryOptions>?), OpenTelemetryBuilder.WithEncina(EncinaOpenTelemetryOptions?), TracerProviderBuilder.AddEncinaInstrumentation() and MeterProviderBuilder.AddEncinaInstrumentation() (src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs). The OTLP exporter is PrivateAssets="all" in that package, so the application references it. The #87 plan created the examples/ scaffold.
TASK: Build examples/06-observability-dashboard as in Phase 1 tasks 1-5: a Web API that sends traces, metrics and logs to the standalone Aspire Dashboard, with a succeeding command, a failing coded command and a notification.
KEY RULES: pass the options to WithEncina (the options given to AddEncinaOpenTelemetry do not reach the resource); verify every API in src/ and in the OpenTelemetry package; errors carry codes, never EncinaError.Message; zero warnings (TreatWarningsAsErrors); English only.
REFERENCE FILES: src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs, src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj, Directory.Packages.props, docs/plans/example-basic-encina-implementation-plan-87.md.
```

</details>

### Phase 2: Run it against the dashboard and capture evidence

> **Goal**: Every statement of the guide comes from a real run.

<details>
<summary>Tasks</summary>

1. Start the standalone dashboard with Docker (image tag per Design Choice 4; ports for the UI and for OTLP over gRPC), run the sample, send the three requests.
2. Record: the resource name shown (it must be the configured service name), the list of Encina `ActivitySource` names that appear, the Encina meter and instrument names on the metrics page, the structured log fields, and the span status of the failing request (code only).
3. Capture screenshots of the traces list, one trace detail, the metrics page and the structured logs page. If no tool is available to the worker for screenshots, the worker says so; the guide then ships with the Mermaid diagrams and the text of the real output, and the missing screenshots are reported for the maintainer (acceptance criterion "Screenshots and diagrams" is then only partly met).
4. Run the AppHost snippet once in a scratch folder outside the repository (Design Choice 2) and record what the resource and health view shows.
5. If the service-name behavior of `WithEncina` versus `AddEncinaOpenTelemetry` is a defect, write an issue file; do not change `src/` in this pull request.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The sample exists (Phase 1). The standalone dashboard image is mcr.microsoft.com/dotnet/aspire-dashboard; the repository pins Aspire at 13.6.5 in Directory.Packages.props.
TASK: Run the dashboard in Docker, run the sample, send the requests and record the real evidence listed in Phase 2 tasks 1-5 into the worker's notes for docs-writer.
KEY RULES: use a pinned image tag that exists, not latest; unauthenticated dashboard access is for local development only; never invent output; if Docker or a screenshot tool is unavailable, say so; no hand-typed counts or timings in the guide.
REFERENCE FILES: examples/06-observability-dashboard/Program.cs, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md, docs/testing/aspire-migration-guide.md.
```

</details>

### Phase 3: The guide

> **Goal**: One how-to page a developer can follow in about ten minutes.

<details>
<summary>Tasks</summary>

1. `docs/guides/how-to-view-encina-telemetry-in-the-aspire-dashboard.md` by `docs-writer`: front matter `title: "How to view Encina telemetry in the Aspire Dashboard"`, `layout: default`, `parent: "Guides"`; the opening names the reader and the result.
2. The eight sections of the Summary, using the Phase 2 evidence; a Mermaid diagram of the path application, OTLP, dashboard.
3. A short table of the Encina `ActivitySource` and meter names that exist today (from the code, with the date and the command that listed them, not a count).
4. Troubleshooting: no data (endpoint, port, protocol), wrong service name (the options pitfall), missing logs (logging pipeline), unsecured dashboard warning.
5. Links: ADR-054, the health checks guide, the Aspire testing guide, #1043 for the exporter option, the sample folder.
6. Images under the repository's docs assets folder (checked at write time), each with alt text.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: The evidence of Phase 2 exists. The page is how-to quadrant; ADR-054 holds the conventions and is linked, not restated.
TASK: Spawn docs-writer (foreground, naming the worktree path) to write docs/guides/how-to-view-encina-telemetry-in-the-aspire-dashboard.md as in Phase 3 tasks 1-6 and to add the bullet to docs/guides/index.md.
KEY RULES: How-to only (steps and results; the concepts go to ADR-054); every identifier verified in src/; names that ADR-054 will change are labelled as today's; no hand-typed figures; say pre-1.0; English only; no private client application names.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, docs/guides/health-checks.md, docs/guides/how-to-write-a-pipeline-behavior.md, docs/guides/index.md.
```

</details>

### Phase 4: Verification and finalization

> **Goal**: The guide and the sample pass the gates.

<details>
<summary>Tasks</summary>

1. `dotnet build examples/06-observability-dashboard -c Release -warnaserror` and `dotnet build Encina.slnx -c Release -warnaserror` exit 0; `dotnet format Encina.slnx --verify-no-changes` exits 0.
2. `lychee --offline --config .github/lychee.toml 'docs/**/*.md' 'README.md'` (or `Test-Path` on every relative link if lychee is unavailable), `npx --yes markdownlint-cli2 "docs/**/*.md" "#docs/INVENTORY.md"`, and the `cov-docs-render.cs --check-dangling` command of the skill.
3. A row for the sample in `examples/README.md` (created by the #87 plan).
4. No changelog fragment (documentation only), no ADR, no `PublicAPI` change; no test project: no file under `src/` changes, so no per-flag coverage target applies.
5. The guide is cited from the OpenTelemetry package README by a single link line, if the README has a documentation section (checked at write time).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The sample and the guide are written (Phases 1-3).
TASK: Run the build, format and documentation checks of Phase 4 tasks 1-2 from the worktree root, add the examples/README.md row, and report the real output.
KEY RULES: never skip a gate silently; spawn ci-diagnoser for a failure that is not obvious; findings by file and heading, ranked blocker, major, minor; English only.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md sections 5 and 6, Directory.Build.props, .github/lychee.toml.
```

</details>

---

## Research

### Relevant Standards and Prior Art

| Source | Relevance to this plan |
|--------|------------------------|
| ADR-054 observability conventions and trace propagation | Telemetry names, error status carries the code only, exporters outside `WithEncina()` |
| OpenTelemetry specification: OTLP, `OTEL_EXPORTER_OTLP_ENDPOINT` | How the application reaches the dashboard |
| [.NET Aspire Dashboard overview](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/overview) | Views the guide walks through |
| [Standalone Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/standalone) | The Docker path of section 1 |
| AGENTS.md section 3 (`EncinaError.Message` rule) and section 6 (cross-cutting functions) | What the screenshots may show; the matrix below |
| Comparable guides (Wolverine and MassTransit observability pages) | A how-to per backend with real screenshots |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AddEncinaOpenTelemetry`, `WithEncina`, `AddEncinaInstrumentation` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | The configuration the guide shows |
| `EncinaOpenTelemetryOptions` | `src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs` | `ServiceName`, `ServiceVersion`, `EnableMessagingEnrichers` |
| OTLP exporter reference (`PrivateAssets="all"`) | `src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj` | Why the application references the exporter itself |
| Aspire pins | `Directory.Packages.props` | Version of the dashboard image and Aspire packages named in the guide |
| Health checks guide and Aspire testing guide | `docs/guides/health-checks.md`, `docs/testing/aspire-migration-guide.md` | Neighbours linked from the guide |
| `examples/` scaffold | #87 plan | Home of the sample |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| None | None | Documentation and a sample only; no `[LoggerMessage]` in `src/` and no range registered in `EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Guide | 2 | the page (about 400 lines) and the bullet in `docs/guides/index.md` |
| Sample | 4 | csproj, `Program.cs`, messages and handlers (2 files), counted with the README below |
| Sample README and index row | 2 | example `README.md` and one row in `examples/README.md` |
| Images | 3 to 4 | screenshots of traces, a trace, metrics and logs (only if a capture tool is available) |
| **Total** | **about 11 to 12** | About 650 lines plus images; within one worker pull request |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 mediator library with Railway Oriented Programming (Either<EncinaError, T>) and opt-in features (AGENTS.md). Issue #188 adds a how-to guide for the .NET Aspire Dashboard and a sample application (plan docs/plans/aspire-dashboard-guide-implementation-plan-188.md). D1 (sample location) is recommended as examples/ with ProjectReference and D2 (AppHost project in the repository) as no, both pending the maintainer. Documentation follows the encina-docs skill.

IMPLEMENTATION OVERVIEW:
1. Sample examples/06-observability-dashboard: Web API with AddEncina, AddEncinaOpenTelemetry, AddOpenTelemetry().WithEncina(new EncinaOpenTelemetryOptions { ServiceName = ... }), the application's own OTLP exporter for traces, metrics and logs; a succeeding command, a failing coded command, a notification.
2. Evidence: run the standalone dashboard in Docker (pinned image tag), send requests, record the real resource name, ActivitySource and meter names, log fields and the failing span's code; capture screenshots when a tool exists.
3. Guide docs/guides/how-to-view-encina-telemetry-in-the-aspire-dashboard.md via docs-writer: eight sections, Mermaid path diagram, table of today's source and meter names, troubleshooting, links to ADR-054, #1043 and the sample.
4. Verification: zero-warnings build of the sample and the solution, format check, lychee offline (or Test-Path), markdownlint-cli2, cov-docs-render dangling check.

KEY PATTERNS: the options given to AddEncinaOpenTelemetry do not reach the resource, pass them to WithEncina; the OTLP exporter is PrivateAssets=all in Encina.OpenTelemetry so the application references it; names ADR-054 will change are labelled as today's; errors show codes, never EncinaError.Message; no hand-typed counts or timings; no latest image tag; unauthenticated dashboard only for local development; English only; no AI attribution; no private client application names.

REFERENCE FILES: src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs, src/Encina.OpenTelemetry/Encina.OpenTelemetry.csproj, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md, docs/guides/health-checks.md, docs/testing/aspire-migration-guide.md, Directory.Packages.props, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Documentation and a sample; no runtime component |
| 2 | OpenTelemetry | ✅ | The guide is about OpenTelemetry configuration for the dashboard; it cites names and adds none |
| 3 | Structured Logging | ✅ | The logs view and trace correlation are a section of the guide; no library EventIds |
| 4 | Health Checks | ✅ | The resource and health view (section 6), linking `docs/guides/health-checks.md`; no new check |
| 5 | Validation | ❌ | Documentation; no runtime component |
| 6 | Resilience | ❌ | Documentation; no runtime component |
| 7 | Distributed Locks | ❌ | Documentation; no runtime component |
| 8 | Transactions | ❌ | Documentation; no runtime component |
| 9 | Idempotency | ❌ | Documentation; no runtime component |
| 10 | Multi-Tenancy | ❌ | Documentation; tenant tags are not covered by this guide |
| 11 | Module Isolation | ❌ | Documentation; no runtime component |
| 12 | Audit Trail | ❌ | Documentation; no runtime component |

No function is deferred, so no follow-up issue is needed for the matrix.

---

## Prerequisites & Dependencies

| Item | State | Relation |
|------|-------|----------|
| #87 plan: `examples/` scaffold and D1 | open (plan in this family) | The sample starts after the scaffold merges |
| #1043 OTLP exporter as an opt-in option | open | Linked as the future shape; the guide documents the application-side reference that is needed today |
| #1787, #1790, #1791 (ADR-054 follow-ups) | open | The names in the guide are today's and are labelled; revisit the page when they land |
| Docker and a screenshot tool for the worker | environment | Needed for Phase 2; if absent the worker reports it and the guide ships with diagrams and real text output |
| Service-name behavior of `WithEncina` versus `AddEncinaOpenTelemetry` | to be confirmed by the Phase 2 run | If it is a defect, an issue file is written; this plan does not change `src/` |

**Risks**: the standalone dashboard's environment variable names and image tags change between Aspire releases (the worker checks the Microsoft documentation and the pinned version on the day); screenshots can show error text from a library path that still leaks a message (ADR-054 lists the tracking issues), in which case they are cropped or omitted and reported.

---

## Next Steps

1. Merge this plan.
2. When the issue is picked up, re-check D1 (recorded in the #87 plan), ask D2 and re-check the telemetry names against `main`; record the answers in `## Maintainer Decisions`.
3. One worker brief for Phases 1 to 4 after the #87 scaffold merges; the guide goes to `docs-writer` and the review to `adversarial-reviewer`.
4. Close #188 with the implementation pull request.
