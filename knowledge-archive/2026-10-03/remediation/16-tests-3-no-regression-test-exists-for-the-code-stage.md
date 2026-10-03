<!--
title: [TEST] Add regression tests for EncinaError.Message leakage in InstrumentedSagaStore OpenTelemetry Activity status
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

No regression test exists for the code stage's finding 2 (`InstrumentedSagaStore` leaking `EncinaError.Message` into the OpenTelemetry `Activity` status). `tests/Encina.UnitTests/OpenTelemetry/MessagingStores/InstrumentedSagaStoreTests.cs` contains only `*_DelegatesToInner` tests, all of which stub the inner store to `Right(...)`; none stub a `Left(EncinaError)` and assert what reaches `activity.SetStatus`. The failure branch (`Failed(activity, err.Message)`, `InstrumentedSagaStore.cs:49,59,69,87,104`) has no test coverage at all, so a fix to redact the message would not be verified by any existing test, and the bug itself would not have been caught by CI.

## Packages / Providers Affected

- **Package(s)**: Encina.UnitTests (covering OpenTelemetry instrumentation for SagaStores)
- **Provider(s)**: MessagingStores (SagaStore)

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

- [ ] Test 1: Stub inner store to return Left(EncinaError) and assert activity.SetStatus does not contain EncinaError.Message
- [ ] Test 2: Verify failure branch at InstrumentedSagaStore.cs:49 does not leak sensitive data into Activity status
- [ ] Test 3: Verify failure branch at InstrumentedSagaStore.cs:59 does not leak sensitive data into Activity status
- [ ] Test 4: Verify failure branch at InstrumentedSagaStore.cs:69 does not leak sensitive data into Activity status
- [ ] Test 5: Verify failure branch at InstrumentedSagaStore.cs:87 does not leak sensitive data into Activity status
- [ ] Test 6: Verify failure branch at InstrumentedSagaStore.cs:104 does not leak sensitive data into Activity status

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

- #16 - SPEC-003 audit of closed GitHub issue #16 of the Encina .NET library (tests stage, finding 3, severity Major)