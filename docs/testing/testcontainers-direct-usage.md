# Testcontainers Direct Usage Analysis

> **Related Documentation:**
>
> - [ADR-008: Aspire vs Testcontainers Testing Strategy](../architecture/adr/008-aspire-vs-testcontainers-testing-strategy.md)
> - [Aspire Migration Guide](./aspire-migration-guide.md)
> - [Integration Testing with Docker](./integration-tests.md)

---

## Overview

This document analyzes test projects that have **direct Testcontainers package dependencies** instead of using the centralized fixtures from `Encina.TestInfrastructure`. It provides guidance on when direct usage is appropriate and how to refactor to use centralized fixtures.

## Projects with Direct Testcontainers Dependencies

### Summary

| Project | Direct Dependencies | Uses TestInfrastructure | Refactoring Recommendation |
|---------|---------------------|------------------------|---------------------------|
| Encina.Caching.IntegrationTests | `Testcontainers`, `Testcontainers.Redis` | No | Consider refactoring |
| Encina.DistributedLock.IntegrationTests | `Testcontainers`, `Testcontainers.Redis`, `Testcontainers.MsSql` | Yes (partial) | Keep as is |
| Encina.EntityFrameworkCore.IntegrationTests | `Testcontainers.MsSql` | Yes (partial) | Keep as is |
| Encina.Marten.IntegrationTests | `Testcontainers.PostgreSql` | Yes (partial) | Keep as is |

---

## Detailed Analysis

### 1. Encina.Caching.IntegrationTests

**Location:** `tests/Encina.Caching.IntegrationTests/`

**Direct Dependencies:**

```xml
<PackageReference Include="Testcontainers" />
<PackageReference Include="Testcontainers.Redis" />
```

**Missing Reference:**

- Does NOT reference `Encina.TestInfrastructure`

**Analysis:**

This project tests Redis-based caching (`Encina.Caching.Redis`) and requires a Redis container. It has its own inline Testcontainers setup instead of using the centralized `RedisFixture` from TestInfrastructure.

**Why Direct Usage Exists:**

- Historical: Project may have been created before `RedisFixture` was added to TestInfrastructure
- Isolation: May have wanted to avoid dependency on TestInfrastructure

**Refactoring Recommendation:** Consider refactoring to use `Encina.TestInfrastructure.Fixtures.RedisFixture`

**Refactoring Steps:**

1. Add reference to `Encina.TestInfrastructure`
2. Remove direct `Testcontainers` and `Testcontainers.Redis` references
3. Use `IClassFixture<RedisFixture>` or create test collection
4. Update tests to use `fixture.ConnectionString`

**Before:**

```csharp
public class RedisCacheIntegrationTests : IAsyncLifetime
{
    private RedisContainer? _container;

    public async Task InitializeAsync()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
        await _container.StartAsync();
    }

    [Fact]
    public async Task Cache_Set_ReturnsValue()
    {
        var connectionString = _container!.GetConnectionString();
        // Test logic
    }
}
```

**After:**

```csharp
public class RedisCacheIntegrationTests : IClassFixture<RedisFixture>
{
    private readonly RedisFixture _fixture;

    public RedisCacheIntegrationTests(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Cache_Set_ReturnsValue()
    {
        var connectionString = _fixture.ConnectionString;
        // Test logic
    }
}
```

---

### 2. Encina.DistributedLock.IntegrationTests

**Location:** `tests/Encina.DistributedLock.IntegrationTests/`

**Direct Dependencies:**

```xml
<PackageReference Include="Testcontainers" />
<PackageReference Include="Testcontainers.Redis" />
<PackageReference Include="Testcontainers.MsSql" />
```

**References TestInfrastructure:** Yes

**Analysis:**

This project tests distributed locking with Redis and SQL Server backends. It has both direct Testcontainers dependencies AND a reference to TestInfrastructure.

**Why Direct Usage Exists:**

- **Intentional:** Project needs specialized container configurations for distributed lock testing
- May need custom wait strategies or timeouts specific to lock acquisition scenarios
- TestInfrastructure fixtures may not expose lock-specific configuration

**Refactoring Recommendation:** Keep as is

**Justification:**

- Already references TestInfrastructure (can use shared fixtures if needed)
- Distributed locking tests may need fine-grained container control
- Lock timeout testing may require custom container configurations
- No clear benefit to forcing standardization here

---

### 3. Encina.EntityFrameworkCore.IntegrationTests

**Location:** `tests/Encina.EntityFrameworkCore.IntegrationTests/`

**Direct Dependencies:**

```xml
<PackageReference Include="Testcontainers.MsSql" />
```

**References TestInfrastructure:** Yes

**Analysis:**

This project tests Entity Framework Core integration with SQL Server. It has a direct SQL Server Testcontainers dependency but also references TestInfrastructure.

**Why Direct Usage Exists:**

- **EF Core-specific fixtures:** Has its own `EFCoreFixture` in the project that wraps both container and DbContext setup
- DbContext creation and migration require specialized setup not provided by base `SqlServerFixture`
- Needs to register EF Core-specific services

**Refactoring Recommendation:** Keep as is

**Justification:**

- `EFCoreFixture` (local) extends functionality beyond raw SQL Server container
- Combines container lifecycle with DbContext configuration
- Migration and seeding logic is EF Core-specific
- TestInfrastructure's `SqlServerFixture` doesn't include DbContext setup

**Pattern Example:**

```csharp
// EFCoreFixture creates container AND configures DbContext
public class EFCoreFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    public TestDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        DbContext = new TestDbContext(options);
        await DbContext.Database.MigrateAsync();
    }
}
```

---

### 4. Encina.Marten.IntegrationTests

**Location:** `tests/Encina.Marten.IntegrationTests/`

**Direct Dependencies:**

```xml
<PackageReference Include="Testcontainers.PostgreSql" />
```

**References TestInfrastructure:** Yes

**Analysis:**

This project tests Marten (PostgreSQL-based document database / event store) integration. It has a direct PostgreSQL Testcontainers dependency and references TestInfrastructure.

**Why Direct Usage Exists:**

- **Marten-specific setup:** Has its own `MartenFixture` that configures Marten's `IDocumentStore`
- Marten requires specific PostgreSQL configuration (schema, naming conventions)
- Document store initialization differs from raw SQL operations

**Refactoring Recommendation:** Keep as is

**Justification:**

- `MartenFixture` (local) wraps PostgreSQL container with Marten configuration
- Marten's `IDocumentStore` requires specialized setup not in base `PostgreSqlFixture`
- Event sourcing schema differs from messaging schema
- Already uses TestInfrastructure for other shared utilities

**Pattern Example:**

```csharp
// MartenFixture creates container AND configures Marten IDocumentStore
public class MartenFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    public IDocumentStore Store { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .Build();
        await _container.StartAsync();

        Store = DocumentStore.For(opts =>
        {
            opts.Connection(_container.GetConnectionString());
            opts.AutoCreateSchemaObjects = AutoCreate.All;
            // Marten-specific configuration
        });
    }
}
```

---

## Guidelines for Direct Testcontainers Usage

### When Direct Usage is Appropriate

1. **Specialized Container Configuration:** Need custom wait strategies, timeouts, or environment variables not exposed by TestInfrastructure fixtures

2. **Technology-Specific Setup:** Container lifecycle must be combined with technology-specific initialization (e.g., EF Core migrations, Marten store configuration)

3. **Local Fixtures:** Project has its own fixture class that extends container functionality

4. **Isolation Requirements:** Need to avoid coupling to TestInfrastructure for specific reasons

### When to Use Centralized Fixtures

1. **Standard Database Access:** Only need connection string, not technology-specific setup

2. **Messaging Pattern Tests:** Testing outbox, inbox, saga, scheduling with standard schema

3. **Multiple Providers:** Need to run same tests against multiple databases

4. **Consistency:** Want to ensure all projects use same container versions/configurations

---

## Refactoring Checklist

When refactoring from direct Testcontainers to centralized fixtures:

### Pre-Refactoring Assessment

- [ ] Check if TestInfrastructure has appropriate fixture (e.g., `RedisFixture`, `SqlServerFixture`)
- [ ] Verify fixture exposes needed APIs (connection string, custom configuration)
- [ ] Identify any custom container setup that would be lost
- [ ] Review existing tests for technology-specific initialization

### Refactoring Steps

1. [ ] Add project reference to `Encina.TestInfrastructure`
2. [ ] Remove direct Testcontainers package references
3. [ ] Replace inline container setup with `IClassFixture<T>` or `ICollectionFixture<T>`
4. [ ] Update tests to use fixture properties
5. [ ] Run all tests to verify behavior parity
6. [ ] Update project documentation if needed

### Post-Refactoring Validation

- [ ] All tests pass
- [ ] No orphaned container cleanup code
- [ ] Container lifecycle properly managed by xUnit

---

## Current TestInfrastructure Fixtures Available

| Fixture | Package | Provider |
|---------|---------|----------|
| `SqlServerFixture` | `Testcontainers.MsSql` | SQL Server |
| `PostgreSqlFixture` | `Testcontainers.PostgreSql` | PostgreSQL |
| `MySqlFixture` | `Testcontainers.MySql` | MySQL |
| `OracleFixture` | `Testcontainers` (Generic) | Oracle |
| `SqliteFixture` | N/A (in-memory) | SQLite |
| `MongoDbFixture` | `Testcontainers.MongoDB` | MongoDB |
| `RedisFixture` | `Testcontainers.Redis` | Redis |
| `RabbitMqFixture` | `Testcontainers.RabbitMq` | RabbitMQ |
| `KafkaFixture` | `Testcontainers.Kafka` | Kafka |
| `NatsFixture` | `Testcontainers` (Generic) | NATS |
| `MqttFixture` | `Testcontainers` (Generic) | MQTT |

---

## References

- [Encina.TestInfrastructure](../../tests/Encina.TestInfrastructure/) - Centralized fixture implementations
- [ADR-008: Aspire vs Testcontainers Testing Strategy](../architecture/adr/008-aspire-vs-testcontainers-testing-strategy.md)
- [Integration Testing with Docker](./integration-tests.md)
