using Dapr;
using Dapr.Client;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Dapr;

/// <summary>
/// Handler for Dapr input/output binding requests.
/// </summary>
/// <typeparam name="TRequest">The type of the request implementing <see cref="IDaprBindingRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// This handler:
/// - Executes binding operations (create, get, delete, list) through Dapr Bindings API
/// - Converts <see cref="DaprException"/> to <see cref="EncinaError"/> for Railway Oriented Programming
/// - Supports 100+ pre-built Dapr bindings (AWS, Azure, GCP, databases, message brokers, etc.)
/// - Provides automatic retry and error handling via Dapr sidecar
/// </remarks>
public sealed partial class DaprBindingHandler<TRequest, TResponse>
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IDaprBindingRequest<TResponse>
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprBindingHandler<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DaprBindingHandler{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="daprClient">The Dapr client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public DaprBindingHandler(
        DaprClient daprClient,
        ILogger<DaprBindingHandler<TRequest, TResponse>> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    /// <summary>
    /// Handles the Dapr binding request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains either the response or a Encina error.
    /// </returns>
    public async Task<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            LogBindingInvocationStarting(request.BindingName, request.Operation);

            var response = await request.ExecuteAsync(_daprClient, cancellationToken).ConfigureAwait(false);

            LogBindingInvocationSucceeded(request.BindingName, request.Operation);

            return response;
        }
        catch (DaprException daprEx)
        {
            LogBindingInvocationFailed(request.BindingName, request.Operation, daprEx);

            return EncinaError.New(
                $"Dapr binding invocation failed for binding '{request.BindingName}', operation '{request.Operation}': {daprEx.Message}",
                daprEx);
        }
        catch (Exception ex)
        {
            LogBindingInvocationFailed(request.BindingName, request.Operation, ex);

            return EncinaError.New(
                $"Unexpected error during binding invocation for binding '{request.BindingName}', operation '{request.Operation}': {ex.Message}",
                ex);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting Dapr binding invocation for binding '{BindingName}', operation '{Operation}'")]
    partial void LogBindingInvocationStarting(string bindingName, string operation);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Dapr binding invocation succeeded for binding '{BindingName}', operation '{Operation}'")]
    partial void LogBindingInvocationSucceeded(string bindingName, string operation);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Dapr binding invocation failed for binding '{BindingName}', operation '{Operation}'")]
    partial void LogBindingInvocationFailed(string bindingName, string operation, Exception exception);
}
