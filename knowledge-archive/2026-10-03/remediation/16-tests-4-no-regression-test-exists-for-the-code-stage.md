<!--
title: [TEST] Add regression test for raw exception message leakage in SagaRunner
labels: area-testing
milestone: 
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

A clear description of the testing work needed. No regression test exists for the code stage's finding 3 (raw `ex.Message` reaching `EncinaError.Message`, logs, and the persisted `ISagaState.ErrorMessage` column). `SagaRunnerTests.cs:371` asserts only the error *code* (`SagaErrorCodes.HandlerFailed`); `SagaOrchestratorTests.cs` asserts only hand-authored friendly messages (`"not found"`, `"status"`) for different, non-exception code paths. No test drives a step handler to throw and then asserts the returned `EncinaError.Message` is not the raw exception text, nor that the persisted saga state's `ErrorMessage` is redacted.

## Packages / Providers Affected

- **Package(s)**: Encina
- **Provider(s)**: None

## Current Coverage

> Fill in if this is a coverage gap issue.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| N/A | N/A | N/A | N/A |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Test 1: Drive a step handler to throw an exception with a unique raw message and assert the returned `EncinaError.Message` does not contain the raw exception text
- [ ] Test 2: Drive a step handler to throw an exception and assert the persisted `ISagaState.ErrorMessage` column is redacted and does not contain the raw exception text

### Success Criteria

- [x] All new tests pass
- [ ] Coverage meets ≥85% target (if coverage gap)
- [x] No flaky tests introduced
- [x] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

> Per `AGENTS.md` §9 (Testing obligations): Integration tests MUST use shared `[Collection]` fixtures.

- **Collection**: N/A
- **Fixture**: N/A

## Related Issues

- #16 - [AUDIT] SPEC-003 audit of closed GitHub issue #16 of the Encina .NET library