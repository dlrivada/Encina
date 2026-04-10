using BenchmarkDotNet.Attributes;
using MongoDB.Bson;

namespace Encina.Cdc.MongoDb.Benchmarks.Benchmarks;

/// <summary>
/// Measures CPU-bound serialization / deserialization / comparison of the MongoDB CDC
/// position type (a <see cref="BsonDocument"/> resume token). These operations run once per
/// change event on the hot path of every connector.
/// </summary>
[MemoryDiagnoser]
public class PositionBenchmarks
{
    private BsonDocument _resumeToken = null!;
    private MongoCdcPosition _position = null!;
    private MongoCdcPosition _positionOther = null!;
    private byte[] _positionBytes = null!;

    /// <summary>
    /// Pre-builds a realistic Change Stream resume token (same shape MongoDB emits:
    /// an <c>_data</c> field containing an opaque hex-encoded token).
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _resumeToken = new BsonDocument
        {
            { "_data", "826412345600000002010000000000000001" }
        };

        _position = new MongoCdcPosition(_resumeToken);
        _positionOther = new MongoCdcPosition(new BsonDocument
        {
            { "_data", "826412345600000003010000000000000001" }
        });

        _positionBytes = _position.ToBytes();
    }

    /// <summary>
    /// Baseline: allocate a new <see cref="MongoCdcPosition"/> wrapping an existing resume
    /// token document. Fired once per change event in the stream.
    /// </summary>
    /// <returns>The new position instance.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:cdc-mongodb/position-ctor")]
    public MongoCdcPosition CreatePosition()
    {
        return new MongoCdcPosition(_resumeToken);
    }

    /// <summary>
    /// Measures <see cref="MongoCdcPosition.ToBytes"/> — BSON-serializes the resume token.
    /// Executed every time the connector persists progress to the position store.
    /// </summary>
    /// <returns>The serialized BSON bytes.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-mongodb/position-tobytes")]
    public object ToBytes()
    {
        return _position.ToBytes();
    }

    /// <summary>
    /// Measures <see cref="MongoCdcPosition.FromBytes"/> — BSON-deserializes the resume
    /// token. Executed every time the connector resumes from a persisted position.
    /// </summary>
    /// <returns>The deserialized position.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-mongodb/position-frombytes")]
    public MongoCdcPosition FromBytes()
    {
        return MongoCdcPosition.FromBytes(_positionBytes);
    }

    /// <summary>
    /// Measures <see cref="MongoCdcPosition.CompareTo"/> — compares two resume token
    /// documents field by field.
    /// </summary>
    /// <returns>The comparison result.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc-mongodb/position-compare")]
    public int ComparePositions()
    {
        return _position.CompareTo(_positionOther);
    }
}
