<!--
title: [DEBT] ROADMAP.md footnote claims non-existent .backup/deprecated-packages/ location
labels: technical-debt
milestone: 
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

The footnote at `ROADMAP.md:805` under the deprecation table states that "Deprecated code preserved in `.backup/deprecated-packages/`." This claim is factually incorrect because the `.backup/` directory does not exist in the repository checkout.

## Location

- **File(s)**: `ROADMAP.md:805`
- **Package(s)**: Encina.EventStoreDB

## Current Behavior

The documentation at `ROADMAP.md:805` indicates that deprecated code is preserved in the `.backup/deprecated-packages/` directory. However, `.backup/` does not exist in the current checkout (`Test-Path .backup` returns `False`), and `.gitignore:16` excludes it.

## Expected Behavior

The footnote at `ROADMAP.md:805` should accurately reflect where deprecated code is actually located, or be removed if the code is no longer preserved in a dedicated directory.

## Root Cause

The documentation was not updated to match the actual repository state regarding the location of deprecated packages, leading to a false factual claim in the ROADMAP.

## Proposed Fix

Update `ROADMAP.md:805` to correct the location of deprecated code or remove the reference if the `.backup/deprecated-packages/` directory is not intended to exist.

## Priority

- [x] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

#17