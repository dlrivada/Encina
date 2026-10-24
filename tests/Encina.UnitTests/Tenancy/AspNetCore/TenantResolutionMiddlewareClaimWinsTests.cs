using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Tenancy.AspNetCore;

/// <summary>
/// The verified tenant claim always wins (#2399): real resolvers, real chain, real middleware.
/// </summary>
public sealed class TenantResolutionMiddlewareClaimWinsTests
{
    private const string TrustedPeer = "10.0.0.7";
    private const string UntrustedPeer = "203.0.113.9";

    private readonly TenancyAspNetCoreOptions _aspNetCoreOptions = new();
    private readonly TenancyOptions _tenancyOptions = new();
    private readonly FakeLogger<TenantResolutionMiddleware> _middlewareLogger = new();
    private readonly FakeLogger<HeaderTenantResolver> _headerLogger = new();
    private readonly InMemoryTenantStore _store = new([]);
    private readonly RequestContextAccessor _accessor = new();
    private bool _nextCalled;
    private IRequestContext? _seen; // the context the next middleware sees (AsyncLocal does not flow back to the test)

    public TenantResolutionMiddlewareClaimWinsTests()
    {
        _aspNetCoreOptions.HeaderResolver.Enabled = true;
        _aspNetCoreOptions.HeaderResolver.TrustedProxies.Add(IPNetwork.Parse("10.0.0.0/24"));
    }

    [Fact]
    public async Task ClaimAndDifferingTrustedHeader_Answers403_AndDoesNotCallNext()
    {
        var context = Request(claim: "tenant-a", header: "tenant-b", peer: TrustedPeer);

        await Create().InvokeAsync(context, _accessor, TimeProvider.System);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        context.Response.ContentType.ShouldBe("application/problem+json");
        _nextCalled.ShouldBeFalse();
        _seen.ShouldBeNull();
        var body = await BodyOf(context);
        body.ShouldContain("Tenant mismatch");
        body.ShouldNotContain("tenant-a");
        body.ShouldNotContain("tenant-b");
    }

    [Fact]
    public async Task Mismatch_LogsWarningWithResolverNamesOnly()
    {
        var context = Request(claim: "tenant-a", header: "tenant-b", peer: TrustedPeer);

        await Create().InvokeAsync(context, _accessor, TimeProvider.System);

        var record = _middlewareLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Id.Id.ShouldBe(3851);
        record.Message.ShouldContain(nameof(ClaimTenantResolver));
        record.Message.ShouldContain(nameof(HeaderTenantResolver));
        record.Message.ShouldNotContain("tenant-a");
        record.Message.ShouldNotContain("tenant-b");
    }

    [Fact]
    public async Task ClaimAndSameHeader_ResolvesTheTenant()
    {
        var context = Request(claim: "tenant-a", header: "tenant-a", peer: TrustedPeer);

        await Create().InvokeAsync(context, _accessor, TimeProvider.System);

        _nextCalled.ShouldBeTrue();
        _seen!.TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task ClaimWithoutHeader_ResolvesTheClaim()
    {
        var context = Request(claim: "tenant-a", header: null, peer: UntrustedPeer);

        await Create().InvokeAsync(context, _accessor, TimeProvider.System);

        _nextCalled.ShouldBeTrue();
        _seen!.TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task ClaimAndDifferingHeaderFromUntrustedPeer_ClaimWinsWithoutMismatch()
    {
        // The header is ignored (untrusted peer), so nothing disagrees with the claim.
        var context = Request(claim: "tenant-a", header: "tenant-b", peer: UntrustedPeer);

        await Create().InvokeAsync(context, _accessor, TimeProvider.System);

        _nextCalled.ShouldBeTrue();
        _seen!.TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task NoClaimAndHeaderFromTrustedProxy_ResolvesTheHeader()
    {
        var context = Request(claim: null, header: "tenant-b", peer: TrustedPeer);

        await Create().InvokeAsync(context, _accessor, TimeProvider.System);

        _nextCalled.ShouldBeTrue();
        _seen!.TenantId.ShouldBe("tenant-b");
    }

    [Fact]
    public async Task NoClaimAndHeaderFromUntrustedPeer_Answers400WithTheDefaultRequireTenant()
    {
        var context = Request(claim: null, header: "tenant-b", peer: UntrustedPeer);

        await Create().InvokeAsync(context, _accessor, TimeProvider.System);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        _nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task ClaimAndDifferingRouteValue_Answers403()
    {
        _aspNetCoreOptions.RouteResolver.Enabled = true;
        var context = Request(claim: "tenant-a", header: null, peer: UntrustedPeer);
        context.Request.RouteValues["tenantId"] = "tenant-b";

        await Create().InvokeAsync(context, _accessor, TimeProvider.System);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        _nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task ClaimAndDifferingSubdomain_Answers403()
    {
        _aspNetCoreOptions.SubdomainResolver.Enabled = true;
        _aspNetCoreOptions.SubdomainResolver.BaseDomain = "example.com";
        var context = Request(claim: "tenant-a", header: null, peer: UntrustedPeer);
        context.Request.Host = new HostString("tenant-b.example.com");

        await Create().InvokeAsync(context, _accessor, TimeProvider.System);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        _nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Mismatch_IsCheckedBeforeTenantStoreValidation()
    {
        _tenancyOptions.ValidateTenantOnRequest = true; // the store is empty: "tenant-a" is unknown
        var context = Request(claim: "tenant-a", header: "tenant-b", peer: TrustedPeer);

        await Create().InvokeAsync(context, _accessor, TimeProvider.System);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task HeaderPriorityLowerThanClaim_StillCannotOverrideTheClaim()
    {
        // The old chain let a lower header priority put the header before the claim.
        _aspNetCoreOptions.HeaderResolver.Priority = 1;
        _aspNetCoreOptions.ClaimResolver.Priority = 500;
        var context = Request(claim: "tenant-a", header: "tenant-b", peer: TrustedPeer);

        await Create().InvokeAsync(context, _accessor, TimeProvider.System);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void TrustAnyPeerWithHeaderEnabled_LogsAWarningAtConstruction()
    {
        _aspNetCoreOptions.HeaderResolver.TrustAnyPeer = true;

        Create();

        var record = _middlewareLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Id.Id.ShouldBe(3852);
    }

    [Fact]
    public void RequireTenantDisabled_LogsAWarningAtConstruction()
    {
        _tenancyOptions.RequireTenant = false;

        Create();

        var record = _middlewareLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Id.Id.ShouldBe(3853);
    }

    [Fact]
    public void TrustAnyPeerWithHeaderDisabled_LogsNothing()
    {
        _aspNetCoreOptions.HeaderResolver.Enabled = false;
        _aspNetCoreOptions.HeaderResolver.TrustAnyPeer = true;

        Create();

        _middlewareLogger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public void Create_WithoutTrustAnyPeer_LogsNothing()
    {
        Create();

        _middlewareLogger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public void IsAuthoritative_OnlyTheClaimResolverIsAuthoritative()
    {
        var options = Options.Create(_aspNetCoreOptions);

        new ClaimTenantResolver(options).IsAuthoritative.ShouldBeTrue();
        new HeaderTenantResolver(options, _headerLogger).IsAuthoritative.ShouldBeFalse();
        new RouteTenantResolver(options).IsAuthoritative.ShouldBeFalse();
        new SubdomainTenantResolver(options).IsAuthoritative.ShouldBeFalse();
    }

    [Fact]
    public void OptionsValidator_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new TenancyAspNetCoreOptionsValidator().Validate(null, null!));
    }

    [Fact]
    public void OptionsValidator_HeaderDisabled_Succeeds()
    {
        new TenancyAspNetCoreOptionsValidator().Validate(null, new TenancyAspNetCoreOptions()).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaTenancyAspNetCore_HeaderEnabledWithoutTrust_FailsOptionsValidation()
    {
        var services = BuildServices(o => o.HeaderResolver.Enabled = true);

        using var provider = services.BuildServiceProvider();

        var ex = Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<TenancyAspNetCoreOptions>>().Value);
        ex.Message.ShouldContain("TrustedProxies");
        ex.Message.ShouldContain("TrustAnyPeer");
    }

    [Fact]
    public void AddEncinaTenancyAspNetCore_HeaderEnabledWithoutTrust_FailsStartupValidation()
    {
        var services = BuildServices(o => o.HeaderResolver.Enabled = true);

        using var provider = services.BuildServiceProvider();

        // ValidateOnStart registers the IStartupValidator the host runs before it starts
        var validator = provider.GetRequiredService<IStartupValidator>();
        Should.Throw<OptionsValidationException>(() => validator.Validate());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void AddEncinaTenancyAspNetCore_ValidConfigurations_BuildWithValidateOnBuildAndScopes(bool trustAnyPeer, bool enabled)
    {
        var services = BuildServices(o =>
        {
            o.HeaderResolver.Enabled = enabled;
            o.HeaderResolver.TrustAnyPeer = trustAnyPeer;
        });
        services.AddSingleton<TenantResolutionMiddleware>(sp => new TenantResolutionMiddleware(
            _ => Task.CompletedTask,
            sp.GetServices<ITenantResolver>(),
            sp.GetRequiredService<IOptions<TenancyOptions>>(),
            sp.GetRequiredService<IOptions<TenancyAspNetCoreOptions>>(),
            sp.GetRequiredService<ITenantStore>(),
            sp.GetRequiredService<ILogger<TenantResolutionMiddleware>>()));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<TenantResolutionMiddleware>().ShouldNotBeNull();
        provider.GetServices<ITenantResolver>().Count().ShouldBe(4);
    }

    [Fact]
    public void AddEncinaTenancyAspNetCore_HeaderEnabledWithTrustedProxies_PassesValidation()
    {
        var services = BuildServices(o =>
        {
            o.HeaderResolver.Enabled = true;
            o.HeaderResolver.TrustedProxies.Add(IPNetwork.Parse("10.0.0.0/24"));
        });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<IOptions<TenancyAspNetCoreOptions>>().Value.HeaderResolver.Enabled.ShouldBeTrue();
    }

    private static ServiceCollection BuildServices(Action<TenancyAspNetCoreOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddEncinaTenancy();
        services.AddEncinaTenancyAspNetCore(configure);
        return services;
    }

    private TenantResolutionMiddleware Create()
    {
        var options = Options.Create(_aspNetCoreOptions);
        ITenantResolver[] resolvers =
        [
            new HeaderTenantResolver(options, _headerLogger),
            new ClaimTenantResolver(options),
            new RouteTenantResolver(options),
            new SubdomainTenantResolver(options),
        ];

        return new TenantResolutionMiddleware(
            _ =>
            {
                _nextCalled = true;
                _seen = _accessor.RequestContext;
                return Task.CompletedTask;
            },
            resolvers,
            Options.Create(_tenancyOptions),
            options,
            _store,
            _middlewareLogger);
    }

    private static DefaultHttpContext Request(string? claim, string? header, string peer)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);

        if (claim is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", claim)], "test"));
        }

        if (header is not null)
        {
            context.Request.Headers["X-Tenant-ID"] = header;
        }

        return context;
    }

    private static async Task<string> BodyOf(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
