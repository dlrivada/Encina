using BenchmarkDotNet.Attributes;
using Encina.Cdc.Abstractions;
using NpgsqlTypes;

namespace Encina.Cdc.PostgreSql.Benchmarks.Benchmarks;

/// <summary>
/// Measures CPU-bound serialization / deserialization / comparison of the PostgreSQL CDC
/// position type (an 8-byte LSN). These operations run once per change event on the hot path
/// of every connector.
/// </summary>
[MemoryDiagnoser]
public class PositionBenchmarks
{
    private PostgresCdcPosition _position = null!;
    private PostgresCdcPosition _positionOther = null!;
    private byte[] _positionBytes = null!;

    /// <summary>
    /// Pre-builds fixtures so the benchmarks isolate the cost of each individual operation.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _position = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(0x0102030405060708UL));
        _positionOther = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(0x0102030405060709UL));
        _positionBytes = _position.ToBytes();
    }

    /// <summary>
    /// Baseline: allocate a new <see cref="PostgresCdcPosition"/>. Fired once per change event
    /// emitted by the logical replication stream.
    /// </summary>
    /// <returns>The new position instance.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:cdc-postgresql/position-ctor")]
    public PostgresCdcPosition CreatePosition()
    {
        return new PostgresCdcPosition(new NpgsqlLogSequenceNumber(0x0102030405060708UL));
    }

    /// <summary>
    /// Measures <see cref="PostgresCdcPosition.ToBytes"/> — a fixed-size 8-byte big-endian
    /// write, the serialization path executed every time the connector persists progress.
    /// </summary>
    /// <returns>The serialized 8-byte LSN.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-postgresql/position-tobytes")]
    public object ToBytes()
    {
        return _position.ToBytes();
    }

    /// <summary>
    /// Measures <see cref="PostgresCdcPosition.FromBytes"/> — a fixed-size 8-byte big-endian
    /// read, the deserialization path executed every time the connector resumes.
    /// </summary>
    /// <returns>The deserialized position.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-postgresql/position-frombytes")]
    public PostgresCdcPosition FromBytes()
    {
        return PostgresCdcPosition.FromBytes(_positionBytes);
    }

    /// <summary>
    /// Measures <see cref="PostgresCdcPosition.CompareTo"/> — used when the connector checks
    /// whether a snapshot has caught up to the live WAL stream.
    /// </summary>
    /// <returns>The comparison result.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-postgresql/position-compare")]
    public int ComparePositions()
    {
        return _position.CompareTo(_positionOther);
    }
}
