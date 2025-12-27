# Changelog

All notable changes to Encina will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> **Note**: Encina is in pre-1.0 development. Version numbers below represent conceptual milestones, not published releases. The first official release will be v1.0.0.

## [Unreleased]

### Added

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
  - Example:
    ```csharp
    // Define a read model
    public class OrderSummary : IReadModel
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // Define a projection
    public class OrderSummaryProjection :
        IProjection<OrderSummary>,
        IProjectionCreator<OrderCreated, OrderSummary>,
        IProjectionHandler<OrderItemAdded, OrderSummary>
    {
        public string ProjectionName => "OrderSummary";

        public OrderSummary Create(OrderCreated e, ProjectionContext ctx) =>
            new() { Id = ctx.StreamId, CustomerName = e.CustomerName };

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
  - Example:
    ```csharp
    // Configure DLQ
    services.AddEncinaDeadLetterQueue<DeadLetterStoreEF, DeadLetterMessageFactoryEF>(options =>
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

    var result = await sagaRunner.RunAsync(saga, initialData);
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
