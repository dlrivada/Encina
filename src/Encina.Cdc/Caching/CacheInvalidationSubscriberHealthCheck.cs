using Encina.Caching;
using Encina.Messaging.Health;
using Microsoft.Extensions.Options;

namespace Encina.Cdc.Caching;

/// <summary>
/// Health check for the CDC-driven cache invalidation subscriber service.
/// Verifies that the pub/sub provider is accessible by performing a connectivity test.
/// </summary>
/// <remarks>
/// <para>
/// This health check reports:
/// <list type="bullet">
///   <item><description><b>Healthy</b>: Pub/sub provider is accessible and subscriber is configured</description></item>
///   <item><description><b>Unhealthy</b>: Pub/sub provider is not accessible or connectivity test failed</description></item>
/// </list>
/// </para>
/// <para>
/// Tags: "encina", "cdc", "cache-invalidation", "ready" for Kubernetes readiness probes.
/// </para>
/// </remarks>
internal sealed class CacheInvalidationSubscriberHealthCheck : EncinaHealthCheck
{
    private static readonly string[] DefaultTags = ["encina", "cdc", "cache-invalidation", "ready"];

    private readonly IPubSubProvider _pubSubProvider;
    private readonly QueryCacheInvalidationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheInvalidationSubscriberHealthCheck"/> class.
    /// </summary>
    /// <param name="pubSubProvider">The pub/sub provider to check connectivity.</param>
    /// <param name="options">Cache invalidation configuration options.</param>
    public CacheInvalidationSubscriberHealthCheck(
        IPubSubProvider pubSubProvider,
        IOptions<QueryCacheInvalidationOptions> options)
        : base("encina-cdc-cache-invalidation", DefaultTags)
    {
        ArgumentNullException.ThrowIfNull(pubSubProvider);
        ArgumentNullException.ThrowIfNull(options);

        _pubSubProvider = pubSubProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(
        CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, object>
        {
            ["channel"] = _options.PubSubChannel,
            ["cache_key_prefix"] = _options.CacheKeyPrefix
        };

        // Verify pub/sub connectivity by checking if the provider is operational.
        // PublishAsync with an empty health-check message serves as a connectivity probe.
        await _pubSubProvider.PublishAsync(
            _options.PubSubChannel,
            "__health_check__",
            cancellationToken).ConfigureAwait(false);

        data["pubsub_status"] = "connected";

        return HealthCheckResult.Healthy(
            "Cache invalidation subscriber is healthy",
            data: data);
    }
}
