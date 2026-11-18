# Implementation Plan: .NET Aspire integration (umbrella) — family map, naming and configuration contract, conformance tests and quickstart

> **Issue**: [#440](https://github.com/dlrivada/Encina/issues/440)
> **Type**: Feature (umbrella; tests and documentation only, no change under `src/`)
> **Complexity**: Low (6 phases, 2 pull requests of 4 and 8 files)
> **Estimated Scope**: ~350 lines of tests + ~700 lines of documentation
> **Family**: Aspire (aspire-1): #416 hosting package, #417 service defaults, #419 dashboard commands, #421 deployment; #607 (`Encina.ServiceDiscovery.Aspire`), #423 (modules in the AppHost) and #188 (dashboard guide) are related plans; the family continues in #424, #425, #382, #387 and #391. **This plan owns the family layout and naming rules, the answer to the entry-point name, the parity tests between the AppHost and the application sides, and the quickstart.**

---

## Summary

#440 is the umbrella of the Aspire integration. Its body proposes one package, `Encina.Aspire`, with `EncinaResource`, an `AddEncina` fluent API, a dashboard panel, health checks, OTLP helpers and service discovery. The closed duplicate #449 added the name `AddEncinaAspireDefaults` and README examples (comment of 2026-10-06). Every item now has a dedicated issue. State of `origin/main` (`528f34f7e`, re-checked on 2026-11-18; the commits since `ec129cae5` change none of the facts below), verified by reading and grep (the only Aspire project in `src/` is `Encina.Aspire.Testing`; nothing named `EncinaResource`, `AddEncinaDefaults` or `AddEncinaAspireDefaults` exists in `src` or `tests`):

| #440 acceptance item | Owner | What the owner delivers |
|----------------------|-------|-------------------------|
| `EncinaResource` as an Aspire resource | #416 | `AddEncina`, `WithEncina`, the model-only resource, the environment contract |
| Dashboard panel for Encina status | #419 (its Design Choice 5, a Dilemma pending the maintainer) and #417 | `Aspire.Hosting` 13.6.5 has no panel extension, so what is delivered is commands over HTTP endpoints, resource relationships and the telemetry that arrives over OTLP; whether that meets "panel" and "metrics endpoint" is the maintainer's answer to #419 D5, not decided here |
| Automatic health check registration: Outbox, Inbox, Saga | #417 | The checks of the declared patterns (plus the dead letter check and a start check) |
| Automatic health check registration: Cache (`CacheHealthCheck`) | #754 (open, health checks for the caching providers; the query cache check `AddEncinaQueryCache` exists in `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs`) | Not delivered by #417, which handles the five messaging patterns |
| Automatic health check registration: `TransportHealthCheck` per configured transport | #622 (open, health checks for all components; the transport checks live in each transport package) | Not delivered by #417 |
| OTLP configuration helpers | #1043 (open: the OTLP exporter as an opt-in option of `Encina.OpenTelemetry`) and #417 (`WithEncina` telemetry, no exporter in the library, ADR-054 settled point 1) | The template and the AppHost already provide the OTLP endpoint; an Encina exporter helper is #1043's decision |
| Service discovery integration | #607 | `Encina.ServiceDiscovery.Aspire` with `AddEncinaServiceDiscoveryAspire`; the `config.UseAspireServiceDiscovery()` shape of the issue is not built (the satellite rule is `AddEncina{Feature}{Provider}`) |
| Documentation with an Aspire quickstart | **This plan** | The quickstart page, the family section of the README and the executable examples |
| Resilience defaults (deferred in the issue) | #417 (Design Choice 8, not delivered) and #1233 (outbound HTTP, open) | The Aspire template already adds the standard HTTP resilience handler |
| Deployment publishers | #421 | Docker Compose and Kubernetes processor settings; Azure Container Apps is #2698 (outside 1.0) |
| Modules in the AppHost | #423 | `WithEncinaModules` in the hosting package |

Differences from the issue text found while reading the code and the packages:

- The package structure of the issue (`Encina.Aspire` with a `ServiceDefaults/` folder, plus `Encina.Aspire.Hosting`) overlaps #416 and #417, which name their own packages. Design Choice 1 decides the layout.
- Aspire is at 13.6.5 in the repository (`Directory.Packages.props`, after #3491), not 9.4. The packages of the family must move together (Design Choice 5).
- `WithReference(encina)` of the #440 snippet does not exist for a model-only resource (the #416 plan, Design Choice 2); the project attaches the resource with `WithEncina(encina)`.

**Delivered**: (1) `tests/Encina.ContractTests/Aspire/EnvironmentContractTests.cs`, which runs the AppHost writer (#416) into the application reader (#417) so the environment contract cannot drift; (2) `tests/Encina.UnitTests/Testing/Architecture/AspireNamingRulesTests.cs`, the naming rules of the family as an architecture test; (3) `tests/Encina.UnitTests/Aspire/AspireQuickstartTests.cs`, which runs the snippets of the quickstart; (4) the quickstart page, the family section of the root README, `docs/features/index.md`, inventory and roadmap lines, an ADR on the layout and the contract (the number is assigned when the ADR pull request opens) and the changelog fragment.

**Not in scope**: any package (they have their own plans), a sample solution under `examples/` (#87 owns the scaffold and #188 plans the observability sample), AI features (#422, outside 1.0 by SPEC-000 section 6).

**Shared pieces and owners**, so the sibling family aspire-2 reuses them:

| Piece | Owner | Reused by |
|-------|-------|-----------|
| Hosting package, `EncinaResource`, `AddEncina`, `WithEncina` | #416 | #424, #387, #391 |
| Environment contract `Encina__Aspire__*` (writer #416, reader #417) and its parity test | #416 and #417; the test is this plan | #382, #425 |
| `AddEncinaDefaults` and `EncinaDefaultsOptions` | #417 (the name is Design Choice 2 here) | #425 |
| `WithEncinaCommands`, `EncinaCommandsOptions`, the failure-code table | #419 | #425 |
| `EncinaProcessorOptions`, `EncinaProcessorAnnotation`, `WithEncinaProcessor` | #421 | #387, #391, #2698 |
| Naming rule: `AddEncina*` on the builder, `WithEncina*` on a resource, `AddEncinaDefaults` on the host builder | This plan | all |
| EventId ranges: `AspireServiceDefaults` 800-819 (#417), `AspireHosting` 820-839 (#419), 840-849 free; aspire-2 uses its own block 850-899 | The family block | aspire-2 |

**Affected packages**: none under `src/`; test projects (`Encina.ContractTests`, `Encina.UnitTests`) and the documentation.

**Provider category**: none.

---

## Design Choices

<details>
<summary><strong>1. Package layout of the family (Dilemma D1, shared with #417 D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No `Encina.Aspire` assembly: packages named by role, `Encina.Aspire.Hosting` (AppHost), `Encina.Aspire.ServiceDefaults` (application), `Encina.Aspire.Deployment` (+ `.Kubernetes`), `Encina.ServiceDiscovery.Aspire` (#607), `Encina.Aspire.Testing` (exists)** | Each side takes only its dependencies (an AppHost does not load application packages and the reverse); one issue, one package | More packages than the issue's sketch; the umbrella has no package to install |
| **B) A package `Encina.Aspire` with no code that depends on `Encina.Aspire.ServiceDefaults`** | One name to install on the application side | A meta-package is a package for a dependency line; the AppHost side still needs `Encina.Aspire.Hosting` |
| **C) The issue's `Encina.Aspire` with `EncinaResource` and `ServiceDefaults/` inside** | Matches the #440 sketch | Mixes AppHost and application dependencies in one assembly and duplicates #416 and #417 |

### Chosen Option: **A — packages by role** (recommended, pending the maintainer)

### Rationale

A wins because the AppHost and the application are different processes with different dependencies (the AppHost takes no reference to Encina application packages; the application takes none to Aspire hosting). B wins if the maintainer wants a single name for "Aspire support" in a README. C is rejected: it would put hosting and application code in one assembly, which no Aspire integration does. A decision of this weight gets an ADR in Phase 6.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Name of the application-side entry point (Dilemma D2, shared with #417 D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaDefaults`, the name of the #417 issue; it sits beside the template's `AddServiceDefaults` and follows `AddEncina*`** | Short; named by two issues | The #449 triage comment asked to carry `AddEncinaAspireDefaults` into #440 |
| **B) `AddEncinaAspireDefaults`, the name carried from #449** | Says it is Aspire-specific | Longer; the package namespace already says so |

### Chosen Option: **A — `AddEncinaDefaults`** (recommended, pending the maintainer)

### Rationale

Exactly one name exists: the other would be a compatibility alias, which AGENTS.md section 3 forbids. A wins on brevity and because two issues use it; B wins if the maintainer prefers the wording of the closed duplicate. When the maintainer chooses B, the #417 plan and the quickstart change by renaming one method.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Every quickstart example is run by a test</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A unit test builds the same AppHost model and the same service defaults as each snippet of the page; a renamed API breaks the build of the tests, not the reader** | The page cannot drift; no extra project | A snippet and its test are kept side by side |
| **B) Snippets typed in the page with no test** | Cheapest | They rot silently; the documentation rule forbids unverified identifiers |
| **C) A sample solution under `examples/` that CI builds** | A runnable sample | Owned by #87 and #188 (the scaffold is a Dilemma there); a solution needs the Aspire AppHost SDK that the repository does not carry yet |

### Chosen Option: **A — executable snippets**

### Rationale

Both A and C satisfy the rule, and A needs no new project; C stays with #87 and #188.

**Class:** Trivial
**Settled by:** `.claude/skills/encina-docs/SKILL.md` (every code example names real API, house rule 3, cited the same way in the #188 plan).

</details>

<details>
<summary><strong>4. The family conventions are enforced by tests (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An architecture test lists the public extension methods of the Aspire packages and checks the verbs (`AddEncina*` on `IDistributedApplicationBuilder`, `WithEncina*` on `IResourceBuilder<T>`, `AddEncinaDefaults` on `IHostApplicationBuilder`), and a contract test runs the AppHost writer into the application reader** | The sibling family aspire-2 inherits the rule without reading this plan; a drifted key or name fails a build | A reflection test over public API shapes covers no production line, so it carries no coverage target |
| **B) Documentation only** | No test | The contract between two packages written by different pull requests is the kind of thing that drifts |

### Chosen Option: **A — conformance tests** (recommended, pending the maintainer)

### Rationale

A wins because the environment keys are written in one package and read in another, and the next family adds more. B wins if the maintainer judges two tests too much for an umbrella. The contract test executes the real writer (`WithEncina` over the real Aspire model) and the real reader (`EncinaAspireConfiguration.Read` over a real `IConfiguration`), so it is not a type-only test.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Version policy of the Aspire packages of the family (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The stable Aspire hosting packages move together: one version, central in `Directory.Packages.props`, for `Aspire.Hosting`, `Aspire.Hosting.Docker`, `Aspire.Hosting.Testing` and `Aspire.Hosting.PostgreSQL` (13.6.5 today); a preview satellite, if #421 keeps one, carries its own preview version of the same release line** | One place to bump; the satellites require at least the same `Aspire.Hosting`, so a mismatch cannot hide | A bump moves every package together |
| **B) Independent versions per package, each at or above 13.6.5** | Each moves when it likes; still satisfies the advisory fix | Mixed versions are only caught at restore (NU1605, NU1608) or at run time |

### Chosen Option: **A — stable packages together** (recommended, pending the maintainer)

### Rationale

Both options satisfy GHSA-f7vh-h5g3-7jrh (#3491, closed: 13.6.5 is the first patched release, recorded in the comment of `Directory.Packages.props`), so this is a policy choice. A wins for a small family that bumps together; B wins if one package needs a newer Aspire feature before the others can move. Today `Directory.Packages.props` pins only `Aspire.Hosting.Testing` and `Aspire.Hosting.PostgreSQL` (lines 159-160); #416 adds `Aspire.Hosting` and #421 adds `Aspire.Hosting.Docker`. The nuspec of `Aspire.Hosting.Docker` 13.6.5 declares `Aspire.Hosting` 13.6.5 as a NuGet minimum version. `Aspire.Hosting.Kubernetes` exists only as `13.6.5-preview.1.26559.7` and whether it is pinned depends on the answer to #421 D1.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Conformance tests (pull request 1)

> **Goal**: The AppHost and the application sides cannot drift, and the naming rules are enforced.

<details>
<summary>Tasks</summary>

1. `tests/Encina.ContractTests/Aspire/EnvironmentContractTests.cs`: builds a real `DistributedApplication.CreateBuilder()` model, `AddEncina("encina", o => { o.UseOutbox = true; o.UseSagas = true; })`, a container resource with `WithEncina(encina)`, runs the `EnvironmentCallbackAnnotation` callbacks into a dictionary, maps `__` to `:` into an in-memory `IConfiguration` and passes it to `EncinaAspireConfiguration.Read` of #417: exactly the declared patterns come back, an undeclared pattern comes back false, and the five key names of the writer equal the five of the reader (both classes are `internal` and visible through `InternalsVisibleTo` for `Encina.ContractTests`). The project references of `Encina.ContractTests.csproj` to `Encina.Aspire.Hosting` (added by #416 pull request 2) and `Encina.Aspire.ServiceDefaults` (added by #417 pull request 1, which ships its own contract test) already exist, so this plan edits no csproj.
2. `tests/Encina.UnitTests/Testing/Architecture/AspireNamingRulesTests.cs`: reflects the public extension methods of the assemblies whose name starts with `Encina.Aspire` (except `Encina.Aspire.Testing`, whose `WithEncinaTestSupport` is a test helper) and asserts the verb rules; a method that breaks a rule fails with its full name. A reflection test over an assembly the project does not reference would check nothing, so the test first asserts that the expected assemblies were found and loaded (`Encina.Aspire.Hosting`, `Encina.Aspire.ServiceDefaults`, `Encina.Aspire.Deployment`; and `Encina.Aspire.Deployment.Kubernetes` when its pull request has merged, listed in the test as an optional entry) and that each exposes at least one rule-checked method; a missing required assembly fails the test.
3. `tests/Encina.UnitTests/Aspire/AspireQuickstartTests.cs`: one test per snippet of the quickstart (Design Choice 3), built from the same calls as the page.
4. No `src/` file changes, so no per-file coverage target and no new manifest entry are added (the one manifest edit of the split table only raises the existing Contract target of `EncinaAspireEnvironment.cs`); the changelog fragment of Phase 6 covers the user-visible documentation.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #440; plan docs/plans/aspire-integration-implementation-plan-440.md. D1, D2, D4 and D5 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists. The packages of #416 (pull request 2) and #417 (pull requests 1 and 2: `AddEncinaDefaults` ships in pull request 2) are merged.

TASK:
Write the environment contract test, the naming architecture test and the quickstart snippet tests.

KEY RULES:
- Tests execute the real writer and the real reader; never compare constants alone.
- The naming test names the offending method in its failure message.
- No test swallows an exception; Shouldly through Encina.Testing.Shouldly.
- A nullable argument that accepts null gets a valid-result test, not an exception test.

REFERENCE FILES:
- src/Encina.Aspire.Hosting (#416)
- src/Encina.Aspire.ServiceDefaults (#417)
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- tests/Encina.ContractTests
```

</details>

---

### Phase 2: Quickstart snippets

<details>
<summary>Tasks</summary>

1. The snippets of the quickstart (an AppHost with a database container resource added with `AddContainer` (the PostgreSQL hosting package `Aspire.Hosting.PostgreSQL` is pinned in `Directory.Packages.props` but no test project references it, so the snippets use the core `Aspire.Hosting` API that `Encina.Aspire.Hosting` brings in; a snippet with `AddPostgres` is shown in the page text only if a test project gets the reference), `AddEncina`, a project with `WithEncina`, the service defaults project calling `AddEncinaDefaults`, and the dashboard command of #419) are written first as tests (Phase 1 task 3) and copied into the page by `docs-writer`, so the page shows only compiled code.
2. Each snippet ends with what the dashboard shows (resources, health checks, the Encina traces and metrics) in words that match #188's guide, which owns the telemetry walkthrough.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #440, Phase 1 tests exist. The reference application is named only as "the reference application".

TASK:
Hand the snippets to docs-writer with the exact method names and the facts to state.

KEY RULES:
- Every identifier is verified in src/ or in the pinned Aspire package; no figure is typed by hand.

REFERENCE FILES:
- docs/plans/aspire-dashboard-guide-implementation-plan-188.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

### Phase 3: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. This umbrella adds no runtime code, so each function is owned by a package plan: OpenTelemetry, logging and health by #417, commands by #419 (logging 820-824), processors by #421, modules by #423.
2. Failure codes: none are created here; the codes of the family are in the #417 plan (`Validation`, `FailedPrecondition`) and the #419 plan (the command failure table with its `ErrorCategory` column).
3. Tenancy (ADR-054 D4) and fail-closed gates: stated once in the quickstart's "what is not included" box, with the owner of each (the tenant tag is emitted by `Encina.OpenTelemetry`; commands never invent a tenant and never send an anonymous request).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #440; the cross-cutting matrix is the contract.

TASK:
Verify the matrix rows against the sibling plans and the merged code.

KEY RULES:
- Never claim a feature that a sibling plan does not deliver; cite the owner by issue number.

REFERENCE FILES:
- AGENTS.md section 6
```

</details>

---

### Phase 4: Observability

<details>
<summary>Tasks</summary>

1. No `ActivitySource`, `Meter` or log class here. The family block of EventIds 800-849 is assigned: `AspireServiceDefaults` (800, 819) by #417, `AspireHosting` (820, 839) by #419, 840-849 free (aspire-2 uses 850-899).
2. The quickstart lists the real telemetry the dashboard shows for the patterns: the instruments of `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs` (for example `encina.outbox.pending_count`) and the sources `WithEncina` subscribes; the metric names of the original issue (`encina_outbox_pending_total` and siblings) do not exist and are not documented.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #440. Names come from the code.

TASK:
Check every telemetry name in the quickstart against src/.

KEY RULES:
- A name that does not exist in src/ is removed, never kept.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 5: Testing

<details>
<summary>Tasks</summary>

1. **Unit** (naming architecture test, quickstart tests) and **Contract** (environment contract) as in Phase 1. **Guard, Property, Integration, Load, Benchmark**: not applicable, no `src/` file is added; this is stated in the pull request instead of justification files, because the test folders for them hold no feature to justify.
2. **Per-file targets**: none; the pull requests of the packages (#416, #417, #419, #421) carry the per-file targets of their `src/` files. The reference tests of this plan reach the package code through the real writer and reader, which adds contract-flag coverage to `EncinaAspireEnvironment.cs`; the Contract target of `EncinaAspireConfiguration.cs` is carried by #417's own pull request 1 (its `DeclarationReaderContractTests`) and the Contract flag of `EncinaAspireEnvironment.cs` is set to 0 in #416 and raised by this plan's pull request 1, so no package pull request fails its target before this one merges.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Issue #440.

TASK:
Run the three test files and report the results.

KEY RULES:
- Targets never go down; a failing parity test blocks the pull request of the package that broke it.

REFERENCE FILES:
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 6: Documentation & Finalization (pull request 2)

<details>
<summary>Tasks</summary>

1. `docs/features/aspire.md` (through `docs-writer`, one Diataxis quadrant: a tutorial that takes an application from no Aspire to the dashboard, with the snippets of Phase 2; a table of the family with owners; "what is not included and who owns it"); a family section in the root `README.md` and an entry in `docs/features/index.md`; `docs/INVENTORY.md` and `ROADMAP.md` lines.
2. An ADR on the layout (Design Choice 1), the naming rule (Design Choice 2) and the environment contract (the number is assigned when the ADR pull request opens), written after the maintainer answers D1 and D2.
3. `changelog.d/440-aspire-integration.added.md` (through `mechanical-fixer`).
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test` of the three test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Issue #440; the packages of the family are implemented.

TASK:
Write the documentation set of the Phase 6 tasks through the specialists that own them.

KEY RULES:
- Pages and the ADR through docs-writer; the fragment through mechanical-fixer; never edit CHANGELOG.md; coverage figures are cited with covref markers, never typed; the reference application is named only as "the reference application".

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/index.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Approx. lines |
|----|---------|------:|--------------:|
| 1 | `EnvironmentContractTests`, `AspireNamingRulesTests`, `AspireQuickstartTests`; the `Encina.Aspire.Hosting` manifest edit that raises the Contract target of `EncinaAspireEnvironment.cs` | 4 | 450 |
| 2 | `docs/features/aspire.md`, root `README.md` edit, `docs/features/index.md` edit, `docs/INVENTORY.md` edit, `ROADMAP.md` edit, the ADR, `docs/architecture/adr/index.md` entry, changelog fragment | 8 | 760 |

PR 1 needs #416 pull request 2 and #417 pull requests 1 and 2 merged (`AddEncinaDefaults` and `EncinaDefaultsExtensions` ship in #417's pull request 2; the quickstart tests call it and the naming test needs a rule-checked ServiceDefaults method); PR 2 needs PR 1 and the maintainer's answers to D1 and D2. No production code and no interface changes, so no implementer is touched. A pull request that measures over 15 files or 1,500 lines when it is briefed is split again.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 1, 3, 5 and 6 | Opt-in, no compatibility aliases, Cloud category, 12 functions | D1, D2, matrix |
| SPEC-000 section 6 and DEC-009 | Aspire integration that runs locally is in 1.0; AI/LLM (#422) and cloud services without a free emulator (#2698) are out | Scope |
| ADR-054 | Exporters outside the library; tenant tag | Phase 3 |
| ADR-062 | Error category contract | Phase 3 (codes live in the package plans) |
| ADR-021 | EventId ranges | Phase 4 (block 800-849) |
| `Aspire.Hosting` 13.6.5 | The AppHost model and its capabilities | D5, the panel finding |
| `.claude/skills/encina-docs/SKILL.md` | Documentation house style | D3, Phase 6 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `Encina.Aspire.Testing` | `src/Encina.Aspire.Testing/` | The only Aspire package today; shows the repository conventions |
| `EncinaEventIdAllocationTests` | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | Pattern for an architecture test over assemblies |
| `Encina.Aspire.Hosting`, `Encina.Aspire.ServiceDefaults`, `Encina.Aspire.Deployment` | #416, #417, #421 | What the tests and the page exercise |
| The #188 guide and sample | `docs/plans/aspire-dashboard-guide-implementation-plan-188.md` | Owns the telemetry walkthrough and the `examples/` sample |
| `Encina.ServiceDiscovery.Aspire` | #607 | Service discovery leg of the family |
| `AddEncinaModules`, `WithEncinaModules` | #423 | Modules leg of the family |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | This plan adds no logging. The family block 800-849 (`artifacts/decisions/plan-batch/eventid-blocks.md`) is split in the plans of #417 (800-819) and #419 (820-839); 840-849 are free |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 0 | None |
| Tests and manifests | 4 | Three test files and one manifest edit (no csproj edit) |
| Documentation | 8 | Page, README, features index, inventory, roadmap, ADR, ADR index entry, changelog fragment |
| **Total** | **12 files in 2 pull requests** | The split table (4 + 8) is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #440 (umbrella): the Aspire family has dedicated plans (#416 hosting, #417 service defaults, #419 commands, #421 deployment). Plan: docs/plans/aspire-integration-implementation-plan-440.md; D1, D2, D4 and D5 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (2 pull requests, each builds alone):
1. The environment contract test (writer to reader), the naming architecture test and the quickstart snippet tests (Phases 1 and 2).
2. The quickstart page, README, features index, inventory, roadmap, ADR and changelog fragment (Phase 6).

KEY PATTERNS:
- Packages by role; the AppHost takes no application package and the application takes no hosting package; one Aspire version for all of them.
- The environment contract Encina__Aspire__* is written by #416 and read by #417; the contract test runs the real writer into the real reader.
- AddEncinaDefaults (or the name the maintainer chooses) is the only application entry point; no alias exists.
- Every snippet in the quickstart is run by a test; telemetry names come from src/.

REFERENCE FILES:
- docs/plans/aspire-hosting-implementation-plan-416.md
- docs/plans/aspire-service-defaults-implementation-plan-417.md
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- Directory.Packages.props
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: tests and documentation only |
| 2 | OpenTelemetry | ⏭️ | Owned by #417 (open, planned); the quickstart documents what the dashboard shows |
| 3 | Structured Logging | ⏭️ | Owned by #417 (800-803) and #419 (820-824); no logging here |
| 4 | Health Checks | ⏭️ | Owned by #416 (the resource check) and #417 (the application checks) |
| 5 | Validation | ❌ | Not applicable: no input is accepted; the packages validate their own input |
| 6 | Resilience | ⏭️ | Deferred as in the issue: #1233 for outbound HTTP (open) and #417 Design Choice 8; the Aspire template already adds the standard HTTP handler |
| 7 | Distributed Locks | ❌ | Not applicable: no runtime code |
| 8 | Transactions | ❌ | Not applicable: no runtime code |
| 9 | Idempotency | ❌ | Not applicable here: the second-call guards belong to the package plans and their tests |
| 10 | Multi-Tenancy | ❌ | Not applicable: no tenant data; the reserved `default` tenant segment does not apply here (the #419 plan covers the tenant input of commands) |
| 11 | Module Isolation | ⏭️ | Owned by #423 (open, planned) |
| 12 | Audit Trail | ❌ | Not applicable: developer infrastructure; the audit of operator replay is #2013 through #419 |

---

## Prerequisites & Dependencies

- Pull request 1 needs #416 pull request 2 (the writer), #417 pull requests 1 (the reader and the `Encina.ContractTests.csproj` reference) and 2 (`AddEncinaDefaults`), #419 pull requests 1 and 2 (the quickstart includes the dashboard command) and #421 pull request 1 (the `Encina.UnitTests.csproj` reference to `Encina.Aspire.Deployment`; `Encina.Aspire.Deployment.Kubernetes` is optional in the naming test); pull request 2 needs the maintainer's answers to D1 and D2 (the ADR and the page state them).
- #440 can close when its acceptance items map to merged owners and the quickstart is merged; the aspire-2 issues (#424, #425, #382, #387, #391) do not block it.
- #607, #423 and #188 are independent plans that the quickstart links by issue number.

---

## Next Steps

1. The maintainer answers D1, D2, D4 and D5 when work on #440 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Pull request 1 once the writer and the reader exist; pull request 2 last.
