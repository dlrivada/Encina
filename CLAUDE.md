# Claude Code - Encina Guidelines

## Active Plans

> **IMPORTANT**: Before starting work, check for active plans in `docs/plans/`. Read the plan file to understand the current state and continue from where the last session left off.

| Plan | File | Status |
|------|------|--------|
| Test Consolidation | `docs/plans/test-consolidation-plan.md` | 🟡 In Progress |
| Performance Measurement Infrastructure | `docs/plans/performance-infrastructure-plan.md` | 🟢 Phase 4 implemented (ADR-025) |

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

### Scripting & Tooling Policy (MANDATORY)

> **CRITICAL**: Claude MUST use only PowerShell (`pwsh`) or C# 14 scripts (`.cs` files via `dotnet run`) for any scripting or automation tasks. Python and direct bash/shell scripting are **PROHIBITED**.

#### Allowed Execution Methods

| Method | Usage | Example |
|--------|-------|---------|
| **PowerShell** | Automation, file operations, text processing | `pwsh -Command "Get-ChildItem -Recurse -Filter '*.cs'"` |
| **C# 14 scripts** | Complex logic, code generation, data processing | `dotnet run script.cs` (no .csproj needed) |
| **CLI tools** | Direct invocation only (no bash piping/scripting) | `dotnet build`, `git status`, `gh issue list`, `docker ps` |

#### Prohibited

- ❌ `python` or `python3` for any purpose
- ❌ Bash scripting constructs: `for`/`do`/`done` loops, `if`/`then`/`fi`, bash pipes (`|`), subshells (`$(...)`)
- ❌ Unix commands via Git Bash: `grep`, `find`, `cat`, `ls`, `sed`, `awk`, `head`, `tail`, `wc`, `sort`, `xargs`, `curl`, `tee`, `cut`, `paste`, `shuf`, `unzip`, `basename`, `xxd`, `dd`
- ❌ Here-docs in bash (`<< 'EOF'`) for file creation — use PowerShell `Set-Content` or C# `File.WriteAllText`
- ❌ Inline bash scripts with `sh -c` or `bash -c`

#### PowerShell Equivalents for Common Operations

| Operation | ❌ Unix/Bash | ✅ PowerShell |
|-----------|-------------|--------------|
| Search file contents | `grep -r "pattern" src/` | `Get-ChildItem -Recurse src/ -Filter *.cs \| Select-String "pattern"` |
| Find files | `find . -name "*.cs" -type f` | `Get-ChildItem -Recurse -Filter "*.cs" -File` |
| Read file | `cat file.txt` | `Get-Content file.txt` |
| List directory | `ls -la` | `Get-ChildItem` |
| Count lines | `wc -l file.txt` | `(Get-Content file.txt).Count` |
| Replace text | `sed -i 's/old/new/g' file.txt` | `(Get-Content file.txt) -replace 'old','new' \| Set-Content file.txt` |
| First N lines | `head -n 10 file.txt` | `Get-Content file.txt \| Select-Object -First 10` |
| Download URL | `curl -s https://...` | `Invoke-RestMethod https://...` |
| Write file | `echo "content" > file.txt` | `Set-Content -Path file.txt -Value "content"` |
| Append to file | `echo "line" >> file.txt` | `Add-Content -Path file.txt -Value "line"` |
| Loop over files | `for f in *.cs; do ...; done` | `Get-ChildItem *.cs \| ForEach-Object { ... }` |
| Check file exists | `if [ -f "file" ]; then ...` | `if (Test-Path "file") { ... }` |
| Delete file | `rm file.txt` | `Remove-Item file.txt` |
| JSON processing | `jq '.data'` | `(Get-Content file.json \| ConvertFrom-Json).data` |
| Process kill | `kill -9 PID` | `Stop-Process -Id PID -Force` |

#### C# 14 Script Examples (No .csproj Required)

**Simple file processing:**

```csharp
// process-files.cs
var files = Directory.GetFiles("src", "*.cs", SearchOption.AllDirectories);
foreach (var file in files)
{
    var content = File.ReadAllText(file);
    if (content.Contains("TODO"))
        Console.WriteLine($"Found TODO in: {file}");
}
```

Run: `dotnet run process-files.cs`

**API generation / code scaffolding:**

```csharp
// append-api.cs
var lines = new List<string>
{
    "Namespace.Type.Method(params) -> ReturnType",
    "Namespace.Type.Property.get -> string!"
};
File.AppendAllLines("src/Package/PublicAPI.Unshipped.txt", lines);
Console.WriteLine($"Appended {lines.Count} entries");
```

Run: `dotnet run append-api.cs`

#### Why This Policy Exists

1. **Consistency**: One scripting language (PowerShell) + one programming language (C#) instead of bash+python+PowerShell
2. **Windows-native**: PowerShell is the natural shell for Windows development
3. **Type safety**: C# 14 scripts provide compile-time checks that bash/python lack
4. **Maintainability**: C# scripts can reference the same NuGet packages as the project
5. **Alignment**: Project is 100% .NET — scripting should be too

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

> **CRITICAL**: All provider-dependent features MUST be implemented for ALL 10 providers. This is a fundamental project rule, not just a testing requirement.

**The 10 Providers:**

| Category | Providers | Count |
|----------|-----------|-------|
| **ADO.NET** | SqlServer, PostgreSQL, MySQL | 3 |
| **Dapper** | SqlServer, PostgreSQL, MySQL | 3 |
| **EF Core** | SqlServer, PostgreSQL, MySQL | 3 |
| **MongoDB** | MongoDB | 1 |

> **Note**: Oracle was removed from pre-1.0 scope due to disproportionate maintenance cost. See [ADR-009](docs/architecture/adr/009-remove-oracle-provider-pre-1.0.md) for details. Oracle code is preserved in `.backup/oracle/` for potential future restoration.
>
> **Note**: SQLite was removed from the supported provider matrix. It lacks features required for production use (proper DateTime/DateTimeOffset handling, concurrent write access, distributed scenarios). Source packages (`Encina.ADO.Sqlite`, `Encina.Dapper.Sqlite`) remain in the codebase but are not tested or guaranteed.

**When this rule applies:**

- Implementing any store (OutboxStore, InboxStore, SagaStore, ScheduledMessageStore, etc.)
- Implementing repositories, Unit of Work, bulk operations
- Any feature that interacts with database-specific SQL or connection types
- Registering services in `ServiceCollectionExtensions`

**Provider-specific SQL differences to consider:**

| Provider | Parameters | LIMIT | Boolean | Notes |
|----------|------------|-------|---------|-------|
| SQL Server | `@param` | `TOP (@n)` | `bit` | Native DateTime, GUID |
| PostgreSQL | `@param` | `LIMIT @n` | `true/false` | Case-sensitive identifiers |
| MySQL | `@param` | `LIMIT @n` | `0/1` | Backtick identifiers |

**Excluded from this rule** (specialized providers with different purposes):

- Message brokers: RabbitMQ, Kafka, NATS, MQTT
- Caching: Redis, Memory
- Event sourcing: Marten

#### Specialized Provider Categories (Beyond the 10 Database Providers)

Beyond the 10 database providers, Encina has **specialized provider categories** that apply to specific feature areas. Each category has its own coherence rules.

##### 1. Caching Providers (8 providers)

| Provider | Type | Characteristics |
|----------|------|-----------------|
| **Encina.Caching.Memory** | L1 (In-Memory) | `IMemoryCache` wrapper, no distribution |
| **Encina.Caching.Hybrid** | L1+L2 (Multi-tier) | .NET 10 `HybridCache`, best of both |
| **Encina.Caching.Redis** | L2 (Distributed) | StackExchange.Redis, industry standard |
| **Encina.Caching.Valkey** | L2 (Distributed) | Open-source Redis fork (LF project) |
| **Encina.Caching.Dragonfly** | L2 (Distributed) | Redis-compatible, lower latency |
| **Encina.Caching.Garnet** | L2 (Distributed) | Microsoft's Redis alternative (C# native) |
| **Encina.Caching.KeyDB** | L2 (Distributed) | Redis-compatible, multi-threaded |
| **Encina.Caching.Memcached** | L2 (Distributed) | Simple protocol (planned) |

**When caching provider rules apply:**

- Implementing `ICacheProvider`
- Implementing `IPubSubProvider` (for backplane sync)
- Cache stampede protection, eager refresh, fail-safe patterns
- Tag-based invalidation, read/write-through patterns

**All 8 caching providers must support:**

- Get/Set/Remove operations
- TTL and expiration
- Serialization abstraction
- Pub/Sub for backplane (where applicable)

##### 2. Messaging Transport Providers (10 existing + 6 planned)

| Provider | Strategy | Best For |
|----------|----------|----------|
| **Encina.RabbitMQ** | Message Broker | Task distribution, work queues |
| **Encina.AzureServiceBus** | Message Broker | Azure-native, enterprise |
| **Encina.AmazonSQS** | Message Broker | AWS-native, serverless |
| **Encina.Kafka** | Event Streaming | Audit logs, event sourcing, replay |
| **Encina.NATS** | Pub/Sub + JetStream | Real-time, IoT, edge |
| **Encina.Redis.PubSub** | Pub/Sub | Real-time broadcasting |
| **Encina.MQTT** | Pub/Sub (IoT) | IoT devices, constrained networks |
| **Encina.InMemory** | Testing | Unit tests, development |
| **Encina.gRPC** | Request/Response | Service-to-service RPC |
| **Encina.GraphQL** | Query Language | API gateway, BFF |

**Planned transports (v0.15.0):**

- `Encina.GoogleCloudPubSub` - GCP coverage
- `Encina.AmazonEventBridge` - AWS EventBridge
- `Encina.Pulsar` - Apache Pulsar
- `Encina.Redis.Streams` - Redis Streams
- `Encina.ActiveMQ` - Apache ActiveMQ Artemis
- `Encina.Dapr` - Dapr abstraction layer

**When messaging transport rules apply:**

- Implementing `IMessageTransport`
- Outbox publishing to external brokers
- Inbox consumption from external sources
- Dead letter queue handling

**All messaging transports must support:**

- Send/Publish operations
- Subscription management
- Error handling and DLQ
- Message metadata propagation

##### 3. Distributed Lock Providers (4 existing + 8 planned)

| Provider | Backend | Mechanism |
|----------|---------|-----------|
| **Encina.DistributedLock.InMemory** | In-Memory | Single-process (testing) |
| **Encina.DistributedLock.Redis** | Redis | Redlock algorithm |
| **Encina.DistributedLock.SqlServer** | SQL Server | `sp_getapplock` |
| **Encina.DistributedLock.PostgreSQL** | PostgreSQL | `pg_advisory_lock` (planned) |
| **Encina.DistributedLock.MySQL** | MySQL | `GET_LOCK` (planned) |
| **Encina.DistributedLock.Azure** | Azure Blob | Blob leases (planned) |
| **Encina.DistributedLock.DynamoDB** | DynamoDB | Conditional writes (planned) |
| **Encina.DistributedLock.Consul** | Consul | Sessions (planned) |
| **Encina.DistributedLock.etcd** | etcd | Leases (planned) |
| **Encina.DistributedLock.ZooKeeper** | ZooKeeper | Ephemeral nodes (planned) |

**When distributed lock rules apply:**

- Implementing `IDistributedLockProvider`
- Leader election features
- Resource coordination patterns

**All lock providers must support:**

- TryAcquire with timeout
- Auto-release on timeout
- Cancellation token support

##### 4. Validation Providers (3 providers)

| Provider | Framework | Characteristics |
|----------|-----------|-----------------|
| **Encina.FluentValidation** | FluentValidation | Fluent API, complex rules, async |
| **Encina.DataAnnotations** | DataAnnotations | Attribute-based, built-in |
| **Encina.MiniValidator** | MiniValidator | Lightweight, zero-allocation |

**When validation provider rules apply:**

- Implementing `IValidationProvider`
- Registration via `ServiceCollectionExtensions`

**All validation providers must:**

- Integrate with `ValidationOrchestrator`
- Return `ValidationResult` with errors
- Support the same `ValidationPipelineBehavior`

##### 5. Scheduling Providers (2 + adapters)

| Provider | Backend | Characteristics |
|----------|---------|-----------------|
| **Encina.Messaging** (built-in) | Database | In-database scheduling, simple |
| **Encina.Hangfire** | Hangfire | Persistent jobs, dashboard |
| **Encina.Quartz** | Quartz.NET | Advanced triggers, clustering |

**When scheduling rules apply:**

- Implementing `IScheduledMessageStore`
- Scheduler backend adapters

##### 6. Event Sourcing Providers (1 primary)

| Provider | Backend | Characteristics |
|----------|---------|-----------------|
| **Encina.Marten** | PostgreSQL | Event store + document DB |
| **Encina.EventStoreDB** | EventStoreDB | Dedicated event store (future) |

**When event sourcing rules apply:**

- Implementing aggregate repositories
- Projection infrastructure
- Snapshot handling
- GDPR compliance (crypto-shredding)

##### 7. Cloud/Serverless Providers (3 providers)

| Provider | Platform | Triggers |
|----------|----------|----------|
| **Encina.AzureFunctions** | Azure Functions | HTTP, Queue, Timer, Durable |
| **Encina.AwsLambda** | AWS Lambda | HTTP, SQS, CloudWatch, API Gateway |
| **Encina.GoogleCloudFunctions** | GCP Functions | HTTP, Pub/Sub (planned) |

**Cloud provider triangle rule:**
When implementing cloud-specific features, consider AWS/Azure/GCP coverage.

##### 8. Resilience Providers (3 providers)

| Provider | Framework | Use Case |
|----------|-----------|----------|
| **Encina.Polly** | Polly | Circuit breaker, retry, bulkhead |
| **Encina.Extensions.Resilience** | Microsoft.Extensions.Resilience | .NET 10 native patterns |
| **Encina.Extensions.Http.Resilience** | Microsoft.Extensions.Http.Resilience | HTTP client resilience |

##### 9. Observability Providers (1 + exporters)

| Provider | Backend | Purpose |
|----------|---------|---------|
| **Encina.OpenTelemetry** | OpenTelemetry SDK | Tracing, metrics, instrumentation |
| **Encina.OpenTelemetry.AzureMonitor** | Azure Monitor | Azure exporter (planned) |
| **Encina.OpenTelemetry.AwsXRay** | AWS X-Ray | AWS exporter (planned) |
| **Encina.OpenTelemetry.Prometheus** | Prometheus | Metrics scraping (planned) |

##### 10. Testing Providers (12 packages)

| Provider | Purpose |
|----------|---------|
| **Encina.Testing** | Core test fixtures, fluent assertions |
| **Encina.Testing.Fakes** | Fake IEncina, stores, collectors |
| **Encina.Testing.Respawn** | Database reset between tests |
| **Encina.Testing.WireMock** | HTTP mocking |
| **Encina.Testing.Shouldly** | Shouldly assertions |
| **Encina.Testing.Verify** | Snapshot testing |
| **Encina.Testing.Bogus** | Fake data generation |
| **Encina.Testing.FsCheck** | Property-based testing |
| **Encina.Testing.Architecture** | ArchUnitNET rules |
| **Encina.Testing.Testcontainers** | Docker-based DB testing |
| **Encina.Testing.TUnit** | NativeAOT-compatible tests |
| **Encina.Testing.Pact** | Contract testing |

#### Provider Applicability Matrix

Use this matrix to determine which providers apply to each feature type:

| Feature Type | Database (10) | Caching (8) | Transport (10+) | Lock (4+) | Validation (3) |
|--------------|:-------------:|:-----------:|:---------------:|:---------:|:--------------:|
| **Outbox/Inbox/Saga** | ✅ Required | ❌ | ❌ | ❌ | ❌ |
| **Scheduled Messages** | ✅ Required | ❌ | ❌ | ❌ | ❌ |
| **Query Caching** | ❌ | ✅ Required | ❌ | ❌ | ❌ |
| **Message Publishing** | ❌ | ❌ | ✅ Required | ❌ | ❌ |
| **Resource Locking** | ❌ | ❌ | ❌ | ✅ Required | ❌ |
| **Request Validation** | ❌ | ❌ | ❌ | ❌ | ✅ Required |
| **Unit of Work** | ✅ Required | ❌ | ❌ | ❌ | ❌ |
| **Multi-Tenancy** | ✅ Required | ✅ Where applicable | ✅ Where applicable | ❌ | ❌ |
| **Audit Trail** | ✅ Required | ❌ | ❌ | ❌ | ❌ |

#### When to Consider Each Provider Category

| Scenario | Providers to Consider |
|----------|----------------------|
| Implementing a messaging store feature | All 10 database providers |
| Adding a new cache pattern | All 8 caching providers |
| Creating a new transport-agnostic feature | All messaging transports |
| Adding cloud-specific feature | AWS + Azure + GCP (triangle) |
| Creating a validation pattern | All 3 validation providers |
| Adding distributed coordination | All distributed lock providers |

> **Rule of thumb**: If a feature touches provider-specific code, it must be implemented consistently across ALL providers in that category.

#### Cross-Cutting Integration Rule (MANDATORY)

> **CRITICAL**: Every new feature MUST be evaluated against ALL cross-cutting (transversal) functions. Missing integrations create invisible gaps that compound over time. See [ADR-018](docs/architecture/adr/018-cross-cutting-integration-principle.md) for the architectural decision behind this rule.

**The 12 Transversal Functions:**

| # | Function | Key Question | Integration Pattern |
|---|----------|-------------|-------------------|
| 1 | **Caching** | Does this feature read data that benefits from caching? | `ICacheProvider`, decorator pattern, `[Cache]` attribute |
| 2 | **OpenTelemetry** | Does this feature perform operations worth tracing/metering? | `ActivitySource`, `Meter`, semantic attributes |
| 3 | **Structured Logging** | Does this feature need operational visibility? | `Log.cs` with `[LoggerMessage]`, EventId range |
| 4 | **Health Checks** | Does this feature have a health-checkable dependency? | `IEncinaHealthCheck` implementation |
| 5 | **Validation** | Does this feature receive input that needs validation? | `IValidationProvider`, pipeline behavior |
| 6 | **Resilience** | Does this feature call external systems that can fail? | Polly retry, circuit breaker, timeout |
| 7 | **Distributed Locks** | Does this feature have concurrent access to shared state? | `IDistributedLockProvider` |
| 8 | **Transactions** | Does this feature need atomic multi-operation guarantees? | `IUnitOfWork`, `TransactionPipelineBehavior` |
| 9 | **Idempotency** | Can this feature receive duplicate requests? | `InboxPipelineBehavior`, deduplication key |
| 10 | **Multi-Tenancy** | Does this feature store/query data that belongs to a tenant? | `TenantId` field, `ITenantContext` |
| 11 | **Module Isolation** | In modular monolith, does this feature need module scoping? | `ModuleId` field, `IModuleContext` |
| 12 | **Audit Trail** | Does this feature perform operations with compliance/security implications? | `IAuditStore`, audit events |

**When this rule applies:** Every feature that creates new entities, stores, pipeline behaviors, background services, or external integrations.

**Required outcome for each of the 12 functions:**

- ✅ **Integrate** — Implement the integration in the current feature
- ⏭️ **Defer** — Create a GitHub Issue for future integration (reference it in the plan/PR)
- ❌ **Not Applicable** — Document WHY in the plan or PR description (1 sentence)

**Examples of common misses this rule prevents:**

| New Feature | Commonly Missed Integrations |
|-------------|------------------------------|
| New store/entity | OpenTelemetry, TenantId, ModuleId, Health Check |
| New background service | Distributed Locks, Leader Election, Structured Logging |
| New external integration | Resilience (retry/circuit breaker), Health Check, OpenTelemetry |
| New pipeline behavior | Validation, Idempotency, Audit Trail |
| New messaging pattern | Transactions, Distributed Locks, Multi-Tenancy |

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

#### Per-Flag Coverage System (Obligations Model) — CRITICAL

> **CRITICAL**: The coverage system uses a **per-flag obligations model**. Each test type (unit, guard, contract, property, integration) is a separate "flag". Coverage is measured **independently per flag** — a line covered by unit tests does NOT count toward the guard or contract target. Each flag must independently reach its own target percentage.

**How it works:**

1. **Manifest** (`.github/coverage-manifest/{Package}.json`): Defines which test types apply to each source file and the target percentage per flag (e.g., `"unit": 70, "guard": 20, "contract": 15, "property": 15`).

2. **CI Full** runs each test project separately (`Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.PropertyTests`, `Encina.IntegrationTests`) and collects Cobertura XML coverage per flag.

3. **Coverage report** (`coverage-report.cs`): For each source file, counts coverable lines **per flag independently**. The number of coverable lines differs per flag because each test type exercises different code paths — POCO properties, infrastructure glue, business rules, and guard clauses have different coverability per test type. For a file with flags U+G+C, the obligations are A+B+C where A, B, C are the coverable lines for each flag (NOT the same value repeated). Example: a file might have 200 unit-coverable lines, 150 guard-coverable lines, and 80 contract-coverable lines = 430 total obligations.

4. **Dashboard** (`dlrivada.github.io/Encina/coverage/`): Shows per-package per-flag coverage. A package is "green" only when ALL applicable flags reach their targets.

**CRITICAL RULE — Tests must execute real package code:**

| Approach | Coverage Impact | Example |
|----------|----------------|---------|
| ✅ **Instantiate and call** | Covers lines | `new DefaultBreachDetector([]).DetectAsync(event)` |
| ✅ **Create domain objects** | Covers factory lines | `BreachRecord.Create("nature", ...)` |
| ✅ **Call validators** | Covers validation lines | `validator.Validate(null, options)` |
| ❌ **Reflection only** | Covers ZERO lines | `typeof(IBreachDetector).GetMethod("DetectAsync")` |
| ❌ **Only assert on types** | Covers ZERO lines | `typeof(T).IsInterface.ShouldBeTrue()` |

Reflection-based tests (e.g., `typeof(ISomeInterface).GetMethod(...)`) do NOT execute any code in the target assembly — they only load metadata. The Cobertura coverage tool reports 0 lines covered for the target package. **Contract and property tests MUST instantiate real implementations** from the package to generate coverage.

**Workflow discipline:**

- Check the manifest to know which test types are required and their target percentages
- Do NOT push/merge until ALL required test types for the package reach their targets
- CI Full takes ~40 min — avoid triggering it prematurely with incomplete work
- Use `dotnet test` locally to verify tests pass before pushing

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
dotnet run --file .github/scripts/run-integration-tests.cs
```

#### Collection Fixtures (Container Reduction Strategy)

> **CRITICAL**: Integration tests use shared xUnit `[Collection]` fixtures to keep Docker containers low (~23 instead of ~71). **NEVER** create per-class fixtures for database tests.

**Rule**: Every integration test class MUST use `[Collection("Provider-Database")]` to share a single Docker container per provider/database combination.

**Existing collections** (defined in `Collections.cs` files):

| Collection | Fixture | Notes |
| ---------- | ------- | ----- |
| `ADO-SqlServer` | `SqlServerFixture` | |
| `ADO-PostgreSQL` | `PostgreSqlFixture` | |
| `ADO-MySQL` | `MySqlFixture` | |
| `Dapper-SqlServer` | `SqlServerFixture` | |
| `Dapper-PostgreSQL` | `PostgreSqlFixture` | |
| `Dapper-MySQL` | `MySqlFixture` | |
| `EFCore-SqlServer` | `EFCoreSqlServerFixture` | |
| `EFCore-PostgreSQL` | `EFCorePostgreSqlFixture` | |
| `EFCore-MySQL` | `EFCoreMySqlFixture` | |

**New test class template:**

```csharp
[Collection("ADO-PostgreSQL")]  // REQUIRED - shares fixture across all classes
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public class MyNewTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public MyNewTests(PostgreSqlFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync() => await _fixture.ClearAllDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;
}
```

**Rules:**

1. ❌ NEVER use `IClassFixture<T>` for database fixtures - always use `[Collection]`
2. ❌ NEVER call `new SqlServerFixture()` or `_fixture = new()` - inject via constructor
3. ❌ NEVER call `_fixture.DisposeAsync()` from tests - the collection owns the lifecycle
4. ✅ Use `_fixture.ClearAllDataAsync()` in `InitializeAsync()` for data cleanup
5. ✅ Use `_fixture.CreateConnection()` for shared connections

See [Integration Test Container Strategy](docs/testing/integration-tests.md#collection-fixture-strategy) for full details.

#### Test Organization

```
tests/
├── Encina.UnitTests/          # Consolidated unit tests (9,164 tests as of v0.12.0-dev, Feb 2026)
│   ├── ADO/
│   ├── AspNetCore/
│   ├── Caching/
│   ├── Core/
│   ├── Dapper/
│   ├── DomainModeling/
│   ├── EntityFrameworkCore/
│   ├── Messaging/
│   └── ...
├── Encina.IntegrationTests/   # Consolidated integration tests (2,251 tests)
├── Encina.PropertyTests/      # Property-based tests (352 tests)
├── Encina.ContractTests/      # Contract tests (247 tests)
├── Encina.GuardTests/         # Guard clause tests (1,037 tests)
├── Encina.LoadTests/          # Load testing harness
├── Encina.NBomber/            # NBomber scenarios
├── Encina.BenchmarkTests/     # BenchmarkDotNet benchmarks
│   ├── Encina.Benchmarks/
│   ├── Encina.AspNetCore.Benchmarks/
│   └── ...
├── Encina.TestInfrastructure/ # Shared test infrastructure
└── Encina.Testing.Examples/   # Reference examples
```

#### Test Coverage for All 10 Providers

Tests MUST cover ALL 10 providers as defined in the [Multi-Provider Implementation Rule](#multi-provider-implementation-rule-mandatory) section above.

> **Reminder**: The 10 providers are: ADO.NET (3), Dapper (3), EF Core (3), and MongoDB (1).

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

#### BenchmarkDotNet Guidelines

> **CRITICAL**: Follow these rules when creating or modifying benchmarks to avoid common pitfalls discovered during Issue #564.

**1. Use BenchmarkSwitcher, NOT BenchmarkRunner**

```csharp
// ❌ WRONG - BenchmarkRunner.Run<T>() IGNORES command-line arguments like --filter
BenchmarkRunner.Run<MyBenchmarks>(config);

// ✅ CORRECT - BenchmarkSwitcher supports --filter, --list, --job, etc.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
```

**Why this matters**: Without `BenchmarkSwitcher`, you cannot filter benchmarks at runtime. Running 200+ benchmarks when you only need 10 wastes hours of time.

**2. Materialize IQueryable Results**

BenchmarkDotNet validates return values. Deferred execution (like `IQueryable<T>`) causes validation errors:

```csharp
// ❌ WRONG - Returns deferred execution, BenchmarkDotNet rejects it
public IQueryable<Entity> GetEntities()
{
    return _context.Entities.Where(e => e.IsActive);
}

// ✅ CORRECT - Materialize with .ToList()
public List<Entity> GetEntities()
{
    return _context.Entities.Where(e => e.IsActive).ToList();
}
```

**Error you'll see**: "Benchmark returns a deferred execution result (IQueryable<T>). You need to either change the method declaration to return a materialized result or consume it on your own."

**3. Verify Filtering Before Full Execution**

Always verify your filter works before running the full benchmark suite:

```bash
# Step 1: List benchmarks that will run (fast)
dotnet run -c Release -- --list flat --filter "*EntityFrameworkCore*"

# Step 2: Only if list looks correct, run the actual benchmarks
dotnet run -c Release -- --filter "*EntityFrameworkCore*" --job short
```

**4. Test Entities Must Match Benchmark Operations**

Don't use `AddInclude()` on entities without navigation properties:

```csharp
// ❌ WRONG - BenchmarkEntity has no "Category" navigation property
public class StringIncludeSpec : QuerySpecification<BenchmarkEntity>
{
    public StringIncludeSpec()
    {
        AddInclude("Category"); // This will fail or produce NA results
    }
}

// ✅ CORRECT - Only include existing navigation properties
public class StringIncludeSpec : QuerySpecification<Order>
{
    public StringIncludeSpec()
    {
        AddInclude("Customer"); // Customer exists as navigation property
    }
}
```

**5. Common BenchmarkDotNet Arguments**

| Argument | Purpose | Example |
|----------|---------|---------|
| `--list flat` | List matching benchmarks without running | `--list flat --filter "*Repo*"` |
| `--filter` | Run only matching benchmarks | `--filter "*EntityFrameworkCore*"` |
| `--job short` | Quick validation run (3 iterations) | `--job short` |
| `--job dry` | Validate setup without execution | `--job dry` |
| `--exporters` | Control output formats | `--exporters json markdown` |

**6. Benchmark Output Location**

All benchmark results go to `artifacts/performance/`:

```csharp
var config = DefaultConfig.Instance
    .WithArtifactsPath(Path.Combine(
        Directory.GetCurrentDirectory(),
        "..", "..", "..", "..", "..",
        "artifacts", "performance", "results"));
```

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
dotnet run --file .github/scripts/run-stryker.cs
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

### Structured Logging & EventId Allocation (MANDATORY)

> **CRITICAL**: Every `[LoggerMessage]` EventId MUST be registered in the central registry before use. See [ADR-021](docs/architecture/adr/021-eventid-uniqueness-enforcement.md) for the full architectural decision.

#### Central Registry

- **Location**: `src/Encina/Diagnostics/EventIdRanges.cs`
- **Format**: `public static readonly (int Min, int Max) RangeName = (min, max);`
- **Discovery**: `EventIdRanges.GetAllRanges()` returns all registered allocations

#### Current Range Map (Quick Reference)

| Area | Range | Packages |
|------|-------|----------|
| Core | 1-99 | Sanitization |
| DomainModeling | 1100-1699 | Repository, UoW, Bulk, Spec, SoftDelete, Audit |
| Security Audit | 1700-1799 | Read Audit |
| Infrastructure | 1800-1999 | Tenancy, Module Isolation |
| Messaging | 2000-2499 | Outbox, Inbox, Saga, Scheduling, QueryCache, Encryption |
| Domain Events / ES | 2500-2699 | DomainEvents, AuditMarten |
| Security | 8000-8099 | Security (8000-8009), PII (8010-8029), IdGen (8030-8099) |
| Compliance | 8100-8949 | GDPR, Consent, DSR, LawfulBasis, Anonymization, CryptoShredding, Retention, DataResidency, BreachNotification, DPIA, PrivacyByDesign |
| Security Extensions | 9000-9199 | ABAC, AntiTampering |
| Compliance Extensions | 9200-9499 | NIS2, CrossBorderTransfer, ProcessorAgreements |
| Reserved | 9500-9999 | Future modules |

#### Allocation Workflow (When Adding Structured Logging to a Feature)

1. **Check** `EventIdRanges.cs` for the next free range in the appropriate area
2. **Register** a new `public static readonly (int Min, int Max)` field with an appropriate size (typically 50 or 100 slots)
3. **Create** your `Diagnostics/*LogMessages.cs` file with EventIds **within** the registered range
4. **Update** `PublicAPI.Unshipped.txt` for the new public field
5. **Run** architecture tests to verify no collisions or range violations

#### Rules

- ❌ NEVER use an EventId without registering its range first
- ❌ NEVER assign EventIds outside your package's registered range
- ❌ NEVER create sparse allocations with large gaps (e.g., 8400, 8410, 8420...) — pack sequentially to avoid overflowing the range
- ✅ Use `[LoggerMessage]` source generator (not `LoggerMessage.Define`) for new code
- ✅ Group EventIds by functional area within your range (pipeline, enforcement, health check, etc.)
- ✅ Add XML doc comments referencing the range: `/// Event IDs: 8120-8133 (see EventIdRanges.ComplianceGDPR)`

#### Architecture Test Enforcement

The `EventIdUniquenessRule` class in `Encina.Testing.Architecture` provides:

| Method | Purpose |
|--------|---------|
| `AssertEventIdsAreGloballyUnique()` | No duplicate EventIds across all assemblies |
| `AssertEventIdsWithinRegisteredRanges()` | Every EventId within its registered range |
| `AssertNoRangeOverlaps()` | No two registered ranges overlap |
| `GenerateAllocationReport()` | Human-readable allocation table |

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
8. ❌ Don't implement database features for only some providers - ALL 10 database providers required
9. ❌ Don't implement caching features for only some providers - ALL 8 caching providers required
10. ❌ Don't implement messaging features for only some transports - consider ALL applicable transports
11. ❌ Don't implement cloud features without considering AWS/Azure/GCP triangle
12. ❌ Don't skip test types without creating a justification `.md` file
13. ❌ Don't leave test coverage below 85%
14. ❌ Don't use `BenchmarkRunner.Run<T>()` - use `BenchmarkSwitcher.FromAssembly().Run(args, config)`
15. ❌ Don't return `IQueryable<T>` from benchmark methods - always materialize with `.ToList()`
16. ❌ Don't use `[LoggerMessage]` EventIds without registering the range in `EventIdRanges.cs` first
17. ❌ Don't create sparse EventId allocations (e.g., 8400, 8410, 8420...) — pack sequentially to stay within range

> **Provider Rules Summary**: See [Specialized Provider Categories](#specialized-provider-categories-beyond-the-10-database-providers) for detailed rules on each provider category.

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

| Template | Prefix | Use Case | Default Label |
|----------|--------|----------|---------------|
| `bug_report.md` | `[BUG]` | Bugs or unexpected behavior in Encina code | `bug` |
| `feature_request.md` | `[FEATURE]` | New features or enhancements | `enhancement` |
| `technical_debt.md` | `[DEBT]` | Code quality issues, missing implementations, optimization | `technical-debt` |
| `test_implementation.md` | `[TEST]` | New tests, benchmarks, load tests, coverage gaps | `area-testing` |
| `architecture_spike.md` | `[SPIKE]` | Technical investigations, evaluations, architecture decisions | `investigation` |
| `epic.md` | `[EPIC]` | Group related work spanning multiple issues | `epic` |
| `refactoring.md` | `[REFACTOR]` | Code restructuring without behavior change | `enhancement` |
| `infrastructure.md` | `[INFRA]` | CI/CD, Docker, build system, developer tooling | `area-ci-cd` |

**Choosing the right template:**

| Scenario | Template |
|----------|----------|
| Code throws wrong exception | `[BUG]` |
| Add GDPR compliance module | `[FEATURE]` |
| Coverage below 85%, need more tests | `[TEST]` |
| Implement NBomber load tests for Kafka | `[TEST]` |
| Code works but is messy / duplicated | `[DEBT]` |
| Evaluate Aspire vs Testcontainers | `[SPIKE]` |
| Apply Orchestrator pattern to 3 modules | `[REFACTOR]` |
| CI workflow takes 2+ hours | `[INFRA]` |
| 10-issue initiative with phases | `[EPIC]` |
| Failing tests due to code bug | `[BUG]` (not `[TEST]`) |
| Missing tests for existing code | `[TEST]` (not `[DEBT]`) |

**Prefix normalization rules:**

- ❌ `[TECH-DEBT]` → use `[DEBT]`
- ❌ `[TESTING]` → use `[TEST]`
- ❌ `[ARCHITECTURE]` / `[DECISION]` / `[REVIEW]` → use `[SPIKE]`
- ❌ `[Phase N]` → don't use phase prefixes; use milestones instead

#### When to Create Issues

Nunca dejaremos sin resolver o anotar un problema identificado. Lo normal será resolver en el momento, pero si resolverlo significa cambiar significativamente el limitado contexto, lo que haremos es anotarlo, para no olvidarlo y resolverlo en otro momento con otro contexto. Para anotarlo, abriremos una Issue con el problema encontrado, y continuaremos con nuestra tarea. Al final en el resumen informaremos de las issues abiertas.

- **Bugs found during development** → Create immediately with `[BUG]` prefix
- **Technical debt discovered** → Create with `[DEBT]` prefix (don't fix inline if it risks derailing current work)
- **Feature ideas** → Create with `[FEATURE]` prefix for later discussion
- **Missing tests / coverage gaps** → Create with `[TEST]` prefix and link to specific packages
- **Failing tests due to code bug** → Create with `[BUG]` (the bug is in the code, not the tests)
- **Architecture investigations** → Create with `[SPIKE]` prefix with time-boxed scope
- **CI/CD or build issues** → Create with `[INFRA]` prefix
- **Large multi-issue initiatives** → Create with `[EPIC]` prefix, then create child issues
- **Code restructuring** → Create with `[REFACTOR]` prefix (no behavior change)

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
| `docs/releases/vX.Y.Z/` | Version-based release documentation |
| `docs/architecture/adr/*.md` | Architecture Decision Records |
| `docs/roadmap-documentacion.md` | Documentation-specific roadmap |

### When Updating Documentation

- **After completing a feature** → Update `CHANGELOG.md` (Unreleased section)
- **After major implementation phase** → Update `docs/releases/vX.Y.Z/README.md`
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
