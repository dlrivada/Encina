namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for <see cref="ErrorCategoryHttp"/>: its parameters are an enum, an int and the
/// <see cref="EncinaError"/> struct, so there is no null check; these tests pin the out-of-range and
/// missing-metadata fallbacks instead.
/// </summary>
public class ErrorCategoryHttpGuardTests
{
    /// <summary>
    /// An undefined category falls back to 500.
    /// </summary>
    [Fact]
    public void GetStatusCode_UndefinedCategory_Returns500()
    {
        ErrorCategoryHttp.GetStatusCode((ErrorCategory)9999).ShouldBe(500);
        ErrorCategoryHttp.GetStatusCode((ErrorCategory)(-1)).ShouldBe(500);
    }

    /// <summary>
    /// A defined category maps to its status.
    /// </summary>
    [Fact]
    public void GetStatusCode_NotFoundCategory_Returns404()
    {
        ErrorCategoryHttp.GetStatusCode(ErrorCategory.NotFound).ShouldBe(404);
    }

    /// <summary>
    /// The extension reads the category of the error.
    /// </summary>
    [Fact]
    public void GetStatusCode_Error_UsesItsCategory()
    {
        var error = EncinaErrors.Create("test.conflict", ErrorCategory.Conflict, "conflict");

        error.GetStatusCode().ShouldBe(409);
    }

    /// <summary>
    /// An unknown status gets the default reason phrase.
    /// </summary>
    [Fact]
    public void GetReasonPhrase_UnknownStatus_ReturnsDefaultPhrase()
    {
        ErrorCategoryHttp.GetReasonPhrase(299).ShouldBe("An error occurred");
        ErrorCategoryHttp.GetReasonPhrase(404).ShouldBe("Not Found");
    }

    /// <summary>
    /// A 401 without a challenge in the details carries the Bearer default.
    /// </summary>
    [Fact]
    public void GetResponseHeaders_UnauthorizedWithoutChallenge_ReturnsBearer()
    {
        var error = EncinaErrors.Create("test.unauth", ErrorCategory.Unauthenticated, "no");

        var headers = ErrorCategoryHttp.GetResponseHeaders(error, 401);

        headers.ShouldHaveSingleItem().ShouldBe(new KeyValuePair<string, string>("WWW-Authenticate", "Bearer"));
    }

    /// <summary>
    /// A challenge with a control character is refused and the default is used.
    /// </summary>
    [Fact]
    public void GetResponseHeaders_ChallengeWithControlCharacter_FallsBackToBearer()
    {
        var error = EncinaErrors.Create(
            "test.unauth", ErrorCategory.Unauthenticated, "no",
            details: new Dictionary<string, object?> { [EncinaErrorMetadataKeys.AuthenticateChallenge] = "Basic\r\nX: y" });

        ErrorCategoryHttp.GetResponseHeaders(error, 401).ShouldHaveSingleItem().Value.ShouldBe("Bearer");
    }

    /// <summary>
    /// A rate-limit error with no Retry-After detail carries no header.
    /// </summary>
    [Fact]
    public void GetResponseHeaders_RateLimitedWithoutRetryAfter_ReturnsNoHeaders()
    {
        var error = EncinaErrors.Create("test.rate", ErrorCategory.RateLimited, "slow down");

        ErrorCategoryHttp.GetResponseHeaders(error, 429).ShouldBeEmpty();
    }

    /// <summary>
    /// A positive Retry-After is rounded up to whole seconds.
    /// </summary>
    [Fact]
    public void GetResponseHeaders_RateLimitedWithRetryAfter_RoundsUp()
    {
        var error = EncinaErrors.Create(
            "test.rate", ErrorCategory.RateLimited, "slow down",
            details: new Dictionary<string, object?> { [EncinaErrorMetadataKeys.RetryAfter] = TimeSpan.FromMilliseconds(1500) });

        ErrorCategoryHttp.GetResponseHeaders(error, 429).ShouldHaveSingleItem().ShouldBe(new KeyValuePair<string, string>("Retry-After", "2"));
    }

    /// <summary>
    /// A negative Retry-After is refused.
    /// </summary>
    [Fact]
    public void GetResponseHeaders_NegativeRetryAfter_ReturnsNoHeaders()
    {
        var error = EncinaErrors.Create(
            "test.rate", ErrorCategory.Unavailable, "down",
            details: new Dictionary<string, object?> { [EncinaErrorMetadataKeys.RetryAfter] = TimeSpan.FromSeconds(-1) });

        ErrorCategoryHttp.GetResponseHeaders(error, 503).ShouldBeEmpty();
    }

    /// <summary>
    /// A status that carries no header gets none.
    /// </summary>
    [Fact]
    public void GetResponseHeaders_OtherStatus_ReturnsNoHeaders()
    {
        var error = EncinaErrors.Create("test.nf", ErrorCategory.NotFound, "missing");

        ErrorCategoryHttp.GetResponseHeaders(error, 404).ShouldBeEmpty();
    }
}
