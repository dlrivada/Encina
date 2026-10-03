<!--
title: [DEBT] Coverage methodology, TESTING.md and AGENTS.md disagree on whether per-flag targets are enforced
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

Three documents describe the status of per-flag coverage targets differently:

- `docs/testing/coverage-measurement-methodology.md:149` says the manifest `targets` "are **not** used as the overall percentage threshold. They are per-flag floors used by the dashboard to color individual cells". Line `:192` says "The real enforcement happens at the per-package level where manifests carry explicit numbers".
- `docs/en/guides/TESTING.md:54` says "a package is green only when every applicable flag reaches its own target", and `:62` calls the dashboard "the only gate".
- AGENTS.md section 9 states a MUST: each flag reaches its own target, and nothing is pushed or merged until it does.

No job compares a flag with its target, so the pages claim enforcement that does not exist and colour is the only effect.

The colour rule at `coverage-measurement-methodology.md:178-182` also makes amber (80-100% of the target) pass without comment, which sits badly with "MUST independently reach"; the page should say that amber means the target is unmet.

## Location

- **File(s)**: `docs/testing/coverage-measurement-methodology.md` (`:149`, `:178-182`, `:192`), `docs/en/guides/TESTING.md` (`:54`, `:62`), `AGENTS.md` section 9
- **Package(s)**: none (documentation, not an Encina package)

## Current Behavior

The methodology page says colour only, with "real enforcement" at package level; TESTING.md says the green rule is the gate; AGENTS.md says MUST. A reader cannot tell what is enforced.

## Expected Behavior

All three documents say the same thing. Either the per-flag target gate exists and the pages describe it, or the pages state "advisory; shown on the dashboard, not gated". Amber is described as unmet.

## Root Cause

The per-flag model was documented before (and without) the comparison step that would enforce it, and each document was written separately.

## Proposed Fix

Decide together with the per-flag gate decision: if the gate lands, describe it in the methodology page and TESTING.md; otherwise reword the three places to "advisory". Add the amber-means-unmet wording at `:178-182` in both cases.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #19 (This issue)
