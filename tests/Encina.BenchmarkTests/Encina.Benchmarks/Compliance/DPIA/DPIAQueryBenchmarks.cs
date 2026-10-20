using BenchmarkDotNet.Attributes;
using Encina.Compliance.DPIA.ReadModels;

namespace Encina.Benchmarks.Compliance.DPIA;

/// <summary>
/// Benchmarks for the DPIA assessment query operations (by ID, by request type, expired, all).
/// </summary>
/// <remarks>
/// <para>
/// <c>GetAssessmentByRequestTypeAsync</c> runs on every decorated request when
/// <c>DPIARequiredPipelineBehavior</c> is active, making it a hot-path operation.
/// </para>
/// <para>
/// Split from <see cref="DPIAServiceBenchmarks"/> so each class runs as its own matrix job under the
/// 60-minute limit (#2510).
/// <code>
/// dotnet run -c Release -- --filter "*DPIAQueryBenchmarks*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class DPIAQueryBenchmarks : DPIABenchmarkBase
{
    [Benchmark(Baseline = true, Description = "Get assessment by ID (cached)")]
    public async Task<DPIAReadModel?> GetAssessmentById()
    {
        var result = await Service.GetAssessmentAsync(ExistingAssessmentId);
        return result.Match(Right: rm => rm, Left: _ => null);
    }

    [Benchmark(Description = "Get assessment by request type (pipeline hot-path)")]
    public async Task<DPIAReadModel?> GetAssessmentByRequestType()
    {
        var result = await Service.GetAssessmentByRequestTypeAsync("BenchmarkApp.Commands.ProcessData");
        return result.Match(Right: rm => rm, Left: _ => null);
    }

    [Benchmark(Description = "Get expired assessments")]
    public async Task<IReadOnlyList<DPIAReadModel>?> GetExpiredAssessments()
    {
        var result = await Service.GetExpiredAssessmentsAsync();
        return result.Match(Right: list => list, Left: _ => null);
    }

    [Benchmark(Description = "Get all assessments")]
    public async Task<IReadOnlyList<DPIAReadModel>?> GetAllAssessments()
    {
        var result = await Service.GetAllAssessmentsAsync();
        return result.Match(Right: list => list, Left: _ => null);
    }
}
