# Implementation Plan: Operations Endpoints for Outbox, Saga, Scheduled Message and Inbox

> **Issue**: [#3994](https://github.com/dlrivada/Encina/issues/3994)
> **Type**: Feature (the issue carries `needs-decision`; its own Dilemma D1 is recorded below as recommended, pending the maintainer)
> **Complexity**: High (7 phases, no provider code, 7 pull requests of 4 to 10 files; more if D1 is answered B)
> **Estimated Scope**: ~900 lines of production code + ~2,300 lines of tests and documentation
> **Family**: operations and Aspire. It extends the endpoint model of #2229 (dead letter admin API) to the other four messaging patterns, depends on the tenant rows of #737, #738, #739 and #760 and on the commit owner of #720, and feeds the Aspire dashboard commands of #419. Siblings are referred to by issue number only.

---

## Summary

An operator with a stuck backlog has no way to act on it except custom tooling or direct database access: exhausted outbox rows stay put until someone calls an orchestrator method from code, a stuck saga stays `Running`, and expired inbox rows wait for the purge timer. #3994 adds an opt-in group of HTTP endpoints in `Encina.AspNetCore`, behind an authorization policy, the ambient tenant and a confirmation, that call the operations that exist today. The same endpoints are what the Aspire dashboard commands of #419 call.

**What exists today** (`origin/main` at `278c164f1`, 2026-11-24; the facts of the issue were verified at `ec129cae5` and are re-checked here):

- Outbox: `OutboxOrchestrator.GetExhaustedCountAsync` and `RequeueExhaustedAsync` (all exhausted, or a list of ids) exist and return `Either<EncinaError, int>` (`src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:178,195,212`); the XML documentation says the requeue is saved with `IOutboxStore.SaveChangesAsync`. There is no operation that clears processed rows and no "run now": `ProcessPendingMessagesAsync` needs a publish callback owned by the processor.
- Saga: `SagaOrchestrator.StartCompensationAsync(sagaId, errorCode)` (`:267`) moves a `Running` or `Compensating` saga to `Compensating`, stores the code in `ErrorMessage` and returns the current step; any other status is `saga.invalid_status` (`ErrorCategory.Conflict`). It calls `ISagaStore.UpdateAsync` and no `SaveChangesAsync`; the commit owner of the saga family is `SagaOrchestrator.CommitAsync`, planned in #720. `FailAsync(sagaId, errorCode)` (`:377`) marks the saga `Failed` and captures it into the dead letter queue. There is no cancel operation. **Nothing re-invokes `SagaRunner.CompensateAsync` after a process restart**: the compensation delegates live in the in-memory `BuiltSagaDefinition<TData>` of the process that ran the saga (verified in the #736 plan with a repo-wide search; `GetStuckSagasAsync` has no caller), so an operator "compensate" is a state transition, not an execution of the compensating steps.
- Scheduled message: `SchedulerOrchestrator.CancelAsync(messageId)` exists (`:249`); `ProcessDueMessagesAsync` and `GetPendingCountAsync` exist; there is no run-now operation.
- Inbox: `IInboxStore.GetExpiredMessagesAsync(batchSize)` and `RemoveExpiredMessagesAsync(ids)` exist, and `MessagePurgeCycle.PurgeInboxAsync` runs them in batches for the timer service, but `MessagePurgeCycle` is `internal` and `Encina.Messaging` grants `InternalsVisibleTo` to the test projects and to the data packages, **not to `Encina.AspNetCore`** (`Encina.Messaging.csproj`). `InboxOrchestrator` has no purge member. So the issue's "exists" for the inbox purge is half true: the logic exists, no public operation does.
- Tenancy: the outbox, saga, scheduled message and inbox rows carry **no tenant today**: `git grep TenantId` over the Outbox, Sagas, Scheduling and Inbox folders of `Encina.Messaging` finds none on a row interface, and `ScheduledMessageProcessor.cs:42` documents "`IScheduledMessage` does not carry TenantId yet. See Issue #739"; the inbox orchestrator only passes the request's tenant through metadata. #737, #738, #739 and #760 (all open, plans merged) add the columns. Only the dead letter queue is tenant-scoped.
- HTTP: `MapDPIAEndpoints(authorizationPolicy, prefix)` (`src/Encina.AspNetCore/DPIAEndpointExtensions.cs`) is the fail-closed model: policy required, checked at mapping time (`EnsurePolicyRegistered` is private there; the #2229 plan extracts it to a shared internal helper in its pull request A1). `ToProblemDetails` takes the status from the error category (`error.GetStatusCode()`, `ErrorCategoryHttp`). `Encina.AspNetCore` grants `InternalsVisibleTo` to `Encina.UnitTests`, `Encina.IntegrationTests`, `Encina.ContractTests` and `Encina.PropertyTests` but **not `Encina.GuardTests`**, so guard tests reach only public members.
- EventIds: `EventIdRanges.AspNetCore` is 200-249; 200-207 are used and the merged plans claim 208-211 (#3443), 212-217 (#710), 218-225 (#2229), 226-229 (#454), 230-231 (#355) and 232 (#141); this plan takes **233-240**.

**What this plan delivers**: `MapEncinaOperationsEndpoints(authorizationPolicy, prefix)` with, in the recommended scope (D1 option A): `POST {prefix}/outbox/requeue-exhausted`, `GET {prefix}/outbox/exhausted-count`, `POST {prefix}/sagas/{sagaId}/compensate` and `POST {prefix}/inbox/purge-expired`; a public `InboxOrchestrator.PurgeExpiredAsync`; five error codes; one counter, one span, eight log messages; the tenant gate; and, after #419, the matching Aspire commands.

**Corrections to the issue text**: (1) the metric `encina.operations.requests_total` becomes `encina.operations.requests` (no `_total` suffix, ADR-054); (2) "a repeated call returns a conflict code" holds for the saga (a terminal status is a conflict) but not for the requeue (a second call finds nothing and returns count 0) or the purge; the register below states each operation's repeat behavior; (3) the inbox purge needs a public member first (see above); (4) "runs under the ambient tenant" cannot be implemented for outbox, saga and scheduled rows until their tenant columns exist (Design Choice 2).

**Standards and references**: RFC 9457 problem details (as `ToProblemDetails`), ADR-046 (dead letter), ADR-050 (modules), ADR-054 (observability: D3 names, D4 tenant tag), ADR-062 (error category contract). No law applies.

**Affected packages**: `Encina.AspNetCore` (group, DTOs, gate, telemetry), `Encina.Messaging` (one public purge member), `Encina.Aspire.Hosting` (the last pull request, after #419).

**Provider category**: Database (10) as a consumer only. The endpoints sit above `IOutboxStore`, `ISagaStore`, `IScheduledMessageStore` and `IInboxStore`, which every database provider implements; the one new store-level need (an exact expired-row count, Design Choice 3 option A) would be provider-dependent and is not part of the recommended scope. Integration tests run one provider per family (ADO.NET, Dapper, EF Core, MongoDB).

**Shared pieces and who owns them**: #2229 owns the shared policy helper, the `Encina.AspNetCore` meter and `ActivitySource` and the problem-details mapping idiom; if this plan lands first it creates the same-named meter and source and #2229 reuses them. #419 owns the command infrastructure in `Encina.Aspire.Hosting`. #737, #738, #739 and #760 own the tenant rows. #720 owns `SagaOrchestrator.CommitAsync`. This plan owns the five `operations.*` error codes, the group, the gate and EventIds 233-240.

---

## Design Choices

<details>
<summary><strong>1. Which operations this feature adds (Dilemma D1, the issue's own)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Expose only what exists: requeue exhausted outbox rows (and the exhausted count), start saga compensation, purge expired inbox rows (after adding the one public member the purge needs). The four missing domain operations (outbox process trigger, saga cancel, scheduled run-now, clear processed outbox) are listed in Phase 7 as deferred additions** | Every delivered endpoint calls an operation that is tested today; each missing operation needs its own domain design (what a cancelled saga's compensation means, how the processor is woken, a store method for clearing processed rows on 10 providers) | The issue's seven dashboard commands arrive as three first |
| **B) Add the four missing domain operations in this feature** | All seven commands in one release | The clear-processed operation adds a store method to `IOutboxStore` (10 providers, fakes and tests) and "saga cancel" has no defined semantics; this feature becomes at least six more pull requests and the maintainer decides the semantics inside an endpoint feature |

### Chosen Option: **A — Expose what exists** (recommended, pending the maintainer)

### Rationale

- A wins when each missing operation deserves its own design review (a cancelled saga's compensation is not obvious, and an endpoint must not be the first place that defines it); B wins when the maintainer wants all seven dashboard commands in one release and accepts the split into one pull request set per pattern, which the issue already requires ("decomposed into one sub-issue per pattern before any worker is briefed").
- The compensate endpoint is a state transition only (Summary); its response says the saga is now `Compensating` and names the current step, and the documentation states that nothing resumes the compensating steps of a saga whose process is gone (#736 records the same fact).
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Tenant scoping when the rows have no tenant yet (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed: when multi-tenancy is registered, a pattern's endpoint runs only if that pattern's store is tenant-scoped (a marker the tenant plans add); otherwise it answers the code `operations.tenant_scoping_unavailable` (`ErrorCategory.FailedPrecondition`) and does nothing** | An operator of tenant A can never requeue or compensate rows of tenant B; matches the gate rule of `AGENTS.md` section 3; the endpoint starts working per pattern as #737, #738, #739 and #760 land | Until those land, a multi-tenant application cannot use the endpoints for outbox, saga or scheduled rows (the inbox purge and a single-tenant application are unaffected) |
| **B) Require a second "platform operator" policy for these patterns whenever multi-tenancy is on, and operate on all tenants' rows** | Works today for a platform team that owns the whole deployment | A request that passes the policy touches every tenant's rows; the same cross-tenant mode that #2229 Design Choice 2 leaves pending |
| **C) Operate on all rows regardless of tenancy** | Simplest | A tenant-scoped caller changes other tenants' messages: a cross-tenant leak |

### Chosen Option: **A — Fail closed per pattern** (recommended, pending the maintainer)

### Rationale

- A wins by default (missing context denies); B wins in a deployment run by one platform team that accepts all-tenant operations behind a stricter policy, and would share the policy decision of #2229; C is not an option for a multi-tenant application (it violates the isolation criterion of the issue). A single-tenant application (no `ITenantContext` registered) is unaffected: the gate is off and the requests operate on the whole store, as the orchestrators do.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. How a purge is confirmed (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `expectedCount` compared with an exact expired-row count (as #2229 Design Choice 4 option B)** | The operator deletes exactly what they saw | `IInboxStore` has no count of expired rows (`GetExpiredMessagesAsync` returns a batch); a new store method is provider-dependent: 10 providers, fakes and tests, a feature of its own |
| **B) A bounded purge: the request names `maxMessages` (default 500, at most 5000, validated), the purge removes at most that many expired rows in batches and the response returns the number removed** | No store change; the blast radius is capped; expired rows are by definition no longer needed for idempotency | The operator cannot compare against a count they saw first |

### Chosen Option: **B — A bounded purge** (recommended, pending the maintainer)

### Rationale

- B wins now because the rows are already past retention and a cap bounds the effect; A wins if the maintainer requires the exact-count confirmation of #2229 on every destructive endpoint and accepts the new store method on the 10 providers (a separate plan). The outbox requeue keeps the exact confirmation because both counts exist: a request for "all exhausted" must carry `expectedCount` equal to `GetExhaustedCountAsync` or it is `409` (`operations.confirmation_mismatch`, `ErrorCategory.Conflict`); a request that names ids needs no count.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Opt-in mapping that fails closed (Settled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The group is mapped only by an explicit call that requires a policy name, applies it to every route and throws at mapping time when authorization or the policy is missing; no anonymous mode** | Pay-for-what-you-use; an unmapped group does not exist; a forgotten policy is a start error, not an open endpoint | The application registers a policy |
| **B) Map by default, anonymous unless configured** | Zero setup | State-changing operations open to anyone |

### Chosen Option: **A — Opt-in and fail closed**

### Rationale

Only A complies with the fail-closed rule; the precedent already exists in `MapDPIAEndpoints`.

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed; every messaging pattern is opt-in) and `src/Encina.AspNetCore/DPIAEndpointExtensions.cs` (`MapDPIAEndpoints` requires a policy and checks it at mapping time).

</details>

<details>
<summary><strong>5. Responses and logs carry codes only (Settled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A response body names the operation, the outcome code, counts and, for a saga, the status and step; no `EncinaError.Message`, no payload, no actor; logs carry the error code or `ForLogging()`** | Messages and exception texts can hold personal data | The operator reads a code table |
| **B) Return `error.Message` to ease diagnosis** | Easier reading | Personal data in responses and logs |

### Chosen Option: **A — Codes only**

### Rationale

Only A complies; `ToProblemDetails` exposes a message only in Development behind its own option, and these routes never pass one.

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs, activity tags, health-check results or plaintext storage; an exception passed to a logger goes through `ForLogging()`).

</details>

<details>
<summary><strong>6. A client can never name a tenant, a saga status or an error code (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Request bodies hold only ids, `expectedCount` and `maxMessages`; the tenant is the ambient one; the code written into a compensated saga is the fixed constant `operations.operator_compensation`, never a client value** | `StartCompensationAsync` persists its `errorCode` argument and logs it, so a client-supplied code would be stored data from the network | The operator cannot add a reason |
| **B) Accept a free-text reason** | Documents intent | A client-controlled string persisted in `ISagaState.ErrorMessage` and logged |

### Chosen Option: **A — Fixed inputs**

### Rationale

The orchestrator's own documentation says the code is "never free text".

- **Class:** Trivial
- **Settled by:** `src/Encina.Messaging/Sagas/SagaOrchestrator.cs:256-262` (the XML documentation of `StartCompensationAsync`: the code is persisted and logged, so it must not carry user-supplied text) and the same rule in the #2229 plan (Design Choice 6: no tenant field in a request).

</details>

<details>
<summary><strong>7. Telemetry names and tags (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Counter `encina.operations.requests` (tags `pattern`, `operation`, `outcome` as a code; the tenant tag only when multi-tenancy is on) and span `encina.operations.execute` on the `Encina.AspNetCore` meter and source; no actor, no saga or message id in any tag** | Bounded cardinality (four patterns, a handful of operations, a fixed code set); follows ADR-054 | No per-id breakdown (logs carry the code, never the id of a person) |
| **B) The issue's `encina.operations.requests_total`** | As written in the issue | A `_total` suffix is forbidden by ADR-054 |

### Chosen Option: **A — ADR-054 names**

### Rationale

The meter and source are shared with #2229, whose naming is `encina.dlq.api.requests`.

- **Class:** Trivial
- **Settled by:** `docs/architecture/adr/054-observability-conventions-and-trace-propagation.md` (D3 names and conventions: dot-separated, no `_total` suffix; D4: the tenant is an attribute and a tag when multi-tenancy is on) and the #2229 plan (`encina.dlq.api.requests`).

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions — Error Codes, DTOs and the Public Inbox Purge (pull requests 1 and 2)

> **Goal**: The contract of the group and the one missing public operation.

<details>
<summary>Tasks</summary>

1. **Pull request 1** — `src/Encina.Messaging/Inbox/InboxOrchestrator.cs` (edit): `public Task<Either<EncinaError, int>> PurgeExpiredAsync(int maxMessages, CancellationToken cancellationToken = default)` (`maxMessages` at least 1; it runs `MessagePurgeCycle.PurgeInboxAsync` over the orchestrator's store in batches until the cap or a short batch, and returns the count removed; the store call and the `SaveChangesAsync` belong to the cycle, not to the endpoint). `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.Messaging.json`, the changelog fragment, unit and guard tests.
2. **Pull request 2** — in `src/Encina.AspNetCore/Operations/` (new folder): `OperationsErrorCodes.cs` (five error codes, each with its ADR-062 category in the factory that creates it: `operations.confirmation_mismatch` = `Conflict`; `operations.tenant_scoping_unavailable` = `FailedPrecondition`; `operations.tenant_required` = `Forbidden`; `operations.invalid_request` = `Validation`; `operations.pattern_not_enabled` = `NotFound`; plus the constant `operations.operator_compensation`, a stored saga status code and not an `EncinaError`, so it has no category), `OperationsRequests.cs` and `OperationsResponses.cs` (sealed records: `RequeueExhaustedRequest(IReadOnlyList<Guid>? MessageIds, int? ExpectedCount)`, `PurgeExpiredRequest(int? MaxMessages)`, `OperationResponse(string Pattern, string Operation, int Count)`, `CompensationResponse(Guid SagaId, string Status, int CurrentStep)`), `OperationsTenantGate.cs` (internal static: `Either<EncinaError, Unit> Check(IServiceProvider services, string pattern)`: no multi-tenancy registered returns `Unit`; multi-tenancy registered and no ambient tenant is denied with `operations.tenant_required`; a pattern whose store is not tenant-scoped is `operations.tenant_scoping_unavailable`).
3. `OperationsEndpointExtensions.cs` (public static): `RouteGroupBuilder MapEncinaOperationsEndpoints(this IEndpointRouteBuilder endpoints, string authorizationPolicy, string prefix = "/api/operations")`: `ArgumentNullException.ThrowIfNull(endpoints)`, `ArgumentException.ThrowIfNullOrWhiteSpace` on both strings, the shared policy helper of #2229 (authorization registered and the policy exists, else `InvalidOperationException` at mapping time), `RequireAuthorization(authorizationPolicy)` on the group. Public symbols added: the extension method (the `PublicAPI.Unshipped.txt` line) and `OperationsErrorCodes` constants; the request and response records are public so the contract test and the Aspire commands can bind to them (a constructor line each). No constructor takes a reference parameter that needs a guard besides the records' own nullable members.
4. Repeat behavior register: requeue (a second call finds nothing exhausted and returns count 0: success, no second effect), compensate (`Running` or `Compensating`: the first call moves to `Compensating` and returns the step; a repeat while `Compensating` returns the same step with no new effect, because the status is already set; `Completed`, `Compensated`, `Failed`, `TimedOut`: `saga.invalid_status`, `Conflict`), purge (a second call removes the rows that expired since: count 0 when none).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library; Railway Oriented Programming (Either<EncinaError, T>); issue #3994; plan docs/plans/messaging-operations-endpoints-implementation-plan-3994.md. Dilemmas D1 to D3 are recommended, pending the maintainer: read the Maintainer Decisions section first if it exists.
- MapDPIAEndpoints is the fail-closed model; #2229 extracts the shared policy helper.

TASK:
Pull request 1: add InboxOrchestrator.PurgeExpiredAsync with its tests. Pull request 2: add the error codes (each with its ADR-062 category), the request and response records, OperationsTenantGate and MapEncinaOperationsEndpoints (group and policy only; routes come in Phase 2).

KEY RULES:
- Codes only in responses and logs; never EncinaError.Message.
- No client-supplied tenant, saga status or reason.
- The endpoint layer never calls SaveChangesAsync; the orchestrator method owns the commit.
- Encina.AspNetCore does not grant InternalsVisibleTo to Encina.GuardTests: guard tests cover public members only.

REFERENCE FILES:
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs
- src/Encina.Messaging/Purge/MessagePurgeCycle.cs
- docs/plans/deadletter-admin-api-implementation-plan-2229.md
```

</details>

---

### Phase 2: Endpoint Group — The Routes (pull request 3)

> **Goal**: The four routes, each delegating to one orchestrator call.

<details>
<summary>Tasks</summary>

1. `Operations/OutboxOperations.cs` (internal static): `GET {prefix}/outbox/exhausted-count` returns `OperationResponse` from `OutboxOrchestrator.GetExhaustedCountAsync`; `POST {prefix}/outbox/requeue-exhausted` with `RequeueExhaustedRequest`: ids present (1 to 100, `Guid.Empty` rejected) call `RequeueExhaustedAsync(ids)`; no ids require `ExpectedCount` equal to `GetExhaustedCountAsync` (else `409` `operations.confirmation_mismatch`) and call `RequeueExhaustedAsync()`.
2. `Operations/SagaOperations.cs`: `POST {prefix}/sagas/{sagaId:guid}/compensate` calls `SagaOrchestrator.StartCompensationAsync(sagaId, OperationsErrorCodes.OperatorCompensation)` and then the commit owner `SagaOrchestrator.CommitAsync` of #720 (the endpoint never calls the store's `SaveChangesAsync`); returns `CompensationResponse`. **The route is mapped only after #720 is on `main`**: until then pull request 3 ships the outbox and inbox routes and the saga route waits (the Prerequisites list the order).
3. `Operations/InboxOperations.cs`: `POST {prefix}/inbox/purge-expired` with `PurgeExpiredRequest` (default 500, 1 to 5000) calls `InboxOrchestrator.PurgeExpiredAsync`.
4. Every handler: resolve the orchestrator with `GetService<T>()` from `RequestServices` (scoped; none is injected into a singleton), answer `operations.pattern_not_enabled` when it is not registered, run `OperationsTenantGate.Check` first, map a `Left` with `ToProblemDetails` (status from the error category, never from the code text), map `Right` to the response record. Handlers return `IResult`; no handler swallows a `Left` and none reports success on one.
5. Add the handlers to `MapEncinaOperationsEndpoints` (edit). Unit tests drive each handler with real orchestrators over substituted stores.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #3994, Phase 2. The group, the codes, the DTOs and the tenant gate exist (Phase 1).

TASK:
Write OutboxOperations, SagaOperations (after #720) and InboxOperations as in the Phase 2 Tasks and wire them into MapEncinaOperationsEndpoints, with unit tests.

KEY RULES:
- One orchestrator call per route; the endpoint never calls SaveChangesAsync.
- Gate first (tenant), then confirmation, then the call; a Left is returned, never swallowed.
- Status from ErrorCategory through ToProblemDetails; never parse an error code.
- Never inject a scoped service into a singleton; resolve from RequestServices.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (lines 165-235)
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs (lines 255-300)
- src/Encina.AspNetCore/ProblemDetailsExtensions.cs
```

</details>

---

### Phase 3: Cross-Cutting Integration

> **Goal**: Validation, tenancy, audit, idempotency and transactions as in the matrix below.

<details>
<summary>Tasks</summary>

1. Validation: route ids, the id list (at most 100), `ExpectedCount` (not negative) and `MaxMessages` (1 to 5000) are validated before any call; a failure is `operations.invalid_request` (`Validation`) with no echo of the input.
2. Multi-tenancy: the gate of Design Choice 2; the reserved `default` tenant segment is never invented (no id is built here, so no tenant-qualified key arises); a client can never name a tenant.
3. Audit: each state-changing call (requeue, compensate, purge) records the operator action through the audit port of #2013 when it is registered (intent before, outcome after), carrying the pattern, the operation, the count and the outcome code, never the actor in a tag; with no port registered the call still runs and the absence is logged once at mapping time (the port is not yet on `main`).
4. Transactions: no multi-operation atomicity is claimed; each route is one orchestrator call whose commit owner is documented by that orchestrator (the outbox requeue saves through its store per its XML documentation; `StartCompensationAsync` is followed by `SagaOrchestrator.CommitAsync` of #720).
5. Distributed locks: the claim of a requeue or compensation against concurrent processors follows #713 and #715 (open); this plan adds no lock, and a repeated call is safe by the register of Phase 1.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #3994, Phase 3.

TASK:
Add the request validation, confirm the tenant gate runs before every call, and wire the audit port of #2013 when it exists.

KEY RULES:
- Validation errors never echo the input; the audit entry never carries EncinaError.Message or the actor in a tag.
- No lock and no multi-operation atomicity claim.

REFERENCE FILES:
- docs/plans/deadletter-replay-context-audit-implementation-plan-2013.md
- docs/plans/deadletter-admin-api-implementation-plan-2229.md (validator and purge confirmation)
```

</details>

---

### Phase 4: Observability (pull request 4)

> **Goal**: One counter, one span, eight log messages.

<details>
<summary>Tasks</summary>

1. `Diagnostics/OperationsApiMetrics.cs`: the counter `encina.operations.requests` (unit `{request}`; tags `pattern`, `operation`, `outcome`; the tenant tag through the tenant telemetry helper of #2676 only when multi-tenancy is on, ADR-054 D4) on the `Encina.AspNetCore` meter; and the `encina.operations.execute` span (`operation`, `outcome`; the error status carries the code only) on the `Encina.AspNetCore` `ActivitySource`, both shared with #2229.
2. `Diagnostics/OperationsApiLog.cs` (`[LoggerMessage]`, EventIds **233-240**, packed, inside the registered `AspNetCore` range 200-249, XML doc `Event IDs: 233-240`): 233 group mapped, 234 operation succeeded, 235 operation failed with code, 236 confirmation mismatch, 237 tenant scoping unavailable, 238 tenant or identity context missing, 239 unexpected exception (`ForLogging()`), 240 target not found. Ids 241-249 stay free. No new range, no registry row, no `AssemblyRanges` change (`Encina.AspNetCore` is already mapped).
3. Add the calls to the handlers (edit). No log line holds a saga id, a message id list, a tenant id or an actor; they carry pattern, operation and code.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #3994, Phase 4. EventIds 200-232 are claimed by other plans; this plan takes 233-240 only.

TASK:
Create OperationsApiMetrics and OperationsApiLog and call them from the handlers.

KEY RULES:
- Counter name without a _total suffix; tags pattern, operation, outcome; tenant tag only with multi-tenancy on.
- EventIds 233-240, packed; no raw ids of tenants, users, sagas or messages in tags or logs.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/deadletter-admin-api-implementation-plan-2229.md (Phase 3)
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

### Phase 5: Testing (pull requests 1 to 5)

> **Goal**: Every flag at its target; real orchestrators and real stores.

<details>
<summary>Tasks</summary>

| Flag | Scope | Per-file target (justification) |
|------|-------|---------------------------------|
| Unit | the handlers, the gate, the validator, `InboxOrchestrator.PurgeExpiredAsync`, status mapping, the fail-closed mapping check | `OperationsTenantGate.cs` 95, `OutboxOperations.cs` 90, `SagaOperations.cs` 90, `InboxOperations.cs` 90 (every route outcome reachable with real orchestrators over substituted stores); `OperationsEndpointExtensions.cs` 85 (mapping and policy checks); `InboxOrchestrator.cs` keeps its measured baseline and adds the new branches (targets never go down) |
| Guard | `MapEncinaOperationsEndpoints` (null endpoints; null, empty and whitespace policy and prefix) and `InboxOrchestrator.PurgeExpiredAsync` (`maxMessages` below 1) | `OperationsEndpointExtensions.cs` 90 (only argument checks are reachable by the guard flag; internal handlers are not reachable because `Encina.AspNetCore` does not grant `InternalsVisibleTo` to `Encina.GuardTests`) |
| Contract | the route table, the status per outcome and the response shapes (`tests/Encina.ContractTests/AspNetCore/OperationsEndpointsContractTests.cs`) | `OperationsEndpointExtensions.cs` 50 (the contract flag reaches the mapping only), `OperationsErrorCodes.cs` 100 (constants pinned with their categories) |
| Property | none: the confirmation arithmetic is one equality | justification in the PR |
| Integration | one provider per family through `TestServer` and the shared fixtures: `[Collection("ADO-SqlServer")]`, `[Collection("Dapper-SqlServer")]`, `[Collection("EFCore-SqlServer")]`, `[Collection("MongoDB")]` (`ClearAllDataAsync` in `InitializeAsync`): seed exhausted rows, call requeue with the right and a wrong `expectedCount`, compensate a running saga, purge expired inbox rows; an unauthorized and an anonymous request are refused; a request under tenant A cannot reach tenant B rows once the tenant rows exist | the handler files record their integration target in the manifest (80), measured by the pull request |

Load and benchmark: a justification `.md` each (operator-rate calls).

Measure from the repository root per flag: `dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests --filter "FullyQualifiedName~Operations"` (and the same for `GuardTests`, `ContractTests`, `IntegrationTests`), then `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report` and `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`. CRAP at most 10: each handler is a short method that validates, gates, calls and maps, so the complexity stays low.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #3994, Phase 5. TestServer (Microsoft.AspNetCore.TestHost) is already referenced by Encina.IntegrationTests.

TASK:
Write the tests of the Phase 5 table for the pull request you are on and fill the manifest targets with one-sentence justifications.

KEY RULES:
- Tests execute real orchestrators and real stores; no reflection-only asserts; no Thread.Sleep.
- Integration: shared [Collection] fixtures, ClearAllDataAsync in InitializeAsync; never IClassFixture.
- Justification .md files for load and benchmark only; never for unit, guard, contract or database integration.

REFERENCE FILES:
- tests/Encina.UnitTests/AspNetCore/
- .github/coverage-manifest/Encina.AspNetCore.json
- docs/plans/deadletter-admin-api-implementation-plan-2229.md (Phase 4)
```

</details>

---

### Phase 6: Documentation & Finalization (pull request 6)

> **Goal**: The mandatory documentation set.

<details>
<summary>Tasks</summary>

1. XML docs on every new public API (`<summary>`, `<remarks>` stating "opt-in, fails closed, codes only", `<param>`, `<returns>`, an `<example>` of `app.MapEncinaOperationsEndpoints("EncinaOperators")`).
2. `src/Encina.AspNetCore/README.md` section, an operations guide page under `docs/` and a `docs/INVENTORY.md` row, written through `docs-writer`; the guide states the compensate limit (a state transition, nothing resumes the steps), the tenant behavior of Design Choice 2 and the code table; no hand-typed coverage figures.
3. One changelog fragment per code pull request, `changelog.d/3994-<slug>.added.md` (`-inbox-purge`, `-operations-endpoints-group`, `-operations-routes`, `-operations-telemetry`, `-aspire-commands`); never `CHANGELOG.md`.
4. `PublicAPI.Unshipped.txt` lines and manifest entries through `mechanical-fixer`. No ADR (the choices are recorded as Dilemmas or follow ADR-054, ADR-062 and the #2229 model), no ROADMAP change.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Issue #3994, Phase 6.

TASK:
Finish the XML docs, the README section, the guide page, the INVENTORY row and the changelog fragments.

KEY RULES:
- Documentation pages go through the docs-writer agent; never edit CHANGELOG.md.

REFERENCE FILES:
- src/Encina.AspNetCore/README.md
- changelog.d/README.md
```

</details>

---

### Phase 7: Aspire Commands and the Deferred Domain Operations (pull request 7, and D1 option B)

> **Goal**: The dashboard commands that call the operations; the extra operations if D1 is answered B.

<details>
<summary>Tasks</summary>

1. **Pull request 7** (after #419 pull request 2 is on `main`): in `src/Encina.Aspire.Hosting/Commands/`, add to `EncinaCommandsBuilder` `AddRequeueOutbox()` (`POST {prefix}/outbox/requeue-exhausted` with an `ExpectedCount` argument), `AddCompensateSaga()` (a saga id argument) and `AddPurgeInbox(int maxMessages = 500)` as `EncinaCommandDefinition` entries on the infrastructure of #419 (authentication parameter and tenant header per #419 Design Choices 3 and 4); the commands show a confirmation message and are never retried automatically. Unit tests for each definition and the README of the package.
2. **If D1 is answered B** (each is its own pull request set, planned when chosen; none is part of this plan's count): (a) outbox process trigger: a signal the processor owns (a wake-up the polling loop observes), because `ProcessPendingMessagesAsync` needs the processor's publish callback; (b) saga cancel: needs the maintainer's definition of what cancelling means for steps already executed (state `Failed`, or a new status that touches every `SagaStatus` consumer); (c) scheduled run-now: an orchestrator method that marks the message due and lets `ProcessDueMessagesAsync` pick it up; (d) clear processed outbox rows: a new `IOutboxStore` method on the 10 database providers, their fakes and tests, which alone exceeds one pull request (a plan of its own at that time). Each adds one route and one Aspire command.
3. The Aspire commands for the four deferred operations are added with their operations, never before.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #3994, Phase 7. #419 pull request 2 is on main: EncinaCommandsBuilder, EncinaCommandDefinition and the failure mapper exist.

TASK:
Add AddRequeueOutbox, AddCompensateSaga and AddPurgeInbox to EncinaCommandsBuilder with their tests and README lines.

KEY RULES:
- Commands call the endpoints of this plan only; confirmation text on every command; no automatic retry.
- ResourceCommandVisibility is not authorization: the endpoint authorizes every request.

REFERENCE FILES:
- docs/plans/aspire-dashboard-commands-implementation-plan-419.md
- src/Encina.Aspire.Hosting/Commands/ (after #419)
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Seven pull requests in the recommended scope, in this order (each builds and passes alone; pull request 2 needs the shared policy helper of #2229 pull request A1 on `main`, and the saga route of pull request 3 needs #720):

<!-- local-draft: D:\Proyectos\Encina\.claude\worktrees\pb-e\artifacts\local-ai\out\p3994-prtable.md -->

| PR | Scope | Source files | Test files | Manifest, PublicAPI, README, changelog | Total |
|---|---|---|---|---|---|
| PR1 | Public on-demand inbox purge in `InboxOrchestrator` | 1 | 2 | 3 | 6 |
| PR2 | Endpoint group, error codes, DTOs, tenant gate | 5 | 3 | 2 | 10 |
| PR3 | Outbox, saga and inbox routes | 4 | 2 | 1 | 7 |
| PR4 | Metrics, spans, log messages | 3 | 2 | 1 | 6 |
| PR5 | Integration tests on four provider families and justification files | 0 | 6 | 1 | 7 |
| PR6 | Documentation | 0 | 0 | 4 | 4 |
| PR7 | Aspire dashboard commands in `Encina.Aspire.Hosting` | 2 | 2 | 3 | 7 |
| All | | 15 | 17 | 15 | 47 |

Notes on the counts: pull request 3 counts the three handler files and the edit of the extension; pull request 4 counts the metrics class, the log class and the edit of the extension; no csproj and no ProjectReference edit is needed (`Encina.AspNetCore` already references `Encina.Messaging`, and the test projects already reference `Encina.AspNetCore`); the changelog fragments of pull requests 2 to 4 and 7 are counted in the "other" column and the fragment of pull request 5 is not needed (tests only); constructor sites: no constructor with new parameters is added (`InboxOrchestrator` gains a method, not a parameter), so there is no `new X(` site to update; each pull request stays below about 700 changed lines.

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| RFC 9457 problem details | Response shape of every `Left` (through `ToProblemDetails`) |
| ADR-054 (observability conventions) | Metric and span names, no `_total`, tenant tag rule |
| ADR-062 (error category contract) | The category of every new error code and the HTTP status derived from it |
| ADR-050 (modules may require modules) | Which pattern modules the endpoints need (each pattern is opt-in, so each route answers `operations.pattern_not_enabled` when its pattern is off) |
| ADR-046 (persistent dead letter queue) | The model for an admin API over an orchestrator |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `MapDPIAEndpoints`, `EnsurePolicyRegistered` | `src/Encina.AspNetCore/DPIAEndpointExtensions.cs` | The fail-closed mapping model; the shared helper arrives with #2229 |
| `ToProblemDetails`, `ErrorCategoryHttp` | `src/Encina.AspNetCore/ProblemDetailsExtensions.cs` | Status from the error category |
| `OutboxOrchestrator.RequeueExhaustedAsync`, `GetExhaustedCountAsync` | `src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:178-212` | The outbox operations |
| `SagaOrchestrator.StartCompensationAsync`, `FailAsync` | `src/Encina.Messaging/Sagas/SagaOrchestrator.cs:267,377` | The saga operation (and the commit owner of #720) |
| `MessagePurgeCycle.PurgeInboxAsync`, `InboxOrchestrator` | `src/Encina.Messaging/Purge/`, `src/Encina.Messaging/Inbox/` | The purge logic behind the new public member |
| `EventIdRanges.AspNetCore` | `src/Encina/Diagnostics/EventIdRanges.cs` | Ids 233-240 inside the existing range |
| `TestServer` integration tests | `tests/Encina.IntegrationTests/` | Endpoint integration test style (as #2229) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.AspNetCore` (existing range 200-249) | 233-240 | `OperationsApiLog`: eight messages, packed. 200-207 used; 208-231 and 232 claimed by merged plans (#3443, #710, #2229, #454, #355, #141); 241-249 stay free. No new range, registry row or `EventIdRanges` field |

### Provider Matrix

| Provider | Operations | Notes |
|----------|:-:|-------|
| ADO-SqlServer / ADO-PostgreSQL / ADO-MySQL | via stores | Covered by the ADO family integration test |
| Dapper-SqlServer / Dapper-PostgreSQL / Dapper-MySQL | via stores | Covered by the Dapper family integration test |
| EFCore-SqlServer / EFCore-PostgreSQL / EFCore-MySQL | via stores | Covered by the EF Core family integration test |
| MongoDB | via stores | Covered by the MongoDB integration test |

No provider-specific code is added in the recommended scope (D3 option B avoids the one store method that would be).

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production code | 15 | 1 edit in Messaging (pull request 1), 5 new in pull request 2, 4 in pull request 3 (3 new handler files and the edit of the extension), 3 in pull request 4 (2 new telemetry files and the edit of the extension), 2 in `Encina.Aspire.Hosting` (pull request 7) |
| Test files | 17 | unit, guard, contract, 4 integration, 2 justification `.md` |
| Manifest, PublicAPI, README, changelog | 15 | across seven pull requests |
| Total | 47 | 6 + 10 + 7 + 6 + 7 + 4 + 7 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Railway Oriented Programming (Either<EncinaError, T>); no [Obsolete]; XML docs on every public API; zero warnings; the endpoint layer never calls SaveChangesAsync.
- Issue #3994: an opt-in HTTP group for outbox, saga and inbox operations behind an authorization policy, the ambient tenant and a confirmation. Plan: docs/plans/messaging-operations-endpoints-implementation-plan-3994.md. Dilemmas D1 to D3 are recommended, pending the maintainer; read the Maintainer Decisions section first if it exists.

IMPLEMENTATION OVERVIEW:
1. Phase 1: InboxOrchestrator.PurgeExpiredAsync (pull request 1); OperationsErrorCodes, request and response records, OperationsTenantGate and MapEncinaOperationsEndpoints (pull request 2).
2. Phase 2: the routes: outbox exhausted-count and requeue-exhausted, saga compensate (after #720), inbox purge-expired.
3. Phase 3: validation, tenant gate, audit port of #2013, no locks, no atomicity claims.
4. Phase 4: counter encina.operations.requests, span encina.operations.execute, OperationsApiLog EventIds 233-240.
5. Phases 5 to 7: tests per flag on four provider families, documentation, then the Aspire commands.

KEY PATTERNS:
- Fail closed: no policy, no mapping; no tenant context or an unscoped pattern with multi-tenancy on, no operation.
- Codes only in responses, logs and tags; no payload, no actor, no ids in tags; a client never names a tenant, a status or a reason.
- Status from ErrorCategory (ADR-062); five operations.* codes with their categories; no _total metric suffix.
- Encina.AspNetCore has no InternalsVisibleTo for GuardTests: guard tests reach public members only.
- Shared [Collection] integration fixtures; per-flag targets from the Encina.AspNetCore and Encina.Messaging manifests; CRAP at most 10.

REFERENCE FILES:
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Messaging/Purge/MessagePurgeCycle.cs
- docs/plans/deadletter-admin-api-implementation-plan-2229.md
- docs/plans/aspire-dashboard-commands-implementation-plan-419.md
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: operations, not reads; nothing worth caching |
| 2 | OpenTelemetry | ✅ | Phase 4: counter `encina.operations.requests` and span `encina.operations.execute`; the tenant tag only when multi-tenancy is on (ADR-054 D4); no ids in tags |
| 3 | Structured Logging | ✅ | Phase 4: `OperationsApiLog`, EventIds 233-240 inside the existing `AspNetCore` range; codes only, `ForLogging()` for exceptions |
| 4 | Health Checks | ❌ | Not applicable: an operator endpoint has no dependency of its own to check |
| 5 | Validation | ✅ | Phases 1 and 3: ids, id list size, `ExpectedCount` and `MaxMessages` validated before any call (`operations.invalid_request`) |
| 6 | Resilience | ❌ | Not applicable: one call per request; a state-changing operation is never retried automatically (the Aspire commands add no retry) |
| 7 | Distributed Locks | ⏭️ | Deferred to #713 and #715 (open): the claim of a requeue or compensation against concurrent processors; no lock is added here and a repeated call is safe by the register |
| 8 | Transactions | ✅ | One orchestrator call per route with its own commit owner (the outbox requeue per its documentation; compensation followed by `SagaOrchestrator.CommitAsync` of #720); no multi-operation atomicity is claimed |
| 9 | Idempotency | ✅ | Phase 1 register: a repeated requeue or purge returns count 0, a repeated compensate while `Compensating` has no new effect, a terminal saga is a conflict code |
| 10 | Multi-Tenancy | ✅ | Design Choice 2: ambient tenant only; a pattern without tenant rows fails closed per pattern until #737, #738, #739 and #760 land; the reserved `default` segment is never invented and no tenant-qualified id is built |
| 11 | Module Isolation | ❌ | Not applicable: operations are not module-scoped; a module-scoped variant follows `ModuleId` on the messaging rows (#747) |
| 12 | Audit Trail | ✅ | Phase 3: each state-changing operator action through the audit port of #2013 when registered; never the actor in a tag |

---

## Prerequisites & Dependencies

- **Blocking for pull request 2**: #2229 pull request A1 (the shared policy helper, `MapEncinaDeadLetterEndpoints` and its DTO idiom) on `main`. **Blocking for the saga route**: #720 (`SagaOrchestrator.CommitAsync`). The `dlq.*` and ADR-062 category work of #3419 is already used by `ToProblemDetails` (`error.GetStatusCode()`).
- **Required for multi-tenant applications**: #737 (outbox), #738 (saga), #739 (scheduled message) and #760 (inbox) tenant rows; without them Design Choice 2 option A refuses the pattern.
- **Advisable**: #2013 (the audit port), #713 and #715 (locks against concurrent processors), #736 (the saga compensation idempotency plan that states nothing resumes compensation after a restart).
- **Consumer**: #419 (Aspire dashboard commands; its plan delivers only `retry-dead-letters` and names this issue for the rest) and its pull request 2 for Phase 7.
- Deferred inside this plan (not new issues): the four missing domain operations of Design Choice 1 option B (Phase 7 item 2); the exact expired-count store method of Design Choice 3 option A (10 providers, its own plan at that time); the scheduled-message routes (`cancel` exists; run-now is part of option B) once #739 lands.

---

## Next Steps

1. The maintainer answers Dilemmas D1 to D3 when work on #3994 starts (D1 is the issue's own `needs-decision`); the answers go into `## Maintainer Decisions` at the end of this plan.
2. Brief pull request 1 (the inbox purge, independent of the other plans), then pull request 2 once #2229 A1 is on `main`; pull requests 3 to 7 follow in order, the saga route and the Aspire commands waiting for #720 and #419.
3. After pull request 6, link the guide page from #3994 and #419.
