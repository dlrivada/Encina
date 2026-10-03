<!--
title: [DEBT] Methodology page documents hand-typed overall percentage thresholds for a project-wide number AGENTS.md says does not exist
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

The subsection "Overall project percentage (no explicit target)" of `docs/testing/coverage-measurement-methodology.md` (`:184-192`) documents hand-typed colour thresholds for the overall number (green at 70% or more, amber from 50%, red below 50%, `:188-190`) and describes a weighted headline. That describes a project-wide percentage that AGENTS.md section 9 says does not exist. `docs/en/guides/TESTING.md:60` already calls the overall number "informational only", and `ROADMAP.md:34` says Encina does not track a single project-wide percentage. The page itself admits it is a "historical choice" (`:192`), and repeats this at `:358` ("The overall thresholds 70/50 for green/amber/red are historical, not computed").

## Location

- **File(s)**: `docs/testing/coverage-measurement-methodology.md` (`:184-192`, `:358`), `docs/en/guides/TESTING.md:60`, `ROADMAP.md:34`, `AGENTS.md` section 9
- **Package(s)**: none (documentation, not an Encina package)

## Current Behavior

The methodology page presents fixed percentage thresholds for the overall coverage headline.

## Expected Behavior

If the headline stays, AGENTS.md section 9 and this page label it informational, in the page's section title as well. If the headline is removed together with the badge and the overall report line, the subsection and the limitation at `:358` are deleted.

## Root Cause

The overall headline and its scale predate the per-flag model; the documentation kept describing them as the rules around them changed.

## Proposed Fix

Follow the decision on whether the overall headline is kept or removed, and edit the page, TESTING.md and AGENTS.md section 9 consistently.

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
