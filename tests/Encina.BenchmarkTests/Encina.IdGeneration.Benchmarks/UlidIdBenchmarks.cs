using BenchmarkDotNet.Attributes;
using Encina.IdGeneration.Generators;

namespace Encina.IdGeneration.Benchmarks;

/// <summary>
/// Benchmarks for ULID ID generation throughput and operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class UlidIdBenchmarks
{
    private UlidIdGenerator _generator = null!;

    [GlobalSetup]
    public void Setup()
    {
        _generator = new UlidIdGenerator();
    }

    [Benchmark(Baseline = true)]
    public UlidId Generate()
    {
        var result = _generator.Generate();
        return result.Match(id => id, _ => default);
    }

    [Benchmark]
    public string Generate_ToString()
    {
        var result = _generator.Generate();
        var id = result.Match(id => id, _ => default);
        return id.ToString();
    }

    [Benchmark]
    public UlidId NewUlid_Direct()
    {
        return UlidId.NewUlid();
    }
}
