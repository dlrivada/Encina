using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;
using static LanguageExt.Prelude;

namespace SimpleMediator.AspNetCore.Tests;

public class AuthorizationPipelineBehaviorTests
{
    [Fact]
    public async Task Handle_NoAuthorizeAttribute_ProceedsToNextStep()
    {
        // Arrange
        var behavior = CreateBehavior<UnauthorizedRequest, Unit>();
        var request = new UnauthorizedRequest();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<MediatorError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AuthorizeAttribute_UnauthenticatedUser_ReturnsError()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var behavior = CreateBehavior<AuthorizedRequest, Unit>(httpContext);
        var request = new AuthorizedRequest();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<MediatorError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("requires authentication");
            error.GetCode().Match(
                Some: code => code.Should().Be("authorization.unauthenticated"),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    [Fact]
    public async Task Handle_AuthorizeAttribute_AuthenticatedUser_ProceedsToNextStep()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var behavior = CreateBehavior<AuthorizedRequest, Unit>(httpContext);
        var request = new AuthorizedRequest();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<MediatorError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RoleRequirement_UserHasRole_ProceedsToNextStep()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123", roles: AdminRole);
        var behavior = CreateBehavior<AdminOnlyRequest, Unit>(httpContext);
        var request = new AdminOnlyRequest();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<MediatorError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RoleRequirement_UserLacksRole_ReturnsError()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123", roles: UserRole);
        var behavior = CreateBehavior<AdminOnlyRequest, Unit>(httpContext);
        var request = new AdminOnlyRequest();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<MediatorError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("does not have any of the required roles");
            error.Message.Should().Contain("Admin");
            error.GetCode().Match(
                Some: code => code.Should().Be("authorization.insufficient_roles"),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    [Fact]
    public async Task Handle_MultipleRoles_UserHasAnyRole_ProceedsToNextStep()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123", roles: ManagerRole);
        var behavior = CreateBehavior<MultiRoleRequest, Unit>(httpContext);
        var request = new MultiRoleRequest();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<MediatorError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PolicyRequirement_PolicySucceeds_ProceedsToNextStep()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var authorizationService = new TestAuthorizationService(shouldSucceed: true);
        var behavior = CreateBehavior<PolicyProtectedRequest, Unit>(httpContext, authorizationService);
        var request = new PolicyProtectedRequest();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<MediatorError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PolicyRequirement_PolicyFails_ReturnsError()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var authorizationService = new TestAuthorizationService(shouldSucceed: false);
        var behavior = CreateBehavior<PolicyProtectedRequest, Unit>(httpContext, authorizationService);
        var request = new PolicyProtectedRequest();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<MediatorError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("does not satisfy policy");
            error.Message.Should().Contain("RequireElevation");
            error.GetCode().Match(
                Some: code => code.Should().Be("authorization.policy_failed"),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    [Fact]
    public async Task Handle_MultipleAuthorizeAttributes_AllMustPass()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123", roles: AdminRole);
        var authorizationService = new TestAuthorizationService(shouldSucceed: true);
        var behavior = CreateBehavior<MultipleRequirementsRequest, Unit>(httpContext, authorizationService);
        var request = new MultipleRequirementsRequest();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<MediatorError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MultipleAuthorizeAttributes_OneFails_ReturnsError()
    {
        // Arrange - User has Admin role but fails policy
        var httpContext = CreateAuthenticatedContext("user-123", roles: AdminRole);
        var authorizationService = new TestAuthorizationService(shouldSucceed: false);
        var behavior = CreateBehavior<MultipleRequirementsRequest, Unit>(httpContext, authorizationService);
        var request = new MultipleRequirementsRequest();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<MediatorError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("does not satisfy policy");
        });
    }

    [Fact]
    public async Task Handle_NoHttpContext_ReturnsError()
    {
        // Arrange
        var behavior = CreateBehavior<AuthorizedRequest, Unit>(httpContext: null);
        var request = new AuthorizedRequest();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<MediatorError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.Message.Should().Contain("Authorization requires HTTP context");
            error.GetCode().Match(
                Some: code => code.Should().Be("authorization.no_http_context"),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    // Test request types
    private sealed record UnauthorizedRequest : ICommand<Unit>;

    [Authorize]
    private sealed record AuthorizedRequest : ICommand<Unit>;

    [Authorize(Roles = "Admin")]
    private sealed record AdminOnlyRequest : ICommand<Unit>;

    [Authorize(Roles = "Admin,Manager,Supervisor")]
    private sealed record MultiRoleRequest : ICommand<Unit>;

    [Authorize(Policy = "RequireElevation")]
    private sealed record PolicyProtectedRequest : ICommand<Unit>;

    [Authorize(Roles = "Admin")]
    [Authorize(Policy = "RequireApproval")]
    private sealed record MultipleRequirementsRequest : ICommand<Unit>;

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

    private static readonly string[] AdminRole = ["Admin"];
    private static readonly string[] UserRole = ["User"];
    private static readonly string[] ManagerRole = ["Manager"];

    private static DefaultHttpContext CreateAuthenticatedContext(
        string userId,
        string[]? roles = null)
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

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        return httpContext;
    }
}

/// <summary>
/// Test authorization service for unit testing.
/// </summary>
public class TestAuthorizationService : IAuthorizationService, IAuthorizationHandler
{
    private readonly bool _shouldSucceed;

    public TestAuthorizationService(bool shouldSucceed)
    {
        _shouldSucceed = shouldSucceed;
    }

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        IEnumerable<IAuthorizationRequirement> requirements)
    {
        var result = _shouldSucceed
            ? AuthorizationResult.Success()
            : AuthorizationResult.Failed(
                AuthorizationFailure.Failed(new[] { new AuthorizationFailureReason(this, "Policy failed") }));

        return Task.FromResult(result);
    }

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        string policyName)
    {
        var result = _shouldSucceed
            ? AuthorizationResult.Success()
            : AuthorizationResult.Failed(
                AuthorizationFailure.Failed(new[] { new AuthorizationFailureReason(this, $"Policy '{policyName}' failed") }));

        return Task.FromResult(result);
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        // Not used in tests
        return Task.CompletedTask;
    }
}
