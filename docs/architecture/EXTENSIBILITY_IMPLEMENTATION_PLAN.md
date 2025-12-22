# Extensibility Implementation Plan (Pre-1.0)

**Date:** 2025-12-14
**Status:** Ready for Implementation
**Related:** ADR-007-v2, QUALITY_SECURITY_ROADMAP.md

---

## Executive Summary

**Decision:** Adopt `IRequestContext` as explicit parameter in all pipeline interfaces.

**Rationale:**

- Best technical solution (no compromises)
- Clean API (context always available, no null checks)
- Supports 100% of integration scenarios
- Pre-1.0 = no breaking change concerns

**Impact:**

- ✅ All pipeline interfaces gain context parameter
- ✅ Enables idempotency, multi-tenancy, user context, correlation IDs
- ✅ Foundation for all satellite packages
- ⚠️ Requires updating existing behaviors + tests (~200 LOC changes)

**Timeline:** 7 weeks (aligned with roadmap "Siguiente Sprint" items)

---

## Architecture Changes

### Core Interfaces (NEW)

```csharp
namespace Encina;

// NEW: Ambient context for all requests
public interface IRequestContext
{
    string CorrelationId { get; }           // Always present
    string? UserId { get; }                 // From auth
    string? IdempotencyKey { get; }         // For deduplication
    string? TenantId { get; }               // For multi-tenancy
    DateTimeOffset Timestamp { get; }       // Request time
    IReadOnlyDictionary<string, object?> Metadata { get; }

    IRequestContext WithMetadata(string key, object? value);
    IRequestContext WithUserId(string? userId);
    IRequestContext WithIdempotencyKey(string? key);
    IRequestContext WithTenantId(string? tenantId);
}

// UPDATED: Behavior signature
public interface IPipelineBehavior<TRequest, TResponse>
{
    ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,  // NEW PARAMETER
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken);
}

// UPDATED: Pre-processor signature
public interface IRequestPreProcessor<in TRequest>
{
    Task Process(
        TRequest request,
        IRequestContext context,  // NEW PARAMETER
        CancellationToken cancellationToken);
}

// UPDATED: Post-processor signature
public interface IRequestPostProcessor<in TRequest, TResponse>
{
    Task Process(
        TRequest request,
        IRequestContext context,  // NEW PARAMETER
        Either<EncinaError, TResponse> response,
        CancellationToken cancellationToken);
}

// NEW: Handler metadata for attribute-driven behaviors
public interface IRequestHandlerMetadataProvider
{
    HandlerMetadata GetMetadata<TRequest, TResponse>();
}
```

### Implementation (RequestContext.cs)

```csharp
public sealed class RequestContext : IRequestContext
{
    public string CorrelationId { get; init; }
    public string? UserId { get; init; }
    public string? IdempotencyKey { get; init; }
    public string? TenantId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; }

    private RequestContext() { }

    public static IRequestContext Create() => new RequestContext
    {
        CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        Metadata = ImmutableDictionary<string, object?>.Empty
    };

    public static IRequestContext FromHttpContext(HttpContext httpContext) => new RequestContext
    {
        CorrelationId = Activity.Current?.Id ?? httpContext.TraceIdentifier,
        UserId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        IdempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault(),
        Timestamp = DateTimeOffset.UtcNow,
        Metadata = ImmutableDictionary<string, object?>.Empty
    };

    public IRequestContext WithMetadata(string key, object? value) =>
        new RequestContext(this) { Metadata = Metadata.SetItem(key, value) };

    public IRequestContext WithUserId(string? userId) =>
        new RequestContext(this) { UserId = userId };

    public IRequestContext WithIdempotencyKey(string? key) =>
        new RequestContext(this) { IdempotencyKey = key };

    public IRequestContext WithTenantId(string? tenantId) =>
        new RequestContext(this) { TenantId = tenantId };

    private RequestContext(RequestContext source)
    {
        CorrelationId = source.CorrelationId;
        UserId = source.UserId;
        IdempotencyKey = source.IdempotencyKey;
        TenantId = source.TenantId;
        Timestamp = source.Timestamp;
        Metadata = source.Metadata;
    }
}
```

---

## Implementation Plan (7 Weeks)

### Week 1: Core Context Implementation

**Tasks:**

1. ✅ Create `IRequestContext` interface in `Abstractions/`
2. ✅ Create `RequestContext` implementation in `Core/`
3. ✅ Update `IPipelineBehavior<,>` signature
4. ✅ Update `IRequestPreProcessor<>` signature
5. ✅ Update `IRequestPostProcessor<,>` signature
6. ✅ Update `RequestHandlerCallback<>` delegate
7. ✅ Update `RequestDispatcher.SendCoreAsync` to create context
8. ✅ Update `NotificationDispatcher.PublishCoreAsync` to create context
9. ✅ Add tests for `RequestContext` (immutability, enrichment, creation)

**Files Modified:**

- `src/Encina/Abstractions/IRequestContext.cs` (NEW)
- `src/Encina/Core/RequestContext.cs` (NEW)
- `src/Encina/Abstractions/IPipelineBehavior.cs` (UPDATED)
- `src/Encina/Abstractions/IRequestPreProcessor.cs` (UPDATED)
- `src/Encina/Abstractions/IRequestPostProcessor.cs` (UPDATED)
- `src/Encina/Dispatchers/Encina.RequestDispatcher.cs` (UPDATED)
- `src/Encina/Dispatchers/Encina.NotificationDispatcher.cs` (UPDATED)
- `tests/Encina.Tests/RequestContextTests.cs` (NEW)

**Estimated LOC:** ~300 (150 new, 150 updated)

**Tests Required:**

- RequestContext creation
- Immutability (With* methods)
- FromHttpContext extraction
- Correlation ID auto-generation
- Metadata enrichment

### Week 2: Update Built-in Behaviors

**Tasks:**

1. ✅ Update `CommandActivityPipelineBehavior<,>` to accept context
2. ✅ Update `QueryActivityPipelineBehavior<,>` to accept context
3. ✅ Update `CommandMetricsPipelineBehavior<,>` to accept context
4. ✅ Update `QueryMetricsPipelineBehavior<,>` to accept context
5. ✅ Enrich Activity tags with context.UserId, context.TenantId
6. ✅ Update all behavior tests to pass context

**Files Modified:**

- `src/Encina/Pipeline/Behaviors/CommandActivityPipelineBehavior.cs`
- `src/Encina/Pipeline/Behaviors/QueryActivityPipelineBehavior.cs`
- `src/Encina/Pipeline/Behaviors/CommandMetricsPipelineBehavior.cs`
- `src/Encina/Pipeline/Behaviors/QueryMetricsPipelineBehavior.cs`
- `tests/Encina.Tests/PipelineBehaviorsTests.cs`
- `tests/Encina.Tests/EncinaTests.cs`

**Estimated LOC:** ~200 (mostly test updates)

**Tests Required:**

- Behaviors receive context correctly
- Activity enrichment with UserId/TenantId
- Metrics tagged with user context

### Week 3: Handler Metadata Provider

**Tasks:**

1. ✅ Create `IRequestHandlerMetadataProvider` interface
2. ✅ Create `HandlerMetadata` record
3. ✅ Implement `RequestHandlerMetadataProvider` with caching
4. ✅ Update `EncinaAssemblyScanner` to collect handler metadata
5. ✅ Register provider as singleton in DI
6. ✅ Add tests for metadata discovery
7. ✅ Add performance benchmarks (reflection cost)

**Files Modified:**

- `src/Encina/Abstractions/IRequestHandlerMetadataProvider.cs` (NEW)
- `src/Encina/Core/HandlerMetadata.cs` (NEW)
- `src/Encina/Core/RequestHandlerMetadataProvider.cs` (NEW)
- `src/Encina/Dispatchers/EncinaAssemblyScanner.cs` (UPDATED)
- `src/Encina/Core/ServiceCollectionExtensions.cs` (UPDATED)
- `tests/Encina.Tests/RequestHandlerMetadataProviderTests.cs` (NEW)
- `benchmarks/Encina.Benchmarks/MetadataProviderBenchmarks.cs` (NEW)

**Estimated LOC:** ~250 (200 new, 50 updated)

**Tests Required:**

- Attribute discovery
- Caching behavior
- Thread safety
- Missing handler handling

### Week 4: Satellite Package #1 - AspNetCore

**Package:** `Encina.AspNetCore`

**Purpose:** Bridge between ASP.NET Core and Encina

**Tasks:**

1. ✅ Create new project `Encina.AspNetCore`
2. ✅ Create `EncinaContextMiddleware` to extract context from HttpContext
3. ✅ Extension method: `app.UseEncinaContext()`
4. ✅ Extension method: `services.AddEncinaAspNetCore()`
5. ✅ Support for custom context factories
6. ✅ Tests with TestServer
7. ✅ Documentation + examples

**Features:**

- Auto-extract UserId from ClaimsPrincipal
- Auto-extract CorrelationId from Activity or TraceIdentifier
- Auto-extract IdempotencyKey from header "Idempotency-Key"
- Store context in HttpContext.Items for access outside Encina

**Estimated LOC:** ~150

### Week 5: Satellite Package #2 & #3 - FluentValidation + EF Core

**Package 1:** `Encina.FluentValidation`

**Tasks:**

1. ✅ Create `ValidationBehavior<,>`
2. ✅ Extension: `cfg.AddFluentValidation()`
3. ✅ Auto-discover validators from assemblies
4. ✅ Return validation errors as `Left<EncinaError>`
5. ✅ Tests
6. ✅ Documentation

**Estimated LOC:** ~100

**Package 2:** `Encina.EntityFrameworkCore`

**Tasks:**

1. ✅ Create `TransactionBehavior<,>` (commands only)
2. ✅ Create `OutboxBehavior<,>` for domain events
3. ✅ Extension: `cfg.AddEntityFrameworkCore<TDbContext>()`
4. ✅ Tests with in-memory database
5. ✅ Documentation + outbox pattern guide

**Estimated LOC:** ~200

### Week 6: Satellite Package #4 & #5 - Idempotency + Polly

**Package 1:** `Encina.Idempotency`

**Tasks:**

1. ✅ Create `IdempotencyBehavior<,>` (reads context.IdempotencyKey)
2. ✅ Create `IIdempotencyStore` abstraction
3. ✅ Implement `InMemoryIdempotencyStore`
4. ✅ Implement `DistributedIdempotencyStore` (IDistributedCache)
5. ✅ Extension: `cfg.AddIdempotency()`
6. ✅ Tests
7. ✅ Documentation

**Estimated LOC:** ~250

**Package 2:** `Encina.Polly`

**Tasks:**

1. ✅ Create `RetryBehavior<,>` with attribute support
2. ✅ Create `CircuitBreakerBehavior<,>` with attribute support
3. ✅ Attributes: `[Retry(MaxAttempts = 3)]`, `[CircuitBreaker(...)]`
4. ✅ Use `IRequestHandlerMetadataProvider` to read attributes
5. ✅ Extension: `cfg.AddPolly()`
6. ✅ Tests
7. ✅ Documentation

**Estimated LOC:** ~200

### Week 7: Documentation + Polish

**Tasks:**

1. ✅ Update integration guide with all satellite packages
2. ✅ Create sample app: E-commerce with validation + EF Core + idempotency
3. ✅ Create sample app: Multi-tenant SaaS with context enrichment
4. ✅ Update README with satellite package list
5. ✅ Update roadmap (mark extensibility as complete)
6. ✅ Performance benchmarks for context overhead
7. ✅ Migration guide (current → new architecture)

**Deliverables:**

- Updated `docs/integration-guide.md`
- Sample repo with 2+ apps
- Benchmarks showing <1% overhead for context
- Migration checklist

---

## Alignment with Quality Roadmap

### Completed Roadmap Items

From `QUALITY_SECURITY_ROADMAP.md`:

**Fase 1 - Fundamentos de Calidad Extrema:**

- ✅ 1.1 Documentación API Comprehensiva (100% completado)
- ✅ 1.2 Código y Estilo (namespaces, guards completados)
- ✅ 1.3 Testing Comprehensivo (92.5% cobertura, 79.75% mutation score)
- ✅ 1.4 Análisis Estático Avanzado (SonarCloud configurado)

### New Roadmap Items (Extensibility)

**Siguiente Sprint** (add to roadmap):

- [ ] **Implement Request Context** (Week 1)
  - Add `IRequestContext` + `RequestContext`
  - Update all pipeline interfaces
  - Update built-in behaviors
  - Tests: 95%+ coverage

- [ ] **Implement Handler Metadata Provider** (Week 3)
  - Add `IRequestHandlerMetadataProvider`
  - Update assembly scanner
  - Benchmarks: <1ms reflection cost per handler

- [ ] **Create Satellite Packages** (Weeks 4-6)
  - `Encina.AspNetCore` (Week 4)
  - `Encina.FluentValidation` (Week 5)
  - `Encina.EntityFrameworkCore` (Week 5)
  - `Encina.Idempotency` (Week 6)
  - `Encina.Polly` (Week 6)

- [ ] **Integration Guide + Samples** (Week 7)
  - 10+ integration patterns documented
  - 2+ sample applications
  - Migration guide

**Mediano Plazo** (add to roadmap):

- [ ] Additional satellite packages
  - `Encina.OpenTelemetry`
  - `Encina.Caching.Redis`
  - `Encina.Authorization`
  - `Encina.RabbitMQ`
  - `Encina.Kafka`

---

## Quality Gates

All changes must pass:

✅ **Code Quality:**

- 0 warnings
- 0 analyzer violations
- Mutation score ≥ 79% (current baseline)
- Code coverage ≥ 92% (current baseline)

✅ **Performance:**

- Context overhead < 1% (benchmark)
- Metadata provider < 1ms per handler (cached)
- No allocations in hot path (stackalloc for small contexts)

✅ **Tests:**

- All existing tests updated
- New tests for context, metadata provider
- Contract tests for satellite packages

✅ **Documentation:**

- XML docs for all public APIs
- Integration guide updated
- Sample apps functional

---

## Risk Mitigation

**Risk 1:** Context overhead impacts performance

- **Mitigation:** Benchmark early (Week 1), optimize allocation
- **Acceptance:** <1% overhead vs current architecture

**Risk 2:** Satellite packages delay 1.0 release

- **Mitigation:** Core changes (Weeks 1-3) sufficient for 1.0, satellites can ship after
- **Minimum viable:** Core + AspNetCore + FluentValidation

**Risk 3:** Migration complexity for tests

- **Mitigation:** Create test helper `RequestContext.CreateForTest()`
- **Estimated effort:** ~2 hours for all tests

**Risk 4:** Metadata provider reflection cost

- **Mitigation:** Aggressive caching, benchmark target <1ms
- **Fallback:** Compile expression trees for attribute reading

---

## Success Metrics

✅ **Week 1 Complete:**

- IRequestContext implemented
- All pipeline signatures updated
- All tests passing (225/225)
- Mutation score ≥79%

✅ **Week 3 Complete:**

- Handler metadata provider working
- Benchmark: <1ms reflection cost
- Satellite package foundation ready

✅ **Week 7 Complete:**

- 5+ satellite packages published
- Integration guide with 10+ examples
- 2+ sample apps running
- All quality gates passing

✅ **Post-1.0:**

- User feedback on context API
- Performance metrics from real apps
- Additional satellite packages based on demand

---

## Next Immediate Actions

**Today:**

1. Create branch: `feature/request-context`
2. Implement `IRequestContext` interface
3. Implement `RequestContext` class
4. Add tests for immutability

**This Week:**
5. Update `IPipelineBehavior<,>` signature
6. Update `IRequestPreProcessor<>` signature
7. Update `IRequestPostProcessor<,>` signature
8. Update `RequestDispatcher` to create context
9. Update all built-in behaviors
10. Update all tests

**Completion Target:** Week 1 complete by end of this sprint (5 working days)

---

**This plan represents the complete path from current architecture to fully extensible library with satellite packages.**
