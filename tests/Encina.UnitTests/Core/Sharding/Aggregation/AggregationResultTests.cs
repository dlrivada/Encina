using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding.Aggregation;

/// <summary>
/// Unit tests for <see cref="AggregationResult{T}"/> and <see cref="ShardAggregatePartial{TValue}"/>.
/// </summary>
public sealed class AggregationResultTests
{
    // ────────────────────────────────────────────────────────────
    //  AggregationResult — Record properties
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AggregationResult_RecordProperties_AreAccessible()
    {
        // Arrange
        var failures = new List<ShardFailure>
        {
            new("shard-2", EncinaErrors.Create("test", "Shard failed"))
        };
        var duration = TimeSpan.FromMilliseconds(150);

        // Act
        var result = new AggregationResult<long>(42L, 3, failures, duration);

        // Assert
        result.Value.ShouldBe(42L);
        result.ShardsQueried.ShouldBe(3);
        result.FailedShards.Count.ShouldBe(1);
        result.Duration.ShouldBe(duration);
    }

    [Fact]
    public void AggregationResult_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(100);
        IReadOnlyList<ShardFailure> failures = [];
        var r1 = new AggregationResult<long>(10L, 2, failures, duration);
        var r2 = new AggregationResult<long>(10L, 2, failures, duration);

        // Assert
        r1.ShouldBe(r2);
    }

    // ────────────────────────────────────────────────────────────
    //  AggregationResult — IsPartial
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void IsPartial_NoFailedShards_ReturnsFalse()
    {
        // Arrange
        var result = new AggregationResult<long>(42L, 3, [], TimeSpan.FromMilliseconds(50));

        // Assert
        result.IsPartial.ShouldBeFalse();
    }

    [Fact]
    public void IsPartial_WithFailedShards_ReturnsTrue()
    {
        // Arrange
        var failure = new ShardFailure("shard-2", EncinaErrors.Create("test", "Failed"));
        var result = new AggregationResult<long>(30L, 3, [failure], TimeSpan.FromMilliseconds(50));

        // Assert
        result.IsPartial.ShouldBeTrue();
    }

    [Fact]
    public void IsPartial_AllFailed_ReturnsTrue()
    {
        // Arrange
        var failures = new List<ShardFailure>
        {
            new("shard-1", EncinaErrors.Create("test", "Failed 1")),
            new("shard-2", EncinaErrors.Create("test", "Failed 2"))
        };
        var result = new AggregationResult<long>(0L, 2, failures, TimeSpan.FromMilliseconds(50));

        // Assert
        result.IsPartial.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  AggregationResult — Nullable value type
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AggregationResult_WithNullableInt_SupportsNull()
    {
        // Arrange
        var result = new AggregationResult<int?>(null, 3, [], TimeSpan.FromMilliseconds(50));

        // Assert
        result.Value.ShouldBeNull();
        result.IsPartial.ShouldBeFalse();
    }

    [Fact]
    public void AggregationResult_WithNullableInt_SupportsValue()
    {
        // Arrange
        var result = new AggregationResult<int?>(42, 3, [], TimeSpan.FromMilliseconds(50));

        // Assert
        result.Value.ShouldBe(42);
    }

    // ────────────────────────────────────────────────────────────
    //  AggregationResult — With expression (record clone)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AggregationResult_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new AggregationResult<long>(42L, 3, [], TimeSpan.FromMilliseconds(50));

        // Act
        var modified = original with { Value = 100L };

        // Assert
        modified.Value.ShouldBe(100L);
        modified.ShardsQueried.ShouldBe(3);
        original.Value.ShouldBe(42L); // Original unchanged
    }

    // ────────────────────────────────────────────────────────────
    //  ShardAggregatePartial — Record properties
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ShardAggregatePartial_RecordProperties_AreAccessible()
    {
        // Arrange
        var partial = new ShardAggregatePartial<int>("shard-1", Sum: 100, Count: 10, Min: 5, Max: 20);

        // Assert
        partial.ShardId.ShouldBe("shard-1");
        partial.Sum.ShouldBe(100);
        partial.Count.ShouldBe(10L);
        partial.Min.ShouldBe(5);
        partial.Max.ShouldBe(20);
    }

    [Fact]
    public void ShardAggregatePartial_NullableMinMax_CanBeNull()
    {
        // Arrange
        var partial = new ShardAggregatePartial<int>("shard-1", Sum: 0, Count: 0, Min: null, Max: null);

        // Assert
        partial.Min.ShouldBeNull();
        partial.Max.ShouldBeNull();
    }

    [Fact]
    public void ShardAggregatePartial_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var p1 = new ShardAggregatePartial<int>("shard-1", Sum: 100, Count: 10, Min: 5, Max: 20);
        var p2 = new ShardAggregatePartial<int>("shard-1", Sum: 100, Count: 10, Min: 5, Max: 20);

        // Assert
        p1.ShouldBe(p2);
    }

    [Fact]
    public void ShardAggregatePartial_DifferentShardId_NotEqual()
    {
        // Arrange
        var p1 = new ShardAggregatePartial<int>("shard-1", Sum: 100, Count: 10, Min: 5, Max: 20);
        var p2 = new ShardAggregatePartial<int>("shard-2", Sum: 100, Count: 10, Min: 5, Max: 20);

        // Assert
        p1.ShouldNotBe(p2);
    }

    [Fact]
    public void ShardAggregatePartial_WithDecimalType_WorksCorrectly()
    {
        // Arrange
        var partial = new ShardAggregatePartial<decimal>("shard-1", Sum: 1234.56m, Count: 42, Min: 0.01m, Max: 999.99m);

        // Assert
        partial.Sum.ShouldBe(1234.56m);
        partial.Count.ShouldBe(42L);
        partial.Min.ShouldBe(0.01m);
        partial.Max.ShouldBe(999.99m);
    }

    [Fact]
    public void ShardAggregatePartial_WithDoubleType_WorksCorrectly()
    {
        // Arrange
        var partial = new ShardAggregatePartial<double>("shard-1", Sum: 3.14, Count: 1, Min: 3.14, Max: 3.14);

        // Assert
        partial.Sum.ShouldBe(3.14);
        partial.Min.ShouldBe(3.14);
        partial.Max.ShouldBe(3.14);
    }
}
