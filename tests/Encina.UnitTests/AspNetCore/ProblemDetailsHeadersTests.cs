using System.Text.Json;
using Encina.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Response headers and titles of <see cref="ProblemDetailsExtensions"/>: the result is executed against a real
/// response, so the headers written by <c>ExecuteAsync</c> are observed, not only the returned status.
/// </summary>
public sealed class ProblemDetailsHeadersTests
{
    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .Configure<EncinaAspNetCoreOptions>(_ => { })
                .AddLogging()
                .BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static EncinaError WithDetails(ErrorCategory category, string key, object? value) =>
        EncinaErrors.Create(
            "test.categorized",
            category,
            "message",
            details: new Dictionary<string, object?> { [key] = value });

    private static async Task<DefaultHttpContext> ExecuteAsync(EncinaError error, int? statusCode = null, Action<HttpContext>? prepare = null)
    {
        var context = CreateHttpContext();
        prepare?.Invoke(context);
        await error.ToProblemDetails(context, statusCode).ExecuteAsync(context);
        return context;
    }

    [Fact]
    public async Task Cancelled_Answers499WithTheClientClosedRequestTitle()
    {
        var context = await ExecuteAsync(EncinaErrors.Create("test.cancelled", ErrorCategory.Cancelled, "message"));

        context.Response.StatusCode.ShouldBe(499);
        context.Response.Headers.ContainsKey("Retry-After").ShouldBeFalse();
        (await ReadTitleAsync(context)).ShouldBe("Client Closed Request");
    }

    [Theory]
    [InlineData(ErrorCategory.RateLimited, 429, "Too Many Requests")]
    [InlineData(ErrorCategory.UpstreamFailure, 502, "Bad Gateway")]
    [InlineData(ErrorCategory.Unavailable, 503, "Service Unavailable")]
    [InlineData(ErrorCategory.Timeout, 504, "Gateway Timeout")]
    public async Task NewStatuses_CarryTheirReasonPhraseAsTitle(ErrorCategory category, int status, string title)
    {
        var context = await ExecuteAsync(EncinaErrors.Create("test.categorized", category, "message"));

        context.Response.StatusCode.ShouldBe(status);
        (await ReadTitleAsync(context)).ShouldBe(title);
    }

    [Theory]
    [InlineData(ErrorCategory.RateLimited, 429)]
    [InlineData(ErrorCategory.Unavailable, 503)]
    public async Task RetryAfter_IsWrittenInSeconds_RoundedUp(ErrorCategory category, int status)
    {
        var error = WithDetails(category, EncinaErrorMetadataKeys.RetryAfter, TimeSpan.FromMilliseconds(1500));

        var context = await ExecuteAsync(error);

        context.Response.StatusCode.ShouldBe(status);
        context.Response.Headers["Retry-After"].ToString().ShouldBe("2");
    }

    [Fact]
    public async Task RetryAfter_OverwritesAnExistingValue()
    {
        var error = WithDetails(ErrorCategory.RateLimited, EncinaErrorMetadataKeys.RetryAfter, TimeSpan.FromSeconds(30));

        var context = await ExecuteAsync(error, prepare: c => c.Response.Headers["Retry-After"] = "5");

        context.Response.Headers["Retry-After"].ToString().ShouldBe("30");
    }

    [Fact]
    public async Task RetryAfter_WithoutTheMetadata_IsNotWritten()
    {
        var context = await ExecuteAsync(EncinaErrors.Create("test.categorized", ErrorCategory.RateLimited, "message"));

        context.Response.Headers.ContainsKey("Retry-After").ShouldBeFalse();
    }

    [Fact]
    public async Task RetryAfter_IsNotWrittenForOtherStatuses()
    {
        var error = WithDetails(ErrorCategory.Conflict, EncinaErrorMetadataKeys.RetryAfter, TimeSpan.FromSeconds(30));

        var context = await ExecuteAsync(error);

        context.Response.Headers.ContainsKey("Retry-After").ShouldBeFalse();
    }

    [Fact]
    public async Task Unauthenticated_CarriesTheDefaultBearerChallenge()
    {
        var context = await ExecuteAsync(EncinaErrors.Create("test.categorized", ErrorCategory.Unauthenticated, "message"));

        context.Response.StatusCode.ShouldBe(401);
        context.Response.Headers.WWWAuthenticate.ToString().ShouldBe("Bearer");
    }

    [Fact]
    public async Task Unauthenticated_UsesTheChallengeOfTheError()
    {
        var error = WithDetails(ErrorCategory.Unauthenticated, EncinaErrorMetadataKeys.AuthenticateChallenge, "Basic realm=\"api\"");

        var context = await ExecuteAsync(error);

        context.Response.Headers.WWWAuthenticate.ToString().ShouldBe("Basic realm=\"api\"");
    }

    [Fact]
    public async Task Unauthenticated_KeepsAChallengeAlreadyWrittenByTheAuthenticationHandler()
    {
        var error = EncinaErrors.Create("test.categorized", ErrorCategory.Unauthenticated, "message");

        var context = await ExecuteAsync(error, prepare: c => c.Response.Headers.WWWAuthenticate = "Bearer error=\"invalid_token\"");

        context.Response.Headers.WWWAuthenticate.ToString().ShouldBe("Bearer error=\"invalid_token\"");
    }

    [Fact]
    public async Task ExplicitStatus401_CarriesTheChallengeWhateverTheCategory()
    {
        var context = await ExecuteAsync(EncinaErrors.Create("test.categorized", ErrorCategory.Forbidden, "message"), 401);

        context.Response.StatusCode.ShouldBe(401);
        context.Response.Headers.WWWAuthenticate.ToString().ShouldBe("Bearer");
    }

    [Fact]
    public async Task ExplicitStatus429_OverridingTheCategory_CarriesRetryAfter()
    {
        var error = WithDetails(ErrorCategory.Internal, EncinaErrorMetadataKeys.RetryAfter, TimeSpan.FromSeconds(7));

        var context = await ExecuteAsync(error, 429);

        context.Response.Headers["Retry-After"].ToString().ShouldBe("7");
    }

    [Fact]
    public void Building_TheResult_DoesNotTouchTheResponse()
    {
        var context = CreateHttpContext();
        var error = EncinaErrors.Create("test.categorized", ErrorCategory.Unauthenticated, "message");

        _ = error.ToProblemDetails(context);

        context.Response.Headers.ContainsKey("WWW-Authenticate").ShouldBeFalse();
    }

    [Fact]
    public async Task TheBody_NeverCarriesTheErrorMessage()
    {
        var context = await ExecuteAsync(
            EncinaErrors.Create("test.categorized", ErrorCategory.Unauthenticated, "secret-message-4711"));

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        (await reader.ReadToEndAsync()).ShouldNotContain("secret-message-4711");
    }

    private static async Task<string?> ReadTitleAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.GetProperty("title").GetString();
    }
}
