using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding.Specification;

/// <summary>
/// Unit tests for <see cref="ShardedPaginationOptions"/>.
/// </summary>
public sealed class ShardedPaginationOptionsTests
{
    // ────────────────────────────────────────────────────────────
    //  Defaults
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Page_DefaultValue_IsOne()
    {
        // Arrange & Act
        var options = new ShardedPaginationOptions();

        // Assert
        options.Page.ShouldBe(1);
    }

    [Fact]
    public void PageSize_DefaultValue_IsTwenty()
    {
        // Arrange & Act
        var options = new ShardedPaginationOptions();

        // Assert
        options.PageSize.ShouldBe(20);
    }

    [Fact]
    public void Strategy_DefaultValue_IsOverfetchAndMerge()
    {
        // Arrange & Act
        var options = new ShardedPaginationOptions();

        // Assert
        options.Strategy.ShouldBe(ShardedPaginationStrategy.OverfetchAndMerge);
    }

    // ────────────────────────────────────────────────────────────
    //  Page — valid values
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Page_SetValid_UpdatesValue()
    {
        // Arrange
        var options = new ShardedPaginationOptions();

        // Act
        options.Page = 5;

        // Assert
        options.Page.ShouldBe(5);
    }

    // ────────────────────────────────────────────────────────────
    //  Page — invalid values
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Page_SetToZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new ShardedPaginationOptions();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => options.Page = 0);
    }

    [Fact]
    public void Page_SetNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new ShardedPaginationOptions();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => options.Page = -1);
    }

    // ────────────────────────────────────────────────────────────
    //  PageSize — valid values
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void PageSize_SetValid_UpdatesValue()
    {
        // Arrange
        var options = new ShardedPaginationOptions();

        // Act
        options.PageSize = 50;

        // Assert
        options.PageSize.ShouldBe(50);
    }

    // ────────────────────────────────────────────────────────────
    //  PageSize — invalid values
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void PageSize_SetToZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new ShardedPaginationOptions();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => options.PageSize = 0);
    }

    [Fact]
    public void PageSize_SetNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new ShardedPaginationOptions();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => options.PageSize = -1);
    }

    // ────────────────────────────────────────────────────────────
    //  Strategy
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Strategy_SetEstimateAndDistribute_UpdatesValue()
    {
        // Arrange
        var options = new ShardedPaginationOptions();

        // Act
        options.Strategy = ShardedPaginationStrategy.EstimateAndDistribute;

        // Assert
        options.Strategy.ShouldBe(ShardedPaginationStrategy.EstimateAndDistribute);
    }
}
