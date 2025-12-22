# ADR-002: Dependency Injection Strategy

**Status:** Accepted
**Date:** 2025-12-12
**Deciders:** Architecture Team
**Technical Story:** Establish service lifetime and resolution patterns for mediator components

## Context

Encina needs a robust dependency injection strategy that:

- Integrates seamlessly with ASP.NET Core and .NET Generic Host applications
- Supports proper service lifetimes for handlers, behaviors, and processors
- Enables testability and mockability of all components
- Provides clear isolation between concurrent requests
- Minimizes memory allocations and service resolution overhead
- Allows flexible registration patterns (manual, assembly scanning, open generics)

The mediator orchestrates multiple component types with different lifecycle needs:

- **Handlers:** Process individual requests/notifications (may be stateful, database-dependent)
- **Behaviors:** Cross-cutting concerns that wrap handlers (logging, caching, validation)
- **Pre/Post Processors:** Side-effect operations (auditing, metrics, enrichment)
- **Mediator Instance:** Coordinates pipeline execution and manages scopes

## Decision

We adopt **Microsoft.Extensions.DependencyInjection** as the DI container with the following lifetime strategy:

### Service Lifetimes

| Component Type | Lifetime | Rationale |
|---------------|----------|-----------|
| `IMediator` (Encina) | **Singleton** | Stateless coordinator, safe to share across application lifetime |
| `IRequestHandler<,>` | **Transient** (default) or **Scoped** | Allows stateful handlers, fresh instance per request, supports DbContext injection |
| `INotificationHandler<>` | **Transient** (default) or **Scoped** | Same as request handlers - fresh instance per notification |
| `IPipelineBehavior<,>` | **Transient** (default) or **Scoped** | Allows stateful behaviors (e.g., caching), fresh instance per pipeline |
| `IRequestPreProcessor<>` | **Transient** (default) or **Scoped** | Fresh instance per request |
| `IRequestPostProcessor<,>` | **Transient** (default) or **Scoped** | Fresh instance per request |
| `IMediatorMetrics` | **Singleton** | Aggregates metrics across all requests |

**Important:** Handlers, behaviors, and processors are registered as **Transient** by default, but **Scoped** is the recommended lifetime for handlers that depend on scoped services like Entity Framework's `DbContext`.

### Scope Management

```csharp
// Encina creates a fresh scope for each request/notification
using var scope = _scopeFactory.CreateScope();
var serviceProvider = scope.ServiceProvider;
```

**Benefits:**

- Isolates service resolution between concurrent requests
- Enables scoped service injection (DbContext, HttpContext, user context)
- Automatic disposal of scoped services when request completes
- Prevents cross-request contamination of stateful services

### Registration Patterns

#### 1. Manual Registration (Full Control)

```csharp
services.AddMediator();
services.AddScoped<IRequestHandler<GetUserQuery, User>, GetUserHandler>();
services.AddTransient<IPipelineBehavior<GetUserQuery, User>, CachingBehavior<GetUserQuery, User>>();
```

**Use when:** Fine-grained control needed, few handlers, testing scenarios.

#### 2. Assembly Scanning (Convention-Based)

```csharp
services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped; // Default for handlers/behaviors
});
services.RegisterMediatorHandlers(typeof(GetUserHandler).Assembly);
```

**Use when:** Many handlers, following naming conventions, production applications.

#### 3. Open Generic Registration (Cross-Cutting Behaviors)

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

**Use when:** Behavior applies to all or most request types.

### Handler Resolution Strategy

#### Single Request Handler (Exactly One Required)

```csharp
// RequestDispatcher.ExecuteAsync
var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
var handler = serviceProvider.GetService(handlerType);

if (handler is null)
{
    return MediatorErrors.Create(MediatorErrorCodes.RequestHandlerMissing, ...);
}
```

**Behavior:** Returns error if no handler registered (requests require exactly one handler).

#### Multiple Notification Handlers (Zero or More)

```csharp
// NotificationDispatcher.ExecuteAsync
var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
var handlers = serviceProvider.GetServices(handlerType).ToList();

if (handlers.Count == 0)
{
    // This is OK - notifications are fire-and-forget
    return Unit.Default;
}
```

**Behavior:** Resolves all handlers, executes in registration order, zero handlers is valid.

#### Behaviors and Processors (Zero or More)

```csharp
// PipelineBuilder.Build
var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>()?.ToArray()
                ?? Array.Empty<IPipelineBehavior<TRequest, TResponse>>();
```

**Behavior:** Resolves all registered behaviors, falls back to empty array if none found.

## Consequences

### Positive

- **Host Integration:** Works seamlessly with ASP.NET Core, Blazor, Worker Services, and any .NET Generic Host
- **Testability:** Easy to mock handlers and behaviors using standard DI patterns
- **Isolation:** Scoped resolution prevents cross-request contamination
- **Flexibility:** Supports Transient, Scoped, and Singleton lifetimes based on handler needs
- **Standard Patterns:** Follows .NET ecosystem conventions (familiar to all .NET developers)
- **Assembly Scanning:** Automatic handler registration reduces boilerplate
- **Open Generics:** Cross-cutting behaviors work without per-type registration
- **Memory Efficiency:** Singleton mediator minimizes allocations

### Negative

- **Scope Overhead:** Creating a scope per request has minor allocation cost (~80 bytes per scope)
- **DI Container Dependency:** Tightly coupled to Microsoft.Extensions.DependencyInjection
- **Configuration Burden:** Developers must understand lifetime implications (Scoped vs Transient)
- **No Compile-Time Validation:** Missing handler registrations only discovered at runtime

### Neutral

- **Handler Lifetime Choice:** Library provides defaults but allows override (Transient/Scoped choice left to consumer)
- **No Constructor Injection:** Mediator itself uses `IServiceScopeFactory` instead of direct service injection (intentional for scope control)

## Implementation Details

### Encina Constructor

```csharp
public Encina(IServiceScopeFactory scopeFactory, ILogger<Encina> logger)
{
    _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

**Why `IServiceScopeFactory` instead of `IServiceProvider`?**

- Enables explicit scope creation for request isolation
- Prevents accidental singleton service resolution when scoped is intended
- Gives mediator control over service lifetime boundaries

### Registration Extension Method

```csharp
public static IServiceCollection AddMediator(
    this IServiceCollection services,
    Action<EncinaConfiguration>? configure = null)
{
    var config = new EncinaConfiguration();
    configure?.Invoke(config);

    services.TryAddSingleton<IMediator, Encina>();

    // Optional: Register default behaviors
    if (config.RegisterDefaultBehaviors)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    }

    return services;
}
```

### Assembly Scanning Strategy

```csharp
public static IServiceCollection RegisterMediatorHandlers(
    this IServiceCollection services,
    Assembly assembly,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)
{
    // Scan for IRequestHandler<,> implementations
    var handlerTypes = assembly.GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract)
        .SelectMany(type => type.GetInterfaces()
            .Where(i => i.IsGenericType &&
                       i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
            .Select(i => new { Service = i, Implementation = type }));

    foreach (var registration in handlerTypes)
    {
        services.Add(new ServiceDescriptor(
            registration.Service,
            registration.Implementation,
            lifetime));
    }

    // Repeat for INotificationHandler<>, IPipelineBehavior<,>, etc.
    return services;
}
```

## Examples

### Scoped Handler with DbContext

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
{
    private readonly AppDbContext _db;

    public CreateUserHandler(AppDbContext db) // DbContext is Scoped
    {
        _db = db;
    }

    public async Task<User> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var user = new User { Name = request.Name };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }
}

// Registration:
services.AddDbContext<AppDbContext>(); // Scoped by default
services.AddScoped<IRequestHandler<CreateUserCommand, User>, CreateUserHandler>();
```

### Singleton Behavior (Anti-Pattern)

```csharp
// ❌ DON'T DO THIS - Behaviors should be Transient or Scoped
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ✅ CORRECT - Allows stateful behaviors and proper disposal
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

### Testing with Mocks

```csharp
[Fact]
public async Task Send_CallsHandler()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddMediator();

    var mockHandler = new Mock<IRequestHandler<GetUserQuery, User>>();
    mockHandler.Setup(h => h.Handle(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new User { Id = 1, Name = "Test" });

    services.AddScoped<IRequestHandler<GetUserQuery, User>>(_ => mockHandler.Object);

    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    // Act
    var result = await mediator.Send(new GetUserQuery(1));

    // Assert
    result.IsRight.Should().BeTrue();
    mockHandler.Verify(h => h.Handle(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()), Times.Once);
}
```

## Alternatives Considered

### 1. Static Service Locator

```csharp
public class ServiceLocator
{
    public static IServiceProvider Current { get; set; }
}
```

**Rejected:** Anti-pattern, hides dependencies, makes testing difficult, not thread-safe.

### 2. Constructor Injection of IServiceProvider

```csharp
public Encina(IServiceProvider serviceProvider) { }
```

**Rejected:** Prevents proper scope isolation. Would resolve services from root provider (Singleton scope), causing issues with Scoped services like DbContext.

### 3. Support Multiple DI Containers (Autofac, DryIoc, etc.)

**Rejected:** Adds complexity, Microsoft.Extensions.DependencyInjection is the .NET standard, abstractions exist for other containers to adapt.

### 4. Manual Handler Factories

```csharp
public interface IHandlerFactory
{
    IRequestHandler<TRequest, TResponse> Create<TRequest, TResponse>();
}
```

**Rejected:** Reinvents DI container, adds boilerplate, doesn't integrate with .NET ecosystem.

## Related Decisions

- ADR-001: Railway Oriented Programming (errors from DI are converted to Either)
- ADR-003: Caching Strategy (affects handler resolution caching)

## References

- [Microsoft.Extensions.DependencyInjection Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Service Lifetimes](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes)
- [Dependency Injection Best Practices](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)

## Notes

The decision to use `IServiceScopeFactory` instead of `IServiceProvider` is critical for correctness. Early versions of Encina incorrectly used `IServiceProvider` directly, which caused subtle bugs when handlers depended on scoped services. This was fixed before the first public release.

The default lifetime for handlers was changed from Transient to Scoped in version 0.2.0 to better align with ASP.NET Core patterns where DbContext and other scoped services are common.
