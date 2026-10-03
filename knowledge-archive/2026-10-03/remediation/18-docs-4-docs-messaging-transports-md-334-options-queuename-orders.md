<!--
title: [DEBT] docs/messaging/transports.md Azure Service Bus sample sets options.QueueName, which does not exist
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

The Azure Service Bus sample in `docs/messaging/transports.md` assigns `options.QueueName`, a member that `EncinaAzureServiceBusOptions` does not declare. The sample does not compile.

## Location

- **File(s)**: `docs/messaging/transports.md:334`; `src/Encina.AzureServiceBus/EncinaAzureServiceBusOptions.cs:24`
- **Package(s)**: Encina.AzureServiceBus

## Current Behavior

Line 334 reads `options.QueueName = "orders";`. The options class of `Encina.AzureServiceBus` exposes `DefaultQueueName` (`EncinaAzureServiceBusOptions.cs:24`), `DefaultTopicName` and `SubscriptionName`, never `QueueName`.

## Expected Behavior

The sample sets `options.DefaultQueueName = "orders";`.

## Root Cause

The sample was written without being checked against `EncinaAzureServiceBusOptions.cs`.

## Proposed Fix

Change `QueueName` to `DefaultQueueName` at `docs/messaging/transports.md:334`.

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
