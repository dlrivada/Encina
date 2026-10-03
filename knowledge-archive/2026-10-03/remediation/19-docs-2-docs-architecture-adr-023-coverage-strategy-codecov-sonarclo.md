<!--
title: [DEBT] ADR-023 records category coverage targets and Codecov Components enforcement that the repository does not use; supersede it and delete coverage-weights.json
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

Reported by: code 8, docs 2, docs 9.

ADR-023 and a legacy data file still describe the single-threshold, category-target coverage model that AGENTS.md section 9 replaced with per-flag manifest targets.

**ADR-023** (`docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md`):

- "Module Categories and Targets" (`:66-83`) is a table of 14 category targets (Core Logic 85%, Security 80%, ..., Event Sourcing 50%).
- `:35-37`, `:90` and `:100` say per-module thresholds are enforced by Codecov Components and that "the real coverage enforcement is done by Codecov status checks".
- The code says otherwise. `codecov.yml:26-27` says "Components require Codecov Pro plan (not available on free tier)" and defines no component. `codecov.yml:15-24` has no per-category target. `.github/coverage-manifest/Encina.json` carries per-flag `targets` (unit 70, guard 20, contract 15), not category targets.
- The ADR has `Status: Accepted (March 2026)` (`:12`), with no "superseded" status or addendum, and its own table stays in the 85%-for-core model that #19 asked for.
- It is internally inconsistent: Decision 3 (`:43-45`) says to "Create a custom Quality Gate ... without coverage", while the Consequences (`:100`) say the free plan does not allow custom Quality Gates.
- The title still says "Codecov Components" (`:2`, `:8`).

**Methodology page** (`docs/testing/coverage-measurement-methodology.md`): `:25` repeats the claim ("ADR-023 documents what we use (Codecov Components + Flags)"), and `:151` documents `coverage-weights.json` and its 12 legacy categories ("up to 85% for Full") as "kept in the repo as historical documentation". AGENTS.md section 3 forbids keeping legacy aids for history.

**Legacy data file** (`.github/scripts/coverage-weights.json`): it defines 12 categories (Full, Logic, Provider, Transport, Cloud, CDC, Validation, DistributedLock, Caching, TestingLibrary, Tooling, Excluded) with targets up to 85.0 ("Full" is 85.0). No script, workflow or test references it: a repository search finds only the methodology page (`:151`) and the file itself, whose `$schema` points to a `coverage-weights-schema.json` that does not exist.

## Location

- **File(s)**: `docs/architecture/adr/023-coverage-strategy-codecov-sonarcloud.md` (`:2`, `:8`, `:12`, `:35-37`, `:43-45`, `:66-83`, `:90`, `:100`), `docs/testing/coverage-measurement-methodology.md` (`:4`, `:25`, `:151`), `.github/scripts/coverage-weights.json`, `codecov.yml` (`:15-27`), `.github/coverage-manifest/Encina.json`
- **Package(s)**: none (ADR, documentation and repository data file, not an Encina package)

## Current Behavior

An accepted ADR documents a category-target model and Codecov Components enforcement that do not exist, and the methodology page links to it as the statement of what the project uses. An unreferenced data file keeps the same category targets alive.

## Expected Behavior

- A new, short ADR records the per-flag manifest model as the decision (targets in `.github/coverage-manifest/{Package}.json`, the obligations model, Codecov flags and the SonarCloud static-analysis-only role). ADR-023's Status shows that it is superseded by the new ADR, or carries an addendum pointing to the manifests. ADR-023's history is not rewritten silently; the ADR rule is to supersede.
- The category-target table is removed from the current decision text, and the Decision 1 wording, the "Codecov Components" title and the Decision 3 / Consequences contradiction are fixed.
- `docs/testing/coverage-measurement-methodology.md` `:25` (and the ADR line at `:4`) describe what is used today, and the paragraph at `:151` with the legacy category list is removed.
- `.github/scripts/coverage-weights.json` is deleted.

## Root Cause

The ADR was written for the category model of March 2026 and was never superseded when targets moved to per-flag manifests. The legacy file was left in place and then documented as historical, which kept the old figures reachable.

## Proposed Fix

1. Add the superseding ADR (and link it from `docs/architecture/adr/index.md`), then mark ADR-023 as superseded.
2. Update the methodology page at `:4`, `:25` and delete the paragraph at `:151`.
3. Delete `.github/scripts/coverage-weights.json` in the same change, after confirming again that nothing references it.

## Priority

- [x] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #19 (This issue)
