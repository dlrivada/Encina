using Microsoft.AspNetCore.Http;

namespace Encina.UnitTests.Tenancy.AspNetCore.Resolvers;

/// <summary>
/// Unit tests for RouteTenantResolver.
/// </summary>
public class RouteTenantResolverTests
{
    private readonly RouteTenantResolver _resolver;
    private readonly TenancyAspNetCoreOptions _options;

    public RouteTenantResolverTests()
    {
        _options = new TenancyAspNetCoreOptions();
        _options.RouteResolver.Enabled = true; // Disabled by default, enable for tests
        _resolver = new RouteTenantResolver(Options.Create(_options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RouteTenantResolver(null!));
    }

    [Fact]
    public void Priority_ReturnsConfiguredPriority()
    {
        // Assert
        _resolver.Priority.ShouldBe(RouteTenantResolver.DefaultPriority);
    }

    [Fact]
    public async Task ResolveAsync_WithValidRouteParameter_ReturnsTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = "acme-corp";

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBe("acme-corp");
    }

    [Fact]
    public async Task ResolveAsync_WithCustomParameterName_ReturnsTenantId()
    {
        // Arrange
        _options.RouteResolver.ParameterName = "org";
        var context = new DefaultHttpContext();
        context.Request.RouteValues["org"] = "contoso";

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBe("contoso");
    }

    [Fact]
    public async Task ResolveAsync_RouteParameterNotPresent_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_EmptyRouteParameter_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = "";

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhitespaceRouteParameter_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = "   ";

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenDisabled_ReturnsNull()
    {
        // Arrange
        _options.RouteResolver.Enabled = false;
        var context = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = "acme-corp";

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
        _options.RouteResolver.Priority = 50;
        var resolver = new RouteTenantResolver(Options.Create(_options));

        // Assert
        resolver.Priority.ShouldBe(50);
    }

    [Fact]
    public async Task ResolveAsync_WithIntRouteValue_ConvertsTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.RouteValues["tenantId"] = 12345;

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBe("12345");
    }
}
