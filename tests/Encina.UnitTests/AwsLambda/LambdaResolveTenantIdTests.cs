using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Encina.AwsLambda;
using LanguageExt;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.AwsLambda;

/// <summary>
/// The verified tenant claim always wins in <see cref="LambdaContextExtensions.ResolveTenantId"/> (#2399).
/// </summary>
public class LambdaResolveTenantIdTests
{
    private readonly ILambdaContext _context = Substitute.For<ILambdaContext>();
    private readonly FakeLogger _logger = new();
    private readonly EncinaAwsLambdaOptions _options = new();

    private static APIGatewayProxyRequest Request(string? header = null, string? claimInClaimsObject = null, string? claimInAuthorizer = null)
    {
        var authorizer = new APIGatewayCustomAuthorizerContext();
        if (claimInClaimsObject is not null)
        {
            authorizer["claims"] = JsonDocument.Parse($$"""{"tenant_id":"{{claimInClaimsObject}}"}""").RootElement;
        }

        if (claimInAuthorizer is not null)
        {
            authorizer["tenant_id"] = claimInAuthorizer;
        }

        return new APIGatewayProxyRequest
        {
            Headers = header is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["X-Tenant-ID"] = header },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext { Authorizer = authorizer },
        };
    }

    private const string NoTenant = "<none>";

    private static string Tenant(Either<EncinaError, Option<string>> result) =>
        result.Match(Right: o => o.IfNone(NoTenant), Left: _ => "<left>");

    [Fact]
    public void ClaimInClaimsObject_WithNoHeader_ReturnsClaim()
    {
        var result = _context.ResolveTenantId(Request(claimInClaimsObject: "tenant-a"), _options, _logger);

        Tenant(result).ShouldBe("tenant-a");
    }

    [Fact]
    public void ClaimInAuthorizerEntry_WithNoHeader_ReturnsClaim()
    {
        var result = _context.ResolveTenantId(Request(claimInAuthorizer: "tenant-a"), _options, _logger);

        Tenant(result).ShouldBe("tenant-a");
    }

    [Fact]
    public void ClaimAndSameHeader_ReturnsClaim()
    {
        var result = _context.ResolveTenantId(Request(header: "tenant-a", claimInClaimsObject: "tenant-a"), _options, _logger);

        Tenant(result).ShouldBe("tenant-a");
        _logger.Collector.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClaimAndDifferingHeader_IsALeftWithTheMismatchCode_EvenWhenTheHeaderIsTrusted(bool trust)
    {
        _options.TrustTenantIdHeader = trust;

        var result = _context.ResolveTenantId(Request(header: "tenant-b", claimInClaimsObject: "tenant-a"), _options, _logger);

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.GetCode().IfNone(string.Empty).ShouldBe(LambdaContextExtensions.TenantMismatchErrorCode);
        LambdaContextExtensions.TenantMismatchErrorCode.ShouldBe("tenancy.tenant_mismatch");
    }

    [Fact]
    public void Mismatch_LogsAWarningWithTheCodeAndNoTenantValue()
    {
        _context.ResolveTenantId(Request(header: "tenant-b", claimInAuthorizer: "tenant-a"), _options, _logger);

        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(Microsoft.Extensions.Logging.LogLevel.Warning);
        record.Id.Id.ShouldBe(4707);
        record.Message.ShouldContain("tenancy.tenant_mismatch");
        record.Message.ShouldNotContain("tenant-a");
        record.Message.ShouldNotContain("tenant-b");
    }

    [Fact]
    public void NoClaimAndTrustedHeader_ReturnsHeaderAndLogsAWarning()
    {
        _options.TrustTenantIdHeader = true;

        var result = _context.ResolveTenantId(Request(header: "tenant-b"), _options, _logger);

        Tenant(result).ShouldBe("tenant-b");
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(Microsoft.Extensions.Logging.LogLevel.Warning);
        record.Id.Id.ShouldBe(4708);
        record.Message.ShouldNotContain("tenant-b");
    }

    [Fact]
    public void NoClaimAndCustomTrustedHeader_ReturnsHeader()
    {
        _options.TrustTenantIdHeader = true;
        _options.TenantIdHeader = "X-Org";
        var request = new APIGatewayProxyRequest { Headers = new Dictionary<string, string> { ["X-Org"] = "org-1" } };

        Tenant(_context.ResolveTenantId(request, _options, _logger)).ShouldBe("org-1");
    }

    [Fact]
    public void NoClaimAndUntrustedHeader_IsIgnoredAndLogsDebug()
    {
        var result = _context.ResolveTenantId(Request(header: "tenant-b"), _options, _logger);

        result.IsRight.ShouldBeTrue();
        Tenant(result).ShouldBe(NoTenant);
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(Microsoft.Extensions.Logging.LogLevel.Debug);
        record.Id.Id.ShouldBe(4709);
        record.Message.ShouldNotContain("tenant-b");
    }

    [Fact]
    public void NothingPresent_ReturnsNone()
    {
        Tenant(_context.ResolveTenantId(Request(), _options, _logger)).ShouldBe(NoTenant);
        _logger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public void NullRequest_ReturnsNone()
    {
        Tenant(_context.ResolveTenantId(null, _options, _logger)).ShouldBe(NoTenant);
    }

    [Fact]
    public void RequestWithoutAuthorizerOrHeaders_ReturnsNone()
    {
        var request = new APIGatewayProxyRequest();

        Tenant(_context.ResolveTenantId(request, _options, _logger)).ShouldBe(NoTenant);
    }

    [Theory]
    [InlineData("""{"tenant_id":42}""", true)]
    [InlineData("""{"tenant_id":{"a":1}}""", true)]
    [InlineData("""{"tenant_id":null}""", true)]
    [InlineData("""{"tenant_id":""}""", true)]
    [InlineData("""{"tenant_id":42}""", false)]
    public void UnreadableClaimInClaimsObject_IsALeftWithTheInvalidClaimCode_NeverTheHeader(string claims, bool withTrustedHeader)
    {
        _options.TrustTenantIdHeader = withTrustedHeader;
        var authorizer = new APIGatewayCustomAuthorizerContext
        {
            ["claims"] = JsonDocument.Parse(claims).RootElement,
        };
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string> { ["X-Tenant-ID"] = "tenant-b" },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext { Authorizer = authorizer },
        };

        var result = _context.ResolveTenantId(request, _options, _logger);

        AssertInvalidClaim(result);
    }

    [Fact]
    public void ClaimsObjectWithoutTheTenantClaim_FallsBackToTheAuthorizerEntry()
    {
        var authorizer = new APIGatewayCustomAuthorizerContext
        {
            ["claims"] = JsonDocument.Parse("""{"sub":"user-1"}""").RootElement,
            ["tenant_id"] = "tenant-a",
        };
        var request = new APIGatewayProxyRequest
        {
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext { Authorizer = authorizer },
        };

        Tenant(_context.ResolveTenantId(request, _options, _logger)).ShouldBe("tenant-a");
    }

    [Fact]
    public void UnreadableClaimInAuthorizerEntry_IsALeftWithTheInvalidClaimCode()
    {
        _options.TrustTenantIdHeader = true;
        var authorizer = new APIGatewayCustomAuthorizerContext { ["tenant_id"] = 42 };
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string> { ["X-Tenant-ID"] = "tenant-b" },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext { Authorizer = authorizer },
        };

        AssertInvalidClaim(_context.ResolveTenantId(request, _options, _logger));
    }

    private void AssertInvalidClaim(Either<EncinaError, Option<string>> result)
    {
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.GetCode().IfNone(string.Empty).ShouldBe(LambdaContextExtensions.InvalidTenantClaimErrorCode);
        LambdaContextExtensions.InvalidTenantClaimErrorCode.ShouldBe("tenancy.invalid_tenant_claim");
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(Microsoft.Extensions.Logging.LogLevel.Warning);
        record.Id.Id.ShouldBe(4710);
        record.Message.ShouldContain("tenancy.invalid_tenant_claim");
        record.Message.ShouldNotContain("tenant-b");
    }

    [Fact]
    public void CustomClaimType_IsUsed()
    {
        _options.TenantIdClaimType = "org_id";
        var authorizer = new APIGatewayCustomAuthorizerContext { ["org_id"] = "org-9" };
        var request = new APIGatewayProxyRequest
        {
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext { Authorizer = authorizer },
        };

        Tenant(_context.ResolveTenantId(request, _options, _logger)).ShouldBe("org-9");
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var request = Request();

        Should.Throw<ArgumentNullException>(() => ((ILambdaContext)null!).ResolveTenantId(request, _options, _logger))
            .ParamName.ShouldBe("context");
        Should.Throw<ArgumentNullException>(() => _context.ResolveTenantId(request, null!, _logger))
            .ParamName.ShouldBe("options");
        Should.Throw<ArgumentNullException>(() => _context.ResolveTenantId(request, _options, null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void TrustTenantIdHeader_DefaultsToFalse()
    {
        new EncinaAwsLambdaOptions().TrustTenantIdHeader.ShouldBeFalse();
    }
}
