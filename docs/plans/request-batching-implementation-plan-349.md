# Implementation Plan: Request Batching and Bulk Operations (batch command and query wrappers, strategies, per-item results, builder, all-or-nothing transaction)

> **Issue**: [#349](https://github.com/dlrivada/Encina/issues/349)
> **Type**: Feature
> **Complexity**: High (6 phases, 7 pull requests of 14, 14, 11, 10, 8, 9 and 6 files)
> **Estimated Scope**: about 1,000 lines of production code and about 2,000 lines of tests, split over seven pull requests
> **Family**: msg-batching (#121, #252, #302, #339, #349). This plan **owns** `BatchCommand<,>`, `BatchQuery<,>`, `BatchResult<>`, `BatchItemResult<>`, `BatchStrategy`, `IBatchExecutor`, the builder, the core seam `IScopedRequestSender`, `IBatchTransactionBoundary` and the EventIds 7758-7765. It builds on #302 (package `Encina.Batching`, `AddEncinaBatching`, the `Batching` EventId range). #339 adds deduplication on top of this plan.

---

## Summary

The issue asks for wrappers that run many single-item commands or queries as one operation, strategies (all or nothing, partial success, stop on first error, parallel), a result with per-item outcomes, a fluent builder, transactional batching and parallel execution, reusing the existing single-item handlers. State of `origin/main` (`b50b8c981`): none of it exists (`git grep` for `BatchCommand`, `BatchQuery` and `BatchStrategy` in `src` finds nothing; the two `BatchCommand` hits in `tests` and `src/Encina.Quartz/README.md` are unrelated sample records).

Two facts of today's code shape the plan:

| Fact | Evidence | Consequence |
|------|----------|-------------|
| Every `IEncina.Send` creates its own DI scope | `src/Encina/Dispatchers/Encina.RequestDispatcher.cs`, `using var scope = Encina._scopeFactory.CreateScope()` | Looping over `Send` gives each item its own scoped `IDbTransactionAccessor` and connection, so "all or nothing" is not atomic. A batch needs to run its items in one scope (Design Choice 4) |
| `IEncina` has no service provider member | `src/Encina/Abstractions/IEncina.cs` | The issue's `encina.Batch<...>()` builder can only call `Send`, so the builder wraps a `BatchCommand` and sends it (Design Choice 8) |

Delivered: in `Encina.Batching` the wrappers, the result types, the strategies, `IBatchExecutor` with a `BatchCommandHandler<,>` and a `BatchQueryHandler<,>` that call it, the builder, registration by assembly scan, telemetry and logging; in core `Encina` a small seam `IScopedRequestSender` (send in an existing scope); and, for all or nothing, `IBatchTransactionBoundary` with an adapter in `Encina.Messaging` over the provider-neutral `ITransactionLeaseSource` (EF Core, ADO.NET, Dapper and MongoDB register it today), proven on the 10 providers.

**Provider category**: Database (10) for the all-or-nothing atomicity, because the transaction semantics differ per provider (SQL Server and MySQL reject a command on a connection with a pending transaction, PostgreSQL silently joins, ADR-048; MongoDB needs a replica set). The code is provider-neutral (one adapter over `ITransactionLeaseSource`), and the integration tests run on all 10 providers (Phase 6).

---

## Design Choices

<details>
<summary><strong>1. The strategy set (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `BatchStrategy { AllOrNothing, PartialSuccess, StopOnFirstError }`; parallelism is an option (`MaxDegreeOfParallelism`), not a strategy; #339's `ContinueOnError` is dropped as a duplicate of `PartialSuccess`** | Each member answers one question (what happens on a failure); no two members mean the same | Neither the issue's `ParallelAll` nor `ContinueOnError` exists by name |
| **B) The five names of the two issues (`AllOrNothing`, `PartialSuccess`, `StopOnFirstError`, `ParallelAll`, `ContinueOnError`)** | Matches the issue text | `ParallelAll` mixes "how" with "what on failure"; `ContinueOnError` equals `PartialSuccess` |
| **C) A strategy enum plus a separate `ExecutionMode { Sequential, Parallel }`** | Orthogonal and explicit | A second enum where an integer option says the same |

### Chosen Option: **A, three strategies and a degree-of-parallelism option** (recommended, pending the maintainer)

### Rationale

A wins when the API should be small and each name unambiguous; B wins only if the maintainer wants the issue text verbatim. `AllOrNothing` and `StopOnFirstError` always run sequentially (Design Choice 7).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Items run through `IEncina.Send`, so every item keeps its full pipeline</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each item is sent with `IEncina.Send` (or `IScopedRequestSender` in an all-or-nothing batch) with the batch's context** | Validation, authorization, idempotency and domain events run per item; existing single-item handlers are reused unchanged | Cost of one pipeline per item (use `IBatchHandler` of #302 where a bulk handler exists) |
| **B) Call the single-item handler directly** | Faster | Bypasses the pipeline: the issue's alternatives (#339, #349) reject it |

### Chosen Option: **A, through the pipeline**

### Rationale

Both issues list "bypasses the CQRS pipeline, validation and domain events" as the reason against direct bulk calls and require leveraging the existing handlers.

**Class:** Settled
**Settled by:** #349 acceptance criteria ("Batch commands/queries leverage existing single-item handlers") and #339 alternatives ("Batch commands preserve the full CQRS pipeline per item").

</details>

<details>
<summary><strong>3. The result carries index, outcome and a rolled-back flag per item</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `BatchResult<T>(IReadOnlyList<BatchItemResult<T>> Items, int SuccessCount, int FailureCount, BatchStrategy StrategyUsed)` with `AllSucceeded` computed, and `BatchItemResult<T>(int Index, Either<EncinaError, T> Result, bool WasRolledBack)`** | Satisfies #339's per-item index and rollback flag and #349's per-item error reporting with one type | Callers read `Items[i].Result` |
| **B) #349's `IReadOnlyList<Either<EncinaError, T>>`** | Smallest | No `Index` and no `WasRolledBack`, which #339 requires |

### Chosen Option: **A, the richer shape**

### Rationale

Only A satisfies both issues, and adding the fields later would change a public record. A skipped item (stop on first error) is `Left` with the code `batching.item_skipped` (category `FailedPrecondition`); a rolled-back success keeps its `Right` and sets `WasRolledBack`.

**Class:** Settled
**Settled by:** #339 body (`BatchItemResult(int Index, Either<EncinaError, TResponse> Result, bool WasRolledBack)`) and #349 acceptance ("Clear error reporting per item").

</details>

<details>
<summary><strong>4. How all or nothing becomes atomic (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A core seam `IScopedRequestSender.SendInScope(request, IServiceProvider scope, IRequestContext, ct)` runs an item's pipeline in the scope that already holds the batch's transaction; the executor begins the transaction through `IBatchTransactionBoundary` (adapter in `Encina.Messaging` over `ITransactionLeaseSource`) and commits only when every item is `Right`; items' `TransactionPipelineBehavior` instances join the lease** | Reuses existing single-item handlers and their pipelines inside one transaction on every provider that registers `ITransactionLeaseSource`; no provider-specific code | A core change (one interface, one refactor of the dispatcher's scope creation); AllOrNothing needs a registered boundary |
| **B) All or nothing only through an `IBatchHandler` (#302) whose handler owns the transaction** | No core change | Does not reuse existing single-item handlers, which #349 requires |
| **C) Per-item `Send` plus compensation (undo the earlier items on a failure)** | No shared scope | No handler has an undo; not atomic; failure of the compensation leaves partial state |

### Chosen Option: **A, the scoped sender and the transaction boundary** (recommended, pending the maintainer)

### Rationale

A wins because it is the only option that is both atomic and reuses single-item handlers; B wins if the maintainer prefers no core change and accepts that atomic bulk work uses bulk handlers; C is rejected (no atomicity). Without a registered `IBatchTransactionBoundary`, a request for `AllOrNothing` fails with `batching.transaction_unavailable` (fail closed, category `FailedPrecondition`): it never silently degrades to partial success. The executor is the commit owner of this transaction: it calls the lease's commit once; nothing in the orchestration calls `SaveChangesAsync`, so a handler that writes through a `DbContext` calls it itself (or through its unit of work) and the commit makes those writes durable together.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Result when an all-or-nothing batch fails (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Right(BatchResult)` with `AllSucceeded = false`: the failing item's `Left`, earlier successes marked `WasRolledBack = true`, later items `batching.item_skipped`** | The caller sees which item failed and why; one shape for every strategy | A caller that ignores `AllSucceeded` may believe the call succeeded |
| **B) `Left(batching.all_or_nothing_failed)` with no item detail** | A failure is a `Left`, as ROP callers expect | The per-item detail the issues require is lost |

### Chosen Option: **A, a result with flags** (recommended, pending the maintainer)

### Rationale

A wins when the caller must know the failing item; B wins when the caller only branches on success and failure. `Left` is reserved for failures of the batch itself (too large, no tenant when multi-tenancy is on, transaction unavailable, commit failed, cancellation). The XML docs of `BatchResult` state that `AllSucceeded` must be checked.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. How the closed handlers `BatchCommandHandler<TCommand, TResponse>` are registered (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaBatching(params Assembly[])` scans the given assemblies for `ICommand<T>` and `IQuery<T>` implementations and registers the closed `ICommandHandler<BatchCommand<C,T>, BatchResult<T>>` and `IQueryHandler<...>` for each** | Zero ceremony; the same way `AddEncina(assembly)` finds handlers | A handler for every command type, used or not (small, generated lazily by DI) |
| **B) An explicit `AddBatchCommand<TCommand, TResponse>()` per type** | Pay for what you use per type; no scan | A call per type |
| **C) The builder calls `IBatchExecutor` directly, with no wrapper request** | No registration | The batch is not a request: no pipeline (authorization, transaction, logging) around the batch itself |

### Chosen Option: **A, assembly scan** (recommended, pending the maintainer)

### Rationale

DI cannot register an open `ICommandHandler<BatchCommand<,>, BatchResult<>>` (the service type is a constructed generic over the implementation's parameters, not an open generic definition), so a closed registration per command type is unavoidable. A wins when many commands are batchable; B wins when only a few are and an explicit list is preferred; C is rejected because the wrapper-as-request is what lets the batch pass the pipeline.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Parallelism rules</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PartialSuccess` may run up to `MaxDegreeOfParallelism` items at once (each in its own scope through `Send`); `AllOrNothing` and `StopOnFirstError` run sequentially** | A shared scope has one scoped accessor and one connection, which are not safe for concurrent use; stop-on-first-error needs an order | Only partial success scales out |
| **B) Parallel everywhere** | Fastest | A joined transaction on one connection would be used concurrently; "first" error loses its meaning |

### Chosen Option: **A, sequential where a scope or an order is shared**

### Rationale

`IDbTransactionAccessor` is scoped, one instance per request scope shared by every participant, over one connection (its remarks, ADR-048), and `StopOnFirstError` is defined by order.

**Class:** Settled
**Settled by:** `src/Encina.Messaging/Transactions/IDbTransactionAccessor.cs` (one scoped accessor and connection per scope) and ADR-048.

</details>

<details>
<summary><strong>8. The builder wraps a `BatchCommand` and calls `Send`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `encina.Batch<TCommand, TResponse>()` returns a `BatchBuilder` whose `ExecuteAsync` builds a `BatchCommand` and calls `IEncina.Send`** | Needs only `IEncina`; the batch itself passes the pipeline | The command's handler must be registered (D6) |
| **B) The builder resolves `IBatchExecutor`** | Direct | `IEncina` exposes no service provider to resolve it from |

### Chosen Option: **A, send the wrapper**

### Rationale

`IEncina` declares `Send`, `Publish` and stream members only.

**Class:** Settled
**Settled by:** `src/Encina/Abstractions/IEncina.cs` (no service access) and #302 Design Choice 9.

</details>

---

## Implementation Phases

### Phase 1: Types and the executor without all or nothing (pull request 1)

<details>
<summary>Tasks</summary>

1. In `src/Encina.Batching/`: `BatchStrategy` (D1), `BatchItemResult<T>` and `BatchResult<T>` (D3), `BatchCommand<TCommand, TResponse>(IReadOnlyList<TCommand> Commands, BatchStrategy? Strategy = null) : ICommand<BatchResult<TResponse>>` (a null strategy means `BatchExecutionOptions.DefaultStrategy`; the constructor rejects a null list), `BatchExecutionOptions` (`DefaultStrategy` `PartialSuccess`, `MaxBatchSize` 100 validated 1 to 10,000, `MaxDegreeOfParallelism` 1 validated 1 to 64 (default `Environment.ProcessorCount` capped at 64 when parallelism is enabled by the caller), `TimeoutPerItem` default none, validated above zero).
2. `IBatchExecutor.ExecuteAsync<TItem, TResponse>(IReadOnlyList<TItem> items, BatchStrategy strategy, IRequestContext context, CancellationToken)` where `TItem : IRequest<TResponse>`, and `BatchExecutor` (internal sealed, scoped; dependencies `IEncina`, `IOptions<BatchExecutionOptions>`, `TimeProvider`, `ILogger`): too large a list returns `Left` `batching.batch_too_large` (`InvalidRequest`); when the container holds `MultiTenancyMarker` and `context.TenantId` is null it returns `Left` `batching.tenant_required` (`FailedPrecondition`, fail closed); `PartialSuccess` runs every item with up to the configured degree of parallelism, `StopOnFirstError` runs in order and marks the rest `batching.item_skipped`; a per-item timeout (`TimeoutPerItem`, a `CancellationTokenSource` created with the injected `TimeProvider` and linked to the caller's token) yields `Left` `batching.item_timeout` (`Timeout`) for that item and does not fail the others; an item exception is caught as `Left` `batching.item_failed` (`Internal`, exception type through `ForLogging()` only); caller cancellation propagates as `OperationCanceledException`. Every item is sent with the batch's context, so no item can run under another tenant or identity.
3. `BatchCommandHandler<TCommand, TResponse> : ICommandHandler<BatchCommand<TCommand, TResponse>, BatchResult<TResponse>>`: reads the strategy, calls the executor with `IRequestContext` from the dispatcher (the handler receives it through the ambient `IRequestContextAccessor`), and returns the `BatchResult` as `Right`; `AllOrNothing` is not implemented yet in this pull request and returns `Left` `batching.transaction_unavailable` until pull request 4 (D4).
4. `Diagnostics/BatchExecutionLog.cs` (`[LoggerMessage]`, EventIds 7758-7765: 7758 batch started, 7759 batch completed with counts, 7760 item failed by error code, 7761 stopped on first error, 7762 rolled back, 7763 batch rejected as too large, 7764 transaction unavailable, 7765 item timed out; pull request 1 uses 7758-7761, 7763 and 7765, the others arrive with their code), inside the `Batching` range registered by #302.
5. Tests: unit `BatchExecutorTests` (each strategy, parallel degree respected with a gate, timeout with `FakeTimeProvider`, skipped items, a throwing item, tenant fail-closed, too large), `BatchCommandHandlerTests`, guard `BatchWrappersGuardTests` (null lists, null context); PublicAPI (through `mechanical-fixer`), manifest entries.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: #302 created the Encina.Batching package, AddEncinaBatching and the Batching EventId range. Each IEncina.Send runs in its own DI scope. D1 (three strategies), D2 (items through the pipeline), D3 (BatchResult with items) and D7 (parallel only for partial success) apply; D4 and D5 arrive in pull request 4.
TASK: Add the wrapper, result and strategy types, the executor for PartialSuccess and StopOnFirstError, the command handler and the log, with tests.
KEY RULES: Items run with the batch's own IRequestContext; never log or return an exception message or EncinaError.Message; time from TimeProvider; ADR-062 category on every error code; method complexity 8 or less (CRAP <= 10); EventIds 7758-7765 only.
REFERENCE FILES: src/Encina/Abstractions/CqrsContracts.cs, src/Encina/Abstractions/IEncina.cs, src/Encina/Abstractions/MultiTenancyMarker.cs, src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs (batch loop style), .github/coverage-manifest/Encina.Batching.json.
```

</details>

### Phase 2: Queries, registration and the builder (pull request 2)

<details>
<summary>Tasks</summary>

1. `BatchQuery<TQuery, TResponse>(IReadOnlyList<TQuery> Queries, BatchStrategy? Strategy = null) : IQuery<BatchResult<TResponse>>` and `BatchQueryHandler<,>`: `AllOrNothing` on a query batch returns `Left` `batching.strategy_not_supported` (`InvalidRequest`) because a read has nothing to roll back.
2. `BatchHandlerRegistrar` (internal static) and `AddEncinaBatching(this IServiceCollection, params Assembly[] assemblies)` scan overload (D6); `BatchExecutionOptions` registered with `ValidateOnStart`; the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` in both registration orders and proves a registered closed handler resolves.
3. `BatchBuilder<TRequest, TResponse>` and the extension `encina.Batch<TCommand, TResponse>()` / `encina.Batch<TQuery, TResponse>()` (D8): `Add`, `AddRange`, `WithStrategy`, `WithParallelism`, `ExecuteAsync(ct)` (builds the wrapper and calls `IEncina.Send`).
4. `Diagnostics/BatchExecutionTelemetry.cs`: counters `encina.batching.batch_executions` and `encina.batching.items_executed` (tags `batch.strategy`, `outcome`, the tenant tag only when multi-tenancy is on, ADR-054 D4) and an `Activity` per batch on the `Encina.Batching` source of #302.
5. Tests: `BatchQueryHandlerTests`, `BatchBuilderTests`, `BatchRegistrationTests` (DI), guard tests for the builder and the extension; PublicAPI, manifest, README edit through `docs-writer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Pull request 1 added the executor and the command handler. D6 (assembly scan registers the closed handlers) and D8 (the builder sends the wrapper) apply.
TASK: Add query batching, the registrar, the DI overload, the builder and the telemetry, with tests.
KEY RULES: Registration independent of order, proven by a DI test with ValidateOnBuild and ValidateScopes; AllOrNothing on a query is rejected, never ignored; no scoped service in a singleton; tenant tag only when multi-tenancy is on.
REFERENCE FILES: src/Encina/Dispatchers/MediatorAssemblyScanner.cs, src/Encina/Core/ServiceCollectionExtensions.cs, src/Encina.Batching/ServiceCollectionExtensions.cs (from #302), src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs.
```

</details>

### Phase 3: The core seam (pull request 3)

<details>
<summary>Tasks</summary>

1. `src/Encina/Abstractions/IScopedRequestSender.cs`: `ValueTask<Either<EncinaError, TResponse>> SendInScope<TResponse>(IRequest<TResponse> request, IServiceProvider scope, IRequestContext context, CancellationToken cancellationToken = default)`; the XML docs state that the caller owns the scope and that the same identity and tenant rules as the explicit-context `Send` apply.
2. `src/Encina/Dispatchers/Encina.RequestDispatcher.cs`: the scope creation moves behind a small private method so that `ExecuteAsync` takes an optional existing provider (the rest of the dispatch is unchanged); `src/Encina/Core/Encina.cs` implements the interface by calling it; `src/Encina/Core/ServiceCollectionExtensions.cs` registers `IScopedRequestSender` as the same singleton instance as `IEncina`.
3. Callers and implementers: `IEncina` has no change (a separate interface, so no existing implementer or fake breaks); verify at work start with `git grep -n ": IEncina"` that no other class implements `IEncina` outside tests.
4. Tests: unit `ScopedRequestSenderTests` (an item sent in a scope resolves the same scoped service instance as the caller's scope; a different scope gives a different one; the ambient context and the identity rules behave as in `Send`), guard `ScopedRequestSenderGuardTests`; core PublicAPI line, `Encina.json` manifest entries.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Each Send creates a DI scope, so batch items cannot share a transaction. D4 (option A) adds a core seam that sends in an existing scope.
TASK: Add IScopedRequestSender and its implementation in Encina without changing the behavior of Send.
KEY RULES: The existing Send overloads and their tests must pass unchanged; the identity, tenant and ambient-context rules are identical to the explicit-context Send; the caller owns and disposes the scope; XML docs with an example.
REFERENCE FILES: src/Encina/Dispatchers/Encina.RequestDispatcher.cs, src/Encina/Core/Encina.cs, src/Encina/Abstractions/IEncina.cs, .github/coverage-manifest/Encina.json.
```

</details>

### Phase 4: All or nothing (pull request 4)

<details>
<summary>Tasks</summary>

1. `IBatchTransactionBoundary` in `Encina.Batching`: `ValueTask<Either<EncinaError, IBatchTransaction>> BeginAsync(CancellationToken)`; `IBatchTransaction : IAsyncDisposable` with `CommitAsync` and `RollbackAsync` (a dispose without commit rolls back).
2. `BatchExecutor` all-or-nothing path (D4, D5): resolve the optional boundary from the handler's scope (a null boundary returns `batching.transaction_unavailable`, EventId 7764); begin; run the items in order with `IScopedRequestSender.SendInScope(item, scopeProvider, context, ct)`; stop at the first `Left`, roll back, mark earlier successes `WasRolledBack`, later items `batching.item_skipped` (EventId 7762); commit once when all succeeded; a failed commit returns `Left` (the lease's `transaction.*` code) and marks every item rolled back. `BatchCommandHandler` passes its injected `IServiceProvider` (the handler's scope).
3. Tests: unit `BatchExecutorAllOrNothingTests` with a fake boundary and fake sender (commit once; rollback on a failure; commit failure; missing boundary fails closed; items share one scope instance), guard, PublicAPI, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: The core seam exists. D4 and D5 apply: the executor begins a transaction through IBatchTransactionBoundary, sends the items in the handler's scope and commits only when all are Right; a failure returns Right(BatchResult) with WasRolledBack flags.
TASK: Add the boundary contract and the all-or-nothing path with unit tests.
KEY RULES: Never degrade silently to partial success; the executor is the single commit owner; never claim atomicity beyond the shared scope's transaction (the executor never calls SaveChangesAsync); exception type only in logs; AllOrNothing is always sequential.
REFERENCE FILES: src/Encina.Messaging/Transactions/ITransactionLeaseSource.cs, src/Encina.Messaging/Transactions/ITransactionLease.cs, src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs.
```

</details>

### Phase 5: The Messaging adapter (pull request 5)

<details>
<summary>Tasks</summary>

1. `src/Encina.Messaging/Encina.Messaging.csproj`: a `ProjectReference` to `Encina.Batching` (the reverse direction does not exist, so there is no cycle; #302 states `Encina.Batching` depends on `Encina` only).
2. `MessagingBatchTransactionBoundary` (internal sealed, scoped) implementing `IBatchTransactionBoundary` over `ITransactionLeaseSource.BeginOrJoinAsync`; its transaction commits only when the lease is the owner (a batch started inside an existing transaction joins and leaves the commit to the owner, which the XML docs state); registered with `TryAddScoped` in `MessagingServiceCollectionExtensions` next to the existing `ITransactionLeaseSource` registration (`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:307`), and in the EF Core and MongoDB registrations that register the lease source (`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:248`, `src/Encina.MongoDB/ServiceCollectionExtensions.cs:513`) so that every provider path has a boundary.
3. Tests: unit `MessagingBatchTransactionBoundaryTests`, a DI test per registration site, guard test; PublicAPI for `Encina.Messaging`, manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Encina.Batching defines IBatchTransactionBoundary; Encina.Messaging owns ITransactionLeaseSource, registered by Messaging, EF Core and MongoDB.
TASK: Add the adapter and register it wherever ITransactionLeaseSource is registered.
KEY RULES: One adapter over the provider-neutral lease, no provider-specific code; TryAdd so the application wins; a joined lease never commits; DI tests with ValidateOnBuild and ValidateScopes.
REFERENCE FILES: src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs, src/Encina.MongoDB/ServiceCollectionExtensions.cs, docs/architecture/adr/048-inbox-record-vs-business-transaction.md.
```

</details>

### Phase 6: Atomicity proven on the 10 providers (pull requests 6 and 7)

<details>
<summary>Tasks</summary>

1. Pull request 6, ADO.NET and Dapper (SqlServer, PostgreSQL, MySQL): `tests/Encina.IntegrationTests/Batching/AllOrNothingBatchTestBase.cs` (shared scenarios) plus six classes, each `[Collection("<Family>-<Database>")]`, `[Trait("Category", "Integration")]` and `[Trait("Database", "<Db>")]`, injecting the shared fixture and calling `ClearAllDataAsync()` in `InitializeAsync()`: three items write rows, the third fails, then no row exists and the result flags the first two `WasRolledBack`; all succeed, then all rows exist; a batch inside an existing transaction joins and leaves the commit to the owner.
2. Pull request 7, EF Core (three providers) and MongoDB: the same scenarios on the `EFCore-*` collections and on `MongoDB` (replica set fixture; a standalone server returns `transaction.not_supported` and the batch fails closed, ADR-050).
3. Load and benchmark: justification files `tests/Encina.LoadTests/Batching/RequestBatching.md` and `tests/Encina.BenchmarkTests/Batching/RequestBatching.md` (in pull request 6): the cost is one pipeline per item and is dominated by the handlers, which a micro-benchmark would not isolate; the concurrency (parallel partial success) is covered by the executor unit tests and property tests.
4. Property tests `BatchExecutorPropertyTests` (every item appears exactly once in `Items` in input order; `SuccessCount + FailureCount` equals the item count; `StopOnFirstError` never runs an item after the first failure) in pull request 2 or 4 (pull request 4 here).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: All-or-nothing is implemented and must be proven on real databases of all 10 providers (AGENTS.md section 9: no .md justification for integration tests of database features).
TASK: Add the shared scenarios and the provider test classes in two pull requests.
KEY RULES: Shared [Collection("<Family>-<Database>")] fixtures only, never IClassFixture or a per-class fixture; ClearAllDataAsync in InitializeAsync; never dispose the fixture; the MongoDB class needs the replica-set fixture and asserts the fail-closed result on a standalone server.
REFERENCE FILES: tests/Encina.IntegrationTests (an existing transaction behavior test per provider), docs/testing/integration-tests.md, docs/architecture/adr/050-modules-may-require-modules.md.
```

</details>

### Phase 7: Cross-Cutting Integration and documentation

<details>
<summary>Tasks</summary>

1. OpenTelemetry and logging: Phase 2 task 4 and Phase 1 task 4.
2. Multi-tenancy (function 10): the batch's context, and therefore its tenant, is the context of every item (Phase 1 task 2); a missing tenant fails closed when the marker is present; the tenant tag only when multi-tenancy is on (ADR-054 D4); the reserved `default` segment is not composed into any identifier here.
3. Errors (ADR-062): `batching.batch_too_large` `InvalidRequest`, `batching.tenant_required` `FailedPrecondition`, `batching.item_skipped` `FailedPrecondition`, `batching.item_timeout` `Timeout`, `batching.item_failed` `Internal`, `batching.strategy_not_supported` `InvalidRequest`, `batching.transaction_unavailable` `FailedPrecondition`.
4. Documentation (pull request 6 or a final small pull request): `docs/features/request-batching.md`, `src/Encina.Batching/README.md` and a changelog fragment `changelog.d/349-request-batching.added.md`, `docs/INVENTORY.md` and `ROADMAP.md` lines (the page and README through `docs-writer`, the fragment through `mechanical-fixer`); an ADR for the scoped sender and the transaction boundary: ADR number assigned when the ADR pull request opens.
5. Deferred: an ASP.NET Core batch endpoint (the issue lists `Encina.AspNetCore`, with no contract); issue file `artifacts/issues/aspnetcore-batch-endpoint.md` in this worktree.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: The feature adds no store; integrations are decisions recorded above.
TASK: Verify each integration exists in code and tests, write the documentation through the owning specialists, and confirm the deferred issue file exists.
KEY RULES: Exception type only in logs and tags; never edit CHANGELOG.md; no hand-typed coverage figures in docs.
REFERENCE FILES: AGENTS.md sections 3 and 6, docs/architecture/adr/062-error-category-contract.md, .claude/skills/encina-docs/SKILL.md, changelog.d/README.md.
```

</details>

### Per-flag targets

<details>
<summary>Tasks</summary>

Targets are entered in `.github/coverage-manifest/Encina.Batching.json` (and `Encina.json`, `Encina.Messaging.json` for the seam and the adapter) in the pull request that adds each file; baselines of the edited files `Encina.RequestDispatcher.cs`, `Encina.cs` and `MessagingServiceCollectionExtensions.cs` are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`; each target is the measured value or higher.

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `BatchExecutor.cs` | 92 | 0 | 0 | 80 | 70 |
| `BatchCommandHandler.cs`, `BatchQueryHandler.cs` | 95 | 0 | 0 | 0 | 0 |
| `BatchCommand.cs`, `BatchQuery.cs` | 100 | 90 | 0 | 0 | 0 |
| `BatchBuilder.cs`, builder extension | 95 | 85 | 0 | 0 | 0 |
| `BatchHandlerRegistrar.cs`, `ServiceCollectionExtensions.cs` (edited) | 90 | measured baseline or higher | 0 | 0 | 0 |
| `BatchResult.cs`, `BatchItemResult.cs`, `BatchExecutionOptions.cs` | 100 | 0 | 0 | 0 | 0 |
| `MessagingBatchTransactionBoundary.cs` | 95 | 0 | 0 | 0 | 80 |
| `Encina.RequestDispatcher.cs`, `Encina.cs` (edited) | measured baseline or higher | measured baseline or higher | 0 | 0 | 0 |
| `IScopedRequestSender.cs`, `IBatchExecutor.cs`, `IBatchTransactionBoundary.cs`, `BatchStrategy.cs` | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| `BatchExecutor.cs` unit 92, property 80, integration 70 | Strategies, timeouts, parallel degree and skipped items run with fakes; the property test covers ordering and counts; the integration tests (10 providers) reach the transaction path through the real boundary and miss defensive catch blocks. Guard 0: the type is internal, with no `InternalsVisibleTo` for `Encina.GuardTests` (verified at work start; raised if present). |
| Handlers unit 95 | Thin classes whose branches run in unit tests; guard 0 for the internal-type reason. |
| `BatchCommand.cs`, `BatchQuery.cs` unit 100, guard 90 | Public records with a null check in the constructor, fully reached by guard tests. |
| Builder unit 95, guard 85 | Every method runs against a fake `IEncina`; guard tests reach the null and empty checks. |
| Registrar and DI unit 90 | The scan and both registration orders run in the DI test; the remainder is assembly-loading error handling. |
| Result types and options unit 100 | Pure data and validation. |
| Adapter unit 95, integration 80 | A thin class over the lease; integration proves it on the providers. Guard 0: internal type. |
| Edited core files: baseline or higher | The change is a small refactor; the existing tests cover it, and the new seam has its own tests, so the targets are the measured baselines. |
| Interfaces and the enum 0 in every flag | No executable lines; contract tests arrive with a second implementer of `IBatchTransactionBoundary` (the Messaging adapter has the only one, tested in its own class). |

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` section 3 | ROP, fail-closed gates, exception messages never in logs, registration completeness | D2, D4, Phase 2 |
| `AGENTS.md` sections 5 and 9 | 10-provider matrix and integration tests on real databases | Phase 6 |
| ADR-048, ADR-050 | Scoped transaction accessor and lease; MongoDB transactions need a replica set | D4, D7, Phase 6 |
| ADR-021, ADR-054, ADR-062 | EventIds, telemetry conventions with the tenant tag, error categories | Phases 1, 2, 7 |
| Enterprise Integration Patterns: Aggregator; MassTransit batch consumers (named by the issues) | Comparable batch handling | Issue references |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ICommand<T>`, `IQuery<T>`, `ICommandHandler<,>`, `IQueryHandler<,>` | `src/Encina/Abstractions/CqrsContracts.cs` | Wrapper and handler contracts |
| `IEncina.Send`, `Encina.RequestDispatcher` | `src/Encina/Abstractions/IEncina.cs`, `src/Encina/Dispatchers/Encina.RequestDispatcher.cs` | Per-item pipeline; the scope creation the seam changes |
| `ITransactionLeaseSource`, `ITransactionLease`, `IDbTransactionAccessor` | `src/Encina.Messaging/Transactions/` | The transaction the boundary adapts |
| `TransactionPipelineBehavior` (EF Core, Messaging) | `src/Encina.EntityFrameworkCore/`, `src/Encina.Messaging/` | Item behaviors that join the batch lease |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Fail-closed tenant check |
| `Encina.Batching` package, `AddEncinaBatching`, `Batching` range | #302 | Package, registration, EventIds |
| `MediatorAssemblyScanner` | `src/Encina/Dispatchers/MediatorAssemblyScanner.cs` | Scan style for D6 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Batching` (`Batching`, registered by #302) | 7758-7765, all eight used | Inside the assigned block 7750-7799; #302 uses 7750-7757, #339 7766-7769 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 24 | 15 in `Encina.Batching` (types, options, executor, handlers, builder, registrar, boundary, log, telemetry), 4 in core (`IScopedRequestSender`, `Encina.cs`, `Encina.RequestDispatcher.cs`, core `ServiceCollectionExtensions.cs`), 5 in Messaging, EF Core and MongoDB registrations |
| Public API and manifest | 8 | PublicAPI for `Encina.Batching`, core and Messaging; manifests `Encina.Batching.json`, `Encina.json`, `Encina.Messaging.json` |
| Tests | 24 | unit 10, guard 4, property 1, DI 2, integration 7 (1 base, 6 provider classes in PR 6; 4 provider classes in PR 7) |
| Justification files | 2 | load, benchmark |
| Documentation | 5 | feature page, README edit, changelog fragment, inventory, roadmap |
| **Pull request 1** | **14** | 9 production, PublicAPI, manifest, 3 tests |
| **Pull request 2** | **14** | 5 production (query, query handler, builder with extension, registrar, telemetry), the `ServiceCollectionExtensions` edit, PublicAPI, manifest, 4 tests, README edit |
| **Pull request 3** | **11** | 4 core production, core PublicAPI, `Encina.json`, 3 tests (sender unit, sender guard, edited dispatcher test) |
| **Pull request 4** | **10** | 2 contracts, executor edit, log edit, PublicAPI, manifest, 4 tests (all-or-nothing unit, guard, property, handler test edit) |
| **Pull request 5** | **8** | Messaging csproj, adapter, 3 registration edits (Messaging, EF Core, MongoDB), PublicAPI for `Encina.Messaging`, manifest, 2 tests (unit and DI) |
| **Pull request 6** | **9** | integration base, 6 ADO.NET and Dapper classes, 2 justification files |
| **Pull request 7** | **6** | 4 EF Core and MongoDB classes, feature page, changelog fragment |

Callers and implementers, by grep on `origin/main` (2026-11-17): nothing implements or calls the new types. The seam is a new interface, so no `IEncina` implementer changes (the implementers are the `Encina` class and test doubles, found by `git grep ": IEncina"` at work start). Each pull request adds production code only with its own tests and builds alone in the order 1, 2, 3, 4, 5, 6, 7; pull request 1 depends on #302 pull request 2 (the `Batching` EventId range) and pull request 4 on pull request 3.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP. IEncina.Send creates a DI scope per request, IEncina has no service access, and transactions are leased per scope through ITransactionLeaseSource (ADR-048). #302 provides Encina.Batching, AddEncinaBatching and the Batching EventId range.

IMPLEMENTATION OVERVIEW: PR1 wrappers, BatchStrategy (AllOrNothing, PartialSuccess, StopOnFirstError), BatchResult with BatchItemResult, BatchExecutor for partial success and stop on first error, BatchCommandHandler, log 7758-7765; PR2 queries, assembly-scan registration, the builder that sends the wrapper, telemetry; PR3 the core seam IScopedRequestSender; PR4 all-or-nothing through IBatchTransactionBoundary, items sent in the batch's scope, commit once; PR5 the Messaging adapter over ITransactionLeaseSource registered wherever the lease source is; PR6 and PR7 integration tests on all 10 providers. Dilemmas D1, D4, D5 and D6 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Items keep their pipelines and run with the batch's context; AllOrNothing is sequential and fails closed without a boundary; a failure of the all-or-nothing batch returns Right with WasRolledBack flags; never claim atomicity beyond the shared transaction (nothing here calls SaveChangesAsync); exception type only in logs; ADR-062 category on every code; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina/Dispatchers/Encina.RequestDispatcher.cs, src/Encina/Abstractions/IEncina.cs, src/Encina.Messaging/Transactions/ITransactionLeaseSource.cs, src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs, .github/coverage-manifest/Encina.Batching.json, docs/testing/integration-tests.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Caching batch query results is a separate optimization (the issue defers it); it needs its own issue after the cache family settles keys, since a batch result cannot be cached as one entry without per-item keys. |
| 2 | OpenTelemetry | ✅ | `Activity` per batch and counters with strategy and outcome tags on the `Encina.Batching` source; tenant tag only when multi-tenancy is on (ADR-054 D4). |
| 3 | Structured Logging | ✅ | `BatchExecutionLog`, EventIds 7758-7765 in the registered `Batching` range; error codes only, never messages. |
| 4 | Health Checks | ❌ | An in-process request wrapper with no dependency of its own; the stores it uses have their own health checks. |
| 5 | Validation | ✅ | Each item runs through its own validation behaviors (D2); the batch size is validated by the executor and the options by `ValidateOnStart`. |
| 6 | Resilience | ⏭️ | Per-item timeout is included; retry of failed items is a policy that belongs to the resilience family and is not added here. |
| 7 | Distributed Locks | ❌ | A batch is one request in one process; locking batches across instances is not part of this feature. |
| 8 | Transactions | ✅ | `AllOrNothing` through the scoped sender and `IBatchTransactionBoundary` (D4); the executor is the commit owner; proven on the 10 providers. |
| 9 | Idempotency | ⏭️ | Deduplication of items inside a batch is #339; batch-level idempotency keys go through the existing inbox behavior of the wrapper request. |
| 10 | Multi-Tenancy | ✅ | Every item runs under the batch's tenant; a missing tenant fails closed when the marker is present; no identifier composes a tenant into a string. |
| 11 | Module Isolation | ⏭️ | Module-scoped batch handlers need the module family's context (#747 covers messaging entities); no module identifier is used here. |
| 12 | Audit Trail | ⏭️ | The wrapper request passes the audit behaviors once and each item passes them individually; a dedicated bulk-operation audit record belongs to the audit family (#749). |

---

## Prerequisites & Dependencies

- **#302, pull requests 1 and 2**: the `Encina.Batching` package, `AddEncinaBatching`, `BatchingOptions` conventions and the `Batching` EventId range (registered in #302 pull request 2). Pull request 1 of this plan waits for them; its placement follows #302's D1.
- **Pull requests 4 to 7** depend on pull request 3 (the core seam) and, for the proofs, on `ITransactionLeaseSource` as registered today by Messaging, EF Core and MongoDB.
- **Order inside the family**: #302, #349, #339, #121 (#252 is independent).
- **Related, not blocking**: #339 (deduplication on top of the executor), the audit family (#749), the idempotency family (#733).

---

## Next Steps

1. Ask the maintainer D1, D4, D5 and D6 when work on #349 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: types, executor and command handler (14 files), after #302 pull request 2.
3. Pull requests 2 and 3: queries, registration and builder (14 files); the core seam (11 files).
4. Pull requests 4 and 5: all or nothing (10 files) and the Messaging adapter (8 files).
5. Pull requests 6 and 7: provider integration tests (9 and 6 files) with the documentation.
