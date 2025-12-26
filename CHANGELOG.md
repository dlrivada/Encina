# Changelog

All notable changes to Encina will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> **Note**: Encina is in pre-1.0 development. Version numbers below represent conceptual milestones, not published releases. The first official release will be v1.0.0.

## [Unreleased]

### Added

- Health Check Abstractions (Issue #35):
  - `IEncinaHealthCheck` interface for provider-agnostic health monitoring
  - `HealthCheckResult` struct with `Healthy`/`Degraded`/`Unhealthy` status
  - `EncinaHealthCheck` abstract base class with exception handling
  - `OutboxHealthCheck` for monitoring pending outbox messages
  - `InboxHealthCheck` for monitoring inbox processing state
  - `SagaHealthCheck` for detecting stuck/expired sagas
  - `SchedulingHealthCheck` for monitoring overdue scheduled messages
  - Configurable warning/critical thresholds for all health checks
  - ASP.NET Core integration via `EncinaHealthCheckAdapter`
  - `CompositeEncinaHealthCheck` for aggregating multiple health checks
  - Extension methods: `AddEncinaHealthChecks()`, `AddEncinaOutbox()`, `AddEncinaInbox()`, `AddEncinaSaga()`, `AddEncinaScheduling()`
  - Kubernetes readiness/liveness probe compatible
- Automatic Provider Health Checks (Issue #113):
  - `ProviderHealthCheckOptions` for configuring provider health checks (enabled by default)
  - `DatabaseHealthCheck` abstract base class for database connectivity checks
  - Automatic health check registration when configuring Dapper providers:
    - `PostgreSqlHealthCheck` for PostgreSQL (Encina.Dapper.PostgreSQL)
    - `MySqlHealthCheck` for MySQL/MariaDB (Encina.Dapper.MySQL)
    - `SqlServerHealthCheck` for SQL Server (Encina.Dapper.SqlServer)
    - `OracleHealthCheck` for Oracle Database (Encina.Dapper.Oracle)
    - `SqliteHealthCheck` for SQLite (Encina.Dapper.Sqlite)
  - Automatic health check registration when configuring ADO.NET providers:
    - `PostgreSqlHealthCheck` for PostgreSQL (Encina.ADO.PostgreSQL)
    - `MySqlHealthCheck` for MySQL/MariaDB (Encina.ADO.MySQL)
    - `SqlServerHealthCheck` for SQL Server (Encina.ADO.SqlServer)
    - `OracleHealthCheck` for Oracle Database (Encina.ADO.Oracle)
    - `SqliteHealthCheck` for SQLite (Encina.ADO.Sqlite)
  - `EntityFrameworkCoreHealthCheck` for EF Core DbContext connectivity (Encina.EntityFrameworkCore)
  - `MongoDbHealthCheck` for MongoDB connectivity (Encina.MongoDB)
  - `RedisHealthCheck` for Redis/Valkey/KeyDB/Dragonfly/Garnet connectivity (Encina.Caching.Redis)
  - `RabbitMQHealthCheck` for RabbitMQ broker connectivity (Encina.RabbitMQ)
  - `KafkaHealthCheck` for Apache Kafka broker connectivity (Encina.Kafka)
  - `AzureServiceBusHealthCheck` for Azure Service Bus connectivity (Encina.AzureServiceBus)
  - `AmazonSQSHealthCheck` for Amazon SQS connectivity (Encina.AmazonSQS)
  - `NATSHealthCheck` for NATS server connectivity (Encina.NATS)
  - `MQTTHealthCheck` for MQTT broker connectivity (Encina.MQTT)
  - `MartenHealthCheck` for Marten/PostgreSQL event store connectivity (Encina.Marten)
  - `HangfireHealthCheck` for Hangfire scheduler status (Encina.Hangfire)
  - `QuartzHealthCheck` for Quartz.NET scheduler status (Encina.Quartz)
  - Configurable timeout, tags, and failure status
  - Opt-out via `config.ProviderHealthCheck.Enabled = false`
  - Integration tests with Testcontainers for all providers
  - `SignalRHealthCheck` for SignalR hub connectivity (Encina.SignalR)
  - `GrpcHealthCheck` for gRPC service connectivity (Encina.gRPC)
  - Health check documentation in all provider READMEs
- Modular Monolith support (Issue #57):
  - `IModule` interface for defining application modules
  - `IModuleLifecycle` interface for modules with startup/shutdown hooks
  - `IModuleRegistry` for runtime module discovery and lookup
  - `ModuleConfiguration` for fluent module registration
  - `ModuleLifecycleHostedService` for automatic lifecycle management
  - `AddEncinaModules()` extension method for service registration
  - Automatic handler discovery from module assemblies
  - Module ordering: start in registration order, stop in reverse (LIFO)
- Saga Not Found Handler support (Issue #43):
  - `IHandleSagaNotFound<TMessage>` interface for custom handling when saga correlation fails
  - `SagaNotFoundContext` with `Ignore()` and `MoveToDeadLetterAsync()` actions
  - `SagaNotFoundAction` enum (`None`, `Ignored`, `MovedToDeadLetter`)
  - `ISagaNotFoundDispatcher` for invoking registered handlers
  - `SagaErrorCodes.HandlerCancelled` and `SagaErrorCodes.HandlerFailed` error codes
  - Automatic DI registration when `UseSagas` is enabled

### Changed

- **BREAKING**: `EncinaErrors.Create()` and `EncinaErrors.FromException()` `details` parameter changed from `object?` to `IReadOnlyDictionary<string, object?>?` (Issue #34)
- **BREAKING**: `EncinaErrorExtensions.GetDetails()` now returns `IReadOnlyDictionary<string, object?>` instead of `Option<object>`
- `EncinaException` internal class now stores `Details` as `IReadOnlyDictionary<string, object?>` instead of `object?`
- `GetMetadata()` is now an alias for `GetDetails()` (both return the same dictionary)
- Saga timeout support (Issue #38):
  - `TimeoutAtUtc` property in `ISagaState` interface
  - `SagaStatus.TimedOut` status constant
  - `SagaErrorCodes.Timeout` error code
  - `TimeoutAsync()` method in `SagaOrchestrator` to mark sagas as timed out
  - `GetExpiredSagasAsync()` method in `ISagaStore` and all implementations
  - `StartAsync()` overload with timeout parameter
  - `DefaultSagaTimeout` and `ExpiredSagaBatchSize` options in `SagaOptions`
  - Full implementation across all providers (EF Core, Dapper, MongoDB, ADO.NET)
- Regex timeout protection against ReDoS attacks (S6444) in caching and SignalR components
- SQL injection prevention via `SqlIdentifierValidator` for dynamic table names
- ROP assertion extensions in `Encina.TestInfrastructure.Extensions`:
  - `ShouldBeSuccess()` / `ShouldBeRight()` - Assert Either is Right
  - `ShouldBeError()` / `ShouldBeLeft()` - Assert Either is Left
  - `ShouldBeBottom()` / `ShouldNotBeBottom()` - Assert Either default state
  - `AllShouldBeSuccess()` / `AllShouldBeError()` - Collection assertions
  - `ShouldContainSuccess()` / `ShouldContainError()` - Collection contains assertions
  - `ShouldBeErrorWithCode()`, `ShouldBeValidationError()`, `ShouldBeAuthorizationError()` - EncinaError assertions
  - Async variants: `ShouldBeSuccessAsync()`, `ShouldBeErrorAsync()`, `ShouldBeErrorWithCodeAsync()`

### Changed

- Centralized messaging patterns with shared `Log.cs` and `TransactionPipelineBehavior.cs`
- Improved null handling in `InboxOrchestrator` response deserialization

### Fixed

- SonarCloud coverage detection with proper `SonarQubeTestProject` configuration
- Benchmark CSV parsing and mutation report path detection
- EF Core PropertyTests compilation errors (Issue #116):
  - Removed obsolete FsCheck 2.x files (`Generators.cs`, `OutboxStoreEFFsCheckTests.cs`)
  - Fixed `SagaStatus` type ambiguity in `SagaStoreEFPropertyTests.cs`
  - Fixed `SagaStatus` type ambiguity in `SagaStoreEFIntegrationTests.cs`

---

## [0.9.0] - 2025-12-23

Major milestone: **Orchestrator Pattern** adoption and project stabilization.

### Added

#### Validation Orchestrator Architecture

- `IValidationProvider`, `ValidationResult`, `ValidationError` abstractions in `Encina.Validation` namespace
- `ValidationOrchestrator` for centralized validation orchestration
- `FluentValidationProvider`, `DataAnnotationsValidationProvider`, `MiniValidationProvider` implementations

#### Messaging Orchestrator/Factory Architecture

- `InboxOrchestrator`, `OutboxOrchestrator`, `SagaOrchestrator`, `SchedulerOrchestrator` for centralized domain logic
- Factory interfaces: `IInboxMessageFactory`, `IOutboxMessageFactory`, `ISagaStateFactory`, `IScheduledMessageFactory`
- Factories implemented for all 12 providers (Dapper ×5, ADO ×5, EF Core, MongoDB)

#### Documentation

- Comprehensive Saga patterns documentation with Mermaid diagrams
- Messaging Transports guide with decision flowchart

### Changed

- **BREAKING**: Centralized `ValidationPipelineBehavior` to `Encina.Validation` namespace
- **BREAKING**: Centralized `InboxPipelineBehavior` to `Encina.Messaging.Inbox` namespace
- **BREAKING**: Centralized `OutboxPostProcessor` to `Encina.Messaging.Outbox` namespace
- Validation packages now use Orchestrator → Provider architecture
- Lowered coverage threshold to 45% (current: ~47%) for CI stability

### Deprecated

- **Encina.EventStoreDB** - Marten provides better .NET integration for event sourcing
- **Encina.Wolverine** - Overlapping concerns with Encina's messaging patterns
- **Encina.NServiceBus** - Overlapping concerns with Encina's messaging patterns
- **Encina.MassTransit** - Overlapping concerns with Encina's messaging patterns
- **Encina.Dapr** - Infrastructure concerns delegated to platform

> Deprecated packages preserved in `.backup/deprecated-packages/`.

### Fixed

- Quartz logging tests now use `FakeLogger` instead of NSubstitute mocks
- SQLite PropertyTests failures with missing database tables
- ADO.Oracle and ADO.Sqlite SQL scripts syntax errors

---

## [0.8.0] - 2025-12-22

Major milestone: **Project renaming** from SimpleMediator to Encina.

### Changed

- **BREAKING**: Renamed project from **SimpleMediator** to **Encina**
- Updated all namespaces, package names, and references
- Error codes changed to lowercase (`encina.*` instead of `Encina.*`)
- License changed from proprietary to **MIT**

### Added

- GitHub Issue templates (bug_report, feature_request, technical_debt)
- VSCode tasks.json with safe build/test configurations
- Solution filters for focused development (.slnf files)

### Fixed

- Duplicate behavior registration prevention
- CLR-crashing load tests skipped (Issue #5 - .NET 10 JIT bug)
- Quality Gate coverage detection in CI
- Benchmark AOT compilation errors

### Removed

- 10 obsolete skipped tests cleaned up
- Hardcoded passwords from configuration files

---

## [0.7.0] - 2025-12-21

Major milestone: **Caching, Messaging Transports, and Event Sourcing**.

### Added

#### Caching Infrastructure (8 packages)

- **Encina.Caching** - Core abstractions (`ICacheProvider`, `ICacheKeyGenerator`, `CachingPipelineBehavior`)
- **Encina.Caching.Memory** - IMemoryCache provider (109 tests)
- **Encina.Caching.Hybrid** - Microsoft HybridCache for multi-tier caching (.NET 9+)
- **Encina.Caching.Redis** - StackExchange.Redis provider
- **Encina.Caching.Garnet** - Microsoft Garnet provider (Redis-compatible)
- **Encina.Caching.Valkey** - Valkey provider (Redis fork)
- **Encina.Caching.Dragonfly** - Dragonfly provider (Redis-compatible)
- **Encina.Caching.KeyDB** - KeyDB provider (Redis fork)

#### Messaging Transports (10 packages)

- **Encina.RabbitMQ** - RabbitMQ.Client 7.2.0 integration
- **Encina.AzureServiceBus** - Azure Service Bus 7.20.1 integration
- **Encina.AmazonSQS** - AWS SQS/SNS 4.0.2.3 integration
- **Encina.Kafka** - Confluent.Kafka 2.12.0 integration
- **Encina.Redis.PubSub** - StackExchange.Redis pub/sub
- **Encina.InMemory** - System.Threading.Channels message bus
- **Encina.NATS** - NATS.Net 2.6.11 with JetStream support
- **Encina.MQTT** - MQTTnet 5.0.1 integration
- **Encina.gRPC** - Grpc.AspNetCore 2.71.0 Encina service
- **Encina.GraphQL** - HotChocolate 15.1.11 bridge

#### Event Sourcing

- **Encina.Marten** - Marten v8.0.0-beta-1 event store with projections
- `IAggregate` / `AggregateBase` abstractions
- `IAggregateRepository<TAggregate>` pattern
- `EventPublishingPipelineBehavior` for auto-publishing domain events

#### Real-time & Integration

- **Encina.SignalR** - SignalR hub base class with `[BroadcastToSignalR]` attribute
- **Encina.MongoDB** - MongoDB provider for messaging patterns

### Changed

- LoggerMessage source generators across all packages for CA1848 compliance
- Parallel notification dispatch strategies (opt-in)

---

## [0.6.0] - 2025-12-20

Major milestone: **Comprehensive test coverage** and resilience packages.

### Added

#### Resilience Packages (3 packages)

- **Encina.Extensions.Resilience** - Microsoft standard resilience patterns
- **Encina.Polly** - Retry, circuit breaker, timeout policies
- **Encina.Refit** - Type-safe REST API clients integration

#### Test Infrastructure

- Comprehensive validation test coverage: FluentValidation (68 tests), DataAnnotations (95%), MiniValidator (95%), GuardClauses (95%)
- Job Scheduling tests: Guard, Contract, Property, Integration, Load tests
- AspNetCore comprehensive test suite (104 tests)
- EntityFrameworkCore test suite (100% coverage)
- Provider comparison benchmarks (ADO.NET vs Dapper vs EF Core)

---

## [0.5.0] - 2025-12-19

Major milestone: **OpenTelemetry, Stream Requests, and database provider tests**.

### Added

#### Observability

- **Encina.OpenTelemetry** - Distributed tracing and metrics
- `EncinaOpenTelemetryOptions` with ServiceName, ServiceVersion
- `MessagingActivityEnricher` for Outbox, Inbox, Sagas, Scheduling
- Docker Compose observability stack (Jaeger, Prometheus, Loki, Grafana)

#### Stream Requests

- `IStreamRequest<TItem>` interface for async enumerable patterns
- `IStreamRequestHandler<TRequest, TItem>` interface
- `IStreamPipelineBehavior<TRequest, TItem>` interface
- `StreamPipelineBuilder<TRequest, TItem>` for pipeline construction
- 98 tests covering Guard, Contract, Property, Integration, Load scenarios

#### Benchmarks

- Messaging pattern benchmarks (Outbox, Inbox, Infrastructure)
- Validation benchmarks comparing 4 approaches
- Stream Request benchmarks (8 scenarios)

### Changed

- Comprehensive database provider test suite (1,763 tests)

---

## [0.4.0] - 2025-12-18

Major milestone: **Multi-database support** with 10 provider packages.

### Added

#### Dapper Providers (5 packages)

- **Encina.Dapper.SqlServer** - SQL Server optimized
- **Encina.Dapper.PostgreSQL** - PostgreSQL with Npgsql
- **Encina.Dapper.MySQL** - MySQL/MariaDB with MySqlConnector
- **Encina.Dapper.Sqlite** - SQLite for testing
- **Encina.Dapper.Oracle** - Oracle with ManagedDataAccess

#### ADO.NET Providers (5 packages)

- **Encina.ADO.SqlServer** - Raw ADO.NET (fastest)
- **Encina.ADO.PostgreSQL** - PostgreSQL optimized
- **Encina.ADO.MySQL** - MySQL/MariaDB optimized
- **Encina.ADO.Sqlite** - SQLite optimized
- **Encina.ADO.Oracle** - Oracle optimized

### Changed

- Each provider implements dialect-specific SQL (TOP vs LIMIT, GETUTCDATE vs NOW, etc.)
- All providers share interfaces from `Encina.Messaging`

---

## [0.3.0] - 2025-12-17

Major milestone: **Web integration and messaging patterns**.

### Added

- **Encina.AspNetCore** - Middleware, authorization, Problem Details integration
- **Encina.Messaging** - Shared abstractions for Outbox, Inbox, Sagas, Scheduling
- **Encina.EntityFrameworkCore** - EF Core implementation of messaging patterns
- **Encina.Dapper** - Initial Dapper provider with messaging patterns
- **Encina.ADO** - Initial ADO.NET provider with messaging patterns
- **Encina.Hangfire** - Background job scheduling with Hangfire
- **Encina.Quartz** - Enterprise CRON scheduling with Quartz.NET

---

## [0.2.0] - 2025-12-14

Major milestone: **Validation satellite packages**.

### Added

- **Encina.FluentValidation** - FluentValidation integration with ROP
- **Encina.DataAnnotations** - Built-in .NET validation attributes
- **Encina.MiniValidator** - Ultra-lightweight validation (~20KB)
- **Encina.GuardClauses** - Defensive programming with Ardalis.GuardClauses
- `IRequestContext` interface for pipeline extensibility
- `RequestContext` with CorrelationId, UserId, TenantId, Metadata
- `IRequestContextAccessor` with AsyncLocal storage

---

## [0.1.0] - 2025-12-06

Initial release: **Core toolkit with Railway Oriented Programming**.

### Added

- Pure Railway Oriented Programming with `Either<EncinaError, T>`
- Request/Notification dispatch with Expression tree compilation
- Pipeline pattern (Behaviors, PreProcessors, PostProcessors)
- CQRS markers (`ICommand`, `IQuery`)
- Observability with `ActivitySource` and Metrics
- PublicAPI Analyzers compliance
- Comprehensive unit tests with property-based testing (FsCheck)
- NBomber load harness for performance validation
- BenchmarkDotNet micro-benchmarks
- Stryker mutation testing (79.75% mutation score)

---

[Unreleased]: https://github.com/dlrivada/Encina/compare/main...HEAD
