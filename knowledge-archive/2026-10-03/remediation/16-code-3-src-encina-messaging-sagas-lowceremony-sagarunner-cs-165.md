<!--
title: [BUG] Raw exception message leaked in SagaRunner catch block, logged twice, and persisted unredacted
labels: bug
milestone: v0.14.0 — Hardening
-->

## Description

When a saga step throws an exception, the raw `ex.Message` is used directly in the `EncinaError.Message` property, logged twice (once in `SagaRunner` and once in `SagaOrchestrator`), and persisted as plaintext in the saga store's `ErrorMessage` column. This violates the AGENTS.md §3 rule regarding error message leakage. The issue exists because the `catch (Exception ex)` block in `SagaRunner.cs` does not apply the same error-handling discipline seen earlier in the file, where only the error code is threaded through `StartCompensationAsync`.

## Steps to Reproduce

1. Configure a saga with a step that throws an exception containing sensitive information in its message.
2. Call method `SagaRunner.RunAsync` (implied by the context of the catch block in `SagaRunner.cs`).
3. Pass parameter `sagaId` and allow the step to throw `Exception ex`.
4. See error: The exception message is logged at `src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs:165` and `:171`, and `EncinaErrors.Create` is called with `ex.Message` at `:174`. The message is also persisted via `SagaOrchestrator.FailAsync` at `src/Encina.Messaging/Sagas/SagaOrchestrator.cs:351`, `:355`, and logged again at `:359`.

## Expected Behavior

The saga failure handling should use a generic error message for user-facing `EncinaError.Message` and logs, while persisting only a redacted or generic identifier in the saga store. The raw `ex.Message` should not be exposed to clients, logs, or the database.

## Actual Behavior

The raw `ex.Message` is returned in `EncinaError.Message`, logged in `SagaRunner` and `SagaOrchestrator`, and persisted as plaintext in the `ErrorMessage` column of the saga store.

## Environment

- **Encina Version**: 0.14.0-dev
- **.NET Version**: .NET 10
- **OS**: Not applicable (found by static review of the code, not at runtime)
- **Package(s) Affected**: Encina.Messaging

## Code Sample

```csharp
// Minimal reproducible example demonstrating the leak in SagaRunner.cs:165, :171, :174
// and SagaOrchestrator.cs:351, :355, :359
//
// src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs:165
Log.SagaException(_logger, sagaId, ex.Message, ex);
// src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs:171
_orchestrator.FailAsync(sagaId, ex.Message, CancellationToken.None);
// src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs:174
EncinaErrors.Create(SagaErrorCodes.HandlerFailed, ex.Message)
//
// src/Encina.Messaging/Sagas/SagaOrchestrator.cs:351
state.ErrorMessage = errorMessage;
// src/Encina.Messaging/Sagas/SagaOrchestrator.cs:355
await _store.UpdateAsync(...)
// src/Encina.Messaging/Sagas/SagaOrchestrator.cs:359
Log.SagaFailed(_logger, sagaId, errorMessage)
```

## Stack Trace

```
// Not applicable: Issue found via static code review, no runtime execution occurred.
```

## Additional Context

- The rule was known and correctly applied in `src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs:111-113`, where a comment explicitly cites "#1259 review" and only the error code is threaded through `StartCompensationAsync`. The `catch (Exception ex)` block below does not follow this pattern.
- The `ErrorMessage` column exists and is bound with no redaction in `ISagaState.ErrorMessage` (`src/Encina.Messaging/Sagas/ISagaState.cs:67`) and in provider implementations like `src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs:90,129`.
- Related Issue: [#1343] [DEBT] Error-message leak static scan misses multi-line calls and ex.Message; leaks in SagaRunner and CDC cache invalidation
- #1343 - partially related (it covers only part of this finding)
