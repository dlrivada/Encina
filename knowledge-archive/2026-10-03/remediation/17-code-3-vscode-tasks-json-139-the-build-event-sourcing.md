<!--
title: [DEBT] Remove stale "EventStoreDB" from "build: event-sourcing" task detail
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

The `"detail"` field for the `"build: event-sourcing"` task in `.vscode/tasks.json` still advertises "EventStoreDB, Marten". Since EventStoreDB was deprecated, this label is misleading. Additionally, the task is broken because it targets `Encina.EventSourcing.slnf`, which does not exist in the repository, but the stale label is the specific finding addressed here.

## Location

- **File(s)**: `.vscode/tasks.json:139`
- **Package(s)**: N/A

## Current Behavior

The `"build: event-sourcing"` task's `"detail"` field reads `"EventStoreDB, Marten"`, incorrectly implying that EventStoreDB is a valid provider being built. The task itself fails because it targets a non-existent solution filter file (`Encina.EventSourcing.slnf`).

## Expected Behavior

The `"detail"` field should accurately reflect the current state of the codebase, removing any reference to deprecated providers like EventStoreDB. Ideally, the broken task target should also be corrected, but that is a separate pre-existing issue.

## Root Cause

The task definition in `.vscode/tasks.json` was not updated when EventStoreDB was deprecated and when the solution filter files were removed or renamed from the repository.

## Proposed Fix

Update the `"detail"` field in `.vscode/tasks.json` for the `"build: event-sourcing"` task to remove "EventStoreDB". Note that the underlying task failure due to the missing `.slnf` file is a pre-existing, repo-wide issue and out of scope for this specific label correction.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

#17