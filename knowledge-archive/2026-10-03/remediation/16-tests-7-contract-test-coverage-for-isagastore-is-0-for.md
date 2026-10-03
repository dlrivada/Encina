<!--
title: [TEST] Contract-test coverage for ISagaStore is 0% for 7 of 8 non-EF implementations
labels: area-testing
milestone: 
-->

## Test Category

- [ ] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [x] Contract Tests
- [ ] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

Contract-test coverage for `ISagaStore` is 0% for 7 of its 8 non-fake, non-interface implementations. Specifically, `Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Sagas/SagaStoreADO.cs`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/Sagas/SagaStoreDapper.cs`, and `Encina.MongoDB/Sagas/SagaStoreMongoDB.cs` all show 0 covered lines under `Encina.ContractTests`. Only `Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs` (76.1%) has any contract-test exercise, via `SagaContractTests.cs`/`StoreImplementationContractTests.cs`, both EF-Core-only.

`ISagaStore` is a provider-dependent database feature (AGENTS.md §9: contract tests required for database features; §5: provider-dependent features must be implemented and — by extension of the same coherence principle — tested consistently across all 10 providers). This is exactly the kind of provider-coherence gap that let finding 5 (MongoDB) and finding 4 (ADO.NET) go undetected: no contract test enforces that all 8 non-EF stores honor the same `GetStuckSagasAsync`/`OpenConnectionAsync`-adjacent behavior as the one provider (EF) that is tested.

**Location / Evidence**:
- `Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs`
- `Encina.ADO.PostgreSQL/Sagas/SagaStoreADO.cs`
- `Encina.ADO.MySQL/Sagas/SagaStoreADO.cs`
- `Encina.Dapper.SqlServer/Sagas/SagaStoreDapper.cs`
- `Encina.Dapper.PostgreSQL/Sagas/SagaStoreDapper.cs`
- `Encina.Dapper.MySQL/Sagas/SagaStoreDapper.cs`
- `Encina.MongoDB/Sagas/SagaStoreMongoDB.cs`

## Packages / Providers Affected

- **Package(s)**: Encina.ADO.SqlServer, Encina.ADO.PostgreSQL, Encina.ADO.MySQL, Encina.Dapper.SqlServer, Encina.Dapper.PostgreSQL, Encina.Dapper.MySQL, Encina.MongoDB
- **Provider(s)**: ADO-SqlServer, ADO-PostgreSQL, ADO-MySQL, Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL, MongoDB

## Current Coverage

> Fill in if this is a coverage gap issue.

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Encina.ADO.SqlServer | 0% | 85% | -85% |
| Encina.ADO.PostgreSQL | 0% | 85% | -85% |
| Encina.ADO.MySQL | 0% | 85% | -85% |
| Encina.Dapper.SqlServer | 0% | 85% | -85% |
| Encina.Dapper.PostgreSQL | 0% | 85% | -85% |
| Encina.Dapper.MySQL | 0% | 85% | -85% |
| Encina.MongoDB | 0% | 85% | -85% |
| Encina.EntityFrameworkCore | 76.1% | 85% | -8.9% |

## Infrastructure Required

- [x] Docker / Testcontainers
- [x] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [ ] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Test 1: Extend `Encina.ContractTests` to include `Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs` implementations and verify 100% line coverage.
- [ ] Test 2: Extend `Encina.ContractTests` to include `Encina.ADO.PostgreSQL/Sagas/SagaStoreADO.cs` implementations and verify 100% line coverage.
- [ ] Test 3: Extend `Encina.ContractTests` to include `Encina.ADO.MySQL/Sagas/SagaStoreADO.cs` implementations and verify 100% line coverage.
- [ ] Test 4: Extend `Encina.ContractTests` to include `Encina.Dapper.SqlServer/Sagas/SagaStoreDapper.cs` implementations and verify 100% line coverage.
- [ ] Test 5: Extend `Encina.ContractTests` to include `Encina.Dapper.PostgreSQL/Sagas/SagaStoreDapper.cs` implementations and verify 100% line coverage.
- [ ] Test 6: Extend `Encina.ContractTests` to include `Encina.Dapper.MySQL/Sagas/SagaStoreDapper.cs` implementations and verify 100% line coverage.
- [ ] Test 7: Extend `Encina.ContractTests` to include `Encina.MongoDB/Sagas/SagaStoreMongoDB.cs` implementations and verify 100% line coverage.
- [ ] Test 8: Ensure contract tests enforce that all 8 non-EF stores honor the same `GetStuckSagasAsync`/`OpenConnectionAsync`-adjacent behavior as the EF-Core provider.

### Success Criteria

- [ ] All new tests pass
- [x] Coverage meets ≥85% target (if coverage gap)
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

> Per `AGENTS.md` §9 (Testing obligations): Integration tests MUST use shared `[Collection]` fixtures.

- **Collection**: `ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL`, `Dapper-SqlServer`, `Dapper-PostgreSQL`, `Dapper-MySQL`, `MongoDB`
- **Fixture**: `SqlServerFixture`, `PostgreSqlFixture`, `MySqlFixture`, `SqlServerFixture`, `PostgreSqlFixture`, `MySqlFixture`, `MongoDbFixture`

## Related Issues

- #16 - Closed GitHub issue of the Encina .NET library (tests stage, finding 7, severity Major)