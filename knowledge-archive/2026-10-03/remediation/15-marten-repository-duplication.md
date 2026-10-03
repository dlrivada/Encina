<!-- issue
title: [DEBT] MartenAggregateRepository and SnapshotAwareAggregateRepository duplicate concurrency/collision classification and save/create orchestration
labels: technical-debt, area-event-sourcing
milestone: v0.14.0 — Hardening
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [x] Refactoring needed
- [ ] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

Found by the SPEC-003 audit of #15. `MartenAggregateRepository` and `SnapshotAwareAggregateRepository` (both implement `IAggregateRepository<TAggregate>`, the only event-sourcing provider per ADR-027) duplicate concurrency-exception classification, stream-collision classification, and most of the `SaveAsync`/`CreateAsync` orchestration bodies. This is exactly the kind of duplicated orchestration logic issue #15 originally raised — issue #15 was closed as superseded because EventStoreDB and Marten needed a Strategy pattern rather than a simple Orchestrator, but that closure never addressed the duplication *within* Marten's own two repository implementations, which is a separate, smaller-scoped concern that still exists today.

## Location

- **File(s)**:
  - `src/Encina.Marten/MartenAggregateRepository.cs`
  - `src/Encina.Marten/Snapshots/SnapshotAwareAggregateRepository.cs`
- **Package(s)**: Encina.Marten

## Current Behavior

- `IsConcurrencyException` is byte-for-byte duplicated: `MartenAggregateRepository.cs:361-368` vs `SnapshotAwareAggregateRepository.cs:475-481`.
- `IsStreamCollisionException` is byte-for-byte duplicated: `MartenAggregateRepository.cs:373-379` vs `SnapshotAwareAggregateRepository.cs:486-492`.
- The concurrency-conflict `Left` construction (expected-version calculation, `conflictDetails` dictionary, error message) is duplicated: `MartenAggregateRepository.cs:230-254` vs `SnapshotAwareAggregateRepository.cs:200-224`.
- `SaveAsync` and `CreateAsync` have near-identical bodies (enrich → append/start stream → `SaveChangesAsync` → clear uncommitted events → log → project) in both files; the snapshot-aware version only adds one `TryCreateSnapshotAsync` call.
- `SnapshotAwareAggregateRepository.LoadWithSnapshotAsync` (lines 336-345) re-implements the "stream does not belong to aggregate" check a third time, instead of reusing the existing private helper `StreamDoesNotBelongToAggregate` already present in `MartenAggregateRepository.cs:348-356`.
- `IsConcurrencyException` relies on fragile string-matching against Marten's internal exception type names (the code comment itself says "Marten v8 uses different exception types"); nothing enforces that both copies stay in sync if a future Marten upgrade renames or restructures those types.

## Expected Behavior

Concurrency classification, collision classification, the conflict-details builder, and the "stream does not belong to aggregate" check live in one shared internal type used by both repositories — following the precedent already set by `InlineProjectionRelay`, which both repositories already share for projection dispatch.

## Root Cause

`SnapshotAwareAggregateRepository` was added by copying `MartenAggregateRepository` and layering snapshot support on top, without first extracting the concurrency/collision/conflict-details logic the two share. No shared base class, helper type, or contract test enforces that the two repositories classify exceptions identically going forward.

## Proposed Fix

1. Extract an internal shared type (e.g., `MartenSaveOperationSupport`) holding `IsConcurrencyException`, `IsStreamCollisionException`, the conflict-details builder, and `StreamDoesNotBelongToAggregate`.
2. Update both `MartenAggregateRepository` and `SnapshotAwareAggregateRepository` to use it, removing the three duplicated blocks and the third re-implementation of the stream-ownership check.
3. Add or extend a contract test (`tests/Encina.ContractTests/Marten/Core/`) asserting both repositories classify the same set of representative Marten exceptions identically, so a future edit to one copy without the other fails CI.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

Found by the SPEC-003 audit of #15. Related: ADR-027 (Marten as the sole event-sourcing provider, which is why this duplication is now an intra-package concern rather than a cross-provider one).
