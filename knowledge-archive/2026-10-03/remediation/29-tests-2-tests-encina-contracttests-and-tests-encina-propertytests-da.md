<!--
title: [TEST] 105 adjacent duplicated SqlServer lines in 16 contract and property test files, left over from the SQLite removal
labels: area-testing
milestone:
kind: test
-->

## Test Category

- [ ] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [x] Property-Based Tests (FsCheck)
- [x] Contract Tests
- [ ] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

`tests/Encina.ContractTests` and `tests/Encina.PropertyTests/Database/Tenancy/TenancyOptionsPropertyTests.cs` hold 105 pairs of adjacent identical lines in 16 test files. In each pair two consecutive lines contain the same SqlServer expression: a `typeof(<SqlServer type>)`, a quoted `"Encina.<ADO|Dapper>.SqlServer..."` namespace, a `new <SqlServer type>(...)` or a `.Name.ShouldBe(...)` assertion. The first line of each pair stands where the SQLite provider was before ADR-024 removed it: variables are still named `adoSqliteType`, `dapperSqliteType`, `adoSqlite`, `dapperSqlite`, `adoSqliteConstraints`, `dapperSqliteConstraints`, or `SqlServerType` next to `sqlServerType`, and comments still say "ADO.NET providers: SqlServer, SqlServer, PostgreSQL, MySQL" (`ModuleIsolationContractTests.cs:287`, `:305`) or "ADO.NET (4)" (`ProcessingActivityRegistryProviderContractTests.cs:30`).

Effects:

- A comparison such as `VerifyPublicMethodsMatch(adoSqliteType, adoSqlServerType, "ADO.SqlServer")` (`UnitOfWorkContractTests.cs:76`, both variables are `typeof(ADOSqlServerUoW.UnitOfWorkADO)` at `:70-71`) or `VerifyInterfaceMembersMatch(adoSqliteType, adoSqlServerType, ...)` (`RepositoryContractTests.cs:37`) compares SqlServer with itself and cannot fail.
- Arrays list SqlServer twice: in `AllADOProviders_ShouldHaveModuleIsolationSupport` the array at `ModuleIsolationContractTests.cs:288-294` has four entries for three distinct providers, and `Assert.Equal(4, adoProviderTypes.Length)` (`:296`) pins the duplicate; the Dapper twin (`:306-312`, `Assert.Equal(4, dapperProviderTypes.Length)` at `:314`) does the same.
- `UnitOfWorkContractTests.cs:28-31` and `:42-45` also repeat the same `IsAssignableFrom` assertion twice for one type.

The PostgreSQL and MySQL comparisons in the same tests remain real, so the contracts are not vacuous; each affected test carries dead assertions and a wrong provider count.

Complete list of the 105 pairs (paths under `tests/Encina.ContractTests/` unless stated; each pair is lines N and N+1):

- `Compliance/GDPR/ProcessingActivityRegistryProviderContractTests.cs`, 6: 31-32, 36-37, 67-68, 84-85, 140-141, 144-145.
- `Database/ModuleIsolation/ModuleIsolationContractTests.cs`, 11: 105-106, 125-126, 141-142, 161-162, 165-166, 207-208, 211-212, 265-266, 269-270 (these 9 are in #1725), and 290-291, 308-309 (the two `All*Providers_ShouldHaveModuleIsolationSupport` facts, not in #1725).
- `Database/Pagination/CursorPaginationHelperContractTests.cs`, 8: 24-25, 39-40, 54-55, 69-70, 84-85, 103-104, 118-119, 137-138.
- `Database/ReadWriteSeparation/ReadWriteSeparationContractTests.cs`, 16: 118-119, 122-123, 138-139, 142-143, 158-159, 162-163, 178-179, 182-183, 198-199, 202-203, 276-277, 280-281, 299-300, 303-304, 314-315, 318-319 (all in #1725).
- `Database/Repository/RepositoryContractTests.cs`, 11: 29-30, 54-55, 83-84, 107-108, 132-133, 137-138, 331-332, 359-360, 381-382, 456-457, 485-486.
- `Database/Repository/UpdateImmutableAsyncContractTests.cs`, 4: 109-110, 124-125, 157-158, 162-163.
- `Database/Resilience/DatabaseHealthMonitorContractTests.cs`, 2: 186-187, 209-210.
- `Database/Sharding/ShardedConnectionFactoryContractTests.cs`, 2: 98-99, 117-118.
- `Database/Sharding/ShardedReadWriteConnectionFactoryContractTests.cs`, 3: 101-102, 146-147, 150-151.
- `Database/Sharding/ShardedRepositoryContractTests.cs`, 4: 138-139, 142-143, 163-164, 172-173.
- `Database/Sharding/ShardingServiceRegistrationContractTests.cs`, 4: 84-85, 88-89 (the `expectedNamespaces` array of `Contract_AllProviders_HaveShardingNamespace`, `:80`), 98-99, 102-103 (the `repoTypes` array).
- `Database/UnitOfWork/UnitOfWorkContractTests.cs`, 12: 70-71, 85-86, 116-117, 151-152, 190-191, 194-195, 219-220, 223-224, 248-249, 252-253, 281-282, 295-296.
- `Database/UnitOfWork/UpdateImmutableContractTests.cs`, 4: 173-174, 189-190, 208-209, 213-214.
- `Security/Audit/ReadAudit/ReadAuditContractTests.cs`, 2: 30-31, 34-35 (in #1725).
- `Sharding/ReferenceTables/ReferenceTableStoreContractTests.cs`, 14: 298-299, 330-331, 358-359, 362-363, 382-383, 386-387, 408-409, 417-418, 426-427, 435-436, 466-467, 470-471, 497-498, 501-502.
- `tests/Encina.PropertyTests/Database/Tenancy/TenancyOptionsPropertyTests.cs`, 2: 144-145 (`ADOSqlServer` and `adoSqlServer`, both `new ADOSqlServerTenancy.ADOTenancyOptions()`), 148-149 (`DapperSqlServer` and `dapperSqlServer`).

Total 6+11+8+16+11+4+2+2+3+4+4+12+4+2+14+2 = 105 pairs in 16 files (15 in ContractTests, 1 in PropertyTests). The list comes from a scan of all 3474 `.cs` files under `tests\` (excluding `bin` and `obj`) that compares every line with the next after trimming, removing a leading declaration prefix and a trailing `,` or `;`; a pair is a hit when the two results are equal, at least 15 characters long, contain `SqlServer` and match `typeof\(|"[^"]*SqlServer|new\s|\w+\.\w+SqlServer`. Seven further hits in five files were read and excluded because the repetition is deliberate (equality or idempotency tests that build two equal objects or register twice: `CdcProviderPositionContractTests.cs:321-322`, `SqlServerCdcPositionPropertyTests.cs:57-58`, `SqlServerCdcPositionTests.cs:156-157`, `ServiceCollectionExtensionsTests.cs:133-134` for Cdc.SqlServer, and `ShardingServiceCollectionExtensionsTests.cs:441-442`, `:457-458`, `:473-474`). The scan finds only adjacent identical lines holding `SqlServer`; non-adjacent repetition, a duplicated PostgreSQL or MySQL line and a pair split across lines are not found. The first pair of each file was read in context, and the first four pairs of the sharded repository, sharded connection factory, sharded read/write connection factory and reference table store files; the rest were not read line by line, but their normalized text has the shape of the pairs that were read.

## Packages / Providers Affected

- **Package(s)**: test projects `tests/Encina.ContractTests` and `tests/Encina.PropertyTests`.
- **Provider(s)**: ADO-SqlServer and Dapper-SqlServer (the duplicated side of every pair); the ADO and Dapper PostgreSQL and MySQL comparisons sit next to them.

## Current Coverage

Not measured. The fix removes duplicate lines and does not change which product code the tests execute.

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] In each of the 105 pairs delete the duplicate line.
- [ ] Rename the reference variable that still carries the SQLite name to the first real provider (`adoSqlServerType`, `dapperSqlServerType`, and the equivalents `adoSqlServer`, `dapperSqlServer`, `adoSqlServerConstraints`, `dapperSqlServerConstraints`).
- [ ] Update the provider counts in comments (`ModuleIsolationContractTests.cs:287`, `:305`; `ProcessingActivityRegistryProviderContractTests.cs:30`) and the assertions that pin them (`ModuleIsolationContractTests.cs:296`, `:314`).
- [ ] Remove the repeated `IsAssignableFrom` assertions at `UnitOfWorkContractTests.cs:28-31` and `:42-45`.
- [ ] Rerun the scan over `tests\` and confirm that no adjacent identical SqlServer pair is left outside the excluded deliberate ones.

### Success Criteria

- [ ] All new tests pass
- [ ] No comparison in these files compares a SqlServer type with itself
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: contract and property tests, no database fixture.

## Related Issues

- #29 (This issue)
- #1725 - removes the duplicate theory rows in `ReadWriteSeparationContractTests.cs`, `ModuleIsolationContractTests.cs` and `ReadAuditContractTests.cs`, which are 27 of the 105 pairs (16 + 9 + 2); its fix and this one touch the same files and should be done together. The other 78 pairs are `[Fact]` bodies or arrays that a `foreach` walks, not theory rows, so xUnit does not report them as discarded cases.
