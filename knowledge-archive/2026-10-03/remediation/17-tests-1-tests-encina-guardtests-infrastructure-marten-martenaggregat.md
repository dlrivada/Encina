<!--
title: [DEBT] Duplicate guard tests for MartenAggregateRepository and SnapshotAwareAggregateRepository
labels: technical-debt
milestone: 
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

Two pairs of test classes exist that test identical constructor null-guards for `MartenAggregateRepository<TestAggregate>` and `SnapshotAwareAggregateRepository<T>` but reside in different namespaces with varying mock-setup styles. These duplicates provide zero incremental coverage while doubling maintenance costs, contrary to the goals outlined in `docs/plans/test-consolidation-plan.md`.

## Location

- **File(s)**: `tests\Encina.GuardTests\Infrastructure\Marten\MartenAggregateRepositoryGuardTests.cs`, `tests\Encina.GuardTests\Marten\Core\MartenAggregateRepositoryGuardTests.cs`, `tests\Encina.GuardTests\Infrastructure\Marten\SnapshotAwareAggregateRepositoryGuardTests.cs`, `tests\Encina.GuardTests\Marten\Snapshots\SnapshotAwareAggregateRepositoryGuardTests.cs`
- **Package(s)**: [Encina.GuardTests]

## Current Behavior

The codebase contains duplicate test implementations:
1. `Encina.GuardTests.Infrastructure.Marten.MartenAggregateRepositoryGuardTests` and `Encina.GuardTests.Marten.Core.MartenAggregateRepositoryGuardTests` both test `MartenAggregateRepository<TestAggregate>` constructor null-guards (`Constructor_Null{Session,RequestContext,Logger,Options}_Throws`). They differ only in mock-setup style (instance fields with `Substitute.For` vs. static readonly fields with `NullLogger`).
2. `Encina.GuardTests.Infrastructure.Marten.SnapshotAwareAggregateRepositoryGuardTests` and `Encina.GuardTests.Marten\Snapshots.SnapshotAwareAggregateRepositoryGuardTests` both test `SnapshotAwareAggregateRepository<T>` constructor guards line for line.
Both duplicate pairs cover the same measured lines (24/164 and 42/259 respectively) with no additional coverage benefit.

## Expected Behavior

The duplicate test pairs should be consolidated into single, unified test classes to eliminate redundancy, reduce maintenance overhead, and align with the test consolidation plan.

## Root Cause

Historical development led to separate test classes being created in different namespaces for the same production types without subsequent consolidation. This debt was not introduced by issue #17 (commit `87d92a39`) but is pre-existing and currently in scope for test consolidation.

## Proposed Fix

Consolidate the duplicate test pairs by removing one implementation from each pair and ensuring the remaining test class covers all necessary scenarios using a consistent mock-setup style. This aligns with the active plan in `docs/plans/test-consolidation-plan.md`.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #17