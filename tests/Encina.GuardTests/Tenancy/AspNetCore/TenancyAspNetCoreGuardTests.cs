using Encina.Tenancy.AspNetCore;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Tenancy.AspNetCore;

/// <summary>
/// Guard tests for Encina.Tenancy.AspNetCore covering constructor and method null guards
/// for resolvers, middleware, health checks, and service collection extensions.
/// </summary>
[Trait("Category", "Guard")]
public sealed class TenancyAspNetCoreGuardTests
{
    // ─── ClaimTenantResolver guards ───

    [Fact]
    public void ClaimTenantResolver_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ClaimTenantResolver(null!));
    }

    [Fact]
    public void ClaimTenantResolver_ValidOptions_Constructs()
    {
        var sut = new ClaimTenantResolver(Options.Create(new TenancyAspNetCoreOptions()));
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task ClaimTenantResolver_NullContext_Throws()
    {
        var sut = new ClaimTenantResolver(Options.Create(new TenancyAspNetCoreOptions()));
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ResolveAsync(null!));
    }

    // ─── HeaderTenantResolver guards ───

    [Fact]
    public void HeaderTenantResolver_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new HeaderTenantResolver(null!));
    }

    [Fact]
    public void HeaderTenantResolver_ValidOptions_Constructs()
    {
        var sut = new HeaderTenantResolver(Options.Create(new TenancyAspNetCoreOptions()));
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task HeaderTenantResolver_NullContext_Throws()
    {
        var sut = new HeaderTenantResolver(Options.Create(new TenancyAspNetCoreOptions()));
        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.ResolveAsync(null!));
    }

    // ─── RouteTenantResolver guards ───

    [Fact]
    public void RouteTenantResolver_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RouteTenantResolver(null!));
    }

    [Fact]
    public void RouteTenantResolver_ValidOptions_Constructs()
    {
        var sut = new RouteTenantResolver(Options.Create(new TenancyAspNetCoreOptions()));
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task RouteTenantResolver_NullContext_Throws()
    {
        var sut = new RouteTenantResolver(Options.Create(new TenancyAspNetCoreOptions()));
        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.ResolveAsync(null!));
    }

    // ─── SubdomainTenantResolver guards ───

    [Fact]
    public void SubdomainTenantResolver_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new SubdomainTenantResolver(null!));
    }

    [Fact]
    public void SubdomainTenantResolver_ValidOptions_Constructs()
    {
        var sut = new SubdomainTenantResolver(Options.Create(new TenancyAspNetCoreOptions()));
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task SubdomainTenantResolver_NullContext_Throws()
    {
        var sut = new SubdomainTenantResolver(Options.Create(new TenancyAspNetCoreOptions()));
        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.ResolveAsync(null!));
    }

    // ─── ApplicationBuilderExtensions guard ───

    [Fact]
    public void UseTenantResolution_NullApp_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ApplicationBuilderExtensions.UseTenantResolution(null!));
    }

    // ─── HealthCheckBuilderExtensions guard ───

    [Fact]
    public void AddEncinaTenancy_NullBuilder_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            global::Encina.Tenancy.AspNetCore.Health.HealthCheckBuilderExtensions.AddEncinaTenancy(null!));
    }

    [Fact]
    public void AddEncinaTenancy_ValidBuilder_Succeeds()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        Should.NotThrow(() =>
            global::Encina.Tenancy.AspNetCore.Health.HealthCheckBuilderExtensions.AddEncinaTenancy(builder));
    }

    // ─── ServiceCollectionExtensions guards ───

    [Fact]
    public void AddEncinaTenancyAspNetCore_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaTenancyAspNetCore(_ => { }));
    }

    [Fact]
    public void AddEncinaTenancyAspNetCore_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaTenancyAspNetCore(null!));
    }

    [Fact]
    public void AddEncinaTenancyAspNetCore_ValidArgs_Registers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        var result = services.AddEncinaTenancyAspNetCore(_ => { });
        result.ShouldNotBeNull();
    }

    [Fact]
    public void AddTenantResolver_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddTenantResolver<HeaderTenantResolver>());
    }

    [Fact]
    public void AddTenantResolverInstance_NullServices_Throws()
    {
        var resolver = Substitute.For<ITenantResolver>();
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddTenantResolver(resolver));
    }

    [Fact]
    public void AddTenantResolverInstance_NullResolver_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddTenantResolver((ITenantResolver)null!));
    }

    [Fact]
    public void AddTenantResolverFactory_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddTenantResolver(_ => Substitute.For<ITenantResolver>()));
    }

    [Fact]
    public void AddTenantResolverFactory_NullFactory_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddTenantResolver((Func<IServiceProvider, ITenantResolver>)null!));
    }

    // ─── TenancyAspNetCoreOptions defaults ───

    [Fact]
    public void TenancyAspNetCoreOptions_Defaults()
    {
        var options = new TenancyAspNetCoreOptions();

        options.ShouldNotBeNull();
        options.Return400WhenTenantRequired.ShouldBeTrue();
        options.HeaderResolver.ShouldNotBeNull();
        options.HeaderResolver.Enabled.ShouldBeTrue();
        options.ClaimResolver.ShouldNotBeNull();
        options.ClaimResolver.Enabled.ShouldBeTrue();
        options.RouteResolver.ShouldNotBeNull();
        options.RouteResolver.Enabled.ShouldBeFalse();
        options.SubdomainResolver.ShouldNotBeNull();
        options.SubdomainResolver.Enabled.ShouldBeFalse();
    }
}
