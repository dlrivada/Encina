<!--
title: [DEBT] Coverage methodology page has no front matter and mixes explanation, reference and how-to under one title
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

`docs/testing/coverage-measurement-methodology.md:1-12` has no front matter (no `title`, `layout` or `parent`), while ADR-023 has. The page's opening says who it is for, but it spans three Diataxis quadrants under one title: explanation of the obligations model, reference for the JSON schema and the covref fields, and a how-to for running a recalculation.

This is not a blocker for a living document. The compass should be applied per section when the page is next touched: the schema and covref sections are reference, and "Recalculation" is how-to.

## Location

- **File(s)**: `docs/testing/coverage-measurement-methodology.md` (`:1-12`)
- **Package(s)**: none (documentation, not an Encina package)

## Current Behavior

The page starts at the H1 with no front matter, and its sections serve three different needs without being separated.

## Expected Behavior

The page has front matter consistent with its neighbours, and each section is clearly an explanation, a reference or a how-to, either by splitting the page or by marking the sections.

## Root Cause

The page grew as a single living document without a documentation-type review.

## Proposed Fix

When the page is next edited for the coverage-model changes, add the front matter and apply the compass per section (the schema and covref sections as reference, "Recalculation" as how-to).

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
