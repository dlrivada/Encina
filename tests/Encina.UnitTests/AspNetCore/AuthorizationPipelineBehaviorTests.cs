using System.Security.Claims;
using Encina.AspNetCore;
using Encina.AspNetCore.Authorization;
using Encina.Testing;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Unit tests for <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/>: it evaluates the
/// request identity of the dispatch (<see cref="IRequestContext.Identity"/>, #1705 Phase 3) and never
/// logs or returns the user id.
/// </summary>
public class AuthorizationPipelineBehaviorTests
{
    private const string Sentinel = "sentinel-user-5c1e";

    private static readonly string[] AdminRole = ["Admin"];
    private static readonly string[] UserRole = ["User"];
    private static readonly string[] ManagerRole = ["Manager"];

    private static IRequestContext UserContext(string userId = "user-123", string[]? roles = null) =>
        TestRequestContext.For(TestIdentity.User(userId, roles));

    private static async Task<(Either<EncinaError, TResponse> Result, bool NextCalled)> RunAsync<TRequest, TResponse>(
        TRequest request,
        IRequestContext context,
        IAuthorizationService? authorizationService = null,
        AuthorizationConfiguration? configuration = null,
        FakeLogger<AuthorizationPipelineBehavior<TRequest, TResponse>>? logger = null)
        where TRequest : IRequest<TResponse>
    {
        var behavior = CreateBehavior<TRequest, TResponse>(authorizationService, configuration, logger);
        var nextCalled = false;
        var result = await behavior.Handle(request, context, () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, TResponse>(default!));
        }, CancellationToken.None);
        return (result, nextCalled);
    }

    private static void ShouldBeDeniedWith<T>(Either<EncinaError, T> result, string code, string? messagePart = null)
    {
        result.ShouldBeError();
        result.IfLeft(error =>
        {
            error.GetCode().IfNone("none").ShouldBe(code);
            if (messagePart is not null)
            {
                error.Message.ShouldContain(messagePart);
            }
        });
    }

    // ── No requirement ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoAuthorizeAttribute_ProceedsToNextStep()
    {
        var (result, nextCalled) = await RunAsync<UnauthorizedRequest, Unit>(new UnauthorizedRequest(), RequestContext.CreateForTest());

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_AllowAnonymous_BypassesAuthorization()
    {
        var (result, nextCalled) = await RunAsync<PublicRequest, Unit>(new PublicRequest(), RequestContext.CreateForTest());

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_AllowAnonymous_WithAuthorize_AllowAnonymousWins()
    {
        var (result, nextCalled) = await RunAsync<MixedAuthRequest, Unit>(new MixedAuthRequest(), RequestContext.CreateForTest());

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    // ── The caller is the request identity ────────────────────────────────

    [Fact]
    public async Task Handle_AuthorizeAttribute_AnonymousIdentity_ReturnsUnauthorized()
    {
        var (result, nextCalled) = await RunAsync<AuthorizedRequest, Unit>(new AuthorizedRequest(), RequestContext.CreateForTest());

        nextCalled.ShouldBeFalse();
        ShouldBeDeniedWith(result, EncinaErrorCodes.AuthorizationUnauthenticated, "requires authentication");
        result.IfLeft(error => error.GetDetails()["identityKind"].ShouldBe(nameof(IdentityKind.Anonymous)));
    }

    [Fact]
    public async Task Handle_AuthorizeAttribute_NullIdentity_IsDeniedAsAnonymous()
    {
        var context = Substitute.For<IRequestContext>();

        var (result, nextCalled) = await RunAsync<AuthorizedRequest, Unit>(new AuthorizedRequest(), context);

        nextCalled.ShouldBeFalse();
        ShouldBeDeniedWith(result, EncinaErrorCodes.AuthorizationUnauthenticated);
    }

    [Fact]
    public async Task Handle_AuthorizeAttribute_AuthenticatedUser_ProceedsToNextStep()
    {
        var (result, nextCalled) = await RunAsync<AuthorizedRequest, Unit>(new AuthorizedRequest(), UserContext());

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_AuthorizeAttribute_ServiceIdentity_ProceedsToNextStep()
    {
        var context = TestRequestContext.For(TestIdentity.Service("billing-job", roles: ["Admin"]));

        var (result, nextCalled) = await RunAsync<AdminOnlyRequest, Unit>(new AdminOnlyRequest(), context);

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_AuthenticatedIdentityWithoutPrincipal_SatisfiesNoRole()
    {
        var context = TestRequestContext.For(RequestIdentity.ForUser("no-principal-user"));

        var (result, _) = await RunAsync<AdminOnlyRequest, Unit>(new AdminOnlyRequest(), context);

        ShouldBeDeniedWith(result, EncinaErrorCodes.AuthorizationForbidden);
    }

    [Fact]
    public async Task Handle_PolicyAndRoleChecks_SeeOnlyTheIdentityPrincipal_NotAMutatedCopy()
    {
        var identity = TestIdentity.User("user-123", UserRole);
        identity.Principal!.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "late"));

        var (result, _) = await RunAsync<AdminOnlyRequest, Unit>(new AdminOnlyRequest(), TestRequestContext.For(identity));

        ShouldBeDeniedWith(result, EncinaErrorCodes.AuthorizationForbidden);
    }

    // ── Roles ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_RoleRequirement_UserHasRole_ProceedsToNextStep()
    {
        var (result, nextCalled) = await RunAsync<AdminOnlyRequest, Unit>(new AdminOnlyRequest(), UserContext(roles: AdminRole));

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_RoleRequirement_UserLacksRole_ReturnsError()
    {
        var (result, nextCalled) = await RunAsync<AdminOnlyRequest, Unit>(new AdminOnlyRequest(), UserContext(roles: UserRole));

        nextCalled.ShouldBeFalse();
        ShouldBeDeniedWith(result, EncinaErrorCodes.AuthorizationForbidden, "required roles");
        result.IfLeft(error => error.GetDetails()["identityKind"].ShouldBe(nameof(IdentityKind.User)));
    }

    [Fact]
    public async Task Handle_MultipleRoles_UserHasAnyRole_ProceedsToNextStep()
    {
        var (result, nextCalled) = await RunAsync<MultiRoleRequest, Unit>(new MultiRoleRequest(), UserContext(roles: ManagerRole));

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    // ── Policies ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PolicyRequirement_PolicySucceeds_ProceedsToNextStep()
    {
        var (result, nextCalled) = await RunAsync<PolicyProtectedRequest, Unit>(
            new PolicyProtectedRequest(), UserContext(), new TestAuthorizationService(shouldSucceed: true));

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_PolicyRequirement_PolicyFails_ReturnsError()
    {
        var (result, nextCalled) = await RunAsync<PolicyProtectedRequest, Unit>(
            new PolicyProtectedRequest(), UserContext(), new TestAuthorizationService(shouldSucceed: false));

        nextCalled.ShouldBeFalse();
        ShouldBeDeniedWith(result, EncinaErrorCodes.AuthorizationPolicyFailed, "RequireElevation");
    }

    [Fact]
    public async Task Handle_MultipleAuthorizeAttributes_AllMustPass()
    {
        var (result, nextCalled) = await RunAsync<MultipleRequirementsRequest, Unit>(
            new MultipleRequirementsRequest(), UserContext(roles: AdminRole), new TestAuthorizationService(shouldSucceed: true));

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_MultipleAuthorizeAttributes_OneFails_ReturnsError()
    {
        var (result, _) = await RunAsync<MultipleRequirementsRequest, Unit>(
            new MultipleRequirementsRequest(), UserContext(roles: AdminRole), new TestAuthorizationService(shouldSucceed: false));

        ShouldBeDeniedWith(result, EncinaErrorCodes.AuthorizationPolicyFailed, "does not satisfy policy");
    }

    [Fact]
    public async Task Handle_PolicyAuthorization_ReceivesRequestAsResource_AndTheIdentityPrincipal()
    {
        object? capturedResource = null;
        var request = new PolicyProtectedRequest();
        var service = new ResourceCapturingAuthorizationService(true, resource => capturedResource = resource);

        await RunAsync<PolicyProtectedRequest, Unit>(request, UserContext("user-777"), service);

        capturedResource.ShouldBeSameAs(request);
        service.LastUser.ShouldNotBeNull();
        service.LastUser.FindFirst("sub")!.Value.ShouldBe("user-777");
    }

    // ── CQRS auto-applied policies ────────────────────────────────────────

    [Fact]
    public async Task Handle_CommandWithoutAttributes_AutoApplyEnabled_AppliesDefaultCommandPolicy()
    {
        var config = new AuthorizationConfiguration { AutoApplyPolicies = true, DefaultCommandPolicy = "RequireAuthenticated" };

        var (result, _) = await RunAsync<PlainCommand, Unit>(new PlainCommand(), UserContext(), new TestAuthorizationService(false), config);

        ShouldBeDeniedWith(result, EncinaErrorCodes.AuthorizationPolicyFailed, "auto-applied default policy");
        result.IfLeft(error =>
        {
            error.Message.ShouldContain("RequireAuthenticated");
            error.GetDetails()["isCommand"].ShouldBe(true);
        });
    }

    [Fact]
    public async Task Handle_QueryWithoutAttributes_AutoApplyEnabled_AppliesDefaultQueryPolicy()
    {
        var config = new AuthorizationConfiguration { AutoApplyPolicies = true, DefaultQueryPolicy = "ReadOnly" };

        var (result, _) = await RunAsync<PlainQuery, string>(new PlainQuery(), UserContext(), new TestAuthorizationService(false), config);

        ShouldBeDeniedWith(result, EncinaErrorCodes.AuthorizationPolicyFailed, "ReadOnly");
        result.IfLeft(error => error.GetDetails()["isCommand"].ShouldBe(false));
    }

    [Fact]
    public async Task Handle_AutoApplyEnabled_AnonymousCaller_IsUnauthorized()
    {
        var config = new AuthorizationConfiguration { AutoApplyPolicies = true };

        var (result, nextCalled) = await RunAsync<PlainCommand, Unit>(new PlainCommand(), RequestContext.CreateForTest(), configuration: config);

        nextCalled.ShouldBeFalse();
        ShouldBeDeniedWith(result, EncinaErrorCodes.AuthorizationUnauthenticated);
    }

    [Fact]
    public async Task Handle_AutoApplyPoliciesDisabled_NoDefaultPolicyApplied()
    {
        var config = new AuthorizationConfiguration { AutoApplyPolicies = false };

        var (result, nextCalled) = await RunAsync<PlainCommand, Unit>(new PlainCommand(), RequestContext.CreateForTest(), configuration: config);

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_CommandWithExplicitAttribute_AutoApplyEnabled_DoesNotDoubleApply()
    {
        var config = new AuthorizationConfiguration { AutoApplyPolicies = true };

        var (result, nextCalled) = await RunAsync<AuthorizedRequest, Unit>(new AuthorizedRequest(), UserContext(), new TestAuthorizationService(false), config);

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_AutoApplySucceeds_ProceedsToNextStep()
    {
        var config = new AuthorizationConfiguration { AutoApplyPolicies = true };

        var (result, nextCalled) = await RunAsync<PlainCommand, Unit>(new PlainCommand(), UserContext(), new TestAuthorizationService(true), config);

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    // ── Resource-based authorization ──────────────────────────────────────

    [Fact]
    public async Task Handle_ResourceAuthorizeAttribute_PolicySucceeds_Proceeds()
    {
        var (result, nextCalled) = await RunAsync<ResourceProtectedCommand, Unit>(
            new ResourceProtectedCommand("order-1"), UserContext(), new TestAuthorizationService(true));

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_ResourceAuthorizeAttribute_PolicyFails_ReturnsResourceDenied()
    {
        var (result, _) = await RunAsync<ResourceProtectedCommand, Unit>(
            new ResourceProtectedCommand("order-1"), UserContext(), new TestAuthorizationService(false));

        ShouldBeDeniedWith(result, EncinaErrorCodes.AuthorizationResourceDenied, "Resource authorization denied");
        result.IfLeft(error => error.Message.ShouldContain("CanEditOrder"));
    }

    [Fact]
    public async Task Handle_ResourceAuthorizeAttribute_PassesRequestAsResource()
    {
        object? capturedResource = null;
        var service = new ResourceCapturingAuthorizationService(true, resource => capturedResource = resource);

        await RunAsync<ResourceProtectedCommand, Unit>(new ResourceProtectedCommand("order-42"), UserContext(), service);

        capturedResource.ShouldBeOfType<ResourceProtectedCommand>().OrderId.ShouldBe("order-42");
    }

    [Fact]
    public async Task Handle_ResourceAuthorize_WithAuthorize_BothChecked()
    {
        var (result, nextCalled) = await RunAsync<AuthorizedResourceCommand, Unit>(
            new AuthorizedResourceCommand(), UserContext(roles: AdminRole), new TestAuthorizationService(true));

        nextCalled.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    // ── No user id in logs or error details (finding 4) ───────────────────

    [Fact]
    public async Task Denials_And_Successes_NeverCarryTheUserId()
    {
        var logger = new FakeLogger<AuthorizationPipelineBehavior<AdminOnlyRequest, Unit>>();
        var policyLogger = new FakeLogger<AuthorizationPipelineBehavior<PolicyProtectedRequest, Unit>>();
        var resourceLogger = new FakeLogger<AuthorizationPipelineBehavior<ResourceProtectedCommand, Unit>>();

        var denied = await RunAsync<AdminOnlyRequest, Unit>(new AdminOnlyRequest(), UserContext(Sentinel, UserRole), logger: logger);
        await RunAsync<AdminOnlyRequest, Unit>(new AdminOnlyRequest(), UserContext(Sentinel, AdminRole), logger: logger);
        var policy = await RunAsync<PolicyProtectedRequest, Unit>(new PolicyProtectedRequest(), UserContext(Sentinel), new TestAuthorizationService(false), logger: policyLogger);
        var resource = await RunAsync<ResourceProtectedCommand, Unit>(new ResourceProtectedCommand("o"), UserContext(Sentinel), new TestAuthorizationService(false), logger: resourceLogger);

        var records = logger.Collector.GetSnapshot().Concat(policyLogger.Collector.GetSnapshot()).Concat(resourceLogger.Collector.GetSnapshot()).ToList();
        records.Select(static record => record.Id.Id).ShouldBe([201, 200, 201, 201]);
        foreach (var record in records)
        {
            record.Message.ShouldNotContain(Sentinel);
            record.Message.ShouldContain("IdentityKind: User");
            foreach (var pair in record.StructuredState ?? [])
            {
                (pair.Value ?? string.Empty).ShouldNotContain(Sentinel);
            }
        }

        foreach (var result in new[] { denied.Result, policy.Result, resource.Result })
        {
            result.IfLeft(error =>
            {
                error.GetDetails().ContainsKey("userId").ShouldBeFalse();
                error.GetDetails()["identityKind"].ShouldBe(nameof(IdentityKind.User));
                error.GetDetails().Values.Select(static value => value?.ToString() ?? string.Empty).ShouldAllBe(value => !value.Contains(Sentinel));
            });
        }
    }

    // ── Test request types ────────────────────────────────────────────────

    private sealed record UnauthorizedRequest : ICommand<Unit>;

    [Authorize]
    private sealed record AuthorizedRequest : ICommand<Unit>;

    [Authorize(Roles = "Admin")]
    internal sealed record AdminOnlyRequest : ICommand<Unit>;

    [Authorize(Roles = "Admin,Manager,Supervisor")]
    private sealed record MultiRoleRequest : ICommand<Unit>;

    [Authorize(Policy = "RequireElevation")]
    internal sealed record PolicyProtectedRequest : ICommand<Unit>;

    [Authorize(Roles = "Admin")]
    [Authorize(Policy = "RequireApproval")]
    private sealed record MultipleRequirementsRequest : ICommand<Unit>;

    [AllowAnonymous]
    private sealed record PublicRequest : ICommand<Unit>;

    [Authorize(Roles = "Admin")]
    [AllowAnonymous]
    private sealed record MixedAuthRequest : ICommand<Unit>;

    private sealed record PlainCommand : ICommand<Unit>;

    private sealed record PlainQuery : IQuery<string>;

    [ResourceAuthorize("CanEditOrder")]
    internal sealed record ResourceProtectedCommand(string OrderId) : ICommand<Unit>;

    [Authorize(Roles = "Admin")]
    [ResourceAuthorize("CanEdit")]
    private sealed record AuthorizedResourceCommand : ICommand<Unit>;

    private static AuthorizationPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        IAuthorizationService? authorizationService = null,
        AuthorizationConfiguration? configuration = null,
        FakeLogger<AuthorizationPipelineBehavior<TRequest, TResponse>>? logger = null)
        where TRequest : IRequest<TResponse>
    {
        return new AuthorizationPipelineBehavior<TRequest, TResponse>(
            authorizationService ?? new TestAuthorizationService(shouldSucceed: true),
            Options.Create(configuration ?? new AuthorizationConfiguration()),
            (Microsoft.Extensions.Logging.ILogger<AuthorizationPipelineBehavior<TRequest, TResponse>>?)logger
                ?? NullLogger<AuthorizationPipelineBehavior<TRequest, TResponse>>.Instance);
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
/// Authorization service that captures the resource and the principal passed to it.
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

    /// <summary>Gets the principal of the last call.</summary>
    public ClaimsPrincipal? LastUser { get; private set; }

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        IEnumerable<IAuthorizationRequirement> requirements)
    {
        LastUser = user;
        _onAuthorize(resource);
        return Task.FromResult(Result());
    }

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        string policyName)
    {
        LastUser = user;
        _onAuthorize(resource);
        return Task.FromResult(Result());
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        // Not used in tests
        return Task.CompletedTask;
    }

    private AuthorizationResult Result() => _shouldSucceed
        ? AuthorizationResult.Success()
        : AuthorizationResult.Failed(AuthorizationFailure.Failed(new[] { new AuthorizationFailureReason(this, "Policy failed") }));
}
