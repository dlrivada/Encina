<!--
title: [DEBT] A project-wide coverage percentage is still produced and headlined, against AGENTS.md section 9
labels: technical-debt
milestone: 
kind: debt
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [x] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [ ] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

AGENTS.md section 9 says "There is no project-wide percentage", yet the coverage tooling still produces and headlines one:

- `.github/scripts/coverage-report.cs:690-692` prints "ENCINA COVERAGE (obligations model): x%".
- `:706` writes "## Overall: x%" into the markdown report.
- `:808-823` builds a "weighted coverage" badge (JSON and SVG) from the overall number, using a fixed colour scale of 90/80/70/60/50.
- `.github/workflows/ci-full.yml:454-471` adds a reportgenerator "Coverage Summary" (one overall number) to the job summary.

The methodology page calls this a "historical choice" and a visual cue (`docs/testing/coverage-measurement-methodology.md:184-192`, `:358`). The overall also divides met obligations by obligations that exist only for flags that have data (`coverage-report.cs:592-602`), so a flag that produced no data raises the overall instead of lowering it.

## Location

- **File(s)**: `.github/scripts/coverage-report.cs` (`:690-692`, `:706`, `:808-823`), `.github/workflows/ci-full.yml` (`:454-471`), `docs/testing/coverage-measurement-methodology.md` (`:184-192`, `:358`), `AGENTS.md` section 9
- **Package(s)**: none (repository tooling, not an Encina package)

## Current Behavior

The report, the badge and the CI job summary present a single overall percentage as the headline of the coverage state, with its own colour thresholds.

## Expected Behavior

Either the overall number is explicitly demoted ("informational headline, never a target") in AGENTS.md section 9 and in the methodology page, or the badge and headline are removed.

## Root Cause

The overall number predates the per-flag model and was kept as a convenient visual cue; the rule in AGENTS.md was written without removing it.

## Proposed Fix

Decide between the two options above and apply it consistently to `coverage-report.cs`, the CI job summary step, the methodology page and AGENTS.md. If the headline is removed, drop the badge outputs together with the `badge.json` and `badge.svg` copy in `.github/workflows/publish-coverage.yml:110-111`.

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
