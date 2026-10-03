<!--
title: [DEBT] Feature request template incorrectly lists EventStoreDB as an active provider
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

The feature request issue template at `.github/ISSUE_TEMPLATE/feature_request.md:73` instructs contributors to check off "Event sourcing features: Marten, EventStoreDB" when proposing provider-coherence work. This contradicts ADR-027 and the decision in issue #17, which deprecates EventStoreDB and establishes Marten as the sole provider. The template will mislead contributors into requesting EventStoreDB implementations for a deprecated package.

## Location

- **File(s)**: `.github/ISSUE_TEMPLATE/feature_request.md:73`
- **Package(s)**: N/A (Infrastructure/Documentation)

## Current Behavior

The template lists both Marten and EventStoreDB as valid event sourcing providers to consider in feature proposals.

## Expected Behavior

The template should list only Marten as the event sourcing provider, reflecting the deprecation of EventStoreDB per ADR-027 and issue #17.

## Root Cause

The template was not updated following the architectural decision to deprecate EventStoreDB. It falls into a "code-adjacent file" category (issue templates) that was not covered by the initial documentation sweeps targeting `AGENTS.md` and `docs/`.

## Proposed Fix

Update `.github/ISSUE_TEMPLATE/feature_request.md:73` to remove "EventStoreDB" from the list, leaving only "Marten". Verify no other templates or checklists reference EventStoreDB as an active provider.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues
- #1347 - possibly related (the local model proposed it as a duplicate; the evidence check rejected it)

- #17