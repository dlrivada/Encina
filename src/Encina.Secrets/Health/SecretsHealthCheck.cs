using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.Secrets.Health;

/// <summary>
/// Health check that verifies the <see cref="ISecretProvider"/> service is registered and resolvable.
/// </summary>
/// <remarks>
/// <para>
/// This health check resolves <see cref="ISecretProvider"/> from the DI container
/// to verify it is properly configured.
/// </para>
/// <para>
/// Enable via <see cref="ProviderHealthCheckOptions.Enabled"/>:
/// <code>
/// services.AddEncinaSecretsCaching(options => { });
/// // Or configure health check in provider-specific options
/// </code>
/// </para>
/// </remarks>
public sealed class SecretsHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-secrets";

    private static readonly string[] DefaultTags = ["encina", "secrets", "ready"];

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretsHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve secret services.</param>
    public SecretsHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the default tags for the secrets health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var provider = scope.ServiceProvider.GetService<ISecretProvider>();

        if (provider is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("ISecretProvider is not registered."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Secret provider is registered and resolvable."));
    }
}
