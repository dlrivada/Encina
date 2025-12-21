# Claude Code - SimpleMediator Framework Guidelines

## Project Philosophy

### Pre-1.0 Development Status

- **Current Phase**: Pre-1.0 - Initial Design & Architecture
- **No Backward Compatibility Required**: We are NOT maintaining backward compatibility
- **Breaking Changes**: Fully acceptable and encouraged if they improve the design
- **Migration Support**: NOT needed - no existing users to migrate
- **Final Name Change**: The framework will be renamed in the last step (post-1.0)

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

- Core pattern: `Either<MediatorError, T>`
- Explicit error handling, no exceptions for business logic
- Validation returns `Either` with detailed errors

#### Messaging Patterns (All Optional)

1. **Outbox Pattern**: Reliable event publishing (at-least-once delivery)
2. **Inbox Pattern**: Idempotent message processing (exactly-once semantics)
3. **Saga Pattern**: Distributed transactions with compensation (orchestration-based)
4. **Scheduled Messages**: Delayed/recurring command execution
5. **Transactions**: Automatic database transaction management

#### Provider Coherence

- **SimpleMediator.Messaging**: Shared abstractions (IOutboxStore, IInboxStore, etc.)
- **SimpleMediator.EntityFrameworkCore**: EF Core implementation
- **SimpleMediator.Dapper**: Future - Dapper implementation
- **SimpleMediator.Data**: Future - ADO.NET implementation
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

- **Same interfaces** (from SimpleMediator.Messaging)
- **Same configuration options** (from SimpleMediator.Messaging)
- **Different implementations** (provider-specific)
- **Easy migration** (change DI registration, rest stays the same)

Example:

```csharp
// Using EF Core
services.AddSimpleMediatorEntityFrameworkCore(config => {
    config.UseOutbox = true;
});

// Switch to Dapper (same interface, different implementation)
services.AddSimpleMediatorDapper(config => {
    config.UseOutbox = true; // Same configuration!
});
```

#### Validation Libraries Support

- Support multiple: FluentValidation, DataAnnotations, MiniValidator
- User chooses their preferred library
- Similar pattern for scheduling: SimpleMediator.Scheduling vs Hangfire/Quartz adapters

### Testing Standards - MANDATORY 100% COVERAGE

**CRITICAL**: Every line of code MUST be covered by ALL applicable test types. No exceptions.

#### Coverage Requirements

- **Line Coverage**: 100% (not 90%, not 95% - ONE HUNDRED PERCENT)
- **Branch Coverage**: 100% (every if/else, switch case, ternary)
- **Method Coverage**: 100% (every method, including private via public API)
- **Mutation Score**: ≥95% (Stryker must kill 95%+ of mutants)

#### Test Types - ALL MANDATORY for Every Feature

Every piece of code MUST have:

1. **Unit Tests** ✅
   - Test individual methods in isolation
   - Mock all dependencies
   - Fast execution (<1ms per test)
   - Location: `tests/{Package}.Tests/`
   - Naming: `{ClassName}Tests.cs`
   - Example: `SimpleMediatorTests.cs`, `OutboxStoreEFTests.cs`

2. **Integration Tests** ✅
   - Test against real databases (via Docker)
   - Test full workflows end-to-end
   - Medium execution (<100ms per test)
   - Mark with: `[Trait("Category", "Integration")]`
   - Location: `tests/{Package}.Tests/Integration/`
   - Example: Database operations, HTTP calls, file I/O

3. **Contract Tests** ✅
   - Verify public API contracts don't break
   - Test interfaces, abstract classes
   - Verify all implementations follow contract
   - Location: `tests/SimpleMediator.ContractTests/`
   - Example: `IRequestHandler` contract, `IOutboxStore` contract

4. **Property-Based Tests** ✅
   - Use FsCheck to generate random inputs
   - Verify invariants hold for ALL inputs
   - Find edge cases humans miss
   - Location: `tests/SimpleMediator.PropertyTests/`
   - Example: Pipeline ordering, cache consistency, ROP laws

5. **Guard Clause Tests** ✅
   - Verify ALL null checks throw `ArgumentNullException`
   - Verify ALL empty/invalid inputs throw appropriate exceptions
   - Use GuardClauses.xUnit library
   - Location: `tests/SimpleMediator.GuardClauses.Tests/`
   - Example: Every public method with parameters

6. **Load Tests** ✅
   - Stress test under high concurrency
   - Verify no race conditions
   - Verify performance degradation is linear
   - Location: `load/SimpleMediator.LoadTests/`, `load/SimpleMediator.NBomber/`
   - Tools: NBomber, custom load generators

7. **Benchmarks** ✅
   - Measure actual performance
   - Prevent performance regressions
   - Compare implementations
   - Location: `benchmarks/SimpleMediator.Benchmarks/`
   - Tool: BenchmarkDotNet

#### Test Quality Standards

**EVERY test MUST**:

- ✅ Have a clear, descriptive name (no `Test1`, `Test2`)
- ✅ Follow AAA pattern (Arrange, Act, Assert)
- ✅ Test ONE thing (single responsibility)
- ✅ Be independent (no shared state between tests)
- ✅ Be deterministic (same input = same output, always)
- ✅ Clean up resources (dispose, delete temp files, etc.)
- ✅ Have XML documentation explaining WHAT it tests and WHY

**NEVER**:

- ❌ Skip tests (`[Fact(Skip = "...")]` is FORBIDDEN except for Pure ROP)
- ❌ Ignore flaky tests (fix or delete them)
- ❌ Test implementation details (test behavior, not internals)
- ❌ Use `Thread.Sleep` (use proper synchronization)
- ❌ Hard-code paths, dates, GUIDs (use test data generators)

#### Docker Integration Testing

ALL database-dependent code MUST have integration tests using Docker:

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
├── SimpleMediator.Tests/              # Unit tests for core
│   ├── SimpleMediatorTests.cs
│   ├── PipelineBuilderTests.cs
│   └── Integration/                   # Integration tests
│       └── EndToEndTests.cs
├── SimpleMediator.ContractTests/      # Contract tests
│   ├── HandlerRegistrationContracts.cs
│   └── OutboxStoreContract.cs
├── SimpleMediator.PropertyTests/      # Property-based tests
│   ├── PipelineInvariants.cs
│   └── CacheInvariants.cs
├── SimpleMediator.GuardClauses.Tests/ # Guard clause tests
│   ├── MediatorGuardsTests.cs
│   └── OutboxGuardsTests.cs
├── SimpleMediator.Dapper.SqlServer.Tests/  # Dapper provider tests
│   ├── Unit/
│   └── Integration/
├── appsettings.Testing.json           # Test configuration
load/
├── SimpleMediator.NBomber/            # NBomber load tests
└── SimpleMediator.LoadTests/          # Custom load tests
benchmarks/
└── SimpleMediator.Benchmarks/         # BenchmarkDotNet
```

#### Testing Workflow

**BEFORE writing ANY production code**:

1. Write failing unit test
2. Write failing integration test
3. Write failing contract test
4. Write property-based test for invariants
5. Write guard clause tests
6. Implement feature
7. ALL tests pass
8. Add load test if applicable
9. Add benchmark if performance-critical
10. Verify 100% coverage

**BEFORE any commit**:

```bash
# Run all tests
dotnet test SimpleMediator.slnx --configuration Release

# Verify coverage (must be 100%)
dotnet test --collect "XPlat Code Coverage"
dotnet tool run reportgenerator ... # Must show 100%

# Run mutation testing (must be ≥95%)
dotnet run --file scripts/run-stryker.cs

# Run integration tests with Docker
dotnet run --file scripts/run-integration-tests.cs

# Run benchmarks (optional, but recommended)
dotnet run --file scripts/run-benchmarks.cs
```

**CI/CD enforces*3*:

- ✅ All tests pass
- ✅ 100% line coverage
- ✅ 100% branch coverage
- ✅ ≥95% mutation score
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

**Result**: EVERY line, EVERY branch, EVERY scenario covered.

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

> **100% coverage is NOT optional. It is MANDATORY.**
>
> Every commit that reduces coverage below 100% will be REJECTED by CI.
> Every test that is skipped without justification will be REJECTED.
> Every missing test type will be REJECTED.
>
> Quality is not negotiable in this project.

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
- **Commit Messages**: Clear, descriptive, include emoji (🤖 Generated with Claude Code)
- **Co-Authored-By**: Include Claude attribution on AI-assisted commits

### Future Roadmap Items

1. Stream Requests (IAsyncEnumerable support)
2. Dapper satellite package
3. ADO.NET satellite package
4. Hangfire/Quartz adapters for scheduling
5. Final framework renaming (post-1.0)

### Spanish/English

- User communicates in Spanish
- Code, comments, documentation: English
- Commit messages: English
- User-facing messages: Spanish when responding to user

## Current Project Status (Updated: 2025-12-21)

### ✅ Completed (90% to 1.0)

**Core & Validation**:

- ✅ SimpleMediator core (Railway Oriented Programming, 194 tests)
- ✅ FluentValidation satellite (18 tests)
- ✅ DataAnnotations satellite (10 tests)
- ✅ MiniValidator satellite (10 tests)
- ✅ GuardClauses satellite (262 tests)

**Web & Messaging**:

- ✅ AspNetCore satellite (49 tests)
- ✅ SimpleMediator.Messaging abstractions
- ✅ EntityFrameworkCore (33 tests) - Outbox, Inbox, Sagas, Scheduling, Transactions

**Database Providers** (10 packages completed):

- ✅ Dapper.SqlServer, Dapper.PostgreSQL, Dapper.MySQL, Dapper.Sqlite, Dapper.Oracle
- ✅ ADO.SqlServer, ADO.PostgreSQL, ADO.MySQL, ADO.Sqlite, ADO.Oracle
- **Note**: Old SimpleMediator.Dapper and SimpleMediator.ADO deprecated (code in .backup/deprecated-packages)

**Caching** (8 packages completed - NEW 2025-12-21):

- ✅ SimpleMediator.Caching - Core abstractions (ICacheProvider, ICacheKeyGenerator, CachingPipelineBehavior)
- ✅ SimpleMediator.Caching.Memory - IMemoryCache provider (109 tests)
- ✅ SimpleMediator.Caching.Hybrid - Microsoft HybridCache provider (.NET 9+ multi-tier caching, 56 tests)
- ✅ SimpleMediator.Caching.Redis - StackExchange.Redis provider
- ✅ SimpleMediator.Caching.Garnet - Microsoft Garnet provider (Redis-compatible)
- ✅ SimpleMediator.Caching.Valkey - Valkey provider (Redis fork)
- ✅ SimpleMediator.Caching.Dragonfly - Dragonfly provider (Redis-compatible)
- ✅ SimpleMediator.Caching.KeyDB - KeyDB provider (Redis fork)
- **Tests**: 367 tests passing (49 core + 109 memory + 56 hybrid + 43 guard + 78 contract + 32 property)
- **Benchmarks**: SimpleMediator.Caching.Benchmarks with provider comparisons

**Job Scheduling**:

- ✅ Hangfire adapter (15 tests)
- ✅ Quartz adapter (18 tests)

**Quality Metrics**:

- ✅ Line Coverage: 92.5% (target: ≥90%)
- ✅ Mutation Score: 79.75% (target: ≥80%)
- ✅ Build Warnings: 0
- ✅ XML Documentation: 100%
- ✅ PublicAPI Analyzers enabled

**Solution Filters** (.slnf - NEW 2025-12-21):

For efficient development with reduced MSBuild overhead, use solution filters:

```bash
# Work only on caching (17 projects)
dotnet build SimpleMediator.Caching.slnf

# Work only on core (7 projects)
dotnet build SimpleMediator.Core.slnf

# Work only on validation (25 projects)
dotnet build SimpleMediator.Validation.slnf

# Work only on database providers (21 projects)
dotnet build SimpleMediator.Database.slnf

# Work only on scheduling (15 projects)
dotnet build SimpleMediator.Scheduling.slnf

# Work only on web (9 projects)
dotnet build SimpleMediator.Web.slnf
```

**Total Tests**: 752+ passing (385 core + 367 caching)

### 🔄 In Progress

**Documentation** (80% complete):

- DocFX configured, needs GitHub Pages deploy
- Package comparison guides pending
- MediatR migration guide pending

### ⏳ Pending (Pre-1.0 Only - NO post-1.0 versions planned yet!)

**Critical Core Improvements**:

- Refactor `SimpleMediator.Publish` with guards (like Send)
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

**Satellite Packages**:

- ⭐⭐⭐⭐⭐ SimpleMediator.OpenTelemetry (CRITICAL - observability)
- ~~⭐⭐⭐⭐ SimpleMediator.Caching~~ ✅ COMPLETED
- ⭐⭐⭐⭐ SimpleMediator.Polly (retry + circuit breaker)
- ⭐⭐⭐⭐ Stream Requests (IAsyncEnumerable support)
- ~~⭐⭐⭐⭐⭐ Redis provider~~ ✅ COMPLETED (SimpleMediator.Caching.Redis)
- ⭐⭐⭐ ODBC provider (legacy databases)
- Event Sourcing package (EventStoreDB/Marten)

**Strategic Initiatives** (to be done JUST BEFORE 1.0):

- Parallel execution support (opt-in parallel notification dispatch)
- Framework renaming to "Encina Framework"

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

- **SimpleMediator.Scheduling**: Domain messages (commands, queries, notifications)
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
