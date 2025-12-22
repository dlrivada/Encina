using System.Security.Claims;
using Encina.AspNetCore.Tests;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.AspNetCore.PropertyTests;

/// <summary>
/// Property-based tests for <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies invariants that must always hold true.
/// </summary>
[Trait("Category", "Property")]
public sealed class AuthorizationPipelineBehaviorPropertyTests
{
    [Fact]
    public async Task Property_NoAuthAttribute_AlwaysCallsNextStep()
    {
        // Property: Request WITHOUT [Authorize] ALWAYS proceeds to next step
        var behavior = CreateBehavior<UnauthorizedRequest, string>();
        var contexts = Enumerable.Range(0, 10)
            .Select(_ => RequestContext.CreateForTest())
            .ToList();

        foreach (var context in contexts)
        {
            var request = new UnauthorizedRequest();
            var nextStepCalled = false;

            RequestHandlerCallback<string> nextStep = () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult(Right<EncinaError, string>("success"));
            };

            await behavior.Handle(request, context, nextStep, CancellationToken.None);

            nextStepCalled.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Property_NoHttpContext_AlwaysReturnsError()
    {
        // Property: [Authorize] WITHOUT HttpContext ALWAYS returns error
        var behavior = CreateBehavior<AuthorizedRequest, string>(httpContext: null);
        var contexts = Enumerable.Range(0, 5)
            .Select(_ => RequestContext.CreateForTest())
            .ToList();

        foreach (var context in contexts)
        {
            var request = new AuthorizedRequest();

            RequestHandlerCallback<string> nextStep = () =>
                ValueTask.FromResult(Right<EncinaError, string>("success"));

            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            result.IsLeft.ShouldBeTrue();
            result.Match(
                Left: error => error.GetCode().Match(
                    Some: code => code.ShouldBe("authorization.no_http_context"),
                    None: () => Assert.Fail("Expected error code")),
                Right: _ => Assert.Fail("Expected Left"));
        }
    }

    [Fact]
    public async Task Property_UnauthenticatedUser_AlwaysReturnsError()
    {
        // Property: [Authorize] with UNAUTHENTICATED user ALWAYS returns error
        var httpContext = new DefaultHttpContext(); // No user
        var behavior = CreateBehavior<AuthorizedRequest, string>(httpContext);
        var contexts = Enumerable.Range(0, 5)
            .Select(_ => RequestContext.CreateForTest())
            .ToList();

        foreach (var context in contexts)
        {
            var request = new AuthorizedRequest();

            RequestHandlerCallback<string> nextStep = () =>
                ValueTask.FromResult(Right<EncinaError, string>("success"));

            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            result.IsLeft.ShouldBeTrue();
            result.Match(
                Left: error => error.GetCode().Match(
                    Some: code => code.ShouldBe("authorization.unauthenticated"),
                    None: () => Assert.Fail("Expected error code")),
                Right: _ => Assert.Fail("Expected Left"));
        }
    }

    [Fact]
    public async Task Property_AuthenticatedUser_AlwaysCallsNextStep()
    {
        // Property: [Authorize] with AUTHENTICATED user ALWAYS proceeds to next step
        var userIds = Enumerable.Range(0, 10).Select(i => $"user-{i}").ToList();

        foreach (var userId in userIds)
        {
            var httpContext = CreateAuthenticatedContext(userId);
            var behavior = CreateBehavior<AuthorizedRequest, string>(httpContext);
            var request = new AuthorizedRequest();
            var context = RequestContext.CreateForTest();
            var nextStepCalled = false;

            RequestHandlerCallback<string> nextStep = () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult(Right<EncinaError, string>("success"));
            };

            await behavior.Handle(request, context, nextStep, CancellationToken.None);

            nextStepCalled.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Property_MissingRole_AlwaysReturnsError()
    {
        // Property: Role requirement WITHOUT matching role ALWAYS returns error
        var userRoles = new[] { "User", "Guest", "Viewer", "Reader", "Contributor" };

        foreach (var role in userRoles)
        {
            var httpContext = CreateAuthenticatedContext("user-123", roles: [role]);
            var behavior = CreateBehavior<AdminOnlyRequest, string>(httpContext);
            var request = new AdminOnlyRequest();
            var context = RequestContext.CreateForTest();

            RequestHandlerCallback<string> nextStep = () =>
                ValueTask.FromResult(Right<EncinaError, string>("success"));

            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            result.IsLeft.ShouldBeTrue();
            result.Match(
                Left: error => error.GetCode().Match(
                    Some: code => code.ShouldBe("authorization.insufficient_roles"),
                    None: () => Assert.Fail("Expected error code")),
                Right: _ => Assert.Fail("Expected Left"));
        }
    }

    [Fact]
    public async Task Property_HasRequiredRole_AlwaysCallsNextStep()
    {
        // Property: User with REQUIRED role ALWAYS proceeds
        var roles = new[] { "Admin", "Manager", "Supervisor" };

        foreach (var role in roles)
        {
            var httpContext = CreateAuthenticatedContext("user-123", roles: [role]);
            var behavior = CreateBehavior<MultiRoleRequest, string>(httpContext);
            var request = new MultiRoleRequest();
            var context = RequestContext.CreateForTest();
            var nextStepCalled = false;

            RequestHandlerCallback<string> nextStep = () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult(Right<EncinaError, string>("success"));
            };

            await behavior.Handle(request, context, nextStep, CancellationToken.None);

            nextStepCalled.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Property_PolicyFailed_AlwaysReturnsError()
    {
        // Property: Failed policy ALWAYS returns error
        var userIds = Enumerable.Range(0, 5).Select(i => $"user-{i}").ToList();

        foreach (var userId in userIds)
        {
            var httpContext = CreateAuthenticatedContext(userId);
            var authService = new TestAuthorizationService(shouldSucceed: false);
            var behavior = CreateBehavior<PolicyProtectedRequest, string>(httpContext, authService);
            var request = new PolicyProtectedRequest();
            var context = RequestContext.CreateForTest();

            RequestHandlerCallback<string> nextStep = () =>
                ValueTask.FromResult(Right<EncinaError, string>("success"));

            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            result.IsLeft.ShouldBeTrue();
            result.Match(
                Left: error => error.GetCode().Match(
                    Some: code => code.ShouldBe("authorization.policy_failed"),
                    None: () => Assert.Fail("Expected error code")),
                Right: _ => Assert.Fail("Expected Left"));
        }
    }

    [Fact]
    public async Task Property_PolicySucceeded_AlwaysCallsNextStep()
    {
        // Property: Successful policy ALWAYS proceeds
        var userIds = Enumerable.Range(0, 5).Select(i => $"user-{i}").ToList();

        foreach (var userId in userIds)
        {
            var httpContext = CreateAuthenticatedContext(userId);
            var authService = new TestAuthorizationService(shouldSucceed: true);
            var behavior = CreateBehavior<PolicyProtectedRequest, string>(httpContext, authService);
            var request = new PolicyProtectedRequest();
            var context = RequestContext.CreateForTest();
            var nextStepCalled = false;

            RequestHandlerCallback<string> nextStep = () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult(Right<EncinaError, string>("success"));
            };

            await behavior.Handle(request, context, nextStep, CancellationToken.None);

            nextStepCalled.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Property_NextStepResult_AlwaysPreserved()
    {
        // Property: If authorization passes, behavior ALWAYS returns next step's result
        var expectedResults = Enumerable.Range(0, 10).Select(i => $"result-{i}").ToList();

        foreach (var expectedResult in expectedResults)
        {
            var behavior = CreateBehavior<UnauthorizedRequest, string>();
            var request = new UnauthorizedRequest();
            var context = RequestContext.CreateForTest();

            RequestHandlerCallback<string> nextStep = () =>
                ValueTask.FromResult(Right<EncinaError, string>(expectedResult));

            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            result.Match(
                Left: _ => Assert.Fail("Expected Right"),
                Right: actual => actual.ShouldBe(expectedResult));
        }
    }

    [Fact]
    public async Task Property_NextStepError_AlwaysPreserved()
    {
        // Property: If authorization passes, behavior ALWAYS propagates next step's error
        var errorMessages = Enumerable.Range(0, 5).Select(i => $"error-{i}").ToList();

        foreach (var errorMessage in errorMessages)
        {
            var behavior = CreateBehavior<UnauthorizedRequest, string>();
            var request = new UnauthorizedRequest();
            var context = RequestContext.CreateForTest();
            var expectedError = EncinaErrors.Create("test.error", errorMessage);

            RequestHandlerCallback<string> nextStep = () =>
                ValueTask.FromResult(Left<EncinaError, string>(expectedError));

            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            result.Match(
                Left: error => error.Message.ShouldBe(errorMessage),
                Right: _ => Assert.Fail("Expected Left"));
        }
    }

    // Test types
    private sealed record UnauthorizedRequest : IRequest<string>;

    [Authorize]
    private sealed record AuthorizedRequest : IRequest<string>;

    [Authorize(Roles = "Admin")]
    private sealed record AdminOnlyRequest : IRequest<string>;

    [Authorize(Roles = "Admin,Manager,Supervisor")]
    private sealed record MultiRoleRequest : IRequest<string>;

    [Authorize(Policy = "RequireElevation")]
    private sealed record PolicyProtectedRequest : IRequest<string>;

    // Helper methods
    private static AuthorizationPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        HttpContext? httpContext = null,
        IAuthorizationService? authorizationService = null)
        where TRequest : IRequest<TResponse>
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        authorizationService ??= new TestAuthorizationService(shouldSucceed: true);

        return new AuthorizationPipelineBehavior<TRequest, TResponse>(
            authorizationService,
            httpContextAccessor);
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string userId, string[]? roles = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        if (roles != null)
        {
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        return new DefaultHttpContext
        {
            User = principal
        };
    }
}
