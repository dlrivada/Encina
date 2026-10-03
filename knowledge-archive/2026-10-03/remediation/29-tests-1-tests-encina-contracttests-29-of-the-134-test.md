<!--
title: [TEST] 29 of the 134 Encina.ContractTests files assert type shape by reflection only and execute no product code
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

In `tests/Encina.ContractTests`, 29 of the 134 test files that hold tests (419 of the 1527 `[Fact]`/`[Theory]` attributes in the project) execute no product code. They assert the shape of interfaces and classes by reflection only (`typeof(...)`, `IsAssignableFrom`, `GetMethod`, `GetMethods`, `GetParameters`), with no `new`, factory call, DI build or method call. AGENTS.md section 9 requires that tests execute real package code and that "contract and property tests MUST instantiate real implementations", and it states that reflection-only tests cover zero lines.

Six of the 29 files were read and confirmed reflection-only:

| File | Lines | What it asserts |
|---|---|---|
| `tests/Encina.ContractTests/Database/Sharding/ShardingServiceRegistrationContractTests.cs` | `:26-139` (11 attributes) | `typeof(...).Namespace` and `.IsPublic` for the sharding types; the helper `VerifyExtensionClassExists` (`:145`) only runs `Assembly.GetTypes()` and asserts `IsAbstract` and `IsSealed` of the extension class |
| `tests/Encina.ContractTests/Database/UnitOfWork/UnitOfWorkContractTests.cs` | `:24-60` | `typeof(IUnitOfWork).IsAssignableFrom(typeof(UnitOfWorkADO))` and the same for the Dapper, EF Core and MongoDB unit-of-work types |
| `tests/Encina.ContractTests/Security/ABAC/PersistentPAPContractTests.cs` | `:22-88` | `typeof(IPolicyAdministrationPoint).GetMethods(...)`: method count, return types, `CancellationToken` as last parameter |
| `tests/Encina.ContractTests/Compliance/BreachNotification/IBreachNotifierContractTests.cs` | `:18-36` | `InterfaceType.IsInterface` and `GetMethod("NotifyAuthorityAsync")` / `GetMethod("NotifyDataSubjectsAsync")` are not null; no implementation is involved |
| `tests/Encina.ContractTests/Database/Pagination/CursorPaginationHelperContractTests.cs` | `:20-145` | type existence, method names, constructor parameter counts, namespace |
| `tests/Encina.ContractTests/Database/ReadWriteSeparation/ReadWriteSeparationContractTests.cs` | `:38-64` | `IReadWriteConnectionFactory.IsAssignableFrom(ReadWriteConnectionFactory)` for each provider |

The other 23 files were selected by a text heuristic and not read one by one: no `new`, `new(`, `Create...(`, `await`, `Substitute.For`, `BuildServiceProvider`, `GetRequiredService`, `.Invoke(`, `.Compute(` or `.Validate...(` anywhere in the file. Files that instantiate real code through target-typed `new(...)` or a `Create...` helper match a narrower marker set but not this one, so a false positive among the 23 is still possible. They are (test attributes in parentheses):

- `Compliance/BreachNotification/`: `IBreachDetectionRuleContractTests.cs` (3), `IBreachDetectorContractTests.cs` (4), `IBreachNotificationServiceContractTests.cs` (9)
- `Compliance/Consent/IConsentServiceContractTests.cs` (32; covered by #1316)
- `Compliance/CrossBorderTransfer/`: `IApprovedTransferServiceContractTests.cs` (11), `ISCCServiceContractTests.cs` (12), `ITransferValidatorContractTests.cs` (5)
- `Database/ModuleIsolation/ModuleIsolationContractTests.cs` (15)
- `Database/Repository/UpdateImmutableAsyncContractTests.cs` (6)
- `Database/Resilience/DatabaseHealthMonitorContractTests.cs` (20)
- `Database/Sharding/`: `ShardedConnectionFactoryContractTests.cs` (9), `ShardedReadWriteConnectionFactoryContractTests.cs` (12), `ShardedRepositoryContractTests.cs` (15), `ShardedSpecificationSupportContractTests.cs` (12)
- `Database/UnitOfWork/UpdateImmutableContractTests.cs` (8)
- `EntityFrameworkCore/`: `IMessageSchedulerContractTests.cs` (18), `ITenantSchemaConfiguratorContractTests.cs` (12), `ITransactionalCommandContractTests.cs` (13), `StoreContractTests.cs` (16)
- `MongoDB/MongoDbContractTests.cs` (8)
- `Security/ABAC/Persistence/Xacml/XacmlInfrastructureContractTests.cs` (29)
- `Sharding/ReferenceTables/ReferenceTableStoreContractTests.cs` (34)
- `Sharding/Resharding/ReshardingContractTests.cs` (17)

For contrast, contract tests in the same project that do execute real code: `Messaging/Scheduling/ExponentialBackoffRetryPolicyContractTests.cs:15-16` (`new ExponentialBackoffRetryPolicy(new SchedulingOptions { ... })`, `Compute(...)` called at `:22`, `:32`, `:42`, `:53-55`), `IdGeneration/IdGeneratorContractTests.cs:93-98` (`new SnowflakeIdGenerator`, `new UlidIdGenerator`, `new UuidV7IdGenerator`, `new ShardPrefixedIdGenerator`) and `Marten/Core/IAggregateRepositoryContractTests.cs:122-127` (`new MartenAggregateRepository<ContractTestAggregate>(...)` over substitutes).

## Packages / Providers Affected

- **Package(s)**: `Encina.ContractTests` (test project only; the files exercise types of the packages named by their folders: sharding, unit of work, ABAC persistence, breach notification, pagination, read/write separation and others)
- **Provider(s)**: ADO-SqlServer, ADO-PostgreSQL, ADO-MySQL, Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL, EFCore, MongoDB (the sharding, unit-of-work and read/write separation files reference these types)

## Current Coverage

Not measured. No contract-flag coverage run was done for any package, so whether the contract flag misses its manifest target because of these files is not established. The whole `Encina.ContractTests` project builds in Release with no warnings and passes in full (1871 passed, 0 failed, 0 skipped; Release configuration, measured by running `dotnet test tests\Encina.ContractTests -c Release` on 2026-10-03), which shows the shape-only tests pass but says nothing about the product lines they execute.

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Read the 23 files not yet read and confirm or drop each one as reflection-only.
- [ ] For each confirmed file, instantiate the implementation (or resolve it from a built provider with `ValidateOnBuild` and `ValidateScopes`) and call the contract members, asserting the `Either` result or behaviour the contract promises, for example registering the sharding services in `ShardingServiceRegistrationContractTests` and resolving them, and exercising a real `IBreachNotifier` implementation in `IBreachNotifierContractTests`.
- [ ] Where the interface shape alone is the intended contract, keep the reflection assertions only next to at least one behavioural test of the same type, and state that choice in the class summary.
- [ ] Run the contract flag coverage for the affected packages before and after, and record the result against each package's `.github/coverage-manifest/{Package}.json` target.

### Success Criteria

- [x] All new tests pass
- [ ] Coverage meets ≥85% target (if coverage gap)
- [x] No flaky tests introduced
- [x] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

Not applicable: these are contract tests with no database or container.

## Related Issues

- #29 (This issue)
- #1316 - covers only `Encina.Compliance.Consent`; no issue covers the project as a whole
