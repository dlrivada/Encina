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
    /// <param name="errorStatusCode">Optional HTTP status code for errors. If not provided, the status of the error's category is used.</param>
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
    /// <param name="statusCode">Optional HTTP status code. If not provided, the status of the error's category is used.</param>
    /// <returns>The HTTP response with Problem Details.</returns>
    /// <remarks>
    /// <para>
    /// The status is read from the error's <see cref="ErrorCategory"/> through <see cref="ErrorCategoryHttp"/> (ADR-062);
    /// the error code text is never parsed. The title is the reason phrase of the status written. An explicit
    /// <paramref name="statusCode"/> overrides the category's status.
    /// </para>
    /// <para>
    /// <c>Retry-After</c> is written for 429 and 503 when the error carries a retry delay, and
    /// <c>WWW-Authenticate</c> for 401, as <see cref="ErrorCategoryHttp.GetResponseHeaders(EncinaError, int)"/> decides.
    /// </para>
    /// </remarks>
    public static async ValueTask<HttpResponseData> ToProblemDetailsResponse(
        this EncinaError error,
        HttpRequestData request,
        int? statusCode = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveStatusCode = statusCode ?? error.GetStatusCode();
        var response = request.CreateResponse((HttpStatusCode)effectiveStatusCode);

        var problemDetails = new ProblemDetailsResponse
        {
            Status = effectiveStatusCode,
            Title = ErrorCategoryHttp.GetReasonPhrase(effectiveStatusCode),
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
        foreach (var header in ErrorCategoryHttp.GetResponseHeaders(error, effectiveStatusCode))
        {
            response.Headers.Add(header.Key, header.Value);
        }

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
