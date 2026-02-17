using System.Security.Claims;
using Encina.Security;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security;

/// <summary>
/// Unit tests for <see cref="SecurityPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class SecurityPipelineBehaviorTests
{
    private readonly ISecurityContextAccessor _accessor;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IResourceOwnershipEvaluator _ownershipEvaluator;
    private readonly ILogger<SecurityPipelineBehavior<NoAttributeCommand, Unit>> _logger;

    public SecurityPipelineBehaviorTests()
    {
        _accessor = Substitute.For<ISecurityContextAccessor>();
        _permissionEvaluator = Substitute.For<IPermissionEvaluator>();
        _ownershipEvaluator = Substitute.For<IResourceOwnershipEvaluator>();
        _logger = Substitute.For<ILogger<SecurityPipelineBehavior<NoAttributeCommand, Unit>>>();
    }

    // -- No attributes --

    [Fact]
    public async Task Handle_NoAttributes_ShouldPassthrough()
    {
        // Arrange
        var behavior = CreateBehavior<NoAttributeCommand, Unit>();
        var request = new NoAttributeCommand();

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // -- AllowAnonymous --

    [Fact]
    public async Task Handle_AllowAnonymous_ShouldBypassAllChecks()
    {
        // Arrange
        var behavior = CreateBehavior<AllowAnonymousCommand, Unit>();
        var request = new AllowAnonymousCommand();
        _accessor.SecurityContext.Returns((ISecurityContext?)null);

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // -- DenyAnonymous --

    [Fact]
    public async Task Handle_DenyAnonymous_Authenticated_ShouldAllow()
    {
        // Arrange
        var behavior = CreateBehavior<DenyAnonymousCommand, Unit>();
        var request = new DenyAnonymousCommand();
        _accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1"));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DenyAnonymous_NotAuthenticated_ShouldReturnUnauthenticatedError()
    {
        // Arrange
        var behavior = CreateBehavior<DenyAnonymousCommand, Unit>();
        var request = new DenyAnonymousCommand();
        _accessor.SecurityContext.Returns(SecurityContext.Anonymous);

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").Should().Be(SecurityErrors.UnauthenticatedCode);
    }

    // -- RequireRole --

    [Fact]
    public async Task Handle_RequireRole_WithMatchingRole_ShouldAllow()
    {
        // Arrange
        var behavior = CreateBehavior<RequireRoleCommand, Unit>();
        var request = new RequireRoleCommand();
        _accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1", roles: ["Admin"]));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RequireRole_WithoutMatchingRole_ShouldReturnInsufficientRoles()
    {
        // Arrange
        var behavior = CreateBehavior<RequireRoleCommand, Unit>();
        var request = new RequireRoleCommand();
        _accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1", roles: ["Editor"]));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").Should().Be(SecurityErrors.InsufficientRolesCode);
    }

    [Fact]
    public async Task Handle_RequireRole_AnyMatching_ShouldAllow()
    {
        // Arrange — RequireRoleCommand requires "Admin" or "Manager"
        var behavior = CreateBehavior<RequireRoleCommand, Unit>();
        var request = new RequireRoleCommand();
        _accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1", roles: ["Manager"]));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // -- RequireAllRoles --

    [Fact]
    public async Task Handle_RequireAllRoles_WithAllRoles_ShouldAllow()
    {
        // Arrange
        var behavior = CreateBehavior<RequireAllRolesCommand, Unit>();
        var request = new RequireAllRolesCommand();
        _accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1", roles: ["Admin", "Manager"]));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RequireAllRoles_WithPartialRoles_ShouldReturnError()
    {
        // Arrange
        var behavior = CreateBehavior<RequireAllRolesCommand, Unit>();
        var request = new RequireAllRolesCommand();
        _accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1", roles: ["Admin"]));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").Should().Be(SecurityErrors.InsufficientRolesCode);
    }

    // -- RequirePermission --

    [Fact]
    public async Task Handle_RequirePermission_WithPermission_ShouldAllow()
    {
        // Arrange
        var behavior = CreateBehavior<RequirePermissionCommand, Unit>();
        var request = new RequirePermissionCommand();
        var ctx = CreateAuthenticatedContext("user-1");
        _accessor.SecurityContext.Returns(ctx);

#pragma warning disable CA2012
        _permissionEvaluator
            .HasAnyPermissionAsync(ctx, Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));
#pragma warning restore CA2012

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RequirePermission_WithoutPermission_ShouldReturnError()
    {
        // Arrange
        var behavior = CreateBehavior<RequirePermissionCommand, Unit>();
        var request = new RequirePermissionCommand();
        var ctx = CreateAuthenticatedContext("user-1");
        _accessor.SecurityContext.Returns(ctx);

#pragma warning disable CA2012
        _permissionEvaluator
            .HasAnyPermissionAsync(ctx, Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(false));
#pragma warning restore CA2012

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").Should().Be(SecurityErrors.PermissionDeniedCode);
    }

    [Fact]
    public async Task Handle_RequirePermission_RequireAll_ShouldCallHasAllPermissions()
    {
        // Arrange
        var behavior = CreateBehavior<RequireAllPermissionsCommand, Unit>();
        var request = new RequireAllPermissionsCommand();
        var ctx = CreateAuthenticatedContext("user-1");
        _accessor.SecurityContext.Returns(ctx);

#pragma warning disable CA2012
        _permissionEvaluator
            .HasAllPermissionsAsync(ctx, Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));
#pragma warning restore CA2012

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
#pragma warning disable CA2012
        await _permissionEvaluator.Received(1)
            .HasAllPermissionsAsync(ctx, Arg.Any<string[]>(), Arg.Any<CancellationToken>());
#pragma warning restore CA2012
    }

    // -- RequireClaim --

    [Fact]
    public async Task Handle_RequireClaim_TypeOnly_WithClaimPresent_ShouldAllow()
    {
        // Arrange
        var behavior = CreateBehavior<RequireClaimTypeOnlyCommand, Unit>();
        var request = new RequireClaimTypeOnlyCommand();
        _accessor.SecurityContext.Returns(
            CreateAuthenticatedContext("user-1", claims: [("department", "engineering")]));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RequireClaim_TypeAndValue_WithExactMatch_ShouldAllow()
    {
        // Arrange
        var behavior = CreateBehavior<RequireClaimWithValueCommand, Unit>();
        var request = new RequireClaimWithValueCommand();
        _accessor.SecurityContext.Returns(
            CreateAuthenticatedContext("user-1", claims: [("department", "engineering")]));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RequireClaim_WithWrongValue_ShouldReturnError()
    {
        // Arrange
        var behavior = CreateBehavior<RequireClaimWithValueCommand, Unit>();
        var request = new RequireClaimWithValueCommand();
        _accessor.SecurityContext.Returns(
            CreateAuthenticatedContext("user-1", claims: [("department", "marketing")]));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").Should().Be(SecurityErrors.ClaimMissingCode);
    }

    [Fact]
    public async Task Handle_RequireClaim_WithMissingClaim_ShouldReturnError()
    {
        // Arrange
        var behavior = CreateBehavior<RequireClaimTypeOnlyCommand, Unit>();
        var request = new RequireClaimTypeOnlyCommand();
        _accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1"));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").Should().Be(SecurityErrors.ClaimMissingCode);
    }

    // -- RequireOwnership --

    [Fact]
    public async Task Handle_RequireOwnership_CorrectOwner_ShouldAllow()
    {
        // Arrange
        var behavior = CreateBehavior<RequireOwnershipCommand, Unit>();
        var request = new RequireOwnershipCommand { OwnerId = "user-1" };
        var ctx = CreateAuthenticatedContext("user-1");
        _accessor.SecurityContext.Returns(ctx);

#pragma warning disable CA2012
        _ownershipEvaluator
            .IsOwnerAsync(ctx, request, "OwnerId", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));
#pragma warning restore CA2012

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RequireOwnership_WrongOwner_ShouldReturnError()
    {
        // Arrange
        var behavior = CreateBehavior<RequireOwnershipCommand, Unit>();
        var request = new RequireOwnershipCommand { OwnerId = "other-user" };
        var ctx = CreateAuthenticatedContext("user-1");
        _accessor.SecurityContext.Returns(ctx);

#pragma warning disable CA2012
        _ownershipEvaluator
            .IsOwnerAsync(ctx, request, "OwnerId", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(false));
#pragma warning restore CA2012

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").Should().Be(SecurityErrors.NotOwnerCode);
    }

    // -- RequireAuthenticatedByDefault --

    [Fact]
    public async Task Handle_RequireAuthByDefault_NoAttributes_Authenticated_ShouldAllow()
    {
        // Arrange
        var behavior = CreateBehavior<NoAttributeCommand, Unit>(requireAuthByDefault: true);
        var request = new NoAttributeCommand();
        _accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1"));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RequireAuthByDefault_NoAttributes_Anonymous_ShouldReturnError()
    {
        // Arrange
        var behavior = CreateBehavior<NoAttributeCommand, Unit>(requireAuthByDefault: true);
        var request = new NoAttributeCommand();
        _accessor.SecurityContext.Returns(SecurityContext.Anonymous);

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").Should().Be(SecurityErrors.UnauthenticatedCode);
    }

    // -- Missing security context --

    [Fact]
    public async Task Handle_NullContext_ThrowOnMissing_ShouldReturnMissingContextError()
    {
        // Arrange
        var options = new SecurityOptions { ThrowOnMissingSecurityContext = true };
        var behavior = CreateBehavior<DenyAnonymousCommand, Unit>(options);
        var request = new DenyAnonymousCommand();
        _accessor.SecurityContext.Returns((ISecurityContext?)null);

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").Should().Be(SecurityErrors.MissingContextCode);
    }

    [Fact]
    public async Task Handle_NullContext_NoThrow_ShouldTreatAsAnonymous()
    {
        // Arrange — DenyAnonymous with anonymous context should fail with unauthenticated
        var options = new SecurityOptions { ThrowOnMissingSecurityContext = false };
        var behavior = CreateBehavior<DenyAnonymousCommand, Unit>(options);
        var request = new DenyAnonymousCommand();
        _accessor.SecurityContext.Returns((ISecurityContext?)null);

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").Should().Be(SecurityErrors.UnauthenticatedCode);
    }

    // -- Multiple attributes combined --

    [Fact]
    public async Task Handle_MultipleAttributes_ShouldShortCircuitOnFirstFailure()
    {
        // Arrange — DenyAnonymous + RequireRole, anonymous user should fail on DenyAnonymous
        var behavior = CreateBehavior<AuthAndRoleCommand, Unit>();
        var request = new AuthAndRoleCommand();
        _accessor.SecurityContext.Returns(SecurityContext.Anonymous);

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert — Should fail on DenyAnonymous, not RequireRole
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        error.GetCode().IfNone("").Should().Be(SecurityErrors.UnauthenticatedCode);
    }

    [Fact]
    public async Task Handle_MultipleAttributes_AllPass_ShouldAllow()
    {
        // Arrange
        var behavior = CreateBehavior<AuthAndRoleCommand, Unit>();
        var request = new AuthAndRoleCommand();
        _accessor.SecurityContext.Returns(CreateAuthenticatedContext("user-1", roles: ["Admin"]));

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // -- Error metadata --

    [Fact]
    public async Task Handle_DenyAnonymous_Error_ShouldContainRequestTypeMetadata()
    {
        // Arrange
        var behavior = CreateBehavior<DenyAnonymousCommand, Unit>();
        var request = new DenyAnonymousCommand();
        _accessor.SecurityContext.Returns(SecurityContext.Anonymous);

        // Act
        var result = await behavior.Handle(
            request, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.Match(_ => default, e => e);
        var details = error.GetDetails();
        details.Should().ContainKey("requestType");
        details["requestType"].Should().Be(typeof(DenyAnonymousCommand).FullName);
    }

    // -- Null request guard --

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var behavior = CreateBehavior<NoAttributeCommand, Unit>();

        // Act
        Func<Task> act = async () => await behavior.Handle(
            null!, RequestContext.CreateForTest(), Next(Unit.Default), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    #region Helpers

    private SecurityPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        bool requireAuthByDefault = false)
        where TRequest : IRequest<TResponse>
    {
        var options = new SecurityOptions { RequireAuthenticatedByDefault = requireAuthByDefault };
        return CreateBehavior<TRequest, TResponse>(options);
    }

    private SecurityPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        SecurityOptions options)
        where TRequest : IRequest<TResponse>
    {
        var logger = Substitute.For<ILogger<SecurityPipelineBehavior<TRequest, TResponse>>>();
        return new SecurityPipelineBehavior<TRequest, TResponse>(
            _accessor,
            _permissionEvaluator,
            _ownershipEvaluator,
            Options.Create(options),
            logger);
    }

    private static RequestHandlerCallback<TResponse> Next<TResponse>(TResponse value)
        => () => new ValueTask<Either<EncinaError, TResponse>>(value);

    private static SecurityContext CreateAuthenticatedContext(
        string userId,
        string[]? roles = null,
        string[]? permissions = null,
        (string Type, string Value)[]? claims = null)
    {
        var claimList = new List<Claim> { new("sub", userId) };

        if (roles is not null)
        {
            foreach (var role in roles)
                claimList.Add(new Claim("role", role));
        }

        if (permissions is not null)
        {
            foreach (var perm in permissions)
                claimList.Add(new Claim("permission", perm));
        }

        if (claims is not null)
        {
            foreach (var (type, value) in claims)
                claimList.Add(new Claim(type, value));
        }

        var identity = new ClaimsIdentity(claimList, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        return new SecurityContext(principal);
    }

    #endregion

    #region Test Request Types

    public sealed class NoAttributeCommand : ICommand<Unit> { }

    [AllowAnonymous]
    public sealed class AllowAnonymousCommand : ICommand<Unit> { }

    [DenyAnonymous]
    public sealed class DenyAnonymousCommand : ICommand<Unit> { }

    [RequireRole("Admin", "Manager")]
    public sealed class RequireRoleCommand : ICommand<Unit> { }

    [RequireAllRoles("Admin", "Manager")]
    public sealed class RequireAllRolesCommand : ICommand<Unit> { }

    [RequirePermission("orders:read")]
    public sealed class RequirePermissionCommand : ICommand<Unit> { }

    [RequirePermission("orders:read", "orders:write", RequireAll = true)]
    public sealed class RequireAllPermissionsCommand : ICommand<Unit> { }

    [RequireClaim("department")]
    public sealed class RequireClaimTypeOnlyCommand : ICommand<Unit> { }

    [RequireClaim("department", "engineering")]
    public sealed class RequireClaimWithValueCommand : ICommand<Unit> { }

    [RequireOwnership("OwnerId")]
    public sealed class RequireOwnershipCommand : ICommand<Unit>
    {
        public string OwnerId { get; init; } = string.Empty;
    }

    [DenyAnonymous]
    [RequireRole("Admin")]
    public sealed class AuthAndRoleCommand : ICommand<Unit> { }

    #endregion
}
