using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;
using Encina.Security.Encryption.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR.Health;

/// <summary>
/// Health check that verifies the crypto-shredding subsystem is operational by validating
/// that required services are resolvable and the key store is accessible.
/// </summary>
/// <remarks>
/// <para>
/// This health check performs the following verifications:
/// <list type="number">
/// <item><description>Resolves <see cref="IFieldEncryptor"/> from the DI container (from <c>AddEncinaEncryption</c>).</description></item>
/// <item><description>Resolves <see cref="ISubjectKeyProvider"/> from the DI container.</description></item>
/// <item><description>Checks that the <see cref="CryptoShreddedPropertyCache"/> has discovered event types
/// (Degraded if empty, which may indicate auto-registration has not run).</description></item>
/// </list>
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
            using var scope = _serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            // 1. Verify IFieldEncryptor is resolvable (from AddEncinaEncryption)
            var fieldEncryptor = scopedProvider.GetService<IFieldEncryptor>();

            if (fieldEncryptor is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Missing prerequisite: IFieldEncryptor is not registered. "
                    + "Ensure AddEncinaEncryption() is called before AddEncinaMartenGdpr()."));
            }

            // 2. Verify ISubjectKeyProvider is resolvable
            var subjectKeyProvider = scopedProvider.GetService<ISubjectKeyProvider>();

            if (subjectKeyProvider is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Missing service: ISubjectKeyProvider is not registered."));
            }

            // 3. Check if property cache has discovered types
            if (!CryptoShreddedPropertyCache.HasAnyRegisteredTypes)
            {
                var data = new Dictionary<string, object>
                {
                    ["keyProviderType"] = subjectKeyProvider.GetType().Name
                };

                return Task.FromResult(HealthCheckResult.Degraded(
                    "Crypto-shredding property cache is empty. No event types with [CryptoShredded] "
                    + "properties have been discovered. This may be normal if no events have been "
                    + "serialized yet, or it could indicate that auto-registration was not configured.",
                    data: data));
            }

            // All checks passed
            var healthData = new Dictionary<string, object>
            {
                ["keyProviderType"] = subjectKeyProvider.GetType().Name,
                ["cachedTypeCount"] = CryptoShreddedPropertyCache.CachedTypeCount
            };

            _logger.HealthCheckCompleted("Healthy", CryptoShreddedPropertyCache.CachedTypeCount);

            return Task.FromResult(HealthCheckResult.Healthy(
                "Crypto-shredding subsystem is healthy. Key provider and property cache are operational.",
                healthData));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Crypto-shredding health check failed with exception: {ex.Message}",
                exception: ex));
        }
    }
}
