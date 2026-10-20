## Pages reviewed

Delta rules-2026-10, rule (a) only. Scope: pages that describe what #1 delivered (stream handlers and stream pipeline behaviors discovered by the `AddEncina` assembly scan; `src/Encina/Dispatchers/MediatorAssemblyScanner.cs:72-81`, `src/Encina/Core/ServiceCollectionExtensions.cs:82-83`). The original audit published no docs stage, so the page set was found by search.

Search (run from relative paths inside the worktree, case-insensitive, `.md` under `README.md`, `docs`, `src`, excluding `docs/knowledge`):
`IStreamRequestHandler|IStreamPipelineBehavior|StreamRequest|stream handler|stream pipeline|streaming`

| Page | Hits | Classification |
|---|---|---|
| `README.md` | 5 (lines 51, 52, 357, 361, 363 plus sample lines) | in scope: `## Streaming` (357-383); line 52 is CDC "change streaming", skip |
| `docs/INVENTORY.md` | 40 | one in scope (:328 lists `IStreamPipelineBehavior<TRequest, TItem>`, name exists in `src/Encina/Abstractions/IStreamPipelineBehavior.cs:57`); rest are event streaming/CDC/assertions, skip; the page is a Spanish inventory, not touched by this issue |
| `docs/comparacion-nestjs.md` | 4 | skip: Spanish comparison page, hand-typed "70%" completeness (:49, :120, :2384) predates rule; not about #1 delivery, noted below |
| `docs/architecture/extensibility-analysis.md`, `docs/engineering/*`, `docs/releases/*`, `docs/plans/*`, ADRs, `docs/features/cdc*.md`, `docs/benchmarks/*`, `docs/messaging/*`, `docs/testing/load-test-baselines.md`, `src/Encina.Cdc/README.md` | 1-23 each | skip, except `docs/releases/pre-v0.10.0/README.md:140-142,312` (release history: names the stream interfaces and their assembly-scan registration, not a concept, guide, reference or tutorial page; cited in finding 2); the rest use "streaming" in the CDC/event-streaming/history sense and none describes stream-handler registration |
| `docs/guides/index.md`, `docs/tutorials/index.md`, `docs/index.md`, `docs/introduction.md`, `docs/architecture/request-pipeline.md`, `docs/guides/how-to-write-a-pipeline-behavior.md` | 0 | checked for rule (a) point 5: no streaming mention |

`src/Encina/README.md` does not exist (Test-Path false).

## Findings

1. **Major** — `README.md:365` (`## Streaming`, lines 357-383): the `StreamProductsHandler.Handle` sample is declared `public async IAsyncEnumerable<Product> Handle(...)`, but `IStreamRequestHandler<in TRequest, TItem>.Handle` returns `IAsyncEnumerable<Either<EncinaError, TItem>>` (`src/Encina/Abstractions/IStreamRequestHandler.cs:91`; the interface's own XML example at :33 uses `IAsyncEnumerable<Either<EncinaError, Product>>`). The sample does not compile against today's API (rule (a) point 2: C# sample correct against `src/`), and it is inconsistent with the consumer half of the same block (:377-381), which already calls `result.Match(Left:, Right:)` on an `Either`. The handler body also yields raw `Product` (:371) where `yield return Right(product)` is required. The other API names in the block exist: `IStreamRequest<TItem>` (`src/Encina/Abstractions/IStreamRequest.cs:35`), `IEncina.Stream<TItem>(IStreamRequest<TItem>, CancellationToken)` (`src/Encina/Abstractions/IEncina.cs:126`).
2. **Minor** — `README.md` `## Streaming` (357-383), rule (a) point 5: the section never says that `AddEncina(typeof(Program).Assembly)` (README.md:102) discovers `IStreamRequestHandler<,>` and `IStreamPipelineBehavior<,>` implementations, which is the behavior #1 delivered. No concept, guide or reference page for streaming exists under `docs/` (search above: the release-history notes `docs/releases/pre-v0.10.0/README.md:140-142` and `:312` mention `IStreamRequest<TItem>`, `IStreamRequestHandler<,>`, `IStreamPipelineBehavior<,>` and their assembly-scan registration, and the README and the Spanish `INVENTORY.md` name the interfaces, but no concept, guide, reference or tutorial page explains them), and `docs/guides/index.md` and `docs/tutorials/index.md` have 0 streaming hits, so the feature is in no tutorial or learning path. Severity is minor because #1 was a bug fix to DI wiring, not a new feature; the missing streaming page is a broader gap.
3. **Minor** — `README.md` `## Streaming`, rule (a) point 1: one code block with no diagram or table showing how a stream request flows through `IStreamPipelineBehavior` to the handler. The README has 4 Mermaid blocks elsewhere (count by search), so the style is available; not a blocker.

## Informational (not findings)

- Point 3 (figures): `README.md` `## Streaming` has no coverage, mutation or performance literal. `docs/comparacion-nestjs.md:49,120,2384` carries a hand-typed "70%" for stream requests; it is an old Spanish comparison page outside this issue's pages and is left to a page-level docs audit.
- Point 4 (placement): README placement is correct for an overview snippet; no new page was delivered, so nothing to place.
- Lint and link tools were not run: delta mode checks only rule (a).
- Not checked: `docs/INVENTORY.md:328` currency (Spanish inventory, not scope).

## Lessons for the pipeline

- A feature-adjacent bug-fix audit still has one page worth reading: the README sample for the feature. Checking its handler signature against the interface found a return-type error that symbol-existence checks pass (all names exist, the return type is wrong).
