using System.Reflection;
using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardingErrorCodes"/>.
/// </summary>
public sealed class ShardingErrorCodesTests
{
    [Fact]
    public void ShardKeyNotConfigured_IsNotNullOrEmpty()
    {
        ShardingErrorCodes.ShardKeyNotConfigured.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ShardKeyEmpty_IsNotNullOrEmpty()
    {
        ShardingErrorCodes.ShardKeyEmpty.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ShardNotFound_IsNotNullOrEmpty()
    {
        ShardingErrorCodes.ShardNotFound.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void KeyOutsideRange_IsNotNullOrEmpty()
    {
        ShardingErrorCodes.KeyOutsideRange.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void OverlappingRanges_IsNotNullOrEmpty()
    {
        ShardingErrorCodes.OverlappingRanges.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void NoActiveShards_IsNotNullOrEmpty()
    {
        ShardingErrorCodes.NoActiveShards.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RegionNotFound_IsNotNullOrEmpty()
    {
        ShardingErrorCodes.RegionNotFound.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ScatterGatherTimeout_IsNotNullOrEmpty()
    {
        ShardingErrorCodes.ScatterGatherTimeout.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ScatterGatherPartialFailure_IsNotNullOrEmpty()
    {
        ShardingErrorCodes.ScatterGatherPartialFailure.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CacheOperationFailed_IsNotNullOrEmpty()
    {
        ShardingErrorCodes.CacheOperationFailed.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TopologyRefreshFailed_IsNotNullOrEmpty()
    {
        ShardingErrorCodes.TopologyRefreshFailed.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AllErrorCodes_HaveEncinaShardingPrefix()
    {
        // Arrange
        var fields = typeof(ShardingErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .ToList();

        // Assert
        fields.Count.ShouldBeGreaterThan(0);

        foreach (var field in fields)
        {
            var value = (string)field.GetValue(null)!;
            value.ShouldStartWith("encina.sharding.");
        }
    }

    [Fact]
    public void AllErrorCodes_AreUnique()
    {
        // Arrange
        var fields = typeof(ShardingErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        // Assert
        var distinct = fields.Distinct().Count();
        distinct.ShouldBe(fields.Count, "All error codes should be unique");
    }
}
