# Implementation Plan: Dead Letter Queue Operations — an Opt-in Admin API over `IDeadLetterManager`

> **Issue**: [#2229](https://github.com/dlrivada/Encina/issues/2229)
> **Type**: Feature
> **Related**: #583 (persistent dead letter stores, closed), #2013 (replay under the stored context and audited operator actions; this plan consumes its audit), #135 (quarantined rows), #1991 (what reaches the queue), #2203 (saga not found identity), #419 and #445 (alternative surfaces), ADR-046, ADR-054
> **Complexity**: Medium (one package, no provider code)
> **Estimated Scope**: ~700-1,000 lines of production code + ~1,600 lines of tests

---

## Summary

### What exists today (read on 2026-11-11, main `68d08ddae`)

- `IDeadLetterManager` (`src/Encina.Messaging/DeadLetter/IDeadLetterManager.cs`) offers `ReplayAsync`, `ReplayAllAsync(filter, maxMessages = 100)`, `GetMessageAsync`, `GetMessagesAsync(filter, skip, take)`, `GetCountAsync`, `GetStatisticsAsync`, `DeleteAsync`, `DeleteAllAsync(filter)` and `CleanupExpiredAsync`, all returning `Either<EncinaError, T>`. `ReplayResult`, `BatchReplayResult` and `DeadLetterStatistics` are the result records.
- The manager is tenant-aware: it defaults to the ambient tenant, denies `DeadLetterFilter.AllTenants` without an explicit opt-out and returns `encina.authorization.dlq_tenant_required` (`DeadLetterErrorCodes.TenantRequired`); `DeadLetterFilter.All` deletes the whole queue without confirmation at manager level (`DeadLetterFilter.cs:104`). The result of a replay is a code (`ReplayResult.ErrorMessage` holds a code, not exception text, #2012).
- The one endpoint group in `Encina.AspNetCore` is `MapDPIAEndpoints(string authorizationPolicy, string prefix)` (`src/Encina.AspNetCore/DPIAEndpointExtensions.cs:144`): it checks at mapping time that authorization is registered and the policy exists (`EnsurePolicyRegistered` `:523`, throws `InvalidOperationException`), groups the routes, calls `RequireAuthorization` and maps `Left` to RFC 9457 problem details with `ToProblemDetails` (`ProblemDetailsExtensions.cs:67`, status inferred by `MapErrorCodeToStatusCode`).
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
| **A) API only, `MapEncinaDeadLetterEndpoints(authorizationPolicy, prefix)` like `MapDPIAEndpoints`; start-up `InvalidOperationException` when authorization or the policy is missing; no cleanup endpoint** | Fail closed, same shape and tests as the precedent; every UI, Aspire command or CLI needs the API underneath; `CleanupExpiredAsync` stays the retention processor's job | An operator without a front end uses curl or Swagger |
| **B) API plus a bundled UI package** | Closest to ServicePulse and Fleet Manager | A second product to maintain, secure and test; pricing and scope of the competitors unverified |

### Chosen Option: **A — API only, policy required at mapping time**

### Rationale

- The issue comment of 2026-10-17 settled "API now, UI later on top". Cleanup is not exposed: #2013 keeps retention cleanup out of the operator audit, so an endpoint would add an unaudited operator purge.

**Class:** Settled
**Settled by:** the settled-by-elimination comment of 2026-10-17 on #2229, AGENTS.md section 3 (security gates fail closed), precedent `DPIAEndpointExtensions.cs`, comparable products NServiceBus ServicePulse and Rebus Fleet Manager

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
| **A) A `DeadLetterMessageResponse` without `RequestContent`, `ExceptionStackTrace` or the actor: ids, `RequestType`, `ErrorCode`, `ExceptionType`, `SourcePattern`, `SourceMessageId`, `CorrelationId`, `TenantId`, `TotalRetryAttempts`, UTC timestamps, `IsReplayed`, `ReplayResult`** | The payload may hold personal or health data and the stack trace names internals; nothing sensitive leaves through HTTP | An operator cannot read the payload in the API |
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
| **B) `expectedCount` must equal `GetCountAsync` for the same filter, else 409** | The operator deletes exactly what they saw; no accidental whole-queue purge | A count query per purge; clients must do a list first |

### Chosen Option: **B — Expected count confirms the purge** (recommended, pending the maintainer)

### Rationale

- `DeleteAllAsync(DeadLetterFilter.All)` deletes without confirmation at manager level (`DeadLetterFilter.cs:104`), so the HTTP contract carries the only guard.
- A wins when scripts call the API without a previous list and the volume is small.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Validation, error mapping, idempotent replay and audit</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Validate filter and paging at the boundary (`DeadLetterStoreLimits.MaxPageSize` 1000, length limits, `maxMessages` cap); map `Left` through `ToProblemDetails` with explicit statuses for `dlq.already_replayed`, `dlq.replay_in_progress` (409) and `dlq.expired` (410); a repeated replay returns the recorded outcome and dispatches nothing (the claim guarantees it); audit and the tenant restore stay in the manager (#2013)** | One trail, one place for the rules; HTTP retries are safe | Depends on #2013 for the audit acceptance criterion |
| **B) Audit in the HTTP layer** | No dependency on #2013 | Misses replays by code; two trails to reconcile |

### Chosen Option: **A — Boundary validation, codes only, audit stays in the manager**

### Rationale

- Concurrent replays are already excluded by `TryClaimForReplayAsync` (#2012). Responses and logs carry error codes only; telemetry follows ADR-054 (no payload, no actor, tenant tag only with multi-tenancy).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (`EncinaError.Message` rule, gates fail closed), ADR-046, ADR-054 D4, #2012 claim contract, SPEC-002 REQ-019

</details>

---

## Implementation Phases

### Phase 1: Contract, DTOs and Endpoint Group

> **Goal**: The route table and request/response types, with fail-closed mapping.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.AspNetCore/DeadLetterEndpointExtensions.cs`: `MapEncinaDeadLetterEndpoints(this IEndpointRouteBuilder endpoints, string authorizationPolicy, string? allTenantsPolicy = null, string prefix = "/api/dead-letters")` returning `RouteGroupBuilder`; reuse the policy check of `EnsurePolicyRegistered` (extract it to a shared internal helper rather than copy it).
2. **Routes**: `GET /` (filter, `skip`, `take`), `GET /{id:guid}`, `GET /count`, `GET /statistics`, `POST /{id:guid}/replay`, `POST /replay` (filter, `maxMessages`), `DELETE /{id:guid}`, `DELETE /` (filter, `expectedCount`).
3. **Create** `DeadLetter/DeadLetterMessageResponse.cs`, `DeadLetterFilterRequest.cs`, `DeadLetterPageResponse.cs`, `PurgeResponse.cs` (records; Design Choice 3) and the mapper from `IDeadLetterMessage`.
4. **Modify** `ProblemDetailsExtensions.cs` (or a dead letter mapper) for the explicit statuses of Design Choice 5.

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
2. Map every Left through ToProblemDetails with explicit statuses for the dead letter codes.
3. Never put RequestContent, ExceptionStackTrace or EncinaError.Message in a response.

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

1. **Create** `DeadLetter/DeadLetterRequestValidator.cs`: `take` within `DeadLetterStoreLimits.MaxPageSize`, string lengths from `DeadLetterStoreLimits`, `maxMessages` cap, date order; failures are `400` problem details with a code.
2. **Implement** the purge confirmation (Design Choice 4): count first, compare with `expectedCount`, then `DeleteAllAsync`; a mismatch is `409`.
3. **Implement** the cross-tenant mode (Design Choice 2): authorize `allTenantsPolicy` through `IAuthorizationService` before setting `AllTenants`; null policy returns `403`.
4. **Verify** that a request without a tenant cannot read or delete another tenant's rows (the manager returns `dlq_tenant_required`).

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

1. **Create** `Diagnostics/DeadLetterApiMetrics.cs` on a meter named `Encina.AspNetCore` (ADR-054 D3): `encina.dlq.api.requests_total` (tags operation, outcome code; tenant only with multi-tenancy on, ADR-054 D4) and `encina.dlq.api.purged_total`. The issue's `encina.deadletter.api.*` proposals are renamed to the existing `encina.dlq.*` family.
2. **Create** an `ActivitySource` `Encina.AspNetCore` with spans `encina.dlq.api.replay` and `encina.dlq.api.purge`; no tenant or actor attributes beyond the ADR-054 rule.
3. **Create** `Diagnostics/DeadLetterApiLog.cs` with `[LoggerMessage]` EventIds 208-215 inside the registered `AspNetCore` range (200-249); codes only, `ForLogging()` for exceptions. Add the type to the existing `Encina.AspNetCore` entry of `EncinaEventIdAllocationTests` (already mapped).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Encina.AspNetCore has no Meter or ActivitySource yet. EventIds 200-207 are used; the range is (200, 249).

TASK:
1. Add DeadLetterApiMetrics, the ActivitySource and DeadLetterApiLog (EventIds 208-215, packed sequentially).

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
4. **Integration tests** through `WebApplicationFactory` against real stores on the shared collection fixtures, one provider per family (ADO, Dapper, EF Core, MongoDB), including a cross-tenant denial and a purge with a stale `expectedCount`.
5. Load and benchmark: justification files.

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
| `Encina.AspNetCore` | 208-215 inside `AspNetCore` (200-249; 200-207 used) | No new range; the allocation test already maps the assembly |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files | ~9 | Extension, 4 DTO files, validator, metrics, log, mapper |
| Changed production files | ~4 | `DPIAEndpointExtensions.cs` (shared helper), `ProblemDetailsExtensions.cs`, PublicAPI, manifest |
| Tests | ~12 | Unit, guard, contract, property, integration x 4 families, justification files |
| Documentation | ~5 | README, guide, INVENTORY, changelog |
| **Total** | **~30** | Two pull requests: code and tests, then documentation |

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
2. validator, purge confirmation by expected count; 3. metrics, spans, EventIds 208-215; 4. tests; 5. docs.

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
| 2 | OpenTelemetry | ✅ Phase 3 | Request counter and spans; operation and error code only |
| 3 | Structured Logging | ✅ Phase 3 | EventIds 208-215 in the `AspNetCore` range |
| 4 | Health Checks | ❌ N/A | No new dependency; the dead letter store checks already exist |
| 5 | Validation | ✅ Phase 2 | Filter, paging, batch limits and purge confirmation validated at the boundary |
| 6 | Resilience | ❌ N/A | Local store calls only; replay resilience belongs to the manager |
| 7 | Distributed Locks | ❌ N/A | Concurrent replays are excluded by `TryClaimForReplayAsync` |
| 8 | Transactions | ❌ N/A | Each operation is one manager call |
| 9 | Idempotency | ✅ Phase 1 | A repeated replay returns the recorded outcome and dispatches nothing |
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

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| What reaches the queue | #1991 | The API shows what is stored |
| Quarantined rows | #135 | Hidden from default lists unless a quarantine policy applies |

---

## Next Steps

1. Link this plan from #2229 and ask the maintainer to answer Design Choices 2 to 4 when work starts.
2. Implement Phases 1 to 5 as two pull requests, `Fixes #2229` on the last; sequence after #2013.
