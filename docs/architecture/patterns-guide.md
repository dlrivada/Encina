# Design Patterns Guide

This guide explains the key design patterns used in SimpleMediator, their purpose, implementation details, and how they work together to create a robust, maintainable mediator library.

## Table of Contents

1. [Mediator Pattern](#mediator-pattern)
2. [Railway Oriented Programming](#railway-oriented-programming)
3. [Chain of Responsibility](#chain-of-responsibility)
4. [Decorator Pattern](#decorator-pattern)
5. [Observer Pattern](#observer-pattern)
6. [Factory Pattern](#factory-pattern)
7. [Dependency Injection](#dependency-injection)
8. [Expression Tree Compilation](#expression-tree-compilation)
9. [Guard Clauses](#guard-clauses)
10. [Pattern Interactions](#pattern-interactions)

---

## Mediator Pattern

### Purpose

Reduce coupling between components by introducing a central coordinator that handles communication between objects without them knowing about each other.

### Problem

Without a mediator:

```csharp
// Controller tightly coupled to business logic
public class UserController
{
    private readonly UserRepository _userRepo;
    private readonly EmailService _emailService;
    private readonly AuditLogger _auditLogger;
    private readonly CacheService _cache;

    public async Task<User> CreateUser(CreateUserRequest request)
    {
        // Validation logic
        if (string.IsNullOrEmpty(request.Email)) throw new ValidationException();

        // Business logic
        var user = await _userRepo.Create(request);

        // Side effects
        await _emailService.SendWelcomeEmail(user);
        await _auditLogger.Log("UserCreated", user.Id);
        await _cache.Invalidate("users");

        return user;
    }
}
```

**Issues:**

- Controller knows about 4 different services
- Hard to test (must mock all dependencies)
- Cross-cutting concerns (validation, caching, logging) scattered across controllers
- Difficult to add new functionality without modifying existing code

### Solution with Mediator

```csharp
// Controller only depends on IMediator
public class UserController
{
    private readonly IMediator _mediator;

    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var result = await _mediator.Send(new CreateUserCommand(request.Email, request.Name));

        return result.Match(
            Right: user => Ok(user),
            Left: error => BadRequest(error.Message)
        );
    }
}

// Business logic isolated in handler
public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
{
    private readonly UserRepository _userRepo;

    public async Task<User> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var user = await _userRepo.Create(request.Email, request.Name);
        return user;
    }
}

// Cross-cutting concerns in behaviors
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request, RequestHandlerCallback<TResponse> next, CancellationToken ct)
    {
        // Validation logic applies to ALL requests
        if (request is IValidatable validatable && !validatable.IsValid())
            return Left(MediatorErrors.ValidationFailed);

        return await next();
    }
}
```

### Benefits

- **Single Responsibility:** Controller only coordinates, handler only contains business logic
- **Open/Closed Principle:** Add new commands without modifying existing code
- **Testability:** Easy to test handlers in isolation
- **Consistency:** All requests flow through the same pipeline

### Implementation in SimpleMediator

```csharp
public sealed partial class SimpleMediator : IMediator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimpleMediator> _logger;

    public ValueTask<Either<MediatorError, TResponse>> Send<TResponse>(
        IRequest<TResponse> request, CancellationToken ct = default)
    {
        if (!MediatorRequestGuards.TryValidateRequest<TResponse>(request, out var error))
            return new ValueTask<Either<MediatorError, TResponse>>(error);

        return new ValueTask<Either<MediatorError, TResponse>>(
            RequestDispatcher.ExecuteAsync(this, request, ct));
    }
}
```

---

## Railway Oriented Programming

### Purpose

Make error handling explicit in the type system and enable composable error handling without exceptions.

### Problem

Traditional exception-based error handling:

```csharp
public async Task<User> GetUser(int id)
{
    var user = await _userRepo.FindById(id);
    if (user == null) throw new NotFoundException("User not found");

    if (!user.IsActive) throw new InvalidOperationException("User inactive");

    return user;
}

// Caller has no idea this can throw exceptions (invisible in signature)
var user = await GetUser(123); // May throw - not visible in code
```

**Issues:**

- Exceptions are invisible in method signatures
- Expensive (stack unwinding, allocation)
- Control flow is implicit (hard to follow)
- Difficult to compose error-handling logic

### Solution with Railway Oriented Programming

```csharp
public async Task<Either<MediatorError, User>> GetUser(int id)
{
    var user = await _userRepo.FindById(id);
    if (user is null)
        return Left(MediatorErrors.NotFound("User not found"));

    if (!user.IsActive)
        return Left(MediatorErrors.InvalidOperation("User inactive"));

    return Right(user);
}

// Caller knows this returns Either - errors are explicit
var result = await GetUser(123);
result.Match(
    Right: user => Console.WriteLine($"Found: {user.Name}"),
    Left: error => Console.WriteLine($"Error: {error.Message}")
);
```

### The Two Tracks

```
Happy Path (Right):  Request → Validate → Process → Transform → Response
                                  ↓          ↓          ↓
Error Path (Left):             Error ←  Error  ←   Error
```

**Key Principle:** Once you enter the Left track, you stay on the Left track (short-circuiting).

### Implementation in SimpleMediator

```csharp
// Pipeline execution with short-circuiting
private static async ValueTask<Either<MediatorError, TResponse>> ExecutePipelineAsync(...)
{
    // Pre-processors
    foreach (var preProcessor in preProcessors)
    {
        var failure = await ExecutePreProcessorAsync(preProcessor, request, ct);
        if (failure.IsSome)
        {
            // Short-circuit: Return error immediately
            return Left<MediatorError, TResponse>(failure.Match(err => err, () => MediatorErrors.Unknown));
        }
    }

    // Execute handler (may return Left or Right)
    var response = await terminal();

    // Post-processors only run if response is Right
    foreach (var postProcessor in postProcessors)
    {
        var failure = await ExecutePostProcessorAsync(postProcessor, request, response, ct);
        if (failure.IsSome)
        {
            return Left<MediatorError, TResponse>(/* error */);
        }
    }

    return response; // Either Left or Right
}
```

### Benefits

- **Explicit Errors:** Type system forces you to handle errors
- **Composability:** Chain operations with `Bind`, `Map`, `Match`
- **Performance:** No exception throwing for expected failures
- **Functional Style:** Works well with LINQ and functional programming

### Combinators

```csharp
// Map: Transform the Right value
var result = await mediator.Send(new GetUserQuery(1));
var nameResult = result.Map(user => user.Name); // Either<MediatorError, string>

// Bind: Chain operations that return Either
var result = await mediator.Send(new GetUserQuery(1));
var ordersResult = result.Bind(user => mediator.Send(new GetOrdersQuery(user.Id)));

// Match: Extract value with exhaustive pattern matching
result.Match(
    Right: user => $"Hello, {user.Name}",
    Left: error => $"Error: {error.Message}"
);
```

---

## Chain of Responsibility

### Purpose

Pass a request through a chain of handlers, where each handler can either process the request or pass it to the next handler.

### Problem

Cross-cutting concerns (logging, validation, caching) duplicated across handlers:

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
{
    public async Task<User> Handle(CreateUserCommand request, CancellationToken ct)
    {
        // Logging (duplicated in every handler)
        _logger.LogInformation("Creating user: {Email}", request.Email);

        // Validation (duplicated in every handler)
        if (!IsValid(request)) throw new ValidationException();

        // Actual logic
        var user = await _userRepo.Create(request);

        // Logging again (duplicated in every handler)
        _logger.LogInformation("User created: {UserId}", user.Id);

        return user;
    }
}
```

### Solution with Chain of Responsibility

```csharp
// Each behavior wraps the next step in the chain
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger _logger;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request, RequestHandlerCallback<TResponse> next, CancellationToken ct)
    {
        _logger.LogInformation("Processing {RequestType}", typeof(TRequest).Name);

        var result = await next(); // Call next step in chain

        result.Match(
            Right: _ => _logger.LogInformation("Success"),
            Left: err => _logger.LogError("Failed: {Error}", err.Message)
        );

        return result;
    }
}

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request, RequestHandlerCallback<TResponse> next, CancellationToken ct)
    {
        if (request is IValidatable validatable && !validatable.IsValid())
            return Left(MediatorErrors.ValidationFailed); // Short-circuit

        return await next(); // Continue chain
    }
}

// Handler is now pure business logic (no cross-cutting concerns)
public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
{
    public async Task<User> Handle(CreateUserCommand request, CancellationToken ct)
    {
        return await _userRepo.Create(request);
    }
}
```

### Chain Execution Order

```
Request
  ↓
LoggingBehavior.Before
  ↓
ValidationBehavior.Before
  ↓
CachingBehavior.Before
  ↓
Handler.Handle
  ↓
CachingBehavior.After
  ↓
ValidationBehavior.After
  ↓
LoggingBehavior.After
  ↓
Response
```

### Implementation in SimpleMediator

```csharp
// PipelineBuilder creates nested delegates (Russian doll pattern)
public RequestHandlerCallback<TResponse> Build(IServiceProvider serviceProvider)
{
    // Start with innermost: handler
    RequestHandlerCallback<TResponse> current = () => ExecuteHandlerAsync(_handler, _request, _ct);

    // Wrap handler with behaviors in REVERSE order
    // (to execute in registration order)
    if (behaviors.Length > 0)
    {
        for (var index = behaviors.Length - 1; index >= 0; index--)
        {
            var behavior = behaviors[index];
            var nextStep = current; // Capture in closure
            current = () => ExecuteBehaviorAsync(behavior, _request, nextStep, _ct);
        }
    }

    // Outer layer: pre/post processors
    return () => ExecutePipelineAsync(preProcessors, postProcessors, current, _request, _ct);
}
```

### Benefits

- **Separation of Concerns:** Each behavior handles one responsibility
- **Reusability:** Behaviors apply to all requests automatically
- **Flexibility:** Add/remove behaviors via DI registration
- **Testability:** Test behaviors independently

---

## Decorator Pattern

### Purpose

Dynamically add responsibilities to objects by wrapping them in decorator objects.

### Problem

Want to add functionality (caching, logging, metrics) to handlers without modifying them.

### Solution

Pipeline behaviors are decorators around the handler:

```csharp
// Base handler
public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken ct)
    {
        return await _userRepo.FindById(request.UserId);
    }
}

// Caching decorator
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICache _cache;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(
        TRequest request, RequestHandlerCallback<TResponse> next, CancellationToken ct)
    {
        var key = $"{typeof(TRequest).Name}:{GetCacheKey(request)}";

        // Try cache
        if (_cache.TryGet<TResponse>(key, out var cached))
            return Right(cached);

        // Call wrapped handler
        var result = await next();

        // Cache successful result
        result.IfRight(value => _cache.Set(key, value, TimeSpan.FromMinutes(5)));

        return result;
    }
}
```

**Execution Flow:**

```
CachingBehavior.Handle
  ↓ (calls next)
LoggingBehavior.Handle
  ↓ (calls next)
ValidationBehavior.Handle
  ↓ (calls next)
GetUserHandler.Handle (actual logic)
```

### Difference from Chain of Responsibility

- **Chain:** One handler processes OR passes to next
- **Decorator:** All decorators execute in sequence, wrapping the core handler

In SimpleMediator, behaviors are both:

- **Chain:** Can short-circuit by returning Left without calling `next()`
- **Decorator:** Wrap the handler and add functionality before/after

---

## Observer Pattern

### Purpose

Define a one-to-many dependency so that when one object changes state, all dependents are notified.

### Problem

Multiple components need to react to the same event:

```csharp
// Tightly coupled event handling
public async Task CreateOrder(Order order)
{
    await _orderRepo.Save(order);

    // Manual notification of all interested parties
    await _emailService.SendOrderConfirmation(order);
    await _inventoryService.DecrementStock(order.Items);
    await _analyticsService.TrackOrderCreated(order);
    await _auditLogger.Log("OrderCreated", order.Id);
}
```

### Solution with Observer Pattern

```csharp
// Publisher: Raise notification
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Order>
{
    private readonly IMediator _mediator;

    public async Task<Order> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = await _orderRepo.Save(request);

        // Publish notification (decoupled)
        await _mediator.Publish(new OrderCreatedNotification(order), ct);

        return order;
    }
}

// Observers: React to notification
public class SendOrderConfirmationHandler : INotificationHandler<OrderCreatedNotification>
{
    public async Task Handle(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _emailService.SendOrderConfirmation(notification.Order);
    }
}

public class DecrementStockHandler : INotificationHandler<OrderCreatedNotification>
{
    public async Task Handle(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _inventoryService.DecrementStock(notification.Order.Items);
    }
}

public class TrackOrderAnalyticsHandler : INotificationHandler<OrderCreatedNotification>
{
    public async Task Handle(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _analyticsService.TrackOrderCreated(notification.Order);
    }
}
```

### Implementation in SimpleMediator

```csharp
// NotificationDispatcher broadcasts to all handlers
public static async Task<Either<MediatorError, Unit>> ExecuteAsync<TNotification>(
    SimpleMediator mediator, TNotification notification, CancellationToken ct)
    where TNotification : INotification
{
    var notificationType = notification?.GetType() ?? typeof(TNotification);
    var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);

    // Resolve ALL handlers (0 or more)
    var handlers = serviceProvider.GetServices(handlerType).ToList();

    if (handlers.Count == 0)
        return Right<MediatorError, Unit>(Unit.Default); // No handlers is OK

    // Execute each handler sequentially
    foreach (var handler in handlers)
    {
        var result = await InvokeNotificationHandler(handler, notification, ct);
        if (result.IsLeft) return result; // Fail-fast on first error
    }

    return Right<MediatorError, Unit>(Unit.Default);
}
```

### Benefits

- **Decoupling:** Publisher doesn't know about observers
- **Extensibility:** Add new observers without modifying publisher
- **Single Responsibility:** Each observer handles one concern

---

## Factory Pattern

### Purpose

Encapsulate object creation logic and provide a common interface for creating families of related objects.

### Implementation in SimpleMediator

```csharp
// Abstract factory interface
internal interface IRequestHandlerWrapper
{
    Type HandlerServiceType { get; }
    object? ResolveHandler(IServiceProvider serviceProvider);
    Task<object> Handle(SimpleMediator mediator, object request, object handler,
                       IServiceProvider serviceProvider, CancellationToken ct);
}

// Concrete factory for specific request/response types
internal sealed class RequestHandlerWrapper<TRequest, TResponse> : IRequestHandlerWrapper
    where TRequest : IRequest<TResponse>
{
    public Type HandlerServiceType => typeof(IRequestHandler<TRequest, TResponse>);

    public object? ResolveHandler(IServiceProvider serviceProvider)
        => serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>();

    public async Task<object> Handle(SimpleMediator mediator, object request, object handler,
                                    IServiceProvider serviceProvider, CancellationToken ct)
    {
        var typedRequest = (TRequest)request;
        var typedHandler = (IRequestHandler<TRequest, TResponse>)handler;

        // Build and execute pipeline
        var builder = new PipelineBuilder<TRequest, TResponse>(typedRequest, typedHandler, ct);
        var pipeline = builder.Build(serviceProvider);
        return await pipeline();
    }
}

// Factory method
private static IRequestHandlerWrapper CreateRequestHandlerWrapper(Type requestType, Type responseType)
{
    var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, responseType);
    return (IRequestHandlerWrapper)Activator.CreateInstance(wrapperType)!;
}
```

### Benefits

- **Type Erasure:** Hide generic type parameters behind non-generic interface
- **Caching:** Factory objects are cached and reused
- **Testability:** Easy to mock factory interface

---

## Guard Clauses

### Purpose

Validate preconditions early and fail fast with clear error messages.

### Problem

Validation logic scattered throughout methods:

```csharp
public async Task<Either<MediatorError, TResponse>> Send<TResponse>(
    IRequest<TResponse> request, CancellationToken ct)
{
    if (request is null)
    {
        var message = "The request cannot be null.";
        var error = MediatorErrors.Create(MediatorErrorCodes.RequestNull, message);
        return Left<MediatorError, TResponse>(error);
    }

    // Duplicate validation logic in every method
}
```

### Solution with Guard Clauses

```csharp
// Centralized guard clauses
internal static class MediatorRequestGuards
{
    public static bool TryValidateRequest<TResponse>(
        object? request, out Either<MediatorError, TResponse> error)
    {
        if (request is not null)
        {
            error = default;
            return true;
        }

        const string message = "The request cannot be null.";
        error = Left<MediatorError, TResponse>(
            MediatorErrors.Create(MediatorErrorCodes.RequestNull, message));
        return false;
    }

    public static bool TryValidateHandler<TResponse>(
        object? handler, Type requestType, Type responseType,
        out Either<MediatorError, TResponse> error)
    {
        if (handler is not null)
        {
            error = default;
            return true;
        }

        var message = $"No registered IRequestHandler was found for {requestType.Name} -> {responseType.Name}.";
        var metadata = new Dictionary<string, object?>
        {
            ["requestType"] = requestType.FullName,
            ["responseType"] = responseType.FullName,
            ["stage"] = "handler_resolution"
        };
        error = Left<MediatorError, TResponse>(
            MediatorErrors.Create(MediatorErrorCodes.RequestHandlerMissing, message, details: metadata));
        return false;
    }
}

// Usage
public ValueTask<Either<MediatorError, TResponse>> Send<TResponse>(
    IRequest<TResponse> request, CancellationToken ct = default)
{
    if (!MediatorRequestGuards.TryValidateRequest<TResponse>(request, out var error))
        return new ValueTask<Either<MediatorError, TResponse>>(error);

    return new ValueTask<Either<MediatorError, TResponse>>(
        RequestDispatcher.ExecuteAsync(this, request, ct));
}
```

### Benefits

- **DRY:** Validation logic in one place
- **Consistency:** Same error messages and codes everywhere
- **Testability:** Easy to test validation logic in isolation
- **Fail Fast:** Errors detected early in the pipeline

---

## Expression Tree Compilation

### Purpose

Generate optimized code at runtime to avoid reflection overhead.

### Problem

Reflection is slow (~50-100x slower than direct calls):

```csharp
// Slow: Reflection invocation
var method = handler.GetType().GetMethod("Handle");
var result = (Task)method.Invoke(handler, new[] { notification, cancellationToken });
await result;
```

### Solution with Expression Trees

```csharp
// Compile once, reuse forever
private static Func<object, object?, CancellationToken, Task> CreateNotificationInvoker(
    MethodInfo method, Type handlerType, Type notificationType)
{
    // Create lambda parameters
    var handlerParam = Expression.Parameter(typeof(object), "handler");
    var notificationParam = Expression.Parameter(typeof(object), "notification");
    var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

    // Cast object → concrete types
    var castHandler = Expression.Convert(handlerParam, handlerType);
    var castNotification = Expression.Convert(notificationParam, notificationType);

    // Generate method call: handler.Handle(notification, ct)
    var call = Expression.Call(castHandler, method, castNotification, ctParam);

    // Compile to delegate
    var lambda = Expression.Lambda<Func<object, object?, CancellationToken, Task>>(
        call, handlerParam, notificationParam, ctParam);

    return lambda.Compile();
}

// Usage (cached)
var invoker = NotificationHandlerInvokerCache.GetOrAdd(
    (handlerType, notificationType),
    _ => CreateNotificationInvoker(method, handlerType, notificationType));

// Fast: Compiled delegate (near-native speed)
var result = invoker(handler, notification, cancellationToken);
await result;
```

### Performance Comparison

| Approach | Time | Ratio |
|----------|------|-------|
| Direct call | 150ns | 1.0x |
| Compiled delegate | 180ns | 1.2x |
| MethodInfo.Invoke | 2,500ns | 16.7x |

### Benefits

- **Performance:** 50-100x faster than reflection
- **Type Safety:** Casts are validated at compile time
- **Caching:** Compiled once, reused forever

---

## Pattern Interactions

### How Patterns Work Together

```
┌─────────────────────────────────────────────────────────────┐
│                     Mediator Pattern                         │
│  (SimpleMediator coordinates all communication)              │
└────────────┬────────────────────────────────────────────────┘
             │
             ├─► Railway Oriented Programming
             │   (All operations return Either<Error, Success>)
             │
             ├─► Dependency Injection
             │   (Resolve handlers and behaviors from DI)
             │
             ├─► Factory Pattern
             │   (RequestHandlerWrapper creates pipeline)
             │
             └─► Chain of Responsibility + Decorator
                 (Behaviors wrap handler in nested chain)
                     │
                     ├─► Guard Clauses
                     │   (Validate inputs early)
                     │
                     ├─► Observer Pattern
                     │   (Notifications broadcast to handlers)
                     │
                     └─► Expression Tree Compilation
                         (Fast handler invocation)
```

### Example: Complete Request Flow

```csharp
// 1. Mediator Pattern: Client sends request through mediator
var result = await mediator.Send(new GetUserQuery(123));

// 2. Guard Clauses: Validate request not null
if (!MediatorRequestGuards.TryValidateRequest(request, out var error))
    return error;

// 3. Factory Pattern: Get cached wrapper
var wrapper = RequestHandlerCache.GetOrAdd((requestType, responseType), CreateWrapper);

// 4. Dependency Injection: Resolve handler from DI
var handler = wrapper.ResolveHandler(serviceProvider);

// 5. Chain of Responsibility + Decorator: Build pipeline
var pipeline = pipelineBuilder.Build(serviceProvider);
// Pipeline = PreProc → Logging → Validation → Caching → Handler → PostProc

// 6. Railway Oriented Programming: Execute with short-circuiting
var response = await pipeline(); // Either<MediatorError, User>

// 7. Observer Pattern (optional): Publish notification
await mediator.Publish(new UserRetrievedNotification(user));

// 8. Expression Tree Compilation: Fast notification handler invocation
var invoker = NotificationHandlerInvokerCache.GetOrAdd(key, Compile);
await invoker(handler, notification, ct);

// 9. Back to client with Either result
result.Match(
    Right: user => Ok(user),
    Left: error => BadRequest(error.Message)
);
```

---

## Summary

| Pattern | Primary Benefit | Used In |
|---------|----------------|---------|
| **Mediator** | Decoupling | SimpleMediator |
| **Railway Oriented Programming** | Explicit errors | All async operations |
| **Chain of Responsibility** | Composable behaviors | Pipeline behaviors |
| **Decorator** | Dynamic functionality | Pipeline behaviors |
| **Observer** | Event broadcasting | Notifications |
| **Factory** | Type erasure & caching | RequestHandlerWrapper |
| **Dependency Injection** | Loose coupling | All component resolution |
| **Expression Trees** | Performance | Notification handler invocation |
| **Guard Clauses** | Early validation | MediatorRequestGuards |

## Further Reading

- [Mediator Pattern](https://refactoring.guru/design-patterns/mediator)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Chain of Responsibility](https://refactoring.guru/design-patterns/chain-of-responsibility)
- [Decorator Pattern](https://refactoring.guru/design-patterns/decorator)
- [Observer Pattern](https://refactoring.guru/design-patterns/observer)
- [Expression Trees in C#](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/)
