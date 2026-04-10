using BenchmarkDotNet.Attributes;
using Encina.Cdc.Abstractions;

namespace Encina.Cdc.MySql.Benchmarks.Benchmarks;

/// <summary>
/// Measures CPU-bound serialization / deserialization / comparison of the MySQL CDC position
/// type. These are paid once per change event on the hot path of every connector, so even
/// small regressions (e.g. JSON serializer changes) would show up here immediately.
/// </summary>
[MemoryDiagnoser]
public class PositionBenchmarks
{
    private const string GtidSet = "3E11FA47-71CA-11E1-9E33-C80AA9429562:1-5";
    private MySqlCdcPosition _gtidPosition = null!;
    private MySqlCdcPosition _filePosition = null!;
    private MySqlCdcPosition _filePositionOther = null!;
    private byte[] _gtidBytes = null!;

    /// <summary>
    /// Pre-builds fixtures so the benchmarks isolate the cost of each individual operation.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _gtidPosition = new MySqlCdcPosition(GtidSet);
        _filePosition = new MySqlCdcPosition("mysql-bin.000001", 4096);
        _filePositionOther = new MySqlCdcPosition("mysql-bin.000001", 8192);
        _gtidBytes = _gtidPosition.ToBytes();
    }

    /// <summary>
    /// Baseline: allocate a new GTID-based <see cref="MySqlCdcPosition"/>. Fired once per
    /// committed transaction in GTID replication mode.
    /// </summary>
    /// <returns>The new position instance.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:cdc-mysql/position-ctor")]
    public MySqlCdcPosition CreateGtidPosition()
    {
        return new MySqlCdcPosition(GtidSet);
    }

    /// <summary>
    /// Measures <see cref="MySqlCdcPosition.ToBytes"/> — the serialization path executed
    /// every time the connector persists progress to the position store.
    /// </summary>
    /// <returns>The serialized position bytes.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-mysql/position-tobytes")]
    public object ToBytes()
    {
        return _gtidPosition.ToBytes();
    }

    /// <summary>
    /// Measures <see cref="MySqlCdcPosition.FromBytes"/> — the deserialization path executed
    /// every time the connector resumes from a persisted position.
    /// </summary>
    /// <returns>The deserialized position.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-mysql/position-frombytes")]
    public MySqlCdcPosition FromBytes()
    {
        return MySqlCdcPosition.FromBytes(_gtidBytes);
    }

    /// <summary>
    /// Measures <see cref="MySqlCdcPosition.CompareTo"/> on the file/position path — used when
    /// the connector needs to know whether a snapshot has caught up to the live stream.
    /// </summary>
    /// <returns>The comparison result.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-mysql/position-compare")]
    public int CompareFilePositions()
    {
        return _filePosition.CompareTo(_filePositionOther);
    }
}
