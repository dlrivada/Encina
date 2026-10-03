<!--
title: [TEST] Add guard clause and unit tests for Encina.Messaging.Sagas.SagaNotFoundContext
labels: area-testing
milestone: 
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

The class `Encina.Messaging.Sagas.SagaNotFoundContext` currently has 0% guard coverage (0/29 lines) despite containing three real guard clauses: `ArgumentNullException` checks for `sagaType` and `messageType` in the constructor, and `ArgumentException.ThrowIfNullOrWhiteSpace(reason)` in `MoveToDeadLetterAsync`. Currently, no `Encina.GuardTests` file targets this class (confirmed by directory listing, as `SagaNotFoundContextTests.cs` exists only under `Encina.UnitTests`). The manifest requires unit and guard coverage for this component.

## Packages / Providers Affected

- **Package(s)**: Encina.Messaging
- **Provider(s)**: None (Core Library)

## Current Coverage

> Fill in if this is a coverage gap issue.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Encina.Messaging.Sagas.SagaNotFoundContext | 0% | 85% | -85% |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Test 1: Verify `ArgumentNullException` is thrown when `sagaType` is null in the constructor.
- [ ] Test 2: Verify `ArgumentNullException` is thrown when `messageType` is null in the constructor.
- [ ] Test 3: Verify `ArgumentException.ThrowIfNullOrWhiteSpace(reason)` behavior in `MoveToDeadLetterAsync` when `reason` is null.
- [ ] Test 4: Verify `ArgumentException.ThrowIfNullOrWhiteSpace(reason)` behavior in `MoveToDeadLetterAsync` when `reason` is whitespace.
- [ ] Test 5: Verify valid construction of `SagaNotFoundContext` with valid `sagaType` and `messageType`.

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
- #1389 - possibly related (the local model proposed it as a duplicate; the evidence check rejected it)

- #16 - SPEC-003 audit finding for closed issue
- #1389 - [TEST] Guard flag below target in 42 of 99 packages: close the gap per package family