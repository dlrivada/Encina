using Encina.AspNetCore;
using Encina.Tenancy;
using Encina.Tenancy.AspNetCore;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Tenancy.AspNetCore;

/// <summary>
/// Unit tests for <see cref="TenantResolutionMiddleware"/>.
/// </summary>
public sealed class TenantResolutionMiddlewareTests
{
    private bool _nextCalled;

    /// <summary>
    /// Simple fake tenant store to avoid NSubstitute issues with default interface methods.
    /// </summary>
    private sealed class FakeTenantStore : ITenantStore
    {
        private readonly Dictionary<string, TenantInfo> _tenants = new(StringComparer.Ordinal);

        public void AddTenant(string tenantId, string name = "Test Tenant")
        {
            _tenants[tenantId] = new TenantInfo(tenantId, name, TenantIsolationStrategy.SharedSchema);
        }

        public ValueTask<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            _tenants.TryGetValue(tenantId, out var tenant);
            return new ValueTask<TenantInfo?>(tenant);
        }

        public ValueTask<IReadOnlyList<TenantInfo>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<IReadOnlyList<TenantInfo>>(_tenants.Values.ToList());
        }
    }

    private readonly FakeTenantStore _tenantStore = new();

    private TenantResolutionMiddleware CreateMiddleware(
        IEnumerable<ITenantResolver>? resolvers = null,
        TenancyOptions? tenancyOptions = null,
        TenancyAspNetCoreOptions? aspNetCoreOptions = null)
    {
        resolvers ??= Enumerable.Empty<ITenantResolver>();
        tenancyOptions ??= new TenancyOptions();
        aspNetCoreOptions ??= new TenancyAspNetCoreOptions();

        return new TenantResolutionMiddleware(
            _ =>
            {
                _nextCalled = true;
                return Task.CompletedTask;
            },
            resolvers,
            Options.Create(tenancyOptions),
            Options.Create(aspNetCoreOptions),
            _tenantStore);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    #region Next Delegate

    [Fact]
    public async Task InvokeAsync_NoResolver_ShouldCallNext()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = CreateHttpContext();
        var accessor = Substitute.For<IRequestContextAccessor>();

        // Act
        await middleware.InvokeAsync(context, accessor);

        // Assert
        _nextCalled.ShouldBeTrue();
    }

    #endregion

    #region Tenant Required - 400 Response

    [Fact]
    public async Task InvokeAsync_TenantRequired_NoTenantResolved_ShouldReturn400()
    {
        // Arrange
        var tenancyOptions = new TenancyOptions { RequireTenant = true };
        var aspNetCoreOptions = new TenancyAspNetCoreOptions { Return400WhenTenantRequired = true };
        var middleware = CreateMiddleware(tenancyOptions: tenancyOptions, aspNetCoreOptions: aspNetCoreOptions);
        var context = CreateHttpContext();
        var accessor = Substitute.For<IRequestContextAccessor>();

        // Act
        await middleware.InvokeAsync(context, accessor);

        // Assert
        context.Response.StatusCode.ShouldBe(400);
        _nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_TenantRequired_Return400Disabled_ShouldCallNext()
    {
        // Arrange
        var tenancyOptions = new TenancyOptions { RequireTenant = true };
        var aspNetCoreOptions = new TenancyAspNetCoreOptions { Return400WhenTenantRequired = false };
        var middleware = CreateMiddleware(tenancyOptions: tenancyOptions, aspNetCoreOptions: aspNetCoreOptions);
        var context = CreateHttpContext();
        var accessor = Substitute.For<IRequestContextAccessor>();

        // Act
        await middleware.InvokeAsync(context, accessor);

        // Assert
        _nextCalled.ShouldBeTrue();
    }

    #endregion

    #region Tenant Resolved - Context Updated

    [Fact]
    public async Task InvokeAsync_TenantResolved_ShouldUpdateRequestContext()
    {
        // Arrange
        var resolver = Substitute.For<ITenantResolver>();
        resolver.Priority.Returns(100);
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string?>("tenant-abc"));

        var middleware = CreateMiddleware(resolvers: [resolver]);
        var context = CreateHttpContext();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.WithTenantId(Arg.Any<string?>()).Returns(requestContext);
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(requestContext);

        // Act
        await middleware.InvokeAsync(context, accessor);

        // Assert
        _nextCalled.ShouldBeTrue();
        requestContext.Received(1).WithTenantId("tenant-abc");
    }

    #endregion

    #region Tenant Validation

    [Fact]
    public async Task InvokeAsync_ValidateTenant_TenantNotFound_ShouldTreatAsNoTenant()
    {
        // Arrange
        var resolver = Substitute.For<ITenantResolver>();
        resolver.Priority.Returns(100);
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string?>("unknown-tenant"));

        // FakeTenantStore has no tenants, so ExistsAsync returns false for "unknown-tenant"

        var tenancyOptions = new TenancyOptions
        {
            RequireTenant = true,
            ValidateTenantOnRequest = true
        };
        var aspNetCoreOptions = new TenancyAspNetCoreOptions { Return400WhenTenantRequired = true };

        var middleware = CreateMiddleware(
            resolvers: [resolver],
            tenancyOptions: tenancyOptions,
            aspNetCoreOptions: aspNetCoreOptions);

        var context = CreateHttpContext();
        var accessor = Substitute.For<IRequestContextAccessor>();

        // Act
        await middleware.InvokeAsync(context, accessor);

        // Assert: tenant was validated and not found, so 400 is returned
        context.Response.StatusCode.ShouldBe(400);
        _nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_ValidateTenant_TenantExists_ShouldProceedNormally()
    {
        // Arrange
        var resolver = Substitute.For<ITenantResolver>();
        resolver.Priority.Returns(100);
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string?>("valid-tenant"));

        // Add tenant to fake store so ExistsAsync returns true
        _tenantStore.AddTenant("valid-tenant");

        var tenancyOptions = new TenancyOptions { ValidateTenantOnRequest = true };
        var middleware = CreateMiddleware(
            resolvers: [resolver],
            tenancyOptions: tenancyOptions);

        var context = CreateHttpContext();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.WithTenantId(Arg.Any<string?>()).Returns(requestContext);
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(requestContext);

        // Act
        await middleware.InvokeAsync(context, accessor);

        // Assert
        _nextCalled.ShouldBeTrue();
        requestContext.Received(1).WithTenantId("valid-tenant");
    }

    #endregion

    #region Null Context Accessor

    [Fact]
    public async Task InvokeAsync_NullRequestContext_ShouldNotThrow()
    {
        // Arrange
        var resolver = Substitute.For<ITenantResolver>();
        resolver.Priority.Returns(100);
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string?>("tenant-x"));

        var middleware = CreateMiddleware(resolvers: [resolver]);
        var context = CreateHttpContext();
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns((IRequestContext?)null);

        // Act
        await middleware.InvokeAsync(context, accessor);

        // Assert: should not throw, just proceed
        _nextCalled.ShouldBeTrue();
    }

    #endregion
}
