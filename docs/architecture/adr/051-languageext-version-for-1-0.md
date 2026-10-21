# ADR-051: Which LanguageExt Version Encina 1.0 Ships On

## Status

**Proposed - awaiting the maintainer's decision (#1899).** No option below is chosen. The spike that produced the evidence is [#1899](https://github.com/dlrivada/Encina/issues/1899); its measurements are in the [spike report](../../engineering/languageext-v5-spike-1899.md).

## Context

[ADR-001](001-railway-oriented-programming.md) makes LanguageExt's `Either<EncinaError, T>` the return type of every Encina operation, and rejects a custom `Result<T,E>` as "reinventing the wheel". [ADR-004](004-reject-mediator-result.md) keeps `Either` from LanguageExt directly and already names "LanguageExt makes breaking changes to Either" as a known risk. [ADR-006](006-pure-rop-exception-handling.md) constrains how errors become `Either` and does not discuss the library. Encina is pre-1.0, so a breaking change is acceptable now and costs users after 1.0 ([SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) defines the 1.0 boundary).

The spike measured these facts on 2026-10-21 (details and commands in the [spike report](../../engineering/languageext-v5-spike-1899.md)):

| Topic | Fact |
| --- | --- |
| v5 status | Latest is `5.0.0-beta-78` (2026-10-09), after a gap of about 9.3 months since beta-77. No release candidate or stable 5.0 exists, and no date was found. The v5 notes are marked "WORK IN PROGRESS" with an open TODO list |
| v4 status | `4.4.9` (2024-06-26), listed and not deprecated; the `v4-latest` branch has no commits since 2025-12-30, so v4 is frozen |
| Maintenance | Bus factor 1: of 100 commits between 2025-06-30 and 2026-10-21, 98 are the maintainer's (a lower bound, the API caps the page) |
| Adoption | 4.4.9 has 10,029,610 downloads; beta-77 has 49,239 (about 200 to 1) |
| Framework | v5 targets `net10.0` only with no dependencies; v4 targets `.NETStandard2.0` with 10 dependencies. Neither is annotated for trimming or AOT |
| Encina surface | Narrow: `Either<EncinaError,T>`, `Unit`, `Option<T>`. `Either<` appears 5138 times in 811 files; 830 of 2951 files have `using LanguageExt` |
| Public API | 1040 `PublicAPI` lines (6.7% of 15,480) in 49 packages mention `LanguageExt.`; there is no adapter layer |
| Pilot cost, core only | `src/Encina` reached 0 errors after +168 / -167 lines in 33 files and +181 / -181 `PublicAPI` lines; 515 distinct errors on the first build. The full solution build then reported 435 distinct errors in 10 more projects, a lower bound |
| Extrapolation | About 1,650 source lines, about 1,020 `PublicAPI` lines, about 160 judgement edits and about 140 test files (an estimate scaled from the core; method in the report) |
| Behaviour | `Either` becomes an abstract class: `default` is null, a positional `Match` swaps to (Left, Right) order and compiles silently when both lambdas have the same type. The benchmark loop that bypasses Encina rose from 400 B to 2.7 KB; Encina paths were about flat in allocations |
| Idiomatic v4 | 117 `LeftToArray()` / `RightToArray()` calls in 17 files, 8 `.Case` uses and 8 `Match(async ...)` sites use members that v5 removes or obsoletes; cleaning them up is possible on 4.4.9 |

## Decision

**No decision is taken in this ADR yet.** The maintainer chooses one of the four options below; this section is then replaced by the chosen option and the Status changes to Accepted. The options are listed in a fixed order that does not rank them.

### Option A: migrate to v5 beta now, before 1.0

Encina moves to `LanguageExt.Core` `5.0.0-beta-78` (or the newest beta) and ships 1.0 on it.

- **What it means:** 1.0 depends on a pre-release package of a project with a bus factor of 1, and `Either` becomes a reference type across the whole public API. Encina follows later betas until a stable 5.0 appears.
- **Measured cost:** the pilot numbers above, for the core only; the extrapolation (about 1,650 source lines, about 1,020 `PublicAPI` lines, about 160 judgement edits, about 140 test files) covers 30 packages that each need at least one extra build cycle.
- **Risks:** the v5 removal list may change before stable; beta-to-beta changes were not diffed; no stable date exists; the library's trim and AOT safety is unverified; the silent hazards (null `default`, positional `Match`) need a review beyond what the compiler reports.
- **Better when:** the maintainer values being on the line the library's author develops, accepts shipping 1.0 on a beta, and prefers to pay the break before users exist rather than after.

### Option B: stay on 4.4.9 for 1.0 and use it idiomatically

Encina keeps `LanguageExt.Core` `4.4.9`, ships 1.0 on it and cleans up the non-idiomatic patterns.

- **What it means:** a stable package that is frozen (no fixes) and not deprecated. A later move to v5 is a post-1.0 breaking change for users, because `Either` is in 1040 `PublicAPI` lines.
- **Measured cost:** the idiomatic cleanup only (117 `LeftToArray` / `RightToArray` calls, 8 `.Case`, 8 `Match(async ...)` sites and related patterns); no migration cost is paid now.
- **Risks:** v4 gets no fixes and the author's work is on v5; the cost of the v5 move is deferred to users (the pilot's break categories, `PublicAPI` churn and the null `default` change all reach them); a .NET 10 or C# 14 problem in v4 would have no upstream fix.
- **Better when:** the maintainer prioritises a stable dependency for 1.0 and accepts that moving to v5 later is a breaking change, or expects no v5 move at all.

### Option C: isolate LanguageExt behind Encina-owned result types

Encina defines its own result and option types and keeps LanguageExt (any version) as an internal detail or drops it.

- **What it means:** the public API stops exposing `LanguageExt.*` types, so a LanguageExt version change no longer reaches users. It contradicts the reasoning of [ADR-001](001-railway-oriented-programming.md) ("reinventing the wheel") and the choice recorded in [ADR-004](004-reject-mediator-result.md).
- **Measured cost:** not piloted. It is larger than Option A: the same call sites change (5138 `Either<` occurrences in 811 files and 1040 `PublicAPI` lines), plus the design and tests of the new types and the combinators Encina uses.
- **Risks:** a second `Either`-like type to maintain, test and document; users lose interoperability with LanguageExt code; ADR-001 and ADR-004 need amendments.
- **Better when:** the maintainer wants Encina's public contract independent of any third-party library's release cycle and accepts owning the types.

### Option D: gate on v5 stable

Prepare on 4.4.9 now, and migrate before 1.0 only if a stable 5.0 ships by a date the maintainer sets; otherwise ship 1.0 on 4.4.9.

- **What it means:** Encina first removes the patterns that v5 removes or obsoletes while staying on 4.4.9 (`LeftToArray`, `Case`, `MatchAsync`, `Match(async ...)` and the hand-rolled async combinators), so a later bump is mostly mechanical. The migration is then a conditional second step with a deadline.
- **Measured cost:** the Option B cleanup now, plus the Option A migration cost only if the condition is met. The pilot's mechanical part (factories, `PublicAPI`) is unchanged by the cleanup.
- **Risks:** the deadline can pass without a stable release (no date exists, and beta-78 followed beta-77 after about 9.3 months), which lands on Option B; the cleanup is work that pays off only if the move happens or if the patterns are worth fixing anyway.
- **Better when:** the maintainer wants to keep the choice of v5 open without shipping 1.0 on a beta, and can name a date after which 1.0 ships regardless.

## Alternatives considered

- **Replace LanguageExt with another library** (`OneOf`, `ErrorOr`, `FluentResults`, `CSharpFunctionalExtensions`, `Dunet` and similar): not evaluated by the spike. [ADR-001](001-railway-oriented-programming.md) rejected `OneOf` for lacking functional combinators; the others were not researched, and any of them would be a change of the same size as Option C.
- **Wait for v5 stable and decide then, with no preparation:** equivalent to Option B until the date arrives, without the cleanup that Option D includes.

## Consequences

- Until the maintainer decides, nothing changes: Encina stays on `LanguageExt.Core` 4.4.9 and no migration work starts.
- Whichever option is chosen, the idiomatic cleanup findings of the spike report (section 6) are valid on 4.4.9; the order in which they are scheduled depends on the option.
- Any option other than B moves 1.0 or the post-1.0 public contract; the PublicAPI files and the `docs/` pages that show `Either` signatures change with it.
- This ADR becomes Accepted when the chosen option replaces the Decision section, and its consequences are then rewritten for that option.

## Related

- [ADR-001](001-railway-oriented-programming.md) (Railway Oriented Programming; adopts LanguageExt `Either`)
- [ADR-004](004-reject-mediator-result.md) (rejects a Mediator-style result; names LanguageExt `Either` breaking changes as a risk)
- [ADR-006](006-pure-rop-exception-handling.md) (pure ROP exception handling)
- [SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) (the 1.0 boundary)
- [Spike report](../../engineering/languageext-v5-spike-1899.md)
- Issue [#1899](https://github.com/dlrivada/Encina/issues/1899)

## Date

2026-10-21
