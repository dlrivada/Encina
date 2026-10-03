<!--
title: [BUG] EncinaError.Message leaks into OpenTelemetry Activity status in InstrumentedSagaStore
labels: bug
milestone: v0.14.0 — Hardening
-->

## Description

The `InstrumentedSagaStore` class in `Encina.OpenTelemetry` violates the AGENTS.md §3 security policy by writing `EncinaError.Message` directly into the OpenTelemetry `Activity` status description. This occurs on every failed saga-store operation. The policy explicitly states that `EncinaError.Message` NEVER reaches logs, activity tags, health-check results, or plaintext storage; only the error code or the exception type should be recorded.

## Steps to Reproduce

1. Configure an `InstrumentedSagaStore` with OpenTelemetry instrumentation enabled.
2. Call a saga store operation method (e.g., at lines 49, 59, 69, or 87) that results in a failure.
3. Pass parameters that trigger a failure path returning a left `EncinaError` result.
4. Observe that the `Failed` helper method (lines 192-195) is invoked with `err.Message`.
5. See error: The OpenTelemetry `Activity` status description contains the full `EncinaError.Message`.

## Expected Behavior

When a saga store operation fails, the OpenTelemetry `Activity` status should only contain the error code or the exception type, in compliance with AGENTS.md §3. The raw `EncinaError.Message` content must not appear in the activity tags or status description.

## Actual Behavior

The `EncinaError.Message` is written directly into the OpenTelemetry `Activity` status description on every failed saga-store operation. This is implemented in `InstrumentedSagaStore.cs` where `result.IfLeft(err => Failed(activity, err.Message))` is called, and `Failed` executes `activity?.SetStatus(ActivityStatusCode.Error, errorMessage)`.

## Environment

- **Encina Version**: 0.14.0-dev
- **.NET Version**: .NET 10
- **OS**: Not applicable (found by static review of the code, not at runtime)
- **Package(s) Affected**: Encina.OpenTelemetry

## Code Sample

```csharp
// Minimal reproducible example from src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs

// Line 49, 59, 69, 87, 104
result.IfLeft(err => Failed(activity, err.Message));

// Line 192-195
private static void Failed(Activity? activity, string? errorMessage) => activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
```

## Stack Trace

```
// N/A - Static review finding, no runtime stack trace generated
```

## Additional Context

**Location Evidence**: `src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs:49,59,69,87,104` and `:192-195`.

**Policy Violation**: Fails AGENTS.md §3 verbatim: "`EncinaError.Message` NEVER reaches logs, activity tags, health-check results or plaintext storage; record only the error code or the exception type" (references: #1168, #1173, #1259, #1274).

**Related Issues**:
- #16 (Source audit issue)