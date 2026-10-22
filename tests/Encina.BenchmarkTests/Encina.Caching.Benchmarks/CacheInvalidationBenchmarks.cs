using BenchmarkDotNet.Attributes;
using LanguageExt;

namespace Encina.Caching.Benchmarks;

/// <summary>
/// Benchmarks for CacheInvalidationPipelineBehavior with real caching: single commands,
/// with and without matching keys, and sequential commands.
/// </summary>
/// <remarks>
/// Split into <see cref="CacheInvalidationMultipleKeysBenchmarks"/> and
/// <see cref="CacheInvalidationConcurrentBenchmarks"/> so each class runs as its own matrix job under
/// the 60-minute limit (#2554). This class keeps its original name so the series of the benchmarks
/// it still contains keep their dashboard identity.
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory("Unstable")]
public class CacheInvalidationBenchmarks : CacheInvalidationBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public async Task<Either<EncinaError, InvalidationResult>> Invalidation_NoMatchingKeys()
    {
        return await Behavior.Handle(Command, Context, Handler, CancellationToken.None);
    }

    [Benchmark]
    public async Task<Either<EncinaError, InvalidationResult>> Invalidation_WithMatchingKeys()
    {
        // Pre-populate cache with keys that match the invalidation pattern
        var productId = Command.ProductId;
        await CacheProvider.SetAsync($"bench:product:{productId}:details", "details", TimeSpan.FromMinutes(5), CancellationToken.None);
        await CacheProvider.SetAsync($"bench:product:{productId}:pricing", "pricing", TimeSpan.FromMinutes(5), CancellationToken.None);
        await CacheProvider.SetAsync($"bench:product:{productId}:inventory", "inventory", TimeSpan.FromMinutes(5), CancellationToken.None);

        return await Behavior.Handle(Command, Context, Handler, CancellationToken.None);
    }

    [Benchmark]
    public async Task Invalidation_SequentialCommands()
    {
        for (var i = 0; i < 10; i++)
        {
            var command = new InvalidatingCommand(Guid.NewGuid(), $"Command-{i}");
            await Behavior.Handle(command, Context, Handler, CancellationToken.None);
        }
    }
}
