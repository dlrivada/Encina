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
    public async Task CircuitHandler_ScopeOpened_DoesNotRunTheMaskOrThrow()
    {
        var (handler, scopes) = HandlerWithRefusal(null);
        var ran = false;

        await handler.CreateInboundActivityHandler(_ =>
        {
            ran = true;
            return Task.CompletedTask;
        })(null!);

        ran.ShouldBeFalse("the stubbed factory does not call the work");
        await scopes.DidNotReceiveWithAnyArgs().RunAnonymousMarkerAsync(default, null!, default);
    }

    [Fact]
    public async Task CircuitHandler_UnknownRefusalCode_FailsClosedWithoutCallingNext()
    {
        var (handler, scopes) = HandlerWithRefusal("some.future.code");
        var ran = false;

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => handler.CreateInboundActivityHandler(_ =>
        {
            ran = true;
            return Task.CompletedTask;
        })(null!));

        ran.ShouldBeFalse();
        thrown.Message.ShouldContain("some.future.code");
        await scopes.DidNotReceiveWithAnyArgs().RunAnonymousMarkerAsync(default, null!, default);
    }

    [Fact]
    public async Task CircuitHandler_HostRefusal_RunsMasked()
    {
        var (handler, scopes) = HandlerWithRefusal(RequestIdentityErrorCodes.ScopeConflict);

        await handler.CreateInboundActivityHandler(_ => Task.CompletedTask)(null!);

        await scopes.ReceivedWithAnyArgs(1).RunAnonymousMarkerAsync(default, null!, default);
    }

    private static (RequestIdentityCircuitHandler Handler, IInternalRequestContextScopeFactory Scopes) HandlerWithRefusal(string? code)
    {
        var authentication = Substitute.For<AuthenticationStateProvider>();
        authentication.GetAuthenticationStateAsync()
            .Returns(Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal())));
        var scopes = Substitute.For<IInternalRequestContextScopeFactory>();
        scopes.RunHostInboundAsync<Unit>(default!, default!, default)
            .ReturnsForAnyArgs(Task.FromResult(code is null
                ? LanguageExt.Prelude.Right<EncinaError, Unit>(Unit.Default)
                : LanguageExt.Prelude.Left<EncinaError, Unit>(EncinaErrors.Create(code, "refused"))));
        scopes.RunAnonymousMarkerAsync(default, null!, default)
            .ReturnsForAnyArgs(Task.FromResult(LanguageExt.Prelude.Right<EncinaError, Unit>(Unit.Default)));
        return (new RequestIdentityCircuitHandler(authentication, scopes), scopes);
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
