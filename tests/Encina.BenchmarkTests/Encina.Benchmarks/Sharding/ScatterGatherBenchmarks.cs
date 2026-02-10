using BenchmarkDotNet.Attributes;

using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Execution;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Benchmarks.Sharding;

/// <summary>
/// Benchmarks for scatter-gather query execution with in-memory mock connections.
/// Measures the overhead of parallel shard coordination.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[MarkdownExporter]
public class ScatterGatherBenchmarks
{
    private ShardedQueryExecutor _executor = null!;
    private ShardTopology _topology = null!;

    [Params(3, 10, 25)]
    public int ShardCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var shards = Enumerable.Range(1, ShardCount)
            .Select(i => new ShardInfo($"shard-{i}", $"Server=localhost;Database=Shard{i}"))
            .ToList();
        _topology = new ShardTopology(shards);

        var options = new ScatterGatherOptions
        {
            MaxParallelism = Environment.ProcessorCount,
            Timeout = TimeSpan.FromSeconds(30),
            AllowPartialResults = false
        };

        _executor = new ShardedQueryExecutor(
            _topology,
            options,
            NullLogger<ShardedQueryExecutor>.Instance);
    }

    [Benchmark(Baseline = true, Description = "Scatter-gather all shards (sync result)")]
    public async Task<Either<EncinaError, ShardedQueryResult<string>>> ScatterGatherAllShards()
    {
        return await _executor.ExecuteAllAsync<string>(
            async (shardId, ct) =>
            {
                // Simulate minimal work per shard — no actual I/O
                await Task.CompletedTask;
                return Either<EncinaError, IReadOnlyList<string>>.Right(
                    new List<string> { $"result-from-{shardId}" });
            },
            CancellationToken.None);
    }

    [Benchmark(Description = "Scatter-gather subset (3 shards)")]
    public async Task<Either<EncinaError, ShardedQueryResult<string>>> ScatterGatherSubset()
    {
        var targetShards = _topology.AllShardIds.Take(Math.Min(3, ShardCount)).ToList();

        return await _executor.ExecuteAsync<string>(
            targetShards,
            async (shardId, ct) =>
            {
                await Task.CompletedTask;
                return Either<EncinaError, IReadOnlyList<string>>.Right(
                    new List<string> { $"result-from-{shardId}" });
            },
            CancellationToken.None);
    }

    [Benchmark(Description = "Scatter-gather with large results")]
    public async Task<Either<EncinaError, ShardedQueryResult<string>>> ScatterGatherLargeResults()
    {
        return await _executor.ExecuteAllAsync<string>(
            async (shardId, ct) =>
            {
                await Task.CompletedTask;
                // Each shard returns 100 results
                return Either<EncinaError, IReadOnlyList<string>>.Right(
                    Enumerable.Range(0, 100)
                        .Select(i => $"result-{shardId}-{i}")
                        .ToList());
            },
            CancellationToken.None);
    }

    [Benchmark(Description = "Scatter-gather single shard")]
    public async Task<Either<EncinaError, ShardedQueryResult<string>>> ScatterGatherSingleShard()
    {
        return await _executor.ExecuteAsync<string>(
            [_topology.AllShardIds[0]],
            async (shardId, ct) =>
            {
                await Task.CompletedTask;
                return Either<EncinaError, IReadOnlyList<string>>.Right(
                    new List<string> { $"result-from-{shardId}" });
            },
            CancellationToken.None);
    }

    [Benchmark(Description = "Topology lookup all shards")]
    public IReadOnlyList<ShardInfo> TopologyGetAllShards()
    {
        return _topology.GetAllShards();
    }
}
