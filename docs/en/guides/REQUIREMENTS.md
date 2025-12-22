# Encina Requirements Guide

Last updated: 2025-12-08

## Purpose

- Capture the functional and quality requirements that govern Encina.
- Describe how each requirement is validated so contributors can run the right checks before merging.
- Provide a repeatable process for documenting new requirements and keeping the traceability matrix current.

## Requirement Catalogue

| Requirement ID | Category | Summary | Primary Owner |
|----------------|----------|---------|---------------|
| `REQ-REQ-LIFECYCLE` | Functional | Mediator handles command lifecycle (success, domain errors, cancellations). | Core Team |
| `REQ-REQ-QUERY` | Functional | Queries execute deterministically and preserve pipeline ordering. | Core Team |
| `REQ-REQ-PIPELINE` | Functional | Pipeline behaviors, pre-processors, and post-processors are registered and invoked correctly. | Core Team |
| `REQ-REQ-CONCURRENCY` | Functional | Concurrent send/publish operations are safe and do not lose notifications. | Core Team |
| `REQ-NOT-ORDER` | Functional | Notifications honour handler ordering rules and cancellation semantics. | Core Team |
| `REQ-NOT-MULTI` | Functional | Multiple notification handlers can be registered without duplication. | Core Team |
| `REQ-INF-METRICS` | Quality-of-Service | Metrics capture success/failure outcomes and relevant tags. | Observability WG |
| `REQ-ERR-ZEROEXC` | Quality-of-Service | Mediator operations surface expected failures through functional results rather than exceptions. | Reliability WG |
| `REQ-CONF-LIFETIME` | Quality-of-Service | Dependency injection lifetimes and resolution semantics are configurable. | Core Team |
| `REQ-CONF-SCAN` | Quality-of-Service | Assembly scanning falls back gracefully and caches discoveries safely. | Core Team |
| `REQ-CONF-EDGE` | Quality-of-Service | Configuration extensions handle nulls, duplicates, and multi-assembly scenarios. | Core Team |
| `REQ-QUAL-MUTATION` | Quality Gate | Mutation score remains ≥93.74% on the main branch. | Quality Guild |
| `REQ-PERF-BASELINE` | Quality Gate | Benchmarks respect latency/allocation guardrails (≤15% latency regression, ≤25% allocation spike). | Performance WG |
| `REQ-LOAD-THROUGHPUT` | Quality Gate | Load harnesses sustain required throughput within CPU/memory envelopes. | Reliability WG |

> ℹ️ See `docs/en/guides/REQUIREMENTS_MAPPING.md` for detailed traceability to code, tests, and automation.

## Traceability Overview

- **Unit & property suites** – execute via `dotnet test Encina.slnx --configuration Release`. Coverage results must stay ≥90% line coverage (see `docs/en/guides/TESTING.md`).
- **Mutation testing** – run `dotnet run --file scripts/run-stryker.cs`; refresh the badge with `scripts/update-mutation-summary.cs`. CI enforces the 93.74% baseline.
- **Zero Exceptions policy** – unit suites in `tests/Encina.Tests` (for example, `EncinaTests` and `PipelineBehaviorsTests`) assert mediator operations return functional results instead of throwing for expected failures; property suites reuse the same result patterns when exploring pipeline invariants.
- **Benchmarks** – launch `scripts/run-benchmarks.cs`, then fail builds if `scripts/check-benchmarks.cs` detects regressions against the baseline snapshots.
- **Load harnesses** – the console harness uses `scripts/check-load-metrics.cs -- --config ci/load-thresholds.json`; NBomber profiles are summarised via `scripts/summarize-nbomber-run.cs -- --thresholds ci/nbomber-thresholds.json`.

## Adding or Updating Requirements

1. **Define the requirement** – capture the scope, success criteria, and owner. Assign a new `REQ-` identifier (prefix by domain, e.g., `REQ-LOAD-*`).
2. **Update this guide** – add the requirement to the catalogue table with a concise summary.
3. **Extend the mapping** – edit `docs/en/guides/REQUIREMENTS_MAPPING.md` to link the requirement to tests, scripts, or workflows.
4. **Add guardrails** – if the requirement introduces a quality gate, update CI or scripts to enforce it. Document the workflow in the relevant guide (e.g., testing, performance, load).
5. **Log the change** – capture the update in `docs/internal/testing-roadmap.md` so the roadmap reflects new coverage.

## Quality Gates & CI Hooks

| Area | Command / Script | Threshold |
|------|------------------|-----------|
| Coverage | `dotnet test` + reportgenerator | ≥90% line coverage |
| Mutation | `scripts/run-stryker.cs` | ≥93.74% mutation score |
| Benchmarks | `scripts/check-benchmarks.cs` | ≤15% latency regression, ≤25% allocation regression |
| Load (console) | `scripts/check-load-metrics.cs -- --config ci/load-thresholds.json` | CPU ≤3%, peak working set ≤160 MB, send/publish throughput ≥ configured mins |
| Load (NBomber) | `scripts/summarize-nbomber-run.cs -- --thresholds ci/nbomber-thresholds.json` | Send-burst throughput ≥6.75M ops/sec, latency ≤0.85 ms |

## Maintainer Checklist

- [ ] Review this guide when new features land to ensure requirements remain current.
- [ ] Keep the mapping document aligned with new suites or scripts.
- [ ] Periodically audit CI outputs to confirm all guardrails are firing as expected.
- [ ] Surface any requirement deltas in the roadmap progress log so stakeholders stay informed.
