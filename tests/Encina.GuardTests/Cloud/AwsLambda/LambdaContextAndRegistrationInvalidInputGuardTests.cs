using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Encina.AwsLambda.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.GuardTests.Cloud.AwsLambda;

/// <summary>
/// Guard tests for <see cref="LambdaContextExtensions"/>, <see cref="AwsLambdaHealthCheck"/> and
/// <see cref="ServiceCollectionExtensions"/>: missing request data, invalid claims and
/// repeated or conflicting registrations.
/// </summary>
public sealed class LambdaContextAndRegistrationInvalidInputGuardTests
{
    private static ILambdaContext Context(string requestId = "req-1")
    {
        var context = Substitute.For<ILambdaContext>();
        context.AwsRequestId.Returns(requestId);
        context.FunctionName.Returns("fn");
        context.RemainingTime.Returns(TimeSpan.FromMilliseconds(1500));
        return context;
    }

    private static APIGatewayProxyRequest RequestWithAuthorizer(string key, object? value)
    {
        var authorizer = new APIGatewayCustomAuthorizerContext();
        authorizer[key] = value;
        return new APIGatewayProxyRequest
        {
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext { Authorizer = authorizer }
        };
    }

    #region GetCorrelationId

    [Fact]
    public void GetCorrelationId_V2Overload_NullContext_Throws()
    {
        ILambdaContext context = null!;

        var action = () => context.GetCorrelationId(new APIGatewayHttpApiV2ProxyRequest());

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("context");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetCorrelationId_MissingOrEmptyHeader_FallsBackToAwsRequestId(bool emptyHeader)
    {
        var request = new APIGatewayProxyRequest
        {
            Headers = emptyHeader ? new Dictionary<string, string> { ["X-Correlation-ID"] = string.Empty } : null
        };

        Context().GetCorrelationId(request).ShouldBe("req-1");
        Context().GetCorrelationId(request: (APIGatewayProxyRequest?)null).ShouldBe("req-1");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetCorrelationId_V2_MissingOrEmptyHeader_FallsBackToAwsRequestId(bool emptyHeader)
    {
        var request = new APIGatewayHttpApiV2ProxyRequest
        {
            Headers = emptyHeader ? new Dictionary<string, string> { ["X-Correlation-ID"] = string.Empty } : null
        };

        Context().GetCorrelationId(request).ShouldBe("req-1");
        Context().GetCorrelationId((APIGatewayHttpApiV2ProxyRequest?)null).ShouldBe("req-1");
    }

    [Fact]
    public void GetCorrelationId_CustomHeaderName_IsHonoured()
    {
        var options = new EncinaAwsLambdaOptions { CorrelationIdHeader = "X-Trace" };
        var request = new APIGatewayProxyRequest { Headers = new Dictionary<string, string> { ["X-Trace"] = "t-1" } };
        var v2 = new APIGatewayHttpApiV2ProxyRequest { Headers = new Dictionary<string, string> { ["X-Trace"] = "t-2" } };

        Context().GetCorrelationId(request, options).ShouldBe("t-1");
        Context().GetCorrelationId(v2, options).ShouldBe("t-2");
    }

    #endregion

    #region ResolveTenantId

    [Fact]
    public void ResolveTenantId_NullRequest_ReturnsNone()
    {
        var result = Context().ResolveTenantId(null, new EncinaAwsLambdaOptions(), NullLogger.Instance);

        result.IsRight.ShouldBeTrue();
        result.IfRight(o => o.IsNone.ShouldBeTrue());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ResolveTenantId_EmptyOrNullClaim_IsRejectedAsInvalidClaim(string? claim)
    {
        var request = RequestWithAuthorizer("tenant_id", claim);

        var result = Context().ResolveTenantId(request, new EncinaAwsLambdaOptions(), NullLogger.Instance);

        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe(LambdaContextExtensions.InvalidTenantClaimErrorCode));
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ResolveTenantId_NonStringClaim_IsRejectedAsInvalidClaim()
    {
        var request = RequestWithAuthorizer("tenant_id", 42);

        var result = Context().ResolveTenantId(request, new EncinaAwsLambdaOptions(), NullLogger.Instance);

        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe(LambdaContextExtensions.InvalidTenantClaimErrorCode));
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ResolveTenantId_HeaderDifferentFromClaim_IsRejectedAsMismatch()
    {
        var request = RequestWithAuthorizer("tenant_id", "t-1");
        request.Headers = new Dictionary<string, string> { ["X-Tenant-ID"] = "t-2" };

        var result = Context().ResolveTenantId(request, new EncinaAwsLambdaOptions(), NullLogger.Instance);

        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe(LambdaContextExtensions.TenantMismatchErrorCode));
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ResolveTenantId_HeaderMatchingClaim_ResolvesTheClaim()
    {
        var request = RequestWithAuthorizer("tenant_id", "t-1");
        request.Headers = new Dictionary<string, string> { ["X-Tenant-ID"] = "t-1" };

        var result = Context().ResolveTenantId(request, new EncinaAwsLambdaOptions(), NullLogger.Instance);

        result.IfRight(o => o.IfNone(string.Empty).ShouldBe("t-1"));
        result.IsRight.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResolveTenantId_HeaderOnly_IsIgnoredUnlessTrusted(bool trusted)
    {
        var request = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string> { ["X-Tenant-ID"] = "t-9" }
        };
        var options = new EncinaAwsLambdaOptions { TrustTenantIdHeader = trusted };

        var result = Context().ResolveTenantId(request, options, NullLogger.Instance);

        result.IfRight(o => o.IsSome.ShouldBe(trusted));
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void ResolveTenantId_JsonClaimsObjectWithEmptyString_IsRejected()
    {
        using var doc = JsonDocument.Parse("{\"tenant_id\":\"\"}");
        var request = RequestWithAuthorizer("claims", doc.RootElement.Clone());

        var result = Context().ResolveTenantId(request, new EncinaAwsLambdaOptions(), NullLogger.Instance);

        result.IfLeft(e => e.GetCode().IfNone(string.Empty).ShouldBe(LambdaContextExtensions.InvalidTenantClaimErrorCode));
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ResolveTenantId_JsonClaimsObjectWithoutTheClaim_ResolvesNone()
    {
        using var doc = JsonDocument.Parse("{\"other\":\"x\"}");
        var request = RequestWithAuthorizer("claims", doc.RootElement.Clone());

        var result = Context().ResolveTenantId(request, new EncinaAwsLambdaOptions(), NullLogger.Instance);

        result.IfRight(o => o.IsNone.ShouldBeTrue());
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region GetUserId, context accessors

    [Fact]
    public void GetUserId_NullRequestOrNoAuthorizer_ReturnsNull()
    {
        Context().GetUserId().ShouldBeNull();
        Context().GetUserId(new APIGatewayProxyRequest()).ShouldBeNull();
    }

    [Fact]
    public void GetUserId_AuthorizerWithoutMatchingEntries_ReturnsNull()
    {
        Context().GetUserId(RequestWithAuthorizer("unrelated", "x")).ShouldBeNull();
        Context().GetUserId(RequestWithAuthorizer("sub", 5)).ShouldBeNull();
    }

    [Fact]
    public void GetUserId_ClaimTypeFromOptions_IsUsed()
    {
        var options = new EncinaAwsLambdaOptions { UserIdClaimType = "uid" };

        Context().GetUserId(RequestWithAuthorizer("uid", "u-1"), options).ShouldBe("u-1");
        Context().GetUserId(RequestWithAuthorizer("principalId", "p-1"), options).ShouldBe("p-1");
    }

    [Fact]
    public void ContextAccessors_ReturnTheContextValues()
    {
        var context = Context("req-9");

        context.GetAwsRequestId().ShouldBe("req-9");
        context.GetFunctionName().ShouldBe("fn");
        context.GetRemainingTimeMs().ShouldBe(1500);
    }

    #endregion

    #region AwsLambdaHealthCheck

    [Fact]
    public async Task HealthCheck_WithDefaultOptions_ReportsHealthyAndConfiguration()
    {
        var check = new AwsLambdaHealthCheck(Options.Create(new EncinaAwsLambdaOptions { CorrelationIdHeader = "X-Trace" }));

        var result = await check.CheckHealthAsync(CancellationToken.None);

        check.Name.ShouldBe("aws-lambda");
        check.Tags.ShouldContain("lambda");
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["correlationIdHeader"].ShouldBe("X-Trace");
        result.Data.ShouldContainKey("isInLambdaEnvironment");
    }

    // Pins accidental behaviour, not a contract: update this test when a production guard clause is added (see src/Encina.AwsLambda/Health/AwsLambdaHealthCheck.cs:42).
    [Fact]
    public void HealthCheck_OptionsWrapperWithNullValue_ThrowsNullReferenceOnCheck()
    {
        var options = Substitute.For<IOptions<EncinaAwsLambdaOptions>>();
        options.Value.Returns((EncinaAwsLambdaOptions)null!);
        var check = new AwsLambdaHealthCheck(options);

        var action = () => check.CheckHealthAsync(CancellationToken.None);

        Should.Throw<NullReferenceException>(action);
    }

    #endregion

    #region AddEncinaAwsLambda

    [Fact]
    public void AddEncinaAwsLambda_ParameterlessOverload_RegistersTheHealthCheck()
    {
        var services = new ServiceCollection();

        services.AddEncinaAwsLambda();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetServices<IEncinaHealthCheck>().OfType<AwsLambdaHealthCheck>().Count().ShouldBe(1);
    }

    [Fact]
    public void AddEncinaAwsLambda_CalledTwice_RegistersOneHealthCheckAndLastOptionsWin()
    {
        var services = new ServiceCollection();

        services.AddEncinaAwsLambda(o => o.CorrelationIdHeader = "X-First");
        services.AddEncinaAwsLambda(o => o.CorrelationIdHeader = "X-Second");

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetServices<IEncinaHealthCheck>().OfType<AwsLambdaHealthCheck>().Count().ShouldBe(1);
        provider.GetRequiredService<IOptions<EncinaAwsLambdaOptions>>().Value.CorrelationIdHeader.ShouldBe("X-Second");
    }

    [Fact]
    public void AddEncinaAwsLambda_ThrowingConfigureDelegate_SurfacesWhenOptionsAreResolved()
    {
        var services = new ServiceCollection();
        services.AddEncinaAwsLambda(_ => throw new InvalidOperationException("bad options"));
        using var provider = services.BuildServiceProvider();

        var action = () => provider.GetRequiredService<IOptions<EncinaAwsLambdaOptions>>().Value;

        Should.Throw<InvalidOperationException>(action);
    }

    #endregion
}
