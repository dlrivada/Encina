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
    /// <param name="errorStatusCode">Optional HTTP status code for errors. If not provided, will be inferred from error code.</param>
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
    /// <param name="statusCode">Optional HTTP status code. If not provided, will be inferred from error code.</param>
    /// <returns>The API Gateway proxy response with Problem Details.</returns>
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
    public static APIGatewayProxyResponse ToProblemDetailsResponse(
        this EncinaError error,
        int? statusCode = null)
    {
        var effectiveStatusCode = statusCode ?? MapErrorCodeToStatusCode(error);

        var problemDetails = new ProblemDetailsResponse
        {
            Status = effectiveStatusCode,
            Title = GetTitle(effectiveStatusCode),
            Detail = error.Message
        };

        // Add error code for client-side handling
        var errorCode = error.GetCode().IfNone(string.Empty);
        if (!string.IsNullOrEmpty(errorCode))
        {
            problemDetails.Extensions["errorCode"] = errorCode;
        }

        return new APIGatewayProxyResponse
        {
            StatusCode = effectiveStatusCode,
            Body = JsonSerializer.Serialize(problemDetails, JsonOptions),
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/problem+json"
            }
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
                    ["Content-Type"] = "application/json",
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
                    ["Content-Type"] = "application/json"
                }
            },
            Left: error =>
            {
                var effectiveStatusCode = errorStatusCode ?? MapErrorCodeToStatusCode(error);
                var problemDetails = new ProblemDetailsResponse
                {
                    Status = effectiveStatusCode,
                    Title = GetTitle(effectiveStatusCode),
                    Detail = error.Message
                };

                var errorCode = error.GetCode().IfNone(string.Empty);
                if (!string.IsNullOrEmpty(errorCode))
                {
                    problemDetails.Extensions["errorCode"] = errorCode;
                }

                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = effectiveStatusCode,
                    Body = JsonSerializer.Serialize(problemDetails, JsonOptions),
                    Headers = new Dictionary<string, string>
                    {
                        ["Content-Type"] = "application/problem+json"
                    }
                };
            });
    }

    private static APIGatewayProxyResponse CreateSuccessResponse<T>(T value, int statusCode)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(value, JsonOptions),
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            }
        };
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
