using BenchmarkDotNet.Attributes;
using LanguageExt;

namespace Encina.Caching.Benchmarks;

/// <summary>
/// Benchmarks for CacheInvalidationPipelineBehavior handling 10 and 50 concurrent commands.
/// </summary>
/// <remarks>
/// Split from <see cref="CacheInvalidationBenchmarks"/> so each class runs as its own matrix job
/// under the 60-minute limit (#2554). <c>Invalidation_NoMatchingKeys</c> is repeated here as the
/// baseline, because a ratio needs a baseline in the same class.
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory("Unstable")]
public class CacheInvalidationConcurrentBenchmarks : CacheInvalidationBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public async Task<Either<EncinaError, InvalidationResult>> Invalidation_NoMatchingKeys()
    {
        return await Behavior.Handle(Command, Context, Handler, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching/invalidation-concurrent")]
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task Invalidation_ConcurrentCommands(int concurrencyLevel)
    {
        var tasks = new Task<Either<EncinaError, InvalidationResult>>[concurrencyLevel];

        for (var i = 0; i < concurrencyLevel; i++)
        {
            var command = new InvalidatingCommand(Guid.NewGuid(), $"Concurrent-{i}");
            tasks[i] = Behavior.Handle(command, Context, Handler, CancellationToken.None).AsTask();
        }

        await Task.WhenAll(tasks);
    }
}
