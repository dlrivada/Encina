<!--
title: [DEBT] Issue and PR templates still carry the rejected 85% project-wide coverage target
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

Reported by: code 2, docs 6.

The issue and PR templates still carry the old single 85% coverage target. They are the tool that tracks per-package gaps (the decision of #19 was that gaps are tracked as `[TEST]` issues per package). AGENTS.md section 9 says there is no project-wide percentage, each flag has its own target in `.github/coverage-manifest/{Package}.json`, and documentation never types coverage figures by hand.

The stale text is in:

- `.github/ISSUE_TEMPLATE/test_implementation.md:18` ("Coverage Gap (below 85% target)"), `:35` (the example table row with 85% in the Target cell) and `:56` ("Coverage meets ≥85% target")
- `.github/ISSUE_TEMPLATE/epic.md:65` ("Tests passing (coverage ≥85%)")
- `.github/ISSUE_TEMPLATE/feature_request.md:182` ("Code coverage >= 85%")
- `.github/pull_request_template.md:20` ("Coverage does not decrease below threshold", a threshold that no longer exists)

Open #1627 shows the symptom: it leaves the 85% box unticked and writes the package-wide target in the table by hand.

## Location

- **File(s)**: `.github/ISSUE_TEMPLATE/test_implementation.md`, `.github/ISSUE_TEMPLATE/epic.md`, `.github/ISSUE_TEMPLATE/feature_request.md`, `.github/pull_request_template.md`, `.claude/hooks/tests/Test-Hooks.ps1:2484`
- **Package(s)**: none (repository templates, not an Encina package)

## Current Behavior

A contributor filling the `[TEST]` template is told to measure a coverage gap against 85% and to type a target in the table. The epic and feature templates ask for coverage of at least 85%, and the PR template asks that coverage not drop below a threshold that does not exist.

`.claude/hooks/tests/Test-Hooks.ps1:2484` embeds the template's example table row (the one with the 62.3%, 85% and -22.7% cells) as one of the placeholder fixtures, so the hook test depends on the template text.

## Expected Behavior

The templates name the flag and read the target from the manifest instead of stating a number. The `[TEST]` gap table has a Flag column and a Target column to be filled from `.github/coverage-manifest/{Package}.json`, and the checkboxes in the epic, feature and PR templates refer to per-flag targets and the blocking CRAP gate rather than a percentage. The hook test fixture is updated together with the template.

## Root Cause

A rejected rule survived in the templates when the per-flag model replaced the single threshold; nothing checks the templates for hand-typed coverage figures.

## Proposed Fix

1. Rewrite the three `test_implementation.md` lines, the `epic.md` and `feature_request.md` lines and the `pull_request_template.md` line so they refer to the per-flag target in the manifest.
2. Change the example row and the matching placeholder string in `.claude/hooks/tests/Test-Hooks.ps1:2484` in the same change.
3. Optionally add one deterministic text check that no hand-typed 85% coverage target remains in `.github/ISSUE_TEMPLATE`, `.opencode`, or `docs/coverage/app.js`.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #19 (This issue)
- #1627 - leaves the 85% box unticked and writes the package-wide target in the table by hand
