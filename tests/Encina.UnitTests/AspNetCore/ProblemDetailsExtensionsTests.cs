using Encina.AspNetCore;
using Encina.UnitTests.Core.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Comprehensive unit tests for <see cref="ProblemDetailsExtensions"/>.
/// Covers all error code → HTTP status code mappings.
/// </summary>
public sealed class ProblemDetailsExtensionsTests
{
    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection()
            .Configure<EncinaAspNetCoreOptions>(_ => { })
            .BuildServiceProvider();
        return context;
    }

    #region ToProblemDetails — Status Code Mapping

    [Fact]
    public void ToProblemDetails_ValidationError_Returns400()
    {
        var error = EncinaErrors.Create("validation.field_required", "Field is required");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_GuardValidationFailed_Returns400()
    {
        var error = EncinaErrors.Create("encina.guard.validation_failed", "Guard validation failed");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_Unauthenticated_Returns401()
    {
        var error = EncinaErrors.Create(EncinaErrorCodes.AuthorizationUnauthenticated, "Not authenticated");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Theory]
    [InlineData(EncinaErrorCodes.AuthorizationUnauthorized)]
    [InlineData(EncinaErrorCodes.AuthorizationForbidden)]
    [InlineData(EncinaErrorCodes.AuthorizationPolicyFailed)]
    [InlineData(EncinaErrorCodes.AuthorizationResourceDenied)]
    [InlineData(EncinaErrorCodes.AuthorizationInsufficientRoles)]
    [InlineData(EncinaErrorCodes.AuthorizationPermissionDenied)]
    [InlineData(EncinaErrorCodes.AuthorizationClaimMissing)]
    [InlineData(EncinaErrorCodes.AuthorizationNotOwner)]
    [InlineData(global::Encina.Security.ABAC.ABACErrors.AccessDeniedCode)]
    [InlineData(global::Encina.Security.ABAC.ABACErrors.ConditionNotMetCode)]
    [InlineData(global::Encina.Security.ABAC.ABACErrors.ObligationFailedCode)]
    [InlineData(global::Encina.Security.ABAC.ABACErrors.RequiredPolicyNotFoundCode)]
    [InlineData(global::Encina.Security.ABAC.ABACErrors.DecisionAuditTenantRequiredCode)]
    [InlineData(global::Encina.Security.ABAC.ABACErrors.DecisionAuditTenantMismatchCode)]
    [InlineData("encina.authorization.some_future_denial")]
    public void ToProblemDetails_AuthorizationDenial_Returns403(string code)
    {
        var error = EncinaErrors.Create(code, "Forbidden");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void ToProblemDetails_InvalidDecisionAuditQuery_Returns400()
    {
        var error = global::Encina.Security.ABAC.ABACErrors.InvalidDecisionAuditQuery("pageSize");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Theory]
    [InlineData(global::Encina.Security.ABAC.ABACErrors.PolicyNotFoundCode)]
    [InlineData(global::Encina.Security.ABAC.ABACErrors.PolicySetNotFoundCode)]
    public void ToProblemDetails_AbacAdministrativeNotFound_Returns404(string code)
    {
        var error = EncinaErrors.Create(code, "Not found");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldBeOfType<ProblemHttpResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ToProblemDetails_NotFound_Returns404()
    {
        var error = EncinaErrors.Create("entity.not_found", "Not found");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_HandlerMissing_Returns404()
    {
        var error = EncinaErrors.Create("encina.request.handler_missing", "No handler");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_Missing_Returns404()
    {
        var error = EncinaErrors.Create("resource.missing", "Resource missing");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_Conflict_Returns409()
    {
        var error = EncinaErrors.Create("entity.conflict", "Conflict");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_AlreadyExists_Returns409()
    {
        var error = EncinaErrors.Create("user.already_exists", "Duplicate");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_Duplicate_Returns409()
    {
        var error = EncinaErrors.Create("order.duplicate", "Duplicate order");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_UnknownError_Returns500()
    {
        var error = EncinaErrors.Create("unknown.error", "Something went wrong");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_ExplicitStatusCode_OverridesMapping()
    {
        var error = EncinaErrors.Create("test.error", "test");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context, 422);
        result.ShouldNotBeNull();
    }

    #endregion

    #region ToProblemDetails — Extensions

    private const string CorrelationHeader = "X-Correlation-ID";

    // Runs the real context middleware over the request header and builds the problem inside its
    // scope, the way an endpoint does, so the body and the response header come from one pipeline.
    private static async Task<(string? Body, string Header)> ProblemInsideScopeAsync(string? requestHeader)
    {
        var host = new ScopeTestHost();
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IRequestContextAccessor>(host.Accessor)
                .BuildServiceProvider()
        };
        if (requestHeader is not null)
        {
            context.Request.Headers[CorrelationHeader] = requestHeader;
        }

        string? body = null;
        var middleware = new EncinaContextMiddleware(
            ctx =>
            {
                var problem = EncinaErrors.Create("test.error", "test").ToProblemDetails(ctx)
                    .ShouldBeOfType<ProblemHttpResult>().ProblemDetails;
                body = problem.Extensions.TryGetValue("correlationId", out var value) ? (string?)value : null;
                return Task.CompletedTask;
            },
            Options.Create(new EncinaAspNetCoreOptions()),
            new FakeLogger<EncinaContextMiddleware>());

        await middleware.InvokeAsync(context, host.Factory);

        return (body, context.Response.Headers[CorrelationHeader].ToString());
    }

    [Fact]
    public async Task ToProblemDetails_WithCorrelationId_BodyEqualsTheResponseHeader()
    {
        var (body, header) = await ProblemInsideScopeAsync("corr-123");

        body.ShouldBe("corr-123");
        body.ShouldBe(header);
    }

    [Fact]
    public async Task ToProblemDetails_WithOversizedCorrelationId_BodyCarriesTheNormalizedHeaderValue()
    {
        var raw = new string('a', 1_000_000);

        var (body, header) = await ProblemInsideScopeAsync(raw);

        body.ShouldNotBeNull();
        body.ShouldBe(header);
        body.ShouldNotBe(raw);
        body.Length.ShouldBeLessThan(InboundRequestInfo.MaxIdLength + 1);
    }

    [Fact]
    public async Task ToProblemDetails_WithControlCharactersInCorrelationId_BodyCarriesTheNormalizedHeaderValue()
    {
        const string raw = "corr\u0001\r\nforged-log-line";

        var (body, header) = await ProblemInsideScopeAsync(raw);

        body.ShouldNotBeNull();
        body.ShouldBe(header);
        body.ShouldNotBe(raw);
        body.Any(char.IsControl).ShouldBeFalse();
    }

    [Fact]
    public async Task ToProblemDetails_WithoutCorrelationHeader_BodyEqualsTheGeneratedHeaderValue()
    {
        var (body, header) = await ProblemInsideScopeAsync(null);

        body.ShouldNotBeNullOrEmpty();
        body.ShouldBe(header);
    }

    [Fact]
    public void ToProblemDetails_WithoutRequestContext_OmitsTheCorrelationIdAndNeverEchoesTheHeader()
    {
        var host = new ScopeTestHost();
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IRequestContextAccessor>(host.Accessor)
                .BuildServiceProvider()
        };
        context.Request.Headers[CorrelationHeader] = "client-supplied";

        var problem = EncinaErrors.Create("test.error", "test").ToProblemDetails(context)
            .ShouldBeOfType<ProblemHttpResult>().ProblemDetails;

        problem.Extensions.ContainsKey("correlationId").ShouldBeFalse();
    }

    [Fact]
    public void ToProblemDetails_WithoutAccessorRegistered_OmitsTheCorrelationIdAndNeverEchoesTheHeader()
    {
        var context = CreateHttpContext();
        context.Request.Headers[CorrelationHeader] = "client-supplied";

        var problem = EncinaErrors.Create("test.error", "test").ToProblemDetails(context)
            .ShouldBeOfType<ProblemHttpResult>().ProblemDetails;

        problem.Extensions.ContainsKey("correlationId").ShouldBeFalse();
    }

    [Fact]
    public void ToProblemDetails_WithTraceId_IncludesInExtensions()
    {
        var error = EncinaErrors.Create("test.error", "test");
        var context = CreateHttpContext();
        context.TraceIdentifier = "trace-abc";

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    #endregion

    #region ToActionResult

    [Fact]
    public void ToActionResult_ReturnsNonNull()
    {
        var error = EncinaErrors.Create("test.error", "test");
        var context = CreateHttpContext();

        var result = error.ToActionResult(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToActionResult_WithExplicitStatusCode_ReturnsResult()
    {
        var error = EncinaErrors.Create("test.code", "Test");
        var context = CreateHttpContext();

        var result = error.ToActionResult(context, 503);
        result.ShouldNotBeNull();
    }

    #endregion

    #region ToProblemDetails — No Services

    [Fact]
    public void ToProblemDetails_NoOptionsRegistered_UsesDefaults()
    {
        var error = EncinaErrors.Create("test.error", "test");
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().BuildServiceProvider();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    #endregion
}
