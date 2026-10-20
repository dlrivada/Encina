# Delta scope of issue #28 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published stage files of the original audit (docs/knowledge/audits/28/stages/).

## Knowledge record (docs/knowledge/issues/28.md)

```yaml
schema: 1
nav_exclude: true
issue: 28
title: "[DEBT] Re-enable ContractTests and PropertyTests in SonarCloud workflow"
closed: 2025-12-24
state_reason: completed
outcome: duplicate
duplicate_of: 29
type: debt
area: ci-process
review: verified
packages:
prs:
linked_prs:
knowledge:
  - kind: decision
    statement: "Tests excluded from the SonarCloud workflow because they fail are fixed and re-enabled, not left excluded. The temporary ContractTests/PropertyTests exclusion (commit c4e7f415, #7) was lifted by ccf83b00 (Closes #29, #30). Today sonarcloud.yml runs no tests at all (static analysis on a build only; coverage comes from the CI workflows, ADR-023) and ContractTests and PropertyTests run in ci.yml and ci-full.yml."
    current: yes
    sources:
      - "quote: \"ContractTests and PropertyTests are currently excluded from SonarCloud workflow due to failures. These tests provide important quality coverage and need to be re-enabled.\" (issue #28 body, 2025-12-24)"
    destinations:
      - kind: executable-rule
        status: done
        target: ".github/workflows/ci.yml"
audit:
  checklist: 1
  date: 2026-10-03
  verdict: not-audited
  record: "docs/knowledge/audits/issue-28.md"
remediation:
```

## From the original archivist.md (docs/knowledge/audits/28/stages/archivist.md)

Issue #28 (created 2025-12-24T11:32:38Z, closed 2025-12-24T11:52:34Z, state_reason COMPLETED, comment "Reverted - issue created in error") touched no code itself. Its only commit reference is 2b50a1ec (docs restructure that listed it); `git show --stat` there: `.claude/CLAUDE.md`, `ROADMAP.md`, `docs/history/2025-12.md` only. The code it asked about, mapped to today:
- `.github/workflows/sonarcloud.yml` (exists; the exclusion filter was removed by ccf83b00 on 2025-12-24, "Closes #29, Closes #30"; today the job only builds, lines 166-170: "No test execution needed", coverage is from CI, ADR-023). Checked: `Select-String` for `ContractTests|PropertyTests` in `.github/workflows/sonarcloud.yml` finds nothing.
- `.github/workflows/ci.yml` (lines ~272 and ~311) and `.github/workflows/ci-full.yml` (lines ~212 and ~250) run `tests/Encina.ContractTests` and `tests/Encina.PropertyTests`; `mutation-tests.yml` also references both projects. Pattern `ContractTests|PropertyTests` over `.github/workflows/*.yml`.
- `tests/Encina.ContractTests/`, `tests/Encina.PropertyTests/` (the "failing tests"; the failures were the 57 of #7, fixed there, per commit message of ccf83b00).
- History: exclusion added c4e7f415 (2025-12-22, #7), lifted ccf83b00. No Oracle/SQLite removal involved.

## From the original code.md (docs/knowledge/audits/28/stages/code.md)

- Empty diff confirmed: `git show --stat 2b50a1ec` (the only commit that references #28) lists `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md` only; no `src/`, `tests/` or workflow file. Issue #28 delivered no code, so there is no "PR" to review; the scope is the CI configuration the issue asked about, as the task defined it.
- `.github/workflows/ci.yml` (lines 1-125 change detection and build, 255-333 test-contract and test-property, 493-505 crap-gate, 636-651 ci-result), `.github/workflows/ci-full.yml` (lines 1-30, 197-271), `.github/workflows/sonarcloud.yml`, `.github/workflows/mutation-tests.yml` (references only), `tests/Encina.ContractTests/Encina.ContractTests.csproj`, `tests/Encina.PropertyTests/Encina.PropertyTests.csproj`, `tests/Directory.Build.targets`.
- The successors' code (#29 ContractTests, #30 PropertyTests and the tests inside the two projects) is left to their own audits and to test-auditor.
- No scope correction to the archivist's list; it was accurate.

Checks run in the worktree and their results:
- `Select-String -Path .github\workflows\*.yml -Pattern 'ContractTests|PropertyTests'`: `sonarcloud.yml` has no match; `ci.yml` (lines 55, 58, 272-275, 282-283, 292, 311-314, 321-322, 331), `ci-full.yml` (212-215, 222-223, 232, 250-253, 260-261, 270) and `mutation-tests.yml` (89, 255, 256) match.
- `Select-String -Path .github\workflows\*.yml -Pattern 'continue-on-error|--filter|FullyQualifiedName|Category!=|Category!~|--no-build|\|\| true|exit 0'`: in `ci.yml` and `ci-full.yml` the two test steps (`ci.yml:272`, `:311`; `ci-full.yml:212`, `:250`) carry no `--filter`, no `continue-on-error` and no `|| true`; each is followed by the comment "Test failures MUST fail the CI — no continue-on-error". The `--filter` and `FullyQualifiedName` hits in those files belong to the unit, integration and EF shards, not to these two jobs. `continue-on-error` hits exist only in `benchmarks.yml`, `docs.yml`, `load-tests.yml`, `mutation-tests.yml`, `publish-*.yml` and `testing-dogfooding-validation.yml`, none of which run these two projects as a gate. The old exclusion `FullyQualifiedName!~ContractTests&FullyQualifiedName!~PropertyTests` is not present in any workflow.
- `ci.yml:641` lists `test-contract` and `test-property` in `ci-result.needs`; `ci.yml:649-651` fails on `failure` or `cancelled`.
- `sonarcloud.yml:169-170` states no tests run there; `Select-String` for `dotnet test|--filter` in that file finds nothing (the match list above shows only the comment lines 20, 60, 169, 170).
- `*.cs` under `tests/Encina.ContractTests` and `tests/Encina.PropertyTests`, pattern `Skip\s*=|\[Ignore|Explicit\s*=`: 2 hits (see Informational).
- Neither csproj contains `Compile Remove` or a `Condition` that drops test files.
- Job conditions re-read for finding 1 with `Select-String -Path .github\workflows\ci.yml -Pattern '^  [a-z-]+:$|^    needs:|^    if:'`: `build` `:71`, `test-unit` `:124`, `test-integration` `:185`, `test-contract` `:259`, `test-property` `:298`, `test-guard` `:337`, `test-ef-providers` `:381`, `crap-gate` `:499-504`, `ci-result` `:642`; `coverage-citations`, `changelog-fragments` and `knowledge-records` have no `if`.


