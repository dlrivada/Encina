<!-- issue
title: [DEBT] Six near-identical TransactionPipelineBehavior guard test files duplicate one shared class
labels: technical-debt, area-testing
milestone: v0.14.0 — Hardening
-->

Found by the SPEC-003 audit of #20.

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [ ] Documentation gap
- [ ] Incorrect implementation
- [x] Other

## Description

Issue #20 consolidated the production `TransactionPipelineBehavior` from 8 per-provider files into one shared class in `Encina.Messaging`, but the parallel test duplication was never cleaned up. Six near-identical guard test files still exist, all testing the same shared `Encina.Messaging.TransactionPipelineBehavior<,>`, with inconsistent thoroughness (three cover all four guard clauses, three cover only one), and `tests/Encina.UnitTests/Messaging/Behaviors/TransactionPipelineBehaviorTests.cs` already covers all four guard clauses plus the full commit/rollback/dispose matrix on the one real class. This is the same class of duplication issue #20 was opened to fix, just left standing in the test tree.

## Location

- **File(s)**:
  - `tests/Encina.GuardTests/Dapper/SqlServer/TransactionPipelineBehaviorGuardTests.cs`
  - `tests/Encina.GuardTests/ADO/SqlServer/TransactionPipelineBehaviorGuardsTests.cs`
  - `tests/Encina.GuardTests/ADO/PostgreSQL/TransactionPipelineBehaviorGuardsTests.cs`
  - `tests/Encina.GuardTests/Dapper/PostgreSQL/TransactionPipelineBehaviorGuardsTests.cs`
  - `tests/Encina.GuardTests/Dapper/MySQL/TransactionPipelineBehaviorGuardsTests.cs`
  - `tests/Encina.GuardTests/ADO/MySQL/TransactionPipelineBehaviorGuardsTests.cs`
- **Package(s)**: Encina.GuardTests

## Current Behavior

Six separate test files, one per Dapper/ADO provider, each instantiate and guard-test the same shared `Encina.Messaging.TransactionPipelineBehavior<,>` class. Three test all four guard clauses (constructor, request, context, nextStep); three test only one.

## Expected Behavior

A single guard test class covers the shared behavior's guard clauses (already true of `tests/Encina.UnitTests/Messaging/Behaviors/TransactionPipelineBehaviorTests.cs`); each provider's own test suite instead asserts (via a contract or DI test) that its registration resolves the shared behavior type, rather than re-testing guard clauses that belong to `Encina.Messaging`.

## Root Cause

The production-code refactoring in issue #20 moved the class but not its tests; the six per-provider guard test files were left in place as dead weight instead of being deleted or converted to a provider-specific DI/contract check.

## Proposed Fix

Delete the six redundant per-provider guard test files (or replace each with a one-line DI resolution/contract assertion specific to that provider's registration), keeping `tests/Encina.ContractTests/Messaging/TransactionPipelineBehaviorContractTests.cs` and `tests/Encina.UnitTests/Messaging/Behaviors/TransactionPipelineBehaviorTests.cs` as the source of truth for the shared behavior's guard clauses.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

Found by the SPEC-003 audit of #20.
