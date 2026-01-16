using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Encina.AwsLambda;

namespace Encina.UnitTests.AwsLambda;

public class LambdaContextExtensionsTests
{
    private readonly ILambdaContext _context;

    public LambdaContextExtensionsTests()
    {
        _context = Substitute.For<ILambdaContext>();
        _context.AwsRequestId.Returns("aws-request-123");
        _context.FunctionName.Returns("test-function");
        _context.RemainingTime.Returns(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void GetCorrelationId_WithHeaderPresent_ReturnsHeaderValue()
    {
        // Arrange
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Correlation-ID"] = "correlation-from-header"
            }
        };

        // Act
        var correlationId = _context.GetCorrelationId(request);

        // Assert
        correlationId.ShouldBe("correlation-from-header");
    }

    [Fact]
    public void GetCorrelationId_WithCustomHeader_ReturnsHeaderValue()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions { CorrelationIdHeader = "X-Request-ID" };
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Request-ID"] = "custom-correlation-id"
            }
        };

        // Act
        var correlationId = _context.GetCorrelationId(request, options);

        // Assert
        correlationId.ShouldBe("custom-correlation-id");
    }

    [Fact]
    public void GetCorrelationId_WithoutHeader_ReturnsAwsRequestId()
    {
        // Act
        var correlationId = _context.GetCorrelationId();

        // Assert
        correlationId.ShouldBe("aws-request-123");
    }

    [Fact]
    public void GetCorrelationId_WithEmptyHeader_ReturnsAwsRequestId()
    {
        // Arrange
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Correlation-ID"] = ""
            }
        };

        // Act
        var correlationId = _context.GetCorrelationId(request);

        // Assert
        correlationId.ShouldBe("aws-request-123");
    }

    [Fact]
    public void GetCorrelationId_WithNullHeaders_ReturnsAwsRequestId()
    {
        // Arrange
        var request = new APIGatewayProxyRequest { Headers = null };

        // Act
        var correlationId = _context.GetCorrelationId(request);

        // Assert
        correlationId.ShouldBe("aws-request-123");
    }

    [Fact]
    public void GetCorrelationId_WithHttpApiV2Request_ReturnsHeaderValue()
    {
        // Arrange
        var request = new APIGatewayHttpApiV2ProxyRequest
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Correlation-ID"] = "v2-correlation-id"
            }
        };

        // Act
        var correlationId = _context.GetCorrelationId(request);

        // Assert
        correlationId.ShouldBe("v2-correlation-id");
    }

    [Fact]
    public void GetTenantId_WithHeaderPresent_ReturnsHeaderValue()
    {
        // Arrange
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Tenant-ID"] = "tenant-123"
            }
        };

        // Act
        var tenantId = _context.GetTenantId(request);

        // Assert
        tenantId.ShouldBe("tenant-123");
    }

    [Fact]
    public void GetTenantId_WithCustomHeader_ReturnsHeaderValue()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions { TenantIdHeader = "X-Organization-ID" };
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Organization-ID"] = "org-456"
            }
        };

        // Act
        var tenantId = _context.GetTenantId(request, options);

        // Assert
        tenantId.ShouldBe("org-456");
    }

    [Fact]
    public void GetTenantId_WithNoHeader_ReturnsNull()
    {
        // Arrange
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>()
        };

        // Act
        var tenantId = _context.GetTenantId(request);

        // Assert
        tenantId.ShouldBeNull();
    }

    [Fact]
    public void GetTenantId_WithNullRequest_ReturnsNull()
    {
        // Act
        var tenantId = _context.GetTenantId(null);

        // Assert
        tenantId.ShouldBeNull();
    }

    [Fact]
    public void GetTenantId_WithNoAuthorizerClaims_ReturnsNull()
    {
        // Arrange - Request without authorizer
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>(),
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext()
        };

        // Act
        var tenantId = _context.GetTenantId(request);

        // Assert
        tenantId.ShouldBeNull();
    }

    [Fact]
    public void GetUserId_WithNoAuthorizerClaims_ReturnsNull()
    {
        // Arrange - Request without authorizer
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>(),
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext()
        };

        // Act
        var userId = _context.GetUserId(request);

        // Assert
        userId.ShouldBeNull();
    }

    [Fact]
    public void GetUserId_WithRequestContext_ReturnsNull()
    {
        // Arrange - Request with empty request context
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>(),
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext()
        };

        // Act
        var userId = _context.GetUserId(request);

        // Assert
        userId.ShouldBeNull();
    }

    [Fact]
    public void GetUserId_WithCustomClaimType_AndNoAuthorizerClaims_ReturnsNull()
    {
        // Arrange
        var options = new EncinaAwsLambdaOptions { UserIdClaimType = "user_id" };
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>(),
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext()
        };

        // Act
        var userId = _context.GetUserId(request, options);

        // Assert
        userId.ShouldBeNull();
    }

    [Fact]
    public void GetUserId_WithNullRequest_ReturnsNull()
    {
        // Act
        var userId = _context.GetUserId(null);

        // Assert
        userId.ShouldBeNull();
    }

    [Fact]
    public void GetAwsRequestId_ReturnsContextValue()
    {
        // Act
        var requestId = _context.GetAwsRequestId();

        // Assert
        requestId.ShouldBe("aws-request-123");
    }

    [Fact]
    public void GetFunctionName_ReturnsContextValue()
    {
        // Act
        var functionName = _context.GetFunctionName();

        // Assert
        functionName.ShouldBe("test-function");
    }

    [Fact]
    public void GetRemainingTimeMs_ReturnsContextValue()
    {
        // Act
        var remainingMs = _context.GetRemainingTimeMs();

        // Assert
        remainingMs.ShouldBe(30000);
    }

    [Fact]
    public void GetCorrelationId_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        ILambdaContext nullContext = null!;

        // Act
        var action = () => nullContext.GetCorrelationId();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public void GetTenantId_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        ILambdaContext nullContext = null!;

        // Act
        var action = () => nullContext.GetTenantId();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public void GetUserId_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        ILambdaContext nullContext = null!;

        // Act
        var action = () => nullContext.GetUserId();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public void GetAwsRequestId_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        ILambdaContext nullContext = null!;

        // Act
        var action = () => nullContext.GetAwsRequestId();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public void GetFunctionName_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        ILambdaContext nullContext = null!;

        // Act
        var action = () => nullContext.GetFunctionName();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public void GetRemainingTimeMs_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        ILambdaContext nullContext = null!;

        // Act
        Action action = () => _ = nullContext.GetRemainingTimeMs();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(action);
        ex.ParamName.ShouldBe("context");
    }
}
