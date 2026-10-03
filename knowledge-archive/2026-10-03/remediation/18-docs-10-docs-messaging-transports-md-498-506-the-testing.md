<!--
title: [DEBT] docs/messaging/transports.md "Testing with InMemory" example calls GetPublishedMessages, which IInMemoryMessageBus does not have
labels: technical-debt
milestone:
kind: docs
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

The "Testing with InMemory" example in `docs/messaging/transports.md`, the page's flagship testing pattern for `Encina.InMemory`, calls a method that the bus interface does not declare, so it cannot be written against the real API.

## Location

- **File(s)**: `docs/messaging/transports.md:498-506`; `src/Encina.InMemory/IInMemoryMessageBus.cs`
- **Package(s)**: Encina.InMemory

## Current Behavior

Lines 498-506 call `bus.GetPublishedMessages<OrderCreatedEvent>()` on an `IInMemoryMessageBus`. `src/Encina.InMemory/IInMemoryMessageBus.cs` (52 lines) declares only `PublishAsync`, `EnqueueAsync`, `Subscribe<TMessage>(Func<TMessage, ValueTask>)`, `PendingCount` and `SubscriberCount`; there is no way to read back published messages. The "Benefits" list below the example (`:510-514`) also promises "Inspect all published messages".

## Expected Behavior

The example shows a test that observes published messages through the real API, for example by registering a handler with `Subscribe<TMessage>` that records each message it receives, then asserting on the recorded list.

## Root Cause

The example was written against an inspection API that `IInMemoryMessageBus` never had.

## Proposed Fix

Rewrite the example at `docs/messaging/transports.md:489-508` to subscribe with `Subscribe<OrderCreatedEvent>(...)` before sending the command and assert on the messages collected by the handler, and correct the "Inspect all published messages" bullet so it describes what the interface offers.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #18 (This issue)
