# Encina.TestInfrastructure

Shared test infrastructure for Encina database provider testing using **Testcontainers**.

## Overview

This project provides reusable test infrastructure components that enable real database testing for all Encina database providers. It uses [Testcontainers for .NET](https://dotnet.testcontainers.org/) to automatically manage Docker containers for integration tests.

## Features

- ✅ **Database Fixtures** - Automated container lifecycle management for 5 databases
- ✅ **SQL Schema Builders** - Ready-to-use schemas for Outbox, Inbox, Sagas, Scheduling
- ✅ **Test Data Builders** - Fluent API for creating test messages and saga states
- ✅ **Assertion Extensions** - Domain-specific assertions for messaging entities
- ✅ **Dapper Type Handlers** - SQLite compatibility utilities

## Architecture

```
Encina.TestInfrastructure/
├── Fixtures/               # Testcontainers database fixtures
│   ├── DatabaseFixture.cs  # Abstract base class
│   ├── SqlServerFixture.cs
│   ├── PostgreSqlFixture.cs
│   ├── MySqlFixture.cs
│   ├── OracleFixture.cs
│   └── SqliteFixture.cs
├── Schemas/                # SQL schema creation scripts
│   ├── SqlServerSchema.cs
│   ├── PostgreSqlSchema.cs
│   ├── MySqlSchema.cs
│   ├── OracleSchema.cs
│   └── SqliteSchema.cs
├── Builders/               # Test data builders
│   ├── OutboxMessageBuilder.cs
│   ├── InboxMessageBuilder.cs
│   ├── SagaStateBuilder.cs
│   └── ScheduledMessageBuilder.cs
└── Extensions/             # Helper extensions
    ├── AssertionExtensions.cs
    └── DapperTypeHandlers.cs
```

## Usage

### 1. Database Fixtures

Use xUnit's `IClassFixture<T>` to share database containers across tests in a test class.

```csharp
using Encina.TestInfrastructure.Fixtures;
using Xunit;

[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class OutboxStoreSqlServerIntegrationTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public OutboxStoreSqlServerIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistToDatabase()
    {
        // Arrange
        using var connection = _fixture.CreateConnection();
        var store = new OutboxStoreSqlServer(connection);
        var message = OutboxMessageBuilder.Create()
            .WithNotificationType("TestNotification")
            .Build();

        // Act
        await store.AddAsync(message);

        // Assert
        var retrieved = await store.GetMessageAsync(message.Id);
        Assert.NotNull(retrieved);
        message.ShouldBePending();
    }
}
```

### 2. Test Data Builders

Create test data with fluent API:

```csharp
using Encina.TestInfrastructure.Builders;

// Outbox message
var outboxMessage = OutboxMessageBuilder.Create()
    .WithNotificationType("OrderPlaced")
    .WithContent("{\"orderId\":123}")
    .Build();

// Failed message with retries
var failedMessage = OutboxMessageBuilder.Create()
    .WithError("Connection timeout", retryCount: 3)
    .WithNextRetryAtUtc(DateTime.UtcNow.AddMinutes(5))
    .Build();

// Inbox message
var inboxMessage = InboxMessageBuilder.Create()
    .WithMessageId("msg-001")
    .AsProcessed("{\"status\":\"success\"}")
    .Build();

// Saga state
var saga = SagaStateBuilder.Create()
    .WithSagaType("OrderSaga")
    .WithCurrentStep(2)
    .WithStatus("Running")
    .Build();

// Scheduled message
var scheduled = ScheduledMessageBuilder.Create()
    .AsDaily(hour: 9, minute: 0)  // Daily at 9:00 AM
    .Build();
```

### 3. Assertion Extensions

Use domain-specific assertions:

```csharp
using Encina.TestInfrastructure.Extensions;

// Outbox assertions
outboxMessage.ShouldBePending();
outboxMessage.ShouldBeProcessed();
outboxMessage.ShouldHaveFailed("Connection timeout");

// Inbox assertions
inboxMessage.ShouldBePending();
inboxMessage.ShouldBeProcessed();
inboxMessage.ShouldBeExpired();

// Saga assertions
saga.ShouldBeRunning();
saga.ShouldBeCompleted();
saga.ShouldBeCompensating();
saga.ShouldHaveFailed("Payment failed");

// Scheduled message assertions
scheduledMessage.ShouldBePending();
scheduledMessage.ShouldBeDue();
scheduledMessage.ShouldBeRecurring();
```

### 4. Dapper Type Handlers

For SQLite compatibility, register type handlers:

```csharp
using Encina.TestInfrastructure.Extensions;

public class OutboxStoreDapperTests : IClassFixture<SqliteFixture>
{
    public OutboxStoreDapperTests()
    {
        // Register once per test run
        DapperTypeHandlers.RegisterSqliteHandlers();
    }
}
```

## Supported Databases

| Database   | Fixture Class          | Container Image                     |
|------------|------------------------|-------------------------------------|
| SQL Server | `SqlServerFixture`     | `mcr.microsoft.com/mssql/server:2022-latest` |
| PostgreSQL | `PostgreSqlFixture`    | `postgres:17-alpine`                |
| MySQL      | `MySqlFixture`         | `mysql:9.1`                         |
| Oracle     | `OracleFixture`        | `gvenzl/oracle-free:23-slim-faststart` |
| SQLite     | `SqliteFixture`        | In-memory (no container)            |

## Database Schemas

Each fixture automatically creates all 4 messaging tables:

1. **OutboxMessages** - Reliable event publishing
2. **InboxMessages** - Idempotent message processing
3. **SagaStates** - Distributed transaction orchestration
4. **ScheduledMessages** - Delayed/recurring execution

Schemas are created in `InitializeAsync()` and dropped in `DisposeAsync()` automatically.

## Running Integration Tests

```bash
# Run all integration tests (requires Docker)
dotnet test --filter "Category=Integration"

# Run only SQL Server integration tests
dotnet test --filter "Database=SqlServer"

# Run only PostgreSQL integration tests
dotnet test --filter "Database=PostgreSQL"

# Run unit tests (no Docker required)
dotnet test --filter "Category!=Integration"
```

## Prerequisites

- **.NET 10.0** or later
- **Docker Desktop** running (for integration tests)
- **xUnit** test framework

## NuGet Dependencies

```xml
<PackageReference Include="Testcontainers" Version="4.2.0" />
<PackageReference Include="Testcontainers.MsSql" Version="4.2.0" />
<PackageReference Include="Testcontainers.PostgreSql" Version="4.2.0" />
<PackageReference Include="Testcontainers.MySql" Version="4.2.0" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="Dapper" Version="2.1.66" />
```

## Design Principles

### Why Testcontainers?

- ✅ **Real Databases** - Tests run against actual database instances, not mocks
- ✅ **Automatic Lifecycle** - Containers start/stop automatically per test run
- ✅ **Isolation** - Each test class gets its own database instance
- ✅ **CI/CD Ready** - Works in GitHub Actions, Azure DevOps, etc.
- ✅ **Fast Feedback** - ~5-10 seconds startup per container
- ✅ **No Manual Setup** - No need to install/configure databases locally

### DRY (Don't Repeat Yourself)

- ✅ **Shared Fixtures** - Same fixture classes for all 11 database providers
- ✅ **Shared Schemas** - SQL scripts optimized per database dialect
- ✅ **Shared Builders** - Test data creation standardized across all tests
- ✅ **Shared Assertions** - Common validation logic for all messaging entities

### Provider Coherence

All database providers (Dapper.Sqlite, Dapper.SqlServer, ADO.PostgreSQL, etc.) use:

- ✅ **Same fixture base class** (`DatabaseFixture<T>`)
- ✅ **Same builder API** (OutboxMessageBuilder, etc.)
- ✅ **Same assertion extensions** (ShouldBePending, ShouldBeProcessed)
- ✅ **Same test structure** (Unit/, Guards/, Integration/, Property/, Contract/, Load/)

## Examples

See existing test projects:

- `Encina.Dapper.Sqlite.Tests` - Reference implementation
- More providers coming soon (Phase 4 of roadmap)

## Roadmap

- ✅ **Phase 1** - Create TestInfrastructure with Testcontainers fixtures
- ⏳ **Phase 2** - Refactor Dapper.Sqlite.Tests to use new infrastructure
- ⏳ **Phase 3** - Delete obsolete test projects
- ⏳ **Phase 4** - Create tests for all 11 database providers
- ⏳ **Phase 5** - Achieve 100% test coverage

## Contributing

When adding new test infrastructure:

1. **Follow naming conventions** - `{Feature}Builder`, `{Database}Fixture`, `{Database}Schema`
2. **Add XML documentation** - All public APIs must be documented
3. **Keep it provider-agnostic** - Infrastructure should work for ALL providers
4. **Test against real databases** - Integration tests > mocks

## License

Part of the Encina project. See root LICENSE file.
