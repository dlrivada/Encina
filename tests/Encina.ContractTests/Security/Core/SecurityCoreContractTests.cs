using System.Security.Claims;
using Encina.Security;
using Encina.Security.Health;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.ContractTests.Security.Core;

/// <summary>
/// Contract tests for <c>Encina.Security</c>: the real <see cref="SecurityContext"/>, evaluators,
/// accessor, pipeline behavior and registration keep their documented behavior, and the
/// authorization path stays fail closed when the security context is missing.
/// </summary>
[Trait("Category", "Contract")]
public sealed class SecurityCoreContractTests
{
    // -- ISecurityContext --

    [Fact]
    public void SecurityContext_FromPrincipal_MapsClaimsUsingOptionClaimTypes()
    {
        var options = new SecurityOptions
        {
            UserIdClaimType = "uid",
            RoleClaimType = "r",
            PermissionClaimType = "p",
            TenantIdClaimType = "tid"
        };
        var principal = Principal(
            new Claim("uid", "user-1"), new Claim("tid", "tenant-1"),
            new Claim("r", "Admin"), new Claim(ClaimTypes.Role, "Auditor"),
            new Claim("p", "orders:read"));

        var context = new SecurityContext(principal, options);

        context.IsAuthenticated.ShouldBeTrue();
        context.UserId.ShouldBe("user-1");
        context.TenantId.ShouldBe("tenant-1");
        context.User.ShouldBeSameAs(principal);
        context.Roles.ShouldBe(["Admin", "Auditor"], ignoreOrder: true);
        context.Permissions.ShouldBe(["orders:read"]);
        context.Roles.Contains("admin").ShouldBeTrue("roles are case-insensitive");
    }

    [Fact]
    public void SecurityContext_UserIdFallsBackToNameIdentifier()
    {
        var context = new SecurityContext(Principal(new Claim(ClaimTypes.NameIdentifier, "nid-7")));

        context.UserId.ShouldBe("nid-7");
    }

    [Fact]
    public void SecurityContext_Anonymous_IsUnauthenticatedAndEmpty()
    {
        var context = SecurityContext.Anonymous;

        context.IsAuthenticated.ShouldBeFalse();
        context.User.ShouldBeNull();
        context.UserId.ShouldBeNull();
        context.TenantId.ShouldBeNull();
        context.Roles.ShouldBeEmpty();
        context.Permissions.ShouldBeEmpty();
    }

    [Fact]
    public void SecurityContext_PrincipalWithoutAuthenticationType_IsNotAuthenticated()
    {
        var context = new SecurityContext(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "u1")])));

        context.IsAuthenticated.ShouldBeFalse();
        context.UserId.ShouldBe("u1");
    }

    // -- ISecurityContextAccessor --

    [Fact]
    public async Task Accessor_ResolvedFromRegistration_FlowsContextToChildrenWithoutLeakingBack()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ISecurityContextAccessor>();
        accessor.SecurityContext.ShouldBeNull("no context is ambient by default");

        var mine = new SecurityContext(Principal(new Claim("sub", "flow-user")));
        accessor.SecurityContext = mine;
        await Task.Yield();
        accessor.SecurityContext.ShouldBeSameAs(mine);

        // A child flow inherits the value but its own assignment never leaks back to the parent.
        var other = new SecurityContext(Principal(new Claim("sub", "child-user")));
        ISecurityContext? inherited = null;
        await Task.Run(() =>
        {
            inherited = accessor.SecurityContext;
            accessor.SecurityContext = other;
        });
        inherited.ShouldBeSameAs(mine);
        accessor.SecurityContext.ShouldBeSameAs(mine);

        accessor.SecurityContext = null;
    }

    // -- IPermissionEvaluator --

    [Fact]
    public async Task PermissionEvaluator_AnonymousContext_DeniesEverything()
    {
        var evaluator = new DefaultPermissionEvaluator();

        (await evaluator.HasPermissionAsync(SecurityContext.Anonymous, "orders:read")).ShouldBeFalse();
        (await evaluator.HasAnyPermissionAsync(SecurityContext.Anonymous, ["orders:read"])).ShouldBeFalse();
        (await evaluator.HasAllPermissionsAsync(SecurityContext.Anonymous, ["orders:read"])).ShouldBeFalse();
    }

    [Fact]
    public async Task PermissionEvaluator_HonorsAnyAndAllSemantics()
    {
        var evaluator = new DefaultPermissionEvaluator();
        var context = new SecurityContext(Principal(new Claim("permission", "a"), new Claim("permission", "B")));

        (await evaluator.HasPermissionAsync(context, "b")).ShouldBeTrue("permissions are case-insensitive");
        (await evaluator.HasAnyPermissionAsync(context, ["x", "a"])).ShouldBeTrue();
        (await evaluator.HasAnyPermissionAsync(context, ["x", "y"])).ShouldBeFalse();
        (await evaluator.HasAllPermissionsAsync(context, ["a", "b"])).ShouldBeTrue();
        (await evaluator.HasAllPermissionsAsync(context, ["a", "x"])).ShouldBeFalse();
    }

    // -- IResourceOwnershipEvaluator --

    [Fact]
    public async Task OwnershipEvaluator_ContextWithoutUserId_IsNeverOwner()
    {
        var evaluator = new DefaultResourceOwnershipEvaluator();
        var resource = new Owned("");

        (await evaluator.IsOwnerAsync(SecurityContext.Anonymous, resource, nameof(Owned.OwnerId))).ShouldBeFalse();
    }

    [Fact]
    public async Task OwnershipEvaluator_MatchesOnlyExactPublicPropertyValue()
    {
        var evaluator = new DefaultResourceOwnershipEvaluator();
        var context = new SecurityContext(Principal(new Claim("sub", "u1")));

        (await evaluator.IsOwnerAsync(context, new Owned("u1"), nameof(Owned.OwnerId))).ShouldBeTrue();
        (await evaluator.IsOwnerAsync(context, new Owned("U1"), nameof(Owned.OwnerId))).ShouldBeFalse("comparison is ordinal");
        (await evaluator.IsOwnerAsync(context, new Owned(null), nameof(Owned.OwnerId))).ShouldBeFalse();
        (await evaluator.IsOwnerAsync(context, new Owned("u1"), "DoesNotExist")).ShouldBeFalse("unknown property denies");
    }

    // -- SecurityErrors --

    [Fact]
    public void SecurityErrors_UseStableCodesAndSecurityStage()
    {
        var type = typeof(SecuredByRole);
        var errors = new (EncinaError Error, string Code)[]
        {
            (SecurityErrors.Unauthenticated(type), EncinaErrorCodes.AuthorizationUnauthenticated),
            (SecurityErrors.MissingContext(type), EncinaErrorCodes.AuthorizationUnauthenticated),
            (SecurityErrors.InsufficientRoles(type, ["Admin"], "u1"), EncinaErrorCodes.AuthorizationInsufficientRoles),
            (SecurityErrors.PermissionDenied(type, ["p"], "u1"), EncinaErrorCodes.AuthorizationPermissionDenied),
            (SecurityErrors.ClaimMissing(type, "c", "v", "u1"), EncinaErrorCodes.AuthorizationClaimMissing),
            (SecurityErrors.ClaimMissing(type, "c", null, "u1"), EncinaErrorCodes.AuthorizationClaimMissing),
            (SecurityErrors.NotOwner(type, "OwnerId", "u1"), EncinaErrorCodes.AuthorizationNotOwner)
        };

        foreach (var (error, code) in errors)
        {
            error.GetCode().IfNone("").ShouldBe(code);
            var details = error.GetDetails();
            details["stage"].ShouldBe("security");
            details["requestType"].ShouldBe(type.FullName);
        }
    }

    [Fact]
    public void SecurityErrors_RequireAllVariants_ReportAllRequirement()
    {
        var type = typeof(SecuredByRole);

        SecurityErrors.InsufficientRoles(type, ["a"], null, requireAll: true).GetDetails()["requirement"].ShouldBe("all_roles");
        SecurityErrors.InsufficientRoles(type, ["a"], null).GetDetails()["requirement"].ShouldBe("any_role");
        SecurityErrors.PermissionDenied(type, ["a"], null, requireAll: true).GetDetails()["requirement"].ShouldBe("all_permissions");
        SecurityErrors.PermissionDenied(type, ["a"], null).GetDetails()["requirement"].ShouldBe("any_permission");
    }

    // -- Fail-closed pipeline contract --

    [Fact]
    public async Task Pipeline_MissingContext_WithRoleRequirement_IsDeniedEvenWithoutThrowOption()
    {
        var accessor = new FixedAccessor(null);

        var result = await Run<SecuredByRole>(accessor, new SecurityOptions());

        AssertDenied(result, EncinaErrorCodes.AuthorizationInsufficientRoles);
    }

    [Fact]
    public async Task Pipeline_MissingContext_WithDenyAnonymous_IsDeniedAsUnauthenticated()
    {
        var result = await Run<SecuredDenyAnonymous>(new FixedAccessor(null), new SecurityOptions());

        AssertDenied(result, EncinaErrorCodes.AuthorizationUnauthenticated);
    }

    [Fact]
    public async Task Pipeline_MissingContext_WithThrowOption_ReturnsMissingContextError()
    {
        var result = await Run<SecuredByRole>(
            new FixedAccessor(null), new SecurityOptions { ThrowOnMissingSecurityContext = true });

        var error = AssertDenied(result, EncinaErrorCodes.AuthorizationUnauthenticated);
        error.GetDetails()["requirement"].ShouldBe("security_context");
    }

    [Fact]
    public async Task Pipeline_MissingContext_WithPermissionRequirement_IsDenied()
    {
        var result = await Run<SecuredByPermission>(new FixedAccessor(null), new SecurityOptions());

        AssertDenied(result, EncinaErrorCodes.AuthorizationPermissionDenied);
    }

    [Fact]
    public async Task Pipeline_MissingContext_WithOwnershipRequirement_IsDenied()
    {
        var result = await Run<SecuredByOwnership>(new FixedAccessor(null), new SecurityOptions());

        AssertDenied(result, EncinaErrorCodes.AuthorizationNotOwner);
    }

    [Fact]
    public async Task Pipeline_MissingContext_WithRequireAuthenticatedByDefault_IsDeniedWithoutAttributes()
    {
        var result = await Run<Unsecured>(
            new FixedAccessor(null), new SecurityOptions { RequireAuthenticatedByDefault = true, ThrowOnMissingSecurityContext = true });

        AssertDenied(result, EncinaErrorCodes.AuthorizationUnauthenticated);
    }

    [Fact]
    public async Task Pipeline_MissingContext_WithRequireAuthenticatedByDefaultOnly_IsDeniedAsAnonymous()
    {
        var result = await Run<Unsecured>(
            new FixedAccessor(null), new SecurityOptions { RequireAuthenticatedByDefault = true });

        var error = AssertDenied(result, EncinaErrorCodes.AuthorizationUnauthenticated);
        error.GetDetails()["requirement"].ShouldBe("authenticated");
    }

    [Fact]
    public async Task Pipeline_MissingContext_WithClaimRequirement_IsDenied()
    {
        var result = await Run<SecuredByClaim>(new FixedAccessor(null), new SecurityOptions());

        AssertDenied(result, EncinaErrorCodes.AuthorizationClaimMissing);
    }

    [Fact]
    public async Task Pipeline_AllowAnonymousRequest_PassesWithoutContext()
    {
        var result = await Run<OpenRequest>(new FixedAccessor(null), new SecurityOptions { RequireAuthenticatedByDefault = true });

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Pipeline_AuthorizedContext_ReachesHandler()
    {
        var context = new SecurityContext(Principal(new Claim("sub", "u1"), new Claim("role", "Admin")));

        var result = await Run<SecuredByRole>(new FixedAccessor(context), new SecurityOptions());

        result.IsRight.ShouldBeTrue();
    }

    // -- Registration and health --

    [Fact]
    public void AddEncinaSecurity_RegistersDefaultsAndBehavior_ValidatingOnBuild()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISecurityContextAccessor>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>().ShouldBeOfType<DefaultPermissionEvaluator>();
        scope.ServiceProvider.GetRequiredService<IResourceOwnershipEvaluator>().ShouldBeOfType<DefaultResourceOwnershipEvaluator>();
        scope.ServiceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value.RequireAuthenticatedByDefault.ShouldBeFalse();
        scope.ServiceProvider.GetServices<IPipelineBehavior<SecuredByRole, Unit>>()
            .ShouldContain(b => b is SecurityPipelineBehavior<SecuredByRole, Unit>);
    }

    [Fact]
    public void AddEncinaSecurity_KeepsApplicationRegisteredEvaluator()
    {
        var custom = new CustomPermissionEvaluator();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPermissionEvaluator>(custom);
        services.AddEncinaSecurity();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>().ShouldBeSameAs(custom);
    }

    [Fact]
    public async Task HealthCheck_WhenRequiredServicesRegistered_IsHealthy()
    {
        using var provider = BuildProvider(o => o.AddHealthCheck = true);
        var check = new SecurityHealthCheck(provider);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task HealthCheck_WhenServicesMissing_IsUnhealthyAndNamesThem()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var check = new SecurityHealthCheck(provider);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain(nameof(ISecurityContextAccessor));
        result.Description.ShouldContain(nameof(IPermissionEvaluator));
        result.Description.ShouldContain(nameof(IResourceOwnershipEvaluator));
    }

    [Fact]
    public async Task HealthCheck_WhenProviderThrows_ReportsOnlyExceptionType()
    {
        var check = new SecurityHealthCheck(new ThrowingServiceProvider());

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain(nameof(InvalidOperationException));
        result.Description.ShouldNotContain("secret-detail");
    }

    // -- helpers --

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "test"));

    private static ServiceProvider BuildProvider(Action<SecurityOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaSecurity(configure);
        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static ValueTask<Either<EncinaError, Unit>> Run<TRequest>(FixedAccessor accessor, SecurityOptions options)
        where TRequest : ICommand<Unit>, new()
    {
        var behavior = new SecurityPipelineBehavior<TRequest, Unit>(
            accessor,
            new DefaultPermissionEvaluator(),
            new DefaultResourceOwnershipEvaluator(),
            Options.Create(options),
            NullLogger<SecurityPipelineBehavior<TRequest, Unit>>.Instance);

        return behavior.Handle(
            new TRequest(),
            RequestContext.CreateForTest(),
            () => new ValueTask<Either<EncinaError, Unit>>(Unit.Default),
            CancellationToken.None);
    }

    private static EncinaError AssertDenied(Either<EncinaError, Unit> result, string expectedCode)
    {
        result.IsLeft.ShouldBeTrue("a missing or insufficient security context must deny");
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").ShouldBe(expectedCode);
        return error;
    }

    private sealed class FixedAccessor(ISecurityContext? context) : ISecurityContextAccessor
    {
        public ISecurityContext? SecurityContext { get; set; } = context;
    }

    private sealed class CustomPermissionEvaluator : IPermissionEvaluator
    {
        public ValueTask<bool> HasPermissionAsync(ISecurityContext context, string permission, CancellationToken cancellationToken = default) => new(true);

        public ValueTask<bool> HasAnyPermissionAsync(ISecurityContext context, IEnumerable<string> permissions, CancellationToken cancellationToken = default) => new(true);

        public ValueTask<bool> HasAllPermissionsAsync(ISecurityContext context, IEnumerable<string> permissions, CancellationToken cancellationToken = default) => new(true);
    }

    private sealed class ThrowingServiceProvider : IServiceProvider, IServiceScopeFactory
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IServiceScopeFactory)
                ? throw new InvalidOperationException("secret-detail")
                : null;

        public IServiceScope CreateScope() => throw new InvalidOperationException("secret-detail");
    }

    private sealed record Owned(string? OwnerId);

    [RequireRole("Admin")]
    private sealed class SecuredByRole : ICommand<Unit>;

    [DenyAnonymous]
    private sealed class SecuredDenyAnonymous : ICommand<Unit>;

    [RequirePermission("orders:write")]
    private sealed class SecuredByPermission : ICommand<Unit>;

    [RequireOwnership("OwnerId")]
    private sealed class SecuredByOwnership : ICommand<Unit>
    {
        public string OwnerId { get; init; } = "someone";
    }

    [RequireClaim("flag", "on")]
    private sealed class SecuredByClaim : ICommand<Unit>;

    private sealed class Unsecured : ICommand<Unit>;

    [AllowAnonymous]
    private sealed class OpenRequest : ICommand<Unit>;
}
