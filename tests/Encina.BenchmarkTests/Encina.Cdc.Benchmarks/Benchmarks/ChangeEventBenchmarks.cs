using BenchmarkDotNet.Attributes;
using Encina.Cdc.Abstractions;

namespace Encina.Cdc.Benchmarks.Benchmarks;

/// <summary>
/// Measures the per-operation cost of materializing and comparing the core CDC event records
/// (<see cref="ChangeEvent"/> + <see cref="ChangeMetadata"/>). These records sit on the hot path
/// of every CDC stream — one instance is created per captured row and multiplied by the
/// throughput of the connector.
/// </summary>
[MemoryDiagnoser]
public class ChangeEventBenchmarks
{
    private ChangeEvent _eventA = null!;
    private ChangeEvent _eventB = null!;
    private ChangeMetadata _metadata = null!;
    private CdcPosition _position = null!;

    /// <summary>
    /// Pre-builds the positional and metadata fixtures once to isolate allocation hot spots
    /// from the per-iteration measurements.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _position = new StubCdcPosition(42);
        _metadata = new ChangeMetadata(
            Position: _position,
            CapturedAtUtc: new DateTime(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc),
            TransactionId: "tx-abc-123",
            SourceDatabase: "benchmarks",
            SourceSchema: "dbo");

        _eventA = new ChangeEvent(
            TableName: "Orders",
            Operation: ChangeOperation.Update,
            Before: new { Id = 1, Total = 50m },
            After: new { Id = 1, Total = 75m },
            Metadata: _metadata);

        _eventB = _eventA with { Operation = ChangeOperation.Insert };
    }

    /// <summary>
    /// Baseline: allocate a fresh <see cref="ChangeMetadata"/> record (fires once per change
    /// event in every connector).
    /// </summary>
    /// <returns>The materialized metadata.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:cdc/metadata-ctor")]
    public ChangeMetadata CreateChangeMetadata()
    {
        return new ChangeMetadata(
            Position: _position,
            CapturedAtUtc: DateTime.UtcNow,
            TransactionId: "tx-abc-123",
            SourceDatabase: "benchmarks",
            SourceSchema: "dbo");
    }

    /// <summary>
    /// Measures the full <see cref="ChangeEvent"/> allocation path including its embedded
    /// <see cref="ChangeMetadata"/>.
    /// </summary>
    /// <returns>The materialized change event.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc/event-ctor")]
    public ChangeEvent CreateChangeEvent()
    {
        return new ChangeEvent(
            TableName: "Orders",
            Operation: ChangeOperation.Update,
            Before: new { Id = 1, Total = 50m },
            After: new { Id = 1, Total = 75m },
            Metadata: _metadata);
    }

    /// <summary>
    /// Measures record equality on two unequal events — a common operation when
    /// deduplicating change streams.
    /// </summary>
    /// <returns><c>true</c> if the events are equal.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc/event-equals")]
    public bool ChangeEvent_Equals()
    {
        return _eventA.Equals(_eventB);
    }

    /// <summary>
    /// Measures <c>with</c>-expression mutation — the non-destructive update path for records.
    /// </summary>
    /// <returns>A new event with a mutated operation.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc/event-with")]
    public ChangeEvent ChangeEvent_WithExpression()
    {
        return _eventA with { Operation = ChangeOperation.Delete };
    }

    private sealed class StubCdcPosition(long version) : CdcPosition
    {
        private readonly long _version = version;

        public override byte[] ToBytes() => BitConverter.GetBytes(_version);

        public override int CompareTo(CdcPosition? other)
        {
            if (other is null) return 1;
            if (other is not StubCdcPosition stub)
                throw new ArgumentException("Incompatible position type", nameof(other));
            return _version.CompareTo(stub._version);
        }

        public override string ToString() => $"Stub:{_version}";
    }
}
