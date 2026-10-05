using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR.Health;

/// <summary>
/// Health check that verifies crypto-shredding is installed and its key provider resolves.
/// </summary>
/// <remarks>
/// <para>
/// Unhealthy when the store serializer is not a <see cref="CryptoShredderSerializer"/>, when the installation check
/// fails (the contract modifier was replaced or bypassed, or a nested canary is not encrypted), or when
/// <see cref="ISubjectKeyProvider"/> cannot be resolved in a scope. The data reports the key provider type, the
/// number of crypto contracts built and the number of types rejected at runtime; never a subject id.
/// </para>
/// <para>
/// Enable via <see cref="CryptoShreddingOptions.AddHealthCheck"/>:
/// <code>
/// services.AddEncinaMartenGdpr(options => options.AddHealthCheck = true);
/// </code>
/// </para>
/// </remarks>
public sealed class CryptoShreddingHealthCheck : IHealthCheck
{
    /// <summary>
    /// Default health check name.
    /// </summary>
    public const string DefaultName = "encina-crypto-shredding";

    private static readonly string[] DefaultTags = ["encina", "gdpr", "crypto-shredding", "security", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CryptoShreddingHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoShreddingHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve crypto-shredding services.</param>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    public CryptoShreddingHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<CryptoShreddingHealthCheck> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the default tags for the crypto-shredding health check.
    /// </summary>
    internal static IEnumerable<string> Tags => DefaultTags;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(Check());
        }
        catch (Exception ex)
        {
            // Only the exception type: the message and the exception object can carry personal data.
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Crypto-shredding health check failed with exception: {ex.GetType().Name}"));
        }
    }

    private HealthCheckResult Check()
    {
        var store = _serviceProvider.GetService<IDocumentStore>();
        if (store?.Options.Serializer() is not CryptoShredderSerializer serializer)
        {
            return HealthCheckResult.Unhealthy("The Marten store serializer is not the crypto-shredding serializer.");
        }

        try
        {
            serializer.VerifyContractModifierInstalled();
        }
        catch (CryptoShreddingConfigurationException ex)
        {
            return HealthCheckResult.Unhealthy($"Crypto-shredding installation check failed: {ex.Problem}.");
        }

        using var scope = _serviceProvider.CreateScope();
        if (scope.ServiceProvider.GetService<ISubjectKeyProvider>() is not { } keyProvider)
        {
            return HealthCheckResult.Unhealthy("Missing service: ISubjectKeyProvider is not registered.");
        }

        var data = new Dictionary<string, object>
        {
            ["keyProviderType"] = keyProvider.GetType().Name,
            ["cryptoContractCount"] = serializer.Registry.CryptoContractCount,
            ["misconfiguredTypeCount"] = serializer.Registry.MisconfiguredTypeCount
        };

        _logger.HealthCheckCompleted("Healthy", serializer.Registry.CryptoContractCount, serializer.Registry.MisconfiguredTypeCount);
        return HealthCheckResult.Healthy("Crypto-shredding is installed and its key provider resolves.", data);
    }
}
