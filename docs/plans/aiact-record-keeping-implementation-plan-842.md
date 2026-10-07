# Implementation Plan: `Encina.Compliance.AIAct` — Record-Keeping & Automatic Logging (Art. 12)

> **Issue**: [#842](https://github.com/dlrivada/Encina/issues/842) (child of [#415](https://github.com/dlrivada/Encina/issues/415), milestone v0.16.0 — AI Act)
> **Type**: Feature
> **Complexity**: Medium-High (11 phases, Marten event sourcing, 1 append-only log, 1 background service, 1 exporter, ~55 files)
> **Estimated Scope**: ~1,800-2,300 lines of production code + ~1,800-2,200 lines of tests
> **Hard prerequisite**: [#847](https://github.com/dlrivada/Encina/issues/847) (AIAct to Marten event sourcing), open, not implemented

---

## Summary

Implement the EU AI Act Article 12 record-keeping capability for high-risk AI systems built with Encina: automatic, durable recording of the events of each use of an AI system (Art. 12(1), 12(2)), the minimum fields Art. 12(3) asks for remote biometric identification systems, retention of those logs for at least six months (Art. 19(1) for providers, Art. 26(6) for deployers) followed by their disposal, and an export of the logs for a conformity assessment (Art. 43) or a market surveillance authority's reasoned request (Art. 19, Art. 21, Art. 74).

### Relationship with #847: this plan must follow #847, it cannot include it

#842 itself was annotated in March 2026 as "largely subsumed" by the Marten migration (#847), with a remaining scope of the exporter, the retention policy and standard-conformant formatting. Reading today's code and #847 shows that the remaining scope is larger than that note says, and that it depends on #847:

1. **#847 is not implemented.** `src/Encina.Compliance.AIAct/Encina.Compliance.AIAct.csproj:12` references only `Encina`; there is no `Aggregates/`, `Events/`, `ReadModels/` or `Services/` folder in the package; the registry is still `InMemoryAISystemRegistry` (`InMemoryAISystemRegistry.cs:33`, a `ConcurrentDictionary`) and human decisions are still kept in `DefaultHumanOversightEnforcer.cs:31`. The Marten, DomainModeling, Caching and Tenancy references, the `AddAIActAggregates()` registration, the Marten integration-test fixture usage and the system identity (#847 keys `AISystemAggregate` by `Guid`, today's attributes and registry key by `string SystemId`, `IAISystemRegistry.cs:55`) all arrive with #847. #842 builds on each of them.
2. **#847 does not record the use of a system.** Its two aggregates record the registry lifecycle (Art. 6(3), Art. 51) and human oversight sessions (Art. 14). Art. 12 asks for the events of each *use*: the period of each use, the input that led to a result, the persons who verified it, errors and anomalies. Nothing in #847 records a request the pipeline let through. That per-use log is the core of this plan and is not subsumed.
3. **One overlap must be settled in #847 before it is built.** #847 puts `AISystemComplianceEvaluated` (one event per pipeline compliance check) into the `AISystemAggregate` stream (`docs/plans/aiact-marten-es-migration-plan.md`, Phase 1). That turns the registry stream into an unbounded per-request log. This plan recommends that #847 drops that event and that compliance-check outcomes go into the record-keeping log designed here (Design Choice 3). The orchestrator should comment this on #847.
4. **Size.** #847 is ~46 files on its own; adding this plan's ~55 files to one pull request would produce an unreviewable change mixing two decisions.

So the plan for #842 is sequenced after #847 (Design Choice 1); its Phase 1 starts from the state #847 leaves.

### What is already in the code

| Item | Location | State |
|---|---|---|
| Structured logging of compliance checks (#415) | `Diagnostics/AIActLogMessages.cs:25`-`157` (EventIds 9500-9512, `LoggerMessage.Define`) | Done; ephemeral logs, not durable records |
| OpenTelemetry for the pipeline | `Diagnostics/AIActDiagnostics.cs` (ActivitySource and Meter `Encina.Compliance.AIAct`) | Done; reused and extended |
| Pipeline hook point | `AIActCompliancePipelineBehavior.cs:127` (`RunCheckAsync`), `:161` (`nextStep()`), `:220` (`BlockProhibited`) | Done; no durable record of any outcome |
| Durable per-use log, retention, export | none | **This plan** |
| Operation and read audit stores (#573, ADR-036) | `src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs:32`, `IReadAuditStore.cs` | Done; used for the audit of exports and disposals, not as the log itself |
| Period-key crypto-shredding | `src/Encina.Marten.GDPR/Attributes/CryptoShreddedAttribute.cs`, `Abstractions/ISubjectKeyProvider.cs:119` (`DeleteSubjectKeysAsync`) | Done; reused for retention (Design Choice 6) |

### Deviations from the issue text

- **No in-memory default.** The acceptance criterion "In-memory default implementation" contradicts `AGENTS.md` §3: event-sourced compliance modules have no InMemory stores; unit tests mock the Marten abstractions and integration tests run on PostgreSQL. Design Choice 8 puts this to the maintainer.
- **`IAuditStore` (#573)** is now `IOperationAuditStore` (ADR-036). The log is not a fourth audit store; ADR-036 keeps exactly three. The audit stores record who exported or disposed of logs (Phase 8).
- **`IConformityLogExporter.ExportForConformityAsync`** writes to a caller-supplied `Stream` and returns a manifest, instead of returning the whole export in memory (Design Choice 7).
- **Dates.** Annex III high-risk obligations apply from 2 Dec 2027 and Annex I from 2 Aug 2028 (Reg. (EU) 2026/1744, SPEC-002 §3.2, issue comment). The feature is opt-in and carries no date logic; the XML documentation and the feature page state those dates.

**Affected packages**: `Encina.Compliance.AIAct` (modified: new `RecordKeeping/` folder). New project references: `Encina.Marten.GDPR` and `Encina.Compliance.DataSubjectRights` (for `[CryptoShredded]` and `[PersonalData]`), `Encina.DistributedLock` (abstraction only). `Encina.Security.Audit` and `Encina.Compliance.Attestation` are referenced for optional integrations.

**Provider category**: Event sourcing (Marten on PostgreSQL), per ADR-019 and ADR-027. The 10-provider database rule does not apply (`AGENTS.md` §5 excludes event sourcing).

---

## Design Choices

<details>
<summary><strong>1. Sequencing — follow #847, include #847, or build independently</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Follow #847 (hard prerequisite, separate PR)** | Builds on the Marten references, registration and system identity #847 decides; each PR carries one decision; #847's overlap (`AISystemComplianceEvaluated`) is settled once | #842 waits for #847; the two plans must stay aligned |
| **B — Include #847 in this plan and PR** | One delivery for all AIAct persistence; no cross-plan alignment | ~100 files in one PR; mixes registry/oversight design with logging design; #847 already has its own 8-phase plan |
| **C — Build independently now, with its own Marten wiring** | Not blocked; the log does not need #847's aggregates | Adds the Marten references and registration twice (once here, again in #847); the exporter cannot include registry and oversight events; the system identity may change under it |

### Chosen Option: **A — Follow #847** (recommended, pending the maintainer)

### Rationale

We recommend A. Every building block this plan uses that is not in today's code (`Encina.Marten` reference, `AddAIActAggregates()`, `IAISystemService`, the Marten integration-test collection usage for AIAct) is delivered by #847, and the exporter is only complete when it can add the registry and oversight streams of #847 to the per-use log. B would double the change and hide two separate decisions in one review. C duplicates wiring and forces a later rework when #847 changes the system identity.

</details>

<details>
<summary><strong>2. Package placement — folder in the core package vs satellite package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — `RecordKeeping/` folder in `Encina.Compliance.AIAct`** | Same pattern as the nine compliance modules migrated under ADR-019 and as #847 (aggregates in the core package); the pipeline behavior calls the recorder without cross-package plumbing; one `AddAIActRecordKeeping()` call | The core package gains the Marten.GDPR and DataSubjectRights references |
| **B — New `Encina.Compliance.AIAct.RecordKeeping` package (issue title)** | Applications that do not need Art. 12 do not take the crypto-shredding references | The pipeline behavior in the core needs an extension point for a satellite; one more package to version, document and measure; no compliance module has such a satellite |
| **C — Inside `Encina.Audit.Marten`** | Reuses the temporal-key machinery directly | Mixes an AI Act concept into a generic audit package; ADR-036 keeps that package for the three audit stores |

### Chosen Option: **A — `RecordKeeping/` folder in the core package** (recommended, pending the maintainer)

### Rationale

We recommend A. After #847 the AIAct core already depends on Marten, so the incremental cost of A is two project references. Registration stays opt-in (`AddAIActRecordKeeping()`), which satisfies pay-for-what-you-use at the service level. The folder follows `AGENTS.md` §4 (feature-named folder, as `LawfulBasis/`, `Consent/`).

</details>

<details>
<summary><strong>3. Storage model of the log</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Dedicated append-only Marten streams, one per system and UTC day, with an inline projection to `AISystemEventReadModel`** | Bounded streams; range queries by system and period hit the read model; retention and export work on whole periods; no aggregate invariants to load before each append | A deterministic stream id scheme to maintain; read model storage doubles the bytes |
| **B — Events inside #847's `AISystemAggregate` stream** | No new stream design | Unbounded stream per system (one event per request); every registry load folds the whole usage history; retention cannot dispose of usage without touching registry evidence |
| **C — Entries in `IOperationAuditStore` (ADR-036) with AI fields in `Metadata`** | Reuses the 10 database providers and the audit retention service | Loses typed Art. 12(3) fields; one retention period for all audit entries; ADR-036 forbids a fourth purpose inside the operation store; payload redaction rules differ |
| **D — One stream per event** | Trivial append | Millions of one-event streams; retention and export must scan all streams |

### Chosen Option: **A — Dedicated per-system, per-day streams with an inline projection** (recommended, pending the maintainer)

### Rationale

We recommend A. The log is append-only and has no invariants, so it does not need an aggregate: the recorder appends `AISystemEventRecorded` to the stream of its bucket, as `MartenOperationAuditStore.cs:127`-`130` does for audit entries. Encina's aggregate repository loads by `Guid` (`src/Encina.Marten/IAggregateRepository.cs:20`) and a Marten store has a single stream identity setting, so bucket ids are deterministic name-based UUIDs of `aiact-log:{tenant}:{systemId}:{yyyy-MM-dd}` and stay compatible with #847's `Guid`-keyed aggregates. The inline projection keyed by `EventId` makes queries cheap and gives idempotency (Design Choice 4 and the matrix).

</details>

<details>
<summary><strong>4. What is recorded automatically</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Automatic recording by the pipeline plus an explicit recorder API** | The pipeline records every decorated request (use started, use completed with outcome and period, prohibited attempt, failed compliance check, oversight required); applications add domain events (prediction, override, model update) through `IAIEventRecorder` | The pipeline behavior grows; two event sources to document |
| **B — Explicit API only** | Smallest change to the pipeline | "Automatic recording" (Art. 12(1)) depends on every application remembering to call it |
| **C — Automatic only** | No API to misuse | The pipeline cannot know prediction outputs, reference databases or the person who verified a match (Art. 12(3)(b)-(d)) |

### Chosen Option: **A — Pipeline recording plus explicit API** (recommended, pending the maintainer)

### Rationale

We recommend A. Art. 12(1) asks that the system "technically allow" automatic recording; only the pipeline sees every use, so it is the place for the automatic part. The domain facts of Art. 12(3) are only known to the handler, so the explicit API stays. The pipeline records only for requests that carry an AI Act attribute (`AIActCompliancePipelineBehavior.cs:114`-`123` already skips the rest), so there is no overhead for other requests.

</details>

<details>
<summary><strong>5. Recording failure semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Fail closed before execution, explicit logged opt-out** | A use that cannot be recorded does not happen; matches `AGENTS.md` §3 (compliance gates fail closed, the only opt-out is explicit and logged) | A PostgreSQL outage blocks AI requests |
| **B — Fail open (log a warning, continue)** | Availability first | Silent gaps in a log whose purpose is completeness; contradicts §3 |
| **C — Buffer locally and retry later** | Availability and eventual completeness | A local buffer is volatile; a second persistence path; ordering and duplicate handling |

### Chosen Option: **A — Fail closed before execution, explicit logged opt-out** (recommended, pending the maintainer)

### Rationale

We recommend A. The pipeline appends `UsageStarted` before `nextStep()`; if that append fails, the request returns `Left(AIActErrors.RecordKeepingUnavailable)` in `Block` mode. `UsageCompleted` is appended after the handler; its failure cannot undo the handler's effects, so it is logged at Error level, counted in a metric and reported by the health check, never swallowed. `AIActRecordKeepingOptions.RecordingFailureMode = FailOpen` is the explicit opt-out; choosing it logs a warning at startup and on every failed append.

</details>

<details>
<summary><strong>6. Retention and disposal mechanism</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Period-key crypto-shredding through `Encina.Marten.GDPR`** | Personal fields are bound to a key per system and month (`[CryptoShredded(SubjectIdProperty = nameof(RetentionKeyId))]`); disposal destroys the key (`ISubjectKeyProvider.DeleteSubjectKeysAsync`, `ISubjectKeyProvider.cs:119`); the event skeleton (type, time, system, outcome) stays as traceability evidence; the event store stays immutable | Adds the Marten.GDPR serializer requirement; per-person erasure inside the retention period is not possible (covered by GDPR Art. 17(3)(b) legal obligation) |
| **B — Temporal keys of `Encina.Audit.Marten`** | Proven retention service (`MartenOperationAuditRetentionService.cs`) | Keys are global per period (`ITemporalKeyProvider.cs:53`), so one retention period for every system; couples AIAct to the audit package |
| **C — Hard delete of expired bucket streams and read models** | Real deletion; nothing left | Breaks the immutability ADR-019 relies on; raw SQL against Marten tables; no skeleton for traceability |
| **D — Marten stream archiving** | Built into Marten | Archived events still hold the personal data; does not meet storage limitation (GDPR Art. 5(1)(e)) |

### Chosen Option: **A — Period-key crypto-shredding** (recommended, pending the maintainer)

### Rationale

We recommend A. It reuses the crypto-shredding engine the other Marten compliance modules use, allows a retention period per system (Art. 26(6) lets national law set longer periods), and leaves an unreadable-but-present skeleton, which keeps the stream's integrity and the count of uses verifiable. A period is shredded only when its whole month ends before the cutoff, so the effective retention is never shorter than configured. The options validator rejects any period shorter than six months (Art. 19(1), Art. 26(6)), and a legal hold on the system defers disposal; a failed hold lookup also defers it (fail closed, the lesson of #1143).

</details>

<details>
<summary><strong>7. Conformity export format and integrity</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — JSON Lines streamed to a caller `Stream`, plus a manifest with a SHA-256 digest and an optional attestation receipt** | Constant memory for any range; line-oriented format suits large logs and diff tools; the digest and an `IAuditAttestationProvider` receipt (`src/Encina.Compliance.Attestation/Abstractions/IAuditAttestationProvider.cs:26`) make the export tamper-evident | No spreadsheet-friendly format out of the box |
| **B — One JSON document returned in memory (issue signature)** | Simplest API | Memory grows with the range; no integrity evidence |
| **C — Pluggable `IConformityLogFormatter` (JSON Lines, CSV, others)** | Any format an authority asks for | More API before any authority has asked for a format; no harmonised standard to target yet |

### Chosen Option: **A — JSON Lines to a stream with a digest manifest and optional attestation** (recommended, pending the maintainer)

### Rationale

We recommend A. Art. 12(1) asks for logging that conforms to recognised standards or common specifications; no harmonised standard for AI logging was found cited under Art. 40 at the time of this plan (a draft ISO/IEC 24970 on AI system logging is in preparation; to be re-checked at implementation). A documented, versioned JSON Lines schema (`schemaVersion` in each manifest) is the neutral choice and can gain a formatter when a standard is published. Writing to a stream keeps memory constant. The attestation receipt is used only when `Encina.Compliance.Attestation` is registered.

</details>

<details>
<summary><strong>8. In-memory default implementation (issue acceptance criterion)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — No in-memory recorder; unit tests mock the Marten session and `ISubjectKeyProvider`, integration tests run on PostgreSQL** | Follows `AGENTS.md` §3 for event-sourced compliance modules; one production path | Local development without PostgreSQL cannot record (the feature is opt-in, so nothing breaks) |
| **B — In-memory recorder registered by default, replaced by Marten** | Matches the issue text; demo without a database | Contradicts `AGENTS.md` §3; a volatile "log" can be mistaken for compliance in production; two paths to test |

### Chosen Option: **A — No in-memory recorder** (recommended, pending the maintainer)

### Rationale

We recommend A. A record-keeping log that disappears on restart is the exact gap #847 removes for the registry; offering it as a default would recreate it. `AddAIActRecordKeeping()` is opt-in, so applications that do not need Art. 12 do not register anything. The acceptance criterion of #842 is updated accordingly when the maintainer decides.

</details>

---

## Implementation Phases

### Phase 1: Domain Model, Events & Errors

> **Goal**: Define the log entry, the event types, the Marten event and the result records.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Compliance.AIAct/RecordKeeping/AIEventType.cs`** (namespace `Encina.Compliance.AIAct.RecordKeeping`)
   - Enum with the issue's values plus `UsageStarted` and `UsageCompleted` (Art. 12(3)(a), period of each use): `SystemStarted`, `SystemStopped`, `UsageStarted`, `UsageCompleted`, `PredictionMade`, `ClassificationMade`, `RecommendationMade`, `HumanOverrideRecorded`, `HumanApprovalRecorded`, `ErrorOccurred`, `AnomalyDetected`, `ModelUpdated`, `ConfigurationChanged`, `DataIngested`, `DataQualityChecked`, `ComplianceCheckPassed`, `ComplianceCheckFailed`, `ProhibitedUseAttempted`, `HumanOversightRequired`.
2. **Create `RecordKeeping/AISystemEvent.cs`** — `sealed record` (input of `IAIEventRecorder`)
   - Required: `Guid EventId`, `string SystemId`, `AIEventType EventType`, `DateTimeOffset OccurredAtUtc`.
   - Optional: `string? Description`, `string? UserId`, `string? InputSummary`, `string? OutputSummary`, `string? CorrelationId`, `string? RequestType`, `string? Outcome`, `DateTimeOffset? UsageStartedAtUtc`, `DateTimeOffset? UsageEndedAtUtc`, `string? ReferenceDatabase` (Art. 12(3)(b)), `string? MatchedInputReference` (Art. 12(3)(c)), `string? VerifiedBy` (Art. 12(3)(d)), `string? TenantId`, `string? ModuleId`, `IReadOnlyDictionary<string, string>? Metadata` (documented: must not hold personal data).
3. **Create `RecordKeeping/Events/AISystemEventRecorded.cs`** — the Marten event, `sealed record`
   - Same fields plus `string RetentionKeyId` (`aiact-log:{tenant}:{systemId}:{yyyy-MM}`) and `int SchemaVersion`.
   - `[PersonalData]` + `[CryptoShredded(SubjectIdProperty = nameof(RetentionKeyId))]` on `Description`, `UserId`, `InputSummary`, `OutputSummary`, `MatchedInputReference`, `VerifiedBy`, `ReferenceDatabase`.
4. **Create `RecordKeeping/RetentionPolicyResult.cs`** — the issue's record (`SystemId`, `EventsRetained`, `EventsPurged`, `RetentionCutoffUtc`) plus `int PeriodsShredded`, `int PeriodsDeferred`.
5. **Create `RecordKeeping/ConformityExportRequest.cs`** (`SystemId`, `FromUtc`, `ToUtc`, `bool IncludeRegistryAndOversightEvents = true`) and **`RecordKeeping/ExportedLogs.cs`** (manifest: `SystemId`, `FromUtc`, `ToUtc`, `GeneratedAtUtc`, `int SchemaVersion`, `long EventCount`, `long ShreddedEventCount`, `string ContentType` = `application/x-ndjson`, `string Sha256`, `string? AttestationReceiptId`).
6. **Create `RecordKeeping/RecordingFailureMode.cs`** — `FailClosed` (default), `FailOpen`.
7. **Modify `src/Encina.Compliance.AIAct/AIActErrors.cs`** (class at line 22): codes `aiact.record_keeping_unavailable`, `aiact.record_keeping_invalid_event`, `aiact.retention_failed`, `aiact.export_failed`, `aiact.export_invalid_range` with factory methods; messages never contain exception messages (`AGENTS.md` §3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #842 (AI Act Art. 12 record-keeping) in Encina.Compliance.AIAct.

CONTEXT:
- #847 (AIAct to Marten event sourcing) is merged: the package references Encina.Marten and has Aggregates/, Events/, ReadModels/, Services/.
- This phase adds the record-keeping domain model under src/Encina.Compliance.AIAct/RecordKeeping/.
- The log is append-only; personal fields are crypto-shredded per system and month (Encina.Marten.GDPR).

TASK:
Create AIEventType, AISystemEvent, Events/AISystemEventRecorded, RetentionPolicyResult, ConformityExportRequest,
ExportedLogs and RecordingFailureMode, and add the five record-keeping error codes to AIActErrors.cs.

KEY RULES:
- .NET 10 / C# 14, nullable enabled, sealed records with required members, IReadOnlyDictionary for maps.
- UTC timestamps end in AtUtc. No DateTime.UtcNow anywhere.
- AISystemEventRecorded marks Description, UserId, InputSummary, OutputSummary, MatchedInputReference, VerifiedBy and
  ReferenceDatabase with [PersonalData] and [CryptoShredded(SubjectIdProperty = nameof(RetentionKeyId))].
- Error messages never include exception messages; codes follow "aiact.*".
- XML docs cite the AI Act article each field serves (Art. 12(1)-(3), Art. 19(1), Art. 26(6)).
- Add every public symbol to PublicAPI.Unshipped.txt.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActErrors.cs
- src/Encina.Compliance.AIAct/Model/HumanDecisionRecord.cs
- src/Encina.Marten.GDPR/Attributes/CryptoShreddedAttribute.cs
- src/Encina.Audit.Marten/Events/OperationAuditEntryRecordedEvent.cs
```

</details>

---

### Phase 2: Abstractions, Options & Validator

> **Goal**: Public contracts and configuration.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `RecordKeeping/Abstractions/IAIEventRecorder.cs`**
   - `ValueTask<Either<EncinaError, Unit>> RecordEventAsync(AISystemEvent systemEvent, CancellationToken cancellationToken = default)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<AISystemEventReadModel>>> GetEventsAsync(string systemId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<AISystemEventReadModel>>> GetEventsByTypeAsync(string systemId, AIEventType eventType, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)` (a range is required; an unbounded query over a lifetime log is not offered)
2. **Create `RecordKeeping/Abstractions/IAILogRetentionPolicy.cs`** — `ValueTask<Either<EncinaError, RetentionPolicyResult>> ApplyRetentionAsync(string systemId, CancellationToken cancellationToken = default)` and `ValueTask<Either<EncinaError, IReadOnlyList<RetentionPolicyResult>>> ApplyRetentionToAllAsync(CancellationToken cancellationToken = default)`.
3. **Create `RecordKeeping/Abstractions/IConformityLogExporter.cs`** — `ValueTask<Either<EncinaError, ExportedLogs>> ExportForConformityAsync(ConformityExportRequest request, Stream destination, CancellationToken cancellationToken = default)`.
4. **Create `RecordKeeping/Abstractions/IAILogLegalHoldCheck.cs`** — `ValueTask<Either<EncinaError, bool>> IsUnderLegalHoldAsync(string systemId, string? tenantId, CancellationToken cancellationToken = default)`; optional port, no default registration; the feature page shows a three-line adapter over `ILegalHoldService.HasActiveHoldsAsync` (`src/Encina.Compliance.Retention/Abstractions/ILegalHoldService.cs:178`).
5. **Create `RecordKeeping/AIActRecordKeepingOptions.cs`** — `int RetentionMonths = 6`; `Dictionary<string, int> SystemRetentionMonths`; `RecordingFailureMode RecordingFailureMode = FailClosed`; `bool RecordPipelineUsage = true`; `bool EnableAutoRetention`; `TimeSpan RetentionInterval = 24 h`; `int ExportPageSize = 1000`; `bool AddHealthCheck`.
6. **Create `RecordKeeping/AIActRecordKeepingOptionsValidator.cs`** (`IValidateOptions<AIActRecordKeepingOptions>`): retention ≥ 6 months globally and per system (Art. 19(1), Art. 26(6)); interval ≥ 1 h; page size 1..10,000.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #842 in Encina.Compliance.AIAct.

CONTEXT:
- Phase 1 created the record-keeping model under src/Encina.Compliance.AIAct/RecordKeeping/.
- This phase defines the public interfaces and the options with their validator.

TASK:
Create IAIEventRecorder, IAILogRetentionPolicy, IConformityLogExporter, IAILogLegalHoldCheck,
AIActRecordKeepingOptions and AIActRecordKeepingOptionsValidator.

KEY RULES:
- Every method returns ValueTask<Either<EncinaError, T>> and takes a CancellationToken.
- Queries always take a UTC range; no unbounded lifetime query.
- The validator rejects retention below 6 months (Art. 19(1), Art. 26(6)) for the default and every per-system override.
- The options class holds no password, connection string, token or key, so the [JsonIgnore]/ToString rule does not apply.
- XML docs with <example> on each interface; PublicAPI.Unshipped.txt updated.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActOptions.cs
- src/Encina.Compliance.AIAct/AIActOptionsValidator.cs
- src/Encina.Compliance.Retention/Abstractions/ILegalHoldService.cs
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
```

</details>

---

### Phase 3: Marten Recorder, Stream Buckets & Projection

> **Goal**: Durable, idempotent recording and range queries.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `RecordKeeping/AILogStreamIds.cs`** (internal static): `Guid ForBucket(string? tenantId, string systemId, DateOnly dayUtc)` (name-based UUID v5 over `aiact-log:{tenant or "-"}:{systemId}:{yyyy-MM-dd}`, fixed namespace Guid); `string RetentionKeyFor(string? tenantId, string systemId, DateTimeOffset occurredAtUtc)` (`aiact-log:{tenant}:{systemId}:{yyyy-MM}`).
2. **Create `RecordKeeping/ReadModels/AISystemEventReadModel.cs`** (`IReadModel`, `Id` = `EventId`) with the event's fields, `string RetentionKeyId`, `bool IsShredded`.
3. **Create `RecordKeeping/ReadModels/AISystemEventProjection.cs`** — `IProjection<AISystemEventReadModel>`, `IProjectionCreator<AISystemEventRecorded, AISystemEventReadModel>`; idempotent (same `EventId` yields the same document).
4. **Create `RecordKeeping/MartenAIEventRecorder.cs`** (`IAIEventRecorder`, scoped). Dependencies: `IDocumentSession`, `IRequestContextAccessor`, `IModuleExecutionContext?`, `TimeProvider`, `ILogger<MartenAIEventRecorder>`.
   - Validate input (non-empty `SystemId`, `OccurredAtUtc` not in the future beyond a 5-minute skew from `TimeProvider`, usage end ≥ start) → `Left(RecordKeepingInvalidEvent)`.
   - Fill `TenantId`/`ModuleId` from the request context when the caller left them null.
   - Idempotency: if `AISystemEventReadModel` with `EventId` exists, return `Right(unit)` and log `DuplicateEventSkipped`.
   - Append `AISystemEventRecorded` to `AILogStreamIds.ForBucket(...)`, `SaveChangesAsync(ct)`; exceptions → `Left(RecordKeepingUnavailable)` with `ex.ForLogging()`.
   - Queries use the read model with `SystemId`, `TenantId` and range filters, ordered by `OccurredAtUtc`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #842 in Encina.Compliance.AIAct.

CONTEXT:
- Phases 1-2 created the model, the interfaces and the options.
- The log is append-only: no aggregate. Events go to one Marten stream per system and UTC day; the stream id is a
  deterministic name-based UUID because Encina's aggregates (from #847) use Guid stream identity.
- An inline projection keyed by EventId gives range queries and idempotency.

TASK:
Create AILogStreamIds, AISystemEventReadModel, AISystemEventProjection and MartenAIEventRecorder.

KEY RULES:
- Time only from TimeProvider. All database calls async with the CancellationToken.
- Errors return Left; exceptions are logged through ex.ForLogging(), never their message; EncinaError.Message never logged.
- TenantId and ModuleId are taken from IRequestContextAccessor / IModuleExecutionContext when not supplied.
- A duplicate EventId is a success (Right) and logs DuplicateEventSkipped.
- Methods stay under CRAP 10: split validation, enrichment and append into small private methods.

REFERENCE FILES:
- src/Encina.Audit.Marten/MartenOperationAuditStore.cs (raw append, error mapping)
- src/Encina.Audit.Marten/Projections/OperationAuditEntryProjection.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs (tenant resolution)
- src/Encina.Marten/Projections/IProjection.cs
```

</details>

---

### Phase 4: Pipeline Integration (Automatic Recording)

> **Goal**: The pipeline records every decorated use.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs`**
   - Constructor (line 69): add optional `IAIEventRecorder? recorder = null` and `IOptions<AIActRecordKeepingOptions>? recordKeepingOptions = null` (Microsoft DI honours default values when the service is not registered).
   - `BlockProhibited` (line 220): record `ProhibitedUseAttempted` (best effort, never changes the block).
   - Violations blocked: record `ComplianceCheckFailed`.
   - Before `nextStep()` (line 161): record `UsageStarted`; on `Left` apply `RecordingFailureMode` (Design Choice 5).
   - After `nextStep()`: record `UsageCompleted` with `UsageStartedAtUtc`, `UsageEndedAtUtc` and `Outcome` (`success` or the error code); also `HumanOversightRequired` when `compliance.RequiresHumanOversight`.
   - Move the recording into a private `UsageRecording` helper (or an internal `AIActUsageRecorder` class) so `RunCheckAsync` stays under CRAP 10.
   - `CorrelationId` from `IRequestContext.CorrelationId`, `UserId` from the request context identity, `RequestType` = request type name.
2. **No recording in `Disabled` mode** (line 104 returns before any check).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #842 in Encina.Compliance.AIAct.

CONTEXT:
- Phase 3 delivered MartenAIEventRecorder (IAIEventRecorder).
- AIActCompliancePipelineBehavior (src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs) evaluates AI Act
  attributes; it must now record each use automatically when record-keeping is registered.

TASK:
Inject an optional IAIEventRecorder and AIActRecordKeepingOptions; record UsageStarted before nextStep (fail closed by
default), UsageCompleted after it, ProhibitedUseAttempted and ComplianceCheckFailed on blocks, HumanOversightRequired
when required.

KEY RULES:
- No recording when no recorder is registered or the mode is Disabled; zero overhead for requests without attributes.
- FailClosed: a failed UsageStarted returns Left(AIActErrors.RecordKeepingUnavailable(...)) in Block mode.
  FailOpen: log a warning (EventId in the AIAct range) and continue.
- A failed UsageCompleted is logged at Error level and counted; it never changes the handler's result and is never swallowed silently.
- Keep every changed method at CRAP <= 10; extract the recording into a small helper.
- TimeProvider for timestamps; the behavior gains TimeProvider through DI.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs
- src/Encina/Abstractions/IRequestContext.cs (CorrelationId, TenantId, identity)
- docs/engineering/crap-gate-design.md
```

</details>

---

### Phase 5: Retention Policy & Background Enforcement

> **Goal**: Dispose of personal fields after the retention period, never earlier.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `RecordKeeping/CryptoShredAILogRetentionPolicy.cs`** (`IAILogRetentionPolicy`, scoped). Dependencies: `IQuerySession`, `ISubjectKeyProvider`, `IAILogLegalHoldCheck?`, `IOptions<AIActRecordKeepingOptions>`, `TimeProvider`, `ILogger<...>`.
   - Cutoff = start of the month that is `RetentionMonths` (or the system override) before the current UTC month.
   - Distinct `RetentionKeyId` values of the system's read models older than the cutoff and not shredded.
   - Legal hold: `IsUnderLegalHoldAsync` → `true` or `Left` defers every period of that system (fail closed), logs `PeriodDeferredLegalHold` or `LegalHoldLookupFailed`.
   - Per period: `DeleteSubjectKeysAsync(retentionKeyId)`; on `Right` mark the read models `IsShredded = true`; on `Left` stop and return it (no partial success reported as success).
2. **Create `RecordKeeping/AILogRetentionHostedService.cs`** (`BackgroundService`, registered only when `EnableAutoRetention`): `PeriodicTimer(RetentionInterval)`; new scope per cycle; acquires `IDistributedLockProvider.TryAcquireAsync("encina:aiact:log-retention", ...)` when registered (`src/Encina.DistributedLock/IDistributedLockProvider.cs:77`); a `Left` from the policy is logged and counted, the cycle ends, the next tick retries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #842 in Encina.Compliance.AIAct.

CONTEXT:
- Personal fields of AISystemEventRecorded are crypto-shredded per RetentionKeyId (system + month) by Encina.Marten.GDPR.
- Retention destroys the key of each month that ended before the cutoff; the event skeleton remains.
- Minimum retention is 6 months (Art. 19(1), Art. 26(6)); the validator already enforces it.

TASK:
Create CryptoShredAILogRetentionPolicy and AILogRetentionHostedService.

KEY RULES:
- Never shred a month that is not entirely older than the cutoff.
- A legal hold, or a failed legal-hold lookup, defers disposal (fail closed, see #1143).
- A Left from ISubjectKeyProvider fails the operation; never report success after a partial failure.
- Background service: new DI scope per cycle, optional IDistributedLockProvider, TimeProvider, [LoggerMessage] logs.
- Never log EncinaError.Message; log the code.

REFERENCE FILES:
- src/Encina.Audit.Marten/MartenOperationAuditRetentionService.cs
- src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs
- src/Encina.Compliance.Retention/ (enforcement service and legal hold handling)
- docs/architecture/adr/031-retention-erasure-port.md
```

</details>

---

### Phase 6: Conformity Log Exporter

> **Goal**: Export a system's logs for a period, verifiably.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `RecordKeeping/JsonLinesConformityLogExporter.cs`** (`IConformityLogExporter`, scoped). Dependencies: `IQuerySession`, `IAISystemService` (from #847), `IAuditAttestationProvider?`, `IOptions<AIActRecordKeepingOptions>`, `TimeProvider`, `ILogger<...>`.
   - Validate range (`FromUtc < ToUtc`, span ≤ 10 years) → `Left(ExportInvalidRange)`.
   - Page through `AISystemEventReadModel` (`ExportPageSize`), write one JSON object per line with `System.Text.Json` (`Utf8JsonWriter`) through an `IncrementalHash` (SHA-256) wrapper stream; shredded fields are written as `null` with `"shredded": true`.
   - When `IncludeRegistryAndOversightEvents`: append the registry and oversight events of #847 for the system and range, tagged `"source": "registry"` / `"oversight"`.
   - Manifest: counts, digest, `SchemaVersion = 1`; when an attestation provider is registered, attest an `AuditRecord` whose content is the manifest and put the receipt id in `ExportedLogs.AttestationReceiptId`; an attestation `Left` fails the export (`ExportFailed`).
2. **Create `RecordKeeping/ConformityLogSchema.cs`** (internal constants of the JSON Lines property names, `SchemaVersion`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #842 in Encina.Compliance.AIAct.

CONTEXT:
- The log lives in AISystemEventReadModel (Phase 3). #847 provides IAISystemService and the registry/oversight streams.
- Exports serve conformity assessment (Art. 43) and authority requests (Art. 19, 21, 74).

TASK:
Create JsonLinesConformityLogExporter and ConformityLogSchema: stream JSON Lines to the caller's Stream, compute SHA-256
while writing, return the ExportedLogs manifest, attest it when IAuditAttestationProvider is registered.

KEY RULES:
- Constant memory: page through the read model; never materialize the whole range.
- Shredded fields are null with "shredded": true; never attempt to decrypt them.
- Every failure returns Left; a failed attestation fails the export.
- Async I/O with the CancellationToken; TimeProvider for GeneratedAtUtc.

REFERENCE FILES:
- src/Encina.Compliance.Attestation/Abstractions/IAuditAttestationProvider.cs
- src/Encina.Compliance.Attestation/Model/AuditRecord.cs
- src/Encina.Compliance.AIAct/Services/DefaultAISystemService.cs (from #847)
```

</details>

---

### Phase 7: Configuration, DI & Health Check

> **Goal**: One opt-in registration that is complete on its own.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `RecordKeeping/AIActRecordKeepingServiceCollectionExtensions.cs`** — `public static IServiceCollection AddAIActRecordKeeping(this IServiceCollection services, Action<AIActRecordKeepingOptions>? configure = null)`
   - Options + validator (`ValidateOnStart`), `TryAddSingleton(TimeProvider.System)`.
   - `TryAddScoped<IAIEventRecorder, MartenAIEventRecorder>`, `TryAddScoped<IAILogRetentionPolicy, CryptoShredAILogRetentionPolicy>`, `TryAddScoped<IConformityLogExporter, JsonLinesConformityLogExporter>`.
   - `AddProjection<AISystemEventProjection, AISystemEventReadModel>()`.
   - Hosted service when `EnableAutoRetention`; health check when `AddHealthCheck`.
   - Startup check (hosted descriptor): `ISubjectKeyProvider` must be resolvable (crypto-shredding registered); fail at startup with a clear error otherwise.
2. **Create `RecordKeeping/Health/AIActRecordKeepingHealthCheck.cs`** — `DefaultName = "encina-aiact-record-keeping"`, `Tags` static array; scoped resolution via `IServiceProvider.CreateScope()`; Unhealthy when the read model cannot be queried, Degraded when `UsageCompleted` failures were counted since the last check or the oldest unshredded period is older than retention + one interval.
3. **Update `src/Encina.Compliance.AIAct/Encina.Compliance.AIAct.csproj`**: references to `Encina.Marten.GDPR`, `Encina.Compliance.DataSubjectRights`, `Encina.DistributedLock`, `Encina.Security.Audit`, `Encina.Compliance.Attestation`; drop the stale `InternalsVisibleTo` entries for `Encina.ADO.Sqlite` and `Encina.Dapper.Sqlite` if #847 has not already (SQLite is out of the matrix, ADR-024).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #842 in Encina.Compliance.AIAct.

CONTEXT:
- Phases 1-6 delivered recorder, retention policy, hosted service and exporter.
- AddAIActRecordKeeping() is called after AddEncinaAIAct() and AddAIActAggregates() (from #847).

TASK:
Create AddAIActRecordKeeping(), the record-keeping health check and the startup check for crypto-shredding, and update
the csproj references.

KEY RULES:
- Registration completeness (AGENTS.md §3): every option and dependency the services resolve is registered; prove it with
  a DI test that builds the provider with ValidateOnBuild and ValidateScopes.
- TryAdd for every service so applications can replace them.
- Health check: DefaultName const, static Tags, scoped resolution, never puts EncinaError.Message in the result.
- Missing crypto-shredding registration fails at startup, not at the first append.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Consent/ConsentMartenExtensions.cs
- src/Encina.Compliance.AIAct/Health/AIActHealthCheck.cs
- src/Encina.Audit.Marten/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 8: Cross-Cutting Integration

> **Goal**: Tenant and module scoping, audit of exports and disposals, locks.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy**: `TenantId` on every event and read model; stream and retention keys include the tenant; queries and exports filter by the current tenant when one is present (`IRequestContext.TenantId`), and the background retention runs per tenant found in the read model.
2. **Module isolation**: `ModuleId` from `IModuleExecutionContext.CurrentModule` (`src/Encina/Modules/Isolation/IModuleExecutionContext.cs:80`) on every event and read model; queries filter by module when set.
3. **Audit trail**: each export writes an `OperationAuditEntry` (`Action = "aiact.log.export"`, entity = system id, outcome) to `IOperationAuditStore` and a `ReadAuditEntry` to `IReadAuditStore` when registered; each shredded period writes `Action = "aiact.log.shred"`. A `Left` from an audit store fails the export or the disposal (errors are never swallowed in background infrastructure).
4. **Distributed lock** for the hosted service (Phase 5).
5. **Transactions**: the append and its inline projection commit in one `SaveChangesAsync`; the read-model `IsShredded` update follows a successful key deletion in the same scope.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #842 in Encina.Compliance.AIAct.

CONTEXT:
- Recorder, retention and exporter exist (Phases 3-7).
- ADR-036 keeps three audit stores; the AI log is not one of them, but exports and disposals are audited in them.

TASK:
Propagate TenantId/ModuleId, filter queries by them, audit exports (operation + read audit) and disposals (operation audit).

KEY RULES:
- Audit stores are optional (resolve if registered); when registered, a Left from them fails the operation.
- The background retention has no ambient tenant: iterate the tenants present in the read model explicitly.
- Never log EncinaError.Message.

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
- src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs
- docs/architecture/adr/036-three-audit-stores.md
- src/Encina/Modules/Isolation/IModuleExecutionContext.cs
```

</details>

---

### Phase 9: Observability

> **Goal**: Traces, metrics and structured logs for recording, retention and export.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `Diagnostics/AIActDiagnostics.cs`**: activities `AIAct.RecordEvent`, `AIAct.ApplyRetention`, `AIAct.ExportConformityLogs`; counters `aiact.log.events_recorded` (tags `aiact.event_type`, `aiact.system_id`), `aiact.log.record_failed` (tag `aiact.failure_reason` = error code), `aiact.log.periods_shredded`, `aiact.log.periods_deferred`, `aiact.log.exports`; histogram `aiact.log.record.duration` (ms).
2. **Create `Diagnostics/AIActRecordKeepingLogMessages.cs`** with the `[LoggerMessage]` source generator, ~18 messages packed sequentially inside `EventIdRanges.ComplianceAIAct` (9500-9599) immediately after the last id #847 allocates: recorded, duplicate skipped, record failed, usage start failed (blocked), usage start failed (fail open), usage completion failed, fail-open configured at startup, retention cycle started/completed/failed, period shredded, period deferred (legal hold), legal hold lookup failed, lock not acquired, export started/completed/failed, health degraded.
3. Add the XML doc line `/// Event IDs: <first>-<last> (see EventIdRanges.ComplianceAIAct)`; `EncinaEventIdAllocationTests.cs:59` already maps the assembly to `ComplianceAIAct`, no change there.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #842 in Encina.Compliance.AIAct.

CONTEXT:
- The package's range is EventIdRanges.ComplianceAIAct = (9500, 9599); 9500-9512 are used by AIActLogMessages.cs, #847 uses 9513-9529, and #842 uses 9530-9547.

TASK:
Extend AIActDiagnostics with record-keeping activities and instruments, and create AIActRecordKeepingLogMessages.cs with
[LoggerMessage] methods in EventIds 9530-9547, packed with no gaps.

KEY RULES:
- ADR-021: never use an id outside ComplianceAIAct; no sparse allocation; run the architecture tests.
- Tags never carry EncinaError.Message or exception messages; error codes only.
- Activities start only when ActivitySource.HasListeners().

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Compliance.AIAct/Diagnostics/AIActDiagnostics.cs
- src/Encina.Compliance.AIAct/Diagnostics/AIActLogMessages.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 10: Testing

> **Goal**: Every flag reaches its target; real PostgreSQL for the event store.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/AIAct/RecordKeeping/`): recorder validation, enrichment, duplicate skip, error mapping (NSubstitute `IDocumentSession`); stream id determinism; retention cutoff arithmetic (month boundaries, per-system override, legal hold true/Left); exporter paging, shredded rendering, digest, attestation failure; pipeline: fail closed, fail open, completion failure, no recorder, Disabled mode; options validator; DI completeness test with `ValidateOnBuild` + `ValidateScopes`.
2. **Guard**: every public constructor and method parameter of the new types.
3. **Contract**: `IAIEventRecorder`, `IAILogRetentionPolicy`, `IConformityLogExporter` contracts exercised against the real Marten implementations (instantiated, not reflected).
4. **Property** (FsCheck): stream id and retention key are deterministic and injective over (tenant, system, day); the cutoff never shreds a month newer than `RetentionMonths`; export digest equals the SHA-256 of the written bytes.
5. **Integration** (`tests/Encina.IntegrationTests/Compliance/AIAct/RecordKeeping/`, `[Collection(MartenCollection.Name)]`, `[Trait("Category", "Integration")]`): append and query by range; idempotent re-append; tenant isolation; crypto-shred a period and read back `IsShredded` with null fields; export of a mixed shredded/unshredded range; pipeline end to end with `UsageStarted`/`UsageCompleted`.
6. **Load**: `.md` justification (appends are independent; concurrency is Marten's). **Benchmark**: one BenchmarkDotNet benchmark of the pipeline overhead with recording on, since the pipeline is a hot path (`BenchmarkSwitcher`, results under `artifacts/performance/`).
7. **Coverage manifest**: add per-file targets and one-sentence justifications for every new file to `.github/coverage-manifest/Encina.Compliance.AIAct.json`; measure each flag and run `coverage-report.cs --check-justifications`; local CRAP table for changed methods (≤ 10).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 (testing) of issue #842 in Encina.Compliance.AIAct.

CONTEXT:
- Phases 1-9 are implemented. Unit tests mock Marten abstractions; integration tests use the shared Marten collection
  fixture on PostgreSQL (Testcontainers). There is no in-memory recorder.

TASK:
Write unit, guard, contract, property, integration tests and one benchmark; write the load-test justification; add
per-file coverage targets with justifications to the AIAct coverage manifest and measure every flag.

KEY RULES:
- Tests execute real package code; no reflection-only tests. Shouldly via Encina.Testing.Shouldly; no FluentAssertions.
- Integration: [Collection(MartenCollection.Name)], never a per-class fixture, never dispose the fixture.
- Deterministic time with FakeTimeProvider; no Thread.Sleep.
- Every changed src method CRAP <= 10, measured locally.
- Outputs under artifacts/ only.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
- tests/Encina.UnitTests/Compliance/AIAct/
- .github/coverage-manifest/Encina.Compliance.AIAct.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 11: Documentation & Finalization

> **Goal**: Documentation, changelog, public API and verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on all new public APIs (`<summary>`, `<remarks>` with the AI Act article, `<param>`, `<returns>`, `<example>`).
2. `changelog.d/842-aiact-record-keeping.added.md`.
3. `ROADMAP.md`: mark the v0.16.0 AI Act record-keeping item.
4. `src/Encina.Compliance.AIAct/README.md`: record-keeping section (registration, retention floor, export).
5. `docs/features/aiact-compliance.md`: Art. 12 section (what is recorded automatically, explicit API, retention and legal hold adapter, export schema, dates of Reg. (EU) 2026/1744), in the house style (`encina-docs` skill, docs-writer).
6. `docs/INVENTORY.md`: new files.
7. ADR: `docs/architecture/adr/041-aiact-record-keeping-log.md` (reserved) recording Design Choices 3, 5 and 6 (append-only per-day streams, fail-closed recording, period-key crypto-shredding).
8. `PublicAPI.Unshipped.txt` complete; `docs/releases/v0.16.0/` notes if the folder exists.
9. Build `dotnet build Encina.slnx --configuration Release` (0 errors, 0 warnings); `dotnet test`; every coverage flag reaches its manifest target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of issue #842 in Encina.Compliance.AIAct.

CONTEXT:
- Phases 1-10 are implemented and tested.

TASK:
Write the changelog fragment, README and feature-page sections, INVENTORY entry, the ADR for the log design, finish the
public API files, and verify build, tests and per-flag coverage.

KEY RULES:
- Never edit CHANGELOG.md [Unreleased]; add changelog.d/842-aiact-record-keeping.added.md.
- Documentation never types coverage figures by hand (covref markers, SPEC-001).
- English only; no AI attribution; docs pages go through the docs-writer agent.
- Zero warnings.

REFERENCE FILES:
- changelog.d/README.md
- docs/features/aiact-compliance.md
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/index.md
```

</details>

---

## Research

### Regulatory References

| Provision | Requirement | Where in this plan |
|-----------|-------------|--------------------|
| AI Act Art. 12(1) | High-risk systems technically allow automatic recording of events over their lifetime | Phase 4 (pipeline), Phase 3 (recorder) |
| AI Act Art. 12(2) | Logs enable identifying risk situations and substantial modifications, post-market monitoring (Art. 72), monitoring of operation (Art. 26(5)) | Event types `AnomalyDetected`, `ModelUpdated`, `ConfigurationChanged`, `ErrorOccurred` |
| AI Act Art. 12(3) | Remote biometric identification: period of each use, reference database, matched input, natural persons verifying results | `UsageStartedAtUtc`/`UsageEndedAtUtc`, `ReferenceDatabase`, `MatchedInputReference`, `VerifiedBy` |
| AI Act Art. 19(1) | Providers keep automatically generated logs at least six months | Validator floor, Phase 5 |
| AI Act Art. 26(6) | Deployers keep logs at least six months unless other law provides otherwise | Per-system retention override |
| AI Act Art. 21, 74 | Access to logs for competent authorities on reasoned request | Phase 6 exporter |
| AI Act Art. 43 | Conformity assessment | Phase 6 exporter; consumer is #844 |
| GDPR Art. 5(1)(e), 17(3)(b) | Storage limitation; erasure exemption for legal obligations | Design Choice 6 |
| Reg. (EU) 2026/1744 | Annex III high-risk from 2 Dec 2027, Annex I from 2 Aug 2028 | Feature page and XML docs |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AIActCompliancePipelineBehavior` | `src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs:54` | Hook for automatic recording |
| `AIActDiagnostics`, `AIActLogMessages` | `src/Encina.Compliance.AIAct/Diagnostics/` | Extended instruments; logging style |
| Marten raw append + projection | `src/Encina.Audit.Marten/MartenOperationAuditStore.cs:127`, `Projections/OperationAuditEntryProjection.cs` | Pattern for the append-only log |
| `IProjection<T>`, `IReadModel` | `src/Encina.Marten/Projections/` | Inline projection to `AISystemEventReadModel` |
| `[CryptoShredded]`, `ISubjectKeyProvider` | `src/Encina.Marten.GDPR/` | Period-key disposal |
| Retention background service | `src/Encina.Audit.Marten/MartenOperationAuditRetentionService.cs` | Pattern for the hosted service |
| `IOperationAuditStore`, `IReadAuditStore` | `src/Encina.Security.Audit/Abstractions/` | Audit of exports and disposals |
| `IAuditAttestationProvider` | `src/Encina.Compliance.Attestation/Abstractions/IAuditAttestationProvider.cs:26` | Optional export receipt |
| `IDistributedLockProvider` | `src/Encina.DistributedLock/IDistributedLockProvider.cs:77` | Single-instance retention cycle |
| `ILegalHoldService` | `src/Encina.Compliance.Retention/Abstractions/ILegalHoldService.cs:178` | Adapter target for `IAILogLegalHoldCheck` |
| `IRequestContext`, `IModuleExecutionContext` | `src/Encina/`, `src/Encina/Modules/Isolation/IModuleExecutionContext.cs:80` | Correlation, tenant, user, module |
| `IAISystemService`, aggregates (from #847) | `src/Encina.Compliance.AIAct/Services/` (after #847) | Registry and oversight events in exports |
| `MartenCollection` fixture | `tests/Encina.IntegrationTests/` | Integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.AIAct` (#415) | 9500-9512 | `AIActLogMessages.cs:25`-`157`, in use |
| `Encina.Compliance.AIAct` (#847) | 9513-9529 | #847 is built from Phase 1 to Phase 6 of its plan |
| **`Encina.Compliance.AIAct` (#842)** | **9530-9547** | **No new range; reserved per plan** |

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| Model, events, errors (Phase 1) | 7 new, 1 modified | `RecordKeeping/` |
| Abstractions and options (Phase 2) | 6 new | |
| Recorder, projection, stream ids (Phase 3) | 4 new | |
| Pipeline (Phase 4) | 1-2 modified/new | |
| Retention and hosted service (Phase 5) | 2 new | |
| Exporter (Phase 6) | 2 new | |
| DI, health, csproj (Phase 7) | 2 new, 1 modified | |
| Observability (Phase 9) | 1 new, 1 modified | |
| Tests (Phase 10) | ~22 new | 7 test projects, 1 `.md` justification |
| Documentation (Phase 11) | ~7 | changelog, README, feature page, INVENTORY, ADR, PublicAPI, ROADMAP |
| **Total** | **~55** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #842 — EU AI Act Art. 12 record-keeping and automatic logging — in Encina.Compliance.AIAct.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0: no backward compatibility, best solution always.
- Railway Oriented Programming: every operation returns Either<EncinaError, T>.
- Compliance modules use Marten event sourcing (ADR-019, ADR-027); event-sourced modules have no in-memory stores.
- #847 is merged first: the AIAct package already references Encina.Marten and has aggregates, read models and
  IAISystemService.

IMPLEMENTATION OVERVIEW:
Phase 1: RecordKeeping model (AIEventType, AISystemEvent, AISystemEventRecorded with crypto-shredded personal fields,
         RetentionPolicyResult, ConformityExportRequest, ExportedLogs, RecordingFailureMode) and AIActErrors codes.
Phase 2: IAIEventRecorder, IAILogRetentionPolicy, IConformityLogExporter, IAILogLegalHoldCheck, options + validator
         (retention >= 6 months, Art. 19(1)/26(6)).
Phase 3: MartenAIEventRecorder appending to one stream per system and UTC day (deterministic UUID), inline projection
         to AISystemEventReadModel keyed by EventId (idempotent).
Phase 4: AIActCompliancePipelineBehavior records UsageStarted (fail closed), UsageCompleted, ProhibitedUseAttempted,
         ComplianceCheckFailed, HumanOversightRequired.
Phase 5: CryptoShredAILogRetentionPolicy (destroy period keys older than the cutoff, legal hold defers, fail closed) and
         AILogRetentionHostedService (PeriodicTimer, scope per cycle, optional distributed lock).
Phase 6: JsonLinesConformityLogExporter (stream, SHA-256 manifest, optional attestation, registry/oversight events).
Phase 7: AddAIActRecordKeeping(), health check, startup check for crypto-shredding, csproj references.
Phase 8: TenantId/ModuleId propagation and filtering; audit of exports and disposals in IOperationAuditStore/IReadAuditStore.
Phase 9: Activities, counters, histogram; [LoggerMessage] EventIds 9530-9547 in ComplianceAIAct.
Phase 10: Unit, guard, contract, property, integration (MartenCollection), benchmark; load .md; coverage manifest targets.
Phase 11: XML docs, changelog fragment, README, feature page, INVENTORY, ADR, PublicAPI, verification.

KEY PATTERNS:
- TimeProvider only; async database calls with CancellationToken.
- Errors never swallowed; EncinaError.Message and exception messages never logged or tagged (ex.ForLogging()).
- Compliance gates fail closed; FailOpen is an explicit, logged opt-out.
- TryAdd registrations; DI completeness test with ValidateOnBuild and ValidateScopes.
- CRAP <= 10 on every changed method.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs
- src/Encina.Audit.Marten/MartenOperationAuditStore.cs
- src/Encina.Audit.Marten/MartenOperationAuditRetentionService.cs
- src/Encina.Marten.GDPR/Attributes/CryptoShreddedAttribute.cs
- src/Encina.Compliance.Consent/ConsentMartenExtensions.cs
- docs/plans/aiact-marten-es-migration-plan.md (#847)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ❌ N/A | Write-heavy append log; reads are rare range queries and exports, where stale cached ranges would be wrong |
| 2 | **OpenTelemetry** | ✅ Phase 9 | Activities for record, retention and export; counters and histogram on the existing `Encina.Compliance.AIAct` meter |
| 3 | **Structured Logging** | ✅ Phase 9 | `[LoggerMessage]` source generator, ~18 ids packed after #847's block in `ComplianceAIAct` |
| 4 | **Health Checks** | ✅ Phase 7 | `AIActRecordKeepingHealthCheck`: read-model reachability, completion failures, overdue disposal |
| 5 | **Validation** | ✅ Phases 2-3 | Options validator (retention floor); event validation in the recorder; range validation in the exporter |
| 6 | **Resilience** | ⏭️ Issue file `artifacts/issues/plan-842-aiact-marten-resilience.md` (to be opened) | Marten writes of AIAct (#847 aggregates and this log) have no retry or circuit breaker; #847 also defers it without an issue |
| 7 | **Distributed Locks** | ✅ Phase 5 | Retention cycle takes `IDistributedLockProvider` when registered; appends need none (append-only) |
| 8 | **Transactions** | ✅ Phases 3, 8 | Append and inline projection in one `SaveChangesAsync`; shred flag updated only after key deletion succeeds |
| 9 | **Idempotency** | ✅ Phase 3 | Duplicate `EventId` detected through the read model and treated as success; projection idempotent |
| 10 | **Multi-Tenancy** | ✅ Phase 8 | `TenantId` on events, stream and retention keys; queries and exports tenant-filtered; retention iterates tenants |
| 11 | **Module Isolation** | ✅ Phase 8 | `ModuleId` from `IModuleExecutionContext` on events and read models; module filter on queries |
| 12 | **Audit Trail** | ✅ Phase 8 | The log is the Art. 12 record; exports and disposals are audited in `IOperationAuditStore` and `IReadAuditStore` (ADR-036) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | State | Why |
|---|---|---|
| [#847](https://github.com/dlrivada/Encina/issues/847) AIAct to Marten event sourcing | Open | Marten references, `AddAIActAggregates()`, system identity, `IAISystemService` for exports |
| Comment on #847: drop `AISystemComplianceEvaluated` from `AISystemAggregate` (per-request outcomes go to this log) and reserve EventIds 9513-9529 | To post (orchestrator) | Avoids an unbounded registry stream and a sparse EventId allocation |
| `Encina.Marten.GDPR` crypto-shredding registered by the application | Available | Required by Design Choice 6; Phase 7 checks it at startup |

### Recommended (Not Blocking)

- [#839](https://github.com/dlrivada/Encina/issues/839) human oversight: its decision records add `HumanApprovalRecorded`/`HumanOverrideRecorded` events through `IAIEventRecorder`. Stale text "13 database providers" remains in `Abstractions/IHumanOversightEnforcer.cs:29` and `Model/HumanDecisionRecord.cs:17`; #847/#839 should correct it.
- [#844](https://github.com/dlrivada/Encina/issues/844) conformity assessment: consumes `IConformityLogExporter`.
- [#845](https://github.com/dlrivada/Encina/issues/845), [#846](https://github.com/dlrivada/Encina/issues/846): tenant and module scoping of the rest of AIAct; this plan stamps both on the log.
- [#1204](https://github.com/dlrivada/Encina/issues/1204) (AIAct integration tests) and [#1205](https://github.com/dlrivada/Encina/issues/1205) (AIAct article-coverage specification): Art. 12 rows come from this plan.

---

## Next Steps

1. The maintainer decides Design Choices 1-8; the orchestrator records the decisions in this plan.
2. Post the #847 comment listed under Required Prerequisites; open the resilience issue from its file.
3. Update #842's acceptance criteria to the decided scope (in-memory default, exporter signature, `IOperationAuditStore`).
4. Implement after #847 merges, one phase per commit, final PR with `Fixes #842`.
