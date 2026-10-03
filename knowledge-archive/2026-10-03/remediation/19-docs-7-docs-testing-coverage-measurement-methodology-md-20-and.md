<!--
title: [DEBT] Methodology page and ADR-023 hand-type a wrong package count and undated figures, and present 85% as a core-logic bar
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

Reported by: docs 7, docs 8.

`docs/testing/coverage-measurement-methodology.md:20` and `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md:16` state "108 source packages". The checkout has 106 package manifests in `.github/coverage-manifest/` (plus `defaults.json`). A count that no dashboard measures must be a dated sentence with its command, or be left out (documentation rule on hand-typed figures); this one is also wrong.

The ADR hand-types other figures too: "170K+ NCLOC and 13,000+ tests across 7 types" (`:16`), "67.9%" and "2,251 integration tests" (`:20`), and SonarCloud runtimes of "~45 min" and "~15-20 min" (`:22`, `:49`, `:91`).

The methodology page also uses 85% as the realistic bar for core logic and 50% for providers (`:20`, `:23`). The worked example at `:52` (`85 / 100 = **85%**`) is illustrative arithmetic and is fine. The prose at `:20` and `:23` presents 85% for core logic as a fact, which no manifest holds (`.github/coverage-manifest/Encina.json` has unit 70), so it reintroduces the figure that #19 proposed.

## Location

- **File(s)**: `docs/testing/coverage-measurement-methodology.md` (`:20`, `:23`), `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md` (`:16`, `:20`, `:22`, `:49`, `:91`)
- **Package(s)**: none (documentation, not an Encina package)

## Current Behavior

Both pages state a package count that does not match the repository, and several figures that no tool measures. The methodology prose states a core-logic coverage bar that no manifest defines.

## Expected Behavior

The counts are removed, or dated ("measured in March 2026") in the ADR if it keeps them as context at the time of the decision. The methodology prose is rephrased without numbers (for example, core logic can reach a high bar while provider stores cannot from unit tests alone), or cites a covref target.

## Root Cause

Figures were typed by hand when the pages were written and nothing keeps them in step with the repository.

## Proposed Fix

Edit the two pages as described. If ADR-023 is superseded by a new ADR, apply the dated-figure wording in ADR-023 and keep the new text free of figures.

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
