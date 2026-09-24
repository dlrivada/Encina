using Encina.AspNetCore;
using Encina.AspNetCore.Blazor;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Encina.GuardTests.AspNetCore;

/// <summary>
/// Guard tests covering null-guard clauses for public Encina.AspNetCore.Blazor APIs.
/// </summary>
[Trait("Category", "Guard")]
public sealed class AspNetCoreBlazorGuardTests
{
    [Fact]
    public void Constructor_NullAuthenticationStateProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AuthenticationStatePrincipalResolver(null!));
    }

    [Fact]
    public void Constructor_ValidAuthenticationStateProvider_Succeeds()
    {
        var provider = Substitute.For<AuthenticationStateProvider>();
        var accessor = Substitute.For<IHttpContextAccessor>();

        Should.NotThrow(() => new AuthenticationStatePrincipalResolver(provider, accessor));
    }

    [Fact]
    public void AddEncinaBlazorAuthorization_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            global::Encina.AspNetCore.Blazor.ServiceCollectionExtensions.AddEncinaBlazorAuthorization(null!));
    }

    [Fact]
    public void AddEncinaBlazorAuthorization_ValidServices_RegistersResolver()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaBlazorAuthorization();

        result.ShouldNotBeNull();
        services.ShouldContain(sd => sd.ServiceType == typeof(IPrincipalResolver));
    }
}
