using Encina.AspNetCore;
using FluentAssertions;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Unit tests for <see cref="HttpAuditContextExtensions"/>.
/// </summary>
public class HttpAuditContextExtensionsTests
{
    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        // Assert
        HttpAuditContextExtensions.IpAddressKey.Should().Be("Encina.Audit.IpAddress");
        HttpAuditContextExtensions.UserAgentKey.Should().Be("Encina.Audit.UserAgent");
    }

    #region GetIpAddress Tests

    [Fact]
    public void GetIpAddress_WhenPresent_ShouldReturnValue()
    {
        // Arrange
        var context = RequestContext.CreateForTest()
            .WithMetadata(HttpAuditContextExtensions.IpAddressKey, "192.168.1.100");

        // Act
        var result = context.GetIpAddress();

        // Assert
        result.Should().Be("192.168.1.100");
    }

    [Fact]
    public void GetIpAddress_WhenNotPresent_ShouldReturnNull()
    {
        // Arrange
        var context = RequestContext.CreateForTest();

        // Act
        var result = context.GetIpAddress();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetIpAddress_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var context = RequestContext.CreateForTest()
            .WithMetadata(HttpAuditContextExtensions.IpAddressKey, null);

        // Act
        var result = context.GetIpAddress();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetIpAddress_WithNonStringValue_ShouldReturnNull()
    {
        // Arrange
        var context = RequestContext.CreateForTest()
            .WithMetadata(HttpAuditContextExtensions.IpAddressKey, 12345);

        // Act
        var result = context.GetIpAddress();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetIpAddress_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act
        var act = () => context.GetIpAddress();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    #endregion

    #region WithIpAddress Tests

    [Fact]
    public void WithIpAddress_ShouldAddIpAddressToMetadata()
    {
        // Arrange
        var context = RequestContext.CreateForTest();

        // Act
        var result = context.WithIpAddress("10.0.0.1");

        // Assert
        result.GetIpAddress().Should().Be("10.0.0.1");
    }

    [Fact]
    public void WithIpAddress_ShouldReturnNewContext()
    {
        // Arrange
        var original = RequestContext.CreateForTest();

        // Act
        var result = original.WithIpAddress("10.0.0.1");

        // Assert
        result.Should().NotBeSameAs(original);
        original.GetIpAddress().Should().BeNull();
    }

    [Fact]
    public void WithIpAddress_WithNull_ShouldSetNullValue()
    {
        // Arrange
        var context = RequestContext.CreateForTest()
            .WithIpAddress("10.0.0.1");

        // Act
        var result = context.WithIpAddress(null);

        // Assert
        result.GetIpAddress().Should().BeNull();
    }

    [Fact]
    public void WithIpAddress_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act
        var act = () => context.WithIpAddress("10.0.0.1");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    #endregion

    #region GetUserAgent Tests

    [Fact]
    public void GetUserAgent_WhenPresent_ShouldReturnValue()
    {
        // Arrange
        var context = RequestContext.CreateForTest()
            .WithMetadata(HttpAuditContextExtensions.UserAgentKey, "Mozilla/5.0");

        // Act
        var result = context.GetUserAgent();

        // Assert
        result.Should().Be("Mozilla/5.0");
    }

    [Fact]
    public void GetUserAgent_WhenNotPresent_ShouldReturnNull()
    {
        // Arrange
        var context = RequestContext.CreateForTest();

        // Act
        var result = context.GetUserAgent();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserAgent_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var context = RequestContext.CreateForTest()
            .WithMetadata(HttpAuditContextExtensions.UserAgentKey, null);

        // Act
        var result = context.GetUserAgent();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserAgent_WithNonStringValue_ShouldReturnNull()
    {
        // Arrange
        var context = RequestContext.CreateForTest()
            .WithMetadata(HttpAuditContextExtensions.UserAgentKey, 12345);

        // Act
        var result = context.GetUserAgent();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserAgent_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act
        var act = () => context.GetUserAgent();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    #endregion

    #region WithUserAgent Tests

    [Fact]
    public void WithUserAgent_ShouldAddUserAgentToMetadata()
    {
        // Arrange
        var context = RequestContext.CreateForTest();

        // Act
        var result = context.WithUserAgent("MyApp/1.0");

        // Assert
        result.GetUserAgent().Should().Be("MyApp/1.0");
    }

    [Fact]
    public void WithUserAgent_ShouldReturnNewContext()
    {
        // Arrange
        var original = RequestContext.CreateForTest();

        // Act
        var result = original.WithUserAgent("MyApp/1.0");

        // Assert
        result.Should().NotBeSameAs(original);
        original.GetUserAgent().Should().BeNull();
    }

    [Fact]
    public void WithUserAgent_WithNull_ShouldSetNullValue()
    {
        // Arrange
        var context = RequestContext.CreateForTest()
            .WithUserAgent("MyApp/1.0");

        // Act
        var result = context.WithUserAgent(null);

        // Assert
        result.GetUserAgent().Should().BeNull();
    }

    [Fact]
    public void WithUserAgent_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act
        var act = () => context.WithUserAgent("MyApp/1.0");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    #endregion

    #region Combined Usage Tests

    [Fact]
    public void CombinedUsage_ShouldSupportChaining()
    {
        // Arrange
        var context = RequestContext.CreateForTest();

        // Act
        var result = context
            .WithIpAddress("192.168.1.1")
            .WithUserAgent("TestAgent/2.0");

        // Assert
        result.GetIpAddress().Should().Be("192.168.1.1");
        result.GetUserAgent().Should().Be("TestAgent/2.0");
    }

    [Fact]
    public void CombinedUsage_ShouldPreserveOtherMetadata()
    {
        // Arrange
        var context = RequestContext.CreateForTest()
            .WithMetadata("CustomKey", "CustomValue");

        // Act
        var result = context
            .WithIpAddress("192.168.1.1")
            .WithUserAgent("TestAgent/2.0");

        // Assert
        result.Metadata["CustomKey"].Should().Be("CustomValue");
        result.GetIpAddress().Should().Be("192.168.1.1");
        result.GetUserAgent().Should().Be("TestAgent/2.0");
    }

    [Fact]
    public void CombinedUsage_ShouldPreserveCoreContextProperties()
    {
        // Arrange
        var context = RequestContext.CreateForTest(
            userId: "user-123",
            tenantId: "tenant-456",
            correlationId: "corr-789");

        // Act
        var result = context
            .WithIpAddress("192.168.1.1")
            .WithUserAgent("TestAgent/2.0");

        // Assert
        result.UserId.Should().Be("user-123");
        result.TenantId.Should().Be("tenant-456");
        result.CorrelationId.Should().Be("corr-789");
    }

    #endregion
}
