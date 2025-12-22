using Dapr;
using Dapr.Client;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Dapr;

/// <summary>
/// Handler for Dapr secret management requests.
/// </summary>
/// <typeparam name="TRequest">The type of the request implementing <see cref="IDaprSecretRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response containing secret data.</typeparam>
/// <remarks>
/// This handler:
/// - Retrieves secrets from Dapr Secret Management API
/// - Converts <see cref="DaprException"/> to <see cref="EncinaError"/> for Railway Oriented Programming
/// - Supports multiple secret stores (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, etc.)
/// - Provides automatic secret rotation and secure access via Dapr sidecar
/// - IMPORTANT: Secrets are never logged for security reasons
/// </remarks>
public sealed partial class DaprSecretHandler<TRequest, TResponse>
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IDaprSecretRequest<TResponse>
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprSecretHandler<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DaprSecretHandler{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="daprClient">The Dapr client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public DaprSecretHandler(
        DaprClient daprClient,
        ILogger<DaprSecretHandler<TRequest, TResponse>> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    /// <summary>
    /// Handles the Dapr secret retrieval request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains either the secret response or a Encina error.
    /// </returns>
    public async Task<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // SECURITY: Never log the secret name or key in production
            LogSecretRetrievalStarting(request.SecretStoreName);

            var response = await request.ExecuteAsync(_daprClient, cancellationToken).ConfigureAwait(false);

            LogSecretRetrievalSucceeded(request.SecretStoreName);

            return response;
        }
        catch (DaprException daprEx)
        {
            LogSecretRetrievalFailed(request.SecretStoreName, daprEx);

            return EncinaError.New(
                $"Dapr secret retrieval failed for store '{request.SecretStoreName}': {daprEx.Message}",
                daprEx);
        }
        catch (Exception ex)
        {
            LogSecretRetrievalFailed(request.SecretStoreName, ex);

            return EncinaError.New(
                $"Unexpected error during secret retrieval for store '{request.SecretStoreName}': {ex.Message}",
                ex);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting Dapr secret retrieval from store '{SecretStoreName}'")]
    partial void LogSecretRetrievalStarting(string secretStoreName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Dapr secret retrieval succeeded from store '{SecretStoreName}'")]
    partial void LogSecretRetrievalSucceeded(string secretStoreName);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Dapr secret retrieval failed from store '{SecretStoreName}'")]
    partial void LogSecretRetrievalFailed(string secretStoreName, Exception exception);
}
