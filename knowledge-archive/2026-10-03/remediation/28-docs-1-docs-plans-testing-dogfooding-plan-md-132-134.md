<!--
title: [DEBT] testing-dogfooding-plan.md says the Encina.Testing packages are excluded from SonarCloud coverage, which sonarcloud.yml does not do
labels: technical-debt
milestone: 
kind: docs
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [x] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

The "Note on Code Coverage" in `docs/plans/testing-dogfooding-plan.md:132-134`, under the table of self-referencing `Encina.Testing*` packages, says these packages "are excluded from SonarCloud and other coverage metrics (see `.github/workflows/sonarcloud.yml`)" and that "low coverage numbers for these packages should not affect quality gates". The cited workflow does not say that. `sonarcloud.yml` runs no tests and collects no coverage: the comments at `:20`, `:60` and `:170` state that coverage is handled by Codecov (ADR-023). The only lines of the file that mention `Encina.Testing` are issue-ignore rules (`:74`, the `e3` rule for `src/Encina.Testing/**/NeedsMutationCoverageAttribute.cs`; `:86`, `e9`; `:118`, `e25`) and a duplication exclusion (`sonar.cpd.exclusions`, `:163`). The analysis exclusions (`sonar.exclusions`, `:162`) are `**/Migrations/**,**/*.g.cs,**/obj/**,**/bin/**`.

The exclusion from coverage metrics does exist, but in Codecov: `codecov.yml:53-55` ignores `src/Encina.Testing/**` and `src/Encina.Testing.*/**`.

The plan carries a Historical Note at `:7` about the January 2026 test consolidation, but it does not cover this sentence.

## Location

- **File(s)**: `docs/plans/testing-dogfooding-plan.md:132-134`; cited file `.github/workflows/sonarcloud.yml:20,60,74,86,118,162,163,170`; actual exclusion `codecov.yml:53-55`
- **Package(s)**: None (documentation only)

## Current Behavior

The note sends the reader to `.github/workflows/sonarcloud.yml` for a coverage exclusion that is not in that file, and refers to a SonarCloud quality gate on coverage that the workflow does not feed (it performs static analysis only, `:20`).

## Expected Behavior

The note names the file that holds the exclusion (`codecov.yml:53-55`), says that SonarCloud runs static analysis only and receives no coverage (`sonarcloud.yml:20`), and no longer refers to a SonarCloud coverage gate.

## Root Cause

Not established. No date is verified for when the sentence was written or for when `sonarcloud.yml` stopped collecting coverage.

## Proposed Fix

Rewrite `docs/plans/testing-dogfooding-plan.md:132-134` so it states that `src/Encina.Testing*` is excluded from Codecov coverage (`codecov.yml:53-55`), that SonarCloud does not measure coverage (ADR-023), and that the packages are tested indirectly through the packages that use them. Do not type any coverage figure by hand.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #28 (This issue)
