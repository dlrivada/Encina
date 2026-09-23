# Implementation Plan: `Encina.Compliance.Retention` — Minimum-Retention Floor, Event-Anchored Start, Keyed Policies and Immutable Records

> **Issue**: [#1187](https://github.com/dlrivada/Encina/issues/1187) (SPEC-002 tracking id **P-01**, priority P0)
> **Type**: Feature
> **Complexity**: High (10 phases, Marten-only per ADR-019, ~45 files touched)
> **Estimated Scope**: ~1,800-2,400 lines of production code + ~2,200-2,800 lines of tests
> **Milestone**: v0.17.0 — Compliance Lifecycle
> **Parent EPIC**: [#1186](https://github.com/dlrivada/Encina/issues/1186) — EU regulatory readiness (SPEC-002)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-001, AC-001, scenario S14; cross-cutting REQ-061 (AC-043) and REQ-062 (AC-044)
> **Depends on**: PR [#1185](https://github.com/dlrivada/Encina/pull/1185) (ADR-031, `IRetentionDataEraser`, closes #1160 and #1161); [#1158](https://github.com/dlrivada/Encina/issues/1158) (cycle lock); [#1249](https://github.com/dlrivada/Encina/issues/1249) (tenant-scoped legal holds)
> **Consumers**: [#1188](https://github.com/dlrivada/Encina/issues/1188) (P-02, DSR erasure arbitration), [#1189](https://github.com/dlrivada/Encina/issues/1189) (P-03, blocked data state), [#1191](https://github.com/dlrivada/Encina/issues/1191) (P-04, subject key store), [#1227](https://github.com/dlrivada/Encina/issues/1227) (P-25, reference scenario S14)

---

## Summary

Today a retention policy can only say "delete N after tracking starts": `DefaultRetentionRecordService.TrackEntityAsync` computes `expiresAtUtc = now + retentionPeriod` (`src/Encina.Compliance.Retention/Services/DefaultRetentionRecordService.cs:95`), `RetentionPolicyType.EventBased` is never read, `RetentionPolicyBuilder.RetainForYears` multiplies by 365, `AutoDelete` is stored but not consulted by the sweep, and policies are looked up by data category alone with no tenant, jurisdiction or document type (`DefaultRetentionPolicyService.GetPolicyByCategoryAsync` returns the first active match).

This plan turns the retention policy into the shape that SPEC-002 REQ-001 requires:

1. **Floor and maximum.** A policy carries a `MinimumRetention` (a floor: erasure is refused before it) and a `MaximumRetention` (erasure or blocking is due after it). Either can be absent.
2. **Anchored start.** The period starts at an anchor the application raises (episode discharge, end of fiscal year, death). A later anchor re-anchors the period; the clock can be suspended while an episode is open. A record whose policy needs an anchor and has none is "floor not started" and the sweep never erases it.
3. **Calendar arithmetic.** Periods are calendar periods (years, months, days) added with calendar rules (a 29 February anchor plus one year gives 28 February), reading time only from `TimeProvider`.
4. **Keyed policies.** A policy is keyed by tenant, jurisdiction, document type and data category, and applies to its data category only (the design side of #1160; the enforcement side is ADR-031).
5. **Immutable-record class.** A policy can mark its records immutable (invoices, chained records): the retention record cannot be re-tracked or changed in place, and a correction is a new record linked to the original.
6. **Floor query for consumers.** A read API returns, per entity and category, the effective floor end, maximum, anchor state, hold state and immutability, so that DSR erasure (P-02), blocking (P-03) and crypto-shredding (P-04, #1144) can refuse with a grantable-after date.

**Standards**: GDPR Art. 5(1)(e) (storage limitation), Art. 5(2) (accountability), Art. 17(3)(b) and (e) (erasure exemptions); Ley 41/2002 art. 17.1 (at least 5 years from each discharge; longer regional periods); LGT and Código de Comercio art. 30 (tax and commercial retention, [K]/[S] per SPEC-002 §12 question 6).

**Affected packages**: `Encina.Compliance.Retention` (all changes); `Encina.DomainModeling` (only if open decision OD-5 chooses the data-level immutable guard); `Encina.Compliance.DataSubjectRights` is **not** changed here (P-02 consumes the new query; after #1185 Retention no longer references it).

**Provider category**: none of the 10 database providers. `Encina.Compliance.Retention` is event-sourced on Marten (PostgreSQL) and SPEC-002 DEC-008 (a) keeps the nine event-sourced compliance modules Marten-only in 1.0, as [ADR-019](../architecture/adr/019-compliance-event-sourcing-marten.md) states. Integration tests run against Marten on PostgreSQL through Testcontainers.

---

## Interaction with P-03 (Blocked Data State, #1189)

P-01 and P-03 meet at two points. Both plans state the same contract ([blocked-data-state-implementation-plan-1189.md](blocked-data-state-implementation-plan-1189.md), section "Interaction with P-01").

| Topic | Contract |
|-------|----------|
| **Who decides what** | Retention decides **when** (floor, maximum, anchor, hold). Blocking decides **how data is kept once it may no longer be processed** (hidden state, audited disclosure, secure copy, destruction). |
| **Expiry disposition** | P-01 replaces the unused `AutoDelete` flag with `ExpiryDisposition { Erase, Block, NotifyOnly }`. `Erase` calls the ADR-031 `IRetentionDataEraser`. `Block` hands the expired, non-held target to P-03's `IBlockingService` instead. P-01 ships the enum member and a start-up validation that rejects `Block` while no `IBlockingService` is registered; P-03 wires the call and adds the `RetentionRecordBlocked` event and the `Blocked` status. |
| **A blocked record's retention clock** | Blocking neither stops nor resets the retention clock. The record keeps its anchor, floor end and maximum. P-03 computes the destruction date of a block as `max(FloorEndsAtUtc, BlockedAtUtc + LimitationPeriod)`, reading `FloorEndsAtUtc` from P-01's `IRetentionFloorQuery`. A legal hold suspends destruction of blocked data exactly as it suspends erasure. |
| **Erasure request under a floor** | When a DSR erasure (P-02) meets a category whose floor has not elapsed, P-02 refuses that category with the Art. 17(3) exemption and the grantable-after date `FloorEndsAtUtc` from P-01; if the tenant's jurisdiction is configured for blocking (LOPDGDD art. 32), the refused category is blocked through P-03 instead of staying processable. The retention record stays under P-01's control; when its maximum elapses the sweep destroys the blocked data through P-03. |
| **Anchor after blocking** | Whether a new anchor (a returning patient opens a new episode) on an entity and category that is blocked unblocks the data is an open decision (OD-2 here, OD-11 in P-03). Until decided, P-01 records the anchor and P-03 leaves the block in place. |
| **Order of delivery** | P-01 lands first. P-03 depends on `ExpiryDisposition.Block` and `IRetentionFloorQuery`. |

---

## Design Choices

<details>
<summary><strong>1. Period Model — a calendar period value type instead of <code>TimeSpan</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `TimeSpan`** | No change | Cannot express "5 years" (years have 365 or 366 days, months 28 to 31); `RetainForYears` already computes `years*365` and drifts by a day every leap year |
| **B) NodaTime `Period` and `LocalDate`** | Mature calendar arithmetic, time-zone aware | New third-party dependency in a compliance package for one operation; leaks NodaTime types into the public API |
| **C) Own `readonly record struct CalendarPeriod(int Years, int Months, int Days)`** | No dependency; `DateTimeOffset.AddYears/AddMonths/AddDays` already implement the calendar rules (29 Feb + 1 year = 28 Feb, 31 Jan + 1 month = 28/29 Feb); serialises as three integers in Marten events | Time-zone handling must be explicit (see OD-1) |

### Chosen Option: **C — `CalendarPeriod`**

### Rationale

- `CalendarPeriod.AddTo(DateTimeOffset anchorUtc, TimeZoneInfo zone)` converts the anchor to the zone's local date-time, adds years, then months, then days, and converts back to UTC. With `TimeZoneInfo.Utc` this is plain `DateTimeOffset` arithmetic.
- The type replaces every `TimeSpan RetentionPeriod` in events, aggregates, read models, the builder and `[RetentionPeriod]` (pre-1.0, no compatibility layer). `RetainForYears(5)` becomes `new CalendarPeriod(Years: 5)`.
- Validation: all components `>= 0`, at least one `> 0`; `CalendarPeriod.Zero` is not a valid policy period.
- The name `CalendarPeriod` avoids a clash with `RetentionPeriodAttribute` (the issue sketch used `RetentionPeriod`; see OD-6).

</details>

<details>
<summary><strong>2. Floor, Maximum and Expiry Disposition on the Policy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two policy kinds (floor policy, deletion policy) per category** | Each aggregate stays simple | Two policies must be resolved and kept consistent for one category; the "floor ≤ maximum" invariant crosses aggregates |
| **B) One policy with optional `MinimumRetention` and optional `MaximumRetention`, plus `ExpiryDisposition`** | One resolution; the invariant is local to the aggregate; matches the SPEC wording | More fields on one event |
| **C) A list of rules (period + action) per policy** | Flexible (e.g. "anonymise after 2 years, erase after 5") | Over-general for the requirement; harder to explain to a compliance officer |

### Chosen Option: **B — one policy, optional floor and maximum, explicit disposition**

### Rationale

- `MinimumRetention` absent means "no floor"; `MaximumRetention` absent means "no automatic action" (the data is kept until an erasure request, which the floor then arbitrates).
- Invariant, enforced in `RetentionPolicyAggregate.Create/Update`: at least one of the two is present; when both are, `Maximum.AddTo(x) >= Minimum.AddTo(x)` for the policy's zone (checked against a fixed reference instant and against 29 February of a leap year).
- `ExpiryDisposition` replaces `bool AutoDelete`, which the sweep never read:
  - `Erase`: the sweep calls `IRetentionDataEraser` (ADR-031) after the maximum.
  - `Block`: the sweep hands the target to P-03's `IBlockingService` (wired by P-03; rejected at start-up until then).
  - `NotifyOnly`: the sweep marks the record expired and raises `DataExpiringNotification`/`RetentionRecordExpired`, erasing nothing (today's `AutoDelete = false` intent).
- The sweep never acts before the floor: the effective due date is `max(FloorEndsAtUtc, MaximumEndsAtUtc)`, so a misconfigured maximum shorter than the floor (impossible after validation, possible in replayed old streams) still cannot erase early.

</details>

<details>
<summary><strong>3. Anchor Model — anchor events on the retention record, fanned out by an anchor service</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Application computes the start and passes a date to `TrackEntityAsync`** | No new API | Moves calendar and re-anchoring logic into every application and loses the audit trail of anchors (rejected in the issue, alternative 2) |
| **B) Anchor events on `RetentionRecordAggregate`; `IRetentionAnchorService` applies an anchor to every non-terminal record of (tenant, entity, category)** | The anchor history is in the record's own stream (Art. 5(2) evidence); works with the ADR-031 sibling rule (several records per entity and category) | An anchor for an entity with no record yet has nowhere to go |
| **C) A separate `RetentionAnchorAggregate` stream per (tenant, entity, category)** | Anchors can precede tracking; one place per subject/category | A second stream to keep in step with records; the sweep must join two read models |

### Chosen Option: **B — anchor events on the record aggregate**

### Rationale

- New events on the record stream: `RetentionRecordAnchored(RecordId, AnchorKind, AnchoredAtUtc, FloorEndsAtUtc?, MaximumEndsAtUtc?, OccurredAtUtc)` and `RetentionClockSuspended(RecordId, Reason, OccurredAtUtc)`.
- `RetentionRecordTracked` no longer carries a computed `ExpiresAtUtc` for anchored policies: it carries `RequiresAnchor` and the policy snapshot (floor, maximum, disposition, immutability, zone id). Time-based policies (anchor = tracking time) anchor at tracking, so the existing behaviour is one case of the new model.
- Re-anchoring is **monotonic**: an anchor earlier than the record's current anchor is refused with `retention.anchor_not_monotonic` (a floor never moves earlier). An anchor on a record under legal hold is recorded; the hold still wins.
- "A new episode opens" is modelled as `SuspendClockAsync` (status `AwaitingAnchor`, floor not running) followed by the discharge anchor. Whether opening an episode must suspend the clock, or whether the clock keeps running from the previous discharge until the next anchor, is OD-2.
- An anchor for an entity with no active record returns `retention.no_record_to_anchor` (a `Left`), so an ordering bug in the application is visible instead of silently lost. OD-8 covers subject-wide anchors (death).

</details>

<details>
<summary><strong>4. Policy Key and Resolution — tenant, jurisdiction, document type, data category</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep category-only lookup, add jurisdiction as metadata** | Minimal change | Two policies for one category (national clinical record vs regional) cannot coexist; AC-001 fails |
| **B) Exact key `(TenantId, Jurisdiction, DocumentType, DataCategory)`, with an explicit fallback chain** | Deterministic, testable, explainable ("this record follows policy X because …") | The fallback chain must be specified and validated |
| **C) Rule engine with priorities and wildcards** | Flexible | Ambiguity is only discovered at run time; hard to audit |

### Chosen Option: **B — exact key with a fixed fallback chain**

### Rationale

- `RetentionPolicyKey(string Jurisdiction, string DocumentType, string DataCategory)`; `Jurisdiction` and `DocumentType` accept the reserved value `RetentionPolicyKey.Any` (`"*"`).
- Resolution for a tracking call in tenant T: `(T, J, D, C)` → `(T, J, *, C)` → `(T, *, D, C)` → `(T, *, *, C)` → the same four keys at deployment level (tenant `null`) → `RetentionOptions.DefaultPolicy` if configured → `Left(retention.no_policy_for_category)`. The first match wins; two active policies with the same full key are rejected at creation (`retention.policy_already_exists`), which the current `CreatePolicyAsync` does not check.
- Where `J` and `D` come from at tracking time: `[RetentionPeriod(DataCategory = ..., DocumentType = ...)]` on the response gives `D`; `J` comes from `IRetentionJurisdictionResolver` (default: `RetentionOptions.DefaultJurisdiction`, overridable per tenant). The source of `J` is OD-3; the deployment/tenant layering is OD-4.
- A policy applies to its data category only: the record carries its own category and the sweep erases through `IRetentionDataEraser` scoped to that category (ADR-031). P-01 adds the regression test that expiry of one category leaves the others untouched (AC-001, #1160).

</details>

<details>
<summary><strong>5. Immutable-Record Class</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Retention-level only: the policy flag freezes the record; `LinkCorrectionAsync` tracks a corrective record linked to the original** | Stays inside the Marten-only package; answers "which record corrects which" with evidence | Does not stop the application from updating the row itself through a repository |
| **B) A + a data-level guard: `IImmutableRecord` marker in `Encina.DomainModeling` and a provider-neutral `ImmutableRecordGuardRepository<TEntity,TId>` decorator over `IFunctionalRepository` that returns `Left` for `UpdateAsync`, `UpdateRangeAsync`, `UpdateImmutableAsync` and `DeleteAsync` | Enforced on all 10 providers without provider-specific code (one decorator) | Touches `Encina.DomainModeling`; deletion at the end of the period must bypass the guard (the eraser uses its own path) |
| **C) Database triggers per provider** | Strongest guarantee | Provider-specific DDL on 10 providers for a Marten-only module; out of proportion |

### Chosen Option: **A now, B proposed as OD-5**

### Rationale

- A: `RetentionPolicyCreated.Immutable` is snapshot onto each record. An immutable record rejects `Track` for the same entity and category a second time (`retention.immutable_record`), rejects policy re-application, and accepts `LinkCorrectionAsync(originalRecordId, correctiveEntityId)`, which tracks the corrective entity under the same policy and anchor and raises `RetentionRecordCorrected(RecordId, CorrectiveRecordId, CorrectiveEntityId, OccurredAtUtc)` on the original.
- The acceptance criterion "an immutable-record class rejects updates and accepts a corrective record" is met at the retention level by A; whether Encina should also guard the application's own rows (B) is a scope decision for the maintainer (OD-5). REQ-006 (P-04, #1191) separately requires that immutable records never share a crypto-shredding key with erasable data; `IRetentionFloorQuery` exposes `IsImmutable` for that check.

</details>

<details>
<summary><strong>6. Floor Query for Consumers — <code>IRetentionFloorQuery</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Consumers read `RetentionRecordReadModel` directly** | No new API | Every consumer re-implements "max over sibling records, holds, anchors"; P-02, P-03 and P-04 would disagree |
| **B) `IRetentionFloorQuery.GetEffectiveRetentionAsync(tenantId, entityId, dataCategory)` returning `EffectiveRetention`** | One definition of "erasable now" and of the grantable-after date | One more interface |
| **C) A guard method `EnsureErasableAsync` returning `Left` with the reason** | Convenient for simple callers | Loses the dates P-02 must put in its reasoned refusal |

### Chosen Option: **B, plus a thin C helper built on it**

### Rationale

- `EffectiveRetention(string EntityId, string DataCategory, EffectiveRetentionState State, DateTimeOffset? FloorEndsAtUtc, DateTimeOffset? MaximumEndsAtUtc, bool UnderLegalHold, bool IsImmutable, Guid? PolicyId, string? LegalBasis)`; `EffectiveRetentionState { NotTracked, AwaitingAnchor, WithinFloor, FloorElapsed, Expired, Blocked, Deleted }` (`Blocked` is set by P-03).
- Across sibling records (ADR-031) the effective floor end is the **latest** floor end of the non-terminal siblings, so erasing the category never undercuts any record.
- `EnsureErasableAsync(...)` returns `Left(retention.floor_not_elapsed)` with `grantableAfterUtc` in the error details, or `Left(retention.legal_hold_active)`; it fails closed (`Left`) when the records or holds cannot be read.
- Consumers: P-02 (`DefaultDataErasureExecutor` arbitration), P-03 (destruction date), P-04 (refuse shredding under a floor), and the sweep itself.

</details>

<details>
<summary><strong>7. Policy Changes After Tracking — snapshot on the record</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Records follow the live policy** | A corrected period applies at once | A policy edit silently shortens the retention of data already tracked (INV-001 risk) and the record stream no longer explains its own dates |
| **B) Records snapshot the policy at tracking; an explicit `ReapplyPolicyAsync(policyId)` re-computes non-terminal records and records an event per record** | Every date is explainable from the record stream; lengthening or shortening is a deliberate, audited act | One more operation |

### Chosen Option: **B — snapshot plus explicit re-application**

### Rationale

- `RetentionPolicyReappliedToRecord(RecordId, PolicyId, PolicyVersion, FloorEndsAtUtc?, MaximumEndsAtUtc?, OccurredAtUtc)`. Re-application may lengthen a floor freely; shortening a floor below its current end requires `allowShortening: true` and logs a warning (the application's legal decision, SPEC-002 §2.1). Immutable records are never re-applied.
- This replaces today's implicit behaviour (records copy `RetentionPeriod` at tracking and never change), which is already a snapshot but without a way to correct it.

</details>

---

## Implementation Phases

### Phase 1: Core Model — `CalendarPeriod`, Keys, Dispositions, States

> **Goal**: The value types every later phase uses.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Compliance.Retention/Model/`

1. **`CalendarPeriod.cs`** — `public readonly record struct CalendarPeriod(int Years = 0, int Months = 0, int Days = 0)`
   - `public static CalendarPeriod Zero { get; }`, `public bool IsZero { get; }`
   - `public DateTimeOffset AddTo(DateTimeOffset instantUtc, TimeZoneInfo zone)` — local date-time in `zone`, `AddYears(Years).AddMonths(Months).AddDays(Days)`, back to UTC; ambiguous or invalid local times after a DST change resolve to the later valid instant (documented)
   - `public static CalendarPeriod FromYears(int)`, `FromMonths(int)`, `FromDays(int)`
   - `public override string ToString()` → ISO 8601 duration (`P5Y`, `P1Y6M`)
   - `public static bool TryParse(string, out CalendarPeriod)` (ISO 8601 subset `PnYnMnD`) for configuration binding
2. **`RetentionPolicyKey.cs`** — `public sealed record RetentionPolicyKey(string Jurisdiction, string DocumentType, string DataCategory)`; `public const string Any = "*"`; ordinal, case-sensitive comparison; guard clauses reject null/whitespace
3. **`ExpiryDisposition.cs`** — `public enum ExpiryDisposition { Erase = 0, Block = 1, NotifyOnly = 2 }` (replaces `bool AutoDelete`)
4. **`RetentionAnchorKinds.cs`** — `public static class RetentionAnchorKinds` with well-known string constants `TrackingStart`, `EpisodeDischarge`, `FiscalYearEnd`, `SubjectDeath`; anchor kinds remain open strings
5. **`EffectiveRetention.cs`**, **`EffectiveRetentionState.cs`** — as in design choice 6
6. **`RetentionStatus.cs`** (modify) — add `AwaitingAnchor = 4` (P-03 later adds `Blocked = 5`); update XML docs of the lifecycle
7. **`RetentionPolicyType.cs`** — **delete**: the policy type is now implied by `RequiresAnchor` and the anchor kind; `ConsentBased` had no implementation (pre-1.0, no `[Obsolete]`)
8. **`PublicAPI.Unshipped.txt`** — add and remove symbols

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1187 (SPEC-002 P-01) in src/Encina.Compliance.Retention.

CONTEXT:
- .NET 10 / C# 14, nullable enabled, Railway Oriented Programming (Either<EncinaError, T>).
- Pre-1.0: breaking changes are expected; no [Obsolete], no compatibility layers.
- The module is event-sourced on Marten (ADR-019). Value types appear in persisted events, so keep them
  simple, immutable and serialisable by System.Text.Json (Marten's serializer).

TASK:
Create CalendarPeriod, RetentionPolicyKey, ExpiryDisposition, RetentionAnchorKinds, EffectiveRetention and
EffectiveRetentionState in Model/. Add RetentionStatus.AwaitingAnchor. Delete RetentionPolicyType and fix
every reference (the hosted services hard-code TimeBased today).

KEY RULES:
- CalendarPeriod.AddTo(instantUtc, zone): convert to local time in zone, add Years, then Months, then Days,
  convert back to UTC. 2024-02-29 + 1 year = 2025-02-28. Document DST resolution.
- CalendarPeriod never reads the clock; callers pass instants obtained from TimeProvider.
- ToString/TryParse use the ISO 8601 PnYnMnD subset.
- XML docs on every public member; cite GDPR Art. 5(1)(e) and Ley 41/2002 art. 17.1 where relevant.
- Update PublicAPI.Unshipped.txt (RS0016/RS0017 must be clean).

REFERENCE FILES:
- src/Encina.Compliance.Retention/Model/RetentionStatus.cs
- src/Encina.Compliance.Retention/Model/RetentionPolicyType.cs (to delete)
- src/Encina.Compliance.Retention/RetentionAutoRegistrationHostedService.cs (line ~202)
- src/Encina.Compliance.Retention/RetentionFluentPolicyHostedService.cs (line ~71)
```

</details>

---

### Phase 2: Policy Aggregate, Events and Read Model

> **Goal**: Policies carry floor, maximum, disposition, key, anchor kind, zone and immutability.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Events/RetentionPolicyEvents.cs`** (modify)
   - `RetentionPolicyCreated(Guid PolicyId, RetentionPolicyKey Key, CalendarPeriod? MinimumRetention, CalendarPeriod? MaximumRetention, ExpiryDisposition Disposition, string? AnchorKind, string TimeZoneId, bool Immutable, string? Reason, string? LegalBasis, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
   - `RetentionPolicyUpdated(Guid PolicyId, CalendarPeriod? MinimumRetention, CalendarPeriod? MaximumRetention, ExpiryDisposition Disposition, string? Reason, string? LegalBasis, DateTimeOffset OccurredAtUtc)` (key, anchor kind and immutability cannot change: deactivate and create instead)
   - `RetentionPolicyDeactivated` unchanged
2. **`Aggregates/RetentionPolicyAggregate.cs`** (modify)
   - Properties: `Key`, `MinimumRetention`, `MaximumRetention`, `Disposition`, `AnchorKind`, `RequiresAnchor` (`AnchorKind is not null and not TrackingStart`), `TimeZoneId`, `Immutable`, `Version` (event count, used by re-application)
   - `static Create(Guid id, RetentionPolicyKey key, CalendarPeriod? minimum, CalendarPeriod? maximum, ExpiryDisposition disposition, string? anchorKind, string timeZoneId, bool immutable, string? reason, string? legalBasis, DateTimeOffset occurredAtUtc, string? tenantId, string? moduleId)` — invariants of design choice 2; `TimeZoneInfo.FindSystemTimeZoneById` must succeed
   - `Update(...)`, `Deactivate(...)`
3. **`ReadModels/RetentionPolicyReadModel.cs` and `RetentionPolicyProjection.cs`** (modify) — mirror the new fields; `Jurisdiction`, `DocumentType`, `DataCategory` as separate indexed properties for Marten LINQ
4. **`Abstractions/IRetentionPolicyService.cs`** (modify)
   - `CreatePolicyAsync(RetentionPolicyDefinition definition, string? tenantId = null, string? moduleId = null, CancellationToken ct = default)` → `Either<EncinaError, Guid>`; `RetentionPolicyDefinition` is a sealed record grouping the policy fields
   - `ResolvePolicyAsync(string? tenantId, RetentionPolicyKey key, CancellationToken ct = default)` → `Either<EncinaError, RetentionPolicyReadModel>` (the fallback chain of design choice 4); replaces `GetPolicyByCategoryAsync` and `GetRetentionPeriodAsync`
   - `GetActivePoliciesAsync(string? tenantId, CancellationToken ct = default)`
5. **`Services/DefaultRetentionPolicyService.cs`** (modify) — duplicate-key check on create; tenant-filtered queries; cache key `ret:policy:{tenant}:{jurisdiction}:{documentType}:{category}`
6. **`RetentionErrors.cs`** (modify) — new codes: `retention.invalid_period`, `retention.floor_exceeds_maximum`, `retention.unknown_time_zone`, `retention.policy_ambiguous`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1187 in src/Encina.Compliance.Retention.

CONTEXT:
- Phase 1 added CalendarPeriod, RetentionPolicyKey, ExpiryDisposition and removed RetentionPolicyType.
- Aggregates derive from Encina.DomainModeling.AggregateBase, raise events with RaiseEvent and apply them in
  a switch in Apply(object). Events are sealed records implementing INotification.
- Policies are resolved per tenant and key with the fallback chain:
  (T,J,D,C) -> (T,J,*,C) -> (T,*,D,C) -> (T,*,*,C) -> same four with tenant null -> DefaultPolicy -> Left.

TASK:
Reshape RetentionPolicyCreated/Updated, RetentionPolicyAggregate, RetentionPolicyReadModel/Projection,
IRetentionPolicyService and DefaultRetentionPolicyService as listed in the Phase 2 tasks.

KEY RULES:
- Invariants in the aggregate: at least one of minimum/maximum; maximum >= minimum when both are set
  (compare AddTo on a fixed instant and on 2024-02-29T00:00Z in the policy zone); zone id resolvable.
- Reject a second active policy with the same (tenant, key).
- Every query filters by tenant. With tenancy off the tenant is null and the deployment-level policies apply.
- Services catch non-cancellation exceptions and return RetentionErrors.ServiceError (existing pattern).
- No DateTime.UtcNow / DateTimeOffset.UtcNow anywhere; time comes from TimeProvider.

REFERENCE FILES:
- src/Encina.Compliance.Retention/Aggregates/RetentionPolicyAggregate.cs
- src/Encina.Compliance.Retention/Services/DefaultRetentionPolicyService.cs
- src/Encina.Compliance.Retention/ReadModels/RetentionPolicyProjection.cs
- src/Encina.Compliance.Consent/ (tenant-aware Marten read-model queries, for the query style)
```

</details>

---

### Phase 3: Record Aggregate — Anchors, Suspension, Floor and Maximum, Corrections

> **Goal**: The record stream explains every date; expiry is computed from the anchor.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Events/RetentionRecordEvents.cs`** (modify / add)
   - `RetentionRecordTracked(Guid RecordId, string EntityId, string DataCategory, Guid PolicyId, int PolicyVersion, CalendarPeriod? MinimumRetention, CalendarPeriod? MaximumRetention, ExpiryDisposition Disposition, string? AnchorKind, string TimeZoneId, bool Immutable, DateTimeOffset? AnchoredAtUtc, DateTimeOffset? FloorEndsAtUtc, DateTimeOffset? MaximumEndsAtUtc, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)` — for `TrackingStart` policies the anchor is `OccurredAtUtc`
   - **New** `RetentionRecordAnchored(Guid RecordId, string AnchorKind, DateTimeOffset AnchoredAtUtc, DateTimeOffset? FloorEndsAtUtc, DateTimeOffset? MaximumEndsAtUtc, DateTimeOffset OccurredAtUtc)`
   - **New** `RetentionClockSuspended(Guid RecordId, string Reason, DateTimeOffset OccurredAtUtc)`
   - **New** `RetentionPolicyReappliedToRecord(...)` (design choice 7)
   - **New** `RetentionRecordCorrected(Guid RecordId, Guid CorrectiveRecordId, string CorrectiveEntityId, DateTimeOffset OccurredAtUtc)`
   - `RetentionRecordExpired`, `Held`, `Released`, `DataDeleted`, `DataAnonymized` unchanged in shape; `RetentionRecordReleased` decides `Active`/`Expired`/`AwaitingAnchor` from the stored dates, deterministically on replay
2. **`Aggregates/RetentionRecordAggregate.cs`** (modify)
   - State: `AnchorKind`, `AnchoredAtUtc?`, `FloorEndsAtUtc?`, `MaximumEndsAtUtc?`, `Disposition`, `Immutable`, `TimeZoneId`, `CorrectedByRecordId?`; `ExpiresAtUtc` becomes a computed `DueAtUtc => Max(FloorEndsAtUtc, MaximumEndsAtUtc)` (null while awaiting an anchor or when no maximum)
   - `Anchor(string kind, DateTimeOffset anchoredAtUtc, DateTimeOffset occurredAtUtc)` — refuses non-monotonic anchors and terminal states; computes the ends with `CalendarPeriod.AddTo`
   - `SuspendClock(string reason, DateTimeOffset occurredAtUtc)` — status `AwaitingAnchor`; refused for immutable records
   - `ReapplyPolicy(...)`, `LinkCorrection(Guid correctiveRecordId, string correctiveEntityId, DateTimeOffset occurredAtUtc)`
   - `MarkExpired(DateTimeOffset nowUtc)` refuses when `DueAtUtc` is null or in the future (fail closed against an early sweep)
3. **`ReadModels/RetentionRecordReadModel.cs` and `RetentionRecordProjection.cs`** (modify) — new fields; `IsDue(DateTimeOffset nowUtc)` replaces `IsExpired`
4. **`Abstractions/IRetentionRecordService.cs`** (modify)
   - `TrackEntityAsync(string entityId, RetentionPolicyKey key, string? tenantId = null, string? moduleId = null, CancellationToken ct = default)` — resolves the policy (Phase 2), snapshots it, anchors at tracking time for `TrackingStart` policies
   - `GetRecordsByEntityAsync(string? tenantId, string entityId, string? dataCategory = null, CancellationToken ct = default)`
   - `GetDueRecordsAsync(CancellationToken ct = default)` replaces `GetExpiredRecordsAsync` (records with `DueAtUtc <= now` in `Active`, plus `Expired`); never returns `AwaitingAnchor`
   - `LinkCorrectionAsync(Guid originalRecordId, string correctiveEntityId, CancellationToken ct = default)` → `Either<EncinaError, Guid>`
   - `ReapplyPolicyAsync(Guid policyId, bool allowShortening = false, CancellationToken ct = default)` → `Either<EncinaError, int>`
5. **`Services/DefaultRetentionRecordService.cs`** (modify) — no more `now + retentionPeriod`
6. **`RetentionErrors.cs`** — `retention.anchor_not_monotonic`, `retention.no_record_to_anchor`, `retention.record_awaiting_anchor`, `retention.immutable_record`, `retention.floor_not_elapsed` (details: `grantableAfterUtc`), `retention.legal_hold_active`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1187 in src/Encina.Compliance.Retention.

CONTEXT:
- Policies (Phase 2) carry optional floor/maximum CalendarPeriods, a disposition, an anchor kind, a time zone
  id and an Immutable flag. Records snapshot the policy at tracking.
- ADR-031 (merged with PR #1185) erases through IRetentionDataEraser per record and defers erasure while a
  sibling record (same tenant, module, entity, category) still retains the data. Keep that logic intact.

TASK:
Add anchoring, clock suspension, policy re-application and correction links to RetentionRecordAggregate and
its events, read model and service, as listed in the Phase 3 tasks.

KEY RULES:
- DueAtUtc = max(FloorEndsAtUtc, MaximumEndsAtUtc); null when awaiting an anchor or when there is no maximum.
- Anchors are monotonic: an earlier anchor than the current one returns Left(retention.anchor_not_monotonic).
- Apply() must be deterministic on replay: compute nothing from the clock inside Apply; events carry every
  computed instant.
- MarkExpired refuses when DueAtUtc is null or later than the instant passed in (fail closed).
- Immutable records: no second Track for the same (tenant, entity, category), no re-application, no
  suspension; LinkCorrection tracks the corrective entity under the same policy and anchor.
- Tenant filter on every query; tenant and module copied onto every new record.

REFERENCE FILES:
- src/Encina.Compliance.Retention/Aggregates/RetentionRecordAggregate.cs
- src/Encina.Compliance.Retention/Services/DefaultRetentionRecordService.cs
- src/Encina.Compliance.Retention/ReadModels/RetentionRecordProjection.cs
- ADR-031 (docs/architecture/adr/031-retention-erasure-port.md once PR #1185 merges)
```

</details>

---

### Phase 4: Anchor Service and Floor Query

> **Goal**: Public entry points for applications (anchor, suspend) and for consumers (floor query).

<details>
<summary><strong>Tasks</strong></summary>

1. **`Abstractions/IRetentionAnchorService.cs`** (new)
   - `ValueTask<Either<EncinaError, int>> AnchorAsync(string entityId, string dataCategory, string anchorKind, DateTimeOffset occurredAtUtc, CancellationToken ct = default)` — applies to every non-terminal record of (current tenant, entity, category); returns the number of records anchored
   - `ValueTask<Either<EncinaError, int>> SuspendClockAsync(string entityId, string dataCategory, string reason, CancellationToken ct = default)`
   - The tenant comes from `IRequestContext.TenantId` (REQ-015); an explicit `tenantId` overload exists for background jobs that restore a persisted context
2. **`Services/DefaultRetentionAnchorService.cs`** (new) — constructor `(IRetentionRecordService, IAggregateRepository<RetentionRecordAggregate>, IRequestContext, TimeProvider, ILogger<DefaultRetentionAnchorService>)`; loads each record, calls `Anchor`, saves; partial failure returns `Left` listing the record ids that failed (the successful ones stay anchored, each is its own stream)
3. **`Abstractions/IRetentionFloorQuery.cs`** (new)
   - `ValueTask<Either<EncinaError, EffectiveRetention>> GetEffectiveRetentionAsync(string? tenantId, string entityId, string dataCategory, CancellationToken ct = default)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<EffectiveRetention>>> GetEffectiveRetentionForEntityAsync(string? tenantId, string entityId, CancellationToken ct = default)` (one entry per category; P-02 builds its per-category refusal from it)
   - `ValueTask<Either<EncinaError, Unit>> EnsureErasableAsync(string? tenantId, string entityId, string dataCategory, CancellationToken ct = default)`
4. **`Services/DefaultRetentionFloorQuery.cs`** (new) — constructor `(IRetentionRecordService, ILegalHoldService, TimeProvider, ILogger<DefaultRetentionFloorQuery>)`; latest floor end across non-terminal siblings; fails closed on read errors
5. **`RetentionValidationPipelineBehavior.cs`** (modify) — reads `DataCategory` and `DocumentType` from `[RetentionPeriod]`, resolves the jurisdiction through `IRetentionJurisdictionResolver`, takes the tenant and module from `IRequestContext` (as ADR-031 requires), calls the new `TrackEntityAsync`; the `PolicyId = Guid.Empty` shortcut disappears
6. **`Abstractions/IRetentionJurisdictionResolver.cs`** + **`Services/OptionsRetentionJurisdictionResolver.cs`** (new) — `ValueTask<string> ResolveAsync(string? tenantId, CancellationToken ct)`; default reads `RetentionOptions.DefaultJurisdiction` and `RetentionOptions.TenantJurisdictions`
7. **`Attributes/RetentionPeriodAttribute.cs`** (modify) — `Years`, `Months`, `Days` (floor-less maximum for simple cases), `MinimumYears/Months/Days`, `DocumentType`, `AnchorKind`, `Disposition`, `Immutable`; the computed `TimeSpan RetentionPeriod` property is removed

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1187 in src/Encina.Compliance.Retention.

CONTEXT:
- Records can be anchored and suspended (Phase 3). Consumers (DSR erasure #1188, blocking #1189, subject key
  store #1191) need one definition of "erasable now" and of the grantable-after date.
- IRequestContext (src/Encina/Core/RequestContext.cs) carries TenantId and ModuleId.

TASK:
Create IRetentionAnchorService/DefaultRetentionAnchorService, IRetentionFloorQuery/DefaultRetentionFloorQuery,
IRetentionJurisdictionResolver/OptionsRetentionJurisdictionResolver; update RetentionValidationPipelineBehavior
and [RetentionPeriod] as listed in the Phase 4 tasks.

KEY RULES:
- EnsureErasableAsync returns Left(retention.floor_not_elapsed) with detail grantableAfterUtc, or
  Left(retention.legal_hold_active); any read error returns Left (fail closed, SPEC-002 INV-005).
- The effective floor end across sibling records is the latest one.
- The pipeline behavior keeps its static per-generic-type attribute cache and its Block/Warn/Disabled modes.
- Guard clauses (ArgumentNullException / ArgumentException) on every public method.

REFERENCE FILES:
- src/Encina.Compliance.Retention/RetentionValidationPipelineBehavior.cs
- src/Encina.Compliance.Retention/Services/DefaultLegalHoldService.cs
- src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs (attribute cache pattern)
```

</details>

---

### Phase 5: Enforcement Sweep — Floor-Aware, Disposition-Aware

> **Goal**: The sweep acts only on due records and follows the disposition.

<details>
<summary><strong>Tasks</strong></summary>

1. **`RetentionEnforcementService.cs`** (modify, on top of PR #1185)
   - Select with `GetDueRecordsAsync`; `AwaitingAnchor` records are never selected
   - Before calling the eraser, call `IRetentionFloorQuery.EnsureErasableAsync` for (tenant, entity, category); a `Left` defers the record and increments `retention.erasure.refused_floor.total`
   - `ExpiryDisposition.Erase` → `IRetentionDataEraser` (ADR-031, unchanged); `NotifyOnly` → `MarkExpired` + `DataExpiringNotification`, no erasure; `Block` → not reachable in P-01 (start-up validation), wired by P-03
   - Keep the ADR-031 sibling deferral and the #1158 cycle lock
2. **`RetentionOptions.cs`** (modify)
   - Remove `DefaultRetentionPeriod` (`TimeSpan?`); add `RetentionPolicyDefinition? DefaultPolicy`
   - Add `string? DefaultJurisdiction`, `IDictionary<string, string> TenantJurisdictions`, `string DefaultTimeZoneId = "UTC"`
   - `AddPolicy(RetentionPolicyKey key, Action<RetentionPolicyBuilder> configure)`; builder methods `WithMinimum(CalendarPeriod)`, `WithMaximum(CalendarPeriod)`, `AnchoredTo(string anchorKind)`, `WithDisposition(ExpiryDisposition)`, `Immutable()`, `InTimeZone(string)`, `WithReason`, `WithLegalBasis`; `RetainForDays/Years` and `RetainFor(TimeSpan)` are removed
3. **`RetentionOptionsValidator.cs`** (modify) — period invariants, resolvable zones, no duplicate keys, `Block` requires a registered `IBlockingService` (checked by a start-up validator that has the service provider, `RetentionStartupValidator : IHostedService` or `IStartupFilter`-equivalent in the hosting model already used by the auto-registration hosted services)
4. **`RetentionFluentPolicyHostedService.cs`, `RetentionAutoRegistrationHostedService.cs`** (modify) — create keyed policies; deployment-level policies have tenant `null`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1187 in src/Encina.Compliance.Retention.

CONTEXT:
- PR #1185 (ADR-031) already changed RetentionEnforcementService to erase through IRetentionDataEraser with a
  sibling deferral; #1158 adds a distributed cycle lock. Build on that code; do not reintroduce
  IDataErasureExecutor.
- Records expose DueAtUtc (Phase 3); IRetentionFloorQuery (Phase 4) defines "erasable now".

TASK:
Make the sweep floor-aware and disposition-aware; reshape RetentionOptions, the builder, the validator and the
two policy hosted services as listed in the Phase 5 tasks.

KEY RULES:
- Never erase before the floor: call EnsureErasableAsync right before IRetentionDataEraser; Left defers.
- NotifyOnly never erases. Block is rejected at start-up while no IBlockingService is registered.
- Options: no TimeSpan periods remain; CalendarPeriod everywhere; ISO 8601 strings bind from configuration.
- Metrics and logs per Phase 8; no subject identifiers in tags or messages.

REFERENCE FILES:
- src/Encina.Compliance.Retention/RetentionEnforcementService.cs (as merged by PR #1185)
- src/Encina.Compliance.Retention/RetentionOptions.cs, RetentionOptionsValidator.cs
- src/Encina.Compliance.Retention/RetentionFluentPolicyHostedService.cs
```

</details>

---

### Phase 6: Configuration, DI and Marten Registration

> **Goal**: Register the new services with the existing satellite conventions.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ServiceCollectionExtensions.cs`** (modify) — `TryAddScoped<IRetentionAnchorService, DefaultRetentionAnchorService>()`, `TryAddScoped<IRetentionFloorQuery, DefaultRetentionFloorQuery>()`, `TryAddSingleton<IRetentionJurisdictionResolver, OptionsRetentionJurisdictionResolver>()`, the start-up validator
2. **`RetentionMartenExtensions.cs`** (modify) — Marten indexes on the read models: `(TenantId, EntityId, DataCategory)` for records, `(TenantId, Jurisdiction, DocumentType, DataCategory, IsActive)` for policies
3. **`README.md`** of the package — configuration examples (5-year floor from discharge; 4-year tax floor from fiscal year end, immutable)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1187.

TASK:
Register IRetentionAnchorService, IRetentionFloorQuery, IRetentionJurisdictionResolver and the start-up
validator in AddEncinaRetention with TryAdd*; add Marten indexes for the new query shapes in
RetentionMartenExtensions.

KEY RULES:
- TryAdd* so that applications and satellites can replace any service.
- Keep AddEncinaRetention's options-instance pattern for conditional registrations.
- No IServiceProvider in projections (CLAUDE.md, event-sourced modules rule).

REFERENCE FILES:
- src/Encina.Compliance.Retention/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Retention/RetentionMartenExtensions.cs
```

</details>

---

### Phase 7: Cross-Cutting Integration

> **Goal**: Tenancy (REQ-061), audit trail and validation, per the matrix below.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy** — every new event and read-model row carries the tenant id; `ResolvePolicyAsync`, `GetRecordsByEntityAsync`, `GetDueRecordsAsync` (per tenant partition), `IRetentionFloorQuery` and the anchor service filter by tenant; with tenancy off the tenant is `null` and deployment-level policies apply (no configuration needed). Legal-hold tenancy is #1249; the floor query calls the tenant-aware hold lookup once #1249 lands and, until then, documents the gap.
2. **Audit trail** — the record stream is the evidence (ADR-019): anchors, suspensions, re-applications, corrections and refusals are events. Refusals by the sweep are not events (no state change); they are logged and metered, and P-02 records the refusal in the DSR stream.
3. **Validation** — `RetentionOptionsValidator` and the aggregate invariants; `RetentionPolicyKey` guard clauses.
4. **Module isolation** — `ModuleId` keeps flowing from `IRequestContext` onto records and ADR-031 targets; no new scoping.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 (cross-cutting integration) of issue #1187.

TASK:
Verify and complete tenant scoping of every query and write added in Phases 2-6; make the floor query use the
tenant-aware legal-hold lookup if #1249 has merged, otherwise leave a TODO-free XML remark describing the gap
and link #1249; confirm every state change is an event on the record or policy stream.

KEY RULES:
- SPEC-002 REQ-061 / AC-043: tenant A never reads, anchors or erases tenant B's records.
- With tenancy off (tenant null) nothing needs configuration.

REFERENCE FILES:
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-061, INV-008)
- src/Encina.Compliance.Retention/Services/*.cs
```

</details>

---

### Phase 8: Observability

> **Goal**: Traces, metrics and logs for anchors, refusals and resolution (REQ-062).

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/RetentionDiagnostics.cs`** (modify; `ActivitySource` and `Meter` stay `Encina.Compliance.Retention`)
   - Activities: `Retention.Anchor` (tags `encina.tenant_id`, `retention.data_category`, `retention.anchor_kind`, `retention.outcome`), `Retention.FloorQuery`, `Retention.PolicyResolution` (existing, add `retention.jurisdiction`, `retention.document_type`)
   - Counters: `retention.anchors.recorded.total` (tags: category, anchor kind, outcome `anchored|reanchored|refused`), `retention.clock.suspended.total`, `retention.erasure.refused_floor.total` (tag: category), `retention.records.corrected.total`, `retention.policies.reapplied.total`
   - **Remove** the `retention.entity_id` tag (`TagEntityId`) from every activity: an entity id can be a direct identifier of a data subject (REQ-062). Replace it with nothing; the record id (a random `Guid`) may stay.
2. **`Diagnostics/RetentionLogMessages.cs`** (modify) — new `[LoggerMessage]` entries in the free slots of `ComplianceRetention` (8500–8599), packed sequentially from 8525: `AnchorRecorded` (8525, Debug), `AnchorRefusedNotMonotonic` (8526, Warning), `NoRecordToAnchor` (8527, Warning), `ClockSuspended` (8528, Information), `ErasureRefusedFloor` (8529, Information), `PolicyResolved` (8531, Debug), `PolicyResolutionFailed` (8532, Warning), `PolicyReapplied` (8533, Information), `PolicyReapplyShortened` (8534, Warning), `RecordCorrected` (8535, Information), `FloorQueryFailed` (8536, Error), `BlockDispositionWithoutBlockingService` (8537, Critical). Entity ids are not logged; the record id is.
3. **Existing messages** — review the 75 messages for entity ids in templates (`RetentionRecordTrackedES` logs `EntityId`); replace with the record id.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 (observability) of issue #1187.

CONTEXT:
- ActivitySource and Meter are both "Encina.Compliance.Retention" (RetentionDiagnostics.cs).
- EventId range ComplianceRetention = (8500, 8599) in src/Encina/Diagnostics/EventIdRanges.cs. After PR #1185,
  check which slots it used (8591 is claimed); the free slots for this issue are 8525-8529 and 8531-8537.
- SPEC-002 REQ-062: telemetry carries no payloads and no direct identifiers of data subjects; the tenant id is
  an attribute (tag name "encina.tenant_id", as in DPIA and PrivacyByDesign).

TASK:
Add the activities, counters and [LoggerMessage] entries of the Phase 8 tasks; remove TagEntityId and entity
ids from existing log templates.

KEY RULES:
- [LoggerMessage] source generator only; EventIds packed sequentially, no gaps beyond the ones already used.
- The assembly is already mapped in tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs;
  keep that test green.

REFERENCE FILES:
- src/Encina.Compliance.Retention/Diagnostics/RetentionDiagnostics.cs
- src/Encina.Compliance.Retention/Diagnostics/RetentionLogMessages.cs
- src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs (encina.tenant_id tag)
```

</details>

---

### Phase 9: Testing

> **Goal**: Every test type of the matrix below; AC-001, AC-043 and AC-044 evidenced.

<details>
<summary><strong>Tasks</strong></summary>

#### 9a. Unit Tests (`tests/Encina.UnitTests/Compliance/Retention/`)

- `CalendarPeriodTests` — leap day (2024-02-29 + P1Y = 2025-02-28), month ends, ISO parse/format, zone conversion around the October and March DST changes in `Europe/Madrid`
- `RetentionPolicyAggregateTests` — invariants, duplicate keys, update rules
- `RetentionRecordAggregateTests` — anchor, monotonic refusal, suspension, re-application, correction, `MarkExpired` refusal before due, deterministic replay
- `DefaultRetentionPolicyServiceTests` — fallback chain, tenant filter (mocked `IReadModelRepository`)
- `DefaultRetentionAnchorServiceTests`, `DefaultRetentionFloorQueryTests` (latest sibling floor, hold, fail closed)
- `RetentionEnforcementServiceTests` — floor refusal, `NotifyOnly`, `AwaitingAnchor` never selected, one category erased only (#1160 regression)
- `RetentionValidationPipelineBehaviorTests` — tenant, module, document type and jurisdiction flow into tracking
- Telemetry: in-memory exporter test that `Retention.Anchor` and `retention.anchors.recorded.total` carry `encina.tenant_id` and no entity id; log-capture test that no message template contains an entity id

#### 9b. Guard Tests (`tests/Encina.GuardTests/Compliance/Retention/`)

- Constructors and public methods of the new services, `RetentionPolicyKey`, `CalendarPeriod.AddTo(zone: null)`, builder methods

#### 9c. Contract Tests (`tests/Encina.ContractTests/Compliance/Retention/`)

- `IRetentionFloorQueryContractTests` — same answers for the default implementation and a hand-written fake used by P-02 tests (the fake ships in `Encina.Testing.Fakes` if P-02 needs it)
- `RetentionPolicyResolutionContractTests` — the fallback chain as a contract (one table of cases)

#### 9d. Property Tests (`tests/Encina.PropertyTests/Compliance/Retention/`)

- `CalendarPeriodPropertyTests` — `AddTo` is monotonic in the instant and in each component; `AddTo(x) >= x`; round trip of `ToString`/`TryParse`
- `RetentionRecordAggregatePropertyTests` (extend) — the floor end never decreases under any sequence of anchors, suspensions and re-applications without `allowShortening`
- FsCheck through `Encina.Testing.FsCheck`

#### 9e. Integration Tests (`tests/Encina.IntegrationTests/Compliance/Retention/`)

- Marten on PostgreSQL through Testcontainers (shared collection fixture already used by `RetentionAggregateIntegrationTests`)
- `RetentionFloorIntegrationTests` — AC-001 end to end under `FakeTimeProvider`: 5-year floor from discharge refuses erasure at 4 years 364 days, allows it at 5 years; a new episode suspends and a later discharge re-anchors; a regional policy (longer than 5 years) and the national policy resolve per jurisdiction; clinical record and invoice resolve per document type; expiry of the invoice category leaves the clinical category untouched
- `RetentionTenancyIntegrationTests` — two tenants: policies, anchors, floor queries and the sweep never cross (AC-043)
- `RetentionImmutableRecordIntegrationTests` — second track refused, correction linked
- One test registers every projection with a real store (CLAUDE.md, event-sourced modules rule)

#### 9f. Load Tests

- `tests/Encina.LoadTests/Compliance/Retention/RetentionLoadTests.cs` exists; extend with concurrent anchors on the same entity and category (monotonicity under contention through Marten optimistic concurrency). No `.md` needed.

#### 9g. Benchmark Tests

- `tests/Encina.BenchmarkTests/Compliance/Retention/Retention.md` is stale (it says "Not Implemented" while `Encina.Benchmarks/Compliance/Retention/RetentionServiceBenchmarks.cs` exists). Update it to justify not benchmarking `CalendarPeriod` and policy resolution (not hot paths: one resolution per tracking call, cached) and to reference the existing benchmark file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 (testing) of issue #1187.

CONTEXT:
- Event-sourced module: unit tests mock IAggregateRepository and IReadModelRepository with NSubstitute;
  integration tests use Marten on PostgreSQL through Testcontainers and the existing collection fixture.
- Assertions: Shouldly through Encina.Testing.Shouldly. Property tests: Encina.Testing.FsCheck.
- Time: FakeTimeProvider only; never DateTime.UtcNow.

TASK:
Write the tests listed in 9a-9g. Cover AC-001 (floor, re-anchor, leap day, jurisdiction, document type,
category isolation, immutable class), AC-043 (two tenants) and AC-044 (in-memory exporter, EventId allocation,
no subject identifiers in telemetry).

KEY RULES:
- Tests execute real package code (no reflection-only tests); each flag in
  .github/coverage-manifest/Encina.Compliance.Retention.json must reach its target (unit 70, guard 20,
  property 15, contract 15). Add the new files to the manifest with their flags.
- Integration tests: [Collection] fixtures, ClearAllDataAsync in InitializeAsync; never dispose the fixture.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/Retention/
- tests/Encina.IntegrationTests/Compliance/Retention/RetentionAggregateIntegrationTests.cs
- tests/Encina.PropertyTests/Compliance/Retention/RetentionRecordAggregatePropertyTests.cs
```

</details>

---

### Phase 10: Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation** on every new or changed public API (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` for `AnchorAsync` and `AddPolicy`), citing GDPR Art. 5(1)(e) and Ley 41/2002 art. 17.1 as examples of what an application configures, never as defaults Encina imposes
2. **`changelog.d/1187-retention-floor.added.md`** and **`changelog.d/1187-retention-floor.changed.md`** (breaking: `TimeSpan` periods removed, `RetentionPolicyType` removed, `AutoDelete` replaced) — never edit `CHANGELOG.md`
3. **`src/Encina.Compliance.Retention/README.md`** — floor, anchors, keyed policies, immutable class, floor query
4. **[`docs/features/data-retention.md`](../features/data-retention.md)** — rewrite "Policy Types" and "Policy Resolution"; add "Anchors and episodes" and "Floors and erasure requests"
5. **ADR-032 (proposed)** `docs/architecture/adr/032-retention-floor-and-anchored-periods.md` — calendar period type, anchor model, key and fallback chain, snapshot and re-application; supersedes decision 1 of [retention-implementation-plan-406.md](retention-implementation-plan-406.md) where it defined `TimeSpan` periods
6. **`docs/INVENTORY.md`** — new files
7. **`PublicAPI.Unshipped.txt`** — complete
8. **`ROADMAP.md`** — no change (the milestone already holds the item)
9. **Build**: `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings
10. **Tests**: `dotnet test` → all pass; every coverage flag of `Encina.Compliance.Retention` reaches its manifest target

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are finalising issue #1187.

TASK:
Write the XML docs, the changelog fragments (changelog.d/1187-retention-floor.added.md and .changed.md, bullets
starting with "-"), the package README, docs/features/data-retention.md, ADR-032 and the INVENTORY entries; run
dotnet run .github/scripts/changelog-fragments.cs -- --check; build with zero warnings; run the tests.

KEY RULES:
- English only. Never name the reference application; say "the reference application (a small Spanish
  psychology practice)" if an example needs it.
- Documentation never types coverage percentages; cite with covref markers (SPEC-001).
- The 5-year floor and the tax periods are examples of application configuration, not Encina defaults.

REFERENCE FILES:
- changelog.d/README.md
- docs/features/data-retention.md
- docs/architecture/adr/019-compliance-event-sourcing-marten.md
```

</details>

---

## Research

### Standards and Legal Sources

| Source | Provision | Relevance |
|--------|-----------|-----------|
| GDPR | Art. 5(1)(e) | Storage limitation: keep no longer than necessary (the maximum) |
| GDPR | Art. 5(2) | Accountability: the record stream proves anchors and refusals |
| GDPR | Art. 17(3)(b), (e) | Legal obligation and legal claims exemptions that the floor and holds implement |
| Ley 41/2002 | Art. 17.1 | Clinical records at least 5 years from the discharge of each care episode (floor + anchor) [V] |
| Regional health laws | e.g. Catalonia Ley 21/2000 art. 12 | Longer periods per jurisdiction [S] |
| LGT / Código de Comercio | Art. 66 ff. / art. 30 | Tax (4 years) and commercial (6 years) retention; SPEC-002 §12 question 6 [K]/[S] |
| SPEC-002 | REQ-001, AC-001, S14, REQ-061, REQ-062 | The requirement this plan implements |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `RetentionPolicyAggregate`, `RetentionRecordAggregate` | `src/Encina.Compliance.Retention/Aggregates/` | Extended with floor, anchor and correction events |
| `IRetentionDataEraser`, sibling deferral | PR #1185 (ADR-031) | Erasure path the floor guards |
| `ILegalHoldService` | `src/Encina.Compliance.Retention/Abstractions/` | Hold state in `EffectiveRetention` |
| `IAggregateRepository<T>`, `IReadModelRepository<T>` | `src/Encina.Marten/` | Persistence and queries |
| `IRequestContext` | `src/Encina/Core/RequestContext.cs` | Tenant and module at tracking and anchoring |
| `ICacheProvider` | `src/Encina.Caching/` | Policy resolution cache (existing pattern) |
| `FakeTimeProvider` | `Microsoft.Extensions.TimeProvider.Testing` | Deterministic calendar tests |
| `Encina.Testing.FsCheck` | `src/Encina.Testing.FsCheck/` | Calendar and monotonicity properties |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.Retention` | 8500–8599 (`ComplianceRetention`, registered) | New messages 8525–8529 and 8531–8537, packed; re-check after PR #1185 merges. No new range needed. |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Model (Phase 1) | ~7 | 6 new, 1 deleted |
| Policy (Phase 2) | ~6 | events, aggregate, read model, projection, service, errors |
| Record (Phase 3) | ~6 | same set for records |
| Anchor and floor query (Phase 4) | ~8 | 3 interfaces, 3 services, behavior, attribute |
| Sweep and options (Phase 5-6) | ~7 | enforcement, options, validator, hosted services, DI, Marten |
| Observability (Phase 8) | ~2 | diagnostics, log messages |
| Tests (Phase 9) | ~18 | across 7 test types |
| Documentation (Phase 10) | ~7 | README, feature doc, ADR, changelog ×2, INVENTORY, PublicAPI |
| **Total** | **~60** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1187 (SPEC-002 P-01): minimum-retention floor, event-anchored start, keyed policies
and an immutable-record class in src/Encina.Compliance.Retention.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable, ROP with Either<EncinaError, T>; pre-1.0, breaking changes preferred over layers.
- Retention is event-sourced on Marten (ADR-019, SPEC-002 DEC-008): no 10-provider stores, no InMemory stores;
  unit tests mock IAggregateRepository; integration tests use Marten on PostgreSQL via Testcontainers.
- Prerequisite: PR #1185 (ADR-031) — IRetentionDataEraser, category-scoped erasure, sibling deferral.

IMPLEMENTATION OVERVIEW:
Phase 1: CalendarPeriod, RetentionPolicyKey, ExpiryDisposition, anchor kinds, EffectiveRetention; drop RetentionPolicyType
Phase 2: policy aggregate/events/read model with floor, maximum, disposition, key, zone, immutability; fallback chain
Phase 3: record aggregate with anchors, suspension, re-application, corrections; DueAtUtc = max(floor, maximum)
Phase 4: IRetentionAnchorService, IRetentionFloorQuery, IRetentionJurisdictionResolver; pipeline behavior update
Phase 5: floor-aware, disposition-aware sweep; options and builder in CalendarPeriod terms
Phase 6: DI and Marten indexes
Phase 7: tenancy (REQ-061), audit (events), validation
Phase 8: ActivitySource/Meter "Encina.Compliance.Retention"; EventIds 8525-8529, 8531-8537; no entity ids in telemetry
Phase 9: unit, guard, contract, property, integration (Marten), load (extend), benchmark (.md update)
Phase 10: XML docs, changelog.d fragments, README, docs/features/data-retention.md, ADR-032, INVENTORY, PublicAPI

KEY PATTERNS:
- Time only from TimeProvider; Apply() never reads the clock; events carry computed instants.
- Fail closed: any uncertainty about floors or holds means "do not erase".
- Every query and write is tenant-scoped; tenancy off = tenant null, zero configuration.
- Interaction with P-03 (#1189): ExpiryDisposition.Block and IRetentionFloorQuery are the contract; blocking
  never resets the retention clock; destruction date = max(FloorEndsAtUtc, BlockedAtUtc + LimitationPeriod).

REFERENCE FILES:
- src/Encina.Compliance.Retention/ (whole package)
- docs/plans/retention-es-migration-plan-783.md (Marten patterns of this module)
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-001, AC-001, S14, REQ-061, REQ-062)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Existing `ICacheProvider` use kept for policy resolution; cache key now includes tenant, jurisdiction, document type and category; invalidated on policy create/update/deactivate |
| 2 | OpenTelemetry | ✅ | `Retention.Anchor`, `Retention.FloorQuery` activities and five new counters (Phase 8); `encina.tenant_id` attribute; entity ids removed from tags |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 8525–8529, 8531–8537 inside the registered `ComplianceRetention` range (ADR-021) |
| 4 | Health Checks | ❌ | No new checkable dependency: Marten is covered by `MartenHealthCheck` and the existing `RetentionHealthCheck` (opt-in) already resolves the services |
| 5 | Validation | ✅ | Options validator, aggregate invariants and a start-up check (`Block` needs `IBlockingService`) |
| 6 | Resilience | ❌ | No call to an external system; Marten calls are local and failures fail closed |
| 7 | Distributed Locks | ❌ | Anchoring appends to one record stream per record (optimistic concurrency); the sweep's cycle lock is #1158 |
| 8 | Transactions | ❌ | Each anchor is an append to one stream; a partial fan-out returns `Left` with the failed record ids and is safe to retry because anchors are monotonic and idempotent for the same instant |
| 9 | Idempotency | ❌ | Not a message or request entry point; re-sending the same anchor is a no-op by construction (same instant, same kind) |
| 10 | Multi-Tenancy | ✅ | REQ-061/AC-043: policies, records, anchors and floor queries per tenant; deployment-level policies with tenant `null`; legal-hold tenancy in #1249 |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` keeps flowing onto records as ADR-031 needs, and `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Anchors, suspensions, re-applications and corrections are events on the record stream (Art. 5(2)); refusals are logged and metered, and recorded by P-02 in the DSR stream |

---

## Provider Matrix

| Provider | Applies | Notes |
|----------|:-------:|-------|
| ADO.NET ×3, Dapper ×3, EF Core ×3, MongoDB | ❌ | `Encina.Compliance.Retention` has no relational or document stores since ADR-019; SPEC-002 DEC-008 (a) keeps it Marten-only in 1.0 |
| Marten (PostgreSQL) | ✅ | Events, aggregates, projections and indexes; integration-tested through Testcontainers |
| Data-level immutable guard (OD-5, option B only) | 10 providers via one decorator | Provider-neutral decorator over `IFunctionalRepository`; no provider-specific code |

## Test Matrix

| Test Type | Required? | Scope | Notes |
|-----------|:---------:|-------|-------|
| UnitTests | ✅ | Every new type and branch; telemetry and log redaction | NSubstitute for Marten repositories |
| GuardTests | ✅ | Public constructors and methods | |
| ContractTests | ✅ | Floor query and policy resolution contracts | |
| PropertyTests | ✅ | Calendar arithmetic, floor monotonicity | FsCheck via `Encina.Testing.FsCheck` |
| IntegrationTests | ✅ | Marten on PostgreSQL, two tenants, AC-001 end to end | Testcontainers collection fixture |
| LoadTests | ✅ | Extend the existing `RetentionLoadTests.cs` with concurrent anchors | Existing file, no `.md` |
| BenchmarkTests | 📄 | Update the stale `Retention.md` | Not a hot path |

## Public API Changes

| Change | Kind |
|--------|------|
| `CalendarPeriod`, `RetentionPolicyKey`, `ExpiryDisposition`, `RetentionAnchorKinds`, `EffectiveRetention`, `EffectiveRetentionState`, `RetentionPolicyDefinition` | Added |
| `IRetentionAnchorService`, `IRetentionFloorQuery`, `IRetentionJurisdictionResolver` and default implementations | Added |
| Events `RetentionRecordAnchored`, `RetentionClockSuspended`, `RetentionPolicyReappliedToRecord`, `RetentionRecordCorrected` | Added |
| `RetentionStatus.AwaitingAnchor` | Added |
| `RetentionPolicyCreated`, `RetentionPolicyUpdated`, `RetentionRecordTracked` shapes; aggregates and read models (`TimeSpan` → `CalendarPeriod`, `AutoDelete` → `Disposition`, key fields) | Changed (breaking) |
| `IRetentionPolicyService.GetPolicyByCategoryAsync`, `GetRetentionPeriodAsync` → `ResolvePolicyAsync`; `IRetentionRecordService.TrackEntityAsync` signature; `GetExpiredRecordsAsync` → `GetDueRecordsAsync` | Changed (breaking) |
| `RetentionOptions.DefaultRetentionPeriod`, `RetentionPolicyBuilder.RetainForDays/RetainForYears/RetainFor`, `RetentionPeriodAttribute.RetentionPeriod` (computed `TimeSpan`) | Removed |
| `RetentionPolicyType` | Removed |

## Migration Notes

None. Encina is pre-1.0 and has no users to migrate (CLAUDE.md). Event shapes change without upcasters; development databases with retention streams written by earlier builds must be recreated. The changelog fragment states the breaking changes.

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| PR #1185 changes before merging | Phase 5 builds on its enforcement code | Start Phase 5 only after #1185 merges; Phases 1-4 are independent of it |
| Calendar arithmetic in a local zone around DST | Off-by-one-hour floors near midnight | Document resolution; property tests over DST days in `Europe/Madrid`; OD-1 |
| Anchors missing because the application never raises them | Data kept forever (`AwaitingAnchor`) | Metric of records awaiting an anchor older than a configurable age; health check degraded above a threshold (optional, `RetentionHealthCheck`) |
| Legal holds are not tenant-scoped until #1249 | A hold in tenant A could defer erasure in tenant B (safe direction, never unsafe) | Document; switch the floor query to the tenant-aware lookup when #1249 merges |
| Removing the entity id from telemetry reduces operability | Harder support diagnostics | Record ids are logged and can be looked up in Marten |
| Scope of the immutable class (OD-5) | Expectation mismatch with AC-001 | Maintainer decision before Phase 3 |

---

## Open Decisions for the Maintainer

1. **OD-1 — Time zone of calendar arithmetic.** Add periods in UTC, or in a configured zone per policy (default `UTC`, with `Europe/Madrid` set by the application)? The plan implements a per-policy zone defaulting to UTC.
2. **OD-2 — Episode semantics and anchors after blocking.** Does opening a new episode suspend the clock until the next discharge (the plan's default, `SuspendClockAsync`), or does the clock keep running from the previous discharge? And does a new anchor on a blocked entity and category unblock the data (shared with P-03 OD-11)?
3. **OD-3 — Jurisdiction source at tracking time.** Options default plus per-tenant map (the plan's default), a resolver the application implements, or a value on each tracking call or attribute. What happens when no policy matches: fall back to `DefaultPolicy`, or refuse to track (fail closed)?
4. **OD-4 — Deployment-level versus tenant-level policies.** Do code-configured policies apply to every tenant as defaults a tenant can override (the plan's default)? May a tenant policy set a shorter floor than the deployment-level one?
5. **OD-5 — Scope of the immutable-record class.** Retention-level only (records frozen, corrections linked; the plan's default), or also a data-level guard in `Encina.DomainModeling` (`IImmutableRecord` plus a decorator over `IFunctionalRepository` that refuses updates and deletes on all 10 providers)?
6. **OD-6 — Name of the period type.** `CalendarPeriod` (the plan) or `RetentionPeriod` as in the issue sketch, which collides with `RetentionPeriodAttribute` and the existing `RetentionPeriod` members?
7. **OD-7 — `ExpiryDisposition.Block` before P-03.** Ship the member now with a start-up rejection until an `IBlockingService` exists (the plan), or add it only in P-03?
8. **OD-8 — Subject-wide anchors.** Should an anchor such as a death apply to every category of an entity in one call, or stay per (entity, category) as the plan implements?
9. **OD-9 — Policy changes after tracking.** Snapshot at tracking plus explicit `ReapplyPolicyAsync` (the plan), or records that follow the live policy? May re-application shorten a floor (the plan allows it only with `allowShortening: true` and a warning)?

## Spec Gaps Found

- REQ-001 does not say what happens after the floor elapses when a policy has no maximum; the plan takes "no automatic action; erasure only on request".
- REQ-001 and AC-001 say the period "re-anchors when a new episode opens"; the legal anchor is the discharge, not the opening, so the plan models opening as a suspension (OD-2).
- REQ-001 does not state the time zone of calendar arithmetic (OD-1), while S4 relies on `Europe/Madrid` for scheduling.
- REQ-001 does not say whether a regional period replaces or adds to the national one; the plan resolves the most specific key.
- The retention pipeline and hosted services pass no tenant today (`TrackEntityAsync(..., Guid.Empty, ...)` with `tenantId` null), and `RetentionDiagnostics` tags activities with the entity id; both conflict with REQ-061/REQ-062 and are fixed here.

---

## Next Steps

1. Review and approve this plan; decide OD-1 … OD-9
2. Link it from issue #1187
3. Wait for PR #1185 before Phase 5
4. One commit per phase; the final commit references `Fixes #1187`
