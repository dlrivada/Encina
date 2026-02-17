using Encina.DomainModeling;
using Encina.DomainModeling.Sharding;

namespace Encina.UnitTests.DomainModeling.Sharding;

/// <summary>
/// Unit tests for <see cref="ScatterGatherResultMerger"/>.
/// </summary>
public sealed class ScatterGatherResultMergerTests
{
    // ────────────────────────────────────────────────────────────
    //  Test entities and specifications
    // ────────────────────────────────────────────────────────────

    private sealed class TestEntity
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public DateTime CreatedAt { get; init; }
    }

    private sealed class OrderByIdSpec : QuerySpecification<TestEntity>
    {
        public OrderByIdSpec()
        {
            ApplyOrderBy(e => e.Id);
        }

        public override System.Linq.Expressions.Expression<Func<TestEntity, bool>> ToExpression()
            => _ => true;
    }

    private sealed class OrderByIdDescSpec : QuerySpecification<TestEntity>
    {
        public OrderByIdDescSpec()
        {
            ApplyOrderByDescending(e => e.Id);
        }

        public override System.Linq.Expressions.Expression<Func<TestEntity, bool>> ToExpression()
            => _ => true;
    }

    private sealed class NoOrderSpec : Specification<TestEntity>
    {
        public override System.Linq.Expressions.Expression<Func<TestEntity, bool>> ToExpression()
            => _ => true;
    }

    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static Dictionary<string, IReadOnlyList<TestEntity>> CreatePerShardItems(
        params (string shardId, TestEntity[] items)[] shards)
    {
        var dict = new Dictionary<string, IReadOnlyList<TestEntity>>();
        foreach (var (shardId, items) in shards)
        {
            dict[shardId] = items;
        }

        return dict;
    }

    private static TestEntity Entity(int id, string name = "")
        => new() { Id = id, Name = name };

    // ────────────────────────────────────────────────────────────
    //  MergeAndOrder — empty / single shard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MergeAndOrder_EmptyShards_ReturnsEmptyList()
    {
        // Arrange
        var perShard = CreatePerShardItems();
        var spec = new OrderByIdSpec();

        // Act
        var result = ScatterGatherResultMerger.MergeAndOrder(perShard, spec);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void MergeAndOrder_SingleShard_ReturnsSameItems()
    {
        // Arrange
        var items = new[] { Entity(3), Entity(1), Entity(2) };
        var perShard = CreatePerShardItems(("shard-1", items));
        var spec = new OrderByIdSpec();

        // Act
        var result = ScatterGatherResultMerger.MergeAndOrder(perShard, spec);

        // Assert
        result.Count.ShouldBe(3);
        result.Select(e => e.Id).ShouldBe([1, 2, 3]);
    }

    // ────────────────────────────────────────────────────────────
    //  MergeAndOrder — multiple shards
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MergeAndOrder_MultipleShards_MergesAll()
    {
        // Arrange
        var perShard = CreatePerShardItems(
            ("shard-1", [Entity(1), Entity(3)]),
            ("shard-2", [Entity(2), Entity(4)]),
            ("shard-3", [Entity(5)]));
        var spec = new OrderByIdSpec();

        // Act
        var result = ScatterGatherResultMerger.MergeAndOrder(perShard, spec);

        // Assert
        result.Count.ShouldBe(5);
        result.Select(e => e.Id).ShouldBe([1, 2, 3, 4, 5]);
    }

    // ────────────────────────────────────────────────────────────
    //  MergeAndOrder — ordering
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MergeAndOrder_WithOrderByAsc_SortsAscending()
    {
        // Arrange
        var perShard = CreatePerShardItems(
            ("shard-1", [Entity(5), Entity(2)]),
            ("shard-2", [Entity(4), Entity(1), Entity(3)]));
        var spec = new OrderByIdSpec();

        // Act
        var result = ScatterGatherResultMerger.MergeAndOrder(perShard, spec);

        // Assert
        result.Select(e => e.Id).ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact]
    public void MergeAndOrder_WithOrderByDesc_SortsDescending()
    {
        // Arrange
        var perShard = CreatePerShardItems(
            ("shard-1", [Entity(1), Entity(4)]),
            ("shard-2", [Entity(2), Entity(5), Entity(3)]));
        var spec = new OrderByIdDescSpec();

        // Act
        var result = ScatterGatherResultMerger.MergeAndOrder(perShard, spec);

        // Assert
        result.Select(e => e.Id).ShouldBe([5, 4, 3, 2, 1]);
    }

    [Fact]
    public void MergeAndOrder_WithNoOrdering_ReturnsInMergeOrder()
    {
        // Arrange — plain Specification (no IQuerySpecification), so no ordering is applied
        var perShard = CreatePerShardItems(
            ("shard-1", [Entity(3), Entity(1)]),
            ("shard-2", [Entity(2)]));
        var spec = new NoOrderSpec();

        // Act
        var result = ScatterGatherResultMerger.MergeAndOrder(perShard, spec);

        // Assert — items appear in the order they were merged (shard-1 first, then shard-2)
        result.Count.ShouldBe(3);
        result[0].Id.ShouldBe(3);
        result[1].Id.ShouldBe(1);
        result[2].Id.ShouldBe(2);
    }

    // ────────────────────────────────────────────────────────────
    //  MergeAndOrder — null argument checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MergeAndOrder_NullPerShardItems_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyDictionary<string, IReadOnlyList<TestEntity>> perShard = null!;
        var spec = new OrderByIdSpec();

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => ScatterGatherResultMerger.MergeAndOrder(perShard, spec));
    }

    [Fact]
    public void MergeAndOrder_NullSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var perShard = CreatePerShardItems(("shard-1", [Entity(1)]));
        Specification<TestEntity> spec = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => ScatterGatherResultMerger.MergeAndOrder(perShard, spec));
    }

    // ────────────────────────────────────────────────────────────
    //  MergeOrderAndPaginate — pagination
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MergeOrderAndPaginate_FirstPage_ReturnsCorrectItems()
    {
        // Arrange — 7 items across 2 shards, pageSize = 5
        var perShard = CreatePerShardItems(
            ("shard-1", [Entity(1), Entity(3), Entity(5), Entity(7)]),
            ("shard-2", [Entity(2), Entity(4), Entity(6)]));
        var spec = new OrderByIdSpec();

        // Act
        var result = ScatterGatherResultMerger.MergeOrderAndPaginate(perShard, spec, page: 1, pageSize: 5);

        // Assert
        result.Count.ShouldBe(5);
        result.Select(e => e.Id).ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact]
    public void MergeOrderAndPaginate_SecondPage_SkipsFirstPage()
    {
        // Arrange — 7 items across 2 shards, page 2 pageSize 5 => items 6,7
        var perShard = CreatePerShardItems(
            ("shard-1", [Entity(1), Entity(3), Entity(5), Entity(7)]),
            ("shard-2", [Entity(2), Entity(4), Entity(6)]));
        var spec = new OrderByIdSpec();

        // Act
        var result = ScatterGatherResultMerger.MergeOrderAndPaginate(perShard, spec, page: 2, pageSize: 5);

        // Assert
        result.Count.ShouldBe(2);
        result.Select(e => e.Id).ShouldBe([6, 7]);
    }

    [Fact]
    public void MergeOrderAndPaginate_LastPage_ReturnsFewer()
    {
        // Arrange — 7 items, page 2, pageSize 5 => 2 items remaining
        var perShard = CreatePerShardItems(
            ("shard-1", [Entity(1), Entity(2), Entity(3), Entity(4)]),
            ("shard-2", [Entity(5), Entity(6), Entity(7)]));
        var spec = new OrderByIdSpec();

        // Act
        var result = ScatterGatherResultMerger.MergeOrderAndPaginate(perShard, spec, page: 2, pageSize: 5);

        // Assert
        result.Count.ShouldBe(2);
        result.Select(e => e.Id).ShouldBe([6, 7]);
    }

    [Fact]
    public void MergeOrderAndPaginate_BeyondLastPage_ReturnsEmpty()
    {
        // Arrange — 3 items total, page 2 with pageSize 5 => beyond available data
        var perShard = CreatePerShardItems(
            ("shard-1", [Entity(1), Entity(2)]),
            ("shard-2", [Entity(3)]));
        var spec = new OrderByIdSpec();

        // Act
        var result = ScatterGatherResultMerger.MergeOrderAndPaginate(perShard, spec, page: 2, pageSize: 5);

        // Assert
        result.ShouldBeEmpty();
    }

    // ────────────────────────────────────────────────────────────
    //  MergeOrderAndPaginate — null and invalid argument checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MergeOrderAndPaginate_NullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyDictionary<string, IReadOnlyList<TestEntity>> perShard = null!;
        var spec = new OrderByIdSpec();

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => ScatterGatherResultMerger.MergeOrderAndPaginate(perShard, spec, page: 1, pageSize: 10));
    }

    [Fact]
    public void MergeOrderAndPaginate_InvalidPage_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var perShard = CreatePerShardItems(("shard-1", [Entity(1)]));
        var spec = new OrderByIdSpec();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => ScatterGatherResultMerger.MergeOrderAndPaginate(perShard, spec, page: 0, pageSize: 10));
    }

    [Fact]
    public void MergeOrderAndPaginate_InvalidPageSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var perShard = CreatePerShardItems(("shard-1", [Entity(1)]));
        var spec = new OrderByIdSpec();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => ScatterGatherResultMerger.MergeOrderAndPaginate(perShard, spec, page: 1, pageSize: 0));
    }
}
