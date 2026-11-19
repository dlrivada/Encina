using Encina.AspNetCore;
using Encina.UnitTests.Core.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Unit tests for <see cref="ProblemDetailsExtensions"/>: the status, title and extensions come from the
/// <see cref="ErrorCategory"/> of the error through the core HTTP table (ADR-062), never from the error code.
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

    private static ProblemDetailsExtensions.ProblemResult Problem(IResult result) =>
        result.ShouldBeOfType<ProblemDetailsExtensions.ProblemResult>();

    #region ToProblemDetails — Status Code Mapping

    public static TheoryData<ErrorCategory, int> AllCategories => new()
    {
        { ErrorCategory.Internal, 500 },
        { ErrorCategory.Validation, 400 },
        { ErrorCategory.InvalidRequest, 400 },
        { ErrorCategory.Unauthenticated, 401 },
        { ErrorCategory.Forbidden, 403 },
        { ErrorCategory.NotFound, 404 },
        { ErrorCategory.Conflict, 409 },
        { ErrorCategory.AlreadyExists, 409 },
        { ErrorCategory.FailedPrecondition, 400 },
        { ErrorCategory.RateLimited, 429 },
        { ErrorCategory.Unavailable, 503 },
        { ErrorCategory.Timeout, 504 },
        { ErrorCategory.UpstreamFailure, 502 },
        { ErrorCategory.Cancelled, 499 },
    };

    [Theory]
    [MemberData(nameof(AllCategories))]
    public void ToProblemDetails_EveryCategory_AnswersTheStatusOfTheCoreTable(ErrorCategory category, int expected)
    {
        var error = EncinaErrors.Create("test.categorized", category, "message");

        var result = Problem(error.ToProblemDetails(CreateHttpContext()));

        result.StatusCode.ShouldBe(expected);
        result.Inner.ProblemDetails.Status.ShouldBe(expected);
        result.Inner.ProblemDetails.Title.ShouldBe(ErrorCategoryHttp.GetReasonPhrase(expected));
    }

    [Fact]
    public void ToProblemDetails_CoversEveryCategoryOfTheEnum() =>
        AllCategories.Select(row => row.Data.Item1).ShouldBe(Enum.GetValues<ErrorCategory>(), ignoreOrder: true);

    // One row per rule of the legacy code mapping: the code text no longer decides, the category does.
    [Theory]
    [InlineData("validation.field_required", ErrorCategory.Validation, 400)]
    [InlineData("encina.guard.validation_failed", ErrorCategory.Validation, 400)]
    [InlineData(EncinaErrorCodes.AuthorizationUnauthenticated, ErrorCategory.Unauthenticated, 401)]
    [InlineData(EncinaErrorCodes.AuthorizationForbidden, ErrorCategory.Forbidden, 403)]
    [InlineData(EncinaErrorCodes.AuthorizationResourceDenied, ErrorCategory.Forbidden, 403)]
    [InlineData("entity.not_found", ErrorCategory.NotFound, 404)]
    [InlineData("encina.request.handler_missing", ErrorCategory.NotFound, 404)]
    [InlineData("resource.missing", ErrorCategory.NotFound, 404)]
    [InlineData("entity.conflict", ErrorCategory.Conflict, 409)]
    [InlineData("user.already_exists", ErrorCategory.AlreadyExists, 409)]
    [InlineData("order.duplicate", ErrorCategory.Conflict, 409)]
    public void ToProblemDetails_CategorizedCodeFamilies_AnswerTheStatusOfTheirCategory(
        string code, ErrorCategory category, int expected)
    {
        var error = EncinaErrors.Create(code, category, "message");

        Problem(error.ToProblemDetails(CreateHttpContext())).StatusCode.ShouldBe(expected);
    }

    [Fact]
    public void ToProblemDetails_CategoryDecidesNotTheCodeText()
    {
        // A "not_found" code under the Forbidden category answers 403.
        var error = EncinaErrors.Create("entity.not_found", ErrorCategory.Forbidden, "message");

        Problem(error.ToProblemDetails(CreateHttpContext())).StatusCode.ShouldBe(403);
    }

    [Fact]
    public void ToProblemDetails_ErrorWithoutCode_Answers500()
    {
        var error = EncinaError.New("no code");

        var result = Problem(error.ToProblemDetails(CreateHttpContext()));

        result.StatusCode.ShouldBe(500);
        result.Inner.ProblemDetails.Title.ShouldBe("Internal Server Error");
    }

    [Fact]
    public void ToProblemDetails_UncategorizedUnknownCode_Answers500()
    {
        var error = EncinaErrors.Create("unknown.error", "Something went wrong");

        Problem(error.ToProblemDetails(CreateHttpContext())).StatusCode.ShouldBe(500);
    }

    [Fact]
    public void ToProblemDetails_ExplicitStatusCode_OverridesTheCategory()
    {
        var error = EncinaErrors.Create("test.error", ErrorCategory.NotFound, "test");

        var result = Problem(error.ToProblemDetails(CreateHttpContext(), 422));

        result.StatusCode.ShouldBe(422);
        result.Inner.ProblemDetails.Title.ShouldBe("Unprocessable Entity");
    }

    [Fact]
    public void ToProblemDetails_ExplicitStatusCodeOutsideTheTable_PassesThroughWithTheGenericTitle()
    {
        var error = EncinaErrors.Create("test.error", ErrorCategory.NotFound, "test");

        var result = Problem(error.ToProblemDetails(CreateHttpContext(), 410));

        result.StatusCode.ShouldBe(410);
        result.Inner.ProblemDetails.Title.ShouldBe("An error occurred");
    }

    [Fact]
    public void ToProblemDetails_KeepsTheErrorCodeInTheBody()
    {
        var error = EncinaErrors.Create("order.not_found", ErrorCategory.NotFound, "message");

        var problem = Problem(error.ToProblemDetails(CreateHttpContext())).Inner.ProblemDetails;

        problem.Extensions["errorCode"].ShouldBe("order.not_found");
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
                var problem = Problem(EncinaErrors.Create("test.error", "test").ToProblemDetails(ctx)).Inner.ProblemDetails;
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

        var problem = Problem(EncinaErrors.Create("test.error", "test").ToProblemDetails(context)).Inner.ProblemDetails;

        problem.Extensions.ContainsKey("correlationId").ShouldBeFalse();
    }

    [Fact]
    public void ToProblemDetails_WithoutAccessorRegistered_OmitsTheCorrelationIdAndNeverEchoesTheHeader()
    {
        var context = CreateHttpContext();
        context.Request.Headers[CorrelationHeader] = "client-supplied";

        var problem = Problem(EncinaErrors.Create("test.error", "test").ToProblemDetails(context)).Inner.ProblemDetails;

        problem.Extensions.ContainsKey("correlationId").ShouldBeFalse();
    }

    [Fact]
    public void ToProblemDetails_WithTraceId_IncludesInExtensions()
    {
        var error = EncinaErrors.Create("test.error", "test");
        var context = CreateHttpContext();
        context.TraceIdentifier = "trace-abc";

        var problem = Problem(error.ToProblemDetails(context)).Inner.ProblemDetails;

        problem.Extensions["traceId"].ShouldBe("trace-abc");
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
