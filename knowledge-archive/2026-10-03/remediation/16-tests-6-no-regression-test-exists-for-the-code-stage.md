<!--
title: [TEST] Add integration test for GetStuckSagasAsync to verify TimedOut sagas are not re-surfaced
labels: area-testing
milestone: 
-->

## Test Category

- [ ] Unit Tests
- [x] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [ ] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

A regression test is required to address the semantic drift in MongoDB's `GetStuckSagasAsync` regarding `TimedOut` sagas. The current test suite (`SagaStoreMongoDBIntegrationTests.cs`) contains dedicated status-transition tests for `Completed`, `Failed`, and `Compensating→Compensated`, but lacks any coverage for the `TimedOut` state. Existing tests `GetStuckSagasAsync_ShouldReturnOldUncompletedSagas` and `GetExpiredSagasAsync_ShouldReturnExpiredActiveSagas` only cover sagas that are still running; neither exercises a saga already marked as `TimedOut`, which is the specific state incorrectly re-surfaced by the bug identified in the code stage.

## Packages / Providers Affected

- **Package(s)**: Encina.MONGO (or specific MongoDB Saga Store package)
- **Provider(s)**: MongoDB

## Current Coverage

> Fill in if this is a coverage gap issue.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| N/A | N/A | N/A | N/A |

## Infrastructure Required

- [x] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [ ] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Test 1: Create a saga with status `TimedOut` and verify it is NOT returned by `GetStuckSagasAsync`.
- [ ] Test 2: Create a saga with status `Running` (old/expired) to ensure it IS returned by `GetStuckSagasAsync` (regression check for existing behavior).

### Success Criteria

- [ ] All new tests pass
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

> Per `AGENTS.md` §9 (Testing obligations): Integration tests MUST use shared `[Collection]` fixtures.

- **Collection**: MongoDB
- **Fixture**: MongoDBFixture

## Related Issues

- #16 - [Code] Semantic drift in MongoDB GetStuckSagasAsync for TimedOut sagas