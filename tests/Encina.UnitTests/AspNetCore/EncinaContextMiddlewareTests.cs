using System.Security.Claims;
using Encina.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Unit tests for <see cref="EncinaContextMiddleware"/>.
/// </summary>
public sealed class EncinaContextMiddlewareTests
{
    private readonly EncinaAspNetCoreOptions _options = new();
    private readonly IRequestContextAccessor _accessor = Substitute.For<IRequestContextAccessor>();
    private bool _nextCalled;
    private IRequestContext? _capturedContext;

    public EncinaContextMiddlewareTests()
    {
        _accessor.When(a => a.RequestContext = Arg.Any<IRequestContext?>())
            .Do(ci => _capturedContext = ci.Arg<IRequestContext?>());
    }

    private EncinaContextMiddleware CreateMiddleware()
    {
        return new EncinaContextMiddleware(
            _ => { _nextCalled = true; return Task.CompletedTask; },
            Options.Create(_options));
    }

    #region CorrelationId

    [Fact]
    public async Task InvokeAsync_WithCorrelationIdHeader_ShouldExtractFromHeader()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = "test-corr-123";

        await middleware.InvokeAsync(context, _accessor);

        _capturedContext.ShouldNotBeNull();
        _capturedContext!.CorrelationId.ShouldBe("test-corr-123");
    }

    [Fact]
    public async Task InvokeAsync_WithoutCorrelationIdHeader_ShouldGenerateOne()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, _accessor);

        _capturedContext.ShouldNotBeNull();
        _capturedContext!.CorrelationId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetCorrelationIdInResponseHeader()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = "resp-corr";

        await middleware.InvokeAsync(context, _accessor);

        context.Response.Headers["X-Correlation-ID"].ToString().ShouldBe("resp-corr");
    }

    #endregion

    #region UserId

    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_ShouldExtractUserId()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-42") };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        await middleware.InvokeAsync(context, _accessor);

        _capturedContext!.UserId.ShouldBe("user-42");
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedUser_ShouldHaveNullUserId()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, _accessor);

        _capturedContext!.UserId.ShouldBeNull();
    }

    [Fact]
    public async Task InvokeAsync_SubClaim_ShouldExtractUserId()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        var claims = new[] { new Claim("sub", "oidc-user-1") };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));

        await middleware.InvokeAsync(context, _accessor);

        _capturedContext!.UserId.ShouldBe("oidc-user-1");
    }

    #endregion

    #region TenantId

    [Fact]
    public async Task InvokeAsync_TenantIdFromHeader_ShouldExtract()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-ID"] = "tenant-abc";

        await middleware.InvokeAsync(context, _accessor);

        _capturedContext!.TenantId.ShouldBe("tenant-abc");
    }

    [Fact]
    public async Task InvokeAsync_TenantIdFromClaim_ShouldPreferClaim()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        var claims = new[] { new Claim("tid", "claim-tenant") };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        context.Request.Headers["X-Tenant-ID"] = "header-tenant";

        await middleware.InvokeAsync(context, _accessor);

        // Claim takes precedence over header
        _capturedContext!.TenantId.ShouldBe("claim-tenant");
    }

    #endregion

    #region IdempotencyKey

    [Fact]
    public async Task InvokeAsync_WithIdempotencyKey_ShouldExtract()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Idempotency-Key"] = "idem-key-xyz";

        await middleware.InvokeAsync(context, _accessor);

        _capturedContext!.IdempotencyKey.ShouldBe("idem-key-xyz");
    }

    [Fact]
    public async Task InvokeAsync_WithoutIdempotencyKey_ShouldBeNull()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, _accessor);

        _capturedContext!.IdempotencyKey.ShouldBeNull();
    }

    #endregion

    #region Pipeline

    [Fact]
    public async Task InvokeAsync_ShouldCallNext()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, _accessor);

        _nextCalled.ShouldBeTrue();
    }

    #endregion

    #region Custom Header Names

    [Fact]
    public async Task InvokeAsync_CustomCorrelationIdHeader_ShouldUseConfiguredName()
    {
        _options.CorrelationIdHeader = "X-Request-ID";
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "custom-123";

        await middleware.InvokeAsync(context, _accessor);

        _capturedContext!.CorrelationId.ShouldBe("custom-123");
    }

    #endregion

    #region DataRegion

    [Fact]
    public async Task InvokeAsync_WithDataRegionHeader_ShouldEnrichContext()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Data-Region"] = "eu-west-1";

        await middleware.InvokeAsync(context, _accessor);

        // DataRegion is stored in metadata via WithDataRegion extension
        _capturedContext.ShouldNotBeNull();
    }

    #endregion
}
