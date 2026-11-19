using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Encina.AspNetCore;

/// <summary>
/// Extension methods to convert <see cref="EncinaError"/> to RFC 7807 Problem Details.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Converts a <see cref="EncinaError"/> to an <see cref="IResult"/> with Problem Details.
    /// </summary>
    /// <param name="error">The Encina error.</param>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="statusCode">Optional HTTP status code that overrides the status of the error category.</param>
    /// <returns>An <see cref="IResult"/> containing RFC 7807 Problem Details.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a standardized error response following RFC 7807 (Problem Details for HTTP APIs).
    /// </para>
    /// <para>
    /// The response carries the status, a generic title, the <c>errorCode</c> and the trace and correlation
    /// ids. The error message (<c>detail</c>) and the exception type, message and stack trace
    /// (<c>exception</c>) are omitted unless <see cref="EncinaAspNetCoreOptions.IncludeErrorMessageInProblemDetails"/>
    /// is true and the host environment is Development.
    /// </para>
    /// <para>
    /// The status comes from the <see cref="ErrorCategory"/> of the error through the core HTTP table
    /// (<see cref="ErrorCategoryHttp"/>, ADR-062); the host never parses the error code. The title is the reason
    /// phrase of the status. <c>Retry-After</c> (429 and 503) and <c>WWW-Authenticate</c> (401) are written when
    /// the result is executed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a Minimal API endpoint
    /// app.MapPost("/users", async (CreateUserCommand cmd, IEncina Encina, HttpContext httpContext) =>
    /// {
    ///     var result = await Encina.Send(cmd);
    ///     return result.Match(
    ///         Right: user => Results.Created($"/users/{user.Id}", user),
    ///         Left: error => error.ToProblemDetails(httpContext)
    ///     );
    /// });
    ///
    /// // In a controller
    /// [HttpPost]
    /// public async Task&lt;IActionResult&gt; Create(CreateUserCommand cmd)
    /// {
    ///     var result = await _Encina.Send(cmd);
    ///     return result.Match(
    ///         Right: user => Created($"/users/{user.Id}", user),
    ///         Left: error => error.ToActionResult(HttpContext)
    ///     );
    /// }
    /// </code>
    /// </example>
    public static IResult ToProblemDetails(
        this EncinaError error,
        HttpContext httpContext,
        int? statusCode = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var options = ResolveOptions(httpContext);
        var effectiveStatusCode = statusCode ?? error.GetStatusCode();

        var exposeMessage = ShouldExposeErrorMessage(
            options,
            httpContext.RequestServices.GetService<IHostEnvironment>());

        var problemDetails = new ProblemDetails
        {
            Status = effectiveStatusCode,
            Title = ErrorCategoryHttp.GetReasonPhrase(effectiveStatusCode),
            Detail = exposeMessage ? error.Message : null,
            Instance = options.IncludeRequestPathInProblemDetails
                ? httpContext.Request.Path
                : null
        };

        AddCorrelationExtensions(problemDetails, httpContext);
        AddErrorCodeExtension(problemDetails, error);

        // Exception details follow the same option and the same Development-only guard as the detail
        if (exposeMessage)
        {
            AddExceptionExtension(problemDetails, error);
        }

        return new ProblemResult(
            TypedResults.Problem(problemDetails),
            ErrorCategoryHttp.GetResponseHeaders(error, effectiveStatusCode));
    }

    private static EncinaAspNetCoreOptions ResolveOptions(HttpContext httpContext) =>
        httpContext.RequestServices.GetService<IOptions<EncinaAspNetCoreOptions>>()?.Value
        ?? new EncinaAspNetCoreOptions();

    // Error code for client-side handling.
    private static void AddErrorCodeExtension(ProblemDetails problemDetails, EncinaError error)
    {
        var errorCode = GetErrorCode(error);
        if (!string.IsNullOrEmpty(errorCode))
        {
            problemDetails.Extensions["errorCode"] = errorCode;
        }
    }

    // The correlation id is the normalized one of the ambient request context, the value the
    // Correlation-Id response header carries; the raw request header is never echoed. With no
    // context the field is omitted.
    private static void AddCorrelationExtensions(ProblemDetails problemDetails, HttpContext httpContext)
    {
        var traceId = httpContext.TraceIdentifier;
        if (!string.IsNullOrEmpty(traceId))
        {
            problemDetails.Extensions["traceId"] = traceId;
        }

        var correlationId = httpContext.RequestServices.GetService<IRequestContextAccessor>()?.RequestContext?.CorrelationId;
        if (!string.IsNullOrEmpty(correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }
    }

    private static void AddExceptionExtension(ProblemDetails problemDetails, EncinaError error)
    {
        error.Exception.IfSome(ex =>
        {
            problemDetails.Extensions["exception"] = new
            {
                type = ex.GetType().FullName,
                message = ex.Message,
                stackTrace = ex.StackTrace
            };
        });
    }

    /// <summary>
    /// Whether error messages may reach the client: the option is on and the host is Development.
    /// A missing environment counts as non-Development (fail closed).
    /// </summary>
    internal static bool ShouldExposeErrorMessage(EncinaAspNetCoreOptions options, IHostEnvironment? environment) =>
        options.IncludeErrorMessageInProblemDetails && environment?.IsDevelopment() == true;

    /// <summary>
    /// Converts a <see cref="EncinaError"/> to an <see cref="IActionResult"/> for controllers.
    /// </summary>
    /// <param name="error">The Encina error.</param>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="statusCode">Optional HTTP status code that overrides the status of the error category.</param>
    /// <returns>An <see cref="IActionResult"/> containing Problem Details.</returns>
    public static IActionResult ToActionResult(
        this EncinaError error,
        HttpContext httpContext,
        int? statusCode = null)
    {
        var result = error.ToProblemDetails(httpContext, statusCode);

        // Convert IResult to IActionResult by executing it
        return new ProblemDetailsActionResult(result);
    }

    private static string? GetErrorCode(EncinaError error)
    {
        // An error created without a code has none: the body omits the field and the status stays 500.
        return error.GetCode().IfNoneUnsafe((string?)null);
    }

    /// <summary>
    /// A problem result that writes the response headers of the error (<c>Retry-After</c>,
    /// <c>WWW-Authenticate</c>) when it is executed, so building the result never touches the response.
    /// </summary>
    internal sealed class ProblemResult : IResult, IStatusCodeHttpResult
    {
        private const string RetryAfterHeader = "Retry-After";

        private readonly IReadOnlyList<KeyValuePair<string, string>> _headers;

        public ProblemResult(ProblemHttpResult inner, IReadOnlyList<KeyValuePair<string, string>> headers)
        {
            Inner = inner;
            _headers = headers;
        }

        public ProblemHttpResult Inner { get; }

        public int? StatusCode => Inner.StatusCode;

        public Task ExecuteAsync(HttpContext httpContext)
        {
            var responseHeaders = httpContext.Response.Headers;

            foreach (var header in _headers)
            {
                // Retry-After comes from the error and is the authority; a challenge already written by the
                // authentication handler is more specific than the default one, so it stays.
                if (header.Key == RetryAfterHeader || !responseHeaders.ContainsKey(header.Key))
                {
                    responseHeaders[header.Key] = header.Value;
                }
            }

            return Inner.ExecuteAsync(httpContext);
        }
    }

    /// <summary>
    /// Helper class to convert IResult to IActionResult.
    /// </summary>
    private sealed class ProblemDetailsActionResult : IActionResult
    {
        private readonly IResult _result;

        public ProblemDetailsActionResult(IResult result)
        {
            _result = result;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            await _result.ExecuteAsync(context.HttpContext);
        }
    }
}
