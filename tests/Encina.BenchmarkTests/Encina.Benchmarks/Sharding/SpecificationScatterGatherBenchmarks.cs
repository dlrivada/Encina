using System.Linq.Expressions;

using BenchmarkDotNet.Attributes;

using Encina.DomainModeling;
using Encina.DomainModeling.Sharding;

namespace Encina.Benchmarks.Sharding;

/// <summary>
/// Benchmarks for specification-based scatter-gather operations.
/// Measures the overhead of result merging, ordering, and pagination.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[MarkdownExporter]
public class SpecificationScatterGatherBenchmarks
{
    private Dictionary<string, IReadOnlyList<BenchmarkEntity>> _perShardItems = null!;
    private OrderByIdAscSpec _orderByIdAscSpec = null!;
    private OrderByIdDescSpec _orderByIdDescSpec = null!;
    private NoOrderSpec _noOrderSpec = null!;

    [Params(3, 10, 25)]
    public int ShardCount { get; set; }

    [Params(10, 100)]
    public int ItemsPerShard { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _perShardItems = new Dictionary<string, IReadOnlyList<BenchmarkEntity>>(ShardCount);

        var globalId = 0;
        for (var shard = 0; shard < ShardCount; shard++)
        {
            var items = new List<BenchmarkEntity>(ItemsPerShard);
            for (var i = 0; i < ItemsPerShard; i++)
            {
                items.Add(new BenchmarkEntity
                {
                    Id = globalId++,
                    Name = $"Entity-{shard}-{i}",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-globalId),
                });
            }

            _perShardItems[$"shard-{shard}"] = items;
        }

        _orderByIdAscSpec = new OrderByIdAscSpec();
        _orderByIdDescSpec = new OrderByIdDescSpec();
        _noOrderSpec = new NoOrderSpec();
    }

    [Benchmark(Baseline = true, Description = "MergeAndOrder with ascending ordering")]
    public IReadOnlyList<BenchmarkEntity> MergeAndOrder_WithOrdering()
    {
        return ScatterGatherResultMerger.MergeAndOrder(_perShardItems, _orderByIdAscSpec);
    }

    [Benchmark(Description = "MergeAndOrder without ordering")]
    public IReadOnlyList<BenchmarkEntity> MergeAndOrder_NoOrdering()
    {
        return ScatterGatherResultMerger.MergeAndOrder(_perShardItems, _noOrderSpec);
    }

    [Benchmark(Description = "MergeOrderAndPaginate overfetch (page 1, size 20)")]
    public IReadOnlyList<BenchmarkEntity> MergeOrderAndPaginate_OverfetchStrategy()
    {
        return ScatterGatherResultMerger.MergeOrderAndPaginate(_perShardItems, _orderByIdAscSpec, 1, 20);
    }

    [Benchmark(Description = "MergeOrderAndPaginate large page (page 2, size 100)")]
    public IReadOnlyList<BenchmarkEntity> MergeOrderAndPaginate_LargePage()
    {
        return ScatterGatherResultMerger.MergeOrderAndPaginate(_perShardItems, _orderByIdAscSpec, 2, 100);
    }

    [Benchmark(Description = "MergeAndOrder with descending ordering")]
    public IReadOnlyList<BenchmarkEntity> MergeAndOrder_Descending()
    {
        return ScatterGatherResultMerger.MergeAndOrder(_perShardItems, _orderByIdDescSpec);
    }

    public sealed class BenchmarkEntity
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public DateTime CreatedAt { get; init; }
    }

    private sealed class OrderByIdAscSpec : QuerySpecification<BenchmarkEntity>
    {
        public OrderByIdAscSpec() => ApplyOrderBy(e => e.Id);
    }

    private sealed class OrderByIdDescSpec : QuerySpecification<BenchmarkEntity>
    {
        public OrderByIdDescSpec() => ApplyOrderByDescending(e => e.Id);
    }

    private sealed class NoOrderSpec : Specification<BenchmarkEntity>
    {
        public override Expression<Func<BenchmarkEntity, bool>> ToExpression() => _ => true;
    }
}
