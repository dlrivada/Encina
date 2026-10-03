<!--
title: [DEBT] TESTING.md coverage section never links the coverage methodology page and carries a 1,229-character publishing paragraph
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

The "Coverage" section of `docs/en/guides/TESTING.md` (lines 50-82) is a contributor how-to. It links the Coverage Dashboard and `.github/scripts/coverage-report.cs`, but it never links the explanation page `docs/testing/coverage-measurement-methodology.md` or the DocRef convention (`cov:<Package>/<path>.cs`) that page defines at `:251-261`. The "Mutation Testing" section of the same file does link its methodology page (`:90`), so the two neighbouring sections are inconsistent.

Line 66 is a single paragraph of 1,229 characters (measured on 2026-10-03). It describes publishing mechanics: the orphan `dashboard-data` branch, `publish-coverage.yml`, `pages-dashboard-data.ps1` modes (`Persist`, `Read`, `Live`), the `pages` concurrency lock, how the base `history.json` is chosen between the branch copy, the live Pages copy and the committed copy, and the status of the copies under `docs/coverage/data/`. That is explanation content, not something a contributor needs to run the tests or regenerate the report, and the methodology page already describes the same pipeline in "Data flow and the publishing pipeline" (`docs/testing/coverage-measurement-methodology.md:194-200`), "Publish Coverage steps" (`:202-214`) and "The history file" (`:233-249`).

## Location

- **File(s)**: `docs/en/guides/TESTING.md` (`:50-82`, the paragraph at `:66`, the mutation link at `:90`); `docs/testing/coverage-measurement-methodology.md` (`:194-200`, `:233-249`, `:251-261`)
- **Package(s)**: none (documentation, not an Encina package)

## Current Behavior

- The "Coverage" section has no link to the coverage methodology page or to the DocRef convention.
- Line 66 holds 1,229 characters of branch names, workflow and script modes and Pages concurrency in one paragraph, duplicating what the methodology page already says.

## Expected Behavior

- The "Coverage" section links `docs/testing/coverage-measurement-methodology.md` for the obligations model, the data flow and the DocRef convention, the way the "Mutation Testing" section links its methodology page.
- Line 66 is reduced to one sentence: CI does not commit coverage data to `main`; the dashboard data is published by the Publish Coverage workflow and served live on Pages, with a link to the methodology page's "Data flow and the publishing pipeline" section for the details.
- Nothing is lost: every fact removed from line 66 is already stated in the methodology page, or is added to its "Data flow" section when it is not.

## Root Cause

The publishing paragraph grew inside the how-to each time the pipeline changed (the `dashboard-data` branch, the Pages deployer), instead of being written once in the methodology page and linked from the guide.

## Proposed Fix

1. In `docs/en/guides/TESTING.md`, add a sentence to the "Automated (CI)" subsection linking `../../testing/coverage-measurement-methodology.md` (the path form used at `:90`), mentioning the obligations model and the DocRef convention.
2. Compare line 66 with `docs/testing/coverage-measurement-methodology.md:194-214` and `:233-249`; add to the methodology page any fact of line 66 it does not already state.
3. Replace line 66 with one sentence plus a link to the "Data flow and the publishing pipeline" section.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #19 (This issue)
