using BenchmarkDotNet.Attributes;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

namespace Encina.Benchmarks.Compliance.DPIA;

/// <summary>
/// Benchmarks for the DPIA compliance service creation, evaluation and consultation operations:
/// - Assessment creation (fast path — single aggregate write)
/// - Risk evaluation (assessment engine + risk criteria evaluation)
/// - DPO consultation request (state transition)
/// </summary>
/// <remarks>
/// <para>
/// DPIA assessment checks execute on every request when <c>DPIARequiredPipelineBehavior</c>
/// is active; the pipeline hot-path queries live in <see cref="DPIAQueryBenchmarks"/>.
/// </para>
/// <para>
/// The DPIA service benchmarks are split across <see cref="DPIAServiceBenchmarks"/>,
/// <see cref="DPIAStateTransitionBenchmarks"/> and <see cref="DPIAQueryBenchmarks"/> so each one runs
/// as its own matrix job under the 60-minute limit (#2510). This class keeps its original name so the
/// series of the benchmarks it still contains keep their dashboard identity.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*DPIAServiceBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*DPIAServiceBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*DPIA*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class DPIAServiceBenchmarks : DPIABenchmarkBase
{
    [Benchmark(Baseline = true, Description = "Create assessment (fast path)")]
    public async Task<Guid> CreateAssessment()
    {
        var result = await Service.CreateAssessmentAsync(
            "BenchmarkApp.Commands.ProcessData",
            "AutomatedDecisionMaking",
            "Benchmark test assessment");
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    [Benchmark(Description = "Evaluate assessment (risk engine)")]
    public async Task<DPIAResult?> EvaluateAssessment()
    {
        var result = await Service.EvaluateAssessmentAsync(ExistingAssessmentId, EvaluationContext);
        return result.Match(Right: r => r, Left: _ => null);
    }

    [Benchmark(Description = "RequiresDPIA check (pipeline hot-path)")]
    public async Task<bool> RequiresDPIA()
    {
        var result = await Engine.RequiresDPIAAsync(typeof(string));
        return result.Match(Right: r => r, Left: _ => false);
    }

    [Benchmark(Description = "Request DPO consultation")]
    public async Task<Guid> RequestDPOConsultation()
    {
        var result = await Service.RequestDPOConsultationAsync(ExistingAssessmentId);
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }
}
