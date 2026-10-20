## Pages reviewed

- `docs/plans/testing-dogfooding-plan.md` (Historical Note at :7, SonarCloud note at :132-134)
- `docs/releases/pre-v0.10.0/README.md` (:316-317)
- `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md` (:16-22, :49)
- `docs/contributing/README.md` (:193, :198)
- `docs/engineering/crap-gate-design.md` (:32, :36)
- `docs/engineering/REPLICATION-GUIDE.md` (:119)
- `docs/testing/coverage-measurement-methodology.md`, `docs/en/guides/TESTING.md`, `README.md`, `ROADMAP.md`, `CHANGELOG.md`, `changelog.d/` (searched for SonarCloud + ContractTests/PropertyTests exclusion claims; no hit)
- Compared against `.github/workflows/sonarcloud.yml` and `.github/workflows/ci.yml` as they are today.

## Findings

1. **Minor** — `docs/plans/testing-dogfooding-plan.md:132-134`, the "Note on Code Coverage" under the self-referencing Encina.Testing packages table, says these packages "are excluded from SonarCloud and other coverage metrics (see `.github/workflows/sonarcloud.yml`)". Today `sonarcloud.yml` runs no tests and collects no coverage (:20, :60, :170: "Coverage is collected by Codecov ... see ADR-023"). For `src/Encina.Testing*` it only has issue-ignore rules (:74, the `e3` rule for `src/Encina.Testing/**/NeedsMutationCoverageAttribute.cs`; :86 `e9`; :118 `e25`) and a CPD exclusion (:163); a Select-String for `Encina.Testing` over the file returns exactly these four lines. The analysis `sonar.exclusions` (:162) is `**/Migrations/**,**/*.g.cs,**/obj/**,**/bin/**`. So the cited file does not say what the note says, and the "low coverage numbers should not affect quality gates" advice points at a gate that no longer exists in that file. The plan carries a Historical Note at :7 about the January 2026 consolidation, but that note does not cover this sentence. Check that fails: accuracy against today's workflow (SKILL.md §6, wrong facts).

## Informational (not findings)

- No current page claims ContractTests or PropertyTests are excluded from SonarCloud. The only hit is `docs/releases/pre-v0.10.0/README.md:317` ("Temporarily excluded ContractTests/PropertyTests from SonarCloud (Issue #7)"). That page is a dated December 2025 history with a Historical Note at :9, so it is an accurate historical record, not drift. It does not record that #28 re-enabled them; I do not call that a defect for a frozen history page.
- ADR-023 :20-22 describes SonarCloud running unit tests as a problem it replaced; :49 says the SonarCloud workflow "is simplified to build + static analysis only". This matches today's `sonarcloud.yml`. The ADR is consistent with the workflow and with `ci.yml`, which runs `tests/Encina.ContractTests` (`ci.yml:272`) and `tests/Encina.PropertyTests` (`ci.yml:311`) and uploads their coverage (:292, :331). ADR-023:16 contains the literal "13,000+ tests"; an accepted ADR is frozen and the figure is not in this audit's scope.
- Docs claiming the props/.github path behaviour: none claims that props or `.github` changes run the full fast tier. `docs/engineering/crap-gate-design.md:32` says a PR touching `src/` runs all test jobs (matches `ci.yml` filters, each of which lists `src/**`), and :36 says a docs-only or `.github`-only PR skips every test job (matches the filters). `docs/contributing/README.md:198` says "A docs-only PR skips build and tests", which is also correct. The only statement that contradicts the filters is the code comment at `ci.yml:68-70` ("Anything under src/, tests/, .github/ or the build props still runs the full fast tier"), already the code stage's Major; the docs agree with the filters, so they do not need a separate finding. If the code stage's finding is fixed by widening the filters, `crap-gate-design.md:36` becomes stale and needs the same change in the same PR.
- `docs/engineering/REPLICATION-GUIDE.md:119` ("Static analysis only (coverage comes from Codecov)") matches today's workflow.
- `docs/contributing/README.md:193` cites "target 60%" for Codecov patch coverage as a literal; it is not about this issue and I did not verify it.
- `CHANGELOG.md`, `ROADMAP.md` and `changelog.d/` have no entry for #28, #29 or #30 (searched `#28`, `#29`, `#30`, `Issue #7`, ContractTests/PropertyTests next to Sonar); nothing to correct there.
- Search of `artifacts/` was done with Select-String; no docs page lives there, so the count of reviewed pages above excludes this stage's own artifact.
- Not run: lychee, markdownlint and the coverage-citation command, because no page was edited by #28 and no reviewed page was changed in the audit worktree; the finding is about one factual sentence.

## Lessons for the pipeline

- Before listing "the only settings that mention X" in a file, run the search for X over that file and cite every hit it returns; the first draft of docs finding 1 omitted `sonarcloud.yml:74`. (List every hit of the search output when claiming "only these lines mention X"; never from a partial scan.)
