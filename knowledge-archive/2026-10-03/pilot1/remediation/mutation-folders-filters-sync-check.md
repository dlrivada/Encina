<!-- issue
title: [TEST] Assert mutation-tests.yml FOLDERS and FILTERS arrays stay in sync
labels: area-testing, area-mutation-testing, area-ci-cd
milestone:
-->

## Test Category

- [x] Coverage Gap (below 85% target)

## Description

`.github/workflows/mutation-tests.yml`'s `select-matrix` job pairs a `FOLDERS` array (one entry per `src/Encina/` subfolder) with a parallel `FILTERS` array (the Stryker `test-case-filter` override for that folder). CLAUDE.md's own Mutation Testing System section warns: "When adding or renaming a folder in `FOLDERS`, update the parallel `FILTERS` array in the same step (an empty entry means no override)" — a manual-discipline rule with no automated check found for it during the knowledge-migration pilot's review of issue #1027 (which introduced this mechanism).

If the two arrays silently drift out of sync (a folder added without a matching filter entry, or vice versa), a shard would either apply the wrong filter to the wrong folder or lose its override silently, and nothing in CI would flag it — the workflow's own `continue-on-error: true` on the matrix job means a shard misconfiguration would look like "0 mutants killed" rather than a hard failure.

## Packages / Providers Affected

- **Package(s)**: n/a (CI workflow, not a shipped package)
- **Provider(s)**: n/a

## Current Coverage

Not applicable — this is a workflow-configuration invariant, not application code coverage.

## Infrastructure Required

- [x] None (pure unit tests) — a `dotnet run` C# script or a lightweight PowerShell Pester test can parse the YAML and assert `FOLDERS.Length == FILTERS.Length`

## Test Plan

### Tests to Implement

- [ ] A script or test (PowerShell, run in CI before `select-matrix`, or a `.github/scripts/*.cs` validation script) that parses `.github/workflows/mutation-tests.yml`'s `FOLDERS` and `FILTERS` array literals and fails if their lengths differ.
- [ ] Optionally, assert every `FOLDERS` entry corresponds to an actual existing `src/Encina/<folder>` directory, catching stale entries after a folder rename.

### Success Criteria

- [ ] The check runs as part of `select-matrix` (or a dedicated lint step) and fails the workflow with a clear message if the arrays diverge.
- [ ] No flaky tests introduced.
- [ ] Tests run within acceptable time limits (this is a parse-and-compare check, sub-second).

## Collection Fixture (Integration Tests Only)

- **Collection**: n/a — not a database integration test
- **Fixture**: n/a

## Related Issues

- #1027 — Pass per-folder test-case-filter to Stryker to bypass xUnit v3 perTest bug (introduced the FOLDERS/FILTERS mechanism this check protects)
- #1028 — Run mutation tests in parallel matrix (all 17 folders per weekly run) (consumes the same arrays)
- Pilot source record: `artifacts/pilot/records/1027.md` (knowledge-migration pilot, 2026-09-24)
