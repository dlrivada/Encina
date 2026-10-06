# Implementation Plan: Outbox Transaction Atomicity on All 10 Database Providers

> **Issue**: [#718](https://github.com/dlrivada/Encina/issues/718) (ADO.NET); Dapper twin [#719](https://github.com/dlrivada/Encina/issues/719); EF Core and MongoDB scope from [#1935](https://github.com/dlrivada/Encina/issues/1935) (closed as a duplicate of #718)
> **Type**: Feature
> **Complexity**: Very high (13 phases, all 10 database providers, shared change in `Encina.Messaging`, removal of `OutboxPostProcessor`)
> **Estimated Scope**: ~2,200-3,000 lines of production code + ~4,000-5,000 lines of tests

---

## Summary

Make the outbox write and the business change of a request commit or roll back together on all 10 database providers: ADO.NET and Dapper (SqlServer, PostgreSQL, MySQL), EF Core (SqlServer, PostgreSQL, MySQL) and MongoDB. A committed command leaves exactly one outbox row per notification. A rolled-back command, a `Left` result or a failed outbox write leaves no row and fails the request. When every provider writes through the new behavior, `OutboxPostProcessor` is removed (scope of #1935).

### What the code does today (2026-10-06, `main` at `5b485b12`)

The issue assumes `OutboxStoreADO` only needs to "join the ambient transaction". The code shows four separate defects. The first two affect every provider:

1. **The outbox write runs after the transaction is gone.** `OutboxPostProcessor` is an `IRequestPostProcessor` (`src/Encina.Messaging/Outbox/OutboxPostProcessor.cs:14`). It is registered for all 10 providers by the shared `RegisterOutbox` (`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:265`). `PipelineBuilder` wraps the behaviors first and the pre/post-processors outermost (`src/Encina/Pipeline/PipelineBuilder.cs:83`). Post-processors run after `terminal()` returns (`PipelineBuilder.cs:112-114`). The two transaction behaviors commit inside `terminal()`:
   - `Encina.Messaging.TransactionPipelineBehavior` (ADO.NET, Dapper) keeps its transaction in a local variable (`src/Encina.Messaging/TransactionPipelineBehavior.cs:81-115`).
   - `Encina.EntityFrameworkCore.TransactionPipelineBehavior` commits in `CommitOrRollbackAsync` (`src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs:156-175`).
   - MongoDB has no transaction behavior at all.

   The outbox row is therefore always written in a separate, later statement. If the business change commits and the outbox write fails, the event is **lost**.
2. **The outbox failure is swallowed.** `OutboxPostProcessor.Process` discards the `Either` returned by `_outboxStore.AddAsync` (`OutboxPostProcessor.cs:95`) and by `SaveChangesAsync` (`OutboxPostProcessor.cs:98`). `IRequestPostProcessor.Process` returns `Task` (`src/Encina/Abstractions/IRequestPostProcessor.cs`), so the request still reports success. This breaks AGENTS.md §3 ("Errors are NEVER swallowed").
3. **No relational store can see the transaction; MongoDB stores do not use a session.**
   - ADO.NET: `UnitOfWorkADO.CurrentTransaction` is `internal` (`src/Encina.ADO.SqlServer/UnitOfWork/UnitOfWorkADO.cs:91`) and only `UnitOfWorkRepositoryADO` reads it (`UnitOfWorkRepositoryADO.cs:879`). `OutboxStoreADO` builds commands without `command.Transaction` (`src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:116`).
   - Dapper: `OutboxStoreDapper` calls `ExecuteAsync` without a transaction (`src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs:56`).
   - Driver behavior: on SqlClient and MySqlConnector, a command without the pending transaction throws `InvalidOperationException` when the connection has one; Npgsql runs it inside the connection's transaction.
   - EF Core is already enlisted through the shared `DbContext` (`OutboxStoreEF` uses the scoped `DbContext`, `src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs:38,58,202`). It fails only because of defect 1.
   - MongoDB: `OutboxStoreMongoDB` writes without a session (`src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs:61,106,134,205`). Only `UnitOfWorkRepositoryMongoDB` reads the internal `UnitOfWorkMongoDB.CurrentSession` (`src/Encina.MongoDB/UnitOfWork/UnitOfWorkMongoDB.cs:105`; `UnitOfWorkRepositoryMongoDB.cs:80`).
4. **Two transaction owners on one connection or context.**
   - ADO.NET/Dapper: `UnitOfWorkADO.BeginTransactionAsync` detects only its own transaction (`UnitOfWorkADO.cs:145-148`), not the pipeline behavior's transaction on the same scoped `IDbConnection` (`src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:290`). SqlServer and MySQL also begin and commit synchronously (`UnitOfWorkADO.cs:153,175`; `src/Encina.ADO.MySQL/UnitOfWork/UnitOfWorkADO.cs:161,183`), which #1418 tracks.
   - EF Core: `UnitOfWorkEF` keeps its own `IDbContextTransaction` (`src/Encina.EntityFrameworkCore/UnitOfWork/UnitOfWorkEF.cs:84,226`). EF's `TransactionPipelineBehavior` reuses `Database.CurrentTransaction` (`TransactionPipelineBehavior.cs:89`), but `UnitOfWorkEF` checks only `_transaction` (`UnitOfWorkEF.cs:219`). It then calls `Database.BeginTransactionAsync` while the behavior already holds a transaction, and EF throws.
   - MongoDB: `UnitOfWorkMongoDB` starts a private session (`UnitOfWorkMongoDB.cs:177-178`) that no store shares.

Nothing of #718, #719 or #1935 is implemented. There is no transaction accessor anywhere in `src/`, and no integration test asserts outbox rollback on any provider.

### Scope (decided by the maintainer, 2026-10-06: all 10 providers)

- **Affected packages**:
  - `Encina.Messaging`: lease abstractions, the relational transaction accessor, the outbox behavior, `TransactionPipelineBehavior`, DI, and the removal of `OutboxPostProcessor`.
  - `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`.
  - `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`.
  - `Encina.EntityFrameworkCore` (one package for SqlServer, PostgreSQL and MySQL).
  - `Encina.MongoDB`.
  - `Encina.Cdc` (one XML comment).
- **Provider category**: Database, all 10 providers. SQLite is out of the matrix (ADR-024); the issue's "4 providers" predates that decision.
- **Estimated files**: ~40 production files touched or created, ~55 test files.

---

## Design Choices

<details>
<summary><strong>1. Plan scope — all 10 providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) ADO.NET only (#718 as written)** | Smallest PR; matches the issue literally | The `Encina.Messaging` change lands with only part of its consumers; Dapper keeps the broken post-processor path, so the families diverge (AGENTS.md §5 provider coherence) |
| **B) ADO.NET + Dapper in one plan (#718 + #719)** | One shared core, two thin provider phases; both relational-ADO families switch at once | EF Core and MongoDB keep a known-broken, non-atomic outbox path and `OutboxPostProcessor` survives |
| **C) All 10 providers now** | Full coherence; one outbox write path; `OutboxPostProcessor` removed (#1935) | EF Core needs a `DbContext` transaction and MongoDB a client session, so the change is larger; overlaps #1236 |

### Chosen Option: **C — all 10 providers** (decided by the maintainer, 2026-10-06)

### Rationale

- Original recommendation, kept as context: the plan writer recommended B. The root cause sits in `Encina.Messaging`, which ADO.NET and Dapper consume through `AddMessagingServices` (`src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:69`, `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs:69`). EF Core and MongoDB need a different transaction mechanism, so B proposed moving them to a follow-up (later opened as #1935).
- C makes the outbox guarantee identical on every provider, which AGENTS.md §5 requires for database features. It lets `OutboxPostProcessor`, the only non-atomic write path, be deleted instead of maintained.
- EF Core and MongoDB get their own phases (Phases 7 and 8), and the removal is Phase 9. Because of this decision, the behavior depends on a provider-neutral lease contract that the relational `IDbTransactionAccessor` extends (see Design Choice 3, "Consequence of Decision 1").
- #719 and #1935 are closed by the PR or PRs that complete this plan.

</details>

<details>
<summary><strong>2. Where the outbox write runs — a pipeline behavior that joins or begins the transaction</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TransactionalOutboxPipelineBehavior` that joins the ambient transaction or begins its own** | Works with or without `UseTransactions`; independent of behavior registration order; a failed write returns `Left` and rolls back; requests without `IHasNotifications` skip it at zero cost | New public behavior; each provider must supply a transaction source |
| **B) Pre-commit participants invoked by `TransactionPipelineBehavior`** (`ITransactionParticipant`) | Single transaction owner; generic hook usable by other patterns | Outbox atomicity silently depends on `UseTransactions = true`; MongoDB has no transaction behavior at all |
| **C) Change `PipelineBuilder` so post-processors run inside the behaviors** | Keeps `OutboxPostProcessor` unchanged | Core semantic change for every post-processor of every application; post-processors still cannot fail the request (`Task` return type) |
| **D) Explicit only: handlers call `OutboxOrchestrator.AddAsync` inside their unit of work** | No pipeline magic | `IHasNotifications` stops working; every handler must remember; errors easy to drop |

### Chosen Option: **A — `TransactionalOutboxPipelineBehavior` with join-or-begin semantics** (decided by the maintainer, 2026-10-06)

### Rationale

- Atomicity holds whatever the configuration: when a transaction is already active (from a transaction behavior or `IUnitOfWork`) the behavior joins it; otherwise it begins one around the handler and the outbox write. This is what makes MongoDB, which has no transaction behavior, work at all.
- Join semantics make the behavior order-independent. This matters because `RegisterOutbox` runs before `RegisterTransactions` (`MessagingServiceCollectionExtensions.cs:60-66`), and EF Core registers its transaction behavior before the outbox (`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:159,165`).
- It returns `Either`, so a failed outbox write fails the request and rolls back the business change (fail closed, AGENTS.md §3).
- Pay-for-what-you-use: a static per-`TRequest` check of `IHasNotifications` plus a runtime check short-circuits to `nextStep()` without opening a transaction.

</details>

<details>
<summary><strong>3. Transaction-sharing abstraction — a scoped <code>IDbTransactionAccessor</code> with ownership leases</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Scoped `IDbTransactionAccessor` (current transaction + `BeginOrJoinAsync` returning an `IDbTransactionLease`)** | One owner per scope; every participant reads the same transaction; explicit, testable, async with `CancellationToken` | Every relational store must set `command.Transaction` (or pass `transaction:` to Dapper); new public API |
| **B) Make `UnitOfWork*.CurrentTransaction` public and make stores depend on `IUnitOfWork`** | Reuses an existing type | `IUnitOfWork` is optional (AGENTS.md §3, repository pattern never forced); stores gain a dependency on DomainModeling's UoW |
| **C) `System.Transactions.TransactionScope` with `TransactionScopeAsyncFlowOption.Enabled`** | Ambient, no plumbing | Enlistment differs per driver; escalation to distributed transactions; MongoDB does not enlist; no `Either` |
| **D) `IDbConnection` decorator that assigns the active transaction to every command it creates** | Handler code and every store enlist automatically | Breaks `is SqlConnection` / `is SqlCommand` fast paths (`OutboxStoreADO.cs:349-376`, `UnitOfWorkADO.cs:251`); stacks with `SchemaValidatingConnection`; does nothing for EF Core or MongoDB |

### Chosen Option: **A — scoped `IDbTransactionAccessor` with leases** (decided by the maintainer, 2026-10-06)

### Rationale

- Explicit and identical on SqlClient, Npgsql and MySqlConnector: every command that must be atomic carries the transaction, which removes the per-driver difference of defect 3.
- The lease models ownership. The first `BeginOrJoinAsync` in a scope owns the transaction and is the only participant that commits. Joiners get a non-owning lease: its `CommitAsync` is a no-op and its `RollbackAsync` marks the transaction rollback-only (Design Choice 5).
- EF Core implements the same interface over its `DbContext`. `DatabaseFacade.GetDbConnection()` and `IDbContextTransaction.GetDbTransaction()` expose the relational `DbConnection`/`DbTransaction`, so raw ADO.NET or Dapper code in an EF Core handler can join too (Phase 7).

### Consequence of Decision 1 (to confirm with the maintainer)

MongoDB has no `IDbConnection` or `IDbTransaction`, so `IDbTransactionAccessor` cannot represent a MongoDB session. Covering all 10 providers therefore needs one small provider-neutral contract under the relational one:

```csharp
namespace Encina.Messaging.Transactions;

public interface ITransactionLeaseSource           // what TransactionalOutboxPipelineBehavior depends on
{
    bool HasActiveTransaction { get; }
    ValueTask<Either<EncinaError, ITransactionLease>> BeginOrJoinAsync(CancellationToken cancellationToken = default);
}

public interface ITransactionLease : IAsyncDisposable
{
    bool IsOwner { get; }
    ValueTask<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default);
    ValueTask RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IDbTransactionAccessor : ITransactionLeaseSource   // decided option A, relational providers
{
    IDbConnection Connection { get; }
    IDbTransaction? Current { get; }
    ValueTask<Either<EncinaError, IDbTransactionLease>> BeginOrJoinAsync(
        IsolationLevel? isolationLevel, CancellationToken cancellationToken = default);
}

public interface IDbTransactionLease : ITransactionLease { IDbTransaction Transaction { get; } }

// Encina.MongoDB
public interface IMongoSessionAccessor : ITransactionLeaseSource { IClientSessionHandle? CurrentSession { get; } }
```

The decided option is unchanged for the nine relational providers. The base interface only lets the outbox behavior stay in `Encina.Messaging` without knowing about MongoDB.

</details>

<details>
<summary><strong>4. Placement — <code>Encina.Messaging</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Messaging` (`Transactions/` folder)** | `TransactionPipelineBehavior`, `IOutboxStore` and the shared registrations already live there; all 10 provider packages already reference it | Couples a data-access concept to the messaging package |
| **B) `Encina.DomainModeling` (next to `IUnitOfWork`)** | Next to the UoW contract | `Encina.Messaging` would need a new reference to DomainModeling for the behaviors |
| **C) New `Encina.Data.Abstractions` package** | Clean separation | A new package for a handful of interfaces; more DI ceremony |

### Chosen Option: **A — `Encina.Messaging`** (decided by the maintainer, 2026-10-06)

### Rationale

- The consumers (`TransactionPipelineBehavior`, the new outbox behavior, the messaging stores) are already in or reference `Encina.Messaging`, so no provider package needs a new project reference.
- Provider implementations live in their own packages and depend only on the abstraction:
  - `DbTransactionAccessor` in `Encina.Messaging` for ADO.NET and Dapper;
  - `DbContextTransactionAccessor` in `Encina.EntityFrameworkCore`;
  - `MongoSessionAccessor` in `Encina.MongoDB`.

  This follows AGENTS.md §3 provider coherence.

</details>

<details>
<summary><strong>5. Nested transactions — join with rollback-only, single owner</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `TransactionAlreadyActive` errors** | No new semantics | `IUnitOfWork.BeginTransactionAsync` inside a handler fails once a behavior opened a transaction (and on EF Core throws today, defect 4) |
| **B) Join: inner participants share the owner's transaction; inner commit is a no-op; inner rollback marks the transaction rollback-only; the owner's commit then rolls back and returns `Left`** | Patterns compose (UoW + transaction behavior + outbox) on every provider; a failure anywhere fails closed | Inner `CommitAsync` returning success does not mean data is durable; must be documented |
| **C) Savepoints for inner scopes** | Inner rollback undoes only its part | Syntax differs per database; MongoDB has no savepoints; partial rollback contradicts the outbox guarantee |

### Chosen Option: **B — join with rollback-only and a single owner** (decided by the maintainer, 2026-10-06)

### Rationale

- The outbox guarantee is all-or-nothing for the whole request. Savepoints would allow a committed business change whose inner part rolled back, and MongoDB cannot support them.
- Rollback-only makes a swallowed inner failure impossible: the owner's commit returns `Left(transaction.rollback_only)` instead of committing.
- `HasActiveTransaction` on each unit of work keeps its meaning ("this unit of work holds a lease"). The `TransactionAlreadyActive` error stays only for a second `BeginTransactionAsync` on the same unit of work.

</details>

<details>
<summary><strong>6. Enlistment scope — the four messaging stores and the unit-of-work repositories</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Outbox store only** | Minimal | On SqlClient/MySqlConnector, any inbox, saga or scheduled-message call inside a handler throws; on MongoDB those writes escape the session |
| **B) The four messaging stores (Outbox, Inbox, Saga, ScheduledMessage) + the UoW repositories** | Covers every store a handler typically calls inside a messaging request; one helper per family | Audit, anonymization, ABAC and read/write-separation connections stay outside (#1934) |
| **C) Every store on the scoped connection, context or client** | Complete | Many more stores per family; mixes in audit stores whose rollback semantics need a separate decision |

### Chosen Option: **B — the four messaging stores and the UoW repositories** (decided by the maintainer, 2026-10-06)

### Rationale

- The messaging patterns compose inside one transaction on every provider, without deciding here whether audit records should survive a business rollback.
- EF Core gets B for free: every EF store and repository uses the same scoped `DbContext`, whose current transaction applies to all its commands.
- The remaining relational stores are tracked by #1934. The MongoDB equivalents (audit, anonymization, ABAC stores without a session) are added to its scope in Phase 8.

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions — leases and `IDbTransactionAccessor`

> **Goal**: One scoped transaction owner per DI scope, with a provider-neutral lease contract and the relational accessor used by ADO.NET and Dapper.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Messaging/Transactions/`

1. **`ITransactionLeaseSource.cs`, `ITransactionLease.cs`**: provider-neutral contract (Design Choice 3, "Consequence of Decision 1").
2. **`IDbTransactionAccessor.cs`, `IDbTransactionLease.cs`**: relational contract extending the neutral one: `IDbConnection Connection`, `IDbTransaction? Current`, `BeginOrJoinAsync(IsolationLevel?, CancellationToken)`, `IDbTransaction Transaction`.
3. **`DbTransactionAccessor.cs`**: `public sealed class DbTransactionAccessor : IDbTransactionAccessor, IAsyncDisposable`, for ADO.NET and Dapper.
   - Constructor: `(IDbConnection connection, ILogger<DbTransactionAccessor> logger)`.
   - Opens the connection with `DbConnection.OpenAsync(ct)` when it is closed. The only fallback, for a non-`DbConnection`, is `Task.Run(connection.Open, ct)` (the pattern of `src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs:259-266`, from #1325).
   - Begins with `DbConnection.BeginTransactionAsync(isolationLevel, ct)`.
   - Tracks the owner lease, the depth and a rollback-only flag. Joiners receive a non-owning lease. A joiner that asks for a different isolation level gets `Left(transaction.isolation_level_mismatch)`.
   - Owner `CommitAsync`: if rollback-only, roll back and return `Left(transaction.rollback_only)`; otherwise commit. On an exception, roll back and return `Left(transaction.commit_failed)` (error code only).
   - Cleanup commit and rollback use `CancellationToken.None` (as in `TransactionPipelineBehavior.cs:23-27`).
4. **`TransactionLeaseState.cs`**: `internal sealed class` holding owner, depth and rollback-only. Shared by the three accessor implementations through `InternalsVisibleTo` (`Encina.EntityFrameworkCore`, `Encina.MongoDB`), or as a public `abstract class TransactionLeaseSourceBase`, so the join semantics exist once.
5. **`TransactionErrors.cs`**: codes `transaction.begin_failed`, `transaction.commit_failed`, `transaction.rollback_only`, `transaction.isolation_level_mismatch`, `transaction.lease_disposed`, `transaction.not_supported`.
6. **`PublicAPI.Unshipped.txt`**: all new public symbols.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #718 (outbox atomicity on all 10 database providers) in Encina.

CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming (Either<EncinaError, T>), nullable enabled.
- No component shares the transaction today: Encina.Messaging.TransactionPipelineBehavior keeps it local
  (src/Encina.Messaging/TransactionPipelineBehavior.cs:81), UnitOfWorkADO exposes it internally (UnitOfWorkADO.cs:91),
  UnitOfWorkEF and UnitOfWorkMongoDB own private transactions/sessions.
- The maintainer decided a scoped IDbTransactionAccessor with ownership leases in Encina.Messaging. A provider-neutral
  ITransactionLeaseSource / ITransactionLease sits under it so MongoDB (no IDbTransaction) can implement it in Phase 8.

TASK:
Create the files of Phase 1 Tasks in src/Encina.Messaging/Transactions/ (namespace Encina.Messaging.Transactions) as
listed in docs/plans/outbox-atomicity-ado-implementation-plan-718.md, and add every public symbol to
src/Encina.Messaging/PublicAPI.Unshipped.txt.

KEY RULES:
- Join semantics: first BeginOrJoinAsync in a scope owns; later calls get a non-owning lease; non-owner CommitAsync is a
  no-op returning Right; non-owner RollbackAsync marks rollback-only; owner commit on rollback-only rolls back and
  returns Left(transaction.rollback_only).
- Implement the join/rollback-only state once (TransactionLeaseState or an abstract base) so EF Core and MongoDB
  reuse it.
- Async only (OpenAsync, BeginTransactionAsync, CommitAsync, RollbackAsync with the token); never IDbConnection.Open()
  or BeginTransaction() directly (AGENTS.md section 3).
- Errors carry the code and the exception type; EncinaError.Message never reaches logs.
- Disposing the owner lease without commit rolls back; disposing the accessor rolls back any open transaction.
- XML docs on every public member, with an <example> on IDbTransactionAccessor.

REFERENCE FILES:
- src/Encina.Messaging/TransactionPipelineBehavior.cs
- src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs (async begin/commit/rollback after #1325)
- src/Encina.DomainModeling/IUnitOfWork.cs (UnitOfWorkErrors pattern)
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (OutboxErrorCodes pattern, line 269)
```

</details>

---

### Phase 2: `Encina.Messaging.TransactionPipelineBehavior` on the accessor

> **Goal**: The shared ADO.NET/Dapper transaction behavior becomes a lease holder, so every participant sees its transaction.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Messaging/TransactionPipelineBehavior.cs`**
   - Constructor: `(IDbTransactionAccessor transactionAccessor, ILogger<TransactionPipelineBehavior<TRequest, TResponse>> logger)` (replaces the `IDbConnection` parameter).
   - `Handle` flow:
     - take a lease with `BeginOrJoinAsync`, then call `nextStep()`;
     - on `Right`, call `lease.CommitAsync`; a `Left` from the commit becomes the response;
     - on `Left` or an exception, call `lease.RollbackAsync`;
     - `await using` the lease.
   - Call the existing, unused `MessagingLog.TransactionStarted/Committed/RolledBack` (EventIds 2834-2836, `src/Encina.Messaging/MessagingLog.cs:203-231`). Exceptions go to the logger through `ex.ForLogging()`. This closes #1342's logging finding.
2. **Update the guard tests** in `tests/Encina.GuardTests/{ADO,Dapper}/{SqlServer,PostgreSQL,MySQL}/TransactionPipelineBehaviorGuard*Tests.cs` to the new constructor. Consider consolidating them per #1340.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #718 in Encina: Encina.Messaging.TransactionPipelineBehavior on IDbTransactionAccessor.

CONTEXT:
- Phase 1 added ITransactionLeaseSource, IDbTransactionAccessor and DbTransactionAccessor in src/Encina.Messaging/Transactions/.
- src/Encina.Messaging/TransactionPipelineBehavior.cs begins a transaction on IDbConnection and keeps it local; it never
  calls the MessagingLog methods declared for it (EventIds 2834-2836, MessagingLog.cs:203-231).

TASK:
Rewrite TransactionPipelineBehavior<TRequest, TResponse> to depend on IDbTransactionAccessor and ILogger, take a lease,
commit on Right (a Left from commit replaces the response), roll back on Left or exception, and log through
MessagingLog. Update the six guard-test files.

KEY RULES:
- Never begin a second transaction: join when one is active.
- Cleanup commit/rollback with CancellationToken.None; exceptions logged via ex.ForLogging(); never EncinaError.Message.
- Unit tests first (tests/Encina.UnitTests/Messaging/Behaviors/TransactionPipelineBehaviorTests.cs): owner commit,
  joiner no-op, Left -> rollback, exception -> rollback + Left, cancellation -> rollback + rethrow, commit Left surfaces.
- CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Messaging/TransactionPipelineBehavior.cs
- src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs (logging and reuse pattern)
- src/Encina.Messaging/MessagingLog.cs
- tests/Encina.UnitTests/Messaging/Behaviors/TransactionPipelineBehaviorTests.cs
```

</details>

---

### Phase 3: Transactional outbox pipeline behavior

> **Goal**: Notifications of `IHasNotifications` requests are written inside the request's transaction on any provider that supplies an `ITransactionLeaseSource`; a failed write fails the request.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Outbox/OutboxWriter.cs`**: `internal sealed class OutboxWriter`, extracted from `OutboxPostProcessor.Process` (`OutboxPostProcessor.cs:63-100`).
   - Serializes each notification with `IMessageSerializer.SerializeAsRuntimeType` and creates the messages with `IOutboxMessageFactory` and `TimeProvider`.
   - Calls `IOutboxStore.AddAsync` and `SaveChangesAsync` and **returns the first `Left`**.
   - Signature: `ValueTask<Either<EncinaError, int>> WriteAsync(IReadOnlyList<INotification>, IRequestContext, CancellationToken)`.
2. **`src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs`**: `public sealed class TransactionalOutboxPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>`.
   - Constructor: `(ITransactionLeaseSource transactionSource, IOutboxStore outboxStore, IOutboxMessageFactory messageFactory, IMessageSerializer messageSerializer, ILogger<...> logger, TimeProvider? timeProvider = null)`.
   - Skip path: a static per-generic-type flag (`typeof(IHasNotifications).IsAssignableFrom(typeof(TRequest))`, or `TRequest` is an interface or abstract) plus the runtime `request is IHasNotifications` check. When neither applies, `return await nextStep()`.
   - Flow:
     1. `BeginOrJoinAsync`.
     2. `nextStep()`. On `Left`, roll back and return it (no outbox row).
     3. Read `GetNotifications()` after the handler has run.
     4. `OutboxWriter.WriteAsync`. On `Left`, roll back and return the outbox error.
     5. `lease.CommitAsync`. Return the handler's response or the commit `Left`.
3. **Move `IHasNotifications`** from `OutboxPostProcessor.cs:117` to `src/Encina.Messaging/Outbox/IHasNotifications.cs`. The deletion in Phase 9 depends on this move.
4. **Interim**: `OutboxPostProcessor` delegates to `OutboxWriter` so the providers not yet migrated keep their current behavior until Phases 7-8. This is the status quo, not a regression, and Phase 9 deletes it.
5. **`PublicAPI.Unshipped.txt`**: the behavior.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #718 in Encina: the transactional outbox pipeline behavior.

CONTEXT:
- OutboxPostProcessor (src/Encina.Messaging/Outbox/OutboxPostProcessor.cs) runs after every behavior because
  PipelineBuilder wraps post-processors outermost (src/Encina/Pipeline/PipelineBuilder.cs:83, 112-114); the transaction
  is already committed when it writes, and it discards the Either of AddAsync (line 95) and SaveChangesAsync (line 98).
- Phase 1 added ITransactionLeaseSource; every provider will register one (Phases 5-8).

TASK:
1. Extract serialization + store calls into internal OutboxWriter returning the first Left.
2. Create TransactionalOutboxPipelineBehavior<TRequest, TResponse> on ITransactionLeaseSource (join-or-begin, handler,
   write notifications, commit; any Left rolls back and is returned).
3. Move IHasNotifications to its own file; make OutboxPostProcessor delegate to OutboxWriter (interim, deleted in Phase 9).

KEY RULES:
- Requests that cannot implement IHasNotifications skip with zero allocations (static per-type flag).
- Notifications are read after the handler returns.
- Handler Left -> no outbox row, rollback; outbox or commit Left is returned (fail closed, AGENTS.md section 3).
- The behavior depends only on ITransactionLeaseSource, never on IDbConnection or MongoDB types.
- Serialization stays on IMessageSerializer.SerializeAsRuntimeType (#1168).

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxPostProcessor.cs
- src/Encina.Messaging/Inbox/ (InboxPipelineBehavior: behavior shape and static caching)
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (OutboxErrorCodes)
- tests/Encina.UnitTests/Messaging/Pipeline/OutboxPostProcessorTests.cs
```

</details>

---

### Phase 4: Configuration & DI (shared)

> **Goal**: The shared registrations wire the outbox behavior. Each provider registers its own `ITransactionLeaseSource`, and a provider switches to the behavior in its own phase.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`**
   - `RegisterOutbox` (line 265) gains an internal `OutboxWritePath` parameter (`TransactionalBehavior` or `PostProcessor`). `AddOutboxInboxSagaSchedulingServices` (line 203) passes it through from its callers.
   - `AddMessagingServices` (line 44, ADO.NET + Dapper) and `AddMessagingServicesCore` (line 358, the ADO.NET tenancy entry point used by `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Tenancy/TenancyServiceCollectionExtensions.cs`, which also calls `RegisterTransactions` at line 382):
     - `TryAddScoped<IDbTransactionAccessor, DbTransactionAccessor>()`;
     - `TryAddScoped<ITransactionLeaseSource>(sp => sp.GetRequiredService<IDbTransactionAccessor>())`;
     - path `TransactionalBehavior`.
   - EF Core and MongoDB keep `PostProcessor` until Phases 7 and 8 switch them. Phase 9 deletes the parameter.
2. **DI completeness tests** (AGENTS.md §3 "Registration completeness"): for each of the six ADO.NET/Dapper providers, build the provider with `ValidateOnBuild = true, ValidateScopes = true` for `UseOutbox`, `UseTransactions` and both. Closes #1342's DI-test finding.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #718 in Encina: shared DI wiring of the transactional outbox.

CONTEXT:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs: AddMessagingServices (line 44, ADO.NET + Dapper) and
  AddMessagingServicesCore (line 358, ADO.NET tenancy) call the shared AddOutboxInboxSagaSchedulingServices (line 203,
  also used by EF Core and MongoDB), whose RegisterOutbox registers OutboxPostProcessor<,> at line 265.
- Phases 1-3 added DbTransactionAccessor, ITransactionLeaseSource and TransactionalOutboxPipelineBehavior<,>.

TASK:
Add an internal write-path switch to RegisterOutbox; register IDbTransactionAccessor and ITransactionLeaseSource and
the transactional path from AddMessagingServices and AddMessagingServicesCore; leave EF Core and MongoDB on the
post-processor path for now. Add ValidateOnBuild + ValidateScopes DI tests for the six ADO.NET/Dapper providers.

KEY RULES:
- Nothing new is registered when no messaging pattern is enabled.
- TryAdd so an application's own registration wins.
- Tests use the real AddEncinaADO / AddEncinaDapper extensions.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs (lines 43-87, 254-294)
- src/Encina.ADO.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs
- Existing DI completeness tests from #1260/#1273 (search tests/ for ValidateOnBuild)
```

</details>

---

### Phase 5: ADO.NET providers (SqlServer, PostgreSQL, MySQL)

> **Goal**: The unit of work and the messaging stores of the three ADO.NET packages participate in the accessor's transaction.

<details>
<summary><strong>Tasks</strong></summary>

For each of `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/`:

1. **`UnitOfWork/UnitOfWorkADO.cs`**
   - New constructor: `(IDbTransactionAccessor transactionAccessor, IServiceProvider serviceProvider)`.
   - `BeginTransactionAsync` takes a lease; `TransactionAlreadyActive` only when this unit of work already holds one.
   - Commit, rollback and dispose go through the lease, asynchronously. This replaces SqlServer `:153,175,270` and MySQL `:161,183,278` (#1418).
   - `CurrentTransaction` reads `transactionAccessor.Current`.
2. **`UnitOfWork/UnitOfWorkRepositoryADO.cs`**: keep `command.Transaction = _unitOfWork.CurrentTransaction` (`:879` in SqlServer).
3. **`Outbox/OutboxStoreADO.cs`**
   - Constructor gains `IDbTransactionAccessor? transactionAccessor = null`.
   - Every command sets `command.Transaction = _transactionAccessor?.Current`.
   - `RequeueExhaustedAsync` joins an active transaction instead of beginning its own (`OutboxStoreADO.cs:279`).
   - Replace the no-op `OpenConnectionAsync` (`OutboxStoreADO.cs:340-345`) with a real `OpenAsync` (#1170/#1868).
4. **`Inbox/InboxStoreADO.cs`, `Sagas/SagaStoreADO.cs`, `Scheduling/ScheduledMessageStoreADO.cs`**: the same enlistment.
5. **`ServiceCollectionExtensions.cs`**: `AddEncinaUnitOfWork` (SqlServer `:508-513`) also registers the accessor and `ITransactionLeaseSource`.
6. **Module isolation**: confirm that the accessor's async calls go through `SchemaValidatingConnection`'s async overrides and that `SchemaValidatingCommand.Transaction` forwards to the inner command.
7. One enlistment helper per package (or an `internal` helper in `Encina.Messaging` with `InternalsVisibleTo`), not one copy per store.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #718 in Encina: the three ADO.NET providers join the shared transaction.

CONTEXT:
- Phases 1-4 added IDbTransactionAccessor (scoped, owns the transaction of the scoped IDbConnection) and wired it.
- OutboxStoreADO builds commands without command.Transaction (src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:116);
  SqlClient and MySqlConnector throw when the connection has a pending transaction; Npgsql runs it inside it. Its
  OpenConnectionAsync (line 340) never opens the connection.
- UnitOfWorkADO owns a private transaction (SqlServer UnitOfWorkADO.cs:62, 153, 175).

TASK:
For SqlServer, PostgreSQL and MySQL: move UnitOfWorkADO onto the accessor; enlist every command of OutboxStoreADO,
InboxStoreADO, SagaStoreADO and ScheduledMessageStoreADO; make RequeueExhaustedAsync join an active transaction; open
closed connections with OpenAsync; register the accessor from AddEncinaUnitOfWork.

KEY RULES:
- All three providers in the same change; SQL dialects untouched.
- Async only with CancellationToken (removes the UoW/outbox lines of #1418 for these packages).
- The accessor parameter of the stores is optional: with none, Current is null and commands run in autocommit
  (background processor scopes, standalone store tests).
- Do not change audit, anonymization or ABAC stores (#1934).

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, UnitOfWork/UnitOfWorkADO.cs, UnitOfWork/UnitOfWorkRepositoryADO.cs
- src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs (async pattern from #1325)
- src/Encina.ADO.SqlServer/Modules/SchemaValidatingConnection.cs, SchemaValidatingCommand.cs
```

</details>

---

### Phase 6: Dapper providers (SqlServer, PostgreSQL, MySQL) — #719

> **Goal**: The same participation for the three Dapper packages, with every Dapper call passing the transaction explicitly (#719 acceptance criterion).

<details>
<summary><strong>Tasks</strong></summary>

For each of `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/`:

1. **`UnitOfWork/UnitOfWorkDapper.cs`**: move onto the accessor, as in Phase 5. This replaces the synchronous calls at SqlServer `UnitOfWorkDapper.cs:146,168,263` (#1418).
2. **`UnitOfWork/UnitOfWorkRepositoryDapper.cs`**: it already passes `CurrentTransaction` (21 uses per file); back it with the accessor.
3. **`Outbox/OutboxStoreDapper.cs`**: every `ExecuteAsync`/`QueryAsync` passes `transaction: _transactionAccessor?.Current` through `CommandDefinition`. Today none does (`OutboxStoreDapper.cs:56`). `RequeueExhaustedAsync` joins an active transaction (`:210-224`).
4. **The inbox, saga and scheduling Dapper stores**: the same.
5. **`ServiceCollectionExtensions.cs`**: `AddEncinaUnitOfWork` (SqlServer `:489`) registers the accessor.

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

### Phase 7: EF Core providers (SqlServer, PostgreSQL, MySQL)

> **Goal**: On EF Core, the outbox rows are saved by `SaveChangesAsync` inside the request's `IDbContextTransaction`. EF's transaction behavior and `UnitOfWorkEF` share one owner.

<details>
<summary><strong>Tasks</strong></summary>

#### How EF Core enlists

- All EF stores and repositories use the same scoped `DbContext` (`OutboxStoreEF.cs:38`; `TryAddScoped<DbContext>` at `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:153`). Every command they run, including `SaveChangesAsync` and `ExecuteUpdateAsync`, runs in `DbContext.Database.CurrentTransaction`. EF Core therefore needs no per-store enlistment, only a single owner.
- `OutboxWriter` already calls `IOutboxStore.SaveChangesAsync` (`OutboxStoreEF.cs:198-203`). Inside the lease this saves the outbox entities, and any business changes the handler did not save yet, in the same transaction.

#### Files

1. **`src/Encina.EntityFrameworkCore/Transactions/DbContextTransactionAccessor.cs`**: `public sealed class DbContextTransactionAccessor : IDbTransactionAccessor`.
   - Constructor: `(DbContext dbContext, ILogger<DbContextTransactionAccessor> logger)`.
   - `Connection` returns `dbContext.Database.GetDbConnection()`.
   - `Current` returns `dbContext.Database.CurrentTransaction?.GetDbTransaction()`, so raw ADO.NET/Dapper code in an EF handler can join.
   - `BeginOrJoinAsync`:
     - if `Database.CurrentTransaction` already exists and the accessor does not own it (the application began it itself), return a non-owning lease;
     - otherwise call `Database.BeginTransactionAsync(isolationLevel, ct)` and wrap the `IDbContextTransaction` in the owner lease;
     - reuse `TransactionLeaseState` from Phase 1.
   - Owner commit: `IDbContextTransaction.CommitAsync`; rollback: `RollbackAsync`; dispose: `DisposeAsync`.
2. **Modify `src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs`**
   - Constructor: `(IDbTransactionAccessor transactionAccessor, ILogger<...> logger)`.
   - Keep the opt-in through `ITransactionalCommand` / `[Transaction]` (`:190-201`) and the isolation level (`:203-209`).
   - Replace the "reuse existing transaction" branch (`:89-94`) and `ExecuteInNewTransactionAsync` with a lease: a joiner logs `ReusingExistingTransaction`, and only the owner commits.
   - Cache `RequiresTransaction` and `GetIsolationLevel` per generic type in static fields. Today they reflect on every call (`:197`, `:205`), against the house rule "static per-generic-type attribute caching".
   - Rollback paths use `CancellationToken.None`. Today they use the caller token (`:134`, `:142`, `:173`, `:186`).
3. **Modify `src/Encina.EntityFrameworkCore/UnitOfWork/UnitOfWorkEF.cs`**: constructor `(DbContext dbContext, IDbTransactionAccessor transactionAccessor, IServiceProvider serviceProvider)`. `BeginTransactionAsync` (`:215-232`), `CommitAsync` (`:239-256`), rollback (`:263`, `:311-320`) and dispose (`:291`, `:332-340`) go through the lease. This fixes defect 4: `UnitOfWorkEF` inside a `[Transaction]` request no longer calls `BeginTransactionAsync` on a context that already has a transaction.
4. **Execution strategy**: a retrying execution strategy (`EnableRetryOnFailure` on SqlServer, Npgsql or Pomelo) rejects user-initiated transactions with `InvalidOperationException`. Encina never configures one (no match for `EnableRetryOnFailure` in `src/`), but applications do.
   - When `Database.CreateExecutionStrategy().RetriesOnFailure` is true, the owner returns `Left(transaction.not_supported)` with a clear code.
   - Add an option-validation test that documents the limit.
   - The alternative (wrap the pipeline in `strategy.ExecuteAsync`, which re-runs the handler) is listed under Phase 7's open question in the Research section.
5. **Modify `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs`**:
   - `AddEncinaEntityFrameworkCore` (`:131-200`): `TryAddScoped<IDbTransactionAccessor, DbContextTransactionAccessor>()`, `TryAddScoped<ITransactionLeaseSource>(...)`, and the `TransactionalBehavior` write path in the `AddOutboxInboxSagaSchedulingServices` call (`:165`);
   - the overload at `:530` and `AddEncinaUnitOfWork<TDbContext>` (`:690-698`) register the accessor too.
6. **Domain events**: `DomainEventDispatcherInterceptor` dispatches in `SavedChangesAsync` (`src/Encina.EntityFrameworkCore/DomainEvents/DomainEventDispatcherInterceptor.cs:150`). Inside the lease it now dispatches before the commit. Document this. Moving domain events into the outbox is #1236, which builds on this accessor.
7. **README**: `src/Encina.EntityFrameworkCore/README.md:135,145` describe the post-processor; rewrite them.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #718 in Encina: EF Core (SqlServer, PostgreSQL, MySQL) writes the outbox in the
request's IDbContextTransaction.

CONTEXT:
- EF stores and repositories share the scoped DbContext (src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:153;
  OutboxStoreEF.cs:38), so their commands already run in DbContext.Database.CurrentTransaction; the outbox is not atomic
  only because OutboxPostProcessor runs after EF's TransactionPipelineBehavior committed
  (src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs:156-175).
- UnitOfWorkEF owns a private IDbContextTransaction (UnitOfWorkEF.cs:84, 226) and checks only its own field (:219).
- Phases 1-6 added ITransactionLeaseSource, IDbTransactionAccessor, TransactionLeaseState and the outbox behavior.

TASK:
Create DbContextTransactionAccessor (IDbTransactionAccessor over DatabaseFacade: GetDbConnection, CurrentTransaction,
BeginTransactionAsync, GetDbTransaction); move EF's TransactionPipelineBehavior and UnitOfWorkEF onto it; switch
AddEncinaEntityFrameworkCore (both overloads) and AddEncinaUnitOfWork to the transactional outbox path; handle retrying
execution strategies with Left(transaction.not_supported); update the EF README.

KEY RULES:
- One owner per scope: a transaction the application began itself is joined, never committed by Encina.
- Keep the ITransactionalCommand / [Transaction] opt-in and isolation level; cache both per generic type in static fields.
- Rollback/cleanup with CancellationToken.None; log codes only, exceptions via ForLogging().
- All three EF providers are covered by the same package; integration tests run on EFCore-SqlServer, EFCore-PostgreSQL
  and EFCore-MySQL collections.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs
- src/Encina.EntityFrameworkCore/UnitOfWork/UnitOfWorkEF.cs
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (lines 131-215, 530-541, 690-698)
- src/Encina.EntityFrameworkCore/DomainEvents/DomainEventDispatcherInterceptor.cs
```

</details>

---

### Phase 8: MongoDB provider

> **Goal**: On MongoDB, the outbox write and the business writes share one `IClientSessionHandle` transaction. Without a replica set, the request fails closed.

<details>
<summary><strong>Tasks</strong></summary>

#### How MongoDB enlists

- MongoDB multi-document transactions need a session and a replica set or sharded cluster. `UnitOfWorkMongoDB.BeginTransactionAsync` already maps the standalone `NotSupportedException` to `TransactionStartFailed` (`src/Encina.MongoDB/UnitOfWork/UnitOfWorkMongoDB.cs:181-189`).
- Every write that must be atomic has to pass the session (`InsertOneAsync(session, ...)`, `UpdateOneAsync(session, ...)`). Unlike EF Core, enlistment is per call.

#### Files

1. **`src/Encina.MongoDB/Transactions/IMongoSessionAccessor.cs`, `MongoSessionAccessor.cs`**: `public interface IMongoSessionAccessor : ITransactionLeaseSource { IClientSessionHandle? CurrentSession { get; } }` and its scoped implementation.
   - Constructor: `(IMongoClient mongoClient, ILogger<MongoSessionAccessor> logger)`.
   - Owner `BeginOrJoinAsync`: `StartSessionAsync(ct)` followed by `StartTransaction()`.
   - Owner commit: `CommitTransactionAsync(ct)`; rollback: `AbortTransactionAsync(CancellationToken.None)`.
   - Reuses `TransactionLeaseState`.
   - A standalone server returns `Left(transaction.not_supported)`. Never a silent non-transactional write (fail closed, AGENTS.md §3).
2. **Modify `UnitOfWork/UnitOfWorkMongoDB.cs`**: constructor gains `IMongoSessionAccessor`. `BeginTransactionAsync` (`:165-192`), `CommitAsync` (`:198-216`) and rollback go through the lease. `CurrentSession` (`:105`) reads the accessor, so `UnitOfWorkRepositoryMongoDB` (`:80,102,127`) keeps working.
3. **Modify `Outbox/OutboxStoreMongoDB.cs`** and the inbox, saga and scheduled-message MongoDB stores.
   - Constructor gains `IMongoSessionAccessor? sessionAccessor = null`.
   - Every write uses the session overload when `CurrentSession` is not null. Today `OutboxStoreMongoDB.cs:61,106,134,205` pass no session.
4. **Modify `ServiceCollectionExtensions.cs`**:
   - `RegisterCommonServices` (`:616`) registers `IMongoSessionAccessor` and `ITransactionLeaseSource` (scoped).
   - `RegisterMessagingPatterns` (`:596`) uses the `TransactionalBehavior` write path.
   - `AddEncinaUnitOfWork` (`:491-496`) registers the accessor.
5. **Replica-set guard**: when `UseOutbox` is true, `MongoDbHealthCheck` (`src/Encina.MongoDB/Health/MongoDbHealthCheck.cs`) reports `Unhealthy` with a "transactions unavailable" reason on a standalone server (`hello` command, `setName` missing). The requirement is documented in the README and the feature guide.
6. **Transient errors**: a commit that fails with the `TransientTransactionError` or `UnknownTransactionCommitResult` label returns `Left(transaction.commit_failed)` with the label as metadata. Retrying is the caller's decision (Resilience is not applicable in the matrix).
7. **Remaining MongoDB stores** (audit, anonymization, ABAC) stay outside the session. Add them to #1934's scope.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #718 in Encina: MongoDB writes the outbox in the request's client-session transaction.

CONTEXT:
- OutboxStoreMongoDB writes without a session (src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs:61, 106, 134, 205);
  UnitOfWorkMongoDB starts a private session (UnitOfWorkMongoDB.cs:177-178) exposed only internally (:105).
- MongoDB transactions require a replica set; UnitOfWorkMongoDB maps the standalone error at :181-189.
- MongoDB has no IDbTransaction, so it implements the neutral ITransactionLeaseSource from Phase 1, not
  IDbTransactionAccessor. MongoDB has no UseTransactions flag or transaction behavior: the outbox behavior begins the
  session itself.

TASK:
Create IMongoSessionAccessor / MongoSessionAccessor (scoped; StartSessionAsync + StartTransaction; commit/abort through
TransactionLeaseState); move UnitOfWorkMongoDB onto it; pass the session in every write of the outbox, inbox, saga and
scheduled-message stores; register it and the transactional outbox path in RegisterCommonServices /
RegisterMessagingPatterns / AddEncinaUnitOfWork; make the health check report a standalone server when UseOutbox is on.

KEY RULES:
- Standalone server: Left(transaction.not_supported), never a silent non-transactional write (fail closed).
- Abort with CancellationToken.None; commit errors keep the TransientTransactionError / UnknownTransactionCommitResult
  label as metadata, never the message.
- Integration tests use the MongoDB-ReplicaSet collection (MongoDbReplicaSetFixture), plus one test on the standalone
  MongoDbFixture proving the fail-closed Left.

REFERENCE FILES:
- src/Encina.MongoDB/UnitOfWork/UnitOfWorkMongoDB.cs, UnitOfWorkRepositoryMongoDB.cs
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs
- src/Encina.MongoDB/ServiceCollectionExtensions.cs (lines 62-100, 491-496, 594-620)
- tests/Encina.TestInfrastructure/Fixtures/MongoDbReplicaSetFixture.cs
- tests/Encina.IntegrationTests/Infrastructure/MongoDB/ReadWriteSeparation/MongoDbReplicaSetCollection.cs
```

</details>

---

### Phase 9: Remove `OutboxPostProcessor` (#1935)

> **Goal**: One outbox write path. The non-atomic post-processor and its registration switch are deleted once all 10 providers use the behavior.

<details>
<summary><strong>Tasks</strong></summary>

1. **Delete `src/Encina.Messaging/Outbox/OutboxPostProcessor.cs`**. `IHasNotifications` was moved in Phase 3. Remove its `Log` partial class (EventIds 2842-2844) and the matching lines of `src/Encina.Messaging/PublicAPI.Unshipped.txt` (RS0017).
2. **Delete the `OutboxWritePath` switch** of `RegisterOutbox` (Phase 4). `RegisterOutbox` always registers `TransactionalOutboxPipelineBehavior<,>` and requires an `ITransactionLeaseSource` from the provider; the DI tests prove it on all 10 providers.
3. **Update references**:
   - `src/Encina.Cdc/Messaging/OutboxCdcHandler.cs:28` (XML comment);
   - `tests/Encina.UnitTests/Messaging/Pipeline/OutboxPostProcessorTests.cs`: delete it, its cases move to `TransactionalOutboxPipelineBehaviorTests`;
   - `tests/Encina.UnitTests/Messaging/Serialization/MessageSerializerRegistrationTests.cs`;
   - `tests/Encina.UnitTests/Messaging/MessagingServiceCollectionExtensionsCoreEquivalenceTests.cs`;
   - `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/PostgreSQL/Outbox/OutboxPostProcessorEncryptionEFPostgreSqlTests.cs`: rename it and point it at the behavior, keeping its encryption assertion (#1168);
   - `docs/INVENTORY.md`.
   - Leave `docs/releases/pre-v0.10.0/README.md` alone; it is historical.
4. **Architecture test**: no `IRequestPostProcessor` implementation in `Encina.Messaging` writes to `IOutboxStore`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #718 in Encina: remove OutboxPostProcessor (scope of #1935, closed as duplicate).

CONTEXT:
- After Phases 5-8 all 10 providers register an ITransactionLeaseSource and the TransactionalOutboxPipelineBehavior.
- OutboxPostProcessor (src/Encina.Messaging/Outbox/OutboxPostProcessor.cs) is referenced by MessagingServiceCollectionExtensions,
  PublicAPI.Unshipped.txt, Encina.Cdc/Messaging/OutboxCdcHandler.cs:28, the EF Core README, docs/INVENTORY.md and four
  test files.

TASK:
Delete OutboxPostProcessor and its Log class (EventIds 2842-2844), the interim write-path switch, and every reference
listed in Phase 9 Tasks; port its unit-test cases and the EF PostgreSQL encryption integration test to the behavior;
add an architecture test that no post-processor writes to IOutboxStore.

KEY RULES:
- No [Obsolete], no alias: delete completely (pre-1.0).
- IHasNotifications stays public in src/Encina.Messaging/Outbox/IHasNotifications.cs.
- Do not edit historical release notes.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxPostProcessor.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/PostgreSQL/Outbox/OutboxPostProcessorEncryptionEFPostgreSqlTests.cs
- tests/Encina.UnitTests/Messaging/MessagingServiceCollectionExtensionsCoreEquivalenceTests.cs
```

</details>

---

### Phase 10: Cross-Cutting Integration

> **Goal**: The transactional path keeps tenancy, module isolation, the transactions pattern and the inbox coherent on all 10 providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Transactions**: test the composition matrix on each family: outbox only; transaction behavior only (ADO.NET/Dapper `UseTransactions`, EF `[Transaction]`); both; `IUnitOfWork` inside either (Design Choice 5).
2. **Multi-tenancy**: the accessors wrap the scoped connection, `DbContext` or client, so the tenant connection chosen by each package's `Tenancy/TenancyServiceCollectionExtensions.cs` is the one used. Add one test per family proving that the outbox row and the business row land on the same tenant store. `TenantId` on outbox rows is out of scope (#1257, #1164).
3. **Module isolation**: ADO.NET/Dapper with `UseModuleIsolation = true` (Phase 5 task 6); EF Core and MongoDB module isolation do not change connections.
4. **Read/write separation and sharding**: replica and shard connections, contexts and clients are not enlisted. Document the limit (#1934).
5. **Inbox**: `InboxPipelineBehavior` joins the same transaction when it runs inside it; one test per family.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of issue #718 in Encina: cross-cutting integration of the transactional outbox.

CONTEXT:
- Every provider family supplies an ITransactionLeaseSource (DbTransactionAccessor, DbContextTransactionAccessor,
  MongoSessionAccessor); messaging stores and units of work enlist; TransactionalOutboxPipelineBehavior writes inside it.

TASK:
Add the composition tests per family, one tenancy test and one inbox-in-transaction test per family, one
module-isolation test for ADO.NET/Dapper, and document the read/write-separation and sharding limit.

KEY RULES:
- Real databases through shared collections: ADO-<Db>, Dapper-<Db>, EFCore-<Db>, MongoDB-ReplicaSet.
- Do not add TenantId to outbox rows (#1257); prove only that the same connection/context/client is used.
- Record every new limit as a follow-up issue file, not a TODO.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs
- src/Encina.MongoDB/Tenancy/TenancyServiceCollectionExtensions.cs
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (RegisterTenancy)
- src/Encina.Messaging/Inbox/ (InboxPipelineBehavior)
```

</details>

---

### Phase 11: Observability

> **Goal**: The transaction boundary and the outbox write are traced, metered and logged on every provider.

<details>
<summary><strong>Tasks</strong></summary>

1. **Event IDs**. All ranges below are already registered in `src/Encina/Diagnostics/EventIdRanges.cs`, so no new range is needed:

   | Range | Highest used today | New IDs, packed sequentially |
   |-------|--------------------|------------------------------|
   | `Messaging` 2800-2999 (`:126`) | 2962 | **2963-2969**: `TransactionJoined` (Debug), `TransactionMarkedRollbackOnly` (Warning), `TransactionRollbackOnlyCommitRefused` (Warning), `TransactionCommitFailed` (Error, `ForLogging()`), `OutboxNotificationsWritten` (Debug), `OutboxWriteFailed` (Error, code only), `OutboxBehaviorSkipped` (Trace) |
   | `EntityFrameworkCore` 3000-3099 (`:129`) | 3060 | **3061-3062**: `ExecutionStrategyRejectsTransaction` (Warning), `ExternalTransactionJoined` (Debug) |
   | `MongoDB` 3100-3199 (`:132`) | 3162 | **3163-3164**: `TransactionsUnavailable` (Error), `TransientTransactionLabel` (Warning) |

   - Reuse 2834-2836 for begin, commit and rollback.
   - 2842-2844 are freed by Phase 9. Do not reuse them in the same release.
2. **Tracing**:
   - `OutboxActivitySource` (`src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs:21`) gets `StartEnqueue(int notificationCount)`.
   - A new `Encina.Messaging.Transactions` activity covers begin, commit and rollback in `TransactionLeaseState`, with tags `encina.transaction.owner`, `encina.transaction.outcome`, `db.system`.
   - No payloads.
3. **Metrics**, on the existing outbox meter (`OutboxProcessorMetrics.cs`):
   - `encina.outbox.enqueued` (`Counter<long>`, tags `outcome`, `db.system`);
   - `encina.transaction.rollback_only` (`Counter<long>`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of issue #718 in Encina: observability for the transactional outbox on 10 providers.

CONTEXT:
- Ranges in src/Encina/Diagnostics/EventIdRanges.cs: Messaging (2800-2999, highest used 2962), EntityFrameworkCore
  (3000-3099, highest 3060), MongoDB (3100-3199, highest 3162). MessagingLog declares 2834-2836 for transactions.
- OutboxActivitySource and OutboxProcessorMetrics exist in src/Encina.Messaging/Diagnostics/.

TASK:
Add the [LoggerMessage] methods 2963-2969, 3061-3062 and 3163-3164 listed in Phase 11 Tasks, the Enqueue span, the
transaction activity in TransactionLeaseState and the two counters; call them from the accessors and the behavior.

KEY RULES:
- Verify each "highest used" figure again before allocating; pack IDs sequentially; run
  tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
- No EncinaError.Message, payload or notification content in logs, tags or metrics.
- Activities and counters exercised by unit tests with an in-memory listener.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Messaging/MessagingLog.cs
- src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, OutboxProcessorMetrics.cs
- src/Encina.EntityFrameworkCore/ and src/Encina.MongoDB/ existing Log classes
```

</details>

---

### Phase 12: Testing

> **Goal**: Prove commit and rollback atomicity on all 10 providers and meet every coverage flag.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests**:
   - `TransactionLeaseStateTests`, `DbTransactionAccessorTests`, `DbContextTransactionAccessorTests` (SQLite in-memory is allowed here as a unit-level relational stand-in; no provider claim), `MongoSessionAccessorTests` (mocked `IMongoClient`);
   - `TransactionalOutboxPipelineBehaviorTests`, covering the skip path, a handler `Left`, a write `Left`, a commit `Left`, an exception and cancellation;
   - `OutboxWriterTests`;
   - updated `TransactionPipelineBehaviorTests` for both behaviors.
2. **Guard tests**: every new or changed public constructor and method in `tests/Encina.GuardTests/{Messaging,ADO,Dapper,Infrastructure/EntityFrameworkCore,Infrastructure/MongoDB}/`.
3. **Contract tests** (`tests/Encina.ContractTests/Messaging/`): one `ITransactionLeaseSource` contract (owner/joiner/rollback-only) run against all 10 providers; extend both `TransactionPipelineBehaviorContractTests`.
4. **Property tests** (FsCheck):
   - for any sequence of joins, commits and rollbacks, the transaction commits if and only if the owner commits and no participant rolled back;
   - for any notification count n ≥ 0, a committed request leaves n rows and a rolled-back one leaves 0.
5. **Integration tests**: `OutboxAtomicityTests` per provider, 10 files.
   - Placement:
     - `tests/Encina.IntegrationTests/{ADO,Dapper}/{SqlServer,PostgreSQL,MySQL}/Outbox/`;
     - `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/Outbox/`;
     - `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Outbox/`, on `[Collection("MongoDB-ReplicaSet")]`.
   - Shared collection fixtures, `ClearAllDataAsync` in `InitializeAsync`.
   - Scenarios:
     - commit: n outbox rows;
     - handler `Left`: no row;
     - handler throws: no row;
     - outbox write fails (duplicate `Id`): the business change is rolled back and the request returns `Left`;
     - transaction behavior + outbox: a single transaction;
     - `IUnitOfWork` inside the request joins, and an inner rollback makes the request fail with `transaction.rollback_only`;
     - EF Core: a retrying execution strategy returns `Left(transaction.not_supported)`;
     - MongoDB standalone (`MongoDbFixture`): `Left(transaction.not_supported)` and no document written.
6. **Load tests**: `.md` justification (`tests/Encina.LoadTests/Messaging/OutboxAtomicity.md`). There is one transaction per scope and no state shared across requests.
7. **Benchmark tests**: benchmark the skip path (a request without `IHasNotifications`) against no behavior in `tests/Encina.BenchmarkTests/Encina.ADO.SqlServer.Benchmarks/`, for the "no regression" criterion. Justify the rest in `.md`.
8. **Coverage**: per-file targets with justifications in `.github/coverage-manifest/Encina.Messaging.json`, the six ADO.NET/Dapper manifests, `Encina.EntityFrameworkCore.json` and `Encina.MongoDB.json`. Measure per flag (AGENTS.md §9); CRAP ≤ 10 on changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 12</strong></summary>

```
You are implementing Phase 12 of issue #718 in Encina: tests for outbox atomicity on all 10 providers.

CONTEXT:
- Phases 1-11 are implemented: lease abstractions, three accessors (relational, EF Core, MongoDB), the transactional
  outbox behavior, accessor-based transaction behaviors and units of work, enlisted messaging stores, and
  OutboxPostProcessor removed.
- No existing test asserts outbox rollback on any provider.

TASK:
Write the unit, guard, contract, property and integration tests and the load/benchmark items of Phase 12 Tasks; add
per-file coverage targets with justifications to the manifests and measure every flag.

KEY RULES:
- Tests execute real code; Shouldly through Encina.Testing.Shouldly; no Thread.Sleep.
- Integration tests use shared collections (ADO-<Db>, Dapper-<Db>, EFCore-<Db>, MongoDB-ReplicaSet); never
  IClassFixture, never dispose the fixture.
- Every scenario runs on all 10 providers; MongoDB transactional tests require the replica-set fixture.
- Measure with dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory
  artifacts\coverage\<Flag>Tests and .github/scripts/coverage-report.cs; outputs only under artifacts/.

REFERENCE FILES:
- tests/Encina.IntegrationTests/ADO/SqlServer/Outbox/OutboxStoreADOTests.cs
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Collections.cs
- tests/Encina.TestInfrastructure/Fixtures/MongoDbReplicaSetFixture.cs, MongoDbFixture.cs
- docs/testing/integration-tests.md, docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 13: Documentation & Finalization

> **Goal**: Users know how atomicity works on each provider, how to join the transaction in their own handlers, and what stays outside it.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation** on every new or changed public API, with handler examples:
   - raw ADO.NET: `command.Transaction = accessor.Current`;
   - Dapper: `transaction: accessor.Current`;
   - EF Core: no code needed;
   - MongoDB: `collection.InsertOneAsync(sessionAccessor.CurrentSession, ...)`.
2. **Changelog fragments**:
   - `changelog.d/718-outbox-atomicity.fixed.md` (lost events on all 10 providers);
   - `changelog.d/718-outbox-post-processor.removed.md` (#1935);
   - a `changed` fragment for the constructor changes of both `TransactionPipelineBehavior`s and the four units of work.
3. **ADR**: `docs/architecture/adr/037-transactional-outbox.md` (next free number; check `docs/architecture/adr/index.md`). It records Design Choices 1-5, the neutral lease contract, the EF execution-strategy limit and the MongoDB replica-set requirement. Add it to the index.
4. **Feature guide**: `docs/features/transactional-outbox.md`, a how-to page per the encina-docs skill. It covers the composition matrix, a provider table, handler examples and the limits (read/write separation, sharding, MongoDB standalone, EF retrying strategies).
5. **Package READMEs**: `src/Encina.Messaging/README.md`, the six ADO.NET/Dapper READMEs, `src/Encina.EntityFrameworkCore/README.md` (`:135,145`) and `src/Encina.MongoDB/README.md`.
6. **`docs/INVENTORY.md`**: new and deleted files.
7. **`ROADMAP.md`**: the v0.14.0 hardening item, if listed.
8. **PublicAPI**: the `PublicAPI.Unshipped.txt` of `Encina.Messaging`, the six ADO.NET/Dapper packages, `Encina.EntityFrameworkCore` and `Encina.MongoDB`.
9. **Build verification**: `dotnet build Encina.slnx --configuration Release` gives 0 errors and 0 warnings.
10. **Test verification**: `dotnet test` passes, and every coverage flag (unit, guard, contract, property, integration) reaches its own target in `.github/coverage-manifest/{Package}.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 13</strong></summary>

```
You are implementing Phase 13 of issue #718 in Encina: documentation and finalization.

CONTEXT:
- The transactional outbox is implemented and tested on all 10 providers and OutboxPostProcessor is gone (Phases 1-12).
- Documentation follows the encina-docs skill (Diataxis, one quadrant per page, no hand-typed coverage figures).

TASK:
Complete the XML docs, changelog fragments, ADR 037 (or the next free number),
docs/features/transactional-outbox.md, the nine package READMEs, docs/INVENTORY.md, ROADMAP.md and PublicAPI files; run
the Release build and the full test suite.

KEY RULES:
- Never edit CHANGELOG.md [Unreleased] by hand; use changelog.d/ fragments.
- English only; no emojis; cite coverage with covref markers.
- Zero warnings; PublicAPI lines in the Namespace.Type.Member(params) -> ReturnType format.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- docs/architecture/adr/index.md, docs/architecture/adr/036-three-audit-stores.md
- docs/features/pipeline-behaviors.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Topic | Relevance |
|--------|-------|-----------|
| Transactional Outbox pattern (microservices.io, C. Richardson) | Business change and message row in one local transaction | The guarantee #718 restores |
| MongoDB multi-document transactions | Need a replica set or sharded cluster; session per transaction; `TransientTransactionError` / `UnknownTransactionCommitResult` labels | Phase 8 design and fail-closed behavior |
| EF Core connection resiliency | Retrying execution strategies reject user-initiated transactions unless wrapped in `ExecuteAsync` | Phase 7 limit |
| ADR-001 / ADR-006 (Railway Oriented Programming) | `Either<EncinaError, T>` everywhere | Lease, accessors and behavior return `Either` |
| ADR-018 (cross-cutting integration) | 12 transversal functions | Matrix below |
| ADR-021 (EventId ranges) | Registered ranges, packed IDs | Phase 11 |
| ADR-024 (SQLite out of the matrix) | Provider count | 10 providers |
| AGENTS.md §3 | Async DB calls, errors never swallowed, registration completeness, fail closed | Phases 1-9 |
| SPEC-002 REQ-043 / #1236 | Domain events to the outbox in the same commit | Builds on these accessors |

### Provider Transaction Semantics

| Provider family | Enlistment mechanism | Behavior today | After this plan |
|-----------------|----------------------|----------------|-----------------|
| ADO.NET / Dapper on SqlClient | `command.Transaction` / `transaction:` | A command without the pending transaction throws | Every messaging command carries `accessor.Current` |
| ADO.NET / Dapper on MySqlConnector | Same | Throws unless `IgnoreCommandTransaction=true` | Same |
| ADO.NET / Dapper on Npgsql | Same | Runs in the connection's transaction implicitly | Explicit, same as the others |
| EF Core ×3 | Shared scoped `DbContext` + `Database.CurrentTransaction` | Enlisted, but the outbox is saved after commit (defect 1) | Saved inside the lease |
| MongoDB | `IClientSessionHandle` passed per call | No session in messaging stores | Session from `IMongoSessionAccessor`; standalone fails closed |

### Open Questions for the EF Core and MongoDB Phases

| Question | Phase | Plan default | Alternative |
|----------|-------|--------------|-------------|
| Retrying EF execution strategy | 7 | `Left(transaction.not_supported)` with a clear code and a documented limit | Wrap the pipeline in `strategy.ExecuteAsync`; the handler re-runs on retry, so non-idempotent side effects repeat |
| MongoDB standalone with `UseOutbox = true` | 8 | Fail closed (`Left`) and an `Unhealthy` health check | An explicit, logged opt-out for development servers |
| EF domain events now dispatched before commit | 7 | Document it; #1236 moves them to the outbox | Defer dispatch to after the owner commits |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.Messaging.TransactionPipelineBehavior<,>` | `src/Encina.Messaging/TransactionPipelineBehavior.cs` | Lease holder (Phase 2) |
| `Encina.EntityFrameworkCore.TransactionPipelineBehavior<,>` | `src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs` | Lease holder (Phase 7) |
| `OutboxPostProcessor<,>` | `src/Encina.Messaging/Outbox/OutboxPostProcessor.cs` | Logic extracted to `OutboxWriter`, then deleted (Phase 9) |
| `IHasNotifications` | `OutboxPostProcessor.cs:117` | Trigger of the new behavior; moved to its own file |
| `IMessageSerializer.SerializeAsRuntimeType` | `src/Encina.Messaging/Serialization/` | Payload serialization (#1168) |
| `MessagingLog.Transaction*` | `src/Encina.Messaging/MessagingLog.cs:203-231` | Already declared, unused (#1342) |
| `OutboxActivitySource`, `OutboxProcessorMetrics` | `src/Encina.Messaging/Diagnostics/` | Spans and counters |
| `UnitOfWorkADO` / `UnitOfWorkDapper` / `UnitOfWorkEF` / `UnitOfWorkMongoDB` | `src/Encina.*/UnitOfWork/` | Move onto the accessors |
| Async begin/commit pattern | `src/Encina.ADO.PostgreSQL/UnitOfWork/UnitOfWorkADO.cs:259-295` (#1325) | Template for `DbTransactionAccessor` |
| Replica-set error mapping | `src/Encina.MongoDB/UnitOfWork/UnitOfWorkMongoDB.cs:181-189` | Reused by `MongoSessionAccessor` |
| `AddOutboxInboxSagaSchedulingServices` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:203` | Shared registration of all 10 providers |
| `MongoDbReplicaSetFixture`, `MongoDB-ReplicaSet` collection | `tests/Encina.TestInfrastructure/Fixtures/`, `tests/Encina.IntegrationTests/Infrastructure/MongoDB/ReadWriteSeparation/MongoDbReplicaSetCollection.cs` | MongoDB transactional integration tests |
| Shared fixtures `ADO-*`, `Dapper-*`, `EFCore-*` | `tests/Encina.IntegrationTests/**/Collections.cs` | Relational integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | 2800-2999 (`EventIdRanges.Messaging`) | **2963-2969** new; 2834-2836 reused; 2842-2844 freed by Phase 9 |
| `Encina.EntityFrameworkCore` | 3000-3099 | **3061-3062** new (highest used today 3060) |
| `Encina.MongoDB` | 3100-3199 | **3163-3164** new (highest used today 3162) |
| `Encina.ADO.*`, `Encina.Dapper.*` | 3200-3499 | No new log messages |

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| `Encina.Messaging` (Phases 1-4, 9, 11) | ~14 | 7 new in `Transactions/`, behavior, writer, `IHasNotifications`, DI, log; 1 deleted |
| ADO.NET ×3 (Phase 5) | ~18 | UoW, UoW repository, 4 stores, DI per provider |
| Dapper ×3 (Phase 6) | ~18 | Same |
| EF Core (Phase 7) | ~5 | Accessor, behavior, UoW, DI, README |
| MongoDB (Phase 8) | ~9 | Accessor (2), UoW, 4 stores, DI, health check |
| Tests (Phase 12) | ~55 | 10 integration, ~14 unit, ~12 guard, 3 contract, 2 property, DI tests, benchmark, `.md` |
| Documentation (Phase 13) | ~16 | ADR, feature guide, 9 READMEs, INVENTORY, changelog fragments, PublicAPI |
| **Total** | **~135** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #718 (outbox transaction atomicity) on all 10 database providers in Encina, which also
closes #719 (Dapper) and the scope of #1935 (EF Core, MongoDB, removal of OutboxPostProcessor).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 CQRS library, pre-1.0 (breaking changes welcome, no compatibility shims).
- Railway Oriented Programming: Either<EncinaError, T>; errors are never swallowed; gates fail closed.
- Database providers: ADO.NET x3, Dapper x3, EF Core x3 (SqlServer, PostgreSQL, MySQL) and MongoDB. SQLite is out
  of the matrix (ADR-024).
- Root cause: OutboxPostProcessor runs after the transaction behaviors committed (post-processors are the outermost
  pipeline layer, src/Encina/Pipeline/PipelineBuilder.cs:83) and discards AddAsync's Either; no store can see the
  transaction; units of work and behaviors own separate transactions; MongoDB stores use no session.

IMPLEMENTATION OVERVIEW:
Phase 1: ITransactionLeaseSource/ITransactionLease, IDbTransactionAccessor/IDbTransactionLease, DbTransactionAccessor,
         TransactionLeaseState, TransactionErrors (Encina.Messaging/Transactions)
Phase 2: Encina.Messaging.TransactionPipelineBehavior on the accessor; MessagingLog 2834-2836
Phase 3: OutboxWriter + TransactionalOutboxPipelineBehavior (join-or-begin on ITransactionLeaseSource); move IHasNotifications
Phase 4: shared DI with an interim write-path switch; ADO.NET/Dapper on the behavior; ValidateOnBuild tests
Phase 5: ADO.NET x3: UnitOfWorkADO on the accessor; messaging stores enlist; real OpenAsync
Phase 6: Dapper x3: same; every Dapper call passes transaction explicitly
Phase 7: EF Core x3: DbContextTransactionAccessor (DatabaseFacade, IDbContextTransaction); EF TransactionPipelineBehavior
         and UnitOfWorkEF on it; retrying execution strategy -> Left(transaction.not_supported)
Phase 8: MongoDB: IMongoSessionAccessor (IClientSessionHandle, replica set required, standalone fails closed); stores
         pass the session; UnitOfWorkMongoDB on it; health check flags standalone
Phase 9: delete OutboxPostProcessor and the write-path switch (#1935)
Phase 10: cross-cutting: composition matrix, tenancy, module isolation, inbox, documented limits
Phase 11: observability: EventIds 2963-2969, 3061-3062, 3163-3164; Enqueue span; transaction activity; two counters
Phase 12: tests: unit, guard, contract, property, integration on 10 providers, skip-path benchmark, load .md
Phase 13: docs: XML, changelog.d fragments, ADR 037, docs/features/transactional-outbox.md, 9 READMEs, PublicAPI

KEY PATTERNS:
- One owner per scope; joiners never commit; inner rollback => rollback-only => owner commit returns Left.
- Async DB calls with CancellationToken only; cleanup commit/rollback/abort with CancellationToken.None.
- Pipeline behavior: static per-generic-type flags, zero-cost skip for requests without IHasNotifications.
- Store naming unchanged; TryAdd registrations; DI completeness tests on all 10 providers.
- Integration tests: shared collections ADO-<Db>, Dapper-<Db>, EFCore-<Db>, MongoDB-ReplicaSet; ClearAllDataAsync.
- Observability: [LoggerMessage] with EventIds in registered ranges; no payloads or EncinaError.Message.

REFERENCE FILES:
- src/Encina.Messaging/TransactionPipelineBehavior.cs, Outbox/OutboxPostProcessor.cs, MessagingServiceCollectionExtensions.cs
- src/Encina/Pipeline/PipelineBuilder.cs
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, UnitOfWork/UnitOfWorkADO.cs
- src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, UnitOfWork/UnitOfWorkDapper.cs
- src/Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs, UnitOfWork/UnitOfWorkEF.cs, Outbox/OutboxStoreEF.cs
- src/Encina.MongoDB/UnitOfWork/UnitOfWorkMongoDB.cs, Outbox/OutboxStoreMongoDB.cs, ServiceCollectionExtensions.cs
- docs/plans/outbox-atomicity-ado-implementation-plan-718.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path; the feature is a write-side transaction boundary |
| 2 | OpenTelemetry | ✅ | Enqueue span, transaction activity with `db.system`, `encina.outbox.enqueued` and `encina.transaction.rollback_only` counters on all 10 providers (Phase 11) |
| 3 | Structured Logging | ✅ | `MessagingLog` 2834-2836 wired (#1342); new IDs in the Messaging, EntityFrameworkCore and MongoDB ranges (Phase 11) |
| 4 | Health Checks | ✅ | `MongoDbHealthCheck` reports a standalone server as unable to run the transactional outbox (Phase 8); relational providers need no new check |
| 5 | Validation | ❌ | No user input; configuration flags are booleans already validated by the provider options |
| 6 | Resilience | ❌ | No external call; retrying a business transaction is the application's decision, and the outbox processor already retries delivery |
| 7 | Distributed Locks | ❌ | A local transaction on one scoped connection, context or session; processor locking is #714/#717/#1251 |
| 8 | Transactions | ✅ | The core of the feature: one owner per scope, join semantics, rollback-only on every provider (Phases 1-8) |
| 9 | Idempotency | ⏭️ | Outbox id as idempotency key on dispatch is #1220; `InboxPipelineBehavior` joining the transaction is covered in Phase 10 |
| 10 | Multi-Tenancy | ⏭️ | The tenant connection is reused (Phase 10 test); `TenantId` on outbox rows and tenant-aware cycles are #1257 and #1164 |
| 11 | Module Isolation | ✅ | Relational accessor works through `SchemaValidatingConnection` (Phase 5 task 6, Phase 10 test) |
| 12 | Audit Trail | ⏭️ | Whether audit stores share or escape the business transaction is decided in #1934 |

---

## Prerequisites & Dependencies

### Advisable before or with this work

| Issue | State | Relation |
|-------|-------|----------|
| #1170 / #1868 | open | No-op `OpenConnectionAsync` in ADO stores (`OutboxStoreADO.cs:340-345` and 23 siblings). Phase 5 fixes the four messaging stores; the two issues describe the same defect and look like duplicates |
| #1418 | open | Synchronous UoW/outbox calls in ADO.SqlServer, ADO.MySQL and Dapper ×3; Phases 5-6 remove the UoW and outbox lines, the rest stays |
| #1342 | open | `Encina.Messaging.TransactionPipelineBehavior` logging and DI tests; Phases 2 and 4 cover it |
| #1340 | open | Duplicated `TransactionPipelineBehavior` guard tests; Phase 2 touches the same files |
| #1328 | closed | EF Core `TransactionPipelineBehavior` no longer logs `EncinaError.Message`; Phase 7 must keep that |

### Covered by this plan

| Issue | Relation |
|-------|----------|
| #719 | Dapper twin; Phase 6 |
| #1935 | Closed as duplicate of #718; EF Core (Phase 7), MongoDB (Phase 8), removal of `OutboxPostProcessor` (Phase 9) |

### Follow-ups and overlaps

| Issue | Relation |
|-------|----------|
| #1934 | Stores outside messaging, and read/write-separation and shard connections, on ADO.NET/Dapper; Phase 8 adds the MongoDB audit, anonymization and ABAC stores to its scope |
| #1236 | Domain events to the outbox in the same commit on all 10 providers; builds on the three accessors and resolves the EF dispatch-before-commit note of Phase 7 |
| #720 | Saga compensation transactional safety; can build on the accessors |
| #1220, #1257, #1164 | Idempotency key, tenant id on outbox rows, persisted request context |

---

## Next Steps

1. Done: the maintainer's decisions of 2026-10-06 are recorded in Maintainer Decisions.
2. When work on #718 starts, re-check every choice against the code of that day, then ask the maintainer: whether to confirm the neutral `ITransactionLeaseSource` under Design Choice 3 (a consequence of Decision 1), and the three open questions in Research (the EF retrying strategy, MongoDB without a replica set, and the timing of EF domain events).
3. Link this plan from #718 and #719; #1935 is already closed as its duplicate.
4. Resolve the #1170/#1868 duplication and decide whether they land first.
5. Implement Phases 1-13, one self-contained commit per phase. Recommended PR cut: Phases 1-6 (ADO.NET and Dapper, `Fixes #719`), then Phases 7-13 (`Fixes #718`).

## Maintainer Decisions

1. (2026-10-06) Option C: all 10 providers (ADO.NET x3, Dapper x3, EF Core x3, MongoDB). The plan also covers #719 and the scope of #1935, which is closed as a duplicate.
2. (2026-10-06) Option A: a new `TransactionalOutboxPipelineBehavior` that joins the current transaction or begins one.
3. (2026-10-06) Option A: a scoped `IDbTransactionAccessor` with ownership leases.
4. (2026-10-06) Option A: the accessor and the behavior live in `Encina.Messaging`.
5. (2026-10-06) Option B: nested requests join the transaction. An inner rollback marks it rollback-only, and the owner's commit then returns `Left`.
6. (2026-10-06) Option B: the four messaging stores and the unit-of-work repositories join the shared transaction. The other stores are #1934.
