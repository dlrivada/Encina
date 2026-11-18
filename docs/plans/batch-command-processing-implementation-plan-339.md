# Implementation Plan: Batch Command Processing (deduplication of items inside a batch, on top of the request batching executor)

> **Issue**: [#339](https://github.com/dlrivada/Encina/issues/339)
> **Type**: Feature
> **Complexity**: Low (4 phases, 1 pull request of 14 files)
> **Estimated Scope**: about 250 lines of production code and about 450 lines of tests, in one pull request
> **Family**: msg-batching (#121, #252, #302, #339, #349). #349's plan owns `BatchCommand<,>`, `BatchResult<>`, `BatchItemResult<>`, `BatchStrategy`, the executor and the all-or-nothing transaction; #302's plan owns the `Encina.Batching` package and the `Batching` EventId range. This plan adds what #339 asks for that #349 does not: deduplication of commands inside one batch.

---

## Summary

#339 and #349 describe one feature twice: command batches with strategies, per-item results, transactional all-or-nothing, parallel processing and a configuration block. Planned separately, every item of #339 is either already owned by #349's plan or is the one thing #349 lacks:

| #339 item | Owner | Detail |
|-----------|-------|--------|
| `BatchCommandHandler<,>`, `BatchResult` with `BatchItemResult(Index, Result, WasRolledBack)` | #349 (Design Choice 3 adopts #339's richer shape) | Not repeated here |
| `BatchStrategy` | #349 (Design Choice 1): `AllOrNothing`, `PartialSuccess`, `StopOnFirstError`; #339's `ContinueOnError` equals `PartialSuccess` and its parallelism is `MaxDegreeOfParallelism` | Not repeated here |
| Transaction management for all or nothing | #349 (scoped sender and `IBatchTransactionBoundary`, proven on the 10 providers) | Not repeated here |
| Parallel processing, `MaxBatchSize`, `DefaultStrategy` | #349 (`BatchExecutionOptions`) | Not repeated here |
| `IBatchCommand<TCommand, TResponse>` interface | Not created (Design Choice 3) | #349's `BatchCommand` record is the wrapper |
| `config.UseBatchProcessing(...)` | Not created (Design Choice 4) | `AddEncinaBatching` of #302 configures it |
| "Enforce single-tenant batch scope" (deferred in the issue's matrix) | Already provided by #349: every item runs under the batch's own context | Matrix row 10 |
| **Deduplication of commands inside a batch** | **This plan** | `EnableDeduplication`, a key per command, `BatchDeduplicator` |

State of `origin/main` (`822c310c7`): no batching types exist yet (`git grep` of `BatchCommand` and `EnableDeduplication` in `src` finds nothing relevant), so the plan is written against #302 and #349 as planned.

Delivered, in `Encina.Batching`: `IBatchDeduplicationKey` (a command exposes its identity), `BatchDeduplicator` (internal: finds the first index of each key, in input order, in one pass), `BatchExecutionOptions.EnableDeduplication` (default `false`), the executor change that runs only the first command of each key and copies its outcome to the duplicates, four log messages (EventIds 7766-7769) and the documentation.

**Provider category**: none. Deduplication is in memory and per batch.

---

## Design Choices

<details>
<summary><strong>1. Where the identity of a command comes from (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The command implements `IBatchDeduplicationKey { string DeduplicationKey { get; } }`; a command without it is never deduplicated** | Explicit: the author decides what "the same command" means; no surprises from record equality | Types the application does not own cannot implement it |
| **B) A registered `IBatchItemKeySelector<TCommand>` service returning the key** | Works for types you do not own; one place for the rule | An extra service per command type; easy to forget to register |
| **C) Value equality of the command (`Equals`/`GetHashCode`)** | Zero ceremony for records | Records compare collection members by reference, so equal-looking commands differ; a mutable class compares by reference; hashing a large payload is costly |

### Chosen Option: **A, an interface on the command** (recommended, pending the maintainer)

### Rationale

A wins when the commands are the application's own (the common case of bulk imports); B wins when commands come from a shared contracts assembly that cannot reference `Encina.Batching`; C wins only for small immutable records with scalar members. Keys compare ordinally and only inside one batch, which has exactly one tenant (#349), so no tenant is composed into the key and two different tenant and key pairs can never collide. A null or empty key is not a key: that item is not deduplicated and is logged once per type (EventId 7767).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. What a duplicate receives (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A duplicate is not executed and receives a copy of the first occurrence's `Result` and `WasRolledBack`, in its own `Index`** | Idempotent semantics: the caller sees the same outcome for the same command; an all-or-nothing batch with a repeated command is not failed by the repetition | A caller counting executions sees fewer than items; `SuccessCount` counts the copies |
| **B) A duplicate receives `Left` `batching.duplicate_item` (category `AlreadyExists`) and is not executed** | Explicit: the caller learns the item was skipped | Under `AllOrNothing` a harmless repetition fails and rolls back the whole batch |
| **C) The whole batch is rejected up front with `Left` `batching.duplicate_items` (category `InvalidRequest`) before anything runs** | Strict, simple, nothing partially processed | A bulk import with a few repeated rows can never run as one batch |

### Chosen Option: **A, copy the first outcome** (recommended, pending the maintainer)

### Rationale

A wins when the batch is an import where repetition is noise; B wins when the caller must be told about each repeated item and does not use `AllOrNothing`; C wins for strict APIs where a repeated command is a client bug. With A the first occurrence runs in input order, later ones are never executed, and `StopOnFirstError` treats the copies like the first occurrence (a stop before the first occurrence skips the duplicates with `batching.item_skipped`). Under `AllOrNothing` the executor copies once its own outcome is known (its item results and its own rollback); when the batch joined a transaction owned elsewhere (#349 D9), the owner's later commit failure replaces the whole `BatchResult` with the owner's `Left`, so a copy is never observable as a success for a rolled-back first occurrence. Every item, duplicates included, keeps the per-item context of #349 (no idempotency key of the wrapper).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. No `IBatchCommand` interface</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #349's `BatchCommand<TCommand, TResponse>` record is the only wrapper** | One type to handle, document and test | #339's interface text is not honored |
| **B) Add #339's `IBatchCommand<TCommand, TResponse>` with `Commands` and `Strategy`, implemented by the record** | Matches the issue | An interface with exactly one implementer, kept alongside the record: a parallel layer |

### Chosen Option: **A, the record only**

### Rationale

`AGENTS.md` section 3 forbids parallel aliases and layers, and #349's record already carries `Commands` and `Strategy`.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (no compatibility aliases or layers) and #349 Design Choice 3.

</details>

<details>
<summary><strong>4. Configuration enters through `AddEncinaBatching`, not `UseBatchProcessing`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaBatching` of #302 and #349 registers `BatchExecutionOptions` with validation (`AddOptions<BatchExecutionOptions>()`), and `EnableDeduplication` is one of its properties, set with the standard options pattern (`services.Configure<BatchExecutionOptions>(...)`)** | One registration entry per package, as every satellite package has; no new configuration API | The issue's `config.UseBatchProcessing` name is not used |
| **B) `EncinaConfiguration.UseBatchProcessing(...)` as in the issue** | Matches the issue text | A second way to register the same options; core `EncinaConfiguration` would reference or reflect a satellite package |

### Chosen Option: **A, the satellite registration**

### Rationale

The plan prompt's Encina rules require satellite registration as `AddEncina{Feature}`; #302 and #349 already use it.

**Class:** Settled
**Settled by:** `docs/engineering/prompts/implementation-plan-prompt.md` section 3 ("Satellite DI: AddEncina{Feature}...") and #302.

</details>

<details>
<summary><strong>5. Deduplication is per batch; cross-batch idempotency stays with the Inbox (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-memory, inside one batch only** | No store, no new persistence; every provider behaves the same | A command repeated in a later batch runs again (that is what the inbox is for) |
| **B) A persistent deduplication store across batches** | Catches repeats over time | A second idempotency store next to the Inbox, which already provides exactly-once processing keyed by the request's idempotency key |

### Chosen Option: **A, per batch** (recommended, pending the maintainer)

### Rationale

A wins because `AGENTS.md` section 3 assigns idempotent exactly-once processing to the Inbox and says every pattern is optional; items sent by the executor go through the pipeline and its Inbox behavior (`src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs`). The executor clears the wrapper's idempotency key from each item's context (#349 D2), so an item has no key today and cross-batch deduplication through the Inbox needs a per-item key source, which no plan of the family provides; that gap is the follow-up issue file `artifacts/issues/batch-per-item-idempotency-key.md` of this worktree. B wins when the maintainer wants cross-batch deduplication without an Inbox and accepts a second store with its 10-provider matrix.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Off by default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EnableDeduplication` defaults to `false`** | No cost and no behavior change unless asked | One more option to set |
| **B) Default `true`** | Safer against repeated rows | Hashing keys for every batch, and a surprising copy of outcomes |

### Chosen Option: **A, off by default**

### Rationale

Every feature is opt-in in Encina.

**Class:** Settled
**Settled by:** `AGENTS.md` section 1 (pay-for-what-you-use: every feature opt-in).

</details>

---

## Implementation Phases

### Phase 1: The key contract and the deduplicator (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Batching/IBatchDeduplicationKey.cs`: `string DeduplicationKey { get; }` (D1; XML docs with an example and the rule that the key is compared ordinally inside one batch).
2. `src/Encina.Batching/BatchDeduplicator.cs` (internal static): `Plan Build<TItem>(IReadOnlyList<TItem> items, ILogger logger)` returns, for each index, `FirstIndex` (itself for the first occurrence of a key and for items without a usable key) in one pass over a `Dictionary<string, int>` with `StringComparer.Ordinal`; complexity of the method 6 or less.
3. `BatchExecutionOptions.EnableDeduplication` (D6, default `false`) added to #349's options class.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: #349 added BatchExecutionOptions, BatchExecutor and BatchItemResult in Encina.Batching. D1 (an interface on the command) and D6 (off by default) apply as recommended until the maintainer answers.
TASK: Add IBatchDeduplicationKey, the BatchDeduplicator and the EnableDeduplication option with unit and property tests.
KEY RULES: Ordinal key comparison inside one batch; a null or empty key never deduplicates; no tenant is composed into the key; XML docs with an example on every public member; zero warnings.
REFERENCE FILES: src/Encina.Batching/BatchExecutor.cs, src/Encina.Batching/BatchExecutionOptions.cs, .github/coverage-manifest/Encina.Batching.json.
```

</details>

### Phase 2: Executor integration (pull request 1)

<details>
<summary>Tasks</summary>

1. `BatchExecutor`: when `EnableDeduplication` is true, build the plan, execute only first occurrences (the batch size limit still counts every submitted item, so deduplication cannot be used to exceed it), then fill each duplicate's `BatchItemResult` with a copy of the first occurrence's `Result` and `WasRolledBack` at the duplicate's own `Index` (D2). For `AllOrNothing` the plan runs through #349's transaction path and the copies are made once the executor's own outcome (all items succeeded, or its rollback) is known, so a copy never reports success for a rolled-back first occurrence; the commit itself belongs to the lease owner (#349 D9), and the executor never calls `SaveChangesAsync`.
2. `BatchExecutionLog` (EventIds 7766-7769 in the `Batching` range registered by #302, packed after #349's 7758-7765): 7766 duplicate collapsed (Debug, index and first index), 7767 no usable key for a type while deduplication is enabled (Warning, once per type through a static `ConcurrentDictionary<Type, byte>`), 7768 duplicate summary of a batch (Debug, counts), 7769 deduplication skipped because the batch has no commands with keys (Debug).
3. Tests: unit `BatchDeduplicatorTests` (first occurrence order, empty and null keys, ordinal comparison, large lists), `BatchExecutorDeduplicationTests` (copies for each strategy, rollback copies, skipped duplicates under `StopOnFirstError`, the size limit counts duplicates), property `BatchDeduplicationPropertyTests` (every index has a result; a duplicate's result equals its first's; the executed count equals the number of distinct keys), guard for `BatchExecutionOptions` unchanged; PublicAPI.Unshipped (through `mechanical-fixer`), manifest entries.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: The deduplicator exists. D2 (a duplicate is not executed and copies the first occurrence's outcome) applies as recommended until the maintainer answers.
TASK: Wire the deduplicator into BatchExecutor and add the log messages and tests.
KEY RULES: The batch size limit counts every submitted item; copies are made once the executor's all-or-nothing outcome is known; the lease owner commits (#349 D9) and the executor never calls SaveChangesAsync; exception type only in logs; EventIds 7766-7769 only.
REFERENCE FILES: src/Encina.Batching/BatchExecutor.cs, src/Encina.Batching/Diagnostics/BatchExecutionLog.cs, tests/Encina.UnitTests/Batching (from #349).
```

</details>

### Phase 3: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Logging (function 3): Phase 2 task 2; telemetry reuses #349's counters with a new outcome value `deduplicated` on `encina.batching.items_executed` (no new instrument).
2. Multi-tenancy (function 10): deduplication happens inside a batch that has one tenant (#349); no tenant is part of the key and no identifier is composed from a tenant.
3. Errors (ADR-062): this feature adds no `EncinaError` code under the recommended D2 option A; the alternatives would add `batching.duplicate_item` (`AlreadyExists`) or `batching.duplicate_items` (`InvalidRequest`).
4. Fail-closed: a command type that implements the interface but returns an empty key is not deduplicated and is logged; it is never merged with another empty key.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: The feature adds no store; the integrations are decisions recorded above.
TASK: Verify each integration exists in code and tests.
KEY RULES: Two empty keys are never equal for deduplication; no tenant in the key; exception type only in logs.
REFERENCE FILES: AGENTS.md sections 3 and 6, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 4: Testing and documentation

<details>
<summary>Tasks</summary>

Per-flag targets, entered in `.github/coverage-manifest/Encina.Batching.json` in the same pull request; the edited files `BatchExecutor.cs`, `BatchExecutionOptions.cs` and `BatchExecutionLog.cs` do not exist on `origin/main` (they arrive with #349), so there is no baseline to measure: the targets are numeric and equal to the ones #349's plan sets for the same files, and the pull request measures them with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`.

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `BatchDeduplicator.cs` | 100 | 0 | 0 | 85 | 0 |
| `BatchExecutor.cs` (edited) | 92 | 0 | 0 | 80 | 70 |
| `BatchExecutionOptions.cs` (edited) | 100 | 0 | 0 | 0 | 0 |
| `BatchExecutionLog.cs` (edited) | 80 | 0 | 0 | 0 | 0 |
| `IBatchDeduplicationKey.cs` | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| `BatchDeduplicator.cs` unit 100, property 85 | One pure pass over a list: every branch runs in unit tests and the property test checks the invariants over generated lists. Guard 0: internal static type, with no `InternalsVisibleTo` for `Encina.GuardTests` (verified at work start; raised if present). |
| `BatchExecutor.cs` edited, unit 92, property 80, integration 70 | The same targets as #349's plan for this file: the new branch is covered by the new unit and property tests, and the integration flag keeps #349's 70 because the deduplication path adds no database access. |
| `BatchExecutionOptions.cs` unit 100, `BatchExecutionLog.cs` unit 80 | Pure data (one more property) and a generated log class covered through the executor tests, as #349 targets them. |
| Interface 0 | No executable lines. |

1. **Unit, Property** as above; **Integration**: the all-or-nothing deduplication scenario (a repeated command under `AllOrNothing` is not executed twice) is added to #349's provider classes where #349's integration base exists (one scenario method in the base, no new files); until then the unit test with the fake boundary covers it.
2. **Load and Benchmark**: no new file; the justification files of #349 (`tests/Encina.LoadTests/Batching/RequestBatching.md`, `tests/Encina.BenchmarkTests/Batching/RequestBatching.md`) cover the executor, and a hashing pass over the keys is not a decision-relevant hot path.
3. **Documentation**: the page `docs/features/request-batching.md` and `src/Encina.Batching/README.md` get a deduplication section through `docs-writer`; `changelog.d/339-batch-command-deduplication.added.md` through `mechanical-fixer`.
4. `dotnet build --configuration Release` with zero warnings; all tests green; every flag at its target; CRAP of every changed method below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Per-flag targets live in .github/coverage-manifest/Encina.Batching.json; targets never go down; tests execute real code.
TASK: Write the tests, the manifest entries with one-sentence justifications, and the documentation and changelog fragment through the specialists that own them.
KEY RULES: Shouldly through Encina.Testing.Shouldly; no Thread.Sleep; never edit CHANGELOG.md; no hand-typed coverage figures in docs.
REFERENCE FILES: .github/coverage-manifest/Encina.Batching.json, docs/features/request-batching.md (from #349), changelog.d/README.md, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 1 and 3 | Opt-in features; no parallel layers; Inbox owns exactly-once processing | D3, D5, D6 |
| ADR-021, ADR-054, ADR-062 | EventIds, telemetry, error categories | Phases 2 and 3 |
| Enterprise Integration Patterns: Aggregator; MassTransit batch consumers (named by the issue) | Comparable batch handling | Issue references |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `BatchExecutor`, `BatchExecutionOptions`, `BatchItemResult`, `BatchExecutionLog` | `Encina.Batching` (#349) | Extended |
| `Batching` EventId range | `src/Encina/Diagnostics/EventIdRanges.cs` (#302) | Ids 7766-7769 |
| `InboxPipelineBehavior` | `src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs` | Cross-batch idempotency, unchanged |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Batching` (`Batching`, registered by #302) | 7766-7769, all four used | Inside the assigned block 7750-7799; #302 uses 7750-7757, #349 7758-7765 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 5 | `IBatchDeduplicationKey` and `BatchDeduplicator` (new); `BatchExecutor`, `BatchExecutionOptions`, `BatchExecutionLog` (edited) |
| Public API and manifest | 2 | `PublicAPI.Unshipped.txt` of `Encina.Batching`, `Encina.Batching.json` |
| Tests | 3 | deduplicator unit, executor deduplication unit, property |
| Documentation | 3 | feature page edit, README edit, changelog fragment (no inventory line: no new package or module) |
| **Pull request 1** | **14** | 5 production, PublicAPI, manifest, 3 new tests, 1 existing test file edited (the options validation test of #349), feature page, README, changelog fragment; about 700 lines |

Callers and implementers, by grep on `origin/main` (2026-11-18, `822c310c7`): the types this plan edits do not exist yet (they come from #349), so there are no callers; the only external contract change is a new optional property, `EnableDeduplication`, and a new interface nobody implements. The pull request adds production code together with its tests, builds alone, and needs #349 pull request 1 first.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP. #302 provides Encina.Batching; #349 provides the batch wrapper, the executor, per-item results and strategies.

IMPLEMENTATION OVERVIEW: One pull request adds IBatchDeduplicationKey, BatchDeduplicator, BatchExecutionOptions.EnableDeduplication (default false), the executor change (run first occurrences only, copy outcomes to duplicates once the executor's all-or-nothing outcome is known) and log messages 7766-7769. Dilemmas D1, D2 and D5 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Ordinal keys inside one batch with one tenant; an empty key never deduplicates; the batch size limit counts every submitted item; the lease owner commits, never the deduplication code; exception type only in logs; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.Batching/BatchExecutor.cs, src/Encina.Batching/BatchExecutionOptions.cs, src/Encina.Batching/Diagnostics/BatchExecutionLog.cs, .github/coverage-manifest/Encina.Batching.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Batch writes are not cached; deduplication keeps its state in the batch and drops it afterwards. |
| 2 | OpenTelemetry | ✅ | The `deduplicated` outcome value on #349's items counter; no new instrument. |
| 3 | Structured Logging | ✅ | EventIds 7766-7769 in the registered `Batching` range. |
| 4 | Health Checks | ❌ | An in-process pass over a list; nothing to check. |
| 5 | Validation | ❌ | Keys are the application's own data and an empty key is treated as no key; per-item validation is #349's. |
| 6 | Resilience | ❌ | No external call. |
| 7 | Distributed Locks | ❌ | Deduplication is inside one batch in one process. |
| 8 | Transactions | ✅ | Under all or nothing the plan runs through #349's transaction path (the lease owner commits, #349 D9) and the copies follow the executor's own outcome. |
| 9 | Idempotency | ✅ | This plan is within-batch idempotency; cross-batch idempotency stays with the Inbox (Design Choice 5). |
| 10 | Multi-Tenancy | ✅ | A batch has one tenant (#349), so the key needs no tenant segment and no tenant-qualified identifier is built. |
| 11 | Module Isolation | ❌ | No module-scoped state; the issue's module-scoped batches wait for the module family. |
| 12 | Audit Trail | ⏭️ | Auditing bulk operations is the audit family's work (#749); the items already pass the audit behaviors individually (#349). |

---

## Prerequisites & Dependencies

- **#349, pull request 1** (executor, options, results, log) and, transitively, **#302, pull requests 1 to 3** (package, `Batching` range, `AddEncinaBatching`). The all-or-nothing scenario test needs #349 pull request 4 and the options registration needs #349 pull request 2; the open Dilemmas of #349 (D1 strategies, D4 to D6 and D9 transaction ownership) apply here unchanged.
- **Related, not blocking**: the Inbox (cross-batch idempotency), #749 (audit of bulk operations).

---

## Next Steps

1. Ask the maintainer D1, D2 and D5 when work on #339 starts; record the answers in `## Maintainer Decisions`.
2. Single pull request (14 files) after #349 pull request 1: key contract, deduplicator, executor integration, tests, documentation.
