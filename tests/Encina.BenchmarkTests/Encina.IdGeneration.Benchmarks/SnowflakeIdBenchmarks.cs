using BenchmarkDotNet.Attributes;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;

namespace Encina.IdGeneration.Benchmarks;

/// <summary>
/// Benchmarks for Snowflake ID generation throughput and shard operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class SnowflakeIdBenchmarks
{
    private SnowflakeIdGenerator _generator = null!;

    [GlobalSetup]
    public void Setup()
    {
        _generator = new SnowflakeIdGenerator(new SnowflakeOptions());
    }

    [Benchmark(Baseline = true)]
    public SnowflakeId Generate()
    {
        var result = _generator.Generate();
        return result.Match(id => id, _ => default);
    }

    [Benchmark]
    public SnowflakeId Generate_WithShardId()
    {
        var result = _generator.Generate("42");
        return result.Match(id => id, _ => default);
    }

    [Benchmark]
    public string ExtractShardId()
    {
        var genResult = _generator.Generate("42");
        var id = genResult.Match(id => id, _ => default);
        var extractResult = _generator.ExtractShardId(id);
        return extractResult.Match(s => s, _ => string.Empty);
    }

    [Benchmark]
    public long GenerateAndGetValue()
    {
        var result = _generator.Generate();
        return result.Match(id => id.Value, _ => 0L);
    }
}
