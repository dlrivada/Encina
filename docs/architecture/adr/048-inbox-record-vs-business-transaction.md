# ADR-048: The Inbox Record and the Business Transaction: Enlisted and Independent Writes

## Status

**Accepted** - 2026-10-09, issue [#2084](https://github.com/dlrivada/Encina/issues/2084).

## Context

The Inbox pattern guarantees idempotent exactly-once processing of a message (AGENTS.md section 3). A request that goes through the inbox may also run inside a business transaction: the Transaction pattern is `Encina.Messaging.TransactionPipelineBehavior<,>` for ADO.NET and Dapper and `Encina.EntityFrameworkCore.TransactionPipelineBehavior<,>` for EF Core. That transaction rolls back when the handler returns a `Left` or throws. The transaction behavior is registered first, so the inbox behavior (`InboxPipelineBehavior<,>`, which calls `InboxOrchestrator.ProcessAsync`) runs inside it.

The inbox therefore has two obligations that pull in opposite directions:

1. **Mark a message processed atomically with the business effect.** If the business transaction fails to commit after a successful handler, the message must not stay marked as processed, or its effect would be lost for good.
2. **Keep failure records across the rollback.** `InboxOptions.MaxRetries` only holds if `RetryCount` survives the rollback that a failed attempt causes. The same applies to a handler `Left` cached as the processed response (so a redelivery does not run the handler again) and to the inbox entry itself.

A third requirement is that the behavior is the same on all 10 database providers (AGENTS.md section 5), so switching provider means changing the DI registration only.

## Decision

`IInboxStore` splits its writes in two kinds.

| Kind | Methods | Relation to the business transaction |
| --- | --- | --- |
| Enlisted | `MarkAsProcessedAsync` (success), `GetMessageAsync`, `GetExpiredMessagesAsync`, `RemoveExpiredMessagesAsync` | join the active business transaction, if there is one |
| Independent | `AddAsync`, `MarkAsFailedAsync`, `CacheHandlerErrorAsync` | commit on their own, outside it |

`MarkAsFailedAsync` is the only place where `RetryCount` grows, by exactly one per failed attempt. `CacheHandlerErrorAsync` records a handler `Left` as the processed response.

`InboxOrchestrator` maps outcomes to store calls:

- a handler `Right` goes to `MarkAsProcessedAsync`;
- a handler `Left` goes to `CacheHandlerErrorAsync`;
- a thrown exception goes to `MarkAsFailedAsync`;
- every `Left` returned by a store call fails the operation and is returned to the caller (errors are never swallowed, ADR-001, ADR-006).

**Consequence of the split.** A business commit that fails after a successful handler rolls back the processed mark. The message stays unprocessed and the redelivery runs the handler again. The entry and the failure records are never lost.

### Mechanisms per family

| Family | How the two kinds are implemented |
| --- | --- |
| ADO.NET and Dapper (SqlServer, PostgreSQL, MySQL) | The store reads the open transaction from `IDbTransactionAccessor` (set by `Encina.Messaging.TransactionPipelineBehavior`) through `DbLease`. `DbLease.Enlisted` assigns that transaction to the command. `DbLease.IndependentAsync` uses the shared connection when no transaction is open and otherwise an opened clone of the connection. The connection must implement `ICloneable` (`SqlConnection`, `NpgsqlConnection` and `MySqlConnection` do); a connection wrapper that does not makes the independent write fail with a `Left`. |
| EF Core (SqlServer, PostgreSQL, MySQL) | `InboxStoreEF` runs the independent writes on an isolated `DbContext` created with `Activator` from the injected context's `IDbContextOptions`, so it has its own connection. `MarkAsProcessedAsync` runs on the injected context, so it joins `CurrentTransaction`. Relational providers use `ExecuteUpdate`, so `RetryCount + 1` is one atomic statement. |
| MongoDB | The pipeline has no business transaction, so every write is immediate. A MongoDB `UnitOfWork` session is explicit and not part of the pipeline. |

EF Core requirements: the context type has a public constructor taking `DbContextOptions<TContext>` (validated at registration by `InboxStoreEF.ValidateContextType`), and it is configured with a connection string and not a shared `DbConnection` instance (refused at the first write with a `Left`, because a shared connection would put the "independent" write inside the business transaction).

## Alternatives considered

- **One unit of work for everything.** Rejected: a failed attempt rolls back its own `RetryCount` increment, so `MaxRetries` is unbounded and a handler that always throws is retried forever. A cached handler `Left` and the entry would also be lost on rollback.
- **Everything independent.** Rejected: if the business commit fails after the handler succeeded, the message is already marked processed and the redelivery returns the cached response, so the business effect is lost. That breaks exactly-once processing.
- **An outbox-style compensation** (mark processed independently, then compensate if the commit fails). Rejected: it needs a second durable record and a recovery process for a window that the enlisted write closes with no extra machinery, and the compensation itself can fail.

## Consequences

- **Positive**: the processed mark and the business effect commit or roll back together; `MaxRetries` holds because failure records survive rollback; the behavior is the same on the 10 database providers and MongoDB.
- **Negative, limits**:
  - While a business transaction is open, each independent write uses a second pooled connection, so size the connection pool for it.
  - Under a repeatable-read or serializable `[Transaction]`, the lock taken by the lookup on the business connection can block an independent write until the command timeout.
  - The ADO.NET and Dapper `UnitOfWork` (an explicit `IUnitOfWork` transaction) is not visible to the inbox, which sees only the transaction set by `TransactionPipelineBehavior`.
  - Concurrent redeliveries of one message are not serialized; `MaxRetries` holds for sequential deliveries.

## Related

- [ADR-001](001-railway-oriented-programming.md) and [ADR-006](006-pure-rop-exception-handling.md) (`Either` contracts, store errors fail the operation)
- [SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) (1.0 scope: the 10 database providers)
- [Inbox pattern](../../messaging/inbox.md) (how a developer uses it)
- Issue [#2084](https://github.com/dlrivada/Encina/issues/2084)

## Date

2026-10-09
