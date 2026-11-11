# Implementation Plan: Opt-in stream aggregate-type verification in Marten `LoadAsync`

> **Issue**: [#1118](https://github.com/dlrivada/Encina/issues/1118)
> **Type**: Feature
> **Complexity**: Low (5 phases, 1 provider: Marten, ~8 files)
> **Estimated Scope**: ~250 lines of production code + ~450 lines of tests

---

## Summary

`MartenAggregateRepository<TAggregate>.LoadAsync` and `SnapshotAwareAggregateRepository<TAggregate>.LoadAsync` replay any stream whose id they are given, whatever aggregate type started it. Since #1116 (issue #1095) they reject a replay whose aggregate does not carry the requested id (`aggregate.Id != id`), a free heuristic that misses a foreign stream sharing an event type the aggregate applies. This plan adds an opt-in check, `EncinaMartenOptions.VerifyStreamAggregateType` (default `false`): before replaying, read the stream state (`IEventStoreOperations.FetchStreamStateAsync`) and return `marten.aggregate_not_found` when `StreamState.AggregateType` is not `typeof(TAggregate)` or is absent.

- **Standards**: none (a hardening option); module isolation safeguard for modular monoliths sharing one Marten store.
- **Affected packages**: `Encina.Marten` only (options, both repositories, `Log.cs`, `PublicAPI.Unshipped.txt`).
- **Provider category**: event sourcing, Marten on PostgreSQL only (EventStoreDB is post-1.0); the 10-database, caching, transport and lock matrices do not apply.
- **Scope**: 1 new internal helper, 4 modified source files, tests in 5 existing files plus one new integration test class. Fits one worker PR (under 15 files, 1500 lines).
- **Prerequisites**: none. The open spike #3026 (audit daemon) is unrelated: this feature does not use the async daemon.

---

## Design Choices

<details>
<summary><strong>1. Default and shape of the switch: opt-in boolean on <code>EncinaMartenOptions</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `VerifyStreamAggregateType` bool, default `false`** | Pay-for-what-you-use; no cost on the hottest repository path; one flag for both repositories | Callers who want the guarantee must turn it on |
| **B) Always verify** | Correct by construction | One extra query on every `LoadAsync`; violates opt-in rule |
| **C) Per-aggregate attribute** | Granular | Another metadata path; hard to reason about with snapshots; no demand |

### Chosen Option: **A — `VerifyStreamAggregateType`, default `false`**

### Rationale

- The always-on `aggregate.Id != id` guard (`MartenAggregateRepository.cs:107`, `SnapshotAwareAggregateRepository.cs:337`) stays as the zero-cost layer; the option adds the exact check.
- The option sits next to `ThrowOnConcurrencyConflict` and `StreamPrefix` (`EncinaMartenOptions.cs:29-35`) and is read through the existing `IOptions<EncinaMartenOptions>` of both repositories (`_options`).

- **Class:** Settled
- **Settled by:** AGENTS.md section 1 (pay-for-what-you-use, every feature opt-in) and the settled-by-elimination comment on #1118 (2026-10-17)

</details>

<details>
<summary><strong>2. Where the check lives: one shared internal helper</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Internal static `StreamAggregateTypeGuard` in `Encina.Marten`** | One implementation, one unit test class; both repositories call it | One more file |
| **B) Inline in each of the 4 `LoadAsync` paths** | No new type | The id guard is already duplicated (`StreamDoesNotBelongToAggregate` in one repository, inline in the other); four copies would drift |
| **C) Decorator over `IAggregateRepository<T>`** | Keeps repositories untouched | Cannot reach the snapshot path without double fetching; changes DI registration order |

### Chosen Option: **A — shared internal guard**

### Rationale

- The helper takes `IDocumentSession`, the aggregate `Type`, the id and the options flag, and returns `Either<EncinaError, Unit>`; it never throws for business outcomes (ROP).
- It is called before `FetchStreamAsync` and, in the snapshot-aware repository, before the snapshot lookup, so a foreign id never reaches the snapshot store.

- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (Railway Oriented Programming) and the existing guard pattern in `MartenAggregateRepository.cs:347-360`

</details>

<details>
<summary><strong>3. Error returned on a mismatch or a missing aggregate type</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `marten.aggregate_not_found` (`MartenErrorCodes.AggregateNotFound`)** | The issue's acceptance criterion; callers already handle it; does not reveal that a foreign stream exists | Cannot tell "no such stream" from "wrong type" without the log |
| **B) New code `marten.stream_aggregate_type_mismatch`** | Distinguishable | Leaks stream existence to callers; new public constant; the existing id guard already uses A |

### Chosen Option: **A — reuse `marten.aggregate_not_found`**

### Rationale

- A stream with no recorded aggregate type (started outside Encina, or by `StartStream` without a type) fails the check when the option is on, fail closed: the documentation says so.
- The message names the requested aggregate type and id only, never the type that started the stream's events.

- **Class:** Settled
- **Settled by:** #1118 acceptance criteria ("returns `marten.aggregate_not_found`") and AGENTS.md section 3 (security gates fail closed)

</details>

<details>
<summary><strong>4. Round trips: sequential state read or one batched query</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Sequential `FetchStreamStateAsync` then `FetchStreamAsync`** | Simple; one place to read; matches the issue text | Two round trips per load with the option on |
| **B) One `IBatchedQuery` fetching state and events together** | One round trip | Different code path from the option off; the issue marks it "an implementation detail for the plan"; Marten's batched stream APIs differ per version and need verifying on Marten 9.38 |
| **C) Cache the stream state per session** | No second query on a reload within one session | Staleness inside a session; complicates `SaveAsync` flows |

### Chosen Option: **A — sequential (recommended, pending the maintainer)**

### Rationale

- The option is for hardening, not the hot path; two reads are acceptable and measurable. The plan adds a benchmark justification (`.md`) rather than a benchmark.
- If the integration test shows a significant cost, option B is a contained follow-up inside the helper (same signature).
- Option B wins when a deployment turns the option on for every load of a latency-sensitive command path; option A wins when the option guards occasional, user-supplied ids.

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Observability of a rejected load</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One new `[LoggerMessage]` (EventId 2701, Warning) with aggregate type and id** | Operators can tell wrong-type from not-found; follows `Log.AggregateNotFound` | Consumes one id of the `Marten` range |
| **B) Reuse `Log.AggregateNotFound`** | No new id | Indistinguishable from a missing stream |

### Chosen Option: **A — `Log.StreamAggregateTypeMismatch` (2701)**

### Rationale

- `EventIdRanges.Marten` is `(2600, 2799)` and `src/Encina.Marten` uses 2600-2700, so 2701 is the next id inside the package's existing range (no new range, no block needed).
- The message carries the requested type name and the stream id; the id is a technical aggregate id, not personal data (same as `Log.AggregateNotFound`).

- **Class:** Trivial
- **Settled by:** AGENTS.md section 7 (EventIds packed sequentially inside the package's registered range, ADR-021)

</details>

---

## Implementation Phases

### Phase 1: Option and guard helper

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten/EncinaMartenOptions.cs`: add `public bool VerifyStreamAggregateType { get; set; }` with XML docs (extra query per load, default `false`, streams without a recorded type fail).
2. New `src/Encina.Marten/StreamAggregateTypeGuard.cs`: `internal static class StreamAggregateTypeGuard` with `ValueTask<Either<EncinaError, Unit>> VerifyAsync(IDocumentSession session, Type aggregateType, Guid id, CancellationToken ct)`. Calls `session.Events.FetchStreamStateAsync(id, ct)`; `null` state or `state.AggregateType != aggregateType` returns `Left(EncinaErrors.Create(MartenErrorCodes.AggregateNotFound, ...))`.
3. `src/Encina.Marten/PublicAPI.Unshipped.txt`: add `Encina.Marten.EncinaMartenOptions.VerifyStreamAggregateType.get/set` (via `mechanical-fixer` when implementing).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.Marten (src/Encina.Marten), .NET 10 / C# 14. Issue #1118. EncinaMartenOptions is the options class read by MartenAggregateRepository and SnapshotAwareAggregateRepository.
TASK: Add EncinaMartenOptions.VerifyStreamAggregateType (default false) and the internal StreamAggregateTypeGuard.VerifyAsync described in the plan, Phase 1. Verify on the pinned Marten version that IEventStoreOperations.FetchStreamStateAsync(Guid, CancellationToken) exists and that StreamState.AggregateType is a Type? set by StartStream<T>.
KEY RULES: Either<EncinaError, T>, no exceptions for business outcomes; async with CancellationToken; XML docs on every public member; PublicAPI.Unshipped.txt line; no [Obsolete]; zero warnings.
REFERENCE FILES: src/Encina.Marten/EncinaMartenOptions.cs, src/Encina.Marten/MartenAggregateRepository.cs (lines 83-125, 340-360), src/Encina.Marten/MartenErrorCodes.cs
```

</details>

### Phase 2: Wire both repositories

<details>
<summary>Tasks</summary>

1. `MartenAggregateRepository.LoadAsync(Guid, ct)` (line 83) and `LoadAsync(Guid, int version, ct)` (line 129): when `_options.VerifyStreamAggregateType`, call the guard before `FetchStreamAsync`; on `Left` log `Log.StreamAggregateTypeMismatch` and return it.
2. `SnapshotAwareAggregateRepository.LoadAsync` (lines 79 and 113): same check at the top, before the snapshot lookup, so both the snapshot and the no-snapshot paths (`LoadWithSnapshotAsync`, line 305 onward) are covered.
3. Keep `aggregate.Id != id` unchanged (always-on layer).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 is done: StreamAggregateTypeGuard exists. Both repositories already guard on aggregate.Id != id.
TASK: Call the guard from the four LoadAsync entry points (two per repository) when options.VerifyStreamAggregateType is true; keep the option-off path byte-for-byte unchanged (same queries, same calls).
KEY RULES: the check runs before FetchStreamAsync and before any snapshot read; the returned error code is marten.aggregate_not_found; no extra query when the option is off; catch blocks keep logging through ex.ForLogging().
REFERENCE FILES: src/Encina.Marten/MartenAggregateRepository.cs, src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs
```

</details>

### Phase 3: Observability

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten/Log.cs`: add `[LoggerMessage(EventId = 2701, Level = Warning)] StreamAggregateTypeMismatch(ILogger, string aggregateType, Guid aggregateId)` and widen the XML doc range to 2600-2701 (the registry entry `EventIdRanges.Marten` already covers it).
2. Confirm `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` passes for the assembly (already mapped).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Encina.Marten owns EventIds 2600-2799 (EventIdRanges.Marten); 2600-2700 are in use.
TASK: Add EventId 2701 as a partial LoggerMessage in Log.cs and call it from the guard call sites of Phase 2.
KEY RULES: ADR-021; sequential ids; never log EncinaError.Message; message holds type name and id only.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, src/Encina.Marten/Log.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 4: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Marten/MartenAggregateRepositoryTests.cs` and a new `StreamAggregateTypeGuardTests`): option off performs no `FetchStreamStateAsync`; option on with matching type loads; null state, other type and null `AggregateType` each return `marten.aggregate_not_found`; both repositories, both overloads (NSubstitute for `IEventStoreOperations`).
2. Guard (`tests/Encina.GuardTests/Marten/Core/MartenAggregateRepositoryGuardTests.cs`): the helper's null `session` and `aggregateType` throw `ArgumentNullException`.
3. Integration (`tests/Encina.IntegrationTests/Infrastructure/Marten/Core/`, `MartenCollection` fixture, real PostgreSQL): a stream started for another aggregate type loads as `Left` with the option on and still replays (Id guard permitting) with it off; an own-type stream loads in both modes; a stream started with `StartStream` without a type fails with the option on.
4. Load and benchmark tests: `.md` justifications (one extra read, no concurrency), written by the implementing worker.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Per-flag coverage (AGENTS.md section 9). Package manifest: .github/coverage-manifest/Encina.Marten.json (touched files: MartenAggregateRepository.cs, Snapshots/SnapshotAwareAggregateRepository.cs, EncinaMartenOptions.cs, Log.cs, new StreamAggregateTypeGuard.cs).
TASK: Write the unit, guard and integration tests of Phase 4 using Encina.Testing.Shouldly assertions; add per-file targets with one-sentence justifications for the new and touched files to the manifest via mechanical-fixer.
KEY RULES: tests execute real package code; integration tests use the shared [Collection] Marten fixture and ClearAllDataAsync; no Thread.Sleep; CRAP <= 10 on changed methods.
REFERENCE FILES: tests/Encina.UnitTests/Marten/MartenAggregateRepositoryTests.cs, tests/Encina.IntegrationTests/Infrastructure/Marten/Core/MartenAggregateRepositoryIntegrationTests.cs, .github/coverage-manifest/Encina.Marten.json
```

</details>

### Phase 5: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs on the new option (done in Phase 1); `src/Encina.Marten/README.md`: a "Hardening" section describing the option, its cost and the typeless-stream caveat (via `docs-writer`).
2. `changelog.d/1118-marten-verify-stream-aggregate-type.added.md` (via `mechanical-fixer`).
3. Build `dotnet build --configuration Release` (0 warnings); `dotnet test` of the touched projects; every flag reaches its manifest target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Features are finished in Phases 1-4.
TASK: Document the option in the Encina.Marten README and add the changelog fragment; run the build and the tests.
KEY RULES: never edit CHANGELOG.md; fragment name <issue>-<slug>.<section>.md; docs by docs-writer; PublicAPI lines complete.
REFERENCE FILES: src/Encina.Marten/README.md, changelog.d/README.md, src/Encina.Marten/PublicAPI.Unshipped.txt
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| AGENTS.md section 1 | Opt-in features, provider-agnostic abstractions |
| AGENTS.md section 3 | Fail closed, ROP, no swallowed errors |
| ADR-021 | EventId ranges |
| #1095 / #1116 | Introduced the id-based guard this plan hardens |
| Marten `StreamState.AggregateType` | Type recorded by `StartStream<T>` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EncinaMartenOptions` | `src/Encina.Marten/EncinaMartenOptions.cs` | Hosts the new flag |
| `MartenAggregateRepository<T>` | `src/Encina.Marten/MartenAggregateRepository.cs` | Two `LoadAsync` entry points |
| `SnapshotAwareAggregateRepository<T>` | `src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs` | Two `LoadAsync` entry points |
| `MartenErrorCodes.AggregateNotFound` | `src/Encina.Marten/MartenErrorCodes.cs` | Returned error code |
| `Log` | `src/Encina.Marten/Log.cs` | New message 2701 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Marten | 2701 | Inside the registered `EventIdRanges.Marten` (2600-2799); no new range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | 4 | Options, guard (new), two repositories, `Log.cs` |
| Tests | 4 | Unit, guard, integration, justification `.md` x2 |
| Docs and metadata | 3 | README, changelog fragment, PublicAPI |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #1118: opt-in stream aggregate-type verification in Marten LoadAsync. AGENTS.md rules apply (ROP, opt-in, zero warnings, per-flag coverage, CRAP <= 10).

IMPLEMENTATION OVERVIEW: (1) EncinaMartenOptions.VerifyStreamAggregateType + internal StreamAggregateTypeGuard; (2) call it before FetchStreamAsync / snapshot lookup in the four LoadAsync entry points; (3) EventId 2701 in Log.cs; (4) unit, guard and PostgreSQL integration tests; (5) README, changelog fragment, PublicAPI.

KEY PATTERNS: Either<EncinaError, T>; marten.aggregate_not_found; ex.ForLogging(); shared [Collection] Marten fixture; specialists for PublicAPI, changelog and manifest lines.

REFERENCE FILES: src/Encina.Marten/MartenAggregateRepository.cs, src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs, src/Encina.Marten/EncinaMartenOptions.cs, src/Encina.Marten/Log.cs, .github/coverage-manifest/Encina.Marten.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The state is read once per load; caching it would defeat the check |
| 2 | OpenTelemetry | ⏭️ | Add the extra query to a `LoadAsync` span when Marten repositories get spans (#1043 scope); no new activity here |
| 3 | Structured Logging | ✅ | EventId 2701 inside the `Marten` range (Phase 3) |
| 4 | Health Checks | ❌ | No new dependency |
| 5 | Validation | ❌ | The option is a boolean |
| 6 | Resilience | ❌ | Same session and connection as the load |
| 7 | Distributed Locks | ❌ | Read-only check |
| 8 | Transactions | ❌ | Read-only check |
| 9 | Idempotency | ❌ | Read-only check |
| 10 | Multi-Tenancy | ❌ | Stream state is tenant-scoped by the session already |
| 11 | Module Isolation | ✅ | The check itself is a module-isolation safeguard for shared stores |
| 12 | Audit Trail | ❌ | No state change |

---

## Next Steps

1. Commit this plan, open its PR and link it from #1118.
2. When #1118 is picked up, re-check the choices against today's code (notably Marten batched-query support for choice 4) and ask the maintainer about the Dilemma.
3. Brief a worker for Phases 1-5 (one PR, about 12 files).
