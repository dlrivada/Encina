# AspNetCore Testing Patterns

This document describes the testing patterns used in `Encina.AspNetCore.Tests` to help developers write consistent and effective tests.

## Test Infrastructure

### HostBuilder vs WebApplicationFactory

**When to use HostBuilder with TestServer:**

- Middleware testing (direct control over pipeline order)
- Testing specific middleware behavior in isolation
- Custom authentication/authorization scenarios
- Testing context propagation

**When to use WebApplicationFactory:**

- Full application integration tests
- Testing endpoints with controllers
- Testing with real routing
- E2E scenarios requiring the full ASP.NET Core pipeline

### Current Pattern: CreateTestHost Helper

```csharp
private static async Task<IHost> CreateTestHost(
    RequestDelegate endpoint,
    Action<IServiceCollection>? configureServices = null,
    Action<IApplicationBuilder>? configureApp = null)
{
    var hostBuilder = new HostBuilder()
        .ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                services.AddEncinaAspNetCore();
                configureServices?.Invoke(services);
            });
            webHost.Configure(app =>
            {
                configureApp?.Invoke(app);
                app.UseEncinaContext();
                app.Run(endpoint);
            });
        });

    var host = await hostBuilder.StartAsync();
    return host;
}
```

**Benefits:**

- Explicit middleware ordering control
- Easy to inject custom services
- Clear test isolation
- Fast execution (no full app startup)

## Test Categories

### 1. Middleware Behavior Tests

Test individual middleware behavior and configuration.

```csharp
[Fact]
public async Task Middleware_ExtractsCorrelationId_FromHeader()
{
    IRequestContext? capturedContext = null;

    using var host = await CreateTestHost(ctx =>
    {
        var accessor = ctx.RequestServices.GetRequiredService<IRequestContextAccessor>();
        capturedContext = accessor.RequestContext;
        return Task.CompletedTask;
    });

    var client = host.GetTestClient();
    var request = new HttpRequestMessage(HttpMethod.Get, "/");
    request.Headers.Add("X-Correlation-ID", "test-id");
    await client.SendAsync(request);

    capturedContext.ShouldNotBeNull();
    capturedContext!.CorrelationId.ShouldBe("test-id");
}
```

### 2. Context Propagation Tests

Verify context flows correctly across async boundaries.

```csharp
[Fact]
public async Task Middleware_ContextPropagates_AcrossAsyncBoundaries()
{
    using var host = await CreateTestHost(async ctx =>
    {
        var accessor = ctx.RequestServices.GetRequiredService<IRequestContextAccessor>();
        var initial = accessor.RequestContext?.CorrelationId;

        await Task.Delay(10);
        var afterDelay = accessor.RequestContext?.CorrelationId;

        // Both should be the same
        initial.ShouldBe(afterDelay);
    });
    // ...
}
```

### 3. Pipeline Composition Tests

Test multiple middleware working together.

```csharp
[Fact]
public async Task Middleware_PipelineComposition_MultipleMiddlewareAccessSameContext()
{
    var correlationIds = new List<string?>();

    var hostBuilder = new HostBuilder()
        .ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services => services.AddEncinaAspNetCore());
            webHost.Configure(app =>
            {
                app.UseEncinaContext(); // FIRST
                app.Use(async (ctx, next) =>
                {
                    // Access context in middleware 1
                    await next();
                });
                app.Use(async (ctx, next) =>
                {
                    // Access context in middleware 2
                    await next();
                });
                app.Run(ctx => /* endpoint */);
            });
        });
    // ...
}
```

### 4. Concurrent Request Tests

Verify context isolation between parallel requests.

```csharp
[Fact]
public async Task Middleware_ConcurrentRequests_HaveIsolatedContexts()
{
    var capturedContexts = new ConcurrentDictionary<string, string?>();

    // Send 10 concurrent requests with different correlation IDs
    var tasks = Enumerable.Range(0, 10).Select(async i =>
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-ID", $"request-{i}");
        await client.SendAsync(request);
    });

    await Task.WhenAll(tasks);

    // Each request should maintain its own context
    foreach (var kvp in capturedContexts)
    {
        kvp.Value.ShouldBe(kvp.Key);
    }
}
```

## Assertion Patterns

### Either Assertions (Encina.TestInfrastructure)

```csharp
// Success assertions
result.ShouldBeSuccess();
result.ShouldBeSuccess(expectedValue);
result.ShouldBeSuccess(value => value.Name.ShouldBe("test"));

// Error assertions
result.ShouldBeError();
result.ShouldBeErrorWithCode("validation.invalid_input");
result.ShouldBeErrorContaining("not found");
```

### Standard Assertions (Shouldly)

```csharp
// Value assertions
value.ShouldBe(expected);
value.ShouldNotBeNull();
value.ShouldBeEmpty();

// Collection assertions
list.ShouldContain(item);
list.All(x => x.IsValid).ShouldBeTrue();

// Exception assertions
Should.Throw<ArgumentException>(() => action());
```

## Test Authentication Handler

For tests requiring authenticated users:

```csharp
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        // Add tenant if provided
        if (Request.Headers.TryGetValue("X-Test-Tenant", out var tenantId))
            claims.Add(new Claim("tenant_id", tenantId.ToString()));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

Usage:

```csharp
using var host = await CreateTestHost(
    endpoint,
    configureServices: services =>
    {
        services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
    },
    configureApp: app =>
    {
        app.UseAuthentication();
    });

var request = new HttpRequestMessage(HttpMethod.Get, "/");
request.Headers.Add("X-Test-User", "user-123");
request.Headers.Add("X-Test-Tenant", "tenant-456");
```

## Best Practices

1. **Use `using` with hosts** - Always dispose hosts to release resources
2. **Explicit middleware ordering** - When order matters, use custom HostBuilder
3. **Capture state in lambdas** - Use closures to capture context for assertions
4. **Test isolation** - Each test should create its own host instance
5. **Async all the way** - Use async/await consistently
6. **Clear assertions** - Use descriptive assertion messages
7. **Test performance** - Tests building a full `ServiceProvider` or using `TestServer` are integration tests and can be slow. Tag them with `[Trait("Category", "Integration")]` and consider maintaining a separate fast unit test suite. For example, `SignalRBroadcasterPropertyTests.cs` builds a full `ServiceProvider` per test and should be tagged as integration to align with the "<1ms per test" guideline for unit tests.

## Migration Guide

When adding new middleware tests:

1. Determine if you need full pipeline or isolated middleware testing
2. Use `CreateTestHost` for most cases
3. Add custom middleware in `configureApp` if testing interactions
4. Use `TestAuthHandler` if authentication is needed
5. Capture context in lambdas before asserting
6. Test both success and error scenarios
