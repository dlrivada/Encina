using System.Security.Claims;
using Encina.AspNetCore.Authorization;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.AspNetCore.Authorization;

public class ResourceAuthorizerTests
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly ResourceAuthorizer _authorizer;

    public ResourceAuthorizerTests()
    {
        _authorizationService = Substitute.For<IAuthorizationService>();
        _requestContextAccessor = Substitute.For<IRequestContextAccessor>();
        _authorizer = CreateAuthorizer();
    }

    // ── Generic overload ────────────────────────────────────────────

    [Fact]
    public async Task AuthorizeAsync_Generic_PolicySucceeds_ReturnsRightTrue()
    {
        // Arrange
        SetupAuthenticatedUser("user-1");
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());

        var resource = new TestResource("order-123");

        // Act
        var result = await _authorizer.AuthorizeAsync(resource, "CanEdit", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(value => value.ShouldBeTrue());
    }

    [Fact]
    public async Task AuthorizeAsync_Generic_PolicyFails_ReturnsLeftResourceDenied()
    {
        // Arrange
        SetupAuthenticatedUser("user-1");
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed(
                AuthorizationFailure.Failed(new[] { new AuthorizationFailureReason(null!, "Not owner") })));

        var resource = new TestResource("order-123");

        // Act
        var result = await _authorizer.AuthorizeAsync(resource, "CanEdit", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationResourceDenied),
                None: () => Assert.Fail("Expected error code"));
            error.Message.ShouldContain("Resource authorization denied");
            error.Message.ShouldContain("CanEdit");
        });
    }

    [Theory]
    [InlineData("no-context")]
    [InlineData("anonymous")]
    [InlineData("null-identity")]
    public async Task AuthorizeAsync_WithoutAnAuthenticatedRequestIdentity_ReturnsLeftUnauthenticated(string caller)
    {
        // Arrange: the caller is the request identity, never HttpContext.User (#1705)
        _requestContextAccessor.RequestContext.Returns(caller switch
        {
            "anonymous" => RequestContext.CreateForTest(),
            "null-identity" => Substitute.For<IRequestContext>(),
            _ => null
        });

        // Act
        var result = await _authorizer.AuthorizeAsync(new TestResource("order-123"), "CanEdit", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(EncinaErrorCodes.AuthorizationUnauthenticated));
        await _authorizationService.DidNotReceiveWithAnyArgs().AuthorizeAsync(default!, default, default(string)!);
    }

    [Fact]
    public async Task AuthorizeAsync_EvaluatesTheRequestIdentityPrincipal()
    {
        SetupAuthenticatedUser("identity-user");
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());

        await _authorizer.AuthorizeAsync(new TestResource("order-123"), "CanEdit", CancellationToken.None);

        await _authorizationService.Received(1).AuthorizeAsync(
            Arg.Is<ClaimsPrincipal>(user => user.FindFirst(ClaimTypes.NameIdentifier)!.Value == "identity-user"),
            Arg.Any<object?>(),
            "CanEdit");
    }

    [Fact]
    public async Task AuthorizeAsync_AnAuthenticatedIdentityWithoutPrincipal_SatisfiesNoPolicy()
    {
        _requestContextAccessor.RequestContext.Returns(TestRequestContext.For(RequestIdentity.ForUser("builder-user")));
        _authorizationService
            .AuthorizeAsync(Arg.Is<ClaimsPrincipal>(user => user.Identity!.IsAuthenticated), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());
        _authorizationService
            .AuthorizeAsync(Arg.Is<ClaimsPrincipal>(user => !user.Identity!.IsAuthenticated), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());

        var result = await _authorizer.AuthorizeAsync(new TestResource("order-123"), "CanEdit", CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => new ResourceAuthorizer(null!, _requestContextAccessor));
        Should.Throw<ArgumentNullException>(() => new ResourceAuthorizer(_authorizationService, null!));
    }

    [Fact]
    public async Task AuthorizeAsync_Generic_NullResource_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _authorizer.AuthorizeAsync<TestResource>(null!, "CanEdit", CancellationToken.None));
    }

    [Fact]
    public async Task AuthorizeAsync_Generic_NullPolicy_ThrowsArgumentException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => _authorizer.AuthorizeAsync(new TestResource("x"), null!, CancellationToken.None));
    }

    [Fact]
    public async Task AuthorizeAsync_Generic_EmptyPolicy_ThrowsArgumentException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => _authorizer.AuthorizeAsync(new TestResource("x"), "", CancellationToken.None));
    }

    [Fact]
    public async Task AuthorizeAsync_Generic_DelegatesToIAuthorizationService()
    {
        // Arrange
        SetupAuthenticatedUser("user-1");
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());

        var resource = new TestResource("order-123");

        // Act
        await _authorizer.AuthorizeAsync(resource, "CanEdit", CancellationToken.None);

        // Assert
        await _authorizationService.Received(1).AuthorizeAsync(
            Arg.Any<ClaimsPrincipal>(),
            Arg.Is<object?>(r => Equals(r, resource)),
            Arg.Is<string>("CanEdit"));
    }

    // ── Non-generic overload ────────────────────────────────────────

    [Fact]
    public async Task AuthorizeAsync_NonGeneric_PolicySucceeds_ReturnsRightTrue()
    {
        // Arrange
        SetupAuthenticatedUser("user-1");
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());

        object resource = new TestResource("order-123");

        // Act
        var result = await _authorizer.AuthorizeAsync(resource, "CanEdit", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_NonGeneric_PolicyFails_ReturnsLeftResourceDenied()
    {
        // Arrange
        SetupAuthenticatedUser("user-1");
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed(
                AuthorizationFailure.Failed(new[] { new AuthorizationFailureReason(null!, "Denied") })));

        object resource = new TestResource("order-123");

        // Act
        var result = await _authorizer.AuthorizeAsync(resource, "CanEdit", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationResourceDenied),
                None: () => Assert.Fail("Expected error code"));
        });
    }

    [Fact]
    public async Task AuthorizeAsync_NonGeneric_NullResource_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _authorizer.AuthorizeAsync((object)null!, "CanEdit", CancellationToken.None));
    }

    [Fact]
    public async Task AuthorizeAsync_FailureReasons_IncludedInErrorDetails()
    {
        // Arrange
        SetupAuthenticatedUser("user-1");
        _authorizationService
            .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed(
                AuthorizationFailure.Failed(new[]
                {
                    new AuthorizationFailureReason(null!, "Reason 1"),
                    new AuthorizationFailureReason(null!, "Reason 2")
                })));

        var resource = new TestResource("order-123");

        // Act
        var result = await _authorizer.AuthorizeAsync(resource, "CanEdit", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var details = error.GetDetails();
            details.ShouldNotBeNull();
            details.ShouldContainKey("failureReasons");
            var reasons = details["failureReasons"] as List<string>;
            reasons.ShouldNotBeNull();
            reasons.ShouldContain("Reason 1");
            reasons.ShouldContain("Reason 2");
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private ResourceAuthorizer CreateAuthorizer()
    {
        return new ResourceAuthorizer(_authorizationService, _requestContextAccessor);
    }

    private void SetupAuthenticatedUser(string userId)
    {
        _requestContextAccessor.RequestContext.Returns(TestRequestContext.For(
            TestIdentity.User(userId, claims: [new Claim(ClaimTypes.NameIdentifier, userId)])));
    }

    private sealed record TestResource(string Id);
}
