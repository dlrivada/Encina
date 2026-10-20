# Delta scope of issue #29 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published stage files of the original audit (docs/knowledge/audits/29/stages/).

## Knowledge record (docs/knowledge/issues/29.md)

```yaml
schema: 1
nav_exclude: true
issue: 29
title: "[INFRA] Re-enable ContractTests in SonarCloud workflow"
closed: 2025-12-24
state_reason: completed
outcome: delivered
type: infra
area: ci-process
review: verified
packages:
prs:
linked_prs:
knowledge:
  - kind: decision
    statement: "ContractTests (and PropertyTests, issue #30) are not excluded from CI test runs; only LoadTests and IntegrationTests stay out of the SonarCloud test filter because they need special infrastructure. Today ContractTests and PropertyTests run as their own jobs in ci.yml and ci-full.yml."
    current: yes
    sources:
      - "quote: \"Tests were previously excluded due to 57 failures (Issue #7). Those tests have since been fixed. Only LoadTests and IntegrationTests remain excluded as they require special infrastructure.\" (commit ccf83b00, closes #29 and #30, 2025-12-24)"
    destinations:
      - kind: executable-rule
        status: done
        target: ".github/workflows/ci.yml"
  - kind: direction-change
    statement: "The SonarCloud workflow no longer runs any tests: sonarcloud.yml is static analysis only and coverage comes from Codecov in the CI workflow, so the exclusion filter this issue edited was later removed with the whole test step."
    current: yes
    sources:
      - "paraphrase: sonarcloud.yml says coverage is handled by Codecov and no test execution is needed; ADR-023 records the move (commit 12e12249 closing #911, ADR-023 \"Accepted (March 2026)\", 2026-03-26)"
    destinations:
      - kind: adr
        status: done
        target: "docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md"
  - kind: gotcha
    statement: "Issue #29 was the re-creation of issue #28 (closed the same day as created in error); #29 and #30 were both closed by the single commit ccf83b00 with no pull request, so the issue-level evidence is one four-line workflow diff and no test changes."
    current: no
    sources:
      - "paraphrase: #28 covered ContractTests and PropertyTests together, #29 and #30 split it, and ccf83b00 closed both (commit ccf83b00, 2025-12-24, #28, #30)"
    destinations:
      - kind: none
        status: done
  - kind: pending-work
    statement: "The CI jobs that now run ContractTests are skipped on props, .github and shared test-config changes and still report green, and the contract theories list SqlServer ADO and Dapper rows twice so xUnit discards 31 cases; both are tracked as open issues."
    current: yes
    sources:
      - "paraphrase: audit of #28 opened #1721 (CI skips test jobs on path filters) and #1725 (duplicate contract theory rows) about the current state (#1721, #1725, 2026-10-03)"
    destinations:
      - kind: backlog
        status: planned
        target: "#1721"
      - kind: backlog
        status: planned
        target: "#1725"
audit:
  checklist: 1
  date: 2026-10-03
  verdict: not-audited
  record: "docs/knowledge/audits/issue-29.md"
remediation:
```

## From the original archivist.md (docs/knowledge/audits/29/stages/archivist.md)

- `.github/workflows/sonarcloud.yml`: the only file touched (commit `ccf83b00`, 2025-12-24, 4 insertions, 4 deletions; no PR). The `dotnetTestArguments` filter it edited no longer exists: commit `10aa68d2` (2025-12-29) moved tests to per-project runs, and today the file has no test step ("Static analysis only", lines 20 and 169-170; coverage from Codecov, ADR-023; the test step was removed by commit `12e12249` of 2026-03-26, #911). Verified with `git log -S'dotnetTestArguments'` and a read of the file.
- Where the behaviour lives today (the code stage's real surface): `.github/workflows/ci.yml` (path filter at lines 55 and 58; "Run Contract tests" step at line 270, "Run Property tests" at line 309), `.github/workflows/ci-full.yml` (ContractTests line 212, PropertyTests line 250), `tests/Encina.ContractTests/`, `tests/Encina.PropertyTests/` and the coverage manifests under `.github/coverage-manifest/`.
- No `src/` scope: the commit changed no source or test code. No code was removed on purpose.

## From the original code.md (docs/knowledge/audits/29/stages/code.md)

- The archivist's scope was the four-line `sonarcloud.yml` change of `ccf83b00`, whose filter no longer exists. Scope corrected, as the archivist proposed, to the CURRENT way contract tests are executed and gated: `.github/workflows/ci.yml` (change detection lines 21-63, `build` 66-119, `test-contract` 256-293, `crap-gate` 493-543, `ci-result` 639-651), `.github/workflows/ci-full.yml` (`test-contract` 197-233, `coverage` 424-528, `pack` 531-578), `.github/workflows/sonarcloud.yml` (read in full: no test step, no ContractTests or PropertyTests reference), `.github/workflows/publish-coverage.yml` (trigger conditions only), `codecov.yml` (flags), `tests/Encina.ContractTests/Encina.ContractTests.csproj`, `tests/xunit.runner.json`, `tests/Directory.Build.targets`, and the last scheduled CI Full run (36374317260) for the job's real output.
- Excluded because #1721 and #1725 already cover them: the path-filter and skipped-job-counts-as-success gap in `ci.yml` and the 31 duplicate contract theory rows. I did not re-derive the per-changed-file-class table; #1721 does it.
- Searches (run in the audit worktree): `ContractTests|Contract` over `.github/workflows/*.yml` (hits only in `ci.yml`, `ci-full.yml`, `mutation-tests.yml:89,256`; none in `sonarcloud.yml` or `templates/`); `--no-build` over `.github/workflows/*.yml` (one hit, `testing-dogfooding-validation.yml:52`, none in `ci.yml` or `ci-full.yml`); `nuget push|dotnet pack` over `.github/workflows/*.yml` (`ci-full.yml:561,575` and `templates/encina-full-ci.yml:625,659`).
- Not reviewed, other stages own them: the contract test sources and their quality (test-auditor), documentation (docs-reviewer).


