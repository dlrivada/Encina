# Implementation Plan: `Encina.Compliance.BreachNotification` — Art. 33(3) Package and Art. 34 Communications

> **Issue**: [#1242](https://github.com/dlrivada/Encina/issues/1242) (SPEC-002 tracking id **P-40**, priority **P0**)
> **Type**: Feature
> **Complexity**: High (9 phases, event-sourced on Marten, one new satellite package, ~38 files, 3 pull requests)
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

**Size**: about 38 files, so three worker pull requests: PR 1 (Art. 33(3) content: events, aggregate, read model, service method, contributor, package builder, writers, unit and guard tests); PR 2 (Art. 34: reshaped `IBreachNotifier`, service method, events, tracking, removal of the no-op, unit and guard tests); PR 3 (the satellite package, DI, health check, contract, property and Marten integration tests, documentation). Each stays within 15 files and 1,500 lines.

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
- Content is free text written by the organisation. It is not personal data of the affected subjects; the validator rejects obviously identifying fields by schema (there are none: counts and category names only) and the documentation says so.
- A submission after `DeadlineUtc` without a delay reason returns `Left(breach.delay_reason_required)` (Art. 33(1)); the reason is part of the content event.
- The deadline is the aggregate's `DeadlineUtc` (72 hours by default, `BreachNotificationOptions.NotificationDeadlineHours`); the Omnibus 96 hours stays #813 and off by default.

**Class:** Settled
**Settled by:** GDPR Art. 33(3), 33(4) and 33(5); SPEC-002 REQ-049 and REQ-024; plan #812 (current state: "the event-sourced `BreachAggregate` and `BreachReadModel` do not [carry the Art. 33(3) fields]. #1242 adds them")

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
- Idempotency: the deduplication key of each notification is `breach:{breachId}:{communicationId}:{recipientRef}`. The service sends first and records the event second; a retry after a crash re-sends with the same keys, #1235's store returns the existing rows, so nobody is contacted twice, and the event is written once.
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

### Chosen Option: **A — satellite package**

### Rationale

- Pay-for-what-you-use (AGENTS.md section 1) and provider coherence (abstractions in the module, implementations in satellites, section 3).
- The satellite registers `IBreachNotifier` before `AddEncinaBreachNotification` (satellite DI called before the core, with `TryAdd`).
- Breach communications are `NotificationClass.Transactional` (legal obligation, no marketing consent), with the purpose string `breach-communication`; they never use the commercial basis.

**Class:** Settled
**Settled by:** AGENTS.md section 1 ("pay-for-what-you-use") and section 3 (provider coherence); plan #1235 (`INotificationSender`, `NotificationClass.Transactional`)

</details>

<details>
<summary><strong>6. Fate of <code>IBreachNotifier</code> and its No-Op Default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reshape `IBreachNotifier` to the subject path only (`NotifyDataSubjectsAsync(BreachSubjectCommunication, ct)` returning `Either<EncinaError, SubjectCommunicationResult>`); remove `DefaultBreachNotifier`, `BreachRecord` use and the authority half (#812); with no notifier registered `CommunicateToSubjectsAsync` returns `Left(breach.subject_channel_not_configured)`** | No code that reports success without delivering; one honest contract | Breaking change (pre-1.0, acceptable) |
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

1. **`Model/Art33Content.cs`** (new) — `CategoriesOfDataAffected` (list), `ApproximateRecordsAffected` (int?), `DpoContact`, `LikelyConsequences`, `MeasuresTakenOrProposed`, `DelayReason?`
2. **`Model/Art33Package.cs`** (new) — `ReportId`, `Json` (bytes), `Markdown`, `GeneratedAtUtc`, `IsLate`
3. **`Model/BreachSubjectCommunication.cs`, `Model/SubjectRecipient.cs`, `Model/SubjectCommunicationResult.cs`** (new) — communication request (breach id, tenant, channel, template id, parameters, recipients as opaque refs) and result (queued, failed, deduplicated counts, error codes)
4. **`Events/BreachNotificationEvents.cs`** (modify) — `BreachArt33ContentRecorded`, `BreachAuthorityPackageGenerated`, `BreachSubjectCommunicationDispatched`; each carries `TenantId` and `ModuleId` like the existing events
5. **`Aggregates/BreachAggregate.cs`** (modify) — `RecordArticle33Content`, `RecordAuthorityPackageGenerated`, `RecordSubjectCommunication`; state guards (not closed; content only after detection)
6. **`ReadModels/BreachReadModel.cs`, `ReadModels/BreachProjection.cs`** (modify) — Art. 33(3) fields, package generation hash and time, list of communication summaries
7. **`BreachNotificationErrors.cs`** (modify) — `breach.delay_reason_required`, `breach.article33_content_incomplete`, `breach.subject_channel_not_configured`, `breach.communication_invalid`, `breach.communication_failed` (details: code and counts, never recipients)
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
- Time comes from the caller (TimeProvider in the service), never DateTime.UtcNow in the aggregate.

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

1. **`Abstractions/IBreachPackageService.cs`** (new) — `BuildAuthorityPackageAsync(Guid breachId, string requestedByUserId, CancellationToken)` returning `Either<EncinaError, Art33Package>`; `RecordArticle33ContentAsync(Guid breachId, Art33Content content, string recordedByUserId, CancellationToken)`
2. **`Reporting/Art33IncidentReportContributor.cs`** (new, implements #812's `IIncidentReportContributor`, `Order` after the base mapping) — fills `CategoriesOfDataAffected`, `DpoContact`, `LikelyConsequences`, `MeasuresTaken` and `DelayReason` from the read model; returns `Left(breach.article33_content_incomplete)` when the content is missing and `Reporting.RequireArticle33Content` is true
3. **`Services/DefaultBreachPackageService.cs`** (new) — loads the read model, rejects a late package without a delay reason, builds the report with `IIncidentReportBuilder`, serializes JSON, renders Markdown, appends `BreachAuthorityPackageGenerated` with the content hash (SHA-256 over the canonical JSON)
4. **`Abstractions/IBreachPackageDocumentWriter.cs`, `Reporting/MarkdownBreachPackageWriter.cs`** (new) — renders the Art. 33(3)(a)-(d) items, phased reports and delay reason
5. Until #812 lands, PR 1 ships items 1, 3 (using a local minimal report shape behind an internal interface) and 4; item 2 and the shared serializer join when #812's model exists (Design Choice 2)

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

1. **`Abstractions/IBreachNotifier.cs`** (modify) — only `ValueTask<Either<EncinaError, SubjectCommunicationResult>> NotifyDataSubjectsAsync(BreachSubjectCommunication communication, CancellationToken ct)`; `NotifyAuthorityAsync` is removed by #812
2. **`DefaultBreachNotifier.cs`, `Model/NotificationResult.cs`, `Model/NotificationOutcome.cs`** — removed (Design Choice 6); `BreachRecord`, `Notifications/AuthorityNotifiedNotification.cs`, `Notifications/SubjectsNotifiedNotification.cs` removed when unreferenced
3. **`Abstractions/IBreachNotificationService.cs`** (modify) — `CommunicateToSubjectsAsync(Guid breachId, BreachSubjectCommunication communication, string communicatedByUserId, CancellationToken)` and `GetCommunicationsAsync(Guid breachId, CancellationToken)`
4. **`Services/DefaultBreachNotificationService.cs`** (modify) — flow: load breach (must be assessed, not closed) -> validate (recipients present, template id, channel known) -> `IBreachNotifier` (resolved optionally: `Left(breach.subject_channel_not_configured)`) -> on `Right` append `BreachSubjectCommunicationDispatched` -> a `Left` or any failed recipient is recorded in the event counts and returned as `Left(breach.communication_failed)` when none was queued; partially failed batches return `Right` with the counts so the organisation sees them
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
3. **`ServiceCollectionExtensions.AddEncinaBreachNotificationNotifications()`** (new) — `TryAddScoped<IBreachNotifier, NotificationsBreachNotifier>()`; called before `AddEncinaBreachNotification`
4. **Delivery-status join** — `GetCommunicationsAsync` reads counts through a query on `INotificationDeliveryStore` by correlation key (a prerequisite on #1235, see below)

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

1. **`ServiceCollectionExtensions.AddEncinaBreachNotification`** (modify) — register `IBreachPackageService`, `IBreachPackageDocumentWriter`, `Art33IncidentReportContributor` (additive, after #812), remove the `IBreachNotifier` default registration
2. **`BreachNotificationOptions`** (modify) — remove the dead `SupervisoryAuthority` and `AutoNotifyOnHighSeverity` if #812 has not (coordinate; one of the two plans removes them); `SubjectNotificationSeverityThreshold` stays and is documented as advisory (the decision to communicate is the organisation's)
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
- Tenant comes from the breach/request context, never from the request body.
- With tenancy off, a fixed default tenant applies and no tenant configuration is needed.

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

1. **`Diagnostics/BreachNotificationDiagnostics.cs`** (modify) — counters `breach.packages.generated.total` (tag `breach.outcome`), `breach.communications.dispatched.total` (tags `breach.channel`, `breach.outcome`), `breach.communications.recipients.total` (tag `breach.outcome`: queued, deduplicated, failed); activities `Breach.BuildPackage` and `Breach.Communicate` with tags `breach.tenant_id`, `breach.outcome`; no recipient, no content, no subject identifier
2. **`EventIdRanges.cs`** — register `ComplianceBreachChannels = (6565, 6579)` ("Encina.Compliance.BreachNotification — Art. 33(3) package and Art. 34 communications"; assigned from the batch block 6550-6599; `ComplianceBreachNotification` 8700-8799 keeps only 8795-8799 free after #813, and #812 uses 9700-9749) and map `Encina.Compliance.BreachNotification` to it in `EncinaEventIdAllocationTests`
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

1. **UnitTests** — aggregate (content recorded, package generated, communication recorded, closed-breach guards); package service (late package needs a delay reason, content incomplete, hash stable); contributor; Markdown writer (snapshot of the (a)-(d) order); communication flow (no channel -> coded `Left`, invalid communication, partial failure keeps counts, exemption sends nothing, retry sends with the same dedup keys); the satellite notifier with a fake `INotificationSender`; telemetry with an in-memory exporter (tenant tag present, no recipient or content); unit tests mock `IAggregateRepository` (no InMemory stores)
2. **GuardTests** — all public constructors and methods
3. **ContractTests** — package content contract (JSON and Markdown snapshot); `IBreachNotifier` contract against the satellite notifier
4. **PropertyTests** — justified: document assembly is deterministic and covered by contract and unit tests (matches the issue's own test matrix)
5. **IntegrationTests** — Marten on PostgreSQL through Testcontainers (`BreachNotificationAggregateIntegrationTests` pattern): record content, build package, dispatch a communication, read it back with its counts; S18 (a breach is recorded, the package is produced inside the deadline, subjects are communicated and tracked); two tenants; restart (aggregate rebuilt from events)
6. **Load/Benchmark** — `.md` justifications: not a concurrent hot path
7. **PR split** — Phases 1-2 (PR 1), 3 and the removals (PR 2), 4-5 and the integration tests (PR 3)

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
| **Total** | **~47** | three pull requests |

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
| 9 | Idempotency | ✅ | Deterministic dedup key per recipient; a retried communication re-sends nothing and records one event |
| 10 | Multi-Tenancy | ✅ | REQ-061/AC-043: events and read model carry `TenantId`; two-tenant test; fixed default tenant when tenancy is off |
| 11 | Module Isolation | ✅ | The new events carry `ModuleId` like the existing breach events |
| 12 | Audit Trail | ✅ | Content recorded, package generated (with hash) and each communication are events with user and time (GDPR Art. 33(5)) |

---

## Prerequisites & Dependencies

| Item | Why | State |
|------|-----|-------|
| #812 reporting layer (`IncidentReport`, `IIncidentReportContributor`, `IIncidentReportBuilder`, serializers, file-export adapter) | Authority path (Design Choice 2) | Open, plan merged or in review; PR 1 contains the parts that do not need it |
| #1235 `Encina.Notifications` (`INotificationSender`, delivery store) | Subject delivery | Open, plan in review |
| A delivery-store query by correlation key (`breach:{breachId}:{communicationId}`) | `GetCommunicationsAsync` counts | Not in the #1235 plan today; add it there, or fall back to storing delivery ids on the dispatch event (heavier) |
| Removal of the dead options `SupervisoryAuthority` and `AutoNotifyOnHighSeverity` | Coordinated with #812 | One of the two pull requests removes them |
| EventId range 6565-6579 | New logging | Registered by PR 1 |

---

## Next Steps

1. Link this plan from issue #1242; ask the maintainer the Dilemma of Design Choice 2 when work on the issue starts
2. Implement PR 1 (Art. 33(3) content, package, writer), PR 2 (Art. 34 communications and the removals), PR 3 (satellite, DI, integration tests, documentation)
3. Agree with the plan of #1235 the delivery-store query by correlation key
4. The final commit references `Fixes #1242`
