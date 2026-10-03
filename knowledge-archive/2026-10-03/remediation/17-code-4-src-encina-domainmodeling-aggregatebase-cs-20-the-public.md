<!--
title: [DEBT] Remove EventStoreDB reference from AggregateBase XML documentation
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

The public XML documentation for `AggregateBase` in the `Encina.DomainModeling` package incorrectly references EventStoreDB as a supported event store option. This contradicts the decision made in issue #17 to support only Marten as the event store backend.

## Location

- **File(s)**: `src/Encina.DomainModeling/AggregateBase.cs:20`
- **Package(s)**: Encina.DomainModeling

## Current Behavior

The XML doc comment for `AggregateBase` states: "You're using an event store like Marten or EventStoreDB," presenting EventStoreDB as a live, supported option in the public API documentation.

## Expected Behavior

The XML doc comment should only reference Marten as the supported event store, consistent with the library's current architecture and the decision documented in issue #17.

## Root Cause

The documentation was written before or independently of the decision to restrict event store support to Marten only, and was not updated during the remediation of issue #17.

## Proposed Fix

Update the XML doc comment in `AggregateBase.cs` at line 20 to remove the reference to EventStoreDB and only mention Marten as the supported event store.

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