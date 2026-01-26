# Claude Code - Encina Guidelines

## Active Plans

> **IMPORTANT**: Before starting work, check for active plans in `docs/plans/`. Read the plan file to understand the current state and continue from where the last session left off.

| Plan | File | Status |
|------|------|--------|
| Test Consolidation | `docs/plans/test-consolidation-plan.md` | 🟡 In Progress |

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
- **Encina.Dapper**: Dapper implementation
- **Encina.ADO**: ADO.NET implementation
- Same interfaces, different implementations - easy to switch providers

#### Multi-Provider Implementation Rule (MANDATORY)

> **CRITICAL**: All provider-dependent features MUST be implemented for ALL 13 providers. This is a fundamental project rule, not just a testing requirement.

**The 13 Providers:**

| Category | Providers | Count |
|----------|-----------|-------|
| **ADO.NET** | Sqlite, SqlServer, PostgreSQL, MySQL | 4 |
| **Dapper** | Sqlite, SqlServer, PostgreSQL, MySQL | 4 |
| **EF Core** | Sqlite, SqlServer, PostgreSQL, MySQL | 4 |
| **MongoDB** | MongoDB | 1 |

> **Note**: Oracle was removed from pre-1.0 scope due to disproportionate maintenance cost. See [ADR-009](docs/architecture/adr/009-remove-oracle-provider-pre-1.0.md) for details. Oracle code is preserved in `.backup/oracle/` for potential future restoration.

**When this rule applies:**

- Implementing any store (OutboxStore, InboxStore, SagaStore, ScheduledMessageStore, etc.)
- Implementing repositories, Unit of Work, bulk operations
- Any feature that interacts with database-specific SQL or connection types
- Registering services in `ServiceCollectionExtensions`

**Provider-specific SQL differences to consider:**

| Provider | Parameters | LIMIT | Boolean | Notes |
|----------|------------|-------|---------|-------|
| SQLite | `@param` | `LIMIT @n` | `0/1` | String-based DateTime storage |
| SQL Server | `@param` | `TOP (@n)` | `bit` | Native DateTime, GUID |
| PostgreSQL | `@param` | `LIMIT @n` | `true/false` | Case-sensitive identifiers |
| MySQL | `@param` | `LIMIT @n` | `0/1` | Backtick identifiers |

**Excluded from this rule** (specialized providers with different purposes):

- Message brokers: RabbitMQ, Kafka, NATS, MQTT
- Caching: Redis, Memory
- Event sourcing: Marten

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

#### Repository Pattern (Optional)

The repository pattern is **completely optional**. Most applications work fine using `DbContext` directly.

**When to use Repository:**

| Scenario | Recommendation |
|----------|----------------|
| Simple CRUD with EF Core | Use `DbContext` directly |
| Complex LINQ queries | Use `DbContext` directly |
| Single database, single DbContext | Use `DbContext` directly |
| Multiple DbContexts/databases | Consider Repository |
| Transactions with non-EF components | Consider Repository |
| Easy unit testing with mocks | Consider Repository |
| Switching providers (EF → Dapper) | Consider Repository |
| DDD with aggregate repositories | Consider Repository |

**Philosophy**: The .NET community increasingly recognizes that repositories often add boilerplate without real benefit when using a mature ORM. `DbContext` is already a Unit of Work and `DbSet<T>` is already a repository. Encina provides the pattern for those who need it, but never forces it.

```csharp
// Most apps: Use DbContext directly with messaging patterns
services.AddDbContext<AppDbContext>(options => ...);
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseTransactions = true;
    config.UseOutbox = true;
});

// Only when needed: Opt-in to repository for specific entities
services.AddEncinaRepository<Order, OrderId>();
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
   - Location: `tests/Encina.LoadTests/`

7. **Benchmarks** 🟡 (Hot paths, performance comparisons)
   - Measure actual performance with BenchmarkDotNet
   - Location: `tests/Encina.BenchmarkTests/`

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

For database-dependent code, integration tests using Docker/Testcontainers are recommended.

**Quick Start:**

```bash
# Start essential services
docker compose --profile core up -d

# Start all databases
docker compose --profile databases up -d

# Start everything
docker compose --profile full up -d
```

> **Available Profiles**: `core`, `databases`, `messaging`, `caching`, `cloud`, `observability`, `full`
>
> See [Docker Infrastructure Guide](docs/infrastructure/docker-infrastructure.md) for complete details.

**Example test:**

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
├── Encina.UnitTests/          # Consolidated unit tests (~4,600 tests)
│   ├── ADO/
│   ├── AspNetCore/
│   ├── Caching/
│   ├── Core/
│   ├── Dapper/
│   ├── DomainModeling/
│   ├── EntityFrameworkCore/
│   ├── Messaging/
│   └── ...
├── Encina.IntegrationTests/   # Consolidated integration tests (~710 tests)
├── Encina.PropertyTests/      # Property-based tests (~485 tests)
├── Encina.ContractTests/      # Contract tests (~400 tests)
├── Encina.GuardTests/         # Guard clause tests (~300 tests)
├── Encina.LoadTests/          # Load testing harness
├── Encina.NBomber/            # NBomber scenarios
├── Encina.BenchmarkTests/     # BenchmarkDotNet benchmarks
│   ├── Encina.Benchmarks/
│   ├── Encina.AspNetCore.Benchmarks/
│   └── ...
├── Encina.TestInfrastructure/ # Shared test infrastructure
└── Encina.Testing.Examples/   # Reference examples
```

#### Test Coverage for All 13 Providers

Tests MUST cover ALL 13 providers as defined in the [Multi-Provider Implementation Rule](#multi-provider-implementation-rule-mandatory) section above.

> **Reminder**: The 13 providers are: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).

#### Test Type Guidelines by Feature Category

**IMPORTANT**: Not all test types can be justified with `.md` files. The decision depends on the feature category:

| Test Type | Database Features | Non-DB Features | Notes |
|-----------|-------------------|-----------------|-------|
| **UnitTests** | ✅ Required | ✅ Required | Always required |
| **GuardTests** | ✅ Required | ✅ Required | All public methods |
| **PropertyTests** | ✅ Required | 🟡 If complex | Cross-provider invariants |
| **ContractTests** | ✅ Required | 🟡 If public API | API consistency |
| **IntegrationTests** | ✅ **Required** | 📄 Justify if skip | DB features MUST have real tests |
| **LoadTests** | ⚠️ See below | 📄 Justify if skip | Only for concurrent features |
| **BenchmarkTests** | 📄 Justify if skip | 📄 Justify if skip | Only for hot paths |

#### IntegrationTests for Database Features

**For database-related milestones (like v0.12.0 Database & Repository):**

- ❌ **DO NOT** create `.md` justification files for IntegrationTests
- ✅ **DO** create real IntegrationTests using Docker/Testcontainers
- The whole point of database features is to interact with real databases

**Why real IntegrationTests are mandatory for DB features:**

1. SQL syntax varies by provider (Oracle uses `:param`, MySQL uses backticks)
2. Connection/transaction behavior differs between providers
3. Type mappings vary (GUID storage, DateTime precision, boolean representation)
4. We have Docker infrastructure (`docs/infrastructure/docker-infrastructure.md`)

#### LoadTests Guidelines

LoadTests are only meaningful for features with **concurrent behavior**:

| Feature | LoadTest? | Reason |
|---------|-----------|--------|
| Repository | 📄 Justify | Thin wrapper, load is on DB |
| Specification | 📄 Justify | SQL generation is CPU-bound, fast |
| **Unit of Work** | ✅ Implement | Connection/transaction management under load |
| **Multi-Tenancy** | ✅ Implement | Tenant isolation under concurrent access |
| **Read/Write Separation** | ✅ Implement | Replica distribution is exactly what LoadTests validate |
| Module Isolation | 📄 Justify | Only active in development mode |

#### BenchmarkTests Guidelines

BenchmarkTests are only meaningful for **hot paths** where microseconds matter:

| Feature | Benchmark? | Reason |
|---------|------------|--------|
| Repository | 📄 Justify | Overhead is negligible vs DB |
| Specification | ⚠️ Maybe | Complex query generation might benefit |
| Unit of Work | 📄 Justify | Transaction management, not a hot path |
| Multi-Tenancy | 📄 Justify | Single WHERE clause, O(1) |
| **Read/Write Separation** | ✅ Implement | Replica selection algorithms are benchmarkable |
| Module Isolation | 📄 Justify | Development-only, disabled in production |

#### Test Justification Documents (.md)

When a test type is **legitimately** NOT implemented for a feature, create a justification document:

**Format**: `{TestProject}/{Provider/Feature}/{Feature}.md`

**When to use justification documents:**

- ✅ BenchmarkTests for thin wrappers (Repository, Tenancy)
- ✅ LoadTests for non-concurrent features (Repository, Specification)
- ❌ **NEVER** for IntegrationTests on database features
- ❌ **NEVER** for UnitTests, GuardTests, ContractTests

**Required content**:

```markdown
# {Test Type} - {Provider} {Feature}

## Status: Not Implemented

## Justification
[Technical reasons why this test type is not needed]

### 1. [Reason category]
[Detailed explanation]

### 2. Adequate Coverage from Other Test Types
- **Unit Tests**: [what's covered]
- **Guard Tests**: [what's covered]
- **Property Tests**: [what's covered]
- **Contract Tests**: [what's covered]

### N. Recommended Alternative
[How to test this if needed in the future]

## Related Files
- `src/...` - Source files
- `tests/...` - Existing test files

## Date: YYYY-MM-DD
## Issue: #NNN
```

**Rule**: If a folder has neither `.cs` test files nor `.md` justification, the test coverage for that feature/provider has NOT been evaluated yet.

#### Supported Test Types (Encina.{Type}Tests)

| Project | Purpose | When Required |
|---------|---------|---------------|
| `Encina.UnitTests` | Isolated unit tests | ✅ Always required |
| `Encina.GuardTests` | Null/parameter validation | ✅ All public methods |
| `Encina.PropertyTests` | Invariant verification | 🟡 Complex logic |
| `Encina.ContractTests` | API contract verification | 🟡 Public interfaces |
| `Encina.IntegrationTests` | Real database/external | 🟡 Database operations |
| `Encina.LoadTests` | Performance under load | 🟡 Critical paths |
| `Encina.BenchmarkTests` | Micro-benchmarks | 🟡 Hot paths |

**Coverage target**: ≥85% line coverage across all test types combined.

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

#### Test Output Conventions

**IMPORTANT**: All test outputs MUST go to the `artifacts/` directory, never to the repository root.

| Output Type | Location |
|-------------|----------|
| Test results | `artifacts/test-results/` |
| Code coverage | `artifacts/coverage/` |
| Benchmark results | `artifacts/performance/` |
| Load test metrics | `artifacts/load-metrics/` |
| Mutation reports | `artifacts/stryker/` |

**Forbidden root-level outputs** (these should NOT exist):

- `TestResults/` ❌
- `test-results.log` ❌
- `coverage-*` ❌
- `BenchmarkDotNet.Artifacts/` ❌

When configuring test projects or scripts:

- Use `--results-directory artifacts/test-results` for `dotnet test`
- Use `--output artifacts/coverage` for coverage collectors
- Configure `runsettings` files to use `artifacts/` paths
- Scripts should write to `artifacts/` subdirectories

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

### .NET 10 / C# 14 Reference (Released November 2025)

> **Important**: .NET 10 is an LTS release supported through November 2028. This section documents key changes and new features since Claude's knowledge cutoff (January 2025).

#### C# 14 New Features

1. **Extension Members** (headline feature):
   - Extension properties, extension operators, and static extension members
   - Defined using `extension` block syntax
   - Can add instance methods, properties, indexers, operators to any type

2. **Field Keyword**:
   - Access auto-implemented property backing field directly: `field`
   - Use in custom get/set accessors without declaring backing field

3. **User-Defined Compound Assignment Operators**:
   - Can now override `+=`, `-=`, etc. explicitly for performance

4. **Other C# 14 Features**:
   - Null-conditional assignment
   - Partial constructors and events
   - File-based apps: run `*.cs` files directly with `dotnet run` (no .csproj needed)

#### .NET 10 Breaking Changes

| Change | Impact |
|--------|--------|
| `dotnet new sln` defaults to SLNX format | New solution format |
| `dotnet restore` audits transitive packages | Security audits enabled |
| OpenAPI 3.1 with breaking schema changes | `Nullable` removed from `OpenApiSchema`, use `JsonSchemaType.Null` |
| `WebHostBuilder`, `IWebHost` obsolete | Use minimal hosting APIs |
| `WithOpenApi` deprecated | Use new OpenAPI generator |
| Default images use Ubuntu | Container base image change |

#### PublicAPI Analyzers (RS0016/RS0017)

The `Microsoft.CodeAnalysis.PublicApiAnalyzers` package tracks public API changes via:

- `PublicAPI.Shipped.txt` - APIs in released versions
- `PublicAPI.Unshipped.txt` - APIs in current development

**Key rules**:

- RS0016: Symbol not in declared API (add to Unshipped.txt)
- RS0017: Symbol in declared API but not public/found (remove from txt)
- RS0036/RS0037: Nullable annotation mismatches

**Fixing RS0016/RS0017**:

1. Use Visual Studio code fix to add/remove entries
2. Manually edit `PublicAPI.Unshipped.txt`
3. Format: `Namespace.Type.Member(params) -> ReturnType`
4. Nullable annotations: `string!` (non-null), `string?` (nullable)

#### Official Documentation Links

- [What's new in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [Breaking changes in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0)
- [ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0)

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

### Build Environment Known Issues

> **Note**: After test consolidation (January 2026), the MSBuild CLR crash issue has been resolved.
> The full solution `Encina.slnx` now builds without issues. Solution filters (`.slnf`) are no longer needed.

#### SQLite DateTime Format Incompatibility

**Problem**: SQLite stores DateTime values as ISO 8601 text (e.g., `2026-01-05T12:30:00.0000000`), but SQLite's built-in `datetime('now')` function returns a different format (`2026-01-05 12:30:00`). This causes datetime comparisons in SQL queries to fail silently.

**Solution**: Never use `datetime('now')` in SQLite Dapper queries. Always use parameterized `@NowUtc` with `DateTime.UtcNow` from C#:

```csharp
// ❌ WRONG - datetime('now') format incompatible with ISO 8601
var sql = "SELECT * FROM Messages WHERE ProcessedAtUtc < datetime('now')";

// ✅ CORRECT - Use parameterized DateTime from C#
var nowUtc = DateTime.UtcNow;
var sql = "SELECT * FROM Messages WHERE ProcessedAtUtc < @NowUtc";
await connection.QueryAsync<Message>(sql, new { NowUtc = nowUtc });
```

**Affected stores**: All `Encina.Dapper.Sqlite` stores use `TimeProvider` or `DateTime.UtcNow` for time-based queries.

**For tests**: When testing time-based behavior, use `TimeProvider` injection for deterministic time control.

### Spanish/English

- User communicates in Spanish
- Code, comments, documentation: **English only**
- Commit messages: English
- User-facing messages: Spanish when responding to user
- **Translation Rule**: If you encounter any Spanish comments in code while editing, translate them to English

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
8. ❌ Don't implement provider features for only some providers - ALL 13 required (see Multi-Provider Implementation Rule)
9. ❌ Don't skip test types without creating a justification `.md` file
10. ❌ Don't leave test coverage below 85%

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

Nunca dejaremos sin resolver o anotar un problema identificado. Lo normal será resolver en el momento, pero si resolverlo significa cambiar significativamente el limitado contexto, lo que haremos es anotarlo, para no alvidarlo y resolverlo en otro momento con otro contexto. Para anotarlo, abriremos una Issue con el problema encintrado, y continuaremos con nuestra tarea. Al final en el resumen informaremos de las issues abiertas.

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

## CodeRabbit Integration

This project uses [CodeRabbit](https://coderabbit.ai) for AI-powered code reviews and issue management. Configuration is in `.coderabbit.yaml`.

### CodeRabbit Features

#### 1. Pull Request Reviews

CodeRabbit automatically reviews all PRs with:

- Code quality analysis
- Security vulnerability detection
- Best practices suggestions
- .NET 10 / C# 14 specific guidance

**Interacting with CodeRabbit in PRs:**

| Command | Effect |
|---------|--------|
| `@coderabbitai review` | Request a new review |
| `@coderabbitai resolve` | Mark all comments as resolved |
| `@coderabbitai plan` | Generate implementation plan |
| `@coderabbitai help` | Show available commands |

#### 2. Issue Enrichment (Automatic)

When you **create or edit** an issue, CodeRabbit automatically:

- **Detects duplicates** - Finds existing issues that may be duplicates
- **Finds related issues** - Links similar issues for context
- **Finds related PRs** - Shows PRs that addressed similar problems
- **Suggests assignees** - Recommends team members based on expertise
- **Auto-labels** - Categorizes issues with appropriate labels

> **Note**: CodeRabbit only triggers on new/edited issues, not existing ones. To enrich an existing issue, make a minor edit to trigger re-analysis.

#### 3. Linked Issue Validation

When a PR references an issue (e.g., `Fixes #123`), CodeRabbit:

1. Analyzes the issue title and description
2. Examines the PR changes
3. Validates if changes meet the requirements
4. Provides assessment: ✅ Addressed, ❌ Not addressed, or ❓ Unclear

**Best practices for linked issue validation:**

- Write clear, technical issue titles
- Include specific acceptance criteria in descriptions
- Use consistent terminology between issues and PRs
- Reference issues with `Fixes #N` or `Closes #N` in PR description

#### 4. Plan Mode (Implementation Planning)

CodeRabbit can generate detailed implementation plans from issues:

**How to trigger:**

- Check the "Create Plan" checkbox in the enrichment comment
- Or comment `@coderabbitai plan` on the issue

**What it generates:**

- Step-by-step implementation plan
- File-specific guidance on what to change
- Code examples from the codebase
- Testing recommendations

**Use with Claude Code:**
The generated plan can be copied and used as context for Claude Code sessions. This is especially useful for complex features.

#### 5. Configuration (`.coderabbit.yaml`)

Key settings for this project:

```yaml
language: es-ES  # Spanish summaries
reviews:
  high_level_summary: true
  path_instructions:
    - path: "src/**/*.cs"
      instructions: ".NET 10, C# 14, zero warnings..."
  path_filters:
    - "!docs/INVENTORY.md"  # Excluded from review
```

**Auto-planning by label:**

```yaml
issue_enrichment:
  planning:
    auto_planning:
      enabled: true
      labels:
        - "plan-me"      # Issues with this label get auto-planned
        - "!no-plan"     # Issues with this label are excluded
```

### Workflow: Issues + CodeRabbit + Claude Code

1. **Create Issue** → CodeRabbit enriches with related issues/PRs
2. **Request Plan** → `@coderabbitai plan` generates implementation steps
3. **Copy Plan** → Use as context in Claude Code session
4. **Create PR** → Reference issue with `Fixes #N`
5. **CodeRabbit Reviews** → Validates PR addresses issue requirements
6. **Iterate** → Address CodeRabbit comments
7. **Merge** → Issue auto-closes

### Tips for Effective CodeRabbit Usage

1. **Write detailed issue descriptions** - CodeRabbit uses titles and descriptions (not comments) for analysis
2. **Use conventional commits** - Helps CodeRabbit understand change context
3. **Reference issues in PRs** - Enables linked issue validation
4. **Review the enrichment** - Check related issues/PRs for context before starting work
5. **Leverage Plan Mode** - For complex features, let CodeRabbit analyze the codebase first

### Requesting CodeRabbit Analysis on Existing Issues

CodeRabbit only enriches issues automatically when they are **created or edited**. For existing issues without CodeRabbit analysis:

1. **Comment to trigger analysis**: Add a comment like `@coderabbitai please review this issue`
2. **Wait for response**: CodeRabbit will analyze and reply with enrichment (duplicates, related issues, suggestions)
3. **Request a plan**: Use `@coderabbitai plan` to generate implementation steps

**Standard workflow when reviewing an issue without CodeRabbit comments:**

```
@coderabbitai please analyze this issue and suggest related issues/PRs
```

This practice ensures we get CodeRabbit's insights on all issues we work on, not just newly created ones.
