<!--
title: [DEBT] Public coverage dashboard hard-codes a project-wide "target 85%" line on the trend chart
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

Reported by: code 4, docs 4.

The public coverage dashboard draws a project-wide target that no rule defines. `renderTrendChart` in `docs/coverage/app.js` draws a dashed line at 85 and the label `'target 85%'` (`:817-831`), for the combined series and for every per-flag series. No manifest, methodology page or AGENTS.md defines an overall target: the methodology says the overall headline "has no manifest target" (`docs/testing/coverage-measurement-methodology.md:184-192`), and AGENTS.md section 9 says there is no project-wide percentage and no hand-typed target figures.

This is the single-percentage model of the rejected #19 rendered on the page that the project cites (methodology, ROADMAP.md:34 and AGENTS.md section 9) as the result of the per-flag model.

## Location

- **File(s)**: `docs/coverage/app.js:817-831`
- **Package(s)**: none (dashboard front end, not an Encina package)

## Current Behavior

When the chart's value range includes 85, the trend chart shows a dashed "target 85%" line, which suggests a target that the repository does not have.

## Expected Behavior

The trend chart shows no target line for the overall number, because by the repository's own rules the overall number has no target.

## Root Cause

The line was written for the earlier single-threshold model and was not removed when targets moved to the per-flag manifests.

## Proposed Fix

Delete the `// Target line at 85%` block (`app.js:817-831`). A deterministic text assertion that `docs/coverage/app.js` contains no `85` target can pin the removal.

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
