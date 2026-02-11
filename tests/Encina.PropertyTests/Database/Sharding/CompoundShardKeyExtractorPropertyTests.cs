using Encina.Sharding;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for <see cref="CompoundShardKeyExtractor"/> invariants.
/// </summary>
[Trait("Category", "Property")]
public sealed class CompoundShardKeyExtractorPropertyTests
{
    #region ICompoundShardable extraction

    [Property(MaxTest = 100)]
    public bool Property_Extract_ICompoundShardable_AlwaysReturnsCompoundKey(NonEmptyString component1, NonEmptyString component2)
    {
        var entity = new TestCompoundShardable(component1.Get, component2.Get);
        var result = CompoundShardKeyExtractor.Extract(entity);

        if (!result.IsRight) return false;

        CompoundShardKey? key = null;
        _ = result.IfRight(k => key = k);

        return key is not null &&
               key.ComponentCount == 2 &&
               key.PrimaryComponent == component1.Get &&
               key.Components[1] == component2.Get;
    }

    #endregion

    #region IShardable backward compatibility

    [Property(MaxTest = 100)]
    public bool Property_Extract_IShardable_ReturnsSingleComponentKey(NonEmptyString shardKey)
    {
        var entity = new TestShardable(shardKey.Get);
        var result = CompoundShardKeyExtractor.Extract(entity);

        if (!result.IsRight) return false;

        CompoundShardKey? key = null;
        _ = result.IfRight(k => key = k);

        return key is not null &&
               key.ComponentCount == 1 &&
               key.PrimaryComponent == shardKey.Get;
    }

    #endregion

    #region Determinism

    [Property(MaxTest = 100)]
    public bool Property_Extract_SameEntity_AlwaysReturnsSameKey(NonEmptyString region, NonEmptyString customer)
    {
        var entity = new TestCompoundShardable(region.Get, customer.Get);
        var result1 = CompoundShardKeyExtractor.Extract(entity);
        var result2 = CompoundShardKeyExtractor.Extract(entity);

        if (!result1.IsRight || !result2.IsRight) return false;

        string key1 = string.Empty;
        string key2 = string.Empty;
        _ = result1.IfRight(k => key1 = k.ToString());
        _ = result2.IfRight(k => key2 = k.ToString());

        return key1 == key2;
    }

    #endregion

    #region Component ordering

    [Property(MaxTest = 50)]
    public bool Property_Extract_MultipleAttributes_PreservesOrder(NonEmptyString first, NonEmptyString second)
    {
        var entity = new TestMultiAttribute
        {
            Region = first.Get,
            CustomerId = second.Get
        };

        var result = CompoundShardKeyExtractor.Extract(entity);

        if (!result.IsRight) return false;

        CompoundShardKey? key = null;
        _ = result.IfRight(k => key = k);

        return key is not null &&
               key.Components[0] == first.Get &&
               key.Components[1] == second.Get;
    }

    #endregion

    #region Test entities

    private sealed class TestCompoundShardable(string region, string customer) : ICompoundShardable
    {
        public CompoundShardKey GetCompoundShardKey() => new(region, customer);
    }

    private sealed class TestShardable(string key) : IShardable
    {
        public string GetShardKey() => key;
    }

    private sealed class TestMultiAttribute
    {
        [ShardKey(Order = 0)]
        public string Region { get; set; } = default!;

        [ShardKey(Order = 1)]
        public string CustomerId { get; set; } = default!;
    }

    #endregion
}
