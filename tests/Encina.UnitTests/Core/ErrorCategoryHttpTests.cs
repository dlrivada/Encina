using Shouldly;

namespace Encina.UnitTests.Core;

/// <summary>
/// Unit tests of the HTTP column of the error category contract (ADR-062, #3683): the status of every category, the
/// reason phrases and the <c>Retry-After</c> and <c>WWW-Authenticate</c> headers.
/// </summary>
public sealed class ErrorCategoryHttpTests
{
    private static readonly Dictionary<ErrorCategory, int> Expected = new()
    {
        [ErrorCategory.Internal] = 500,
        [ErrorCategory.Validation] = 400,
        [ErrorCategory.InvalidRequest] = 400,
        [ErrorCategory.Unauthenticated] = 401,
        [ErrorCategory.Forbidden] = 403,
        [ErrorCategory.NotFound] = 404,
        [ErrorCategory.Conflict] = 409,
        [ErrorCategory.AlreadyExists] = 409,
        [ErrorCategory.FailedPrecondition] = 400,
        [ErrorCategory.RateLimited] = 429,
        [ErrorCategory.Unavailable] = 503,
        [ErrorCategory.Timeout] = 504,
        [ErrorCategory.UpstreamFailure] = 502,
        [ErrorCategory.Cancelled] = 499,
    };

    private static EncinaError WithDetails(ErrorCategory category, string key, object? value) =>
        EncinaErrors.Create("test.error", category, "message", details: new Dictionary<string, object?> { [key] = value });

    private static EncinaError WithoutDetails(ErrorCategory category) =>
        EncinaErrors.Create("test.error", category, "message");

    [Fact]
    public void GetStatusCode_EveryCategory_MapsToTheAdrTable()
    {
        var categories = Enum.GetValues<ErrorCategory>();

        categories.Length.ShouldBe(Expected.Count);
        foreach (var category in categories)
        {
            ErrorCategoryHttp.GetStatusCode(category).ShouldBe(Expected[category], category.ToString());
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(14)]
    [InlineData(99)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void GetStatusCode_UndefinedCategory_Answers500(int value)
    {
        ErrorCategoryHttp.GetStatusCode((ErrorCategory)value).ShouldBe(500);
    }

    [Fact]
    public void GetStatusCode_ForAnError_UsesItsCategory()
    {
        WithoutDetails(ErrorCategory.NotFound).GetStatusCode().ShouldBe(404);
        WithoutDetails(ErrorCategory.Cancelled).GetStatusCode().ShouldBe(499);
        default(EncinaError).GetStatusCode().ShouldBe(500);
    }

    [Theory]
    [InlineData(400, "Bad Request")]
    [InlineData(401, "Unauthorized")]
    [InlineData(403, "Forbidden")]
    [InlineData(404, "Not Found")]
    [InlineData(409, "Conflict")]
    [InlineData(422, "Unprocessable Entity")]
    [InlineData(429, "Too Many Requests")]
    [InlineData(499, "Client Closed Request")]
    [InlineData(500, "Internal Server Error")]
    [InlineData(502, "Bad Gateway")]
    [InlineData(503, "Service Unavailable")]
    [InlineData(504, "Gateway Timeout")]
    [InlineData(418, "An error occurred")]
    [InlineData(0, "An error occurred")]
    [InlineData(-5, "An error occurred")]
    public void GetReasonPhrase_ReturnsThePhraseOrTheGenericOne(int status, string expected)
    {
        ErrorCategoryHttp.GetReasonPhrase(status).ShouldBe(expected);
    }

    [Fact]
    public void GetReasonPhrase_EveryStatusOfTheTable_HasADedicatedPhrase()
    {
        foreach (var status in Expected.Values)
        {
            ErrorCategoryHttp.GetReasonPhrase(status).ShouldNotBe("An error occurred", $"status {status}");
        }
    }

    [Theory]
    [InlineData(429)]
    [InlineData(503)]
    public void GetResponseHeaders_RetryAfterWithRetryableStatus_WritesDeltaSeconds(int status)
    {
        var error = WithDetails(ErrorCategory.RateLimited, EncinaErrorMetadataKeys.RetryAfter, TimeSpan.FromSeconds(30));

        var headers = ErrorCategoryHttp.GetResponseHeaders(error, status);

        headers.Count.ShouldBe(1);
        headers[0].Key.ShouldBe("Retry-After");
        headers[0].Value.ShouldBe("30");
    }

    [Theory]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(504)]
    public void GetResponseHeaders_RetryAfterWithAnotherStatus_WritesNoHeader(int status)
    {
        var error = WithDetails(ErrorCategory.RateLimited, EncinaErrorMetadataKeys.RetryAfter, TimeSpan.FromSeconds(30));

        ErrorCategoryHttp.GetResponseHeaders(error, status).ShouldBeEmpty();
    }

    [Fact]
    public void GetResponseHeaders_RetryAfterKeyAbsent_WritesNoHeader()
    {
        ErrorCategoryHttp.GetResponseHeaders(WithoutDetails(ErrorCategory.RateLimited), 429).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(30.2, "31")]
    [InlineData(0.001, "1")]
    [InlineData(1.0, "1")]
    [InlineData(0.0, "0")]
    public void GetResponseHeaders_RetryAfterFraction_RoundsUp(double seconds, string expected)
    {
        var error = WithDetails(ErrorCategory.Unavailable, EncinaErrorMetadataKeys.RetryAfter, TimeSpan.FromSeconds(seconds));

        ErrorCategoryHttp.GetResponseHeaders(error, 503).Single().Value.ShouldBe(expected);
    }

    [Fact]
    public void GetResponseHeaders_RetryAfterNegative_WritesNoHeader()
    {
        var error = WithDetails(ErrorCategory.RateLimited, EncinaErrorMetadataKeys.RetryAfter, TimeSpan.FromSeconds(-1));

        ErrorCategoryHttp.GetResponseHeaders(error, 429).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(30)]
    [InlineData("30")]
    [InlineData(30L)]
    [InlineData(null)]
    public void GetResponseHeaders_RetryAfterNotATimeSpan_WritesNoHeader(object? value)
    {
        var error = WithDetails(ErrorCategory.RateLimited, EncinaErrorMetadataKeys.RetryAfter, value);

        ErrorCategoryHttp.GetResponseHeaders(error, 429).ShouldBeEmpty();
    }

    [Fact]
    public void GetResponseHeaders_RetryAfterMaxValue_DoesNotThrow()
    {
        var error = WithDetails(ErrorCategory.RateLimited, EncinaErrorMetadataKeys.RetryAfter, TimeSpan.MaxValue);

        ErrorCategoryHttp.GetResponseHeaders(error, 429).Single().Value.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void GetResponseHeaders_401WithoutChallenge_WritesBearer()
    {
        var headers = ErrorCategoryHttp.GetResponseHeaders(WithoutDetails(ErrorCategory.Unauthenticated), 401);

        headers.Count.ShouldBe(1);
        headers[0].Key.ShouldBe("WWW-Authenticate");
        headers[0].Value.ShouldBe("Bearer");
    }

    [Fact]
    public void GetResponseHeaders_401WithChallenge_WritesTheChallenge()
    {
        var error = WithDetails(ErrorCategory.Unauthenticated, EncinaErrorMetadataKeys.AuthenticateChallenge, "HMAC realm=\"api\"");

        ErrorCategoryHttp.GetResponseHeaders(error, 401).Single().Value.ShouldBe("HMAC realm=\"api\"");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bearer\r\nX-Injected: 1")]
    [InlineData("Bea\trer")]
    [InlineData("Bearer\0")]
    [InlineData("Bearer realm=\"中\"")]
    [InlineData("Beareré")]
    [InlineData("Bearer\u007f")]
    public void GetResponseHeaders_401WithUnusableChallenge_FallsBackToBearer(string challenge)
    {
        var error = WithDetails(ErrorCategory.Unauthenticated, EncinaErrorMetadataKeys.AuthenticateChallenge, challenge);

        ErrorCategoryHttp.GetResponseHeaders(error, 401).Single().Value.ShouldBe("Bearer");
    }

    [Theory]
    [InlineData(42)]
    [InlineData(null)]
    public void GetResponseHeaders_401WithNonStringChallenge_FallsBackToBearer(object? challenge)
    {
        var error = WithDetails(ErrorCategory.Unauthenticated, EncinaErrorMetadataKeys.AuthenticateChallenge, challenge);

        ErrorCategoryHttp.GetResponseHeaders(error, 401).Single().Value.ShouldBe("Bearer");
    }

    [Fact]
    public void GetResponseHeaders_ChallengeWithAnotherStatus_WritesNoHeader()
    {
        var error = WithDetails(ErrorCategory.Forbidden, EncinaErrorMetadataKeys.AuthenticateChallenge, "Bearer");

        ErrorCategoryHttp.GetResponseHeaders(error, 403).ShouldBeEmpty();
    }

    [Fact]
    public void GetResponseHeaders_NeverReadsTheMessage()
    {
        var error = EncinaErrors.Create("test.error", ErrorCategory.RateLimited, "Retry-After: 99 secret");

        ErrorCategoryHttp.GetResponseHeaders(error, 429).ShouldBeEmpty();
    }
}
