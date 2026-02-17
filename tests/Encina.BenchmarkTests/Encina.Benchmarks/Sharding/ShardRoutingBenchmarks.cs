using BenchmarkDotNet.Attributes;

using Encina.Sharding;
using Encina.Sharding.Routing;

namespace Encina.Benchmarks.Sharding;

/// <summary>
/// Benchmarks for shard routing latency across all routing strategies.
/// Target: &lt;1 microsecond per routing decision.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[MarkdownExporter]
public class ShardRoutingBenchmarks
{
    private HashShardRouter _hashRouter = null!;
    private RangeShardRouter _rangeRouter = null!;
    private DirectoryShardRouter _directoryRouter = null!;
    private GeoShardRouter _geoRouter = null!;
    private string[] _testKeys = null!;
    private int _keyIndex;

    [Params(3, 10, 50)]
    public int ShardCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var shards = Enumerable.Range(1, ShardCount)
            .Select(i => new ShardInfo($"shard-{i}", $"Server=localhost;Database=Shard{i}"))
            .ToList();
        var topology = new ShardTopology(shards);

        // Hash router
        _hashRouter = new HashShardRouter(topology);

        // Range router — split A-Z into ShardCount ranges
        var ranges = CreateRanges(ShardCount);
        _rangeRouter = new RangeShardRouter(topology, ranges);

        // Directory router — pre-populate 1000 mappings
        var store = new InMemoryShardDirectoryStore();
        _directoryRouter = new DirectoryShardRouter(topology, store);
        for (var i = 0; i < 1000; i++)
        {
            store.AddMapping($"key-{i}", $"shard-{(i % ShardCount) + 1}");
        }

        // Geo router — 3 regions cycling through shards
        var regions = new[]
        {
            new GeoRegion("US", $"shard-1"),
            new GeoRegion("EU", $"shard-{Math.Min(2, ShardCount)}"),
            new GeoRegion("AP", $"shard-{Math.Min(3, ShardCount)}")
        };
        _geoRouter = new GeoShardRouter(topology, regions, ExtractRegion, new GeoShardRouterOptions { DefaultRegion = "US" });

        // Pre-generate test keys
        _testKeys = Enumerable.Range(0, 1000)
            .Select(i => $"key-{i}")
            .ToArray();

        _keyIndex = 0;
    }

    [IterationSetup]
    public void IterationSetup() => _keyIndex = 0;

    [Benchmark(Baseline = true, Description = "Hash routing")]
    public string HashRouting()
    {
        var key = _testKeys[_keyIndex++ % _testKeys.Length];
        var result = _hashRouter.GetShardId(key);
        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);
        return shardId;
    }

    [Benchmark(Description = "Range routing")]
    public string RangeRouting()
    {
        // Use alphabetic keys for range routing
        var charKey = ((char)('A' + (_keyIndex++ % 25))).ToString();
        var result = _rangeRouter.GetShardId(charKey);
        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);
        return shardId;
    }

    [Benchmark(Description = "Directory routing")]
    public string DirectoryRouting()
    {
        var key = _testKeys[_keyIndex++ % _testKeys.Length];
        var result = _directoryRouter.GetShardId(key);
        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);
        return shardId;
    }

    [Benchmark(Description = "Geo routing")]
    public string GeoRouting()
    {
        var regions = new[] { "US", "EU", "AP" };
        var region = regions[_keyIndex % 3];
        var key = $"{region}:key-{_keyIndex++}";
        var result = _geoRouter.GetShardId(key);
        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);
        return shardId;
    }

    [Benchmark(Description = "Hash routing (miss → re-route)")]
    public string HashRoutingConsecutive()
    {
        // Route 10 keys consecutively to measure sustained throughput
        string lastShard = string.Empty;
        for (var i = 0; i < 10; i++)
        {
            var key = _testKeys[(_keyIndex + i) % _testKeys.Length];
            var result = _hashRouter.GetShardId(key);
            _ = result.IfRight(s => lastShard = s);
        }

        _keyIndex += 10;
        return lastShard;
    }

    [Benchmark(Description = "GetAllShardIds")]
    public IReadOnlyList<string> GetAllShardIds()
    {
        return _hashRouter.GetAllShardIds();
    }

    [Benchmark(Description = "GetShardConnectionString")]
    public string GetShardConnectionString()
    {
        var shardId = $"shard-{(_keyIndex++ % ShardCount) + 1}";
        var result = _hashRouter.GetShardConnectionString(shardId);
        string conn = string.Empty;
        _ = result.IfRight(s => conn = s);
        return conn;
    }

    [Benchmark(Description = "Directory add + lookup")]
    public string DirectoryAddAndLookup()
    {
        var key = $"new-key-{_keyIndex++}";
        _directoryRouter.AddMapping(key, "shard-1");
        var result = _directoryRouter.GetShardId(key);
        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);
        return shardId;
    }

    private static string ExtractRegion(string key)
    {
        var colonIndex = key.IndexOf(':', StringComparison.Ordinal);
        return colonIndex >= 0 ? key[..colonIndex] : "US";
    }

    private static List<ShardRange> CreateRanges(int count)
    {
        var ranges = new List<ShardRange>();
        var step = 26.0 / count;

        for (var i = 0; i < count; i++)
        {
            var startChar = (char)('A' + (int)(i * step));
            var endChar = i < count - 1
                ? (char)('A' + (int)((i + 1) * step))
                : (char)('Z' + 1); // Past Z for last range

            ranges.Add(new ShardRange(
                startChar.ToString(),
                i < count - 1 ? endChar.ToString() : null,
                $"shard-{i + 1}"));
        }

        return ranges;
    }
}
