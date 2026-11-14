# Implementation Plan: Timezone-aware scheduling — DST policy, zone ids and observability

> **Issue**: [#150](https://github.com/dlrivada/Encina/issues/150)
> **Type**: Feature (the part of the issue after the 1.0 slice taken by #1222)
> **Complexity**: Medium (5 phases, no schema change, provider-independent logic)
> **Estimated Scope**: ~250 lines of production code + ~700 lines of tests, documentation and manifests; 21 file edits in 2 pull requests
> **Family**: scheduling-1 (builds on #1222; shares the recurring path with #133)

---

## Summary

SPEC-002 DEC-005 (a) takes a minimal slice of this issue into 1.0 as #1222: the `TimeZoneId` column and property on all 10 providers, a zone-aware `ICronParser` (a shipped default), `scheduling.invalid_time_zone`, and `SchedulerOrchestrator` evaluating each recurrence in the stored zone, with the AC-033 case (09:00 Europe/Madrid on both sides of a DST change) as acceptance. That plan owns the column, the parser signature and the orchestrator changes, so this plan does not repeat them.

What this plan delivers is the rest of the issue, checked against today's code (`SchedulingOptions` has no zone option, `SchedulingErrorCodes` has no zone code, `TimeZoneId` does not exist yet):

1. **The DST policy** for local times that do not exist (spring forward) or happen twice (fall back): chosen, written down and pinned by tests, so a library upgrade cannot change it silently.
2. **Zone id normalization**: the id persisted is the portable IANA id, so a schedule created on Windows with a Windows id does not fail on a Linux host.
3. **The `DefaultTimeZone` decision** of the issue.
4. **Observability** of rejected zones: counter `encina.scheduling.invalid_time_zone` and log events.
5. **Documentation**: the DST semantics section of the cron and time-zone ADR that #1222 opens.

**Provider category**: Database (10) for the persisted zone, but the persistence is entirely #1222 (columns, read and write, round-trip integration tests on the 10 providers). The logic of this plan lives in `Encina.Messaging` and is provider-independent, so it adds no provider code and no provider tests beyond what #1222 delivers.

**Affected packages**: `Encina.Messaging` only (plus tests and docs).

**Split**: the honest count is 21 file edits (production 6, tests 8, manifests 2, justification files 2, documentation 3), over the 15-file limit of AGENTS.md section 11, so the plan is two pull requests. Public API is unchanged: `SchedulingTimeZones`, the log class and the metrics class are internal, so no `PublicAPI.Unshipped.txt` edit is needed.

| PR | Content | Files | Lines (est.) |
| --- | --- | ---: | ---: |
| Z1 | Normalization in `SchedulingTimeZones`, orchestrator use, DST conformance, property, contract and guard tests | 9 | 400 |
| Z2 | Counter, log events, span attribute, scenario, documentation, justifications | 12 | 350 |
| **Total** | | **21** | **about 750** |

Z1: 2 src (`SchedulingTimeZones.cs`, `SchedulerOrchestrator.cs`) + 6 tests + 1 manifest = 9. Z2: 4 src (`SchedulingScheduleMetrics.cs`, `SchedulingScheduleLog.cs`, `SchedulingActivitySource.cs`, `SchedulerOrchestrator.cs`) + 2 tests + 1 manifest + 2 justification files + 3 documentation files = 12. The header, this table and the Estimated File Count table all state 21. Z1 merges after #1222 P12; Z2 after Z1.

**Story shared with the sibling plans**: #1222 creates `SchedulingErrorCodes.InvalidTimeZone`, `SchedulingTimeZones`, `CronosCronParser`, `SchedulingScheduleLog` (ids 2326-2331) and `SchedulingScheduleMetrics`. This plan adds members to them and takes ids 2350-2351 for the two events in the same class `SchedulingScheduleLog`; the batch registry holds 2326-2389 for #1222, #146, #150, #133 and #147, so each plan's sub-range is its own to state. ADR 062 is merged (PR #3599); this plan adds no error code.

---

## Design Choices

<details>
<summary><strong>1. What happens at a DST transition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A nonexistent local time runs at the next valid instant; an ambiguous one runs once, at the first occurrence** | Never skipped, never doubled; intended to match the documented behavior of Cronos, the default parser of #1222, so no own logic | A run in the gap is shifted (02:30 on the spring-forward day runs at 03:00) |
| **B) A nonexistent time is skipped; an ambiguous one runs at both occurrences** | Literal cron semantics | Missed runs and duplicate runs; needs a wrapper around the parser |
| **C) Whatever the cron library does, undocumented** | No own logic | The behavior silently depends on the library chosen in #1222 (its choice 2) and is hard to document or test |

### Chosen Option: **A — next valid instant, first occurrence** (recommended, pending the maintainer)

### Rationale

- Option A wins for reminders and reports, where a missing or duplicated run is worse than a run an hour late. Option B wins for strictly literal schedules; option C only if the library is trusted completely.
- Under A this plan adds tests, not logic: the conformance tests (gap day, overlap day, a southern-hemisphere zone, a half-hour zone, a zone without DST) assert the policy against the shipped parser, so they fail if a library upgrade changes it. The tests are also the verification of the Cronos behavior: this plan states the policy it wants and the tests prove whether Cronos delivers it; if Cronos differs, the maintainer decides between B (a wrapper `ICronParser` decorator, about 3 more files) and changing the policy.
- The retention-floor plan (#1187) resolves ambiguous local times to the later occurrence (and invalid ones to the later valid instant) because a retention floor must not expire early. The scheduling policy differs on purpose for the ambiguous case (first occurrence: a run is not postponed by an hour) and agrees on the gap. The two plans are recorded here as different by design; the maintainer confirms it with this choice.
- The semantics are documented in the ADR of #1222 (ADR number assigned when the ADR pull request opens).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. A `DefaultTimeZone` option</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SchedulingOptions.DefaultTimeZone`, default "UTC", applied when a schedule passes no zone** | One place for an application-wide zone | An implicit zone surprises a caller who forgets to pass one; "UTC" default equals today's null behavior, so the option adds no capability for the default case |
| **B) No default option; the zone is per schedule only, null means UTC** | Explicit; no hidden configuration; a tenant-level default can be added with multi-tenancy (#739) where it belongs | The zone is repeated at each call |

### Chosen Option: **B — per schedule only** (recommended, pending the maintainer)

### Rationale

- Option B wins when multi-region use is the norm (a global default is wrong for most users); option A wins for single-region applications.
- `SchedulingOptions` today has `ProcessingInterval`, `BatchSize`, `MaxRetries`, `BaseRetryDelay`, `EnableProcessor` and `EnableRecurringMessages`; nothing else would change under B.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Which form of the zone id is persisted (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Store the id as given, once `TimeZoneInfo.FindSystemTimeZoneById` accepts it** | No conversion code | A Windows id ("W. Europe Standard Time") stored on one host may not resolve on another; two ids for one zone |
| **B) Normalize to the IANA id (`TimeZoneInfo.TryConvertWindowsIdToIanaId`) before validating and storing; when the conversion is not available on the host (no ICU data) a Windows id is rejected with `scheduling.invalid_time_zone` instead of being stored non-portably** | One portable form in the column; AC-033 uses the IANA id; fails closed | A small conversion step; a Windows host without ICU cannot schedule with a Windows id |

### Chosen Option: **B — normalize to IANA** (recommended, pending the maintainer)

### Rationale

- Option B wins whenever schedules outlive the host that created them (a database shared by Windows and Linux instances, a container move); option A wins when all hosts are known to be the same operating system.
- The conversion happens in `SchedulingTimeZones` (created by #1222), so the parser and the orchestrator see the normalized id.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How the zone reaches the parser (shared with #1222)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A zone parameter on `ICronParser.GetNextOccurrence`** | The parser that understands zones does the work; DST logic in one place | Every implementation, including test fakes, takes the parameter |
| **B) The orchestrator converts around a UTC-only parser** | The interface stays | DST gap and overlap re-implemented outside the parser |

### Chosen Option: **A — zone parameter on the parser** (recommended, pending the maintainer)

### Rationale

- This is choice 3 of #1222 and one answer covers both plans; this plan assumes A because the DST conformance tests of choice 1 run against the parser.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. A null zone means UTC</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Null means UTC** | Today's behavior (cron evaluated in UTC); the issue's own contract | A caller must pass a zone to get local time |
| **B) Null is rejected for recurring schedules** | Forces an explicit zone | Contradicts the issue text and the `timeZoneId` contract of #1222 |

### Chosen Option: **A — null means UTC**

### Rationale

- The issue states the contract ("If null, UTC is used") and #1222 implements it (its choice 3: null `timeZoneId` means UTC). Option B contradicts the cited text, so it does not satisfy it.

**Class:** Settled
**Settled by:** issue #150 text ("If null, UTC is used"), the `timeZoneId` contract of #1222 (its choice 3)

</details>

<details>
<summary><strong>6. Time zone data comes from the runtime (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TimeZoneInfo` of .NET 10** | No extra package; pay-for-what-you-use | Minimal container images may lack tz data; behavior depends on the host's data |
| **B) Add TimeZoneConverter or NodaTime** | Independent of host data (answers the minimal-image risk the plan itself flags) | A new dependency for a conversion .NET 10 already offers |

### Chosen Option: **A — `TimeZoneInfo`** (recommended, pending the maintainer)

### Rationale

- Option A wins for the smallest footprint; option B wins when hosts cannot be trusted to carry tz data. Pay-for-what-you-use does not forbid a dependency, so both options comply.
- `TimeZoneInfo.FindSystemTimeZoneById` resolves IANA ids and `TimeZoneInfo.TryConvertWindowsIdToIanaId` converts Windows ids. The documentation states the tz-data requirement for minimal images. (The retention-floor plan #1187 uses `TimeZoneInfo` as well, but only `FindSystemTimeZoneById`, so it is not a precedent for the conversion.)

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Canonical spelling of an IANA id (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Store the id as supplied once it resolves ("Asia/Calcutta" and "Asia/Kolkata" stay different strings)** | No rewriting; what the caller sent is what is read back | Two spellings of one zone; nothing compares them (the unique index is on the schedule key, not the zone) |
| **B) Store `TimeZoneInfo.Id` of the resolved zone** | One spelling per host | The spelling depends on the host's data and can differ between hosts, which defeats portability |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Option A wins for predictability; option B would reintroduce host dependence in the column.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Observability details</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The counter `encina.scheduling.invalid_time_zone` has no tags (the rejected id is user input: unbounded cardinality); the zone is a span attribute, not a metric tag; event 2350 `TimeZoneNormalized` at Debug (message id only), event 2351 `TimeZoneRejected` at Warning (error code only)** | Bounded metric cardinality; no user input in logs | The rejected id is not visible in telemetry |
| **B) Tag the counter with the id** | Shows which ids fail | Unbounded cardinality and user input in telemetry |

### Chosen Option: **A**

### Rationale

- A rejected id can be anything a caller typed; keeping it out of tags and logs follows the logging rule that user-controlled values do not reach telemetry (AGENTS.md section 3, error messages never reach logs).

**Class:** Trivial

</details>

---

## Implementation Phases

### Phase 1: Zone resolution and normalization (Z1)

> **Goal**: One internal place that turns a user-supplied id into a validated, portable IANA id.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Scheduling/SchedulingTimeZones.cs` (created by #1222): add `TryNormalize(string? id, out string ianaId, out TimeZoneInfo zone)`: null or empty returns UTC semantics; an id that resolves and is IANA-shaped is kept; a Windows id that `TimeZoneInfo.TryConvertWindowsIdToIanaId` maps is replaced by the IANA id; anything else, including a Windows id on a host where the conversion is unavailable, fails (choices 3 and 7).
2. `SchedulerOrchestrator.cs` (changed by #1222) stores the normalized id in the message's `TimeZoneId` and returns `Left(scheduling.invalid_time_zone)` on failure (the code exists from #1222).
3. Tests in the same PR: `SchedulingTimeZonesTests` (IANA kept, Windows id converted, unknown id rejected, null is UTC, conversion unavailable is rejected), the orchestrator rejection path in `SchedulerOrchestratorScheduleRecurringTests`, `CronosCronParserDstTests` (Phase 2), the property test (Phase 2), the DST cases added to the `ICronParser` contract test of #1222 (any implementation), and the guard test below.
4. Guard test: `ScheduleRecurringAsync` with a null `timeZoneId` is valid and returns the UTC result (a nullable parameter that accepts null gets a valid-result test, not an exception test); the null cron expression guard belongs to `CronosCronParser` and is #1222's.
5. Manifest entries of `Encina.Messaging` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #150. #1222 has merged: SchedulingTimeZones, CronosCronParser, the TimeZoneId column and the orchestrator zone handling exist.

TASK:
Add normalization to SchedulingTimeZones and use it where the orchestrator validates a zone.

KEY RULES:
- Business input errors are Left values (scheduling.invalid_time_zone), never exceptions; an unavailable conversion is a Left, never a non-portable stored id.
- A null zone stays UTC.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulingTimeZones.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorScheduleRecurringTests.cs
```

</details>

### Phase 2: DST conformance of the shipped parser (Z1)

<details>
<summary><strong>Tasks</strong></summary>

1. `tests/Encina.UnitTests/Messaging/Scheduling/CronosCronParserDstTests.cs`: Europe/Madrid 09:00 daily on both sides of the October and March changes (AC-033); America/New_York 09:00 across 2024-03-10/11 (the example of the issue); a 02:30 job on the spring-forward day (runs at the next valid instant) and on the fall-back day (runs once); Australia/Sydney (southern hemisphere); Asia/Kolkata (half-hour offset, no DST); UTC and a null zone. These tests are the verification of the Cronos gap and overlap behavior stated in choice 1.
2. If the maintainer answers choice 1 with B or C, replace the conformance assertions accordingly and add a `DstPolicyCronParser` decorator (B only).
3. `tests/Encina.PropertyTests/Messaging/Scheduling/`: extend the parser property tests of #1222 with generated zones and dates around transitions: the result is after the input and a fixed-time job never runs twice within one local day.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- The DST policy is choice 1 of the plan; implement the answer recorded when the maintainer decides (A by default: Cronos behavior pinned by tests).

TASK:
Write the DST conformance unit tests and the property tests listed in the plan.

KEY RULES:
- Use FakeTimeProvider-independent inputs: the parser takes afterUtc explicitly.
- Tests execute the real CronosCronParser; no reflection-only tests; assert instants computed from TimeZoneInfo of the same host and skip nothing silently.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/CronosCronParser.cs
- tests/Encina.PropertyTests/Messaging/Scheduling/
```

</details>

### Phase 3: Observability and cross-cutting integration (Z2)

<details>
<summary><strong>Tasks</strong></summary>

1. `SchedulingScheduleMetrics` (created by #1222): counter `encina.scheduling.invalid_time_zone` (no tags, choice 8), incremented by the orchestrator rejection path.
2. `SchedulingScheduleLog` (created by #1222, ids 2326-2331): events 2350 `TimeZoneNormalized` (Debug; message id only) and 2351 `TimeZoneRejected` (Warning; error code only), inside `EventIdRanges.MessagingScheduling` (2300-2399, already mapped to `Encina.Messaging`).
3. Span attribute `encina.scheduling.time_zone` on the recurring schedule and run spans in `SchedulingActivitySource`.
4. Tests in the same PR: an exporter test of the counter and the span attribute, and a test that no log, tag or metric carries the rejected id, the schedule key or the request content; the scenario `SchedulerCycleScenarios` (existing, shared EF base) gains the DST case: a recurring schedule with a zone is created, the clock is advanced over a DST change and the next run is persisted at the expected UTC instant (the zone round trip on the 10 providers is #1222).
5. Load and benchmark `.md` justifications (not a concurrent hot path): `tests/Encina.LoadTests/Messaging/Scheduling/TimeZones.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/Scheduling/TimeZones.md`. Unit, guard, contract, property and integration are real tests, never a `.md`.
6. Manifest entry through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- EventIds 2350-2351 are used by #150 inside MessagingScheduling; the batch registry holds 2326-2389 for #1222, #146, #150, #133 and #147.

TASK:
Add the counter, the two log events and the span attribute.

KEY RULES:
- EncinaError.Message never reaches logs, tags or health results.
- Pack ids sequentially; the rejected id is never recorded.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs
- src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/SchedulerCycleScenarios.cs
```

</details>

### Phase 4: Per-file coverage targets (entered in the PR that carries the tests)

<details>
<summary><strong>Tasks</strong></summary>

| File | PR | Flag and target | Justification |
| --- | --- | --- | --- |
| `Scheduling/SchedulingTimeZones.cs` | Z1 | unit 95, guard 0, property 70 | Every normalization branch is a unit test; the property test covers generated zones; the type is internal without `InternalsVisibleTo` for `Encina.GuardTests` (its nullable id accepts null and is covered by the valid-result test in unit). |
| `Scheduling/CronosCronParser.cs` | Z1 | the targets set by #1222, never lowered; contract: the DST cases are added | The DST conformance and contract cases run the real parser. |
| `Scheduling/SchedulerOrchestrator.cs` | Z1, Z2 | unit and guard: the current manifest targets (the worker confirms them); integration: current target | The rejection path and counter are unit-tested; the DST scenario adds integration coverage. |
| `Diagnostics/SchedulingScheduleMetrics.cs` | Z2 | unit: current target | The exporter test drives the new counter. |
| `Diagnostics/SchedulingScheduleLog.cs` | Z2 | unit 0 | `[LoggerMessage]` declarations; the generated bodies are not in the file. |
| `Diagnostics/SchedulingActivitySource.cs` | Z2 | unit: current target | The exporter test reads the new attribute. |

Measure with `dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests` (and the other flag projects) and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- Tests execute real code; Shouldly via Encina.Testing.Shouldly; deterministic dates; targets never go down.

TASK:
Enter the targets above in the manifest of the pull request that carries the tests, with the one-sentence justification.

KEY RULES:
- No Thread.Sleep; a test never swallows an exception thrown by the code it drives; a 0 only with the stated reason.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Messaging.json
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorScheduleRecurringTests.cs
```

</details>

### Phase 5: Documentation and finalization (Z2)

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on the `timeZoneId` parameters (the IANA form, null means UTC) and on `SchedulingTimeZones` members that are public, if any.
2. The DST semantics and zone normalization sections of the cron and time-zone ADR (ADR number assigned when the ADR pull request opens) and of `docs/features/scheduling.md`, through the `docs-writer`, including the tz-data note for minimal container images and the ICU note for Windows ids.
3. `changelog.d/150-scheduling-time-zone.added.md` (a `mechanical-fixer` writes it).
4. `dotnet build --configuration Release` with 0 warnings; all flags at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Pages under docs/ go to the docs-writer; changelog and PublicAPI lines to the mechanical-fixer.

TASK:
Finish the XML docs and delegate the ADR section, the feature page and the changelog.

KEY RULES:
- Never edit CHANGELOG.md; never type coverage figures by hand.

REFERENCE FILES:
- docs/features/scheduling.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| SPEC-002 REQ-033, AC-033, S4 | Time-zone-aware recurring cron | 09:00 Europe/Madrid fires at 09:00 local on both sides of a DST change |
| IANA time zone database | Zone ids | Portable form persisted in the column |
| ADR-018 | Cross-cutting integration | Matrix below |
| ADR-021 | EventIds | Ids 2350-2351 inside `MessagingScheduling` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `CronosCronParser`, `SchedulingTimeZones` | `src/Encina.Messaging/Scheduling/` (created by #1222) | Extended and tested |
| `SchedulerOrchestrator.HandleRecurringMessageAsync` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Evaluates each recurrence in the stored zone (#1222) |
| `SchedulingScheduleMetrics`, `SchedulingScheduleLog` | `src/Encina.Messaging/Diagnostics/` (created by #1222) | Counter and events |
| `SchedulerCycleScenarios` | `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/` | End-to-end DST scenario |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingScheduling`, existing 2300-2399) | **2350-2351** used by #150 (2352-2359 held) | Used on main: 2300-2306, 2320-2325. The batch registry (`eventid-blocks.md`) holds 2326-2389 for #1222, #146, #150, #133 and #147 together; each plan states its own sub-range |
| New range | none | The block 6750-6799 is not needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 6 | Z1 2 (`SchedulingTimeZones.cs`, `SchedulerOrchestrator.cs`), Z2 4 (metrics, log, activity source, `SchedulerOrchestrator.cs`) |
| Tests | 8 | Z1 6 (normalization, orchestrator rejection, DST, property, contract, guard), Z2 2 (exporter and no-leak, scenario) |
| Coverage manifests | 2 | Z1 1, Z2 1 |
| Justification files | 2 | Z2: load and benchmark |
| Documentation and changelog | 3 | Z2: ADR section, feature page, fragment |
| **Total** | **21** | 6 + 8 + 2 + 2 + 3 file edits (two pull requests of 9 and 12) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider.
- Issue #150 after #1222: the column, the zone-aware parser and the orchestrator zone handling already exist.

IMPLEMENTATION OVERVIEW:
Z1: normalize zone ids to IANA in SchedulingTimeZones; DST conformance tests of the shipped parser (policy decided by the maintainer, A by default); property, contract and guard tests.
Z2: counter encina.scheduling.invalid_time_zone, events 2350-2351, span attribute, DST scenario, docs, ADR section, changelog.

KEY PATTERNS:
- No schema or provider code; the zone round trip on 10 providers is #1222.
- No DefaultTimeZone option unless the maintainer chooses it.
- The rejected zone id and the schedule key are never recorded in logs or metric tags.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Messaging/Scheduling/CronosCronParser.cs
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorScheduleRecurringTests.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No distributed cache is needed; an in-process dictionary of resolved zones is an optimization to add only after a benchmark |
| 2 | OpenTelemetry | ✅ | Phase 3: counter `encina.scheduling.invalid_time_zone` and a span attribute for the zone |
| 3 | Structured Logging | ✅ | Phase 3: events 2350-2351 inside the registered range |
| 4 | Health Checks | ❌ | No checkable dependency is added |
| 5 | Validation | ✅ | Phase 1: zone ids are validated and normalized when scheduling; invalid ids return Left |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ⏭️ | Multi-instance claiming is #1251 |
| 8 | Transactions | ❌ | No store operation is added; the zone is written by the #1222 methods in the caller's transaction |
| 9 | Idempotency | ❌ | Not a message or request entry point |
| 10 | Multi-Tenancy | ⏭️ | A tenant-level default zone belongs with the tenant column of #739; choice 2 recommends no global default now |
| 11 | Module Isolation | ❌ | SPEC-002 asks no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant data is created |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #1222 | Must merge first (at least P12) | Column, `SchedulingTimeZones`, `CronosCronParser`, zone handling in the orchestrator, ADR, log and metrics classes |
| #133 | Sibling | Recurring end modes and missed-occurrence handling use the same stored zone |
| #739 | Later | Tenant-level default zone, if wanted |

---

## Next Steps

1. Review this plan; the Dilemmas (choices 1, 2, 3, 4, 6 and 7: DST policy, `DefaultTimeZone`, zone id form, parser contract shared with #1222, tz data source, id spelling) are answered by the maintainer when work starts; the answers are then recorded in a `## Maintainer Decisions` section added at that time.
2. Implement after #1222 P12 merges; two pull requests (Z1, Z2).
3. Link the plan from #150.
