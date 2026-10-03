<!--
title: [DEBT] coverage-report.cs generates a dead HTML dashboard that throws on load and is never published
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

Reported by: code 7, tests 5.

`.github/scripts/coverage-report.cs` carries a generated HTML dashboard that cannot work and is not published.

- `GenerateHtmlDashboard` (`:979-1173`) renders from `categories`, `p.category` and `p.target`, none of which is defined (the category configuration was removed, see the comment at `:37`). `renderCategories()` (`:1087-1095`, called at `:1168`) therefore throws a `ReferenceError` on load, so the package table and the chart never render. The package JSON the page embeds (`:987-996`) has `perFlagTarget` but no `target`.
- The script writes the result to `index.html` (`:827-831`). The file is uploaded in the `coverage-report` artifact (`.github/workflows/ci-full.yml:510-516`), but `.github/workflows/publish-coverage.yml:109-120` copies only the summary JSON, the badges and the DocRef index. The live dashboard is `docs/coverage/app.js`.
- Nothing renders or parses the generated HTML, so no test could have caught the breakage. A headless JavaScript parse would need a node dependency the repository does not use and is not deterministic enough to justify; deleting the generator is better than testing it.

AGENTS.md section 3 forbids legacy code, and the script prints a misleading file.

## Location

- **File(s)**: `.github/scripts/coverage-report.cs` (`:37`, `:827-831`, `:979-1173`), `.github/workflows/ci-full.yml:510-516`, `.github/workflows/publish-coverage.yml:109-120`
- **Package(s)**: none (repository tooling, not an Encina package)

## Current Behavior

Every run of `coverage-report.cs` writes an `index.html` that throws a `ReferenceError` when opened and is uploaded with the coverage artifact.

## Expected Behavior

The script writes no HTML dashboard. A fixture-run test asserts that the output directory contains `encina-coverage-summary.json`, `encina-coverage-report.md`, `docref-index.json`, `badge.json` and `badge.svg` and no `index.html`.

## Root Cause

The categories were removed from the report generator without removing the HTML generator that still depends on them, and the file was never published, so nobody opened it.

## Proposed Fix

1. Delete `GenerateHtmlDashboard` and its call (`coverage-report.cs:827-831`), and the `index.html` console line.
2. Add the output-listing assertion to the fixture-run test for `coverage-report.cs` (no JavaScript runtime needed).
3. Optionally pin the hard-coded `'target 85%'` line in `docs/coverage/app.js` (`:817-831`) with a deterministic text assertion ("no 85 target in `docs/coverage/app.js`"), because the live dashboard has no test either.

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
