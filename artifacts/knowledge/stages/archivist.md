## Scope
- `.github/workflows/sonarcloud.yml`: the only file touched (commit `ccf83b00`, 2025-12-24, 4 insertions, 4 deletions; no PR). The `dotnetTestArguments` filter it edited no longer exists: commit `10aa68d2` (2025-12-29) moved tests to per-project runs, and today the file has no test step ("Static analysis only", lines 20 and 169-170; coverage from Codecov, ADR-023, #911 closed). Verified with `git log -S'dotnetTestArguments'` and a read of the file.
- Where the behaviour lives today (the code stage's real surface): `.github/workflows/ci.yml` (path filter at lines 55 and 58; "Run Contract tests" step at line 270, "Run Property tests" at line 309), `.github/workflows/ci-full.yml` (ContractTests line 212, PropertyTests line 250), `tests/Encina.ContractTests/`, `tests/Encina.PropertyTests/` and the coverage manifests under `.github/coverage-manifest/`.
- No `src/` scope: the commit changed no source or test code. No code was removed on purpose.

## Destinations
- Decision "ContractTests/PropertyTests are not excluded from CI": present in `ci.yml` and `ci-full.yml` (grep above). Caveat: #1721 (OPEN) says the jobs are skipped on props/.github/shared test-config changes while still reporting green.
- Decision "SonarCloud runs no tests, coverage from Codecov": present in ADR-023 (`docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md`) and the comment in `sonarcloud.yml`.
- Pre-draft candidate "regression-test: verify the workflow no longer excludes ContractTests": dropped, no source states it and the filter itself is gone. Pre-draft candidate "docs: CI coverage": ADR-023 already carries it.
- Pre-draft rules ("workflows must not permanently exclude suites without a tracked blocking issue", "commit messages should list suites re-enabled") dropped: the issue text states only the impact, not a rule.

## Successor and duplicate issues
- #28 `[DEBT] Re-enable ContractTests and PropertyTests in SonarCloud workflow`: CLOSED (completed, 2025-12-24T11:52:34Z), closed in error; #29 and #30 are its split re-creation. #29 itself is not a duplicate: it was delivered by `ccf83b00`.
- #30 `[INFRA] Re-enable PropertyTests in SonarCloud workflow`: CLOSED (completed, 2025-12-24T17:42:48Z), same commit.
- #7 `[DEBT] Fix 57 failing tests across multiple packages`: CLOSED (completed, 2025-12-23), the blocker.
- #911 `[INFRA] Migrate coverage measurement from SonarCloud to Codecov`: CLOSED.
- #1721 `[BUG] CI skips every test job on props, .github and shared test-config changes and still reports green`: OPEN, work pending.
- #1725 `[TEST] Contract theories list SqlServer ADO and Dapper rows twice, so xUnit silently discards 31 cases`: OPEN, work pending.
- Pre-draft open questions: PropertyTests were re-enabled by the same commit (diff verified; the issue title only names ContractTests, #30 covers the rest). Whether the #7 failures were truly fixed or forced to pass is not evidenced by this commit (no test change); the code/test stages should judge it (#1725 shows ContractTests today silently drop 31 cases).

## Lessons for the pipeline
- The pre-draft listed `outcome: delivered`, `closed_at` as a local-format date and `packages: []`; it also invented rules with no source. Records here need ISO dates and sourced rules only.
- Commit-closes-two-issues (`Closes #29, Closes #30`) means one diff serves two audits; scope for both is the same four-line change, so the code stage should audit the current CI jobs, not the deleted filter.
