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
/// This handler automatically:
/// - Resolves the Refit client from DI
/// - Executes the API call through the request's ExecuteAsync method
/// - Converts HTTP errors to EncinaError (Railway Oriented Programming)
/// - Handles ApiException (from Refit) and general exceptions
/// </remarks>
public sealed partial class RestApiRequestHandler<TRequest, TApiClient, TResponse>
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRestApiRequest<TApiClient, TResponse>
    where TApiClient : class
{
    private readonly TApiClient _apiClient;
    private readonly ILogger<RestApiRequestHandler<TRequest, TApiClient, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestApiRequestHandler{TRequest, TApiClient, TResponse}"/> class.
    /// </summary>
    /// <param name="apiClient">The Refit-generated API client.</param>
    /// <param name="logger">The logger instance.</param>
    public RestApiRequestHandler(
        TApiClient apiClient,
        ILogger<RestApiRequestHandler<TRequest, TApiClient, TResponse>> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        try
        {
            LogExecutingApiCall(requestType, apiClientType, Guid.NewGuid().ToString());

            var response = await request.ExecuteAsync(_apiClient, cancellationToken)
                .ConfigureAwait(false);

            LogApiCallSucceeded(requestType, apiClientType, string.Empty);
            return response;
        }
        catch (ApiException apiEx)
        {
            // Refit-specific exception with HTTP status code and content
            LogApiException(
                requestType,
                apiClientType,
                (int)apiEx.StatusCode,
                apiEx.Message,
                string.Empty);

            return EncinaError.New(
                $"API call failed with status {apiEx.StatusCode}: {apiEx.Content ?? apiEx.Message}",
                apiEx);
        }
        catch (HttpRequestException httpEx)
        {
            // General HTTP request failure
            LogHttpException(requestType, apiClientType, httpEx.Message, string.Empty);

            return EncinaError.New(
                $"HTTP request failed: {httpEx.Message}",
                httpEx);
        }
        catch (TaskCanceledException tcEx) when (tcEx.CancellationToken == cancellationToken)
        {
            // Request was cancelled by caller
            LogRequestCancelled(requestType, apiClientType, string.Empty);

            return EncinaError.New(
                $"API request was cancelled",
                tcEx);
        }
        catch (TaskCanceledException tcEx)
        {
            // Timeout (not cancelled by caller)
            LogRequestTimedOut(requestType, apiClientType, string.Empty);

            return EncinaError.New(
                $"API request timed out",
                tcEx);
        }
        catch (Exception ex)
        {
            LogUnexpectedException(requestType, apiClientType, ex.Message, string.Empty);

            return EncinaError.New(ex);
        }
    }

    #region Logging

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Executing API call {RequestType} using {ApiClientType} (CorrelationId: {CorrelationId})")]
    private partial void LogExecutingApiCall(string requestType, string apiClientType, string correlationId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "API call {RequestType} using {ApiClientType} succeeded (CorrelationId: {CorrelationId})")]
    private partial void LogApiCallSucceeded(string requestType, string apiClientType, string correlationId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "API call {RequestType} using {ApiClientType} failed with status {StatusCode}: {Message} (CorrelationId: {CorrelationId})")]
    private partial void LogApiException(string requestType, string apiClientType, int statusCode, string message, string correlationId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "HTTP request {RequestType} using {ApiClientType} failed: {Message} (CorrelationId: {CorrelationId})")]
    private partial void LogHttpException(string requestType, string apiClientType, string message, string correlationId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "API request {RequestType} using {ApiClientType} was cancelled (CorrelationId: {CorrelationId})")]
    private partial void LogRequestCancelled(string requestType, string apiClientType, string correlationId);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "API request {RequestType} using {ApiClientType} timed out (CorrelationId: {CorrelationId})")]
    private partial void LogRequestTimedOut(string requestType, string apiClientType, string correlationId);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "Unexpected exception in API call {RequestType} using {ApiClientType}: {Message} (CorrelationId: {CorrelationId})")]
    private partial void LogUnexpectedException(string requestType, string apiClientType, string message, string correlationId);

    #endregion
}
