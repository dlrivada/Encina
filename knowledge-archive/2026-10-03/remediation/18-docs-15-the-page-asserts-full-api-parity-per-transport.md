<!--
title: [DEBT] docs/messaging/transports.md claims a full native API per transport and shows consume samples for packages that only publish
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

`docs/messaging/transports.md` asserts that each transport exposes its full native API ("Each transport exposes its native API... Kafka users get offsets and partitions", `:35`) and shows subscribe and consume samples for packages that implement no subscription, consumption or dead-letter handling. Instead of stating the real gap, the page invents code that implies the gap is closed. AGENTS.md section 5 (Transports row) requires "subscription management" and "error handling and DLQ" from every transport of this category; the page does not disclose that five packages lack them.

## Location

- **File(s)**: `docs/messaging/transports.md:35,235,270-273,316,365`
- **Package(s)**: Encina.RabbitMQ, Encina.Kafka, Encina.AzureServiceBus, Encina.AmazonSQS

## Current Behavior

None of `Encina.RabbitMQ`, `Encina.Kafka`, `Encina.AzureServiceBus` and `Encina.AmazonSQS` (nor `Encina.NATS`) implements subscription, consumption or dead-letter handling in `src/` today: a repo-wide search of the five broker and streaming packages for `SubscribeAsync|ConsumeAsync|Subscribe\(` and for `DeadLetter|DLQ` returns zero matches. Only MQTT, Redis.PubSub and InMemory implement subscribe.

The consume samples the page shows are `IMessageHandler<OrderCreated>` for RabbitMQ (`:235`), `consumer.ConsumeFromAsync(` for Kafka (`:270-273`), `consumer.ReplayFromAsync(` for NATS JetStream (`:316`) and `consumer.StartAsync(ct)` for Amazon SQS (`:365`); none of these APIs exists in the packages. The Azure Service Bus section (`:321-344`) shows no consume sample.

## Expected Behavior

The page states which transports can publish only and which can also subscribe, and shows consume samples only for APIs that exist (`SubscribeAsync` of `Encina.MQTT` and `Encina.Redis.PubSub`, `Subscribe<TMessage>` of `Encina.InMemory`).

## Root Cause

The page was written to describe the intended per-transport capabilities, and the claim of full native API parity was never compared with what the five packages implement.

## Proposed Fix

Reword the design-philosophy callout at `:35` so it does not promise full API parity, add a short statement or table of the publish-only transports, and remove or replace the four consume samples listed above until subscription and dead-letter support exists in those packages.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #18 (This issue)
