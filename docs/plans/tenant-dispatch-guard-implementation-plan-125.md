# Implementation Plan: Tenant dispatch guard for the messaging patterns (multi-tenancy messaging support, remaining work)

> **Issue**: [#125](https://github.com/dlrivada/Encina/issues/125)
> **Type**: Feature (child of the EPIC #876; the tenant columns and the persisted context of its text are owned by other open issues)
> **Complexity**: Medium (3 phases of code, 1 testing and documentation phase, 5 worker pull requests)
> **Estimated Scope**: ~11 production files, ~21 test files, ~10 metadata, justification and documentation files
> **Related plans** (cited by issue number): #1257 (shared tenant-scope primitive, tenant-id rules and the two-tenant isolation suite), #346 (the tenant gate; owner of `[TenantAgnostic]`), #738 (saga tenant, merged plan), #737, #739, #760 (tenant columns of outbox, scheduled and inbox messages), #1164 (persisted request context restored on dispatch), #3437 and #3452 (the Inbox key shape and tenant-scoped idempotency), #740 (tenant-scoped rate limiting), #798 (audit and ABAC tenant scoping)

---

## Summary

#125 asks for first-class multi-tenancy in all messaging patterns: a tenant on every message, isolation modes, per-tenant configuration, audit logging and context propagation. Its pieces are owned by other issues, and this plan says which, so nothing is built twice; what it plans itself is the one thing no other issue covers: **a guard that refuses to dispatch a message under the wrong tenant, or under no tenant in a multi-tenant application**, for the outbox, scheduled messages, delayed retries, the inbox and sagas.

**State of the code on `010d6f995` (current `origin/main`; the cited files were read on it).**

| Item of #125 | State | Evidence or owner |
|--------------|-------|-------------------|
| `TenantId` on `OutboxMessage`, `InboxMessage`, `SagaState`, `ScheduledMessage` | Open: `IOutboxMessage`, `IInboxMessage`, `ISagaState` and `IScheduledMessage` have no tenant member (grep on `origin/main`); the inbox attempt already carries the metadata tenant (`metadata?.TenantId` in `InboxOrchestrator`) but it is not persisted on the message | #737, #738, #739, #760 |
| Context propagation of `TenantId` to background dispatch | Open, settled | #1164 (`RequestContextScopeFactory.RunRestoredAsync` and the envelope) |
| Tenant-scoped idempotency key of the Inbox | Open bugs and an EPIC | #3437, #3452 (the maintainer comment on #125 of 2026-11-09 points there) |
| Tenant-filtered stores and tenant-prioritized processing | Open | #737 (items 4 and 5) |
| Per-tenant rate limits | Open | #740 |
| Tenant-aware dead letters | Done: `DeadLetterFilter.TenantId`, `DeadLetterManager` denies when multi-tenancy is on and no tenant is ambient | `src/Encina.Messaging/DeadLetter/DeadLetterManager.cs` |
| `TenancyMessagingOptions` | Exists; its switches are removed by #3190 | `src/Encina.Messaging/Tenancy/TenancyMessagingOptions.cs` |
| Isolation modes (`PhysicalQueues`, `PhysicalDatabase`, `Hybrid`) | Not built: the message stores do not use `ITenantConnectionFactory` | Design Choice 1 |
| `ITenantAwareMessage`, `ITenantResolver` for messaging | Not built | Design Choice 4 |
| Per-tenant configuration, `ITenantAuditLogger` | Not built here | Design Choices 5 and 6 |
| A guard that dispatch runs under the message's own tenant | **Missing: this plan** | Phases 1 and 2 |

**What is missing, by reading the code.** Once #737 to #739, #760 and #1164 land, a row carries a tenant and the processor rebuilds a context from the persisted envelope. Nothing then compares the two. A row with no tenant in a multi-tenant host (a legacy row, a writer that ran with no context) dispatches its handlers with no tenant; a row whose tenant column and envelope disagree dispatches under the envelope's tenant. `AGENTS.md` section 3 says a gate with missing context denies and that a `Left` in background infrastructure fails the operation. The tenant gate of #346 covers requests; notifications and replayed messages run no pipeline behaviors, so this guard is their gate. For the Inbox it is also defense in depth for #3437: a stored row found by message id whose tenant differs from the current tenant must not be answered from cache.

**Scope.** `ITenantDispatchGuard` and its implementation in `Encina.Messaging`, wiring in the outbox processor base, the scheduled-message processor, the delayed-retry processor, the inbox orchestrator and the saga orchestrator, one counter, three log messages in a new range, documentation. **Out of scope**: the tenant columns and schemas (#737, #738, #739, #760), the persisted context (#1164), the Inbox key shape (#3437, #3452), rate limits (#740), the audit stores (#798).

**Affected packages**: `Encina.Messaging` (all code), `Encina` (EventId range registration). **Provider category**: Database (10 providers) only through the store interfaces; no provider-specific code is written here (the guard sits above the stores). Transports are not touched: a transport handler runs inside the restored context the guard has already checked.

---

## Design Choices

<details>
<summary><strong>1. Isolation modes: logical only, or physical queues and databases as well</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Logical isolation only: one set of message tables, a tenant column and the persisted context; the guard and the filters keep tenants apart** | Provider-agnostic and the same on the 10 databases and the transports; matches what #737 to #760 build | A tenant that must not share tables or a broker with another cannot get it |
| **B) `PhysicalDatabase`: the message stores resolve their connection per tenant through `ITenantConnectionFactory`** | Real database separation for message tables | The background processors sweep every tenant's rows today in one query; per-tenant databases need a processor per tenant or a tenant loop, a redesign of every processor and of the 10 stores |
| **C) `PhysicalQueues`: per-tenant destinations on each transport** | Broker-level separation | Every one of the 10 transports (and the 6 planned) needs a destination-resolution hook (`AGENTS.md` section 5, transports) |

### Chosen Option: **A — Logical isolation only** (recommended, pending the maintainer)

### Rationale

- A wins for the shared-infrastructure SaaS this release targets, where separation is a data and context guarantee; B or C win for a regulated tenant that contractually needs its own database or broker, a deployment the maintainer would scope as its own feature. Nothing in A blocks B or C later: the guard checks the tenant of a row, wherever the row lives.

</details>

<details>
<summary><strong>2. How the guard decides, and which messages it exempts</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITenantScopeResolver.ResolveRead(rowTenantId)` of #1257 (PR 1257-A1) inside the restored context: the ambient tenant wins, a different row tenant is `tenant_mismatch` (`Forbidden`), no tenant in a multi-tenant host is `tenant_required` (`InvalidRequest`); a notification type marked `[TenantAgnostic]` (the attribute of #346, in core `Encina`) is exempt and logged once per type** | One rule shared with every other consumer; the error codes and categories are #1257's (no new code here); the exemption is explicit and logged | Hard prerequisite on #1257-A1, whose Design Choices 1, 2 and 8 are open |
| **B) The guard compares the two strings itself and reads `MultiTenancyMarker` (the way `DeadLetterManager` does)** | Independent of #1257 | A fourth copy of the rule; its own error codes |

### Chosen Option: **A — The shared primitive plus the attribute exemption** (recommended, pending the maintainer)

### Rationale

- A wins when #1257-A1 merges first (the same reasoning as #346 Design Choice 4); B wins if the maintainer answers #1257 Design Choice 1 with "keep the rule inside each package". **If the answer is B**, the guard is self-contained and PR 125-A no longer needs #1257-A1: `TenantDispatchGuard.cs` takes `IRequestContextAccessor` and `MultiTenancyMarker` instead of `ITenantScopeResolver` and the guard compares the two tenant strings itself; one more file is added to PR 125-A (a `TenantDispatchGuardErrors.cs` with its own two error codes and their ADR-062 categories, `InvalidRequest` and `Forbidden`), which makes it 15 files; the dependency on #346-A (the `[TenantAgnostic]` exemption) stays. System-written messages (an operator or scheduled platform job with no tenant) are the reason for the exemption: they carry `[TenantAgnostic]` on the notification type; without it a tenantless row in a multi-tenant host is denied.

</details>

<details>
<summary><strong>3. What happens to a message the guard denies</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The denial is a `Left` from the dispatch step, so the existing failure path of each processor applies: the message is marked failed, retried up to the configured limit and then dead-lettered (outbox, inbox, scheduled, delayed retry) with its tenant** | No change to any store or processor contract; the dead-letter integration (#2202) already captures the terminal failure; the denial is never reported as success | A permanent denial is retried a few times before it reaches the dead letter queue |
| **B) Mark the message exhausted immediately** | No wasted retries | Only `IOutboxStore` has `MarkAsExhaustedAsync`; `IInboxStore` and `IScheduledMessageStore` have no equivalent, so B needs new store methods on 10 providers |

### Chosen Option: **A — The existing failure path** (recommended, pending the maintainer)

### Rationale

- A wins while the retry limit is small and a denial is rare (a migration artifact, a bug); B wins if denials become frequent enough that the retries cost more than a store contract change. The retry cost of one permanently denied message under A, per pattern on `origin/main`: outbox `OutboxOptions.MaxRetries` default 10 (and `MarkAsExhaustedAsync` exists, so B is cheap there); inbox `InboxOptions.MaxRetries` default 3; scheduled messages `SchedulingOptions.MaxRetries` default 3; delayed retry one more attempt per entry of the configured delayed-retry schedule; saga, the step failure path of the orchestrator. The dead-lettered row keeps its `TenantId`, so the tenant-scoped dead letter reads of `DeadLetterManager` show it to the right tenant only.

</details>

<details>
<summary><strong>4. Where a message's tenant comes from: the persisted context, or a tenant inside the payload</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The persisted context only (#1164) for internal messages; for external inbound messages (webhooks, queue consumers) the host passes the tenant it trusts (`RunRestoredAsync(..., configuredTenantId)`, which exists in `Encina/Identity`)** | The tenant is never chosen by the sender; one source for every pattern | The host decides how it trusts a tenant for each inbound source |
| **B) `ITenantAwareMessage { string TenantId }` on the message type, read by the pipeline (issue text)** | The message states its tenant | The tenant is whatever the payload says: for an external message that is sender-controlled, so a sender could choose the tenant; for an internal one it duplicates the persisted context |

### Chosen Option: **A — The persisted or host-configured context** (recommended, pending the maintainer)

### Rationale

- A wins whenever the payload is not authenticated by the same trust path as the tenant, which is true of webhooks and queue consumers; B wins for a message type that is signed end to end and whose tenant is part of what the signature covers, a case a host can already express by verifying the signature and then passing the tenant to `RunRestoredAsync`. `ITenantResolver` of the issue is not created: resolution is the entry points' job (#346 Design Choice 1).

</details>

<details>
<summary><strong>5. Per-tenant messaging configuration (processing interval, backlog limit, rate)</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not built in this plan: rates are #740, fairness and tenant-prioritized processing are #737 (item 5), and a settings class is added by the plan that has a consumer, resolved through the `ITenantConfigurationResolver<TConfig>` of #1257 (PR 1257-B1/B2, fails closed on a missing configuration)** | No settings nobody reads; one resolution mechanism | The issue's `ConfigureTenant("premium-tenant", ...)` shape waits for its consumers |
| **B) A `TenantMessagingSettings` class now (`ProcessingInterval`, `MaxPendingMessages`) resolved per tenant by the processors** | The issue's example works | The processors sweep all tenants in one query, so a per-tenant interval needs a tenant loop; `MaxPendingMessages` needs a tenant-filtered count on 11 outbox implementers (10 stores, the instrumented decorator and the fake) |

### Chosen Option: **A — Not here** (recommended, pending the maintainer)

### Rationale

- A wins until a consumer exists; B wins if the maintainer wants the backlog cap as its own feature, which should then be a sub-issue with the 10-provider store change counted (`IOutboxStore.GetPendingCountAsync(int maxRetries, ct)` has no tenant parameter today).

</details>

<details>
<summary><strong>6. Per-message tenant audit logging</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No `ITenantAuditLogger`: each row is already a per-tenant record (`TenantId`, created and processed timestamps), a failed message is dead-lettered with its tenant, and operator actions are audited by the audit stores (#798, #752)** | No second log of every message; no per-message write on the hot path | An auditor reads rows, not an audit stream |
| **B) `ITenantAuditLogger.LogMessagePublishedAsync` and `LogMessageProcessedAsync` as in the issue** | Explicit audit events | Doubles every outbox and inbox write; high volume; a new dependency of `Encina.Messaging` on the audit stores |

### Chosen Option: **A — The rows are the record** (recommended, pending the maintainer)

### Rationale

- A wins for a log of state changes by an operator, which is what GDPR accountability asks and what the audit stores already hold; B wins if a regulator requires an append-only stream of every message of a tenant independent of the message tables, a requirement the maintainer would state.

</details>

<details>
<summary><strong>7. How the inbox and saga orchestrators receive the guard</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

The outbox base resolves the guard from its cycle scope, but `InboxOrchestrator` and `SagaOrchestrator` are public classes registered with `AddScoped<...>()` and built by constructor injection; their public constructors are `(store, options, logger, factory, serializer, TimeProvider? = null, DeadLetterSourceCapture? = null)` (`InboxOrchestrator.cs:61`, `SagaOrchestrator.cs:56`) and neither has an `IServiceProvider`. Any way to give them the guard changes a public constructor.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A required parameter `ITenantDispatchGuard guard` on both constructors, placed after `messageSerializer` and before the optional `timeProvider` (both constructors end with optional parameters, `TimeProvider? timeProvider = null, DeadLetterSourceCapture? deadLetterCapture = null`, and C# forbids a required parameter after them, CS1737), checked with `ArgumentNullException.ThrowIfNull`; `ITenantDispatchGuard` becomes public (a public constructor cannot take an internal type); the implementation stays internal** | Fail closed: an orchestrator cannot exist unguarded; the dependency is visible | Edits every `new InboxOrchestrator(` (14 matches in 4 test files, none in `src/`) and `new SagaOrchestrator(` (20 matches in 9 test files, none in `src/`; two more matches are in the #696 plan) and each public constructor's `PublicAPI.Unshipped.txt` line; new null-guard tests (#3798) |
| **B) An optional last parameter `ITenantDispatchGuard? guard = null`** | No call-site edit; fewest files | Fail open: an orchestrator built without the guard (a hand-built instance, a registration that omits it) runs unguarded, which `AGENTS.md` section 3 forbids for a gate |
| **C) Inject `IServiceProvider` and resolve the guard per call** | One mechanism like the processors | Service locator; hides the dependency; still a constructor change and still edits every call site |

### Chosen Option: **A — A required, public-interface parameter** (recommended, pending the maintainer)

### Rationale

- A wins because the guard is a gate and a gate that can be missing fails open; B wins only if the maintainer accepts that a hand-built orchestrator runs unguarded and wants the smallest diff. The cost of A is counted: PR 125-A makes the interface public (one more `PublicAPI.Unshipped.txt` file), PR 125-C1 and 125-C2 edit one orchestrator and its call sites each (12 test files in total, two of them shared) and replace the one `PublicAPI.Unshipped.txt` line of each constructor.

</details>

---

## Implementation Phases

### Phase 1: Core Guard (PR 125-A)

> **Goal**: the guard, registered, with no caller yet.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Messaging/Tenancy/ITenantDispatchGuard.cs`** (`public interface`, because the orchestrator constructors of Design Choice 7 take it; its implementation stays internal): `Either<EncinaError, Unit> Check(string? rowTenantId, Type messageType, string pattern)` where `pattern` is `outbox`, `inbox`, `scheduling`, `retry` or `saga` (the counter tag and the log argument).
2. **Create `src/Encina.Messaging/Tenancy/TenantDispatchGuard.cs`** (`internal sealed`): constructor `(ITenantScopeResolver scope, ILogger<TenantDispatchGuard> logger)`; `Check`: a `messageType` carrying `[TenantAgnostic]` returns `Right` (logged once per type, EventId 7882); otherwise `scope.ResolveRead(string.IsNullOrWhiteSpace(rowTenantId) ? null : rowTenantId)`: a `Left` is returned unchanged (`encina.tenancy.tenant_required` or `encina.tenancy.tenant_mismatch`, categories `InvalidRequest` and `Forbidden`), counted and logged by code and pattern (EventIds 7880 mismatch, 7881 missing). The attribute lookup uses a static `ConcurrentDictionary<Type, bool>`.
3. **Create `src/Encina.Messaging/Diagnostics/TenantDispatchGuardMetrics.cs`** (`internal static`): counter `encina.messaging.tenant_guard.denied` on the shared `Encina` meter (ADR-054 naming), tags `pattern` and `reason` (`mismatch`, `missing`); no tenant tag (ADR-054 D4; the helper is #2676).
4. **Create `src/Encina.Messaging/Diagnostics/TenantDispatchGuardLog.cs`** (`[LoggerMessage]`): EventIds 7880, 7881, 7882 of the new range of Phase 3; messages carry the pattern, the message type name and the error code, never a tenant value or `EncinaError.Message`.
5. **Modify `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`** (`AddMessagingServicesCore`): `services.TryAddSingleton<ITenantDispatchGuard, TenantDispatchGuard>()`. The resolver is registered by `AddEncina` (#1257), so the dependency resolves in any registration order; a DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` and `AddEncina` before and after the messaging registration.
6. `PublicAPI.Unshipped.txt` of `Encina.Messaging`: the interface and its `Check` member (the implementation is internal and has no line); the new range field is a PublicAPI line of `Encina` (Phase 3). Both through mechanical-fixer.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #125, plan tenant-dispatch-guard-implementation-plan-125.md. Design Choices 2, 3 and 4 define the guard. PR 1257-A1 (ITenantScopeResolver) and PR 346-A (TenantAgnosticAttribute in core Encina) are merged.
- Encina.Tenancy references Encina.Messaging, so the guard cannot reference anything in Encina.Tenancy; it uses only core types.

TASK:
Create the public ITenantDispatchGuard, the internal TenantDispatchGuard, the counter and the log class, and register the guard with TryAddSingleton.

KEY RULES:
- Fail closed: a missing tenant in a multi-tenant host or a tenant that differs from the restored context returns Left; never Right.
- ROP: Either<EncinaError, Unit>, no exceptions; logs and the counter carry the pattern, the message type name and the error code, never a tenant value or EncinaError.Message.
- No scoped service in the singleton guard; static per-type attribute cache.
- XML docs; zero warnings; CRAP <= 10 per method.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs (AmbientTenantId, the fail-closed tenant reads)
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

### Phase 2: Wiring the Messaging Patterns (PRs 125-B, 125-C1 and 125-C2)

> **Goal**: every dispatch path calls the guard inside the restored context.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 125-B (outbox, scheduled messages, delayed retries)**: `OutboxProcessorBase` resolves `ITenantDispatchGuard` from the cycle's scope (`scopedServices`, the place it already resolves the store and `IEncina`, so none of the 8 provider `OutboxProcessor` subclasses changes) and calls `Check(message.TenantId, notificationType, "outbox")` after the context is restored and before `Publish`; `ScheduledMessageProcessor` (through its dispatcher call) with `"scheduling"`; `DelayedRetryProcessor.DispatchRequestAsync` with `"retry"`. A `Left` is returned as the dispatch result, so each processor's existing failure path applies (Design Choice 3).
2. **PR 125-C1 (inbox) and PR 125-C2 (sagas)**: `InboxOrchestrator` takes the guard as a required constructor parameter placed before `timeProvider` (Design Choice 7 A; the optional `timeProvider` and `deadLetterCapture` stay last) and calls it with `"inbox"` when it finds a stored message by id (the cached-response path): a stored row whose tenant differs from the current tenant is a `Left`, never a cached response (defense in depth for #3437); `SagaOrchestrator` takes it the same way and calls it with `"saga"` when it loads a saga state for a step (the tenant column is #738's). Each PR edits its constructor, the one `PublicAPI.Unshipped.txt` line of that constructor (the line now lists `Encina.Messaging.ITenantDispatchGuard! guard` between `messageSerializer` and `System.TimeProvider? timeProvider = null`; `Encina.Messaging.Inbox.InboxOrchestrator.InboxOrchestrator(` line 90 and `Encina.Messaging.Sagas.SagaOrchestrator.SagaOrchestrator(` line 869 of the file), a null-guard test of the new parameter (#3798) and every `new` call site on `origin/main` (all 14 and 20 sites are edited anyway; a site that passes `timeProvider` or `deadLetterCapture` positionally or by name must keep them correct after the insertion, so the worker checks each site, and the file counts do not change): `new InboxOrchestrator(` 14 matches in `InboxGuardTests.cs` (6), `ErrorMessageLeakTests.cs` (1), `InboxOrchestratorTests.cs` (6), `InboxPipelineBehaviorTests.cs` (1); `new SagaOrchestrator(` 20 matches in `SagaOrchestratorGuardTests.cs` (6), `SagaRunnerGuardTests.cs` (1), `SagaRunnerOutcomeScenarios.cs` (1, integration), `DeadLetterRuntimeCaptureTests.cs` (1), `DeadLetterSourcesCaptureTests.cs` (1), `ErrorMessageLeakTests.cs` (2), `StorePayloadEncryptionTests.cs` (1), `SagaOrchestratorTests.cs` (6), `SagaRunnerTests.cs` (1). `src/` has no `new` site (DI builds both through `AddScoped`), and the DI test of PR 125-A proves the guard resolves for them.
3. The calls need `TenantId` on `IOutboxMessage`, `IScheduledMessage`, `IInboxMessage` and `ISagaState`: they are hard prerequisites (#737, #739, #760, #738), and the restored context is #1164's. The worker confirms each member and call site by grep on `origin/main` before starting; a pattern whose prerequisite has not landed is left to a later pull request.
4. Tests for each wiring use fake stores (`Encina.Testing.Fakes`) and the scoped provider; the database-level proof per provider is the two-tenant suite of #1257 (the messaging stores are descriptors of that suite).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- PR 125-A merged: the guard exists and is registered. #1164 and the tenant columns are merged for the pattern being wired. Each processor already rebuilds the context through RunRestoredAsync and handles a Left from dispatch by its failure path.

TASK:
Call ITenantDispatchGuard.Check after the context is restored and before the dispatch, in the processor or orchestrator named by the pull request; return a Left as the dispatch result.

KEY RULES:
- Never report success for a denied message; never swallow the Left (AGENTS.md section 3).
- Resolve the guard from the cycle's scope (processors) or take it as a required constructor parameter placed before the optional `timeProvider` (the two orchestrators, Design Choice 7), never a scoped service into a singleton; the 8 OutboxProcessor subclasses stay untouched; the two orchestrator constructors change, with their PublicAPI line, null-guard test and every call site.
- A message type marked [TenantAgnostic] is exempt (the guard handles it); a message with a tenant must never be dispatched under another.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
```

</details>

### Phase 3: Observability (PR 125-A)

> **Goal**: the new range, registered first.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina/Diagnostics/EventIdRanges.cs`**: `public static readonly (int Min, int Max) MessagingTenancy = (7880, 7899);` with an XML comment naming this plan (7880-7882 used; 7883-7899 spare). The range comes from the block assigned to the family `multitenancy-1` (7850-7899; 7850-7879 is `TenancyPipeline`, registered by #346).
2. **Modify `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`**: add `MessagingTenancy` to the `Encina.Messaging` entry of `AssemblyRanges`.
3. EventIds 7880 (tenant mismatch, Warning), 7881 (tenant missing, Warning), 7882 (type exempt through `[TenantAgnostic]`, Information, once per type). Packed from 7880, no gaps.
4. The counter of Phase 1 task 3; no span (the check is a comparison).
5. `PublicAPI.Unshipped.txt` of `Encina` for the new range field and the `EventIdRanges.cs` entry in `.github/coverage-manifest/Encina.json` (a `src/Encina` file touched needs its per-file target there) (mechanical-fixer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- ADR-021: a range is registered in EventIdRanges.cs before any [LoggerMessage] uses it; the assembly is mapped in EncinaEventIdAllocationTests. The block 7850-7899 is assigned to this family; 7880-7899 is MessagingTenancy.

TASK:
Register the range, map Encina.Messaging to it, write TenantDispatchGuardLog with EventIds 7880-7882 and run the architecture tests.

KEY RULES:
- Pack EventIds sequentially from 7880; no id outside the range; XML docs name the range.
- No tenant value and no EncinaError.Message in a log message.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Messaging/Diagnostics/OutboxStoreLog.cs
```

</details>

### Phase 4: Testing, Documentation & Finalization (tests in each PR; documentation in PR 125-D)

> **Goal**: every flag at its target; the guide says what the code does.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** `tests/Encina.UnitTests/Messaging/Tenancy/TenantDispatchGuardTests.cs`: single-tenant host with no tenant passes; multi-tenant host with the restored tenant and a matching row passes; a different row tenant returns `encina.tenancy.tenant_mismatch` and counts `reason=mismatch`; no row tenant in a multi-tenant host returns `encina.tenancy.tenant_required`; a `[TenantAgnostic]` message type passes with no tenant and is logged once; blank and whitespace row tenants count as none; the log is emitted once per denial. Wiring tests per processor: a denied message is not published, is marked failed, and the denial is not reported as processed.
2. **Guard tests**: `Encina.Messaging` grants `InternalsVisibleTo` to `Encina.UnitTests` and not to `Encina.GuardTests` (the project file lists `Encina.UnitTests`, `IntegrationTests`, `ContractTests`, `PropertyTests` and the provider packages), and `TenantDispatchGuard` is `internal` with a constructor of two non-null arguments: the guard target of that file is 0 with that reason, and its argument checks are unit-tested. The two orchestrator constructors are public and gain a validated parameter, so their guard tests are required: a null `guard` throws `ArgumentNullException` in `tests/Encina.GuardTests/Messaging/Inbox/InboxGuardTests.cs` (PR 125-C1) and `tests/Encina.GuardTests/Messaging/Sagas/SagaOrchestratorGuardTests.cs` (PR 125-C2).
3. **Property test** (FsCheck, `tests/Encina.PropertyTests/`): for any two tenant ids, the guard passes exactly when they are equal or the type is exempt.
4. **Contract**: justification in the unit test class comment: the guard has one implementation (the interface is public only to type the orchestrator parameter); the contract of each processor's failure path is the existing processor contract tests.
5. **Integration**: justification in the unit test class comment: the guard is store-agnostic and runs above the stores (a non-database feature); the per-provider two-tenant proof is the isolation suite of #1257, which gets a messaging descriptor in the pull request that adds each message tenant column.
6. **Load and benchmark**: justification `.md` files (`tests/Encina.LoadTests/Messaging/TenantDispatchGuard.md`, `tests/Encina.BenchmarkTests/Messaging/TenantDispatchGuard.md`): one resolver call and a cached attribute lookup per message; no shared mutable state beyond a read-mostly cache.
7. **Documentation (PR 125-D, docs-writer)**: `docs/features/multi-tenancy.md` gains "Messaging and tenants" (what is persisted and by which issue, the guard, `[TenantAgnostic]` notifications, why isolation is logical, what a denied message looks like in the dead letter queue); `src/Encina.Messaging/README.md`.
8. **Changelog fragments** `changelog.d/125-tenant-dispatch-guard.added.md` (PR 125-B, the first pull request with an observable change) and, per wiring pull request, none more (one fragment per issue); manifests and PublicAPI through mechanical-fixer.

**Per-file targets** (proposed; measured values replace them and never go down; "none" means the file has no per-file entry in `Encina.Messaging.json`, whose package targets are contract 15, guard 20, property 15, unit 70, integration 65; the integration flag of the touched processors and orchestrators keeps its existing per-file target and is exercised by the existing provider integration tests):

| File | Baseline | Flags | Justification |
|------|----------|-------|---------------|
| `Tenancy/TenantDispatchGuard.cs` (new) | new | unit 100, property 85, guard 0 | every branch is a unit case and the equality rule is a property; guard 0 because the type is internal without `InternalsVisibleTo` for `Encina.GuardTests`. |
| `Tenancy/ITenantDispatchGuard.cs` (new) | new | unit 0 | an interface with no executable line. |
| `Diagnostics/TenantDispatchGuardMetrics.cs` (new) | new | unit 100 | counter read through an in-memory meter listener. |
| `Diagnostics/TenantDispatchGuardLog.cs` (new) | new | unit 90 | generated logging methods exercised with a fake logger. |
| `MessagingServiceCollectionExtensions.cs` | none | unit 90 | one registration, proven by the DI test. |
| `Outbox/OutboxProcessorBase.cs` | unit 85, guard 10, integration 70 | unchanged, new lines covered | the denied branch has a unit case with a fake store. |
| `Scheduling/ScheduledMessageProcessor.cs`, `Recoverability/DelayedRetryProcessor.cs`, `Inbox/InboxOrchestrator.cs`, `Sagas/SagaOrchestrator.cs` | per the manifest on `origin/main` | unchanged, new lines covered | one guarded branch each, with a unit case; the two orchestrators also gain a guard case in `InboxGuardTests.cs` and `SagaOrchestratorGuardTests.cs`. |
| `Encina/Diagnostics/EventIdRanges.cs` | none | unit 0 | one static field covered by the architecture tests that scan it; a 0 because the field has no executable line. |

CRAP gate: each wiring adds one branch to a method; the worker measures the touched methods and extracts the guard call into a small private method where a processor method is already near the limit.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Code phases are written. Per-flag coverage is measured separately (AGENTS.md section 9); tests execute real code.

TASK:
Write the tests; delegate manifests, PublicAPI lines and the fragment to mechanical-fixer and the pages to docs-writer; measure per-flag coverage of every touched file.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; no Thread.Sleep; no reflection-only tests.
- Justification .md only where a test type is legitimately skipped (load, benchmark); never for unit tests.
- Never edit CHANGELOG.md; documentation never types coverage figures.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/
- .github/coverage-manifest/Encina.Messaging.json
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Use in this feature |
|--------|-------------|---------------------|
| `AGENTS.md` section 3 | gates fail closed; a `Left` in background infrastructure fails the operation | Design Choices 2 and 3 |
| SPEC-002 REQ-015, REQ-061, INV-008, AC-043 | persisted context; tenant-aware stores; no cross-tenant action | the guard and the isolation suite |
| ADR-062 | `tenant_required` is `InvalidRequest`; a tenant conflict is `Forbidden` | error categories (owned by #1257's `TenantScopeErrors`) |
| ADR-054 D4 | the tenant is an attribute and tag when multi-tenancy is on; no baggage | no tenant tag here; the tenant is not propagated to brokers |
| ADR-018, ADR-021 | 12-function evaluation; EventId ranges registered first | matrix, Phase 3 |
| Azure Architecture Center, multitenant messaging approaches | shared queues with a tenant identifier versus per-tenant queues | Design Choice 1 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| `OutboxProcessorBase` and its 8 provider subclasses | `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs`, `src/Encina.*/Outbox/OutboxProcessor.cs` | the guard is resolved from the cycle's scope, so no subclass changes |
| `ScheduledMessageProcessor`, `DelayedRetryProcessor` | `src/Encina.Messaging/Scheduling/`, `src/Encina.Messaging/Recoverability/` | wiring points |
| `InboxOrchestrator` (already carries `metadata?.TenantId`), `SagaOrchestrator` | `src/Encina.Messaging/Inbox/InboxOrchestrator.cs`, `src/Encina.Messaging/Sagas/SagaOrchestrator.cs` | wiring points |
| `DeadLetterManager` | `src/Encina.Messaging/DeadLetter/DeadLetterManager.cs` | the fail-closed tenant pattern the guard follows; where a denied message ends |
| `IOutboxStore.MarkAsExhaustedAsync`, `MarkAsFailedAsync` (no equivalent on the inbox and scheduled stores) | `src/Encina.Messaging/Outbox/IOutboxStore.cs`, `Inbox/IInboxStore.cs`, `Scheduling/IScheduledMessageStore.cs` | Design Choice 3 |
| `RequestContextScopeFactory.RunRestoredAsync` (with `configuredTenantId`) | `src/Encina/Identity/RequestContextScopeFactory.cs`, `RequestContextScopeFactory.Restore.cs` | the restored context the guard runs in; the host-configured tenant for inbound sources |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | why shared signals live in core: `Encina.Tenancy` references `Encina.Messaging` |
| `ITenantScopeResolver`, `TenantAgnosticAttribute` | planned in #1257 and #346 (core `Encina`) | the guard's rule and exemption |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | range registration |
| `Encina.Testing.Fakes` stores | `src/Encina.Testing.Fakes/Stores/` | fakes for the wiring tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (tenant dispatch guard) | 7880-7899 (new `MessagingTenancy`, from the block assigned to the family `multitenancy-1`) | registered by PR 125-A first; ids 7880-7882 used, packed from 7880; 7883-7899 spare |
| `Encina.Messaging` (existing) | none | `MessagingOutbox 2000-2099`, `MessagingInbox 2100-2199`, `MessagingSaga 2200-2299`, `MessagingScheduling 2300-2399` are not touched (#736 reserves Messaging ids 2971-2972) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production, guard (interface, implementation, metrics, log, registration edit, `EventIdRanges` edit) | 6 | PR 125-A |
| Tests, guard (unit, property, DI test edit, allocation test edit) | 4 | PR 125-A |
| Metadata, PR 125-A (`Encina.Messaging.json`, `Encina.json`, `PublicAPI.Unshipped.txt` of `Encina.Messaging` and of `Encina`) | 4 | PR 125-A (14 files in total) |
| Production, wiring: outbox base, scheduled processor, delayed retry processor | 3 | PR 125-B |
| Tests, wiring (three unit test files, the outbox one with a fake store) | 3 | PR 125-B |
| Metadata and justifications, PR 125-B (fragment, two justification files) | 3 | PR 125-B (9 files in total) |
| Production, inbox orchestrator and its `PublicAPI.Unshipped.txt` line | 2 | PR 125-C1 |
| Tests, inbox (4 files with 14 `new InboxOrchestrator(` sites, one with the wiring cases and one with the null-guard case) | 4 | PR 125-C1 (6 files in total) |
| Production, saga orchestrator and its `PublicAPI.Unshipped.txt` line | 2 | PR 125-C2 |
| Tests, saga (9 files with 20 `new SagaOrchestrator(` sites, one with the wiring cases and one with the null-guard case) | 9 | PR 125-C2 (11 files in total) |
| Documentation (`multi-tenancy.md`, Messaging README) | 2 | PR 125-D |
| Total | 42 | five worker pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, ROP (Either<EncinaError, T>). Issue #125: the tenant columns (#737, #738, #739, #760), the persisted context (#1164), the Inbox key (#3437, #3452) and rate limits (#740) are owned by other issues; this plan builds the guard that refuses to dispatch a message under the wrong tenant, or none in a multi-tenant host.
- Encina.Tenancy references Encina.Messaging, so shared signals (MultiTenancyMarker, [TenantAgnostic], ITenantScopeResolver) live in core Encina.

IMPLEMENTATION OVERVIEW:
1. ITenantDispatchGuard / TenantDispatchGuard in Encina.Messaging: ITenantScopeResolver.ResolveRead(rowTenantId) inside the restored context; [TenantAgnostic] message types are exempt and logged once.
2. Counter encina.messaging.tenant_guard.denied (tags pattern, reason); range MessagingTenancy 7880-7899, EventIds 7880-7882.
3. Wiring after the context is restored and before dispatch: outbox base (scope-resolved, no subclass edit), scheduled processor, delayed retry processor (PR 125-B); inbox orchestrator (cached-response path, PR 125-C1) and saga orchestrator (PR 125-C2), each taking the guard as a required constructor parameter before `timeProvider` (Design Choice 7) with its PublicAPI line, null-guard test and call sites.
4. A denied message is a Left from the dispatch step: the processor's existing failure path retries and then dead-letters it with its tenant.
5. Tests of unit and property types; justifications; documentation and fragments by specialists.

KEY PATTERNS:
- Fail closed: no tenant in a multi-tenant host, or a different tenant, never dispatches; never reported as success.
- No tenant value in logs, tags or messages; codes only; no scoped service in the singleton guard.
- Logical isolation only; the tenant of a message is the persisted or host-configured context, never the payload.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs
- src/Encina/Identity/RequestContextScopeFactory.cs
- docs/features/multi-tenancy.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The guard reads no cacheable data; the type-level attribute lookup is cached in a static dictionary. |
| 2 | OpenTelemetry | ✅ | Counter `encina.messaging.tenant_guard.denied` with `pattern` and `reason` tags and no tenant tag; the ADR-054 D4 tenant tag is #2676. |
| 3 | Structured Logging | ✅ | `TenantDispatchGuardLog`, EventIds 7880-7882 in the new `MessagingTenancy` range; codes, pattern and message type only. |
| 4 | Health Checks | ❌ | The guard has no dependency to check. |
| 5 | Validation | ✅ | The message's tenant is validated against the restored context before dispatch. |
| 6 | Resilience | ❌ | No external call; a denied message follows the processor's existing retry policy. |
| 7 | Distributed Locks | ❌ | No shared state is updated; processor leadership is unchanged. |
| 8 | Transactions | ❌ | The guard runs before the dispatch and writes nothing; marking the message failed is the processor's existing store call. |
| 9 | Idempotency | ✅ | Inbox: a stored message found by id under another tenant is refused instead of answered from cache, defense in depth for #3437; the key shape is #3437 and #3452. |
| 10 | Multi-Tenancy | ✅ | This feature. |
| 11 | Module Isolation | ❌ | `ModuleId` on messages is #1636, #762 and the modules family. |
| 12 | Audit Trail | ⏭️ | Per-message audit is not built (Design Choice 6: the rows are the record); an audit event for a denied dispatch is the follow-up issue file `tenant-boundary-audit` (written with #304). |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect on this plan |
|--------------|-------|---------------------|
| #1257 PR 1257-A1 (`ITenantScopeResolver`) | plan merged, code not started | the guard's rule (Design Choice 2 A) |
| PR 346-A of #346 (`TenantAgnosticAttribute` in core `Encina`) | plan merged, code not started | the exemption; PR 125-A does not build without it |
| #737, #739, #760, #738 (tenant members of the four message interfaces) | open | the guard's input; PR 125-B and 125-C wait for the pattern they wire |
| #1164 (persisted context restored on dispatch) | open, settled | the restored context the guard runs in |
| #3190 (removal of the `TenancyMessagingOptions` switches) | open | independent: the guard reads no option |
| #3437, #3452 (Inbox key shape) | open | the guard is defense in depth; it does not replace the key fix |

**Sequencing**: PR 1257-A1 (`ITenantScopeResolver`) and PR 346-A (`TenantAgnosticAttribute`) first, because the code of 125-A uses both and does not build alone before they land (with Design Choice 2 answered B, only 346-A is needed); then 125-A, which has no dependency on the tenant columns (it adds a registered guard with unit tests); 125-B, 125-C1 and 125-C2 follow the pull requests that add each tenant member and #1164 (a pattern whose prerequisite has not landed is wired later); 125-D last.

---

## Next Steps

1. Maintainer answers Design Choices 1 to 7 when work on the issue starts (recorded in `## Maintainer Decisions`); Design Choice 2 also follows the answer to #1257 Design Choice 1.
2. Worker pull requests (counts include tests, manifests, PublicAPI lines and fragments; recount by grep when the worker starts):
   - **PR 125-A** (guard, registration, range): `ITenantDispatchGuard.cs`, `TenantDispatchGuard.cs`, `TenantDispatchGuardMetrics.cs`, `TenantDispatchGuardLog.cs`, `MessagingServiceCollectionExtensions.cs`, `EventIdRanges.cs` (6); `TenantDispatchGuardTests.cs`, `TenantDispatchGuardPropertyTests.cs`, the DI test edit, `EncinaEventIdAllocationTests.cs` (4); `Encina.Messaging.json`, `Encina.json`, `PublicAPI.Unshipped.txt` of `Encina.Messaging` and of `Encina` (4): 14 files (15 if Design Choice 2 is answered B).
   - **PR 125-B** (outbox, scheduled, delayed retry): `OutboxProcessorBase.cs`, `ScheduledMessageProcessor.cs`, `DelayedRetryProcessor.cs` (3); three unit test files (3); `125-tenant-dispatch-guard.added.md`, `TenantDispatchGuard.md` for load and for benchmark (3): 9 files.
   - **PR 125-C1** (inbox): `InboxOrchestrator.cs`, `PublicAPI.Unshipped.txt` of `Encina.Messaging` (2); `InboxGuardTests.cs`, `ErrorMessageLeakTests.cs`, `InboxOrchestratorTests.cs`, `InboxPipelineBehaviorTests.cs` (4): 6 files.
   - **PR 125-C2** (sagas): `SagaOrchestrator.cs`, `PublicAPI.Unshipped.txt` of `Encina.Messaging` (2); `SagaOrchestratorGuardTests.cs`, `SagaRunnerGuardTests.cs`, `SagaRunnerOutcomeScenarios.cs`, `DeadLetterRuntimeCaptureTests.cs`, `DeadLetterSourcesCaptureTests.cs`, `ErrorMessageLeakTests.cs`, `StorePayloadEncryptionTests.cs`, `SagaOrchestratorTests.cs`, `SagaRunnerTests.cs` (9): 11 files.
   - **PR 125-D** (documentation): `docs/features/multi-tenancy.md`, `src/Encina.Messaging/README.md`: 2 files.
3. Link this plan from #125; the note on the closed-out items of the issue goes in the same comment.
