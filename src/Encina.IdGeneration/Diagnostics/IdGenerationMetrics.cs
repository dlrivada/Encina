using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.IdGeneration.Diagnostics;

/// <summary>
/// Exposes ID generation metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.id_generation.generated_total</c> (Counter) —
///   Total number of IDs generated, tagged with <c>strategy</c> and optional <c>shard_id</c>.</description></item>
///   <item><description><c>encina.id_generation.collisions_total</c> (Counter) —
///   Total number of ID collisions detected (e.g., Snowflake sequence wraparound within same millisecond).</description></item>
///   <item><description><c>encina.id_generation.generation_duration_ms</c> (Histogram) —
///   Duration of ID generation operations in milliseconds.</description></item>
///   <item><description><c>encina.id_generation.sequence_exhausted_total</c> (Counter) —
///   Total number of Snowflake sequence exhaustion events (all sequences used within a single millisecond).</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments use the same <c>"Encina"</c> meter for consistency with other Encina metrics.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var metrics = new IdGenerationMetrics();
/// metrics.RecordGenerated("Snowflake", "42");
/// metrics.RecordCollision("Snowflake");
/// metrics.RecordDuration("Snowflake", 0.15);
/// metrics.RecordSequenceExhausted();
/// </code>
/// </example>
public sealed class IdGenerationMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _generatedTotal;
    private readonly Counter<long> _collisionsTotal;
    private readonly Histogram<double> _generationDuration;
    private readonly Counter<long> _sequenceExhaustedTotal;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdGenerationMetrics"/> class,
    /// registering all ID generation metric instruments.
    /// </summary>
    public IdGenerationMetrics()
    {
        _generatedTotal = Meter.CreateCounter<long>(
            "encina.id_generation.generated_total",
            unit: "{ids}",
            description: "Total number of IDs generated.");

        _collisionsTotal = Meter.CreateCounter<long>(
            "encina.id_generation.collisions_total",
            unit: "{collisions}",
            description: "Total number of ID collisions detected.");

        _generationDuration = Meter.CreateHistogram<double>(
            "encina.id_generation.generation_duration_ms",
            unit: "ms",
            description: "Duration of ID generation operations in milliseconds.");

        _sequenceExhaustedTotal = Meter.CreateCounter<long>(
            "encina.id_generation.sequence_exhausted_total",
            unit: "{events}",
            description: "Total number of Snowflake sequence exhaustion events.");
    }

    /// <summary>
    /// Records a successful ID generation.
    /// </summary>
    /// <param name="strategy">The ID generation strategy name (e.g., "Snowflake", "Ulid", "UuidV7", "ShardPrefixed").</param>
    /// <param name="shardId">The optional shard ID used for generation.</param>
    public void RecordGenerated(string strategy, string? shardId = null)
    {
        var tags = shardId is not null
            ? new TagList
            {
                { "strategy", strategy },
                { "shard_id", shardId }
            }
            : new TagList
            {
                { "strategy", strategy }
            };

        _generatedTotal.Add(1, tags);
    }

    /// <summary>
    /// Records an ID collision event.
    /// </summary>
    /// <param name="strategy">The ID generation strategy where the collision occurred.</param>
    public void RecordCollision(string strategy)
    {
        _collisionsTotal.Add(1,
            new KeyValuePair<string, object?>("strategy", strategy));
    }

    /// <summary>
    /// Records the duration of an ID generation operation.
    /// </summary>
    /// <param name="strategy">The ID generation strategy name.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    public void RecordDuration(string strategy, double durationMs)
    {
        _generationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("strategy", strategy));
    }

    /// <summary>
    /// Records a Snowflake sequence exhaustion event (all sequences consumed within a single millisecond).
    /// </summary>
    public void RecordSequenceExhausted()
    {
        _sequenceExhaustedTotal.Add(1);
    }
}
