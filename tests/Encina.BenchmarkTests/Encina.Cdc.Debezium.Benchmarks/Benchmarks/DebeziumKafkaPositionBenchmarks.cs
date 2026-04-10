using BenchmarkDotNet.Attributes;
using Encina.Cdc.Debezium.Kafka;

namespace Encina.Cdc.Debezium.Benchmarks.Benchmarks;

/// <summary>
/// Measures CPU-bound serialization / deserialization / comparison of the Kafka-backed
/// Debezium CDC position. This richer position type stores the Kafka <c>(topic, partition,
/// offset)</c> tuple alongside the Debezium source offset so the connector can resume
/// reliably after consumer restarts.
/// </summary>
[MemoryDiagnoser]
public class DebeziumKafkaPositionBenchmarks
{
    private const string OffsetJson =
        "{\"source\":\"mysql\",\"file\":\"mysql-bin.000001\",\"pos\":1024}";
    private const string Topic = "dbserver1.inventory.customers";

    private DebeziumKafkaPosition _position = null!;
    private DebeziumKafkaPosition _positionOther = null!;
    private byte[] _positionBytes = null!;

    /// <summary>
    /// Pre-builds fixtures so the benchmarks isolate the cost of each individual operation.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _position = new DebeziumKafkaPosition(OffsetJson, Topic, partition: 0, offset: 1024);
        _positionOther = new DebeziumKafkaPosition(OffsetJson, Topic, partition: 0, offset: 2048);
        _positionBytes = _position.ToBytes();
    }

    /// <summary>
    /// Measures <see cref="DebeziumKafkaPosition.ToBytes"/> — JSON-serializes the combined
    /// Kafka offset + Debezium source offset tuple. Fired once per committed event.
    /// </summary>
    /// <returns>The serialized UTF-8 bytes.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-debezium/kafka-position-tobytes")]
    public object ToBytes()
    {
        return _position.ToBytes();
    }

    /// <summary>
    /// Measures <see cref="DebeziumKafkaPosition.FromBytes"/> — JSON-deserializes the
    /// combined tuple. Fired every time the Kafka connector resumes.
    /// </summary>
    /// <returns>The deserialized position.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-debezium/kafka-position-frombytes")]
    public DebeziumKafkaPosition FromBytes()
    {
        return DebeziumKafkaPosition.FromBytes(_positionBytes);
    }

    /// <summary>
    /// Measures <see cref="DebeziumKafkaPosition.CompareTo"/> on the same-topic /
    /// same-partition path (the common hot case in a single-partition consumer).
    /// </summary>
    /// <returns>The comparison result.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-debezium/kafka-position-compare")]
    public int ComparePositions()
    {
        return _position.CompareTo(_positionOther);
    }
}
