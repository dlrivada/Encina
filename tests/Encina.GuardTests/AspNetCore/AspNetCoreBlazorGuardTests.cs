using Encina.AspNetCore.Blazor;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Encina.GuardTests.AspNetCore;

/// <summary>
/// Guard tests covering null-guard clauses for Encina.AspNetCore.Blazor.
/// </summary>
[Trait("Category", "Guard")]
public sealed class AspNetCoreBlazorGuardTests
{
    [Fact]
    public void CircuitHandler_NullAuthenticationStateProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RequestIdentityCircuitHandler(null!, Substitute.For<IInternalRequestContextScopeFactory>()))
            .ParamName.ShouldBe("authenticationStateProvider");
    }

    [Fact]
    public void CircuitHandler_NullScopes_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RequestIdentityCircuitHandler(Substitute.For<AuthenticationStateProvider>(), null!))
            .ParamName.ShouldBe("scopes");
    }

    [Fact]
    public void CircuitHandler_NullNext_Throws()
    {
        var handler = new RequestIdentityCircuitHandler(
            Substitute.For<AuthenticationStateProvider>(), Substitute.For<IInternalRequestContextScopeFactory>());

        Should.Throw<ArgumentNullException>(() => handler.CreateInboundActivityHandler(null!))
            .ParamName.ShouldBe("next");
    }

    [Fact]
    public void AddEncinaBlazorAuthorization_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            global::Encina.AspNetCore.Blazor.ServiceCollectionExtensions.AddEncinaBlazorAuthorization(null!));
    }

    [Fact]
    public void AddEncinaBlazorAuthorization_ValidServices_RegistersTheCircuitHandler()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaBlazorAuthorization();

        result.ShouldBeSameAs(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(CircuitHandler) && sd.ImplementationType == typeof(RequestIdentityCircuitHandler));
    }
}
