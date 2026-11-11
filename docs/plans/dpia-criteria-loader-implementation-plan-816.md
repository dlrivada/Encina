# Implementation Plan: DPIA criteria external loader for `Encina.Compliance.DPIA` (EDPB harmonised list, Digital Omnibus COM(2025) 837)

> **Issue**: [#816](https://github.com/dlrivada/Encina/issues/816)
> **Type**: Feature
> **Milestone**: v0.15.0 — EU Compliance: NIS2 & Digital Omnibus
> **Complexity**: Medium (7 phases, Marten module, background refresh, one outbound HTTP source, ~22 production files)
> **Estimated Scope**: ~1,300-1,800 lines of production code + ~2,000-2,800 lines of tests

---

## Summary

Add a **criteria catalog** to `Encina.Compliance.DPIA` that is filled from external sources: a JSON file, a URI, or a custom `IDPIACriteriaProvider`. A data-driven `IRiskCriterion` evaluates the loaded criteria against `DPIAContext.HighRiskTriggers`, so a new criterion needs no code change. The catalog refreshes in the background and reports staleness through the health check. Loading criteria from an external list is allowed by current law (GDPR Art. 35(4) and 35(5) already let supervisory authorities publish lists). The **draft-specific** part is the EDPB harmonised list as a source: the Digital Omnibus proposal COM(2025) 837 would have the EDPB prepare EU-wide lists of processing that does or does not need a DPIA (SPEC-002 §3.5: "GDPR Art. 35(4)–(6)", marked [K], so not fully verified). **This is draft law.** Under SPEC-002 REQ-024, DEC-012 and AC-024 that source sits behind an option that is off by default and named by its draft article, current law stays the default (the national lists, AEPD Art. 35.4 and 35.5), and a test asserts it. The same provider is the one SPEC-002 P-41 (#1243) loads the AEPD lists through.

### Standards covered

- GDPR Art. 35(1): DPIA when processing is likely to result in a high risk. GDPR Art. 35(4) and 35(5): lists published by supervisory authorities of processing that does or does not require a DPIA. Current-law default.
- GDPR Art. 35(6) and Art. 64: consistency mechanism for such lists.
- Digital Omnibus COM(2025) 837 (draft): EU-wide lists prepared by the EDPB (SPEC-002 §3.5: GDPR Art. 35(4)–(6), marked [K]). **Unverified** against the published text: the article numbering, the publication format (the issue says "format TBD, our consultation requested JSON Schema"), who publishes and where. The procedure 2025/0360(COD) is still in committee (SPEC-002 §3.5). Nothing in this plan assumes the EDPB format; the plan defines its own versioned JSON schema and leaves a mapper for the EDPB format to a later issue.
- SPEC-002 REQ-050 / P-41 (#1243): the AEPD lists are loaded through this provider.

### What already exists

- `src/Encina.Compliance.DPIA/Model/HighRiskTriggers.cs`: ten string constants, "defined as strings rather than an enum to support extensibility". `Model/DPIAContext.cs:58` carries `HighRiskTriggers` and a `Metadata` dictionary.
- `Abstractions/IRiskCriterion.cs:57` is the evaluation seam: `ValueTask<RiskItem?> EvaluateAsync(DPIAContext, CancellationToken)`. `ServiceCollectionExtensions.cs:122-127` registers six built-in criteria (`RiskCriteria/*`) with `TryAddEnumerable`. `DefaultDPIAAssessmentEngine.cs:222` (`EvaluateCriteriaAsync`) calls every `IRiskCriterion` with fault isolation: a throwing criterion is logged and skipped.
- `DPIAOptions.cs:20` has no criteria settings. `DPIAOptionsValidator.cs` has four checks. `Health/DPIAHealthCheck.cs:44` (`DefaultName = "encina-dpia"`) resolves `IOptions<DPIAOptions>` at line 127.
- `DPIAAutoDetector.cs` maps name fragments to trigger constants for auto-detection; it is unrelated to external lists and unchanged.
- `DPIAReviewReminderService.cs` is the package's background loop precedent: `PeriodicTimer`, catch per iteration, `TimeProvider` from the scope.
- `Diagnostics/DPIADiagnostics.cs`: `SourceName = "Encina.Compliance.DPIA"`, counters such as `dpia.assessment.total`. EventIds: `ComplianceDPIA` is 8800-8899 with ids up to 8896 in use, so only three are free.
- `src/Encina.Compliance.NIS2/NIS2ResilienceHelper.cs` is the keyed-pipeline precedent (`ResiliencePipelineProvider<string>` from `Polly`); plan #812 uses the same pattern without NIS2's error swallowing.
- Search of `src/` confirms that `IDPIACriteriaProvider`, `DPIACriterion` and `LoadTriggersFrom*` do not exist, and that no code in `src/` uses `IOptionsMonitor` (#817 is open).

### Corrections to the issue body

- The issue's "existing trigger registry" does not exist; the engine consults `IRiskCriterion` instances. This plan adds `IDPIACriteriaCatalog` (the holder) and one data-driven `IRiskCriterion` over it.
- `LoadCriteriaAsync` becomes `ValueTask<Either<EncinaError, IReadOnlyList<DPIACriterion>>> LoadCriteriaAsync(CancellationToken cancellationToken)` (ADR-001, ADR-006, AGENTS.md §3); the record is `sealed`; `RiskLevel` is `Low`, `Medium`, `High`, `VeryHigh`.
- "Uses `IOptionsMonitor` to support hot-reload when criteria are updated": `IOptionsMonitor` reloads **options**, not remote content. Criteria content refreshes through the catalog's refresh loop (Design Choice 4); `IOptionsMonitor<DPIAOptions>` is used for the refresh settings, read on each cycle. Migrating other consumers is #817.
- The names `LoadTriggersFromJson/Uri/Provider` are renamed `LoadCriteriaFromJson`, `LoadCriteriaFromUri`, `LoadCriteriaFromProvider<T>`: the objects loaded are criteria, and a criterion's id is what matches a trigger id.
- "Resilience: retry and timeout, included in the original issue": the issue's matrix marks it included; this plan names the mechanism: a keyed Polly pipeline `dpia-criteria` (Design Choice 6).
- "Caching: deferred": the loaded list is held in memory by the catalog, so there is nothing to defer; the row is ❌.
- The acceptance criterion "existing string-based triggers remain valid" is met by design: the built-in `IRiskCriterion` classes are unchanged and loaded criterion ids reuse the same string space.

### Affected packages

- `Encina.Compliance.DPIA`: new `Criteria/` folder, options, validator, DI, health check, diagnostics.
- `Encina` (core): `src/Encina/Diagnostics/EventIdRanges.cs`, one new range.
- New package references in `Encina.Compliance.DPIA.csproj`: `Polly` (central version, as `Encina.Compliance.NIS2` does) and `Microsoft.Extensions.Http` for `IHttpClientFactory` (the worker confirms the central package versions).
- Tests: `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.PropertyTests`, `Encina.IntegrationTests` (file and WireMock through `Encina.Testing.WireMock`); `.md` justifications for load and benchmark.

### Provider category

None of the 10 database providers (no store). Sources are: JSON file, URI (`HttpClient`) and custom `IDPIACriteriaProvider`. Resilience for the URI source is the keyed Polly pipeline of `Encina.Compliance.NIS2` and plan #812 (not `Encina.Extensions.Http.Resilience`, whose handler suits typed clients; Design Choice 6).

---

## Design Choices

<details>
<summary><strong>1. Provider contract — <code>IDPIACriteriaProvider</code> returning <code>Either</code></strong></summary>

**Class:** Settled
**Settled by:** ADR-001 and ADR-006 (Railway Oriented Programming); AGENTS.md section 3 (operations return `Either<EncinaError, T>`, database and I/O calls are asynchronous with a `CancellationToken`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValueTask<Either<EncinaError, IReadOnlyList<DPIACriterion>>> LoadCriteriaAsync(CancellationToken)`** | Failures are values; matches every Encina contract | None |
| **B) `Task<IReadOnlyList<DPIACriterion>>` as in the issue sketch** | Simpler to implement | Exceptions for I/O failure; contradicts ADR-001 |

### Chosen Option: **A — `Either`-returning contract**

### Rationale

- The catalog needs to tell "source unreachable" from "source returned an invalid document"; typed errors do that (`dpia.criteria_source_unreachable`, `dpia.criteria_invalid`).

</details>

<details>
<summary><strong>2. How loaded criteria enter the assessment — a catalog and one data-driven criterion</strong></summary>

**Class:** Trivial
**Settled by:** precedent `IRiskCriterion` and its `TryAddEnumerable` registration (`ServiceCollectionExtensions.cs:122-127`); AGENTS.md section 3 (registration completeness)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IDPIACriteriaCatalog` (singleton, immutable snapshot swapped atomically) plus `LoadedCriteriaRiskCriterion : IRiskCriterion` that matches a context's trigger ids against the snapshot** | No change to the engine or to `IRiskCriterion`; same fault isolation | One more type |
| **B) Change `DefaultDPIAAssessmentEngine` to read the catalog directly** | Fewer indirections | Couples the engine to a source mechanism; harder to replace |

### Chosen Option: **A — Catalog plus data-driven criterion**

### Rationale

- For each context trigger id found in the snapshot, the criterion returns a `RiskItem(Category: criterion.Name, Level: criterion.DefaultRiskLevel, Description, MitigationSuggestion)`; when several match, it returns the highest level (the engine already takes the maximum overall).
- A loaded id that equals a built-in trigger (for example `HealthData`) produces a second `RiskItem` next to the built-in one. The overall risk is the maximum, so there is no inflation, and the duplication is acceptable.
- `ApplicableSectors`: an empty list means every sector. When the list is non-empty the criterion applies if `DPIAContext.Metadata["Sector"]` is in it **or the context names no sector** (fail closed: more DPIA, not less, AGENTS.md §3).

</details>

<details>
<summary><strong>3. Off by default — the EDPB harmonised list as a gated source origin</strong></summary>

**Class:** Settled
**Settled by:** SPEC-002 REQ-024, DEC-012 and AC-024 (the draft-specific part of #816 sits behind an option that is off by default and named by its draft article); precedent `DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds` (#814); AGENTS.md section 3 (fail closed)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DPIAOptions.EnableOmnibusArticle35EdpbHarmonisedList` (default `false`). Each source has an `Origin` (`Custom`, `NationalList`, `EdpbHarmonisedList`). The validator fails the start-up when a source with origin `EdpbHarmonisedList` is configured while the flag is `false`** | AC-024 testable: default options never load an EDPB source; national lists (AEPD, P-41) and custom sources work today under current law | The flag name embeds the article numbering from SPEC-002, unverified |
| **B) No flag; the application simply does not configure an EDPB source** | One concept fewer | Nothing marks which sources are draft law; no switch to test |

### Chosen Option: **A — Flag plus source origin**

### Rationale

- The loader mechanism is current-law: JSON file, URI and custom provider need no flag.
- The AC-024 test: default options, the catalog holds no `EdpbHarmonisedList` origin and the engine uses only the built-in criteria and any `NationalList` or `Custom` sources the application added.

</details>

<details>
<summary><strong>4. Refresh — a polling loop per catalog, settings read from <code>IOptionsMonitor</code></strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One hosted service, `PeriodicTimer` and `TimeProvider`, refreshing every source each `RefreshInterval` (default `TimeSpan.Zero` = load once at start); the interval and the maximum age are read from `IOptionsMonitor<DPIAOptions>` each cycle** | One mechanism for file, URI and custom; deterministic tests with `FakeTimeProvider`; follows `DPIAReviewReminderService` | A file change is seen at the next interval, not at once |
| **B) A: plus a `FileSystemWatcher` for the JSON file source** | Immediate pickup of file edits | Platform-specific quirks (duplicate events, partial writes); a second mechanism to test |
| **C) Load once at start only; refresh by restarting the application** | Simplest | Does not meet the issue's "hot-reload when criteria are updated" |

### Chosen Option: **A — Polling loop, settings from `IOptionsMonitor`** (recommended, pending the maintainer)

### Rationale

- I recommend A because the criteria change rarely (a published list), polling is enough, and one mechanism is easier to prove correct. B wins for an application that edits the file and expects immediate effect; it can be added behind the same catalog.
- The loop catches per iteration and never lets an exception escape; a failure is handled by the failure policy of Design Choice 5 and logged by error code (AGENTS.md §3; `PeriodicTimer` rule, ADR-052, #2774). Cancellation from the stopping token ends the loop.

</details>

<details>
<summary><strong>5. Failure behaviour — first load and refresh</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) First load of a required source fails: the catalog is "unavailable" and `AssessAsync` returns `Left` `dpia.criteria_unavailable` until it loads (fail closed). Refresh fails with a previous snapshot: keep serving the last good snapshot, log by error code, report degraded health, with a configurable `MaximumSnapshotAge` defaulting to unbounded** | Availability for a flaky public URL; never evaluates against nothing | A stale list can be used for a long time (the age bound is opt-in) |
| **B) First load as in A. Refresh fails: expire the snapshot after `MaximumSnapshotAge` (default 7 days) and then fail closed** | Never evaluates against stale law for long | An outage of the source can block DPIA-gated requests after seven days |
| **C) Any failure falls back to the built-in criteria only** | Never blocks | A configured national or EDPB list is silently ignored; a compliance gate failing open |

### Chosen Option: **A — Fail closed at first load; keep the last good list on refresh failure** (recommended, pending the maintainer)

### Rationale

- I recommend A with the bound available because the first-load case is settled by AGENTS.md §3 (compliance gates fail closed) while the refresh case is a trade between availability and freshness. B wins for a regulated application that wants a hard guarantee about the age of the law it evaluates against. C is rejected by AGENTS.md §3.
- A source can be declared `IsRequired = false` (default `true`): a failed optional source is skipped, logged and shown in the health data, and the built-in criteria still apply.

</details>

<details>
<summary><strong>6. Outbound HTTP — transport security and resilience</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `https` only; named `HttpClient` from `IHttpClientFactory`; keyed Polly pipeline `dpia-criteria` (timeout, retry on transient failures); response size cap 1 MiB; `System.Text.Json` with a depth limit** | Integrity of a compliance list; one resilience pattern across NIS2, #812 and this plan | Integration tests need an HTTPS test endpoint (WireMock supports TLS) |
| **B) A, plus `http` allowed for loopback addresses only** | WireMock on `localhost` works without certificates | A slightly larger surface to explain; a loopback exception is an extra rule to test |
| **C) `Microsoft.Extensions.Http.Resilience` handler on the named client instead of Polly** | Standard handler pipeline | A different mechanism from NIS2 and #812; the keyed pipeline lets the application tune policy by key |

### Chosen Option: **B — `https` plus loopback `http`, keyed Polly pipeline** (recommended, pending the maintainer)

### Rationale

- I recommend B because tests against WireMock on `localhost` stay simple and production stays `https`-only in effect (a non-loopback `http` URI fails validation at start-up). A wins when the maintainer prefers the strictest rule and accepts TLS in tests. The pipeline choice (keyed Polly rather than C) follows `NIS2ResilienceHelper` and plan #812, without NIS2's swallowing: a pipeline failure is returned as `Left` and handled by Design Choice 5.
- No response body, URL query or header value is logged; only the error code, the source name and the status class.

</details>

<details>
<summary><strong>7. Duplicate criterion ids across sources</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The load fails with `dpia.criteria_duplicate_id` naming the id (no silent override)** | A law-derived criterion cannot be silently overridden; curated sources stay consistent | An application that wants to override an entry must remove it from its source |
| **B) The later-registered source wins** | An application can tune a published criterion | A silent change of risk level for a compliance criterion |

### Chosen Option: **A — Fail on duplicates** (recommended, pending the maintainer)

### Rationale

- I recommend A because the risk level of a criterion is a legal judgement. B wins for an application that must lower a published level for its own sector; for that case a custom provider can return the adjusted list, and the duplicate rule still holds across the sources it combines.

</details>

<details>
<summary><strong>8. Document format — an Encina-defined, versioned JSON schema</strong></summary>

**Class:** Settled
**Settled by:** the issue ("EDPB publishes criteria (format TBD)"); SPEC-002 INV-003 and §3.5 (no default follows law that is not yet applicable; the proposal's format is not published)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `{ "schemaVersion": 1, "listVersion": "...", "publishedAtUtc": "...", "criteria": [ { "id", "name", "description", "defaultRiskLevel", "applicableSectors" } ] }`, documented; an unknown `schemaVersion` fails** | Usable today for national lists (P-41) and custom lists; the EDPB format can be mapped later by a custom provider | Not the EDPB's own format |
| **B) Wait for the EDPB format** | No guess | Blocks P-41 and the whole feature on an unpublished format |

### Chosen Option: **A — Encina schema v1**

### Rationale

- `listVersion` and `publishedAtUtc` are kept on the snapshot and exposed by the catalog, so a later issue can record which list version was in force for an assessment (see Prerequisites).

</details>

---

## Implementation Phases

### Phase 1: Core Models and Contracts

> **Goal**: The criterion model, the provider contract, the catalog contract and the errors.

<details>
<summary><strong>Tasks</strong></summary>

All paths are in `src/Encina.Compliance.DPIA/Criteria/`, namespace `Encina.Compliance.DPIA.Criteria`.

1. `Model/DPIACriterion.cs`: `public sealed record DPIACriterion(string Id, string Name, string Description, RiskLevel DefaultRiskLevel, IReadOnlyList<string> ApplicableSectors)`.
2. `Model/CriteriaSourceOrigin.cs`: `Custom`, `NationalList`, `EdpbHarmonisedList` (XML docs name the draft article and "off by default").
3. `Model/DPIACriteriaList.cs`: `ListVersion`, `PublishedAtUtc?`, `Criteria`.
4. `IDPIACriteriaProvider.cs`: `ValueTask<Either<EncinaError, DPIACriteriaList>> LoadCriteriaAsync(CancellationToken cancellationToken)` (the list wrapper replaces the issue's bare `IReadOnlyList`, so the version travels with the content).
5. `IDPIACriteriaCatalog.cs`: `DPIACriteriaSnapshot Current { get; }`, `bool IsAvailable { get; }`.
6. `Model/DPIACriteriaSnapshot.cs`: criteria by id, `LoadedAtUtc`, per-source status (name, origin, last success, last error code).
7. `DPIAErrors.cs` (modify): `dpia.criteria_unavailable`, `dpia.criteria_source_unreachable`, `dpia.criteria_invalid`, `dpia.criteria_duplicate_id`, `dpia.criteria_schema_unsupported`, `dpia.criteria_response_too_large`; messages never contain document content.
8. `PublicAPI.Unshipped.txt` lines (by `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Issue #816, plan docs/plans/dpia-criteria-loader-implementation-plan-816.md. Package src/Encina.Compliance.DPIA
(event-sourced on Marten, ADR-019). Criteria are evaluated by IRiskCriterion implementations; there is no registry
of loaded criteria today. The EDPB harmonised list (Digital Omnibus COM(2025) 837, draft) is gated behind an option
that is off by default (SPEC-002 REQ-024, AC-024).

TASK:
Create the model records, IDPIACriteriaProvider (Either-returning), IDPIACriteriaCatalog and the new error codes.

KEY RULES:
- Operations return Either<EncinaError, T>; no exceptions for business logic.
- Errors never include document content or EncinaError.Message in logs.
- XML docs on every public member; the origin enum names the draft article and "off by default".
- Add PublicAPI.Unshipped.txt lines.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Abstractions/IRiskCriterion.cs
- src/Encina.Compliance.DPIA/DPIAErrors.cs
- src/Encina.Compliance.DPIA/Model/RiskLevel.cs
```

</details>

### Phase 2: Providers (File, URI, Custom)

> **Goal**: The three sources.

<details>
<summary><strong>Tasks</strong></summary>

1. `Providers/JsonFileDPIACriteriaProvider.cs` (public sealed): reads the file with `File.OpenRead` asynchronously and `JsonSerializer.DeserializeAsync` (depth limit 16, size cap 1 MiB), maps to `DPIACriteriaList`; `dpia.criteria_invalid` for a malformed document; `dpia.criteria_schema_unsupported` for an unknown `schemaVersion`.
2. `Providers/UriDPIACriteriaProvider.cs` (public sealed): `IHttpClientFactory` named client, `https` (or loopback `http`, Design Choice 6), keyed pipeline `dpia-criteria` through `ResiliencePipelineProvider<string>` when registered, otherwise one attempt with `UriSourceTimeout`; response size cap; status classification (2xx ok; 4xx permanent `dpia.criteria_source_unreachable` with the status class; 5xx and network errors transient).
3. `Providers/CriteriaDocumentMapper.cs` (internal static): shared parse, validation of each criterion (non-blank id and name, defined `RiskLevel`, sector strings non-blank), duplicate ids within a document.
4. `DPIAOptions.cs` (modify, Phase 4): `LoadCriteriaFromProvider<T>()` registers custom providers.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Plan #816, Phase 2. Sources produce DPIACriteriaList. The keyed Polly pattern is src/Encina.Compliance.NIS2/
NIS2ResilienceHelper.cs, but without its error swallowing; plan #812 uses the same pattern.

TASK:
Implement the JSON file provider, the URI provider and the shared document mapper with validation and size and
depth limits.

KEY RULES:
- Asynchronous I/O with CancellationToken; no synchronous File.ReadAll*.
- https only, except loopback http (Design Choice 6 as decided); a failure is a Left, never an exception.
- Never log the URL query, headers, response body or document content; log the source name and error code.
- Time comes from TimeProvider where a timestamp is needed.

REFERENCE FILES:
- src/Encina.Compliance.NIS2/NIS2ResilienceHelper.cs
- src/Encina.Compliance.DPIA/DPIAErrors.cs
- src/Encina.Testing.WireMock/
```

</details>

### Phase 3: Catalog, Data-Driven Criterion and Refresh

> **Goal**: The holder, the criterion and the loop.

<details>
<summary><strong>Tasks</strong></summary>

1. `DPIACriteriaCatalog.cs` (public sealed, implements `IDPIACriteriaCatalog`): holds an immutable `DPIACriteriaSnapshot` in a volatile field; `internal void Replace(DPIACriteriaSnapshot)`.
2. `DPIACriteriaLoadingService.cs` (public sealed `BackgroundService`): loads all sources at start, then every `RefreshInterval` while greater than zero. Constructor `(IEnumerable<ConfiguredCriteriaSource>, IDPIACriteriaCatalog, IOptionsMonitor<DPIAOptions>, TimeProvider, ILogger<...>)`. `ExecuteAsync` follows `DPIAReviewReminderService` (`PeriodicTimer`, catch per iteration, `OperationCanceledException` from the stopping token ends the loop). A cycle merges the per-source lists, applies the duplicate rule, applies the failure policy (Design Choice 5) and swaps the snapshot.
3. `RiskCriteria/LoadedCriteriaRiskCriterion.cs` (public sealed, `IRiskCriterion`): `Name = "LoadedCriteria"`; matches `DPIAContext.HighRiskTriggers` against the snapshot (Design Choice 2); when the catalog is unavailable for a required source it returns a `RiskItem` at `RiskLevel.VeryHigh` with category `CriteriaUnavailable`, so the assessment cannot silently pass (fail closed). The engine's fault isolation does not hide this because it is a value, not an exception.
4. The engine and the six built-in criteria are unchanged.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Plan #816, Phase 3. DPIAReviewReminderService is the in-package precedent for a PeriodicTimer hosted service.
DefaultDPIAAssessmentEngine isolates a throwing IRiskCriterion by logging and skipping it, so a failure that must
block has to be a returned value.

TASK:
Implement DPIACriteriaCatalog, DPIACriteriaLoadingService and LoadedCriteriaRiskCriterion as in the Tasks block.

KEY RULES:
- Background loop: catch per iteration; only the stopping token's OperationCanceledException ends the loop; a
  failure is handled by the failure policy and logged by error code, never swallowed silently (AGENTS.md
  section 3, #2774). With FakeTimeProvider the callback must not throw.
- All time from TimeProvider. No Thread.Sleep.
- CRAP <= 10 on every method: extract merge, duplicate check and policy into small methods.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs
- src/Encina.Compliance.DPIA/DefaultDPIAAssessmentEngine.cs
- src/Encina.Compliance.DPIA/RiskCriteria/SpecialCategoryDataCriterion.cs
```

</details>

### Phase 4: Configuration, Validation and DI

> **Goal**: The fluent API, the Omnibus flag, start-up validation, registration.

<details>
<summary><strong>Tasks</strong></summary>

1. `DPIAOptions.cs` (modify): `EnableOmnibusArticle35EdpbHarmonisedList` (default `false`, XML docs name the draft article and "off by default"); `RefreshInterval` (default `TimeSpan.Zero`), `MaximumSnapshotAge` (`TimeSpan?`, default `null`), `UriSourceTimeout` (default 30 s); fluent `LoadCriteriaFromJson(string path, Action<CriteriaSourceOptions>? configure = null)`, `LoadCriteriaFromUri(Uri uri, Action<CriteriaSourceOptions>? configure = null)`, `LoadCriteriaFromProvider<TProvider>(Action<CriteriaSourceOptions>? configure = null)`; `CriteriaSourceOptions` has `Name`, `Origin` (default `Custom`), `IsRequired` (default `true`).
2. `DPIAOptionsValidator.cs` (modify): rules for a blank path, a relative URI, a non-`https` URI that is not loopback, a negative `RefreshInterval`, a non-positive `MaximumSnapshotAge`, duplicate source names, and an `EdpbHarmonisedList` source while the flag is off. Delegate to `ValidateCriteriaSources(options, failures)`.
3. `ServiceCollectionExtensions.cs` (modify): `TryAddSingleton<IDPIACriteriaCatalog, DPIACriteriaCatalog>`, `TryAddEnumerable` of `LoadedCriteriaRiskCriterion`, source providers registered per configured source, `AddHostedService<DPIACriteriaLoadingService>` only when at least one source is configured (opt-in, pay for what you use), the named `HttpClient` only when a URI source exists.
4. A DI test (Phase 7) builds the provider with `ValidateOnBuild` and `ValidateScopes` for: no source; a JSON source; a URI source; a custom provider.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Plan #816, Phase 4. Every feature is opt-in: with no source configured nothing new is registered except the
inert catalog. An AddEncina* method must register every option type and dependency it resolves (AGENTS.md
section 3), proven by a DI test with ValidateOnBuild and ValidateScopes.

TASK:
Add the options, the fluent API, the validator rules and the registrations of the Tasks block.

KEY RULES:
- The Omnibus flag is off by default; XML docs name the draft article (GDPR Art. 35(4)-(6) as amended, per
  SPEC-002 section 3.5, unverified) and say "off by default".
- Options validated at start; fail closed with property-named messages and no URL secrets in messages.
- Keep each validator method at CRAP <= 10.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAOptions.cs
- src/Encina.Compliance.DPIA/DPIAOptionsValidator.cs
- src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs
```

</details>

### Phase 5: Cross-Cutting Integration

> **Goal**: Health and the keyed pipeline.

<details>
<summary><strong>Tasks</strong></summary>

1. `Health/DPIAHealthCheck.cs` (modify): when the catalog exists and sources are configured, report `Degraded` when a source's last refresh failed (snapshot kept) or the snapshot is older than `MaximumSnapshotAge`, and `Unhealthy` when a required source never loaded; `data["criteriaListVersion"]`, `data["criteriaSourceCount"]`, `data["criteriaLastLoadedAtUtc"]`; no content.
2. `Providers/CriteriaResilience.cs` (internal static): obtains the keyed `dpia-criteria` pipeline from `ResiliencePipelineProvider<string>` when registered; returns the `Either` result; never swallows.
3. Multi-tenancy and module isolation: criteria are global configuration (the same law for every tenant of a deployment); see the matrix.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Plan #816, Phase 5. DPIAHealthCheck resolves IOptions<DPIAOptions> in a scope (line 127) and must stay cheap.

TASK:
Extend the health check as in the Tasks block and implement CriteriaResilience.

KEY RULES:
- Health data carries counts, versions and timestamps only; never document content, URLs or error messages.
- The resilience helper returns Left on failure; it never converts a failure into success.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Health/DPIAHealthCheck.cs
- src/Encina.Compliance.NIS2/NIS2ResilienceHelper.cs
```

</details>

### Phase 6: Observability

> **Goal**: A registered EventId range, activity and counters.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs` (modify, register first, ADR-021): `public static readonly (int Min, int Max) ComplianceDPIACriteria = (6400, 6429);` with the summary "Encina.Compliance.DPIA — external criteria loading".
2. `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (modify): map `"Encina.Compliance.DPIA"` to `[nameof(EventIdRanges.ComplianceDPIA), nameof(EventIdRanges.ComplianceDPIACriteria)]`.
3. `Diagnostics/DPIACriteriaLogMessages.cs` (new): `[LoggerMessage]` from 6400, packed: load started, load succeeded (count, list version), source failed (source name, error code), duplicate id, snapshot swapped, stale snapshot, unavailable, Omnibus EDPB list active, refresh loop failure.
4. `Diagnostics/DPIADiagnostics.cs` (modify): activity `DPIA.CriteriaLoad` per load; counters `dpia.criteria.loads.total` (tag `outcome`, `origin`) and `dpia.criteria.snapshot.age.seconds` (observable gauge).
5. XML docs: `/// Event IDs: 6400-6429 (see EventIdRanges.ComplianceDPIACriteria)`.

The block 6400-6549 is assigned to this family in the batch EventId registry: 6400-6429 here, 6430-6449 (ENISA, plan #1942) and 6450-6549 (EHDS, plan #808); each plan owns its own registry entry. The range 9700-9749 belongs to plan #812.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Plan #816, Phase 6. ADR-021: register the range in src/Encina/Diagnostics/EventIdRanges.cs before use; the DPIA
range 8800-8899 has only three free ids, so a new range 6400-6429 is registered and mapped as a second range of
the assembly in EncinaEventIdAllocationTests.AssemblyRanges.

TASK:
Register the range, write DPIACriteriaLogMessages with [LoggerMessage], extend DPIADiagnostics, and run the
architecture tests.

KEY RULES:
- Packed EventIds, no gaps, none outside 6400-6429.
- Never log EncinaError.Message, document content or URLs with query strings; use error codes and ex.ForLogging().
- Counters use bounded tag values (outcome, origin) only.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Compliance.DPIA/Diagnostics/DPIALogMessages.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 7: Testing, Documentation and Finalization

> **Goal**: Every flag at its target; AC-024 proven; docs name the draft article.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit**: mapper (valid, malformed, unknown `schemaVersion`, duplicate ids, undefined `RiskLevel`); JSON provider (temp-file fixture); URI provider (status classification with a fake `HttpMessageHandler`, size cap); catalog snapshot swap; loading service with `FakeTimeProvider` (first load, refresh success, refresh failure keeps the snapshot, required-source first-load failure, maximum age, optional source, cancellation ends the loop, a failing source does not stop the loop); `LoadedCriteriaRiskCriterion` (trigger match, sector rules including the missing-sector fail-closed case, unavailable catalog returns the VeryHigh item); options validator rules; **the AC-024 test**: default options give a catalog with no `EdpbHarmonisedList` origin and the assessment uses only the built-in criteria; a DI test with `ValidateOnBuild` and `ValidateScopes`.
2. **Guard**: public constructors and fluent methods, providers, catalog.
3. **Contract**: `IDPIACriteriaProvider` contract base against the file, URI and a custom test provider; `IDPIACriteriaCatalog`.
4. **Property**: JSON round trip of `DPIACriteriaList` (serialize, parse, equal); merge is order-independent when ids are disjoint.
5. **Integration**: file loading from a temp directory; URI loading against WireMock through `Encina.Testing.WireMock` (success, 500 then 200 with the pipeline, oversized body, loopback `http`); an end-to-end assessment on the Marten fixture (shared `[Collection]`) showing a loaded criterion raises the overall risk.
6. **Load / Benchmark**: `.md` justifications (thin loading path, no concurrency beyond an atomic swap).
7. **Per-flag targets** (AGENTS.md §9). Baselines are not measured by this plan; the worker measures them with the §9 commands and sets each target at or above the baseline (targets never go down). Planned targets for new files: providers and mapper unit 95, guard 100, contract 100; catalog unit 100, guard 100; loading service unit 90, guard 100 (every branch reachable with a fake clock and fake sources); `LoadedCriteriaRiskCriterion.cs` unit 95, guard 100, contract 100; `DPIAOptionsValidator.cs` unit 95, guard 100, property 90; `DPIAHealthCheck.cs` unit at least the baseline plus the new branches.
8. **Documentation**: XML docs; `changelog.d/816-dpia-criteria-loader.added.md` (`mechanical-fixer`); package README; DPIA specification page naming the draft article and the unverified numbering (SPEC-002 AC-024); a configuration page with the JSON schema v1 and a failure-behaviour table (`docs-writer`); an ADR only if the maintainer's answers change the architecture; `docs/INVENTORY.md` rows.
9. Build with zero warnings; `dotnet test` on the five projects; the per-flag measurement and `--check-justifications` commands.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Plan #816, Phase 7. Tests execute real package code (AGENTS.md section 9). Event-sourced compliance modules have
no InMemory stores: unit tests mock IAggregateRepository with NSubstitute; integration tests use Marten on
PostgreSQL via Testcontainers and the shared [Collection] fixture, WireMock through Encina.Testing.WireMock.
Assertions use Shouldly through Encina.Testing.Shouldly.

TASK:
Write the tests of the Tasks block including the AC-024 default test, the DI test, the .md justifications, the
documentation through the proper specialists, and run build, tests and coverage measurement.

KEY RULES:
- FakeTimeProvider for the loop; a test never swallows an exception thrown by the code it drives (including out
  of FakeTimeProvider.Advance). No Thread.Sleep.
- Documentation says "proposed" and "draft", names the draft article as given in SPEC-002 section 3.5 and marks
  it unverified; no hand-typed coverage figures (covref markers); no private client application named.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Compliance.DPIA.json
- docs/en/guides/TESTING.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (section 3.5, REQ-024, AC-024, REQ-050)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Article / Section | Relevance |
|----------|-------------------|-----------|
| GDPR (Reg. (EU) 2016/679) | Art. 35(1), 35(4), 35(5) | DPIA duty; national lists of processing that does or does not need a DPIA; current-law default |
| GDPR | Art. 35(6), Art. 64 | Consistency mechanism for the lists |
| Digital Omnibus COM(2025) 837 (draft) | GDPR Art. 35(4)–(6) as amended, per SPEC-002 §3.5 [K] | EU-wide lists by the EDPB; unverified against the published text; off by default |
| SPEC-002 | REQ-024, DEC-012, AC-024 | Draft source off by default; test that the default uses the national lists |
| SPEC-002 | REQ-050, P-41 (#1243) | The AEPD lists (Art. 35.4 and 35.5) load through this provider |
| SPEC-002 | REQ-061, REQ-062 | Tenant-aware and instrumented capabilities (matrix rows 10 and 2) |
| RFC 9110 | Status code semantics | URI provider classification |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IRiskCriterion`, `DefaultDPIAAssessmentEngine` | `src/Encina.Compliance.DPIA/Abstractions/IRiskCriterion.cs`, `DefaultDPIAAssessmentEngine.cs:222` | Evaluation seam; unchanged |
| `HighRiskTriggers`, `DPIAContext` | `src/Encina.Compliance.DPIA/Model/` | Trigger id space; `Metadata` for the sector |
| `DPIAReviewReminderService` | `src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs` | Background-loop precedent |
| `DPIAOptions`, `DPIAOptionsValidator` | `src/Encina.Compliance.DPIA/` | Settings and start-up validation |
| `DPIAHealthCheck` | `src/Encina.Compliance.DPIA/Health/DPIAHealthCheck.cs:44` | Staleness reporting |
| `DPIADiagnostics`, `DPIALogMessages` | `src/Encina.Compliance.DPIA/Diagnostics/` | Shared `ActivitySource` and `Meter` |
| `NIS2ResilienceHelper` | `src/Encina.Compliance.NIS2/NIS2ResilienceHelper.cs` | Keyed pipeline pattern, without swallowing |
| `DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds` | `src/Encina.Compliance.DataSubjectRights/DataSubjectRightsOptions.cs:96` | Naming and default-off precedent |
| `Encina.Testing.WireMock` | `src/Encina.Testing.WireMock/` | HTTP provider integration tests |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Second range for the assembly |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.DPIA` (existing) | 8800-8899 (`ComplianceDPIA`) | Highest id in use is 8896; three free, too few |
| `Encina.Compliance.DPIA` (criteria) | **6400-6429** (new `ComplianceDPIACriteria`) | Free per `EventIdRanges.cs` today and assigned to this family in the batch EventId registry (block 6400-6549); register first (ADR-021) and map as a second range of the assembly. Sibling plans: 6430-6449 (ENISA) and 6450-6549 (EHDS) |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Models and enums | 5 | criterion, origin, list, snapshot, source options |
| Interfaces | 2 | provider, catalog |
| Implementations | 7 | JSON provider, URI provider, mapper, catalog, loading service, data-driven criterion, resilience helper |
| Modified production files | 7 | options, validator, DI, health check, diagnostics, errors, `EventIdRanges` |
| Logging | 1 | `DPIACriteriaLogMessages` |
| Tests | ~20 | unit 9, guard 2, contract 2, property 1, integration 3, justifications 2, event-id test update 1 |
| Documentation and records | 6 | README, specification page, configuration page, changelog fragment, PublicAPI, INVENTORY |

Total about 48 files, above the 15-file limit of one worker PR (AGENTS.md §11). Proposed split into four PRs on #816: **PR A** (Phases 1-2: models, contracts, providers, mapper, with tests: about 14 files); **PR B** (Phase 3: catalog, loading service, data-driven criterion, with tests: about 10 files); **PR C** (Phase 4: options, validator, DI, DI test: about 8 files); **PR D** (Phases 5-6: health check, EventIds, diagnostics, resilience helper, with tests: about 10 files). Documentation goes with the PR of its area. PR B needs PR C only for registration, so the merge order is A, B, C, D.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete implementation prompt (all phases)</strong></summary>

```text
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable on, ROP with Either<EncinaError, T>, TimeProvider
injection, [LoggerMessage] logging with EventIds in registered ranges, TryAdd DI). AGENTS.md binds every change.
Encina.Compliance.DPIA is event-sourced on Marten (ADR-019). Issue #816 adds external loading of DPIA criteria.
The loader is a current-law capability; the EDPB harmonised list as a source (Digital Omnibus COM(2025) 837, draft)
is off by default and named by its draft article (SPEC-002 REQ-024, DEC-012, AC-024).
Plan: docs/plans/dpia-criteria-loader-implementation-plan-816.md (follow the Design Choices as decided in its
Maintainer Decisions section, once it exists).

IMPLEMENTATION OVERVIEW:
1. Models, IDPIACriteriaProvider (Either), IDPIACriteriaCatalog, error codes dpia.criteria_*.
2. JSON file and URI providers (https or loopback http, keyed Polly pipeline dpia-criteria, 1 MiB cap, depth
   limit), shared mapper and validation, custom providers.
3. DPIACriteriaCatalog (atomic snapshot), DPIACriteriaLoadingService (PeriodicTimer, TimeProvider, catch per
   iteration, IOptionsMonitor settings), LoadedCriteriaRiskCriterion (fail closed on unavailable catalog).
4. DPIAOptions fluent API LoadCriteriaFrom{Json,Uri,Provider<T>}, EnableOmnibusArticle35EdpbHarmonisedList
   (default false), validator rules, TryAdd registrations, hosted service only when a source is configured.
5. Health check degradation; EventIds 6400-6429 (register first); activity and counters.
6. Tests on every flag including the AC-024 default test; WireMock and file integration; .md justifications.
7. Docs by docs-writer, changelog fragment and PublicAPI by mechanical-fixer, zero warnings.

KEY PATTERNS:
- Fail closed: a required source that never loaded blocks via a VeryHigh CriteriaUnavailable risk item.
- Never swallow in background loops; never log document content, URLs with queries, or EncinaError.Message.
- Draft law off by default, named by its draft article, "proposed" never "will".
- Pre-1.0: no [Obsolete], no aliases. CRAP <= 10 on changed methods. Per-flag coverage targets.

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DefaultDPIAAssessmentEngine.cs
- src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs
- src/Encina.Compliance.DPIA/DPIAOptions.cs
- src/Encina.Compliance.NIS2/NIS2ResilienceHelper.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The loaded list is held in memory by the catalog; there is nothing further to cache. The issue's "Defer" is withdrawn |
| 2 | OpenTelemetry | ✅ | Activity `DPIA.CriteriaLoad`, counter `dpia.criteria.loads.total`, snapshot-age gauge (Phase 6) |
| 3 | Structured Logging | ✅ | `DPIACriteriaLogMessages` in the new range 6400-6429 (Phase 6) |
| 4 | Health Checks | ✅ | `DPIAHealthCheck` reports degraded or unhealthy on stale or failed sources (Phase 5) |
| 5 | Validation | ✅ | Each loaded criterion and the source configuration are validated (Phases 2 and 4) |
| 6 | Resilience | ✅ | Keyed Polly pipeline `dpia-criteria` for the URI source, no swallowing (Design Choice 6, Phase 5) |
| 7 | Distributed Locks | ❌ | Each process holds its own snapshot; there is no shared mutable state |
| 8 | Transactions | ❌ | No multi-operation write |
| 9 | Idempotency | ❌ | Loading is repeatable and side-effect free |
| 10 | Multi-Tenancy | ❌ | Criteria are the legal list in force for the deployment, global configuration |
| 11 | Module Isolation | ❌ | Same reason as multi-tenancy |
| 12 | Audit Trail | ⏭️ | Recording which list version was in force for a DPIA assessment is deferred to the issue file `artifacts/issues/plan-816-dpia-criteria-list-version-audit.md`; the snapshot already carries `listVersion` and `publishedAtUtc` (Design Choice 8) |

---

## Prerequisites & Dependencies

### Must coordinate

| Item | State (2026-11-11) | Relationship |
|------|--------------------|--------------|
| #1243 DPIA screening mapped to the AEPD Art. 35.4 and 35.5 lists (P-41) | Open | Loads the AEPD lists through `IDPIACriteriaProvider` with origin `NationalList`; whichever lands first defines nothing the other must change. The default test of #816 ("national lists by default") is fully meaningful once #1243 has landed |
| #817 Compliance options to `IOptionsMonitor` | Open | This plan reads `IOptionsMonitor<DPIAOptions>` only in the loading service; #817 migrates the other consumers |
| #409 DPIA module, #781 DPIA Marten migration (plan `dpia-es-migration-plan-781.md`) | Merged / planned | The module is on Marten; this plan adds no aggregate |
| #812 (plan `enisa-single-entry-point-implementation-plan-812.md`) | Open | Same keyed-pipeline pattern; separate EventId range 9700-9749 |
| #1186 SPEC-002 tracking | Open | AC-024: closes before `1.0.0-rc.1` |

### Advisable, not blocking

| Item | State | Relationship |
|------|-------|--------------|
| #1233 `Encina.Http` (authentication, client certificates) | Open | A future authenticated source can reuse the named client without a provider change |

### Missing prerequisites and findings, written as issue files

| File | Template | Finding |
|------|----------|---------|
| `artifacts/issues/plan-816-dpia-criteria-list-version-audit.md` | `[FEATURE]` | A DPIA assessment does not record which criteria list version was in force (GDPR Art. 5(2), Art. 35 accountability) |

---

## Next Steps

1. The maintainer decides Design Choices 4, 5, 6 and 7 when work on #816 starts; the orchestrator records them in a "Maintainer Decisions" section, updates each Chosen Option, then runs `adversarial-reviewer` on Choices 1, 2, 3 and 8.
2. Open the issue file above (`open-issue` skill) and link it here and from #816.
3. Link this plan from #816, then start PR A in a worktree (`worker-brief` skill) with the local CRAP table required in the brief.
4. Re-verify the draft article numbering and whether the EDPB publishes a machine-readable format within 30 days of any publication of COM(2025) 837 (SPEC-002 AC-025); then open the mapper issue for that format.
