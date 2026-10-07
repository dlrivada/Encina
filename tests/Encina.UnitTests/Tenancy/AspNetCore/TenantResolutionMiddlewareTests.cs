using Encina.AspNetCore;
using Encina.Tenancy;
using Encina.Tenancy.AspNetCore;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

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
        await middleware.InvokeAsync(context, accessor, TimeProvider.System);

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
        await middleware.InvokeAsync(context, accessor, TimeProvider.System);

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
        await middleware.InvokeAsync(context, accessor, TimeProvider.System);

        // Assert
        _nextCalled.ShouldBeTrue();
    }

    #endregion

    #region Connection Requests (#1705 Phase 3, finding 14)

    private static ITenantResolver ResolverOf(string? tenant)
    {
        var resolver = Substitute.For<ITenantResolver>();
        resolver.Priority.Returns(100);
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<string?>(tenant));
        return resolver;
    }

    private static DefaultHttpContext CreateSseContext()
    {
        var context = CreateHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.Accept = "text/event-stream";
        return context;
    }

    [Fact]
    public async Task InvokeAsync_ConnectionRequest_ResolvesTheTenant_ButWritesNoContext()
    {
        var resolver = ResolverOf("tenant-abc");
        var middleware = CreateMiddleware(resolvers: [resolver]);
        var accessor = Substitute.For<IRequestContextAccessor>();

        await middleware.InvokeAsync(CreateSseContext(), accessor, TimeProvider.System);

        _nextCalled.ShouldBeTrue();
        await resolver.Received(1).ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>());
        accessor.DidNotReceive().RequestContext = Arg.Any<IRequestContext?>();
    }

    [Fact]
    public async Task InvokeAsync_ConnectionRequest_StillValidatesTheTenant_AndAnswers400WhenRequired()
    {
        // The Accept header is client-controlled: it must not bypass validation or RequireTenant.
        var middleware = CreateMiddleware(
            resolvers: [ResolverOf("unknown-tenant")],
            tenancyOptions: new TenancyOptions { RequireTenant = true, ValidateTenantOnRequest = true },
            aspNetCoreOptions: new TenancyAspNetCoreOptions { Return400WhenTenantRequired = true });
        var context = CreateSseContext();

        await middleware.InvokeAsync(context, Substitute.For<IRequestContextAccessor>(), TimeProvider.System);

        context.Response.StatusCode.ShouldBe(400);
        _nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_PostThatStreams_IsNotAConnectionRequest_AndGetsTheTenantWritten()
    {
        var middleware = CreateMiddleware(resolvers: [ResolverOf("tenant-abc")]);
        var context = CreateHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers.Accept = "text/event-stream";
        IRequestContext? written = null;
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns((IRequestContext?)null);
        accessor.When(a => a.RequestContext = Arg.Any<IRequestContext?>()).Do(call => written = call.Arg<IRequestContext?>());

        await middleware.InvokeAsync(context, accessor, TimeProvider.System);

        written.ShouldNotBeNull();
        written.TenantId.ShouldBe("tenant-abc");
    }

    [Fact]
    public async Task InvokeAsync_InsideAnInboundScope_TheTenantIsAddedToTheRequestIdentityContext()
    {
        // UseEncinaContext then UseTenantResolution: the setter keeps the identity and origin.
        var host = new global::Encina.UnitTests.Core.Identity.ScopeTestHost();
        IRequestContext? seen = null;
        var inner = new TenantResolutionMiddleware(
            _ =>
            {
                seen = host.Accessor.RequestContext;
                return Task.CompletedTask;
            },
            [ResolverOf("tenant-abc")],
            Options.Create(new TenancyOptions()),
            Options.Create(new TenancyAspNetCoreOptions()),
            _tenantStore);

        await host.InInboundScope(global::Encina.Testing.Identity.TestIdentity.Principal("alice"), async _ =>
        {
            await inner.InvokeAsync(CreateHttpContext(), host.Accessor, TimeProvider.System);
            return 0;
        });

        seen.ShouldNotBeNull();
        global::Encina.UnitTests.Core.Identity.IssuedIdentityReads.Issued(seen).UserId.ShouldBe("alice");
        seen.TenantId.ShouldBe("tenant-abc");
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
        await middleware.InvokeAsync(context, accessor, TimeProvider.System);

        // Assert
        _nextCalled.ShouldBeTrue();
        requestContext.Received(1).WithTenantId("tenant-abc");
    }

    [Fact]
    public async Task InvokeAsync_TenantResolved_WithoutAContext_StampsTheNewContextFromTheInjectedTimeProvider()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 10, 5, 9, 30, 0, TimeSpan.Zero);
        var (middleware, accessor, captured) = ArrangeTenantResolvedWithoutContext();
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context, accessor, new FakeTimeProvider(now));

        // Assert
        captured().ShouldNotBeNull();
        captured()!.Timestamp.ShouldBe(now);
        captured()!.TenantId.ShouldBe("tenant-abc");
    }

    [Fact]
    public async Task InvokeAsync_NullArguments_Throw()
    {
        var middleware = CreateMiddleware();
        var accessor = Substitute.For<IRequestContextAccessor>();

        await Should.ThrowAsync<ArgumentNullException>(() => middleware.InvokeAsync(null!, accessor, TimeProvider.System));
        await Should.ThrowAsync<ArgumentNullException>(() => middleware.InvokeAsync(CreateHttpContext(), null!, TimeProvider.System));
        await Should.ThrowAsync<ArgumentNullException>(() => middleware.InvokeAsync(CreateHttpContext(), accessor, null!));
    }

    [Fact]
    public void AddEncinaTenancyAspNetCore_RegistersTheClock_ForTheMiddleware()
    {
        var services = new ServiceCollection();

        services.AddEncinaTenancyAspNetCore();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
    }

    private (TenantResolutionMiddleware Middleware, IRequestContextAccessor Accessor, Func<IRequestContext?> Captured) ArrangeTenantResolvedWithoutContext()
    {
        var resolver = Substitute.For<ITenantResolver>();
        resolver.Priority.Returns(100);
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string?>("tenant-abc"));
        _tenantStore.AddTenant("tenant-abc");

        IRequestContext? set = null;
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns((IRequestContext?)null);
        accessor.When(a => a.RequestContext = Arg.Any<IRequestContext?>()).Do(ci => set = ci.Arg<IRequestContext?>());

        return (CreateMiddleware(resolvers: [resolver]), accessor, () => set);
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
        await middleware.InvokeAsync(context, accessor, TimeProvider.System);

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
        await middleware.InvokeAsync(context, accessor, TimeProvider.System);

        // Assert
        _nextCalled.ShouldBeTrue();
        requestContext.Received(1).WithTenantId("valid-tenant");
    }

    #endregion

    #region No Ambient Context (#1147 review)

    [Fact]
    public async Task InvokeAsync_NoAmbientContext_CreatesOneWithTheTenantAndTheRequestCorrelationId()
    {
        // Arrange - UseEncinaContext() did not run, so the accessor is empty.
        var resolver = Substitute.For<ITenantResolver>();
        resolver.Priority.Returns(100);
        resolver.ResolveAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string?>("tenant-x"));

        var accessor = new RequestContextAccessor();
        accessor.RequestContext.ShouldBeNull();
        IRequestContext? seenByNext = null;
        var middleware = new TenantResolutionMiddleware(
            _ =>
            {
                seenByNext = accessor.RequestContext;
                return Task.CompletedTask;
            },
            [resolver],
            Options.Create(new TenancyOptions()),
            Options.Create(new TenancyAspNetCoreOptions()),
            _tenantStore);
        var context = CreateHttpContext();
        context.TraceIdentifier = "trace-42";

        // Act
        await middleware.InvokeAsync(context, accessor, TimeProvider.System);

        // Assert - the tenant reaches the rest of the pipeline instead of being dropped.
        seenByNext.ShouldNotBeNull();
        seenByNext.TenantId.ShouldBe("tenant-x");
        seenByNext.CorrelationId.ShouldBe(System.Diagnostics.Activity.Current?.Id ?? "trace-42");
        seenByNext.UserId.ShouldBeNull();
        seenByNext.IdempotencyKey.ShouldBeNull();
    }

    [Fact]
    public async Task InvokeAsync_NoAmbientContext_NoTenantResolved_LeavesTheAccessorEmpty()
    {
        // Arrange
        var accessor = new RequestContextAccessor();
        accessor.RequestContext.ShouldBeNull();
        IRequestContext? seenByNext = null;
        var middleware = new TenantResolutionMiddleware(
            _ =>
            {
                seenByNext = accessor.RequestContext;
                return Task.CompletedTask;
            },
            [],
            Options.Create(new TenancyOptions()),
            Options.Create(new TenancyAspNetCoreOptions()),
            _tenantStore);

        // Act
        await middleware.InvokeAsync(CreateHttpContext(), accessor, TimeProvider.System);

        // Assert
        seenByNext.ShouldBeNull();
    }

    #endregion
}
