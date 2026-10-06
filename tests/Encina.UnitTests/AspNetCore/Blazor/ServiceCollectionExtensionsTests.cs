using Encina.AspNetCore;
using Encina.AspNetCore.Blazor;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.AspNetCore.Blazor;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaBlazorAuthorization_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            global::Encina.AspNetCore.Blazor.ServiceCollectionExtensions.AddEncinaBlazorAuthorization(null!));
    }

    [Fact]
    public void AddEncinaBlazorAuthorization_RegistersTheCircuitHandlerOnce_AndResolvesItPerCircuit()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAspNetCore();
        services.AddScoped<AuthenticationStateProvider, NoOpAuthenticationStateProvider>();

        var result = services.AddEncinaBlazorAuthorization();
        services.AddEncinaBlazorAuthorization();

        result.ShouldBeSameAs(services);
        services.Count(sd => sd.ServiceType == typeof(CircuitHandler)).ShouldBe(1);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetServices<CircuitHandler>().ShouldHaveSingleItem().ShouldBeOfType<RequestIdentityCircuitHandler>();
    }

    [Fact]
    public void AddEncinaBlazorAuthorization_WithoutAddEncinaAspNetCore_RegistersTheScopeFactory()
    {
        // AuthenticationStateProvider is Blazor's own registration (AddServerSideBlazor).
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<AuthenticationStateProvider, NoOpAuthenticationStateProvider>();

        services.AddEncinaBlazorAuthorization();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IInternalRequestContextScopeFactory>()
            .ShouldBeSameAs(provider.GetRequiredService<IRequestContextScopeFactory>());
    }

    private sealed class NoOpAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
