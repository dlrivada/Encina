using System.Security.Claims;
using Encina.AspNetCore;
using Encina.AspNetCore.Authorization;
using Encina.Testing;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.AspNetCore;

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
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
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
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("requires authentication");
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationUnauthorized),
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
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
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
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
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
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("does not have any of the required roles");
            error.Message.ShouldContain("Admin");
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationForbidden),
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
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
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
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
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
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("does not satisfy policy");
            error.Message.ShouldContain("RequireElevation");
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationPolicyFailed),
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
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
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
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("does not satisfy policy");
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
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("Authorization requires HTTP context");
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationUnauthorized),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    [Fact]
    public async Task Handle_AllowAnonymous_BypassesAuthorization()
    {
        // Arrange - No HTTP context, no authenticated user
        var behavior = CreateBehavior<PublicRequest, Unit>(httpContext: null);
        var request = new PublicRequest();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_AllowAnonymous_WithAuthorize_AllowAnonymousWins()
    {
        // Arrange - AllowAnonymous should override Authorize even without authentication
        var behavior = CreateBehavior<MixedAuthRequest, Unit>(httpContext: null);
        var request = new MixedAuthRequest();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_PolicyAuthorization_ReceivesRequestAsResource()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var capturedResource = (object?)null;
        var authorizationService = new ResourceCapturingAuthorizationService(
            shouldSucceed: true,
            onAuthorize: resource => capturedResource = resource);
        var behavior = CreateBehavior<PolicyProtectedRequest, Unit>(httpContext, authorizationService);
        var request = new PolicyProtectedRequest();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        capturedResource.ShouldNotBeNull();
        capturedResource.ShouldBeOfType<PolicyProtectedRequest>();
        capturedResource.ShouldBeSameAs(request);
    }

    // ── CQRS auto-apply tests ─────────────────────────────────────────

    [Fact]
    public async Task Handle_CommandWithoutAttributes_AutoApplyEnabled_AppliesDefaultCommandPolicy()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var authorizationService = new TestAuthorizationService(shouldSucceed: false);
        var config = new AuthorizationConfiguration
        {
            AutoApplyPolicies = true,
            DefaultCommandPolicy = "RequireAuthenticated"
        };
        var behavior = CreateBehavior<PlainCommand, Unit>(httpContext, authorizationService, config);
        var request = new PlainCommand();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("auto-applied default policy");
            error.Message.ShouldContain("RequireAuthenticated");
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationPolicyFailed),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    [Fact]
    public async Task Handle_QueryWithoutAttributes_AutoApplyEnabled_AppliesDefaultQueryPolicy()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var authorizationService = new TestAuthorizationService(shouldSucceed: false);
        var config = new AuthorizationConfiguration
        {
            AutoApplyPolicies = true,
            DefaultQueryPolicy = "ReadOnly"
        };
        var behavior = CreateBehavior<PlainQuery, string>(httpContext, authorizationService, config);
        var request = new PlainQuery();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult(Right<EncinaError, string>("ok"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("auto-applied default policy");
            error.Message.ShouldContain("ReadOnly");
        });
    }

    [Fact]
    public async Task Handle_AutoApplyPoliciesDisabled_NoDefaultPolicyApplied()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var config = new AuthorizationConfiguration { AutoApplyPolicies = false };
        var behavior = CreateBehavior<PlainCommand, Unit>(httpContext, configuration: config);
        var request = new PlainCommand();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_CommandWithExplicitAttribute_AutoApplyEnabled_DoesNotDoubleApply()
    {
        // Arrange - Has [Authorize], so auto-apply should NOT kick in
        var httpContext = CreateAuthenticatedContext("user-123");
        var config = new AuthorizationConfiguration { AutoApplyPolicies = true };
        var behavior = CreateBehavior<AuthorizedRequest, Unit>(httpContext, configuration: config);
        var request = new AuthorizedRequest();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_AutoApplySucceeds_ProceedsToNextStep()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var authorizationService = new TestAuthorizationService(shouldSucceed: true);
        var config = new AuthorizationConfiguration { AutoApplyPolicies = true };
        var behavior = CreateBehavior<PlainCommand, Unit>(httpContext, authorizationService, config);
        var request = new PlainCommand();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    // ── ResourceAuthorize tests ─────────────────────────────────────

    [Fact]
    public async Task Handle_ResourceAuthorizeAttribute_PolicySucceeds_Proceeds()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var authorizationService = new TestAuthorizationService(shouldSucceed: true);
        var behavior = CreateBehavior<ResourceProtectedCommand, Unit>(httpContext, authorizationService);
        var request = new ResourceProtectedCommand("order-1");
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_ResourceAuthorizeAttribute_PolicyFails_ReturnsResourceDenied()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var authorizationService = new TestAuthorizationService(shouldSucceed: false);
        var behavior = CreateBehavior<ResourceProtectedCommand, Unit>(httpContext, authorizationService);
        var request = new ResourceProtectedCommand("order-1");
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("Resource authorization denied");
            error.Message.ShouldContain("CanEditOrder");
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationResourceDenied),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    [Fact]
    public async Task Handle_ResourceAuthorizeAttribute_PassesRequestAsResource()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var capturedResource = (object?)null;
        var authorizationService = new ResourceCapturingAuthorizationService(
            shouldSucceed: true,
            onAuthorize: resource => capturedResource = resource);
        var behavior = CreateBehavior<ResourceProtectedCommand, Unit>(httpContext, authorizationService);
        var request = new ResourceProtectedCommand("order-42");
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> nextStep = () =>
            ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        capturedResource.ShouldNotBeNull();
        capturedResource.ShouldBeOfType<ResourceProtectedCommand>();
        ((ResourceProtectedCommand)capturedResource).OrderId.ShouldBe("order-42");
    }

    [Fact]
    public async Task Handle_ResourceAuthorize_WithAuthorize_BothChecked()
    {
        // Arrange - Has both [Authorize(Roles="Admin")] and [ResourceAuthorize("CanEdit")]
        var httpContext = CreateAuthenticatedContext("user-123", roles: AdminRole);
        var authorizationService = new TestAuthorizationService(shouldSucceed: true);
        var behavior = CreateBehavior<AuthorizedResourceCommand, Unit>(httpContext, authorizationService);
        var request = new AuthorizedResourceCommand();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
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

    [AllowAnonymous]
    private sealed record PublicRequest : ICommand<Unit>;

    [Authorize(Roles = "Admin")]
    [AllowAnonymous]
    private sealed record MixedAuthRequest : ICommand<Unit>;

    // CQRS types without attributes (for auto-apply tests)
    private sealed record PlainCommand : ICommand<Unit>;
    private sealed record PlainQuery : IQuery<string>;

    // ResourceAuthorize types
    [ResourceAuthorize("CanEditOrder")]
    private sealed record ResourceProtectedCommand(string OrderId) : ICommand<Unit>;

    [Authorize(Roles = "Admin")]
    [ResourceAuthorize("CanEdit")]
    private sealed record AuthorizedResourceCommand : ICommand<Unit>;

    // Helper methods
    private static AuthorizationPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        HttpContext? httpContext = null,
        IAuthorizationService? authorizationService = null,
        AuthorizationConfiguration? configuration = null)
        where TRequest : IRequest<TResponse>
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        authorizationService ??= new TestAuthorizationService(shouldSucceed: true);
        configuration ??= new AuthorizationConfiguration();

        var options = Options.Create(configuration);
        var logger = NullLogger<AuthorizationPipelineBehavior<TRequest, TResponse>>.Instance;

        return new AuthorizationPipelineBehavior<TRequest, TResponse>(
            authorizationService,
            httpContextAccessor,
            options,
            logger);
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

/// <summary>
/// Authorization service that captures the resource passed to it for testing resource-based authorization.
/// </summary>
public class ResourceCapturingAuthorizationService : IAuthorizationService, IAuthorizationHandler
{
    private readonly bool _shouldSucceed;
    private readonly Action<object?> _onAuthorize;

    public ResourceCapturingAuthorizationService(bool shouldSucceed, Action<object?> onAuthorize)
    {
        _shouldSucceed = shouldSucceed;
        _onAuthorize = onAuthorize;
    }

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        IEnumerable<IAuthorizationRequirement> requirements)
    {
        _onAuthorize(resource);

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
        _onAuthorize(resource);

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
