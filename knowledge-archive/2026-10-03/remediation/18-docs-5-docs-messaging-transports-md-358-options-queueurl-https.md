<!--
title: [DEBT] docs/messaging/transports.md Amazon SQS sample uses options.QueueUrl and a consumer type that do not exist
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

The Amazon SQS sample in `docs/messaging/transports.md` assigns `options.QueueUrl`, which `EncinaAmazonSQSOptions` does not declare, and starts a consumer through a type that does not exist in `Encina.AmazonSQS`. The sample does not compile.

## Location

- **File(s)**: `docs/messaging/transports.md:358,365`; `src/Encina.AmazonSQS/EncinaAmazonSQSOptions.cs:18`
- **Package(s)**: Encina.AmazonSQS

## Current Behavior

Line 358 reads `options.QueueUrl = "https://sqs..."`. The options class exposes `DefaultQueueUrl` (`EncinaAmazonSQSOptions.cs:18`) and `DefaultTopicArn`, never `QueueUrl`.

Line 365 (`await consumer.StartAsync(ct);`) references a consumer type that does not exist in `Encina.AmazonSQS`: the package contains `AmazonSQSMessagePublisher.cs`, `EncinaAmazonSQSOptions.cs`, `IAmazonSQSMessagePublisher.cs`, `Log.cs`, `ServiceCollectionExtensions.cs`, a health check and `GlobalSuppressions.cs`. It is publisher only, with no `StartAsync` and no `IHostedService`/`BackgroundService` implementation.

## Expected Behavior

The sample sets `options.DefaultQueueUrl` and shows publishing only, since the package has no consumer.

## Root Cause

The sample was written without being checked against `EncinaAmazonSQSOptions.cs`, and describes a consumer capability the package does not implement.

## Proposed Fix

Change `QueueUrl` to `DefaultQueueUrl` at `docs/messaging/transports.md:358`, and remove the `consumer.StartAsync(ct)` sample (lines 364-365) or replace it with a statement that the package only publishes today.

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
