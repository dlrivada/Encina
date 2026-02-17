using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Caching.Health;

/// <summary>
/// Health check that verifies query cache provider accessibility.
/// </summary>
/// <remarks>
/// <para>
/// Performs a lightweight existence check against the cache provider to verify
/// connectivity. Returns <see cref="HealthCheckResult"/> with <see cref="HealthStatus.Healthy"/>
/// when the cache responds, <see cref="HealthStatus.Degraded"/> when no cache provider is
/// registered, and <see cref="HealthStatus.Unhealthy"/> on errors.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via ASP.NET Core health checks:
/// builder.Services
///     .AddHealthChecks()
///     .AddEncinaQueryCache();
/// </code>
/// </example>
public sealed class QueryCacheHealthCheck : EncinaHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The default name for this health check.
    /// </summary>
    public const string DefaultName = "encina-query-cache";

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryCacheHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve <see cref="ICacheProvider"/> from.</param>
    public QueryCacheHealthCheck(IServiceProvider serviceProvider)
        : base(DefaultName, ["cache", "query-cache", "ready"])
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var cacheProvider = _serviceProvider.GetService<ICacheProvider>();

        if (cacheProvider is null)
        {
            return HealthCheckResult.Degraded(
                "No cache provider is registered.",
                data: new Dictionary<string, object>
                {
                    ["provider_registered"] = false
                });
        }

        // Lightweight check: verify cache accepts operations.
        await cacheProvider.ExistsAsync("__health_check__", cancellationToken);

        return HealthCheckResult.Healthy(
            "Query cache provider is accessible.",
            data: new Dictionary<string, object>
            {
                ["provider_type"] = cacheProvider.GetType().Name
            });
    }
}
