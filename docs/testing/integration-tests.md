# Integration Testing with Docker

Encina uses Docker containers for integration testing against real database engines. This ensures high-quality, production-like testing without requiring local database installations.

## Prerequisites

- Docker Desktop installed and running
- .NET 10 SDK
- Approximately 5 GB disk space for database images

## Quick Start

### Run All Integration Tests

```bash
dotnet run --file scripts/run-integration-tests.cs
```

This command will:

1. Start SQL Server, PostgreSQL, and MySQL containers
2. Wait for databases to become healthy
3. Execute all integration tests
4. Stop containers upon completion

### Test Specific Database Provider

```bash
# SQL Server only
dotnet run --file scripts/run-integration-tests.cs -- --database sqlserver

# PostgreSQL only
dotnet run --file scripts/run-integration-tests.cs -- --database postgres

# MySQL only
dotnet run --file scripts/run-integration-tests.cs -- --database mysql
```

### Manual Container Management

For faster iteration during development:

```bash
# Start core services (PostgreSQL, Redis, RabbitMQ)
docker compose --profile core up -d

# Start all databases
docker compose --profile databases up -d

# Start databases + messaging
docker compose --profile databases --profile messaging up -d

# Start everything
docker compose --profile full up -d

# Run tests against running containers
dotnet run --file scripts/run-integration-tests.cs -- --skip-docker

# Stop databases
docker compose --profile databases down

# Clean slate (remove volumes)
docker compose --profile full down -v
```

> **Available Profiles**: `core`, `databases`, `messaging`, `caching`, `cloud`, `observability`, `full`
> See [Docker Infrastructure Guide](../infrastructure/docker-infrastructure.md) for complete details.

## Database Configuration

### Connection Strings

Connection strings are defined in `tests/appsettings.Testing.json`:

| Database   | Host          | Port | User      | Password            |
|------------|---------------|------|-----------|---------------------|
| SQL Server | localhost     | 1433 | sa        | Encina123!  |
| PostgreSQL | localhost     | 5432 | Encina  | Encina123!  |
| MySQL      | localhost     | 3306 | Encina  | Encina123!  |
| Oracle XE  | localhost     | 1521 | system    | Encina123!  |
| SQLite     | In-memory     | N/A  | N/A       | N/A                 |

### Docker Images

#### Databases

| Database   | Image                                                 | Startup Time |
|------------|-------------------------------------------------------|--------------|
| SQL Server | `mcr.microsoft.com/mssql/server:2022-latest`          | ~15 seconds  |
| PostgreSQL | `postgres:16-alpine`                                  | ~5 seconds   |
| MySQL      | `mysql:8.0`                                           | ~10 seconds  |
| Oracle XE  | `container-registry.oracle.com/database/express:21.3.0-xe` | ~60 seconds  |
| MongoDB    | `mongo:7`                                             | ~5 seconds   |

#### Messaging

| Broker     | Image                                    | Startup Time |
|------------|------------------------------------------|--------------|
| RabbitMQ   | `rabbitmq:3-management-alpine`           | ~10 seconds  |
| Kafka      | `apache/kafka:3.7.0`                     | ~30 seconds  |
| NATS       | `nats:2-alpine`                          | ~2 seconds   |
| Mosquitto  | `eclipse-mosquitto:2`                    | ~2 seconds   |

#### Caching

| Cache      | Image                                              | Startup Time |
|------------|----------------------------------------------------|--------------|
| Redis      | `redis:7-alpine`                                   | ~2 seconds   |
| Garnet     | `ghcr.io/microsoft/garnet:latest`                  | ~3 seconds   |
| Valkey     | `valkey/valkey:8-alpine`                           | ~2 seconds   |
| Dragonfly  | `docker.dragonflydb.io/dragonflydb/dragonfly:latest` | ~3 seconds |
| KeyDB      | `eqalpha/keydb:latest`                             | ~3 seconds   |

#### Cloud Emulators

| Service    | Image                                              | Startup Time |
|------------|----------------------------------------------------|--------------|
| LocalStack | `localstack/localstack:latest`                     | ~30 seconds  |
| Azurite    | `mcr.microsoft.com/azure-storage/azurite:latest`   | ~5 seconds   |

> **Note**: Oracle XE requires accepting the Oracle license agreement and has significantly longer startup time.

## Collection Fixture Strategy

### Problem: Docker Container Explosion

Without shared fixtures, each test class creates its own Docker container instance. With ~84 database integration test classes running in parallel, this means ~71 simultaneous containers, causing:

- Docker resource exhaustion (`io_setup() failed with EAGAIN`)
- `TimeoutException` during container startup
- SQL Server login failures under heavy parallel load
- Intermittent test failures that pass when run in isolation

### Solution: Shared xUnit Collection Fixtures

xUnit `[CollectionDefinition]` + `ICollectionFixture<T>` shares ONE fixture instance (and thus one Docker container) across ALL test classes in the same collection. This reduces containers from ~71 to ~23 (68% reduction).

### Collection Definitions

Collections are defined in `Collections.cs` files at the root of each provider directory:

| File | Collections Defined |
| ---- | ------------------- |
| `tests/Encina.IntegrationTests/ADO/Collections.cs` | `ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL`, `ADO-Sqlite` |
| `tests/Encina.IntegrationTests/Dapper/Collections.cs` | `Dapper-SqlServer`, `Dapper-PostgreSQL`, `Dapper-MySQL`, `Dapper-Sqlite` |
| `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Collections.cs` | `EFCore-SqlServer`, `EFCore-PostgreSQL`, `EFCore-MySQL`, `EFCore-Sqlite` |

### Expected Container Count

| Database | ADO | Dapper | EFCore | Other | Total |
| -------- | --- | ------ | ------ | ----- | ----- |
| SqlServer | 1 | 1 | 1 | 2 (Concurrency) | 5 |
| PostgreSQL | 1 | 1 | 1 | 1 (Marten) | 4 |
| MySQL | 1 | 1 | 1 | - | 3 |
| SQLite | 0 | 0 | 0 | - | 0 (in-memory) |
| MongoDB | - | - | - | 2 | 2 |
| Redis | - | - | - | 1 | 1 |
| Messaging | - | - | - | ~5 | ~5 |
| **Total** | **3** | **3** | **3** | **~11** | **~20-23** |

### How to Write a New Integration Test Class

**Step 1**: Choose the correct collection for your provider + database combination.

**Step 2**: Add `[Collection("...")]` and inject the fixture via constructor.

**Step 3**: Use `IAsyncLifetime` for per-test setup/teardown.

```csharp
[Collection("ADO-PostgreSQL")]  // Shares the PostgreSQL container with all other ADO PostgreSQL tests
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public class MyNewStoreTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private MyStore _store = null!;

    public MyNewStoreTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Clean data from previous test class (shared fixture = shared DB)
        await _fixture.ClearAllDataAsync();

        // Create your store using the fixture's connection
        _store = new MyStore(_fixture.CreateConnection());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MyTest()
    {
        // Use _store...
    }
}
```

### Rules (Mandatory)

1. **Always use `[Collection]`**: Never use `IClassFixture<T>` or `new()` for database fixtures. The collection manages the fixture lifecycle.

2. **Never dispose the fixture**: Do not call `_fixture.DisposeAsync()` from tests. The xUnit collection runner calls it once after ALL test classes in the collection have finished.

3. **Clean data, not schema**: Use `_fixture.ClearAllDataAsync()` in `InitializeAsync()` to remove data from previous tests. The schema (tables) persists for the lifetime of the collection.

4. **Use the correct traits**: Always include both `[Trait("Category", "Integration")]` and `[Trait("Database", "...")]` for filtering.

### SQLite Special Rules

SQLite uses `Mode=Memory;Cache=Shared` with a unique database name per fixture instance. The fixture's `CreateConnection()` returns the **same shared connection object** (not a new one). Disposing this connection destroys the in-memory database for ALL test classes in the collection.

**Critical rules:**

- ❌ **NEVER** dispose a connection obtained from `_fixture.CreateConnection()`
- ❌ **NEVER** wrap it in `using` or `await using`
- ❌ **NEVER** pass it to wrappers that call `Dispose()` on the inner connection (e.g., `SchemaValidatingConnection`, `ModuleAwareConnectionFactory`, `DatabaseHealthCheck` with `using var connection = ...`)
- ✅ Store it in a field and use it without disposing
- ✅ When you need a **disposable** connection (for wrappers, health checks, etc.), create a new independent one:

```csharp
// WRONG - this disposes the shared connection, destroying the in-memory DB
await using var connection = _fixture.CreateConnection();  // ❌

// WRONG - the wrapper will dispose the shared connection
var inner = _fixture.CreateConnection();
await using var wrapper = new SchemaValidatingConnection(inner, ...);  // ❌

// CORRECT - store without disposing
_connection = _fixture.CreateConnection();  // ✅

// CORRECT - create independent connection for disposable scenarios
var independent = new SqliteConnection(_fixture.ConnectionString);
independent.Open();
await using var wrapper = new SchemaValidatingConnection(independent, ...);  // ✅
```

**Why `DisableParallelization = true`?** SQLite collections disable parallel execution because all test classes share a single in-memory database. Parallel writes from different classes would cause data corruption and non-deterministic test results.

### Adding a New Collection

If you need a new collection (e.g., for a new database provider or a specialized fixture):

1. Add the `[CollectionDefinition]` class to the appropriate `Collections.cs` file
2. Use a descriptive name following the pattern: `{Provider}-{Database}` (e.g., `ADO-Oracle`)
3. For in-memory databases, add `DisableParallelization = true`
4. Update this document's container count table

```csharp
// In the appropriate Collections.cs file
[CollectionDefinition("ADO-Oracle")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit requires collection types to end with 'Collection'")]
public class ADOOracleCollection : ICollectionFixture<OracleFixture>
{
}
```

### Running Tests by Database Group

For clean results without Docker contention, run tests by database group:

```bash
# These groups run independently with zero failures
dotnet test tests/Encina.IntegrationTests --filter "Database=Sqlite|Provider=ADO.Sqlite|Provider=Dapper.Sqlite"
dotnet test tests/Encina.IntegrationTests --filter "Database=PostgreSQL"
dotnet test tests/Encina.IntegrationTests --filter "Database=MySQL"
dotnet test tests/Encina.IntegrationTests --filter "Database=SqlServer"
```

> **Note**: Running ALL groups simultaneously may cause intermittent SqlServer fixture initialization failures due to Docker container startup contention. This is a known Docker limitation, not a code issue. All tests pass when run by database group.

---

## Test Organization

### Test Categories

Integration tests use xUnit traits and collections for categorization:

```csharp
[Collection("ADO-SqlServer")]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class OutboxStoreSqlServerIntegrationTests : IAsyncLifetime
{
    // Tests share a single SQL Server Docker container via the collection fixture
}
```

### Filtering Tests

```bash
# Run only integration tests
dotnet test --filter "Category=Integration"

# Run specific database provider tests
dotnet test --filter "Category=Integration&Database=SqlServer"

# Exclude integration tests (unit tests only)
dotnet test --filter "Category!=Integration"
```

### Test Coverage

Integration tests cover:

- **Outbox Pattern**: Message persistence, retrieval, and processing
- **Inbox Pattern**: Idempotency guarantees and deduplication
- **Saga Pattern**: State persistence and compensation logic
- **Scheduling Pattern**: Delayed and recurring message execution
- **Transaction Pattern**: Commit/rollback behavior with Railway Oriented Programming

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Start database containers
  run: docker-compose up -d sqlserver postgres mysql

- name: Wait for databases to be healthy
  run: sleep 30

- name: Run integration tests
  run: dotnet test --filter "Category=Integration"

- name: Stop database containers
  run: docker-compose down
```

### Local Development Workflow

**Morning Setup:**

```bash
docker-compose up -d
```

**Development Loop:**

```bash
# Make changes
dotnet test --filter "Category=Integration"
```

**End of Day:**

```bash
docker-compose down
```

## Troubleshooting

### Port Conflicts

If local databases are already running on standard ports:

```bash
# Windows: Check port usage
netstat -ano | findstr :1433

# Linux/macOS: Check port usage
lsof -i :1433

# Solution: Stop local service or modify docker-compose.yml ports
```

### Container Startup Failures

```bash
# View container logs
docker-compose logs sqlserver

# Clean start
docker-compose down -v
docker-compose up -d
```

### Connection Timeouts

```bash
# Verify container health
docker ps

# Check specific container health status
docker inspect Encina-sqlserver --format='{{.State.Health.Status}}'

# If unhealthy, examine logs
docker-compose logs sqlserver
```

### Oracle-Specific Issues

Oracle XE has a large image size (~2 GB) and slow startup (~60 seconds):

**Solutions:**

- Skip Oracle tests during development: `--database sqlserver`
- Pull image once: `docker pull container-registry.oracle.com/database/express:21.3.0-xe`
- Keep Oracle container running between test runs

## Provider Support Matrix

The following table shows which providers are supported by each testing approach:

| Provider | Testcontainers | Aspire.Hosting | Recommended Approach | Notes |
|----------|----------------|----------------|---------------------|-------|
| **PostgreSQL** | `PostgreSqlContainer` | `AddPostgres()` | Testcontainers | Established fixtures, simpler API |
| **SQL Server** | `MsSqlContainer` | `AddSqlServer()` | Testcontainers | Established fixtures, simpler API |
| **MySQL** | `MySqlContainer` | `AddMySql()` | Testcontainers | Established fixtures, simpler API |
| **Oracle** | `GenericContainer` | **Not Supported** | **Testcontainers Only** | Critical - no Aspire alternative |
| **SQLite** | N/A (in-memory) | N/A | In-memory | No container needed |
| **MongoDB** | `MongoDbContainer` | `AddMongoDB()` | Testcontainers | Established fixtures |
| **Redis** | `RedisContainer` | `AddRedis()` | Testcontainers | Established fixtures |
| **RabbitMQ** | `RabbitMqContainer` | `AddRabbitMQ()` | Testcontainers | Established fixtures |
| **Kafka** | `KafkaContainer` | `AddKafka()` | Testcontainers | Established fixtures |
| **NATS** | `GenericContainer` | `AddNats()` | Testcontainers | Limited Aspire support |
| **MQTT** | `GenericContainer` | **Limited** | **Testcontainers Only** | No dedicated Aspire resource |

### Decision Guidance

**Use Testcontainers when:**

- Component-level database/service tests
- Oracle database (not supported in Aspire)
- Fine-grained container control needed
- Leveraging existing `Encina.TestInfrastructure` fixtures

**Use Aspire.Hosting.Testing when:**

- Testing full Aspire AppHost configurations
- Multi-service orchestration scenarios
- Production parity validation

See [ADR-008: Aspire vs Testcontainers Testing Strategy](../architecture/adr/008-aspire-vs-testcontainers-testing-strategy.md) for detailed guidance.

---

## Architecture

### Test Infrastructure

The `Encina.TestInfrastructure` project provides:

- **Database Fixtures**: Per-provider fixtures implementing `IAsyncLifetime`
- **Schema Builders**: Database-specific DDL for Outbox, Inbox, Sagas, Scheduling
- **Test Data Builders**: Fluent builders for test entities
- **Testcontainers Integration**: Programmatic container lifecycle management

### Provider Matrix

| Provider             | Integration | Contract | Property | Load |
|----------------------|-------------|----------|----------|------|
| Dapper.SqlServer     | ✅          | ✅       | ✅       | ✅   |
| Dapper.PostgreSQL    | ✅          | ✅       | ✅       | ✅   |
| Dapper.MySQL         | ✅          | ✅       | ✅       | ✅   |
| Dapper.Oracle        | ✅          | ✅       | ✅       | ✅   |
| Dapper.Sqlite        | ✅          | ✅       | ✅       | ✅   |
| ADO.SqlServer        | ✅          | ✅       | ✅       | ✅   |
| ADO.PostgreSQL       | ✅          | ✅       | ✅       | ✅   |
| ADO.MySQL            | ❌          | ✅       | ✅       | ✅   |
| ADO.Oracle           | ❌          | ✅       | ✅       | ✅   |
| ADO.Sqlite           | ❌          | ✅       | ✅       | ✅   |

> **Note**: ADO MySQL/Oracle/Sqlite use Testcontainers-based integration tests in Contract/Property/Load projects instead of separate Integration projects.

## Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [xUnit Trait Attributes](https://xunit.net/docs/getting-started/netcore/cmdline#traits)
- [SQL Server Docker Hub](https://hub.docker.com/_/microsoft-mssql-server)
- [PostgreSQL Docker Hub](https://hub.docker.com/_/postgres)
- [MySQL Docker Hub](https://hub.docker.com/_/mysql)
- [Oracle Container Registry](https://container-registry.oracle.com/)
