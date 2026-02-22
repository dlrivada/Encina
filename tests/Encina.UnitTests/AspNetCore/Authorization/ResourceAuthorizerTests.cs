using System.Security.Claims;
using Encina.AspNetCore.Authorization;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.AspNetCore.Authorization;

public class ResourceAuthorizerTests
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ResourceAuthorizer _authorizer;

    public ResourceAuthorizerTests()
    {
        _authorizationService = Substitute.For<IAuthorizationService>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
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

    [Fact]
    public async Task AuthorizeAsync_Generic_NoHttpContext_ReturnsLeftUnauthorized()
    {
        // Arrange - no HTTP context set
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var resource = new TestResource("order-123");

        // Act
        var result = await _authorizer.AuthorizeAsync(resource, "CanEdit", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationUnauthorized),
                None: () => Assert.Fail("Expected error code"));
            error.Message.ShouldContain("HTTP context");
        });
    }

    [Fact]
    public async Task AuthorizeAsync_Generic_UnauthenticatedUser_ReturnsLeftUnauthorized()
    {
        // Arrange - unauthenticated user
        var httpContext = new DefaultHttpContext(); // default user is not authenticated
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var resource = new TestResource("order-123");

        // Act
        var result = await _authorizer.AuthorizeAsync(resource, "CanEdit", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            error.GetCode().Match(
                Some: code => code.ShouldBe(EncinaErrorCodes.AuthorizationUnauthorized),
                None: () => Assert.Fail("Expected error code"));
        });
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
        return new ResourceAuthorizer(_authorizationService, _httpContextAccessor);
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(httpContext);
    }

    private sealed record TestResource(string Id);
}
