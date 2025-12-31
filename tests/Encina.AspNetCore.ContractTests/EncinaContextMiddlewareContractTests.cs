using Microsoft.AspNetCore.TestHost;
using Shouldly;

namespace Encina.AspNetCore.ContractTests;

/// <summary>
/// Contract tests for <see cref="EncinaContextMiddleware"/>.
/// Verifies that the middleware correctly initializes and manages request context.
/// </summary>
[Trait("Category", "Contract")]
public sealed class EncinaContextMiddlewareContractTests
{
    [Fact]
    public async Task Contract_MustSetRequestContextInAccessor()
    {
        // Arrange
        IRequestContext? capturedContext = null;

        using var host = await CreateTestHost(ctx =>
        {
            var accessor = ctx.RequestServices.GetRequiredService<IRequestContextAccessor>();
            capturedContext = accessor.RequestContext;
            return Task.CompletedTask;
        });

        var client = host.GetTestClient();

        // Act
        await client.GetAsync("/");

        // Assert
        capturedContext.ShouldNotBeNull();
    }

    [Fact]
    public async Task Contract_MustGenerateCorrelationIdWhenNotProvided()
    {
        // Arrange
        IRequestContext? capturedContext = null;

        using var host = await CreateTestHost(ctx =>
        {
            var accessor = ctx.RequestServices.GetRequiredService<IRequestContextAccessor>();
            capturedContext = accessor.RequestContext;
            return Task.CompletedTask;
        });

        var client = host.GetTestClient();

        // Act
        await client.GetAsync("/");

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext!.CorrelationId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Contract_MustUseProvidedCorrelationId()
    {
        // Arrange
        var expectedCorrelationId = Guid.NewGuid().ToString();
        IRequestContext? capturedContext = null;

        using var host = await CreateTestHost(ctx =>
        {
            var accessor = ctx.RequestServices.GetRequiredService<IRequestContextAccessor>();
            capturedContext = accessor.RequestContext;
            return Task.CompletedTask;
        });

        var client = host.GetTestClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-ID", expectedCorrelationId);
        await client.SendAsync(request);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext!.CorrelationId.ShouldBe(expectedCorrelationId);
    }

    [Fact]
    public async Task Contract_MustSetCorrelationIdInResponseHeader()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        using var host = await CreateTestHost(_ => Task.CompletedTask);
        var client = host.GetTestClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-ID", correlationId);
        var response = await client.SendAsync(request);

        // Assert
        response.Headers.Contains("X-Correlation-ID").ShouldBeTrue();
        response.Headers.GetValues("X-Correlation-ID").ShouldContain(correlationId);
    }

    [Fact]
    public async Task Contract_MustExtractTenantIdFromHeader()
    {
        // Arrange
        var expectedTenantId = "tenant-123";
        IRequestContext? capturedContext = null;

        using var host = await CreateTestHost(ctx =>
        {
            var accessor = ctx.RequestServices.GetRequiredService<IRequestContextAccessor>();
            capturedContext = accessor.RequestContext;
            return Task.CompletedTask;
        });

        var client = host.GetTestClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Tenant-ID", expectedTenantId);
        await client.SendAsync(request);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext!.TenantId.ShouldNotBeNull();
        capturedContext.TenantId.ShouldBe(expectedTenantId);
    }

    [Fact]
    public async Task Contract_MustExtractIdempotencyKeyFromHeader()
    {
        // Arrange
        var expectedKey = "idem-key-456";
        IRequestContext? capturedContext = null;

        using var host = await CreateTestHost(ctx =>
        {
            var accessor = ctx.RequestServices.GetRequiredService<IRequestContextAccessor>();
            capturedContext = accessor.RequestContext;
            return Task.CompletedTask;
        });

        var client = host.GetTestClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Idempotency-Key", expectedKey);
        await client.SendAsync(request);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext!.IdempotencyKey.ShouldNotBeNull();
        capturedContext.IdempotencyKey.ShouldBe(expectedKey);
    }

    [Fact]
    public async Task Contract_MustCallNextMiddleware()
    {
        // Arrange
        var nextCalled = false;

        using var host = await CreateTestHost(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var client = host.GetTestClient();

        // Act
        await client.GetAsync("/");

        // Assert
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_MustNotFailWhenNoHeadersProvided()
    {
        // Arrange
        using var host = await CreateTestHost(_ => Task.CompletedTask);
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_MustRespectCustomOptions()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        IRequestContext? capturedContext = null;

        using var host = await CreateTestHost(
            ctx =>
            {
                var accessor = ctx.RequestServices.GetRequiredService<IRequestContextAccessor>();
                capturedContext = accessor.RequestContext;
                return Task.CompletedTask;
            },
            configureServices: services =>
            {
                services.AddEncinaAspNetCore(options =>
                {
                    options.CorrelationIdHeader = "X-Request-ID";
                });
            });

        var client = host.GetTestClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Request-ID", correlationId);
        var response = await client.SendAsync(request);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext!.CorrelationId.ShouldBe(correlationId);
        response.Headers.Contains("X-Request-ID").ShouldBeTrue();
        response.Headers.GetValues("X-Request-ID").ShouldContain(correlationId);
    }

    [Fact]
    public async Task Contract_MultipleRequestsMustHaveIsolatedContexts()
    {
        // Arrange
        var correlation1 = "req-1";
        var correlation2 = "req-2";
        string? captured1 = null;
        string? captured2 = null;

        using var host = await CreateTestHost(ctx =>
        {
            var accessor = ctx.RequestServices.GetRequiredService<IRequestContextAccessor>();
            var correlationId = accessor.RequestContext?.CorrelationId;

            if (correlationId == correlation1) captured1 = correlationId;
            if (correlationId == correlation2) captured2 = correlationId;

            return Task.CompletedTask;
        });

        var client = host.GetTestClient();

        // Act
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/");
        request1.Headers.Add("X-Correlation-ID", correlation1);

        var request2 = new HttpRequestMessage(HttpMethod.Get, "/");
        request2.Headers.Add("X-Correlation-ID", correlation2);

        await client.SendAsync(request1);
        await client.SendAsync(request2);

        // Assert
        captured1.ShouldBe(correlation1);
        captured2.ShouldBe(correlation2);
    }

    // Helper method
    private static async Task<IHost> CreateTestHost(
        RequestDelegate endpoint,
        Action<IServiceCollection>? configureServices = null)
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
                    app.UseEncinaContext();
                    app.Run(endpoint);
                });
            });

        var host = await hostBuilder.StartAsync();
        return host;
    }
}
