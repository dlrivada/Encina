# Implementation Plan: `Encina.Dashboard` — local web UI for debugging Encina applications

> **Issue**: [#445](https://github.com/dlrivada/Encina/issues/445)
> **Type**: Feature (new package, developer experience)
> **Complexity**: High (5 phases, a web surface over operational data, 7 pull requests of 15, 9, 15, 13, 13, 4 and 4 files)
> **Estimated Scope**: ~4,900 lines over 7 pull requests (production code, tests and documentation, summed per pull request in "Estimated File Count")
> **Family**: dev-experience. This plan owns the package `Encina.Dashboard` and the EventIds 70060-70089 (registered in its PR-1, its own registry row; #441 owns 70050-70059 with its own row). The error panel may reuse `Encina.Diagnostics` (#441); a dead-letter admin API is #2229 (AspNetCore).

---

## Summary

A local, read-only-by-default web dashboard mapped with `app.MapEncinaDashboard("/encina", options)`: overview, pipeline, outbox, inbox, sagas and error-count panels, refreshed in real time, disabled outside development unless explicitly enabled, authenticated, reachable from loopback only by default, with sensitive data redacted. The issue's handlers list and cache key browser have no data source today and are follow-ups (Design Choice 3).

What exists today (read on `origin/main` after `git fetch`):

- No dashboard package. Related: `Encina.SignalR` (server-side hub support, `src/Encina.SignalR`), `Encina.AspNetCore` (endpoint extensions, health), and the pipeline inspector `IEncinaPipelineInspector.DescribeAsync` (`src/Encina/Pipeline/Ordering/IEncinaPipelineInspector.cs`) which already describes the ordered steps of a request type.
- Metrics: `IEncinaMetrics` has only `TrackSuccess` and `TrackFailure` (`src/Encina/Diagnostics/IEncinaMetrics.cs`); it exposes no read API. Aggregation must come from a `MeterListener` on the `Encina*` meters (ADR-054 D3: PascalCase `Encina.*` meter names, one per package).
- The stores have no list or browse API: `IOutboxStore` offers `GetPendingMessagesAsync`, `GetPendingCountAsync`, `GetExhaustedCountAsync`, `RequeueExhaustedAsync`; `IInboxStore` only `GetMessageAsync(id)`; `ISagaStore` `GetAsync(id)`, `GetStuckSagasAsync`, `GetExpiredSagasAsync` (`src/Encina.Messaging/{Outbox,Inbox,Sagas}`). A key browser for the cache has no abstraction either. A full viewer needs new read methods on 10 database providers (AGENTS.md section 5), which is a separate feature; this plan shows only what the interfaces expose today (Design Choice 3).
- ADR-064 will change the result carrier of every store method; the dashboard reads stores through one adapter layer (`IDashboardDataSource`) so the migration touches one place.

What this plan delivers: the package, the metrics aggregator, the data-source adapters for what exists, the panels, SignalR push, authentication and the production guard, logging, and docs.

- **Affected packages**: `Encina.Dashboard` (new, references `Encina`, `Encina.Messaging`, `Encina.AspNetCore`, `Encina.SignalR`), `Encina.Security.Audit` (`IOperationAuditStore`, reached through `Encina.AspNetCore`, which references it, `Encina.AspNetCore.csproj:26`), test projects. Core gains one `EventIdRanges` field.
- **Provider category**: none for the dashboard itself; it reads existing store interfaces and so works on every provider; the missing read methods are a follow-up feature.
- **Prerequisites**: none blocking.

### Shared Pieces and Ownership (family dev-experience)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Dashboard`, `IDashboardDataSource`, `DashboardOptions` | **This plan (#445)** | none |
| EventIds 70060-70089 (`Dashboard`) and its own registry Blocks row with the identical bounds, status `registered` | **This plan** (70050-70059 belongs to #441, which has its own row; the two plans never share a row) | none |
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
| **A) Show what the interfaces and meters expose today: outbox pending and exhausted counts, a page of pending messages (type, id, retry count, never the payload), requeue of exhausted messages by explicit id as an opt-in action (Design Choice 8); saga lookup by id plus stuck and expired lists; inbox lookup by id; the pipeline from `IEncinaPipelineInspector`; an errors panel that shows failure counts by reason taken from the aggregator (`IEncinaMetrics.TrackFailure` counts failures by reason, there is no error store and no per-error record); the handlers list, the cache panel, the payload viewer and the key browser are follow-ups** | Ships without touching 10 providers; honest scope: every panel has a data source that exists today | The issue's handlers list, payload viewer, full lists and cache key browser are not in the first release; the errors panel shows counts by reason, not individual errors |
| **B) Add list and browse methods to the stores and the cache abstraction now** | Meets every panel in the issue | Changes `IOutboxStore`, `IInboxStore`, `ISagaStore`, `ICacheProvider` and all implementers and fakes on 10 database providers and 8 caches: a multi-package feature of its own, not one pull request |

### Chosen Option: **A — Current interfaces only; read methods as a follow-up** (recommended, pending the maintainer)

### Rationale

- A wins for a first release inside the 15-file limit; B wins if the maintainer wants the full viewer before release and accepts a separate provider-wide plan first. There is no `/api/handlers` endpoint and the errors endpoint serves counts only: there is no handler registry that is public (the scan results of `EncinaAssemblyScanner` are internal) and no error store, so listing handlers and individual errors would need a new core abstraction, a follow-up. The follow-up (read methods on the stores, a handler list, an in-memory error ring fed by a pipeline behavior) is recorded inside this plan, in "Next Steps", under "Deferred: read-side follow-ups" (a `[FEATURE]` follow-up belongs inside the existing plan, so no separate issue file is written).
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Sensitive data and tenancy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Payloads are never sent to the browser; messages show type, id, status and counts; the errors panel shows failure counts by reason (there is no error store, Design Choice 3), never `EncinaError.Message`; metric tenant tags are read only when multi-tenancy is on (ADR-054 D4); whether and how they are displayed is Design Choice 7, not this choice** | Follows AGENTS.md section 3 and ADR-054 D4; no PII in the browser | The "payload viewer" is dropped (also in Design Choice 3) |
| **B) Show payloads with field-level redaction rules** | More useful for debugging | Needs a redaction rule set to maintain; a missed field leaks personal data |

### Chosen Option: **A — No payloads, no messages**

### Rationale

- The outbox, inbox and saga store interfaces carry no tenant parameter and the outbox message abstraction has no `TenantId` (`src/Encina.Messaging/Outbox`, checked with `git grep -n TenantId`): the panels show what the resolved store returns for the host, as a single-process development view, and the dashboard adds no tenant filter on them; only the metric tenant tags need a rule, and their display is Design Choice 7.
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (`EncinaError.Message` never reaches logs or plaintext; `ForLogging()`), ADR-054 D4 (tenant as attribute and tag when multi-tenancy is on) and the issue's "Sensitive data redaction in payloads".

</details>

<details>
<summary><strong>5. Request surface: every route class passes the same fail-closed guard</strong></summary>

The dashboard serves four kinds of route, and each one leaks or changes data if it skips the gate. Guarding "every endpoint" is not enough unless the hub, the static files and the mutating call are named. This choice settles only that every route class passes the guard; the address, Origin and antiforgery rows depend on Design Choice 6 and the requeue action's shape is Design Choice 8.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every route class passes the guard and the hardening below; the table is the contract and each row has a test** | A single fail-closed rule with no gap a browser can reach | Static files are served by the dashboard itself (no `UseStaticFiles`), a few more files |
| **B) Guard the JSON APIs only; leave the hub negotiation and the static files to the host's pipeline** | Fewer files | An unauthenticated hub leaks the same snapshots as the API; the static assets disclose the API shape; violates AGENTS.md section 3 |

| Route class | Routes | Must pass | Hardening |
|-------------|--------|-----------|-----------|
| JSON read API | `GET /api/overview`, `/api/pipeline`, `/api/outbox`, `/api/inbox`, `/api/sagas`, `/api/errors` | The guard: environment gate (Design Choice 2), authenticated principal (`RequireAuthentication`), and the address and `Host` rules per Design Choice 6; a missing `HttpContext`, principal or remote address denies | `Cache-Control: no-store`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer` |
| SignalR hub | `DashboardHub` negotiate, the WebSocket upgrade and every connection (`OnConnectedAsync` re-checks the guard and aborts the connection when it fails) | The same guard as the API, run as an ASP.NET Core authorization requirement: the route group calls `RequireAuthorization(DashboardPolicy)` and `MapHub` is mapped on that group, so the requirement runs on the negotiate and upgrade requests. It is not an endpoint filter: `IEndpointFilter` runs only inside minimal-API handlers, and a `MapHub` endpoint never executes it. A host without `UseAuthorization` makes ASP.NET Core throw for an endpoint carrying authorization metadata, so the dashboard fails closed; a PR-5 test proves both. The hub exposes no client-invokable method (server push only), so a client call has nothing to reach | `Origin` and `Host` validation per Design Choice 6 on negotiate and upgrade; the hub carries the same snapshots as the API and nothing more |
| Static assets | `index.html`, `app.js`, `app.css`, served from embedded resources through the guarded route group | The same guard (they disclose the API shape and the dashboard's presence) | `Content-Security-Policy: default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; img-src 'self'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'` (no inline script or style, so no `unsafe-inline`), plus the headers above; `app.js` renders every server-provided string (type names, ids, reason codes) with `textContent` or `createTextNode`, never `innerHTML`, `outerHTML`, `insertAdjacentHTML` or `document.write`, and a unit test scans the embedded `app.js` for those tokens and fails when one appears |
| Mutating action | `POST /api/outbox/requeue` | The guard, plus `EnableActions`, plus the preconditions and body rules of Design Choice 8, plus the browser-origin checks per Design Choice 6 | No query-string state change; the body shape per Design Choice 8 |

### Chosen Option: **A — Every route class passes the guard**

### Rationale

- Option B leaves a route class reachable without the gate, which AGENTS.md section 3 rules out. What the guard additionally checks (address, Host, Origin, antiforgery) is Design Choice 6, so those table cells read "per Design Choice 6" and change with its answer; the rule that no route class skips the guard does not.
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 ("Compliance and security gates fail closed: when context is missing ... the gate denies; the only opt-out is explicit and logged") and the issue's "Security Considerations" (authentication on every dashboard endpoint, disabled by default in production).

</details>

<details>
<summary><strong>6. Browser-origin protection and remote access</strong></summary>

Authentication cookies are sent by the browser to the dashboard from any page the developer visits, so a hostile page can post to a mutating endpoint or open the hub (cross-site request forgery, cross-site WebSocket hijacking), and a hostile DNS name can rebind to loopback. Kestrel's binding address belongs to the host, so the guard enforces the address too.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Loopback-only by default: the guard denies when `RemoteIpAddress` is missing or not loopback (read after the host's own forwarded-headers middleware) and when `Host` is not `localhost`, `127.0.0.1` or `[::1]`; `AllowRemote` is an explicit option, logged on use (EventId 70062); on the hub negotiate and upgrade and on every mutating call the `Origin` header must equal the request's own scheme, host and port (a missing `Origin` denies); mutating calls also need an ASP.NET Core antiforgery token sent in a header (`IAntiforgery`)** | Defense in depth against CSRF, hub hijacking and DNS rebinding; remote use is possible but explicit | A developer behind a dev tunnel or container port mapping sets `AllowRemote` |
| **B) As A with no `AllowRemote` at all** | Cannot be exposed by configuration | A container whose port is mapped from the host looks remote to the dashboard, so such developers cannot use it |
| **C) No address check: development-only gating plus authentication, as the issue sketches** | Simplest | On a development host bound to all interfaces on a shared network the dashboard is reachable by any peer who authenticates or, with `RequireAuthentication = false`, by anyone; no defense against CSRF or hub hijacking |

### Chosen Option: **A — Loopback by default, Origin and Host validation, antiforgery on mutations, explicit logged `AllowRemote`** (recommended, pending the maintainer)

### Rationale

- A wins when the tool must be safe on a shared or untrusted network by default and still usable behind a port mapping; B wins if the maintainer wants no way to expose it remotely at all; C wins if the maintainer judges development-only gating enough and accepts the browser-based attack classes. The tasks and tests of PR-2 (guard), PR-4 (mutation) and PR-5 (hub) implement A; the other options change those tasks only.
- **Class:** Dilemma

</details>

<details>
<summary><strong>7. How tenants appear in the metrics panel</strong></summary>

The aggregator reads metric tags that may carry a tenant id when multi-tenancy is on (ADR-054 D4 allows it as an attribute). An unkeyed hash of a low-entropy tenant id is reversible by a dictionary of likely ids, so "hashed" alone does not hide it.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A keyed hash: HMAC-SHA256 of the tenant tag with a random 32-byte per-process key (`RandomNumberGenerator`), the first 8 bytes in hex as the label; the tenant filter hashes the typed value with the same key; the key is never logged or sent** | Labels are stable within one run, cannot be reversed by a dictionary without the key, and the filter still works | Labels change between runs; a developer cannot read the tenant id |
| **B) No tenant breakdown: the metrics panel aggregates across tenants** | Nothing to leak; simplest | Loses per-tenant debugging, which the issue defers anyway |
| **C) Raw tenant ids, trusting development-only gating, authentication and loopback** | Most useful to a developer | Puts tenant identifiers in the browser and in screenshots |

### Chosen Option: **A — Keyed hash labels** (recommended, pending the maintainer)

### Rationale

- A wins when per-tenant debugging is wanted without exposing ids; B wins if the maintainer judges even opaque labels unnecessary for a first release; C wins if the tool is treated as strictly local with no screenshots. ADR-054 D4 covers the tag only (a tenant attribute when multi-tenancy is on, no baggage); the hashing is this plan's own choice, so it is a Dilemma, not Settled. The aggregator also bounds its series: at most `MaxSeries` distinct metric-and-tag combinations (default 1,000), and a new series beyond the cap is dropped and counted in an overflow field, so an attacker-controlled tag value cannot grow memory.
- **Class:** Dilemma

</details>

<details>
<summary><strong>8. The outbox requeue action: audit precondition, explicit ids and batch cap</strong></summary>

**What is not a choice.** The action calls the existing `OutboxOrchestrator.RequeueExhaustedAsync(IEnumerable<Guid>, CancellationToken)` (`src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:212`), resolved from one `IServiceScopeFactory` scope; it does not re-implement the requeue on `IOutboxStore`. That method already reads `OutboxOptions.MaxRetries` (never the request), filters `Guid.Empty` and duplicates, returns 0 for an empty list, calls `IOutboxStore.RequeueExhaustedAsync` and then `IOutboxStore.SaveChangesAsync` itself, logs EventId 2959 and records the requeued-messages metric (`:227-252`). The commit is therefore the orchestrator's, as for any caller; there is no repository rule or ADR that orchestrators never save, and `OutboxOrchestrator.ProcessPendingMessagesAsync` is the one method that leaves the save to its caller (`:141`). Per provider: EF Core (SqlServer, PostgreSQL, MySQL) needs `SaveChangesAsync` to persist, ADO.NET (3), Dapper (3) and MongoDB apply immediately and their `SaveChangesAsync` returns `Right` without work (`src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:382`, `src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs:305`, `src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs:264`), so one call is correct for all 10 providers. `OutboxOrchestrator` is registered scoped only when `UseOutbox` is on (`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:283`), so the route is not mapped when the host has no orchestrator (checked with `IServiceProviderIsService` at map time) and the dashboard logs the reason once (EventId in PR-4). A `Left` is returned as an error state and logged by code, never as success. The store interfaces carry no tenant parameter (Design Choice 4); ids that are not exhausted or unknown are ignored by the store.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Audit store required: with `EnableActions = true` and no registered `IOperationAuditStore` the route is not mapped (the dashboard stays read-only); each action first calls `IOperationAuditStore.RecordAsync` (`src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs:50`) with the principal and the id count, never payloads, and stops when the record fails (audit before act). The body carries a non-empty id list of at most `MaxRequeueBatch` (default 100); an empty, missing or oversized list is rejected before any call, and the action never requeues all** | Fail closed and auditable; the smallest mutation surface | A developer without an audit store gets a read-only dashboard; no "requeue everything" shortcut |
| **B) As A, but without an audit store the route is mapped when the host sets an explicit, logged opt-out (`AllowUnauditedActions`); the record is skipped and the skip logged** | Usable in a bare development host; still an explicit logged opt-out (AGENTS.md section 3) | A mutation without an audit record, by choice |
| **C) As A, plus an optional "requeue all exhausted" body (confirmed in the request)** | Convenient after an outage | One call changes every exhausted message; the cap no longer bounds the effect |

### Chosen Option: **A — Audit store required, explicit ids only, cap 100** (recommended, pending the maintainer)

### Rationale

- A wins when a state-changing call on an operational store must always leave an audit record and be bounded; B wins for a developer host with no audit store who accepts an unaudited requeue as an explicit logged choice; C wins if the maintainer judges an outage clean-up more important than the bound. The default cap of 100 is part of this choice, not a convention. Design Choice 6 is independent: it decides who may reach the route, this choice what the route does.
- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Package skeleton, logging and the EventId range (PR-1)

The guard of PR-2 needs its logged denial and logged opt-out (Design Choice 2, AGENTS.md section 3) and a `[LoggerMessage]` EventId may not exist before its range is registered (AGENTS.md section 7), so the log class and the range registration land first, before any gate ships.

<details>
<summary>Tasks</summary>

1. `src/Encina.Dashboard/Encina.Dashboard.csproj` (framework reference `Microsoft.AspNetCore.App`, project references above, embedded `wwwroot/**` added in PR-5, `InternalsVisibleTo` for `Encina.UnitTests` and `Encina.GuardTests`), `PublicAPI.Shipped.txt` (empty), `PublicAPI.Unshipped.txt` (empty until PR-2); `Encina.slnx`; `.github/scripts/coverage-weights.json`: add `Encina.Dashboard` to the `Cloud` category (`Encina.AspNetCore` and `Encina.SignalR` sit there; applicable tests unit and guard, target 60). New `.github/coverage-manifest/Encina.Dashboard.json` (through `mechanical-fixer`).
2. `src/Encina.Dashboard/DashboardLog.cs` (`[LoggerMessage]`, `internal static partial class`), ids packed from 70060: 70060 dashboard mapped, 70061 request denied (reason code), 70062 explicit opt-out used (production or remote, Warning). Later pull requests append 70063 onward, never with gaps.
3. Register the range: `EventIdRanges.Dashboard = (70060, 70089)` in `src/Encina/Diagnostics/EventIdRanges.cs`, its line in `src/Encina/PublicAPI.Unshipped.txt`, the `AssemblyRanges` entry for `Encina.Dashboard` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`, and in `docs/engineering/eventid-block-registry.md` (through `docs-writer`) one "Registered ranges" row and one Blocks row with bounds identical to the registered range: `70060-70089`, area Testing and tooling, owner #445, status `registered`. `EventIdRegistryAgreementTests` requires a `registered` Blocks row to match a range of `EventIdRanges.cs` with the same bounds; a row shared with #441 such as `70050-70099` would fail it (and name #441's unregistered range if #445 landed first), so each plan has its own row. Run the architecture tests.
4. `tests/Encina.UnitTests/Dashboard/DashboardLogTests.cs` (fake logger asserts the EventIds and that only codes are logged); add the `ProjectReference` and nothing else to `Encina.UnitTests.csproj` and `Encina.GuardTests.csproj`; changelog fragment `changelog.d/445-developer-dashboard.added.md` (through `mechanical-fixer`).

File count 15 (csproj, 2 PublicAPI, log class, `EventIdRanges.cs`, core `PublicAPI.Unshipped.txt`, `AssemblyRanges` test edit, registry page, slnx, weights, manifest, log test, 2 test csproj edits, fragment); about 450 lines. The two test csproj edits and the manifest are in this total.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina has no dashboard. Encina.AspNetCore and Encina.SignalR show endpoint and hub conventions. EventIds may not be used before the range is registered (AGENTS.md section 7).
TASK: Create the Encina.Dashboard package skeleton, DashboardLog with EventIds 70060-70062, and register EventIdRanges.Dashboard = (70060, 70089) with its PublicAPI line, AssemblyRanges entry and its own registry rows (registered range row and a Blocks row 70060-70089, status registered, never a row shared with #441).
KEY RULES: Pack EventIds without gaps; codes and type names only, never messages or payloads. Add the package to the Cloud category of coverage-weights.json. Run the EventId architecture tests. At most 15 files.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.SignalR/Encina.SignalR.csproj.
```

</details>

### Phase 2: Options, guard and registration (PR-2)

<details>
<summary>Tasks</summary>

1. `DashboardOptions.cs` (`Title`, `RefreshInterval` (`TimeSpan`), `DarkMode`, `RequireAuthentication` true, `AllowInProduction` false, `AllowRemote` false, `EnableActions` false, `MaxRequeueBatch` 100, `MaxSeries` 1,000).
2. `DashboardGuard.cs`: one decision shared by the API, the hub and the static assets (Design Choice 5): environment gate (`IHostEnvironment.IsDevelopment()` or `AllowInProduction`), authenticated principal, loopback `RemoteIpAddress` and `Host` (Design Choice 6) unless `AllowRemote`; a missing `HttpContext`, principal, environment or remote address denies; every denial and every opt-out use logs 70061 and 70062 by reason code, never request data.
3. `EndpointRouteBuilderExtensions.MapEncinaDashboard` (a route group that calls `RequireAuthorization(DashboardPolicy)`, so every later route of PR-4 and PR-5, including the SignalR hub, inherits the guard; an endpoint filter would not reach a `MapHub` endpoint, Design Choice 5) and `ServiceCollectionExtensions.AddEncinaDashboard` (registers the options, the authorization policy `DashboardPolicy` with its requirement and handler, which call `DashboardGuard` and live in `DashboardGuard.cs`, and every dependency it resolves; the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`).
4. Tests in `tests/Encina.UnitTests/Dashboard/`: guard behavior (production denied, anonymous denied, non-loopback denied, rebinding `Host` denied, development allowed, missing `HttpContext` denied, each opt-out logged), the DI test, and `tests/Encina.GuardTests/Dashboard/DashboardGuardTests.cs` for the public methods; `PublicAPI.Unshipped.txt` and manifest edits (through `mechanical-fixer`).

File count 9 (options, guard, 2 extension classes, 3 test files, PublicAPI, manifest); about 700 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: PR-1 merged the skeleton, DashboardLog and the range. Encina.AspNetCore and Encina.SignalR show endpoint and hub conventions.
TASK: Add DashboardOptions, DashboardGuard (environment, authenticated principal, loopback RemoteIpAddress and Host, all fail closed, exposed as an authorization requirement and handler), MapEncinaDashboard (route group with RequireAuthorization(DashboardPolicy), never an endpoint filter, so a hub is covered too) and AddEncinaDashboard (registers the policy).
KEY RULES: Missing context denies; the opt-outs (AllowInProduction, AllowRemote, RequireAuthentication = false) are explicit and logged. AddEncinaDashboard registers every option and dependency it resolves (DI test with ValidateOnBuild and ValidateScopes). TimeProvider for any time. At most 15 files.
REFERENCE FILES: src/Encina.AspNetCore/ServiceCollectionExtensions.cs, src/Encina.SignalR/Encina.SignalR.csproj, src/Encina.AspNetCore/ApplicationBuilderExtensions.cs.
```

</details>

### Phase 3: Metrics aggregation and data sources (PR-3)

<details>
<summary>Tasks</summary>

1. `MetricsAggregator.cs` (hosted service with a `MeterListener` on `Encina*` instruments, in-memory ring buffers, a timer callback that never throws, per AGENTS.md section 3), bounded to `MaxSeries` distinct metric-and-tag combinations with an overflow count (Design Choice 7); `IDashboardDataSource.cs` and adapters `OutboxDataSource`, `InboxDataSource`, `SagaDataSource`, `PipelineDataSource` (uses `IEncinaPipelineInspector`), each resolving stores through `IServiceScopeFactory`, never from a singleton directly; a `Left` from a store is shown as an error state, never as an empty success. The errors panel is fed by the failure counts by reason the aggregator reads from `TrackFailure` meters.
2. `DashboardLog.cs` gains 70063 data source failed (error code) and 70064 aggregator cycle failed (error code, exception through `ForLogging()`).
3. Registration: `ServiceCollectionExtensions.AddEncinaDashboard` (PR-2) gains the aggregator hosted service, `IDashboardDataSource` and the four adapters, so registration completeness holds in the same pull request that adds the classes; the PR-2 DI test (`ValidateOnBuild`, `ValidateScopes`) is edited to resolve them.
4. Tests per adapter with substituted stores; aggregator tests with a `FakeTimeProvider` (including the series cap); the aggregator and data-source tests assert EventIds 70063 and 70064 with a fake logger (so `DashboardLogTests` is not edited here); guard tests for the public registration surface; `PublicAPI.Unshipped.txt` and manifest edits.

File count 15: aggregator, data-source interface, 4 adapters, the log edit, the `ServiceCollectionExtensions.cs` edit (8); `MetricsAggregatorTests`, `StoreDataSourceTests` (outbox, inbox, saga), `PipelineDataSourceTests`, the guard test and the DI test edit (13); `PublicAPI.Unshipped.txt` and the manifest (15); about 1,250 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: PR-1 and PR-2 merged.
TASK: Add MetricsAggregator (bounded by MaxSeries, keyed-hash tenant labels per Design Choice 7) and the data-source adapters for outbox, inbox, saga and pipeline, reading only existing store methods; add EventIds 70063 and 70064 to DashboardLog.
KEY RULES: Never inject a scoped service into a singleton (IServiceScopeFactory). A Left from a store is surfaced as an error, not hidden. Timer callbacks catch and log by error code; ForLogging() for exceptions. No payloads. At most 15 files.
REFERENCE FILES: src/Encina.Messaging/Outbox/IOutboxStore.cs, src/Encina.Messaging/Sagas/ISagaStore.cs, src/Encina.Messaging/Inbox/IInboxStore.cs, src/Encina/Pipeline/Ordering/IEncinaPipelineInspector.cs, src/Encina/Diagnostics/IEncinaMetrics.cs.
```

</details>

### Phase 4: API, requeue action, hub and UI (PR-4 and PR-5)

<details>
<summary>Tasks</summary>

1. PR-4 (API and action): a `DashboardEndpoints.cs` with the read endpoints `GET /api/overview`, `/api/pipeline`, `/api/outbox`, `/api/inbox`, `/api/sagas`, `/api/errors` (failure counts by reason), mapped on the guarded route group of PR-2 (so each passes the guard, Design Choice 5); `DashboardSecurityHeaders.cs` (the response headers of the surface table, applied to the group); `OutboxRequeueEndpoint.cs`: `POST /api/outbox/requeue`, shaped by Design Choice 8 (recommended option A): mapped only when `EnableActions` is true, an `OutboxOrchestrator` is registered and an `IOperationAuditStore` is registered (otherwise the dashboard stays read-only and logs the reason), body = a non-empty id list up to `MaxRequeueBatch` (never requeue-all), audit record first, then `OutboxOrchestrator.RequeueExhaustedAsync(ids, ct)` resolved from one scope, which reads `MaxRetries`, commits through `SaveChangesAsync`, logs 2959 and records the metric itself (the dashboard does not call the store); success only when it returns `Right`. The antiforgery token (when Design Choice 6 keeps antiforgery) is issued by a guarded `GET /api/antiforgery` and required in a header, `AddEncinaDashboard` registers `AddAntiforgery` (so `ServiceCollectionExtensions.cs` and its DI test are edited here), and the `Origin` and `Host` checks of Design Choice 6 run on the action. `DashboardLog` gains 70065 action performed (count only) and 70066 action refused (reason code: actions disabled, audit store missing, orchestrator missing, origin mismatch, invalid ids).
2. PR-4 tests (in-memory host through `WebApplicationFactory`-style tests): every read route denied without the guard conditions; headers present; requeue with empty and oversized id lists rejected before any call; the commit is the orchestrator's (a substituted `IOutboxStore` under a real `OutboxOrchestrator` proves `SaveChangesAsync` runs after `RequeueExhaustedAsync` and that a `Left` from either is reported as failure); audit failure stops the action; read-only when no audit store or no orchestrator; origin and antiforgery rejections.
3. PR-5 (hub and UI): `DashboardHub.cs` (server push of aggregated snapshots every `RefreshInterval`; no client-invokable methods; the guard on negotiate and upgrade comes from the route group's `RequireAuthorization(DashboardPolicy)` that `MapHub` inherits, the `Origin` and `Host` validation per Design Choice 6 runs in the same requirement handler, and `OnConnectedAsync` re-checks the guard and aborts a failing connection; `AddEncinaDashboard` registers `AddSignalR`), the embedded `wwwroot/index.html`, `app.js`, `app.css` (dark mode) served through the guarded route group, never through `UseStaticFiles`, with the CSP of the surface table; `app.js` renders server strings only with `textContent` or `createTextNode`. `DashboardLog` gains 70067 hub connection denied (reason code).
4. PR-5 tests: hub negotiate and WebSocket upgrade denied without the guard conditions (the proof that the group authorization, not an endpoint filter, guards the hub) and with a wrong `Origin`; a host without `UseAuthorization` fails closed; a connection that fails the guard is aborted; static assets denied when the guard fails and served with the CSP; a test scans the embedded `app.js` for `innerHTML`, `outerHTML`, `insertAdjacentHTML`, `document.write` and `eval` and fails on any; guard tests; manifest, PublicAPI and csproj (embedded resources) edits.

PR-4: 13 files (3 production files, the log edit, 3 test files, the log test edit, PublicAPI, manifest, the registration edit in `EndpointRouteBuilderExtensions.cs`, the `ServiceCollectionExtensions.cs` edit for antiforgery and the DI test edit), about 1,050 lines. PR-5: 13 files (hub, 3 static assets, the `EndpointRouteBuilderExtensions.cs` edit, the `ServiceCollectionExtensions.cs` edit for `AddSignalR`, the log edit, 3 test files, the log test edit, the DI test edit, manifest; the csproj embedding glob was declared in PR-1 and the hub is `internal`, so no PublicAPI line), about 950 lines.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The guard, the data sources and the log class exist (PR-1 to PR-3).
TASK: PR-4: read endpoints, security headers and the opt-in requeue action. PR-5: the SignalR hub and the embedded static UI.
KEY RULES: Every route class (JSON API, hub negotiate and upgrade, static assets, the mutating action) passes the same fail-closed guard (Design Choice 5), which for the hub is the group's RequireAuthorization policy, never an endpoint filter. The requeue action calls OutboxOrchestrator.RequeueExhaustedAsync(ids, ct) (it reads MaxRetries, commits, logs and counts itself; never re-implement it on IOutboxStore), takes an explicit non-empty id list, calls the audit store first and fails closed without one (Design Choice 8, recommended option A), and reports success only when it returns Right. Origin and Host validation plus an antiforgery token on mutations (Design Choice 6). CSP without inline script, textContent only in app.js. Responses never carry payloads or EncinaError.Message. At most 15 files per pull request.
REFERENCE FILES: src/Encina.AspNetCore/Health, src/Encina.SignalR, src/Encina.Messaging/Outbox/IOutboxStore.cs, src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs.
```

</details>

### Phase 5: Integration tests and documentation (PR-6 and PR-7)

PR-6 is the integration test (a test project edit if the fixture needs a project reference, the test file, the justification file and the manifest edit); PR-7 is the documentation.

<details>
<summary>Tasks</summary>

1. PR-6: an integration test on one provider (the shared `Collection` fixture) that seeds outbox rows, reads them through the dashboard API and runs the requeue action, through the real `OutboxOrchestrator`, against a real EF Core store (proving that the orchestrator's `SaveChangesAsync` persists the requeue); a load-test justification file for the aggregator. **How the test is measured.** `Encina.Dashboard` sits in the `Cloud` category of `.github/scripts/coverage-weights.json`, whose `applicableTests` are `unit` and `guard`; `coverage-report.cs` classifies a test by its project folder (`IntegrationTests`, line 1603) but skips a flag that is not applicable to the file's category (lines 1849 and 1870), so the integration flag does not exist for these files and this test counts toward no per-flag target. It is kept as a documented proof test, unmeasured by design, consistent with `Encina.AspNetCore` and `Encina.SignalR` (same category), because it is the only test that shows a real EF Core store persisting the requeue, the riskiest behavior (AGENTS.md section 9 asks for an integration test of a non-database feature unless justified). Moving the package to a category that applies integration (`Provider` and `Caching` do) would measure it, and is the maintainer's call, not a plan decision.
2. PR-7 through `docs-writer`: README, `docs/features/dashboard.md`, INVENTORY, ROADMAP.

PR-6 about 4 files (the justification file for load tests is counted here only, not again in documentation), PR-7 4 files (README, feature page with a security section on loopback, origin checks and CSP, INVENTORY, ROADMAP).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The dashboard, its guard and its logging are merged.
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
| ADR-054 (observability conventions) | Meter names and the tenant tag rule (D4) | Aggregator, tenant labels (Design Choice 7) |
| ADR-062 (error category contract) | Error codes shown by category | Errors panel shows failure counts by reason; the dashboard adds no error code and so no `ErrorCategory` |
| OWASP guidance on admin interfaces, CSRF, XSS and Content-Security-Policy | Authentication, Origin and antiforgery checks on mutating and hub requests, output encoding, CSP | Guard, requeue action, hub, static assets (Design Choices 5 and 6) |
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
| `Encina.Dashboard` (`EventIdRanges.Dashboard`, new) | 70060-70089 | Ids 70060-70067 used, packed without gaps across PR-1 (70060-70062), PR-3 (70063-70064), PR-4 (70065-70066) and PR-5 (70067); after the #4007 extent 70000-70049, beside #441's 70050-70059. Registered in PR-1: the field, a PublicAPI line, an `AssemblyRanges` entry, a registered-ranges row and a Blocks row `70060-70089`, status `registered`, with bounds identical to the range (`EventIdRegistryAgreementTests`); #441 has its own row, no row is shared |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 26 | csproj, 2 PublicAPI, options, guard, 2 extension classes, aggregator, data-source interface, 4 adapters, endpoints, security headers, requeue endpoint, hub, 3 static assets, log; core edits: `EventIdRanges.cs`, core `PublicAPI.Unshipped.txt` |
| Tests | 18 | log, guard, DI, aggregator, store and pipeline data sources, endpoints, requeue, headers, hub, static assets, guard tests, integration, the `AssemblyRanges` edit, 2 test csproj edits |
| Manifest, weights, slnx, fragment, registry | 5 | `Encina.Dashboard.json`, `coverage-weights.json` (`Cloud` category), `Encina.slnx`, the fragment, the registry page |
| Documentation | 4 | README, feature page, INVENTORY, ROADMAP (the load-test justification file is counted in PR-6) |
| **Total** | **~53 distinct files, 72 file touches over 7 pull requests** | PR-1 15, PR-2 9, PR-3 15, PR-4 13, PR-5 13, PR-6 4, PR-7 4 files (the log class, `EndpointRouteBuilderExtensions.cs`, `ServiceCollectionExtensions.cs`, the DI test, the manifest and `PublicAPI.Unshipped.txt` are touched in several); about 450 + 700 + 1,250 + 1,050 + 950 + 200 + 300 = 4,900 lines in total (each pull request is under 1,500) |

Coverage targets per file (unit, guard; the `Cloud` category applies those two flags): `DashboardGuard.cs` unit 90, guard 80; options and extension classes unit 90, guard 80; `MetricsAggregator.cs` unit 85, guard 20; data sources unit 90, guard 60; `DashboardEndpoints.cs`, `OutboxRequeueEndpoint.cs` and `DashboardSecurityHeaders.cs` unit 85, guard 40 (the requeue endpoint delegates the commit to `OutboxOrchestrator`, so its unit tests cover the validation, audit and error mapping, not the commit); `DashboardHub.cs` unit 70 (the SignalR transport itself is reached only by the PR-6 proof test, which counts toward no flag because the integration flag does not apply to the `Cloud` category (`coverage-weights.json`; see Phase 5), so every file's integration target is 0 with that reason); `DashboardLog.cs` unit 80 (guard 0: an internal log class with no executable parameters worth guarding). Justification: logic is thin adapters over stores, fully reachable with substituted stores; the guard and the security headers are pure decisions reachable in an in-memory host; the hub and static assets are not unit-reachable beyond connection tests. Load: justification file (read-only dev tool); benchmark: not applicable.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 library (AGENTS.md rules). Issue #445 adds Encina.Dashboard: a local, read-only-by-default, fail-closed web UI for outbox, inbox, saga, pipeline, metrics and errors.

IMPLEMENTATION OVERVIEW: Phase 1 package skeleton, DashboardLog and the EventIds range 70060-70089 (before any gate ships). Phase 2 options, guard (environment, principal, loopback, Host) and registration. Phase 3 metrics aggregator and data sources. Phase 4 read endpoints, the requeue action, the SignalR hub and the static UI. Phase 5 integration test and documentation. Seven pull requests, each at most 15 files and 1500 lines.

KEY PATTERNS: Fail closed on every route class (API, hub, static assets, action); loopback by default; Origin, Host and antiforgery checks; CSP and textContent only; no payloads or messages to the browser; keyed-hash tenant labels (pending Dilemma 7); IServiceScopeFactory for scoped stores; MeterListener aggregation with a series cap; timer callbacks never throw; a Left from a store is an error state; the requeue action calls OutboxOrchestrator.RequeueExhaustedAsync with explicit ids after an audit record (Design Choice 8, pending); the hub is guarded by group authorization, not an endpoint filter.

REFERENCE FILES: src/Encina.AspNetCore, src/Encina.SignalR, src/Encina.Messaging/Outbox/IOutboxStore.cs, src/Encina/Pipeline/Ordering/IEncinaPipelineInspector.cs, docs/plans/dsr-implementation-plan-404.md (plan style).
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The dashboard reads meters only; the cache panel and key browser need a cache read API that does not exist (follow-up) |
| 2 | OpenTelemetry | ✅ | Consumes the `Encina*` meters through `MeterListener`; the dashboard itself adds no instrument; tenant tag rule ADR-054 D4 for what it displays |
| 3 | Structured Logging | ✅ | `DashboardLog`, EventIds 70060-70067 in the new range, registered in PR-1 |
| 4 | Health Checks | ❌ | Dev tool; it does not add a check |
| 5 | Validation | ✅ | The requeue action validates its body (non-empty id list, at most `MaxRequeueBatch`; Design Choice 8) before any call; the read routes take no input |
| 6 | Resilience | ❌ | No external calls; store failures are shown as errors |
| 7 | Distributed Locks | ❌ | Read-only queries |
| 8 | Transactions | ✅ | The requeue action owns no atomic unit: it calls `OutboxOrchestrator.RequeueExhaustedAsync`, which requeues and then calls `SaveChangesAsync` on the same store (EF Core persists only then; the other 7 providers apply immediately and their `SaveChangesAsync` is a no-op), and reports success only when it returns `Right` (Design Choice 8) |
| 9 | Idempotency | ❌ | Reads and an idempotent requeue (requeueing an id that is no longer exhausted is ignored by the store) |
| 10 | Multi-Tenancy | ✅ | Metric tenant tags shown as keyed-hash labels (Design Choice 7, pending); the store interfaces carry no tenant parameter, so the store panels show what the host's store returns (Design Choice 4) |
| 11 | Module Isolation | ⏭️ | Filtering by module needs the module registry of #359 (open) |
| 12 | Audit Trail | ✅ | The requeue action writes an `IOperationAuditStore` record before it acts and is not mapped at all when no audit store is registered (fail closed; Design Choice 8, recommended option A, pending) |

---

## Next Steps

1. The maintainer answers Dilemmas 1, 3, 6, 7 and 8 when work starts; Dilemma 6 (loopback, Origin checks, remote access) is answered before PR-2, Dilemma 8 (requeue action) before PR-4.
2. Open the sub-issues for PR-1 to PR-7 and brief Phase 1.
3. Deferred: read-side follow-ups (below). Per the maintainer rule of 2026-11-24 a `[FEATURE]` follow-up found inside a plan belongs inside that plan, so it is recorded here and no separate issue file or issue is opened now; when the maintainer schedules it, it becomes its own issue opened together with its own plan.

### Deferred: read-side follow-ups

Add the read-side abstractions the dashboard cannot show today, after its first release. Issue #445 asks for full lists, a payload viewer, a cache key browser and a handlers panel; those are provider-wide changes of their own, not one pull request.

**Why they are deferred (read on `origin/main`).** `IOutboxStore` has counts and a pending page, `IInboxStore` only `GetMessageAsync(id)`, `ISagaStore` only `GetAsync`, `GetStuckSagasAsync` and `GetExpiredSagasAsync`; there is no cache key browser, no public handler list (the results of `EncinaAssemblyScanner` are internal) and no error store (`IEncinaMetrics.TrackFailure` counts failures by reason).

**Scope.**

- Outbox, inbox and saga list methods with keyset paging and a bounded page size, returning type, id, status and counts only; never payloads (Settled by Design Choice 4 of this plan). Provider matrix: ADO.NET, Dapper and EF Core on SqlServer, PostgreSQL and MySQL, plus MongoDB (10), read-only (no `SaveChangesAsync`).
- A key browse method on the cache abstraction for the 8 caches; a provider that cannot enumerate keys (Memcached, planned) says so with an error code.
- A public handler list (request type, handler type, kind) taken from the registrations without exposing the internal scanner.
- An in-memory error ring fed by a pipeline behavior that records the error code, the category and the exception type only, never `EncinaError.Message`.

**Choices left to its own plan** (each classified there as Trivial, Settled with a citation, or Dilemma): page size, tenant scoping of list methods (the store interfaces carry no tenant parameter today; a Dilemma), behavior of caches that cannot enumerate keys, ring capacity and retention, and any instrument (names per ADR-054). Cross-cutting function 11 (module isolation) follows #359. Tests: unit, guard, contract for the new interface members, property for the paging invariants, integration on the 10 database providers through the shared `Collection` fixtures; load and benchmark justified (read-only, not a hot path). The dead-letter admin API is #2229 and is not part of this follow-up.
