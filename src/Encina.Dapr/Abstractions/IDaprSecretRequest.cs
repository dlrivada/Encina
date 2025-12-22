using Dapr.Client;

namespace Encina.Dapr;

/// <summary>
/// Marker interface for requests that retrieve secrets from Dapr Secret Management.
/// </summary>
/// <typeparam name="TResponse">The type of the response containing the secret value(s).</typeparam>
/// <remarks>
/// Dapr Secret Management provides:
/// - Secure secret retrieval from secret stores (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, etc.)
/// - Automatic secret rotation support
/// - Scope-based access control (limit which apps can access which secrets)
/// - Secrets referenced by name and optional keys
/// - Metadata support for store-specific configuration
///
/// Returns <typeparamref name="TResponse"/> containing the secret data in Railway Oriented Programming.
/// </remarks>
/// <example>
/// <code>
/// // Retrieve database connection string from Azure Key Vault
/// public record GetDatabaseConnectionStringQuery()
///     : IQuery&lt;string&gt;, IDaprSecretRequest&lt;string&gt;
/// {
///     public string SecretStoreName => "azurekeyvault";
///     public string SecretName => "database-connection-string";
///     public string? SecretKey => null; // Single-value secret
///
///     public async Task&lt;string&gt; ExecuteAsync(
///         DaprClient daprClient,
///         CancellationToken cancellationToken)
///     {
///         var secrets = await daprClient.GetSecretAsync(
///             SecretStoreName,
///             SecretName,
///             cancellationToken: cancellationToken);
///
///         // For single-value secrets, the key is typically the secret name itself
///         return secrets.TryGetValue(SecretName, out var value)
///             ? value
///             : throw new KeyNotFoundException($"Secret '{SecretName}' not found");
///     }
/// }
///
/// // Retrieve multiple secrets (e.g., API credentials)
/// public record GetApiCredentialsQuery()
///     : IQuery&lt;ApiCredentials&gt;, IDaprSecretRequest&lt;ApiCredentials&gt;
/// {
///     public string SecretStoreName => "hashicorp-vault";
///     public string SecretName => "api-credentials";
///     public string? SecretKey => null;
///
///     public async Task&lt;ApiCredentials&gt; ExecuteAsync(
///         DaprClient daprClient,
///         CancellationToken cancellationToken)
///     {
///         var secrets = await daprClient.GetSecretAsync(
///             SecretStoreName,
///             SecretName,
///             cancellationToken: cancellationToken);
///
///         return new ApiCredentials
///         {
///             ApiKey = secrets["api-key"],
///             ApiSecret = secrets["api-secret"],
///             Endpoint = secrets["endpoint"]
///         };
///     }
/// }
///
/// // Use through Encina
/// var result = await mediator.Send(new GetDatabaseConnectionStringQuery());
/// result.Match(
///     Right: connectionString => ConfigureDatabase(connectionString),
///     Left: error => Console.WriteLine($"Error retrieving secret: {error.Message}")
/// );
/// </code>
/// </example>
public interface IDaprSecretRequest<TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// Gets the name of the Dapr secret store component.
    /// </summary>
    string SecretStoreName { get; }

    /// <summary>
    /// Gets the name of the secret to retrieve.
    /// </summary>
    string SecretName { get; }

    /// <summary>
    /// Gets the optional secret key for multi-value secrets.
    /// </summary>
    /// <remarks>
    /// If null, all keys for the secret are retrieved.
    /// If specified, only the value for that key is retrieved.
    /// </remarks>
    string? SecretKey { get; }

    /// <summary>
    /// Executes the secret retrieval operation using the provided Dapr client.
    /// </summary>
    /// <param name="daprClient">The Dapr client instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the secret response.</returns>
    Task<TResponse> ExecuteAsync(DaprClient daprClient, CancellationToken cancellationToken);
}
