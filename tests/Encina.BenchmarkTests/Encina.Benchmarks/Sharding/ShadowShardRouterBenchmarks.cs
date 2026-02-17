using BenchmarkDotNet.Attributes;

using Encina.Sharding;
using Encina.Sharding.Routing;
using Encina.Sharding.Shadow;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Benchmarks.Sharding;

/// <summary>
/// Benchmarks for shadow shard router decorator overhead.
/// Target: decorator overhead &lt;1 microsecond per routing decision.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[MarkdownExporter]
public class ShadowShardRouterBenchmarks
{
    private HashShardRouter _bareRouter = null!;
    private ShadowShardRouterDecorator _decoratedRouter = null!;
    private string[] _testKeys = null!;
    private int _keyIndex;

    [Params(3, 10, 50)]
    public int ShardCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var productionTopology = CreateTopology(ShardCount);
        var shadowTopology = CreateTopology(Math.Max(2, ShardCount / 2));

        _bareRouter = new HashShardRouter(productionTopology);

        var shadowRouter = new HashShardRouter(shadowTopology);
        var options = new ShadowShardingOptions
        {
            ShadowTopology = shadowTopology,
            DualWriteEnabled = true,
            ShadowReadPercentage = 10
        };

        _decoratedRouter = new ShadowShardRouterDecorator(
            _bareRouter,
            shadowRouter,
            options,
            NullLogger<ShadowShardRouterDecorator>.Instance);

        _testKeys = Enumerable.Range(0, 1000)
            .Select(i => $"key-{i}")
            .ToArray();

        _keyIndex = 0;
    }

    [IterationSetup]
    public void IterationSetup() => _keyIndex = 0;

    [Benchmark(Baseline = true, Description = "Bare HashRouter")]
    public string BareHashRouting()
    {
        var key = _testKeys[_keyIndex++ % _testKeys.Length];
        var result = _bareRouter.GetShardId(key);
        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);
        return shardId;
    }

    [Benchmark(Description = "Decorated GetShardId (production path)")]
    public string DecoratedGetShardId()
    {
        var key = _testKeys[_keyIndex++ % _testKeys.Length];
        var result = _decoratedRouter.GetShardId(key);
        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);
        return shardId;
    }

    [Benchmark(Description = "Decorated CompareAsync")]
    public ShadowComparisonResult DecoratedCompareAsync()
    {
        var key = _testKeys[_keyIndex++ % _testKeys.Length];
        return _decoratedRouter.CompareAsync(key, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    [Benchmark(Description = "Decorated GetAllShardIds")]
    public IReadOnlyList<string> DecoratedGetAllShardIds()
    {
        return _decoratedRouter.GetAllShardIds();
    }

    [Benchmark(Description = "Decorated GetShardConnectionString")]
    public string DecoratedGetShardConnectionString()
    {
        var shardId = $"shard-{(_keyIndex++ % ShardCount) + 1}";
        var result = _decoratedRouter.GetShardConnectionString(shardId);
        string connStr = string.Empty;
        _ = result.IfRight(s => connStr = s);
        return connStr;
    }

    private static ShardTopology CreateTopology(int shardCount)
    {
        var shards = Enumerable.Range(1, shardCount)
            .Select(i => new ShardInfo($"shard-{i}", $"Server=localhost;Database=Shard{i}"))
            .ToList();
        return new ShardTopology(shards);
    }
}
