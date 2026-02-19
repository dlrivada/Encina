using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.Secrets.AzureKeyVault.Health;

/// <summary>
/// Health check for Azure Key Vault connectivity.
/// </summary>
public sealed class KeyVaultHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-secrets-keyvault";

    private static readonly string[] DefaultTags = ["encina", "secrets", "keyvault", "ready"];

    private readonly SecretClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyVaultHealthCheck"/> class.
    /// </summary>
    /// <param name="client">The Azure Key Vault secret client.</param>
    public KeyVaultHealthCheck(SecretClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Gets the default tags for the Key Vault health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt a lightweight list operation (max 1 result) to verify connectivity
            await foreach (var _ in _client.GetPropertiesOfSecretsAsync(cancellationToken))
            {
                break;
            }

            return HealthCheckResult.Healthy("Azure Key Vault is accessible.");
        }
        catch (RequestFailedException ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Azure Key Vault is not accessible. Status: {ex.Status}.",
                ex);
        }
    }
}
