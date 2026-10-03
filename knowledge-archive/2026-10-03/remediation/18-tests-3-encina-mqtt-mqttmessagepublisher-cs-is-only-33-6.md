<!--
title: [TEST] MQTTMessagePublisher subscribe and pattern-subscribe path has no unit coverage (33.6% line coverage)
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

`src/Encina.MQTT/MQTTMessagePublisher.cs` is 33.6% unit-line-covered (49/146). Every uncovered unit line (107-162 and 184-313) is the `SubscribeAsync` and `SubscribePatternAsync` methods and the internal `MqttSubscription<TMessage>` and `MqttPatternSubscription<TMessage>` classes, including their `DisposeAsync` unsubscribe logic. The only subscribe tests, `tests/Encina.UnitTests/MQTT/Publishing/MQTTMessagePublisherTests.cs:299-353`, are null-argument checks.

Subscription is one of the capabilities AGENTS.md section 5 requires from every transport, and MQTT is one of only three transports (with `Encina.Redis.PubSub` and `Encina.InMemory`) that implement it. Nothing exercises a subscribe, receive-a-message, dispose path, so a regression in unsubscribe cleanup or a double dispose would pass CI silently.

## Packages / Providers Affected

- **Package(s)**: Encina.MQTT
- **Provider(s)**: MQTT (message transport; no database provider)

## Current Coverage

Unit-flag line coverage measured with the `Release` configuration. The unit target (55) in `.github/coverage-manifest/Encina.MQTT.json` is a package-wide aggregate, so no per-file gap is computed.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Encina.MQTT `MQTTMessagePublisher.cs` (unit) | 33.6% (49/146) | 55% (package-wide) | n/a |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

Note on testability: `SubscribeAsync` and `SubscribePatternAsync` cast the injected client with `(MqttClient)_client` (`MQTTMessagePublisher.cs:125,159`) and the subscription classes take the concrete `MqttClient` (`:185,238`). An NSubstitute `IMqttClient` cannot satisfy that cast, so the tests need a real `MqttClient` instance that never connects, or the code first gains a seam that removes the cast.

### Tests to Implement

- [ ] `SubscribeAsync` with a valid handler and topic subscribes with the requested QoS (falling back to the configured `QualityOfService` when none is given) and returns a subscription.
- [ ] A message received on the subscribed topic is deserialized and passed to the handler; a message on another topic is ignored.
- [ ] A payload that fails to deserialize, or a handler that throws, is logged and does not propagate out of the received-message event.
- [ ] Disposing the subscription detaches the received-message handler and unsubscribes the topic; a second dispose does not fail.
- [ ] `SubscribePatternAsync` delivers the concrete topic together with the message to the handler.
- [ ] Topic-filter matching of the pattern subscription: a trailing `#` matches by prefix, `+` matches exactly one level (and rejects a different level count), and a filter without wildcards matches only the equal topic (`MQTTMessagePublisher.cs:271-302`).
- [ ] Disposing a pattern subscription detaches the handler and unsubscribes the filter.

### Success Criteria

- [ ] All new tests pass
- [ ] Unit line coverage of `MQTTMessagePublisher.cs` covers lines 107-162 and 184-313
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: unit tests only, no broker container.

## Related Issues

- #18 (This issue)
