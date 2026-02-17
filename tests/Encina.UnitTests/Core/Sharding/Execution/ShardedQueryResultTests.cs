using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding.Execution;

/// <summary>
/// Unit tests for <see cref="ShardedQueryResult{T}"/> and <see cref="ShardFailure"/>.
/// </summary>
public sealed class ShardedQueryResultTests
{
    // ────────────────────────────────────────────────────────────
    //  IsComplete / IsPartial
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void IsComplete_NoFailedShards_ReturnsTrue()
    {
        // Arrange
        var result = new ShardedQueryResult<string>(
            ["item1", "item2"],
            ["shard-1", "shard-2"],
            []);

        // Assert
        result.IsComplete.ShouldBeTrue();
        result.IsPartial.ShouldBeFalse();
    }

    [Fact]
    public void IsPartial_SomeFailedShards_ReturnsTrue()
    {
        // Arrange
        var failure = new ShardFailure("shard-2", default);
        var result = new ShardedQueryResult<string>(
            ["item1"],
            ["shard-1"],
            [failure]);

        // Assert
        result.IsComplete.ShouldBeFalse();
        result.IsPartial.ShouldBeTrue();
    }

    [Fact]
    public void IsPartial_AllFailed_ReturnsFalse()
    {
        // Arrange
        var failure = new ShardFailure("shard-1", default);
        var result = new ShardedQueryResult<string>(
            [],
            [],
            [failure]);

        // Assert
        result.IsComplete.ShouldBeFalse();
        result.IsPartial.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  TotalShardsQueried
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void TotalShardsQueried_ReturnsSuccessPlusFailed()
    {
        // Arrange
        var failure = new ShardFailure("shard-3", default);
        var result = new ShardedQueryResult<string>(
            ["item1", "item2"],
            ["shard-1", "shard-2"],
            [failure]);

        // Assert
        result.TotalShardsQueried.ShouldBe(3);
    }

    [Fact]
    public void TotalShardsQueried_EmptyResult_ReturnsZero()
    {
        // Arrange
        var result = new ShardedQueryResult<string>([], [], []);

        // Assert
        result.TotalShardsQueried.ShouldBe(0);
    }

    // ────────────────────────────────────────────────────────────
    //  ShardFailure record
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ShardFailure_RecordProperties_AreAccessible()
    {
        // Arrange
        var error = EncinaErrors.Create(ShardingErrorCodes.ShardNotFound, "Not found");
        var failure = new ShardFailure("shard-1", error);

        // Assert
        failure.ShardId.ShouldBe("shard-1");
        failure.Error.Message.ShouldBe("Not found");
    }

    [Fact]
    public void ShardFailure_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var error = EncinaErrors.Create(ShardingErrorCodes.ShardNotFound, "Not found");
        var f1 = new ShardFailure("shard-1", error);
        var f2 = new ShardFailure("shard-1", error);

        // Assert
        f1.ShouldBe(f2);
    }
}
