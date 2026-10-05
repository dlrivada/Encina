## Pages reviewed

Delta `rules-2026-10`, rule (a) only. Scope: the pages that describe what #5 delivered (skip of the Stream load tests, the `DOTNET_JitObjectStackAllocationConditionalEscape=0` workaround).

Search: `JitObjectStackAllocation|StreamRequestLoadTests|CLR crash|NBomber.*(crash|JIT)|load tests? (are )?excluded|0x80131506` over `docs`, `README.md`, `AGENTS.md`, `CLAUDE.md`, `tests` (`*.md`), run from relative paths in the worktree; `artifacts/` excluded.

| Page | Hits | Classification |
| --- | --- | --- |
| docs/testing/load-tests-known-issues.md | 5 | in scope, reviewed (findings 1-3) |
| AGENTS.md | 1 (line 119) | in scope, one-line pointer; accurate (CI templates skip `*LoadTests*`) |
| docs/testing/load-test-baselines.md, docs/en/guides/TESTING.md | 0 hits for the regex | neighbours; neither links the known-issues page (finding 3) |
| docs/engineering/ENGINEERING-HANDBOOK.md (1388, 1391), docs/engineering/PROJECT-HISTORY.md (588, 634, 654, 740) | 6 | frozen snapshot / history; skip |
| docs/releases/pre-v0.10.0/README.md (680-686), docs/releases/v0.11.0/CHANGELOG-DETAILS.md (1016-1021) | 2 | release history; skip |
| docs/plans/testing-dogfooding-plan.md (141, 150, 2202), docs/plans/crypto-shredding-nested-implementation-plan-1698.md (908) | 4 | plans; mention the workaround only, skip |
| docs/knowledge/audits/issue-5.md, docs/knowledge/issues/5.md | 7 | knowledge record of this audit; skip |
| docs/contributing/README.md (31) | 1 | MSBuild CLR crash (#496), not #5; skip |

Code checked: `tests/Encina.LoadTests` has no `.cs` file with `using NBomber`; NBomber is referenced only by `tests/Encina.NBomber/Encina.NBomber.csproj:9` (23 files with `using NBomber`, all in that project). No directory named `Encina.FluentValidation.LoadTests` or `Encina.GuardClauses.LoadTests` exists in the repository.

## Findings

1. **Minor** — `docs/testing/load-tests-known-issues.md`, heading "Affected Load Test Projects" (lines 46-47), point 2 (C# and command samples correct against the repository). It names `Encina.FluentValidation.LoadTests` and `Encina.GuardClauses.LoadTests`; neither project exists (search of every directory name returned nothing; load tests are consolidated in `tests/Encina.LoadTests`). The page's own `dotnet test --filter "FullyQualifiedName~LoadTests"` samples (lines 20, 26, 32) still run, but the project list the reader is told to check is stale.
2. **Major** — `docs/testing/load-tests-known-issues.md`, header "Affected Projects: All `*.LoadTests` projects using NBomber" (line 6) and "Problem" (line 11), point 2 (samples and claims correct against the repository). The page says the workaround applies to the load test projects that use NBomber, and its only test commands target `LoadTests`; today `tests/Encina.LoadTests` does not use NBomber (no `using NBomber` in any file) and NBomber lives only in `tests/Encina.NBomber`. A reader running the documented `FullyQualifiedName~LoadTests` filter does not exercise the code the page describes, and the Stream case that originated the issue (`StreamDispatcher` with `IAsyncEnumerable`) is not named. The page needs the real project and the real trigger.
3. **Minor** — `docs/testing/load-tests-known-issues.md`, whole page, point 4 (placement). The page has no front matter (first line is `# Load Tests Known Issues`, no `parent`, `nav_order` or `title`), and no page links to it: a search of `docs/**/*.md`, `docs/*.md` and `README.md` for `load-tests-known-issues` returns no link, and `docs/testing/load-test-baselines.md` and `docs/en/guides/TESTING.md` (which describe running load tests, `TESTING.md:43-44`) do not mention the JIT workaround. The workaround is reachable only from `AGENTS.md:119` text without a link.
4. **Minor** — `docs/testing/load-tests-known-issues.md`, whole page, point 1 (visual and scannable). The page is short and uses code blocks, but the CI exclusion that it relies on and the three-way platform choice (PowerShell, CMD, bash) could be one table of shell, command and scope; the "Resolution Timeline" is a numbered list of conditions with no status or date, so the reader cannot see whether any condition has been met. Low impact.

## Informational (not findings)

- Point 3 (figures): the page has no coverage, mutation or performance figure; nothing to cite.
- Point 5 (feature adequacy): #5 delivered a test skip, not a feature; no concept, guide or tutorial entry is expected.
- The missing Stream load-test justification and the unverified dotnet/runtime citations are already tracked by the original audit (`docs/knowledge/audits/issue-5.md`, AUD-05, AUD-01, AUD-11); not repeated here.
- The `mkdocs`/Jekyll build was not run; placement was judged from front matter and links only.

## Lessons for the pipeline

- A known-issues page that names projects goes stale when test projects are consolidated; check every named project against `tests/` directory names, and check the technology claim (here NBomber) against the project's own files, not only its name.
