using System.Security.Claims;
using Encina.Security;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.GuardTests.Security.Core;

/// <summary>
/// Guard clause tests for the public surface of <c>Encina.Security</c> (the core package):
/// the default evaluators, the pipeline behavior and the registration extension.
/// Every case executes the real implementation.
/// </summary>
public sealed class SecurityCoreGuardTests
{
    private static readonly ISecurityContext Context = SecurityContext.Anonymous;

    // -- DefaultPermissionEvaluator --

    [Fact]
    public async Task HasPermissionAsync_NullContext_ThrowsArgumentNullException()
    {
        var evaluator = new DefaultPermissionEvaluator();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await evaluator.HasPermissionAsync(null!, "orders:read"));

        ex.ParamName.ShouldBe("context");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HasPermissionAsync_NullOrBlankPermission_Throws(string? permission)
    {
        var evaluator = new DefaultPermissionEvaluator();

        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await evaluator.HasPermissionAsync(Context, permission!));

        ex.ParamName.ShouldBe("permission");
    }

    [Fact]
    public async Task HasAnyPermissionAsync_NullContext_ThrowsArgumentNullException()
    {
        var evaluator = new DefaultPermissionEvaluator();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await evaluator.HasAnyPermissionAsync(null!, ["a"]));

        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task HasAnyPermissionAsync_NullPermissions_ThrowsArgumentNullException()
    {
        var evaluator = new DefaultPermissionEvaluator();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await evaluator.HasAnyPermissionAsync(Context, null!));

        ex.ParamName.ShouldBe("permissions");
    }

    [Fact]
    public async Task HasAllPermissionsAsync_NullContext_ThrowsArgumentNullException()
    {
        var evaluator = new DefaultPermissionEvaluator();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await evaluator.HasAllPermissionsAsync(null!, ["a"]));

        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task HasAllPermissionsAsync_NullPermissions_ThrowsArgumentNullException()
    {
        var evaluator = new DefaultPermissionEvaluator();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await evaluator.HasAllPermissionsAsync(Context, null!));

        ex.ParamName.ShouldBe("permissions");
    }

    // -- DefaultResourceOwnershipEvaluator --

    [Fact]
    public async Task IsOwnerAsync_NullContext_ThrowsArgumentNullException()
    {
        var evaluator = new DefaultResourceOwnershipEvaluator();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await evaluator.IsOwnerAsync(null!, new Resource("u"), nameof(Resource.OwnerId)));

        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task IsOwnerAsync_NullResource_ThrowsArgumentNullException()
    {
        var evaluator = new DefaultResourceOwnershipEvaluator();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await evaluator.IsOwnerAsync<Resource>(Context, null!, nameof(Resource.OwnerId)));

        ex.ParamName.ShouldBe("resource");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task IsOwnerAsync_NullOrBlankPropertyName_Throws(string? propertyName)
    {
        var evaluator = new DefaultResourceOwnershipEvaluator();

        var ex = await Should.ThrowAsync<ArgumentException>(
            async () => await evaluator.IsOwnerAsync(Context, new Resource("u"), propertyName!));

        ex.ParamName.ShouldBe("propertyName");
    }

    // -- SecurityPipelineBehavior --

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var behavior = new SecurityPipelineBehavior<GuardCommand, Unit>(
            new SecurityContextAccessorStub(),
            new DefaultPermissionEvaluator(),
            new DefaultResourceOwnershipEvaluator(),
            Options.Create(new SecurityOptions()),
            NullLogger<SecurityPipelineBehavior<GuardCommand, Unit>>.Instance);

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(
                null!,
                RequestContext.CreateForTest(),
                () => new ValueTask<Either<EncinaError, Unit>>(Unit.Default),
                CancellationToken.None));

        ex.ParamName.ShouldBe("request");
    }

    // -- ServiceCollectionExtensions --

    [Fact]
    public void AddEncinaSecurity_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var ex = Should.Throw<ArgumentNullException>(() => services.AddEncinaSecurity());

        ex.ParamName.ShouldBe("services");
    }

    // -- Boundary inputs that must be accepted (the guards reject only what they document) --

    [Fact]
    public void SecurityContext_NullPrincipal_IsAcceptedAsAnonymous()
    {
        var context = new SecurityContext(principal: null, options: null);

        context.IsAuthenticated.ShouldBeFalse();
        context.UserId.ShouldBeNull();
    }

    [Fact]
    public void SecurityContext_PrincipalWithoutClaims_HasEmptyRolesAndPermissions()
    {
        var context = new SecurityContext(new ClaimsPrincipal(new ClaimsIdentity()));

        context.Roles.ShouldBeEmpty();
        context.Permissions.ShouldBeEmpty();
        context.TenantId.ShouldBeNull();
    }

    [Fact]
    public async Task Evaluators_AcceptValidArguments_AndDenyAnonymousCallers()
    {
        var permissions = new DefaultPermissionEvaluator();
        var ownership = new DefaultResourceOwnershipEvaluator();

        (await permissions.HasPermissionAsync(Context, "p")).ShouldBeFalse();
        (await permissions.HasAnyPermissionAsync(Context, [])).ShouldBeFalse();
        (await permissions.HasAllPermissionsAsync(Context, ["p"])).ShouldBeFalse();
        (await ownership.IsOwnerAsync(Context, new Resource("u"), nameof(Resource.OwnerId))).ShouldBeFalse();
    }

    [Theory]
    [InlineData(typeof(PlainCommand))]
    [InlineData(typeof(RoleCommand))]
    [InlineData(typeof(AllRolesCommand))]
    [InlineData(typeof(PermissionCommand))]
    [InlineData(typeof(ClaimCommand))]
    [InlineData(typeof(ClaimValueCommand))]
    [InlineData(typeof(OwnershipCommand))]
    [InlineData(typeof(DenyAnonymousCommand))]
    public async Task Handle_AnonymousCaller_NeverThrowsAndFailsClosedWhenSecured(Type requestType)
    {
        var result = await (Task<Either<EncinaError, Unit>>)typeof(SecurityCoreGuardTests)
            .GetMethod(nameof(RunAnonymousAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(requestType)
            .Invoke(null, null)!;

        result.IsLeft.ShouldBe(requestType != typeof(PlainCommand));
    }

    [Fact]
    public async Task Handle_AuthenticatedCallerWithEveryRequirement_ReachesHandler()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "u1"), new Claim("role", "Admin"), new Claim("permission", "orders:write"),
                new Claim("flag", "on")
            ],
            authenticationType: "test"));
        var accessor = new SecurityContextAccessorStub { SecurityContext = new SecurityContext(principal) };

        var role = await CreateBehavior<RoleCommand>(accessor).Handle(new RoleCommand(), RequestContext.CreateForTest(), Next, CancellationToken.None);
        var allRoles = await CreateBehavior<AllRolesCommand>(accessor).Handle(new AllRolesCommand(), RequestContext.CreateForTest(), Next, CancellationToken.None);
        var permission = await CreateBehavior<PermissionCommand>(accessor).Handle(new PermissionCommand(), RequestContext.CreateForTest(), Next, CancellationToken.None);
        var claim = await CreateBehavior<ClaimCommand>(accessor).Handle(new ClaimCommand(), RequestContext.CreateForTest(), Next, CancellationToken.None);
        var claimValue = await CreateBehavior<ClaimValueCommand>(accessor).Handle(new ClaimValueCommand(), RequestContext.CreateForTest(), Next, CancellationToken.None);
        var owner = await CreateBehavior<OwnershipCommand>(accessor).Handle(new OwnershipCommand("u1"), RequestContext.CreateForTest(), Next, CancellationToken.None);
        var denyAnonymous = await CreateBehavior<DenyAnonymousCommand>(accessor).Handle(new DenyAnonymousCommand(), RequestContext.CreateForTest(), Next, CancellationToken.None);

        new[] { role, allRoles, permission, claim, claimValue, owner, denyAnonymous }
            .ShouldAllBe(r => r.IsRight);
    }

    [Fact]
    public void SecurityErrors_AcceptEmptyCollectionsAndNullUser()
    {
        SecurityErrors.InsufficientRoles(typeof(RoleCommand), [], null).GetCode().IsSome.ShouldBeTrue();
        SecurityErrors.PermissionDenied(typeof(PermissionCommand), [], null, requireAll: true).GetCode().IsSome.ShouldBeTrue();
        SecurityErrors.NotOwner(typeof(OwnershipCommand), "OwnerId", null).GetCode().IsSome.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaSecurity_ValidArguments_RegistersServicesAndOptionalHealthCheck()
    {
        var services = new ServiceCollection();

        var returned = services.AddEncinaSecurity(o => o.AddHealthCheck = true);

        returned.ShouldBeSameAs(services);
        services.ShouldContain(d => d.ServiceType == typeof(IPermissionEvaluator));
        services.ShouldContain(d => d.ServiceType == typeof(IResourceOwnershipEvaluator));
        services.ShouldContain(d => d.ServiceType == typeof(ISecurityContextAccessor));
    }

    private static ValueTask<Either<EncinaError, Unit>> Next() => new(Unit.Default);

    private static SecurityPipelineBehavior<TRequest, Unit> CreateBehavior<TRequest>(ISecurityContextAccessor accessor)
        where TRequest : IRequest<Unit> =>
        new(
            accessor,
            new DefaultPermissionEvaluator(),
            new DefaultResourceOwnershipEvaluator(),
            Options.Create(new SecurityOptions()),
            NullLogger<SecurityPipelineBehavior<TRequest, Unit>>.Instance);

    private static async Task<Either<EncinaError, Unit>> RunAnonymousAsync<TRequest>()
        where TRequest : class, IRequest<Unit>
    {
        var request = typeof(TRequest) == typeof(OwnershipCommand)
            ? (TRequest)(object)new OwnershipCommand("someone")
            : Activator.CreateInstance<TRequest>();

        return await CreateBehavior<TRequest>(new SecurityContextAccessorStub())
            .Handle(request, RequestContext.CreateForTest(), Next, CancellationToken.None);
    }

    private sealed record Resource(string OwnerId);

    private sealed class GuardCommand : ICommand<Unit>;

    private sealed class PlainCommand : ICommand<Unit>;

    [RequireRole("Admin")]
    private sealed class RoleCommand : ICommand<Unit>;

    [RequireAllRoles("Admin")]
    private sealed class AllRolesCommand : ICommand<Unit>;

    [RequirePermission("orders:write", RequireAll = true)]
    private sealed class PermissionCommand : ICommand<Unit>;

    [RequireClaim("flag")]
    private sealed class ClaimCommand : ICommand<Unit>;

    [RequireClaim("flag", "on")]
    private sealed class ClaimValueCommand : ICommand<Unit>;

    [RequireOwnership("OwnerId")]
    private sealed class OwnershipCommand(string ownerId) : ICommand<Unit>
    {
        public string OwnerId { get; } = ownerId;
    }

    [DenyAnonymous]
    private sealed class DenyAnonymousCommand : ICommand<Unit>;

    private sealed class SecurityContextAccessorStub : ISecurityContextAccessor
    {
        public ISecurityContext? SecurityContext { get; set; }
    }
}
