using System.Security.Claims;
using Encina.AzureFunctions;
using LanguageExt;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Testing;
using EncinaExtensions = Encina.AzureFunctions.FunctionContextExtensions;

namespace Encina.UnitTests.AzureFunctions;

/// <summary>
/// The verified tenant claim always wins in <see cref="EncinaExtensions.ResolveTenantId"/> (#2399).
/// </summary>
public class FunctionResolveTenantIdTests
{
    private readonly FunctionContext _context = Substitute.For<FunctionContext>();
    private readonly FakeLogger _logger = new();
    private readonly EncinaAzureFunctionsOptions _options = new();

    private HttpRequestData Request(string? header = null, string? claim = null, string claimType = "tenant_id")
    {
        var request = Substitute.For<HttpRequestData>(_context);
        var headers = new HttpHeadersCollection();
        if (header is not null)
        {
            headers.Add("X-Tenant-ID", header);
        }

        request.Headers.Returns(headers);
        request.Identities.Returns(claim is null ? [] : [new ClaimsIdentity([new Claim(claimType, claim)])]);
        return request;
    }

    private const string NoTenant = "<none>";

    private static string Tenant(Either<EncinaError, Option<string>> result) =>
        result.Match(Right: o => o.IfNone(NoTenant), Left: _ => "<left>");

    [Fact]
    public void Claim_WithNoHeader_ReturnsClaim()
    {
        Tenant(_context.ResolveTenantId(Request(claim: "tenant-a"), _options, _logger)).ShouldBe("tenant-a");
    }

    [Fact]
    public void CustomClaimType_IsUsed()
    {
        _options.TenantIdClaimType = "tid";

        Tenant(_context.ResolveTenantId(Request(claim: "tenant-a", claimType: "tid"), _options, _logger)).ShouldBe("tenant-a");
    }

    [Fact]
    public void ClaimAndSameHeader_ReturnsClaim()
    {
        var result = _context.ResolveTenantId(Request(header: "tenant-a", claim: "tenant-a"), _options, _logger);

        Tenant(result).ShouldBe("tenant-a");
        _logger.Collector.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClaimAndDifferingHeader_IsALeftWithTheMismatchCode_EvenWhenTheHeaderIsTrusted(bool trust)
    {
        _options.TrustTenantIdHeader = trust;

        var result = _context.ResolveTenantId(Request(header: "tenant-b", claim: "tenant-a"), _options, _logger);

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.GetCode().IfNone(string.Empty).ShouldBe(EncinaExtensions.TenantMismatchErrorCode);
        EncinaExtensions.TenantMismatchErrorCode.ShouldBe("tenancy.tenant_mismatch");
    }

    [Fact]
    public void Mismatch_LogsAWarningWithTheCodeAndNoTenantValue()
    {
        _context.ResolveTenantId(Request(header: "tenant-b", claim: "tenant-a"), _options, _logger);

        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Id.Id.ShouldBe(4755);
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
        record.Level.ShouldBe(LogLevel.Warning);
        record.Id.Id.ShouldBe(4756);
        record.Message.ShouldNotContain("tenant-b");
    }

    [Fact]
    public void NoClaimAndUntrustedHeader_IsIgnoredAndLogsDebug()
    {
        var result = _context.ResolveTenantId(Request(header: "tenant-b"), _options, _logger);

        result.IsRight.ShouldBeTrue();
        Tenant(result).ShouldBe(NoTenant);
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Debug);
        record.Id.Id.ShouldBe(4757);
        record.Message.ShouldNotContain("tenant-b");
    }

    [Fact]
    public void EmptyHeader_IsTreatedAsAbsent()
    {
        _options.TrustTenantIdHeader = true;

        var result = _context.ResolveTenantId(Request(header: ""), _options, _logger);

        Tenant(result).ShouldBe(NoTenant);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyClaim_IsALeftWithTheInvalidClaimCode_NeverTheHeader(bool trust)
    {
        _options.TrustTenantIdHeader = trust;

        var result = _context.ResolveTenantId(Request(header: "tenant-b", claim: ""), _options, _logger);

        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default, Left: e => e);
        error.GetCode().IfNone(string.Empty).ShouldBe(EncinaExtensions.InvalidTenantClaimErrorCode);
        EncinaExtensions.InvalidTenantClaimErrorCode.ShouldBe("tenancy.invalid_tenant_claim");
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Id.Id.ShouldBe(4758);
        record.Message.ShouldContain("tenancy.invalid_tenant_claim");
        record.Message.ShouldNotContain("tenant-b");
    }

    [Fact]
    public void EmptyClaimNextToAValidOne_UsesTheValidClaim()
    {
        var request = Request(claim: "");
        request.Identities.Returns([new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim("tenant_id", ""), new System.Security.Claims.Claim("tenant_id", "tenant-a")])]);

        Tenant(_context.ResolveTenantId(request, _options, _logger)).ShouldBe("tenant-a");
    }

    [Fact]
    public void NothingPresent_ReturnsNone()
    {
        Tenant(_context.ResolveTenantId(Request(), _options, _logger)).ShouldBe(NoTenant);
        _logger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var request = Request();

        Should.Throw<ArgumentNullException>(() => EncinaExtensions.ResolveTenantId(null!, request, _options, _logger))
            .ParamName.ShouldBe("context");
        Should.Throw<ArgumentNullException>(() => _context.ResolveTenantId(null!, _options, _logger))
            .ParamName.ShouldBe("httpRequest");
        Should.Throw<ArgumentNullException>(() => _context.ResolveTenantId(request, null!, _logger))
            .ParamName.ShouldBe("options");
        Should.Throw<ArgumentNullException>(() => _context.ResolveTenantId(request, _options, null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void TrustTenantIdHeader_DefaultsToFalse()
    {
        new EncinaAzureFunctionsOptions().TrustTenantIdHeader.ShouldBeFalse();
    }
}
