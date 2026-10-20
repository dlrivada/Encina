using BenchmarkDotNet.Attributes;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;

namespace Encina.Benchmarks.Compliance.ProcessorAgreements;

/// <summary>
/// Benchmarks for the Processor Agreements validation and query operations (pipeline hot-path):
/// - Agreement validation (hot-path — HasValidDPA check)
/// - Detailed DPA validation (full compliance check)
/// - Agreement queries by ID and by processor ID
/// - Expiring agreements query
/// </summary>
/// <remarks>
/// <para>
/// Processor agreement validation executes on every request when <c>ProcessorValidationPipelineBehavior</c>
/// is active. The pipeline calls <c>HasValidDPAAsync</c> on every decorated request, making it a
/// hot-path operation requiring performance characterization.
/// </para>
/// <para>
/// The Processor Agreements service benchmarks are split across <see cref="ProcessorAgreementServiceBenchmarks"/>,
/// <see cref="ProcessorAgreementLifecycleBenchmarks"/> and <see cref="ProcessorAgreementProcessorBenchmarks"/>
/// so each one runs as its own matrix job under the 60-minute limit (#2510). This class keeps its original
/// name so the series of the benchmarks it still contains keep their dashboard identity.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*ProcessorAgreementServiceBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*ProcessorAgreementServiceBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*ProcessorAgreement*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class ProcessorAgreementServiceBenchmarks : ProcessorAgreementBenchmarkBase
{
    [Benchmark(Baseline = true, Description = "HasValidDPA (pipeline hot-path)")]
    public async Task<bool> HasValidDPA()
    {
        var result = await DpaService.HasValidDPAAsync(ExistingProcessorId);
        return result.Match(Right: r => r, Left: _ => false);
    }

    [Benchmark(Description = "ValidateDPA (detailed compliance check)")]
    public async Task<DPAValidationResult?> ValidateDPA()
    {
        var result = await DpaService.ValidateDPAAsync(ExistingProcessorId);
        return result.Match(Right: r => r, Left: _ => null);
    }

    [Benchmark(Description = "GetDPA by ID (cached read)")]
    public async Task<DPAReadModel?> GetDPAById()
    {
        var result = await DpaService.GetDPAAsync(ExistingDPAId);
        return result.Match(Right: rm => rm, Left: _ => null);
    }

    [Benchmark(Description = "GetActiveDPA by processor ID")]
    public async Task<DPAReadModel?> GetActiveDPAByProcessorId()
    {
        var result = await DpaService.GetActiveDPAByProcessorIdAsync(ExistingProcessorId);
        return result.Match(Right: rm => rm, Left: _ => null);
    }

    [Benchmark(Description = "GetExpiringDPAs (filtered scan)")]
    public async Task<IReadOnlyList<DPAReadModel>?> GetExpiringDPAs()
    {
        var result = await DpaService.GetExpiringDPAsAsync();
        return result.Match(Right: list => list, Left: _ => null);
    }
}
