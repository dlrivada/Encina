# Implementation Plan: Message Priority for the Outbox (`Encina.Messaging` + 10 database providers)

> **Issue**: [#123](https://github.com/dlrivada/Encina/issues/123)
> **Type**: Feature
> **Complexity**: Medium (9 phases, 10 database providers, 17 pull requests of at most 15 files each, see "Delivery order and size")
> **Estimated Scope**: about 1,400 lines of production code + about 1,800 lines of tests
> **Family**: message priority and partitioning (#123, #251, #301, #469, #1254). This plan OWNS the shared priority design (type, attribute, interface, resolver, fetch contract, starvation); #301, #251 and #147 reuse it and must not decide it twice. #147 and #301 plan the same scheduled-store work; which of the two issues delivers it is Dilemma D6 of #301.

---

## Summary

Add opt-in **message priority** to the outbox: a message carries a `MessagePriority`, the outbox stores persist it, and the fetch that feeds the processors returns higher-priority messages first, with an age-based promotion so low-priority messages cannot wait forever. Priority comes from an attribute on the message type, from an interface property on the instance, or both.

Today nothing of this exists in `src/`: `MessagePriority`, `IPrioritizedRequest` and `[Priority]` are not in the tree. Every outbox store fetches through `IOutboxStore.GetPendingMessagesAsync(batchSize, maxRetries, ct)` and orders by `CreatedAtUtc` (for example `src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs`: `ORDER BY CreatedAtUtc`). `OutboxBatchProcessor.ProcessAsync` runs the returned messages one at a time. The change is therefore a new column, a new fetch ordering and a new fetch contract on all 10 database providers, plus a small amount of core code.

- **Standards**: Azure Architecture Center, Priority Queue pattern; Enterprise Integration Patterns (Resequencer is the opposite concern and is #469).
- **Scope**: outbox only. The scheduled store is #301 (it reuses everything defined here); transports are #251; per-key ordering is #469; the nullable `PartitionKey` column is #1254.
- **Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore` (the three EF Core providers share it), `Encina.MongoDB`, `Encina.OpenTelemetry` (instrumented store), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing`.
- **Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3, MongoDB (AGENTS.md section 5).

**Family contract** (which plan owns which shared piece, so the five plans stay consistent):

| Shared piece | Owner | Used by |
| --- | --- | --- |
| `MessagePriority`, `MessagePriorityAttribute`, `IPrioritizedMessage`, `MessagePriorityResolver`, the error code `messaging.invalid_priority` | #123, created by PR-A of this plan (the first pull request, standalone, no consumer) | #301, #251, #147: each starts only after PR-A has merged and never creates the type |
| `OutboxFetchCriteria` (the fetch contract) | #123 | #469 (adds partition ordering), #1251 (adds claiming) |
| Starvation rule (aged messages first) | #123 | #301 (same rule on `ScheduledAtUtc`) |
| `IOutboxMessage.PartitionKey`, `IPartitionedMessage` | #1254 | #469 |
| Merge order | #1254, then #123 (PR-A first), then #301, then #469; #251 and #147 only need PR-A | |

---

## Design Choices

<details>
<summary><strong>1. Shared priority type — one `MessagePriority` for outbox, scheduling and transports (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) One `MessagePriority` in `Encina.Messaging`, five levels `Background=0, Low=1, Normal=2, High=3, Critical=4`, higher is more urgent** | One concept, one attribute, one column type; a priority set on a command survives outbox, scheduler and broker; covers every level name used by #123, #251 and #301 | Every pattern must accept the same five levels; a package that does not reference `Encina.Messaging` needs a new project reference to use the type (the transports are scoped in #251, which registers that dependency decision) |
| **B) Separate enums per pattern (or separate queues per priority, as the issue's alternatives listed)** | Each pattern evolves alone | Two attributes and a mapping at every boundary; separate queues add tables or collections on 10 providers |

The three open issues disagree today: #123 uses `Low=0..Critical=3`, #251 uses `Low=0..Critical=4` with `Urgent`, #301 uses `Critical=0..Background=4` (ascending order by urgency). Option A also fixes direction: with "higher is more urgent" the fetch reads `ORDER BY Priority DESC`, matching RabbitMQ `x-max-priority` where a larger number is delivered first.

### Chosen Option: **A — one shared `MessagePriority`, five levels, higher is more urgent** (recommended, pending the maintainer)

### Rationale

- Option A wins when the same message type can be both outbox and scheduled, which is the normal case for domain notifications.
- Option B wins only if scheduling needs levels the outbox does not, and nothing in the three issues says so.
- The column default is `Normal` (2) written explicitly by the factory, so the numeric value of the first member does not decide what an unset row means; this removes the "zero is the harmless default" argument for `Low=0`.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where a message gets its priority — attribute, interface, or both (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Class attribute only** | Static and cheap, cached per type | Cannot depend on data such as a VIP flag |
| **B) Interface property only (`IPrioritizedMessage`)** | Dynamic per instance | Every type implements it, even when the priority is constant |
| **C) Both; the interface wins over the attribute, then the default `Normal`** | Covers static and dynamic use | Two ways to say one thing |

Names: the issues propose `IPrioritizedRequest` and `[Priority]` (#123), `IPrioritizedMessage` and `PriorityAttribute` (#251), `[MessagePriority]` (#301). The plan uses `IPrioritizedMessage` and `MessagePriorityAttribute` (two of three issues, and the object that carries the priority is the message).

### Chosen Option: **C — attribute and interface, interface wins** (recommended, pending the maintainer)

### Rationale

- Option C wins when static and dynamic use are mixed, which the issue's own examples show (`[Priority(High)] UrgentOrderCommand` and the VIP `OrderCommand`).
- Resolution is a static `ConcurrentDictionary<Type, MessagePriority?>` for the attribute (the project's "static per-type attribute caching" rule for behaviors); the interface is read per instance.
- An undefined numeric value returns a `Left` with the shared code `messaging.invalid_priority` and `ErrorCategory.Validation`, created through the category overload of `EncinaErrors.Create` (ADR 062, merged), instead of throwing (ADR-001, ADR-006). One code serves every pattern because one resolver serves every pattern (register entry R4).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Starvation prevention — aged messages first (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Age-based promotion: a message older than `StarvationThreshold` sorts before the priority order** | Bounded wait for every message; portable as one `CASE` in `ORDER BY` (two queries on MongoDB); no extra state | Needs the current time in the query; promoted messages of low priority can crowd a batch after a long outage |
| **B) Weighted fair queuing, N high for every M low** | No time comparison | Slows low priority even when no high-priority work waits unless weights are adaptive; needs per-priority state or several queries |
| **C) Strict priority, no prevention** | Simplest | Low priority can wait forever under sustained load |

### Chosen Option: **A — age-based promotion, threshold default 5 minutes when priority is enabled** (recommended, pending the maintainer)

### Rationale

- Option A wins when a hard maximum wait is an SLA, which is the stated motivation ("SLA compliance").
- The one ordering used by the SQL stores, EF Core, MongoDB and the fake is: aged rows first (a row is aged when `CreatedAtUtc <= StarvationCutoffUtc`), and inside each of the two groups `Priority DESC`, then `CreatedAtUtc` ascending. In SQL: `ORDER BY CASE WHEN CreatedAtUtc <= @StarvationCutoffUtc THEN 0 ELSE 1 END, Priority DESC, CreatedAtUtc`. MongoDB runs two queries with the same sort (aged group, then the rest up to the remaining batch size). Whether the aged group should instead be ordered by age only is the register entry R7. The cutoff is `TimeProvider.GetUtcNow() - StarvationThreshold`, computed once per fetch by `OutboxBatchProcessor`, so stores never read the clock (AGENTS.md section 3).
- Option C wins where low-priority work is genuinely optional; the option `StarvationThreshold = null` selects it.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Shape of the fetch contract — one criteria record for every fetch concern (Dilemma D4, answers #301 D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Replace `GetPendingMessagesAsync(batchSize, maxRetries, ct)` by `GetPendingMessagesAsync(OutboxFetchCriteria, ct)`** | One fetch path on 10 providers; #469 (partition ordering) and #1251 (claiming) extend the record instead of changing the signature a third and fourth time | One signature change now on the interface, 10 stores, the fakes and about 30 tests |
| **B) Add a second method `GetPendingByPriorityAsync(...)` as #301 proposes** | Explicit; supports per-priority loops | Two fetch paths with duplicated SQL on 10 providers; the next feature adds a third |
| **C) Keep the signature; stores read `OutboxOptions`** | No signature change | Stores take options today as arguments, not from DI; hidden coupling; cannot be unit tested without options |

### Chosen Option: **A — one `OutboxFetchCriteria` record** (recommended, pending the maintainer)

### Rationale

- Pre-1.0 there is no compatibility layer to keep (AGENTS.md section 1), so changing the existing method is cheaper than a parallel one (#301 Alternatives, item 4).
- `OutboxFetchCriteria(int BatchSize, int MaxRetries, bool OrderByPriority, DateTime? StarvationCutoffUtc)`; `NowUtc` is passed by the processor so `NextRetryAtUtc <= @NowUtc` keeps working as today.
- Option B wins only if #301 chooses per-priority loops (its D2-B); this plan recommends against that.
- Cost of option A, measured on origin/main: 18 `src/` files and 63 files under `tests/` mention `GetPendingMessagesAsync` (a few belong to the unrelated delayed-retry scheduler). The delivery order below migrates them in compile-safe steps (register entry R9).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Ordering design — a priority column on the existing table, one queue (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) One `Priority` column plus index, ordered fetch** | Same table, same transaction as the message, one design on all 10 providers | Ordering query is a little costlier than `ORDER BY CreatedAtUtc` |
| **B) A physical queue (table or collection) per priority** | Isolation per level | Five tables or collections times 10 providers; atomic enqueue across levels is harder; breaks the provider-coherence rule's single schema |
| **C) External priority service (Redis sorted set or similar)** | Fast | New infrastructure dependency; the outbox exists to be in the application database |

### Chosen Option: **A — one column, one queue** (recommended, pending the maintainer)

### Rationale

- Option A wins on provider coherence: the same schema change on every provider (AGENTS.md section 5), and no new infrastructure (AGENTS.md section 1).
- Option B (a physical queue per priority) is not excluded by AGENTS.md section 5, because it could be built on all ten providers; it loses on cost (five tables or collections per provider) and on atomic enqueue, so it is a design preference, not a rule. Option C is excluded by pay-for-what-you-use only if the external service is mandatory, which it would be.
- Index per provider: see register entry R3.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Opt-in and default behavior (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `OutboxOptions.Priority` (`OutboxPriorityOptions`): `Enabled = false`, `StarvationThreshold = 5 min`; column always present** | Disabled by default as required; stores keep `ORDER BY CreatedAtUtc` when off; schema is one shape | The column exists even when unused |
| **B) Column only created when enabled** | No unused column | Schema differs per configuration on 10 providers; scripts become conditional |

### Chosen Option: **A — option object, disabled by default, column always present** (recommended, pending the maintainer)

### Rationale

- Both options keep the feature off by default (AGENTS.md section 3, ADR-050), so the rule does not decide between them; option A wins on one schema shape per provider and on already queued rows being ordered when the feature is switched on later, option B wins when an unused column is unacceptable.
- The issue's `ProcessingStrategy.PriorityBased` and `PriorityQueues` flags are replaced by one `Enabled` flag because there is only one strategy after choice 5.
- Message priority is still stored when disabled (a consequence of option A).
- Options are validated in `OutboxOptions.Validate` (existing method for cross-property rules); the threshold must be positive.

**Class:** Dilemma

</details>

---

### Decision register (decisions not covered by choices 1 to 6)

Class is Trivial (no rule or preference is involved), Settled (a verifiable citation excludes every other option) or Dilemma (the maintainer decides; the entry gives a recommendation only).

| Id | Decision | Class | Text |
| --- | --- | --- | --- |
| R1 | Priority is stored on the row even when the feature is disabled | Dilemma (recommended, pending the maintainer) | A consequence of choice 6 option A; folded into it, answered with it |
| R2 | Column types: `TINYINT NOT NULL DEFAULT 2` on SQL Server and MySQL, `SMALLINT NOT NULL DEFAULT 2` on PostgreSQL (no `TINYINT`), EF Core `HasConversion<byte>()` with default 2, MongoDB `int32` | Trivial | The enum has five members (0 to 4); storage size changes no behavior. The default is written explicitly by the store, so the value of the first member does not define "unset" |
| R3 | Index key order `(ProcessedAtUtc, RetryCount, Priority, CreatedAtUtc)` replaces `IX_OutboxMessages_ProcessedAt_RetryCount` | Trivial | Any index that serves the due predicate works; the benchmark of PR-P compares it with the old index before the key order is final |
| R4 | Undefined enum value: the resolver returns `Left` `messaging.invalid_priority` (`ErrorCategory.Validation`); on the writer path (`OutboxWriter`, `TransactionalOutboxPipelineBehavior`) the `Left` fails the caller's request, it is never replaced by `Normal` | Dilemma (recommended: fail, pending the maintainer) | Option B is defaulting to `Normal` and logging; it also satisfies ADR-006 because no exception is thrown. Recommended A because a silent default hides a programming error, and the gate rule of AGENTS.md section 3 favors failing |
| R5 | Metric names `encina.outbox.priority.processed` (counter, tag `priority`), `encina.outbox.priority.wait_time` (histogram, seconds, tag `priority`), `encina.outbox.priority.aged` (counter); the tag has five bounded values | Trivial | Follow the `encina.outbox.*` names of `OutboxProcessorMetrics` |
| R6 | `StarvationThreshold = null` means strict priority (choice 3 option C) | Trivial | A nullable threshold is the smallest switch for an option already in choice 3 |
| R7 | Order inside the aged group: `Priority DESC`, then `CreatedAtUtc` (the SQL of choice 3) or age only (oldest first) | Dilemma (recommended: priority then age, pending the maintainer) | Age only gives a stricter bound on waiting but needs a second sort key per group that is not portable to a single `ORDER BY` without a computed expression on all ten providers |
| R8 | The `aged` counter and log 6707 count returned messages whose `CreatedAtUtc <= StarvationCutoffUtc`; the processor recomputes this from the messages it received, the store reports nothing extra | Trivial | `OutboxFetchCriteria` returns plain messages; no new store output is needed |
| R9 | Compile-safe delivery of the contract change | Dilemma (recommended: A, pending the maintainer) | A) a chain of 17 pull requests, each leaving main building, using transitional default members on the interfaces that are removed in the last steps (see "Delivery order and size"); B) one atomic contract pull request with all implementers and callers (about 80 files) that carries the `size-exception-approved` label. A costs more pull requests and a short-lived default member that AGENTS.md section 3 normally forbids (it is never released: it is removed before the milestone ends); B violates the 15-file rule but has no transitional code |
| R10 | The priority reaches the stored row through the mutable `IOutboxMessage.Priority` set by the orchestrator after `IOutboxMessageFactory.Create`; the factory signature does not change | Dilemma (recommended: A, pending the maintainer) | B) a new `priority` parameter on `Create`, which changes 10 factories and 49 test files that reference the factory. A keeps the contract change to the store fetch and the message property |
| R11 | Per-priority outbox depth for health checks | Defer | Follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/outbox-priority-pending-depth.md`) |

---

## Implementation Phases

### Delivery order and size

The contract change (`IOutboxStore.GetPendingMessagesAsync(OutboxFetchCriteria, ct)` and `IOutboxMessage.Priority`) touches the interface, 10 stores, the fake, the instrumented decorator, `OutboxBatchProcessor` and every caller. The plan keeps main compiling with the chain of register entry R9 (recommended option A): the new members are added as default interface members first, each provider implements them in its own pull request, callers migrate, and the old members are removed last. No type, project or log class is used by a pull request before the pull request that creates it. All pull requests have at most 15 files and 1500 lines; file counts include tests, coverage manifests and PublicAPI files.

| PR | Content (phases) | src and builders | Tests | PublicAPI | Manifests | Total |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| A | Core types: `MessagePriority`, `IPrioritizedMessage`, `MessagePriorityAttribute`, `MessagePriorityResolver`, `MessagePriorityErrorCodes` (Phase 1). Main stays green: nothing uses them yet | 5 | 4 | 1 | 1 | 11 |
| B | Core contracts: `OutboxFetchCriteria`, `OutboxPriorityOptions`, `IOutboxMessage.Priority` and `IOutboxStore` criteria fetch as default members, `OutboxOrchestrator`, `OutboxWriter`, `TransactionalOutboxPipelineBehavior`, `OutboxOptions` (Phase 2 tasks 1 to 5) | 8 | 5 | 1 | 1 | 15 |
| C | Fake store and message override the new members, `InstrumentedOutboxStore` forwards them, faker and builder `WithPriority` (Phase 3) | 5 | 4 | 2 | 3 | 14 |
| D | `OutboxBatchProcessor` builds the criteria only when `Priority.Enabled` (the old fetch is still used otherwise), log class, EventIds 6700-6707, metrics, activity tag (Phase 2 task 6, Phase 7) | 5 | 3 | 2 | 2 | 12 |
| E, F, G | ADO.NET SQL Server, PostgreSQL, MySQL, one per database: message, factory, store, two scripts, plus that database's integration, benchmark, factory/guard/schema-script tests (Phase 4) | 5 each | 3, 5, 4 | 1 each | 1 each | 10, 12, 11 |
| H, I, J | Dapper SQL Server, PostgreSQL, MySQL, one per database (Phase 5) | 5 each | 5, 5, 4 | 1 each | 1 each | 12, 12, 11 |
| K | EF Core (one implementation for the three providers): message, factory, configuration, store; three unit files, four integration files, `OutboxStoreEFTestsBase` (Phase 6 task 1) | 4 | 8 | 1 | 1 | 14 |
| L | MongoDB (message, factory, store, `MongoDbIndexCreator`) with its integration, benchmark and session tests, plus the EF guard, contract and benchmark callers (Phase 6 task 2) | 4 | 6 | 1 | 1 | 12 |
| M | `OutboxBatchProcessor` always uses the criteria fetch; migrates the mock-based callers: 4 files under `Encina.UnitTests/Messaging/Outbox`, 7 provider `OutboxProcessorTests` files (ADO x4 including `OutboxProcessorLeftResultTests`, Dapper x3) and `RetryAfterCeilingProperties` | 1 | 12 | 0 | 1 | 14 |
| N | Remaining callers move to the criteria fetch (12 test files, listed in the brief from `git grep`); the `IOutboxMessage.Priority` default is removed | 1 | 12 | 1 | 0 | 14 |
| O | The old fetch is removed from `IOutboxStore` and from the 10 stores, the fake and the decorator | 13 | 0 | 2 | 0 | 15 |
| P | `OutboxTestHelper` and `ArbitraryMessages` gain priority; property test (starvation bound, permutation), the ordered-fetch benchmark, the load-test justification `.md` | 2 | 3 | 2 | 2 | 9 |
| Q | ADR, `docs/features/message-priority.md`, `Encina.Messaging` README, `docs/INVENTORY.md`, changelog fragment (`docs-writer` and `mechanical-fixer`); these 5 files are not in the columns | - | - | - | - | 5 |
| **Total** | 17 pull requests | | | | | **203** |

The total adds up: A 11 + B 15 + C 14 + D 12 = 52; E to G 33; H to J 35; K 14 + L 12 = 26; M 14 + N 14 + O 15 = 43; P 9; Q 5; 52 + 33 + 35 + 26 + 43 + 9 + 5 = 203. Counts of existing callers come from `git grep -l GetPendingMessagesAsync` on origin/main (18 `src/` files, 63 `tests/` files); the brief of each pull request recounts them. Merge order inside the family is in the Summary.

**Tests and coverage targets ship with the code that needs them.** Each pull request above includes its unit, guard and contract or integration tests (for provider pull requests: priority-ordering cases added to the existing integration classes, in the shared collections), enters the per-file targets of the files it adds or touches in the package manifests of `.github/coverage-manifest/` with a one-sentence justification (written by `mechanical-fixer`), and reports the measured per-flag coverage in the pull request. Targets of existing files never go down.

| New file | Pull request | Unit | Guard | Justification |
| --- | --- | ---: | ---: | --- |
| `Priority/MessagePriority.cs`, `IPrioritizedMessage.cs`, `MessagePriorityErrorCodes.cs` | A | 0 | 0 | An enum, an interface and constants have no executable lines; the resolver tests use all of them |
| `Priority/MessagePriorityAttribute.cs` | A | 90 | 0 | The constructor only stores the level; no parameter is checked, so a guard test has nothing to throw |
| `Priority/MessagePriorityResolver.cs` | A | 95 | 85 | Unit tests cover interface, attribute, default and invalid value; guard tests cover the null message |
| `Outbox/OutboxFetchCriteria.cs` | B | 90 | 0 | A positional record with no parameter checks (the processor builds it from validated options) |
| `Outbox/OutboxPriorityOptions.cs` | B | 95 | 60 | Unit tests assert defaults and each rejected value; guard tests reject a non-positive threshold |
| `Diagnostics/PriorityLogMessages.cs` | D | 70 | 0 | Source-generated methods; processor tests assert the events through a fake logger |

The ordering contract test is created in C against the fake store (real code, no reflection); the 10 real stores run the same ordering cases as priority-ordering integration cases in the existing integration classes of their own pull request (E to L), so a database store never ships without its integration obligation.

### Phase 1: Core Model

<details>
<summary><strong>Tasks</strong></summary>

New folder `src/Encina.Messaging/Priority/`:

1. `MessagePriority.cs`: `public enum MessagePriority { Background = 0, Low = 1, Normal = 2, High = 3, Critical = 4 }` (namespace `Encina.Messaging`).
2. `IPrioritizedMessage.cs`: `MessagePriority Priority { get; }`.
3. `MessagePriorityAttribute.cs`: `[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)] public sealed class MessagePriorityAttribute(MessagePriority priority) : Attribute`.
4. `MessagePriorityResolver.cs`: `public static class MessagePriorityResolver` with `Either<EncinaError, MessagePriority> Resolve(object message)`; interface first, then cached attribute, then `Normal`; an undefined enum value returns `Left` built with `EncinaErrors.Create(MessagePriorityErrorCodes.InvalidPriority, ErrorCategory.Validation, ...)` (ADR 062, merged, category overload).
5. `MessagePriorityErrorCodes.cs`: `public static class MessagePriorityErrorCodes { public const string InvalidPriority = "messaging.invalid_priority"; }`, shared by outbox, scheduling and transports.
6. Update `src/Encina.Messaging/PublicAPI.Unshipped.txt` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. Plan docs/plans/message-priority-implementation-plan-123.md (issue #123). Nothing named MessagePriority exists in src/.
TASK: Create the five types listed in Phase 1 Tasks under src/Encina.Messaging/Priority/ (pull request A; nothing else uses them yet). The resolver returns Either<EncinaError, MessagePriority>; use a static ConcurrentDictionary<Type, MessagePriority?> for attributes.
KEY RULES: ROP, never throw for business input (ADR-001, ADR-006); the error uses the category overload EncinaErrors.Create(code, ErrorCategory.Validation, message) (ADR 062); nullable enabled; XML docs on every public member; no [Obsolete]; PublicAPI.Unshipped lines and manifest entries are written by mechanical-fixer; guard test for the null message in the same pull request.
REFERENCE FILES: src/Encina/Results/EncinaErrors.cs, src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (OutboxErrorCodes style), src/Encina.Messaging/Outbox/IOutboxMessage.cs, docs/plans/dsr-implementation-plan-404.md (style).
```

</details>

### Phase 2: Core Contracts, Options and Processor

<details>
<summary><strong>Tasks</strong></summary>

Pull request B (tasks 1 to 5) and D (task 6). The transitional members of tasks 1 and 2 are default interface members, removed by pull requests N and O (register entry R9).

1. `src/Encina.Messaging/Outbox/IOutboxMessage.cs`: add `MessagePriority Priority { get; set; }` with a default implementation (getter returns `Normal`, setter ignores the value) so existing implementers still compile; PR-N removes the default and makes it abstract, after the 10 provider messages and `FakeOutboxMessage` implement it.
2. `src/Encina.Messaging/Outbox/IOutboxStore.cs`: add `GetPendingMessagesAsync(OutboxFetchCriteria criteria, CancellationToken)` as a default member that calls the old `GetPendingMessagesAsync(batchSize, maxRetries, ct)` and ignores the priority fields; PR-O removes the old method and the default, so the criteria method becomes the only abstract one. New `OutboxFetchCriteria.cs` record (`BatchSize`, `MaxRetries`, `OrderByPriority`, `StarvationCutoffUtc`).
3. `src/Encina.Messaging/Outbox/OutboxOrchestrator.cs`: `AddAsync<TNotification>` resolves the priority through `MessagePriorityResolver`, returns `Left` on an invalid value, and sets `message.Priority` after `IOutboxMessageFactory.Create` (the factory signature does not change, register entry R10).
4. `src/Encina.Messaging/Outbox/OutboxWriter.cs` and `TransactionalOutboxPipelineBehavior.cs`: same resolution before the row is stored; a `Left` fails the caller's request (register entry R4).
5. `src/Encina.Messaging/Outbox/OutboxOptions.cs`: `OutboxPriorityOptions Priority { get; }` with `Enabled` (false) and `StarvationThreshold` (`TimeSpan?`, default 5 minutes); validation in `Validate`.
6. `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs` (pull request D): when `Priority.Enabled`, build `OutboxFetchCriteria` from options and `TimeProvider` and call the criteria fetch; when it is disabled keep calling the old fetch so every existing mock-based test still passes. Pull request M makes the criteria fetch unconditional (`OrderByPriority` false when disabled) and migrates those tests.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 types exist. The outbox fetch today is IOutboxStore.GetPendingMessagesAsync(batchSize, maxRetries, ct) and OutboxBatchProcessor.ProcessAsync calls it with _options.BatchSize and _options.MaxRetries.
TASK: Implement Phase 2 Tasks 1 to 5 (pull request B) or Task 6 (pull request D), as the brief says. The processor computes StarvationCutoffUtc = _timeProvider.GetUtcNow().UtcDateTime - threshold only when Priority.Enabled; otherwise OrderByPriority is false and the cutoff null.
KEY RULES: time only from TimeProvider (AGENTS.md section 3); stores never read the clock; Left for invalid priority; options validated in OutboxOptions.Validate; the default members of tasks 1 and 2 exist only until pull requests N and O remove them (never released); the pull request leaves main building and all its tests passing.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, OutboxOptions.cs, OutboxWriter.cs, TransactionalOutboxPipelineBehavior.cs, src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs (similar factory shape).
```

</details>

### Phase 3: Testing Packages and Builders

<details>
<summary><strong>Tasks</strong></summary>

Pull request C (tasks 1 and 2) and P (task 3).

1. `src/Encina.Testing.Fakes/Models/FakeOutboxMessage.cs` and `Stores/FakeOutboxStore.cs`: Priority and an override of the criteria fetch, with the same ordering as the SQL (choice 3); `src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs` forwards the criteria fetch to the inner store (a decorator that inherited the default member would silently drop the priority).
2. `src/Encina.Testing.Bogus/OutboxMessageFaker.cs` and `tests/Encina.TestInfrastructure/Builders/OutboxMessageBuilder.cs`: a `WithPriority` option.
3. Pull request P: `src/Encina.Testing/Messaging/OutboxTestHelper.cs` and `src/Encina.Testing.FsCheck/ArbitraryMessages.cs`: a `WithPriority` option and a generator.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: IOutboxMessage now has Priority; the fetch takes OutboxFetchCriteria.
TASK: Update the fake store, fake message, instrumented decorator, faker and builder (pull request C); the test helper and FsCheck arbitrary follow in pull request P.
KEY RULES: the fake store must implement the same ordering as the SQL (aged rows first, then Priority DESC, then CreatedAtUtc ascending) so the ordering contract test and the integration cases compare like with like; time from TimeProvider; no reflection-only tests.
REFERENCE FILES: src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs, src/Encina.Testing.Bogus/OutboxMessageFaker.cs, tests/Encina.TestInfrastructure/Builders/OutboxMessageBuilder.cs.
```

</details>

### Phase 4: ADO.NET Providers (SqlServer, PostgreSQL, MySQL)

<details>
<summary><strong>Tasks</strong></summary>

One pull request per database: E `Encina.ADO.SqlServer`, F `Encina.ADO.PostgreSQL`, G `Encina.ADO.MySQL`. Each:

1. `Outbox/OutboxMessage.cs`: `Priority` property (implements the default member of `IOutboxMessage`).
2. `Outbox/OutboxMessageFactory.cs`: sets the default priority `Normal` on the new row (the signature does not change, register entry R10).
3. `Outbox/OutboxStoreADO.cs`: `Priority` in the insert and the reader; override of the criteria fetch: `TOP (@BatchSize)` on SQL Server, `LIMIT @BatchSize` on PostgreSQL and MySQL; ordering expression of choice 3 when `OrderByPriority`, else `ORDER BY CreatedAtUtc`. The old fetch stays until pull request O.
4. `Scripts/001_CreateOutboxMessagesTable.sql` and `Scripts/000_CreateAllTables.sql`: `Priority TINYINT NOT NULL DEFAULT 2` (SQL Server, MySQL), `SMALLINT NOT NULL DEFAULT 2` (PostgreSQL, lower-case `priority`), and the new index (register entries R2 and R3).
5. Tests in the same pull request: priority-ordering cases in the existing integration class of that database (shared collection), the existing unit, guard, benchmark and schema-script test files that call `GetPendingMessagesAsync` or list columns, the factory test, the PublicAPI line and the manifest targets.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Core contracts from Phases 1 and 2 exist. ADO stores build SQL by hand; SQL Server uses TOP (@n) and bracketed identifiers, PostgreSQL uses lower-case unquoted columns and LIMIT, MySQL uses LIMIT and backticks.
TASK: Implement Phase 4 Tasks for the three ADO.NET packages, one package per commit.
KEY RULES: async with CancellationToken only (AGENTS.md section 3); parameters, never string-concatenated values; keep scripts idempotent (CREATE ... IF NOT EXISTS where the script already does); no migration helper; same ordering semantics on all three.
REFERENCE FILES: src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.ADO.PostgreSQL/Scripts/001_CreateOutboxMessagesTable.sql, src/Encina.ADO.MySQL/Outbox/OutboxStoreADO.cs.
```

</details>

### Phase 5: Dapper Providers (SqlServer, PostgreSQL, MySQL)

<details>
<summary><strong>Tasks</strong></summary>

One pull request per database (H `Encina.Dapper.SqlServer`, I `Encina.Dapper.PostgreSQL`, J `Encina.Dapper.MySQL`): the same five source files as Phase 4 (`OutboxMessage.cs`, `OutboxMessageFactory.cs`, `OutboxStoreDapper.cs`, the two scripts) and the same test obligations as Phase 4 task 5. The Dapper stores use `SELECT *` mapped to `OutboxMessage`, so the property must match the column name (PostgreSQL maps lower-case columns through the existing mapping).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Dapper stores use CommandDefinition with CancellationToken and a SELECT * mapped to the provider OutboxMessage class (ORDER BY CreatedAtUtc today, src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs).
TASK: Apply Phase 5 to the three Dapper packages, mirroring the ADO.NET change so both families return identical orderings for identical data.
KEY RULES: same SQL semantics as Phase 4; async only; parameterised; DbLease/transaction accessor usage unchanged.
REFERENCE FILES: src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.MySQL/Outbox/OutboxStoreDapper.cs.
```

</details>

### Phase 6: EF Core (3 providers) and MongoDB

<details>
<summary><strong>Tasks</strong></summary>

1. Pull request K: `src/Encina.EntityFrameworkCore/Outbox/OutboxMessage.cs`, `OutboxMessageFactory.cs`, `OutboxMessageConfiguration.cs` (property with default 2, index `IX_OutboxMessages_Processing` extended), `OutboxStoreEF.cs` (`OrderBy` on the aged expression, then `OrderByDescending` `Priority`, then `ThenBy` `CreatedAtUtc`; `Take(BatchSize)`). One implementation serves SqlServer, PostgreSQL and MySQL; its unit and integration test files (3 and 4) and `OutboxStoreEFTestsBase` migrate here.
2. Pull request L: `src/Encina.MongoDB/Outbox/OutboxMessage.cs` (`[BsonElement("priority")]`), `OutboxMessageFactory.cs`, `OutboxStoreMongoDB.cs` (aged query first, then the rest, both with the sort of choice 3, merged up to `BatchSize`), `MongoDbIndexCreator.cs` (compound index); also the EF guard, contract and benchmark callers.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: EF Core has one store for three database providers; MongoDB cannot express the aged-first CASE in a single sort, so it runs two ordered queries and concatenates them.
TASK: Implement Phase 6 Tasks 1 and 2.
KEY RULES: the EF query must translate on all three providers (test it on all three, not only one); MongoDB indexes are created by MongoDbIndexCreator, never ad hoc; async with CancellationToken.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, OutboxMessageConfiguration.cs, src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs, src/Encina.MongoDB/MongoDbIndexCreator.cs.
```

</details>

### Phase 7: Cross-Cutting Integration and Observability

<details>
<summary><strong>Tasks</strong></summary>

All in pull request D (the first pull request that logs, so it creates the log class and registers the range).

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `MessagingPriority = (6700, 6707)` (exactly the ids used; siblings register their own ranges from 6708, see Event ID Allocation); add `nameof(EventIdRanges.MessagingPriority)` to the `Encina.Messaging` entry of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
2. `src/Encina.Messaging/Diagnostics/PriorityLogMessages.cs` (`[LoggerMessage]`, 6700 to 6707): priority resolved from attribute or interface (type name only), invalid priority rejected (code only), priority fetch enabled at start-up, aged messages fetched (count only, register entry R8).
3. `OutboxProcessorMetrics.cs` on the shared `"Encina"` meter: the three instruments of register entry R5. `OutboxActivitySource.StartAdd` gains the tag `encina.outbox.priority`.
4. `InstrumentedOutboxStore` forwards the criteria fetch in pull request C, not here.

Never log message content or an error message; only codes and counts (AGENTS.md section 3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: ADR-021 requires the EventId range to be registered first in src/Encina/Diagnostics/EventIdRanges.cs (6700-6749 is the block assigned to this family by the plan-batch registry; this plan registers 6700-6707); the shared meter is "Encina" (OutboxProcessorMetrics).
TASK: Implement Phase 7 Tasks 1 to 3. Pack EventIds 6700-6707 sequentially.
KEY RULES: [LoggerMessage] source generator, no LoggerMessage.Define; no payload, no EncinaError.Message in logs, tags or metrics; run the architecture tests (EventId allocation) before committing.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs, src/Encina.Messaging/Diagnostics/OutboxStoreLog.cs.
```

</details>

### Phase 8: Testing

<details>
<summary><strong>Tasks</strong></summary>

Tests are not a separate phase: each pull request of the delivery table owns the tests of the code it ships. This phase lists what each kind covers and which pull request carries it.

1. Unit: resolver (attribute, interface wins, default, invalid value) in A; options validation and orchestrator in B; `OutboxBatchProcessor` criteria and cutoff with `FakeTimeProvider` in D; factory output per provider in E to L.
2. Guard (AGENTS.md section 9, #3798): the resolver's null message in A (`tests/Encina.GuardTests/Messaging/Priority`), the options' non-positive threshold in B; the existing guard classes of the provider stores that call the fetch are migrated in their provider pull request; every new public constructor or method with a validated parameter gets its argument test in the pull request that adds it.
3. Contract: the `IOutboxStore` ordering contract (aged rows first, then priority, then age; disabled mode equals `CreatedAtUtc`) runs against the fake in C; `IPrioritizedMessage` and the attribute contract in A; real stores are covered by item 5.
4. Property (FsCheck, pull request P): no message waits longer than the threshold plus one batch; the result is a permutation of the pending set restricted to `BatchSize`.
5. Integration: priority-ordered fetch on all 10 providers in the existing shared collections (`ADO-*`, `Dapper-*`, `EFCore-*`, MongoDB), `ClearAllDataAsync` in `InitializeAsync`; never a `.md` justification; each in the pull request of its provider (E to L).
6. Load and benchmark (pull request P): `.md` justification for the load test (the change is one query) and a benchmark of the ordered fetch (hot path) under `tests/Encina.BenchmarkTests` that also compares the index of register entry R3 with the old one; the existing provider benchmark classes migrate in E to L.
7. Coverage: per-file targets in `.github/coverage-manifest/Encina.Messaging.json` and the provider manifests, entered by `mechanical-fixer` in the same pull request as the tests that meet them, reported by flag from `coverage-report.cs`; CRAP at most 10 on changed methods (`OutboxBatchProcessor.ProcessAsync` is the one to watch, pull requests D and M).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: AGENTS.md section 9: per-flag coverage, tests must execute real package code, Shouldly through Encina.Testing.Shouldly, integration tests use shared [Collection("<Family>-<Database>")] fixtures.
TASK: Write the tests of Phase 8 Tasks 1 to 6 and report the per-flag coverage of every touched src/ file against its manifest target.
KEY RULES: AAA, one behavior per test, FakeTimeProvider (no Thread.Sleep); never IClassFixture for database fixtures; all 10 providers; SQLite is out of the matrix.
REFERENCE FILES: tests/Encina.IntegrationTests/Dapper/SqlServer/Outbox/OutboxStoreDapperTests.cs, tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Outbox/OutboxStoreEFIntegrationTests.cs, tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/OutboxStoreMongoDBIntegrationTests.cs, .github/coverage-manifest/Encina.Messaging.json.
```

</details>

### Phase 9: Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

Pull request Q (documentation); XML docs and PublicAPI lines ship with each code pull request.

1. XML docs on every new public member; `PublicAPI.Unshipped.txt` in every touched package (`mechanical-fixer`, in the code pull requests).
2. `changelog.d/123-message-priority.added.md` (`mechanical-fixer`, pull request Q).
3. `docs/architecture/adr/`: ADR for the shared priority design (D1 to D4); ADR number assigned when the ADR pull request opens. `docs-writer` writes it and `docs/features/message-priority.md`.
4. Package README of `Encina.Messaging`; `docs/INVENTORY.md`; `ROADMAP.md` only if the milestone changes.
5. `dotnet build --configuration Release` with zero warnings; `dotnet test`; every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT: Documentation is written by docs-writer, changelog fragments and PublicAPI lines by mechanical-fixer (path ownership hooks).
TASK: Finish Phase 9 Tasks 1 to 5 after Phases 1 to 8 are merged; link #251, #301 and #1254 from the ADR.
KEY RULES: documentation never types coverage figures by hand (covref markers); English only; no reference to a private client application (write "the reference application").
REFERENCE FILES: changelog.d/README.md, docs/architecture/adr/index.md, docs/plans/dsr-implementation-plan-404.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard or source | Relevance |
| --- | --- |
| Azure Architecture Center, Priority Queue pattern | Defines the pattern; notes that priority belongs to the queue consumer and that starvation needs a policy |
| Enterprise Integration Patterns (Hohpe and Woolf) | Message priority is a channel-level concern; a Resequencer is the opposite (per-key order, #469) |
| RabbitMQ priority queues | Larger number is delivered first, `x-max-priority`; basis for "higher is more urgent" (vendor documentation to confirm at implementation) |
| AGENTS.md sections 3, 5, 6, 7 | Opt-in patterns, 10-provider matrix, 12-function check, EventId rules |
| ADR-001, ADR-006, ADR-018, ADR-021, ADR-050 | ROP, cross-cutting check, EventIds, module dependencies |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| --- | --- | --- |
| `IOutboxMessage`, `IOutboxStore`, `IOutboxMessageFactory` | `src/Encina.Messaging/Outbox/` | Gain `Priority` and the criteria-based fetch |
| `OutboxBatchProcessor` | `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs` | Builds the criteria with `TimeProvider`; processes the batch |
| `OutboxOptions.Validate` | `src/Encina.Messaging/Outbox/OutboxOptions.cs` | Validates `OutboxPriorityOptions` |
| `OutboxProcessorMetrics`, `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/` | Shared `"Encina"` meter and `Encina.Messaging.Outbox` source |
| `OutboxHealthCheck` | `src/Encina.Messaging/Health/OutboxHealthCheck.cs` | Pending depth per priority is deferred to a new issue |
| Provider stores and scripts | `src/Encina.ADO.*`, `src/Encina.Dapper.*`, `src/Encina.EntityFrameworkCore`, `src/Encina.MongoDB` | Column, index, ordered fetch |
| Fakes, builders | `src/Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `tests/Encina.TestInfrastructure` | Updated for the new contract |

### Event ID Allocation

| Package | Range | Notes |
| --- | --- | --- |
| `Encina.Messaging` (`EventIdRanges.MessagingPriority`, new) | 6700-6707 | From the batch block 6700-6749 assigned to this family; this plan registers exactly the 8 ids it uses, packed from the start of the block. Siblings register their own ranges after it: #301 from 6708, #251 after #301 (see their plans); the rest of the block is for #469 and #1254 |

### Estimated File Count

The authoritative count is the "Delivery order and size" table (203 file changes in 17 pull requests). By category:

| Category | File changes | Notes |
| --- | ---: | --- |
| Source and builders | 78 | A 5, B 8, C 5, D 5, E to J 30, K 4, L 4, M 1, N 1, O 13, P 2; a store file appears twice (its provider pull request and pull request O) |
| Tests | 83 | A 4, B 5, C 4, D 3, E to J 26, K 8, L 6, M 12, N 12, P 3 |
| PublicAPI | 19 | One per package per pull request that adds or removes a public member |
| Manifests | 18 | One per package per pull request that adds or touches targets |
| Documentation and changelog | 5 | Pull request Q |
| Total | 203 | Equals the delivery table |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt — All Phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #123, plan docs/plans/message-priority-implementation-plan-123.md. The plan owns the shared priority design of the family (#123, #251, #301, #469, #1254). Merge order: #1254, #123, #301, #469; #251 after #123. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: (1) Priority types and resolver in Encina.Messaging (PR-A, creates MessagePriority; nothing else may create it). (2) IOutboxMessage.Priority and the criteria fetch as transitional default members, OutboxFetchCriteria, options (PR-B). (3) Fakes, decorator, builders (PR-C). (4) Processor opt-in branch, EventIds 6700-6707, metrics (PR-D). (5) ADO.NET x3, Dapper x3, EF Core, MongoDB, one pull request each (PR-E to PR-L). (6) Processor unconditional, caller migration, removal of the old members (PR-M to PR-O). (7) Helpers, property test, benchmark (PR-P). (8) Docs (PR-Q). Seventeen pull requests of at most 15 files and 1500 lines, each leaving main building with its own tests and manifest targets.

KEY PATTERNS: Either<EncinaError, T> everywhere (ErrorCategory per ADR 062); time from TimeProvider (the processor computes the starvation cutoff, stores never read the clock); opt-in via OutboxOptions.Priority.Enabled = false; ORDER BY aged rows first, then Priority DESC, then CreatedAtUtc when enabled and CreatedAtUtc otherwise; static per-type attribute cache; [LoggerMessage] with registered range; shared "Encina" meter; async database calls with CancellationToken; shared [Collection] integration fixtures.

REFERENCE FILES: src/Encina.Messaging/Outbox/*.cs, src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs, src/Encina/Diagnostics/EventIdRanges.cs, .github/coverage-manifest/Encina.Messaging.json, docs/plans/dsr-implementation-plan-404.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Writes and an ordered fetch of pending work; nothing here benefits from a cache |
| 2 | OpenTelemetry | ✅ | Priority counter, wait-time histogram, aged counter on the shared meter; `encina.outbox.priority` activity tag (Phase 7) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6700-6707 in `EventIdRanges.MessagingPriority` (Phase 7) |
| 4 | Health Checks | ⏭️ | Pending depth per priority needs a count-by-priority store method on 10 providers; follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/outbox-priority-pending-depth.md`) |
| 5 | Validation | ✅ | `OutboxPriorityOptions` in `OutboxOptions.Validate`; undefined priority value returns `Left` |
| 6 | Resilience | ❌ | No external call is added; retry and dead-letter handling are unchanged |
| 7 | Distributed Locks | ⏭️ | Multi-instance claiming is #1251; ordering must not assume one instance |
| 8 | Transactions | ✅ | The priority is written in the same `AddAsync` and transaction as the outbox row (Phase 2) |
| 9 | Idempotency | ⏭️ | Duplicate handling is #735, #148 and #1220; unchanged here |
| 10 | Multi-Tenancy | ⏭️ | The outbox has no `TenantId` yet; #737 adds it, and priority ordering is global until then |
| 11 | Module Isolation | ⏭️ | `ModuleId` on messaging entities is #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant data is created; priority is operational metadata |

---

## Prerequisites & Dependencies

- #1254 (nullable `PartitionKey`) merges first because it changes the same provider files and `IOutboxMessageFactory.Create`; this plan does not change the factory signature (register entry R10), so only the provider files overlap.
- #301, #251 and #147 wait for PR-A of this plan (the type) and for the answers to choices 1 to 4 here; their plans record the same answers.
- #1251 (row claiming) extends `OutboxFetchCriteria` later; nothing here blocks it.

## Next Steps

1. Maintainer answers the Dilemmas (choices 1 to 6, entries R1, R4, R7, R9, R10) when work starts; answers go to `## Maintainer Decisions`.
2. Orchestrator opens the follow-up issue for pending depth per priority (draft in `artifacts/decisions/plan-batch/followups/outbox-priority-pending-depth.md`).
3. Worker briefs are written per pull request (PR-A to PR-Q) with the 15-file limit and a recount of existing callers.
4. After merge: narrow #251 to the transports; the fate of #147 follows Dilemma D6 of #301.

## Maintainer Decisions

None yet. Pending: choices 1 to 6 and register entries R1, R4, R7, R9, R10 (every Dilemma above). Each answer is recorded here with its date before the brief of the first affected pull request is written.
