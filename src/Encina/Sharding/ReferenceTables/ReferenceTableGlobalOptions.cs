namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Global configuration options that apply to all reference table replication operations.
/// </summary>
/// <remarks>
/// <para>
/// These options control cross-cutting concerns like parallelism, default strategies,
/// and health monitoring thresholds. Per-table options in <see cref="ReferenceTableOptions"/>
/// override the defaults set here where applicable.
/// </para>
/// <code>
/// services.Configure&lt;ReferenceTableGlobalOptions&gt;(opts =>
/// {
///     opts.MaxParallelShards = 4;
///     opts.DefaultRefreshStrategy = RefreshStrategy.Polling;
///     opts.HealthCheckUnhealthyThreshold = TimeSpan.FromMinutes(10);
/// });
/// </code>
/// </remarks>
public sealed class ReferenceTableGlobalOptions
{
    /// <summary>
    /// Gets or sets the maximum number of shards to replicate to in parallel.
    /// </summary>
    /// <value>Defaults to <see cref="Environment.ProcessorCount"/>.</value>
    public int MaxParallelShards { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets the default refresh strategy for reference tables that do not specify one explicitly.
    /// </summary>
    /// <value>Defaults to <see cref="ReferenceTables.RefreshStrategy.Polling"/>.</value>
    public RefreshStrategy DefaultRefreshStrategy { get; set; } = RefreshStrategy.Polling;

    /// <summary>
    /// Gets or sets the replication lag threshold above which a reference table is
    /// considered unhealthy by health checks.
    /// </summary>
    /// <value>Defaults to 5 minutes.</value>
    public TimeSpan HealthCheckUnhealthyThreshold { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the replication lag threshold above which a reference table is
    /// considered degraded by health checks.
    /// </summary>
    /// <value>Defaults to 1 minute.</value>
    public TimeSpan HealthCheckDegradedThreshold { get; set; } = TimeSpan.FromMinutes(1);
}
