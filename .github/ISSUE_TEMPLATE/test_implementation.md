---
name: Test Implementation
about: Plan new tests, benchmarks, load tests, or address coverage gaps
title: "[TEST] "
labels: area-testing
assignees: ''
---

## Test Category

- [ ] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [ ] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

A clear description of the testing work needed.

## Packages / Providers Affected

- **Package(s)**: [e.g., Encina.Dapper.SqlServer, Encina.ADO.PostgreSQL]
- **Provider(s)**: [e.g., ADO-SqlServer, Dapper-PostgreSQL, EFCore-MySQL, MongoDB]

## Current Coverage

> Fill in if this is a coverage gap issue.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Example.Package | 62.3% | 85% | -22.7% |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQLite / SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [ ] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Test 1: Description
- [ ] Test 2: Description

### Success Criteria

- [ ] All new tests pass
- [ ] Coverage meets ≥85% target (if coverage gap)
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

> Per CLAUDE.md: Integration tests MUST use shared `[Collection]` fixtures.

- **Collection**: [e.g., `ADO-PostgreSQL`, `Dapper-SqlServer`, `EFCore-MySQL`]
- **Fixture**: [e.g., `PostgreSqlFixture`, `SqlServerFixture`]

## Related Issues

- #___ - Description
