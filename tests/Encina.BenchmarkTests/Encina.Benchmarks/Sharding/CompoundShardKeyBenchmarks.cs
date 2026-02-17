using BenchmarkDotNet.Attributes;

using Encina.Sharding;
using Encina.Sharding.Routing;

namespace Encina.Benchmarks.Sharding;

/// <summary>
/// Benchmarks for compound shard key extraction and routing overhead.
/// Compares compound vs simple key operations to measure the cost of multi-field routing.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[MarkdownExporter]
public class CompoundShardKeyBenchmarks
{
    private HashShardRouter _hashRouter = null!;
    private CompoundShardRouter _compoundRouter = null!;
    private ShardableEntity _simpleEntity = null!;
    private CompoundShardableEntity _compoundEntity2 = null!;
    private CompoundShardableEntity3 _compoundEntity3 = null!;
    private CompoundShardableEntity5 _compoundEntity5 = null!;
    private AttributeEntity _attributeEntity = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var shards = Enumerable.Range(1, 10)
            .Select(i => new ShardInfo($"shard-{i}", $"Server=localhost;Database=Shard{i}"))
            .ToList();
        var topology = new ShardTopology(shards);

        _hashRouter = new HashShardRouter(topology);

        var compoundOptions = new CompoundShardRouterOptions
        {
            ComponentRouters =
            {
                [0] = new HashShardRouter(topology),
                [1] = new HashShardRouter(topology)
            }
        };
        _compoundRouter = new CompoundShardRouter(topology, compoundOptions);

        _simpleEntity = new ShardableEntity("customer-123");
        _compoundEntity2 = new CompoundShardableEntity("us-east", "customer-123");
        _compoundEntity3 = new CompoundShardableEntity3("us-east", "customer-123", "premium");
        _compoundEntity5 = new CompoundShardableEntity5("us-east", "customer-123", "premium", "electronics", "2024");
        _attributeEntity = new AttributeEntity { Region = "us-east", CustomerId = "customer-123" };
    }

    // ────────────────────────────────────────────────────────────
    //  Extraction benchmarks
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Extract: IShardable (simple)")]
    public string ExtractSimpleKey()
    {
        var result = ShardKeyExtractor.Extract(_simpleEntity);
        string key = string.Empty;
        _ = result.IfRight(k => key = k);
        return key;
    }

    [Benchmark(Description = "Extract: ICompoundShardable (2 components)")]
    public string ExtractCompoundKey2()
    {
        var result = CompoundShardKeyExtractor.Extract(_compoundEntity2);
        string key = string.Empty;
        _ = result.IfRight(k => key = k.ToString());
        return key;
    }

    [Benchmark(Description = "Extract: ICompoundShardable (3 components)")]
    public string ExtractCompoundKey3()
    {
        var result = CompoundShardKeyExtractor.Extract(_compoundEntity3);
        string key = string.Empty;
        _ = result.IfRight(k => key = k.ToString());
        return key;
    }

    [Benchmark(Description = "Extract: ICompoundShardable (5 components)")]
    public string ExtractCompoundKey5()
    {
        var result = CompoundShardKeyExtractor.Extract(_compoundEntity5);
        string key = string.Empty;
        _ = result.IfRight(k => key = k.ToString());
        return key;
    }

    [Benchmark(Description = "Extract: [ShardKey] attributes (2 components)")]
    public string ExtractAttributeKey()
    {
        var result = CompoundShardKeyExtractor.Extract(_attributeEntity);
        string key = string.Empty;
        _ = result.IfRight(k => key = k.ToString());
        return key;
    }

    // ────────────────────────────────────────────────────────────
    //  Routing benchmarks
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Route: HashRouter (simple string key)")]
    public string RouteSimpleHash()
    {
        var result = _hashRouter.GetShardId("customer-123");
        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);
        return shardId;
    }

    [Benchmark(Description = "Route: HashRouter (compound key)")]
    public string RouteCompoundHash()
    {
        var key = new CompoundShardKey("us-east", "customer-123");
        var result = _hashRouter.GetShardId(key);
        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);
        return shardId;
    }

    [Benchmark(Description = "Route: CompoundRouter (2 components)")]
    public string RouteCompoundRouter()
    {
        var key = new CompoundShardKey("us-east", "customer-123");
        var result = _compoundRouter.GetShardId(key);
        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);
        return shardId;
    }

    // ────────────────────────────────────────────────────────────
    //  Test entities
    // ────────────────────────────────────────────────────────────

    private sealed class ShardableEntity(string shardKey) : IShardable
    {
        public string GetShardKey() => shardKey;
    }

    private sealed class CompoundShardableEntity(string region, string customerId) : ICompoundShardable
    {
        public CompoundShardKey GetCompoundShardKey() => new(region, customerId);
    }

    private sealed class CompoundShardableEntity3(string region, string customerId, string tier) : ICompoundShardable
    {
        public CompoundShardKey GetCompoundShardKey() => new(region, customerId, tier);
    }

    private sealed class CompoundShardableEntity5(string a, string b, string c, string d, string e) : ICompoundShardable
    {
        public CompoundShardKey GetCompoundShardKey() => new(a, b, c, d, e);
    }

    private sealed class AttributeEntity
    {
        [ShardKey(Order = 0)]
        public string Region { get; set; } = default!;

        [ShardKey(Order = 1)]
        public string CustomerId { get; set; } = default!;
    }
}
