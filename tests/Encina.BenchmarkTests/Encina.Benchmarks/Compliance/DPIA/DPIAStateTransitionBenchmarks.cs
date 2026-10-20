using BenchmarkDotNet.Attributes;
using LanguageExt;

namespace Encina.Benchmarks.Compliance.DPIA;

/// <summary>
/// Benchmarks for the DPIA assessment state transitions: approve, reject, request revision and expire.
/// </summary>
/// <remarks>
/// Split from <see cref="DPIAServiceBenchmarks"/> so each class runs as its own matrix job under the
/// 60-minute limit (#2510).
/// <code>
/// dotnet run -c Release -- --filter "*DPIAStateTransitionBenchmarks*"
/// </code>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class DPIAStateTransitionBenchmarks : DPIABenchmarkBase
{
    [Benchmark(Baseline = true, Description = "Approve assessment")]
    public async Task<Unit> ApproveAssessment()
    {
        var result = await Service.ApproveAssessmentAsync(
            ExistingAssessmentId, "benchmark-approver", DateTimeOffset.UtcNow.AddDays(365));
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Reject assessment")]
    public async Task<Unit> RejectAssessment()
    {
        var result = await Service.RejectAssessmentAsync(
            ExistingAssessmentId, "benchmark-rejector", "Benchmark rejection");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Request revision")]
    public async Task<Unit> RequestRevision()
    {
        var result = await Service.RequestRevisionAsync(
            ExistingAssessmentId, "benchmark-reviewer", "Needs more mitigations");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Expire assessment")]
    public async Task<Unit> ExpireAssessment()
    {
        var result = await Service.ExpireAssessmentAsync(ExistingAssessmentId);
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }
}
