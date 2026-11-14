# Implementation Plan: Dead Letter Queue Operations — an Opt-in Admin API over `IDeadLetterManager`

> **Issue**: [#2229](https://github.com/dlrivada/Encina/issues/2229)
> **Type**: Feature
> **Related**: #583 (persistent dead letter stores, closed), #2013 (replay under the stored context and audited operator actions; this plan consumes its audit), #135 (quarantined rows), #1991 (closed: what reaches the queue), #2203 (saga not found identity), #419 and #445 (alternative surfaces), #3419 (HTTP status from the error category; open), ADR-046, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: Medium (one package plus the error categories of the dead letter codes in `Encina.Messaging`, no provider code)
> **Estimated Scope**: ~1,000 lines of production code + ~1,900 lines of tests + ~250 lines of documentation (~3,150 in all), in five pull requests of at most 15 files and 1,000 lines each (table in Research)

---

## Summary

### What exists today (read on 2026-11-11, main `68d08ddae`)

- `IDeadLetterManager` (`src/Encina.Messaging/DeadLetter/IDeadLetterManager.cs`) offers `ReplayAsync`, `ReplayAllAsync(filter, maxMessages = 100)`, `GetMessageAsync`, `GetMessagesAsync(filter, skip, take)`, `GetCountAsync`, `GetStatisticsAsync`, `DeleteAsync`, `DeleteAllAsync(filter)` and `CleanupExpiredAsync`, all returning `Either<EncinaError, T>`. `ReplayResult`, `BatchReplayResult` and `DeadLetterStatistics` are the result records.
- The manager is tenant-aware: it defaults to the ambient tenant, denies `DeadLetterFilter.AllTenants` without an explicit opt-out and returns `encina.authorization.dlq_tenant_required` (`DeadLetterErrorCodes.TenantRequired`); `DeadLetterFilter.All` deletes the whole queue without confirmation at manager level (`DeadLetterFilter.cs:104`). The result of a replay is a code (`ReplayResult.ErrorMessage` holds a code, not exception text, #2012).
- The one endpoint group in `Encina.AspNetCore` is `MapDPIAEndpoints(string authorizationPolicy, string prefix)` (`src/Encina.AspNetCore/DPIAEndpointExtensions.cs:144`): it checks at mapping time that authorization is registered and the policy exists (`EnsurePolicyRegistered` `:523`, throws `InvalidOperationException`), groups the routes, calls `RequireAuthorization` and maps `Left` to RFC 9457 problem details with `ToProblemDetails` (`ProblemDetailsExtensions.cs:67`, status inferred today by the string-suffix `MapErrorCodeToStatusCode` at `:173`; #3419 replaces it with the error category of ADR-062). `ToProblemDetails` can expose `detail` and the exception message when `IncludeErrorMessageInProblemDetails` is on in Development (`ProblemDetailsExtensions.cs:77-98`).
- The manager gate on tenancy accepts any `DeadLetterFilter.TenantId` the caller names without comparing it with the ambient tenant (`DeadLetterManager.cs:136-138`, `TryResolveTenant`): a named tenant is the caller's responsibility, so the HTTP layer must never let a client set it (Design Choice 6).
- Result shapes: `ReplayAsync` returns `Left(dlq.already_replayed)` for a row already replayed (`DeadLetterManager.cs:251-253`), `Right(ReplayResult.Failed(id, ReplayInProgress))` when another replay holds the claim (`:236-239`) and `Right(Failed(AlreadyReplayed))` after a lost race (`:305-306`); `CleanupExpiredAsync` delegates to the orchestrator with no tenant check (`:630-633`).
- `Encina.AspNetCore` already references `Encina.Messaging`. It owns no `Meter` or `ActivitySource` today and uses EventIds 200-207 of `AspNetCore = (200, 249)`.
- Replay and delete are not audited today; #2013 adds the audit port that the manager calls.

### What this plan delivers

`MapEncinaDeadLetterEndpoints` in `Encina.AspNetCore`: list, read one, count, statistics, replay one, replay by filter, delete one and purge by filter, each delegating to `IDeadLetterManager` and returning codes only. Nothing is mapped unless the application calls it, and the group never runs anonymous. A UI is out of scope (comment of 2026-10-17 on #2229).

- Affected packages: `Encina.AspNetCore` (endpoints, DTOs, metrics, logs); `Encina.Messaging` only if Design Choice 3 needs a manager member. No provider code.
- Provider category: none; the endpoints sit above `IDeadLetterStore`, which all 10 database providers implement. The integration tests run the endpoints against one provider per family.

---

## Design Choices

<details>
<summary><strong>1. Surface and authorization shape</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) API only: `MapEncinaDeadLetterEndpoints(authorizationPolicy, prefix)` like `MapDPIAEndpoints`** | Every UI, Aspire command or CLI needs the API underneath | An operator without a front end uses curl or Swagger |
| **B) API plus a bundled UI package** | Closest to ServicePulse and Fleet Manager | A second product to maintain, secure and test; pricing and scope of the competitors unverified |

### Chosen Option: **A — API only**

### Rationale

- The issue comment of 2026-10-17 settled "API now, UI later on top". The fail-closed mapping, the missing cleanup endpoint and the other defaults are their own register rows (F1 to F6 below).

**Class:** Settled
**Settled by:** the settled-by-elimination comment of 2026-10-17 on #2229; comparable products NServiceBus ServicePulse and Rebus Fleet Manager (API first)

</details>

<details>
<summary><strong>2. Cross-tenant operation over HTTP</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not exposed: every request uses the ambient tenant; cross-tenant work stays in code** | Smallest attack surface | A platform operator cannot see all tenants through the API |
| **B) An optional second parameter `allTenantsPolicy`; when null the cross-tenant query parameter returns 403, when set it sets `DeadLetterFilter.AllTenants` after its own authorization check (the manager logs it)** | Platform operators supported; opt-in and fail closed | Two policies to explain; one more branch to test |

### Chosen Option: **B — Optional second policy for the cross-tenant mode** (recommended, pending the maintainer)

### Rationale

- The issue requires the mode to have its own policy and to be logged; the manager already logs `AllTenants` by event.
- A wins when only tenants operate their own queues and platform staff use database tools.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What a response contains</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `DeadLetterMessageResponse` without `RequestContent`, `ExceptionStackTrace` or the actor (once #2013 adds it; today no `ActorId` exists): ids, `RequestType`, `ErrorCode`, `ExceptionType`, `SourcePattern`, `SourceMessageId`, `CorrelationId`, `TenantId`, `TotalRetryAttempts`, UTC timestamps, `IsReplayed`, `ReplayResult`** | The payload may hold personal or health data and the stack trace names internals; nothing sensitive leaves through HTTP | An operator cannot read the payload in the API |
| **B) Add `RequestContent` to the read-one response behind a separate policy** | Operators can inspect what they replay | A new privileged read path for personal data; larger test and audit surface |

### Chosen Option: **A — No payload and no stack trace over HTTP** (recommended, pending the maintainer)

### Rationale

- `EncinaError.Message` never reaches responses (`AGENTS.md` section 3) and the stored payload is the application's data; A keeps the first release free of a personal-data read path.
- B wins when operators routinely need to inspect a payload and the deployment accepts the audit trail of such reads.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How a purge is confirmed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `confirm=true` is required; an empty filter (the whole queue) also needs `all=true`** | Simple to call and to test | Deletes rows that arrived between the operator's look and the call |
| **B) `expectedCount` must equal `GetCountAsync` for the same filter, else 409, and the delete is bounded by `DeadLetteredBeforeUtc` set to the instant of the list the operator saw** | The operator deletes what they saw: rows that arrive after that instant are outside the filter; no accidental whole-queue purge | A count query per purge; clients must do a list first; count, compare and `DeleteAllAsync` are not atomic, so a row dead-lettered before the instant and added between the check and the delete is still deleted, the same race as option A for rows inside the bound |

### Chosen Option: **B — Expected count confirms the purge** (recommended, pending the maintainer)

### Rationale

- `DeleteAllAsync(DeadLetterFilter.All)` deletes without confirmation at manager level (`DeadLetterFilter.cs:104`), so the HTTP contract carries the only guard. The comparison between the options is fair only with the race stated: the bound removes the arrivals after the list from A's con, not the check-to-delete window.
- A wins when scripts call the API without a previous list and the volume is small.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Where the audit lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Audit and the tenant restore stay in the manager (#2013); the API calls the manager** | One trail, one place for the rules; replays by code and by API land in the same trail | Depends on #2013 for the audit acceptance criterion |
| **B) Audit in the HTTP layer** | No dependency on #2013 | Misses replays by code; two trails to reconcile |

### Chosen Option: **A — Audit stays in the manager**

### Rationale

- Option B leaves replays and deletes made in code unaudited, which the issue's audit criterion does not allow; responses and logs carry error codes only (AGENTS.md section 3).

**Class:** Settled
**Settled by:** SPEC-002 REQ-019 (fail-closed audit of privileged actions), AGENTS.md section 3 (`EncinaError.Message` rule), ADR-046

</details>

<details>
<summary><strong>6. Can a client name a tenant in the filter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DeadLetterFilterRequest` has no tenant field; the tenant is always the ambient one, and the cross-tenant mode of Design Choice 2 is the flag `allTenants=true` after `allTenantsPolicy` succeeds** | A client can never name another tenant; one rule to test | A platform operator cannot target one other tenant, only all of them |
| **B) A `tenantId` query field accepted only after `allTenantsPolicy` succeeds, mapped to `DeadLetterFilter.TenantId`** | A platform operator can target one tenant | The manager accepts any named tenant (`DeadLetterManager.cs:136-138`), so a missed authorization check is a cross-tenant read, replay or purge; one more branch to test |

### Chosen Option: **A — No tenant field** (recommended, pending the maintainer)

### Rationale

- The unit and integration tests send `?tenantId=<other>` under the ambient tenant and assert the field is ignored and never reaches the manager (A) or is refused (B). Both options keep the acceptance criterion "a request without a tenant cannot read or delete another tenant's dead letters", so this is a maintainer choice.
- B wins when platform staff must act on exactly one tenant through the API.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Error categories, HTTP statuses and the Right results of a replay</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A failed `ReplayResult` or a batch with failures is HTTP 200 with the outcome code in the body** | The call completed and the outcome is data; one rule for batches | A client must read the body to see `dlq.already_replayed` or `dlq.replay_in_progress` |
| **B) A single failed `ReplayResult` whose code is a state conflict (`AlreadyReplayed`, `ReplayInProgress`) is HTTP 409 derived from the outcome; any other failed single result and every batch is 200 with the body** | Idiomatic retry semantics for a single replay | Two rules; the outcome-to-status table is a new mapping that picks a status from the outcome code text (`dlq.already_replayed`, `dlq.replay_in_progress`), the parsing ADR 062 forbids for `Left` errors (there it is a `Right` result, so ADR 062 does not cover it, but the same objection applies) |

### Chosen Option: **B — 409 for a state-conflict outcome of a single replay, 200 for the rest** (recommended, pending the maintainer)

### Rationale

- Under ADR 062 a host never parses an error code to choose a status, so the `Left` codes carry an `ErrorCategory` assigned where `DeadLetterManager` creates them. That work is owned by open sub-issue #3724 (#3419 M2, which lists `src/Encina.Messaging/DeadLetter/ => M2` in `ErrorCategoryPending.txt`); this plan does not duplicate it. The categories it must yield for this API: `NotFound` to `NotFound`, `AlreadyReplayed` to `Conflict`, `dlq.expired` to `Conflict` (register row F7), `encina.authorization.dlq_tenant_required` to `InvalidRequest` (ADR 062: "the two `encina.authorization.*_tenant_required` codes are `InvalidRequest` (400)"), store and query failures to `Internal`. This plan does not edit `ProblemDetailsExtensions.cs`: the status table is #3683 (S2) and its consumption by `ToProblemDetails` is #3688 (H1). The HTTP status of a `Right` outcome is not an error category, so this choice is its own mapping.
- An HTTP retry after a successful replay gets 409 (`Left(AlreadyReplayed)` converted by the category), not the recorded outcome, and nothing is dispatched twice (the claim guarantees it, `TryClaimForReplayAsync`, #2012).
- A wins when clients are scripts that only read bodies.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| F1 | The group fails closed when it is mapped: `InvalidOperationException` when authorization or the policy is missing, not a deny on each request (the issue allows both) | Trivial | precedent `DPIAEndpointExtensions.cs` (`EnsurePolicyRegistered`), AGENTS.md section 3 |
| F2 | No cleanup endpoint: `CleanupExpiredAsync` (`DeadLetterManager.cs:630-633`) has no tenant check, so an HTTP route would purge every tenant's expired rows; retention cleanup is also not an operator action (#2013 C11) | Settled | AGENTS.md section 3 (tenant isolation fails closed); #2013 C11 |
| F3 | Default route prefix `/api/dead-letters` | Dilemma (recommended, pending the maintainer) | a public default; `MapDPIAEndpoints` has its own prefix |
| F4 | Boundary limits: default `take` 50 and maximum `DeadLetterStoreLimits.MaxPageSize` (1000, verified); `maxMessages` of `POST /replay` capped at the manager default of 100 (`ReplayAllAsync(maxMessages = 100)`) | Dilemma (recommended, pending the maintainer) | numeric defaults |
| F5 | `GetMessageAsync` returning `Right(None)` is 404 | Trivial | the `NotFound` category (ADR-062) and the `DPIAEndpointExtensions.cs` read routes |
| F6 | Metric names `encina.dlq.api.requests` and `encina.dlq.api.purged` (no `_total`) | Settled | ADR-054 point 4 (no `_total` suffix on counters, `054-...md:22`); the existing `encina.dlq.*_total` names predate it |
| F7 | `dlq.expired` is `Conflict` (409); `encina.authorization.dlq_tenant_required` is `InvalidRequest` (400) | Settled | ADR-062 R2 ("an invalid transition is `Conflict`; do not use `FailedPrecondition` before #3420", which also leaves no 410 row) and ADR-062 ("the two `encina.authorization.*_tenant_required` codes are `InvalidRequest` (400)"); assigned by #3724 |
| F8 | The Development-only `IncludeErrorMessageInProblemDetails` opt-in of `ToProblemDetails` applies to this group unchanged (it is the application's own switch, off by default); the group adds no other message exposure | Dilemma (recommended, pending the maintainer) | `ProblemDetailsExtensions.cs:77-98`; the alternative is to force the message off for this group, which also complies with AGENTS.md section 3 |
| F9 | Logs and telemetry carry the operation and error code only; no payload, no actor, tenant tag only with multi-tenancy | Settled | AGENTS.md section 3; ADR-054 D4 |

---

## Implementation Phases

### Phase 1: Contract, DTOs and Endpoint Group

> **Goal**: The route table and request/response types, with fail-closed mapping.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.AspNetCore/DeadLetterEndpointExtensions.cs` (pull request A1): `MapEncinaDeadLetterEndpoints(this IEndpointRouteBuilder endpoints, string authorizationPolicy, string? allTenantsPolicy = null, string prefix = "/api/dead-letters")` returning `RouteGroupBuilder`; reuse the policy check of `EnsurePolicyRegistered` (extract it to a shared internal helper rather than copy it, which changes `DPIAEndpointExtensions.cs`).
2. **Routes**: read routes in A1: `GET /` (filter, `skip`, `take`), `GET /{id:guid}`, `GET /count`, `GET /statistics`; write routes in A2: `POST /{id:guid}/replay`, `POST /replay` (filter, `maxMessages`), `DELETE /{id:guid}`, `DELETE /` (filter, `expectedCount`). No cleanup route (register row F2).
3. **Create** `DeadLetter/DeadLetterMessageResponse.cs`, `DeadLetterFilterRequest.cs` (fields: source pattern, request type, error code, correlation id, replayed flag, the two dead-lettered instants, source message id; no tenant field, Design Choice 6), `DeadLetterPageResponse.cs`, `PurgeResponse.cs` (records; Design Choice 3) and the mapper from `IDeadLetterMessage`. The actor is not in the response (once #2013 adds `ActorId`, it is still excluded).
4. **Prerequisites, not tasks**: the `dlq.*` categories (#3724), the core HTTP status table (#3683) and `ToProblemDetails` reading the category (#3688) are separate open issues, verified with `gh api repos/dlrivada/Encina/issues/<n>`; until they land `ToProblemDetails` answers 500 for `dlq.already_replayed` and `encina.authorization.dlq_tenant_required` (suffix rules), so every pull request of this plan is sequenced after them. `ProblemDetailsExtensions.cs` is not edited.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #2229 adds an opt-in admin API over IDeadLetterManager in
Encina.AspNetCore, modelled on src/Encina.AspNetCore/DPIAEndpointExtensions.cs. Apply the maintainer's answers
to Design Choices 2 to 4 of docs/plans/deadletter-admin-api-implementation-plan-2229.md.

TASK:
1. Create MapEncinaDeadLetterEndpoints, the DTO records and the mapper; share the policy check with MapDPIAEndpoints.
2. Map every Left through ToProblemDetails; the status comes from the ErrorCategory the manager gives each dlq.* code
   (ADR 062), never from the code text.
3. Never put RequestContent, ExceptionStackTrace or EncinaError.Message in a response; the filter request has no tenant field.

KEY RULES:
- Fail closed: no policy, no mapping (InvalidOperationException at start-up).
- Either<EncinaError, T> handled with Match; no exceptions for business outcomes; XML docs on every public member.

REFERENCE FILES:
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs, ProblemDetailsExtensions.cs
- src/Encina.Messaging/DeadLetter/IDeadLetterManager.cs, DeadLetterFilter.cs, DeadLetterErrorCodes.cs
```

</details>

---

### Phase 2: Validation, Purge Confirmation and Cross-Tenant Mode

> **Goal**: The rules of the issue enforced at the HTTP boundary.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `DeadLetter/DeadLetterRequestValidator.cs` (A2): `take` within `DeadLetterStoreLimits.MaxPageSize`, string lengths from `DeadLetterStoreLimits`, `maxMessages` cap (register row F4), date order; failures are `400` problem details with a code.
2. **Implement** the purge confirmation (Design Choice 4): count first, compare with `expectedCount`, then `DeleteAllAsync` with `DeadLetteredBeforeUtc` set to the instant of the list; a mismatch is `409`.
3. **Implement** the cross-tenant mode (Design Choice 2): authorize `allTenantsPolicy` through `IAuthorizationService` before setting `AllTenants`; null policy returns `403`.
4. **Verify** with a unit test and an integration test that a request with `?tenantId=<other>` under the ambient tenant never reaches `DeadLetterFilter.TenantId` (Design Choice 6 option A), and that a request without a tenant cannot read or delete another tenant's rows (the manager returns `dlq_tenant_required`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 created the group and DTOs. The manager defaults to the ambient tenant and denies AllTenants without opt-out.

TASK:
1. Add the validator, the purge confirmation and the cross-tenant authorization step.
2. Return 400/403/409 problem details with codes only.

KEY RULES:
- The default is the ambient tenant; AllTenants only after the second policy succeeds.
- Whole-queue purge (empty filter) is never executed without the confirmation of Design Choice 4.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/DeadLetterStoreLimits.cs, DeadLetterManager.cs (tenant handling at :100-132)
```

</details>

---

### Phase 3: Observability and Cross-Cutting Integration

> **Goal**: Metrics, span and logs for the endpoint group.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `Diagnostics/DeadLetterApiMetrics.cs` on a meter named `Encina.AspNetCore` (ADR-054 D3): `encina.dlq.api.requests` (tags operation, outcome code; tenant only with multi-tenancy on, ADR-054 D4) and `encina.dlq.api.purged` (no `_total` suffix, ADR-054 point 4; register row F6). The issue's `encina.deadletter.api.*` proposals are renamed to the `encina.dlq.*` family.
2. **Create** an `ActivitySource` `Encina.AspNetCore` with spans `encina.dlq.api.replay` and `encina.dlq.api.purge`; no tenant or actor attributes beyond the ADR-054 rule.
3. **Create** `Diagnostics/DeadLetterApiLog.cs` with eight `[LoggerMessage]` EventIds 218-225 inside the registered `AspNetCore` range (200-249); codes only, `ForLogging()` for exceptions. The assembly is already mapped in `EncinaEventIdAllocationTests`, so no edit of it. This is pull request A3, the first that logs, so no earlier pull request logs or emits a metric.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Encina.AspNetCore has no Meter or ActivitySource yet. EventIds 200-207 are used and 208-217 are reserved by other plans; the range is (200, 249).

TASK:
1. Add DeadLetterApiMetrics, the ActivitySource and DeadLetterApiLog (EventIds 218-225, packed sequentially).

KEY RULES:
- Tags and logs carry operation and error code only; never EncinaError.Message, payload or actor.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/DeadLetterMetrics.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 4: Testing

> **Goal**: Every applicable flag reaches its manifest target for `Encina.AspNetCore`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** (`tests/Encina.UnitTests/AspNetCore/`, next to `DPIAEndpointExtensionsTests.cs`): handlers with a real `IDeadLetterManager` over a substituted store, DTO mapping, status mapping, fail-closed policy, validator.
2. **Guard tests** for every public `Map*` method and option parameter; **contract tests** for the route and DTO shapes.
3. **Property tests** (FsCheck): filter-to-query round trip and paging bounds.
4. **Integration tests** through `TestServer` (`Microsoft.AspNetCore.TestHost`, which `Encina.IntegrationTests` already references; `WebApplicationFactory` would need `Microsoft.AspNetCore.Mvc.Testing`, which it does not, so it is not used and no package is added) against real stores on the shared collection fixtures, one provider per family (ADO, Dapper, EF Core, MongoDB), including a cross-tenant denial (`?tenantId=<other>` under the ambient tenant) and a purge with a stale `expectedCount`.
5. Load and benchmark: justification files (a contract-test justification is never allowed).

Per-file targets, entered in `.github/coverage-manifest/Encina.AspNetCore.json` by the pull request that adds the tests that meet them (one-sentence justification each; measured with `coverage-report.cs`, reported in the pull request). The unit, guard, contract and property targets are entered by the pull request that adds the file (A1 to A3); the integration targets are entered only by A4, the pull request that adds the integration tests, so no pull request enters a target it cannot meet:

| File | Unit, guard, contract, property: PR and targets | Integration (A4) | Justification |
|------|-------------------------------------------------|------------------|---------------|
| `DeadLetterEndpointExtensions.cs` | A1, A2: unit 85, guard 40, contract 40, property 0 | 70 | Thin handlers over the manager, exercised end to end by `TestServer`; the property tests target the validator, not the routes |
| `DeadLetter/DeadLetterRequestValidator.cs` | A2: unit 95, guard 80, contract 0, property 90 | 40 | Pure rules with every branch unit tested and the paging bounds under FsCheck; no public type contract to pin |
| `DeadLetter/DeadLetterMessageResponse.cs`, `DeadLetterFilterRequest.cs`, `DeadLetterPageResponse.cs`, `PurgeResponse.cs` | A1: unit 90, guard 0, contract 50, property 0 | 60 | Records with no logic, covered by mapping and shape tests; guard has no parameters to check |
| DTO mapper | A1: unit 95, guard 80, contract 0, property 70 | 60 | One mapping per field, every field asserted, the filter round trip under FsCheck |
| `Diagnostics/DeadLetterApiMetrics.cs` | A3: unit 90, guard 80, contract 0, property 0 | 0 | Counters asserted with an in-memory listener; not an integration concern |
| `Diagnostics/DeadLetterApiLog.cs` | A3: unit 80, guard 0, contract 0, property 0 | 0 | Source-generated; asserted through the allocation test and a logger test, no parameters to guard |
| `DPIAEndpointExtensions.cs` (shared helper) | A1: unit 85 (was 80), guard 20 (was 4), contract 0, property 0 | 0 | The extracted helper gains its own tests, so the targets rise and never go down |

A 0 appears only where the reason is in the row (no parameters, no executable logic, or a flag that does not apply to the file); every public type of `Encina.AspNetCore` is reachable by `Encina.GuardTests` (`InternalsVisibleTo` is granted in `Properties/AssemblyInfo.cs`), so no 0 rests on that. No contract-test justification file is written.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Per-flag coverage (AGENTS.md section 9) for Encina.AspNetCore; targets in .github/coverage-manifest/Encina.AspNetCore.json.

TASK:
1. Write unit, guard, contract, property and integration tests with real implementations; measure per-flag coverage and CRAP.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; shared [Collection] fixtures only.
- CRAP <= 10: keep handlers thin, move rules into the validator.

REFERENCE FILES:
- tests/Encina.UnitTests/AspNetCore/DPIAEndpointExtensionsTests.cs, tests/Encina.IntegrationTests (Collections.cs files)
```

</details>

---

### Phase 5: Documentation and Finalization

> **Goal**: Operators can secure and call the API.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with examples; `PublicAPI.Unshipped.txt` of `Encina.AspNetCore`.
2. `changelog.d/2229-deadletter-admin-api.added.md`; `Encina.AspNetCore` README; operations guide (routes, policies, purge confirmation); `docs/INVENTORY.md`.
3. `dotnet build --configuration Release` with zero warnings; `dotnet test` with every flag at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
The endpoints are implemented and tested. Documentation is written by the docs-writer agent following the encina-docs skill.

TASK:
1. Complete XML docs and PublicAPI; write the changelog fragment, README and the operations guide.

KEY RULES:
- English only; never edit CHANGELOG.md; no hand-typed coverage numbers.

REFERENCE FILES:
- changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| RFC 9457 | Problem details for HTTP APIs | Error responses through `ToProblemDetails` |
| SPEC-002 REQ-019 | Fail-closed audit of privileged actions | Replay and purge audited through #2013 |
| ADR-046, ADR-054 | Dead letter queue; telemetry names and tenant tag | Behavior and metrics |
| NServiceBus ServicePulse, Rebus Fleet Manager, CAP Dashboard | Failed-message tools | Comparable products (API first) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IDeadLetterManager`, `DeadLetterFilter`, `DeadLetterStoreLimits` | `src/Encina.Messaging/DeadLetter/` | Every operation |
| `MapDPIAEndpoints`, `EnsurePolicyRegistered` | `src/Encina.AspNetCore/DPIAEndpointExtensions.cs` | Group shape and fail-closed check |
| `ToProblemDetails` | `src/Encina.AspNetCore/ProblemDetailsExtensions.cs` | Error mapping |
| `DeadLetterMetrics` | `src/Encina.Messaging/Diagnostics/DeadLetterMetrics.cs` | Naming family |
| Shared integration collections | `tests/Encina.IntegrationTests` | One provider per family |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.AspNetCore` | 218-225 inside `AspNetCore` (200-249; 200-207 used, 208-217 reserved by the #3443 and #710 plans) | No new range; the allocation test already maps the assembly |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI and changelog. The `dlq.*` categories are not a pull request of this plan: they belong to #3724 (a prerequisite), so the earlier first pull request that edited `DeadLetterManager.cs`, `DeadLetterOrchestrator.cs` and `ErrorCategoryPending.txt` is removed and no edit of `ErrorCategoryPending.txt` is counted here. No pull request uses a type created by a later one.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| A1 | Group, read routes, DTOs, mapper, shared policy helper | 6 (extension, 4 DTO files, mapper) | 2 (`DPIAEndpointExtensions.cs`, PublicAPI) | 3 (unit, guard, contract) | 1 (`DPIAEndpointExtensionsTests.cs`) | 1 | 0 | 13 | ~900 |
| A2 | Write routes, validator, purge confirmation, cross-tenant mode | 1 (validator) | 1 (extension) | 3 (validator, routes, property) | 1 (the guard test of A1) | 1 | 0 | 7 | ~700 |
| A3 | Metrics, spans, log class and EventIds 218-225 | 2 | 1 (extension) | 2 | 0 | 1 | 0 | 6 | ~450 |
| A4 | Integration tests (4 families), justification files, the integration targets | 0 | 0 | 6 (4 integration, 2 `.md`) | 0 | 1 | 0 | 7 | ~800 |
| A5 | Documentation | 0 | 0 | 0 | 0 | 0 | 4 (README of `Encina.AspNetCore`, operations guide, `docs/INVENTORY.md`, `changelog.d/2229-deadletter-admin-api.added.md`) | 4 | ~300 |
| **Total** | | **9** | **4** | **14** | **2** | **4** | **4** | **37** | **~3,150** |

The columns sum to the total (9 + 4 + 14 + 2 + 4 + 4 = 37) and every statement of the total in this plan is 37 files in five pull requests and about 3,150 lines; the largest is 13 files and none exceeds 1,000 lines. A1 to A4 follow #3724, #3683, #3688 and #2013 (Prerequisites).

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #2229 - an opt-in dead letter admin API in Encina.AspNetCore.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Railway Oriented Programming: Either<EncinaError, T>; responses carry error codes only.
- No provider code: the endpoints call IDeadLetterManager, which sits on the 10 database providers.

IMPLEMENTATION OVERVIEW:
1. MapEncinaDeadLetterEndpoints with required policy and optional all-tenants policy; DTOs without payload;
2. validator, purge confirmation by expected count; 3. metrics, spans, EventIds 218-225; 4. tests with each pull request;
5. docs. Five pull requests (A1 to A5), see the Estimated File Count table.

KEY PATTERNS:
- Fail closed at mapping time; ambient tenant by default; audit stays in the manager (#2013).
- TimeProvider where time is needed; CancellationToken from HttpContext.RequestAborted.

REFERENCE FILES:
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- src/Encina.Messaging/DeadLetter/IDeadLetterManager.cs
- docs/plans/deadletter-admin-api-implementation-plan-2229.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Operator reads must be current; a cached list would hide new dead letters |
| 2 | OpenTelemetry | ✅ Phase 3 | `encina.dlq.api.requests` and `encina.dlq.api.purged` and spans; operation and error code only |
| 3 | Structured Logging | ✅ Phase 3 | EventIds 218-225 in the `AspNetCore` range |
| 4 | Health Checks | ❌ N/A | No new dependency; the dead letter store checks already exist |
| 5 | Validation | ✅ Phase 2 | Filter, paging, batch limits and purge confirmation validated at the boundary |
| 6 | Resilience | ❌ N/A | Local store calls only; replay resilience belongs to the manager |
| 7 | Distributed Locks | ❌ N/A | Concurrent replays are excluded by `TryClaimForReplayAsync` |
| 8 | Transactions | ❌ N/A | Each operation is one manager call |
| 9 | Idempotency | ✅ Phase 1 | A repeated replay dispatches nothing (the claim, #2012); an HTTP retry after a successful replay gets 409 from `Left(dlq.already_replayed)` (Design Choice 7) |
| 10 | Multi-Tenancy | ✅ Phase 2 | Ambient tenant by default; cross-tenant behind its own policy |
| 11 | Module Isolation | ❌ N/A | Dead letters are not module-scoped today |
| 12 | Audit Trail | ✅ Phase 1 | Replay and purge audited by the manager through the port of #2013 |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Persistent dead letter stores (#583) | ✅ Closed | The data |
| Audit of operator actions (#2013) | Open | Needed for the audit acceptance criterion; land it first or in the same series |
| `dlq.*` error categories (#3724, #3419 M2) | Open | Gives the `Left` codes their category (including `ErrorCategoryPending.txt`); without it `GetCategory` is the legacy inference |
| Core HTTP status table (#3683, #3419 S2) | Open | The category-to-status table the host reads |
| `ToProblemDetails` reads the category (#3688, #3419 H1) | Open | Until it lands `ToProblemDetails` answers 500 for `dlq.already_replayed` and `encina.authorization.dlq_tenant_required`, so the 409 and 400 status tests of A1 to A4 would fail; A1 to A4 are sequenced after it |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| What reaches the queue | #1991 (closed) | The API shows what is stored |
| Quarantined rows | #135 | Appear in listings with their `poison.quarantined.*` code; the API applies its own policy (Dilemma 6 of #135) |

---

## Next Steps

1. Link this plan from #2229 and ask the maintainer to answer Design Choices 2, 3, 4, 6 and 7 and the Dilemma rows F3, F4 and F8 when work starts.
2. Implement A1 to A5 as the pull requests of the Estimated File Count table, `Fixes #2229` on A5; sequence after #3724, #3683, #3688 and #2013.
