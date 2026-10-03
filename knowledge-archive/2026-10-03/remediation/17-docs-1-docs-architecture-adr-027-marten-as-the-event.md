<!--
title: [DEBT] ADR-027 incorrectly states Encina.EventStoreDB remains in the repository when it has been deleted
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

The ADR-027 document states that `Encina.EventStoreDB` remains in the repository as deprecated code, but the actual filesystem and git history indicate that the project has been completely deleted and moved to a gitignored backup directory that is not present in the current checkout.

## Location

- **File(s)**: `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md`
- **Package(s)**: Encina.EventStoreDB

## Current Behavior

The Consequences section of ADR-027 (line 30) claims: "`Encina.EventStoreDB` remains in the repository as deprecated code until a removal decision; it is not part of SPEC-000's 1.0 list." However, commit `87d92a394cccbcb4445e2ac349123f290548742d` deleted every file under `src/Encina.EventStoreDB/` from git tracking. The commit message indicates the files were "moved" to `.backup/`, but this directory is gitignored and absent from the current checkout (verified via `Test-Path .backup` → `False` and `Test-Path src\Encina.EventStoreDB` → `False`).

## Expected Behavior

The ADR-027 documentation should accurately reflect the current state of the repository by acknowledging that `Encina.EventStoreDB` has been removed from the main codebase and is no longer available in the working directory, aligning the documentation with the actual filesystem and git history.

## Root Cause

The ADR text was likely written or not updated after the deletion commit `87d92a394cccbcb4445e2ac349123f290548742d` was merged. The decision to move the code to a gitignored backup directory resulted in a discrepancy between the documented state ("remains in the repository") and the actual state (deleted from tracking, backup absent).

## Proposed Fix

Update line 30 of `docs/architecture/adr/027-marten-as-the-event-sourcing-provider.md` to accurately describe the status of `Encina.EventStoreDB`. The text should clarify that the code has been removed from the repository and is not part of the active codebase, removing the implication that it "remains in the repository."

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