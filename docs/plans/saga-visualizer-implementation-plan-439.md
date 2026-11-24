# Implementation Plan: Saga Visualizer — Mermaid, Graphviz and PlantUML Diagrams from Saga Definitions

> **Issue**: [#439](https://github.com/dlrivada/Encina/issues/439)
> **Type**: Feature
> **Complexity**: Medium (7 phases, no database provider, pure text generation, about 54 files in 5 pull requests)
> **Estimated Scope**: ~900 lines of production code + ~1,800 lines of tests and documentation
> **Family**: saga family. It reads `SagaDefinition`/`ISagaState` (existing) and complements the saga state machine (#297, which exports DOT for state machines), the saga read side (#128, step history) and the CLI host (EPIC #2614). Siblings are referred to by issue number only.

---

## Summary

A developer reading a saga wants to see its flow; an operator looking at a stuck saga wants to see where it stopped. #439 generates the picture from the code: a saga definition becomes a diagram (Mermaid, Graphviz DOT, PlantUML, ASCII), and a saga row (`Status`, `CurrentStep`) highlights the current position and the compensation path. The output is text, so it can be pasted into a README, a pull request or a documentation page and rendered by the reader's own toolchain.

**What exists today** (`origin/main` at `ff9a579c1`, 2026-11-24):

- A saga in Encina is an ordered list of steps: `SagaDefinition.Create<TData>(sagaType).Step(...)...Build()` returns an immutable `BuiltSagaDefinition<TData>` with `SagaType`, `Steps` (each `SagaStepDefinition<TData>` has a `Name`, an `Execute` delegate and an optional `Compensate` delegate) and an optional `Timeout` (`src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs`). The definition is an instance: nothing in the repository registers or discovers definitions by name.
- The persisted row has `SagaType`, `Status` (string constants `Running`, `Completed`, `Compensating`, `Compensated`, `Failed`, `TimedOut`) and `CurrentStep`, an `int` that is the number of steps already completed: it starts at 0, `AdvanceAsync` increments it (`SagaOrchestrator.cs` line 210) and each compensation step decrements it (line 340). The index of the step that failed is not stored.
- There is no `ISagaState` implementation that "parses" into a graph, no `Encina.Diagnostics` package (the issue's "Other" box), no Developer Dashboard type (`git grep -E "(class|interface) \w*Dashboard" -- src` finds none) and no `visualize` CLI command: `src/Encina.Cli/Program.cs` registers `new`, `generate` and `add` only. ADR-058 defines the future in-app command host for commands that touch application data (EPIC #2614). The Aspire dashboard extension is the open issue #419.
- The state machine plan (#297) exports DOT for state machines (`ToDotGraph()`), not Mermaid or PlantUML.

**Corrections to the issue text** (the sketch cannot be built as written):

1. `SagaVisualizer.Generate<OrderFulfillmentSaga>()` assumes a saga class with a static structure. Encina sagas are built definitions (instances); the entry point takes a `BuiltSagaDefinition<TData>`.
2. "Parse `ISagaState` implementations" cannot yield structure: the row has no steps. The runtime overlay takes `(Status, CurrentStep)` from the row.
3. `diagram.SaveAsSvgAsync` and `SaveAsPngAsync` need an external renderer (Graphviz `dot`, Playwright); they are not part of this plan (Design Choice 4).
4. The Mermaid example names the saga states after outcomes ("PaymentPending", "PaymentProcessed"); the real source has step names only, so nodes are steps and the start and end markers.
5. `encina visualize saga ... --output mermaid` needs a command host and a way to find a definition by name (Design Choice 6); both are deferred.
6. "Developer Dashboard" and "Aspire Dashboard" panels do not exist; #419 is the open issue for the Aspire dashboard.

**Standards and references**: Mermaid state diagram syntax; Graphviz DOT language; PlantUML state diagram syntax; MassTransit `StateMachineGraphvizGenerator` (the issue's comparable). No law applies.

**Affected packages**: `Encina.Messaging` only (namespace `Encina.Messaging.Sagas.Visualization`). The issue also ticks `Encina` core and `Encina.AspNetCore`; neither changes.

**Provider category**: none. The feature reads no store and calls no external system, so the 10-provider matrix does not apply.

**Shared pieces and who owns them**: this plan owns `SagaDiagram`, `SagaVisualizer`, the renderers and the `saga.diagram_not_found` and `saga.diagram_state_invalid` codes. #297 owns the DOT export of state machines and the `Encina.StateMachine` package; the adapter that turns a state machine into a `SagaDiagram` is a deferred follow-up after #297 lands. #128 owns step history; an overlay from `SagaStepRecord`s is a deferred follow-up after #128 lands. This plan registers no EventId range.

---

## Design Choices

<details>
<summary><strong>1. Where the visualizer lives (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A namespace in `Encina.Messaging`** (`Encina.Messaging.Sagas.Visualization`) | No new package; sits next to `SagaDefinition`; no new dependency (string building only) | Every messaging consumer carries about 900 lines they may never call |
| **B) A new package `Encina.Messaging.Visualization`** | Strict pay-for-what-you-use; the CLI and a dashboard reference it alone | A csproj, a README, a manifest and a package for text generation; two places for saga code |

### Chosen Option: **A — A namespace in `Encina.Messaging`** (recommended, pending the maintainer)

### Rationale

- A wins because the code is static, allocates only when called and has no dependency; the issue's "Affected Packages" lists `Encina.Messaging`. B wins when the maintainer wants the CLI and the dashboard to take the visualizer without the messaging stack, which is plausible once EPIC #2614 lands.
- Under B the files move to `src/Encina.Messaging.Visualization/` and the project reference points at `Encina.Messaging`; the phases do not change.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. A neutral diagram model with one renderer per format (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SagaDiagram` (nodes, edges, optional runtime overlay) built once; internal renderers `Mermaid`, `Graphviz`, `PlantUml`, `Ascii` read it** | The overlay and the escaping are done once per format; each renderer is tested alone with a golden file | One extra model type |
| **B) Four generators that each walk the definition** | No model | The overlay logic and the step walk are repeated four times |

### Chosen Option: **A — A neutral model and four renderers**

### Rationale

The issue asks for four formats from one source and a highlight in each; a model keeps those two concerns apart.

- **Class:** Trivial
- **Settled by:** issue #439 (Output Formats table and "Highlight current state in runtime mode") and the comparable MassTransit `StateMachineGraphvizGenerator` named in the issue, which renders from a graph.

</details>

<details>
<summary><strong>3. What the diagram is generated from (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Step-list sagas now (`BuiltSagaDefinition<TData>`); state machines through an adapter after #297 lands** | Ships on code that exists; no dependency on an unmerged package | A state-machine saga has no Mermaid or PlantUML until the follow-up |
| **B) Both in this plan, with an adapter over the #297 `StateMachineDefinition`** | One release covers both | Depends on #297, whose package and graph accessor do not exist yet; the #297 plan exports DOT text only, so it needs a structured accessor added there |
| **C) Parse the generated DOT of #297 back into the model** | No new accessor | Parsing a text format that the same repository produces; fragile |

### Chosen Option: **A — Step-list sagas now** (recommended, pending the maintainer)

### Rationale

- A wins because every saga run today through `ISagaRunner` is a step list. B wins when the maintainer wants the visualizer to be the single export surface and accepts the dependency order #297 then #439.
- The follow-up is `artifacts/issues/saga-visualizer-state-machine-adapter.md`.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. How exact the runtime highlight is, and image export (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Text only. The overlay uses `(Status, CurrentStep)` from the row: steps below `CurrentStep` are completed, the step at `CurrentStep` is current while `Running`, and for `Compensating`, `Compensated`, `Failed` and `TimedOut` the steps at or above `CurrentStep` get one "rolled back or failed" style (the failed index is not persisted). SVG and PNG are left to the reader's toolchain** | Works with today's row; no external process; deterministic output | The failed step is not distinguished from the compensated ones until #128 step history exists; no image file |
| **B) Add exactness from the #128 step history (`SagaStepRecord` per step) in this plan** | Exact per-step outcome | Depends on #128, which is still a plan; the history is opt-in per application |
| **C) Add `SaveAsSvgAsync` and `SaveAsPngAsync` by running Graphviz or a headless browser** | Matches the issue's API | Spawns an external process (a security and portability concern); an optional tool that most CI images lack |

### Chosen Option: **A — Text and the row-level overlay** (recommended, pending the maintainer)

### Rationale

- A wins now. B wins when step history ships and operators need the exact failed step (an overload `WithHistory(IReadOnlyList<SagaStepRecord>)` is a deferred follow-up, `artifacts/issues/saga-visualizer-step-history-overlay.md`). C wins when a presentation workflow needs image files and the maintainer accepts an external tool (`artifacts/issues/saga-visualizer-image-export.md`).
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Step names are escaped for every format (Settled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every label is escaped by its renderer: DOT quoted strings (backslash, quote and newline escaped), Mermaid labels quoted with entity codes, PlantUML aliases generated (`s0`, `s1`) with the name only in quoted labels; control characters removed** | A step name such as `Pay" -> evil` cannot add an edge or break the diagram; node ids are generated, never taken from names | A little code per renderer |
| **B) Use step names as node ids** | Shorter output | A name with a space, a colon or a quote breaks or injects into the diagram |

### Chosen Option: **A — Generated ids and escaped labels**

### Rationale

Step names are free text chosen by the author and, through a catalog, can reach a web page or a pull-request comment. Generated ids keep the structure fixed whatever the name is.

- **Class:** Settled
- **Settled by:** the Graphviz DOT language reference (quoted ID escaping), the Mermaid state diagram documentation (entity codes for special characters) and the PlantUML state diagram documentation (alias and quoted description syntax), the named comparable syntaxes of the issue.

</details>

<details>
<summary><strong>6. Finding a definition by name needs a registered catalog (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ISagaDiagramCatalog` with `Get(string sagaType)` and `List()`, filled by `AddSagaDiagram(definition)` in DI; opt-in** | The future CLI and dashboard can list and render by name; a missing name is a `Left` | The application registers each definition it wants discoverable |
| **B) Scan assemblies for sagas** | Nothing to register | Definitions are instances built by code, not types, so there is nothing to scan |

### Chosen Option: **A — A registered catalog**

### Rationale

`SagaDefinition.Create(...).Build()` returns an instance, so discovery by name is impossible without registration. A name that is not in the catalog is a configuration entry that is missing, so `saga.diagram_not_found` is `ErrorCategory.Internal` under the ADR-062 amendment (a configuration or topology entry such as a handler or route is `Internal`, a runtime entity addressed by an id is `NotFound`).

- **Class:** Trivial
- **Settled by:** `src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs` (a definition is an instance), ADR-062 (amendment of 2026-11-20, rule R1) and `AGENTS.md` section 1 (every feature opt-in).

</details>

---

## Implementation Phases

### Phase 1: Core Models & Abstractions

> **Goal**: The neutral model, the builder and the error codes.

<details>
<summary>Tasks</summary>

1. **`SagaDiagram`** (sealed class, `src/Encina.Messaging/Sagas/Visualization/`): `string SagaType`, `IReadOnlyList<SagaDiagramNode> Nodes` (`Id` generated `s0..sN`, `Label`, `Kind`: `Start`, `Step`, `End`), `IReadOnlyList<SagaDiagramEdge> Edges` (`From`, `To`, `Label`, `Kind`: `Forward`, `Compensation`), `TimeSpan? Timeout`, and the render methods `ToMermaid(options)`, `ToGraphviz(options)`, `ToPlantUml(options)`, `ToAscii(options)` returning `string`.
2. **`SagaDiagramOptions`** (sealed record: `bool ShowCompensation = true`, `bool ShowTimeout = true`, `SagaDiagramDirection Direction = LeftToRight`).
3. **`SagaVisualizer`** (static): `SagaDiagram FromDefinition<TData>(BuiltSagaDefinition<TData> definition)` (null check; nodes: start, one per step, end; forward edges in order; a compensation edge from step `i` back to step `i-1` where `Compensate` is not null, labelled `compensate`); the output order is deterministic.
4. **Error codes** in `SagaErrorCodes` (ADR-062 categories): `saga.diagram_not_found` = `Internal` (a catalog entry); `saga.diagram_state_invalid` = `Validation` (a `CurrentStep` below 0 or above the step count, or an unknown status). Messages name the saga type and the code only, never the data.
5. **`PublicAPI.Unshipped.txt`** lines and the manifest entries of the new files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>).
- Issue #439; plan docs/plans/saga-visualizer-implementation-plan-439.md. Dilemmas D1-D3 are recommended, pending the maintainer: read the Maintainer Decisions section if it exists and follow the answers.
- A saga is a BuiltSagaDefinition<TData> (ordered Steps with Name, Execute, optional Compensate; optional Timeout); CurrentStep in the row is the number of completed steps.

TASK:
Create the model, the options, SagaVisualizer.FromDefinition and the two error codes of Phase 1 Tasks with unit and guard tests.

KEY RULES:
- Node ids are generated; a step name is only ever a label.
- Output is deterministic (stable ordering, no timestamps, no GUIDs).
- Every new error code has an ADR-062 ErrorCategory set at creation; messages never embed saga data.
- Sealed types, XML docs on every public member, nullable enabled, no [Obsolete].

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs
- src/Encina.Messaging/Sagas/SagaErrorCodes.cs
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs (SagaStatus, CurrentStep semantics)
```

</details>

---

### Phase 2: Renderers

> **Goal**: Four text formats from the model.

<details>
<summary>Tasks</summary>

1. **`MermaidRenderer`** (internal static): `stateDiagram-v2`, `[*] --> s0`, labels as quoted descriptions (`s1 : "ReserveInventory"` with the escaping of Design Choice 5), forward edges, dashed-style compensation edges as labelled transitions, the timeout as a `note`.
2. **`GraphvizRenderer`**: `digraph`, `rankdir` from the direction, `node [shape=box]`, quoted labels with `\\`, `\"` and `\n` escaped, compensation edges `style=dashed, color=orange`.
3. **`PlantUmlRenderer`**: `@startuml` ... `@enduml`, `state "Label" as s1`, `s0 --> s1`.
4. **`AsciiRenderer`**: a vertical list `[ Start ] -> [ step ] -> ... -> [ End ]` with `(compensates)` markers, no box drawing, safe for a console.
5. Every renderer removes control characters from labels, caps a label at 120 characters with an ellipsis, and ends with one trailing newline.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #439, Phase 2. SagaDiagram exists (Phase 1).

TASK:
Implement the four internal renderers and wire them to SagaDiagram.ToMermaid/ToGraphviz/ToPlantUml/ToAscii, with golden-file unit tests and a property test.

KEY RULES:
- Labels are escaped per format; the property test (FsCheck) proves that for any step name the number of nodes and edges in the output equals the model's.
- Golden files are checked in under the test project and compared exactly.
- No reflection; no I/O; pure string building (StringBuilder).

REFERENCE FILES:
- docs/en/guides/TESTING.md
- tests/Encina.UnitTests/Messaging/Sagas/SagaRunnerTests.cs
```

</details>

---

### Phase 3: Runtime State Overlay

> **Goal**: Highlight the current position from a saga row.

<details>
<summary>Tasks</summary>

1. **`SagaDiagram.WithState(string status, int currentStep)`** returning `Either<EncinaError, SagaDiagram>`: `Left(saga.diagram_state_invalid)` when `currentStep` is outside `0..stepCount` or `status` is not one of the six `SagaStatus` values; otherwise a copy whose nodes carry a `RuntimeState` (`Completed`, `Current`, `Pending`, `RolledBackOrFailed`).
2. **Mapping** (Design Choice 4): `Running` at `k`: steps `< k` completed, step `k` current (when `k < stepCount`); `Completed`: all completed; `Compensating`, `Compensated`, `Failed`, `TimedOut`: steps `< k` completed, steps `>= k` rolled back or failed.
3. **Renderers**: Mermaid `classDef` plus `class` lines, DOT `fillcolor`, PlantUML `<<current>>` stereotype with colors, ASCII `*` marker; colors follow the issue (Running blue, Completed green, Failed red, compensation orange).
4. **Convenience**: `SagaVisualizer.WithState(diagram, ISagaState state)` reads `Status` and `CurrentStep` only; it never reads `Data` or `ErrorMessage`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #439, Phase 3. ISagaState.CurrentStep is the number of completed steps; the failed step index is not persisted.

TASK:
Implement WithState, the mapping table of Design Choice 4 and the overlay in the four renderers, with golden tests for Running, Completed, Compensating, Failed and TimedOut.

KEY RULES:
- Never read ISagaState.Data or ErrorMessage; never include a saga id in the diagram.
- An out-of-range step or an unknown status returns Left with saga.diagram_state_invalid, never throws.
- Output stays deterministic.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/ISagaState.cs
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
```

</details>

---

### Phase 4: Configuration & DI — The Catalog

> **Goal**: Register definitions so tools can find them by name.

<details>
<summary>Tasks</summary>

1. **`ISagaDiagramCatalog`**: `Either<EncinaError, SagaDiagram> Get(string sagaType)` (`Left(saga.diagram_not_found)`), `IReadOnlyList<string> List()`.
2. **`SagaDiagramCatalog`** (internal sealed, singleton, immutable after build) filled from registered `SagaDiagramRegistration` entries; two definitions with the same `SagaType` fail at the first resolve with `Left`.
3. **`AddSagaDiagram<TData>(this IServiceCollection, BuiltSagaDefinition<TData> definition)`**: `TryAddSingleton` of the catalog and one `AddSingleton` registration entry; opt-in.
4. **DI proof**: a test that builds the provider with `ValidateOnBuild` and `ValidateScopes`, resolves the catalog and renders every registered definition.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #439, Phase 4.

TASK:
Add the catalog, the registration extension and the DI proof test.

KEY RULES:
- Opt-in: nothing is registered unless AddSagaDiagram is called.
- A duplicate saga type is a Left, not an exception; a missing one is saga.diagram_not_found.
- TryAdd* registration; ValidateOnBuild and ValidateScopes in the test.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Apply the matrix items marked integrate or defer.

<details>
<summary>Tasks</summary>

1. **Validation**: `WithState` validates its input (Phase 3); the catalog validates uniqueness (Phase 4).
2. **Deferred surfaces** (each its own issue, none planned here): the `visualize` CLI command waits for the CLI command host (ADR-058, EPIC #2614); the dashboard panel waits for #419; an HTTP endpoint would need tenant filtering from #738 and authorization; the issue files are listed in the matrix.
3. **Documentation hook**: the guide shows how to paste a Mermaid diagram into a README; the XML-docs auto-include of the issue is documentation only (a snippet in the guide), not code.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #439, Phase 5. Phases 1-4 exist.

TASK:
Confirm that no phase reads a store or the saga data, and that the public surface is the one in PublicAPI.Unshipped.txt.

KEY RULES:
- The visualizer calls no store and no external system; a test with a substituted ISagaStore asserts zero calls.
- Public text never names a private client application.

REFERENCE FILES:
- docs/architecture/adr/058-encina-cli-command-architecture.md
```

</details>

---

### Phase 6: Testing

> **Goal**: Every flag at its target.

<details>
<summary>Tasks</summary>

| Flag | Scope | Per-file target (justification) |
|------|-------|---------------------------------|
| Unit | builder, four renderers (golden files), overlay mapping, catalog | `SagaVisualizer.cs` 95, `MermaidRenderer.cs` 95, `GraphvizRenderer.cs` 95, `PlantUmlRenderer.cs` 95, `AsciiRenderer.cs` 95, `SagaDiagram.cs` 90 (pure string building, every branch reachable with in-memory definitions) |
| Guard | `FromDefinition(null)`, `WithState`, `AddSagaDiagram`, catalog arguments | `SagaVisualizer.cs` 90, `SagaDiagramServiceCollectionExtensions.cs` 90 (null and empty arguments are the only branches) |
| Contract | none: the renderers are internal and there is no public interface with several implementations | justification file |
| Property | for any step names the output has the model's node and edge counts and no unescaped quote; output is deterministic | `GraphvizRenderer.cs` 80, `MermaidRenderer.cs` 80, `PlantUmlRenderer.cs` 80 (one invariant per renderer over generated names) |
| Integration | none: no database, transport or external system is touched; a justification file | justification file |

Load and benchmark: a justification `.md` each (a diagram is generated on demand from at most a few dozen steps).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Issue #439, Phase 6.

TASK:
Write the tests of the table, the golden files and the justification files.

KEY RULES:
- Tests execute real code; Shouldly through Encina.Testing.Shouldly; no Thread.Sleep.
- InternalsVisibleTo for Encina.UnitTests, Encina.GuardTests and Encina.PropertyTests already exists in src/Encina.Messaging/Encina.Messaging.csproj; no csproj edit is needed for the renderers.

REFERENCE FILES:
- src/Encina.Messaging/Encina.Messaging.csproj
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

### Phase 7: Documentation & Finalization

> **Goal**: The mandatory documentation set.

<details>
<summary>Tasks</summary>

1. XML docs with a runnable `<example>` on `SagaVisualizer` and `SagaDiagram`; `changelog.d/439-saga-visualizer.added.md`.
2. `docs/features/saga-visualizer.md` (formats, overlay limits, escaping, how to render the output), the `Encina.Messaging` README, `docs/INVENTORY.md`, `ROADMAP.md`.
3. `dotnet build --configuration Release` with zero warnings; per-flag coverage measured against the manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #439, Phase 7. Delegated to the docs-writer agent for pages under docs/ and READMEs.

TASK:
Write the feature guide with one Mermaid and one DOT example, the README additions, the changelog fragment and the INVENTORY and ROADMAP lines.

KEY RULES:
- No hand-typed coverage figures; cite cov: references.
- Public text never names a private client application.

REFERENCE FILES:
- docs/plans/dsr-implementation-plan-404.md
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Lines |
|----|---------|-------|-------|
| 1 | Phase 1 and the Mermaid renderer: `SagaDiagram`, nodes and edges, `SagaDiagramOptions`, `SagaVisualizer`, `MermaidRenderer` (5), `SagaErrorCodes` (modified), PublicAPI, manifest, 2 unit tests, 1 golden file, 1 guard test | 12 | 900 |
| 2 | Renderers: Graphviz, PlantUML, ASCII (3), `SagaDiagram` render methods (modified), PublicAPI, manifest, 3 unit tests, 3 golden files, 1 property test | 13 | 1,100 |
| 3 | Phase 3 overlay: `WithState` and the mapping (modified `SagaDiagram`, 4 renderers modified), PublicAPI, manifest, unit test, 5 golden files | 13 | 1,000 |
| 4 | Phase 4 catalog: interface, implementation, extension (3), PublicAPI, manifest, unit test, DI proof, guard test, justification files (3) | 11 | 700 |
| 5 | Phase 7 documentation: guide, README, INVENTORY, ROADMAP, changelog fragment | 5 | 350 |

Total: 5 pull requests, 54 files (upper estimate); no pull request exceeds 15 files or 1,500 lines. The pull requests merge in order 1 to 5.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| Mermaid state diagram syntax | `stateDiagram-v2`, transitions, notes, `classDef` | The Mermaid renderer |
| Graphviz DOT language | `digraph`, quoted IDs and their escapes | The Graphviz renderer and its escaping |
| PlantUML state diagram syntax | `state "Label" as id`, stereotypes | The PlantUML renderer |
| MassTransit `StateMachineGraphvizGenerator` | Graph built from a state machine, rendered to DOT | The issue's comparable |
| ADR-018, ADR-058, ADR-062 | Cross-cutting check, CLI command architecture, error categories | Matrix, deferred CLI surface, code categories |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `SagaDefinition` and `BuiltSagaDefinition<TData>` | `src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs` | Immutable definition with the ordered step list: the source of the diagram |
| `SagaStepDefinition<TData>` | `src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs` | Step name and whether a compensation delegate exists |
| `ISagaState` and `SagaStatus` | `src/Encina.Messaging/Sagas/ISagaState.cs`, `SagaOrchestrator.cs` | `Status` and `CurrentStep` (the number of completed steps) feed the overlay; nothing else is read |
| `SagaErrorCodes` | `src/Encina.Messaging/Sagas/SagaErrorCodes.cs` | `saga.*` code style for the two new codes |
| `ErrorCategory` | `src/Encina/Errors/ErrorCategory.cs` | Category set at creation (ADR-062) |
| State machine plan DOT export | issue #297 | Planned DOT export for state machines; adapter deferred |
| `Encina.Cli` | `src/Encina.Cli/Program.cs` | Today only `new`, `generate`, `add`; the `visualize` command waits for ADR-058's command host (#2614) |
| `Encina.Messaging.csproj` | `src/Encina.Messaging/Encina.Messaging.csproj` | Already declares `InternalsVisibleTo` for the unit, guard, contract, property and integration test assemblies |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (saga store) | 2200-2299 | Full (#720, #738, #750, #1237); not used |
| **This plan** | **none** | **No `[LoggerMessage]`: the visualizer is a development-time generator that logs nothing (matrix item 3), so no range is registered and no `PublicAPI` line for a range is needed** |

### Provider Matrix

| Provider category | Applies | Reason |
|-------------------|---------|--------|
| Database (10), Caching, Transport, Locks, Validation, Cloud | No | The feature reads no store and calls no external system |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Source | ~14 | Model, options, visualizer, four renderers, catalog (3), `SagaErrorCodes` edit |
| PublicAPI and manifests | ~5 | One PublicAPI edit and one manifest edit per code pull request (4) plus the first |
| Tests and golden files | ~30 | Unit (4), guard (2), property (1), DI proof, golden files (9), justification files (3) |
| Documentation | ~5 | Guide, README, INVENTORY, ROADMAP, changelog fragment |
| **Total** | **~54 in 5 pull requests** | Equals the sum of the split table |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Railway Oriented Programming (Either<EncinaError, T>); no [Obsolete]; XML docs on every public API; zero warnings.
- Issue #439: a saga visualizer. Sagas are BuiltSagaDefinition<TData> instances (ordered steps, optional compensation, optional timeout); the row has Status and CurrentStep (completed steps). Plan: docs/plans/saga-visualizer-implementation-plan-439.md. Dilemmas D1-D3 are recommended, pending the maintainer; read the Maintainer Decisions section first if it exists.

IMPLEMENTATION OVERVIEW:
1. Phase 1: SagaDiagram model, SagaVisualizer.FromDefinition, two error codes. Phase 2: Mermaid, Graphviz, PlantUML and ASCII renderers.
2. Phase 3: WithState overlay from (Status, CurrentStep). Phase 4: the opt-in catalog and AddSagaDiagram.
3. Phase 5: confirm no store or data is read. Phase 6: tests with golden files. Phase 7: documentation.

KEY PATTERNS:
- Generated node ids; labels escaped per format; deterministic output.
- The overlay never reads ISagaState.Data or ErrorMessage and never prints a saga id.
- A config-like miss (saga.diagram_not_found) is Internal; an invalid caller state (saga.diagram_state_invalid) is Validation; both are Left.
- No store calls, no external process, no EventIds, no telemetry.
- TryAdd* registration; DI test with ValidateOnBuild and ValidateScopes.
- Tests: Shouldly through Encina.Testing.Shouldly, golden files, FsCheck property tests, per-flag targets from the manifest, CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Messaging/Sagas/SagaErrorCodes.cs
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: diagram generation is deterministic string building from an in-memory definition; nothing is read from a store |
| 2 | OpenTelemetry | ❌ | Not applicable: a development-time generator, not a runtime operation |
| 3 | Structured Logging | ❌ | Not applicable: nothing to log; no EventId range is registered |
| 4 | Health Checks | ❌ | Not applicable: no checkable dependency |
| 5 | Validation | ✅ | Phases 3-4: `WithState` validates the step and the status (`saga.diagram_state_invalid`); the catalog rejects duplicates; the issue marked it not applicable because there is no user input, but the runtime overlay takes one |
| 6 | Resilience | ❌ | Not applicable: no external call (image export, which would call one, is deferred) |
| 7 | Distributed Locks | ❌ | Not applicable: single-process, read-only |
| 8 | Transactions | ❌ | Not applicable: read-only |
| 9 | Idempotency | ❌ | Not applicable: the output is deterministic for the same input |
| 10 | Multi-Tenancy | ❌ | Not applicable here: the visualizer reads no store; a future HTTP surface or dashboard that loads a saga by id must apply the tenant rule of #738 (recorded in `artifacts/issues/saga-visualizer-http-surface.md`) |
| 11 | Module Isolation | ⏭️ | Deferred (as the issue says): module-scoped catalogs follow `ModuleId` on messaging entities (#747); the catalog is a plain registration until then |
| 12 | Audit Trail | ❌ | Not applicable: the visualizer changes nothing; reads of saga rows through a future HTTP surface would be audited there (same issue file) |

---

## Prerequisites & Dependencies

- No blocking issue: `BuiltSagaDefinition<TData>` and `ISagaState` exist on `origin/main`.
- Deferred follow-ups, each its own issue file: the state machine adapter (after #297), the step history overlay (after #128), image export, the `visualize` CLI command (after the CLI command host of EPIC #2614, ADR-058) and the dashboard panel (#419).
- #1237 (saga correlation) and #720 do not touch this feature.

---

## Next Steps

1. The maintainer answers Dilemmas D1 to D3 when work on #439 starts; the answers go into `## Maintainer Decisions` at the end of this plan.
2. Open the deferred issue files named above and link them from #439; #419 and EPIC #2614 are existing open issues.
3. Brief pull request 1 first; pull requests 2 to 5 follow in order.
