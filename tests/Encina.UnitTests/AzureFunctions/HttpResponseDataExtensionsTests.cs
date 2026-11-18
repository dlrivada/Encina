using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Encina.AzureFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Encina.UnitTests.AzureFunctions;

/// <summary>
/// Unit tests of the response helpers: the status, the title and the headers come from the error category
/// through <see cref="ErrorCategoryHttp"/>, never from the text of the error code.
/// </summary>
public class HttpResponseDataExtensionsTests
{
    private static readonly (ErrorCategory Category, int Status, string Title)[] Rows =
    [
        (ErrorCategory.Internal, 500, "Internal Server Error"),
        (ErrorCategory.Validation, 400, "Bad Request"),
        (ErrorCategory.InvalidRequest, 400, "Bad Request"),
        (ErrorCategory.Unauthenticated, 401, "Unauthorized"),
        (ErrorCategory.Forbidden, 403, "Forbidden"),
        (ErrorCategory.NotFound, 404, "Not Found"),
        (ErrorCategory.Conflict, 409, "Conflict"),
        (ErrorCategory.AlreadyExists, 409, "Conflict"),
        (ErrorCategory.FailedPrecondition, 400, "Bad Request"),
        (ErrorCategory.RateLimited, 429, "Too Many Requests"),
        (ErrorCategory.Unavailable, 503, "Service Unavailable"),
        (ErrorCategory.Timeout, 504, "Gateway Timeout"),
        (ErrorCategory.UpstreamFailure, 502, "Bad Gateway"),
        (ErrorCategory.Cancelled, 499, "Client Closed Request"),
    ];

    public static TheoryData<ErrorCategory, int, string> CategoryRows
    {
        get
        {
            var data = new TheoryData<ErrorCategory, int, string>();
            foreach (var (category, status, title) in Rows)
            {
                data.Add(category, status, title);
            }

            return data;
        }
    }

    [Fact]
    public void CategoryRows_CoverEveryCategory()
    {
        // Assert
        Rows.Select(row => row.Category).ToHashSet().ShouldBe(Enum.GetValues<ErrorCategory>().ToHashSet());
    }

    [Theory]
    [MemberData(nameof(CategoryRows))]
    public async Task ToProblemDetailsResponse_EveryCategory_WritesItsStatusAndReasonPhrase(
        ErrorCategory category,
        int expectedStatus,
        string expectedTitle)
    {
        // Arrange
        var error = EncinaErrors.Create("any.code", category, "message");

        // Act
        var response = (TestResponse)await error.ToProblemDetailsResponse(NewRequest());

        // Assert
        ((int)response.StatusCode).ShouldBe(expectedStatus);
        var body = JsonDocument.Parse(response.ReadBody()).RootElement;
        body.GetProperty("status").GetInt32().ShouldBe(expectedStatus);
        body.GetProperty("title").GetString().ShouldBe(expectedTitle);
        response.Headers.GetValues("Content-Type").ShouldBe(["application/problem+json"]);
    }

    [Theory]
    [InlineData("validation.invalid_input")]
    [InlineData("user.not_found")]
    [InlineData("email.already_exists")]
    [InlineData("order.conflict")]
    [InlineData("encina.authorization.forbidden")]
    public async Task ToProblemDetailsResponse_CategorizedInternal_IgnoresTheCodeText(string code)
    {
        // Arrange
        var error = EncinaErrors.Create(code, ErrorCategory.Internal, "message");

        // Act
        var response = await error.ToProblemDetailsResponse(NewRequest());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ToProblemDetailsResponse_ErrorWithoutCode_Answers500()
    {
        // Arrange
        var error = EncinaErrors.Create(code: "x", message: "Something went wrong");
        var codeless = EncinaError.New("Something went wrong");

        // Act
        var first = await error.ToProblemDetailsResponse(NewRequest());
        var second = await codeless.ToProblemDetailsResponse(NewRequest());

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        second.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ToProblemDetailsResponse_RateLimitedWithRetryAfter_WritesRetryAfterSeconds()
    {
        // Arrange
        var error = EncinaErrors.Create(
            "rate.limited",
            ErrorCategory.RateLimited,
            "slow down",
            details: new Dictionary<string, object?> { [EncinaErrorMetadataKeys.RetryAfter] = TimeSpan.FromSeconds(30) });

        // Act
        var response = (TestResponse)await error.ToProblemDetailsResponse(NewRequest());

        // Assert
        response.StatusCode.ShouldBe((HttpStatusCode)429);
        response.Headers.GetValues("Retry-After").ShouldBe(["30"]);
    }

    [Fact]
    public async Task ToProblemDetailsResponse_UnavailableWithRetryAfter_WritesRetryAfterSeconds()
    {
        // Arrange
        var error = EncinaErrors.Create(
            "service.unavailable",
            ErrorCategory.Unavailable,
            "down",
            details: new Dictionary<string, object?> { [EncinaErrorMetadataKeys.RetryAfter] = TimeSpan.FromMilliseconds(1500) });

        // Act
        var response = (TestResponse)await error.ToProblemDetailsResponse(NewRequest());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Headers.GetValues("Retry-After").ShouldBe(["2"]);
    }

    [Fact]
    public async Task ToProblemDetailsResponse_RateLimitedWithoutRetryAfter_WritesNoRetryAfter()
    {
        // Arrange
        var error = EncinaErrors.Create("rate.limited", ErrorCategory.RateLimited, "slow down");

        // Act
        var response = (TestResponse)await error.ToProblemDetailsResponse(NewRequest());

        // Assert
        response.Headers.TryGetValues("Retry-After", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task ToProblemDetailsResponse_Unauthenticated_WritesBearerChallenge()
    {
        // Arrange
        var error = EncinaErrors.Create("auth.missing", ErrorCategory.Unauthenticated, "sign in");

        // Act
        var response = (TestResponse)await error.ToProblemDetailsResponse(NewRequest());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.GetValues("WWW-Authenticate").ShouldBe(["Bearer"]);
    }

    [Fact]
    public async Task ToProblemDetailsResponse_UnauthenticatedWithChallenge_WritesTheChallenge()
    {
        // Arrange
        var error = EncinaErrors.Create(
            "auth.missing",
            ErrorCategory.Unauthenticated,
            "sign in",
            details: new Dictionary<string, object?> { [EncinaErrorMetadataKeys.AuthenticateChallenge] = "Basic realm=\"orders\"" });

        // Act
        var response = (TestResponse)await error.ToProblemDetailsResponse(NewRequest());

        // Assert
        response.Headers.GetValues("WWW-Authenticate").ShouldBe(["Basic realm=\"orders\""]);
    }

    [Fact]
    public async Task ToProblemDetailsResponse_ExplicitStatus_OverridesTheCategoryAndKeepsItsTitle()
    {
        // Arrange
        var error = EncinaErrors.Create("order.not_found", ErrorCategory.NotFound, "no order");

        // Act
        var response = (TestResponse)await error.ToProblemDetailsResponse(NewRequest(), statusCode: 418);

        // Assert
        ((int)response.StatusCode).ShouldBe(418);
        var body = JsonDocument.Parse(response.ReadBody()).RootElement;
        body.GetProperty("status").GetInt32().ShouldBe(418);
        body.GetProperty("title").GetString().ShouldBe("An error occurred");
    }

    [Fact]
    public async Task ToProblemDetailsResponse_OverrideTo503_WritesRetryAfterOfTheEffectiveStatus()
    {
        // Arrange
        var error = EncinaErrors.Create(
            "order.not_found",
            ErrorCategory.NotFound,
            "no order",
            details: new Dictionary<string, object?> { [EncinaErrorMetadataKeys.RetryAfter] = TimeSpan.FromSeconds(5) });

        // Act
        var response = (TestResponse)await error.ToProblemDetailsResponse(NewRequest(), statusCode: 503);

        // Assert
        response.Headers.GetValues("Retry-After").ShouldBe(["5"]);
    }

    [Fact]
    public async Task ToProblemDetailsResponse_WritesErrorCodeTraceIdAndDetail()
    {
        // Arrange
        var error = EncinaErrors.Create("order.not_found", ErrorCategory.NotFound, "no order");

        // Act
        var response = (TestResponse)await error.ToProblemDetailsResponse(NewRequest());

        // Assert
        var body = JsonDocument.Parse(response.ReadBody()).RootElement;
        body.GetProperty("extensions").GetProperty("errorCode").GetString().ShouldBe("order.not_found");
        body.GetProperty("extensions").GetProperty("traceId").GetString().ShouldBe("invocation-1");
    }

    [Fact]
    public async Task ToProblemDetailsResponse_NullRequest_Throws()
    {
        // Arrange
        var error = EncinaErrors.Create("x", ErrorCategory.Internal, "m");

        // Act
        var act = async () => await error.ToProblemDetailsResponse(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ToHttpResponseData_Success_WritesTheValueWithTheSuccessStatus()
    {
        // Arrange
        Either<EncinaError, string> result = "ok";

        // Act
        var response = (TestResponse)await result.ToHttpResponseData(NewRequest(), successStatusCode: 202);

        // Assert
        ((int)response.StatusCode).ShouldBe(202);
        response.ReadBody().ShouldBe("\"ok\"");
    }

    [Fact]
    public async Task ToHttpResponseData_FailureWithOverride_UsesTheOverride()
    {
        // Arrange
        Either<EncinaError, string> result = EncinaErrors.Create("a.b", ErrorCategory.Conflict, "m");

        // Act
        var plain = await result.ToHttpResponseData(NewRequest());
        var overridden = await result.ToHttpResponseData(NewRequest(), errorStatusCode: 422);

        // Assert
        plain.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ((int)overridden.StatusCode).ShouldBe(422);
    }

    [Fact]
    public async Task ToHttpResponseData_NullRequest_Throws()
    {
        // Arrange
        Either<EncinaError, string> result = "ok";

        // Act
        var act = async () => await result.ToHttpResponseData(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ToCreatedResponse_SuccessAndFailure_WriteLocationOrTheCategoryStatus()
    {
        // Arrange
        Either<EncinaError, int> created = 7;
        Either<EncinaError, int> failed = EncinaErrors.Create("o.exists", ErrorCategory.AlreadyExists, "m");

        // Act
        var ok = (TestResponse)await created.ToCreatedResponse(NewRequest(), id => $"/orders/{id}");
        var conflict = await failed.ToCreatedResponse(NewRequest(), id => $"/orders/{id}");

        // Assert
        ok.StatusCode.ShouldBe(HttpStatusCode.Created);
        ok.Headers.GetValues("Location").ShouldBe(["/orders/7"]);
        conflict.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ToCreatedResponse_NullArguments_Throw()
    {
        // Arrange
        Either<EncinaError, int> created = 7;

        // Act
        var nullRequest = async () => await created.ToCreatedResponse(null!, id => $"/orders/{id}");
        var nullFactory = async () => await created.ToCreatedResponse(NewRequest(), null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(nullRequest);
        await Should.ThrowAsync<ArgumentNullException>(nullFactory);
    }

    [Fact]
    public async Task ToNoContentResponse_SuccessAndFailure_WriteNoContentOrTheCategoryStatus()
    {
        // Arrange
        Either<EncinaError, Unit> ok = Unit.Default;
        Either<EncinaError, Unit> failed = EncinaErrors.Create("o.gone", ErrorCategory.NotFound, "m");

        // Act
        var noContent = await ok.ToNoContentResponse(NewRequest());
        var notFound = await failed.ToNoContentResponse(NewRequest());

        // Assert
        noContent.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        notFound.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToNoContentResponse_NullRequest_Throws()
    {
        // Arrange
        Either<EncinaError, Unit> ok = Unit.Default;

        // Act
        var act = async () => await ok.ToNoContentResponse(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    private static TestRequest NewRequest() => new(new TestContext());

    private sealed class TestContext : FunctionContext
    {
        public override string InvocationId => "invocation-1";

        public override string FunctionId => "function-1";

        public override TraceContext TraceContext => null!;

        public override BindingContext BindingContext => null!;

        public override RetryContext RetryContext => null!;

        public override IServiceProvider InstanceServices { get; set; } = null!;

        public override FunctionDefinition FunctionDefinition => null!;

        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

        public override IInvocationFeatures Features => null!;
    }

    private sealed class TestRequest(FunctionContext context) : HttpRequestData(context)
    {
        public override Stream Body { get; } = new MemoryStream();

        public override HttpHeadersCollection Headers { get; } = [];

        public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = [];

        public override Uri Url { get; } = new("https://functions.local/api/orders?x=1");

        public override IEnumerable<ClaimsIdentity> Identities { get; } = [];

        public override string Method => "POST";

        public override HttpResponseData CreateResponse() => new TestResponse(FunctionContext);
    }

    private sealed class TestResponse(FunctionContext context) : HttpResponseData(context)
    {
        public override HttpStatusCode StatusCode { get; set; }

        public override HttpHeadersCollection Headers { get; set; } = [];

        public override Stream Body { get; set; } = new MemoryStream();

        public override HttpCookies Cookies => null!;

        public string ReadBody()
        {
            Body.Position = 0;
            using var reader = new StreamReader(Body, leaveOpen: true);
            return reader.ReadToEnd();
        }
    }
}
