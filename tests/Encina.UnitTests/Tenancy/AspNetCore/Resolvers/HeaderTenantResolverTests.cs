using Microsoft.AspNetCore.Http;

namespace Encina.UnitTests.Tenancy.AspNetCore.Resolvers;

/// <summary>
/// Unit tests for HeaderTenantResolver.
/// </summary>
public class HeaderTenantResolverTests
{
    private readonly HeaderTenantResolver _resolver;
    private readonly TenancyAspNetCoreOptions _options;

    public HeaderTenantResolverTests()
    {
        _options = new TenancyAspNetCoreOptions();
        _resolver = new HeaderTenantResolver(Options.Create(_options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new HeaderTenantResolver(null!));
    }

    [Fact]
    public void Priority_ReturnsConfiguredPriority()
    {
        // Assert
        _resolver.Priority.ShouldBe(HeaderTenantResolver.DefaultPriority);
    }

    [Fact]
    public async Task ResolveAsync_WithValidHeader_ReturnsTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-ID"] = "acme-corp";

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBe("acme-corp");
    }

    [Fact]
    public async Task ResolveAsync_WithCustomHeaderName_ReturnsTenantId()
    {
        // Arrange
        _options.HeaderResolver.HeaderName = "X-Organization-ID";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Organization-ID"] = "contoso";

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBe("contoso");
    }

    [Fact]
    public async Task ResolveAsync_HeaderNotPresent_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_EmptyHeader_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-ID"] = "";

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhitespaceHeader_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-ID"] = "   ";

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenDisabled_ReturnsNull()
    {
        // Arrange
        _options.HeaderResolver.Enabled = false;
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-ID"] = "acme-corp";

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
        _options.HeaderResolver.Priority = 50;
        var resolver = new HeaderTenantResolver(Options.Create(_options));

        // Assert
        resolver.Priority.ShouldBe(50);
    }
}
