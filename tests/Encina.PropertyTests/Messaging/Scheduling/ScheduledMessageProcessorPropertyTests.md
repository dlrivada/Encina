# Property Tests - Scheduling Processor

## Status: Not Implemented

## Justification

The `ScheduledMessageProcessor` is a thin orchestration layer. Its behavior is deterministic conditional on store content, time, and the retry policy output. The relevant invariants are:

### 1. Retry policy is independently tested

The `ExponentialBackoffRetryPolicy.Compute` method — the only non-trivial computation — is already covered by dedicated unit tests in `ExponentialBackoffRetryPolicyTests.cs` with 12 scenarios including edge cases (zero delay, zero maxRetries, determinism).

### 2. Dispatcher uses compiled expression trees

The `CompiledExpressionScheduledMessageDispatcher` builds delegates at runtime. Property-based testing of expression tree compilation would require generating arbitrary `IRequest<>` / `INotification` types at runtime, which is not feasible with FsCheck's standard type generators.

### 3. Processor loop is deterministic

The processor's `ExecuteAsync` / `ProcessOnceAsync` cycle is a straightforward poll-dispatch-log loop with no state that varies between invocations beyond what the store returns. There are no invariants to verify with random inputs.

### Adequate Coverage from Other Test Types

- **Unit Tests**: 12 retry policy scenarios, 13 dispatcher scenarios, orchestrator ROP callback tests
- **Guard Tests**: Constructor null-check validation for all new types
- **Contract Tests**: Formal invariant verification for `IScheduledMessageRetryPolicy` and `IScheduledMessageDispatcher`

### Recommended Alternative

If property tests are desired in the future, the best candidate would be the retry policy: verify `Compute(retryCount, maxRetries, now)` produces monotonically increasing delays for `retryCount < maxRetries` and dead-letters for `retryCount >= maxRetries - 1` across random inputs. This would be a ~5-line FsCheck property.

## Related Files

- `src/Encina.Messaging/Scheduling/ExponentialBackoffRetryPolicy.cs`
- `src/Encina.Messaging/Scheduling/CompiledExpressionScheduledMessageDispatcher.cs`
- `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs`
- `tests/Encina.UnitTests/Messaging/Scheduling/ExponentialBackoffRetryPolicyTests.cs`

## Date: 2026-04-12

## Issue: #765
