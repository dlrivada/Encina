# Load Tests - Dead Letter Store

## Status: Not Implemented

## Justification

Load testing of the persistent dead letter stores (`IDeadLetterStore` on the ten database providers) is intentionally not implemented for the following reasons:

### 1. Written Only on Terminal Failure

A message reaches the dead letter queue only after every retry has failed. The write rate equals the rate of permanently failed messages, which is a small fraction of the traffic and is bounded by the retry policy of each source pattern (recoverability, outbox, inbox, scheduling, saga).

### 2. Read by Operators, Not by Request Traffic

Queries (`GetMessagesAsync`, `GetCountAsync`) are issued by operator tooling, the statistics endpoint, the health check and the cleanup processor. Page size is capped by `DeadLetterStoreLimits.MaxPageSize`; there is no per-request read path.

### 3. Concurrency Safety Is Proven Functionally

The only concurrent hazards are the duplicate capture of one source message and two replays of one message. Both are covered with real databases on all ten providers by the contract facts `Add_ConcurrentCapturesOfOneSource_KeepExactlyOneRow` and `TryClaimForReplay_TwoConcurrentCallers_ExactlyOneWins`. Each is a single conditional statement, so throughput under load does not change the outcome.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: orchestrator and manager behavior, metrics and the instrumented decorator.
- **Contract and Integration Tests**: one contract run against the fake and the ten stores.
- **Property Tests**: add, mark, delete and expiry invariants over random sequences.

### 5. Recommended Alternative

If a deployment expects a sustained burst of failures, measure the insert path with BenchmarkDotNet against the target database, not with a load test of the store: the cost is one indexed `INSERT` per dead letter.

## Related Files

- `src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`
- `tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs`
- `tests/Encina.PropertyTests/Messaging/DeadLetter/DeadLetterStorePropertyTests.cs`

## Date: 2026-10-09
## Issue: #583
