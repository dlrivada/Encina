using BenchmarkDotNet.Attributes;
using LanguageExt;

namespace Encina.Benchmarks.Compliance.Retention;

/// <summary>
/// Benchmarks for the retention record service operations:
/// entity tracking (record creation + expiration calculation) and the record state transitions
/// (expired, deleted, anonymized).
/// </summary>
/// <remarks>
/// Split from <see cref="RetentionServiceBenchmarks"/> so each class runs as its own matrix job
/// under the 60-minute limit (#2554).
/// <code>
/// dotnet run -c Release -- --filter "*RetentionRecordBenchmarks*"
/// </code>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class RetentionRecordBenchmarks : RetentionBenchmarkBase
{
    [Benchmark(Baseline = true, Description = "Record: track entity")]
    public async Task<Guid> Record_TrackEntity()
    {
        var result = await RecordService.TrackEntityAsync(
            "customer-12345", "customer-data",
            Guid.NewGuid(), TimeSpan.FromDays(365));
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    [Benchmark(Description = "Record: mark expired")]
    public async Task<Unit> Record_MarkExpired()
    {
        var result = await RecordService.MarkExpiredAsync(Guid.NewGuid());
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Record: mark deleted (terminal)")]
    public async Task<Unit> Record_MarkDeleted()
    {
        var result = await RecordService.MarkDeletedAsync(Guid.NewGuid());
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Record: mark anonymized (terminal)")]
    public async Task<Unit> Record_MarkAnonymized()
    {
        var result = await RecordService.MarkAnonymizedAsync(Guid.NewGuid());
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }
}
