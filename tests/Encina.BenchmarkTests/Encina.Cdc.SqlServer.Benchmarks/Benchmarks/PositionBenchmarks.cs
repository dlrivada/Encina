using BenchmarkDotNet.Attributes;

namespace Encina.Cdc.SqlServer.Benchmarks.Benchmarks;

/// <summary>
/// Measures CPU-bound serialization / deserialization / comparison of the SQL Server CDC
/// position type (an 8-byte Change Tracking version). These operations run once per change
/// event on the hot path of every connector.
/// </summary>
[MemoryDiagnoser]
public class PositionBenchmarks
{
    private SqlServerCdcPosition _position = null!;
    private SqlServerCdcPosition _positionOther = null!;
    private byte[] _positionBytes = null!;

    /// <summary>
    /// Pre-builds fixtures so the benchmarks isolate the cost of each individual operation.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _position = new SqlServerCdcPosition(1_234_567L);
        _positionOther = new SqlServerCdcPosition(1_234_568L);
        _positionBytes = _position.ToBytes();
    }

    /// <summary>
    /// Baseline: allocate a new <see cref="SqlServerCdcPosition"/>. Fired once per change
    /// event emitted by the Change Tracking stream.
    /// </summary>
    /// <returns>The new position instance.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:cdc-sqlserver/position-ctor")]
    public SqlServerCdcPosition CreatePosition()
    {
        return new SqlServerCdcPosition(1_234_567L);
    }

    /// <summary>
    /// Measures <see cref="SqlServerCdcPosition.ToBytes"/> — a fixed-size 8-byte big-endian
    /// write, the serialization path executed every time the connector persists progress.
    /// </summary>
    /// <returns>The serialized 8-byte Change Tracking version.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-sqlserver/position-tobytes")]
    public object ToBytes()
    {
        return _position.ToBytes();
    }

    /// <summary>
    /// Measures <see cref="SqlServerCdcPosition.FromBytes"/> — a fixed-size 8-byte big-endian
    /// read, the deserialization path executed every time the connector resumes.
    /// </summary>
    /// <returns>The deserialized position.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-sqlserver/position-frombytes")]
    public SqlServerCdcPosition FromBytes()
    {
        return SqlServerCdcPosition.FromBytes(_positionBytes);
    }

    /// <summary>
    /// Measures <see cref="SqlServerCdcPosition.CompareTo"/> — used when the connector
    /// checks whether a snapshot has caught up to the live Change Tracking stream.
    /// </summary>
    /// <returns>The comparison result.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-sqlserver/position-compare")]
    public int ComparePositions()
    {
        return _position.CompareTo(_positionOther);
    }
}
