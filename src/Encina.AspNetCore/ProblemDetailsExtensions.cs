using Microsoft.AspNetCore.Http;
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
    /// <param name="statusCode">Optional HTTP status code. If not provided, will be inferred from error code.</param>
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
    /// Status codes are mapped from error codes:
    /// <list type="bullet">
    /// <item><description><b>400 Bad Request</b>: validation.*, guard.validation_failed</description></item>
    /// <item><description><b>401 Unauthorized</b>: encina.authorization.unauthenticated</description></item>
    /// <item><description><b>403 Forbidden</b>: encina.authorization.* (every other authorization code)</description></item>
    /// <item><description><b>404 Not Found</b>: *.not_found, encina.request.handler_missing</description></item>
    /// <item><description><b>409 Conflict</b>: *.conflict, *.already_exists</description></item>
    /// <item><description><b>500 Internal Server Error</b>: Default for unrecognized codes</description></item>
    /// </list>
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
        var options = ResolveOptions(httpContext);
        var effectiveStatusCode = statusCode ?? MapErrorCodeToStatusCode(error);

        var exposeMessage = ShouldExposeErrorMessage(
            options,
            httpContext.RequestServices.GetService<IHostEnvironment>());

        var problemDetails = new ProblemDetails
        {
            Status = effectiveStatusCode,
            Title = GetTitle(effectiveStatusCode),
            Detail = exposeMessage ? error.Message : null,
            Instance = options.IncludeRequestPathInProblemDetails
                ? httpContext.Request.Path
                : null
        };

        AddCorrelationExtensions(problemDetails, httpContext, options);
        AddErrorCodeExtension(problemDetails, error);

        // Exception details follow the same option and the same Development-only guard as the detail
        if (exposeMessage)
        {
            AddExceptionExtension(problemDetails, error);
        }

        return Results.Problem(problemDetails);
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

    private static void AddCorrelationExtensions(
        ProblemDetails problemDetails,
        HttpContext httpContext,
        EncinaAspNetCoreOptions options)
    {
        var traceId = httpContext.TraceIdentifier;
        if (!string.IsNullOrEmpty(traceId))
        {
            problemDetails.Extensions["traceId"] = traceId;
        }

        if (httpContext.Request.Headers.TryGetValue(options.CorrelationIdHeader, out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId.ToString();
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
    /// <param name="statusCode">Optional HTTP status code. If not provided, will be inferred from error code.</param>
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

    private static int MapErrorCodeToStatusCode(EncinaError error)
    {
        var code = error.GetCode().IfNone(string.Empty).ToLowerInvariant();

        // 400 Bad Request: Validation errors
        if (IsValidationCode(code))
        {
            return StatusCodes.Status400BadRequest;
        }

        // 401 Unauthorized: Authentication required
        if (code == EncinaErrorCodes.AuthorizationUnauthenticated)
        {
            return StatusCodes.Status401Unauthorized;
        }

        // 403 Forbidden: authenticated identity denied (every other encina.authorization.* code)
        if (code.StartsWith(EncinaErrorCodes.AuthorizationPrefix, StringComparison.Ordinal))
        {
            return StatusCodes.Status403Forbidden;
        }

        // 404 Not Found
        if (IsNotFoundCode(code))
        {
            return StatusCodes.Status404NotFound;
        }

        // 409 Conflict
        if (IsConflictCode(code))
        {
            return StatusCodes.Status409Conflict;
        }

        // 500 Internal Server Error: Default for unknown/unhandled errors
        return StatusCodes.Status500InternalServerError;
    }

    private static bool IsValidationCode(string code) =>
        code.StartsWith("validation.", StringComparison.Ordinal) || code == "encina.guard.validation_failed";

    private static bool IsNotFoundCode(string code) =>
        code.EndsWith(".not_found", StringComparison.Ordinal) ||
        code == "encina.request.handler_missing" ||
        code.EndsWith(".missing", StringComparison.Ordinal);

    private static bool IsConflictCode(string code) =>
        code.EndsWith(".conflict", StringComparison.Ordinal) ||
        code.EndsWith(".already_exists", StringComparison.Ordinal) ||
        code.EndsWith(".duplicate", StringComparison.Ordinal);

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            500 => "Internal Server Error",
            _ => "An error occurred"
        };
    }

    private static string? GetErrorCode(EncinaError error)
    {
        // Try to extract error code from error metadata
        return error.GetCode().Match(
            Some: code => code,
            None: () => default(string));
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
