using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Encina.UnitTests.Tenancy.AspNetCore.Resolvers;

/// <summary>
/// Unit tests for ClaimTenantResolver.
/// </summary>
public class ClaimTenantResolverTests
{
    private readonly ClaimTenantResolver _resolver;
    private readonly TenancyAspNetCoreOptions _options;

    public ClaimTenantResolverTests()
    {
        _options = new TenancyAspNetCoreOptions();
        _resolver = new ClaimTenantResolver(Options.Create(_options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ClaimTenantResolver(null!));
    }

    [Fact]
    public void Priority_ReturnsConfiguredPriority()
    {
        // Assert
        _resolver.Priority.ShouldBe(ClaimTenantResolver.DefaultPriority);
    }

    [Fact]
    public async Task ResolveAsync_WithValidClaim_ReturnsTenantId()
    {
        // Arrange
        var context = CreateContextWithClaims(new Claim("tenant_id", "acme-corp"));

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBe("acme-corp");
    }

    [Fact]
    public async Task ResolveAsync_WithCustomClaimType_ReturnsTenantId()
    {
        // Arrange
        _options.ClaimResolver.ClaimType = "org_id";
        var context = CreateContextWithClaims(new Claim("org_id", "contoso"));

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBe("contoso");
    }

    [Fact]
    public async Task ResolveAsync_UserNotAuthenticated_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_ClaimNotPresent_ReturnsNull()
    {
        // Arrange
        var context = CreateContextWithClaims(new Claim("sub", "user-123"));

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_EmptyClaimValue_ReturnsNull()
    {
        // Arrange
        var context = CreateContextWithClaims(new Claim("tenant_id", ""));

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhitespaceClaimValue_ReturnsNull()
    {
        // Arrange
        var context = CreateContextWithClaims(new Claim("tenant_id", "   "));

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenDisabled_ReturnsNull()
    {
        // Arrange
        _options.ClaimResolver.Enabled = false;
        var context = CreateContextWithClaims(new Claim("tenant_id", "acme-corp"));

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_NullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _resolver.ResolveAsync(null!));
    }

    [Fact]
    public void Priority_WithCustomValue_ReturnsCustomValue()
    {
        // Arrange
        _options.ClaimResolver.Priority = 50;
        var resolver = new ClaimTenantResolver(Options.Create(_options));

        // Assert
        resolver.Priority.ShouldBe(50);
    }

    private static DefaultHttpContext CreateContextWithClaims(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuthentication");
        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext
        {
            User = principal
        };

        return context;
    }
}
