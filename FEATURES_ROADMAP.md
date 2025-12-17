# SimpleMediator Features Roadmap

## Vision

SimpleMediator aspira a ser el framework de mediación funcional para .NET que permite construir aplicaciones modernas (microservicios, monolitos modulares, CQRS, Event Sourcing) con **Railway Oriented Programming** como filosofía central, reduciendo el boilerplate y permitiendo que los desarrolladores se enfoquen en la lógica de negocio mientras el framework maneja las "tuberías" de la aplicación.

## Filosofía de Diseño

- **Functional First**: ROP puro con `Either<MediatorError, T>` como ciudadano de primera clase
- **Explicit over Implicit**: El código debe ser claro y predecible, evitando "magia" oculta
- **Performance Conscious**: Zero-allocation hot paths, Expression tree compilation, minimal overhead
- **Composable**: Behaviors son unidades pequeñas y componibles que se pueden combinar
- **Pre-1.0 Freedom**: Libertad total para breaking changes si mejoran el framework

## Status Actual

**Versión**: Pre-1.0 (desarrollo activo, breaking changes permitidos)

**Core Completado**:

- ✅ Pure Railway Oriented Programming (ADR-006)
- ✅ Request/Notification dispatch con Expression tree compilation
- ✅ Pipeline pattern (Behaviors, PreProcessors, PostProcessors)
- ✅ IRequestContext para ambient context (Week 1 completada)
- ✅ Observabilidad con Activity Source y Metrics
- ✅ CQRS markers (ICommand, IQuery)
- ✅ Functional failure detection

---

## Features a NO Implementar

### ❌ Generic Variance & Intelligent Dispatch

**Razón**: Va contra la filosofía "explicit over implicit"

- Añade complejidad de resolución (~15-20% overhead)
- Hace el código menos predecible
- En ROP puro, los tipos suelen ser muy específicos por dominio
- Si alguien necesita esto, puede implementarlo en su capa de aplicación

**Decisión**: NO implementar

---

## Features Pre-1.0 (Extensibilidad Core)

### ✅ COMPLETADO: IRequestContext (Week 1)

**Objetivo**: Ambient context que fluye por el pipeline para permitir integración con bibliotecas externas.

**Implementado**:

- Interface `IRequestContext` con propiedades inmutables
- `CorrelationId` (auto-generado desde Activity.Current o GUID)
- `UserId`, `TenantId`, `IdempotencyKey` (opcionales)
- `Timestamp` (request start time)
- `Metadata` (ImmutableDictionary para extensibilidad)
- `RequestContext` implementation con factory methods
- Actualización de todas las interfaces del pipeline
- 194/194 tests pasando

**Habilita**: Validación, autorización, multi-tenancy, idempotency, audit logging, distributed tracing

---

### 🎯 SIGUIENTE: Stream Requests (Pre-1.0)

**Objetivo**: Soporte para `IAsyncEnumerable<T>` en queries grandes o real-time.

**Prioridad**: ⭐⭐⭐⭐ (Alta)
**Complejidad**: ⭐⭐⭐ (Media)
**Timeline**: Pre-1.0 (versión 1.1+)

**Casos de uso**:

- Queries grandes con paginación implícita (millones de registros)
- Escenarios real-time/Server-Sent Events (SSE)
- gRPC streaming en microservicios
- Procesamiento batch con backpressure

**Diseño propuesto**:

```csharp
// Nueva interfaz para stream requests
public interface IStreamRequest<out TItem> { }

public interface IStreamRequestHandler<in TRequest, TItem>
    where TRequest : IStreamRequest<TItem>
{
    IAsyncEnumerable<Either<MediatorError, TItem>> Handle(
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken);
}

// Ejemplo de uso
public record StreamCustomersQuery(int PageSize) : IStreamRequest<Customer>;

public class StreamCustomersHandler : IStreamRequestHandler<StreamCustomersQuery, Customer>
{
    public async IAsyncEnumerable<Either<MediatorError, Customer>> Handle(
        StreamCustomersQuery request,
        IRequestContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var customer in _db.Customers.AsAsyncEnumerable().WithCancellation(ct))
        {
            yield return Right<MediatorError, Customer>(customer);
        }
    }
}

// Behaviors pueden interceptar streams
public class StreamLoggingBehavior<TRequest, TItem> : IStreamPipelineBehavior<TRequest, TItem>
{
    public async IAsyncEnumerable<Either<MediatorError, TItem>> Handle(
        TRequest request,
        IRequestContext context,
        StreamHandlerCallback<TItem> nextStep,
        [EnumeratorCancellation] CancellationToken ct)
    {
        _logger.LogInformation("Stream started");
        var count = 0;

        await foreach (var item in nextStep().WithCancellation(ct))
        {
            count++;
            yield return item;
        }

        _logger.LogInformation("Stream completed: {Count} items", count);
    }
}
```

**Challenges**:

- Behaviors deben poder interceptar streams (más complejo que requests normales)
- Error handling: ¿Qué hacer si un item falla? (yield Left o cancelar todo)
- Observability: tracking de items procesados, backpressure
- Testing: asegurarse que los tests no consuman todo el stream

**Decisión**: ✅ **SÍ implementar** después de 1.0 (alta utilidad, encaja con ROP)

---

## Satellite Packages (Pre-1.0)

Estos packages extienden SimpleMediator con integraciones específicas. Siguen la filosofía de "el framework maneja las tuberías".

### 🎯 Fase 1: Essentials (Pre-1.0)

#### ✅ 1. SimpleMediator.FluentValidation ⭐⭐⭐⭐⭐

**Status**: ✅ COMPLETADO

**Objetivo**: Validación automática de requests usando FluentValidation.

**Prioridad**: CRÍTICA (reduce boilerplate masivamente)
**Complejidad**: ⭐⭐ (Baja - es un behavior)
**Timeline**: Pre-1.0

**Implementado**:

- `ValidationPipelineBehavior<TRequest, TResponse>` with ROP integration
- `AddSimpleMediatorFluentValidation()` extension methods
- Context enrichment (CorrelationId, UserId, TenantId)
- Parallel validator execution
- Comprehensive test suite (18 tests, 100% passing)
- Full XML documentation and README

**Funcionalidad**:

```csharp
// El developer solo define el validador
public record CreateUserCommand(string Email, string Password) : ICommand<User>;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
    }
}

// El framework automáticamente:
// 1. Detecta el validador vía DI
// 2. Ejecuta validación ANTES del handler
// 3. Si falla, retorna Left<MediatorError> con detalles
// 4. El handler solo se ejecuta si la validación pasa

// Configuración simple
services.AddSimpleMediator(cfg =>
{
    cfg.AddFluentValidation(); // Registra behavior automáticamente
}, assemblies);
```

**Implementación**:

- Behavior `ValidationBehavior<TRequest, TResponse>` que:
  - Resuelve `IValidator<TRequest>` desde DI (si existe)
  - Ejecuta validación antes de `nextStep()`
  - Retorna `Left<MediatorError>` con errores de validación
  - Si no hay validador, simplemente pasa al siguiente step
- Error serializado con metadata:

  ```csharp
  MediatorError.Create(
      "validation.failed",
      "Request validation failed",
      metadata: new Dictionary<string, object?>
      {
          ["validationErrors"] = validationResult.Errors.Select(e => new {
              e.PropertyName,
              e.ErrorMessage,
              e.ErrorCode
          })
      }
  )
  ```

**Tests**:

- Validación exitosa (pasa al handler)
- Validación fallida (cortocircuito, retorna Left)
- Sin validador registrado (pasa al handler)
- Múltiples errores de validación
- Validadores async
- Context enrichment (validador puede acceder a IRequestContext)

---

#### ✅ 1.1. SimpleMediator.DataAnnotations ⭐⭐⭐⭐

**Status**: ✅ COMPLETADO

**Objetivo**: Validación automática usando Data Annotations (built-in .NET, zero dependencies).

**Prioridad**: ALTA (alternativa sin dependencies)
**Complejidad**: ⭐ (Muy baja)
**Timeline**: Pre-1.0

**Implementado**:

- `DataAnnotationsValidationBehavior<TRequest, TResponse>` with ROP integration
- `AddDataAnnotationsValidation()` extension method
- Zero external dependencies (uses System.ComponentModel.DataAnnotations)
- Context enrichment (CorrelationId, UserId, TenantId)
- Comprehensive test suite (10 tests, 100% passing)
- Full XML documentation and comprehensive README

**Ventajas**:

- Zero dependencies (built-in .NET)
- Ideal para prototipos y aplicaciones simples
- Atributos declarativos sobre propiedades
- Compatible con legacy code que ya usa Data Annotations

---

#### ✅ 1.2. SimpleMediator.MiniValidator ⭐⭐⭐⭐

**Status**: ✅ COMPLETADO

**Objetivo**: Validación lightweight usando MiniValidation (~20KB), perfect para Minimal APIs.

**Prioridad**: ALTA (crecimiento en Minimal APIs)
**Complejidad**: ⭐ (Muy baja)
**Timeline**: Pre-1.0

**Implementado**:

- `MiniValidationBehavior<TRequest, TResponse>` with ROP integration
- `AddMiniValidation()` extension method
- Lightweight dependency (MiniValidation ~20KB)
- Uses Data Annotations under the hood
- Comprehensive test suite (10 tests, 100% passing)
- Full XML documentation and PublicAPI support

**Ventajas**:

- Ultra-lightweight (~20KB vs 500KB FluentValidation)
- Perfect para Minimal APIs
- Uses Data Annotations but más minimalista
- Growing trend en la comunidad

---

#### ✅ 2. SimpleMediator.AspNetCore ⭐⭐⭐⭐⭐

**Status**: ✅ COMPLETADO

**Objetivo**: Integración con ASP.NET Core (HttpContext, Authorization, Correlation IDs).

**Prioridad**: CRÍTICA (esencial para APIs)
**Complejidad**: ⭐⭐⭐ (Media)
**Timeline**: Pre-1.0

**Implementado**:

- `SimpleMediatorContextMiddleware` for IRequestContext enrichment from HttpContext
- `AuthorizationPipelineBehavior` with [Authorize] attribute support (roles, policies)
- `ProblemDetailsExtensions` for RFC 7807 Problem Details (intelligent error mapping)
- `IRequestContextAccessor` with AsyncLocal storage for thread-safe context access
- Comprehensive test suite (49 tests, 100% passing)
- Full XML documentation and PublicAPI support
- .NET 10 compatibility with latest ASP.NET Core APIs

**Funcionalidad**:

```csharp
// 1. Middleware que enriquece IRequestContext desde HttpContext
app.UseSimpleMediatorContext(); // Middleware que:
// - Extrae correlation ID desde headers (X-Correlation-ID o genera nuevo)
// - Extrae User ID desde ClaimsPrincipal
// - Extrae Tenant ID desde claims/headers
// - Pasa todo a IRequestContext

// 2. Authorization behavior
[Authorize(Roles = "Admin")] // O [Authorize(Policy = "RequireElevation")]
public record DeleteUserCommand(int Id) : ICommand;

// El framework verifica automáticamente antes del handler
// Usa IRequestContext.UserId para obtener claims del HttpContext

// 3. Extension methods para controladores
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Match(
            Left: error => error.ToProblemDetails(HttpContext), // Helper
            Right: user => Created($"/api/users/{user.Id}", user)
        );
    }
}
```

**Implementación**:

- Middleware `SimpleMediatorContextMiddleware`:
  - `IHttpContextAccessor` para acceder a HttpContext
  - Crear/enriquecer `IRequestContext` desde headers/claims
  - Usar `AsyncLocal<IRequestContext>` o scoped service
- Behavior `AuthorizationBehavior<TRequest, TResponse>`:
  - Detecta atributos `[Authorize]` en requests
  - Verifica contra `IAuthorizationService` de ASP.NET Core
  - Retorna `Left<MediatorError>` si falla autorización
- Extension methods:
  - `ToProblemDetails(HttpContext)` para convertir `MediatorError` a RFC 7807
  - `AddSimpleMediatorAspNetCore()` para registrar todo

**Tests**:

- Correlation ID propagation
- User extraction desde ClaimsPrincipal
- Authorization success/failure
- Multiple authorization policies
- Problem Details generation

---

#### 3. SimpleMediator.OpenTelemetry ⭐⭐⭐⭐⭐

**Objetivo**: Observabilidad avanzada con OpenTelemetry.

**Prioridad**: CRÍTICA (observabilidad es esencial)
**Complejidad**: ⭐⭐⭐ (Media)
**Timeline**: Pre-1.0

**Funcionalidad**:

```csharp
// Configuración simple
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSimpleMediatorInstrumentation() // Auto-instrumenta el mediator
        .AddJaegerExporter())
    .WithMetrics(builder => builder
        .AddSimpleMediatorInstrumentation() // Métricas automáticas
        .AddPrometheusExporter());

// El framework automáticamente emite:
// - Distributed traces con W3C TraceContext
// - Spans por request/behavior/handler
// - Métricas (duración, tasa de errores, throughput)
// - Logs estructurados con correlation
// - Propagación de baggage para contexto custom
```

**Implementación**:

- Mejorar `MediatorDiagnostics` actual:
  - ActivitySource compatible con OTel
  - Tags estandarizados (semantic conventions)
  - Baggage propagation desde IRequestContext
- Meter para métricas:
  - `mediator.request.duration` (Histogram)
  - `mediator.request.count` (Counter)
  - `mediator.request.errors` (Counter)
  - Segmentado por: request_type, request_kind (command/query), status
- Integration con ILogger:
  - Structured logging con correlation ID
  - Log scopes por request

**Tests**:

- Activity propagation
- Metrics emission
- Baggage propagation
- Integration con collectors

---

### 🎯 Fase 2: Enterprise Features (Pre-1.0)

#### 4. SimpleMediator.EntityFrameworkCore ⭐⭐⭐⭐⭐

**Objetivo**: Transaction management y Outbox pattern.

**Prioridad**: ALTA (patrón común en aplicaciones reales)
**Complejidad**: ⭐⭐⭐⭐ (Alta - outbox es complejo)
**Timeline**: Pre-1.0 (versión 1.1)

**Funcionalidad**:

```csharp
// 1. Automatic transaction management
[Transaction] // O implementar ITransactionalCommand
public record CreateOrderCommand(...) : ICommand<Order>;

// El framework automáticamente:
// 1. Inicia transacción
// 2. Ejecuta handler
// 3. Commit si Right, Rollback si Left o excepción

// 2. Outbox pattern para eventos
public record OrderCreatedEvent(...) : INotification;

// El framework guarda el evento en tabla outbox
// Worker procesa outbox y publica eventos (garantiza entrega)
services.AddSimpleMediatorOutbox<AppDbContext>(options =>
{
    options.ProcessingInterval = TimeSpan.FromSeconds(5);
    options.BatchSize = 100;
    options.RetryPolicy = Policy.Exponential(...);
});

// 3. Multi-tenancy query filters
services.AddSimpleMediatorTenancy<AppDbContext>(options =>
{
    options.TenantIdProperty = "TenantId";
    options.AutoFilter = true; // Añade WHERE TenantId = @tenantId automáticamente
});
```

**Implementación**:

- `TransactionBehavior<TRequest, TResponse>`:
  - Detecta marker `[Transaction]` o `ITransactionalCommand`
  - Inicia `DbContext.Database.BeginTransaction()`
  - Commit/Rollback basado en resultado
- Outbox:
  - Tabla `OutboxMessages` con serialización JSON
  - `OutboxPostProcessor` que guarda eventos en lugar de publicarlos
  - Background service `OutboxProcessor` que procesa pendientes
  - Retry logic con Polly
  - Idempotency keys para evitar duplicados
- Tenancy:
  - Global query filter en DbContext
  - Usa `IRequestContext.TenantId` automáticamente

**Tests**:

- Transaction commit/rollback
- Nested transactions
- Outbox message persistence
- Outbox processing + retries
- Tenant isolation

---

#### 5. SimpleMediator.Caching ⭐⭐⭐⭐

**Objetivo**: Query result caching e idempotency.

**Prioridad**: ALTA (performance boost significativo)
**Complejidad**: ⭐⭐⭐ (Media - cache invalidation es difícil)
**Timeline**: Pre-1.0 (versión 1.1)

**Funcionalidad**:

```csharp
// 1. Query caching
[Cache(Duration = "00:05:00", Key = "customer-{request.Id}")]
public record GetCustomerQuery(int Id) : IQuery<Customer>;

// El framework cachea automáticamente el resultado
// Key interpolation usa propiedades del request

// 2. Idempotency para commands
[Idempotent] // O implementar IIdempotentCommand
public record ChargeCustomerCommand(decimal Amount) : ICommand<Receipt>;

// El framework usa IRequestContext.IdempotencyKey para:
// 1. Chequear si ya se procesó
// 2. Si ya existe, retornar resultado cacheado
// 3. Si no, ejecutar y cachear resultado

// 3. Cache invalidation
public record UpdateCustomerCommand(int Id, ...) : ICommand<Customer>
{
    [InvalidatesCache("customer-{Id}")] // Invalida cache del customer
    public int Id { get; init; }
}
```

**Implementación**:

- `CachingBehavior<TQuery, TResponse>`:
  - Detecta `[Cache]` attribute en queries
  - Usa `IDistributedCache` (Redis, InMemory, etc.)
  - Serializa con System.Text.Json
  - Interpolación de keys con expressions
- `IdempotencyBehavior<TCommand, TResponse>`:
  - Usa `IRequestContext.IdempotencyKey`
  - Guarda resultado en cache con TTL largo (24h+)
  - Retorna cached result si existe
- Cache invalidation:
  - PostProcessor que detecta `[InvalidatesCache]`
  - Invalida keys específicas o patterns
  - Soporte para cache tags (Redis)

**Tests**:

- Cache hit/miss
- Cache key interpolation
- Idempotency (same key = same result)
- Cache invalidation
- Distributed cache scenarios

---

#### 6. SimpleMediator.Polly ⭐⭐⭐⭐

**Objetivo**: Retry policies y circuit breakers.

**Prioridad**: ALTA (resiliencia)
**Complejidad**: ⭐⭐ (Baja - wrapper de Polly)
**Timeline**: Pre-1.0 (versión 1.1)

**Funcionalidad**:

```csharp
[Retry(MaxAttempts = 3, BackoffType = BackoffType.Exponential)]
[CircuitBreaker(FailureThreshold = 5, DurationOfBreak = "00:01:00")]
public record CallExternalApiQuery(...) : IQuery<ApiResponse>;

// El framework maneja reintentos y circuit breaking automáticamente
// Usa Polly bajo el capó

// Configuración avanzada
services.AddSimpleMediatorPolly(options =>
{
    options.DefaultRetryPolicy = Policy
        .Handle<HttpRequestException>()
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    options.DefaultCircuitBreakerPolicy = Policy
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1));
});
```

**Implementación**:

- `ResilienceBehavior<TRequest, TResponse>`:
  - Detecta `[Retry]`, `[CircuitBreaker]`, `[Timeout]` attributes
  - Crea Polly policies dinámicamente
  - Envuelve `nextStep()` con policies
  - Logs de retries con IRequestContext.CorrelationId
- Integration con Polly v8 (nuevo API):
  - `ResiliencePipeline` builder
  - Telemetry integration

**Tests**:

- Retry on transient failures
- Circuit breaker open/close
- Timeout behavior
- Combined policies (retry + circuit breaker)

---

### 🎯 Fase 3: Advanced (Pre-1.0 - versión 1.2+)

#### 7. SimpleMediator.EventSourcing

**Objetivo**: Integración con Event Sourcing.

**Prioridad**: MEDIA
**Complejidad**: ⭐⭐⭐⭐⭐ (Muy alta)
**Timeline**: Pre-1.0 (versión 1.2+)

**Funcionalidad**:

- Event store integration (EventStoreDB, Marten, etc.)
- Aggregate pattern con event sourcing
- Projection handlers
- Snapshot support

---

#### 8. Multi-tenancy Advanced

**Objetivo**: Soporte avanzado para multi-tenancy.

**Prioridad**: MEDIA (útil para SaaS)
**Complejidad**: ⭐⭐⭐⭐ (Alta)
**Timeline**: Pre-1.0 (versión 1.2+)

---

## Feature Requests Process

**Pre-1.0**: Cualquier feature puede ser agregada/modificada/removida sin restricciones.

**Pre-1.0**:

1. Crear GitHub Issue con template "Feature Request"
2. Discusión de diseño en issue
3. Si es breaking change: marcar para próximo major version
4. Si es non-breaking: puede ir en minor/patch

---

## Decisiones Arquitecturales

Ver `/docs/architecture/adr/` para decisiones documentadas:

- ADR-001: Railway Oriented Programming
- ADR-004: NO implementar MediatorResult<T>
- ADR-005: NO usar Source Generators
- ADR-006: Pure ROP con fail-fast exception handling
- ADR-007: IRequestContext para extensibilidad

---

## Referencias

- [MediatR](https://github.com/jbogard/MediatR) - Inspiración original
- [Wolverine](https://wolverine.netlify.app/) - Messaging patterns
- [Kommand](https://github.com/NicoJuicy/Kommand) - CQRS patterns
- [NestJS](https://nestjs.com/) - Filosofía de "framework handles plumbing"
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) - Scott Wlaschin
