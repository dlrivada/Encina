# Plan: EF Core Multi-Provider Testing Expansion

> **Status**: ✅ Complete
> **Issue**: #539
> **Created**: 2026-01-25
> **Updated**: 2026-01-25

## Objective

Expand EF Core testing from 1 generic provider to 5 database providers (Sqlite, SqlServer, PostgreSQL, MySQL, Oracle) for all EF Core features to ensure LINQ translation and provider-specific behavior works correctly.

## Final State Summary

| Feature | Integration Tests | Status |
|---------|-------------------|--------|
| **Outbox** | 5/5 providers | ✅ Complete |
| **Inbox** | 5/5 providers | ✅ Complete |
| **Saga** | 5/5 providers | ✅ Complete |
| **Scheduling** | 5/5 providers | ✅ Complete |
| **Repository** | 5/5 providers | ✅ Complete |
| **UnitOfWork** | 5/5 providers | ✅ Complete |
| **Health Check** | 5/5 providers | ✅ Complete |
| **BulkOperations** | N/A | ✅ SQL Server only (feature-specific) |

**Note**: MySQL tests use `[SkippableFact]` pending Pomelo.EntityFrameworkCore.MySql v10 release.

---

## Phase 1: Test Infrastructure Setup ✅ COMPLETE

- [x] Task 1.1: Create EF Core Provider-Specific Fixtures
- [x] Task 1.2: Add NuGet Package Dependencies
- [x] Task 1.3: Create Shared Test Base Classes

## Phase 2: Test Reorganization ✅ COMPLETE (Outbox, Inbox, Saga)

- [x] Task 2.1: Unit Tests Restructuring (determined InMemory is provider-agnostic)
- [x] Task 2.2: Guard Tests Restructuring (determined provider-agnostic)
- [x] Task 2.3: Integration Tests Restructuring (Outbox, Inbox, Saga)
- [x] Task 2.4: xUnit Collections

## Phase 3: CI/CD Pipeline Updates ✅ COMPLETE

- [x] Task 3.1: CI Matrix Job for EF Core providers
- [x] Task 3.2: Scripts and Documentation

---

## Phase 4: Complete Missing Provider Tests ✅ COMPLETE

### 4.1 Inbox - Add Missing Providers ✅

**Current**: SqlServer ✅, Sqlite ✅, PostgreSQL ✅, MySQL ✅, Oracle ✅

| Task | Provider | Status |
|------|----------|--------|
| 4.1.1 | Create MySQL/Inbox/InboxStoreEFMySqlTests.cs | ✅ Complete |
| 4.1.2 | Create Oracle/Inbox/InboxStoreEFOracleTests.cs | ✅ Complete |

### 4.2 Saga - Add Missing Providers ✅

**Current**: SqlServer ✅, Sqlite ✅, PostgreSQL ✅, MySQL ✅, Oracle ✅

| Task | Provider | Status |
|------|----------|--------|
| 4.2.1 | Create MySQL/Sagas/SagaStoreEFMySqlTests.cs | ✅ Complete |
| 4.2.2 | Create Oracle/Sagas/SagaStoreEFOracleTests.cs | ✅ Complete |

### 4.3 Scheduling - Add All Providers ✅

**Current**: All 5 providers complete

| Task | Provider | Status |
|------|----------|--------|
| 4.3.1 | Create Sqlite/Scheduling/ScheduledMessageStoreEFSqliteTests.cs | ✅ Complete |
| 4.3.2 | Create SqlServer/Scheduling/ScheduledMessageStoreEFSqlServerTests.cs | ✅ Complete |
| 4.3.3 | Create PostgreSQL/Scheduling/ScheduledMessageStoreEFPostgreSqlTests.cs | ✅ Complete |
| 4.3.4 | Create MySQL/Scheduling/ScheduledMessageStoreEFMySqlTests.cs | ✅ Complete |
| 4.3.5 | Create Oracle/Scheduling/ScheduledMessageStoreEFOracleTests.cs | ✅ Complete |

### 4.4 Repository - Add All Providers ✅

**Current**: All 5 providers complete

| Task | Provider | Status |
|------|----------|--------|
| 4.4.1 | Create Sqlite/Repository/FunctionalRepositoryEFSqliteTests.cs | ✅ Complete |
| 4.4.2 | Create SqlServer/Repository/FunctionalRepositoryEFSqlServerTests.cs | ✅ Complete |
| 4.4.3 | Create PostgreSQL/Repository/FunctionalRepositoryEFPostgreSqlTests.cs | ✅ Complete |
| 4.4.4 | Create MySQL/Repository/FunctionalRepositoryEFMySqlTests.cs | ✅ Complete |
| 4.4.5 | Create Oracle/Repository/FunctionalRepositoryEFOracleTests.cs | ✅ Complete |

### 4.5 UnitOfWork - Add All Providers ✅

**Current**: All 5 providers complete

| Task | Provider | Status |
|------|----------|--------|
| 4.5.1 | Create Sqlite/UnitOfWork/UnitOfWorkEFSqliteTests.cs | ✅ Complete |
| 4.5.2 | Create SqlServer/UnitOfWork/UnitOfWorkEFSqlServerTests.cs | ✅ Complete |
| 4.5.3 | Create PostgreSQL/UnitOfWork/UnitOfWorkEFPostgreSqlTests.cs | ✅ Complete |
| 4.5.4 | Create MySQL/UnitOfWork/UnitOfWorkEFMySqlTests.cs | ✅ Complete |
| 4.5.5 | Create Oracle/UnitOfWork/UnitOfWorkEFOracleTests.cs | ✅ Complete |

### 4.6 Health Check - Add All Providers ✅

**Current**: All 5 providers complete

| Task | Provider | Status |
|------|----------|--------|
| 4.6.1 | Create Sqlite/Health/EntityFrameworkCoreHealthCheckSqliteTests.cs | ✅ Complete |
| 4.6.2 | Create SqlServer/Health/EntityFrameworkCoreHealthCheckSqlServerTests.cs | ✅ Complete |
| 4.6.3 | Create PostgreSQL/Health/EntityFrameworkCoreHealthCheckPostgreSqlTests.cs | ✅ Complete |
| 4.6.4 | Create MySQL/Health/EntityFrameworkCoreHealthCheckMySqlTests.cs | ✅ Complete |
| 4.6.5 | Create Oracle/Health/EntityFrameworkCoreHealthCheckOracleTests.cs | ✅ Complete |

---

## Phase 5: Create Test Base Classes - DEFERRED

Base classes were not needed as each provider test is self-contained and simple enough to not require shared base classes.

---

## Summary Statistics

### Tests Created

| Feature | Tests Created | MySQL Skip |
|---------|---------------|------------|
| Inbox | 2 | Yes |
| Saga | 2 | Yes |
| Scheduling | 5 | Yes |
| Repository | 5 | Yes |
| UnitOfWork | 5 | Yes |
| Health Check | 5 | Yes |
| **Total** | **24** | - |

---

## Files Created

### Phase 4.1 - Inbox

- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/MySQL/Inbox/InboxStoreEFMySqlTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Oracle/Inbox/InboxStoreEFOracleTests.cs`

### Phase 4.2 - Saga

- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/MySQL/Sagas/SagaStoreEFMySqlTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Oracle/Sagas/SagaStoreEFOracleTests.cs`

### Phase 4.3 - Scheduling

- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Sqlite/Scheduling/ScheduledMessageStoreEFSqliteTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/Scheduling/ScheduledMessageStoreEFSqlServerTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/PostgreSQL/Scheduling/ScheduledMessageStoreEFPostgreSqlTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/MySQL/Scheduling/ScheduledMessageStoreEFMySqlTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Oracle/Scheduling/ScheduledMessageStoreEFOracleTests.cs`

### Phase 4.4 - Repository

- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Sqlite/Repository/FunctionalRepositoryEFSqliteTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/Repository/FunctionalRepositoryEFSqlServerTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/PostgreSQL/Repository/FunctionalRepositoryEFPostgreSqlTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/MySQL/Repository/FunctionalRepositoryEFMySqlTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Oracle/Repository/FunctionalRepositoryEFOracleTests.cs`

### Phase 4.5 - UnitOfWork

- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Sqlite/UnitOfWork/UnitOfWorkEFSqliteTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/UnitOfWork/UnitOfWorkEFSqlServerTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/PostgreSQL/UnitOfWork/UnitOfWorkEFPostgreSqlTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/MySQL/UnitOfWork/UnitOfWorkEFMySqlTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Oracle/UnitOfWork/UnitOfWorkEFOracleTests.cs`

### Phase 4.6 - Health Check

- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Sqlite/Health/EntityFrameworkCoreHealthCheckSqliteTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/Health/EntityFrameworkCoreHealthCheckSqlServerTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/PostgreSQL/Health/EntityFrameworkCoreHealthCheckPostgreSqlTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/MySQL/Health/EntityFrameworkCoreHealthCheckMySqlTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Oracle/Health/EntityFrameworkCoreHealthCheckOracleTests.cs`

### Modified Files

- `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/TestEFDbContext.cs` - Added `TestRepositoryEntity` for repository tests

---

## Test Patterns Used

### Standard Pattern for Provider-Specific Test Class

```csharp
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.{Provider}.{Feature};

[Trait("Category", "Integration")]
[Trait("Database", "{Provider}")]
[Collection("EFCore-{Provider}")]
public sealed class {Feature}EF{Provider}Tests : IAsyncLifetime
{
    private readonly EFCore{Provider}Fixture _fixture;

    public {Feature}EF{Provider}Tests(EFCore{Provider}Fixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [Fact]
    public async Task BasicOperation_WithRealDatabase_ShouldSucceed()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();

        // ... test implementation
    }
}
```

### MySQL Skip Pattern (until Pomelo v10)

```csharp
[SkippableFact]
public async Task BasicOperation_WithRealDatabase_ShouldSucceed()
{
    Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0");
    // ... test implementation
}
```
