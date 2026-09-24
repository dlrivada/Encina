using Encina.AspNetCore;
using Encina.AspNetCore.Blazor;
using Microsoft.AspNetCore.Components.Authorization;
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
    public void AddEncinaBlazorAuthorization_ReplacesDefaultResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaAspNetCore();
        services.AddScoped<AuthenticationStateProvider, NoOpAuthenticationStateProvider>();

        // Act
        var result = services.AddEncinaBlazorAuthorization();

        // Assert
        result.ShouldBeSameAs(services);

        var resolverDescriptors = services
            .Where(sd => sd.ServiceType == typeof(IPrincipalResolver))
            .ToList();

        resolverDescriptors.Count.ShouldBe(1);
        resolverDescriptors[0].ImplementationType.ShouldBe(typeof(AuthenticationStatePrincipalResolver));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IPrincipalResolver>().ShouldBeOfType<AuthenticationStatePrincipalResolver>();
    }

    private sealed class NoOpAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
