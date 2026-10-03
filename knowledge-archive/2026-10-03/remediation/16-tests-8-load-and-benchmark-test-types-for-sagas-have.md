<!--
title: [TEST] Load and Benchmark tests for Sagas missing for 9 of 10 database providers
labels: area-testing
milestone: 
-->

## Test Category

- [ ] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [ ] Guard Clause Tests
- [x] Load Tests (NBomber)
- [x] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

Load and Benchmark test types for Sagas have neither `.cs` tests nor a `.md` justification for 9 of 10 database providers (`SagaStoreEFBenchmarks.cs` is the only implementation; no justification file exists for ADO.NET ×3, Dapper ×3, MongoDB, or for any Load test). Per AGENTS.md §9 this means "the coverage was not evaluated," not that it passes or is exempt — it should be evaluated and either implemented or justified in a future test-implementation issue, not silently left open.

## Packages / Providers Affected

- **Package(s)**: ADO.NET, Dapper, MongoDB
- **Provider(s)**: ADO.NET ×3, Dapper ×3, MongoDB

## Current Coverage

> Fill in if this is a coverage gap issue.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| N/A | N/A | N/A | N/A |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [x] NBomber load testing framework
- [x] BenchmarkDotNet
- [ ] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Evaluate and implement Load tests for ADO.NET ×3, Dapper ×3, MongoDB, or provide a .md justification
- [ ] Evaluate and implement Benchmark tests for ADO.NET ×3, Dapper ×3, MongoDB, or provide a .md justification

### Success Criteria

- [ ] All new tests pass
- [ ] Coverage meets ≥85% target (if coverage gap)
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

> Per `AGENTS.md` §9 (Testing obligations): Integration tests MUST use shared `[Collection]` fixtures.

- **Collection**: N/A
- **Fixture**: N/A

## Related Issues

- #16 - SPEC-003 audit of closed GitHub issue #16