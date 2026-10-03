<!-- issue
title: [BUG] RequestContext.Create(string) and CreateForTest read DateTimeOffset.UtcNow instead of TimeProvider
labels: bug, area-core
milestone: v0.14.0 — Hardening
-->

## Description

`CLAUDE.md`'s "Code Quality Standards" mandates that production code never reads `DateTime.UtcNow`/`DateTimeOffset.UtcNow` directly; it takes `TimeProvider` by injection so time-dependent behaviour is deterministic in tests (project history: #543, #667). `src/Encina/Core/RequestContext.cs` violates this in two of its four factory methods, while the other two follow the rule correctly in the same file.

Found during the SPEC-003 pilot 2 deep quality audit of the core request pipeline audit unit (checklist item AUD-14).

## Steps to Reproduce

1. Call `RequestContext.Create("my-correlation-id")` or `RequestContext.CreateForTest(...)` from code that has replaced `TimeProvider.System` (e.g. `FakeTimeProvider` in a test, or a custom `TimeProvider` in production for clock skew correction).
2. Observe `Timestamp` on the returned context.

## Expected Behavior

`Timestamp` reflects the injected/ambient `TimeProvider`, consistently with `RequestContext.Create()` (no arguments), which already calls `CreateAt(TimeProvider.System.GetUtcNow())`.

## Actual Behavior

- `RequestContext.Create(string correlationId)` (`src/Encina/Core/RequestContext.cs:118-128`) sets `Timestamp = DateTimeOffset.UtcNow` directly (line 125).
- `RequestContext.CreateForTest(...)` (`RequestContext.cs:141-153`) does the same (line 151).
- Both are `public static` members of a shipped type, so a caller has no way to make either path honor a substituted `TimeProvider`.

## Environment

- **Encina Version**: pre-1.0 (main, 2026-09-24)
- **.NET Version**: .NET 10.0
- **OS**: Windows 11
- **Package(s) Affected**: `Encina` (core)

## Code Sample

```csharp
public static IRequestContext Create(string correlationId)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
    return new RequestContext
    {
        CorrelationId = correlationId,
        Timestamp = DateTimeOffset.UtcNow, // should route through TimeProvider
        Metadata = ImmutableDictionary<string, object?>.Empty
    };
}
```

## Stack Trace

N/A.

## Additional Context

- `RequestContext.Create()` (no args) at `RequestContext.cs:66` is the correct pattern to follow: `CreateAt(TimeProvider.System.GetUtcNow())`.
- Proposed fix: add an optional `TimeProvider? timeProvider = null` parameter to `Create(string)` and `CreateForTest(...)` (defaulting to `TimeProvider.System`), or route both through the existing internal `CreateAt(DateTimeOffset)` overload. `CreateForTest` is a test helper shipped in the production assembly, so it should still take the parameter rather than being special-cased.
- SPEC-003 audit unit: `encina-core-pipeline`.
