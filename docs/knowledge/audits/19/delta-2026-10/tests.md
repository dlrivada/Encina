## Coverage measured

not measured: delta rule (b). Scope is tooling, with no `src/` file: `.github/scripts/coverage-report.cs`, `.github/workflows/{ci,ci-full,publish-coverage}.yml`, `.github/coverage-manifest/*.json` as data, `docs/coverage/app.js` and the other files listed in `artifacts\knowledge\delta-scope.md`. No `.github/coverage-manifest/{Package}.json` entry exists or can exist for these files (`--check-missing-manifest` walks `src/<Package>/**/*.cs` only, `coverage-report.cs:496-579`), so there are no per-flag targets to compare, no coverable-line counts and no slack to compute, and this stage proposes no per-file target. Rule (b) is therefore "not applicable: no `src/` file in scope"; what the scope does contain (the tooling's own tests and self-tests, and whether CI runs them) was evaluated instead.

Worktree `D:\Proyectos\Encina\.claude\worktrees\wia-19`, branch `audit/19`, HEAD `5ae48853` (contains `0d44ed76`/#1826, verified with `git merge-base --is-ancestor`). The self-tests were run with their literal absolute script paths, `dotnet run --file` default configuration (Debug; no coverage collected, so the build configuration matters only for the exit codes):

| Tool | Test | Run today | CI wiring (`ci.yml`) |
| --- | --- | --- | --- |
| `.github/scripts/coverage-report.cs` | `--self-test` (`RunSelfTest`, `:364-445`) | `Self-test passed (28 checks)`, exit 0 | `coverage-citations` job, step at `:619-622`; the job has no `if` and no path filter (runs on every PR) and is in `ci-result.needs` (`:718`) |
| `.github/scripts/coverage-report.cs` | `--check-justifications` against the real manifests | `All 85 per-file targets are justified.`, exit 0 | same step, `:622` |
| `.github/scripts/generate-coverage-manifest.cs` | `--self-test` (`RunSelfTest`, `:520`) | `Self-test passed (60 checks).`, exit 0 | `coverage-citations` job, `:596-598` |
| `.github/scripts/crap-gate.cs` | `.github/scripts/crap-gate-selftest.ps1` (5 assertions) | `crap-gate self-test PASSED`, exit 0 | `crap-gate` job, `:554-556`; the job runs only when `needs.changes.outputs.src == 'true'` (`:531-536`) |
| `.github/workflows/templates/*.yml` | `tests/Encina.UnitTests/Workflows/WorkflowTemplateTests.cs` | not re-run (the original audit ran 49 passed; unchanged since `65826302`, `git log`) | `test-unit` job, via the `shared` filter (`:91`) |
| `ci.yml`, `ci-full.yml`, `publish-coverage.yml`, `codecov.yml`, `docs/coverage/app.js` | none | not measured: nothing to run | not applicable |

Two probes of `--check-justifications` with a scratch manifest directory outside the repository confirm the gate works today: a per-file target with no justification exits 1 with `target 'unit' has no justification`, and an empty manifest directory exits 1 with `no coverage manifest was loaded`. The finding below is about the absence of a test, not a defect.

## Findings

1. **Minor** — `.github/scripts/coverage-report.cs:364-445` (`RunSelfTest`, 28 checks) exercises only pure functions (`FindJustificationProblems`, `IsBelowTarget`, `FormatMeasured`, `MeasureFileFlag`, `ToSrcRelativePath`, `BuildFileTargetRows`); nothing runs the gate modes whose exit code CI relies on, so a regression that made them always pass would stay green. Untested: the mode dispatch and its `Environment.Exit(1)` (`:167-199`), `RunCheckJustifications` (`:204-229`), `RunCheckStaleManifest` (`:449-488`), `RunCheckMissingManifest` (`:496-579`), the explicit-`--manifest`-does-not-exist exit (`:76-80`) and the empty-manifest "nothing was checked" guard (`:178-182`, the code comment at `:169-177` calls it a "cannot pass vacuously" guarantee). The only references to the three `--check-*` options in the repository are `ci.yml` and the script itself (searched `*.yml`, `*.ps1`, `*.cs`, `*.json` over the worktree, HEAD `5ae48853`), and `ci.yml:611`, `:615`, `:622` run them only against the real tree, which passes today, so the failing branches are never executed anywhere. Missing: a fixture-run self-test (extend `--self-test`, or the fixture run of #1668 under `.github/scripts/testdata/coverage-report/`) that invokes the script on a tiny manifest directory and asserts exit 1 for (a) a per-file target with no justification, (b) a stale key, (c) a `src/` file with no key, (d) an empty `--manifest` directory and (e) a nonexistent `--manifest`, and exit 0 for the consistent case. Deterministic: yes, inputs are temporary files and exit codes, the probe above already shows the two exits are reachable. Overlaps #1668 (fixture-run self-test): fold it there as extra assertions rather than a new test project; the new surface is the `--check-justifications` mode that #1826 added after #1668 was written. Proposed target and justification: none (tooling has no manifest entry, see Coverage measured).

## Informational (not findings)

Status of the original audit's eight test findings, checked against the code today (all but one are still open and tracked; none is re-raised here):

| Original | Tracker | State today |
| --- | --- | --- |
| tests 1: no test for `coverage-report.cs` | #1668 open | Partly superseded. #1826 added `--self-test` (`:364-445`) for the per-file obligation logic and wired it (`ci.yml:619-622`). Still untested: per-package per-flag aggregation, the no-data omission, the Cobertura load and merge (`:665-768`, max hits per line across class entries and reports at `:716-719`), the `ClassifyDirectory` folder names (#1863), the CRAP union and the output shape. The issue body cites pre-#1826 lines (`:28-35`, `:592-600`, `:507-510`, `:928-935`) and says the per-flag target check "does not exist yet"; the report lists per-file targets as report-only (`:953-963`) and nothing enforces package-level targets (#1651). The orchestrator should refresh its line numbers |
| tests 2: `exit 0` when no Cobertura file | #1653 open | Present: `ci.yml:570-573` (`exit 0` at `:572`); `crap-gate.cs` returns 2 for no input and missing files (`:66-67`, `:75`, `:95`); the self-test has no exit-2 assertion |
| tests 3: crap-gate self-test not run on gate-only changes | #1669 open | Present: `ci.yml:531-536` (job condition) and `:41-44` (`src` filter); the issue cites `:499-504` and `:39-42` |
| tests 4: `generate-coverage-manifest.cs --self-test` never in CI | #1670 closed | Resolved: step at `ci.yml:596-598`; self-test passes (60 checks) |
| tests 5: dead HTML dashboard | #1659 open (merged into code 7) | Present: `GenerateHtmlDashboard` at `coverage-report.cs:1261`, written to `index.html` at `:1110-1113` |
| tests 6: `coverage-threshold` input locked in; no test on `ci-result.needs` | #1671 open | Present: `WorkflowTemplateTests.cs:160`; `ci-result.needs` still lists `crap-gate` and `coverage-citations` and no test pins it |
| tests 7: loose self-test regexes, Debug build | #1672 open | Present: `crap-gate-selftest.ps1:48` (`dotnet run --file`, no `-c`), `:69`, `:117`, `:140` |
| tests 8: no text check for a hand-typed 85% target | #1673 open | Not re-checked here (a text-check proposal, no code to measure) |

Open-issue search (bodies of #1847, #1856, #1864, #1866, #1877, #1886, #1887, #1894, #1896, #1897, #1863, #1762 and the #19 remediation issues #1651-#1673): none of the delta umbrellas mentions `crap-gate`, `coverage-report.cs` self-tests or the `--check-*` modes (#1877 cites `coverage-report.cs:849-859` only as evidence for a `GlobalSuppressions.cs` coverable-line count; #1863 is about the test-auditor folder names). No open issue sets or proposes per-file targets for tooling, and none should: tooling is outside `src/<Package>` manifests by design.

Manifest data point (not a #19 scope item, recorded for the orchestrator): 106 package manifests, every one has `targets`; 2,843 file entries, of which 46 carry per-file `targets` (85 targets, all justified, `--check-justifications` exit 0). The remaining entries are the repository-wide obligations backlog that the per-unit delta audits (#1847, #1856, #1864, ...) work through, not part of #19.

The delta scope text says `coverage-report.cs` was "last touched by 365a7ace"; today's last change is `0d44ed76` (#1826, 2026-10-05).

## CRAP

Pending #1346 (not computed; no `src/` method in scope).

## Lessons for the pipeline

- The first attempt to run the self-tests used a PowerShell variable (`$wt\.github\scripts\...`) in the script path and `guard-orchestrator-writes` blocked it ("whose script path the hook cannot resolve"); the same commands with the literal absolute path ran unblocked, as the lesson of audit #19 says. The test-auditor definition's tooling-audit paragraph should say "literal absolute path, no variable" next to the self-test instruction.
- For a delta on an issue whose original audit already opened its remediation issues (#1651-#1673 for #19), the stage's real work is to re-verify each tracked finding against today's code (state, moved line numbers) and to look for surface added since (here `--check-justifications` and the `--self-test` of #1826). A table of "original finding, tracker, state today" in `## Informational` answers that in one place; the delta brief could pass the tracker numbers up front.
- A self-test that calls pure functions proves the logic but not the exit code of the mode CI runs; for a gate script, check that some test executes the failing branch of every gate mode (a fixture run with a bad input), not only the passing run against the real tree.
- Issue bodies drafted before a large change to the same script carry stale line numbers (`#1668` cites `coverage-report.cs:28-35` and `:592-600`, which the 1,528-line script no longer has there); re-print every cited range against the current file before relying on it, and tell the orchestrator which tracked issue needs a refresh.
