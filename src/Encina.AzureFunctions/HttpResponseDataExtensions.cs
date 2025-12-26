using System.Net;
using System.Text.Json;
using LanguageExt;
using Microsoft.Azure.Functions.Worker.Http;

namespace Encina.AzureFunctions;

/// <summary>
/// Extension methods for converting Encina results to Azure Functions HTTP responses.
/// </summary>
/// <remarks>
/// <para>
/// These extensions enable seamless integration between Encina's Railway Oriented Programming
/// approach (Either&lt;EncinaError, T&gt;) and Azure Functions HTTP responses.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Function("CreateOrder")]
/// public async Task&lt;HttpResponseData&gt; CreateOrder(
///     [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
/// {
///     var command = await req.ReadFromJsonAsync&lt;CreateOrder&gt;();
///     var result = await _encina.Send(command!);
///
///     return await result.ToCreatedResponse(req, order => $"/orders/{order.Id}");
/// }
/// </code>
/// </example>
public static class HttpResponseDataExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Converts an Either result to an HTTP response.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The Either result from an Encina operation.</param>
    /// <param name="request">The HTTP request data.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 200).</param>
    /// <param name="errorStatusCode">Optional HTTP status code for errors. If not provided, will be inferred from error code.</param>
    /// <returns>The HTTP response.</returns>
    /// <example>
    /// <code>
    /// var result = await encina.Send(new GetOrderById(orderId));
    /// return await result.ToHttpResponseData(request);
    /// </code>
    /// </example>
    public static async ValueTask<HttpResponseData> ToHttpResponseData<T>(
        this Either<EncinaError, T> result,
        HttpRequestData request,
        int successStatusCode = 200,
        int? errorStatusCode = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await result.Match(
            Right: async value => await CreateSuccessResponse(request, value, successStatusCode),
            Left: async error => await error.ToProblemDetailsResponse(request, errorStatusCode));
    }

    /// <summary>
    /// Converts an EncinaError to an RFC 7807 Problem Details HTTP response.
    /// </summary>
    /// <param name="error">The Encina error.</param>
    /// <param name="request">The HTTP request data.</param>
    /// <param name="statusCode">Optional HTTP status code. If not provided, will be inferred from error code.</param>
    /// <returns>The HTTP response with Problem Details.</returns>
    /// <remarks>
    /// <para>
    /// Status codes are mapped from error codes:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>400 Bad Request</b>: validation.*, guard.validation_failed</description></item>
    /// <item><description><b>401 Unauthorized</b>: authorization.unauthenticated</description></item>
    /// <item><description><b>403 Forbidden</b>: authorization.*</description></item>
    /// <item><description><b>404 Not Found</b>: *.not_found, encina.request.handler_missing</description></item>
    /// <item><description><b>409 Conflict</b>: *.conflict, *.already_exists</description></item>
    /// <item><description><b>500 Internal Server Error</b>: Default for unrecognized codes</description></item>
    /// </list>
    /// </remarks>
    public static async ValueTask<HttpResponseData> ToProblemDetailsResponse(
        this EncinaError error,
        HttpRequestData request,
        int? statusCode = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveStatusCode = statusCode ?? MapErrorCodeToStatusCode(error);
        var response = request.CreateResponse((HttpStatusCode)effectiveStatusCode);

        var problemDetails = new ProblemDetailsResponse
        {
            Status = effectiveStatusCode,
            Title = GetTitle(effectiveStatusCode),
            Detail = error.Message,
            Instance = request.Url.PathAndQuery
        };

        // Add error code for client-side handling
        var errorCode = error.GetCode().IfNone(string.Empty);
        if (!string.IsNullOrEmpty(errorCode))
        {
            problemDetails.Extensions["errorCode"] = errorCode;
        }

        // Add trace ID from function context
        if (request.FunctionContext is not null)
        {
            problemDetails.Extensions["traceId"] = request.FunctionContext.InvocationId;

            // Add correlation ID if available
            var correlationId = request.FunctionContext.GetCorrelationId();
            if (!string.IsNullOrEmpty(correlationId))
            {
                problemDetails.Extensions["correlationId"] = correlationId;
            }
        }

        response.Headers.Add("Content-Type", "application/problem+json");
        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
        await response.WriteStringAsync(json);

        return response;
    }

    /// <summary>
    /// Converts an Either result to a Created (201) response with location header.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The Either result from an Encina operation.</param>
    /// <param name="request">The HTTP request data.</param>
    /// <param name="locationFactory">Factory function to generate the location URL from the result.</param>
    /// <returns>The HTTP response with 201 status and Location header.</returns>
    /// <example>
    /// <code>
    /// var result = await encina.Send(new CreateOrder(/* ... */));
    /// return await result.ToCreatedResponse(request, order => $"/orders/{order.Id}");
    /// </code>
    /// </example>
    public static async ValueTask<HttpResponseData> ToCreatedResponse<T>(
        this Either<EncinaError, T> result,
        HttpRequestData request,
        Func<T, string> locationFactory)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(locationFactory);

        return await result.Match(
            Right: async value =>
            {
                var response = request.CreateResponse(HttpStatusCode.Created);
                response.Headers.Add("Location", locationFactory(value));
                response.Headers.Add("Content-Type", "application/json");
                var json = JsonSerializer.Serialize(value, JsonOptions);
                await response.WriteStringAsync(json);
                return response;
            },
            Left: async error => await error.ToProblemDetailsResponse(request));
    }

    /// <summary>
    /// Converts an Either&lt;EncinaError, Unit&gt; result to a No Content (204) response.
    /// </summary>
    /// <param name="result">The Either result from an Encina operation.</param>
    /// <param name="request">The HTTP request data.</param>
    /// <returns>The HTTP response with 204 status (success) or error response.</returns>
    /// <example>
    /// <code>
    /// var result = await encina.Send(new DeleteOrder(orderId));
    /// return await result.ToNoContentResponse(request);
    /// </code>
    /// </example>
    public static async ValueTask<HttpResponseData> ToNoContentResponse(
        this Either<EncinaError, Unit> result,
        HttpRequestData request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await result.Match<ValueTask<HttpResponseData>>(
            Right: _ => new ValueTask<HttpResponseData>(request.CreateResponse(HttpStatusCode.NoContent)),
            Left: async error => await error.ToProblemDetailsResponse(request));
    }

    private static async ValueTask<HttpResponseData> CreateSuccessResponse<T>(
        HttpRequestData request,
        T value,
        int statusCode)
    {
        var response = request.CreateResponse((HttpStatusCode)statusCode);
        response.Headers.Add("Content-Type", "application/json");
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await response.WriteStringAsync(json);
        return response;
    }

    internal static int MapErrorCodeToStatusCode(EncinaError error)
    {
        var code = error.GetCode().IfNone(string.Empty).ToLowerInvariant();

        // 400 Bad Request: Validation errors
        if (code.StartsWith("validation.", StringComparison.Ordinal) ||
            code == "encina.guard.validation_failed")
        {
            return (int)HttpStatusCode.BadRequest;
        }

        // 401 Unauthorized: Authentication required
        if (code == "authorization.unauthenticated")
        {
            return (int)HttpStatusCode.Unauthorized;
        }

        // 403 Forbidden: Authorized but insufficient permissions
        if (code.StartsWith("authorization.", StringComparison.Ordinal))
        {
            return (int)HttpStatusCode.Forbidden;
        }

        // 404 Not Found
        if (code.EndsWith(".not_found", StringComparison.Ordinal) ||
            code == "encina.request.handler_missing" ||
            code.EndsWith(".missing", StringComparison.Ordinal))
        {
            return (int)HttpStatusCode.NotFound;
        }

        // 409 Conflict
        if (code.EndsWith(".conflict", StringComparison.Ordinal) ||
            code.EndsWith(".already_exists", StringComparison.Ordinal) ||
            code.EndsWith(".duplicate", StringComparison.Ordinal))
        {
            return (int)HttpStatusCode.Conflict;
        }

        // 500 Internal Server Error: Default for unknown/unhandled errors
        return (int)HttpStatusCode.InternalServerError;
    }

    internal static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        500 => "Internal Server Error",
        _ => "An error occurred"
    };

    /// <summary>
    /// Internal Problem Details response structure following RFC 7807.
    /// </summary>
    private sealed class ProblemDetailsResponse
    {
        public int Status { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public string? Instance { get; set; }
        public Dictionary<string, object?> Extensions { get; } = [];
    }
}
