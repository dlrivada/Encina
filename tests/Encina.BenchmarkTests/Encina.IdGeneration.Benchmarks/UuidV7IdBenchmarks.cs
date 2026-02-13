using BenchmarkDotNet.Attributes;
using Encina.IdGeneration.Generators;

namespace Encina.IdGeneration.Benchmarks;

/// <summary>
/// Benchmarks for UUIDv7 ID generation throughput and operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class UuidV7IdBenchmarks
{
    private UuidV7IdGenerator _generator = null!;

    [GlobalSetup]
    public void Setup()
    {
        _generator = new UuidV7IdGenerator();
    }

    [Benchmark(Baseline = true)]
    public UuidV7Id Generate()
    {
        var result = _generator.Generate();
        return result.Match(id => id, _ => default);
    }

    [Benchmark]
    public Guid Generate_GetValue()
    {
        var result = _generator.Generate();
        return result.Match(id => id.Value, _ => Guid.Empty);
    }

    [Benchmark]
    public Guid NewGuid_Comparison()
    {
        return Guid.NewGuid();
    }
}
