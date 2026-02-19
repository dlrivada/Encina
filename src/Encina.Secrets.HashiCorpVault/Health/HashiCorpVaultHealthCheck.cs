using Microsoft.Extensions.Diagnostics.HealthChecks;
using VaultSharp;

namespace Encina.Secrets.HashiCorpVault.Health;

/// <summary>
/// Health check for HashiCorp Vault connectivity.
/// </summary>
/// <remarks>
/// Verifies connectivity by calling the Vault system health endpoint (<c>/sys/health</c>).
/// </remarks>
public sealed class HashiCorpVaultHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-secrets-vault";

    private static readonly string[] DefaultTags = ["encina", "secrets", "vault", "ready"];

    private readonly IVaultClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashiCorpVaultHealthCheck"/> class.
    /// </summary>
    /// <param name="client">The VaultSharp client.</param>
    public HashiCorpVaultHealthCheck(IVaultClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Gets the default tags for the Vault health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await _client.V1.System.GetHealthStatusAsync();

            if (health.Initialized && !health.Sealed)
            {
                return HealthCheckResult.Healthy("HashiCorp Vault is accessible, initialized, and unsealed.");
            }

            if (health.Sealed)
            {
                return HealthCheckResult.Degraded("HashiCorp Vault is sealed.");
            }

            return HealthCheckResult.Degraded("HashiCorp Vault is not initialized.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "HashiCorp Vault is not accessible.",
                ex);
        }
    }
}
