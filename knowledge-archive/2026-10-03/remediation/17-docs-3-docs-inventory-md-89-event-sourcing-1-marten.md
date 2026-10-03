<!--
title: [DEBT] docs/INVENTORY.md incorrectly lists EventStoreDB as '(future)' for Event Sourcing
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

The package inventory table in `docs/INVENTORY.md` incorrectly labels EventStoreDB support for Event Sourcing as "(future)". This framing contradicts explicit project decisions recorded in #17 and ADR-027, which rule out EventStoreDB support.

## Location

- **File(s)**: `docs/INVENTORY.md:89`
- **Package(s)**: Encina (Documentation)

## Current Behavior

The line `| **Event Sourcing** (1+) | Marten, EventStoreDB (future) | Aggregate persistence |` implies that EventStoreDB is a planned or upcoming feature. A reader consulting this Reference quadrant table to plan event-sourcing work would incorrectly conclude that EventStoreDB support is coming.

## Expected Behavior

The inventory table should accurately reflect the current and planned state of the repository without listing features that have been explicitly ruled out. EventStoreDB should not be listed as "(future)" because support for it is not planned per ADR-027 and #17.

## Root Cause

The documentation was likely drafted or updated without aligning with the architectural decisions recorded in ADR-027 and the conclusions of issue #17 regarding the scope of event sourcing support.

## Proposed Fix

Update line 89 in `docs/INVENTORY.md` to remove the "(future)" designation for EventStoreDB or remove EventStoreDB from the list entirely, ensuring the inventory aligns with the explicit decisions to rule out EventStoreDB support.

## Priority

- [x] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #17