# Implementation Plan: `[RequiresLeadership]` Pipeline Behavior (leader-only requests, optional wait, fail-closed gate)

> **Issue**: [#221](https://github.com/dlrivada/Encina/issues/221)
> **Type**: Feature
> **Complexity**: Medium (one attribute, one gate behavior over the election API of #216, 4 pull requests)
> **Estimated Scope**: ~550 lines of production code + ~1,100 lines of tests (about 2,300 lines over 4 pull requests; the per-PR estimates in "Estimated File Count" sum to it)
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, AC-027; [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3, D4, D6.3; [ADR-062](../architecture/adr/062-error-category-contract.md)
> **Depends on**: [#216](https://github.com/dlrivada/Encina/issues/216) (`ILeaderElectionProvider`; open, plan on main, nothing written; the issue itself says "Requires: #216"); [#2667](https://github.com/dlrivada/Encina/issues/2667) (ADR-052 implementation); PR 1 of [#220](https://github.com/dlrivada/Encina/issues/220) (the resource-template resolver this plan reuses); [#2405](https://github.com/dlrivada/Encina/issues/2405) (open bug: the SQL Server lock ignores its expiry) for the SQL Server failover scenario
> **Related**: family `lock-pipeline` (#220, #223, #224, #225); #216, #717 (leader election for the message processors), #724, #741, #2184 (pipeline stages)

---

## Summary

Some commands must run on one instance only: the scheduler tick, the outbox publisher, a singleton worker. This plan adds a **leadership gate** to `Encina.DistributedLock`: a request type marked `[RequiresLeadership("scheduler")]` runs only on the instance that currently leads that election, and every other instance answers with a stable `Left` (or waits for leadership when asked to).

1. **Attribute.** `RequiresLeadershipAttribute(electionName)` with `WaitForLeadership`, `WaitTimeoutSeconds`; the election name may use the same `{Placeholder}` templates as #220 (Design Choice 4).
2. **Gate.** `LeaderElectionPipelineBehavior<TRequest, TResponse>` asks `ILeaderElectionProvider.IsLeaderAsync(name)` (#216); leader means `await nextStep()`, anything else denies. A `Left` from the election check **denies** the request (fail closed); the gate never lets a request through because the check failed.
3. **Wait mode.** With `WaitForLeadership` the gate polls on `TimeProvider` until the instance leads, the wait elapses, or the token is cancelled.
4. **Stage.** The gate is an admission step: a non-leader rejects before any identity, validation or transaction work (Design Choice 2).
5. **Opt-in.** `AddEncinaLeadershipGate()` registers the behavior and fails at startup when no `ILeaderElectionProvider` is registered.

**Standards**: ADR-052; ADR-054; ADR-062; `AGENTS.md` section 3 (fail-closed gates, ROP, errors never swallowed, registration completeness); the [Leader Election pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/leader-election).

**Affected packages**: `Encina.DistributedLock` (everything); `Encina` (EventId registration only when this plan is the first of the family with a log); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`. No provider package changes: election runs on any lock provider through #216.

**Provider category**: Distributed locks (`AGENTS.md` section 5): election is built on the lock providers by #216, so the gate covers all seven (InMemory, Redis, SQL Server now; PostgreSQL #207, MySQL #208, Azure Blob #209, DynamoDB #210 when they land), proven by #216's contract scenarios plus this plan's. Database (10), caching and transports do not apply.

**What the code does today** (`origin/main` at `f9063538e`):

- Nothing of this feature exists: no `ILeaderElectionProvider`, `ILeadershipHandle`, `RequiresLeadershipAttribute` or leader behavior under `src/` (a search of `src/` for `LeaderElection` and `Leadership` returns no hit). The #216 plan on main defines `ILeaderElectionProvider` with `IsLeaderAsync(electionName, ct)` and `HasLeaderAsync` returning `Task<Either<EncinaError, bool>>` and a synchronous `IsHoldingLeadership(name)`, campaigns configured by `LeadershipCampaignOptions.Elections`, and the safe-leadership rule (leader only while the local lease has not expired).
- `IPipelineBehavior<TRequest, TResponse>.Handle(request, IRequestContext, RequestHandlerCallback<TResponse>, CancellationToken)` (`src/Encina/Abstractions/IPipelineBehavior.cs`); stages exist in `src/Encina/Pipeline/Ordering/PipelineStage.cs` (`Admission = 200`: "load shedding ... rejected cheaply") but the vocabulary is "declared, not yet applied by the pipeline builders" (#2184 open).
- Implementers: this plan consumes `ILeaderElectionProvider` (no implementer yet) and changes no interface, so no implementer, fake or `new X(` site is affected; no test class implements `IDistributedLockProvider` or `ILockHandle`.
- `Encina.DistributedLock` has no `InternalsVisibleTo` for `Encina.GuardTests`; `tests/Encina.ContractTests` references no `Encina.DistributedLock*` project and has no Redis or SQL Server collection; `tests/Encina.IntegrationTests` has the shared `ADO-SqlServer` collection and no Redis collection.
- `.github/coverage-manifest/Encina.DistributedLock.json`: package targets guard 20, unit 60; existing entries stay.

---

## Design Choices

<details>
<summary><strong>1. What "leader" means for the gate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Consult only: the gate asks `IsLeaderAsync(resolvedName)`; leadership is a long-lived state established by the campaigns the application configures in #216; a name no campaign covers is "not leader"** | One election engine; matches the issue's `IsLeaderAsync`; the gate never acquires anything | A per-tenant name (`tenant:{TenantId}:scheduler`) is leader only if the application configured that exact election |
| **B) Acquire on demand: the gate calls `TryAcquireLeadershipAsync(name, lease)` when the instance is not yet leader** | Works for any resolved name with no configuration | Without the hosted renewal the "leadership" lapses after one lease, so the gate behaves as a lease lock and duplicates #220; two ways to become leader |
| **C) A, plus a dynamic campaign API in #216 for templated names** | Per-tenant elections without a static list | Changes the surface of #216, which is a different plan |

### Chosen Option: **A - consult only** (recommended, pending the maintainer)

### Rationale

- A wins for the issue's use cases (scheduler, outbox publisher, singleton worker: static names); B wins for ad-hoc per-resource exclusivity, which is what #220 already provides; C wins when per-tenant leadership is a real requirement, and then belongs inside the #216 plan as an additional PR (it is recorded there as a possible extension, not planned here). Under A the gate documents that the election name must be campaigned.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Pipeline stage</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[PipelineStage(PipelineStage.Admission, Order = 100)]`** | A non-leader rejects before authentication, validation, caching and transactions run; nothing is wasted | The rejection is not audited (`Audit` is the next stage in); `WaitForLeadership` holds the request in the outermost stage |
| **B) `[PipelineStage(PipelineStage.Transaction, Order = -200)]`, outside the lock behavior of #220** | Leadership checked after validation and caching | Work done before the gate is wasted on a non-leader |
| **C) No declaration (`Application`)** | Nothing to decide | Inside the transaction stages: a non-leader opens a transaction first |

### Chosen Option: **A - Admission** (recommended, pending the maintainer)

### Rationale

- A wins for rejecting cheaply; B wins when a rejected attempt must be audited or validated first. The declaration is inert until #2184 applies the vocabulary to the builders; until then the README says to register the behavior first.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Outcomes and categories</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `requires_leadership.not_leader` is `Unavailable` (this instance cannot serve the request, retry elsewhere); `requires_leadership.wait_timeout` is `Timeout`; `requires_leadership.election_failed` (a `Left` from the election check, fail closed) is `Internal` (ADR-062 R4); `requires_leadership.invalid_attribute` and `invalid_template` are `Internal` (R1); `requires_leadership.tenant_required` is `InvalidRequest` (R3); the issue's `NotLeaderErrorCode` string is dropped** | Stable codes with declared categories; `Unavailable` tells a client to retry on another instance | A free override string is gone |
| **B) `not_leader` as `Conflict`** (R2) | Same category as lock contention in #220 | `Conflict` means an operation in progress; here nothing is in progress, the instance is simply not eligible |
| **C) Keep `NotLeaderErrorCode` as a free string** | Matches the issue | Defeats the declared code-and-category pairs |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- A wins for monitoring by category and for load balancers that retry on 503; B wins when the application treats "someone else leads" as the same class as "someone else holds the lock"; C wins for an application that maps codes in clients. Not being the leader is a normal outcome: logs and metrics count it as `skipped`, never `failed`. Codes are stable constants and the message never carries the resolved election name.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Template interpolation of the election name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `DistributedLockResourceTemplate` of #220 (same placeholders, same escaping, same context-wins rule and fail-closed denials); the resolver stays internal and its failures map to this plan's codes** | One implementation of the injective escape; one set of tests | PR 1 of this plan waits for #220 PR 1 |
| **B) A second resolver in this plan** | No dependency | Two copies of escaping and fail-closed logic |

### Chosen Option: **A**

### Rationale

- Only A keeps one escaping rule: a second implementation could map two tenants to one election name. The context-wins rule is Dilemma 6 of #220, answered once for both plans. A template naming `{TenantId}` with no context tenant denies with `tenant_required`; no `default` segment is substituted.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (fail-closed gates) and the injective-id rule applied in #220 Design Choice 6 (one resolver, one escape)

</details>

<details>
<summary><strong>5. Waiting for leadership</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Poll `IsLeaderAsync` every `WaitPollInterval` (option, default 500 ms) on `TimeProvider` until leader, `WaitTimeoutSeconds` (default 30) elapses or the token is cancelled; cancellation propagates; a `Left` during the wait denies at once** | Deterministic with a fake clock; no new #216 API; never waits past the budget | Latency up to one poll interval |
| **B) `AcquireLeadershipAsync(name)` of #216 (waits until elected)** | No polling in the gate | It acquires leadership (Design Choice 1 option B) and waits unbounded unless wrapped |

### Chosen Option: **A**

### Rationale

- `RequestHandlerCallback` takes no token, so the only bound the gate controls is its own wait; polling keeps it bounded and testable. The attribute's `WaitTimeoutSeconds` must be positive and the poll interval is validated at start (`> 0`, `<= 10 s`).

**Class:** Trivial
**Settled by:** `src/Encina/Abstractions/RequestHandlerDelegate.cs:9` (the callback has no token) and the injected-`TimeProvider` rule of `AGENTS.md` section 3

</details>

<details>
<summary><strong>6. Registration and fail-closed startup</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaLeadershipGate(Action<LeadershipGateOptions>?)`: options with validation on start, the open-generic transient behavior; `InvalidOperationException` naming `AddEncinaLeadershipCampaign()` when no `ILeaderElectionProvider` is registered; a second call changes nothing** | Startup failure instead of a gate that denies everything; everything the behavior resolves is registered | The call order matters |
| **B) Resolve lazily** | Any order | Every request denied with `election_failed` in production |

### Chosen Option: **A**

### Rationale

- Registration completeness is binding and proven by a DI test with `ValidateOnBuild` and `ValidateScopes`; the behavior is transient and takes singletons only.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (registration completeness) and ADR-050 (registration errors)

</details>

---

## Implementation Phases

### Phase 1: Attribute, Codes and Options (PR 1)

> **Goal**: The declarative surface, testable alone.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Pipeline/RequiresLeadershipAttribute.cs`** (new, `src/Encina.DistributedLock/`) - public sealed, `AttributeUsage(Class, AllowMultiple = false, Inherited = false)`, constructor `(string electionName)`, `WaitForLeadership` (false), `WaitTimeoutSeconds` (30).
2. **`Pipeline/LeadershipGateErrorCodes.cs`** (new, public static) - the codes of Design Choice 3.
3. **`Pipeline/LeadershipGateOptions.cs`** and **`LeadershipGateOptionsValidator.cs`** (new) - `WaitPollInterval` (500 ms, `> 0` and `<= 10 s`); validator is `IValidateOptions<LeadershipGateOptions>`.
4. **Tests** - options and validator (defaults, bounds), guard test of the validator; PublicAPI lines and manifest entries (`mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #221: the RequiresLeadership attribute, error codes and gate options.

CONTEXT:
- Encina.DistributedLock has no InternalsVisibleTo for Encina.GuardTests: guard-test public types only.
- Errors are EncinaErrors.Create(code, ErrorCategory, message); categories are fixed in the plan (ADR-062).

TASK:
Create RequiresLeadershipAttribute, LeadershipGateErrorCodes, LeadershipGateOptions and its validator, with tests.

KEY RULES:
- XML docs on every public member; no [Obsolete]; messages never carry an election name or any request value.

REFERENCE FILES:
- docs/plans/lock-pipeline-behavior-implementation-plan-220.md, docs/plans/leader-election-provider-implementation-plan-216.md
```

</details>

### Phase 2: The Gate Behavior and Registration (PR 2)

> **Goal**: A request runs only on the leader.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Pipeline/LeaderElectionPipelineBehavior.cs`** (new, public sealed; `where TRequest : IRequest<TResponse>`; `[PipelineStage(PipelineStage.Admission, Order = 100)]`; constructor `(ILeaderElectionProvider, IOptions<LeadershipGateOptions>, ILogger<...>, TimeProvider? timeProvider = null)`) - no attribute passes through; static cached plan (attribute and compiled template, as in #220 Design Choice 7); resolve the name with `DistributedLockResourceTemplate`; `IsLeaderAsync`: `Right(true)` runs `nextStep()`; `Right(false)` is `not_leader` (or the wait of Design Choice 5); `Left` is `election_failed` (fail closed, the cause code is logged, its message never).
2. **`ServiceCollectionExtensions.cs`** (modify) - `AddEncinaLeadershipGate(...)` per Design Choice 6.
3. **Tests** against a controllable `ILeaderElectionProvider` fake and the InMemory lock provider through #216 once written: leader runs, follower denied, election `Left` denies, wait succeeds when leadership arrives on a fake clock, wait times out, cancellation propagates, missing tenant for `{TenantId}` denies, request without attribute untouched, DI test with `ValidateOnBuild` and `ValidateScopes` with and without an election provider, guard tests (constructor nulls). README section and changelog fragment `changelog.d/221-leadership-gate.added.md` (specialists).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #221: LeaderElectionPipelineBehavior and AddEncinaLeadershipGate.

CONTEXT:
- #216 provides ILeaderElectionProvider.IsLeaderAsync(electionName, ct) -> Task<Either<EncinaError, bool>>; leadership is true only while the local lease is valid.
- The resolver DistributedLockResourceTemplate comes from #220 PR 1 (internal, same assembly).
- The behavior is transient; it takes singletons only.

TASK:
Create the behavior and the registration with the tests of the Phase 2 tasks.

KEY RULES:
- Fail closed: a Left from the election check denies the request; the handler never runs on any Left.
- Never log the resolved election name, tenant or user values or EncinaError.Message.
- Time through the injected TimeProvider; Task.Delay(poll, timeProvider, ct).

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs, src/Encina/Pipeline/Ordering/PipelineStage.cs
```

</details>

### Phase 3: Observability (PR 3)

> **Goal**: Gate outcomes are visible without leaking names.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/LeadershipGateLog.cs`** (new, `internal static partial`) - 30605 request rejected because the instance is not the leader (Information: request type), 30606 waiting for leadership (Debug), 30607 leadership wait timed out (Warning: request type, waited seconds), 30608 leadership check failed so the request was denied (Warning: error code). Ids 30605-30608 of the family range `DistributedLockPipeline` (30600-30649).
2. **`Diagnostics/LeadershipGateMetrics.cs`** (new, internal) - on the shared `DistributedLockDiagnostics` meter: counter `encina.distributed_lock.leadership_gate.requests` with tags `request.name` and `encina.leadership.outcome` (`leader`, `skipped`, `timeout`, `failed`); no `_total`; tenant tag only with multi-tenancy (#2676); the election name is never a tag.
3. **EventId registration** only if this PR is the first of the family with a log: `EventIdRanges.cs` field `DistributedLockPipeline = (30600, 30649)`, core `PublicAPI.Unshipped.txt`, one registry row with the same bounds, `AssemblyRanges` entry in `EncinaEventIdAllocationTests.cs` (4 more files, counted below as an option; #220 PR 4 owns it by default).
4. **Tests** - `MeterListener` tests (`skipped` is never `failed`; no election name in any tag or log argument), log tests, load and benchmark justification `.md` files.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #221: telemetry of the leadership gate.

CONTEXT:
- Ids 30605-30608 belong to the family range DistributedLockPipeline (30600-30649), registered by #220 PR 4 or by the first family PR with a log.

TASK:
Write LeadershipGateLog and LeadershipGateMetrics, emit them from the behavior and add the tests.

KEY RULES:
- Not being the leader is a normal outcome (skipped), never a failure; log request types and error codes only.

REFERENCE FILES:
- docs/engineering/eventid-block-registry.md, docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md
```

</details>

### Phase 4: Contract and Integration Scenarios (PR 4)

> **Goal**: The gate is proven with real elections.

<details>
<summary><strong>Tasks</strong></summary>

1. **`tests/Encina.ContractTests/DistributedLock/LeadershipGateContractTests.cs`** (new) - over the InMemory lock provider and the election of #216: the follower is denied while the leader runs, and the roles swap after the leader's lease ends. `Encina.ContractTests.csproj` needs the two `ProjectReference` lines to `Encina.DistributedLock` and `Encina.DistributedLock.InMemory` unless #220 PR 5 already added them (modify, 1 file).
2. **Integration** - `LeadershipGateRedisIntegrationTests.cs` (shared Redis collection added by #220 PR 5, or by this PR if first: 2 more files) and `LeadershipGateSqlServerIntegrationTests.cs` (`ADO-SqlServer`): two gates over one backend, exactly one runs the handler; failover after the leader disposes. The SQL Server expiry-based failover waits for #2405.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #221: contract and integration tests of the leadership gate.

CONTEXT:
- Encina.ContractTests has no lock project reference and no Redis or SQL Server fixture; integration tests use shared [Collection] fixtures only.

TASK:
Write the contract test and the two integration classes.

KEY RULES:
- Real implementations only; short real expiries on Redis; FakeTimeProvider only on InMemory.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/SqlServerDistributedLockAcquireIntegrationTests.cs
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| SPEC-000 REQ-027, AC-027 | Seven lock providers; the gate is provider-agnostic |
| ADR-052, ADR-062, ADR-054 | Either outcomes, categories, telemetry |
| Leader Election pattern (Microsoft Azure Architecture Center) | The gate consults, the campaign elects |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `ILeaderElectionProvider` | #216 (plan on main) | `IsLeaderAsync` |
| `DistributedLockResourceTemplate` | #220 PR 1 | Election-name templates |
| `PipelineStageAttribute`, `PipelineStage.Admission` | `src/Encina/Pipeline/Ordering/` | Stage declaration |
| `IDistributedLockProvider` family | `src/Encina.DistributedLock*` | The engine under #216 |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Status | Proven by |
|----------|--------|-----------|
| InMemory | exists | PR 2 unit, PR 4 contract |
| Redis | exists | PR 4 integration |
| SQL Server | exists (expiry ignored until #2405) | PR 4 integration |
| PostgreSQL, MySQL, Azure Blob, DynamoDB | planned (#207 to #210) | #216's contract subclass in each package's PR |

### Event ID Allocation

| Package | Range | Ids |
|---------|-------|-----|
| `Encina.DistributedLock` (`DistributedLockPipeline`, registered by #220 PR 4; first free 50-slot after 30599, no claim on main, in an open plan head or in the registry, checked 2026-11-27) | `30600-30649` | #221 uses 30605-30608 (rejected, waiting, wait timed out, check failed); one registry row with identical bounds; no id below 10000 |

### Estimated File Count

| PR | Content | New | Modified | Tests, manifests, PublicAPI, docs | Total | Lines (est.) |
|----|---------|-----|----------|------------------------------------|-------|--------------|
| 1 | Attribute, codes, options | 4 | 1 (PublicAPI) | 3 (options tests, validator guard test, manifest) | 8 | 450 |
| 2 | Behavior, registration | 1 | 3 (`ServiceCollectionExtensions.cs`, PublicAPI, README) | 5 (behavior tests, DI test, guard test, manifest, fragment) | 9 | 1,000 |
| 3 | Log, metrics (+4 if first of the family) | 2 | 1 (behavior) | 5 (metrics tests, log tests, manifest, 2 justification `.md`) | 8 (12) | 600 |
| 4 | Contract, integration | 3 (contract, 2 integration) | 1 (ContractTests csproj) | 0 | 4 (6 with the Redis fixture) | 550 |

Total about 2,600 lines. Every PR is at most 15 files and 1,500 lines and builds and passes alone (PR 1 needs #220 PR 1 only for the resolver in PR 2).

### Per-file coverage targets (numeric, per flag)

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `LeaderElectionPipelineBehavior.cs` | 95 | 25 | 50 | 40 | Every branch runs against a controllable election fake and a fake clock; guards cover the constructor; contract and integration run real elections |
| `LeadershipGateOptionsValidator.cs`, `LeadershipGateOptions.cs` | 95 | 40 | 0 | 0 | Pure validation |
| `LeadershipGateMetrics.cs` | 85 | 0 | 0 | 0 | Internal type tested with a `MeterListener`; no guard reach |
| `RequiresLeadershipAttribute.cs`, `LeadershipGateErrorCodes.cs`, `LeadershipGateLog.cs` | 0 | 0 | 0 | 0 | Data holder, constants, source-generated log methods: no executable logic; covered through the behavior |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is pre-1.0 (.NET 10, C# 14, nullable enabled). Rules are in AGENTS.md. Issue #221 adds [RequiresLeadership] and a gate behavior
to Encina.DistributedLock on top of ILeaderElectionProvider (#216).

IMPLEMENTATION OVERVIEW:
1. Phase 1: attribute, error codes with ADR-062 categories, options.
2. Phase 2: LeaderElectionPipelineBehavior and AddEncinaLeadershipGate (startup failure without an election provider).
3. Phase 3: log 30605-30608 and metrics in the family range 30600-30649.
4. Phase 4: contract and integration tests.

KEY PATTERNS:
- Consult-only gate; a Left from the election check denies; not-leader is a normal outcome.
- Reuse DistributedLockResourceTemplate from #220; never log election names or EncinaError.Message.
- Dilemmas 1, 2 and 3 are pending the maintainer: implement the recommended options only after he answers.

REFERENCE FILES:
- docs/plans/lock-leadership-gate-implementation-plan-221.md, docs/plans/leader-election-provider-implementation-plan-216.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Leadership must be read in real time; the gate sits before the caching stage |
| 2 | OpenTelemetry | ✅ Include | Phase 3: outcome counter; tenant tag only with multi-tenancy (#2676); no election names |
| 3 | Structured Logging | ✅ Include | Phase 3: `LeadershipGateLog` 30605-30608, request types and error codes only |
| 4 | Health Checks | ❌ N/A | Election liveness is #216's `LeadershipCampaignHealthCheck` |
| 5 | Validation | ✅ Include | Attribute values and the resolved name are validated; failures are `Left` with their ADR-062 category |
| 6 | Resilience | ❌ N/A | The issue's "graceful degradation" would let requests through when the check fails; that is the opposite of a fail-closed gate, so the gate denies |
| 7 | Distributed Locks | ✅ Include | The gate sits on the election built over the lock providers |
| 8 | Transactions | ❌ N/A | A gate, not data mutation; it runs before the transaction stage |
| 9 | Idempotency | ❌ N/A | A stateless check |
| 10 | Multi-Tenancy | ✅ Include | `{TenantId}` in a name resolves from the context and denies when missing; no `default` segment |
| 11 | Module Isolation | ❌ N/A | Modules are separated with election names and `KeyPrefix` |
| 12 | Audit Trail | ❌ N/A | Covered by #216; a rejected request carries no regulated data |

---

## Prerequisites & Dependencies

| Item | State | Needed for |
|------|-------|------------|
| #216 `ILeaderElectionProvider` | open, plan on main | PR 2 onward |
| #2667 ADR-052 implementation | open | #216 and all PRs |
| #220 PR 1 (resolver) | plan on main | PR 2 |
| Family EventId range | registered by #220 PR 4 or the first family PR with a log | PR 3 |
| #2405 SQL Server expiry | open bug | SQL Server failover scenario of PR 4 |
| #2184 stages applied by the builders | open | The stage declaration becomes effective |

---

## Next Steps

1. After #216 PR 2 merges open PR 1 and PR 2 (PR 2 also needs #220 PR 1); PR 3 after PR 2; PR 4 after PR 2.
2. The maintainer answers Dilemmas 1, 2 and 3 when work on the issue starts; PRs are written for the recommended answers.
