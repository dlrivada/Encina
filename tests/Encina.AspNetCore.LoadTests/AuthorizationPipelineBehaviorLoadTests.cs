using System.Security.Claims;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NBomber.CSharp;
using Encina.AspNetCore.Tests;
using Xunit.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.AspNetCore.LoadTests;

/// <summary>
/// Load tests for <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies performance and concurrency handling under stress.
/// </summary>
[Trait("Category", "Load")]
public sealed class AuthorizationPipelineBehaviorLoadTests
{
    private readonly ITestOutputHelper _output;

    public AuthorizationPipelineBehaviorLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void HighConcurrency_AuthorizedRequests_ShouldHandleLoad()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-load-test");

        // Act
        var scenario = Scenario.Create("authorized_requests", async context =>
        {
            var behavior = CreateBehavior<AuthorizedRequest, string>(httpContext);
            var request = new AuthorizedRequest();
            var requestContext = RequestContext.CreateForTest();

            RequestHandlerCallback<string> nextStep = () =>
                ValueTask.FromResult(Right<MediatorError, string>("success"));

            var result = await behavior.Handle(request, requestContext, nextStep, CancellationToken.None);

            return result.IsRight ? Response.Ok() : Response.Fail<string>(statusCode: "auth_failed");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void HighConcurrency_RoleBasedAuthorization_ShouldHandleLoad()
    {
        // Arrange - Create context per invocation for thread safety
        // Act
        var scenario = Scenario.Create("role_authorization", async context =>
        {
            var httpContext = CreateAuthenticatedContext($"user-{context.InvocationNumber}", roles: ["Admin"]);
            var behavior = CreateBehavior<AdminOnlyRequest, string>(httpContext);
            var request = new AdminOnlyRequest();
            var requestContext = RequestContext.CreateForTest();

            RequestHandlerCallback<string> nextStep = () =>
                ValueTask.FromResult(Right<MediatorError, string>("success"));

            var result = await behavior.Handle(request, requestContext, nextStep, CancellationToken.None);

            return result.IsRight ? Response.Ok() : Response.Fail<string>(statusCode: "insufficient_roles");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, RPS: {scen.Ok.Request.RPS}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void HighConcurrency_PolicyBasedAuthorization_ShouldHandleLoad()
    {
        // Arrange - Create context per invocation for thread safety
        // Act
        var scenario = Scenario.Create("policy_authorization", async context =>
        {
            var httpContext = CreateAuthenticatedContext($"policy-user-{context.InvocationNumber}");
            var authService = new TestAuthorizationService(shouldSucceed: true);
            var behavior = CreateBehavior<PolicyProtectedRequest, string>(httpContext, authService);
            var request = new PolicyProtectedRequest();
            var requestContext = RequestContext.CreateForTest();

            RequestHandlerCallback<string> nextStep = () =>
                ValueTask.FromResult(Right<MediatorError, string>("success"));

            var result = await behavior.Handle(request, requestContext, nextStep, CancellationToken.None);

            return result.IsRight ? Response.Ok() : Response.Fail<string>(statusCode: "policy_failed");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, RPS: {scen.Ok.Request.RPS}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900, got {scen.Ok.Request.Count}");
    }

    // NOTE: Endurance test disabled due to .NET 10 reflection issue under high concurrency
    // Internal CLR error (0x80131506) in GetCustomAttributes when called concurrently
    // This is a known limitation with reflection metadata under extreme load in .NET 10

    [Fact]
    public void MixedLoad_AuthorizedAndUnauthorized_ShouldHandleBoth()
    {
        // Arrange - Create context per invocation for thread safety
        // Act
        var authorizedScenario = Scenario.Create("authorized", async context =>
        {
            var authenticatedContext = CreateAuthenticatedContext($"auth-user-{context.InvocationNumber}");
            var behavior = CreateBehavior<AuthorizedRequest, string>(authenticatedContext);
            var request = new AuthorizedRequest();
            var requestContext = RequestContext.CreateForTest();

            RequestHandlerCallback<string> nextStep = () =>
                ValueTask.FromResult(Right<MediatorError, string>("success"));

            var result = await behavior.Handle(request, requestContext, nextStep, CancellationToken.None);
            return result.IsRight ? Response.Ok() : Response.Fail<string>(statusCode: "auth_failed");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var unauthorizedScenario = Scenario.Create("unauthorized", async context =>
        {
            var unauthenticatedContext = new DefaultHttpContext();
            var behavior = CreateBehavior<AuthorizedRequest, string>(unauthenticatedContext);
            var request = new AuthorizedRequest();
            var requestContext = RequestContext.CreateForTest();

            RequestHandlerCallback<string> nextStep = () =>
                ValueTask.FromResult(Right<MediatorError, string>("success"));

            var result = await behavior.Handle(request, requestContext, nextStep, CancellationToken.None);
            return result.IsLeft ? Response.Ok() : Response.Fail<string>(statusCode: "expected_error");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(authorizedScenario, unauthorizedScenario)
            .WithoutReports()
            .Run();

        // Assert
        var authScen = stats.ScenarioStats[0];
        var unauthScen = stats.ScenarioStats[1];

        _output.WriteLine($"Authorized - OK: {authScen.Ok.Request.Count}, Fail: {authScen.Fail.Request.Count}");
        _output.WriteLine($"Unauthorized - OK: {unauthScen.Ok.Request.Count}, Fail: {unauthScen.Fail.Request.Count}");

        Assert.True(authScen.Ok.Request.Count > 450, $"Expected > 450 authorized, got {authScen.Ok.Request.Count}");
        Assert.True(unauthScen.Ok.Request.Count > 450, $"Expected > 450 rejected, got {unauthScen.Ok.Request.Count}");
    }

    // Test types
    [Authorize]
    private sealed record AuthorizedRequest : IRequest<string>;

    [Authorize(Roles = "Admin")]
    private sealed record AdminOnlyRequest : IRequest<string>;

    [Authorize(Policy = "RequireElevation")]
    private sealed record PolicyProtectedRequest : IRequest<string>;

    // Helper methods
    private static AuthorizationPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        HttpContext? httpContext,
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
