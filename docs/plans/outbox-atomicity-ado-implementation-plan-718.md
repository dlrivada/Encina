# Implementation Plan: Outbox Transaction Atomicity for ADO.NET (and Dapper) Providers

> **Issue**: [#718](https://github.com/dlrivada/Encina/issues/718) (ADO.NET); Dapper twin [#719](https://github.com/dlrivada/Encina/issues/719)
> **Type**: Feature
> **Complexity**: High (10 phases, 6 database providers in this plan, shared change in `Encina.Messaging`)
> **Estimated Scope**: ~1,300-1,800 lines of production code + ~2,500-3,200 lines of tests

---

## Summary

Make the outbox write and the business change of a request commit or roll back together on the ADO.NET providers (SqlServer, PostgreSQL, MySQL), and, as recommended below, on their Dapper twins (#719). A committed command leaves exactly one outbox row per notification; a rolled-back command, a `Left` result or a failed outbox write leaves none and fails the request.

### What the code does today (2026-10-06, `main` at `5b485b12`)

The issue assumes `OutboxStoreADO` only needs to "join the ambient transaction". The code shows four separate defects; the first one exists on every provider:

1. **The outbox write runs after the transaction is gone.** `OutboxPostProcessor` is an `IRequestPostProcessor` (`src/Encina.Messaging/Outbox/OutboxPostProcessor.cs:14`, registered at `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:265`). `PipelineBuilder` wraps the behaviors first and the pre/post-processors outermost (`src/Encina/Pipeline/PipelineBuilder.cs:83`); post-processors run after `terminal()` returns (`PipelineBuilder.cs:112-114`). `TransactionPipelineBehavior` keeps its transaction in a local variable and commits and disposes it inside `terminal()` (`src/Encina.Messaging/TransactionPipelineBehavior.cs:81-115`). The outbox row is therefore always written in a separate, later statement: business committed + outbox failed = **lost event**.
2. **The outbox failure is swallowed.** `OutboxPostProcessor.Process` discards the `Either` returned by `_outboxStore.AddAsync` (`OutboxPostProcessor.cs:95`) and by `SaveChangesAsync` (`OutboxPostProcessor.cs:98`), and `IRequestPostProcessor.Process` returns `Task`, so the request still reports success. This breaks AGENTS.md §3 ("Errors are NEVER swallowed").
3. **No store can see the transaction.** `TransactionPipelineBehavior`'s transaction is a local (`TransactionPipelineBehavior.cs:81`); `UnitOfWorkADO.CurrentTransaction` is `internal` (`src/Encina.ADO.SqlServer/UnitOfWork/UnitOfWorkADO.cs:91`) and only `UnitOfWorkRepositoryADO` reads it (`UnitOfWorkRepositoryADO.cs:879`). `OutboxStoreADO` builds commands without `command.Transaction` (`src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:116`), and `OutboxStoreDapper` calls `ExecuteAsync` without a transaction (`src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs:56`). On SqlClient and MySqlConnector a command without the pending transaction on a connection that has one throws `InvalidOperationException`; Npgsql runs it in the connection's transaction. Behaviour therefore differs per provider today.
4. **Two transaction owners on one connection.** `UnitOfWorkADO.BeginTransactionAsync` returns `TransactionAlreadyActive` only for its own transaction (`UnitOfWorkADO.cs:145-148`) and does not know about `TransactionPipelineBehavior`'s transaction on the same scoped `IDbConnection` (`src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:290`). SqlServer and MySQL also begin and commit synchronously (`UnitOfWorkADO.cs:153,175`; `src/Encina.ADO.MySQL/UnitOfWork/UnitOfWorkADO.cs:161,183`), a defect tracked by #1418.

Nothing of #718 or #719 is implemented: there is no transaction accessor anywhere in `src/` and no integration test asserts outbox rollback (`tests/Encina.IntegrationTests/ADO/*/Outbox/OutboxStoreADOTests.cs` only test the store in isolation).

### Should one plan cover #718 and #719?

**Yes (recommended, Design Choice 1).** Both families use the same scoped `IDbConnection` registration, the same shared `Encina.Messaging.TransactionPipelineBehavior` and the same `OutboxPostProcessor`; the core of the fix (defects 1-3) lives in `Encina.Messaging` and is identical. Only the provider phase differs (Phase 5 for ADO.NET, Phase 6 for Dapper), so #719 can be closed by the same PR or by a second PR that executes Phase 6. EF Core and MongoDB share defect 1 and 2 but need a different transaction mechanism; they are proposed as a follow-up issue (`artifacts/issues/plan-718-outbox-atomicity-efcore-mongodb.md`) and overlap with #1236.

### Scope

- **Affected packages**: `Encina.Messaging` (transaction accessor, outbox behavior, `TransactionPipelineBehavior`, DI), `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, and (Phase 6) `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`.
- **Provider category**: Database. Six of the ten providers in this plan (ADO.NET ×3, Dapper ×3); EF Core ×3 and MongoDB in the follow-up issue. SQLite is out of the matrix (ADR-024): the issue's "4 providers" predates that decision.
- **Estimated files**: ~25 production files touched or created, ~35 test files.

---

## Design Choices

<details>
<summary><strong>1. Plan scope — ADO.NET and Dapper together, EF Core and MongoDB in a follow-up</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) ADO.NET only (#718 as written)** | Smallest PR; matches the issue literally | The `Encina.Messaging` change (outbox behavior, accessor) lands with only half its consumers; Dapper keeps the broken post-processor path, so the two families diverge (AGENTS.md §5 provider coherence) |
| **B) ADO.NET + Dapper in one plan (#718 + #719)** | One shared core, two thin provider phases; both families switch at once; #719 needs no separate plan | Larger PR (or two PRs in sequence); Dapper reviewers must read an ADO-named plan |
| **C) All 10 providers now** | Full coherence; removes `OutboxPostProcessor` everywhere | EF Core needs a `DbContext` transaction and MongoDB a client session: different mechanisms, a much larger change, and it overlaps #1236 (domain events to the outbox on all 10 providers), which has its own plan pending |

### Chosen Option: **B — ADO.NET and Dapper together** (recommended, pending the maintainer)

### Rationale

- I recommend B because the root cause sits in `Encina.Messaging`, which both families consume through `AddMessagingServices` (`src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:69`, `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs:69`); fixing it for one family only would leave the other family on a known-broken path.
- EF Core and MongoDB are not covered by #718/#719 and need their own transaction mechanism; a follow-up issue keeps them visible without blocking this work. Until it lands they keep `OutboxPostProcessor`, with defect 2 (swallowed errors) fixed here so that no provider silently loses an outbox write.
- #719 should be linked to this plan and closed by the PR that completes Phase 6.

</details>

<details>
<summary><strong>2. Where the outbox write runs — a pipeline behavior that joins or begins the transaction</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TransactionalOutboxPipelineBehavior` that joins the ambient transaction or begins its own** | Works with or without `UseTransactions`; independent of behavior registration order; a failed write returns `Left` and rolls back; requests without `IHasNotifications` skip it at zero cost | New public behavior; ADO/Dapper stop using `OutboxPostProcessor`, so two outbox paths exist until the EF Core/MongoDB follow-up |
| **B) Pre-commit participants invoked by `TransactionPipelineBehavior`** (`ITransactionParticipant`) | Single transaction owner; generic hook usable by other patterns | Outbox atomicity silently depends on `UseTransactions = true`; with it off, nothing writes the outbox at all or the write is non-atomic |
| **C) Change `PipelineBuilder` so post-processors run inside the behaviors** | Keeps `OutboxPostProcessor` unchanged | Core semantic change for every post-processor of every application; post-processors still cannot fail the request (`Task` return type) |
| **D) Explicit only: handlers call `OutboxOrchestrator.AddAsync` inside their unit of work** | No pipeline magic | `IHasNotifications` stops working; every handler must remember; errors easy to drop |

### Chosen Option: **A — `TransactionalOutboxPipelineBehavior` with join-or-begin semantics** (recommended, pending the maintainer)

### Rationale

- I recommend A because it is the only option where atomicity holds whatever the configuration: when a transaction is already active (from `TransactionPipelineBehavior` or `IUnitOfWork`) the behavior joins it; otherwise it begins one around the handler and the outbox write.
- Join semantics make the behavior order-independent, which matters because `RegisterOutbox` runs before `RegisterTransactions` (`MessagingServiceCollectionExtensions.cs:60-66`), so the outbox behavior would otherwise sit outside the transaction behavior.
- It returns `Either`, so a failed outbox write fails the request and rolls back the business change (fail closed, AGENTS.md §3), unlike a post-processor.
- Pay-for-what-you-use: a static per-`TRequest` check of `IHasNotifications` plus a runtime check short-circuits to `nextStep()` without opening a transaction, which satisfies the issue's "no performance regression for non-outbox operations".

</details>

<details>
<summary><strong>3. Transaction-sharing abstraction — a scoped <code>IDbTransactionAccessor</code> with ownership leases</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Scoped `IDbTransactionAccessor` (current transaction + `BeginOrJoinAsync` returning an `IDbTransactionLease`)** | One owner per scope; every participant (`TransactionPipelineBehavior`, `UnitOfWorkADO/Dapper`, outbox behavior, stores, user handlers) reads the same transaction; explicit, testable, async with `CancellationToken` | Every store must set `command.Transaction` (or pass `transaction:` to Dapper); new public API |
| **B) Make `UnitOfWork*.CurrentTransaction` public and make stores depend on `IUnitOfWork`** | Reuses an existing type | `IUnitOfWork` is optional (AGENTS.md §3, repository pattern never forced); `TransactionPipelineBehavior` would have to go through a UoW; stores gain a dependency on DomainModeling's UoW |
| **C) `System.Transactions.TransactionScope` with `TransactionScopeAsyncFlowOption.Enabled`** | Ambient, no plumbing; the issue's word "ambient" | Enlistment differs per driver (MySqlConnector and Npgsql need `Enlist=true`; escalation to distributed transactions when a second connection opens); hidden behaviour; no `Either` |
| **D) `IDbConnection` decorator that assigns the active transaction to every command it creates** | Handler code and every store enlist automatically | Breaks `is SqlConnection` / `is SqlCommand` fast paths (`OutboxStoreADO.cs:349-376`, `UnitOfWorkADO.cs:251`) and `SqlBulkCopy`; stacks with `SchemaValidatingConnection` (module isolation); hard to reason about |

### Chosen Option: **A — scoped `IDbTransactionAccessor` with leases** (recommended, pending the maintainer)

### Rationale

- I recommend A because it is explicit and works the same on SqlClient, Npgsql and MySqlConnector: every command that must be atomic carries the transaction, which removes the per-driver difference described in the Summary (defect 3).
- The lease models ownership: the first `BeginOrJoinAsync` in a scope owns the transaction and is the only one that commits; joiners get a non-owning lease whose `CommitAsync` is a no-op and whose `RollbackAsync` marks the transaction rollback-only (Design Choice 5).
- The issue proposed the name `ITransactionAccessor`; `IDbTransactionAccessor` is recommended because the contract is `IDbConnection`/`IDbTransaction`-bound and EF Core/MongoDB will need a different mechanism.
- Shape (namespace `Encina.Messaging.Transactions`):

```csharp
public interface IDbTransactionAccessor
{
    IDbConnection Connection { get; }
    IDbTransaction? Current { get; }
    ValueTask<Either<EncinaError, IDbTransactionLease>> BeginOrJoinAsync(
        IsolationLevel? isolationLevel = null,
        CancellationToken cancellationToken = default);
}

public interface IDbTransactionLease : IAsyncDisposable
{
    bool IsOwner { get; }
    IDbTransaction Transaction { get; }
    ValueTask<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default);
    ValueTask RollbackAsync(CancellationToken cancellationToken = default);
}
```

</details>

<details>
<summary><strong>4. Placement — <code>Encina.Messaging</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Messaging` (`Transactions/` folder)** | `TransactionPipelineBehavior`, `IOutboxStore` and `AddMessagingServices` already live there; every ADO.NET and Dapper package already references it | Couples a data-access concept to the messaging package |
| **B) `Encina.DomainModeling` (next to `IUnitOfWork`)** | Next to the UoW contract | `Encina.Messaging` would need a new reference to DomainModeling for the behaviors; DomainModeling is about the domain model, not `IDbConnection` |
| **C) New `Encina.Data.Abstractions` package** | Clean separation | A new package for two interfaces and one class; more DI ceremony; pre-1.0 package count already high |

### Chosen Option: **A — `Encina.Messaging`** (recommended, pending the maintainer)

### Rationale

- I recommend A because the consumers (`TransactionPipelineBehavior`, the new outbox behavior, the messaging stores) are already in or reference `Encina.Messaging`; no new project reference is needed in any of the six provider packages.
- The provider-agnostic contract stays in the abstraction package and the providers only consume it (AGENTS.md §3 provider coherence).

</details>

<details>
<summary><strong>5. Nested transactions — join with rollback-only, single owner</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `TransactionAlreadyActive` errors** | No new semantics | `IUnitOfWork.BeginTransactionAsync` inside a handler always fails once `UseTransactions` or the outbox behavior opened a transaction; users cannot combine the patterns |
| **B) Join: inner participants share the owner's transaction; inner commit is a no-op; inner rollback marks the transaction rollback-only; the owner's commit then rolls back and returns `Left`** | Patterns compose (UoW + `UseTransactions` + outbox); a failure anywhere fails closed | Inner `CommitAsync` returning success does not mean data is durable; must be documented |
| **C) Savepoints for inner scopes** | Inner rollback undoes only its part | Savepoint syntax and support differ (`SAVE TRANSACTION` / `SAVEPOINT`); partial rollback contradicts the outbox guarantee (business change and outbox row must go together) |

### Chosen Option: **B — join with rollback-only and a single owner** (recommended, pending the maintainer)

### Rationale

- I recommend B because the outbox guarantee is all-or-nothing for the whole request; savepoints would allow a committed business change whose inner part rolled back.
- Rollback-only makes a swallowed inner failure impossible: the owner's commit returns `Left(transaction.rollback_only)` instead of committing.
- `UnitOfWorkADO`/`UnitOfWorkDapper.HasActiveTransaction` keeps its meaning ("this unit of work holds a lease"), and the `TransactionAlreadyActive` error stays only for a second `BeginTransactionAsync` on the same unit of work.

</details>

<details>
<summary><strong>6. Enlistment scope — the four messaging stores now, other ADO/Dapper stores in a follow-up</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Outbox store only** | Minimal | On SqlClient/MySqlConnector, any inbox, saga or scheduled-message call made inside a handler then throws because the connection has a pending transaction the command does not carry |
| **B) The four messaging stores (Outbox, Inbox, Saga, ScheduledMessage) + the UoW repositories** | Covers every store a handler typically calls inside a messaging request; one shared helper per family | Audit, anonymization, ABAC and read/write-separation connections stay outside (tracked by a follow-up issue) |
| **C) Every ADO/Dapper store on the scoped connection** | Complete | ~10 more stores per provider family; mixes this feature with audit and compliance stores whose own atomicity rules (e.g. audit must persist on rollback) need a separate decision |

### Chosen Option: **B — the four messaging stores and the UoW repositories** (recommended, pending the maintainer)

### Rationale

- I recommend B because it makes the messaging patterns compose inside one transaction on all three drivers without deciding, in this issue, whether audit records should survive a business rollback (they arguably should not share the transaction).
- The remaining stores are listed in `artifacts/issues/plan-718-enlist-remaining-stores.md` so the gap is recorded, not forgotten.

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions — `IDbTransactionAccessor` and leases

> **Goal**: One scoped owner of the ADO.NET transaction per DI scope, with async begin/commit/rollback and join semantics.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Messaging/Transactions/`

1. **`IDbTransactionAccessor.cs`** — public interface (Design Choice 3): `IDbConnection Connection { get; }`, `IDbTransaction? Current { get; }`, `ValueTask<Either<EncinaError, IDbTransactionLease>> BeginOrJoinAsync(IsolationLevel? isolationLevel = null, CancellationToken cancellationToken = default)`.
2. **`IDbTransactionLease.cs`** — public interface: `bool IsOwner`, `IDbTransaction Transaction`, `ValueTask<Either<EncinaError, Unit>> CommitAsync(CancellationToken)`, `ValueTask RollbackAsync(CancellationToken)`, `IAsyncDisposable` (owner disposal without commit rolls back).
3. **`DbTransactionAccessor.cs`** — `public sealed class DbTransactionAccessor : IDbTransactionAccessor, IAsyncDisposable`
   - Constructor: `(IDbConnection connection, ILogger<DbTransactionAccessor> logger, TimeProvider? timeProvider = null)`.
   - Opens the connection with `DbConnection.OpenAsync(ct)` when closed; falls back to `Task.Run(connection.Open, ct)` only for a non-`DbConnection` (same pattern as `src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs:259-266`, the #1325 fix).
   - Begins with `DbConnection.BeginTransactionAsync(isolationLevel, ct)`.
   - Tracks `_owner` lease, `_depth`, `_rollbackOnly`; joiners receive a non-owning lease. A joiner asking for a different isolation level gets `Left(TransactionErrors.IsolationLevelMismatch)`.
   - Owner `CommitAsync`: if `_rollbackOnly`, roll back and return `Left(TransactionErrors.RollbackOnly)`; else `DbTransaction.CommitAsync(ct)`; on exception roll back and return `Left(TransactionErrors.CommitFailed)` (error code only, never the message, AGENTS.md §3).
   - Commit and rollback of the owner use `CancellationToken.None` for cleanup paths (same reasoning as `TransactionPipelineBehavior.cs:23-27`).
4. **`DbTransactionLease.cs`** — `internal sealed class` implementing `IDbTransactionLease`.
5. **`TransactionErrors.cs`** — `public static class TransactionErrors` with codes `transaction.begin_failed`, `transaction.commit_failed`, `transaction.rollback_only`, `transaction.isolation_level_mismatch`, `transaction.lease_disposed`; factory methods returning `EncinaError`.
6. **`PublicAPI.Unshipped.txt`** (`src/Encina.Messaging/`) — all new public symbols.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #718 (outbox atomicity for ADO.NET, Dapper twin #719) in Encina.

CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming (Either<EncinaError, T>), nullable enabled.
- Today no component shares the ADO.NET transaction: TransactionPipelineBehavior keeps it in a local variable
  (src/Encina.Messaging/TransactionPipelineBehavior.cs:81) and UnitOfWorkADO exposes it only internally
  (src/Encina.ADO.SqlServer/UnitOfWork/UnitOfWorkADO.cs:91).
- You create the scoped transaction owner that later phases use: IDbTransactionAccessor, IDbTransactionLease,
  DbTransactionAccessor, TransactionErrors, in src/Encina.Messaging/Transactions/ (namespace Encina.Messaging.Transactions).

TASK:
Create the five files listed in Phase 1 Tasks of docs/plans/outbox-atomicity-ado-implementation-plan-718.md and add every
public symbol to src/Encina.Messaging/PublicAPI.Unshipped.txt.

KEY RULES:
- Join semantics: the first BeginOrJoinAsync in a scope owns the transaction; later calls get a non-owning lease.
  Non-owner CommitAsync is a no-op returning Right; non-owner RollbackAsync marks the transaction rollback-only;
  owner CommitAsync on a rollback-only transaction rolls back and returns Left(transaction.rollback_only).
- Async only: DbConnection.OpenAsync / BeginTransactionAsync / DbTransaction.CommitAsync / RollbackAsync with the
  CancellationToken; never IDbConnection.Open() or BeginTransaction() directly (AGENTS.md section 3). The only fallback
  for a non-DbConnection is Task.Run, as in src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs:259-266.
- Errors carry the code and the exception type only; EncinaError.Message never reaches logs.
- Disposing the owner lease without commit rolls back; disposing the accessor rolls back any open transaction.
- XML docs on every public type and member, with an <example> on IDbTransactionAccessor.
- No [Obsolete], no compatibility shims.

REFERENCE FILES:
- src/Encina.Messaging/TransactionPipelineBehavior.cs (commit/rollback with CancellationToken.None)
- src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs (async begin/commit/rollback after #1325)
- src/Encina.DomainModeling/IUnitOfWork.cs (UnitOfWorkErrors pattern)
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (OutboxErrorCodes pattern)
```

</details>

---

### Phase 2: `TransactionPipelineBehavior` on the accessor

> **Goal**: The shared ADO.NET/Dapper transaction behavior becomes a lease holder, so every participant sees its transaction.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Messaging/TransactionPipelineBehavior.cs`**
   - Constructor: `(IDbTransactionAccessor transactionAccessor, ILogger<TransactionPipelineBehavior<TRequest, TResponse>> logger)` (replaces the `IDbConnection` parameter).
   - `Handle`: `BeginOrJoinAsync` → `nextStep()` → on `Right` `lease.CommitAsync` (a `Left` from commit becomes the response) → on `Left` / exception `lease.RollbackAsync`; `await using` the lease.
   - Call the existing, unused `MessagingLog.TransactionStarted/Committed/RolledBack` (EventIds 2834-2836, `src/Encina.Messaging/MessagingLog.cs:203-231`); log `ex.ForLogging()` on the exception path. This closes #1342's logging finding.
   - Keep `catch (OperationCanceledException)` → rollback and rethrow; other exceptions → rollback and `EncinaErrors.FromException("transaction.failed", ex)`.
2. **Update guard tests** in `tests/Encina.GuardTests/{ADO,Dapper}/{SqlServer,PostgreSQL,MySQL}/TransactionPipelineBehaviorGuard*Tests.cs` to the new constructor (consider consolidating them per #1340).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #718 in Encina: TransactionPipelineBehavior on IDbTransactionAccessor.

CONTEXT:
- Phase 1 added IDbTransactionAccessor / IDbTransactionLease in src/Encina.Messaging/Transactions/.
- src/Encina.Messaging/TransactionPipelineBehavior.cs begins a transaction on IDbConnection and keeps it local, so no
  store or handler can join it; it also never calls the MessagingLog methods declared for it (EventIds 2834-2836).

TASK:
Rewrite TransactionPipelineBehavior<TRequest, TResponse> to depend on IDbTransactionAccessor and ILogger, take a lease
with BeginOrJoinAsync, commit on Right (a Left from commit replaces the response), roll back on Left or exception, and
log start/commit/rollback through MessagingLog. Update the six guard-test files for the new constructor.

KEY RULES:
- The behavior never begins a second transaction: when one is active it joins (non-owning lease).
- Commit and rollback in cleanup paths use CancellationToken.None.
- Exceptions passed to the logger go through ex.ForLogging(); never log EncinaError.Message.
- Unit tests first (tests/Encina.UnitTests/Messaging/Behaviors/TransactionPipelineBehaviorTests.cs): owner commit,
  joiner no-op, Left -> rollback, exception -> rollback + Left, cancellation -> rollback + rethrow, commit Left surfaces.
- Keep CRAP <= 10 on changed methods: extract the result-handling into small methods.

REFERENCE FILES:
- src/Encina.Messaging/TransactionPipelineBehavior.cs
- src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs (logging and reuse-existing-transaction pattern)
- src/Encina.Messaging/MessagingLog.cs (lines 203-231)
- tests/Encina.UnitTests/Messaging/Behaviors/TransactionPipelineBehaviorTests.cs
```

</details>

---

### Phase 3: Transactional outbox pipeline behavior

> **Goal**: Notifications of `IHasNotifications` requests are written inside the request's transaction; a failed write fails the request.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Outbox/OutboxWriter.cs`** — `internal sealed class OutboxWriter` extracted from `OutboxPostProcessor.Process` (`OutboxPostProcessor.cs:63-100`): serializes each notification through `IMessageSerializer.SerializeAsRuntimeType`, creates messages with `IOutboxMessageFactory` and `TimeProvider`, calls `IOutboxStore.AddAsync` and `SaveChangesAsync`, and **returns the first `Left`** (`ValueTask<Either<EncinaError, int>> WriteAsync(IReadOnlyList<INotification>, IRequestContext, CancellationToken)`).
2. **`src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs`** — `public sealed class TransactionalOutboxPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>`
   - Constructor: `(IDbTransactionAccessor transactionAccessor, IOutboxStore outboxStore, IOutboxMessageFactory messageFactory, IMessageSerializer messageSerializer, ILogger<TransactionalOutboxPipelineBehavior<TRequest, TResponse>> logger, TimeProvider? timeProvider = null)`.
   - Static per-generic-type flag `private static readonly bool MayHaveNotifications = typeof(IHasNotifications).IsAssignableFrom(typeof(TRequest)) || typeof(TRequest).IsInterface || typeof(TRequest).IsAbstract;` plus the runtime `request is IHasNotifications` check; otherwise `return await nextStep()`.
   - Flow: `BeginOrJoinAsync` → `nextStep()` → on `Left` roll back and return it (no outbox row) → read `GetNotifications()` after the handler ran → `OutboxWriter.WriteAsync` → on `Left` roll back and return the outbox error → `lease.CommitAsync` → return the handler's response or the commit `Left`.
3. **Modify `src/Encina.Messaging/Outbox/OutboxPostProcessor.cs`** (still used by EF Core and MongoDB until the follow-up issue): delegate to `OutboxWriter` and stop discarding its result: log the error code (new `[LoggerMessage]`) and throw nothing; add an XML `<remarks>` saying this path is not atomic and naming the follow-up issue. A post-processor cannot change the response (`IRequestPostProcessor.Process` returns `Task`, `src/Encina/Abstractions/IRequestPostProcessor.cs`), so logging plus a metric is the strongest signal available there.
4. **`src/Encina.Messaging/Outbox/OutboxErrorCodes`** (`OutboxOrchestrator.cs:269`) — add `outbox.write_failed` if `outbox.add_failed` does not fit the batch case.
5. **`PublicAPI.Unshipped.txt`** — the behavior.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #718 in Encina: the transactional outbox pipeline behavior.

CONTEXT:
- OutboxPostProcessor (src/Encina.Messaging/Outbox/OutboxPostProcessor.cs) runs after every behavior, because
  PipelineBuilder wraps post-processors outermost (src/Encina/Pipeline/PipelineBuilder.cs:83, 112-114). The transaction
  is already committed when it writes, and it discards the Either of AddAsync (line 95) and SaveChangesAsync (line 98).
- Phase 1 added IDbTransactionAccessor; Phase 2 made TransactionPipelineBehavior a lease holder.

TASK:
1. Extract the serialization + store calls of OutboxPostProcessor into internal OutboxWriter that returns the first Left.
2. Create TransactionalOutboxPipelineBehavior<TRequest, TResponse> (join-or-begin lease, handler, write notifications,
   commit; any Left rolls back and is returned).
3. Make OutboxPostProcessor use OutboxWriter and log (not swallow silently) a failed write; document that it is the
   non-atomic path still used by EF Core and MongoDB.

KEY RULES:
- Requests that cannot implement IHasNotifications skip the behavior with zero allocations (static per-type flag).
- Notifications are read after the handler returns (handlers add them while running).
- A Left from the handler means no outbox row and a rollback; a Left from the outbox write or the commit is returned
  to the caller (fail closed, AGENTS.md section 3).
- Serialization stays on IMessageSerializer.SerializeAsRuntimeType (#1168, encryption decorators).
- Never log EncinaError.Message; log the error code.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxPostProcessor.cs
- src/Encina.Messaging/Inbox/ (InboxPipelineBehavior: behavior shape and static caching)
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (OutboxErrorCodes)
- tests/Encina.UnitTests/Messaging/Pipeline/OutboxPostProcessorTests.cs
```

</details>

---

### Phase 4: Configuration & DI

> **Goal**: ADO.NET and Dapper registrations wire the accessor and the transactional outbox behavior; EF Core and MongoDB keep the post-processor.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`**
   - `AddMessagingServices<...>` (line 44, used only by ADO.NET and Dapper, see the remarks at line 189) and `AddMessagingServicesCore<...>` (line 358, used by the ADO.NET tenancy registrations in `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Tenancy/TenancyServiceCollectionExtensions.cs`, which also call `RegisterTransactions` at line 382): `services.TryAddScoped<IDbTransactionAccessor, DbTransactionAccessor>()` whenever Outbox, Transactions, Inbox, Sagas or Scheduling is on. Both entry points must get the same outbox write path.
   - When `UseOutbox`: remove the `OutboxPostProcessor<,>` open-generic registration added by `RegisterOutbox` (line 265) and add `TransactionalOutboxPipelineBehavior<,>`. Prefer splitting `RegisterOutbox` into a store/processor part and a "write path" part so nothing needs removing.
   - `AddOutboxInboxSagaSchedulingServices` (EF Core, MongoDB) keeps registering `OutboxPostProcessor<,>`.
2. **DI completeness tests** (AGENTS.md §3 "Registration completeness"): for each of the six providers, build the provider with `ValidateOnBuild = true, ValidateScopes = true` for `UseOutbox = true`, `UseTransactions = true` and both; assert `IDbTransactionAccessor` and the behavior resolve and `OutboxPostProcessor<,>` is not registered. This also closes #1342's DI-test finding.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #718 in Encina: DI wiring of the transactional outbox for ADO.NET and Dapper.

CONTEXT:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs: AddMessagingServices (ADO.NET + Dapper) calls
  AddOutboxInboxSagaSchedulingServices (shared with EF Core and MongoDB), whose RegisterOutbox registers
  OutboxPostProcessor<,> at line 265; RegisterTransactions registers TransactionPipelineBehavior<,> at line 86.
- Phases 1-3 added DbTransactionAccessor and TransactionalOutboxPipelineBehavior<,>.

TASK:
Register IDbTransactionAccessor (scoped, TryAdd) in AddMessagingServices and AddMessagingServicesCore (the ADO.NET
tenancy entry point, line 358); for ADO.NET/Dapper register
TransactionalOutboxPipelineBehavior<,> instead of OutboxPostProcessor<,>; keep EF Core and MongoDB unchanged. Add one
ValidateOnBuild + ValidateScopes DI test per provider (6) for UseOutbox, UseTransactions and both.

KEY RULES:
- Pay-for-what-you-use: nothing new is registered when no messaging pattern is enabled.
- TryAdd so an application's own IDbTransactionAccessor registration wins.
- No registration removal hacks if a cleaner split of RegisterOutbox is possible.
- Tests use the real AddEncinaADO / AddEncinaDapper extension of each provider package.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs (lines 43-87, 254-294)
- src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs
- Existing DI completeness tests from #1260/#1273 (search tests/ for ValidateOnBuild)
```

</details>

---

### Phase 5: ADO.NET providers (SqlServer, PostgreSQL, MySQL) — #718

> **Goal**: The unit of work and the messaging stores of the three ADO.NET packages participate in the accessor's transaction.

<details>
<summary><strong>Tasks</strong></summary>

For each of `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/`:

1. **`UnitOfWork/UnitOfWorkADO.cs`** — constructor `(IDbTransactionAccessor transactionAccessor, IServiceProvider serviceProvider)`; `BeginTransactionAsync` takes a lease (`TransactionAlreadyActive` only when this unit of work already holds one); `CommitAsync`/`RollbackAsync`/`DisposeAsync` go through the lease (async; replaces the synchronous calls at SqlServer `UnitOfWorkADO.cs:153,175,270` and MySQL `:161,183,278` reported by #1418). `CurrentTransaction` reads `transactionAccessor.Current`.
2. **`UnitOfWork/UnitOfWorkRepositoryADO.cs`** — keep `command.Transaction = _unitOfWork.CurrentTransaction` (`:879` in SqlServer), now backed by the accessor.
3. **`Outbox/OutboxStoreADO.cs`** — constructor gains `IDbTransactionAccessor? transactionAccessor = null`; every command sets `command.Transaction = _transactionAccessor?.Current`; `RequeueExhaustedAsync` joins an active transaction through the accessor instead of beginning its own (`OutboxStoreADO.cs:279`), keeping its own transaction only when none is active. Replace the no-op `OpenConnectionAsync` (`OutboxStoreADO.cs:340-345`) with a real `OpenAsync` (overlaps #1170/#1868; see Prerequisites).
4. **`Inbox/InboxStoreADO.cs`, `Sagas/SagaStoreADO.cs`, `Scheduling/ScheduledMessageStoreADO.cs`** — the same enlistment (Design Choice 6).
5. **`ServiceCollectionExtensions.cs`** — `AddEncinaUnitOfWork` (SqlServer `:508-513`) also `TryAddScoped<IDbTransactionAccessor, DbTransactionAccessor>()` so a UoW without messaging works.
6. **Module isolation**: `ModuleAwareConnectionFactory` returns a `SchemaValidatingConnection` (a `DbConnection`); confirm the accessor's `OpenAsync`/`BeginTransactionAsync` pass through its async overrides and that `SchemaValidatingCommand.Transaction` forwards to the inner command.
7. Shared code: put the enlistment helper (`static void Enlist(IDbCommand, IDbTransactionAccessor?)`) once per package or in `Encina.Messaging` as `internal` + `InternalsVisibleTo`, not ten copies.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #718 in Encina: the three ADO.NET providers join the shared transaction.

CONTEXT:
- Phases 1-4 added IDbTransactionAccessor (scoped, owns the transaction of the scoped IDbConnection) and wired it in DI.
- OutboxStoreADO builds commands without command.Transaction (src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:116);
  on SqlClient and MySqlConnector such a command throws when the connection has a pending transaction; Npgsql runs it
  in the connection's transaction. Its OpenConnectionAsync (line 340) never opens the connection.
- UnitOfWorkADO owns a private transaction (SqlServer UnitOfWorkADO.cs:62, 153, 175) unknown to the pipeline behavior.

TASK:
For SqlServer, PostgreSQL and MySQL: move UnitOfWorkADO onto the accessor; enlist every command of OutboxStoreADO,
InboxStoreADO, SagaStoreADO and ScheduledMessageStoreADO in transactionAccessor.Current; make RequeueExhaustedAsync join
an active transaction; open closed connections with OpenAsync; register the accessor from AddEncinaUnitOfWork.

KEY RULES:
- All three providers in the same change (AGENTS.md section 5); identical behavior, SQL dialects untouched.
- Async only with CancellationToken (AGENTS.md section 3); this removes the UoW/outbox lines of #1418 for these packages.
- The accessor parameter of the stores is optional so the stores still work standalone (tests, background processor
  scope without a transaction): Current is then null and commands run in autocommit.
- Do not change audit, anonymization or ABAC stores (follow-up issue).
- Keep CRAP <= 10: one shared enlistment helper, no copy per method.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.ADO.SqlServer/UnitOfWork/UnitOfWorkADO.cs
- src/Encina.ADO.SqlServer/UnitOfWork/UnitOfWorkRepositoryADO.cs (line 879)
- src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs (async pattern from #1325)
- src/Encina.ADO.SqlServer/Modules/SchemaValidatingConnection.cs, SchemaValidatingCommand.cs
```

</details>

---

### Phase 6: Dapper providers (SqlServer, PostgreSQL, MySQL) — #719

> **Goal**: The same participation for the three Dapper packages; every Dapper call passes the transaction explicitly (#719 acceptance criterion).

<details>
<summary><strong>Tasks</strong></summary>

For each of `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/`:

1. **`UnitOfWork/UnitOfWorkDapper.cs`** — on the accessor, as in Phase 5 (replaces synchronous calls at SqlServer `UnitOfWorkDapper.cs:146,168,263`, reported by #1418).
2. **`UnitOfWork/UnitOfWorkRepositoryDapper.cs`** — already passes `CurrentTransaction` (21 uses per file); back it with the accessor.
3. **`Outbox/OutboxStoreDapper.cs`** — every `ExecuteAsync`/`QueryAsync` passes `transaction: _transactionAccessor?.Current` through `CommandDefinition` (today none does, e.g. `OutboxStoreDapper.cs:56`); `RequeueExhaustedAsync` joins an active transaction (`:210-224`).
4. **`Inbox/`, `Sagas/`, `Scheduling/` Dapper stores** — the same.
5. **`ServiceCollectionExtensions.cs`** — `AddEncinaUnitOfWork` (SqlServer `:489`) registers the accessor.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #718 / #719 in Encina: the three Dapper providers join the shared transaction.

CONTEXT:
- Phases 1-5 are done; ADO.NET stores enlist through IDbTransactionAccessor.
- OutboxStoreDapper calls _connection.ExecuteAsync(new CommandDefinition(sql, message, cancellationToken: ct)) without a
  transaction (src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs:56). UnitOfWorkDapper owns its own transaction.

TASK:
For SqlServer, PostgreSQL and MySQL Dapper packages: move UnitOfWorkDapper onto the accessor; pass
transaction: accessor.Current in every CommandDefinition of the outbox, inbox, saga and scheduled-message stores; make
RequeueExhaustedAsync join an active transaction; register the accessor from AddEncinaUnitOfWork.

KEY RULES:
- Every Dapper Execute/Query call of the four stores passes the transaction parameter explicitly (#719 criterion).
- Same semantics and tests as Phase 5; all three providers together.
- Async only; CommandDefinition always carries the CancellationToken.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs
- src/Encina.Dapper.SqlServer/UnitOfWork/UnitOfWorkDapper.cs, UnitOfWorkRepositoryDapper.cs
- The Phase 5 changes in src/Encina.ADO.SqlServer/
```

</details>

---

### Phase 7: Cross-Cutting Integration

> **Goal**: The transactional path keeps tenancy, module isolation and the transactions pattern coherent.

<details>
<summary><strong>Tasks</strong></summary>

1. **Transactions**: document and test the composition matrix — outbox only; `UseTransactions` only; both; UoW inside either (Design Choice 5).
2. **Multi-tenancy**: the accessor wraps the scoped `IDbConnection`, so the tenant connection chosen by `Tenancy/TenancyServiceCollectionExtensions.cs` in each package is the one used. Add one test per family with tenancy on that the outbox row and the business row land on the same tenant connection. Tenant id on outbox rows is not part of this issue (#1257, #1164).
3. **Module isolation**: verify with `UseModuleIsolation = true` (Phase 5 task 6).
4. **Read/write separation and sharding**: connections from `IReadWriteConnectionFactory` and shard factories are separate connections and are not enlisted; document the limit and record it in the follow-up issue.
5. **Inbox**: `InboxPipelineBehavior` now joins the same transaction when it runs inside it, so marking an inbox message processed commits with the handler's effects; add one test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #718 in Encina: cross-cutting integration of the transactional outbox.

CONTEXT:
- IDbTransactionAccessor owns the transaction of the scoped IDbConnection; ADO.NET and Dapper stores and units of work
  enlist in it; TransactionalOutboxPipelineBehavior writes notifications inside it.
- Tenancy, module isolation and read/write separation each register or wrap connections in the provider packages.

TASK:
Add the composition tests (outbox / UseTransactions / both / UoW inside), one tenancy test and one module-isolation
test per family, one inbox-in-transaction test, and document the read/write-separation and sharding limit.

KEY RULES:
- Tests run on real databases through the shared [Collection("ADO-<Db>")] / [Collection("Dapper-<Db>")] fixtures.
- Do not add TenantId to outbox rows here (#1257); only prove the same connection is used.
- Record every limit you find as a follow-up issue file, not as a TODO.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs (module isolation lines 267-291, read/write 162-201)
- src/Encina.Messaging/Inbox/ (InboxPipelineBehavior)
```

</details>

---

### Phase 8: Observability

> **Goal**: The transaction boundary and the outbox write are traced, metered and logged.

<details>
<summary><strong>Tasks</strong></summary>

1. **Event IDs**: `EventIdRanges.Messaging = (2800, 2999)` (`src/Encina/Diagnostics/EventIdRanges.cs:126`) already covers `Encina.Messaging`; the highest used ID is 2962. Use **2963-2970** packed sequentially; no new range needed:
   - 2963 `TransactionJoined` (Debug), 2964 `TransactionMarkedRollbackOnly` (Warning), 2965 `TransactionRollbackOnlyCommitRefused` (Warning), 2966 `TransactionCommitFailed` (Error, exception through `ForLogging()`), 2967 `OutboxNotificationsWritten` (Debug), 2968 `OutboxWriteFailed` (Error, error code only), 2969 `OutboxPostProcessorWriteFailed` (Error, EF Core/MongoDB path), 2970 `OutboxBehaviorSkipped` (Trace).
   - Reuse 2834-2836 for begin/commit/rollback (Phase 2).
2. **Tracing**: extend `src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs` (`Encina.Messaging.Outbox`) with `StartEnqueue(int notificationCount)` around the write; add a `Encina.Messaging.Transactions` activity in the accessor for begin/commit/rollback with tags `encina.transaction.owner`, `encina.transaction.outcome`. No payloads, no notification content.
3. **Metrics**: on the existing outbox meter (`OutboxProcessorMetrics.cs`) add `encina.outbox.enqueued` (`Counter<long>`, tag `outcome` = committed/rolled_back/failed) and `encina.transaction.rollback_only` (`Counter<long>`).
4. `[LoggerMessage]` source generator only; XML doc naming the range on each method.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #718 in Encina: observability for the transactional outbox.

CONTEXT:
- Encina.Messaging owns EventIdRanges.Messaging = (2800, 2999) in src/Encina/Diagnostics/EventIdRanges.cs:126; the
  highest EventId in use is 2962. MessagingLog already declares 2834-2836 for transactions.
- OutboxActivitySource (src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs) and OutboxProcessorMetrics exist.

TASK:
Add the [LoggerMessage] methods 2963-2970 listed in Phase 8 Tasks, the Enqueue span and transaction activity, and the
two counters; call them from DbTransactionAccessor, TransactionalOutboxPipelineBehavior and OutboxPostProcessor.

KEY RULES:
- EventIds packed sequentially inside the registered range; run the architecture tests
  (tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs).
- No EncinaError.Message, payload or notification content in logs, tags or metrics; error codes only.
- Activities and counters must be exercised by unit tests with an in-memory listener.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Messaging/MessagingLog.cs
- src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, OutboxProcessorMetrics.cs
```

</details>

---

### Phase 9: Testing

> **Goal**: Prove commit and rollback atomicity on the six providers and meet every coverage flag.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** (`tests/Encina.UnitTests/Messaging/`): `DbTransactionAccessorTests` (owner/joiner, rollback-only, isolation mismatch, dispose rolls back, open closed connection), `TransactionalOutboxPipelineBehaviorTests` (skip path, handler `Left`, write `Left`, commit `Left`, exception, cancellation), `OutboxWriterTests`, updated `TransactionPipelineBehaviorTests` and `OutboxPostProcessorTests` (failed write is logged, no longer silently dropped).
2. **Guard tests**: new public constructors and methods (accessor, behavior, changed store and UoW constructors) in `tests/Encina.GuardTests/{Messaging,ADO,Dapper}/`.
3. **Contract tests** (`tests/Encina.ContractTests/Messaging/`): an `IDbTransactionAccessor` contract run against all six providers' real connections; extend `TransactionPipelineBehaviorContractTests`.
4. **Property tests** (`tests/Encina.PropertyTests/`): FsCheck — for any sequence of joins, commits and rollbacks, the transaction commits iff the owner commits and no participant rolled back; for any notification count n ≥ 0, a committed request leaves n rows, a rolled-back one 0.
5. **Integration tests** (`tests/Encina.IntegrationTests/{ADO,Dapper}/{SqlServer,PostgreSQL,MySQL}/Outbox/OutboxAtomicityTests.cs`, 6 files, `[Collection("<Family>-<Db>")]`, `[Trait("Category", "Integration")]`, `[Trait("Database", "<Db>")]`, `ClearAllDataAsync` in `InitializeAsync`):
   - commit: business row + n outbox rows;
   - handler `Left`: neither;
   - handler throws: neither;
   - outbox write fails (e.g. duplicate `Id`): business row rolled back, request returns `Left`;
   - `UseTransactions` + outbox: one transaction (no `TransactionAlreadyActive`, no SqlClient "pending local transaction" error);
   - `IUnitOfWork` inside the request: joins; inner rollback makes the request fail with `transaction.rollback_only`.
6. **Load tests**: `.md` justification (`tests/Encina.LoadTests/Messaging/OutboxAtomicity.md`): one transaction per scope, no shared state across requests.
7. **Benchmark tests**: one benchmark of the skip path (request without `IHasNotifications`) vs. no behavior in `tests/Encina.BenchmarkTests/Encina.ADO.SqlServer.Benchmarks/` to prove the "no regression" criterion; the rest justified in `.md`.
8. **Coverage**: per-file targets with justifications in `.github/coverage-manifest/Encina.Messaging.json` and the six provider manifests for every touched file; measure per flag as AGENTS.md §9 describes; CRAP ≤ 10 on every changed method (run the local crap-gate table).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #718 / #719 in Encina: tests for outbox atomicity on six providers.

CONTEXT:
- Phases 1-8 are implemented: IDbTransactionAccessor, TransactionalOutboxPipelineBehavior, accessor-based
  TransactionPipelineBehavior and units of work, enlisted ADO.NET and Dapper messaging stores.
- No existing test asserts outbox rollback; tests/Encina.IntegrationTests/ADO/*/Outbox/OutboxStoreADOTests.cs test the
  store alone.

TASK:
Write the unit, guard, contract, property and integration tests and the load/benchmark items listed in Phase 9 Tasks;
add per-file coverage targets with justifications to the manifests and measure every flag.

KEY RULES:
- Tests execute real code (no reflection-only tests); Shouldly through Encina.Testing.Shouldly; no Thread.Sleep.
- Integration tests use the shared collection fixtures, never IClassFixture or new fixtures, never dispose the fixture.
- Every integration scenario runs on all six providers (ADO and Dapper x SqlServer, PostgreSQL, MySQL).
- Measure each flag with dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory
  artifacts\coverage\<Flag>Tests and .github/scripts/coverage-report.cs; outputs only under artifacts/.

REFERENCE FILES:
- tests/Encina.IntegrationTests/ADO/SqlServer/Outbox/OutboxStoreADOTests.cs (fixture usage)
- tests/Encina.IntegrationTests/ADO/SqlServer/UnitOfWork/UnitOfWorkADOIntegrationTests.cs
- tests/Encina.UnitTests/Messaging/Pipeline/OutboxPostProcessorTests.cs
- docs/testing/integration-tests.md, docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 10: Documentation & Finalization

> **Goal**: Users know how atomicity works, how to join the transaction in their own handlers, and what remains outside it.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation** on every new or changed public API (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`), including an example of a raw ADO.NET handler that sets `command.Transaction = accessor.Current` and a Dapper handler that passes `transaction: accessor.Current`.
2. **Changelog fragments**: `changelog.d/718-outbox-atomicity-ado.fixed.md` and `changelog.d/719-outbox-atomicity-dapper.fixed.md` (lost events were a defect), plus a `changed` fragment for the constructor changes of `TransactionPipelineBehavior`, `UnitOfWorkADO` and `UnitOfWorkDapper`.
3. **ADR**: `docs/architecture/adr/037-transactional-outbox-for-ado-and-dapper.md` (next free number; check `docs/architecture/adr/index.md` for reservations) recording Design Choices 2, 3 and 5; add it to the index.
4. **Feature guide**: `docs/features/transactional-outbox.md` (how-to quadrant per the encina-docs skill): composition matrix, handler examples, limits (read/write separation, sharding, EF Core/MongoDB follow-up).
5. **Package READMEs**: `src/Encina.Messaging/README.md`, the six provider READMEs (UoW and outbox sections).
6. **`docs/INVENTORY.md`**: new files.
7. **`ROADMAP.md`**: mark the v0.14.0 hardening item if listed.
8. **PublicAPI**: `PublicAPI.Unshipped.txt` of `Encina.Messaging` and the six provider packages (changed constructors: remove stale lines, RS0017).
9. **Build verification**: `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings.
10. **Test verification**: `dotnet test` → all pass; every coverage flag (unit, guard, contract, property, integration) reaches its own target in `.github/coverage-manifest/{Package}.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of issue #718 / #719 in Encina: documentation and finalization.

CONTEXT:
- The transactional outbox for ADO.NET and Dapper is implemented and tested (Phases 1-9).
- Documentation follows the encina-docs skill (Diataxis, one quadrant per page, no hand-typed coverage figures).

TASK:
Complete the XML docs, changelog fragments, ADR 037 (or the next free number), docs/features/transactional-outbox.md,
the seven READMEs, docs/INVENTORY.md, ROADMAP.md and PublicAPI files; run the Release build and the full test suite.

KEY RULES:
- Never edit CHANGELOG.md [Unreleased] by hand; use changelog.d/ fragments (changelog.d/README.md).
- English only; no emojis; cite coverage with covref markers, never typed figures.
- Zero warnings; PublicAPI lines in the Namespace.Type.Member(params) -> ReturnType format.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- docs/architecture/adr/index.md, docs/architecture/adr/036-three-audit-stores.md (ADR style)
- docs/features/pipeline-behaviors.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Topic | Relevance |
|--------|-------|-----------|
| Transactional Outbox pattern (microservices.io, C. Richardson) | Business change and message row in one local transaction | The guarantee #718 restores |
| ADR-001 / ADR-006 (Railway Oriented Programming) | `Either<EncinaError, T>` everywhere | Lease, accessor and behavior return `Either`; failures fail the request |
| ADR-018 (cross-cutting integration) | 12 transversal functions | Matrix below |
| ADR-021 (EventId ranges) | Registered ranges, packed IDs | 2963-2970 in `Messaging` |
| ADR-024 (SQLite out of the matrix) | Provider count | The issue's "4 providers" becomes 3 per family |
| AGENTS.md §3 | Async DB calls, errors never swallowed, registration completeness | Phases 1-5, 4 |
| SPEC-002 REQ-043 / #1236 | Domain events to the outbox in the same commit | Same guarantee for domain events; builds on this accessor for ADO.NET/Dapper |

### Provider Transaction Semantics (to verify in Phase 9)

| Driver | Command without `Transaction` on a connection with a pending transaction | Consequence today |
|--------|------|------|
| Microsoft.Data.SqlClient | Throws `InvalidOperationException` | A store call inside `UseTransactions` fails on SqlServer |
| MySqlConnector | Throws unless `IgnoreCommandTransaction=true` | Same on MySQL |
| Npgsql | Runs in the connection's transaction | Atomic by accident on PostgreSQL, if the write ran inside the transaction (it does not, defect 1) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `TransactionPipelineBehavior<,>` | `src/Encina.Messaging/TransactionPipelineBehavior.cs` | Becomes a lease holder (Phase 2) |
| `OutboxPostProcessor<,>` | `src/Encina.Messaging/Outbox/OutboxPostProcessor.cs` | Logic extracted to `OutboxWriter`; kept for EF Core/MongoDB |
| `IHasNotifications` | `OutboxPostProcessor.cs:117` | Trigger of the new behavior |
| `IMessageSerializer.SerializeAsRuntimeType` | `src/Encina.Messaging/Serialization/` | Payload serialization (encryption decorators, #1168) |
| `MessagingLog.Transaction*` | `src/Encina.Messaging/MessagingLog.cs:203-231` | Already declared, unused (#1342) |
| `OutboxActivitySource`, `OutboxProcessorMetrics` | `src/Encina.Messaging/Diagnostics/` | Spans and counters |
| `UnitOfWorkADO` / `UnitOfWorkDapper` | `src/Encina.{ADO,Dapper}.*/UnitOfWork/` | Move onto the accessor |
| Async begin/commit pattern | `src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs:259-295` (#1325) | Template for the accessor |
| `AddMessagingServices` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:44` | DI entry point of ADO.NET and Dapper |
| Shared integration fixtures | `ADO-*`, `Dapper-*` collections | Atomicity tests on real databases |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | 2800-2999 (`EventIdRanges.Messaging`) | Existing range; 2834-2836 reused for begin/commit/rollback |
| **`Encina.Messaging` (this feature)** | **2963-2970** | **New IDs inside the existing range; highest used today is 2962** |
| `Encina.ADO.*`, `Encina.Dapper.*` | 3200-3499 | No new log messages in the provider packages |

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| `Encina.Messaging` (Phases 1-4, 8) | ~10 | 5 new in `Transactions/`, behavior, writer, post-processor, DI, log |
| ADO.NET ×3 (Phase 5) | ~18 | UoW, UoW repository, 4 stores, DI per provider |
| Dapper ×3 (Phase 6) | ~18 | Same |
| Tests (Phase 9) | ~35 | 6 integration, ~10 unit, ~8 guard, 2 contract, 2 property, DI tests, 1 benchmark, `.md` justifications |
| Documentation (Phase 10) | ~14 | ADR, feature guide, 7 READMEs, INVENTORY, changelog fragments, PublicAPI |
| **Total** | **~95** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #718 (outbox transaction atomicity for ADO.NET) together with its Dapper twin #719 in Encina.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 CQRS library, pre-1.0 (breaking changes welcome, no compatibility shims).
- Railway Oriented Programming: Either<EncinaError, T>; errors are never swallowed; compliance gates fail closed.
- Database providers: ADO.NET x3 and Dapper x3 (SqlServer, PostgreSQL, MySQL) in this plan; EF Core x3 and MongoDB
  in a follow-up issue. SQLite is out of the matrix (ADR-024).
- Root cause: OutboxPostProcessor runs after TransactionPipelineBehavior has committed (post-processors are the
  outermost pipeline layer, src/Encina/Pipeline/PipelineBuilder.cs:83) and discards AddAsync's Either; no store can
  see the transaction; UnitOfWork and TransactionPipelineBehavior own separate transactions on one connection.

IMPLEMENTATION OVERVIEW:
Phase 1: IDbTransactionAccessor + IDbTransactionLease + DbTransactionAccessor + TransactionErrors (Encina.Messaging/Transactions)
Phase 2: TransactionPipelineBehavior takes a lease; logs via MessagingLog 2834-2836
Phase 3: OutboxWriter + TransactionalOutboxPipelineBehavior (join-or-begin); OutboxPostProcessor stops dropping errors
Phase 4: DI: accessor + behavior for ADO.NET/Dapper; post-processor stays for EF Core/MongoDB; ValidateOnBuild tests
Phase 5: ADO.NET x3: UnitOfWorkADO on the accessor; outbox/inbox/saga/scheduled stores enlist; real OpenAsync
Phase 6: Dapper x3: same; every Dapper call passes transaction explicitly
Phase 7: Cross-cutting: composition matrix, tenancy, module isolation, inbox, documented limits
Phase 8: Observability: EventIds 2963-2970, Enqueue span, transaction activity, two counters
Phase 9: Tests: unit, guard, contract, property, integration on 6 providers, benchmark of the skip path, load .md
Phase 10: Docs: XML, changelog.d fragments, ADR 037, docs/features/transactional-outbox.md, READMEs, PublicAPI

KEY PATTERNS:
- One owner per scope; joiners never commit; inner rollback => rollback-only => owner commit returns Left.
- Async DB calls with CancellationToken only; cleanup commit/rollback with CancellationToken.None.
- Pipeline behavior: static per-generic-type flag, zero-cost skip for requests without IHasNotifications.
- Store naming unchanged (OutboxStoreADO, OutboxStoreDapper); TryAdd registrations; DI completeness tests.
- Integration tests: [Collection("ADO-<Db>")] / [Collection("Dapper-<Db>")], ClearAllDataAsync in InitializeAsync.
- Observability: [LoggerMessage] with EventIds in the registered range; no payloads or EncinaError.Message.

REFERENCE FILES:
- src/Encina.Messaging/TransactionPipelineBehavior.cs, Outbox/OutboxPostProcessor.cs, MessagingServiceCollectionExtensions.cs
- src/Encina/Pipeline/PipelineBuilder.cs
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, UnitOfWork/UnitOfWorkADO.cs, ServiceCollectionExtensions.cs
- src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs (async pattern)
- src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, UnitOfWork/UnitOfWorkDapper.cs
- docs/plans/outbox-atomicity-ado-implementation-plan-718.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path; the feature is a write-side transaction boundary |
| 2 | OpenTelemetry | ✅ | Enqueue span on `Encina.Messaging.Outbox`, transaction activity, `encina.outbox.enqueued` and `encina.transaction.rollback_only` counters (Phase 8) |
| 3 | Structured Logging | ✅ | `MessagingLog` 2834-2836 finally wired (#1342) and new IDs 2963-2970 inside `EventIdRanges.Messaging` (Phase 8) |
| 4 | Health Checks | ❌ | No new checkable dependency; the database health checks of each provider already cover the connection |
| 5 | Validation | ❌ | No user input; configuration flags are booleans already validated by `MessagingConfiguration` |
| 6 | Resilience | ❌ | No external call; retrying a business transaction is the application's decision, and the outbox processor already retries delivery |
| 7 | Distributed Locks | ❌ | A local database transaction on one scoped connection; no shared state across hosts (processor locking is #714/#717/#1251) |
| 8 | Transactions | ✅ | The core of the feature: one owner per scope, join semantics, rollback-only (Phases 1-6) |
| 9 | Idempotency | ⏭️ | Outbox id as idempotency key on dispatch is #1220; `InboxPipelineBehavior` joining the transaction is covered in Phase 7 |
| 10 | Multi-Tenancy | ⏭️ | The tenant connection is reused (Phase 7 test); `TenantId` on outbox rows and tenant-aware cycles are #1257 and #1164 |
| 11 | Module Isolation | ✅ | Accessor must work through `SchemaValidatingConnection` (Phase 5 task 6, Phase 7 test) |
| 12 | Audit Trail | ⏭️ | Whether audit stores share or escape the business transaction is a separate decision, recorded in `artifacts/issues/plan-718-enlist-remaining-stores.md` |

---

## Prerequisites & Dependencies

### Advisable before or with this work

| Issue | State | Relation |
|-------|-------|----------|
| #1170 / #1868 | open | No-op `OpenConnectionAsync` in ADO stores (`OutboxStoreADO.cs:340-345` and 20 siblings). Phase 5 fixes it for the four messaging stores; land #1170/#1868 first or narrow them to the remaining stores. The two issues describe the same defect and look like duplicates |
| #1418 | open | Synchronous UoW/outbox calls in ADO.SqlServer, ADO.MySQL and Dapper ×3; Phases 5-6 remove the UoW and outbox lines, the rest of #1418 stays |
| #1342 | open | `TransactionPipelineBehavior` logging and DI tests; Phases 2 and 4 cover it, so the PR can close it |
| #1340 | open | Duplicated `TransactionPipelineBehavior` guard tests; Phase 2 touches the same files |

### Follow-ups and overlaps

| Issue | Relation |
|-------|----------|
| #719 | Dapper twin; covered by Phase 6 of this plan (recommended) |
| #1236 | Domain events to the outbox in the same commit on all 10 providers; reuses `IDbTransactionAccessor` for ADO.NET/Dapper |
| #720 | Saga compensation transactional safety; can build on the accessor |
| `artifacts/issues/plan-718-outbox-atomicity-efcore-mongodb.md` | New: same guarantee for EF Core ×3 and MongoDB |
| `artifacts/issues/plan-718-enlist-remaining-stores.md` | New: audit, anonymization and ABAC stores, read/write-separation and shard connections |

---

## Next Steps

1. Maintainer reviews the six Design Choices (the recommendations are pending decision).
2. Link this plan from #718 and #719; decide whether #719 is closed by the same PR.
3. Open the two follow-up issues from `artifacts/issues/`.
4. Resolve the #1170/#1868 duplication and decide whether they land first.
5. Implement Phases 1-10, one self-contained commit per phase; the final commit references `Fixes #718` (and `Fixes #719`).
