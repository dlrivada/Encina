# Implementation Plan: `Encina.Compliance.BreachNotification` — Art. 33(3) Package and Art. 34 Communications

> **Issue**: [#1242](https://github.com/dlrivada/Encina/issues/1242) (SPEC-002 tracking id **P-40**, priority **P0**)
> **Type**: Feature
> **Complexity**: High (9 phases, event-sourced on Marten, one new satellite package, ~50 distinct files, 10 pull requests)
> **Estimated Scope**: ~1,400-1,900 lines of production code + ~2,000-2,600 lines of tests
> **Milestone**: v0.17.0 — Compliance Lifecycle
> **Parent EPIC**: [#1186](https://github.com/dlrivada/Encina/issues/1186) — EU regulatory readiness (SPEC-002)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-049, AC-039, scenario S18, REQ-024 (current law is the default); cross-cutting REQ-061 (AC-043) and REQ-062 (AC-044)
> **Depends on**: [#812](https://github.com/dlrivada/Encina/issues/812) (reporting layer: `IncidentReport`, `IIncidentReportContributor`, serializers; plan [enisa-single-entry-point-implementation-plan-812.md](enisa-single-entry-point-implementation-plan-812.md)) and [#1235](https://github.com/dlrivada/Encina/issues/1235) (P-33, `Encina.Notifications`; plan [notifications-implementation-plan-1235.md](notifications-implementation-plan-1235.md))
> **Related**: [#813](https://github.com/dlrivada/Encina/issues/813) (authority notification threshold, Omnibus, off by default; uses EventIds 8792-8794), [#1942](https://github.com/dlrivada/Encina/issues/1942) (ENISA adapter)

---

## Summary

A controller must notify the supervisory authority within 72 hours with the content of GDPR Art. 33(3), and may have to communicate the breach to the data subjects (Art. 34). The decision whether and when stays with the organisation (SPEC-002 section 2.3). The module can record those steps, but it cannot produce the content or deliver the communication. Today:

- `BreachAggregate` and `BreachReadModel` (event-sourced on Marten) hold nature, severity, estimated affected subjects, description, assessment, authority name, containment measures and phased reports. They hold **none of the Art. 33(3) content**: no categories of data and approximate number of records, no DPO contact, no likely consequences (the old `BreachRecord` model has these fields but nothing persists it).
- `IBreachNotifier` (`Abstractions/IBreachNotifier.cs`) has one implementation, `DefaultBreachNotifier`, which logs a warning and returns `NotificationOutcome.Sent` for a notification that is never delivered. **No production code calls it**; only the health check resolves it. This reports a success that did not happen.
- `IBreachNotificationService.NotifySubjectsAsync` records only a count, a free-text method and an exemption. It tracks no individual communication (channel, template, when, how many were queued or failed).
- Plan #812 (reporting layer) replaces the authority half of `IBreachNotifier` and leaves "the Art. 34 subject path for #1242"; it also names the seam this plan uses for the Art. 33(3) content: `IIncidentReportContributor`.
- Plan #1235 (`Encina.Notifications`) provides `INotificationSender` (consent gate, deduplication, outbox, delivery status) for user-facing delivery.

This plan delivers:

1. **Art. 33(3) content captured on the breach stream** (new event and read-model fields) and a **package builder** that produces the Art. 33(3) package as JSON and as a readable Markdown document, flags a submission after the deadline that has no reason for the delay (Art. 33(1)), and records each generation as evidence (Art. 33(5)).
2. **Authority path**: an `IIncidentReportContributor` that fills the Art. 33(3) fields of #812's `IncidentReport`, so the file-export adapter of #812 (manual submission) and a future ENISA adapter (#812, #1942) carry the same content. Automated submission to the AEPD is out of scope (no public API).
3. **Subject path (Art. 34)**: `IBreachNotifier` reshaped to send a communication through a channel, a service method `CommunicateToSubjectsAsync` that sends and records one `BreachSubjectCommunicationDispatched` event per communication, and a satellite package `Encina.Compliance.BreachNotification.Notifications` that delivers through `Encina.Notifications`. Individual recipients and their delivery status stay in the notification delivery store, not in the event stream.
4. The no-op `DefaultBreachNotifier` is removed; with no channel registered, `CommunicateToSubjectsAsync` returns a coded `Left` instead of reporting success.

**Standards**: GDPR Art. 33(1), (3), (4), (5); Art. 34(1)-(3); Digital Omnibus COM(2025) 837 (draft, off by default: 96 hours and the single entry point, #813 and #812); SPEC-002 REQ-024, REQ-049.

**Affected packages**: `Encina.Compliance.BreachNotification`; new `Encina.Compliance.BreachNotification.Notifications`; `Encina` (core: one EventId range). `Encina.Notifications` (P-33) is consumed, and may need one query on its delivery store (see Prerequisites).

**Provider category**: none of the database matrix. The module is event-sourced on Marten (PostgreSQL), which SPEC-002 DEC-008 (a) and ADR-019 keep Marten-only in 1.0; there are no InMemory stores, unit tests mock `IAggregateRepository` and integration tests run against Marten on PostgreSQL through Testcontainers (AGENTS.md section 3).

**Size**: about 50 distinct files including tests, manifests and PublicAPI (shared files such as `PublicAPI.Unshipped.txt` and the manifest are touched by several pull requests, so the per-PR counts below add up to more), so ten worker pull requests (1, 2, 3, 4a, 4b, 4c, 4d, 5, 6, 7), each building alone with its own tests, manifest entries and PublicAPI lines and each within 15 files and 1,500 lines counting tests, manifests, PublicAPI and `ProjectReference` edits. Nothing in a PR may depend on a package that is not on main when it opens, so the order below is binding and the two external dependencies are hard prerequisites of the PRs that name them:

| PR | Content | Hard prerequisite |
|----|---------|-------------------|
| 1 | Art. 33(3) content on the stream: `Art33Content`, `BreachArt33ContentRecorded`, aggregate method, read-model/projection fields, `breach.delay_reason_required` and `breach.article33_content_incomplete`; unit and guard tests (about 12 files) | none |
| 2 | `RecordArticle33ContentAsync` on `IBreachNotificationService`, `IBreachPackageDocumentWriter` and `MarkdownBreachPackageWriter` (a pure renderer over the read model), range `ComplianceBreachChannels`, log class, diagnostics, test-map entry; unit tests (about 13 files) | PR 1 |
| 3 | Art. 34 on the stream: `BreachSubjectCommunication` (with a required `CommunicationId`), `SubjectRecipient`, `SubjectCommunicationResult`, `BreachSubjectCommunicationDispatched`, aggregate method with idempotent duplicate handling, read model/projection, errors; unit and guard tests (about 13 files) | PR 1 |
| 4a | Reshape and removal of the notifier chain: `IBreachNotifier` reshaped (and `NotifyAuthorityAsync` removed), `DefaultBreachNotifier`, `NotificationResult` and `NotificationOutcome` removed, default registration removed from `ServiceCollectionExtensions`, `PublicAPI.Unshipped.txt`, manifest. Count from origin/main (`git grep` of the removed symbols): 7 source files (3 deleted, `IBreachNotifier.cs`, `ServiceCollectionExtensions.cs`, PublicAPI, manifest) plus 5 deleted tests (`DefaultBreachNotifierContractTests`, `DefaultBreachNotifierGuardTests`, `DefaultBreachNotifierMethodGuardTests`, `DefaultBreachNotifierTests`, `NotificationResultPropertyTests`) plus 3 edited tests (`IBreachNotifierContractTests`, `ServiceCollectionExtensionsTests` with the new DI tests, `BreachModelFactoryGuardTests`, which also drops its `BreachRecord` references): 15 files | PR 2, PR 3 |
| 4b | Health check and options cleanup: `BreachNotificationHealthCheck` (`Degraded` without a subject channel), removal of the dead `SupervisoryAuthority` and `AutoNotifyOnHighSeverity` from `BreachNotificationOptions`, README, `PublicAPI.Unshipped.txt`, manifest; 3 edited tests (`BreachNotificationHealthCheckTests`, `BreachNotificationOptionsTests`, `BreachNotificationAdvancedIntegrationTests`) and the health check guard test: about 10 files. The options do not depend on the removed notifier (no source reference), so this PR builds on PR 4a alone | PR 4a |
| 4c | Removal of the dead types: `Model/BreachRecord.cs`, `Notifications/AuthorityNotifiedNotification.cs`, `Notifications/SubjectsNotifiedNotification.cs`, the two `cref` mentions in `PotentialBreach.cs` and `BreachDetectedNotification.cs`, README, `PublicAPI.Unshipped.txt`, manifest, and the deleted `BreachRecordTests` and `BreachRecordPropertyTests`: 11 files. Every reference left after PR 4a is verified by `git grep` before the deletion | PR 4a |
| 4d | `CommunicateToSubjectsAsync` / `GetCommunicationsAsync` on `IBreachNotificationService` and `DefaultBreachNotificationService`, `ISubjectDeliveryStatusReader`, `PublicAPI.Unshipped.txt`, manifest; unit, failure-injection and guard tests (11 files) | PR 4a |
| 5 | Package service, `BreachAuthorityPackageGenerated`, `Art33IncidentReportContributor` over #812's model, JSON through #812's serializer; unit tests (about 12 files) | PR 2 and #812's reporting-layer pull requests merged |
| 6 | Satellite `Encina.Compliance.BreachNotification.Notifications` over `INotificationSender`, `ISubjectDeliveryStatusReader` implementation over the delivery-store query, DI; unit, guard and contract tests, manifest, README (about 14 files) | PR 4d and #1235's pull requests merged, including the query by correlation key (to be added to the #1235 plan first) |
| 7 | Marten integration tests (S18, two tenants, restart), contract snapshots of the package, load/benchmark justifications, documentation, changelog (about 10 files) | PR 5, PR 6 |

---

## Design Choices

<details>
<summary><strong>1. Where the Art. 33(3) Content Lives — recorded on the breach stream</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new event `BreachArt33ContentRecorded` (categories of data, approximate number of records, DPO contact, likely consequences, measures taken or proposed, optional delay reason) with matching `BreachReadModel` fields and a service method `RecordArticle33ContentAsync`; the package is built from the read model** | Art. 33(5) requires documenting facts, effects and remedial action: the stream is that record; the package is reproducible from history; versions of the content are kept | The content (DPO contact, consequences) is stored in the event stream |
| **B) Pass the content to the package builder on each call and store nothing** | No new events | The organisation cannot later show what content it submitted; the package differs between calls |
| **C) Extend `BreachDetected` and `BreachAssessed` with the new fields** | Fewer event types | The content is usually unknown at detection; a late field forces a fake event; the 72-hour clock needs the content to be updatable |

### Chosen Option: **A — a dedicated event and read-model fields**

### Rationale

- The content is separate from `Assess` because it is gathered in phases (Art. 33(4) allows information in phases) and each update is a new event with its author and time.
- Content is written by the organisation. It holds no data of the affected subjects (counts and category names only), but `DpoContact` is personal data of a natural person (the DPO) and `LikelyConsequences` is free text that could carry names. Decision registered here: the event stream stores them in clear text, because Art. 33(3)(b) requires the notification to name the contact point; the validator caps `DpoContact` at 256 characters and `LikelyConsequences` and `MeasuresTakenOrProposed` at 4,000 characters each, and the documentation tells the organisation not to write the names of affected persons into the free-text fields.
- A submission after `DeadlineUtc` without a delay reason returns `Left(breach.delay_reason_required)` (Art. 33(1)); the reason is part of the content event.
- The deadline is the aggregate's `DeadlineUtc` (72 hours by default, `BreachNotificationOptions.NotificationDeadlineHours`); the Omnibus 96 hours stays #813 and off by default.

**Class:** Trivial
**Settled by:** Existing convention: a phase of the breach lifecycle is its own event (`BreachAssessed` precedent in `Events/BreachNotificationEvents.cs`); GDPR Art. 33(4) (information in phases) and 33(5); plan #812 (current state: "the event-sourced `BreachAggregate` and `BreachReadModel` do not [carry the Art. 33(3) fields]. #1242 adds them"). Option C (extend `BreachDetected`/`BreachAssessed`) would also satisfy Art. 33(5) but cannot be updated in phases, so it is not a rule-based alternative.

</details>

<details>
<summary><strong>2. Authority Path — a contributor on the #812 reporting layer</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Art33IncidentReportContributor : IIncidentReportContributor` fills the Art. 33(3) fields of #812's `IncidentReport`; `IBreachPackageService` builds the package with `IIncidentReportBuilder` and the serializers of #812; the file-export adapter of #812 is the manual-submission channel** | One report model for the file export, the HTTP adapter and the future ENISA adapter; no second authority abstraction; matches both plans | #1242's authority half cannot be completed before #812's model exists |
| **B) A standalone package builder with its own record, independent of #812** | Deliverable alone, in any order | Two report models that must be kept in step; the adapters of #812 would not see the Art. 33(3) content; duplicated serialization |
| **C) Merge the authority half into the #812 pull requests** | One implementation | Grows #812 beyond its size limit; mixes two issues |

### Chosen Option: **A — contributor on the #812 reporting layer (recommended, pending the maintainer)**

### Rationale

- Scenario in which A wins: #812 lands first or in the same release train, which the shared milestone and the plans' mutual references assume; the adapter contract stays fixed (#812 plan, Design Choice 2: "Neither PR waits on the other").
- Scenario in which B wins: #812 slips well beyond #1242 and the organisation needs the Art. 33(3) package for the 72-hour deadline of the first real breach; then a standalone builder avoids the wait, at the price of reconciling two models later.
- Recommendation A: pre-1.0 we do not keep two models for one report. The Art. 33(3) content capture (Design Choice 1) and the Markdown document do not depend on #812 and can be implemented first (PR 1); only the contributor and the use of the shared serializer wait.

**Class:** Dilemma
**Settled by:** none; awaits the maintainer

</details>

<details>
<summary><strong>3. Package Formats — JSON (shared serializer) and a Markdown document</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) JSON through #812's `JsonIncidentReportSerializer` plus a Markdown renderer behind a small `IBreachPackageDocumentWriter`** | Machine-readable and human-readable with no new dependency; the writer abstraction allows another format | Markdown must be pasted or converted when a PDF is needed |
| **B) JSON, Markdown and PDF** | PDF is what some organisations attach | A PDF library dependency for a document the AEPD portal does not accept as an upload |
| **C) JSON only** | Smallest | The issue asks for a readable document; a clinician cannot read the JSON |

### Chosen Option: **A — JSON and Markdown behind a writer abstraction**

### Rationale

- The Markdown document lists the Art. 33(3)(a)-(d) items in the order of the article, then the phased reports (Art. 33(4)) and the delay reason when applicable.
- `Art33Package` carries `ReportId` (deterministic, from #812), the JSON bytes, the Markdown text, `GeneratedAtUtc` and `IsLate`.
- Each generation records `BreachAuthorityPackageGenerated(breachId, reportId, contentHash, generatedAtUtc, generatedByUserId)`: the hash lets the organisation show that the package it submitted matches the recorded content, without storing the package itself.

**Class:** Trivial
**Settled by:** Existing convention: `IExportFormatWriter` of `Encina.Compliance.DataSubjectRights` (a writer abstraction with JSON and other formats); plan #812 (`JsonIncidentReportSerializer`, format `encina-incident-report-v1`)

</details>

<details>
<summary><strong>4. Art. 34 Tracking — one dispatch event per communication; recipients stay in the delivery store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `BreachSubjectCommunicationDispatched` event per communication: `CommunicationId`, channel, `TemplateId`, recipient count, queued count, failed count, dispatched time, user; per-recipient state lives in `INotificationDeliveryStore` under a correlation key `breach:{breachId}:{communicationId}`** | The stream holds no per-subject data (append-only stream, crypto-shredding concerns); proves what was communicated and when; delivery status is read from the store | Per-recipient status needs the delivery store (a query by correlation key) |
| **B) One event per recipient** | Complete in the stream | Thousands of events with subject references in an append-only stream; erasure of a subject would touch the breach record |
| **C) Only the existing count (`NotifySubjectsAsync`)** | Nothing new | Cannot prove which template went on which channel, or that every recipient was reached |

### Chosen Option: **A — one dispatch event per communication**

### Rationale

- Recipients are given as opaque `RecipientRef` values (as in #1235's `NotificationRequest`); no address, name or message body is stored by this module or logged.
- Idempotency: `CommunicationId` is a required field of `BreachSubjectCommunication`, supplied by the caller (the organisation's operation decides one id per intended communication and reuses it on every retry); the service never generates a new id on a retry. The deduplication key of each notification is `breach:{breachId}:{communicationId}:{recipientRef}`. The order is: validate, send with the keys, record the event. A retry after a crash between send and record re-sends with the same keys, #1235's store returns the existing rows for recipients already queued, so nobody is contacted twice, and the event is then written. A retry after the event was saved and before the response reaches the caller sends nothing new (all keys exist) and the aggregate refuses a second `BreachSubjectCommunicationDispatched` with the same `CommunicationId` by returning the existing record as `Right` (decided before `RaiseEvent`, ADR-053), so the stream holds one event per `CommunicationId`.
- Retrying failed recipients: the same `CommunicationId` is used; recipients with an existing delivery row are deduplicated and only recipients without a row (or whose row #1235 marks retryable) are sent. The dispatch event is not rewritten; `GetCommunicationsAsync` reads live queued, deduplicated and failed counts from the delivery store, so the current state is visible without a second event.
- Total failure: when the notifier returns `Left` (nothing could be attempted), or a result with zero queued recipients, no event is appended and the service returns `Left(breach.communication_failed)`; a partial result appends the event with its counts and returns `Right`. Both outcomes are tested.
- `GetCommunicationsAsync(breachId)` returns the dispatch records joined with delivery-status counts from the store.
- The existing `NotifySubjectsAsync` stays for communications made outside Encina and for recording an Art. 34(3) exemption; it is not removed.

**Class:** Settled
**Settled by:** GDPR Art. 5(1)(c) (data minimisation) and Art. 34(1)-(3); ADR-019 (event-sourced compliance modules, Marten; crypto-shredding of personal data in streams); AGENTS.md section 3 ("Errors are NEVER swallowed ...") and section 6 (idempotency); plan #1235 (deduplication key and delivery store)

</details>

<details>
<summary><strong>5. Package Placement of the Subject Channel — a satellite package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.BreachNotification.Notifications` that references both `Encina.Compliance.BreachNotification` and `Encina.Notifications` and implements `IBreachNotifier` with `INotificationSender`** | Applications that never communicate through Encina do not pull `Encina.Notifications`; the core keeps only the abstraction | One more package |
| **B) Implement it inside `Encina.Compliance.BreachNotification`** | No new package | Every breach user gets a dependency on `Encina.Notifications` and, through it, the consent gate and delivery store |
| **C) Put the adapter inside `Encina.Notifications`** | One package | Reverses the dependency: the messaging package would know the compliance module |

### Chosen Option: **A — satellite package (recommended, pending the maintainer)**

### Rationale

- Scenario in which A wins: an application that records breaches but delivers its communications with its own channel code and never uses `Encina.Notifications`.
- Scenario in which C wins: the maintainer prefers one adapter inside `Encina.Notifications` (every consumer of notifications already has the package) and accepts that the messaging package then references the compliance module.
- Scenario in which B wins: the maintainer treats `Encina.Notifications` as part of every breach deployment and wants no extra package.
- Recommendation A: pay-for-what-you-use (AGENTS.md section 1) and provider coherence (abstractions in the module, implementations in satellites, section 3) keep both dependency directions clean.
- The satellite registers `IBreachNotifier` before `AddEncinaBreachNotification` (satellite DI called before the core, with `TryAdd`).
- Breach communications are `NotificationClass.Transactional` (legal obligation, no marketing consent), with the purpose string `breach-communication`; they never use the commercial basis.

**Class:** Dilemma
**Settled by:** none; awaits the maintainer (AGENTS.md section 1 and 3 and plan #1235 are consistent with A, but option C also satisfies them)

</details>

<details>
<summary><strong>6. Fate of <code>IBreachNotifier</code> and its No-Op Default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reshape `IBreachNotifier` to the subject path only (`NotifyDataSubjectsAsync(BreachSubjectCommunication, ct)` returning `Either<EncinaError, SubjectCommunicationResult>`); remove `DefaultBreachNotifier`, `BreachRecord` use and the authority half `NotifyAuthorityAsync` (owner: PR 4a of this plan, so the PR builds alone whatever the order with #812; the #812 plan drops its line that removes it, to be agreed through the sibling-plan link); with no notifier registered `CommunicateToSubjectsAsync` returns `Left(breach.subject_channel_not_configured)`** | No code that reports success without delivering; one honest contract | Breaking change (pre-1.0, acceptable) |
| **B) Keep `DefaultBreachNotifier` as a logging fallback** | Works with no configuration | It reports `Sent` for nothing: the failure AGENTS.md section 3 forbids |
| **C) Keep the interface as is and add a second subject abstraction** | No breaking change | Two subject abstractions, one dead |

### Chosen Option: **A — reshape and remove the no-op**

### Rationale

- The health check `BreachNotificationHealthCheck` resolves `IBreachNotifier`; it reports `Degraded` (not `Unhealthy`) when no subject channel is registered, because communicating through Encina is optional.
- `NotificationResult` and `NotificationOutcome` are replaced by `SubjectCommunicationResult` (queued, failed, deduplicated counts and error codes only); `BreachRecord` and the unused `Notifications/*` published types are removed when no reference remains after this plan and #812 (checked at implementation, no leftovers).
- Pre-1.0: no compatibility layer (AGENTS.md sections 1 and 3).

**Class:** Settled
**Settled by:** AGENTS.md section 1 ("no backward compatibility"), section 3 ("NEVER add ... legacy code, compatibility aliases", "Errors are NEVER swallowed ... never reports success"); plan #812 (it removes the authority half and "keeps the Art. 34 subject path for #1242")

</details>

---

## Implementation Phases

### Phase 1: Core Models, Events and Errors

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Compliance.BreachNotification/`

The models and events of this phase land in the PR that needs them (the Summary table): Art. 33(3) content in PR 1, communications in PR 3, the package in PR 2 and PR 5.

1. **`Model/Art33Content.cs`** (new, PR 1) — `CategoriesOfDataAffected` (list), `ApproximateRecordsAffected` (int?), `DpoContact` (max 256), `LikelyConsequences` and `MeasuresTakenOrProposed` (max 4,000 each), `DelayReason?`
2. **`Model/Art33Package.cs`** (new, PR 5) — `ReportId`, `Json` (bytes), `Markdown`, `GeneratedAtUtc`, `IsLate`
3. **`Model/BreachSubjectCommunication.cs`, `Model/SubjectRecipient.cs`, `Model/SubjectCommunicationResult.cs`** (new, PR 3) — communication request (breach id, **required `CommunicationId`**, channel, template id, parameters, recipients as opaque refs; **no tenant field**: the tenant comes from the breach stream, `BreachReadModel.TenantId`, never from a model the caller builds, and a communication addressed to another tenant's breach id returns not found) and result (queued, failed, deduplicated counts, error codes)
4. **`Events/BreachNotificationEvents.cs`** (modify) — `BreachArt33ContentRecorded` (PR 1), `BreachSubjectCommunicationDispatched` (PR 3), `BreachAuthorityPackageGenerated` (PR 5); each carries `TenantId` (`string?`, null when tenancy is off, as the existing events) and `ModuleId` like the existing events
5. **`Aggregates/BreachAggregate.cs`** (modify) — `RecordArticle33Content`, `RecordSubjectCommunication`, `RecordAuthorityPackageGenerated`; state guards (not closed; content only after detection); they return `Either` and never throw for an illegal transition (ADR-053); `RecordSubjectCommunication` with an already recorded `CommunicationId` returns the existing record as `Right` without raising an event
6. **`ReadModels/BreachReadModel.cs`, `ReadModels/BreachProjection.cs`** (modify) — Art. 33(3) fields, package generation hash and time, list of communication summaries
7. **`BreachNotificationErrors.cs`** (modify) — every code is created with `EncinaErrors.Create(code, category, ...)` (ADR-062; the completeness scan fails an uncategorized call), details are codes and counts, never recipients:

   | Code | `ErrorCategory` | Rule |
   |------|-----------------|------|
   | `breach.delay_reason_required` | `Validation` | R3: the input lacks a required reason |
   | `breach.article33_content_incomplete` | `Validation` | R3: `FailedPrecondition` is not used before #3420, so the input-shaped category is chosen |
   | `breach.subject_channel_not_configured` | `Internal` | R1/R4: wiring is missing, not the caller's mistake |
   | `breach.communication_invalid` | `Validation` | R3 |
   | `breach.communication_failed` | `UpstreamFailure` | R7: the channel could not queue any recipient |

   A `GetCategory` test asserts each category.
8. **`PublicAPI.Unshipped.txt`** (modify)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #1242 (SPEC-002 P-40). Package src/Encina.Compliance.BreachNotification is event-sourced on Marten
  (BreachAggregate, BreachReadModel, BreachProjection, events in Events/BreachNotificationEvents.cs). The aggregate
  holds no Art. 33(3) content; BreachRecord (Model/) is an unused older model.
- Pre-1.0: no [Obsolete], no compatibility layers.

TASK:
Add the models, the three events, the aggregate methods, the read-model/projection fields and the new error codes
listed in the Phase 1 tasks, with XML docs and PublicAPI entries.

KEY RULES:
- Events are immutable records carrying TenantId and ModuleId like the existing ones.
- No recipient address, message body or personal data of affected subjects in events or errors.
- The aggregate methods take the timestamp as a parameter; the service supplies it from `TimeProvider`; never DateTime.UtcNow in the aggregate.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs
- src/Encina.Compliance.BreachNotification/Events/BreachNotificationEvents.cs
- src/Encina.Compliance.BreachNotification/ReadModels/BreachProjection.cs
- src/Encina.Compliance.BreachNotification/BreachNotificationErrors.cs
```

</details>

---

### Phase 2: Package Builder, Contributor and Document Writer

<details>
<summary><strong>Tasks</strong></summary>

1. **`Abstractions/IBreachPackageService.cs`** (new, PR 5) — `BuildAuthorityPackageAsync(Guid breachId, string requestedByUserId, CancellationToken)` returning `Either<EncinaError, Art33Package>`. `RecordArticle33ContentAsync(Guid breachId, Art33Content content, string recordedByUserId, CancellationToken)` lives on the existing `IBreachNotificationService` (PR 2), so recording content does not wait for #812
2. **`Reporting/Art33IncidentReportContributor.cs`** (new, implements #812's `IIncidentReportContributor`, `Order` after the base mapping) — fills `CategoriesOfDataAffected`, `DpoContact`, `LikelyConsequences`, `MeasuresTaken` and `DelayReason` from the read model; returns `Left(breach.article33_content_incomplete)` when the content is missing and `Reporting.RequireArticle33Content` is true
3. **`Services/DefaultBreachPackageService.cs`** (new) — loads the read model, rejects a late package without a delay reason, builds the report with `IIncidentReportBuilder`, serializes JSON, renders Markdown, appends `BreachAuthorityPackageGenerated` with the content hash (SHA-256 over the canonical JSON)
4. **`Abstractions/IBreachPackageDocumentWriter.cs`, `Reporting/MarkdownBreachPackageWriter.cs`** (new) — renders the Art. 33(3)(a)-(d) items, phased reports and delay reason
5. Ordering (Design Choice 2): the Markdown writer (item 4) ships in PR 2 as a renderer over the read model, with no dependency on #812 and no throwaway shape; items 1, 2 and 3 ship together in PR 5 only after #812's reporting-layer pull requests are merged, so no interim report model is written and later deleted

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Plan #812 defines IncidentReport, IIncidentReportBuilder, IIncidentReportContributor (int Order;
  ValueTask<Either<EncinaError, IncidentReport>> ContributeAsync(IncidentReport, BreachReadModel, CancellationToken)),
  JsonIncidentReportSerializer (format "encina-incident-report-v1") and the file-export adapter.
- IBreachNotificationService returns BreachReadModel; the service pattern is DefaultBreachNotificationService
  (load aggregate, mutate with a TimeProvider timestamp, SaveAsync, invalidate cache).

TASK:
Implement the package service, the contributor and the Markdown writer as listed in Phase 2.

KEY RULES:
- ROP: Either everywhere; a late package without a delay reason is Left(breach.delay_reason_required).
- The package is reproducible from the recorded content; only its hash is stored.
- Markdown follows the article order 33(3)(a)-(d); no payload or subject identifiers in logs or tags.
- Deadline: aggregate DeadlineUtc (72 h default); the 96 h Omnibus value stays #813 and off by default.
- CRAP <= 10 per method: split validation, rendering and hashing.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs
- src/Encina.Compliance.DataSubjectRights/Abstractions/IExportFormatWriter.cs
- docs/plans/enisa-single-entry-point-implementation-plan-812.md
```

</details>

---

### Phase 3: Art. 34 Communications — Notifier and Service

<details>
<summary><strong>Tasks</strong></summary>

1. **`Abstractions/IBreachNotifier.cs`** (modify) — only `ValueTask<Either<EncinaError, SubjectCommunicationResult>> NotifyDataSubjectsAsync(BreachSubjectCommunication communication, CancellationToken ct)`; `NotifyAuthorityAsync` is removed in the same PR (PR 4a), which is why that PR builds whether or not #812 has merged
2. **`DefaultBreachNotifier.cs`, `Model/NotificationResult.cs`, `Model/NotificationOutcome.cs`** — removed in PR 4a (Design Choice 6); `BreachRecord`, `Notifications/AuthorityNotifiedNotification.cs`, `Notifications/SubjectsNotifiedNotification.cs` removed in PR 4c (`BreachRecord` is no longer referenced once PR 4a has reshaped `IBreachNotifier`; verified by `git grep` before the deletion, no leftovers)
3. **`Abstractions/IBreachNotificationService.cs`** (modify) — `CommunicateToSubjectsAsync(Guid breachId, BreachSubjectCommunication communication, string communicatedByUserId, CancellationToken)` and `GetCommunicationsAsync(Guid breachId, CancellationToken)`; the live delivery counts come from an optional core abstraction `Abstractions/ISubjectDeliveryStatusReader.cs` (new, PR 4d) that the satellite implements in PR 6; with no reader registered the dispatch records are returned with the event counts only, so PR 4d builds without `Encina.Notifications`
4. **`Services/DefaultBreachNotificationService.cs`** (modify) — flow: load breach (must be assessed, not closed) -> validate (recipients present, template id, channel known) -> `IBreachNotifier` (resolved optionally: `Left(breach.subject_channel_not_configured)`) with the caller's `CommunicationId` -> a `Left` from the notifier, or a result with zero queued recipients, appends nothing and returns `Left(breach.communication_failed)` (the failure counter records it) -> a partial or full success appends `BreachSubjectCommunicationDispatched` with its counts and returns `Right` (partially failed batches show their counts); the tenant is taken from the breach read model, and a breach id of another tenant returns not found. Failure-injection tests cover a crash between send and record and one after record (the retry sends nothing new and the stream holds one event)
5. **Exemptions** — `exemption != None` (Art. 34(3)) is recorded through the existing `NotifySubjectsAsync` path and sends nothing

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Today IBreachNotifier has NotifyAuthorityAsync and NotifyDataSubjectsAsync(BreachRecord, IEnumerable<string>) and one
  no-op implementation reporting Sent. Nothing but the health check calls it.
- #1235's INotificationSender takes NotificationRequest(Channel, RecipientRef, TemplateId, Class, Purpose, DedupKey,
  Parameters, Basis?) and returns Either<EncinaError, NotificationDelivery>.

TASK:
Reshape the notifier, remove the no-op, and add CommunicateToSubjectsAsync / GetCommunicationsAsync as in Phase 3.

KEY RULES:
- Never report success without delivery: no default notifier; missing channel is a coded Left.
- Send first with a deterministic dedup key, record second; a retry must not contact anybody twice.
- A Left from the notifier or the store fails the operation (no swallowing); partial results carry counts.
- No recipient references, template parameters or addresses in logs, events, tags or errors.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Abstractions/IBreachNotifier.cs
- src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs
- docs/plans/notifications-implementation-plan-1235.md
```

</details>

---

### Phase 4: Satellite Package `Encina.Compliance.BreachNotification.Notifications`

<details>
<summary><strong>Tasks</strong></summary>

1. **`Encina.Compliance.BreachNotification.Notifications.csproj`** (new) — references `Encina.Compliance.BreachNotification`, `Encina.Notifications`, `Encina`; PublicApiAnalyzers, `PublicAPI.Shipped.txt`/`Unshipped.txt`, README; added to `Encina.slnx`; coverage manifest created by `mechanical-fixer`
2. **`NotificationsBreachNotifier.cs`** (new, `IBreachNotifier`) — maps each recipient to a `NotificationRequest` (`Class = Transactional`, purpose `breach-communication`, dedup key `breach:{breachId}:{communicationId}:{recipientRef}`, correlation key `breach:{breachId}:{communicationId}`) and calls `INotificationSender`; counts queued, deduplicated and failed
3. **`ServiceCollectionExtensions.AddEncinaBreachNotificationNotifications()`** (new) — `TryAddScoped<IBreachNotifier, NotificationsBreachNotifier>()` (scoped because `INotificationSender` is scoped; the former singleton `DefaultBreachNotifier` registration is gone, nothing injects the notifier into a singleton, and `BreachNotificationHealthCheck` already resolves it from a created scope, `scopedProvider.GetService<IBreachNotifier>()`; the DI test with `ValidateScopes` proves it); called before `AddEncinaBreachNotification`
4. **Delivery-status reader** — `NotificationsSubjectDeliveryStatusReader : ISubjectDeliveryStatusReader` reads counts through a query on `INotificationDeliveryStore` by correlation key. Hard prerequisite on #1235: the query must be added to that plan and merged first; PR 6 does not open before it (see Prerequisites)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- Satellite pattern: a package per integration that references the module and the dependency (see
  src/Encina.Compliance.Attestation for a small compliance package layout and src/Encina.Marten.GDPR for DI).
- #1235 plan: INotificationSender, INotificationDeliveryStore (TryCreateAsync, UpdateStatusAsync, GetAsync),
  NotificationClass.Transactional.

TASK:
Create the satellite package, NotificationsBreachNotifier and its registration as in Phase 4.

KEY RULES:
- Satellite DI uses TryAdd and is called BEFORE the core registration.
- Transactional class only; no commercial basis; no marketing consent involved.
- No recipient reference, parameter value or provider response in logs, tags or errors.

REFERENCE FILES:
- src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs
- docs/plans/notifications-implementation-plan-1235.md
```

</details>

---

### Phase 5: Configuration, DI and Health

<details>
<summary><strong>Tasks</strong></summary>

1. **`ServiceCollectionExtensions.AddEncinaBreachNotification`** (modify) — register `IBreachPackageDocumentWriter` (PR 2), remove the `IBreachNotifier` default registration (PR 4a; the health check follows in PR 4b), register `IBreachPackageService` and `Art33IncidentReportContributor` (PR 5, after #812)
2. **`BreachNotificationOptions`** (modify, PR 4b) — remove the dead `SupervisoryAuthority` and `AutoNotifyOnHighSeverity`. Owner: this plan's PR 4b (the first pull request that touches them in this batch); the #812 plan drops its line that removes them, to be agreed through the sibling-plan link, so both PRs never edit the same lines. `SubjectNotificationSeverityThreshold` stays and is documented as advisory (the decision to communicate is the organisation's)
3. **`Health/BreachNotificationHealthCheck.cs`** (modify) — no registered subject channel is `Degraded`, not `Unhealthy`
4. **DI test** — builds the provider with `ValidateOnBuild` and `ValidateScopes` with and without the satellite

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- AddEncinaBreachNotification registers options, validators, the service, detector, rules, deadline monitor and the
  no-op notifier with TryAdd. The health check resolves IBreachNotifier.

TASK:
Register the new services, remove the notifier default, adjust the health check and add the DI tests.

KEY RULES:
- Registration completeness proven by a provider built with ValidateOnBuild and ValidateScopes.
- Every messaging pattern remains optional and off by default; the satellite is opt-in.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/ServiceCollectionExtensions.cs
- src/Encina.Compliance.BreachNotification/Health/BreachNotificationHealthCheck.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy** — all new events and the dispatch records carry `TenantId`; the package and communications are tenant-scoped via the breach; two-tenant tests (REQ-061)
2. **Module isolation** — the new events carry `ModuleId` like the existing ones
3. **Audit trail** — the events are the audit trail (content recorded, package generated, communication dispatched) with user and time
4. **Idempotency** — deduplication key per recipient (Design Choice 4)
5. **Validation** — communication and content validators return coded errors
6. **Evaluated and not integrated** — caching of the read model already exists (`InvalidateCache`) and is reused; resilience, locks and transactions are delegated (see the matrix)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Existing events carry TenantId and ModuleId; DefaultBreachNotificationService invalidates a cache after saves.

TASK:
Make the new events, service methods and read model tenant-aware and module-aware and add the two-tenant tests.

KEY RULES:
- Tenant comes from the breach stream (BreachReadModel.TenantId), never from a model the caller builds; a breach id of
  another tenant returns not found.
- With tenancy off, TenantId is null on the events (as the existing events) and no tenant filter or tag applies; main has
  no default-tenant constant and a literal would clash with a real tenant of that name.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Events/BreachNotificationEvents.cs
- src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs
```

</details>

---

### Phase 7: Observability

<details>
<summary><strong>Tasks</strong></summary>

The names in the issue (`encina.breach.packages`, `encina.breach.communications`, `encina.breach.package`) were proposals; the plan follows the package's existing naming (`breach.*` counters and tags, source `Encina.Compliance.BreachNotification`).

1. **`Diagnostics/BreachNotificationDiagnostics.cs`** (modify) — counters `breach.packages.generated.total` (tag `breach.outcome`), `breach.communications.dispatched.total` (tags `breach.channel`, `breach.outcome`), `breach.communications.recipients.total` (tag `breach.outcome`: queued, deduplicated, failed); activities `Breach.BuildPackage` and `Breach.Communicate` with tags `breach.tenant_id` (emitted only when multi-tenancy is on, absent when off, ADR-054 D4) and `breach.outcome`; no recipient, no content, no subject identifier
2. **`EventIdRanges.cs`** — register `ComplianceBreachChannels = (6565, 6579)` ("Encina.Compliance.BreachNotification — Art. 33(3) package and Art. 34 communications"; assigned from the batch block 6550-6599; `ComplianceBreachNotification` 8700-8799 keeps only 8795-8799 free after #813, and #812 uses 9700-9749) and map `Encina.Compliance.BreachNotification` to it in `EncinaEventIdAllocationTests`; registered in PR 2, the first PR that logs. The registry (`eventid-blocks.md`) lists 6550-6599 as the wave-2 block "compliance-dsr-audit (family 13)" and as used by #1248, #1242, #1240 and #1241; this plan uses 6565-6579 of it (confirmed on 2026-11-12, not renumbered); 6565-6579 reserves 15 ids for the 8 used
3. **`Diagnostics/BreachChannelsLogMessages.cs`** (new) — `[LoggerMessage]` 6565 onward, packed: `Art33ContentRecorded` (6565), `PackageGenerated` (6566), `PackageLateWithoutReason` (6567, Warning), `PackageContentIncomplete` (6568, Warning), `CommunicationDispatched` (6569, Information, counts only), `CommunicationFailed` (6570, Error), `SubjectChannelNotConfigured` (6571, Warning), `CommunicationInvalid` (6572, Warning)
4. **Satellite** — `Encina.Compliance.BreachNotification.Notifications` logs through the module's messages (no new range)
5. **Health check** — Phase 5

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- ActivitySource and Meter "Encina.Compliance.BreachNotification"; tags prefixed "breach."; the new range 6565-6579 comes
  from the plan batch's assigned block 6550-6599; ComplianceBreachNotification (8700-8799) ids 8792-8794 belong to #813.

TASK:
Add the counters, activities, the range registration and the [LoggerMessage] entries.

KEY RULES:
- Register the range in src/Encina/Diagnostics/EventIdRanges.cs first; pack EventIds sequentially from 6565; XML docs
  name the range ("Event IDs: 6565-6572 (see EventIdRanges.ComplianceBreachChannels)").
- Map the assembly in EncinaEventIdAllocationTests and run it.
- No recipient references, content, parameters or Error.Message in tags, metrics or logs; exceptions via ForLogging().

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationDiagnostics.cs
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationLogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 8: Testing

<details>
<summary><strong>Tasks</strong></summary>

Manifest first: per-file targets with one-sentence justifications for every new file in `.github/coverage-manifest/Encina.Compliance.BreachNotification.json` and the manifest of the new satellite (written by `mechanical-fixer`).

1. **UnitTests** — aggregate (content recorded, package generated, communication recorded, closed-breach guards); package service (late package needs a delay reason, content incomplete, hash stable); contributor; Markdown writer (snapshot of the (a)-(d) order); communication flow (no channel -> coded `Left`, invalid communication, partial failure keeps counts, exemption sends nothing, retry sends with the same dedup keys); the satellite notifier with a fake `INotificationSender`; telemetry with an in-memory exporter (`breach.tenant_id` present when tenancy is on and absent when off; no recipient or content); `GetCategory` test per new error code; failure injection between send and record and after record; a communication for another tenant's breach id returns not found; total failure appends nothing, partial failure appends one event; unit tests mock `IAggregateRepository` (no InMemory stores)
2. **GuardTests** — all public constructors and methods
3. **ContractTests** — package content contract (JSON and Markdown snapshot); `IBreachNotifier` contract against the satellite notifier
4. **PropertyTests** — justified: document assembly is deterministic and covered by contract and unit tests (matches the issue's own test matrix). The determinism of the deduplication key (same inputs, same key; different recipient or communication, different key) is covered by the unit flag on `NotificationsBreachNotifier` and by the contract snapshot, not by a property test
5. **IntegrationTests** — Marten on PostgreSQL through Testcontainers (`BreachNotificationAggregateIntegrationTests` pattern): record content, build package, dispatch a communication, read it back with its counts; S18 (a breach is recorded, the package is produced inside the deadline, subjects are communicated and tracked); two tenants; restart (aggregate rebuilt from events)
6. **Load/Benchmark** — `.md` justifications: not a concurrent hot path
7. **PR split** — the ten pull requests of the Summary table, each with its own unit tests, manifest entries and PublicAPI lines (the Marten integration tests of each new aggregate behavior run in PR 7; each PR before it reaches its integration target for the files it touches through the existing `BreachNotificationAggregateIntegrationTests` extended in PR 1 and PR 3)

**Per-file targets** (percent of coverable lines per flag; `-` not applicable; the PR reports the measured value). Modified files have no per-file target on main today, so they inherit the package defaults (unit 70, guard 20, contract 15, property 15); the numbers below are per-file targets added by this plan and are higher than that baseline:

| File | Unit | Guard | Contract | Property | Integration | One-sentence justification |
|------|-----:|------:|---------:|---------:|------------:|---------------------------|
| `Model/Art33Content.cs` | 90 | 90 | - | - | - | Record members are exercised by the validator and aggregate tests. |
| `Model/Art33Package.cs` | 90 | 0 | 60 | - | - | A data record; no argument to guard; the contract snapshot reads it. |
| `Model/BreachSubjectCommunication.cs`, `SubjectRecipient.cs`, `SubjectCommunicationResult.cs` | 90 | 60 | - | - | - | Data records built by the communication tests. |
| `Abstractions/IBreachPackageService.cs`, `IBreachPackageDocumentWriter.cs`, `ISubjectDeliveryStatusReader.cs` | 0 | 0 | - | - | - | Interfaces with no executable lines. |
| `Reporting/MarkdownBreachPackageWriter.cs` | 100 | 50 | 80 | - | - | Pure rendering; every section and the late/delay branch is a unit case; the snapshot is the contract. |
| `Reporting/Art33IncidentReportContributor.cs` | 95 | 50 | 70 | - | 40 | Each field mapping and the incomplete-content branch are unit cases; the integration test builds a package from a Marten breach. |
| `Services/DefaultBreachPackageService.cs` | 95 | 40 | 60 | - | 60 | Late-without-reason, incomplete content and hash stability are unit cases; integration builds a package end to end. |
| `NotificationsBreachNotifier.cs` (satellite) | 95 | 40 | 80 | - | - | A fake `INotificationSender` drives queued, deduplicated and failed paths; the `IBreachNotifier` contract runs against it. |
| `NotificationsSubjectDeliveryStatusReader.cs` (satellite) | 90 | 40 | - | - | 60 | Count mapping is unit tested; the query runs against the real delivery store in the integration test. |
| Satellite `ServiceCollectionExtensions.cs` | 100 | 100 | - | - | - | Registration and null-argument cases cover every line. |
| `Aggregates/BreachAggregate.cs` (modified) | 90 | 50 | - | - | 60 | Every new command, guard and the duplicate-communication branch is a unit case; integration replays the new events. |
| `Events/BreachNotificationEvents.cs` (modified) | 80 | - | - | - | 40 | New records are built by the aggregate tests; generated record members stay unreached. |
| `ReadModels/BreachReadModel.cs`, `BreachProjection.cs` (modified) | 90 | 40 | - | - | 70 | Projection handlers are pure; the Marten projection test replays the events. |
| `BreachNotificationErrors.cs` (modified) | 100 | - | - | - | - | Every factory is a pure function asserted for code and `ErrorCategory`. |
| `Services/DefaultBreachNotificationService.cs` (modified) | 90 | 30 | - | - | 60 | The communication flow, tenant check and failure injection are unit cases with a mocked repository; integration drives the happy path. |
| `Health/BreachNotificationHealthCheck.cs` (modified) | 100 | 30 | - | - | - | Registered and unregistered channel are both unit cases. |
| `ServiceCollectionExtensions.cs`, `BreachNotificationOptions.cs` (modified) | 100 | 100 | - | - | - | Registration with and without the satellite is built with `ValidateOnBuild` and `ValidateScopes`. |
| `Diagnostics/BreachNotificationDiagnostics.cs` (modified) | 100 | - | - | - | - | Counters and activities are driven by listener tests. |
| `Diagnostics/BreachChannelsLogMessages.cs` (new) | 0 | - | - | - | - | Source-generated delegates without executable lines. |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
- Event-sourced compliance modules have no InMemory stores: unit tests mock IAggregateRepository (NSubstitute) and
  integration tests run against Marten on PostgreSQL via Testcontainers.
- Existing neighbours: tests/Encina.UnitTests/Compliance/BreachNotification/, and
  tests/Encina.IntegrationTests/Compliance/BreachNotification/ (BreachNotificationAggregateIntegrationTests).

TASK:
Implement the tests listed in Phase 8 and the manifest targets, then measure per-flag coverage.

KEY RULES:
- Tests execute real package code; no reflection-only tests.
- Assertions with Shouldly via Encina.Testing.Shouldly; builders for test data; no Thread.Sleep; FakeTimeProvider.
- Integration tests use the shared Marten/PostgreSQL fixtures, ClearAllDataAsync in InitializeAsync.
- Measure per flag with dotnet test ... --collect "XPlat Code Coverage" and the coverage-report script.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Compliance.BreachNotification.json
- tests/Encina.IntegrationTests/Compliance/BreachNotification/BreachNotificationAggregateIntegrationTests.cs
- tests/Encina.UnitTests/Compliance/BreachNotification/BreachAggregateTests.cs
```

</details>

---

### Phase 9: Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on all new public APIs (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`)
2. `changelog.d/1242-breach-art33-package-art34-communications.added.md` and `changelog.d/1242-breach-notifier-reshaped.changed.md` (the removal of the no-op notifier)
3. READMEs of `Encina.Compliance.BreachNotification` and the new satellite
4. `docs/features/` breach notification page: recording Art. 33(3) content, generating the package, communicating under Art. 34, reading communications; the statement that current law (72 hours, "risk") is the default and the Omnibus 96 hours and the single entry point are off by default (#813, #812)
5. `docs/INVENTORY.md`; `PublicAPI.Unshipped.txt` in both packages
6. ADR: not needed (the choices follow AGENTS.md, ADR-019 and SPEC-002). ADR number assigned when the ADR pull request opens, if the maintainer asks for one
7. Build verification: `dotnet build --configuration Release` with 0 warnings; `dotnet test` green; every coverage flag reaches its manifest target; no hand-typed coverage figures (cite `cov:` markers)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT:
- Documentation rules: .claude/skills/encina-docs/SKILL.md. Changelog fragments: changelog.d/README.md.

TASK:
Document the Art. 33(3) package and the Art. 34 communications, including the optional satellite.

KEY RULES:
- English only; no private client application named (write "the reference application").
- The decision to notify stays with the organisation; say so. Current law is the default; Omnibus items are draft law.
- Never type coverage figures; cite cov: markers. Never edit CHANGELOG.md.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/README.md
- docs/features/ (breach notification page)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| GDPR | Art. 33(1) | 72 hours; reasons for delay when later |
| GDPR | Art. 33(3), 33(4) | Content of the notification; information in phases |
| GDPR | Art. 33(5) | Documentation of the breach, effects and remedial action |
| GDPR | Art. 34(1)-(3) | Communication to data subjects; exemptions (encryption, mitigating measures, disproportionate effort) |
| Digital Omnibus | COM(2025) 837 (draft) | 96 hours and single entry point, off by default (#813, #812) |
| SPEC-002 | REQ-049, REQ-024, AC-039, S18, REQ-061, REQ-062 | The requirement this plan implements |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `BreachAggregate`, events, `BreachReadModel`, `BreachProjection` | `src/Encina.Compliance.BreachNotification/` | Extended with the new events and fields |
| `IBreachNotificationService`, `DefaultBreachNotificationService` | `src/Encina.Compliance.BreachNotification/` | Gains communication methods; pattern for the package service |
| `IBreachNotifier` | `src/Encina.Compliance.BreachNotification/Abstractions/` | Reshaped to the subject path |
| `BreachNotificationDiagnostics`, `BreachNotificationLogMessages` | `.../Diagnostics/` | Telemetry and logging style |
| `IIncidentReportContributor`, `IncidentReport`, `JsonIncidentReportSerializer` | Plan #812 | Authority path |
| `INotificationSender`, `INotificationDeliveryStore`, `NotificationClass` | Plan #1235 | Subject delivery |
| `IExportFormatWriter` | `src/Encina.Compliance.DataSubjectRights/Abstractions/` | Writer abstraction precedent |
| `AddEncinaMarten...` registration and Marten integration fixtures | `src/Encina.Marten`, `tests/Encina.IntegrationTests/Compliance/BreachNotification/` | Persistence and tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.BreachNotification` (channels) | 6565-6579 (`ComplianceBreachChannels`, new, from the batch block 6550-6599) | Uses 6565-6572; `ComplianceBreachNotification` 8700-8799 has 8792-8794 reserved by #813; #812 uses 9700-9749 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core module (models, events, aggregate, read model, services, writer, contributor, errors, DI, diagnostics) | ~20 | modified and new |
| Satellite package | ~5 | csproj, notifier, DI, README, PublicAPI |
| Tests | ~14 | unit, guard, contract, Marten integration, DI |
| Manifests, changelog, documentation | ~8 | |
| **Total** | **~50** | distinct files; ten pull requests, none above 15 files (PR 4a is exactly 15) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1242 (SPEC-002 P-40): the Art. 33(3) package and Art. 34 communications in
Encina.Compliance.BreachNotification, plus the satellite Encina.Compliance.BreachNotification.Notifications.

PROJECT CONTEXT:
- .NET 10 / C# 14, ROP (Either<EncinaError, T>), pre-1.0 (breaking changes preferred, no [Obsolete]).
- Event-sourced on Marten (ADR-019, SPEC-002 DEC-008 (a)); unit tests mock IAggregateRepository; integration tests use
  Marten on PostgreSQL through Testcontainers.
- Depends on #812 (IncidentReport, IIncidentReportContributor, serializers) and #1235 (INotificationSender).

IMPLEMENTATION OVERVIEW:
Phase 1: Art33Content, Art33Package, communication models, events (content recorded, package generated, communication
         dispatched), aggregate methods, read model/projection fields, error codes
Phase 2: IBreachPackageService, Art33IncidentReportContributor, MarkdownBreachPackageWriter; late package needs a delay reason
Phase 3: IBreachNotifier reshaped to the subject path; no-op removed; CommunicateToSubjectsAsync / GetCommunicationsAsync
Phase 4: satellite NotificationsBreachNotifier on INotificationSender (transactional, dedup key per recipient)
Phase 5: DI, health check (Degraded without a channel), options cleanup
Phase 6: tenant and module on all events; two-tenant tests
Phase 7: breach.* counters and activities; EventIds 6565-6572 in the new range 6565-6579
Phase 8: unit, guard, contract, Marten integration (S18), manifests
Phase 9: docs, READMEs, changelog fragments, PublicAPI

KEY PATTERNS:
- The decision to notify stays with the organisation; current law (72 h, "risk") is the default; Omnibus items off.
- Never report success without delivery; send first with a deterministic dedup key, record second.
- Individual recipients and their status stay in the notification delivery store, not in the event stream.
- No recipient references, content, parameters or Error.Message in logs, tags, metrics or errors.
- TimeProvider for time; CRAP <= 10 per new method; tests execute real code.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/ (Aggregates, Events, ReadModels, Services, Abstractions, Diagnostics)
- docs/plans/enisa-single-entry-point-implementation-plan-812.md
- docs/plans/notifications-implementation-plan-1235.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-049, AC-039, S18)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The service already caches the read model and invalidates it after saves; the new methods reuse that cache and add none |
| 2 | OpenTelemetry | ✅ | `Breach.BuildPackage` and `Breach.Communicate` activities, `breach.packages.generated.total` and communication counters, `breach.tenant_id`, no recipients or content (Phase 7) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6565-6572 in the new range `ComplianceBreachChannels` (ADR-021) |
| 4 | Health Checks | ✅ | `BreachNotificationHealthCheck` reports `Degraded` when no subject channel is registered |
| 5 | Validation | ✅ | Content, package (late without reason) and communication validators return coded errors; options validated at start-up |
| 6 | Resilience | ❌ | Delivery retries belong to `Encina.Notifications` (outbox and channel retries); this module only enqueues, and a `Left` fails the operation visibly |
| 7 | Distributed Locks | ❌ | The aggregate's optimistic concurrency covers concurrent writers; deduplication keys make a duplicate communication harmless |
| 8 | Transactions | ❌ | Marten appends events atomically per save; the send and the event are deliberately sequenced (send with deduplication, then record) because they live in different stores |
| 9 | Idempotency | ✅ | Caller-supplied `CommunicationId` plus a deterministic dedup key per recipient; a retried communication re-sends nothing new and the aggregate keeps one event per `CommunicationId` |
| 10 | Multi-Tenancy | ✅ | REQ-061/AC-043: events and read model carry `TenantId` (null when tenancy is off, tenant read from the breach stream, never from the caller's model); two-tenant test; another tenant's breach id returns not found |
| 11 | Module Isolation | ✅ | The new events carry `ModuleId` like the existing breach events |
| 12 | Audit Trail | ✅ | Content recorded, package generated (with hash) and each communication are events with user and time (GDPR Art. 33(5)) |

---

## Prerequisites & Dependencies

| Item | Why | State |
|------|-----|-------|
| #812 reporting layer (`IncidentReport`, `IIncidentReportContributor`, `IIncidentReportBuilder`, serializers, file-export adapter) | Authority path (Design Choice 2) | Open, plan in review; hard prerequisite of PR 5 only (PRs 1-4 do not need it) |
| #1235 `Encina.Notifications` (`INotificationSender`, delivery store) | Subject delivery | Open, plan in review; hard prerequisite of PR 6 only, its pull requests merged first |
| A delivery-store query by correlation key (`breach:{breachId}:{communicationId}`) | The satellite's `ISubjectDeliveryStatusReader` | Not in the #1235 plan today; it must be added to that plan and merged before PR 6 opens (the heavier fallback, delivery ids on the dispatch event, is not planned) |
| Removal of the dead options `SupervisoryAuthority` and `AutoNotifyOnHighSeverity`, and of `NotifyAuthorityAsync` | Same interface and options edited by #812's plan | Owner: PR 4a (`NotifyAuthorityAsync`) and PR 4b (the options) of this plan; the #812 plan drops those lines (agreement through the sibling-plan link) |
| EventId range 6565-6579 | New logging | Registered by PR 2 (the first PR that logs) |

---

## Next Steps

1. Link this plan from issue #1242; ask the maintainer the Dilemmas of Design Choices 2 and 5 when work on the issue starts
2. Implement PR 1 to PR 7 (with 4a, 4b, 4c and 4d) in the order of the Summary table (PR 5 after #812's pull requests, PR 6 after #1235's)
3. Agree with the plan of #1235 the delivery-store query by correlation key, and with the plan of #812 that it drops the removal of `NotifyAuthorityAsync` and of the dead options
4. The final commit references `Fixes #1242`
