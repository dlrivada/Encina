# Implementation Plan: OpenTelemetry Documentation — Tracing, Metrics, EventId Ranges and Exporters

> **Issue**: [#906](https://github.com/dlrivada/Encina/issues/906)
> **Type**: Feature (documentation only, no production code)
> **Complexity**: Medium (4 phases, one pull request, 5 files)
> **Estimated Scope**: one page of about 500 lines of Markdown, three small edits, one changelog fragment

---

## Summary

Write `docs/features/observability.md`, the first feature page about Encina's OpenTelemetry integration. Today the root `README.md` ("OpenTelemetry Integration", from line 394) shows `AddSource("Encina")` and `AddMeter("Encina")` only. Those two calls subscribe the core source and meter; `WithEncina()` (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:166`) subscribes the other sources and meters (21 `AddSource` and `AddMeter` calls in the method on origin/main 942c0f124, counted with a search of the file; the writer re-counts and cites the date and command rather than typing the number as a fact). The closest existing pages are `docs/features/abac/reference/observability.md` (ABAC only) and `src/Encina.OpenTelemetry/README.md`.

The page covers, from the code at writing time:

- **Setup**: `AddEncinaOpenTelemetry` (line 47), `WithEncina` (line 166), `AddEncinaInstrumentation` for `TracerProviderBuilder` and `MeterProviderBuilder` (lines 284 and 297), and `EncinaOpenTelemetryOptions` (`ServiceName` "Encina", `ServiceVersion` "1.0.0", `EnableMessagingEnrichers` true).
- **Traces and metrics**: `ActivitySource` names, span naming, the tags of `ActivityTagNames.cs` and the enrichers in `Enrichers/`; `Meter` instruments and the naming rules of [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) (accepted 2026-10-22; its implementation, for example #1787 and #1790, is open, so where code and ADR differ the page documents the code and flags the pending rename).
- **Structured logging**: `[LoggerMessage]` with EventId ranges registered in `src/Encina/Diagnostics/EventIdRanges.cs` (`GetAllRanges()` at line 401, 91 ranges today), the `Log.cs` pattern and the architecture rule `EventIdUniquenessRule` (`src/Encina.Testing.Architecture/EventIdUniquenessRule.cs`), with [ADR-021](../architecture/adr/021-eventid-uniqueness-enforcement.md).
- **Per-subsystem instrumentation table** generated from the code, one row per package that emits. `src/Encina.OpenTelemetry/` has instrumentation folders for Messaging stores, Query cache, Repository, Unit of Work, Tenancy, Modules, Audit, Sharding, Migrations, Resharding, Soft delete, ID generation, Bulk operations, Reference tables and CDC; the issue also lists Security and Compliance, which the writer includes only if a package emits (`rg` for `ActivitySource` and `Meter` in `src/`).
- **Exporters**: OTLP, Console and Prometheus, each shown with a configuration that was run; Jaeger and Azure Monitor from the package README; Zipkin and AWS X-Ray are listed only if a verified configuration exists. No exporter is wired into `WithEncina()` (ADR-054 settled point 1; OTLP wiring is #1043). No telemetry is sent to the project or the maintainer.
- **Local stack**: the full OpenTelemetry stack (collector, Prometheus, Jaeger, Loki, Grafana) is `.github/observability/docker-compose.observability.yml`, started with `docker compose -f .github/observability/docker-compose.observability.yml up -d` as its README says. The main `docker-compose.yml` profile `observability` starts a different, smaller stack (its header comment names Seq), so the issue's wording "started with `docker compose --profile observability up -d`" is checked and the page names each command for what it starts. Pre-built dashboards are #187 (open); the page references the setup, not dashboards that do not exist.

**Which provider category applies**: Observability (OpenTelemetry; planned exporters AzureMonitor, AwsXRay, Prometheus; `AGENTS.md` section 5). No provider code.
**Affected packages**: `Encina.OpenTelemetry` and `Encina` (`EventIdRanges.cs`, diagnostics), documented only.

---

## Design Choices

No open design choices. Every choice follows a cited convention:

| Id | Choice | Class | Settled by | Verified |
|----|--------|-------|------------|----------|
| 1 | One page `docs/features/observability.md`, `parent: "Features"`, entry in `docs/features/index.md` (the repository has no `docs/toc.yml`); reference tables are generated or cited from code, not typed | Trivial | `.claude/skills/encina-docs/SKILL.md` section 1 and section 3 rule 2; SPEC-001 | pending adversarial review |
| 2 | Names follow ADR-054; where the code differs at writing time, the page documents the code and flags the pending rename | Settled | ADR-054 and the open implementation issues #1787 and #1790 | pending adversarial review |
| 3 | Samples record the error code or exception type, never `EncinaError.Message` | Settled | `AGENTS.md` section 3; ADR-054 settled point 2 | pending adversarial review |
| 4 | The page states that no telemetry is sent to the project or the maintainer | Settled | ADR-054 settled point 1, SPEC-000, SPEC-002 DEC-010 | pending adversarial review |
| 5 | Exporter scope: show configurations of exporters that exist in the OpenTelemetry SDK and were run; do not promise Encina exporter packages; at least 3 are run (OTLP, Console, Prometheus) | Settled | ADR-054 settled point 1; the acceptance criteria of #906 | pending adversarial review |
| 6 | The tenant tag is documented as present only when multi-tenancy is on | Settled | ADR-054 settled points (tenant tag rule) | pending adversarial review |
| 7 | The local stack is referenced by its real path `.github/observability/` and the command that starts it; dashboards that do not exist (#187) are not described | Settled | `.claude/skills/encina-docs/SKILL.md` section 3 rule 3 (every statement names real things) and the files read for this plan | pending adversarial review |
| 8 | No hand-typed counts: the number of sources, meters or ranges goes in a sentence with the date and the command that produced it, or is left out | Settled | `.claude/skills/encina-docs/SKILL.md` section 3 rule 2; `AGENTS.md` section 9 | pending adversarial review |

---

## Implementation Phases

### Phase 1: Catalog of Sources, Meters, Tags and EventId Ranges

<details>
<summary>Tasks</summary>

1. Produce the catalog from `src/` at writing time (a throwaway PowerShell script under `artifacts/`, not committed): every `ActivitySource` name and `Meter` name, every instrument name with unit and tags, and the `ActivityTagNames` constants.
2. Produce the per-subsystem table (package, source, meter, main instruments, where registered in `WithEncina`) from that catalog; the first draft can come from the local model, and every row is checked against the code.
3. Read `EventIdRanges.GetAllRanges()` and the area comments of the registry to describe the range system (how to pick a range, the architecture test `EncinaEventIdAllocationTests`, `EventIdUniquenessRule`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
Encina is a pre-1.0 .NET 10 library. Issue #906 asks for an OpenTelemetry page. Names of sources, meters and instruments are being renamed under ADR-054 (implementation issues #1787 and #1790 are open), so the catalog must come from the code at the time you write. Documentation only; write no production code.

TASK:
1. Search src/ for ActivitySource and Meter definitions and for AddSource/AddMeter calls in src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs; build a catalog (name, package, instruments with unit and tags).
2. Read src/Encina.OpenTelemetry/ActivityTagNames.cs and Enrichers/*.cs for the tags.
3. Read src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs and src/Encina.Testing.Architecture/EventIdUniquenessRule.cs.
4. Keep the scripts under artifacts/ (git-ignored); only the page is committed.

KEY RULES:
- Do not type counts as facts: record the command and the date.
- Where the code and ADR-054 differ, record the code name and the pending rename.
- English only; PowerShell for scripts; edit repository files only with the Edit/Write tools.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, ActivityTagNames.cs, Enrichers/, the instrumented store folders
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/architecture/adr/021-eventid-uniqueness-enforcement.md, 054-observability-conventions-and-trace-propagation.md
```

</details>

### Phase 2: Page Content

<details>
<summary>Tasks</summary>

1. Create `docs/features/observability.md` with front matter `title: "Observability"`, `layout: default`, `parent: "Features"`; the opening sentence names the reader (a developer who runs an Encina application in production) and what they get.
2. Sections: overview (traces, metrics, logs; the library depends on the OpenTelemetry API, the application chooses SDK and exporters); setup; traces; metrics; structured logging and EventId ranges; per-subsystem table; exporters; the local stack; custom instrumentation under Encina's conventions; sampling and overhead notes (no cost without a listener, ADR-054).
3. A Mermaid diagram of the pipeline from handler to exporter.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
The catalog of Phase 1 exists. Issue #906 acceptance: all emitted traces and metrics cataloged, the EventId range system documented, at least 3 exporter configurations shown and run, the local stack referenced with the correct path, links from the README and the features index.

TASK:
1. Read .claude/skills/encina-docs/SKILL.md and write the compass verdict before writing.
2. Write docs/features/observability.md with the sections of the plan and the Mermaid diagram.
3. Take every name from the catalog and the sources; do not retype names from memory.
4. Reference .github/observability/ with the exact files and commands, and state which stack each docker compose command starts.

KEY RULES:
- English only; junior-readable; link ADR-021 and ADR-054 instead of restating them.
- Samples show the error code or exception type, never EncinaError.Message.
- State that no exporter is wired into WithEncina() and that no telemetry goes to the project.
- Do not describe dashboards that do not exist (#187) or an OTLP wiring that does not exist (#1043).
- Pre-1.0: no migration notes.

REFERENCE FILES:
- src/Encina.OpenTelemetry/README.md, ServiceCollectionExtensions.cs, EncinaOpenTelemetryOptions.cs
- .github/observability/ (README.md, docker-compose.observability.yml, otel-collector-config.yaml, prometheus.yml)
- docs/features/abac/reference/observability.md
```

</details>

### Phase 3: Exporter Configurations Run and Verification

<details>
<summary>Tasks</summary>

1. Write and run three exporter configurations in a scratch project outside the repository: OTLP to the collector of `.github/observability/`, Console, and Prometheus; paste what you actually saw into the page where it says what to expect.
2. Add Jaeger and Azure Monitor from the package README only if their snippets compile against the packages named; Zipkin and AWS X-Ray only with a verified configuration.
3. Compile every sample; run `lychee --offline --config .github/lychee.toml 'docs/**/*.md' 'README.md'`, `npx --yes markdownlint-cli2 "docs/**/*.md" "#docs/INVENTORY.md"` and the `cov-docs-render.cs --check-dangling` command of the encina-docs skill; name any missing tool and check links with `Test-Path`.
4. `docs-reviewer` reviews the page; fix blockers and majors.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
docs/features/observability.md is written (Phase 2). Verify it against a real run; add only what the run proves.

TASK:
1. Start the stack with docker compose -f .github/observability/docker-compose.observability.yml up -d and run the three exporter configurations (OTLP, Console, Prometheus) from a scratch project outside the repository.
2. Replace each expected-output placeholder in the page with what you saw; remove any configuration you could not run.
3. Run the link, lint and citation checks of .claude/skills/encina-docs/SKILL.md section 5, then docs-reviewer, and fix blockers and majors.
4. Stop the stack: docker compose -f .github/observability/docker-compose.observability.yml down.

KEY RULES:
- Never claim an exporter works without having run it.
- Name any missing tool in the report; do not skip a check silently.
- Keep the scratch project and logs out of the repository (artifacts/ is git-ignored).

REFERENCE FILES:
- docs/features/observability.md
- .github/observability/ (all files)
- .claude/skills/encina-docs/SKILL.md (sections 5 and 6)
```

</details>

### Phase 4: Navigation, README Cross-References and Finalization

<details>
<summary>Tasks</summary>

1. `docs/features/index.md`: add `- [Observability](observability.md)` in the Core section.
2. `README.md`: link the page from the OpenTelemetry Integration section and mention `WithEncina()`.
3. `src/Encina.OpenTelemetry/README.md`: link the page. Package README edits go through `docs-writer`.
4. `changelog.d/906-observability-docs.added.md` (written by `mechanical-fixer`).
5. Check `docs/INVENTORY.md` and update it only if it lists feature pages.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
The observability page is written and verified. Wire it into the navigation and the READMEs.

TASK:
1. Add the Observability entry to docs/features/index.md and link the page from the OpenTelemetry Integration section of README.md (and show WithEncina()).
2. Add a link to the page from src/Encina.OpenTelemetry/README.md.
3. Ask mechanical-fixer, naming the worktree path, for changelog.d/906-observability-docs.added.md.
4. Run changelog-fragments.cs --check and the link and lint checks again.

KEY RULES:
- Never edit CHANGELOG.md.
- Keep the READMEs short: link, do not copy the page.
- No AI attribution in commits.

REFERENCE FILES:
- docs/features/index.md, README.md (OpenTelemetry Integration), src/Encina.OpenTelemetry/README.md
- changelog.d/README.md, .github/scripts/changelog-fragments.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Specification | Relevance |
|--------------------------|-----------|
| ADR-054 (observability conventions and trace propagation) | Source, meter and instrument naming, error status, tenant tag rule, no exporter wired into `WithEncina()` |
| ADR-021 (EventId uniqueness enforcement) | Registry in `EventIdRanges.cs`, the architecture rule that enforces it |
| OpenTelemetry semantic conventions (.NET `ActivitySource` and `Meter`) | Names, units and attributes the catalog is checked against |
| `AGENTS.md` sections 3, 7 and 9, SPEC-001 | Error message rule; EventId workflow; no hand-typed figures |
| SPEC-000, SPEC-002 DEC-010 | No telemetry sent to the project |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AddEncinaOpenTelemetry`, `WithEncina`, `AddEncinaInstrumentation` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | Setup section |
| `EncinaOpenTelemetryOptions` | `src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs` | Options table |
| `ActivityTagNames` and the enrichers | `src/Encina.OpenTelemetry/ActivityTagNames.cs`, `Enrichers/` | Semantic attribute table |
| Instrumented stores and metrics initializers | `src/Encina.OpenTelemetry/` subfolders (MessagingStores, QueryCache, Repository, UnitOfWork, Tenancy, Modules, Audit, Sharding, Migrations, Resharding, SoftDelete, IdGeneration, BulkOperations, ReferenceTable, Cdc) | Per-subsystem table |
| `EventIdRanges` and `GetAllRanges()` | `src/Encina/Diagnostics/EventIdRanges.cs` | EventId range system section |
| `EventIdUniquenessRule` | `src/Encina.Testing.Architecture/EventIdUniquenessRule.cs` | Enforcement description |
| Package README | `src/Encina.OpenTelemetry/README.md` | Jaeger, Prometheus and Azure Monitor snippets to verify and link |
| Local observability stack | `.github/observability/` | Exporter verification and dashboards-setup section |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.OpenTelemetry | 7000-7099 (`EventIdRanges.OpenTelemetry`) | Existing; documented only, no new EventId |
| All packages | 91 registered ranges (`EventIdRanges.GetAllRanges()`, counted 2026-11-11) | The page explains the system; it adds no range |

No new range is needed: this plan adds no logging.

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Feature page | 1 | `docs/features/observability.md` |
| Navigation entry (`docs/features/index.md`) | 1 | Core section |
| README cross-references | 2 | Root `README.md` and `src/Encina.OpenTelemetry/README.md` |
| Changelog fragment | 1 | `changelog.d/906-observability-docs.added.md` |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 library (C# 14, nullable enabled) with Railway Oriented Programming (Either<EncinaError, T>). Issue #906 asks for an OpenTelemetry page. Encina.OpenTelemetry depends on the OpenTelemetry API only; the application chooses the SDK and exporters. Names are being renamed under ADR-054, so everything comes from the code at writing time. Documentation only; write no production code. Follow AGENTS.md and .claude/skills/encina-docs/SKILL.md.

IMPLEMENTATION OVERVIEW:
One pull request of 5 files: docs/features/observability.md (setup, traces, metrics, EventId ranges, per-subsystem table, three exporter configurations that were run, the local stack, custom instrumentation), a line in docs/features/index.md, links in README.md and src/Encina.OpenTelemetry/README.md, and changelog.d/906-observability-docs.added.md (mechanical-fixer). Verify with the commands of the encina-docs skill, section 5, and review with docs-reviewer.

KEY PATTERNS:
- just-the-docs front matter (layout: default, parent: "Features").
- The catalog of sources, meters and instruments is generated from src/ at writing time; counts are written with date and command.
- Where code and ADR-054 differ, document the code and flag the pending rename (#1787, #1790).
- docker compose -f .github/observability/docker-compose.observability.yml starts the OpenTelemetry stack; the profile observability of docker-compose.yml starts a smaller stack.
- Samples record the error code or exception type, never EncinaError.Message.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ (ServiceCollectionExtensions.cs, EncinaOpenTelemetryOptions.cs, ActivityTagNames.cs, Enrichers/, README.md)
- src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Testing.Architecture/EventIdUniquenessRule.cs
- .github/observability/
- docs/architecture/adr/021-eventid-uniqueness-enforcement.md, 054-observability-conventions-and-trace-propagation.md
- docs/features/index.md, README.md
```

</details>

---

## Cross-Cutting Integration Matrix

> Per `AGENTS.md` section 6 (ADR-018): documentation only, so no runtime function is added. A function the page describes is marked ✅ as page content.

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No runtime code; the instrumented cache provider is a row of the per-subsystem table. |
| 2 | OpenTelemetry | ✅ | This is the subject of the page. |
| 3 | Structured Logging | ✅ | EventId ranges and the `[LoggerMessage]` pattern are documented as page content. |
| 4 | Health Checks | ❌ | No runtime code; health checks that emit telemetry (schema drift, resharding) are only listed. |
| 5 | Validation | ❌ | No runtime code. |
| 6 | Resilience | ❌ | No runtime code; the retry counters are linked to the resilience page of #904. |
| 7 | Distributed Locks | ❌ | No runtime code. |
| 8 | Transactions | ❌ | No runtime code. |
| 9 | Idempotency | ❌ | No runtime code. |
| 10 | Multi-Tenancy | ✅ | The tenant tag rule (only when multi-tenancy is on, ADR-054) is documented as page content. |
| 11 | Module Isolation | ❌ | No runtime code; the modules source is a row of the per-subsystem table. |
| 12 | Audit Trail | ❌ | No runtime code; the audit source is a row of the per-subsystem table. |

---

## Prerequisites & Dependencies

- No code prerequisite. The names change when #1787 and #1790 land; the page is refreshed by those pull requests' documentation phase.
- A Docker engine is needed for the exporter runs of Phase 3.
- #1043 (OTLP exporter as opt-in) and #187 (dashboard templates) are open; the page does not describe them as existing.

---

## Next Steps

1. Brief a `docs-writer` with Phase 1 to Phase 4 (one pull request), after re-checking the state of #1787 and #1790.
2. Link this plan from #906.
