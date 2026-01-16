using System.Security.Claims;
using Encina.AzureFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using EncinaExtensions = Encina.AzureFunctions.FunctionContextExtensions;

namespace Encina.UnitTests.AzureFunctions;

public class FunctionContextExtensionsTests
{
    private readonly FunctionContext _context;
    private readonly Dictionary<object, object> _items;

    public FunctionContextExtensionsTests()
    {
        _context = Substitute.For<FunctionContext>();
        _items = new Dictionary<object, object>();
        _context.Items.Returns(_items);
    }

    [Fact]
    public void GetCorrelationId_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EncinaExtensions.GetCorrelationId(null!));
    }

    [Fact]
    public void GetCorrelationId_WhenSet_ReturnsValue()
    {
        // Arrange
        var correlationId = "test-correlation-id";
        _items[EncinaFunctionMiddleware.CorrelationIdKey] = correlationId;

        // Act
        var result = _context.GetCorrelationId();

        // Assert
        result.ShouldBe(correlationId);
    }

    [Fact]
    public void GetCorrelationId_WhenNotSet_ReturnsNull()
    {
        // Act
        var result = _context.GetCorrelationId();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetCorrelationId_WhenSetWithNonStringValue_ReturnsNull()
    {
        // Arrange
        _items[EncinaFunctionMiddleware.CorrelationIdKey] = 12345;

        // Act
        var result = _context.GetCorrelationId();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetTenantId_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EncinaExtensions.GetTenantId(null!));
    }

    [Fact]
    public void GetTenantId_WhenSet_ReturnsValue()
    {
        // Arrange
        var tenantId = "tenant-123";
        _items[EncinaFunctionMiddleware.TenantIdKey] = tenantId;

        // Act
        var result = _context.GetTenantId();

        // Assert
        result.ShouldBe(tenantId);
    }

    [Fact]
    public void GetTenantId_WhenNotSet_ReturnsNull()
    {
        // Act
        var result = _context.GetTenantId();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetTenantId_WhenSetWithNonStringValue_ReturnsNull()
    {
        // Arrange
        _items[EncinaFunctionMiddleware.TenantIdKey] = 12345;

        // Act
        var result = _context.GetTenantId();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetUserId_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EncinaExtensions.GetUserId(null!));
    }

    [Fact]
    public void GetUserId_WhenSet_ReturnsValue()
    {
        // Arrange
        var userId = "user-456";
        _items[EncinaFunctionMiddleware.UserIdKey] = userId;

        // Act
        var result = _context.GetUserId();

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public void GetUserId_WhenNotSet_ReturnsNull()
    {
        // Act
        var result = _context.GetUserId();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetUserId_WhenSetWithNonStringValue_ReturnsNull()
    {
        // Arrange
        _items[EncinaFunctionMiddleware.UserIdKey] = 12345;

        // Act
        var result = _context.GetUserId();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetInvocationId_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EncinaExtensions.GetInvocationId(null!));
    }

    [Fact]
    public void GetInvocationId_ReturnsContextInvocationId()
    {
        // Arrange
        var invocationId = "inv-789";
        _context.InvocationId.Returns(invocationId);

        // Act
        var result = _context.GetInvocationId();

        // Assert
        result.ShouldBe(invocationId);
    }

    [Fact]
    public void GetCorrelationId_WithHttpRequest_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var httpRequest = Substitute.For<HttpRequestData>(_context);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EncinaExtensions.GetCorrelationId(null!, httpRequest));
    }

    [Fact]
    public void GetCorrelationId_WithHttpRequest_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _context.GetCorrelationId(null!));
    }

    [Fact]
    public void GetCorrelationId_WithHttpRequest_FromHeader_ReturnsValue()
    {
        // Arrange
        var correlationId = "header-correlation-id";
        var httpRequest = Substitute.For<HttpRequestData>(_context);
        var headers = new HttpHeadersCollection();
        headers.Add("X-Correlation-ID", correlationId);
        httpRequest.Headers.Returns(headers);

        // Act
        var result = _context.GetCorrelationId(httpRequest);

        // Assert
        result.ShouldBe(correlationId);
    }

    [Fact]
    public void GetCorrelationId_WithHttpRequest_FromCustomHeader_ReturnsValue()
    {
        // Arrange
        var correlationId = "custom-correlation-id";
        var options = new EncinaAzureFunctionsOptions { CorrelationIdHeader = "X-Custom-Correlation" };
        var httpRequest = Substitute.For<HttpRequestData>(_context);
        var headers = new HttpHeadersCollection();
        headers.Add("X-Custom-Correlation", correlationId);
        httpRequest.Headers.Returns(headers);

        // Act
        var result = _context.GetCorrelationId(httpRequest, options);

        // Assert
        result.ShouldBe(correlationId);
    }

    [Fact]
    public void GetCorrelationId_WithHttpRequest_FallsBackToContext()
    {
        // Arrange
        var correlationId = "context-correlation-id";
        _items[EncinaFunctionMiddleware.CorrelationIdKey] = correlationId;
        var httpRequest = Substitute.For<HttpRequestData>(_context);
        httpRequest.Headers.Returns(new HttpHeadersCollection());

        // Act
        var result = _context.GetCorrelationId(httpRequest);

        // Assert
        result.ShouldBe(correlationId);
    }

    [Fact]
    public void GetTenantId_WithHttpRequest_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var httpRequest = Substitute.For<HttpRequestData>(_context);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EncinaExtensions.GetTenantId(null!, httpRequest));
    }

    [Fact]
    public void GetTenantId_WithHttpRequest_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _context.GetTenantId(null!));
    }

    [Fact]
    public void GetTenantId_WithHttpRequest_FromHeader_ReturnsValue()
    {
        // Arrange
        var tenantId = "header-tenant-id";
        var httpRequest = Substitute.For<HttpRequestData>(_context);
        var headers = new HttpHeadersCollection();
        headers.Add("X-Tenant-ID", tenantId);
        httpRequest.Headers.Returns(headers);
        httpRequest.Identities.Returns([]);

        // Act
        var result = _context.GetTenantId(httpRequest);

        // Assert
        result.ShouldBe(tenantId);
    }

    [Fact]
    public void GetTenantId_WithHttpRequest_FromClaim_ReturnsValue()
    {
        // Arrange
        var tenantId = "claim-tenant-id";
        var httpRequest = Substitute.For<HttpRequestData>(_context);
        httpRequest.Headers.Returns(new HttpHeadersCollection());
        var identity = new ClaimsIdentity([new Claim("tenant_id", tenantId)]);
        httpRequest.Identities.Returns([identity]);

        // Act
        var result = _context.GetTenantId(httpRequest);

        // Assert
        result.ShouldBe(tenantId);
    }

    [Fact]
    public void GetTenantId_WithHttpRequest_FromCustomClaim_ReturnsValue()
    {
        // Arrange
        var tenantId = "custom-claim-tenant-id";
        var options = new EncinaAzureFunctionsOptions { TenantIdClaimType = "custom_tenant" };
        var httpRequest = Substitute.For<HttpRequestData>(_context);
        httpRequest.Headers.Returns(new HttpHeadersCollection());
        var identity = new ClaimsIdentity([new Claim("custom_tenant", tenantId)]);
        httpRequest.Identities.Returns([identity]);

        // Act
        var result = _context.GetTenantId(httpRequest, options);

        // Assert
        result.ShouldBe(tenantId);
    }

    [Fact]
    public void GetTenantId_WithHttpRequest_FallsBackToContext()
    {
        // Arrange
        var tenantId = "context-tenant-id";
        _items[EncinaFunctionMiddleware.TenantIdKey] = tenantId;
        var httpRequest = Substitute.For<HttpRequestData>(_context);
        httpRequest.Headers.Returns(new HttpHeadersCollection());
        httpRequest.Identities.Returns([]);

        // Act
        var result = _context.GetTenantId(httpRequest);

        // Assert
        result.ShouldBe(tenantId);
    }

    [Fact]
    public void GetUserId_WithHttpRequest_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var httpRequest = Substitute.For<HttpRequestData>(_context);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EncinaExtensions.GetUserId(null!, httpRequest));
    }

    [Fact]
    public void GetUserId_WithHttpRequest_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _context.GetUserId(null!));
    }

    [Fact]
    public void GetUserId_WithHttpRequest_FromClaim_ReturnsValue()
    {
        // Arrange
        var userId = "claim-user-id";
        var httpRequest = Substitute.For<HttpRequestData>(_context);
        httpRequest.Headers.Returns(new HttpHeadersCollection());
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)]);
        httpRequest.Identities.Returns([identity]);

        // Act
        var result = _context.GetUserId(httpRequest);

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public void GetUserId_WithHttpRequest_FromCustomClaim_ReturnsValue()
    {
        // Arrange
        var userId = "custom-claim-user-id";
        var options = new EncinaAzureFunctionsOptions { UserIdClaimType = "sub" };
        var httpRequest = Substitute.For<HttpRequestData>(_context);
        httpRequest.Headers.Returns(new HttpHeadersCollection());
        var identity = new ClaimsIdentity([new Claim("sub", userId)]);
        httpRequest.Identities.Returns([identity]);

        // Act
        var result = _context.GetUserId(httpRequest, options);

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public void GetUserId_WithHttpRequest_FallsBackToContext()
    {
        // Arrange
        var userId = "context-user-id";
        _items[EncinaFunctionMiddleware.UserIdKey] = userId;
        var httpRequest = Substitute.For<HttpRequestData>(_context);
        httpRequest.Headers.Returns(new HttpHeadersCollection());
        httpRequest.Identities.Returns([]);

        // Act
        var result = _context.GetUserId(httpRequest);

        // Assert
        result.ShouldBe(userId);
    }
}
