<!--
title: [DEBT] docs/messaging/transports.md RabbitMQ sample uses options and a handler interface that do not exist
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

The RabbitMQ sample in `docs/messaging/transports.md` sets option members that `EncinaRabbitMQOptions` does not have and shows a consumer built on an interface that does not exist anywhere in the repository. The sample cannot be compiled against `Encina.RabbitMQ`.

## Location

- **File(s)**: `docs/messaging/transports.md:224-229,235`; `src/Encina.RabbitMQ/EncinaRabbitMQOptions.cs`
- **Package(s)**: Encina.RabbitMQ

## Current Behavior

Lines 224-229 show `options.ConnectionString = "amqp://localhost"`, `options.Exchange = "my-exchange"` and `options.ExchangeType = ExchangeType.Topic`. `src/Encina.RabbitMQ/EncinaRabbitMQOptions.cs` declares `HostName`, `Port`, `VirtualHost`, `UserName`, `Password`, `ExchangeName`, `UsePublisherConfirms`, `PrefetchCount` and `Durable`: there is no `ConnectionString` and no `Exchange` (the member is `ExchangeName`), and no `ExchangeType` property or type exists anywhere under `src/`.

Line 235 (`public class OrderCreatedHandler : IMessageHandler<OrderCreated>`) references `IMessageHandler<T>`, which does not exist anywhere under `src/`. `Encina.RabbitMQ` has no consume or subscribe API at all.

## Expected Behavior

The RabbitMQ sample uses only members that exist: `HostName` (and `Port`, `VirtualHost`, `UserName`, `Password` as needed) and `ExchangeName`, and it shows publishing only, since the package has no consume API.

## Root Cause

The sample was written without being checked against `EncinaRabbitMQOptions.cs`; its option names read like the raw RabbitMQ client API, not the Encina package.

## Proposed Fix

Rewrite lines 224-229 with `HostName` and `ExchangeName`, remove the `ExchangeType` line, and remove the `IMessageHandler<OrderCreated>` consume sample (lines 234-241) or replace it with a statement that the package only publishes today.

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
