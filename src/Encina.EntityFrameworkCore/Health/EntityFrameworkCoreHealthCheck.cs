using Encina.Messaging.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.EntityFrameworkCore.Health;

/// <summary>
/// Health check for Entity Framework Core database connectivity.
/// </summary>
public sealed class EntityFrameworkCoreHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the EF Core health check.
    /// </summary>
    public const string DefaultName = "encina-efcore";

    /// <summary>
    /// Tags that are always included in the health check and merged with any user-provided tags.
    /// </summary>
    private static readonly string[] DefaultTags = ["encina", "database", "efcore", "ready"];

    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkCoreHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve DbContext from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public EntityFrameworkCoreHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, MergeTags(options?.Tags, DefaultTags))
    {
        _serviceProvider = serviceProvider;
        _options = options ?? new ProviderHealthCheckOptions();
    }

    /// <inheritdoc/>
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(_options.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();

            var canConnect = await dbContext.Database.CanConnectAsync(linkedCts.Token);

            if (canConnect)
            {
                return HealthCheckResult.Healthy($"{Name} database is reachable");
            }

            return HealthCheckResult.Unhealthy($"{Name} database connection failed");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"{Name} health check timed out after {_options.Timeout.TotalSeconds}s");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"{Name} health check failed: {ex.Message}");
        }
    }
}
