<!--
title: [TEST] Add integration test for SagaStoreADO opening a closed connection
labels: area-testing
milestone: 
-->

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

No regression test exists for the code stage's finding 4 (`SagaStoreADO.OpenConnectionAsync` never calls `.Open()`/`.OpenAsync()`). The existing ADO integration tests in `tests/Encina.IntegrationTests/ADO/{SqlServer,PostgreSQL,MySQL}/Sagas/SagaStoreADOTests.cs:29` construct `SagaStoreADO` using `_fixture.CreateConnection()`. This fixture method calls the connection's synchronous `.Open()` before returning it, which masks the bug where the store fails to open the connection itself. A new test must construct the store with a closed connection (mirroring the real `AddEncinaADO(connectionString)` factory behavior) to prove the store opens it itself. The broken branch has never executed under any test in the suite.

## Packages / Providers Affected

- **Package(s)**: Encina.ADO.SqlServer, Encina.ADO.PostgreSQL, Encina.ADO.MySQL
- **Provider(s)**: ADO-SqlServer, ADO-PostgreSQL, ADO-MySQL

## Current Coverage

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| N/A | N/A | N/A | N/A |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [ ] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Test 1: Verify SagaStoreADO opens a closed connection provided by a mocked or real factory method (mimicking AddEncinaADO)
- [ ] Test 2: Ensure existing fixture-based tests do not mask the connection opening logic by providing pre-opened connections

### Success Criteria

- [ ] All new tests pass
- [ ] Coverage meets ≥85% target (if coverage gap)
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

- **Collection**: ADO-SqlServer, ADO-PostgreSQL, ADO-MySQL
- **Fixture**: SqlServerFixture, PostgreSqlFixture, MySqlFixture

## Related Issues

- #16 - Closed issue regarding SagaStoreADO connection handling