# Implementation Plan: `Encina.Dashboard` — local web UI for debugging Encina applications

> **Issue**: [#445](https://github.com/dlrivada/Encina/issues/445)
> **Type**: Feature (new package, developer experience)
> **Complexity**: High (6 phases, read-side additions to stores, 7 pull requests)
> **Estimated Scope**: ~2,400 lines of production code + ~2,000 lines of tests + ~300 lines of documentation
> **Family**: dev-experience. This plan owns the package `Encina.Dashboard` and the EventIds 70060-70089. The error panel may reuse `Encina.Diagnostics` (#441); a dead-letter admin API is #2229 (AspNetCore).

---

## Summary

A local, read-only-by-default web dashboard mapped with `app.MapEncinaDashboard("/encina", options)`: overview, handlers, pipeline, outbox, inbox, sagas, cache and errors panels, refreshed in real time, disabled outside development unless explicitly enabled, authenticated, with sensitive data redacted.

What exists today (read on `origin/main` after `git fetch`):

- No dashboard package. Related: `Encina.SignalR` (server-side hub support, `src/Encina.SignalR`), `Encina.AspNetCore` (endpoint extensions, health), and the pipeline inspector `IEncinaPipelineInspector.DescribeAsync` (`src/Encina/Pipeline/Ordering/IEncinaPipelineInspector.cs`) which already describes the ordered steps of a request type.
- Metrics: `IEncinaMetrics` has only `TrackSuccess` and `TrackFailure` (`src/Encina/Diagnostics/IEncinaMetrics.cs`); it exposes no read API. Aggregation must come from a `MeterListener` on the `Encina*` meters (ADR-054 D3: PascalCase `Encina.*` meter names, one per package).
- The stores have no list or browse API: `IOutboxStore` offers `GetPendingMessagesAsync`, `GetPendingCountAsync`, `GetExhaustedCountAsync`, `RequeueExhaustedAsync`; `IInboxStore` only `GetMessageAsync(id)`; `ISagaStore` `GetAsync(id)`, `GetStuckSagasAsync`, `GetExpiredSagasAsync` (`src/Encina.Messaging/{Outbox,Inbox,Sagas}`). A key browser for the cache has no abstraction either. A full viewer needs new read methods on 10 database providers (AGENTS.md section 5), which is a separate feature; this plan shows only what the interfaces expose today (Design Choice 3).
- ADR-064 will change the result carrier of every store method; the dashboard reads stores through one adapter layer (`IDashboardDataSource`) so the migration touches one place.

What this plan delivers: the package, the metrics aggregator, the data-source adapters for what exists, the panels, SignalR push, authentication and the production guard, logging, and docs.

- **Affected packages**: `Encina.Dashboard` (new, references `Encina`, `Encina.Messaging`, `Encina.AspNetCore`, `Encina.SignalR`), test projects. Core is not changed.
- **Provider category**: none for the dashboard itself; it reads existing store interfaces and so works on every provider; the missing read methods are a follow-up feature.
- **Prerequisites**: none blocking.

### Shared Pieces and Ownership (family dev-experience)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Dashboard`, `IDashboardDataSource`, `DashboardOptions` | **This plan (#445)** | none |
| EventIds 70060-70089 (`Dashboard`), block 70050-70099 | **This plan** (70050-70059 belongs to #441) | none |
| `Encina.Diagnostics` formatter for the error panel | #441 (sibling plan, by number) | this plan, optionally |

---

## Design Choices

<details>
<summary><strong>1. Frontend technology</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Static HTML + a small vanilla JS file served as embedded resources, over minimal APIs and a SignalR hub** | No build step, no Node, no extra package; works in any host; easy to test the JSON endpoints | Hand-written UI; fewer widgets |
| **B) Blazor Server** | .NET only, componentized | Needs Razor SDK, a circuit per viewer, and a second UI stack beside `Encina.AspNetCore.Blazor` |
| **C) HTMX** | Simple partial updates | A JS dependency to vendor and keep patched |

### Chosen Option: **A — Embedded static HTML and JS** (recommended, pending the maintainer)

### Rationale

- A wins for a dev-only tool that must carry no build chain; B wins if the maintainer wants component reuse with the Blazor package.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Where it may run and who may open it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Disabled unless `IHostEnvironment.IsDevelopment()` or `AllowInProduction = true` (explicit, logged); `RequireAuthentication` defaults to true and a missing authenticated principal denies (fail closed); read-only unless `EnableActions`** | Matches the issue's security section and AGENTS.md section 3 (gates fail closed, the only opt-out is explicit and logged) | A developer sets two flags to use it in a staging host |
| **B) Authentication off by default, as in the issue's sample (`RequireAuthentication = true` is shown set explicitly)** | Easiest start | Fails open when forgotten |

### Chosen Option: **A — Fail closed, explicit opt-out**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 ("Compliance and security gates fail closed: ... the only opt-out is explicit and logged") and the issue's "Security Considerations" (disabled by default in production, read-only by default).

</details>

<details>
<summary><strong>3. What the outbox, inbox, saga and cache panels can show</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Show what the interfaces expose today: outbox pending and exhausted counts, a page of pending messages (type, id, retry count, never the payload), requeue of exhausted messages as an opt-in action; saga lookup by id plus stuck and expired lists; inbox lookup by id; cache panel limited to hit and miss meters; the payload viewer and key browser are follow-ups** | Ships without touching 10 providers; honest scope | The issue's payload viewer, full lists and cache key browser are not in the first release |
| **B) Add list and browse methods to the stores and the cache abstraction now** | Meets every panel in the issue | Changes `IOutboxStore`, `IInboxStore`, `ISagaStore`, `ICacheProvider` and all implementers and fakes on 10 database providers and 8 caches: a multi-package feature of its own, not one pull request |

### Chosen Option: **A — Current interfaces only; read methods as a follow-up** (recommended, pending the maintainer)

### Rationale

- A wins for a first release inside the 15-file limit; B wins if the maintainer wants the full viewer before release and accepts a separate provider-wide plan first. The follow-up (read methods on the stores) is recorded as an issue file with this plan.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Sensitive data and tenancy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Payloads are never sent to the browser; messages show type, id, status and counts; errors show code, category and exception type with the `ForLogging()` stack trace, never `EncinaError.Message`; tenant is shown as an opaque, hashed label and a tenant filter uses the same hash; metric tenant tag only when multi-tenancy is on (ADR-054 D4)** | Follows AGENTS.md section 3 and ADR-054 D4; no PII in the browser | The "payload viewer" is dropped (also in Design Choice 3) |
| **B) Show payloads with field-level redaction rules** | More useful for debugging | Needs a redaction rule set to maintain; a missed field leaks personal data |

### Chosen Option: **A — No payloads, no messages, hashed tenant labels**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (`EncinaError.Message` never reaches logs or plaintext; `ForLogging()`), ADR-054 D4 (tenant as attribute and tag when multi-tenancy is on) and the issue's "Sensitive data redaction in payloads".

</details>

---

## Implementation Phases

### Phase 1: Package, options and guard (PR-1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Dashboard/Encina.Dashboard.csproj` (framework reference `Microsoft.AspNetCore.App`, project references above, embedded `wwwroot/**`), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, slnx, `coverage-weights.json`, manifest `Encina.Dashboard.json`.
2. `DashboardOptions.cs` (`Title`, `RefreshInterval` (`TimeSpan`), `DarkMode`, `RequireAuthentication` true, `AllowInProduction` false, `EnableActions` false), `DashboardGuard.cs` (environment and principal check, fail closed), `EndpointRouteBuilderExtensions.MapEncinaDashboard`, `ServiceCollectionExtensions.AddEncinaDashboard`.
3. Tests: guard behavior (production denied, anonymous denied, development allowed), DI test with `ValidateOnBuild` and `ValidateScopes`, guard tests for the public methods.

About 14 files, 700 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina has no dashboard. Encina.AspNetCore and Encina.SignalR show endpoint and hub conventions.
TASK: Create Encina.Dashboard with DashboardOptions, the production/authentication guard, MapEncinaDashboard and AddEncinaDashboard.
KEY RULES: Fail closed when the principal or environment is missing; the opt-out is explicit and logged. AddEncinaDashboard registers every option and dependency it resolves (DI test with ValidateOnBuild and ValidateScopes). TimeProvider for any time. At most 15 files.
REFERENCE FILES: src/Encina.AspNetCore/ServiceCollectionExtensions.cs, src/Encina.SignalR/Encina.SignalR.csproj, src/Encina.AspNetCore/ApplicationBuilderExtensions.cs.
```

</details>

### Phase 2: Metrics aggregation and data sources (PR-2)

<details>
<summary>Tasks</summary>

1. `MetricsAggregator.cs` (hosted service with a `MeterListener` on `Encina*` instruments, in-memory ring buffers, a timer callback that never throws, per AGENTS.md section 3), `IDashboardDataSource.cs` and adapters `OutboxDataSource`, `InboxDataSource`, `SagaDataSource`, `PipelineDataSource` (uses `IEncinaPipelineInspector`), each resolving stores through `IServiceScopeFactory`, never from a singleton directly; a `Left` from a store is shown as an error state, never as an empty success.
2. Tests per adapter with substituted stores; aggregator tests with a `FakeTimeProvider`.

About 14 files, 1,100 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 merged.
TASK: Add MetricsAggregator and the data-source adapters for outbox, inbox, saga and pipeline, reading only existing store methods.
KEY RULES: Never inject a scoped service into a singleton (IServiceScopeFactory). A Left from a store is surfaced as an error, not hidden. Timer callbacks catch and log by error code; ForLogging() for exceptions. No payloads. At most 15 files.
REFERENCE FILES: src/Encina.Messaging/Outbox/IOutboxStore.cs, src/Encina.Messaging/Sagas/ISagaStore.cs, src/Encina.Messaging/Inbox/IInboxStore.cs, src/Encina/Pipeline/Ordering/IEncinaPipelineInspector.cs, src/Encina/Diagnostics/IEncinaMetrics.cs.
```

</details>

### Phase 3: API, UI and SignalR (PR-3 and PR-4)

<details>
<summary>Tasks</summary>

1. PR-3: minimal API endpoints (`/api/overview`, `/api/handlers`, `/api/pipeline`, `/api/outbox`, `/api/inbox`, `/api/sagas`, `/api/errors`), GET only; the requeue action `POST /api/outbox/requeue` only when `EnableActions`, with an antiforgery-style same-origin check and an audit record through `IOperationAuditStore` when one is registered.
2. PR-4: `DashboardHub` (SignalR) pushing aggregated snapshots every `RefreshInterval`, `wwwroot/index.html`, `app.js`, `app.css` with dark mode.
3. Tests with `WebApplicationFactory`-style in-memory hosts; guard tests.

PR-3 about 13 files and 900 lines; PR-4 about 9 files and 700 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Data sources exist.
TASK: Add the read endpoints, the opt-in requeue action with audit, the SignalR hub and the static UI.
KEY RULES: Every endpoint passes the guard. Responses never carry payloads or EncinaError.Message. Tenant labels are hashed. A mutation requires EnableActions. At most 15 files.
REFERENCE FILES: src/Encina.AspNetCore/Health, src/Encina.SignalR, src/Encina.Messaging/Outbox/IOutboxStore.cs.
```

</details>

### Phase 4: Logging and EventIds (PR-5)

<details>
<summary>Tasks</summary>

1. `DashboardLog.cs` (`[LoggerMessage]`): 70060 dashboard mapped, 70061 denied (reason code), 70062 production opt-in used (Warning), 70063 data source failed (error code), 70064 action performed, 70065 aggregator cycle failed. Register `EventIdRanges.Dashboard = (70060, 70089)` in `src/Encina/Diagnostics/EventIdRanges.cs`, the PublicAPI line, the `AssemblyRanges` entry, and the registry rows (block 70050-70099 is shared with #441: its row lists both ranges).
2. Tests of the log messages; manifest edit; changelog fragment `changelog.d/445-developer-dashboard.added.md`.

About 8 files and 350 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The dashboard works end to end.
TASK: Add DashboardLog with EventIds 70060-70065 and register the range 70060-70089 (EventIdRanges field, PublicAPI line, AssemblyRanges entry, registry rows).
KEY RULES: Pack ids without gaps; codes and type names only, never messages or payloads. At most 15 files.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 5: Integration tests and documentation (PR-6 and PR-7)

<details>
<summary>Tasks</summary>

1. PR-6: an integration test on one provider (the shared `Collection` fixture) that seeds outbox rows and reads them through the dashboard API; a load-test justification file for the aggregator.
2. PR-7 through `docs-writer`: README, `docs/features/dashboard.md`, INVENTORY, ROADMAP.

About 4 and 5 files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The dashboard and its logging are merged.
TASK: Add the integration test and the justification file; document the feature through docs-writer; run the verification and report per-flag coverage.
KEY RULES: Shared [Collection] fixtures only. No hand-typed coverage figures. English only.
REFERENCE FILES: tests/Encina.IntegrationTests (outbox tests), .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Specification | Relevance | Where it applies in the feature |
|--------------------------|-----------|---------------------------------|
| ADR-054 (observability conventions) | Meter names and the tenant tag rule | Aggregator, tenant labels |
| ADR-062 (error category contract) | Error codes shown by category | Errors panel; the dashboard adds no error code |
| OWASP guidance on admin interfaces | Authentication and CSRF on mutating endpoints | Guard, requeue action |
| SignalR hub protocol | Push transport | `DashboardHub` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IEncinaPipelineInspector` | `src/Encina/Pipeline/Ordering/IEncinaPipelineInspector.cs` | Pipeline panel |
| `IOutboxStore`, `IInboxStore`, `ISagaStore` | `src/Encina.Messaging/` | Data sources |
| `IEncinaMetrics`, meters | `src/Encina/Diagnostics/IEncinaMetrics.cs` | Aggregation by `MeterListener` |
| `Encina.SignalR`, `Encina.AspNetCore` | `src/Encina.SignalR`, `src/Encina.AspNetCore` | Hub and endpoints |
| `ForLogging()` | `src/Encina/Diagnostics/RedactedException.cs` | Safe stack traces |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Dashboard` (`EventIdRanges.Dashboard`, new) | 70060-70089 | Ids 70060-70065 used; same 50-id slot 70050-70099 as #441 (70050-70059), after the #4007 extent 70000-70049; needs the field, a PublicAPI line, an `AssemblyRanges` entry and registry rows (Phase 4) |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 22 | options, guard, extensions, aggregator, 4 adapters, endpoints, hub, 3 static assets, log, csproj, 2 PublicAPI |
| Tests | 16 | unit, guard, DI, endpoint tests, integration, log |
| Manifest, weights, slnx, fragment, registry | 5 | |
| Documentation | 5 | README, feature page, INVENTORY, ROADMAP, justification |
| **Total** | **~48 over 7 pull requests** | PR-1 14, PR-2 14, PR-3 13, PR-4 9, PR-5 8, PR-6 4, PR-7 5 files; about 700 + 1,100 + 900 + 700 + 350 + 200 + 300 = 4,250 lines in total (each under 1,500) |

Coverage targets per file: guard, options and extensions unit 90, guard 80; `MetricsAggregator.cs` unit 85 (guard 20); data sources unit 90 (guard 60); endpoints unit 85; `DashboardHub.cs` unit 70 (SignalR transport is reached by integration tests); `DashboardLog.cs` unit 80; integration flag 60 on data sources (one provider). Justification: logic is thin adapters over stores, fully reachable with substituted stores; the hub and static assets are not unit-reachable beyond connection tests. Load: justification file (read-only dev tool); benchmark: not applicable.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 library (AGENTS.md rules). Issue #445 adds Encina.Dashboard: a local, read-only-by-default, fail-closed web UI for outbox, inbox, saga, pipeline, metrics and errors.

IMPLEMENTATION OVERVIEW: Phase 1 package, options, guard. Phase 2 metrics aggregator and data sources. Phase 3 endpoints, SignalR hub, static UI. Phase 4 logging and EventIds 70060-70089. Phase 5 integration test and documentation. Seven pull requests, each at most 15 files and 1500 lines.

KEY PATTERNS: Fail closed; no payloads or messages to the browser; hashed tenant labels; IServiceScopeFactory for scoped stores; MeterListener aggregation; timer callbacks never throw; a Left from a store is an error state.

REFERENCE FILES: src/Encina.AspNetCore, src/Encina.SignalR, src/Encina.Messaging/Outbox/IOutboxStore.cs, src/Encina/Pipeline/Ordering/IEncinaPipelineInspector.cs, docs/plans/dsr-implementation-plan-404.md (plan style).
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The dashboard reads meters only; the key browser needs a cache read API that does not exist (follow-up) |
| 2 | OpenTelemetry | ✅ | Consumes the `Encina*` meters through `MeterListener`; the dashboard itself adds no instrument; tenant tag rule ADR-054 D4 for what it displays |
| 3 | Structured Logging | ✅ | `DashboardLog`, EventIds 70060-70065 in the new range |
| 4 | Health Checks | ❌ | Dev tool; it does not add a check |
| 5 | Validation | ❌ | Read-only; the requeue action takes no free input |
| 6 | Resilience | ❌ | No external calls; store failures are shown as errors |
| 7 | Distributed Locks | ❌ | Read-only queries |
| 8 | Transactions | ❌ | The requeue action calls an existing store method that owns its commit |
| 9 | Idempotency | ❌ | Reads and an idempotent requeue |
| 10 | Multi-Tenancy | ✅ | Hashed tenant labels and a tenant filter (the issue defers it; it is cheap here and avoids showing raw ids) |
| 11 | Module Isolation | ⏭️ | Filtering by module needs the module registry of #359 (open) |
| 12 | Audit Trail | ✅ | The opt-in requeue action writes an operation audit record when `IOperationAuditStore` is registered |

---

## Next Steps

1. The maintainer answers Dilemmas 1 and 3 when work starts.
2. Follow-up issue: read methods on the stores and the cache abstraction (provider-wide); it is not planned here and is listed in the report.
3. Open the sub-issues for PR-1 to PR-7 and brief Phase 1.
