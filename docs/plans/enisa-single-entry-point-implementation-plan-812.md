# Implementation Plan: Incident reporting adapters for `Encina.Compliance.BreachNotification` — ENISA Single Entry Point slot (Digital Omnibus, COM(2025) 837)

> **Issue**: [#812](https://github.com/dlrivada/Encina/issues/812)
> **Type**: Feature
> **Milestone**: v0.15.0 — EU Compliance: NIS2 & Digital Omnibus
> **Complexity**: Medium-High (8 phases, Marten only (ADR-019, SPEC-002 DEC-008 (a)), ~30 production files)
> **Estimated Scope**: ~2,200-2,900 lines of production code + ~2,500-3,200 lines of tests

---

## Summary

Add a **reporting adapter layer** to `Encina.Compliance.BreachNotification`. It turns a recorded breach or incident into a regulation-neutral `IncidentReport`, serializes it, and sends it to one or more competent authorities through `IIncidentReportingAdapter` implementations. Routing is per regulation (GDPR, NIS2, and later DORA and CRA), per reporting phase and per tenant. Every submission attempt and its outcome is recorded on the breach event stream. Two adapters ship: a file-export adapter, which is the fallback for authorities without an API (the AEPD among them), and a generic HTTP JSON adapter. The ENISA Single Entry Point (SEP) of the Digital Omnibus proposal COM(2025) 837 gets an **adapter slot behind an option that is off by default**. The concrete SEP adapter waits until ENISA publishes the API specification.

### Standards covered

- GDPR Art. 33(1), 33(3), 33(4) and 33(5): notification to the supervisory authority, its content, phased reporting, documentation.
- NIS2 (Directive (EU) 2022/2555) Art. 23(4): early warning (24 h), incident notification (72 h), intermediate report, final report (1 month).
- Digital Omnibus COM(2025) 837: the single entry point managed by ENISA (issue: Arts. 6-9; SPEC-002 §3.5: GDPR Art. 33(1) as amended and NIS2 Art. 23a). This is **draft law**. Under SPEC-002 REQ-024 and DEC-012 the SEP behaviour is off by default, the XML documentation and the package specification (#1208) name it by its draft article, and current law stays the default.

### What already exists

Nothing of the reporting layer exists. The relevant code today:

- `src/Encina.Compliance.BreachNotification/Abstractions/IBreachNotifier.cs:86` declares `NotifyAuthorityAsync(BreachRecord, ...)`. Its only implementation, `DefaultBreachNotifier.cs:49`, logs a warning and returns `NotificationOutcome.Sent` without delivering anything. **No production code calls `IBreachNotifier`.** It is registered (`ServiceCollectionExtensions.cs:100`) and probed by the health check (`Health/BreachNotificationHealthCheck.cs:162`), but `Services/DefaultBreachNotificationService.cs:165` (`ReportToDPAAsync`) only records that a report was made. It never sends one.
- `Aggregates/BreachAggregate.cs:252` (`ReportToDPA`) allows **one** authority report, and only from `Detected` or `Investigating`. Nothing records which regulation a report satisfies or which authority received it, apart from a free-text `AuthorityName`.
- `BreachNotificationOptions.cs:135` (`SupervisoryAuthority`) and `:150` (`AutoNotifyOnHighSeverity`) are declared and validated but never read anywhere in `src/` (verified by search). Both are dead options.
- `BreachNotificationOptions.cs:105` (`NotificationDeadlineHours`) is ignored by the aggregate and the projection, which hard-code 72 hours (`BreachAggregate.cs:457`, `ReadModels/BreachProjection.cs:68`). That is a separate defect, written up as an issue file (see Prerequisites); this plan's deadline policy does not depend on it.
- NIS2 incidents reach the breach stream through `src/Encina.Compliance.NIS2/DefaultNIS2IncidentHandler.cs:141` (`RecordBreachAsync` with `detectedByRule: "NIS2IncidentHandler"`). The stream does not record that the breach is a NIS2 incident.
- `BreachRecord` (`Model/BreachRecord.cs:33`) carries the Art. 33(3) fields (categories, DPO contact, likely consequences, measures), but the event-sourced `BreachAggregate` and `BreachReadModel` do not. #1242 (P-40) adds them through its Art. 33(3) package builder.

### Corrections to the issue body

- The issue sketches `SubmitNotificationAsync(BreachNotification breach, ...)`. No type `BreachNotification` exists. This plan passes a built `IncidentReport` (Design Choice 2). It also replaces the separate `SubmitUpdateAsync` with one `SubmitAsync`, whose `IncidentReportPhase` covers both cases.
- The issue's "AEPD adapter (deferred)" has no target: the AEPD publishes no public API for breach notification, which #1242 already records under its "Alternatives Considered". The AEPD path is the file-export adapter plus the #1242 Art. 33(3) package. No AEPD adapter issue is opened.
- The issue defers Health Checks (⏭️) and Multi-Tenancy (⏭️). SPEC-002 REQ-061 and REQ-062, which came later, require both for every capability SPEC-002 adds or changes. This plan therefore **includes** them (see the matrix).
- The acceptance criterion "coverage >= 85%" is replaced by the per-flag targets of `.github/coverage-manifest/Encina.Compliance.BreachNotification.json` (AGENTS.md §9).

### Affected packages

- `Encina.Compliance.BreachNotification`: new `Reporting/` folder; new events on `BreachAggregate`; read model and projection extended; options, DI, health check, diagnostics.
- `Encina` (core): `src/Encina/Diagnostics/EventIdRanges.cs`, a new range.
- `Encina.Compliance.NIS2`: optional. One documentation example shows a NIS2 incident submitted with `IncidentRegulation.NIS2`; no code change is required.
- Tests: `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.PropertyTests`, `Encina.IntegrationTests` (Marten on PostgreSQL, and WireMock through `Encina.Testing.WireMock`); `.md` justifications under `Encina.LoadTests` and `Encina.BenchmarkTests`.

### Provider category

None of the 10 database providers. `Encina.Compliance.BreachNotification` is event-sourced on Marten (ADR-019; SPEC-002 DEC-008 (a) keeps it PostgreSQL-only in 1.0). The new state lives on the existing breach event stream. Resilience uses Polly through a keyed `ResiliencePipelineProvider<string>`, the pattern of `src/Encina.Compliance.NIS2/NIS2ResilienceHelper.cs`, but **without** that helper's error swallowing (AGENTS.md §3).

---

## Design Choices

<details>
<summary><strong>1. Package Placement — <code>Reporting/</code> inside <code>Encina.Compliance.BreachNotification</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Reporting/` folder and namespace inside `Encina.Compliance.BreachNotification`** | The breach stream is already the persistent incident record for GDPR and NIS2 (`DefaultNIS2IncidentHandler.cs:141` forwards into it); submissions are recorded on the same aggregate atomically; no new package; matches the issue's "Affected Packages" | Grows the package (~30 files); a future DORA module (#804) must reference BreachNotification to report |
| **B) New regulation-neutral package `Encina.Compliance.IncidentReporting`** | Clean name for GDPR, NIS2, DORA and CRA; BreachNotification stays focused on Arts. 33-34 | Needs its own persistence or a dependency back on the breach stream; submission and breach state can no longer be saved in one aggregate write; one more package to test, document and version |
| **C) Abstraction in BreachNotification plus an `Encina.Compliance.ENISA` package created now** | Matches the issue's two-package sketch | The ENISA package would ship empty until ENISA publishes the SEP API (the issue expects 18-24 months after entry into force); an empty package is dead code (AGENTS.md §3) |

### Chosen Option: **A — `Reporting/` inside `Encina.Compliance.BreachNotification`** (recommended, pending the maintainer)

### Rationale

- I recommend A because the submission record has to be written in the same aggregate save as the breach state it changes. Example: a successful GDPR initial report also moves the breach to `AuthorityNotified` (Design Choice 4). With A that is one Marten write; with B it is two stores and a consistency problem (SPEC-002 REQ-037).
- NIS2 already depends on BreachNotification (`Encina.Compliance.NIS2.csproj` references it), so nothing new is coupled.
- The ENISA package is created only when a concrete adapter exists. The issue file `plan-812-enisa-sep-concrete-adapter.md` records it.
- If DORA (#804, post-1.0, SPEC-002 DEC-015) later needs reporting without breach semantics, the `Reporting/` namespace can move into its own package then. Before 1.0 that is a breaking change we may still make freely; after 1.0 the DORA module can simply reference BreachNotification.

</details>

<details>
<summary><strong>2. Report Payload — regulation-neutral <code>IncidentReport</code> built by a composable builder</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Adapters receive `BreachRecord` (issue sketch)** | Already has the Art. 33(3) fields (`Model/BreachRecord.cs:66-94`) | `BreachRecord` is not what the event-sourced service persists or returns (`IBreachNotificationService` returns `BreachReadModel`); no regulation, phase, tenant or idempotency key; GDPR-specific, so NIS2 early warnings do not fit |
| **B) Regulation-neutral `IncidentReport` record built by `IIncidentReportBuilder` from `BreachReadModel` and ordered `IIncidentReportContributor`s** | One shape for GDPR, NIS2 and later DORA and CRA; carries regulation, phase, sequence, tenant, due date and a deterministic report id; #1242 plugs its Art. 33(3) content in as a contributor without changing adapters | One more model and a builder; the fields of the Art. 33(3) package arrive only when #1242 lands (until then they are optional and validated per regulation) |
| **C) Adapters receive `BreachReadModel` directly** | No new model | Mutable projection class exposed to third-party adapters; no regulation or phase; every adapter re-derives deadlines and ids |

### Chosen Option: **B — Regulation-neutral `IncidentReport` with `IIncidentReportBuilder` and contributors** (recommended, pending the maintainer)

### Rationale

- I recommend B because one incident can need several reports: a GDPR Art. 33 notification to the DPA, and a NIS2 early warning plus notification to the CSIRT (issue, "Multi-regulation"). Each report has its own phase, deadline and recipient, so the model must carry them.
- `IncidentReport.ReportId` is deterministic: a hash of breach id, regulation, phase, sequence number and target authority. It doubles as the idempotency key (Design Choice 6) and as the file name of the file-export adapter.
- The contributor seam is what #1242 needs. Its Art. 33(3) package builder becomes an `IIncidentReportContributor` that fills `CategoriesOfDataAffected`, `DpoContact`, `LikelyConsequences` and `MeasuresTaken`. Neither PR waits on the other, and the adapter contract stays fixed.
- Validation per regulation and phase (`IncidentReportValidator`) refuses an incomplete report with a coded error before any adapter is called (matrix row 5).

</details>

<details>
<summary><strong>3. Authority Path — the reporting service replaces <code>IBreachNotifier.NotifyAuthorityAsync</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Remove `IBreachNotifier.NotifyAuthorityAsync`; `IBreachReportingService` is the only authority path; `IBreachNotifier` keeps the Art. 34 subject path for #1242; remove the dead options `SupervisoryAuthority` and `AutoNotifyOnHighSeverity`** | One way to notify an authority; removes a no-op that reports `Sent` for deliveries that never happen (`DefaultBreachNotifier.cs:60-66`); removes two options nothing reads; pre-1.0 breaking change done completely (AGENTS.md §1, §3) | Breaks the public `IBreachNotifier` shape (`PublicAPI.Unshipped.txt` lines change); #1242 must build on the reduced interface |
| **B) Keep both; `DefaultBreachNotifier.NotifyAuthorityAsync` delegates to the reporting service** | No public API removal | Two entry points for the same act, with different inputs (`BreachRecord` against a breach id); the delegate needs a breach id that `BreachRecord.Id` (a string) only approximates |
| **C) Keep both, independent** | Smallest change | Leaves a no-op that claims delivery, and two dead options; contradicts "every line serves a current purpose" |

### Chosen Option: **A — The reporting service replaces the authority path of `IBreachNotifier`** (recommended, pending the maintainer)

### Rationale

- I recommend A because the current `DefaultBreachNotifier` returns `NotificationOutcome.Sent` for an authority notification that is never delivered. A compliance library must not report a submission that did not happen. The file-export adapter (Design Choice 8) is the honest default: it writes a file and reports `Exported`, never `Accepted`.
- `AutoNotifyOnHighSeverity` is removed rather than wired. Submitting to an authority without a human decision contradicts SPEC-002 §2.3 ("Encina facilitates, never blocks; the application keeps every legal decision"). An application that wants automation calls `IBreachReportingService.SubmitAsync` from its own `BreachDetected` handler.
- `SupervisoryAuthority` is superseded by the routing table (Design Choice 5).
- Coordination with #1242: its "authority channel (manual package, #812 adapter slot)" becomes the file-export adapter plus a contributor. Its subject channel stays on `IBreachNotifier.NotifyDataSubjectsAsync`. The issue body of #1242 does not change.

</details>

<details>
<summary><strong>4. Submission Persistence — new events on the breach aggregate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New events `BreachReportSubmitted` and `BreachReportSubmissionFailed` on `BreachAggregate`; a `ReportSubmissions` list on `BreachReadModel`; a successful GDPR initial report also raises the existing `BreachReportedToDPA` in the same save** | One stream is the Art. 33(5) documentation and the audit trail (ADR-019); state transition and submission record commit atomically; idempotency is checked against aggregate state | The aggregate grows; `ReportToDPA`'s "only from Detected or Investigating" guard must accept submissions to further authorities and later phases while keeping the single status transition |
| **B) Separate `IncidentReportSubmissionAggregate` stream per (breach, authority)** | Breach aggregate untouched; per-authority concurrency | Two streams to keep consistent; the breach read model does not show submissions without a second projection; status transition and submission are not atomic |
| **C) Not persisted; logged and returned only** | Smallest | Fails Art. 33(5) documentation and SPEC-002 AC-044's audit expectation; duplicate submissions cannot be detected after a restart |

### Chosen Option: **A — Events on `BreachAggregate`** (recommended, pending the maintainer)

### Rationale

- I recommend A because the breach stream is already the module's audit trail (`IBreachNotificationService.GetBreachHistoryAsync`). An authority asking "what did you send us, and when" is answered from one stream.
- `BreachReportSubmitted` carries `ReportId`, `Regulation`, `Phase`, `SequenceNumber`, `TargetAuthority`, `Format`, `Outcome`, `AuthorityReference`, `SubmittedByUserId`, `SubmittedAtUtc`, `TenantId` and `ModuleId`. `BreachReportSubmissionFailed` carries `ReportId`, `TargetAuthority`, `ErrorCode`, `IsTransient` and `AttemptedAtUtc`. Neither carries the report body, so no personal data enters the event (REQ-062). The body lives at the authority, or in the exported file.
- Transition rule: the first successful submission with `Regulation == GDPR` and `Phase == Notification` while the breach is `Detected` or `Investigating` also raises `BreachReportedToDPA`, so the existing lifecycle and `BreachDeadlineMonitorService` keep working. Every other submission is recorded without a status change, and is allowed in any status except `Closed`.
- Marten's optimistic concurrency on the stream version guards two hosts submitting the same report at once. The loser reloads, finds the `ReportId` already submitted and returns `Duplicate` (matrix row 7).

</details>

<details>
<summary><strong>5. Routing Configuration — string-keyed routes in options plus a replaceable resolver</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `options.Reporting.AddAdapter<T>()` and `Route(regulation, phase?, params targetAuthority[])`, with `RouteForTenant(tenantId, ...)` overrides, read by `IIncidentReportingRouteResolver` (default `OptionsIncidentReportingRouteResolver`, replaceable through DI)** | Routes are data and can be validated at start-up; per-tenant overrides satisfy REQ-061; an application with routes in a database replaces the resolver; adapters are identified by their `TargetAuthority` key | String keys: a typo is caught by the validator at start-up, not by the compiler |
| **B) The issue's typed builder `RouteByRegulation(r => r.ForGDPR().UseAdapter<AEPDReportingAdapter>())`** | Compile-time adapter types | One method per regulation (`ForGDPR`, `ForNIS2`, ...) grows with every regulation; two instances of one adapter type (two national CSIRTs through the generic HTTP adapter) cannot be told apart; no tenant dimension |
| **C) Keyed DI services only (`AddKeyedSingleton<IIncidentReportingAdapter>("CSIRT-ES", ...)`), no routing table** | Uses the platform | No regulation or phase routing; every application writes its own dispatch |

### Chosen Option: **A — String-keyed routes with a replaceable resolver** (recommended, pending the maintainer)

### Rationale

- I recommend A because the generic HTTP adapter (Design Choice 8) is registered once per authority (`AddHttpAdapter("CSIRT-ES", ...)`, `AddHttpAdapter("CERT-PL", ...)`), so adapters need instance keys, not only types.
- `BreachNotificationOptionsValidator` (which already exists) checks at start-up that every route names a registered adapter, that every adapter supports the regulation it is routed for, and that no route reaches an adapter with `IsSingleEntryPoint == true` while the SEP option is off (Design Choice 7). Misconfiguration fails before the first breach.
- Fail closed for tenants (SPEC-002 AC-043): a tenant without an override uses the global routes, which are application configuration and never another tenant's. If neither exists, `SubmitAsync` returns `breach.reporting.no_route`; it never picks an adapter on its own.

</details>

<details>
<summary><strong>6. Resilience and Failure Handling — keyed Polly pipeline, classified outcomes, never swallowed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Run each adapter call through the keyed pipeline `"breach-reporting"` from `ResiliencePipelineProvider<string>` when one is registered (else one attempt with `Reporting.SubmissionTimeout`); adapters classify failures as transient or permanent (ADR-029 vocabulary); every failure is recorded as `BreachReportSubmissionFailed` and returned as `Left`; idempotency key sent to the authority** | Same pattern as `NIS2ResilienceHelper.cs:29` with the swallowing removed; the application tunes retry, circuit breaker and timeout; deadlines stay visible because a failure is persisted | Adds a `Polly` package reference to BreachNotification (NIS2 already has one) |
| **B) Adapters own retries; the reporting service makes one attempt** | No Polly dependency | Every adapter reimplements retry; no uniform circuit breaker; behaviour differs per authority |
| **C) Submit through the Encina.Messaging outbox for durable retry** | Survives process restarts | Adds a hard dependency on `Encina.Messaging` and an outbox store; the outbox stores are relational (10 providers) while this module is Marten-only, so it needs the SPEC-002 REQ-037 bridge first; a deadline-bound legal report is better retried visibly than queued silently |

### Chosen Option: **A — Keyed Polly pipeline, classified outcomes, failures recorded and returned** (recommended, pending the maintainer)

### Rationale

- I recommend A because AGENTS.md §3 forbids swallowing errors in infrastructure, and a missed authority deadline is the costliest failure this module can have. A failed submission is persisted and shows on the read model. `BreachDeadlineMonitorService` already publishes `DeadlineWarningNotification` for breaches not yet reported, so a failed GDPR submission keeps raising warnings.
- Outcomes: `Accepted`, `Exported`, `Duplicate` (the authority or the aggregate already holds this `ReportId`), `Rejected` (permanent: validation refused by the authority, 4xx other than 408 and 429), `TransientFailure` (timeout, 5xx, 408, 429 with `Retry-After`). The HTTP adapter maps status codes. #1223 (outbound error taxonomy) may later move this mapping into a shared classifier; the plan names it under Prerequisites.
- Idempotency key: `IncidentReport.ReportId`, sent by the HTTP adapter as the `Idempotency-Key` header, and used by the file-export adapter as the file name, so a repeated export overwrites instead of duplicating.
- Retries that span process restarts are an application decision: the read model lists failed submissions and the application calls `SubmitAsync` again, which is idempotent.

</details>

<details>
<summary><strong>7. Omnibus Single Entry Point Switch — one option, named after the draft article, off by default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `options.Reporting.SingleEntryPoint.Enabled` (default `false`); adapters declare `IsSingleEntryPoint`; while off, the validator refuses any route to such an adapter and `SubmitAsync` never resolves one; XML docs name COM(2025) 837 and the draft article** | Exactly what SPEC-002 REQ-024 asks for; local to #812; a test proves the default; nothing changes for an application that never opts in | Each Omnibus issue (#810-#816) then has its own switch, so there is no single "Omnibus mode" |
| **B) A shared `RegulatoryProfile` enum (`CurrentLaw`, `DigitalOmnibusDraft`) consumed by #810-#816** | One switch for the whole proposal | Cross-issue coordination before any of the seven lands; the proposal may be adopted article by article, and a single profile cannot express that |
| **C) No switch: the SEP is just another adapter the application may route to** | Simplest | Violates REQ-024 ("off by default, named by its draft article"); an application could report to the SEP before the law exists, and the SEP may not accept it |

### Chosen Option: **A — `Reporting.SingleEntryPoint.Enabled`, off by default** (recommended, pending the maintainer)

### Rationale

- I recommend A because REQ-024 and AC-024 require a test showing that the default follows current law. With A the test is direct: default options, a registered SEP-flagged test adapter, and a route to it, then start-up validation fails with `breach.reporting.single_entry_point_disabled`. Without the route, GDPR and NIS2 reports go only to the national routes.
- **Current-law deadlines are the default** (AC-024): `CurrentLawReportingDeadlinePolicy` computes GDPR `Notification` = detection + 72 h; NIS2 `EarlyWarning` = detection + 24 h, `Notification` = detection + 72 h, `Final` = notification submission + 1 month. With the SEP switch on, the timelines stay the same (SPEC-002 §3.5: "timelines unchanged" for NIS2). The GDPR 96-hour deadline and the "high risk" threshold of the Omnibus belong to #813, not to this switch.
- The reporting service applies no severity filter: every breach the application chooses to report is routed. That keeps GDPR's current "risk" standard as the default (AC-024); the threshold is #813's option.
- If the maintainer later prefers one profile (B), A's option becomes one of the settings the profile flips, with no rework.

</details>

<details>
<summary><strong>8. Concrete Adapters Shipped Now — file export and a generic HTTP JSON adapter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) File-export adapter only** | No HTTP dependency; meets the issue's "at least one concrete adapter (file export)" | The issue's WireMock integration tests would test only a test double; network behaviour (idempotency header, `Retry-After`, 409 duplicate) stays unproven until the ENISA adapter |
| **B) File-export adapter plus a generic `HttpIncidentReportingAdapter` (named `HttpClient` from `IHttpClientFactory`, configurable endpoint, serializer and success codes), integration-tested with `Encina.Testing.WireMock`** | Proves the contract over a real network path; national CSIRTs that do publish APIs are usable today; the future ENISA adapter is a thin specialisation; authentication stays in the application's named `HttpClient` (and later #1233 `Encina.Http`) | Adds `Microsoft.Extensions.Http` (already in `Directory.Packages.props`); one more adapter to test |
| **C) File export plus an `EnisaSingleEntryPointAdapter` placeholder** | Visible SEP type | The SEP API is not published; a placeholder that cannot submit is dead code and invites misuse |

### Chosen Option: **B — File-export adapter plus a generic HTTP JSON adapter** (recommended, pending the maintainer)

### Rationale

- I recommend B because the issue's test matrix requires WireMock integration tests, and they only mean something against a real adapter. The generic adapter is also what the ENISA adapter will build on (`plan-812-enisa-sep-concrete-adapter.md`).
- The file-export adapter writes `{ReportId}.{ext}` (default JSON through `JsonIncidentReportSerializer`, format `encina-incident-report-v1`) to `Reporting.FileExport.Directory`, inside a per-tenant sub-folder when a tenant is set. It returns `Exported`, never `Accepted`, so the read model shows that a human must still file it, for example in the AEPD's electronic office.
- Client certificates, OAuth and rate limits are not built here. They come from the application's named `HttpClient` today and from #1233 later. The adapter only adds `Idempotency-Key` and the content type.

</details>

---

## Implementation Phases

### Phase 1: Core Models & Abstractions

> **Goal**: The regulation-neutral report model and the four extension interfaces.

<details>
<summary><strong>Tasks</strong></summary>

All files are in `src/Encina.Compliance.BreachNotification/Reporting/`, namespace `Encina.Compliance.BreachNotification.Reporting`.

1. `Model/IncidentRegulation.cs`: `public enum IncidentRegulation { GDPR, NIS2, DORA, CRA }`. XML docs: DORA and CRA are routable shapes; Encina ships no DORA module before 1.0 (#804, SPEC-002 DEC-015).
2. `Model/IncidentReportPhase.cs`: `EarlyWarning` (NIS2 Art. 23(4)(a)), `Notification` (GDPR Art. 33(1); NIS2 Art. 23(4)(b)), `Intermediate` (NIS2 Art. 23(4)(c); GDPR Art. 33(4) phased information), `Final` (NIS2 Art. 23(4)(d)), `Update`.
3. `Model/ReportSubmissionOutcome.cs`: `Accepted`, `Exported`, `Duplicate`, `Rejected`, `TransientFailure`.
4. `Model/IncidentReport.cs`: sealed record. `ReportId` (string, deterministic), `BreachId` (Guid), `Regulation`, `Phase`, `SequenceNumber` (int, 1-based per regulation and phase), `TargetAuthority`, `TenantId?`, `ModuleId?`, `Nature`, `Severity` (`BreachSeverity`), `EstimatedAffectedSubjects`, `Description`, `CategoriesOfDataAffected` (`IReadOnlyList<string>`, default empty), `DpoContact?`, `LikelyConsequences?`, `MeasuresTaken?`, `DelayReason?`, `DetectedAtUtc`, `DueAtUtc?`, `GeneratedAtUtc`, `AdditionalSections` (`IReadOnlyDictionary<string, string>`, default empty, for contributors). Static `ComputeReportId(Guid breachId, IncidentRegulation, IncidentReportPhase, int sequence, string targetAuthority)`: SHA-256 over the joined values, lower-case hex, first 32 characters.
5. `Model/IncidentReportRequest.cs`: sealed record. `BreachId`, `Regulation`, `Phase`, `SubmittedByUserId`, `DelayReason?`.
6. `Model/SerializedIncidentReport.cs`: sealed record. `Format`, `ContentType`, `FileExtension`, `Content` (`ReadOnlyMemory<byte>`).
7. `Model/ReportingResult.cs`: sealed record. `ReportId`, `TargetAuthority`, `Outcome`, `Format`, `AuthorityReference?`, `CompletedAtUtc`, `ErrorCode?`.
8. `IIncidentReportingAdapter.cs`: `string TargetAuthority { get; }`, `bool IsSingleEntryPoint { get; }`, `IReadOnlyCollection<IncidentRegulation> SupportedRegulations { get; }`, `ValueTask<Either<EncinaError, ReportingResult>> SubmitAsync(IncidentReport report, SerializedIncidentReport payload, CancellationToken cancellationToken)`.
9. `IIncidentReportingAdapterProbe.cs` (optional companion): `ValueTask<Either<EncinaError, Unit>> ProbeAsync(CancellationToken)`, used by the health check.
10. `IIncidentReportSerializer.cs`: `string Format { get; }`, `string ContentType { get; }`, `string FileExtension { get; }`, `Either<EncinaError, SerializedIncidentReport> Serialize(IncidentReport report)`.
11. `IIncidentReportBuilder.cs`: `ValueTask<Either<EncinaError, IncidentReport>> BuildAsync(BreachReadModel breach, IncidentReportRequest request, string targetAuthority, int sequenceNumber, CancellationToken)`.
12. `IIncidentReportContributor.cs`: `int Order { get; }`, `ValueTask<Either<EncinaError, IncidentReport>> ContributeAsync(IncidentReport report, BreachReadModel breach, CancellationToken)`. This is the #1242 seam.
13. `IIncidentReportingRouteResolver.cs`: `ValueTask<Either<EncinaError, IReadOnlyList<string>>> ResolveAsync(IncidentRegulation, IncidentReportPhase, string? tenantId, CancellationToken)`.
14. `IIncidentReportingDeadlinePolicy.cs`: `DateTimeOffset? ComputeDueAtUtc(BreachReadModel breach, IncidentRegulation, IncidentReportPhase)`.
15. `IBreachReportingService.cs` (in `Abstractions/`, namespace `Encina.Compliance.BreachNotification.Abstractions`): `ValueTask<Either<EncinaError, IReadOnlyList<ReportingResult>>> SubmitAsync(IncidentReportRequest request, CancellationToken cancellationToken = default)` and `ValueTask<Either<EncinaError, IReadOnlyList<ReportSubmissionSummary>>> GetSubmissionsAsync(Guid breachId, CancellationToken cancellationToken = default)`.
16. `BreachNotificationErrors.cs` (modify): codes `breach.reporting.no_route`, `breach.reporting.adapter_not_registered`, `breach.reporting.report_incomplete`, `breach.reporting.serialization_failed`, `breach.reporting.submission_failed`, `breach.reporting.submission_rejected`, `breach.reporting.single_entry_point_disabled`, `breach.reporting.breach_closed`, with factory methods following the existing pattern (`BreachNotificationErrors.cs:29-77`). Error messages never contain report content.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina is a .NET 10 / C# 14 library (nullable enabled, Railway Oriented Programming with
Either<EncinaError, T>). Issue #812 adds incident reporting adapters to
src/Encina.Compliance.BreachNotification (event-sourced on Marten, ADR-019). The plan is
docs/plans/enisa-single-entry-point-implementation-plan-812.md; Design Choices 2, 5, 6 and 7 fix the shapes.

TASK:
Create the models and interfaces of Phase 1 under
src/Encina.Compliance.BreachNotification/Reporting/ (namespace Encina.Compliance.BreachNotification.Reporting)
and IBreachReportingService under Abstractions/. Types: IncidentRegulation, IncidentReportPhase,
ReportSubmissionOutcome, IncidentReport (with static ComputeReportId: SHA-256 of breachId|regulation|phase|
sequence|targetAuthority, lower-case hex, first 32 chars), IncidentReportRequest, SerializedIncidentReport,
ReportingResult, IIncidentReportingAdapter, IIncidentReportingAdapterProbe, IIncidentReportSerializer,
IIncidentReportBuilder, IIncidentReportContributor, IIncidentReportingRouteResolver,
IIncidentReportingDeadlinePolicy, IBreachReportingService. Add the breach.reporting.* error codes and
factories to BreachNotificationErrors.cs.

KEY RULES:
- Every async method returns ValueTask<Either<EncinaError, T>> and takes a CancellationToken.
- Sealed records with init-only properties; collections default to empty, never null.
- XML docs on every public member, citing GDPR Art. 33 and NIS2 Art. 23(4) where relevant.
- IsSingleEntryPoint's XML docs name the draft: Digital Omnibus COM(2025) 837 single entry point,
  off by default (SPEC-002 REQ-024).
- Error messages never contain report content or subject identifiers (AGENTS.md §3).
- Add every public symbol to PublicAPI.Unshipped.txt (RS0016).
- No [Obsolete], no compatibility aliases.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Abstractions/IBreachNotificationService.cs
- src/Encina.Compliance.BreachNotification/Model/BreachRecord.cs
- src/Encina.Compliance.BreachNotification/Model/NotificationResult.cs
- src/Encina.Compliance.BreachNotification/BreachNotificationErrors.cs
- src/Encina.Compliance.BreachNotification/ReadModels/BreachReadModel.cs
- src/Encina.Compliance.NIS2/Model/NIS2NotificationPhase.cs
```

</details>

### Phase 2: Aggregate Events, Projection & Read Model

> **Goal**: Record submissions on the breach stream (Design Choice 4).

<details>
<summary><strong>Tasks</strong></summary>

1. `Events/BreachNotificationEvents.cs` (modify): add `BreachReportSubmitted(Guid BreachId, string ReportId, IncidentRegulation Regulation, IncidentReportPhase Phase, int SequenceNumber, string TargetAuthority, string Format, ReportSubmissionOutcome Outcome, string? AuthorityReference, string SubmittedByUserId, DateTimeOffset SubmittedAtUtc, string? TenantId, string? ModuleId)` and `BreachReportSubmissionFailed(Guid BreachId, string ReportId, IncidentRegulation Regulation, IncidentReportPhase Phase, string TargetAuthority, string ErrorCode, bool IsTransient, string SubmittedByUserId, DateTimeOffset AttemptedAtUtc, string? TenantId, string? ModuleId)`. Both implement `INotification`, like the existing events.
2. `Aggregates/BreachAggregate.cs` (modify):
   - private `HashSet<string> _submittedReportIds` and `public bool HasSubmitted(string reportId)`;
   - `public int NextSequenceNumber(IncidentRegulation regulation, IncidentReportPhase phase)`;
   - `public void RecordReportSubmission(ReportingResult result, IncidentReport report, string submittedByUserId, DateTimeOffset submittedAtUtc)`: throws `InvalidOperationException` when `Status == Closed`; raises `BreachReportSubmitted`. When `report.Regulation == GDPR && report.Phase == Notification && result.Outcome is Accepted or Exported && Status is Detected or Investigating`, it also raises `BreachReportedToDPA` (`authorityName` = `TargetAuthority`, `authorityContactInfo` = `AuthorityReference ?? TargetAuthority`, `reportSummary` = `$"{Format} report {ReportId}"`), so the existing lifecycle moves to `AuthorityNotified`;
   - `public void RecordReportSubmissionFailure(...)`: raises `BreachReportSubmissionFailed`;
   - `Apply`: track ids and sequence counters.
3. `ReadModels/BreachReadModel.cs` (modify): `public List<ReportSubmissionSummary> ReportSubmissions { get; set; } = [];` and a new record `ReportSubmissionSummary(string ReportId, IncidentRegulation Regulation, IncidentReportPhase Phase, int SequenceNumber, string TargetAuthority, ReportSubmissionOutcome Outcome, string? AuthorityReference, string? ErrorCode, DateTimeOffset AtUtc)`.
4. `ReadModels/BreachProjection.cs` (modify): implement `IProjectionHandler<BreachReportSubmitted, BreachReadModel>` and `IProjectionHandler<BreachReportSubmissionFailed, BreachReadModel>`; append to `ReportSubmissions`, update `LastModifiedAtUtc` and `Version`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Encina.Compliance.BreachNotification keeps each breach as an event-sourced BreachAggregate on Marten
(ADR-019). Issue #812 records incident report submissions on that stream (plan Design Choice 4,
docs/plans/enisa-single-entry-point-implementation-plan-812.md). Phase 1 types exist in Reporting/.

TASK:
Add the events BreachReportSubmitted and BreachReportSubmissionFailed, the aggregate methods
RecordReportSubmission, RecordReportSubmissionFailure, HasSubmitted and NextSequenceNumber, the
ReportSubmissions list and ReportSubmissionSummary record on BreachReadModel, and the two projection
handlers in BreachProjection. A successful GDPR Notification submission (Accepted or Exported) while the
breach is Detected or Investigating also raises the existing BreachReportedToDPA in the same call.

KEY RULES:
- Events carry no report body and no subject identifiers; only ids, codes, enums and timestamps.
- Copy TenantId and ModuleId from aggregate state onto each event, like the existing events.
- Closed breaches refuse submissions with InvalidOperationException (the service maps it to
  breach.reporting.breach_closed).
- Keep the existing ReportToDPA path working; do not change its guard.
- TimeProvider supplies every timestamp; never DateTime.UtcNow.
- PublicAPI.Unshipped.txt updated for every new public symbol.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs
- src/Encina.Compliance.BreachNotification/Events/BreachNotificationEvents.cs
- src/Encina.Compliance.BreachNotification/ReadModels/BreachProjection.cs
- src/Encina.Compliance.BreachNotification/ReadModels/BreachReadModel.cs
- docs/architecture/adr/019-compliance-event-sourcing-marten.md
```

</details>

### Phase 3: Default Implementations

> **Goal**: Builder, serializer, deadline policy, route resolver, both adapters and the reporting service.

<details>
<summary><strong>Tasks</strong></summary>

All in `Reporting/`, `internal sealed` unless noted.

1. `DefaultIncidentReportBuilder` (public sealed): maps `BreachReadModel` fields (`Nature`, `Severity`, `EstimatedAffectedSubjects`, `Description`, `ContainmentMeasures` → `MeasuresTaken`, `DetectedAtUtc`, `TenantId`, `ModuleId`), sets `DueAtUtc` from `IIncidentReportingDeadlinePolicy` and `ReportId` from `IncidentReport.ComputeReportId`, then applies `IEnumerable<IIncidentReportContributor>` ordered by `Order`. Dependencies: `IIncidentReportingDeadlinePolicy`, `IEnumerable<IIncidentReportContributor>`, `TimeProvider`.
2. `CurrentLawReportingDeadlinePolicy` (public sealed): GDPR `Notification` = `DetectedAtUtc + 72 h`; NIS2 `EarlyWarning` = +24 h, `Notification` = +72 h, `Final` = first successful NIS2 `Notification` submission + 1 month (null until then); `Intermediate` and `Update` = null; DORA and CRA = null. XML docs cite the articles. This is the AC-024 default.
3. `IncidentReportValidator`: required fields per regulation and phase. Every report needs `Nature`, `Description` and `DetectedAtUtc`. GDPR `Notification` also needs `EstimatedAffectedSubjects >= 0`, plus `DelayReason` when submitted after `DueAtUtc` (Art. 33(1)). The Art. 33(3) fields are required only when `Reporting.RequireArticle33Content` is `true` (default `false` until #1242 provides them). It returns `breach.reporting.report_incomplete` with the missing field **names** only.
4. `JsonIncidentReportSerializer` (public sealed): `Format = "encina-incident-report-v1"`, `application/json`, `.json`; `System.Text.Json` with a source-generated `JsonSerializerContext` (`IncidentReportJsonContext`); camelCase; enums as strings.
5. `OptionsIncidentReportingRouteResolver`: reads `IOptionsMonitor<BreachNotificationOptions>` (hot-swappable, ready for #817); tenant override first, then global; filters out SEP adapters while `SingleEntryPoint.Enabled == false`; returns `breach.reporting.no_route` when empty.
6. `FileExportIncidentReportingAdapter` (public sealed): `TargetAuthority` from options (default `"file-export"`), `IsSingleEntryPoint = false`, all regulations supported; writes `{Directory}/{tenantId?}/{ReportId}{FileExtension}` with `File.WriteAllBytesAsync(..., cancellationToken)`; overwrites, so it is idempotent; returns `Exported`; maps `IOException` and `UnauthorizedAccessException` to `TransientFailure` and `Rejected` respectively. Implements `IIncidentReportingAdapterProbe` (the directory exists and is writable).
7. `HttpIncidentReportingAdapter` (public sealed): constructed per registration with `HttpIncidentReportingAdapterOptions` (`TargetAuthority`, `Endpoint` (Uri), `HttpClientName`, `SupportedRegulations`, `IsSingleEntryPoint` (default false), `AuthorityReferenceHeader?`, `ProbeEndpoint?`); POSTs the payload with `Idempotency-Key: {ReportId}`. 2xx → `Accepted` (with `AuthorityReference` read from the configured header or `Location`); 409 → `Duplicate`; 408, 429 and 5xx → `TransientFailure` (reads `Retry-After`); other 4xx → `Rejected`; `HttpRequestException` and timeout → `TransientFailure`. Response bodies are never logged or put on errors (REQ-034).
8. `DefaultBreachReportingService` (public sealed, implements `IBreachReportingService`): loads the aggregate through `IAggregateRepository<BreachAggregate>` and the read model through the existing read path; refuses `Closed`; resolves routes; for each target authority it computes the sequence and `ReportId`, short-circuits to `Duplicate` when `HasSubmitted`, builds, validates, serializes with the configured serializer (`Reporting.DefaultFormat`), calls the adapter through the resilience helper (Phase 5), records success or failure on the aggregate, saves once per target (optimistic concurrency; on a version conflict it reloads and re-checks `HasSubmitted`), invalidates the `breach:{id}` cache key like `DefaultBreachNotificationService.cs:509`. It returns one `ReportingResult` per target; if every target failed, it returns the first `Left` after recording all failures. Dependencies: `IAggregateRepository<BreachAggregate>`, `IReadModelRepository<BreachReadModel>`, `IIncidentReportingRouteResolver`, `IIncidentReportBuilder`, `IEnumerable<IIncidentReportingAdapter>`, `IEnumerable<IIncidentReportSerializer>`, `IOptionsMonitor<BreachNotificationOptions>`, `IServiceProvider` (resilience lookup), `ICacheProvider`, `TimeProvider`, `ILogger<DefaultBreachReportingService>`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #812, plan docs/plans/enisa-single-entry-point-implementation-plan-812.md (Design Choices 2-8).
Phases 1-2 created the Reporting/ models, interfaces and the aggregate events. Encina uses ROP
(Either<EncinaError, T>), TimeProvider injection, [LoggerMessage] logging and TryAdd DI registration.

TASK:
Implement DefaultIncidentReportBuilder, CurrentLawReportingDeadlinePolicy, IncidentReportValidator,
JsonIncidentReportSerializer (source-generated JsonSerializerContext), OptionsIncidentReportingRouteResolver,
FileExportIncidentReportingAdapter, HttpIncidentReportingAdapter (IHttpClientFactory named client,
Idempotency-Key header, status-code classification) and DefaultBreachReportingService as specified in the
Phase 3 tasks of the plan.

KEY RULES:
- Never swallow a failure: every adapter failure is recorded as BreachReportSubmissionFailed and returned
  as Left (AGENTS.md §3, "Errors are never swallowed").
- Never log or attach report content, response bodies or subject identifiers; log the error code and the
  exception type through ForLogging() only.
- All file and HTTP I/O is async with the CancellationToken.
- The deadline policy defaults to current law: GDPR 72 h; NIS2 24 h / 72 h / 1 month (SPEC-002 AC-024).
- SEP-flagged adapters are never resolved while Reporting.SingleEntryPoint.Enabled is false.
- Keep each method's cyclomatic complexity low enough for CRAP <= 10 (AGENTS.md §9); split the status-code
  mapping into a single-question switch if needed.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs
- src/Encina.Compliance.NIS2/NIS2ResilienceHelper.cs (pattern only; do NOT copy its catch-all swallowing)
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationLogMessages.cs
- docs/architecture/adr/029-recoverability-error-classification.md
- src/Encina.Messaging/Recoverability/IErrorClassifier.cs (vocabulary only; no project reference)
```

</details>

### Phase 4: Configuration, DI & Authority-Path Cleanup

> **Goal**: Options, validation, registration, and removal of the dead authority path (Design Choices 3, 5, 7).

<details>
<summary><strong>Tasks</strong></summary>

1. `Reporting/IncidentReportingOptions.cs` (public sealed), exposed as `BreachNotificationOptions.Reporting`:
   - `AddAdapter<TAdapter>()`, `AddHttpAdapter(string targetAuthority, Action<HttpIncidentReportingAdapterOptions> configure)`, `AddFileExport(Action<FileExportReportingOptions>? configure = null)`;
   - `Route(IncidentRegulation regulation, params string[] targetAuthorities)`, `Route(IncidentRegulation regulation, IncidentReportPhase phase, params string[] targetAuthorities)`, `RouteForTenant(string tenantId, IncidentRegulation regulation, params string[] targetAuthorities)`;
   - `SingleEntryPoint` (`SingleEntryPointOptions { bool Enabled = false; }`; XML docs: "Digital Omnibus COM(2025) 837, draft; off by default under SPEC-002 REQ-024");
   - `DefaultFormat = "encina-incident-report-v1"`, `ResiliencePipelineKey = "breach-reporting"`, `SubmissionTimeout = 30 s`, `RequireArticle33Content = false`, `AddHealthCheck = false`.
2. `BreachNotificationOptions.cs` (modify): add `public IncidentReportingOptions Reporting { get; } = new();`; **remove** `SupervisoryAuthority` (line 135) and `AutoNotifyOnHighSeverity` (line 150) together with their XML docs and PublicAPI lines.
3. `BreachNotificationOptionsValidator.cs` (modify): every route names a registered adapter key; adapter keys are unique; each routed adapter supports the routed regulation; no route reaches an `IsSingleEntryPoint` adapter while `SingleEntryPoint.Enabled == false` (`breach.reporting.single_entry_point_disabled`); `FileExport.Directory` is set when file export is added; `SubmissionTimeout > 0`.
4. `Abstractions/IBreachNotifier.cs`, `DefaultBreachNotifier.cs` (modify): remove `NotifyAuthorityAsync`; update the XML docs to point to `IBreachReportingService`; keep `NotifyDataSubjectsAsync` (owned by #1242).
5. `Health/BreachNotificationHealthCheck.cs` (modify): keep the `IBreachNotifier` probe for the subject path; no authority probe there (Phase 5 adds `BreachReportingHealthCheck`).
6. `ServiceCollectionExtensions.cs` (modify): when at least one adapter is configured, register `TryAddScoped<IBreachReportingService, DefaultBreachReportingService>`, `TryAddSingleton<IIncidentReportBuilder, DefaultIncidentReportBuilder>`, `TryAddSingleton<IIncidentReportingDeadlinePolicy, CurrentLawReportingDeadlinePolicy>`, `TryAddSingleton<IIncidentReportingRouteResolver, OptionsIncidentReportingRouteResolver>`, `TryAddEnumerable` the JSON serializer, each configured adapter (`AddHttpClient(name)` for HTTP adapters) and `IValidateOptions` already present. The registration does nothing when `Reporting` is untouched, so the feature is opt-in (AGENTS.md §1).
7. DI test: `ValidateOnBuild = true`, `ValidateScopes = true` with reporting configured (AGENTS.md §3, registration completeness).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #812, plan docs/plans/enisa-single-entry-point-implementation-plan-812.md, Design Choices 3, 5 and 7.
Encina is pre-1.0: breaking changes are done completely, with no [Obsolete] and no aliases.

TASK:
Create IncidentReportingOptions (adapters, routes, tenant routes, SingleEntryPoint.Enabled=false,
DefaultFormat, ResiliencePipelineKey, SubmissionTimeout, RequireArticle33Content, AddHealthCheck) and hang it
on BreachNotificationOptions.Reporting. Remove BreachNotificationOptions.SupervisoryAuthority and
AutoNotifyOnHighSeverity (never read anywhere). Remove IBreachNotifier.NotifyAuthorityAsync and its
DefaultBreachNotifier implementation. Extend BreachNotificationOptionsValidator with the routing rules.
Register the reporting services in AddEncinaBreachNotification only when an adapter is configured.

KEY RULES:
- TryAdd* for every default so applications can override (register before AddEncinaBreachNotification).
- Start-up validation fails closed: unknown adapter key, unsupported regulation, or a route to a
  single-entry-point adapter while the option is off.
- PublicAPI.Unshipped.txt: add the new symbols, remove the lines of the removed members (RS0017).
- Write a DI test that builds the provider with ValidateOnBuild and ValidateScopes.
- Update every test and sample that used the removed members.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachNotificationOptions.cs
- src/Encina.Compliance.BreachNotification/BreachNotificationOptionsValidator.cs
- src/Encina.Compliance.BreachNotification/ServiceCollectionExtensions.cs
- src/Encina.Compliance.BreachNotification/Abstractions/IBreachNotifier.cs
- src/Encina.Compliance.BreachNotification/DefaultBreachNotifier.cs
- src/Encina.Compliance.BreachNotification/PublicAPI.Unshipped.txt
```

</details>

### Phase 5: Cross-Cutting Integration

> **Goal**: Resilience, idempotency, transactions, tenancy, validation, audit and health (matrix rows marked ✅).

<details>
<summary><strong>Tasks</strong></summary>

1. `Reporting/ReportingResilience.cs` (internal static): `ExecuteAsync(IServiceProvider, string pipelineKey, TimeSpan timeout, Func<CancellationToken, ValueTask<Either<EncinaError, ReportingResult>>>, CancellationToken)`. It uses `ResiliencePipelineProvider<string>.GetPipeline(key)` when the provider is registered and the key exists; otherwise one attempt under a linked `CancellationTokenSource` with `timeout`. Retries are driven by a `ShouldHandle` predicate on `ReportSubmissionOutcome.TransientFailure`. **No catch-all**: exceptions become `Left(breach.reporting.submission_failed)` with the exception type only. Add a `Polly` package reference to the csproj.
2. Idempotency: `ReportId` plus `BreachAggregate.HasSubmitted`, the `Idempotency-Key` header, and the overwriting file name. A test shows that a second `SubmitAsync` for the same request returns `Duplicate` and calls no adapter.
3. Transactions: one aggregate save per target records the submission and, when applicable, `BreachReportedToDPA`. A test shows both events in one stream version step.
4. Multi-tenancy (SPEC-002 REQ-061): the tenant comes from the aggregate's `TenantId`; routes resolve per tenant; file export writes to a tenant sub-folder; telemetry tags carry the tenant. Two-tenant test: tenant A's submission never uses tenant B's routes and never writes into B's folder. With tenancy off, there are no tenant folders and no tenant configuration.
5. Validation: `IncidentReportValidator` before every adapter call; options validated at start-up (Phase 4).
6. Audit trail: the submission events are the trail (ADR-019); `GetBreachHistoryAsync` returns them.
7. `Health/BreachReportingHealthCheck.cs` (public sealed): `DefaultName = "encina-breach-reporting"`, static `Tags` (`"encina"`, `"compliance"`, `"breach"`, `"reporting"`); resolves adapters in a scope (`IServiceProvider.CreateScope()`); runs each `IIncidentReportingAdapterProbe`; `Degraded` when a probe fails or a read model holds a failed submission past its `DueAtUtc`; registered only when `Reporting.AddHealthCheck == true`.
8. Cache: invalidate `breach:{id}` after every save (reuses the existing pattern); no new cached read.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #812, plan docs/plans/enisa-single-entry-point-implementation-plan-812.md, Design Choice 6 and the
Cross-Cutting Integration Matrix. AGENTS.md §6 (ADR-018) requires each ✅ function to be integrated.
SPEC-002 REQ-061 (tenancy) and REQ-062 (observability, health) apply.

TASK:
Add ReportingResilience (keyed Polly pipeline "breach-reporting" via ResiliencePipelineProvider<string>,
timeout fallback, no catch-all), the idempotency short-circuit, the single-save transaction rule, per-tenant
routing and file folders, report validation before submission, and BreachReportingHealthCheck with
adapter probes. Add the Polly package reference to Encina.Compliance.BreachNotification.csproj.

KEY RULES:
- A failure is recorded and returned as Left; it is never converted into success or dropped.
- Missing tenant routes fall back only to the global routes; never to another tenant's (AC-043).
- Health check: DefaultName const, static Tags, scoped resolution through CreateScope().
- No payloads, response bodies or subject identifiers in errors, logs, tags or health data.

REFERENCE FILES:
- src/Encina.Compliance.NIS2/NIS2ResilienceHelper.cs (pattern, minus the swallowing)
- src/Encina.Compliance.BreachNotification/Health/BreachNotificationHealthCheck.cs
- src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs
- src/Encina/Abstractions/IRequestContext.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-061, REQ-062, AC-043, AC-044)
```

</details>

### Phase 6: Observability

> **Goal**: Traces, metrics and `[LoggerMessage]` logs inside a newly registered EventId range.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs` (modify): register `public static readonly (int Min, int Max) ComplianceBreachReporting = (9700, 9749);` with the summary "Encina.Compliance.BreachNotification — incident reporting adapters (#812)". `ComplianceBreachNotification` (8700-8799) has only 8792-8799 free (54 ids used, max 8791), which is not enough; 8950-8999 is claimed by the #1189 plan.
2. `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (modify): map `"Encina.Compliance.BreachNotification"` to `[nameof(EventIdRanges.ComplianceBreachNotification), nameof(EventIdRanges.ComplianceBreachReporting)]` (line 62).
3. `Diagnostics/BreachReportingLogMessages.cs`: `[LoggerMessage]` 9700 onward, packed: submission started, submitted (outcome), duplicate skipped, no route, adapter not registered, report incomplete (field names), serialization failed, transient failure (code, attempt), rejected (code), failure recorded, SEP route refused, deadline passed without delay reason, probe failed, file exported. XML doc names the range ("Event IDs: 9700-97xx (see EventIdRanges.ComplianceBreachReporting)").
4. `Diagnostics/BreachNotificationDiagnostics.cs` (modify): reuse the package `ActivitySource` and `Meter` (`SourceName = "Encina.Compliance.BreachNotification"`, line 31). Add activity `BreachNotification.ReportSubmission` with tags `breach.reporting.regulation`, `breach.reporting.phase`, `breach.reporting.authority`, `breach.reporting.outcome`, `encina.tenant_id`. Add counter `breach.reporting.submissions.total` (same tags), counter `breach.reporting.duplicates.total`, and histogram `breach.reporting.submission.duration.ms`. The breach id is **not** a tag (it identifies an incident, not a subject, but stays off metrics because of its cardinality); the report id is never a tag.
5. Add the new field to `src/Encina/PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #812, plan docs/plans/enisa-single-entry-point-implementation-plan-812.md. ADR-021: every
[LoggerMessage] EventId lives in a range registered in src/Encina/Diagnostics/EventIdRanges.cs and the
assembly is mapped in EncinaEventIdAllocationTests. SPEC-002 REQ-062 forbids payloads, response bodies
and subject identifiers in telemetry.

TASK:
Register ComplianceBreachReporting = (9700, 9749), map it as a second range for
Encina.Compliance.BreachNotification in EncinaEventIdAllocationTests.AssemblyRanges, write
Diagnostics/BreachReportingLogMessages.cs with packed EventIds from 9700, and extend
BreachNotificationDiagnostics with the ReportSubmission activity, the two counters and the histogram.

KEY RULES:
- Register the range BEFORE writing any [LoggerMessage]; sequential ids, no gaps.
- Tags: regulation, phase, authority, outcome, tenant id. Never report content, response bodies,
  breach ids on metrics, or subject identifiers.
- Start activities only when ActivitySource.HasListeners(), as the existing helpers do.
- Run the architecture tests (EncinaEventIdAllocationTests, EventIdUniquenessRule).

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationDiagnostics.cs
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationLogMessages.cs
- docs/architecture/adr/021-eventid-uniqueness-enforcement.md
```

</details>

### Phase 7: Testing

> **Goal**: Every flag of `.github/coverage-manifest/Encina.Compliance.BreachNotification.json` reaches its target on the new and touched files, with CRAP ≤ 10 on changed methods.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/BreachNotification/Reporting/`):
   - `IncidentReportTests` (deterministic `ReportId`);
   - `CurrentLawReportingDeadlinePolicyTests`. **AC-024**: GDPR 72 h; NIS2 24 h / 72 h / final = notification + 1 month;
   - `IncidentReportValidatorTests`;
   - `JsonIncidentReportSerializerTests` (snapshot through `Encina.Testing.Verify`);
   - `OptionsIncidentReportingRouteResolverTests` (tenant override, global fallback, no route, SEP filtered);
   - `FileExportIncidentReportingAdapterTests` (temp directory, overwrite, tenant folder, error mapping);
   - `HttpIncidentReportingAdapterTests` (fake `HttpMessageHandler`: 2xx, 409, 408, 429 with `Retry-After`, 4xx, 5xx, timeout; `Idempotency-Key` header present);
   - `DefaultBreachReportingServiceTests` (NSubstitute `IAggregateRepository<BreachAggregate>`: routing to several targets, duplicate short-circuit, closed breach, failure recorded and returned, GDPR transition raises `BreachReportedToDPA`, cache invalidated);
   - `BreachAggregateReportingTests`;
   - `BreachProjectionReportingTests`;
   - `ReportingResilienceTests` (pipeline present, key missing, no provider; no swallowing);
   - `BreachReportingHealthCheckTests`;
   - `BreachNotificationOptionsValidatorReportingTests`. **AC-024**: default options plus a route to a SEP-flagged adapter fail validation;
   - `ReportingRegistrationTests` (`ValidateOnBuild`, `ValidateScopes`; nothing registered when reporting is unconfigured);
   - `ReportingTelemetryTests` (in-memory exporter: activity and metric carry the tenant; no tag holds content or ids; REQ-062 / AC-044).
2. **Guard** (`tests/Encina.GuardTests/Compliance/BreachNotification/Reporting/`): every public constructor and method of the new public types.
3. **Contract** (`tests/Encina.ContractTests/Compliance/BreachNotification/Reporting/`): `IIncidentReportingAdapterContractTests` as an abstract base run against the file-export adapter and the HTTP adapter (over a fake handler). It checks: returns `Right` with the report's `ReportId`; never `Accepted` from file export; a repeated submit of the same `ReportId` is idempotent; cancellation honoured. Also `IIncidentReportSerializerContractTests` and `IBreachReportingServiceContractTests`. Update `IBreachNotifierContractTests` and `DefaultBreachNotifierContractTests` for the removed method.
4. **Property** (`tests/Encina.PropertyTests/Compliance/BreachNotification/Reporting/`): FsCheck. `ReportId` is deterministic and distinct across any differing input; resolved routes never contain a SEP adapter while the option is off; resolved routes for tenant A never contain an authority configured only for tenant B; JSON serialization round-trips every generated `IncidentReport`.
5. **Integration** (`tests/Encina.IntegrationTests/Compliance/BreachNotification/Reporting/`, `[Collection(MartenCollection.Name)]`, `[Trait("Category", "Integration")]`):
   - `BreachReportingMartenIntegrationTests`: submissions persist on the stream, the projection fills `ReportSubmissions`, concurrent duplicate submission yields one `BreachReportSubmitted`, two-tenant isolation;
   - `HttpIncidentReportingAdapterWireMockTests` (`Encina.Testing.WireMock` fixture): header present, 409, 429 with `Retry-After` and the Polly pipeline retrying, 500 recorded as failure;
   - `FileExportIntegrationTests`: real directory.
6. **Load**: `tests/Encina.LoadTests/Compliance/BreachNotification/Reporting.md` justification (reports are a handful per incident; concurrency is covered by the integration duplicate test).
7. **Benchmark**: `tests/Encina.BenchmarkTests/Compliance/BreachNotification/Reporting.md` justification (not a hot path).
8. Coverage manifest: add per-file targets with one-sentence justifications for every new and touched file in `.github/coverage-manifest/Encina.Compliance.BreachNotification.json`; run `--check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Issue #812, plan docs/plans/enisa-single-entry-point-implementation-plan-812.md. AGENTS.md §9: per-flag
coverage (unit, guard, contract, property, integration) against
.github/coverage-manifest/Encina.Compliance.BreachNotification.json; tests execute real package code;
CRAP <= 10 on changed methods; Shouldly via Encina.Testing.Shouldly; NSubstitute for IAggregateRepository;
Marten integration tests use the shared [Collection(MartenCollection.Name)] fixture.

TASK:
Write the unit, guard, contract, property and integration tests listed in the Phase 7 tasks, the two
.md justifications (load, benchmark) and the per-file manifest targets with justifications. Include the
AC-024 tests: default deadlines follow current law, and a route to a single-entry-point adapter fails
validation while SingleEntryPoint.Enabled is false. Include the AC-043 two-tenant test and the AC-044
telemetry test with an in-memory exporter.

KEY RULES:
- Never reflection-only tests; instantiate the real types.
- No Thread.Sleep; use FakeTimeProvider for deadlines.
- WireMock through Encina.Testing.WireMock; never the raw library.
- Measure: dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory
  artifacts\coverage\<Flag>Tests, then dotnet run --file .github/scripts/coverage-report.cs -- --input
  artifacts/coverage --output artifacts/coverage-report, and --check-justifications.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Compliance/BreachNotification/BreachNotificationAggregateIntegrationTests.cs
- tests/Encina.ContractTests/Compliance/BreachNotification/IBreachNotifierContractTests.cs
- tests/Encina.PropertyTests/Compliance/BreachNotification/BreachNotificationOptionsPropertyTests.cs
- tests/Encina.IntegrationTests/Testing/WireMock/EncinaWireMockFixtureTests.cs
- tests/Encina.BenchmarkTests/Compliance/BreachNotification/BreachNotification.md
- docs/testing/coverage-measurement-methodology.md
```

</details>

### Phase 8: Documentation & Finalization

> **Goal**: Every mandatory documentation artifact, build and test verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public API (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`). `SingleEntryPointOptions` and `IIncidentReportingAdapter.IsSingleEntryPoint` name COM(2025) 837 as draft law, off by default (REQ-024).
2. `changelog.d/812-incident-reporting-adapters.added.md` (adapters, routing, SEP slot off by default) and `changelog.d/812-breach-notifier-authority-path.removed.md` (`IBreachNotifier.NotifyAuthorityAsync`, `SupervisoryAuthority`, `AutoNotifyOnHighSeverity`).
3. `src/Encina.Compliance.BreachNotification/README.md`: an "Authority reporting" section with configuration and the current-law default.
4. `docs/features/breach-notification.md`: rewrite "Notification Workflow → Authority Notification (Article 33)" around `IBreachReportingService`; add "Routing to several authorities" and "Digital Omnibus single entry point (draft, off)". Follow `.claude/skills/encina-docs/SKILL.md` (one Diátaxis quadrant per page; a how-to may be split out).
5. `docs/INVENTORY.md`: the new `Reporting/` files.
6. ADR at the next free number in `docs/architecture/adr/`: "Incident reporting adapters on the breach stream" (Design Choices 1, 4, 6 and 7 as decided by the maintainer); add it to `docs/architecture/adr/index.md`.
7. `ROADMAP.md`: mark #812's abstraction delivered in v0.15.0 and the ENISA adapter pending on the ENISA specification.
8. `PublicAPI.Unshipped.txt` of `Encina.Compliance.BreachNotification` and `Encina`: complete (RS0016 and RS0017 clean).
9. The article-coverage specification #1208 gets a row for COM(2025) 837 SEP (Partial: slot, adapter pending); note it on #1208 rather than editing a specification that does not exist yet.
10. `docs/releases/v0.15.0/` notes, if the folder exists at implementation time.
11. Verification: `dotnet build Encina.slnx --configuration Release` gives 0 errors and 0 warnings; `dotnet test` passes; every coverage flag reaches its manifest target; the `crap-gate` table is measured locally.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
Issue #812, plan docs/plans/enisa-single-entry-point-implementation-plan-812.md. Documentation follows
.claude/skills/encina-docs/SKILL.md; changelog fragments follow changelog.d/README.md; never edit the
[Unreleased] section of CHANGELOG.md by hand.

TASK:
Complete XML docs, write the two changelog fragments, update the package README,
docs/features/breach-notification.md, docs/INVENTORY.md, ROADMAP.md, the ADR (next free number) and its
index entry, the PublicAPI files, and run the build and test verification.

KEY RULES:
- English only; no hand-typed coverage figures (use covref markers, SPEC-001).
- The SEP option is documented as draft law (COM(2025) 837), off by default, with current law as the
  default (72 h and "risk" for GDPR Art. 33; NIS2 24 h / 72 h / 1 month).
- Say plainly that the file-export adapter does not submit anything: the organisation files the export.
- Zero warnings; every coverage flag at its manifest target.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/README.md
- docs/features/breach-notification.md
- docs/architecture/adr/index.md
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Article / Section | Relevance |
|----------|-------------------|-----------|
| GDPR (Reg. (EU) 2016/679) | Art. 33(1) | 72-hour notification to the supervisory authority; reasons for delay; current-law default |
| GDPR | Art. 33(3) | Minimum content of the notification; filled by the #1242 contributor |
| GDPR | Art. 33(4) | Information in phases; `IncidentReportPhase.Intermediate` and `Update` |
| GDPR | Art. 33(5) | Documentation of every breach; submission events on the stream |
| NIS2 (Dir. (EU) 2022/2555) | Art. 23(4)(a)-(d) | Early warning 24 h, notification 72 h, intermediate, final 1 month; `CurrentLawReportingDeadlinePolicy` |
| Digital Omnibus COM(2025) 837 (draft) | Single entry point (issue: Arts. 6-9; SPEC-002 §3.5: GDPR Art. 33(1) as amended, NIS2 Art. 23a) | SEP adapter slot, off by default (SPEC-002 REQ-024, DEC-012) |
| DORA (Reg. (EU) 2022/2554) | Art. 19 | Major ICT incident reporting; enum value only, module post-1.0 (#804) |
| CRA (Reg. (EU) 2024/2847) | Arts. 14 and 16 | Reporting through the ENISA single reporting platform; enum value only; Encina itself is not a manufacturer (SPEC-002 DEC-010) |
| SPEC-002 | REQ-024, DEC-012, AC-024 | Draft law off by default; test that the default follows current law |
| SPEC-002 | REQ-061, REQ-062, AC-043, AC-044 | Tenant-aware and instrumented; no personal data in telemetry |
| RFC 9110 | §10.2.3 `Retry-After`; status semantics | HTTP adapter classification |
| IETF draft `Idempotency-Key` header (httpapi WG) | Whole draft | Header sent by the HTTP adapter |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `BreachAggregate` | `src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs` | New submission events; `ReportToDPA` transition reused (line 252) |
| `BreachReadModel`, `BreachProjection` | `src/Encina.Compliance.BreachNotification/ReadModels/` | `ReportSubmissions` list; two new handlers |
| `DefaultBreachNotificationService` | `src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs` | Aggregate load/save and cache invalidation pattern (lines 165-206, 509) |
| `BreachNotificationOptionsValidator` | `src/Encina.Compliance.BreachNotification/BreachNotificationOptionsValidator.cs` | Routing validation at start-up |
| `BreachNotificationDiagnostics` | `src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationDiagnostics.cs` | Shared `ActivitySource` and `Meter` (line 31) |
| `BreachDeadlineMonitorService` | `src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs` | Keeps warning while the GDPR report has not succeeded |
| `NIS2ResilienceHelper` | `src/Encina.Compliance.NIS2/NIS2ResilienceHelper.cs` | Keyed-pipeline pattern (without swallowing) |
| `DefaultNIS2IncidentHandler` | `src/Encina.Compliance.NIS2/DefaultNIS2IncidentHandler.cs` | Puts NIS2 incidents on the breach stream (line 141) |
| `IErrorClassifier`, ADR-029 | `src/Encina.Messaging/Recoverability/IErrorClassifier.cs` | Transient and permanent vocabulary (no reference) |
| `IRequestContext` | `src/Encina/Abstractions/IRequestContext.cs` | Tenant and idempotency semantics (lines 109, 118) |
| `Encina.Testing.WireMock` | `src/Encina.Testing.WireMock/` | HTTP adapter integration tests |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Second range for the assembly (line 62) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.BreachNotification` (existing) | 8700-8799 (`ComplianceBreachNotification`) | 54 ids used, highest 8791; 8792-8799 too few for ~14 messages |
| `Encina.Compliance.BreachNotification` (reporting) | **9700-9749** (new `ComplianceBreachReporting`) | Free per AGENTS.md §7 (9700-9999); 8950-8999 is claimed by the #1189 plan; register first (ADR-021) and map as a second range of the assembly |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Models and enums | 8 | `Reporting/Model/` |
| Interfaces | 9 | adapters, serializer, builder, contributor, resolver, deadline policy, probe, service |
| Implementations | 9 | builder, deadline policy, validator, serializer and JSON context, resolver, two adapters, service, resilience |
| Options | 4 | `IncidentReportingOptions`, `SingleEntryPointOptions`, `FileExportReportingOptions`, `HttpIncidentReportingAdapterOptions` |
| Health and diagnostics | 2 new, 1 modified | `BreachReportingHealthCheck`, `BreachReportingLogMessages`; diagnostics extended |
| Modified production files | 11 | aggregate, events, read model, projection, options, validator, notifier interface and default, DI, errors, `EventIdRanges` |
| Tests | ~26 | unit 15, guard 2, contract 3 (+2 updated), property 1, integration 3, justifications 2 |
| Documentation | 8 | README, feature doc, INVENTORY, ADR and index, ROADMAP, 2 changelog fragments |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete implementation prompt (all phases)</strong></summary>

```text
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable on, ROP with Either<EncinaError, T>, TimeProvider
injection, [LoggerMessage] logging with EventIds in registered ranges, TryAdd DI). AGENTS.md binds every
change. Encina.Compliance.BreachNotification is event-sourced on Marten (ADR-019, PostgreSQL only in 1.0 per
SPEC-002 DEC-008 (a)). Issue #812 adds incident reporting adapters and an off-by-default slot for the
Digital Omnibus (COM(2025) 837, draft) ENISA single entry point (SPEC-002 REQ-024, DEC-012, AC-024).
Plan: docs/plans/enisa-single-entry-point-implementation-plan-812.md (follow the Design Choices as decided
in its Maintainer Decisions section).

IMPLEMENTATION OVERVIEW:
1. Reporting/ models (IncidentRegulation, IncidentReportPhase, IncidentReport with deterministic ReportId,
   ReportingResult, ReportSubmissionOutcome) and interfaces (IIncidentReportingAdapter,
   IIncidentReportSerializer, IIncidentReportBuilder, IIncidentReportContributor,
   IIncidentReportingRouteResolver, IIncidentReportingDeadlinePolicy, IBreachReportingService).
2. BreachAggregate events BreachReportSubmitted and BreachReportSubmissionFailed; read model
   ReportSubmissions; a GDPR Notification success also raises BreachReportedToDPA in the same save.
3. Defaults: builder with contributors, CurrentLawReportingDeadlinePolicy (GDPR 72 h; NIS2 24 h/72 h/1
   month), validator, JSON serializer (encina-incident-report-v1), options route resolver, file-export
   adapter (Exported, never Accepted), generic HTTP adapter (Idempotency-Key, status classification),
   DefaultBreachReportingService.
4. Options under BreachNotificationOptions.Reporting (routes, tenant routes, SingleEntryPoint.Enabled=false);
   remove IBreachNotifier.NotifyAuthorityAsync, SupervisoryAuthority, AutoNotifyOnHighSeverity.
5. Cross-cutting: keyed Polly pipeline "breach-reporting" without swallowing, idempotency, single-save
   transaction, tenant-aware routing and folders, validation, BreachReportingHealthCheck.
6. Observability: register ComplianceBreachReporting = (9700, 9749); BreachReportingLogMessages; activity
   and metrics on the package ActivitySource/Meter with tenant tag and no content.
7. Tests on every flag, including AC-024, AC-043 and AC-044 tests; load and benchmark .md justifications.
8. Documentation, changelog fragments, ADR, PublicAPI, zero warnings.

KEY PATTERNS:
- Fail closed and never swallow: every failure is persisted as an event and returned as Left.
- No payloads, response bodies or subject identifiers in events, errors, logs, tags or health data.
- Draft law off by default and named by its draft article in XML docs.
- Pre-1.0: remove dead members completely; no [Obsolete], no aliases.
- Per-flag coverage targets with justifications; CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs
- src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs
- src/Encina.Compliance.BreachNotification/Abstractions/IBreachNotifier.cs
- src/Encina.Compliance.BreachNotification/BreachNotificationOptions.cs
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationDiagnostics.cs
- src/Encina.Compliance.NIS2/NIS2ResilienceHelper.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Reports are never cached (issue); the only cache work is invalidating the existing `breach:{id}` read-model key after each save, as the current service does |
| 2 | OpenTelemetry | ✅ | `BreachNotification.ReportSubmission` activity, `breach.reporting.submissions.total`, `breach.reporting.duplicates.total`, `breach.reporting.submission.duration.ms`; tenant tag; no content (Phase 6, AC-044) |
| 3 | Structured Logging | ✅ | `BreachReportingLogMessages` in the new `ComplianceBreachReporting` range 9700-9749 (ADR-021, Phase 6) |
| 4 | Health Checks | ✅ | `BreachReportingHealthCheck` with adapter probes (export directory writable, optional HTTP probe endpoint) and overdue failed submissions; the issue deferred it, SPEC-002 REQ-062 requires it (Phase 5) |
| 5 | Validation | ✅ | `IncidentReportValidator` before every submission; routing rules in `BreachNotificationOptionsValidator` at start-up (Phases 3-4) |
| 6 | Resilience | ✅ | Keyed Polly pipeline `breach-reporting`, timeout fallback, transient and permanent classification, no swallowing (Design Choice 6, Phase 5) |
| 7 | Distributed Locks | ❌ | Concurrent submissions of one breach are serialised by Marten's optimistic concurrency on the stream plus the `ReportId` check; no shared state outside the stream |
| 8 | Transactions | ✅ | Submission event and the `BreachReportedToDPA` transition commit in one aggregate save (Design Choice 4, Phase 5) |
| 9 | Idempotency | ✅ | Deterministic `ReportId`: aggregate short-circuit, `Idempotency-Key` header, overwriting export file name (Phase 5) |
| 10 | Multi-Tenancy | ✅ | Tenant from the aggregate; per-tenant routes with fail-closed fallback; tenant export folders; two-tenant test; the issue deferred it, SPEC-002 REQ-061 requires it (Phase 5) |
| 11 | Module Isolation | ❌ | No module-scoped routing is needed; `ModuleId` is copied onto the new events like the existing breach events, and SPEC-002 requires no module scoping |
| 12 | Audit Trail | ✅ | `BreachReportSubmitted` and `BreachReportSubmissionFailed` on the breach stream are the audit trail (ADR-019); returned by `GetBreachHistoryAsync` |

No row is ⏭️. The only deferred work is the concrete ENISA SEP adapter, a feature that waits for an external specification rather than a cross-cutting function. Its issue file is `artifacts/issues/plan-812-enisa-sep-concrete-adapter.md`.

---

## Prerequisites & Dependencies

### Must coordinate

| Item | State (2026-10-06) | Relationship |
|------|--------------------|--------------|
| #1242 Breach notifier channels: Art. 33(3) package and Art. 34 communications (P-40) | Open | Its "authority channel (#812 adapter slot)" is this plan's file-export adapter plus an `IIncidentReportContributor`. Either can land first; whichever lands second wires the contributor. Design Choice 3 reduces `IBreachNotifier` to the subject path that #1242 builds on |
| #813 Authority notification severity threshold (Omnibus, off by default) | Open | Independent. The 96-hour deadline and the "high risk" threshold live there; this plan applies no severity filter |
| #817 Compliance options to `IOptionsMonitor` | Open | The route resolver already reads `IOptionsMonitor<BreachNotificationOptions>` |
| #1208 Article-coverage specification for BreachNotification | Open | Must list the SEP slot as Partial (draft, off) with REQ-024 wording |

### Advisable, not blocking

| Item | State | Relationship |
|------|-------|--------------|
| #1223 Outbound error taxonomy and `Retry-After`-aware classification | Open | The HTTP adapter's status mapping can move into it later |
| #1233 `Encina.Http` (auth providers, client certificates, rate limiting) | Open | The HTTP adapter uses a named `HttpClient`; #1233 will supply authentication without an adapter change |
| #804 DORA module | Open, post-1.0 (DEC-015) | `IncidentRegulation.DORA` exists for routing; no DORA content builder |

### Missing prerequisites and findings, written as issue files

| File | Template | Finding |
|------|----------|---------|
| `artifacts/issues/plan-812-enisa-sep-concrete-adapter.md` | `[FEATURE]` | The concrete ENISA SEP adapter (`Encina.Compliance.ENISA`) has no issue; it is blocked on ENISA publishing the API |
| `artifacts/issues/plan-812-breach-deadline-hours-ignored.md` | `[BUG]` | `BreachNotificationOptions.NotificationDeadlineHours` is ignored: `BreachAggregate.cs:457` and `BreachProjection.cs:68` hard-code 72 h |
| `artifacts/issues/plan-812-stale-sqlite-internalsvisibleto.md` | `[DEBT]` | 13 `src/` csproj files still grant `InternalsVisibleTo` to the removed `Encina.ADO.Sqlite` and `Encina.Dapper.Sqlite` (ADR-024) |

---

## Next Steps

1. The maintainer decides Design Choices 1-8; the orchestrator records them in a "Maintainer Decisions" section and updates each Chosen Option.
2. Open the three issue files above (`open-issue` skill) and link them here and from #812.
3. Agree the ordering with #1242. Recommendation: #812 Phases 1-4 first, so #1242 builds its contributor and subject channel on the reduced `IBreachNotifier`.
4. Link this plan from #812, then start Phase 1 in a worktree (`worker-brief` skill), with the local CRAP table required in the brief.
5. After the merge, note the SEP row on #1208 and re-verify SPEC-002 §3.5 within 30 days of any publication of COM(2025) 837 in the Official Journal (AC-025).
