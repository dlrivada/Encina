# SimpleMediator Roadmap

**Last Updated**: 2025-12-22
**Version**: Pre-1.0 (breaking changes allowed)
**Future Name**: Encina (to be renamed before 1.0)

---

## Vision

SimpleMediator (future: **Encina**) is a functional mediation library for .NET that enables building modern applications with **Railway Oriented Programming** as the core philosophy.

### Design Principles

- **Functional First**: Pure ROP with `Either<MediatorError, T>` as first-class citizen
- **Explicit over Implicit**: Code should be clear and predictable
- **Performance Conscious**: Zero-allocation hot paths, Expression tree compilation
- **Composable**: Behaviors are small, composable units
- **Pay-for-What-You-Use**: All features are opt-in

---

## Project Status: 90% to Pre-1.0

| Category | Packages | Status |
|----------|----------|--------|
| Core & Validation | 5 | ✅ Production |
| Web Integration | 3 | ✅ Production |
| Database Providers | 12 | ✅ Production |
| Messaging Transports | 10 | ✅ Production |
| Caching | 8 | ✅ Production |
| Job Scheduling | 2 | ✅ Production |
| Resilience | 3 | ✅ Production |
| Event Sourcing | 2 | ✅ Production |
| Observability | 1 | ✅ Production |
| **Developer Tooling** | 0/3 | 📋 Pending |
| **EDA Enhancements** | 0/4 | 📋 Pending |
| **Microservices Enhancements** | 0/4 | 📋 Pending |
| **Modular Monolith** | 0/1 | 📋 Pending |
| **Serverless** | 0/2 | 📋 Pending |
| **DDD Tactical Patterns** | 0/1 | 📋 Pending |
| **TDD Tooling** | 0/1 | 📋 Pending |
| **Enterprise Messaging** | 0/1 | 📋 Pending |
| **Event Sourcing Advanced** | 0/1 | 📋 Pending |
| **Performance (Source Gen)** | 0/1 | 📋 Pending |
| **Enterprise Integration Patterns** | 0/1 | 📋 Pending |
| **Self-Sufficient Architecture** | 0/1 | 📋 Pending |
| **Competitive Edge** | 0/1 | 📋 Pending |

### Quality Metrics

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Line Coverage | 67.1% | ≥85% | 🟡 Needs work |
| Branch Coverage | 70.9% | ≥80% | 🟡 Needs work |
| Mutation Score | 79.75% | ≥80% | ✅ Achieved |
| Build Warnings | 0 | 0 | ✅ Perfect |
| Tests | 3,803 | ~5,000+ | 🟡 In progress |

### Test Coverage

- **Core Tests**: 265 passing
- **Database Provider Tests**: 1,763 passing (10 providers)
- **Caching Tests**: 367 passing
- **Total**: 3,000+ tests

---

## Completed Features

> Detailed implementation history: [docs/history/2025-12.md](docs/history/2025-12.md)
> Version history: [CHANGELOG.md](CHANGELOG.md)

### Core (5 packages)

- SimpleMediator Core - ROP, pipelines, CQRS
- FluentValidation, DataAnnotations, MiniValidator, GuardClauses

### Web (3 packages)

- AspNetCore - Middleware, authorization, Problem Details
- SignalR - Real-time notifications
- ~~MassTransit~~ (deprecated) - See "Competitive Edge"

### Database (12 packages)

- EntityFrameworkCore, MongoDB
- Dapper: SqlServer, PostgreSQL, MySQL, Sqlite, Oracle
- ADO: SqlServer, PostgreSQL, MySQL, Sqlite, Oracle
- Messaging abstractions (Outbox, Inbox, Sagas, Choreography)

### Messaging (10 packages)

- RabbitMQ, AzureServiceBus, AmazonSQS, Kafka
- Redis.PubSub, InMemory, NATS, MQTT
- gRPC, GraphQL
- ~~Wolverine, NServiceBus, MassTransit~~ (deprecated) - See "Competitive Edge"

### Caching (8 packages)

- Core, Memory, Hybrid
- Redis, Valkey, KeyDB, Dragonfly, Garnet

### Resilience (3 packages)

- Extensions.Resilience, Polly, Refit

### Event Sourcing (2 packages)

- Marten, EventStoreDB

### Observability (1 package)

- OpenTelemetry - Distributed tracing and metrics

### Other Features (in Core)

- Stream Requests (IAsyncEnumerable)
- Parallel Notification Dispatch strategies
- Choreography Sagas abstractions (in Messaging)

---

## In Progress

### Test Architecture Refactoring

**Status**: 🔄 In Progress

Restructuring all test projects to use Testcontainers for real database integration testing.

**Completed**:

- ✅ SimpleMediator.TestInfrastructure with shared fixtures
- ✅ Dapper.Sqlite tests refactored (187 tests, 4 projects)
- ✅ Architecture established (1 project per test type)

**Pending**:

- ⏳ Testcontainers fixtures for SQL Server, PostgreSQL, MySQL, Oracle
- ⏳ Remaining provider tests (9 databases × 4 test types)

---

## Pending Features (Pre-1.0)

### Developer Tooling (0% complete)

| Package | Purpose | Priority |
|---------|---------|----------|
| SimpleMediator.Cli | Command-line scaffolding & analysis | ⭐⭐⭐⭐ |
| SimpleMediator.Testing | MediatorFixture fluent API | ⭐⭐⭐⭐ |
| SimpleMediator.OpenApi | Auto-generation from handlers | ⭐⭐⭐ |

### Core Improvements

| Task | Priority | Complexity |
|------|----------|------------|
| Refactor `SimpleMediator.Publish` with guards | ⭐⭐⭐ | Low |
| Optimize delegate caches (minimize reflection) | ⭐⭐⭐ | Medium |
| Replace `object? Details` with `ImmutableDictionary` | ⭐⭐⭐ | Medium |

### Testing Excellence

| Task | Current | Target |
|------|---------|--------|
| Line Coverage | 67.1% | ≥85% |
| Mutation Score | 79.75% | ≥95% |
| Property-based tests | Partial | Complete |
| Load tests | Partial | All providers |

### Event-Driven Architecture Enhancements

| Feature | Package | Priority | Notes |
|---------|---------|----------|-------|
| **Projections/Read Models** | SimpleMediator.Projections | ⭐⭐⭐⭐ | Abstractions for CQRS read side |
| **Event Versioning** | EventStoreDB, Marten | ⭐⭐⭐⭐ | Upcasting, schema evolution |
| **Snapshotting** | EventStoreDB, Marten | ⭐⭐⭐ | For large aggregates |
| **Dead Letter Queue** | Messaging providers | ⭐⭐⭐ | Enhanced DLQ handling |

### Microservices Enhancements

| Feature | Package | Priority | Notes |
|---------|---------|----------|-------|
| **Health Check Abstractions** | Core / AspNetCore | ⭐⭐⭐ | IHealthCheck integration for handler health |
| **Bulkhead Isolation** | Polly | ⭐⭐⭐ | Parallel execution isolation |
| **API Versioning Helpers** | AspNetCore | ⭐⭐ | Contract evolution support |
| **Distributed Lock Abstractions** | SimpleMediator.DistributedLock | ⭐⭐ | IDistributedLock interface |

> **Note**: Service Discovery, Secret Management, and Configuration will be provided natively through the "Self-Sufficient Architecture" package, eliminating the need for external sidecars.

### Modular Monolith Support

**Package**: `SimpleMediator.Modules`

Enable true modular monolith architecture with explicit module boundaries, lifecycle management, and controlled inter-module communication.

#### Core Abstractions

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| `IModule` interface | ⭐⭐⭐⭐⭐ | Low | Module definition with Name, Assembly, ConfigureServices |
| `IModuleRegistry` | ⭐⭐⭐⭐⭐ | Low | Runtime module discovery and introspection |
| Module lifecycle hooks | ⭐⭐⭐⭐ | Low | OnStartAsync/OnStopAsync for initialization |
| Module-scoped behaviors | ⭐⭐⭐⭐ | Medium | Apply behaviors only to specific modules |
| Event routing with filters | ⭐⭐⭐ | Medium | Selective notification subscription per module |
| Module contracts | ⭐⭐⭐ | High | Compile-time validation of inter-module dependencies |
| Anti-Corruption Layers | ⭐⭐ | High | Translation between module boundaries |

#### Proposed API

```csharp
// Module definition
public interface IModule
{
    string Name { get; }
    Assembly Assembly { get; }
    void ConfigureServices(IServiceCollection services);
    Task OnStartAsync(CancellationToken ct);
    Task OnStopAsync(CancellationToken ct);
}

// Registration
services.AddSimpleMediator()
    .AddModules(modules =>
    {
        modules.Register<OrdersModule>();
        modules.Register<InvoicingModule>();
        modules.Register<ShippingModule>();

        // Explicit contracts between modules
        modules.DefineContract<OrdersModule, InvoicingModule>(contract =>
        {
            contract.Publishes<OrderPlacedEvent>();
            contract.Publishes<OrderCancelledEvent>();
        });
    });

// Event with module scope
[ModuleEvent(SourceModule = "Orders", TargetModules = new[] { "Invoicing", "Shipping" })]
public record OrderPlacedEvent(Guid OrderId) : INotification;
```

#### Current Support (Without Package)

Applications can still use modular patterns today:

- ✅ Assembly-based handler discovery per module
- ✅ `IRequestContext` with TenantId/UserId for isolation
- ✅ Outbox/Inbox/Sagas for reliable inter-module messaging
- ✅ Notifications for event-driven communication

#### Gaps Addressed by This Package

- ❌ → ✅ Explicit module registry and discovery
- ❌ → ✅ Module lifecycle management
- ❌ → ✅ Handler isolation (prevent cross-module collisions)
- ❌ → ✅ Selective event routing (not global broadcast)
- ❌ → ✅ Module boundary enforcement
- ❌ → ✅ Module-scoped pipeline behaviors

### Serverless Integration

First-class support for serverless architectures with Azure Functions and AWS Lambda.

#### Packages

| Package | Priority | Notes |
|---------|----------|-------|
| `SimpleMediator.AzureFunctions` | ⭐⭐⭐⭐ | Azure Functions integration (.NET 10, Flex Consumption) |
| `SimpleMediator.AwsLambda` | ⭐⭐⭐⭐ | AWS Lambda integration (managed instances, containers) |

#### Features

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| Function triggers as handlers | ⭐⭐⭐⭐⭐ | Low | HTTP, Timer, Queue, Blob triggers dispatch to mediator |
| Cold start optimization | ⭐⭐⭐⭐ | Medium | Pre-warming, lazy initialization strategies |
| Durable Functions orchestration | ⭐⭐⭐⭐ | Medium | Saga-like workflows with Durable Functions |
| Step Functions integration | ⭐⭐⭐ | Medium | AWS Step Functions state machine support |
| Context propagation | ⭐⭐⭐⭐⭐ | Low | RequestContext from function context/headers |
| OpenTelemetry integration | ⭐⭐⭐⭐ | Low | Distributed tracing across function invocations |

#### Proposed API

```csharp
// Azure Functions
public class OrderFunctions
{
    private readonly IMediator _mediator;

    [Function("CreateOrder")]
    public async Task<IActionResult> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        var command = await req.ReadFromJsonAsync<CreateOrderCommand>();
        return await _mediator.SendToActionResult(command);
    }

    [Function("ProcessOrderQueue")]
    public async Task ProcessOrder(
        [QueueTrigger("orders")] CreateOrderCommand command,
        FunctionContext context)
    {
        // Context automatically propagated (correlation, tenant, etc.)
        await _mediator.Send(command, context.ToRequestContext());
    }
}

// AWS Lambda
public class OrderHandler : SimpleMediatorLambdaHandler<CreateOrderCommand, OrderResult>
{
    // Automatic serialization, context propagation, error handling
}

// Durable Functions with Sagas
[Function("OrderSagaOrchestrator")]
public async Task<OrderResult> RunOrchestrator(
    [OrchestrationTrigger] TaskOrchestrationContext context)
{
    var saga = new OrderSaga();
    return await _mediator.ExecuteSaga(saga, context);
}
```

#### Current Support (Without Packages)

SimpleMediator works in serverless today but requires manual setup:

- ✅ DI registration in function startup
- ✅ Manual context creation from headers
- ✅ Basic request/response handling

#### Gaps Addressed by These Packages

- ❌ → ✅ Automatic context propagation from function context
- ❌ → ✅ Cold start optimizations (pre-warming behaviors)
- ❌ → ✅ Native trigger-to-handler mapping
- ❌ → ✅ Durable Functions / Step Functions orchestration
- ❌ → ✅ Lambda base classes with automatic serialization
- ❌ → ✅ OpenTelemetry auto-instrumentation for functions

### Domain-Driven Design (DDD) Support

**Package**: `SimpleMediator.DomainModel`

Tactical DDD patterns with first-class ROP integration for building rich domain models.

#### Current Support

| Pattern | Status | Location |
|---------|--------|----------|
| Aggregates | ✅ Strong | `AggregateBase` in Marten/EventStoreDB |
| Domain Events | 🟡 Partial | Via `INotification` + auto-publishing |
| Repositories | ✅ Strong | `IAggregateRepository<T>` with ROP |
| Domain Errors | ✅ Excellent | `MediatorError` + Either monad |
| Value Objects | ❌ Missing | No base class |
| Entities | ❌ Missing | No interface |
| Specifications | ❌ Missing | No support |
| Strongly-Typed IDs | ❌ Missing | No base record |

#### Proposed Abstractions

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| `IDomainEvent` interface | ⭐⭐⭐⭐⭐ | Low | AggregateId, OccurredAtUtc, Version |
| `ValueObject` base record | ⭐⭐⭐⭐⭐ | Low | Immutable, equality by value |
| `Entity<TId>` base class | ⭐⭐⭐⭐ | Low | Identity-based equality |
| `StronglyTypedId<T>` record | ⭐⭐⭐⭐ | Low | Avoid primitive obsession |
| `ISpecification<T>` with ROP | ⭐⭐⭐⭐ | Medium | Composable business rules |
| `IDomainService` marker | ⭐⭐⭐ | Low | Documentation/discovery |
| `EnsureInvariant` in AggregateBase | ⭐⭐⭐⭐ | Low | ROP-based invariant validation |

#### Proposed API

```csharp
// Domain Event with metadata
public interface IDomainEvent : INotification
{
    Guid AggregateId { get; }
    DateTimeOffset OccurredAtUtc { get; }
    int Version { get; }
}

// Value Object (immutable, equality by value)
public abstract record ValueObject
{
    protected abstract IEnumerable<object?> GetAtomicValues();
}

public record Money(decimal Amount, string Currency) : ValueObject
{
    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }
}

// Strongly-Typed ID (avoid primitive obsession)
public abstract record StronglyTypedId<T>(T Value) where T : notnull;

public record OrderId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public static OrderId New() => new(Guid.NewGuid());
}

// Specification Pattern with ROP
public interface ISpecification<T>
{
    Either<MediatorError, bool> IsSatisfiedBy(T entity);
    ISpecification<T> And(ISpecification<T> other);
    ISpecification<T> Or(ISpecification<T> other);
    ISpecification<T> Not();
}

public class OrderMustBeShippable : ISpecification<Order>
{
    public Either<MediatorError, bool> IsSatisfiedBy(Order order) =>
        order.Status == OrderStatus.Paid && order.Items.Any()
            ? true
            : MediatorError.New("order.not_shippable");
}

// Invariant validation in aggregates
public abstract class AggregateBase
{
    protected Either<MediatorError, Unit> EnsureInvariant(
        bool condition, string errorCode, string message) =>
        condition ? Unit.Default : MediatorError.New(errorCode, message);

    public Either<MediatorError, Unit> Ship() =>
        EnsureInvariant(Status == OrderStatus.Paid, "order.not_paid", "Cannot ship unpaid order")
            .Map(_ => { RaiseEvent(new OrderShipped(Id)); return Unit.Default; });
}
```

### Test-Driven Development (TDD) Support

**Package**: `SimpleMediator.Testing`

Fluent testing API for handlers, aggregates, and pipelines with first-class ROP assertions.

#### Current Support

| Component | Status | Location |
|-----------|--------|----------|
| Test Infrastructure | ✅ Excellent | `SimpleMediator.TestInfrastructure` |
| Database Fixtures | ✅ Strong | Testcontainers (5 DBs) |
| Test Builders | ✅ Strong | `OutboxMessageBuilder`, etc. |
| Handler Testing | 🟡 Partial | Basic fixtures only |
| Assertion Extensions | 🟡 Incomplete | Planned |
| MediatorFixture | ❌ Missing | In Developer Tooling |

#### Proposed Abstractions

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| `MediatorFixture` fluent builder | ⭐⭐⭐⭐⭐ | Medium | Configure handlers, behaviors, fakes |
| `AggregateTestBase<T>` | ⭐⭐⭐⭐⭐ | Medium | Given/When/Then for event-sourced aggregates |
| ROP Assertion Extensions | ⭐⭐⭐⭐⭐ | Low | `ShouldBeSuccess()`, `ShouldBeError()` |
| Aggregate Assertions | ⭐⭐⭐⭐ | Low | `ShouldHaveRaisedEvent<T>()` |
| `FakeRepository<T>` | ⭐⭐⭐⭐ | Low | In-memory aggregate store for tests |
| Handler Test Base | ⭐⭐⭐ | Medium | Simplified handler unit testing |

#### Proposed API

```csharp
// MediatorFixture - Fluent test setup
var mediator = new MediatorFixture()
    .WithHandler<CreateOrderHandler>()
    .WithBehavior<ValidationBehavior<CreateOrderCommand, OrderId>>()
    .WithFakeRepository(existingOrder, anotherOrder)
    .Build();

var result = await mediator.Send(new CreateOrderCommand(...));
result.ShouldBeSuccess();

// AggregateTestBase - Given/When/Then for Event Sourcing
public class OrderAggregateTests : AggregateTestBase<Order>
{
    [Fact]
    public void Ship_WhenPaid_RaisesOrderShippedEvent()
    {
        Given(new OrderCreated(orderId), new OrderPaid(orderId));
        When(order => order.Ship());
        Then<OrderShipped>(e => e.OrderId.Should().Be(orderId));
    }

    [Fact]
    public void Ship_WhenNotPaid_ReturnsError()
    {
        Given(new OrderCreated(orderId));
        When(order => order.Ship());
        ThenError("order.not_paid");
    }
}

// ROP Assertion Extensions
public static class MediatorAssertions
{
    public static T ShouldBeSuccess<T>(this Either<MediatorError, T> result);
    public static MediatorError ShouldBeError<T>(this Either<MediatorError, T> result);
    public static void ShouldBeError<T>(this Either<MediatorError, T> result, string code);
}

// Aggregate Assertions
public static class AggregateAssertions
{
    public static void ShouldHaveRaisedEvent<TEvent>(this IAggregate aggregate);
    public static void ShouldHaveRaisedEvent<TEvent>(this IAggregate aggregate, Action<TEvent> assertions);
    public static void ShouldHaveNoUncommittedEvents(this IAggregate aggregate);
}

// Usage
var result = await mediator.Send(command);
result.ShouldBeSuccess();

order.ShouldHaveRaisedEvent<OrderCreated>(e =>
    e.CustomerId.Should().Be(customerId));
```

#### Gaps Addressed by This Package

- ❌ → ✅ Fluent mediator setup for isolated tests
- ❌ → ✅ Given/When/Then syntax for aggregate testing
- ❌ → ✅ Type-safe ROP assertions
- ❌ → ✅ Aggregate event assertions
- ❌ → ✅ In-memory repository fakes
- ❌ → ✅ Reduced boilerplate in test code

### Enterprise Messaging Maturity

**Goal**: Compete with NServiceBus-level enterprise features for mission-critical messaging.

**Package**: `SimpleMediator.Messaging.Enterprise`

#### Recoverability & Error Handling

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **Immediate Retries** | ⭐⭐⭐⭐⭐ | Low | Configurable retry count before delay |
| **Delayed Retries** | ⭐⭐⭐⭐⭐ | Medium | Exponential backoff, custom intervals |
| **Automatic Rate Limiting** | ⭐⭐⭐⭐⭐ | Medium | Detect outages, auto-throttle to 1 msg probe |
| **Poison Message Handling** | ⭐⭐⭐⭐⭐ | Medium | Bypass retries, move to error queue |
| **Error Queue Management** | ⭐⭐⭐⭐ | Medium | Retry/replay from error queue |
| **Message Auditing** | ⭐⭐⭐⭐ | Medium | Centralized audit trail |

#### Saga Enhancements

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **Saga Timeouts** | ⭐⭐⭐⭐⭐ | Medium | `RequestTimeout<T>()` with DateTime/TimeSpan |
| **Saga Not Found Handling** | ⭐⭐⭐⭐ | Low | `IHandleSagaNotFound` for orphan messages |
| **Saga Concurrency Control** | ⭐⭐⭐⭐ | Medium | Optimistic/pessimistic locking |
| **Message Deduplication** | ⭐⭐⭐ | Low | MessageId-based (complements Inbox) |

#### Proposed API

```csharp
// Recoverability configuration
services.AddSimpleMediator(config =>
{
    config.UseRecoverability(r =>
    {
        r.ImmediateRetries(3);
        r.DelayedRetries(5, TimeSpan.FromSeconds(30));
        r.OnPoisonMessage(msg => msg.MoveToErrorQueue());
        r.EnableAutomaticRateLimiting(consecutiveFailures: 10);
    });
});

// Saga with timeout
public class OrderSaga : Saga<OrderSagaState>,
    IHandleTimeout<ShippingDeadlineExpired>
{
    public void Handle(OrderPlaced message)
    {
        Data.OrderId = message.OrderId;
        RequestTimeout<ShippingDeadlineExpired>(TimeSpan.FromDays(7));
    }

    public void Timeout(ShippingDeadlineExpired timeout)
    {
        // Handle timeout - escalate, compensate, etc.
        Publish(new OrderShippingEscalated(Data.OrderId));
    }
}

// Saga not found handler
public class OrderSagaNotFoundHandler : IHandleSagaNotFound
{
    public Task Handle(object message, SagaNotFoundContext context)
    {
        _logger.LogWarning("Saga not found for {MessageType}", message.GetType().Name);
        return Task.CompletedTask;
    }
}
```

### Event Sourcing Advanced

**Goal**: Compete with EventFlow/Marten direct usage for pure event sourcing scenarios.

**Package**: `SimpleMediator.EventSourcing`

#### Subscriptions

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **Catch-up Subscriptions** | ⭐⭐⭐⭐⭐ | Medium | Subscribe from position with checkpoint |
| **Persistent Subscriptions** | ⭐⭐⭐⭐⭐ | High | Server-side, competing consumers |
| **Consumer Groups** | ⭐⭐⭐⭐⭐ | High | Load balancing across consumers |
| **Exactly-once Processing** | ⭐⭐⭐⭐ | Medium | Checkpoint + projection in one transaction |

#### Projections

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **`IProjection<TEvent, TReadModel>`** | ⭐⭐⭐⭐⭐ | Medium | Provider-agnostic projection interface |
| **Inline Projections** | ⭐⭐⭐⭐ | Low | Strong consistency, same transaction |
| **Async Projections** | ⭐⭐⭐⭐⭐ | Medium | Eventual consistency, background daemon |
| **Projection Rebuilding** | ⭐⭐⭐⭐ | Medium | Replay events to rebuild read models |

#### Advanced Features

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **Event Upcasting** | ⭐⭐⭐⭐ | Medium | Transform old events to new schema |
| **Snapshot Strategies** | ⭐⭐⭐⭐ | Medium | Every-N, Time-based, Size-based |
| **Stream Compaction** | ⭐⭐⭐ | Medium | Reduce storage while maintaining state |
| **$by_category Projections** | ⭐⭐⭐ | Low | System projections for event types |

#### Proposed API

```csharp
// Catch-up subscription with checkpoint
public class OrderProjection : IProjection<OrderEvent, OrderReadModel>
{
    public async Task ProjectAsync(OrderEvent @event, ProjectionContext context)
    {
        var readModel = await _db.FindAsync<OrderReadModel>(@event.OrderId);

        readModel = @event switch
        {
            OrderCreated e => new OrderReadModel { Id = e.OrderId, Status = "Created" },
            OrderShipped e => readModel with { Status = "Shipped", ShippedAt = e.ShippedAt },
            _ => readModel
        };

        await _db.SaveAsync(readModel);
        await context.SaveCheckpointAsync(); // Same transaction
    }
}

// Register projections
services.AddSimpleMediator(config =>
{
    config.UseEventSourcing(es =>
    {
        es.AddProjection<OrderProjection>(ProjectionLifecycle.Async);
        es.AddProjection<InventoryProjection>(ProjectionLifecycle.Inline);

        es.UseSnapshotting<Order>(every: 100);
        es.UseUpcaster<OrderCreatedV1, OrderCreatedV2>(Upcasters.OrderCreated);
    });
});

// Competing consumers
services.AddSimpleMediator(config =>
{
    config.UseEventSourcing(es =>
    {
        es.AddPersistentSubscription("$ce-Order", "order-processors", options =>
        {
            options.ConsumerGroup = "order-service";
            options.MaxConcurrency = 10;
            options.BufferSize = 100;
        });
    });
});
```

### Performance: Source Generators

**Goal**: Compete with Mediator/SwitchMediator for maximum performance and NativeAOT support.

**Package**: `SimpleMediator.SourceGenerator`

#### Performance Targets

| Metric | Current | Target | Improvement |
|--------|---------|--------|-------------|
| Startup (600 handlers) | ~24MB alloc | ~145KB alloc | 165x less |
| Handler Discovery | Runtime reflection | Compile-time | 100x faster |
| Dispatch Overhead | Dictionary lookup | Switch statement | Near-zero |
| First Request | Cold | Pre-compiled | Instant |

#### Features

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **Zero-reflection discovery** | ⭐⭐⭐⭐⭐ | High | Source generator scans at build |
| **Compile-time pipeline** | ⭐⭐⭐⭐⭐ | High | Pipeline wiring generated |
| **Switch-based dispatch** | ⭐⭐⭐⭐⭐ | Medium | No dictionary lookup |
| **Zero-allocation dispatch** | ⭐⭐⭐⭐⭐ | High | No closures, no boxing |
| **Compile-time validation** | ⭐⭐⭐⭐⭐ | Medium | Build error if handler missing |
| **NativeAOT support** | ⭐⭐⭐⭐⭐ | Medium | Trimming-friendly, no reflection |
| **ValueTask everywhere** | ⭐⭐⭐⭐ | Low | Sync completion optimization |
| **Pooled async state machines** | ⭐⭐⭐ | Medium | Reduced GC pressure |

#### Proposed Usage

```csharp
// Assembly attribute enables source generation
[assembly: SimpleMediatorSourceGeneration]

// Handlers discovered at compile-time
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, OrderId>
{
    // If this handler is missing, BUILD FAILS (not runtime exception)
}

// Generated code (simplified)
[GeneratedCode("SimpleMediator.SourceGenerator", "1.0.0")]
internal static class MediatorDispatcher
{
    public static ValueTask<Either<MediatorError, TResponse>> Dispatch<TRequest, TResponse>(
        TRequest request,
        IServiceProvider sp,
        CancellationToken ct) where TRequest : IRequest<TResponse>
    {
        // Switch-based dispatch - no dictionary lookup
        return request switch
        {
            CreateOrderCommand cmd => DispatchCreateOrder(cmd, sp, ct),
            GetOrderQuery query => DispatchGetOrder(query, sp, ct),
            // ... all handlers generated at compile-time
            _ => ValueTask.FromResult(Left<MediatorError, TResponse>(
                MediatorError.New("handler.not_found")))
        };
    }
}

// Configuration (optional - defaults work)
services.AddSimpleMediatorSourceGenerated(config =>
{
    config.DefaultLifetime = ServiceLifetime.Singleton; // Best perf
    config.EnablePooledAsyncStateMachines = true;
});
```

#### Compatibility

- ✅ Full API compatibility with reflection-based version
- ✅ Same `IMediator` interface
- ✅ Gradual migration path
- ✅ Can coexist with reflection version during transition

### Enterprise Integration Patterns (EIP)

**Goal**: Support classic enterprise integration patterns from the Hohpe/Woolf book.

**Package**: `SimpleMediator.IntegrationPatterns`

#### Message Routing Patterns

| Pattern | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **Content-Based Router** | ⭐⭐⭐⭐ | Medium | Route based on message content |
| **Routing Slip** | ⭐⭐⭐⭐⭐ | Medium | Dynamic processing sequence |
| **Scatter-Gather** | ⭐⭐⭐⭐ | High | Parallel dispatch, aggregate responses |
| **Splitter** | ⭐⭐⭐⭐ | Medium | Split message into parts |
| **Aggregator** | ⭐⭐⭐⭐ | High | Combine multiple messages |
| **Dynamic Router** | ⭐⭐⭐ | Medium | Self-configuring via control channel |

#### Message Transformation Patterns

| Pattern | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **Message Enricher** | ⭐⭐⭐ | Low | Add data from external sources |
| **Content Filter** | ⭐⭐⭐ | Low | Remove unwanted data |
| **Claim Check** | ⭐⭐⭐ | Medium | Store large payload, pass reference |
| **Normalizer** | ⭐⭐ | Medium | Convert formats to canonical |

#### Messaging Infrastructure Patterns

| Pattern | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **Wire Tap** | ⭐⭐⭐ | Low | Inspect without modifying flow |
| **Message Store** | ⭐⭐⭐ | Medium | Persist for audit/replay |
| **Idempotent Receiver** | ✅ Already exists | - | Inbox pattern |
| **Return Address** | ⭐⭐⭐ | Low | Reply-to pattern |

#### Proposed API

```csharp
// Content-Based Router
public class OrderRouter : IContentBasedRouter<OrderMessage>
{
    public string Route(OrderMessage message) => message.Priority switch
    {
        Priority.High => "high-priority-queue",
        Priority.Normal => "normal-queue",
        Priority.Low => "batch-queue",
        _ => "default-queue"
    };
}

// Routing Slip - dynamic processing sequence
var slip = RoutingSlip.Create()
    .AddStep<ValidateOrder>()
    .AddStep<CheckInventory>()
    .AddStep<ProcessPayment>()
    .AddStep<ShipOrder>()
    .WithCompensation<RefundPayment>(onFailureOf: typeof(ShipOrder));

await _mediator.ExecuteRoutingSlip(order, slip);

// Scatter-Gather - parallel requests, aggregate responses
var request = new GetPriceQuotes(productId);
var responses = await _mediator.ScatterGather<GetPriceQuotes, PriceQuote>(
    request,
    targets: new[] { "supplier-a", "supplier-b", "supplier-c" },
    timeout: TimeSpan.FromSeconds(5),
    minResponses: 2);

var bestPrice = responses.MinBy(r => r.Price);

// Splitter + Aggregator
var orderLines = await _mediator.Split<Order, OrderLine>(
    order,
    splitter: o => o.Lines);

var results = await Task.WhenAll(orderLines.Select(line =>
    _mediator.Send(new ProcessOrderLine(line))));

var summary = await _mediator.Aggregate<OrderLineResult, OrderSummary>(
    results,
    aggregator: (results) => new OrderSummary(results));

// Claim Check - for large payloads
var claimCheck = await _mediator.StorePayload(largeDocument);
await _mediator.Send(new ProcessDocument(claimCheck.Reference));
// Later...
var document = await _mediator.RetrievePayload<Document>(claimCheck.Reference);

// Wire Tap - inspect without modifying
services.AddSimpleMediator(config =>
{
    config.AddWireTap<OrderCommand>(msg =>
        _logger.LogInformation("Order command: {Command}", msg));
});
```

### Self-Sufficient Architecture

**Goal**: Make Encina a complete, self-sufficient solution that doesn't require external sidecars or competing frameworks.

**Rationale**: Dapr and similar sidecars provide capabilities that overlap with Encina's core value proposition. Users shouldn't need to choose between Encina and Dapr - Encina should provide everything needed for distributed systems natively.

**Package**: `SimpleMediator.Infrastructure`

#### Service Discovery

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **`IServiceDiscovery`** | ⭐⭐⭐⭐⭐ | Medium | Resolve service endpoints by name |
| **DNS-based Discovery** | ⭐⭐⭐⭐ | Low | Kubernetes DNS, Docker Compose |
| **Consul Provider** | ⭐⭐⭐⭐ | Medium | HashiCorp Consul integration |
| **Eureka Provider** | ⭐⭐⭐ | Medium | Netflix Eureka for Spring ecosystem |
| **Health-based Routing** | ⭐⭐⭐⭐ | Medium | Only route to healthy instances |

#### Distributed Configuration

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **`IDistributedConfiguration`** | ⭐⭐⭐⭐⭐ | Medium | Centralized config with change notifications |
| **Consul KV Provider** | ⭐⭐⭐⭐ | Low | HashiCorp Consul Key/Value |
| **etcd Provider** | ⭐⭐⭐ | Medium | Kubernetes native config |
| **Azure App Configuration** | ⭐⭐⭐⭐ | Low | Azure-native centralized config |
| **AWS AppConfig** | ⭐⭐⭐ | Low | AWS-native config |
| **Hot Reload** | ⭐⭐⭐⭐⭐ | Medium | Real-time config updates without restart |

#### Secret Management

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **`ISecretManager`** | ⭐⭐⭐⭐⭐ | Medium | Secure secret retrieval with caching |
| **HashiCorp Vault Provider** | ⭐⭐⭐⭐⭐ | Medium | Industry standard secret management |
| **Azure Key Vault Provider** | ⭐⭐⭐⭐ | Low | Azure-native secrets |
| **AWS Secrets Manager** | ⭐⭐⭐⭐ | Low | AWS-native secrets |
| **Secret Rotation** | ⭐⭐⭐ | Medium | Automatic credential rotation |
| **Lease Management** | ⭐⭐⭐ | Medium | Time-limited secret access |

#### Virtual Actors (Optional)

**Package**: `SimpleMediator.Actors`

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **`IActor<TState>`** | ⭐⭐⭐ | High | Stateful, single-threaded actors |
| **Actor Activation/Deactivation** | ⭐⭐⭐ | High | Lifecycle management |
| **Actor Reminders** | ⭐⭐⭐ | Medium | Durable timers |
| **Actor State Persistence** | ⭐⭐⭐ | Medium | Pluggable state stores |
| **Orleans Interop** | ⭐⭐ | High | Compatibility with Microsoft Orleans |

> **Note**: Actors are a specialized pattern. Consider whether sagas + handlers cover most use cases before implementing.

#### Proposed API

```csharp
// Service Discovery
public interface IServiceDiscovery
{
    Task<ServiceEndpoint?> ResolveAsync(string serviceName, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceEndpoint>> ResolveAllAsync(string serviceName, CancellationToken ct = default);
    IAsyncEnumerable<ServiceEndpoint> WatchAsync(string serviceName, CancellationToken ct = default);
}

// Usage in handlers
public class OrderHandler : ICommandHandler<CreateOrderCommand, OrderId>
{
    private readonly IServiceDiscovery _discovery;
    private readonly HttpClient _httpClient;

    public async Task<Either<MediatorError, OrderId>> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var inventoryService = await _discovery.ResolveAsync("inventory-service", ct);
        if (inventoryService is null)
            return MediatorError.New("service.not_found", "Inventory service unavailable");

        var response = await _httpClient.PostAsJsonAsync(
            $"{inventoryService.BaseUrl}/reserve",
            new { command.ProductId, command.Quantity },
            ct);
        // ...
    }
}

// Distributed Configuration
public interface IDistributedConfiguration
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, CancellationToken ct = default);
    IAsyncEnumerable<ConfigurationChange<T>> WatchAsync<T>(string key, CancellationToken ct = default);
}

// Secret Management
public interface ISecretManager
{
    Task<Secret<T>> GetSecretAsync<T>(string path, CancellationToken ct = default);
    Task<Secret<string>> GetSecretAsync(string path, CancellationToken ct = default);
}

public record Secret<T>(T Value, DateTimeOffset? ExpiresAt, bool IsRotatable);

// Registration
services.AddSimpleMediator(config =>
{
    config.UseServiceDiscovery(sd =>
    {
        sd.UseConsul("http://consul:8500");
        sd.UseHealthChecks(healthyOnly: true);
    });

    config.UseDistributedConfiguration(dc =>
    {
        dc.UseConsulKV("http://consul:8500/v1/kv");
        dc.EnableHotReload();
    });

    config.UseSecretManagement(sm =>
    {
        sm.UseVault("https://vault:8200", token: Environment.GetVariable("VAULT_TOKEN"));
        sm.EnableSecretCaching(TimeSpan.FromMinutes(5));
    });
});
```

#### Current Support (Without This Package)

Applications must rely on external solutions:

- ❌ Dapr sidecar (competes with Encina's value proposition)
- ❌ Direct infrastructure SDK integration (tight coupling)
- ❌ Manual endpoint configuration (no dynamic discovery)

#### Gaps Addressed by This Package

- ❌ → ✅ Native service discovery without sidecars
- ❌ → ✅ Centralized configuration with hot reload
- ❌ → ✅ Unified secret management interface
- ❌ → ✅ Provider-agnostic abstractions (switch Consul ↔ Kubernetes ↔ Azure)
- ❌ → ✅ Complete solution for distributed systems

### Competitive Edge: Surpassing Dapr, NServiceBus, MassTransit & Wolverine

**Goal**: Make Encina the definitive choice for .NET distributed systems by natively implementing the best features from all major competitors - and going beyond.

**Strategic Decision**: Deprecate integration packages (`SimpleMediator.Dapr`, `SimpleMediator.NServiceBus`, `SimpleMediator.MassTransit`, `SimpleMediator.Wolverine`) and instead implement their unique capabilities natively in Encina.

#### Competitor Analysis Summary

| Capability | Dapr | NServiceBus | MassTransit | Wolverine | Encina (Current) | Encina (Planned) |
|------------|------|-------------|-------------|-----------|------------------|------------------|
| **Mediator/CQRS** | ❌ | ❌ | ❌ | ✅ | ✅ ROP-first | ✅ |
| **Outbox Pattern** | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Inbox Pattern** | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Sagas (Orchestration)** | ❌ | ✅ | ✅ State Machine | ✅ Low-ceremony | ✅ Basic | ✅ Advanced |
| **Saga Timeouts** | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ |
| **Saga Concurrency** | ❌ | ✅ Optimistic | ✅ | ✅ | ❌ | ✅ |
| **Routing Slip** | ❌ | ❌ | ✅ Courier | ❌ | ❌ | ✅ |
| **Scatter-Gather** | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ |
| **Recoverability** | ❌ | ✅ Enterprise | ✅ | ✅ | 🟡 Basic | ✅ Full |
| **Error Queue Mgmt** | ❌ | ✅ ServicePulse | ❌ | ❌ | ❌ | ✅ Native |
| **Message Auditing** | ❌ | ✅ ServiceControl | ❌ | ❌ | ❌ | ✅ |
| **Scheduled Messages** | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Virtual Actors** | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ Optional |
| **Service Discovery** | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Secret Management** | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Distributed Config** | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Monitoring Dashboard** | ❌ | ✅ ServicePulse | ❌ | ❌ | ❌ | ✅ Encina.Dashboard |
| **Message Flow Viz** | ❌ | ✅ ServiceInsight | ❌ | ❌ | ❌ | ✅ Encina.Dashboard |
| **Source Generators** | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| **NativeAOT** | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |
| **Free/OSS** | ✅ | ❌ Paid | ✅ → ❌ v9 | ✅ | ✅ | ✅ |

#### Features to Implement (Consolidated from All Competitors)

##### From NServiceBus (Enterprise Messaging)

| Feature | Priority | Package | Notes |
|---------|----------|---------|-------|
| **Recoverability Pipeline** | ⭐⭐⭐⭐⭐ | Messaging.Enterprise | Immediate + Delayed retries, custom policies |
| **Automatic Rate Limiting** | ⭐⭐⭐⭐⭐ | Messaging.Enterprise | Detect outages, auto-throttle, probe with 1 msg |
| **Saga Timeouts** | ⭐⭐⭐⭐⭐ | Messaging | `RequestTimeout<T>()` with TimeSpan/DateTime |
| **Saga Not Found Handler** | ⭐⭐⭐⭐ | Messaging | `IHandleSagaNotFound` for orphan messages |
| **Message Auditing** | ⭐⭐⭐⭐ | Messaging.Enterprise | Centralized audit trail for compliance |
| **Encina.Dashboard** | ⭐⭐⭐⭐⭐ | New Package | Web dashboard like ServicePulse |
| **Message Flow Visualization** | ⭐⭐⭐⭐ | Dashboard | Sequence diagrams like ServiceInsight |
| **Error Queue Retry/Delete** | ⭐⭐⭐⭐ | Dashboard | Manual intervention UI |

##### From MassTransit (Distributed Transactions)

| Feature | Priority | Package | Notes |
|---------|----------|---------|-------|
| **Routing Slip (Courier)** | ⭐⭐⭐⭐⭐ | IntegrationPatterns | Dynamic activity sequence with compensation |
| **Scatter-Gather** | ⭐⭐⭐⭐ | IntegrationPatterns | Parallel requests, aggregate responses |
| **Saga State Machine DSL** | ⭐⭐⭐⭐ | Messaging | Fluent state machine syntax |
| **Consumer-per-Queue** | ⭐⭐⭐⭐ | Messaging | Independent retry/DLQ per consumer |
| **In-Memory Outbox** | ⭐⭐⭐ | Messaging | Lighter alternative to DB outbox |

##### From Wolverine (Developer Experience)

| Feature | Priority | Package | Notes |
|---------|----------|---------|-------|
| **Low-Ceremony Sagas** | ⭐⭐⭐⭐⭐ | Messaging | Minimal boilerplate, convention-based |
| **Source Generators** | ⭐⭐⭐⭐⭐ | SourceGenerator | Zero-reflection, NativeAOT support |
| **Cascading Messages** | ⭐⭐⭐⭐ | Core | Return messages from handlers |
| **MediatorOnly Mode** | ⭐⭐⭐⭐ | Core | Disable messaging for pure mediator usage |
| **Durable Agent (Background)** | ⭐⭐⭐⭐ | Messaging | Background daemon for durable messaging |
| **Scheduled Message Testing** | ⭐⭐⭐ | Testing | Test scheduled messages in unit tests |

##### From Dapr (Infrastructure Abstractions)

| Feature | Priority | Package | Notes |
|---------|----------|---------|-------|
| **Service Discovery** | ⭐⭐⭐⭐⭐ | Infrastructure | Consul, DNS, Kubernetes |
| **Secret Management** | ⭐⭐⭐⭐⭐ | Infrastructure | Vault, Azure Key Vault, AWS Secrets |
| **Distributed Configuration** | ⭐⭐⭐⭐⭐ | Infrastructure | Hot reload, multiple providers |
| **Virtual Actors** | ⭐⭐⭐ | Actors (optional) | Stateful actors with reminders |
| **Bindings (100+ integrations)** | ⭐⭐ | Consider | May be too broad - focus on top 10 |

#### Encina.Dashboard: Enterprise Monitoring

**Goal**: Provide ServicePulse/ServiceInsight-level monitoring without commercial licensing.

**Package**: `SimpleMediator.Dashboard`

| Feature | Priority | Complexity | Notes |
|---------|----------|------------|-------|
| **Failed Messages View** | ⭐⭐⭐⭐⭐ | Medium | List, filter, search failed messages |
| **Retry Individual/Batch** | ⭐⭐⭐⭐⭐ | Medium | Retry selected messages |
| **Delete from Error Queue** | ⭐⭐⭐⭐⭐ | Low | Remove poison messages |
| **Message Details** | ⭐⭐⭐⭐⭐ | Low | Headers, body, stack trace, timestamp |
| **Saga State Viewer** | ⭐⭐⭐⭐ | Medium | Current state, history, correlation |
| **Endpoint Health** | ⭐⭐⭐⭐ | Medium | Health status per handler |
| **Message Flow Diagram** | ⭐⭐⭐ | High | Visualize message chains |
| **Throughput Metrics** | ⭐⭐⭐⭐ | Medium | Messages/sec, latency percentiles |
| **Audit Trail Search** | ⭐⭐⭐ | Medium | Search historical messages |

##### Proposed Dashboard API

```csharp
// Registration
services.AddSimpleMediator(config =>
{
    config.UseDashboard(dashboard =>
    {
        dashboard.Endpoint = "/encina-dashboard";
        dashboard.RequireAuthorization("Admin");
        dashboard.EnableAuditTrail();
        dashboard.RetentionPeriod = TimeSpan.FromDays(30);
    });
});

// Failed message retry API
public interface IFailedMessageManager
{
    Task<PagedResult<FailedMessage>> GetFailedMessagesAsync(FailedMessageQuery query, CancellationToken ct);
    Task RetryAsync(Guid messageId, CancellationToken ct);
    Task RetryAllAsync(FailedMessageQuery query, CancellationToken ct);
    Task DeleteAsync(Guid messageId, CancellationToken ct);
    Task<FailedMessageDetails> GetDetailsAsync(Guid messageId, CancellationToken ct);
}

// Saga viewer API
public interface ISagaViewer
{
    Task<PagedResult<SagaInstance>> GetSagasAsync(SagaQuery query, CancellationToken ct);
    Task<SagaDetails> GetSagaDetailsAsync(Guid sagaId, CancellationToken ct);
    Task<IReadOnlyList<SagaHistoryEntry>> GetSagaHistoryAsync(Guid sagaId, CancellationToken ct);
}
```

#### Low-Ceremony Saga Syntax (Wolverine-Inspired)

**Goal**: Make sagas as simple as Wolverine while retaining enterprise features.

```csharp
// Current verbose approach
public class OrderSaga : Saga<OrderSagaState>
{
    public void Handle(OrderPlaced message, ILogger<OrderSaga> logger)
    {
        Data.OrderId = message.OrderId;
        Data.Status = OrderStatus.Placed;
        // Manual state management...
    }
}

// NEW: Low-ceremony approach (Wolverine-style)
public class OrderSaga : Saga<OrderSagaState>
{
    // Convention: First parameter is correlated by SagaId property
    public OrderSagaState Handle(OrderPlaced message) => new()
    {
        OrderId = message.OrderId,
        Status = OrderStatus.Placed
    };

    // Return messages to publish
    public IEnumerable<object> Handle(PaymentReceived message)
    {
        Data.Status = OrderStatus.Paid;
        yield return new ShipOrder(Data.OrderId);

        // Request timeout
        yield return new TimeoutMessage<ShippingDeadline>(TimeSpan.FromDays(7));
    }

    // Timeout handler
    public IEnumerable<object> Timeout(ShippingDeadline timeout)
    {
        if (Data.Status != OrderStatus.Shipped)
        {
            yield return new EscalateOrder(Data.OrderId);
        }
    }

    // Mark saga complete by returning Complete
    public SagaCompletion Handle(OrderDelivered message)
    {
        Data.Status = OrderStatus.Delivered;
        return SagaCompletion.Complete;
    }
}
```

#### Routing Slip with Compensation (MassTransit Courier-style)

```csharp
// Define activities
public class ReserveInventoryActivity : IRoutingSlipActivity<ReserveInventoryArgs>
{
    public async Task<ActivityResult> ExecuteAsync(ReserveInventoryArgs args, ActivityContext context)
    {
        var reserved = await _inventory.ReserveAsync(args.ProductId, args.Quantity);

        // Store compensation data
        context.SetCompensationData(new { args.ProductId, args.Quantity });

        return reserved
            ? ActivityResult.Completed()
            : ActivityResult.Faulted("Insufficient inventory");
    }

    public async Task CompensateAsync(ActivityContext context)
    {
        var data = context.GetCompensationData<dynamic>();
        await _inventory.ReleaseAsync(data.ProductId, data.Quantity);
    }
}

// Build and execute routing slip
var slip = await RoutingSlip.CreateAsync()
    .AddActivity<ValidateOrderActivity>(new { Order = order })
    .AddActivity<ReserveInventoryActivity>(new { order.ProductId, order.Quantity })
    .AddActivity<ProcessPaymentActivity>(new { order.CustomerId, order.Total })
    .AddActivity<ShipOrderActivity>(new { order.ShippingAddress })
    .WithCompensationOrder(CompensationOrder.ReverseExecution)
    .BuildAsync();

var result = await _mediator.ExecuteRoutingSlipAsync(slip);

result.Match(
    success => logger.LogInformation("Order completed: {OrderId}", order.Id),
    failure => logger.LogError("Order failed at {Activity}: {Error}", failure.FailedActivity, failure.Error)
);
```

#### Why Encina Will Win

| Factor | Competitors | Encina |
|--------|-------------|--------|
| **Licensing** | NServiceBus: Paid, MassTransit v9: Paid | **Free forever (MIT)** |
| **Philosophy** | Messaging-first or Sidecar-first | **ROP-first, messaging opt-in** |
| **Learning Curve** | Multiple concepts to learn | **Single coherent model** |
| **Vendor Lock-in** | Tied to specific patterns | **Provider-agnostic** |
| **.NET 10+** | Legacy support burden | **Latest features only** |
| **NativeAOT** | Limited or none | **First-class support** |
| **Monitoring** | Paid (NServiceBus) or DIY | **Free dashboard included** |
| **Self-Sufficient** | Need sidecars (Dapr) | **No external dependencies** |

### Additional Providers

| Package | Priority | Notes |
|---------|----------|-------|
| SimpleMediator.ODBC | ⭐⭐⭐ | Legacy databases |

---

## Strategic Initiatives (Just Before 1.0)

### Renaming: Encina

**Current Name**: SimpleMediator → **New Name**: Encina

**Why Encina?** Spanish word for holm oak - symbolizing strength, resilience, and longevity.

**Checklist**:

- [ ] Rename GitHub repository
- [ ] Update all namespaces
- [ ] Register new NuGet packages
- [ ] Update documentation

**Timeline**: Complete before 1.0 release

---

## Quality & Security

### Implemented

- ✅ CodeQL scanning on every PR
- ✅ SBOM generation workflow
- ✅ Dependabot enabled
- ✅ TreatWarningsAsErrors=true
- ✅ PublicAPI Analyzers
- ✅ LoggerMessage source generators (CA1848 compliance)

### Planned

- [ ] SLSA Level 2 compliance
- [ ] SonarCloud integration
- [ ] Supply chain security (Sigstore/cosign)

---

## Not Implementing / Deprecated

| Feature | Reason |
|---------|--------|
| Generic Variance | Goes against "explicit over implicit" |
| MediatorResult<T> Wrapper | Either<L,R> from LanguageExt is sufficient |
| **SimpleMediator.Dapr** (deprecated) | Dapr competes with Encina's value proposition. See "Self-Sufficient Architecture". |
| **SimpleMediator.NServiceBus** (deprecated) | Enterprise licensing conflicts with free philosophy. See "Competitive Edge". |
| **SimpleMediator.MassTransit** (deprecated) | Overlapping patterns (Outbox/Inbox/Sagas). See "Competitive Edge". |
| **SimpleMediator.Wolverine** (deprecated) | Competing message bus with own patterns. See "Competitive Edge". |

See ADR-004 and ADR-005 for detailed rationale.

### Competitor Integration Packages Deprecation

**Status**: All deprecated (code preserved in `.backup/deprecated-packages/`)

**Packages Deprecated**: `SimpleMediator.Dapr`, `SimpleMediator.NServiceBus`, `SimpleMediator.MassTransit`, `SimpleMediator.Wolverine`

**Rationale**: After careful analysis, these frameworks compete with Encina rather than complement it:

| Framework | Why It Competes | What Encina Does Instead |
|-----------|-----------------|--------------------------|
| **Dapr** | Sidecar provides messaging, state, secrets, discovery | Native implementations via Self-Sufficient Architecture |
| **NServiceBus** | Enterprise messaging with Sagas, Recoverability, ServicePulse | Native enterprise features + free Encina.Dashboard |
| **MassTransit** | State machine sagas, Routing Slip (Courier), Outbox/Inbox | Native low-ceremony sagas + Routing Slip pattern |
| **Wolverine** | Source generators, durable messaging, low-ceremony handlers | Native source generators + cascading messages |

**Problem**: Users choosing "Encina + Competitor" gain minimal additional value but add complexity:

- Duplicate patterns (whose Saga should I use? whose Outbox?)
- Conflicting philosophies (messaging-first vs ROP-first)
- Licensing concerns (NServiceBus paid, MassTransit v9 commercial)

**Solution**: Implement the best features from all competitors natively in Encina (see "Competitive Edge" section).

> **Note**: Source Generators were previously listed here but are now planned (see "Performance: Source Generators" section) to enable NativeAOT and maximum performance scenarios.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Pre-1.0 Policy

Any feature can be added/modified/removed without restrictions.

### Post-1.0 Policy

Breaking changes only in major versions.

---

## References

### Inspiration

- [MediatR](https://github.com/jbogard/MediatR)
- [Wolverine](https://wolverine.netlify.app/)
- [LanguageExt](https://github.com/louthy/language-ext)

### Concepts

- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)

---

**Maintained by**: @dlrivada
**History**: See [docs/history/](docs/history/) for detailed implementation records
**Changelog**: See [CHANGELOG.md](CHANGELOG.md) for version history
