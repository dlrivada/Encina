using BenchmarkDotNet.Attributes;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;

namespace Encina.IdGeneration.Benchmarks;

/// <summary>
/// Cross-strategy comparison benchmarks for all ID generation algorithms.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class IdGenerationComparisonBenchmarks
{
    private SnowflakeIdGenerator _snowflake = null!;
    private UlidIdGenerator _ulid = null!;
    private UuidV7IdGenerator _uuidV7 = null!;
    private ShardPrefixedIdGenerator _shardPrefixed = null!;

    [GlobalSetup]
    public void Setup()
    {
        _snowflake = new SnowflakeIdGenerator(new SnowflakeOptions());
        _ulid = new UlidIdGenerator();
        _uuidV7 = new UuidV7IdGenerator();
        _shardPrefixed = new ShardPrefixedIdGenerator(new ShardPrefixedOptions());
    }

    [Benchmark(Baseline = true)]
    public long Snowflake()
    {
        var result = _snowflake.Generate();
        return result.Match(id => id.Value, _ => 0L);
    }

    [Benchmark]
    public UlidId Ulid()
    {
        var result = _ulid.Generate();
        return result.Match(id => id, _ => default);
    }

    [Benchmark]
    public Guid UuidV7()
    {
        var result = _uuidV7.Generate();
        return result.Match(id => id.Value, _ => Guid.Empty);
    }

    [Benchmark]
    public string ShardPrefixed()
    {
        var result = _shardPrefixed.Generate("shard-01");
        return result.Match(id => id.ToString(), _ => string.Empty);
    }

    [Benchmark]
    public Guid DotNet_GuidNewGuid()
    {
        return Guid.NewGuid();
    }
}
