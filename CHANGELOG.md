## [Unreleased]

### Table of Contents

- [Dogfooding Initiative](#dogfooding-initiative-issues-498-502)
- [Solution Filters Reorganization](#solution-filters-reorganization)
- [Encina.Testing.Pact](#encinatestingpact-new-package-issue-436)
- [Encina.Testing.FsCheck](#encinatestingfscheck-new-package-issue-435)
- [Encina.Testing.TUnit](#encinatestingtunit-new-package-issue-171)

- [Language Requirements](#language-requirements)
- [Added](#added)
  - [AI/LLM Patterns Issues](#aillm-patterns-issues-12-new-features-planned-based-on-december-29-2025-research)
  - [Hexagonal Architecture Patterns Issues](#hexagonal-architecture-patterns-issues-10-new-features-planned-based-on-december-29-2025-research)
  - [TDD Patterns Issues](#tdd-patterns-issues-12-new-features-planned-based-on-december-29-2025-research)
  - [Developer Tooling & DX Issues](#developer-tooling--dx-issues-11-new-features-planned-based-on-december-29-2025-research)
  - [.NET Aspire Integration Patterns Issues](#net-aspire-integration-patterns-issues-10-new-features-planned-based-on-december-29-2025-research)
  - [Cloud-Native Patterns Issues](#cloud-native-patterns-issues-11-new-features-planned-based-on-december-29-2025-research)
  - [Microservices Architecture Patterns Issues](#microservices-architecture-patterns-issues-12-new-features-planned-based-on-december-29-2025-research)
  - [Security Patterns Issues](#security-patterns-issues-8-new-features-planned-based-on-december-29-2025-research)
  - [Advanced Validation Patterns Issues](#advanced-validation-patterns-issues-10-new-features-planned-based-on-december-2025-research)
  - [Advanced Event Sourcing Patterns Issues](#advanced-event-sourcing-patterns-issues-13-new-features-planned-based-on-december-2025-research)
  - [Advanced CQRS Patterns Issues](#advanced-cqrs-patterns-issues-12-new-features-planned-based-on-december-2025-market-research)
  - [Domain Modeling Building Blocks Issues](#domain-modeling-building-blocks-issues-15-new-features-planned-based-on-december-29-2025-ddd-research)
  - [Vertical Slice Architecture Patterns Issues](#vertical-slice-architecture-patterns-issues-12-new-features-planned-based-on-december-29-2025-research)
  - [Modular Monolith Architecture Patterns Issues](#modular-monolith-architecture-patterns-issues-10-new-features-planned-based-on-december-29-2025-research)
  - [Advanced Messaging Patterns Issues](#advanced-messaging-patterns-issues-15-new-features-planned-based-on-market-research)
  - [Database Providers Patterns Issues](#database-providers-patterns-issues-16-new-features-planned-based-on-december-2025-research)
  - [Advanced DDD & Workflow Patterns Issues](#advanced-ddd--workflow-patterns-issues-13-new-features-planned-based-on-december-29-2025-research)
  - [Advanced EDA Patterns Issues](#advanced-eda-patterns-issues-12-new-features-planned-based-on-december-29-2025-research)
  - [Advanced Caching Patterns Issues](#advanced-caching-patterns-issues-13-new-features-planned-based-on-december-2025-research)
  - [Advanced Resilience Patterns Issues](#advanced-resilience-patterns-issues-9-new-features-planned-based-on-2025-research)
  - [Advanced Scheduling Patterns Issues](#advanced-scheduling-patterns-issues-15-new-features-planned-based-on-2025-research)
  - [Advanced Observability Patterns Issues](#advanced-observability-patterns-issues-15-new-features-planned-based-on-2025-research)
  - [Web/API Integration Patterns Issues](#webapi-integration-patterns-issues-18-new-features-planned-based-on-december-2025-research)
  - [Advanced Testing Patterns Issues](#advanced-testing-patterns-issues-13-new-features-planned-based-on-2025-research)
  - [Advanced Distributed Lock Patterns Issues](#advanced-distributed-lock-patterns-issues-20-new-features-planned-based-on-december-2025-research)
  - [Message Transport Patterns Issues](#message-transport-patterns-issues-29-new-features-planned-based-on-december-2025-research)
  - [Clean Architecture Patterns Issues](#clean-architecture-patterns-issues-2-new-features-planned-based-on-december-29-2025-research)
- [Changed](#changed)

---

### Language Requirements

> **Encina requires C# 14 / .NET 10 or later.** All packages in this framework use modern C# features including target-typed `new()`, `with` expressions (requires `record` types), and other .NET 10 enhancements.

---

### Added

#### Dogfooding Initiative (Issues #498-502)

Epic initiative to refactor all Encina tests to use `Encina.Testing.*` infrastructure (dogfooding).

**Phases Completed**:

- **Phase 1** (#499): Core package tests - **CLOSED**
- **Phase 2** (#500): DomainModeling package tests - **CLOSED**
- **Phase 3** (#501): Messaging package tests - **CLOSED**
- **Phase 4** (#502): Database provider tests (ADO, Dapper, EF Core) - **IN PROGRESS**

**Test Infrastructure Improvements**:

- `BogusArbitrary<T>` class bridging Bogus Faker with FsCheck generators
- `MessageDataGenerators` with pre-built generators for messaging entities
- `TimeProvider` injection to Dapper stores for deterministic time control
- Fixed SQLite datetime format incompatibility (ISO 8601 vs `datetime('now')`)

**Test Fixes Applied**:

- ADO/Dapper GuardTests: Fixed parameter name "next" → "nextStep"
- EF Core ContractTests: Fixed invalid regex `ShouldMatch("*Type*")` → `ShouldContain("Type")`
- EF Core IntegrationTests: Added `HasConversion<string>()` for SagaStatus enum
- EF Core HealthCheck: Added `DefaultTags` with ["encina", "database", "efcore", "ready"]

**Test Results**:

- ADO.Sqlite: 209 tests ✅
- EF Core: 219 tests ✅

#### Solution Filters Reorganization

Updated all solution filters (`.slnf` files) to include complete test project coverage.

**Updated Filters** (11 files):

- `Encina.Core.slnf` - Added DomainModeling and all its tests
- `Encina.Messaging.slnf` - Added TestInfrastructure
- `Encina.EventSourcing.slnf` - Added TestInfrastructure, Marten.IntegrationTests
- `Encina.Observability.slnf` - Added Messaging, TestInfrastructure
- `Encina.Validation.slnf` - Added GuardClauses.Tests
- `Encina.Web.slnf` - Added SignalR, gRPC and tests
- `Encina.Scheduling.slnf` - Reorganized TestInfrastructure
- `Encina.Testing.slnf` - Added FsCheck, Verify, Testcontainers, Architecture, Aspire (32 projects)
- `Encina.Database.slnf` - Full expansion with all 85 database provider test projects
- `Encina.Caching.slnf` - Added TestInfrastructure, Redis.Tests
- `Encina.Resilience.slnf` - Added TestInfrastructure

**New Filters Created** (5 files):

- `Encina.Transports.slnf` - RabbitMQ, Kafka, AzureServiceBus, AmazonSQS, NATS, MQTT
- `Encina.Serverless.slnf` - AwsLambda, AzureFunctions
- `Encina.DistributedLock.slnf` - DistributedLock, Redis, SqlServer
- `Encina.Cli.slnf` - CLI tool
- `Encina.Workflows.slnf` - Workflows

#### Encina.Testing.Pact (New Package, Issue #436)

PactNet integration for Consumer-Driven Contract Testing (CDC) with Encina framework.

- **`EncinaPactConsumerBuilder`** - Fluent builder for defining consumer-side Pact expectations:
  - `WithCommandExpectation<TCommand, TResponse>()` - Define command request/response contracts
  - `WithQueryExpectation<TQuery, TResponse>()` - Define query request/response contracts
  - `WithNotificationExpectation<TNotification>()` - Define notification contracts
  - `WithCommandFailureExpectation<TCommand, TResponse>()` - Define expected error responses for commands
  - `WithQueryFailureExpectation<TQuery, TResponse>()` - Define expected error responses for queries
  - `BuildAsync()` - Build the Pact and write to configured directory
  - `GetMockServerUri()` - Get mock server URI for testing

- **`EncinaPactProviderVerifier`** - Verifies Pact contracts against provider implementation:
  - `WithProviderName()` - Set the provider name for verification
  - `WithProviderState(stateName, Action)` - Register synchronous provider state setup
  - `WithProviderState(stateName, Func<Task>)` - Register async provider state setup
  - `WithProviderState(stateName, Func<IDictionary<string,object>, Task>)` - State setup with parameters
  - `VerifyAsync(pactFilePath)` - Verify a local Pact JSON file
  - `VerifyFromBrokerAsync(brokerUrl, providerName)` - Verify from Pact Broker

- **`EncinaPactFixture`** - xUnit test fixture for simplified test setup:
  - Implements `IAsyncLifetime` and `IDisposable` for lifecycle management
  - `CreateConsumer(consumerName, providerName)` - Create a consumer builder
  - `CreateVerifier(providerName)` - Create a provider verifier
  - `VerifyAsync(consumer, Action<Uri>)` - Verify with sync test action
  - `VerifyAsync(consumer, Func<Uri, Task>)` - Verify with async test action
  - `VerifyProviderAsync(providerName)` - Verify all Pact files for a provider
  - `WithEncina(encina, serviceProvider)` - Configure with Encina instance
  - `WithServices(configureServices)` - Configure with DI services

- **`PactExtensions`** - Extension methods for working with Pact:
  - `CreatePactHttpClient(Uri)` - Create HTTP client for mock server
  - `SendCommandAsync<TCommand, TResponse>()` - Send command to mock server
  - `SendQueryAsync<TQuery, TResponse>()` - Send query to mock server
  - `PublishNotificationAsync<TNotification>()` - Publish notification to mock server
  - `ReadAsEitherAsync<TResponse>()` - Deserialize response as Either result
  - `ToPactResponse<TResponse>()` - Convert Either to Pact-compatible response

- **Response Types**:
  - `PactSuccessResponse<T>` - Success response wrapper with `IsSuccess` and `Data`
  - `PactErrorResponse` - Error response with `IsSuccess`, `ErrorCode`, `ErrorMessage`
  - `PactVerificationResult` - Verification result with `Success`, `Errors`, `InteractionResults`
  - `InteractionVerificationResult` - Individual interaction result with `Description`, `Success`, `ErrorMessage`

- **Error Code Mapping** - Automatic HTTP status code mapping from Encina error codes:
  - `encina.validation.*` → 400 Bad Request
  - `encina.authorization.*` → 403 Forbidden
  - `encina.authentication.*` → 401 Unauthorized
  - `encina.notfound.*` → 404 Not Found
  - `encina.conflict.*` → 409 Conflict
  - `encina.timeout.*` → 408 Request Timeout
  - `encina.ratelimit.*` → 429 Too Many Requests
  - Other errors → 500 Internal Server Error

- **Tests**: 118 unit tests covering all public APIs
  - EncinaPactConsumerBuilderTests (17 tests)
  - EncinaPactProviderVerifierTests (24 tests)
  - EncinaPactFixtureTests (23 tests)
  - PactExtensionsTests (14 tests)
  - GuardClauseTests (40 tests)

#### Encina.Testing.FsCheck (New Package, Issue #435)

FsCheck property-based testing extensions for Encina framework, compatible with FsCheck 3.x.

- **`EncinaArbitraries`** - Pre-built arbitraries for generating Encina types:
  - Core types: `EncinaError()`, `EncinaErrorWithException()`, `RequestContext()`
  - Either types: `EitherOf<T>()`, `SuccessEither<T>()`, `FailureEither<T>()`
  - Messaging types: `OutboxMessage()`, `PendingOutboxMessage()`, `FailedOutboxMessage()`
  - `InboxMessage()`, `SagaState()`, `ScheduledMessage()`, `RecurringScheduledMessage()`

- **`EncinaProperties`** - Common property validators for invariants:
  - Either properties: `EitherIsExclusive()`, `MapPreservesRightState()`, `BindToFailureProducesLeft()`
  - Error properties: `ErrorHasNonEmptyMessage()`, `ErrorFromStringPreservesMessage()`
  - Context properties: `ContextHasCorrelationId()`, `WithMetadataIsImmutable()`, `WithUserIdCreatesNewContext()`
  - Outbox: `OutboxProcessedStateIsConsistent()`, `OutboxDeadLetterIsConsistent()`, `OutboxHasRequiredFields()`
  - Inbox: `InboxProcessedStateIsConsistent()`, `InboxHasRequiredFields()`
  - Saga: `SagaStatusIsValid()`, `SagaHasRequiredFields()`, `SagaCurrentStepIsNonNegative()`
  - Scheduled: `RecurringHasCronExpression()`, `ScheduledHasRequiredFields()`
  - Handler: `HandlerIsDeterministic()`, `AsyncHandlerIsDeterministic()`

- **`GenExtensions`** - Generator extension methods:
  - Either generators: `ToEither()`, `ToSuccess()`, `ToFailure<T>()`
  - Nullable generators: `OrNull()`, `OrNullValue()`
  - String generators: `NonEmptyString()`, `AlphaNumericString()`, `EmailAddress()`
  - Data generators: `JsonObject()`, `UtcDateTime()`, `PastUtcDateTime()`, `FutureUtcDateTime()`
  - Other: `CronExpression()`, `PositiveDecimal()`, `ListOf()`, `NonEmptyListOf()`

- **xUnit Integration**:
  - `PropertyTestBase` - Base class with auto-registered arbitraries
  - `EncinaArbitraryProvider` - Arbitrary provider for FsCheck type registration
  - Custom attributes: `[EncinaProperty]`, `[QuickProperty]`, `[ThoroughProperty]`
  - `PropertyTestConfig` - Configuration constants for test runs

- **Concrete Message Types** for testing:
  - `ArbitraryOutboxMessage` - Implements `IOutboxMessage`
  - `ArbitraryInboxMessage` - Implements `IInboxMessage`
  - `ArbitrarySagaState` - Implements `ISagaState`
  - `ArbitraryScheduledMessage` - Implements `IScheduledMessage`

- **Tests**: 75 unit tests covering all public APIs
  - EncinaArbitrariesTests (22 tests)
  - EncinaPropertiesTests (22 tests)
  - GenExtensionsTests (18 tests)
  - PropertyTestBaseTests (13 tests)

#### Encina.Testing.TUnit (New Package, Issue #171)

TUnit framework support for modern, source-generated testing with NativeAOT compatibility.

- **`EncinaTUnitFixture`** - TUnit-compatible test fixture with fluent builder pattern:
  - Implements TUnit's `IAsyncInitializer` and `IAsyncDisposable` for lifecycle management
  - `WithConfiguration(Action<EncinaConfiguration>)` - Custom Encina configuration
  - `WithServices(Action<IServiceCollection>)` - Register custom services for DI
  - `WithHandlersFromAssemblyContaining<T>()` - Scan assembly for handlers
  - `Encina` property - Get the configured Encina instance
  - `CreateScope()` - Create service scope for scoped services
  - `GetService<T>()`, `GetRequiredService<T>()` - Resolve services
  - Fluent builder pattern with chaining support
  - Proper `GC.SuppressFinalize` in `DisposeAsync()` per CA1816

- **`TUnitEitherAssertions`** - Async-first assertions for `Either<TLeft, TRight>`:
  - `ShouldBeSuccessAsync()` - Assert Right and return value
  - `ShouldBeSuccessAsync(expected)` - Assert Right with expected value
  - `ShouldBeSuccessAsync(Func<TRight, Task>)` - Assert with async validator
  - `ShouldBeErrorAsync()` - Assert Left and return error
  - `ShouldBeErrorAsync(Func<TLeft, Task>)` - Assert with async validator
  - `AndReturnAsync()` - Alias for `ShouldBeSuccessAsync()` for fluent chaining

- **EncinaError-Specific Assertions**:
  - `ShouldBeErrorWithCodeAsync(code)` - Assert error with specific code
  - `ShouldBeErrorContainingAsync(text)` - Assert error message contains text
  - `ShouldBeValidationErrorAsync()` - Assert code starts with "encina.validation"
  - `ShouldBeAuthorizationErrorAsync()` - Assert code starts with "encina.authorization"
  - `ShouldBeNotFoundErrorAsync()` - Assert code starts with "encina.notfound"
  - `ShouldBeConflictErrorAsync()` - Assert code starts with "encina.conflict"
  - `ShouldBeInternalErrorAsync()` - Assert code starts with "encina.internal"

- **Task Extension Methods** - All assertions work with `Task<Either<>>`:
  - `task.ShouldBeSuccessAsync()`, `task.ShouldBeErrorAsync()`
  - `task.ShouldBeValidationErrorAsync()`, `task.AndReturnAsync()`, etc.

- **`TUnitEitherCollectionAssertions`** - Collection assertions for `IEnumerable<Either<>>`:
  - `ShouldAllBeSuccessAsync()` - Assert all items are Right, return values
  - `ShouldAllBeErrorAsync()` - Assert all items are Left, return errors
  - `ShouldContainSuccessAsync()` - Assert at least one Right
  - `ShouldContainErrorAsync()` - Assert at least one Left
  - `ShouldHaveSuccessCountAsync(count)` - Assert exact success count
  - `ShouldHaveErrorCountAsync(count)` - Assert exact error count
  - `GetSuccesses()`, `GetErrors()` - Extract values/errors from collection

- **NativeAOT Compatibility**:
  - Package marked with `<IsAotCompatible>true</IsAotCompatible>`
  - No reflection-based patterns in package code
  - Compatible with TUnit's source-generated test discovery

- **Tests**: 56 unit tests covering all public APIs
  - EncinaTUnitFixtureTests (15 tests)
  - TUnitEitherAssertionsTests (21 tests)
  - TUnitEitherCollectionAssertionsTests (20 tests)

- **CI/CD Workflow Templates** (Issue #173) - Reusable GitHub Actions workflow templates for testing .NET 10 applications:
  - `encina-test.yml` - Basic test workflow with unit tests and coverage:
    - Cross-platform support (Windows, Linux, macOS)
    - Configurable coverage threshold enforcement
    - NuGet package caching for faster builds
    - Test filter expressions for selective testing
    - Integration test opt-in
  - `encina-matrix.yml` - Matrix testing across OS and database providers:
    - Multiple OS testing (Windows, Linux, macOS)
    - Database service containers (PostgreSQL, SQL Server, MySQL, Redis, MongoDB, SQLite)
    - Parallel execution with configurable max-parallel
    - Automatic connection string configuration
    - Summary report across all matrix combinations
  - `encina-full-ci.yml` - Complete CI pipeline with all stages:
    - Build & analyze (formatting, warnings-as-errors)
    - Unit tests with coverage threshold
    - Integration tests with Docker services (optional)
    - Architecture tests (optional)
    - Mutation tests with Stryker (optional)
    - NuGet package creation and publishing
    - CI summary report
  - Documentation: `docs/ci-cd-templates.md` with usage examples and best practices
  - Tests: `Encina.Workflows.Tests` project with 49 YAML validation tests

- **Encina.Testing Package** - Enhanced testing fixtures for fluent test setup (Issue #444):
  - `EncinaTestFixture` - Fluent builder pattern for test setup:
    - `WithMockedOutbox()`, `WithMockedInbox()`, `WithMockedSaga()` - Configure fake stores
    - `WithMockedScheduling()`, `WithMockedDeadLetter()` - Additional store mocking
    - `WithAllMockedStores()` - Enable all mocked stores at once
    - `WithHandler<THandler>()` - Register request/notification handlers
    - `WithService<TService>(instance)` - Register custom service instances
    - `WithService<TService, TImplementation>()` - Register service with implementation type
    - `WithConfiguration(Action<EncinaConfiguration>)` - Custom Encina configuration
    - `SendAsync<TResponse>(IRequest<TResponse>)` - Send request and get test context
    - `PublishAsync(INotification)` - Publish notification and get test context
    - Properties: `Outbox`, `Inbox`, `SagaStore`, `ScheduledMessageStore`, `DeadLetterStore`
    - `ClearStores()` - Reset all stores for test isolation
    - `IAsyncLifetime` support for xUnit integration
  - `EncinaTestContext<TResponse>` - Chainable assertion context:
    - `ShouldSucceed()`, `ShouldFail()` - Assert result state
    - `ShouldSucceedWith(Action<TResponse>)` - Assert and verify success value
    - `ShouldFailWith(Action<EncinaError>)` - Assert and verify error
    - `ShouldSatisfy(Action<TResponse>)` - Custom verification
    - `OutboxShouldContain<TNotification>()` - Verify outbox messages
    - `OutboxShouldBeEmpty()`, `OutboxShouldContainExactly(count)` - Outbox assertions
    - `SagaShouldBeStarted<TSaga>()` - Verify saga lifecycle
    - `SagaShouldHaveTimedOut<TSaga>()`, `SagaShouldHaveCompleted<TSaga>()` - Saga state assertions
    - `SagaShouldBeCompensating<TSaga>()`, `SagaShouldHaveFailed<TSaga>()` - Saga failure assertions
    - `GetSuccessValue()`, `GetErrorValue()` - Extract values
    - `And` property for fluent chaining
    - Implicit conversion to `Either<EncinaError, TResponse>`
  - **Time-Travel Testing** (Phase 3):
    - `WithFakeTimeProvider()`, `WithFakeTimeProvider(DateTimeOffset)` - Configure fake time
    - `AdvanceTimeBy(TimeSpan)`, `AdvanceTimeByMinutes(int)`, `AdvanceTimeByHours(int)` - Advance time
    - `AdvanceTimeByDays(int)`, `SetTimeTo(DateTimeOffset)`, `GetCurrentTime()` - Time control
    - `ThenAdvanceTimeBy()`, `ThenAdvanceTimeByHours()` - Context chaining for time-travel
    - `TimeProvider` property for direct FakeTimeProvider access
  - **Messaging Pattern Test Helpers** (Issue #169) - BDD Given/When/Then helpers for messaging patterns:
    - `OutboxTestHelper` - Fluent test helper for outbox pattern testing:
      - `GivenEmptyOutbox()`, `GivenMessages()`, `GivenPendingMessage()`, `GivenProcessedMessage()`, `GivenFailedMessage()` - Setup methods
      - `WhenMessageAdded()`, `WhenMessageProcessed()`, `WhenMessageFailed()`, `When()`, `WhenAsync()` - Action methods
      - `ThenMessageWasAdded<T>()`, `ThenOutboxContains<T>()`, `ThenMessageWasProcessed()`, `ThenNoMessagesWereAdded()`, `ThenNoException()`, `ThenThrows<T>()` - Assertion methods
      - Time-travel: `AdvanceTimeBy()`, `AdvanceTimeByMinutes()`, `AdvanceTimeByHours()`, `AdvanceTimeByDays()`, `GetCurrentTime()`
    - `InboxTestHelper` - Fluent test helper for inbox/idempotency testing:
      - `GivenEmptyInbox()`, `GivenNewMessage()`, `GivenProcessedMessage()`, `GivenFailedMessage()` - Setup methods
      - `WhenMessageReceived()`, `WhenMessageRegistered()`, `WhenMessageProcessed()`, `WhenMessageFailed()`, `When()`, `WhenAsync()` - Action methods
      - `ThenMessageWasRegistered()`, `ThenMessageIsProcessed()`, `ThenCachedResponseIs<T>()`, `ThenMessageWasDuplicate()`, `ThenNoException()`, `ThenThrows<T>()` - Assertion methods
    - `SagaTestHelper` - Fluent test helper for saga orchestration testing:
      - `GivenNoSagas()`, `GivenNewSaga<TSaga, TData>()`, `GivenRunningSaga<TSaga, TData>()`, `GivenCompletedSaga()`, `GivenFailedSaga()`, `GivenTimedOutSaga()` - Setup methods
      - `WhenSagaStarts<TSaga, TData>()`, `WhenSagaAdvancesToNextStep()`, `WhenSagaDataUpdated<TData>()`, `WhenSagaCompletes()`, `WhenSagaStartsCompensating()`, `WhenSagaFails()`, `WhenSagaTimesOut()` - Action methods
      - `ThenSagaWasStarted<TSaga>()`, `ThenSagaIsAtStep()`, `ThenSagaIsCompleted()`, `ThenSagaIsFailed()`, `ThenSagaIsCompensating()`, `ThenSagaData<TData>()`, `ThenNoException()`, `ThenThrows<T>()` - Assertion methods
    - `SchedulingTestHelper` - Fluent test helper for scheduled message testing:
      - `GivenNoScheduledMessages()`, `GivenScheduledMessage()`, `GivenRecurringMessage()`, `GivenDueMessage()`, `GivenProcessedMessage()`, `GivenFailedMessage()`, `GivenCancelledMessage()` - Setup methods
      - `WhenMessageScheduled()`, `WhenRecurringMessageScheduled()`, `WhenMessageProcessed()`, `WhenMessageFailed()`, `WhenMessageCancelled()`, `WhenMessageRescheduled()` - Action methods
      - `ThenMessageWasScheduled<T>()`, `ThenMessageIsDue<T>()`, `ThenMessageIsNotDue()`, `ThenMessageWasProcessed()`, `ThenMessageWasCancelled()`, `ThenMessageIsRecurring()`, `ThenMessageHasCron()`, `ThenScheduledMessageCount()` - Assertion methods
      - Time-travel: `AdvanceTimeBy()`, `AdvanceTimeByMinutes()`, `AdvanceTimeByHours()`, `AdvanceTimeByDays()`, `AdvanceTimeUntilDue()`, `GetCurrentTime()`, `GetDueMessagesAsync()`
  - **(NEW Issue #170)** Improved Assertions with Shouldly-like fluent chaining (xUnit-based):
    - `AndConstraint<T>` - Fluent chaining pattern for assertions:
      - `Value` property for accessing the asserted value
      - `And` property for continuing assertion chains
      - `ShouldSatisfy(Action<T>)` for custom assertions
      - Implicit conversion to underlying value type
    - `EitherAssertions` enhancements:
      - `ShouldBeSuccessAnd()`, `ShouldBeRightAnd()` returning `AndConstraint<TRight>`
      - `ShouldBeErrorAnd()`, `ShouldBeLeftAnd()` returning `AndConstraint<TLeft>`
      - `ShouldBeValidationErrorForProperty()`, `ShouldBeValidationErrorForPropertyAnd()` for property-specific validation
      - EncinaError `*And` variants: `ShouldBeErrorWithCodeAnd()`, `ShouldBeValidationErrorAnd()`, `ShouldBeAuthorizationErrorAnd()`, `ShouldBeNotFoundErrorAnd()`, `ShouldBeErrorContainingAnd()`
      - Async `*And` variants: `ShouldBeSuccessAndAsync()`, `ShouldBeErrorAndAsync()`, `ShouldBeValidationErrorAndAsync()`, etc.
    - `EitherCollectionAssertions` - Collection assertions for `IEnumerable<Either<TLeft, TRight>>`:
      - `ShouldAllBeSuccess()`, `ShouldAllBeSuccessAnd()` for all-success verification
      - `ShouldAllBeError()`, `ShouldAllBeErrorAnd()` for all-error verification
      - `ShouldContainSuccess()`, `ShouldContainSuccessAnd()`, `ShouldContainError()`, `ShouldContainErrorAnd()`
      - `ShouldHaveSuccessCount()`, `ShouldHaveSuccessCountAnd()`, `ShouldHaveErrorCount()`, `ShouldHaveErrorCountAnd()`
      - EncinaError-specific: `ShouldContainValidationErrorFor()`, `ShouldNotContainAuthorizationErrors()`, `ShouldContainAuthorizationError()`, `ShouldAllHaveErrorCode()`
      - Async variants: `ShouldAllBeSuccessAsync()`, `ShouldAllBeErrorAsync()`, `ShouldContainSuccessAsync()`, `ShouldContainErrorAsync()`
      - Helper methods: `GetSuccesses()`, `GetErrors()`
    - `StreamingAssertions` - `IAsyncEnumerable<Either<TLeft, TRight>>` assertions:
      - `ShouldAllBeSuccessAsync()`, `ShouldAllBeSuccessAndAsync()`, `ShouldAllBeErrorAsync()`, `ShouldAllBeErrorAndAsync()`
      - `ShouldContainSuccessAsync()`, `ShouldContainSuccessAndAsync()`, `ShouldContainErrorAsync()`, `ShouldContainErrorAndAsync()`
      - `ShouldHaveCountAsync()`, `ShouldHaveSuccessCountAsync()`, `ShouldHaveErrorCountAsync()`
      - `FirstShouldBeSuccessAsync()`, `FirstShouldBeErrorAsync()` for first-item assertions
      - `ShouldBeEmptyAsync()`, `ShouldNotBeEmptyAsync()` for stream emptiness
      - EncinaError-specific: `ShouldContainValidationErrorForAsync()`, `ShouldNotContainAuthorizationErrorsAsync()`, `ShouldContainAuthorizationErrorAsync()`, `ShouldAllHaveErrorCodeAsync()`
      - Helper: `CollectAsync()` to materialize async streams
  - **(NEW Issue #434)** BDD Handler and Saga Specification Testing:
    - `HandlerSpecification<TRequest, TResponse>` - Abstract base class for handler BDD testing:
      - `Given(Action<TRequest>)` - Setup request modifications
      - `GivenRequest(TRequest)` - Setup explicit request
      - `When(Action<TRequest>)`, `WhenAsync(Action<TRequest>, CancellationToken)` - Execute handler
      - `ThenSuccess(Action<TResponse>?)` - Assert success and validate
      - `ThenSuccessAnd()` - Assert success returning `AndConstraint<TResponse>`
      - `ThenError(Action<EncinaError>?)` - Assert error and validate
      - `ThenErrorAnd()` - Assert error returning `AndConstraint<EncinaError>`
      - `ThenValidationError(params string[])` - Assert validation error for properties
      - `ThenValidationErrorAnd(params string[])` - Returns `AndConstraint<EncinaError>`
      - `ThenErrorWithCode(string)` - Assert specific error code
      - `ThenErrorWithCodeAnd(string)` - Returns `AndConstraint<EncinaError>`
      - `ThenThrows<TException>()` - Assert exception thrown
      - `ThenThrowsAnd<TException>()` - Returns `AndConstraint<TException>`
    - `Scenario<TRequest, TResponse>` - Fluent inline scenario builder:
      - `Describe(string)` - Create named scenario
      - `Given(Action<TRequest>)` - Setup request modifications
      - `UsingHandler(Func<IRequestHandler<TRequest, TResponse>>)` - Set handler factory
      - `WhenAsync(TRequest, CancellationToken)` - Execute and return `ScenarioResult<TResponse>`
    - `ScenarioResult<TResponse>` - Result wrapper with assertions:
      - `IsSuccess`, `HasException`, `Result`, `Exception` properties
      - `ShouldBeSuccess(Action<TResponse>?)` - Assert success
      - `ShouldBeSuccessAnd()` - Returns `AndConstraint<TResponse>`
      - `ShouldBeError(Action<EncinaError>?)` - Assert error
      - `ShouldBeErrorAnd()` - Returns `AndConstraint<EncinaError>`
      - `ShouldBeValidationError(params string[])` - Assert validation error
      - `ShouldBeErrorWithCode(string)` - Assert specific error code
      - `ShouldThrow<TException>()` - Assert exception
      - `ShouldThrowAnd<TException>()` - Returns `AndConstraint<TException>`
      - Implicit conversion to `Either<EncinaError, TResponse>`
    - `SagaSpecification<TSaga, TSagaData>` - Abstract base class for saga BDD testing:
      - `GivenData(Action<TSagaData>)` - Setup saga data modifications
      - `GivenSagaData(TSagaData)` - Setup explicit saga data
      - `WhenComplete(CancellationToken)` - Execute saga from step 0
      - `WhenStep(int, CancellationToken)` - Execute saga from specific step
      - `WhenCompensate(int, CancellationToken)` - Execute compensation
      - `ThenSuccess(Action<TSagaData>?)` - Assert saga success
      - `ThenSuccessAnd()` - Returns `AndConstraint<TSagaData>`
      - `ThenError(Action<EncinaError>?)` - Assert saga error
      - `ThenErrorAnd()` - Returns `AndConstraint<EncinaError>`
      - `ThenErrorWithCode(string)` - Assert specific error code
      - `ThenThrows<TException>()` - Assert exception
      - `ThenThrowsAnd<TException>()` - Returns `AndConstraint<TException>`
      - `ThenCompleted()` - Assert saga completed successfully
      - `ThenCompensated()` - Assert compensation executed
      - `ThenFailed(string?)` - Assert saga failed with optional message
      - `ThenData(Action<TSagaData>)` - Validate saga data state
  - **(NEW Issue #362)** Module Testing Utilities for modular monolith testing:
    - `ModuleTestFixture<TModule>` - Fluent test fixture for isolated module testing:
      - `WithMockedModule<TModuleApi>(Action<MockModuleApi<TModuleApi>>)` - Mock dependent module with fluent setup
      - `WithMockedModule<TModuleApi>(TModuleApi)` - Mock dependent module with implementation
      - `WithFakeModule<TModuleApi, TFakeModule>()` - Register fake module implementation
      - `WithFakeModule<TModuleApi, TFakeModule>(TFakeModule)` - Register fake module instance
      - `WithService<TService>(TService)`, `WithService<TService, TImplementation>()` - Service registration
      - `ConfigureServices(Action<IServiceCollection>)` - Custom service configuration
      - `WithMockedOutbox()`, `WithMockedInbox()`, `WithMockedSaga()` - Messaging store mocking
      - `WithMockedScheduling()`, `WithMockedDeadLetter()`, `WithAllMockedStores()` - Additional stores
      - `WithFakeTimeProvider()`, `WithFakeTimeProvider(DateTimeOffset)` - Time control
      - `AdvanceTimeBy(TimeSpan)` - Time advancement
      - `Configure(Action<EncinaConfiguration>)` - Encina configuration
      - `SendAsync<TResponse>(IRequest<TResponse>)` - Send request returning `ModuleTestContext`
      - `PublishAsync(INotification)` - Publish notification capturing integration events
      - Properties: `Module`, `IntegrationEvents`, `Outbox`, `Inbox`, `SagaStore`, `ScheduledMessageStore`, `DeadLetterStore`, `TimeProvider`, `ServiceProvider`
      - `ClearStores()` - Reset all stores and captured events
      - `IDisposable` and `IAsyncDisposable` support
    - `ModuleTestContext<TResponse>` - Fluent assertion context for module test results:
      - `ShouldSucceed()`, `ShouldFail()` - Basic result assertions
      - `ShouldSucceedWith(Action<TResponse>)`, `ShouldFailWith(Action<EncinaError>)` - With validation
      - `ShouldSucceedAnd()`, `ShouldFailAnd()` - Return `AndConstraint<T>` for chaining
      - `ShouldFailWithMessage(string)` - Assert error contains message
      - `ShouldBeValidationError()` - Assert validation error
      - `OutboxShouldContain<T>()`, `OutboxShouldBeEmpty()`, `OutboxShouldHaveCount(int)` - Outbox assertions
      - `IntegrationEventShouldContain<T>()`, `IntegrationEventsShouldBeEmpty()` - Integration event assertions
      - Properties: `Fixture`, `Result`, `IsSuccess`, `Value`, `Error`
    - `IntegrationEventCollector` - Thread-safe collection for integration event assertions:
      - `Add(INotification)` (internal) - Capture events
      - `Clear()` - Clear captured events
      - `GetEvents<TEvent>()`, `GetFirst<TEvent>()`, `GetFirstOrDefault<TEvent>()`, `GetSingle<TEvent>()` - Query events
      - `Contains<TEvent>()`, `Contains<TEvent>(Func<TEvent, bool>)` - Check existence
      - `ShouldContain<TEvent>()`, `ShouldContain<TEvent>(Func<TEvent, bool>)` - Fluent assertions
      - `ShouldContainAnd<TEvent>()`, `ShouldContainSingle<TEvent>()`, `ShouldContainSingleAnd<TEvent>()` - With chaining
      - `ShouldNotContain<TEvent>()`, `ShouldBeEmpty()`, `ShouldHaveCount(int)`, `ShouldHaveCount<TEvent>(int)` - Additional assertions
    - `MockModuleApi<TModuleApi>` - Simple mock builder for module APIs using DispatchProxy:
      - `Setup(string methodName, Delegate)` - Configure method implementation
      - `SetupProperty(string propertyName, object?)` - Configure property value
      - `Build()` - Create proxy instance implementing the interface
    - `ModuleArchitectureRules` - Pre-built ArchUnitNET rules for modules:
      - `ModulesShouldBeSealed()` - Module implementations should be sealed
      - `IntegrationEventsShouldBeSealed()` - Integration events should be sealed
      - `DomainShouldNotDependOnInfrastructure(string domainNs, string infraNs)` - Layer dependency rule
    - `ModuleArchitectureAnalyzer` - Module dependency analysis:
      - `Analyze(params Assembly[])` - Analyze assemblies
      - `AnalyzeAssemblyContaining<T1>()`, `AnalyzeAssemblyContaining<T1, T2>()` - Type-based analysis
      - `Result` property returning `ModuleAnalysisResult`
      - `Architecture` property for ArchUnitNET access
    - `ModuleAnalysisResult` - Analysis result with assertions:
      - `Modules`, `Dependencies`, `CircularDependencies` properties
      - `ModuleCount`, `HasCircularDependencies` properties
      - `ShouldHaveNoCircularDependencies()` - Assert no cycles
      - `ShouldContainModule(string)` - Assert module exists
      - `ShouldHaveDependency(string source, string target)` - Assert dependency exists
      - `ShouldNotHaveDependency(string source, string target)` - Assert no dependency
    - Supporting records: `ModuleInfo`, `ModuleDependency`, `CircularDependency`
    - `DependencyType` enum: `Direct`, `PublicApi`, `IntegrationEvent`
  - **(NEW Issue #172)** Mutation Testing Helper Attributes (`Encina.Testing.Mutations` namespace):
    - `NeedsMutationCoverageAttribute` - Mark tests needing stronger assertions:
      - `Reason` (required) - Description of mutation coverage gap
      - `MutantId` (optional) - Stryker mutant ID from report
      - `SourceFile` (optional) - Path to source file with surviving mutant
      - `Line` (optional) - Line number where mutation was applied
      - `[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]` - Multiple instances per test allowed
    - `MutationKillerAttribute` - Document tests that kill specific mutations:
      - `MutationType` (required) - Type of mutation killed (e.g., "EqualityMutation", "ArithmeticMutation")
      - `Description` (optional) - Detailed description of what mutation is killed
      - `SourceFile` (optional) - Path to source file containing the mutation target
      - `TargetMethod` (optional) - Method name where mutation applies
      - `Line` (optional) - Line number where mutation applies
      - `[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]` - Multiple instances per test allowed
  - **(FIX Issue #497)** ModuleArchitectureAnalyzerTests: Fix false positive dependency detection:
    - **Root Cause**: Test modules (`OrdersModule`, `PaymentsModule`, `ShippingModule`) were in the same namespace, causing the analyzer to detect false dependencies
    - **Solution**: Moved test modules to distinct namespaces (`Encina.Testing.Tests.Modules.TestModules.Orders`, etc.) to simulate real modular architecture
    - **Additional Fix**: Adjusted `Result_DiscoversModulesInAssembly` test to verify module containment rather than exact match (assembly may contain additional test modules from other files)
    - Common mutation types: `EqualityMutation`, `ArithmeticMutation`, `BooleanMutation`, `UnaryMutation`, `NullCheckMutation`, `StringMutation`, `LinqMutation`, `BlockRemoval`
    - Documentation updated in `docs/en/guides/MUTATION_TESTING.md`

- **Encina.Testing.Testcontainers Package** - Docker container fixtures for integration tests (Issues #162, #163):
  - `SqlServerContainerFixture` - SQL Server container (mcr.microsoft.com/mssql/server:2022-latest)
  - `PostgreSqlContainerFixture` - PostgreSQL container (postgres:17-alpine)
  - `MySqlContainerFixture` - MySQL container (mysql:9.1)
  - `MongoDbContainerFixture` - MongoDB container (mongo:7)
  - `RedisContainerFixture` - Redis container (redis:7-alpine)
  - `ConfiguredContainerFixture<TContainer>` - Generic fixture for custom-configured containers
  - `EncinaContainers` - Static factory class for creating container fixtures:
    - `SqlServer()`, `PostgreSql()`, `MySql()`, `MongoDb()`, `Redis()` - Default configurations
    - Overloads accepting `Action<TBuilder>` for custom container configuration
  - All fixtures implement `IAsyncLifetime` for xUnit integration
  - Properties: `Container`, `ConnectionString`, `IsRunning`
  - Automatic cleanup with `WithCleanUp(true)`
  - **Respawn Integration** (Issue #163):
    - `DatabaseIntegrationTestBase<TFixture>` - Abstract base class combining Testcontainers with Respawn
    - `SqlServerIntegrationTestBase` - Pre-configured base class for SQL Server integration tests
    - `PostgreSqlIntegrationTestBase` - Pre-configured base class for PostgreSQL integration tests
    - `MySqlIntegrationTestBase` - Pre-configured base class for MySQL integration tests
    - Automatic database reset before each test via Respawn
    - Customizable `RespawnOptions` through property override
    - `ResetDatabaseAsync()` method for mid-test cleanup
    - Default exclusion of Encina messaging tables from cleanup

- **Encina.Testing.Architecture Package** - Architecture testing rules using ArchUnitNET (Issue #432, Phase 6 of #444):
  - `EncinaArchitectureRules` - Static class with pre-built architecture rules:
    - `HandlersShouldNotDependOnInfrastructure()` - Handlers should use abstractions
    - `HandlersShouldBeSealed()` - Handler classes should be sealed
    - `NotificationsShouldBeSealed()` - Notifications and events should be sealed
    - `BehaviorsShouldBeSealed()` - Pipeline behaviors should be sealed
    - `ValidatorsShouldFollowNamingConvention()` - Validators should end with "Validator"
    - `DomainShouldNotDependOnMessaging(namespace)` - Domain layer isolation
    - `DomainShouldNotDependOnApplication(domain, app)` - Domain independence
    - `ApplicationShouldNotDependOnInfrastructure(app, infra)` - Layer separation
    - `CleanArchitectureLayersShouldBeSeparated(domain, app, infra)` - Combined layer rules
    - `RepositoryInterfacesShouldResideInDomain(namespace)` - Repository interface location
    - `RepositoryImplementationsShouldResideInInfrastructure(namespace)` - Impl location
    - **(NEW Phase 6)** `RequestsShouldFollowNamingConvention()` - Requests should end with Command/Query
    - **(NEW Phase 6)** `AggregatesShouldFollowPattern(namespace)` - Aggregates should be sealed
    - **(NEW Phase 6)** `ValueObjectsShouldBeSealed()` - Value objects should be sealed
    - **(NEW Phase 6)** `SagasShouldBeSealed()` - Sagas should be sealed
    - **(NEW Phase 6)** `StoreImplementationsShouldBeSealed()` - Store impls should be sealed
    - **(NEW Phase 6)** `EventHandlersShouldBeSealed()` - Event handlers should be sealed
    - **(NEW Issue #166)** `HandlersShouldImplementCorrectInterface()` - Handlers must implement IRequestHandler, ICommandHandler, IQueryHandler, or INotificationHandler
    - **(NEW Issue #166)** `CommandsShouldImplementICommand()` - Commands must implement ICommand<TResponse> or ICommand
    - **(NEW Issue #166)** `QueriesShouldImplementIQuery()` - Queries must implement IQuery<TResponse> or IQuery
    - **(NEW Issue #166)** `HandlersShouldNotDependOnControllers()` - Handlers must not depend on presentation layer
    - **(NEW Issue #166)** `PipelineBehaviorsShouldImplementCorrectInterface()` - Behaviors must implement IPipelineBehavior<,>
    - **(NEW Issue #166)** `SagaDataShouldBeSealed()` - Saga data classes must be sealed for serialization
  - `EncinaArchitectureTestBase` - Abstract test class with pre-defined tests:
    - Override `ApplicationAssembly`, `DomainAssembly`, `InfrastructureAssembly`
    - Override namespace properties for layer separation rules
    - Pre-built test methods: `HandlersShouldNotDependOnInfrastructure()`, etc.
    - **(NEW Issue #166)** `HandlersShouldImplementCorrectInterface()` - Test handler interfaces
    - **(NEW Issue #166)** `CommandsShouldImplementICommand()` - Test command interfaces
    - **(NEW Issue #166)** `QueriesShouldImplementIQuery()` - Test query interfaces
    - **(NEW Issue #166)** `HandlersShouldNotDependOnControllers()` - Test handler-controller separation
    - **(NEW Issue #166)** `PipelineBehaviorsShouldImplementCorrectInterface()` - Test behavior interfaces
    - **(NEW Issue #166)** `SagaDataShouldBeSealed()` - Test saga data sealing
  - `EncinaArchitectureRulesBuilder` - Fluent builder for custom rule composition:
    - Chain multiple rules with fluent API
    - `Verify()` - Throws `ArchitectureRuleException` on violations
    - `VerifyWithResult()` - Returns `ArchitectureVerificationResult` without throwing
    - `ApplyAllStandardRules()` - Apply all standard Encina rules at once (excludes saga rules)
    - **(NEW)** `ApplyAllSagaRules()` - Apply saga-specific rules (opt-in)
    - `AddCustomRule(IArchRule)` - Add custom ArchUnitNET rules
    - **(NEW Phase 6)** `EnforceRequestNaming()` - Enforce request naming conventions
    - **(NEW Phase 6)** `EnforceSealedAggregates(namespace)` - Enforce sealed aggregates
    - **(NEW Phase 6)** `EnforceSealedValueObjects()` - Enforce sealed value objects
    - **(NEW Phase 6)** `EnforceSealedSagas()` - Enforce sealed sagas
    - **(NEW Phase 6)** `EnforceSealedEventHandlers()` - Enforce sealed event handlers
    - **(NEW Issue #166)** `EnforceHandlerInterfaces()` - Enforce handler interface implementation
    - **(NEW Issue #166)** `EnforceCommandInterfaces()` - Enforce command interface implementation
    - **(NEW Issue #166)** `EnforceQueryInterfaces()` - Enforce query interface implementation
    - **(NEW Issue #166)** `EnforceHandlerControllerIsolation()` - Enforce handler-controller separation
    - **(NEW Issue #166)** `EnforcePipelineBehaviorInterfaces()` - Enforce behavior interface implementation
    - **(NEW Issue #166)** `EnforceSealedSagaData()` - Enforce sealed saga data classes
  - `ArchitectureRuleViolation` - Record for rule violation details
  - `ArchitectureVerificationResult` - Result with `IsSuccess`, `IsFailure`, `Violations`
  - `ArchitectureRuleException` - Exception with formatted violation messages
- **Encina.Testing.Verify Package** - Snapshot testing integration with Verify (Issue #430):
  - `EncinaVerifySettings` - Configuration for Verify with Encina-specific scrubbers:
    - `Initialize()` - Configures scrubbers and converters (idempotent)
    - Automatic scrubbing of UTC timestamps (CreatedAtUtc, ProcessedAtUtc, etc.)
    - Automatic scrubbing of ISO 8601 timestamps in content
    - Stack trace removal from error messages
    - Custom EncinaError converter for clean output
  - `EncinaVerify` - Static helper methods for snapshot preparation:
    - `PrepareEither<TLeft, TRight>()` - Prepare Either results (shows IsRight, Value/Error)
    - `ExtractSuccess<TResponse>()` - Extract success value or throw
    - `ExtractError<TResponse>()` - Extract error or throw
    - `PrepareUncommittedEvents()` - Prepare aggregate events with metadata
    - `PrepareOutboxMessages()` - Prepare outbox messages for verification
    - `PrepareInboxMessages()` - Prepare inbox messages for verification
    - `PrepareSagaState()` - Prepare saga state for verification
    - `PrepareScheduledMessages()` - Prepare scheduled messages for verification
    - `PrepareDeadLetterMessages()` - Prepare dead letter messages for verification
    - **(NEW Phase 5)** `PrepareHandlerResult<TRequest, TResponse>()` - Prepare handler result with request context
    - **(NEW Phase 5)** `PrepareSagaStates()` - Prepare multiple saga states for verification
    - **(NEW Phase 5)** `PrepareValidationError()` - Prepare validation error for verification
    - **(NEW Phase 5)** `PrepareTestScenario<TResponse>()` - Prepare complete test scenario with outbox/sagas
  - **(NEW Phase 5)** `EncinaTestContextExtensions` - Extension methods for Either results:
    - `ForVerify<TResponse>()` - Prepare Either for snapshot verification
    - `SuccessForVerify<TResponse>()` - Extract success value for verification
    - `ErrorForVerify<TResponse>()` - Extract error for verification
  - Automatic GUID scrubbing with deterministic placeholders (Guid_1, Guid_2, etc.)
  - Integration with Verify.Xunit for xUnit test framework
- **Encina.Aspire.Testing Package** - Aspire integration testing support (Issue #418):
  - `WithEncinaTestSupport()` - Extension for `DistributedApplicationTestingBuilder`:
    - Registers fake stores for testing (outbox, inbox, saga, scheduled, dead letter)
    - Configurable data cleanup before each test
    - Customizable wait timeouts and polling intervals
  - `EncinaTestSupportOptions` - Configuration for test behavior:
    - `ClearOutboxBeforeTest`, `ClearInboxBeforeTest`, `ResetSagasBeforeTest`
    - `ClearScheduledMessagesBeforeTest`, `ClearDeadLetterBeforeTest`
    - `DefaultWaitTimeout`, `PollingInterval`
  - `EncinaTestContext` - Centralized access to test state and operations:
    - Direct access to fake stores for inspection
    - `ClearAll()`, `ClearOutbox()`, `ClearInbox()`, `ClearSagas()` methods
  - Assertion extensions for messaging patterns:
    - `AssertOutboxContainsAsync<T>()` - Verify outbox contains notification type
    - `AssertInboxProcessedAsync()` - Verify inbox message was processed
    - `AssertSagaCompletedAsync<T>()`, `AssertSagaCompensatedAsync<T>()` - Verify saga lifecycle
    - `AssertDeadLetterContainsAsync<T>()` - Verify dead letter contains message type
  - Wait helpers for async operations:
    - `WaitForOutboxProcessingAsync()` - Wait for all outbox messages to be processed
    - `WaitForSagaCompletionAsync<T>()` - Wait for specific saga to complete
  - Inspection helpers:
    - `GetPendingOutboxMessages()`, `GetRunningSagas<T>()`, `GetDeadLetterMessages()`
    - `GetEncinaTestContext()`, `GetOutboxStore()`, `GetSagaStore()`
  - Failure simulation for resilience testing:
    - `SimulateSagaTimeout()`, `SimulateSagaFailure()` - Saga failure scenarios
    - `SimulateOutboxMessageFailure()`, `SimulateOutboxDeadLetter()` - Outbox failures
    - `SimulateInboxMessageFailure()`, `SimulateInboxExpiration()` - Inbox failures
    - `AddToDeadLetterAsync()` - Directly add messages to dead letter store

- **Encina.Testing.Respawn Package** - Intelligent database cleanup for integration tests (Issue #427):
  - `DatabaseRespawner` - Abstract base class for provider-specific respawners
  - `SqlServerRespawner` - SQL Server implementation using Respawn library
  - `PostgreSqlRespawner` - PostgreSQL implementation using Respawn library
  - `MySqlRespawner` - MySQL/MariaDB implementation using Respawn library
  - `SqliteRespawner` - Custom SQLite implementation (Respawn doesn't support SQLite natively)
  - `RespawnOptions` - Configuration for table filtering, schema control, and Encina messaging tables
  - `RespawnerFactory` - Factory for creating respawners with automatic provider detection
  - `RespawnAdapter` - Enum for supported database adapters (SqlServer, PostgreSql, MySql, Oracle)
  - Features:
    - Foreign key-aware reset (deletes in correct dependency order)
    - `TablesToIgnore` - Exclude specific tables from cleanup
    - `SchemasToInclude`/`SchemasToExclude` - Schema filtering
    - `ResetEncinaMessagingTables` - Option to preserve Outbox/Inbox/Saga tables (default: true)
    - `WithReseed` - Reset identity columns (default: true)
    - `CheckTemporalTables` - Handle SQL Server temporal tables
    - `InferAdapter()` - Automatically detect database provider from connection string
    - Builder pattern support via `FromBuilder()` method
  - Async initialization with `InitializeAsync()` for lazy respawner setup
  - `GetDeleteCommands()` for debugging and verification

- **Encina.Testing.Bogus Package** - Realistic test data generation with Bogus (Issue #431):
  - `EncinaFaker<T>` - Base faker class for Encina requests with reproducibility:
    - Default seed (12345) for deterministic test data
    - `UseSeed()`, `WithLocale()`, `StrictMode()` configuration methods
    - Fluent API for custom rules
  - `OutboxMessageFaker` - Generate outbox messages for testing:
    - Default pending state with random notification types
    - `AsProcessed()` - Generate processed messages
    - `AsFailed(retryCount)` - Generate failed messages with error info
    - `WithNotificationType()`, `WithContent()` customization
  - `InboxMessageFaker` - Generate inbox messages for idempotency testing:
    - `AsProcessed(response)` - Generate processed with cached response
    - `AsFailed(retryCount)` - Generate failed messages
    - `AsExpired()` - Generate expired messages for cleanup tests
    - `WithMessageId()`, `WithRequestType()` customization
  - `SagaStateFaker` - Generate saga states for orchestration testing:
    - `AsCompleted()`, `AsCompensating()`, `AsFailed()`, `AsTimedOut()` lifecycle states
    - `WithSagaType()`, `WithSagaId()`, `WithData()`, `AtStep()` customization
  - `ScheduledMessageFaker` - Generate scheduled messages:
    - `AsDue()` - Generate messages ready for execution
    - `AsRecurring(cron)` - Generate recurring messages with cron expression
    - `AsRecurringExecuted()` - Generate recurring with last execution
    - `ScheduledAt()`, `WithRequestType()`, `WithContent()` customization
  - Extension methods for common Encina patterns:
    - Identifiers: `CorrelationId()`, `UserId()`, `TenantId()`, `IdempotencyKey()`
    - Types: `NotificationType()`, `RequestType()`, `SagaType()`, `SagaStatus()`
    - UTC dates: `RecentUtc()`, `SoonUtc()`
    - JSON: `JsonContent(propertyCount)`
  - Fake implementations: `FakeOutboxMessage`, `FakeInboxMessage`, `FakeSagaState`, `FakeScheduledMessage`
  - **Domain Model Faker Extensions** (Issue #161) - Extension methods for DDD patterns:
    - Entity ID generation (`Randomizer` extensions):
      - `EntityId<TId>()` - Generic type-switched entity ID generation (Guid, int, long, string)
      - `GuidEntityId()` - Generate non-empty GUID identifiers
      - `IntEntityId(min, max)` - Generate positive integer IDs (min >= 1)
      - `LongEntityId(min, max)` - Generate positive long IDs (min >= 1)
      - `StringEntityId(length, prefix)` - Generate alphanumeric IDs with optional prefix
    - Strongly-typed ID value generation (`Randomizer` extensions):
      - `StronglyTypedIdValue<TValue>()` - Generic value generation for StronglyTypedId
      - `GuidStronglyTypedIdValue()` - Non-empty GUID for StronglyTypedId<Guid>
      - `IntStronglyTypedIdValue(min, max)` - Positive int for StronglyTypedId<int>
      - `LongStronglyTypedIdValue(min, max)` - Positive long for StronglyTypedId<long>
      - `StringStronglyTypedIdValue(length, prefix)` - Alphanumeric for StronglyTypedId<string>
    - Value object generation (`Randomizer` and `Date` extensions):
      - `QuantityValue(min, max)` - Non-negative integers (default: 0-1000)
      - `PercentageValue(min, max, decimals)` - Decimal percentage 0-100 with precision
      - `DateRangeValue(daysInPast, daysSpan)` - (DateOnly Start, DateOnly End) tuple
      - `TimeRangeValue(minHourSpan, maxHourSpan)` - (TimeOnly Start, TimeOnly End) tuple
    - Seed reproducibility for all domain model methods:
      - All ID and value methods are fully reproducible with seed alone
      - Date/time methods (`DateRangeValue`, `TimeRangeValue`) are reproducible relative to the current base date/time (i.e., seed + current UTC date/time)

- **Encina.Testing.WireMock Package** - HTTP API mocking for integration tests (Issues #428, #164):
  - `EncinaWireMockFixture` - xUnit fixture for in-process WireMock server with fluent API:
    - HTTP method stubs: `StubGet()`, `StubPost()`, `StubPut()`, `StubPatch()`, `StubDelete()`
    - Advanced stubbing: `Stub()` with request configuration, `StubSequence()` for sequential responses
    - Fault simulation: `StubFault()` for EmptyResponse, MalformedResponse, Timeout
    - Delay simulation: `StubDelay()` for testing timeout handling
    - Request verification: `VerifyCallMade()`, `VerifyNoCallsMade()`, `GetReceivedRequests()`
    - Server management: `Reset()`, `ResetRequestHistory()`, `CreateClient()`
  - `WireMockContainerFixture` - Docker-based WireMock via Testcontainers:
    - Automatic container lifecycle management
    - Admin API access via `CreateAdminClient()`
    - Full isolation for CI/CD environments
  - **(NEW Issue #164)** `EncinaRefitMockFixture<TApiClient>` - Refit API client testing fixture:
    - Generic fixture for any Refit API interface
    - Auto-configured Refit client via `CreateClient()`
    - HTTP method stubs: `StubGet()`, `StubPost()`, `StubPut()`, `StubPatch()`, `StubDelete()`
    - Error simulation: `StubError()` with status code and error response
    - Delay simulation: `StubDelay()` for timeout testing
    - Request verification: `VerifyCallMade()`, `VerifyNoCallsMade()`
    - Server management: `Reset()`, `ResetRequestHistory()`
  - **(NEW Issue #164)** `WebhookTestingExtensions` - Webhook endpoint testing:
    - `SetupWebhookEndpoint()` - Generic webhook endpoint setup
    - `SetupOutboxWebhook()` - Outbox pattern webhook endpoint (expects JSON POST)
    - `SetupWebhookFailure()` - Simulate webhook failures for retry testing
    - `SetupWebhookTimeout()` - Simulate webhook timeouts for resilience testing
    - `VerifyWebhookReceived()` - Verify webhook was called with count
    - `VerifyNoWebhooksReceived()` - Verify no webhooks were sent
    - `GetReceivedWebhooks()` - Get all received webhook requests
    - `GetReceivedWebhookBodies<T>()` - Deserialize received webhook bodies
  - `FaultType` enum - Defines fault types: EmptyResponse, MalformedResponse, Timeout
  - `ReceivedRequest` record - Captures request path, method, headers, body, timestamp
  - Fluent method chaining for stub configuration
  - Automatic JSON serialization with camelCase naming

- **Encina.Testing.Shouldly Package** - Open-source assertion extensions (Issue #429):
  - `EitherShouldlyExtensions` - Shouldly-style assertions for `Either<TLeft, TRight>` types:
    - Success assertions: `ShouldBeSuccess()`, `ShouldBeRight()` with value/validator overloads
    - Error assertions: `ShouldBeError()`, `ShouldBeLeft()` with validator overloads
    - EncinaError-specific: `ShouldBeValidationError()`, `ShouldBeNotFoundError()`, `ShouldBeAuthorizationError()`, `ShouldBeConflictError()`, `ShouldBeInternalError()`
    - Code/message assertions: `ShouldBeErrorWithCode()`, `ShouldBeErrorContaining()`
    - Async versions: `ShouldBeSuccessAsync()`, `ShouldBeErrorAsync()`, etc.
  - `EitherCollectionShouldlyExtensions` - Batch operation assertions:
    - `ShouldAllBeSuccess()`, `ShouldAllBeError()` for verifying all results
    - `ShouldContainSuccess()`, `ShouldContainError()` for at-least-one verification
    - `ShouldHaveSuccessCount()`, `ShouldHaveErrorCount()` for exact counts
    - Helper methods: `GetSuccesses()`, `GetErrors()`
  - **(NEW Issue #170)** `StreamingShouldlyExtensions` - IAsyncEnumerable assertions:
    - `ShouldAllBeSuccessAsync()`, `ShouldAllBeErrorAsync()` for streaming results
    - `ShouldContainSuccessAsync()`, `ShouldContainErrorAsync()` for at-least-one verification
    - `ShouldHaveCountAsync()`, `ShouldHaveSuccessCountAsync()`, `ShouldHaveErrorCountAsync()` for counts
    - `FirstShouldBeSuccessAsync()`, `FirstShouldBeErrorAsync()` for first item assertions
    - `ShouldBeEmptyAsync()`, `ShouldNotBeEmptyAsync()` for stream emptiness
    - EncinaError-specific: `ShouldContainValidationErrorForAsync()`, `ShouldNotContainAuthorizationErrorsAsync()`, `ShouldContainAuthorizationErrorAsync()`, `ShouldAllHaveErrorCodeAsync()`
    - Helper: `CollectAsync()` to materialize async streams
  - `AggregateShouldlyExtensions` - Event sourcing assertions:
    - `ShouldHaveRaisedEvent<T>()` with predicate overloads
    - `ShouldHaveRaisedEvents<T>(count)` for multiple events
    - `ShouldNotHaveRaisedEvent<T>()` for negative assertions
    - `ShouldHaveNoUncommittedEvents()`, `ShouldHaveUncommittedEventCount()`
    - `ShouldHaveVersion()`, `ShouldHaveId()` for aggregate state
    - Helpers: `GetRaisedEvents<T>()`, `GetLastRaisedEvent<T>()`
  - Open-source alternative to FluentAssertions ($130/dev/year)

- **FakeTimeProvider** - Controllable time for testing (Issue #433):
  - `FakeTimeProvider` - Thread-safe TimeProvider implementation for testing time-dependent code
  - Time manipulation: `SetUtcNow()`, `Advance()`, `AdvanceToNextDay()`, `AdvanceToNextHour()`, `AdvanceMinutes()`, `AdvanceSeconds()`, `AdvanceMilliseconds()`
  - Timer support via `CreateTimer()` with full `ITimer` implementation
  - Frozen time scope via `Freeze()` method that restores time on dispose
  - `Reset()` methods to restore time and clear timers
  - `ActiveTimerCount` property for timer verification
  - Support for one-shot and periodic timers
  - Deterministic timer firing controlled by time advancement
  - **Concurrency Guarantees**:
    - **Thread-safe**: `SetUtcNow()`, `Advance()` (atomic updates); `CreateTimer()` (concurrent creation); `ActiveTimerCount`, `GetUtcNow()` (read-only accessors)
    - **Not thread-safe**: Manual timer manipulation (`Change()`, `Dispose()` on individual timers); composed sequences (e.g., read-then-advance across threads). These require external synchronization.

- **Encina.Testing.Fakes Package** - Test doubles for Encina components (Issue #426):
  - `FakeEncina` - In-memory IEncina implementation with verification methods
  - `FakeOutboxStore`, `FakeInboxStore`, `FakeSagaStore` - Messaging store fakes
  - `FakeScheduledMessageStore`, `FakeDeadLetterStore` - Additional store fakes
  - Thread-safe implementations preserving insertion order
  - Fluent API for setup: `SetupResponse()`, `SetupError()`, `SetupStream()`
  - Verification methods: `WasSent()`, `WasPublished()`, `GetSentRequests()`
  - DI extensions: `AddFakeEncina()`, `AddFakeStores()`, `AddEncinaTestingFakes()`

- **Encina.DomainModeling Package** - DDD tactical pattern building blocks (Issues #367, #369, #374):
  - `Entity<TId>` - Base class for entities with identity-based equality
  - `ValueObject` - Base class for value objects with structural equality
  - `SingleValueObject<TValue>` - Wrapper for single-value primitives with implicit conversion
  - `StronglyTypedId<TValue>` - Base class for type-safe identifiers with comparison and equality
  - `GuidStronglyTypedId<TSelf>` - GUID-based strongly typed IDs with `New()`, `From()`, `TryParse()`, `Empty`
  - `IntStronglyTypedId<TSelf>`, `LongStronglyTypedId<TSelf>`, `StringStronglyTypedId<TSelf>` - Numeric and string IDs
  - `AggregateRoot<TId>` - Base class for aggregate roots with domain event support
  - `AuditableAggregateRoot<TId>` - Aggregate with `CreatedAtUtc`, `CreatedBy`, `ModifiedAtUtc`, `ModifiedBy`
  - `SoftDeletableAggregateRoot<TId>` - Aggregate with soft delete (`IsDeleted`, `DeletedAtUtc`, `DeletedBy`)
  - `DomainEvent` and `RichDomainEvent` - Base records for domain events with correlation/causation tracking
  - `IntegrationEvent` - Base record for cross-boundary events with versioning
  - `IDomainEventToIntegrationEventMapper<TDomain, TIntegration>` - Anti-corruption layer mapper interface
  - Auditing interfaces: `IAuditable`, `ISoftDeletable`, `IConcurrencyAware`, `IVersioned`

- **Specification Pattern** (Issue #295) - Query composition and encapsulation:
  - `Specification<T>` - Base class with `And()`, `Or()`, `Not()` composition
  - `QuerySpecification<T>` - Extended specification with includes, ordering, paging, tracking options
  - `QuerySpecification<T, TResult>` - Specification with projection support via `Selector`
  - Expression tree composition for LINQ provider compatibility
  - Implicit conversion to `Expression<Func<T, bool>>`

- **Business Rules Pattern** (Issue #372) - Domain invariant validation:
  - `IBusinessRule` interface with `ErrorCode`, `ErrorMessage`, `IsSatisfied()`
  - `BusinessRule` abstract base class
  - `BusinessRuleViolationException` for throw-based validation
  - `BusinessRuleError` and `AggregateBusinessRuleError` records for ROP
  - `BusinessRuleExtensions`: `Check()`, `CheckFirst()`, `CheckAll()`, `ThrowIfNotSatisfied()`, `ThrowIfAnyNotSatisfied()`

- **Domain Service Marker** (Issue #377) - Semantic clarity:
  - `IDomainService` marker interface for domain services identification

- **Anti-Corruption Layer Pattern** (Issue #299) - Bounded context translation:
  - `TranslationError` record with factory methods (`UnsupportedType`, `MissingRequiredField`, `InvalidFormat`)
  - `IAntiCorruptionLayer<TExternal, TInternal>` sync interface
  - `IAsyncAntiCorruptionLayer<TExternal, TInternal>` async variant
  - `AntiCorruptionLayerBase<TExternal, TInternal>` with helper methods

- **Result Pattern Extensions** (Issue #468) - Fluent API for Either type:
  - Combination: `Combine()` for 2/3/4 values and collections
  - Conditional: `When()`, `Ensure()`, `OrElse()`, `GetOrDefault()`, `GetOrElse()`
  - Side effects: `Tap()`, `TapError()`
  - Async: `BindAsync()`, `MapAsync()`, `TapAsync()` (for `Task<Either>` and `Either`)
    - New Encina extensions in `Encina.Core.Extensions.EitherAsyncExtensions`
    - Namespace: `using Encina.Core.Extensions;`
    - Example: `Task<Either<L, R>> BindAsync<L, R, R2>(this Task<Either<L, R>> task, Func<R, Task<Either<L, R2>>> f)`
  - Conversion: `ToOption()`, `ToEither()` (from Option), `GetOrThrow()`

- **Rich Domain Event Envelope** (Issue #368) - Extended domain event metadata:
  - `IDomainEventMetadata` interface with `CorrelationId`, `CausationId`, `UserId`, `TenantId`, `AdditionalMetadata`
  - `DomainEventMetadata` record with factory methods (`Empty`, `WithCorrelation`, `WithCausation`)
  - `DomainEventEnvelope<TEvent>` - Wraps events with metadata, envelope ID, and timestamp
  - `DomainEventExtensions` - Fluent API: `ToEnvelope()`, `WithMetadata()`, `WithCorrelation()`, `Map()`

- **Integration Event Extensions** (Issue #373) - Cross-context event mapping:
  - `IAsyncDomainEventToIntegrationEventMapper<TDomain, TIntegration>` - Async mapper interface
  - `IFallibleDomainEventToIntegrationEventMapper<TDomain, TIntegration, TError>` - ROP mapper with Either
  - `IIntegrationEventPublisher` - Publisher interface with `PublishAsync()`, `PublishManyAsync()`
  - `IFallibleIntegrationEventPublisher<TError>` - ROP publisher variant
  - `IntegrationEventMappingError` and `IntegrationEventPublishError` - Structured error types
  - `IntegrationEventMappingExtensions` - `MapTo()`, `MapToAsync()`, `MapAll()`, `TryMapTo()`, `Compose()`

- **Generic Repository Pattern** (Issue #380) - Provider-agnostic data access:
  - `IReadOnlyRepository<TEntity, TId>` - Query operations with Specification support
  - `IRepository<TEntity, TId>` - Full CRUD operations extending read-only
  - `IAggregateRepository<TAggregate, TId>` - Aggregate-specific with `SaveAsync()`
  - `PagedResult<T>` - Pagination with `TotalPages`, `HasPreviousPage`, `HasNextPage`, `Map()`
  - `RepositoryError` - Error types (`NotFound`, `AlreadyExists`, `ConcurrencyConflict`, `OperationFailed`)
  - `RepositoryExtensions` - `GetByIdOrErrorAsync()`, `GetByIdOrThrowAsync()`, `AddIfNotExistsAsync()`, `UpdateIfExistsAsync()`
  - `EntityNotFoundException` - Exception for entity lookup failures

- **Ports & Adapters Factory Pattern** (Issue #475) - Hexagonal Architecture support:
  - `IPort`, `IInboundPort`, `IOutboundPort` - Port marker interfaces
  - `IAdapter<TPort>` - Adapter marker interface with port constraint
  - `AdapterBase<TPort>` - Base class with `Execute()`, `ExecuteAsync()` for error handling
  - `AdapterError` - Error types (`OperationFailed`, `Cancelled`, `NotFound`, `CommunicationFailed`, `ExternalError`)
  - `PortRegistrationExtensions` - DI registration: `AddPort<TPort, TAdapter>()`, `AddPortsFromAssembly()`

- **Result/DTO Mapping with ROP Semantics** (Issue #478) - Domain-to-DTO mapping:
  - `IResultMapper<TDomain, TDto>` - Sync mapper returning `Either<MappingError, TDto>`
  - `IAsyncResultMapper<TDomain, TDto>` - Async mapper variant
  - `IBidirectionalMapper<TDomain, TDto>` - Two-way mapping with `MapToDomain()`
  - `IAsyncBidirectionalMapper<TDomain, TDto>` - Async bidirectional variant
  - `MappingError` - Error types (`NullProperty`, `ValidationFailed`, `ConversionFailed`, `EmptyCollection`)
  - `ResultMapperExtensions` - `MapAll()`, `MapAllCollectErrors()`, `TryMap()`, `Compose()`
  - `ResultMapperRegistrationExtensions` - `AddResultMapper()`, `AddResultMappersFromAssembly()`

- **Application Services Interface** (Issue #479) - Use case orchestration:
  - `IApplicationService` - Marker interface for application services
  - `IApplicationService<TInput, TOutput>` - Typed service with `ExecuteAsync()`
  - `IApplicationService<TOutput>` - Parameterless service for scheduled tasks
  - `IVoidApplicationService<TInput>` - Service returning `Unit` on success
  - `ApplicationServiceError` - Error types (`NotFound`, `ValidationFailed`, `BusinessRuleViolation`, `ConcurrencyConflict`, `InfrastructureFailure`, `Unauthorized`)
  - `ApplicationServiceExtensions` - `ToApplicationServiceError()` for error conversion
  - `ApplicationServiceRegistrationExtensions` - `AddApplicationService()`, `AddApplicationServicesFromAssembly()`

- **Bounded Context Patterns** (Issues #379, #477) - Strategic DDD support:
  - `BoundedContextAttribute` - Mark types with bounded context membership
  - `ContextMap` - Document relationships between contexts with fluent API
  - `ContextRelationship` enum - Conformist, ACL, SharedKernel, CustomerSupplier, PublishedLanguage, SeparateWays, Partnership, OpenHost
  - `ContextRelation` record - Stores upstream/downstream context with relationship type
  - `BoundedContextModule` abstract class - Modular monolith module base with DI configuration
  - `IBoundedContextModule` interface - Module contract with `Name`, `Dependencies`, `ConfigureServices()`
  - `BoundedContextValidator` - Validate circular dependencies and orphaned consumers
  - `BoundedContextError` - Error types (`OrphanedConsumer`, `CircularDependency`, `ValidationFailed`)
  - `BoundedContextExtensions` - `GetBoundedContextName()`, `AddBoundedContextModules()`, `ValidateBoundedContexts()`
  - Mermaid diagram generation via `ToMermaidDiagram()`

- **Domain Language DSL** (Issue #381) - Fluent domain building:
  - `DomainBuilder<T, TBuilder>` - CRTP fluent builder with ROP
  - `AggregateBuilder<TAggregate, TId, TBuilder>` - Aggregate builder with business rule validation
  - `DomainBuilderError` - Error types (`MissingValue`, `ValidationFailed`, `BusinessRulesViolated`, `InvalidState`)
  - `DomainDslExtensions` - Fluent specification checks: `Is()`, `Satisfies()`, `Violates()`, `Passes()`, `Fails()`
  - Fluent validation: `EnsureValid()`, `EnsureNotNull()`, `EnsureNotEmpty()` returning Either
  - **Common Domain Types**:
    - `Quantity` struct - Non-negative quantity with arithmetic operators
    - `Percentage` struct - 0-100 percentage with `ApplyTo()`, `AsFraction`, `Complement`
    - `DateRange` struct - Date range with `Contains()`, `Overlaps()`, `Intersect()`, `ExtendBy()`
    - `TimeRange` struct - Time range with `Duration`, `Contains()`, `Overlaps()`

- **Vertical Slice + Hexagonal Hybrid Architecture** (Issue #476) - Feature slices:
  - `FeatureSlice` abstract class - Base for vertical slices with `FeatureName`, `ConfigureServices()`
  - `IFeatureSliceWithEndpoints` - Slices with HTTP endpoint configuration
  - `IFeatureSliceWithDependencies` - Slices with explicit inter-slice dependencies
  - `SliceDependency` record - Represents dependency on another slice (optional flag)
  - `FeatureSliceConfiguration` - Fluent configuration: `AddSlice<T>()`, `AddSlicesFromAssembly()`
  - `FeatureSliceExtensions` - `AddFeatureSlices()`, `AddFeatureSlice<T>()`, `GetFeatureSlices()`, `GetFeatureSlice()`
  - `FeatureSliceError` - Error types (`MissingDependency`, `CircularDependency`, `RegistrationFailed`)
  - **Use Case Handlers**:
    - `IUseCaseHandler` marker interface
    - `IUseCaseHandler<TInput, TOutput>` - Handler with input and output
    - `IUseCaseHandler<TInput>` - Command handler (void output)
    - `UseCaseHandlerExtensions` - `AddUseCaseHandler<T>()`, `AddUseCaseHandlersFromAssembly()`

- Comprehensive test coverage: 175 unit tests, 275 property tests, 531 contract tests, 80 guard tests (1061 total).
  - **Note**: Load tests (`[Trait("Category", "Load")]`) are excluded from default CI runs due to a .NET 10 JIT bug.
  - **Known Issue - CLR Crash on .NET 10** (Encina Issue #5):
    - **Scope**: Affects load tests only (NBomber + `IAsyncEnumerable<Either<EncinaError, T>>` under high concurrency). Production code is not affected.
    - **Affected Versions**: .NET 10.0.x (all current releases)
    - **Upstream Bug**: [dotnet/runtime#121736](https://github.com/dotnet/runtime/issues/121736) - Fixed in .NET 11, awaiting .NET 10.x backport
    - **CI/CD Mitigation**: Load tests are excluded via project name pattern (`*LoadTests*`) in [.github/workflows/ci.yml](.github/workflows/ci.yml). Dedicated load test workflow runs separately with workaround.
    - **Local Workaround**: Set `DOTNET_JitObjectStackAllocationConditionalEscape=0` before running load tests
    - **Internal Docs**: See [docs/history/2025-12.md](docs/history/2025-12.md#clr-crash-on-net-10-issue-5) "Known Issues" section

#### AI/LLM Patterns Issues (12 new features planned based on December 29, 2025 research)

- **MCP (Model Context Protocol) Support** (Issue #481) - MCP server/client integration
    - `MCPServerBuilder` for creating MCP servers in C#
    - `MCPClientBehavior` for consuming external MCP tools
    - Native integration with `IEncina` - expose handlers as AI tools
    - SSE and HTTP transports
    - Azure Functions support for remote MCP servers
    - Priority: HIGH - Industry standard adopted by OpenAI, Anthropic, Microsoft
  - **Semantic Caching Pipeline Behavior** (Issue #482) - Embedding-based cache
    - `SemanticCachingPipelineBehavior<TRequest, TResponse>`
    - `ISemanticCacheProvider` abstraction with Redis, Qdrant providers
    - Similarity threshold configurable (default 0.95)
    - Reduces LLM costs by 40-70%, latency from 850ms to <120ms
    - New packages planned: `Encina.AI.Caching.Redis`, `Encina.AI.Caching.Qdrant`
    - Priority: HIGH - Major cost reduction
  - **AI Guardrails & Safety Pipeline** (Issue #483) - Security for AI applications
    - `PromptInjectionDetectionBehavior` - OWASP #1 threat for LLMs
    - `PIIDetectionBehavior` - Detect and redact sensitive data
    - `ContentModerationBehavior` - Filter harmful content
    - `IGuardrailProvider` abstraction with Azure Prompt Shields, AWS Bedrock, OpenGuardrails
    - Configurable actions: Block, Warn, Log, Redact
    - New package planned: `Encina.AI.Safety`
    - Priority: HIGH - Essential for production AI
  - **RAG Pipeline Patterns** (Issue #484) - Retrieval-Augmented Generation
    - `IRagPipeline<TQuery, TResponse>` abstraction
    - Query rewriting (multi-query, HyDE), chunk retrieval, re-ranking
    - Hybrid search (keyword + semantic)
    - Agentic RAG with query planning
    - Citation/source tracking
    - New package planned: `Encina.AI.RAG`
    - Priority: HIGH - Most demanded LLM pattern
  - **Token Budget & Cost Management** (Issue #485) - LLM cost control
    - `TokenBudgetPipelineBehavior<TRequest, TResponse>`
    - `ITokenBudgetStore` with Redis/SQL providers
    - Per-user/tenant/request type limits
    - Cost estimation and reporting
    - Automatic fallback to cheaper models
    - Priority: MEDIUM - Enterprise cost control
  - **LLM Observability Integration** (Issue #486) - AI-specific metrics
    - Enhancement to `Encina.OpenTelemetry`
    - `LLMActivityEnricher` with GenAI semantic conventions
    - Token usage metrics (input/output/cached/reasoning)
    - Time to first token (TTFT) measurement
    - Cost attribution per model/user/tenant
    - Integration with Langfuse, Datadog LLM Observability
    - Priority: HIGH - Production monitoring
  - **Multi-Agent Orchestration Patterns** (Issue #487) - AI agent workflows
    - `IAgent` and `IAgentHandler<TRequest, TResponse>` interfaces
    - Orchestration patterns: Sequential, Concurrent, Handoff, GroupChat, Magentic
    - `IAgentSelector` for dynamic routing
    - Human-in-the-Loop (HITL) support
    - Cross-language agents via MCP
    - Semantic Kernel adapter: `SemanticKernelAgentAdapter`
    - New package planned: `Encina.Agents`
    - Priority: MEDIUM - Microsoft Agent Framework
  - **Structured Output Handler** (Issue #488) - JSON schema enforcement
    - `IStructuredOutputHandler<TRequest, TOutput>` interface
    - `IJsonSchemaGenerator` with System.Text.Json support
    - Response validation with retry on failure
    - Fallback parsing for edge cases
    - Priority: MEDIUM - Schema guarantees
  - **Function Calling Orchestration** (Issue #489) - LLM tool use
    - `IFunctionCallingOrchestrator` interface
    - `[AIFunction]` attribute for handler decoration
    - Auto/Manual/Confirm invocation modes
    - Parallel function calls support
    - Semantic Kernel plugin adapter: `EncinaPluginAdapter`
    - Priority: MEDIUM - Native LLM feature
  - **Vector Store Abstraction** (Issue #490) - Embedding storage
    - `IVectorStore` and `IVectorRecord` abstractions
    - Integration with `IEmbeddingGenerator` (Microsoft.Extensions.AI)
    - Metadata filtering, hybrid search, batch operations
    - New packages planned: `Encina.VectorData`, `Encina.VectorData.Qdrant`, `Encina.VectorData.AzureSearch`, `Encina.VectorData.Milvus`, `Encina.VectorData.Chroma`, `Encina.VectorData.InMemory`
    - Priority: HIGH - Foundation for RAG and Semantic Cache
  - **Prompt Management & Versioning** (Issue #491) - Enterprise prompt governance
    - `IPromptRepository` and `IPromptTemplateEngine`
    - Versioned prompt templates with A/B testing
    - Prompt analytics and performance tracking
    - Storage providers: FileSystem (YAML/JSON), Database, Git
    - Priority: LOW - Enterprise governance
  - **AI Streaming Pipeline Enhancement** (Issue #492) - Token-level streaming
    - `IAIStreamRequest<TChunk>` and `TokenChunk` types
    - `BackpressureStreamBehavior` for slow consumers
    - SSE endpoint helper in `Encina.AspNetCore`
    - Time to first token (TTFT) measurement
    - Integration with `IChatClient.CompleteStreamingAsync`
    - Priority: MEDIUM - UX enhancement

#### Hexagonal Architecture Patterns Issues (10 new features planned based on December 29, 2025 research)

- **Domain Events vs Integration Events** (Issue #470) - Formal separation between domain events and integration events
    - `DomainEvent` base class with AggregateId, OccurredAtUtc, Version
    - `IntegrationEvent` base class extending INotification with EventType, CorrelationId
    - `IDomainEventHandler<TEvent>` for in-process synchronous processing
    - Integration with existing Outbox pattern for integration event publishing
    - Priority: CRITICAL - Foundational for DDD/microservices
  - **Specification Pattern** (Issue #471) - Composable, testable query encapsulation
    - `Specification<T>` and `Specification<T, TResult>` base classes
    - `ISpecificationRepository<T>` with EF Core and Dapper support
    - And/Or/Not composition operators
    - New packages planned: `Encina.Specifications`, `Encina.Specifications.EFCore`
    - Priority: CRITICAL - High demand (~9M downloads for Ardalis.Specification)
  - **Value Objects & Aggregates** (Issue #472) - DDD building blocks
    - `ValueObject` with structural equality
    - `StronglyTypedId<T>` for type-safe identifiers
    - `Entity<TId>` for non-root entities
    - `AggregateRoot<TId>` with domain events and version for concurrency
    - New package planned: `Encina.DDD`
    - Priority: CRITICAL - Core DDD patterns
  - **Domain Services** (Issue #473) - IDomainService marker interface
    - Pure domain logic without infrastructure dependencies
    - Auto-registration via `AddDomainServicesFromAssembly()`
    - Priority: HIGH - Complements handlers with domain logic
  - **Anti-Corruption Layer** (Issue #474) - External system isolation
    - `IAntiCorruptionLayer<TExternal, TDomain>` bidirectional interface
    - `IInboundAntiCorruptionLayer` and `IOutboundAntiCorruptionLayer`
    - ROP semantics for all translations
    - New package planned: `Encina.Hexagonal`
    - Priority: HIGH - Essential for integrations
  - **Ports & Adapters Factory** (Issue #475) - Hexagonal architecture formalization
    - `IPort`, `IInboundPort`, `IOutboundPort` marker interfaces
    - `AddPort<TPort, TAdapter>()` registration method
    - `AdapterBase<TPort>` with logging and error handling
    - Priority: HIGH - Formalizes hexagonal boundaries
  - **Vertical Slice + Hexagonal Hybrid** (Issue #476) - Feature organization
    - `IFeatureSlice` extending `IModule`
    - `MapEncinaSlices()` for endpoint mapping
    - Combines vertical slice organization with hexagonal boundaries
    - Priority: MEDIUM - Architectural guidance
  - **Bounded Context Modules** (Issue #477) - Module boundary contracts
    - `IBoundedContextModule` with PublishedIntegrationEvents and ConsumedIntegrationEvents
    - `IContextMap` for relationship visualization
    - Mermaid diagram generation
    - Priority: MEDIUM - SaaS and modular monolith support
  - **Result/DTO Mapping** (Issue #478) - Domain to DTO with ROP
    - `IResultMapper<TDomain, TDto>` interface
    - `IAsyncResultMapper<TDomain, TDto>` for async mappings
    - `MapAll()` and `MapAllCollectErrors()` extensions
    - Priority: MEDIUM
  - **Application Services** (Issue #479) - Use case orchestration
    - `IApplicationService<TInput, TOutput>` interface
    - Clear separation from Domain Services (logic vs orchestration)
    - Priority: MEDIUM

- New labels created for Hexagonal Architecture Patterns:
  - `area-application-services` - Application Services and use case orchestration (#2E8B57)
  - `area-ports-adapters` - Ports and Adapters (Hexagonal Architecture) patterns (#4169E1)
  - `area-hexagonal` - Hexagonal Architecture patterns and infrastructure (#6A5ACD)
  - `area-dto-mapping` - DTO mapping and object transformation patterns (#9370DB)
  - `clean-architecture` - Clean Architecture patterns and structure (#32CD32)
  - `abp-inspired` - Pattern inspired by ABP Framework (#FF6B6B)
  - `ardalis-inspired` - Pattern inspired by Steve Smith (Ardalis) libraries (#FF8C00)

#### TDD Patterns Issues (12 new features planned based on December 29, 2025 research)

- **Encina.Testing.Fakes** (Issue #426) - Test doubles for IEncina and messaging stores
    - `FakeEncina : IEncina` with configurable handlers
    - `FakeOutboxStore`, `FakeInboxStore`, `FakeSagaStore`, `FakeScheduledMessageStore`
    - Verification methods: `VerifySent<T>()`, `VerifyPublished<T>()`
    - Implemented: `Encina.Testing.Fakes`
    - Priority: HIGH - Foundational for unit testing
  - **Encina.Testing.Respawn** (Issue #427) - Intelligent database reset with Respawn
    - `RespawnDatabaseFixture<TContainer>` base class
    - FK-aware deterministic deletion (3x faster than truncate)
    - Integration with existing Testcontainers fixtures
    - Implemented: `Encina.Testing.Respawn`
    - Priority: HIGH - Essential for integration testing
  - **Encina.Testing.WireMock** (Issue #428) - HTTP API mocking for integration tests
    - `EncinaWireMockFixture` with fluent stubbing API
    - Fault simulation for resilience testing
    - Implemented: `Encina.Testing.WireMock`
    - Priority: HIGH - External API testing
  - **Encina.Testing.Shouldly** (Issue #429) - Open-source assertions (FluentAssertions alternative)
    - `ShouldBeSuccess()`, `ShouldBeError()` for `Either<EncinaError, T>`
    - Replaces FluentAssertions after commercial license change (Jan 2025)
    - Implemented: `Encina.Testing.Shouldly`
    - Priority: HIGH - Open-source assertion library
  - **Encina.Testing.Verify** (Issue #430) - Snapshot testing integration
    - `VerifyEither()`, `VerifyUncommittedEvents()`, `VerifySagaState()`
    - Automatic scrubbers for timestamps and GUIDs
    - New package planned: `Encina.Testing.Verify`
    - Priority: MEDIUM
  - **Encina.Testing.Bogus** (Issue #431) - Realistic test data generation
    - `EncinaFaker<TRequest>` base class with conventions
    - Pre-built fakers for messaging entities
    - New package planned: `Encina.Testing.Bogus`
    - Priority: MEDIUM
  - **Encina.Testing.Architecture** (Issue #432) - Architectural rules enforcement
    - `EncinaArchitectureRules` with CQRS/DDD rules
    - ArchUnitNET integration
    - New package planned: `Encina.Testing.Architecture`
    - Priority: MEDIUM
  - **FakeTimeProvider** (Issue #433) - Time control for testing
    - `FakeTimeProvider : TimeProvider` (.NET 8+ compatible)
    - `Advance()`, `SetUtcNow()`, `Freeze()` methods
    - Priority: MEDIUM - Added to Encina.Testing core
  - **BDD Specification Testing** (Issue #434) - Given/When/Then for handlers
    - `HandlerSpecification<TRequest, TResponse>` base class
    - Extension of existing `AggregateTestBase` pattern
    - Priority: LOW
  - **Encina.Testing.FsCheck** (Issue #435) - Property-based testing extensions
    - `EncinaArbitraries` for Encina types
    - `EncinaProperties` for common invariants
    - New package planned: `Encina.Testing.FsCheck`
    - Priority: LOW - Advanced testing
  - **Encina.Testing.Pact** (Issue #436) - Consumer-Driven Contract Testing
    - `EncinaPactConsumerBuilder`, `EncinaPactProviderVerifier`
    - Microservices contract verification
    - New package planned: `Encina.Testing.Pact`
    - Priority: LOW - Microservices focus
  - **Stryker.NET Configuration** (Issue #437) - Mutation testing templates
    - `stryker-config.json` templates
    - GitHub Actions and Azure DevOps workflows
    - `encina generate stryker` CLI command
    - Priority: LOW - Quality tooling

- New labels created for TDD Patterns:
  - `testing-property-based` - Property-based testing and invariant verification (#9B59B6)
  - `testing-contract` - Contract testing and consumer-driven contracts (#8E44AD)
  - `testing-bdd` - Behavior-driven development and Given/When/Then patterns (#A569BD)
  - `testing-time-control` - Time manipulation and FakeTimeProvider for testing (#7D3C98)
  - `testing-assertions` - Assertion libraries and fluent assertions (#6C3483)
  - `testing-database-reset` - Database cleanup and reset between tests (#5B2C6F)

#### Developer Tooling & DX Issues (11 new features planned based on December 29, 2025 research)

- **Encina.Analyzers** (Issue #438) - Roslyn analyzers and code fixes
    - 10+ analyzers: ENC001 (CancellationToken), ENC002 (Validator missing), ENC003 (Saga compensation)
    - Code fixes: generate handler skeleton, add CancellationToken, implement IIdempotentRequest
    - Compatible with NativeAOT and Source Generators
    - New package planned: `Encina.Analyzers`
    - Priority: HIGH - Compile-time error detection
  - **Saga Visualizer** (Issue #439) - State machine diagram generation
    - Generate Mermaid, Graphviz (DOT), PlantUML from saga definitions
    - Runtime visualization with current state highlighted
    - CLI: `encina visualize saga OrderFulfillmentSaga --output mermaid`
    - Priority: MEDIUM - Documentation and debugging
  - **Encina.Aspire** (Issue #440) - .NET Aspire integration package
    - `EncinaResource` as first-class Aspire resource
    - Dashboard panel for Outbox, Inbox, Sagas
    - Health checks and OTLP pre-configured
    - New packages planned: `Encina.Aspire`, `Encina.Aspire.Hosting`
    - Priority: HIGH - Modern .NET stack integration
  - **Encina.Diagnostics** (Issue #441) - Enhanced exception formatting
    - Pretty-print for `EncinaError` with box-drawing characters
    - Demystified stack traces (Ben.Demystifier-inspired)
    - Validation errors grouped by property
    - ANSI colors with plain text fallback
    - New package planned: `Encina.Diagnostics`
    - Priority: MEDIUM - Developer experience
  - **Hot Reload Support** (Issue #442) - Handler hot reload
    - Integration with `MetadataUpdateHandler.ClearCache`
    - Automatic pipeline cache invalidation
    - Development-only (no production impact)
    - Priority: MEDIUM - Inner loop improvement
  - **AI-Ready Request Tracing** (Issue #443) - Request/response capture
    - Automatic serialization with PII redaction
    - `[Trace]` attribute with RedactProperties
    - AI-compatible export format
    - Sampling strategies (errors always, slow requests, random)
    - Priority: MEDIUM - Modern debugging
  - **Enhanced Testing Fixtures** (Issue #444) - Testing improvements
    - `EncinaTestFixture` with fluent builder
    - Either assertions: `ShouldBeSuccess()`, `ShouldBeError()`
    - Time-travel for sagas: `AdvanceTimeBy()`
    - Outbox/Inbox assertions: `OutboxShouldContain<T>()`
    - Priority: MEDIUM - Testing DX
  - **Encina.Dashboard** (Issue #445) - Developer dashboard web UI
    - Local web UI for debugging (Hangfire Dashboard-style)
    - Panels: Handlers, Pipeline, Outbox, Inbox, Sagas, Cache, Errors
    - Real-time updates via SignalR
    - Actions: Retry Outbox, Cancel Saga, Invalidate Cache
    - New package planned: `Encina.Dashboard`
    - Priority: HIGH - Development visibility
  - **Encina.OpenApi** (Issue #446) - OpenAPI integration
    - Auto-generate OpenAPI spec from Commands/Queries
    - `app.MapEncinaEndpoints()` for endpoint generation
    - FluentValidation constraints in schema
    - OpenAPI 3.1 support (.NET 10)
    - New package planned: `Encina.OpenApi`
    - Priority: MEDIUM - API-first development
  - **Dev Containers Support** (Issue #447) - Container development
    - `.devcontainer/` configuration
    - GitHub Codespaces support
    - Docker Compose with Postgres, Redis, RabbitMQ
    - CLI: `encina add devcontainer --services postgres,redis`
    - Priority: LOW - Developer onboarding
  - **Interactive Documentation** (Issue #448) - Documentation site
    - Docusaurus or DocFX site
    - Playground with executable code
    - API reference from XML docs
    - Versioned documentation
    - Priority: LOW - Community growth

- New labels created for Developer Tooling:
  - `area-analyzers` - Roslyn analyzers and code fixes (#5C2D91)
  - `area-visualization` - Visualization and diagram generation (#9B59B6)
  - `area-dashboard` - Developer dashboard and monitoring UI (#E74C3C)
  - `area-diagnostics` - Diagnostics, error formatting, and debugging aids (#E67E22)
  - `area-devcontainers` - Dev Containers and Codespaces support (#0DB7ED)
  - `area-documentation-site` - Documentation website and interactive docs (#3498DB)

#### .NET Aspire Integration Patterns Issues (10 new features planned based on December 29, 2025 research)

- **Encina.Aspire.Hosting** (Issue #416) - AppHost integration package
    - `WithEncina()` extension method for `IResourceBuilder<ProjectResource>`
    - Custom `EncinaResource` for Dashboard visibility
    - Configuration propagation via environment variables
    - Custom commands: Process Outbox, Retry Dead Letters, Cancel Saga
    - New package planned: `Encina.Aspire.Hosting`
    - Priority: MEDIUM - Foundational for Aspire integration
  - **Encina.Aspire.ServiceDefaults** (Issue #417) - Service Defaults extension
    - `AddEncinaDefaults()` for `IHostApplicationBuilder`
    - OpenTelemetry integration (tracing + metrics)
    - Health checks for all messaging patterns
    - Standard Resilience pipeline integration
    - New package planned: `Encina.Aspire.ServiceDefaults`
    - Priority: HIGH - Centralizes cross-cutting concerns
  - **Encina.Aspire.Testing** (Issue #418) - Testing integration
    - `WithEncinaTestSupport()` for `DistributedApplicationTestingBuilder`
    - Assertion extensions: `AssertOutboxContains<T>()`, `AssertSagaCompleted<T>()`
    - Test data reset helpers (clear outbox, inbox, sagas)
    - Wait helpers: `WaitForOutboxProcessing()`, `WaitForSagaCompletion()`
    - New package planned: `Encina.Aspire.Testing`
    - Priority: HIGH - "Largest gap" per official Aspire roadmap
  - **Encina.Aspire.Dashboard** (Issue #419) - Dashboard extensions
    - Custom commands via `WithCommand()` API
    - Encina-specific metrics visibility
    - Commands: `process-outbox`, `retry-dead-letters`, `cancel-saga`
    - New package planned: `Encina.Aspire.Dashboard`
    - Priority: MEDIUM - Improves observability
  - **Encina.Dapr** (Issue #420) - Dapr building blocks integration
    - Dapr State Store for Saga/Scheduling state
    - Dapr Pub/Sub for Outbox publishing
    - Dapr Service Invocation for inter-service commands
    - Dapr Actors for saga orchestration (optional)
    - New package planned: `Encina.Dapr`
    - Priority: MEDIUM - CNCF graduated, high demand
  - **Encina.Aspire.Deployment** (Issue #421) - Deployment publishers
    - Azure Container Apps publisher with Encina infrastructure
    - Kubernetes manifests generation
    - Docker Compose environment support
    - KEDA scaling rules for processors
    - New package planned: `Encina.Aspire.Deployment`
    - Priority: LOW - azd already automates much
  - **Encina.Aspire.AI** (Issue #422) - AI Agent & MCP Server support
    - MCP Server to expose Encina state to AI agents
    - Tools: `analyze_saga_failure`, `retry_dead_letter`
    - Azure AI Foundry integration
    - Dashboard Copilot integration
    - New package planned: `Encina.Aspire.AI`
    - Priority: LOW - Roadmap 2026
  - **Modular Monolith Architecture Support** (Issue #423)
    - `IEncinaModule` interface with lifecycle hooks
    - `WithEncinaModules()` for AppHost
    - Inter-module communication via Encina messaging
    - Module isolation with separate DbContexts
    - Priority: MEDIUM - Trending architecture 2025
  - **Multi-Repo Support** (Issue #424)
    - `AddEncinaExternalService()` for services in other repos
    - Service discovery: Kubernetes, Consul, DNS
    - Shared message broker configuration
    - Contract-first approach
    - Priority: MEDIUM - Enterprise demand
  - **Hot Reload Support** (Issue #425)
    - Hot reload of handlers during development
    - Integration with `MetadataUpdateHandler`
    - State preservation (outbox, inbox, sagas)
    - Dashboard indication during reload
    - Priority: MEDIUM - Developer experience

- New labels created for Aspire integration:
  - `area-aspire` - Aspire hosting, orchestration, and deployment (#512BD4)
  - `area-mcp` - Model Context Protocol and AI agent integration (#8B5CF6)
  - `area-hot-reload` - Hot reload and live code updates (#F59E0B)

#### Cloud-Native Patterns Issues (11 new features planned based on December 29, 2025 research)

- **Encina.Aspire** (Issue #449) - .NET Aspire integration
    - `AddEncinaAspireDefaults()` extension method
    - Service Discovery integration for distributed handlers
    - OpenTelemetry pre-configured for Encina pipeline
    - Health checks for Outbox, Inbox, Sagas
    - Aspire Dashboard integration
    - New package planned: `Encina.Aspire`
    - Priority: HIGH - Official Microsoft cloud-native stack
  - **Encina.Dapr** (Issue #450) - Dapr Building Blocks integration
    - `DaprSagaStore`, `DaprOutboxStore`, `DaprInboxStore` via Dapr State API
    - `DaprOutboxPublisher` via Dapr Pub/Sub
    - `DaprDistributedLockProvider` via Dapr Lock API
    - Secrets injection via `[DaprSecret]` attribute
    - Cloud-agnostic: same code on AWS, Azure, GCP, on-prem
    - New package planned: `Encina.Dapr`
    - Priority: HIGH - CNCF graduated, multi-cloud demand
  - **Encina.FeatureFlags** (Issue #451) - Feature flags abstraction
    - `IFeatureFlagProvider` abstraction
    - `[Feature("key")]` attribute for handler injection
    - `FeatureFlagInjectionBehavior` pipeline behavior
    - Providers: ConfigCat, LaunchDarkly, Azure App Configuration, OpenFeature
    - New packages planned: `Encina.FeatureFlags`, `Encina.FeatureFlags.ConfigCat`, `Encina.FeatureFlags.LaunchDarkly`, `Encina.FeatureFlags.OpenFeature`
    - Priority: MEDIUM - Progressive deployment enabler
  - **Encina.Secrets** (Issue #452) - Secrets management abstraction
    - `ISecretsProvider` abstraction
    - `[Secret("key")]` attribute for DI injection
    - Secret rotation monitoring (optional)
    - Providers: Azure Key Vault, AWS Secrets Manager, HashiCorp Vault
    - New packages planned: `Encina.Secrets`, `Encina.Secrets.AzureKeyVault`, `Encina.Secrets.AwsSecretsManager`, `Encina.Secrets.HashiCorpVault`
    - Priority: MEDIUM - Security best practice
  - **Encina.ServiceDiscovery** (Issue #453) - Service discovery abstraction
    - `IServiceDiscoveryProvider` abstraction
    - Load balancing strategies: RoundRobin, Random, LeastConnections
    - `IEncina.SendToService<>()` extension methods
    - Providers: Kubernetes DNS, Consul, Aspire
    - New packages planned: `Encina.ServiceDiscovery`, `Encina.ServiceDiscovery.Kubernetes`, `Encina.ServiceDiscovery.Consul`
    - Priority: MEDIUM - Microservices fundamental
  - **Encina.HealthChecks** (Issue #454) - Kubernetes health probes
    - `OutboxHealthCheck`, `InboxHealthCheck`, `SagaHealthCheck`, `HandlerHealthCheck`
    - Separate endpoints: `/health/live`, `/health/ready`, `/health/startup`
    - Integration with `Microsoft.Extensions.Diagnostics.HealthChecks`
    - New package planned: `Encina.HealthChecks`
    - Priority: MEDIUM - Kubernetes deployment essential
  - **Encina.GracefulShutdown** (Issue #455) - Kubernetes graceful termination
    - `IInFlightRequestTracker` for active request tracking
    - `InFlightTrackingBehavior` pipeline behavior
    - Pre-stop delay for LB drain
    - Outbox flush before shutdown
    - New package planned: `Encina.GracefulShutdown`
    - Priority: MEDIUM - K8s reliability essential
  - **Encina.MultiTenancy** (Issue #456) - Multi-tenancy for SaaS
    - Tenant resolution: Header, Subdomain, Route, Claim, Custom
    - Data isolation: Row, Schema, Database strategies
    - `TenantAwareOutboxStore`, `TenantAwareSagaStore`, `TenantAwareInboxStore`
    - GDPR data residency support
    - New package planned: `Encina.MultiTenancy`
    - Priority: MEDIUM - SaaS market enabler
  - **Encina.CDC** (Issue #457) - Change Data Capture for Outbox
    - `ICdcProvider` abstraction for change streaming
    - CDC Orchestrator hosted service
    - Providers: SQL Server CDC, PostgreSQL Logical Replication, Debezium
    - Near real-time message capture, minimal database load
    - New packages planned: `Encina.CDC`, `Encina.CDC.SqlServer`, `Encina.CDC.PostgreSQL`
    - Priority: LOW - Alternative to polling, high complexity
  - **Encina.ApiVersioning** (Issue #458) - Handler versioning support
    - `[ApiVersion("1.0")]` attribute for handlers
    - Version resolution: Header, Query, Path, MediaType
    - Deprecation support with Sunset header
    - Version discovery endpoint
    - New package planned: `Encina.ApiVersioning`
    - Priority: LOW - API evolution support
  - **Encina.Orleans** (Issue #459) - Orleans virtual actors integration
    - `IGrainHandler<,>` interface
    - Orleans-based request dispatcher
    - Saga grains with Orleans state and reminders
    - Scheduling via grain timers
    - New package planned: `Encina.Orleans`
    - Priority: LOW - High-concurrency niche

#### Microservices Architecture Patterns Issues (12 new features planned based on December 29, 2025 research)

- **Service Discovery & Configuration Management** (Issue #382) - Foundational microservices pattern
    - `IServiceDiscovery` with `ResolveAsync`, `RegisterAsync`, `DeregisterAsync`, `WatchAsync`
    - `IConfigurationProvider` for externalized configuration
    - Multiple backends: Consul, Kubernetes DNS, .NET Aspire
    - New packages planned: `Encina.ServiceDiscovery`, `Encina.ServiceDiscovery.Consul`, `Encina.ServiceDiscovery.Kubernetes`, `Encina.ServiceDiscovery.Aspire`
    - Priority: CRITICAL - Fundamental pattern missing from Encina
  - **API Gateway / Backends for Frontends (BFF)** (Issue #383) - Essential modern architecture pattern
    - `IBffRequestAdapter` for proxying with Encina pipeline
    - `IResponseAggregator<T>` for combining multiple service responses
    - `[BffRoute]`, `[AggregateFrom]` declarative attributes
    - Microsoft YARP integration
    - New packages planned: `Encina.BFF`, `Encina.BFF.YARP`, `Encina.BFF.Aggregation`
    - Priority: CRITICAL - Very high demand in 2025
  - **Domain Events vs Integration Events Separation** (Issue #384) - DDD best practice
    - `IDomainEvent` for in-process bounded context events
    - `IIntegrationEvent` with `EventId`, `SourceService`, `Version`
    - `IIntegrationEventMapper<TDomain, TIntegration>` for automatic translation
    - `DomainToIntegrationEventBehavior` for Outbox auto-publishing
    - Priority: CRITICAL - Inspired by MassTransit, NServiceBus
  - **Multi-Tenancy Support** (Issue #385) - SaaS-critical pattern
    - `ITenantContext` with `CurrentTenant`, `IsolationLevel`
    - `TenantIsolationLevel`: SharedSchema, SeparateSchema, SeparateDatabase
    - `ITenantResolver` with Header, Claims, Subdomain, Route resolvers
    - `TenantFilteringPipelineBehavior` for automatic query filtering
    - EF Core integration with automatic tenant filtering
    - New packages planned: `Encina.MultiTenancy`, `Encina.MultiTenancy.EntityFrameworkCore`
    - Priority: HIGH - Essential for SaaS applications
  - **Anti-Corruption Layer (ACL)** (Issue #386) - Legacy integration pattern
    - `IAntiCorruptionLayer<TExternal, TDomain>` bidirectional translation
    - `[AntiCorruptionLayer]` attribute for HTTP clients (Refit integration)
    - `AntiCorruptionPipelineBehavior` for automatic translation
    - New package planned: `Encina.AntiCorruption`
    - Priority: HIGH - Essential for brownfield development
  - **Dapr Integration** (Issue #387) - CNCF graduated project integration
    - Dapr State Store as Outbox, Inbox, Saga store backend
    - Dapr Pub/Sub as message transport
    - Dapr Workflows as Saga backend alternative
    - Dapr Actors integration
    - New packages planned: `Encina.Dapr`, `Encina.Dapr.StateStore`, `Encina.Dapr.PubSub`
    - **Re-planned**: Previously deprecated, now restored due to CNCF graduation and high community demand
    - Priority: HIGH - Leading microservices framework 2025
  - **Virtual Actors (Orleans Integration)** (Issue #388) - High concurrency pattern
    - `IEncinaActor` and `IEncinaActor<TState>` abstractions
    - `EncinaGrain<TState>` base class for Orleans Grains
    - Full Encina pipeline support within actors
    - New packages planned: `Encina.Actors`, `Encina.Orleans`
    - Priority: MEDIUM - Gaming, IoT, high-concurrency use cases
  - **API Versioning Pipeline Behavior** (Issue #389) - API evolution support
    - `[ApiVersion]` attribute with deprecation support
    - `IApiVersionContext` with `CurrentVersion`, `IsDeprecated`
    - `IApiVersionResolver` with Header, QueryString, URL, MediaType resolvers
    - `ApiVersioningPipelineBehavior` for version-aware handler routing
    - Priority: MEDIUM - Important for production API evolution
  - **Enhanced Message Deduplication** (Issue #390) - Inbox pattern improvement
    - `IDeduplicationKeyGenerator<T>` for content-based keys
    - `SlidingWindowDeduplicationOptions` with Window, Strategy, Cleanup
    - `DeduplicationStrategy`: RejectSilently, ReturnCachedResult, ReturnError
    - `[Deduplicate]` declarative attribute
    - Priority: MEDIUM - Improves existing Inbox functionality
  - **Sidecar/Ambassador Pattern Support** (Issue #391) - Kubernetes-native pattern
    - `ISidecarProxy` for Encina as sidecar process
    - `IAmbassadorProxy` for client connectivity offloading
    - `EncinaSidecarHost` BackgroundService
    - Kubernetes deployment examples, Docker images
    - New package planned: `Encina.Sidecar`
    - Priority: MEDIUM - Important for containerized deployments
  - **Event Collaboration / Process Manager** (Issue #392) - Hybrid orchestration pattern
    - `IProcessManager<TState>` with `HandleEventAsync`, `GetAuditTrailAsync`
    - `ProcessManagerBase<TState>` base class
    - `[CorrelateBy]` attribute for event routing
    - `ProcessManagerRoutingBehavior` for automatic event dispatch
    - Dashboard/visibility queries
    - Priority: MEDIUM - Hybrid choreography/orchestration with visibility
  - **Eventual Consistency Helpers** (Issue #393) - Distributed systems helpers
    - `IEventualConsistencyMonitor` with `CheckAsync`, `WaitForConsistencyAsync`
    - `IConflictResolver<TState>` with LastWriteWins, Merge, ManualResolution strategies
    - `[EventuallyConsistent]` attribute with `MaxLagMs`, `WaitForConsistency`
    - `IReadYourWritesGuarantee` for session-level consistency
    - Priority: LOW - Nice-to-have for complex systems

- New labels created for Microservices patterns:
  - `area-service-discovery` - Service discovery and registry patterns
  - `area-configuration` - Configuration management and externalization
  - `orleans-integration` - Microsoft Orleans integration
  - `dapr-integration` - Dapr runtime integration
  - `yarp-integration` - Microsoft YARP reverse proxy integration
  - `consul-integration` - HashiCorp Consul integration
  - `aspire-integration` - .NET Aspire integration
  - `kubernetes-native` - Kubernetes-native patterns and deployment
  - `pattern-sidecar` - Sidecar and Ambassador patterns
  - `pattern-bff` - Backend for Frontend pattern

#### Security Patterns Issues (8 new features planned based on December 29, 2025 research)

- **Core Security Abstractions** (Issue #394) - Foundational security pattern
    - `ISecurityContext` with CurrentPrincipal, Permissions, Roles, Claims
    - `IPermissionEvaluator<TResource>` for dynamic permission evaluation
    - `SecurityPipelineBehavior` with `[Authorize]`, `[RequirePermission]`, `[RequireRole]` attributes
    - RBAC, ABAC, Permission-based authorization support
    - New package planned: `Encina.Security`
    - Priority: CRITICAL - Foundation for all security patterns
  - **Audit Trail Logging** (Issue #395) - Compliance-ready audit logging
    - `IAuditLogger` with who/what/when/where tracking
    - `AuditPipelineBehavior` for automatic capture
    - `[Auditable]` attribute with None, Minimal, Standard, Detailed levels
    - Storage backends: Database, Elasticsearch, Azure Table Storage, CloudWatch
    - Sensitive data redaction
    - New package planned: `Encina.Security.Audit`
    - Priority: CRITICAL - Required for SOX, HIPAA, GDPR, PCI compliance
  - **Field-Level Encryption** (Issue #396) - Data protection at rest
    - `IFieldEncryptor` with encrypt/decrypt/rotate key operations
    - `[Encrypt]` attribute for sensitive properties
    - `EncryptionPipelineBehavior` for automatic encrypt/decrypt
    - Key rotation with versioning, Azure Key Vault/AWS KMS integration
    - Crypto-shredding for GDPR (delete key = "forget" data)
    - New package planned: `Encina.Security.Encryption`
    - Priority: HIGH - PCI-DSS, GDPR sensitive data protection
  - **PII Masking** (Issue #397) - Personal data protection
    - `IPIIMasker` with mask/unmask/detect operations
    - `[PII]` attribute with Email, Phone, SSN, CreditCard, Address types
    - `PIIMaskingPipelineBehavior` for automatic response masking
    - Auto-detection, logging redaction
    - New package planned: `Encina.Security.PII`
    - Priority: HIGH - GDPR essential
  - **Anti-Tampering** (Issue #398) - Request integrity verification
    - `IRequestSigner` with HMAC-SHA256/512, RSA-SHA256, ECDSA
    - `[SignedRequest]` attribute for handlers requiring verification
    - `SignatureVerificationPipelineBehavior`
    - Timestamp validation, nonce management for replay attack prevention
    - New package planned: `Encina.Security.AntiTampering`
    - Priority: HIGH - API security for webhooks, inter-service communication
  - **Input Sanitization** (Issue #399) - OWASP Top 10 prevention
    - `ISanitizer<T>` with sanitize/validate operations
    - `[Sanitize]` attribute with Html, Sql, Command, Path, Url types
    - `SanitizationPipelineBehavior` for automatic input cleaning
    - XSS, SQL injection, command injection, path traversal prevention
    - New package planned: `Encina.Security.Sanitization`
    - Priority: HIGH - OWASP Top 10 prevention
  - **Secrets Management** (Issue #400) - Cloud-native secrets handling
    - `ISecretProvider` with get/set/rotate operations
    - `SecretProviderChain` for fallback between providers
    - Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, GCP Secret Manager integration
    - Automatic rotation, TTL caching, access auditing
    - New packages planned: `Encina.Security.Secrets.*`
    - Priority: MEDIUM - Cloud-native best practice
  - **ABAC Engine** (Issue #401) - Advanced authorization
    - `IAbacEngine` with evaluate(subject, resource, action, environment)
    - Policy DSL for access policies
    - `AbacPipelineBehavior` for automatic evaluation
    - PDP/PEP pattern, OPA integration
    - New package planned: `Encina.Security.ABAC`
    - Priority: MEDIUM - Complex enterprise authorization

- New labels created for Security patterns:
  - `area-security` - Security patterns and authentication/authorization
  - `owasp-pattern` - Based on OWASP security best practices

- Compliance Patterns Issues - GDPR & EU Laws (14 new features planned based on December 29, 2025 research):
  - **GDPR Core Abstractions** (Issue #402) - Foundation for EU compliance
    - `IDataController`, `IDataProcessor` interfaces
    - `RoPARegistry` (Record of Processing Activities) - Art. 30
    - `GDPRCompliancePipelineBehavior` for automatic validation
    - New package planned: `Encina.Compliance.GDPR`
    - Priority: CRITICAL - Mandatory for EU operations
  - **Consent Management** (Issue #403) - Art. 7 compliance
    - `IConsentManager` with request/grant/withdraw/check operations
    - `[RequireConsent("purpose")]` attribute
    - Consent versioning, proof of consent, granular purposes
    - New package planned: `Encina.Compliance.Consent`
    - Priority: CRITICAL - Art. 7 requirement
  - **Data Subject Rights** (Issue #404) - Arts. 15-22 implementation
    - `IDataSubjectRightsService` for all GDPR rights
    - Right of Access (Art. 15), Rectification (Art. 16), Erasure (Art. 17)
    - Right to Portability (Art. 20), Restriction (Art. 18), Object (Art. 21)
    - Request tracking, 30-day SLA monitoring
    - New package planned: `Encina.Compliance.DataSubjectRights`
    - Priority: CRITICAL - Fundamental GDPR rights
  - **Data Residency** (Issue #405) - Data sovereignty enforcement
    - `IDataResidencyEnforcer` with geo-routing
    - `[DataResidency("EU")]` attribute
    - Multi-region database routing, SCCs validation
    - Cloud provider region mapping (AWS, Azure, GCP)
    - New package planned: `Encina.Compliance.DataResidency`
    - Priority: CRITICAL - Post-Schrems II requirement
  - **Data Retention** (Issue #406) - Storage limitation (Art. 5(1)(e))
    - `IRetentionPolicyEngine` with automatic deletion
    - Legal hold support, retention reporting
    - New package planned: `Encina.Compliance.Retention`
    - Priority: HIGH
  - **Anonymization** (Issue #407) - Art. 4(5) pseudonymization
    - `IAnonymizer` with k-anonymity, l-diversity, differential privacy
    - Crypto-shredding: delete key = data "forgotten"
    - New package planned: `Encina.Compliance.Anonymization`
    - Priority: HIGH
  - **Breach Notification** (Issue #408) - 72-hour notification (Arts. 33-34)
    - `IBreachNotificationService` with detection/assessment/notification
    - SIEM integration (Splunk, Azure Sentinel)
    - New package planned: `Encina.Compliance.BreachNotification`
    - Priority: HIGH
  - **DPIA Automation** (Issue #409) - Art. 35 impact assessment
    - `IDPIAService` with risk assessment and report generation
    - New package planned: `Encina.Compliance.DPIA`
    - Priority: MEDIUM
  - **Processor Agreements** (Issue #410) - Art. 28 compliance
    - `IProcessorAgreementService` for DPA management
    - New package planned: `Encina.Compliance.ProcessorAgreements`
    - Priority: MEDIUM
  - **Privacy by Design** (Issue #411) - Art. 25 enforcement
    - `IPrivacyByDesignValidator`, Roslyn analyzer
    - New package planned: `Encina.Compliance.PrivacyByDesign`
    - Priority: MEDIUM
  - **Cross-Border Transfer** (Issue #412) - Chapter V compliance
    - `ICrossBorderTransferValidator` with SCCs, BCRs, TIA
    - New package planned: `Encina.Compliance.CrossBorderTransfer`
    - Priority: MEDIUM
  - **Lawful Basis** (Issue #413) - Art. 6 tracking
    - `ILawfulBasisService` for processing validation
    - New package planned: `Encina.Compliance.LawfulBasis`
    - Priority: MEDIUM
  - **NIS2 Directive** (Issue #414) - EU 2022/2555 cybersecurity
    - `INIS2ComplianceService` for incident reporting
    - New package planned: `Encina.Compliance.NIS2`
    - Priority: MEDIUM
  - **EU AI Act** (Issue #415) - EU 2024/1689 AI governance
    - `IAIActComplianceService` for risk classification
    - New package planned: `Encina.Compliance.AIAct`
    - Priority: MEDIUM

- New labels created for Compliance patterns:
  - `area-compliance` - Regulatory compliance patterns (GDPR, NIS2, AI Act)
  - `eu-regulation` - Related to European Union regulations
  - `pattern-data-masking` - Data masking and PII protection
  - `pattern-consent-management` - Consent management and tracking
  - `pattern-data-sovereignty` - Data residency and sovereignty
  - `area-data-protection` - Data protection and privacy features

#### Advanced Validation Patterns Issues (10 new features planned based on December 2025 research)

- **Source-Generated Validation** (Issue #227) - Compile-time validation code generation
    - Zero reflection, NativeAOT and trimming compatible
    - ~1.6x faster, ~4.7x less memory (Validot benchmarks)
    - Attributes: `[GenerateValidation]`, `[NotEmpty]`, `[Email]`, `[Positive]`, etc.
    - New package planned: `Encina.Validation.Generators`
    - Inspired by Validot, Microsoft Options Validation Source Generator
  - **Domain/Value Object Validation** (Issue #228) - Always-Valid Domain Model
    - Value Objects with built-in validation
    - Factory methods returning `Either<EncinaError, T>` for ROP
    - Base classes: `ValueObject<TSelf>`, `SingleValueObject<TSelf, TValue>`
    - Common value objects: `Email`, `PhoneNumber`, `Url`, `Money`, `NonEmptyString`
    - New package planned: `Encina.Validation.Domain`
    - Inspired by Enterprise Craftsmanship, Milan Jovanovic's DDD patterns
  - **Consolidate ValidationPipelineBehavior** (Issue #229) - Remove duplicate behaviors
    - CRITICAL technical debt: Each provider has its own duplicated behavior
    - Affected: FluentValidation, DataAnnotations, MiniValidator packages
    - Solution: Use centralized `ValidationPipelineBehavior` from core
    - Low effort, high impact cleanup
  - **Enhanced Async/Cross-Field Validation** (Issue #230) - Database-backed validation
    - Extensions: `MustExistAsync()`, `MustBeUniqueAsync()`, `GreaterThan(x => x.OtherProperty)`
    - Cross-field comparison validators
    - Conditional validation with `WhenAsync()`, `UnlessAsync()`
  - **OpenAPI Schema Validation** (Issue #231) - Contract-first validation
    - Automatic validation against OpenAPI 3.1 schemas
    - Request/Response validation
    - Prevents API drift between contract and implementation
    - New package planned: `Encina.Validation.OpenApi`
    - Inspired by Zuplo, openVALIDATION
  - **Security-Focused Validation** (Issue #232) - OWASP-compliant validation
    - Prevents >90% of injection attacks (OWASP statistics)
    - Allowlist validators: `AllowlistPattern()`, `AllowlistValues()`
    - Injection prevention: `NoSqlInjection()`, `NoXss()`, `NoCommandInjection()`
    - Sanitizers: `SanitizeHtml()`, `StripHtml()`, `EncodeHtml()`
    - New package planned: `Encina.Validation.Security`
    - Inspired by OWASP Input Validation Cheat Sheet, ASVS
  - **Validation Error Localization** (Issue #233) - Internationalization support
    - Integration with ASP.NET Core `IStringLocalizer`
    - Built-in translations for 12+ languages
    - Placeholder support: `{PropertyName}`, `{PropertyValue}`
  - **Validation Result Aggregation** (Issue #234) - Multi-source validation
    - `ValidationAggregator` builder for combining validators
    - Strategies: FailFast, CollectAll, ParallelCollectAll, ParallelFailFast
    - Error source tracking and deduplication
  - **Zod-like Schema Builder** (Issue #235) - TypeScript-inspired fluent API
    - Chainable schema definitions
    - Parse returns `Either<EncinaError, T>`
    - New package planned: `Encina.Validation.Schema`
    - Inspired by Zod (TypeScript), zod-rs (Rust)
  - **Two-Phase Validation Pattern** (Issue #236) - Pipeline + Domain separation
    - Phase 1 (Pipeline): Fast structural validation
    - Phase 2 (Handler): Domain validation with repository access
    - Interfaces: `IDomainValidator<TRequest>`, `IDomainValidatedRequest`
    - Best practice for clean CQRS architecture

- New label created: `area-source-generators` for source generator-related features

#### Advanced Event Sourcing Patterns Issues (13 new features planned based on December 2025 research)

- **Decider Pattern Support** (Issue #320) - Functional event sourcing with pure functions
    - `IDecider<TCommand, TEvent, TState>` interface with `Decide`, `Evolve`, `InitialState`
    - Pure functions = trivial testing without mocks
    - Industry best practice 2025 (Marten, Wolverine recommended)
    - Aligns with Encina's Railway Oriented Programming philosophy
  - **Causation/Correlation ID Tracking** (Issue #321) - Distributed tracing metadata
    - `EventMetadata` with MessageId, CausationId, CorrelationId
    - Automatic propagation from `IRequestContext`
    - 30-40% reduction in troubleshooting time (empirical studies)
    - Integration with `Encina.OpenTelemetry`
  - **Crypto-Shredding for GDPR** (Issue #322) - GDPR Article 17 compliance
    - New package planned: `Encina.Marten.GDPR`
    - `[PersonalData]` attribute for PII properties
    - `ICryptoShredder` with key vault integrations (HashiCorp, Azure, AWS)
    - Delete encryption key = data becomes unreadable ("forgotten")
    - **No .NET library offers first-class GDPR support** - competitive differentiator
  - **Advanced Snapshot Strategies** (Issue #323) - Beyond event-count based
    - `SnapshotStrategy` enum: EventCount, TimeInterval, BusinessBoundary, Composite
    - `ISnapshotBoundaryDetector` for custom boundaries
    - Per-aggregate configuration
  - **Blue-Green Projection Rebuild** (Issue #324) - Zero-downtime updates
    - `IProjectionRebuildManager` with progress tracking
    - Build projection in secondary schema, switch when caught up
    - CLI command: `encina projections rebuild <name>`
  - **Temporal Queries** (Issue #325) - Point-in-time state reconstruction
    - `LoadAtAsync(id, timestamp)` for historical state
    - `ITemporalEventStore` for range queries
    - 30% improvement in debugging time
  - **Multi-Tenancy Event Sourcing** (Issue #326) - SaaS tenant isolation
    - `MultiTenancyMode`: Conjoined, Dedicated, SchemaPerTenant
    - Automatic tenant filtering in repositories
    - `ITenantManager` for provisioning
  - **Event Archival and Compaction** (Issue #327) - Storage management at scale
    - Hot/warm/cold tiering with cloud storage (Azure Blob, S3)
    - Stream compaction strategies
    - Background archival service
  - **Bi-Temporal Modeling** (Issue #328) - Transaction + Valid time
    - `IBiTemporalEvent` with ValidTime and TransactionTime
    - Timeline visualization for audit
    - Use cases: Financial, insurance, HR
  - **Visual Event Stream Explorer** (Issue #329) - CLI debugging tool
    - `encina events list/show/replay/trace` commands
    - Projection status and lag monitoring
    - Rich terminal output with Spectre.Console
  - **Actor-Based Event Sourcing** (Issue #330) - Alternative pattern
    - `IEventSourcedActor<TState, TCommand, TEvent>` interface
    - Orleans/Akka-inspired concurrency model
    - Automatic lifecycle management
  - **EventQL Preconditions** (Issue #331) - Query-based constraints
    - `IAppendPrecondition` interface
    - Built-in: `StreamExists`, `NoEventOfType<T>`, `ExpectedVersion`
    - Composite with `Preconditions.All()`/`Any()`
  - **Tri-Temporal Modeling** (Issue #332) - Full audit trail
    - Transaction + Valid + Decision time
    - Use cases: Fraud detection, legal discovery

- New labels created for Event Sourcing patterns:
  - `area-event-sourcing` - Event Sourcing patterns and infrastructure
  - `area-gdpr` - GDPR compliance and data privacy
  - `pattern-decider` - Decider pattern for functional event sourcing
  - `pattern-crypto-shredding` - Crypto-shredding pattern for GDPR compliance
  - `pattern-blue-green` - Blue-Green deployment/rebuild pattern
  - `pattern-temporal-query` - Temporal queries and time-travel pattern
  - `pattern-snapshot` - Aggregate snapshotting pattern
  - `area-archival` - Event archival and cold storage patterns
  - `area-developer-experience` - Developer experience and tooling improvements
  - `marten-integration` - Marten library integration

#### Advanced CQRS Patterns Issues (12 new features planned based on December 2025 market research)

- **Zero-Interface Handlers** (Issue #333) - Convention-based handler discovery
    - Handlers discovered by naming convention, no `IRequestHandler<,>` required
    - Static handlers supported (`public static class CreateOrderHandler`)
    - Reduces boilerplate and improves DDD alignment
    - Inspired by Wolverine's convention-based approach
    - Depends on #50 (Source Generators) for NativeAOT compatibility
  - **Idempotency Pipeline Behavior** (Issue #334) - Lightweight deduplication
    - `IIdempotencyStore` interface for key tracking
    - `IdempotencyPipelineBehavior<,>` for automatic verification
    - Cache-based storage (Redis, In-memory) for lightweight dedup
    - Complements existing Inbox pattern for simpler use cases (APIs, webhooks)
    - Inspired by MassTransit and Stripe patterns
  - **Request Timeout Behavior** (Issue #335) - Per-request timeouts
    - `[Timeout(Seconds = 30)]` attribute for declarative configuration
    - `IHasTimeout` interface for programmatic configuration
    - Fallback strategies: ThrowException, ReturnDefault, ReturnCached
    - OpenTelemetry integration for timeout events
    - Inspired by Brighter's timeout middleware
  - **Cursor-Based Pagination** (Issue #336) - O(1) pagination helpers
    - `ICursorPaginatedQuery<T>` interface
    - `CursorPaginatedResult<T>` with NextCursor, HasNextPage
    - `ToCursorPaginatedAsync()` EF Core extension
    - O(1) performance vs O(n) for offset pagination
    - Inspired by GraphQL Cursor Connections specification
  - **Request Versioning** (Issue #337) - Command/query upcasting
    - `[RequestVersion(1)]` attribute for versioning
    - `IRequestUpcaster<TFrom, TTo>` for automatic migration
    - Version chains: V1 → V2 → V3 automatic upcasting
    - Deprecation logging and metrics
    - Inspired by Axon Framework's event upcasting
  - **Multi-Tenant Context Middleware** (Issue #338) - Tenant isolation
    - `ITenantResolver` with built-in implementations (Header, Subdomain, Claims, Route)
    - `TenantValidationBehavior` for tenant validation
    - `TenantIsolationBehavior` for isolation enforcement
    - EF Core global query filter integration
    - Labels: `area-multitenancy`, `area-security`, `industry-best-practice`
  - **Batch Command Processing** (Issue #339) - Atomic batch operations
    - `IBatchCommand<TCommand, TResponse>` interface
    - Strategies: AllOrNothing, PartialSuccess, StopOnFirstError, ContinueOnError
    - Batch deduplication and parallel processing options
    - Inspired by MassTransit batch consumers
  - **Request Enrichment** (Issue #340) - Auto-populate from context
    - `[EnrichFrom(ContextProperty.UserId)]` for context properties
    - `[EnrichFromClaim("claim_name")]` for JWT claims
    - `IRequestEnricher<T>`, `IResponseEnricher<T>` interfaces
    - Reduces handler boilerplate
    - Inspired by Wolverine middleware
  - **Notification Fanout Strategies** (Issue #341) - Advanced delivery
    - New strategies: PriorityOrdered, Throttled, Quorum, FirstSuccessful
    - `[NotificationPriority]` attribute for ordering
    - Dead letter handling for failed notifications
    - Circuit breaker integration
  - **Request Composition** (Issue #342) - Combine multiple queries
    - `QueryComposer<TResult>` fluent builder
    - Parallel execution of independent queries
    - Dependency resolution for dependent queries
    - Reduces API chattiness
    - Inspired by GraphQL query composition
  - **Handler Discovery Analyzers** (Issue #343) - Compile-time validation
    - ENCINA001: Handler not registered
    - ENCINA002: Handler naming convention mismatch
    - ENCINA003: Query modifies state (anti-pattern)
    - ENCINA004: Response type mismatch
    - ENCINA005: Missing validator
    - Automatic code fixes
    - New package: `Encina.Analyzers`
  - **Progressive CQRS Adoption Guide** (Issue #344) - Documentation
    - Decision tree: when to use/not use CQRS
    - Adoption levels (0-4)
    - Anti-patterns to avoid
    - Vertical Slice Architecture integration

- New labels created for Advanced CQRS patterns:
  - `wolverine-inspired` - Pattern inspired by Wolverine library
  - `brighter-inspired` - Pattern inspired by Brighter library
  - `axon-inspired` - Pattern inspired by Axon Framework (Java)
  - `graphql-inspired` - Pattern inspired by GraphQL ecosystem
  - `pattern-batch-processing` - Batch processing and bulk command pattern
  - `pattern-request-composition` - Request composition and aggregation pattern

#### Domain Modeling Building Blocks Issues (15 new features planned based on December 29, 2025 DDD research)

- **Value Objects Base Class** (Issue #367) - Structural equality and immutability
    - `ValueObject<T>` abstract record with `GetEqualityComponents()`
    - ROP-compatible factory methods returning `Either<EncinaError, T>`
    - Prevents "primitive obsession" anti-pattern
    - Inspired by Vogen (~5M downloads demand)
    - New package planned: `Encina.DomainModeling`
    - Labels: `foundational`, `area-domain-modeling`, `aot-compatible`, `industry-best-practice`
  - **Rich Domain Events** (Issue #368) - Domain events with metadata
    - `DomainEvent` base record implementing `INotification`
    - Properties: EventId, OccurredAtUtc, EventVersion, CorrelationId, CausationId, AggregateId, AggregateVersion
    - Integrates with existing Encina pipeline and Marten event sourcing
    - Labels: `foundational`, `area-domain-modeling`, `area-messaging`, `area-observability`
  - **Entity Base Class** (Issue #369) - Identity equality for non-aggregates
    - `Entity<TId>` with identity-based equality
    - Separate from `AggregateRoot` for entities within aggregates
    - Labels: `foundational`, `area-domain-modeling`, `aot-compatible`
  - **Provider-Agnostic AggregateRoot** (Issue #370) - State-based persistence support
    - `AggregateRoot<TId>` extending `Entity<TId>` with domain event collection
    - `RaiseDomainEvent()`, `ClearDomainEvents()`, `CheckRule()` methods
    - Works with EF Core, Dapper (not just Marten event sourcing)
    - Labels: `foundational`, `area-domain-modeling`, `area-messaging`
  - **Specification Pattern** (Issue #371) - Composable query specifications
    - `Specification<T>` abstract class with `ToExpression()`
    - `And()`, `Or()`, `Not()` composition operators
    - New packages: `Encina.Specifications`, `Encina.Specifications.EntityFrameworkCore`
    - Inspired by Ardalis.Specification (~7M downloads)
    - Labels: `new-package`, `area-specifications`, `industry-best-practice`
  - **Business Rules Validation** (Issue #372) - Domain invariant validation
    - `IBusinessRule` interface with `ErrorCode`, `ErrorMessage`, `IsSatisfied()`
    - ROP extension: `Check()` returning `Either<EncinaError, Unit>`
    - Separate from input validation (FluentValidation, DataAnnotations)
    - Labels: `foundational`, `area-domain-modeling`, `area-validation`
  - **Integration Events** (Issue #373) - Cross-bounded-context events
    - `IntegrationEvent` base record with schema versioning
    - `IDomainToIntegrationEventMapper<TDomain, TIntegration>` interface
    - Bounded context isolation pattern
    - Labels: `area-domain-modeling`, `area-modular-monolith`, `area-microservices`
  - **Strongly Typed IDs** (Issue #374) - Type-safe entity identifiers
    - `StronglyTypedId<TValue>` base record
    - Convenience classes: `GuidId`, `IntId`, `LongId`, `StringId`
    - Prevents mixing `OrderId` with `CustomerId` at compile-time
    - Inspired by StronglyTypedId (~3M downloads)
    - Labels: `foundational`, `area-domain-modeling`, `aot-compatible`
  - **Soft Delete Pattern** (Issue #375) - Logical deletion with auto-filtering
    - `ISoftDeletable` interface with `IsDeleted`, `DeletedAtUtc`, `DeletedBy`
    - EF Core global query filter extension
    - Pipeline behavior for query filtering
    - Labels: `area-auditing`, `area-domain-modeling`, `area-gdpr`, `area-compliance`
  - **Auditing Pattern** (Issue #376) - Created/Modified tracking
    - `IAudited` interface with CreatedAtUtc/By, LastModifiedAtUtc/By
    - EF Core SaveChanges interceptor for auto-population
    - Uses `IRequestContext.UserId`
    - Labels: `area-auditing`, `area-domain-modeling`, `area-compliance`, `area-security`
  - **Domain Service Marker** (Issue #377) - Semantic interface for domain services
    - `IDomainService` marker interface
    - Auto-registration extension method
    - Labels: `area-domain-modeling`, `aot-compatible`
  - **Anti-Corruption Layer** (Issue #378) - External system isolation
    - `IAntiCorruptionTranslator<TExternal, TDomain>` interface
    - `IBidirectionalTranslator<,>` for two-way translation
    - Labels: `area-domain-modeling`, `area-integration`, `area-microservices`
  - **Bounded Context Helpers** (Issue #379) - Context mapping
    - `BoundedContext` base class with `Configure()`
    - `ContextMap` for documenting context relationships
    - Integration with existing `IModule` system
    - Labels: `area-domain-modeling`, `area-modular-monolith`, `area-architecture-testing`
  - **Generic Repository** (Issue #380) - Provider-agnostic repository abstraction
    - `IRepository<TAggregate, TId>` with CRUD operations
    - `IRepositoryWithSpecification<,>` for specification queries
    - Note: Controversial pattern - many prefer DbContext directly
    - Labels: `area-domain-modeling`, `area-specifications`
  - **Domain DSL Helpers** (Issue #381) - Fluent domain builders
    - `AggregateBuilder<TAggregate, TId, TBuilder>` with rule validation
    - Fluent extensions for ubiquitous language
    - Common domain types: `Quantity`, `Percentage`, `DateRange`
    - Labels: `area-domain-modeling`, `aot-compatible`

- New labels created for Domain Modeling patterns:
  - `area-domain-modeling` - Domain modeling building blocks (Entities, Value Objects, Aggregates)
  - `area-specifications` - Specification pattern for composable queries
  - `area-auditing` - Auditing, change tracking, and soft delete patterns (already existed)
  - `foundational` - Core building block that other features depend on

#### Vertical Slice Architecture Patterns Issues (12 new features planned based on December 29, 2025 research)

- **Feature Flags Integration** (Issue #345) - Microsoft.FeatureManagement integration
    - `[FeatureFlag("NewCheckoutFlow")]` attribute for handlers
    - `FeatureFlagPipelineBehavior<,>` for automatic verification
    - Built-in filters: Percentage, TimeWindow, Targeting, Contextual
    - New package planned: `Encina.FeatureFlags`
    - Labels: `area-feature-flags`, `saas-essential`, `industry-best-practice`, `aot-compatible`
  - **Multi-Tenancy Support** (Issue #346) - Comprehensive SaaS multi-tenant patterns
    - `ITenantResolver` with implementations (Header, Subdomain, Claim, Route, QueryString)
    - `TenantResolutionPipelineBehavior` and `TenantIsolationPipelineBehavior`
    - Database strategies: DatabasePerTenant, SchemaPerTenant, SharedDatabase
    - EF Core query filter integration
    - New packages planned: `Encina.MultiTenancy`, `Encina.MultiTenancy.EntityFrameworkCore`
    - Labels: `area-multi-tenancy`, `saas-essential`, `area-security`
  - **Specification Pattern Integration** (Issue #347) - Composable query specifications
    - `Specification<T>` base class with `And()`, `Or()`, `Not()` composition
    - `QuerySpecification<T>` with includes, ordering, paging
    - `ISpecificationEvaluator<T>` for EF Core and Dapper
    - New packages planned: `Encina.Specification`, `Encina.Specification.EntityFrameworkCore`
    - Inspired by Ardalis.Specification
  - **API Versioning Integration** (Issue #348) - Handler-level API versioning
    - `[ApiVersion("1.0")]` attribute for versioned handlers
    - `VersionedRequestDispatcher` for automatic routing
    - Deprecation headers: Deprecation, Sunset, Link
    - New package planned: `Encina.AspNetCore.Versioning`
  - **Request Batching / Bulk Operations** (Issue #349) - Batch processing for DDD
    - `BatchCommand<TCommand, TResponse>` and `BatchQuery<,>` wrappers
    - Strategies: AllOrNothing, PartialSuccess, StopOnFirstError, ParallelAll
    - Fluent `BatchBuilder<,>` API
    - New package planned: `Encina.Batching`
  - **Domain Events vs Integration Events** (Issue #350) - Clear separation (DDD best practice)
    - `IDomainEvent` (in-process) vs `IIntegrationEvent` (cross-boundary)
    - `IDomainEventDispatcher` with configurable timing (BeforeCommit, AfterCommit)
    - `IIntegrationEventPublisher` using Outbox pattern
    - `AggregateRoot<TId>` base class with domain event collection
    - EF Core `DomainEventDispatchingInterceptor`
  - **Audit Trail Pipeline Behavior** (Issue #351) - Compliance and auditing
    - `[Auditable]`, `[NotAuditable]` attributes
    - `IAuditStore` with EF Core, Dapper, Elasticsearch implementations
    - Sensitive data redaction with `ISensitiveDataRedactor`
    - New packages planned: `Encina.Auditing`, `Encina.Auditing.EntityFrameworkCore`
    - Labels: `area-auditing`, `area-gdpr`, `area-compliance`
  - **Modular Monolith Support** (Issue #352) - Architecture pattern 2025
    - `EncinaModule` base class with lifecycle hooks
    - `IModuleEventBus` for inter-module communication
    - `ModuleIsolationPipelineBehavior` for boundary enforcement
    - Database isolation strategies: SharedDatabase, SchemaPerModule
    - New package planned: `Encina.Modules`
    - Inspired by Milan Jovanovic, kgrzybek/modular-monolith-with-ddd
  - **CDC Integration** (Issue #353) - Change Data Capture patterns
    - `IChangeDataHandler<T>` for entity change handling
    - Providers: SQL Server CDC, PostgreSQL Logical Replication, Debezium/Kafka
    - New packages planned: `Encina.CDC`, `Encina.CDC.SqlServer`, `Encina.CDC.Debezium`
  - **Enhanced Streaming Support** (Issue #354) - IAsyncEnumerable improvements
    - `StreamCachingPipelineBehavior<,>` for caching streams
    - `StreamRateLimitingPipelineBehavior<,>` for per-item rate limiting
    - Backpressure strategies: Block, Drop, DropOldest, Error
    - Extension methods: `ToListAsync()`, `FirstOrDefaultAsync()`
  - **Enhanced Idempotency** (Issue #355) - Stripe-style idempotency keys
    - `[Idempotent]` attribute for handlers
    - `IIdempotencyStore` with TTL and distributed locking
    - ASP.NET Core middleware for X-Idempotency-Key header
    - Response caching with Idempotent-Replayed header
  - **Policy-Based Authorization Enhancement** (Issue #356) - Resource-based auth
    - `[AuthorizeRoles]`, `[AuthorizeClaim]` shortcuts
    - `[ResourceAuthorize(typeof(Order), "Edit")]` for resource-based auth
    - CQRS-aware default policies (Commands vs Queries)
    - Pre/Post authorization processors

- New labels created for Vertical Slice Architecture patterns:
  - `area-feature-flags` - Feature flags and feature toggles patterns
  - `area-authorization` - Authorization and access control patterns
  - `area-streaming` - Streaming and IAsyncEnumerable patterns
  - `area-batching` - Request batching and bulk operations
  - `area-specification` - Specification pattern for queries
  - `area-versioning` - API versioning patterns
  - `area-auditing` - Audit trail and logging patterns
  - `area-domain-events` - Domain events and integration events
  - `saas-essential` - Essential pattern for SaaS applications

#### Modular Monolith Architecture Patterns Issues (10 new features planned based on December 29, 2025 research)

- **Multi-Tenancy Support** (Issue #357) - Comprehensive SaaS multi-tenant patterns
    - `ITenantContext` with CurrentTenantId, CurrentTenantName, IsHost
    - `ITenantResolver` implementations: Header, Subdomain, QueryString, Route, Claim, Cookie
    - `DataIsolationLevel` enum: RowLevel, Schema, Database
    - `TenantContextPipelineBehavior` for automatic tenant propagation
    - EF Core integration with automatic `TenantQueryFilter`
    - New packages planned: `Encina.MultiTenancy`, `Encina.MultiTenancy.EntityFrameworkCore`, `Encina.MultiTenancy.AspNetCore`
    - Labels: `area-multitenancy`, `area-data-isolation`, `saas-enabler`, `cloud-azure`, `cloud-aws`
    - Inspired by ABP Framework, Milan Jovanović
  - **Inter-Module Communication** (Issue #358) - Integration Events pattern
    - `IDomainEvent : INotification` for in-process, synchronous events
    - `IIntegrationEvent : INotification` with EventId, OccurredAtUtc, SourceModule
    - `IIntegrationEventBus` for in-memory inter-module communication
    - `IModulePublicApi<TModule>` for module public contracts
    - Optional Outbox integration for reliability
    - Labels: `area-messaging`, `area-microservices`, `area-ddd`, `industry-best-practice`
    - Inspired by Microsoft Domain Events, Milan Jovanović
  - **Data Isolation per Module** (Issue #359) - Module boundary enforcement
    - `ModuleDataIsolation` enum: None, SeparateSchema, SeparateDatabase
    - `[ModuleSchema("orders")]` attribute for module schema declaration
    - `IModuleDbContext<TModule>` interface
    - Roslyn analyzer `ModuleDataIsolationAnalyzer` with rules:
      - ENC001: Cross-module DbContext access detected
      - ENC002: Direct table reference to another module's schema
      - ENC003: JOIN across module boundaries
    - Runtime query boundary enforcement
    - New packages planned: `Encina.Modular.Data`, `Encina.Modular.Data.Analyzers`
    - Labels: `area-data-isolation`, `roslyn-analyzer`, `area-architecture-testing`
    - Inspired by Milan Jovanović Data Isolation patterns
  - **Module Lifecycle Enhancement** (Issue #360) - Orleans/NestJS-inspired module system
    - Automatic module discovery: `DiscoverModulesFromAssemblies()`, `DiscoverModulesFromPattern()`
    - `[DependsOn(typeof(OtherModule))]` for dependency declaration
    - Topological sort for startup order
    - Expanded lifecycle hooks: OnModulePreConfigure, Configure, PostConfigure, Initialize, Started, Stopping, Stopped
    - Module exports (NestJS-inspired): `public override Type[] Exports`
    - `ModuleGraph` for visualization/debugging
    - Labels: `area-module-system`, `area-pipeline`, `industry-best-practice`
    - Inspired by Orleans Lifecycle, NestJS Modules
  - **Feature Flags Integration** (Issue #361) - Microsoft.FeatureManagement integration
    - `[FeatureGate("FeatureName")]` attribute for handlers
    - `FeatureGatePipelineBehavior` with short-circuit on disabled feature
    - `[FallbackHandler]` for fallback when feature is off
    - Per-tenant feature flags with `[TenantFeatureFilter]`
    - Azure App Configuration support
    - New packages planned: `Encina.FeatureManagement`, `Encina.FeatureManagement.AspNetCore`
    - Labels: `area-feature-flags`, `saas-enabler`, `cloud-azure`
  - **Module Testing Utilities** (Issue #362) - Encina.Testing extensions
    - `ModuleTestBase<TModule>` base class for isolated module testing
    - `WithMockedModule<TApi>()` for mocking module dependencies
    - `IntegrationEvents.ShouldContain<TEvent>()` assertions
    - `ModuleArchitecture.Analyze()` for architecture testing
    - `ModuleDataArchitecture` for data isolation tests
    - Given/When/Then helpers for saga testing
    - Labels: `area-testing`, `area-architecture-testing`, `testing-integration`
    - Inspired by ArchUnitNET, NetArchTest
  - **Anti-Corruption Layer Support** (Issue #363) - DDD pattern
    - `IAntiCorruptionLayer<TExternal, TInternal>` interface
    - `IAsyncAntiCorruptionLayer<,>` for complex async translations
    - `[ModuleAdapter(From, To)]` for inter-module adapters
    - `[ExternalSystemAdapter("LegacyERP")]` for external systems
    - `AntiCorruptionPipelineBehavior` for automatic translation
    - Auto-discovery of adapters
    - Labels: `area-acl`, `area-ddd`, `area-interop`, `area-microservices`
    - Inspired by Azure ACL Pattern
  - **Module Health & Readiness** (Issue #364) - Cloud-native module health
    - `IModuleHealthCheck` for per-module health checks
    - `IModuleReadinessCheck` for readiness probes
    - `ModuleHealthCheckBase` abstract class
    - Dependency-aware health propagation
    - ASP.NET Core integration: `AddEncinaModuleHealthChecks()`
    - Per-module endpoints: `/health/{moduleName}`
    - Labels: `area-health-checks`, `area-cloud-native`, `cloud-aws`, `cloud-azure`
  - **Vertical Slice Architecture Support** (Issue #365) - VSA formalization
    - `[VerticalSlice("Orders/PlaceOrder")]` attribute
    - `[SlicePipeline(...)]` for slice-scoped behaviors
    - Feature folder convention: `Features/{Domain}/{Slice}/`
    - CLI generator: `encina generate slice`
    - `SliceTestBase<TSlice>` for isolated testing
    - Labels: `area-vertical-slice`, `area-cli`, `industry-best-practice`
    - Inspired by Jimmy Bogard VSA
  - **Module Versioning** (Issue #366) - API evolution for modules
    - `[ModuleVersion("2.0")]` attribute
    - `[ModuleApiVersion]` for versioned public APIs
    - `[Deprecated("message", RemovalVersion = "3.0")]` with warnings
    - `IModuleVersionAdapter<TFrom, TTo>` for version bridging
    - Roslyn analyzer for deprecated API usage (ENC010)
    - Version compatibility validation at startup
    - Labels: `area-versioning`, `roslyn-analyzer`, `area-openapi`

- New labels created for Modular Monolith patterns:
  - `area-modular-monolith` - Modular Monolith architecture patterns
  - `area-data-isolation` - Data isolation and schema separation
  - `area-acl` - Anti-Corruption Layer patterns
  - `area-vertical-slice` - Vertical Slice Architecture patterns
  - `area-module-system` - Module system, lifecycle, and discovery
  - `saas-enabler` - Enables SaaS application development
  - `roslyn-analyzer` - Requires Roslyn analyzer implementation

#### Advanced Messaging Patterns Issues (15 new features planned based on market research)

- **Message Batching** (Issue #121) - Process multiple messages in a single handler invocation
    - Inspired by Wolverine 4.0's batch handler support
    - Time-based, count-based, and size-based batching modes
    - Integration with Outbox/Inbox patterns
  - **Claim Check Pattern** (Issue #122) - External storage for large message payloads
    - Store large payloads in Azure Blob, S3, or FileSystem
    - Pass only reference through message broker
    - Reduces messaging costs and improves throughput
    - New packages planned: `Encina.ClaimCheck`, `Encina.ClaimCheck.AzureBlob`, `Encina.ClaimCheck.AmazonS3`
  - **Message Priority** (Issue #123) - Priority-based message processing
    - Process high-priority messages before lower-priority ones
    - Anti-starvation mechanisms for fairness
  - **Enhanced Deduplication** (Issue #124) - Multiple deduplication strategies
    - MessageId, ContentHash, TimeWindow, IdempotencyKey strategies
    - Extends current Inbox pattern capabilities
  - **Multi-Tenancy Messaging** (Issue #125) - First-class SaaS tenant isolation
    - Automatic TenantId propagation in message context
    - Tenant-isolated stores (Outbox, Inbox, Saga)
    - Per-tenant configuration and rate limits
  - **Message TTL** (Issue #126) - Time-to-live and automatic expiration
    - Prevents processing stale data
    - Integration with Dead Letter Queue
  - **Request/Response RPC** (Issue #127) - RPC-style messaging
    - Synchronous-style communication over message brokers
    - Correlation ID management and timeout handling
  - **Saga Visibility** (Issue #128) - Enhanced saga observability
    - Query APIs for saga state
    - Step history and audit trail
    - Metrics for in-flight, completed, and failed sagas
  - **Message Encryption** (Issue #129) - Compliance-ready encryption
    - Transparent encryption/decryption in Outbox/Inbox
    - Multiple providers: Azure Key Vault, AWS KMS, Data Protection
    - GDPR, HIPAA, PCI-DSS compliance support
    - New packages planned: `Encina.Encryption`, `Encina.Encryption.AzureKeyVault`, `Encina.Encryption.AwsKms`
  - **Competing Consumers** (Issue #130) - Consumer group management
    - Consumer group registration and rebalancing
    - Partition assignment strategies
    - Kubernetes-native scaling support
  - **Backpressure & Flow Control** (Issue #131) - Overload protection
    - Producer rate limiting
    - Queue depth monitoring
    - Adaptive concurrency control
  - **W3C Trace Context** (Issue #132) - OpenTelemetry context propagation
    - Full W3C Trace Context support (traceparent, tracestate)
    - Baggage propagation for custom metadata
    - Activity integration for handlers
  - **Recurring Messages** (Issue #133) - Cron-style scheduling
    - Cron expression support with timezone handling
    - Missed occurrence strategies
    - Extends current Scheduling pattern
  - **Message Versioning** (Issue #134) - Schema evolution with upcasting
    - Version stamps on messages
    - Upcaster registry for automatic transformation
    - Zero-downtime deployments
  - **Poison Message Detection** (Issue #135) - Intelligent poison message handling
    - Automatic classification (transient, permanent, malformed)
    - Per-classification actions (retry, DLQ, quarantine)
    - Security violation alerting

#### Database Providers Patterns Issues (16 new features planned based on December 2025 research)

- **Generic Repository Pattern** (Issue #279) - Unified data access abstraction
    - `IRepository<TEntity, TId>` with GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync, ListAsync
    - `IReadRepository<TEntity, TId>` for CQRS scenarios
    - Implementations for EF Core, Dapper, MongoDB
    - Inspired by Ardalis.Specification
  - **Specification Pattern** (Issue #280) - Reusable query encapsulation
    - `ISpecification<T>` with Criteria, Includes, OrderBy, Paging
    - `Specification<T>` base class with fluent API
    - Provider-specific evaluators (EF Core LINQ, Dapper SQL, MongoDB FilterDefinition)
  - **Unit of Work Pattern** (Issue #281) - Cross-aggregate transactions
    - `IUnitOfWork` with SaveChangesAsync, BeginTransactionAsync, CommitAsync, RollbackAsync
    - Repository factory method for transactional consistency
  - **Multi-Tenancy Database Support** (Issue #282) - SaaS tenant isolation
    - Strategies: Shared Schema, Schema-per-Tenant, Database-per-Tenant
    - Tenant resolvers: Header, Subdomain, JWT Claim, Route
    - New packages planned: `Encina.Tenancy`, `Encina.Tenancy.SharedSchema`, etc.
  - **Read/Write Database Separation** (Issue #283) - CQRS physical split
    - `IReadWriteDbContextFactory<TContext>` for read replica routing
    - Automatic routing based on IQuery/ICommand
    - Azure SQL ApplicationIntent=ReadOnly support
  - **Bulk Operations** (Issue #284) - High-performance data operations
    - `IBulkOperations<TEntity>`: BulkInsertAsync, BulkUpdateAsync, BulkDeleteAsync, BulkMergeAsync
    - Performance: 680x faster than SaveChanges() for 1M rows
    - Inspired by EFCore.BulkExtensions
  - **Soft Delete & Temporal Tables** (Issue #285) - Logical delete + history
    - `ISoftDeletable` interface with automatic global query filter
    - `ITemporalRepository<TEntity, TId>` for SQL Server temporal tables
    - Queries: GetAsOfAsync, GetHistoryAsync, GetChangedBetweenAsync
  - **Audit Trail Pattern** (Issue #286) - Change tracking
    - `IAuditableEntity` with CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
    - `AuditInterceptor` for automatic population via IRequestContext.UserId
    - TimeProvider integration for testable timestamps
  - **Optimistic Concurrency Abstractions** (Issue #287) - Conflict resolution
    - `IConcurrencyAware` (RowVersion) and `IVersioned` (integer version)
    - `IConcurrencyConflictResolver<TEntity>`: ClientWins, DatabaseWins, Merge strategies
    - `IConcurrentRepository<TEntity, TId>` with retry support
  - **CDC Integration** (Issue #288) - Change Data Capture
    - `ChangeEvent<T>` with Operation, Before, After, Metadata
    - `ICDCConsumer<T>`, `ICDCSubscriptionManager` interfaces
    - New packages planned: `Encina.CDC`, `Encina.CDC.Debezium`
    - Complements Outbox for legacy system integration
  - **Database Sharding Abstractions** (Issue #289) - Horizontal partitioning
    - `IShardable`, `IShardRouter<TEntity>`, `IShardedRepository<TEntity, TId>`
    - Strategies: Hash (consistent hashing), Range, Directory, Geo
    - Scatter-Gather for cross-shard queries
  - **Connection Pool Resilience** (Issue #290) - Pool monitoring and health
    - `IDatabaseHealthMonitor` with ConnectionPoolStats
    - Database-aware circuit breaker, connection warm-up
    - Integration with Encina.Extensions.Resilience
  - **Query Cache Interceptor** (Issue #291) - EF Core second-level cache
    - `IDbCommandInterceptor` for automatic query result caching
    - Automatic invalidation on SaveChanges
    - Integration with existing ICacheProvider
  - **Domain Entity Base Classes** (Issue #292) - DDD foundations
    - `Entity<TId>` with equality and domain events collection
    - `AggregateRoot<TId>` with audit + concurrency traits
    - `DomainEventDispatcher` SaveChanges interceptor
  - **Pagination Abstractions** (Issue #293) - Standardized paging
    - `PagedResult<T>` with TotalCount, TotalPages, HasNext/Previous
    - `PaginationOptions`, `SortedPaginationOptions`
    - `IPagedSpecification<T>` integration
  - **Cursor-based Pagination** (Issue #294) - Keyset pagination research
    - O(1) performance vs offset O(n) for large datasets
    - GraphQL Relay Connection spec compatible
    - Use cases: Infinite scroll, real-time feeds, public APIs

- New labels created (10) for database patterns:
  - `area-repository` - Repository pattern and data access abstractions
  - `area-unit-of-work` - Unit of Work and transaction patterns
  - `area-cqrs` - CQRS and read/write separation patterns
  - `area-bulk-operations` - Bulk insert, update, delete operations
  - `area-audit` - Audit trails and change tracking
  - `area-cdc` - Change Data Capture patterns
  - `area-sharding` - Database sharding and horizontal partitioning
  - `area-pagination` - Pagination patterns (offset, cursor, keyset)
  - `area-concurrency` - Concurrency control and conflict resolution
  - `area-connection-pool` - Connection pooling and management

#### Advanced DDD & Workflow Patterns Issues (13 new features planned based on December 29, 2025 research)

- **Specification Pattern** (Issue #295) - Query composition with reusable specifications
    - `ISpecification<T>` with Criteria, Includes, OrderBy, Paging
    - `Specification<T>` base class with fluent builder
    - AND/OR/NOT composition operators
    - Provider evaluators for EF Core and Dapper
    - Inspired by Ardalis.Specification
    - Labels: `pattern-specification`, `area-ddd`, `area-repository`, `industry-best-practice`
  - **Process Manager Pattern** (Issue #296) - Dynamic workflow orchestration
    - `IProcessManager<TData>` with `ProcessDecision` types
    - Dynamic routing vs predefined Saga sequences
    - Background processor for process advancement
    - Inspired by Enterprise Integration Patterns
    - Labels: `pattern-process-manager`, `area-workflow`, `area-eip`, `masstransit-inspired`
  - **State Machine Pattern (FSM)** (Issue #297) - Fluent state machine for entity lifecycle
    - `StateMachineBuilder<TState, TTrigger>` fluent API
    - Entry/exit actions, substates, guards
    - External state accessor for ORM integration
    - DOT graph export for visualization
    - Inspired by Stateless library (6k+ stars) and MassTransit Automatonymous
    - Labels: `pattern-state-machine`, `area-saga`, `area-workflow`, `masstransit-inspired`
  - **Claim Check Pattern** (Issue #298) - Large payload handling
    - `IClaimCheckStore` for external payload storage
    - `[ClaimCheck]` attribute with threshold and expiry
    - `ClaimCheckPipelineBehavior` for automatic handling
    - Providers: Azure Blob, S3, File System, In-Memory
    - Classic Enterprise Integration Pattern
    - Labels: `pattern-claim-check`, `area-eip`, `area-messaging`, `area-scalability`
  - **Anti-Corruption Layer (ACL)** (Issue #299) - Domain protection from external APIs
    - `IAntiCorruptionLayer<TExternal, TInternal>` interface
    - `AntiCorruptionLayerBehavior` for automatic translation
    - `IExternalEventTranslator` for integration events
    - Core DDD pattern by Eric Evans
    - Labels: `pattern-acl`, `area-ddd`, `area-integration`, `industry-best-practice`
  - **Feature Flag Integration** (Issue #300) - Microsoft.FeatureManagement pipeline
    - `[FeatureGate]` attribute with behavior options
    - `FeatureFlagPipelineBehavior` for short-circuit
    - Contextual targeting via IRequestContext
    - Use cases: dark deployments, canary releases, A/B testing
    - Labels: `pattern-feature-flags`, `area-feature-management`, `cloud-azure`
  - **Priority Queue Support** (Issue #301) - Priority-based message processing
    - `MessagePriority` enum (Critical, High, Normal, Low, Background)
    - `[MessagePriority]` attribute for requests
    - Priority-aware batch fetching in Outbox and Scheduling
    - Labels: `area-messaging`, `area-scheduling`, `area-scalability`
  - **Batching/Bulk Operations** (Issue #302) - Batch handler pattern
    - `IBatchHandler<TRequest, TResponse>` interface
    - `BatchingPipelineBehavior` with auto-batching
    - `[BatchOptions]` with MaxBatchSize, MaxDelayMs
    - Failure strategies: Individual, FailAll, RetryFailed
    - Labels: `area-bulk-operations`, `area-messaging`, `area-performance`
  - **Durable Execution / Checkpointing** (Issue #303) - Long-running workflow support
    - `IDurableContext` with ExecuteActivityAsync, WaitForEventAsync, CreateTimerAsync
    - `IDurableWorkflow<TInput, TOutput>` interface
    - Deterministic replay with history
    - Inspired by Azure Durable Functions and Temporal.io
    - Labels: `pattern-durable-execution`, `area-workflow`, `temporal-inspired`, `cloud-azure`
  - **Multi-Tenancy Pipeline Behavior** (Issue #304) - Automatic tenant isolation
    - `ITenantScopedQuery/Command` marker interfaces
    - `TenantIsolationPipelineBehavior` for enforcement
    - EF Core global query filter extension
    - `[AllowCrossTenant]` for admin scenarios
    - Labels: `area-multi-tenancy`, `area-security`, `area-compliance`, `industry-best-practice`
  - **AI Agent Orchestration** (Issue #305) - LLM agent coordination (Future)
    - `IAgentHandler<TRequest, TResponse>` with capabilities
    - Orchestration patterns: Sequential, Concurrent, Handoff
    - Semantic Kernel adapter
    - Inspired by Microsoft Agent Framework (Oct 2025)
    - Labels: `area-ai-ml`, `area-workflow`, `new-package`
  - **Integration Events** (Issue #306) - Modular Monolith inter-module events
    - `IIntegrationEvent` with EventId, OccurredAtUtc, SourceModule
    - `IModuleEventBus` for in-process publishing
    - Outbox integration for reliability
    - Inspired by Spring Modulith 2.0
    - Labels: `area-modular-monolith`, `area-messaging`, `area-microservices`
  - **Request Versioning Pattern** (Issue #307) - Request evolution and upcasting
    - `[RequestVersion]` and `IUpgradableFrom<T>` interfaces
    - `RequestVersioningBehavior` for auto-upgrade
    - `[DeprecatedRequest]` with warnings
    - Inspired by Marten Event Upcasting
    - Labels: `pattern-versioning`, `area-messaging`, `area-event-sourcing`

- New labels created (12) for DDD & Workflow patterns:
  - `pattern-specification` - Specification pattern for query composition
  - `pattern-process-manager` - Process Manager workflow orchestration
  - `pattern-state-machine` - Finite State Machine pattern
  - `pattern-claim-check` - Claim Check pattern for large payloads
  - `pattern-acl` - Anti-Corruption Layer pattern
  - `pattern-feature-flags` - Feature Flags/Toggles pattern
  - `pattern-durable-execution` - Durable Execution and checkpointing
  - `pattern-versioning` - Request/Event versioning pattern
  - `area-feature-management` - Feature flag management
  - `area-workflow` - Workflow and process orchestration
  - `temporal-inspired` - Pattern inspired by Temporal.io
  - `masstransit-inspired` - Pattern inspired by MassTransit

#### Advanced EDA Patterns Issues (12 new features planned based on December 29, 2025 research)

- Based on analysis of MassTransit, Wolverine 5.0, Temporal.io, Axon Framework, Debezium, and community demand
- **CDC (Change Data Capture) Pattern** (Issue #308) - Database change streaming
    - `ICdcConnector`, `IChangeEventHandler<TEntity>` for insert/update/delete
    - New packages planned: `Encina.CDC`, `Encina.CDC.Debezium`, `Encina.CDC.SqlServer`
    - Use case: Strangler Fig migration, legacy system integration
    - Labels: `area-cdc`, `area-microservices`, `industry-best-practice`, `aot-compatible`
  - **Schema Registry Integration** (Issue #309) - Event schema governance
    - `ISchemaRegistry` with GetSchema, RegisterSchema, CheckCompatibility
    - New packages planned: `Encina.SchemaRegistry`, `Encina.SchemaRegistry.Confluent`
    - Supports Avro, Protobuf, JsonSchema formats
    - Labels: `area-schema-registry`, `transport-kafka`, `area-compliance`, `industry-best-practice`
  - **Event Mesh / Event Gateway** (Issue #310) - Enterprise event distribution
    - `IEventMesh`, `IEventGateway` for cross-transport routing
    - New packages planned: `Encina.EventMesh`, `Encina.EventMesh.CloudEvents`
    - Cross-transport: Kafka → RabbitMQ → Azure Service Bus
    - Labels: `area-cloud-native`, `area-integration`, `industry-best-practice`
  - **Claim Check Pattern** (Issue #311) - Large payload external storage
    - `IClaimCheckStore` with Store/Retrieve/Delete and `ClaimTicket`
    - New packages planned: `Encina.ClaimCheck`, `Encina.ClaimCheck.AzureBlob`, `Encina.ClaimCheck.S3`
    - `[ClaimCheck]` attribute with ThresholdBytes
    - Labels: `area-eip`, `area-performance`, `cloud-azure`, `cloud-aws`
  - **Domain vs Integration Events** (Issue #312) - Clear event type separation
    - `IDomainEvent` (in-process), `IIntegrationEvent` (cross-service)
    - `IEventTranslator<TDomain, TIntegration>` for Anti-Corruption Layer
    - Core DDD pattern for bounded context isolation
    - Labels: `area-ddd`, `area-event-sourcing`, `area-modular-monolith`, `area-microservices`
  - **Event Correlation & Causation Tracking** (Issue #313) - Full event traceability
    - `IEventMetadata` with EventId, CorrelationId, CausationId, Timestamp
    - `EventCorrelationPipelineBehavior` for automatic propagation
    - OpenTelemetry integration with span tags
    - Labels: `area-observability`, `netflix-pattern`, `industry-best-practice`
  - **Temporal Queries (Time Travel)** (Issue #314) - Point-in-time state queries
    - `ITemporalRepository<T>` with GetAt(pointInTime), GetAtVersion, GetHistory
    - `AggregateDiff<T>` for state comparison
    - Use case: Auditing, debugging, what-if scenarios
    - Labels: `area-event-sourcing`, `area-compliance`, `industry-best-practice`
  - **Durable Execution / Workflow Engine** (Issue #315) - Lightweight Temporal.io-style
    - `IDurableWorkflow<TInput, TOutput>`, `IWorkflowContext`, `IWorkflowRunner`
    - Activities, durable timers, signals, deterministic replay
    - New packages planned: `Encina.DurableExecution`, `Encina.DurableExecution.EntityFrameworkCore`
    - Labels: `area-workflow`, `temporal-pattern`, `uber-pattern`, `netflix-pattern`
  - **Event Enrichment Pipeline** (Issue #316) - Batch enrichment for projections
    - `IEventEnricher<T>`, `IBatchEventEnricher` for N+1 avoidance
    - `EnrichmentContext` with StreamId, Version, Services
    - Inspired by Marten 4.13 EnrichEventsAsync
    - Labels: `area-event-sourcing`, `area-performance`, `area-pipeline`
  - **Process Manager Pattern** (Issue #317) - Long-running aggregate coordination
    - `IProcessManager<TState>`, `ProcessManagerBase<TState>`
    - Event-driven coordination vs Saga's predefined sequences
    - `IProcessManagerStore` for persistence
    - Labels: `area-saga`, `area-event-sourcing`, `area-coordination`
  - **Event Streaming Abstractions** (Issue #318) - First-class event streams
    - `IEventStreamPublisher`, `IEventStreamSubscription`
    - Consumer groups, position tracking, acknowledgment
    - Similar to Kafka consumer groups, RabbitMQ Streams
    - Labels: `area-event-streaming`, `transport-kafka`, `transport-redis`, `netflix-pattern`
  - **Idempotency Key Generator** (Issue #319) - Standardized key generation
    - `IIdempotencyKeyGenerator` with Generate, GenerateFromParts
    - `[IdempotencyKey]` attribute with Properties, Namespace, Format
    - Strategies: Hash (SHA256), Composite, UUID v5
    - Labels: `area-idempotency`, `stripe-pattern`, `uber-pattern`, `netflix-pattern`

- New labels created (6) for EDA patterns:
  - `area-schema-registry` - Schema Registry and event schema governance
  - `area-event-streaming` - Event streaming and persistent log patterns
  - `area-idempotency` - Idempotency and exactly-once processing
  - `uber-pattern` - Pattern inspired by Uber engineering
  - `stripe-pattern` - Pattern inspired by Stripe engineering
  - `temporal-pattern` - Pattern inspired by Temporal.io

#### Advanced Caching Patterns Issues (13 new features planned based on December 2025 research)

- **Cache Stampede Protection** (Issue #266) - Thundering herd prevention with multiple strategies
    - Inspired by FusionCache (most popular .NET caching library 2025)
    - Single-Flight pattern: Coalesce concurrent requests into one factory execution
    - Probabilistic Early Expiration (PER): Renew cache before expiration probabilistically
    - TTL Jitter: Add random variation to prevent synchronized expiration
    - Labels: `pattern-stampede-protection`, `area-resilience`, `area-performance`
  - **Eager Refresh / Background Refresh** (Issue #267) - Proactive cache refresh
    - Inspired by FusionCache's EagerRefreshThreshold
    - Refresh in background before TTL expires (e.g., after 80% of duration)
    - Users always get cached response, fresh data arrives asynchronously
    - Eliminates latency spikes from cache expiration
  - **Fail-Safe / Stale-While-Revalidate** (Issue #268) - Resilient caching
    - Serve stale data when factory fails or is slow
    - Soft/Hard timeout support: Return stale immediately if factory exceeds threshold
    - FailSafeDurationSeconds: Extended TTL for emergency use
    - Fail-safe throttling to prevent retry storms
  - **Cache Warming / Pre-warming** (Issue #269) - Cold cache elimination
    - `ICacheWarmer` interface for custom warmers
    - `[CacheWarmer]` attribute for automatic query warming
    - Startup warming via `CacheWarmingHostedService`
    - Configurable strategies: Sequential, Parallel, TopHeavy
  - **Cache Backplane** (Issue #270) - Multi-node synchronization
    - `ICacheBackplane` interface for L1 cache sync across instances
    - Redis backplane implementation with Pub/Sub
    - Modes: InvalidationOnly, SmallValueReplication, FullReplication
    - Node coordination and health tracking
  - **Enhanced Tag-Based Invalidation** (Issue #271) - Semantic cache grouping
    - `Tags` property on `[Cache]` attribute
    - `[CacheTag]` attribute for dynamic tags from response
    - `RemoveByTagAsync` on `ICacheProvider`
    - More efficient than pattern-based invalidation (O(1) vs scan)
  - **Read-Through / Write-Through Patterns** (Issue #272) - Alternative caching strategies
    - `CacheStrategy` enum: CacheAside, ReadThrough, WriteThrough, WriteBehind
    - Read-Through: Cache as primary data source
    - Write-Through: Synchronous write to cache + database
    - Write-Behind: Async persistence with batching
  - **Cache Metrics OpenTelemetry** (Issue #273) - Comprehensive observability
    - Counters: hits, misses, sets, removals, evictions
    - Histograms: latency, value size
    - Gauges: size_bytes, entry_count, hit_rate
    - Resilience: stampede_prevented, stale_served, backplane_messages
  - **Advanced Serialization** (Issue #274) - Performance optimization
    - Per-type serializer configuration
    - MemoryPack support (NativeAOT, ~10x faster than MessagePack)
    - Zstd compression (better ratio than LZ4)
    - Smart compression based on payload size
  - **Multi-Tenant Cache Policies** (Issue #275) - SaaS support
    - `CacheTenantPolicy` per tier (premium, standard, free)
    - Quotas: MaxEntries, MaxMemoryMb, DefaultDuration
    - Rate limiting per tenant
    - Tenant isolation levels: KeyPrefix, Database, Instance
  - **Cache Diagnostics & Debugging** (Issue #276) - Development tooling
    - HTTP headers: X-Cache-Status, X-Cache-Key, X-Cache-Age, X-Cache-TTL
    - Diagnostic endpoints: /cache/stats, /cache/keys, /cache/key/{key}
    - `ICacheInspector` API for programmatic access
    - Cache debugger middleware (?cache-debug=true)
  - **New Cache Providers** (Issue #277) - Expanded ecosystem
    - `Encina.Caching.Memcached` - Pure Memcached support
    - `Encina.Caching.MemoryPack` - AOT-friendly serialization
    - `Encina.Caching.Pogocache` - New 2025 cache (evaluate when stable)
  - **Auto-Recovery / Self-Healing** (Issue #278) - Automatic resilience
    - Retry logic with exponential backoff
    - Circuit breaker for cache operations
    - Automatic reconnection with `ICacheConnectionManager`
    - Fallback strategies: SkipCache, UseLocalMemory, UseSecondaryProvider
    - Self-healing backplane (auto-resubscribe, clear L1 on reconnect)

- New pattern labels created for caching:
  - `pattern-stampede-protection` - Cache stampede and thundering herd protection
  - `pattern-stale-while-revalidate` - Stale-While-Revalidate caching pattern
  - `pattern-read-through` - Read-Through caching pattern
  - `pattern-write-through` - Write-Through caching pattern
  - `pattern-cache-aside` - Cache-Aside caching pattern
  - `pattern-backplane` - Cache backplane synchronization pattern
  - `pattern-circuit-breaker` - Circuit Breaker resilience pattern
  - `fustioncache-inspired` - Pattern inspired by FusionCache library

- Issue #140 (Cache Stampede Prevention) closed as duplicate of #266 (more comprehensive)

#### Advanced Resilience Patterns Issues (9 new features planned based on 2025 research)

- **Hedging Pattern** (Issue #136) - Parallel redundant requests for latency reduction
    - Inspired by Polly v8 and Istio service mesh
    - Configure parallel requests with first-response-wins semantics
    - Latency percentile-based triggering (P95, P99)
    - Integration with OpenTelemetry for observability
  - **Fallback / Graceful Degradation** (Issue #137) - Alternative responses when primary operations fail
    - Inspired by Resilience4j and Polly
    - Cached fallbacks, static defaults, and degraded responses
    - Fallback chain with priority ordering
    - Circuit breaker integration for proactive fallback
  - **Load Shedding with Priority** (Issue #138) - Netflix/Uber-inspired priority-based request shedding
    - Request priority levels: Critical, Degraded, BestEffort, Bulk
    - Adaptive shedding based on system load metrics
    - Integration with rate limiting and circuit breakers
  - **Adaptive Concurrency Control** (Issue #139) - Netflix-inspired dynamic concurrency limits
    - Inspired by Netflix's `concurrency-limits` library
    - AIMD (Additive Increase/Multiplicative Decrease) algorithm
    - Gradient-based limit adjustment based on latency
    - TCP Vegas-style congestion control for services
  - ~~**Cache Stampede Prevention** (Issue #140)~~ - Closed as duplicate of #266 (more comprehensive)
    - Probabilistic early expiration to spread load
  - **Cascading Timeout Coordination** (Issue #141) - Timeout budget propagation across call chains
    - Request deadline propagation via gRPC-style patterns
    - Remaining budget calculation at each service hop
    - Early termination when budget exhausted
  - **Health Checks Standardization** (Issue #142) - Unified health checks across all providers
    - Kubernetes liveness/readiness/startup probe patterns
    - Consistent health check interface for all Encina providers
    - Health aggregation for composite systems
  - **Observability-Resilience Correlation** (Issue #143) - OpenTelemetry integration for resilience events
    - Resilience events as OpenTelemetry spans
    - Metrics for circuit breaker state, retry counts, fallback usage
    - Distributed tracing context propagation through resilience policies
  - **Backpressure / Flow Control** (Issue #144) - Reactive Streams-style backpressure for streaming
    - Producer/consumer rate coordination
    - Buffer overflow strategies (drop, block, latest)
    - Integration with IAsyncEnumerable and stream requests
  - **Chaos Engineering Integration** (Issue #145) - Polly Chaos strategies for fault injection testing
    - Latency injection, exception injection, result manipulation
    - Controlled chaos via feature flags
    - Integration with testing infrastructure

#### Advanced Scheduling Patterns Issues (15 new features planned based on 2025 research)

- **Cancellation & Update API** (Issue #146) - Cancel, reschedule, or update scheduled messages
    - Inspired by MassTransit, Hangfire, Temporal
    - `CancelAsync`, `RescheduleAsync`, `UpdatePayloadAsync` methods
    - Batch cancellation with filters
  - **Priority Queue Support** (Issue #147) - Priority-based message processing
    - Inspired by Meta FOQS and BullMQ
    - Priority levels: Critical, High, Normal, Low, Background
    - Anti-starvation with aging mechanism
  - **Idempotency Keys** (Issue #148) - Exactly-once execution guarantee
    - Inspired by Temporal, Azure Durable Functions
    - User-provided idempotency keys
    - Automatic duplicate detection and rejection
  - **Dead Letter Queue Integration** (Issue #149) - DLQ for failed scheduled messages
    - Integration with existing DLQ infrastructure
    - Automatic move after max retries
    - Inspection and replay capabilities
  - **Timezone-Aware Scheduling** (Issue #150) - Full timezone support
    - Inspired by Hangfire, Quartz.NET
    - DST transition handling
    - IANA timezone database support
  - **Rate Limiting for Scheduled** (Issue #151) - Prevent burst execution
    - Inspired by Meta FOQS, Redis Queue
    - Per-type and global rate limits
    - Token bucket algorithm
  - **Dependency Chains** (Issue #152) - DAG-based job dependencies
    - Inspired by Apache Airflow, Temporal
    - Job dependencies with `DependsOn`
    - Parallel execution of independent jobs
  - **Observability & Metrics** (Issue #153) - OpenTelemetry integration
    - Scheduling-specific spans and metrics
    - Queue depth, execution latency, success/failure rates
    - Grafana dashboard templates
  - **Batch Scheduling** (Issue #154) - Efficient bulk operations
    - `ScheduleManyAsync` for bulk scheduling
    - Transactional batch support
    - Optimized database operations
  - **Delayed Message Visibility** (Issue #155) - SQS-style visibility timeout
    - Inspired by Amazon SQS
    - Lease-based processing
    - Automatic re-queue on timeout
  - **Scheduling Persistence Providers** (Issue #156) - Backend adapters
    - Hangfire backend adapter
    - Quartz.NET backend adapter
    - Unified API across backends
  - **Execution Windows** (Issue #157) - Business hours support
    - Inspired by Quartz.NET, enterprise schedulers
    - Business hours and maintenance windows
    - Holiday calendar integration
  - **Schedule Templates** (Issue #158) - Reusable configurations
    - Named schedule templates
    - Template inheritance and override
    - Centralized schedule management
  - **Webhook Notifications** (Issue #159) - External system notifications
    - HTTP webhook on schedule events
    - Retry with exponential backoff
    - Signature verification for security
  - **Multi-Region Scheduling** (Issue #160) - Globally distributed scheduling
    - Inspired by Meta FOQS, Uber Cadence
    - Leader election per region
    - Cross-region coordination

#### Advanced Observability Patterns Issues (15 new features planned based on 2025 research)

- **Real Metrics Collection (EncinaMetrics)** (Issue #174) - Full IEncinaMetrics implementation
    - System.Diagnostics.Metrics for zero-allocation metrics
    - Histograms: `encina.request.duration`, `encina.handler.duration`
    - Counters: `encina.requests.total`, `encina.errors.total`
    - Gauges: `encina.active.handlers`, `encina.pending.outbox`
    - Standardized tags for all metrics
    - Inspired by MassTransit, Wolverine OpenTelemetry
  - **Correlation & Causation ID Support** (Issue #175) - Request tracking across services
    - CorrelationId propagation through pipeline
    - CausationId for message causation chains
    - Extension methods for context enrichment
    - Standard headers for all transports
    - Inspired by NServiceBus, MassTransit
  - **Baggage Propagation Utilities** (Issue #176) - W3C Baggage support
    - Helpers for IRequestContext baggage
    - AddBaggage(), GetBaggage(), GetAllBaggage()
    - Automatic propagation to handlers
    - Activity.Baggage integration
    - Inspired by OpenTelemetry spec, .NET Aspire
  - **Missing Semantic Convention Attributes** (Issue #177) - OTel messaging semantics
    - Complete messaging semantic attributes
    - messaging.system, messaging.operation, messaging.destination
    - Handler-specific attributes
    - OpenTelemetry Semantic Conventions compliant
  - **Encina.OpenTelemetry.AzureMonitor** (Issue #178) - Azure integration package
    - Azure Application Insights integration
    - Native Azure Monitor exporters
    - Live Metrics Stream support
    - Azure distributed tracing
  - **Encina.OpenTelemetry.AwsXRay** (Issue #179) - AWS integration package
    - AWS X-Ray via ADOT integration
    - Native AWS exporters
    - AWS Lambda instrumentation
    - AWS distributed tracing
  - **Encina.OpenTelemetry.Prometheus** (Issue #180) - Prometheus metrics package
    - Native /metrics endpoint
    - OpenMetrics format
    - Configurable labels
    - Grafana integration
  - **Encina.HealthChecks Package** (Issue #181) - Dedicated health checks
    - Kubernetes probes: liveness, readiness, startup
    - Health check aggregation
    - Status dashboard support
    - Pattern-specific health checks
  - **Encina.Serilog.OpenTelemetry** (Issue #182) - Serilog to OTel bridge
    - Serilog → OpenTelemetry Logs export
    - Automatic trace context enrichment
    - Optimized formatters
  - **Sampling Behaviors** (Issue #183) - Configurable trace sampling
    - Head and tail sampling
    - Probabilistic sampling
    - Rate-limiting sampler
    - Per-request-type sampling rules
  - **Request Tracing Behavior** (Issue #184) - Detailed request tracing
    - Per-request Activity spans
    - Handler timing
    - Pipeline step visibility
    - Error correlation
  - **Error Recording Enhancements** (Issue #185) - Enhanced error capture
    - Exception details in spans
    - Stack trace recording
    - Error categorization
    - OTel error semantic conventions
  - **Distributed Context Properties** (Issue #186) - Context propagation
    - Custom property propagation
    - Cross-service context sharing
    - W3C Trace Context extensions
  - **Grafana Dashboards** (Issue #187) - Pre-built visualizations
    - Main Encina dashboard JSON
    - Per-pattern dashboards (Outbox, Saga, etc.)
    - Configurable alerts
    - One-click import
  - **Aspire Dashboard Integration Guide** (Issue #188) - .NET Aspire docs
    - Aspire Dashboard configuration
    - Encina trace visualization
    - Local development setup

#### Web/API Integration Patterns Issues (18 new features planned based on December 2025 research)

- **Server-Sent Events** (Issue #189) - Native .NET 10 SSE support
    - Leverage `TypedResults.ServerSentEvents` API from ASP.NET Core 10
    - `SseEndpointExtensions` for easy endpoint registration
    - Heartbeat/keep-alive and automatic retry configuration
    - Integration with notification streaming patterns
    - Use cases: dashboards, real-time notifications, progress indicators
  - **REPR Pattern Support** (Issue #190) - Request-Endpoint-Response pattern
    - `EncinaEndpoint<TRequest, TResponse>` abstract base class
    - `CommandEndpoint` and `QueryEndpoint` specialized variants
    - Fluent `EndpointBuilder` for configuration
    - Auto-registration via assembly scanning
    - Alignment with Vertical Slice Architecture and CQRS
    - Inspired by FastEndpoints and industry best practices
  - **Problem Details RFC 9457** (Issue #191) - Updated error response standard
    - Update from RFC 7807 to RFC 9457 (supersedes 7807)
    - Automatic TraceId inclusion in error responses
    - `IExceptionHandler` implementation for global exception handling
    - Timestamp and error code in extensions
    - Validation problem details with proper grouping
  - **API Versioning Helpers** (Issue #192) - Comprehensive versioning support
    - Integration with `Asp.Versioning.Http` package
    - `[ApiVersion]` attribute support for handlers
    - Version-aware handler resolution
    - Deprecation headers (Sunset RFC 8594, Deprecation)
    - Multiple versioning strategies (URI, query, header, media type)
  - **Minimal APIs Organization** (Issue #193) - Endpoint organization extensions
    - `IEncinaEndpointModule` interface for modular organization
    - `MapEncinaModules` for auto-registration
    - `MapPostEncina`, `MapGetEncina`, etc. extension methods
    - `WithEncinaResponses()` for common response documentation
    - Feature folder conventions support
  - **Encina.SignalR Package** (Issue #194) - Real-time bidirectional communication
    - New package: `Encina.SignalR` (documented but not yet implemented)
    - `ISignalRNotificationBroadcaster` for broadcasting notifications
    - `EncinaHub` base class with Send/Publish methods
    - Group management with tenant isolation
    - `[BroadcastToSignalR]` attribute for automatic broadcasting
    - Integration with existing notification patterns
  - **GraphQL/HotChocolate Full Integration** (Issue #195) - Complete GraphQL support
    - Enhance existing `Encina.GraphQL` from basic bridge to full integration
    - Auto-generate Query/Mutation types from handlers with `[GraphQLQuery]`, `[GraphQLMutation]`
    - Subscription support with pub/sub integration
    - DataLoader base class for N+1 prevention
    - `Either<EncinaError, T>` → GraphQL error mapping via `EncinaErrorFilter`
    - RequestContext propagation to resolvers
  - **gRPC Improvements** (Issue #196) - Strong typing and streaming
    - Proto code generation from handler types
    - Strongly-typed service implementations (replacing reflection-based)
    - Server, client, and bidirectional streaming with `IAsyncEnumerable`
    - Service interceptors for logging, auth, metrics
    - `EncinaError` → gRPC Status code mapping
  - **Rate Limiting Pipeline Behavior** (Issue #197) - Handler-level rate limiting
    - `[RateLimit]` attribute for per-handler configuration
    - Support for Fixed/Sliding/Token/Concurrency limiters
    - Partition keys: User, Tenant, IP, ApiKey, Custom
    - Response headers: X-RateLimit-Limit, X-RateLimit-Remaining, Retry-After
    - Distributed rate limiting support (Redis, SQL Server)
    - Integration with ASP.NET Core rate limiting middleware
  - **OpenAPI 3.1 Enhanced** (Issue #198) - Schema generation and SDK support
    - Auto-generate OpenAPI schemas from handler types
    - Data annotation → OpenAPI constraint mapping
    - XML comments integration
    - Encina error response documentation
    - Client SDK generation helpers (NSwag, OpenAPI Generator)
    - YAML export support
  - **BFF Pattern Support** (Issue #199) - Backend for Frontend
    - New package: `Encina.AspNetCore.BFF`
    - `IBffAggregator` for query aggregation
    - Client-specific response transformation
    - Client detection (header, user-agent, claims)
    - Client-aware caching
    - Parallel aggregation with timeout and partial results
  - **AI/LLM Integration Patterns** (Issue #200) - Provider-agnostic AI integration
    - New packages: `Encina.AI`, `Encina.AI.OpenAI`, `Encina.AI.Azure`, `Encina.AI.Anthropic`, `Encina.AI.Ollama`, `Encina.AI.SemanticKernel`
    - `IAIProvider` abstraction for multiple LLM providers
    - `IAIRequest<TResponse>` integration with Encina pipeline
    - Chat completion, embedding, and structured output support
    - Streaming responses via `IAsyncEnumerable`
    - Prompt validation behavior (PII detection, injection prevention)
    - Semantic Kernel adapter for orchestration
    - Fallback chain for provider resilience
  - **Vertical Slice Architecture Templates** (Issue #201) - CLI scaffolding
    - `encina new feature <name>` command for complete feature slices
    - Generate command, query, endpoint, validator, and test files
    - Module registration and DI extension files
    - Custom template support
    - Interactive mode for guided generation
  - **WebHook Support** (Issue #202) - Webhook receiving and sending
    - `IWebhookHandler<TPayload>` interface
    - Signature validation (HMAC-SHA256, HMAC-SHA1)
    - Timestamp validation for replay attack prevention
    - Inbox pattern integration for idempotency
    - Webhook sender with retry and dead letter
    - Provider configuration (Stripe, GitHub, etc.)
  - **Health Aggregation Endpoint** (Issue #203) - Combined health checks
    - Aggregated `/health` endpoint
    - `/health/ready` and `/health/live` separation (Kubernetes probes)
    - `/health/detailed` with authorization
    - Module health check auto-discovery
    - Response caching
  - **Passkey Authentication** (Issue #204) - WebAuthn/FIDO2 support
    - `[RequirePasskey]` attribute for high-security operations
    - Integration with .NET 10 ASP.NET Core Identity passkey features
    - `IPasskeyChallenger` for challenge/response flow
    - Fallback to password option
  - **Google Cloud Functions** (Issue #205) - GCF integration
    - New package: `Encina.GoogleCloudFunctions`
    - `EncinaHttpFunction` base class
    - `EncinaCloudEventFunction` for Pub/Sub events
    - Context enrichment (correlation ID, trace ID)
    - Health check integration
  - **Cloudflare Workers** (Issue #206) - Edge computing integration
    - New package: `Encina.CloudflareWorkers`
    - `EncinaWorker` base class
    - KV storage integration (as cache provider)
    - Durable Objects for saga coordination (future)
    - D1 database integration (future)

- New Labels Created (Web/API - December 2025):
  - `area-ai-ml` - AI/ML and LLM integration patterns
  - `area-bff` - Backend for Frontend patterns
  - `area-openapi` - OpenAPI/Swagger documentation and generation
  - `area-webhooks` - Webhook receiving and sending patterns
  - `area-rate-limiting` - Rate limiting and throttling patterns
  - `area-health-checks` - Health checks and readiness probes
  - `area-authentication` - Authentication patterns (Passkeys, OAuth, etc.)
  - `cloud-cloudflare` - Cloudflare Workers and services

#### Advanced Testing Patterns Issues (13 new features planned based on 2025 research)

- **Test Data Generation** (Issue #161) - Bogus/AutoBogus integration for realistic test data
    - `EncinaFaker<T>` base class with Encina-specific conventions
    - Pre-built fakers for messaging entities (Outbox, Inbox, Saga, Scheduled)
    - Seed support for deterministic, reproducible tests
    - New package planned: `Encina.Testing.DataGeneration`
  - **Testcontainers Integration** (Issue #162) - Docker fixtures for database testing
    - Pre-configured fixtures: `SqlServerContainerFixture`, `PostgreSqlContainerFixture`, `MongoDbContainerFixture`, `RedisContainerFixture`
    - Integration with existing `EncinaFixture`
    - GitHub Actions CI/CD compatible
    - New package planned: `Encina.Testing.Testcontainers`
  - **Database Reset with Respawn** (Issue #163) - Intelligent cleanup between tests
    - `EncinaRespawner` factory with Encina-specific table exclusions
    - `DatabaseIntegrationTestBase` abstract class
    - 3x faster than truncate/recreate approach
  - **HTTP Mocking with WireMock** (Issue #164) - External API mocking
    - `EncinaWireMockFixture` with helpers: `SetupOutboxWebhook()`, `SetupExternalApi()`
    - `EncinaRefitMockFixture<TClient>` for Refit clients
    - Fault simulation: `SetupFault()`, `SetupDelay()`
    - New package planned: `Encina.Testing.WireMock`
  - **Snapshot Testing with Verify** (Issue #165) - Approval testing for complex responses
    - `EncinaVerifyExtensions` for Either, ValidationResult, EncinaError
    - Custom JSON converters for Encina types
    - Data scrubbing for non-deterministic values (GUIDs, dates)
  - **Architecture Testing with ArchUnitNET** (Issue #166) - CQRS architecture rules
    - `EncinaArchitectureRules` with pre-defined rules
    - `CommandsMustNotReturnVoid()`, `QueriesMustNotModifyState()`
    - `HandlersMustNotDependOnControllers()`, `DomainMustNotDependOnInfrastructure()`
    - New package planned: `Encina.Testing.Architecture`
  - **Handler Registration Tests** (Issue #167) - Verify all handlers are registered
    - `EncinaRegistrationAssertions.AllRequestsShouldHaveHandlers(assembly)`
    - `EncinaRegistrationAssertions.AllNotificationsShouldHaveHandlers(assembly)`
    - `RegistrationVerifier` fluent API
    - Early detection of missing handler registrations
  - **Pipeline Testing Utilities** (Issue #168) - Control behaviors in tests
    - `PipelineTestContext<TRequest, TResponse>` for pipeline testing
    - `WithBehavior<T>()`, `WithoutBehavior<T>()`, `WithMockedHandler()` methods
    - `VerifyBehaviorCalled<T>(Times)` verification
    - `PipelineTest.For<TRequest, TResponse>()` factory
  - **Messaging Pattern Helpers** (Issue #169) - Helpers for Outbox, Inbox, Saga, Scheduling
    - `OutboxTestHelper`: `CaptureMessages()`, `VerifyMessagePublished<T>()`
    - `InboxTestHelper`: `SimulateIdempotentMessage()`, `VerifyProcessedOnce()`
    - `SagaTestBase<TSaga, TData>`: Given/When/Then for sagas
    - `SchedulingTestHelper`: `AdvanceTimeAndGetDue()`, `VerifyCronNextExecution()`
  - **Improved Assertions** (Issue #170) - Fluent assertions with chaining
    - `AndConstraint<T>` for chained assertions
    - `ShouldBeSuccess().And.ShouldSatisfy(x => ...)`
    - Streaming assertions for `IAsyncEnumerable<Either<EncinaError, T>>`
    - Error collection assertions
  - **TUnit Support** (Issue #171) - Source-generated testing framework
    - `EncinaTUnitFixture` adapted for TUnit model
    - NativeAOT compatible (aligns with Source Generators #50)
    - 10-200x faster test execution
    - New package planned: `Encina.Testing.TUnit`
  - **Mutation Testing Integration** (Issue #172) - Stryker.NET configuration
    - Pre-configured `stryker-config.json` for Encina projects
    - `scripts/run-stryker.cs` helper script
    - GitHub Actions workflow for mutation testing
    - `MutationKillerAttribute` for edge case tests
  - **CI/CD Workflow Templates** (Issue #173) - Reusable GitHub Actions
    - `encina-test.yml` - Basic unit + integration tests
    - `encina-matrix.yml` - Cross-platform, multi-database testing
    - `encina-full-ci.yml` - Complete CI with architecture + mutation tests

- New Labels Created (Testing):
  - `area-testing` - Testing utilities and frameworks
  - `testing-integration` - Integration testing utilities
  - `testing-unit` - Unit testing utilities
  - `testing-mocking` - Mocking and stubbing utilities
  - `testing-snapshot` - Snapshot and approval testing
  - `testing-data-generation` - Test data generation and fixtures
  - `area-mutation-testing` - Mutation testing and test quality
  - `area-architecture-testing` - Architecture rules and verification
  - `area-ci-cd` - CI/CD pipelines and automation
  - `area-docker` - Docker and containerization
  - `aot-compatible` - NativeAOT and trimming compatible

#### Advanced Distributed Lock Patterns Issues (20 new features planned based on December 2025 research)

- **PostgreSQL Provider** (Issue #207) - Native PostgreSQL advisory locks
    - `pg_advisory_lock(key)` for exclusive locks
    - `pg_advisory_lock_shared(key)` for shared locks
    - `pg_try_advisory_lock()` for non-blocking acquisition
    - Session-level and transaction-level lock APIs
    - No additional table required, uses PostgreSQL native locks
    - Inspired by DistributedLock (madelson) - most requested database provider
  - **MySQL Provider** (Issue #208) - GET_LOCK-based locking
    - `GET_LOCK(name, timeout)` / `RELEASE_LOCK(name)` functions
    - Session-scoped locks with timeout support
    - Lock name validation and sanitization
    - Compatible with MySQL 5.7+, MariaDB 10.0+
  - **Azure Blob Storage Provider** (Issue #209) - Cloud-native blob leases
    - Blob lease acquisition with 15-60s duration (max 60s)
    - Auto-renewal background thread
    - `IAzureBlobLockProvider` specialized interface
    - Health checks for lease monitoring
    - Inspired by Azure SDK, Medallion.Threading
  - **DynamoDB Provider** (Issue #210) - AWS-native conditional writes
    - Conditional writes with `attribute_not_exists` for lock acquisition
    - TTL-based automatic expiration
    - Heartbeat mechanism for lease extension
    - Fence tokens for consistency
    - Inspired by AWS DynamoDB Locking Client
  - **Consul Provider** (Issue #211) - HashiCorp session-based locks
    - Session-based locking with configurable TTL
    - Health check integration for automatic lock release
    - Leader election primitives
    - Watch for lock state changes
    - Inspired by HashiCorp Consul
  - **etcd Provider** (Issue #212) - Lease-based distributed coordination
    - etcd lease creation with TTL
    - Transaction-based lock acquisition
    - Watch for key changes
    - Integration with etcd v3 API
    - Inspired by etcd.io distributed lock recipe
  - **ZooKeeper Provider** (Issue #213) - Ephemeral sequential nodes
    - Ephemeral sequential node creation
    - Watch mechanism for predecessor node deletion
    - Automatic lock release on session disconnect
    - Lock fairness through sequence ordering
    - Inspired by Apache ZooKeeper recipes
  - **Oracle Provider** (Issue #214) - DBMS_LOCK package
    - `DBMS_LOCK.REQUEST()` / `DBMS_LOCK.RELEASE()` procedures
    - Multiple lock modes (Shared, Exclusive)
    - Lock handle allocation and management
    - Compatible with Oracle 12c+
    - Inspired by DistributedLock (madelson)
  - **Distributed Semaphores** (Issue #215) - Counting locks for N-concurrent access
    - `IDistributedSemaphore` interface with count-based acquisition
    - `TryAcquireAsync(resource, maxCount, expiry)` method
    - `GetAvailableCountAsync(resource)` for monitoring
    - Use cases: rate limiting, connection pooling, resource throttling
    - Implementations: Redis (Lua scripts), PostgreSQL, SQL Server
    - Inspired by DistributedLock (madelson) semaphore support
  - **Leader Election** (Issue #216) - Cluster-wide leader selection
    - `ILeaderElectionProvider` interface for leader management
    - `AcquireLeadershipAsync()`, `IsLeaderAsync()` methods
    - `WatchLeadershipAsync()` for change notifications via `IAsyncEnumerable`
    - Automatic lease renewal
    - Use cases: singleton services, scheduled job coordination
    - Inspired by Consul, etcd, Kubernetes leader election
  - **Read/Write Locks** (Issue #217) - Multiple readers, exclusive writer
    - `IDistributedReadWriteLockProvider` interface
    - Shared read locks (multiple concurrent readers)
    - Exclusive write locks (single writer, no readers)
    - Upgrade/downgrade support where possible
    - Inspired by PostgreSQL `pg_advisory_lock_shared`, ReaderWriterLockSlim
  - **Fencing Tokens** (Issue #218) - Split-brain prevention
    - Monotonically increasing token with each lock acquisition
    - Storage rejects operations with stale tokens
    - Prevents processing with expired locks
    - Inspired by Martin Kleppmann "Designing Data-Intensive Applications"
  - **Multi-Resource Locks** (Issue #219) - Atomic multi-lock acquisition
    - Acquire multiple resources atomically
    - Deadlock prevention via consistent ordering
    - All-or-nothing semantics
    - Inspired by Two-Phase Locking protocol
  - **DistributedLockPipelineBehavior** (Issue #220) - Declarative handler locking
    - `[DistributedLock("{request.EntityId}", ExpirySeconds = 30)]` attribute
    - Key template support with property placeholders
    - Configurable expiry, retry, and wait timeouts
    - Automatic lock acquisition/release around handler
    - Integration with existing pipeline infrastructure
  - **LeaderElectionPipelineBehavior** (Issue #221) - Leader-only handler execution
    - `[RequiresLeadership("scheduler-leader")]` attribute
    - Handler only executes on current leader node
    - Non-leaders receive predefined fallback response
    - Automatic leadership monitoring
    - Use cases: singleton scheduled jobs, exclusive processors
  - **OpenTelemetry Integration for Locks** (Issue #222) - Metrics and traces
    - Metrics: `encina.lock.acquired`, `encina.lock.released`, `encina.lock.wait_time`, `encina.lock.contention`
    - Traces: Spans for lock acquisition, hold duration, release
    - Tags: lock type, resource name, outcome (acquired/timeout/failed)
    - Integration with existing Encina.OpenTelemetry infrastructure
  - **Auto-extend Locks** (Issue #223) - Automatic lease extension
    - Background renewal for long-running operations
    - Configurable extension interval (e.g., renew at 50% of expiry)
    - Graceful handling of extension failures
    - Prevents accidental lock expiration during processing
  - **Lock Metadata** (Issue #224) - Lock holder information
    - `GetLockInfoAsync(resource)` returning holder identity, acquisition time
    - Useful for debugging and operations
    - Optional: machine name, process ID, correlation ID
    - Read-only, does not affect lock semantics
  - **Lock Queuing & Fairness** (Issue #225) - FIFO ordering for waiters
    - Fair ordering: first waiter acquires lock first
    - Prevents starvation of long-waiting requesters
    - Optional: priority-based queuing
    - Implementations vary by backend capabilities
  - **RedLock Algorithm** (Issue #226) - High-availability multi-Redis locking
    - Consensus across N/2+1 Redis instances for lock acquisition
    - Clock drift compensation
    - Automatic retry with jitter on partial acquisition
    - `IRedLockProvider` as wrapper around `IDistributedLockProvider`
    - Inspired by Redis RedLock specification, RedLock.net

- New Labels Created (Distributed Lock - December 2025):
  - `area-distributed-lock` - Distributed locking patterns
  - `area-leader-election` - Leader election and coordination
  - `area-semaphore` - Distributed semaphores and counting locks
  - `area-coordination` - Distributed coordination primitives
  - `area-pipeline` - Pipeline behaviors and middleware

#### Message Transport Patterns Issues (29 new features planned based on December 2025 research)

- **New Message Transports (6 issues)**:
    - **Google Cloud Pub/Sub Transport** (Issue #237) - Native GCP integration
      - `IMessageTransportPubSub` interface with dead-lettering and ordering keys
      - Exactly-once delivery (Preview feature), flow control
      - Schema validation, message filtering, BigQuery subscriptions
      - New package planned: `Encina.Transport.GooglePubSub`
    - **AWS EventBridge Transport** (Issue #238) - Event-driven AWS integration
      - Event bus publishing with partner/custom buses
      - Content-based filtering with event patterns
      - Archive and replay, cross-account delivery
      - Schema discovery integration
      - New package planned: `Encina.Transport.EventBridge`
    - **Apache Pulsar Transport** (Issue #239) - Multi-tenant messaging
      - Exclusive, Shared, Failover, Key_Shared subscription types
      - Topic compaction, tiered storage, geo-replication
      - Schema registry with Avro/Protobuf/JSON
      - Pulsar Functions integration for stream processing
      - New package planned: `Encina.Transport.Pulsar`
    - **Redis Streams Transport** (Issue #240) - Redis-native streaming
      - `XADD`/`XREAD`/`XREADGROUP` command integration
      - Consumer groups with automatic rebalancing
      - Stream trimming (`MAXLEN`, `MINID`)
      - Pending Entry List (PEL) management, message acknowledgment
      - New package planned: `Encina.Transport.RedisStreams`
    - **Apache ActiveMQ Artemis Transport** (Issue #241) - Enterprise JMS-compatible broker
      - AMQP 1.0 and CORE protocol support
      - Scheduled messages, last-value queues, ring queues
      - Message grouping, large message support
      - Divert and bridge configurations
      - New package planned: `Encina.Transport.ActiveMQ`
    - **Dapr Transport** (Issue #242) - Cloud-agnostic pub/sub abstraction
      - Component-based pub/sub (40+ broker implementations)
      - CloudEvents format native support
      - Bulk publish, per-message metadata
      - Subscriber routing rules
      - New package planned: `Encina.Transport.Dapr`
  - **Enterprise Integration Patterns (9 issues)**:
    - **Message Translator** (Issue #243) - Transform message formats between systems
      - `IMessageTranslator<TFrom, TTo>` interface
      - Bidirectional translation support
      - AutoMapper and Mapster integration
    - **Content Enricher** (Issue #244) - Augment messages with external data
      - `IContentEnricher<TMessage>` interface
      - Async enrichment from external services
      - Caching support for enrichment data
    - **Splitter Pattern** (Issue #245) - Break composite messages into parts
      - `IMessageSplitter<TComposite, TPart>` interface
      - Correlation ID propagation
      - Sequential and parallel splitting
    - **Aggregator Pattern** (Issue #246) - Combine related messages
      - `IMessageAggregator<TPart, TResult>` interface
      - Time-based and count-based completion conditions
      - Correlation strategies (CorrelationId, custom keys)
    - **Claim Check Pattern** (Issue #247) - Large message handling
      - `IClaimCheckStore` interface for payload storage
      - Azure Blob, S3, local filesystem providers
      - Automatic check-in/check-out with message metadata
    - **Async Request-Reply** (Issue #248) - Correlation-based responses
      - `IAsyncRequestReply<TRequest, TResponse>` interface
      - Reply-to queue management
      - Timeout handling with continuation tokens
    - **Competing Consumers** (Issue #249) - Parallel message processing
      - `ICompetingConsumerPool` interface
      - Dynamic scaling based on queue depth
      - Affinity and stickiness options
    - **Message Filter** (Issue #250) - Route messages by content
      - `IMessageFilter<TMessage>` interface
      - Predicate-based filtering
      - Dead-letter routing for filtered messages
    - **Priority Queue** (Issue #251) - Priority-based message delivery
      - `IMessagePriority` interface for priority assignment
      - Multiple priority levels configuration
      - Fair scheduling to prevent starvation
  - **Advanced Transport Features (8 issues)**:
    - **Message Batching** (Issue #252) - Efficient bulk operations
      - `BatchPublisher<TMessage>` with configurable batch size/timeout
      - Async flush with backpressure
      - Per-transport batch optimization
    - **Native Delayed Delivery** (Issue #253) - Broker-native scheduling
      - `IDelayedDeliveryTransport` interface
      - `DelayUntil(DateTimeOffset)`, `DelayFor(TimeSpan)` methods
      - Fallback to Encina.Scheduling when not supported
    - **Message Deduplication** (Issue #254) - Transport-level idempotency
      - `IDeduplicationStrategy` interface
      - Content-hash, Message-ID, custom key strategies
      - Configurable deduplication window
    - **Partitioning** (Issue #255) - Ordered message delivery
      - `IPartitionKeyProvider<TMessage>` interface
      - Consistent hashing for partition assignment
      - Partition affinity for stateful consumers
    - **Consumer Groups** (Issue #256) - Coordinated consumption
      - `IConsumerGroup` interface
      - Automatic partition assignment and rebalancing
      - Offset tracking and commit strategies
    - **Bidirectional Streaming** (Issue #257) - gRPC streaming support
      - `IStreamingTransport<TRequest, TResponse>` interface
      - Client and server streaming modes
      - Flow control and backpressure
    - **Message Compression** (Issue #258) - Payload compression
      - `IMessageCompressor` interface
      - Gzip, Brotli, LZ4, Snappy algorithms
      - Content-encoding negotiation
    - **Schema Registry Integration** (Issue #259) - Schema evolution
      - `ISchemaRegistry` interface
      - Confluent Schema Registry, AWS Glue support
      - Compatibility checks (BACKWARD, FORWARD, FULL)
  - **Transport Interoperability (3 issues)**:
    - **CloudEvents Format Support** (Issue #260) - CNCF standard events
      - `ICloudEventsSerializer` interface
      - Structured and binary content modes
      - Extension attributes support
    - **NServiceBus Interoperability** (Issue #261) - Bridge to NServiceBus
      - Message format translation
      - Header mapping (NServiceBus ↔ Encina)
      - Gateway pattern for gradual migration
    - **MassTransit Interoperability** (Issue #262) - Bridge to MassTransit
      - Envelope format translation
      - Consumer adapter patterns
      - Saga state migration utilities
  - **Transport Observability (3 issues)**:
    - **Transport Health Checks** (Issue #263) - Liveness and readiness probes
      - `ITransportHealthCheck` interface per transport
      - Connection state, queue depth, consumer lag
      - ASP.NET Core Health Checks integration
    - **Transport Metrics** (Issue #264) - Performance metrics
      - Messages sent/received/failed per transport
      - Latency histograms (P50, P95, P99)
      - OpenTelemetry Metrics integration
    - **Transport Distributed Tracing** (Issue #265) - End-to-end tracing
      - W3C Trace Context propagation
      - Span creation for publish/consume operations
      - Baggage propagation for cross-service context

- New Labels Created (Message Transport - December 2025):
  - `transport-rabbitmq` - RabbitMQ transport provider
  - `transport-kafka` - Apache Kafka transport provider
  - `transport-azure-sb` - Azure Service Bus transport provider
  - `transport-sqs` - AWS SQS transport provider
  - `transport-redis` - Redis transport provider
  - `transport-nats` - NATS transport provider
  - `transport-pulsar` - Apache Pulsar transport provider
  - `transport-grpc` - gRPC transport provider
  - `transport-dapr` - Dapr transport provider
  - `transport-eventbridge` - AWS EventBridge transport provider
  - `transport-pubsub` - Google Cloud Pub/Sub transport provider
  - `transport-activemq` - Apache ActiveMQ Artemis transport provider

- New Labels Created (Previously):
  - `area-scheduling` - Scheduling and recurring message patterns
  - `area-saga` - Saga and Process Manager patterns
  - `area-encryption` - Message encryption and data protection
  - `area-scalability` - Horizontal scaling and consumer patterns
  - `area-polly` - Polly v8 integration and resilience strategies
  - `netflix-pattern` - Patterns inspired by Netflix OSS
  - `industry-best-practice` - Industry-proven patterns from major tech companies
  - `meta-pattern` - Patterns inspired by Meta/Facebook infrastructure (FOQS, etc.)

- CLI Scaffolding Tool (`Encina.Cli`) - Issue #47:
  - `encina new <template> <name>` - Create new Encina projects (api, worker, console)
    - Options: `--database`, `--caching`, `--transport`, `--output`, `--force`
  - `encina generate handler <name>` - Generate command handlers with optional response types
  - `encina generate query <name> --response <type>` - Generate query handlers
  - `encina generate saga <name> --steps <steps>` - Generate saga definitions
  - `encina generate notification <name>` - Generate notifications and handlers
  - `encina add caching|database|transport|validation|resilience|observability` - Add packages
  - Built with System.CommandLine 2.0 and Spectre.Console
  - Packaged as .NET global tool (`dotnet tool install Encina.Cli`)
  - Comprehensive test coverage: 65 tests (unit, guard)

#### Clean Architecture Patterns Issues (2 new features planned based on December 29, 2025 research)

- **Result Pattern Extensions** (Issue #468) - Fluent API for Either
    - `EitherCombineExtensions`: `Combine<T1, T2>()`, `Combine<T1, T2, T3>()` for combining multiple results
    - `EitherAccumulateExtensions`: Error accumulation instead of fail-fast
    - `EitherAsyncExtensions`: `BindAsync()`, `MapAsync()`, `TapAsync()` for async chains
    - `EitherHttpExtensions`: `ToProblemDetails()`, `ToActionResult()`, `ToResult()` for Minimal APIs
    - `EitherConditionalExtensions`: `When()`, `Ensure()`, `OrElse()` for conditional operations
    - Inspired by FluentResults, language-ext, CSharpFunctionalExtensions
    - Priority: MEDIUM - Improves ROP ergonomics
  - **Partitioned Sequential Messaging** (Issue #469) - Wolverine 5.0-inspired pattern
    - `IPartitionedMessage` interface with `PartitionKey`
    - Specialized interfaces: `ISagaPartitionedMessage`, `ITenantPartitionedMessage`, `IAggregatePartitionedMessage`
    - `IPartitionedQueueManager` with System.Threading.Channels
    - `PartitionedMessageBehavior` for pipeline integration
    - `IPartitionStore` for optional durability
    - Messages with same PartitionKey process sequentially; different partitions in parallel
    - Priority: MEDIUM - Critical for saga workflows and multi-tenancy

- New labels created for Clean Architecture Patterns:
  - `area-value-objects` - Value Objects and domain primitives (#2E8B57)
  - `area-strongly-typed-ids` - Strongly Typed IDs and identity patterns (#2E8B57)
  - `area-specification-pattern` - Specification pattern for queries (#2E8B57)
  - `area-domain-services` - Domain Services abstraction (#2E8B57)
  - `area-result-pattern` - Result/Either pattern and functional error handling (#9932CC)
  - `area-bounded-context` - Bounded Context and module boundaries (#2E8B57)

### Removed

- **API Versioning Helpers** (Issue #54) - Closed as "won't fix". `Asp.Versioning` provides complete HTTP-level versioning; adding `[ApiVersion]` to CQRS handlers would be redundant since versioning belongs on the public API surface (controllers/endpoints), not internal handlers.

### Deferred

- **ODBC Provider** (Issue #56) - Moved to post-1.0 evaluation. Valuable for legacy database scenarios but not critical for core 1.0 release.

### Added (Patterns & Infrastructure)

- Scatter-Gather Pattern (Issue #63):
  - Enterprise Integration Pattern for sending requests to multiple handlers and aggregating results
  - `IScatterGatherRunner` interface with `ExecuteAsync` method returning `Either<EncinaError, ScatterGatherResult<T>>`
  - `ScatterGatherBuilder` fluent API for defining scatter-gather operations:
    - `ScatterTo(name, handler)` for adding scatter handlers with multiple overloads (sync/async, with/without Either)
    - `WithPriority(int)` for handler ordering (lower = higher priority)
    - `WithMetadata(key, value)` for handler metadata
    - `ExecuteInParallel(maxDegreeOfParallelism?)` / `ExecuteSequentially()` for execution mode
    - `WithTimeout(TimeSpan)` for operation timeout
  - Four gather strategies via `GatherStrategy` enum:
    - `WaitForAll` - Wait for all handlers to complete (fail on any failure)
    - `WaitForFirst` - Return on first successful response
    - `WaitForQuorum` - Return when quorum count is reached
    - `WaitForAllAllowPartial` - Wait for all, tolerate partial failures
  - Gather configuration via `GatherBuilder`:
    - `GatherAll()` / `GatherFirst()` / `GatherQuorum(count)` / `GatherAllAllowingPartialFailures()`
    - `GatherWith(GatherStrategy, quorumCount?)` for explicit strategy
    - Aggregation methods: `TakeFirst()`, `TakeMin()`, `TakeMax()`, `Aggregate()`, `AggregateSuccessful()`
  - `ScatterGatherResult<TResponse>` with execution metrics:
    - `Response` - Aggregated result
    - `ScatterResults` - List of `ScatterExecutionResult<TResponse>` with handler name, result, duration
    - `SuccessCount`, `FailureCount`, `OperationId`, `TotalDuration`
  - `ScatterGatherOptions` configuration:
    - `DefaultTimeout` (default: 30s)
    - `ExecuteScattersInParallel` (default: true)
    - `MaxDegreeOfParallelism` (default: null/unlimited)
  - `ScatterGatherErrorCodes` for standardized error codes:
    - `scattergather.cancelled`, `scattergather.timed_out`
    - `scattergather.all_scatters_failed`, `scattergather.quorum_not_reached`
    - `scattergather.gather_failed`, `scattergather.scatter_failed`
  - High-performance logging with `LoggerMessage` source generators (EventIds 600-615)
  - DI integration via `MessagingConfiguration.UseScatterGather = true`
  - Comprehensive test coverage: 131 tests (unit, property, contract, guard, load)
  - Benchmarks for scatter-gather performance across strategies
  - Example: a fully working, copy-pastable scatter-gather example is available in
    [docs/examples.md](docs/examples.md). The changelog previously contained
    simplified placeholder snippets; the full examples (with imports and helper
    implementations) live in the documentation to avoid non-compilable copy/paste.

- Content-Based Router (Issue #64):
  - Enterprise Integration Pattern for routing messages based on content inspection
  - `IContentRouter` interface with `RouteAsync` methods returning `Either<EncinaError, ContentRouterResult<T>>`
  - `ContentRouterBuilder` fluent API for defining routing rules:
    - `When(condition)` / `When(name, condition)` for conditional routes
    - `RouteTo(handler)` with multiple overloads (sync/async, with/without Either)
    - `WithPriority(int)` for route ordering (lower = higher priority)
    - `WithMetadata(key, value)` for route metadata
    - `Default(handler)` / `DefaultResult(value)` for fallback handling
    - `Build()` to create immutable `BuiltContentRouterDefinition<TMessage, TResult>`
  - `ContentRouterOptions` configuration:
    - `ThrowOnNoMatch` - Return error when no route matches (default: true)
    - `AllowMultipleMatches` - Execute all matching routes (default: false)
    - `EvaluateInParallel` - Parallel route execution with `MaxDegreeOfParallelism`
  - `ContentRouterResult<TResult>` with execution metrics:
    - `RouteResults` - List of `RouteExecutionResult<TResult>` with route name, result, duration
    - `MatchedRouteCount`, `TotalDuration`, `UsedDefaultRoute`
  - `RouteDefinition<TMessage, TResult>` for route configuration
  - `ContentRouterErrorCodes` for standardized error codes:
    - `contentrouter.no_matching_route`, `contentrouter.route_execution_failed`
    - `contentrouter.cancelled`, `contentrouter.invalid_configuration`
  - High-performance logging with `LoggerMessage` source generators
  - DI integration via `MessagingConfiguration.UseContentRouter = true`
  - Comprehensive test coverage: 117 tests (unit, integration, property, contract, guard, load)
  - Benchmarks for routing performance
  - Example:

    ```csharp
    // RouteTo accepts both sync and async handlers
    var definition = ContentRouterBuilder.Create<Order, string>()
        .When("HighValue", o => o.Total > 10000)
            .WithPriority(1)
            .RouteTo(async (o, ct) => await ProcessHighValueOrder(o, ct))
        .When("International", o => o.IsInternational)
            .WithPriority(2)
            .RouteTo(async (o, ct) => await Task.FromResult(Right<EncinaError, string>("InternationalHandler")))
        .Default(async (o, ct) => await Task.FromResult(Right<EncinaError, string>("StandardHandler")))
        .Build();

    var result = await router.RouteAsync(definition, order);
    ```

- Distributed Lock Abstractions (Issue #55):
  - **Encina.DistributedLock** - Core abstractions for distributed locking
    - `IDistributedLockProvider` interface with `TryAcquireAsync`, `AcquireAsync`, `IsLockedAsync`, `ExtendAsync`
    - `ILockHandle` interface with lock metadata (`Resource`, `LockId`, `AcquiredAtUtc`, `ExpiresAtUtc`, `IsReleased`)
    - `LockAcquisitionException` for lock acquisition failures
    - `DistributedLockOptions` with `KeyPrefix`, `DefaultExpiry`, `DefaultWait`, `DefaultRetry`, `ProviderHealthCheck`
    - DI registration via `AddEncinaDistributedLock()`
  - **Encina.DistributedLock.InMemory** - In-memory provider for testing
    - `InMemoryDistributedLockProvider` with `ConcurrentDictionary` storage
    - `InMemoryLockOptions` with `WarnOnUse` flag
    - `TimeProvider` injection for testability
    - DI registration via `AddEncinaDistributedLockInMemory()`
  - **Encina.DistributedLock.Redis** - Redis provider for production
    - `RedisDistributedLockProvider` using StackExchange.Redis
    - Lua scripts for atomic lock release (owner verification)
    - Wire-compatible with Redis, Garnet, Valkey, Dragonfly, KeyDB
    - `RedisLockOptions` with `Database` and `KeyPrefix`
    - `RedisDistributedLockHealthCheck` implementing `IEncinaHealthCheck`
    - DI registration via `AddEncinaDistributedLockRedis()`
  - **Encina.DistributedLock.SqlServer** - SQL Server provider
    - `SqlServerDistributedLockProvider` using `sp_getapplock`/`sp_releaseapplock`
    - Session-scoped locks with automatic release on connection close
    - `SqlServerLockOptions` with `ConnectionString` and `KeyPrefix`
    - `SqlServerDistributedLockHealthCheck` implementing `IEncinaHealthCheck`
    - DI registration via `AddEncinaDistributedLockSqlServer()`
  - Updated `Encina.Caching` to reference `Encina.DistributedLock` abstractions
  - Comprehensive test coverage:
    - Unit tests for all providers
    - Integration tests with Testcontainers (Redis, SQL Server)
    - Property-based tests with FsCheck
    - Contract tests for `IDistributedLockProvider`
    - Guard clause tests for all public APIs
    - Load tests for high concurrency scenarios
    - Benchmarks for performance validation
  - Full documentation with usage examples

- Module-scoped Pipeline Behaviors (Issue #58):
  - **IModulePipelineBehavior<TModule, TRequest, TResponse>** interface for module-specific behaviors
  - **ModuleBehaviorAdapter** wraps module behaviors and filters execution by module ownership
  - **IModuleHandlerRegistry** maps handler types to their owning modules via assembly association
  - Request context extensions for module information:
    - `GetModuleName()` for retrieving current module name
    - `WithModuleName()` for setting module name (overloads for string and IModule)
    - `IsInModule()` for checking if context is in a specific module
  - DI extension methods:
    - `AddEncinaModuleBehavior<TModule, TRequest, TResponse, TBehavior>()` for registration
    - Overload with `ServiceLifetime` parameter for custom lifetimes
  - Case-insensitive module name matching
  - Null Object pattern with `NullModuleHandlerRegistry` for when modules aren't configured
  - Comprehensive test coverage: 100 module-related unit tests

- AWS Lambda Integration (Issue #60):
  - **Encina.AwsLambda** package for serverless function execution on AWS
  - API Gateway integration with result-to-response extensions:
    - `ToApiGatewayResponse<T>()` for standard 200 OK responses
    - `ToCreatedResponse<T>()` for 201 Created with Location header
    - `ToNoContentResponse()` for 204 No Content responses
    - `ToHttpApiResponse<T>()` for HTTP API (V2) responses
  - `ToProblemDetailsResponse()` for RFC 7807 compliant error responses
  - SQS trigger support with batch processing:
    - `ProcessBatchAsync<T>()` for partial batch failure reporting via `SQSBatchResponse`
    - `ProcessAllAsync()` for all-or-nothing processing
    - `DeserializeMessage<T>()` for type-safe message deserialization
    - Automatic `BatchItemFailures` for failed message IDs
  - EventBridge (CloudWatch Events) integration:
    - `ProcessAsync<TDetail, TResult>()` for strongly-typed event handling
    - `ProcessRawAsync<TDetail, TResult>()` for raw JSON event processing
    - `GetMetadata()` for extracting event metadata
    - `EventBridgeMetadata` class with Id, Source, DetailType, Account, Region, Time
  - `LambdaContextExtensions` for context information access:
    - `GetCorrelationId()`, `GetUserId()`, `GetTenantId()`
    - `GetAwsRequestId()`, `GetFunctionName()`, `GetRemainingTimeMs()`
  - `EncinaAwsLambdaOptions` for configuration:
    - `EnableRequestContextEnrichment` toggle
    - Customizable header names (`CorrelationIdHeader`, `TenantIdHeader`)
    - Claim types (`UserIdClaimType`, `TenantIdClaimType`)
    - `IncludeExceptionDetailsInResponse` for development debugging
    - `UseApiGatewayV2Format`, `EnableSqsBatchItemFailures` toggles
    - `ProviderHealthCheck` configuration
  - `AwsLambdaHealthCheck` implementing `IEncinaHealthCheck`
  - Error code to HTTP status mapping:
    - `validation.*` → 400 Bad Request
    - `authorization.unauthenticated` → 401 Unauthorized
    - `authorization.*` → 403 Forbidden
    - `*.not_found`, `*.missing` → 404 Not Found
    - `*.conflict`, `*.already_exists`, `*.duplicate` → 409 Conflict
  - DI registration via `AddEncinaAwsLambda()`
  - Comprehensive test coverage: 97 unit, 21 contract, 10 property, 21 guard tests
  - Benchmarks for API Gateway response creation performance
  - Full documentation with examples for API Gateway, SQS, and EventBridge triggers

- Azure Functions Integration (Issue #59):
  - **Encina.AzureFunctions** package for serverless function execution
  - HTTP Trigger integration with automatic result-to-response conversion:
    - `ToHttpResponseData<T>()` for standard responses
    - `ToCreatedResponse<T>()` for 201 Created with Location header
    - `ToNoContentResponse()` for 204 No Content responses
  - `ToProblemDetailsResponse()` for RFC 7807 compliant error responses
  - `EncinaFunctionMiddleware` for request context enrichment:
    - Automatic correlation ID extraction/generation
    - User ID extraction from claims
    - Tenant ID extraction from headers or claims
    - Structured logging for function execution
  - `FunctionContextExtensions` for context information access:
    - `GetCorrelationId()`, `GetUserId()`, `GetTenantId()`, `GetInvocationId()`
  - `EncinaAzureFunctionsOptions` for configuration:
    - `EnableRequestContextEnrichment` toggle
    - Customizable header names and claim types
    - `IncludeExceptionDetailsInResponse` for development
    - `ProviderHealthCheck` configuration
  - `AzureFunctionsHealthCheck` implementing `IEncinaHealthCheck`
  - Error code to HTTP status mapping:
    - `validation.*` → 400 Bad Request
    - `authorization.unauthenticated` → 401 Unauthorized
    - `authorization.*` → 403 Forbidden
    - `*.not_found`, `*.missing` → 404 Not Found
    - `*.conflict`, `*.already_exists`, `*.duplicate` → 409 Conflict
  - DI registration via `AddEncinaAzureFunctions()`
  - Middleware registration via `builder.UseEncinaMiddleware()`
  - Comprehensive test coverage: unit, contract, property, guard, benchmarks
  - Full documentation with examples for HTTP, Queue, and Timer triggers

- Durable Functions Integration (Issue #61):
  - Azure Durable Functions support with Railway Oriented Programming (ROP)
  - `OrchestrationContextExtensions` for ROP-compatible activity calls:
    - `CallEncinaActivityAsync<TInput, TResult>()` for Either-returning activities
    - `CallEncinaActivityWithResultAsync<TInput, TResult>()` for ActivityResult activities
    - `CallEncinaSubOrchestratorAsync<TInput, TResult>()` for sub-orchestrators
    - `WaitForEncinaExternalEventAsync<T>()` for external events with timeout
    - `CreateRetryOptions()` for retry configuration
    - `GetCorrelationId()` for instance ID access
  - `ActivityResult<T>` serializable wrapper for Either results:
    - `Success()` and `Failure()` factory methods
    - `ToEither()` for conversion back to Either
    - `ToActivityResult()` extension for Either conversion
  - `DurableSagaBuilder` fluent API for saga workflows:
    - `Step()` for adding saga steps
    - `Execute()` and `Compensate()` for activity configuration
    - `WithRetry()` for step-level retry options
    - `SkipCompensationOnFailure()` for idempotent operations
    - `WithTimeout()` for saga-level timeout
    - `WithDefaultRetryOptions()` for default retry configuration
    - Automatic compensation in reverse order on failure
  - `DurableSaga<TData>` executable saga with `ExecuteAsync()`
  - `DurableSagaError` with original error and compensation results
  - Fan-out/fan-in pattern extensions:
    - `FanOutAsync<TInput, TResult>()` for parallel activity execution
    - `FanOutAllAsync<TInput, TResult>()` requiring all to succeed
    - `FanOutFirstSuccessAsync<TInput, TResult>()` returning first success
    - `FanOutMultipleAsync<T1, T2>()` for different activities in parallel
    - `Partition<T>()` for separating successes from failures
  - `DurableFunctionsOptions` for configuration:
    - `DefaultMaxRetries`, `DefaultFirstRetryInterval`, `DefaultBackoffCoefficient`
    - `DefaultMaxRetryInterval`, `ContinueCompensationOnError`, `DefaultSagaTimeout`
    - `ProviderHealthCheck` configuration
  - `DurableFunctionsHealthCheck` implementing `IEncinaHealthCheck`
  - DI registration via `AddEncinaDurableFunctions()`
  - Comprehensive test coverage: 124 unit, 58 contract, 27 property, 19 guard tests
  - Full documentation with examples for orchestrations, sagas, and fan-out/fan-in

- Routing Slip Pattern for Dynamic Message Routing (Issue #62):
  - `RoutingSlipBuilder` fluent API for defining routing slips with inline step definitions
  - `RoutingSlipStepBuilder` for configuring individual steps with execute and compensate functions
  - `BuiltRoutingSlipDefinition<TData>` immutable definition ready for execution
  - `RoutingSlipStepDefinition<TData>` representing a single step in the itinerary
  - `RoutingSlipContext<TData>` for dynamic route modification during execution:
    - `AddStep()`, `AddStepNext()`, `InsertStep()` for adding steps
    - `RemoveStepAt()`, `ClearRemainingSteps()` for removing steps
    - `GetRemainingStepNames()` for inspecting itinerary
  - `RoutingSlipActivityEntry<TData>` for activity log with compensation data
  - `RoutingSlipResult<TData>` with execution metrics:
    - `RoutingSlipId`, `FinalData`, `StepsExecuted`, `StepsAdded`, `StepsRemoved`
    - `Duration`, `ActivityLog`
  - `IRoutingSlipRunner` interface and `RoutingSlipRunner` implementation with:
    - Step-by-step execution with dynamic modification tracking
    - Automatic compensation in reverse order on failure
    - Configurable compensation failure handling
    - High-performance logging with LoggerMessage (EventIds 400-415)
  - `RoutingSlipOptions` for configuration:
    - `DefaultTimeout`, `StuckCheckInterval`, `StuckThreshold`, `BatchSize`
    - `ContinueCompensationOnFailure` (default: true)
  - `RoutingSlipStatus` constants: Running, Completed, Compensating, Compensated, Failed, TimedOut
  - `RoutingSlipErrorCodes` with error codes:
    - `routingslip.not_found`, `routingslip.invalid_status`, `routingslip.step_failed`
    - `routingslip.compensation_failed`, `routingslip.timeout`
    - `routingslip.handler.cancelled`, `routingslip.handler.failed`
  - DI integration via `MessagingConfiguration.UseRoutingSlips`
  - Comprehensive test coverage: 137+ tests (unit, property, guard)
  - Example:

    ```csharp
    var definition = RoutingSlipBuilder.Create<OrderData>("ProcessOrder")
        .Step("Validate Order")
            .Execute(async (data, ctx, ct) => {
                // Validation logic
                return Right<EncinaError, OrderData>(data);
            })
        .Step("Process Payment")
            .Execute(async (data, ctx, ct) => {
                // Dynamically add verification step if needed
                if (data.RequiresVerification)
                    ctx.AddStepNext(verificationStep);
                return Right<EncinaError, OrderData>(data);
            })
            .Compensate(async (data, ctx, ct) => await RefundPaymentAsync(data))
        .Step("Ship Order")
            .Execute(async (data, ctx, ct) => {
                data.TrackingNumber = await ShipAsync(data);
                return Right<EncinaError, OrderData>(data);
            })
            .Compensate(async (data, ctx, ct) => await CancelShipmentAsync(data))
        .OnCompletion(async (data, ctx, ct) => await NotifyCompletedAsync(data))
        .WithTimeout(TimeSpan.FromMinutes(5))
        .Build();

    var result = await runner.RunAsync(definition, new OrderData());
    ```

- Event Versioning/Upcasting for Schema Evolution (Issue #37):
  - `IEventUpcaster` marker interface for event upcasters with `SourceEventTypeName`, `TargetEventType`, `SourceEventType`
  - `IEventUpcaster<TFrom, TTo>` strongly-typed generic interface with `Upcast(TFrom)` method
  - `EventUpcasterBase<TFrom, TTo>` abstract base class wrapping Marten's `EventUpcaster<TFrom, TTo>`
  - `LambdaEventUpcaster<TFrom, TTo>` for inline lambda-based upcasting without dedicated classes
  - `EventUpcasterRegistry` for discovering and managing event upcasters:
    - `Register<TUpcaster>()`, `Register(Type)`, `Register(IEventUpcaster)` registration methods
    - `TryRegister()` for non-throwing duplicate handling
    - `GetUpcasterForEventType(string)`, `GetAllUpcasters()`, `HasUpcasterFor(string)` lookup methods
    - `ScanAndRegister(Assembly)` for automatic assembly scanning
  - `EventVersioningOptions` for configuration:
    - `Enabled` toggle (default: false)
    - `ThrowOnUpcastFailure` option (default: true)
    - `AddUpcaster<TUpcaster>()` for type registration
    - `AddUpcaster<TFrom, TTo>(Func<TFrom, TTo>)` for inline lambda registration
    - `ScanAssembly(Assembly)`, `ScanAssemblies(params Assembly[])` for assembly scanning
    - `ApplyTo(EventUpcasterRegistry)` for applying configuration to registry
  - `ConfigureMartenEventVersioning` as `IConfigureOptions<StoreOptions>` for Marten integration
  - `EventVersioningErrorCodes` with error codes:
    - `event.versioning.upcast_failed`, `event.versioning.upcaster_not_found`
    - `event.versioning.registration_failed`, `event.versioning.duplicate_upcaster`
    - `event.versioning.invalid_configuration`
  - `VersioningLog` high-performance logging with LoggerMessage source generators (EventIds 100-129)
  - DI integration via `AddEventVersioning()` internal method
  - `AddEventUpcaster<TUpcaster>()` extension method for individual upcaster registration
  - Comprehensive test coverage: 85+ tests (unit, property, contract, guard, integration)
  - Example:

    ```csharp
    // Define upcaster class
    public class OrderCreatedV1ToV2Upcaster : EventUpcasterBase<OrderCreatedV1, OrderCreatedV2>
    {
        protected override OrderCreatedV2 Upcast(OrderCreatedV1 old)
            => new(old.OrderId, old.CustomerName, Email: "unknown@example.com");
    }

    // Configure with class-based upcasters
    services.AddEncinaMarten(options =>
    {
        options.EventVersioning.Enabled = true;
        options.EventVersioning.AddUpcaster<OrderCreatedV1ToV2Upcaster>();
        options.EventVersioning.ScanAssembly(typeof(Program).Assembly);
    });

    // Or use inline lambda for simple transformations
    services.AddEncinaMarten(options =>
    {
        options.EventVersioning.Enabled = true;
        options.EventVersioning.AddUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "migrated@example.com"));
    });
    ```

- Snapshotting for large aggregates (Issue #52):
  - `ISnapshotable<TAggregate>` marker interface for aggregates supporting snapshots
  - `ISnapshot<TAggregate>` interface with `AggregateId`, `Version`, `CreatedAtUtc` properties
  - `Snapshot<TAggregate>` sealed class storing aggregate state at a specific version
  - `SnapshotOptions` for global and per-aggregate configuration:
    - `Enabled` - toggle snapshotting (default: false)
    - `SnapshotEvery` - event threshold for snapshot creation (default: 100)
    - `KeepSnapshots` - retention limit (default: 3, 0 = keep all)
    - `AsyncSnapshotCreation` - async vs sync snapshot creation (default: true)
    - `ConfigureAggregate<T>(snapshotEvery, keepSnapshots)` for per-aggregate overrides
  - `ISnapshotStore<TAggregate>` interface for snapshot storage:
    - `SaveAsync`, `GetLatestAsync`, `PruneAsync` with ROP error handling
  - `MartenSnapshotStore<TAggregate>` Marten-based implementation:
    - PostgreSQL document storage via Marten
    - Composite key: `{AggregateType}:{AggregateId}:{Version}`
    - Automatic pruning of old snapshots
  - `SnapshotEnvelope<TAggregate>` document wrapper for Marten storage
  - `SnapshotAwareAggregateRepository<TAggregate>` for optimized aggregate loading:
    - Loads from latest snapshot + replays only subsequent events
    - Automatic snapshot creation when threshold exceeded
    - Falls back to standard event replay if no snapshot exists
  - `SnapshotErrorCodes` with standardized error codes:
    - `snapshot.load_failed`, `snapshot.save_failed`, `snapshot.prune_failed`, `snapshot.invalid_state`
  - High-performance logging with `LoggerMessage` source generators (EventIds 100-159)
  - DI registration via `AddSnapshotableAggregate<TAggregate>()`
  - Comprehensive test coverage: 121 unit tests, property tests, contract tests, guard clause tests
  - Integration tests with Testcontainers/PostgreSQL
  - Example:

    ```csharp
    // Enable snapshotting for aggregates
    services.AddEncinaMarten(options =>
    {
        options.Snapshots.Enabled = true;
        options.Snapshots.SnapshotEvery = 100;
        options.Snapshots.KeepSnapshots = 3;

        // Per-aggregate configuration
        options.Snapshots.ConfigureAggregate<Order>(
            snapshotEvery: 50,
            keepSnapshots: 5);
    });

    // Register snapshotable aggregate
    services.AddSnapshotableAggregate<Order>();

    // Aggregate must implement ISnapshotable<TAggregate>
    public class Order : AggregateBase, ISnapshotable<Order>
    {
        // ... standard aggregate implementation
    }
    ```

- Projections/Read Models for CQRS read side (Issue #36):
  - `IReadModel` interface for read model abstraction with `Guid Id` property
  - `IReadModel<TId>` generic variant for strongly-typed identifiers
  - `IProjection<TReadModel>` interface with `ProjectionName` property
  - `IProjectionHandler<TEvent, TReadModel>` for handling events on existing read models:
    - `Apply(TEvent, TReadModel, ProjectionContext)` method
  - `IProjectionCreator<TEvent, TReadModel>` for creating read models from events:
    - `Create(TEvent, ProjectionContext)` method
  - `IProjectionDeleter<TEvent, TReadModel>` for conditional deletion:
    - `ShouldDelete(TEvent, TReadModel, ProjectionContext)` method
  - `ProjectionContext` with event metadata:
    - `StreamId`, `SequenceNumber`, `GlobalPosition`, `Timestamp`
    - `EventType`, `CorrelationId`, `CausationId`, `Metadata`
  - `IReadModelRepository<TReadModel>` for read model persistence:
    - `GetByIdAsync`, `GetByIdsAsync`, `QueryAsync`
    - `StoreAsync`, `StoreManyAsync`, `DeleteAsync`, `DeleteAllAsync`
    - `ExistsAsync`, `CountAsync`
    - ROP-based error handling with `Either<EncinaError, T>`
  - `IProjectionManager` for projection lifecycle management:
    - `RebuildAsync<TReadModel>` with configurable options
    - `GetStatusAsync`, `GetAllStatusesAsync` for monitoring
    - `StartAsync`, `StopAsync`, `PauseAsync`, `ResumeAsync` lifecycle methods
  - `RebuildOptions` for rebuild configuration:
    - `BatchSize` (default 1000), `DeleteExisting`, `OnProgress` callback
    - `StartPosition`, `EndPosition` for incremental rebuilds
    - `RunInBackground` for async rebuilding
  - `ProjectionStatus` with state tracking:
    - `State` enum: `Stopped`, `Starting`, `Running`, `CatchingUp`, `Rebuilding`, `Paused`, `Faulted`, `Stopping`
    - `LastProcessedPosition`, `EventsProcessed`, `LastUpdatedAtUtc`, `ErrorMessage`
  - `ProjectionRegistry` for projection registration and discovery:
    - `Register<TProjection, TReadModel>()` method
    - `GetProjectionsForEvent(Type)`, `GetProjectionForReadModel<T>()`, `GetAllProjections()`
  - `IInlineProjectionDispatcher` for synchronous projection updates:
    - `DispatchAsync(object, ProjectionContext)` for single event
    - `DispatchManyAsync(IEnumerable<(object, ProjectionContext)>)` for batch
  - Marten implementations:
    - `MartenReadModelRepository<TReadModel>` with Marten IDocumentSession
    - `MartenProjectionManager` with event stream processing
    - `MartenInlineProjectionDispatcher` for inline projection updates
  - `ProjectionOptions` for configuration:
    - `EnableInlineProjections`, `RebuildOnStartup`
    - `DefaultBatchSize`, `MaxConcurrentRebuilds`
    - `OnProjectionFaulted` callback
  - `IProjectionRegistrar` interface for startup registration
  - DI registration via `AddProjection<TProjection, TReadModel>()`
  - High-performance logging with `LoggerMessage` attributes
  - 80 tests: 30 unit, 22 property-based, 11 contract, 17 guard clause
  - **Note**: Example uses target-typed `new()` and `with` expression (requires read model to be a `record` type). See [Language Requirements](#language-requirements).
  - **Context-to-Model Mapping**: `ctx.StreamId` is a `Guid` (non-nullable struct, defaults to `Guid.Empty`). This maps directly to `IReadModel.Id` to correlate read models with their source aggregates. Guard against `Guid.Empty` if your domain requires a valid stream ID.
  - **Null Handling**: Event properties (e.g., `e.CustomerName`) may be null depending on your domain model. The `Create` method should validate required fields with `ArgumentNullException.ThrowIfNull()` or use null-coalescing for optional fields. The example below demonstrates defensive validation.
  - Example:

    ```csharp
    // Define a read model (must be a record to use 'with' expression)
    public record OrderSummary : IReadModel
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    // Define a projection with input validation
    public class OrderSummaryProjection :
        IProjection<OrderSummary>,
        IProjectionCreator<OrderCreated, OrderSummary>,
        IProjectionHandler<OrderItemAdded, OrderSummary>
    {
        public string ProjectionName => "OrderSummary";

        public OrderSummary Create(OrderCreated e, ProjectionContext ctx)
        {
            // StreamId is a non-nullable Guid; validate against Guid.Empty only if your domain requires
            // Guid.Empty typically indicates "no meaningful identifier" - domain logic decides if this is valid
            if (ctx.StreamId == Guid.Empty)
                throw new ArgumentException("StreamId must represent a valid aggregate identifier", nameof(ctx));

            // Validate required event properties
            ArgumentNullException.ThrowIfNull(e.CustomerName, nameof(e.CustomerName));

            return new() { Id = ctx.StreamId, CustomerName = e.CustomerName };
        }

        public OrderSummary Apply(OrderItemAdded e, OrderSummary m, ProjectionContext ctx) =>
            m with { TotalAmount = m.TotalAmount + e.Price * e.Quantity };
    }

    // Register and use
    services.AddEncinaMarten(options => {
        options.Projections.EnableInlineProjections = true;
    }).AddProjection<OrderSummaryProjection, OrderSummary>();

    // Query read models
    var summary = await repository.GetByIdAsync(orderId);
    ```

- Bulkhead Isolation Pattern (Issue #53):
  - `BulkheadAttribute` for attribute-based bulkhead configuration:
    - `MaxConcurrency` - Maximum parallel executions allowed (default: 10)
    - `MaxQueuedActions` - Additional requests that can wait in queue (default: 20)
    - `QueueTimeoutMs` - Maximum time to wait in queue (default: 30000ms)
  - `IBulkheadManager` interface for bulkhead management:
    - `TryAcquireAsync` - Acquire permit with timeout and cancellation support
    - `GetMetrics` - Get current bulkhead metrics (concurrency, queue, rejection rate)
    - `Reset` - Reset bulkhead state for a key
  - `BulkheadManager` implementation with `SemaphoreSlim`:
    - Thread-safe concurrent dictionary for per-key bulkhead isolation
    - Automatic permit release via `IDisposable` pattern
    - `TimeProvider` injection for testability
  - `BulkheadPipelineBehavior<TRequest, TResponse>` for automatic bulkhead enforcement
  - `BulkheadAcquireResult` record struct with factory methods:
    - `Acquired()` - Successful acquisition with releaser
    - `RejectedBulkheadFull()` - Both concurrency and queue limits reached
    - `RejectedQueueTimeout()` - Queue wait timeout exceeded
    - `RejectedCancelled()` - Request cancelled while waiting
  - `BulkheadMetrics` record struct with calculated properties:
    - `ConcurrencyUtilization` - Percentage of concurrency capacity in use
    - `QueueUtilization` - Percentage of queue capacity in use
    - `RejectionRate` - Total rejection rate as percentage
  - `BulkheadRejectionReason` enum (`None`, `BulkheadFull`, `QueueTimeout`, `Cancelled`)
  - Automatic DI registration via `AddEncinaPolly()` (singleton manager)
  - Comprehensive test coverage: unit, integration, property-based, contract, guard, load tests
  - Performance benchmarks for acquire/release operations
  - Example:

    ```csharp
    // Limit payment processing to 10 concurrent executions
    [Bulkhead(MaxConcurrency = 10, MaxQueuedActions = 20)]
    public record ProcessPaymentCommand(PaymentData Data) : ICommand<PaymentResult>;

    // Limit external API calls with custom timeout
    [Bulkhead(MaxConcurrency = 5, MaxQueuedActions = 10, QueueTimeoutMs = 5000)]
    public record CallExternalApiQuery(string Endpoint) : IRequest<ApiResponse>;
    ```

- Dead Letter Queue (Issue #42):
  - `IDeadLetterMessage` interface for dead letter message abstraction
  - `IDeadLetterStore` interface for provider-agnostic storage:
    - `AddAsync`, `GetAsync`, `GetMessagesAsync`, `GetCountAsync`
    - `MarkAsReplayedAsync`, `DeleteAsync`, `DeleteExpiredAsync`
    - Pagination support with `skip` and `take` parameters
  - `IDeadLetterMessageFactory` for creating messages from failed requests
  - `DeadLetterFilter` for querying messages:
    - Factory methods: `All`, `FromSource`, `Since`, `ByCorrelationId`
    - Filter by source pattern, request type, correlation ID, date range
    - `ExcludeReplayed` option for pending messages only
  - `DeadLetterOptions` for configuration:
    - `RetentionPeriod` - how long to keep messages (default: 30 days)
    - `CleanupInterval` - background cleanup frequency (default: 1 hour)
    - `EnableAutomaticCleanup` - toggle cleanup processor
    - Integration flags: `IntegrateWithRecoverability`, `IntegrateWithOutbox`, etc.
    - `OnDeadLetter` callback for custom notifications
  - `DeadLetterOrchestrator` for coordinating DLQ operations
  - `IDeadLetterManager` with message replay capabilities:
    - `ReplayAsync(messageId)` - replay single message
    - `ReplayAllAsync(filter)` - batch replay with filter
    - `GetStatisticsAsync()` - queue statistics
    - `CleanupExpiredAsync()` - manual cleanup
  - `DeadLetterManager` implementation with reflection-based replay
  - `ReplayResult` and `BatchReplayResult` for replay operation results
  - `DeadLetterStatistics` with counts by source pattern
  - `DeadLetterHealthCheck` with warning/critical thresholds:
    - Configurable `WarningThreshold` (default: 10 messages)
    - Configurable `CriticalThreshold` (default: 100 messages)
    - `OldMessageThreshold` for stale message detection
  - `DeadLetterCleanupProcessor` background service for automatic cleanup
  - `DeadLetterSourcePatterns` constants: Recoverability, Outbox, Inbox, Scheduling, Saga, Choreography
  - `DeadLetterErrorCodes` for standardized error codes
  - High-performance logging with `LoggerMessage` attributes
  - DI registration via `AddEncinaDeadLetterQueue<TStore, TFactory>()`
  - Comprehensive test coverage: 75 unit tests, 11 integration tests, 22 property tests, 12 contract tests
  - **Built-in vs Custom Implementations**:
    - **Built-in (Testing)**: `FakeDeadLetterStore` in `Encina.Testing.Fakes` - In-memory store for unit/integration tests
    - **Custom (Production)**: Implement `IDeadLetterStore` and `IDeadLetterMessageFactory` for your persistence layer (EF Core, Dapper, ADO.NET, NoSQL, etc.)
    - **API Location**: Interface contracts in `Encina.Messaging.DeadLetter` namespace ([IDeadLetterStore.cs](src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs), [IDeadLetterMessageFactory.cs](src/Encina.Messaging/DeadLetter/IDeadLetterMessageFactory.cs))
    - **Contract Tests**: Use `IDeadLetterStoreContractTests` base class to verify custom implementations
    - **Sample Implementation**: See `InMemoryDeadLetterStore` in [DeadLetterIntegrationTests.cs](tests/Encina.Tests/Integration/DeadLetterIntegrationTests.cs) for implementation reference
  - Example (Testing with built-in FakeDeadLetterStore):

    ```csharp
    // Testing scenario: Use built-in FakeDeadLetterStore
    services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, FakeDeadLetterMessageFactory>(options =>
    {
        options.RetentionPeriod = TimeSpan.FromDays(30);
        options.EnableAutomaticCleanup = false; // Disable for testing
    });
    ```

  - Example (Production with custom store):

    ```csharp
    // Production scenario: Implement IDeadLetterStore for your persistence layer
    public class MyEfCoreDeadLetterStore : IDeadLetterStore
    {
        private readonly MyDbContext _context;
        public MyEfCoreDeadLetterStore(MyDbContext context) => _context = context;
        
        public async Task AddAsync(IDeadLetterMessage message, CancellationToken ct = default)
        {
            _context.DeadLetterMessages.Add(MapToEntity(message));
            await _context.SaveChangesAsync(ct);
        }
        // ... implement remaining interface methods
    }

    // Register with custom implementations
    services.AddEncinaDeadLetterQueue<MyEfCoreDeadLetterStore, MyDeadLetterMessageFactory>(options =>
    {
        options.RetentionPeriod = TimeSpan.FromDays(30);
        options.EnableAutomaticCleanup = true;
        options.IntegrateWithRecoverability = true;
        options.OnDeadLetter = async (msg, ct) =>
            await alertService.SendAlertAsync($"Message dead-lettered: {msg.RequestType}");
    });

    // Query and replay
    var stats = await manager.GetStatisticsAsync();
    var result = await manager.ReplayAsync(messageId);
    ```

- Low-Ceremony Sagas (Issue #41):
  - `SagaDefinition.Create<TData>(sagaType)` fluent API for defining sagas inline
  - `SagaStepBuilder<TData>` with `Execute()` and `Compensate()` methods
  - `ISagaRunner` interface for executing saga definitions
  - `SagaRunner` implementation with full lifecycle management:
    - Sequential step execution with data flow between steps
    - Automatic compensation in reverse order on failure
    - Exception handling with compensation continuation
  - `SagaResult<TData>` record with `SagaId`, `Data`, and `StepsExecuted`
  - Simplified overloads without `IRequestContext` parameter
  - Optional timeout configuration via `WithTimeout(TimeSpan)`
  - Auto-generated step names (`Step 1`, `Step 2`, etc.) when not specified
  - High-performance logging with `LoggerMessage` attributes
  - Full test coverage: unit, property-based, and contract tests
  - Automatic DI registration when `UseSagas = true`
  - Example:

    ```csharp
    var saga = SagaDefinition.Create<OrderData>("ProcessOrder")
        .Step("Reserve Inventory")
            .Execute(async (data, ct) => /* ... */)
            .Compensate(async (data, ct) => /* ... */)
        .Step("Process Payment")
            .Execute(async (data, ct) => /* ... */)
            .Compensate(async (data, ct) => /* ... */)
        .WithTimeout(TimeSpan.FromMinutes(5))
        .Build();

    // SagaResult<TData> contains IsSuccess, Data (final state), Error (if failed),
    // SagaId, and StepsExecuted count for observability
    var result = await sagaRunner.RunAsync(saga, initialData);

    if (result.IsSuccess)
    {
        // Saga completed successfully - continue business flow
        logger.LogInformation("Order {OrderId} processed. Steps: {Steps}", 
            result.Data.OrderId, result.StepsExecuted);
        await notificationService.SendConfirmationAsync(result.Data);
    }
    else
    {
        // Saga failed and compensations ran - handle the failure
        logger.LogError("Order saga failed: {Error}", result.Error?.Message);
        await alertService.NotifyFailureAsync(result.SagaId, result.Error);
    }
    ```

- Automatic Rate Limiting with Adaptive Throttling (Issue #40):
  - `RateLimitAttribute` with configurable properties:
    - `MaxRequestsPerWindow` - Maximum requests allowed in the time window
    - `WindowSizeSeconds` - Duration of the sliding window
    - `ErrorThresholdPercent` - Error rate threshold for adaptive throttling (default: 50%)
    - `CooldownSeconds` - Duration to remain in throttled state (default: 30s)
    - `RampUpFactor` - Rate of capacity increase during recovery (default: 1.5x)
    - `EnableAdaptiveThrottling` - Toggle adaptive behavior (default: true)
    - `MinimumThroughputForThrottling` - Minimum requests before error rate is calculated
  - `IRateLimiter` interface with `AcquireAsync`, `RecordSuccess`, `RecordFailure`, `GetState`, `Reset`
  - `AdaptiveRateLimiter` implementation with:
    - Sliding window rate limiting algorithm
    - State machine: `Normal` → `Throttled` → `Recovering` → `Normal`
    - Thread-safe `ConcurrentDictionary` for per-key state management
    - Automatic outage detection via error rate monitoring
    - Gradual recovery with configurable ramp-up
  - `RateLimitingPipelineBehavior<TRequest, TResponse>` for automatic rate limiting
  - `RateLimitResult` record struct with `Allowed()` and `Denied()` factory methods
  - `RateLimitState` enum (`Normal`, `Throttled`, `Recovering`)
  - `EncinaErrorCodes.RateLimitExceeded` error code
  - Automatic DI registration as singleton (shared state across requests)
  - Comprehensive test coverage: 104 unit tests, 22 property tests, 22 contract tests, 10 guard tests, 4 load tests
  - Performance benchmarks for rate limiter operations
- AggregateTestBase for Event Sourcing testing (Issue #46):
  - `AggregateTestBase<TAggregate, TId>` base class for Given/When/Then testing pattern
  - `AggregateTestBase<TAggregate>` convenience class for Guid identifiers
  - `Given(params object[] events)` for setting up event history
  - `GivenEmpty()` for testing aggregate creation scenarios
  - `When(Action<TAggregate>)` and `WhenAsync(Func<TAggregate, Task>)` for command execution
  - `Then<TEvent>()` and `Then<TEvent>(Action<TEvent>)` for event assertions
  - `ThenEvents(params Type[])` for verifying event sequence
  - `ThenNoEvents()` for idempotency testing
  - `ThenState(Action<TAggregate>)` for state assertions
  - `ThenThrows<TException>()` for exception assertions
  - `GetUncommittedEvents()` and `GetUncommittedEvents<TEvent>()` for direct access
  - Located in `Encina.Testing.EventSourcing` namespace
  - 75 tests covering unit, property, contract, and guard clause scenarios
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
- Module Health Checks (Issue #114):
  - `IModuleWithHealthChecks` interface for modules to expose health checks
  - `IModuleHealthCheck` interface for module-specific health checks with `ModuleName` property
  - `AddEncinaModuleHealthChecks()` extension method for registering all module health checks
  - `AddEncinaModuleHealthChecks<TModule>()` for registering specific module health checks
  - Automatic tagging with `encina`, `ready`, and `modules` tags
  - Integration with ASP.NET Core health check endpoints
- Health Checks Integration Guide (Issue #115):
  - Comprehensive documentation for integrating Encina with AspNetCore.HealthChecks.* packages
  - Examples for microservice and modular monolith architectures
  - Kubernetes probes configuration (liveness, readiness, startup)
  - Recommended NuGet packages table for databases, caches, message brokers, cloud services
  - Best practices for health check organization and tagging
  - Located at `docs/guides/health-checks.md`
- Saga Not Found Handler support (Issue #43):
  - `IHandleSagaNotFound<TMessage>` interface for custom handling when saga correlation fails
  - `SagaNotFoundContext` with `Ignore()` and `MoveToDeadLetterAsync()` actions
  - `SagaNotFoundAction` enum (`None`, `Ignored`, `MovedToDeadLetter`)
  - `ISagaNotFoundDispatcher` for invoking registered handlers
  - `SagaErrorCodes.HandlerCancelled` and `SagaErrorCodes.HandlerFailed` error codes
  - Automatic DI registration when `UseSagas` is enabled
- Delegate Cache Optimization benchmarks (Issue #49):
  - New `CacheOptimizationBenchmarks.cs` for validating cache performance improvements
  - Benchmarks for TryGetValue vs GetOrAdd patterns
  - Type check caching comparison benchmarks
- Recoverability Pipeline (Issue #39):
  - Two-phase retry strategy: immediate retries (in-memory) + delayed retries (persistent/scheduled)
  - `RecoverabilityOptions` with configurable immediate retries (default 3), delayed retries (30s, 5m, 30m, 2h), exponential backoff, and jitter
  - `IErrorClassifier` interface with `DefaultErrorClassifier` for classifying errors as Transient/Permanent/Unknown
  - Error classification by exception type (TimeoutException → Transient, ArgumentException → Permanent)
  - Error classification by HTTP status codes (5xx → Transient, 4xx → Permanent)
  - Error classification by message patterns ("timeout", "connection" → Transient)
  - `RecoverabilityContext` for tracking retry state and history
  - `FailedMessage` record for dead letter queue handling with full context
  - `RecoverabilityPipelineBehavior<TRequest, TResponse>` pipeline behavior
  - `IDelayedRetryScheduler`, `IDelayedRetryStore`, `IDelayedRetryMessage` abstractions
  - `DelayedRetryScheduler` and `DelayedRetryProcessor` (BackgroundService) implementations
  - `OnPermanentFailure` callback for DLQ integration
  - Opt-in via `MessagingConfiguration.UseRecoverability = true`
  - 64 unit tests covering all recoverability scenarios

### Changed

- **Test Framework Migration from FluentAssertions to Shouldly** (Issue #495):
  - **BREAKING**: Replaced FluentAssertions with Shouldly across all 114 test projects
  - Motivation: FluentAssertions adopted commercial licensing ($130/developer/year) in January 2025
  - Shouldly remains MIT-licensed and is a mature, actively maintained library
  - All assertion syntax migrated:
    - `.Should().Be(x)` → `.ShouldBe(x)`
    - `.Should().BeTrue()` → `.ShouldBeTrue()`
    - `.Should().Throw<T>()` → `Should.Throw<T>(action)`
    - `.Should().HaveCount(n)` → `.Count.ShouldBe(n)` or `.ShouldHaveCount(n)`
    - `FluentActions.Invoking(...).Should().Throw<T>()` → `Should.Throw<T>(() => ...)`
  - Custom `Encina.Testing.Shouldly` package provides `Either<TLeft, TRight>` assertion extensions
  - Tests verified: 1000+ tests passing across core projects

- **Validation Architecture Consolidation** (Issue #229) - Remove duplicate validation behaviors:
  - **BREAKING**: Removed `Encina.FluentValidation.ValidationPipelineBehavior<TRequest, TResponse>` (use centralized `Encina.Validation.ValidationPipelineBehavior<,>`)
  - **BREAKING**: Removed `Encina.DataAnnotations.DataAnnotationsValidationBehavior<TRequest, TResponse>` (use centralized behavior)
  - **BREAKING**: Removed `Encina.MiniValidator.MiniValidationBehavior<TRequest, TResponse>` (use centralized behavior)
  - All validation now goes through `ValidationOrchestrator` + provider-specific `IValidationProvider`
  - DRY: Single `ValidationPipelineBehavior` in `Encina.Validation` namespace
  - Consistent error handling across all validation providers

- **Milestone Reorganization**: Phase 2 (364 issues) split into 10 incremental milestones:
  - v0.10.0 — DDD Foundations (31 issues)
  - v0.11.0 — Testing Infrastructure (25 issues)
  - v0.12.0 — Database & Repository (22 issues)
  - v0.13.0 — Security & Compliance (25 issues)
  - v0.14.0 — Cloud-Native & Aspire (23 issues)
  - v0.15.0 — Messaging & EIP (71 issues)
  - v0.16.0 — Multi-Tenancy & Modular (21 issues)
  - v0.17.0 — AI/LLM Patterns (16 issues)
  - v0.18.0 — Developer Experience (43 issues)
  - v0.19.0 — Observability & Resilience (87 issues)

- **Performance**: Optimized delegate caches to minimize reflection and boxing (Issue #49):
  - TryGetValue-before-GetOrAdd pattern on ConcurrentDictionary to avoid delegate allocation on cache hits
  - Cached `GetRequestKind` type checks to avoid repeated `IsAssignableFrom` calls on hot paths
  - Applied to both `RequestDispatcher` and `NotificationDispatcher`

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

## [0.9.0]
