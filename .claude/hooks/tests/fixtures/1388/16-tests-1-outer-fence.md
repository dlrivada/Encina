```markdown
<!--
title: [TEST] Add Unit and Guard Clause Tests for SagaNotFoundDispatcher and IHandleSagaNotFound
labels: area-testing
milestone: 
-->

## Test Category

- [x] Unit Tests
- [x] Guard Clause Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

Add unit and guard clause tests for the `SagaNotFoundDispatcher` class and the `IHandleSagaNotFound` interface in the `Encina.Messaging` package. Currently, these components have 0% unit and 0% guard coverage. `Grep` confirms that zero files in the `tests/` directory reference `SagaNotFoundDispatcher` or `IHandleSagaNotFound`. As a DI-resolved production class, `SagaNotFoundDispatcher` involves cancellation handling, exception handling, and three distinct logged/returned outcomes (`no handler`, `success`, `cancelled`, `failed`). The existing code is not a thin wrapper and requires specific test coverage for each branch to meet the `.github/coverage-manifest/Encina.Messaging.json` requirements for unit and guard tests.

## Packages / Providers Affected

- **Package(s)**: Encina.Messaging
- **Provider(s)**: None

## Current Coverage

> Fill in if this is a coverage gap issue.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Encina.Messaging | 0.0% | 85% | -85.0% |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Test 1: Verify `SagaNotFoundDispatcher` handles the `no handler` outcome
- [ ] Test 2: Verify `SagaNotFoundDispatcher` handles the `success` outcome
- [ ] Test 3: Verify `SagaNotFoundDispatcher` handles the `cancelled` outcome
- [ ] Test 4: Verify `SagaNotFoundDispatcher` handles the `failed` outcome and exception handling
- [ ] Test 5: Verify guard clause behavior for `SagaNotFoundDispatcher` inputs

### Success Criteria

- [x] All new tests pass
- [x] Coverage meets ≥85% target (if coverage gap)
- [x] No flaky tests introduced
- [x] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

> Per `AGENTS.md` §9 (Testing obligations): Integration tests MUST use shared `[Collection]` fixtures.

- **Collection**: N/A
- **Fixture**: N/A

## Related Issues

- #16 - Finding: 0% unit and guard coverage for SagaNotFoundDispatcher and IHandleSagaNotFound
- #1338 - [TEST] Guard tests missing for all three validation providers (DataAnnotations, FluentValidation, MiniValidator)
- #1339 - [TEST] Add ValidateOnBuild DI proof tests for Encina.FluentValidation and Encina.MiniValidator
```