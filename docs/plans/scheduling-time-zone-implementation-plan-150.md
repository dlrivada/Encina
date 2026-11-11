# Implementation Plan: Timezone-aware scheduling — DST policy, zone ids and observability

> **Issue**: [#150](https://github.com/dlrivada/Encina/issues/150)
> **Type**: Feature (the part of the issue after the 1.0 slice taken by #1222)
> **Complexity**: Medium (5 phases, no schema change, provider-independent logic)
> **Estimated Scope**: ~250 lines of production code + ~700 lines of tests, ~14 files in 1 pull request
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

---

## Design Choices

<details>
<summary><strong>1. What happens at a DST transition</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A nonexistent local time runs at the next valid instant; an ambiguous one runs once, at the first occurrence** | Never skipped, never doubled; matches the documented behavior of Cronos, the default parser of #1222, so no own logic | A run in the gap is shifted (02:30 on the spring-forward day runs at 03:00) |
| **B) A nonexistent time is skipped; an ambiguous one runs at both occurrences** | Literal cron semantics | Missed runs and duplicate runs; needs a wrapper around the parser |
| **C) Whatever the cron library does, undocumented** | No own logic | The behavior silently depends on the library chosen in #1222 (Dilemma 2) and is hard to document or test |

### Chosen Option: **A — next valid instant, first occurrence** (recommended, pending the maintainer)

### Rationale

- Option A wins for reminders and reports, where a missing or duplicated run is worse than a run an hour late. Option B wins for strictly literal schedules; option C only if the library is trusted completely.
- Under A this plan adds tests, not logic: the conformance tests (gap day, overlap day, a southern-hemisphere zone, a half-hour zone, a zone without DST) assert the policy against the shipped parser, so they fail if a library upgrade changes it. If the maintainer picks B, a wrapper `ICronParser` decorator is added and this plan grows by about 3 files.
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
<summary><strong>3. Which form of the zone id is persisted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Store the id as given, once `TimeZoneInfo.FindSystemTimeZoneById` accepts it** | No conversion code | A Windows id ("W. Europe Standard Time") stored on one host may not resolve on another; two ids for one zone |
| **B) Normalize to the IANA id (`TimeZoneInfo.TryConvertWindowsIdToIanaId`) before validating and storing** | One portable form in the column; AC-033 uses the IANA id | A small conversion step; an unmappable id returns `scheduling.invalid_time_zone` |

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

- This is Dilemma 3 of #1222 and one answer covers both plans; this plan assumes A because the DST conformance tests of choice 1 run against the parser.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. A null zone means UTC</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Null means UTC** | Today's behavior (cron evaluated in UTC); no migration of existing rows | A caller must pass a zone to get local time |
| **B) Null is rejected for recurring schedules** | Forces an explicit zone | Breaks every existing recurring caller for no gain |

### Chosen Option: **A — null means UTC**

### Rationale

- Existing rows have no zone and keep their meaning; the original issue states the same contract ("If null, UTC is used").

**Class:** Trivial
**Settled by:** AGENTS.md section 1 (there are no existing users to protect, so the choice follows the simplest contract of the issue), #150 issue text

</details>

<details>
<summary><strong>6. Time zone data comes from the runtime</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TimeZoneInfo` of .NET 10** | No extra package; pay-for-what-you-use | Minimal container images may lack tz data |
| **B) Add TimeZoneConverter or NodaTime** | Independent of host data | A new dependency for a conversion .NET 10 already offers |

### Chosen Option: **A — `TimeZoneInfo`**

### Rationale

- `TimeZoneInfo.FindSystemTimeZoneById` resolves IANA ids and `TryConvertWindowsIdToIanaId` converts Windows ids; the retention-floor plan (#1187) relies on the same behavior. The documentation states the tz-data requirement for minimal images.

**Class:** Settled
**Settled by:** AGENTS.md section 1 (pay-for-what-you-use), #1187 (precedent in the retention-floor plan)

</details>

---

## Implementation Phases

### Phase 1: Zone resolution and normalization

> **Goal**: One internal place that turns a user-supplied id into a validated, portable IANA id.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Scheduling/SchedulingTimeZones.cs` (created by #1222): add `TryNormalize(string? id, out string ianaId, out TimeZoneInfo zone)`: null or empty returns UTC semantics; an id that resolves and is IANA-shaped is kept; a Windows id that `TimeZoneInfo.TryConvertWindowsIdToIanaId` maps is replaced by the IANA id; anything else fails.
2. The orchestrator (changed by #1222) stores the normalized id in the draft's `TimeZoneId` and returns `Left(scheduling.invalid_time_zone)` on failure.
3. `SchedulingErrorCodes`: no new code (`InvalidTimeZone` is created by #1222).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #150. #1222 has merged: SchedulingTimeZones, CronosCronParser, the TimeZoneId column and the orchestrator zone handling exist.

TASK:
Add normalization to SchedulingTimeZones and use it where the orchestrator validates a zone.

KEY RULES:
- Business input errors are Left values (scheduling.invalid_time_zone), never exceptions.
- A null zone stays UTC.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulingTimeZones.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- docs/plans/retention-floor-implementation-plan-1187.md
```

</details>

### Phase 2: DST conformance of the shipped parser

<details>
<summary><strong>Tasks</strong></summary>

1. `tests/Encina.UnitTests/Messaging/Scheduling/CronosCronParserDstTests.cs`: Europe/Madrid 09:00 daily on both sides of the October and March changes (AC-033); America/New_York 09:00 across 2024-03-10/11 (the example of the issue); a 02:30 job on the spring-forward day (runs at the next valid instant) and on the fall-back day (runs once); Australia/Sydney (southern hemisphere); Asia/Kolkata (half-hour offset, no DST); UTC and a null zone.
2. If the maintainer answers choice 1 with B or C, replace the conformance assertions accordingly and add a `DstPolicyCronParser` decorator (B only).
3. `tests/Encina.PropertyTests/Messaging/Scheduling/`: extend the parser property tests of #1222 with generated zones and dates around transitions: the result is after the input and a fixed-time job never runs twice within one local day.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- The DST policy is choice 1 of the plan; implement the answer recorded in Maintainer Decisions (A by default: Cronos behavior pinned by tests).

TASK:
Write the DST conformance unit tests and the property tests listed in the plan.

KEY RULES:
- Use FakeTimeProvider-independent inputs: the parser takes afterUtc explicitly.
- Tests execute the real CronosCronParser; no reflection-only tests.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/CronosCronParser.cs
- tests/Encina.PropertyTests/Messaging/Scheduling/
```

</details>

### Phase 3: Observability and cross-cutting integration

<details>
<summary><strong>Tasks</strong></summary>

1. `SchedulingScheduleMetrics` (created by #1222): counter `encina.scheduling.invalid_time_zone` (no tags; the rejected id is user input and is not recorded).
2. `SchedulingScheduleLog`: events 2350 `TimeZoneNormalized` (Debug; message id only) and 2351 `TimeZoneRejected` (Warning; error code only), inside `EventIdRanges.MessagingScheduling` (2300-2399; #150 reserves 2350-2359).
3. Span attribute `encina.scheduling.time_zone` on the recurring schedule and run spans (a span attribute, not a metric tag, to keep metric cardinality bounded).
4. A test that no log, tag or metric carries the schedule key or the request content.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- EventIds 2350-2359 are reserved for #150 inside MessagingScheduling; #1222 owns 2326-2339, #146 2340-2349, #133 2360-2379, #147 2380-2389.

TASK:
Add the counter, the two log events and the span attribute.

KEY RULES:
- EncinaError.Message never reaches logs, tags or health results.
- Pack ids sequentially from 2350.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs
- src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs
```

</details>

### Phase 4: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit**: normalization (IANA kept, Windows id converted, unknown id rejected, null is UTC) in `SchedulingTimeZonesTests`; orchestrator rejection path with the counter.
2. **Guard**: `SchedulingTimeZones` and the parser reject null cron expressions with `ArgumentNullException`.
3. **Contract**: the `ICronParser` contract test of #1222 gains the DST cases for any implementation.
4. **Property**: phase 2.
5. **Integration**: a recurring schedule with a zone is created, the clock is advanced over a DST change and the next run is persisted at the expected UTC instant, run through the shared EF scenario base `SchedulerCycleScenarios` (the logic is provider-independent; the zone round trip on the 10 providers is #1222).
6. **Load and benchmark**: `.md` justifications (not a concurrent hot path).
7. **Obligations**: `Scheduling/SchedulingTimeZones.cs` unit 95, guard 60, property 70; `Scheduling/CronosCronParser.cs` keeps the targets set by #1222 (targets never go down). Measure with `dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- Tests execute real code; Shouldly via Encina.Testing.Shouldly; deterministic dates.

TASK:
Write the tests listed in the plan.

KEY RULES:
- No Thread.Sleep.
- Time zone data differs per host: assert instants computed from TimeZoneInfo of the same host, and skip nothing silently.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/SchedulerCycleScenarios.cs
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorScheduleRecurringTests.cs
```

</details>

### Phase 5: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on `SchedulingTimeZones` members that are public, if any, and on the `timeZoneId` parameters (the IANA form, null means UTC).
2. The DST semantics and zone normalization sections of the cron and time-zone ADR (ADR number assigned when the ADR pull request opens) and of `docs/features/scheduling.md`, through the `docs-writer`, including the tz-data note for minimal container images.
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
| ADR-021 | EventIds | Ids 2350-2359 inside `MessagingScheduling` |

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
| `Encina.Messaging` (`MessagingScheduling`, existing 2300-2399) | **2350-2359** for #150 (uses 2350-2351) | Used: 2300-2306, 2320-2325; reserved: #1222 2326-2339, #146 2340-2349, #133 2360-2379, #147 2380-2389 |
| New range | none | The block 6750-6799 is not needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Core `Encina.Messaging` | ~4 | time zones, orchestrator, metrics, log |
| Tests | ~7 | unit, guard, contract, property, integration |
| Documentation and changelog | ~3 | ADR section, feature page, fragment |
| **Total** | **~14** | one pull request |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider.
- Issue #150 after #1222: the column, the zone-aware parser and the orchestrator zone handling already exist.

IMPLEMENTATION OVERVIEW:
Phase 1: normalize zone ids to IANA in SchedulingTimeZones.
Phase 2: DST conformance tests of the shipped parser (policy of Maintainer Decisions, A by default).
Phase 3: counter encina.scheduling.invalid_time_zone, events 2350-2351, span attribute.
Phase 4: tests. Phase 5: docs, ADR section, changelog.

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
| #1222 | Must merge first | Column, `SchedulingTimeZones`, `CronosCronParser`, zone handling in the orchestrator, ADR, log and metrics classes |
| #133 | Sibling | Recurring end modes and missed-occurrence handling use the same stored zone |
| #739 | Later | Tenant-level default zone, if wanted |

---

## Next Steps

1. Review this plan; the Dilemmas (DST policy, `DefaultTimeZone`, zone id form, parser contract shared with #1222) are answered by the maintainer when work starts and recorded in `## Maintainer Decisions`.
2. Implement after #1222 S4 merges; one pull request.
3. Link the plan from #150.
