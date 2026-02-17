namespace Encina.Sharding.Diagnostics;

/// <summary>
/// Configuration options for sharding OpenTelemetry integration.
/// </summary>
/// <remarks>
/// <para>
/// All metrics and tracing features are enabled by default but can be selectively disabled
/// for environments where only specific observability signals are needed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaShardingMetrics(options =>
/// {
///     options.EnableRoutingMetrics = true;
///     options.EnableScatterGatherMetrics = true;
///     options.EnableHealthMetrics = false; // Disable health metrics
///     options.EnableTracing = true;
///     options.HealthCheckInterval = TimeSpan.FromSeconds(60);
/// });
/// </code>
/// </example>
public sealed class ShardingMetricsOptions
{
    /// <summary>
    /// Gets or sets the interval for periodic health check metric updates.
    /// </summary>
    /// <value>Defaults to 30 seconds.</value>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether shard routing metrics are enabled.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    /// <remarks>
    /// When enabled, routing decisions are tracked with latency histograms
    /// and decision counters tagged by router type and resolved shard.
    /// </remarks>
    public bool EnableRoutingMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether scatter-gather metrics are enabled.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    /// <remarks>
    /// When enabled, scatter-gather operations are tracked with duration histograms,
    /// per-shard query times, partial failure counters, and active query gauges.
    /// </remarks>
    public bool EnableScatterGatherMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether per-shard health metrics are enabled.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    /// <remarks>
    /// When enabled, per-shard health status, pool utilization, and active connections
    /// are exposed as observable gauges.
    /// </remarks>
    public bool EnableHealthMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether aggregation metrics are enabled.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    /// <remarks>
    /// When enabled, distributed aggregation operations (Count, Sum, Avg, Min, Max)
    /// are tracked with duration histograms, shard fan-out counts, and partial result counters.
    /// </remarks>
    public bool EnableAggregationMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether specification scatter-gather metrics are enabled.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    /// <remarks>
    /// When enabled, specification-based scatter-gather operations are tracked with
    /// query counters, merge duration histograms, items-per-shard distributions,
    /// and shard fan-out counts.
    /// </remarks>
    public bool EnableSpecificationMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether read/write separation metrics are enabled.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    /// <remarks>
    /// When enabled, replica selection decisions, connection latencies, fallback events,
    /// replication lag, and unhealthy replica counts are tracked.
    /// </remarks>
    public bool EnableReadWriteMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether distributed tracing is enabled for sharding operations.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    /// <remarks>
    /// When enabled, activities are created for routing decisions, scatter-gather operations,
    /// and individual shard queries, providing end-to-end distributed tracing.
    /// </remarks>
    public bool EnableTracing { get; set; } = true;
}
