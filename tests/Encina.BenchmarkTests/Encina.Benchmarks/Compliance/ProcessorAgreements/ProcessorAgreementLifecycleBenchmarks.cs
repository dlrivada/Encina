using BenchmarkDotNet.Attributes;
using LanguageExt;

namespace Encina.Benchmarks.Compliance.ProcessorAgreements;

/// <summary>
/// Benchmarks for the Processor Agreements DPA lifecycle operations:
/// execute, amend, audit, renew and terminate.
/// </summary>
/// <remarks>
/// Split from <see cref="ProcessorAgreementServiceBenchmarks"/> so each class runs as its own
/// matrix job under the 60-minute limit (#2510).
/// <code>
/// dotnet run -c Release -- --filter "*ProcessorAgreementLifecycleBenchmarks*"
/// </code>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class ProcessorAgreementLifecycleBenchmarks : ProcessorAgreementBenchmarkBase
{
    [Benchmark(Baseline = true, Description = "ExecuteDPA (new agreement)")]
    public async Task<Guid> ExecuteDPA()
    {
        var result = await DpaService.ExecuteDPAAsync(
            Guid.NewGuid(), FullyCompliantTerms(), true,
            ["data-analytics"], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    [Benchmark(Description = "AmendDPA (update terms)")]
    public async Task<Unit> AmendDPA()
    {
        var result = await DpaService.AmendDPAAsync(
            ExistingDPAId, FullyCompliantTerms(), true,
            ["analytics", "reporting"], "Added reporting purpose");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "AuditDPA (record audit)")]
    public async Task<Unit> AuditDPA()
    {
        var result = await DpaService.AuditDPAAsync(
            ExistingDPAId, "auditor-bench", "Compliance verified");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "RenewDPA (extend expiration)")]
    public async Task<Unit> RenewDPA()
    {
        var result = await DpaService.RenewDPAAsync(
            ExistingDPAId, DateTimeOffset.UtcNow.AddYears(2));
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "TerminateDPA (end agreement)")]
    public async Task<Unit> TerminateDPA()
    {
        var result = await DpaService.TerminateDPAAsync(
            ExistingDPAId, "Benchmark termination");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }
}
