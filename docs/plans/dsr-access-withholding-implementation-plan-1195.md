# Implementation Plan: `Encina.Compliance.DataSubjectRights` — DSR Access Export with Field-Level Exclusions and Withholding Reasons

> **Issue**: [#1195](https://github.com/dlrivada/Encina/issues/1195)
> **Type**: Feature, [Tier B] (SPEC-002 REQ-009, AC-009, scenario S15 shape part, tracking id P-07, P0)
> **Milestone**: EU regulatory readiness (parent EPIC #1186)
> **Complexity**: Medium-High (8 phases, one package, no database provider matrix)
> **Estimated Scope**: ~700-900 lines of production code + ~1,500-1,900 lines of tests; three pull requests (see Scope)

---

## Summary

Let a DSR access export **leave out declared fields or records** (third parties' data, the professional's subjective annotations, serious-harm withholding) and **record the reason and legal basis of every exclusion**, so that the export never contains what the law says to withhold and the controller can still state why (GDPR Art. 12 and 15; Ley 41/2002 art. 18.3; Código Deontológico art. 42, as cited in the issue).

### What the issue asks and what the code does today

- The issue asks for: a withholding classification on `[PersonalData]` (for example `Withhold = WithholdReason.SubjectiveAnnotation | ThirdPartyData`); a runtime withholding policy the application can register (serious-harm decisions); and an `AccessResponse` that carries, per excluded field or record, the reason and its legal basis, with the excluded values never present.
- The issue body is **settled by elimination** (comment of 2026-10-17): both the attribute classification and the runtime policy are implemented (a combined option); post-filtering by the application and silent exclusion are rejected (GDPR Art. 12 and 15 require stating the reason).
- Today `DefaultDSRService.HandleAccessAsync` (`Services/DefaultDSRService.cs:535-587`) calls `IPersonalDataLocator.LocateAllDataAsync` and returns every location in `AccessResponse.Data` with its `CurrentValue`; there is no filter. `AccessResponse` (`Model/AccessResponse.cs`) has `SubjectId`, `Data`, `ProcessingActivities`, `GeneratedAtUtc`. `PersonalDataAttribute` (`Attributes/PersonalDataAttribute.cs`) has `Category`, `Erasable`, `Portable`, `LegalRetention`, `RetentionReason` and no withholding marker.
- **The classification cannot ride on the locator.** Locators build `PersonalDataLocation` themselves (`src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs:139-148` copies `Category`, `Erasable`, `Portable`, `LegalRetention` from the attribute); `PersonalDataMap` is internal and no service uses it. A custom locator that forgets a new field would leak, so the plan resolves the attribute at export time from `PersonalDataLocation.EntityType` and `FieldName` (Design Choice 1).
- The access export is not tied to a request aggregate: `AccessRequest(SubjectId, IncludeProcessingActivities)` (`Requests/AccessRequest.cs`) has no request id, and `DSRAuditEntry` (`Model/DSRAuditEntry.cs`) is a model with no store. The issue's audit row ("each exclusion and its reason are recorded with the DSR request") therefore needs a request id on the access call and an event on the request stream (Design Choice 4).
- The portability exporter (`Export/DefaultDataPortabilityExporter.cs:93-100`) filters only on `IsPortable`; it would still export a field classified for withholding (Design Choice 5).

### Scope

- **Affected package**: `Encina.Compliance.DataSubjectRights` (plus tests, README, feature guide, coverage manifest, changelog fragment, `PublicAPI.Unshipped.txt`) and `src/Encina/Diagnostics/EventIdRanges.cs` (a second DSR EventId range, see Research).
- **Provider category**: none of the AGENTS.md §5 matrices. The module is event-sourced on Marten (SPEC-002 DEC-008 (a), ADR-019); unit tests mock `IAggregateRepository`, integration tests run on Marten/PostgreSQL through Testcontainers (AGENTS.md §3).
- **Estimated files**: ~9 new source files, ~14 modified source files, ~14 test files, ~5 documentation files.
- **Pull request split** (exceeds 15 files and 1,500 lines): **PR 1** = Phases 1-2 (types, attribute, policy, evaluator, options; ~14 files); **PR 2** = Phases 3, 5 and 6 (service integration, request-stream event, cross-cutting, observability, EventId range; ~14 files); **PR 3** = Phases 4, 7 (rest) and 8 (portability, remaining tests, documentation).
- **Event IDs**: a new second range `ComplianceDSRExtensions = (9800, 9849)` registered by this plan; 9800-9805 used here, 9806-9813 reserved for #2700.

### Related open issues that touch the same files

- #1197 (representation and deceased status) edits `DSRRequestSubmitted`, `IDSRService`, `DefaultDSRService`, the projection and the read model; #1937 (declared-but-never-emitted DSR instruments) edits `HandleAccessAsync` instrumentation; #814 is closed and its denial code is on `main`. Order is in Prerequisites & Dependencies.
- #2700 (DSR rules) reads the same service but adds rules before processing; it does not touch the access filter.
- P-02 (DSR erasure arbitration) is related by the issue; it touches erasure, not access.

---

## Design Choices

<details>
<summary><strong>1. Where the static classification is read — from the attribute at export time, by entity type and field name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Locators copy `Withhold` into a new `PersonalDataLocation.Withhold` property, as they do for `Erasable` and `Portable`** | Same pattern as the existing flags; no reflection at export time | Every locator (the Marten one and every custom one) must be changed; a locator that forgets silently discloses |
| **B) The service resolves `[PersonalData]` for each `(EntityType, FieldName)` of a location with a cached reflection lookup and applies the classification itself** | One place; custom locators cannot cause a leak; no change to `IPersonalDataLocator` or the Marten locator | Reflection (cached per type and field); a location whose `EntityType` has no CLR property for `FieldName` has no static classification |
| **C) Both: B as the guarantee, A as an optional hint** | Covers non-CLR sources | Two sources of truth to keep in step |

### Chosen Option: **B — the service resolves the attribute at export time**

Class: Settled
Settled by: `AGENTS.md` §3 (compliance gates fail closed: a locator that omits the classification must not cause disclosure), `src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs:139-148` (locators build the location themselves)

### Rationale

- A location whose entity type has no property of that name cannot be classified statically; the runtime policy (Design Choice 2) is the route for such sources, and the evaluator logs EventId 9805 once per unresolvable `(EntityType, FieldName)` so the gap is visible.
- `PersonalDataField` (`Model/PersonalDataField.cs`) gains `Withhold` so the existing startup scan stays consistent with the attribute.

</details>

<details>
<summary><strong>2. Runtime withholding policy — an enumerable of policies, union of reasons, an error fails the export</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IAccessWithholdingPolicy` registered zero or more times; the evaluator asks all of them and withholds when any says so; a `Left` from any policy fails the access request** | Several concerns coexist (serious-harm review, third-party rules); no policy can silently re-allow what another withheld; errors never read as "allow" | A policy cannot veto an attribute classification (by design) |
| **B) A single policy slot (`TryAdd`), the application replaces it** | Simplest | Two applications of different rules must compose by hand; the default would need to be a no-op that hides the extension point |
| **C) Policy may also *release* an attribute-classified field** | Handles "the patient's own annotation" cases | A release path in a withholding gate is the permissive direction; undermines the classification |

### Chosen Option: **A — enumerable policies, union, error fails**

Class: Settled
Settled by: issue #1195 Proposed Solution ("the application can register a runtime withholding policy"), `AGENTS.md` §3 (errors are never swallowed in background infrastructure; a `Left` fails the operation)

### Rationale

- The policy input is `AccessWithholdingContext(SubjectId, TenantId, EntityType, EntityId, FieldName, Category, StaticReasons)`; the output is `Either<EncinaError, AccessWithholdingDecision>` with `Reasons` and an optional `LegalBasis` and may withhold a whole record (`FieldName` null in the exclusion).
- The default registration is the attribute classification only; with no custom policy registered, nothing runtime is asked.

</details>

<details>
<summary><strong>3. Legal basis of an exclusion — an options table keyed by reason, with the statutory defaults from the issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Constants in code (Ley 41/2002 art. 18.3 for both statutory reasons)** | No configuration | Couples a Europe-wide module to one country's text |
| **B) `DataSubjectRightsOptions.WithholdingLegalBasis` (`WithholdReason` to text), defaults for `SubjectiveAnnotation` ("Ley 41/2002 art. 18.3; Código Deontológico art. 42") and `ThirdPartyData` ("GDPR Art. 15(4); Ley 41/2002 art. 18.3"); a policy may supply its own for `SeriousHarm`; startup validation fails on an empty basis for a reason in use** | Replaceable per jurisdiction; every exclusion has a non-empty basis | Defaults name Spanish law (the first target); the text is re-checked before it is documented |

### Chosen Option: **B — options table with defaults**

Class: Trivial
Settled by: `src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs` (configuration lives in the options class with a validator, `DataSubjectRightsOptionsValidator.cs`), issue #1195 Motivation (names the two statutory sources)

### Rationale

- The legal basis is text that goes to the data subject; it carries no personal data and is safe in logs by reason only (the text itself is not logged).

</details>

<details>
<summary><strong>4. Recording exclusions with the DSR request — a new event on the request stream when the access call carries the request id</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AccessRequest` gains `Guid? RequestId`; `HandleAccessAsync` appends `DSRAccessExclusionsRecorded` (entity type, entity id, field or null, reason, legal basis; no values) to the request aggregate while it is `InProgress`** | Auditable, retry-safe, independent of completion; the projection keeps the list for evidence | An optional parameter; an access call without a request id records nothing |
| **B) Extend `DSRRequestCompleted` with the exclusions** | One event | `CompleteRequestAsync` is a separate call that does not see the export; the service would have to carry the list between calls |
| **C) Only return the exclusions in `AccessResponse` and log counts** | Smallest change | The issue's audit row ("recorded with the DSR request") is not met; the controller cannot later prove what was withheld |

### Chosen Option: **A — new event on the request stream** (recommended, pending the maintainer)

Class: Dilemma

### Rationale

- We recommend A because it records what was withheld at the moment of the export, and a retried export appends a new event rather than overwriting history.
- B wins when the maintainer wants exactly one completion event per request carrying the final disclosure summary and accepts that `CompleteRequestAsync` takes the exclusions as an argument; the stored data is the same, only the event differs.
- An access call without `RequestId` still filters and returns exclusions; it records none and logs the count (EventId 9803).

</details>

<details>
<summary><strong>5. Portability export — apply the same evaluator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Access only, as the issue and AC-009 say; portability keeps filtering on `IsPortable`** | Smallest scope | A field classified for withholding that is also `Portable` is exported by Art. 20 |
| **B) `DefaultDataPortabilityExporter` runs the same evaluator and omits the withheld locations; `PortabilityResponse` gains the same `Exclusions` list** | The classification means one thing everywhere; no leak path | Larger scope (two more files and tests) |

### Chosen Option: **B — portability uses the same evaluator** (recommended, pending the maintainer)

Class: Dilemma

### Rationale

- We recommend B because a classification that is honoured by one export and ignored by another is a disclosure path, and the evaluator is already built (AGENTS.md §3, fail closed).
- A wins when the maintainer reads Art. 20 as limited to data the subject provided (so an application marks subjective annotations `Portable = false` and never needs withholding there) and prefers to keep PR 3 out of the 1.0 gate; the portability work is isolated in Phase 4 so it can be cut without touching the rest.

</details>

---

## Implementation Phases

### Phase 1: Core Models and Attribute

> **Goal**: The vocabulary: reasons, exclusions, the attribute marker and the response shape.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Model/WithholdReason.cs`** (new, `[Flags] public enum`): `None = 0`, `SubjectiveAnnotation = 1`, `ThirdPartyData = 2`, `SeriousHarm = 4`; XML docs name Ley 41/2002 art. 18.3 and Código Deontológico art. 42 and state that `SeriousHarm` is set by a runtime policy only.
2. **`Model/AccessExclusion.cs`** (new `sealed record`): `Type EntityType`, `string EntityId`, `string? FieldName` (null means the whole record), `WithholdReason Reason`, `string LegalBasis`; no value property by design; `ToString()` omits `EntityId`.
3. **`Attributes/PersonalDataAttribute.cs`** (modify): `public WithholdReason Withhold { get; set; } = WithholdReason.None;` with XML docs and an example.
4. **`Model/PersonalDataField.cs`** (modify): `public WithholdReason Withhold { get; init; }`; **`PersonalDataMap.cs`** (modify, `ScanType` line 111) fills it.
5. **`Model/AccessResponse.cs`** (modify): `public IReadOnlyList<AccessExclusion> Exclusions { get; init; } = [];`; **`Requests/AccessRequest.cs`** (modify): `Guid? RequestId = null` appended to the positional record.
6. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of DSR access withholding in Encina.Compliance.DataSubjectRights (issue #1195).

CONTEXT:
- PersonalDataAttribute (Attributes/PersonalDataAttribute.cs) has Category, Erasable, Portable, LegalRetention, RetentionReason.
- AccessResponse (Model/AccessResponse.cs) has SubjectId, Data, ProcessingActivities, GeneratedAtUtc; AccessRequest (Requests/AccessRequest.cs) is a positional record (SubjectId, IncludeProcessingActivities).
- PersonalDataMap.ScanType (PersonalDataMap.cs:111) builds PersonalDataField from the attribute.

TASK:
Add WithholdReason ([Flags], explicit values), AccessExclusion, PersonalDataAttribute.Withhold, PersonalDataField.Withhold, AccessResponse.Exclusions and AccessRequest.RequestId as listed in the plan's Phase 1 Tasks.

KEY RULES:
- AccessExclusion never carries a value, only identifiers, the reason and the legal basis; its ToString omits the entity id.
- WithholdReason.None = 0 is the only zero member; XML docs on every public member.
- No [Obsolete], no compatibility shims; pre-1.0 changes are made completely.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Attributes/PersonalDataAttribute.cs
- src/Encina.Compliance.DataSubjectRights/Model/AccessResponse.cs
- src/Encina.Compliance.DataSubjectRights/PersonalDataMap.cs
- tests/Encina.UnitTests/Compliance/DataSubjectRights/PersonalDataAttributeTests.cs
```

</details>

---

### Phase 2: Policy, Evaluator and Options

> **Goal**: One component decides what is withheld and why.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Abstractions/IAccessWithholdingPolicy.cs`** (new): `ValueTask<Either<EncinaError, AccessWithholdingDecision>> EvaluateAsync(AccessWithholdingContext context, CancellationToken cancellationToken = default)`; **`Model/AccessWithholdingContext.cs`** and **`Model/AccessWithholdingDecision.cs`** (new records; the decision has `Reasons`, `LegalBasis?`, `WholeRecord`, and static `NotWithheld`).
2. **`Export/AccessWithholdingEvaluator.cs`** (new, `internal sealed`, scoped): input `IReadOnlyList<PersonalDataLocation>`; for each location resolve the static `Withhold` through a cached `(Type, string)` lookup of `[PersonalData]`, then ask every registered policy; produce `AccessWithholdingResult(IReadOnlyList<PersonalDataLocation> Included, IReadOnlyList<AccessExclusion> Exclusions)`. A whole-record decision excludes all locations of that `(EntityType, EntityId)` and yields one exclusion with `FieldName = null`. A `Left` from a policy is returned as `Left`. The legal basis comes from the policy, else from the options table.
3. **`DataSubjectRightsOptions.cs`** (modify): `Dictionary<WithholdReason, string> WithholdingLegalBasis` with the two defaults of Design Choice 3; **`DataSubjectRightsOptionsValidator.cs`** (modify): every reason key has a non-empty text.
4. **`ServiceCollectionExtensions.cs`** (modify): `TryAddScoped<AccessWithholdingEvaluator>()`; policies are plain `AddScoped<IAccessWithholdingPolicy, T>` by the application; the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`.
5. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of DSR access withholding in Encina.Compliance.DataSubjectRights (issue #1195).

CONTEXT:
- Phase 1 added WithholdReason, AccessExclusion, PersonalDataAttribute.Withhold and AccessResponse.Exclusions.
- IPersonalDataLocator returns PersonalDataLocation(EntityType, EntityId, FieldName, Category, IsErasable, IsPortable, HasLegalRetention, CurrentValue).
- Locators build the location themselves, so the classification is resolved from the attribute at export time.

TASK:
Create IAccessWithholdingPolicy with its context and decision records, the internal AccessWithholdingEvaluator, the WithholdingLegalBasis options table with its validator rule and the DI registration, as listed in the plan's Phase 2 Tasks.

KEY RULES:
- Union of reasons from the attribute and every policy; a policy cannot release an attribute-classified field.
- A Left from a policy fails the evaluation (never treated as "not withheld").
- The excluded CurrentValue is never copied into an exclusion, a log or a tag.
- Cache the (Type, string) attribute lookups in a ConcurrentDictionary.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Locators/CompositePersonalDataLocator.cs
- src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs
- src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs
- src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs
```

</details>

---

### Phase 3: Service Integration and Request-Stream Event

> **Goal**: `HandleAccessAsync` builds a filtered export and records the exclusions.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Services/DefaultDSRService.cs`** (modify `HandleAccessAsync`, lines 535-587): after the locator call, run the evaluator; build `AccessResponse` with `Data = Included` and `Exclusions`; when `request.RequestId` is set, load the request aggregate, call `RecordAccessExclusions(...)` and save; a `Left` anywhere returns `Left` and no `AccessResponse` (fail closed). Extract the filter-and-record step into a private method to keep CRAP at or below 10.
2. **`Events/DSRRequestEvents.cs`** (modify): `DSRAccessExclusionsRecorded(Guid RequestId, IReadOnlyList<AccessExclusionRecord> Exclusions, DateTimeOffset OccurredAtUtc)` implementing `INotification`; `AccessExclusionRecord` carries entity type name, entity id, field name or null, reason, legal basis.
3. **`Aggregates/DSRRequestAggregate.cs`** (modify): `RecordAccessExclusions(IReadOnlyList<AccessExclusionRecord> exclusions, DateTimeOffset occurredAtUtc)` allowed in `InProgress` only; **`Projections/DSRRequestProjection.cs`** and **`Projections/DSRRequestReadModel.cs`** (modify): `AccessExclusions` list.
4. The tenant of the request is resolved by the service's existing tenant scope (no new logic).
5. **`PublicAPI.Unshipped.txt`** (via `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of DSR access withholding in Encina.Compliance.DataSubjectRights (issue #1195).

CONTEXT:
- DefaultDSRService.HandleAccessAsync (Services/DefaultDSRService.cs:535-587) locates data and returns it whole.
- The request lifecycle is a Marten aggregate (Aggregates/DSRRequestAggregate.cs: Submit, Verify, StartProcessing, Complete, Deny, Extend, Expire) with events in Events/DSRRequestEvents.cs and a projection/read model.
- Phase 2 added AccessWithholdingEvaluator.

TASK:
Run the evaluator in HandleAccessAsync, return Included and Exclusions, and record a DSRAccessExclusionsRecorded event on the request stream when AccessRequest.RequestId is set, as listed in the plan's Phase 3 Tasks.

KEY RULES:
- Fail closed: any error from the evaluator or the save returns Left; the response is never built from a partial result.
- The event carries identifiers, reasons and legal basis only, never values.
- Time comes from TimeProvider; ROP everywhere; the existing tenant scope helper is used unchanged.
- Keep CRAP at or below 10 on changed methods.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- src/Encina.Compliance.DataSubjectRights/Aggregates/DSRRequestAggregate.cs
- src/Encina.Compliance.DataSubjectRights/Events/DSRRequestEvents.cs
- src/Encina.Compliance.DataSubjectRights/Projections/DSRRequestProjection.cs
```

</details>

---

### Phase 4: Portability Export (separable)

> **Goal**: The portability export honours the same classification.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Export/DefaultDataPortabilityExporter.cs`** (modify, lines 93-118): after `.Where(l => l.IsPortable)` run the evaluator; export only `Included`; a `Left` fails the export.
2. **`Model/PortabilityResponse.cs`** (modify): `IReadOnlyList<AccessExclusion> Exclusions { get; init; } = []`.
3. Writers (`Export/JsonExportFormatWriter.cs` and siblings) are unchanged: they serialise the included data only.
4. If the maintainer chooses option A of Design Choice 5, this phase is dropped and a follow-up issue records that portability does not honour the withholding classification.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of DSR access withholding in Encina.Compliance.DataSubjectRights (issue #1195).

CONTEXT:
- DefaultDataPortabilityExporter.ExportAsync locates data and keeps locations with IsPortable (Export/DefaultDataPortabilityExporter.cs:93-100).
- AccessWithholdingEvaluator (Phase 2) decides what is withheld.

TASK:
Run the evaluator in the portability exporter and add Exclusions to PortabilityResponse, as listed in the plan's Phase 4 Tasks.

KEY RULES:
- Only Included locations reach a format writer.
- A Left from the evaluator fails the export.
- Do not change IExportFormatWriter.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Export/DefaultDataPortabilityExporter.cs
- src/Encina.Compliance.DataSubjectRights/Model/PortabilityResponse.cs
- tests/Encina.UnitTests/Compliance/DataSubjectRights/DefaultDataPortabilityExporterTests.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Tenant, audit and validation behave like the rest of the module.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy**: `AccessWithholdingContext.TenantId` is filled from the service's ambient tenant; the request aggregate and read model already carry the tenant; a two-tenant test shows tenant A's exclusions are never read, exported or acted on in tenant B (AC-043).
2. **Audit trail**: the `DSRAccessExclusionsRecorded` event is the audit record (Phase 3).
3. **Validation**: options validated at startup (Phase 2).
4. Not applicable (see matrix): caching, health checks, resilience, locks, transactions, idempotency, module isolation.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are verifying the cross-cutting integration of DSR access withholding in Encina.Compliance.DataSubjectRights (issue #1195).

CONTEXT:
- DefaultDSRService resolves the ambient tenant for its operations; DSR events are the audit trail.

TASK:
Pass the tenant into AccessWithholdingContext and add the two-tenant test of the plan's Phase 7.

KEY RULES:
- No new tenant logic; no new audit store; no caching of withholding decisions (a decision depends on mutable policy state).

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs
- tests/Encina.IntegrationTests/Compliance/DataSubjectRights/DSRTenantScopingIntegrationTests.cs
```

</details>

---

### Phase 6: Observability

> **Goal**: Instruments and logs for exclusions, without values or identifiers (REQ-062, AC-044).

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina/Diagnostics/EventIdRanges.cs`** (modify): register `ComplianceDSRExtensions = (9800, 9849)` in the compliance extensions area with the summary "Encina.Compliance.DataSubjectRights — second range (access withholding, request rules)"; add the field to `src/Encina/PublicAPI.Unshipped.txt`; add `nameof(EventIdRanges.ComplianceDSRExtensions)` as a second range of `Encina.Compliance.DataSubjectRights` in `AssemblyRanges` of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (line 66).
2. **`Diagnostics/DSRWithholdingLogMessages.cs`** (new, `internal static partial class`, `[LoggerMessage]`, XML doc "Event IDs: 9800-9805 (see EventIdRanges.ComplianceDSRExtensions)"): 9800 `AccessFieldWithheld` (reason only), 9801 `AccessRecordWithheld`, 9802 `AccessExclusionsRecorded` (count), 9803 `AccessExclusionsNotRecordedNoRequestId`, 9804 `WithholdingPolicyFailed` (error code only), 9805 `WithholdingFieldUnresolvable` (once per type and field name).
3. **`Diagnostics/DataSubjectRightsDiagnostics.cs`** (modify): counter `dsr.access.exclusions.total` (tag `dsr.withhold_reason`); the `DSR.Request` activity gains `encina.tenant_id` and `dsr.access.exclusions` (count). The issue's proposed names (`encina.dsr.access_exclusions`, `encina.dsr.access_export`) are replaced by the package's naming (`dsr.*`, `DSR.Request`).
4. Registration of the range is a prerequisite of PR 2; #2700 reuses it (9806-9813).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of DSR access withholding in Encina.Compliance.DataSubjectRights (issue #1195).

CONTEXT:
- ComplianceDSR = (8300, 8349) has no packed free ids (8304-8309 and 8317-8319 are kept for #1937, 8315-8316 for #1197), so a second range is registered first (ADR-021, AGENTS.md section 7).
- DataSubjectRightsDiagnostics owns the ActivitySource, Meter and instruments; DSR.Request is the request activity.

TASK:
Register ComplianceDSRExtensions = (9800, 9849), map it as a second range for the assembly in EncinaEventIdAllocationTests, create DSRWithholdingLogMessages with EventIds 9800-9805, and add the counter and activity tags as listed in the plan's Phase 6 Tasks.

KEY RULES:
- Register the range before using any id; ids packed sequentially from 9800.
- No excluded value, subject id, entity id or EncinaError.Message in any log, tag or metric; log the reason name or the error code only.
- Run the EventId architecture tests.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 7: Testing

> **Goal**: Every flag reaches its manifest target; AC-009 and the S15 shape part are proven.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Compliance/DataSubjectRights/Withholding/`): `AccessWithholdingEvaluatorTests` (attribute reasons, policy reasons, union, whole-record exclusion, policy `Left` fails, unresolvable field), `WithholdReasonTests`, `AccessExclusionTests` (ToString omits ids), options validator tests, `DefaultDSRServiceAccessWithholdingTests` (response without excluded values, exclusions with reason and basis, event recorded with `RequestId`, none without), aggregate/projection tests for the new event, DI test with `ValidateOnBuild` and `ValidateScopes`.
2. **Guard**: public members of the policy, evaluator inputs, new options.
3. **Contract**: `IAccessWithholdingPolicy` against a real implementation; `AccessResponse`/`AccessExclusion` shape.
4. **Property** (`tests/Encina.PropertyTests/Compliance/DataSubjectRights/`): for any generated set of locations and classifications an excluded value never appears in the response (checked on the serialised JSON of `Data`), and every exclusion has a non-empty reason and basis.
5. **Integration** (`[Collection(MartenCollection.Name)]`, PostgreSQL): the S15 shape path (subjective annotation omitted with its reason, third-party data omitted, event stored and projected), two-tenant isolation.
6. **Telemetry/privacy**: in-memory exporter test (AC-044) and a leak test: no value, subject id or entity id in tags, metrics or logs.
7. **Load and benchmark**: `.md` justifications (not concurrent or hot).
8. **Coverage manifest**: per-file targets and justifications in `.github/coverage-manifest/Encina.Compliance.DataSubjectRights.json` (via `mechanical-fixer`); measure each flag; CRAP gate.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of DSR access withholding in Encina.Compliance.DataSubjectRights (issue #1195).

CONTEXT:
- DSR tests live in tests/Encina.{Unit,Guard,Contract,Property,Integration}Tests/Compliance/DataSubjectRights/. Unit tests mock IAggregateRepository (NSubstitute); integration tests use [Collection(MartenCollection.Name)].
- Assertions use Shouldly through Encina.Testing.Shouldly.

TASK:
Write the tests listed in the plan's Phase 7 Tasks; they must execute real package code.

KEY RULES:
- FakeTimeProvider; no Thread.Sleep; no swallowed exceptions.
- Property test: an excluded value never appears in the serialised response.
- Measure per-flag coverage against the manifest targets before reporting.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/DataSubjectRights/Services/DefaultDSRServiceTests.cs
- tests/Encina.IntegrationTests/Compliance/DataSubjectRights/DSRTenantScopingIntegrationTests.cs
- .github/coverage-manifest/Encina.Compliance.DataSubjectRights.json
```

</details>

---

### Phase 8: Documentation and Finalization

> **Goal**: Users know how to classify fields and write a policy; the build is clean.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public API with an example of `[PersonalData(Withhold = ...)]` and a policy.
2. `changelog.d/1195-dsr-access-withholding.added.md` (via `mechanical-fixer`).
3. `src/Encina.Compliance.DataSubjectRights/README.md` and the DSR feature guide (via `docs-writer`), including the legal-basis defaults and their verification date.
4. `PublicAPI.Unshipped.txt` (both packages); verification: `dotnet build Encina.slnx --configuration Release` (0 warnings), the five DSR test projects, EventId architecture tests, `dotnet run --file .github/scripts/changelog-fragments.cs -- --check`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are finalising DSR access withholding in Encina.Compliance.DataSubjectRights (issue #1195).

CONTEXT:
- Documentation pages go to the docs-writer; changelog fragments and PublicAPI lines to the mechanical-fixer.

TASK:
Complete the plan's Phase 8 Tasks and run the verification commands.

KEY RULES:
- No hand-typed coverage figures in documentation (covref markers).
- Re-check the cited articles of Ley 41/2002, the Código Deontológico and GDPR Art. 15(4) before documenting them.
- Zero build warnings.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/README.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Provision | Relevance |
|----------|-----------|-----------|
| Ley 41/2002 | art. 18.3 | The right of access to the clinical history does not reach third parties' data nor the professional's subjective annotations (as cited in the issue) |
| Código Deontológico (psychology) | art. 42 | Professional's annotations (as cited in the issue) |
| GDPR | Art. 12, 15(3), 15(4) | State the reasons; copy of the data; must not adversely affect the rights of others |
| GDPR | Art. 20 | Portability (Design Choice 5) |
| SPEC-002 | REQ-009, AC-009, S15, DEC-001 (b), DEC-006, DEC-008, REQ-061, REQ-062 | Requirement, acceptance, Tier B, fail closed, Marten-only, tenancy, instrumentation |
| ADR-001, ADR-006, ADR-018, ADR-019, ADR-021 | ROP, cross-cutting, Marten, EventIds | Design rules followed |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `DefaultDSRService.HandleAccessAsync` | `src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs:535-587` | Where the filter runs |
| `IPersonalDataLocator`, `PersonalDataLocation` | `Abstractions/IPersonalDataLocator.cs`, `Model/PersonalDataLocation.cs` | Input of the evaluator |
| `MartenEventPersonalDataLocator` | `src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs:139-148` | Shows locators build locations; unchanged |
| `DSRRequestAggregate`, events, projection, read model | `Aggregates/`, `Events/`, `Projections/` | Records the exclusions |
| `DataSubjectRightsOptions` and validator | `DataSubjectRightsOptions.cs`, `DataSubjectRightsOptionsValidator.cs` | Legal-basis table |
| `DataSubjectRightsDiagnostics` | `Diagnostics/DataSubjectRightsDiagnostics.cs` | Counter and activity tags |
| `DefaultDataPortabilityExporter` | `Export/DefaultDataPortabilityExporter.cs:66-118` | Phase 4 |
| `MartenFixture` and `MartenCollection` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/` | Integration tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.DataSubjectRights` (existing) | 8300-8349 (`EventIdRanges.ComplianceDSR`) | Used: 8300-8303, 8310-8314, 8320-8349; 8304-8309 and 8317-8319 kept for #1937; 8315-8316 named by the #1197 plan |
| **This plan (#1195)** | **9800-9805** in the new `ComplianceDSRExtensions = (9800, 9849)` | Second range for the assembly, registered here; free in AGENTS.md §7 (9700-9999) and not claimed by any plan (9700-9749 is #812, 9750-9799 is #1244) |
| #2700 | 9806-9813 | Reserved by its plan, same range |
| Left in the range | 9814-9849 | For #1937 and #1197 overflow |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New source | 9 | Reason, exclusion, policy, context, decision, evaluator, log messages, event record, result type |
| Modified source | 14 | Attribute, field, map, response, request, service, events, aggregate, projection, read model, options, validator, registration, diagnostics, exporter, `EventIdRanges.cs` |
| Tests | 14 | Unit 6, guard 1, contract 1, property 1, integration 2, leak 1, justifications 2 |
| Documentation | 5 | README, feature guide, changelog fragment, PublicAPI (two) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 library (no backward compatibility, ROP with Either<EncinaError, T>, TimeProvider for time, tenant-scoped reads). Encina.Compliance.DataSubjectRights is event-sourced on Marten (PostgreSQL). Issue #1195 lets a DSR access export leave out fields or records classified for withholding (subjective annotation, third-party data, serious harm) and records the reason and legal basis of each exclusion.

IMPLEMENTATION OVERVIEW:
1. Add WithholdReason, AccessExclusion, PersonalDataAttribute.Withhold, AccessResponse.Exclusions and AccessRequest.RequestId.
2. Add IAccessWithholdingPolicy, the internal AccessWithholdingEvaluator and the legal-basis options table.
3. Run the evaluator in HandleAccessAsync and record DSRAccessExclusionsRecorded on the request stream.
4. Run the same evaluator in the portability exporter (separable).
5. Pass the tenant; register the second EventId range 9800-9849; add the counter, activity tags and log messages 9800-9805.
6. Tests of every flag and Marten integration; docs, changelog, PublicAPI.

KEY PATTERNS:
- The attribute is resolved at export time from (EntityType, FieldName); locators are not trusted to classify.
- Union of reasons; policies can only withhold; an error from a policy fails the export.
- Exclusions carry identifiers, reason and legal basis, never values; no value or identifier in logs, tags or metrics.
- [LoggerMessage] with a registered range; TryAdd registrations; DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/ (Services/DefaultDSRService.cs, Attributes/PersonalDataAttribute.cs, Model/AccessResponse.cs, Export/DefaultDataPortabilityExporter.cs)
- src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs
- docs/plans/dsar-rejection-reasons-implementation-plan-814.md (style of a DSR change)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | A withholding decision depends on mutable policy state; caching would risk disclosure |
| 2 | OpenTelemetry | ✅ | Counter and `DSR.Request` tags with the tenant, no values or identifiers (Phase 6) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` EventIds 9800-9805 in the new `ComplianceDSRExtensions` range (ADR-021, Phase 6) |
| 4 | Health Checks | ❌ N/A | No new checkable dependency |
| 5 | Validation | ✅ | Options validated at startup: every reason has a legal basis (Phase 2) |
| 6 | Resilience | ❌ N/A | No call to an external system |
| 7 | Distributed Locks | ❌ N/A | No shared state updated by concurrent hosts |
| 8 | Transactions | ❌ N/A | A single append to the request stream; no multi-store atomicity |
| 9 | Idempotency | ❌ N/A | A retried export appends a new event by design; the DSR pipeline owns request deduplication |
| 10 | Multi-Tenancy | ✅ | Tenant-aware per SPEC-002 REQ-061 through the existing scope and the policy context (Phase 5) |
| 11 | Module Isolation | ❌ N/A | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Each exclusion and its reason are recorded with the DSR request (Phase 3) |

---

## Prerequisites & Dependencies

### Blocking

- None. The feature adds types, a policy and an event; it does not need another feature first.

### Coordination

- **Same-file changes**: #1197 edits `DSRRequestSubmitted`, `IDSRService`, `DefaultDSRService`, the projection and the read model; #1937 edits the instrumentation of `HandleAccessAsync`. Land one after another and rebase; this plan appends to `AccessRequest`, the events file and the read model, so conflicts are additive.
- **EventIds**: the second DSR range (9800-9849) is registered by whichever of #1195 and #2700 lands first; the other reuses it.
- **Shared piece owned here**: `WithholdReason` and `AccessExclusion`; #2700 does not use them.

### Deferred

- Portability scope, if the maintainer chooses option A of Design Choice 5: a follow-up issue is opened then.

---

## Next Steps

1. Maintainer answers the two Dilemmas (Design Choices 4 and 5) when work on #1195 starts; answers go in a `## Maintainer Decisions` section appended to this plan.
2. Brief the worker for PR 1 (Phases 1-2); PR 2 follows once #1197 and #1937 are merged or their order is agreed.
3. Implement PR 3 (portability, tests, documentation).
