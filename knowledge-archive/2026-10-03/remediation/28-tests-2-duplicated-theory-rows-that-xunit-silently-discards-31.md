<!--
title: [TEST] Contract theories list SqlServer ADO and Dapper rows twice, so xUnit silently discards 31 cases
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

Three contract test classes list the `SqlServer` ADO row and the `SqlServer` Dapper row twice in their `[Theory]` data. xUnit skips a test case whose ID duplicates an earlier one ("Skipping test case with duplicate ID" at discovery), so 31 cases are dropped and the run still reports green because nothing flags them.

| File | Dropped cases | Duplicated rows |
|---|:-:|---|
| `tests/Encina.ContractTests/Database/ReadWriteSeparation/ReadWriteSeparationContractTests.cs` | 16 | `ADOSqlServerRW` and `DapperSqlServerRW` rows in each of eight `[Theory]` blocks: `:118-119` and `:122-123`, `:138-139` and `:142-143`, `:158-159` and `:162-163`, `:178-179` and `:182-183`, `:198-199` and `:202-203`, `:276-277` and `:280-281`, `:299-300` and `:303-304`, `:314-315` and `:318-319` |
| `tests/Encina.ContractTests/Database/ModuleIsolation/ModuleIsolationContractTests.cs` | 9 | `:105-106`, `:125-126`, `:141-142`, `:161-162`, `:165-166`, `:207-208`, `:211-212`, `:265-266`, `:269-270` |
| `tests/Encina.ContractTests/Security/Audit/ReadAudit/ReadAuditContractTests.cs` | 6 | `AllProviderTypes` at `:30-31` and `:34-35` (`ADOSqlServerStore` and `DapperSqlServerStore` twice), feeding the three `[MemberData(nameof(ProviderTypes))]` theories at `:228`, `:240` and `:249` |

The doubled line stands where a fourth ADO and a fourth Dapper provider presumably were before a provider was removed. The XML comment at `ReadAuditContractTests.cs:25` still counts "4 ADO + 4 Dapper + 1 EF + 1 MongoDB" while the array at `:28-41` lists only three distinct ADO and three distinct Dapper providers (SqlServer, PostgreSQL, MySQL), which matches the provider matrix of AGENTS.md section 5. The repeated row therefore stands in for a provider the test no longer exercises.

## Packages / Providers Affected

- **Package(s)**: None (test code only: `Encina.ContractTests`)
- **Provider(s)**: ADO-SqlServer and Dapper-SqlServer (the duplicated rows)

## Current Coverage

No coverage figure applies to this item. The duplicated rows add no coverage: each dropped case is identical to the case kept before it.

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Delete the duplicate rows in the three files above (or, where a provider is genuinely missing from a matrix, replace the repeated row with the missing provider).
- [ ] Correct the provider count in the XML comment at `ReadAuditContractTests.cs:25` (and confirm the "All 10" total there) against the entries the array lists.
- [ ] Add a check that fails when test discovery reports a duplicate test case ID, so a repeated theory row cannot be dropped silently again.

### Success Criteria

- [x] All new tests pass
- [ ] Coverage meets ≥85% target (if coverage gap)
- [x] No flaky tests introduced
- [x] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: these are contract tests with no database or container.

## Related Issues

- #28 (This issue)
