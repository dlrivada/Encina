## Coverage measured
Scope is tooling and documentation under `.github/` and `docs/`; the issue delivered no `src/` code, so no coverage flag applies.
- unit, guard, contract, property, integration: not measured: no src/ scope (`.github/scripts/*.cs` are file-based apps outside every coverage project; the manifest has no entry for them).
- Self-tests and CI wiring were measured instead (build configuration not applicable; scripts run by their literal absolute path):

| Script (lines per `(Get-Content).Count`) | Has `--self-test` | Run in CI | Result |
| --- | --- | --- | --- |
| `.github/scripts/mutation-shard-reports.cs` (209) | yes | `mutation-tests.yml:1274` (aggregate step) | run here: "Self-test passed." |
| `.github/scripts/run-stryker.cs` (171) | no | none | n/a |
| `.github/scripts/update-mutation-summary.cs` (352) | no | none | n/a |
| `.github/scripts/mutation-history.cs` (479) | no | none | n/a |
| `.github/scripts/analyze-mutation-report.cs` (271) | no | none | n/a |
| `.github/scripts/list_survivors.cs` (168) | no | none | n/a |
| `.github/scripts/show-mutant.cs` (202), `show-lines.cs` (34) | no | none | n/a |
| `select-matrix` job of `mutation-tests.yml` (`:43`, checks at `:300-334`) | no | runs only on `workflow_dispatch` and `schedule` (`mutation-tests.yml:3-18`); no pull_request trigger, and `ci.yml` has no mutation job | the twin/length checks are never exercised on a PR |

Search used: `Get-ChildItem .github\scripts` filtered on `mutation|stryker|survivor|show-` plus `Select-String 'self-test'`; Grep of `tests\`, `.github\workflows\` and `.github\scripts\` for each script name and `mutation-score-threshold` found 0 test references.

## Findings
1. **Major** — `.github/workflows/mutation-tests.yml:300-334` (`select-matrix`) is the only guard of the shard partition and has no test: it checks `SHARDS`/`TIMEOUTS` length and span twins, never that the shard globs cover the files of `src/Encina`, and the workflow runs only on `workflow_dispatch` and `schedule`, so nothing exercises it on a PR. This is the missing test counterpart of code finding 1 (220 of 348 candidate files in no shard by the code stage's simulation; the verifier's independent simulation measured 215 of 341, 63 % in both, same folders; a documented "every file lands in some shard" with no check). Needed: a check script with a `--self-test` (a fixture tree with a covered, an uncovered and an explicitly excluded file, plus a span half without twin) run from `ci.yml` on changes to `mutation-tests.yml`, so a new folder or a deleted glob fails a PR instead of a Sunday run.
2. **Minor** — `.github/scripts/list_survivors.cs:85` (`case "TimedOut":`) has no test, so the defect of code finding 3 (Stryker writes `"Timeout"`) survived. None of the report readers (`list_survivors.cs`, `analyze-mutation-report.cs`, `update-mutation-summary.cs`, `mutation-history.cs`) has a `--self-test` or a fixture run, and `MutationCounts` is duplicated in two of them with nothing asserting parity. Needed: one fixture `mutation-report.json` with one mutant per status (`Killed`, `Survived`, `NoCoverage`, `Timeout`, `RuntimeError`, `CompileError`, `Ignored`) and a self-test that feeds it to each reader and asserts the same per-file score as the methodology formula (`docs/testing/mutation-measurement-methodology.md:40-49`); the Timeout case fails today for `list_survivors.cs`.
3. **Minor** — `.github/workflows/templates/encina-full-ci.yml:561-579` (the `mutation-score-threshold` step) was never run against a real Stryker report; a fixture run would have shown at once that `"mutationScore"` is not a key of the report and that the step cannot enforce a real score (code finding 2). The step sets `shell: bash` (`:563`), which GitHub runs with `-eo pipefail`: if a report path matched, the `grep -oP` at `:572` would find no `mutationScore`, return 1 and end the step with exit 1 for every threshold other than 0; but the loop `for report in artifacts/mutation/**/mutation-report.json` (`:568`) has no `globstar`, so `**` acts as `*` and probably never matches Stryker's `reports/mutation-report.json` layout under `--output "artifacts/mutation/$proj_name"` (`:549`), the literal pattern is skipped by `[[ ! -f "$report" ]] && continue`, the loop body never runs and the step passes (the likelier branch; both derived from the script text, not run). Whatever replaces it (deletion of the input, or a per-file report) needs a fixture-run check with a failing input and a control input in the report's own schema, so a future template gate cannot fail open silently.

## Informational (not findings)
- Regression-test check for the code stage's bugs: finding 1 (partition gap) has no test, finding 2 (fail-open template) has no test, finding 3 (`TimedOut`) has no test, finding 4 (dashboard target at `docs/mutations/app.js:38`) and finding 5 (`show-mutant.cs` needs an HTML report that is never produced) have no test; they are in findings 1-3 above or are presentation and dead-code defects without a natural automated test (a `show-mutant.cs` check would be "exits with Report not found when no HTML reporter is configured", better solved by removing the script).
- `src/Encina.Testing/Mutations/NeedsMutationCoverageAttribute.cs` (named by code finding 5) has tests (`tests\Encina.UnitTests\Testing\Mutations\MutationAttributesTests.cs`, `MutationAttributesGuardClauseTests.cs`) but each exists twice, also under `tests\Encina.UnitTests\Testing\Base\Mutations\` (the known folder-doubling of #1693). Its manifest entry (`.github/coverage-manifest/Encina.Testing.json:269-276`) has `defaultRule` `*.cs` and reason "Default: assume testable with mocks"; not in the archivist's scope, so no rule (b) finding, and not measured.
- `run-stryker.cs` has no test; the code stage found no defect in it, and its exit-code branch is unreachable with `break` 0.
- Tests for the per-flag model (`coverage-report.cs --self-test`, `ci.yml:625`) exist and are wired; they were not run here (other audits own them).
- Real infrastructure, regression of the other stages' docs findings: not applicable (no database or Marten feature).
- Duplicate check: open issues #2829, #2897, #2830, #2914 (package-level scope), #1656 and #1660 (coverage dashboard and coverage threshold of the templates), #3323 (methodology wording) were taken from the code stage's search; none holds a test for the shard partition or the report readers (title/body search only, comments not read).

## CRAP
pending #1346

## Lessons for the pipeline
- For a tooling-only scope the useful test-stage output is a table of script, self-test, CI wiring and trigger; here only one of eight mutation scripts has a self-test, and the workflow that hosts the partition check has no PR trigger, so nothing guards it.
- Every code-stage finding of "a helper recomputes a score with another formula" implies a missing fixture test; ask for one fixture report with one mutant per status as the standard regression test.
