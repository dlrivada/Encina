# Implementation Plan: Error Recording on Spans According to the OpenTelemetry Conventions

> **Issue**: [#185](https://github.com/dlrivada/Encina/issues/185)
> **Type**: Feature (observability core)
> **Related**: ADR-054 (settled point 2: code or exception type only; D5 says #185 stays with the #1794 triage because as written it conflicts with that rule), #1794 (backlog triage), #1788 (messages reaching spans in about 26 satellite files), #1789 (command and query behaviors leave spans Unset on `Left`), #1468, #2474, #184 (owns `EncinaTracingOptions`), ADR-062
> **Complexity**: Low-Medium (a small helper; the care is in what it must never record)
> **Estimated Scope**: ~150 lines of production code + ~420 lines of tests, in two pull requests of at most 15 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-20, main `098caa920`)

- `ActivityStatusDescriptions` (`src/Encina/Diagnostics/ActivityStatusDescriptions.cs`) is the public rule for the status text: `ForError(EncinaError)` returns the code, `ForException(Exception)` the type name. `EncinaDiagnostics.SendCompleted` (`EncinaDiagnostics.cs:55-70`) sets status Error with the code and the tag `Encina.failure_reason`; `CommandActivityPipelineBehavior.RecordException` sets status with the exception type name and the tag `exception.type`.
- No code records the OpenTelemetry error attributes: a search of `src` finds no `error.type` tag and no `exception` span event (`AddException`, `AddEvent` for an exception). `ActivityTagNames.ExceptionMessage` (`"exception.message"`, `src/Encina/Diagnostics/ActivityTagNames.cs:34`) is declared and is the key this feature must never set.
- `Activity.AddException(Exception)` (.NET 9+) writes `exception.message`; it cannot be used (ADR-054 settled point 2). `RedactedException` (`src/Encina/Diagnostics/RedactedException.cs`) keeps type and stack trace but not the message and is for loggers.
- #1788 (satellite spans carry messages) and #1789 (the unreachable `RecordErrorOutcome`) are open bugs; this plan supplies the helper they adopt and fixes neither.

### What the issue proposes and what changed since

The issue proposes `ErrorRecordingBehavior<,>` writing `error.message` (the `EncinaError.Message`), `RecordException(ex)` and a `SanitizeErrorMessages` option. ADR-054 (2026-10-22) forbids every message in telemetry; there is no option to allow it.

### What this plan delivers

- `ActivityErrorRecorder` (core, public static): `RecordError(Activity?, EncinaError)` sets status Error with the code and the OpenTelemetry attribute `error.type` = the code; `RecordException(Activity?, Exception, bool includeStackTrace)` sets status Error with the type name, `error.type` = the exception type full name, and adds the `exception` event with `exception.type` and, only when requested, `exception.stacktrace` (the original stack trace, no message, no inner messages); never `exception.message`.
- The dispatcher span and the command and query behaviors call it; `EncinaTracingOptions.IncludeExceptionStackTrace` (default false) controls the stack trace.
- A static scan test that no source line in core sets `exception.message` or `error.message`.
- Shared pieces and owner: this plan owns `ActivityErrorRecorder`; #184 owns `EncinaTracingOptions` (this plan adds one member); #1788 migrates the satellites to the helper; #1789 fixes the unreachable branch.
- Affected packages: `Encina`. Provider category: observability.
- Not delivered: `ErrorRecordingBehavior<,>` (Design Choice 1), `error.message`, `SanitizeErrorMessages`, the satellites (#1788).

---

## Design Choices

<details>
<summary><strong>1. A separate behavior or a helper called by the span owners</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A static helper called by the code that owns each span** | Writes on the right span; no extra pipeline step; satellites reuse it | The issue's class name is not built |
| **B) `ErrorRecordingBehavior<,>` writing on `Activity.Current`** | Literal | At that point the current activity is the span of whichever behavior is outermost: it can write on the wrong span, adds a step per request, and duplicates the dispatcher's recording |

### Chosen Option: **A — A helper** (recommended, pending the maintainer)

### Rationale

- A wins in every case where a span owner exists, which is all of them. B wins only for a host that wants one switch that touches foreign spans, which no design here wants.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Messages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Never record `EncinaError.Message` or `Exception.Message`; no option** | Complies | The issue's `error.message` and sanitize option are not built |
| **B) Record messages with an opt-out sanitizer** | OpenTelemetry convention allows it | Violates the rule; sanitizers miss personal data |

### Chosen Option: **A — Never**

### Rationale

- ADR-054 settled point 2 and `AGENTS.md` section 3 forbid it.

**Class:** Settled
**Settled by:** ADR-054 Decision, settled point 2; `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs or activity tags); option B contradicts both

</details>

<details>
<summary><strong>3. Stack trace</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Off by default, `EncinaTracingOptions.IncludeExceptionStackTrace` turns it on; the original `StackTrace` string only** | Useful for debugging; no message; opt-in | Stack traces name internal methods and can be large (truncated to 4,096 characters) |
| **B) Always on** | Convention complete | Exports internals by default |
| **C) Never** | Smallest | The issue's acceptance criterion "stack trace inclusion option" is lost |

### Chosen Option: **A — Opt-in** (recommended, pending the maintainer)

### Rationale

- A wins for production defaults; B wins in development hosts (set the option there). `AGENTS.md` allows the type and stack trace in logs through `ForLogging()`, so the same data is acceptable here when requested.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Attribute names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `error.type` and the `exception` event keys of the OpenTelemetry convention** | Backends group by them | New strings next to `Encina.failure_reason` |
| **B) Only the existing Encina tags** | No new strings | Backends do not recognize errors |

### Chosen Option: **A — The convention keys**

### Rationale

- The conventions are the point of the issue; `Encina.failure_reason` stays unchanged (renames belong to #177 and #2678).

**Class:** Trivial
**Settled by:** OpenTelemetry semantic conventions (`error.type`, exceptions on spans)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code, so no ADR-062 `ErrorCategory`; a `Left` is recorded with the code it already has | Settled | ADR-062 |
| C2 | `error.type` for a `Left` is the error code; for an exception the full type name (bounded sets, no cardinality risk) | Settled | ADR-054 settled point 2 |
| C3 | A null activity is a no-op; with no listener nothing runs | Settled | ADR-054 settled point 7 |
| C4 | No tenant attribute and no tenant-qualified id; a tenant attribute is #2676's | Settled | ADR-054 D4 |
| C5 | Cancellation is not an error span: `OperationCanceledException` of the request token is recorded as status Error with the cancellation code only where the existing behaviors do so today | Trivial | `CommandActivityPipelineBehavior.RecordCancellation` |
| C6 | Public names: `ActivityErrorRecorder`, `EncinaTracingOptions.IncludeExceptionStackTrace` | Dilemma (recommended, pending the maintainer) | public surface |

---

## Implementation Phases

### Phase 1: The Helper (pull request E1)

> **Goal**: One place writes the OpenTelemetry error attributes without a message.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Diagnostics/ActivityErrorRecorder.cs` (public static class, XML docs with an example) with the two methods of the summary; the stack trace is truncated to 4,096 characters.
2. **Modify** `src/Encina/Diagnostics/EncinaTracingOptions.cs` (created by #184): `IncludeExceptionStackTrace`; `src/Encina/Diagnostics/EncinaDiagnostics.cs` (`SendCompleted` calls `RecordError`; the dispatcher passes the options); `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.json`; add `changelog.d/185-error-recording-helper.added.md` through `mechanical-fixer`.
3. Tests (new): `tests/Encina.UnitTests/Diagnostics/ActivityErrorRecorderTests.cs` (status Error with the code, `error.type`, an exception with a secret message records the type and never the message, the stack trace only when requested and truncated, null activity no-op), `tests/Encina.GuardTests/Diagnostics/ActivityErrorRecorderGuardTests.cs` (null exception throws; a null activity is valid), `tests/Encina.UnitTests/Diagnostics/NoMessageInSpansScanTests.cs` (a static scan of `src/Encina` for `exception.message` and `error.message` literals in `SetTag`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #185, plan docs/plans/error-recording-implementation-plan-185.md, phase 1.
ADR-054 settled point 2: spans carry the error code or exception type only, never a message.

TASK:
1. Create ActivityErrorRecorder.RecordError and RecordException (error.type, exception event with type and optional truncated stack trace).
2. Use it in EncinaDiagnostics.SendCompleted; add EncinaTracingOptions.IncludeExceptionStackTrace.
3. Tests incl. a message-leak assertion and a static scan; PublicAPI, manifest, changelog through mechanical-fixer.

KEY RULES:
- Never Activity.AddException (it writes exception.message); never exception.message or error.message.
- No scoped service in a singleton; zero cost without a listener.

REFERENCE FILES:
- src/Encina/Diagnostics/ActivityStatusDescriptions.cs, src/Encina/Diagnostics/EncinaDiagnostics.cs
```

</details>

---

### Phase 2: Adopt in the Activity Behaviors (pull request E2)

> **Goal**: The command and query behaviors use the helper.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Pipeline/Behaviors/CommandActivityPipelineBehavior.cs` and `QueryActivityPipelineBehavior.cs` (`RecordException` and `RecordErrorOutcome` call the helper); do not change the unreachable-branch bug of #1789 (it lands first or alongside, and its test then covers `RecordErrorOutcome`).
2. **Modify** `.github/coverage-manifest/Encina.json`; changelog `185-activity-behaviors-error-recording.changed.md` through `mechanical-fixer`.
3. Tests (edit): `tests/Encina.UnitTests/Pipeline/Behaviors/CommandActivityPipelineBehaviorTests.cs` and the query equivalent: an exception records `error.type` and the event, no message.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #185 plan docs/plans/error-recording-implementation-plan-185.md, phase 2. Phase 1 merged.

TASK:
1. Make the command and query activity behaviors call ActivityErrorRecorder; extend their existing tests.

KEY RULES:
- No message in any tag; do not fix #1789 here.

REFERENCE FILES:
- src/Encina/Pipeline/Behaviors/CommandActivityPipelineBehavior.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| OpenTelemetry semantic conventions, exceptions on spans | `exception` event with `exception.type`, `exception.message`, `exception.stacktrace`; `error.type` | Keys used, except the message |
| ADR-054 | Settled point 2, D5 | Scope |
| ADR-062 | Error category contract | None created (row C1) |
| `AGENTS.md` section 3 | Messages never reach telemetry | Core rule |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ActivityStatusDescriptions` | `src/Encina/Diagnostics/ActivityStatusDescriptions.cs` | Status text |
| `EncinaDiagnostics.SendCompleted` | `src/Encina/Diagnostics/EncinaDiagnostics.cs` | First caller |
| Activity behaviors | `src/Encina/Pipeline/Behaviors/` | Second callers |
| `EncinaTracingOptions` | plan of #184 | Stack trace flag |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No log event; nothing taken from 5970-5999 |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| E1 | Helper | 1 | 3 (options, `EncinaDiagnostics`, PublicAPI) | 3 | 0 | 1 | 1 (changelog) | 9 | ~520 |
| E2 | Adoption | 0 | 2 (the behaviors) | 0 | 2 | 1 | 1 | 6 | ~200 |
| **Total** | | **1** | **5** | **3** | **2** | **2** | **2** | **15** | **~720** |

The columns sum (1 + 5 + 3 + 2 + 2 + 2 = 15; 9 + 6 = 15). The pull request of this plan is one file.

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `Diagnostics/ActivityErrorRecorder.cs` | 100 | 100 | 0 | 0 | Few branches, all reachable with plain activities; guard covers the one throwing parameter; a static helper has no contract |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #185 - OpenTelemetry error recording without messages - in two pull requests.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0; edit source only with Edit/Write; PublicAPI, manifests, changelog through mechanical-fixer.
- ADR-054 settled point 2: error code or exception type only, never a message.

IMPLEMENTATION OVERVIEW:
1. E1: ActivityErrorRecorder, the stack trace option, SendCompleted uses it.
2. E2: command and query activity behaviors use it.

KEY PATTERNS:
- Real ActivityListener tests with a secret message that must never appear; guard tests for the new public methods.

REFERENCE FILES:
- src/Encina/Diagnostics/ActivityStatusDescriptions.cs, docs/plans/error-recording-implementation-plan-185.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Real-time span enrichment |
| 2 | OpenTelemetry | ✅ Phases 1-2 | This is the error recording |
| 3 | Structured Logging | ❌ N/A | Logs already carry codes through `ForLogging()`; no new event |
| 4 | Health Checks | ❌ N/A | No dependency |
| 5 | Validation | ❌ N/A | No external input |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Non-transactional |
| 9 | Idempotency | ❌ N/A | Events are append-only on a span |
| 10 | Multi-Tenancy | ❌ N/A | Error recording is tenant-independent |
| 11 | Module Isolation | ❌ N/A | Module-independent |
| 12 | Audit Trail | ❌ N/A | Telemetry is not an audit record |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `EncinaTracingOptions` | Plan #184 (T1) | E1 adds a member; E1 cannot compile before it merges |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Satellite messages in spans | #1788 | Adopts the helper |
| Unreachable `Left` branch | #1789 | Fix before E2 |

---

## Next Steps

1. Link this plan from #185 and ask the maintainer to answer Design Choices 1 and 3 and register row C6 when work starts.
2. Implement E1 after #184 T1, then E2; when #185 is picked up ask that its acceptance criteria be amended (messages are never recorded).
