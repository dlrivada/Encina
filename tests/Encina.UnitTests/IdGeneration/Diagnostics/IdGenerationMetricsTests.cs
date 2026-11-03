using System.Diagnostics.Metrics;
using Encina.IdGeneration.Diagnostics;

namespace Encina.UnitTests.IdGeneration.Diagnostics;

/// <summary>
/// Unit tests for <see cref="IdGenerationMetrics"/>. A <see cref="MeterListener"/> captures the
/// measurements; every test uses its own strategy tag so concurrent tests cannot see each other's data.
/// </summary>
public sealed class IdGenerationMetricsTests : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly List<(string Instrument, double Value, Dictionary<string, object?> Tags)> _measurements = [];
    private readonly Lock _gate = new();

    public IdGenerationMetricsTests()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name.StartsWith("encina.id_generation.", StringComparison.Ordinal))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Capture(instrument, value, tags));
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Capture(instrument, value, tags));
        _listener.Start();
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public void RecordGenerated_WithShard_RecordsOneWithStrategyAndShardTags()
    {
        var metrics = new IdGenerationMetrics();

        metrics.RecordGenerated("metrics-test-generated-shard", "42");

        var m = Single("encina.id_generation.generated_total", "metrics-test-generated-shard");
        m.Value.ShouldBe(1);
        m.Tags["shard_id"].ShouldBe("42");
    }

    [Fact]
    public void RecordGenerated_WithoutShard_OmitsShardTag()
    {
        var metrics = new IdGenerationMetrics();

        metrics.RecordGenerated("metrics-test-generated-noshard");

        var m = Single("encina.id_generation.generated_total", "metrics-test-generated-noshard");
        m.Tags.ContainsKey("shard_id").ShouldBeFalse();
    }

    [Fact]
    public void RecordCollision_RecordsOneWithStrategyTag()
    {
        var metrics = new IdGenerationMetrics();

        metrics.RecordCollision("metrics-test-collision");

        Single("encina.id_generation.collisions_total", "metrics-test-collision").Value.ShouldBe(1);
    }

    [Fact]
    public void RecordDuration_RecordsTheGivenMilliseconds()
    {
        var metrics = new IdGenerationMetrics();

        metrics.RecordDuration("metrics-test-duration", 0.25);

        Single("encina.id_generation.generation_duration_ms", "metrics-test-duration").Value.ShouldBe(0.25);
    }

    [Fact]
    public void RecordSequenceExhausted_RecordsAnEvent()
    {
        var metrics = new IdGenerationMetrics();

        metrics.RecordSequenceExhausted();

        lock (_gate)
        {
            _measurements.ShouldContain(m => m.Instrument == "encina.id_generation.sequence_exhausted_total");
        }
    }

    private void Capture(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var map = new Dictionary<string, object?>();
        foreach (var tag in tags)
        {
            map[tag.Key] = tag.Value;
        }

        lock (_gate)
        {
            _measurements.Add((instrument.Name, value, map));
        }
    }

    private (string Instrument, double Value, Dictionary<string, object?> Tags) Single(string instrument, string strategy)
    {
        lock (_gate)
        {
            return _measurements.Single(m =>
                m.Instrument == instrument && Equals(m.Tags.GetValueOrDefault("strategy"), strategy));
        }
    }
}
