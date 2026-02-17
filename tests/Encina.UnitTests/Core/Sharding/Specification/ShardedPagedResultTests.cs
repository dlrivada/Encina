using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding.Specification;

/// <summary>
/// Unit tests for <see cref="ShardedPagedResult{T}"/>.
/// </summary>
public sealed class ShardedPagedResultTests
{
    // ────────────────────────────────────────────────────────────
    //  TotalPages
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void TotalPages_CalculatesCorrectly()
    {
        // Arrange
        var result = new ShardedPagedResult<string>(
            Items: [],
            TotalCount: 100,
            Page: 1,
            PageSize: 20,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 50, ["shard-2"] = 50 },
            FailedShards: []);

        // Assert
        result.TotalPages.ShouldBe(5);
    }

    [Fact]
    public void TotalPages_RoundsUp()
    {
        // Arrange
        var result = new ShardedPagedResult<string>(
            Items: [],
            TotalCount: 101,
            Page: 1,
            PageSize: 20,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 51, ["shard-2"] = 50 },
            FailedShards: []);

        // Assert
        result.TotalPages.ShouldBe(6);
    }

    [Fact]
    public void TotalPages_ZeroPageSize_ReturnsZero()
    {
        // Arrange — construct the record directly to bypass ShardedPaginationOptions validation
        var result = new ShardedPagedResult<string>(
            Items: [],
            TotalCount: 100,
            Page: 1,
            PageSize: 0,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 100 },
            FailedShards: []);

        // Assert
        result.TotalPages.ShouldBe(0);
    }

    [Fact]
    public void TotalPages_ZeroTotalCount_ReturnsZero()
    {
        // Arrange
        var result = new ShardedPagedResult<string>(
            Items: [],
            TotalCount: 0,
            Page: 1,
            PageSize: 20,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 0 },
            FailedShards: []);

        // Assert
        result.TotalPages.ShouldBe(0);
    }

    // ────────────────────────────────────────────────────────────
    //  HasNextPage / HasPreviousPage
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void HasNextPage_FirstPageOfMany_ReturnsTrue()
    {
        // Arrange
        var result = new ShardedPagedResult<string>(
            Items: [],
            TotalCount: 100,
            Page: 1,
            PageSize: 20,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 100 },
            FailedShards: []);

        // Assert
        result.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public void HasNextPage_LastPage_ReturnsFalse()
    {
        // Arrange
        var result = new ShardedPagedResult<string>(
            Items: [],
            TotalCount: 100,
            Page: 5,
            PageSize: 20,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 100 },
            FailedShards: []);

        // Assert
        result.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public void HasPreviousPage_FirstPage_ReturnsFalse()
    {
        // Arrange
        var result = new ShardedPagedResult<string>(
            Items: [],
            TotalCount: 100,
            Page: 1,
            PageSize: 20,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 100 },
            FailedShards: []);

        // Assert
        result.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public void HasPreviousPage_SecondPage_ReturnsTrue()
    {
        // Arrange
        var result = new ShardedPagedResult<string>(
            Items: [],
            TotalCount: 100,
            Page: 2,
            PageSize: 20,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 100 },
            FailedShards: []);

        // Assert
        result.HasPreviousPage.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  IsComplete / IsPartial
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void IsComplete_NoFailedShards_ReturnsTrue()
    {
        // Arrange
        var result = new ShardedPagedResult<string>(
            Items: ["item1", "item2"],
            TotalCount: 50,
            Page: 1,
            PageSize: 20,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 25, ["shard-2"] = 25 },
            FailedShards: []);

        // Assert
        result.IsComplete.ShouldBeTrue();
        result.IsPartial.ShouldBeFalse();
    }

    [Fact]
    public void IsPartial_SomeFailedShards_ReturnsTrue()
    {
        // Arrange
        var failure = new ShardFailure("shard-2", default);
        var result = new ShardedPagedResult<string>(
            Items: ["item1"],
            TotalCount: 25,
            Page: 1,
            PageSize: 20,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 25 },
            FailedShards: [failure]);

        // Assert
        result.IsComplete.ShouldBeFalse();
        result.IsPartial.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  ShardsQueried
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ShardsQueried_ReturnsSumOfSuccessAndFailed()
    {
        // Arrange
        var failure = new ShardFailure("shard-3", default);
        var result = new ShardedPagedResult<string>(
            Items: ["item1"],
            TotalCount: 50,
            Page: 1,
            PageSize: 20,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 25, ["shard-2"] = 25 },
            FailedShards: [failure]);

        // Assert
        result.ShardsQueried.ShouldBe(3);
    }
}
