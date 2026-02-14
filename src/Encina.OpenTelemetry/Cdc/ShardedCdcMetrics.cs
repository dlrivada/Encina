using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.OpenTelemetry.Cdc;

/// <summary>
/// Exposes sharded CDC metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.cdc.sharded.events_total</c> (Counter) —
///   Total number of CDC events processed across all shards, tagged with
///   <c>shard.id</c> and <c>cdc.operation</c>.</description></item>
///   <item><description><c>encina.cdc.sharded.position_saves_total</c> (Counter) —
///   Total number of position checkpoint saves, tagged with <c>shard.id</c>.</description></item>
///   <item><description><c>encina.cdc.sharded.errors_total</c> (Counter) —
///   Total number of errors during sharded CDC processing, tagged with
///   <c>shard.id</c> and <c>error.type</c>.</description></item>
///   <item><description><c>encina.cdc.sharded.lag_ms</c> (ObservableGauge) —
///   Current replication lag per shard in milliseconds.</description></item>
///   <item><description><c>encina.cdc.sharded.active_connectors</c> (ObservableGauge) —
///   Number of currently active shard connectors.</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments use the same <c>"Encina"</c> meter for consistency with
/// <c>ColocationMetrics</c> and <c>ShardRoutingMetrics</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered automatically via AddEncinaOpenTelemetry()
/// services.AddEncinaOpenTelemetry();
///
/// // Or manually:
/// var metrics = new ShardedCdcMetrics(
///     activeConnectorsCallback: () => 3,
///     lagCallback: () => [("shard-1", 120.5), ("shard-2", 45.0)]);
/// metrics.RecordEventsProcessed("shard-1", "insert", 10);
/// metrics.RecordPositionSave("shard-1");
/// metrics.RecordError("shard-1", "Timeout");
/// </code>
/// </example>
public sealed class ShardedCdcMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _eventsProcessed;
    private readonly Counter<long> _positionSaves;
    private readonly Counter<long> _errors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedCdcMetrics"/> class,
    /// registering all sharded CDC metric instruments.
    /// </summary>
    /// <param name="activeConnectorsCallback">
    /// Callback that returns the current number of active shard connectors.
    /// </param>
    /// <param name="lagCallback">
    /// Callback that returns per-shard lag measurements as tuples of (shardId, lagMs).
    /// </param>
    public ShardedCdcMetrics(
        Func<int> activeConnectorsCallback,
        Func<IEnumerable<(string ShardId, double LagMs)>> lagCallback)
    {
        ArgumentNullException.ThrowIfNull(activeConnectorsCallback);
        ArgumentNullException.ThrowIfNull(lagCallback);

        _eventsProcessed = Meter.CreateCounter<long>(
            "encina.cdc.sharded.events_total",
            unit: "{events}",
            description: "Total number of CDC events processed across shards.");

        _positionSaves = Meter.CreateCounter<long>(
            "encina.cdc.sharded.position_saves_total",
            unit: "{saves}",
            description: "Total number of CDC position checkpoint saves.");

        _errors = Meter.CreateCounter<long>(
            "encina.cdc.sharded.errors_total",
            unit: "{errors}",
            description: "Total number of errors during sharded CDC processing.");

        Meter.CreateObservableGauge(
            "encina.cdc.sharded.active_connectors",
            activeConnectorsCallback,
            unit: "{connectors}",
            description: "Number of currently active shard connectors.");

        Meter.CreateObservableGauge(
            "encina.cdc.sharded.lag_ms",
            () => lagCallback().Select(lag =>
                new Measurement<double>(
                    lag.LagMs,
                    new KeyValuePair<string, object?>(ActivityTagNames.Cdc.ShardId, lag.ShardId))),
            unit: "ms",
            description: "Current replication lag per shard in milliseconds.");
    }

    /// <summary>
    /// Records that CDC events were processed from a specific shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="operation">The CDC operation type (e.g., "insert", "update", "delete").</param>
    /// <param name="count">The number of events processed.</param>
    public void RecordEventsProcessed(string shardId, string operation, long count = 1)
    {
        var tags = new TagList
        {
            { ActivityTagNames.Cdc.ShardId, shardId },
            { ActivityTagNames.Cdc.Operation, operation }
        };

        _eventsProcessed.Add(count, tags);
    }

    /// <summary>
    /// Records that a CDC position was saved (checkpointed) for a shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    public void RecordPositionSave(string shardId)
    {
        _positionSaves.Add(1,
            new KeyValuePair<string, object?>(ActivityTagNames.Cdc.ShardId, shardId));
    }

    /// <summary>
    /// Records an error during sharded CDC processing.
    /// </summary>
    /// <param name="shardId">The shard identifier where the error occurred.</param>
    /// <param name="errorType">The type or category of the error.</param>
    public void RecordError(string shardId, string errorType)
    {
        var tags = new TagList
        {
            { ActivityTagNames.Cdc.ShardId, shardId },
            { "error.type", errorType }
        };

        _errors.Add(1, tags);
    }
}
