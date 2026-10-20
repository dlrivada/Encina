## Scope
Issue #28 (created 2025-12-24T11:32:38Z, closed 2025-12-24T11:52:34Z, state_reason COMPLETED, comment "Reverted - issue created in error") touched no code itself. Its only commit reference is 2b50a1ec (docs restructure that listed it); `git show --stat` there: `.claude/CLAUDE.md`, `ROADMAP.md`, `docs/history/2025-12.md` only. The code it asked about, mapped to today:
- `.github/workflows/sonarcloud.yml` (exists; the exclusion filter was removed by ccf83b00 on 2025-12-24, "Closes #29, Closes #30"; today the job only builds, lines 166-170: "No test execution needed", coverage is from CI, ADR-023). Checked: `Select-String` for `ContractTests|PropertyTests` in `.github/workflows/sonarcloud.yml` finds nothing.
- `.github/workflows/ci.yml` (lines ~272 and ~311) and `.github/workflows/ci-full.yml` (lines ~212 and ~250) run `tests/Encina.ContractTests` and `tests/Encina.PropertyTests`; `mutation-tests.yml` also references both projects. Pattern `ContractTests|PropertyTests` over `.github/workflows/*.yml`.
- `tests/Encina.ContractTests/`, `tests/Encina.PropertyTests/` (the "failing tests"; the failures were the 57 of #7, fixed there, per commit message of ccf83b00).
- History: exclusion added c4e7f415 (2025-12-22, #7), lifted ccf83b00. No Oracle/SQLite removal involved.

## Destinations
- Decision "excluded failing tests are fixed and re-enabled, not left excluded": destination executable-rule `.github/workflows/ci.yml`, present (runs both test projects). Nothing else was decided by #28; no ADR/AGENTS.md rule is needed.
- The pre-draft rule "Issues should be validated before creation" was dropped: it is the pre-draft's inference, not a statement in the issue or comment (the comment only says "Reverted - issue created in error").

## Successor and duplicate issues
Same-scope search (`gh issue list --state all --search "SonarCloud ContractTests PropertyTests"` and `"ContractTests PropertyTests in:title,body"`), created soon after, verified today:
- #29 "[INFRA] Re-enable ContractTests in SonarCloud workflow": CLOSED, COMPLETED, created 2025-12-24T13:21:28Z, closed 17:42:47Z. Recorded as `duplicate_of: 29`.
- #30 "[INFRA] Re-enable PropertyTests in SonarCloud workflow": CLOSED, COMPLETED, created 13:21:42Z, closed 17:42:48Z (the other half of #28's scope; `duplicate_of` holds a single number).
- #31 "[INFRA] Verify all CI workflows run green": CLOSED, COMPLETED; its comment says ContractTests and PropertyTests were re-enabled and pass.
- #7 "[DEBT] Fix 57 failing tests across multiple packages": CLOSED, COMPLETED 2025-12-23 (the root cause #28's body refers to).
The pre-draft outcome `rejected-unexplained` was wrong: #28 was split into #29/#30, not abandoned. Duration created to closed: 19 min 56 s.

## Lessons for the pipeline
- An issue closed "created in error" two hours before its same-scope re-creation is a duplicate even when the re-creation splits it in two (#29 + #30); the commit that finally delivered (ccf83b00) names the successors in "Closes", so `git log --grep` on the file's history finds them quickly. Third audit in a row (#25, #26, #28) where the pre-draft said rejected-unexplained.
- The pre-draft's "Candidate destination" and "Rules and lessons" invented a rule ("validate issues before creation") that no source states; drop pre-draft rules with no quotable source.
