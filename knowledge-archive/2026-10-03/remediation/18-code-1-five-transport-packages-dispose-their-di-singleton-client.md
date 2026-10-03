<!--
title: [BUG] NATS, RabbitMQ, MQTT, Azure Service Bus and Kafka scoped publishers dispose the DI-singleton client when their scope ends
labels: bug
milestone: v0.14.0 — Hardening
kind: bug
-->

## Description

Five transport packages register their connection, channel, client or producer as a DI singleton, but register the publisher that wraps it as scoped, and the publisher disposes the shared object in its own `Dispose`/`DisposeAsync`. The .NET DI container disposes every disposable service it created when the owning scope ends, so the first time a consumer resolves the publisher inside a scope that later ends (an ASP.NET Core request scope, `IServiceScopeFactory.CreateScope()` in a handler, a background-job scope), the shared singleton is closed for the rest of the application's lifetime. Every later publish on that transport then fails until the application restarts.

The disposed object and the registrations involved, per package:

| Package | Publisher disposal | Shared object (singleton registration) | Publisher registration |
|---------|--------------------|----------------------------------------|------------------------|
| `Encina.NATS` | `NATSMessagePublisher.cs:191-194`: `DisposeAsync` awaits `_connection.DisposeAsync()` | `INatsConnection`, `ServiceCollectionExtensions.cs:42` (`TryAddSingleton`) | `TryAddScoped`, `ServiceCollectionExtensions.cs:61` |
| `Encina.RabbitMQ` | `RabbitMQMessagePublisher.cs:158-162`: `DisposeAsync` calls `_channel.CloseAsync()` then `_connection.CloseAsync()` | `IConnection` (`:42`) and `IChannel` (`:56`), both `TryAddSingleton` | `TryAddScoped`, `ServiceCollectionExtensions.cs:62` |
| `Encina.MQTT` | `MQTTMessagePublisher.cs:165-173`: `DisposeAsync` disconnects when connected, then `_client.Dispose()` (`:172`) | `IMqttClient`, `ServiceCollectionExtensions.cs:43` (`TryAddSingleton`) | `TryAddScoped`, `ServiceCollectionExtensions.cs:72` |
| `Encina.AzureServiceBus` | `AzureServiceBusMessagePublisher.cs:201-204`: `DisposeAsync` awaits `_client.DisposeAsync()`; `_client` is the constructor-injected `ServiceBusClient` (`:15,26,34`) | `ServiceBusClient`, `ServiceCollectionExtensions.cs:47-48` (`TryAddSingleton`) | `TryAddScoped`, `ServiceCollectionExtensions.cs:50` |
| `Encina.Kafka` | `KafkaMessagePublisher.cs:196-200`: `Dispose()` calls `_producer.Flush(TimeSpan.FromSeconds(10))` then `_producer.Dispose()`; `_producer` is the constructor-injected `IProducer<string, byte[]>` (`:15,26,34`) | the `ProducerBuilder<string, byte[]>(...).Build()` producer, `ServiceCollectionExtensions.cs:42-59` (`TryAddSingleton`) | `TryAddScoped`, `ServiceCollectionExtensions.cs:61` |

`ValidateOnBuild`/`ValidateScopes` do not catch this: they detect captive dependencies (a long-lived service holding a short-lived one), not premature disposal of a longer-lived dependency by a shorter-lived owner. It fails the AGENTS.md section 3 registration-completeness expectation that an `AddEncina*` registration is safe to build and use in any composition. Kafka additionally blocks the disposing thread for up to 10 seconds in a synchronous `Flush` inside a container-invoked `Dispose()`.

`Encina.AmazonSQS` and the non-broker transports are not affected.

## Steps to Reproduce

1. Register one of the five transports (for example `services.AddEncinaKafka()`, or a substitute singleton for the producer plus `services.AddScoped<IKafkaMessagePublisher, KafkaMessagePublisher>()`).
2. Build the provider and create a scope: `using (var scope = provider.CreateScope()) { scope.ServiceProvider.GetRequiredService<IKafkaMessagePublisher>(); }`.
3. Let the scope end (the `using` block exits).
4. Observe that the singleton producer was flushed and disposed by the scope's disposal.
5. Publish again from a new scope or from the root provider.

The same sequence applies to NATS (`INatsConnection`), RabbitMQ (`IConnection`, `IChannel`), MQTT (`IMqttClient`) and Azure Service Bus (`ServiceBusClient`).

## Expected Behavior

Ending a scope that resolved the publisher leaves the shared connection, channel, client or producer open and usable. The singleton is disposed once, by the container, when the root provider is disposed.

## Actual Behavior

Ending the scope disposes (or closes) the shared singleton. Every later publish on that transport fails until the application restarts. For Kafka the scope's disposal also blocks the thread for up to 10 seconds in `Flush`.

## Environment

- **Encina Version**: 0.14.0-dev
- **.NET Version**: .NET 10
- **OS**: Not applicable (found by static review of the code, not at runtime)
- **Package(s) Affected**: Encina.NATS, Encina.RabbitMQ, Encina.MQTT, Encina.AzureServiceBus, Encina.Kafka

## Code Sample

```csharp
var producer = Substitute.For<IProducer<string, byte[]>>();

var services = new ServiceCollection();
services.AddLogging();
services.AddOptions<EncinaKafkaOptions>();
services.AddSingleton(producer);
services.AddScoped<IKafkaMessagePublisher, KafkaMessagePublisher>();

using var provider = services.BuildServiceProvider(validateScopes: true);

using (var scope = provider.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<IKafkaMessagePublisher>();
}

// Today both calls were received once: the scope disposed the singleton producer.
producer.Received(1).Flush(Arg.Any<TimeSpan>());
producer.Received(1).Dispose();
```

## Stack Trace

Not applicable: the disposal happens silently when the scope ends, so there is no exception at the point of the defect.

## Root Cause

Each publisher implements `IAsyncDisposable` (`IDisposable` for Kafka) and releases a constructor-injected object it does not own, while `AddEncina*` registers the publisher with a shorter lifetime (scoped) than that object (singleton). None of the five publishers holds per-request state.

## Proposed Fix

Per package, either register the publisher as a singleton, or remove the publisher's `Dispose`/`DisposeAsync` so that only the container's singleton-disposal path owns the connection, channel, client or producer. The existing unit tests assert the shared object IS disposed and must change with the fix: `tests/Encina.UnitTests/RabbitMQ/Publishing/RabbitMQMessagePublisherTests.cs:313`, `tests/Encina.UnitTests/NATS/Publishing/NATSMessagePublisherTests.cs:366`, `tests/Encina.UnitTests/NATS/Publishing/NATSMessagePublisherRequestAsyncTests.cs:82`, `tests/Encina.UnitTests/MQTT/Publishing/MQTTMessagePublisherTests.cs:264,281` and `tests/Encina.UnitTests/Kafka/Publishing/KafkaMessagePublisherTests.cs:354`.

## Additional Context

Related Issues: #18 (This issue).
