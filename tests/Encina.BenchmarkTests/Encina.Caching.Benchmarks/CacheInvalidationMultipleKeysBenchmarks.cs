using BenchmarkDotNet.Attributes;
using LanguageExt;

namespace Encina.Caching.Benchmarks;

/// <summary>
/// Benchmarks for CacheInvalidationPipelineBehavior invalidating 5, 10 and 25 matching keys.
/// </summary>
/// <remarks>
/// Split from <see cref="CacheInvalidationBenchmarks"/> so each class runs as its own matrix job
/// under the 60-minute limit (#2554). <c>Invalidation_NoMatchingKeys</c> is repeated here as the
/// baseline, because a ratio needs a baseline in the same class.
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory("Unstable")]
public class CacheInvalidationMultipleKeysBenchmarks : CacheInvalidationBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public async Task<Either<EncinaError, InvalidationResult>> Invalidation_NoMatchingKeys()
    {
        return await Behavior.Handle(Command, Context, Handler, CancellationToken.None);
    }

    [Benchmark]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(25)]
    public async Task Invalidation_MultipleKeys(int keyCount)
    {
        var productId = Guid.NewGuid();
        var command = new InvalidatingCommand(productId, "MultiInvalidate");

        // Pre-populate cache
        for (var i = 0; i < keyCount; i++)
        {
            await CacheProvider.SetAsync(
                $"bench:product:{productId}:data-{i}",
                $"value-{i}",
                TimeSpan.FromMinutes(5),
                CancellationToken.None);
        }

        await Behavior.Handle(command, Context, Handler, CancellationToken.None);
    }
}
