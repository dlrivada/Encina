using Encina.AspNetCore;
using Encina.AspNetCore.Authorization;
using Encina.Testing;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Spike reproduction for finding N2 (verification spike, branch spike/verify-request-context):
/// <see cref="AuthorizationPipelineBehavior{TRequest,TResponse}"/> determines "who is calling" and
/// "are they authenticated" exclusively from <see cref="IHttpContextAccessor.HttpContext"/>. It never
/// consults the <see cref="IRequestContext"/> parameter it already receives (the transport-agnostic
/// ambient context that <c>context.UserId</c> is documented to carry — see
/// src/Encina/Abstractions/IRequestContext.cs).
/// </summary>
/// <remarks>
/// Root cause: src/Encina.AspNetCore/AuthorizationPipelineBehavior.cs, <c>Handle</c> method,
/// step 5 (around lines 156-171): <c>var httpContext = _httpContextAccessor.HttpContext; if
/// (httpContext is null) { ... return Left(...AuthorizationUnauthorized...); }</c>. In an
/// interactive Blazor Server circuit, <see cref="IHttpContextAccessor.HttpContext"/> is <c>null</c>
/// for the lifetime of the circuit after the initial negotiate request (there is no per-render or
/// per-event HTTP request) — this is standard, well-documented ASP.NET Core Blazor Server behavior,
/// not a test artifact. The user's identity in that scenario is normally obtained from
/// <c>AuthenticationStateProvider</c> / <c>CascadingAuthenticationState</c> and would be the thing an
/// application places on <see cref="IRequestContext.UserId"/> before calling <c>IEncina.Send</c>. But
/// because the behavior hard-codes <c>httpContext is null =&gt; deny</c> without ever looking at
/// <c>context.UserId</c>, an already-authenticated Blazor Server user is unconditionally denied.
/// Confirmed alongside the existing (and still valid) baseline test
/// <c>AuthorizationPipelineBehaviorTests.Handle_NoHttpContext_ReturnsError</c>, which documents the
/// same "HttpContext null =&gt; deny" behavior as the currently expected outcome. There is no
/// Blazor-specific Encina package and no <c>AuthenticationStateProvider</c> reference anywhere under
/// <c>src/</c> (verified via search), so no supported non-HTTP authorization path exists today.
/// </remarks>
public class AuthorizationPipelineBehaviorBlazorServerSpikeTests
{
    [Authorize]
    private sealed record AuthorizedRequest : ICommand<Unit>;

    /// <summary>
    /// CONFIRMED (N2): a caller that is already authenticated per the transport-agnostic
    /// <see cref="IRequestContext"/> — the only ambient signal available inside a Blazor Server
    /// circuit, where <see cref="IHttpContextAccessor.HttpContext"/> is <c>null</c> — should be
    /// authorized for a request that merely requires authentication ([Authorize] with no policy or
    /// role). Instead, the behavior denies unconditionally because it only looks at
    /// <c>IHttpContextAccessor.HttpContext.User</c> and never at <c>context.UserId</c>.
    /// </summary>
    [Fact]
    public async Task Handle_BlazorServerCircuit_AuthenticatedViaRequestContext_ButNoHttpContext_ShouldAuthorizeNotDeny()
    {
        // Arrange: simulates a Blazor Server interactive circuit. The app already knows the caller
        // is authenticated (e.g. resolved from AuthenticationStateProvider and placed on the ambient
        // IRequestContext before calling IEncina.Send), but there is no HttpContext for this
        // invocation - by design, not by test omission.
        var context = RequestContext.CreateForTest(userId: "blazor-user-1");
        var behavior = CreateBehavior<AuthorizedRequest, Unit>(httpContext: null);
        var request = new AuthorizedRequest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - expected (correct) behavior for an already-authenticated ambient caller.
        // Actual (current, buggy) behavior: Left(AuthorizationUnauthorized, "Authorization requires
        // HTTP context but none is available.") - nextStep is never invoked.
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    private static AuthorizationPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        HttpContext? httpContext)
        where TRequest : IRequest<TResponse>
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        // Never expected to be invoked: with HttpContext null, the behavior short-circuits before
        // reaching any policy/role check that would call into IAuthorizationService.
        var authorizationService = Substitute.For<IAuthorizationService>();
        var options = Options.Create(new AuthorizationConfiguration());
        var logger = NullLogger<AuthorizationPipelineBehavior<TRequest, TResponse>>.Instance;

        return new AuthorizationPipelineBehavior<TRequest, TResponse>(
            authorizationService,
            httpContextAccessor,
            options,
            logger);
    }
}
