<!--
title: [TEST] No test executes the RabbitMQ and MQTT singleton factory delegates that block on the async connect
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

The `TryAddSingleton` factory delegates of `src/Encina.RabbitMQ/ServiceCollectionExtensions.cs` (`:53` `factory.CreateConnectionAsync().GetAwaiter().GetResult()` and `:59` `connection.CreateChannelAsync().GetAwaiter().GetResult()`) and of `src/Encina.MQTT/ServiceCollectionExtensions.cs` (`:67` `client.ConnectAsync(connectOptions).GetAwaiter().GetResult()`) block synchronously on an async connect. They are measured at 0% executed in the Release unit run: no existing test resolves `IConnection`, `IChannel` or `IMqttClient` from the built `ServiceProvider`, so the delegate bodies never run.

There is no substitution seam today. The RabbitMQ delegates create `new ConnectionFactory` inline (`:44-51`) and the MQTT delegate creates `new MqttClientFactory()` inline (`:45`); pre-registering a substitute `IConnection`, `IChannel` or `IMqttClient` makes `TryAddSingleton` skip the delegate instead of running it. A regression test for the blocking connect therefore depends on the fix of the sync-over-async defect, which has to expose a seam (for example an injectable connection factory or an awaited startup step).

## Packages / Providers Affected

- **Package(s)**: Encina.RabbitMQ, Encina.MQTT
- **Provider(s)**: RabbitMQ, MQTT (message transports; no database provider)

## Current Coverage

Unit-flag line coverage of each package's `ServiceCollectionExtensions.cs`, measured with the `Release` configuration. The unit target (55) in `.github/coverage-manifest/{Package}.json` is a package-wide aggregate, so no per-file gap is computed.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Encina.RabbitMQ `ServiceCollectionExtensions.cs` (uncovered lines 44-51, 53, 58-59) | 71.1% (27/38) | 55% (package-wide) | n/a |
| Encina.MQTT `ServiceCollectionExtensions.cs` (uncovered lines 45-69) | 71.4% (35/49) | 55% (package-wide) | n/a |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] RabbitMQ: with the connection factory seam substituted, resolving `IConnection` and `IChannel` from the built provider creates the connection and then the channel exactly once each, from the same singleton instances on a second resolution.
- [ ] MQTT: with the client seam substituted, resolving `IMqttClient` connects it once with the configured host, port, client id, clean-session flag and keep-alive, and applies credentials only when a username is set and TLS only when `UseTls` is true.
- [ ] RabbitMQ and MQTT: resolving the connection while a single-threaded `SynchronizationContext` is current does not deadlock when the connect completes asynchronously (this test fails against the blocking `GetAwaiter().GetResult()` pattern).

### Success Criteria

- [ ] All new tests pass
- [ ] Unit line coverage of both `ServiceCollectionExtensions.cs` files covers the factory delegates
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: unit tests only, no broker container.

## Related Issues

- #18 (This issue)
