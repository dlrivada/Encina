using System.Security.Claims;
using Encina.AspNetCore;
using Encina.AspNetCore.Blazor;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.AspNetCore.Blazor;

public class AuthenticationStatePrincipalResolverTests
{
    [Fact]
    public void Constructor_NullAuthenticationStateProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AuthenticationStatePrincipalResolver(null!));
    }

    [Fact]
    public async Task ResolvePrincipalAsync_HttpContextAvailable_ReturnsHttpContextUser()
    {
        // Arrange: simulates a classic HTTP request (e.g. a Blazor Server prerender) where HttpContext
        // is available; it must win over AuthenticationStateProvider.
        var httpUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "http-user")], "TestAuthType"));
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = httpUser }
        };

        var authenticationStateProvider = new StubAuthenticationStateProvider(
            new ClaimsPrincipal(new ClaimsIdentity()));

        var resolver = new AuthenticationStatePrincipalResolver(authenticationStateProvider, httpContextAccessor);

        // Act
        var resolved = await resolver.ResolvePrincipalAsync(CancellationToken.None);

        // Assert
        resolved.ShouldBeSameAs(httpUser);
    }

    [Fact]
    public async Task ResolvePrincipalAsync_NoHttpContext_ReturnsAuthenticationStateUser()
    {
        // Arrange: simulates a Blazor Server interactive circuit, where HttpContext is null for the
        // whole lifetime of the circuit after the initial negotiate request.
        var circuitUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "blazor-user-1")], "TestAuthType"));
        var httpContextAccessor = new HttpContextAccessor { HttpContext = null };
        var authenticationStateProvider = new StubAuthenticationStateProvider(circuitUser);

        var resolver = new AuthenticationStatePrincipalResolver(authenticationStateProvider, httpContextAccessor);

        // Act
        var resolved = await resolver.ResolvePrincipalAsync(CancellationToken.None);

        // Assert
        resolved.ShouldBeSameAs(circuitUser);
    }

    [Fact]
    public async Task ResolvePrincipalAsync_NoHttpContextAccessor_ReturnsAuthenticationStateUser()
    {
        // Arrange: IHttpContextAccessor itself is optional (e.g. minimal Blazor Server hosting).
        var circuitUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "blazor-user-2")], "TestAuthType"));
        var authenticationStateProvider = new StubAuthenticationStateProvider(circuitUser);

        var resolver = new AuthenticationStatePrincipalResolver(authenticationStateProvider);

        // Act
        var resolved = await resolver.ResolvePrincipalAsync(CancellationToken.None);

        // Assert
        resolved.ShouldBeSameAs(circuitUser);
    }

    private sealed class StubAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _user;

        public StubAuthenticationStateProvider(ClaimsPrincipal user) => _user = user;

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(_user));
    }
}
