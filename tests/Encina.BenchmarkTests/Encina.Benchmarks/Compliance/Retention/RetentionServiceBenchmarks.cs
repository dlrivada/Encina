using BenchmarkDotNet.Attributes;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using LanguageExt;

namespace Encina.Benchmarks.Compliance.Retention;

/// <summary>
/// Benchmarks for the retention policy service operations.
/// Measures throughput and allocations for:
/// - Policy creation (fast path — single aggregate write)
/// - Retention period lookup (cached query with fallback)
/// - Policy lookup by ID
/// - Policy deactivation
/// </summary>
/// <remarks>
/// <para>
/// Policy lookups happen on every tracked entity creation, making them hot-path operations
/// requiring performance characterization.
/// </para>
/// <para>
/// The retention benchmarks are split across <see cref="RetentionServiceBenchmarks"/>,
/// <see cref="RetentionRecordBenchmarks"/> and <see cref="RetentionLegalHoldBenchmarks"/> so each one
/// runs as its own matrix job under the 60-minute limit (#2554). This class keeps its original
/// name so the series of the benchmarks it still contains keep their dashboard identity.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*RetentionServiceBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*RetentionServiceBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*Retention*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class RetentionServiceBenchmarks : RetentionBenchmarkBase
{
    [Benchmark(Baseline = true, Description = "Policy: create (fast path)")]
    public async Task<Guid> Policy_Create()
    {
        var result = await PolicyService.CreatePolicyAsync(
            "customer-data", TimeSpan.FromDays(365), true,
            RetentionPolicyType.TimeBased, "GDPR compliance", "Art. 5(1)(e)");
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    [Benchmark(Description = "Policy: get retention period (cached lookup)")]
    public async Task<TimeSpan> Policy_GetRetentionPeriod()
    {
        var result = await PolicyService.GetRetentionPeriodAsync("customer-data");
        return result.Match(Right: period => period, Left: _ => TimeSpan.Zero);
    }

    [Benchmark(Description = "Policy: get by ID")]
    public async Task<RetentionPolicyReadModel?> Policy_GetById()
    {
        var result = await PolicyService.GetPolicyAsync(Guid.NewGuid());
        return result.Match(Right: p => p, Left: _ => null);
    }

    [Benchmark(Description = "Policy: deactivate")]
    public async Task<Unit> Policy_Deactivate()
    {
        var result = await PolicyService.DeactivatePolicyAsync(
            Guid.NewGuid(), "No longer needed");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }
}
