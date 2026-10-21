using System.Diagnostics.CodeAnalysis;
using System.Net;
using Encina.Diagnostics;
using Encina.Recoverability;
using Encina.Refit.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Refit;

namespace Encina.Refit;

/// <summary>
/// Generic handler for REST API requests using Refit clients.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TApiClient">The Refit API client interface type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// This handler automatically:
/// - Resolves the Refit client from DI
/// - Executes the API call through the request's ExecuteAsync method
/// - Converts HTTP errors to EncinaError (Railway Oriented Programming)
/// - Handles ApiException (from Refit) and general exceptions
/// </para>
/// <para>
/// Every failure is an error with a code of <see cref="OutboundErrorCodes"/> and the metadata of
/// <see cref="OutboundErrorMetadataKeys"/> (<c>http.status</c> when a response arrived, <c>http.retry_after</c>
/// when a valid <c>Retry-After</c> header arrived, and <c>transient</c> always), so the retry layers can classify
/// it. The error never carries the response body: its message is fixed text, the <see cref="ApiException"/>
/// (whose content is the body) is not attached, and the text of a network exception (which can name a host) is
/// not copied.
/// </para>
/// <para>
/// A failure that may already have been processed by the server (500, 502, 504, a timeout, a connection that
/// broke after the request was sent) is still transient, and on a POST or PATCH it also carries
/// <c>outbound.ambiguous</c>: retry layers do not retry it automatically unless the request opts in.
/// </para>
/// </remarks>
[SuppressMessage("SonarQube", "S2436:Classes and methods should not have too many generic parameters",
    Justification = "Three generic parameters are required: request type, API client interface type, and response type for type-safe REST API handling.")]
public sealed partial class RestApiRequestHandler<TRequest, TApiClient, TResponse>
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRestApiRequest<TApiClient, TResponse>
    where TApiClient : class
{
    private readonly TApiClient _apiClient;
    private readonly ILogger<RestApiRequestHandler<TRequest, TApiClient, TResponse>> _logger;
    private readonly IRequestContextAccessor? _contextAccessor;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/> class.
    /// </summary>
    /// <param name="apiClient">The Refit-generated API client.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="contextAccessor">
    /// Optional accessor of the ambient request context: its correlation id appears in the logs and its tenant id
    /// is a telemetry attribute.
    /// </param>
    /// <param name="timeProvider">Clock that turns an HTTP-date <c>Retry-After</c> into a delay; defaults to <see cref="TimeProvider.System"/>.</param>
    public RestApiRequestHandler(
        TApiClient apiClient,
        ILogger<RestApiRequestHandler<TRequest, TApiClient, TResponse>> logger,
        IRequestContextAccessor? contextAccessor = null,
        TimeProvider? timeProvider = null)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contextAccessor = contextAccessor;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Handles the REST API request.
    /// </summary>
    public async Task<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var apiClientType = typeof(TApiClient).Name;
        var context = _contextAccessor?.RequestContext;
        var correlationId = context?.CorrelationId ?? string.Empty;
        var tenantId = context?.TenantId;

        var activity = OutboundTelemetry.Start(requestType, tenantId);
        using var methodScope = OutboundMethodCapture.Begin();

        Either<EncinaError, TResponse> outcome;
        try
        {
            LogExecutingApiCall(requestType, apiClientType, correlationId);

            var response = await request.ExecuteAsync(_apiClient, cancellationToken)
                .ConfigureAwait(false);

            LogApiCallSucceeded(requestType, apiClientType, correlationId);
            OutboundTelemetry.Complete(activity, requestType, null, null, null, tenantId);
            return response;
        }
        catch (ApiException apiEx)
        {
            // Refit-specific exception with HTTP status code and content (the body is never read here)
            var statusCodeInt = (int)apiEx.StatusCode;
            LogApiException(requestType, apiClientType, statusCodeInt, apiEx.ForLogging(), correlationId);
            outcome = FromStatus(statusCodeInt, apiEx.Headers?.RetryAfter, apiEx.HttpMethod ?? methodScope.Capture.Method);
        }
        catch (HttpRequestException httpEx)
        {
            // General HTTP request failure
            LogHttpException(requestType, apiClientType, httpEx.ForLogging(), correlationId);
            outcome = httpEx.StatusCode is { } status
                ? FromStatus((int)status, null, methodScope.Capture.Method)
                : FromNetworkFailure(httpEx.HttpRequestError, methodScope.Capture.Method);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Request was cancelled by caller
            LogRequestCancelled(requestType, apiClientType, correlationId);
            outcome = Failure(EncinaErrorCodes.RequestCancelled, "API request was cancelled", transient: false);
        }
        catch (TaskCanceledException)
        {
            // Timeout (not cancelled by caller)
            LogRequestTimedOut(requestType, apiClientType, correlationId);
            outcome = Failure(OutboundErrorCodes.Timeout, "API request timed out", transient: true, ambiguous: IsNonIdempotent(methodScope.Capture.Method));
        }
        catch (Exception ex)
        {
            LogUnexpectedException(requestType, apiClientType, ex.ForLogging(), correlationId);
            outcome = FromUnexpected(ex, methodScope.Capture.Method);
        }

        CompleteTelemetry(activity, requestType, outcome, tenantId);
        return outcome;
    }

    private static void CompleteTelemetry(
        System.Diagnostics.Activity? activity,
        string requestType,
        Either<EncinaError, TResponse> outcome,
        string? tenantId)
    {
        var error = (EncinaError)outcome;
        var details = error.GetDetails();
        bool? transient = details.TryGetValue(OutboundErrorMetadataKeys.Transient, out var t) && t is bool b ? b : null;
        int? status = details.TryGetValue(OutboundErrorMetadataKeys.HttpStatus, out var s) && s is int i ? i : null;
        OutboundTelemetry.Complete(activity, requestType, error.GetCode().IfNone("encina.unknown"), transient, status, tenantId);
    }

    // The fixed-text message names the status and reason phrase only: never the response body.
    private Either<EncinaError, TResponse> FromStatus(int status, System.Net.Http.Headers.RetryConditionHeaderValue? retryAfterHeader, HttpMethod? method)
    {
        var message = $"API call failed with status {status} ({(HttpStatusCode)status})";
        var retryAfter = RetryAfterParser.Parse(retryAfterHeader, _timeProvider);

        return status switch
        {
            >= 200 and < 300 => Failure(OutboundErrorCodes.UnexpectedResponse, message, transient: false, status),
            429 => Failure(OutboundErrorCodes.RateLimited, message, transient: true, status, retryAfter),
            408 => Failure(OutboundErrorCodes.Timeout, message, transient: true, status, retryAfter),
            >= 500 and <= 599 => Failure(
                OutboundErrorCodes.ServerError,
                message,
                transient: true,
                status,
                retryAfter,
                ambiguous: status is 500 or 502 or 504 && IsNonIdempotent(method)),
            _ => Failure(OutboundErrorCodes.ClientError, message, transient: false, status, retryAfter)
        };
    }

    // A connection that never opened proves the request was not sent; any other network failure is ambiguous.
    private static Either<EncinaError, TResponse> FromNetworkFailure(HttpRequestError error, HttpMethod? method) =>
        Failure(
            OutboundErrorCodes.NetworkError,
            "HTTP request failed",
            transient: true,
            ambiguous: error is not (HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError
                or HttpRequestError.SecureConnectionError or HttpRequestError.ProxyTunnelError)
                && IsNonIdempotent(method));

    private static Either<EncinaError, TResponse> FromUnexpected(Exception exception, HttpMethod? method) =>
        exception is TimeoutException or IOException
            ? Failure(OutboundErrorCodes.Unexpected, "Unexpected failure in API call", transient: true, ambiguous: IsNonIdempotent(method))
            : Failure(OutboundErrorCodes.Unexpected, "Unexpected failure in API call", transient: false);

    // Unknown method (a client registered without AddEncinaRefitClient) is treated as non-idempotent.
    private static bool IsNonIdempotent(HttpMethod? method) =>
        method is null || method == HttpMethod.Post || method == HttpMethod.Patch;

    private static Either<EncinaError, TResponse> Failure(
        string code,
        string message,
        bool transient,
        int? status = null,
        TimeSpan? retryAfter = null,
        bool ambiguous = false)
    {
        var details = new Dictionary<string, object?> { [OutboundErrorMetadataKeys.Transient] = transient };

        if (status is { } httpStatus)
        {
            details[OutboundErrorMetadataKeys.HttpStatus] = httpStatus;
        }

        if (retryAfter is { } delay)
        {
            details[OutboundErrorMetadataKeys.HttpRetryAfter] = delay;
        }

        if (ambiguous)
        {
            details[OutboundErrorMetadataKeys.Ambiguous] = true;
        }

        return EncinaErrors.Create(code, message, details: details);
    }

    #region Logging

    [LoggerMessage(
        EventId = 4650,
        Level = LogLevel.Debug,
        Message = "Executing API call {RequestType} using {ApiClientType} (CorrelationId: {CorrelationId})")]
    private partial void LogExecutingApiCall(string requestType, string apiClientType, string correlationId);

    [LoggerMessage(
        EventId = 4651,
        Level = LogLevel.Debug,
        Message = "API call {RequestType} using {ApiClientType} succeeded (CorrelationId: {CorrelationId})")]
    private partial void LogApiCallSucceeded(string requestType, string apiClientType, string correlationId);

    [LoggerMessage(
        EventId = 4652,
        Level = LogLevel.Warning,
        Message = "API call {RequestType} using {ApiClientType} failed with status {StatusCode} (CorrelationId: {CorrelationId})")]
    private partial void LogApiException(string requestType, string apiClientType, int statusCode, Exception exception, string correlationId);

    [LoggerMessage(
        EventId = 4653,
        Level = LogLevel.Error,
        Message = "HTTP request {RequestType} using {ApiClientType} failed (CorrelationId: {CorrelationId})")]
    private partial void LogHttpException(string requestType, string apiClientType, Exception exception, string correlationId);

    [LoggerMessage(
        EventId = 4654,
        Level = LogLevel.Information,
        Message = "API request {RequestType} using {ApiClientType} was cancelled (CorrelationId: {CorrelationId})")]
    private partial void LogRequestCancelled(string requestType, string apiClientType, string correlationId);

    [LoggerMessage(
        EventId = 4655,
        Level = LogLevel.Warning,
        Message = "API request {RequestType} using {ApiClientType} timed out (CorrelationId: {CorrelationId})")]
    private partial void LogRequestTimedOut(string requestType, string apiClientType, string correlationId);

    [LoggerMessage(
        EventId = 4656,
        Level = LogLevel.Error,
        Message = "Unexpected exception in API call {RequestType} using {ApiClientType} (CorrelationId: {CorrelationId})")]
    private partial void LogUnexpectedException(string requestType, string apiClientType, Exception exception, string correlationId);

    #endregion
}
