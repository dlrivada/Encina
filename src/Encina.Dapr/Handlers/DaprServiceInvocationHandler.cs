using Dapr;
using Dapr.Client;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Dapr;

/// <summary>
/// Generic handler for Dapr service-to-service invocation requests.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// This handler:
/// - Uses DaprClient for service invocation
/// - Handles DaprException and converts to EncinaError
/// - Provides automatic retries, service discovery, and mTLS via Dapr sidecar
/// - Integrates with distributed tracing and observability
/// </remarks>
public sealed partial class DaprServiceInvocationHandler<TRequest, TResponse>
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IDaprServiceInvocationRequest<TResponse>
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprServiceInvocationHandler<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DaprServiceInvocationHandler{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="daprClient">The Dapr client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public DaprServiceInvocationHandler(
        DaprClient daprClient,
        ILogger<DaprServiceInvocationHandler<TRequest, TResponse>> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    /// <summary>
    /// Handles the Dapr service invocation request.
    /// </summary>
    public async Task<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;

        try
        {
            LogInvokingService(
                requestType,
                request.AppId,
                request.MethodName,
                request.HttpMethod.ToString());

            var response = await request.InvokeAsync(_daprClient, cancellationToken)
                .ConfigureAwait(false);

            LogServiceInvocationSucceeded(
                requestType,
                request.AppId,
                request.MethodName);

            return response;
        }
        catch (DaprException daprEx)
        {
            // Dapr-specific errors (service not found, sidecar unavailable, etc.)
            LogDaprException(
                requestType,
                request.AppId,
                request.MethodName,
                daprEx.Message);

            return EncinaError.New(
                $"Dapr service invocation failed for {request.AppId}/{request.MethodName}: {daprEx.Message}",
                daprEx);
        }
        catch (HttpRequestException httpEx)
        {
            // HTTP-level errors from the invoked service
            LogHttpException(
                requestType,
                request.AppId,
                request.MethodName,
                httpEx.Message);

            return EncinaError.New(
                $"HTTP error invoking {request.AppId}/{request.MethodName}: {httpEx.Message}",
                httpEx);
        }
        catch (TaskCanceledException tcEx) when (tcEx.CancellationToken == cancellationToken)
        {
            // Request was cancelled by caller
            LogRequestCancelled(requestType, request.AppId, request.MethodName);

            return EncinaError.New(
                $"Service invocation to {request.AppId}/{request.MethodName} was cancelled",
                tcEx);
        }
        catch (TaskCanceledException tcEx)
        {
            // Timeout
            LogRequestTimedOut(requestType, request.AppId, request.MethodName);

            return EncinaError.New(
                $"Service invocation to {request.AppId}/{request.MethodName} timed out",
                tcEx);
        }
        catch (Exception ex)
        {
            LogUnexpectedException(
                requestType,
                request.AppId,
                request.MethodName,
                ex.Message);

            return EncinaError.New(ex);
        }
    }

    #region Logging

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Invoking Dapr service: {RequestType} -> {AppId}/{MethodName} ({HttpMethod})")]
    private partial void LogInvokingService(
        string requestType,
        string appId,
        string methodName,
        string httpMethod);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Dapr service invocation succeeded: {RequestType} -> {AppId}/{MethodName}")]
    private partial void LogServiceInvocationSucceeded(
        string requestType,
        string appId,
        string methodName);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Dapr exception invoking {RequestType} -> {AppId}/{MethodName}: {Message}")]
    private partial void LogDaprException(
        string requestType,
        string appId,
        string methodName,
        string message);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "HTTP exception invoking {RequestType} -> {AppId}/{MethodName}: {Message}")]
    private partial void LogHttpException(
        string requestType,
        string appId,
        string methodName,
        string message);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Service invocation cancelled: {RequestType} -> {AppId}/{MethodName}")]
    private partial void LogRequestCancelled(
        string requestType,
        string appId,
        string methodName);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Service invocation timed out: {RequestType} -> {AppId}/{MethodName}")]
    private partial void LogRequestTimedOut(
        string requestType,
        string appId,
        string methodName);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "Unexpected exception invoking {RequestType} -> {AppId}/{MethodName}: {Message}")]
    private partial void LogUnexpectedException(
        string requestType,
        string appId,
        string methodName,
        string message);

    #endregion
}
