# ADR-007: Extensibility Strategy for External Library Integration (v2 - Pre-1.0)

**Status:** Proposed (Pre-1.0 - Best Technical Solution)
**Date:** 2025-12-14
**Authors:** Architecture Team
**Supersedes:** None
**Related:** ADR-001 (Railway Oriented Programming), ADR-006 (Pure ROP)

**Pre-1.0 Context:** We prioritize **best technical solution** over backward compatibility. No existing users, no migration concerns.

---

## Context

SimpleMediator needs to integrate seamlessly with common infrastructure libraries without forcing dependencies:

- Logging (Serilog, NLog, ILogger)
- Observability (OpenTelemetry, Prometheus, Jaeger)
- Validation (FluentValidation)
- Databases (EF Core, Dapper, EventStoreDB)
- Caching (Redis, in-memory)
- Messaging (RabbitMQ, Kafka, Azure Service Bus)
- Resilience (Polly)
- Security (ASP.NET Core Authorization)
- Idempotency stores
- Multi-tenancy

**Critical Requirements:**

1. Core package must remain lightweight (zero external dependencies)
2. Extensibility must support 100% of common scenarios
3. Clean, ergonomic APIs (no workarounds or hacks)
4. Type-safe where possible
5. Testable

---

## Decision

**Adopt Request Context as First-Class Pipeline Concept**

We will add `IRequestContext` as an **explicit parameter** to all pipeline interfaces. This is the cleanest,  most flexible solution.

### 1. Core Interfaces (UPDATED)

```csharp
namespace SimpleMediator;

/// <summary>
/// Ambient context that flows through the mediator pipeline.
/// Created per request, immutable, supports enrichment.
/// </summary>
public interface IRequestContext
{
    /// <summary>
    /// Correlation ID for distributed tracing (always present, auto-generated if not provided).
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// User ID initiating the request (null if unauthenticated).
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Idempotency key for duplicate detection (null if not applicable).
    /// </summary>
    string? IdempotencyKey { get; }

    /// <summary>
    /// Tenant ID for multi-tenant apps (null if not applicable).
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Request timestamp (UTC).
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Custom metadata for extensibility.
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Creates a new context with additional metadata (immutable pattern).
    /// </summary>
    IRequestContext WithMetadata(string key, object? value);

    /// <summary>
    /// Creates a new context with updated user ID.
    /// </summary>
    IRequestContext WithUserId(string? userId);

    /// <summary>
    /// Creates a new context with updated idempotency key.
    /// </summary>
    IRequestContext WithIdempotencyKey(string? idempotencyKey);

    /// <summary>
    /// Creates a new context with updated tenant ID.
    /// </summary>
    IRequestContext WithTenantId(string? tenantId);
}

// UPDATED: All pipeline interfaces now include IRequestContext
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,  // NEW: Always available
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken);
}

public interface IRequestPreProcessor<in TRequest>
{
    Task Process(
        TRequest request,
        IRequestContext context,  // NEW: Can enrich context
        CancellationToken cancellationToken);
}

public interface IRequestPostProcessor<in TRequest, TResponse>
{
    Task Process(
        TRequest request,
        IRequestContext context,  // NEW: Read-only access
        Either<MediatorError, TResponse> response,
        CancellationToken cancellationToken);
}
```

**Default Implementation:**

```csharp
public sealed class RequestContext : IRequestContext
{
    public string CorrelationId { get; init; }
    public string? UserId { get; init; }
    public string? IdempotencyKey { get; init; }
    public string? TenantId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; }

    private RequestContext(/* ... */) { }

    /// <summary>
    /// Creates a new context with auto-generated correlation ID.
    /// </summary>
    public static IRequestContext Create() => new RequestContext
    {
        CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        Metadata = ImmutableDictionary<string, object?>.Empty
    };

    /// <summary>
    /// Creates a new context from HttpContext (ASP.NET Core integration).
    /// </summary>
    public static IRequestContext FromHttpContext(HttpContext httpContext)
    {
        var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var correlationId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();

        return new RequestContext
        {
            CorrelationId = correlationId,
            UserId = userId,
            IdempotencyKey = idempotencyKey,
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = ImmutableDictionary<string, object?>.Empty
        };
    }

    public IRequestContext WithMetadata(string key, object? value) =>
        new RequestContext(this) { Metadata = Metadata.SetItem(key, value) };

    // Other With* methods...
}
```

### 2. Handler Metadata Access

Add `IRequestHandlerMetadataProvider` for attribute-driven behavior configuration:

```csharp
public interface IRequestHandlerMetadataProvider
{
    HandlerMetadata GetMetadata<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>;
}

public sealed class HandlerMetadata
{
    public Type HandlerType { get; init; }
    public IReadOnlyList<Attribute> Attributes { get; init; }
    public ServiceLifetime Lifetime { get; init; }
}
```

This allows behaviors to read handler attributes:

```csharp
[CacheFor(Minutes = 5)]
public sealed class GetProductHandler : IRequestHandler<GetProduct, Product> { }

// Behavior reads attribute
public sealed class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IRequestHandlerMetadataProvider _metadata;
    private readonly IDistributedCache _cache;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var handlerMetadata = _metadata.GetMetadata<TRequest, TResponse>();
        var cacheAttr = handlerMetadata.Attributes.OfType<CacheForAttribute>().FirstOrDefault();

        if (cacheAttr is null)
            return await nextStep();

        var cacheKey = $"{typeof(TRequest).Name}:{JsonSerializer.Serialize(request)}";
        // Cache logic...
    }
}
```

### 3. Satellite Package Strategy (UNCHANGED)

Core remains minimal, integrations via satellite packages:

| Package | Purpose | Priority |
|---------|---------|----------|
| `SimpleMediator.FluentValidation` | Validation behavior | 🔴 Critical |
| `SimpleMediator.EntityFrameworkCore` | Transaction/outbox behaviors | 🔴 Critical |
| `SimpleMediator.OpenTelemetry` | Pre-configured OTEL setup | 🟡 High |
| `SimpleMediator.Polly` | Retry/circuit breaker | 🟡 High |
| `SimpleMediator.Caching.Redis` | Redis caching | 🟡 High |
| `SimpleMediator.Authorization` | ASP.NET Core auth | 🟡 High |
| `SimpleMediator.Idempotency` | Idempotency checking | 🟡 High |
| `SimpleMediator.AspNetCore` | HttpContext integration | 🟡 High |
| `SimpleMediator.RabbitMQ` | Message publishing | 🟢 Medium |
| `SimpleMediator.Kafka` | Event streaming | 🟢 Medium |

---

## Consequences

### Positive

✅ **Clean API**

- Context **always available** (no null checks, no marker interfaces)
- Immutable API prevents accidental mutations
- Correlation ID always present (auto-generated)

✅ **Flexible**

- Supports all common scenarios (idempotency, multi-tenancy, user context)
- Extensible via Metadata dictionary
- Pre-processors can enrich context

✅ **Type-Safe**

- Handler metadata provider enables attribute-driven configuration
- Compile-time errors if handler not registered

✅ **Testable**

- Easy to mock `IRequestContext` in tests
- No static state, no `AsyncLocal<T>` hacks

✅ **Performance**

- Context allocation once per request
- Immutable = safe for concurrent access
- Handler metadata cached

### Negative

⚠️ **Changes Existing Behaviors**

- All behaviors, pre/post processors need context parameter
- **ACCEPTABLE PRE-1.0** - no existing users

⚠️ **Slightly More Verbose**

- Behaviors have 4 parameters instead of 3
- Mitigated by clear naming and IntelliSense

---

## Implementation Roadmap

### Phase 1: Core Changes (Week 1)

1. **Add `IRequestContext` interface + `RequestContext` implementation**
   - Properties: CorrelationId, UserId, IdempotencyKey, TenantId, Timestamp, Metadata
   - Methods: WithMetadata, WithUserId, etc.
   - Tests: immutability, enrichment

2. **Update all pipeline interfaces**
   - `IPipelineBehavior<,>`: add `IRequestContext context` parameter
   - `IRequestPreProcessor<>`: add `IRequestContext context` parameter
   - `IRequestPostProcessor<,>`: add `IRequestContext context` parameter
   - Update `RequestHandlerCallback<>` to accept context

3. **Update `RequestDispatcher`**
   - Create context at start of Send: `var context = RequestContext.Create();`
   - Pass context to all behaviors/processors
   - Enrich Activity with context properties

4. **Update built-in behaviors**
   - `CommandActivityPipelineBehavior`: read CorrelationId from context
   - `QueryActivityPipelineBehavior`: read CorrelationId from context
   - `CommandMetricsPipelineBehavior`: read UserId from context for tagging
   - `QueryMetricsPipelineBehavior`: read UserId from context for tagging

5. **Update all tests**
   - Create test helper: `RequestContext.CreateForTest(userId: "test")`
   - Update all behavior tests to pass context
   - Add context enrichment tests

### Phase 2: Handler Metadata (Week 2)

1. **Add `IRequestHandlerMetadataProvider` service**
   - Reflection-based implementation with caching
   - Register as singleton in DI

2. **Update `MediatorAssemblyScanner`**
   - Build handler metadata cache during scanning
   - Store attributes, lifetime, type info

3. **Add tests**
   - Attribute discovery
   - Caching behavior
   - Performance tests (reflection cost)

### Phase 3: Satellite Packages (Weeks 3-6)

**Priority 1 (Week 3):**

1. `SimpleMediator.AspNetCore`
   - Middleware to create context from HttpContext
   - Extension: `app.UseSimpleMediatorContext()`
   - Automatic correlation ID, user ID extraction

2. `SimpleMediator.FluentValidation`
   - `ValidationBehavior<,>`
   - Extension: `cfg.AddFluentValidation()`
   - Auto-validator discovery

**Priority 2 (Week 4):**
3. `SimpleMediator.EntityFrameworkCore`

- `TransactionBehavior<,>`
- `OutboxBehavior<,>` for domain events
- Extension: `cfg.AddEntityFrameworkCore<TDbContext>()`

4. `SimpleMediator.Idempotency`
   - `IdempotencyBehavior<,>` (reads context.IdempotencyKey)
   - `IIdempotencyStore` abstraction
   - In-memory and distributed implementations

**Priority 3 (Week 5):**
5. `SimpleMediator.OpenTelemetry`

- Pre-configured ActivitySource listener
- Metrics exporter
- Context enrichment (baggage propagation)

6. `SimpleMediator.Polly`
   - `RetryBehavior<,>` with attribute support
   - `CircuitBreakerBehavior<,>`
   - Handler attribute: `[Retry(MaxAttempts = 3)]`

**Priority 4 (Week 6):**
7. `SimpleMediator.Caching.Redis`

- `RedisCachingBehavior<,>`
- Handler attribute: `[CacheFor(Minutes = 5)]`

8. `SimpleMediator.Authorization`
   - `AuthorizationBehavior<,>`
   - Handler attribute: `[Authorize(Policy = "...")]`

### Phase 4: Documentation (Week 7)

1. **Update integration guide**
   - Context usage patterns
   - Each satellite package with examples
   - Migration from current architecture

2. **Create samples repository**
   - E-commerce app (full stack)
   - CQRS app with context enrichment
   - Multi-tenant SaaS app

3. **API documentation**
   - XML docs for all new types
   - Conceptual docs for context pattern

---

## Comparison with Alternatives

### ❌ Alternative 1: Marker Interface (`IHasMetadata`)

```csharp
// Request must implement interface
public record CreateOrder(...) : ICommand<OrderCreated>, IHasMetadata
{
    public IRequestContext Context { get; init; }
}

// Behavior must check interface
if (request is IHasMetadata hasMetadata)
{
    var key = hasMetadata.Context.IdempotencyKey;
}
```

**Rejected:**

- ❌ Not all requests have context (opt-in)
- ❌ Behaviors must check `is IHasMetadata` (runtime check)
- ❌ Pollutes request types with infrastructure concerns
- ❌ Context attached to immutable request (awkward)

### ❌ Alternative 2: Static `AsyncLocal<T>`

```csharp
public static class RequestContextAccessor
{
    private static readonly AsyncLocal<IRequestContext> _context = new();
    public static IRequestContext Current => _context.Value;
}
```

**Rejected:**

- ❌ Hidden dependency (not visible in signatures)
- ❌ Hard to test (global state)
- ❌ Can be null (requires null checks)
- ❌ Anti-pattern in modern .NET

### ❌ Alternative 3: Service Injection

```csharp
public sealed class IdempotencyBehavior<TRequest, TResponse>
{
    private readonly IRequestContextAccessor _contextAccessor;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(...)
    {
        var context = await _contextAccessor.GetCurrentContext();
    }
}
```

**Rejected:**

- ❌ Extra indirection (accessor pattern)
- ❌ Can return null (requires null checks)
- ❌ Not clear that context is request-scoped

### ✅ CHOSEN: Explicit Parameter

```csharp
public async ValueTask<Either<MediatorError, TResponse>> Handle(
    TRequest request,
    IRequestContext context,  // Clear, always available
    RequestHandlerCallback<TResponse> nextStep,
    CancellationToken cancellationToken)
```

**Why:**

- ✅ Explicit dependency (visible in signature)
- ✅ Never null (guaranteed by library)
- ✅ Easy to test (pass mock)
- ✅ Modern .NET style (cf. HttpContext in minimal APIs)

---

## Migration Path (Current Architecture → New)

### Step 1: Add Context Parameter (Breaking Change)

**Before:**

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // ...
    }
}
```

**After:**

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,  // NEW
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling {RequestType} for user {UserId} (correlation: {CorrelationId})",
            typeof(TRequest).Name,
            context.UserId,
            context.CorrelationId);
        // ...
    }
}
```

### Step 2: Update Built-in Behaviors

All 4 built-in behaviors updated automatically (part of Phase 1).

### Step 3: Update Tests

```csharp
// Before
var result = await behavior.Handle(request, nextStep, CancellationToken.None);

// After
var context = RequestContext.Create().WithUserId("test-user");
var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);
```

**Pre-1.0 Note:** No migration concerns. This is the initial architecture.

---

## Success Criteria

✅ All common integration scenarios supported without workarounds
✅ Context always available (no null checks)
✅ Zero external dependencies in core package
✅ 5+ satellite packages published
✅ Integration guide with 10+ examples
✅ Sample apps demonstrate real-world usage
✅ All tests updated and passing
✅ Mutation score ≥80% maintained

---

## Next Actions

**Immediate (This Sprint):**

1. Implement `IRequestContext` + `RequestContext`
2. Update all pipeline interfaces
3. Update `RequestDispatcher` to create/pass context
4. Update built-in behaviors
5. Update all tests
6. Add `IRequestHandlerMetadataProvider`

**Next Sprint:**
7. Create `SimpleMediator.AspNetCore`
8. Create `SimpleMediator.FluentValidation`
9. Create `SimpleMediator.EntityFrameworkCore`
10. Write integration guide

**Following Sprints:**
11. Remaining satellite packages
12. Sample applications
13. Performance benchmarks
14. Documentation polish

---

**This ADR represents the best technical solution for extensibility, free from backward compatibility constraints.**
