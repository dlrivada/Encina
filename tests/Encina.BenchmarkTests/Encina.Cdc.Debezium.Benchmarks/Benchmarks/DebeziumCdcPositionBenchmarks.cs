using BenchmarkDotNet.Attributes;

namespace Encina.Cdc.Debezium.Benchmarks.Benchmarks;

/// <summary>
/// Measures CPU-bound serialization / deserialization / comparison of the HTTP-backed
/// Debezium CDC position. This is the opaque offset JSON emitted by Debezium Server that
/// the Encina HTTP listener persists per change event.
/// </summary>
/// <remarks>
/// These benchmarks are CPU-bound on purpose: a full Debezium end-to-end benchmark would
/// require a Kafka Connect cluster plus a source DB container plus the Debezium connectors,
/// which is prohibitively heavy for this target. Position serialization is the only
/// Debezium-specific hot path exposed by the public API of <c>Encina.Cdc.Debezium</c>.
/// </remarks>
[MemoryDiagnoser]
public class DebeziumCdcPositionBenchmarks
{
    private const string OffsetJson =
        "{\"source\":\"mysql\",\"file\":\"mysql-bin.000001\",\"pos\":1024,\"gtids\":\"3E11FA47-71CA-11E1-9E33-C80AA9429562:1-5\",\"ts_ms\":1712750400000}";

    private DebeziumCdcPosition _position = null!;
    private DebeziumCdcPosition _positionOther = null!;
    private byte[] _positionBytes = null!;

    /// <summary>
    /// Pre-builds fixtures so the benchmarks isolate the cost of each individual operation.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _position = new DebeziumCdcPosition(OffsetJson);
        _positionOther = new DebeziumCdcPosition(OffsetJson.Replace("1024", "2048", System.StringComparison.Ordinal));
        _positionBytes = _position.ToBytes();
    }

    /// <summary>
    /// Baseline: allocate a new <see cref="DebeziumCdcPosition"/> wrapping the offset JSON.
    /// Fired once per change event forwarded by the Debezium HTTP listener.
    /// </summary>
    /// <returns>The new position instance.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:cdc-debezium/position-ctor")]
    public DebeziumCdcPosition CreatePosition()
    {
        return new DebeziumCdcPosition(OffsetJson);
    }

    /// <summary>
    /// Measures <see cref="DebeziumCdcPosition.ToBytes"/> — UTF-8 encodes the offset JSON.
    /// </summary>
    /// <returns>The serialized UTF-8 bytes.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-debezium/position-tobytes")]
    public object ToBytes()
    {
        return _position.ToBytes();
    }

    /// <summary>
    /// Measures <see cref="DebeziumCdcPosition.FromBytes"/> — UTF-8 decodes the offset JSON.
    /// </summary>
    /// <returns>The deserialized position.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-debezium/position-frombytes")]
    public DebeziumCdcPosition FromBytes()
    {
        return DebeziumCdcPosition.FromBytes(_positionBytes);
    }
}
