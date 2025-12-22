# Encina Component Diagram

This document provides architectural diagrams showing the structure and flow of Encina.

## High-Level Architecture

```mermaid
graph TB
    subgraph "Client Application"
        Client[Client Code<br/>Controller/Service]
    end

    subgraph "Encina Core"
        Encina[IEncina<br/>Encina]
        RequestDispatcher[Request Dispatcher]
        NotificationDispatcher[Notification Dispatcher]
    end

    subgraph "Pipeline Components"
        PreProc[Pre-Processors]
        Behaviors[Pipeline Behaviors]
        PostProc[Post-Processors]
        PipelineBuilder[Pipeline Builder]
    end

    subgraph "User Code"
        ReqHandler[Request Handlers<br/>IRequestHandler]
        NotifHandler[Notification Handlers<br/>INotificationHandler]
    end

    subgraph "Infrastructure"
        DI[Dependency Injection<br/>IServiceScopeFactory]
        Metrics[Metrics & Diagnostics<br/>IEncinaMetrics]
        Logging[Logging<br/>ILogger]
    end

    Client -->|Send/Publish| Encina
    Encina -->|Requests| RequestDispatcher
    Encina -->|Notifications| NotificationDispatcher

    RequestDispatcher -->|Build Pipeline| PipelineBuilder
    PipelineBuilder -->|Compose| PreProc
    PipelineBuilder -->|Compose| Behaviors
    PipelineBuilder -->|Compose| PostProc
    PipelineBuilder -->|Execute| ReqHandler

    NotificationDispatcher -->|Invoke| NotifHandler

    RequestDispatcher -->|Resolve Services| DI
    NotificationDispatcher -->|Resolve Services| DI

    RequestDispatcher -->|Track| Metrics
    NotificationDispatcher -->|Track| Metrics

    Encina -->|Log| Logging

    style Encina fill:#e1f5ff
    style RequestDispatcher fill:#fff4e1
    style NotificationDispatcher fill:#fff4e1
    style ReqHandler fill:#e8f5e9
    style NotifHandler fill:#e8f5e9
```

## Request Processing Flow

```mermaid
sequenceDiagram
    participant Client
    participant Encina as IEncina
    participant Dispatcher as RequestDispatcher
    participant DI as ServiceScope
    participant Pipeline as PipelineBuilder
    participant PreProc as Pre-Processor
    participant Behavior as Pipeline Behavior
    participant Handler as Request Handler
    participant PostProc as Post-Processor
    participant Metrics

    Client->>Encina: Send(request)
    Encina->>Encina: Validate request not null
    Encina->>Dispatcher: ExecuteAsync(request)

    Dispatcher->>DI: CreateScope()
    activate DI
    Dispatcher->>Dispatcher: Resolve cached wrapper
    Dispatcher->>DI: GetService(IRequestHandler)
    DI-->>Dispatcher: handler instance

    Dispatcher->>Pipeline: Build(serviceProvider)
    Pipeline->>DI: GetServices(IPipelineBehavior)
    Pipeline->>DI: GetServices(IRequestPreProcessor)
    Pipeline->>DI: GetServices(IRequestPostProcessor)
    Pipeline-->>Dispatcher: pipeline delegate

    Dispatcher->>Pipeline: Execute pipeline()
    activate Pipeline

    Pipeline->>PreProc: Process(request)
    alt Pre-processor fails
        PreProc-->>Pipeline: Left(error)
        Pipeline-->>Dispatcher: Short-circuit
    else Pre-processor succeeds
        PreProc-->>Pipeline: Continue
    end

    Pipeline->>Behavior: Handle(request, next)
    activate Behavior

    Behavior->>Handler: Handle(request)
    activate Handler
    Handler->>Handler: Business Logic
    Handler-->>Behavior: TResponse
    deactivate Handler

    Behavior-->>Pipeline: Either<Error, TResponse>
    deactivate Behavior

    Pipeline->>PostProc: Process(request, response)
    alt Post-processor fails
        PostProc-->>Pipeline: Left(error)
    else Post-processor succeeds
        PostProc-->>Pipeline: Continue
    end

    Pipeline-->>Dispatcher: Either<Error, TResponse>
    deactivate Pipeline

    Dispatcher->>Metrics: TrackSuccess/TrackFailure
    Dispatcher->>DI: Dispose scope
    deactivate DI

    Dispatcher-->>Encina: Either<Error, TResponse>
    Encina-->>Client: Either<Error, TResponse>
```

## Notification Broadcasting Flow

```mermaid
sequenceDiagram
    participant Client
    participant Encina as IEncina
    participant Dispatcher as NotificationDispatcher
    participant DI as ServiceScope
    participant Cache as InvokerCache
    participant Handler1 as Handler 1
    participant Handler2 as Handler 2
    participant HandlerN as Handler N

    Client->>Encina: Publish(notification)
    Encina->>Dispatcher: ExecuteAsync(notification)

    Dispatcher->>DI: CreateScope()
    activate DI
    Dispatcher->>DI: GetServices(INotificationHandler)
    DI-->>Dispatcher: [handler1, handler2, ..., handlerN]

    alt No handlers registered
        Dispatcher-->>Encina: Right(Unit) - OK
    else Handlers exist
        loop For each handler
            Dispatcher->>Cache: GetOrAdd(handlerType, notificationType)
            alt Cache hit
                Cache-->>Dispatcher: compiled delegate
            else Cache miss
                Cache->>Cache: CreateNotificationInvoker()
                Cache->>Cache: Compile Expression tree
                Cache-->>Dispatcher: compiled delegate
            end

            Dispatcher->>Handler1: invoker(handler, notification, ct)
            activate Handler1
            Handler1->>Handler1: Handle notification
            Handler1-->>Dispatcher: Task
            deactivate Handler1

            alt Handler fails
                Dispatcher-->>Encina: Left(error) - Fail fast
            else Handler succeeds
                Dispatcher->>Handler2: invoker(handler, notification, ct)
                activate Handler2
                Handler2->>Handler2: Handle notification
                Handler2-->>Dispatcher: Task
                deactivate Handler2
            end
        end
    end

    Dispatcher->>DI: Dispose scope
    deactivate DI
    Dispatcher-->>Encina: Either<Error, Unit>
    Encina-->>Client: Either<Error, Unit>
```

## Component Responsibilities

### Core Components

| Component | Responsibility | Lifetime |
|-----------|---------------|----------|
| **Encina** | Entry point, validation, dispatcher coordination | Singleton |
| **RequestDispatcher** | Orchestrates request pipeline execution | Static class |
| **NotificationDispatcher** | Broadcasts to multiple handlers | Static class |
| **PipelineBuilder** | Composes behaviors and processors into callable delegate | Per-request instance |

### Pipeline Components

| Component | Responsibility | Lifetime |
|-----------|---------------|----------|
| **IRequestPreProcessor** | Side-effect before handler (e.g., logging input) | Transient/Scoped |
| **IPipelineBehavior** | Cross-cutting concern wrapping handler (e.g., caching, validation) | Transient/Scoped |
| **IRequestPostProcessor** | Side-effect after handler (e.g., audit trail) | Transient/Scoped |
| **IRequestHandler** | Core business logic for a request | Transient/Scoped |
| **INotificationHandler** | Subscriber to a notification event | Transient/Scoped |

### Infrastructure Components

| Component | Responsibility | Lifetime |
|-----------|---------------|----------|
| **IServiceScopeFactory** | Creates DI scopes for request isolation | Singleton |
| **IEncinaMetrics** | Tracks success/failure metrics | Singleton |
| **ILogger** | Diagnostic logging | Singleton |
| **EncinaDiagnostics** | OpenTelemetry activity/tracing support | Static class |

## Pipeline Composition Pattern

```mermaid
graph LR
    subgraph "Outer Layer"
        PreProc1[Pre-Processor 1]
        PreProc2[Pre-Processor 2]
    end

    subgraph "Middle Layer - Behaviors (Russian Doll)"
        Behavior1[Behavior A<br/>e.g., Logging]
        Behavior2[Behavior B<br/>e.g., Caching]
        Behavior3[Behavior C<br/>e.g., Validation]
    end

    subgraph "Core"
        Handler[Request Handler]
    end

    subgraph "Outer Layer"
        PostProc1[Post-Processor 1]
        PostProc2[Post-Processor 2]
    end

    PreProc1 --> PreProc2
    PreProc2 --> Behavior1
    Behavior1 --> Behavior2
    Behavior2 --> Behavior3
    Behavior3 --> Handler
    Handler --> PostProc1
    PostProc1 --> PostProc2

    style Handler fill:#4caf50,color:#fff
    style Behavior1 fill:#2196f3,color:#fff
    style Behavior2 fill:#2196f3,color:#fff
    style Behavior3 fill:#2196f3,color:#fff
```

**Key Insight:** Behaviors are nested delegates (onion architecture). The outermost behavior is called first but can execute code both before and after calling `next()`, giving it full control over the request/response lifecycle.

## Error Flow (Railway Oriented Programming)

```mermaid
graph TB
    Start[Request Received]

    Start --> ValidateRequest{Validate<br/>Request}
    ValidateRequest -->|Null| ErrorNull[Left: Request Null]
    ValidateRequest -->|Valid| ResolveHandler{Resolve<br/>Handler}

    ResolveHandler -->|Missing| ErrorMissing[Left: Handler Missing]
    ResolveHandler -->|Wrong Type| ErrorType[Left: Type Mismatch]
    ResolveHandler -->|Valid| PreProc{Execute<br/>Pre-Processors}

    PreProc -->|Failure| ErrorPreProc[Left: PreProcessor Error]
    PreProc -->|Success| Behaviors{Execute<br/>Behaviors}

    Behaviors -->|Failure| ErrorBehavior[Left: Behavior Error]
    Behaviors -->|Success| Handler{Execute<br/>Handler}

    Handler -->|Exception| ErrorHandler[Left: Handler Exception]
    Handler -->|Cancellation| ErrorCancel[Left: Cancelled]
    Handler -->|Success| PostProc{Execute<br/>Post-Processors}

    PostProc -->|Failure| ErrorPostProc[Left: PostProcessor Error]
    PostProc -->|Success| Success[Right: TResponse]

    ErrorNull --> Return[Return Either]
    ErrorMissing --> Return
    ErrorType --> Return
    ErrorPreProc --> Return
    ErrorBehavior --> Return
    ErrorHandler --> Return
    ErrorCancel --> Return
    ErrorPostProc --> Return
    Success --> Return

    style ErrorNull fill:#f44336,color:#fff
    style ErrorMissing fill:#f44336,color:#fff
    style ErrorType fill:#f44336,color:#fff
    style ErrorPreProc fill:#f44336,color:#fff
    style ErrorBehavior fill:#f44336,color:#fff
    style ErrorHandler fill:#f44336,color:#fff
    style ErrorCancel fill:#ff9800,color:#fff
    style ErrorPostProc fill:#f44336,color:#fff
    style Success fill:#4caf50,color:#fff
```

**Key Principle:** Any Left value short-circuits the pipeline. All errors flow through the same `Either<EncinaError, TResponse>` type, enabling consistent error handling.

## Caching Architecture

```mermaid
graph TB
    subgraph "Request Handling"
        ReqDispatcher[Request Dispatcher]
        ReqCache[(Request Handler<br/>Wrapper Cache<br/>ConcurrentDictionary)]
        ReqWrapper[Request Handler Wrapper]
    end

    subgraph "Notification Handling"
        NotifDispatcher[Notification Dispatcher]
        InvokerCache[(Notification Invoker<br/>Cache<br/>ConcurrentDictionary)]
        Compiler[Expression Tree<br/>Compiler]
        Invoker[Compiled Delegate]
    end

    ReqDispatcher -->|GetOrAdd<br/>requestType, responseType| ReqCache
    ReqCache -->|Cache Hit| ReqWrapper
    ReqCache -->|Cache Miss| CreateWrapper[Create Wrapper]
    CreateWrapper --> ReqWrapper

    NotifDispatcher -->|GetOrAdd<br/>handlerType, notificationType| InvokerCache
    InvokerCache -->|Cache Hit| Invoker
    InvokerCache -->|Cache Miss| Compiler
    Compiler -->|Compile Expression| Invoker

    style ReqCache fill:#fff9c4
    style InvokerCache fill:#fff9c4
    style Compiler fill:#ffccbc
```

**Performance:**
- **First call:** ~1-5ms (compilation overhead)
- **Subsequent calls:** ~10ns (dictionary lookup) + 180ns (delegate invocation)
- **Total improvement:** 50-100x faster than reflection

## Dependency Graph

```mermaid
graph TB
    subgraph "User Code Layer"
        Handlers[Request/Notification<br/>Handlers]
        Behaviors[Pipeline Behaviors]
        Processors[Pre/Post Processors]
    end

    subgraph "Encina Layer"
        Encina[Encina]
        Dispatchers[Dispatchers]
        Pipeline[PipelineBuilder]
    end

    subgraph "Abstraction Layer"
        Contracts[IEncina<br/>IRequest<br/>INotification<br/>IRequestHandler<br/>INotificationHandler<br/>IPipelineBehavior]
    end

    subgraph "Infrastructure Layer"
        DI[Microsoft.Extensions<br/>.DependencyInjection]
        Logging[Microsoft.Extensions<br/>.Logging]
        LanguageExt[LanguageExt<br/>Either/Option/Unit]
    end

    Handlers -.->|implements| Contracts
    Behaviors -.->|implements| Contracts
    Processors -.->|implements| Contracts

    Encina -->|uses| Contracts
    Encina -->|uses| Dispatchers
    Dispatchers -->|uses| Pipeline

    Encina -->|depends on| DI
    Encina -->|depends on| Logging
    Encina -->|depends on| LanguageExt

    Dispatchers -->|depends on| DI
    Pipeline -->|depends on| DI

    style Contracts fill:#e1f5ff
    style Handlers fill:#e8f5e9
    style Encina fill:#fff4e1
```

## Key Design Patterns

| Pattern | Component | Purpose |
|---------|-----------|---------|
| **Encina** | Encina | Decouples request sender from handler |
| **Chain of Responsibility** | Pipeline Behaviors | Sequential processing with short-circuiting |
| **Decorator** | PipelineBuilder | Dynamically wrap handler with behaviors |
| **Observer** | Notification Handlers | Multiple subscribers to same event |
| **Factory** | RequestHandlerWrapper | Abstract handler creation and invocation |
| **Repository** | ConcurrentDictionary Caches | Store and retrieve compiled delegates |
| **Railway Oriented Programming** | Either<L,R> | Explicit error handling without exceptions |

## Related Documentation

- [ADR-001: Railway Oriented Programming](adr/001-railway-oriented-programming.md) - Error handling strategy
- [ADR-002: Dependency Injection Strategy](adr/002-dependency-injection-strategy.md) - DI lifetimes and scoping
- [ADR-003: Caching Strategy](adr/003-caching-strategy.md) - Performance optimization approach
- [Patterns Guide](patterns-guide.md) - Detailed explanation of design patterns used
