# ADR-007: Extensibility Strategy for External Library Integration

**Status:** Proposed (Pre-1.0)
**Date:** 2025-12-14
**Authors:** Architecture Team
**Supersedes:** None
**Related:** ADR-001 (Railway Oriented Programming), ADR-006 (Pure ROP)
**Note:** This is a pre-1.0 architectural decision. We prioritize best technical solution over backward compatibility.

---

## Context

SimpleMediator aims to be a production-ready mediator library for .NET applications. Modern applications integrate with numerous external libraries and services:

- **Logging:** Serilog, NLog, Microsoft.Extensions.Logging
- **Observability:** OpenTelemetry, Application Insights, Prometheus
- **Validation:** FluentValidation, DataAnnotations
- **Databases:** Entity Framework Core, Dapper, EventStoreDB
- **Caching:** Redis, In-Memory
- **Messaging:** RabbitMQ, Kafka, Azure Service Bus
- **Resilience:** Polly (retry, circuit breaker, rate limiting)
- **Security:** ASP.NET Core Authorization, Identity

Users expect SimpleMediator to integrate seamlessly with these libraries **without forcing it to take dependencies on them**. This is a critical architectural decision that affects:

1. **Core package size** - Should remain minimal
2. **Versioning conflicts** - Avoid dependency hell
3. **Flexibility** - Support multiple implementations (e.g., multiple logging libraries)
4. **Discoverability** - Users should easily find integration patterns

### Key Questions

1. Should integrations be built into the core package or distributed as satellite packages?
2. What extensibility mechanisms should the core provide?
3. Are breaking changes needed to support common integration patterns?
4. How do we handle ambient context (correlation IDs, user context, idempotency keys)?

---

## Decision

**We adopt a layered extensibility strategy with satellite packages:**

### 1. Core Package Responsibilities

`SimpleMediator` (core) provides **extensibility mechanisms** only:

✅ **Included:**

- `IPipelineBehavior<TRequest, TResponse>` - Cross-cutting concerns
- `IRequestPreProcessor<TRequest>` - Pre-execution hooks
- `IRequestPostProcessor<TRequest, TResponse>` - Post-execution hooks
- `IFunctionalFailureDetector` - Domain error extraction for telemetry
- `MediatorDiagnostics` (ActivitySource) - OpenTelemetry integration point
- `MediatorMetrics` (Meter) - Metrics collection
- Dependency injection support via `IServiceCollection`

❌ **NOT Included:**

- Concrete behaviors for validation, caching, logging, etc.
- Dependencies on external libraries (FluentValidation, Polly, Redis, etc.)
- ASP.NET Core-specific features

### 2. Satellite Package Strategy

Integration with external libraries is provided via **optional NuGet packages**:

| Package | Purpose | Dependencies |
|---------|---------|--------------|
| `SimpleMediator.FluentValidation` | Validation behaviors | FluentValidation.DependencyInjectionExtensions |
| `SimpleMediator.EntityFrameworkCore` | Transaction + outbox behaviors | Microsoft.EntityFrameworkCore |
| `SimpleMediator.OpenTelemetry` | Pre-configured OTEL setup | OpenTelemetry.Api, OpenTelemetry.Instrumentation |
| `SimpleMediator.Polly` | Retry/circuit breaker | Polly.Core |
| `SimpleMediator.Caching.Redis` | Redis caching behavior | StackExchange.Redis |
| `SimpleMediator.Caching.Memory` | In-memory caching | Microsoft.Extensions.Caching.Memory |
| `SimpleMediator.Authorization` | ASP.NET Core authorization | Microsoft.AspNetCore.Authorization |
| `SimpleMediator.Idempotency` | Idempotency checking | (abstractions only) |
| `SimpleMediator.RabbitMQ` | RabbitMQ publishing | RabbitMQ.Client |
| `SimpleMediator.Kafka` | Kafka producing | Confluent.Kafka |

**Package Versioning:**

- Each satellite package versions independently
- Compatible with `SimpleMediator >= X.Y` (stated in package description)
- Breaking changes in core require major version bump in satellites

### 3. Request Context (Ambient Metadata)

**Problem:** No standard way to attach ambient context (correlation IDs, user info, idempotency keys).

**Solution (RECOMMENDED - Best Technical Approach):**

Add `IRequestContext` as **explicit parameter** to all pipeline interfaces:

```csharp
namespace SimpleMediator;

/// <summary>
/// Marker interface for requests that carry ambient context metadata.
/// </summary>
/// <remarks>
/// Enables behaviors to access correlation IDs, user context, idempotency keys,
/// and custom metadata without modifying core mediator signatures.
/// </remarks>
public interface IHasMetadata
{
    /// <summary>
    /// Ambient context metadata for this request.
    /// </summary>
    IRequestContext Context { get; }
}

/// <summary>
/// Carries ambient context metadata through the pipeline.
/// </summary>
public interface IRequestContext
{
    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// User ID initiating this request (if authenticated).
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Idempotency key for duplicate detection (if applicable).
    /// </summary>
    string? IdempotencyKey { get; }

    /// <summary>
    /// Custom metadata key-value pairs.
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Creates a new context with additional metadata.
    /// </summary>
    IRequestContext WithMetadata(string key, object? value);
}
```

**Usage:**

```csharp
public record CreateOrder(string ProductId, int Quantity)
    : ICommand<OrderCreated>, IHasMetadata
{
    public IRequestContext Context { get; init; } = RequestContext.Current;
}

// Behavior accesses metadata
public sealed class IdempotencyBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IHasMetadata
{
    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var key = request.Context.IdempotencyKey;
        if (string.IsNullOrEmpty(key))
            return await nextStep();

        // Check idempotency store...
    }
}
```

**Trade-offs:**

- ✅ Non-breaking (opt-in via interface)
- ✅ No performance impact for requests that don't need metadata
- ❌ Not all requests will have context (behaviors must check interface)
- ❌ Requires request types to implement interface

**Future (2.0 - Breaking Change):**

Add `IRequestContext` as explicit parameter to all pipeline methods:

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,  // NEW
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken);
}
```

This makes context **always available** without marker interfaces, but breaks all existing behaviors.

### 4. Handler Metadata Access

**Problem:** Behaviors cannot inspect handler attributes (e.g., `[CacheFor(Minutes=5)]`).

**Solution (Non-Breaking):**

Add metadata provider service:

```csharp
namespace SimpleMediator;

/// <summary>
/// Provides metadata about request handlers for introspection in behaviors.
/// </summary>
public interface IRequestHandlerMetadataProvider
{
    /// <summary>
    /// Gets metadata for the handler of the specified request type.
    /// </summary>
    HandlerMetadata GetMetadata<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>;
}

/// <summary>
/// Metadata about a registered handler.
/// </summary>
public sealed class HandlerMetadata
{
    /// <summary>
    /// Handler implementation type.
    /// </summary>
    public Type HandlerType { get; init; }

    /// <summary>
    /// Custom attributes applied to the handler.
    /// </summary>
    public IReadOnlyList<Attribute> Attributes { get; init; }

    /// <summary>
    /// Service lifetime of the handler (Scoped, Transient, Singleton).
    /// </summary>
    public ServiceLifetime Lifetime { get; init; }
}
```

**Usage:**

```csharp
// Handler with attribute
[CacheFor(Minutes = 5)]
public sealed class GetProductByIdHandler : IRequestHandler<GetProductById, Product>
{
    // ...
}

// Behavior reads attribute
public sealed class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IRequestHandlerMetadataProvider _metadataProvider;
    private readonly IDistributedCache _cache;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var metadata = _metadataProvider.GetMetadata<TRequest, TResponse>();
        var cacheAttr = metadata.Attributes.OfType<CacheForAttribute>().FirstOrDefault();

        if (cacheAttr is null)
            return await nextStep();  // No caching for this handler

        var cacheKey = GenerateCacheKey(request);
        var ttl = TimeSpan.FromMinutes(cacheAttr.Minutes);

        // Cache logic...
    }
}
```

**Trade-offs:**

- ✅ Non-breaking (new service, opt-in)
- ✅ Enables attribute-driven behavior configuration
- ❌ Requires reflection (but cached)
- ❌ Attribute discovery happens at runtime

---

## Consequences

### Positive

✅ **Minimal Core Package**

- `SimpleMediator` has zero dependencies on external libraries
- Small package size, fast installation
- No versioning conflicts with user dependencies

✅ **Flexibility**

- Users choose which integrations to install
- Can use custom implementations (e.g., NLog instead of Serilog)
- No forced dependencies

✅ **Discoverability**

- Satellite packages appear in NuGet search
- Clear naming convention: `SimpleMediator.{Integration}`
- Package descriptions explain purpose

✅ **Non-Breaking Evolution**

- Marker interfaces (`IHasMetadata`) are opt-in
- Metadata provider is new service (doesn't affect existing code)
- Breaking changes deferred to 2.0

✅ **Testability**

- Behaviors can be tested independently
- Mock implementations for `IRequestContext`, `IRequestHandlerMetadataProvider`
- No hidden static state

### Negative

❌ **More Packages to Maintain**

- Each satellite package needs documentation, tests, versioning
- Updates to core may require updating satellites
- More GitHub repos or monorepo complexity

❌ **Discovery Friction**

- New users must discover satellite packages
- Not as obvious as "batteries included" core
- Requires better documentation

❌ **Migration Effort (2.0)**

- Adding `IRequestContext` parameter will break all behaviors
- Users will need to update custom behaviors
- Migration guide required

❌ **Incomplete Metadata Support (1.0)**

- `IHasMetadata` is opt-in (not all requests will have context)
- Behaviors must check `if (request is IHasMetadata)`
- Less ergonomic than built-in context parameter

---

## Alternatives Considered

### Alternative 1: Batteries-Included Core

**Approach:** Include all common integrations in `SimpleMediator` package.

**Pros:**

- One package to install
- Everything works out of the box
- Easier for beginners

**Cons:**

- ❌ Massive dependency tree (FluentValidation + Polly + StackExchange.Redis + EF Core + ...)
- ❌ Versioning conflicts inevitable
- ❌ Forces upgrades when any dependency updates
- ❌ Violates single responsibility principle

**Rejected:** This approach doesn't scale and creates dependency hell.

### Alternative 2: Plugin Architecture with Dynamic Loading

**Approach:** Load integration assemblies at runtime via `AssemblyLoadContext`.

**Pros:**

- No compile-time dependencies
- True plugin isolation

**Cons:**

- ❌ Complex implementation
- ❌ Performance overhead
- ❌ Poor debugging experience
- ❌ Incompatible with trimming/AOT

**Rejected:** Over-engineered for this use case.

### Alternative 3: Source Generators for Integrations

**Approach:** Generate behaviors at compile time based on configuration.

**Pros:**

- Zero runtime overhead
- Strong typing

**Cons:**

- ❌ Limited flexibility
- ❌ Complex to maintain
- ❌ Poor IDE experience (Roslyn can be slow)

**Rejected:** See ADR-005 for detailed analysis of source generators.

### Alternative 4: Breaking Changes in 1.0

**Approach:** Add `IRequestContext` parameter to all interfaces immediately.

**Pros:**

- Best ergonomics
- No marker interfaces needed
- Context always available

**Cons:**

- ❌ Breaks all existing behaviors (even examples in docs)
- ❌ Migration pain before 1.0 adoption
- ❌ No escape hatch for users who don't need context

**Rejected:** Too disruptive for 1.0. Reserve for 2.0 after user feedback.

---

## Implementation Plan

### Phase 1: Core Enhancements (Before 1.0)

1. ✅ Add `IHasMetadata` interface
2. ✅ Add `IRequestContext` interface and `RequestContext` implementation
3. ✅ Add `IRequestHandlerMetadataProvider` service
4. ✅ Update `RequestDispatcher` to populate handler metadata cache
5. ✅ Write tests for metadata provider
6. ✅ Document metadata patterns in architecture guide

### Phase 2: Satellite Packages (Before 1.0)

Priority order:

1. **`SimpleMediator.FluentValidation`** (Critical)
   - `ValidationBehavior<,>`
   - Extension method: `cfg.AddFluentValidation()`
   - Auto-validator discovery

2. **`SimpleMediator.EntityFrameworkCore`** (Critical)
   - `TransactionBehavior<,>`
   - `OutboxBehavior<,>` (for domain events)
   - `SaveChangesBehavior<,>`

3. **`SimpleMediator.OpenTelemetry`** (High)
   - Pre-configured `ActivitySource` listener
   - Metrics exporter setup
   - Extension method: `services.AddSimpleMediatorTelemetry()`

4. **`SimpleMediator.Polly`** (Medium)
   - `RetryBehavior<,>` with policy configuration
   - `CircuitBreakerBehavior<,>`
   - Attribute-driven: `[Retry(MaxAttempts = 3)]`

5. **`SimpleMediator.Caching.Redis`** (Medium)
   - `RedisCachingBehavior<,>`
   - Cache key generation strategies
   - TTL configuration via attributes

### Phase 3: Documentation (Before 1.0)

1. ✅ Create **Integration Guide** with examples for:
   - FluentValidation
   - EF Core transactions
   - Redis caching
   - Polly resilience
   - OpenTelemetry observability
   - RabbitMQ publishing
   - Idempotency

2. ✅ Create **Samples Repository** with:
   - E-commerce app (EF Core + validation + events)
   - CQRS app (read/write separation)
   - Event-sourced app (EventStoreDB)
   - Microservices app (RabbitMQ)

3. ✅ Write **Migration Guide** from MediatR

### Phase 4: Future Enhancements (2.0)

1. ⏳ Add `IRequestContext context` parameter to all pipeline interfaces (breaking)
2. ⏳ Add `HandlerMetadata metadata` parameter to behaviors (breaking)
3. ⏳ Support `IAsyncEnumerable<T>` for streaming queries
4. ⏳ Add `IRequestMiddleware` for ASP.NET Core integration

---

## Success Criteria

We'll know this decision is successful when:

1. ✅ Core package has zero dependencies on external libraries
2. ✅ At least 5 satellite packages published before 1.0
3. ✅ Integration guide covers 10+ common scenarios
4. ✅ Users report easy integration with their tech stack
5. ✅ No GitHub issues requesting "built-in" integrations (indicates satellite packages work)
6. ✅ Migration from MediatR is straightforward (documented patterns exist)

---

## References

- [Extensibility Analysis](../extensibility-analysis.md) - Detailed analysis of integration patterns
- [ADR-001: Railway Oriented Programming](001-railway-oriented-programming.md) - Error handling foundation
- [ADR-006: Pure ROP Exception Handling](006-pure-rop-exception-handling.md) - Exception philosophy
- [MediatR](https://github.com/jbogard/MediatR) - Inspiration for mediator pattern
- [Polly](https://github.com/App-vNext/Polly) - Resilience library
- [FluentValidation](https://github.com/FluentValidation/FluentValidation) - Validation library
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet) - Observability standard

---

## Appendix A: Example Integrations

### FluentValidation

```csharp
// Install: dotnet add package SimpleMediator.FluentValidation

// Startup
services.AddValidatorsFromAssembly(typeof(CreateOrderValidator).Assembly);
services.AddSimpleMediator(cfg =>
{
    cfg.AddFluentValidation();  // Registers ValidationBehavior<,>
}, assemblies);

// Validator
public sealed class CreateOrderValidator : AbstractValidator<CreateOrder>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

// Request (no changes needed)
public record CreateOrder(string ProductId, int Quantity) : ICommand<OrderCreated>;

// Behavior runs automatically, returns Left<MediatorError> on validation failure
```

### Entity Framework Core Transactions

```csharp
// Install: dotnet add package SimpleMediator.EntityFrameworkCore

// Startup
services.AddDbContext<ApplicationDbContext>();
services.AddSimpleMediator(cfg =>
{
    cfg.AddEntityFrameworkCore<ApplicationDbContext>();  // Registers TransactionBehavior
}, assemblies);

// Automatic transaction for commands
public record CreateOrder(...) : ICommand<OrderCreated>;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrder, OrderCreated>
{
    private readonly ApplicationDbContext _db;

    public async ValueTask<Either<MediatorError, OrderCreated>> Handle(
        CreateOrder request,
        CancellationToken cancellationToken)
    {
        var order = new Order(request.ProductId, request.Quantity);
        await _db.Orders.AddAsync(order, cancellationToken);
        // SaveChanges + Commit handled by TransactionBehavior
        return Right<MediatorError, OrderCreated>(new OrderCreated(order.Id));
    }
}
```

### Redis Caching

```csharp
// Install: dotnet add package SimpleMediator.Caching.Redis

// Startup
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
services.AddSimpleMediator(cfg =>
{
    cfg.AddRedisCaching();  // Registers RedisCachingBehavior
}, assemblies);

// Attribute-driven caching
[CacheFor(Minutes = 5)]
public sealed class GetProductByIdHandler : IRequestHandler<GetProductById, Product>
{
    // Result is cached for 5 minutes in Redis
}
```

---

## Appendix B: Breaking Changes Roadmap (2.0)

### IRequestContext Parameter

**Current (1.0):**

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken);
}
```

**Future (2.0):**

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,  // NEW
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken);
}
```

**Migration:**

1. Mark old signature `[Obsolete]` in 1.9
2. Introduce new signature alongside old in 1.9
3. Remove old signature in 2.0
4. Provide automated migration tool (Roslyn analyzer + code fix)

### HandlerMetadata Parameter

**Future (2.0):**

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        HandlerMetadata metadata,  // NEW
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken);
}
```

**Migration:** Same as above (Obsolete → dual support → remove)

---

**This ADR represents our commitment to keeping SimpleMediator lightweight, flexible, and integration-friendly without compromising core simplicity.**
