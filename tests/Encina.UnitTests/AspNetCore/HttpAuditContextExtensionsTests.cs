using Encina.AspNetCore;
using Shouldly;

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
        HttpAuditContextExtensions.IpAddressKey.ShouldBe("Encina.Audit.IpAddress");
        HttpAuditContextExtensions.UserAgentKey.ShouldBe("Encina.Audit.UserAgent");
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
        result.ShouldBe("192.168.1.100");
    }

    [Fact]
    public void GetIpAddress_WhenNotPresent_ShouldReturnNull()
    {
        // Arrange
        var context = RequestContext.CreateForTest();

        // Act
        var result = context.GetIpAddress();

        // Assert
        result.ShouldBeNull();
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
        result.ShouldBeNull();
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
        result.ShouldBeNull();
    }

    [Fact]
    public void GetIpAddress_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act
        Action act = () => context.GetIpAddress();

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("context");
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
        result.GetIpAddress().ShouldBe("10.0.0.1");
    }

    [Fact]
    public void WithIpAddress_ShouldReturnNewContext()
    {
        // Arrange
        var original = RequestContext.CreateForTest();

        // Act
        var result = original.WithIpAddress("10.0.0.1");

        // Assert
        result.ShouldNotBeSameAs(original);
        original.GetIpAddress().ShouldBeNull();
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
        result.GetIpAddress().ShouldBeNull();
    }

    [Fact]
    public void WithIpAddress_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act
        Action act = () => context.WithIpAddress("10.0.0.1");

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("context");
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
        result.ShouldBe("Mozilla/5.0");
    }

    [Fact]
    public void GetUserAgent_WhenNotPresent_ShouldReturnNull()
    {
        // Arrange
        var context = RequestContext.CreateForTest();

        // Act
        var result = context.GetUserAgent();

        // Assert
        result.ShouldBeNull();
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
        result.ShouldBeNull();
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
        result.ShouldBeNull();
    }

    [Fact]
    public void GetUserAgent_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act
        Action act = () => context.GetUserAgent();

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("context");
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
        result.GetUserAgent().ShouldBe("MyApp/1.0");
    }

    [Fact]
    public void WithUserAgent_ShouldReturnNewContext()
    {
        // Arrange
        var original = RequestContext.CreateForTest();

        // Act
        var result = original.WithUserAgent("MyApp/1.0");

        // Assert
        result.ShouldNotBeSameAs(original);
        original.GetUserAgent().ShouldBeNull();
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
        result.GetUserAgent().ShouldBeNull();
    }

    [Fact]
    public void WithUserAgent_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        IRequestContext context = null!;

        // Act
        Action act = () => context.WithUserAgent("MyApp/1.0");

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("context");
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
        result.GetIpAddress().ShouldBe("192.168.1.1");
        result.GetUserAgent().ShouldBe("TestAgent/2.0");
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
        result.Metadata["CustomKey"].ShouldBe("CustomValue");
        result.GetIpAddress().ShouldBe("192.168.1.1");
        result.GetUserAgent().ShouldBe("TestAgent/2.0");
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
        result.UserId.ShouldBe("user-123");
        result.TenantId.ShouldBe("tenant-456");
        result.CorrelationId.ShouldBe("corr-789");
    }

    #endregion
}
