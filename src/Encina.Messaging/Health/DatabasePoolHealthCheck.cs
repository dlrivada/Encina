using Encina.Database;

namespace Encina.Messaging.Health;

/// <summary>
/// Health check for database connection pool utilization.
/// </summary>
/// <remarks>
/// <para>
/// This health check monitors the registered <see cref="IDatabaseHealthMonitor"/> and reports
/// pool utilization thresholds. The result status is determined by:
/// <list type="bullet">
/// <item><description><b>Unhealthy</b>: Circuit breaker is open, or pool utilization exceeds 95%</description></item>
/// <item><description><b>Degraded</b>: Pool utilization exceeds 80%</description></item>
/// <item><description><b>Healthy</b>: Pool utilization is below 80%</description></item>
/// </list>
/// </para>
/// <para>
/// The thresholds can be configured via <see cref="DatabasePoolHealthCheckOptions"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via ASP.NET Core health checks
/// builder.Services
///     .AddHealthChecks()
///     .AddEncinaDatabasePool();
///
/// // Or with custom thresholds
/// builder.Services
///     .AddHealthChecks()
///     .AddEncinaDatabasePool(options: new DatabasePoolHealthCheckOptions
///     {
///         DegradedThreshold = 0.7,
///         UnhealthyThreshold = 0.9
///     });
/// </code>
/// </example>
public sealed class DatabasePoolHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the database pool health check.
    /// </summary>
    public const string DefaultName = "encina-database-pool";

    private readonly IDatabaseHealthMonitor _monitor;
    private readonly DatabasePoolHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabasePoolHealthCheck"/> class.
    /// </summary>
    /// <param name="monitor">The database health monitor to check.</param>
    /// <param name="options">Health check options. If null, default thresholds are used.</param>
    public DatabasePoolHealthCheck(
        IDatabaseHealthMonitor monitor,
        DatabasePoolHealthCheckOptions? options = null)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "database", "pool", "ready"])
    {
        ArgumentNullException.ThrowIfNull(monitor);

        _monitor = monitor;
        _options = options ?? new DatabasePoolHealthCheckOptions();
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        // Check circuit breaker state first
        if (_monitor.IsCircuitOpen)
        {
            return HealthCheckResult.Unhealthy(
                $"Circuit breaker is open for provider '{_monitor.ProviderName}'.",
                data: CreateData(ConnectionPoolStats.CreateEmpty()));
        }

        // Perform active health check
        var healthResult = await _monitor.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

        if (healthResult.Status == DatabaseHealthStatus.Unhealthy)
        {
            return HealthCheckResult.Unhealthy(
                healthResult.Description,
                healthResult.Exception,
                healthResult.Data);
        }

        // Check pool utilization thresholds
        var stats = _monitor.GetPoolStatistics();
        var data = CreateData(stats);

        if (stats.PoolUtilization >= _options.UnhealthyThreshold)
        {
            return HealthCheckResult.Unhealthy(
                $"Pool utilization ({stats.PoolUtilization:P0}) exceeds unhealthy threshold ({_options.UnhealthyThreshold:P0}) for provider '{_monitor.ProviderName}'.",
                data: data);
        }

        if (stats.PoolUtilization >= _options.DegradedThreshold)
        {
            return HealthCheckResult.Degraded(
                $"Pool utilization ({stats.PoolUtilization:P0}) exceeds degraded threshold ({_options.DegradedThreshold:P0}) for provider '{_monitor.ProviderName}'.",
                data: data);
        }

        return HealthCheckResult.Healthy(
            $"Database pool is healthy for provider '{_monitor.ProviderName}'.",
            data);
    }

    private Dictionary<string, object> CreateData(ConnectionPoolStats stats)
    {
        return new Dictionary<string, object>
        {
            ["provider"] = _monitor.ProviderName,
            ["activeConnections"] = stats.ActiveConnections,
            ["idleConnections"] = stats.IdleConnections,
            ["totalConnections"] = stats.TotalConnections,
            ["pendingRequests"] = stats.PendingRequests,
            ["maxPoolSize"] = stats.MaxPoolSize,
            ["poolUtilization"] = stats.PoolUtilization,
            ["circuitBreakerOpen"] = _monitor.IsCircuitOpen,
            ["degradedThreshold"] = _options.DegradedThreshold,
            ["unhealthyThreshold"] = _options.UnhealthyThreshold
        };
    }
}

/// <summary>
/// Configuration options for the database pool health check.
/// </summary>
public sealed class DatabasePoolHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the pool utilization threshold that triggers a degraded status.
    /// </summary>
    /// <remarks>
    /// When pool utilization (ratio of total connections to max pool size) exceeds this value,
    /// the health check reports <see cref="HealthStatus.Degraded"/>.
    /// </remarks>
    /// <value>Default: 0.8 (80%).</value>
    public double DegradedThreshold { get; set; } = 0.8;

    /// <summary>
    /// Gets or sets the pool utilization threshold that triggers an unhealthy status.
    /// </summary>
    /// <remarks>
    /// When pool utilization exceeds this value, the health check reports
    /// <see cref="HealthStatus.Unhealthy"/>.
    /// </remarks>
    /// <value>Default: 0.95 (95%).</value>
    public double UnhealthyThreshold { get; set; } = 0.95;

    /// <summary>
    /// Gets or sets the name of the health check.
    /// </summary>
    /// <remarks>
    /// If not specified, defaults to <see cref="DatabasePoolHealthCheck.DefaultName"/>.
    /// </remarks>
    /// <value>Default: null (uses <see cref="DatabasePoolHealthCheck.DefaultName"/>).</value>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the tags to apply to the health check.
    /// </summary>
    /// <value>Default: null (uses default tags: "encina", "database", "pool", "ready").</value>
    public IReadOnlyCollection<string>? Tags { get; set; }
}
