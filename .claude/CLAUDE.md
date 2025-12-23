# Claude Code - Encina Guidelines

## Project Philosophy

### Pre-1.0 Development Status

- **Current Phase**: Pre-1.0 - Initial Design & Architecture
- **No Backward Compatibility Required**: We are NOT maintaining backward compatibility
- **Breaking Changes**: Fully acceptable and encouraged if they improve the design
- **Migration Support**: NOT needed - no existing users to migrate
- **Final Name Change**: The library will be renamed in the last step (post-1.0)

### Design Principles

1. **Best Solution First**: Always choose the best technical solution, never compromise for compatibility
2. **Clean Architecture**: No legacy code, no deprecated features, no obsolete properties
3. **Pay-for-What-You-Use**: All features are opt-in, never forced on users
4. **Provider-Agnostic**: Use abstractions to support multiple implementations (EF Core, Dapper, ADO.NET)
5. **.NET 10 Only**: We use .NET 10 exclusively (very recent, stable release)

### Technology Stack

- **.NET Version**: .NET 10.0 (mandatory, no support for older versions)
- **Language Features**: Use latest C# features without hesitation
- **Breaking Changes**: Expected and acceptable in .NET 10 APIs
- **Nullable Reference Types**: Enabled everywhere

### Code Quality Standards

- **No Obsolete Attributes**: Never mark code as `[Obsolete]` for backward compatibility
- **No Legacy Code**: If we need to change something, we change it completely
- **No Migration Paths**: Don't implement migration helpers or compatibility layers
- **Clean Codebase**: Every line of code should serve a current purpose

### Architecture Decisions

#### Railway Oriented Programming (ROP)

- Core pattern: `Either<EncinaError, T>`
- Explicit error handling, no exceptions for business logic
- Validation returns `Either` with detailed errors

#### Messaging Patterns (All Optional)

1. **Outbox Pattern**: Reliable event publishing (at-least-once delivery)
2. **Inbox Pattern**: Idempotent message processing (exactly-once semantics)
3. **Saga Pattern**: Distributed transactions with compensation (orchestration-based)
4. **Scheduled Messages**: Delayed/recurring command execution
5. **Transactions**: Automatic database transaction management

#### Provider Coherence

- **Encina.Messaging**: Shared abstractions (IOutboxStore, IInboxStore, etc.)
- **Encina.EntityFrameworkCore**: EF Core implementation
- **Encina.Dapper**: Future - Dapper implementation
- **Encina.Data**: Future - ADO.NET implementation
- Same interfaces, different implementations - easy to switch providers

#### Opt-In Configuration

All messaging patterns are disabled by default:

```csharp
// Simple app - only what you need
config.UseTransactions = true;

// Complex distributed system - all patterns
config.UseTransactions = true;
config.UseOutbox = true;
config.UseInbox = true;
config.UseSagas = true;
config.UseScheduling = true;
```

### Naming Conventions

#### Messaging Entities

- **Outbox**: `OutboxMessage` (not Message)
- **Inbox**: `InboxMessage` (not Message)
- **Saga**: `SagaState` (not Saga)
- **Scheduling**: `ScheduledMessage` (not ScheduledCommand)

#### Property Names (Standardized)

- **Type Information**: `RequestType` or `NotificationType` (not MessageType)
- **Error Information**: `ErrorMessage` (not Error - avoids CA1716 keyword conflict)
- **Timestamps**: Always UTC with `AtUtc` suffix
  - `CreatedAtUtc`, `ProcessedAtUtc`, `ScheduledAtUtc`, etc.
  - **Saga timestamps**: `StartedAtUtc`, `LastUpdatedAtUtc`, `CompletedAtUtc`
- **Retry Logic**: `RetryCount`, `NextRetryAtUtc` (not AttemptCount)
- **Identifiers**: Descriptive names (`SagaId` not `Id` when implementing interface)

#### Store Implementations

- Pattern: `{Pattern}Store{Provider}`
- Examples: `OutboxStoreEF`, `InboxStoreEF`, `SagaStoreEF`
- Never just `Store` or `Repository`

### Satellite Packages Philosophy

#### Coherence Across Providers

When implementing the same feature across different data access providers:

- **Same interfaces** (from Encina.Messaging)
- **Same configuration options** (from Encina.Messaging)
- **Different implementations** (provider-specific)
- **Easy migration** (change DI registration, rest stays the same)

Example:

```csharp
// Using EF Core
services.AddEncinaEntityFrameworkCore(config => {
    config.UseOutbox = true;
});

// Switch to Dapper (same interface, different implementation)
services.AddEncinaDapper(config => {
    config.UseOutbox = true; // Same configuration!
});
```

#### Validation Libraries Support

- Support multiple: FluentValidation, DataAnnotations, MiniValidator
- User chooses their preferred library
- Similar pattern for scheduling: Encina.Scheduling vs Hangfire/Quartz adapters

#### Validation Architecture (Orchestrator Pattern)

```
Encina (core)
├── Encina.Validation.IValidationProvider (interface)
├── Encina.Validation.ValidationOrchestrator (domain logic)
├── Encina.Validation.ValidationPipelineBehavior<,> (centralized behavior)
├── Encina.Validation.ValidationResult (immutable result)
└── Encina.Validation.ValidationError (record)

Encina.FluentValidation / DataAnnotations / MiniValidator
├── *ValidationProvider (implements IValidationProvider)
└── ServiceCollectionExtensions (registers orchestrator + provider)
```

Example:
```csharp
// All validation packages use the same pattern:
services.AddEncinaFluentValidation(typeof(MyValidator).Assembly);
// or
services.AddDataAnnotationsValidation();
// or
services.AddMiniValidation();

// Each registers:
// - IValidationProvider → Provider-specific implementation
// - ValidationOrchestrator → Centralized orchestration
// - ValidationPipelineBehavior<,> → Generic behavior
```

### Testing Standards

Maintain high-quality test coverage that balances thoroughness with development velocity.

#### Coverage Targets

- **Line Coverage**: ≥85% (target for overall codebase)
- **Branch Coverage**: ≥80% (target for overall codebase)
- **Method Coverage**: ≥90% (target for overall codebase)
- **Mutation Score**: ≥80% (Stryker mutation testing)

#### Test Types - Apply Where Appropriate

Choose test types based on risk and value. Not every piece of code needs all test types:

1. **Unit Tests** ✅ (Required for all code)
   - Test individual methods in isolation
   - Mock all dependencies
   - Fast execution (<1ms per test)
   - Location: `tests/{Package}.Tests/`

2. **Integration Tests** 🟡 (Critical paths, database operations)
   - Test against real databases (via Docker/Testcontainers)
   - Test full workflows end-to-end
   - Mark with: `[Trait("Category", "Integration")]`
   - Location: `tests/{Package}.IntegrationTests/`

3. **Contract Tests** 🟡 (Public APIs, interfaces)
   - Verify public API contracts don't break
   - Test interfaces, abstract classes
   - Location: `tests/{Package}.ContractTests/`

4. **Property-Based Tests** 🟡 (Complex logic, invariants)
   - Use FsCheck to generate random inputs
   - Verify invariants hold for varied inputs
   - Location: `tests/{Package}.PropertyTests/`

5. **Guard Clause Tests** 🟡 (Public methods with parameters)
   - Verify null checks throw `ArgumentNullException`
   - Use GuardClauses.xUnit library
   - Location: `tests/{Package}.GuardTests/`

6. **Load Tests** 🟡 (Performance-critical, concurrent code)
   - Stress test under high concurrency
   - Location: `load/Encina.LoadTests/`

7. **Benchmarks** 🟡 (Hot paths, performance comparisons)
   - Measure actual performance with BenchmarkDotNet
   - Location: `benchmarks/Encina.Benchmarks/`

#### Test Quality Standards

**Good tests should**:

- Have a clear, descriptive name (no `Test1`, `Test2`)
- Follow AAA pattern (Arrange, Act, Assert)
- Test ONE thing (single responsibility)
- Be independent (no shared state between tests)
- Be deterministic (same input = same output, always)
- Clean up resources (dispose, delete temp files, etc.)

**Avoid**:

- Skipping tests without justification
- Ignoring flaky tests (fix or delete them)
- Testing implementation details (test behavior, not internals)
- Using `Thread.Sleep` (prefer proper synchronization)
- Hard-coding paths, dates, GUIDs when avoidable

#### Docker Integration Testing

For database-dependent code, integration tests using Docker/Testcontainers are recommended:

```csharp
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class OutboxStoreSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabase _db;

    public OutboxStoreSqlServerIntegrationTests()
    {
        _db = new TestDatabase("Server=localhost,1433;...");
    }

    public async Task InitializeAsync()
    {
        await _db.CreateSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DropSchemaAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistMessage()
    {
        // Arrange
        var store = new OutboxStoreDapper(_db.Connection);
        var message = new OutboxMessage { ... };

        // Act
        await store.AddAsync(message);

        // Assert
        var retrieved = await store.GetMessageAsync(message.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(message.Payload, retrieved.Payload);
    }
}
```

Run with:

```bash
dotnet run --file scripts/run-integration-tests.cs
```

#### Test Organization

```
tests/
├── Encina.Tests/              # Unit tests for core
│   ├── EncinaTests.cs
│   ├── PipelineBuilderTests.cs
│   └── Integration/                   # Integration tests
│       └── EndToEndTests.cs
├── Encina.ContractTests/      # Contract tests
│   ├── HandlerRegistrationContracts.cs
│   └── OutboxStoreContract.cs
├── Encina.PropertyTests/      # Property-based tests
│   ├── PipelineInvariants.cs
│   └── CacheInvariants.cs
├── Encina.GuardClauses.Tests/ # Guard clause tests
│   ├── EncinaGuardsTests.cs
│   └── OutboxGuardsTests.cs
├── Encina.Dapper.SqlServer.Tests/  # Dapper provider tests
│   ├── Unit/
│   └── Integration/
├── appsettings.Testing.json           # Test configuration
load/
├── Encina.NBomber/            # NBomber load tests
└── Encina.LoadTests/          # Custom load tests
benchmarks/
└── Encina.Benchmarks/         # BenchmarkDotNet
```

#### Testing Workflow

**Recommended approach for new features**:

1. Write unit tests covering the main functionality
2. Implement feature
3. Add additional test types based on risk/complexity:
   - Integration tests for database/external dependencies
   - Property tests for complex logic with invariants
   - Guard tests for public APIs
4. Verify tests pass

**Before committing**:

```bash
# Run all tests
dotnet test Encina.slnx --configuration Release

# Optional: Check coverage
dotnet test --collect "XPlat Code Coverage"

# Optional: Run mutation testing
dotnet run --file scripts/run-stryker.cs
```

**CI/CD enforces**:

- ✅ All tests pass
- ✅ 0 build warnings
- ✅ Code formatting
- ✅ Public API compatibility

#### Examples of Complete Test Coverage

**Example: OutboxStore**

```csharp
// 1. Unit Tests
OutboxStoreTests.cs
- AddAsync_ValidMessage_ShouldSucceed()
- GetPendingMessagesAsync_WithFilter_ShouldReturnFiltered()
- MarkAsProcessedAsync_ValidId_ShouldUpdateTimestamp()

// 2. Integration Tests (Docker)
OutboxStoreIntegrationTests.cs
- AddAsync_ShouldPersistToRealDatabase()
- GetPendingMessages_ShouldQueryRealDatabase()
- ConcurrentWrites_ShouldNotCorruptData()

// 3. Contract Tests
OutboxStoreContractTests.cs
- AllImplementations_MustFollowIOutboxStoreContract()
- AddAsync_AllProviders_MustReturnSameResult()

// 4. Property Tests
OutboxStorePropertyTests.cs
- AddThenGet_AlwaysReturnsWhatWasAdded()
- GetPending_NeverReturnsProcessedMessages()

// 5. Guard Tests
OutboxStoreGuardTests.cs
- AddAsync_NullMessage_ThrowsArgumentNullException()
- GetMessageAsync_EmptyGuid_ThrowsArgumentException()

// 6. Load Tests
OutboxStoreLoadTests.cs
- HighConcurrency_1000Writes_AllSucceed()
- BulkOperations_10000Messages_WithinTimeout()

// 7. Benchmarks
OutboxStoreBenchmarks.cs
- AddAsync_Baseline()
- GetPendingMessages_Batch100vs1000()
```

**Result**: Comprehensive coverage of critical functionality.

#### Test Data Management

Use builders for test data:

```csharp
public class OutboxMessageBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _payload = "{}";
    private DateTime _createdAt = DateTime.UtcNow;

    public OutboxMessageBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public OutboxMessageBuilder WithPayload(string payload)
    {
        _payload = payload;
        return this;
    }

    public OutboxMessage Build() => new()
    {
        Id = _id,
        Payload = _payload,
        CreatedAtUtc = _createdAt
    };
}

// Usage
var message = new OutboxMessageBuilder()
    .WithPayload("{\"test\":true}")
    .Build();
```

#### Remember

> **Quality is important but should be balanced with development velocity.**
>
> Focus on testing what matters: critical paths, complex logic, and public APIs.
> Add tests incrementally as features mature.

### Code Analysis

- **Zero Warnings**: All CA warnings must be addressed (fix or suppress with justification)
- **Suppression Rules**:
  - CA1848 (LoggerMessage delegates): Suppress if performance optimization is future work
  - CA2263 (Generic overload): Suppress when dynamic serialization is needed
  - CA1716 (Keyword conflicts): Fix by renaming (e.g., `Error` → `ErrorMessage`)

### Documentation

- **XML Comments**: Required on all public APIs
- **Examples**: Provide code examples in XML docs when helpful
- **README Files**: Each satellite package has its own comprehensive README
- **Architecture Docs**: Maintain design decision records (ADRs) when applicable

### Git Workflow

- **No Force Push to main/master**: Never use `--force` on main branches
- **Commit Messages**: Clear, descriptive, in English
- **No AI Attribution**: Do NOT include any AI signatures, co-author tags, or Claude references in commits
  - ❌ Never add `Co-Authored-By: Claude...`
  - ❌ Never add `🤖 Generated with Claude Code`
  - ❌ Never add any reference to AI assistance in commit messages
- **Author**: All commits should appear as authored solely by the repository owner

### Future Roadmap Items

1. Stream Requests (IAsyncEnumerable support)
2. Dapper satellite package
3. ADO.NET satellite package
4. Hangfire/Quartz adapters for scheduling
5. Final renaming (post-1.0)

### Spanish/English

- User communicates in Spanish
- Code, comments, documentation: **English only**
- Commit messages: English
- User-facing messages: Spanish when responding to user
- **Translation Rule**: If you encounter any Spanish comments in code while editing, translate them to English

## Current Project Status (Updated: 2025-12-23)

### ✅ Completed (90% to 1.0)

**Core & Validation**:

- ✅ Encina core (Railway Oriented Programming, 194 tests)
- ✅ Encina.Validation namespace with Orchestrator pattern (IValidationProvider, ValidationOrchestrator)
- ✅ FluentValidation satellite (28 tests) - uses FluentValidationProvider
- ✅ DataAnnotations satellite (20 tests) - uses DataAnnotationsValidationProvider
- ✅ MiniValidator satellite (20 tests) - uses MiniValidationProvider
- ✅ GuardClauses satellite (262 tests)

**Web & Messaging**:

- ✅ AspNetCore satellite (49 tests)
- ✅ Encina.Messaging abstractions
- ✅ EntityFrameworkCore (33 tests) - Outbox, Inbox, Sagas, Scheduling, Transactions

**Database Providers** (10 packages completed):

- ✅ Dapper.SqlServer, Dapper.PostgreSQL, Dapper.MySQL, Dapper.Sqlite, Dapper.Oracle
- ✅ ADO.SqlServer, ADO.PostgreSQL, ADO.MySQL, ADO.Sqlite, ADO.Oracle
- **Note**: Old Encina.Dapper and Encina.ADO deprecated (code in .backup/deprecated-packages)

**Caching** (8 packages completed - NEW 2025-12-21):

- ✅ Encina.Caching - Core abstractions (ICacheProvider, ICacheKeyGenerator, CachingPipelineBehavior)
- ✅ Encina.Caching.Memory - IMemoryCache provider (109 tests)
- ✅ Encina.Caching.Hybrid - Microsoft HybridCache provider (.NET 9+ multi-tier caching, 56 tests)
- ✅ Encina.Caching.Redis - StackExchange.Redis provider
- ✅ Encina.Caching.Garnet - Microsoft Garnet provider (Redis-compatible)
- ✅ Encina.Caching.Valkey - Valkey provider (Redis fork)
- ✅ Encina.Caching.Dragonfly - Dragonfly provider (Redis-compatible)
- ✅ Encina.Caching.KeyDB - KeyDB provider (Redis fork)
- **Tests**: 367 tests passing (49 core + 109 memory + 56 hybrid + 43 guard + 78 contract + 32 property)
- **Benchmarks**: Encina.Caching.Benchmarks with provider comparisons

**Messaging Transports** (10 packages completed):

- ✅ Encina.RabbitMQ - RabbitMQ.Client 7.2.0 integration
- ✅ Encina.AzureServiceBus - Azure Service Bus 7.20.1 integration
- ✅ Encina.AmazonSQS - AWS SQS/SNS 4.0.2.3 integration
- ✅ Encina.Kafka - Confluent.Kafka 2.12.0 integration
- ✅ Encina.Redis.PubSub - StackExchange.Redis pub/sub
- ✅ Encina.InMemory - System.Threading.Channels message bus
- ✅ Encina.NATS - NATS.Net 2.6.11 with JetStream support
- ✅ Encina.MQTT - MQTTnet 5.0.1 integration
- ✅ Encina.gRPC - Grpc.AspNetCore 2.71.0 Encina service
- ✅ Encina.GraphQL - HotChocolate 15.1.11 bridge
- **Note**: All packages use `#pragma warning disable CA1848` for logging (LoggerMessage delegates pending)

**Job Scheduling**:

- ✅ Hangfire adapter (15 tests)
- ✅ Quartz adapter (18 tests)

**Event Sourcing** (2 packages):

- ✅ Encina.EventStoreDB - EventStoreDB integration with aggregate repository
- ✅ Encina.Marten - Marten/PostgreSQL event store with projections support

**Resilience** (3 packages):

- ✅ Encina.Extensions.Resilience - Core resilience abstractions
- ✅ Encina.Polly - Retry, circuit breaker, timeout policies
- ✅ Encina.Refit - HTTP client integration with resilience

**Real-time & Integration** (2 packages):

- ✅ Encina.SignalR - Real-time notification broadcasting
- ✅ Encina.MongoDB - MongoDB persistence provider

**Observability**:

- ✅ Encina.OpenTelemetry - Distributed tracing and metrics

**Quality Metrics**:

- ✅ Line Coverage: 92.5% (target: ≥90%)
- ✅ Mutation Score: 79.75% (target: ≥80%)
- ✅ Build Warnings: 0
- ✅ XML Documentation: 100%
- ✅ PublicAPI Analyzers enabled

**CRITICAL: MSBuild Stability Issue**

⚠️ **Building the full solution can cause MSBuild crashes and even Windows restarts** due to parallel execution overload with the large test suite (70+ projects).

**Mitigations** (ALWAYS use one of these):

1. **Use `-maxcpucount:1` flag** for single-process builds:

   ```bash
   dotnet build -maxcpucount:1
   dotnet test -maxcpucount:1
   ```

2. **Use Solution Filters (.slnf)** to build only what you need (preferred):

**Solution Filters** (.slnf):

For efficient development with reduced MSBuild overhead, use solution filters:

```bash
# Work only on caching (17 projects)
dotnet build Encina.Caching.slnf

# Work only on core (7 projects)
dotnet build Encina.Core.slnf

# Work only on validation (25 projects)
dotnet build Encina.Validation.slnf

# Work only on database providers (21 projects)
dotnet build Encina.Database.slnf

# Work only on scheduling (15 projects)
dotnet build Encina.Scheduling.slnf

# Work only on web (9 projects)
dotnet build Encina.Web.slnf
```

**Total Tests**: 752+ passing (385 core + 367 caching)

### 🔄 In Progress

**Documentation** (80% complete):

- DocFX configured, needs GitHub Pages deploy
- Package comparison guides pending
- MediatR migration guide pending

### ⏳ Pending (Pre-1.0 Only - NO post-1.0 versions planned yet!)

**Critical Core Improvements**:

- Refactor `Encina.Publish` with guards (like Send)
- Optimize delegate caches (minimize reflection/boxing)
- Apply `CollectionsMarshal.AsSpan` for performance
- Substitute `object? Details` with `ImmutableDictionary<string, object?>`

**Testing Excellence**:

- Amplify property-based testing (pipeline invariants, cache behavior)
- Elevate mutation score to ≥95% (currently 79.75%)
- Load testing with strict thresholds
- Telemetry exhaustive tests

**Static Analysis**:

- Configure SONAR_TOKEN and run first SonarCloud scan
- Cyclomatic complexity analysis (≤10/method)
- Code duplication analysis (<3%)

**EDA Enhancements** (Pre-1.0):

- ⭐⭐⭐⭐ Projections/Read Models - Abstractions for CQRS read side
- ⭐⭐⭐⭐ Event Versioning - Upcasting, schema evolution for EventStoreDB/Marten
- ⭐⭐⭐ Snapshotting - For large aggregates in EventStoreDB/Marten
- ⭐⭐⭐ Dead Letter Queue - Enhanced DLQ handling across messaging providers

**Microservices Enhancements** (Pre-1.0):

- ⭐⭐⭐ Health Check Abstractions - IHealthCheck integration for handler health
- ⭐⭐⭐ Bulkhead Isolation - Parallel execution isolation via Polly
- ⭐⭐ API Versioning Helpers - Contract evolution support
- ⭐⭐ Distributed Lock Abstractions - IDistributedLock interface

> Note: Service Discovery, Secret Management, and Configuration delegated to infrastructure (Dapr, Kubernetes, Azure)

**Remaining Satellite Packages**:

- ⭐⭐⭐ ODBC provider (legacy databases)

**Strategic Initiatives**:

- ✅ Renaming to "Encina" (completed 2025-12-22)

**Security & Supply Chain**:

- SLSA Level 2 compliance
- Automatic SBOM on releases
- Supply chain security (Sigstore/cosign)

## Quick Reference

### When to Use Each Pattern

- **Outbox**: Publishing domain events reliably (e-commerce order placed event)
- **Inbox**: Processing external messages idempotently (webhook handling, queue consumers)
- **Saga**: Coordinating distributed transactions (order fulfillment across services)
- **Scheduling**: Delayed execution of domain operations (send reminder in 24 hours)
- **Transactions**: Automatic commit/rollback based on ROP result

### Scheduling vs Hangfire/Quartz

- **Encina.Scheduling**: Domain messages (commands, queries, notifications)
- **Hangfire/Quartz**: Infrastructure jobs (cleanup tasks, reports, batch processing)
- **Complementary**: Both can coexist in the same application
- **Future**: Adapters to use Hangfire/Quartz as scheduling backends

### Common Errors to Avoid

1. ❌ Don't add `[Obsolete]` attributes for backward compatibility
2. ❌ Don't create migration helpers or compatibility layers
3. ❌ Don't use .NET 9 or older - only .NET 10
4. ❌ Don't name properties `Error` (use `ErrorMessage` to avoid CA1716)
5. ❌ Don't make patterns mandatory - everything is opt-in
6. ❌ Don't mix provider-specific code with abstractions
7. ❌ Don't compromise design for non-existent legacy users

### Remember
>
> "We're in Pre-1.0. Choose the best solution, not the compatible one."

## Issue Tracking & Project Documentation

This project uses a structured approach to track issues, changes, and history.

### GitHub Issues (Primary Issue Tracker)

**All bugs, features, and technical debt MUST be tracked via GitHub Issues.**

Location: <https://github.com/dlrivada/Encina/issues>

#### Issue Templates

Use the appropriate template when creating issues:

| Template | Use Case | Label |
|----------|----------|-------|
| `bug_report.md` | Report bugs or unexpected behavior | `bug` |
| `feature_request.md` | Suggest new features or enhancements | `enhancement` |
| `technical_debt.md` | Track internal code quality issues, refactoring, test gaps | `technical-debt` |

#### When to Create Issues

- **Bugs found during development** → Create immediately with `[BUG]` prefix
- **Technical debt discovered** → Create with `[DEBT]` prefix (don't fix inline if it risks derailing current work)
- **Feature ideas** → Create with `[FEATURE]` prefix for later discussion
- **Failing tests** → Create with `[DEBT]` and link to specific test files

#### Workflow

1. **Find issue** → Create GitHub Issue using appropriate template
2. **Start work** → Assign issue to yourself, move to "In Progress"
3. **Complete work** → Reference issue in commit (`Fixes #123`) or PR
4. **Issue auto-closes** when PR is merged

### Project Documentation Files

| File | Purpose |
|------|---------|
| `CHANGELOG.md` | Track released changes (follows Keep a Changelog format) |
| `ROADMAP.md` | High-level roadmap and planned features |
| `docs/history/YYYY-MM.md` | Detailed monthly implementation history |
| `docs/architecture/adr/*.md` | Architecture Decision Records |
| `docs/roadmap-documentacion.md` | Documentation-specific roadmap |

### When Updating Documentation

- **After completing a feature** → Update `CHANGELOG.md` (Unreleased section)
- **After major implementation phase** → Update `docs/history/YYYY-MM.md`
- **After architectural decisions** → Create ADR in `docs/architecture/adr/`
- **After releasing** → Move Unreleased to version section in `CHANGELOG.md`

### DO NOT Track Issues Here

This CLAUDE.md file is for **development guidelines**, not issue tracking.

- ❌ Don't add "Known Issues" sections here
- ❌ Don't track bugs or technical debt in this file
- ✅ Use GitHub Issues for all tracking
- ✅ Reference issue numbers in commits and PRs
