# SimpleMediator Quality & Testing Roadmap

Last updated: 2025-12-06

## Overview

This roadmap tracks the effort to bring SimpleMediator to the same multi-layered testing maturity as the vacations pilot. It captures scope, current status, and next actions so the work can advance incrementally without losing context.

## Objectives

- Establish comprehensive automated coverage across unit, property-based, contract, integration, mutation, and performance suites.
- Provide reproducible tooling (coverage, benchmarks, mutation) with documented execution steps and quality gates.
- Maintain a live log of progress so each iteration updates status, outcomes, and upcoming work.

## Workstreams & Status

| Area | Scope | Owner | Status | Notes |
|------|-------|-------|--------|-------|
| Foundation | Relocate imported reference docs, update `.gitignore`, create roadmap doc | Copilot | ✅ Done | Zip moved to `.backup/`, roadmap created |
| Coverage Baseline | Collect current `dotnet test` coverage report and archive results | Copilot | ✅ Done | Release run on 2025-12-06 — 89.1% line / 68.4% branch (see `artifacts/coverage/latest`) |
| Unit Expansion | Increase coverage for mediator core, behaviors, metrics | Copilot | ⏳ Planned | Target ≥90% lines for `SimpleMediator` namespace |
| Property Tests | Create `SimpleMediator.PropertyTests` with FsCheck generators | Copilot | 🚧 In progress | Configuration, pipeline determinism, and notification publish ordering covered |
| Contract Tests | Ensure handlers/behaviors honor interfaces across implementations | Copilot | 🚧 In progress | DI registrations cover pipelines, handlers, and processors |
| Mutation Testing | Configure Stryker.NET thresholds and CI integration | Copilot | ⏳ Planned | Thresholds: high 85, low 70, break 65 |
| Performance Benchmarks | Add BenchmarkDotNet project & publish results doc | Copilot | ⏳ Planned | Measure send/publish overhead & allocations |
| Load Harness | Prototype NBomber (or console) throughput tests | Copilot | ⏳ Planned | Document CPU/memory requirements |
| Documentation | Publish guides (`docs/en/guides`) & requirements mapping | Copilot | 🚧 In progress | Testing guide drafted; map remaining docs |

Status legend: ✅ Done · 🚧 In progress · ⏳ Planned · ⚠️ Blocked

## Progress Log

- **2025-12-06** — Imported reference documentation moved to `.backup/`; roadmap established.
- **2025-12-06** — Captured Release coverage baseline (lines 89.1%, branches 68.4%); reports stored in `artifacts/coverage/latest`.
- **2025-12-06** — Property test suite validates configuration dedup/order and pipeline composition invariants with FsCheck; all tests green.
- **2025-12-06** — Added outcome-aware pipeline properties verifying behavior under success, exception, and cancellation flows.
- **2025-12-06** — Handler determinism validated via property tests with instrumented pipeline and simulated outcomes.
- **2025-12-06** — Input generators bounded to keep property-based exploration focused while preserving coverage variance.
- **2025-12-06** — Contract test project introduced to enforce pipeline interface contracts via reflection.
- **2025-12-06** — Authored testing guide (`docs/en/guides/TESTING.md`) covering suites, commands, and coverage workflow.
- **2025-12-06** — Contract suite validates DI registration for specialized pipelines and configured request processors.
- **2025-12-06** — Handler registration contracts ensure scoped lifetime, deduplication, and multi-notification support.
- **2025-12-06** — Notification properties assert publish ordering, fault propagation, and cancellation semantics.

## Upcoming Actions

1. Outline remaining documentation deliverables (requirements mapping, mutation/perf guides).
2. Design contract coverage for mediator configuration edge cases (e.g., multiple assembly scans).
3. Introduce mutation testing scaffolding with Stryker.NET configuration template.

---

_Keep this roadmap updated at each milestone: add log entries, adjust statuses, and expand scope as needed._
