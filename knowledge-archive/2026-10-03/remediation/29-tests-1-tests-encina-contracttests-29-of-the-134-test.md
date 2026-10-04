<!--
title: [TEST] 29 of the 134 test files of Encina.ContractTests assert type shape by reflection only and execute no product code
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

29 of the 134 test files of `tests/Encina.ContractTests` that contain tests (419 of the 1527 `[Fact]`/`[Theory]` attributes in the project) execute no product code. They assert interface or class shape by reflection only (`typeof(...)`, `IsAssignableFrom`, `GetMethod`, `GetMethods`, `GetParameters`), with no `new`, factory call, DI build or method call. AGENTS.md section 9 says "Tests MUST execute real package code ... contract and property tests MUST instantiate real implementations" and that reflection-only tests "cover zero lines".

Six files were read and confirmed reflection-only:

- `Database/Sharding/ShardingServiceRegistrationContractTests.cs:26-139` (11 attributes): `VerifyExtensionClassExists` at `:145` only calls `Assembly.GetTypes()` and asserts `IsAbstract`/`IsSealed`; the other tests assert `typeof(...).Namespace` and `.IsPublic`.
- `Database/UnitOfWork/UnitOfWorkContractTests.cs:24-60`: `typeof(IUnitOfWork).IsAssignableFrom(typeof(UnitOfWorkADO))` and the same for each other implementation.
- `Security/ABAC/PersistentPAPContractTests.cs:23-88`: `typeof(IPolicyAdministrationPoint).GetMethods(...)`, return and parameter types.
- `Compliance/BreachNotification/IBreachNotifierContractTests.cs:19-36`: `InterfaceType.GetMethod("NotifyAuthorityAsync")` and `"NotifyDataSubjectsAsync"`, three tests, no implementation.
- `Database/Pagination/CursorPaginationHelperContractTests.cs:21-127`: type existence, method names, constructor parameter counts.
- `Database/ReadWriteSeparation/ReadWriteSeparationContractTests.cs:38-60`: `IsAssignableFrom` for each factory.

The other 23 files were selected by a text heuristic (none of `new`, `new(`, `Create...(`, `await`, `Substitute.For`, `BuildServiceProvider`, `GetRequiredService`, `.Invoke(`, `.Compute(` or `.Validate...(` appears anywhere in the file) and were not read one by one, so each may still turn out to be a false positive (a target-typed `new(...)` or a helper factory hides an instantiation from the heuristic). Test attributes per file in parentheses:

- `Compliance/BreachNotification/IBreachDetectionRuleContractTests.cs` (3), `IBreachDetectorContractTests.cs` (4), `IBreachNotificationServiceContractTests.cs` (9)
- `Compliance/Consent/IConsentServiceContractTests.cs` (32; already covered by #1316)
- `Compliance/CrossBorderTransfer/IApprovedTransferServiceContractTests.cs` (11), `ISCCServiceContractTests.cs` (12), `ITransferValidatorContractTests.cs` (5)
- `Database/ModuleIsolation/ModuleIsolationContractTests.cs` (15)
- `Database/Repository/UpdateImmutableAsyncContractTests.cs` (6)
- `Database/Resilience/DatabaseHealthMonitorContractTests.cs` (20)
- `Database/Sharding/ShardedConnectionFactoryContractTests.cs` (9), `ShardedReadWriteConnectionFactoryContractTests.cs` (12), `ShardedRepositoryContractTests.cs` (15), `ShardedSpecificationSupportContractTests.cs` (12)
- `Database/UnitOfWork/UpdateImmutableContractTests.cs` (8)
- `EntityFrameworkCore/IMessageSchedulerContractTests.cs` (18), `ITenantSchemaConfiguratorContractTests.cs` (12), `ITransactionalCommandContractTests.cs` (13), `StoreContractTests.cs` (16)
- `MongoDB/MongoDbContractTests.cs` (8)
- `Security/ABAC/Persistence/Xacml/XacmlInfrastructureContractTests.cs` (29)
- `Sharding/ReferenceTables/ReferenceTableStoreContractTests.cs` (34), `Sharding/Resharding/ReshardingContractTests.cs` (17)

All paths are relative to `tests/Encina.ContractTests/`.

## Packages / Providers Affected

- **Package(s)**: test project `tests/Encina.ContractTests`; the product code each file should exercise is the implementation of the interface or class it names (for example the unit of work, sharding, pagination, ABAC policy administration, breach notification and reference table store types in the files above).
- **Provider(s)**: not provider-specific as a whole; the database files compare ADO.NET, Dapper, EF Core and MongoDB implementations of the same contract.

## Current Coverage

Not measured. No contract-flag coverage run was done for any package, so it is not known whether the contract flag misses its manifest target in `.github/coverage-manifest/` because of these files. The project itself builds without warnings in Release and runs 1871 tests, 0 failed, 0 skipped (`dotnet test tests\Encina.ContractTests -c Release`, 2026-10-03).

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

Per file, one of two outcomes:

- [ ] Instantiate the implementation (or resolve it from a built provider) and call the contract members, asserting behaviour, so the test executes package code.
- [ ] Where the interface-only shape is itself the contract, keep the reflection test only next to a behavioural one, and say so in the test class summary.

Order of work: first read the 23 files that were not read, and drop from the list any that already instantiate real code; then convert the six confirmed files; `IConsentServiceContractTests.cs` is handled by #1316.

### Success Criteria

- [ ] All new tests pass
- [ ] Every file of the final list executes product code from the package it names, or states why the shape is the contract
- [ ] Contract coverage for the affected packages is measured against its manifest target after the change
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: contract tests, no database fixture.

## Related Issues

- #29 (This issue)
- #1316 - covers only `Encina.Compliance.Consent`
