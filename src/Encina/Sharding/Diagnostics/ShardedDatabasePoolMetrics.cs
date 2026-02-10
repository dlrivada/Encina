using System.Diagnostics.Metrics;
using Encina.Database;
using Encina.Sharding.Health;

namespace Encina.Sharding.Diagnostics;

/// <summary>
/// Exposes per-shard database connection pool and health metrics via the <c>Encina</c> meter
/// using <see cref="ObservableGauge{T}"/> instruments.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.sharding.shard.health</c> — Shard health status (0=unhealthy, 1=degraded, 2=healthy)</description></item>
///   <item><description><c>encina.sharding.shard.pool.utilization</c> — Pool utilization ratio (0.0 to 1.0)</description></item>
///   <item><description><c>encina.sharding.shard.pool.connections.active</c> — Active connections per shard</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments include <c>db.shard.id</c> and <c>db.provider</c> tags for per-shard filtering.
/// </para>
/// <para>
/// Pool metrics are observed synchronously via <see cref="IShardedDatabaseHealthMonitor.GetAllShardPoolStatistics()"/>.
/// Health metrics use a snapshot that must be updated via <see cref="UpdateHealthSnapshot"/>
/// (called by the health check infrastructure or a periodic background service).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered automatically via AddEncinaShardingHealthMetrics()
/// services.AddEncinaShardingHealthMetrics();
///
/// // Or manually:
/// services.AddSingleton&lt;ShardedDatabasePoolMetrics&gt;();
/// </code>
/// </example>
public sealed class ShardedDatabasePoolMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly IShardedDatabaseHealthMonitor _monitor;
    private volatile IReadOnlyList<ShardHealthResult> _healthSnapshot = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedDatabasePoolMetrics"/> class,
    /// registering observable gauge instruments for the given sharded health monitor.
    /// </summary>
    /// <param name="monitor">The sharded database health monitor to observe.</param>
    public ShardedDatabasePoolMetrics(IShardedDatabaseHealthMonitor monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        _monitor = monitor;

        Meter.CreateObservableGauge(
            "encina.sharding.shard.health",
            ObserveHealthStatus,
            description: "Shard health status (0 = unhealthy, 1 = degraded, 2 = healthy).");

        Meter.CreateObservableGauge(
            "encina.sharding.shard.pool.utilization",
            ObservePoolUtilization,
            unit: "1",
            description: "Shard connection pool utilization ratio (0.0 to 1.0).");

        Meter.CreateObservableGauge(
            "encina.sharding.shard.pool.connections.active",
            ObserveActiveConnections,
            unit: "{connections}",
            description: "Number of active database connections per shard.");
    }

    /// <summary>
    /// Updates the health status snapshot used by the observable health gauge.
    /// </summary>
    /// <param name="healthResults">The latest health check results for all shards.</param>
    /// <remarks>
    /// This method is thread-safe and should be called periodically by the health check
    /// infrastructure or a background service.
    /// </remarks>
    public void UpdateHealthSnapshot(IReadOnlyList<ShardHealthResult> healthResults)
    {
        ArgumentNullException.ThrowIfNull(healthResults);
        _healthSnapshot = healthResults;
    }

    private IEnumerable<Measurement<int>> ObserveHealthStatus()
    {
        var snapshot = _healthSnapshot;

        foreach (var result in snapshot)
        {
            var healthValue = result.Status switch
            {
                DatabaseHealthStatus.Healthy => 2,
                DatabaseHealthStatus.Degraded => 1,
                _ => 0
            };

            yield return new Measurement<int>(
                healthValue,
                new KeyValuePair<string, object?>(ActivityTagNames.ShardId, result.ShardId));
        }
    }

    private IEnumerable<Measurement<double>> ObservePoolUtilization()
    {
        var allStats = _monitor.GetAllShardPoolStatistics();

        foreach (var (shardId, stats) in allStats)
        {
            yield return new Measurement<double>(
                stats.PoolUtilization,
                new KeyValuePair<string, object?>(ActivityTagNames.ShardId, shardId));
        }
    }

    private IEnumerable<Measurement<int>> ObserveActiveConnections()
    {
        var allStats = _monitor.GetAllShardPoolStatistics();

        foreach (var (shardId, stats) in allStats)
        {
            yield return new Measurement<int>(
                stats.ActiveConnections,
                new KeyValuePair<string, object?>(ActivityTagNames.ShardId, shardId));
        }
    }
}
