using BenchmarkDotNet.Attributes;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;

namespace Encina.IdGeneration.Benchmarks;

/// <summary>
/// Benchmarks for ShardPrefixed ID generation throughput and shard extraction.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ShardPrefixedIdBenchmarks
{
    private ShardPrefixedIdGenerator _ulidGenerator = null!;
    private ShardPrefixedIdGenerator _uuidV7Generator = null!;
    private ShardPrefixedIdGenerator _timestampRandomGenerator = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ulidGenerator = new ShardPrefixedIdGenerator(
            new ShardPrefixedOptions { Format = ShardPrefixedFormat.Ulid });

        _uuidV7Generator = new ShardPrefixedIdGenerator(
            new ShardPrefixedOptions { Format = ShardPrefixedFormat.UuidV7 });

        _timestampRandomGenerator = new ShardPrefixedIdGenerator(
            new ShardPrefixedOptions { Format = ShardPrefixedFormat.TimestampRandom });
    }

    [Benchmark(Baseline = true)]
    public ShardPrefixedId Generate_UlidFormat()
    {
        var result = _ulidGenerator.Generate("shard-01");
        return result.Match(id => id, _ => default);
    }

    [Benchmark]
    public ShardPrefixedId Generate_UuidV7Format()
    {
        var result = _uuidV7Generator.Generate("shard-01");
        return result.Match(id => id, _ => default);
    }

    [Benchmark]
    public ShardPrefixedId Generate_TimestampRandomFormat()
    {
        var result = _timestampRandomGenerator.Generate("shard-01");
        return result.Match(id => id, _ => default);
    }

    [Benchmark]
    public string ExtractShardId_Ulid()
    {
        var genResult = _ulidGenerator.Generate("shard-01");
        var id = genResult.Match(id => id, _ => default);
        var extractResult = _ulidGenerator.ExtractShardId(id);
        return extractResult.Match(s => s, _ => string.Empty);
    }

    [Benchmark]
    public string Generate_ToString()
    {
        var result = _ulidGenerator.Generate("shard-01");
        var id = result.Match(id => id, _ => default);
        return id.ToString();
    }
}
