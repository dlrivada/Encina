<!--
title: [DEBT] CONTRIBUTING.md states a 90% project-wide coverage gate that does not exist
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

`CONTRIBUTING.md:98` ("Coverage does not drop below the CI threshold (90% lines)") and `CONTRIBUTING.md:110` (the `ci.yml` row: "tests with coverage, 90% gate") state a project-wide 90% line-coverage gate. No such gate exists:

- `.github/workflows/ci.yml` has no 90 coverage threshold; its only numeric gate is `crap-gate.cs ... --threshold 10` (`:543`).
- `codecov.yml:15-24` sets the project `target: auto` and the patch target to 60%.
- AGENTS.md section 9 says there is no project-wide percentage and each flag has its own manifest target.

This is a hand-typed figure that is false, on the page every contributor reads before a PR. It is a third value (90) beside the rejected 85 of #19 and the 80 in other files. `CONTRIBUTING.md` currently links nothing under `docs/testing/`.

## Location

- **File(s)**: `CONTRIBUTING.md:98`, `CONTRIBUTING.md:110`
- **Package(s)**: none (contributor documentation, not an Encina package)

## Current Behavior

`CONTRIBUTING.md` tells contributors that CI enforces a 90% line-coverage threshold.

## Expected Behavior

`CONTRIBUTING.md` points to what is actually enforced: the per-flag targets in `.github/coverage-manifest/{Package}.json`, the blocking `crap-gate` job and the methodology page `docs/testing/coverage-measurement-methodology.md`. It types no coverage percentage.

## Root Cause

The figure was written for an earlier project-wide threshold model and was not updated when the per-flag manifest model replaced it.

## Proposed Fix

Replace both lines with a pointer to the per-flag targets, the blocking `crap-gate` job and the methodology page, and add the missing link into `docs/testing/`. If a per-flag target gate is added later, describe it here in the same change.

## Priority

- [x] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #19 (This issue)
