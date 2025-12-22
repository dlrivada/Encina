using Dapr;
using Dapr.Client;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Dapr;

/// <summary>
/// Handler for Dapr state management requests.
/// </summary>
/// <typeparam name="TRequest">The type of the request implementing <see cref="IDaprStateRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// This handler:
/// - Executes state operations (get/save/delete) through Dapr State Management API
/// - Converts <see cref="DaprException"/> to <see cref="MediatorError"/> for Railway Oriented Programming
/// - Supports any Dapr state store (Redis, Cosmos DB, SQL Server, etc.)
/// - Provides automatic retry and consistency guarantees via Dapr sidecar
/// </remarks>
public sealed partial class DaprStateHandler<TRequest, TResponse>
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IDaprStateRequest<TResponse>
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprStateHandler<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DaprStateHandler{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="daprClient">The Dapr client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public DaprStateHandler(
        DaprClient daprClient,
        ILogger<DaprStateHandler<TRequest, TResponse>> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    /// <summary>
    /// Handles the Dapr state management request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains either the response or a mediator error.
    /// </returns>
    public async Task<Either<MediatorError, TResponse>> Handle(
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            LogStateOperationStarting(request.StoreName, request.StateKey);

            var response = await request.ExecuteAsync(_daprClient, cancellationToken).ConfigureAwait(false);

            LogStateOperationSucceeded(request.StoreName, request.StateKey);

            return response;
        }
        catch (DaprException daprEx)
        {
            LogStateOperationFailed(request.StoreName, request.StateKey, daprEx);

            return MediatorError.New(
                $"Dapr state operation failed for store '{request.StoreName}', key '{request.StateKey}': {daprEx.Message}",
                daprEx);
        }
        catch (Exception ex)
        {
            LogStateOperationFailed(request.StoreName, request.StateKey, ex);

            return MediatorError.New(
                $"Unexpected error during state operation for store '{request.StoreName}', key '{request.StateKey}': {ex.Message}",
                ex);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting Dapr state operation for store '{StoreName}', key '{StateKey}'")]
    partial void LogStateOperationStarting(string storeName, string stateKey);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Dapr state operation succeeded for store '{StoreName}', key '{StateKey}'")]
    partial void LogStateOperationSucceeded(string storeName, string stateKey);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Dapr state operation failed for store '{StoreName}', key '{StateKey}'")]
    partial void LogStateOperationFailed(string storeName, string stateKey, Exception exception);
}
