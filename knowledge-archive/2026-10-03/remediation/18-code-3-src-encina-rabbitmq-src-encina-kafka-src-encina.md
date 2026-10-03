<!--
title: [DEBT] RabbitMQ, Kafka, NATS, Azure Service Bus and Amazon SQS transports are publish-only: no subscription, consumption or dead-letter support
labels: technical-debt
milestone:
kind: debt
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [x] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [ ] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

AGENTS.md section 5 (Transports row) states that every provider of the transport category MUST support send/publish, subscription management, error handling and DLQ, and metadata propagation. Five of the ten existing transports are send-only: `Encina.RabbitMQ`, `Encina.Kafka`, `Encina.NATS`, `Encina.AzureServiceBus` and `Encina.AmazonSQS`. These are the message-broker and streaming transports, the ones where subscription and dead-letter handling matter most. Only `Encina.MQTT`, `Encina.Redis.PubSub` and `Encina.InMemory` implement subscribe. (`Encina.gRPC` and `Encina.GraphQL` are API bridges; `Encina.GraphQL` declares `SubscribeAsync` at `GraphQLMediatorBridge.cs:124` as a bridge API, not as transport subscription management.)

## Location

- **File(s)**: `src/Encina.RabbitMQ`, `src/Encina.Kafka`, `src/Encina.NATS`, `src/Encina.AzureServiceBus`, `src/Encina.AmazonSQS` (whole packages); `docs/messaging/transports.md:235,270-273,316,365` (consume samples for APIs that do not exist)
- **Package(s)**: Encina.RabbitMQ, Encina.Kafka, Encina.NATS, Encina.AzureServiceBus, Encina.AmazonSQS

## Current Behavior

Each of the five packages contains only an options class, an `I{X}MessagePublisher` interface, its implementation, `Log.cs`, `ServiceCollectionExtensions.cs` and a health check. A search of each package for `SubscribeAsync|ConsumeAsync|Subscribe\(` and for `DeadLetter|DLQ` returns zero matches in every one; none has a subscribe, consume or dead-letter-handling type.

`docs/messaging/transports.md` shows consume samples for four of them (`IMessageHandler<OrderCreated>` at :235 for RabbitMQ, `consumer.ConsumeFromAsync(` at :270-273 for Kafka, `consumer.ReplayFromAsync(` at :316 for NATS JetStream, `consumer.StartAsync(ct)` at :365 for Amazon SQS). A search of `src/Encina.{RabbitMQ,Kafka,NATS,AzureServiceBus,AmazonSQS}` for `ConsumeFromAsync|ReplayFromAsync|interface IMessageHandler|IKafkaMessageConsumer|StartAsync\(` returns no matches, so those APIs do not exist.

## Expected Behavior

Each of the five transports offers subscription management and error handling with dead-lettering consistent with the transports that already implement subscribe (`Encina.MQTT` and `Encina.Redis.PubSub` expose `SubscribeAsync`), so that the transport category is coherent as AGENTS.md section 5 requires. Alternatively, the AGENTS.md row is amended by an explicit, recorded decision that these packages are publish-only.

## Root Cause

The five packages were built as publishers only. The coherence requirement for the transport category was never applied to subscription and dead-letter handling, and the documentation page describing the transports promised full native APIs per transport without the code providing them.

## Proposed Fix

Decide per transport the consume model that fits it (queue consumer for RabbitMQ, Azure Service Bus and Amazon SQS; consumer groups and offsets for Kafka; core subscriptions and JetStream consumers for NATS), write the plan first, then implement subscription management and dead-letter handling in each package with the same shape as the existing `SubscribeAsync` of MQTT and Redis.PubSub, with unit tests and integration tests against real brokers. Until a package gains this support, the transports page must not show consume samples for it.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [x] Large (> 4 hours)

## Related Issues

- #18 (This issue)
