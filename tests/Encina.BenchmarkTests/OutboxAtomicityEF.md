# Benchmarks - Transactional Outbox (EF Core)

## Status: Not Implemented

## Justification

1. The only hot path this slice adds is the skip path of `TransactionalOutboxPipelineBehavior` (a request without
   `IHasNotifications`). That behavior is provider-neutral and lives in `Encina.Messaging`; its skip-path benchmark
   already exists for the ADO.NET provider (see `OutboxAtomicity.md`), and the code measured is identical for EF Core.
2. The transaction work that EF Core adds is a database round trip (`BeginTransactionAsync`, commit). Its cost is the
   cost of the database, not of Encina code, so a benchmark of it would measure the engine.
3. `DbContextTransactionAccessor` and `UnitOfWorkEF` only moved onto the shared lease; they add no loop, allocation
   hot spot or serialization of their own.

### Adequate coverage from other test types

- **Unit, guard, contract and property tests** exercise the accessor, the lease contract and the commit-iff-owner-commits
  invariant against SQLite in memory.
- **Integration tests** prove the behavior on real SqlServer and PostgreSQL.

### Recommended alternative

Extend the skip-path benchmark only if EF Core ever gets its own variant of the behavior.

## Related Files

- `tests/Encina.BenchmarkTests/OutboxAtomicity.md`
- `src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs`
- `src/Encina.EntityFrameworkCore/Transactions/DbContextTransactionAccessor.cs`

## Date: 2026-10-23

## Issue: #718
