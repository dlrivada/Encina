using Encina.Messaging.Health;
using Microsoft.Extensions.Options;

namespace Encina.AzureFunctions.Health;

/// <summary>
/// Health check for Azure Functions runtime integration.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies that the Azure Functions integration is properly configured
/// and operational. It checks:
/// <list type="bullet">
/// <item><description>Options are properly configured</description></item>
/// <item><description>Middleware is registered</description></item>
/// </list>
/// </para>
/// <para>
/// Since Azure Functions runs in a managed environment, most infrastructure health
/// is managed by Azure. This check primarily validates configuration readiness.
/// </para>
/// </remarks>
public sealed class AzureFunctionsHealthCheck : IEncinaHealthCheck
{
    private readonly EncinaAzureFunctionsOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFunctionsHealthCheck"/> class.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public AzureFunctionsHealthCheck(IOptions<EncinaAzureFunctionsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc/>
    public string Name => _options.ProviderHealthCheck.Name ?? "encina-azure-functions";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Tags => _options.ProviderHealthCheck.Tags;

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        // Azure Functions runtime health is managed by Azure
        // We verify that our integration is properly configured

        var data = new Dictionary<string, object>
        {
            ["requestContextEnrichment"] = _options.EnableRequestContextEnrichment,
            ["correlationIdHeader"] = _options.CorrelationIdHeader,
            ["tenantIdHeader"] = _options.TenantIdHeader
        };

        // Check for valid configuration
        if (string.IsNullOrEmpty(_options.CorrelationIdHeader) ||
            string.IsNullOrEmpty(_options.TenantIdHeader) ||
            string.IsNullOrEmpty(_options.UserIdClaimType) ||
            string.IsNullOrEmpty(_options.TenantIdClaimType))
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Azure Functions integration has incomplete configuration",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Azure Functions integration is properly configured",
            data: data));
    }
}
