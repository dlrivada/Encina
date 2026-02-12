using Encina.DomainModeling;
using Encina.DomainModeling.Sharding;
using Encina.Sharding;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for specification scatter-gather invariants.
/// Verifies pagination arithmetic, merge-and-order guarantees, and count consistency
/// across all generated inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class ShardedSpecificationPropertyTests
{
    #region Pagination Invariants (ShardedPagedResult)

    [Property(MaxTest = 100)]
    public bool Property_TotalPages_AlwaysNonNegative(PositiveInt totalCount, PositiveInt pageSize)
    {
        var count = (long)Math.Clamp(totalCount.Get, 0, 100_000);
        var size = Math.Clamp(pageSize.Get, 1, 100);

        var result = new ShardedPagedResult<int>(
            Items: [],
            TotalCount: count,
            Page: 1,
            PageSize: size,
            CountPerShard: new Dictionary<string, long>(),
            FailedShards: []);

        return result.TotalPages >= 0;
    }

    [Property(MaxTest = 100)]
    public bool Property_TotalPages_CeilingDivision(PositiveInt totalCount, PositiveInt pageSize)
    {
        var count = (long)Math.Clamp(totalCount.Get, 0, 100_000);
        var size = Math.Clamp(pageSize.Get, 1, 100);

        var result = new ShardedPagedResult<int>(
            Items: [],
            TotalCount: count,
            Page: 1,
            PageSize: size,
            CountPerShard: new Dictionary<string, long>(),
            FailedShards: []);

        var expectedTotalPages = (int)Math.Ceiling((double)count / size);

        return result.TotalPages == expectedTotalPages;
    }

    [Property(MaxTest = 100)]
    public bool Property_HasNextPage_FalseOnLastPage(PositiveInt totalCount, PositiveInt pageSize)
    {
        var count = (long)Math.Clamp(totalCount.Get, 1, 100_000);
        var size = Math.Clamp(pageSize.Get, 1, 100);
        var totalPages = (int)Math.Ceiling((double)count / size);

        if (totalPages < 1) return true; // Skip degenerate cases

        var result = new ShardedPagedResult<int>(
            Items: [],
            TotalCount: count,
            Page: totalPages,
            PageSize: size,
            CountPerShard: new Dictionary<string, long>(),
            FailedShards: []);

        return !result.HasNextPage;
    }

    [Property(MaxTest = 100)]
    public bool Property_HasPreviousPage_FalseOnFirstPage(PositiveInt totalCount, PositiveInt pageSize)
    {
        var count = (long)Math.Clamp(totalCount.Get, 0, 100_000);
        var size = Math.Clamp(pageSize.Get, 1, 100);

        var result = new ShardedPagedResult<int>(
            Items: [],
            TotalCount: count,
            Page: 1,
            PageSize: size,
            CountPerShard: new Dictionary<string, long>(),
            FailedShards: []);

        return !result.HasPreviousPage;
    }

    [Property(MaxTest = 100)]
    public bool Property_HasPreviousPage_TrueAfterFirstPage(PositiveInt totalCount, PositiveInt pageSize, PositiveInt pageOffset)
    {
        var count = (long)Math.Clamp(totalCount.Get, 0, 100_000);
        var size = Math.Clamp(pageSize.Get, 1, 100);
        var page = Math.Clamp(pageOffset.Get, 2, 1000); // Always > 1

        var result = new ShardedPagedResult<int>(
            Items: [],
            TotalCount: count,
            Page: page,
            PageSize: size,
            CountPerShard: new Dictionary<string, long>(),
            FailedShards: []);

        return result.HasPreviousPage;
    }

    [Property(MaxTest = 100)]
    public bool Property_ShardsQueried_SumOfSuccessAndFailed(PositiveInt successCount, PositiveInt failCount)
    {
        var successes = Math.Clamp(successCount.Get, 1, 10);
        var failures = Math.Clamp(failCount.Get, 0, 5);

        var countPerShard = new Dictionary<string, long>();
        for (var i = 0; i < successes; i++)
        {
            countPerShard[$"shard-{i}"] = i * 10;
        }

        var failedShards = new List<ShardFailure>();
        for (var i = 0; i < failures; i++)
        {
            failedShards.Add(new ShardFailure($"failed-{i}", default));
        }

        var result = new ShardedPagedResult<int>(
            Items: [],
            TotalCount: countPerShard.Values.Sum(),
            Page: 1,
            PageSize: 10,
            CountPerShard: countPerShard,
            FailedShards: failedShards);

        return result.ShardsQueried == successes + failures;
    }

    #endregion

    #region MergeAndOrder Invariants (ScatterGatherResultMerger)

    [Property(MaxTest = 100)]
    public bool Property_MergeAndOrder_PreservesAllItems(PositiveInt shardCount)
    {
        var count = Math.Clamp(shardCount.Get, 1, 5);
        var perShardItems = new Dictionary<string, IReadOnlyList<int>>();
        var totalExpected = 0;

        for (var i = 0; i < count; i++)
        {
            var items = Enumerable.Range(i * 10, 3).ToList();
            perShardItems[$"shard-{i}"] = items;
            totalExpected += items.Count;
        }

        var spec = new NoOrderSpec();
        var merged = ScatterGatherResultMerger.MergeAndOrder(perShardItems, spec);

        return merged.Count == totalExpected;
    }

    [Property(MaxTest = 100)]
    public bool Property_MergeAndOrder_OrderedResultIsSorted(PositiveInt shardCount)
    {
        var count = Math.Clamp(shardCount.Get, 1, 5);
        var perShardItems = new Dictionary<string, IReadOnlyList<int>>();

        for (var i = 0; i < count; i++)
        {
            // Each shard has items in reverse order to exercise sorting
            var items = Enumerable.Range(i * 10, 5).Reverse().ToList();
            perShardItems[$"shard-{i}"] = items;
        }

        var spec = new OrderAscSpec();
        var merged = ScatterGatherResultMerger.MergeAndOrder(perShardItems, spec);

        // Verify the merged result is sorted ascending
        for (var i = 1; i < merged.Count; i++)
        {
            if (merged[i] < merged[i - 1])
            {
                return false;
            }
        }

        return true;
    }

    [Property(MaxTest = 100)]
    public bool Property_MergeOrderAndPaginate_PageSizeIsRespected(PositiveInt shardCount, PositiveInt pageSize)
    {
        var count = Math.Clamp(shardCount.Get, 1, 5);
        var size = Math.Clamp(pageSize.Get, 1, 20);
        var perShardItems = new Dictionary<string, IReadOnlyList<int>>();

        for (var i = 0; i < count; i++)
        {
            var items = Enumerable.Range(i * 10, 5).ToList();
            perShardItems[$"shard-{i}"] = items;
        }

        var spec = new OrderAscSpec();
        var paginated = ScatterGatherResultMerger.MergeOrderAndPaginate(perShardItems, spec, page: 1, pageSize: size);

        return paginated.Count <= size;
    }

    [Property(MaxTest = 100)]
    public bool Property_MergeOrderAndPaginate_AllItemsAppearExactlyOnce(PositiveInt shardCount)
    {
        var count = Math.Clamp(shardCount.Get, 1, 4);
        var perShardItems = new Dictionary<string, IReadOnlyList<int>>();
        var allExpected = new List<int>();

        for (var i = 0; i < count; i++)
        {
            var items = Enumerable.Range(i * 10, 5).ToList();
            perShardItems[$"shard-{i}"] = items;
            allExpected.AddRange(items);
        }

        var spec = new OrderAscSpec();
        var pageSize = 3;
        var totalPages = (int)Math.Ceiling((double)allExpected.Count / pageSize);
        var collected = new List<int>();

        for (var page = 1; page <= totalPages; page++)
        {
            var paginated = ScatterGatherResultMerger.MergeOrderAndPaginate(perShardItems, spec, page, pageSize);
            collected.AddRange(paginated);
        }

        // Every item appears exactly once across all pages
        if (collected.Count != allExpected.Count)
        {
            return false;
        }

        collected.Sort();
        allExpected.Sort();

        return collected.SequenceEqual(allExpected);
    }

    #endregion

    #region Count Consistency (ShardedCountResult)

    [Property(MaxTest = 100)]
    public bool Property_CountResult_ShardsQueried_Consistent(PositiveInt successCount, PositiveInt failCount)
    {
        var successes = Math.Clamp(successCount.Get, 1, 10);
        var failures = Math.Clamp(failCount.Get, 0, 5);

        var countPerShard = new Dictionary<string, long>();
        for (var i = 0; i < successes; i++)
        {
            countPerShard[$"shard-{i}"] = i * 10;
        }

        var failedShards = new List<ShardFailure>();
        for (var i = 0; i < failures; i++)
        {
            failedShards.Add(new ShardFailure($"failed-{i}", default));
        }

        var result = new ShardedCountResult(
            countPerShard.Values.Sum(),
            countPerShard,
            failedShards);

        return result.ShardsQueried == successes + failures;
    }

    [Property(MaxTest = 100)]
    public bool Property_IsComplete_TrueOnlyWhenNoFailures(PositiveInt successCount, PositiveInt failCount)
    {
        var successes = Math.Clamp(successCount.Get, 1, 10);
        var failures = Math.Clamp(failCount.Get, 0, 5);

        var countPerShard = new Dictionary<string, long>();
        for (var i = 0; i < successes; i++)
        {
            countPerShard[$"shard-{i}"] = i * 10;
        }

        var failedShards = new List<ShardFailure>();
        for (var i = 0; i < failures; i++)
        {
            failedShards.Add(new ShardFailure($"failed-{i}", default));
        }

        var result = new ShardedCountResult(
            countPerShard.Values.Sum(),
            countPerShard,
            failedShards);

        // IsComplete <=> FailedShards.Count == 0
        return result.IsComplete == (failures == 0);
    }

    [Property(MaxTest = 100)]
    public bool Property_IsPartial_TrueOnlyWhenMixed(PositiveInt successCount, PositiveInt failCount)
    {
        var successes = Math.Clamp(successCount.Get, 1, 10);
        var failures = Math.Clamp(failCount.Get, 0, 5);

        var countPerShard = new Dictionary<string, long>();
        for (var i = 0; i < successes; i++)
        {
            countPerShard[$"shard-{i}"] = i * 10;
        }

        var failedShards = new List<ShardFailure>();
        for (var i = 0; i < failures; i++)
        {
            failedShards.Add(new ShardFailure($"failed-{i}", default));
        }

        var result = new ShardedCountResult(
            countPerShard.Values.Sum(),
            countPerShard,
            failedShards);

        // IsPartial <=> (FailedShards.Count > 0 AND CountPerShard.Count > 0)
        var expectedPartial = failures > 0 && successes > 0;
        return result.IsPartial == expectedPartial;
    }

    #endregion

    #region Helper Specifications

    private sealed class NoOrderSpec : Specification<int>
    {
        public override System.Linq.Expressions.Expression<Func<int, bool>> ToExpression()
            => x => true;
    }

    private sealed class OrderAscSpec : QuerySpecification<int>
    {
        public OrderAscSpec()
        {
            ApplyOrderBy(x => x);
        }

        public override System.Linq.Expressions.Expression<Func<int, bool>> ToExpression()
            => x => true;
    }

    #endregion
}
