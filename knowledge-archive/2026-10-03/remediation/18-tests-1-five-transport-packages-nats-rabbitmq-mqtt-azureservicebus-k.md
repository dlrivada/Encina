<!--
title: [TEST] Scope-lifetime regression tests for NATS, RabbitMQ, MQTT, Azure Service Bus and Kafka publishers; existing disposal tests assert the buggy behaviour
labels: area-testing
milestone:
kind: test
-->

## Test Category

- [x] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [ ] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

The scoped publishers of five transport packages dispose the DI-singleton connection, channel, client or producer they wrap when their scope ends, and no test can catch it; in four of the five packages the existing tests encode the buggy behaviour as the expected, passing outcome. Two gaps compound this.

1. No `ServiceCollectionExtensions*Tests.cs` of any of the five packages creates a scope or resolves the publisher (a search of every `.cs` under `tests/Encina.UnitTests` in the five package folders for `CreateScope` returns zero matches). `tests/Encina.UnitTests/RabbitMQ/ServiceCollectionExtensionsTests.cs:81` (`AddEncinaRabbitMQ_RegistersPublisher`) checks only the `ServiceDescriptor` lifetime; its own comment at :89 reads "will fail at resolution without real connection". The singleton factory delegates are never executed in RabbitMQ (`ServiceCollectionExtensions.cs:44-51,53,58-59`), NATS (`:44-48`), MQTT (`:45-69`) and Kafka (`:44-58`); the Azure Service Bus registration lines are fully covered but only at descriptor level, with no scope ever created or disposed.
2. The existing disposal tests assert that the shared object IS disposed, so fixing the lifetime defect would make them fail:
   - `tests/Encina.UnitTests/RabbitMQ/Publishing/RabbitMQMessagePublisherTests.cs:313` `DisposeAsync_ShouldCloseChannelAndConnection`
   - `tests/Encina.UnitTests/NATS/Publishing/NATSMessagePublisherTests.cs:366` `DisposeAsync_ShouldDisposeConnection` and `tests/Encina.UnitTests/NATS/Publishing/NATSMessagePublisherRequestAsyncTests.cs:82` `DisposeAsync_DisposesConnection` (the class in that file is named `NATSMessagePublisherAdditionalTests`)
   - `tests/Encina.UnitTests/MQTT/Publishing/MQTTMessagePublisherTests.cs:264,281` `DisposeAsync_WhenConnected_ShouldDisconnectAndDispose` and `DisposeAsync_WhenNotConnected_ShouldOnlyDispose` (`_client.Received(1).Dispose()`)
   - `tests/Encina.UnitTests/Kafka/Publishing/KafkaMessagePublisherTests.cs:354` `Dispose_ShouldFlushAndDisposeProducer` (`_producer.Received(1).Flush(...)` and `.Dispose()`)

   Azure Service Bus has no disposal test at all: `AzureServiceBusMessagePublisher.cs:203-204` (the `DisposeAsync` body) is not executed by the unit run, and no file under `tests/Encina.UnitTests/AzureServiceBus` mentions `Dispose`.

## Packages / Providers Affected

- **Package(s)**: Encina.NATS, Encina.RabbitMQ, Encina.MQTT, Encina.AzureServiceBus, Encina.Kafka
- **Provider(s)**: NATS, RabbitMQ, MQTT, AzureServiceBus, Kafka (message transports; no database provider)

## Current Coverage

Unit-flag line coverage of each package's `ServiceCollectionExtensions.cs`, measured with the `Release` configuration (398 unit tests passed, 0 failed). The unit target in `.github/coverage-manifest/{Package}.json` is 55 and is a package-wide aggregate, so no per-file gap is computed.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Encina.NATS `ServiceCollectionExtensions.cs` | 78.8% (26/33) | 55% (package-wide) | n/a |
| Encina.RabbitMQ `ServiceCollectionExtensions.cs` | 71.1% (27/38) | 55% (package-wide) | n/a |
| Encina.MQTT `ServiceCollectionExtensions.cs` | 71.4% (35/49) | 55% (package-wide) | n/a |
| Encina.Kafka `ServiceCollectionExtensions.cs` | 63.2% (24/38) | 55% (package-wide) | n/a |
| Encina.AzureServiceBus `ServiceCollectionExtensions.cs` | 100% (24/24), descriptor level only | 55% (package-wide) | n/a |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] RabbitMQ: register NSubstitute `IConnection` and `IChannel` as singletons plus `services.AddScoped<IRabbitMQMessagePublisher, RabbitMQMessagePublisher>()`, build the provider, resolve the publisher inside `using (var scope = provider.CreateScope()) { ... }`, and assert that neither substitute received `CloseAsync` after the scope ended.
- [ ] NATS: same shape with a substitute `INatsConnection` singleton (assert it was not disposed). The publisher constructor also takes `INatsJSContext?` (`NATSMessagePublisher.cs:30`), which `AddEncinaNATS` registers only when `UseJetStream` is true, so the test registers a substitute `INatsJSContext` as well.
- [ ] MQTT: same shape with a substitute `IMqttClient` singleton (assert no `Dispose` and no `DisconnectAsync` after the scope ended).
- [ ] Azure Service Bus: same shape with a substitute `ServiceBusClient` singleton (assert it was not disposed).
- [ ] Kafka: same shape with a substitute `IProducer<string, byte[]>` singleton (assert it was neither flushed nor disposed after the scope ended).
- [ ] Rewrite `DisposeAsync_ShouldCloseChannelAndConnection`, `DisposeAsync_ShouldDisposeConnection`, `DisposeAsync_DisposesConnection`, `DisposeAsync_WhenConnected_ShouldDisconnectAndDispose`, `DisposeAsync_WhenNotConnected_ShouldOnlyDispose` and `Dispose_ShouldFlushAndDisposeProducer` to the fixed contract; delete them if the fix removes `Dispose`/`DisposeAsync` from the publisher.
- [ ] Assert, per package, that the shared object is disposed exactly once when the root provider is disposed.

### Success Criteria

- [ ] All new tests pass
- [ ] Every test above fails against the current code and passes after the lifetime fix
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: unit tests only, no database or broker container.

## Related Issues

- #18 (This issue)
