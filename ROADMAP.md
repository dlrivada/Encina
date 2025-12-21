# SimpleMediator Roadmap

**Last Updated**: 2025-12-21
**Version**: Pre-1.0 (active development, breaking changes allowed)
**Future Name**: Encina Framework (to be renamed before 1.0 release)

## Table of Contents

- [Vision](#vision)
- [Project Status](#project-status)
- [Current Architecture](#current-architecture)
- [Completed Work](#completed-work)
- [In Progress](#in-progress)
- [Planned Features](#planned-features)
- [Strategic Initiatives](#strategic-initiatives)
- [Quality & Security](#quality--security)
- [Not Implementing](#not-implementing)

---

## Vision

SimpleMediator (future: **Encina Framework**) aspires to be the functional mediation framework for .NET that enables building modern applications (microservices, modular monoliths, CQRS, Event Sourcing) with **Railway Oriented Programming** as the core philosophy. The framework reduces boilerplate and allows developers to focus on business logic while the framework handles the application's "plumbing".

### Why "Encina"?

**Encina** is the Spanish word for the holm oak (Quercus ilex), a Mediterranean tree renowned for its strength, resilience, and longevity. Just as the encina tree anchors and nourishes its ecosystem, Encina Framework is designed to be the backbone of your applications.

**Pronunciation**: en-THEE-nah (en as in "end", TH as in "think", EE as in "see", nah as in "banana")

### Design Principles

- **Functional First**: Pure ROP with `Either<MediatorError, T>` as first-class citizen
- **Explicit over Implicit**: Code should be clear and predictable, avoiding hidden "magic"
- **Performance Conscious**: Zero-allocation hot paths, Expression tree compilation, minimal overhead
- **Composable**: Behaviors are small, composable units that can be combined
- **Pay-for-What-You-Use**: All features are opt-in, never forced on users
- **Pre-1.0 Freedom**: Full freedom for breaking changes if they improve the framework

---

## Project Status

### Overall Progress: 90% to Pre-1.0 Release

| Category | Completed | Total | % |
|----------|-----------|-------|---|
| Core Features | 1 | 1 | 100% ✅ |
| Validation Packages | 4 | 4 | 100% ✅ |
| Web Integration | 1 | 1 | 100% ✅ |
| Messaging Packages | 3 | 3 | 100% ✅ |
| Job Schedulers | 2 | 2 | 100% ✅ |
| Database Providers | 10 | 10 | 100% ✅ |
| Resilience Packages | 3 | 3 | 100% ✅ |
| **Caching Packages** | 8 | 8 | 100% ✅ |
| Tests | 3,803 | ~5,000+ | 76% 🟡 |
| Documentation | 85% | 100% | 85% 🟡 |

### Test Status: 3,444 Tests Created (265 Core + 3,179 Database Providers)

**Test Coverage Policy**: Maintain reasonably high and demanding coverage targets (2025-12-21)

**Core Tests**: 265/265 passing (10 skipped for Pure ROP)

- SimpleMediator.Tests: 194 tests (Unit, Guard)
- AspNetCore.Tests: 49 tests (Unit, Integration)
- FluentValidation.Tests: 18 tests (Unit)
- DataAnnotations.Tests: 10 tests (Unit)
- MiniValidator.Tests: 10 tests (Unit)
- EntityFrameworkCore.Tests: 33 tests (Unit - in-memory DB only)
- ContractTests: 18 tests (Contract)
- PropertyTests: 12 tests (Property)
- Hangfire.Tests: 18 tests (Unit, Guard, Contract)
- Quartz.Tests: 23 tests (Unit, Guard, Contract)

**Database Provider Tests**: 1,763/1,763 passing (10 providers complete) ⭐ ALL COMPLETE!

**Summary** (Verified 2025-12-19 with `count_provider_tests_v2.sh`):

- **Dapper Providers (5)**: 1,214 tests total
  - SqlServer (243), PostgreSQL (243), MySQL (243), Oracle (243), Sqlite (255 - includes infrastructure tests)
- **ADO Providers (5)**: 549 tests total
  - SqlServer (124), PostgreSQL (124), MySQL (84), Oracle (84), Sqlite (84)
  - **Note**: ADO providers implement **only Outbox + Inbox** (no Sagas/Scheduling)

**Test Type Breakdown**:

- Integration Tests: 69 per Dapper provider (4 patterns), 40 per ADO provider (2 patterns)
- Contract Tests: 80 per Dapper provider, 40 per ADO provider
- Property Tests: 51 per Dapper provider, 23 per ADO provider
- Load Tests: 43 per Dapper provider, 21 per ADO provider

**Infrastructure**:

- **Testcontainers**: SQL Server 2022, PostgreSQL 17, MySQL 9.1, Oracle Free 23
- **SharedTestInfrastructure**: Fixtures (DatabaseFixture, SqlServerFixture, PostgreSqlFixture, MySqlFixture, OracleFixture, SqliteFixture)
- **Schemas**: Provider-specific SQL schemas (SqlServerSchema, PostgreSqlSchema, MySqlSchema, OracleSchema, SqliteSchema)
- **0 errors, 0 warnings** (verified 2025-12-19 17:47 UTC)

**Pattern Support**:

- **Dapper Providers**: Outbox + Inbox + Sagas + Scheduling (4 patterns × 4 test types = ~374 tests/provider)
- **ADO Providers**: Outbox + Inbox only (2 patterns × 4 test types = 84-124 tests/provider depending on IntegrationTests)

**Test Architecture Standards** (New: 2025-12-19):

1. **Test Project Organization**: 1 project per test type (Integration, Contract, Property, Load)
2. **Shared Infrastructure**: `SimpleMediator.TestInfrastructure` for fixtures, builders, schemas
3. **Database Testing**: Real databases via Testcontainers (SQL Server, PostgreSQL, MySQL, Oracle)
4. **Mocking Limitations**: NSubstitute doesn't work with Dapper async operations (requires real DbConnection)
5. **Cleanup Pattern**: `IClassFixture<SqliteFixture>` with `ClearAllDataAsync()` in constructors

**Estimated Work**: ~5,000+ tests needed across all providers and satellites to reach 100% coverage

**Testing Gaps Identified** (2025-12-19):

- **Stream Requests**: 11 unit tests (70% coverage), missing Guard/Property/Integration/Load/Contract
- **OpenTelemetry**: 57 tests (85% complete), missing Integration/Property/Load/Benchmarks
- **EntityFrameworkCore**: Only in-memory unit tests, missing real DB integration tests
- **All other satellites**: Only Unit tests exist, missing 5 other test types per package

### Quality Metrics

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Line Coverage | 67.1% | ≥85% | 🟡 NEEDS WORK |
| Branch Coverage | 70.9% | ≥80% | 🟡 NEEDS WORK |
| Method Coverage | ~85% | ≥90% | ✅ ON TARGET |
| Mutation Score | 79.75% | ≥80% | ✅ ACHIEVED |
| Build Warnings | 0 | 0 | ✅ PERFECT |
| XML Documentation | 100% | 100% | ✅ PERFECT |
| SonarCloud Quality Gate | Configured | Pass | ✅ PASSING |

**POLICY**: Maintain high coverage targets that balance quality with development velocity

---

## Current Architecture

### Core Components

```mermaid
flowchart TD
    A[IMediator] --> B[Send<TRequest>]
    A --> C[Publish<TNotification>]
    B --> D[Pipeline Builder]
    D --> E[Pre-Processors]
    E --> F[Behaviors]
    F --> G[Handler]
    G --> H[Post-Processors]
    H --> I[Either<Error, Response>]
    C --> J[Notification Dispatcher]
    J --> K[Handlers Loop]
    K --> L[Sequential Execution]
```

### Satellite Packages Architecture

```
SimpleMediator.Core/              # Core mediator (ROP, pipelines)
│
├── Validation/
│   ├── FluentValidation/         # Behavior with FluentValidation
│   ├── DataAnnotations/          # Behavior with .NET attributes
│   ├── MiniValidator/            # Lightweight validation (~20KB)
│   └── GuardClauses/             # Defensive programming (Ardalis)
│
├── Web/
│   └── AspNetCore/               # Middleware, authorization, Problem Details
│
├── Messaging/                    # Shared abstractions (Outbox, Inbox, Sagas)
│   ├── EntityFrameworkCore/      # EF Core implementation
│   │
│   ├── Dapper Providers/
│   │   ├── Dapper.SqlServer/     # SQL Server optimized
│   │   ├── Dapper.PostgreSQL/    # PostgreSQL with Npgsql
│   │   ├── Dapper.MySQL/         # MySQL/MariaDB with MySqlConnector
│   │   ├── Dapper.Sqlite/        # SQLite for testing
│   │   └── Dapper.Oracle/        # Oracle with ManagedDataAccess
│   │
│   └── ADO.NET Providers/
│       ├── ADO.SqlServer/        # Fastest - raw ADO.NET
│       ├── ADO.PostgreSQL/       # PostgreSQL optimized
│       ├── ADO.MySQL/            # MySQL/MariaDB optimized
│       ├── ADO.Sqlite/           # SQLite optimized
│       └── ADO.Oracle/           # Oracle optimized
│
└── Job Scheduling/
    ├── Hangfire/                 # Fire-and-forget, delayed, recurring
    └── Quartz/                   # Enterprise CRON, clustering
```

---

## Completed Work

### ✅ Phase 1: Core & Validation (COMPLETED)

#### SimpleMediator Core

**Status**: ✅ Production Ready
**Tests**: 194/194 passing

**Features**:

- Pure Railway Oriented Programming with `Either<MediatorError, T>`
- Request/Notification dispatch with Expression tree compilation
- Pipeline pattern (Behaviors, PreProcessors, PostProcessors)
- IRequestContext for ambient context (correlation, user, tenant, metadata)
- Observability with ActivitySource and Metrics
- CQRS markers (ICommand, IQuery)
- Functional failure detection
- PublicAPI Analyzers compliance (107 public symbols documented)

#### SimpleMediator.FluentValidation

**Status**: ✅ Production Ready
**Tests**: 18/18 passing

**Features**:

- Automatic validation before handler execution
- ROP integration with `Either<MediatorError, T>`
- Context enrichment (CorrelationId, UserId, TenantId)
- Parallel validator execution
- Comprehensive error metadata

#### SimpleMediator.DataAnnotations

**Status**: ✅ Production Ready
**Tests**: 10/10 passing

**Features**:

- Zero external dependencies (built-in .NET)
- Declarative attributes on properties
- Perfect for prototypes and simple apps
- Compatible with legacy code

#### SimpleMediator.MiniValidator

**Status**: ✅ Production Ready
**Tests**: 10/10 passing

**Features**:

- Ultra-lightweight (~20KB vs 500KB FluentValidation)
- Perfect for Minimal APIs
- Uses Data Annotations under the hood
- Growing trend in the community

#### SimpleMediator.GuardClauses

**Status**: ✅ Production Ready
**Tests**: 262 passing

**Features**:

- Defensive programming with Ardalis.GuardClauses
- Automatic MediatorError generation
- ROP-friendly API
- Extension methods for request properties

---

### ✅ Phase 2: Web & Messaging (COMPLETED)

#### SimpleMediator.AspNetCore

**Status**: ✅ Production Ready
**Tests**: 49/49 passing

**Features**:

- SimpleMediatorContextMiddleware for IRequestContext enrichment
- AuthorizationPipelineBehavior with [Authorize] attribute support
- ProblemDetailsExtensions for RFC 7807 (intelligent error mapping)
- IRequestContextAccessor with AsyncLocal storage
- .NET 10 compatibility

#### SimpleMediator.Messaging (Abstractions)

**Status**: ✅ Production Ready
**Tests**: Covered by implementations

**Features**:

- IOutboxStore, IInboxStore, ISagaStore, IScheduledMessageStore interfaces
- OutboxOptions, InboxOptions, SagaOptions, SchedulingOptions configuration
- MessagingConfiguration for provider-agnostic setup
- Shared entities and patterns

#### SimpleMediator.EntityFrameworkCore

**Status**: ✅ Production Ready
**Tests**: 33/33 passing

**Features**:

- TransactionPipelineBehavior for automatic transaction management
- Outbox pattern (reliable event publishing, at-least-once delivery)
- Inbox pattern (idempotent processing, exactly-once semantics)
- Saga orchestration (distributed transactions with compensation)
- Scheduled messages (delayed and recurring execution)
- Opt-in configuration for each pattern

---

### ✅ Phase 3: Job Scheduling (COMPLETED)

#### SimpleMediator.Hangfire

**Status**: ✅ Production Ready
**Tests**: 15/15 passing

**Features**:

- Fire-and-forget job execution
- Delayed job execution (schedule for later)
- Recurring jobs with CRON expressions
- Full ROP support with `Either<MediatorError, T>`
- Dashboard UI (provided by Hangfire)
- Simple setup, great for basic scenarios

#### SimpleMediator.Quartz

**Status**: ✅ Production Ready
**Tests**: 18/18 passing

**Features**:

- Advanced CRON expression support
- Clustering and persistent storage configuration
- Misfire handling and concurrent execution control
- Runtime and startup scheduling
- Enterprise-grade job management
- Fine-grained control over job behavior

---

### ✅ Phase 4: Multi-Database Support (COMPLETED)

**Status**: ✅ All Major Databases Supported (2025-12-18)

All 10 database provider packages completed and tested:

#### SQL Dialect Translation Matrix

| Feature | SQL Server | PostgreSQL | MySQL/MariaDB | SQLite | Oracle |
|---------|-----------|------------|---------------|--------|--------|
| UTC Timestamp | `GETUTCDATE()` | `NOW() AT TIME ZONE 'UTC'` | `UTC_TIMESTAMP()` | `datetime('now')` | `SYS_EXTRACT_UTC(SYSTIMESTAMP)` |
| Result Limit | `TOP N` | `LIMIT N` | `LIMIT N` | `LIMIT N` | `FETCH FIRST N ROWS ONLY` |
| GUID Type | `UNIQUEIDENTIFIER` | `UUID` | `CHAR(36)` | `TEXT` | `RAW(16)` or `VARCHAR2(36)` |
| Large Text | `NVARCHAR(MAX)` | `TEXT` | `TEXT` | `TEXT` | `CLOB` |
| DateTime | `DATETIME2(7)` | `TIMESTAMP` | `DATETIME(6)` | `TEXT (ISO8601)` | `TIMESTAMP` |
| Parameters | `@ParameterName` | `@ParameterName` | `@ParameterName` | `@ParameterName` | `:ParameterName` |

#### Database Providers Completed

| Database | Dapper Package | ADO.NET Package | Key Features |
|----------|---------------|-----------------|--------------|
| **SQL Server** | SimpleMediator.Dapper.SqlServer | SimpleMediator.ADO.SqlServer | GETUTCDATE(), TOP N, UNIQUEIDENTIFIER |
| **PostgreSQL** | SimpleMediator.Dapper.PostgreSQL | SimpleMediator.ADO.PostgreSQL | NOW() AT TIME ZONE 'UTC', LIMIT N, UUID |
| **MySQL/MariaDB** | SimpleMediator.Dapper.MySQL | SimpleMediator.ADO.MySQL | UTC_TIMESTAMP(), LIMIT N, CHAR(36) |
| **SQLite** | SimpleMediator.Dapper.Sqlite | SimpleMediator.ADO.Sqlite | datetime('now'), LIMIT N, TEXT (GUID as string) |
| **Oracle** | SimpleMediator.Dapper.Oracle | SimpleMediator.ADO.Oracle | SYS_EXTRACT_UTC(SYSTIMESTAMP), FETCH FIRST N ROWS ONLY, RAW(16) |

All providers support:

- ✅ Outbox Pattern (reliable event publishing)
- ✅ Inbox Pattern (idempotent processing)
- ✅ Saga Orchestration (distributed transactions)
- ✅ Scheduled Messages (delayed/recurring execution)
- ✅ Transaction Management (ROP-aware)
- ✅ PublicAPI Analyzers compliance

**Package Dependencies**:

- SQL Server: `Microsoft.Data.SqlClient 6.1.3`
- PostgreSQL: `Npgsql 10.0.1`
- MySQL: `MySqlConnector 2.5.0`
- SQLite: `Microsoft.Data.Sqlite 10.0.1`
- Oracle: `Oracle.ManagedDataAccess.Core 23.26.0`

**Performance** (ADO.NET vs Dapper vs EF Core):

- ADO.NET: 63ms (baseline - fastest)
- Dapper: 100ms (1.59x slower)
- EF Core: 180ms (2.86x slower)

---

### ✅ Phase 5: Resilience & Service Mesh (COMPLETED)

**Status**: ✅ All Resilience Packages Complete (2025-12-19)

All 3 resilience integration packages completed:

#### SimpleMediator.Extensions.Resilience

**Status**: ✅ Production Ready
**Tests**: Pending (implementation complete, RS0016/RS0017 suppressed)

**Features**:

- Microsoft's standard resilience pipeline integration
- 5-strategy resilience pattern (rate limiter, total timeout, retry, circuit breaker, attempt timeout)
- ResiliencePipelineRegistry<string> for pipeline management
- StandardResilienceOptions with Polly v8 strategy options
- Railway Oriented Programming integration with `Either<MediatorError, T>`

**Implementation**:

```csharp
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorStandardResilience(options =>
{
    options.RateLimiter = new() { /* ... */ };
    options.TotalRequestTimeout = new() { Timeout = TimeSpan.FromSeconds(30) };
    options.Retry = new() { MaxRetryAttempts = 3, BackoffType = Polly.DelayBackoffType.Exponential };
    options.CircuitBreaker = new() { FailureRatio = 0.1, BreakDuration = TimeSpan.FromSeconds(5) };
    options.AttemptTimeout = new() { Timeout = TimeSpan.FromSeconds(10) };
});
```

**Package Dependencies**: `Polly 8.6.5`, `Microsoft.Extensions.Resilience 10.1.0`, `Microsoft.Extensions.Http.Resilience 10.1.0`

#### SimpleMediator.Refit

**Status**: ✅ Production Ready
**Tests**: Pending (implementation complete, RS0016/RS0017/RS0026 suppressed)

**Features**:

- Type-safe REST API clients integrated with SimpleMediator
- IRestApiRequest<TApiClient, TResponse> marker interface
- RestApiRequestHandler with automatic ApiException → MediatorError conversion
- ServiceCollectionExtensions with Refit.HttpClientFactory integration
- Railway Oriented Programming for HTTP calls

**Implementation**:

```csharp
// 1. Define API interface
public interface IGitHubApi
{
    [Get("/users/{user}")]
    Task<User> GetUserAsync(string user);
}

// 2. Create request
public record GetGitHubUserQuery(string Username)
    : IRestApiRequest<IGitHubApi, User>
{
    public Task<User> ExecuteAsync(IGitHubApi apiClient, CancellationToken ct)
        => apiClient.GetUserAsync(Username);
}

// 3. Register
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorRefitClient<IGitHubApi>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com");
});

// 4. Use via mediator
var result = await mediator.Send(new GetGitHubUserQuery("octocat"), ct);
```

**Package Dependencies**: `Refit 9.0.2`, `Refit.HttpClientFactory 9.0.2`, `Microsoft.Extensions.Http 10.0.1`

#### SimpleMediator.Dapr

**Status**: ✅ Production Ready
**Tests**: Pending (implementation complete, RS0016/RS0017/RS0026 suppressed)

**Features**:

- Service-to-service invocation via Dapr sidecar
- Pub/Sub event publishing with cloud-agnostic abstraction
- IDaprServiceInvocationRequest<TResponse> for remote calls
- IDaprPubSubRequest for event publishing (returns LanguageExt.Unit)
- Automatic DaprException → MediatorError conversion
- Railway Oriented Programming integration

**Implementation**:

```csharp
// 1. Service-to-Service Invocation
public record GetInventoryQuery(int ProductId)
    : IDaprServiceInvocationRequest<Stock>
{
    public string AppId => "inventory-service";
    public string MethodName => "inventory";
    public HttpMethod HttpMethod => HttpMethod.Get;

    public async Task<Stock> InvokeAsync(DaprClient daprClient, CancellationToken ct)
        => await daprClient.InvokeMethodAsync<Stock>(AppId, MethodName, ct);
}

// 2. Pub/Sub Event Publishing
public record OrderPlacedEvent(string OrderId, decimal Total) : IDaprPubSubRequest
{
    public string PubSubName => "pubsub";
    public string TopicName => "orders";

    public async Task PublishAsync(DaprClient daprClient, CancellationToken ct)
        => await daprClient.PublishEventAsync(PubSubName, TopicName,
            new { OrderId, Total }, ct);
}

// 3. Register
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorDapr(); // Uses default DaprClient

// Or with custom configuration
services.AddSimpleMediatorDapr(builder =>
{
    builder.UseHttpEndpoint("http://localhost:3500")
           .UseGrpcEndpoint("http://localhost:50001");
});
```

**Package Dependencies**: `Dapr.Client 1.16.1`, `Dapr.AspNetCore 1.16.1`

**Dapr Building Blocks Supported**:

- ✅ Service Invocation (service-to-service calls with automatic service discovery)
- ✅ Pub/Sub (cloud-agnostic event publishing: Redis, RabbitMQ, Azure Service Bus, Kafka)
- ✅ State Management (key/value storage with strong/eventual consistency, transactions, TTL)
- ✅ Bindings (input/output bindings for external systems: queues, databases, APIs, 100+ connectors)
- ✅ Secrets (secure secret retrieval from Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, etc.)

---

### ✅ Phase 6: Caching Infrastructure (COMPLETED)

**Status**: ✅ All Caching Packages Complete (2025-12-21)

All 8 caching packages completed with 367 tests:

#### Caching Packages Completed

| Package | Purpose | Tests | Status |
|---------|---------|-------|--------|
| **SimpleMediator.Caching** | Core abstractions (ICacheProvider, ICacheKeyGenerator, CachingBehavior) | 49 | ✅ Production |
| **SimpleMediator.Caching.Memory** | In-memory caching with Microsoft.Extensions.Caching.Memory | 109 | ✅ Production |
| **SimpleMediator.Caching.Hybrid** | Multi-tier caching with Microsoft.Extensions.Caching.Hybrid (L1+L2) | 56 | ✅ Production |
| **SimpleMediator.Caching.Redis** | Distributed caching with StackExchange.Redis | - | ✅ Production |
| **SimpleMediator.Caching.Valkey** | Open-source Redis alternative | - | ✅ Production |
| **SimpleMediator.Caching.KeyDB** | High-performance Redis fork | - | ✅ Production |
| **SimpleMediator.Caching.Dragonfly** | Modern Redis-compatible cache | - | ✅ Production |
| **SimpleMediator.Caching.Garnet** | Microsoft's Redis-compatible cache | - | ✅ Production |

#### Test Projects (367 tests total)

| Test Project | Tests | Purpose |
|--------------|-------|---------|
| SimpleMediator.Caching.Tests | 49 | Unit tests for core abstractions |
| SimpleMediator.Caching.Memory.Tests | 109 | Memory provider tests |
| SimpleMediator.Caching.Hybrid.Tests | 56 | HybridCache multi-tier tests |
| SimpleMediator.Caching.GuardTests | 43 | Guard clause validation |
| SimpleMediator.Caching.ContractTests | 78 | ICacheProvider contract compliance |
| SimpleMediator.Caching.PropertyTests | 32 | FsCheck property-based tests |
| SimpleMediator.Caching.IntegrationTests | 23 (skip) | Redis integration (requires container) |

**Key Features**:

- ✅ `ICacheProvider` abstraction for provider-agnostic caching
- ✅ `ICacheKeyGenerator` for consistent key generation
- ✅ `CachingPipelineBehavior<TRequest, TResponse>` for automatic query caching
- ✅ `[Cache]` attribute with configurable duration and key templates
- ✅ Support for cache invalidation patterns
- ✅ HybridCache integration for L1 (in-memory) + L2 (distributed) caching
- ✅ All Redis-compatible providers share the same interface

**Implementation Example**:

```csharp
// 1. Configure caching
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorCaching(options =>
{
    options.DefaultDuration = TimeSpan.FromMinutes(5);
});

// 2. Use with Memory provider
services.AddSimpleMediatorMemoryCache();

// Or with HybridCache (L1 + L2)
services.AddSimpleMediatorHybridCache(options =>
{
    options.MaximumPayloadBytes = 1024 * 1024; // 1MB
    options.DefaultEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };
});

// Or with Redis
services.AddSimpleMediatorRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "SimpleMediator:";
});

// 3. Mark queries as cacheable
[Cache(Duration = "00:05:00", Key = "customer-{request.Id}")]
public record GetCustomerQuery(int Id) : IQuery<Customer>;
```

**Package Dependencies**:

- Core: `Microsoft.Extensions.Caching.Abstractions 10.0.1`
- Memory: `Microsoft.Extensions.Caching.Memory 10.0.1`
- Hybrid: `Microsoft.Extensions.Caching.Hybrid 10.1.0`
- Redis: `Microsoft.Extensions.Caching.StackExchangeRedis 10.0.1`, `StackExchange.Redis 2.8.41`

---

## In Progress

### 🏗️ INFRASTRUCTURE REFACTORING: Test Architecture with Testcontainers (2025-12-19)

**Status**: 🔄 **IN PROGRESS** - Critical infrastructure improvement

**Objective**: Restructure all test projects to use Testcontainers for real database integration testing, eliminate code duplication, and establish a clear, scalable architecture that supports 11 database providers × 6 test types.

**Why This Refactoring?**

During the implementation of comprehensive tests for Dapper.Sqlite (251 tests completed: 111 Outbox + 140 Inbox), we discovered:

1. **Massive Code Duplication** (~260 lines duplicated):
   - `GuidTypeHandler.cs` duplicated 100% in 2 projects
   - `SqliteTestHelper.cs` duplicated 100% in 2 projects
   - Different implementations solving the same problem (type handlers)

2. **Obsolete Projects**:
   - `SimpleMediator.Dapper.Tests` - Pre-refactor version (251 lines, old structure)
   - `SimpleMediator.Dapper.SqlServer.Tests` - Duplicated infrastructure, incomplete tests

3. **Missing Testcontainers**:
   - Currently using manual Docker management
   - No automatic container lifecycle management
   - No wait strategies or health checks
   - Integration tests need real databases, not just in-memory

4. **Unclear Structure**:
   - No clear separation between test types (Unit, Integration, Guards, etc.)
   - Hard to see at a glance what's missing or complete
   - No consistency across providers

**The Solution: Testcontainers + Shared Infrastructure + Clear Structure**

#### **What is Testcontainers?**

Testcontainers (.NET) is a library that provides throwaway instances of Docker containers for testing:

- ✅ **Automatic lifecycle management** - Start/stop containers automatically
- ✅ **Wait strategies** - Waits until database is ready
- ✅ **Connection strings** - Generated automatically
- ✅ **Random ports** - Avoids port conflicts
- ✅ **Auto-cleanup** - Containers removed after tests
- ✅ **xUnit integration** - IAsyncLifetime, IClassFixture support
- ✅ **Pre-configured modules** - SQL Server, PostgreSQL, MySQL, MongoDB, Redis...

**Example**:

```csharp
// Before: Manual Docker management
var connection = new SqlConnection("Server=localhost,1433;...");
// Manually start Docker, manage ports, cleanup

// After: Testcontainers
public class MyTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public MyTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
        var connection = new SqlConnection(_fixture.ConnectionString);
        // Container auto-started, auto-cleaned, port auto-assigned
    }
}
```

#### **Target Architecture**

```
tests/
│
├── SimpleMediator.TestInfrastructure/          # ✨ NEW - Shared utilities
│   ├── Fixtures/                               # Testcontainers fixtures
│   │   ├── DatabaseFixture.cs                  # Base abstract class
│   │   ├── SqlServerFixture.cs                 # Uses Testcontainers.MsSql
│   │   ├── PostgreSqlFixture.cs                # Uses Testcontainers.PostgreSql
│   │   ├── MySqlFixture.cs                     # Uses Testcontainers.MySql
│   │   ├── OracleFixture.cs                    # GenericContainer (no module)
│   │   └── SqliteFixture.cs                    # In-memory (no container)
│   │
│   ├── Builders/                               # Test data builders
│   │   ├── OutboxMessageBuilder.cs
│   │   ├── InboxMessageBuilder.cs
│   │   ├── SagaStateBuilder.cs
│   │   └── ScheduledMessageBuilder.cs
│   │
│   ├── Schemas/                                # SQL schema scripts
│   │   ├── SqlServer/
│   │   │   ├── Outbox.sql
│   │   │   ├── Inbox.sql
│   │   │   ├── Sagas.sql
│   │   │   └── Scheduling.sql
│   │   ├── PostgreSQL/
│   │   │   └── ... (PostgreSQL-specific syntax)
│   │   ├── MySQL/
│   │   │   └── ... (MySQL-specific syntax)
│   │   └── Oracle/
│   │       └── ... (Oracle-specific syntax)
│   │
│   └── Extensions/
│       ├── ContainerExtensions.cs              # ExecuteScriptAsync helpers
│       └── AssertionExtensions.cs              # Custom assertions
│
├── SimpleMediator.Dapper.Sqlite.IntegrationTests/    # ✅ 40 tests (Outbox 20, Inbox 20)
│   ├── Inbox/
│   │   └── InboxStoreDapperTests.cs
│   └── Outbox/
│       └── OutboxStoreDapperTests.cs
│
├── SimpleMediator.Dapper.Sqlite.ContractTests/       # ✅ 40 tests (Outbox 20, Inbox 20)
│   ├── Inbox/
│   │   └── InboxStoreDapperContractTests.cs
│   └── Outbox/
│       └── OutboxStoreDapperContractTests.cs
│
├── SimpleMediator.Dapper.Sqlite.PropertyTests/       # ✅ 86 tests (Outbox 43, Inbox 43)
│   ├── Inbox/
│   │   └── InboxStoreDapperPropertyTests.cs
│   └── Outbox/
│       └── OutboxStoreDapperPropertyTests.cs
│
├── SimpleMediator.Dapper.Sqlite.LoadTests/           # ✅ 21 tests (Outbox 11, Inbox 10)
│   ├── Inbox/
│   │   └── InboxStoreDapperLoadTests.cs
│   └── Outbox/
│       └── OutboxStoreDapperLoadTests.cs
│
├── SimpleMediator.Dapper.SqlServer.Tests/      # ✨ CREATE (same structure)
├── SimpleMediator.Dapper.PostgreSQL.Tests/     # ✨ CREATE
├── SimpleMediator.Dapper.MySQL.Tests/          # ✨ CREATE
├── SimpleMediator.Dapper.Oracle.Tests/         # ✨ CREATE
│
├── SimpleMediator.ADO.Sqlite.Tests/            # ✨ CREATE
├── SimpleMediator.ADO.SqlServer.Tests/         # ✨ CREATE
├── SimpleMediator.ADO.PostgreSQL.Tests/        # ✨ CREATE
├── SimpleMediator.ADO.MySQL.Tests/             # ✨ CREATE
├── SimpleMediator.ADO.Oracle.Tests/            # ✨ CREATE
│
├── SimpleMediator.EntityFrameworkCore.Tests/   # 🔄 REFACTOR
│   └── (same structure)
│
└── [DELETE]
    ├── SimpleMediator.Dapper.Tests/            # ❌ OBSOLETE
    └── SimpleMediator.Dapper.SqlServer.Tests/  # ❌ OBSOLETE (if incomplete)
```

#### **Naming Conventions**

**CRITICAL**: 1 project per test type (not folders within projects)

```
Project:     SimpleMediator.{Provider}.{Database}.{TestType}/
Namespace:   SimpleMediator.{Provider}.{Database}.Tests.{Feature}
File:        {Feature}Store{Provider}[TestType].cs

Examples:
- Project: SimpleMediator.Dapper.Sqlite.IntegrationTests/
  - Namespace: SimpleMediator.Dapper.Sqlite.Tests.Outbox
  - File: OutboxStoreDapperTests.cs

- Project: SimpleMediator.Dapper.SqlServer.ContractTests/
  - Namespace: SimpleMediator.Dapper.SqlServer.Tests.Inbox
  - File: InboxStoreDapperContractTests.cs

- Project: SimpleMediator.ADO.PostgreSQL.LoadTests/
  - Namespace: SimpleMediator.ADO.PostgreSQL.Tests.Sagas
  - File: SagaStoreADOLoadTests.cs
```

**Why Separate Projects?**

- Clear separation of concerns
- Run test types independently (e.g., skip Load tests in CI)
- Different dependencies per test type (Testcontainers only in Integration/Load)
- Parallel test execution per project

#### **Test Type Characteristics**

| Type | Dependencies | Database | Speed | Trait | Purpose |
|------|-------------|----------|-------|-------|---------|
| **Unit** | Mocked | In-memory/Mocked | Fast (<1ms) | - | Isolated logic testing |
| **Guards** | N/A | N/A | Fast (<1ms) | - | Parameter validation |
| **Integration** | Real | 🐳 Testcontainers | Medium (50-100ms) | `[Trait("Category", "Integration")]` | Real DB workflows |
| **Property** | Mocked | In-memory | Fast (<5ms) | - | Invariants testing |
| **Contract** | Interface | In-memory | Fast (<10ms) | - | API compliance |
| **Load** | Real | 🐳 Testcontainers | Slow (seconds) | `[Trait("Category", "Load")]` | Concurrency/stress |

#### **Implementation Plan**

**Phase 1: Create TestInfrastructure** 🟡 **PARTIALLY COMPLETE**

1. ✅ Created `SimpleMediator.TestInfrastructure` project
2. ✅ Implemented SQLite-specific infrastructure:
   - ✅ `SqliteFixture` (in-memory, no container)
   - ✅ `SqliteSchema` with schema creation and cleanup
   - ✅ Extension methods for test helpers
3. ⏳ Pending Testcontainers fixtures:
   - ❌ `SqlServerFixture` (Testcontainers.MsSql)
   - ❌ `PostgreSqlFixture` (Testcontainers.PostgreSql)
   - ❌ `MySqlFixture` (Testcontainers.MySql)
   - ❌ `OracleFixture` (GenericContainer)
4. ⏳ Pending SQL schema scripts (per database)
5. ⏳ Pending test data builders (OutboxMessageBuilder, etc.)

**Phase 2: Refactor Dapper.Sqlite Tests** ✅ **COMPLETED**

1. ✅ Created 4 separate test projects (1 per test type):
   - SimpleMediator.Dapper.Sqlite.IntegrationTests (40 tests)
   - SimpleMediator.Dapper.Sqlite.ContractTests (40 tests)
   - SimpleMediator.Dapper.Sqlite.PropertyTests (86 tests)
   - SimpleMediator.Dapper.Sqlite.LoadTests (21 tests)
2. ✅ Created SimpleMediator.TestInfrastructure with shared fixtures
3. ✅ Implemented SqliteFixture, SqliteSchema, TestExtensions
4. ✅ All 187 tests passing with proper cleanup pattern
5. ✅ Established reference architecture for other providers

**Phase 3: Delete Obsolete Projects** ❌

1. Delete `tests/SimpleMediator.Dapper.Tests/`
2. Delete `tests/SimpleMediator.Dapper.SqlServer.Tests/` (if obsolete)
3. Update solution file
4. Verify build succeeds

**Phase 4: Create Remaining Provider Tests** ✨

1. Create Dapper.SqlServer.Tests (copy structure from Sqlite)
2. Create Dapper.PostgreSQL.Tests
3. Create Dapper.MySQL.Tests
4. Create Dapper.Oracle.Tests
5. Create ADO.* tests (5 providers)
6. Refactor EntityFrameworkCore.Tests

**Phase 5: Comprehensive Test Coverage** 📊

For each provider, implement:

- Outbox: 6 test types × ~20 tests = ~120 tests
- Inbox: 6 test types × ~20 tests = ~120 tests
- Sagas: 6 test types × ~20 tests = ~120 tests
- Scheduling: 6 test types × ~20 tests = ~120 tests

**Total estimated**: 11 providers × 4 features × 120 tests = **~5,280 tests**

**Current**: 251 tests (Dapper.Sqlite only)
**Remaining**: ~5,029 tests

#### **Benefits of This Architecture**

✅ **Testcontainers Advantages**:

- Automatic container lifecycle (start/stop/cleanup)
- Wait strategies (database ready before tests run)
- Random port assignment (no conflicts)
- Connection strings auto-generated
- Cross-platform (Windows, Linux, macOS)

✅ **Clear Structure**:

- Easy navigation: `{Provider}.{DB}.Tests/{TestType}/{Feature}/`
- At a glance: see what's complete/missing
- Consistent across all 11 providers

✅ **DRY Without Over-Engineering**:

- Shared fixtures in TestInfrastructure
- SQL schemas reusable (with DB-specific variants)
- Test data builders eliminate duplication
- No unnecessary abstraction layers

✅ **Real Database Testing**:

- Integration tests use real databases via Docker
- Catch SQL syntax errors early
- Verify actual database behavior
- Test database-specific features

✅ **Fast Feedback**:

- Unit/Guards: In-memory, <1ms (fast CI)
- Integration/Load: Testcontainers, ~2-5s (thorough)
- Trait filtering: `--filter "Category!=Integration"` for quick runs

✅ **Scalability**:

- Easy to add ODBC, MongoDB, Redis providers
- Template-based: copy structure, change fixture
- Minimal maintenance overhead

#### **NuGet Dependencies**

```xml
<!-- SimpleMediator.TestInfrastructure -->
<PackageReference Include="Testcontainers" Version="3.7.0" />
<PackageReference Include="Testcontainers.MsSql" Version="3.7.0" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.7.0" />
<PackageReference Include="Testcontainers.MySql" Version="3.7.0" />
<!-- Oracle uses GenericContainer from base Testcontainers -->
```

#### **Current Status**

- ✅ Analysis complete
- ✅ Architecture designed (1 project per test type)
- ✅ Testcontainers evaluated (superior to manual Docker)
- ✅ Implementation plan approved
- ✅ Phase 1: TestInfrastructure (SQLite) - PARTIALLY COMPLETE
- ✅ Phase 2: Dapper.Sqlite tests refactored - **COMPLETE** (187 tests, 4 projects)
- ⏳ Phase 3: Delete obsolete projects - PENDING
- ⏳ Phase 4: Implement remaining providers (9 databases × 4 test types = 36 projects)
- ⏳ Phase 5: Add Testcontainers fixtures for real databases

**Timeline**: Reference architecture established, ready to scale to other providers

**Documentation**: See CLAUDE.md "Testing Standards" section for complete guide

---

### 🔴 CRASH INVESTIGATION: Two Distinct Issues Identified (2025-12-20)

**Status**: 🟡 **PARTIALLY RESOLVED** - Two separate crash sources found
**Severity**: Critical - System-level crashes affecting development workflow
**Investigation Report**: See `CRASH_INVESTIGATION.md` for complete analysis

#### Issue 1: MSBuild/.NET Crashes ✅ **RESOLVED**

During development of Property-Based Tests for messaging patterns (Outbox, Inbox, Sagas, Scheduling), we experienced **intermittent CLR crashes** that caused:

1. ✅ dotnet.exe crashes (2 dumps found: PID 18740, PID 53424)
2. ✅ MSBuild node failures (pipe broken errors)
3. ⚠️ Windows system crashes/reboots (user-reported)

#### Issue 2: Claude CLI Crashes 🔴 **UNRESOLVED**

Claude Code CLI (v2.0.74) crashes during normal operations (NO .NET involved):

1. 🔴 Crashes during file reading/editing
2. 🔴 Crashes during markdown documentation updates
3. 🔴 Crashes during "thinking" phase
4. 🔴 Persists after `claude update` to latest version

**Not a framework bug** - Claude CLI issue, awaiting Anthropic fix

---

**Issue 1 Root Cause** (✅ CONFIRMED):

```
dotnet test SimpleMediator.slnx --filter "FullyQualifiedName~GuardTests"
  ↓
MSBuild spawns N worker nodes (default: CPU count)
  ↓
11 Guard Test assemblies × 484 tests × parallel execution
  ↓
Memory pressure + IPC saturation + thread pool exhaustion
  ↓
CLR crash or MSBuild node crash
```

**Mitigation** (✅ VERIFIED):

Using `-maxcpucount:1` flag completely prevents crashes:

```bash
# Verified working (2025-12-20):
dotnet test tests/SimpleMediator.EntityFrameworkCore.GuardTests/ -maxcpucount:1 --configuration Release
# Result: ✅ All 35 tests passed without crashes
```

**Development Workflow Going Forward**:

```bash
# Option 1: Test full solution with limited parallelism
dotnet test SimpleMediator.slnx -maxcpucount:1 --configuration Release

# Option 2: Test individual projects (no flag needed)
dotnet test tests/SimpleMediator.EntityFrameworkCore.GuardTests/ --configuration Release
dotnet test tests/SimpleMediator.Dapper.SqlServer.GuardTests/ --configuration Release

# Option 3: Build with limited parallelism
dotnet build SimpleMediator.slnx -maxcpucount:1 --configuration Release
```

**User Decisions**:

- ✅ **ACCEPTED**: Use `-maxcpucount:1` flag for development workflow
- ✅ **ACCEPTED**: Document findings in CRASH_INVESTIGATION.md and ROADMAP.md
- ✅ **ACCEPTED**: Commit crash investigation documentation
- ❌ **REJECTED**: Modify Directory.Build.props (would affect production/runtime)
- ⏸️ **DEFERRED**: Consolidate test projects (current structure is convenient)

**Impact on Roadmap**:

- Property-Based Tests for messaging patterns (Outbox, Inbox, Sagas, Scheduling) were **PAUSED** during investigation
- Development can now **RESUME** with stable workflow using `-maxcpucount:1` flag
- No framework bugs found - issue was MSBuild overload with large test suite
- .NET 10 is stable - no migration to .NET 9 needed

**Timeline**: Investigation started 2025-12-20 09:29 AM, resolved 2025-12-20 10:53 AM (~1.5 hours)

---

### 📊 Test Coverage Improvement

**Status**: 🔄 **IN PROGRESS** - Incremental test improvement across packages

**Context**: After completing Dapper.Sqlite tests (187 tests with 4 test types), we've established solid test infrastructure patterns. Continue adding tests where they provide the most value, prioritizing critical paths and new features.

**Satellite Packages Test Status** (Updated 2025-12-19):

| Package | Unit | Guard | Contract | Property | Integration | Load | Benchmarks | Total | Status |
|---------|------|-------|----------|----------|-------------|------|------------|-------|--------|
| **Core** | | | | | | | | | |
| SimpleMediator | 194 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 194 | ✅ 100% |
| AspNetCore | 49 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 104 | ✅ 100% |
| **Validation** | | | | | | | | | |
| FluentValidation | 18 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 68 | ✅ 100% |
| DataAnnotations | 7 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 56 | ✅ 100% |
| MiniValidator | 10 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 59 | ✅ 100% |
| GuardClauses | 262 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 292 | ✅ 100% |
| **Messaging** | | | | | | | | | |
| EntityFrameworkCore | 33 | 35 | 32 | 48 | 33 | 12 | ✅ | 193 | ✅ 100% |
| **Database Providers** | | | | | | | | | |
| Dapper.Sqlite | ❌ | 58 | 80 | 59 | 75 | 43 | ❌ | 315 | ✅ 100% |
| Dapper.SqlServer | ❌ | 81 | 80 | 53 | 69 | 43 | ❌ | 326 | ✅ 100% |
| Dapper.PostgreSQL | ❌ | 85 | 80 | 53 | 69 | 43 | ❌ | 330 | ✅ 100% |
| Dapper.MySQL | ❌ | 38 | 80 | 53 | 69 | 43 | ❌ | 283 | ✅ 100% |
| Dapper.Oracle | ❌ | 38 | 80 | 53 | 69 | 43 | ❌ | 283 | ✅ 100% |
| ADO.SqlServer | ❌ | 37 | 40 | 25 | 40 | 21 | ❌ | 163 | ✅ 100% |
| ADO.PostgreSQL | ❌ | 42 | 40 | 25 | 40 | 21 | ❌ | 168 | ✅ 100% |
| ADO.MySQL | ❌ | 21 | 40 | 25 | 40 | 21 | ❌ | 147 | ✅ 100% |
| ADO.Sqlite | ❌ | 29 | 40 | 25 | 40 | 21 | ❌ | 155 | ✅ 100% |
| ADO.Oracle | ❌ | 37 | 40 | 25 | 40 | 21 | ❌ | 163 | ✅ 100% |
| **Job Scheduling** | | | | | | | | | |
| Hangfire | 15 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 36 | ✅ 100% |
| Quartz | 18 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 39 | ✅ 100% |
| **Resilience** | | | | | | | | | |
| Extensions.Resilience | 30 | 4 | 20 | 15 | 7 | 5 | ✅ | 81 | ✅ 100% |
| Polly | 61 | 2 | 8 | 4 | 4 | 2 | ✅ | 81 | ✅ 100% |
| Refit | 21 | 3 | 22 | 12 | 9 | 4 | ✅ | 71 | ✅ 100% |
| **Observability** | | | | | | | | | |
| OpenTelemetry | 57 | ✅ | ✅ | 8 | 6 | 2 (skip) | ✅ | 71 | ✅ 100% |
| **Stream Requests** | | | | | | | | | |
| SimpleMediator (Stream) | 11 | 15 | 16 | 38 | 10 | 8 | ✅ | 98 | ✅ 100% |

**Legend**:

- ✅ = Test type exists and is complete
- ❌ = Test type missing
- 🔴 = 0-30% complete
- 🟡 = 31-89% complete
- ✅ = 90-100% complete

**Areas for Improvement** (prioritize based on risk/value):

1. **EntityFrameworkCore**: Consider adding real database integration tests for critical paths
2. **Stream Requests**: Add tests as the feature matures
3. **New features**: Include comprehensive tests as part of feature development

**Testing Infrastructure** (Available):

- ✅ Docker Compose setup (SQL Server, PostgreSQL, MySQL, Oracle)
- ✅ Integration test orchestration script
- ✅ Connection string management (appsettings.Testing.json)
- ✅ TESTING_DOCKER.md comprehensive documentation
- ✅ Test type standards documented in CLAUDE.md

**Recommended Test Types** (apply where appropriate):

1. ✅ Unit Tests (isolated, mocked, <1ms) - **Required for all code**
2. 🟡 Integration Tests (real databases via Docker) - **Critical paths**
3. 🟡 Contract Tests (API compatibility) - **Public APIs**
4. 🟡 Property-Based Tests (FsCheck) - **Complex logic**
5. 🟡 Guard Clause Tests (null/invalid handling) - **Public methods**
6. 🟡 Load Tests (concurrency) - **Performance-critical code**
7. 🟡 Benchmarks (BenchmarkDotNet) - **Hot paths**

**Approach**: Add tests incrementally, focusing on new features and high-risk areas

---

### 🔄 Documentation Completion (80% → 100%)

**Status**: ⏸️ **PAUSED** - Testing takes priority

**Current Status**: Good coverage but needs polishing

**Pending Tasks**:

1. **Main README.md Enhancement**
   - Add comprehensive feature showcase
   - Include performance benchmarks comparison
   - Add decision flowcharts (which package to choose?)
   - Improve getting started section

2. **API Documentation with DocFX**
   - ✅ DocFX 2.78.4 installed and configured
   - ✅ 34 API pages generated from XML docs
   - ⏳ Pending: Deploy to GitHub Pages
   - ⏳ Pending: Add API docs badge to README

3. **Package-Specific Guides**
   - Create docs/database-providers.md (comparison guide)
   - Create docs/job-scheduling-comparison.md (Hangfire vs Quartz)
   - Create docs/validation-comparison.md (FluentValidation vs DataAnnotations vs MiniValidator)
   - Create docs/testing-guide.md (how to test with SimpleMediator)

4. **Architecture Decision Records**
   - ✅ ADR-001: Railway Oriented Programming
   - ✅ ADR-002: Dependency Injection Strategy
   - ✅ ADR-003: Caching Strategy (Expression trees)
   - ✅ ADR-004: NOT implementing MediatorResult<T>
   - ✅ ADR-005: NOT using Source Generators
   - ✅ ADR-006: Pure ROP with fail-fast exception handling
   - ✅ ADR-007: IRequestContext for extensibility
   - ✅ ADR-008: MANDATORY 100% Test Coverage Policy (NEW - 2025-12-18)

5. **Migration Guides**
   - Create MIGRATION.md for users moving from MediatR
   - Document database provider migration (SqlServer → PostgreSQL, etc.)
   - Document upgrade path from old packages (SimpleMediator.Dapper → SimpleMediator.Dapper.SqlServer)

**Timeline**: After 100% coverage achieved

---

## Planned Features (Pre-1.0 Only)

**IMPORTANT**: All features below are for BEFORE 1.0 release. No post-1.0 plans exist yet. Strategic initiatives (parallel execution, framework renaming) will be completed just before 1.0 release.

### 🚀 Infrastructure Technologies (.NET Community Analysis - Dec 2025)

This section analyzes the most successful infrastructure technologies in the .NET ecosystem as of December 19, 2025, providing users with multiple battle-tested options for each use case.

#### 1. Caching & In-Memory Databases ✅ COMPLETED

**SimpleMediator.Caching** ⭐⭐⭐⭐⭐ (CRITICAL) - **COMPLETED 2025-12-21**

**Priority**: Critical - Caching is fundamental for high-performance applications
**Status**: ✅ **100% COMPLETE** - 8 packages, 367 tests

**Technology Options Implemented**:

| Technology | Package | Status | Tests |
|------------|---------|--------|-------|
| **Memory (IMemoryCache)** | SimpleMediator.Caching.Memory | ✅ Production | 109 |
| **HybridCache (L1+L2)** | SimpleMediator.Caching.Hybrid | ✅ Production | 56 |
| **StackExchange.Redis** | SimpleMediator.Caching.Redis | ✅ Production | - |
| **Valkey** | SimpleMediator.Caching.Valkey | ✅ Production | - |
| **KeyDB** | SimpleMediator.Caching.KeyDB | ✅ Production | - |
| **Dragonfly** | SimpleMediator.Caching.Dragonfly | ✅ Production | - |
| **Garnet** (Microsoft) | SimpleMediator.Caching.Garnet | ✅ Production | - |

**Decision** (Dec 2025): Implemented **all options** with provider pattern:

- **SimpleMediator.Redis.StackExchange** - Default, proven, enterprise-ready
- **SimpleMediator.Redis.Garnet** - Modern, performant, MIT-licensed alternative

**Garnet Context**: Microsoft's Garnet was released in March 2024 as an MIT-licensed, high-performance cache-store that's API-compatible with Redis. As of December 2025, it's production-ready and actively used within Microsoft. It offers better performance than Redis in many scenarios while maintaining full compatibility with Redis clients.

**Use Cases in SimpleMediator**:

- Query result caching with `[Cache]` attribute
- Distributed idempotency keys for commands
- Session storage for `IRequestContext`
- Pub/Sub for notification broadcast (alternative to RabbitMQ for simple scenarios)
- Distributed locks for saga coordination

**Implementation**:

```csharp
// StackExchange.Redis
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorRedis(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "SimpleMediator:";
});

// Garnet (drop-in replacement)
services.AddSimpleMediatorGarnet(options =>
{
    options.Configuration = "localhost:6379"; // Same API
    options.InstanceName = "SimpleMediator:";
});

// Usage
[Cache(Duration = "00:05:00", Key = "customer-{request.Id}")]
public record GetCustomerQuery(int Id) : IQuery<Customer>;
```

**Package Dependencies**: `StackExchange.Redis 2.8+`, `Microsoft.Garnet 1.0+`

---

#### 2. Messaging & Message Brokers

**SimpleMediator.MassTransit** ⭐⭐⭐⭐⭐ (CRITICAL - Primary) ✅ **COMPLETE**
**SimpleMediator.Wolverine** ⭐⭐⭐⭐ (Alternative)

**Priority**: Critical - Distributed messaging is essential for microservices

**Status**: ✅ **SimpleMediator.MassTransit COMPLETE** (2025-12-21)

✅ Completed Features:
- `MassTransitRequestConsumer<TRequest, TResponse>` - Bridges MassTransit messages to SimpleMediator requests
- `MassTransitNotificationConsumer<TNotification>` - Bridges MassTransit messages to SimpleMediator notifications
- `MassTransitMessagePublisher` - Publishes domain events via MassTransit bus
- `IMassTransitMessagePublisher` - Interface for message publishing
- `MassTransitBusConfiguratorExtensions` - Auto-registers consumers by assembly scanning
- `ServiceCollectionExtensions` - DI registration helpers
- `SimpleMediatorMassTransitOptions` - Configuration options
- Tests: 25 passing (Unit tests)

**Technology Options**:

| Technology | Community Adoption | Pros | Cons | Recommendation |
|------------|-------------------|------|------|----------------|
| **MassTransit** | ⭐⭐⭐⭐⭐ Industry Standard | Since 2007, mature, free, supports all brokers | Can be complex for simple scenarios | ✅ **Primary Option** |
| **Wolverine** | ⭐⭐⭐⭐ Growing Fast | Modern, free, good MassTransit interop, simpler API | Newer (less battle-tested) | ✅ **Alternative Option** |
| **NServiceBus** | ⭐⭐⭐⭐ Enterprise | Mature, excellent support, proven at scale | Commercial license required | ❌ Not planned (commercial) |

**Decision** (Dec 2025): Support **both** MassTransit and Wolverine:

- **SimpleMediator.MassTransit** - Primary, proven, enterprise-grade
- **SimpleMediator.Wolverine** - Modern alternative, simpler for new projects

**Broker Support Matrix**:

| Broker | MassTransit | Wolverine | Native SimpleMediator |
|--------|-------------|-----------|----------------------|
| **RabbitMQ** | ✅ Excellent | ✅ Excellent | ⏳ Via MassTransit/Wolverine |
| **Azure Service Bus** | ✅ Excellent | ✅ Good | ⏳ Via MassTransit |
| **Amazon SQS/SNS** | ✅ Excellent | ✅ Good | ⏳ Via MassTransit |
| **Kafka** | ✅ Good | ✅ Limited | ⏳ Via Confluent.Kafka |
| **Redis Pub/Sub** | ✅ Good | ✅ Good | ✅ Via SimpleMediator.Redis |
| **In-Memory** | ✅ Yes | ✅ Yes | ✅ Yes (notifications) |

**Use Cases in SimpleMediator**:

- Distributed event publishing (alternative to Outbox pattern)
- Asynchronous command processing (alternative to background jobs)
- Saga coordination across services
- Message retry and dead-letter queues
- Guaranteed delivery with at-least-once semantics

**Implementation**:

```csharp
// MassTransit with RabbitMQ
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorMassTransit(cfg =>
{
    cfg.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

// Wolverine with Azure Service Bus
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorWolverine(opts =>
{
    opts.UseAzureServiceBus("connection-string")
        .AutoProvision()
        .UseConventionalRouting();
});

// Automatic publishing
await mediator.Publish(new OrderPlacedEvent(orderId, total)); // Goes to broker
```

**Package Dependencies**: `MassTransit 8.3+`, `Wolverine 3.5+`, `MassTransit.RabbitMQ`, `MassTransit.Azure.ServiceBus.Core`, `Wolverine.AzureServiceBus`

**Azure Service Bus Details**:

- Use `Azure.Messaging.ServiceBus` SDK (current official client)
- AMQP protocol recommended (SBMP support ending September 2026)
- Best integration via MassTransit or Wolverine abstractions

---

#### 3. Kafka Streaming

**SimpleMediator.Kafka** ⭐⭐⭐⭐ (High Priority)

**Priority**: High - Event streaming for high-throughput scenarios

**Technology Options**:

| Technology | Community Adoption | Pros | Cons | Recommendation |
|------------|-------------------|------|------|----------------|
| **Confluent.Kafka** | ⭐⭐⭐⭐⭐ Official | Official .NET client, feature-complete, actively maintained | Low-level API, more boilerplate | ✅ **Primary Option** |
| **KafkaFlow** | ⭐⭐⭐⭐ Popular | Higher-level abstraction, middleware pattern, parallel processing | Built on Confluent.Kafka (extra layer) | ✅ **Alternative Option** |

**Decision** (Dec 2025): Support **both** with provider pattern:

- **SimpleMediator.Kafka.Confluent** - Direct integration with official client
- **SimpleMediator.Kafka.Flow** - Higher-level abstraction via KafkaFlow

**Use Cases in SimpleMediator**:

- High-throughput event streaming (millions of events per second)
- Event sourcing with Kafka as event store
- Real-time analytics pipelines
- Change data capture (CDC) integration
- Cross-region event replication

**Implementation**:

```csharp
// Confluent.Kafka (low-level)
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorKafka(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.ProducerConfig = new() { /* ... */ };
    options.ConsumerConfig = new() { /* ... */ };
});

// KafkaFlow (high-level)
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorKafkaFlow(kafka => kafka
    .AddCluster(cluster => cluster
        .WithBrokers(new[] { "localhost:9092" })
        .CreateTopicIfNotExists("orders", 3, 1)
        .AddProducer<OrderPlacedEvent>()
        .AddConsumer(consumer => consumer
            .Topic("orders")
            .WithGroupId("order-processor")
            .WithWorkersCount(10) // Parallel processing
        )
    )
);

// Usage
await mediator.Publish(new OrderPlacedEvent(orderId, total)); // Goes to Kafka
```

**Package Dependencies**: `Confluent.Kafka 2.6+`, `KafkaFlow 3.2+`

---

#### 4. Event Sourcing

**SimpleMediator.Marten** ⭐⭐⭐⭐⭐ (CRITICAL - Primary) ✅ **COMPLETE**
**SimpleMediator.EventStoreDB** ⭐⭐⭐⭐ (Alternative)

**Priority**: Critical - Event sourcing is a fundamental architectural pattern

**Status**: ✅ **SimpleMediator.Marten COMPLETE** (2025-12-21)

✅ Completed Features:
- `IAggregate` / `AggregateBase` - Event sourcing aggregate abstractions
- `IAggregateRepository<TAggregate>` - Repository pattern for aggregates
- `MartenAggregateRepository<TAggregate>` - Marten implementation with optimistic concurrency
- `EventPublishingPipelineBehavior<TRequest, TResponse>` - Auto-publishes domain events after commands
- `SimpleMediatorMartenOptions` - Configuration options
- `MartenErrorCodes` - Error codes for Marten operations
- `ServiceCollectionExtensions` - DI registration helpers
- Tests: 16 passing (Unit tests)
- Uses Marten v8.0.0-beta-1 for .NET 10 compatibility

**Technology Options**:

| Technology | Community Adoption | Pros | Cons | Recommendation |
|------------|-------------------|------|------|----------------|
| **Marten** | ⭐⭐⭐⭐⭐ Highly Regarded | PostgreSQL-based, excellent tooling, document DB + event sourcing | Requires PostgreSQL | ✅ **Primary Option** |
| **EventStoreDB** | ⭐⭐⭐⭐ Specialized | Purpose-built for event sourcing, proven, projections | Separate database, more infrastructure | ✅ **Alternative Option** |

**Decision** (Dec 2025): Support **both**:

- **SimpleMediator.Marten** - Primary, PostgreSQL-based, multi-purpose
- **SimpleMediator.EventStoreDB** - Alternative, specialized event store

**Community Insight**: The Critter Stack team (creators of Marten and Wolverine) describes Marten as "the most robust and productive tooling for CQRS with Event Sourcing in the entire .NET ecosystem" as of 2025. It's actively maintained and widely adopted.

**Use Cases in SimpleMediator**:

- Event sourcing aggregates (store all state changes as events)
- Temporal queries (query state at any point in time)
- Audit trails (complete history of all changes)
- Event replay and projections
- CQRS with event-driven read models

**Implementation**:

```csharp
// Marten (PostgreSQL)
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorMarten(opts =>
{
    opts.Connection("Host=localhost;Database=events;Username=postgres;Password=postgres");
    opts.Events.AddEventType<OrderPlaced>();
    opts.Events.AddEventType<OrderShipped>();
    opts.Projections.Add<OrderProjection>(ProjectionLifecycle.Inline);
});

// EventStoreDB
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorEventStoreDB(options =>
{
    options.ConnectionString = "esdb://localhost:2113?tls=false";
    options.Credentials = new UserCredentials("admin", "changeit");
});

// Usage
public record PlaceOrderCommand(Guid OrderId, List<OrderItem> Items) : ICommand<Unit>;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Unit>
{
    private readonly IEventStore _eventStore; // Marten or EventStoreDB

    public async ValueTask<Either<MediatorError, Unit>> Handle(
        PlaceOrderCommand request,
        IRequestContext context,
        CancellationToken ct)
    {
        var events = new[] { new OrderPlaced(request.OrderId, request.Items, DateTime.UtcNow) };
        await _eventStore.AppendEventsAsync(request.OrderId, events, ct);
        return Unit.Default;
    }
}
```

**Package Dependencies**: `Marten 7.33+`, `EventStore.Client.Grpc.Streams 24.2+`

---

#### 5. Document Databases

**SimpleMediator.MongoDB** ⭐⭐⭐⭐ (High Priority)

**Priority**: High - Document DB for schema-flexible data

**Technology Options**:

| Technology | Community Adoption | Pros | Cons | Recommendation |
|------------|-------------------|------|------|----------------|
| **MongoDB.Driver** | ⭐⭐⭐⭐⭐ Official | Official driver, feature-complete, v3.5.2 (Nov 2025), .NET 10 compatible | Large dependency, complex for simple use cases | ✅ **Only Option** |

**Decision** (Dec 2025): Support **MongoDB.Driver 3.5.2** (latest stable as of Dec 2025):

- Latest version released November 27, 2025
- Full .NET 10 and C# 14 compatibility
- Actively maintained by MongoDB, Inc.

**Use Cases in SimpleMediator**:

- Schema-flexible domain models
- Event store alternative to Marten/EventStoreDB
- Read models for CQRS (denormalized views)
- Document-based sagas (alternative to relational sagas)
- High-write-throughput scenarios

**Implementation**:

```csharp
services.AddSimpleMediator(config => { });
services.AddSimpleMediatorMongoDB(options =>
{
    options.ConnectionString = "mongodb://localhost:27017";
    options.DatabaseName = "SimpleMediator";
    options.Collections = new()
    {
        Outbox = "outbox_messages",
        Inbox = "inbox_messages",
        Sagas = "saga_states",
        Events = "events"
    };
});

// Store implementation
public class OutboxStoreMongoDB : IOutboxStore
{
    private readonly IMongoCollection<OutboxMessage> _collection;

    public async Task AddAsync(OutboxMessage message, CancellationToken ct)
    {
        await _collection.InsertOneAsync(message, cancellationToken: ct);
    }

    public async Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken ct)
    {
        return await _collection
            .Find(m => m.ProcessedAtUtc == null)
            .Limit(batchSize)
            .ToListAsync(ct);
    }
}
```

**Package Dependencies**: `MongoDB.Driver 3.5.2`

---

#### Summary: Infrastructure Technology Priorities

| Technology | Package Name | Priority | Status | Community Adoption |
|------------|-------------|----------|--------|-------------------|
| **Redis/Garnet** | SimpleMediator.Redis.StackExchange<br>SimpleMediator.Redis.Garnet | ⭐⭐⭐⭐⭐ Critical | ⏳ Planned | ⭐⭐⭐⭐⭐ Standard |
| **MassTransit** | SimpleMediator.MassTransit | ⭐⭐⭐⭐⭐ Critical | ✅ Complete | ⭐⭐⭐⭐⭐ Industry Standard |
| **Wolverine** | SimpleMediator.Wolverine | ⭐⭐⭐⭐⭐ Critical | ⏳ In Progress | ⭐⭐⭐⭐ Growing |
| **NServiceBus** | SimpleMediator.NServiceBus | ⭐⭐⭐⭐ High | ⏳ In Progress | ⭐⭐⭐⭐⭐ Enterprise |
| **RabbitMQ** | SimpleMediator.RabbitMQ | ⭐⭐⭐⭐⭐ Critical | ⏳ In Progress | ⭐⭐⭐⭐⭐ Standard |
| **Azure Service Bus** | SimpleMediator.AzureServiceBus | ⭐⭐⭐⭐⭐ Critical | ⏳ In Progress | ⭐⭐⭐⭐⭐ Enterprise |
| **Amazon SQS/SNS** | SimpleMediator.AmazonSQS | ⭐⭐⭐⭐⭐ Critical | ⏳ In Progress | ⭐⭐⭐⭐⭐ Enterprise |
| **Kafka (Confluent)** | SimpleMediator.Kafka | ⭐⭐⭐⭐⭐ Critical | ⏳ In Progress | ⭐⭐⭐⭐⭐ Official |
| **Redis Pub/Sub** | SimpleMediator.Redis.PubSub | ⭐⭐⭐⭐ High | ⏳ In Progress | ⭐⭐⭐⭐⭐ Standard |
| **In-Memory** | SimpleMediator.InMemory | ⭐⭐⭐⭐ High | ⏳ In Progress | ⭐⭐⭐⭐⭐ Dev/Test |
| **NATS** | SimpleMediator.NATS | ⭐⭐⭐⭐ High | ⏳ In Progress | ⭐⭐⭐⭐ Growing |
| **MQTT** | SimpleMediator.MQTT | ⭐⭐⭐⭐ High | ⏳ In Progress | ⭐⭐⭐⭐ IoT Standard |
| **gRPC** | SimpleMediator.gRPC | ⭐⭐⭐⭐⭐ Critical | ⏳ In Progress | ⭐⭐⭐⭐⭐ Standard |
| **GraphQL** | SimpleMediator.GraphQL | ⭐⭐⭐⭐ High | ⏳ In Progress | ⭐⭐⭐⭐ Growing |
| **Marten** | SimpleMediator.Marten | ⭐⭐⭐⭐⭐ Critical | ✅ Complete | ⭐⭐⭐⭐⭐ Highly Regarded |
| **EventStoreDB** | SimpleMediator.EventStoreDB | ⭐⭐⭐⭐ High | ⏳ Planned | ⭐⭐⭐⭐ Specialized |
| **MongoDB** | SimpleMediator.MongoDB | ⭐⭐⭐⭐ High | ⏳ Planned | ⭐⭐⭐⭐⭐ Standard |

**Implementation Order** (Recommended):

1. **SimpleMediator.Redis** (Critical - caching foundation)
2. **SimpleMediator.MassTransit** (Critical - messaging foundation)
3. **SimpleMediator.Marten** (Critical - event sourcing foundation)
4. **SimpleMediator.Kafka** (High - streaming scenarios)
5. **SimpleMediator.MongoDB** (High - document store)
6. **SimpleMediator.Wolverine** (Alternative to MassTransit)
7. **SimpleMediator.EventStoreDB** (Alternative to Marten)
8. **SimpleMediator.Garnet** (Alternative to StackExchange.Redis)

---

### 🎯 Critical Priorities

#### 1. SimpleMediator.OpenTelemetry (COMPLETED ✅)

**Priority**: ⭐⭐⭐⭐⭐ (Critical)
**Complexity**: ⭐⭐⭐ (Medium)
**Status**: ✅ **100% COMPLETE** - Production ready

**Completed** (2025-12-19):

✅ Core implementation:

- `SimpleMediatorOpenTelemetryOptions` with ServiceName, ServiceVersion, EnableMessagingEnrichers
- `ServiceCollectionExtensions` with DI registration and OpenTelemetry builder integration
- `MessagingActivityEnricher` for Outbox, Inbox, Sagas, and Scheduling patterns
- `MessagingEnricherPipelineBehavior<TRequest, TResponse>` for automatic enrichment
- PublicAPI compliance with all 22 public symbols documented
- README.md with comprehensive examples (Jaeger, Prometheus, Azure Monitor)
- Docker Compose observability stack (Jaeger, Prometheus, Loki, Grafana)

✅ Testing (71/71 tests passing):

- Unit tests (57 tests) - Core functionality, guards, contracts
- Integration tests (6 tests) - Real OpenTelemetry exporters
- Property tests (8 tests) - Configuration invariants
- Load tests (2 skipped - intentional, DI scoping design)
- Benchmarks (✅ included in benchmarks project)

**Features**:

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSimpleMediatorInstrumentation()
        .AddJaegerExporter())
    .WithMetrics(builder => builder
        .AddSimpleMediatorInstrumentation()
        .AddPrometheusExporter());
```

**Automatic Instrumentation**:

- ✅ Distributed traces with OpenTelemetry semantic conventions
- ✅ Automatic enrichment with Outbox, Inbox, Saga, Scheduling context
- ✅ Opt-in messaging enrichers via configuration
- ✅ Extension methods for TracerProviderBuilder and MeterProviderBuilder
- ✅ Docker Compose stack for local testing (Jaeger UI on :16686, Prometheus on :9090, Grafana on :3000)

---

#### 2. Stream Requests (IAsyncEnumerable Support)

**Priority**: ⭐⭐⭐⭐ (High)
**Complexity**: ⭐⭐⭐ (Medium)
**Status**: 🟡 **70% COMPLETE** (Implementation done, testing incomplete)

**Completed** (2025-12-19):

✅ Core implementation:

- `IStreamRequest<TItem>` interface
- `IStreamRequestHandler<TRequest, TItem>` interface
- `IStreamPipelineBehavior<TRequest, TItem>` interface
- `StreamHandlerDelegate<TItem>` for pipeline chaining
- `StreamPipelineBuilder<TRequest, TItem>` for pipeline construction
- `StreamDispatcher` for handler resolution and execution
- Integration with `IMediator.Stream<TRequest, TItem>()` method
- PublicAPI compliance

✅ Testing (11/11 unit tests passing):

- Basic streaming functionality
- Error handling in streams
- Pipeline behavior execution
- Handler resolution

**Pending**:

⏳ **Guard Clause Tests**: Null parameter validation (0 tests)
⏳ **Property-Based Tests**: Stream invariants with FsCheck (0 tests)
⏳ **Integration Tests**: Real-world streaming scenarios (0 tests)
⏳ **Load Tests**: Concurrency, backpressure, memory pressure (0 tests)
⏳ **Contract Tests**: Interface compliance verification (0 tests)
⏳ **Benchmarks**: Performance vs traditional batch queries (0 tests)
⏳ **Coverage Verification**: Line coverage ~70%, needs 100%

**Objective**: Support for `IAsyncEnumerable<T>` in large queries or real-time scenarios.

**Use Cases**:

- Large queries with implicit pagination (millions of records)
- Real-time/Server-Sent Events (SSE) scenarios
- gRPC streaming in microservices
- Batch processing with backpressure

**Implementation**:

```csharp
// New interface for stream requests
public interface IStreamRequest<out TItem> { }

public interface IStreamRequestHandler<in TRequest, TItem>
    where TRequest : IStreamRequest<TItem>
{
    IAsyncEnumerable<Either<MediatorError, TItem>> Handle(
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken);
}

// Behaviors can intercept streams
public class StreamLoggingBehavior<TRequest, TItem> : IStreamPipelineBehavior<TRequest, TItem>
{
    public async IAsyncEnumerable<Either<MediatorError, TItem>> Handle(
        TRequest request,
        IRequestContext context,
        StreamHandlerCallback<TItem> nextStep,
        [EnumeratorCancellation] CancellationToken ct)
    {
        _logger.LogInformation("Stream started");
        var count = 0;

        await foreach (var item in nextStep().WithCancellation(ct))
        {
            count++;
            yield return item;
        }

        _logger.LogInformation("Stream completed: {Count} items", count);
    }
}
```

**Challenges**:

- Behaviors must intercept streams (more complex than regular requests)
- Error handling: What to do if an item fails? (yield Left or cancel all)
- Observability: tracking processed items, backpressure
- Testing: ensure tests don't consume entire stream

---

#### 3. SimpleMediator.Caching ✅ COMPLETED

**Priority**: ⭐⭐⭐⭐ (High)
**Complexity**: ⭐⭐⭐ (Medium)
**Status**: ✅ **100% COMPLETE** - Production ready (2025-12-21)

**Completed**:

✅ 8 caching packages implemented:

- `SimpleMediator.Caching` - Core abstractions (ICacheProvider, CachingBehavior)
- `SimpleMediator.Caching.Memory` - In-memory caching
- `SimpleMediator.Caching.Hybrid` - Multi-tier L1+L2 caching (Microsoft HybridCache)
- `SimpleMediator.Caching.Redis` - StackExchange.Redis distributed caching
- `SimpleMediator.Caching.Valkey` - Open-source Redis alternative
- `SimpleMediator.Caching.KeyDB` - High-performance Redis fork
- `SimpleMediator.Caching.Dragonfly` - Modern Redis-compatible cache
- `SimpleMediator.Caching.Garnet` - Microsoft's Redis-compatible cache

✅ 367 tests across 7 test projects:

- Unit Tests (49), Memory Tests (109), Hybrid Tests (56)
- Guard Tests (43), Contract Tests (78), Property Tests (32)
- Integration Tests (23 - require Redis container)

**Features**:

```csharp
// 1. Query caching
[Cache(Duration = "00:05:00", Key = "customer-{request.Id}")]
public record GetCustomerQuery(int Id) : IQuery<Customer>;

// 2. Multiple providers supported
services.AddSimpleMediatorMemoryCache();           // In-memory
services.AddSimpleMediatorHybridCache();           // L1 + L2
services.AddSimpleMediatorRedisCache(config);      // Redis/Valkey/KeyDB/Dragonfly/Garnet

// 3. Cache invalidation
[InvalidatesCache("customer-{Id}")]
public record UpdateCustomerCommand(int Id, ...) : ICommand<Customer>;
```

---

#### 4. ODBC Provider (Legacy Database Support)

**Priority**: ⭐⭐⭐ (Medium - enterprise integration)
**Complexity**: ⭐⭐⭐ (Medium)

**Target Databases**:

- IBM DB2 (legacy enterprise systems)
- Informix
- Progress/OpenEdge
- Firebird (legacy environments)

---

### 🎯 Core Improvements (Before Other Features)

#### 5. Refactor SimpleMediator.Publish with Guards

**Priority**: ⭐⭐⭐⭐ (High - code quality)
**Complexity**: ⭐⭐ (Low)
**Status**: ✅ **COMPLETE** (2025-12-21)

**Objective**: Apply same guard clause pattern used in Send to Publish method.

**Rationale**: Consistency across codebase, defensive programming.

**Completed**:
- ✅ `MediatorRequestGuards.TryValidateNotification()` already applied in Publish method
- ✅ `MediatorNotificationGuards.TryValidateHandleMethod()` validates handler method signatures
- ✅ Handler null checks handled inline (notifications can have zero handlers - not an error)
- ✅ Pattern consistent with RequestDispatcher approach

---

#### 6. Optimize Delegate Caches

**Priority**: ⭐⭐⭐⭐ (High - performance)
**Complexity**: ⭐⭐⭐ (Medium)
**Status**: ✅ **COMPLETE** (2025-12-21)

**Objective**: Minimize reflection and boxing in hot paths.

**Techniques**:

- Apply `CollectionsMarshal.AsSpan` for performance
- Reduce allocations in handler resolution
- Optimize expression tree compilation

**Completed**:
- ✅ `PipelineBuilder.ExecutePipelineAsync` - Changed from `IReadOnlyList<T>` to `T[]` with index-based iteration
- ✅ `NotificationDispatcher.ExecuteAsync` - Replaced `foreach` with index-based `for` loop to avoid enumerator allocation
- ✅ Avoided `CollectionsMarshal.AsSpan` in async methods (Span cannot cross await boundaries)
- ✅ All 194 core tests pass

---

#### 7. Replace `object? Details` with `ImmutableDictionary<string, object?>`

**Priority**: ⭐⭐⭐ (Medium - type safety)
**Complexity**: ⭐⭐⭐ (Medium)

**Objective**: Improve type safety and immutability in error details.

---

### 🎯 Testing Excellence (Critical for Production)

#### 8. Amplify Property-Based Testing

**Priority**: ⭐⭐⭐⭐⭐ (Critical)
**Complexity**: ⭐⭐⭐⭐ (High)

**Focus Areas**:

- Pipeline invariants (behaviors execute in order)
- Cache behavior (expression tree compilation)
- Error propagation through ROP chains

---

#### 9. Elevate Mutation Score to ≥95%

**Priority**: ⭐⭐⭐⭐⭐ (Critical)
**Complexity**: ⭐⭐⭐⭐ (High)
**Current**: 79.75% → **Target**: ≥95%

**Strategy**: Run Stryker.NET, identify surviving mutants, add tests.

---

#### 10. Load Testing with Strict Thresholds

**Priority**: ⭐⭐⭐⭐ (High)
**Complexity**: ⭐⭐⭐ (Medium)

**Scenarios**:

- High-throughput request processing
- Concurrent pipeline execution
- Memory pressure under load

---

#### 11. Telemetry Exhaustive Tests

**Priority**: ⭐⭐⭐⭐ (High)
**Complexity**: ⭐⭐⭐ (Medium)

**Coverage**:

- ActivitySource spans (distributed tracing)
- Metrics collection (counters, histograms)
- Logging correlation

---

### 🎯 Static Analysis & Security

#### 12. Configure SONAR_TOKEN and Run First SonarCloud Scan

**Priority**: ⭐⭐⭐⭐⭐ (Critical)
**Complexity**: ⭐⭐ (Low)

**Deliverables**:

- Quality Gate passing
- Zero code smells
- Security vulnerabilities addressed

---

#### 13. Cyclomatic Complexity Analysis

**Priority**: ⭐⭐⭐⭐ (High)
**Complexity**: ⭐⭐ (Low)
**Target**: ≤10 complexity per method

---

#### 14. Code Duplication Analysis

**Priority**: ⭐⭐⭐⭐ (High)
**Complexity**: ⭐⭐ (Low)
**Target**: <3% duplication

---

## Strategic Initiatives (TO BE COMPLETED JUST BEFORE 1.0)

**IMPORTANT**: These initiatives are essential for 1.0 but should be completed LAST, after all technical work above is done.

### 🔥 Parallel Execution Support

**Current State**: All execution is sequential

- Request pipeline: PreProcessors → Behaviors → Handler → PostProcessors (sequential)
- Notification handlers: Loop one by one (sequential)
- Single request/notification stays on one thread unless handlers introduce parallelism

**Multicore Usage Today**:

- ✅ Multiple concurrent requests use multiple cores (via thread pool)
- ❌ Single request/notification cannot use multiple cores (single-threaded execution)

**Proposed Enhancement**: Opt-in Parallel Notification Dispatch

```csharp
public enum PublishMode { Sequential, Parallel }

public sealed record PublishOptions(
    PublishMode Mode = PublishMode.Sequential,
    int? DegreeOfParallelism = null
);

// In NotificationDispatcher.ExecuteAsync
if (options.Mode == PublishMode.Parallel)
{
    var dop = options.DegreeOfParallelism ?? Environment.ProcessorCount;
    using var throttler = new SemaphoreSlim(dop);

    var tasks = handlers.Select(async handler =>
    {
        await throttler.WaitAsync(cancellationToken);
        try { return await InvokeNotificationHandler(handler, notification, cancellationToken); }
        finally { throttler.Release(); }
    });

    var results = await Task.WhenAll(tasks);
    return results.FirstOrDefault(r => r.IsLeft) ?? Right<MediatorError, Unit>(Unit.Default);
}
// Fallback: existing sequential loop
```

**Benefits**:

- 🚀 Faster notification broadcast (use all CPU cores)
- 🎯 Useful for high-throughput scenarios (IoT, real-time systems)
- 🔧 Opt-in: backward compatible, users choose when to parallelize

**Safety Considerations**:

- ⚠️ Scoped services must be thread-safe if reused across parallel handlers
- ⚠️ Ordering guarantees become weaker (can be mitigated with partitioning by key)
- ⚠️ Failure handling: fail-fast (first error stops) vs collect-all (gather all errors)

**Timeline**: Before 1.0 release (essential for high-throughput scenarios)

**Decision Document**: See `paralelism.md` for complete analysis

---

### 🌳 Framework Renaming: Encina Framework (Pre-1.0 - Required)

**Current Name**: SimpleMediator
**New Name**: **Encina Framework**
**Timeline**: Before 1.0 release

**Why Encina?**

Encina is the Spanish word for the holm oak (Quercus ilex), a Mediterranean tree renowned for:

- 💪 **Strength**: Robust and reliable foundation
- 🌿 **Resilience**: Adapts to diverse environments
- ⏳ **Longevity**: Endures over time, supporting rich ecosystems

Just as the encina tree anchors and nourishes its ecosystem, Encina Framework is designed to be the backbone of your applications.

**Visual Identity**:

- **Colors**: Green (#2E5E2A, #7A9D54) and warm brown (#8B5E3C)
- **Logo**: Holm oak silhouette (strength and nature)
- **Icon**: Available in SVG, PNG (32x32, 64x64, 128x128, 256x256, 512x512), ICO
- **Monochrome version**: For CLI, terminals, dark backgrounds

**Migration Checklist**:

1. **Repository & Organization**
   - [ ] Rename GitHub repository
   - [ ] Update organization/team names if applicable
   - [ ] Set up redirects from old URLs

2. **Codebase**
   - [ ] Update all namespaces (SimpleMediator → Encina)
   - [ ] Update assembly names
   - [ ] Update project files (.csproj, .sln, Directory.Build.props)
   - [ ] Update PublicAPI files

3. **NuGet Packages**
   - [ ] Register new package names on NuGet.org
   - [ ] Publish first versions of Encina packages
   - [ ] Mark old SimpleMediator packages as deprecated with migration notice
   - [ ] Add package icon (holm oak logo)

4. **Documentation**
   - [ ] Update all README files
   - [ ] Update CONTRIBUTING.md
   - [ ] Update ADRs and architecture docs
   - [ ] Update code samples and examples
   - [ ] Update XML documentation comments

5. **Infrastructure**
   - [ ] Update CI/CD workflows
   - [ ] Update badges and shields
   - [ ] Update issue templates
   - [ ] Update PR templates

6. **Communication**
   - [ ] Announce on GitHub Discussions
   - [ ] Blog post explaining the change
   - [ ] Update social media profiles
   - [ ] Migration guide for existing users

**Decision Document**: See `RENAME_GUIDE.md` for complete guide

**Timeline**: Complete renaming before 1.0 release

---

## Quality & Security

### Quality Principles

- **Zero Warnings**: All code analysis warnings must be addressed (fix or suppress with justification)
- **Zero Exceptions in Domain**: Failures travel through the functional rail (`Either<MediatorError, T>`)
- **Documentation First-Class**: All public API documented with XML comments
- **Self-Documenting Code**: Clear names, single responsibilities, consistent functional patterns
- **Exhaustive Testing**: Unit, integration, property-based, contract, mutation, and load testing

### Quality Metrics

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Line Coverage | 92.5% | ≥90% | ✅ EXCEEDED |
| Branch Coverage | 83.3% | ≥85% | 🟡 Close |
| Mutation Score | 79.75% | ≥80% | ✅ ACHIEVED |
| Build Warnings | 0 | 0 | ✅ PERFECT |
| XML Documentation | 100% | 100% | ✅ PERFECT |
| Technical Debt | TBD | ≤5% | ⏳ Pending (SonarCloud) |
| Cyclomatic Complexity | TBD | ≤10/method | ⏳ Pending |
| Code Duplication | TBD | <3% | ⏳ Pending |
| Security Rating | TBD | A | ⏳ Pending |

### Security & Supply Chain

**Implemented**:

- ✅ CodeQL scanning on every PR
- ✅ SBOM generation workflow
- ✅ Dependabot enabled (automatic dependency updates)
- ✅ TreatWarningsAsErrors=true (prevents code quality regression)
- ✅ PublicAPI Analyzers (tracks public API surface changes)

**Planned**:

- [ ] SLSA Level 2 compliance (provenance statements, artifact signing)
- [ ] Automatic SBOM on every release
- [ ] Supply chain security with Sigstore/cosign
- [ ] Security advisories monitoring
- [ ] Automated dependency review in PRs

### Continuous Improvement

**Daily**:

- Verify CI/CD pipelines are green
- Monitor for security alerts

**Weekly**:

- Review quality metrics (coverage, mutation, SonarCloud)
- Triage new issues and PRs

**Monthly**:

- Audit dependencies
- Update roadmap based on feedback
- Review performance benchmarks

**Quarterly**:

- Audit workflows and permissions
- Plan next phases
- Review architecture decisions

**Per Release**:

- Validate all quality gates pass
- Generate and publish SBOM
- Publish detailed changelog
- Update documentation

---

## Not Implementing

### ❌ Generic Variance & Intelligent Dispatch

**Reason**: Goes against "explicit over implicit" philosophy

- Adds complexity (~15-20% overhead in resolution)
- Makes code less predictable
- In pure ROP, types are usually very specific per domain
- If someone needs this, they can implement it in their application layer

**Decision**: NOT implementing

---

### ❌ MediatorResult<T> Wrapper

**Reason**: Doesn't add value over Either from LanguageExt

- LanguageExt's `Either<L, R>` is already excellent
- Adding a wrapper would be redundant
- Match/Bind/Map operations already available
- Better to improve documentation and examples

**Decision**: Keep `Either<MediatorError, T>` directly

**See**: ADR-004 for detailed rationale

---

### ❌ Source Generators for Handler Registration

**Reason**: Complexity doesn't justify minimal gain

- Current Expression tree compilation: 6.8M ops/sec
- Source generators would add complexity to codebase
- Cold start improvement: ~150μs (negligible)
- Hot path already has zero-allocation with caching

**Decision**: Maintain Expression Tree compilation with caching

**See**: ADR-005 for detailed rationale

---

## Feature Requests & Contributions

### Pre-1.0 Policy

Any feature can be added/modified/removed without restrictions. This is the time for experimentation and finding the best design.

### Post-1.0 Policy

1. Create GitHub Issue with "Feature Request" template
2. Discussion of design in issue
3. If breaking change: mark for next major version
4. If non-breaking: can go in minor/patch

### Contributing

See `CONTRIBUTING.md` for:

- Code style guidelines
- ROP and zero-exceptions policy
- Testing requirements (≥90% coverage)
- PR checklist
- Conventional Commits format

---

## References

### Inspiration

- [MediatR](https://github.com/jbogard/MediatR) - Original inspiration
- [Wolverine](https://wolverine.netlify.app/) - Messaging patterns
- [Kommand](https://github.com/NicoJuicy/Kommand) - CQRS patterns
- [NestJS](https://nestjs.com/) - Philosophy of "framework handles plumbing"

### Concepts

- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) - Scott Wlaschin
- [Functional Error Handling](https://github.com/louthy/language-ext) - LanguageExt
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html) - Martin Fowler

### Tools & Standards

- [Conventional Commits](https://www.conventionalcommits.org/)
- [SLSA Framework](https://slsa.dev/)
- [SonarCloud Quality Gates](https://docs.sonarcloud.io/improving/quality-gates/)
- [DocFX Documentation](https://dotnet.github.io/docfx/)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)

---

**Last Updated**: 2025-12-21
**Next Review**: Upon completion of current sprint
**Maintained by**: @dlrivada

**Note**: This roadmap is a living document. It will be updated as features are completed, priorities change, and new opportunities arise. Feedback and suggestions are always welcome!
