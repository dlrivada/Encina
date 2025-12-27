# Encina - Inventario Completo

> **Documento generado**: 27 de diciembre de 2025
> **Versión**: Pre-1.0 (Phase 2: Functionality ~93%)
> **Propósito**: Inventario exhaustivo de todas las funcionalidades, patrones, paquetes y características de Encina

---

## Tabla de Contenidos

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Arquitectura General](#arquitectura-general)
3. [Paquetes por Categoría](#paquetes-por-categoría)
4. [Patrones Implementados](#patrones-implementados)
5. [Características por Ángulo](#características-por-ángulo)
6. [Features Pendientes (Phase 2)](#features-pendientes-phase-2)
7. [Matriz de Completitud](#matriz-de-completitud)

---

## Resumen Ejecutivo

### Estadísticas Globales

| Métrica | Valor |
|---------|-------|
| **Total de Paquetes** | 52 |
| **Patrones de Messaging** | 10 |
| **Providers de Base de Datos** | 14 |
| **Providers de Caching** | 8 |
| **Transportes/Message Brokers** | 8 |
| **Integraciones Web/Serverless** | 6 |
| **Interfaces Públicas (Core)** | ~25 |
| **Pipeline Behaviors** | ~20+ |

### Filosofía de Diseño

- **Railway Oriented Programming (ROP)**: `Either<EncinaError, T>` en todo el sistema
- **Pay-for-what-you-use**: Todos los patrones son opt-in
- **Provider-agnostic**: Mismas interfaces, diferentes implementaciones
- **.NET 10 Only**: Sin soporte para versiones anteriores
- **Zero Backward Compatibility**: Pre-1.0, cambios breaking aceptados

---

## Arquitectura General

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              APLICACIÓN                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│  Web/API          │  Serverless        │  Workers          │  Console       │
│  ─────────────    │  ────────────      │  ────────         │  ────────      │
│  AspNetCore       │  AwsLambda         │  Hangfire         │  CLI Apps      │
│  GraphQL          │  AzureFunctions    │  Quartz           │                │
│  gRPC             │                    │                   │                │
│  SignalR          │                    │                   │                │
├─────────────────────────────────────────────────────────────────────────────┤
│                           ENCINA CORE                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  IEncina (Mediator)                                                  │    │
│  │  ├── Send<TRequest, TResponse>() → Either<EncinaError, TResponse>   │    │
│  │  ├── Publish<TNotification>() → Either<EncinaError, Unit>           │    │
│  │  └── Stream<TRequest, TItem>() → IAsyncEnumerable<Either<...>>      │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                    │                                         │
│  ┌─────────────────────────────────▼───────────────────────────────────┐    │
│  │  Pipeline (Pre-Processors → Behaviors → Handler → Post-Processors)  │    │
│  │  ├── ValidationPipelineBehavior                                      │    │
│  │  ├── AuthorizationPipelineBehavior                                   │    │
│  │  ├── CachingPipelineBehavior                                         │    │
│  │  ├── TransactionPipelineBehavior                                     │    │
│  │  ├── RetryPipelineBehavior                                           │    │
│  │  ├── CircuitBreakerPipelineBehavior                                  │    │
│  │  └── ...más behaviors...                                             │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────────────────────┤
│                         MESSAGING PATTERNS                                   │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌────────────┐ ┌───────────────┐    │
│  │  Outbox  │ │  Inbox   │ │  Sagas   │ │ Scheduling │ │ Dead Letter   │    │
│  └──────────┘ └──────────┘ └──────────┘ └────────────┘ └───────────────┘    │
│  ┌──────────────┐ ┌───────────────┐ ┌────────────────┐ ┌────────────────┐   │
│  │ Routing Slip │ │Content Router │ │ Scatter-Gather │ │  Choreography  │   │
│  └──────────────┘ └───────────────┘ └────────────────┘ └────────────────┘   │
├─────────────────────────────────────────────────────────────────────────────┤
│                            CROSS-CUTTING                                     │
│  ┌───────────┐ ┌─────────────┐ ┌────────────┐ ┌─────────────────────────┐   │
│  │ Caching   │ │ Resilience  │ │ Dist. Lock │ │ Observability           │   │
│  └───────────┘ └─────────────┘ └────────────┘ └─────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────────┤
│                          DATA PROVIDERS                                      │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌─────────┐ ┌───────────┐     │
│  │ EF Core    │ │ Dapper     │ │ ADO.NET    │ │ MongoDB │ │ Marten    │     │
│  └────────────┘ └────────────┘ └────────────┘ └─────────┘ └───────────┘     │
├─────────────────────────────────────────────────────────────────────────────┤
│                          TRANSPORTS                                          │
│  ┌──────────┐ ┌───────┐ ┌──────────────┐ ┌───────────┐ ┌──────┐ ┌──────┐   │
│  │ RabbitMQ │ │ Kafka │ │ Azure SB     │ │ Amazon SQS│ │ NATS │ │ MQTT │   │
│  └──────────┘ └───────┘ └──────────────┘ └───────────┘ └──────┘ └──────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Paquetes por Categoría

### 1. Core (2 paquetes)

| Paquete | Descripción | Estado |
|---------|-------------|--------|
| **Encina** | Core del mediador, CQRS, pipeline, handlers | ✅ Completo |
| **Encina.Messaging** | Abstracciones de patrones de mensajería | ✅ Completo |

#### Encina (Core) - Contenido Detallado

**Interfaces Principales:**
- `IEncina` - Mediador central
- `IRequest<TResponse>`, `ICommand<TResponse>`, `IQuery<TResponse>` - Contratos CQRS
- `INotification` - Eventos/señales
- `IRequestHandler<TRequest, TResponse>`, `ICommandHandler`, `IQueryHandler`
- `INotificationHandler<TNotification>`
- `IPipelineBehavior<TRequest, TResponse>`
- `IStreamPipelineBehavior<TRequest, TItem>`
- `IRequestPreProcessor<TRequest>`, `IRequestPostProcessor<TRequest, TResponse>`
- `IRequestContext` - Contexto con CorrelationId, UserId, TenantId, IdempotencyKey
- `IValidationProvider` - Abstracción de validación
- `IModule`, `IModuleRegistry`, `IModuleLifecycle` - Sistema de módulos
- `IFunctionalFailureDetector` - Detección de fallos funcionales
- `IEncinaMetrics` - Métricas

**Clases Principales:**
- `Encina` - Implementación del mediador
- `RequestContext` - Contexto inmutable
- `EncinaError` - Record struct para errores
- `EncinaErrors` - Factory de errores
- `ValidationResult`, `ValidationError` - Resultados de validación
- `ValidationOrchestrator`, `ValidationPipelineBehavior`

**Behaviors Incorporados:**
- `CommandActivityPipelineBehavior` - Trazas para comandos
- `QueryActivityPipelineBehavior` - Trazas para queries
- `CommandMetricsPipelineBehavior` - Métricas para comandos
- `QueryMetricsPipelineBehavior` - Métricas para queries

**Dispatchers:**
- `RequestDispatcher` - Despacho de requests
- `NotificationDispatcher` - Despacho de notificaciones (Sequential/Parallel/ParallelWhenAll)
- `StreamDispatcher` - Despacho de streaming

---

### 2. Messaging Patterns (1 paquete, 10 patrones)

| Patrón | Descripción | Estado |
|--------|-------------|--------|
| **Outbox** | Publishing confiable (at-least-once) | ✅ Completo |
| **Inbox** | Procesamiento idempotente (exactly-once) | ✅ Completo |
| **Saga** | Transacciones distribuidas con compensación | ✅ Completo |
| **Scheduling** | Ejecución programada/recurrente | ✅ Completo |
| **Dead Letter Queue** | Manejo de fallos permanentes | ✅ Completo |
| **Recoverability** | Reintentos inteligentes (immediate + delayed) | ✅ Completo |
| **Routing Slip** | Enrutamiento dinámico de pasos | ✅ Completo |
| **Content Router** | Enrutamiento basado en contenido | ✅ Completo |
| **Scatter-Gather** | Distribución y agregación | ✅ Completo |
| **Choreography** | Event bus para sagas coreografiadas | ✅ Completo |

#### Detalle de Cada Patrón

**Outbox Pattern:**
- `IOutboxMessage`, `IOutboxStore`, `IOutboxMessageFactory`
- `OutboxOrchestrator`, `OutboxPostProcessor`
- Configuración: `OutboxOptions` (ProcessingInterval, BatchSize, MaxRetries, BaseRetryDelay)

**Inbox Pattern:**
- `IInboxMessage`, `IInboxStore`, `IInboxMessageFactory`
- `InboxOrchestrator`, `InboxPipelineBehavior`
- `IIdempotentRequest` - Marker interface
- Configuración: `InboxOptions` (MaxRetries, MessageRetentionPeriod, PurgeInterval)

**Saga Pattern:**
- `ISagaState`, `ISagaStore`, `ISagaStateFactory`
- `SagaOrchestrator` (Start, Advance, Complete, Compensate, Fail, Timeout)
- Estados: Running, Completed, Compensating, Compensated, Failed, TimedOut
- Configuración: `SagaOptions` (StuckSagaThreshold, DefaultSagaTimeout)

**Scheduling Pattern:**
- `IScheduledMessage`, `IScheduledMessageStore`
- `SchedulerOrchestrator`
- Soporte para CRON y mensajes recurrentes
- Configuración: `SchedulingOptions` (ProcessingInterval, BatchSize, EnableRecurringMessages)

**Dead Letter Queue:**
- `IDeadLetterMessage`, `IDeadLetterStore`, `IDeadLetterManager`
- `DeadLetterOrchestrator`
- Replay individual y batch
- Configuración: `DeadLetterOptions`

**Recoverability:**
- `IErrorClassifier`, `IDelayedRetryScheduler`
- `RecoverabilityPipelineBehavior`
- Dos fases: Immediate retries + Delayed retries
- Jitter configurable
- Configuración: `RecoverabilityOptions` (ImmediateRetries, DelayedRetries, UseExponentialBackoff)

**Routing Slip:**
- `IRoutingSlipRunner`, `IRoutingSlipState`, `IRoutingSlipStore`
- `RoutingSlipBuilder<TData>` - Fluent API
- Capacidad de añadir/remover pasos dinámicamente
- Activity log completo

**Content Router:**
- `IContentRouter`
- `ContentRouterBuilder<TMessage, TResult>` - Fluent API
- Prioridades, metadata, default routes

**Scatter-Gather:**
- `IScatterGatherRunner`
- `ScatterGatherBuilder<TRequest, TResponse>` - Fluent API
- Estrategias: WaitForAll, WaitForFirst, WaitForQuorum, WaitForAllAllowPartial
- Ejecución paralela o secuencial

**Choreography:**
- `IChoreographyEventBus`, `IChoreographyState`, `IChoreographyStateStore`
- Event-driven sagas sin orquestador central

---

### 3. Database Providers (14 paquetes)

| Paquete | Tecnología | Bases de Datos | Estado |
|---------|------------|----------------|--------|
| **Encina.EntityFrameworkCore** | EF Core | Cualquiera soportada | ✅ Completo |
| **Encina.Dapper.SqlServer** | Dapper | SQL Server | ✅ Completo |
| **Encina.Dapper.PostgreSQL** | Dapper | PostgreSQL | ✅ Completo |
| **Encina.Dapper.MySQL** | Dapper | MySQL/MariaDB | ✅ Completo |
| **Encina.Dapper.Oracle** | Dapper | Oracle | ✅ Completo |
| **Encina.Dapper.Sqlite** | Dapper | SQLite | ✅ Completo |
| **Encina.ADO.SqlServer** | ADO.NET | SQL Server | ✅ Completo |
| **Encina.ADO.PostgreSQL** | ADO.NET | PostgreSQL | ✅ Completo |
| **Encina.ADO.MySQL** | ADO.NET | MySQL/MariaDB | ✅ Completo |
| **Encina.ADO.Oracle** | ADO.NET | Oracle | ✅ Completo |
| **Encina.ADO.Sqlite** | ADO.NET | SQLite | ✅ Completo |
| **Encina.MongoDB** | MongoDB Driver | MongoDB | ✅ Completo |
| **Encina.Marten** | Marten | PostgreSQL (Event Store) | ✅ Completo |
| **Encina.InMemory** | Channels | N/A (Testing) | ✅ Completo |

#### Stores Implementados por Provider

| Store | EF Core | Dapper | ADO.NET | MongoDB | Marten |
|-------|---------|--------|---------|---------|--------|
| OutboxStore | ✅ | ✅ | ✅ | ✅ | N/A |
| InboxStore | ✅ | ✅ | ✅ | ✅ | N/A |
| SagaStore | ✅ | ✅ | ✅ | ✅ | N/A |
| ScheduledMessageStore | ✅ | ✅ | ✅ | ✅ | N/A |
| AggregateRepository | N/A | N/A | N/A | N/A | ✅ |
| SnapshotStore | N/A | N/A | N/A | N/A | ✅ |
| ProjectionManager | N/A | N/A | N/A | N/A | ✅ |

#### Marten (Event Sourcing) - Features Específicas

- `MartenAggregateRepository<TAggregate>` - Repository de agregados
- `IAggregate`, `AggregateBase<TId>` - Base para agregados
- **Projections**: `IProjection`, `MartenProjectionManager`, `InlineProjectionDispatcher`
- **Snapshots**: `ISnapshotStore`, `SnapshotAwareAggregateRepository`
- **Event Versioning**: `IEventUpcaster`, `EventUpcasterRegistry`
- Concurrency detection (optimistic locking)

---

### 4. Caching (8 paquetes)

| Paquete | Tecnología | Tipo | Estado |
|---------|------------|------|--------|
| **Encina.Caching** | Abstracciones | Base | ✅ Completo |
| **Encina.Caching.Memory** | IMemoryCache | Single-instance | ✅ Completo |
| **Encina.Caching.Redis** | StackExchange.Redis | Distributed | ✅ Completo |
| **Encina.Caching.Valkey** | (wrapper Redis) | Distributed | ✅ Completo |
| **Encina.Caching.KeyDB** | (wrapper Redis) | Distributed | ✅ Completo |
| **Encina.Caching.Garnet** | (wrapper Redis) | Distributed | ✅ Completo |
| **Encina.Caching.Dragonfly** | (wrapper Redis) | Distributed | ✅ Completo |
| **Encina.Caching.Hybrid** | HybridCache | L1+L2 | ✅ Completo |

#### Interfaces y Abstracciones

- `ICacheProvider` - GetAsync, SetAsync, RemoveAsync, GetOrSetAsync, RefreshAsync
- `ICacheKeyGenerator` - GenerateKey, GeneratePattern, GeneratePatternFromTemplate
- `IPubSubProvider` - PublishAsync, SubscribeAsync, UnsubscribeAsync

#### Atributos

- `[Cache]` - Cachear respuesta (DurationSeconds, KeyTemplate, VaryByUser, VaryByTenant, Priority)
- `[InvalidatesCache]` - Invalidar cache por patrón (KeyPattern, BroadcastInvalidation, DelayMilliseconds)
- `[CacheableQuery]` - Marker para queries cacheables

#### Behaviors

- `QueryCachingPipelineBehavior` - Cache de queries
- `CacheInvalidationPipelineBehavior` - Invalidación de cache
- `DistributedIdempotencyPipelineBehavior` - Idempotencia vía cache

---

### 5. Message Transports (8 paquetes)

| Paquete | Broker | Tipo | Estado |
|---------|--------|------|--------|
| **Encina.RabbitMQ** | RabbitMQ | Queue/Pub-Sub | ✅ Completo |
| **Encina.Kafka** | Apache Kafka | Stream | ✅ Completo |
| **Encina.AzureServiceBus** | Azure Service Bus | Queue/Topic | ✅ Completo |
| **Encina.AmazonSQS** | AWS SQS/SNS | Queue/Topic | ✅ Completo |
| **Encina.NATS** | NATS | Pub-Sub | ✅ Completo |
| **Encina.MQTT** | MQTT (MQTTnet) | Pub-Sub | ✅ Completo |
| **Encina.Redis.PubSub** | Redis Pub/Sub | Pub-Sub | ✅ Completo |
| **Encina.SignalR** | SignalR | WebSocket | ✅ Completo |

#### Interfaces por Transport

| Transport | Publisher Interface | Features Especiales |
|-----------|---------------------|---------------------|
| RabbitMQ | `IRabbitMQMessagePublisher` | Publisher confirms, Durable messages |
| Kafka | `IKafkaMessagePublisher` | Batch, Headers, Idempotence |
| Azure SB | `IAzureServiceBusMessagePublisher` | Scheduling nativo |
| SQS/SNS | `IAmazonSQSMessagePublisher` | FIFO, Batch, Deduplication |
| NATS | `INATSMessagePublisher` | Request/Reply, JetStream |
| MQTT | `IMQTTMessagePublisher` | QoS levels, Subscriptions |
| Redis | `IRedisPubSubMessagePublisher` | Pattern subscriptions |
| SignalR | `ISignalRNotificationBroadcaster` | Client broadcast, Groups |

---

### 6. Validation (4 paquetes)

| Paquete | Librería | Tipo | Estado |
|---------|----------|------|--------|
| **Encina.FluentValidation** | FluentValidation | Validadores fluent | ✅ Completo |
| **Encina.DataAnnotations** | System.ComponentModel | Atributos built-in | ✅ Completo |
| **Encina.MiniValidator** | MiniValidation | Minimalista | ✅ Completo |
| **Encina.GuardClauses** | Custom | Guards de dominio | ✅ Completo |

#### Arquitectura de Validación

```
IValidationProvider (interface)
    │
    ├── FluentValidationProvider
    ├── DataAnnotationsValidationProvider
    └── MiniValidationProvider

ValidationOrchestrator (coordina validación)
    │
    └── ValidationPipelineBehavior<TRequest, TResponse>
```

#### GuardClauses (Validación de Invariantes)

Métodos Try-pattern:
- `TryValidateNotNull`, `TryValidateNotEmpty`, `TryValidateNotWhiteSpace`
- `TryValidatePositive`, `TryValidateNegative`, `TryValidateInRange`
- `TryValidateEmail`, `TryValidateUrl`, `TryValidatePattern`

---

### 7. Resilience (3 paquetes)

| Paquete | Descripción | Estado |
|---------|-------------|--------|
| **Encina.Extensions.Resilience** | Standard resilience pipeline | ✅ Completo |
| **Encina.Polly** | Políticas vía atributos | ✅ Completo |
| **Encina.DistributedLock** | Abstracciones de locks | ✅ Completo |

#### Encina.Extensions.Resilience

`StandardResiliencePipelineBehavior`:
1. Rate Limiter (1,000 permits default)
2. Total Timeout (30s)
3. Retry (3 attempts, exponential backoff)
4. Circuit Breaker (10% failure threshold)
5. Attempt Timeout (10s)

#### Encina.Polly - Atributos

| Atributo | Función |
|----------|---------|
| `[Retry]` | Reintentos con backoff configurable |
| `[CircuitBreaker]` | Circuit breaker por tipo de request |
| `[RateLimit]` | Rate limiting adaptativo con throttling |
| `[Bulkhead]` | Aislamiento de recursos (semáforos) |

#### Managers

- `IBulkheadManager`, `BulkheadManager` - Gestión de semáforos por handler
- `IRateLimiter`, `AdaptiveRateLimiter` - Rate limiting con estados (Normal/Throttled/Recovering)

---

### 8. Distributed Lock (4 paquetes)

| Paquete | Backend | Estado |
|---------|---------|--------|
| **Encina.DistributedLock** | Abstracciones | ✅ Completo |
| **Encina.DistributedLock.InMemory** | ConcurrentDictionary | ✅ Completo |
| **Encina.DistributedLock.Redis** | Redis (SET NX + Lua) | ✅ Completo |
| **Encina.DistributedLock.SqlServer** | sp_getapplock | ✅ Completo |

#### Interface Principal

```csharp
public interface IDistributedLockProvider
{
    Task<IAsyncDisposable?> TryAcquireAsync(string resource, TimeSpan expiry, TimeSpan wait, TimeSpan retry, CancellationToken ct);
    Task<IAsyncDisposable> AcquireAsync(string resource, TimeSpan expiry, TimeSpan retry, CancellationToken ct);
    Task<bool> IsLockedAsync(string resource, CancellationToken ct);
    Task<bool> ExtendAsync(string resource, string lockId, TimeSpan extension, CancellationToken ct);
}
```

---

### 9. Scheduling (2 paquetes)

| Paquete | Librería | Estado |
|---------|----------|--------|
| **Encina.Hangfire** | Hangfire | ✅ Completo |
| **Encina.Quartz** | Quartz.NET | ✅ Completo |

#### Hangfire

- Adapters: `HangfireRequestJobAdapter`, `HangfireNotificationJobAdapter`
- Extensions para `IBackgroundJobClient`: Enqueue, Schedule, Recurring
- Health check incluido

#### Quartz

- Jobs: `QuartzRequestJob`, `QuartzNotificationJob` (con `[DisallowConcurrentExecution]`)
- Extensions para `IScheduler` y `IServiceCollectionQuartzConfigurator`
- HostedService integrado

---

### 10. Web/API Integration (6 paquetes)

| Paquete | Framework | Estado |
|---------|-----------|--------|
| **Encina.AspNetCore** | ASP.NET Core | ✅ Completo |
| **Encina.AwsLambda** | AWS Lambda | ✅ Completo |
| **Encina.AzureFunctions** | Azure Functions | ✅ Completo |
| **Encina.GraphQL** | HotChocolate/GraphQL | ✅ Completo |
| **Encina.gRPC** | gRPC | ✅ Completo |
| **Encina.Refit** | Refit | ✅ Completo |

#### Encina.AspNetCore

- `EncinaContextMiddleware` - Extrae contexto HTTP
- `AuthorizationPipelineBehavior` - Autorización por atributos
- `ProblemDetailsExtensions` - RFC 7807 responses
- Health checks integration

#### Encina.AwsLambda

- `EventBridgeHandler` - Procesamiento de eventos
- `SqsMessageHandler` - Procesamiento de mensajes SQS (batch)
- `ApiGatewayResponseExtensions` - Conversión a API Gateway format

#### Encina.AzureFunctions

- `EncinaFunctionMiddleware` - Enriquecimiento de contexto
- `DurableSagaBuilder<TData>` - Sagas con Durable Functions
- Health checks incluidos

---

### 11. Observability (1 paquete)

| Paquete | Descripción | Estado |
|---------|-------------|--------|
| **Encina.OpenTelemetry** | Trazas y métricas | ✅ Completo |

#### Features

- `MessagingEnricherPipelineBehavior` - Enriquecimiento automático de actividades
- `MessagingActivityEnricher` - Tags para Outbox, Inbox, Saga, Scheduling
- Integración con traces: `"Encina"` source
- Integración con metrics: `"Encina"` meter

---

### 12. Testing (1 paquete)

| Paquete | Descripción | Estado |
|---------|-------------|--------|
| **Encina.Testing** | Helpers para tests | ✅ Completo |

#### Features

- `EncinaFixture` - DI fixture para tests
- `EitherAssertions` - Assertions para `Either<L, R>`
  - `ShouldBeSuccess`, `ShouldBeError`
  - `ShouldBeErrorWithCode`, `ShouldBeValidationError`
- `AggregateTestBase<TAggregate, TId>` - Given/When/Then para Event Sourcing

---

## Patrones Implementados

### Patrones Arquitectónicos

| Patrón | Ubicación | Descripción |
|--------|-----------|-------------|
| **CQRS** | Encina Core | Separación Command/Query |
| **Mediator** | Encina Core | Desacoplamiento sender/handler |
| **Pipeline** | Encina Core | Behaviors en cadena |
| **Railway Oriented Programming** | Todo | `Either<EncinaError, T>` |
| **Module Pattern** | Encina Core | Organización en módulos |
| **Strategy Pattern** | Múltiples | Dispatch strategies, providers |
| **Factory Pattern** | Múltiples | `*Factory` interfaces |

### Patrones de Messaging

| Patrón | Paquete | Descripción |
|--------|---------|-------------|
| **Transactional Outbox** | Encina.Messaging | Publishing confiable |
| **Idempotent Consumer** | Encina.Messaging (Inbox) | Procesamiento exactly-once |
| **Saga (Orchestration)** | Encina.Messaging | Transacciones distribuidas |
| **Saga (Choreography)** | Encina.Messaging | Event-driven sagas |
| **Routing Slip** | Encina.Messaging | Enrutamiento dinámico |
| **Content-Based Router** | Encina.Messaging | Enrutamiento por contenido |
| **Scatter-Gather** | Encina.Messaging | Distribución y agregación |
| **Dead Letter Channel** | Encina.Messaging | Manejo de fallos |

### Patrones de Resilience

| Patrón | Paquete | Descripción |
|--------|---------|-------------|
| **Retry** | Encina.Polly | Reintentos con backoff |
| **Circuit Breaker** | Encina.Polly | Fail-fast protection |
| **Bulkhead** | Encina.Polly | Aislamiento de recursos |
| **Rate Limiting** | Encina.Polly | Throttling adaptativo |
| **Timeout** | Encina.Extensions.Resilience | Timeouts por operación |

### Patrones de Data Access

| Patrón | Paquete | Descripción |
|--------|---------|-------------|
| **Repository** | Encina.Marten | Aggregate repositories |
| **Unit of Work** | EF Core, Dapper | Transacciones |
| **Event Sourcing** | Encina.Marten | Persistencia de eventos |
| **CQRS Read Models** | Encina.Marten (Projections) | Modelos de lectura |
| **Snapshotting** | Encina.Marten | Optimización de carga |

---

## Características por Ángulo

### Desde la perspectiva EDA (Event-Driven Architecture)

| Característica | Soporte | Paquetes |
|----------------|---------|----------|
| Event Publishing | ✅ | Encina.Messaging (Outbox) |
| Event Subscription | ✅ | Todos los transports |
| Event Sourcing | ✅ | Encina.Marten |
| Event Replay | ✅ | Encina.Marten, Dead Letter |
| Event Versioning | ✅ | Encina.Marten (Upcasters) |
| At-least-once delivery | ✅ | Outbox pattern |
| Exactly-once processing | ✅ | Inbox pattern |

### Desde la perspectiva CQRS

| Característica | Soporte | Paquetes |
|----------------|---------|----------|
| Command/Query separation | ✅ | Encina Core |
| Dedicated handlers | ✅ | ICommandHandler, IQueryHandler |
| Read model projections | ✅ | Encina.Marten |
| Command behaviors | ✅ | ICommandPipelineBehavior |
| Query behaviors | ✅ | IQueryPipelineBehavior |
| Query caching | ✅ | Encina.Caching |

### Desde la perspectiva Microservicios

| Característica | Soporte | Paquetes |
|----------------|---------|----------|
| Inter-service messaging | ✅ | Todos los transports |
| Distributed transactions | ✅ | Sagas, Routing Slips |
| Service resilience | ✅ | Encina.Polly, Extensions.Resilience |
| Distributed caching | ✅ | Encina.Caching.Redis/Valkey/etc. |
| Distributed locking | ✅ | Encina.DistributedLock.* |
| Health checks | ✅ | Todos los providers |
| Observability | ✅ | Encina.OpenTelemetry |

### Desde la perspectiva Cloud-Native

| Característica | Soporte | Paquetes |
|----------------|---------|----------|
| Serverless (AWS) | ✅ | Encina.AwsLambda |
| Serverless (Azure) | ✅ | Encina.AzureFunctions |
| Container-ready | ✅ | Todos (DI-based) |
| Cloud message brokers | ✅ | Azure SB, SQS/SNS |
| Cloud databases | ✅ | MongoDB, PostgreSQL, etc. |

---

## Features Pendientes (Phase 2)

### Issues Abiertos del Milestone

| Issue | Título | Complejidad | Prioridad |
|-------|--------|-------------|-----------|
| **#47** | Encina.Cli - Command-line scaffolding tool | Alta | Media |
| **#50** | Source Generators - Zero-reflection, NativeAOT ready | Muy Alta | Media |
| **#51** | Switch-based dispatch - No dictionary lookup | Alta | Media |

#### #47 - Encina.Cli

**Descripción**: CLI tool para scaffolding de proyectos y componentes Encina.

**Features planificadas**:
- `encina new api MyApi` - Crear proyecto API
- `encina generate handler CreateOrder` - Generar handlers
- `encina generate saga OrderProcessing --steps "Create,Pay,Ship"` - Generar sagas
- `encina add caching redis` - Añadir paquetes
- Integración con `dotnet new` templates

#### #50 - Source Generators

**Descripción**: Generadores de código en tiempo de compilación para zero-reflection dispatch.

**Features planificadas**:
- Handler discovery en compile-time
- Switch-based dispatch (en lugar de diccionarios)
- NativeAOT compatible
- Trimming-safe

#### #51 - Switch-based Dispatch

**Descripción**: Reemplazar dictionary lookups con switch expressions generadas.

**Dependencia**: Requiere #50 (Source Generators)

**Beneficios**:
- Mejor rendimiento en hot path
- Eliminación de reflection en runtime
- Compatibilidad con AOT compilation

---

## Matriz de Completitud

### Por Categoría

| Categoría | Paquetes | Completos | Porcentaje |
|-----------|----------|-----------|------------|
| Core | 2 | 2 | 100% |
| Messaging | 1 | 1 | 100% |
| Database | 14 | 14 | 100% |
| Caching | 8 | 8 | 100% |
| Transports | 8 | 8 | 100% |
| Validation | 4 | 4 | 100% |
| Resilience | 3 | 3 | 100% |
| Dist. Lock | 4 | 4 | 100% |
| Scheduling | 2 | 2 | 100% |
| Web/API | 6 | 6 | 100% |
| Observability | 1 | 1 | 100% |
| Testing | 1 | 1 | 100% |
| **Total** | **52** | **52** | **100%** |

### Features Pendientes

| Feature | Estado | Bloqueante para 1.0 |
|---------|--------|---------------------|
| Source Generators (#50) | Pendiente | No (nice-to-have) |
| CLI Tool (#47) | Pendiente | No (nice-to-have) |
| Switch Dispatch (#51) | Pendiente | No (nice-to-have) |

### Conclusión

**Phase 2: Functionality está al ~93%** con 3 issues restantes que son mejoras de rendimiento y developer experience, no funcionalidades core. El framework está funcionalmente completo para uso en producción.

---

## Apéndice: Registro Completo de Paquetes

```
src/
├── Encina/                          # Core mediator, CQRS, pipeline
├── Encina.Messaging/                # Messaging patterns abstractions
├── Encina.EntityFrameworkCore/      # EF Core provider
├── Encina.Dapper.SqlServer/         # Dapper SQL Server
├── Encina.Dapper.PostgreSQL/        # Dapper PostgreSQL
├── Encina.Dapper.MySQL/             # Dapper MySQL
├── Encina.Dapper.Oracle/            # Dapper Oracle
├── Encina.Dapper.Sqlite/            # Dapper SQLite
├── Encina.ADO.SqlServer/            # ADO.NET SQL Server
├── Encina.ADO.PostgreSQL/           # ADO.NET PostgreSQL
├── Encina.ADO.MySQL/                # ADO.NET MySQL
├── Encina.ADO.Oracle/               # ADO.NET Oracle
├── Encina.ADO.Sqlite/               # ADO.NET SQLite
├── Encina.MongoDB/                  # MongoDB provider
├── Encina.Marten/                   # Event sourcing with Marten
├── Encina.InMemory/                 # In-memory message bus
├── Encina.Caching/                  # Caching abstractions
├── Encina.Caching.Memory/           # In-memory cache
├── Encina.Caching.Redis/            # Redis cache
├── Encina.Caching.Valkey/           # Valkey cache (Redis-compatible)
├── Encina.Caching.KeyDB/            # KeyDB cache (Redis-compatible)
├── Encina.Caching.Garnet/           # Garnet cache (Redis-compatible)
├── Encina.Caching.Dragonfly/        # Dragonfly cache (Redis-compatible)
├── Encina.Caching.Hybrid/           # Hybrid L1+L2 cache
├── Encina.RabbitMQ/                 # RabbitMQ transport
├── Encina.Kafka/                    # Kafka transport
├── Encina.AzureServiceBus/          # Azure Service Bus transport
├── Encina.AmazonSQS/                # AWS SQS/SNS transport
├── Encina.NATS/                     # NATS transport
├── Encina.MQTT/                     # MQTT transport
├── Encina.Redis.PubSub/             # Redis Pub/Sub transport
├── Encina.SignalR/                  # SignalR integration
├── Encina.FluentValidation/         # FluentValidation provider
├── Encina.DataAnnotations/          # DataAnnotations provider
├── Encina.MiniValidator/            # MiniValidator provider
├── Encina.GuardClauses/             # Guard clause utilities
├── Encina.Extensions.Resilience/    # Standard resilience pipeline
├── Encina.Polly/                    # Polly-based policies
├── Encina.DistributedLock/          # Distributed lock abstractions
├── Encina.DistributedLock.InMemory/ # In-memory locks
├── Encina.DistributedLock.Redis/    # Redis locks
├── Encina.DistributedLock.SqlServer/# SQL Server locks
├── Encina.Hangfire/                 # Hangfire integration
├── Encina.Quartz/                   # Quartz.NET integration
├── Encina.AspNetCore/               # ASP.NET Core integration
├── Encina.AwsLambda/                # AWS Lambda integration
├── Encina.AzureFunctions/           # Azure Functions integration
├── Encina.GraphQL/                  # GraphQL integration
├── Encina.gRPC/                     # gRPC integration
├── Encina.Refit/                    # Refit HTTP client integration
├── Encina.OpenTelemetry/            # OpenTelemetry observability
└── Encina.Testing/                  # Testing utilities
```

---

*Documento generado automáticamente mediante análisis exhaustivo del código fuente.*
