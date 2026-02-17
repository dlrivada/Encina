using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.OpenTelemetry.Resharding;

/// <summary>
/// Exposes resharding metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.resharding.phase_duration_ms</c> (Histogram) —
///   Duration of each resharding phase in milliseconds, tagged with
///   <c>resharding.id</c> and <c>resharding.phase</c>.</description></item>
///   <item><description><c>encina.resharding.rows_copied_total</c> (Counter) —
///   Total number of rows copied during resharding, tagged with
///   <c>resharding.source_shard</c> and <c>resharding.target_shard</c>.</description></item>
///   <item><description><c>encina.resharding.rows_per_second</c> (ObservableGauge) —
///   Current rows-per-second throughput, updated via callback.</description></item>
///   <item><description><c>encina.resharding.cdc_lag_ms</c> (ObservableGauge) —
///   Current CDC replication lag in milliseconds, updated via callback.</description></item>
///   <item><description><c>encina.resharding.verification_mismatches_total</c> (Counter) —
///   Total number of verification mismatches detected.</description></item>
///   <item><description><c>encina.resharding.cutover_duration_ms</c> (Histogram) —
///   Duration of the cutover phase in milliseconds.</description></item>
///   <item><description><c>encina.resharding.active_resharding_count</c> (ObservableGauge) —
///   Number of currently active resharding operations, updated via callback.</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments use the same <c>"Encina"</c> meter for consistency with
/// other Encina metrics classes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered automatically via AddEncinaOpenTelemetry()
/// services.AddEncinaOpenTelemetry();
///
/// // Or manually:
/// var metrics = new ReshardingMetrics(callbacks);
/// metrics.RecordPhaseDuration(reshardingId, "Copying", 12345.6);
/// metrics.RecordRowsCopied("shard-1", "shard-3", 5000);
/// metrics.RecordVerificationMismatch(reshardingId);
/// metrics.RecordCutoverDuration(reshardingId, 250.0);
/// </code>
/// </example>
public sealed class ReshardingMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Histogram<double> _phaseDuration;
    private readonly Counter<long> _rowsCopied;
    private readonly Counter<long> _verificationMismatches;
    private readonly Histogram<double> _cutoverDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReshardingMetrics"/> class,
    /// registering all resharding metric instruments.
    /// </summary>
    /// <param name="callbacks">The callbacks for observable gauge metric values.</param>
    public ReshardingMetrics(ReshardingMetricsCallbacks callbacks)
    {
        ArgumentNullException.ThrowIfNull(callbacks);

        Meter.CreateObservableGauge<double>(
            "encina.resharding.rows_per_second",
            () =>
            {
                var value = callbacks.RowsPerSecondCallback();
                return [new Measurement<double>(value)];
            },
            unit: "{rows}/s",
            description: "Current rows-per-second throughput during the copy phase.");

        Meter.CreateObservableGauge<double>(
            "encina.resharding.cdc_lag_ms",
            () =>
            {
                var value = callbacks.CdcLagMsCallback();
                return [new Measurement<double>(value)];
            },
            unit: "ms",
            description: "Current CDC replication lag in milliseconds.");

        Meter.CreateObservableGauge<int>(
            "encina.resharding.active_resharding_count",
            () =>
            {
                var count = callbacks.ActiveReshardingCountCallback();
                return [new Measurement<int>(count)];
            },
            unit: "{operations}",
            description: "Number of currently active resharding operations.");

        _phaseDuration = Meter.CreateHistogram<double>(
            "encina.resharding.phase_duration_ms",
            unit: "ms",
            description: "Duration of each resharding phase in milliseconds.");

        _rowsCopied = Meter.CreateCounter<long>(
            "encina.resharding.rows_copied_total",
            description: "Total number of rows copied during resharding.");

        _verificationMismatches = Meter.CreateCounter<long>(
            "encina.resharding.verification_mismatches_total",
            description: "Total number of verification mismatches detected.");

        _cutoverDuration = Meter.CreateHistogram<double>(
            "encina.resharding.cutover_duration_ms",
            unit: "ms",
            description: "Duration of the cutover phase in milliseconds.");
    }

    /// <summary>
    /// Records the duration of a completed resharding phase.
    /// </summary>
    /// <param name="reshardingId">The resharding operation identifier.</param>
    /// <param name="phase">The phase name.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    public void RecordPhaseDuration(Guid reshardingId, string phase, double durationMs)
    {
        var tags = new TagList
        {
            { ActivityTagNames.Resharding.Id, reshardingId.ToString() },
            { ActivityTagNames.Resharding.Phase, phase }
        };

        _phaseDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records rows copied between shards.
    /// </summary>
    /// <param name="sourceShardId">The source shard identifier.</param>
    /// <param name="targetShardId">The target shard identifier.</param>
    /// <param name="rowCount">The number of rows copied.</param>
    public void RecordRowsCopied(string sourceShardId, string targetShardId, long rowCount)
    {
        var tags = new TagList
        {
            { ActivityTagNames.Resharding.SourceShard, sourceShardId },
            { ActivityTagNames.Resharding.TargetShard, targetShardId }
        };

        _rowsCopied.Add(rowCount, tags);
    }

    /// <summary>
    /// Records a verification mismatch during the verification phase.
    /// </summary>
    /// <param name="reshardingId">The resharding operation identifier.</param>
    public void RecordVerificationMismatch(Guid reshardingId)
    {
        _verificationMismatches.Add(1,
            new KeyValuePair<string, object?>(
                ActivityTagNames.Resharding.Id, reshardingId.ToString()));
    }

    /// <summary>
    /// Records the duration of a cutover operation.
    /// </summary>
    /// <param name="reshardingId">The resharding operation identifier.</param>
    /// <param name="durationMs">The cutover duration in milliseconds.</param>
    public void RecordCutoverDuration(Guid reshardingId, double durationMs)
    {
        _cutoverDuration.Record(durationMs,
            new KeyValuePair<string, object?>(
                ActivityTagNames.Resharding.Id, reshardingId.ToString()));
    }
}
