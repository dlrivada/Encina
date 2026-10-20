using BenchmarkDotNet.Attributes;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;

namespace Encina.Benchmarks.Compliance.ProcessorAgreements;

/// <summary>
/// Benchmarks for the Processor Agreements processor operations:
/// registration, lookup and sub-processor chain traversal.
/// </summary>
/// <remarks>
/// Split from <see cref="ProcessorAgreementServiceBenchmarks"/> so each class runs as its own
/// matrix job under the 60-minute limit (#2510).
/// <code>
/// dotnet run -c Release -- --filter "*ProcessorAgreementProcessorBenchmarks*"
/// </code>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class ProcessorAgreementProcessorBenchmarks : ProcessorAgreementBenchmarkBase
{
    [Benchmark(Baseline = true, Description = "RegisterProcessor (new processor)")]
    public async Task<Guid> RegisterProcessor()
    {
        var result = await ProcessorService.RegisterProcessorAsync(
            "BenchProc", "DE", null, null, 0, SubProcessorAuthorizationType.Specific);
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    [Benchmark(Description = "GetProcessor by ID (cached read)")]
    public async Task<ProcessorReadModel?> GetProcessorById()
    {
        var result = await ProcessorService.GetProcessorAsync(ExistingProcessorId);
        return result.Match(Right: rm => rm, Left: _ => null);
    }

    [Benchmark(Description = "GetFullSubProcessorChain (BFS traversal)")]
    public async Task<IReadOnlyList<ProcessorReadModel>?> GetFullSubProcessorChain()
    {
        var result = await ProcessorService.GetFullSubProcessorChainAsync(ExistingProcessorId);
        return result.Match(Right: list => list, Left: _ => null);
    }
}
