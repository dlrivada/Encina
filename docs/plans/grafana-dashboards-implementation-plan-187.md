# Implementation Plan: Pre-built Grafana Dashboard Templates

> **Issue**: [#187](https://github.com/dlrivada/Encina/issues/187)
> **Type**: Feature (observability, documentation assets)
> **Related**: ADR-054 (settled point 1 dashboards stay outside `WithEncina()`, settled point 4 instrument names, D4 tenant tag cardinality), #2678 (renames every instrument: the dashboards must use the final names), #2677 (meter names), #174 (request and messaging instruments, sibling plan), #906 (observability guide), #180 (Prometheus package), ADR-062
> **Complexity**: Medium (JSON is easy; keeping it true to the names the code really exports is the work)
> **Estimated Scope**: 5 dashboard files + 1 alert rules file + ~250 lines of tests + 1 documentation page, in two pull requests of at most 15 files and 1500 lines each

---

## Summary

### What exists today (read on 2026-11-20, main `098caa920`)

- There is no dashboard asset: `docs/observability/` does not exist and no `*.json` Grafana model is tracked. ADR-054 settled point 1 names #187 as the owner of dashboards and keeps them out of `WithEncina()`.
- **The metric names in the issue do not exist.** The issue queries `encina_requests_total`, `encina_errors_total`, `encina_request_duration_seconds_bucket`, `encina_outbox_pending_count`, `encina_saga_completed_total`. The code exports `Encina.request.success`, `Encina.request.failure`, `Encina.request.duration` in milliseconds (`src/Encina/Diagnostics/IEncinaMetrics.cs:47-50`), `encina.outbox.processor.messages_total` (`OutboxProcessorMetrics`), `encina.scheduling.processor.cycle_duration_seconds`, `encina.querycache.hits_total` and `encina.repository.operations_total` (names with `_total` and `_ms` that ADR-054 settled point 4 and #2678 will rename), and `Encina.db.pool.connections.active` (`DatabasePoolMetrics.cs:52`). There is no `encina.saga.completed` instrument at all.
- Some instruments never emit today (`MessagingStoreMetrics`, wired by the plan of #174).
- The Prometheus exporter appends `_total` to counters and the unit to histograms (`s` becomes `_seconds`), replaces dots by underscores, and drops the case: a name in a PromQL query is a function of instrument name, kind and unit.
- No test ties a dashboard to the code.

### What the issue proposes and what changed since

Five dashboards (overview, requests, messaging, caching, database), a variable for the service, alert rule templates and import instructions. Nothing in the proposal is wrong in intent; the queries are, and the instrument names are about to change (#2678), so the dashboards must be built after the rename and checked by a test.

### What this plan delivers

- `docs/observability/grafana/` with `encina-overview.json`, `encina-requests.json`, `encina-messaging.json`, `encina-caching.json`, `encina-database.json` and `docs/observability/prometheus/encina-alerts.yml`, each panel naming an exported series.
- A test (`GrafanaDashboardMetricNamesTests`) that creates the real instruments, lists them with a `MeterListener`, translates each to its Prometheus series name by the exporter's rules and fails when a dashboard or alert expression names a series that does not exist.
- Variables `service` (the `service_name` resource attribute, `job`) and `instance`; a tenant variable only when the series carries one (ADR-054 D4, #2676).
- An import page (through `docs-writer`).
- Shared pieces and owner: this plan owns the dashboard assets; #174 and #2678 own the names; #2676 owns the tenant attribute; #906 owns the guide that links the page.
- Affected packages: none (documentation assets and one test in `Encina.UnitTests`). Provider category: observability.
- Not delivered: an "Infrastructure (CPU, memory, GC)" dashboard of Encina series (those come from `OpenTelemetry.Instrumentation.Runtime`, a host package; the overview links its panels by name only, ADR-054 D6 point 2), traces panels (Grafana Tempo is the host's), a tenant filter (Design Choice 4).

---

## Design Choices

<details>
<summary><strong>1. When the dashboards are built relative to the instrument rename</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) After #2678 and the #174 pull requests, so the series names are final** | Built once; the consistency test passes on final names | Delivery waits for #2678 |
| **B) Now, on today's names, updated by #2678** | Earlier delivery | Every panel changes once more; #2678's own proposed fix lists "update dashboards" as a task |

### Chosen Option: **A — After the rename** (recommended, pending the maintainer)

### Rationale

- A wins because a dashboard on names that are about to change is rework; B wins if the maintainer needs dashboards before 0.14.0 and accepts the churn. The test of Phase 2 makes either order safe: a stale panel fails CI.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the assets live</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `docs/observability/grafana/` and `docs/observability/prometheus/`** | The issue's own path; published with the documentation site, importable by URL | Non-page files under `docs/` |
| **B) A top-level `deploy/` folder** | Separates assets from pages | A new top-level folder for six files |

### Chosen Option: **A — Under `docs/observability/`**

### Rationale

- The issue states the path and the site already hosts data files; the import page links them.

**Class:** Trivial
**Settled by:** the issue text of #187 ("docs/observability/grafana/")

</details>

<details>
<summary><strong>3. How a dashboard is kept true to the code</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A unit test lists the real instruments with a `MeterListener`, translates names by the Prometheus exporter rules and checks every series the dashboards and alerts reference** | A renamed or removed instrument breaks CI; no hand-kept list | The translation rules are code in a test |
| **B) Reviewers check by eye** | No code | The failure the issue itself shows (names that do not exist) |

### Chosen Option: **A — The consistency test**

### Rationale

- `AGENTS.md` section 9 requires tests to execute real package code and section 11 forbids leaving an identified problem unrecorded; the issue's own queries are the problem, and only a test prevents its return.

**Class:** Settled
**Settled by:** `AGENTS.md` section 9 (tests execute real code) and the defect in the issue's own queries (verified in Summary); option B has no mechanism

</details>

<details>
<summary><strong>4. Tenant filter and cardinality</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No tenant variable until the series carries a tenant attribute (#2676); document the cardinality warning** | Honest: no panel filters on a label that is absent | The issue's "tenant filter" row waits |
| **B) A tenant variable now** | Visible | Filters on a label that does not exist; ADR-054 D4 emits it only when multi-tenancy is on |

### Chosen Option: **A — Wait for #2676**

### Rationale

- ADR-054 D4 makes the tenant tag conditional and adds it through one helper in #2676. A tenant variable is added by a later change to the dashboards, caught by the test.

**Class:** Settled
**Settled by:** ADR-054 D4 (the tenant is a metric tag only when multi-tenancy is enabled; the helper is #2676); option B filters on an absent label

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code and no runtime code, so no ADR-062 `ErrorCategory`; no EventId | Settled | ADR-062 |
| C2 | The pending-outbox panel uses `max by (service_name)`: every host instance reports the same database count (plan of #174, row C7) | Settled | A global count read by N instances |
| C3 | Error rate panels compute `failure / (success + failure)` while the two request counters exist; if #2678 or the maintainer merges them into one counter with an outcome attribute, the expression changes with the test | Trivial | Plan of #174, Design Choice 2 |
| C4 | Alert rules: error ratio above 5 percent for 10 minutes, p99 request latency above 1 second for 10 minutes, outbox pending count above 1,000 for 15 minutes, with the thresholds as template values the host edits | Trivial | The issue's "alert rule templates"; thresholds are examples, not policy |
| C5 | Dashboard JSON uses a datasource variable (`${DS_PROMETHEUS}`), no hard-coded datasource uid or host, and no personal data | Settled | `AGENTS.md` section 3 (nothing environment-specific or personal in assets) |
| C6 | Public names: none (assets and a test) | Trivial | No code API |

---

## Implementation Phases

### Phase 1: The Consistency Test (pull request G1)

> **Goal**: A test that knows every exported series, written before the JSON so the JSON is built against it.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `tests/Encina.UnitTests/Observability/PrometheusSeriesNames.cs` (a helper: `Translate(name, kind, unit)` lowercases, replaces dots, appends the unit suffix for `s` and `ms` and `_total` for counters, `_bucket/_sum/_count` for histograms) and `tests/Encina.UnitTests/Observability/GrafanaDashboardMetricNamesTests.cs` (builds `EncinaMetrics`, `MessagingStoreMetrics`, `OutboxProcessorMetrics.Instance` and the other instrument holders the dashboards use, listens through `MeterListener.InstrumentPublished`, collects series names, then reads `docs/observability/**` (`*.json` expressions and `*.yml` rules), extracts every `encina_`-prefixed metric token and asserts it is in the set; also asserts each file is valid JSON or YAML and contains no `uid` of a datasource).
2. Add `changelog.d/187-dashboard-consistency-test.added.md` through `mechanical-fixer` (only when Phase 2 ships assets in the same release; otherwise folded into Phase 2).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #187, plan docs/plans/grafana-dashboards-implementation-plan-187.md, phase 1.
The issue's PromQL names do not exist in the code; a test must tie dashboards to real instruments.

TASK:
1. Create PrometheusSeriesNames (instrument name, kind, unit -> exported series) and GrafanaDashboardMetricNamesTests that lists real instruments
   with a MeterListener and checks every encina_ token in docs/observability/**.

KEY RULES:
- Tests execute real instrument holders; no hand-kept name list. The path is resolved from the repository root, not hard-coded.

REFERENCE FILES:
- src/Encina/Diagnostics/IEncinaMetrics.cs, src/Encina.Messaging/Diagnostics/*Metrics.cs
```

</details>

---

### Phase 2: Dashboards and Alerts (pull request G2)

> **Goal**: Five importable dashboards and the alert rules.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** (all new) `docs/observability/grafana/encina-overview.json` (request rate, error ratio, p99 latency, active requests, outbox pending), `encina-requests.json` (latency histogram by `request_name`, failures by `failure_reason`), `encina-messaging.json` (outbox processor, retries, inbox duplicates, saga duration, scheduling cycle), `encina-caching.json` (query cache hits, misses, evictions; cache provider series that exist), `encina-database.json` (repository operations and errors, pool connections), `docs/observability/prometheus/encina-alerts.yml`. Variables: datasource, `service_name`, `instance`. Every series is one the Phase 1 test accepts; a panel for an instrument that does not emit (for example saga active count, #174 follow-up) is left out, not faked.
2. Add `changelog.d/187-grafana-dashboards.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #187 plan docs/plans/grafana-dashboards-implementation-plan-187.md, phase 2. Phase 1 merged; #2678 and the #174 pull requests merged.

TASK:
1. Write the five dashboard JSON files and the alert rules under docs/observability/, using only series the consistency test accepts.

KEY RULES:
- Datasource and service are variables; no datasource uid, host or personal data. The pending outbox panel uses max by (service_name).
- No panel for an instrument that does not emit.

REFERENCE FILES:
- tests/Encina.UnitTests/Observability/GrafanaDashboardMetricNamesTests.cs, docs/plans/request-metrics-implementation-plan-174.md
```

</details>

---

### Phase 3: Import Documentation (pull request D1)

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/observability/grafana-dashboards.md` through `docs-writer` (how-to): import steps, the variables, the alert thresholds as examples, the `max by` rule, the tenant cardinality warning (ADR-054 D4), that the host chooses the exporter (ADR-054 settled point 1).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #187 plan docs/plans/grafana-dashboards-implementation-plan-187.md, phase 3, documentation only.

TASK:
1. Write docs/observability/grafana-dashboards.md (how-to). Verify every identifier and series name exists.

KEY RULES:
- encina-docs skill; no emojis; no hand-typed coverage figures.

REFERENCE FILES:
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| OpenTelemetry Prometheus compatibility | Dots become underscores, unit suffix, `_total` on counters | Series translation |
| Prometheus naming practices | Base units (seconds), suffixes | Panel units |
| Grafana dashboard JSON model | Panels, variables, datasource variable | Assets |
| ADR-054 | Settled points 1 and 4, D4 | Scope, names, tenant |
| ADR-062 | Error category contract | None created (row C1) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EncinaMetrics` | `src/Encina/Diagnostics/IEncinaMetrics.cs` | Request series |
| `OutboxProcessorMetrics`, `SchedulingProcessorMetrics` | `src/Encina.Messaging/Diagnostics/` | Messaging series |
| `MessagingStoreMetrics` | `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs` | Wired by #174 |
| `DatabasePoolMetrics` | `src/Encina/Diagnostics/DatabasePoolMetrics.cs` | Pool series |
| `docs/observability/` | none today | New folder |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No runtime code; nothing taken from 5970-5999 |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| G1 | Consistency test | 0 | 0 | 2 | 0 | 0 | 1 (changelog) | 3 | ~260 |
| G2 | Dashboards and alerts | 0 | 0 | 0 | 0 | 0 | 7 (5 JSON, 1 YAML, changelog) | 7 | ~1,100 |
| D1 | Import page | 0 | 0 | 0 | 0 | 0 | 1 | 1 | ~100 |
| **Total** | | **0** | **0** | **2** | **0** | **0** | **9** | **11** | **~1,460** |

The columns sum (2 + 9 = 11; 3 + 7 + 1 = 11). No production code, so no coverage manifest entry and no PublicAPI line. The pull request of this plan is one file.

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `tests/.../GrafanaDashboardMetricNamesTests.cs` | n/a | n/a | n/a | n/a | A test file, not a `src/` file; it carries no per-file target; the assets have none either (no executable code) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #187 - Grafana dashboards for Encina - in three small pull requests.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0; edit files only with Edit/Write; changelog through mechanical-fixer; the import page through docs-writer.
- ADR-054: dashboards stay outside WithEncina(); instrument names follow settled point 4; the tenant tag is #2676's.

IMPLEMENTATION OVERVIEW:
1. G1: PrometheusSeriesNames and GrafanaDashboardMetricNamesTests (real instruments via MeterListener).
2. G2: five dashboards and encina-alerts.yml under docs/observability/, after #2678 and #174.
3. D1: docs/observability/grafana-dashboards.md.

KEY PATTERNS:
- Every series in an asset must exist; no panel for an instrument that does not emit; datasource and service are variables.

REFERENCE FILES:
- src/Encina/Diagnostics/IEncinaMetrics.cs, docs/plans/grafana-dashboards-implementation-plan-187.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Static JSON assets |
| 2 | OpenTelemetry | ✅ Phases 1-2 | The dashboards visualize the metrics Encina emits |
| 3 | Structured Logging | ❌ N/A | No runtime component |
| 4 | Health Checks | ❌ N/A | Dashboards display health data; they are not checks |
| 5 | Validation | ❌ N/A | The test validates the assets' syntax; there is no runtime input |
| 6 | Resilience | ❌ N/A | No runtime component |
| 7 | Distributed Locks | ❌ N/A | No runtime component |
| 8 | Transactions | ❌ N/A | No runtime component |
| 9 | Idempotency | ❌ N/A | No runtime component |
| 10 | Multi-Tenancy | ⏭️ #2676 | A tenant variable follows the tenant attribute (Design Choice 4) |
| 11 | Module Isolation | ⏭️ Follow-up issue file | `module-id-on-request-telemetry.md` (written by the #174 plan); a module variable follows it |
| 12 | Audit Trail | ❌ N/A | Documentation asset |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #2678 instrument rename | Open | G2 waits for it (Design Choice 1) |
| Plan #174 pull requests | Planned | Wire the messaging instruments the dashboards show |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Tenant attribute | #2676 | Tenant variable |
| Observability guide | #906 | Links the page |
| Prometheus package | #180 | Exposes the series |

---

## Next Steps

1. Link this plan from #187 and ask the maintainer to answer Design Choice 1 when work starts.
2. Implement G1 any time, G2 after #2678 and the #174 pull requests, then D1.
