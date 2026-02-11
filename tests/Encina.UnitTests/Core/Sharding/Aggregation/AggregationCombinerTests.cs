using Encina.Sharding;
using Encina.Sharding.Aggregation;

namespace Encina.UnitTests.Core.Sharding.Aggregation;

/// <summary>
/// Unit tests for <see cref="AggregationCombiner"/>.
/// </summary>
public sealed class AggregationCombinerTests
{
    // ────────────────────────────────────────────────────────────
    //  CombineCount
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CombineCount_EmptyPartials_ReturnsZero()
    {
        // Arrange
        var partials = Array.Empty<ShardAggregatePartial<int>>();

        // Act
        var result = AggregationCombiner.CombineCount(partials);

        // Assert
        result.ShouldBe(0L);
    }

    [Fact]
    public void CombineCount_SingleShard_ReturnsShardCount()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 100, Count: 42, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineCount(partials);

        // Assert
        result.ShouldBe(42L);
    }

    [Fact]
    public void CombineCount_MultipleShards_SumsAllCounts()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 10, Min: null, Max: null),
            new("shard-2", Sum: 0, Count: 20, Min: null, Max: null),
            new("shard-3", Sum: 0, Count: 30, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineCount(partials);

        // Assert
        result.ShouldBe(60L);
    }

    [Fact]
    public void CombineCount_ZeroCountShards_ReturnsZero()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 0, Min: null, Max: null),
            new("shard-2", Sum: 0, Count: 0, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineCount(partials);

        // Assert
        result.ShouldBe(0L);
    }

    [Fact]
    public void CombineCount_LargeValues_HandlesOverflow()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 1_000_000_000, Min: null, Max: null),
            new("shard-2", Sum: 0, Count: 2_000_000_000, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineCount(partials);

        // Assert
        result.ShouldBe(3_000_000_000L);
    }

    // ────────────────────────────────────────────────────────────
    //  CombineSum
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CombineSum_EmptyPartials_ReturnsZero()
    {
        // Arrange
        var partials = Array.Empty<ShardAggregatePartial<int>>();

        // Act
        var result = AggregationCombiner.CombineSum(partials);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void CombineSum_SingleShard_ReturnsShardSum()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 500, Count: 10, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineSum(partials);

        // Assert
        result.ShouldBe(500);
    }

    [Fact]
    public void CombineSum_MultipleShards_SumsAllSums()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 100, Count: 5, Min: null, Max: null),
            new("shard-2", Sum: 200, Count: 10, Min: null, Max: null),
            new("shard-3", Sum: 300, Count: 15, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineSum(partials);

        // Assert
        result.ShouldBe(600);
    }

    [Fact]
    public void CombineSum_WithDecimalType_SumsCorrectly()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<decimal>> partials =
        [
            new("shard-1", Sum: 100.50m, Count: 2, Min: null, Max: null),
            new("shard-2", Sum: 200.75m, Count: 3, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineSum(partials);

        // Assert
        result.ShouldBe(301.25m);
    }

    [Fact]
    public void CombineSum_WithDoubleType_SumsCorrectly()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<double>> partials =
        [
            new("shard-1", Sum: 1.5, Count: 1, Min: null, Max: null),
            new("shard-2", Sum: 2.5, Count: 1, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineSum(partials);

        // Assert
        result.ShouldBe(4.0);
    }

    [Fact]
    public void CombineSum_NegativeValues_SumsCorrectly()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: -50, Count: 5, Min: null, Max: null),
            new("shard-2", Sum: 100, Count: 10, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineSum(partials);

        // Assert
        result.ShouldBe(50);
    }

    // ────────────────────────────────────────────────────────────
    //  CombineAvg (two-phase aggregation)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CombineAvg_EmptyPartials_ReturnsZero()
    {
        // Arrange
        var partials = Array.Empty<ShardAggregatePartial<int>>();

        // Act
        var result = AggregationCombiner.CombineAvg(partials);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void CombineAvg_AllZeroCount_ReturnsZero()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 0, Min: null, Max: null),
            new("shard-2", Sum: 0, Count: 0, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineAvg(partials);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void CombineAvg_SingleShard_ReturnsSumDividedByCount()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 100, Count: 10, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineAvg(partials);

        // Assert
        result.ShouldBe(10);
    }

    [Fact]
    public void CombineAvg_EqualRowCountShards_ComputesCorrectAverage()
    {
        // Arrange: Both shards have 5 rows each
        IReadOnlyList<ShardAggregatePartial<double>> partials =
        [
            new("shard-1", Sum: 50.0, Count: 5, Min: null, Max: null),
            new("shard-2", Sum: 100.0, Count: 5, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineAvg(partials);

        // Assert: (50 + 100) / (5 + 5) = 15.0
        result.ShouldBe(15.0);
    }

    /// <summary>
    /// CRITICAL TEST: Verifies that two-phase avg avoids the "average-of-averages" error.
    /// Shard A has 1 item with value 10, Shard B has 99 items with value 1.
    /// Naive average-of-averages: (10/1 + 99/99) / 2 = (10 + 1) / 2 = 5.5 (WRONG!)
    /// Correct two-phase avg: (10 + 99) / (1 + 99) = 109 / 100 = 1.09 (CORRECT!)
    /// </summary>
    [Fact]
    public void CombineAvg_UnequalRowCounts_AvoidAverageOfAveragesError()
    {
        // Arrange: Shard A = 1 row, value 10. Shard B = 99 rows, total sum = 99
        IReadOnlyList<ShardAggregatePartial<double>> partials =
        [
            new("shard-A", Sum: 10.0, Count: 1, Min: null, Max: null),
            new("shard-B", Sum: 99.0, Count: 99, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineAvg(partials);

        // Assert: (10 + 99) / (1 + 99) = 1.09, NOT 5.5
        result.ShouldBe(1.09, tolerance: 0.0001);
    }

    [Fact]
    public void CombineAvg_HighlySkewedData_ProducesCorrectResult()
    {
        // Arrange: One shard has 1M rows, another has just 1 row with outlier value
        IReadOnlyList<ShardAggregatePartial<double>> partials =
        [
            new("shard-main", Sum: 1_000_000.0, Count: 1_000_000, Min: null, Max: null),
            new("shard-outlier", Sum: 1_000_000.0, Count: 1, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineAvg(partials);

        // Assert: (1M + 1M) / (1M + 1) ≈ 1.999998
        var expected = 2_000_000.0 / 1_000_001.0;
        result.ShouldBe(expected, tolerance: 0.001);
    }

    [Fact]
    public void CombineAvg_ThreeShards_ProducesCorrectResult()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<double>> partials =
        [
            new("shard-1", Sum: 30.0, Count: 3, Min: null, Max: null),   // avg per shard = 10
            new("shard-2", Sum: 200.0, Count: 10, Min: null, Max: null), // avg per shard = 20
            new("shard-3", Sum: 70.0, Count: 7, Min: null, Max: null)    // avg per shard = 10
        ];

        // Act
        var result = AggregationCombiner.CombineAvg(partials);

        // Assert: (30 + 200 + 70) / (3 + 10 + 7) = 300 / 20 = 15.0
        result.ShouldBe(15.0);
    }

    [Fact]
    public void CombineAvg_WithDecimalType_ProducesCorrectResult()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<decimal>> partials =
        [
            new("shard-1", Sum: 100.50m, Count: 3, Min: null, Max: null),
            new("shard-2", Sum: 200.25m, Count: 7, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineAvg(partials);

        // Assert: (100.50 + 200.25) / (3 + 7) = 300.75 / 10 = 30.075
        result.ShouldBe(30.075m);
    }

    // ────────────────────────────────────────────────────────────
    //  CombineMin
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CombineMin_EmptyPartials_ReturnsNull()
    {
        // Arrange
        var partials = Array.Empty<ShardAggregatePartial<int>>();

        // Act
        var result = AggregationCombiner.CombineMin(partials);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CombineMin_AllNullMins_ReturnsNull()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 0, Min: null, Max: null),
            new("shard-2", Sum: 0, Count: 0, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineMin(partials);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CombineMin_SingleShard_ReturnsShardMin()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 15, Count: 3, Min: 3, Max: 7)
        ];

        // Act
        var result = AggregationCombiner.CombineMin(partials);

        // Assert
        result.ShouldBe(3);
    }

    [Fact]
    public void CombineMin_MultipleShards_ReturnsGlobalMinimum()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 5, Min: 10, Max: 100),
            new("shard-2", Sum: 0, Count: 3, Min: 5, Max: 50),
            new("shard-3", Sum: 0, Count: 7, Min: 20, Max: 200)
        ];

        // Act
        var result = AggregationCombiner.CombineMin(partials);

        // Assert
        result.ShouldBe(5);
    }

    [Fact]
    public void CombineMin_SomeNullMins_IgnoresNulls()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 5, Min: 10, Max: 100),
            new("shard-2", Sum: 0, Count: 0, Min: null, Max: null),
            new("shard-3", Sum: 0, Count: 3, Min: 20, Max: 200)
        ];

        // Act
        var result = AggregationCombiner.CombineMin(partials);

        // Assert
        result.ShouldBe(10);
    }

    [Fact]
    public void CombineMin_NegativeValues_ReturnsSmallestNegative()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 2, Min: -50, Max: 10),
            new("shard-2", Sum: 0, Count: 3, Min: -100, Max: 5)
        ];

        // Act
        var result = AggregationCombiner.CombineMin(partials);

        // Assert
        result.ShouldBe(-100);
    }

    [Fact]
    public void CombineMin_WithDoubleType_ReturnsCorrectMin()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<double>> partials =
        [
            new("shard-1", Sum: 0, Count: 2, Min: 1.5, Max: 10.0),
            new("shard-2", Sum: 0, Count: 3, Min: 0.5, Max: 5.0)
        ];

        // Act
        var result = AggregationCombiner.CombineMin(partials);

        // Assert
        result.ShouldBe(0.5);
    }

    // ────────────────────────────────────────────────────────────
    //  CombineMax
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CombineMax_EmptyPartials_ReturnsNull()
    {
        // Arrange
        var partials = Array.Empty<ShardAggregatePartial<int>>();

        // Act
        var result = AggregationCombiner.CombineMax(partials);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CombineMax_AllNullMaxes_ReturnsNull()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 0, Min: null, Max: null),
            new("shard-2", Sum: 0, Count: 0, Min: null, Max: null)
        ];

        // Act
        var result = AggregationCombiner.CombineMax(partials);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CombineMax_SingleShard_ReturnsShardMax()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 15, Count: 3, Min: 3, Max: 7)
        ];

        // Act
        var result = AggregationCombiner.CombineMax(partials);

        // Assert
        result.ShouldBe(7);
    }

    [Fact]
    public void CombineMax_MultipleShards_ReturnsGlobalMaximum()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 5, Min: 10, Max: 100),
            new("shard-2", Sum: 0, Count: 3, Min: 5, Max: 50),
            new("shard-3", Sum: 0, Count: 7, Min: 20, Max: 200)
        ];

        // Act
        var result = AggregationCombiner.CombineMax(partials);

        // Assert
        result.ShouldBe(200);
    }

    [Fact]
    public void CombineMax_SomeNullMaxes_IgnoresNulls()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 5, Min: 10, Max: 100),
            new("shard-2", Sum: 0, Count: 0, Min: null, Max: null),
            new("shard-3", Sum: 0, Count: 3, Min: 20, Max: 200)
        ];

        // Act
        var result = AggregationCombiner.CombineMax(partials);

        // Assert
        result.ShouldBe(200);
    }

    [Fact]
    public void CombineMax_NegativeValues_ReturnsLargestNegative()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<int>> partials =
        [
            new("shard-1", Sum: 0, Count: 2, Min: -100, Max: -10),
            new("shard-2", Sum: 0, Count: 3, Min: -200, Max: -50)
        ];

        // Act
        var result = AggregationCombiner.CombineMax(partials);

        // Assert
        result.ShouldBe(-10);
    }

    [Fact]
    public void CombineMax_WithDecimalType_ReturnsCorrectMax()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<decimal>> partials =
        [
            new("shard-1", Sum: 0m, Count: 2, Min: 1.5m, Max: 99.99m),
            new("shard-2", Sum: 0m, Count: 3, Min: 0.5m, Max: 150.50m)
        ];

        // Act
        var result = AggregationCombiner.CombineMax(partials);

        // Assert
        result.ShouldBe(150.50m);
    }

    // ────────────────────────────────────────────────────────────
    //  Cross-operation consistency
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AllOperations_SamePartials_ProduceConsistentResults()
    {
        // Arrange: 3 shards with known data
        IReadOnlyList<ShardAggregatePartial<double>> partials =
        [
            new("shard-1", Sum: 30.0, Count: 3, Min: 5.0, Max: 15.0),
            new("shard-2", Sum: 40.0, Count: 4, Min: 2.0, Max: 20.0),
            new("shard-3", Sum: 50.0, Count: 5, Min: 3.0, Max: 25.0)
        ];

        // Act
        var count = AggregationCombiner.CombineCount(partials);
        var sum = AggregationCombiner.CombineSum(partials);
        var avg = AggregationCombiner.CombineAvg(partials);
        var min = AggregationCombiner.CombineMin(partials);
        var max = AggregationCombiner.CombineMax(partials);

        // Assert
        count.ShouldBe(12L);           // 3 + 4 + 5
        sum.ShouldBe(120.0);           // 30 + 40 + 50
        avg.ShouldBe(10.0);            // 120 / 12
        min.ShouldBe(2.0);             // min(5, 2, 3)
        max.ShouldBe(25.0);            // max(15, 20, 25)
    }

    [Fact]
    public void CombineAvg_EqualsSumDividedByCount()
    {
        // Arrange
        IReadOnlyList<ShardAggregatePartial<double>> partials =
        [
            new("shard-1", Sum: 100.0, Count: 10, Min: null, Max: null),
            new("shard-2", Sum: 200.0, Count: 20, Min: null, Max: null)
        ];

        // Act
        var sum = AggregationCombiner.CombineSum(partials);
        var count = AggregationCombiner.CombineCount(partials);
        var avg = AggregationCombiner.CombineAvg(partials);

        // Assert: avg should always equal sum / count
        var expected = sum / count;
        avg.ShouldBe(expected);
    }
}
