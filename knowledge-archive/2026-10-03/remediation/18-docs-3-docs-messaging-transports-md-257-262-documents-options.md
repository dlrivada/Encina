<!--
title: [DEBT] docs/messaging/transports.md Kafka sample uses options and a consumer API that do not exist
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

The Kafka sample in `docs/messaging/transports.md` names two option members that `EncinaKafkaOptions` does not declare and calls a consumer method that exists nowhere in `Encina.Kafka`. The sample cannot be compiled against the package.

## Location

- **File(s)**: `docs/messaging/transports.md:257-262,270-273`; `src/Encina.Kafka/EncinaKafkaOptions.cs`
- **Package(s)**: Encina.Kafka

## Current Behavior

Lines 257-262 set `options.ConsumerGroup = "my-service"` and `options.DefaultTopic = "events"`. `src/Encina.Kafka/EncinaKafkaOptions.cs` declares `GroupId` and `DefaultCommandTopic` / `DefaultEventTopic` instead (together with `BootstrapServers`, `AutoOffsetReset`, `EnableAutoCommit`, `Acks`, `EnableIdempotence` and `MessageTimeoutMs`); neither documented name exists.

Lines 270-273 (`consumer.ConsumeFromAsync(topic: "events", offset: 12345, ct)`) reference a consumer type and method that do not exist in `Encina.Kafka`: the package contains only `EncinaKafkaOptions.cs`, `IKafkaMessagePublisher.cs`, `KafkaMessagePublisher.cs`, `Log.cs`, `ServiceCollectionExtensions.cs`, a health check and `GlobalSuppressions.cs`. It is publisher only, with no consumer.

## Expected Behavior

The Kafka sample uses `GroupId` and `DefaultEventTopic` (or `DefaultCommandTopic`), and shows publishing only, since the package has no consumer.

## Root Cause

The sample was written without being checked against `EncinaKafkaOptions.cs`, and describes a consumer capability the package does not implement.

## Proposed Fix

Rewrite lines 257-262 with the real member names, and remove the `ConsumeFromAsync` sample (lines 269-273) or replace it with a statement that the package only publishes today.

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
