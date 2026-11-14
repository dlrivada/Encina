# Implementation Plan: Streaming Documentation — `IStreamRequest`, `IEncina.Stream`, Stream Pipeline Behaviors

> **Issue**: [#903](https://github.com/dlrivada/Encina/issues/903)
> **Type**: Feature (documentation only, no production code)
> **Complexity**: Low (3 phases, 5 files)
> **Estimated Scope**: one page of about 300 lines of Markdown, three small edits, one changelog fragment

---

## Summary

Write `docs/features/streaming.md`, the first page about Encina streaming. The root `README.md` ("Streaming", from line 356) showcases the feature but links to no detailed page, and `docs/features/index.md` does not list one. The page covers the public streaming API read from `src/Encina`:

- `IStreamRequest<out TItem>` (`src/Encina/Abstractions/IStreamRequest.cs`) and `IStreamRequestHandler<in TRequest, TItem>` (`src/Encina/Abstractions/IStreamRequestHandler.cs`, whose `Handle` returns `IAsyncEnumerable<Either<EncinaError, TItem>>`).
- `IEncina.Stream(request, ct)` and `Stream(request, context, ct)` (`src/Encina/Abstractions/IEncina.cs`, lines 144 and 172), implemented in `src/Encina/Core/Encina.Stream.cs` and `src/Encina/Core/StreamDispatcher.cs`.
- Error handling: a null request, a missing handler and a refused ambient request context each yield one `Left` and end the stream; a `Left` produced by the handler travels as an item; cancellation is logged by `StreamCancelled` (EventId 104); the page logs the error code, never `EncinaError.Message` (`AGENTS.md` section 3).
- `IStreamPipelineBehavior<TRequest, TItem>` (`src/Encina/Abstractions/IStreamPipelineBehavior.cs`) registered with `EncinaConfiguration.AddStreamPipelineBehavior<TBehavior>()` and its staged overloads (`src/Encina/Core/EncinaConfiguration.cs`, lines 364-406). `src/` ships no built-in stream behavior, so the page shows a user-written behavior and does not claim that caching or transactions apply or do not apply.
- Performance: backpressure (the consumer's `await foreach` pace drives the producer), memory and buffering; the benchmark `tests/Encina.BenchmarkTests/Encina.Benchmarks/StreamRequestBenchmarks.cs` is cited, not its figures.

One defect found while reading the sources and recorded as a task of Phase 3: the handler in the README streaming sample returns `IAsyncEnumerable<Product>`, which does not match `IStreamRequestHandler<,>.Handle` (it returns `IAsyncEnumerable<Either<EncinaError, Product>>`), so the sample does not compile. The README edit of this feature fixes the sample.

**Which provider category applies**: none. The EF Core, Dapper and MongoDB samples illustrate data sources; they are not provider features.
**Affected packages**: `Encina` (core), documented only.

---

## Design Choices

No open design choices. Every choice follows a cited convention:

| Id | Choice | Class | Settled by | Verified |
|----|--------|-------|------------|----------|
| 1 | One page `docs/features/streaming.md` with just-the-docs front matter (`parent: "Features"`); the navigation entry is a line in `docs/features/index.md` because the repository has no `docs/toc.yml` | Trivial | `.claude/skills/encina-docs/SKILL.md` section 1 and the sibling page `docs/features/database-resilience.md` | pending adversarial review |
| 2 | Quadrant: a feature page is "mixed today"; the page is a how-to plus the reference of the streaming API, with the reasoning for per-item `Either` linked to ADR-001 and ADR-006 instead of restated | Trivial | `.claude/skills/encina-docs/SKILL.md` section 1 (Mixed today) and section 3 rule 4; ADR-001, ADR-006 | pending adversarial review |
| 3 | Samples log the error code or exception type, never `EncinaError.Message` | Settled | `AGENTS.md` section 3 (`EncinaError.Message` NEVER reaches logs) | pending adversarial review |
| 4 | No hand-typed figures; benchmark and coverage data are cited by marker or link | Settled | `AGENTS.md` section 9 and SPEC-001 | pending adversarial review |
| 5 | Stream behaviors: state that `src/` ships none and show a user-written behavior; do not repeat the unsupported claim of the original outline | Settled | `.claude/skills/encina-docs/SKILL.md` section 3 rule 3 (every statement names real API) and the code read for this plan | pending adversarial review |
| 6 | Fix the non-compiling README streaming sample in the same pull request | Trivial | One-line edit in the file Phase 3 already edits, same PR; no maintainer choice is involved | pending adversarial review |
| 7 | Compass verdict recorded for the docs-reviewer: quadrant "how-to plus reference of the streaming API" (mixed today), reader a developer returning many items from a handler, outcome a working stream handler and consumer | Trivial | `.claude/skills/encina-docs/SKILL.md` section 1 (same verdict as choice 2) | pending adversarial review |

---

## Implementation Phases

### Phase 1: Page Content — API, Error Handling and Stream Behaviors

<details>
<summary>Tasks</summary>

1. `docs/features/streaming.md` with front matter `title: "Streaming"`, `layout: default`, `parent: "Features"`; opening sentence names the reader (a developer who returns many items from a handler) and what they get.
2. Sections: overview (when to stream instead of returning a list); defining a request and a handler (`IStreamRequest<out TItem>`, `IStreamRequestHandler<in TRequest, TItem>.Handle` with `[EnumeratorCancellation]`); consuming (`Stream` overloads, `Match` per item); error handling and cancellation (Left cases, `StreamCancelled`, the log events 100-105 of `Encina.Stream.cs`, the stream activity and its item-count tag, cited by activity name and tag, not by `EncinaDiagnostics.StartStreamActivity`, which is `internal static` and not API); stream pipeline behaviors (`IStreamPipelineBehavior`, `StreamHandlerCallback<TItem>`, `AddStreamPipelineBehavior` four overloads, `PipelineStage`); performance (backpressure, buffering).
3. A Mermaid diagram of the stream path: caller, `Encina.Stream`, `StreamDispatcher`, stream behaviors, handler, items flowing back.
4. At least two complete samples: define plus consume with `Either` matching, and a user-written stream behavior; both are compiled. The EF Core, Dapper and MongoDB data-source illustrations are fragments marked as such on the page and are not compiled (they would need `Microsoft.EntityFrameworkCore`, `Dapper` and `MongoDB.Driver`, and the README sample uses an undefined `_repository`); `.claude/skills/encina-docs/SKILL.md` section 3 rule 3 applies to the two complete samples.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
Encina is a pre-1.0 .NET 10 library with Railway Oriented Programming. Issue #903 asks for a streaming page. IEncina.Stream returns IAsyncEnumerable<Either<EncinaError, TItem>>. Documentation only; write no production code.

TASK:
1. Read .claude/skills/encina-docs/SKILL.md and diataxis.md; write the compass verdict (quadrant, reader, outcome) before writing.
2. Read src/Encina/Abstractions/IStreamRequest.cs, IStreamRequestHandler.cs, IStreamPipelineBehavior.cs, IEncina.cs (Stream overloads), src/Encina/Core/Encina.Stream.cs, StreamDispatcher.cs, EncinaConfiguration.cs (AddStreamPipelineBehavior) and src/Encina/Pipeline/StreamPipelineBuilder.cs.
3. Write docs/features/streaming.md as described in the plan, with the Mermaid diagram.
4. The two complete samples must compile: copy member names from the sources; run a scratch project (referencing `Encina` only) outside the repository. Mark the EF Core, Dapper and MongoDB illustrations as fragments.

KEY RULES:
- English only; junior-readable; link ADR-001 and ADR-006 instead of restating the reasoning.
- Log the error code or exception type, never EncinaError.Message.
- No hand-typed performance or coverage figures.
- State that src/ ships no built-in stream behavior; do not claim which behaviors apply to streams.
- Pre-1.0: no migration notes, no Obsolete members.

REFERENCE FILES:
- src/Encina/Abstractions/IStreamRequest.cs, IStreamRequestHandler.cs, IStreamPipelineBehavior.cs, IEncina.cs
- src/Encina/Core/Encina.Stream.cs, StreamDispatcher.cs, EncinaConfiguration.cs
- tests/Encina.BenchmarkTests/Encina.Benchmarks/StreamRequestBenchmarks.cs
- docs/features/database-resilience.md (front matter precedent)
```

</details>

### Phase 2: Samples and Verification

<details>
<summary>Tasks</summary>

1. Compile the two complete samples (define-and-consume, user-written behavior) in a scratch project outside the repository that references `Encina` only; the data-source fragments are marked as fragments and not compiled; run the define-and-consume sample against an in-memory handler to confirm the output the page shows.
2. Run `lychee --offline --config .github/lychee.toml 'docs/**/*.md' 'README.md'`, `npx --yes markdownlint-cli2 "docs/**/*.md" "#docs/INVENTORY.md"` and `dotnet run .github/scripts/cov-docs-render.cs -- --check-dangling --docs-root docs --scan-roots src --manifest-dir .github/coverage-manifest --src-root src`; when a tool is missing, check relative links with `Test-Path` and say so.
3. `docs-reviewer` reviews the page; fix every blocker and major finding.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
The page docs/features/streaming.md exists (Phase 1). Verify it; do not add content.

TASK:
1. Build a scratch project outside the repository and compile the two complete C# samples of the page (the data-source fragments are marked and excluded).
2. Run the define-and-consume sample and compare its output with the page.
3. Run the link, lint and citation checks of .claude/skills/encina-docs/SKILL.md section 5.
4. Run docs-reviewer on the page and fix blockers and majors.

KEY RULES:
- Never skip a verification step silently; name any missing tool.
- Keep the scratch project out of the repository.

REFERENCE FILES:
- docs/features/streaming.md
- .claude/skills/encina-docs/SKILL.md (sections 5 and 6)
- .github/lychee.toml, .markdownlint.json
```

</details>

### Phase 3: Navigation, README Cross-Reference and Finalization

<details>
<summary>Tasks</summary>

1. `docs/features/index.md`: add `- [Streaming](streaming.md)` in the Core section.
2. `README.md`: in the Streaming section, link the new page and fix the handler sample so `Handle` returns `IAsyncEnumerable<Either<EncinaError, Product>>` (verify against `IStreamRequestHandler.cs`).
3. `docs/features/pipeline-behaviors.md`: add one cross-link line to the streaming page (the page mentions streams nowhere today).
4. `changelog.d/903-streaming-docs.added.md` (written by `mechanical-fixer`).
5. Check `docs/INVENTORY.md` for a features list and update it only if it lists feature pages.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
docs/features/streaming.md is written and verified. Wire it into the navigation and the README, and add the changelog fragment.

TASK:
1. Add the Streaming entry to the Core section of docs/features/index.md.
2. In README.md, link the page from the Streaming section and correct the handler sample to the real IStreamRequestHandler<,>.Handle signature.
3. Ask mechanical-fixer, naming the worktree path, for changelog.d/903-streaming-docs.added.md.
4. Run changelog-fragments.cs --check and the link and lint checks again.

KEY RULES:
- Never edit CHANGELOG.md.
- The corrected README handler sample must compile; check it in a scratch project.
- No AI attribution in commits.

REFERENCE FILES:
- docs/features/index.md, README.md (Streaming section)
- src/Encina/Abstractions/IStreamRequestHandler.cs
- changelog.d/README.md, .github/scripts/changelog-fragments.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Specification | Relevance |
|--------------------------|-----------|
| ADR-001, ADR-006 (Railway Oriented Programming) | Per-item `Either<EncinaError, TItem>` is the reason streams have an error channel; linked, not restated |
| `AGENTS.md` section 3 | Error messages never reach logs; samples log the error code |
| `AGENTS.md` section 9 and SPEC-001 | No hand-typed figures |
| `.claude/skills/encina-docs/SKILL.md` | Page shape, front matter, verification |
| MediatR `IStreamRequest` (comparable product) | Yields plain items and has no per-item error channel; used for one comparison sentence |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IStreamRequest<out TItem>` | `src/Encina/Abstractions/IStreamRequest.cs` | Marker contract the page defines first |
| `IStreamRequestHandler<in TRequest, TItem>` | `src/Encina/Abstractions/IStreamRequestHandler.cs` | Handler contract and the `[EnumeratorCancellation]` rule |
| `IStreamPipelineBehavior<TRequest, TItem>` and `StreamHandlerCallback<TItem>` | `src/Encina/Abstractions/IStreamPipelineBehavior.cs` | Stream behavior section |
| `IEncina.Stream` (two overloads) | `src/Encina/Abstractions/IEncina.cs` | Consuming section |
| `Encina.Stream` partial and its `Log` events 100-105 | `src/Encina/Core/Encina.Stream.cs` | Error handling and cancellation section |
| `StreamDispatcher` | `src/Encina/Core/StreamDispatcher.cs` | Handler resolution, missing-handler error, item count |
| `StreamPipelineBuilder<,>` | `src/Encina/Pipeline/StreamPipelineBuilder.cs` | Ordering of stream behaviors |
| `EncinaConfiguration.AddStreamPipelineBehavior` | `src/Encina/Core/EncinaConfiguration.cs` | Registration, four overloads |
| `EncinaDiagnostics.StartStreamActivity` (`internal static`, EncinaDiagnostics.cs:67) | `src/Encina/Diagnostics/EncinaDiagnostics.cs` | Source of the stream activity name and item-count tag; the page cites the activity and tags, never the method as API |
| `StreamRequestBenchmarks` | `tests/Encina.BenchmarkTests/Encina.Benchmarks/StreamRequestBenchmarks.cs` | Performance citation (link, no typed figures) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina (core) | 100-105 inside the core range (`src/Encina/Core/Encina.Stream.cs`) | Existing; documented only, no new EventId |

No new range is needed: this plan adds no logging.

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Feature page | 1 | `docs/features/streaming.md` |
| Navigation entry (`docs/features/index.md`) | 1 | Core section |
| README cross-reference and sample fix | 1 | `README.md` |
| Cross-link line (`docs/features/pipeline-behaviors.md`) | 1 | One line |
| Changelog fragment | 1 | `changelog.d/903-streaming-docs.added.md` |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 library (C# 14, nullable enabled) with Railway Oriented Programming (Either<EncinaError, T>). Issue #903 asks for a streaming page. IEncina.Stream returns IAsyncEnumerable<Either<EncinaError, TItem>>; src/ ships no built-in stream pipeline behavior. Documentation only; write no production code. Follow AGENTS.md and .claude/skills/encina-docs/SKILL.md.

IMPLEMENTATION OVERVIEW:
One pull request of 5 files: docs/features/streaming.md (API, error handling and cancellation, stream behaviors, performance, two or more compiling samples, a Mermaid diagram), a line in docs/features/index.md, a cross-link line in docs/features/pipeline-behaviors.md, a README link plus the fix of the non-compiling handler sample, and changelog.d/903-streaming-docs.added.md (mechanical-fixer). Verify with the commands of the encina-docs skill, section 5, and review with docs-reviewer.

KEY PATTERNS:
- just-the-docs front matter (layout: default, parent: "Features").
- Real API only: every member copied from the sources listed below.
- Samples log the error code or exception type, never EncinaError.Message.
- No hand-typed figures; link the benchmark class instead.
- Reasoning about Either is linked to ADR-001 and ADR-006.

REFERENCE FILES:
- src/Encina/Abstractions/IStreamRequest.cs, IStreamRequestHandler.cs, IStreamPipelineBehavior.cs, IEncina.cs
- src/Encina/Core/Encina.Stream.cs, StreamDispatcher.cs, EncinaConfiguration.cs
- src/Encina/Pipeline/StreamPipelineBuilder.cs
- tests/Encina.BenchmarkTests/Encina.Benchmarks/StreamRequestBenchmarks.cs
- docs/features/index.md, README.md
```

</details>

---

## Cross-Cutting Integration Matrix

> Per `AGENTS.md` section 6 (ADR-018): documentation only, so no runtime function is added. A function the page describes is marked ✅ as page content.

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No runtime code; the page states that `src/` applies no caching behavior to streams. |
| 2 | OpenTelemetry | ✅ | The page mentions the stream activity and item count (`StartStreamActivity`) as page content; tracing of items by a user behavior is only an example. |
| 3 | Structured Logging | ✅ | The stream log events (EventIds 100-105 in `Encina.Stream.cs`) are documented as page content. |
| 4 | Health Checks | ❌ | No runtime code. |
| 5 | Validation | ❌ | No runtime code. |
| 6 | Resilience | ❌ | No runtime code. |
| 7 | Distributed Locks | ❌ | No runtime code. |
| 8 | Transactions | ❌ | No runtime code; the page states that no transaction behavior wraps a stream. |
| 9 | Idempotency | ❌ | No runtime code. |
| 10 | Multi-Tenancy | ❌ | No runtime code. |
| 11 | Module Isolation | ❌ | No runtime code. |
| 12 | Audit Trail | ❌ | No runtime code. |

---

## Next Steps

1. Brief a `docs-writer` with Phase 1 to Phase 3 (one pull request, 5 files).
2. Link this plan from #903.
