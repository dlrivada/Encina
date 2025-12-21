# SimpleMediator Extensibility Analysis

**Date:** 2025-12-14
**Status:** Analysis Complete
**Authors:** Architecture Team

## Executive Summary

This document analyzes SimpleMediator's current extensibility capabilities and readiness for integration with common infrastructure libraries (logging, observability, caching, messaging, databases, validation, etc.).

**Key Findings:**

- ✅ **Strong foundation:** Pipeline behaviors, pre/post processors provide flexible extension points
- ✅ **DI-friendly:** All components support constructor injection
- ⚠️ **Missing patterns:** Some common scenarios lack documented patterns or helper types
- 🔴 **Breaking changes needed:** Request metadata enrichment requires architectural changes

**Recommendation:** Library is 80% ready. We need to add specific extension patterns and satellite packages before 1.0 release.

---

## 1. Current Extensibility Mechanisms

### 1.1 Pipeline Behaviors (`IPipelineBehavior<TRequest, TResponse>`)

**Purpose:** Cross-cutting concerns that wrap handler execution (Russian doll pattern).

**Current Capabilities:**

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken);
}
```

**Strengths:**

- ✅ Full control over pipeline execution (can short-circuit)
- ✅ Access to request and response via Either<L,R>
- ✅ Constructor injection for dependencies (ILogger, DbContext, etc.)
- ✅ Supports open generics (`IPipelineBehavior<,>`) for apply-to-all behaviors
- ✅ Supports closed generics for request-specific behaviors
- ✅ Registration order matters (behaviors compose in reverse registration order)

**Limitations:**

- ❌ No access to raw `HttpContext` or ambient context (by design - keeps library agnostic)
- ❌ No built-in correlation ID propagation (users must implement)
- ❌ Cannot modify request properties (requests should be immutable records)
- ❌ No access to handler type metadata at compile time

**Good For:**

- ✅ Logging (Serilog, NLog, Microsoft.Extensions.Logging)
- ✅ Metrics collection (OpenTelemetry, Prometheus)
- ✅ Distributed tracing (Activity/OpenTelemetry)
- ✅ Validation (FluentValidation)
- ✅ Authorization checks
- ✅ Transaction management (EF Core, Dapper)
- ✅ Caching (Redis, in-memory)
- ✅ Retry/circuit breaker (Polly)
- ✅ Rate limiting
- ✅ Idempotency checking

**Example - FluentValidation Integration:**

```csharp
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await nextStep();

        var context = new ValidationContext<TRequest>(request);
        var failures = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = failures
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToArray();

        if (errors.Length > 0)
        {
            var metadata = new Dictionary<string, object?>
            {
                ["validation_errors"] = errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            };
            return Left<MediatorError, TResponse>(
                MediatorErrors.Create("VALIDATION_FAILED", "One or more validation errors occurred.", null, metadata));
        }

        return await nextStep();
    }
}

// Registration
services.AddValidatorsFromAssembly(typeof(CreateOrderValidator).Assembly);
services.AddSimpleMediator(cfg =>
{
    cfg.AddPipelineBehavior(typeof(ValidationBehavior<,>));
}, assemblies);
```

### 1.2 Request Pre-Processors (`IRequestPreProcessor<TRequest>`)

**Purpose:** Execute logic before behaviors and handlers (enrichment, normalization, auditing).

**Current Capabilities:**

```csharp
public interface IRequestPreProcessor<in TRequest>
{
    Task Process(TRequest request, CancellationToken cancellationToken);
}
```

**Strengths:**

- ✅ Early pipeline stage (runs before behaviors)
- ✅ Constructor injection support
- ✅ Multiple pre-processors compose in registration order
- ✅ Ideal for ambient context setup

**Limitations:**

- ❌ Cannot short-circuit pipeline (no return value)
- ❌ Cannot modify request (contravariant `in TRequest`)
- ❌ Exceptions will propagate (fail-fast)

**Good For:**

- ✅ Correlation ID injection (`Activity.Current.SetBaggage`)
- ✅ User context enrichment
- ✅ Audit logging (request received)
- ✅ Request normalization (trim strings, case normalization)
- ⚠️ Security checks (better in behavior for short-circuit capability)

**Example - Correlation ID:**

```csharp
public sealed class CorrelationIdPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
{
    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        if (Activity.Current is not null && string.IsNullOrEmpty(Activity.Current.Id))
        {
            Activity.Current.SetBaggage("correlation_id", Guid.NewGuid().ToString());
        }
        return Task.CompletedTask;
    }
}
```

### 1.3 Request Post-Processors (`IRequestPostProcessor<TRequest, TResponse>`)

**Purpose:** Execute logic after handler completes (side effects, notifications, cleanup).

**Current Capabilities:**

```csharp
public interface IRequestPostProcessor<in TRequest, TResponse>
{
    Task Process(
        TRequest request,
        Either<MediatorError, TResponse> response,
        CancellationToken cancellationToken);
}
```

**Strengths:**

- ✅ Access to both request and response
- ✅ Can inspect success/failure via Either pattern matching
- ✅ Multiple post-processors compose in registration order
- ✅ Runs even on functional failures

**Limitations:**

- ❌ Cannot modify response
- ❌ Exceptions propagate (fail-fast)
- ❌ No short-circuit capability

**Good For:**

- ✅ Event publishing (RabbitMQ, Kafka, EventStoreDB)
- ✅ Cache invalidation (Redis)
- ✅ Audit logging (operation completed)
- ✅ Metrics emission (success/failure counters)
- ✅ Notification triggers (email, SMS)
- ⚠️ Database commits (better in behavior for transaction control)

**Example - Event Publishing:**

```csharp
public sealed class EventPublisherPostProcessor<TRequest, TResponse>
    : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IEventEmitter
{
    private readonly IMessageBus _messageBus;

    public EventPublisherPostProcessor(IMessageBus messageBus)
        => _messageBus = messageBus;

    public async Task Process(
        TRequest request,
        Either<MediatorError, TResponse> response,
        CancellationToken cancellationToken)
    {
        await response.Match(
            Right: async _ =>
            {
                foreach (var @event in request.GetDomainEvents())
                {
                    await _messageBus.PublishAsync(@event, cancellationToken);
                }
            },
            Left: _ => Task.CompletedTask
        );
    }
}
```

### 1.4 Functional Failure Detector (`IFunctionalFailureDetector`)

**Purpose:** Extract business errors from domain-specific response types for telemetry.

**Current Capabilities:**

```csharp
public interface IFunctionalFailureDetector
{
    bool TryExtractFailure(object? response, out string reason, out object? capturedFailure);
    string TryGetErrorCode(object? failure);
    string TryGetErrorMessage(object? failure);
}
```

**Strengths:**

- ✅ Keeps behaviors decoupled from domain types
- ✅ Enables generic telemetry behaviors
- ✅ Single registration per application

**Limitations:**

- ❌ Reflection-based (performance cost, but cached)
- ❌ Only one implementation per DI container

**Good For:**

- ✅ OpenTelemetry integration (tagging activities with business errors)
- ✅ Metrics (count failures by error code)
- ✅ Logging (structured logs with error context)

### 1.5 Diagnostics & Metrics

**Current Built-in Support:**

**MediatorDiagnostics:**

- ✅ `ActivitySource` for distributed tracing
- ✅ Automatic span creation for Send operations
- ✅ Tag enrichment (request type, handler, error codes)
- ✅ OpenTelemetry-compatible

**MediatorMetrics:**

- ✅ Histogram for request duration
- ✅ Counter for total requests
- ✅ Counter for failures
- ✅ Tags: request_kind (command/query), request_name, error_code

**Integration Ready:**

- ✅ **OpenTelemetry:** Direct integration via `ActivitySource`
- ✅ **Prometheus:** Metrics exposed via OTEL exporter
- ✅ **Application Insights:** Via OTEL or native instrumentation
- ✅ **Jaeger/Zipkin:** Via OTEL trace exporter

**Example - OTEL Setup:**

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSource("SimpleMediator")  // MediatorDiagnostics.ActivitySource
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddJaegerExporter())
    .WithMetrics(builder => builder
        .AddMeter("SimpleMediator.Metrics")  // MediatorMetrics.Meter
        .AddPrometheusExporter());
```

---

## 2. Integration Patterns for Common Scenarios

### 2.1 Logging (Serilog, NLog, Microsoft.Extensions.Logging)

**Status:** ✅ **Ready** - Via behaviors with constructor injection

**Pattern:**

```csharp
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestType} {@Request}", typeof(TRequest).Name, request);

        var sw = Stopwatch.StartNew();
        var result = await nextStep();
        sw.Stop();

        result.Match(
            Right: _ => _logger.LogInformation("Handled {RequestType} in {ElapsedMs}ms",
                typeof(TRequest).Name, sw.ElapsedMilliseconds),
            Left: error => _logger.LogError("Failed {RequestType} with {ErrorCode}: {ErrorMessage}",
                typeof(TRequest).Name, error.GetMediatorCode(), error.Message)
        );

        return result;
    }
}
```

**Satellite Package Opportunity:** `SimpleMediator.Logging` with pre-built behaviors

### 2.2 Validation (FluentValidation)

**Status:** ✅ **Ready** - Via behaviors (example shown in 1.1)

**Satellite Package Opportunity:** `SimpleMediator.FluentValidation`

- Pre-built `ValidationBehavior<,>`
- Extension method: `cfg.AddFluentValidation()`
- Automatic validator discovery

### 2.3 Database Transactions (EF Core, Dapper)

**Status:** ✅ **Ready** - Via behaviors

**Pattern - EF Core:**

```csharp
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ApplicationDbContext _dbContext;

    public TransactionBehavior(ApplicationDbContext dbContext)
        => _dbContext = dbContext;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Queries don't need transactions
        if (request is IQuery<TResponse>)
            return await nextStep();

        // Commands use transactions
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await nextStep();

            return await result.Match(
                Right: async response =>
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return Right<MediatorError, TResponse>(response);
                },
                Left: async error =>
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Left<MediatorError, TResponse>(error);
                }
            );
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;  // Fail-fast per Pure ROP
        }
    }
}
```

**Satellite Package Opportunity:** `SimpleMediator.EntityFrameworkCore`

### 2.4 Caching (Redis, In-Memory)

**Status:** ✅ **Ready** - Via behaviors

**Pattern:**

```csharp
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheable
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var cacheKey = request.GetCacheKey();

        // Try cache first
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            var cachedResponse = JsonSerializer.Deserialize<TResponse>(cached);
            return Right<MediatorError, TResponse>(cachedResponse!);
        }

        // Execute handler
        var result = await nextStep();

        // Cache on success
        await result.Match(
            Right: async response =>
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = request.GetCacheDuration()
                };
                var serialized = JsonSerializer.Serialize(response);
                await _cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);
            },
            Left: _ => Task.CompletedTask
        );

        return result;
    }
}

public interface ICacheable
{
    string GetCacheKey();
    TimeSpan GetCacheDuration();
}
```

**Satellite Package Opportunity:** `SimpleMediator.Caching.Redis`

### 2.5 Message Brokers (RabbitMQ, Kafka, Azure Service Bus)

**Status:** ✅ **Ready** - Via post-processors or behaviors

**Pattern - Outbox Pattern:**

```csharp
public sealed class OutboxBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IEventEmitter
{
    private readonly ApplicationDbContext _dbContext;

    public OutboxBehavior(ApplicationDbContext dbContext)
        => _dbContext = dbContext;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var result = await nextStep();

        await result.Match(
            Right: async _ =>
            {
                // Store events in outbox table (same transaction as command)
                var events = request.GetDomainEvents();
                foreach (var @event in events)
                {
                    var outboxMessage = new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        Type = @event.GetType().FullName!,
                        Payload = JsonSerializer.Serialize(@event),
                        CreatedAt = DateTime.UtcNow
                    };
                    await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
                }
            },
            Left: _ => Task.CompletedTask
        );

        return result;
    }
}

// Separate background service processes outbox and publishes to RabbitMQ/Kafka
```

**Satellite Package Opportunities:**

- `SimpleMediator.RabbitMQ` - Direct publish behaviors
- `SimpleMediator.Kafka` - Producer behaviors
- `SimpleMediator.Outbox` - Generic outbox pattern with EF Core

### 2.6 Idempotency

**Status:** ⚠️ **Needs Enhancement** - Requires request metadata enrichment

**Current Gap:** No built-in way to attach idempotency keys to requests without modifying request types.

**Pattern (Requires Enhancement):**

```csharp
public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IIdempotent
{
    private readonly IIdempotencyStore _store;

    public IdempotencyBehavior(IIdempotencyStore store)
        => _store = store;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = request.GetIdempotencyKey();

        // Check if already processed
        var existing = await _store.GetAsync<TResponse>(idempotencyKey, cancellationToken);
        if (existing is not null)
            return Right<MediatorError, TResponse>(existing);

        // Execute handler
        var result = await nextStep();

        // Store result on success
        await result.Match(
            Right: async response =>
            {
                await _store.SetAsync(idempotencyKey, response, TimeSpan.FromHours(24), cancellationToken);
            },
            Left: _ => Task.CompletedTask
        );

        return result;
    }
}

public interface IIdempotent
{
    string GetIdempotencyKey();
}
```

**Recommended Enhancement:** Add `IRequestMetadata` interface for ambient context (see Section 3.1).

**Satellite Package Opportunity:** `SimpleMediator.Idempotency`

### 2.7 Retry & Circuit Breaker (Polly)

**Status:** ✅ **Ready** - Via behaviors

**Pattern:**

```csharp
public sealed class PollyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .Build();

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _pipeline.ExecuteAsync(
                async ct => await nextStep(),
                cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            return Left<MediatorError, TResponse>(
                MediatorErrors.Create("CIRCUIT_OPEN", "Circuit breaker is open", ex));
        }
    }
}
```

**Satellite Package Opportunity:** `SimpleMediator.Polly`

### 2.8 Authorization

**Status:** ✅ **Ready** - Via behaviors

**Pattern:**

```csharp
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUser;

    public AuthorizationBehavior(
        IAuthorizationService authorizationService,
        ICurrentUserService currentUser)
    {
        _authorizationService = authorizationService;
        _currentUser = currentUser;
    }

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var authorizeAttributes = request.GetType()
            .GetCustomAttributes<AuthorizeAttribute>()
            .ToArray();

        if (authorizeAttributes.Length == 0)
            return await nextStep();

        foreach (var attribute in authorizeAttributes)
        {
            var result = await _authorizationService.AuthorizeAsync(
                _currentUser.User,
                request,
                attribute.Policy);

            if (!result.Succeeded)
            {
                return Left<MediatorError, TResponse>(
                    MediatorErrors.Create("UNAUTHORIZED", "Insufficient permissions"));
            }
        }

        return await nextStep();
    }
}

// Usage
[Authorize(Policy = "CanCreateOrders")]
public record CreateOrder(string ProductId, int Quantity) : ICommand<Order>;
```

**Satellite Package Opportunity:** `SimpleMediator.Authorization`

### 2.9 Rate Limiting

**Status:** ✅ **Ready** - Via behaviors

**Pattern:**

```csharp
public sealed class RateLimitingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly RateLimiter _rateLimiter;
    private readonly ICurrentUserService _currentUser;

    public RateLimitingBehavior(RateLimiter rateLimiter, ICurrentUserService currentUser)
    {
        _rateLimiter = rateLimiter;
        _currentUser = currentUser;
    }

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var requestType = typeof(TRequest).Name;

        using var lease = await _rateLimiter.AcquireAsync(
            permitCount: 1,
            cancellationToken: cancellationToken);

        if (!lease.IsAcquired)
        {
            return Left<MediatorError, TResponse>(
                MediatorErrors.Create("RATE_LIMIT_EXCEEDED", "Too many requests"));
        }

        return await nextStep();
    }
}
```

### 2.10 Event Sourcing (EventStoreDB, Marten)

**Status:** ✅ **Ready** - Via behaviors or handlers

**Pattern:**

```csharp
// Handlers write to event stream directly
public sealed class CreateOrderHandler : IRequestHandler<CreateOrder, OrderCreated>
{
    private readonly IEventStore _eventStore;

    public CreateOrderHandler(IEventStore eventStore)
        => _eventStore = eventStore;

    public async ValueTask<Either<MediatorError, OrderCreated>> Handle(
        CreateOrder request,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        var @event = new OrderCreatedEvent(orderId, request.ProductId, request.Quantity);

        await _eventStore.AppendToStreamAsync(
            $"order-{orderId}",
            StreamState.NoStream,
            new[] { @event },
            cancellationToken);

        return Right<MediatorError, OrderCreated>(new OrderCreated(orderId));
    }
}
```

**Satellite Package Opportunity:** `SimpleMediator.EventStoreDB`

---

## 3. Architectural Gaps & Recommendations

### 3.1 Request Metadata Enrichment (CRITICAL)

**Problem:**

- No standard way to attach ambient context to requests (correlation IDs, user context, idempotency keys, trace context)
- Modifying request types breaks immutability
- Using static `AsyncLocal<T>` is anti-pattern

**Proposed Solution:**

**Option A: Metadata Container in Pipeline (Recommended)**

Add `IRequestContext` that flows through pipeline:

```csharp
public interface IRequestContext
{
    string CorrelationId { get; }
    string? UserId { get; }
    string? IdempotencyKey { get; }
    IReadOnlyDictionary<string, object?> Metadata { get; }

    IRequestContext WithMetadata(string key, object? value);
}

// Updated signatures
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,  // NEW
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken);
}

// Handlers remain unchanged (don't need context in most cases)
// Behaviors can enrich context and pass down
```

**Breaking Change:** YES - Changes all behavior signatures

**Migration Path:**

1. Add `IRequestContext` as optional parameter (default null)
2. Mark old signature as `[Obsolete]` in 1.x
3. Remove in 2.0

**Option B: Marker Interface (Non-Breaking)**

```csharp
public interface IHasMetadata
{
    IRequestContext Context { get; }
}

// Requests opt-in
public record CreateOrder(...) : ICommand<Order>, IHasMetadata
{
    public IRequestContext Context { get; init; } = RequestContext.Empty;
}
```

**Pros:** Non-breaking
**Cons:** Not all requests will have context, behaviors must check interface

**Recommendation:** Implement Option B for 1.0, plan Option A for 2.0

### 3.2 Handler Metadata Access

**Problem:**

- Behaviors cannot access handler type at compile time
- Useful for per-handler configuration (custom retry policies, cache TTLs)

**Proposed Solution:**

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        HandlerMetadata metadata,  // NEW: { HandlerType, Attributes }
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken);
}
```

**Use Cases:**

- Read `[CacheFor(Minutes = 5)]` attribute from handler
- Read `[Retry(MaxAttempts = 3)]` attribute
- Log handler name in telemetry

**Breaking Change:** YES

**Alternative:** Provide metadata via separate injection:

```csharp
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IRequestHandlerMetadataProvider _metadataProvider;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(...)
    {
        var metadata = _metadataProvider.GetMetadata<TRequest, TResponse>();
        var cacheAttribute = metadata.HandlerType.GetCustomAttribute<CacheForAttribute>();
        // ...
    }
}
```

**Recommendation:** Add `IRequestHandlerMetadataProvider` service for 1.0

### 3.3 Notification Handlers for Domain Events

**Current State:**

- `INotification` and `INotificationHandler` exist
- Multiple handlers per notification
- No ordering guarantees

**Gap:**

- No documented pattern for domain event publishing after command success
- No integration with outbox pattern

**Recommended Pattern:**

```csharp
// Commands emit events via marker interface
public interface IEventEmitter
{
    IReadOnlyCollection<INotification> GetDomainEvents();
}

// Post-processor publishes events
public sealed class DomainEventPublisherPostProcessor<TRequest, TResponse>
    : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IEventEmitter
{
    private readonly IMediator _mediator;

    public async Task Process(
        TRequest request,
        Either<MediatorError, TResponse> response,
        CancellationToken cancellationToken)
    {
        await response.Match(
            Right: async _ =>
            {
                foreach (var @event in request.GetDomainEvents())
                {
                    await _mediator.Publish(@event, cancellationToken);
                }
            },
            Left: _ => Task.CompletedTask
        );
    }
}
```

**Recommendation:** Document this pattern in architecture guide

### 3.4 Scoped Service Access in Handlers

**Current State:** ✅ Works via DI

**Potential Issue:**

- Users might access `DbContext` in queries without readonly semantics
- No enforcement of CQRS separation

**Recommendation:**

- Document best practices: Commands modify, Queries are read-only
- Consider `IQueryDbContext` vs `ICommandDbContext` pattern in docs

### 3.5 Streaming Responses

**Gap:**

- No support for `IAsyncEnumerable<T>` responses
- Large queries must load all data in memory

**Proposed:**

```csharp
public interface IStreamingRequest<out TItem> : IRequest<IAsyncEnumerable<TItem>>
{
}

public interface IStreamingRequestHandler<in TRequest, TItem>
    : IRequestHandler<TRequest, IAsyncEnumerable<TItem>>
    where TRequest : IStreamingRequest<TItem>
{
}
```

**Breaking Change:** NO - additive only

**Recommendation:** Add in 1.1 or 2.0

---

## 4. Satellite Package Roadmap

To avoid bloating core package, create optional integration packages:

| Package | Purpose | Priority |
|---------|---------|----------|
| `SimpleMediator.FluentValidation` | Validation behavior + auto-registration | 🔴 High |
| `SimpleMediator.EntityFrameworkCore` | Transaction behavior, outbox pattern | 🔴 High |
| `SimpleMediator.OpenTelemetry` | Pre-configured OTEL setup | 🟡 Medium |
| `SimpleMediator.Polly` | Retry/circuit breaker behaviors | 🟡 Medium |
| `SimpleMediator.Caching.Redis` | Redis caching behavior | 🟡 Medium |
| `SimpleMediator.Caching.Memory` | In-memory caching behavior | 🟡 Medium |
| `SimpleMediator.RabbitMQ` | Direct publish behaviors | 🟢 Low |
| `SimpleMediator.Kafka` | Kafka producer behaviors | 🟢 Low |
| `SimpleMediator.Outbox` | Generic outbox pattern | 🟡 Medium |
| `SimpleMediator.Authorization` | ASP.NET Core authorization behavior | 🟡 Medium |
| `SimpleMediator.Idempotency` | Idempotency behavior + store abstractions | 🟡 Medium |
| `SimpleMediator.EventStoreDB` | Event sourcing helpers | 🟢 Low |
| `SimpleMediator.Serilog` | Serilog enrichment behaviors | 🟢 Low |

---

## 5. Documentation Needs

Before 1.0 release:

1. **Integration Guide** - Document all patterns shown in Section 2
2. **Satellite Package Docs** - Setup guides for each package
3. **Samples Repository** - Real-world examples:
   - E-commerce with EF Core + validation + events
   - CQRS with read/write separation
   - Event sourcing with EventStoreDB
   - Microservices with RabbitMQ
4. **Migration Guide** - From MediatR to SimpleMediator
5. **Best Practices** - CQRS, immutability, error handling

---

## 6. Summary & Action Plan

### Current State

✅ **Strengths:**

- Solid extensibility via behaviors/processors
- DI-first design enables all integrations
- Pure ROP encourages clean error handling
- OpenTelemetry-ready diagnostics

⚠️ **Needs Work:**

- Request metadata enrichment (breaking change)
- Handler metadata access
- Documentation/samples for common scenarios
- Satellite packages for popular integrations

### Recommended Actions (Before 1.0)

**Critical (Must Have):**

1. ✅ Add `IRequestHandlerMetadataProvider` for handler introspection (non-breaking)
2. ✅ Add `IHasMetadata` marker interface for request context (non-breaking)
3. ✅ Create `SimpleMediator.FluentValidation` package
4. ✅ Create `SimpleMediator.EntityFrameworkCore` package
5. ✅ Write integration guide with 10+ real-world examples

**Important (Should Have):**
6. ✅ Create `SimpleMediator.OpenTelemetry` package
7. ✅ Create samples repository with 3+ reference apps
8. ✅ Document domain event publishing pattern
9. ✅ Add ADR for metadata strategy

**Nice to Have:**
10. ⚪ Create `SimpleMediator.Polly` package
11. ⚪ Create `SimpleMediator.Caching.Redis` package
12. ⚪ Migration guide from MediatR

### Breaking Changes for 2.0 (Future)

- Add `IRequestContext context` parameter to all pipeline interfaces
- Add `HandlerMetadata metadata` to behavior Handle method
- Support `IAsyncEnumerable<T>` for streaming queries

---

## 7. Conclusion

**SimpleMediator is 80% ready for production use with external libraries.**

The core extensibility mechanisms (behaviors, processors, DI) are sufficient for integrating with virtually any infrastructure library. The remaining 20% is:

- Better documented patterns
- Convenience satellite packages
- Non-breaking metadata enrichment

**No architectural changes are needed before 1.0.** The current design is flexible enough to support all common scenarios without breaking changes. The recommended additions (metadata provider, marker interfaces) are purely additive.

**Recommendation:** Proceed with satellite packages and documentation. Reserve breaking changes (context parameter) for 2.0 after gathering user feedback.
