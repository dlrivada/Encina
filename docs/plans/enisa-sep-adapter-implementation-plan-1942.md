# Implementation Plan: `Encina.Compliance.ENISA` — concrete ENISA Single Entry Point reporting adapter (Digital Omnibus COM(2025) 837, off by default)

> **Issue**: [#1942](https://github.com/dlrivada/Encina/issues/1942)
> **Type**: Feature
> **Verified against**: origin/main `c49b987e9` (2026-11-12)
> **Milestone**: none yet; the issue is blocked on an external publication (see Phase 0)
> **Complexity**: Medium (7 phases including a go/no-go gate, new package, no store, ~12 production files)
> **Estimated Scope**: ~600-1,000 lines of production code + ~1,200-1,800 lines of tests

---

## Summary

Build the concrete adapter for the **ENISA Single Entry Point (SEP)** on top of the reporting layer of #812: a new package `Encina.Compliance.ENISA` with a serializer that follows the published SEP schema, an `IIncidentReportingAdapter` marked `IsSingleEntryPoint = true`, and a registration extension `options.Reporting.AddEnisaSingleEntryPoint(...)`. The SEP is a feature of the Digital Omnibus proposal COM(2025) 837 (SPEC-002 §3.5: NIS2 Art. 23a and GDPR Art. 33(1) as amended). **This is draft law and the SEP API specification is not published.** Under SPEC-002 REQ-024 and DEC-012 the adapter is unreachable while `BreachNotificationOptions.Reporting.SingleEntryPoint.Enabled` is `false` (its default), and a test proves it. Because the schema, authentication and endpoints are unknown, this plan starts with a **go/no-go gate (Phase 0)**: nothing is implemented until the specification is published and has been read. Phases 1 to 6 are written for the two outcomes the issue's comment anticipates.

### Standards covered

- Digital Omnibus COM(2025) 837 (draft): reporting through an ENISA single entry point for several regimes (SPEC-002 §3.5: NIS2 Art. 23a, NIS2 Art. 23(12), GDPR Art. 33(1) as amended). **Unverified** against the published text: article numbers, the date from which the SEP applies, its schema and its authentication. The #812 issue body expects the API "18-24 months after the Regulation enters into force"; that expectation is also unverified. The procedure 2025/0360(COD) is still in committee (SPEC-002 §3.5).
- NIS2 (Directive (EU) 2022/2555) Art. 23(4): early warning (24 h), notification (72 h), final report (1 month). SPEC-002 §3.5 states the timelines are unchanged by the proposal; the deadline policy is #812's `CurrentLawReportingDeadlinePolicy`.
- GDPR Art. 33: current-law default, handled by #812's adapters.
- SPEC-002 REQ-024, DEC-012, AC-024: off by default, named by the draft article, a test that the default follows current law.

### What already exists

- Nothing of the adapter layer exists on `main` today. Plan #812 (`docs/plans/enisa-single-entry-point-implementation-plan-812.md`, issue #812 open) specifies `IIncidentReportingAdapter` (with `IsSingleEntryPoint`), `IIncidentReportSerializer`, `HttpIncidentReportingAdapter` + `HttpIncidentReportingAdapterOptions`, `IncidentReportingOptions` (`AddAdapter<T>()`, `AddHttpAdapter`, `Route`, `RouteForTenant`, `SingleEntryPoint.Enabled = false`, `ResiliencePipelineKey = "breach-reporting"`) and the start-up validator that refuses routes to an SEP adapter while the flag is off. This plan builds on those names; if #812's final shape differs, this plan changes with it.
- `src/Encina.Compliance.BreachNotification/` is the host package; `BreachNotificationOptions.cs` has no `Reporting` property yet.
- `src/Encina.Compliance.NIS2/DefaultNIS2IncidentHandler.cs:141` puts NIS2 incidents on the breach stream, so one incident can already be reported under GDPR and NIS2 (SPEC-002 §3.5).
- Search of `src/` confirms there is no `Encina.Compliance.ENISA` project and no `EnisaSingleEntryPointAdapter`.
- `#1233` (`Encina.Http`: authentication providers, client certificates) is open; `src/Encina.Testing.WireMock/` exists for HTTP stubs.
- The issue's comment of 2026-10-17 settles the placement: no placeholder adapter now; build `Encina.Compliance.ENISA` on the generic HTTP adapter; if the SEP turns out to be a plain JSON endpoint, reduce the work to the serializer plus configuration.

### Corrections to the issue body

- The issue's matrix marks Idempotency, Multi-Tenancy and Health Checks included; they are kept, but the **mechanism of each depends on the published specification** (for example the SEP's own idempotency key), so Phase 0 confirms them.
- "Authentication through the application's named `HttpClient` or `Encina.Http` (#1233), whichever exists by then": recorded as Design Choice 3, a Dilemma.
- The issue's Test Matrix names "WireMock stub of the published SEP API": the stub can be written only after publication; before that no test of the wire format is possible and none is planned.
- The acceptance criterion "Every applicable coverage flag reaches its target in `.github/coverage-manifest/`" needs a manifest for the new package (`.github/coverage-manifest/Encina.Compliance.ENISA.json`), created in Phase 1.

### Affected packages

- `Encina.Compliance.ENISA` (new): serializer, adapter or configuration extension, options, DI, health probe, diagnostics, README, `PublicAPI.*.txt`, `Encina.slnx` entry, coverage manifest.
- `Encina.Compliance.BreachNotification`: **no change expected**; the extension method is defined in the ENISA package on `IncidentReportingOptions` (from #812).
- `Encina` (core): `src/Encina/Diagnostics/EventIdRanges.cs`, one new range.
- Tests: `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.PropertyTests`, `Encina.IntegrationTests` (WireMock); `.md` justifications for load and benchmark.

### Provider category

None of the 10 database providers: no store. Submissions are recorded on the Marten breach stream by #812's reporting service (ADR-019, SPEC-002 DEC-008 (a)). The transport is HTTP; the transport category of AGENTS.md §5 (message brokers) does not apply.

---

## Design Choices

<details>
<summary><strong>1. Go/no-go gate — nothing is built before the SEP specification is published</strong></summary>

**Class:** Settled
**Settled by:** the maintainer's issue comment of 2026-10-17 on #1942 ("keep it off by default and do not ship a placeholder adapter") and AGENTS.md section 3 (no legacy or dead code). Related, not the basis: plan #812 Design Choice 8 rejects the placeholder adapter too (its option C), but that choice is still "(recommended, pending the maintainer)"; the gate here stands on the maintainer comment alone.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Phase 0 is a gate: implement only when ENISA has published the SEP API specification** | No guessing of schema or authentication; no dead code | The package cannot ship in 1.0 if ENISA publishes later (the SEP is not part of current law) |
| **B) Implement now against an assumed schema** | A visible type exists | Dead code until publication; high risk of a rewrite; invites misuse |

### Chosen Option: **A — Gate**

### Rationale

- Phase 0 produces a one-page record (spec version, schema, authentication, endpoints, test environment, rate limits, licence of the schema) committed in the plan. The gate also requires the adoption status of COM(2025) 837 to be checked; adoption is not required to build the adapter, which stays off by default either way.

</details>

<details>
<summary><strong>2. Adapter class versus configuration only</strong></summary>

**Class:** Settled
**Settled by:** the maintainer's issue comment of 2026-10-17 on #1942 (build on the generic adapter once the specification is published; reduce to serializer plus configuration if the SEP is a plain JSON endpoint). **Dependency on a pending #812 choice**: Variant 1 uses the generic `HttpIncidentReportingAdapter`, which is plan #812 Design Choice 8 option B, still "(recommended, pending the maintainer)"; it holds if the maintainer confirms #812 DC8 B, and otherwise Variant 2 applies.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Variant 1 — plain HTTP + JSON: `EnisaSepIncidentReportSerializer` plus `AddEnisaSingleEntryPoint` configuring the generic `HttpIncidentReportingAdapter` with `IsSingleEntryPoint = true`; no adapter class** | Least code; reuses #812's status classification, `Idempotency-Key` and probe | Only valid if the SEP follows ordinary HTTP semantics |
| **B) Variant 2 — a dedicated `EnisaSingleEntryPointAdapter : IIncidentReportingAdapter`** | Handles a custom protocol (multi-step upload, signed payloads, polling for receipts) | More code and tests |

### Chosen Option: **A for a plain HTTP JSON endpoint, B otherwise — decided by the Phase 0 record, not by preference**

### Rationale

- The rule is mechanical: if the specification needs more than one POST with an authentication header and a status-code response, Variant 2 applies.
- Both variants set `IsSingleEntryPoint = true`, so #812's gate (validator and route resolver) applies unchanged.

</details>

<details>
<summary><strong>3. Authentication of the SEP client</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The application supplies a named `HttpClient` configured with whatever the SEP requires (client certificate, bearer token handler); the package only references the client name** | No dependency on an unpublished spec or on #1233; works with any scheme | Each application writes its own handler |
| **B) Use `Encina.Http` authentication providers and client certificates (#1233)** | Reusable, tested handlers | #1233 is open; the schemes the SEP needs are unknown; a package dependency |
| **C) Options such as `ApiKey` or `ClientCertificatePath` on `EnisaSepOptions`** | Convenient | Secrets in options classes need `[JsonIgnore]` and a redacting `ToString()` (AGENTS.md §3); a scheme guess before publication |

### Chosen Option: **A — Named `HttpClient` supplied by the application** (recommended, pending the maintainer)

### Rationale

- I recommend A because it needs no assumption about the unpublished scheme and keeps secrets out of Encina options. B wins once #1233 has landed and the SEP uses a scheme #1233 supports; switching is then a registration change, not an API change. C is rejected unless the specification fixes a simple scheme, in which case the options class carries the AGENTS.md §3 safeguards (`[JsonIgnore]`, `ToString()` override).

</details>

<details>
<summary><strong>4. Schema conformance — runtime validation versus test-time conformance</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The serializer validates required fields in code; the published JSON Schema is copied under `tests/` and used only by conformance tests** | No runtime schema dependency; no redistribution of ENISA's schema in the package | Code and schema can drift; caught by the conformance tests |
| **B) Embed the schema in the package and validate every report at runtime with a JSON Schema library** | Always exactly conformant | A new dependency; the schema's licence may not allow redistribution (unverified); schema updates need a package release |

### Chosen Option: **A — Required-field checks in code, schema in tests** (recommended, pending the maintainer)

### Rationale

- I recommend A because a failed validation before submission already becomes `breach.reporting.report_incomplete` through #812's validator. B wins if ENISA publishes the schema under a licence that allows embedding and the schema is large or changes often.

</details>

<details>
<summary><strong>5. Gate that keeps the adapter unreachable while the flag is off</strong></summary>

**Class:** Settled
**Settled by:** SPEC-002 REQ-024, DEC-012 and AC-024 (off by default; a registration call that turned it on would violate REQ-024, so only option A complies). **Dependency on a pending #812 choice**: the mechanism (`Reporting.SingleEntryPoint.Enabled`, default `false`; the validator refuses routes to an `IsSingleEntryPoint` adapter) is plan #812 Design Choice 7 option A, still "(recommended, pending the maintainer)"; if the maintainer picks another gate there, this choice follows it.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEnisaSingleEntryPoint` registers the adapter but never sets the flag; #812's validator and route resolver make it unreachable until the application enables `SingleEntryPoint.Enabled`** | One gate for every SEP adapter; the test is direct | The application must set two things (the flag and a route) |
| **B) `AddEnisaSingleEntryPoint` sets the flag itself** | One call | An off-by-default draft behaviour turned on by a registration call; against REQ-024 |

### Chosen Option: **A — Registration never enables the flag**

### Rationale

- The AC test: default options with the adapter registered and a route to it; start-up validation fails with `breach.reporting.single_entry_point_disabled`; with the flag on and a route, a WireMock-stubbed submission is accepted.

</details>

<details>
<summary><strong>6. Per-tenant credentials and routes</strong></summary>

**Class:** Trivial

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEnisaSingleEntryPoint(string targetAuthority = "ENISA-SEP", ...)` can be called several times, each call with a distinct authority key and a distinct `HttpClientName`; `RouteForTenant` points a tenant to its own key** | Reuses #812's routing; credentials stay in the application's clients | The application registers one adapter per tenant that needs its own credentials |
| **B) A tenant-keyed credential store inside the package** | One adapter | A new store of secrets in a package that should hold none |

### Chosen Option: **A — One registration per credential set**

### Rationale

- Option B also satisfies REQ-061 (tenant-aware capabilities); it is rejected for a design reason, not a rule: this package should hold no secrets (Design Choice 3). Routing per tenant is `RouteForTenant` of plan #812 Design Choice 5 (string-keyed routes; still "(recommended, pending the maintainer)"), with a fail-closed fallback: a tenant without its own route falls back to the global route and never to another tenant's (AC-043).
- The validator requires `TargetAuthority` to be unique among adapters and the default is a constant, so calling the method twice with the default fails: every call after the first must pass a distinct authority key (and a distinct `HttpClientName`, because the named client of each call is keyed by it). A unit test pins this and the message names the offending key.

</details>

---

## Implementation Phases

### Phase 0: Specification Intake and Go/No-Go (no code)

> **Goal**: Replace every assumption with the published text, or stop.

<details>
<summary><strong>Tasks</strong></summary>

1. Confirm ENISA has published the SEP API specification and record its version, location and licence.
2. Record: payload schema (format, versioning), authentication scheme, endpoints (submit, status, test environment), idempotency mechanism, rate limits, error model, supported regimes (NIS2, GDPR, DORA, CRA) and the report phases it knows.
3. Check the adoption status of COM(2025) 837 (SPEC-002 §3.5, AC-025) and update the "draft" wording in the XML docs accordingly; the adapter stays off by default either way.
4. Apply Design Choice 2: choose Variant 1 or Variant 2 and update Phases 2-3.
5. Check that #812 has landed and that its names are the ones this plan uses; if not, wait or adapt.
6. Commit the one-page record in this plan under "Maintainer Decisions" or as a new section agreed with the maintainer; classify any new choice (Trivial, Settled, Dilemma). This is a docs-only pull request on the plan, and the first code pull request (A1) is blocked until it has merged.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 0</strong></summary>

```text
CONTEXT:
Issue #1942, plan docs/plans/enisa-sep-adapter-implementation-plan-1942.md. The ENISA Single Entry Point is part
of the Digital Omnibus proposal COM(2025) 837 (draft). The plan has no implementation until the specification is
published.

TASK:
Research only. Find the published SEP API specification, read it, and write the one-page record of Phase 0 (items
1-6 of the Tasks block). Write no code.

KEY RULES:
- Legal and technical claims you cannot verify against the published text are marked unverified; never repeat a
  claim from SPEC-002 or an issue body as fact.
- Report the specification licence; do not copy a schema into the repository without checking it.
- If the specification is not published, report "no-go" with the evidence and stop.

REFERENCE FILES:
- docs/plans/enisa-single-entry-point-implementation-plan-812.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (section 3.5, REQ-024)
```

</details>

### Phase 1: Package Scaffold and Serializer

> **Goal**: The new package builds, packs and has a manifest; the serializer follows the published schema.

<details>
<summary><strong>Tasks</strong></summary>

All paths are in `src/Encina.Compliance.ENISA/` unless noted.

1. `Encina.Compliance.ENISA.csproj`: references `Encina.Compliance.BreachNotification` and `Encina`; `Microsoft.CodeAnalysis.PublicApiAnalyzers`; `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`; package metadata and `README.md` like the other compliance packages. Add the project to `Encina.slnx`.
1a. `ProjectReference` edits in the consolidated test projects, in the pull request that first needs each (verified: `Encina.Compliance.BreachNotification.csproj` is referenced by `tests/Encina.UnitTests`, `GuardTests`, `ContractTests`, `PropertyTests`, `IntegrationTests` and `LoadTests`): `Encina.UnitTests.csproj` and `Encina.GuardTests.csproj` in A1; `Encina.ContractTests.csproj` and `Encina.PropertyTests.csproj` in A2; `Encina.IntegrationTests.csproj` in B (WireMock tests). The load and benchmark tests are justification files only, so `Encina.LoadTests.csproj` is not edited.
2. `.github/coverage-manifest/Encina.Compliance.ENISA.json` (written by `mechanical-fixer`): A1 carries the package-level targets only; A2, A3 and B each add the per-file entries (targets and one-sentence justifications) for the files they introduce, so `--check-justifications` passes at every pull request.
3. `Serialization/EnisaSepIncidentReportSerializer.cs` (public sealed, `IIncidentReportSerializer`): `Format` is the published schema's version label (provisional `"ENISA-SEP-v1"`, the name the #812 issue proposes), `ContentType` and `FileExtension` per the specification; maps `IncidentReport` to the SEP payload with `System.Text.Json` and a source-generated context; `Either` return; required-field checks (Design Choice 4); report content never appears in errors.
4. `EnisaSepOptions.cs` (public sealed): `Endpoint` (`Uri`, https), `HttpClientName`, `TargetAuthority` (default `"ENISA-SEP"`), `SupportedRegulations` (from the specification). No secret property (Design Choice 3).
4a. `EnisaSepErrorCodes.cs` (new), in the `breach.reporting.*` convention of #812, each created with an `ErrorCategory` (ADR-062, `EncinaErrors.Create(code, ErrorCategory, message, ...)`) and a unit test asserting `GetCategory()`: `breach.reporting.enisa_field_missing` (`Validation`: a required field of the SEP payload is absent; the error names the field, never its content) and `breach.reporting.enisa_options_invalid` (`InvalidRequest`: a malformed endpoint, a blank client name or a duplicate authority key). The gate failure `breach.reporting.single_entry_point_disabled` belongs to #812 and is not redefined here.
5. `PublicAPI.Unshipped.txt`: every public symbol (by `mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Plan #1942, Phase 1, executed only after the Phase 0 record exists. The reporting contracts come from plan #812
(IIncidentReportSerializer, IIncidentReportingAdapter, IncidentReportingOptions). Follow the structure of an
existing small compliance package such as src/Encina.Compliance.Attestation.

TASK:
Create the project, solution entry, README, coverage manifest request and the serializer and options as in the
Tasks block, following the published schema recorded in Phase 0.

KEY RULES:
- Nullable enabled, XML docs on every public API, names the draft article and "off by default".
- Never put report content, response bodies or EncinaError.Message in errors or logs.
- Options hold no secret (Design Choice 3 as decided); if one is added it gets [JsonIgnore] and a redacting
  ToString() (AGENTS.md section 3).

REFERENCE FILES:
- src/Encina.Compliance.Attestation/ (package layout)
- docs/plans/enisa-single-entry-point-implementation-plan-812.md (Phase 1 types)
```

</details>

### Phase 2: Adapter or Configuration Extension, and DI

> **Goal**: `AddEnisaSingleEntryPoint` registers the SEP adapter without enabling it.

<details>
<summary><strong>Tasks</strong></summary>

1. `ServiceCollectionExtensions.cs` / `EnisaReportingOptionsExtensions.cs`: `public static IncidentReportingOptions AddEnisaSingleEntryPoint(this IncidentReportingOptions options, Action<EnisaSepOptions> configure, string targetAuthority = "ENISA-SEP")` (guards on both arguments). **Variant 1**: registers `HttpIncidentReportingAdapter` with `IsSingleEntryPoint = true`, the serializer and the named client. **Variant 2**: registers `EnisaSingleEntryPointAdapter` (`Adapters/`, public sealed, `IIncidentReportingAdapter`, `IsSingleEntryPoint = true`) and the serializer.
2. `EnisaSepOptionsValidator.cs` (internal): `Endpoint` is an absolute https `Uri`, `HttpClientName` is non-blank, `TargetAuthority` is non-blank and unique among adapters (a second `AddEnisaSingleEntryPoint` call with the default authority fails with `breach.reporting.enisa_options_invalid`; Design Choice 6); failures are reported as `breach.reporting.enisa_options_invalid` in the validator result text, codes only.
3. The method never sets `Reporting.SingleEntryPoint.Enabled` (Design Choice 5).
4. DI test: builds the provider with `ValidateOnBuild` and `ValidateScopes` with the adapter registered and the flag off (start-up fails when a route reaches it) and on.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Plan #1942, Phase 2. #812's validator refuses routes to an IsSingleEntryPoint adapter while
Reporting.SingleEntryPoint.Enabled is false; this plan relies on that and must not duplicate or bypass it.

TASK:
Implement AddEnisaSingleEntryPoint, its options validator and registrations for the Variant chosen in Phase 0.

KEY RULES:
- TryAdd / TryAddEnumerable registrations; register every option type and dependency the adapter resolves
  (AGENTS.md section 3), proven by a DI test with ValidateOnBuild and ValidateScopes.
- The extension never enables the off-by-default flag.
- ArgumentNullException guards on public methods.

REFERENCE FILES:
- docs/plans/enisa-single-entry-point-implementation-plan-812.md (Phases 3-4)
- src/Encina.Compliance.NIS2/ServiceCollectionExtensions.cs (style)
```

</details>

### Phase 3: Cross-Cutting Integration

> **Goal**: Probe, idempotency, tenancy.

<details>
<summary><strong>Tasks</strong></summary>

1. `Health/EnisaSepProbe.cs` (public sealed, `IIncidentReportingAdapterProbe`): calls the SEP status or test endpoint recorded in Phase 0; omitted if the specification publishes none. #812's `BreachReportingHealthCheck` discovers it.
2. Idempotency: send `ReportId` as the SEP's idempotency mechanism if the specification defines one, otherwise as the `Idempotency-Key` header of the generic adapter.
3. Multi-tenancy: documentation and an integration test with two registrations and `RouteForTenant` (Design Choice 6).
4. Resilience: reuse #812's keyed pipeline `breach-reporting`; nothing new.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Plan #1942, Phase 3. Resilience, tenancy and audit come from plan #812; this phase only adds what is specific to
the SEP.

TASK:
Implement the probe (if the specification has a status endpoint), the idempotency mapping and the two-tenant test.

KEY RULES:
- Health data carries counts and states only; never payloads, URLs with secrets or error messages.
- No exception escapes a probe; a failure is a Left.

REFERENCE FILES:
- docs/plans/enisa-single-entry-point-implementation-plan-812.md (Phase 5)
- src/Encina.Testing.WireMock/
```

</details>

### Phase 4: Observability

> **Goal**: A registered EventId range and the shared activity and metrics.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs` (modify, register first, ADR-021): `public static readonly (int Min, int Max) ComplianceENISA = (6430, 6449);` with the summary "Encina.Compliance.ENISA — ENISA single entry point adapter".
2. `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (modify): map `"Encina.Compliance.ENISA"` to `[nameof(EventIdRanges.ComplianceENISA)]`.
3. `Diagnostics/EnisaLogMessages.cs`: `[LoggerMessage]` from 6430, packed: serializer validation failed (field names), submission classified (status class), probe result, registration summary. No payload, URL query or response body.
4. Metrics and spans: reuse the #812 activity `BreachNotification.ReportSubmission` and counter `breach.reporting.submissions.total` with `breach.reporting.authority = "ENISA-SEP"`; no new meter. Per ADR-054 D4 the tenant is a span attribute and a metric tag **only when multi-tenancy is enabled**, and is not emitted otherwise; no new tag carries a URL, credential or report content.

The range 6430-6449 sits in the block 6400-6549 assigned to this family in the batch EventId registry: 6400-6429 is plan #816 (DPIA criteria), 6450-6549 is plan #808 (EHDS); 9700-9749 is plan #812.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Plan #1942, Phase 4. ADR-021: register the range in src/Encina/Diagnostics/EventIdRanges.cs before use and map the
new assembly in EncinaEventIdAllocationTests.AssemblyRanges.

TASK:
Register ComplianceENISA = (6430, 6449), write EnisaLogMessages with [LoggerMessage], and run the architecture
tests.

KEY RULES:
- Packed EventIds, none outside 6430-6449. Never log EncinaError.Message, payloads, URL queries or response bodies.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 5: Testing

> **Goal**: Every flag at its target; the off-by-default gate proven.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit**: serializer mapping per regulation and phase, required-field failures, options validator, extension method guards and registrations, probe results; **the AC-024 test** (flag off: start-up fails for a route to the adapter; flag on: it resolves).
2. **Guard**: extension method, serializer, adapter or probe constructors, options validator.
3. **Contract**: #812's `IIncidentReportingAdapter` and `IIncidentReportSerializer` contract bases against the SEP implementations.
4. **Property**: serializer round trip against the published schema (schema copy under `tests/`, only if its licence allows; otherwise hand-written invariants).
5. **Integration**: WireMock stub built from the published specification: accepted, duplicate, transient failure then success, permanent rejection, authentication failure; two-tenant routing.
6. **Load / Benchmark**: `.md` justifications (low throughput, not a hot path).
7. **Per-flag targets** (AGENTS.md §9). The package is new, so there is no baseline (the manifest with the package-level targets, the `Encina.slnx` entry and the README arrive in the first code pull request A1; the per-file entries below are added by the pull request that introduces each file, A2, A3 or B); the manifest ends with at least these targets, each with the one-sentence justification below recorded in the manifest:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `Serialization/EnisaSepIncidentReportSerializer.cs` | 95 | 100 | 100 | 90 | 0 | A pure mapping: every regulation, phase and required-field failure is a unit case; property is 90 and not 95 because the schema round trip depends on a licence-permitting schema copy, otherwise on hand-written invariants. |
| `EnisaReportingOptionsExtensions.cs` / `Adapters/EnisaSingleEntryPointAdapter.cs` (Variant 2 only) | 95 | 100 | 100 | 0 | 90 | Registration and guards are fully unit-testable; the adapter's wire behavior is proven against the WireMock stub. |
| `EnisaSepOptionsValidator.cs` | 95 | 100 | 0 | 0 | 0 | Rules over one options object; each rule, including the duplicate authority, is one case. |
| `Health/EnisaSepProbe.cs` | 90 | 100 | 100 | 0 | 80 | The probe is a thin call to the status endpoint; success, failure and timeout are unit cases and the integration test runs it against the WireMock stub (80 because the stub cannot reproduce every transport failure). |
| `Diagnostics/EnisaLogMessages.cs` | 90 | 0 | 0 | 0 | 0 | Source-generated logging; one test per message through a capturing logger. |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Plan #1942, Phase 5. Tests execute real package code (AGENTS.md section 9). The reporting store is Marten (not
touched here); unit tests mock IBreachReportingService collaborators with NSubstitute. Assertions use Shouldly
through Encina.Testing.Shouldly; HTTP stubs use Encina.Testing.WireMock.

TASK:
Write the tests of the Tasks block, including the AC-024 gate test, and the .md justifications. Measure per-flag
coverage with the AGENTS.md section 9 commands.

KEY RULES:
- A test never swallows an exception thrown by the code it drives; no Thread.Sleep.
- The WireMock stubs reproduce the published specification, not an invented one.

REFERENCE FILES:
- tests/Encina.UnitTests/ (existing compliance test layout)
- .github/coverage-manifest/ (manifest format)
```

</details>

### Phase 6: Documentation and Finalization

> **Goal**: Docs name the draft article; changelog, PublicAPI, manifest and verification.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every public API, naming the draft article (as published, or as given in SPEC-002 §3.5 and marked unverified) and "off by default".
2. `changelog.d/1942-enisa-single-entry-point-adapter.added.md` (by `mechanical-fixer`).
3. `src/Encina.Compliance.ENISA/README.md` and a configuration page (by `docs-writer`): registration, the two-step enablement (flag plus route), authentication through the named client, per-tenant routes.
4. BreachNotification specification page (#1208): the SEP row moves from "slot" to "adapter available, off by default".
5. `docs/INVENTORY.md` rows, `ROADMAP.md` line if the milestone is affected. No ADR unless Phase 0 changes the architecture.
6. Build with zero warnings; `dotnet test` on the five projects; the per-flag measurement and `--check-justifications` commands of AGENTS.md §9.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Plan #1942, Phase 6. Documentation edits go to the docs-writer; changelog fragments, PublicAPI lines and coverage
manifests go to mechanical-fixer (path-ownership hooks enforce it).

TASK:
Finish the documentation, the changelog fragment, PublicAPI lines and the manifest; run the build, tests and
coverage commands.

KEY RULES:
- Draft law: say "proposed" and "draft"; never repeat an unverified legal claim as fact; no hand-typed coverage
  figures (covref markers); public text names no private client application.

REFERENCE FILES:
- changelog.d/README.md
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (section 3.5, REQ-024)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Article / Section | Relevance |
|----------|-------------------|-----------|
| Digital Omnibus COM(2025) 837 (draft) | Single entry point (SPEC-002 §3.5: NIS2 Art. 23a and 23(12), GDPR Art. 33(1) as amended) | The reason for the adapter; unverified against the published text; off by default |
| NIS2 (Directive (EU) 2022/2555) | Art. 23(4) | Early warning, notification, final report; timelines unchanged per SPEC-002 §3.5 |
| GDPR (Reg. (EU) 2016/679) | Art. 33(1), 33(3) | Notification content; current-law default handled by #812 |
| CRA (Reg. (EU) 2024/2847) | Art. 14 | A CRA report may count as NIS2 reporting under the proposal (SPEC-002 §3.5); enum value only in #812 |
| SPEC-002 | REQ-024, DEC-012, AC-024, REQ-061, REQ-062 | Off by default; tenant-aware and instrumented |
| ENISA SEP API specification | Not published (unverified) | The Phase 0 gate |
| RFC 9110 | Status code semantics | Generic HTTP adapter classification (#812) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IIncidentReportingAdapter`, `IIncidentReportSerializer`, `HttpIncidentReportingAdapter`, `IncidentReportingOptions` | Plan #812, `src/Encina.Compliance.BreachNotification/Reporting/` (not yet on `main`) | The contracts this package implements; the validator and route resolver that gate SEP adapters |
| `BreachNotificationOptions` | `src/Encina.Compliance.BreachNotification/BreachNotificationOptions.cs` | Gets `Reporting` from #812 |
| `DefaultNIS2IncidentHandler` | `src/Encina.Compliance.NIS2/DefaultNIS2IncidentHandler.cs:141` | NIS2 incidents reach the breach stream |
| `Encina.Testing.WireMock` | `src/Encina.Testing.WireMock/` | SEP stub in integration tests |
| `Encina.Compliance.Attestation` | `src/Encina.Compliance.Attestation/` | Layout of a small compliance package with an HTTP provider (`HttpAttestationProvider`, `HttpAttestationOptions`) |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Mapping for the new assembly |
| `Encina.Http` (#1233) | Not yet on `main` | Possible future authentication handlers |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.ENISA` | **6430-6449** (new `ComplianceENISA`) | Free per `EventIdRanges.cs` today and assigned to this family (block 6400-6549): 6400-6429 plan #816, 6450-6549 plan #808; plan #812 uses 9700-9749; register first (ADR-021) |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Project and package files | 4 | csproj, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, solution entry |
| Production | 6-8 | serializer and JSON context, options and validator, extension, adapter (Variant 2 only), probe, log messages |
| Modified production files | 1 | `EventIdRanges.cs` |
| Tests | ~14 | unit 6, guard 2, contract 2, property 1, integration 2, justifications 2, event-id test update 1 (counted in unit) |
| Documentation and records | 5 | README, configuration page, manifest, changelog fragment, INVENTORY |
| Test project references | 5 | `ProjectReference` edits in `Encina.UnitTests.csproj`, `Encina.GuardTests.csproj`, `Encina.ContractTests.csproj`, `Encina.PropertyTests.csproj`, `Encina.IntegrationTests.csproj` |

Total about 50 files across the pull requests below (a file edited in more than one pull request, such as the manifest and the `PublicAPI` files, is counted once per pull request), above the 15-file limit of one worker PR (AGENTS.md §11). Counts include tests, the coverage manifest and `PublicAPI` files. Proposed split on #1942, each pull request building alone:

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| Phase 0 | Docs-only pull request: the one-page specification record in this plan; blocks A1 | 1 |
| A1 | Scaffold: csproj, 2 `PublicAPI` files, `Encina.slnx` entry, coverage manifest (package-level targets), README, `EnisaSepOptions`, `EnisaSepOptionsValidator`, `EnisaSepErrorCodes`, 3 unit test files (options, validator, codes), 1 guard test file, and the `ProjectReference` edits of `Encina.UnitTests.csproj` and `Encina.GuardTests.csproj` | 15 |
| A2 | Serializer and its JSON context, with unit, guard, contract and property tests, manifest per-file entries and `PublicAPI` updates, and the `ProjectReference` edits of `Encina.ContractTests.csproj` and `Encina.PropertyTests.csproj` | 11 |
| A3 | `AddEnisaSingleEntryPoint` (and the adapter in Variant 2), DI test with `ValidateOnBuild`/`ValidateScopes`, the AC-024 gate test, unit and guard tests, manifest entries, `PublicAPI` | 8 |
| B | Probe, `EventIdRanges.cs` and `EncinaEventIdAllocationTests` edits, `EnisaLogMessages`, integration tests with WireMock (acceptance, two tenants), the `ProjectReference` edit of `Encina.IntegrationTests.csproj`, load and benchmark justification files, manifest entries | 14 |
| C | Phase 6 documentation: configuration page, INVENTORY, changelog fragment, BreachNotification spec page row | 5 |

A1 requires Phase 0 and #812's Phases 1 to 4 on `main`; A2, A3 follow A1; B follows A3; C last.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete implementation prompt (all phases)</strong></summary>

```text
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (nullable on, ROP with Either<EncinaError, T>, TimeProvider
injection, [LoggerMessage] logging with EventIds in registered ranges, TryAdd DI). AGENTS.md binds every change.
Issue #1942 adds the concrete ENISA Single Entry Point adapter on top of the reporting layer of #812. The SEP is a
Digital Omnibus proposal (COM(2025) 837, draft) and the API specification is not published: the adapter is off by
default (SPEC-002 REQ-024, DEC-012, AC-024) and implementation starts only after the Phase 0 record exists.
Plan: docs/plans/enisa-sep-adapter-implementation-plan-1942.md (follow the Design Choices as decided in its
Maintainer Decisions section, once it exists).

IMPLEMENTATION OVERVIEW:
0. Phase 0: read the published specification, write the one-page record, choose Variant 1 (serializer plus
   configuration of the generic HTTP adapter) or Variant 2 (dedicated adapter) by the rule of Design Choice 2.
1. New package Encina.Compliance.ENISA: csproj, PublicAPI files, README, manifest, solution entry; the serializer.
2. AddEnisaSingleEntryPoint(IncidentReportingOptions, ...) with IsSingleEntryPoint = true; it never enables
   Reporting.SingleEntryPoint.Enabled.
3. Probe, idempotency mapping, per-tenant registrations through #812's RouteForTenant.
4. EventIds 6430-6449 (register first), reuse of the #812 activity and counter.
5. Tests on every flag including the gate test; WireMock stubs built from the published specification.
6. Docs by docs-writer, changelog fragment, PublicAPI and manifest by mechanical-fixer, zero warnings.

KEY PATTERNS:
- No placeholder code before publication; unverified legal or technical claims are marked unverified.
- Fail closed; Either returns; no payload, URL query, response body or EncinaError.Message in logs or errors.
- Secrets never in options classes without [JsonIgnore] and a redacting ToString() (AGENTS.md section 3).
- Pre-1.0: no [Obsolete], no aliases. CRAP <= 10 on changed methods. Per-flag coverage targets.

REFERENCE FILES:
- docs/plans/enisa-single-entry-point-implementation-plan-812.md
- src/Encina.Compliance.Attestation/ (small package with an HTTP provider)
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Reports are never cached |
| 2 | OpenTelemetry | ✅ | Reuses the #812 activity and counter with `breach.reporting.authority = "ENISA-SEP"`; no new meter; the tenant is a span attribute and tag only when multi-tenancy is on (ADR-054 D4), and no tag carries a URL, credential or content (Phase 4) |
| 3 | Structured Logging | ✅ | `EnisaLogMessages` in the new range 6430-6449 (Phase 4) |
| 4 | Health Checks | ✅ | `EnisaSepProbe` against the SEP status or test endpoint, only if the specification publishes one (Phase 3) |
| 5 | Validation | ✅ | Required-field checks in the serializer and the options validator (Phases 1-2) |
| 6 | Resilience | ✅ | #812's keyed pipeline `breach-reporting`; nothing new (Phase 3) |
| 7 | Distributed Locks | ❌ | Concurrency handled by the breach stream (#812) |
| 8 | Transactions | ❌ | The submission is recorded by #812 in one aggregate save |
| 9 | Idempotency | ✅ | `ReportId` as the SEP's idempotency mechanism or `Idempotency-Key` (Phase 3; confirmed in Phase 0) |
| 10 | Multi-Tenancy | ✅ | One registration per credential set, each with a distinct authority key and client name, and `RouteForTenant` (Design Choice 6) |
| 11 | Module Isolation | ❌ | No module scoping |
| 12 | Audit Trail | ✅ | Submission events on the breach stream (#812, ADR-019) |

---

## Prerequisites & Dependencies

### Must coordinate

| Item | State (2026-11-11) | Relationship |
|------|--------------------|--------------|
| #812 Incident reporting adapters and the SEP slot | Open, plan merged | **Hard prerequisite**: every contract this plan uses is specified there. Implementation cannot start until #812's Phases 1 to 4 are on `main` |
| ENISA publication of the SEP API specification | External, not published (unverified) | **Hard prerequisite** (Phase 0 gate) |
| Adoption of COM(2025) 837 | In committee per SPEC-002 §3.5 | Not required to build; changes the wording of the docs |
| #1233 `Encina.Http` | Open | Optional authentication handlers (Design Choice 3) |
| #1208 Article-coverage specification for BreachNotification | Open | The SEP row |
| #813 Authority notification severity threshold | Open | Independent; the severity filter is #813's policy |

### Advisable, not blocking

| Item | State | Relationship |
|------|-------|--------------|
| #1242 Art. 33(3) package and Art. 34 communications | Open | Its content contributor enriches reports that this serializer maps |
| #804 DORA module | Open | The SEP will also carry DORA reports; `IncidentRegulation.DORA` exists in #812 for routing |

### Missing prerequisites and findings, written as issue files

None. The one gap found (the missing SEP specification) is already the blocking condition of #1942 itself.

---

## Next Steps

1. The maintainer decides Design Choices 3 and 4 when Phase 0 passes; the orchestrator records them in a "Maintainer Decisions" section, updates each Chosen Option, then runs `adversarial-reviewer` on Choices 1, 2, 5 and 6.
2. Do not brief a worker for Phases 1 to 6 before the Phase 0 record exists and #812's Phases 1 to 4 have merged.
3. Link this plan from #1942, and re-check the ENISA publication within 30 days of any publication of COM(2025) 837 (SPEC-002 AC-025).
