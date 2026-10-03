<!--
title: [TEST] Four contract test files list SqlServer types twice after SQLite was removed, leaving 22 duplicated pairs and comparisons that cannot fail
labels: area-testing
milestone: 
kind: test
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

In four contract test files the same SqlServer type or namespace is listed twice, 22 duplicated pairs in all. In three files two adjacent variables hold the identical `typeof(<SqlServer type>)` expression (18 pairs). The first variable is still named after the provider that was removed (`adoSqliteType`, `dapperSqliteType`) or differs from its neighbour only by case (`SqlServerType` next to `sqlServerType`), so the SQLite slot (ADR-024) was re-pointed at SqlServer instead of being deleted. The fourth file has the same leftover in array-literal form (4 pairs).

| File | Duplicate pairs (lines) |
|---|---|
| `tests/Encina.ContractTests/Database/Pagination/CursorPaginationHelperContractTests.cs` | `:24-25`, `:39-40`, `:54-55`, `:69-70`, `:84-85`, `:103-104`, `:118-119`, `:137-138` |
| `tests/Encina.ContractTests/Database/UnitOfWork/UnitOfWorkContractTests.cs` | `:70-71`, `:85-86`, `:281-282`, `:295-296` |
| `tests/Encina.ContractTests/Database/Repository/RepositoryContractTests.cs` | `:29-30`, `:54-55`, `:83-84`, `:107-108`, `:359-360`, `:381-382` |
| `tests/Encina.ContractTests/Database/Sharding/ShardingServiceRegistrationContractTests.cs` | array entries in `Contract_AllProviders_HaveShardingNamespace` (`:80`): `expectedNamespaces` `:84-85` and `:88-89`, `repoTypes` `:98-99` and `:102-103` |

The assertions that compare a variable pair compare SqlServer with itself and cannot fail: `VerifyPublicMethodsMatch(adoSqliteType, adoSqlServerType, "ADO.SqlServer")` at `UnitOfWorkContractTests.cs:76`, `VerifyInterfaceMembersMatch(adoSqliteType, adoSqlServerType, "ADO.SqlServer")` at `RepositoryContractTests.cs:37`, and `VerifyMethodExists(sqlServerType, "ExecuteAsync", "SQL Server")` at `CursorPaginationHelperContractTests.cs:60`, whose "SQL Server" label names the same provider as the "SqlServer" label on the line above it (`:59`). In the sharding test the loop at `:110-114` checks the SqlServer ADO and the SqlServer Dapper namespace twice, and the two arrays hold 10 entries for 8 distinct providers. The PostgreSQL and MySQL comparisons in the same tests are real, so the contract is not empty, but each affected test carries dead assertions and a provider count that is one too high.

The same leftover shows in repeated assertions and provider lists of `UnitOfWorkContractTests.cs`: `:28-31` and `:42-45` assert `IsAssignableFrom` twice for the same type (`UnitOfWorkADO` of SqlServer and `UnitOfWorkDapper` of SqlServer), and the provider arrays at `:116-117`, `:151-152`, `:190-191`, `:194-195`, `:219-220`, `:223-224`, `:248-249` and `:252-253` list the same `typeof(...)` twice.

This is the `[Fact]` counterpart of #1725, which covers only the `[Theory]` rows that xUnit discards as duplicates; #1725 does not list these four files.

## Packages / Providers Affected

- **Package(s)**: None (test code only: `Encina.ContractTests`)
- **Provider(s)**: ADO-SqlServer and Dapper-SqlServer (the repeated types); the pagination helper file covers the ADO providers only

## Current Coverage

No coverage figure applies to this item. The repeated assertions add no coverage: each one repeats the call that precedes it on the same type.

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] In the three files with variable pairs, delete the duplicate variable of each pair and its assertions, and make the remaining SqlServer variable the reference the PostgreSQL and MySQL types are compared with (for example `adoSqlServerType` compared with `adoPostgresType` and `adoMySQLType`).
- [ ] In `ShardingServiceRegistrationContractTests.cs`, delete the repeated entries of `expectedNamespaces` (`:85`, `:89`) and `repoTypes` (`:99`, `:103`) so each array holds the 8 distinct providers.
- [ ] In `UnitOfWorkContractTests.cs`, delete the repeated `IsAssignableFrom` assertions (`:30-31`, `:44-45`) and the repeated array entries listed above.
- [ ] Correct the provider counts in the comments and messages that still say four providers where three remain (`CursorPaginationHelperContractTests.cs:23` says "all 4 ADO.NET providers").
- [ ] Rename `SqlServerType`/`sqlServerType` in `CursorPaginationHelperContractTests.cs` so one SqlServer variable is left, with one spelling in the assertion labels.

### Success Criteria

- [x] All new tests pass
- [ ] Coverage meets ≥85% target (if coverage gap)
- [x] No flaky tests introduced
- [x] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: these are contract tests with no database or container.

## Related Issues

- #29 (This issue)
- #1725 - the `[Theory]` counterpart (duplicate rows dropped by xUnit); does not list these files
