using System.Diagnostics;
using System.Security.Claims;
using Shouldly;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Encina.AspNetCore.Tests;

public class EncinaContextMiddlewareTests
{
    [Fact]
    public async Task Middleware_ExtractsCorrelationId_FromHeader()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
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
        request.Headers.Add("X-Correlation-ID", correlationId);
        await client.SendAsync(request);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext!.CorrelationId.ShouldBe(correlationId);
    }

    [Fact]
    public async Task Middleware_GeneratesCorrelationId_WhenNotProvided()
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
        capturedContext!.CorrelationId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Middleware_ExtractsUserId_FromAuthenticatedUser()
    {
        // Arrange
        var userId = "user-123";
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
                services.AddAuthentication("Test")
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test", options => { });
            },
            configureApp: app =>
            {
                app.UseAuthentication();
            });

        var client = host.GetTestClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Test-User", userId);
        await client.SendAsync(request);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext!.UserId.ShouldNotBeNull();
        capturedContext.UserId.ShouldBe(userId);
    }

    [Fact]
    public async Task Middleware_ExtractsTenantId_FromClaims()
    {
        // Arrange
        var tenantId = "tenant-456";
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
                services.AddAuthentication("Test")
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test", options => { });
            },
            configureApp: app =>
            {
                app.UseAuthentication();
            });

        var client = host.GetTestClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Test-User", "user-123");
        request.Headers.Add("X-Test-Tenant", tenantId);
        await client.SendAsync(request);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext!.TenantId.ShouldNotBeNull();
        capturedContext.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task Middleware_ExtractsTenantId_FromHeader_WhenNotInClaims()
    {
        // Arrange
        var tenantId = "tenant-789";
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
        request.Headers.Add("X-Tenant-ID", tenantId);
        await client.SendAsync(request);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext!.TenantId.ShouldNotBeNull();
        capturedContext.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task Middleware_ExtractsIdempotencyKey_FromHeader()
    {
        // Arrange
        var idempotencyKey = "idem-key-123";
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
        request.Headers.Add("X-Idempotency-Key", idempotencyKey);
        await client.SendAsync(request);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext!.IdempotencyKey.ShouldNotBeNull();
        capturedContext.IdempotencyKey.ShouldBe(idempotencyKey);
    }

    [Fact]
    public async Task Middleware_SetsCorrelationIdInResponse()
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
    public async Task Middleware_UsesActivityCurrentId_WhenAvailable()
    {
        // Arrange
        IRequestContext? capturedContext = null;
        var activity = new Activity("test-operation");
        activity.Start();

        try
        {
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
            capturedContext!.CorrelationId.ShouldNotBeNullOrEmpty();
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public async Task Middleware_CustomOptions_UsesCustomHeaders()
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
                    options.TenantIdHeader = "X-Customer-ID";
                    options.IdempotencyKeyHeader = "Idempotency-Key";
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
    }

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
}

/// <summary>
/// Test authentication handler that creates a ClaimsPrincipal from request headers.
/// </summary>
public class TestAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var userId))
        {
            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (Request.Headers.TryGetValue("X-Test-Tenant", out var tenantId))
        {
            claims.Add(new Claim("tenant_id", tenantId.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");

        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}
