<!--
title: [TEST] Unit and Guard Clause tests for SagaNotFoundDispatcher and IHandleSagaNotFound (0% coverage)
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

The `SagaNotFoundDispatcher` class in `src/Encina.Messaging/Sagas/SagaNotFoundDispatcher.cs` (28 lines) and the `IHandleSagaNotFound` interface in `src/Encina.Messaging/Sagas/IHandleSagaNotFound.cs` currently have 0% unit and 0% guard clause coverage. This was re-confirmed by a fresh `dotnet test tests/Encina.UnitTests|Encina.GuardTests --filter "FullyQualifiedName~Sagas" --collect "XPlat Code Coverage"` run, where `line-rate="0"` was reported for both the constructor and `DispatchAsync` methods in `artifacts/audit/coverage/unit-recheck/coverage.cobertura.xml` and the corresponding guard coverage report.

`SagaNotFoundDispatcher` is a DI-resolved production class responsible for handling saga-not-found scenarios, including cancellation handling, exception handling, and producing four distinct logged/returned outcomes: `no handler`, `success`, `cancelled`, and `failed`. Although `.github/coverage-manifest/Encina.Messaging.json` explicitly requires unit and guard coverage for these components, no tests exist in the current worktree (branch `audit/16`) to exercise these branches. Previous verifier claims of coverage were traced to DI-registration smoke checks on a newer branch state that are not ancestors of the audited worktree and do not test dispatch behavior.

## Packages / Providers Affected

- **Package(s)**: Encina.Messaging
- **Provider(s)**: N/A (Core Messaging / Sagas)

## Current Coverage

> Fill in if this is a coverage gap issue.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Encina.Messaging | 0% | 85% | -85% |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Test 1: Verify that `SagaNotFoundDispatcher.DispatchAsync` returns the `no handler` outcome when no `IHandleSagaNotFound` implementation is registered.
- [ ] Test 2: Verify that `SagaNotFoundDispatcher.DispatchAsync` returns the `success` outcome and logs correctly when the registered handler completes successfully.
- [ ] Test 3: Verify that `SagaNotFoundDispatcher.DispatchAsync` returns the `cancelled` outcome and logs correctly when the operation is cancelled via `CancellationToken`.
- [ ] Test 4: Verify that `SagaNotFoundDispatcher.DispatchAsync` returns the `failed` outcome and logs correctly when the registered handler throws an exception.
- [ ] Test 5: Implement Guard Clause tests for `SagaNotFoundDispatcher` constructor to verify null handling for required dependencies.
- [ ] Test 6: Implement Guard Clause tests for `DispatchAsync` to verify null handling for the saga message and token parameters.

### Success Criteria

- [x] All new tests pass
- [x] Coverage meets ≥85% target (if coverage gap)
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

> Per `AGENTS.md` §9 (Testing obligations): Integration tests MUST use shared `[Collection]` fixtures.

- **Collection**: N/A (Unit/Guard Tests)
- **Fixture**: N/A

## Related Issues

- #16 - Finding 1: 0% unit and guard coverage for SagaNotFoundDispatcher and IHandleSagaNotFound