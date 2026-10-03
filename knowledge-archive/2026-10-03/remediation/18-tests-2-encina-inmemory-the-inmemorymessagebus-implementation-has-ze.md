<!--
title: [TEST] Encina.InMemory has no tests and its coverage manifest marks InMemoryMessageBus.cs as an interface
labels: area-testing
milestone:
kind: test
-->

## Test Category

- [x] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [x] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

`Encina.InMemory` (the `InMemoryMessageBus` implementation) has no tests of any type anywhere in `tests/`. Measured line coverage is 0/110 for unit and 0/110 for guard on `src/Encina.InMemory/InMemoryMessageBus.cs`, and 0% on `EncinaInMemoryOptions.cs` and `ServiceCollectionExtensions.cs` as well. No `new InMemoryMessageBus(` construction exists anywhere under `tests/`; the only references are the `Encina.NBomber` load-scenario factories, which build a real host to measure throughput, not correctness, and an architecture check in `EncinaEventIdAllocationTests.cs` that only verifies EventId-range ownership.

This is the package that `docs/messaging/transports.md` recommends as the purpose-built substitute for testing every other transport, so the package the test story leans on hardest is itself untested.

The cause is in the coverage manifest: `.github/coverage-manifest/Encina.InMemory.json:27-30` lists `"InMemoryMessageBus.cs"` with `"defaultTests": []` and `"reason": "Interface — no implementation"`, a copy of the entry of the real interface file `IInMemoryMessageBus.cs` (`:22-25`). `InMemoryMessageBus.cs` is a concrete `sealed class` (`:13`) of 251 lines: a `Channel<object>`-backed publish/enqueue/subscribe implementation with background worker tasks, a `ConcurrentDictionary` subscriber registry and `IDisposable`. The manifest tells tooling this file needs no tests.

## Packages / Providers Affected

- **Package(s)**: Encina.InMemory
- **Provider(s)**: InMemory (message transport; no database provider)

## Current Coverage

Line coverage measured with the `Release` configuration. The unit (55) and guard (15) targets in `.github/coverage-manifest/Encina.InMemory.json` are package-wide aggregates, so no per-file gap is computed.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Encina.InMemory `InMemoryMessageBus.cs` (unit) | 0% (0/110) | 55% (package-wide) | n/a |
| Encina.InMemory `InMemoryMessageBus.cs` (guard) | 0% (0/110) | 15% (package-wide) | n/a |
| Encina.InMemory `EncinaInMemoryOptions.cs` (unit) | 0% (0/5) | 55% (package-wide) | n/a |
| Encina.InMemory `ServiceCollectionExtensions.cs` (unit) | 0% (0/13) | 55% (package-wide) | n/a |
| Encina.InMemory `ServiceCollectionExtensions.cs` (guard) | 0% (0/13) | 15% (package-wide) | n/a |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Correct the `InMemoryMessageBus.cs` entry in `.github/coverage-manifest/Encina.InMemory.json` so that unit and guard apply to it, like the `*.cs` default rule it already names.
- [ ] Unit: `PublishAsync` delivers the message to every subscriber of its type and returns `Right`; a handler that throws makes `PublishAsync` return a `Left` with code `INMEMORY_PUBLISH_FAILED`.
- [ ] Unit: `EnqueueAsync` increments `PendingCount` and the background worker decrements it after delivering to subscribers; a failure while writing returns a `Left` with code `INMEMORY_ENQUEUE_FAILED`.
- [ ] Unit: bounded-channel `FullMode` behaviour for `Wait`, `DropOldest` and `DropNewest`, and the unbounded channel when `UseUnboundedChannel` is true.
- [ ] Unit: `Subscribe` increments `SubscriberCount`; disposing the returned subscription removes the handler; concurrent subscribe and unsubscribe leave a consistent count.
- [ ] Unit: `Dispose` cancels the workers and completes the channel writer.
- [ ] Unit: `AddEncinaInMemory` registers `IInMemoryMessageBus` as a singleton, copies every `EncinaInMemoryOptions` member, and does not replace an existing registration.
- [ ] Guard: null `message` for `PublishAsync` and `EnqueueAsync`, null `handler` for `Subscribe`, null `logger` and `options` in the `InMemoryMessageBus` constructor, null `services` for `AddEncinaInMemory`.

### Success Criteria

- [ ] All new tests pass
- [ ] Unit and guard line coverage of `src/Encina.InMemory` reaches the targets in `.github/coverage-manifest/Encina.InMemory.json`
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: unit and guard tests only, no database or broker container.

## Related Issues

- #18 (This issue)
