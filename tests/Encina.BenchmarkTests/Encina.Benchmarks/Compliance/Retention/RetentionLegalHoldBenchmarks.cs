using BenchmarkDotNet.Attributes;
using LanguageExt;

namespace Encina.Benchmarks.Compliance.Retention;

/// <summary>
/// Benchmarks for the legal hold service operations:
/// placing a hold (cross-aggregate coordination), lifting it and the read-only active hold check.
/// </summary>
/// <remarks>
/// Split from <see cref="RetentionServiceBenchmarks"/> so each class runs as its own matrix job
/// under the 60-minute limit (#2554).
/// <code>
/// dotnet run -c Release -- --filter "*RetentionLegalHoldBenchmarks*"
/// </code>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class RetentionLegalHoldBenchmarks : RetentionBenchmarkBase
{
    [Benchmark(Baseline = true, Description = "Legal hold: place (cross-aggregate)")]
    public async Task<Guid> LegalHold_Place()
    {
        var result = await HoldService.PlaceHoldAsync(
            "customer-12345", "Ongoing litigation - Case #54321",
            "legal-counsel-1");
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    [Benchmark(Description = "Legal hold: lift")]
    public async Task<Unit> LegalHold_Lift()
    {
        var result = await HoldService.LiftHoldAsync(
            Guid.NewGuid(), "legal-counsel-2");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Legal hold: has active holds (read-only)")]
    public async Task<bool> LegalHold_HasActiveHolds()
    {
        var result = await HoldService.HasActiveHoldsAsync("customer-12345");
        return result.Match(Right: has => has, Left: _ => false);
    }
}
