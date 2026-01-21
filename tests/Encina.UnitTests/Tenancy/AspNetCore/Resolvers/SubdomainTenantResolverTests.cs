using Microsoft.AspNetCore.Http;

namespace Encina.UnitTests.Tenancy.AspNetCore.Resolvers;

/// <summary>
/// Unit tests for SubdomainTenantResolver.
/// </summary>
public class SubdomainTenantResolverTests
{
    private readonly SubdomainTenantResolver _resolver;
    private readonly TenancyAspNetCoreOptions _options;

    public SubdomainTenantResolverTests()
    {
        _options = new TenancyAspNetCoreOptions();
        _options.SubdomainResolver.Enabled = true;
        _options.SubdomainResolver.BaseDomain = "example.com";
        _resolver = new SubdomainTenantResolver(Options.Create(_options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SubdomainTenantResolver(null!));
    }

    [Fact]
    public void Priority_ReturnsConfiguredPriority()
    {
        // Assert
        _resolver.Priority.ShouldBe(SubdomainTenantResolver.DefaultPriority);
    }

    [Fact]
    public async Task ResolveAsync_WithValidSubdomain_ReturnsTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.example.com");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBe("acme");
    }

    [Fact]
    public async Task ResolveAsync_WithNestedSubdomain_ReturnsFirstSegment()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("staging.acme.example.com");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBe("staging");
    }

    [Fact]
    public async Task ResolveAsync_BaseDomainOnly_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("example.com");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_DifferentDomain_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.different.com");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_ExcludedSubdomain_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("www.example.com");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_ApiSubdomain_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("api.example.com");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_AdminSubdomain_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("admin.example.com");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenDisabled_ReturnsNull()
    {
        // Arrange
        _options.SubdomainResolver.Enabled = false;
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.example.com");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_NoBaseDomainConfigured_ReturnsNull()
    {
        // Arrange
        _options.SubdomainResolver.BaseDomain = null;
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.example.com");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_EmptyHost_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("");

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
    public async Task ResolveAsync_CaseInsensitive_ReturnsTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("ACME.EXAMPLE.COM");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBe("ACME");
    }

    [Fact]
    public async Task ResolveAsync_ExcludedSubdomainCaseInsensitive_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("WWW.example.com");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithPort_ReturnsTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("acme.example.com", 8080);

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBe("acme");
    }

    [Fact]
    public void Priority_WithCustomValue_ReturnsCustomValue()
    {
        // Arrange
        _options.SubdomainResolver.Priority = 50;
        var resolver = new SubdomainTenantResolver(Options.Create(_options));

        // Assert
        resolver.Priority.ShouldBe(50);
    }

    [Fact]
    public async Task ResolveAsync_CustomExcludedSubdomain_ReturnsNull()
    {
        // Arrange
        _options.SubdomainResolver.ExcludedSubdomains.Add("staging");
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("staging.example.com");

        // Act
        var result = await _resolver.ResolveAsync(context);

        // Assert
        result.ShouldBeNull();
    }
}
