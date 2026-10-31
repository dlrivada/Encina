using System.Security.Claims;
using Encina.AzureFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Testing;
using FunctionContextExtensions = Encina.AzureFunctions.FunctionContextExtensions;

namespace Encina.IntegrationTests.Cloud.AzureFunctions;

/// <summary>
/// Drives <see cref="FunctionContextExtensions"/> with HTTP requests that carry real headers and claims identities.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cloud", "AzureFunctions")]
public sealed class FunctionContextIntegrationTests
{
    private static TestFunctionContext NewContext() => new(new ServiceCollection().BuildServiceProvider(), "PlaceOrder", "inv-1");

    private static TestHttpRequestData NewRequest(
        FunctionContext context,
        string? header = null,
        string? headerValue = null,
        params Claim[] claims) =>
        new(
            context,
            headers: header is null ? null : [new KeyValuePair<string, string>(header, headerValue!)],
            identities: claims.Length == 0 ? null : [new ClaimsIdentity(claims, "test")]);

    private static string Tenant(LanguageExt.Either<EncinaError, LanguageExt.Option<string>> result) =>
        result.Match(
            Right: tenant => tenant.IfNone("<none>"),
            Left: error => "error:" + error.GetCode().IfNone(string.Empty));

    [Fact]
    public void GetCorrelationId_PrefersTheRequestHeaderOverTheContextItem()
    {
        // Arrange
        var context = NewContext();
        context.Items["Encina.CorrelationId"] = "from-context";
        var request = NewRequest(context, "X-Correlation-ID", "from-header");

        // Act & Assert
        context.GetCorrelationId(request).ShouldBe("from-header");
        context.GetCorrelationId(NewRequest(context)).ShouldBe("from-context");
        context.GetCorrelationId().ShouldBe("from-context");
    }

    [Fact]
    public void GetCorrelationId_HonoursTheConfiguredHeaderAndReturnsNullWithoutAny()
    {
        // Arrange
        var context = NewContext();
        var request = NewRequest(context, "X-Request-ID", "req-1");
        var options = new EncinaAzureFunctionsOptions { CorrelationIdHeader = "X-Request-ID" };

        // Act & Assert
        context.GetCorrelationId(request, options).ShouldBe("req-1");
        context.GetCorrelationId(NewRequest(context)).ShouldBeNull();
    }

    [Fact]
    public void GetCorrelationId_WithAnEmptyRequestHeader_FallsBackToTheContextItem()
    {
        // Arrange
        var context = NewContext();
        context.Items["Encina.CorrelationId"] = "from-context";
        var request = NewRequest(context, "X-Correlation-ID", string.Empty);

        // Act
        var correlationId = context.GetCorrelationId(request);

        // Assert
        correlationId.ShouldBe("from-context");
    }

    [Fact]
    public void GetUserId_ReadsTheNameIdentifierClaimThenTheContextItem()
    {
        // Arrange
        var context = NewContext();
        var withClaim = NewRequest(context, claims: new Claim(ClaimTypes.NameIdentifier, "user-1"));

        // Act & Assert
        context.GetUserId(withClaim).ShouldBe("user-1");
        context.GetUserId(NewRequest(context)).ShouldBeNull();
        context.Items["Encina.UserId"] = "from-context";
        context.GetUserId(NewRequest(context)).ShouldBe("from-context");
        context.GetUserId().ShouldBe("from-context");
    }

    [Fact]
    public void GetUserId_HonoursTheConfiguredClaimType()
    {
        // Arrange
        var context = NewContext();
        var request = NewRequest(context, claims: new Claim("oid", "user-9"));

        // Act
        var userId = context.GetUserId(request, new EncinaAzureFunctionsOptions { UserIdClaimType = "oid" });

        // Assert
        userId.ShouldBe("user-9");
    }

    [Fact]
    public void ResolveTenantId_FromTheVerifiedClaim_ReturnsTheTenant()
    {
        // Arrange
        var context = NewContext();
        var request = NewRequest(context, claims: new Claim("tenant_id", "acme"));

        // Act
        var result = context.ResolveTenantId(request, new EncinaAzureFunctionsOptions(), new FakeLogger());

        // Assert
        Tenant(result).ShouldBe("acme");
    }

    [Fact]
    public void ResolveTenantId_WhenTheHeaderDisagreesWithTheClaim_ReturnsTheMismatchErrorAndLogsOnlyTheCode()
    {
        // Arrange
        var context = NewContext();
        var request = NewRequest(context, "X-Tenant-ID", "other", new Claim("tenant_id", "acme"));
        var logger = new FakeLogger();

        // Act
        var result = context.ResolveTenantId(request, new EncinaAzureFunctionsOptions(), logger);

        // Assert
        Tenant(result).ShouldBe("error:" + FunctionContextExtensions.TenantMismatchErrorCode);
        var entry = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Message.ShouldNotContain("acme");
        entry.Message.ShouldNotContain("other");
    }

    [Fact]
    public void ResolveTenantId_WithAnEmptyClaim_FailsClosedInsteadOfUsingTheHeader()
    {
        // Arrange
        var context = NewContext();
        var request = NewRequest(context, "X-Tenant-ID", "acme", new Claim("tenant_id", string.Empty));

        // Act
        var result = context.ResolveTenantId(
            request, new EncinaAzureFunctionsOptions { TrustTenantIdHeader = true }, new FakeLogger());

        // Assert
        Tenant(result).ShouldBe("error:" + FunctionContextExtensions.InvalidTenantClaimErrorCode);
    }

    [Theory]
    [InlineData(false, "<none>", LogLevel.Debug)]
    [InlineData(true, "acme", LogLevel.Warning)]
    public void ResolveTenantId_WithOnlyAHeader_UsesItOnlyWhenTheHeaderIsTrusted(
        bool trust, string expectedTenant, LogLevel expectedLevel)
    {
        // Arrange
        var context = NewContext();
        var request = NewRequest(context, "X-Tenant-ID", "acme");
        var logger = new FakeLogger();

        // Act
        var result = context.ResolveTenantId(
            request, new EncinaAzureFunctionsOptions { TrustTenantIdHeader = trust }, logger);

        // Assert
        Tenant(result).ShouldBe(expectedTenant);
        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(expectedLevel);
    }

    [Fact]
    public void ResolveTenantId_WithoutAnyTenantInformation_ReturnsNoTenant()
    {
        // Arrange
        var context = NewContext();

        // Act
        var result = context.ResolveTenantId(NewRequest(context), new EncinaAzureFunctionsOptions(), new FakeLogger());

        // Assert
        Tenant(result).ShouldBe("<none>");
    }
}
