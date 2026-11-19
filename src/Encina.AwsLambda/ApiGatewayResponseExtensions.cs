using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using LanguageExt;

namespace Encina.AwsLambda;

/// <summary>
/// Extension methods for converting Encina results to AWS API Gateway responses.
/// </summary>
/// <remarks>
/// <para>
/// These extensions enable seamless integration between Encina's Railway Oriented Programming
/// approach (Either&lt;EncinaError, T&gt;) and AWS API Gateway responses (both REST API and HTTP API).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public async Task&lt;APIGatewayProxyResponse&gt; CreateOrder(
///     APIGatewayProxyRequest request, ILambdaContext context)
/// {
///     var command = JsonSerializer.Deserialize&lt;CreateOrder&gt;(request.Body);
///     var result = await _encina.Send(command!);
///     return result.ToApiGatewayResponse();
/// }
/// </code>
/// </example>
public static class ApiGatewayResponseExtensions
{
    private const string HeaderContentType = "Content-Type";
    private const string MediaTypeJson = "application/json";
    private const string MediaTypeProblemJson = "application/problem+json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Converts an Either result to an API Gateway proxy response.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The Either result from an Encina operation.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 200).</param>
    /// <param name="errorStatusCode">Optional HTTP status code for errors. If not provided, the status of the error's category is used.</param>
    /// <returns>The API Gateway proxy response.</returns>
    /// <example>
    /// <code>
    /// var result = await encina.Send(new GetOrderById(orderId));
    /// return result.ToApiGatewayResponse();
    /// </code>
    /// </example>
    public static APIGatewayProxyResponse ToApiGatewayResponse<T>(
        this Either<EncinaError, T> result,
        int successStatusCode = 200,
        int? errorStatusCode = null)
    {
        return result.Match(
            Right: value => CreateSuccessResponse(value, successStatusCode),
            Left: error => error.ToProblemDetailsResponse(errorStatusCode));
    }

    /// <summary>
    /// Converts an EncinaError to an RFC 7807 Problem Details API Gateway response.
    /// </summary>
    /// <param name="error">The Encina error.</param>
    /// <param name="statusCode">Optional HTTP status code. If not provided, the status of the error's category is used.</param>
    /// <returns>The API Gateway proxy response with Problem Details.</returns>
    /// <remarks>
    /// <para>
    /// The status is read from the error's <see cref="ErrorCategory"/> through <see cref="ErrorCategoryHttp"/> (ADR-062);
    /// the helper never parses the error code text. An error created without a category still gets one from the
    /// legacy inference of core (<c>LegacyErrorCategoryInference</c>) until F2 (#3740) removes it. The title is the
    /// reason phrase of the status written, taken from the core table only when it has an entry (otherwise
    /// <c>An error occurred</c>). An explicit <paramref name="statusCode"/> overrides the category's status and is
    /// kept as given, even outside the table (for example 418).
    /// </para>
    /// <para>
    /// <c>Retry-After</c> is written for 429 and 503 when the error carries a retry delay, and
    /// <c>WWW-Authenticate</c> for 401, as <see cref="ErrorCategoryHttp.GetResponseHeaders(EncinaError, int)"/> decides.
    /// The error message is never written to the body.
    /// </para>
    /// </remarks>
    public static APIGatewayProxyResponse ToProblemDetailsResponse(
        this EncinaError error,
        int? statusCode = null)
    {
        var (effectiveStatusCode, body, headers) = BuildProblemDetails(error, statusCode);

        return new APIGatewayProxyResponse
        {
            StatusCode = effectiveStatusCode,
            Body = body,
            Headers = headers
        };
    }

    /// <summary>
    /// Converts an Either result to a Created (201) response with location header.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The Either result from an Encina operation.</param>
    /// <param name="locationFactory">Factory function to generate the location URL from the result.</param>
    /// <returns>The API Gateway proxy response with 201 status and Location header.</returns>
    /// <example>
    /// <code>
    /// var result = await encina.Send(new CreateOrder(/* ... */));
    /// return result.ToCreatedResponse(order => $"/orders/{order.Id}");
    /// </code>
    /// </example>
    public static APIGatewayProxyResponse ToCreatedResponse<T>(
        this Either<EncinaError, T> result,
        Func<T, string> locationFactory)
    {
        ArgumentNullException.ThrowIfNull(locationFactory);

        return result.Match(
            Right: value => new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Body = JsonSerializer.Serialize(value, JsonOptions),
                Headers = new Dictionary<string, string>
                {
                    [HeaderContentType] = MediaTypeJson,
                    ["Location"] = locationFactory(value)
                }
            },
            Left: error => error.ToProblemDetailsResponse());
    }

    /// <summary>
    /// Converts an Either&lt;EncinaError, Unit&gt; result to a No Content (204) response.
    /// </summary>
    /// <param name="result">The Either result from an Encina operation.</param>
    /// <returns>The API Gateway proxy response with 204 status (success) or error response.</returns>
    /// <example>
    /// <code>
    /// var result = await encina.Send(new DeleteOrder(orderId));
    /// return result.ToNoContentResponse();
    /// </code>
    /// </example>
    public static APIGatewayProxyResponse ToNoContentResponse(
        this Either<EncinaError, Unit> result)
    {
        return result.Match(
            Right: _ => new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.NoContent
            },
            Left: error => error.ToProblemDetailsResponse());
    }

    /// <summary>
    /// Converts an Either result to an API Gateway HTTP API (V2) response.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The Either result from an Encina operation.</param>
    /// <param name="successStatusCode">HTTP status code for success (default: 200).</param>
    /// <param name="errorStatusCode">Optional HTTP status code for errors.</param>
    /// <returns>The API Gateway HTTP API response.</returns>
    public static APIGatewayHttpApiV2ProxyResponse ToHttpApiResponse<T>(
        this Either<EncinaError, T> result,
        int successStatusCode = 200,
        int? errorStatusCode = null)
    {
        return result.Match(
            Right: value => new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = successStatusCode,
                Body = JsonSerializer.Serialize(value, JsonOptions),
                Headers = new Dictionary<string, string>
                {
                    [HeaderContentType] = MediaTypeJson
                }
            },
            Left: error =>
            {
                var (effectiveStatusCode, body, headers) = BuildProblemDetails(error, errorStatusCode);

                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = effectiveStatusCode,
                    Body = body,
                    Headers = headers
                };
            });
    }

    // One builder for the v1 and v2 shapes: status, title, body and headers are decided once (ADR-062).
    // The body never carries the error message (AGENTS.md section 3).
    private static (int StatusCode, string Body, Dictionary<string, string> Headers) BuildProblemDetails(
        EncinaError error,
        int? statusCode)
    {
        var effectiveStatusCode = statusCode ?? error.GetStatusCode();

        var problemDetails = new ProblemDetailsResponse
        {
            Status = effectiveStatusCode,
            Title = ErrorCategoryHttp.GetReasonPhrase(effectiveStatusCode)
        };

        // Add error code for client-side handling
        var errorCode = error.GetCode().IfNone(string.Empty);
        if (!string.IsNullOrEmpty(errorCode))
        {
            problemDetails.Extensions["errorCode"] = errorCode;
        }

        var headers = new Dictionary<string, string>
        {
            [HeaderContentType] = MediaTypeProblemJson
        };

        foreach (var header in ErrorCategoryHttp.GetResponseHeaders(error, effectiveStatusCode))
        {
            headers[header.Key] = header.Value;
        }

        return (effectiveStatusCode, JsonSerializer.Serialize(problemDetails, JsonOptions), headers);
    }

    private static APIGatewayProxyResponse CreateSuccessResponse<T>(T value, int statusCode)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(value, JsonOptions),
            Headers = new Dictionary<string, string>
            {
                [HeaderContentType] = MediaTypeJson
            }
        };
    }

    /// <summary>
    /// Internal Problem Details response structure following RFC 7807. It has no detail member on purpose:
    /// the error message never reaches a client.
    /// </summary>
    private sealed class ProblemDetailsResponse
    {
        public int Status { get; set; }
        public string Title { get; set; } = string.Empty;
        public Dictionary<string, object?> Extensions { get; } = [];
    }
}
