# ADR-008: Aspire vs Testcontainers Integration Testing Strategy

**Status:** Accepted
**Date:** 2026-01-07
**Deciders:** Architecture Team
**Technical Story:** Evaluate Aspire.Hosting.Testing as potential replacement or complement to Testcontainers for integration testing (Issue #509)

## Context

### Current Testing Architecture

The Encina project has an established integration testing infrastructure built on Testcontainers:

**Scale and Coverage:**

- 1,565 test files across the test suite
- 113 files directly using container/async lifecycle patterns
- 31 fixture classes providing centralized container management
- Support for 11 different services (databases, message brokers, caches)

**Centralized Fixture Architecture:**

```
tests/Encina.TestInfrastructure/
├── Fixtures/
│   ├── DatabaseFixture.cs          # Abstract base (IAsyncLifetime)
│   ├── PostgreSqlFixture.cs        # PostgreSQL via Testcontainers
│   ├── SqlServerFixture.cs         # SQL Server via Testcontainers
│   ├── MySqlFixture.cs             # MySQL via Testcontainers
│   ├── SqliteFixture.cs            # SQLite (in-memory, no container)
│   ├── OracleFixture.cs            # Oracle via GenericContainer
│   ├── MongoDbFixture.cs           # MongoDB via Testcontainers
│   ├── RedisFixture.cs             # Redis via Testcontainers
│   ├── RabbitMqFixture.cs          # RabbitMQ via Testcontainers
│   ├── KafkaFixture.cs             # Kafka via Testcontainers
│   ├── NatsFixture.cs              # NATS via Testcontainers
│   └── MqttFixture.cs              # MQTT via Testcontainers
└── Schemas/
    ├── PostgreSqlSchema.cs
    ├── SqlServerSchema.cs
    └── ...
```

**Key Patterns:**

1. **IAsyncLifetime Integration:** All fixtures implement xUnit's `IAsyncLifetime` for async container lifecycle
2. **Abstract Base Class:** `DatabaseFixture<TContainer>` provides common lifecycle management
3. **Schema Automation:** Each fixture creates/drops database schemas automatically
4. **Connection Factory:** Fixtures expose `CreateConnection()` for test access
5. **Programmatic Control:** Full control over container configuration (ports, environment, wait strategies)

**Example: Current PostgreSQL Fixture Usage:**

```csharp
public sealed class PostgreSqlFixture : DatabaseFixture<PostgreSqlContainer>
{
    protected override async Task<PostgreSqlContainer> CreateContainerAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("encina_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
        return _container;
    }

    public override IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}
```

### Aspire.Hosting.Testing Evaluation (POC Results)

We conducted a proof-of-concept comparing both approaches for PostgreSQL integration testing.

**Aspire Pattern Discovered:**

```csharp
// Requires an AppHost entry point class
public sealed class TestAppHost
{
    public const string PostgresDatabaseName = "encina_test";

    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        var postgres = builder.AddPostgres("postgres")
            .WithImage("postgres")
            .WithImageTag("17-alpine");
        postgres.AddDatabase(PostgresDatabaseName);
        builder.Build().Run();
    }
}

// Test class usage
public sealed class PostgreSqlAspireTests : IAsyncLifetime
{
    private DistributedApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<TestAppHost>();

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        await _app.ResourceNotifications.WaitForResourceAsync(
            TestAppHost.PostgresDatabaseName,
            KnownResourceStates.Running);

        _connectionString = await _app.GetConnectionStringAsync(
            TestAppHost.PostgresDatabaseName);
    }
}
```

### Architectural Differences

| Aspect | Testcontainers | Aspire.Hosting.Testing |
|--------|---------------|------------------------|
| **Model** | In-process programmatic control | Closed-box AppHost orchestration |
| **Startup** | Direct Docker API calls | Runs AppHost in background thread |
| **Configuration** | Builder pattern per container | Declarative resource model |
| **Container Access** | Direct container reference | Via resource abstraction |
| **Connection String** | `container.GetConnectionString()` | `app.GetConnectionStringAsync(name)` |
| **Wait Strategy** | Custom per container | `WaitForResourceAsync` + states |
| **Multi-Container** | Manual coordination | Automatic dependency management |

### Provider Support Matrix

| Provider | Testcontainers | Aspire.Hosting |
|----------|---------------|----------------|
| PostgreSQL | PostgreSqlContainer | PostgresServerResource |
| SQL Server | MsSqlContainer | SqlServerServerResource |
| MySQL | MySqlContainer | MySqlServerResource |
| SQLite | N/A (in-memory) | N/A |
| **Oracle** | GenericContainer | **Not Supported** |
| MongoDB | MongoDbContainer | MongoDBServerResource |
| Redis | RedisContainer | RedisResource |
| RabbitMQ | RabbitMqContainer | RabbitMQServerResource |
| Kafka | KafkaContainer | KafkaServerResource |
| NATS | Custom container | NatsServerResource |

**Critical Gap:** Oracle is not supported by Aspire.Hosting. The Encina project actively uses Oracle for enterprise scenarios.

### Performance Comparison

Based on Phase 1 POC analysis (benchmarks pending execution with Docker):

**Expected Characteristics:**

| Metric | Testcontainers | Aspire | Notes |
|--------|---------------|--------|-------|
| Single Container Startup | Faster (~30-50%) | Slower | No orchestration overhead |
| Memory Usage | Lower | Higher | No DI/resource tracking |
| Multi-Container Startup | Similar | Similar/Faster | Aspire parallelizes |
| API Complexity | Lower | Higher | Direct vs abstracted |

**Testcontainers Advantages:**

- Direct Docker API interaction without orchestration layer
- Single container initialization without AppHost scaffolding
- Less memory due to no distributed application infrastructure
- Simpler programmatic control

**Aspire Advantages:**

- Automatic dependency tracking and lifecycle management
- Built-in resource health monitoring and notifications
- Consistent configuration model with production Aspire apps
- Better for multi-service orchestration scenarios

## Decision

**We will maintain Testcontainers as the primary integration testing infrastructure for component-level tests, while introducing Aspire.Hosting.Testing selectively for distributed application scenarios.**

### Selection Criteria

**Use Testcontainers when:**

1. **Component-level integration tests** - Testing a single service (database store, message handler)
2. **Programmatic control needed** - Custom container configuration, wait strategies, or lifecycle management
3. **Oracle database testing** - Not supported by Aspire.Hosting
4. **Fine-grained container control** - Port mapping, environment variables, custom images
5. **Performance-critical test suites** - Lower overhead for large test suites
6. **Existing fixture reuse** - Leveraging established `DatabaseFixture<T>` infrastructure

**Use Aspire.Hosting.Testing when:**

1. **End-to-end distributed application testing** - Testing full Aspire AppHost configurations
2. **Multi-service orchestration** - Testing cross-service communication
3. **AppHost validation** - Verifying production Aspire resource configurations
4. **Integration with Aspire dashboard** - Debugging distributed test scenarios
5. **Production parity testing** - Using same orchestration model as production

### Implementation Approach

1. **Keep existing infrastructure:** All 31 fixture classes remain unchanged
2. **Add Aspire when needed:** Create `tests/Encina.Aspire.IntegrationTests/` for distributed scenarios
3. **No migration:** Do not migrate existing tests to Aspire
4. **Document patterns:** Both approaches documented with clear examples (see POC)

## Consequences

### Benefits

**Proven Patterns (Testcontainers):**

- 3+ years of production use in Encina
- Well-understood by development team
- Comprehensive fixture library covering all providers
- Minimal code changes required

**Oracle Support:**

- Critical for enterprise customers using Oracle
- No workaround available in Aspire
- `OracleFixture` uses GenericContainer with custom wait strategy

**Minimal Disruption:**

- Zero changes to existing test suites
- No learning curve for current developers
- Existing CI/CD pipelines unchanged

**Future Flexibility:**

- Can introduce Aspire incrementally for new distributed scenarios
- Both approaches coexist without conflict
- Easy to evaluate Aspire as it matures

### Trade-offs

**Two Approaches to Learn:**

- New developers may encounter both patterns
- Need documentation for when to use each
- Potential for inconsistent test patterns if not governed

**Aspire Features Unavailable:**

- Resource health integration with Aspire dashboard
- Automatic dependency resolution between services
- Production-parity configuration testing

**Maintenance Overhead:**

- Two testing patterns to maintain
- Different upgrade paths (Testcontainers vs Aspire SDK)

### Future Considerations

**Monitor Oracle Support:**

- If Aspire adds Oracle support, re-evaluate migration
- Track [dotnet/aspire issues](https://github.com/dotnet/aspire/issues) for Oracle requests

**Re-evaluate for Microservices:**

- If Encina adds microservice architectures, Aspire becomes more valuable
- Multi-AppHost testing scenarios favor Aspire

**BenchmarkDotNet Results:**

- Run Phase 1 benchmarks when Docker is available
- Update performance section with actual measurements
- Adjust recommendations if Aspire overhead is lower than expected

## Alternatives Considered

### 1. Full Migration to Aspire.Hosting.Testing

**Description:** Replace all Testcontainers usage with Aspire

**Rejected because:**

- Oracle not supported (critical for enterprise)
- Significant migration effort (31 fixtures, 113+ test files)
- Higher overhead for simple single-container tests
- Loss of fine-grained programmatic control
- Breaking change to established patterns

### 2. Aspire-Only for New Tests

**Description:** Keep existing Testcontainers tests, use Aspire for all new tests

**Rejected because:**

- Oracle tests still need Testcontainers
- Component-level tests don't benefit from Aspire orchestration
- Forces context-switching between patterns for similar tests

### 3. Wrapper Abstraction Layer

**Description:** Create abstraction over both Testcontainers and Aspire

**Rejected because:**

- Adds unnecessary complexity
- Lowest-common-denominator API loses benefits of each
- Maintenance burden of abstraction layer
- Both APIs are already well-designed

### 4. Wait for Aspire Maturity

**Description:** Delay decision until Aspire adds Oracle and stabilizes

**Rejected because:**

- No timeline for Oracle support
- Current infrastructure works well
- Can introduce Aspire incrementally when beneficial

## Related Decisions

- ADR-003: Caching Strategy (test performance considerations)
- Issue #509: Aspire Testing Evaluation (tracking issue)
- Issue #517: POC Disposition Decision (follow-up for POC project fate)

## References

### Documentation

- [Aspire Testing Documentation](https://aspire.dev/testing/write-your-first-test/)
- [Testcontainers .NET](https://dotnet.testcontainers.org/)
- [Microsoft Learn: Aspire Testing](https://learn.microsoft.com/en-us/dotnet/aspire/testing/write-your-first-test)

### POC Artifacts

- `tests/Encina.Aspire.POC.Tests/` - Phase 1 proof of concept
- `tests/Encina.Aspire.POC.Tests/PERFORMANCE-RESULTS.md` - Benchmark documentation
- `tests/Encina.Aspire.POC.Tests/PostgreSqlAspireTests.cs` - Aspire pattern example
- `tests/Encina.Aspire.POC.Tests/PostgreSqlTestcontainersTests.cs` - Testcontainers pattern example

### External Analysis

- [Aspire vs Testcontainers Analysis (endjin)](https://endjin.com/blog/2025/06/dotnet-aspire-db-testing-integration-tests)

## Notes

This decision reflects the current state of both technologies as of January 2026. .NET Aspire is actively developed and may address current limitations (Oracle support, overhead) in future releases. The hybrid approach allows us to adopt Aspire capabilities incrementally while maintaining our proven testing infrastructure.

The key insight from the POC is that **Aspire excels at what it was designed for: distributed application orchestration**. For component-level database tests where we need direct container control, Testcontainers remains the better tool. This is not a weakness of either technology - it's using the right tool for the right job.
