<!--
title: [DEBT] Open coverage issues #901, #910 and #521 still state the old global 85% target
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

Three open issues still state the old global coverage target and would drive work toward a number no manifest holds:

- #901 "Increase coverage for 13 low-coverage modules (1-59%) to ≥85%". Its body says "well below the project target of ≥85%" and has a per-module "Cov%" table with no flag or manifest target; it is dated 2026-03-25.
- #910 "Increase coverage for 8 mejorable modules (60-79%) to reach 85%".
- #521 "[DEBT] Web packages coverage below 85% target".

AGENTS.md section 9 says there is no project-wide percentage and that each flag reaches its own target from `.github/coverage-manifest/{Package}.json`.

## Location

- **File(s)**: GitHub issues #901, #910 and #521 (no repository file)
- **Package(s)**: the packages those three issues list; no additional package is named here

## Current Behavior

The three issues set 85% as the goal for modules and packages, with tables that carry no flag and no manifest target.

## Expected Behavior

Each remaining gap is tracked against the manifest flag targets of its own package, the way #1627 does it, and the three issues no longer state a global 85% target.

## Root Cause

The issues were written under the single global target of the rejected #19 and were not revisited when the per-flag manifest model replaced it.

## Proposed Fix

Rewrite each issue per package against the manifest flag targets, or close it in favour of per-package `[TEST]` issues. #1389 already covers the guard-flag family, so that part needs no new issue.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #19 (This issue)
- #901 - states the old global target
- #910 - states the old global target
- #521 - states the old global target
- #1627 - the per-package rewrite to follow
- #1389 - already covers the guard-flag family
