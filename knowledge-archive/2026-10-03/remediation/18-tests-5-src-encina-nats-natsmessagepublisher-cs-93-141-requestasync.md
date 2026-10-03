<!--
title: [TEST] NATSMessagePublisher.RequestAsync is untested beyond its null guard (lines 93-141 uncovered)
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

`src/Encina.NATS/NATSMessagePublisher.cs:93-141` (`RequestAsync<TRequest, TResponse>`, everything after the `ArgumentNullException.ThrowIfNull` at :91) is 0% unit-covered and 0% guard-covered in the Release run: 31 uncovered lines, 35% of the file's 89 coverable lines. The only tests of the method are null-request checks (`tests/Encina.UnitTests/NATS/Publishing/NATSMessagePublisherRequestAsyncTests.cs:71` and `NATSMessagePublisherTests.cs:185`), which stop at the guard. Despite its name, `NATSMessagePublisherRequestAsyncTests.cs` holds a class named `NATSMessagePublisherAdditionalTests` whose remaining tests are about `PublishAsync`, `JetStreamPublishAsync` and `DisposeAsync`.

The method has four outcomes and none is tested: success with a deserialized response, `NATS_DESERIALIZE_FAILED` when the reply deserializes to null (`:114-117`), `NATS_REQUEST_TIMEOUT` on a timeout-triggered cancellation (`:124-130`) and `NATS_REQUEST_FAILED` on any other exception (`:131-139`). The timeout-versus-caller-cancellation distinction at `:124` (`catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)`) could be inverted without a test failing.

## Packages / Providers Affected

- **Package(s)**: Encina.NATS
- **Provider(s)**: NATS (message transport; no database provider)

## Current Coverage

Line coverage of `NATSMessagePublisher.cs` measured with the `Release` configuration. The unit target (55) in `.github/coverage-manifest/Encina.NATS.json` is a package-wide aggregate, so no per-file gap is computed.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Encina.NATS `NATSMessagePublisher.cs` (unit) | 65.2% (58/89) | 55% (package-wide) | n/a |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Success: a substitute `INatsConnection.RequestAsync<byte[], byte[]>` returns a JSON reply; `RequestAsync` returns `Right` holding the deserialized response, and the request goes to the default subject `{SubjectPrefix}.{TRequest name}` when no subject is passed.
- [ ] A reply that deserializes to null returns a `Left` with code `NATS_DESERIALIZE_FAILED`.
- [ ] A connection that does not answer within the requested timeout (the linked token fires) returns a `Left` with code `NATS_REQUEST_TIMEOUT`.
- [ ] A token cancelled by the caller does not produce `NATS_REQUEST_TIMEOUT`: it falls through to a `Left` with code `NATS_REQUEST_FAILED`.
- [ ] Any other exception thrown by the connection returns a `Left` with code `NATS_REQUEST_FAILED`.

### Success Criteria

- [ ] All new tests pass
- [ ] Unit line coverage of `NATSMessagePublisher.cs` covers lines 93-141
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: unit tests only, no broker container.

## Related Issues

- #18 (This issue)
