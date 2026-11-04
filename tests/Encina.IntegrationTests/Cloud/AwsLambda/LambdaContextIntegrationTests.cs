using Amazon.Lambda.APIGatewayEvents;
using Encina.AwsLambda;
using Microsoft.Extensions.Logging.Testing;
using static Encina.IntegrationTests.Cloud.AwsLambda.AwsLambdaTestSupport;

namespace Encina.IntegrationTests.Cloud.AwsLambda;

/// <summary>
/// Drives <see cref="LambdaContextExtensions"/> with API Gateway requests deserialized by the Lambda runtime serializer.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cloud", "AwsLambda")]
public sealed class LambdaContextIntegrationTests
{
    private static APIGatewayProxyRequest Request(string headers = "{}", string authorizer = "null") =>
        Deserialize<APIGatewayProxyRequest>(
            $$$"""{"httpMethod":"POST","path":"/orders","headers":{{{headers}}},"requestContext":{"authorizer":{{{authorizer}}}}}""");

    private static string Tenant(Either<EncinaError, Option<string>> result) =>
        result.Match(
            Right: tenant => tenant.IfNone("<none>"),
            Left: error => "error:" + error.GetCode().IfNone(string.Empty));

    [Fact]
    public void GetCorrelationId_PrefersTheRequestHeaderOverTheAwsRequestId()
    {
        // Arrange
        var context = new TestLambdaContext();
        var request = Request("""{"X-Correlation-ID":"corr-1"}""");

        // Act
        var correlationId = context.GetCorrelationId(request);

        // Assert
        correlationId.ShouldBe("corr-1");
    }

    [Fact]
    public void GetCorrelationId_WithoutAHeader_FallsBackToTheAwsRequestId()
    {
        // Arrange
        var context = new TestLambdaContext { AwsRequestId = "aws-req-9" };

        // Act
        var fromRequest = context.GetCorrelationId(Request());
        var withoutRequest = context.GetCorrelationId();

        // Assert
        fromRequest.ShouldBe("aws-req-9");
        withoutRequest.ShouldBe("aws-req-9");
    }

    [Fact]
    public void GetCorrelationId_ForAnHttpApiV2Request_HonoursTheConfiguredHeader()
    {
        // Arrange
        var context = new TestLambdaContext { AwsRequestId = "aws-req-9" };
        var request = Deserialize<APIGatewayHttpApiV2ProxyRequest>("""{"headers":{"X-Request-ID":"corr-2"}}""");
        var options = new EncinaAwsLambdaOptions { CorrelationIdHeader = "X-Request-ID" };

        // Act
        var configured = context.GetCorrelationId(request, options);
        var defaultHeader = context.GetCorrelationId(request);

        // Assert
        configured.ShouldBe("corr-2");
        defaultHeader.ShouldBe("aws-req-9");
    }

    [Fact]
    public void ResolveTenantId_FromTheVerifiedJwtClaim_ReturnsTheTenant()
    {
        // Arrange
        var request = Request(authorizer: """{"claims":{"tenant_id":"acme"}}""");

        // Act
        var result = new TestLambdaContext().ResolveTenantId(request, new EncinaAwsLambdaOptions(), new FakeLogger());

        // Assert
        Tenant(result).ShouldBe("acme");
    }

    [Fact]
    public void ResolveTenantId_WhenTheHeaderDisagreesWithTheClaim_ReturnsTheMismatchErrorAndLogsOnlyTheCode()
    {
        // Arrange
        var request = Request("""{"X-Tenant-ID":"other"}""", """{"claims":{"tenant_id":"acme"}}""");
        var logger = new FakeLogger();

        // Act
        var result = new TestLambdaContext().ResolveTenantId(request, new EncinaAwsLambdaOptions(), logger);

        // Assert
        Tenant(result).ShouldBe("error:" + LambdaContextExtensions.TenantMismatchErrorCode);
        var entry = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Message.ShouldNotContain("acme");
        entry.Message.ShouldNotContain("other");
    }

    [Fact]
    public void ResolveTenantId_WithAnUnreadableClaim_FailsClosedInsteadOfUsingTheHeader()
    {
        // Arrange
        var request = Request("""{"X-Tenant-ID":"acme"}""", """{"claims":{"tenant_id":42}}""");

        // Act
        var result = new TestLambdaContext().ResolveTenantId(
            request, new EncinaAwsLambdaOptions { TrustTenantIdHeader = true }, new FakeLogger());

        // Assert
        Tenant(result).ShouldBe("error:" + LambdaContextExtensions.InvalidTenantClaimErrorCode);
    }

    [Fact]
    public void ResolveTenantId_FromAnAuthorizerEntry_ReturnsTheTenant()
    {
        // Arrange
        var request = Request(authorizer: """{"tenant_id":"globex"}""");

        // Act
        var result = new TestLambdaContext().ResolveTenantId(request, new EncinaAwsLambdaOptions(), new FakeLogger());

        // Assert
        Tenant(result).ShouldBe("globex");
    }

    [Theory]
    [InlineData(false, "<none>", LogLevel.Debug)]
    [InlineData(true, "acme", LogLevel.Warning)]
    public void ResolveTenantId_WithOnlyAHeader_UsesItOnlyWhenTheHeaderIsTrusted(
        bool trust, string expectedTenant, LogLevel expectedLevel)
    {
        // Arrange
        var request = Request("""{"X-Tenant-ID":"acme"}""");
        var logger = new FakeLogger();

        // Act
        var result = new TestLambdaContext().ResolveTenantId(
            request, new EncinaAwsLambdaOptions { TrustTenantIdHeader = trust }, logger);

        // Assert
        Tenant(result).ShouldBe(expectedTenant);
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(expectedLevel);
    }

    [Fact]
    public void ResolveTenantId_WithoutARequest_ReturnsNoTenant()
    {
        // Act
        var result = new TestLambdaContext().ResolveTenantId(null, new EncinaAwsLambdaOptions(), new FakeLogger());

        // Assert
        Tenant(result).ShouldBe("<none>");
    }

    [Fact]
    public void GetUserId_ReadsTheClaimThenThePrincipalId()
    {
        // Arrange
        var context = new TestLambdaContext();
        var withClaims = Request(authorizer: """{"claims":{"sub":"user-1"}}""");
        // The principalId is set as a string by hand: a request deserialized from JSON holds a JsonElement there,
        // which GetUserId does not read (follow-up bug issue drafted from #2699).
        var withPrincipal = Request();
        withPrincipal.RequestContext.Authorizer = new APIGatewayCustomAuthorizerContext { ["principalId"] = "principal-1" };

        // Act & Assert
        context.GetUserId(withClaims).ShouldBe("user-1");
        context.GetUserId(withPrincipal).ShouldBe("principal-1");
        context.GetUserId(Request()).ShouldBeNull();
        context.GetUserId().ShouldBeNull();
    }

    [Fact]
    public void ContextAccessors_ExposeTheRuntimeValues()
    {
        // Arrange
        var context = new TestLambdaContext
        {
            AwsRequestId = "aws-req-1",
            FunctionName = "orders-function",
            RemainingTime = TimeSpan.FromSeconds(12)
        };

        // Act & Assert
        context.GetAwsRequestId().ShouldBe("aws-req-1");
        context.GetFunctionName().ShouldBe("orders-function");
        context.GetRemainingTimeMs().ShouldBe(12_000);
    }
}
